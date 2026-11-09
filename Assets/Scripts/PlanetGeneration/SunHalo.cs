using UnityEngine;

// Soft glow around the sun. Put on a Quad that is a child of the Sun sphere.
// It draws its own round gradient texture, so no image file is needed.
[RequireComponent(typeof(MeshRenderer))]
public class SunHalo : MonoBehaviour
{
    public Camera cam;
    public Color color = new Color(1f, 0.85f, 0.5f, 1f);
    [Tooltip("Higher = the glow fades faster toward the edge")]
    [Range(0.5f, 6f)] public float falloff = 2.5f;
    public int textureSize = 128;

    void Start()
    {
        if (cam == null) cam = Camera.main;

        var r = GetComponent<MeshRenderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;

        // A quad comes with a collider; remove it so the player's ground check ignores it
        var col = GetComponent<Collider>();
        if (col != null) Destroy(col);

        Material mat = r.material;                       // our own copy of the material
        mat.SetTexture("_BaseMap", BuildTexture());      // URP texture slot
        mat.SetColor("_BaseColor", color);               // URP color slot
    }

    // White circle whose alpha fades from 1 at the centre to 0 at the edge
    Texture2D BuildTexture()
    {
        var tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float u = (x + 0.5f) / textureSize * 2f - 1f;    // -1..1
                float v = (y + 0.5f) / textureSize * 2f - 1f;
                float d = Mathf.Sqrt(u * u + v * v);             // distance from centre
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), falloff);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return tex;
    }

    // Always face the camera (a "billboard")
    void LateUpdate()
    {
        if (cam != null) transform.rotation = cam.transform.rotation;
    }
}