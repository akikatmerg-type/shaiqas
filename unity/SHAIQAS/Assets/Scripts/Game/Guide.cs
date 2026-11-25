// SHAIQAS · ӘДІСТЕР КІТАБЫ (веб: skDrawGuide, skDrawCard, skSeqDraw) және ЕРКІН АЛАҢ (веб: skArenaStart).
// Кітап лоббиден ашылады (бөлек экран) немесе ұрыс үзілісінен (ұрыстың үстінде қабат болып).
using UnityEngine;

public partial class GameRoot
{
    public const string GUIDE = "guide";
    public int guideSel; public float guideDemo; public bool guideOverlay; string guideBack = LOBBY;

    public void OpenGuide(bool fromPause)
    {
        Skills.Init();
        guideBack = fromPause ? FIGHT : LOBBY; guideDemo = 0;
        if (fromPause) guideOverlay = true; else Go(GUIDE);
    }
    void CloseGuide() { if (guideOverlay) guideOverlay = false; else Go(LOBBY); }

    /// <summary>Еркін алаң: мақсатты әдіспен шексіз жаттығу.</summary>
    public void StartArena(string selId)
    {
        Skills.Init();
        int ch = Progress.LastOpen();
        match.StartMode("arena", ch);
        match.arenaSel = selId != null ? selId : FirstOpenSkill();
        fv.Setup(match); Go(FIGHT);
    }
    static string FirstOpenSkill()
    {
        for (int i = Skills.All.Count() - 1; i >= 0; i--) if (Skills.Unlocked(Skills.All.Get(i))) return Skills.All.Get(i).id;
        return Skills.All.Get(0).id;
    }

    static Rect GCard(int i) { return new Rect(462 + (i % 2) * 398, 110 + (i / 2) * 180, 386, 170); }

