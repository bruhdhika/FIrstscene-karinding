using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class _FontProbe
{
    static readonly string[] Lines =
    {
        "Abah, ceritakan lagi asal-usul karinding itu, dong.",
        "Konon, sebelum ada kehidupan, tak ada apa-apa. Awang uwung tong enong marga na enong, Kosong.",
        "Hingga suatu ketika, dari kekosongan itu lahir getar, dan getar melahirkan dengung.",
        "Dari dengung itu, dunia mulai bergerak.",
        "Itu dunia kita, Bah?",
        "Betul, Cu. Itulah bumi, yang lahir dari dengung.",
        "Gunung berdiri, air mengalir turun menuju laut.",
        "Dan kehidupan pun tumbuh di antara keduanya.",
        "Karuhun kita memahami ini bukan lewat kitab, tapi lewat alam itu sendiri.",
        "Alam? Maksud Abah, hutan?",
        "Mereka membagi hutan menjadi tiga.",
        "Leuweung Larangan adalah tempat yang disucikan.",
        "Leuweung Tutupan, tempat kita belajar, lalu lahirlah kesadaran.",
        "Leuweung Baladahan, tempat kita berladang, lalu munculnya kesabaran.",
        "Yakin, Sadar, Sabar.",
        "Yakin, sadar, sabar...",
        "Tiga hal yang dijaga turun-temurun, dirawat dari generasi ke generasi,",
        "seperti batas yang ditetapkan lalu dipelihara, netepkeun lalu netepan.",
        "Sebab apa yang tak dirawat akan hilang, dan apa yang hilang akan dicari.",
        "Tiga hal yang sama, yang kelak dituangkan di dalam sebilah bambu kecil.",
        "Digenggam dengan yakin, dipukul dengan sabar, dan didengarkan dengan sadar.",
        "Karena suara yang keluar darinya bukan sekadar suara.",
        "Ia gema dari getar pertama yang melahirkan dunia.",
    };

    [MenuItem("Tools/_Probe Font VT323")]
    public static void Run()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_projects/Fonts/VT323 SDF.asset");
        Debug.Log("PROBE font=" + (font == null ? "NULL" : font.name) + " hasLowerA=" + (font != null && font.HasCharacter('a')) + " hasUpperA=" + (font != null && font.HasCharacter('A')));
        if (font == null) return;

        var go = new GameObject("FontProbeCanvas", typeof(Canvas), typeof(CanvasScaler));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        var textGo = new GameObject("ProbeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        var tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Truncate;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.rectTransform.anchorMin = new Vector2(0f, 1f);
        tmp.rectTransform.anchorMax = new Vector2(0f, 1f);
        tmp.rectTransform.pivot = new Vector2(0f, 1f);
        tmp.rectTransform.sizeDelta = new Vector2(820f, 120f);

        foreach (float size in new[] { 28f, 29f, 30f, 31f, 32f })
        {
            foreach (float tracking in new[] { 0f, -1f, -2f })
            {
                tmp.fontSize = size;
                tmp.characterSpacing = tracking;
                tmp.lineSpacing = 0f;
                int maxChars = 0;
                int maxLines = 0;
                float maxHeight = 0f;
                foreach (string line in Lines)
                {
                    tmp.text = line;
                    tmp.ForceMeshUpdate();
                    maxLines = Mathf.Max(maxLines, tmp.textInfo.lineCount);
                    maxHeight = Mathf.Max(maxHeight, tmp.preferredHeight);
                    for (int i = 0; i < tmp.textInfo.lineCount; i++)
                        maxChars = Mathf.Max(maxChars, tmp.textInfo.lineInfo[i].characterCount);
                }
                var sb = new StringBuilder();
                sb.Append("PROBE size=").Append(size).Append(" tracking=").Append(tracking)
                  .Append(" maxLines=").Append(maxLines)
                  .Append(" maxCharsPerLine=").Append(maxChars)
                  .Append(" maxPreferredHeight=").Append(maxHeight.ToString("F2"))
                  .Append(" lineHeight=").Append(tmp.textInfo.lineInfo[0].lineHeight.ToString("F2"));
                Debug.Log(sb.ToString());
            }
        }

        Object.DestroyImmediate(go);
        AssetDatabase.SaveAssets();
    }
}
