// SHAIQAS Core · Жауынгерді салу: дене бөліктері — үшбұрыштар, бас/бас киім/қару — атластағы суреттер.
// Нәтиже — бір тор (VMesh): Unity оны бір Mesh етіп бір рет салады. Қабаттардың реті вебтегідей.
using UnityEngine;
public static class Col
{
    public static int Parse(string s)
    {
        if (s == null || U.Len(s) == 0) return Pack(255, 0, 255, 255);
        if (U.Ch(s, 0) == '#')
        {
            int r, g, b;
            if (U.Len(s) == 4) { r = Hx(U.Ch(s, 1)) * 17; g = Hx(U.Ch(s, 2)) * 17; b = Hx(U.Ch(s, 3)) * 17; }
            else { r = Hx(U.Ch(s, 1)) * 16 + Hx(U.Ch(s, 2)); g = Hx(U.Ch(s, 3)) * 16 + Hx(U.Ch(s, 4)); b = Hx(U.Ch(s, 5)) * 16 + Hx(U.Ch(s, 6)); }
            return Pack(r, g, b, 255);
        }
        int o = U.IndexOf(s, "("), c = U.IndexOf(s, ")");
        if (o < 0) return Pack(255, 0, 255, 255);
        string[] p = U.Split(U.Sub(s, o + 1, (c < 0 ? U.Len(s) : c) - o - 1), ',');
        int a = 255;
        if (p.Length > 3) a = (int)(Pz.Clamp((float)U.ParseNum(U.Trim(p[3])), 0, 1) * 255 + 0.5f);
        return Pack((int)U.ParseNum(U.Trim(p[0])), (int)U.ParseNum(U.Trim(p[1])), (int)U.ParseNum(U.Trim(p[2])), a);
    }
    static int Hx(char c) { if (c >= '0' && c <= '9') return c - '0'; if (c >= 'a' && c <= 'f') return c - 'a' + 10; if (c >= 'A' && c <= 'F') return c - 'A' + 10; return 0; }
    public static int Pack(int r, int g, int b, int a) { return (a << 24) | ((r & 255) << 16) | ((g & 255) << 8) | (b & 255); }
    public static int R(int c) { return (c >> 16) & 255; }
    public static int G(int c) { return (c >> 8) & 255; }
    public static int B(int c) { return c & 255; }
    public static int A(int c) { return (c >> 24) & 255; }
    public static int WithA(int c, float a) { return Pack(R(c), G(c), B(c), (int)(Pz.Clamp(a, 0, 1) * 255 + 0.5f)); }
}

/// <summary>Үшбұрыштар торы + канвас тәрізді түрлендіру стегі.</summary>
public class VMesh
{
    public float[] vx = new float[2048], vy = new float[2048], u = new float[2048], v = new float[2048];
    public int[] col = new int[2048], slot = new int[2048], tri = new int[6144];
    public int nv, nt;
    // түрлендіру: x' = a*x + c*y + e ; y' = b*x + d*y + f
    public float a = 1, b = 0, c = 0, d = 1, e = 0, f = 0;
    float[] stack = new float[6 * 16]; int sp;
    public int color = -1;      // ағымдағы түс (ARGB)
    public int solid;            // 0 — түсті; әйтпесе барлық бөлік осы бір түспен (көлеңке/жиек өтуі)

    public void Clear() { nv = 0; nt = 0; a = 1; b = 0; c = 0; d = 1; e = 0; f = 0; sp = 0; }
    public void Save() { int k = sp * 6; stack[k] = a; stack[k + 1] = b; stack[k + 2] = c; stack[k + 3] = d; stack[k + 4] = e; stack[k + 5] = f; sp++; }
    public void Restore() { sp--; int k = sp * 6; a = stack[k]; b = stack[k + 1]; c = stack[k + 2]; d = stack[k + 3]; e = stack[k + 4]; f = stack[k + 5]; }
    public void Translate(float x, float y) { e += a * x + c * y; f += b * x + d * y; }
    public void Scale(float sx, float sy) { a *= sx; b *= sx; c *= sy; d *= sy; }
    public void Rotate(float r)
    {
        float cs = Mathf.Cos(r), sn = Mathf.Sin(r);
        float na = a * cs + c * sn, nb = b * cs + d * sn, nc = c * cs - a * sn, nd = d * cs - b * sn;
        a = na; b = nb; c = nc; d = nd;
    }
    public void Fill(string css) { color = Col.Parse(css); }
    public void FillC(int argb) { color = argb; }
    int Cur() { return solid != 0 ? Col.WithA(solid, Col.A(color) / 255f) : color; }

