// SHAIQAS · Фондағы шайқас (веб: BGW): алыста қазақ қолы мен жау қолы шайқасады —
// жұптасып семсерлесу, салт аттылар, тулар, шаң. Қан жоқ — балаларға лайық.
using UnityEngine;

public class BgWar
{
    class Side { public int coat; public string hat; public bool spear, shield; }
    class Pair { public float x, ph, sp; public int row; public Side a = new Side(), b = new Side(); }
    class Rider { public float x0, v, ph; public int side, row, coat; }
    class Flag { public float x, ph; public int col; public bool kz; }
    readonly Pair[] pairs = new Pair[16]; readonly Rider[] riders = new Rider[4]; readonly Flag[] flags = new Flag[4];
    static readonly int[] KZ = { C("#b03030"), C("#2f7a4a"), C("#8a2a3a"), C("#c08a2a"), C("#3a7a5a"), C("#a8452a") };
    static readonly string[] HAT = { "turban", "helmet", "fur", "helmet", "turban", "fur", "helmet", "fur", "fur" };
    static readonly float[] RPAR = { 0.42f, 0.52f }, RY = { 574, 592 }, RK = { 0.56f, 0.66f };
    string enemyHat = "helmet";
    static int C(string s) { return Col.Parse(s); }
    static int Dk(int c, float t) { return Col.Pack((int)(Col.R(c) * (1 - t)), (int)(Col.G(c) * (1 - t)), (int)(Col.B(c) * (1 - t)), 255); }
    static float[] xs = new float[12], ys = new float[12];
    static void Poly(VMesh m, params float[] p) { int n = p.Length / 2; for (int i = 0; i < n; i++) { xs[i] = p[2 * i]; ys[i] = p[2 * i + 1]; } m.Poly(xs, ys, n); }
    static void Line(VMesh m, float w, params float[] p) { int n = p.Length / 2; for (int i = 0; i < n; i++) { xs[i] = p[2 * i]; ys[i] = p[2 * i + 1]; } m.Stroke(xs, ys, n, w, true); }

    System.Random rng;
    float R() { return (float)rng.NextDouble(); }

    public void Build(int ch)
    {
        rng = new System.Random(7700 + ch * 31);
        int acc = C(Db.Ch(ch).S("accent"));
        enemyHat = HAT[Mathf.Clamp(ch, 0, HAT.Length - 1)];
        for (int i = 0; i < 16; i++)
        {
            Pair p = new Pair { x = -260 + i * (Db.ARENA + 520) / 16f + R() * 60, row = i % 2, ph = R() * 6.28f, sp = 2.4f + R() * 1.6f };
            p.a.coat = KZ[(int)(R() * KZ.Length) % KZ.Length]; p.a.hat = R() < 0.65f ? "kalpak" : "fur"; p.a.spear = R() >= 0.6f; p.a.shield = R() < 0.35f;
            p.b.coat = R() < 0.5f ? acc : Dk(acc, 0.2f + R() * 0.25f); p.b.hat = enemyHat; p.b.spear = R() >= 0.55f; p.b.shield = R() < 0.45f;
            pairs[i] = p;
        }
        for (int i = 0; i < 4; i++) riders[i] = new Rider { x0 = R() * (Db.ARENA + 600) - 300, v = (i % 2 == 1 ? -1 : 1) * (70 + R() * 50), side = i % 2 == 1 ? 1 : -1, ph = R() * 6, row = R() < 0.5f ? 0 : 1, coat = i % 2 == 1 ? acc : KZ[i % KZ.Length] };
        for (int i = 0; i < 4; i++) flags[i] = new Flag { x = -120 + i * (Db.ARENA + 240) / 3.2f + R() * 80, col = i % 2 == 1 ? acc : C("#2f6fc8"), kz = i % 2 == 0, ph = R() * 6 };
    }

