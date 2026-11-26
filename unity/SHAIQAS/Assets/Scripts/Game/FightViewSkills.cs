// SHAIQAS · Әдістердің Unity қабаты (веб: skSample, skDrawBack, skDrawFront, skFxFallback, skOverlay):
// енгізу буферін толтыру, снарядтар (жел құйыны, мұз найза, арқан), тас найзалар, найзағай,
// аруақ-көлеңке, мұз/есеңгіреу/қалқан белгілері, әдіс атауы, алғаш қолдану жолағы, дыбыстар.
using System.Collections.Generic;
using UnityEngine;

public partial class FightView
{
    Mesh meshSkF; readonly VMesh vskf = new VMesh();
    // аруақ үшін поза тарихы (веб: T.hist — 16 кадр)
    class PoseRec { public float[] p = new float[Pz.N]; public float x, y; public int fc; }
    readonly List<PoseRec> histP = new List<PoseRec>(), histE = new List<PoseRec>();
    static float[] zx = new float[32], zy = new float[32];

    void SkSetup()
    {
        meshSkF = NewMesh("SkFront", 25, out MeshRenderer _r);
        histP.Clear(); histE.Clear();
    }

    /// <summary>Әр кадр: бағыт белгісі (жауға қатысты F/B) мен батырмалар буферге (веб: skSample).</summary>
    void SkFeed(bool upHeld)
    {
        if (m.phase != Match.FIGHT || m.paused) { m.skBuf.fresh = false; return; }
        int xr = intent.move * m.player.facing;
        string tok = (intent.down ? "D" : upHeld ? "U" : "") + (xr > 0 ? "F" : xr < 0 ? "B" : "");
        if (tok.Length == 0) tok = "N";
        Skills.Feed(m.skBuf, m.clock, tok, intent.j, intent.k, intent.l);
    }

    void SkSounds()
    {
        for (int i = 0; i < m.sndQ.Count(); i++) Snd.Play(m.sndQ.Get(i), 1, 0);
        m.sndQ.Clear();
    }

    /// <summary>FX_SKILL: түсі мен күші бойынша жарқыл, сақина, бөлшектер (веб: skFxFallback).</summary>
    void SkFx(FxEv e)
    {
        int ki = Mathf.Clamp(e.flags, 0, Skills.KINDS.Length - 1); string kind = Skills.KINDS[ki];
        int c = Col.Parse(Skills.KIND_COL[ki]); float pw = Mathf.Max(0.2f, e.n / 100f), d = e.dir == 0 ? 1 : e.dir, x = e.x, y = e.y;
        if (prts.Count > 480) pw *= 0.5f;
        Flash(x, y, 150 * pw, c, 0.26f, 0.3f); Flash(x, y, 64 * pw, C(255, 255, 255), 0.12f, 0.2f);
        RingFx(x, y, 10, 560 * pw, 0.3f, c, 2.5f + 2.5f * pw);
        int n = Mathf.RoundToInt(10 + 14 * pw);
        for (int i = 0; i < n; i++)
        {
            float a = Rn(0, 6.28f), s = Rn(0.3f, 1) * 520 * pw;
            if (kind == "fire") P(P_EMBER, x + Rn(-20, 20), y + Rn(-20, 20), Rn(-90, 90) + d * Rn(40, 200), -Rn(140, 460), Rn(0.3f, 0.7f), i % 3 != 0 ? c : C(255, 211, 107), Rn(6, 14), -180);
            else if (kind == "heal" || kind == "spirit") P(P_EMBER, x + Rn(-40, 40), y + Rn(-30, 30), Rn(-60, 60), -Rn(80, 300), Rn(0.4f, 0.8f), i % 3 != 0 ? c : C(255, 255, 255), Rn(5, 11), -120);
            else if (kind == "wind") P(P_HOT, x + Mathf.Cos(a) * 30, y + Mathf.Sin(a) * 30, -Mathf.Sin(a) * s + d * 120, Mathf.Cos(a) * s * 0.5f - 60, Rn(0.2f, 0.45f), i % 2 != 0 ? c : C(255, 255, 255), Rn(1.4f, 2.8f), 0);
            else if (kind == "thunder") { if (i % 4 != 0) P(P_HOT, x + Rn(-10, 10), y + Rn(-10, 10), Mathf.Cos(a) * s * 1.4f, Mathf.Sin(a) * s * 1.4f, Rn(0.12f, 0.3f), i % 2 != 0 ? c : C(255, 255, 255), Rn(1.2f, 2.6f), 400); else P(P_GLINT, x, y, Mathf.Cos(a) * s, Mathf.Sin(a) * s, Rn(0.12f, 0.3f), c, Rn(14, 26), 400); }
            else P(P_HOT, x, y, Mathf.Cos(a) * s, Mathf.Sin(a) * s - 120, Rn(0.18f, 0.4f), i % 3 != 0 ? c : C(255, 255, 255), Rn(1.4f, 3), 1300);
        }
        if (kind == "ice") for (int i = 0; i < Mathf.RoundToInt(8 * pw); i++) P(P_CHIP, x, y, d * Rn(80, 380), -Rn(100, 420), Rn(0.4f, 0.8f), i % 3 == 0 ? C(242, 253, 255) : i % 3 == 1 ? C(180, 236, 255) : C(124, 200, 240), Rn(4, 9), 1500);
        if (kind == "earth") { for (int i = 0; i < Mathf.RoundToInt(8 * pw); i++) P(P_CHIP, x, Mathf.Min(y, Db.GROUND), Rn(-260, 260), -Rn(200, 520), Rn(0.5f, 0.9f), C(120, 92, 64), Rn(5, 10), 1500); Dust(x, Mathf.Min(y, Db.GROUND), d, 6); }
        if (kind == "blade") for (int i = 0; i < 3; i++) P(P_GLINT, x + Rn(-24, 24), y + Rn(-24, 24), d * Rn(40, 160), Rn(-80, 20), Rn(0.14f, 0.26f), C(255, 244, 208), Rn(16, 30), 0);
    }

