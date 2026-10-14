using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    public Transform target;
    public float smoothTime = 0.15f;
    // World area the camera may show (matches the level built by LevelSetupHelper).
    public Rect bounds = new Rect(-1f, -6f, 82f, 12f);

    Camera cam;
    Vector3 velocity;
    Vector3 basePos;
    float shakeUntil;
    float shakeMagnitude;

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        basePos = transform.position;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = Clamp(new Vector3(target.position.x, target.position.y, -10f));
        basePos = Vector3.SmoothDamp(basePos, desired, ref velocity, smoothTime);

        Vector3 offset = Vector3.zero;
        if (Time.unscaledTime < shakeUntil)
            offset = (Vector3)(Random.insideUnitCircle * shakeMagnitude);
        transform.position = basePos + offset;
    }

    public void Snap()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (target == null) return;
        basePos = Clamp(new Vector3(target.position.x, target.position.y, -10f));
        transform.position = basePos;
        velocity = Vector3.zero;
    }

    public void Shake(float magnitude, float duration)
    {
        shakeMagnitude = magnitude;
        shakeUntil = Time.unscaledTime + duration;
    }

    Vector3 Clamp(Vector3 p)
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        float minX = bounds.xMin + halfW, maxX = bounds.xMax - halfW;
        float minY = bounds.yMin + halfH, maxY = bounds.yMax - halfH;
        p.x = minX > maxX ? bounds.center.x : Mathf.Clamp(p.x, minX, maxX);
        p.y = minY > maxY ? bounds.center.y : Mathf.Clamp(p.y, minY, maxY);
        p.z = -10f;
        return p;
    }
}
