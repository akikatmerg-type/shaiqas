// SHAIQAS · «Тірі» әлем: лоббидегі жырау мен батыр (тыныс, тербеліс, басқанда жауап), аспан мен дала тіршілігі (бұлт, құс,
// бүркіт, атты, түтін, шөп, көбелек), картадағы бұлт көлеңкелері мен жолға шыққан атты, сюжеттегі ауа бөлшектері мен беттердің
// жұмсақ ауысуы, экрандар арасындағы бәсең өтулер.
using UnityEngine;

public partial class GameRoot
{
    readonly VMesh lifeV = new VMesh();
    static float Fr(float x) { return x - Mathf.Floor(x); }
    static float Hs(int i) { return Fr(Mathf.Sin(i * 127.1f + 311.7f) * 43758.5453f); }   // тұрақты «кездейсоқ» сан

    // ================= ЛОББИ =================
    float heroHopT = -9, heroSayT = -9, zhT0 = -9; string heroSay; int zhIdx; Lst<string> zhLines; float noteT0 = -9;

    string ZhLine()
    {
        float t = Time.unscaledTime;
        if (zhLines == null || zhLines.Count() == 0) { zhLines = Zh.LobbyLines(); zhIdx = 0; zhT0 = t; }
        if (t - zhT0 > 9) ZhNext();
        return zhLines.Get(zhIdx % zhLines.Count());
    }
    void ZhNext()
    {
        float t = Time.unscaledTime; zhIdx++; zhT0 = t; noteT0 = t;
        if (zhIdx >= zhLines.Count()) { zhLines = Zh.LobbyLines(); zhIdx = 0; }
    }
    /// <summary>Лоббиге кіргенде: жырау сөздерін жаңарту.</summary>
    void LobbyEnter() { zhLines = Zh.LobbyLines(); zhIdx = 0; zhT0 = Time.unscaledTime; noteT0 = zhT0; }

    /// <summary>Артқы қабат: күн шуағы, қалқыған бұлттар, құстар, бүркіт, алыстағы атты, киіз үй түтіні.</summary>
    void LobbyBack(float t)
    {
        // күн сәулелері ақырын айналады (экран шегінен шықпайды)
        Ui.BeginClip(0, 0, 1280, 720);
        Ui.ImgRot("Art/camp/rays", 1075, 125, 820, 820, t * 3.5f, new Color(1, 0.97f, 0.86f, 0.13f + 0.05f * Mathf.Sin(t * 0.8f)));
        // жұмсақ бұлттар (баяу қозғалады)
        for (int i = 0; i < 3; i++)
        {
            float sp = 9 + i * 4, cx = Fr((t * sp + i * 520) / 1700f) * 1700 - 210, cy = 120 + i * 48 + Mathf.Sin(t * 0.3f + i) * 4, s = 1 - i * 0.18f;
            for (int q = 0; q < 5; q++)
            {
                float qx = (q - 2) * 46 * s, qy = -Mathf.Abs(q - 2) * -8 * s - (q == 2 ? 22 : q % 2 == 1 ? 12 : 0) * s, r = (q == 2 ? 70 : 52) * s;
                Ui.ImgC("Art/camp/glow", cx + qx - r, cy + qy - r * 0.62f, r * 2, r * 1.24f, new Color(1, 1, 1, 0.42f));
            }
        }
        Ui.EndClip();
        // киіз үй түтіні (алыстағы үйлерден)
        float[] sx = { 54, 163, 1074, 1202 }, sy = { 508, 518, 512, 522 };
        for (int k = 0; k < 4; k++)
            for (int j = 0; j < 4; j++)
            {
                float ph = Fr(t * 0.18f + j / 4f + k * 0.37f), r = 6 + ph * 16;
                Ui.ImgC("Art/camp/glow", sx[k] + ph * 22 + Mathf.Sin(ph * 6 + k) * 4 - r, sy[k] - ph * 120 - r, r * 2, r * 2, new Color(0.92f, 0.92f, 0.95f, 0.38f * (1 - ph)));
            }
        VMesh v = lifeV; v.Clear();
        // бүркіт аспанда шеңбер сызады
        float ea = t * 0.22f, exx = 860 + Mathf.Cos(ea) * 170, eyy = 170 + Mathf.Sin(ea) * 46;
        StageLive.Eagle(v, exx, eyy, 0.32f, Mathf.Sin(t * 2.1f) * 0.35f, Mathf.Sin(ea) * 0.12f, Col.Pack(40, 30, 32, 230));
        // құстар тобы өтіп кетеді (әр 26 с)
        float bp = Fr(t / 26f);
        if (bp < 0.55f)
        {
            float bx0 = -60 + bp / 0.55f * 1400, by0 = 210 + Mathf.Sin(bp * 9) * 10;
            v.FillC(Col.Pack(50, 40, 48, 200));
            for (int i = 0; i < 6; i++)
            {
                float bx = bx0 - (i % 3) * 26 - i * 9, by = by0 + (i % 2) * 14 + i * 4, fl = Mathf.Sin(t * 9 + i * 1.7f) * 4;
                float[] xs = { bx - 8, bx, bx + 8 }, ys = { by - fl, by + 2, by - fl };
                v.Stroke(xs, ys, 3, 1.8f, true);
            }
        }
        // алыстағы атты дала бойымен шабады (әр 38 с)
        float rp = Fr((t + 12) / 38f);
        if (rp < 0.6f) StageLive.Rider(v, -80 + rp / 0.6f * 1440, 566, 0.2f, 1, t * 11, "kalpak", true, false);
        Ui.Mesh(v);
    }

