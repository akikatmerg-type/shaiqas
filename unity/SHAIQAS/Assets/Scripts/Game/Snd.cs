// SHAIQAS · Дыбыс: веб-қозғалтқыштан жазылған эффектілер мен күйлер (Resources/Audio).
// Эффектілер — көздер пулы (дыбыс деңгейі, панорама, биіктік, кездейсоқ нұсқа), музыка — жұмсақ ауысумен.
using System.Collections.Generic;
using UnityEngine;

public class Snd : MonoBehaviour
{
    public static Snd I;
    AudioSource[] pool;
    AudioSource musA, musB;
    int next;
    string musKey = "";
    float fade = 1, fadeSpeed = 1;
    readonly Dictionary<string, AudioClip[]> clips = new Dictionary<string, AudioClip[]>();
    readonly Dictionary<string, float> lastT = new Dictionary<string, float>();
    static readonly int[] KPOOL0 = { 0, 1, 2, 8, 16, 17 }, KPOOL1 = { 0, 3, 1, 7, 12, 13, 15 }, KPOOL2 = { 5, 4, 2, 14, 16 }, KPOOL3 = { 6, 1, 3, 7, 4, 12, 14, 17 }, KPOOL4 = { 8, 6, 7, 13, 17 }, KPOOL5 = { 8, 2, 0, 15, 16 };
    static readonly int[] KBOSS = { 9, 10, 11, 12, 14 };
    int lastKuy = -1;

    void Awake()
    {
        I = this;
        pool = new AudioSource[12];
        for (int i = 0; i < pool.Length; i++) { pool[i] = gameObject.AddComponent<AudioSource>(); pool[i].playOnAwake = false; }
        musA = gameObject.AddComponent<AudioSource>(); musB = gameObject.AddComponent<AudioSource>();
        musA.loop = true; musB.loop = true; musA.playOnAwake = false; musB.playOnAwake = false;
    }

    AudioClip[] Get(string name)
    {
        AudioClip[] a;
        if (clips.TryGetValue(name, out a)) return a;
        AudioClip c1 = Resources.Load<AudioClip>("Audio/sfx/" + name), c2 = Resources.Load<AudioClip>("Audio/sfx/" + name + "_2");
        a = c1 == null ? new AudioClip[0] : c2 == null ? new[] { c1 } : new[] { c1, c2 };
        clips[name] = a;
        return a;
    }

    /// <summary>Эффект: vol 0..1+, pan -1..1 (сол/оң), pitch.</summary>
    public static void Play(string name, float vol = 1, float pan = 0, float pitch = 1)
    {
        if (I == null || Game.S == null || !Game.S.sound) return;
        float now = Time.unscaledTime, lt;
        if (I.lastT.TryGetValue(name, out lt) && now - lt < 0.03f) return;
        I.lastT[name] = now;
        AudioClip[] a = I.Get(name);
        if (a.Length == 0) return;
        AudioSource s = I.pool[I.next]; I.next = (I.next + 1) % I.pool.Length;
        s.clip = a[Random.Range(0, a.Length)];
        s.volume = Mathf.Clamp01(vol * Game.S.volSfx * 0.9f);
        s.panStereo = Mathf.Clamp(pan, -1, 1);
        s.pitch = pitch * Random.Range(0.96f, 1.04f);
        s.Play();
    }
    /// <summary>Жауынгердің орнына қарай панорама.</summary>
    public static float Pan(float worldX, float cam) { return Mathf.Clamp((worldX - cam - 640) / 900f, -0.8f, 0.8f); }

    /// <summary>Музыка: menu | map | battle | boss | arena | tournament | victory | defeat.</summary>
    public static void Music(string track, int chapter)
    {
        if (I == null) return;
        int ch = Mathf.Clamp(chapter, 0, 5);
        string key = track + "|" + ch;
        if (key == I.musKey) return;
        I.musKey = key;
        string file = track;
        if (track == "map") file = "map_" + ch;
        else if (track == "battle") file = "battle_k" + I.PickKuy(false, ch);
        else if (track == "boss") file = "boss_k" + I.PickKuy(true, ch);
        AudioClip c = Resources.Load<AudioClip>("Audio/music/" + file);
        AudioSource from = I.musA.isPlaying ? I.musA : I.musB, to = from == I.musA ? I.musB : I.musA;
        to.clip = c; to.loop = track != "victory" && track != "defeat"; to.volume = 0;
        if (c != null) to.Play();
        I.fadeOut = from; I.fadeIn = to; I.fade = 0;
        I.fadeSpeed = (track == "victory" || track == "defeat" || track == "battle" || track == "boss") ? 3f : 1f;
    }
    AudioSource fadeOut, fadeIn;

    int PickKuy(bool boss, int ch)
    {
        int[] pool = boss ? KBOSS : (Random.value < 0.25f ? null : ch == 0 ? KPOOL0 : ch == 1 ? KPOOL1 : ch == 2 ? KPOOL2 : ch == 3 ? KPOOL3 : ch == 4 ? KPOOL4 : KPOOL5);
        if (pool == null) pool = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 12, 13, 14, 15, 16, 17 };
        int k = pool[Random.Range(0, pool.Length)];
        if (k == lastKuy && pool.Length > 1) k = pool[(System.Array.IndexOf(pool, k) + 1) % pool.Length];
        lastKuy = k;
        return k;
    }

    void Update()
    {
        float vol = Game.S == null || !Game.S.sound ? 0 : Game.S.volMusic * 0.7f;
        if (fade < 1) fade = Mathf.Min(1, fade + Time.unscaledDeltaTime * fadeSpeed);
        if (fadeIn != null) fadeIn.volume = vol * fade;
        if (fadeOut != null) { fadeOut.volume = vol * (1 - fade); if (fade >= 1 && fadeOut.isPlaying) fadeOut.Stop(); }
    }
}
