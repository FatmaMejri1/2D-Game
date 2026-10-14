using UnityEngine;

/// <summary>
/// Input wrapper that works with the legacy Input Manager, the new Input System, or both.
/// </summary>
public static class GameInput
{
    public static Vector2 Move()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#elif ENABLE_INPUT_SYSTEM
        var k = UnityEngine.InputSystem.Keyboard.current;
        if (k == null) return Vector2.zero;
        float x = 0f, y = 0f;
        if (k.aKey.isPressed || k.leftArrowKey.isPressed) x -= 1f;
        if (k.dKey.isPressed || k.rightArrowKey.isPressed) x += 1f;
        if (k.sKey.isPressed || k.downArrowKey.isPressed) y -= 1f;
        if (k.wKey.isPressed || k.upArrowKey.isPressed) y += 1f;
        return new Vector2(x, y);
#else
        return Vector2.zero;
#endif
    }

    public static bool Sprint()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#elif ENABLE_INPUT_SYSTEM
        var k = UnityEngine.InputSystem.Keyboard.current;
        return k != null && k.leftShiftKey.isPressed;
#else
        return false;
#endif
    }

    public static bool Interact()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.E);
#elif ENABLE_INPUT_SYSTEM
        var k = UnityEngine.InputSystem.Keyboard.current;
        return k != null && k.eKey.wasPressedThisFrame;
#else
        return false;
#endif
    }

    public static bool Confirm()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#elif ENABLE_INPUT_SYSTEM
        var k = UnityEngine.InputSystem.Keyboard.current;
        return k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame);
#else
        return false;
#endif
    }

    public static bool Pause()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
#elif ENABLE_INPUT_SYSTEM
        var k = UnityEngine.InputSystem.Keyboard.current;
        return k != null && (k.escapeKey.wasPressedThisFrame || k.pKey.wasPressedThisFrame);
#else
        return false;
#endif
    }

    public static bool Restart()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.R);
#elif ENABLE_INPUT_SYSTEM
        var k = UnityEngine.InputSystem.Keyboard.current;
        return k != null && k.rKey.wasPressedThisFrame;
#else
        return false;
#endif
    }
    public static bool Mute()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.M);
#elif ENABLE_INPUT_SYSTEM
        var k = UnityEngine.InputSystem.Keyboard.current;
        return k != null && k.mKey.wasPressedThisFrame;
#else
        return false;
#endif
    }
    public static bool Menu()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Q);
#elif ENABLE_INPUT_SYSTEM
        var k = UnityEngine.InputSystem.Keyboard.current;
        return k != null && k.qKey.wasPressedThisFrame;
#else
        return false;
#endif
    }
    public static bool Jump()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
#elif ENABLE_INPUT_SYSTEM
        var k = UnityEngine.InputSystem.Keyboard.current;
        return k != null && (k.spaceKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame);
#else
        return false;