    static Vector2 Hand(Fighter f) { return new Vector2(f.x + f.facing * 58 * f.sc / Db.S, f.y - 175 * f.sc / Db.S); }

    void HistPush(Fighter f, List<PoseRec> h)
    {
        SkState T = f.sks;
        if (T == null || T.cloneT <= 0) { h.Clear(); return; }
        PoseRec r = h.Count >= 16 ? h[0] : new PoseRec(); if (h.Count >= 16) h.RemoveAt(0);
        Pz.Copy(r.p, f.cur); r.x = f.x; r.y = f.y; r.fc = f.facing; h.Add(r);
    }

    /// <summary>Артқы қабат (vfx — қалыпты араластыру; vadd — жарқын): тас найзалар, найзағай белгісі, аруақ, аяз/шипа шеңберлері.</summary>
    void SkBack(VMesh v, VMesh va)
    {
        float T = Time.time;
        SkArt.Spikes(v, va, m);
        for (int i = 0; i < m.skProj.Count(); i++) { SkProj p = m.skProj.Get(i); if (p.kind == "earth") SkArt.EarthWave(v, va, p, T); }
        SkBackF(v, va, m.player, histP, T); SkBackF(v, va, m.enemy, histE, T);
    }
    void SkBackF(VMesh v, VMesh va, Fighter f, List<PoseRec> hist, float T)
    {
        SkState S = f.sks; if (S == null) return;
        float sc = f.sc / Db.S;
        if (f.sk != null && f.sk.def.id == "naizagai" && !f.sk.struck)
        {
            float x = f.sk.tx, a = Mathf.Clamp01(f.sk.t * 3), pul = 0.6f + 0.4f * Mathf.Sin(T * 24);
            va.Save(); va.Translate(x, Db.GROUND - 5); va.Scale(1, 0.32f); va.Glow(0, 0, 110, Col.WithA(C(255, 242, 122), a * 0.8f)); va.Restore();
            va.FillC(Col.WithA(C(255, 242, 122), 0.8f * pul * a)); va.Save(); va.Translate(x, Db.GROUND - 2); va.Scale(1, 16f / 90f); va.Ring(0, 0, 90, 3 * 90f / 16f * 0.35f); va.Restore();
            va.FillC(Col.WithA(C(255, 248, 192), a * 0.25f * pul)); va.Quad(x - 6, 0, x + 6, 0, x + 6, Db.GROUND, x - 6, Db.GROUND);
        }
        if (hist.Count > 6 && S.cloneT > 0)
        {
            PoseRec hh = hist[0]; float a = Mathf.Clamp01(Mathf.Min(S.cloneT, 1)) * (1 - Mathf.Clamp01(S.cloneFade * 2)) * (0.55f + 0.08f * Mathf.Sin(T * 6));
            float gx = hh.x - hh.fc * 64 * sc;
            va.Save(); va.Translate(gx, hh.y - 150 * sc); va.Scale(120f / 180f, 1); va.Glow(0, 0, 180 * sc, Col.WithA(C(200, 168, 255), a * 0.6f)); va.Restore();
            float[] keep = new float[Pz.N]; Pz.Copy(keep, f.cur); float kx = f.x, ky = f.y; int kf = f.facing;
            Pz.Copy(f.cur, hh.p); f.x = gx; f.y = hh.y; f.facing = hh.fc;
            Painter.Draw(v, f, Looks.For(f), Col.WithA(C(186, 160, 255), a), 0, 0);
            Pz.Copy(f.cur, keep); f.x = kx; f.y = ky; f.facing = kf;
        }
        SkArt.Frost(v, va, f, T);
        SkArt.AuraBack(v, va, f, T);
    }

