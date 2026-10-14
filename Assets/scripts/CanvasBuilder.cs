using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the whole HUD in code: rounded neon panels, heart icons, key slots, objective card,
/// progress bar, prompt keycap, room banner and the title / pause / win / lose card.
/// </summary>
public static class CanvasBuilder
{
    // Set by the editor tool so the HUD can be built in the scene with real sprite assets.
    public static Sprite SquareOverride, CircleOverride, RoundedOverride, HeartOverride, StarOverride, VignetteOverride;

    public static void ClearOverrides()
    {
        SquareOverride = CircleOverride = RoundedOverride = HeartOverride = StarOverride = VignetteOverride = null;
    }

    static Sprite Sq() { return SquareOverride != null ? SquareOverride : LevelSetupHelper.Square(); }
    static Sprite Ci() { return CircleOverride != null ? CircleOverride : LevelSetupHelper.Circle(); }

    static Font font;
    static Sprite roundedSprite, heartSprite, starSprite, vignetteSprite;

    static readonly Vector2 TL = new Vector2(0f, 1f);
    static readonly Vector2 TC = new Vector2(0.5f, 1f);
    static readonly Vector2 TR = new Vector2(1f, 1f);
    static readonly Vector2 LC = new Vector2(0f, 0.5f);
    static readonly Vector2 C = new Vector2(0.5f, 0.5f);
    static readonly Vector2 BC = new Vector2(0.5f, 0f);
    static readonly Vector2 BR = new Vector2(1f, 0f);

    static readonly Color PanelFill = new Color(0.04f, 0.06f, 0.11f, 0.80f);
    static readonly Color Accent = new Color(0.25f, 0.75f, 1f, 0.55f);
    static readonly Color Cyan = new Color(0.35f, 0.85f, 1f);
    static readonly Color Gold = new Color(1f, 0.88f, 0.4f);
    static readonly Color Grey = new Color(0.62f, 0.66f, 0.74f);

    public static UIManager Build(Transform parent, int maxLives)
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasGO = new GameObject("HUD Canvas");
        canvasGO.transform.SetParent(parent, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var ui = canvasGO.AddComponent<UIManager>();
        Transform root = canvasGO.transform;

        var vignette = MakeImage(root, "Vignette", Vignette(), new Color(0f, 0f, 0f, 0.85f), C, C, Vector2.zero, Vector2.zero);
        Stretch(vignette.rectTransform);

        BuildMission(root, ui);
        BuildTimer(root, ui);
        BuildStatus(root, ui, maxLives);
        BuildBottom(root, ui);
        BuildToasts(root, ui);
        BuildFlash(root, ui);
        BuildOverlay(root, ui);
        return ui;
    }

    // ------------------------------------------------------------------ HUD pieces

    static void BuildMission(Transform root, UIManager ui)
    {
        var card = MakePanel(root, "Mission Card", TL, TL, new Vector2(24f, -24f), new Vector2(560f, 330f), PanelFill, Accent);
        var header = MakeText(card.transform, "Header", 26, TextAnchor.UpperLeft, Cyan, TL, TL, new Vector2(24f, -14f), new Vector2(500f, 40f), true);
        header.text = "MISSION";
        ui.objectivesText = MakeText(card.transform, "Objectives", 25, TextAnchor.UpperLeft, Color.white, TL, TL,
            new Vector2(24f, -56f), new Vector2(520f, 260f), true);
    }

