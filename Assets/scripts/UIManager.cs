using UnityEngine;
using UnityEngine.UI;

/// <summary>Holds the HUD references (created by CanvasBuilder) and animates/updates them.</summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // HUD texts
    public Text objectivesText;
    public Text timeText;
    public Text scoreText;
    public Text bestText;
    public Text gemsText;
    public Text coinsText;
    public Text powerText;
    public Text comboText;
    public Text soundText;
    public Text promptText;
    public Text messageText;
    public Text bannerTitle;
    public Text bannerSub;

    // HUD graphics
    public Image timerFill;
    public Image flashImage;
    public Image[] hearts;
    public Image[] keySlots;
    public Image[] roomSegments;
    public RectTransform progressDot;
    public RectTransform bannerRect;
    public GameObject progressRoot;

    // popups
    public GameObject promptRoot;
    public GameObject messageRoot;
    public GameObject bannerRoot;
    public CanvasGroup messageGroup;
    public CanvasGroup bannerGroup;

    // overlay (title / pause / win / lose)
    public GameObject overlayRoot;
    public GameObject legendRoot;
    public GameObject starsRoot;
    public Text overlayTitle;
    public Text overlayBody;
    public Text overlayHint;
    public Text controlsText;
    public Image[] stars;

    float messageTimer;
    float bannerTimer;
    float comboTimer;
    float flashAlpha;
    Color flashColor = Color.red;
    float displayedScore;
    int targetScore;
    int shownScore = -1;
    int lastTimeSecond = -1;
    int lastVisited = -1;

    static readonly Color Gold = new Color(1f, 0.88f, 0.4f);
    static readonly Color Dim = new Color(0.2f, 0.22f, 0.3f, 0.9f);

    const float BannerTotal = 3.4f;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // score count-up
        if (scoreText != null)
        {
            displayedScore = Mathf.Lerp(displayedScore, targetScore, 1f - Mathf.Exp(-10f * dt));
            if (Mathf.Abs(displayedScore - targetScore) < 1f) displayedScore = targetScore;
            int shown = Mathf.RoundToInt(displayedScore);
            if (shown != shownScore)
            {
                shownScore = shown;
                scoreText.text = shown.ToString("N0");
            }
        }

        // toast message
        if (messageTimer > 0f)
        {
            messageTimer -= dt;
            if (messageGroup != null) messageGroup.alpha = Mathf.Clamp01(messageTimer / 0.5f);
            if (messageTimer <= 0f && messageRoot != null) messageRoot.SetActive(false);
        }

        // room banner (fade in, hold, fade out, slides up slightly)
        if (bannerTimer > 0f)
        {
            bannerTimer -= dt;
            float elapsed = BannerTotal - bannerTimer;
            if (bannerGroup != null)
                bannerGroup.alpha = Mathf.Clamp01(elapsed / 0.4f) * Mathf.Clamp01(bannerTimer / 0.8f);
            if (bannerRect != null)
                bannerRect.anchoredPosition = new Vector2(0f, 170f - (1f - Mathf.Clamp01(elapsed / 0.5f)) * 40f);
            if (bannerTimer <= 0f && bannerRoot != null) bannerRoot.SetActive(false);
        }

        // combo text
        if (comboTimer > 0f)
        {
            comboTimer -= dt;
            if (comboTimer <= 0f && comboText != null) comboText.text = "";
        }

        // damage flash
        if (flashAlpha > 0f && flashImage != null)
        {
            flashAlpha -= dt * 1.8f;
            Color c = flashColor;
            c.a = Mathf.Max(0f, flashAlpha);
            flashImage.color = c;
        }

        // pulsing hint on the overlay
        if (overlayRoot != null && overlayRoot.activeSelf && overlayHint != null)
        {
            Color c = Gold;
            c.a = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f);
            overlayHint.color = c;
        }
    }

    // ------------------------------------------------------------------ HUD setters

    public void SetTime(float seconds, float fraction)
    {
        if (timeText == null) return;
        int s = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        if (s != lastTimeSecond)
        {
            lastTimeSecond = s;
            timeText.text = GameManager.FormatTime(seconds);
            timeText.color = seconds < 30f ? new Color(1f, 0.35f, 0.35f) : Color.white;
        }
        if (timerFill != null)
        {
            timerFill.fillAmount = Mathf.Clamp01(fraction);
            timerFill.color = fraction < 0.15f ? new Color(1f, 0.3f, 0.3f)
                            : (fraction < 0.35f ? new Color(1f, 0.7f, 0.25f) : new Color(0.3f, 0.85f, 1f));
        }
    }

    public void SetLives(int lives)
    {
        if (hearts == null) return;
        for (int i = 0; i < hearts.Length; i++)
            if (hearts[i] != null)
                hearts[i].color = i < lives ? new Color(1f, 0.28f, 0.38f) : new Color(0.25f, 0.13f, 0.17f, 0.9f);
    }

    public void SetScore(int score, bool instant)
    {
        targetScore = score;
        if (instant) displayedScore = score;
    }

    public void SetBest(int best)
    {
        if (bestText != null) bestText.text = "BEST " + best.ToString("N0");
    }

    public void SetGems(int current, int needed)
    {
        if (gemsText != null) gemsText.text = current + "/" + needed;
    }

    public void SetCoins(int coins)
    {
        if (coinsText != null) coinsText.text = coins.ToString();
    }

    public void SetKeys(bool red, bool blue, bool gold)
    {
        if (keySlots == null || keySlots.Length < 3) return;
        keySlots[0].color = red ? new Color(0.95f, 0.25f, 0.25f) : new Color(0.25f, 0.12f, 0.12f, 0.9f);
        keySlots[1].color = blue ? new Color(0.3f, 0.5f, 1f) : new Color(0.12f, 0.15f, 0.27f, 0.9f);
        keySlots[2].color = gold ? new Color(1f, 0.82f, 0.2f) : new Color(0.27f, 0.23f, 0.1f, 0.9f);
    }

    public void SetPowerups(bool shield, int speedSeconds)
    {
        if (powerText == null) return;
        string s = "";
        if (shield) s += "<color=#7FE8FF>SHIELD</color>   ";
        if (speedSeconds > 0) s += "<color=#FFB347>SPEED " + speedSeconds + "s</color>";
        powerText.text = s;
    }

    public void SetObjectives(string text)
    {
        if (objectivesText != null) objectivesText.text = text;
    }

    public void SetProgress(float t, int visited)
    {
        if (progressDot != null) progressDot.anchoredPosition = new Vector2((t - 0.5f) * 1000f, -4f);
        if (visited != lastVisited && roomSegments != null)
        {
            lastVisited = visited;
            for (int i = 0; i < roomSegments.Length; i++)
                roomSegments[i].color = i < visited ? new Color(0.3f, 0.85f, 1f, 0.9f) : new Color(0.25f, 0.28f, 0.38f, 0.7f);
        }
    }

    public void SetSound(bool on)
    {
        if (soundText != null) soundText.text = on ? "[M] SOUND ON" : "[M] SOUND OFF";
    }

    public void SetPrompt(string text)
    {
        if (promptRoot == null) return;
        bool show = !string.IsNullOrEmpty(text);
        if (promptRoot.activeSelf != show) promptRoot.SetActive(show);
        if (show && promptText != null) promptText.text = text;
    }

    // ------------------------------------------------------------------ popups

    public void ShowMessage(string text, float seconds = 2.5f)
    {
        if (messageText == null) return;
        messageText.text = text;
        if (messageRoot != null) messageRoot.SetActive(true);
        if (messageGroup != null) messageGroup.alpha = 1f;
        messageTimer = seconds;
    }

    public void ShowBanner(string title, string subtitle)
    {
        if (bannerRoot == null) return;
        bannerTitle.text = title;
        bannerSub.text = subtitle;
        bannerRoot.SetActive(true);
        if (bannerGroup != null) bannerGroup.alpha = 0f;
        bannerTimer = BannerTotal;
    }

    public void ShowCombo(int combo)
    {
        if (comboText == null) return;
        comboText.text = "COMBO x" + combo;
        comboTimer = 2f;
    }

    public void Flash(Color color, float strength)
    {
        flashColor = color;
        flashAlpha = strength;
    }

    // ------------------------------------------------------------------ overlay

    public void ShowOverlay(string title, string body, string hint, bool showLegend, int starCount)
    {
        if (overlayRoot == null) return;
        overlayTitle.text = title;
        overlayBody.text = body;
        overlayHint.text = hint;

        if (legendRoot != null) legendRoot.SetActive(showLegend);
        if (controlsText != null) controlsText.gameObject.SetActive(showLegend);

        bool showStars = starCount >= 0;
        if (starsRoot != null) starsRoot.SetActive(showStars);
        if (showStars && stars != null)
            for (int i = 0; i < stars.Length; i++)
                stars[i].color = i < starCount ? Gold : Dim;

        overlayBody.rectTransform.anchoredPosition = new Vector2(0f, showStars ? -330f : -190f);
        overlayRoot.SetActive(true);
    }

    public void HideOverlay()
    {
        if (overlayRoot != null) overlayRoot.SetActive(false);
    }
}
