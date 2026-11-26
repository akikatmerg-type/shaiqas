// SHAIQAS Core · Жауынгердің сыртқы түрі (Look) және оны VMesh-ке салу (веб: drawBody, arm, leg, drawCoat).
using UnityEngine;
public class Look
{
    public string id, coat = "chapan";
    public int headSlot = -1, hatSlot = -1, wepSlot = -1;
    public bool sleeve;
    public float bw = 1, bh = 1;
    // палитра (ARGB)
    public int cCoat, cSkin, cTrim, cTorso, cBelt, cShoulderB, cSkirt, cPauldron, cFur;
    public int aFup, aFfore, aFsk, aBup, aBfore, aBsk, lFpants, lFboot, lBpants, lBboot;
    // бас киім мен қарудың қозғалатын бөліктері (суретте жоқ — Painter салады)
    public string hatBase = "", wShape = "";
    public float wk = 1;
    public int cHatA, cHatC, cAcc;
}

/// <summary>Атлас ұяшықтары: сурет кілті → нөмір (Unity атласты осы тізіммен жинайды).</summary>
public static class Atlas
{
    public static Lst<string> keys = new Lst<string>();
    public static int Slot(string key)
    {
        if (key == null || U.Len(key) == 0) return -1;
        int i = keys.IndexOf(key);
        if (i >= 0) return i;
        keys.Add(key); return keys.Count() - 1;
    }
}

public static class Looks
{
    static JNode meta;
    static Lst<string> ids = new Lst<string>();
    static Lst<Look> cache = new Lst<Look>();
    public static float[] HB = { -50, -95, 100, 140 }, WB = { -70, -120, 140, 380 };

    public static void Init(JNode partsMeta)
    {
        meta = partsMeta; ids.Clear(); cache.Clear();
        JNode hb = meta.Get("headBox"), wb = meta.Get("wepBox");
        if (hb.Count() == 4) for (int i = 0; i < 4; i++) HB[i] = hb.At(i).F();
        if (wb.Count() == 4) for (int i = 0; i < 4; i++) WB[i] = wb.At(i).F();
    }

    /// <summary>Жауынгердің түрі: дене/бас/бас киім — look кілтінен, қару — қару+әшекей түсінен.</summary>
    public static Look For(Fighter f)
    {
        string key = f.look + "|" + f.weapon + "|" + f.accent;
        int i = ids.IndexOf(key);
        if (i >= 0) return cache.Get(i);
        Look L = Build(f);
        ids.Add(key); cache.Add(L);
        return L;
    }

    static Look Build(Fighter f)
    {
        Look L = new Look(); L.id = f.look; L.coat = f.coat;
        JNode looks = meta == null ? JNode.Null : meta.Get("looks");
        JNode lk = looks.Get(f.look);
        if (lk.IsNull()) lk = looks.Get("p_sharua_chapan_kalpak");
        JNode pal = meta.Get("pals").At(lk.Ik("pal"));
        L.headSlot = Atlas.Slot(lk.S("head")); L.hatSlot = Atlas.Slot(lk.S("hat"));
        string wk = meta.Get("weps").S(f.weapon + "|" + f.accent);
        if (U.Len(wk) == 0) wk = meta.Get("weps").S(f.weapon + "|" + Match.GOLD);
        L.wepSlot = Atlas.Slot(wk);
        L.sleeve = lk.Bk("sleeve"); L.bw = lk.Get("bw").F(1); L.bh = lk.Get("bh").F(1);
        L.cCoat = C(pal, "coat"); L.cSkin = C(pal, "skin"); L.cTrim = C(pal, "trim"); L.cTorso = C(pal, "torso"); L.cBelt = C(pal, "belt");
        L.cShoulderB = C(pal, "shoulderB"); L.cSkirt = C(pal, "skirt"); L.cPauldron = C(pal, "pauldron"); L.cFur = C(pal, "fur");
        JNode af = pal.Get("armF"), ab = pal.Get("armB"), lf = pal.Get("legF"), lb = pal.Get("legB");
        L.aFup = C(af, "up"); L.aFfore = C(af, "fore"); L.aFsk = C(af, "sk"); L.aBup = C(ab, "up"); L.aBfore = C(ab, "fore"); L.aBsk = C(ab, "sk");
        L.hatBase = Cloth.HatBase(f.hat); L.wShape = Cloth.WShape(f.weapon); L.wk = Cloth.WScale(f.weapon);
        JNode hp = pal.Get("hat"); L.cHatA = C(hp, "a"); L.cHatC = C(hp, "c"); L.cAcc = Col.Parse(f.accent);
        L.lFpants = C(lf, "pants"); L.lFboot = C(lf, "boot"); L.lBpants = C(lb, "pants"); L.lBboot = C(lb, "boot");
        return L;
    }
    static int C(JNode p, string k) { return Col.Parse(p.S(k)); }
}

