using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Title, Playing, Paused, Won, Lost }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    static bool skipTitleOnce;

    [Header("Rules")]
    public int maxLives = 3;
    public float timeLimit = 420f;
    public int gemsRequired = 5;
    public float invulnerableSeconds = 1.5f;

    [Header("Scene flow (set by the editor-built Level1 scene)")]
    public bool useSceneFlow;       // true: Win/Lose load the Victory / GameOver scenes
    public bool startImmediately;   // true: skip the title overlay (the MainMenu scene is the title)
    public bool trackRooms = true;  // false for the platformer (no room banners)
    public float levelWidth = 80f;  // used by the progress bar

    public GameState State { get; private set; }
    public int Lives { get; private set; }
    public int Gems { get; private set; }
    public int Coins { get; private set; }
    public int Score { get; private set; }
    public float TimeRemaining { get; private set; }
    public Vector2 RespawnPoint { get; set; }

    readonly HashSet<string> keys = new HashSet<string>();
    readonly bool[] roomVisited = new bool[5];
    PlayerMovement player;
    int best;
    int combo;
    float lastCoinTime = -10f;
    int currentRoom = -1;
    int lastTickSecond = -1;
    bool tense;
    bool lastShield;
    int lastSpeedSecond = -1;

    const float RoomWidth = 16f;

    static readonly string[] RoomNames =
    {
        "STORAGE ROOM", "LASER GALLERY", "SENTINEL HALL", "SPIKE FIELD", "VAULT GAUNTLET"
    };
    static readonly string[] RoomHints =
    {
        "Find the red key",
        "Time the lasers. Dodge the saws.",
        "Sentinels hunt what they see. Break line of sight.",
        "Mind the spikes. A lever opens the gate.",
        "Collect every gem. The vault awaits."
    };

    void Awake()
    {
        Instance = this;
        Lives = maxLives;
        TimeRemaining = timeLimit;
        best = PlayerPrefs.GetInt("EscapeRoomBest", 0);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Initialize(PlayerMovement p)
    {
        player = p;
        RespawnPoint = p.transform.position;
        RefreshHud();
        if (UIManager.Instance != null) UIManager.Instance.SetSound(!AudioManager.Instance.Muted);

        if (skipTitleOnce || startImmediately)
        {
            skipTitleOnce = false;
            StartGame();
        }
        else
        {
            SetState(GameState.Title, "<color=#FFFFFF>ESCAPE</color> <color=#35D6FF>ROOM</color>",
                "Find the keys, dodge the traps, grab the treasure.\nCollect " + gemsRequired + " gems to open the vault and escape!\n" +
                "<color=#9AA4B5>Best score: " + best.ToString("N0") + "</color>",
                "PRESS SPACE TO START", true, -1);
        }
    }

    void Update()
    {
        if (GameInput.Mute()) AudioManager.ToggleMute();

        switch (State)
        {
            case GameState.Title:
                if (GameInput.Confirm()) StartGame();
                break;

            case GameState.Playing:
                TickPlaying();
                break;

            case GameState.Paused:
                if (GameInput.Pause()) SetState(GameState.Playing);
                else if (GameInput.Restart()) Restart();
                else if (useSceneFlow && GameInput.Menu())
                {
                    Time.timeScale = 1f;
                    SceneManager.LoadScene("MainMenu");
                }
                break;

            case GameState.Won:
            case GameState.Lost:
                if (GameInput.Restart()) Restart();
                break;
        }
    }

    void TickPlaying()
    {
        TimeRemaining -= Time.deltaTime;
        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            Lose("Time ran out!");
            return;
        }

        var ui = UIManager.Instance;
        if (ui != null)
        {
            ui.SetTime(TimeRemaining, TimeRemaining / timeLimit);
            if (player != null)
            {
                ui.SetProgress(Mathf.Clamp01(player.transform.position.x / levelWidth), VisitedCount());
                int speedSecs = Mathf.CeilToInt(player.SpeedRemaining);
                if (player.HasShield != lastShield || speedSecs != lastSpeedSecond)
                {
                    lastShield = player.HasShield;
                    lastSpeedSecond = speedSecs;
                    ui.SetPowerups(lastShield, speedSecs);
                }
            }
        }

        int secs = Mathf.CeilToInt(TimeRemaining);
        if (secs != lastTickSecond)
        {
            lastTickSecond = secs;
            if (secs <= 10) AudioManager.Play(Sfx.Tick);
        }

        bool wantTense = TimeRemaining < 60f;
        if (wantTense != tense)
        {
            tense = wantTense;
            AudioManager.SetTense(tense);
            if (tense && ui != null) ui.ShowMessage("One minute left! Hurry!", 3f);
        }

        if (trackRooms && player != null)
        {
            int room = Mathf.Clamp(Mathf.FloorToInt(player.transform.position.x / RoomWidth), 0, RoomNames.Length - 1);
            if (room != currentRoom) EnterRoom(room);
        }

        if (GameInput.Pause())
            SetState(GameState.Paused, "PAUSED",
                "Score: " + Score.ToString("N0") + "      Time: " + FormatTime(TimeRemaining) +
                "\nGems: " + Gems + "/" + gemsRequired + "      Coins: " + Coins,
                (useSceneFlow ? "ESC - RESUME      R - RESTART      Q - MAIN MENU" : "ESC - RESUME      R - RESTART"), false, -1);
    }

    void EnterRoom(int room)
    {
        currentRoom = room;
        bool first = !roomVisited[room];
        roomVisited[room] = true;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowBanner(RoomNames[room], "ROOM " + (room + 1) + "   |   " + RoomHints[room]);
        if (room > 0 && first)
        {
            AudioManager.Play(Sfx.Room);
            AddScore(250, player.transform.position, "+250 NEW ROOM");
        }
    }

    int VisitedCount()
    {
        int n = 0;
        for (int i = 0; i < roomVisited.Length; i++) if (roomVisited[i]) n++;
        return n;
    }

    void StartGame()
    {
        SetState(GameState.Playing);
        AudioManager.Play(Sfx.Start);
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage("Find a way out. Good luck!", 3f);
    }

    void SetState(GameState s, string title = "", string body = "", string hint = "", bool legend = false, int stars = -1)
    {
        State = s;
        Time.timeScale = s == GameState.Playing ? 1f : 0f;

        var ui = UIManager.Instance;
        if (ui == null) return;
        if (s == GameState.Playing)
        {
            ui.HideOverlay();
        }
        else
        {
            ui.SetPrompt("");
            ui.ShowOverlay(title, body, hint, legend, stars);
        }
    }

    // ------------------------------------------------------------------ gameplay API

    public bool HasKey(string id) { return keys.Contains(id); }

    public void AddKey(string id)
    {
        keys.Add(id);
        RefreshHud();
    }

    public void AddGem(Vector2 pos)
    {
        Gems++;
        AddScore(500, pos);
        if (ObjectiveManager.Instance != null)
        {
            if (Gems >= gemsRequired) ObjectiveManager.Instance.Complete("gems");
            else ObjectiveManager.Instance.Refresh();
        }
        if (UIManager.Instance != null) UIManager.Instance.SetGems(Gems, gemsRequired);
    }

    public void AddCoin(Vector2 pos)
    {
        Coins++;
        combo = (Time.time - lastCoinTime < 2.5f) ? Mathf.Min(combo + 1, 5) : 1;
        lastCoinTime = Time.time;
        AddScore(100 * combo, pos, combo > 1 ? "+" + (100 * combo) + " x" + combo : null);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetCoins(Coins);
            if (combo > 1) UIManager.Instance.ShowCombo(combo);
        }
    }

    public void AddScore(int points, Vector2 worldPos, string label = null)
    {
        Score += points;
        if (UIManager.Instance != null) UIManager.Instance.SetScore(Score, false);
        Fx.Popup(worldPos, label ?? ("+" + points), new Color(1f, 0.9f, 0.4f), 0.45f);
    }

    public void AddTime(float seconds)
    {
        TimeRemaining += seconds;
    }

    public void Damage(string reason)
    {
        if (State != GameState.Playing || player == null || player.IsInvulnerable) return;
        var ui = UIManager.Instance;
        Vector2 pos = player.transform.position;

        if (player.HasShield)
        {
            player.ConsumeShield();
            player.SetInvulnerable(1f);
            AudioManager.Play(Sfx.ShieldBreak);
            Fx.Burst(pos, new Color(0.3f, 0.9f, 1f), 16, 4.5f, 0.2f);
            if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.15f, 0.25f);
            if (ui != null) ui.ShowMessage("Your shield absorbed the hit!", 2f);
            return;
        }

        Lives--;
        combo = 0;
        AudioManager.Play(Sfx.Hurt);
        Fx.Burst(pos, new Color(1f, 0.25f, 0.3f), 18, 5f, 0.22f);
        if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.35f, 0.4f);
        if (ui != null)
        {
            ui.SetLives(Lives);
            ui.Flash(new Color(1f, 0.1f, 0.15f), 0.5f);
        }

        if (Lives <= 0)
        {
            Lose(reason);
            return;
        }
        if (ui != null) ui.ShowMessage(reason + "   Lives left: " + Lives, 2.5f);
        player.Respawn(RespawnPoint, invulnerableSeconds);
        if (CameraFollow.Instance != null) CameraFollow.Instance.Snap();
    }

    public void Win()
    {
        if (State != GameState.Playing) return;
        AudioManager.Play(Sfx.Win);
        if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.Complete("escape");

        int timeBonus = Mathf.RoundToInt(TimeRemaining * 10f);
        int lifeBonus = Lives * 500;
        int clearBonus = 2000;
        Score += timeBonus + lifeBonus + clearBonus;

        int stars = TimeRemaining >= timeLimit * 0.35f ? 3 : (TimeRemaining >= timeLimit * 0.15f ? 2 : 1);
        bool newBest = Score > best;
        SaveBest();

        GameResult.Won = true;
        GameResult.Score = Score;
        GameResult.Best = best;
        GameResult.Coins = Coins;
        GameResult.Lives = Lives;
        GameResult.Stars = stars;
        GameResult.TimeLeft = TimeRemaining;
        GameResult.Reason = "";

        EndGame(GameState.Won, "Victory", "<color=#7CFC9A>YOU ESCAPED!</color>",
            "Time bonus   +" + timeBonus.ToString("N0") + "\n" +
            "Lives bonus   +" + lifeBonus.ToString("N0") + "\n" +
            "Clear bonus   +" + clearBonus.ToString("N0") + "\n" +
            "Coins collected: " + Coins + "\n\n" +
            "<size=52><color=#FFE066>SCORE  " + Score.ToString("N0") + "</color></size>" +
            (newBest ? "\n<color=#7CFC9A>NEW BEST!</color>" : "\n<color=#9AA4B5>Best: " + best.ToString("N0") + "</color>"),
            "PRESS R TO PLAY AGAIN", false, stars);
    }

    void Lose(string reason)
    {
        if (State != GameState.Playing) return;
        AudioManager.Play(Sfx.Lose);
        AudioManager.SetTense(false);
        bool newBest = Score > best;
        SaveBest();
        GameResult.Won = false;
        GameResult.Score = Score;
        GameResult.Best = best;
        GameResult.Coins = Coins;
        GameResult.Lives = Lives;
        GameResult.Stars = 0;
        GameResult.TimeLeft = TimeRemaining;
        GameResult.Reason = reason;

        EndGame(GameState.Lost, "GameOver", "<color=#FF5566>GAME OVER</color>",
            reason + "\n\nScore: " + Score.ToString("N0") +
            (newBest ? "   <color=#7CFC9A>NEW BEST!</color>" : "   <color=#9AA4B5>Best: " + best.ToString("N0") + "</color>"),
            "PRESS R TO TRY AGAIN", false, -1);
    }

    // Either show the in-scene overlay (generated mode) or load the Victory / GameOver scene.
    void EndGame(GameState s, string scene, string title, string body, string hint, bool legend, int stars)
    {
        if (!useSceneFlow)
        {
            SetState(s, title, body, hint, legend, stars);
            return;
        }

        State = s;
        Time.timeScale = 1f;
        GameResult.LevelScene = SceneManager.GetActiveScene().name;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetPrompt("");
            UIManager.Instance.ShowMessage(s == GameState.Won ? "You made it out!" : "Better luck next time...", 1.6f);
        }
        StartCoroutine(LoadSceneAfter(scene, 1.6f));
    }

    IEnumerator LoadSceneAfter(string scene, float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        SceneManager.LoadScene(scene);
    }

    void SaveBest()
    {
        if (Score > best)
        {
            best = Score;
            PlayerPrefs.SetInt("EscapeRoomBest", best);
            PlayerPrefs.Save();
        }
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        if (useSceneFlow)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }
        skipTitleOnce = true;
        AudioManager.SetTense(false);
        if (LevelSetupHelper.Instance != null)
            LevelSetupHelper.Instance.Rebuild();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void RefreshHud()
    {
        var ui = UIManager.Instance;
        if (ui == null) return;
        ui.SetLives(Lives);
        ui.SetTime(TimeRemaining, 1f);
        ui.SetScore(Score, true);
        ui.SetBest(best);
        ui.SetGems(Gems, gemsRequired);
        ui.SetCoins(Coins);
        ui.SetKeys(HasKey("red"), HasKey("blue"), HasKey("gold"));
        if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.Refresh();
    }

    public static string FormatTime(float seconds)
    {
        int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }
}
