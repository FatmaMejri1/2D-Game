using UnityEngine;

/// <summary>The goal flag of the platformer: reach it with every gem to win.</summary>
public class FlagGoal : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerMovement>() == null) return;
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        if (gm.Gems >= gm.gemsRequired)
        {
            Fx.Burst(transform.position + Vector3.up, new Color(1f, 0.9f, 0.3f), 24, 6f, 0.25f);
            gm.Win();
        }
        else if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMessage("Collect all " + gm.gemsRequired + " gems first!  (" + gm.Gems + "/" + gm.gemsRequired + ")", 2.5f);
            AudioManager.Play(Sfx.Locked);
        }
    }
}