    static void BuildTimer(Transform root, UIManager ui)
    {
        var pill = MakePanel(root, "Timer Pill", TC, TC, new Vector2(0f, -20f), new Vector2(300f, 100f), PanelFill, Accent);
        ui.timeText = MakeText(pill.transform, "Time", 64, TextAnchor.MiddleCenter, Color.white, C, C, Vector2.zero, new Vector2(280f, 90f), true);

        var bg = MakeImage(root, "Timer Bar", Rounded(), new Color(0.05f, 0.07f, 0.12f, 0.85f), TC, TC, new Vector2(0f, -130f), new Vector2(300f, 14f));
        bg.type = Image.Type.Sliced;
        var fill = MakeImage(bg.transform, "Fill", Sq(), Cyan, C, C, Vector2.zero, Vector2.zero);
        Stretch(fill.rectTransform);
        fill.rectTransform.offsetMin = new Vector2(3f, 3f);
        fill.rectTransform.offsetMax = new Vector2(-3f, -3f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 1f;
        ui.timerFill = fill;
    }

    static void BuildStatus(Transform root, UIManager ui, int maxLives)
    {
        var card = MakePanel(root, "Status Card", TR, TR, new Vector2(-24f, -24f), new Vector2(460f, 250f), PanelFill, Accent);

        var scoreLabel = MakeText(card.transform, "ScoreLabel", 20, TextAnchor.UpperLeft, Grey, TL, TL, new Vector2(24f, -12f), new Vector2(200f, 30f), true);
        scoreLabel.text = "SCORE";
        ui.bestText = MakeText(card.transform, "Best", 20, TextAnchor.UpperRight, Grey, TR, TR, new Vector2(-24f, -12f), new Vector2(260f, 30f), true);
        ui.scoreText = MakeText(card.transform, "Score", 60, TextAnchor.UpperLeft, Gold, TL, TL, new Vector2(24f, -34f), new Vector2(410f, 76f), true);

        // hearts
        ui.hearts = new Image[maxLives];
        for (int i = 0; i < maxLives; i++)
        {
            ui.hearts[i] = MakeImage(card.transform, "Heart" + i, Heart(), new Color(1f, 0.28f, 0.38f), TL, C,
                new Vector2(46f + i * 54f, -142f), new Vector2(44f, 44f));
        }

        // gems
        var gemIcon = MakeImage(card.transform, "GemIcon", Sq(), new Color(0.3f, 0.95f, 1f), TL, C, new Vector2(37f, -200f), new Vector2(24f, 24f));
        gemIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        ui.gemsText = MakeText(card.transform, "Gems", 34, TextAnchor.MiddleLeft, new Color(0.5f, 0.95f, 1f), TL, LC, new Vector2(62f, -200f), new Vector2(110f, 44f), true);

        // coins
        MakeImage(card.transform, "CoinIcon", Ci(), Gold, TL, C, new Vector2(205f, -200f), new Vector2(30f, 30f));
        ui.coinsText = MakeText(card.transform, "Coins", 34, TextAnchor.MiddleLeft, Gold, TL, LC, new Vector2(228f, -200f), new Vector2(100f, 44f), true);

        // key slots: red, blue, gold
        ui.keySlots = new Image[3];
        for (int i = 0; i < 3; i++)
            ui.keySlots[i] = MakeImage(card.transform, "KeySlot" + i, Ci(), Color.gray, TL, C,
                new Vector2(340f + i * 44f, -200f), new Vector2(28f, 28f));

        ui.powerText = MakeText(root, "Powerups", 28, TextAnchor.UpperRight, Color.white, TR, TR, new Vector2(-24f, -285f), new Vector2(460f, 40f), true);
        ui.comboText = MakeText(root, "Combo", 46, TextAnchor.UpperRight, Gold, TR, TR, new Vector2(-24f, -325f), new Vector2(460f, 60f), true);
    }

    static void BuildBottom(Transform root, UIManager ui)
    {
        // interaction prompt with a keycap
        var pill = MakePanel(root, "Prompt Pill", BC, BC, new Vector2(0f, 150f), new Vector2(820f, 88f), PanelFill, new Color(1f, 0.88f, 0.4f, 0.6f));
        var cap = MakePanel(pill.transform, "Keycap", LC, LC, new Vector2(18f, 0f), new Vector2(60f, 60f), new Color(0.92f, 0.94f, 1f), new Color(0.45f, 0.5f, 0.7f, 1f));
        var capText = MakeText(cap.transform, "Key", 40, TextAnchor.MiddleCenter, new Color(0.1f, 0.12f, 0.2f), C, C, Vector2.zero, new Vector2(60f, 60f), false);
        capText.text = "E";
        ui.promptText = MakeText(pill.transform, "PromptText", 36, TextAnchor.MiddleLeft, Color.white, LC, LC, new Vector2(96f, 0f), new Vector2(700f, 70f), true);
        ui.promptRoot = pill.gameObject;
        pill.gameObject.SetActive(false);

        // level progress: 5 room segments with a moving dot
        var track = MakeRect("Progress", root, BC, BC, new Vector2(0f, 48f), new Vector2(1000f, 40f));
        ui.progressRoot = track.gameObject;
        ui.roomSegments = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            var seg = MakeImage(track, "Seg" + i, Rounded(), new Color(0.25f, 0.28f, 0.38f, 0.7f), C, C,
                new Vector2((i - 2) * 200f, -4f), new Vector2(190f, 12f));
            seg.type = Image.Type.Sliced;
            ui.roomSegments[i] = seg;

            var num = MakeText(track, "Num" + i, 20, TextAnchor.MiddleCenter, Grey, C, C, new Vector2((i - 2) * 200f, 16f), new Vector2(60f, 24f), true);
            num.text = (i + 1).ToString();
        }
        ui.progressDot = MakeImage(track, "Dot", Ci(), Gold, C, C, new Vector2(-500f, -4f), new Vector2(26f, 26f)).rectTransform;

        ui.soundText = MakeText(root, "Sound", 22, TextAnchor.LowerRight, Grey, BR, BR, new Vector2(-24f, 20f), new Vector2(400f, 34f), true);
        ui.soundText.text = "[M] SOUND ON";
    }

