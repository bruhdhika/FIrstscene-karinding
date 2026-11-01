using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class _FinalRevise
{
    const string ScenePath = "Assets/Opening_Cutscene.unity";
    const string FlashbackFolder = "Assets/_projects/Art/Flashback";
    const string GeneratedFolder = "Assets/_projects/Art/Flashback/Segments";
    const string FontPath = "Assets/_projects/Fonts/VT323 SDF.asset";

    static readonly string Marker = Path.Combine(
        Directory.GetParent(Application.dataPath).FullName, "Temp", "_final_revise_done.txt");

    static _FinalRevise()
    {
        EditorApplication.delayCall += TryRun;
    }

    static void TryRun()
    {
        if (File.Exists(Marker)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryRun;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            EditorApplication.delayCall += TryRun;
            return;
        }

        Run();
    }

    static void Run()
    {
        try
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                Debug.LogError("_FinalRevise: scene aktif bukan " + ScenePath + " (aktif: " + scene.path + ").");
                File.WriteAllText(Marker, "wrong-scene");
                return;
            }

            ConfigureImporters();
            ApplyLayout(scene);
            ApplyFont();
            ApplyStory(scene);
            FillFrames(scene);
            MeasureText();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            File.WriteAllText(Marker, "done " + DateTime.Now.ToString("O"));
            Debug.Log("_FinalRevise: selesai dan scene disimpan.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            File.WriteAllText(Marker, "error " + ex.Message);
        }
    }

    static void ConfigureImporters()
    {
        // Sumber 49 frame: naikkan batas atlas dan pastikan tanpa kompresi / point.
        var flash = AssetImporter.GetAtPath(FlashbackFolder + "/flashback.aseprite") as TextureImporter;
        if (flash != null)
        {
            flash.textureType = TextureImporterType.Sprite;
            flash.spriteImportMode = SpriteImportMode.Multiple;
            flash.spritePixelsPerUnit = 100f;
            flash.filterMode = FilterMode.Point;
            flash.textureCompression = TextureImporterCompression.Uncompressed;
            flash.maxTextureSize = 4096;
            flash.mipmapEnabled = false;
            flash.alphaIsTransparency = true;
            flash.wrapMode = TextureWrapMode.Clamp;
            flash.npotScale = TextureImporterNPOTScale.None;
            var platform = flash.GetPlatformTextureSettings("DefaultTexturePlatform");
            platform.overridden = true;
            platform.maxTextureSize = 4096;
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            flash.SetPlatformTextureSettings(platform);
            flash.SaveAndReimport();
            Debug.Log("_FinalRevise: flashback.aseprite maxTextureSize=4096, compression=None, filter=Point.");
        }
        else
        {
            Debug.LogError("_FinalRevise: TextureImporter flashback.aseprite tidak ditemukan.");
        }

        for (int i = 49; i <= 60; i++)
        {
            string path = GeneratedFolder + "/Frame_" + i + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }
            if (importer == null)
            {
                Debug.LogError("_FinalRevise: importer tidak ditemukan untuk " + path);
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 512;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0.5f, 0f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("_FinalRevise: Frame_49..60 diimpor sebagai sprite 320x180, pivot bawah.");
    }

    static void ApplyLayout(Scene scene)
    {
        var cam = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 540f / (3.5f * 100f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        var flash = GameObject.Find("FlashbackScreen");
        if (flash != null)
        {
            flash.transform.position = new Vector3(0f, (20f / 350f) - 0.9f, 0f);
            flash.transform.localScale = Vector3.one;
            var sr = flash.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingLayerName = "Flashback";
                sr.sortingOrder = 0;
            }
        }

        SetCharacter("Abah", -0.8f, -0.7571429f, 1.1142857f / 10.68f);
        SetCharacter("Rimba", 0.8f, -0.7571429f, 1.1142857f / 7.83f);

        var panelGo = GameObject.Find("DialoguePanel");
        if (panelGo != null)
        {
            var rt = panelGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 20.82f);
            rt.sizeDelta = new Vector2(982.63f, 333.80f);

            var img = panelGo.GetComponent<Image>();
            if (img != null)
            {
                img.preserveAspect = false;
                img.type = Image.Type.Simple;
                img.pixelsPerUnitMultiplier = 1f;
            }
        }

        ConfigureText("SpeakerText", new Vector2(81.08f, -93.62f), new Vector2(820f, 34f), 32f, 0f);
        ConfigureText("BodyText", new Vector2(81.08f, -135.62f), new Vector2(820f, 77f), 28f,
            6f / (28f * 0.01f));

        var indicator = GameObject.Find("DialogueIndicator");
        if (indicator != null)
        {
            var rt = indicator.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            // Pusat penanda: 48 px dari kanan dan 30 px dari bawah kotak.
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-65.08f, 117.18f);
            rt.sizeDelta = new Vector2(30f, 30f);
        }

        var scaler = GameObject.Find("Canvas")?.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
        }

        Debug.Log("_FinalRevise layout: camSize=1.542857, flashback 1120x630 @ x400-1520/y205-835, " +
                  "karakter 680/1240 base y805 tinggi 390, panel 948x187 pad 64/34/8/34.");
    }

    static void SetCharacter(string name, float x, float y, float scale)
    {
        var go = GameObject.Find(name);
        if (go == null)
        {
            Debug.LogError("_FinalRevise: karakter " + name + " tidak ditemukan.");
            return;
        }
        go.SetActive(true);
        go.transform.position = new Vector3(x, y, 0f);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Characters";
            sr.sortingOrder = 10;
        }
        var pose = go.GetComponent<CharacterPose>();
        if (pose == null || pose.watchSprite == null || pose.faceSprite == null || pose.talkSprite == null)
            Debug.LogError("_FinalRevise: sprite pose " + name + " ada yang null.");
    }

    static void ConfigureText(string name, Vector2 pos, Vector2 size, float fontSize, float lineSpacing)
    {
        var go = GameObject.Find(name);
        if (go == null)
        {
            Debug.LogError("_FinalRevise: " + name + " tidak ditemukan.");
            return;
        }
        var tmp = go.GetComponent<TMP_Text>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        tmp.fontSize = fontSize;
        tmp.enableAutoSizing = false;
        tmp.characterSpacing = 0f;
        tmp.lineSpacing = lineSpacing;
        tmp.paragraphSpacing = 0f;
        tmp.wordSpacing = 0f;
        tmp.margin = Vector4.zero;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Truncate;
        tmp.fontStyle = FontStyles.Normal;
        tmp.raycastTarget = false;
    }

    static void ApplyFont()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            Debug.LogError("_FinalRevise: font VT323 SDF tidak ditemukan.");
            return;
        }

        if (font.atlasTexture != null)
        {
            font.atlasTexture.filterMode = FilterMode.Point;
            EditorUtility.SetDirty(font.atlasTexture);
        }
        if (font.material != null && font.material.mainTexture != null)
        {
            font.material.mainTexture.filterMode = FilterMode.Point;
            EditorUtility.SetDirty(font.material);
        }
        EditorUtility.SetDirty(font);

        foreach (string name in new[] { "SpeakerText", "BodyText" })
        {
            var go = GameObject.Find(name);
            if (go == null) continue;
            var tmp = go.GetComponent<TMP_Text>();
            tmp.font = font;
            if (font.material != null) tmp.fontSharedMaterial = font.material;
        }
        Debug.Log("_FinalRevise font: " + font.name + " (lowercase=" + font.HasCharacter('a') + ").");
    }

    static void ApplyStory(Scene scene)
    {
        var seq = GameObject.Find("Cutscene")?.GetComponent<FlashbackSequence>();
        if (seq == null)
        {
            var all = UnityEngine.Object.FindObjectsByType<FlashbackSequence>(FindObjectsSortMode.None);
            if (all.Length > 0) seq = all[0];
        }
        if (seq == null)
        {
            Debug.LogError("_FinalRevise: FlashbackSequence tidak ditemukan.");
            return;
        }

        seq.flashbackRenderer = GameObject.Find("FlashbackScreen")?.GetComponent<SpriteRenderer>();
        seq.abah = GameObject.Find("Abah")?.GetComponent<CharacterPose>();
        seq.rimba = GameObject.Find("Rimba")?.GetComponent<CharacterPose>();
        seq.dialoguePanel = GameObject.Find("DialoguePanel");
        seq.panelGroup = seq.dialoguePanel != null ? seq.dialoguePanel.GetComponent<CanvasGroup>() : null;
        seq.speakerLabel = GameObject.Find("SpeakerText")?.GetComponent<TMP_Text>();
        seq.bodyLabel = GameObject.Find("BodyText")?.GetComponent<TMP_Text>();
        seq.indicatorImage = GameObject.Find("DialogueIndicator")?.GetComponent<Image>();
        seq.indicatorGroup = seq.indicatorImage != null ? seq.indicatorImage.GetComponent<CanvasGroup>() : null;
        seq.targetCamera = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        seq.cutsceneAudio = seq.GetComponent<CutsceneAudio>();
        seq.screenFade = UnityEngine.Object.FindFirstObjectByType<ScreenFade>();
        seq.startDelay = 0f;
        seq.turnDelay = 1f;
        seq.returnToWatchAfterDialogue = false;
        seq.endDelay = 2.5f;
        seq.screenFadeOutSeconds = 1f;
        seq.musicFadeOutSeconds = 3f;
        seq.fadeInSeconds = 1f;
        seq.nextSceneName = "";
        seq.panelShowSeconds = 0.12f;
        seq.panelHideSeconds = 0.1f;
        seq.panelRisePx = 12f;
        seq.indicatorBlinkSeconds = 0.45f;
        seq.charsPerSecond = 40f;
        seq.gapBetweenLines = 0.3f;

        seq.segments = new List<FlashbackSegment>
        {
            Seg(0, "pembuka", 0, 0, 2.0f, 0f, Lines(
                L("Rimba", "Abah, ceritakan lagi asal-usul karinding itu, dong."),
                L("Abah", "Konon, sebelum ada kehidupan, tak ada apa-apa. Awang uwung tong enong marga na enong, Kosong."))),
            Seg(1, "Frekuensi", 1, 11, 0.1f, 0.5f, Lines(
                L("Abah", "Hingga suatu ketika, dari kekosongan itu lahir getar, dan getar melahirkan dengung."))),
            Seg(2, "meledak", 12, 25, 0.1f, 0.5f, Lines(
                L("Abah", "Dari dengung itu, dunia mulai bergerak.")), 1.2f, 2f),
            Seg(3, "planet", 26, 32, 0.15f, 0.5f, Lines(
                L("Rimba", "Itu dunia kita, Bah?"),
                L("Abah", "Betul, Cu. Itulah bumi, yang lahir dari dengung."))),
            Seg(4, "tumbuh", 33, 45, 0.1f, 0.5f, Lines(
                L("Abah", "Gunung berdiri, air mengalir turun menuju laut."))),
            Seg(5, "kehidupan", 46, 48, 0.4f, 0.5f, Lines(
                L("Abah", "Dan kehidupan pun tumbuh di antara keduanya."),
                L("Abah", "Karuhun kita memahami ini bukan lewat kitab, tapi lewat alam itu sendiri."),
                L("Rimba", "Alam? Maksud Abah, hutan?"),
                L("Abah", "Mereka membagi hutan menjadi tiga."))),
            Seg(6, "htn.larangan", 49, 51, 0.4f, 0.5f, Lines(
                L("Abah", "Leuweung Larangan adalah tempat yang disucikan."))),
            Seg(7, "htn.tutupan", 52, 54, 0.4f, 0.5f, Lines(
                L("Abah", "Leuweung Tutupan, tempat kita belajar, lalu lahirlah kesadaran."))),
            Seg(8, "baladahan", 55, 57, 0.4f, 0.5f, Lines(
                L("Abah", "Leuweung Baladahan, tempat kita berladang, lalu munculnya kesabaran."))),
            Seg(9, "penutupan", 58, 60, 0.4f, 0.5f, Lines(
                L("Abah", "Yakin, Sadar, Sabar."),
                L("Rimba", "Yakin, sadar, sabar..."),
                L("Abah", "Tiga hal yang dijaga turun-temurun, dirawat dari generasi ke generasi,"),
                L("Abah", "seperti batas yang ditetapkan lalu dipelihara, netepkeun lalu netepan."),
                L("Abah", "Sebab apa yang tak dirawat akan hilang, dan apa yang hilang akan dicari."),
                L("Abah", "Tiga hal yang sama, yang kelak dituangkan di dalam sebilah bambu kecil."),
                L("Abah", "Digenggam dengan yakin, dipukul dengan sabar, dan didengarkan dengan sadar."),
                L("Abah", "Karena suara yang keluar darinya bukan sekadar suara."),
                L("Abah", "Ia gema dari getar pertama yang melahirkan dunia."))),
        };

        int total = 0;
        foreach (var s in seq.segments) total += s.lines.Count;
        Debug.Log("_FinalRevise story: " + seq.segments.Count + " segment, " + total + " baris dialog.");
    }

    static FlashbackSegment Seg(int index, string label, int start, int end, float spf, float hold,
        List<DialogueLine> lines, float shakeSeconds = 0f, float shakePx = 0f)
    {
        return new FlashbackSegment
        {
            label = label,
            startFrame = start,
            endFrame = end,
            secondsPerFrame = spf,
            repeatCount = 1,
            holdLastFrame = hold,
            lines = lines,
            shakeSeconds = shakeSeconds,
            shakeStrengthPx = shakePx,
        };
    }

    static List<DialogueLine> Lines(params DialogueLine[] lines)
    {
        return new List<DialogueLine>(lines);
    }

    static DialogueLine L(string speaker, string text)
    {
        float hold = 1.5f + text.Length / 30f;
        hold = Mathf.Round(hold * 2f) / 2f;
        return new DialogueLine { speaker = speaker, text = text, holdSeconds = hold };
    }

    static void FillFrames(Scene scene)
    {
        var seq = UnityEngine.Object.FindFirstObjectByType<FlashbackSequence>();
        if (seq == null)
        {
            Debug.LogError("_FinalRevise: tidak bisa mengisi frames, FlashbackSequence null.");
            return;
        }

        MethodInfo fill = typeof(FlashbackSequence).GetMethod("AutoFillFramesEditor",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (fill == null)
        {
            Debug.LogError("_FinalRevise: metode Isi Frames Otomatis tidak ditemukan.");
            return;
        }
        fill.Invoke(seq, null);
        Debug.Log("_FinalRevise frames: " + (seq.frames == null ? 0 : seq.frames.Length) +
                  " sprite, first=" + (seq.frames != null && seq.frames.Length > 0 ? seq.frames[0].name : "-") +
                  " last=" + (seq.frames != null && seq.frames.Length > 0 ? seq.frames[seq.frames.Length - 1].name : "-"));

        Sprite s51 = AssetDatabase.LoadAssetAtPath<Sprite>(GeneratedFolder + "/Frame_51.png");
        Sprite s60 = AssetDatabase.LoadAssetAtPath<Sprite>(GeneratedFolder + "/Frame_60.png");
        if (s51 != null) Debug.Log("_FinalRevise pivot Frame_51=" + s51.pivot + " size=" + s51.rect.size);
        if (s60 != null) Debug.Log("_FinalRevise pivot Frame_60=" + s60.pivot + " size=" + s60.rect.size);
    }

    static void MeasureText()
    {
        var go = GameObject.Find("BodyText");
        if (go == null) return;
        var body = go.GetComponent<TMP_Text>();
        string[] lines =
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

        int maxLines = 0;
        int maxChars = 0;
        float maxH = 0f;
        foreach (string line in lines)
        {
            body.text = line;
            body.ForceMeshUpdate();
            maxLines = Mathf.Max(maxLines, body.textInfo.lineCount);
            maxH = Mathf.Max(maxH, body.preferredHeight);
            for (int i = 0; i < body.textInfo.lineCount; i++)
                maxChars = Mathf.Max(maxChars, body.textInfo.lineInfo[i].characterCount);
            if (body.textInfo.lineCount > 2)
                Debug.LogError("_FinalRevise: baris >2: " + line);
        }
        body.text = "";
        body.ForceMeshUpdate();
        Debug.Log("_FinalRevise teks: body 28px width 820, maxLines=" + maxLines +
                  " maxCharsPerLine=" + maxChars + " maxPreferredHeight=" + maxH.ToString("F2") +
                  " lineHeight=" + body.textInfo.lineInfo[0].lineHeight.ToString("F2"));
    }
}