    void Soldier(VMesh m, float x, float y, float k, float dir, Side o, float sw, float bob)
    {
        m.Save(); m.Translate(x, y + bob); m.Scale(k * dir, k);
        float st = Mathf.Sin(sw * Mathf.PI) * 6;
        m.FillC(C("#2a1c14")); Line(m, 6, -3, -34, -9 - st, 0); Line(m, 6, 3, -34, 10 + st, 0);
        m.FillC(o.coat); Poly(m, -12, -30, 12, -30, 9, -70, -9, -70);
        m.FillC(Col.Pack(0, 0, 0, 51)); Poly(m, 0, -30, 12, -30, 9, -70, 1, -70);
        m.FillC(C("#e9ad3c")); Poly(m, -10, -48, 10, -48, 10, -45, -10, -45);
        if (o.shield) { m.FillC(C("#6a4a2a")); m.Circle(8, -52, 10); m.FillC(C("#c8a060")); m.Circle(8, -52, 3); }
        float ang = -2.1f + sw * 2.0f, hx = Mathf.Cos(ang) * 22, hy = -64 + Mathf.Sin(ang) * 22;
        m.FillC(o.coat); Line(m, 6, 2, -64, hx, hy);
        if (o.spear)
        {
            float L = 46, a2 = ang + 0.5f;
            m.FillC(C("#5a3a1e")); Line(m, 2.6f, hx - Mathf.Cos(a2) * 14, hy - Mathf.Sin(a2) * 14, hx + Mathf.Cos(a2) * L, hy + Mathf.Sin(a2) * L);
            m.FillC(C("#d8dde2")); m.Circle(hx + Mathf.Cos(a2) * L, hy + Mathf.Sin(a2) * L, 2.6f);
        }
        else { m.FillC(C("#e6ebf0")); Line(m, 2.6f, hx, hy, hx + Mathf.Cos(ang) * 16, hy + Mathf.Sin(ang) * 16 - 6, hx + Mathf.Cos(ang) * 30, hy + Mathf.Sin(ang) * 30); }
        m.FillC(C("#d8a47c")); m.Circle(1, -79, 8);
        float h2 = -86;
        if (o.hat == "kalpak") { m.FillC(C("#f2ece0")); Poly(m, -8, h2 + 2, 9, h2 + 2, 4, h2 - 16, -2, h2 - 17); m.FillC(C("#1a1210")); Poly(m, -10, h2 + 3, -5, h2 - 3, -2, h2 + 3); }
        else if (o.hat == "fur") { m.FillC(C("#6a4a2a")); m.Ellipse(1, h2 - 2, 10, 7, 0); m.FillC(C("#c8a070")); Poly(m, -9, h2, 11, h2, 11, h2 + 4, -9, h2 + 4); }
        else if (o.hat == "turban") { m.FillC(C("#f0e8d8")); m.Ellipse(1, h2 - 2, 10, 7, 0); Poly(m, -9, h2, 11, h2, 11, h2 + 4, -9, h2 + 4); }
        else { m.FillC(C("#9aa4ae")); m.Ellipse(1, h2 + 1, 9, 8, 0); Poly(m, -3, h2 - 5, 5, h2 - 5, 1, h2 - 15); }
        m.Restore();
    }

    void Horse(VMesh m, float s, float ph)
    {
        m.Ellipse(0, -46 * s, 36 * s, 14 * s, -0.05f);
        Poly(m, 22 * s, -56 * s, 34 * s, -50 * s, 52 * s, -82 * s, 44 * s, -90 * s);
        Poly(m, 44 * s, -92 * s, 66 * s, -80 * s, 64 * s, -73 * s, 46 * s, -76 * s);
        Poly(m, 40 * s, -94 * s, 44 * s, -104 * s, 47 * s, -92 * s);
        Poly(m, 28 * s, -60 * s, 46 * s, -92 * s, 38 * s, -96 * s, 22 * s, -66 * s);
        Leg(m, s, 24, 0.5f + Mathf.Sin(ph) * 0.6f, -0.2f + Mathf.Sin(ph) * 0.5f); Leg(m, s, 18, 0.3f + Mathf.Sin(ph + 1) * 0.6f, 0.1f + Mathf.Sin(ph + 1) * 0.4f);
        Leg(m, s, -24, -0.4f + Mathf.Sin(ph + 2) * 0.6f, 0.2f + Mathf.Sin(ph + 2) * 0.4f); Leg(m, s, -18, -0.2f + Mathf.Sin(ph + 3) * 0.6f, 0.3f + Mathf.Sin(ph + 3) * 0.4f);
        Poly(m, -34 * s, -52 * s, -40 * s, -50 * s, -60 * s, -26 * s + Mathf.Sin(ph) * 4 * s, -48 * s, -30 * s);
    }
    void Leg(VMesh m, float s, float x0, float a1, float a2)
    {
        float kx = x0 * s + Mathf.Sin(a1) * 20 * s, ky = -38 * s + Mathf.Cos(a1) * 20 * s;
        Line(m, 5 * s, x0 * s, -42 * s, kx, ky, kx + Mathf.Sin(a2) * 20 * s, ky + Mathf.Cos(a2) * 20 * s);
    }
    void RiderDraw(VMesh m, float x, float y, float s, float dir, float ph, string hat)
    {
        m.Save(); m.Translate(x, y); m.Scale(dir, 1);
        Horse(m, s, ph);
        Poly(m, -8 * s, -58 * s, 10 * s, -58 * s, 12 * s, -92 * s, -2 * s, -97 * s);
        Line(m, 6 * s, 2 * s, -60 * s, 12 * s, -46 * s, 8 * s, -32 * s);
        m.Circle(6 * s, -105 * s, 7.5f * s);
        if (hat == "kalpak") Poly(m, -3 * s, -109 * s, 15 * s, -109 * s, 9 * s, -126 * s, 2 * s, -127 * s);
        else if (hat == "fur") m.Circle(6 * s, -111 * s, 9 * s);
        else if (hat == "turban") m.Ellipse(6 * s, -112 * s, 10 * s, 7 * s, 0);
        else Poly(m, -2 * s, -108 * s, 14 * s, -108 * s, 6 * s, -130 * s);
        Line(m, 5 * s, 8 * s, -90 * s, 20 * s, -80 * s);
        Line(m, 3 * s, 0, -70 * s, 80 * s, -128 * s);
        m.Restore();
    }

