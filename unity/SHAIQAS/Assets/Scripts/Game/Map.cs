// SHAIQAS · Жорық картасы 2.0 (веб: drawCampMap / drawMapPage / campMapUI) және режим экрандары:
// турнир торы (drawBracket), «VS» (drawVs), жекпе-жек таңдауы (drawDuelSel).
using UnityEngine;

public partial class GameRoot
{
    public const string BRACKET = "bracket", VS = "vs", DUELSEL = "duelsel";
    public string mapView = "map", mapPop;   // map — режимдер картасы, stages — негізгі жорық кезеңдері
    public int mapSite;
    float popT;

    // веб: CAMP_SITES
    static readonly string[] SITE_ID = { "story", "train", "tour", "surv", "duel", "boss" };
    static readonly float[] SITE_X = { 640, 206, 1074, 206, 1074, 640 }, SITE_Y = { 296, 222, 222, 474, 474, 548 }, SITE_R = { 84, 56, 56, 56, 56, 46 };
    static readonly string[] SITE_NAME = { "", "ЖАТТЫҒУ АЛАҢЫ", "ХАНДЫҚ ТУРНИРІ", "ТӨЗІМ ҚАМАЛЫ", "ЖЕКПЕ-ЖЕК", "БАСТАР ШАБУЫЛЫ" };
    static readonly string[] SITE_COL = { "", "#3f7a3a", "#a32222", "#6a5a4a", "#2f5f9a", "#5a2a7a" };

    static Color H(string hex) { int c = Col.Parse(hex); return new Color32((byte)Col.R(c), (byte)Col.G(c), (byte)Col.B(c), (byte)Col.A(c)); }
    static Color HA(string hex, float a) { Color c = H(hex); c.a = a; return c; }
    /// <summary>Веб: text(g, s, x, y_baseline, size) — орталық/сол жақ.</summary>
    static void T(string s, float x, float y, int size, Color c, bool left = false, bool bold = true)
    {
        // қараңғы мәтін — көлеңкесіз (веб: text), ашық мәтін — жұқа көлеңкемен
        bool lightTxt = c.r + c.g + c.b > 1.8f;
        if (left) { if (lightTxt) Ui.TextS(s, x, y - size * 0.92f, 900, size * 1.3f, size, c, TextAnchor.MiddleLeft, bold); else Ui.Text(s, x, y - size * 0.92f, 900, size * 1.3f, size, c, TextAnchor.MiddleLeft, bold); }
        else { if (lightTxt) Ui.TextS(s, x - 450, y - size * 0.92f, 900, size * 1.3f, size, c, TextAnchor.MiddleCenter, bold); else Ui.Text(s, x - 450, y - size * 0.92f, 900, size * 1.3f, size, c, TextAnchor.MiddleCenter, bold); }
    }
    static void TO(string s, float x, float y, int size, Color c) { Ui.TextO(s, x - 450, y - size * 0.92f, 900, size * 1.3f, size, c, TextAnchor.MiddleCenter, true, 2); }
    static void Wrap(string s, float cx, float y, float w, int size, Color c, float h = 80) { Ui.TextS(s, cx - w / 2, y - size, w, h, size, c, TextAnchor.UpperCenter, false); }
    static void Star(float x, float y, float r, bool on) { Ui.Img("Art/camp/star_" + (on ? 1 : 0), x - r * 16 / 14f, y - r * 16 / 14f, r * 32 / 14f, r * 32 / 14f); }
    static void Dashed(float x0, float y0, float cx, float cy, float x1, float y1, Color c, float t)
    {
        // квадрат қисық бойымен үзік сызық (веб: setLineDash [9,8])
        float len = 0, px = x0, py = y0; int N = 60;
        for (int i = 1; i <= N; i++)
        {
            float u = i / (float)N, a = (1 - u) * (1 - u), b = 2 * (1 - u) * u, d = u * u;
            float qx = a * x0 + b * cx + d * x1, qy = a * y0 + b * cy + d * y1, sl = Mathf.Sqrt((qx - px) * (qx - px) + (qy - py) * (qy - py));
            float ph = (len + t) % 17f;
            if (ph < 9) Ui.Fill(qx - 1.5f, qy - 1.5f, 3, 3, c);
            len += sl; px = qx; py = qy;
        }
    }

    bool SiteLocked(int i) { int ci = mapCh; return !Progress.Unlocked(ci) || (i == 5 && Game.S.prog[ci] < 4 && !Progress.TestUnlockAll); }
    static string SiteSub(string id, int ci)
    {
        SaveData S = Game.S;
        if (id == "train") return "Әр жеңіс: +" + (int)(Match.ECO_K * (15 + ci * 6)) + " теңге";
        if (id == "tour") { int n = 0; for (int i = 0; i < 3; i++) if (S.tourW[ci][i] > 0) n++; return "Кубоктар: " + n + "/3"; }
        if (id == "surv") return "Рекорд: " + S.surv[ci] + " толқын";
        if (id == "duel") return Db.J("RATING_TIERS").At(Match.RatingTier(S.rating)).S("name") + " · " + S.rating;
        if (id == "boss") return S.prog[ci] >= 4 || Progress.TestUnlockAll ? "Рекорд: " + S.bossRec + " бас жау" : "Жорықты аяқтасаң ашылады";
        return "";
    }
    static float PopX(string id) { for (int i = 0; i < 6; i++) if (SITE_ID[i] == id) return SITE_X[i] > 640 ? 44 : 856; return 856; }

    public void OpenMapHub(int ch) { mapCh = Mathf.Clamp(ch, 0, Db.NCH - 1); mapView = "map"; mapPop = null; Go(MAP); }
    void SetPage(int ci) { if (ci < 0 || ci >= Db.NCH) return; mapCh = ci; mapStage = Mathf.Clamp(Game.S.prog[ci], 0, 3); mapPop = null; pt = 0; Snd.Play("ui_move", 0.5f); }
    void OpenSite(int i)
    {
        mapSite = i;
        if (!Progress.Unlocked(mapCh)) { Toast(Progress.LockMsg(mapCh)); return; }
        if (i == 0) { mapView = "stages"; mapStage = Mathf.Min(3, Game.S.prog[mapCh]); pt = 0; return; }
        mapPop = SITE_ID[i]; popT = Time.unscaledTime;
    }