public static class Painter
{
    static Geo G = new Geo();
    static float[] px = new float[32], py = new float[32];
    static int BLACK18 = Col.Pack(0, 0, 0, 46), BLACK35 = Col.Pack(0, 0, 0, 89), BLACK25 = Col.Pack(0, 0, 0, 64), WHITE25 = Col.Pack(255, 255, 255, 64), WHITE45 = Col.Pack(255, 255, 255, 115);

    static void P(int i, float x, float y) { px[i] = x; py[i] = y; }
    static float Lp(float a, float b, float k) { return a + (b - a) * k; }

    /// <summary>Жауынгерді салу. solid≠0 — бір түсті өту (көлеңке/жиек), ox/oy — әлемдік ығысу.</summary>
    /// <summary>Мата тізбектерін салу (тек тірі жауынгердің ағымдағы позасы үшін; сағым/көлеңке көшірмелерінде — жоқ).</summary>
    public static bool Live;
    static float[] cx = new float[8], cy = new float[8];
    static bool live; static Fighter lf;
    public static void Draw(VMesh m, Fighter f, Look L, int solid, float ox, float oy)
    {
        live = Live && f.belt1 != null && f.belt1.ok && f.hem.ok; lf = f;
        m.solid = solid;
        float[] p = f.cur;
        m.Save();
        m.Translate(ox, oy);
        m.Translate(f.x, f.y);
        float sq = f.squash; if (sq != 1) m.Scale(1 / Mathf.Sqrt(sq), sq);
        m.Scale(f.SpinFacing() * f.sc, f.sc);
        m.Scale(L.bw, L.bh);
        if (f.fp != null) { m.Translate(f.fp[0], f.fp[1]); m.Rotate(f.rot); m.Translate(-f.fp[2], -f.fp[3]); } else m.Rotate(f.rot);
        G.Build(p);

        Arm(m, G.Sbx, G.Sby, G.eBx, G.eBy, G.hBx, G.hBy, 0.92f, L.aBup, L.aBfore, L.aBsk, L);
        LegDraw(m, G.legB, p[Pz.HX] - 3, p[Pz.HY], L.lBpants, L.lBboot);
        if (live)
        {
            // белбеудің артқы ұштары — желбіреген екі таспа (веб: drawSash)
            int n = Cloth.Local(f, f.belt1, cx, cy); m.FillC(L.cTrim); Cloth.Ribbon(m, cx, cy, n, 7, 3);
            n = Cloth.Local(f, f.belt2, cx, cy); m.FillC(L.cBelt); Cloth.Ribbon(m, cx, cy, n, 6, 2.5f);
            m.FillC(L.cTrim); m.Circle(cx[n - 1], cy[n - 1], 2.2f);
        }

        // кеуде
        float[] lvS = { 0, 0.4f, 0.78f, 1 }, lvF = { 15, 13, 21, 17 }, lvB = { 15, 12, 15, 14 };
        for (int i = 0; i < 4; i++) P(i, G.AtX(lvS[i] * Db.TOR) + G.nx * lvF[i], G.AtY(lvS[i] * Db.TOR) + G.ny * lvF[i]);
        for (int i = 3; i >= 0; i--) P(4 + (3 - i), G.AtX(lvS[i] * Db.TOR) - G.nx * lvB[i], G.AtY(lvS[i] * Db.TOR) - G.ny * lvB[i]);
        m.FillC(L.cTorso); m.Poly(px, py, 8);
        m.FillC(L.cShoulderB); m.Circle(G.Sx - G.nx * 2, G.Sy - G.ny * 2 + 3, 12);
        m.FillC(L.cSkin); m.Capsule(G.Nx, G.Ny, 7.5f, G.Cx - G.ux * 8, G.Cy - G.uy * 8, 6.5f);

        Coat(m, f, L, p);
        LegDraw(m, G.legF, p[Pz.HX] + 3, p[Pz.HY], L.lFpants, L.lFboot);

        // белбеу
        float b0x = G.AtX(12) + G.nx * 17, b0y = G.AtY(12) + G.ny * 17, b1x = G.AtX(12) - G.nx * 16, b1y = G.AtY(12) - G.ny * 16;
        P(0, b0x, b0y); P(1, b1x, b1y);
        m.FillC(L.cBelt); m.Stroke(px, py, 2, 6, false);
        int acc = Col.Parse(f.accent);
        m.FillC(acc); m.Stroke(px, py, 2, 2.2f, false);
        if (solid == 0)
        {
            m.FillC(acc);
            m.Circle(Lp(b0x, b1x, 0.22f), Lp(b0y, b1y, 0.22f), 2.4f); m.Circle(Lp(b0x, b1x, 0.5f), Lp(b0y, b1y, 0.5f), 3.6f); m.Circle(Lp(b0x, b1x, 0.78f), Lp(b0y, b1y, 0.78f), 2.4f);
            m.FillC(WHITE45); m.Circle(Lp(b0x, b1x, 0.5f) - 1, Lp(b0y, b1y, 0.5f) - 1, 1.4f);
        }
        if (U.Eq(f.coat, "robe2"))
        {
            P(0, G.AtX(17) + G.nx * 17, G.AtY(17) + G.ny * 17); P(1, G.AtX(17) - G.nx * 16, G.AtY(17) - G.ny * 16);
            m.FillC(acc); m.Stroke(px, py, 2, 2, false);
        }

        // бас + бас киім (бас жүйесі: C нүктесі, p.t бұрышы)
        m.Save(); m.Translate(G.Cx, G.Cy); m.Rotate(p[Pz.T]);
        if (L.bw != 1 || L.bh != 1) m.Scale(1 / L.bw, 1 / L.bh);
        float[] hb = Looks.HB;
        if (L.headSlot >= 0) m.Sprite(L.headSlot, hb[0], hb[1], hb[2], hb[3]);
        bool flown = live && f.hatOff != null;   // бас киім ұшып кеткен — басында жоқ
        if (!flown)
        {
            HatBack(m, f, L, p);
            if (L.hatSlot >= 0) m.Sprite(L.hatSlot, hb[0], hb[1], hb[2], hb[3]);
            HatFront(m, f, L, p);
        }
        m.Restore();

        // қару (қол жүйесі: 0 бұрыш — төмен; p.w бұрышымен)
        m.Save(); m.Translate(G.hFx, G.hFy); m.Rotate(-p[Pz.W]);
        float[] wb = Looks.WB;
        bool wDrop = live && f.wepOff != null;    // қару қолдан ұшып кеткен
        if (L.wepSlot >= 0 && !wDrop) m.Sprite(L.wepSlot, wb[0], wb[1], wb[2], wb[3]);
        m.Restore();
        if (!wDrop) WeaponTassel(m, f, L, p);

        Arm(m, G.Sx, G.Sy, G.eFx, G.eFy, G.hFx, G.hFy, 1, L.aFup, L.aFfore, L.aFsk, L);
        m.Restore();
        if (live && f.hatOff != null) FlyingHat(m, f, L, ox, oy);
        if (live && f.wepOff != null && L.wepSlot >= 0)
        {
            float[] w = f.wepOff, wb2 = Looks.WB;
            m.Save(); m.Translate(ox + w[0], oy + w[1]); m.Scale(w[6] * f.sc, f.sc); m.Rotate(w[4]);
            m.Sprite(L.wepSlot, wb2[0], wb2[1], wb2[2], wb2[3]);
            m.Restore();
        }
        m.solid = 0;
    }

