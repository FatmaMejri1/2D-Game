using UnityEngine;

/// <summary>
/// A walking enemy for the platformer. It turns around at walls and ledges.
/// Jump on its head to squash it; touch it from the side and you lose a life.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Walker : MonoBehaviour
{
    public float speed = 1.8f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    ContactFilter2D filter;
    readonly RaycastHit2D[] hits = new RaycastHit2D[8];
    int dir = -1;
    bool dead;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        filter = new ContactFilter2D();
        filter.useTriggers = false;
    }

    void FixedUpdate()
    {
        var gm = GameManager.Instance;
        if (dead || gm == null || gm.State != GameState.Playing)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        Vector2 pos = rb.position;
        bool wall = Blocked(pos, pos + new Vector2(dir * 0.55f, 0.05f));
        bool ground = Blocked(pos + new Vector2(dir * 0.5f, 0f), pos + new Vector2(dir * 0.5f, -0.9f));
        if (wall || !ground) dir = -dir;

        rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
        if (sr != null) sr.flipX = dir > 0;
    }

    bool Blocked(Vector2 from, Vector2 to)
    {
        int n = Physics2D.Linecast(from, to, filter, hits);
        for (int i = 0; i < n; i++)
        {
            Collider2D c = hits[i].collider;
            if (c == null || c.isTrigger) continue;
            if (c.attachedRigidbody == rb) continue;
            if (c.GetComponent<PlayerMovement>() != null) continue;
            return true;
        }
        return false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Touch(collision.collider);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        Touch(collision.collider);
    }

    void Touch(Collider2D other)
    {
        if (dead) return;
        var player = other.GetComponent<PlayerMovement>();
        var gm = GameManager.Instance;
        if (player == null || gm == null || gm.State != GameState.Playing) return;

        bool stomp = player.transform.position.y > transform.position.y + 0.3f;
        if (stomp)
        {
            dead = true;
            player.Bounce(9f);
            gm.AddScore(200, transform.position, "SQUASH +200");
            AudioManager.Play(Sfx.Coin);
            Fx.Burst(transform.position, new Color(0.3f, 0.85f, 0.35f), 12, 4f, 0.2f);
            Destroy(gameObject);
        }
        else
        {
            gm.Damage("An enemy got you!");
        }
    }
}
