using UnityEngine;

// Membuat tepi flashback memudar halus ke hitam.
// Sprite hitam 256x144 dibuat saat runtime (tidak menyentuh file sumber).
[RequireComponent(typeof(SpriteRenderer))]
public class FlashbackFeather : MonoBehaviour
{
    [Tooltip("Overscan kecil supaya tidak ada garis tepi keras")]
    public float overscan = 0.015f;

    [Tooltip("Lebar area pudar (fraksi lebar gambar)")]
    public float featherX = 0.12f;

    [Tooltip("Tinggi area pudar (fraksi tinggi gambar)")]
    public float featherY = 0.18f;

    public int textureWidth = 256;
    public int textureHeight = 144;

    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        Build();
    }

    void Build()
    {
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        var tex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        tex.name = "FlashbackFeather Texture";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        var px = new Color32[textureWidth * textureHeight];
        for (int y = 0; y < textureHeight; y++)
        {
            float v = (y + 0.5f) / textureHeight;
            for (int x = 0; x < textureWidth; x++)
            {
                float u = (x + 0.5f) / textureWidth;
                float edge = Mathf.Min(Mathf.Min(u, 1f - u) / Mathf.Max(0.0001f, featherX),
                                       Mathf.Min(v, 1f - v) / Mathf.Max(0.0001f, featherY));
                float t = Mathf.Clamp01(edge);
                float a = 1f - Mathf.SmoothStep(0f, 1f, t);
                px[y * textureWidth + x] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false, false);

        var sprite = Sprite.Create(tex, new Rect(0f, 0f, textureWidth, textureHeight),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        sprite.name = "FlashbackFeather Sprite";

        sr.sprite = sprite;
        sr.color = Color.white;
        sr.sortingLayerName = "Flashback";
        sr.sortingOrder = 1;
        sr.flipX = false;
        sr.flipY = false;

        // Parent memakai pivot bottom-center. Letakkan feather di tengah gambar.
        var parentSr = transform.parent != null ? transform.parent.GetComponent<SpriteRenderer>() : null;
        float imgW = 3.2f, imgH = 1.8f;
        if (parentSr != null && parentSr.sprite != null)
        {
            var size = parentSr.sprite.bounds.size;
            imgW = size.x;
            imgH = size.y;
        }

        float texW = textureWidth / 100f;
        float texH = textureHeight / 100f;
        float scale = Mathf.Max(imgW * (1f + overscan) / texW, imgH * (1f + overscan) / texH);

        transform.localPosition = new Vector3(0f, imgH * 0.5f, 0f);
        transform.localRotation = Quaternion.identity;
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
