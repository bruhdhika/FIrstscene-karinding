using System.Collections;
using UnityEngine;

// Dua AudioSource 2D untuk musik latar dan suara dialog.
// Semua transisi memakai SmoothStep supaya tidak terdengar mendadak.
public class CutsceneAudio : MonoBehaviour
{
    [Header("Clip")]
    public AudioClip musicClip;        // Kacapi_Suling
    public AudioClip dialogueClip;     // dialog_sound

    [Header("Source (kosong = dibuat otomatis)")]
    public AudioSource musicSource;
    public AudioSource dialogueSource;

    [Header("Volume musik")]
    public float musicLow = 0.08f;     // saat dialog / awal
    public float musicHigh = 0.20f;    // saat frame flashback berputar

    [Header("Durasi transisi musik (detik)")]
    public float fadeInSeconds = 4f;   // 0 -> low saat Play
    public float riseSeconds = 1f;     // low -> high
    public float dipSeconds = 0.6f;    // high -> low saat dialog
    public float endFadeSeconds = 3f;  // fade out di akhir

    [Header("Suara dialog")]
    public float voiceVolume = 0.6f;
    public float abahPitch = 0.85f;
    public float rimbaPitch = 1.2f;
    public float voiceRandomStartMax = 12f;
    public float voiceFadeOutSeconds = 0.05f;

    Coroutine musicRoutine;
    Coroutine voiceRoutine;
    float musicTarget;

    void Awake()
    {
        EnsureSources();
        Configure(musicSource);
        Configure(dialogueSource);
    }

    // Hanya membuat source yang belum ada. Jangan mengonfigurasi ulang source
    // yang sudah berjalan: Configure mereset volume dan bisa mematikan musik
    // setiap kali baris dialog baru dimulai.
    void EnsureSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            Configure(musicSource);
        }
        if (dialogueSource == null)
        {
            dialogueSource = gameObject.AddComponent<AudioSource>();
            Configure(dialogueSource);
        }
    }

    static void Configure(AudioSource src)
    {
        if (src == null) return;
        src.playOnAwake = false;
        src.loop = true;
        src.spatialBlend = 0f;
        src.volume = 0f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.bypassEffects = true;
        src.bypassListenerEffects = true;
        src.bypassReverbZones = true;
    }

    // Dipanggil sekali di awal oleh FlashbackSequence.
    public void Begin()
    {
        EnsureSources();
        StopAllCoroutines();
        musicRoutine = null;
        voiceRoutine = null;

        if (musicSource == null || musicClip == null) return;
        musicSource.clip = musicClip;
        musicSource.volume = 0f;
        musicSource.time = 0f;
        musicSource.Play();
        musicTarget = musicLow;
        musicRoutine = StartCoroutine(FadeMusicRoutine(musicLow, fadeInSeconds, false));
    }

    // Musik naik halus ketika frame flashback berputar.
    public void MusicToFlashback()
    {
        MusicTo(musicHigh, riseSeconds);
    }

    // Musik turun halus ketika dialog mulai.
    public void MusicToDialogue()
    {
        MusicTo(musicLow, dipSeconds);
    }

    void MusicTo(float target, float seconds)
    {
        if (musicSource == null || musicClip == null) return;
        if (!musicSource.isPlaying)
        {
            musicSource.clip = musicClip;
            musicSource.volume = Mathf.Clamp01(musicSource.volume);
            musicSource.Play();
        }
        musicTarget = target;
        if (musicRoutine != null) StopCoroutine(musicRoutine);
        musicRoutine = StartCoroutine(FadeMusicRoutine(target, seconds, false));
    }

    // Fade out musik; dipakai di akhir cutscene.
    public IEnumerator FadeOutMusic(float seconds)
    {
        if (musicSource == null || !musicSource.isPlaying) yield break;
        if (musicRoutine != null) StopCoroutine(musicRoutine);
        musicTarget = 0f;
        yield return FadeMusicRoutine(0f, seconds, true);
    }

    IEnumerator FadeMusicRoutine(float target, float seconds, bool stopAfter)
    {
        if (musicSource == null) yield break;
        float from = musicSource.volume;
        if (seconds <= 0f)
        {
            musicSource.volume = target;
        }
        else
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / seconds);
                musicSource.volume = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, k));
                yield return null;
            }
            musicSource.volume = target;
        }

        if (stopAfter)
        {
            musicSource.Stop();
            musicSource.volume = 0f;
        }
        musicRoutine = null;
    }

    // Suara ketikan. pitch rendah untuk Abah, tinggi untuk Rimba.
    public void StartVoice(bool isAbah)
    {
        EnsureSources();
        if (dialogueSource == null || dialogueClip == null) return;

        StopVoiceImmediate();
        dialogueSource.clip = dialogueClip;
        dialogueSource.loop = true;
        dialogueSource.volume = voiceVolume;
        dialogueSource.pitch = isAbah ? abahPitch : rimbaPitch;

        float maxStart = Mathf.Max(0f, Mathf.Min(voiceRandomStartMax, dialogueClip.length - 0.1f));
        dialogueSource.time = maxStart > 0f ? Random.Range(0f, maxStart) : 0f;
        dialogueSource.Play();
    }

    // Berhenti halus setelah teks selesai diketik.
    public void StopVoice()
    {
        if (dialogueSource == null || !dialogueSource.isPlaying) return;
        if (voiceRoutine != null) StopCoroutine(voiceRoutine);
        voiceRoutine = StartCoroutine(FadeVoiceRoutine());
    }

    IEnumerator FadeVoiceRoutine()
    {
        float from = dialogueSource.volume;
        float t = 0f;
        float seconds = Mathf.Max(0.01f, voiceFadeOutSeconds);
        while (t < seconds)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / seconds);
            dialogueSource.volume = Mathf.Lerp(from, 0f, Mathf.SmoothStep(0f, 1f, k));
            yield return null;
        }
        dialogueSource.Stop();
        dialogueSource.volume = voiceVolume;
        voiceRoutine = null;
    }

    void StopVoiceImmediate()
    {
        if (voiceRoutine != null)
        {
            StopCoroutine(voiceRoutine);
            voiceRoutine = null;
        }
        if (dialogueSource != null)
        {
            dialogueSource.Stop();
            dialogueSource.volume = voiceVolume;
        }
    }

    public void StopAll()
    {
        StopAllCoroutines();
        musicRoutine = null;
        voiceRoutine = null;
        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.volume = 0f;
        }
        if (dialogueSource != null)
        {
            dialogueSource.Stop();
            dialogueSource.volume = 0f;
        }
    }

    public float MusicTarget { get { return musicTarget; } }
}
