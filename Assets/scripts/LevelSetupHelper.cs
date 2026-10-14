using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the complete game at runtime: managers, HUD, five rooms, doors, pickups, hazards,
/// enemies, the player and the camera. Add this to any scene (or use Escape Room > Create Game Scene);
/// it is no longer started automatically: it only runs if you add it to a scene yourself.
///
/// Level layout (x grows to the right, 1 unit = 1 tile, each room is 16 wide):
///   Room 1 (x  0-16) Storage Room    : red key, gem, coins, speed boost
///   Room 2 (x 16-32) Laser Gallery   : lasers + saws, 2 gems, blue key, shield
///   Room 3 (x 32-48) Sentinel Hall   : chasing sentinels, gem, gold key
///   Room 4 (x 48-64) Spike Field     : blinking spikes, saws, teleporters, gem, lever gate
///   Room 5 (x 64-80) Vault Gauntlet  : spinning beams, lasers, saw, gem-locked exit vault + portal
/// </summary>
public class LevelSetupHelper : MonoBehaviour
{
    public static LevelSetupHelper Instance { get; private set; }

    /// <summary>Optional custom background (Assets/Resources/Background.png). See ApplyBackground.</summary>
    public bool backgroundPerRoom = true;

    static Sprite squareSprite;
    static Sprite circleSprite;

    GameObject root;
    Transform levelRoot;
    Font font;

    static readonly Color WallColor = new Color(0.35f, 0.37f, 0.46f);
    static readonly Color RedColor = new Color(0.9f, 0.22f, 0.22f);
    static readonly Color BlueColor = new Color(0.28f, 0.5f, 1f);
    static readonly Color GoldColor = new Color(1f, 0.8f, 0.2f);
    static readonly Color GreenColor = new Color(0.2f, 0.85f, 0.45f);
    static readonly Color PurpleColor = new Color(0.7f, 0.4f, 1f);
    static readonly Color GemColor = new Color(0.25f, 0.95f, 1f);
    static readonly Color CoinColor = new Color(1f, 0.85f, 0.2f);
    static readonly Color WoodColor = new Color(0.45f, 0.3f, 0.18f);
    static readonly Color PaperColor = new Color(0.95f, 0.92f, 0.75f);

    void Awake()
    {
        Instance = this;
        Build();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Rebuild()
    {
        if (root != null)
        {
            root.SetActive(false);
            Destroy(root);
        }
        Build();
    }

    // ------------------------------------------------------------------ build

    void Build()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        root = new GameObject("Game Root");
        root.transform.SetParent(transform, false);

        var gm = Spawn<GameManager>("Game Manager");
        Spawn<ObjectiveManager>("Objective Manager");
        Spawn<AudioManager>("Audio Manager");
        CanvasBuilder.Build(root.transform, gm.maxLives);

        levelRoot = new GameObject("Level").transform;
        levelRoot.SetParent(root.transform, false);
        BuildLevel();
        ApplyBackground();

        PlayerMovement player = BuildPlayer();
        SetupCamera(player.transform);
        gm.Initialize(player);
    }

