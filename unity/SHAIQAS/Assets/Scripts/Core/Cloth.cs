// SHAIQAS Core · Мата физикасы (веб: mkChain / stepChain / updSecondary / chainLocal / ribbon):
// белбеудің екі ұшы мен шапан етегі — инерциясы, салмағы мен серпімділігі бар нүктелер тізбегі.
// Жауынгер жүгіргенде, секіргенде, бұрылғанда мата кешігіп ереді де, желбірейді.
using UnityEngine;

public class Chain
{
    public int n; public float seg; public bool ok;
    public float[] x, y, px, py;
    public Chain(int n0, float seg0) { n = n0; seg = seg0; x = new float[n0]; y = new float[n0]; px = new float[n0]; py = new float[n0]; }

    /// <summary>Бір қадам: (ax, ay) — бекітілген нүкте (әлемде), (rdx, rdy) — тыныштық бағыты.</summary>
    public void Step(float ax, float ay, float rdx, float rdy, float stiff, float damp, float grav, float dt)
    {
        if (!ok || Mathf.Abs(x[0] - ax) + Mathf.Abs(y[0] - ay) > 260)
        {
            for (int i = 0; i < n; i++) { x[i] = ax + rdx * seg * i; px[i] = x[i]; y[i] = ay + rdy * seg * i; py[i] = y[i]; }
            ok = true;
        }
        x[0] = ax; px[0] = ax; y[0] = ay; py[0] = ay;
        float f = dt * 60, dm = Mathf.Pow(damp, f), sf = stiff > 0 ? 1 - Mathf.Pow(1 - stiff, f) : 0, g2 = grav * dt * dt;
        for (int i = 1; i < n; i++)
        {
            float vx = (x[i] - px[i]) * dm, vy = (y[i] - py[i]) * dm;
            px[i] = x[i]; py[i] = y[i];
            x[i] += vx; y[i] += vy + g2;
            if (sf > 0) { x[i] += (ax + rdx * seg * i - x[i]) * sf; y[i] += (ay + rdy * seg * i - y[i]) * sf; }
        }
        for (int i = 1; i < n; i++)
        {
            float dx = x[i] - x[i - 1], dy = y[i] - y[i - 1], d = Pz.Hypot(dx, dy); if (d < 0.0001f) d = 1;
            x[i] = x[i - 1] + dx / d * seg; y[i] = y[i - 1] + dy / d * seg;
            if (y[i] > Db.GROUND - 2) y[i] = Db.GROUND - 2;
        }
    }
}

public static class Cloth
{
    static readonly Geo G = new Geo();
    // дене түрлендіруі (веб: _setXf): әлем ↔ дененің жергілікті жүйесі
    static float X, Y, F, S, Cs, Sn, Ax, Ay, Bx, By;
    static void SetXf(Fighter f)
    {
        float sq = f.squash; if (sq <= 0) sq = 1;
        X = f.x; Y = f.y; F = f.SpinFacing() * f.sc / Mathf.Sqrt(sq); S = f.sc * sq; Cs = Mathf.Cos(f.rot); Sn = Mathf.Sin(f.rot);
        if (f.fp != null) { Ax = f.fp[0]; Ay = f.fp[1]; Bx = f.fp[2]; By = f.fp[3]; } else { Ax = 0; Ay = 0; Bx = 0; By = 0; }
    }
    static float ox, oy;
    static void L2W(float lx, float ly) { float a = lx - Bx, b = ly - By; ox = X + F * (Cs * a - Sn * b + Ax); oy = Y + S * (Sn * a + Cs * b + Ay); }
    static void W2L(float wx, float wy) { float rx = (wx - X) / F - Ax, ry = (wy - Y) / S - Ay; ox = Cs * rx + Sn * ry + Bx; oy = -Sn * rx + Cs * ry + By; }
    static void D2W(float dx, float dy) { float wx = F * (Cs * dx - Sn * dy), wy = S * (Sn * dx + Cs * dy), l = Pz.Hypot(wx, wy); if (l < 0.0001f) l = 1; ox = wx / l; oy = wy / l; }
    static void Link(Chain c, float lx, float ly, float rdx, float rdy, float stiff, float damp, float grav, float dt)
    {
        L2W(lx, ly); float ax = ox, ay = oy; D2W(rdx, rdy);
        c.Step(ax, ay, ox, oy, stiff, damp, grav, dt);
    }