    // ================= КАРТА =================
    void DrawMap()
    {
        if (mapView == "stages") { DrawStages(); return; }
        int ci = mapCh; JNode C = Db.Ch(ci); bool open = Progress.Unlocked(ci); float t = Time.unscaledTime;
        Ui.Grad3(0, 0, 1280, 720, H("#2a1810"), H("#201209"), H("#140b07"));
        float slide = Mathf.Clamp01(pt * 4);
        Ui.Fill(24 + 8, 92 + 10, 1232, 560, new Color(0, 0, 0, 0.5f * slide));
        Ui.Img("Art/campwide/wide_" + ci, 24, 92, 1232, 560, slide);
        for (int i = 1; i < 6; i++)
        {
            float sx = SITE_X[i], sy = SITE_Y[i], mx = (640 + sx) / 2, my = (296 + sy) / 2 + (sy < 296 ? -30 : 30) * (sx == 640 ? 0 : 1);
            Dashed(640, 296, mx, my, sx, sy, new Color(0.47f, 0.12f, 0.08f, 0.55f), t * 14);
        }
        int selI = mapPop != null ? System.Array.IndexOf(SITE_ID, mapPop) : mapSite;
        for (int i = 1; i < 6; i++) DrawSite(i, i == selI, SiteLocked(i), t, ci);
        DrawStorySite(selI == 0, t, ci, open);
        if (!open)
        {
            Ui.Fill(24, 92, 1232, 560, new Color(0.08f, 0.05f, 0.03f, 0.35f));
            string lm = Progress.LockMsg(ci); float tw = Ui.TextW(lm, 24) + 60;
            Ui.Nine("Art/camp/pill", 640 - tw / 2, 92 + 14, tw, 44, 20, 20, 20, 20);
            T(lm, 640, 92 + 45, 24, H("#ffe3a0"));
        }
        // төменгі жолақ
        Ui.Fill(0, 658, 1280, 62, new Color(0.04f, 0.024f, 0.016f, 0.78f));
        Ui.Fill(0, 657, 1280, 2, Ui.GOLD);
        string tt = Db.Roman(ci) + " ТАРАУ · " + U.Upper(C.S("name")) + " · " + Db.Years.At(ci).Str();
        TO(tt, 640, 686, Ui.Fit(tt, 820, 24), new Color32(255, 214, 120, 255));
        for (int i = 0; i < Db.NCH; i++)
        {
            float dx = 640 + (i - (Db.NCH - 1) / 2f) * 28, r = i == ci ? 7 : 5;
            Ui.ImgC("Art/camp/glow", dx - r * 1.6f, 706 - r * 1.6f, r * 3.2f, r * 3.2f, i == ci ? Ui.GOLD : Progress.Unlocked(i) ? new Color(0.91f, 0.68f, 0.24f, 0.6f) : new Color(0.47f, 0.43f, 0.4f, 0.6f));
        }
        Ui.TopBar("ЖОРЫҚ");
        if (mapPop != null) { DrawPop(ci, open); return; }
        // батырмалар (веб: campMapUI)
        for (int i = 0; i < 6; i++) if (Ui.Hot(SITE_X[i] - SITE_R[i] - 8, SITE_Y[i] - SITE_R[i] - 8, (SITE_R[i] + 8) * 2, (SITE_R[i] + 8) * 2 + (i > 0 ? 30 : 0))) OpenSite(i);
        if (Ui.DzBtn("◀ АЛДЫҢҒЫ", 24, 664, 176, 46, false, ci > 0, null, true)) SetPage(ci - 1);
        if (Ui.DzBtn("КЕЛЕСІ ▶", 1080, 664, 176, 46, false, ci < Db.NCH - 1, null, true)) SetPage(ci + 1);
        if (Ui.DzBtn("МӘЗІР", 24, 18, 150, 46, false, true, null, true)) Go(LOBBY);
        if (Progress.QuizGate(ci) && Ui.DzBtn(Db.Roman(ci - 1) + " ТАРАУ ВИКТОРИНАСЫ ▶", 450, 470, 380, 60, true)) StartQuiz(ci - 1, true);
    }

    void DrawSite(int i, bool sel, bool locked, float t, int ci)
    {
        float x = SITE_X[i], y = SITE_Y[i], r = SITE_R[i], bob = sel ? Mathf.Sin(t * 4) * 2.5f : 0, Y = y + bob;
        Ui.Img("Art/camp/shadow", x - r * 0.95f, y + r + 6 - 16, r * 1.9f, 32);
        if (sel)
        {
            Ui.ImgC("Art/camp/glow", x - r * 2, Y - r * 2, r * 4, r * 4, new Color(1, 0.81f, 0.42f, 0.55f + 0.2f * Mathf.Sin(t * 5)));
            float rr = r + 13 + Mathf.Sin(t * 4) * 2;
            Ui.ImgC("Art/camp/ring", x - rr * 1.1f, Y - rr * 1.1f, rr * 2.2f, rr * 2.2f, new Color(1, 0.84f, 0.47f, 0.7f + 0.3f * Mathf.Sin(t * 6)));
        }
        string sid = SITE_ID[i];
        Ui.Img("Art/camp/site_" + sid + "_" + (locked ? 1 : 0), x - r * 76 / 56f, Y - r * 76 / 56f, r * 152 / 56f, r * 152 / 56f);
        // тақта
        string title = SITE_NAME[i], sub = SiteSub(sid, ci);
        float w = Mathf.Max(Ui.TextW(title, 16), Ui.TextW(sub, 13, false)) + 30, h = 44, py = y + r + 12;
        Ui.Nine("Art/camp/plate_" + (sel ? 1 : 0), x - w / 2 - 4, py - 4, w + 8, h + 8, 14, 14, 14, 14);
        T(title, x, py + 20, 16, H("#3a2410"));
        T(sub, x, py + 37, 13, H("#7a4a2a"), false, false);
    }

    void DrawStorySite(bool sel, float t, int ci, bool open)
    {
        float x = 640, y = 296, r = 84, pulse = 0.5f + 0.5f * Mathf.Sin(t * 3);
        Ui.ImgRot("Art/camp/rays", x, y, 400, 400, t * 0.15f * Mathf.Rad2Deg, new Color(1, 1, 1, (0.10f + 0.06f * pulse) * (sel ? 1.4f : 1)));
        Ui.ImgC("Art/camp/glow", x - r * 2.2f, y - r * 2.2f, r * 4.4f, r * 4.4f, new Color(1, 0.81f, 0.42f, 0.5f + 0.3f * pulse));
        float ph = (t * 0.6f) % 1, wr = r + 10 + ph * 40;
        Ui.ImgC("Art/camp/ring", x - wr * 1.1f, y - wr * 1.1f, wr * 2.2f, wr * 2.2f, new Color(1, 0.84f, 0.47f, (1 - ph) * 0.7f));
        Ui.Img("Art/camp/story_" + ci, x - 110, y - 110, 220, 220);
        float gr = r + 13;
        Ui.ImgC("Art/camp/ring", x - gr * 1.1f, y - gr * 1.1f, gr * 2.2f, gr * 2.2f, new Color(1, (200 + 40 * pulse) / 255f, 0.47f, 1));
        if (sel) { float sr = r + 24; Ui.ImgC("Art/camp/ring", x - sr * 1.1f, y - sr * 1.1f, sr * 2.2f, sr * 2.2f, new Color(1, 0.94f, 0.78f, 0.6f + 0.4f * pulse)); }
        if (!open) { Ui.ImgC("Art/camp/glow", x - r, y - r, r * 2, r * 2, new Color(0.08f, 0.05f, 0.03f, 0.6f)); Ui.Img("Art/camp/lock", x - 32, y - 36, 64, 64); }
        Ui.Ribbon(x, y - r - 40, 330, "НЕГІЗГІ ЖОРЫҚ", 26);
        // прогресс тақтасы
        float py = y + r + 22;
        Ui.Img("Art/camp/point_" + (sel ? 1 : 0), x - 205 - 20, py - 14, 450, 90);
        int prog = Game.S.prog[ci], tot = 0; for (int i = 0; i < 4; i++) tot += Progress.StarCount(Game.S.stars[ci][i]);
        int done = Mathf.Min(4, prog);
        T("Ұрыстар:", x - 150, py + 25, 16, H("#ffe3a0"), true);
        for (int i = 0; i < 4; i++) { float dx = x - 56 + i * 22; Ui.Text("◆", dx - 10, py + 7, 20, 24, 16, i < done ? (Color)Ui.GOLD : new Color(1, 0.9f, 0.7f, 0.25f), TextAnchor.MiddleCenter, true); }
        T(done + "/4", x + 26, py + 25, 16, H("#fff3e0"), true);
        Star(x + 92, py + 19, 9, true); T(tot + "/12", x + 106, py + 25, 16, H("#fff3e0"), true);
        DiffLine(x, py + 45, true, ci, Mathf.Min(3, prog));
    }

