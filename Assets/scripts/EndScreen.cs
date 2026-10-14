using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Used by both the Victory and the GameOver scenes.</summary>
public class EndScreen : MonoBehaviour
{
    public bool victory;
    public Text titleText;
    public Text bodyText;
    public Image[] stars;
    public Button retryButton;
    public Button menuButton;

    void Start()
    {
        Time.timeScale = 1f;

        if (retryButton != null) retryButton.onClick.AddListener(Retry);
        if (menuButton != null) menuButton.onClick.AddListener(Menu);

        if (titleText != null)
            titleText.text = victory ? "YOU ESCAPED!" : "GAME OVER";

        if (bodyText != null)
        {
            if (victory)
                bodyText.text = "Score  " + GameResult.Score.ToString("N0") +
                                "\nTime left  " + GameManager.FormatTime(GameResult.TimeLeft) +
                                "\nLives left  " + GameResult.Lives +
                                "\nCoins  " + GameResult.Coins +
                                "\nBest  " + GameResult.Best.ToString("N0");
            else
                bodyText.text = (string.IsNullOrEmpty(GameResult.Reason) ? "You did not make it out." : GameResult.Reason) +
                                "\n\nScore  " + GameResult.Score.ToString("N0") +
                                "\nBest  " + GameResult.Best.ToString("N0");
        }

        if (stars != null)
            for (int i = 0; i < stars.Length; i++)
                if (stars[i] != null)
                    stars[i].color = i < GameResult.Stars ? new Color(1f, 0.88f, 0.3f) : new Color(0.25f, 0.27f, 0.35f);
    }

    void Update()
    {
        if (GameInput.Restart() || GameInput.Confirm()) Retry();
        else if (GameInput.Menu() || GameInput.Pause()) Menu();
    }

    public void Retry()
    {
        // go back to the level that was just played (Level1 or Platformer)
        SceneManager.LoadScene(string.IsNullOrEmpty(GameResult.LevelScene) ? SceneNames.Game : GameResult.LevelScene);
    }

    public void Menu()
    {
        SceneManager.LoadScene(SceneNames.Menu);
    }
}
