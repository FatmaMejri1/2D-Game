#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Menu: Escape Room > Build Full Course Project.
/// Generates sprites, tiles, animations, prefabs and the four scenes
/// (MainMenu, Level1, GameOver, Victory) and puts them in the Build Settings.
/// </summary>
public static class CourseSceneBuilder
{
    const string SceneDir = "Assets/Scenes";

    static readonly Color RedColor = new Color(0.9f, 0.22f, 0.22f);
    static readonly Color BlueColor = new Color(0.28f, 0.5f, 1f);
    static readonly Color GoldColor = new Color(1f, 0.8f, 0.2f);
    static readonly Color GreenColor = new Color(0.2f, 0.85f, 0.45f);
    static readonly Color PurpleColor = new Color(0.7f, 0.4f, 1f);
    static readonly Color GemColor = new Color(0.25f, 0.95f, 1f);
    static readonly Color CoinColor = new Color(1f, 0.85f, 0.2f);

    static Transform itemsRoot, hazardsRoot, enemiesRoot, doorsRoot, propsRoot;

    [MenuItem("Escape Room/Build Full Course Project")]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        try
        {
            EditorUtility.DisplayProgressBar("Escape Room", "Generating sprites, tiles and animations...", 0.1f);
            CourseAssetFactory.EnsureFolder(SceneDir);
            CourseAssetFactory.BuildAll();

            EditorUtility.DisplayProgressBar("Escape Room", "Creating prefabs...", 0.45f);
            CoursePrefabFactory.BuildAll();

            EditorUtility.DisplayProgressBar("Escape Room", "Building scenes...", 0.7f);
            BuildMenuScene();
            BuildLevelScene();
            BuildPlatformerScene();
            BuildEndScene(false);
            BuildEndScene(true);
            SetBuildSettings();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(SceneDir + "/MainMenu.unity");
        EditorUtility.DisplayDialog("Escape Room",
            "Done!\n\nSprites, Tiles, Animations, Prefabs and 5 scenes were created in Assets/.\n\n" +
            (CourseAssetFactory.SheetSliced
                ? "The player sprite sheet (Assets/Sprites/player_sheet.png) is set to Multiple and was sliced into 8 sprites.\n\n"
                : "The player sprite sheet (Assets/Sprites/player_sheet.png) is set to Multiple but could NOT be sliced automatically.\n" +
                  "Open it in the Sprite Editor > Slice > Grid By Cell Size 32 x 32 > Slice > Apply, then run this menu again.\n\n") +
            "The MainMenu scene is open. Press Play to start the game.", "OK");
    }