    /// <summary>«Сен үшін: ҚАЛЫПТЫ ★★★☆☆ · жабдығың лайық» (веб: campDiffLine).</summary>
    void DiffLine(float cx, float y, bool light, int ch, int si)
    {
        DiffInfo I = Eco.Stage(ch, si);
        string a = "Сен үшін:", b = "· " + I.why;
        float wa = Ui.TextW(a, 14), wl = Ui.TextW(I.label, 16), wb = Ui.TextW(b, 12, false);
        float full = wa + 7 + wl + 7 + 55 + 7 + wb; bool showB = full <= 372; float tot = showB ? full : full - 7 - wb, x0 = cx - tot / 2;
        T(a, x0, y + 5, 14, light ? H("#ffe3a0") : H("#5a3a20"), true);
        T(I.label, x0 + wa + 7, y + 6, 16, H(light ? I.colL : I.col), true);
        for (int k = 0; k < 5; k++) Star(x0 + wa + 7 + wl + 7 + 5 + k * 11, y, 5, k < I.stars);
        if (showB) T(b, x0 + wa + 7 + wl + 7 + 55 + 7, y + 5, 12, light ? new Color(1, 0.93f, 0.78f, 0.85f) : H("#7a4a2a"), true, false);
    }

    // ---------------- орын терезесі ----------------
    void DrawPop(int ci, bool open)
    {
        float k = Mathf.Clamp01((Time.unscaledTime - popT) / 0.26f); k = 1 - (1 - k) * (1 - k) * (1 - k);
        Ui.Fill(0, 92, 1280, 566, new Color(0.04f, 0.024f, 0.016f, 0.5f * k));
        float px = PopX(mapPop), dx = px - 870 + (1 - k) * (px > 640 ? 40 : -40);
        Color o = GUI.color; GUI.color = new Color(1, 1, 1, k);
        SidePanel(ci, mapPop, dx);
        GUI.color = o;
        string mm = mapPop; int prog = Progress.TestUnlockAll ? 4 : Game.S.prog[ci];
        bool can = mm == "boss" ? open && prog >= 4 : open;
        Run tsv = mm == "tour" ? Match.TourSaved(ci) : null;
        string lbl = mm == "train" ? "ЖАТТЫҒУ" : mm == "tour" ? (tsv != null ? "ТУРНИРДІ ЖАЛҒАСТЫРУ" : "ТУРНИРДІ БАСТАУ") : mm == "surv" ? "БАСТАУ" : mm == "duel" ? "ЖЕКПЕ-ЖЕК" : "БАСТАР ШАБУЫЛЫ";
        float bx = px - 870;
        if (Ui.DzBtn(can ? lbl : "ЖАБЫҚ", 892 + bx, 590, 336, 56, true, can))
        {
            if (mm == "duel") GoDuelSel(ci); else StartModeUI(mm, ci);
        }
        if (mm == "tour")
        {
            JNode TT = Db.J("TOUR_TIERS"); int curT = tsv != null ? tsv.tier : Match.TierOpen(ci, Game.S.tourTier) ? Game.S.tourTier : 0;
            for (int i = 0; i < 3; i++)
            {
                bool ok = open && Match.TierOpen(ci, i);
                if (Ui.DzBtn(ok ? TT.At(i).S("name") : "ЖАБЫҚ", 896 + bx + i * 112, 246, 104, 36, false, true, null, true, true, true, i == curT && ok))
                {
                    if (ok && tsv != null && i != tsv.tier) { Snd.Play("ui_error"); Toast("Алдымен басталған " + U.Lower(TT.At(tsv.tier).S("name")) + " турнирін аяқта", 2.6f); }
                    else if (ok) { Game.S.tourTier = i; Game.Persist(); }
                    else Toast(open ? "Алдымен " + U.Lower(TT.At(i - 1).S("name")) + " турнирін жең" : Progress.LockMsg(ci));
                }
            }
        }
        if (Ui.Hot(px + 380 - 56, 100, 44, 44)) { mapPop = null; Snd.Play("ui_back"); }
        Ui.TextO("✕", px + 380 - 56, 100, 44, 44, 24, Ui.CREAM);
        if (Ui.DzBtn("◀ КАРТА", 24, 18, 150, 46, false, true, null, true)) mapPop = null;
    }

