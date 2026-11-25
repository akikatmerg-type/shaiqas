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
    }

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