    static void SetBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(SceneDir + "/MainMenu.unity", true),
            new EditorBuildSettingsScene(SceneDir + "/Level1.unity", true),
            new EditorBuildSettingsScene(SceneDir + "/Platformer.unity", true),
            new EditorBuildSettingsScene(SceneDir + "/GameOver.unity", true),
            new EditorBuildSettingsScene(SceneDir + "/Victory.unity", true)
        };
    }

    // ================================================================== LEVEL1 (Tilemap + prefabs)

    static bool shiftRooms = true;   // escape room: walls sit on cell borders, so x >= 16 is shifted by 0.5
    static float X(float x) { return (shiftRooms && x >= 16f) ? x + 0.5f : x; }

    static void Mod(Object o)
    {
        if (o != null) PrefabUtility.RecordPrefabInstancePropertyModifications(o);
    }

    static Transform Group(string name)
    {
        return new GameObject(name).transform;
    }

    static GameObject Inst(string prefab, Transform parent, float x, float y)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(CoursePrefabFactory.PrefabDir + "/" + prefab + ".prefab");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(X(x), y, 0f);
        return go;
    }

    static void BuildLevelScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PopulateEscapeLevel(true);
        EditorSceneManager.SaveScene(scene, SceneDir + "/Level1.unity");
    }

    /// <summary>
    /// Menu: Escape Room > Build Level Into Current Scene.
    /// Puts the whole level (Tilemap, prefabs, managers) into the scene that is open right now,
    /// keeping whatever is already there (for example your Background). Everything is a real object
    /// in the Hierarchy / Scene view; only the behaviour (movement, hazards, UI...) is code.
    /// </summary>
    [MenuItem("Escape Room/Build Level Into Current Scene")]
    public static void BuildIntoCurrentScene()
    {
        if (Object.FindFirstObjectByType<GameManager>() != null)
        {
            EditorUtility.DisplayDialog("Escape Room",
                "This scene already contains the level (a Game Manager object was found).\n" +
                "Open an empty scene, or delete the old level objects first.", "OK");
            return;
        }

        try
        {
            EditorUtility.DisplayProgressBar("Escape Room", "Preparing sprites, tiles, animations and prefabs...", 0.3f);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CoursePrefabFactory.PrefabDir + "/Player.prefab") == null
                || CourseAssetFactory.LoadSprite("ui_rounded") == null)
            {
                CourseAssetFactory.EnsureFolder(SceneDir);
                CourseAssetFactory.BuildAll();
                CoursePrefabFactory.BuildAll();
            }
            EditorUtility.DisplayProgressBar("Escape Room", "Building the level in the scene...", 0.7f);
            PopulateEscapeLevel(false);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        var scene = EditorSceneManager.GetActiveScene();
        string path = scene.path;
        if (string.IsNullOrEmpty(path))
        {
            CourseAssetFactory.EnsureFolder(SceneDir);
            path = SceneDir + "/MyLevel.unity";        // the scene was never saved
        }
        EditorSceneManager.SaveScene(scene, path);
        AddToBuildSettings(path);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Escape Room",
            "The level is now in the scene \"" + scene.name + "\" (saved, and added to the Build Settings).\n\n" +
            "Look at the Hierarchy: Grid (Floor / Walls Tilemaps), Items, Hazards, Enemies, Doors, Props, Player, HUD Canvas...\n" +
            "Press Play. If your Background hides the level, set its Order in Layer to -20.", "OK");
    }

    static void AddToBuildSettings(string path)
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var scene in list)
            if (scene.path == path) return;
        list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    // Builds the whole escape-room level as real objects in the OPEN scene.
    // sceneFlow = true  : Win / Lose load the Victory / GameOver scenes (full course project)
    // sceneFlow = false : self-contained, Win / Lose show an overlay (works in any single scene)
    static void PopulateEscapeLevel(bool sceneFlow)
    {
        shiftRooms = true;

        // camera: reuse the scene's camera if there is one
        Camera cam = Camera.main;
        GameObject camGO;
        if (cam == null)
        {
            camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            cam = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
        }
        else
        {
            camGO = cam.gameObject;
        }
        camGO.transform.position = new Vector3(8f, 0f, -10f);
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.04f, 0.07f);
        var follow = camGO.GetComponent<CameraFollow>();
        if (follow == null) follow = camGO.AddComponent<CameraFollow>();
        follow.bounds = new Rect(-1f, -6f, 83f, 12f);

        itemsRoot = Group("Items");
        hazardsRoot = Group("Hazards");
        enemiesRoot = Group("Enemies");
        doorsRoot = Group("Doors");
        propsRoot = Group("Props");

        BuildTilemaps();

        var player = Inst("Player", null, 2f, 0f);
        follow.target = player.transform;

        BuildRoom1();
        BuildRoom2();
        BuildRoom3();
        BuildRoom4();
        BuildRoom5();

        // managers
        var gm = new GameObject("Game Manager").AddComponent<GameManager>();
        gm.useSceneFlow = sceneFlow;
        gm.startImmediately = sceneFlow;
        new GameObject("Objective Manager").AddComponent<ObjectiveManager>();
        new GameObject("Audio Manager").AddComponent<AudioManager>();
        var setup = new GameObject("Game Setup").AddComponent<GameSceneSetup>();
        setup.gameManager = gm;

        BuildHudInScene(gm.maxLives);
    }

    // ---- Tilemap: floors + walls painted with Tile assets
    static void BuildTilemaps()
    {
        var gridGO = new GameObject("Grid");
        gridGO.AddComponent<Grid>();

        var floor = MakeTilemap(gridGO.transform, "Floor", -10, false);
        var walls = MakeTilemap(gridGO.transform, "Walls", 0, true);

        var floorTiles = new TileBase[5];
        for (int i = 0; i < 5; i++)
            floorTiles[i] = AssetDatabase.LoadAssetAtPath<TileBase>(CourseAssetFactory.TileDir + "/floor_" + (i + 1) + ".asset");
        var wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(CourseAssetFactory.TileDir + "/wall.asset");

        int[] roomStart = { 0, 17, 33, 49, 65 };
        int[] roomEnd = { 16, 32, 48, 64, 80 };
        for (int r = 0; r < 5; r++)
            for (int x = roomStart[r]; x <= roomEnd[r]; x++)
                for (int y = -5; y <= 4; y++)
                    floor.SetTile(new Vector3Int(x, y, 0), floorTiles[r]);

        // outer walls
        for (int x = -1; x <= 81; x++)
        {
            walls.SetTile(new Vector3Int(x, 5, 0), wallTile);
            walls.SetTile(new Vector3Int(x, -6, 0), wallTile);
        }
        for (int y = -6; y <= 5; y++)
        {
            walls.SetTile(new Vector3Int(-1, y, 0), wallTile);
            walls.SetTile(new Vector3Int(81, y, 0), wallTile);
        }

        // dividers with a 4-tile doorway (rows -2 .. 1)
        int[] dividers = { 16, 32, 48, 64, 76 };
        foreach (int d in dividers)
            for (int y = -5; y <= 4; y++)
            {
                if (y >= -2 && y <= 1) continue;
                walls.SetTile(new Vector3Int(d, y, 0), wallTile);
            }

        // pillars
        Column(walls, wallTile, 5, 1, 4);     // room 1
        Column(walls, wallTile, 9, -5, -2);
        Column(walls, wallTile, 37, -5, -1);  // room 3
        Column(walls, wallTile, 43, 0, 4);
    }

    static Tilemap MakeTilemap(Transform parent, string name, int order, bool solid)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tilemap = go.AddComponent<Tilemap>();
        var renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = order;
        if (solid) go.AddComponent<TilemapCollider2D>();
        return tilemap;
    }

    static void Column(Tilemap map, TileBase tile, int x, int y0, int y1)
    {
        for (int y = y0; y <= y1; y++) map.SetTile(new Vector3Int(x, y, 0), tile);
    }

    // ---- placement helpers
    static void Coins(params Vector2[] positions)
    {
        foreach (var p in positions) Inst("Coin", itemsRoot, p.x, p.y);
    }

    static void Gem(float x, float y) { Inst("Gem", itemsRoot, x, y); }
    static void Shield(float x, float y) { Inst("Shield", itemsRoot, x, y); }
    static void Speed(float x, float y) { Inst("SpeedBoost", itemsRoot, x, y); }

    static void Clock(float x, float y, float seconds)
    {
        var go = Inst("Clock", itemsRoot, x, y);
        var c = go.GetComponent<Collectible>();
        c.amount = seconds;
        Mod(c);
    }

    static void Key(string id, string display, Color color, float x, float y)
    {
        var go = Inst("Key", itemsRoot, x, y);
        var c = go.GetComponent<Collectible>();
        c.keyId = id;
        c.displayName = display;
        Mod(c);
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        sr.color = color;
        Mod(sr);
    }

    static DoorController Door(float x, string name, string doorName, Color color, string key, int gems, float respawnX)
    {
        var go = Inst("Door", doorsRoot, x, 0f);
        go.name = name;
        go.transform.localScale = new Vector3(1f, 4f, 1f);
        var sr = go.GetComponent<SpriteRenderer>();
        sr.color = color;
        var d = go.GetComponent<DoorController>();
        d.doorName = doorName;
        d.requiredKey = key;
        d.requiredGems = gems;
        d.setRespawn = true;
        d.respawnPoint = new Vector2(X(respawnX), 0f);
        Mod(go); Mod(go.transform); Mod(sr); Mod(d);
        return d;
    }

    static void Laser(float x, float phase, float on, float off)
    {
        var go = Inst("Laser", hazardsRoot, x, 0f);
        var h = go.GetComponent<HazardZone>();
        h.phase = phase;
        h.onTime = on;
        h.offTime = off;
        Mod(h);
    }

    static void Saw(float ax, float ay, float bx, float by, float speed, float phase)
    {
        var go = Inst("Saw", hazardsRoot, ax, ay);
        var h = go.GetComponent<HazardZone>();
        h.pointA = new Vector2(X(ax), ay);
        h.pointB = new Vector2(X(bx), by);
        h.patrolSpeed = speed;
        h.phase = phase;
        Mod(h);
    }

    static void Spinner(float x, float y, float angle, float degreesPerSecond)
    {
        var go = Inst("Spinner", hazardsRoot, x, y);
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        var h = go.GetComponent<HazardZone>();
        h.spinSpeed = degreesPerSecond;
        Mod(go.transform);
        Mod(h);
    }

    static void Spikes(float x, float y, float phase)
    {
        var go = Inst("SpikeTile", hazardsRoot, x, y);
        var h = go.GetComponent<HazardZone>();
        h.phase = phase;
        Mod(h);
    }

    static void SentinelAt(float ax, float ay, float bx, float by)
    {
        var go = Inst("Sentinel", enemiesRoot, ax, ay);
        var s = go.GetComponent<Sentinel>();
        s.pointA = new Vector2(X(ax), ay);
        s.pointB = new Vector2(X(bx), by);
        Mod(s);
    }

    static void Note(float x, float y, string message)
    {
        var go = Inst("Note", propsRoot, x, y);
        var n = go.GetComponent<Interactable>();
        n.message = message;
        Mod(n);
    }

    static void Label(string text, float x, float y, float height, Color color)
    {
        var go = new GameObject("Label " + text);
        go.transform.SetParent(propsRoot, false);
        go.transform.position = new Vector3(X(x), y, 0f);

        // a real TextMesh, so the text is visible in the Scene view (WorldLabel only re-applies the settings at play)
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.font = font;
        tm.fontSize = 64;
        tm.characterSize = height * 10f / 64f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            if (font != null && font.material != null) mr.sharedMaterial = font.material;
            mr.sortingOrder = 20;
        }

        var l = go.AddComponent<WorldLabel>();
        l.text = text;
        l.height = height;
        l.color = color;
    }

    // The whole HUD (Canvas, panels, hearts, texts...) as real objects in the scene.
    static void BuildHudInScene(int maxLives)
    {
        CanvasBuilder.SquareOverride = CourseAssetFactory.LoadSprite("white_square");
        CanvasBuilder.CircleOverride = CourseAssetFactory.LoadSprite("white_circle");
        CanvasBuilder.RoundedOverride = CourseAssetFactory.LoadSprite("ui_rounded");
        CanvasBuilder.HeartOverride = CourseAssetFactory.LoadSprite("ui_heart");
        CanvasBuilder.StarOverride = CourseAssetFactory.LoadSprite("star");
        CanvasBuilder.VignetteOverride = CourseAssetFactory.LoadSprite("ui_vignette");
        try
        {
            CanvasBuilder.Build(null, maxLives);
        }
        finally
        {
            CanvasBuilder.ClearOverrides();
        }
    }

    // ---- the five rooms (same layout as the generated version)
    static void BuildRoom1()
    {
        var desk = Inst("Desk", propsRoot, 3f, 3.6f);
        desk.transform.localScale = new Vector3(2.2f, 1f, 1f);
        var deskNote = desk.GetComponent<Interactable>();
        deskNote.message = "A dusty note: \"Take the red key from the shelf. The vault only opens for five gems.\"";
        Mod(desk.transform); Mod(deskNote);

        var shelf = Inst("Shelf", propsRoot, 13.5f, 4.45f);
        shelf.transform.localScale = new Vector3(3.4f, 0.7f, 1f);
        Mod(shelf.transform);

        Label("STORAGE ROOM", 11f, 4.3f, 0.8f, new Color(1f, 1f, 1f, 0.3f));
        Key("red", "red key", RedColor, 13.5f, 3.4f);
        Label("RED KEY", 13.5f, 2.7f, 0.4f, RedColor);
        Gem(8f, -3.6f);
        Speed(12.2f, -3.8f);
        Coins(new Vector2(4f, -2f), new Vector2(4f, -3.2f), new Vector2(7.5f, -0.5f), new Vector2(8.5f, 1f),
              new Vector2(9f, 2.2f), new Vector2(11.5f, 2f), new Vector2(12.5f, 0.5f), new Vector2(14f, -1f),
              new Vector2(14.5f, -3f));

        Door(16f, "Red Door", "red door", RedColor, "red", 0, 17.4f);
        Label("RED DOOR", 16f, 2.6f, 0.4f, Color.white);
    }

    static void BuildRoom2()
    {
        Label("LASER GALLERY", 24f, 4.9f, 0.8f, new Color(1f, 0.6f, 0.7f, 0.4f));
        Note(17.4f, -3.8f, "Scratched on the wall: \"Lasers sleep in rhythm. Saws never do. Watch, then run.\"");
        Shield(17.4f, 3.8f);

        Laser(18.5f, 0f, 1.6f, 2f);
        Saw(21.5f, -4f, 21.5f, 4f, 3.5f, 0f);
        Laser(24.5f, 1.8f, 1.6f, 2f);
        Saw(27.5f, 4f, 27.5f, -4f, 4f, 0.5f);
        Laser(29.5f, 0.9f, 1.4f, 2.2f);

        Gem(20f, 3.8f);
        Gem(26f, -3.8f);
        Clock(23.2f, 0f, 20f);
        Label("+20s", 23.2f, 0.9f, 0.4f, CoinColor);
        Key("blue", "blue key", BlueColor, 30.8f, -3.5f);
        Label("BLUE KEY", 30.8f, -2.8f, 0.4f, BlueColor);
        Coins(new Vector2(19.5f, -1.2f), new Vector2(20.4f, 1.4f), new Vector2(23.2f, 2.4f), new Vector2(23.2f, -2.4f),
              new Vector2(25.6f, 0.2f), new Vector2(25.6f, 2.6f), new Vector2(30.6f, 2.4f), new Vector2(30.6f, 0.4f));

        Door(32f, "Blue Door", "blue door", BlueColor, "blue", 0, 33.4f);
        Label("BLUE DOOR", 32f, 2.6f, 0.4f, Color.white);
    }

    static void BuildRoom3()
    {
        Label("SENTINEL HALL", 40f, 4.9f, 0.8f, new Color(1f, 0.75f, 0.45f, 0.4f));
        Note(33.7f, -3.8f, "\"The sentinels never sleep. But they only chase what they can SEE. Use the pillars.\"");

        SentinelAt(34f, 3.6f, 41f, 3.6f);
        SentinelAt(39f, -3.6f, 46.5f, -3.6f);

        Gem(35.2f, 4.2f);
        Speed(35.6f, -3.8f);
        Key("gold", "gold key", GoldColor, 46f, 3.6f);
        Label("GOLD KEY", 46f, 2.9f, 0.4f, GoldColor);
        Clock(40f, 0.2f, 25f);
        Label("+25s", 40f, 1.1f, 0.4f, CoinColor);
        Coins(new Vector2(34.6f, -1.6f), new Vector2(35.6f, 1.8f), new Vector2(38.4f, 2f), new Vector2(41.6f, -1.9f),
              new Vector2(44.4f, -1.6f), new Vector2(44.6f, 1.8f), new Vector2(46.6f, 0.6f), new Vector2(38.4f, 0.8f));

        Door(48f, "Gold Door", "gold door", GoldColor, "gold", 0, 49.4f);
        Label("GOLD DOOR", 48f, 2.6f, 0.4f, Color.white);
    }

    static void BuildRoom4()
    {
        Label("SPIKE FIELD", 56f, 4.9f, 0.8f, new Color(1f, 0.5f, 0.4f, 0.4f));
        Note(49.4f, 3.8f, "\"Purple pads link two places. A lever on the far side opens the gate.\"");

        float[] xs = { 52f, 55f, 58f };
        float[] ys = { -3.2f, 0f, 3.2f };
        for (int ix = 0; ix < 3; ix++)
            for (int iy = 0; iy < 3; iy++)
                Spikes(xs[ix], ys[iy], (ix + iy * 2) * 0.6f);

        Saw(49.5f, 1.6f, 62.5f, 1.6f, 2.4f, 0f);
        Saw(62.5f, -1.6f, 49.5f, -1.6f, 2.8f, 1.1f);

        Gem(56.5f, 1.6f);

        var padA = Inst("Teleporter", propsRoot, 49.6f, -3.8f).GetComponent<Teleporter>();
        var padB = Inst("Teleporter", propsRoot, 60.4f, 3.8f).GetComponent<Teleporter>();
        padA.partner = padB;
        padB.partner = padA;
        Mod(padA); Mod(padB);
        Label("PAD", 49.6f, -2.9f, 0.35f, PurpleColor);
        Label("PAD", 60.4f, 2.9f, 0.35f, PurpleColor);

        Coins(new Vector2(53.5f, 1.6f), new Vector2(53.5f, -1.6f), new Vector2(56.5f, -1.6f), new Vector2(50f, 0f),
              new Vector2(50f, 2.5f), new Vector2(60.5f, 0.5f), new Vector2(61.8f, 2f), new Vector2(60.5f, -1.5f),
              new Vector2(63f, 0.3f));

        var gate = Door(64f, "Lever Gate", "lever gate", PurpleColor, "", 0, 65.4f);
        gate.leverOnly = true;
        Mod(gate);
        Label("LEVER GATE", 64f, 2.6f, 0.4f, Color.white);

        var lever = Inst("Lever", propsRoot, 62.4f, -3.6f).GetComponent<Lever>();
        lever.door = gate;
        Mod(lever);
        Label("LEVER", 62.4f, -2.7f, 0.4f, new Color(1f, 0.5f, 0.5f));
    }

    static void BuildRoom5()
    {
        Label("VAULT GAUNTLET", 70f, 4.9f, 0.8f, new Color(0.7f, 1f, 0.85f, 0.4f));
        Note(75f, -3.9f, "\"Beams, lasers, saws... and then the vault. Five gems, no excuses.\"");

        Spinner(67.6f, 3.2f, 0f, 120f);
        Spinner(67.6f, -3.2f, 90f, -120f);
        Laser(72f, 0f, 1.2f, 2.4f);
        Saw(73.9f, -4f, 73.9f, 4f, 5f, 0f);

        Shield(65.2f, 0f);
        Clock(72.85f, -3.8f, 30f);
        Label("+30s", 72.85f, -3f, 0.4f, CoinColor);
        Coins(new Vector2(65.8f, 3.2f), new Vector2(69f, 0.1f), new Vector2(70.5f, -4.2f),
              new Vector2(72.85f, 2.5f), new Vector2(75f, 1.2f), new Vector2(75f, -1.2f));

        var exit = Door(76f, "Exit Vault", "exit vault", GreenColor, "", 5, 77f);
        exit.setRespawn = false;
        exit.objectiveId = "exitdoor";
        Mod(exit);
        Label("EXIT VAULT", 76f, 2.6f, 0.4f, Color.white);
        Label("5 GEMS", 76f, -2.6f, 0.4f, GemColor);

        Inst("ExitPortal", propsRoot, 78.3f, 0f);
        Label("EXIT", 78.3f, 1.5f, 0.5f, GreenColor);
    }


    // ================================================================== PLATFORMER (side-scroller with jump)

    static void BuildPlatformerScene()
    {
        shiftRooms = false;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // camera (sky blue)
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.position = new Vector3(8f, 1f, -10f);
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.42f, 0.68f, 0.95f);
        camGO.AddComponent<AudioListener>();
        var follow = camGO.AddComponent<CameraFollow>();
        follow.bounds = new Rect(-1f, -6f, 82f, 24f);

        itemsRoot = Group("Items");
        hazardsRoot = Group("Hazards");
        enemiesRoot = Group("Enemies");
        doorsRoot = Group("Doors");
        propsRoot = Group("Props");

        BuildPlatformerTilemap();

        var player = Inst("PlayerPlat", null, 2f, 1f);
        follow.target = player.transform;

        // ---- hazards: spikes (tag "danger") and a saw moved by an AnimationClip
        Inst("Spike", hazardsRoot, 21.5f, 0.5f);
        Inst("Spike", hazardsRoot, 22.5f, 0.5f);
        Inst("Spike", hazardsRoot, 72.5f, 0.5f);
        Inst("Spike", hazardsRoot, 73.5f, 0.5f);
        Inst("MovingSaw", hazardsRoot, 37.5f, 2f);

        // ---- enemies
        Inst("Walker", enemiesRoot, 30f, 0.45f);
        Inst("Walker", enemiesRoot, 53f, 0.45f);
        Inst("Walker", enemiesRoot, 63f, 0.45f);
        Inst("Walker", enemiesRoot, 76f, 0.45f);

        // ---- props: spring, checkpoints, goal
        Inst("Spring", propsRoot, 66.5f, 0.25f);
        Inst("Checkpoint", propsRoot, 17.5f, 0f);
        Inst("Checkpoint", propsRoot, 41.5f, 0f);
        Inst("Checkpoint", propsRoot, 60.5f, 0f);
        Inst("FlagGoal", propsRoot, 77.5f, 0f);
        Label("START", 2f, 3f, 0.6f, Color.white);
        Label("LEFT / RIGHT  +  SPACE", 6f, 4.2f, 0.5f, new Color(1f, 1f, 1f, 0.8f));
        Label("JUMP ON ENEMIES!", 29f, 5.5f, 0.5f, new Color(1f, 1f, 1f, 0.7f));
        Label("GOAL", 77.5f, 4.2f, 0.7f, new Color(1f, 0.9f, 0.3f));

        // ---- collectibles
        Gem(28.5f, 4.2f);     // on the high platform
        Gem(50.5f, 6.2f);     // top of the staircase
        Gem(69f, 5.2f);       // reached with the spring
        Shield(19.5f, 1.4f);
        Clock(62f, 1.4f, 30f);
        Coins(
            new Vector2(4f, 1.4f), new Vector2(5f, 1.4f), new Vector2(6f, 1.4f), new Vector2(7f, 1.4f), new Vector2(8f, 1.4f),
            new Vector2(14.6f, 2.4f), new Vector2(15.5f, 3f), new Vector2(16.4f, 2.4f),
            new Vector2(21.5f, 2.8f), new Vector2(22.5f, 2.8f), new Vector2(24.5f, 2.4f), new Vector2(25.5f, 2.4f),
            new Vector2(27.5f, 4.2f), new Vector2(29.5f, 4.2f), new Vector2(30.5f, 4.2f),
            new Vector2(34f, 2.6f), new Vector2(35.8f, 2.4f), new Vector2(39.2f, 2.4f), new Vector2(40.5f, 2.4f),
            new Vector2(42f, 1.4f), new Vector2(43f, 1.4f), new Vector2(44.5f, 2.4f), new Vector2(45.5f, 2.4f),
            new Vector2(47.5f, 4.4f), new Vector2(48.5f, 4.4f),
            new Vector2(57.6f, 2.4f), new Vector2(58.5f, 3f), new Vector2(59.4f, 2.4f),
            new Vector2(64f, 1.4f), new Vector2(65f, 1.4f), new Vector2(68f, 5.2f), new Vector2(70f, 5.2f),
            new Vector2(75f, 1.4f), new Vector2(76f, 1.4f));

        // ---- managers
        var gm = new GameObject("Game Manager").AddComponent<GameManager>();
        gm.useSceneFlow = true;
        gm.startImmediately = true;
        gm.trackRooms = false;
        gm.levelWidth = 80f;
        gm.gemsRequired = 3;
        gm.timeLimit = 240f;
        new GameObject("Objective Manager").AddComponent<ObjectiveManager>();
        new GameObject("Audio Manager").AddComponent<AudioManager>();
        var setup = new GameObject("Platformer Setup").AddComponent<PlatformerSetup>();
        setup.gameManager = gm;

        BuildHudInScene(gm.maxLives);

        EditorSceneManager.SaveScene(scene, SceneDir + "/Platformer.unity");
        shiftRooms = true;
    }

    static void BuildPlatformerTilemap()
    {
        var gridGO = new GameObject("Grid");
        gridGO.AddComponent<Grid>();

        var go = new GameObject("Ground");
        go.transform.SetParent(gridGO.transform, false);
        var map = go.AddComponent<Tilemap>();
        var renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = 0;
        var tileCollider = go.AddComponent<TilemapCollider2D>();
        MakeComposite(go, tileCollider);

        var grass = AssetDatabase.LoadAssetAtPath<TileBase>(CourseAssetFactory.TileDir + "/grass.asset");
        var dirt = AssetDatabase.LoadAssetAtPath<TileBase>(CourseAssetFactory.TileDir + "/dirt.asset");

        // ground segments (the gaps between them are pits)
        int[,] ground = { { 0, 13 }, { 17, 32 }, { 41, 56 }, { 60, 79 } };
        for (int i = 0; i < ground.GetLength(0); i++)
        {
            for (int x = ground[i, 0]; x <= ground[i, 1]; x++)
            {
                for (int y = -4; y <= -2; y++) map.SetTile(new Vector3Int(x, y, 0), dirt);
                map.SetTile(new Vector3Int(x, -1, 0), grass);
            }
        }

        // floating platforms: x from, x to, row
        int[,] platforms =
        {
            { 24, 25, 0 }, { 27, 30, 2 },            // steps up to the first gem
            { 35, 36, 0 }, { 38, 39, 0 },            // stepping stones over the saw pit
            { 44, 45, 0 }, { 47, 48, 2 }, { 50, 51, 4 },   // staircase
            { 68, 70, 3 }                            // reached with the spring
        };
        for (int i = 0; i < platforms.GetLength(0); i++)
            for (int x = platforms[i, 0]; x <= platforms[i, 1]; x++)
                map.SetTile(new Vector3Int(x, platforms[i, 2], 0), grass);

        // level borders
        Column(map, dirt, -1, -4, 14);
        Column(map, dirt, 80, -4, 14);
    }

    // merge every tile collider into one smooth shape (no "ghost" snagging on tile seams)
    static void MakeComposite(GameObject go, Collider2D collider)
    {
        try
        {
            var property = typeof(Collider2D).GetProperty("compositeOperation");
            if (property != null)
            {
                property.SetValue(collider, System.Enum.ToObject(property.PropertyType, 1), null);   // 1 = Merge
            }
            else
            {
                var old = typeof(Collider2D).GetProperty("usedByComposite");
                if (old != null) old.SetValue(collider, true, null);
            }
            go.AddComponent<CompositeCollider2D>();
            var body = go.GetComponent<Rigidbody2D>();
            if (body != null) body.bodyType = RigidbodyType2D.Static;
        }
        catch (System.Exception)
        {
            // keep the plain tile colliders
        }
    }

    // ================================================================== UI scenes

    static DefaultControls.Resources UIRes()
    {
        var res = new DefaultControls.Resources();
        res.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        res.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        res.inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
        res.knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        res.checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
        res.dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
        res.mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
        return res;
    }

    static void CreateCamera(Color background)
    {
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.position = new Vector3(0f, 0f, -10f);
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        camGO.AddComponent<AudioListener>();
    }

    static Transform CreateCanvas()
    {
        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var module = es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        module.AssignDefaultActions();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
        return go.transform;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    static void Background(Transform canvas, Color color)
    {
        var bg = DefaultControls.CreatePanel(UIRes());
        bg.name = "Background";
        bg.transform.SetParent(canvas, false);
        var rt = bg.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = bg.GetComponent<Image>();
        img.sprite = null;
        img.color = color;
    }

    static Text UIText(Transform parent, string text, int size, Color color, Vector2 pos, Vector2 sizeDelta)
    {
        var go = DefaultControls.CreateText(UIRes());
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        var t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(3f, -3f);
        return t;
    }

    static Button UIButton(Transform parent, string label, Vector2 pos)
    {
        var go = DefaultControls.CreateButton(UIRes());
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(440f, 90f);
        go.GetComponent<Image>().color = new Color(0.15f, 0.4f, 0.65f, 1f);
        var text = go.GetComponentInChildren<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = label;
        text.fontSize = 46;
        text.color = Color.white;
        return go.GetComponent<Button>();
    }

    static void BuildMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera(new Color(0.04f, 0.06f, 0.11f));
        Transform canvas = CreateCanvas();
        Background(canvas, new Color(0.04f, 0.06f, 0.11f, 1f));

        // main panel
        var main = NewRect("MainPanel", canvas);
        UIText(main, "ESCAPE ROOM", 140, new Color(0.35f, 0.85f, 1f), new Vector2(0f, 330f), new Vector2(1500f, 180f));
        UIText(main, "Find the keys. Dodge the traps. Escape!", 42, new Color(0.8f, 0.85f, 0.95f), new Vector2(0f, 215f), new Vector2(1500f, 70f));
        var play = UIButton(main, "ESCAPE ROOM", new Vector2(0f, 110f));
        var platformer = UIButton(main, "JUMP MODE", new Vector2(0f, 0f));
        var settings = UIButton(main, "SETTINGS", new Vector2(0f, -110f));
        var quit = UIButton(main, "QUIT", new Vector2(0f, -220f));
        var best = UIText(main, "", 38, new Color(1f, 0.88f, 0.4f), new Vector2(0f, -340f), new Vector2(1000f, 60f));
        UIText(main, "WASD / Arrows move   |   Shift sprint   |   E interact   |   Esc pause", 28,
               new Color(0.6f, 0.66f, 0.76f), new Vector2(0f, -440f), new Vector2(1700f, 50f));

        // settings panel (volume slider)
        var panel = NewRect("SettingsPanel", canvas);
        UIText(panel, "SETTINGS", 110, new Color(0.35f, 0.85f, 1f), new Vector2(0f, 300f), new Vector2(1200f, 150f));
        var volumeLabel = UIText(panel, "VOLUME  100%", 56, Color.white, new Vector2(0f, 130f), new Vector2(1000f, 80f));
        var sliderGO = DefaultControls.CreateSlider(UIRes());
        sliderGO.name = "Volume Slider";
        sliderGO.transform.SetParent(panel, false);
        var srt = sliderGO.GetComponent<RectTransform>();
        srt.anchoredPosition = new Vector2(0f, 30f);
        srt.sizeDelta = new Vector2(800f, 50f);
        var slider = sliderGO.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        var back = UIButton(panel, "BACK", new Vector2(0f, -150f));
        panel.gameObject.SetActive(false);

        // background music
        new GameObject("Audio Manager").AddComponent<AudioManager>();

        var menu = new GameObject("Main Menu").AddComponent<MainMenu>();
        menu.playButton = play;
        menu.platformerButton = platformer;
        menu.settingsButton = settings;
        menu.quitButton = quit;
        menu.backButton = back;
        menu.mainPanel = main.gameObject;
        menu.settingsPanel = panel.gameObject;
        menu.volumeSlider = slider;
        menu.volumeLabel = volumeLabel;
        menu.bestText = best;

        EditorSceneManager.SaveScene(scene, SceneDir + "/MainMenu.unity");
    }

    static void BuildEndScene(bool victory)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Color bgColor = victory ? new Color(0.03f, 0.12f, 0.09f, 1f) : new Color(0.14f, 0.03f, 0.05f, 1f);
        CreateCamera(bgColor);
        Transform canvas = CreateCanvas();
        Background(canvas, bgColor);

        var title = UIText(canvas, victory ? "YOU ESCAPED!" : "GAME OVER", 150,
            victory ? new Color(0.5f, 1f, 0.65f) : new Color(1f, 0.35f, 0.4f), new Vector2(0f, 360f), new Vector2(1600f, 190f));

        var stars = new Image[victory ? 3 : 0];
        var starSprite = CourseAssetFactory.LoadSprite("star");
        for (int i = 0; i < stars.Length; i++)
        {
            var go = new GameObject("Star" + i, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = new Vector2((i - 1) * 150f, 205f);
            rt.sizeDelta = new Vector2(120f, 120f);
            var img = go.GetComponent<Image>();
            img.sprite = starSprite;
            img.color = new Color(1f, 0.88f, 0.3f);
            stars[i] = img;
        }

        var body = UIText(canvas, "", 52, Color.white, new Vector2(0f, -10f), new Vector2(1400f, 380f));
        var retry = UIButton(canvas, victory ? "PLAY AGAIN" : "TRY AGAIN", new Vector2(-260f, -320f));
        var menuBtn = UIButton(canvas, "MAIN MENU", new Vector2(260f, -320f));
        UIText(canvas, "R / Space: retry     Q / Esc: main menu", 28, new Color(0.65f, 0.7f, 0.8f), new Vector2(0f, -440f), new Vector2(1400f, 50f));

        new GameObject("Audio Manager").AddComponent<AudioManager>();

        var end = new GameObject("End Screen").AddComponent<EndScreen>();
        end.victory = victory;
        end.titleText = title;
        end.bodyText = body;
        end.stars = stars;
        end.retryButton = retry;
        end.menuButton = menuBtn;

        EditorSceneManager.SaveScene(scene, SceneDir + (victory ? "/Victory.unity" : "/GameOver.unity"));
    }
}
#endif