#endif
    }
}

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5.5f;
    public float sprintMultiplier = 1.5f;
    public float speedBoostMultiplier = 1.4f;
    public Transform face;
    public GameObject shieldRing;

    [Header("Platformer (side-scroller) mode")]
    public bool platformerMode;
    public float jumpForce = 13f;
    public float fallLimitY = -9f;
    public Vector2 groundCheckOffset = new Vector2(0f, -0.45f);
    public float groundCheckRadius = 0.18f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Animator anim;
    Collider2D myCollider;
    Color baseColor = Color.white;
    Vector2 input;
    Vector2 lastDir = Vector2.right;
    bool sprinting;
    float invulnerableUntil;
    float speedUntil;
    float dustTimer;
    float ringBase = 1.75f;

    // platformer state
    ContactFilter2D groundFilter;
    readonly Collider2D[] groundHits = new Collider2D[8];
    bool grounded;
    bool jumpQueued;
    float coyoteTimer;
    float jumpBuffer;

    public bool HasShield { get; private set; }
    public bool IsInvulnerable { get { return Time.time < invulnerableUntil; } }
    public float SpeedRemaining { get { return Mathf.Max(0f, speedUntil - Time.time); } }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();
        groundFilter = new ContactFilter2D();
        groundFilter.useTriggers = false;
        if (sr != null) baseColor = sr.color;
        if (shieldRing != null) ringBase = shieldRing.transform.localScale.x;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing)
        {
            input = Vector2.zero;
            jumpQueued = false;
            return;
        }

        input = Vector2.ClampMagnitude(GameInput.Move(), 1f);
        sprinting = GameInput.Sprint();

        if (platformerMode)
        {
            input.y = 0f;          // side-scroller: only left / right
            sprinting = false;
            UpdatePlatformer(gm);
        }

        if (input.sqrMagnitude > 0.01f)
        {
            lastDir = input.normalized;
            dustTimer -= Time.deltaTime;
            if (dustTimer <= 0f && (!platformerMode || grounded))
            {
                dustTimer = sprinting ? 0.07f : 0.14f;
                Fx.Burst((Vector2)transform.position - lastDir * 0.35f, new Color(1f, 1f, 1f, 0.35f), 1, 0.8f, 0.16f);
            }
        }
        if (face != null) face.localPosition = lastDir * 0.3f;
        if (sr != null && Mathf.Abs(input.x) > 0.1f) sr.flipX = input.x < 0f;   // mirror the sprite left/right

        if (shieldRing != null && HasShield)
            shieldRing.transform.localScale = Vector3.one * (ringBase + 0.05f * Mathf.Sin(Time.time * 6f));

        if (sr != null)
        {
            Color c = baseColor;
            c.a = IsInvulnerable ? (Mathf.Sin(Time.time * 30f) > 0f ? 0.3f : 1f) : 1f;
            sr.color = c;
        }
    }

    void UpdatePlatformer(GameManager gm)
    {
        // on the ground? (small circle under the feet; the "velocity.y == 0" trick of the slides is fragile)
        grounded = CheckGround() && rb.linearVelocity.y <= 0.1f;
        if (grounded) coyoteTimer = 0.1f; else coyoteTimer -= Time.deltaTime;

        if (GameInput.Jump()) jumpBuffer = 0.12f; else jumpBuffer -= Time.deltaTime;
        if (jumpBuffer > 0f && coyoteTimer > 0f)
        {
            jumpQueued = true;       // the force is applied in FixedUpdate
            jumpBuffer = 0f;
            coyoteTimer = 0f;
            AudioManager.Play(Sfx.Jump);
        }

        // fell into the void
        if (transform.position.y < fallLimitY)
        {
            gm.Damage("You fell into the void!");
            if (transform.position.y < fallLimitY) Respawn(gm.RespawnPoint, invulnerableTime());
        }
    }

    float invulnerableTime() { return 1.5f; }

    bool CheckGround()
    {
        Vector2 p = (Vector2)transform.position + groundCheckOffset;
        int n = Physics2D.OverlapCircle(p, groundCheckRadius, groundFilter, groundHits);
        for (int i = 0; i < n; i++)
        {
            Collider2D c = groundHits[i];
            if (c != null && c != myCollider && !c.isTrigger) return true;
        }
        return false;
    }

    void FixedUpdate()
    {
        if (platformerMode)
        {
            float speed = moveSpeed * (SpeedRemaining > 0f ? speedBoostMultiplier : 1f);
            Vector2 v = rb.linearVelocity;
            v.x = input.x * speed;                       // like the slides: speed * horizontal input
            if (jumpQueued)
            {
                v.y = 0f;
                rb.linearVelocity = v;
                rb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);   // AddForce, like the slides
                jumpQueued = false;
            }
            else
            {
                rb.linearVelocity = v;
            }

            if (anim != null)
            {
                anim.SetFloat("walk", Mathf.Abs(rb.linearVelocity.x));
                anim.SetBool("grounded", grounded);
            }
            return;
        }

        float s = moveSpeed * (sprinting ? sprintMultiplier : 1f) * (SpeedRemaining > 0f ? speedBoostMultiplier : 1f);
        rb.linearVelocity = input * s;
        if (anim != null) anim.SetFloat("walk", rb.linearVelocity.magnitude);   // drives Idle <-> Walk
    }

    // ------------------------------------------------------------------ tag "danger" (as in the course slides)

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryDanger(collision.gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryDanger(other.gameObject);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryDanger(other.gameObject);
    }

    void TryDanger(GameObject other)
    {
        if (other.tag != "danger") return;
        var gm = GameManager.Instance;
        if (gm == null) return;
        var hazard = other.GetComponentInParent<HazardZone>();
        gm.Damage(hazard != null ? hazard.reason : "Ouch! Watch out for danger!");
    }

    // ------------------------------------------------------------------ public helpers

    public void Bounce(float speed)
    {
        Vector2 v = rb.linearVelocity;
        v.y = speed;
        rb.linearVelocity = v;
        jumpQueued = false;
        coyoteTimer = 0f;
    }

    public void GrantShield()
    {
        HasShield = true;
        if (shieldRing != null) shieldRing.SetActive(true);
    }

    public void ConsumeShield()
    {
        HasShield = false;
        if (shieldRing != null) shieldRing.SetActive(false);
    }

    public void GrantSpeed(float seconds)
    {
        speedUntil = Time.time + seconds;
    }

    public void SetInvulnerable(float seconds)
    {
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);
    }

    public void Respawn(Vector2 position, float invulnerableSeconds)
    {
        transform.position = position;
        rb.position = position;
        rb.linearVelocity = Vector2.zero;
        invulnerableUntil = Time.time + invulnerableSeconds;
    }
}
