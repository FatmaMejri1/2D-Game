using UnityEngine;

/// <summary>Placed in the editor-built Platformer scene: creates the HUD and starts the game.</summary>
public class PlatformerSetup : MonoBehaviour
{
    public GameManager gameManager;

    void Awake()
    {
        Time.timeScale = 1f;
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        // the HUD normally already exists in the scene (built in the editor); create it only if it is missing
        if (FindFirstObjectByType<UIManager>() == null)
            CanvasBuilder.Build(transform, gameManager != null ? gameManager.maxLives : 3);
    }

    void Start()
    {
        var player = FindFirstObjectByType<PlayerMovement>();
        if (gameManager == null || player == null) return;

        // objectives of the platformer level
        var objectives = ObjectiveManager.Instance;
        if (objectives != null)
        {
            objectives.Clear();
            objectives.Add("gems", "Collect the gems ({0}/{1})");
            objectives.Add("escape", "Reach the flag");
        }

        var ui = UIManager.Instance;
        if (ui != null && ui.progressRoot != null) ui.progressRoot.SetActive(false);

        gameManager.Initialize(player);

        if (ui != null)
        {
            ui.ShowBanner("JUMP MODE", "Collect all gems, then reach the flag");
            ui.ShowMessage("LEFT / RIGHT to move     SPACE to jump     Stomp enemies!", 5f);
        }
    }
}
