// SHAIQAS Core · Әдістердің суреті (VMesh): тас найзалар (Жер қозғалысы), жер астындағы толқын,
// мұз найза (Аяз), мұзға қатқан жау, қырау. Core-да тұр — Unity де, призрак та (Java) бірдей салады.
// v — қалыпты араластыру, va — жарқын (additive) қабат.
using UnityEngine;

public static class SkArt
{
    static float[] px = new float[32], py = new float[32];

    // тұрақты «кездейсоқ» сан 0..1 (бір тас — бір пішін, кадр сайын өзгермейді)
    static float H(float a, float b) { float s = Mathf.Sin(a * 12.9898f + b * 78.233f) * 43758.547f; return s - Mathf.Floor(s); }
    static int C(int r, int g, int b) { return Col.Pack(r, g, b, 255); }
    static int CA(int r, int g, int b, float a) { return Col.Pack(r, g, b, Mathf.RoundToInt(Mathf.Clamp01(a) * 255)); }
    static void P(int i, float x, float y) { px[i] = x; py[i] = y; }
    static void Tri(VMesh v, float x0, float y0, float x1, float y1, float x2, float y2) { P(0, x0, y0); P(1, x1, y1); P(2, x2, y2); v.Poly(px, py, 3); }
    static void Seg(VMesh v, float x0, float y0, float x1, float y1, float w) { P(0, x0, y0); P(1, x1, y1); v.Thin(px, py, 2, w); }
    static void Diamond(VMesh v, float x, float y, float r) { P(0, x, y - r); P(1, x + r * 0.55f, y); P(2, x, y + r); P(3, x - r * 0.55f, y); v.Poly(px, py, 4); }
    static float EaseBack(float t) { t = Mathf.Clamp01(t); float c = 1.9f; return 1 + (c + 1) * Mathf.Pow(t - 1, 3) + c * Mathf.Pow(t - 1, 2); }

    // ================= ЖЕР ҚОЗҒАЛЫСЫ =================
    /// <summary>Тас найзалар: [x, h, w, lean, life, t]. Жарықшақ, жыныс сынықтары, екі қырлы жартас, шаң.</summary>
    public static void Spikes(VMesh v, VMesh va, Match m)
    {
        float G = Db.GROUND;
        for (int i = 0; i < m.spikes.Count(); i++)
        {
            float[] s = m.spikes.Get(i);
            float k = s[5] / s[4], seed = s[0] * 0.37f;
            // шығу: серпіліп атылып шығады (асып кетіп, қайта отырады), соңында жерге батады
            float up = EaseBack(k / 0.16f), down = k > 0.72f ? 1 - Pz.Smooth01((k - 0.72f) / 0.28f) : 1;
            float h = s[1] * 1.6f * up * down, w = s[2] * 2.1f, fade = k > 0.85f ? 1 - (k - 0.85f) / 0.15f : 1;
            // жердегі жарықшақ (ішінен жер оты жылтырайды)
            float cw = w * (1.4f + 0.6f * Mathf.Min(1, k * 6));
            int n = 7;
            for (int j = 0; j < n; j++) { float u = j / (float)(n - 1); P(j, s[0] - cw + 2 * cw * u, G + 3 + (H(seed, j) - 0.5f) * 5 * Mathf.Sin(u * Mathf.PI)); }
            va.FillC(CA(255, 170, 80, 0.45f * fade * (1 - k * 0.6f))); va.Thin(px, py, n, 6);
            v.FillC(CA(34, 22, 14, 0.85f * fade)); v.Thin(px, py, n, 2.6f);
            if (h < 2) continue;
            // сынықтар
            for (int j = 0; j < 3; j++)
            {
                float rx = s[0] + (j - 1) * w * 0.75f + (H(seed, j + 9) - 0.5f) * 10, rs = (5 + H(seed, j + 3) * 6) * Mathf.Min(1, up), ry = G + 2;
                v.FillC(j == 1 ? C(132, 98, 66) : C(98, 72, 50));
                P(0, rx - rs, ry); P(1, rx - rs * 0.6f, ry - rs * 0.9f); P(2, rx + rs * 0.3f, ry - rs * 1.1f); P(3, rx + rs, ry - rs * 0.3f); P(4, rx + rs * 0.8f, ry); v.Poly(px, py, 5);
            }
            // қасындағы кіші жартастар (екі жағына еңкейген)
            for (int j = 0; j < 2; j++)
            {
                float sd = j == 0 ? -1 : 1, sx = s[0] + sd * w * (0.62f + 0.2f * H(seed, j + 40)), sh = h * (0.38f + 0.22f * H(seed, j + 41)), sw = w * 0.55f;
                Rock(v, sx, G + 4, s[3] + sd * (0.3f + 0.2f * H(seed, j + 42)), sw, sh, seed + j * 7);
            }
            // жартас: екі қыры (жарық және көлеңке), жиегі кедір-бұдыр
            v.Save(); v.Translate(s[0], G + 4); v.Rotate(s[3]);
            float j1 = (H(seed, 1) - 0.5f) * 0.16f, j2 = (H(seed, 2) - 0.5f) * 0.16f, tipx = w * (0.04f + j1);
            // жарық қыры (сол)
            P(0, -w / 2, 0); P(1, -w * (0.44f + j2), -h * 0.34f); P(2, -w * 0.3f, -h * (0.66f + j1)); P(3, tipx, -h); P(4, w * 0.06f, -h * 0.5f); P(5, w * 0.02f, 0);
            v.FillC(C(168, 124, 82)); v.Poly(px, py, 6);
            v.QuadC(-w * 0.3f, -h * 0.66f, tipx, -h, w * 0.06f, -h * 0.5f, -w * 0.44f, -h * 0.34f, C(214, 172, 118), C(236, 204, 150), C(184, 140, 94), C(168, 124, 82));
            // көлеңке қыры (оң)
            P(0, w * 0.02f, 0); P(1, w * 0.06f, -h * 0.5f); P(2, tipx, -h); P(3, w * (0.32f + j1), -h * 0.6f); P(4, w * (0.5f + j2), -h * 0.26f); P(5, w / 2, 0);
            v.FillC(C(92, 64, 42)); v.Poly(px, py, 6);
            // қыр сызығы мен жарықшақтар
            v.FillC(CA(255, 232, 190, 0.55f)); Seg(v, tipx, -h, w * 0.05f, -h * 0.45f, 1.6f);
            v.FillC(CA(40, 26, 16, 0.7f)); Seg(v, -w * 0.2f, -h * 0.22f, -w * 0.05f, -h * 0.4f, 1.4f); Seg(v, w * 0.22f, -h * 0.55f, w * 0.3f, -h * 0.38f, 1.2f);
            v.Restore();
            // шығарда шаң бұлты
            if (k < 0.3f) { float a = (0.3f - k) / 0.3f; v.FillC(CA(190, 160, 120, 0.35f * a)); v.Ellipse(s[0], G - 6, w * (1.2f + k * 4), 12 + k * 30, 0); }
        }
    }