    /// <summary>Оң жақ пергамент панель (веб: drawMapPage, _sideOnly). dx — көлденең жылжу.</summary>
    void SidePanel(int ci, string mm, float dx)
    {
        float px = 870 + dx, pw = 380, cx = px + pw / 2;
        JNode C = Db.Ch(ci); SaveData S = Game.S;
        Ui.DzPanel(px, 92, pw, 560, "parch");
        Ui.Fill(px + 10, 96, pw - 20, 92, new Color(0.48f, 0.29f, 0.08f, 0.18f));
        if (mm == "story")
        {
            T("Ұрысты таңда", cx, 132, 26, H("#3a2410"));
            Wrap("Жау ұрыс сайын күшейеді. Жабдығыңды жаңартып отыр — сонда жеңіс оңай болады!", cx, 158, pw - 60, 13, H("#6a4a2a"));
        }
        else
        {
            int si = System.Array.IndexOf(SITE_ID, mm);
            Ui.Img("Art/camp/site_" + mm + "_0", px + 70 - 46, 142 - 46, 92, 92);
            string nm = SITE_NAME[si];
            T(nm, px + 120, 138, Ui.Fit(nm, pw - 190, 24), H("#3a2410"), true);
            T(SiteSub(mm, ci), px + 120, 162, 14, H("#7a4a2a"), true, false);
        }
        if (mm == "story")
        {
            int si = mapStage; JNode e = C.Get("enemies").At(si); bool boss = e.Bk("boss");
            Thumb(px, pw, C.S("stage"), "Art/efront/efront_" + ci + "_" + si, boss);
            T((si + 1) + "-ұрыс · " + C.Get("nodes").At(si).S("n"), cx, 372, 18, H("#5a3a20"), false, false);
            T(e.S("name") + (boss ? " · БАС ЖАУ" : ""), cx, 400, 24, boss ? H("#8a1a12") : H("#3a2410"));
            T("Қаруы: " + Db.Weapon(e.S("weapon")).S("name"), cx, 424, 15, H("#5a3a20"), false, false);
            int sm = S.stars[ci][si], tot = 0; for (int i = 0; i < 4; i++) tot += Progress.StarCount(S.stars[ci][i]);
            Ui.Fill(px + 28, 204, 104, 30, new Color(0.08f, 0.05f, 0.03f, 0.72f));
            for (int q = 0; q < 3; q++) Star(px + 48 + q * 32, 219, 11, (sm & (1 << q)) != 0);
            Ui.Fill(px + pw - 120, 204, 92, 30, new Color(0.08f, 0.05f, 0.03f, 0.72f));
            Star(px + pw - 102, 219, 9, true); T(tot + "/12", px + pw - 88, 226, 18, H("#fff3e0"), true);
            DiffBox(px, pw, 434, ci, si, Progress.Unlocked(ci) && si < S.prog[ci]);
        }
        else if (mm == "train")
        {
            Thumb(px, pw, C.S("stage"), "Art/efront/efront_trainer", false);
            T("Жаттығу алаңы", cx, 380, 24, H("#3a2410"));
            Wrap("Жаттықтырушымен бір раундтық ұрыс. Жаңа қимылдарды үйреніп, теңге жина. Шексіз қайталауға болады.", cx, 404, pw - 50, 15, H("#4a3420"));
            Reward(cx, (int)(Match.ECO_K * (15 + ci * 6)), 500, "Әр жеңіс үшін");
        }
        else if (mm == "tour")
        {
            int tq = Match.TierOpen(ci, S.tourTier) ? S.tourTier : 0; JNode TT = Db.J("TOUR_TIERS").At(tq), rule = Db.J("TOUR_RULES").Get(Match.TourRule(ci, tq));
            T("Хандық турнирі", cx, 230, 26, H("#3a2410"));
            for (int i = 0; i < 3; i++)
            {
                float tx = px + 95 + i * 95;
                if (i == tq) Ui.ImgC("Art/camp/glow", tx - 40, 330 - 40, 80, 80, new Color(0.91f, 0.68f, 0.24f, 0.5f));
                Ui.Img("Art/camp/trophy_" + i + "_" + (S.tourW[ci][i] > 0 ? 1 : 0), tx - 40, 334 - 44, 80, 80);
                if (S.tourW[ci][i] > 0) T("ЖЕҢІЛДІ", tx, 378, 12, H("#3c7a3a"));
            }
            T("Ереже: " + rule.S("name"), cx, 404, 20, H("#8a1a12"));
            Wrap(rule.S("desc"), cx, 428, pw - 50, 15, H("#4a3420"));
            T("8 қатысушы · 3 ұрыс · финалда бас жау", cx, 478, 15, H("#5a3a20"), false, false);
            Reward(cx, (int)(Match.ECO_K * Mathf.Round((120 + ci * 60) * TT.Fk("pay"))), 518, S.tourW[ci][tq] > 0 ? "Бұл кубок саған бұрын бұйырған" : tq == 2 ? "Чемпионға сыйлық: Бәйге бөркі" : "Чемпионға");
        }
        else if (mm == "surv")
        {
            T("Төзім сынағы", cx, 228, 26, H("#3a2410"));
            T(U.Int(S.surv[ci]), cx, 312, 76, H("#8a1a12"));
            T("толқын — рекорд", cx, 340, 18, H("#5a3a20"), false, false);
            Wrap("Жаулар толқын-толқын шабуылдайды, бара-бара күшейеді. Әр бесінші толқында — бас жау. Денсаулық толық қалпына келмейді.", cx, 372, pw - 50, 15, H("#4a3420"));
            Reward(cx, (int)(Match.ECO_K * (8 + ci * 4)), 500, "Әр толқын үшін");
        }
        else if (mm == "duel")
        {
            JNode RT = Db.J("RATING_TIERS"); int ti = Match.RatingTier(S.rating); JNode Tr = RT.At(ti), nx = ti + 1 < RT.Count() ? RT.At(ti + 1) : null;
            T("Жекпе-жек", cx, 228, 26, H("#3a2410"));
            Ui.ImgC("Art/camp/glow", cx - 44, 292 - 44, 88, 88, H(Tr.S("col")));
            Ui.Medal(cx, 292, 42, "2f5f9a"); Ui.ImgC("Art/camp/glow", cx - 40, 252, 80, 80, H(Tr.S("col"))); Star(cx, 292, 22, true);
            T(Tr.S("name"), cx, 362, 24, H("#3a2410"));
            T("Рейтинг: " + S.rating + (nx != null ? " · келесі деңгей " + nx.Ik("min") : ""), cx, 388, 16, H("#5a3a20"), false, false);
            if (nx != null) { float kk = Pz.Clamp((S.rating - Tr.Ik("min")) / (float)(nx.Ik("min") - Tr.Ik("min")), 0, 1); Ui.Bar(px + 60, 400, pw - 120, 10, kk, H(nx.S("col")), H("#4a3a2a")); }
            Wrap("Батырыңды, қарсыласыңды және алаңды өзің таңда. Компьютерге қарсы ойна, рейтингіңді көтер.", cx, 440, pw - 50, 15, H("#4a3420"));
            T("Жеңіс: +25 рейтинг · жеңіліс: −15", cx, 530, 15, H("#5a3a20"), false, false);
        }
        else if (mm == "boss")
        {
            T("Бастар шабуылы", cx, 228, 26, H("#3a2410"));
            int kq = 0;
            for (int i = 0; i <= ci; i++)
            {
                bool dn = S.prog[i] >= 4; float yy = 248 + kq * 30; kq++;
                string ab = Db.J("BOSS_AB").At(i).Str();
                T((dn ? "● " : "○ ") + Db.Ch(i).Get("enemies").At(3).S("name") + " — " + U.Lower(Db.J("AB_INFO").Get(ab).S("name")), cx, yy + 20, 15, dn ? H("#3a2410") : H("#9a8a7a"), false, false);
            }
            if (ci == Db.NCH - 1) T(Progress.RankIndex() == Db.NCH ? "● Қара Көлеңке — құпия бас жау" : "??? — құпия бас жау", cx, 248 + kq * 30 + 20, 16, H("#8a1a12"));
            Wrap("Бас жаулар қатарынан. Әрқайсысының өз қабілеті бар, жорықтағыдан 20% күшті.", cx, 470, pw - 50, 15, H("#4a3420"));
            T("Рекорд: " + S.bossRec + " бас жау" + (S.bossTime[ci] > 0 ? " · уақыт " + Match.FmtTime(S.bossTime[ci]) : ""), cx, 532, 18, H("#5a3a20"), false, false);
        }
    }