    T Spawn<T>(string name) where T : Component
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        return go.AddComponent<T>();
    }

    void BuildLevel()
    {
        // ---- floors (one per room)
        Color[] floors =
        {
            new Color(0.16f, 0.18f, 0.25f), new Color(0.2f, 0.15f, 0.22f), new Color(0.2f, 0.17f, 0.13f),
            new Color(0.18f, 0.12f, 0.12f), new Color(0.13f, 0.21f, 0.2f)
        };
        for (int i = 0; i < 5; i++)
            Block("Floor " + (i + 1), new Vector2(8f + 16f * i, 0f), new Vector2(16f, 10f), floors[i], -10, false);

        // ---- outer walls
        Block("Wall Top", new Vector2(40f, 5.5f), new Vector2(82f, 1f), WallColor, 0, true);
        Block("Wall Bottom", new Vector2(40f, -5.5f), new Vector2(82f, 1f), WallColor, 0, true);
        Block("Wall Left", new Vector2(-0.5f, 0f), new Vector2(1f, 12f), WallColor, 0, true);
        Block("Wall Right", new Vector2(80.5f, 0f), new Vector2(1f, 12f), WallColor, 0, true);

        // ---- dividers with a 3-tile doorway (y -1.5 .. 1.5)
        Divider(16f);
        Divider(32f);
        Divider(48f);
        Divider(64f);
        Divider(76f);   // exit vault alcove

        // ---- room labels
        Label("STORAGE ROOM", new Vector2(11f, 4.3f), 0.8f, new Color(1f, 1f, 1f, 0.3f));
        Label("LASER GALLERY", new Vector2(24f, 4.9f), 0.8f, new Color(1f, 0.6f, 0.7f, 0.4f));
        Label("SENTINEL HALL", new Vector2(40f, 4.9f), 0.8f, new Color(1f, 0.75f, 0.45f, 0.4f));
        Label("SPIKE FIELD", new Vector2(56f, 4.9f), 0.8f, new Color(1f, 0.5f, 0.4f, 0.4f));
        Label("VAULT GAUNTLET", new Vector2(70f, 4.9f), 0.8f, new Color(0.7f, 1f, 0.85f, 0.4f));

        BuildRoom1();
        BuildRoom2();
        BuildRoom3();
        BuildRoom4();
        BuildRoom5();
    }

    // ================================================================== ROOM 1
    void BuildRoom1()
    {
        Block("Pillar A", new Vector2(6f, 3f), new Vector2(1f, 4f), WallColor, 0, true);
        Block("Pillar B", new Vector2(10f, -3f), new Vector2(1f, 4f), WallColor, 0, true);

        var desk = Block("Desk", new Vector2(3f, 3.6f), new Vector2(2.2f, 1f), WoodColor, 1, true);
        var note = desk.AddComponent<Interactable>();
        note.prompt = "Read the note on the desk";
        note.message = "A dusty note: \"Take the red key from the shelf. The vault only opens for five gems.\"";

        Block("Shelf", new Vector2(13.5f, 4.45f), new Vector2(3.4f, 0.7f), WoodColor, 1, true);
        Label("SHELF", new Vector2(13.5f, 4.45f), 0.4f, new Color(1f, 1f, 1f, 0.6f));

        MakePickup(CollectibleType.Key, "red", "red key", new Vector2(13.5f, 3.4f), RedColor, false);
        Label("RED KEY", new Vector2(13.5f, 2.7f), 0.4f, RedColor);
        MakePickup(CollectibleType.Gem, "", "gem", new Vector2(8f, -3.6f), GemColor, true);
        MakePickup(CollectibleType.Speed, "", "speed boost", new Vector2(12.2f, -3.8f), new Color(1f, 0.65f, 0.2f), true);

        Coins(new[]
        {
            new Vector2(4f, -2f), new Vector2(4f, -3.2f), new Vector2(7.5f, -0.5f), new Vector2(8.5f, 1f),
            new Vector2(9f, 2.2f), new Vector2(11.5f, 2f), new Vector2(12.5f, 0.5f), new Vector2(14f, -1f),
            new Vector2(14.5f, -3f)
        });

        MakeDoor("Red Door", "red door", 16f, RedColor, "red", 0, new Vector2(17.4f, 0f));
        Label("RED DOOR", new Vector2(16f, 2.4f), 0.4f, Color.white);
    }

    // ================================================================== ROOM 2
    void BuildRoom2()
    {
        AddNote("Wall Note", new Vector2(17.4f, -3.8f), "Scratched on the wall: \"Lasers sleep in rhythm. Saws never do. Watch, then run.\"");
        MakePickup(CollectibleType.Shield, "", "shield", new Vector2(17.4f, 3.8f), new Color(0.3f, 0.9f, 1f), true);

        MakeLaser("Laser 1", 18.5f, 0f, 1.6f, 2.0f);
        MakeSaw("Saw 1", new Vector2(21.5f, -4f), new Vector2(21.5f, 4f), 3.5f, 0f);
        MakeLaser("Laser 2", 24.5f, 1.8f, 1.6f, 2.0f);
        MakeSaw("Saw 2", new Vector2(27.5f, 4f), new Vector2(27.5f, -4f), 4f, 0.5f);
        MakeLaser("Laser 3", 29.5f, 0.9f, 1.4f, 2.2f);

        MakePickup(CollectibleType.Gem, "", "gem", new Vector2(20f, 3.8f), GemColor, true);
        MakePickup(CollectibleType.Gem, "", "gem", new Vector2(26f, -3.8f), GemColor, true);
        MakePickup(CollectibleType.TimeBonus, "", "clock", new Vector2(23.2f, 0f), CoinColor, true, 20f);
        Label("+20s", new Vector2(23.2f, 0.9f), 0.4f, CoinColor);
        MakePickup(CollectibleType.Key, "blue", "blue key", new Vector2(30.8f, -3.5f), BlueColor, false);
        Label("BLUE KEY", new Vector2(30.8f, -2.8f), 0.4f, BlueColor);

        Coins(new[]
        {
            new Vector2(19.5f, -1.2f), new Vector2(20.4f, 1.4f), new Vector2(23.2f, 2.4f), new Vector2(23.2f, -2.4f),
            new Vector2(25.6f, 0.2f), new Vector2(25.6f, 2.6f), new Vector2(30.6f, 2.4f), new Vector2(30.6f, 0.4f)
        });

        MakeDoor("Blue Door", "blue door", 32f, BlueColor, "blue", 0, new Vector2(33.4f, 0f));
        Label("BLUE DOOR", new Vector2(32f, 2.4f), 0.4f, Color.white);
    }

    // ================================================================== ROOM 3
    void BuildRoom3()
    {
        AddNote("Guard Note", new Vector2(33.7f, -3.8f), "\"The sentinels never sleep. But they only chase what they can SEE. Use the pillars.\"");

        Block("Pillar C", new Vector2(37f, -2.5f), new Vector2(1f, 5f), WallColor, 0, true);
        Block("Pillar D", new Vector2(43f, 2.5f), new Vector2(1f, 5f), WallColor, 0, true);

        MakeSentinel("Sentinel 1", new Vector2(34f, 3.6f), new Vector2(41f, 3.6f));
        MakeSentinel("Sentinel 2", new Vector2(39f, -3.6f), new Vector2(46.5f, -3.6f));

        MakePickup(CollectibleType.Gem, "", "gem", new Vector2(35.2f, 4.2f), GemColor, true);
        MakePickup(CollectibleType.Speed, "", "speed boost", new Vector2(35.6f, -3.8f), new Color(1f, 0.65f, 0.2f), true);
        MakePickup(CollectibleType.Key, "gold", "gold key", new Vector2(46f, 3.6f), GoldColor, false);
        Label("GOLD KEY", new Vector2(46f, 2.9f), 0.4f, GoldColor);
        MakePickup(CollectibleType.TimeBonus, "", "clock", new Vector2(40f, 0.2f), CoinColor, true, 25f);
        Label("+25s", new Vector2(40f, 1.1f), 0.4f, CoinColor);

        Coins(new[]
        {
            new Vector2(34.6f, -1.6f), new Vector2(35.6f, 1.8f), new Vector2(38.4f, 2f), new Vector2(41.6f, -1.9f),
            new Vector2(44.4f, -1.6f), new Vector2(44.6f, 1.8f), new Vector2(46.6f, 0.6f), new Vector2(38.4f, 0.8f)
        });

        MakeDoor("Gold Door", "gold door", 48f, GoldColor, "gold", 0, new Vector2(49.4f, 0f));
        Label("GOLD DOOR", new Vector2(48f, 2.4f), 0.4f, Color.white);
    }

    // ================================================================== ROOM 4
    void BuildRoom4()
    {
        AddNote("Pad Note", new Vector2(49.4f, 3.8f), "\"Purple pads link two places. A lever on the far side opens the gate.\"");

        // blinking spike tiles in a 3 x 3 grid with gaps to slip through
        float[] xs = { 52f, 55f, 58f };
        float[] ys = { -3.2f, 0f, 3.2f };
        for (int ix = 0; ix < 3; ix++)
            for (int iy = 0; iy < 3; iy++)
                MakeSpikeTile("Spikes " + ix + "," + iy, new Vector2(xs[ix], ys[iy]), 1.8f, (ix + iy * 2) * 0.6f);

        MakeSaw("Saw H1", new Vector2(49.5f, 1.6f), new Vector2(62.5f, 1.6f), 2.4f, 0f);
        MakeSaw("Saw H2", new Vector2(62.5f, -1.6f), new Vector2(49.5f, -1.6f), 2.8f, 1.1f);

        MakePickup(CollectibleType.Gem, "", "gem", new Vector2(56.5f, 1.6f), GemColor, true);
        MakeTeleporterPair(new Vector2(49.6f, -3.8f), new Vector2(60.4f, 3.8f));
        Label("PAD", new Vector2(49.6f, -2.9f), 0.35f, PurpleColor);
        Label("PAD", new Vector2(60.4f, 2.9f), 0.35f, PurpleColor);

        Coins(new[]
        {
            new Vector2(53.5f, 1.6f), new Vector2(53.5f, -1.6f), new Vector2(56.5f, -1.6f), new Vector2(50f, 0f),
            new Vector2(50f, 2.5f), new Vector2(60.5f, 0.5f), new Vector2(61.8f, 2f), new Vector2(60.5f, -1.5f),
            new Vector2(63f, 0.3f)
        });

        var gate = MakeDoor("Lever Gate", "lever gate", 64f, PurpleColor, "", 0, new Vector2(65.4f, 0f));
        gate.leverOnly = true;
        Label("LEVER GATE", new Vector2(64f, 2.4f), 0.4f, Color.white);
        MakeLever("Gate Lever", new Vector2(62.4f, -3.6f), gate);
        Label("LEVER", new Vector2(62.4f, -2.7f), 0.4f, new Color(1f, 0.5f, 0.5f));
    }

    // ================================================================== ROOM 5
    void BuildRoom5()
    {
        AddNote("Vault Note", new Vector2(75f, -3.9f), "\"Beams, lasers, saws... and then the vault. Five gems, no excuses.\"");

        // two counter-rotating beams guard the entrance, then a laser and a saw
        MakeSpinner("Spinner 1", new Vector2(67.6f, 3.2f), 3.2f, 120f, 0f);
        MakeSpinner("Spinner 2", new Vector2(67.6f, -3.2f), 3.2f, -120f, 90f);
        MakeLaser("Laser 5", 72f, 0f, 1.2f, 2.4f);
        MakeSaw("Saw 5", new Vector2(73.9f, -4f), new Vector2(73.9f, 4f), 5f, 0f);

        MakePickup(CollectibleType.Shield, "", "shield", new Vector2(65.2f, 0f), new Color(0.3f, 0.9f, 1f), true);
        MakePickup(CollectibleType.TimeBonus, "", "clock", new Vector2(72.85f, -3.8f), CoinColor, true, 30f);
        Label("+30s", new Vector2(72.85f, -3f), 0.4f, CoinColor);

        Coins(new[]
        {
            new Vector2(65.8f, 3.2f), new Vector2(69f, 0.1f), new Vector2(70.5f, -4.2f),
            new Vector2(72.85f, 2.5f), new Vector2(75f, 1.2f), new Vector2(75f, -1.2f)
        });

        var exitDoor = MakeDoor("Exit Vault", "exit vault", 76f, GreenColor, "", 5, new Vector2(77f, 0f));
        exitDoor.setRespawn = false;
        exitDoor.objectiveId = "exitdoor";
        Label("EXIT VAULT", new Vector2(76f, 2.4f), 0.4f, Color.white);
        Label("5 GEMS", new Vector2(76f, -2.4f), 0.4f, GemColor);

        var portal = Orb("Exit Portal", new Vector2(78.3f, 0f), 1.8f, GreenColor, 2, true);
        portal.AddComponent<ExitZone>();
        Label("EXIT", new Vector2(78.3f, 1.4f), 0.5f, GreenColor);
    }

    // ------------------------------------------------------------------ player / camera

    PlayerMovement BuildPlayer()
    {
        var go = new GameObject("Player");
        go.tag = "Player";
        go.transform.SetParent(root.transform, false);
        go.transform.position = new Vector3(2f, 0f, 0f);
        go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Circle();
        sr.color = new Color(0.35f, 0.75f, 1f);
        sr.sortingOrder = 10;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        var cc = go.AddComponent<CircleCollider2D>();
        cc.radius = 0.5f;

        var pm = go.AddComponent<PlayerMovement>();
        go.AddComponent<PlayerInteraction>();

        var face = Spr(go.transform, Circle(), Vector2.zero, new Vector2(0.35f, 0.35f), 0f, Color.white, 11);
        pm.face = face.transform;

        var ring = Spr(go.transform, Circle(), Vector2.zero, new Vector2(1.75f, 1.75f), 0f, new Color(0.3f, 0.9f, 1f, 0.35f), 9);
        ring.gameObject.SetActive(false);
        pm.shieldRing = ring.gameObject;

        return pm;
    }

    void SetupCamera(Transform target)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.04f, 0.07f);

        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.target = target;
        follow.Snap();
    }

    // ------------------------------------------------------------------ background image

    /// <summary>
    /// Optional custom background. Put an image at Assets/Resources/Background.png (or .jpg).
    /// With backgroundPerRoom = true it is stretched over each of the 5 rooms; with false it is
    /// scaled once to cover the whole level.
    /// </summary>
    void ApplyBackground()
    {
        Sprite bg = Resources.Load<Sprite>("Background");
        if (bg == null)
        {
            var tex = Resources.Load<Texture2D>("Background");
            if (tex == null) return;   // no image provided: keep the plain colours
            bg = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        foreach (Transform t in levelRoot)
            if (t.name.StartsWith("Floor")) t.gameObject.SetActive(false);

        Vector2 size = bg.bounds.size;
        if (backgroundPerRoom)
        {
            for (int i = 0; i < 5; i++)
                MakeBackground(bg, new Vector2(8f + 16f * i, 0f), new Vector2(16f / size.x, 10f / size.y));
        }
        else
        {
            float s = Mathf.Max(82f / size.x, 12f / size.y);
            MakeBackground(bg, new Vector2(40f, 0f), new Vector2(s, s));
        }
    }

    void MakeBackground(Sprite bg, Vector2 pos, Vector2 scale)
    {
        var go = new GameObject("Background");
        go.transform.SetParent(levelRoot, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = bg;
        sr.sortingOrder = -9;
    }

    // ------------------------------------------------------------------ builders

    void Divider(float x)
    {
        Block("Divider " + x + " (upper)", new Vector2(x, 3.25f), new Vector2(1f, 3.5f), WallColor, 0, true);
        Block("Divider " + x + " (lower)", new Vector2(x, -3.25f), new Vector2(1f, 3.5f), WallColor, 0, true);
    }

    void AddNote(string name, Vector2 pos, string message)
    {
        var go = Block(name, pos, new Vector2(0.6f, 0.7f), PaperColor, 1, false);
        AddTriggerBox(go);
        var n = go.AddComponent<Interactable>();
        n.prompt = "Read the note";
        n.message = message;
    }

    void Coins(Vector2[] positions)
    {
        foreach (var p in positions)
            MakePickup(CollectibleType.Coin, "", "coin", p, CoinColor, true);
    }

    DoorController MakeDoor(string name, string doorName, float x, Color color, string key, int gems, Vector2 respawn)
    {
        var go = Block(name, new Vector2(x, 0f), new Vector2(1f, 3f), color, 3, true);
        var d = go.AddComponent<DoorController>();
        d.doorName = doorName;
        d.requiredKey = key;
        d.requiredGems = gems;
        d.setRespawn = true;
        d.respawnPoint = respawn;
        return d;
    }

    Collectible MakePickup(CollectibleType type, string id, string display, Vector2 pos, Color color, bool auto, float amount = 0f)
    {
        var go = new GameObject(display);
        go.transform.SetParent(levelRoot, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var cc = go.AddComponent<CircleCollider2D>();
        cc.radius = 0.45f;
        cc.isTrigger = true;

        var vis = new GameObject("Visual");
        vis.transform.SetParent(go.transform, false);
        Transform v = vis.transform;

        // soft glow behind every pickup
        Spr(v, Circle(), Vector2.zero, new Vector2(0.95f, 0.95f), 0f, new Color(color.r, color.g, color.b, 0.18f), 4);

        switch (type)
        {
            case CollectibleType.Coin:
                Spr(v, Circle(), Vector2.zero, new Vector2(0.5f, 0.5f), 0f, color, 5);
                Spr(v, Circle(), Vector2.zero, new Vector2(0.28f, 0.28f), 0f, Color.Lerp(color, Color.white, 0.6f), 6);
                break;
            case CollectibleType.Gem:
                Spr(v, Square(), Vector2.zero, new Vector2(0.45f, 0.45f), 45f, color, 5);
                Spr(v, Square(), new Vector2(-0.04f, 0.05f), new Vector2(0.16f, 0.16f), 45f, new Color(1f, 1f, 1f, 0.65f), 6);
                break;
            case CollectibleType.Key:
                Spr(v, Circle(), new Vector2(-0.16f, 0f), new Vector2(0.36f, 0.36f), 0f, color, 5);
                Spr(v, Circle(), new Vector2(-0.16f, 0f), new Vector2(0.14f, 0.14f), 0f, new Color(0.1f, 0.1f, 0.15f), 6);
                Spr(v, Square(), new Vector2(0.12f, 0f), new Vector2(0.4f, 0.1f), 0f, color, 5);
                Spr(v, Square(), new Vector2(0.24f, -0.08f), new Vector2(0.08f, 0.16f), 0f, color, 5);
                break;
            case CollectibleType.TimeBonus:
                Spr(v, Circle(), Vector2.zero, new Vector2(0.6f, 0.6f), 0f, color, 5);
                Spr(v, Square(), new Vector2(0f, 0.07f), new Vector2(0.05f, 0.22f), 0f, new Color(0.2f, 0.15f, 0.05f), 6);
                Spr(v, Square(), new Vector2(0.06f, 0f), new Vector2(0.16f, 0.05f), 0f, new Color(0.2f, 0.15f, 0.05f), 6);
                break;
            case CollectibleType.Shield:
                Spr(v, Circle(), Vector2.zero, new Vector2(0.6f, 0.6f), 0f, color, 5);
                Spr(v, Circle(), Vector2.zero, new Vector2(0.38f, 0.38f), 0f, new Color(0.05f, 0.1f, 0.2f), 6);
                Spr(v, Circle(), Vector2.zero, new Vector2(0.16f, 0.16f), 0f, color, 7);
                break;
            case CollectibleType.Speed:
                Spr(v, Circle(), Vector2.zero, new Vector2(0.6f, 0.6f), 0f, color, 5);
                Spr(v, Square(), new Vector2(-0.04f, 0.07f), new Vector2(0.28f, 0.07f), 30f, Color.white, 6);
                Spr(v, Square(), new Vector2(0.04f, -0.07f), new Vector2(0.28f, 0.07f), 30f, Color.white, 6);
                break;
        }

        var c = go.AddComponent<Collectible>();
        c.type = type;
        c.keyId = id;
        c.displayName = display;
        c.autoPickup = auto;
        c.amount = amount;
        c.visual = v;
        return c;
    }

    HazardZone MakeLaser(string name, float x, float phase, float on, float off)
    {
        var go = Block(name, new Vector2(x, 0f), new Vector2(0.5f, 10f), new Color(1f, 0.15f, 0.25f), 4, false);
        AddTriggerBox(go);
        var h = go.AddComponent<HazardZone>();
        h.blink = true;
        h.onTime = on;
        h.offTime = off;
        h.phase = phase;
        h.reason = "Laser burn!";
        return h;
    }

    HazardZone MakeSaw(string name, Vector2 a, Vector2 b, float speed, float phase)
    {
        var go = Orb(name, a, 1.2f, new Color(0.85f, 0.87f, 0.92f), 4, true);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        var bladeColor = new Color(0.8f, 0.1f, 0.15f);
        Spr(go.transform, Square(), Vector2.zero, new Vector2(0.95f, 0.16f), 0f, bladeColor, 5);
        Spr(go.transform, Square(), Vector2.zero, new Vector2(0.16f, 0.95f), 0f, bladeColor, 5);

        var h = go.AddComponent<HazardZone>();
        h.patrol = true;
        h.pointA = a;
        h.pointB = b;
        h.patrolSpeed = speed;
        h.phase = phase;
        h.spinSpeed = 360f;
        h.reason = "Sliced by a saw!";
        return h;
    }

    // a one-armed beam that rotates around a pivot
    HazardZone MakeSpinner(string name, Vector2 pivot, float length, float degreesPerSecond, float startAngle)
    {
        var go = new GameObject(name);
        go.transform.SetParent(levelRoot, false);
        go.transform.position = new Vector3(pivot.x, pivot.y, 0f);
        go.transform.rotation = Quaternion.Euler(0f, 0f, startAngle);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        var bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.offset = new Vector2(length * 0.5f, 0f);
        bc.size = new Vector2(length, 0.35f);

        Spr(go.transform, Square(), new Vector2(length * 0.5f, 0f), new Vector2(length, 0.3f), 0f, new Color(1f, 0.2f, 0.35f), 4);
        Spr(go.transform, Circle(), Vector2.zero, new Vector2(0.9f, 0.9f), 0f, new Color(0.15f, 0.15f, 0.22f), 5);
        Spr(go.transform, Circle(), Vector2.zero, new Vector2(0.4f, 0.4f), 0f, new Color(1f, 0.2f, 0.35f), 6);

        var h = go.AddComponent<HazardZone>();
        h.spinSpeed = degreesPerSecond;
        h.reason = "Cut by a spinning beam!";
        return h;
    }

    HazardZone MakeSpikeTile(string name, Vector2 pos, float size, float phase)
    {
        var go = Block(name, pos, new Vector2(size, size), new Color(1f, 0.45f, 0.1f), 4, false);
        AddTriggerBox(go);
        Spr(go.transform, Square(), Vector2.zero, new Vector2(0.7f, 0.7f), 0f, new Color(0.3f, 0.07f, 0.02f), 5);
        Spr(go.transform, Square(), Vector2.zero, new Vector2(0.14f, 0.5f), 0f, new Color(1f, 0.6f, 0.2f), 6);
        Spr(go.transform, Square(), Vector2.zero, new Vector2(0.5f, 0.14f), 0f, new Color(1f, 0.6f, 0.2f), 6);

        var h = go.AddComponent<HazardZone>();
        h.blink = true;
        h.onTime = 1.1f;
        h.offTime = 2.0f;
        h.phase = phase;
        h.reason = "Impaled by spikes!";
        return h;
    }

    Sentinel MakeSentinel(string name, Vector2 a, Vector2 b)
    {
        var go = new GameObject(name);
        go.transform.SetParent(levelRoot, false);
        go.transform.position = new Vector3(a.x, a.y, 0f);
        go.transform.localScale = new Vector3(1.05f, 1.05f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Circle();
        sr.color = new Color(1f, 0.6f, 0.2f);
        sr.sortingOrder = 8;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.mass = 50f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        var cc = go.AddComponent<CircleCollider2D>();
        cc.radius = 0.5f;

        Spr(go.transform, Circle(), Vector2.zero, new Vector2(0.55f, 0.55f), 0f, Color.white, 9);
        var pupil = Spr(go.transform, Circle(), Vector2.zero, new Vector2(0.25f, 0.25f), 0f, new Color(0.05f, 0.05f, 0.1f), 10);

        var s = go.AddComponent<Sentinel>();
        s.pointA = a;
        s.pointB = b;
        s.pupil = pupil.transform;
        return s;
    }

    void MakeTeleporterPair(Vector2 a, Vector2 b)
    {
        var ta = MakeTeleporter("Teleporter A", a);
        var tb = MakeTeleporter("Teleporter B", b);
        ta.partner = tb;
        tb.partner = ta;
    }

    Teleporter MakeTeleporter(string name, Vector2 pos)
    {
        var go = Orb(name, pos, 1.3f, PurpleColor, 2, true);
        Spr(go.transform, Circle(), Vector2.zero, new Vector2(0.6f, 0.6f), 0f, new Color(0.25f, 0.12f, 0.45f), 3);
        return go.AddComponent<Teleporter>();
    }

    Lever MakeLever(string name, Vector2 pos, DoorController door)
    {
        var go = new GameObject(name);
        go.transform.SetParent(levelRoot, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        Spr(go.transform, Square(), new Vector2(0f, -0.15f), new Vector2(0.9f, 0.3f), 0f, new Color(0.3f, 0.32f, 0.4f), 5);

        var handle = new GameObject("Handle");
        handle.transform.SetParent(go.transform, false);
        handle.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        handle.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
        Spr(handle.transform, Square(), new Vector2(0f, 0.3f), new Vector2(0.1f, 0.6f), 0f, new Color(0.8f, 0.8f, 0.85f), 6);
        Spr(handle.transform, Circle(), new Vector2(0f, 0.62f), new Vector2(0.28f, 0.28f), 0f, new Color(1f, 0.3f, 0.3f), 7);

        var bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(1f, 1f);

        var l = go.AddComponent<Lever>();
        l.door = door;
        l.handle = handle.transform;
        return l;
    }

    // ------------------------------------------------------------------ primitives

    SpriteRenderer Spr(Transform parent, Sprite sprite, Vector2 localPos, Vector2 scale, float rotZ, Color color, int order)
    {
        var go = new GameObject("S");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    GameObject Block(string name, Vector2 pos, Vector2 size, Color color, int order, bool solid)
    {
        var go = new GameObject(name);
        go.transform.SetParent(levelRoot, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Square();
        sr.color = color;
        sr.sortingOrder = order;
        if (solid)
        {
            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = Vector2.one;
        }
        return go;
    }

    GameObject Orb(string name, Vector2 pos, float diameter, Color color, int order, bool trigger)
    {
        var go = new GameObject(name);
        go.transform.SetParent(levelRoot, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(diameter, diameter, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Circle();
        sr.color = color;
        sr.sortingOrder = order;
        var cc = go.AddComponent<CircleCollider2D>();
        cc.radius = 0.5f;
        cc.isTrigger = trigger;
        return go;
    }

    static void AddTriggerBox(GameObject go)
    {
        var bc = go.AddComponent<BoxCollider2D>();
        bc.size = Vector2.one;
        bc.isTrigger = true;
    }

    void Label(string text, Vector2 pos, float height, Color color)
    {
        var go = new GameObject("Label " + text);
        go.transform.SetParent(levelRoot, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.font = font;
        tm.fontSize = 64;
        tm.characterSize = height * 10f / 64f;   // roughly 'height' world units tall
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = font.material;
        mr.sortingOrder = 20;
    }

    // ------------------------------------------------------------------ sprites

    public static Sprite Square()
    {
        if (squareSprite == null)
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            squareSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }
        return squareSprite;
    }

    public static Sprite Circle()
    {
        if (circleSprite == null)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    byte a = (byte)(Mathf.Clamp01(size * 0.5f - d) * 255f);
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
        return circleSprite;
    }
}