    void Grow()
    {
        int n = vx.Length * 2;
        vx = Re(vx, n); vy = Re(vy, n); u = Re(u, n); v = Re(v, n); col = ReI(col, n); slot = ReI(slot, n);
    }
    static float[] Re(float[] s, int n) { float[] r = new float[n]; for (int i = 0; i < s.Length; i++) r[i] = s[i]; return r; }
    static int[] ReI(int[] s, int n) { int[] r = new int[n]; for (int i = 0; i < s.Length; i++) r[i] = s[i]; return r; }

    int V(float x, float y, int cl, int sl, float tu, float tv)
    {
        if (nv >= vx.Length) Grow();
        vx[nv] = a * x + c * y + e; vy[nv] = b * x + d * y + f; col[nv] = cl; slot[nv] = sl; u[nv] = tu; v[nv] = tv;
        nv++; return nv - 1;
    }
    void T(int i0, int i1, int i2)
    {
        if (nt + 3 > tri.Length) tri = ReI(tri, tri.Length * 2);
        tri[nt] = i0; tri[nt + 1] = i1; tri[nt + 2] = i2; nt += 3;
    }

    // ---------------- примитивтер ----------------
    public void Poly(float[] xs, float[] ys, int n)
    {
        if (n < 3) return;
        int cl = Cur(), b0 = nv;
        for (int i = 0; i < n; i++) V(xs[i], ys[i], cl, -1, 0, 0);
        Tri.Ear(xs, ys, n, this, b0);
    }
    public void AddTri(int i0, int i1, int i2) { T(i0, i1, i2); }

    public void Circle(float x, float y, float r) { Ellipse(x, y, r, r, 0); }
    public void Ellipse(float x, float y, float rx, float ry, float rot)
    {
        int seg = 18, cl = Cur();
        float cs = Mathf.Cos(rot), sn = Mathf.Sin(rot);
        int c0 = V(x, y, cl, -1, 0, 0);
        for (int i = 0; i < seg; i++)
        {
            float t = i * Mathf.PI * 2 / seg, px = Mathf.Cos(t) * rx, py = Mathf.Sin(t) * ry;
            V(x + px * cs - py * sn, y + px * sn + py * cs, cl, -1, 0, 0);
        }
        for (int i = 0; i < seg; i++) T(c0, c0 + 1 + i, c0 + 1 + (i + 1) % seg);
    }
    /// <summary>Веб: capsule(g, x1,y1,r1, x2,y2,r2) — трапеция + екі шеңбер.</summary>
    public void Capsule(float x1, float y1, float r1, float x2, float y2, float r2)
    {
        float dx = x2 - x1, dy = y2 - y1, l = Pz.Hypot(dx, dy); if (l < 0.0001f) l = 1;
        float nx = -dy / l, ny = dx / l;
        int cl = Cur();
        int i0 = V(x1 + nx * r1, y1 + ny * r1, cl, -1, 0, 0), i1 = V(x2 + nx * r2, y2 + ny * r2, cl, -1, 0, 0);
        int i2 = V(x2 - nx * r2, y2 - ny * r2, cl, -1, 0, 0), i3 = V(x1 - nx * r1, y1 - ny * r1, cl, -1, 0, 0);
        T(i0, i1, i2); T(i0, i2, i3);
        Circle(x1, y1, r1); Circle(x2, y2, r2);
    }
    /// <summary>Сызық (lineCap: round — ұштарында жарты шеңбер).</summary>
    public void Stroke(float[] xs, float[] ys, int n, float w, bool round)
    {
        float h = w / 2;
        for (int i = 0; i < n - 1; i++)
        {
            float dx = xs[i + 1] - xs[i], dy = ys[i + 1] - ys[i], l = Pz.Hypot(dx, dy); if (l < 0.0001f) continue;
            float nx = -dy / l * h, ny = dx / l * h; int cl = Cur();
            int i0 = V(xs[i] + nx, ys[i] + ny, cl, -1, 0, 0), i1 = V(xs[i + 1] + nx, ys[i + 1] + ny, cl, -1, 0, 0);
            int i2 = V(xs[i + 1] - nx, ys[i + 1] - ny, cl, -1, 0, 0), i3 = V(xs[i] - nx, ys[i] - ny, cl, -1, 0, 0);
            T(i0, i1, i2); T(i0, i2, i3);
            if (round || i > 0) Circle(xs[i], ys[i], h);
        }
        if (round && n > 1) Circle(xs[n - 1], ys[n - 1], h);
    }
    /// <summary>Атластағы сурет: жергілікті тіктөртбұрыш (x0,y0,w,h) — сурет кілтінің ұяшығы (slot).</summary>
    public void Sprite(int sl, float x0, float y0, float w, float h)
    {
        int cl = solid != 0 ? solid : -1;
        int i0 = V(x0, y0, cl, sl, 0, 1), i1 = V(x0 + w, y0, cl, sl, 1, 1), i2 = V(x0 + w, y0 + h, cl, sl, 1, 0), i3 = V(x0, y0 + h, cl, sl, 0, 0);
        T(i0, i1, i2); T(i0, i2, i3);
    }
}

