using UnityEngine;

/// <summary>The glowing portal at the end of the level.</summary>
public class ExitZone : MonoBehaviour
{
    SpriteRenderer sr;
    Color baseColor = Color.white;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    void Update()
    {
        if (sr == null) return;
        Color c = baseColor;
        c.a = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 4f);
        sr.color = c;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerMovement>() != null && GameManager.Instance != null)
            GameManager.Instance.Win();
    }
}
