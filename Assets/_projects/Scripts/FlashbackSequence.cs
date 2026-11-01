using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Satu baris dialog
[Serializable]
public class DialogueLine
{
    [Tooltip("Harus sama dengan Abah Name atau Rimba Name")]
    public string speaker;

    [TextArea(2, 4)]
    public string text;

    [Tooltip("Detik dialog tetap tampil setelah teks selesai diketik")]
    public float holdSeconds = 2f;
}

// Satu bagian flashback: putar frame X sampai Y, lalu (opsional) berhenti untuk dialog
[Serializable]
public class FlashbackSegment
{
    public string label = "Bagian";          // hanya catatan untuk Anda

    [Header("Frame yang diputar")]
    public int startFrame = 0;               // angka dari nama Frame_0, Frame_1, ...
    public int endFrame = 0;
    public float secondsPerFrame = 1f;       // tiap frame tampil berapa detik
    public int repeatCount = 1;              // diulang berapa kali (1 = sekali)

    [Header("Setelah frame terakhir")]
    public float holdLastFrame = 1f;         // tahan frame terakhir sebelum dialog
    public List<DialogueLine> lines = new List<DialogueLine>(); // kosong = tanpa dialog

    [Header("Getar kamera (opsional)")]
    public float shakeSeconds = 0f;
    [Tooltip("Amplitudo dalam piksel seni (bukan piksel layar)")]
    public float shakeStrengthPx = 0f;
}

public class FlashbackSequence : MonoBehaviour
{
    [Header("Flashback")]
    public SpriteRenderer flashbackRenderer;
    public Sprite[] frames;                  // semua Frame_0 ... Frame_N
    public float startDelay = 0f;

    [Header("Karakter")]
    public CharacterPose abah;
    public CharacterPose rimba;
    public string abahName = "Abah";
    public string rimbaName = "Rimba";
    public float turnDelay = 1f;             // jeda setelah menoleh / kembali menonton

    [Header("Dialog")]
    public GameObject dialoguePanel;
    public CanvasGroup panelGroup;
    public TMP_Text speakerLabel;
    public TMP_Text bodyLabel;
    public Image indicatorImage;
    public CanvasGroup indicatorGroup;
    public float charsPerSecond = 40f;
    public float gapBetweenLines = 0.3f;
    [Tooltip("false = karakter tetap saling menatap setelah dialog")]
    public bool returnToWatchAfterDialogue = false;

    [Header("Polesan kotak dialog")]
    public float panelShowSeconds = 0.12f;
    public float panelHideSeconds = 0.1f;
    public float panelRisePx = 12f;
    public float indicatorBlinkSeconds = 0.45f;
    public Color abahSpeakerColor = new Color(0.957f, 0.722f, 0.376f, 1f); // #F4B860
    public Color rimbaSpeakerColor = new Color(0.494f, 0.839f, 0.784f, 1f); // #7ED6C8

    [Header("Jeda tanda baca (detik)")]
    public float commaPause = 0.12f;
    public float sentencePause = 0.28f;

    [Header("Kamera")]
    public Camera targetCamera;

    [Header("Audio")]
    public CutsceneAudio cutsceneAudio;

    [Header("Urutan cerita")]
    public List<FlashbackSegment> segments = new List<FlashbackSegment>();

    [Header("Selesai")]
    public float endDelay = 2.5f;
    public string nextSceneName;
    public ScreenFade screenFade;
    public float fadeInSeconds = 1f;
    public float screenFadeOutSeconds = 1f;
    public float musicFadeOutSeconds = 3f;

    Dictionary<int, Sprite> frameMap = new Dictionary<int, Sprite>();
    bool facing;
    int maxFrame = -1;
    Vector2 panelBasePos;
    RectTransform panelRect;
    Coroutine indicatorRoutine;
    Sprite indicatorSprite;

