using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class Scout : MonoBehaviour
{
    [Header("Identity and scene references")]
    [Range(0, 1)] public int id;
    public string displayName = "Fern";
    public int CharacterIndex { get; private set; }
    public string SkillName => SkillIndex == 0 ? "VINE BOLT" : SkillIndex == 1 ? "FIRE BOLT" : SkillIndex == 2 ? "FROST WAVE" : "STONE WAVE";
    public string UltimateName => SkillIndex == 0 ? "WILDROOT PRISON" : SkillIndex == 1 ? "PHOENIX COMBO" : SkillIndex == 2 ? "MOONFROST SEAL" : "WORLD SHATTER";
    public const float SpecialCooldown = 6;
    public float SpecialCooldownRemaining => Mathf.Max(0, nextSpecial - Time.time);
    public float SpecialReady => 1 - Mathf.Clamp01(SpecialCooldownRemaining / SpecialCooldown);
    public bool UltimateUsed { get; private set; }
    public bool UltimateActive { get; private set; }
    public bool UltimateReady => !UltimateUsed && !KnockedOut && Health > 0 && Health <= 25;
    public static event System.Action<Scout, bool> UltimateCinematic;
    public static event System.Action<Scout, Scout> UltimateImpact;
    float nextSpecial;
    bool specialRequested, ultimateRequested, ultimateChordHeld, victoryPlaying;
    int SkillIndex => characterSelected ? CharacterIndex : id;
    bool characterSelected;
    Color characterElement;
    float slowedUntil, rootedUntil;
    public bool Rooted => Time.time < rootedUntil;
    public bool Slowed => Time.time < slowedUntil;
    Rigidbody2D groundBody;
    public bool inputEnabled = true;
    public bool aiControlled;
    public int AIDecisions { get; private set; }
    // Counts requested actions; normal cooldowns and validation still decide whether they execute.
    public int AIActionRequests { get; private set; }
    float nextAIThink, aiBlockUntil, aiChargeUntil, aiThreatSince = -1;
    BoxCollider2D aiPlankCollider;

    public SpriteRenderer art;
    public Animator animator;
    public ScoutAnimations animations;
    public Transform spawnPoint;
    [Header("Movement")]
    public float moveSpeed = 4.4f;
    public float jumpSpeed = 8.7f;
    public float acceleration = 28;
    public float airAcceleration = 8;
    public bool Grounded => grounded;
    public float MovementInput => axis;
    [Header("Defense and aerial combat")]
    public float shieldCapacity = 100;
    public float shieldDrain = 14;
    public float shieldRecovery = 25;
    public float parryWindow = .12f;
    public float dashCooldown = .85f;
    public float dashSpeed = 10;
    public float Shield { get; private set; } = 100;
    public float Stamina => Shield;
    public float StaminaCapacity => shieldCapacity;
    public float BackstepReady => Mathf.Clamp01(1 - (nextEvade - Time.time) / .9f);
    public float GrabReady => Mathf.Clamp01(1 - (nextGrab - Time.time) / 1.1f);
    float nextEnergyRegen, nextEvade, nextGrab;
    bool backstepRequested, grabRequested;
    public bool Blocking { get; private set; }
    public bool Charging { get; private set; }
    public int AirJumpsRemaining { get; private set; } = 1;
    public float DashReady => Mathf.Clamp01(1 - (nextDash - Time.time) / dashCooldown);
    public float Charge => Charging ? Mathf.Clamp01((Time.time - chargeStart) / .85f) : 0;
    public int ComboCount => Time.time <= comboUntil ? comboCount : 0;
    public bool ActionBusy => action != 0;
    float comboUntil, bufferedUntil;
    int comboCount, bufferedAttack;
    public float Facing => face;
    public Color Element => characterSelected ? characterElement : id == 0 ? new Color(.38f, 1, .68f) : new Color(1, .48f, .18f);
    float nextParry, shieldStarted, shieldBrokenUntil, nextWard, nextDash, dashUntil, chargeStart, doubleJumpUntil;
    bool blockHeld, dashRequested, chargePressed, chargeReleased, chargeHeld;
    Gamepad previousGamepad;
    [Header("Damage and knockout")]
    public float maxHealth = 100;
    public float pushDamage = 12;
    public float punchDamage = 16;
    public float pushKnockback = 4.8f;
    public float punchKnockback = 4;
    public float damageKnockbackMultiplier = .30f;
    public float pushCooldown = .32f;
    public float punchCooldown = .36f;
    public float pushRange = 1.65f;
    public float punchRange = 1.45f;
    public float Health { get; private set; }
    public bool KnockedOut { get; private set; }
    public Rigidbody2D Body { get; private set; }
    public string AnimationState { get; private set; } = "Idle";
    public float PushReady => Mathf.Clamp01(1 - (nextPush - Time.time) / pushCooldown);
    public float PunchReady => Mathf.Clamp01(1 - (nextPunch - Time.time) / punchCooldown);
    public string Status => KnockedOut ? "KNOCKED OUT" : Time.time < shieldBrokenUntil ? "GUARD BROKEN" : Time.time < hurtUntil ? "STAGGERED" : Blocking ? "WARD UP" : UltimateActive ? UltimateName : Charging ? "CHARGING SPECIAL" : Time.time < dashUntil ? "DASH!" : action == 3 ? SkillName : action == 1 ? "KICK!" : action == 2 ? "PUNCH!" : action == 4 ? "GRAB & TOSS!" : "HOLD YOUR GROUND";
    float axis, face, hurtUntil, invulnerableUntil, nextPush, nextPunch, nextTrail, nextTouch, nextFootstep, lastVerticalSpeed;
    bool jumpRequested, pushRequested, punchRequested, grounded;
    int action;
    string actionAnimation = "Idle";
    Vector2 groundNormal = Vector2.up;
    Vector3 baseScale, smoothScale;
    Quaternion baseRotation;
    MaterialPropertyBlock textureBinding;
    readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];

    void Awake()
    {
        Body = GetComponent<Rigidbody2D>();
        baseScale = art.transform.localScale;
        smoothScale = baseScale;
        baseRotation = art.transform.localRotation;
        face = id == 0 ? 1 : -1;
        textureBinding = new MaterialPropertyBlock();
    }
    public void SetCharacter(int index, string name, ScoutAnimations set, RuntimeAnimatorController controller, Color color)
    {
        CharacterIndex = Mathf.Clamp(index, 0, 3);
        characterSelected = true;
        characterElement = color;
        displayName = name;
        if (set != null) animations = set;
        if (controller != null) animator.runtimeAnimatorController = controller;
        if (animations != null && animations.walk != null && animations.walk.Length > 0) art.sprite = animations.walk[0];
        AnimationState = "";
    }
    public void ResetScout()
    {
        CancelUltimate();
        StopAllCoroutines();
        Body.simulated = false;
        Vector2 spawn = spawnPoint.position;
        transform.SetPositionAndRotation(spawn, Quaternion.identity);
        Body.position = spawn;
        Body.linearVelocity = Vector2.zero;
        Body.angularVelocity = 0;
        face = id == 0 ? 1 : -1;
        Health = maxHealth;
        Shield = shieldCapacity; Blocking = Charging = blockHeld = chargeHeld = false;
        nextEnergyRegen = nextEvade = nextGrab = nextSpecial = 0;
        UltimateUsed = ultimateChordHeld = victoryPlaying = false;
        slowedUntil = rootedUntil = 0; groundBody = null;
        nextParry = 0; shieldStarted = -10; shieldBrokenUntil = nextWard = nextDash = dashUntil = doubleJumpUntil = 0;
        AirJumpsRemaining = 1; comboCount = bufferedAttack = 0; comboUntil = bufferedUntil = 0;
        KnockedOut = false;
        hurtUntil = invulnerableUntil = nextPush = nextPunch = 0;
        axis = 0; action = 0;
        grounded = false; lastVerticalSpeed = 0; nextFootstep = 0;
        ClearRequests();
        nextAIThink = aiBlockUntil = aiChargeUntil = 0; aiThreatSince = -1;
        AIDecisions = AIActionRequests = 0;
        previousGamepad = null;
        art.color = Color.white;
        art.flipX = face < 0;
        art.transform.localScale = smoothScale = baseScale;
        art.transform.localRotation = baseRotation;
        AnimationState = "";
        PlayState("Idle");
    }
    void Update()
    {
        var game = WoodlandGame.Instance;
        if (game == null || game.Paused) {
            if (aiControlled) ClearAIInput();
            return;
        }
        if (!game.playing && UltimateActive) { CancelUltimate(); StopAllCoroutines(); action = 0; }
        var keys = Keyboard.current;
        if (aiControlled) {
            if (inputEnabled && game.playing && !KnockedOut) UpdateAI(game);
            else ClearAIInput();
        } else if (inputEnabled && game.playing && !KnockedOut) {
            axis = keys == null ? 0 : id == 0 ? (keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0)
                           : (keys.rightArrowKey.isPressed ? 1 : 0) - (keys.leftArrowKey.isPressed ? 1 : 0);
            bool held = false;
            blockHeld = false;
            if (keys != null) {
                jumpRequested |= id == 0 ? keys.wKey.wasPressedThisFrame : keys.upArrowKey.wasPressedThisFrame;
                pushRequested |= id == 0 ? keys.fKey.wasPressedThisFrame : keys.kKey.wasPressedThisFrame;
                punchRequested |= id == 0 ? keys.gKey.wasPressedThisFrame : keys.lKey.wasPressedThisFrame;
                blockHeld = id == 0 ? keys.sKey.isPressed : keys.downArrowKey.isPressed;
                dashRequested |= id == 0 ? keys.leftShiftKey.wasPressedThisFrame : keys.rightShiftKey.wasPressedThisFrame;
                backstepRequested |= id == 0 ? keys.qKey.wasPressedThisFrame : keys.uKey.wasPressedThisFrame;
                grabRequested |= id == 0 ? keys.eKey.wasPressedThisFrame : keys.oKey.wasPressedThisFrame;
                specialRequested |= id == 0 ? keys.hKey.wasPressedThisFrame : keys.jKey.wasPressedThisFrame;
                ultimateRequested |= id == 0 ? keys.tKey.wasPressedThisFrame : keys.iKey.wasPressedThisFrame;
            }
            var pad = game.GetGamepad(id);
            // Losing a held special cancels its charge rather than firing an unintended smash.
            if (pad != previousGamepad && chargeHeld && !held) { Charging = false; chargeHeld = false; }
            if (pad != previousGamepad) ultimateChordHeld = false;
            previousGamepad = pad;
            if (WoodlandGame.PadMenuChord(pad) && !held) { Charging = false; chargeHeld = false; }
            if (pad != null && !WoodlandGame.PadMenuChord(pad)) {
                float padAxis = pad.dpad.x.ReadValue();
                if (Mathf.Abs(padAxis) < .5f) padAxis = pad.leftStick.x.ReadValue();
                if (Mathf.Abs(padAxis) > .15f) axis = padAxis;
                bool ultimateChord = pad.leftTrigger.isPressed && pad.rightTrigger.isPressed;
                if (ultimateChord) {
                    ultimateRequested |= !ultimateChordHeld;
                    blockHeld = false;
                    // The ultimate chord has priority over every ordinary button action.
                    jumpRequested = pushRequested = punchRequested = backstepRequested = dashRequested = specialRequested = false;
                } else {
                    jumpRequested |= pad.buttonSouth.wasPressedThisFrame;
                    punchRequested |= pad.buttonWest.wasPressedThisFrame;
                    pushRequested |= pad.buttonEast.wasPressedThisFrame;
                    blockHeld |= pad.leftTrigger.isPressed;
                    backstepRequested |= pad.leftShoulder.wasPressedThisFrame;
                    dashRequested |= pad.rightShoulder.wasPressedThisFrame;
                    specialRequested |= pad.buttonNorth.wasPressedThisFrame;
                }
                ultimateChordHeld = ultimateChord;
            }
            chargePressed |= held && !chargeHeld;
            chargeReleased |= !held && chargeHeld;
            chargeHeld = held;
            if (pad == null) ultimateChordHeld = false;
            else if (WoodlandGame.PadMenuChord(pad)) ultimateChordHeld = pad.leftTrigger.isPressed && pad.rightTrigger.isPressed;
        } else { axis = 0; blockHeld = chargeHeld = false; }
        if (game.playing && transform.position.y < game.fallBoundary) KnockOut();
        SetBlock(blockHeld && game.playing);
        if (Blocking) {
            Shield = Mathf.Max(0, Shield - shieldDrain * Time.deltaTime);
            if (Shield <= 0) BreakGuard();
            else if (Time.time >= nextWard) { nextWard = Time.time + .07f; WoodlandEffects.Instance?.Ward(Body.position, face, Element, Shield / shieldCapacity); }
        } else if (Time.time >= shieldBrokenUntil && Time.time >= nextEnergyRegen) Shield = Mathf.Min(shieldCapacity, Shield + shieldRecovery * Time.deltaTime);
        if (Charging && Time.time >= nextWard) { nextWard = Time.time + .08f; WoodlandEffects.Instance?.Windup(Body.position + Vector2.right * face * .45f, face, Element); }
        if (Charging && Charge >= 1) ReleaseSmash();
        string state = victoryPlaying ? "Victory" : KnockedOut ? "Knockout" : Time.time < hurtUntil ? "Hurt" : Blocking ? "Block" : Charging ? "Charge" : Time.time < dashUntil ? (AnimationState == "Backstep" ? "Backstep" : "Dash") : action != 0 ? actionAnimation : Time.time < doubleJumpUntil ? "DoubleJump" : !grounded && game.playing ? "Jump" : Mathf.Abs(axis) > .1f ? "Walk" : "Idle";
        PlayState(state);
        art.flipX = face < 0;
        art.color = Time.time < hurtUntil && !KnockedOut ? new Color(1, .72f, .62f) : Color.white;
        float lean = KnockedOut ? 0 : action > 0 ? -face * 5 : -axis * 3;
        art.transform.localRotation = Quaternion.Slerp(art.transform.localRotation, baseRotation * Quaternion.Euler(0, 0, lean), 1 - Mathf.Exp(-22 * Time.deltaTime));
        Vector3 stretch = KnockedOut ? Vector3.one : Time.time < hurtUntil ? new Vector3(1.05f, .95f, 1) : !grounded && game.playing ? new Vector3(.97f, 1.03f, 1) : Vector3.one;
        smoothScale = Vector3.Lerp(smoothScale, Vector3.Scale(baseScale, stretch), 1 - Mathf.Exp(-18 * Time.deltaTime));
        art.transform.localScale = smoothScale;
        if (Body.simulated && Time.time >= nextTrail && (Body.linearVelocity.sqrMagnitude > 10 || action > 0 || Time.time < hurtUntil)) {
            nextTrail = Time.time + .05f;
            WoodlandEffects.Instance?.Ghost(art, Color.Lerp(Element, Color.white, .15f));
        }
    }
    void ClearAIInput()
    {
        axis = 0;
        blockHeld = chargeHeld = Blocking = Charging = false;
        aiBlockUntil = aiChargeUntil = 0;
        aiThreatSince = -1;
        ClearRequests();
    }
    void UpdateAI(WoodlandGame game)
    {
        // The CPU writes exactly the same requests as a keyboard. It never moves a body directly.
        if (game.plank == null || game.players == null || game.players.Length < 2) { ClearAIInput(); return; }
        Scout rival = game.players[1 - id];
        if (rival == null || rival.KnockedOut || !Body.simulated || Time.time < hurtUntil) { ClearAIInput(); return; }
        blockHeld = Time.time < aiBlockUntil && Shield > 25;
        if (chargeHeld && Time.time >= aiChargeUntil) {
            chargeHeld = false;
            chargeReleased |= Charging;
        }
        Vector2 delta = rival.Body.position - Body.position;
        bool threatening = rival.Charging || rival.action != 0;
        if (threatening && Mathf.Abs(delta.x) < 2.3f && Mathf.Abs(delta.y) < 1.3f) {
            if (aiThreatSince < 0) aiThreatSince = Time.time;
        } else aiThreatSince = -1;
        if (Time.time < nextAIThink) return;
        nextAIThink = Time.time + Random.Range(.18f, .32f);
        AIDecisions++;
        if (aiPlankCollider == null || aiPlankCollider.attachedRigidbody != game.plank)
            aiPlankCollider = game.plank.GetComponent<BoxCollider2D>();
        if (aiPlankCollider == null) { ClearAIInput(); return; }
        Transform surface = game.plank.transform;
        Vector2 local = surface.InverseTransformPoint(Body.position);
        Vector2 rivalLocal = surface.InverseTransformPoint(rival.Body.position);
        float center = aiPlankCollider.offset.x;
        float safeHalf = Mathf.Max(.5f, aiPlankCollider.size.x * .5f - .95f);
        float top = aiPlankCollider.offset.y + aiPlankCollider.size.y * .5f;
        Vector2 relativeVelocity = Body.linearVelocity - game.plank.GetPointVelocity(Body.position);
        float alongSpeed = Vector2.Dot(relativeVelocity, surface.right) / Mathf.Max(.01f, Mathf.Abs(surface.lossyScale.x));
        bool nearEdge = Mathf.Abs(local.x - center) > safeHalf;
        bool driftingOff = Mathf.Abs(local.x - center + alongSpeed * .28f) > safeHalf;
        bool recovering = !grounded && (local.y < top + .1f || Mathf.Abs(local.x - center) > safeHalf + .4f);
        float target = Mathf.Clamp(rivalLocal.x - Mathf.Sign(rivalLocal.x - local.x) * 1.1f, center - safeHalf, center + safeHalf);
        if (nearEdge || driftingOff || recovering) target = center;
        float distance = target - local.x;
        axis = Mathf.Abs(distance) > .22f ? Mathf.Sign(distance) : 0;
        if (!grounded && recovering) {
            // Air controls act on world X, while ground controls follow the rotated plank tangent.
            float centerX = surface.TransformPoint(new Vector3(center, top, 0)).x;
            axis = Mathf.Abs(centerX - Body.position.x) > .2f ? Mathf.Sign(centerX - Body.position.x) : 0;
        }
        if (action == 0 && !Charging && Time.time >= dashUntil) {
            if (axis != 0) face = Mathf.Sign(axis);
            else if (Mathf.Abs(delta.x) > .1f) face = Mathf.Sign(delta.x);
        }
        if (recovering || nearEdge || driftingOff) {
            aiBlockUntil = 0; blockHeld = false;
            if (Charging) { Charging = false; chargeHeld = false; aiChargeUntil = 0; }
            if ((grounded && nearEdge) || (!grounded && Body.linearVelocity.y < .6f && AirJumpsRemaining > 0)) {
                jumpRequested = true; AIActionRequests++;
            }
            // A recovery dash points inward and only fires while it can still reach the surface.
            if (!grounded && recovering && axis != 0 && Mathf.Abs(local.x - center) > safeHalf && local.y > top - 1.5f && Time.time >= nextDash) {
                face = Mathf.Sign(axis); dashRequested = true; AIActionRequests++;
            }
            return;
        }
        if (Charging || chargeHeld || blockHeld || Time.time < shieldBrokenUntil) return;
        if (Shield < 14) {
            float retreat = Mathf.Clamp(local.x - Mathf.Sign(delta.x) * 1.4f, center - safeHalf, center + safeHalf);
            axis = Mathf.Abs(retreat - local.x) > .2f ? Mathf.Sign(retreat - local.x) : 0;
            return;
        }
        // Reaction delay is longer than the parry window: no instant input-reading parries.
        if (aiThreatSince >= 0 && Time.time - aiThreatSince >= .2f && grounded && Shield > 35 && Random.value < .65f) {
            axis = 0; face = Mathf.Sign(delta.x);
            float escape = local.x - face * 1.2f;
            if (rival.Charging && Shield >= 12 && Time.time >= nextEvade && Mathf.Abs(escape - center) < safeHalf && Random.value < .4f) {
                backstepRequested = true; AIActionRequests++; return;
            }
            blockHeld = true; aiBlockUntil = Time.time + Random.Range(.3f, .5f); AIActionRequests++;
            return;
        }
        if (!game.BalanceMode && delta.y > .8f && (grounded || (AirJumpsRemaining > 0 && Body.linearVelocity.y < .8f))) {
            jumpRequested = true; AIActionRequests++; return;
        }
        if (UltimateReady && action == 0 && Mathf.Abs(delta.x) < 3 && Mathf.Abs(delta.y) < 1.2f && Random.value < .45f) {
            axis = 0; face = Mathf.Sign(delta.x); ultimateRequested = true; AIActionRequests++; return;
        }
        if (action == 0 && SpecialCooldownRemaining == 0 && Mathf.Abs(delta.x) > 1.8f && Mathf.Abs(delta.x) < 6.5f && Mathf.Abs(delta.y) < 1 && Random.value < .35f) {
            axis = 0; face = Mathf.Sign(delta.x); specialRequested = true; AIActionRequests++; return;
        }
        bool inRange = Mathf.Abs(delta.x) < 1.35f && Mathf.Abs(delta.y) < 1.05f;
        if (inRange) {
            axis = 0;
            if (action == 0 && Mathf.Abs(delta.x) > .1f) face = Mathf.Sign(delta.x);
            if (rival.Blocking && Mathf.Abs(delta.x) < 1.1f && action == 0 && Time.time >= nextGrab && Shield >= 18) {
                grabRequested = true; AIActionRequests++; return;
            }
            if (action == 0 && SpecialCooldownRemaining == 0 && ComboCount == 0 && Random.value < .12f) {
                specialRequested = true;
            } else if (ComboCount > 0 || Random.value < .55f) punchRequested = true;
            else pushRequested = true;
            AIActionRequests++;
        } else if (grounded && Mathf.Abs(delta.x) > 2 && Mathf.Abs(delta.x) < 4 && Random.value < .09f) {
            jumpRequested = true; AIActionRequests++;
        } else if (grounded && Mathf.Abs(delta.x) > 3 && !driftingOff && Random.value < .07f && Time.time >= nextDash) {
            // Leave a full dash length before an edge instead of dashing off the plank.
            float projected = local.x + axis * dashSpeed * .16f;
            if (Mathf.Abs(projected - center) < safeHalf) { dashRequested = true; AIActionRequests++; }
        }
    }
    void LateUpdate()
    {
        if (art.sprite == null) return;
        art.GetPropertyBlock(textureBinding);
        textureBinding.SetTexture("_MainTex", art.sprite.texture);
        art.SetPropertyBlock(textureBinding);
    }
    void PlayState(string state)
    {
        if (AnimationState == state) return;
        AnimationState = state;
        animator.Play(state, 0, 0);
    }
    void FixedUpdate()
    {
        var game = WoodlandGame.Instance;
        bool wasGrounded = grounded;
        grounded = false;
        int count = Physics2D.Raycast(Body.position + Vector2.down * .49f, Vector2.down, new ContactFilter2D { useTriggers = false }, groundHits, .25f);
        for (int i = 0; i < count; i++) {
            var hit = groundHits[i];
            // Only arena surface tops grant traction. Their vertical edges and the rival never do.
            if (Body.linearVelocity.y <= .5f && game != null && game.IsGroundBody(hit.rigidbody) && hit.normal.y > .65f && Vector2.Dot(hit.normal, hit.rigidbody.transform.up) > .9f) {
                grounded = true; groundNormal = hit.normal; groundBody = hit.rigidbody; break;
            }
        }
        if (grounded && Body.linearVelocity.y <= .5f) { AirJumpsRemaining = 1; doubleJumpUntil = 0; }
        if (Body.simulated && grounded && !wasGrounded && lastVerticalSpeed < -2)
            WoodlandEffects.Instance?.Land(Body.position + Vector2.down * .625f, -lastVerticalSpeed);
        lastVerticalSpeed = Body.linearVelocity.y;
        if (game == null || !game.playing || KnockedOut) { ClearRequests(); return; }
        if (Time.time >= hurtUntil) {
            if (axis != 0 && action == 0) face = Mathf.Sign(axis);
            float movement = Blocking ? axis * .2f : Charging || action == 3 || action == 5 ? 0 : action == 1 || action == 2 ? axis * .55f : axis;
            movement *= Time.time < rootedUntil ? 0 : Time.time < slowedUntil ? .45f : 1;
            if (Time.time < dashUntil) { ClearRequests(); return; }
            if (grounded) {
                Vector2 tangent = new Vector2(groundNormal.y, -groundNormal.x);
                Vector2 platformVelocity = (groundBody != null ? groundBody : game.plank).GetPointVelocity(Body.position);
                float speed = Vector2.Dot(Body.linearVelocity - platformVelocity, tangent);
                float control = Mathf.Clamp((movement * moveSpeed - speed) * 10, -acceleration, acceleration);
                float slopeGravity = Vector2.Dot(Physics2D.gravity * Body.gravityScale, tangent);
                Body.AddForce(tangent * (control - slopeGravity) * Body.mass);
            } else if (Mathf.Abs(axis) > .1f) {
                // Air steering never cancels gravity or applies wall friction.
                Body.AddForce(Vector2.right * Mathf.Clamp((movement * moveSpeed - Body.linearVelocity.x) * 3, -airAcceleration, airAcceleration) * Body.mass);
            }
            if (grounded && Mathf.Abs(axis) > .1f && Time.time >= nextFootstep) {
                nextFootstep = Time.time + (WoodlandEffects.Instance ? WoodlandEffects.Instance.footstepInterval : .16f);
                WoodlandEffects.Instance?.Footstep(Body.position + Vector2.down * .625f, axis);
            }
            if (ultimateRequested) { TryUltimate(); ClearRequests(); return; }
            if (jumpRequested) TryJump();
            if (backstepRequested) TryBackstep();
            if (dashRequested) TryDash();
            if (grabRequested) TryGrab();
            if (specialRequested) TrySpecial();
            if (chargePressed) BeginSmash();
            if (chargeReleased) ReleaseSmash();
            if (punchRequested) TryAttack(true);
            else if (pushRequested) TryAttack(false);
        }
        ClearRequests();
    }
    void ClearRequests() { jumpRequested = pushRequested = punchRequested = dashRequested = backstepRequested = grabRequested = specialRequested = ultimateRequested = chargePressed = chargeReleased = false; }
    bool CanAct => WoodlandGame.Instance != null && WoodlandGame.Instance.playing && !WoodlandGame.Instance.Paused && !KnockedOut && Time.time >= hurtUntil && Time.time >= shieldBrokenUntil;
    bool SpendStamina(float cost)
    {
        if (Shield < cost) return false;
        Shield = Mathf.Clamp(Shield - cost, 0, shieldCapacity);
        nextEnergyRegen = Time.time + .65f;
        return true;
    }
    public bool TryJump()
    {
        if (!CanAct || Time.time < rootedUntil || Blocking || Charging || Time.time < dashUntil || action != 0 || (!grounded && AirJumpsRemaining == 0)) return false;
        bool aerial = !grounded;
        if (!SpendStamina(aerial ? 6 : 4)) return false;
        if (aerial) AirJumpsRemaining--;
        else { AirJumpsRemaining = 1; grounded = false; }
        Body.linearVelocity = new Vector2(Body.linearVelocity.x, jumpSpeed * (aerial ? .92f : 1));
        if (aerial) { doubleJumpUntil = Time.time + .48f; PlayState("DoubleJump"); WoodlandEffects.Instance?.AirJump(Body.position + Vector2.down * .6f, Element, id); }
        else WoodlandEffects.Instance?.Jump(Body.position + Vector2.down * .625f, Element);
        return true;
    }
    public bool TryDash()
    {
        if (!CanAct || Time.time < rootedUntil || Blocking || Charging || action != 0 || Time.time < dashUntil || Time.time < nextDash) return false;
        if (!SpendStamina(10)) return false;
        nextDash = Time.time + dashCooldown; dashUntil = Time.time + .16f;
        Body.linearVelocity = new Vector2(face * dashSpeed, Mathf.Max(-2, Body.linearVelocity.y));
        PlayState("Dash"); WoodlandEffects.Instance?.Dash(Body.position, face, Element);
        return true;
    }
    public bool TryBackstep()
    {
        if (!CanAct || Time.time < rootedUntil || Blocking || Charging || action != 0 || Time.time < dashUntil || Time.time < nextEvade) return false;
        if (!SpendStamina(12)) return false;
        nextEvade = Time.time + .9f; dashUntil = Time.time + .14f;
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + .16f);
        Body.linearVelocity = new Vector2(-face * 7, Mathf.Max(-2, Body.linearVelocity.y));
        PlayState("Backstep");
        WoodlandEffects.Instance?.Dash(Body.position, -face, Color.Lerp(Element, Color.white, .5f));
        return true;
    }
    public bool TryGrab()
    {
        if (!CanAct || Blocking || Charging || action != 0 || Time.time < dashUntil || Time.time < nextGrab) return false;
        if (!SpendStamina(18)) return false;
        nextGrab = Time.time + 1.1f;
        StartCoroutine(Grab());
        return true;
    }
    IEnumerator Grab()
    {
        action = 4; actionAnimation = "Grab"; PlayState(actionAnimation);
        float direction = face;
        WoodlandEffects.Instance?.Windup(Body.position + Vector2.right * direction * .5f, direction, Element);
        yield return new WaitForSeconds(.22f);
        var game = WoodlandGame.Instance;
        Scout other = game.players[1 - id];
        Vector2 delta = other.Body.position - Body.position;
        if (game.playing && !KnockedOut && Time.time >= hurtUntil && delta.x * direction > 0 && Mathf.Abs(delta.x) < 1.15f && Mathf.Abs(delta.y) < 1.1f) {
            bool guarded = other.Blocking;
            other.Blocking = false;
            if (other.ReceiveHit(this, direction, false, .5f)) {
                other.Body.AddForce(new Vector2(direction * 3, 2), ForceMode2D.Impulse);
                WoodlandEffects.Instance?.ElementHit(other.Body.position, direction, Element, 1, id);
                WoodlandEffects.Instance?.Impact(other.Body.position, direction, false, false);
            } else other.Blocking = guarded;
        }
        yield return new WaitForSeconds(.32f);
        action = 0;
    }
    public bool SetBlock(bool held)
    {
        bool allowed = held && CanAct && action == 0 && !Charging && Time.time >= dashUntil && Time.time >= shieldBrokenUntil && Shield > 0 && (Blocking || Shield >= 20);
        if (allowed && !Blocking) { shieldStarted = Time.time >= nextParry ? Time.time : -10; nextParry = Time.time + .45f; }
        Blocking = allowed;
        return Blocking;
    }
    void BreakGuard()
    {
        Blocking = false; Shield = 0; shieldBrokenUntil = Time.time + 1.25f; hurtUntil = shieldBrokenUntil;
        WoodlandEffects.Instance?.GuardImpact(Body.position, face, Element, false, true);
    }
    // Compatibility for existing callers; ordinary special input uses TrySpecial directly.
    public bool BeginSmash()
    {
        if (!CanAct || Blocking || Charging || action != 0 || Time.time < dashUntil || Time.time < nextSpecial || Shield < 14) return false;
        Charging = true; chargeStart = Time.time; PlayState("Charge");
        return true;
    }
    public bool ReleaseSmash()
    {
        if (!Charging) return false;
        Charging = false;
        return TrySpecial();
    }
    public bool TrySpecial()
    {
        if (!CanAct || Blocking || Charging || action != 0 || Time.time < dashUntil || Time.time < nextSpecial) return false;
        if (!SpendStamina(14)) return false;
        nextSpecial = Time.time + SpecialCooldown;
        StartCoroutine(RangedCast());
        return true;
    }
    IEnumerator RangedCast()
    {
        action = 3; actionAnimation = "Smash"; PlayState(actionAnimation);
        float direction = face;
        WoodlandEffects.Instance?.Windup(Body.position + Vector2.right * direction * .45f, direction, Element);
        yield return new WaitForSeconds(.18f);
        if (CanAct) StartCoroutine(SpecialProjectile(Body.position, direction, SkillIndex));
        yield return new WaitForSeconds(.2f);
        action = 0;
    }
    IEnumerator SpecialProjectile(Vector2 origin, float direction, int skill)
    {
        // A fixed path, visible travel and one contact let the rival jump, dodge, guard or parry it.
        var game = WoodlandGame.Instance;
        Scout other = game.players[1 - id];
        for (int pulse = 0; pulse < 12; pulse++) {
            if (!game.playing || KnockedOut) yield break;
            Vector2 point = origin + Vector2.right * direction * (.6f + pulse * .6f);
            Vector2 spray = skill == 0 ? Vector2.up : skill == 3 ? new Vector2(direction, .6f) : Vector2.right * direction;
            WoodlandEffects.Instance?.Burst(point, spray, skill == 1 ? 10 : 6, Element, skill == 2 ? .8f : 1.4f);
            float forward = (other.Body.position.x - origin.x) * direction;
            if (forward > 0 && forward <= 7.5f && Mathf.Abs(other.Body.position.x - point.x) < .65f && Mathf.Abs(other.Body.position.y - point.y) < 1.1f) {
                float power = skill == 0 ? .95f : skill == 1 ? 1.05f : skill == 2 ? .85f : 1.1f;
                SpecialHit(other, direction, power, skill == 0 ? .65f : skill == 2 ? .8f : 0, skill == 0 ? .3f : 0, skill == 3 ? 2.5f : 0);
                yield break;
            }
            yield return new WaitForSeconds(.055f);
        }
    }
    bool SpecialHit(Scout other, float direction, float power, float slow = 0, float root = 0, float lift = 0)
    {
        var game = WoodlandGame.Instance;
        if (game == null || !game.playing || KnockedOut || !other.ReceiveHit(this, direction, true, power)) return false;
        other.slowedUntil = Mathf.Max(other.slowedUntil, Time.time + slow);
        other.rootedUntil = Mathf.Max(other.rootedUntil, Time.time + root);
        if (lift > 0) other.Body.AddForce(Vector2.up * lift, ForceMode2D.Impulse);
        WoodlandEffects.Instance?.HeavyImpact(other.Body.position, direction, Element, Mathf.Clamp01(power - 1));
        return true;
    }
    public bool TryUltimate()
    {
        if (!CanAct || !UltimateReady || Charging || action != 0 || Time.time < dashUntil) return false;
        Blocking = blockHeld = false;
        UltimateUsed = true;
        UltimateActive = true;
        axis = 0;
        UltimateCinematic?.Invoke(this, true);
        StartCoroutine(Ultimate());
        return true;
    }
    void CancelUltimate()
    {
        if (!UltimateActive) return;
        UltimateActive = false;
        UltimateCinematic?.Invoke(this, false);
    }
    void OnDisable() { CancelUltimate(); }
    bool UltimateHit(Scout other, float direction, float damage, float slow = 0, float root = 0, float lift = 0)
    {
        if (!CanAct || !other.ReceiveHit(this, direction, true, Mathf.Min(35, damage) / Mathf.Max(.01f, punchDamage))) return false;
        other.slowedUntil = Mathf.Max(other.slowedUntil, Time.time + slow);
        other.rootedUntil = Mathf.Max(other.rootedUntil, Time.time + root);
        if (lift > 0) other.Body.AddForce(Vector2.up * lift, ForceMode2D.Impulse);
        UltimateImpact?.Invoke(this, other);
        WoodlandEffects.Instance?.HeavyImpact(other.Body.position, direction, Element, 1);
        return true;
    }
    IEnumerator Ultimate()
    {
        action = 5; actionAnimation = "Ultimate"; PlayState(actionAnimation);
        float direction = face;
        int skill = SkillIndex;
        // The long tell gives the rival control and a chance to interrupt or escape.
        for (int i = 0; i < 5; i++) {
            WoodlandEffects.Instance?.Windup(Body.position + Vector2.up * .25f, direction, Element);
            yield return new WaitForSeconds(.11f);
        }
        var game = WoodlandGame.Instance;
        var other = game.players[1 - id];
        if (skill == 1) {
            Body.AddForce(Vector2.right * direction * 2.5f, ForceMode2D.Impulse);
            for (int i = 0; i < 3; i++) {
                Vector2 delta = other.Body.position - Body.position;
                WoodlandEffects.Instance?.Burst(Body.position + Vector2.right * direction, Vector2.up, 16, Element, 3);
                if (delta.x * direction > 0 && Mathf.Abs(delta.x) < 3 && Mathf.Abs(delta.y) < 1.5f) UltimateHit(other, direction, 10);
                if (i < 2) yield return new WaitForSeconds(.25f);
            }
        } else if (skill == 2) {
            Vector2 origin = Body.position;
            for (int i = 0; i < 9; i++) {
                Vector2 point = origin + Vector2.right * direction * (.6f + i * .6f);
                WoodlandEffects.Instance?.Burst(point, Vector2.up, 12, Element, 2);
                if ((other.Body.position.x - origin.x) * direction > 0 && Mathf.Abs(other.Body.position.x - point.x) < .7f && Mathf.Abs(other.Body.position.y - point.y) < 1.3f) {
                    UltimateHit(other, direction, 26, 1.2f, .55f); break;
                }
                yield return new WaitForSeconds(.055f);
            }
        } else {
            Vector2 delta = other.Body.position - Body.position;
            bool contact = skill == 3 ? delta.sqrMagnitude < 3.2f * 3.2f && grounded : delta.x * direction > 0 && Mathf.Abs(delta.x) < 3.2f && Mathf.Abs(delta.y) < 1.4f;
            WoodlandEffects.Instance?.Burst(Body.position, Vector2.up, 26, Element, 3.5f);
            if (contact) UltimateHit(other, skill == 3 && Mathf.Abs(delta.x) > .01f ? Mathf.Sign(delta.x) : direction, skill == 3 ? 35 : 30, skill == 0 ? 1.1f : 0, skill == 0 ? .8f : 0, skill == 3 ? 4 : 0);
        }
        yield return new WaitForSeconds(.3f);
        action = 0;
        CancelUltimate();
    }
    public void PlayVictory()
    {
        CancelUltimate(); StopAllCoroutines(); ClearRequests();
        victoryPlaying = true;
        axis = 0; action = 0;
        Blocking = Charging = blockHeld = chargeHeld = false;
        PlayState("Victory");
    }
    public bool TryAttack(bool punch)
    {
        if (!CanAct || Blocking || Charging || Time.time < dashUntil) return false;
        if (action != 0) {
            if (action == 1 || action == 2) { bufferedAttack = punch ? 2 : 1; bufferedUntil = Time.time + .4f; }
            return false;
        }
        bool chain = ComboCount > 0 && comboCount < 3;
        if (!chain && Time.time < (punch ? nextPunch : nextPush)) return false;
        if (!SpendStamina(punch ? 8 : 6)) return false;
        if (!chain) comboCount = 0;
        if (punch) nextPunch = Time.time + punchCooldown;
        else nextPush = Time.time + pushCooldown;
        StartCoroutine(Attack(punch));
        return true;
    }
    IEnumerator Attack(bool punch)
    {
        action = punch ? 2 : 1;
        float direction = face;
        actionAnimation = comboCount == 1 ? "Combo2" : comboCount == 2 ? "Combo3" : punch ? "Punch" : "Kick";
        PlayState(actionAnimation);
        WoodlandEffects.Instance?.Windup(Body.position + new Vector2(direction * .55f, 0), direction, Element);
        // Grounded startup: punch .10 s / kick .12 s. Air startup: .18 s.
        bool groundedAttack = grounded;
        yield return new WaitForSeconds(groundedAttack ? (punch ? .10f : .12f) : .18f);
        WoodlandEffects.Instance?.Swipe(Body.position, direction, punch,id);
        var game = WoodlandGame.Instance;
        Scout other = game.players[1 - id];
        Vector2 delta = other.Body.position - Body.position;
        if (game.playing && !KnockedOut && Time.time >= hurtUntil && delta.x * direction > 0 && Mathf.Abs(delta.x) < (punch ? punchRange : pushRange) && Mathf.Abs(delta.y) < 1.1f) {
            bool finisher = comboCount == 2;
            if (other.ReceiveHit(this, direction, punch, finisher ? 1.4f : 1)) {
                comboCount++; comboUntil = Time.time + .8f;
                WoodlandEffects.Instance?.ElementHit(other.Body.position, direction, Element, comboCount, id, SkillIndex);
                WoodlandEffects.Instance?.Impact(other.Body.position, direction, punch, finisher);
            } else { comboCount = 0; comboUntil = 0; }
        } else { comboCount = 0; comboUntil = 0; }
        // Recovery: .18 s grounded / .24 s air. Hit-to-next-hit clears .22 s invulnerability.
        yield return new WaitForSeconds(groundedAttack ? .18f : .24f);
        action = 0;
        if (bufferedAttack != 0 && Time.time <= bufferedUntil) {
            bool followup = bufferedAttack == 2; bufferedAttack = 0; TryAttack(followup);
        } else bufferedAttack = 0;
    }
    public bool ReceiveHit(Scout attacker, float direction, bool punch, float power = 1)
    {
        if (KnockedOut || Time.time < invulnerableUntil) return false;
        float damage = (punch ? attacker.punchDamage : attacker.pushDamage) * power;
        bool frontal = (attacker.Body.position.x - Body.position.x) * face > 0;
        if (Blocking && frontal) {
            bool parry = Time.time - shieldStarted <= parryWindow;
            WoodlandEffects.Instance?.GuardImpact(Body.position, face, Element, parry, false);
            if (parry) { attacker.Stagger(.4f); Shield = Mathf.Min(shieldCapacity, Shield + 8); }
            else { Shield = Mathf.Max(0, Shield - damage * 2.2f); Body.AddForce(Vector2.right * direction * .7f, ForceMode2D.Impulse); if (Shield <= 0) BreakGuard(); }
            return false;
        }
        Blocking = Charging = false; comboCount = bufferedAttack = 0; comboUntil = 0;
        Health = Mathf.Max(0, Health - damage);
        float buildup = 1 + (1 - Health / maxHealth) * damageKnockbackMultiplier;
        float force = (punch ? attacker.punchKnockback : attacker.pushKnockback) * buildup * Mathf.Lerp(1, 1.65f, Mathf.Clamp01(power - 1));
        bool groundedCombo = attacker.grounded && grounded && (attacker.action == 1 || attacker.action == 2);
        bool finisher = groundedCombo && attacker.comboCount == 2;
        // Early grounded hits preserve close spacing so a buffered string can finish without auto-tracking.
        if (groundedCombo && !finisher) force *= .22f;
        Body.linearVelocity = new Vector2(0, Mathf.Max(0, Body.linearVelocity.y));
        Body.AddForce(new Vector2(direction * force, groundedCombo && !finisher ? .35f : punch ? 1.9f : 1.1f), ForceMode2D.Impulse);
        hurtUntil = Mathf.Max(hurtUntil, Time.time + (groundedCombo ? .16f : punch ? .28f : .24f));
        invulnerableUntil = Time.time + .22f;
        action = 0;
        CancelUltimate();
        StopAllCoroutines();
        PlayState("Hurt");
        if (Health <= 0) KnockOut();
        return true;
    }
    public void Stagger(float duration)
    {
        CancelUltimate(); StopAllCoroutines(); action = 0; Blocking = Charging = false; comboCount = bufferedAttack = 0; comboUntil = 0;
        hurtUntil = Time.time + duration; PlayState("Hurt");
    }
    public void KnockOut()
    {
        if (KnockedOut) return;
        CancelUltimate();
        KnockedOut = true; Blocking = Charging = false; comboCount = bufferedAttack = 0; comboUntil = 0;
        Health = 0;
        action = 0;
        axis = 0;
        StopAllCoroutines();
        PlayState("Knockout");
        WoodlandEffects.Instance?.Knockout(Body.position, Element);
        WoodlandGame.Instance.RegisterKnockout(this);
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (Time.time < nextTouch || WoodlandEffects.Instance == null) return;
        nextTouch = Time.time + .18f;
        bool rival = collision.gameObject.GetComponent<Scout>() != null;
        if (collision.contactCount > 0 && (rival || collision.relativeVelocity.magnitude > 2))
            WoodlandEffects.Instance.Burst(collision.GetContact(0).point, Vector2.up, rival ? 10 : 6, new Color(.6f, .78f, .34f), rival ? 2 : 1.3f);
    }
}
