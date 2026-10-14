#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Creates every game object of the level as a Prefab in Assets/Prefabs.</summary>
public static class CoursePrefabFactory
{
    public const string PrefabDir = "Assets/Prefabs";

    static Sprite S(string name) { return CourseAssetFactory.LoadSprite(name); }

    public static void BuildAll()
    {
        CourseAssetFactory.EnsureFolder(PrefabDir);
        UnityEditorInternal.InternalEditorUtility.AddTag("danger");

        BuildPlayer();

        BuildPickup("Coin", CollectibleType.Coin, "coin_0", "Coin", "coin", true);
        BuildPickup("Gem", CollectibleType.Gem, "gem_0", "Gem", "gem", true);
        BuildPickup("Key", CollectibleType.Key, "key", null, "key", false);
        BuildPickup("Clock", CollectibleType.TimeBonus, "clock", null, "clock", true);
        BuildPickup("Shield", CollectibleType.Shield, "shield", null, "shield", true);
        BuildPickup("SpeedBoost", CollectibleType.Speed, "speed", null, "speed boost", true);

        BuildSaw();
        BuildLaser();
        BuildSpikeTile();
        BuildSpinner();
        BuildSentinel();
        BuildDoor();
        BuildLever();
        BuildTeleporter();
        BuildNote();
        BuildFurniture("Desk", true);
        BuildFurniture("Shelf", false);
        BuildPortal();

        BuildPlatformerPrefabs();
    }

    static void SavePrefab(GameObject go, string name)
    {
        string path = PrefabDir + "/" + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    static SpriteRenderer AddSprite(GameObject go, string sprite, Color color, int order)
    {
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = S(sprite);
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    static GameObject Child(GameObject parent, string name, string sprite, Vector2 pos, Vector2 scale, Color color, int order)
    {
        var c = new GameObject(name);
        c.transform.SetParent(parent.transform, false);
        c.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
        c.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        AddSprite(c, sprite, color, order);
        return c;
    }

    // ------------------------------------------------------------------ player

    static void BuildPlayer()
    {
        var go = new GameObject("Player");
        go.tag = "Player";
        AddSprite(go, "player_idle_0", Color.white, 10);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.36f;
        col.offset = new Vector2(0f, -0.03f);

        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CourseAssetFactory.AnimDir + "/Player.controller");

        var move = go.AddComponent<PlayerMovement>();
        go.AddComponent<PlayerInteraction>();

        var ring = Child(go, "ShieldRing", "white_circle", Vector2.zero, new Vector2(1.3f, 1.3f), new Color(0.3f, 0.9f, 1f, 0.35f), 9);
        ring.SetActive(false);
        move.shieldRing = ring;

        SavePrefab(go, "Player");
    }

    // ------------------------------------------------------------------ pickups

    static void BuildPickup(string prefabName, CollectibleType type, string sprite, string controller, string display, bool auto)
    {
        var go = new GameObject(prefabName);
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.45f;
        col.isTrigger = true;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        AddSprite(visual, sprite, Color.white, 5);
        if (controller != null)
        {
            var anim = visual.AddComponent<Animator>();
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CourseAssetFactory.AnimDir + "/" + controller + ".controller");
        }

        var c = go.AddComponent<Collectible>();
        c.type = type;
        c.displayName = display;
        c.autoPickup = auto;
        c.visual = visual.transform;

        SavePrefab(go, prefabName);
    }

    // ------------------------------------------------------------------ hazards

    static void BuildSaw()
    {
        var go = new GameObject("Saw");
        go.tag = "danger";
        go.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
        AddSprite(go, "saw", Color.white, 4);

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.45f;
        col.isTrigger = true;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        var h = go.AddComponent<HazardZone>();
        h.patrol = true;
        h.patrolSpeed = 3f;
        h.spinSpeed = 360f;
        h.reason = "Sliced by a saw!";
        SavePrefab(go, "Saw");
    }

    static void BuildLaser()
    {
        var go = new GameObject("Laser");
        go.tag = "danger";
        go.transform.localScale = new Vector3(0.5f, 10f, 1f);
        AddSprite(go, "white_square", new Color(1f, 0.15f, 0.25f), 4);

        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
        col.isTrigger = true;

        var h = go.AddComponent<HazardZone>();
        h.blink = true;
        h.onTime = 1.6f;
        h.offTime = 2f;
        h.reason = "Laser burn!";
        SavePrefab(go, "Laser");
    }