    /// <summary>Алдыңғы қабат: снарядтар, найзағайлар, арқан, қалқан, құйын, мұз, есеңгіреу жұлдыздары.</summary>
    void SkFront(VMesh v, VMesh va)
    {
        float T = Time.time;
        for (int i = 0; i < m.skProj.Count(); i++)
        {
            SkProj p = m.skProj.Get(i); float k = Mathf.Clamp01(p.life * 4) * Mathf.Clamp01(p.t * 8), dir = p.vx >= 0 ? 1 : -1;
            int wc = Col.Parse(Skills.KIND_COL[0]), ic = Col.Parse(Skills.KIND_COL[4]);
            if (p.kind == "wind") SkArt.Tornado(v, va, p, T);
            else if (p.kind == "ice") SkArt.IceLance(v, va, p, T);
            else if (p.kind == "lasso" && p.owner != null) SkArt.Lasso(v, p, T);
        }
        // найзағайлар: [x, t, seed, kind]
        for (int i = 0; i < m.bolts.Count(); i++)
        {
            float[] b = m.bolts.Get(i); float a = 1 - b[1] / 0.34f; int kc = Col.Parse(Skills.KIND_COL[Mathf.Clamp((int)b[3], 0, 7)]);
            System.Random r = new System.Random((int)b[2]); int n = 12; float x = b[0];
            for (int j = 0; j <= n; j++) { zy[j] = Db.GROUND * j / n; zx[j] = x + (j == 0 || j == n ? 0 : ((float)r.NextDouble() - 0.5f) * 70); }
            va.FillC(Col.WithA(kc, 0.22f * a)); va.Thin(zx, zy, n + 1, 26);
            va.FillC(Col.WithA(kc, 0.7f * a)); va.Stroke(zx, zy, n + 1, 8, true);
            va.FillC(Col.WithA(C(255, 255, 255), a)); va.Stroke(zx, zy, n + 1, 3, true);
            // бұтақтар: негізгі найзағайдан жан-жаққа тарайды
            for (int q = 0; q < 3; q++)
            {
                int j0 = 3 + q * 3; float bx = zx[j0], by = zy[j0], sd = ((float)r.NextDouble() < 0.5f ? -1 : 1);
                lx[0] = bx; ly[0] = by; lx[1] = bx + sd * (40 + (float)r.NextDouble() * 60); ly[1] = by + 40 + (float)r.NextDouble() * 50;
                va.FillC(Col.WithA(kc, 0.6f * a)); va.Thin(lx, ly, 2, 4); va.FillC(Col.WithA(C(255, 255, 255), 0.8f * a)); va.Thin(lx, ly, 2, 1.5f);
            }
            va.Save(); va.Translate(x, Db.GROUND - 20); va.Scale(1, 0.5f); va.Glow(0, 0, 140, Col.WithA(kc, a)); va.Restore();
        }
        SkFrontF(v, va, m.player, T); SkFrontF(v, va, m.enemy, T);
    }
    void SkFrontF(VMesh v, VMesh va, Fighter f, float T)
    {
        SkState S = f.sks; if (S == null) return;
        float sc = f.sc / Db.S, hy = f.y - 290 * sc;
        if (S.pullT > 0 && f.opp != null) { Vector2 hnd = Hand(f.opp); v.FillC(C(138, 90, 42)); lx[0] = hnd.x; ly[0] = hnd.y; lx[1] = f.x; ly[1] = f.y - 190 * sc; v.Stroke(lx, ly, 2, 3.2f, true); }
        SkArt.AuraFront(v, va, f, T);
        SkArt.IceBlock(v, va, f, T);
        if (S.stun > 0 && f.state != Fighter.DOWN && f.state != Fighter.DEAD && !(S.ice > 0))
            for (int i = 0; i < 3; i++) { float a = T * 6 + i * 2.09f; v.FillC(C(255, 220, 90)); v.Star(f.x + Mathf.Cos(a) * 34 * sc, hy + Mathf.Sin(a) * 9, 8, T * 3); }
    }

