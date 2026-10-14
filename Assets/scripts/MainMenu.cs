using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Main menu: Play / Jump mode / Settings (volume slider) / Quit.</summary>
public class MainMenu : MonoBehaviour
{
    public Button playButton;
    public Button platformerButton;
    public Button settingsButton;
    public Button quitButton;
    public Button backButton;
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public Slider volumeSlider;
    public Text volumeLabel;
    public Text bestText;

    void Start()
    {
        Time.timeScale = 1f;

        if (playButton != null) playButton.onClick.AddListener(Play);
        if (platformerButton != null) platformerButton.onClick.AddListener(PlayPlatformer);
        if (settingsButton != null) settingsButton.onClick.AddListener(delegate { ShowSettings(true); });
        if (backButton != null) backButton.onClick.AddListener(delegate { ShowSettings(false); });
        if (quitButton != null) quitButton.onClick.AddListener(Quit);

        float volume = PlayerPrefs.GetFloat("EscapeRoomVolume", 1f);
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = volume;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        ApplyVolume(volume);

        if (bestText != null)
            bestText.text = "BEST SCORE  " + PlayerPrefs.GetInt("EscapeRoomBest", 0).ToString("N0");

        ShowSettings(false);
    }

    void Update()
    {
        if (mainPanel != null && mainPanel.activeSelf && GameInput.Confirm()) Play();
    }

    public void Play()
    {
        SceneManager.LoadScene(SceneNames.Game);
    }

    public void PlayPlatformer()
    {
        SceneManager.LoadScene(SceneNames.Platformer);
    }

    public void ShowSettings(bool show)
    {
        if (mainPanel != null) mainPanel.SetActive(!show);
        if (settingsPanel != null) settingsPanel.SetActive(show);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("EscapeRoomVolume", value);
        ApplyVolume(value);
    }

    void ApplyVolume(float value)
    {
        AudioListener.volume = value;
        if (volumeLabel != null) volumeLabel.text = "VOLUME  " + Mathf.RoundToInt(value * 100f) + "%";
    }
}
