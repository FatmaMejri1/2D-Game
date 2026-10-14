using UnityEngine;

/// <summary>A piece of floating text in the world (room names, door labels).</summary>
public class WorldLabel : MonoBehaviour
{
    public string text = "";
    public float height = 0.4f;
    public Color color = Color.white;

    void Awake()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var tm = GetComponent<TextMesh>();
        if (tm == null) tm = gameObject.AddComponent<TextMesh>();
        tm.text = text;
        tm.font = font;
        tm.fontSize = 64;
        tm.characterSize = height * 10f / 64f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        var mr = GetComponent<MeshRenderer>();
        mr.sharedMaterial = font.material;
        mr.sortingOrder = 20;
    }
}