    // ---------- сызба көмекшілері ----------
    static void Tri3(VMesh v, float x0, float y0, float x1, float y1, float x2, float y2) { zx[0] = x0; zy[0] = y0; zx[1] = x1; zy[1] = y1; zx[2] = x2; zy[2] = y2; v.Poly(zx, zy, 3); }
    static void Arc(VMesh v, float cx, float cy, float rx, float ry, float a0, float span, float w)
    {
        int n = 14;
        for (int i = 0; i <= n; i++) { float a = a0 + span * i / n; zx[i] = cx + Mathf.Cos(a) * rx; zy[i] = cy + Mathf.Sin(a) * ry; }
        v.Thin(zx, zy, n + 1, w);
    }
    static void Curve(VMesh v, float x0, float y0, float cx, float cy, float x1, float y1, float w)
    {
        int n = 12;
        for (int i = 0; i <= n; i++) { float u = i / (float)n, a = (1 - u) * (1 - u), b = 2 * (1 - u) * u, c = u * u; zx[i] = a * x0 + b * cx + c * x1; zy[i] = a * y0 + b * cy + c * y1; }
        v.Stroke(zx, zy, n + 1, w, true);
    }

    // ---------- HUD: әдіс атауы, алғаш қолдану, тізбек көмегі ----------
    Vector2 W2S(float x, float y)
    {
        float z = m.zoom, pv = m.pivY;
        return new Vector2((x - m.cam - 640) * z + 640, (y - pv) * z + pv);
    }
    void SkHud()
    {
        foreach (Fighter f in new[] { m.player, m.enemy })
        {
            SkState S = f.sks; if (S == null || S.callText == null || S.callT >= 1.15f) continue;
            float k = S.callT, a = Mathf.Clamp01(Mathf.Min(k * 9, (1.15f - k) * 4)), pop = 1 + 0.3f * Mathf.Max(0, 1 - k * 7);
            Vector2 p = W2S(f.x, f.y - 290 * f.sc / Db.S - 34 - k * 14);
            int sz = Mathf.RoundToInt(22 * pop); float tw = Ui.TextW(S.callText, sz) + 46;
            Color edge = S.callFail ? new Color32(255, 138, 106, 255) : (Color)Col32(Skills.KIND_COL[Skills.KindIdx(S.callKind)]);
            Ui.Fill(p.x - tw / 2, p.y - 22 * pop, tw, 34 * pop, S.callFail ? new Color(0.16f, 0.04f, 0.04f, 0.88f * a) : new Color(0.055f, 0.04f, 0.1f, 0.86f * a));
            Ui.Frame(p.x - tw / 2, p.y - 22 * pop, tw, 34 * pop, new Color(edge.r, edge.g, edge.b, a), 2);
            Color tc = S.callFail ? new Color32(255, 180, 160, 255) : (Color)Col32(Skills.KIND_COL[Skills.KindIdx(S.callKind)]);
            Ui.TextO(S.callText, p.x - tw / 2, p.y - 22 * pop, tw, 34 * pop, sz, new Color(tc.r, tc.g, tc.b, a), TextAnchor.MiddleCenter, true, 2);
        }
        if (m.skFirstName != null && m.skFirstT < 2.8f)
        {
            float a = Mathf.Clamp01(Mathf.Min(m.skFirstT * 6, (2.8f - m.skFirstT) * 3));
            string s = "Әдіс алғаш орындалды: " + m.skFirstName + " · +20 тәжірибе";
            float w = Ui.TextW(s, 17) + 60;
            Ui.Fill(640 - w / 2, 118, w, 34, new Color(0.055f, 0.04f, 0.1f, 0.9f * a));
            Ui.Frame(640 - w / 2, 118, w, 34, new Color(0.62f, 0.88f, 0.54f, a), 1.6f);
            Ui.Text(s, 640 - w / 2, 118, w, 34, 17, new Color(0.85f, 1, 0.78f, a), TextAnchor.MiddleCenter, true);
        }
    }
    static Color32 Col32(string css) { int c = Col.Parse(css); return new Color32((byte)Col.R(c), (byte)Col.G(c), (byte)Col.B(c), 255); }