    void DrawGuide()
    {
        Skills.Init();
        int n = Skills.All.Count(); guideSel = Mathf.Clamp(guideSel, 0, n - 1);
        guideDemo += Time.unscaledDeltaTime;
        // пернетақта: ← → ↑ ↓ — таңдау, Esc — артқа
        if (Event.current.type == EventType.Layout)
        {
            int s0 = guideSel;
            if (Inp.Tap(Inp.LEFT) || Inp.Tap(Inp.A)) guideSel = (guideSel - 1 + n) % n;
            if (Inp.Tap(Inp.RIGHT) || Inp.Tap(Inp.D)) guideSel = (guideSel + 1) % n;
            if (Inp.Tap(Inp.UP) || Inp.Tap(Inp.W)) guideSel = (guideSel - 2 + n) % n;
            if (Inp.Tap(Inp.DOWN) || Inp.Tap(Inp.S)) guideSel = (guideSel + 2) % n;
            if (s0 != guideSel) { guideDemo = 0; Snd.Play("ui_move", 0.5f, 0); }
            if (Inp.Tap(Inp.ESC)) { CloseGuide(); return; }
        }
        // фон: көк-қара градиент, киіз, қошқар мүйіз өрнегі
        Ui.Grad3(0, 0, 1280, 720, new Color32(29, 24, 64, 255), new Color32(20, 16, 42, 255), new Color32(11, 9, 24, 255));
        for (int y = 140; y < 720; y += 120) for (int x = 520; x < 1280; x += 120) Ui.Img("Art/ui/ram", x - 24, y - 12, 48, 24, 0.07f);
        Ui.TopBar("ӘДІСТЕР КІТАБЫ");
        if (Ui.DzBtn(guideOverlay ? "АРТҚА" : "МӘЗІР", 24, 18, 150, 46, false, true, null, true)) { CloseGuide(); return; }
        Ui.Text("← → ↑ ↓ — таңдау · Esc — артқа", 654, 90, 400, 20, 13, new Color(1, 0.9f, 0.75f, 0.55f), TextAnchor.MiddleCenter, true);

        // --- сол жақ: түсіндірме (пергамент) ---
        float px = 24, py = 110, pw = 420;
        Ui.DzPanel(px, py, pw, 596, "parch");
        Color ink = new Color32(42, 26, 16, 255), red = new Color32(122, 26, 18, 255), dred = new Color32(90, 22, 12, 255);
        Ui.Text("ҚАЛАЙ ОРЫНДАЛАДЫ?", px, py + 22, pw, 34, 25, dred, TextAnchor.MiddleCenter, true);
        Ui.Fill(px + pw / 2 - 150, py + 54, 300, 1.5f, new Color32(122, 74, 20, 153)); Ui.Img("Art/ui/ram", px + pw / 2 - 14, py + 52, 28, 14, 0.9f);
        Ui.Text("Әдіс — бағыттар тізбегі мен соққы батырмасы. Тізбекті бір қимылмен, шамамен 0,6 секунд ішінде енгіз (ұзын тізбекке сәл көбірек уақыт беріледі).", px + 24, py + 74, pw - 48, 84, 15, ink);
        string[] lt = { "F", "B", "D", "DF", "DB" }; string[] ll = { "алға — жауға қарай", "артқа — жаудан кері", "төмен", "төмен + алға бірге", "төмен + артқа бірге" };
        for (int i = 0; i < 5; i++)
        {
            int col = i < 3 ? 0 : 1, row = i < 3 ? i : i - 3; float gx = px + 34 + col * 196, gy = py + 172 + row * 36;
            Glyph(lt[i], gx, gy, 28, false, false); Ui.Text(ll[i], gx + 22, gy - 10, 170, 22, 14, ink, TextAnchor.MiddleLeft, true);
        }
        string[] bt = { "J", "K", "L" }; string[] bl = { "қару", "тебу", "күшті соққы" };
        for (int i = 0; i < 3; i++) { float gx = px + 34 + i * 120 + (i == 2 ? -8 : 0), gy = py + 172 + 3 * 36 + 6; Glyph(bt[i], gx, gy, 28, false, false); Ui.Text(bl[i], gx + 22, gy - 10, 110, 22, 14, ink, TextAnchor.MiddleLeft, true); }
        Ui.Text("Бағыт жауға қатысты: жау оң жақта тұрса → дегенің D, ← дегенің A. Жау сол жақта болса — керісінше.", px + 24, py + 300, pw - 48, 44, 14, red, TextAnchor.UpperLeft, true);
        string[] ka = { "Пернетақта:", "Телефон:" }; string[] kb = { "W A S D + J K L", "экрандағы батырмалар" };
        for (int i = 0; i < 2; i++) { Ui.Text(ka[i], px + 24, py + 360 + i * 22, 110, 20, 14, dred, TextAnchor.MiddleLeft, true); Ui.Text(kb[i], px + 132, py + 360 + i * 22, 260, 20, 14, ink, TextAnchor.MiddleLeft); }
        // үлгі: тізбек жанып-сөнеді, джойстик қозғалады
        SkDef d = Skills.All.Get(guideSel); bool open = Skills.Unlocked(d); float dy = py + 440;
        Ui.Fill(px + 18, dy, pw - 36, 100, new Color32(60, 30, 10, 31)); Ui.Frame(px + 18, dy, pw - 36, 100, new Color32(122, 74, 20, 115), 1.2f);
        Ui.Text("Үлгі: " + d.name, px + 32, dy + 8, pw - 60, 24, 15, dred, TextAnchor.MiddleLeft, true);
        int ns = d.seq.Length; float per = 0.32f, cyc = ns * per + 1, tc = guideDemo % cyc; int step = tc < ns * per ? (int)(tc / per) : -1;
        string tok = step >= 0 ? d.seq[step] : null;
        float jx = px + 66, jy = dy + 66, jr = 26, kx = 0, ky = 0;
        string dirTok = tok != null && !IsBtn(tok) ? tok : null;
        if (dirTok != null) { float a = Ang(dirTok); kx = Mathf.Cos(a) * jr * 0.7f; ky = Mathf.Sin(a) * jr * 0.7f; }
        Ui.Circle(jx, jy, jr + 4, new Color32(40, 24, 10, 64));
        Ui.Circle(jx + kx, jy + ky, 14, Ui.GOLD); Ui.Circle(jx + kx, jy + ky, 10, new Color32(142, 29, 28, 255));
        for (int i = 0; i < 3; i++) Glyph(bt[i], px + 118 + i * 34, jy, 28, tok == bt[i], false);
        SeqDraw(d, px + 230, jy, 24, step < 0 ? -1 : step + 1, !open, true);
        Ui.Text("Тәжірибе жеңістен және әр әдісті алғаш қолданғаннан жиналады. Деңгей өскен сайын жаңа әдіс ашылады.", px + 24, py + 536, pw - 48, 50, 13, new Color32(90, 58, 26, 255), TextAnchor.UpperLeft, true);

        // --- оң жақ: әдіс карталары (бетке 6) ---
        int pg = guideSel / 6, pages = (n + 5) / 6;
        for (int i = 0; i < 6; i++)
        {
            int idx = pg * 6 + i; if (idx >= n) break;
            Rect r = GCard(i); float k = Mathf.Clamp01((pt - i * 0.05f) / 0.35f); k = 1 - (1 - k) * (1 - k) * (1 - k);
            Color o = GUI.color; GUI.color = new Color(1, 1, 1, k);
            Card(Skills.All.Get(idx), r.x + (1 - k) * 40, r.y, r.width, r.height, idx == guideSel);
            GUI.color = o;
            if (GUI.Button(Ui.R(r.x, r.y, r.width, r.height), GUIContent.none, GUIStyle.none) && guideSel != idx) { guideSel = idx; guideDemo = 0; Snd.Play("ui_move", 0.5f, 0); }
        }
        Ui.TextO((pg + 1) + " / " + pages, 644, 662, 100, 30, 22, new Color32(255, 227, 160, 255), TextAnchor.MiddleCenter, true, 2);
        for (int i = 0; i < pages; i++) Ui.Circle(676 + i * 18, 700, 4, i == pg ? (Color)new Color32(244, 197, 92, 255) : new Color32(244, 197, 92, 77));
        if (Ui.DzBtn("◀ АЛДЫҢҒЫ", 462, 656, 150, 46, false, true, null, true)) { guideSel = (guideSel - 6 + n) % n; guideDemo = 0; }
        if (Ui.DzBtn("КЕЛЕСІ ▶", 1096, 656, 150, 46, false, true, null, true)) { guideSel = (guideSel + 6) % n; guideDemo = 0; }
        if (!guideOverlay && Ui.DzBtn("ЕРКІН АЛАҢДА ЖАТТЫҒУ", 776, 656, 304, 46, false, open, null, true))
        {
            if (open) StartArena(d.id); else { Toast("Бұл әдіс " + d.lvl + "-деңгейде ашылады"); Snd.Play("ui_error"); }
        }
    }