    static void BuildToasts(Transform root, UIManager ui)
    {
        // toast message
        var msg = MakePanel(root, "Message", TC, TC, new Vector2(0f, -165f), new Vector2(1300f, 84f), PanelFill, new Color(0.5f, 1f, 0.6f, 0.5f));
        ui.messageGroup = msg.gameObject.AddComponent<CanvasGroup>();
        ui.messageText = MakeText(msg.transform, "MessageText", 36, TextAnchor.MiddleCenter, new Color(0.9f, 1f, 0.92f), C, C, Vector2.zero, new Vector2(1260f, 80f), true);
        ui.messageRoot = msg.gameObject;
        msg.gameObject.SetActive(false);

        // room banner
        var banner = MakeRect("Banner", root, C, C, new Vector2(0f, 170f), new Vector2(1500f, 220f));
        ui.bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
        ui.bannerRect = banner;
        ui.bannerTitle = MakeText(banner, "BannerTitle", 96, TextAnchor.MiddleCenter, Gold, C, C, new Vector2(0f, 30f), new Vector2(1500f, 120f), true);
        ui.bannerSub = MakeText(banner, "BannerSub", 38, TextAnchor.MiddleCenter, Color.white, C, C, new Vector2(0f, -50f), new Vector2(1500f, 60f), true);
        ui.bannerRoot = banner.gameObject;
        banner.gameObject.SetActive(false);
    }

    static void BuildFlash(Transform root, UIManager ui)
    {
        var flash = MakeImage(root, "Flash", Sq(), new Color(1f, 0f, 0f, 0f), C, C, Vector2.zero, Vector2.zero);
        Stretch(flash.rectTransform);
        ui.flashImage = flash;
    }

    static void BuildOverlay(Transform root, UIManager ui)
    {
        var dim = MakeImage(root, "Overlay", Sq(), new Color(0f, 0.01f, 0.04f, 0.84f), C, C, Vector2.zero, Vector2.zero);
        Stretch(dim.rectTransform);
        ui.overlayRoot = dim.gameObject;

        var card = MakePanel(dim.transform, "Card", C, C, Vector2.zero, new Vector2(1240f, 860f),
            new Color(0.04f, 0.06f, 0.11f, 0.95f), new Color(0.25f, 0.75f, 1f, 0.7f));
        Transform ct = card.transform;

        ui.overlayTitle = MakeText(ct, "Title", 110, TextAnchor.MiddleCenter, Color.white, TC, TC, new Vector2(0f, -40f), new Vector2(1160f, 140f), true);
        ui.overlayBody = MakeText(ct, "Body", 36, TextAnchor.UpperCenter, new Color(0.85f, 0.9f, 1f), TC, TC, new Vector2(0f, -190f), new Vector2(1080f, 400f), true);

        // stars (win screen)
        var starsRect = MakeRect("Stars", ct, TC, TC, new Vector2(0f, -190f), new Vector2(400f, 110f));
        ui.stars = new Image[3];
        for (int i = 0; i < 3; i++)
            ui.stars[i] = MakeImage(starsRect, "Star" + i, Star(), Gold, C, C, new Vector2((i - 1) * 120f, 0f), new Vector2(100f, 100f));
        ui.starsRoot = starsRect.gameObject;
        starsRect.gameObject.SetActive(false);

        // legend (title screen)
        var legend = MakeRect("Legend", ct, TC, TC, new Vector2(0f, -345f), new Vector2(1160f, 220f));
        AddLegend(legend, 0, 0, Ci(), new Color(1f, 0.82f, 0.2f), 0f, "Keys open colored doors");
        AddLegend(legend, 1, 0, Sq(), new Color(0.3f, 0.95f, 1f), 45f, "Collect gems to open the vault");
        AddLegend(legend, 2, 0, Ci(), new Color(1f, 0.9f, 0.3f), 0f, "Coins give combo points");
        AddLegend(legend, 0, 1, Ci(), new Color(0.85f, 0.87f, 0.92f), 0f, "Lasers, saws and spikes cost a life");
        AddLegend(legend, 1, 1, Ci(), new Color(1f, 0.55f, 0.2f), 0f, "Sentinels hunt you: hide!");
        AddLegend(legend, 2, 1, Ci(), new Color(0.7f, 0.4f, 1f), 0f, "Purple pads teleport you");
        ui.legendRoot = legend.gameObject;

        ui.controlsText = MakeText(ct, "Controls", 28, TextAnchor.MiddleCenter, Grey, TC, TC, new Vector2(0f, -610f), new Vector2(1160f, 80f), true);
        ui.controlsText.text = "WASD / ARROWS  Move      SHIFT  Sprint      E  Interact      ESC  Pause      M  Mute";

        ui.overlayHint = MakeText(ct, "Hint", 46, TextAnchor.MiddleCenter, Gold, BC, BC, new Vector2(0f, 50f), new Vector2(1000f, 80f), true);

        dim.gameObject.SetActive(false);
    }