    public static float HemLen(string c) { return U.Eq(c, "robe") ? 112 : U.Eq(c, "robe2") ? 88 : U.Eq(c, "kobe") ? 44 : 62; }

    /// <summary>Әр кадр (Fighter.Animate-тен кейін): белбеу ұштары мен етек.</summary>
    public static void Update(Fighter f, float dt)
    {
        if (f.belt1 == null) { f.belt1 = new Chain(5, 8); f.belt2 = new Chain(4, 8); f.hem = new Chain(3, 30); }
        if (dt <= 0 || f.cur == null) return;
        SetXf(f); G.Build(f.cur);
        float GR = 1700;
        float bkx = G.AtX(12) - G.nx * 15, bky = G.AtY(12) - G.ny * 15;
        f.belt1.seg = 8 * f.sc; f.belt2.seg = 8 * f.sc;
        Link(f.belt1, bkx, bky, 0, 1, 0.02f, 0.955f, GR, dt);
        Link(f.belt2, bkx + 2, bky + 3, 0, 1, 0.03f, 0.95f, GR, dt);
        string c = f.coat;
        float len = U.Eq(c, "robe") ? 112 : U.Eq(c, "robe2") ? 88 : U.Eq(c, "kobe") ? 44 : U.Eq(c, "fur") ? 62 : U.Eq(c, "chapan") ? 62 : 48;
        f.hem.seg = len * 0.5f * f.sc;
        Link(f.hem, G.Hx - 4, G.Hy + 2, 0, 1, 0.16f, 0.9f, GR, dt);

        // бас киім (веб: updSecondary → hd): бас жүйесіндегі нүкте мен бағыт денеге көшеді
        if (f.plume == null) { f.plume = new Chain(3, 11); f.hatc = new Chain(5, 7); f.tas = new Chain(5, 6); }
        string hb = HatBase(f.hat); float t = f.cur[Pz.T];
        if (U.Eq(hb, "altyn") || U.Eq(hb, "plume") || U.Eq(hb, "bayge"))
        {
            f.plume.seg = 11 * f.sc;
            if (U.Eq(hb, "bayge")) Hd(t, 2, -32, -0.35f, -1); else Hd(t, 1, -42, -0.3f, -1);
            Link(f.plume, hx, hy, hdx, hdy, 0.42f, 0.86f, 500, dt);
        }
        if (U.Eq(hb, "turban")) { Hd(t, -16, -11, -0.5f, 1); f.hatc.seg = 7 * f.sc; Link(f.hatc, hx, hy, hdx, hdy, 0.05f, 0.94f, GR, dt); }
        else if (U.Eq(hb, "malgai")) { Hd(t, 0, -29, 0, 1); f.hatc.seg = 5 * f.sc; Link(f.hatc, hx, hy, hdx, hdy, 0, 0.93f, GR, dt); }
        // қару шашағы (найза, сүңгі): ұштан 124 бірлік төмен, еркін салбырайды
        string ws = WShape(f.weapon);
        if (U.Eq(ws, "naiza") || U.Eq(ws, "sungi"))
        {
            float w = f.cur[Pz.W], dx = Mathf.Sin(w), dy = Mathf.Cos(w), wk = WScale(f.weapon);
            f.tas.seg = (U.Eq(ws, "sungi") ? 7 : 6) * f.sc;
            Link(f.tas, G.hFx + dx * 124 * wk, G.hFy + dy * 124 * wk, 0, 1, 0, 0.93f, GR, dt);
        }
    }