    /// <summary>Кіші жартас (екі қырлы).</summary>
    static void Rock(VMesh v, float x, float y, float rot, float w, float h, float seed)
    {
        if (h < 2) return;
        v.Save(); v.Translate(x, y); v.Rotate(rot);
        float tx = w * (H(seed, 5) - 0.5f) * 0.3f;
        P(0, -w / 2, 0); P(1, -w * 0.36f, -h * 0.5f); P(2, tx, -h); P(3, w * 0.04f, 0); v.FillC(C(178, 134, 90)); v.Poly(px, py, 4);
        P(0, w * 0.04f, 0); P(1, tx, -h); P(2, w * 0.38f, -h * 0.45f); P(3, w / 2, 0); v.FillC(C(96, 68, 44)); v.Poly(px, py, 4);
        v.FillC(CA(255, 232, 190, 0.45f)); Seg(v, tx, -h, w * 0.03f, -h * 0.4f, 1.2f);
        v.Restore();
    }

    /// <summary>Жер астымен жүгірген толқын (снаряд): топырақ дөңесі, секірген тастар, жарқылдаған жарықшақ.</summary>
    public static void EarthWave(VMesh v, VMesh va, SkProj p, float T)
    {
        float G = Db.GROUND, dir = p.vx >= 0 ? 1 : -1, k = Mathf.Clamp01(p.life * 4) * Mathf.Clamp01(p.t * 8);
        va.FillC(CA(255, 170, 80, 0.4f * k)); va.Ellipse(p.x, G + 2, 46, 6, 0);
        v.FillC(CA(110, 80, 52, k));
        int n = 9;
        for (int j = 0; j < n; j++) { float u = j / (float)(n - 1), a = u * Mathf.PI; P(j, p.x - dir * 10 + Mathf.Cos(a) * 44, G + 2 - Mathf.Sin(a) * (14 + 4 * Mathf.Sin(T * 30))); }
        v.Poly(px, py, n);
        for (int j = 0; j < 4; j++)
        {
            float ph = T * 9 + j * 1.7f, hop = Mathf.Abs(Mathf.Sin(ph)) * (16 + j * 5), rx = p.x + (j - 1.5f) * 16 - dir * 8, rs = 4 + j % 2 * 2;
            v.FillC(j % 2 == 0 ? CA(132, 98, 66, k) : CA(86, 62, 42, k));
            v.Save(); v.Translate(rx, G - 4 - hop); v.Rotate(ph * 2); P(0, -rs, -rs * 0.6f); P(1, rs * 0.4f, -rs); P(2, rs, rs * 0.3f); P(3, -rs * 0.3f, rs); v.Poly(px, py, 4); v.Restore();
        }
    }