    void Thumb(float px, float pw, string stage, string fig, bool boss)
    {
        Ui.Img("Art/campwide/thumb_" + stage, px + 20, 166, pw - 40, 210);
        Ui.Fill(px + 20, 166, pw - 40, 30, H("#e8d2a6"));                          // жоғарғы жиекті панель жабады (веб: clip 196..346)
        Ui.Fill(px + 20, 346, pw - 40, 30, H("#e8d2a6"));
        float s = boss ? 1.08f : 1;
        Ui.Img(fig, px + pw / 2 - 150 * 0.62f / 0.78f * s, 346 - 360 * 0.62f / 0.78f * s, 300 * 0.62f / 0.78f * s, 380 * 0.62f / 0.78f * s);
        Ui.Frame(px + 20, 196, pw - 40, 150, Ui.GOLD);
    }
    void Reward(float cx, int v, float y, string note)
    {
        float rw = Ui.TextW(U.Int(v), 28) + 74, rx = cx - rw / 2;
        Ui.Nine("Art/camp/pill", rx - 6, y - 34, rw + 12, 50, 20, 20, 20, 20);
        Ui.Img("Art/camp/coin", rx + 26 - 13, y - 10 - 13, 26, 26);
        Ui.TextO(U.Int(v), rx + 46, y - 27, rw - 46, 36, 28, new Color32(255, 214, 120, 255), TextAnchor.MiddleLeft);
        if (note != null) T(note, cx, y + 26, 14, H("#7a4a2a"), false, false);
    }
    void DiffBox(float px, float pw, float y, int ci, int si, bool replay)
    {
        DiffInfo I = Eco.Stage(ci, si); JNode C = Db.Ch(ci); float bx = px + 24, bw = pw - 48, cx = px + pw / 2;
        Color ic = H(I.col); Ui.Panel(bx, y, bw, 30, Color.Lerp(ic, H("#f4e6c4"), 0.82f), ic);
        string a = "Сен үшін:"; float wa = Ui.TextW(a, 15), wl = Ui.TextW(I.label, 19), tot = wa + 8 + wl + 12 + 80, x0 = cx - tot / 2;
        T(a, x0, y + 21, 15, H("#4a2c12"), true);
        T(I.label, x0 + wa + 8, y + 22, 19, ic, true);
        for (int k = 0; k < 5; k++) Star(x0 + wa + 8 + wl + 12 + 7 + k * 16, y + 15, 7, k < I.stars);
        string why = I.weak ? "Жабдығың әлсіз — " + Eco.Tip(ci, si) : I.stars <= 2 ? "Жабдығың мықты — жеңіс оңай болады!" : "Жабдығың бұл ұрысқа лайық.";
        Wrap(why, cx, y + 48, bw, 13, I.weak ? H("#9a1e10") : H("#4a3420"));
        int v = (int)(C.Get("rewards").At(si).F() * (replay ? 0.5f : 1) * I.pay);
        string note = replay ? "қайта ұрыс: жартысы" : I.pay > 1 ? "қиын ұрыс бонусы +" + Mathf.RoundToInt((I.pay - 1) * 100) + "%" : "";
        string sv = U.Int(v), lab = "Сыйақы:";
        float sw = Ui.TextW(sv, 22), nw = note.Length > 0 ? Ui.TextW(note, 13, false) + 10 : 0, lw = Ui.TextW(lab, 15);
        float rw = lw + 10 + 26 + sw + nw + 24, rx = cx - rw / 2, ry = y + 76;
        Ui.Nine("Art/camp/pill", rx - 6, ry - 7, rw + 12, 42, 18, 18, 18, 18);
        T(lab, rx + 12, ry + 19, 15, H("#ffe3a0"), true);
        Ui.Img("Art/camp/coin", rx + 12 + lw + 10, ry + 4, 20, 20);
        Ui.TextO(sv, rx + 12 + lw + 10 + 24, ry, sw + 4, 28, 22, new Color32(255, 214, 120, 255), TextAnchor.MiddleLeft);
        if (note.Length > 0) T(note, rx + 12 + lw + 10 + 26 + sw + 8, ry + 19, 13, I.pay > 1 && !replay ? H("#ffb06a") : H("#e8d6b8"), true, false);
    }

    // ---------------- негізгі жорық кезеңдері (веб: drawMapPage) ----------------
    void DrawStages()
    {
        int ci = mapCh; JNode C = Db.Ch(ci); bool open = Progress.Unlocked(ci); int prog = Progress.TestUnlockAll ? 4 : Game.S.prog[ci];
        float t = Time.unscaledTime, OX = 30, OY = 92;
        Ui.Grad3(0, 0, 1280, 720, H("#2a1810"), H("#201209"), H("#140b07"));
        float slide = Mathf.Clamp01(pt * 4), ox = OX + (1 - slide) * 60;
        Ui.Fill(ox + 8, OY + 10, 820, 560, new Color(0, 0, 0, 0.5f * slide));
        Ui.Img("Art/maps/chapter" + ci, ox, OY, 820, 560, slide);
        JNode nodes = C.Get("nodes");
        for (int i = 1; i < nodes.Count(); i++)
        {
            JNode p0 = nodes.At(i - 1), p1 = nodes.At(i);
            float x0 = ox + p0.Fk("x"), y0 = OY + p0.Fk("y"), x1 = ox + p1.Fk("x"), y1 = OY + p1.Fk("y");
            Dashed(x0, y0, (x0 + x1) / 2 + 30, (y0 + y1) / 2 - 40, x1, y1, new Color(0.47f, 0.12f, 0.08f, 0.8f), t * 20);
        }
        for (int i = 0; i < nodes.Count(); i++)
        {
            JNode n = nodes.At(i); float x = ox + n.Fk("x"), y = OY + n.Fk("y"), R = i == 3 ? 42 : 34;
            string st = !open || (i > prog && !Progress.TestUnlockAll) ? "locked" : i < prog ? "done" : "open";
            bool sel = mapStage == i && open;
            if (sel) { float rr = R + 12 + Mathf.Sin(t * 3.85f) * 3; Ui.ImgC("Art/camp/glow", x - rr * 1.3f, y - rr * 1.3f, rr * 2.6f, rr * 2.6f, new Color(1, 0.84f, 0.47f, 0.9f)); }
            Ui.Img("Art/camp/stage_" + ci + "_" + i + "_" + st, x - R - 20, y - R - 44, R * 2 + 40, R * 2 + 64);
            string nm = n.S("n"); float tw = Ui.TextW(nm, 15) + 22, ly = y + (i == 3 ? 48 : 40);
            Ui.Nine("Art/camp/plate_0", x - tw / 2 - 4, ly - 4, tw + 8, 32, 12, 12, 12, 12);
            T(nm, x, y + (i == 3 ? 65 : 57), 15, H("#3a2410"));
            // реттік нөмір мен жұлдыздар (веб: campNodeExtra)
            float bx = x - R * 0.74f, by = y - R * 0.74f;
            Ui.Medal(bx, by, 18, ""); T(U.Int(i + 1), bx, by + 5, 14, H("#2a1205"));
            if (st != "locked")
            {
                int sm = Game.S.stars[ci][i]; float yy = y + (i == 3 ? 86 : 78);
                Ui.Fill(x - 30, yy - 9, 60, 18, new Color(0.12f, 0.06f, 0.03f, 0.72f));
                for (int q = 0; q < 3; q++) Star(x - 17 + q * 17, yy, 6.5f, (sm & (1 << q)) != 0);
            }
            if (Ui.Hot(x - 44, y - 44, 88, 110)) { if (open && i <= prog) mapStage = i; else Toast(open ? "Алдымен алдыңғы ұрысты жең" : Progress.LockMsg(ci)); }
        }
        // атау жапсырмасы
        Ui.Ribbon(ox + 410, OY + 36, 440, Db.Roman(ci) + " ТАРАУ · " + U.Upper(C.S("name")), 24);
        T(Db.Years.At(ci).Str(), ox + 410, OY + 74, 14, H("#ffd36b"));
        if (!open)
        {
            Ui.Fill(ox, OY, 820, 560, new Color(0.08f, 0.05f, 0.03f, 0.62f));
            Ui.Img("Art/camp/lock", ox + 410 - 48, OY + 230, 96, 96);
            T(Progress.LockMsg(ci), ox + 410, OY + 360, 26, H("#fff3e0"));
        }
        SidePanel(ci, "story", 0);
        // төменгі жолақ
        Ui.Fill(0, 658, 860, 62, new Color(0.04f, 0.024f, 0.016f, 0.7f));
        for (int i = 0; i < Db.NCH; i++) { float dx = 430 + (i - (Db.NCH - 1) / 2f) * 28, r = i == ci ? 8 : 6; Ui.ImgC("Art/camp/glow", dx - r * 1.6f, 702 - r * 1.6f, r * 3.2f, r * 3.2f, i == ci ? Ui.GOLD : Progress.Unlocked(i) ? new Color(0.91f, 0.68f, 0.24f, 0.6f) : new Color(0.47f, 0.43f, 0.4f, 0.6f)); }
        Ui.TopBar("НЕГІЗГІ ЖОРЫҚ");
        bool can = open && mapStage <= prog;
        if (Ui.DzBtn(can ? "ҰРЫСҚА!" : "ЖАБЫҚ", 892, 590, 336, 56, true, can)) Campaign(ci, mapStage);
        if (Ui.DzBtn("ТАРИХЫН ОҚУ", 892, 538, 336, 44, false, open, null, true)) StartStory(Story.Episodes(ci), "back", ci, 0);
        if (Ui.DzBtn("◀ АЛДЫҢҒЫ", 24, 664, 150, 46, false, ci > 0, null, true)) SetPage(ci - 1);
        if (Ui.DzBtn("КЕЛЕСІ ▶", 686, 664, 150, 46, false, ci < Db.NCH - 1, null, true)) SetPage(ci + 1);
        if (Ui.DzBtn("◀ КАРТА", 24, 18, 150, 46, false, true, null, true)) { mapView = "map"; mapSite = 0; pt = 0; }
        if (Progress.ChapterDone(ci) && Ui.DzBtn("ВИКТОРИНА · " + Game.S.quizBest[ci] + "/" + Db.QZ_N, 892, 486, 336, 44, false, true, null, true)) StartQuiz(ci, false);
    }

