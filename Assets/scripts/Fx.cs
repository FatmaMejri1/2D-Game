using UnityEngine;

/// <summary>Tiny particle / floating-text effects built from sprites (no assets needed).</summary>
public class Fx : MonoBehaviour
{
    static Font font;

    SpriteRenderer sr;
    TextMesh tm;
    Vector2 velocity;
    float life;
    float maxLife;
    float startSize;
    Color baseColor;
    bool isText;

    public static void Burst(Vector2 pos, Color color, int count = 10, float speed = 3f, float size = 0.18f)
    {
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Fx");
            if (GameManager.Instance != null) go.transform.SetParent(GameManager.Instance.transform, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(size, size, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = LevelSetupHelper.Circle();
            renderer.color = color;
            renderer.sortingOrder = 15;

            var fx = go.AddComponent<Fx>();
            fx.sr = renderer;
            fx.baseColor = color;
            fx.startSize = size;
            fx.velocity = Random.insideUnitCircle.normalized * Random.Range(0.4f, 1f) * speed;
            fx.life = Random.Range(0.35f, 0.7f);
            fx.maxLife = fx.life;
        }
    }

    public static void Popup(Vector2 pos, string text, Color color, float size = 0.5f)
    {
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var go = new GameObject("Popup");
        if (GameManager.Instance != null) go.transform.SetParent(GameManager.Instance.transform, false);
        go.transform.position = new Vector3(pos.x, pos.y + 0.4f, 0f);

        var mesh = go.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.font = font;
        mesh.fontSize = 64;
        mesh.characterSize = size * 10f / 64f;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = color;
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = font.material;
        mr.sortingOrder = 30;

        var fx = go.AddComponent<Fx>();
        fx.tm = mesh;
        fx.isText = true;
        fx.baseColor = color;
        fx.velocity = new Vector2(0f, 1.4f);
        fx.life = 1.1f;
        fx.maxLife = 1.1f;
    }

    void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(velocity * Time.deltaTime);
        float k = life / maxLife;

        if (isText)
        {
            Color c = baseColor;
            c.a = Mathf.Clamp01(k * 2f);
            tm.color = c;
        }
        else
        {
            velocity *= Mathf.Max(0f, 1f - 3f * Time.deltaTime);
            Color c = baseColor;
            c.a = baseColor.a * k;
            sr.color = c;
            float s = startSize * Mathf.Lerp(0.3f, 1f, k);
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
