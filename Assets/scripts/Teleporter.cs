using UnityEngine;

/// <summary>Step on a pad to jump to its partner pad.</summary>
public class Teleporter : MonoBehaviour
{
    public Teleporter partner;

    static float lockUntil;
    SpriteRenderer sr;
    Color baseColor = Color.white;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    void Update()
    {
        if (sr == null) return;
        Color c = baseColor;
        c.a = 0.6f + 0.3f * Mathf.Sin(Time.time * 3f);
        sr.color = c;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (partner == null || Time.time < lockUntil) return;
        var player = other.GetComponent<PlayerMovement>();
        if (player == null) return;

        lockUntil = Time.time + 1f;
        Vector2 from = transform.position;
        Vector2 to = partner.transform.position;
        Fx.Burst(from, baseColor, 14, 4f, 0.2f);
        player.Respawn(to, 0.6f);
        Fx.Burst(to, baseColor, 14, 4f, 0.2f);
        AudioManager.Play(Sfx.Teleport);
        if (CameraFollow.Instance != null) CameraFollow.Instance.Snap();
    }
}