    // ================= АЯЗ =================
    /// <summary>Мұз найза: кристалды қырлы найза, айналған серік-сынықтар, жылтыр із, суық тұман.</summary>
    public static void IceLance(VMesh v, VMesh va, SkProj p, float T)
    {
        float k = Mathf.Clamp01(p.life * 4) * Mathf.Clamp01(p.t * 8), dir = p.vx >= 0 ? 1 : -1;
        // суық жарқыл
        va.Save(); va.Translate(p.x - dir * 10, p.y); va.Scale(1.6f, 0.7f); va.Glow(0, 0, 60, CA(150, 225, 255, 0.55f * k)); va.Restore();
        // жылтыр із: артта қалған қырау ұшқындары
        for (int j = 0; j < 12; j++)
        {
            float q = j * 13 + (p.t * 700) % 13, sx = p.x - dir * (34 + q * 1.15f), sy = p.y + Mathf.Sin(j * 2.3f + p.t * 22) * (3 + j * 1.4f), a = (1 - j / 12f) * k;
            va.FillC(j % 3 == 0 ? CA(255, 255, 255, a) : CA(170, 235, 255, a * 0.8f)); Diamond(va, sx, sy, 2 + (1 - j / 12f) * 4.5f);
        }
        va.FillC(CA(210, 245, 255, 0.35f * k));
        for (int j = -1; j <= 1; j++) Seg(va, p.x - dir * 26, p.y + j * 7, p.x - dir * (110 + (1 - Mathf.Abs(j)) * 40), p.y + j * 9, 2.4f - Mathf.Abs(j));
        // серік-сынықтар (айналып ұшады; артындағысы — найзаның артында)
        for (int pass = 0; pass < 2; pass++)
        {
            if (pass == 1) Lance(v, va, p.x, p.y, dir, 1, k, Mathf.Sin(T * 30) * 0.04f);
            for (int j = 0; j < 2; j++)
            {
                float ph = p.t * 16 + j * Mathf.PI, front = Mathf.Cos(ph);
                if ((front > 0) != (pass == 1)) continue;
                Lance(v, va, p.x - dir * (22 + j * 12), p.y + Mathf.Sin(ph) * 16, dir, 0.42f, k * (0.7f + 0.3f * front), Mathf.Sin(ph) * 0.2f);
            }
        }
        // ұштағы жұлдызша
        va.FillC(CA(255, 255, 255, k * (0.7f + 0.3f * Mathf.Sin(T * 40)))); va.Star(p.x + dir * 50, p.y, 7, T * 6);
    }
    /// <summary>Бір кристалл найза (жергілікті жүйеде, s — өлшемі).</summary>
    static void Lance(VMesh v, VMesh va, float x, float y, float dir, float s, float a, float rot)
    {
        v.Save(); v.Translate(x, y); v.Scale(dir * s, s); v.Rotate(rot);
        // жоғарғы қыр — жарық, төменгісі — қою көк, ортасы — ақ қыр сызығы
        P(0, 50, 0); P(1, 20, -11); P(2, -26, -9); P(3, -42, 0); v.FillC(CA(226, 248, 255, a)); v.Poly(px, py, 4);
        P(0, 50, 0); P(1, -42, 0); P(2, -26, 9); P(3, 20, 11); v.FillC(CA(108, 186, 232, a)); v.Poly(px, py, 4);
        P(0, 50, 0); P(1, 20, -11); P(2, 8, -3); v.FillC(CA(255, 255, 255, a)); v.Poly(px, py, 3);
        P(0, 20, 11); P(1, -26, 9); P(2, -6, 4); v.FillC(CA(70, 140, 200, a)); v.Poly(px, py, 3);
        v.FillC(CA(255, 255, 255, a * 0.9f)); Seg(v, -38, 0, 46, 0, 1.4f);
        v.FillC(CA(190, 236, 255, a * 0.8f)); Seg(v, -20, -5, 16, -7, 1);
        v.Restore();
    }

    /// <summary>Мұзға қатқан жауынгер: кристалдар өсіп шығады, ішінен жылтырайды, соңында жарылып сынады.</summary>
    public static void IceBlock(VMesh v, VMesh va, Fighter f, float T)
    {
        SkState S = f.sks; if (S == null || S.ice <= 0 || f.state == Fighter.DEAD) return;
        float sc = f.sc / Db.S, grow = EaseBack(S.iceAge / 0.2f), a = Mathf.Clamp01(S.ice * 5);
        float w = 70 * sc, h = 282 * sc * grow, bx = f.x, by = f.y + 4, seed = Mathf.Floor(f.x * 0.1f);
        // негіз — жерде қырау дағы
        v.FillC(CA(220, 245, 255, 0.55f * a)); v.Ellipse(bx, Db.GROUND + 3, w * 1.9f, 12, 0);
        // негізгі блок (мөлдір мұз), қырлары әртүрлі
        P(0, bx - w * 1.05f, by); P(1, bx - w * 1.12f, by - h * 0.52f); P(2, bx - w * 0.72f, by - h * 0.96f); P(3, bx + w * 0.08f, by - h * 1.04f);
        P(4, bx + w * 0.86f, by - h * 0.86f); P(5, bx + w * 1.08f, by - h * 0.4f); P(6, bx + w * 0.96f, by);
        v.FillC(CA(170, 226, 255, 0.4f * a)); v.Poly(px, py, 7);
        float[] ex = new float[8], ey = new float[8]; for (int i = 0; i < 7; i++) { ex[i] = px[i]; ey[i] = py[i]; } ex[7] = px[0]; ey[7] = py[0];
        // ішкі қырлар (жарық панельдер)
        v.FillC(CA(235, 250, 255, 0.28f * a)); Tri(v, ex[1], ey[1], ex[2], ey[2], bx - w * 0.1f, by - h * 0.5f); Tri(v, ex[3], ey[3], ex[4], ey[4], bx + w * 0.2f, by - h * 0.62f);
        v.FillC(CA(90, 160, 220, 0.22f * a)); Tri(v, ex[5], ey[5], ex[6], ey[6], bx + w * 0.3f, by - h * 0.3f);
        // жиегі
        v.FillC(CA(255, 255, 255, 0.85f * a)); v.Thin(ex, ey, 8, 2.4f);
        // жарықшақтар (уақыт өте көбейеді)
        float crack = Mathf.Clamp01(1 - S.ice / 0.6f);
        v.FillC(CA(255, 255, 255, 0.7f * a * crack));
        Seg(v, bx - w * 0.5f, by - h * 0.78f, bx - w * 0.1f, by - h * 0.56f, 1.6f); Seg(v, bx - w * 0.1f, by - h * 0.56f, bx + w * 0.3f, by - h * 0.6f, 1.2f);
        Seg(v, bx + w * 0.55f, by - h * 0.28f, bx + w * 0.15f, by - h * 0.12f, 1.4f);
        // кристалл тікенектер: етегінде және төбесінде, сыртқа еңкейіп өседі
        for (int i = 0; i < 6; i++)
        {
            bool top = i >= 4; float side = i % 2 == 0 ? -1 : 1, hh = (top ? 46 : 58 + H(seed, i) * 40) * sc * grow, ww = (12 + H(seed, i + 7) * 8) * sc;
            float sx = top ? bx + side * w * (0.3f + 0.2f * H(seed, i + 2)) : bx + side * w * (0.75f + 0.45f * (i / 2)), sy = top ? by - h * 0.95f : by;
            float lean = side * (0.25f + 0.25f * H(seed, i + 4)) + (top ? 0 : 0);
            v.Save(); v.Translate(sx, sy); v.Rotate(lean);
            P(0, -ww / 2, 0); P(1, 0, -hh); P(2, ww / 2, 0); v.FillC(CA(150, 214, 248, 0.85f * a)); v.Poly(px, py, 3);
            P(0, -ww / 2, 0); P(1, 0, -hh); P(2, 0, 0); v.FillC(CA(240, 252, 255, 0.9f * a)); v.Poly(px, py, 3);
            v.Restore();
        }
        // ішкі жылтыр жарқыл және жұлдызшалар
        va.Save(); va.Translate(bx, by - h * 0.5f); va.Scale(0.7f, 1.2f); va.Glow(0, 0, w * 1.4f, CA(150, 220, 255, 0.4f * a)); va.Restore();
        for (int i = 0; i < 4; i++)
        {
            float tw = Mathf.Max(0, Mathf.Sin(T * 5 + i * 1.9f)), sxx = bx + (H(seed, i + 20) - 0.5f) * w * 1.6f, syy = by - h * (0.15f + 0.8f * H(seed, i + 30));
            va.FillC(CA(255, 255, 255, tw * a)); va.Star(sxx, syy, 5 + 5 * tw, T * 2 + i);
        }
    }

