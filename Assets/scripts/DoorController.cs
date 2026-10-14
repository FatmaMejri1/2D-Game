using System.Collections;
using UnityEngine;

public class DoorController : Interactable
{
    public string doorName = "door";
    public string requiredKey = "";      // e.g. "red" (empty = no key needed)
    public int requiredGems = 0;         // > 0 = gem lock
    public bool leverOnly = false;       // opened only by a Lever
    public string objectiveId = "";      // objective completed when opened
    public bool setRespawn = true;
    public Vector2 respawnPoint;

    bool isOpen;
    Collider2D col;
    SpriteRenderer sr;

    public bool IsOpen { get { return isOpen; } }
    public override bool CanInteract { get { return !isOpen; } }

    void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    public override string GetPrompt()
    {
        var gm = GameManager.Instance;
        if (gm == null) return prompt;
        if (leverOnly) return "The " + doorName + " has no keyhole";
        if (requiredGems > 0)
            return gm.Gems >= requiredGems ? "Insert gems and open the " + doorName : "The " + doorName + " needs gems (" + gm.Gems + "/" + requiredGems + ")";
        if (!string.IsNullOrEmpty(requiredKey))
            return gm.HasKey(requiredKey) ? "Unlock the " + doorName : "The " + doorName + " is locked";
        return "Open the " + doorName;
    }

    public override void Interact()
    {
        var gm = GameManager.Instance;
        var ui = UIManager.Instance;
        if (isOpen || gm == null || ui == null) return;

        if (leverOnly)
        {
            ui.ShowMessage("No keyhole here. Look for a lever nearby.", 3f);
            AudioManager.Play(Sfx.Locked);
            return;
        }
        if (requiredGems > 0 && gm.Gems < requiredGems)
        {
            ui.ShowMessage("The lock needs " + requiredGems + " gems. You have " + gm.Gems + ".", 3f);
            AudioManager.Play(Sfx.Locked);
            return;
        }
        if (!string.IsNullOrEmpty(requiredKey) && !gm.HasKey(requiredKey))
        {
            ui.ShowMessage("Locked. You need the " + requiredKey + " key.", 3f);
            AudioManager.Play(Sfx.Locked);
            return;
        }
        Open();
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        if (col == null) col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        AudioManager.Play(Sfx.Door);
        Fx.Burst(transform.position, sr != null ? sr.color : Color.white, 18, 5f, 0.22f);
        if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.12f, 0.35f);

        if (setRespawn && GameManager.Instance != null) GameManager.Instance.RespawnPoint = respawnPoint;
        if (!string.IsNullOrEmpty(objectiveId) && ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.Complete(objectiveId);
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage("The " + doorName + " opens!", 2.5f);

        StartCoroutine(Slide());
    }

    IEnumerator Slide()
    {
        Vector3 start = transform.localScale;
        float t = 0f;
        const float duration = 0.6f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            transform.localScale = new Vector3(start.x, Mathf.Lerp(start.y, 0.01f, k), 1f);
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
