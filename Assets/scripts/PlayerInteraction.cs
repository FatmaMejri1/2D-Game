using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public float radius = 1.3f;
    Interactable current;

    void Update()
    {
        var gm = GameManager.Instance;
        var ui = UIManager.Instance;
        if (gm == null || ui == null) return;

        if (gm.State != GameState.Playing)
        {
            current = null;
            ui.SetPrompt("");
            return;
        }

        current = FindBest();
        ui.SetPrompt(current != null ? current.GetPrompt() : "");

        if (current != null && GameInput.Interact())
            current.Interact();
    }

    Interactable FindBest()
    {
        Interactable best = null;
        float bestDist = float.MaxValue;
        Vector2 pos = transform.position;

        foreach (var c in Physics2D.OverlapCircleAll(pos, radius))
        {
            var it = c.GetComponentInParent<Interactable>();
            if (it == null || !it.CanInteract) continue;
            float d = Vector2.Distance(pos, c.ClosestPoint(pos));
            if (d < bestDist)
            {
                bestDist = d;
                best = it;
            }
        }
        return best;
    }
}