    /// <summary>Ақ боран әсері: аяғының астындағы қырау және аяқ-қолына қонған мұз.</summary>
    public static void Frost(VMesh v, VMesh va, Fighter f, float T)
    {
        SkState S = f.sks; if (S == null || S.frost <= 0) return;
        float a = Mathf.Clamp01(S.frost), sc = f.sc / Db.S;
        v.FillC(CA(223, 246, 255, a * 0.5f)); v.Ellipse(f.x, Db.GROUND + 2, 86, 12, 0);
        v.FillC(CA(255, 255, 255, a * 0.75f));
        for (int i = 0; i < 7; i++) { float u = i / 6f, x = f.x - 80 + 160 * u, hh = (8 + 10 * H(i, 3)) * a; Tri(v, x - 4, Db.GROUND + 3, x + (H(i, 5) - 0.5f) * 6, Db.GROUND + 3 - hh, x + 4, Db.GROUND + 3); }
        for (int i = 0; i < 3; i++) { float tw = Mathf.Max(0, Mathf.Sin(T * 4 + i * 2.1f)); va.FillC(CA(220, 248, 255, tw * a)); va.Star(f.x + (i - 1) * 40 * sc, f.y - (60 + i * 70) * sc, 4 + 4 * tw, T + i); }
    }

    // ================= ӘР ӘДІСТІҢ ӨЗ БЕЛГІСІ (әдіс орындалып жатқанда) =================
    static readonly Geo GG = new Geo();
    static float wx, wy;
    // дене нүктесі → әлем (жауынгер тік тұрғанда)
    static void W(Fighter f, float lx, float ly) { wx = f.x + f.facing * lx * f.sc; wy = f.y + ly * f.sc; }
    static float Wa(Fighter f, float a) { return f.facing > 0 ? a : Mathf.PI - a; }   // бағытты айналдыру (0 — алға)

    /// <summary>Артқы қабат: қанаттар, жарық бағанасы, жалын бағанасы, аруақ тұманы, сарғыш аура.</summary>
    public static void AuraBack(VMesh v, VMesh va, Fighter f, float T)
    {
        SkState S = f.sks;
        if (S != null && S.hot > 0) HealPillar(v, va, f, T, Mathf.Clamp01(S.hot / 3f));
        SkRun s = f.sk; if (s == null || f.cur == null) return;
        string id = s.def.id; float t = s.t, sc = f.sc;
        GG.Build(f.cur);
        if (U.Eq(id, "burkit")) EagleWings(v, va, f, t, T);
        else if (U.Eq(id, "kokzhal")) SpiritTrail(va, f, t, T, 200, 168, 255);
        else if (U.Eq(id, "tulpar")) Afterimages(v, va, f, t, T);
        else if (U.Eq(id, "jalyn")) FlameColumn(v, va, f, t, T, false);
        else if (U.Eq(id, "aruaq") || U.Eq(id, "aiqai")) Wisps(va, f, t, T, 200, 168, 255);
        else if (U.Eq(id, "alpamys")) { va.Save(); W(f, 0, -150); va.Translate(wx, wy); va.Scale(0.8f, 1.3f); va.Glow(0, 0, 220 * sc / Db.S, CA(255, 214, 107, 0.5f)); va.Restore(); Wisps(va, f, t, T, 255, 214, 107); }
        else if (U.Eq(id, "naizagai") && !s.struck) StormCloud(v, va, s.tx, t, T);
        else if (U.Eq(id, "boran")) SnowSwirl(v, va, f, t, T);
    }

