// SHAIQAS Core · Қаңқа: позалар (P), араластыру, геометрия және IK — веб-нұсқадан сөзбе-сөз көшірілген.
// Координаттар — канвас жүйесі (y төмен қарай), жамбас нүктесі (0,0) — табан деңгейі.
public static class Pz
{
    public const int HX = 0, HY = 1, T = 2, FFX = 3, FFY = 4, BFX = 5, BFY = 6, FA1 = 7, FA2 = 8, BA1 = 9, BA2 = 10, W = 11, N = 12;
    public static readonly string[] KEYS = { "hx", "hy", "t", "ffx", "ffy", "bfx", "bfy", "fa1", "fa2", "ba1", "ba2", "w" };
    public static readonly float[] GRP0 = { 1.15f, 1.15f, 1f, 1.15f, 1.15f, 1.15f, 1.15f, 0.92f, 0.84f, 0.92f, 0.84f, 0.84f };
    public static readonly float[] GRP1 = { 1.05f, 1.05f, 1f, 1.05f, 1.05f, 1.05f, 1.05f, 0.95f, 0.8f, 0.95f, 0.8f, 0.8f };

    static Lst<string> names;
    static Lst<float[]> poses;

    public static void Init()
    {
        names = new Lst<string>(); poses = new Lst<float[]>();
        JNode P = Db.Poses;
        for (int i = 0; i < P.Count(); i++)
        {
            JNode p = P.items.Get(i); float[] a = new float[N];
            for (int k = 0; k < N; k++) a[k] = p.Get(KEYS[k]).F();
            names.Add(P.KeyAt(i)); poses.Add(a);
        }
    }
    public static float[] Get(string name)
    {
        int i = names.IndexOf(name);
        if (i < 0) i = names.IndexOf("guard");
        return poses.Get(i);
    }
    public static float[] New() { return new float[N]; }
    public static void Copy(float[] o, float[] a) { for (int k = 0; k < N; k++) o[k] = a[k]; }
    public static void Mix(float[] o, float[] a, float[] b, float t) { for (int k = 0; k < N; k++) o[k] = a[k] + (b[k] - a[k]) * t; }
    public static void Extrap(float[] o, float[] a, float[] b, float s) { for (int k = 0; k < N; k++) o[k] = b[k] + (b[k] - a[k]) * s; }

    public static float Smooth01(float t) { t = t < 0 ? 0 : t > 1 ? 1 : t; return t * t * (3 - 2 * t); }
    public static float EaseOut3(float t) { t = t < 0 ? 0 : t > 1 ? 1 : t; float u = 1 - t; return 1 - u * u * u; }
    public static float Clamp(float v, float a, float b) { return v < a ? a : v > b ? b : v; }
    public static float Lerp(float a, float b, float k) { return a + (b - a) * k; }
    public static float Toward(float v, float t, float d) { return v < t ? Mathf.Min(t, v + d) : Mathf.Max(t, v - d); }
    public static float Rand(float a, float b) { return a + U.Rand() * (b - a); }
    public static float Hypot(float x, float y) { return Mathf.Sqrt(x * x + y * y); }
}

/// <summary>Аяқ (IK нәтижесі): тізе мен табан.</summary>
public class Leg { public float kx, ky, fx, fy; }

/// <summary>Дене геометриясы (веб: geometry(p)).</summary>
public class Geo
{
    public float Hx, Hy, ux, uy, nx, ny, Sx, Sy, Nx, Ny, Cx, Cy;
    public Leg legF = new Leg(), legB = new Leg();
    public float eFx, eFy, hFx, hFy, Sbx, Sby, eBx, eBy, hBx, hBy;

    public float AtX(float s) { return Hx + ux * s; }
    public float AtY(float s) { return Hy + uy * s; }

    static void Ik(float hx, float hy, float fx, float fy, Leg o)
    {
        float dx = fx - hx, dy = fy - hy, d = Pz.Hypot(dx, dy);
        if (d < 0.001f) d = 0.001f;
        float max = Db.TH + Db.SH - 0.5f;
        if (d > max) { float s = max / d; dx *= s; dy *= s; d = max; fx = hx + dx; fy = hy + dy; }
        float a = Mathf.Acos(Pz.Clamp((Db.TH * Db.TH + d * d - Db.SH * Db.SH) / (2 * Db.TH * d), -1, 1));
        float bs = Mathf.Atan2(dy, dx);
        float k1x = hx + Mathf.Cos(bs + a) * Db.TH, k1y = hy + Mathf.Sin(bs + a) * Db.TH;
        float k2x = hx + Mathf.Cos(bs - a) * Db.TH, k2y = hy + Mathf.Sin(bs - a) * Db.TH;
        if (k1x > k2x) { o.kx = k1x; o.ky = k1y; } else { o.kx = k2x; o.ky = k2y; }
        o.fx = fx; o.fy = fy;
    }

    public void Build(float[] p)
    {
        Hx = p[Pz.HX]; Hy = p[Pz.HY];
        float t = p[Pz.T];
        ux = Mathf.Sin(t); uy = -Mathf.Cos(t);
        nx = Mathf.Cos(t); ny = Mathf.Sin(t);
        Sx = AtX(Db.TOR - 8); Sy = AtY(Db.TOR - 8);
        Nx = AtX(Db.TOR); Ny = AtY(Db.TOR);
        Cx = Nx + ux * 20 + nx * 2; Cy = Ny + uy * 20 + ny * 2;
        Ik(Hx + 3, Hy, p[Pz.FFX], p[Pz.FFY], legF);
        Ik(Hx - 3, Hy, p[Pz.BFX], p[Pz.BFY], legB);
        float d1x = Mathf.Sin(p[Pz.FA1]), d1y = Mathf.Cos(p[Pz.FA1]), d2x = Mathf.Sin(p[Pz.FA2]), d2y = Mathf.Cos(p[Pz.FA2]);
        eFx = Sx + d1x * Db.UA; eFy = Sy + d1y * Db.UA;
        hFx = eFx + d2x * Db.FA; hFy = eFy + d2y * Db.FA;
        Sbx = Sx - nx * 5; Sby = Sy - ny * 5;
        float b1x = Mathf.Sin(p[Pz.BA1]), b1y = Mathf.Cos(p[Pz.BA1]), b2x = Mathf.Sin(p[Pz.BA2]), b2y = Mathf.Cos(p[Pz.BA2]);
        eBx = Sbx + b1x * Db.UA; eBy = Sby + b1y * Db.UA;
        hBx = eBx + b2x * Db.FA; hBy = eBy + b2y * Db.FA;
    }
}
