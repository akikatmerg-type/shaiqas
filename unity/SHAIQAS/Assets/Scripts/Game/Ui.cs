// SHAIQAS · Интерфейс құралдары (IMGUI). Барлық өлшемдер — вебтегідей 1280×720 виртуал экранда,
// сосын нақты экранға масштабталады (мәтін бұлыңғыр болмауы үшін қаріп өлшемі де бірге өседі).
using System.Collections.Generic;
using UnityEngine;

public static class Ui
{
    public static float k = 1, ox, oy;          // виртуал → экран
    public static Font font, fontBold;
    static GUIStyle st;
    static Texture2D white;
    static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
    public static readonly Color GOLD = new Color32(242, 184, 64, 255), INK = new Color32(32, 18, 10, 255), PAPER = new Color32(246, 225, 180, 255);
    public static readonly Color NAVY = new Color32(22, 18, 44, 235), RED = new Color32(125, 23, 25, 255), CREAM = new Color32(255, 243, 214, 255);
    public static bool clicked;                  // осы кадрда батырма басылды ма (ұстазға қажет)

    public static void Init()
    {
        font = Resources.Load<Font>("Fonts/DejaVuSansCondensed");
        fontBold = Resources.Load<Font>("Fonts/DejaVuSansCondensed-Bold");
        white = Texture2D.whiteTexture;
    }

    /// <summary>Әр OnGUI басында: 16:9 аймағын табу.</summary>
    public static void Begin()
    {
        float sw = Screen.width, sh = Screen.height;
        k = Mathf.Min(sw / 1280f, sh / 720f);
        ox = (sw - 1280f * k) * 0.5f; oy = (sh - 720f * k) * 0.5f;
        clicked = false;
        if (st == null)
        {
            st = new GUIStyle();
            st.wordWrap = true; st.richText = false; st.clipping = TextClipping.Overflow;
        }
    }
    public static Rect R(float x, float y, float w, float h) { return new Rect(ox + x * k, oy + y * k, w * k, h * k); }
    public static Rect R(Rect v) { return R(v.x, v.y, v.width, v.height); }
    public static Vector2 ToVirtual(Vector2 screen) { return new Vector2((screen.x - ox) / k, (screen.y - oy) / k); }

    public static Texture2D Tex(string path)
    {
        Texture2D t;
        if (cache.TryGetValue(path, out t) && t != null) return t;
        t = Resources.Load<Texture2D>(path);
        cache[path] = t;
        return t;
    }
    public static void Forget(string prefix)
    {
        List<string> rm = new List<string>();
        foreach (var kv in cache) if (kv.Key.StartsWith(prefix)) rm.Add(kv.Key);
        foreach (string key in rm) { if (cache[key] != null) Resources.UnloadAsset(cache[key]); cache.Remove(key); }
    }

    public static void Fill(float x, float y, float w, float h, Color c)
    {
        Color o = GUI.color; GUI.color = c; GUI.DrawTexture(R(x, y, w, h), white); GUI.color = o;
    }
    public static void Img(string path, float x, float y, float w, float h, float alpha = 1)
    {
        Texture2D t = Tex(path); if (t == null) return;
        Color o = GUI.color; GUI.color = new Color(1, 1, 1, alpha); GUI.DrawTexture(R(x, y, w, h), t, ScaleMode.StretchToFill, true); GUI.color = o;
    }
    public static void ImgFit(string path, float x, float y, float w, float h)
    {
        Texture2D t = Tex(path); if (t == null) return;
        GUI.DrawTexture(R(x, y, w, h), t, ScaleMode.ScaleToFit, true);
    }

    public static void Text(string s, float x, float y, float w, float h, int size, Color c, TextAnchor a = TextAnchor.UpperLeft, bool bold = false)
    {
        st.font = bold ? fontBold : font; st.fontSize = Mathf.Max(6, Mathf.RoundToInt(size * k)); st.alignment = a; st.normal.textColor = c;
        GUI.Label(R(x, y, w, h), s, st);
    }
    /// <summary>Көлеңкелі мәтін (қараңғы фонда оқу оңай).</summary>
    public static void TextS(string s, float x, float y, float w, float h, int size, Color c, TextAnchor a = TextAnchor.UpperLeft, bool bold = true)
    {
        Text(s, x + 2, y + 2, w, h, size, new Color(0, 0, 0, 0.75f), a, bold);
        Text(s, x, y, w, h, size, c, a, bold);
    }
    public static float TextHeight(string s, float w, int size, bool bold = false)
    {
        st.font = bold ? fontBold : font; st.fontSize = Mathf.Max(6, Mathf.RoundToInt(size * k));
        return st.CalcHeight(new GUIContent(s), w * k) / k;
    }

    public static void Panel(float x, float y, float w, float h, Color bg, Color border)
    {
        Fill(x, y, w, h, border); Fill(x + 3, y + 3, w - 6, h - 6, bg);
    }

    /// <summary>Батырма: басылса true. primary — алтын түсті негізгі батырма.</summary>
    public static bool Btn(string label, float x, float y, float w, float h, bool primary = false, bool enabled = true, int size = 22)
    {
        Rect r = R(x, y, w, h);
        bool hover = r.Contains(Event.current.mousePosition);
        Color bg = !enabled ? new Color(0.25f, 0.22f, 0.2f, 0.85f) : primary ? (hover ? new Color32(255, 205, 96, 255) : (Color)GOLD) : (hover ? new Color32(70, 52, 110, 240) : (Color)NAVY);
        Fill(x, y, w, h, primary ? new Color32(120, 70, 10, 255) : new Color32(233, 173, 60, 200));
        Fill(x + 2, y + 2, w - 4, h - 4, bg);
        Text(label, x, y, w, h, size, primary ? (Color)INK : (enabled ? (Color)CREAM : new Color(0.7f, 0.65f, 0.6f)), TextAnchor.MiddleCenter, true);
        if (!enabled) { if (GUI.enabled && GUI.Button(r, GUIContent.none, GUIStyle.none)) Snd.Play("ui_error", 0.7f); return false; }
        bool hit = GUI.Button(r, GUIContent.none, GUIStyle.none);
        if (hit) { clicked = true; Snd.Play(primary ? "ui_select" : "ui_move", 0.8f); }
        return hit;
    }
    /// <summary>Мөлдір аймақ-батырма (сурет үстінде).</summary>
    public static bool Hot(float x, float y, float w, float h)
    {
        bool hit = GUI.Button(R(x, y, w, h), GUIContent.none, GUIStyle.none);
        if (hit) { clicked = true; Snd.Play("ui_move", 0.6f); }
        return hit;
    }

    public static void Bar(float x, float y, float w, float h, float frac, Color c, Color back)
    {
        Fill(x, y, w, h, back);
        Fill(x, y, w * Mathf.Clamp01(frac), h, c);
    }

    /// <summary>Жоғарғы жолақ: тақырып, теңге, атақ.</summary>
    public static void TopBar(string title)
    {
        Fill(0, 0, 1280, 64, new Color(0.06f, 0.04f, 0.12f, 0.88f));
        Fill(0, 64, 1280, 3, GOLD);
        if (title != null) TextS(title, 0, 8, 1280, 48, 30, GOLD, TextAnchor.MiddleCenter);
        Img("Art/icons/coin", 1080, 12, 40, 40);
        TextS(U.Int(Game.S.coins), 1126, 10, 140, 44, 24, CREAM, TextAnchor.MiddleLeft);
        TextS(Game.PlayerName() + " · " + Progress.RankName(), 120, 10, 600, 44, 20, CREAM, TextAnchor.MiddleLeft, false);
    }
}