    /// <summary>Алдыңғы қабат: тырнақ іздері, қалқан, жалынды қылыш, айқай толқыны, құйын шеңбері, алтын жүз.</summary>
    public static void AuraFront(VMesh v, VMesh va, Fighter f, float T)
    {
        SkState S = f.sks;
        if (S != null && S.parry > 0) Shield(v, va, f, T, Mathf.Clamp01(S.parry * 5));
        SkRun s = f.sk; if (s == null || f.cur == null) return;
        string id = s.def.id; float t = s.t;
        GG.Build(f.cur);
        if (U.Eq(id, "kokzhal") && t > 0.12f) Claws(v, va, f, t, T);
        else if (U.Eq(id, "otty")) FlameBlade(v, va, f, t, T);
        else if (U.Eq(id, "jalyn")) FlameColumn(v, va, f, t, T, true);
        else if (U.Eq(id, "aiqai") && t > 0.22f) RoarWaves(v, va, f, t, T);
        else if (U.Eq(id, "quiyn")) BladeCircle(v, va, f, t, T);
        else if (U.Eq(id, "alpamys")) SpiritBlade(v, va, f, t, T);
        else if ((U.Eq(id, "naizagai") || U.Eq(id, "boran")) && t < 0.62f) { WeaponTip(f); va.FillC(U.Eq(id, "boran") ? CA(220, 248, 255, 0.9f) : CA(255, 242, 122, 0.9f)); va.Star(wx, wy, 9 + 5 * Mathf.Sin(T * 30), T * 8); va.Glow(wx, wy, 40, U.Eq(id, "boran") ? CA(170, 230, 255, 0.6f) : CA(255, 236, 120, 0.6f)); }
        else if (U.Eq(id, "sadaq") && t < 0.4f) { WeaponTip(f); va.Glow(wx, wy, 46, CA(160, 255, 220, 0.6f * (1 - t / 0.4f))); }
    }

    static void WeaponTip(Fighter f)
    {
        float w = f.cur[Pz.W], L = f.wpLen + 20;
        W(f, GG.hFx + Mathf.Sin(w) * L, GG.hFy + Mathf.Cos(w) * L);
    }

    // ---------- Бүркіт шүйілуі: алтын қанаттар ----------
    static void EagleWings(VMesh v, VMesh va, Fighter f, float t, float T)
    {
        float a = Mathf.Clamp01(t * 10), flap = Mathf.Sin(t * 16) * 0.22f, sc = f.sc;
        W(f, GG.Sx - 10, GG.Sy + 4); float sx = wx, sy = wy;
        va.Glow(sx, sy, 150 * sc / Db.S, CA(255, 214, 107, 0.35f * a));
        for (int side = 0; side < 2; side++)
        {
            float base0 = side == 0 ? -0.75f : -0.35f, len = (side == 0 ? 300 : 250) * sc / Db.S;
            v.Save(); v.Translate(sx, sy); v.Scale(-f.facing, 1); v.Rotate(base0 - flap * (side == 0 ? 1 : 0.7f));
            // үш қатар қауырсын: ішкісі қою, сыртқысы ашық
            for (int row = 0; row < 3; row++)
            {
                int n = 6 - row; float rl = len * (1 - row * 0.22f), alpha = a * (side == 0 ? 0.95f : 0.7f);
                v.FillC(row == 0 ? CA(150, 96, 40, alpha) : row == 1 ? CA(196, 140, 64, alpha) : CA(236, 196, 120, alpha));
                for (int j = 0; j < n; j++)
                {
                    float u = j / (float)(n - 1), ang = -0.2f - u * 1.1f, fx = Mathf.Cos(ang) * rl * (0.55f + 0.45f * (1 - u)), fy = Mathf.Sin(ang) * rl * (0.55f + 0.45f * (1 - u));
                    P(0, fx * 0.25f, fy * 0.25f - 6); P(1, fx * 1.0f, fy * 1.0f - 4); P(2, fx * 1.08f + 6, fy * 1.08f + 6); P(3, fx * 0.25f + 6, fy * 0.25f + 6); v.Poly(px, py, 4);
                }
            }
            v.Restore();
        }
        // төменге шүйілу сызықтары
        if (f.vy > 0) { va.FillC(CA(255, 232, 170, 0.5f * a)); for (int j = 0; j < 4; j++) { W(f, -20 + j * 14, -260 - j * 8); Seg(va, wx, wy, wx - f.vx * 0.12f, wy - 160, 2); } }
    }

    // ---------- рух ізі (Көкжал) ----------
    static void SpiritTrail(VMesh va, Fighter f, float t, float T, int r, int g, int b)
    {
        float a = Mathf.Clamp01(t * 8) * Mathf.Clamp01((0.8f - t) * 4);
        for (int j = 0; j < 5; j++)
        {
            float lag = j * 0.035f, yy = -60 - j * 30;
            W(f, -30 - j * 4, yy); float x0 = wx, y0 = wy;
            P(0, x0, y0); P(1, x0 - f.vx * (0.06f + lag), y0 - f.vy * (0.06f + lag) + Mathf.Sin(T * 20 + j) * 6); P(2, x0 - f.vx * (0.14f + lag), y0 - f.vy * (0.14f + lag));
            va.FillC(CA(r, g, b, a * (0.5f - j * 0.07f))); va.Thin(px, py, 3, 10 - j);
        }
        W(f, 0, -150); va.Glow(wx, wy, 120 * f.sc / Db.S, CA(r, g, b, 0.35f * a));
    }
    static void Claws(VMesh v, VMesh va, Fighter f, float t, float T)
    {
        float a = Mathf.Clamp01((t - 0.12f) * 8) * Mathf.Clamp01((0.7f - t) * 5);
        W(f, GG.legF.fx + 26, GG.legF.fy - 30); float cx = wx, cy = wy;
        for (int j = 0; j < 3; j++)
        {
            int n = 8;
            for (int q = 0; q < n; q++) { float u = q / (float)(n - 1); P(q, cx + f.facing * (-30 + u * 70), cy + (j - 1) * 18 - 40 + u * 80 + Mathf.Sin(u * Mathf.PI) * -10); }
            va.FillC(CA(220, 200, 255, 0.5f * a)); va.Thin(px, py, n, 9);
            v.FillC(CA(255, 255, 255, 0.9f * a)); v.Thin(px, py, n, 2.6f);
        }
    }

