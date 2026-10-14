using UnityEngine;

/// <summary>A spring: launches the player upward.</summary>
public class JumpPad : MonoBehaviour
{
    public float power = 17f;
    float cooldownUntil;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.time < cooldownUntil) return;
        var player = other.GetComponent<PlayerMovement>();
        if (player == null) return;

        cooldownUntil = Time.time + 0.3f;
        player.Bounce(power);
        AudioManager.Play(Sfx.PowerUp);
        Fx.Burst(transform.position + Vector3.up * 0.3f, new Color(1f, 0.4f, 0.4f), 8, 3f, 0.16f);
    }
}