    // бас жүйесі: C нүктесі, t бұрышы (веб: headPt)
    static float hx, hy, hdx, hdy;
    static void Hd(float t, float px, float py, float rx, float ry)
    {
        float c = Mathf.Cos(t), s = Mathf.Sin(t);
        hx = G.Cx + c * px - s * py; hy = G.Cy + s * px + c * py;
        hdx = c * rx - s * ry; hdy = s * rx + c * ry;
    }
    /// <summary>Дүкен бас киімінің негізгі пішіні (веб: HELMS[id].v.base).</summary>
    public static string HatBase(string hat)
    {
        JNode h = Db.Helms == null ? JNode.Null : Db.Helms.Get(hat);
        if (!h.IsNull()) { string b = h.Get("v").S("base"); if (U.Len(b) > 0) return b; }
        return hat;
    }
    /// <summary>Қарудың негізгі пішіні (веб: wShape).</summary>
    public static string WShape(string id)
    {
        JNode w = Db.Weapons == null ? JNode.Null : Db.Weapons.Get(id);
        string s = w.IsNull() ? null : w.S("shape");
        return U.Len(s) > 0 ? s : id;
    }
    static readonly string[] WB_ID = { "soyil", "shoqpar", "qylysh", "aibalta", "naiza", "kurzi", "semser", "qanzhar", "qamshy", "balta", "ai", "sungi", "pernach" };
    static readonly float[] WB_LEN = { 92, 74, 98, 122, 150, 88, 106, 64, 112, 80, 130, 162, 86 };
    /// <summary>Сурет масштабы: қару ұзындығы негізгі пішіннен қаншалықты өзгеше (веб: wScaleOf).</summary>
    public static float WScale(string id)
    {
        JNode w = Db.Weapons == null ? JNode.Null : Db.Weapons.Get(id); if (w.IsNull()) return 1;
        string sh = WShape(id);
        for (int i = 0; i < WB_ID.Length; i++) if (U.Eq(WB_ID[i], sh)) return Mathf.Clamp(w.Get("len").F(WB_LEN[i]) / WB_LEN[i], 0.8f, 1.25f);
        return 1;
    }

    /// <summary>Тізбекті бас жүйесіне көшіреді (веб: chainLocal(c, xs, ys, headG, t)).</summary>
    public static int LocalHead(Fighter f, Chain c, float[] xs, float[] ys, float cx, float cy, float t)
    {
        int n = Local(f, c, xs, ys);
        float cs = Mathf.Cos(-t), sn = Mathf.Sin(-t);
        for (int i = 0; i < n; i++) { float a = xs[i] - cx, b = ys[i] - cy; xs[i] = cs * a - sn * b; ys[i] = sn * a + cs * b; }
        return n;
    }
    /// <summary>Бас жүйесіндегі тізбектің бірінші буыны тыныштық бағытынан қаншалық ауытқыды (веб: chainAngle).</summary>
    public static float Angle(Fighter f, Chain c, float cx, float cy, float t, float rx, float ry, float lim)
    {
        int n = LocalHead(f, c, ax2, ay2, cx, cy, t); if (n < 2) return 0;
        float dx = ax2[1] - ax2[0], dy = ay2[1] - ay2[0];
        return Mathf.Clamp(Mathf.Atan2(rx * dy - ry * dx, rx * dx + ry * dy), -lim, lim);
    }
    static float[] ax2 = new float[8], ay2 = new float[8];

    /// <summary>Тізбекті дененің жергілікті жүйесіне көшіреді (веб: chainLocal).</summary>
    public static int Local(Fighter f, Chain c, float[] xs, float[] ys)
    {
        SetXf(f);
        for (int i = 0; i < c.n; i++) { W2L(c.x[i], c.y[i]); xs[i] = ox; ys[i] = oy; }
        return c.n;
    }

    static float[] rx = new float[16], ry = new float[16];
    /// <summary>Ұшына қарай жіңішкеретін таспа (веб: ribbon).</summary>
    public static void Ribbon(VMesh m, float[] xs, float[] ys, int n, float w0, float w1)
    {
        if (n < 2) return;
        int k = 0;
        for (int side = 1; side >= -1; side -= 2)
            for (int j = 0; j < n; j++)
            {
                int i = side > 0 ? j : n - 1 - j, a = Mathf.Max(0, i - 1), b = Mathf.Min(n - 1, i + 1);
                float tx = xs[b] - xs[a], ty = ys[b] - ys[a], l = Pz.Hypot(tx, ty); if (l < 0.0001f) l = 1; tx /= l; ty /= l;
                float w = (w0 + (w1 - w0) * i / (n - 1)) * 0.5f * side;
                rx[k] = xs[i] - ty * w; ry[k] = ys[i] + tx * w; k++;
            }
        m.Poly(rx, ry, k);
    }
}