    // ---------- Тұлпар: алтын елестер мен жал сызықтары ----------
    static void Afterimages(VMesh v, VMesh va, Fighter f, float t, float T)
    {
        float a = Mathf.Clamp01(t * 8) * Mathf.Clamp01((0.7f - t) * 4);
        if (a <= 0) return;
        Look L = Looks.For(f); bool lv = Painter.Live; Painter.Live = false;
        for (int j = 3; j >= 1; j--) Painter.Draw(v, f, L, CA(255, 214, 107, a * (0.42f - j * 0.1f)), -f.facing * j * 46, 0);
        Painter.Live = lv;
        for (int j = 0; j < 6; j++) { W(f, -40, -40 - j * 36); va.FillC(CA(255, 236, 170, 0.55f * a)); Seg(va, wx, wy, wx - f.facing * (160 + (j % 3) * 50), wy + Mathf.Sin(T * 12 + j) * 4, 3); }
    }

    // ---------- Жалынды шапшу / Отты қылыш ----------
    static void Flame(VMesh v, float x, float y, float w, float h, float lean, int c0, int c1)
    {
        P(0, x - w / 2, y); P(1, x - w * 0.42f + lean * 0.4f, y - h * 0.45f); P(2, x + lean, y - h); P(3, x + w * 0.42f + lean * 0.5f, y - h * 0.5f); P(4, x + w / 2, y);
        v.FillC(c0); v.Poly(px, py, 5);
        P(0, x - w * 0.25f, y); P(1, x + lean * 0.6f, y - h * 0.62f); P(2, x + w * 0.25f, y); v.FillC(c1); v.Poly(px, py, 3);
    }
    static void FlameColumn(VMesh v, VMesh va, Fighter f, float t, float T, bool front)
    {
        float a = Mathf.Clamp01(t * 10) * Mathf.Clamp01((0.75f - t) * 4), sc = f.sc / Db.S;
        if (!front) { va.Save(); W(f, 0, -130); va.Translate(wx, wy); va.Scale(0.55f, 1.25f); va.Glow(0, 0, 200 * sc, CA(255, 138, 58, 0.6f * a)); va.Restore(); }
        // денені орап тұрған жалын тілдері: етектен басқа дейін, ұштары артқа (қозғалысқа қарсы) ығысады
        for (int j = 0; j < 9; j++)
        {
            if ((j % 2 == 0) != front) continue;
            float ph = T * 11 + j * 1.7f, ly = -10 - j * 30, lx = (j % 3 - 1) * 22 + Mathf.Sin(ph) * 6;
            float sz = 1 - Mathf.Abs(j - 4) / 6f, hh = (90 + 40 * Mathf.Sin(ph)) * sc * (0.6f + 0.6f * sz), ww = (46 + 16 * sz) * sc;
            W(f, lx, ly);
            Flame(v, wx, wy + hh * 0.3f, ww, hh, -f.vx * 0.04f + Mathf.Sin(ph * 1.3f) * 10, CA(240, 90, 30, 0.8f * a), CA(255, 210, 100, 0.92f * a));
        }
        // ұшқындар
        for (int j = 0; j < 8; j++) { float u = (T * 1.4f + j / 8f) % 1f; W(f, Mathf.Sin(j * 2.7f + T * 4) * 50, -40 - u * 320); va.FillC(CA(255, 220, 120, a * (1 - u))); Diamond(va, wx, wy, 3 + 3 * (1 - u)); }
    }
    static void FlameBlade(VMesh v, VMesh va, Fighter f, float t, float T)
    {
        float w = f.cur[Pz.W], L = f.wpLen + 30, a = Mathf.Clamp01(t * 10) * Mathf.Clamp01((1.3f - t) * 4), dx = Mathf.Sin(w), dy = Mathf.Cos(w);
        for (int j = 0; j < 7; j++)
        {
            float u = 0.25f + j / 8f; W(f, GG.hFx + dx * L * u, GG.hFy + dy * L * u);
            float fl = 26 + 14 * Mathf.Sin(T * 22 + j * 1.7f);
            va.Glow(wx, wy, 34, CA(255, 140, 50, 0.35f * a));
            Flame(v, wx, wy + 6, 16, fl, Mathf.Sin(T * 15 + j) * 6, CA(255, 110, 36, 0.9f * a), CA(255, 236, 140, 0.95f * a));
        }
        WeaponTip(f); va.Glow(wx, wy, 60, CA(255, 180, 80, 0.6f * a));
    }

    // ---------- Тәңір шапағаты: жарық бағанасы ----------
    static void HealPillar(VMesh v, VMesh va, Fighter f, float T, float k)
    {
        float sc = f.sc / Db.S, w = (46 + 8 * Mathf.Sin(T * 5)) * sc;
        va.QuadC(f.x - w, 0, f.x + w, 0, f.x + w * 0.6f, f.y, f.x - w * 0.6f, f.y, CA(200, 255, 210, 0), CA(200, 255, 210, 0), CA(170, 255, 180, 0.45f * k), CA(170, 255, 180, 0.45f * k));
        va.QuadC(f.x - w * 0.3f, 0, f.x + w * 0.3f, 0, f.x + w * 0.2f, f.y, f.x - w * 0.2f, f.y, CA(255, 255, 255, 0), CA(255, 255, 255, 0), CA(240, 255, 240, 0.5f * k), CA(240, 255, 240, 0.5f * k));
        va.FillC(CA(141, 255, 154, 0.7f * k)); va.Save(); va.Translate(f.x, Db.GROUND - 2); va.Scale(1, 0.2f); va.Ring(0, 0, 70 + 10 * Mathf.Sin(T * 6), 14); va.Restore();
        for (int j = 0; j < 8; j++)
        {
            float u = ((T * 0.6f + j / 8f) % 1f), x = f.x + Mathf.Sin(j * 2.4f + T * 2) * 50 * sc, y = f.y - u * 300 * sc;
            va.FillC(CA(200, 255, 200, k * Mathf.Sin(u * Mathf.PI))); Diamond(va, x, y, 5);
        }
    }

