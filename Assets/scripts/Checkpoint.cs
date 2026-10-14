using UnityEngine;

/// <summary>Touch it once to set your respawn point.</summary>
public class Checkpoint : MonoBehaviour
{
    bool reached;
    SpriteRenderer flag;

    void Awake()
    {
        flag = GetComponentInChildren<SpriteRenderer>();
        if (flag != null) flag.color = new Color(0.6f, 0.62f, 0.68f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (reached || other.GetComponent<PlayerMovement>() == null) return;
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        reached = true;
        gm.RespawnPoint = (Vector2)transform.position + new Vector2(0f, 0.8f);
        if (flag != null) flag.color = new Color(0.3f, 0.95f, 0.5f);
        Fx.Burst(transform.position + Vector3.up, new Color(0.3f, 0.95f, 0.5f), 12, 4f, 0.2f);
        AudioManager.Play(Sfx.Room);
        gm.AddScore(150, transform.position + Vector3.up, "CHECKPOINT");
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage("Checkpoint reached!", 1.5f);
    }
}