    /// <summary>Ұшқан бас киім (веб: kidHatDraw): өз орнында айналып, жерге түседі.</summary>
    static void FlyingHat(VMesh m, Fighter f, Look L, float ox, float oy)
    {
        float[] h = f.hatOff; float[] hb = Looks.HB;
        bool lv = live; live = false;              // қозғалмалы бөліктер — тыныш күйде
        m.Save();
        m.Translate(ox + h[0], oy + h[1]); m.Rotate(h[4]); m.Scale(h[6] * f.sc, f.sc); m.Translate(0, 22);
        HatBack(m, f, L, f.cur);
        if (L.hatSlot >= 0) m.Sprite(L.hatSlot, hb[0], hb[1], hb[2], hb[3]);
        HatFront(m, f, L, f.cur);
        m.Restore();
        live = lv;
    }

    // ---------- бас киім: үкі/қауырсын (күмбездің артында), сәлде ұшы мен малақай шашағы (үстінде) ----------
    static void HatBack(VMesh m, Fighter f, Look L, float[] p)
    {
        string h = L.hatBase; bool bay = U.Eq(h, "bayge");
        if (!bay && !U.Eq(h, "altyn") && !U.Eq(h, "plume")) return;
        bool on = live && f.plume != null && f.plume.ok;
        float pl = on ? Cloth.Angle(f, f.plume, G.Cx, G.Cy, p[Pz.T], bay ? -0.33f : -0.29f, -0.95f, 1.1f) : 0;
        m.FillC(L.cHatC);
        m.Save();
        if (bay) { m.Translate(2, -32); m.Rotate(-0.35f + pl * 0.9f); m.Ellipse(0, -12, 4, 13, 0); }
        else
        {
            // веб: әр қауырсын тізбектің бұрышына ілесіп, ұшы кешірек бұрылады
            m.Translate(1, -42); m.Rotate(pl * 0.7f);
            for (int i = 0; i < 4; i++) { m.Rotate(-0.28f + pl * 0.1f); m.Ellipse(-4, -12, 3.2f, 13, 0); }
        }
        m.Restore();
    }
    static void HatFront(VMesh m, Fighter f, Look L, float[] p)
    {
        string h = L.hatBase; bool on = live && f.hatc != null && f.hatc.ok;
        if (U.Eq(h, "turban"))
        {
            m.FillC(L.cHatA);
            if (on) { int n = Cloth.LocalHead(f, f.hatc, cx, cy, G.Cx, G.Cy, p[Pz.T]); Cloth.Ribbon(m, cx, cy, n, 9, 3); }
            else { P(0, -16, -12); P(1, -22, 8); P(2, -15, 6); m.Poly(px, py, 3); }
        }
        else if (U.Eq(h, "malgai") && on)
        {
            m.FillC(L.cAcc); int n = Cloth.LocalHead(f, f.hatc, cx, cy, G.Cx, G.Cy, p[Pz.T]); Cloth.Ribbon(m, cx, cy, n, 5, 1.5f);
        }
    }
    // ---------- найза шашағы (үш тал) және сүңгі жалауы ----------
    static float[] tx2 = new float[8], ty2 = new float[8];
    static void WeaponTassel(VMesh m, Fighter f, Look L, float[] p)
    {
        string w = L.wShape; bool nz = U.Eq(w, "naiza"), sg = U.Eq(w, "sungi");
        if (!nz && !sg) return;
        float a = p[Pz.W], dx = Mathf.Sin(a), dy = Mathf.Cos(a), nx = -dy, ny = dx, k = L.wk;
        float hx = G.hFx, hy = G.hFy;
        bool on = live && f.tas != null && f.tas.ok;
        m.FillC(L.cAcc);
        if (on)
        {
            int n = Cloth.Local(f, f.tas, cx, cy);
            if (sg) { Cloth.Ribbon(m, cx, cy, n, 12, 2); return; }
            Cloth.Ribbon(m, cx, cy, n, 9, 2.5f);
            for (int s = -1; s <= 1; s += 2)
            {
                for (int i = 0; i < n; i++) { tx2[i] = cx[i] + nx * s * i * 1.3f; ty2[i] = cy[i] + ny * s * i * 1.3f + i * 0.4f; }
                Cloth.Ribbon(m, tx2, ty2, n, 5, 1.2f);
            }
            m.Circle(cx[0], cy[0], 4);
            return;
        }
        // тыныш күй (сағым/көлеңке көшірмесі): веб суреттегі қалпы
        if (sg) { P(0, hx + dx * 126 * k, hy + dy * 126 * k); P(1, hx + dx * 110 * k, hy + dy * 110 * k); P(2, hx + dx * 104 * k + nx * 18, hy + dy * 104 * k + ny * 18); m.Poly(px, py, 3); return; }
        P(0, hx + dx * 124 * k + nx * 3, hy + dy * 124 * k + ny * 3); P(1, hx + dx * 108 * k + nx * 8, hy + dy * 108 * k + ny * 8);
        P(2, hx + dx * 110 * k - nx * 6, hy + dy * 110 * k - ny * 6); P(3, hx + dx * 124 * k - nx * 3, hy + dy * 124 * k - ny * 3);
        m.Poly(px, py, 4);
    }