    // ---------- Қалқан: алтын дөңгелек қалқан ----------
    static void Shield(VMesh v, VMesh va, Fighter f, float T, float a)
    {
        W(f, 52, -150); float cx = wx, cy = wy, r = 64 * f.sc / Db.S, pul = 1 + 0.05f * Mathf.Sin(T * 30);
        va.Glow(cx, cy, r * 2.2f, CA(255, 226, 160, 0.5f * a));
        v.Save(); v.Translate(cx, cy); v.Scale(0.42f * f.facing, 1);
        v.FillC(CA(242, 184, 64, 0.9f * a)); v.Circle(0, 0, r * pul);
        v.FillC(CA(150, 90, 34, 0.9f * a)); v.Circle(0, 0, r * 0.84f * pul);
        v.FillC(CA(210, 150, 70, 0.9f * a)); v.Circle(0, 0, r * 0.62f * pul);
        v.FillC(CA(255, 232, 170, a)); v.Circle(0, 0, r * 0.18f);
        v.FillC(CA(255, 226, 150, 0.9f * a)); for (int j = 0; j < 8; j++) { float an = j * 0.785f + T; v.Circle(Mathf.Cos(an) * r * 0.73f, Mathf.Sin(an) * r * 0.73f, 3.6f); }
        v.Restore();
    }

    // ---------- Көкбөрі айқайы: алға таралған дыбыс толқыны ----------
    static void RoarWaves(VMesh v, VMesh va, Fighter f, float t, float T)
    {
        W(f, GG.Cx + 10, GG.Cy); float cx = wx, cy = wy;
        for (int j = 0; j < 4; j++)
        {
            float u = ((t - 0.22f) * 2.2f + j * 0.25f) % 1f, r = 30 + u * 320, a = (1 - u) * Mathf.Clamp01((0.9f - t) * 4);
            int n = 12; for (int q = 0; q < n; q++) { float an = -0.7f + 1.4f * q / (n - 1); P(q, cx + f.facing * Mathf.Cos(an) * r, cy + Mathf.Sin(an) * r * 0.9f); }
            va.FillC(CA(200, 168, 255, 0.55f * a)); va.Thin(px, py, n, 12 * (1 - u) + 3);
            v.FillC(CA(255, 255, 255, 0.6f * a)); v.Thin(px, py, n, 2);
        }
    }

    // ---------- Құйын: жүз шеңбері ----------
    static void BladeCircle(VMesh v, VMesh va, Fighter f, float t, float T)
    {
        float a = Mathf.Clamp01(t * 8) * Mathf.Clamp01((1.1f - t) * 4), R = (f.wpLen + 70) * f.sc / Db.S;
        W(f, 0, -130); float cx = wx, cy = wy, tilt = 0.32f;
        // үш жүз ізі: бастары жарқын, құйрығы бәсеңдеп сөнеді (көлбеу шеңбер)
        for (int j = 0; j < 3; j++)
        {
            float head = T * 16 + j * 2.09f;
            for (int seg = 0; seg < 4; seg++)
            {
                int n = 6; float span = 0.45f, a0 = head - (seg + 1) * span;
                for (int q = 0; q < n; q++) { float an = a0 + span * q / (n - 1), ex = Mathf.Cos(an) * R, ey = Mathf.Sin(an) * R * 0.5f; P(q, cx + ex, cy + ey + ex * tilt); }
                float fade = (1 - seg / 4f) * a;
                va.FillC(CA(190, 255, 236, 0.45f * fade)); va.Thin(px, py, n, 18 - seg * 3);
                v.FillC(CA(255, 255, 255, 0.85f * fade)); v.Thin(px, py, n, 2.6f - seg * 0.5f);
            }
        }
        v.FillC(CA(200, 180, 140, 0.35f * a)); v.Ellipse(f.x, Db.GROUND - 4, R * 1.1f, 14, 0);
        for (int j = 0; j < 6; j++) { float an = T * 10 + j * 1.05f, rr = R * (0.9f + 0.2f * (j % 2)); va.FillC(CA(210, 190, 150, 0.5f * a)); Diamond(va, f.x + Mathf.Cos(an) * rr, Db.GROUND - 10 - (j % 3) * 14, 3); }
    }

    // ---------- Аруақ / айқай: рух тұманы ----------
    static void Wisps(VMesh va, Fighter f, float t, float T, int r, int g, int b)
    {
        float a = Mathf.Clamp01(t * 6) * Mathf.Clamp01((0.9f - t) * 3);
        for (int j = 0; j < 9; j++)
        {
            float u = ((T * 0.8f + j / 9f) % 1f), lx = Mathf.Sin(j * 2.3f + T * 3) * 60, ly = -u * 300;
            W(f, lx, ly); va.FillC(CA(r, g, b, 0.45f * a * Mathf.Sin(u * Mathf.PI)));
            va.Ellipse(wx, wy, 10 + 8 * u, 16 + 14 * u, Mathf.Sin(T + j));
        }
    }

    // ---------- Найзағай: бұлт ----------
    static void StormCloud(VMesh v, VMesh va, float x, float t, float T)
    {
        float a = Mathf.Clamp01(t * 3);
        for (int j = 0; j < 6; j++) { float cx = x + (j - 2.5f) * 46 + Mathf.Sin(T + j) * 6, cy = 70 + (j % 2) * 18; v.FillC(CA(40, 36, 60, 0.75f * a)); v.Ellipse(cx, cy, 60, 32, 0); }
        if (Mathf.Sin(T * 23) > 0.7f) { va.FillC(CA(255, 248, 192, 0.5f * a)); va.Ellipse(x + Mathf.Sin(T * 7) * 60, 74, 50, 22, 0); }
    }

