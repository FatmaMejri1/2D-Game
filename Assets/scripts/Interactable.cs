using UnityEngine;

/// <summary>
/// Base class for anything the player can use with the E key.
/// Used directly for notes; DoorController and Collectible derive from it.
/// </summary>
public class Interactable : MonoBehaviour
{
    public string prompt = "Interact";
    [TextArea] public string message = "";

    public virtual bool CanInteract { get { return true; } }

    public virtual string GetPrompt()
    {
        return prompt;
    }

    public virtual void Interact()
    {
        if (!string.IsNullOrEmpty(message) && UIManager.Instance != null)
        {
            UIManager.Instance.ShowMessage(message, 6f);
            AudioManager.Play(Sfx.Start);
        }
    }
}