    void Start()
    {
        BuildFrameMap();
        ValidateSegments();
        SetAllWatch();
        facing = false;

        if (segments.Count > 0 && segments[0] != null) ShowFrame(segments[0].startFrame);

        if (targetCamera == null) targetCamera = Camera.main;
        if (cutsceneAudio == null) cutsceneAudio = GetComponent<CutsceneAudio>();
        if (screenFade == null) screenFade = UnityEngine.Object.FindFirstObjectByType<ScreenFade>();

        PreparePanel();
        PrepareIndicator();

        // Fade in berjalan paralel, tidak menggeser timeline cerita.
        if (screenFade != null) StartCoroutine(screenFade.FadeIn(fadeInSeconds));
        if (cutsceneAudio != null) cutsceneAudio.Begin();

        StartCoroutine(Run());
    }

    void PreparePanel()
    {
        if (dialoguePanel == null) return;
        dialoguePanel.SetActive(true);
        panelRect = dialoguePanel.GetComponent<RectTransform>();
        if (panelGroup == null) panelGroup = dialoguePanel.GetComponent<CanvasGroup>();
        if (panelGroup == null) panelGroup = dialoguePanel.AddComponent<CanvasGroup>();
        panelGroup.blocksRaycasts = false;
        panelGroup.interactable = false;
        panelGroup.alpha = 0f;
        if (panelRect != null)
        {
            panelBasePos = panelRect.anchoredPosition;
            panelRect.anchoredPosition = panelBasePos + Vector2.down * panelRisePx;
        }
    }

    void PrepareIndicator()
    {
        if (indicatorGroup == null && indicatorImage != null)
            indicatorGroup = indicatorImage.GetComponent<CanvasGroup>();
        if (indicatorGroup != null) indicatorGroup.alpha = 0f;

        if (indicatorImage != null && indicatorImage.sprite == null)
        {
            indicatorSprite = MakeTriangleSprite();
            indicatorImage.sprite = indicatorSprite;
            indicatorImage.preserveAspect = true;
            indicatorImage.raycastTarget = false;
        }
    }

    // Membaca angka dari nama sprite (Frame_12 -> 12), jadi urutan drag tidak masalah
    void BuildFrameMap()
    {
        frameMap.Clear();
        maxFrame = -1;
        if (frames == null) return;
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] == null) continue;
            string n = frames[i].name;
            int us = n.LastIndexOf('_');
            int number = i;
            if (us >= 0) int.TryParse(n.Substring(us + 1), out number);
            frameMap[number] = frames[i];
            if (number > maxFrame) maxFrame = number;
        }
    }

    static int ParseFrameNumber(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName) || !spriteName.StartsWith("Frame_"))
            return -1;
        int number;
        return int.TryParse(spriteName.Substring(6), out number) ? number : -1;
    }

    // Pengaman permanen: jangan diam-diam memutar segment yang frame-nya tidak ada.
    void ValidateSegments()
    {
        var issues = new List<string>();

        if (frames == null || frames.Length != 61)
            issues.Add("array frames harus 61 sprite Frame_0..Frame_60, sekarang " +
                       (frames == null ? 0 : frames.Length) + ".");

        var seen = new HashSet<Sprite>();
        if (frames != null)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                Sprite sprite = frames[i];
                if (sprite == null)
                {
                    issues.Add("frames[" + i + "] null/Missing.");
                    continue;
                }
                if (!seen.Add(sprite))
                    issues.Add("sprite duplikat: " + sprite.name + ".");
                int number = ParseFrameNumber(sprite.name);
                if (number < 0)
                    issues.Add("nama sprite bukan Frame_N: " + sprite.name + ".");
            }
        }

        if (segments != null)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                FlashbackSegment seg = segments[i];
                if (seg == null) continue;
                int start = Mathf.Max(0, seg.startFrame);
                int end = Mathf.Max(start, seg.endFrame);
                for (int f = start; f <= end; f++)
                {
                    if (!frameMap.ContainsKey(f))
                        issues.Add("segment " + i + " (" + seg.label + ") kehilangan Frame_" + f + ".");
                }
            }
        }

        if (issues.Count > 0)
            Debug.LogError("FlashbackSequence: " + string.Join(" | ", issues));
    }

