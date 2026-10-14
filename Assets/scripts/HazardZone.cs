using UnityEngine;

/// <summary>
/// Damages the player on contact. Can blink on/off (lasers, spikes), patrol between two points (saws)
/// and spin (rotating beams). Every sprite under this object fades with the blink.
/// </summary>
public class HazardZone : MonoBehaviour
{
    [Header("Blinking")]
    public bool blink;
    public float onTime = 1.5f;
    public float offTime = 2f;
    public float phase;

    [Header("Patrol")]
    public bool patrol;
    public Vector2 pointA;
    public Vector2 pointB;
    public float patrolSpeed = 3f;

    [Header("Misc")]
    public float spinSpeed;      // degrees per second
    public string reason = "Ouch!";

    SpriteRenderer[] renderers;
    Color[] baseColors;
    Collider2D col;
    Rigidbody2D rb;
    bool active = true;

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>();
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (!blink) return;

        float cycle = onTime + offTime;
        float t = Mathf.Repeat(Time.time + phase, cycle);
        bool on = t < onTime;
        float untilOn = on ? 0f : cycle - t;

        if (on != active)
        {
            active = on;
            if (col != null) col.enabled = on;
        }

        float factor;
        if (on) factor = 1f;
        else if (untilOn < 0.6f) factor = Mathf.Sin(Time.time * 40f) > 0f ? 0.55f : 0.2f;   // warning flicker
        else factor = 0.12f;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Color c = baseColors[i];
            c.a *= factor;
            renderers[i].color = c;
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        if (patrol)
        {
            float dist = Vector2.Distance(pointA, pointB);
            if (dist > 0.01f)
            {
                float t = Mathf.PingPong((Time.time + phase) * patrolSpeed / dist, 1f);
                rb.MovePosition(Vector2.Lerp(pointA, pointB, t));
            }
        }
        if (spinSpeed != 0f)
            rb.MoveRotation(rb.rotation + spinSpeed * Time.fixedDeltaTime);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!active) return;
        if (other.GetComponent<PlayerMovement>() == null) return;
        if (GameManager.Instance != null) GameManager.Instance.Damage(reason);
    }
}