    /// <summary>Экран координатындағы кадр (cam — камера жылжуы).</summary>
    public void Draw(VMesh m, float cam, float t)
    {
        // тулар
        foreach (Flag f in flags)
        {
            float x = f.x - cam * RPAR[0], y = RY[0]; if (x < -80 || x > 1360) continue;
            m.FillC(C("#3a2418")); Poly(m, x - 1.5f, y - 120, x + 1.5f, y - 120, x + 1.5f, y - 8, x - 1.5f, y - 8);
            float sw = Mathf.Sin(t * 2.6f + f.ph) * 5;
            m.FillC(f.col); Poly(m, x, y - 118, x + 20, y - 124 + sw, x + 40, y - 114 + sw, x + 38, y - 96 + sw * 0.6f, x + 18, y - 102 + sw * 0.6f, x, y - 94);
            if (f.kz) { m.FillC(C("#f2c230")); m.Circle(x + 18, y - 107 + sw * 0.5f, 3.5f); }
        }
        for (int row = 0; row < 2; row++)
        {
            float par = RPAR[row], ry = RY[row], k = RK[row];
            foreach (Rider rd in riders)
            {
                if (rd.row != row) continue;
                float span = Db.ARENA + 700, wx = ((rd.x0 + t * rd.v) % span + span) % span - 350, x = wx - cam * par;
                if (x < -120 || x > 1400) continue;
                m.FillC(Col.WithA(Dk(rd.coat, 0.15f), 0.9f));
                RiderDraw(m, x, ry + 4, k * 0.62f, rd.v > 0 ? 1 : -1, t * 9 + rd.ph, rd.side < 0 ? "kalpak" : (enemyHat == "fur" ? "fur" : enemyHat));
                m.FillC(Col.Pack(200, 170, 130, 64));
                for (int d = 0; d < 3; d++) { float dd = (t * 3 + d * 0.33f + rd.ph) % 1; m.Circle(x - (rd.v > 0 ? 1 : -1) * (30 + dd * 40), ry - 2 - dd * 10, 6 + dd * 10); }
            }
            foreach (Pair p in pairs)
            {
                if (p.row != row) continue;
                float x = p.x - cam * par; if (x < -80 || x > 1360) continue;
                float tt = t * p.sp + p.ph, gap = 22 + Mathf.Sin(tt * 0.5f) * 8;
                float swA = (Mathf.Sin(tt) + 1) / 2, swB = (Mathf.Sin(tt + Mathf.PI) + 1) / 2;
                float lA = Mathf.Max(0, Mathf.Sin(tt)) * 5, lB = Mathf.Max(0, Mathf.Sin(tt + Mathf.PI)) * 5;
                Soldier(m, x - gap + lA, ry, k, 1, p.a, swA, -Mathf.Abs(Mathf.Sin(tt * 2)) * 1.5f);
                Soldier(m, x + gap - lB, ry, k, -1, p.b, swB, -Mathf.Abs(Mathf.Sin(tt * 2 + 1)) * 1.5f);
                if (Mathf.Sin(tt * 2) > 0.96f) { m.FillC(Col.Pack(255, 220, 120, 102)); m.Circle(x, ry - 70 * k, 7); m.FillC(Col.Pack(255, 240, 180, 230)); m.Circle(x, ry - 70 * k, 3); }
                m.FillC(Col.Pack(190, 160, 120, 41)); m.Ellipse(x, ry + 2, 38, 6, 0);
            }
        }
        // алыс тұман
        for (int i = 0; i < 6; i++) { m.FillC(Col.Pack(210, 200, 215, (int)(41 * (i + 1) / 6f))); Poly(m, 0, 470 + i * 21.6f, 1280, 470 + i * 21.6f, 1280, 491.6f + i * 21.6f, 0, 491.6f + i * 21.6f); }
    }
}