    /// <summary>Алдыңғы қабат: желмен тербелген шөп, көбелектер, гүл тозаңы.</summary>
    void LobbyFront(float t)
    {
        VMesh v = lifeV; v.Clear();
        float[] xs = new float[3], ys = new float[3];
        for (int i = 0; i < 70; i++)
        {
            float x = i * 18.5f + Hs(i) * 14, h = 18 + Hs(i + 99) * 26, w = Mathf.Sin(t * 1.7f + x * 0.013f) * (5 + h * 0.2f) + Mathf.Sin(t * 3.1f + i) * 1.5f;
            int g = (int)(90 + Hs(i + 7) * 60);
            v.FillC(Col.Pack(60 + (int)(Hs(i + 3) * 40), g + 40, 40, 235));
            xs[0] = x; ys[0] = 722; xs[1] = x + w * 0.4f; ys[1] = 722 - h * 0.55f; xs[2] = x + w; ys[2] = 722 - h;
            v.Stroke(xs, ys, 3, 2.4f, true);
        }
        // көбелектер
        for (int b = 0; b < 2; b++)
        {
            float bt = t * (0.33f + b * 0.07f) + b * 3, bx = 360 + Fr(bt * 0.08f + b * 0.5f) * 880, by = 600 + Mathf.Sin(bt * 1.7f) * 40 + Mathf.Sin(bt * 4.3f) * 8;
            float fl = Mathf.Abs(Mathf.Sin(t * 16 + b)); int wcol = b == 0 ? Col.Pack(255, 210, 80, 240) : Col.Pack(255, 140, 170, 240);
            v.FillC(wcol); v.Ellipse(bx - 5 * fl, by - 3, 6 * fl + 1, 5, -0.4f); v.Ellipse(bx + 5 * fl, by - 3, 6 * fl + 1, 5, 0.4f);
            v.FillC(Col.Pack(60, 40, 30, 255)); v.Ellipse(bx, by - 2, 1.4f, 4, 0);
        }
        Ui.Mesh(v);
        // ауадағы тозаң жылтырлары
        for (int i = 0; i < 22; i++)
        {
            float ph = Fr(t * (0.03f + Hs(i) * 0.03f) + Hs(i + 50)), x = 80 + Hs(i + 20) * 1120 + Mathf.Sin(t * 0.7f + i) * 20, y = 700 - ph * 420;
            float a = Mathf.Sin(ph * Mathf.PI) * (0.35f + 0.25f * Mathf.Sin(t * 3 + i)), r = 3 + Hs(i + 9) * 3;
            Ui.ImgC("Art/camp/glow", x - r, y - r, r * 2, r * 2, new Color(1, 0.96f, 0.78f, Mathf.Max(0, a)));
        }
    }

