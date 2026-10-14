using UnityEngine;

/// <summary>Placed in the editor-built Level1 scene: creates the HUD and starts the game.</summary>
public class GameSceneSetup : MonoBehaviour
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
        if (gameManager != null && player != null) gameManager.Initialize(player);
    }
}