    static void BuildSpikeTile()
    {
        var go = new GameObject("SpikeTile");
        go.tag = "danger";
        go.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
        AddSprite(go, "spike_tile", Color.white, 4);

        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
        col.isTrigger = true;

        var h = go.AddComponent<HazardZone>();
        h.blink = true;
        h.onTime = 1.1f;
        h.offTime = 2f;
        h.reason = "Impaled by spikes!";
        SavePrefab(go, "SpikeTile");
    }

    // a one-armed beam that rotates around its pivot
    static void BuildSpinner()
    {
        const float length = 3.2f;
        var go = new GameObject("Spinner");
        go.tag = "danger";

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.offset = new Vector2(length * 0.5f, 0f);
        col.size = new Vector2(length, 0.35f);

        Child(go, "Beam", "white_square", new Vector2(length * 0.5f, 0f), new Vector2(length, 0.3f), new Color(1f, 0.2f, 0.35f), 4);
        Child(go, "Hub", "white_circle", Vector2.zero, new Vector2(0.9f, 0.9f), new Color(0.15f, 0.15f, 0.22f), 5);
        Child(go, "HubCore", "white_circle", Vector2.zero, new Vector2(0.4f, 0.4f), new Color(1f, 0.2f, 0.35f), 6);

        var h = go.AddComponent<HazardZone>();
        h.spinSpeed = 120f;
        h.reason = "Cut by a spinning beam!";
        SavePrefab(go, "Spinner");
    }

    static void BuildSentinel()
    {
        var go = new GameObject("Sentinel");
        go.tag = "danger";
        go.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
        AddSprite(go, "sentinel_0", new Color(1f, 0.6f, 0.2f), 8);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.mass = 50f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.38f;

        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CourseAssetFactory.AnimDir + "/Sentinel.controller");