    // ================= режимдерді бастау =================
    public void StartModeUI(string mm, int ci)
    {
        match.StartMode(mm, ci);
        mapPop = null;
        if (mm == "tour") { if (match.tourNote != null) Toast(match.tourNote, 3); Go(BRACKET); return; }
        if (mm == "boss" && match.run.list.Count() == 0) { Toast("Алдымен тараудың бас жауын жең"); return; }
        fv.Setup(match); Go(FIGHT);
    }
    int duelFoe, duelArena;
    public void GoDuelSel(int ci)
    {
        duelArena = ci; Lst<FoeCfg> foes = Match.DuelFoes();
        for (int i = 0; i < foes.Count(); i++) if (foes.Get(i).ch == ci) { duelFoe = i; break; }
        mapPop = null; Go(DUELSEL);
    }

    // ================= ТУРНИР ТОРЫ =================
    static readonly float[] BXS = { 56, 340, 624, 960 }, BBW = { 240, 240, 240, 270 };
    static float BCy(int L, int j) { return L == 0 ? 176 + j * 60 : L == 1 ? 206 + j * 120 : L == 2 ? 266 + j * 240 : 386; }
    void TourBack(int ch)
    {
        Ui.Img("Art/campwide/thumb_" + Db.Ch(ch).S("stage"), 0, 0, 1280, 720);
        Ui.Grad3(0, 0, 1280, 720, new Color(0.055f, 0.04f, 0.12f, 0.8f), new Color(0.05f, 0.03f, 0.09f, 0.87f), new Color(0.04f, 0.024f, 0.06f, 0.94f));
    }
    void DrawBracket()
    {
        Run rn = match.run; int ch = match.chapter; JNode Tt = Db.J("TOUR_TIERS").At(rn.tier), rule = Db.J("TOUR_RULES").Get(rn.rule);
        float t = Time.unscaledTime, fade = Mathf.Clamp01(pt * 2.2f); int last = rn.lv.Count() - 1;
        TourBack(ch);
        // байланыс сызықтары
        for (int L = 0; L < 3; L++)
        {
            int[] lev = L < rn.lv.Count() ? rn.lv.Get(L) : null, nx = L + 1 < rn.lv.Count() ? rn.lv.Get(L + 1) : null;
            int cnt = L == 0 ? 8 : L == 1 ? 4 : 2;
            for (int j = 0; j < cnt; j++)
            {
                float x0 = BXS[L] + BBW[L], y0 = BCy(L, j), x1 = BXS[L + 1], y1 = BCy(L + 1, j >> 1), mx = (x0 + x1) / 2;
                bool went = lev != null && nx != null && nx[j >> 1] == lev[j], me = went && rn.slots.Get(lev[j]).me;
                float al = L + 1 == last ? 0.35f + fade * 0.65f : 1;
                Color c = went ? (me ? Ui.GOLD : new Color(0.91f, 0.86f, 0.78f, 0.75f)) : new Color(0.63f, 0.55f, 0.63f, 0.25f); c.a *= al;
                float lw = went ? (me ? 4 : 2.5f) : 2;
                Ui.Fill(x0, y0 - lw / 2, mx - x0, lw, c); Ui.Fill(mx - lw / 2, Mathf.Min(y0, y1), lw, Mathf.Abs(y1 - y0), c); Ui.Fill(mx, y1 - lw / 2, x1 - mx, lw, c);
            }
        }
        for (int L = 0; L < 4; L++) { int cnt = L == 0 ? 8 : L == 1 ? 4 : L == 2 ? 2 : 1; for (int j = 0; j < cnt; j++) BracketBox(rn, L, j, last, fade, t); }
        string[] cols = { "ШИРЕК ФИНАЛ", "ЖАРТЫЛАЙ ФИНАЛ", "ФИНАЛ", "ЧЕМПИОН" };
        for (int L = 0; L < 4; L++)
        {
            bool on = L == rn.stage && !rn.outd;
            if (on) Ui.Panel(BXS[L] + BBW[L] / 2 - 90, 128, 180, 24, new Color(0.64f, 0.13f, 0.13f, 0.85f), new Color(0.96f, 0.77f, 0.36f, 0.8f));
            T(cols[L], BXS[L] + BBW[L] / 2, 145, 15, on ? H("#ffe39a") : H("#a8a0b0"));
        }
        float tx = BXS[3] + BBW[3] / 2;
        Ui.ImgC("Art/camp/glow", tx - 110, 300 - 110, 220, 220, new Color(1, 0.82f, 0.47f, rn.champ ? 0.5f : 0.16f));
        Ui.Medal(tx, 300, 42, "a32222");
        Ui.Img("Art/camp/trophy_" + rn.tier + "_" + (rn.champ ? 1 : 0), tx - 64, 304 - 70, 128, 128);
        if (rn.champ)
        {
            TO("ЧЕМПИОН!", tx, 470, 46, Ui.GOLD);
            if (rn.prize) T("Сыйлық: Бәйге бөркі", tx, 504, 18, H("#fff3e0"));
        }
        else if (rn.outd) TO("ТУРНИРДЕН ШЫҚТЫҢ", tx, 470, 26, H("#e8dccb"));
        else
        {
            TourSlot opp = rn.slots.Get(rn.lv.Get(rn.stage)[1]);
            T("Келесі қарсылас:", tx, 452, 16, H("#c9b8b0"), false, false);
            TO(opp.name, tx, 484, 26, opp.boss ? H("#ff8a6a") : H("#fff3e0"));
        }
        Ui.TopBar("ХАНДЫҚ ТУРНИРІ");
        string label = Tt.S("name") + " ТУРНИР  ·  " + Db.ChName(ch) + "  ·  Ереже: " + rule.S("name");
        float lw2 = Ui.TextW(label, 17) + 60;
        Ui.Panel(640 - lw2 / 2, 96, lw2, 30, new Color(0.055f, 0.04f, 0.12f, 0.9f), H(Tt.S("col")));
        T(label, 640, 117, 17, H("#fff3e0"));
        if (rn.champ || rn.outd)
        {
            if (Ui.DzBtn("КАРТАҒА", 940, 600, 300, 60, true)) { OpenMapHub(ch); mapSite = 2; }
            if (Ui.DzBtn("ҚАЙТА БАСТАУ", 940, 536, 300, 50, false, true, null, true)) { match.StartMode("tour", ch); pt = 0; }
        }
        else
        {
            if (Ui.DzBtn("ҰРЫСҚА!", 940, 600, 300, 60, true)) Go(VS);
            if (Ui.DzBtn("КАРТАҒА", 24, 18, 150, 46, false, true, null, true)) { OpenMapHub(ch); mapSite = 2; }
        }
    }
    void BracketBox(Run rn, int L, int j, int last, float fade, float t)
    {
        int[] lev = L < rn.lv.Count() ? rn.lv.Get(L) : null; float x = BXS[L], w = BBW[L], y = BCy(L, j) - 23;
        float ek = Mathf.Clamp01((pt - L * 0.12f - j * 0.03f) / 0.35f); ek = 1 - (1 - ek) * (1 - ek) * (1 - ek);
        float a = ek * (L == last && L > 0 ? fade : 1); x -= (1 - ek) * 30;
        if (lev == null) { Ui.Panel(x, y, w, 46, new Color(0.094f, 0.07f, 0.16f, 0.6f * a), new Color(0.63f, 0.55f, 0.63f, 0.35f * a)); T("?", x + w / 2, y + 31, 20, new Color(0.78f, 0.7f, 0.78f, 0.5f * a)); return; }
        int id = lev[j]; TourSlot sl = rn.slots.Get(id); int[] nx = L + 1 < rn.lv.Count() ? rn.lv.Get(L + 1) : null;
        bool lost = nx != null ? System.Array.IndexOf(nx, id) < 0 : (sl.me && rn.outd && L == last);
        bool cur = !rn.outd && !rn.champ && L == rn.stage && (j >> 1) == 0;
        if (lost) a *= 0.42f;
        Color top = sl.me ? H("#b8862e") : sl.boss ? H("#9a2420") : H("#2b2556"), bot = sl.me ? H("#6a3e0c") : sl.boss ? H("#4a0b0e") : H("#14112c");
        top.a = a; bot.a = a;
        Ui.Grad3(x, y, w, 46, top, Color.Lerp(top, bot, 0.5f), bot);
        Color br = cur ? new Color(1, 0.83f, 0.42f, a * (0.7f + 0.3f * Mathf.Sin(t * 5))) : sl.me ? HA("#ffe39a", a) : sl.boss ? HA("#e0533a", a) : new Color(0.91f, 0.68f, 0.24f, 0.5f * a);
        Ui.Frame(x, y, w, 46, br);
        if (cur) Ui.Frame(x - 2, y - 2, w + 4, 50, br);
        Ui.Fill(x + 8, y + 19, 8, 8, sl.me ? HA("#fff1c4", a) : sl.boss ? HA("#ffb08a", a) : new Color(0.96f, 0.77f, 0.36f, 0.7f * a));
        Ui.TextS(sl.name, x + 26, y + 4, w - 30, 24, 17, HA("#fff3e0", a), TextAnchor.MiddleLeft, true);
        Ui.TextS(sl.me ? "сен" : sl.type, x + 26, y + 26, w - 30, 18, 12, sl.me ? HA("#fff1c4", a) : HA("#c9b8b0", a), TextAnchor.MiddleLeft, false);
        if (lost) Ui.Fill(x + 10, y + 22, w - 20, 2, HA("#e0533a", a));
    }