    // ---------- карта ----------
    void Card(SkDef d, float x, float y, float w, float h, bool sel)
    {
        bool open = Skills.Unlocked(d); float T = Time.unscaledTime;
        int kc = Col.Parse(Skills.KIND_COL[Skills.KindIdx(d.kind)]); Color kcol = new Color32((byte)Col.R(kc), (byte)Col.G(kc), (byte)Col.B(kc), 255);
        if (sel) { Color g = kcol; g.a = 0.18f + 0.08f * Mathf.Sin(T * 4); Ui.Fill(x - 8, y - 8, w + 16, h + 16, g); }
        Ui.Fill(x, y, w, h, open ? new Color32(20, 15, 40, 240) : new Color32(26, 22, 24, 235));
        Ui.Frame(x + 1, y + 1, w - 2, h - 2, sel ? (Color)new Color32(255, 227, 154, 255) : open ? new Color32(244, 197, 92, 140) : new Color32(140, 120, 100, 128), sel ? 3 : 1.6f);
        if (sel) Ui.Frame(x + 6, y + 6, w - 12, h - 12, new Color32(255, 225, 160, 77), 1);
        Ui.Img("Art/sk/ic_" + d.kind + "_" + (open ? 0 : 1), x + 40 - 36 * 24 / 30f, y + 42 - 36 * 24 / 30f, 72 * 24 / 30f, 72 * 24 / 30f);
        int ns = Ui.Fit(d.name, w - (open ? 170 : 130), 26);
        if (open) Ui.TextO(d.name, x + 76, y + 28, w - 120, 36, ns, Ui.GOLD, TextAnchor.MiddleLeft, true, 2);
        else Ui.Text(d.name, x + 76, y + 28, w - 120, 36, ns, new Color32(168, 154, 138, 255), TextAnchor.MiddleLeft, true);
        if (open) Ui.Text(d.lvl + "-деңгей", x + w - 116, y + 20, 100, 20, 14, new Color(1, 0.9f, 0.75f, 0.7f), TextAnchor.MiddleRight, true);
        else Ui.Img("Art/sk/lock", x + w - 46, y + 12, 32, 32);
        int lit = 0;
        if (sel && open) { int n = d.seq.Length; float per = 0.32f, cyc = n * per + 1, tc = guideDemo % cyc; lit = tc < n * per ? (int)(tc / per) + 1 : -1; }
        SeqDraw(d, x + 22, y + 86, 32, lit, !open, false);
        Ui.Text(d.desc, x + 20, y + 108, w - 40, 44, 15, open ? (Color)new Color32(239, 226, 204, 255) : new Color32(154, 142, 132, 255));
        string meta = (d.ult ? "Рух: толық шкала" : d.cost > 0 ? "Рух: " + U.Num(d.cost) : "Рух: керек емес") + " · Күту: " + U.Num(d.cd) + " с" + (open ? " · " + Skills.KIND_NAME[Skills.KindIdx(d.kind)] : "");
        Ui.Text(meta, x + 20, y + h - 26, w - 40, 20, 13, open ? new Color(1, 0.83f, 0.42f, 0.85f) : new Color(0.63f, 0.55f, 0.47f, 0.7f), TextAnchor.MiddleLeft, true);
        if (!open) Ui.Text(d.lvl + "-деңгейде ашылады", x + w - 220, y + h - 26, 202, 20, 13, new Color32(240, 168, 136, 255), TextAnchor.MiddleRight, true);
    }