    static void Arm(VMesh m, float sx, float sy, float ex, float ey, float hx, float hy, float k, int cUp, int cFore, int cSk, Look L)
    {
        float midx = Lp(sx, ex, 0.45f), midy = Lp(sy, ey, 0.45f);
        m.FillC(cUp);
        m.Capsule(sx, sy, 9.5f * k, midx, midy, 10 * k);
        m.Capsule(midx, midy, 10 * k, ex, ey, 7 * k);
        float fmx = Lp(ex, hx, 0.3f), fmy = Lp(ey, hy, 0.3f);
        m.FillC(cFore);
        m.Capsule(ex, ey, 7 * k, fmx, fmy, 7.5f * k);
        m.Capsule(fmx, fmy, 7.5f * k, hx, hy, 5 * k);
        if (L.sleeve)
        {
            m.FillC(cUp);
            m.Capsule(ex, ey, 8.5f * k, Lp(ex, hx, 0.78f), Lp(ey, hy, 0.78f), 10.5f * k);
            if (m.solid == 0)
            {
                P(0, Lp(ex, hx, 0.66f), Lp(ey, hy, 0.66f));
                float dx0 = hx - ex, dy0 = hy - ey, l0 = Pz.Hypot(dx0, dy0); if (l0 < 0.001f) l0 = 1;
                float nx = -dy0 / l0 * 10 * k, ny = dx0 / l0 * 10 * k;
                P(1, px[0] - nx, py[0] - ny); P(2, px[0] + nx, py[0] + ny);
                px[0] = px[1]; py[0] = py[1]; px[1] = px[2]; py[1] = py[2];
                m.FillC(L.cTrim); m.Stroke(px, py, 2, 3.2f * k, true);
            }
        }
        m.FillC(cSk);
        float dx = hx - ex, dy = hy - ey, l = Pz.Hypot(dx, dy); if (l < 0.001f) l = 1;
        float ux = dx / l, uy = dy / l;
        m.Circle(hx + ux * 2, hy + uy * 2, 7.2f * k);
        m.Circle(hx + ux * 5 - uy * 4, hy + uy * 5 + ux * 4, 3.6f * k);
    }

