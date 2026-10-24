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
    public string SkillName => new[]{"VINE BOLT","FIRE BOLT","FROST WAVE","STONE WAVE","GALE CRESCENT","ARC LANCE","DUSK ORB","PRISM SHARD"}[Mathf.Clamp(SkillIndex,0,7)];
    public string UltimateName => new[]{"WILDROOT PRISON","PHOENIX COMBO","MOONFROST SEAL","WORLD SHATTER","TEMPEST SPIRAL","THUNDER RUSH","ECLIPSE SNARE","CRYSTAL CROWN"}[Mathf.Clamp(SkillIndex,0,7)];
    public const float SpecialCooldown = 0;
    public float SpecialCooldownRemaining => 0;
    public float SpecialReady => Charging ? Charge : 1;
    public const int HitsToKnockdown = 5;
    public int HitsRemaining { get; private set; } = HitsToKnockdown;
    public bool Downed { get; private set; }
    public bool RingOutFalling { get; private set; }
    float ringOutStarted;
    int standingArtOrder;
    public float knockdownDuration = 3;
    public float KnockdownRemaining => Downed?Mathf.Max(0,knockedDownUntil-Time.time):0;
    bool recovering;
    float knockedDownUntil;
    CapsuleCollider2D bodyCollider;
    Vector2 standingSize, standingOffset;
    CapsuleDirection2D standingDirection;
    public bool UltimateUsed { get; private set; }
    public bool UltimateActive { get; private set; }
    public bool UltimateReady => !UltimateUsed && !KnockedOut && !Downed && HitsRemaining == 1;
    public static event System.Action<Scout, bool> UltimateCinematic;
    public static event System.Action<Scout, Scout> UltimateImpact;
    bool pointerGuard, padUpHeld;
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
    public bool ActionBusy => action != 0 || Downed;
    public bool AttackRecovery { get; private set; }
    public bool AttackStartup => (action==1 || action==2) && !AttackRecovery;
    public string CombatCue => Time.time<cueUntil ? combatCue : "";
    string combatCue="";
    float cueUntil, blockStunUntil, aiPressureUntil;
    int lastConfirmedAttack;
    float comboUntil, bufferedUntil;
    public int AirHitsTaken { get; private set; }
    int comboCount, bufferedAttack;
    public float Facing => face;
    public Color Element => characterSelected ? characterElement : id == 0 ? new Color(.38f, 1, .68f) : new Color(1, .48f, .18f);
    float nextParry, shieldStarted, shieldBrokenUntil, nextWard, nextDash, dashUntil, chargeStart, doubleJumpUntil;
    bool blockHeld, dashRequested, chargePressed, chargeReleased, chargeHeld;
    Gamepad previousGamepad;
    [Header("Damage and knockout")]
    public const float DamageScale=.85f;
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
    public string Status => KnockedOut ? "RING OUT" : Downed ? "KNOCKED DOWN" : Time.time < shieldBrokenUntil ? "GUARD BROKEN" : Time.time < hurtUntil ? "STAGGERED" : Blocking ? "WARD UP" : UltimateActive ? UltimateName : Charging ? "CHARGING SPECIAL" : Time.time < dashUntil ? "DASH!" : action == 3 ? SkillName : action == 1 ? "KICK!" : action == 2 ? "PUNCH!" : action == 4 ? "GRAB & TOSS!" : "HOLD YOUR GROUND";
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
        bodyCollider=GetComponent<CapsuleCollider2D>();standingSize=bodyCollider.size;
        standingOffset=bodyCollider.offset;standingDirection=bodyCollider.direction;
        baseScale = art.transform.localScale;
        smoothScale = baseScale;
        baseRotation = Quaternion.identity;standingArtOrder=art.sortingOrder;
        face = id == 0 ? 1 : -1;
        textureBinding = new MaterialPropertyBlock();
    }
    public void SetCharacter(int index, string name, ScoutAnimations set, RuntimeAnimatorController controller, Color color)
    {
        CharacterIndex = Mathf.Clamp(index, 0, WoodlandGame.Instance?WoodlandGame.Instance.CharacterNames.Length-1:7);
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
        RingOutFalling=false;bodyCollider.enabled=true;art.sortingOrder=standingArtOrder;
        AttackRecovery=false;combatCue="";cueUntil=blockStunUntil=aiPressureUntil=0;lastConfirmedAttack=0;
        Downed=recovering=false;knockedDownUntil=0;HitsRemaining=HitsToKnockdown;RestoreStandingCollider();
        Health = maxHealth;
        Shield = shieldCapacity; Blocking = Charging = blockHeld = chargeHeld = false;
        nextEnergyRegen = nextEvade = nextGrab = 0;
        pointerGuard = padUpHeld = false;
        UltimateUsed = ultimateChordHeld = victoryPlaying = false;
        slowedUntil = rootedUntil = 0; groundBody = null;
        nextParry = 0; shieldStarted = -10; shieldBrokenUntil = nextWard = nextDash = dashUntil = doubleJumpUntil = 0;
        AirHitsTaken = 0; AirJumpsRemaining = 1; comboCount = bufferedAttack = 0; comboUntil = bufferedUntil = 0;
        KnockedOut = false;
        hurtUntil = invulnerableUntil = nextPush = nextPunch = 0;
        axis = 0; action = 0;
        grounded = false; lastVerticalSpeed = 0; nextFootstep = 0;
        ClearRequests();
        nextAIThink = aiBlockUntil = aiChargeUntil = 0; aiThreatSince = -1;
        AIDecisions = AIActionRequests = 0;
        previousGamepad = null;
        art.enabled=true;art.color = Color.white;
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
        if(RingOutFalling) {
            float t=Mathf.Clamp01((Time.time-ringOutStarted)/.65f);
            art.transform.localScale=baseScale*Mathf.Lerp(1,.55f,t*t);
            art.transform.localRotation=Quaternion.identity;
            art.color=new Color(1,1,1,1-Mathf.SmoothStep(0,1,t));
            return;
        }
        if(game.playing && !KnockedOut && Body.linearVelocity.y<-.25f && Mathf.Abs(Body.position.x)>game.SafeFloorHalfWidth-.05f
            && Body.position.y<game.plank.position.y+standingSize.y*.5f-.1f) {
            StartRingOut(game);return;
        }
        if (!game.playing && UltimateActive) { CancelUltimate(); StopAllCoroutines(); action = 0; }
        var keys = Keyboard.current;
        if (aiControlled) {
            if (inputEnabled && game.playing && !KnockedOut && !Downed) UpdateAI(game);
            else ClearAIInput();
        } else if (inputEnabled && game.playing && !KnockedOut && !Downed) {
            axis = keys == null ? 0 : id == 0 ? (keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0)
                           : (keys.rightArrowKey.isPressed ? 1 : 0) - (keys.leftArrowKey.isPressed ? 1 : 0);
            bool held = false;
            blockHeld = false;
            if (keys != null) {
                jumpRequested |= id == 0 ? keys.wKey.wasPressedThisFrame : keys.upArrowKey.wasPressedThisFrame;
                pushRequested |= id == 0 ? keys.yKey.wasPressedThisFrame : keys.oKey.wasPressedThisFrame;
                punchRequested |= id == 0 ? keys.tKey.wasPressedThisFrame : keys.iKey.wasPressedThisFrame;
                blockHeld = id == 0 ? keys.sKey.isPressed : keys.downArrowKey.isPressed;
                dashRequested |= id == 0 ? keys.eKey.wasPressedThisFrame : keys.pKey.wasPressedThisFrame;
                backstepRequested |= id == 0 ? keys.qKey.wasPressedThisFrame : keys.uKey.wasPressedThisFrame;
                grabRequested |= id == 0 ? keys.fKey.wasPressedThisFrame : keys.jKey.wasPressedThisFrame;
                specialRequested |= id == 0 ? keys.gKey.wasPressedThisFrame : keys.kKey.wasPressedThisFrame;
                ultimateRequested |= id == 0 ? keys.hKey.wasPressedThisFrame : keys.lKey.wasPressedThisFrame;
            }
            var pad = game.GetGamepad(id);
            // Losing a held special cancels its charge rather than firing an unintended smash.
            if (pad != previousGamepad) { if(previousGamepad!=null && Charging)CancelSpecialCharge();padUpHeld=false; }
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
                    bool up=pad.dpad.up.isPressed || pad.leftStick.y.ReadValue()>.65f;
                    jumpRequested |= up && !padUpHeld;padUpHeld=up;
                    punchRequested |= pad.buttonWest.wasPressedThisFrame;
                    pushRequested |= pad.buttonEast.wasPressedThisFrame;
                    blockHeld |= pad.buttonSouth.isPressed || pad.leftTrigger.isPressed;
                    backstepRequested |= pad.leftShoulder.wasPressedThisFrame;
                    dashRequested |= pad.rightShoulder.wasPressedThisFrame;
                    specialRequested |= pad.buttonNorth.wasPressedThisFrame;
                }
                ultimateChordHeld = ultimateChord;
            }
            chargePressed |= held && !chargeHeld;
            chargeReleased |= !held && chargeHeld;
            chargeHeld = held;
            if (pad == null) { ultimateChordHeld = false;padUpHeld=false; }
            else if (WoodlandGame.PadMenuChord(pad)) ultimateChordHeld = pad.leftTrigger.isPressed && pad.rightTrigger.isPressed;
        } else { axis = 0; blockHeld = chargeHeld = false; }
        if (game.playing && transform.position.y < game.fallBoundary) KnockOut();
        if(Charging && (!CanAct || !game.playing))CancelSpecialCharge();
        SetBlock((blockHeld || pointerGuard) && game.playing);
        if (Blocking) {
            Shield = Mathf.Max(0, Shield - shieldDrain * Time.deltaTime);
            if (Shield <= 0) BreakGuard();
            else if (Time.time >= nextWard) { nextWard = Time.time + .07f; WoodlandEffects.Instance?.Ward(Body.position, face, Element, Shield / shieldCapacity); }
        } else if (!Charging && action==0 && Time.time >= shieldBrokenUntil && Time.time >= nextEnergyRegen) Shield = Mathf.Min(shieldCapacity, Shield + shieldRecovery * Time.deltaTime);
        if (Charging && Time.time >= nextWard) { nextWard = Time.time + .08f; WoodlandEffects.Instance?.ChargeAura(Body.position + Vector2.up*.15f, SkillIndex, Charge); }
        if (Charging && Charge >= 1) ReleaseSmash();
        string state = victoryPlaying ? "Victory" : KnockedOut ? "Knockout" : Downed ? (recovering?"GetUp":KnockdownRemaining>knockdownDuration-.35f?"Knockout":"Downed") : Time.time < hurtUntil ? "Hurt" : Blocking ? "Block" : Charging ? "Charge" : Time.time < dashUntil ? (AnimationState == "Backstep" ? "Backstep" : "Dash") : action != 0 ? actionAnimation : Time.time < doubleJumpUntil ? "DoubleJump" : !grounded && game.playing ? "Jump" : Mathf.Abs(axis) > .1f ? "Walk" : "Idle";
        PlayState(state);
        art.flipX = face < 0;
        art.color = Time.time < hurtUntil && !KnockedOut ? new Color(1, .72f, .62f) : Color.white;
        art.transform.localRotation = baseRotation;
        smoothScale = baseScale;
        art.transform.localScale = smoothScale;
        if (Body.simulated && Time.time >= nextTrail && (Time.time<dashUntil || UltimateActive)) {
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
        bool threatening = rival.Charging || rival.AttackStartup;
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
        // Recovery is visible information; normal CPU think timing still applies.
        if(rival.AttackRecovery && Mathf.Abs(delta.x)<1.35f && Mathf.Abs(delta.y)<1.05f && action==0 && Shield>=8){
            blockHeld=false;aiBlockUntil=0;axis=0;face=Mathf.Sign(delta.x);punchRequested=true;AIActionRequests++;return;
        }
        if (Charging || chargeHeld || blockHeld || Time.time < shieldBrokenUntil) return;
        if (Shield < 14) {
            float retreat = Mathf.Clamp(local.x - Mathf.Sign(delta.x) * 1.4f, center - safeHalf, center + safeHalf);
            axis = Mathf.Abs(retreat - local.x) > .2f ? Mathf.Sign(retreat - local.x) : 0;
            return;
        }
        // Defend against pressure already received, without reading the next input.
        if(grounded && Time.time<aiPressureUntil && Mathf.Abs(delta.x)<1.8f && Shield>25 && action==0 && ComboCount==0){
            axis=0;face=Mathf.Sign(delta.x);blockHeld=true;aiBlockUntil=Time.time+.4f;AIActionRequests++;return;
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
            } else if(ComboCount==2)pushRequested=true;
            else if (ComboCount > 0 || Random.value < .55f) punchRequested = true;
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
        string nativeState=state;
        if((state=="Downed" || state=="GetUp") && !animator.HasState(0,Animator.StringToHash(state)) && !animator.HasState(0,Animator.StringToHash("Base Layer."+state)))
            nativeState=state=="Downed"?"Knockout":"Idle";
        animator.Play(nativeState, 0, 0);
    }
    void FixedUpdate()
    {
        var game = WoodlandGame.Instance;
        bool wasGrounded = grounded;
        grounded = false;
        int count = Physics2D.Raycast(Body.position + Vector2.down * 1.04f, Vector2.down, new ContactFilter2D { useTriggers = false }, groundHits, .25f);
        for (int i = 0; i < count; i++) {
            var hit = groundHits[i];
            // Only arena surface tops grant traction. Their vertical edges and the rival never do.
            if (Body.linearVelocity.y <= .5f && game != null && game.IsGroundBody(hit.rigidbody) && hit.normal.y > .65f && Vector2.Dot(hit.normal, hit.rigidbody.transform.up) > .9f) {
                grounded = true; groundNormal = hit.normal; groundBody = hit.rigidbody; break;
            }
        }
        if (grounded && Body.linearVelocity.y <= .5f) { AirHitsTaken = 0; AirJumpsRemaining = 1; doubleJumpUntil = 0; }
        if (Body.simulated && grounded && !wasGrounded && lastVerticalSpeed < -2)
            WoodlandEffects.Instance?.Land(Body.position + Vector2.down * 1.1875f, -lastVerticalSpeed);
        lastVerticalSpeed = Body.linearVelocity.y;
        if (game == null || !game.playing || KnockedOut || Downed || RingOutFalling) { ClearRequests(); return; }
        if (Time.time >= hurtUntil) {
            if (action == 0 && !Charging && Time.time >= dashUntil) {
                var rival=game.players[1-id];
                if(rival && Mathf.Abs(rival.Body.position.x-Body.position.x)>.1f)
                    face=Mathf.Sign(rival.Body.position.x-Body.position.x);
            }
            float movement = AttackRecovery ? 0 : Blocking ? axis * .2f : Charging || action == 3 || action == 5 ? 0 : action == 1 || action == 2 ? axis * .55f : axis;
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
                WoodlandEffects.Instance?.Footstep(Body.position + Vector2.down * 1.1875f, axis);
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
    bool CanAct => WoodlandGame.Instance != null && WoodlandGame.Instance.playing && !WoodlandGame.Instance.Paused && !KnockedOut && !Downed && !RingOutFalling && Time.time >= hurtUntil && Time.time >= shieldBrokenUntil && Time.time>=blockStunUntil;
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
        else WoodlandEffects.Instance?.Jump(Body.position + Vector2.down * 1.1875f, Element);
        return true;
    }
    public bool TryDash()
    {
        if (!CanAct || Time.time < rootedUntil || Blocking || Charging || action != 0 || Time.time < dashUntil || Time.time < nextDash) return false;
        if (!SpendStamina(10)) return false;
        nextDash = Time.time + dashCooldown; dashUntil = Time.time + .16f;
        Body.linearVelocity = new Vector2(face * dashSpeed, Mathf.Max(-2, Body.linearVelocity.y));
        PlayState("Dash"); WoodlandEffects.Instance?.Ghost(art,Element);WoodlandEffects.Instance?.Dash(Body.position, face, Element);
        return true;
    }
    public bool TryBackstep()
    {
        if (!CanAct || Time.time < rootedUntil || Blocking || Charging || action != 0 || Time.time < dashUntil || Time.time < nextEvade) return false;
        if (!SpendStamina(12)) return false;
        nextEvade = Time.time + .9f; dashUntil = Time.time + .14f;
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + .16f);
        Body.linearVelocity = new Vector2(-face * 7, Mathf.Max(-2, Body.linearVelocity.y));
        PlayState("Backstep");WoodlandEffects.Instance?.Ghost(art,Element);
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
                other.Body.AddForce(new Vector2(direction * 3, other.Downed?0:2), ForceMode2D.Impulse);
                WoodlandEffects.Instance?.ElementHit(other.Body.position, direction, Element, 1, id, SkillIndex);
                WoodlandEffects.Instance?.Impact(other.Body.position, direction, false, false);
            } else other.Blocking = guarded;
        }
        yield return new WaitForSeconds(.32f);
        action = 0;
    }
    public bool SetBlock(bool held)
    {
        bool allowed = held && (CanAct || (Blocking && Time.time<blockStunUntil && WoodlandGame.Instance.playing && !WoodlandGame.Instance.Paused)) && action == 0 && !Charging && Time.time >= dashUntil && Time.time >= shieldBrokenUntil && Shield > 0 && (Blocking || Shield >= 20);
        if (allowed && !Blocking) { shieldStarted = Time.time >= nextParry ? Time.time : -10; nextParry = Time.time + .45f; }
        Blocking = allowed;
        return Blocking;
    }
    void BreakGuard()
    {
        Blocking = false; Shield = 0; shieldBrokenUntil = Time.time + 1.25f; hurtUntil = shieldBrokenUntil;
        WoodlandEffects.Instance?.GuardImpact(Body.position, face, Element, false, true,CharacterIndex);
    }
    public bool BeginSmash() => TrySpecial();
    public void CancelSpecialCharge(){Charging=false;chargeHeld=false;chargePressed=chargeReleased=false;}
    public bool ReleaseSmash()
    {
        if(!Charging || Charge<1)return false;
        if(!CanAct){CancelSpecialCharge();return false;}
        Charging=false;StartCoroutine(RangedCast());return true;
    }
    public bool TrySpecial()
    {
        if(!CanAct || Blocking || Charging || action!=0 || Time.time<dashUntil)return false;
        if(!SpendStamina(14))return false;
        Charging=true;chargeStart=Time.time;PlayState("Charge");
        WoodlandEffects.Instance?.ChargeAura(Body.position,SkillIndex,0);
        return true;
    }
    public bool SetPointerGuard(bool held)
    {
        pointerGuard=held && CanAct && !Charging && action==0 && Time.time>=dashUntil;
        if(!held)return SetBlock(blockHeld);
        return SetBlock(pointerGuard);
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
            WoodlandEffects.Instance?.RangedProjectile(point,direction,skill,1);
            float forward = (other.Body.position.x - origin.x) * direction;
            if (forward > 0 && forward <= 7.5f && Mathf.Abs(other.Body.position.x - point.x) < .65f && Mathf.Abs(other.Body.position.y - point.y) < 1.1f) {
                float power = skill == 0 ? .95f : skill == 1 ? 1.05f : skill == 2 ? .85f : 1.1f;
                if(skill>=4) {
                    bool hit=SpecialHit(other,direction,power,skill==7?.5f:0,skill==6?.55f:0,skill==4?1.8f:0);
                    if(hit && skill==4)other.Body.AddForce(Vector2.right*direction*2.5f,ForceMode2D.Impulse);
                    yield break;
                }
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
        if (lift > 0 && !other.Downed) other.Body.AddForce(Vector2.up * lift, ForceMode2D.Impulse);
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
    void OnDisable() { CancelUltimate();CancelSpecialCharge();pointerGuard=false; }
    bool UltimateHit(Scout other, float direction, float damage, float slow = 0, float root = 0, float lift = 0)
    {
        if (!CanAct || !other.ReceiveHit(this, direction, true, Mathf.Min(35, damage) / Mathf.Max(.01f, punchDamage))) return false;
        other.slowedUntil = Mathf.Max(other.slowedUntil, Time.time + slow);
        other.rootedUntil = Mathf.Max(other.rootedUntil, Time.time + root);
        if (lift > 0 && !other.Downed) other.Body.AddForce(Vector2.up * lift, ForceMode2D.Impulse);
        WoodlandGame.Instance.DamageEdgeHatch(other.Body.position,2);
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
            WoodlandEffects.Instance?.ChargeAura(Body.position + Vector2.up * .25f, skill, i/4f);
            WoodlandEffects.Instance?.Ghost(art,Element);
            yield return new WaitForSeconds(.14f);
        }
        var game = WoodlandGame.Instance;
        var other = game.players[1 - id];
        if (skill == 1 || skill == 5) {
            Body.AddForce(Vector2.right * direction * 2.5f, ForceMode2D.Impulse);
            for (int i = 0; i < 3; i++) {
                Vector2 delta = other.Body.position - Body.position;
                WoodlandEffects.Instance?.Burst(Body.position + Vector2.right * direction, Vector2.up, 16, Element, 3);
                if (delta.x * direction > 0 && Mathf.Abs(delta.x) < 3 && Mathf.Abs(delta.y) < 1.5f) UltimateHit(other, direction, 10);
                if (i < 2) yield return new WaitForSeconds(.25f);
            }
        } else if (skill == 2 || skill == 7) {
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
            if (contact) UltimateHit(other, skill == 3 && Mathf.Abs(delta.x) > .01f ? Mathf.Sign(delta.x) : direction, skill == 3 ? 35 : 30, skill == 0 || skill == 6 ? 1.1f : 0, skill == 0 || skill == 6 ? .8f : 0, skill == 3 || skill == 4 ? 4 : 0);
        }
        yield return new WaitForSeconds(.45f);
        action = 0;
        CancelUltimate();
    }
    public void PlayVictory()
    {
        CancelUltimate(); StopAllCoroutines(); ClearRequests();
        Downed=recovering=false;RestoreStandingCollider();
        victoryPlaying = true;
        axis = 0; action = 0;
        Blocking = Charging = blockHeld = chargeHeld = false;
        PlayState("Victory");
    }
    public bool TryAttack(bool punch)
    {
        if (!CanAct || Blocking || Charging || Time.time < dashUntil) return false;
        if (action != 0) {
            // Only a confirmed route can queue the next move. A whiff or block commits to recovery.
            if ((action == 1 || action == 2) && ComboCount>0 && comboCount<3) { bufferedAttack = punch ? 2 : 1; bufferedUntil = Time.time + .4f; }
            return false;
        }
        bool chain = ComboCount > 0 && comboCount < 3;
        if (!chain && Time.time < (punch ? nextPunch : nextPush)) return false;
        if (!SpendStamina(punch ? 8 : 6)) return false;
        if (!chain) {comboCount = 0;lastConfirmedAttack=0;}
        if (punch) nextPunch = Time.time + punchCooldown;
        else nextPush = Time.time + pushCooldown;
        StartCoroutine(Attack(punch));
        return true;
    }
    IEnumerator Attack(bool punch)
    {
        action = punch ? 2 : 1;
        AttackRecovery=false;
        float direction = face;
        bool routeFinisher=ComboCount==2 && lastConfirmedAttack==2 && !punch;
        actionAnimation = punch ? "Punch" : "Kick";
        PlayState(actionAnimation);
        WoodlandEffects.Instance?.Windup(Body.position + new Vector2(direction * .55f, 0), direction, Element);
        // Punch checks pressure quickly. Kick trades slower startup for reach.
        bool groundedAttack = grounded;
        yield return new WaitForSeconds(groundedAttack ? (punch ? .10f : routeFinisher ? .12f : .20f) : .22f);
        WoodlandEffects.Instance?.Swipe(Body.position, direction, punch,id,SkillIndex);
        var game = WoodlandGame.Instance;
        Scout other = game.players[1 - id];
        Vector2 delta = other.Body.position - Body.position;
        bool connected=false, guarded=false;
        if (game.playing && !KnockedOut && Time.time >= hurtUntil && delta.x * direction > 0 && Mathf.Abs(delta.x) < (punch ? punchRange : pushRange) && Mathf.Abs(delta.y) < 1.1f) {
            guarded=other.Blocking && (Body.position.x-other.Body.position.x)*other.face>0;
            if (other.ReceiveHit(this, direction, punch, routeFinisher ? 1.4f : 1)) {
                connected=true;
                comboCount=routeFinisher?3:punch && ComboCount==1 && lastConfirmedAttack==2?2:punch?1:0;
                lastConfirmedAttack=punch?2:1;comboUntil = Time.time + .8f;
                WoodlandEffects.Instance?.ElementHit(other.Body.position, direction, Element, comboCount, id, SkillIndex);
                WoodlandEffects.Instance?.Impact(other.Body.position, direction, punch, routeFinisher);
            } else { comboCount = 0; comboUntil = 0; }
        } else { comboCount = 0; comboUntil = 0; }
        // A parry can stop this coroutine from inside ReceiveHit.
        if(action==0 || Time.time<hurtUntil){AttackRecovery=false;yield break;}
        AttackRecovery=true;
        if(!connected){bufferedAttack=0;ShowCombatCue(guarded?"BLOCKED":"WHIFF");}
        // A defender gets a turn after blocking. Misses expose the longest opening.
        float recovery=connected?(routeFinisher?.38f:groundedAttack?.18f:.26f):guarded?(punch?.34f:.44f):(punch?.42f:.52f);
        yield return new WaitForSeconds(recovery);
        action = 0;AttackRecovery=false;
        if (connected && comboCount<3 && bufferedAttack != 0 && Time.time <= bufferedUntil) {
            bool followup = bufferedAttack == 2; bufferedAttack = 0; TryAttack(followup);
        } else bufferedAttack = 0;
    }
    public bool ReceiveHit(Scout attacker, float direction, bool punch, float power = 1)
    {
        if (KnockedOut || RingOutFalling || Time.time < invulnerableUntil || (!Downed && !grounded && AirHitsTaken>=3)) return false;
        if(Downed) {
            // Further hits push a downed body; they never extend its recovery timer.
            Body.AddForce(Vector2.right*direction*(punch?2.8f:3.6f),ForceMode2D.Impulse);
            invulnerableUntil=Time.time+.18f;
            return true;
        }
        float damage = (punch ? attacker.punchDamage : attacker.pushDamage) * power * DamageScale;
        bool frontal = (attacker.Body.position.x - Body.position.x) * face > 0;
        if (Blocking && frontal) {
            bool parry = Time.time - shieldStarted <= parryWindow;
            WoodlandEffects.Instance?.GuardImpact(Body.position, face, Element, parry, false,CharacterIndex);
            if (parry) { attacker.Stagger(.4f); Shield = Mathf.Min(shieldCapacity, Shield + 8);ShowCombatCue("PARRY"); }
            else { Shield = Mathf.Max(0, Shield - damage * .8f); Body.AddForce(Vector2.right * direction * .35f, ForceMode2D.Impulse);blockStunUntil=Time.time+.055f;nextEnergyRegen=Time.time+.65f; if (Shield <= 0) BreakGuard(); }
            return false;
        }
        bool punish=AttackRecovery,counter=AttackStartup || Charging;
        if(punish || counter){attacker.ShowCombatCue(punish?"PUNISH":"COUNTER HIT");WoodlandEffects.Instance?.TacticalImpact(Body.position,direction,punish);}
        if(aiControlled)aiPressureUntil=Time.time+1.1f;
        if(!grounded) { AirHitsTaken++;damage*=Mathf.Pow(.75f,AirHitsTaken); }
        Blocking = Charging = false; comboCount = bufferedAttack = 0; comboUntil = 0;
        HitsRemaining=Mathf.Max(0,HitsRemaining-1);
        // Legacy readers receive a ratio; confirmed hit count is the sole knockdown rule.
        Health=maxHealth*HitsRemaining/HitsToKnockdown;
        float buildup = 1 + (1 - Health / maxHealth) * damageKnockbackMultiplier;
        float force = (punch ? attacker.punchKnockback : attacker.pushKnockback) * buildup * Mathf.Lerp(1, 1.65f, Mathf.Clamp01(power - 1));
        bool groundedCombo = attacker.grounded && grounded && (attacker.action == 1 || attacker.action == 2);
        bool finisher = groundedCombo && attacker.comboCount == 2 && attacker.lastConfirmedAttack==2 && !punch;
        if(punish || counter)force*=1.2f;
        // Early grounded hits preserve close spacing so a buffered string can finish without auto-tracking.
        if (groundedCombo && !finisher) force *= .22f;
        Body.linearVelocity = new Vector2(0, Mathf.Max(0, Body.linearVelocity.y));
        Body.AddForce(new Vector2(direction * force, groundedCombo && finisher && !punch ? 5.5f : groundedCombo && !finisher ? .35f : AirHitsTaken>0 ? .6f : punch ? 1.9f : 1.1f), ForceMode2D.Impulse);
        hurtUntil = Mathf.Max(hurtUntil, Time.time + (groundedCombo ? .16f : punch ? .28f : .24f));
        invulnerableUntil = Time.time + .22f;
        action = 0;AttackRecovery=false;
        CancelSpecialCharge();pointerGuard=false;
        CancelUltimate();
        StopAllCoroutines();
        PlayState("Hurt");
        if (HitsRemaining == 0) StartCoroutine(TemporaryKnockdown());
        return true;
    }
    void RestoreStandingCollider()
    {
        if(!bodyCollider)return;
        bodyCollider.direction=standingDirection;bodyCollider.size=standingSize;bodyCollider.offset=standingOffset;
    }
    IEnumerator TemporaryKnockdown()
    {
        Downed=true;recovering=false;knockedDownUntil=Time.time+knockdownDuration;
        axis=0;ClearRequests();pointerGuard=false;Blocking=Charging=blockHeld=chargeHeld=false;
        bodyCollider.direction=CapsuleDirection2D.Horizontal;
        bodyCollider.size=new Vector2(2.1f,.9f);bodyCollider.offset=new Vector2(0,-.7375f);
        PlayState("Knockout");
        yield return new WaitForSeconds(.35f);
        PlayState("Downed");
        yield return new WaitForSeconds(Mathf.Max(0,knockdownDuration-.95f));
        recovering=true;PlayState("GetUp");
        yield return new WaitForSeconds(.6f);
        Downed=recovering=false;RestoreStandingCollider();
        if(!KnockedOut) {
            HitsRemaining=HitsToKnockdown;Health=maxHealth;hurtUntil=0;
            invulnerableUntil=Mathf.Max(invulnerableUntil,Time.time+.35f);
            PlayState("Idle");
        }
    }
    public void Stagger(float duration)
    {
        pointerGuard=false;CancelSpecialCharge();
        CancelUltimate(); StopAllCoroutines(); action = 0; Blocking = Charging = false; comboCount = bufferedAttack = 0; comboUntil = 0;
        AttackRecovery=false;hurtUntil = Time.time + duration; PlayState("Hurt");
    }
    void ShowCombatCue(string message){combatCue=message;cueUntil=Time.time+.7f;}
    void StartRingOut(WoodlandGame game)
    {
        CancelUltimate();CancelSpecialCharge();StopAllCoroutines();ClearRequests();
        AttackRecovery=false;RingOutFalling=true;ringOutStarted=Time.time;Downed=recovering=false;
        axis=0;action=0;Blocking=Charging=blockHeld=chargeHeld=false;
        bodyCollider.enabled=false;art.sortingOrder=3;
        Body.linearVelocity=new Vector2(Body.linearVelocity.x*.12f,Mathf.Min(-1,Body.linearVelocity.y));
        PlayState("Knockout");
        WoodlandEffects.Instance?.RingOut(new Vector2(Body.position.x,game.plank.position.y+.12f),Element);
        StartCoroutine(FinishRingOut());
    }
    IEnumerator FinishRingOut()
    {
        yield return new WaitForSeconds(.65f);
        Body.simulated=false;art.enabled=false;KnockOut();
    }
    public void KnockOut()
    {
        if (KnockedOut) return;
        CancelUltimate();
        AttackRecovery=false;Downed=recovering=false;
        KnockedOut = true; Blocking = Charging = false; comboCount = bufferedAttack = 0; comboUntil = 0;
        Health = 0;
        action = 0;
        axis = 0;
        StopAllCoroutines();
        PlayState("Knockout");
        if(!RingOutFalling)WoodlandEffects.Instance?.Knockout(Body.position, Element);
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