        go.AddComponent<Sentinel>();
        SavePrefab(go, "Sentinel");
    }

    // ------------------------------------------------------------------ props

    static void BuildDoor()
    {
        var go = new GameObject("Door");
        AddSprite(go, "door", Color.white, 3);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
        var d = go.AddComponent<DoorController>();
        d.setRespawn = true;
        SavePrefab(go, "Door");
    }

    static void BuildLever()
    {
        var go = new GameObject("Lever");
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = Vector2.one;

        Child(go, "Base", "white_square", new Vector2(0f, -0.15f), new Vector2(0.9f, 0.3f), new Color(0.3f, 0.32f, 0.4f), 5);

        var handle = new GameObject("Handle");
        handle.transform.SetParent(go.transform, false);
        handle.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        handle.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
        Child(handle, "Stick", "white_square", new Vector2(0f, 0.3f), new Vector2(0.1f, 0.6f), new Color(0.8f, 0.8f, 0.85f), 6);
        Child(handle, "Knob", "white_circle", new Vector2(0f, 0.62f), new Vector2(0.28f, 0.28f), new Color(1f, 0.3f, 0.3f), 7);

        var lever = go.AddComponent<Lever>();
        lever.handle = handle.transform;
        SavePrefab(go, "Lever");
    }

    static void BuildTeleporter()
    {
        var go = new GameObject("Teleporter");
        AddSprite(go, "teleporter", Color.white, 2);
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.55f;
        col.isTrigger = true;
        go.AddComponent<Teleporter>();
        SavePrefab(go, "Teleporter");
    }

    static void BuildNote()
    {
        var go = new GameObject("Note");
        go.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
        AddSprite(go, "paper", new Color(0.95f, 0.92f, 0.75f), 1);
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.6f, 0.7f);
        var n = go.AddComponent<Interactable>();
        n.prompt = "Read the note";
        SavePrefab(go, "Note");
    }

    static void BuildFurniture(string name, bool readable)
    {
        var go = new GameObject(name);
        AddSprite(go, "white_square", new Color(0.45f, 0.3f, 0.18f), 1);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
        if (readable)
        {
            var n = go.AddComponent<Interactable>();
            n.prompt = "Read the note on the desk";
        }
        SavePrefab(go, name);
    }

    static void BuildPortal()
    {
        var go = new GameObject("ExitPortal");
        go.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
        AddSprite(go, "portal", Color.white, 2);
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        col.isTrigger = true;
        go.AddComponent<ExitZone>();
        SavePrefab(go, "ExitPortal");
    }

    // ------------------------------------------------------------------ platformer prefabs

    static PhysicsMaterial2D MakeNoFriction()
    {
        string path = PrefabDir + "/NoFriction.physicsMaterial2D";
        AssetDatabase.DeleteAsset(path);
        var material = new PhysicsMaterial2D("NoFriction");
        material.friction = 0f;
        material.bounciness = 0f;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static RuntimeAnimatorController Controller(string name)
    {
        return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CourseAssetFactory.AnimDir + "/" + name + ".controller");
    }

    static void BuildPlatformerPrefabs()
    {
        var noFriction = MakeNoFriction();
        BuildPlatformerPlayer(noFriction);
        BuildSpike();
        BuildWalker(noFriction);
        BuildMovingSaw();
        BuildSpring();
        BuildFlagPrefab("Checkpoint", false);
        BuildFlagPrefab("FlagGoal", true);
    }

    static void BuildPlatformerPlayer(PhysicsMaterial2D noFriction)
    {
        var go = new GameObject("PlayerPlat");
        go.tag = "Player";
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CourseAssetFactory.PlatformerFrame(0, "player_idle_0");   // frame cut from the sprite sheet
        sr.sortingOrder = 10;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;                       // gravity: the character falls and can jump
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        var col = go.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.55f, 0.8f);
        col.offset = new Vector2(0f, -0.02f);
        col.sharedMaterial = noFriction;            // no friction: no sticking to walls

        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = Controller("PlayerPlat");

        var move = go.AddComponent<PlayerMovement>();
        move.platformerMode = true;
        move.moveSpeed = 6f;
        move.jumpForce = 13f;
        move.fallLimitY = -9f;

        var ring = Child(go, "ShieldRing", "white_circle", Vector2.zero, new Vector2(1.3f, 1.3f), new Color(0.3f, 0.9f, 1f, 0.35f), 9);
        ring.SetActive(false);
        move.shieldRing = ring;

        SavePrefab(go, "PlayerPlat");
    }

    static void BuildSpike()
    {
        var go = new GameObject("Spike");
        go.tag = "danger";                           // detected by the player through its tag
        AddSprite(go, "spike_up", Color.white, 4);
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.8f, 0.45f);
        col.offset = new Vector2(0f, -0.15f);
        SavePrefab(go, "Spike");
    }

    static void BuildWalker(PhysicsMaterial2D noFriction)
    {
        var go = new GameObject("Walker");
        AddSprite(go, "walker_0", Color.white, 8);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.7f, 0.55f);
        col.offset = new Vector2(0f, -0.12f);
        col.sharedMaterial = noFriction;

        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = Controller("Walker");

        go.AddComponent<Walker>();
        SavePrefab(go, "Walker");
    }

    // an obstacle animated with an AnimationClip (moves up and down while spinning)
    static void BuildMovingSaw()
    {
        var go = new GameObject("MovingSaw");
        go.tag = "danger";
        go.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
        AddSprite(go, "saw", Color.white, 4);
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.45f;
        col.isTrigger = true;
        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = Controller("MovingSaw");
        SavePrefab(go, "MovingSaw");
    }

    static void BuildSpring()
    {
        var go = new GameObject("Spring");
        AddSprite(go, "spring", Color.white, 3);
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.9f, 0.6f);
        col.offset = new Vector2(0f, 0.2f);
        var pad = go.AddComponent<JumpPad>();
        pad.power = 17f;
        SavePrefab(go, "Spring");
    }

    // a pole with a flag: either a checkpoint or the final goal
    static void BuildFlagPrefab(string name, bool goal)
    {
        var go = new GameObject(name);
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = goal ? new Vector2(1.2f, 3f) : new Vector2(1f, 2f);
        col.offset = goal ? new Vector2(0f, 1.5f) : new Vector2(0f, 1f);

        float scale = goal ? 1.5f : 1f;
        Color tint = goal ? new Color(1f, 0.85f, 0.2f) : Color.white;
        Child(go, "Flag", "flag", new Vector2(0f, scale), new Vector2(scale, scale), tint, 5);

        if (goal) go.AddComponent<FlagGoal>();
        else go.AddComponent<Checkpoint>();
        SavePrefab(go, name);
    }
}
#endif
