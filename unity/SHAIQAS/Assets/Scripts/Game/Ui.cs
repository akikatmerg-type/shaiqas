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
    static float clipOx, clipOy;
    /// <summary>Тіктөртбұрышпен кесу (виртуал координаттар сақталады): ішінде бәрі бұрынғыдай R() арқылы салынады.</summary>
    public static void BeginClip(float x, float y, float w, float h)
    {
        Rect r = R(x, y, w, h); GUI.BeginClip(r);
        clipOx = ox; clipOy = oy; ox -= r.x; oy -= r.y;
    }
    public static void EndClip() { GUI.EndClip(); ox = clipOx; oy = clipOy; }
    static Texture2D disk;
    /// <summary>Тегіс шетті шеңбер (IMGUI-де шеңбер жоқ — бір рет салынған 64px дискіні бояймыз).</summary>
    public static void Circle(float cx, float cy, float r, Color c)
    {
        if (disk == null)
        {
            disk = new Texture2D(64, 64, TextureFormat.RGBA32, false); disk.wrapMode = TextureWrapMode.Clamp;
            Color32[] px = new Color32[64 * 64];
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                { float d = Mathf.Sqrt((x - 31.5f) * (x - 31.5f) + (y - 31.5f) * (y - 31.5f)); px[y * 64 + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(31.5f - d) * 255)); }
            disk.SetPixels32(px); disk.Apply();
        }
        Color o = GUI.color; GUI.color = new Color(c.r * o.r, c.g * o.g, c.b * o.b, c.a * o.a);
        GUI.DrawTexture(R(cx - r, cy - r, r * 2, r * 2), disk); GUI.color = o;
    }
    public static Vector2 ToVirtual(Vector2 screen) { return new Vector2((screen.x - ox) / k, (screen.y - oy) / k); }

    // ---- интерфейс атласы (Art/uiatlas.png + Data/meta_uiatlas.json): "Art/ui/<аты>" жолдары осыдан ----
    static Texture2D uiAtlas; static JNode uiMeta;
    /// <summary>Сурет көзі: текстура және оның ішіндегі UV тіктөртбұрышы.</summary>
    static Texture2D campAtlas; static JNode campMeta;
    public static bool Src(string path, out Texture2D t, out Rect uv, out float pw, out float ph)
    {
        uv = new Rect(0, 0, 1, 1); pw = 0; ph = 0;
        if (path.StartsWith("Art/camp/"))
        {
            if (campAtlas == null) { campAtlas = Resources.Load<Texture2D>("Art/campatlas"); campMeta = Db.J("meta_campatlas"); }
            t = campAtlas;
            JNode cr = campMeta.Get("r").Get(path.Substring(9));
            if (t == null || cr.IsNull()) return false;
            float CW = campMeta.Fk("w"), CH = campMeta.Fk("h"), cx = cr.At(0).F(), cy = cr.At(1).F(); pw = cr.At(2).F(); ph = cr.At(3).F();
            uv = new Rect(cx / CW, 1 - (cy + ph) / CH, pw / CW, ph / CH);
            return true;
        }
        if (path.StartsWith("Art/ui/"))
        {
            if (uiAtlas == null) { uiAtlas = Resources.Load<Texture2D>("Art/uiatlas"); uiMeta = Db.J("meta_uiatlas"); }
            t = uiAtlas;
            JNode rr = uiMeta.Get("r").Get(path.Substring(7));
            if (t == null || rr.IsNull()) return false;
            float W = uiMeta.Fk("w"), H = uiMeta.Fk("h"), x = rr.At(0).F(), y = rr.At(1).F(); pw = rr.At(2).F(); ph = rr.At(3).F();
            uv = new Rect(x / W, 1 - (y + ph) / H, pw / W, ph / H);
            return true;
        }
        t = Tex(path);
        if (t == null) return false;
        pw = t.width; ph = t.height;
        return true;
    }

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

    static Texture2D fadeTex;
    /// <summary>Көлденең бәсеңдеу: сол жақта c, оң жаққа қарай мөлдір (жолақсыз тегіс градиент).</summary>
    public static void FadeH(float x, float y, float w, float h, Color c)
    {
        if (fadeTex == null)
        {
            fadeTex = new Texture2D(256, 1, TextureFormat.RGBA32, false); fadeTex.wrapMode = TextureWrapMode.Clamp;
            for (int i = 0; i < 256; i++) fadeTex.SetPixel(i, 0, new Color(1, 1, 1, 1 - i / 255f));
            fadeTex.Apply();
        }
        Color o = GUI.color; GUI.color = c; GUI.DrawTexture(R(x, y, w, h), fadeTex); GUI.color = o;
    }
    public static void Fill(float x, float y, float w, float h, Color c)
    {
        Color o = GUI.color; GUI.color = c; GUI.DrawTexture(R(x, y, w, h), white); GUI.color = o;
    }
    public static void Img(string path, float x, float y, float w, float h, float alpha = 1)
    {
        Texture2D t; Rect uv; float pw, ph;
        if (!Src(path, out t, out uv, out pw, out ph)) return;
        Color o = GUI.color; GUI.color = new Color(o.r, o.g, o.b, o.a * alpha); GUI.DrawTextureWithTexCoords(R(x, y, w, h), t, uv, true); GUI.color = o;
    }
    /// <summary>Түске боялған сурет (ақ сызықтар/сақиналар үшін).</summary>
    public static void ImgC(string path, float x, float y, float w, float h, Color c)
    {
        Texture2D t; Rect uv; float pw, ph;
        if (!Src(path, out t, out uv, out pw, out ph)) return;
        Color o = GUI.color; GUI.color = new Color(c.r, c.g, c.b, c.a * o.a); GUI.DrawTextureWithTexCoords(R(x, y, w, h), t, uv, true); GUI.color = o;
    }
    /// <summary>Орталықты айналып бұрылған сурет (градус).</summary>
    public static void ImgRot(string path, float cx, float cy, float w, float h, float deg, Color c)
    {
        Matrix4x4 m = GUI.matrix;
        GUIUtility.RotateAroundPivot(deg, new Vector2(ox + cx * k, oy + cy * k));
        ImgC(path, cx - w / 2, cy - h / 2, w, h, c);
        GUI.matrix = m;
    }
    /// <summary>Мәтін ені (виртуал пиксельмен).</summary>
    public static float TextW(string s, int size, bool bold = true)
    {
        st.font = bold ? fontBold : font; st.fontSize = Mathf.Max(6, Mathf.RoundToInt(size * k));
        return st.CalcSize(new GUIContent(s)).x / k;
    }
    /// <summary>Көлденең айнадағы сурет.</summary>
    public static void ImgFlip(string path, float x, float y, float w, float h)
    {
        Texture2D t; Rect uv; float pw, ph;
        if (!Src(path, out t, out uv, out pw, out ph)) return;
        GUI.DrawTextureWithTexCoords(R(x, y, w, h), t, new Rect(uv.x + uv.width, uv.y, -uv.width, uv.height), true);
    }
    /// <summary>Тік градиент (3 түс) — веб-жолақтарға арналған.</summary>
    public static void Grad3(float x, float y, float w, float h, Color a, Color b, Color c)
    {
        if (w <= 0) return;
        int n = 6;
        for (int i = 0; i < n; i++)
        {
            float t0 = i / (float)n, tm = (i + 0.5f) / n;
            Color col = tm < 0.45f ? Color.Lerp(a, b, tm / 0.45f) : Color.Lerp(b, c, (tm - 0.45f) / 0.55f);
            Fill(x, y + h * t0, w, h / n + 0.5f, col);
        }
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

    /// <summary>Тек жиек (іші бос).</summary>
    public static void Frame(float x, float y, float w, float h, Color c, float t = 2)
    {
        Fill(x, y, w, t, c); Fill(x, y + h - t, w, t, c); Fill(x, y + t, t, h - 2 * t, c); Fill(x + w - t, y + t, t, h - 2 * t, c);
    }
    public static void Panel(float x, float y, float w, float h, Color bg, Color border)
    {
        Fill(x, y, w, h, border); Fill(x + 3, y + 3, w - 6, h - 6, bg);
    }

    // ================= веб-стильдегі бөлшектер (Art/ui: веб-кодтан шығарылған) =================
    /// <summary>9 бөлікті сурет: виртуал өлшемде, шеттері (bl,bt,br,bb) суреттің пиксельдерінде = виртуал бірлік.</summary>
    public static void Nine(string path, float x, float y, float w, float h, float bl, float bt, float br, float bb, float alpha = 1)
    {
        Texture2D t; Rect uvr; float tw, th;
        if (!Src(path, out t, out uvr, out tw, out th)) return;
        bl = Mathf.Min(bl, w / 2); br = Mathf.Min(br, w / 2); bt = Mathf.Min(bt, h / 2); bb = Mathf.Min(bb, h / 2);
        float[] xs = { x, x + bl, x + w - br, x + w }, ys = { y, y + bt, y + h - bb, y + h };
        float[] us = { 0, bl / tw, 1 - br / tw, 1 }, vs = { 1, 1 - bt / th, bb / th, 0 };
        for (int q = 0; q < 4; q++) { us[q] = uvr.x + us[q] * uvr.width; vs[q] = uvr.y + vs[q] * uvr.height; }
        // жиектерді экран пикселіне тураймыз — әйтпесе бөліктер арасында 1 px саңылау (тік сызық) көрінеді
        for (int q = 0; q < 4; q++) { xs[q] = Mathf.Round(ox + xs[q] * k); ys[q] = Mathf.Round(oy + ys[q] * k); }
        Color o = GUI.color; GUI.color = new Color(o.r, o.g, o.b, o.a * alpha);
        for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++)
        {
            float rw = xs[i + 1] - xs[i], rh = ys[j + 1] - ys[j]; if (rw <= 0 || rh <= 0) continue;
            GUI.DrawTextureWithTexCoords(new Rect(xs[i], ys[j], rw, rh), t, new Rect(us[i], vs[j + 1], us[i + 1] - us[i], vs[j] - vs[j + 1]), true);
        }
        GUI.color = o;
    }
    /// <summary>Веб: dzPanel. style: felt | parch | red | dark.</summary>
    public static void DzPanel(float x, float y, float w, float h, string style = "felt", bool band = false)
    {
        Nine("Art/ui/panel_" + style + (band ? "_band" : ""), x - 22, y - 22, w + 44, h + 44, 72, 72, 72, 72);
    }
    /// <summary>Веб: dzRibbon — қызыл тақырып лентасы.</summary>
    public static void Ribbon(float cx, float cy, float w, string label, int size = 30)
    {
        Nine("Art/ui/ribbon", cx - w / 2 - 50, cy - 40, w + 100, 80, 110, 0, 110, 0);
        TextO(label, cx - w / 2 + 30, cy - 24, w - 60, 48, Fit(label, w - 60, size), new Color32(255, 214, 120, 255), TextAnchor.MiddleCenter);
    }
    public static void Medal(float cx, float cy, int r, string col)
    {
        string p = "Art/ui/medal_" + r + (U.Len(col) > 0 ? "_" + col : "");
        Img(p, cx - r - 20, cy - r - 20, r * 2 + 40, r * 2 + 40);
    }
    /// <summary>Медальон ішіндегі портрет.</summary>
    public static void Portrait(string path, float cx, float cy, float r)
    {
        Img(path, cx - r * 64f / 60f, cy - r * 64f / 60f, r * 128f / 60f, r * 128f / 60f);
    }

    /// <summary>Жиегі бар мәтін (веб: inkText/goldText).</summary>
    public static void TextO(string s, float x, float y, float w, float h, int size, Color c, TextAnchor a = TextAnchor.MiddleCenter, bool bold = true, float edge = 2)
    {
        Color e = new Color(0.04f, 0.02f, 0.05f, 0.85f * c.a);
        for (int i = 0; i < 8; i++)
        {
            float ang = i * Mathf.PI / 4;
            Text(s, x + Mathf.Cos(ang) * edge, y + Mathf.Sin(ang) * edge, w, h, size, e, a, bold);
        }
        Text(s, x, y, w, h, size, c, a, bold);
    }
    public static int Fit(string s, float w, int size, bool bold = true)
    {
        st.font = bold ? fontBold : font;
        for (int sz = size; sz > 10; sz--)
        {
            st.fontSize = Mathf.Max(6, Mathf.RoundToInt(sz * k));
            if (st.CalcSize(new GUIContent(s)).x / k <= w) return sz;
        }
        return 10;
    }

    /// <summary>Көп жолды мәтін қорапқа сыятын өлшем.</summary>
    public static int FitBox(string s, float w, float h, int size, bool bold = true)
    {
        st.font = bold ? fontBold : font; bool ww = st.wordWrap; st.wordWrap = true;
        int res = 10;
        for (int sz = size; sz > 10; sz--)
        {
            st.fontSize = Mathf.Max(6, Mathf.RoundToInt(sz * k));
            if (st.CalcHeight(new GUIContent(s), w * k) / k <= h) { res = sz; break; }
        }
        st.wordWrap = ww; return res;
    }

    /// <summary>Веб-стильдегі батырма: басылса true. icon — эмблема (map, batyr, arms, fight, ...).</summary>
    public static bool DzBtn(string label, float x, float y, float w, float h, bool primary = false, bool enabled = true, string icon = null, bool small = false, bool tab = false, bool tiny = false, bool active = false)
    {
        Rect r = R(x, y, w, h);
        bool hover = enabled && GUI.enabled && r.Contains(Event.current.mousePosition);
        string kind = !enabled ? "dis" : active ? "act" : primary ? (hover ? "priH" : "pri") : (hover ? "norH" : "nor");
        string shp = icon != null ? "ar" : tab ? "tb" : primary ? "pt" : "ch";
        int ih = h >= 56 ? 62 : 48;
        float dy = hover ? -2 : 0, dx = icon != null && hover ? 6 : 0;
        float m = 26, side = ih * 0.75f + m;
        Nine("Art/ui/btn_" + shp + "_" + kind + "_" + ih, x - m + dx, y - m + dy, w + m * 2, h + m * 2, side, m + 6, side, m + 6);
        Color tc = active ? new Color32(42, 20, 6, 255) : !enabled ? new Color32(168, 154, 138, 255) : primary ? new Color32(255, 214, 120, 255) : hover ? new Color32(255, 233, 176, 255) : new Color32(255, 243, 220, 255);
        if (icon != null)
        {
            Img("Art/ui/icon_" + icon + "_" + (hover ? 1 : 0) + (primary ? 1 : 0), x + dx + 42 - 48, y + dy + h / 2 - 48, 96, 96);
            int sz = Fit(label, w - 150, 30);
            TextO(label, x + dx + 80, y + dy, w - 100, h, sz, tc, TextAnchor.MiddleLeft);
            if (w > 260) Img(hover ? "Art/ui/ram_lit" : "Art/ui/ram", x + dx + w - 66, y + dy + h / 2 - 22, 40, 20, 0.9f);
        }
        else
        {
            int size = tiny ? 17 : small ? 22 : 30, sz = Fit(label, w - (primary ? 44 : 20), size);
            TextO(label, x + dx, y + dy, w, h, sz, tc, TextAnchor.MiddleCenter, true, Mathf.Max(1.5f, sz * 0.08f));
        }
        if (!enabled) { if (GUI.enabled && GUI.Button(r, GUIContent.none, GUIStyle.none)) Snd.Play("ui_error", 0.7f); return false; }
        bool hit = GUI.Button(r, GUIContent.none, GUIStyle.none);
        if (hit) { clicked = true; Snd.Play(primary ? "ui_select" : "ui_move", 0.8f); }
        return hit;
    }

    /// <summary>Батырма (барлық экрандар): веб-стильдегі батырманы салады.</summary>
    public static bool Btn(string label, float x, float y, float w, float h, bool primary = false, bool enabled = true, int size = 22)
    {
        return DzBtn(label, x, y, w, h, primary, enabled, null, size < 22, false, size < 17);
    }
    /// <summary>Мөлдір аймақ-батырма (сурет үстінде).</summary>
    /// <summary>Көрінбейтін батырма: тіктөртбұрышқа басылса true.</summary>
    public static bool Click(float x, float y, float w, float h) { return GUI.Button(R(x, y, w, h), GUIContent.none, GUIStyle.none); }
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

    /// <summary>Жоғарғы жолақ (веб: topBar): киіз фон, ою жиек, деңгей белгісі, тақырып лентасы, теңге.</summary>
    public static void TopBar(string title, bool lite = false)
    {
        if (lite) Img("Art/ui/topbar_lite", 0, 0, 1280, 110);
        else Img("Art/ui/topbar_bg", 0, 0, 1280, 100);
        // деңгей белгісі
        int L = Progress.Level();
        DzPanelSmall(192, 10, 180, 50);
        Medal(212, 35, 18, "");
        TextO(U.Int(L), 194, 17, 36, 36, 20, new Color32(42, 20, 6, 255), TextAnchor.MiddleCenter, true, 0.5f);
        Text("ДЕҢГЕЙ", 238, 14, 130, 22, 14, CREAM, TextAnchor.MiddleLeft, true);
        Bar(238, 38, 124, 8, L >= Progress.MAXL ? 1 : Progress.XpInto() / (float)Progress.Need(L), GOLD, new Color(0, 0, 0, 0.6f));
        if (title != null) Ribbon(640, 38, 380, title, 26);
        // теңге
        string cs = U.Int(Game.S.coins);
        float tw = Mathf.Max(60, cs.Length * 15 + 56);
        DzPanelSmall(1280 - 40 - tw, 16, tw, 44);
        Img("Art/icons/coin", 1280 - 40 - tw + 6, 20, 36, 36);
        TextO(cs, 1280 - 40 - tw + 44, 16, tw - 50, 44, 24, CREAM, TextAnchor.MiddleLeft);
    }
    /// <summary>Шағын қараңғы тақта (белгі/пилюля).</summary>
    public static void DzPanelSmall(float x, float y, float w, float h)
    {
        Nine("Art/ui/btn_ch_nor_48", x - 26, y - 26 + (h - 48) / 2, w + 52, 100, 40, 32, 40, 32);
    }
}