    // ---------- ЕРКІН АЛАҢ: мақсатты әдіс тақтасы (веб: skDrawArenaHud) ----------
    void ArenaHud()
    {
        if (m.paused) return;
        Skills.Init();
        Lst<SkDef> open = new Lst<SkDef>();
        for (int i = 0; i < Skills.All.Count(); i++) if (Skills.Unlocked(Skills.All.Get(i))) open.Add(Skills.All.Get(i));
        if (open.Count() == 0) return;
        int cur = 0; for (int i = 0; i < open.Count(); i++) if (open.Get(i).id == m.arenaSel) cur = i;
        SkDef d = open.Get(cur); m.arenaSel = d.id;
        float x = 24, y = 132, w = 360, h = 150;
        Ui.Fill(x, y, w, h, new Color32(14, 10, 26, 220)); Ui.Frame(x, y, w, h, new Color32(244, 197, 92, 160), 1.6f);
        Ui.TextO("ЕРКІН АЛАҢ", x, y + 6, w, 26, 18, Ui.GOLD, TextAnchor.MiddleCenter, true, 2);
        Ui.Img("Art/sk/ic_" + d.kind + "_0", x + 12, y + 40, 44, 44);
        Ui.TextO(d.name, x + 62, y + 40, w - 120, 26, Ui.Fit(d.name, w - 120, 20), new Color32(255, 227, 160, 255), TextAnchor.MiddleLeft, true, 2);
        // соңғы енгізу: ағымдағы тізбектің қай қадамы орындалды (жылдам көрсету үшін — соңғы сәтті орындау жарқылы)
        float ok = Mathf.Clamp01(1 - m.arenaOkT / 0.8f);
        GameRoot.SeqDraw(d, x + 62, y + 88, 28, m.arenaOk == d.id && ok > 0 ? -1 : 0, false, false);
        if (ok > 0 && m.arenaOk == d.id) Ui.Img("Art/sk/check", x + w - 52, y + 66, 40, 40, ok);
        Ui.Text("Орындалды: " + m.arenaN + " рет", x + 14, y + 112, 200, 24, 15, new Color32(216, 255, 200, 255), TextAnchor.MiddleLeft, true);
        if (Ui.DzBtn("◀", x + w - 104, y + 110, 44, 34, false, true, null, true, false, true)) { m.arenaSel = open.Get((cur - 1 + open.Count()) % open.Count()).id; m.arenaN = 0; Snd.Play("ui_move", 0.5f, 0); }
        if (Ui.DzBtn("▶", x + w - 54, y + 110, 44, 34, false, true, null, true, false, true)) { m.arenaSel = open.Get((cur + 1) % open.Count()).id; m.arenaN = 0; Snd.Play("ui_move", 0.5f, 0); }
    }

    /// <summary>Балалар режимі (веб: kidWarnDraw): жау соққы дайындап жатқанда басының үстінде «!» және нұсқау —
    /// «← ҚОРҒАН» (жоғары/орта соққы) немесе «↙ ОТЫР» (төмен соққы).</summary>
    void KidWarnHud()
    {
        if (m == null || m.paused) return;
        float k = m.KidWarn(); if (k <= 0) return;
        bool low = m.KidWarnLow();
        Fighter kE = m.enemy, kP = m.player;
        Vector2 c = W2S(kE.x, kE.y - 270 * kE.sc / Db.S - 40 - 10 * k);
        float a = Mathf.Clamp01(k * 3), pul = 1 + 0.12f * Mathf.Sin(Time.time * 16.7f), r = 26 * pul;
        Color col = low ? (Color)new Color32(255, 176, 46, 255) : new Color32(255, 74, 58, 255);
        Ui.Circle(c.x + 2, c.y + 3, r + 1, new Color(0.08f, 0.03f, 0.02f, 0.7f * a));
        Ui.Circle(c.x, c.y, r + 3, new Color(1, 0.96f, 0.86f, a));
        Ui.Circle(c.x, c.y, r, new Color(col.r, col.g, col.b, a));
        Ui.TextO("!", c.x - 30, c.y - 26, 60, 52, Mathf.RoundToInt(36 * pul), new Color(1, 1, 1, a), TextAnchor.MiddleCenter, true, 2);
        if (kP == null) return;
        bool left = kP.x < kE.x;
        string lab = low ? (left ? "↙" : "↘") + " ОТЫР" : (left ? "←" : "→") + " ҚОРҒАН";
        Ui.TextO(lab, c.x - 120, c.y - 74, 240, 30, 20, new Color(1, 0.95f, 0.86f, a), TextAnchor.MiddleCenter, true, 3);
    }
}
