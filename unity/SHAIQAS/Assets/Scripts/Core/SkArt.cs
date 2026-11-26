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
}