    static void AddLegend(RectTransform parent, int col, int row, Sprite icon, Color color, float rotation, string text)
    {
        float x = (col - 1) * 380f;
        float y = -45f - row * 95f;
        var img = MakeImage(parent, "LegendIcon", icon, color, TC, C, new Vector2(x - 150f, y), new Vector2(46f, 46f));
        img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        var label = MakeText(parent, "LegendText", 24, TextAnchor.MiddleLeft, new Color(0.85f, 0.9f, 1f), TC, LC, new Vector2(x - 112f, y), new Vector2(290f, 80f), true);
        label.text = text;
    }

    // ------------------------------------------------------------------ helpers

    static RectTransform MakeRect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Image MakeImage(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var rt = MakeRect(name, parent, anchor, pivot, pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static Image MakePanel(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color fill, Color border)
    {
        var img = MakeImage(parent, name, Rounded(), fill, anchor, pivot, pos, size);
        img.type = Image.Type.Sliced;
        var outline = img.gameObject.AddComponent<Outline>();
        outline.effectColor = border;
        outline.effectDistance = new Vector2(2f, -2f);
        return img;
    }

    static Text MakeText(Transform parent, string name, int size, TextAnchor align, Color color,
        Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 sizeDelta, bool outline)
    {
        var rt = MakeRect(name, parent, anchor, pivot, pos, sizeDelta);
        var text = rt.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = align;
        text.color = color;
        text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        if (outline)
        {
            var o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.9f);
            o.effectDistance = new Vector2(2f, -2f);
        }
        return text;
    }

    // ------------------------------------------------------------------ generated sprites

    static Sprite Rounded()
    {
        if (RoundedOverride != null) return RoundedOverride;
        if (roundedSprite == null)
        {
            const int size = 64;
            const float radius = 18f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x - c) - (c - radius), 0f);
                    float dy = Mathf.Max(Mathf.Abs(y - c) - (c - radius), 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    byte a = (byte)(Mathf.Clamp01(0.5f - d) * 255f);
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            roundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(20f, 20f, 20f, 20f));
        }
        return roundedSprite;
    }

    static Sprite Heart()
    {
        if (HeartOverride != null) return HeartOverride;
        if (heartSprite == null)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
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
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(hit * 255 / 4));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            heartSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
        return heartSprite;
    }

    static Sprite Star()
    {
        if (StarOverride != null) return StarOverride;
        if (starSprite == null)
        {
            const int size = 64;
            var poly = new Vector2[10];
            for (int k = 0; k < 10; k++)
            {
                float ang = Mathf.PI / 2f + k * Mathf.PI / 5f;
                float r = (k % 2 == 0) ? 30f : 12f;
                poly[k] = new Vector2(31.5f + Mathf.Cos(ang) * r, 33f + Mathf.Sin(ang) * r);
            }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hit = 0;
                    for (int sy = 0; sy < 2; sy++)
                        for (int sx = 0; sx < 2; sx++)
                            if (InPolygon(poly, x + 0.25f + sx * 0.5f, y + 0.25f + sy * 0.5f)) hit++;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(hit * 255 / 4));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            starSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
        return starSprite;
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

    static Sprite Vignette()
    {
        if (VignetteOverride != null) return VignetteOverride;
        if (vignetteSprite == null)
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - c) / c;
                    float dy = (y - c) / c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01((d - 0.55f) / 0.65f);
                    a *= a;
                    px[y * size + x] = new Color32(0, 0, 0, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            vignetteSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
        return vignetteSprite;
    }
}
