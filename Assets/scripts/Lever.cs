using UnityEngine;

/// <summary>A lever that opens a linked door.</summary>
public class Lever : Interactable
{
    public DoorController door;
    public Transform handle;
    public string objectiveId = "lever";

    bool used;

    public override bool CanInteract { get { return !used; } }

    public override string GetPrompt()
    {
        return "Pull the lever";
    }

    public override void Interact()
    {
        if (used) return;
        used = true;

        if (handle != null) handle.localRotation = Quaternion.Euler(0f, 0f, -35f);
        AudioManager.Play(Sfx.Lever);
        Fx.Burst(transform.position, new Color(1f, 0.4f, 0.4f), 12, 3.5f, 0.18f);

        if (door != null) door.Open();
        if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.Complete(objectiveId);
        if (GameManager.Instance != null) GameManager.Instance.AddScore(300, transform.position);
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage("CLUNK! A gate rumbles open somewhere nearby.", 3f);
    }
}
