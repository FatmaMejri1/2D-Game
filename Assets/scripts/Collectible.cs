using UnityEngine;

public enum CollectibleType { Key, Gem, Coin, TimeBonus, Shield, Speed }

public class Collectible : Interactable
{
    public CollectibleType type = CollectibleType.Coin;
    public string keyId = "";          // for keys, e.g. "red"
    public string displayName = "item";
    public float amount = 20f;         // seconds for TimeBonus
    public bool autoPickup = true;     // everything except keys is picked up by touching it
    public Transform visual;           // animated child

    bool collected;
    float seed;
    bool hasAnimator;

    public override bool CanInteract { get { return !collected && !autoPickup; } }

    public override string GetPrompt()
    {
        return "Pick up the " + displayName;
    }

    public override void Interact()
    {
        Collect(null);
    }

    void Start()
    {
        seed = Random.value * 6.28f;
        hasAnimator = visual != null && visual.GetComponentInChildren<Animator>() != null;
    }

    void Update()
    {
        if (visual == null || collected) return;
        float t = Time.time + seed;
        visual.localPosition = new Vector3(0f, Mathf.Sin(t * 3f) * 0.07f, 0f);
        if (type == CollectibleType.Coin && !hasAnimator)
            visual.localScale = new Vector3(Mathf.Abs(Mathf.Cos(t * 3f)) * 0.85f + 0.15f, 1f, 1f);
        else if (type == CollectibleType.Gem)
            visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2f) * 12f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!autoPickup || collected) return;
        var player = other.GetComponent<PlayerMovement>();
        if (player != null) Collect(player);
    }

    void Collect(PlayerMovement player)
    {
        var gm = GameManager.Instance;
        if (collected || gm == null || gm.State != GameState.Playing) return;
        collected = true;

        Vector2 pos = transform.position;
        Color fxColor = Color.white;
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            fxColor = sr.color;
            fxColor.a = 1f;
        }

        string msg = "";
        switch (type)
        {
            case CollectibleType.Key:
                gm.AddKey(keyId);
                if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.Complete(keyId + "key");
                gm.AddScore(200, pos);
                AudioManager.Play(Sfx.Pickup);
                msg = "You picked up the " + displayName + "!";
                break;

            case CollectibleType.Gem:
                gm.AddGem(pos);
                AudioManager.Play(Sfx.Gem);
                msg = "Gem collected (" + gm.Gems + "/" + gm.gemsRequired + ")";
                break;

            case CollectibleType.Coin:
                gm.AddCoin(pos);
                AudioManager.Play(Sfx.Coin);
                break;

            case CollectibleType.TimeBonus:
                gm.AddTime(amount);
                gm.AddScore(100, pos, "+" + Mathf.RoundToInt(amount) + "s");
                AudioManager.Play(Sfx.PowerUp);
                msg = "+" + Mathf.RoundToInt(amount) + " seconds!";
                break;

            case CollectibleType.Shield:
                if (player != null) player.GrantShield();
                AudioManager.Play(Sfx.PowerUp);
                msg = "Shield ready! It absorbs one hit.";
                break;

            case CollectibleType.Speed:
                if (player != null) player.GrantSpeed(8f);
                AudioManager.Play(Sfx.PowerUp);
                msg = "Speed boost for 8 seconds!";
                break;
        }

        Fx.Burst(pos, fxColor, type == CollectibleType.Coin ? 6 : 14, 3.2f, 0.16f);
        if (!string.IsNullOrEmpty(msg) && UIManager.Instance != null) UIManager.Instance.ShowMessage(msg, 2.5f);
        Destroy(gameObject);
    }
}
