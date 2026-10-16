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
    public string Status => KnockedOut ? "KNOCKED OUT" : Time.time < hurtUntil ? "STAGGERED" : action == 1 ? "SHOVE!" : action == 2 ? "PUNCH!" : "HOLD YOUR GROUND";
    float axis, face, hurtUntil, invulnerableUntil, nextPush, nextPunch, nextTrail, nextTouch, nextFootstep, lastVerticalSpeed;
    bool jumpRequested, pushRequested, punchRequested, grounded;
    int action;
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
        KnockedOut = false;
        hurtUntil = invulnerableUntil = nextPush = nextPunch = 0;
        axis = 0; action = 0;
        grounded = false; lastVerticalSpeed = 0; nextFootstep = 0;
        jumpRequested = pushRequested = punchRequested = false;
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
        } else axis = 0;
        if (game.playing && transform.position.y < game.fallBoundary) KnockOut();
        string state = KnockedOut ? "Knockout" : Time.time < hurtUntil ? "Hurt" : action == 1 ? "Push" : action == 2 ? "Punch" : !grounded && game.playing ? "Jump" : Mathf.Abs(axis) > .1f ? "Walk" : "Idle";
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
            if (game != null && hit.rigidbody == game.plank && hit.normal.y > .65f && Vector2.Dot(hit.normal, game.plank.transform.up) > .9f) {
                grounded = true; groundNormal = hit.normal; break;
            }
        }
        if (Body.simulated && grounded && !wasGrounded && lastVerticalSpeed < -2)
            WoodlandEffects.Instance?.Land(Body.position + Vector2.down * .625f, -lastVerticalSpeed);
        lastVerticalSpeed = Body.linearVelocity.y;
        if (game == null || !game.playing || KnockedOut) { ClearRequests(); return; }
        if (Time.time >= hurtUntil) {
            if (axis != 0 && action == 0) face = Mathf.Sign(axis);
            if (grounded) {
                Vector2 tangent = new Vector2(groundNormal.y, -groundNormal.x);
                Vector2 platformVelocity = game.plank.GetPointVelocity(Body.position);
                float speed = Vector2.Dot(Body.linearVelocity - platformVelocity, tangent);
                float control = Mathf.Clamp((axis * moveSpeed - speed) * 10, -acceleration, acceleration);
                float slopeGravity = Vector2.Dot(Physics2D.gravity * Body.gravityScale, tangent);
                Body.AddForce(tangent * (control - slopeGravity) * Body.mass);
            } else if (Mathf.Abs(axis) > .1f) {
                // Air steering never cancels gravity or applies wall friction.
                Body.AddForce(Vector2.right * Mathf.Clamp((axis * moveSpeed - Body.linearVelocity.x) * 3, -airAcceleration, airAcceleration) * Body.mass);
            }
            if (grounded && Mathf.Abs(axis) > .1f && Time.time >= nextFootstep) {
                nextFootstep = Time.time + (WoodlandEffects.Instance ? WoodlandEffects.Instance.footstepInterval : .16f);
                WoodlandEffects.Instance?.Footstep(Body.position + Vector2.down * .625f, axis);
            }
            if (jumpRequested && grounded) {
                Body.linearVelocity = new Vector2(Body.linearVelocity.x, jumpSpeed);
                WoodlandEffects.Instance?.Jump(Body.position + Vector2.down * .625f, id == 0 ? new Color(.6f, .8f, .3f) : new Color(1, .65f, .3f));
            }
            if (punchRequested) TryAttack(true);
            else if (pushRequested) TryAttack(false);
        }
        ClearRequests();
    }
    void ClearRequests() { jumpRequested = pushRequested = punchRequested = false; }
    public bool TryAttack(bool punch)
    {
        var game = WoodlandGame.Instance;
        if (game == null || !game.playing || KnockedOut || action != 0 || Time.time < hurtUntil || Time.time < (punch ? nextPunch : nextPush)) return false;
        if (punch) nextPunch = Time.time + punchCooldown;
        else nextPush = Time.time + pushCooldown;
        StartCoroutine(Attack(punch));
        return true;
    }
    IEnumerator Attack(bool punch)
    {
        action = punch ? 2 : 1;
        float direction = face;
        PlayState(punch ? "Punch" : "Push");
        WoodlandEffects.Instance?.Windup(Body.position + new Vector2(direction * .55f, 0), direction, id == 0 ? new Color(.4f, .85f, .4f) : new Color(1, .55f, .2f));
        yield return new WaitForSeconds(.18f);
        WoodlandEffects.Instance?.Swipe(Body.position, direction, punch);
        var game = WoodlandGame.Instance;
        Scout other = game.players[1 - id];
        Vector2 delta = other.Body.position - Body.position;
        if (game.playing && !KnockedOut && Time.time >= hurtUntil && delta.x * direction > 0 && Mathf.Abs(delta.x) < (punch ? punchRange : pushRange) && Mathf.Abs(delta.y) < 1.1f) {
            if (other.ReceiveHit(this, direction, punch)) WoodlandEffects.Instance?.Impact(other.Body.position, direction, punch);
        }
        yield return new WaitForSeconds(.32f);
        action = 0;
    }
    public bool ReceiveHit(Scout attacker, float direction, bool punch)
    {
        if (KnockedOut || Time.time < invulnerableUntil) return false;
        float damage = punch ? attacker.punchDamage : attacker.pushDamage;
        Health = Mathf.Max(0, Health - damage);
        float buildup = 1 + (1 - Health / maxHealth) * damageKnockbackMultiplier;
        float force = (punch ? attacker.punchKnockback : attacker.pushKnockback) * buildup;
        Body.linearVelocity = new Vector2(0, Mathf.Max(0, Body.linearVelocity.y));
        Body.AddForce(new Vector2(direction * force, punch ? 1.9f : 1.1f), ForceMode2D.Impulse);
        hurtUntil = Time.time + (punch ? .32f : .27f);
        invulnerableUntil = Time.time + .22f;
        action = 0;
        StopAllCoroutines();
        PlayState("Hurt");
        if (Health <= 0) KnockOut();
        return true;
    }
    public void KnockOut()
    {
        if (KnockedOut) return;
        KnockedOut = true;
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