#if UNITY_EDITOR
    [ContextMenu("Isi Frames Otomatis")]
    void AutoFillFramesEditor()
    {
        const string folder = "Assets/_projects/Art/Flashback";
        var byNumber = new SortedDictionary<int, Sprite>();

        foreach (string path in AssetDatabase.GetAllAssetPaths())
        {
            if (!path.StartsWith(folder + "/", StringComparison.Ordinal)) continue;
            if (!path.EndsWith(".aseprite", StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (UnityEngine.Object asset in assets)
            {
                Sprite sprite = asset as Sprite;
                if (sprite == null) continue;
                int number = ParseFrameNumber(sprite.name);
                if (number < 0 || byNumber.ContainsKey(number)) continue;
                byNumber.Add(number, sprite);
            }
        }

        frames = new Sprite[byNumber.Count];
        int index = 0;
        foreach (KeyValuePair<int, Sprite> pair in byNumber)
            frames[index++] = pair.Value;

        EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        Debug.Log("FlashbackSequence: Isi Frames Otomatis -> " + frames.Length +
                  " sprite Frame_N dari " + folder + ".");
    }
#endif

    void ShowFrame(int number)
    {
        if (flashbackRenderer == null) return;
        Sprite s;
        if (frameMap.TryGetValue(number, out s))
        {
            if (flashbackRenderer.sprite != s) flashbackRenderer.sprite = s;
        }
        else
        {
            Debug.LogError("Frame_" + number + " tidak ada di daftar Frames.");
        }
    }

    IEnumerator Run()
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        for (int i = 0; i < segments.Count; i++)
        {
            FlashbackSegment seg = segments[i];
            if (seg == null) continue;

            if (seg.shakeSeconds > 0f && seg.shakeStrengthPx > 0f)
                StartCoroutine(ShakeCamera(seg.shakeSeconds, seg.shakeStrengthPx));

            // Naikkan musik saat frame berputar lagi setelah dialog.
            if (i > 0 && cutsceneAudio != null) cutsceneAudio.MusicToFlashback();

            yield return StartCoroutine(PlayFrames(seg));

            if (seg.lines != null && seg.lines.Count > 0)
            {
                if (seg.holdLastFrame > 0f) yield return new WaitForSeconds(seg.holdLastFrame);
                if (cutsceneAudio != null) cutsceneAudio.MusicToDialogue();
                yield return StartCoroutine(PlayDialogue(seg));
            }
        }

        if (endDelay > 0f) yield return new WaitForSeconds(endDelay);
        yield return StartCoroutine(Finish());
    }

    IEnumerator PlayFrames(FlashbackSegment seg)
    {
        int start = Mathf.Max(0, seg.startFrame);
        int end = Mathf.Max(start, seg.endFrame);

        if (maxFrame >= 0)
        {
            if (start > maxFrame) start = maxFrame;
            if (end > maxFrame) end = maxFrame;
            if (end < start) end = start;
        }

        int repeats = Mathf.Max(1, seg.repeatCount);
        for (int r = 0; r < repeats; r++)
        {
            for (int f = start; f <= end; f++)
            {
                ShowFrame(f);
                if (seg.secondsPerFrame > 0f) yield return new WaitForSeconds(seg.secondsPerFrame);
                else yield return null;
            }
        }
    }

    IEnumerator PlayDialogue(FlashbackSegment seg)
    {
        // Keduanya menoleh hanya sekali, saat dialog pertama.
        if (!facing)
        {
            SetAllFace();
            facing = true;
            if (turnDelay > 0f) yield return new WaitForSeconds(turnDelay);
        }

        foreach (DialogueLine line in seg.lines)
        {
            if (line == null) continue;

            // Hentikan mulut baris sebelumnya.
            if (abah != null) abah.Face();
            if (rimba != null) rimba.Face();

            CharacterPose speaker = GetSpeaker(line.speaker);
            SetSpeakerFocus(speaker);

            if (speakerLabel != null)
            {
                speakerLabel.text = line.speaker;
                speakerLabel.color = SpeakerColor(line.speaker);
            }

            if (bodyLabel != null)
            {
                bodyLabel.text = line.text;
                bodyLabel.maxVisibleCharacters = 0;
                bodyLabel.ForceMeshUpdate();
            }

            if (indicatorGroup != null) indicatorGroup.alpha = 0f;
            if (panelGroup != null) StartCoroutine(ShowPanel());

            if (speaker != null) speaker.StartTalking();
            if (cutsceneAudio != null && speaker != null) cutsceneAudio.StartVoice(speaker == abah);

            int total = bodyLabel != null ? bodyLabel.textInfo.characterCount : 0;
            float shown = 0f;
            int revealed = 0;
            float cps = Mathf.Max(1f, charsPerSecond);

            while (revealed < total)
            {
                shown += cps * Time.deltaTime;
                int target = Mathf.Min(total, Mathf.FloorToInt(shown));
                float pause = 0f;
                if (target > revealed)
                {
                    for (int i = revealed; i < target; i++)
                    {
                        char c = bodyLabel.textInfo.characterInfo[i].character;
                        if (c == ',' || c == ';' || c == ':') pause = Mathf.Max(pause, commaPause);
                        else if (c == '.' || c == '?' || c == '!') pause = Mathf.Max(pause, sentencePause);
                    }
                    revealed = target;
                    bodyLabel.maxVisibleCharacters = revealed;
                }
                if (pause > 0f) yield return new WaitForSeconds(pause);
                yield return null;
            }

            if (bodyLabel != null) bodyLabel.maxVisibleCharacters = total;
            if (cutsceneAudio != null && speaker != null) cutsceneAudio.StopVoice();
            if (speaker != null) speaker.Face();

            // Penanda ▼ hanya setelah teks selesai.
            if (indicatorGroup != null)
            {
                if (indicatorRoutine != null) StopCoroutine(indicatorRoutine);
                indicatorRoutine = StartCoroutine(BlinkIndicator());
            }

            if (line.holdSeconds > 0f) yield return new WaitForSeconds(line.holdSeconds);

            StopIndicator();
            yield return StartCoroutine(HidePanel());

            if (gapBetweenLines > 0f) yield return new WaitForSeconds(gapBetweenLines);
        }
    }

    IEnumerator ShowPanel()
    {
        if (panelGroup == null || panelRect == null) yield break;

        Vector2 from = panelBasePos + Vector2.down * panelRisePx;
        panelRect.anchoredPosition = from;
        panelGroup.alpha = 0f;

        if (panelShowSeconds <= 0f)
        {
            panelGroup.alpha = 1f;
            panelRect.anchoredPosition = panelBasePos;
            yield break;
        }

        float t = 0f;
        while (t < panelShowSeconds)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / panelShowSeconds));
            panelGroup.alpha = k;
            panelRect.anchoredPosition = Vector2.Lerp(from, panelBasePos, k);
            yield return null;
        }
        panelGroup.alpha = 1f;
        panelRect.anchoredPosition = panelBasePos;
    }

    IEnumerator HidePanel()
    {
        if (panelGroup == null) yield break;
        float from = panelGroup.alpha;
        if (panelHideSeconds <= 0f)
        {
            panelGroup.alpha = 0f;
            yield break;
        }
        float t = 0f;
        while (t < panelHideSeconds)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / panelHideSeconds));
            panelGroup.alpha = Mathf.Lerp(from, 0f, k);
            yield return null;
        }
        panelGroup.alpha = 0f;
    }

    IEnumerator BlinkIndicator()
    {
        if (indicatorGroup == null) yield break;
        float on = Mathf.Max(0.05f, indicatorBlinkSeconds);
        while (true)
        {
            indicatorGroup.alpha = 1f;
            yield return new WaitForSeconds(on);
            indicatorGroup.alpha = 0f;
            yield return new WaitForSeconds(on);
        }
    }

    void StopIndicator()
    {
        if (indicatorRoutine != null)
        {
            StopCoroutine(indicatorRoutine);
            indicatorRoutine = null;
        }
        if (indicatorGroup != null) indicatorGroup.alpha = 0f;
    }

    IEnumerator Finish()
    {
        Coroutine musicFade = null;
        if (cutsceneAudio != null)
            musicFade = StartCoroutine(cutsceneAudio.FadeOutMusic(musicFadeOutSeconds));

        if (screenFade != null)
            yield return StartCoroutine(screenFade.FadeOut(screenFadeOutSeconds));
        else
            yield return new WaitForSeconds(screenFadeOutSeconds);

        if (musicFade != null) yield return musicFade;
        if (cutsceneAudio != null) cutsceneAudio.StopAll();

        if (!string.IsNullOrEmpty(nextSceneName)) SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator ShakeCamera(float seconds, float strengthPx)
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null || strengthPx <= 0f) yield break;

        Vector3 basePos = targetCamera.transform.position;
        float ppu = 100f;
        if (flashbackRenderer != null && flashbackRenderer.sprite != null)
            ppu = Mathf.Max(1f, flashbackRenderer.sprite.pixelsPerUnit);
        float artPixel = 1f / ppu;

        float t = 0f;
        while (t < seconds)
        {
            // Amplitudo dalam piksel seni, diskalakan ke grid piksel seni.
            float x = Mathf.Round(UnityEngine.Random.Range(-strengthPx, strengthPx));
            float y = Mathf.Round(UnityEngine.Random.Range(-strengthPx, strengthPx));
            targetCamera.transform.position = basePos + new Vector3(x * artPixel, y * artPixel, 0f);
            t += Time.deltaTime;
            yield return null;
        }
        targetCamera.transform.position = basePos;
    }

    CharacterPose GetSpeaker(string name)
    {
        if (name == abahName) return abah;
        if (name == rimbaName) return rimba;
        return null;
    }

    Color SpeakerColor(string name)
    {
        if (name == abahName) return abahSpeakerColor;
        if (name == rimbaName) return rimbaSpeakerColor;
        return Color.white;
    }

    void SetSpeakerFocus(CharacterPose speaker)
    {
        if (abah != null) abah.SetFocus(speaker == abah);
        if (rimba != null) rimba.SetFocus(speaker == rimba);
    }

    void SetAllWatch()
    {
        if (abah != null) abah.Watch();
        if (rimba != null) rimba.Watch();
    }

    void SetAllFace()
    {
        if (abah != null) abah.Face();
        if (rimba != null) rimba.Face();
    }

    // Segitiga putih kecil untuk penanda ▼ (tanpa bergantung glyph font).
    static Sprite MakeTriangleSprite()
    {
        const int w = 24, h = 24;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.name = "DialogueIndicator Triangle";
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[w * h];
        var clear = new Color32(255, 255, 255, 0);
        var white = new Color32(255, 255, 255, 255);
        for (int i = 0; i < px.Length; i++) px[i] = clear;
        for (int y = 0; y < h; y++)
        {
            float t = y / (float)(h - 1);
            int half = Mathf.RoundToInt(t * (w - 1) * 0.5f);
            int cx = w / 2;
            for (int x = cx - half; x <= cx + half; x++)
            {
                if (x >= 0 && x < w) px[y * w + x] = white;
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false, false);
        var s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        s.name = "DialogueIndicator";
        return s;
    }
}