    /// <summary>Батыр: тыныс, салмақты ауыстыру, ара-тұра мақтанышпен кеуде керу; басса — секіріп, сөз айтады.</summary>
    void LobbyHero(float t)
    {
        float hop = Mathf.Clamp01((t - heroHopT) / 0.55f), hopY = hop < 1 ? -46 * Mathf.Sin(hop * Mathf.PI) : 0;
        float land = t - heroHopT - 0.55f, squash = land > 0 && land < 0.22f ? Mathf.Sin(land / 0.22f * Mathf.PI) * 0.05f : 0;
        float g = Fr(t / 11f), proud = g > 0.82f ? Mathf.Sin((g - 0.82f) / 0.18f * Mathf.PI) : 0;
        float br = Mathf.Sin(t * 2.2f);
        float sy = 1 + 0.011f * br + 0.02f * proud - squash, sx = 1 - 0.004f * br + squash * 0.8f;
        float deg = 0.6f * Mathf.Sin(t * 0.9f) - 1.6f * proud;
        Ui.ImgPose("Art/lobby/front_" + Game.S.hero, 550, 110 + hopY, 420, 560, 760, 660 + hopY, deg, sx, sy);
        if (Ui.GB(Ui.R(640, 170, 240, 480)) && t - heroHopT > 0.7f)
        {
            heroHopT = t; heroSayT = t; Snd.Play("jump", 0.7f);
            heroSay = Game.Fix(Db.Replies.At((int)(U.Rand() * Db.Replies.Count())).Str());
        }
        float sk = t - heroSayT;
        if (sk < 2.8f && heroSay != null)
        {
            float a = Mathf.Clamp01(Mathf.Min(sk * 6, (2.8f - sk) * 3)), pop = 1 + 0.15f * Mathf.Max(0, 1 - sk * 6);
            float tw = Mathf.Min(320, Ui.TextW(heroSay, 18)) + 34, bx = 760 - tw / 2, by = 118 - 6 * pop + hopY * 0.5f;
            Color o = GUI.color; GUI.color = new Color(1, 1, 1, a);
            Ui.Fill(bx + 3, by + 4, tw, 44, new Color(0, 0, 0, 0.25f));
            Ui.Fill(bx, by, tw, 44, new Color32(30, 58, 110, 255)); Ui.Fill(bx + 2.5f, by + 2.5f, tw - 5, 39, new Color32(250, 244, 230, 255));
            for (int i = 0; i < 12; i++) { float w = 16 * (1 - i / 12f); Ui.Fill(760 - w / 2, by + 42 + i, w, 1.2f, new Color32(250, 244, 230, 255)); }
            Ui.Text(heroSay, bx, by, tw, 44, Ui.Fit(heroSay, tw - 20, 18), new Color32(30, 40, 80, 255), TextAnchor.MiddleCenter, true);
            GUI.color = o;
        }
    }

    /// <summary>Жырау: баяу тербеліп, домбыраның әуені (♪) ұшады; басса — келесі кеңесін айтады.</summary>
    void LobbyZhyrau(float t)
    {
        float br = Mathf.Sin(t * 1.6f);
        Ui.ImgPose("Art/lobby/zhyrau", 980, 250, 300, 420, 1130, 665, 0.8f * Mathf.Sin(t * 0.7f), 1 - 0.003f * br, 1 + 0.009f * br);
        if (Ui.GB(Ui.R(1040, 300, 220, 360))) { ZhNext(); Snd.Play("ui_move", 0.6f); }
        float nk = t - noteT0;
        if (nk < 2.6f)
            for (int i = 0; i < 3; i++)
            {
                float k = Mathf.Clamp01((nk - i * 0.25f) / 2f); if (k <= 0 || k >= 1) continue;
                float x = 1180 + i * 12 + Mathf.Sin(k * 8 + i) * 10, y = 430 - k * 120;
                Ui.TextO(i % 2 == 0 ? "♪" : "♫", x - 15, y - 15, 30, 30, 22, new Color(1, 0.88f, 0.5f, Mathf.Sin(k * Mathf.PI)), TextAnchor.MiddleCenter, true, 1.5f);
            }
    }

    // ================= КАРТА =================
    void MapLife(float t, int ci, int selI)
    {
        // бұлт көлеңкелері карта үстімен жылжиды
        Ui.BeginClip(24, 92, 1232, 560);
        for (int i = 0; i < 3; i++)
        {
            float x = Fr((t * (12 + i * 5) + i * 600) / 1700f) * 1700 - 260, y = 120 + i * 170 + Mathf.Sin(t * 0.2f + i) * 20, w = 320 - i * 40;
            Ui.ImgC("Art/camp/glow", x, y, w, w * 0.55f, new Color(0.08f, 0.05f, 0.03f, 0.22f));
        }
        Ui.EndClip();
        VMesh v = lifeV; v.Clear();
        // таңдалған алаңға қарай жолмен атты жүреді
        int dst = selI > 0 ? selI : 1;
        float ph = Fr(t / 7f), sx = SITE_X[dst], sy = SITE_Y[dst], mx = (640 + sx) / 2, my = (296 + sy) / 2 + (sy < 296 ? -30 : 30) * (sx == 640 ? 0 : 1);
        float u = 0.32f + 0.68f * Pz.Smooth01(ph), a1 = (1 - u) * (1 - u), b1 = 2 * (1 - u) * u, c1 = u * u;
        float rx = a1 * 640 + b1 * mx + c1 * sx, ry = a1 * 296 + b1 * my + c1 * sy;
        float dirx = sx >= 640 ? 1 : -1;
        if (ph > 0.06f && ph < 0.94f) StageLive.Rider(v, rx, ry + 8, 0.16f, dirx, t * 10, "kalpak", false, true);
        // қалықтаған бүркіт
        float ea = t * 0.3f;
        StageLive.Eagle(v, 640 + Mathf.Cos(ea) * 380, 170 + Mathf.Sin(ea * 1.3f) * 30, 0.28f, Mathf.Sin(t * 2.4f) * 0.35f, Mathf.Sin(ea) * 0.15f, Col.Pack(40, 26, 18, 200));
        Ui.Mesh(v);
    }

