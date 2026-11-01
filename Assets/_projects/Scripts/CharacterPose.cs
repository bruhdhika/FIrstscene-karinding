using System.Collections;
using UnityEngine;

// Pasang di Abah dan Rimba. Mengganti gambar (sprite) sesuai pose.
[RequireComponent(typeof(SpriteRenderer))]
public class CharacterPose : MonoBehaviour
{
    [Header("3 gambar karakter")]
    public Sprite watchSprite;   // duduk membelakangi, menonton layar
    public Sprite faceSprite;    // menoleh ke samping, mulut tertutup
    public Sprite talkSprite;    // menoleh ke samping, mulut terbuka

    [Tooltip("Detik per pergantian mulut saat bicara")]
    public float mouthInterval = 0.15f;

    [Header("Fokus penutur")]
    public Color focusColor = Color.white;
    public Color dimColor = new Color(0.62f, 0.62f, 0.68f, 1f);

    SpriteRenderer sr;
    Coroutine talkRoutine;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr.sprite == null) SetSprite(watchSprite);
    }

    void SetSprite(Sprite s)
    {
        if (s != null && sr != null) sr.sprite = s;
    }

    // Duduk menonton layar
    public void Watch()
    {
        StopTalkLoop();
        SetSprite(watchSprite);
    }

    // Menoleh ke lawan bicara, diam
    public void Face()
    {
        StopTalkLoop();
        SetSprite(faceSprite);
    }

    // Bicara: mulut terbuka dan tertutup bergantian
    public void StartTalking()
    {
        StopTalkLoop();
        if (talkSprite == null && faceSprite == null) return;
        talkRoutine = StartCoroutine(TalkLoop());
    }

    public void StopTalking()
    {
        StopTalkLoop();
    }

    // Penutur = putih, lawan bicara = sedikit redup
    public void SetFocus(bool focused)
    {
        if (sr != null) sr.color = focused ? focusColor : dimColor;
    }

    void StopTalkLoop()
    {
        if (talkRoutine != null)
        {
            StopCoroutine(talkRoutine);
            talkRoutine = null;
        }
    }

    IEnumerator TalkLoop()
    {
        bool open = true;
        while (true)
        {
            SetSprite(open ? talkSprite : faceSprite);
            open = !open;
            yield return new WaitForSeconds(Mathf.Max(0.03f, mouthInterval));
        }
    }
}