/// <summary>Құлақ кесу әдісімен көпбұрышты үшбұрыштарға бөлу (сәтсіз болса — желдеткіш).</summary>
public static class Tri
{
    static int[] idx = new int[256];
    public static void Ear(float[] xs, float[] ys, int n, VMesh m, int b0)
    {
        if (n > 256) n = 256;
        float area = 0;
        for (int i = 0; i < n; i++) { int j = (i + 1) % n; area += xs[i] * ys[j] - xs[j] * ys[i]; }
        float sg = area >= 0 ? 1 : -1;
        int cnt = n;
        for (int i = 0; i < n; i++) idx[i] = i;
        int guard = 0;
        while (cnt > 3 && guard < 4 * n)
        {
            guard++;
            bool cut = false;
            for (int k = 0; k < cnt; k++)
            {
                int ia = idx[(k + cnt - 1) % cnt], ib = idx[k], ic = idx[(k + 1) % cnt];
                float cr = (xs[ib] - xs[ia]) * (ys[ic] - ys[ia]) - (ys[ib] - ys[ia]) * (xs[ic] - xs[ia]);
                if (cr * sg <= 0) continue;
                bool inside = false;
                for (int q = 0; q < cnt && !inside; q++)
                {
                    int ip = idx[q];
                    if (ip == ia || ip == ib || ip == ic) continue;
                    if (In(xs[ip], ys[ip], xs[ia], ys[ia], xs[ib], ys[ib], xs[ic], ys[ic], sg)) inside = true;
                }
                if (inside) continue;
                m.AddTri(b0 + ia, b0 + ib, b0 + ic);
                for (int q = k; q < cnt - 1; q++) idx[q] = idx[q + 1];
                cnt--; cut = true; break;
            }
            if (!cut) break;
        }
        // қалған бөлік: желдеткішпен
        for (int k = 1; k < cnt - 1; k++) m.AddTri(b0 + idx[0], b0 + idx[k], b0 + idx[k + 1]);
    }
    static bool In(float px, float py, float ax, float ay, float bx, float by, float cx, float cy, float sg)
    {
        float d1 = ((bx - ax) * (py - ay) - (by - ay) * (px - ax)) * sg;
        float d2 = ((cx - bx) * (py - by) - (cy - by) * (px - bx)) * sg;
        float d3 = ((ax - cx) * (py - cy) - (ay - cy) * (px - cx)) * sg;
        return d1 >= 0 && d2 >= 0 && d3 >= 0;
    }
}
