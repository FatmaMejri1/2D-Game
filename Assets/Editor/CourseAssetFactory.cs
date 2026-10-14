#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Generates the art assets for the course project: sprite PNGs (imported as Sprites),
/// Tile assets for the Tilemap, AnimationClips and Animator Controllers.
/// </summary>
public static class CourseAssetFactory
{
    public const string SpriteDir = "Assets/Sprites";
    public const string TileDir = "Assets/Tiles";
    public const string AnimDir = "Assets/Animations";

    public static readonly Color[] FloorColors =
    {
        new Color(0.16f, 0.18f, 0.25f), new Color(0.2f, 0.15f, 0.22f), new Color(0.2f, 0.17f, 0.13f),
        new Color(0.18f, 0.12f, 0.12f), new Color(0.13f, 0.21f, 0.2f)
    };

    public static void BuildAll()
    {
        EnsureFolder(SpriteDir);
        EnsureFolder(TileDir);
        EnsureFolder(AnimDir);
        BuildSprites();
        BuildPlatformerSprites();
        BuildUiSprites();
        BuildTiles();
        BuildAnimations();
    }

    public static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "/" + name + ".png");
    }

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    // ------------------------------------------------------------------ sprites

    static void BuildSprites()
    {
        // plain white shapes (tinted in the editor / by scripts)
        var sq = NewTex(4, 4);
        Box(sq, 0, 0, 4, 4, Color.white);
        Save("white_square", sq, 4f, FilterMode.Point);

        var ci = NewTex(64, 64);
        Disc(ci, 32f, 32f, 32f, Color.white);
        Save("white_circle", ci, 64f, FilterMode.Bilinear);

        // player: 2 idle frames (breathing) + 4 walk frames
        DrawPlayer("player_idle_0", 0, 0, 0);
        DrawPlayer("player_idle_1", 1, 0, 0);
        DrawPlayer("player_walk_0", 0, 2, 0);
        DrawPlayer("player_walk_1", 1, 0, 0);
        DrawPlayer("player_walk_2", 0, 0, 2);
        DrawPlayer("player_walk_3", 1, 0, 0);

        // sentinel: 2 frames
        DrawSentinel("sentinel_0", 0f);
        DrawSentinel("sentinel_1", 1.5f);

        // coin spin: 4 frames, gem shine: 2 frames
        DrawCoin("coin_0", 6.5f);
        DrawCoin("coin_1", 4f);
        DrawCoin("coin_2", 1.5f);
        DrawCoin("coin_3", 4f);
        DrawGem("gem_0", false);
        DrawGem("gem_1", true);

        // other pickups
        var key = NewTex(16, 16);
        Disc(key, 5f, 8f, 4.2f, Color.white);
        Erase(key, 5f, 8f, 1.8f);
        Box(key, 8, 7, 15, 9, Color.white);
        Box(key, 12, 4, 14, 7, Color.white);
        Box(key, 9, 5, 10, 7, Color.white);
        Save("key", key, 32f, FilterMode.Point);

        var clock = NewTex(16, 16);
        Disc(clock, 8f, 8f, 7.5f, new Color(0.6f, 0.4f, 0.05f));
        Disc(clock, 8f, 8f, 6.5f, new Color(1f, 0.85f, 0.2f));
        Box(clock, 8, 8, 9, 13, new Color(0.25f, 0.18f, 0.05f));
        Box(clock, 8, 8, 12, 9, new Color(0.25f, 0.18f, 0.05f));
        Save("clock", clock, 32f, FilterMode.Point);

        var shield = NewTex(16, 16);
        Disc(shield, 8f, 8f, 7.5f, new Color(0.15f, 0.55f, 0.7f));
        Disc(shield, 8f, 8f, 6.5f, new Color(0.3f, 0.9f, 1f));
        Disc(shield, 8f, 8f, 4.5f, new Color(0.05f, 0.1f, 0.2f));
        Disc(shield, 8f, 8f, 2f, new Color(0.3f, 0.9f, 1f));
        Save("shield", shield, 32f, FilterMode.Point);

        var speed = NewTex(16, 16);
        Disc(speed, 8f, 8f, 7.5f, new Color(0.7f, 0.35f, 0.05f));
        Disc(speed, 8f, 8f, 6.5f, new Color(1f, 0.65f, 0.2f));
        Box(speed, 7, 9, 10, 12, Color.white);
        Box(speed, 6, 7, 9, 10, Color.white);
        Box(speed, 5, 4, 8, 7, Color.white);
        Save("speed", speed, 32f, FilterMode.Point);

        // saw (spins in the game)
        var saw = NewTex(32, 32);
        Disc(saw, 16f, 16f, 15.5f, new Color(0.85f, 0.87f, 0.92f));
        Box(saw, 3, 14, 29, 18, new Color(0.8f, 0.1f, 0.15f));
        Box(saw, 14, 3, 18, 29, new Color(0.8f, 0.1f, 0.15f));
        Disc(saw, 16f, 16f, 4.5f, new Color(0.15f, 0.15f, 0.22f));
        Disc(saw, 16f, 16f, 2f, new Color(0.8f, 0.8f, 0.85f));
        Save("saw", saw, 32f, FilterMode.Point);

        // teleporter pad
        var tp = NewTex(40, 40);
        Disc(tp, 20f, 20f, 19.5f, new Color(0.45f, 0.2f, 0.75f));
        Disc(tp, 20f, 20f, 17f, new Color(0.7f, 0.4f, 1f));
        Disc(tp, 20f, 20f, 11f, new Color(0.25f, 0.12f, 0.45f));
        Disc(tp, 20f, 20f, 6f, new Color(0.75f, 0.5f, 1f));
        Save("teleporter", tp, 32f, FilterMode.Point);

        // spike tile
        var sp = NewTex(32, 32);
        Box(sp, 0, 0, 32, 32, new Color(1f, 0.45f, 0.1f));
        Box(sp, 2, 2, 30, 30, new Color(0.3f, 0.07f, 0.02f));
        int[] pts = { 8, 16, 24 };
        foreach (int gx in pts)
            foreach (int gy in pts)
                Triangle(sp, gx, gy - 4f, 3f, 8f, new Color(1f, 0.6f, 0.2f));
        Save("spike_tile", sp, 32f, FilterMode.Point);

        // paper note
        var paper = NewTex(8, 10);
        Box(paper, 0, 0, 8, 10, Color.white);
        Box(paper, 1, 2, 7, 3, new Color(0.65f, 0.65f, 0.65f));
        Box(paper, 1, 4, 7, 5, new Color(0.65f, 0.65f, 0.65f));
        Box(paper, 1, 6, 7, 7, new Color(0.65f, 0.65f, 0.65f));
        Save("paper", paper, 16f, FilterMode.Point);

        // exit portal
        var portal = NewTex(64, 64);
        Disc(portal, 32f, 32f, 31.5f, new Color(0.2f, 0.85f, 0.45f, 0.35f));
        Disc(portal, 32f, 32f, 26f, new Color(0.2f, 0.85f, 0.45f, 0.6f));
        Disc(portal, 32f, 32f, 18f, new Color(0.1f, 0.5f, 0.3f, 0.9f));
        Disc(portal, 32f, 32f, 10f, new Color(0.6f, 1f, 0.75f, 0.9f));
        Save("portal", portal, 64f, FilterMode.Bilinear);

        // door (white so it can be tinted red / blue / gold ...)
        var door = NewTex(32, 32);
        Box(door, 0, 0, 32, 32, Color.white);
        Box(door, 7, 0, 8, 32, new Color(0.72f, 0.72f, 0.72f));
        Box(door, 15, 0, 16, 32, new Color(0.72f, 0.72f, 0.72f));
        Box(door, 23, 0, 24, 32, new Color(0.72f, 0.72f, 0.72f));
        Box(door, 0, 0, 32, 2, new Color(0.65f, 0.65f, 0.65f));
        Box(door, 0, 30, 32, 32, new Color(0.65f, 0.65f, 0.65f));
        Disc(door, 27f, 16f, 2f, new Color(0.3f, 0.3f, 0.3f));
        Save("door", door, 32f, FilterMode.Point);

        // floor tiles (one per room) and the wall tile
        for (int i = 0; i < FloorColors.Length; i++)
            DrawFloor("floor_" + (i + 1), FloorColors[i], 100 + i);
        DrawWall("wall");

        // star used by the Victory scene
        DrawStar("star");
    }

    static void DrawPlayer(string name, int bodyUp, int footL, int footR)
    {
        Save(name, PlayerTex(bodyUp, footL, footR), 32f, FilterMode.Point);
    }

    static Texture2D PlayerTex(int bodyUp, int footL, int footR)
    {
        var t = NewTex(32, 32);
        Color dark = new Color(0.1f, 0.22f, 0.5f);
        Oval(t, 16f, 4f, 9f, 3f, new Color(0f, 0f, 0f, 0.25f));
        Oval(t, 11f, 5f + footL, 4f, 3f, dark);
        Oval(t, 21f, 5f + footR, 4f, 3f, dark);
        Disc(t, 16f, 15f + bodyUp, 11.5f, new Color(0.1f, 0.25f, 0.55f));
        Disc(t, 16f, 15f + bodyUp, 10f, new Color(0.35f, 0.75f, 1f));
        Disc(t, 12.5f, 19.5f + bodyUp, 3.5f, new Color(0.75f, 0.92f, 1f, 0.7f));
        Disc(t, 12f, 17f + bodyUp, 2.8f, Color.white);
        Disc(t, 20f, 17f + bodyUp, 2.8f, Color.white);
        Disc(t, 12.5f, 17f + bodyUp, 1.4f, new Color(0.05f, 0.05f, 0.1f));
        Disc(t, 20.5f, 17f + bodyUp, 1.4f, new Color(0.05f, 0.05f, 0.1f));
        return t;
    }

    static void DrawSentinel(string name, float spike)
    {
        var t = NewTex(32, 32);
        for (int k = 0; k < 4; k++)
        {
            float ang = k * Mathf.PI / 2f + Mathf.PI / 4f;
            Disc(t, 16f + Mathf.Cos(ang) * (12f + spike), 16f + Mathf.Sin(ang) * (12f + spike), 2.2f, new Color(0.6f, 0.6f, 0.65f));
        }
        Disc(t, 16f, 16f, 12.5f, new Color(0.3f, 0.3f, 0.36f));
        Disc(t, 16f, 16f, 11f, Color.white);      // white so scripts can tint it orange / red
        Disc(t, 16f, 16f, 6f, new Color(0.12f, 0.12f, 0.18f));
        Disc(t, 16f, 16f, 4.2f, new Color(0.95f, 0.95f, 0.95f));
        Disc(t, 16f, 16f, 2f, new Color(0.1f, 0.1f, 0.15f));
        Save(name, t, 32f, FilterMode.Point);
    }

    static void DrawCoin(string name, float rx)
    {
        var t = NewTex(16, 16);
        Oval(t, 8f, 8f, rx, 7f, new Color(0.8f, 0.55f, 0.05f));
        Oval(t, 8f, 8f, Mathf.Max(0.6f, rx - 1f), 6f, new Color(1f, 0.85f, 0.2f));
        if (rx > 3f) Oval(t, 7f, 10f, Mathf.Max(0.6f, rx - 3f), 2.5f, new Color(1f, 0.95f, 0.6f, 0.9f));
        Save(name, t, 32f, FilterMode.Point);
    }

    static void DrawGem(string name, bool spark)
    {
        var t = NewTex(16, 16);
        Diamond(t, 8f, 8f, 7.5f, new Color(0.1f, 0.55f, 0.7f));
        Diamond(t, 8f, 8f, 6f, new Color(0.25f, 0.95f, 1f));
        Diamond(t, 6.5f, 9.5f, 2f, new Color(1f, 1f, 1f, 0.7f));
        if (spark)
        {
            Box(t, 11, 11, 12, 14, Color.white);
            Box(t, 10, 12, 13, 13, Color.white);
        }
        Save(name, t, 32f, FilterMode.Point);
    }

    static void DrawFloor(string name, Color baseColor, int seed)
    {
        var t = NewTex(32, 32);
        var rnd = new System.Random(seed);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float k = 1f;
                if (x == 0 || y == 0) k = 0.82f;
                else if (rnd.NextDouble() < 0.08) k = rnd.NextDouble() < 0.5 ? 0.9f : 1.12f;
                t.SetPixel(x, y, new Color(baseColor.r * k, baseColor.g * k, baseColor.b * k, 1f));
            }
        }
        Save(name, t, 32f, FilterMode.Point);
    }

    static void DrawWall(string name)
    {
        var t = NewTex(32, 32);
        Color baseColor = new Color(0.35f, 0.37f, 0.46f);
        Color mortar = new Color(0.2f, 0.21f, 0.28f);
        for (int y = 0; y < 32; y++)
        {
            int row = y / 8;
            int shift = (row % 2 == 0) ? 0 : 8;
            for (int x = 0; x < 32; x++)
            {
                bool joint = (y % 8 == 0) || ((x + shift) % 16 == 0);
                Color c = joint ? mortar : baseColor;
                if (!joint && y % 8 == 7) c = new Color(0.42f, 0.44f, 0.54f);   // brick highlight
                t.SetPixel(x, y, c);
            }
        }
        Save(name, t, 32f, FilterMode.Point);
    }

    static void DrawStar(string name)
    {
        var poly = new Vector2[10];
        for (int k = 0; k < 10; k++)
        {
            float ang = Mathf.PI / 2f + k * Mathf.PI / 5f;
            float r = (k % 2 == 0) ? 30f : 12f;
            poly[k] = new Vector2(31.5f + Mathf.Cos(ang) * r, 33f + Mathf.Sin(ang) * r);
        }
        var t = NewTex(64, 64);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                int hit = 0;
                for (int sy = 0; sy < 2; sy++)
                    for (int sx = 0; sx < 2; sx++)
                        if (InPolygon(poly, x + 0.25f + sx * 0.5f, y + 0.25f + sy * 0.5f)) hit++;
                if (hit > 0) t.SetPixel(x, y, new Color(1f, 1f, 1f, hit / 4f));
            }
        }
        Save(name, t, 100f, FilterMode.Bilinear);
    }

    static bool InPolygon(Vector2[] poly, float px, float py)
    {
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if (((poly[i].y > py) != (poly[j].y > py)) &&
                (px < (poly[j].x - poly[i].x) * (py - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                inside = !inside;
        }
        return inside;
    }

    // ------------------------------------------------------------------ HUD sprites (so the HUD can live in the scene)

    static void BuildUiSprites()
    {
        // rounded panel, 9-sliced (border 20)
        var rounded = NewTex(64, 64);
        const float radius = 18f;
        float c = 31.5f;
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x - c) - (c - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y - c) - (c - radius), 0f);
                float d = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                rounded.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d)));
            }
        }
        Save("ui_rounded", rounded, 100f, FilterMode.Bilinear);
        var importer = AssetImporter.GetAtPath(SpriteDir + "/ui_rounded.png") as TextureImporter;
        if (importer != null)
        {
            importer.spriteBorder = new Vector4(20f, 20f, 20f, 20f);
            importer.SaveAndReimport();
        }

        // heart
        var heart = NewTex(64, 64);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                int hit = 0;
                for (int sy = 0; sy < 2; sy++)
                {
                    for (int sx = 0; sx < 2; sx++)
                    {
                        float u = (x + 0.25f + sx * 0.5f - 31.5f) / 22f;
                        float v = (y + 0.25f + sy * 0.5f - 30f) / 22f;
                        float a = u * u + v * v - 1f;
                        if (a * a * a - u * u * v * v * v <= 0f) hit++;
                    }
                }
                if (hit > 0) heart.SetPixel(x, y, new Color(1f, 1f, 1f, hit / 4f));
            }
        }
        Save("ui_heart", heart, 100f, FilterMode.Bilinear);

        // vignette (dark corners)
        var vignette = NewTex(128, 128);
        float vc = 63.5f;
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float dx = (x - vc) / vc;
                float dy = (y - vc) / vc;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((d - 0.55f) / 0.65f);
                vignette.SetPixel(x, y, new Color(0f, 0f, 0f, a * a));
            }
        }
        Save("ui_vignette", vignette, 100f, FilterMode.Bilinear);
    }

    // ------------------------------------------------------------------ platformer assets

    /// <summary>True when the player sheet was sliced automatically; otherwise slice it by hand (Sprite Editor).</summary>
    public static bool SheetSliced { get; private set; }

    public static Sprite SheetSprite(int index)
    {
        var all = AssetDatabase.LoadAllAssetsAtPath(SpriteDir + "/player_sheet.png");
        string wanted = "player_sheet_" + index;
        foreach (var o in all)
        {
            var sprite = o as Sprite;
            if (sprite != null && sprite.name == wanted) return sprite;
        }
        return null;
    }

    /// <summary>Sheet frame if available, otherwise the single-image fallback.</summary>
    public static Sprite PlatformerFrame(int index, string fallback)
    {
        var sprite = SheetSprite(index);
        return sprite != null ? sprite : LoadSprite(fallback);
    }

    static void BuildPlatformerSprites()
    {
        // single frames (fallback when the sheet is not sliced yet)
        DrawPlayer("player_jump", 2, 3, 3);

        // the sprite sheet, as in the course: imported in mode "Multiple", cut with the Sprite Editor
        BuildPlayerSheet();

        DrawGround("grass", true);
        DrawGround("dirt", false);
        DrawSpikeUp("spike_up");
        DrawFlag("flag");
        DrawSpring("spring");
        DrawWalker("walker_0", 0f);
        DrawWalker("walker_1", 1.5f);
    }

    // 4 columns x 2 rows of 32 x 32 frames:
    //   top row    : idle 0, idle 1, walk 0, walk 1
    //   bottom row : walk 2, walk 3, jump, fall
    static void BuildPlayerSheet()
    {
        const int cell = 32;
        const int cols = 4;
        const int rows = 2;
        int[,] poses = { { 0, 0, 0 }, { 1, 0, 0 }, { 0, 2, 0 }, { 1, 0, 0 }, { 0, 0, 2 }, { 1, 0, 0 }, { 2, 3, 3 }, { 0, -1, -1 } };

        var sheet = NewTex(cell * cols, cell * rows);
        for (int i = 0; i < 8; i++)
        {
            var frame = PlayerTex(poses[i, 0], poses[i, 1], poses[i, 2]);
            int col = i % cols;
            int row = i / cols;                       // row 0 = top row
            sheet.SetPixels(col * cell, (rows - 1 - row) * cell, cell, cell, frame.GetPixels());
            Object.DestroyImmediate(frame);
        }
        SaveSheet("player_sheet", sheet, 32f, cell, cols, rows);
    }

    static void SaveSheet(string name, Texture2D tex, float pixelsPerUnit, int cell, int cols, int rows)
    {
        tex.Apply();
        string path = SpriteDir + "/" + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;     // the course: "mode Multiple"
        importer.spritePixelsPerUnit = pixelsPerUnit;              // 32 pixels per unit
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        // Try to slice it automatically (the same result as Sprite Editor > Slice > Grid By Cell Size).
        // This uses reflection so the project still compiles if Unity changes that API; if it fails
        // the sheet simply stays un-sliced and you can slice it yourself in the Sprite Editor.
        TrySlice(importer, name, cell, cols, rows);
        importer.SaveAndReimport();

        SheetSliced = SheetSprite(0) != null;
    }

    static void TrySlice(TextureImporter importer, string name, int cell, int cols, int rows)
    {
        try
        {
            var property = typeof(TextureImporter).GetProperty("spritesheet");
            if (property == null) return;

            System.Type metaType = null;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                metaType = assembly.GetType("UnityEditor.SpriteMetaData");
                if (metaType != null) break;
            }
            if (metaType == null) return;

            var array = System.Array.CreateInstance(metaType, cols * rows);
            int index = 0;
            for (int row = rows - 1; row >= 0; row--)       // top row first
            {
                for (int col = 0; col < cols; col++)
                {
                    object meta = System.Activator.CreateInstance(metaType);
                    metaType.GetField("name").SetValue(meta, name + "_" + index);
                    metaType.GetField("rect").SetValue(meta, new Rect(col * cell, row * cell, cell, cell));
                    metaType.GetField("alignment").SetValue(meta, 0);                       // SpriteAlignment.Center
                    metaType.GetField("pivot").SetValue(meta, new Vector2(0.5f, 0.5f));
                    array.SetValue(meta, index);
                    index++;
                }
            }
            property.SetValue(importer, array, null);
        }
        catch (System.Exception)
        {
            // leave the sheet un-sliced
        }
    }

    static void DrawGround(string name, bool grassTop)
    {
        var t = NewTex(32, 32);
        var rnd = new System.Random(grassTop ? 7 : 8);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float k = 1f;
                if (rnd.NextDouble() < 0.1) k = rnd.NextDouble() < 0.5 ? 0.85f : 1.15f;
                t.SetPixel(x, y, new Color(0.45f * k, 0.3f * k, 0.18f * k, 1f));
            }
        }
        if (grassTop)
        {
            for (int x = 0; x < 32; x++)
            {
                int depth = 6 + (x * 7) % 3;
                for (int y = 31; y > 31 - depth; y--)
                {
                    bool edge = y == 31 - depth + 1;
                    t.SetPixel(x, y, edge ? new Color(0.2f, 0.5f, 0.15f) : new Color(0.3f, 0.72f, 0.25f));
                }
            }
        }
        Save(name, t, 32f, FilterMode.Point);
    }

    static void DrawSpikeUp(string name)
    {
        var t = NewTex(32, 32);
        Box(t, 0, 0, 32, 4, new Color(0.3f, 0.3f, 0.36f));
        int[] xs = { 7, 16, 25 };
        foreach (int cx in xs)
        {
            Triangle(t, cx, 3f, 6f, 17f, new Color(0.55f, 0.57f, 0.65f));
            Triangle(t, cx + 1.5f, 3f, 3f, 15f, new Color(0.82f, 0.85f, 0.93f));
        }
        Save(name, t, 32f, FilterMode.Point);
    }

    // white flag so it can be tinted (grey = not reached, green = checkpoint, gold = goal)
    static void DrawFlag(string name)
    {
        var t = NewTex(32, 64);
        Box(t, 14, 0, 17, 62, new Color(0.7f, 0.7f, 0.74f));
        Disc(t, 15.5f, 61f, 2.8f, new Color(1f, 0.85f, 0.2f));
        Box(t, 17, 36, 30, 58, Color.white);
        Box(t, 17, 36, 30, 38, new Color(0.8f, 0.8f, 0.8f));
        Save(name, t, 32f, FilterMode.Point);
    }

    static void DrawSpring(string name)
    {
        var t = NewTex(32, 16);
        Box(t, 2, 0, 30, 4, new Color(0.35f, 0.35f, 0.4f));
        Box(t, 6, 4, 26, 6, new Color(0.8f, 0.8f, 0.85f));
        Box(t, 8, 7, 24, 9, new Color(0.8f, 0.8f, 0.85f));
        Box(t, 3, 10, 29, 14, new Color(0.9f, 0.2f, 0.2f));
        Box(t, 3, 13, 29, 14, new Color(1f, 0.5f, 0.5f));
        Save(name, t, 32f, FilterMode.Point);
    }

    static void DrawWalker(string name, float squash)
    {
        var t = NewTex(32, 32);
        Oval(t, 16f, 12f, 13f + squash, 10f - squash, new Color(0.12f, 0.45f, 0.18f));
        Oval(t, 16f, 12f, 11.5f + squash, 8.5f - squash, new Color(0.3f, 0.85f, 0.35f));
        Oval(t, 12f, 15f, 3f, 2f, new Color(0.7f, 1f, 0.75f, 0.6f));
        Disc(t, 11f, 14f, 3.2f, Color.white);
        Disc(t, 21f, 14f, 3.2f, Color.white);
        Disc(t, 11.5f, 13.5f, 1.5f, new Color(0.05f, 0.05f, 0.1f));
        Disc(t, 21.5f, 13.5f, 1.5f, new Color(0.05f, 0.05f, 0.1f));
        Save(name, t, 32f, FilterMode.Point);
    }

    static void BuildPlatformerAnimations()
    {
        string[] fallback = { "player_idle_0", "player_idle_1", "player_walk_0", "player_walk_1", "player_walk_2", "player_walk_3", "player_jump", "player_jump" };
        var f = new Sprite[8];
        for (int i = 0; i < 8; i++) f[i] = PlatformerFrame(i, fallback[i]);

        var idle = MakeClipFromSprites("plat_idle", new[] { f[0], f[1] }, 3f);
        var walk = MakeClipFromSprites("plat_walk", new[] { f[2], f[3], f[4], f[5] }, 8f);
        var jump = MakeClipFromSprites("plat_jump", new[] { f[6] }, 1f);
        MakePlatformerController(idle, walk, jump);

        var walker = MakeClip("walker_walk", new[] { "walker_0", "walker_1" }, 4f);
        MakeSingleStateController("Walker", "Walk", walker);

        MakeMovingSaw();
    }

    static AnimationClip MakeClipFromSprites(string name, Sprite[] sprites, float fps)
    {
        var clip = new AnimationClip();
        clip.frameRate = fps;
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe();
            keys[i].time = i / fps;
            keys[i].value = sprites[i];
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string path = AnimDir + "/" + name + ".anim";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    // Idle <-> Walk by the float "walk"; any state -> Jump when the bool "grounded" is false.
    static void MakePlatformerController(AnimationClip idle, AnimationClip walk, AnimationClip jump)
    {
        string path = AnimDir + "/PlayerPlat.controller";
        AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("walk", AnimatorControllerParameterType.Float);
        controller.AddParameter("grounded", AnimatorControllerParameterType.Bool);

        var machine = controller.layers[0].stateMachine;
        var stateIdle = machine.AddState("Idle");
        stateIdle.motion = idle;
        var stateWalk = machine.AddState("Walk");
        stateWalk.motion = walk;
        var stateJump = machine.AddState("Jump");
        stateJump.motion = jump;

        var idleToWalk = stateIdle.AddTransition(stateWalk);
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0f;
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "walk");

        var walkToIdle = stateWalk.AddTransition(stateIdle);
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0f;
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "walk");

        var idleToJump = stateIdle.AddTransition(stateJump);
        idleToJump.hasExitTime = false;
        idleToJump.duration = 0f;
        idleToJump.AddCondition(AnimatorConditionMode.IfNot, 0f, "grounded");

        var walkToJump = stateWalk.AddTransition(stateJump);
        walkToJump.hasExitTime = false;
        walkToJump.duration = 0f;
        walkToJump.AddCondition(AnimatorConditionMode.IfNot, 0f, "grounded");

        var jumpToIdle = stateJump.AddTransition(stateIdle);
        jumpToIdle.hasExitTime = false;
        jumpToIdle.duration = 0f;
        jumpToIdle.AddCondition(AnimatorConditionMode.If, 0f, "grounded");

        EditorUtility.SetDirty(controller);
    }

    // An obstacle moved with the Animation window (position + rotation curves), like slide 14.
    static void MakeMovingSaw()
    {
        var clip = new AnimationClip();
        clip.frameRate = 30f;
        clip.SetCurve("", typeof(Transform), "localPosition.y",
            new AnimationCurve(new Keyframe(0f, 1.9f), new Keyframe(1.3f, 5.2f), new Keyframe(2.6f, 1.9f)));
        clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.z", AnimationCurve.Linear(0f, 0f, 2.6f, 720f));
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string path = AnimDir + "/moving_saw.anim";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        MakeSingleStateController("MovingSaw", "Move", clip);
    }

    // ------------------------------------------------------------------ pixel helpers

    static Texture2D NewTex(int w, int h)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.SetPixels(new Color[w * h]);   // fully transparent
        return t;
    }

    static void Blend(Texture2D t, int x, int y, Color c, float cover)
    {
        if (x < 0 || y < 0 || x >= t.width || y >= t.height || cover <= 0f) return;
        Color o = t.GetPixel(x, y);
        float a = c.a * cover;
        float na = a + o.a * (1f - a);
        if (na <= 0f) return;
        t.SetPixel(x, y, new Color(
            (c.r * a + o.r * o.a * (1f - a)) / na,
            (c.g * a + o.g * o.a * (1f - a)) / na,
            (c.b * a + o.b * o.a * (1f - a)) / na,
            na));
    }

    static void Disc(Texture2D t, float cx, float cy, float r, Color c)
    {
        int x0 = Mathf.FloorToInt(cx - r - 1f), x1 = Mathf.CeilToInt(cx + r + 1f);
        int y0 = Mathf.FloorToInt(cy - r - 1f), y1 = Mathf.CeilToInt(cy + r + 1f);
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                Blend(t, x, y, c, Mathf.Clamp01(r - d + 0.5f));
            }
        }
    }

    static void Oval(Texture2D t, float cx, float cy, float rx, float ry, Color c)
    {
        int x0 = Mathf.FloorToInt(cx - rx - 1f), x1 = Mathf.CeilToInt(cx + rx + 1f);
        int y0 = Mathf.FloorToInt(cy - ry - 1f), y1 = Mathf.CeilToInt(cy + ry + 1f);
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                Blend(t, x, y, c, Mathf.Clamp01((1f - d) * Mathf.Min(rx, ry) + 0.5f));
            }
        }
    }

    static void Diamond(Texture2D t, float cx, float cy, float r, Color c)
    {
        int x0 = Mathf.FloorToInt(cx - r - 1f), x1 = Mathf.CeilToInt(cx + r + 1f);
        int y0 = Mathf.FloorToInt(cy - r - 1f), y1 = Mathf.CeilToInt(cy + r + 1f);
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                float d = Mathf.Abs(x + 0.5f - cx) + Mathf.Abs(y + 0.5f - cy);
                Blend(t, x, y, c, Mathf.Clamp01(r - d + 0.5f));
            }
        }
    }

    // x1 / y1 are exclusive
    static void Box(Texture2D t, int x0, int y0, int x1, int y1, Color c)
    {
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
                Blend(t, x, y, c, 1f);
    }

    static void Erase(Texture2D t, float cx, float cy, float r)
    {
        for (int y = 0; y < t.height; y++)
        {
            for (int x = 0; x < t.width; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                if (dx * dx + dy * dy <= r * r) t.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
            }
        }
    }

    static void Triangle(Texture2D t, float cx, float baseY, float halfWidth, float height, Color c)
    {
        for (int y = Mathf.FloorToInt(baseY); y < Mathf.CeilToInt(baseY + height); y++)
        {
            float k = 1f - (y + 0.5f - baseY) / height;
            float hw = halfWidth * k;
            for (int x = Mathf.FloorToInt(cx - halfWidth); x <= Mathf.CeilToInt(cx + halfWidth); x++)
                if (Mathf.Abs(x + 0.5f - cx) <= hw) Blend(t, x, y, c, 1f);
        }
    }

    static void Save(string name, Texture2D tex, float pixelsPerUnit, FilterMode filter)
    {
        tex.Apply();
        string path = SpriteDir + "/" + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = filter;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    // ------------------------------------------------------------------ tiles

    static void BuildTiles()
    {
        for (int i = 1; i <= FloorColors.Length; i++) MakeTile("floor_" + i, false);
        MakeTile("wall", true);
        MakeTile("grass", true);   // platformer tiles
        MakeTile("dirt", true);
    }

    static void MakeTile(string name, bool solid)
    {
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = LoadSprite(name);
        tile.colliderType = solid ? Tile.ColliderType.Grid : Tile.ColliderType.None;
        string path = TileDir + "/" + name + ".asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(tile, path);
    }

    // ------------------------------------------------------------------ animations

    static void BuildAnimations()
    {
        var idle = MakeClip("player_idle", new[] { "player_idle_0", "player_idle_1" }, 3f);
        var walk = MakeClip("player_walk", new[] { "player_walk_0", "player_walk_1", "player_walk_2", "player_walk_3" }, 8f);
        MakePlayerController(idle, walk);

        var hover = MakeClip("sentinel_hover", new[] { "sentinel_0", "sentinel_1" }, 4f);
        MakeSingleStateController("Sentinel", "Hover", hover);

        var spin = MakeClip("coin_spin", new[] { "coin_0", "coin_1", "coin_2", "coin_3" }, 8f);
        MakeSingleStateController("Coin", "Spin", spin);

        var shine = MakeClip("gem_shine", new[] { "gem_0", "gem_1" }, 3f);
        MakeSingleStateController("Gem", "Shine", shine);

        BuildPlatformerAnimations();
    }

    static AnimationClip MakeClip(string name, string[] frames, float fps)
    {
        var clip = new AnimationClip();
        clip.frameRate = fps;

        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe();
            keys[i].time = i / fps;
            keys[i].value = LoadSprite(frames[i]);
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string path = AnimDir + "/" + name + ".anim";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    // Idle <-> Walk driven by the float parameter "walk" (like the course slides).
    static void MakePlayerController(AnimationClip idle, AnimationClip walk)
    {
        string path = AnimDir + "/Player.controller";
        AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("walk", AnimatorControllerParameterType.Float);

        var machine = controller.layers[0].stateMachine;
        var stateIdle = machine.AddState("Idle");
        stateIdle.motion = idle;
        var stateWalk = machine.AddState("Walk");
        stateWalk.motion = walk;

        var toWalk = stateIdle.AddTransition(stateWalk);
        toWalk.hasExitTime = false;
        toWalk.duration = 0f;
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "walk");

        var toIdle = stateWalk.AddTransition(stateIdle);
        toIdle.hasExitTime = false;
        toIdle.duration = 0f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "walk");

        EditorUtility.SetDirty(controller);
    }

    static void MakeSingleStateController(string name, string stateName, AnimationClip clip)
    {
        string path = AnimDir + "/" + name + ".controller";
        AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var state = controller.layers[0].stateMachine.AddState(stateName);
        state.motion = clip;
        EditorUtility.SetDirty(controller);
    }
}
#endif