    // ================= VS =================
    void DrawVs()
    {
        Run rn = match.run; int ch = match.chapter; JNode C = Db.Ch(ch), Tt = Db.J("TOUR_TIERS").At(rn.tier);
        float k = Mathf.Clamp01(pt / 0.55f), e = 1 - (1 - k) * (1 - k) * (1 - k), t = Time.unscaledTime;
        TourBack(ch);
        Ui.Fill(0, 0, 640, 720, new Color(0.055f, 0.1f, 0.25f, 0.6f * e));
        Color ac = H(C.S("accent")); ac.a = 0.5f * e; Ui.Fill(640, 0, 640, 720, ac);
        Ui.Fill(637, 0, 6, 720, Ui.GOLD);
        float lx = Mathf.Lerp(-220, 330, e), rx = Mathf.Lerp(1500, 950, e);
        Ui.Medal(lx, 340, 42, "2f5f9a"); Ui.Portrait("Art/portraits/" + Game.S.hero, lx, 340, 130);
        TourSlot opp = rn.slots.Get(rn.lv.Get(rn.stage)[1]);
        Ui.Medal(rx, 340, 42, "a32222"); Ui.Portrait("Art/ui/por_e_" + opp.cfg.ch + "_" + opp.cfg.idx, rx, 340, 130);
        Ui.Ribbon(lx, 528, 330, Game.Fix(Db.Hero(Game.S.hero).S("full")), 28);
        Ui.Ribbon(rx, 528, 330, opp.name, 28);
        if (opp.type != null && !opp.boss) TO(opp.type, rx, 580, 18, H("#e8dccb"));
        float pop = Mathf.Clamp01((pt - 0.45f) * 4);
        if (pop > 0)
        {
            int sz = (int)(150 * (pop < 1 ? 1.6f - pop * 0.6f : 1));
            Ui.ImgC("Art/camp/glow", 640 - 170, 330 - 170, 340, 340, new Color(1, 0.75f, 0.35f, 0.55f * pop));
            Ui.TextO("VS", 0, 330 - sz * 0.6f, 1280, sz * 1.2f, sz, new Color(1, 0.84f, 0.47f, pop), TextAnchor.MiddleCenter, true, 4);
        }
        Ui.Ribbon(640, 104, 460, Db.J("TOUR_STAGES").At(Mathf.Min(rn.stage, 2)).Str(), 40);
        TO(Tt.S("name") + " турнир · Ереже: " + Db.J("TOUR_RULES").Get(rn.rule).S("name"), 640, 166, 20, H("#ffd36b"));
        TO("Жалғастыру үшін бас", 640, 680, 16, new Color(0.91f, 0.86f, 0.8f, 0.5f + 0.5f * Mathf.Sin(t * 4)));
        if (pt > 0.6f && (Ui.Hot(0, 0, 1280, 720) || Inp.Tap(Inp.ENTER) || Inp.Tap(Inp.SPACE) || Inp.Tap(Inp.J))) { match.NextFight(); fv.Setup(match); Go(FIGHT); }
    }

