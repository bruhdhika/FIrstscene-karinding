using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Overlay hitam penuh layar. Dipakai untuk fade in di awal dan
// fade out layar di akhir cutscene.
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFade : MonoBehaviour
{
    public bool startOpaque = true;
    public Color overlayColor = Color.black;

    CanvasGroup group;
    Image image;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        image = GetComponent<Image>();

        if (image != null)
        {
            image.color = overlayColor;
            image.raycastTarget = false;
        }

        group.blocksRaycasts = false;
        group.interactable = false;

        var rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        if (startOpaque) group.alpha = 1f;
    }

    public void SetAlpha(float alpha)
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        if (group != null) group.alpha = Mathf.Clamp01(alpha);
    }

    public void SetBlack()
    {
        SetAlpha(1f);
    }

    public IEnumerator FadeIn(float seconds)
    {
        yield return FadeTo(0f, seconds);
    }

    public IEnumerator FadeOut(float seconds)
    {
        yield return FadeTo(1f, seconds);
    }

    public IEnumerator FadeTo(float target, float seconds)
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        if (group == null) yield break;

        float from = group.alpha;
        target = Mathf.Clamp01(target);
        if (seconds <= 0f)
        {
            group.alpha = target;
            yield break;
        }

        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / seconds);
            group.alpha = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, k));
            yield return null;
        }
        group.alpha = target;
    }
}