    static void LegDraw(VMesh m, Leg L, float hx, float hy, int cPants, int cBoot)
    {
        m.FillC(cPants);
        m.Capsule(hx, hy, 14, L.kx, L.ky, 9);
        float calfx = Lp(L.kx, L.fx, 0.35f), calfy = Lp(L.ky, L.fy, 0.35f);
        m.FillC(cBoot);
        m.Capsule(L.kx, L.ky, 9.5f, calfx - 1.5f, calfy, 9.5f);
        m.Capsule(calfx - 1.5f, calfy, 9.5f, L.fx, L.fy, 5.5f);
        float fx = L.fx, fy = L.fy;
        P(0, fx - 8, fy - 13); P(1, fx + 6, fy - 12); P(2, fx + 17, fy - 4); P(3, fx + 27, fy - 7); P(4, fx + 25, fy + 1);
        P(5, fx - 1, fy + 3); P(6, fx - 3, fy + 6); P(7, fx - 11, fy + 6); P(8, fx - 10, fy);
        m.Poly(px, py, 9);
        if (m.solid == 0)
        {
            m.FillC(BLACK35);
            P(0, fx - 1, fy + 1); P(1, fx + 25, fy - 1); P(2, fx + 25, fy + 1); P(3, fx - 1, fy + 3); m.Poly(px, py, 4);
            P(0, fx - 11, fy + 3); P(1, fx - 3, fy + 3); P(2, fx - 3, fy + 6); P(3, fx - 11, fy + 6); m.Poly(px, py, 4);
        }
    }