    // ---------- тізбек белгілері (веб: skGlyph / skSeqDraw) ----------
    static bool IsBtn(string t) { return t == "J" || t == "K" || t == "L"; }
    static float Ang(string t)
    {
        switch (t) { case "F": return 0; case "DF": return Mathf.PI / 4; case "D": return Mathf.PI / 2; case "DB": return Mathf.PI * 0.75f; case "B": return Mathf.PI; case "UB": return -Mathf.PI * 0.75f; case "U": return -Mathf.PI / 2; case "UF": return -Mathf.PI / 4; }
        return 0;
    }
    /// <summary>Бір белгі: орталығы (cx, cy), өлшемі s (сурет 56 өлшемде салынған, 72-лік кенепте).</summary>
    public static void Glyph(string tok, float cx, float cy, float s, bool lit, bool dim)
    {
        float k = s / 56f, sz = 72 * k;
        Ui.Img("Art/sk/g_" + tok + "_" + (lit ? 1 : 0), cx - sz / 2, cy - sz / 2, sz, sz, dim ? 0.38f : 1);
    }
    static float Tag(string label, float x, float y, float h, Color col)
    {
        int fs = Mathf.RoundToInt(h * 0.58f); float w = Ui.TextW(label, fs) + h * 0.8f;
        Ui.Fill(x, y - h / 2, w, h, new Color32(10, 6, 20, 204)); Ui.Frame(x, y - h / 2, w, h, col, 1.2f);
        Ui.Text(label, x, y - h / 2, w, h, fs, col, TextAnchor.MiddleCenter, true);
        return w;
    }
    public static float SeqDraw(SkDef d, float x, float y, float s, int lit, bool dim, bool noTag)
    {
        float cx = x, gap = s * 0.16f; int n = d.seq.Length;
        if (d.air && !noTag) cx += Tag("ауада", cx, y, s * 0.62f, new Color32(191, 239, 255, 255)) + gap;
        for (int i = 0; i < n; i++)
        {
            string tok = d.seq[i];
            if (i == n - 1 && i > 0 && !IsBtn(d.seq[i - 1])) { Ui.TextO("+", cx, y - s * 0.4f, s * 0.44f, s * 0.8f, Mathf.RoundToInt(s * 0.62f), new Color32(255, 227, 160, 255), TextAnchor.MiddleCenter, true, 2); cx += s * 0.44f; }
            Glyph(tok, cx + s / 2, y, s, lit < 0 || i < lit, dim);
            cx += s + gap;
        }
        if (d.ult && !noTag) cx += Tag("РУХ 100%", cx, y, s * 0.62f, new Color32(255, 178, 122, 255)) + gap;
        return cx - gap;
    }