    // ================= СЮЖЕТ =================
    string storyPrevPic; int storyLastPage = -1; float storyFadeT0 = -9; string storyCurPic;
    /// <summary>Бет ауысқанда алдыңғы сурет жұмсақ сөнеді (кесілмейді).</summary>
    void StoryPicFade(string pic)
    {
        if (page != storyLastPage)
        {
            if (storyLastPage >= 0 && storyCurPic != null && storyCurPic != pic) { storyPrevPic = storyCurPic; storyFadeT0 = Time.unscaledTime; }
            storyLastPage = page; storyCurPic = pic;
        }
    }
    float StoryPrevA() { return 1 - Mathf.Clamp01((Time.unscaledTime - storyFadeT0) / 0.6f); }
    /// <summary>Сурет үстіндегі ауа: тозаң, қар, ұшқын не құм — тараудың өлкесіне қарай; жиегі жұмсақ күңгірт.</summary>
    void StoryAir(int ch)
    {
        float t = Time.unscaledTime;
        string st = Db.Ch(Mathf.Clamp(ch, 0, Db.NCH - 1)).S("stage");
        int k = PostFx.Idx(st); string kind = k == 3 ? "snow" : k == 4 ? "ember" : k >= 5 ? "sand" : "dust";
        for (int i = 0; i < 34; i++)
        {
            float sp = 0.04f + Hs(i) * 0.05f, ph = Fr(t * sp + Hs(i + 40)), x, y, r, a; Color c;
            if (kind == "snow") { x = Fr(Hs(i + 11) + t * 0.01f) * 1280 + Mathf.Sin(t + i) * 18; y = 84 + ph * 450; r = 2 + Hs(i + 5) * 3; a = 0.7f; c = Color.white; }
            else if (kind == "ember") { x = Hs(i + 11) * 1280 + Mathf.Sin(t * 1.3f + i) * 26; y = 530 - ph * 440; r = 2 + Hs(i + 5) * 2.5f; a = (0.5f + 0.5f * Mathf.Sin(t * 6 + i)) * Mathf.Sin(ph * Mathf.PI); c = new Color(1, 0.6f, 0.25f); }
            else if (kind == "sand") { x = Fr(Hs(i + 11) + t * 0.05f) * 1280; y = 300 + Hs(i + 3) * 230 + Mathf.Sin(t * 2 + i) * 6; r = 1.6f + Hs(i + 5) * 2; a = 0.45f; c = new Color(1, 0.86f, 0.6f); }
            else { x = Hs(i + 11) * 1280 + Mathf.Sin(t * 0.5f + i) * 30; y = 520 - ph * 420; r = 2 + Hs(i + 5) * 2.5f; a = 0.5f * Mathf.Sin(ph * Mathf.PI); c = new Color(1, 0.95f, 0.78f); }
            Ui.ImgC("Art/camp/glow", x - r * 2, y - r * 2, r * 4, r * 4, new Color(c.r, c.g, c.b, Mathf.Max(0, a)));
        }
        if (kind == "ember") Ui.FadeH(0, 84, 260, 450, new Color(0.4f, 0.1f, 0.02f, 0.12f + 0.06f * Mathf.Sin(t * 7)));
    }

    // ================= ЭКРАН АУЫСУЫ =================
    float goT = -9;
    void TransitionFade()
    {
        float k = (Time.unscaledTime - goT) / 0.3f;
        if (k < 1) Ui.Fill(0, 0, 1280, 720, new Color(0.02f, 0.01f, 0.03f, 1 - k * k));
    }
}
