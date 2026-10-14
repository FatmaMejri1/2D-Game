using UnityEngine;

/// <summary>
/// A guard that patrols between two points and chases the player when it can see them.
/// Break line of sight (hide behind pillars) and it gives up.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Sentinel : MonoBehaviour
{
    public Vector2 pointA;
    public Vector2 pointB;
    public float patrolSpeed = 2.2f;
    public float chaseSpeed = 4.2f;
    public float detectRadius = 5f;
    public float loseSightTime = 1.5f;
    public Transform pupil;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Transform target;
    ContactFilter2D filter;
    readonly RaycastHit2D[] hits = new RaycastHit2D[8];
    Vector2 moveDir;
    bool chasing;
    bool toB = true;
    float lastSeen;
    float lastAlert = -10f;
    float stuckTimer;

    static readonly Color PatrolColor = new Color(1f, 0.6f, 0.2f);
    static readonly Color ChaseColor = new Color(1f, 0.15f, 0.2f);

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        filter = new ContactFilter2D();
        filter.useTriggers = false;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (target == null)
        {
            var p = FindFirstObjectByType<PlayerMovement>();
            if (p != null) target = p.transform;
        }
        if (gm == null || gm.State != GameState.Playing || target == null)
        {
            moveDir = Vector2.zero;
            return;
        }

        Vector2 pos = rb.position;
        Vector2 tp = target.position;
        bool sees = Vector2.Distance(pos, tp) < detectRadius && CanSee(pos, tp);

        if (sees)
        {
            if (!chasing && Time.time - lastAlert > 2f)
            {
                AudioManager.Play(Sfx.Alert);
                Fx.Popup(pos + Vector2.up * 0.9f, "!", ChaseColor, 0.8f);
                lastAlert = Time.time;
            }
            chasing = true;
            lastSeen = Time.time;
        }
        else if (chasing && Time.time - lastSeen > loseSightTime)
        {
            chasing = false;
        }

        Vector2 goal;
        if (chasing)
        {
            goal = tp;
        }
        else
        {
            goal = toB ? pointB : pointA;
            if (Vector2.Distance(pos, goal) < 0.3f)
            {
                toB = !toB;
                goal = toB ? pointB : pointA;
            }
        }

        Vector2 to = goal - pos;
        moveDir = to.sqrMagnitude > 0.01f ? to.normalized : Vector2.zero;

        // safety net: if the sentinel gets wedged somewhere, send it home
        if (!chasing && rb.linearVelocity.sqrMagnitude < 0.04f) stuckTimer += Time.deltaTime; else stuckTimer = 0f;
        if (stuckTimer > 2.5f)
        {
            rb.position = pointA;
            stuckTimer = 0f;
        }

        if (sr != null) sr.color = chasing ? ChaseColor : PatrolColor;
        if (pupil != null)
            pupil.localPosition = Vector3.Lerp(pupil.localPosition, (Vector3)(moveDir * 0.12f), 10f * Time.deltaTime);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveDir * (chasing ? chaseSpeed : patrolSpeed);
    }

    bool CanSee(Vector2 from, Vector2 to)
    {
        int n = Physics2D.Linecast(from, to, filter, hits);
        for (int i = 0; i < n; i++)
        {
            Collider2D c = hits[i].collider;
            if (c == null || c.isTrigger) continue;
            if (c.attachedRigidbody == rb) continue;
            if (c.GetComponent<PlayerMovement>() != null) continue;
            return false;
        }
        return true;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.GetComponent<PlayerMovement>() != null && GameManager.Instance != null)
            GameManager.Instance.Damage("Caught by a sentinel!");
    }
}