    // ---------- ЖАҢА ДЕҢГЕЙ жолағы (веб: SKXP.ban) ----------
    int lvlSeen = -1, banLvl; float banT = 99; readonly Lst<SkDef> banSk = new Lst<SkDef>();
    void LevelWatch(float dt)
    {
        int L = Progress.Level();
        if (lvlSeen < 0) lvlSeen = L;
        if (L > lvlSeen)
        {
            Skills.Init(); banSk.Clear();
            for (int i = 0; i < Skills.All.Count(); i++) { SkDef d = Skills.All.Get(i); if (d.lvl > lvlSeen && d.lvl <= L) banSk.Add(d); }
            banLvl = L; banT = 0; lvlSeen = L; Snd.Play("levelup");
        }
        banT += dt;
    }
    void DrawLevelBanner()
    {
        if (banT > 4.4f) return;
        float k = Mathf.Clamp01(banT / 0.4f); k = 1 + 2.7f * Mathf.Pow(k - 1, 3) + 1.7f * Mathf.Pow(k - 1, 2);
        float a = Mathf.Clamp01(Mathf.Min(banT * 5, (4.4f - banT) * 3)); int n = Mathf.Min(2, banSk.Count());
        float h = 78 + n * 40 + (banSk.Count() > 2 ? 18 : 0), x = 640 - 280, y = 12 - (1 - k) * 40;
        Color o = GUI.color; GUI.color = new Color(1, 1, 1, a);
        Ui.DzPanel(x, y, 560, h, "red");
        Ui.TextO("ЖАҢА ДЕҢГЕЙ: " + banLvl + "!", x, y + 22, 560, 40, 34, Ui.GOLD, TextAnchor.MiddleCenter, true, 3);
        for (int i = 0; i < n; i++)
        {
            SkDef d = banSk.Get(i); float yy = y + 86 + i * 40; string t = "Жаңа әдіс: " + d.name;
            float tw = Ui.TextW(t, 19), sw = d.seq.Length * 26 * 1.16f + 12, x0 = 640 - (tw + 18 + sw) / 2;
            Ui.TextO(t, x0, yy - 14, tw + 4, 28, 19, new Color32(255, 233, 184, 255), TextAnchor.MiddleLeft, true, 2);
            SeqDraw(d, x0 + tw + 18, yy, 26, -1, false, true);
        }
        if (banSk.Count() > 2) Ui.Text("тағы " + (banSk.Count() - 2) + " әдіс — Әдістер кітабында", x, y + h - 26, 560, 20, 13, new Color32(255, 211, 160, 255), TextAnchor.MiddleCenter, true);
        GUI.color = o;
    }
}