    static void Coat(VMesh m, Fighter f, Look L, float[] p)
    {
        float sway = Pz.Clamp(-f.vx * 0.02f, -8, 8) + Mathf.Sin(f.at * 2.6f) * 1.5f;
        float Hx = G.Hx, Hy = G.Hy; string c = f.coat;
        // етек тізбегінен: алдыңғы шет ортаңғы буынға, артқы шет ұшына ереді — мата қисайып желбірейді
        float swF = sway, swB = sway, liF = 0, liB = 0;
        if (live && lf == f)
        {
            int n0 = Cloth.Local(f, f.hem, cx, cy); float ax = Hx - 4, ay = Hy + 2, Lh = f.hem.seg / f.sc * 2;
            swB = Pz.Clamp(cx[n0 - 1] - ax, -Lh * 0.8f, Lh * 0.8f); liB = Pz.Clamp(ay + Lh - cy[n0 - 1], -4, Lh * 0.55f);
            swF = Pz.Clamp((cx[1] - ax) * 2, -Lh * 0.8f, Lh * 0.8f); liF = Pz.Clamp((ay + Lh * 0.5f - cy[1]) * 2, -4, Lh * 0.55f);
            sway = (swF + swB) * 0.5f;
        }
        bool cloth = U.Eq(c, "chapan") || U.Eq(c, "robe") || U.Eq(c, "robe2") || U.Eq(c, "kobe") || U.Eq(c, "fur");
        if (cloth)
        {
            float len = U.Eq(c, "robe") ? 112 : U.Eq(c, "robe2") ? 88 : U.Eq(c, "kobe") ? 44 : 62;
            int n = 0;
            P(n++, G.Sx + G.nx * 16, G.Sy + G.ny * 16);
            P(n++, G.AtX(Db.TOR * 0.45f) + G.nx * 16, G.AtY(Db.TOR * 0.45f) + G.ny * 16);
            P(n++, Hx + 22, Hy);
            if (U.Eq(c, "fur"))
            {
                for (int i = 0; i <= 7; i++) { float k = i / 7f; P(n++, Hx + 28 + Lp(swF, swB, k) - i * (58f / 7), Hy + len - Lp(liF, liB, k) + (i % 2 == 1 ? 8 : 0)); }
            }
            else { P(n++, Hx + 28 + swF, Hy + len - liF); P(n++, Hx + 1 + (swF + swB) * 0.5f, Hy + len + 2 - (liF + liB) * 0.5f); P(n++, Hx - 30 + swB, Hy + len + 3 - liB); }
            P(n++, Hx - 21, Hy);
            P(n++, G.AtX(Db.TOR * 0.45f) - G.nx * 15, G.AtY(Db.TOR * 0.45f) - G.ny * 15);
            P(n++, G.Sx - G.nx * 16, G.Sy - G.ny * 16);
            m.FillC(L.cCoat); m.Poly(px, py, n);
            if (!U.Eq(c, "kobe"))
            {
                m.FillC(L.cTrim); m.Stroke(px, py, U.Eq(c, "fur") ? 3 : 6, 3, true);
                if (m.solid == 0)
                {
                    float ax = G.AtX(Db.TOR * 0.3f) - G.nx * 8, ay = G.AtY(Db.TOR * 0.3f) - G.ny * 8;
                    float[] lx = { ax, Hx - 12 + sway * 0.6f }, ly = { ay, Hy + len * 0.9f };
                    m.FillC(BLACK18); m.Stroke(lx, ly, 2, 4, true);
                }
            }
            if (U.Eq(c, "fur")) { m.FillC(L.cFur); m.Circle(G.Sx, G.Sy - 2, 15); m.Circle(G.Nx - 4, G.Ny + 4, 13); }
        }
        else
        {
            bool lam = U.Eq(c, "lamellar"); float len = lam ? 52 : 42;
            int n = 0;
            P(n++, Hx + 22, Hy - 6);
            for (int i = 0; i <= 6; i++) P(n++, Hx + 24 - i * 8 + Lp(swF, swB, i / 6f) * 0.45f, Hy + len + (i % 2) * 6 - Lp(liF, liB, i / 6f) * 0.4f);
            P(n++, Hx - 24, Hy - 6);
            m.FillC(L.cSkirt); m.Poly(px, py, n);
            if (m.solid == 0)
            {
                m.FillC(BLACK25);
                for (int i = 1; i < 6; i++)
                {
                    float[] lx = { Hx + 24 - i * 8, Hx + 24 - i * 8 + Lp(swF, swB, i / 6f) * 0.45f }, ly = { Hy - 4, Hy + len - Lp(liF, liB, i / 6f) * 0.4f };
                    m.Stroke(lx, ly, 2, 1.2f, false);
                }
            }
            m.FillC(L.cPauldron); m.Ellipse(G.Sx + 2, G.Sy + 6, 17, 11, p[Pz.T] + 0.4f);
            if (lam) m.Ellipse(G.Sx + 2, G.Sy + 16, 16, 9, p[Pz.T] + 0.5f);
            if (m.solid == 0) { m.FillC(WHITE25); m.Ellipse(G.Sx + 5, G.Sy + 2, 8, 4, p[Pz.T] + 0.4f); }
        }
    }
}