    // ================= ЖЕКПЕ-ЖЕК ТАҢДАУЫ =================
    void DrawDuelSel()
    {
        Lst<FoeCfg> foes = Match.DuelFoes(); if (foes.Count() == 0) { OpenMapHub(duelArena); return; }
        duelFoe = ((duelFoe % foes.Count()) + foes.Count()) % foes.Count();
        FoeCfg fo = foes.Get(duelFoe); JNode C = Db.Ch(duelArena), E = Db.Ch(fo.ch).Get("enemies").At(fo.idx);
        float t = Time.unscaledTime;
        Ui.Img("Art/campwide/thumb_" + C.S("stage"), 0, 0, 1280, 720);
        Ui.Fill(0, 0, 1280, 720, new Color(0.047f, 0.03f, 0.094f, 0.7f));
        // 1-ойыншы
        Ui.DzPanel(40, 100, 380, 540, "felt");
        Ui.Ribbon(230, 132, 250, "1-ОЙЫНШЫ", 24);
        T("W A S D + J K L", 230, 174, 14, H("#c9b8b0"), false, false);
        Ui.ImgC("Art/camp/glow", 230 - 170, 380 - 170, 340, 340, new Color(0.47f, 0.67f, 1, 0.28f));
        Ui.Img("Art/camp/shadow", 230 - 95, 464 - 16, 190, 32);
        float br = Mathf.Sin(t * 2.4f) * 1.2f;
        Ui.Img("Art/lobby/front_" + Game.S.hero, 230 - 160, 462 - 420 + br, 320, 427);
        JNode h1 = Db.Hero(Game.S.hero);
        TO(h1.S("name"), 230, 518, 26, Ui.GOLD);
        T("Қабілеті: " + h1.S("ability"), 230, 562, 17, H("#ffd36b"));
        Wrap(h1.S("desc"), 230, 588, 330, 14, H("#e8dccb"));
        // қарсылас
        Ui.DzPanel(860, 100, 380, 540, "red");
        T(Db.ChName(fo.ch), 1050, 172, 15, H("#f0c8b0"));
        Ui.ImgC("Art/camp/glow", 1050 - 170, 380 - 170, 340, 340, new Color(1, 0.47f, 0.31f, 0.25f));
        Ui.Img("Art/camp/shadow", 1050 - 95, 464 - 16, 190, 32);
        ImgFlipX("Art/efront/efront_" + fo.ch + "_" + fo.idx, 1050 - 150 / 0.78f * 0.78f, 462 - 360, 300, 380);
        TO(E.S("name"), 1050, 518, 24, E.Bk("boss") ? Ui.GOLD : H("#fff3e0"));
        T("Қаруы: " + Db.Weapon(E.S("weapon")).S("name") + (E.Bk("boss") ? " · бас жау" : ""), 1050, 562, 16, H("#ffd36b"));
        float kq = Pz.Clamp((Game.S.rating - 1000) / 600f, 0, 1);
        T(kq > 0 ? "Рейтингіңе қарай күшейген: +" + Mathf.RoundToInt(kq * 25) + "%" : "Рейтинг өскен сайын қарсылас күшейеді", 1050, 590, 14, H("#f0dccb"), false, false);
        // алаң
        TO("АЛАҢ", 640, 142, 22, H("#ffd36b"));
        Ui.Img("Art/campwide/thumb_" + C.S("stage"), 500, 160, 280, 140);
        Ui.Frame(500, 160, 280, 140, Ui.GOLD);
        TO(C.S("name"), 640, 330, 20, H("#fff3e0"));
        // рейтинг
        Ui.DzPanel(450, 352, 380, 220, "felt");
        JNode RT = Db.J("RATING_TIERS"); int ti = Match.RatingTier(Game.S.rating); JNode Tr = RT.At(ti), nx = ti + 1 < RT.Count() ? RT.At(ti + 1) : null;
        Ui.ImgC("Art/camp/glow", 640 - 34, 410 - 34, 68, 68, H(Tr.S("col"))); Star(640, 410, 16, true);
        TO(Tr.S("name"), 640, 474, 24, Ui.GOLD);
        T("Рейтинг: " + Game.S.rating + (nx != null ? " / " + nx.Ik("min") : ""), 640, 498, 16, H("#c9b8b0"), false, false);
        if (nx != null) Ui.Bar(500, 510, 280, 10, Pz.Clamp((Game.S.rating - Tr.Ik("min")) / (float)(nx.Ik("min") - Tr.Ik("min")), 0, 1), Ui.GOLD, new Color(0, 0, 0, 0.5f));
        T("Жеңіс +25 · жеңіліс −15", 640, 548, 15, H("#a89aa8"), false, false);
        Ui.TopBar("ЖЕКПЕ-ЖЕК");
        // батырмалар
        Lst<string> hs = Game.S.heroes; int hi = Mathf.Max(0, hs.IndexOf(Game.S.hero));
        if (Ui.DzBtn("◀", 60, 486, 60, 44, false, hs.Count() > 1, null, true)) { Game.S.hero = hs.Get((hi - 1 + hs.Count()) % hs.Count()); Game.Persist(); }
        if (Ui.DzBtn("▶", 340, 486, 60, 44, false, hs.Count() > 1, null, true)) { Game.S.hero = hs.Get((hi + 1) % hs.Count()); Game.Persist(); }
        if (Ui.DzBtn("◀", 880, 486, 60, 44, false, true, null, true)) duelFoe = (duelFoe - 1 + foes.Count()) % foes.Count();
        if (Ui.DzBtn("▶", 1160, 486, 60, 44, false, true, null, true)) duelFoe = (duelFoe + 1) % foes.Count();
        Lst<int> ars = new Lst<int>(); for (int c = 0; c < Db.NCH; c++) if (Progress.Unlocked(c)) ars.Add(c);
        int ai2 = Mathf.Max(0, ars.IndexOf(duelArena));
        if (Ui.DzBtn("◀", 450, 208, 44, 44, false, ars.Count() > 1, null, true)) duelArena = ars.Get((ai2 - 1 + ars.Count()) % ars.Count());
        if (Ui.DzBtn("▶", 786, 208, 44, 44, false, ars.Count() > 1, null, true)) duelArena = ars.Get((ai2 + 1) % ars.Count());
        if (Ui.DzBtn("ЖЕКПЕ-ЖЕК!", 440, 600, 400, 64, true))
        {
            match.duelIdx = fo.idx; match.duelCh = fo.ch; match.StartMode("duel", duelArena);
            fv.Setup(match); Go(FIGHT);
        }
        if (Ui.DzBtn("КАРТАҒА", 24, 18, 150, 46, false, true, null, true)) { OpenMapHub(duelArena); mapSite = 4; }
    }
    static void ImgFlipX(string path, float x, float y, float w, float h) { Ui.ImgFlip(path, x, y, w, h); }
}
