using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class Scout : MonoBehaviour
{
    [Header("Identity and scene references")]
    [Range(0, 1)] public int id;
    public string displayName = "Fern";
    public bool inputEnabled = true;
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
    public bool Blocking { get; private set; }
    public bool Charging { get; private set; }
    public int AirJumpsRemaining { get; private set; } = 1;
    public float DashReady => Mathf.Clamp01(1 - (nextDash - Time.time) / dashCooldown);
    public float Charge => Charging ? Mathf.Clamp01((Time.time - chargeStart) / .85f) : 0;
    public int ComboCount => Time.time <= comboUntil ? comboCount : 0;
    float comboUntil, bufferedUntil;
    int comboCount, bufferedAttack;
    public float Facing => face;
    public Color Element => id == 0 ? new Color(.38f, 1, .68f) : new Color(1, .48f, .18f);
    float nextParry, shieldStarted, shieldBrokenUntil, nextWard, nextDash, dashUntil, chargeStart, doubleJumpUntil;
    bool blockHeld, dashRequested, chargePressed, chargeReleased;
    [Header("Damage and knockout")]
    public float maxHealth = 100;
    public float pushDamage = 12;
    public float punchDamage = 16;
    public float pushKnockback = 4.8f;
    public float punchKnockback = 4;
    public float damageKnockbackMultiplier = .30f;
    public float pushCooldown = .58f;
    public float punchCooldown = .85f;
    public float pushRange = 1.65f;
    public float punchRange = 1.45f;
    public float Health { get; private set; }
    public bool KnockedOut { get; private set; }
    public Rigidbody2D Body { get; private set; }
    public string AnimationState { get; private set; } = "Idle";
    public float PushReady => Mathf.Clamp01(1 - (nextPush - Time.time) / pushCooldown);
    public float PunchReady => Mathf.Clamp01(1 - (nextPunch - Time.time) / punchCooldown);
    public string Status => KnockedOut ? "KNOCKED OUT" : Time.time < shieldBrokenUntil ? "GUARD BROKEN" : Time.time < hurtUntil ? "STAGGERED" : Blocking ? "WARD UP" : Charging ? "CHARGING SMASH" : Time.time < dashUntil ? "DASH!" : action == 3 ? "SMASH!" : action == 1 ? "SHOVE!" : action == 2 ? "PUNCH!" : "HOLD YOUR GROUND";
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
    public void ResetScout()
    {
        StopAllCoroutines();
        Body.simulated = false;
        Vector2 spawn = spawnPoint.position;
        transform.SetPositionAndRotation(spawn, Quaternion.identity);
        Body.position = spawn;
        Body.linearVelocity = Vector2.zero;
        Body.angularVelocity = 0;
        face = id == 0 ? 1 : -1;
        Health = maxHealth;
        Shield = shieldCapacity; Blocking = Charging = blockHeld = false;
        nextParry = 0; shieldStarted = -10; shieldBrokenUntil = nextWard = nextDash = dashUntil = doubleJumpUntil = 0;
        AirJumpsRemaining = 1; comboCount = bufferedAttack = 0; comboUntil = bufferedUntil = 0;
        KnockedOut = false;
        hurtUntil = invulnerableUntil = nextPush = nextPunch = 0;
        axis = 0; action = 0;
        grounded = false; lastVerticalSpeed = 0; nextFootstep = 0;
        ClearRequests();
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
        if (game == null || game.Paused) return;
        var keys = Keyboard.current;
        if (inputEnabled && game.playing && !KnockedOut && keys != null) {
            axis = id == 0 ? (keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0)
                           : (keys.rightArrowKey.isPressed ? 1 : 0) - (keys.leftArrowKey.isPressed ? 1 : 0);
            jumpRequested |= id == 0 ? keys.wKey.wasPressedThisFrame : keys.upArrowKey.wasPressedThisFrame;
            pushRequested |= id == 0 ? keys.fKey.wasPressedThisFrame : keys.kKey.wasPressedThisFrame;
            punchRequested |= id == 0 ? keys.gKey.wasPressedThisFrame : keys.lKey.wasPressedThisFrame;
            blockHeld = id == 0 ? keys.sKey.isPressed : keys.downArrowKey.isPressed;
            dashRequested |= id == 0 ? keys.leftShiftKey.wasPressedThisFrame : keys.rightShiftKey.wasPressedThisFrame;
            chargePressed |= id == 0 ? keys.hKey.wasPressedThisFrame : keys.jKey.wasPressedThisFrame;
            chargeReleased |= id == 0 ? keys.hKey.wasReleasedThisFrame : keys.jKey.wasReleasedThisFrame;
        } else { axis = 0; blockHeld = false; }
        if (game.playing && transform.position.y < game.fallBoundary) KnockOut();
        SetBlock(blockHeld && game.playing);
        if (Blocking) {
            Shield = Mathf.Max(0, Shield - shieldDrain * Time.deltaTime);
            if (Shield <= 0) BreakGuard();
            else if (Time.time >= nextWard) { nextWard = Time.time + .07f; WoodlandEffects.Instance?.Ward(Body.position, face, Element, Shield / shieldCapacity); }
        } else if (Time.time >= shieldBrokenUntil) Shield = Mathf.Min(shieldCapacity, Shield + shieldRecovery * Time.deltaTime);
        if (Charging && Time.time >= nextWard) { nextWard = Time.time + .08f; WoodlandEffects.Instance?.Windup(Body.position + Vector2.right * face * .45f, face, Element); }
        if (Charging && Charge >= 1) ReleaseSmash();
        string state = KnockedOut ? "Knockout" : Time.time < hurtUntil ? "Hurt" : Blocking ? "Block" : Charging ? "Charge" : Time.time < dashUntil ? "Dash" : action != 0 ? actionAnimation : Time.time < doubleJumpUntil ? "DoubleJump" : !grounded && game.playing ? "Jump" : Mathf.Abs(axis) > .1f ? "Walk" : "Idle";
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
            WoodlandEffects.Instance?.Ghost(art, id == 0 ? new Color(.5f, 1, .62f) : new Color(1, .62f, .32f));
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
            // Only the top of the plank is ground. Its vertical edges and the rival cannot grant traction.
            if (Body.linearVelocity.y <= .5f && game != null && hit.rigidbody == game.plank && hit.normal.y > .65f && Vector2.Dot(hit.normal, game.plank.transform.up) > .9f) {
                grounded = true; groundNormal = hit.normal; break;
            }
        }
        if (grounded && Body.linearVelocity.y <= .5f) AirJumpsRemaining = 1;
        if (Body.simulated && grounded && !wasGrounded && lastVerticalSpeed < -2)
            WoodlandEffects.Instance?.Land(Body.position + Vector2.down * .625f, -lastVerticalSpeed);
        lastVerticalSpeed = Body.linearVelocity.y;
        if (game == null || !game.playing || KnockedOut) { ClearRequests(); return; }
        if (Time.time >= hurtUntil) {
            if (axis != 0 && action == 0) face = Mathf.Sign(axis);
            float movement = Blocking ? axis * .2f : Charging ? 0 : axis;
            if (Time.time < dashUntil) { ClearRequests(); return; }
            if (grounded) {
                Vector2 tangent = new Vector2(groundNormal.y, -groundNormal.x);
                Vector2 platformVelocity = game.plank.GetPointVelocity(Body.position);
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
            if (jumpRequested) TryJump();
            if (dashRequested) TryDash();
            if (chargePressed) BeginSmash();
            if (chargeReleased) ReleaseSmash();
            if (punchRequested) TryAttack(true);
            else if (pushRequested) TryAttack(false);
        }
        ClearRequests();
    }
    void ClearRequests() { jumpRequested = pushRequested = punchRequested = dashRequested = chargePressed = chargeReleased = false; }
    bool CanAct => WoodlandGame.Instance != null && WoodlandGame.Instance.playing && !KnockedOut && Time.time >= hurtUntil && Time.time >= shieldBrokenUntil;
    public bool TryJump()
    {
        if (!CanAct || Blocking || Charging || Time.time < dashUntil || action != 0 || (!grounded && AirJumpsRemaining == 0)) return false;
        bool aerial = !grounded;
        if (aerial) AirJumpsRemaining--;
        else { AirJumpsRemaining = 1; grounded = false; }
        Body.linearVelocity = new Vector2(Body.linearVelocity.x, jumpSpeed * (aerial ? .92f : 1));
        if (aerial) { doubleJumpUntil = Time.time + .48f; PlayState("DoubleJump"); WoodlandEffects.Instance?.AirJump(Body.position + Vector2.down * .6f, Element, id); }
        else WoodlandEffects.Instance?.Jump(Body.position + Vector2.down * .625f, Element);
        return true;
    }
    public bool TryDash()
    {
        if (!CanAct || Blocking || Charging || action != 0 || Time.time < nextDash) return false;
        nextDash = Time.time + dashCooldown; dashUntil = Time.time + .16f;
        Body.linearVelocity = new Vector2(face * dashSpeed, Mathf.Max(-2, Body.linearVelocity.y));
        PlayState("Dash"); WoodlandEffects.Instance?.Dash(Body.position, face, Element);
        return true;
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
    public bool BeginSmash()
    {
        if (!CanAct || Blocking || Charging || action != 0 || Time.time < dashUntil || Time.time < nextPunch) return false;
        Charging = true; chargeStart = Time.time; PlayState("Charge");
        WoodlandEffects.Instance?.Windup(Body.position, face, Element);
        return true;
    }
    public bool ReleaseSmash()
    {
        if (!Charging) return false;
        float power = Charge; Charging = false;
        if (!CanAct) return false;
        nextPunch = Time.time + 1.05f;
        StartCoroutine(Smash(power)); return true;
    }
    IEnumerator Smash(float power)
    {
        action = 3; actionAnimation = "Smash"; PlayState(actionAnimation);
        float direction = face;
        yield return new WaitForSeconds(.16f);
        WoodlandEffects.Instance?.Smash(Body.position, direction, Element, power);
        var game = WoodlandGame.Instance;
        var other = game.players[1-id]; Vector2 delta = other.Body.position - Body.position;
        if (game.playing && !KnockedOut && delta.x * direction > 0 && Mathf.Abs(delta.x) < 1.9f && Mathf.Abs(delta.y) < 1.3f)
            if(other.ReceiveHit(this, direction, true, 1.1f + power * .9f)) WoodlandEffects.Instance?.HeavyImpact(other.Body.position,direction,Element,power);
        yield return new WaitForSeconds(.4f); action = 0;
    }
    public bool TryAttack(bool punch)
    {
        var game = WoodlandGame.Instance;
        if (game == null || !game.playing || KnockedOut || Blocking || Charging || Time.time < dashUntil || Time.time < hurtUntil) return false;
        if (action != 0) {
            if (action == 1 || action == 2) { bufferedAttack = punch ? 2 : 1; bufferedUntil = Time.time + .55f; }
            return false;
        }
        bool chain = ComboCount > 0 && comboCount < 3;
        if (!chain && Time.time < (punch ? nextPunch : nextPush)) return false;
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
        actionAnimation = comboCount == 1 ? "Combo2" : comboCount == 2 ? "Combo3" : punch ? "Punch" : "Push";
        PlayState(actionAnimation);
        WoodlandEffects.Instance?.Windup(Body.position + new Vector2(direction * .55f, 0), direction, id == 0 ? new Color(.4f, .85f, .4f) : new Color(1, .55f, .2f));
        yield return new WaitForSeconds(.18f);
        WoodlandEffects.Instance?.Swipe(Body.position, direction, punch,id);
        var game = WoodlandGame.Instance;
        Scout other = game.players[1 - id];
        Vector2 delta = other.Body.position - Body.position;
        if (game.playing && !KnockedOut && Time.time >= hurtUntil && delta.x * direction > 0 && Mathf.Abs(delta.x) < (punch ? punchRange : pushRange) && Mathf.Abs(delta.y) < 1.1f) {
            bool finisher = comboCount == 2;
            if (other.ReceiveHit(this, direction, punch, finisher ? 1.4f : 1)) {
                comboCount++; comboUntil = Time.time + 1.05f;
                WoodlandEffects.Instance?.ElementHit(other.Body.position, direction, Element, comboCount, id);
                WoodlandEffects.Instance?.Impact(other.Body.position, direction, punch, finisher);
            } else { comboCount = 0; comboUntil = 0; }

        }
        yield return new WaitForSeconds(.32f);
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
        Body.linearVelocity = new Vector2(0, Mathf.Max(0, Body.linearVelocity.y));
        Body.AddForce(new Vector2(direction * force, punch ? 1.9f : 1.1f), ForceMode2D.Impulse);
        hurtUntil = Mathf.Max(hurtUntil, Time.time + (punch ? .32f : .27f));
        invulnerableUntil = Time.time + .22f;
        action = 0;
        StopAllCoroutines();
        PlayState("Hurt");
        if (Health <= 0) KnockOut();
        return true;
    }
    public void Stagger(float duration)
    {
        StopAllCoroutines(); action = 0; Blocking = Charging = false; comboCount = bufferedAttack = 0; comboUntil = 0;
        hurtUntil = Time.time + duration; PlayState("Hurt");
    }
    public void KnockOut()
    {
        if (KnockedOut) return;
        KnockedOut = true; Blocking = Charging = false; comboCount = bufferedAttack = 0; comboUntil = 0;
        Health = 0;
        action = 0;
        axis = 0;
        StopAllCoroutines();
        PlayState("Knockout");
        WoodlandEffects.Instance?.Knockout(Body.position, id == 0 ? new Color(.6f, .85f, .35f) : new Color(1, .6f, .25f));
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