    // ---------- Ақ боран: қар құйыны ----------
    static void SnowSwirl(VMesh v, VMesh va, Fighter f, float t, float T)
    {
        float a = Mathf.Clamp01(t * 5) * Mathf.Clamp01((0.9f - t) * 3);
        W(f, 0, -150); float cx = wx, cy = wy;
        for (int j = 0; j < 14; j++)
        {
            float an = T * 4 + j * 0.45f, rr = 60 + (j % 5) * 22 + t * 120;
            va.FillC(CA(235, 250, 255, 0.85f * a)); va.Star(cx + Mathf.Cos(an) * rr, cy + Mathf.Sin(an) * rr * 0.6f, 4 + (j % 3) * 2, an);
        }
        va.Glow(cx, cy, 160, CA(170, 230, 255, 0.35f * a));
    }

    // ---------- Алпамыс: алып алтын жүз ----------
    static void SpiritBlade(VMesh v, VMesh va, Fighter f, float t, float T)
    {
        float a = Mathf.Clamp01(t * 4) * Mathf.Clamp01((1.6f - t) * 3), sc = f.sc / Db.S;
        W(f, 120, -170); float cx = wx, cy = wy, R = 190 * sc;
        int n = 16; for (int q = 0; q < n; q++) { float an = -1.3f + 2.4f * q / (n - 1); P(q, cx + f.facing * Mathf.Cos(an) * R * 0.55f, cy + Mathf.Sin(an) * R); }
        va.FillC(CA(255, 214, 107, 0.45f * a)); va.Thin(px, py, n, 34);
        v.FillC(CA(255, 248, 220, 0.9f * a)); v.Thin(px, py, n, 4);
        va.Glow(cx, cy, R * 1.2f, CA(255, 214, 107, 0.3f * a));
    }

    // ---------- Жел жебе (Садақ оғы) ----------
    public static void ArrowTrail(VMesh va, float[] ar, float T)
    {
        float x = ar[0], y = ar[1], d = ar[2];
        va.Glow(x, y, 40, CA(160, 255, 220, 0.4f));
        for (int j = 0; j < 3; j++) { va.FillC(CA(190, 255, 236, 0.5f - j * 0.12f)); Seg(va, x - d * 30, y + (j - 1) * 5, x - d * (110 + j * 30), y + (j - 1) * 7 + Mathf.Sin(T * 20 + j) * 3, 3 - j * 0.6f); }
    }

    // ---------- Дала дауылы: құйын снаряды ----------
    public static void Tornado(VMesh v, VMesh va, SkProj p, float T)
    {
        float k = Mathf.Clamp01(p.life * 4) * Mathf.Clamp01(p.t * 8), G = Db.GROUND;
        va.Save(); va.Translate(p.x, G - 130); va.Scale(0.7f, 1); va.Glow(0, 0, 160, CA(159, 240, 220, 0.4f * k)); va.Restore();
        // конус денесі
        for (int j = 0; j < 12; j++)
        {
            float yy = G - 4 - j * 22, rw = 12 + j * 8, off = Mathf.Sin(p.t * 11 + j * 0.6f) * 10 * (j / 11f), rot = p.t * 18 + j * 0.9f;
            int n = 14; for (int q = 0; q < n; q++) { float an = rot + 4.4f * q / (n - 1); P(q, p.x + off + Mathf.Cos(an) * rw, yy + Mathf.Sin(an) * rw * 0.28f); }
            va.FillC(CA(j % 3 == 0 ? 255 : 190, 255, j % 3 == 0 ? 255 : 236, k * (0.3f + 0.45f * j / 11f))); va.Thin(px, py, n, 3 + j * 0.25f);
        }
        // шаң мен ұшқан шөп
        v.FillC(CA(190, 160, 120, 0.4f * k)); v.Ellipse(p.x, G - 4, 70, 14, 0);
        for (int j = 0; j < 8; j++) { float an = p.t * 9 + j * 0.8f, h = (j * 31 + p.t * 200) % 240; v.FillC(j % 2 == 0 ? CA(120, 96, 60, k) : CA(150, 170, 90, k)); Diamond(v, p.x + Mathf.Cos(an) * (16 + h * 0.3f), G - 10 - h, 3.5f); }
    }

    // ---------- Арқан ----------
    public static void Lasso(VMesh v, SkProj p, float T)
    {
        Fighter o = p.owner; if (o == null || o.cur == null) return;
        GG.Build(o.cur); W(o, GG.hFx, GG.hFy); float hx = wx, hy = wy, dir = p.vx >= 0 ? 1 : -1;
        int n = 14; float cx = (hx + p.x) / 2, cy = Mathf.Max(hy, p.y) + 24;
        for (int q = 0; q < n; q++) { float u = q / (float)(n - 1), a = (1 - u) * (1 - u), b = 2 * (1 - u) * u, c = u * u; P(q, a * hx + b * cx + c * p.x, a * hy + b * cy + c * p.y + Mathf.Sin(u * 12 + T * 20) * 2); }
        v.FillC(C(110, 70, 30)); v.Thin(px, py, n, 5);
        v.FillC(C(200, 148, 90)); v.Thin(px, py, n, 2.2f);
        v.Save(); v.Translate(p.x + dir * 18, p.y); v.Rotate(T * 10); v.FillC(C(110, 70, 30)); v.Ring(0, 0, 18, 4.5f); v.FillC(C(214, 166, 104)); v.Ring(0, 0, 18, 1.8f); v.Restore();
    }
}
