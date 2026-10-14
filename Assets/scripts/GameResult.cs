/// <summary>Result of the last run, handed from the level scenes to the Victory / GameOver scenes.</summary>
public static class GameResult
{
    public static bool Won;
    public static int Score;
    public static int Best;
    public static int Coins;
    public static int Lives;
    public static int Stars;
    public static float TimeLeft;
    public static string Reason = "";
    public static string LevelScene = "Level1";   // scene to reload on "Try again"
}

/// <summary>Scene names used by the project (they must be in the Build Settings).</summary>
public static class SceneNames
{
    public const string Menu = "MainMenu";
    public const string Game = "Level1";
    public const string Platformer = "Platformer";
    public const string GameOver = "GameOver";
    public const string Victory = "Victory";
}
