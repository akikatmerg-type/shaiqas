// SHAIQAS · Ұрыс көрінісі: параллакс фон, жауынгерлер (бір Mesh + атлас), ұшқын/шаң, кино-камера,
// ұрыс HUD-ы, үзіліс, нәтиже және телефонға арналған сенсор батырмалары.
using System.Collections.Generic;
using UnityEngine;

public class FightView : MonoBehaviour
{
    Match m;
    GameObject world;
    Transform par0, par1, par2, groundT, actors;
    float[] parK = { 0.15f, 0.35f, 0.6f };
    string stageId;
    Material mat;
    Mesh meshP, meshE, meshFx;
    MeshRenderer rP, rE, rFx;
    readonly VMesh vm = new VMesh(), vfx = new VMesh();
    readonly Intent intent = new Intent();
    bool showTouch;
    float lastL = -9, lastR = -9;
    bool prevTL, prevTR, prevTU, prevTJ, prevTK, prevTX;
    int rimCol; float lightX = 1080;

    // ---------------- атлас ----------------
    static Texture2D atlas; static Rect[] rects; static Rect whiteRect; static int packed = -1;

    class Prt { public float x, y, vx, vy, life, max, size, g; public int col, kind; }
    readonly List<Prt> prts = new List<Prt>();

    void Awake()
    {
        mat = Resources.Load<Material>("Materials/FighterAtlas");
        if (mat == null)
        {
            Shader sh = Shader.Find("Shaiqas/FighterAtlas");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            mat = new Material(sh);
        }
        showTouch = Application.isMobilePlatform;
    }

    void EnsureAtlas()
    {
        int n = Atlas.keys.Count();
        if (n == packed && atlas != null) return;
        Texture2D[] src = new Texture2D[n + 1];
        Texture2D w = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        Color32[] px = new Color32[64]; for (int i = 0; i < 64; i++) px[i] = new Color32(255, 255, 255, 255);
        w.SetPixels32(px); w.Apply();
        src[0] = w;
        for (int i = 0; i < n; i++)
        {
            Texture2D t = Resources.Load<Texture2D>("Art/parts/" + Atlas.keys.Get(i));
            src[i + 1] = t != null ? t : w;
            if (t == null) U.Warn("Атласта сурет жоқ: " + Atlas.keys.Get(i));
        }
        if (atlas != null) Destroy(atlas);
        atlas = new Texture2D(2048, 2048, TextureFormat.RGBA32, false);
        Rect[] r = atlas.PackTextures(src, 2, 4096, false);
        atlas.filterMode = FilterMode.Bilinear; atlas.wrapMode = TextureWrapMode.Clamp;
        Rect wr = r[0];
        whiteRect = new Rect(wr.x + wr.width * 0.5f, wr.y + wr.height * 0.5f, 0, 0);
        rects = new Rect[n];
        for (int i = 0; i < n; i++) rects[i] = r[i + 1];
        packed = n;
        mat.mainTexture = atlas;
    }

    // ---------------- құру / жою ----------------
    public void Setup(Match match)
    {
        Teardown();
        m = match;
        stageId = m.stageId;
        world = new GameObject("FightWorld");
        JNode meta = Db.J("meta_stages").Get(stageId);
        JNode pk = meta.Get("par");
        for (int i = 0; i < 3 && i < pk.Count(); i++) parK[i] = pk.At(i).F();
        rimCol = Col.Parse(meta.S("rim")); lightX = meta.Get("lightX").F(1080);
        string b = "Art/stages/" + stageId + "/";
        Spr(b + "sky", 0, 0, 0, world.transform);
        par0 = Spr(b + "layer0", 0, 0, 1, world.transform);
        par1 = Spr(b + "layer1", 0, 0, 2, world.transform);
        Spr(b + "fogMid", 0, 420, 3, world.transform);
        par2 = Spr(b + "layer2", 0, 0, 4, world.transform);
        Spr(b + "fogLow", 0, 540, 5, world.transform);
        actors = new GameObject("Actors").transform; actors.SetParent(world.transform, false);
        groundT = Spr(b + "ground", 0, 0, 6, actors);
        meshFx = NewMesh("Fx", 20, out rFx);
        meshE = NewMesh("Enemy", 10, out rE);
        meshP = NewMesh("Player", 11, out rP);
        prts.Clear();
        Looks.For(m.player); Looks.For(m.enemy);
        EnsureAtlas();
    }

    Transform Spr(string path, float x, float y, int order, Transform parent)
    {
        Texture2D t = Ui.Tex(path);
        GameObject go = new GameObject(path);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, -y, 0);
        if (t != null)
        {
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0, 1), 1f);
            sr.sharedMaterial = mat;
            sr.sortingOrder = order;
        }
        return go.transform;
    }

    Mesh NewMesh(string name, int order, out MeshRenderer r)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(actors, false);
        Mesh mesh = new Mesh(); mesh.MarkDynamic();
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat; r.sortingOrder = order;
        return mesh;
    }

    public void Teardown()
    {
        if (world != null) Destroy(world);
        world = null;
        if (stageId != null) Ui.Forget("Art/stages/" + stageId + "/");
        stageId = null;
    }

    // ---------------- кадр ----------------
    public void Tick(float dt)
    {
        if (m == null || world == null) return;
        ReadIntent();
        if (Inp.Tap(Inp.ESC) && m.phase != Match.MATCHEND) m.paused = !m.paused;
        if (!m.paused && !Tut.Active()) m.Update(dt, intent);
        EatFx();
        StepParticles(m.paused || Tut.Active() ? 0 : dt);
        // камера: зум (W/2, pivY) нүктесінің айналасында, діріл
        float z = m.zoom, pv = m.pivY;
        float sx = m.shake > 0 ? (U.Rand() - 0.5f) * m.shake * 1.6f : 0, sy = m.shake > 0 ? (U.Rand() - 0.5f) * m.shake * 1.6f : 0;
        Camera c = GameRoot.I.cam;
        c.orthographicSize = 360f / z;
        c.transform.position = new Vector3(640 - sx / z, -(pv + (360 - pv) / z) + sy / z, -100);
        par0.localPosition = new Vector3(-m.cam * parK[0], 0, 0);
        par1.localPosition = new Vector3(-m.cam * parK[1], 0, 0);
        par2.localPosition = new Vector3(-m.cam * parK[2], 0, 0);
        actors.localPosition = new Vector3(-m.cam, 0, 0);
        // жауынгерлер
        Build(meshE, m.enemy);
        Build(meshP, m.player);
        BuildFx();
    }

    void Build(Mesh mesh, Fighter f)
    {
        Look L = Looks.For(f);
        EnsureAtlas();
        vm.Clear();
        float side = lightX + m.cam >= f.x ? 1 : -1;
        Painter.Draw(vm, f, L, Col.Pack(20, 12, 10, 255), -side * 1.8f, 1.2f);
        Painter.Draw(vm, f, L, rimCol, side * 2.6f, -1.6f);
        Painter.Draw(vm, f, L, 0, 0, 0);
        if (f.flash > 0) Painter.Draw(vm, f, L, Col.Pack(255, 70, 40, (int)(Mathf.Clamp01(f.flash / 0.15f) * 0.5f * 255)), 0, 0);
        if (f.phase2) Painter.Draw(vm, f, L, Col.Pack(255, 60, 30, (int)((0.22f + 0.14f * Mathf.Sin(f.at * 12)) * 255)), 0, 0);
        Upload(mesh, vm);
    }

    static Vector3[] vbuf = new Vector3[0]; static Color32[] cbuf = new Color32[0]; static Vector2[] ubuf = new Vector2[0], u2buf = new Vector2[0]; static int[] tbuf = new int[0];
    void Upload(Mesh mesh, VMesh v)
    {
        if (vbuf.Length < v.nv) { int n = v.nv * 2; vbuf = new Vector3[n]; cbuf = new Color32[n]; ubuf = new Vector2[n]; u2buf = new Vector2[n]; }
        for (int i = 0; i < v.nv; i++)
        {
            vbuf[i] = new Vector3(v.vx[i], -v.vy[i], 0);
            int c = v.col[i];
            cbuf[i] = new Color32((byte)((c >> 16) & 255), (byte)((c >> 8) & 255), (byte)(c & 255), (byte)((c >> 24) & 255));
            int sl = v.slot[i];
            if (sl < 0 || sl >= rects.Length) { ubuf[i] = new Vector2(whiteRect.x, whiteRect.y); u2buf[i] = Vector2.zero; }
            else
            {
                Rect r = rects[sl];
                ubuf[i] = new Vector2(r.x + v.u[i] * r.width, r.y + v.v[i] * r.height);
                u2buf[i] = new Vector2(c == -1 ? 0 : 1, 0);
            }
        }
        if (tbuf.Length != v.nt) tbuf = new int[v.nt];
        System.Array.Copy(v.tri, tbuf, v.nt);
        mesh.Clear();
        mesh.SetVertices(vbuf, 0, v.nv);
        mesh.SetColors(cbuf, 0, v.nv);
        mesh.SetUVs(0, ubuf, 0, v.nv);
        mesh.SetUVs(1, u2buf, 0, v.nv);
        mesh.SetTriangles(tbuf, 0, true);
    }

    // ---------------- әсерлер ----------------
    void EatFx()
    {
        for (int i = 0; i < m.fx.Count(); i++)
        {
            FxEv e = m.fx.Get(i);
            switch (e.kind)
            {
                case Match.FX_HIT: Burst(e.x, e.y, e.dir, Col.Pack(255, 207, 107, 255), e.n, 520); break;
                case Match.FX_HITSPEC: Burst(e.x, e.y, e.dir, Col.Pack(255, 224, 138, 255), e.n, 700); break;
                case Match.FX_BLOCK: Burst(e.x, e.y, e.dir, Col.Pack(191, 228, 255, 255), e.n, 480); break;
                case Match.FX_DUST: Dust(e.x, e.y, e.dir, e.n); break;
                case Match.FX_KO: Burst(e.x, e.y, 0, Col.Pack(255, 90, 50, 255), 40, 900); break;
                case Match.FX_SPECIAL: Burst(e.x, e.y, 0, Col.Pack(255, 211, 107, 255), 30, 600); break;
                case Match.FX_PHASE: Burst(e.x, e.y, 0, Col.Pack(255, 90, 58, 255), 40, 500); break;
            }
        }
        m.fx.Clear();
    }
    void Burst(float x, float y, float dir, int col, int n, float spd)
    {
        for (int i = 0; i < n; i++)
        {
            float a = dir == 0 ? U.Rand() * Mathf.PI * 2 : (dir > 0 ? 0 : Mathf.PI) + (U.Rand() - 0.5f) * 2.2f;
            float s = spd * (0.35f + U.Rand() * 0.65f);
            prts.Add(new Prt { x = x, y = y, vx = Mathf.Cos(a) * s, vy = Mathf.Sin(a) * s - 120, life = 0.25f + U.Rand() * 0.2f, max = 0.45f, size = 2 + U.Rand() * 2.5f, g = 900, col = col, kind = 0 });
        }
    }
    void Dust(float x, float y, float dir, int n)
    {
        for (int i = 0; i < n; i++)
        {
            float vx = dir != 0 ? dir * (60 + U.Rand() * 180) + (U.Rand() - 0.5f) * 100 : (i % 2 == 1 ? 1 : -1) * (90 + U.Rand() * 170);
            prts.Add(new Prt { x = x + (U.Rand() - 0.5f) * 40, y = Mathf.Min(y, Db.GROUND) - U.Rand() * 10, vx = vx, vy = -(15 + U.Rand() * 75), life = 0.45f + U.Rand() * 0.35f, max = 0.8f, size = 9 + U.Rand() * 9, g = 0, col = Col.Pack(170, 150, 120, 150), kind = 1 });
        }
    }
    void StepParticles(float dt)
    {
        for (int i = prts.Count - 1; i >= 0; i--)
        {
            Prt p = prts[i];
            p.life -= dt; p.vy += p.g * dt; p.x += p.vx * dt; p.y += p.vy * dt;
            if (p.kind == 1) { p.vx *= 1 - 2 * dt; p.size += 14 * dt; }
            if (p.life <= 0) prts.RemoveAt(i);
        }
    }
    static float[] lx = new float[2], ly = new float[2];
    void BuildFx()
    {
        vfx.Clear();
        // жердегі көлеңкелер
        Shadow(m.enemy); Shadow(m.player);
        foreach (Prt p in prts)
        {
            float k = Mathf.Clamp01(p.life / p.max);
            if (p.kind == 0)
            {
                vfx.FillC(Col.WithA(p.col, k));
                lx[0] = p.x; ly[0] = p.y; lx[1] = p.x - p.vx * 0.03f; ly[1] = p.y - p.vy * 0.03f;
                vfx.Stroke(lx, ly, 2, p.size, true);
            }
            else { vfx.FillC(Col.WithA(p.col, k * Col.A(p.col) / 255f)); vfx.Circle(p.x, p.y, p.size); }
        }
        Upload(meshFx, vfx);
    }
    void Shadow(Fighter f)
    {
        float lift = Mathf.Clamp01((Db.GROUND - f.y) / 260);
        vfx.FillC(Col.Pack(10, 4, 4, (int)((0.45f - lift * 0.25f) * 255)));
        float r = (58 - lift * 18) * f.sc;
        vfx.Ellipse(f.x, Db.GROUND, r, r * 0.18f, 0);
    }

    // ---------------- енгізу ----------------
    static readonly Rect TL = new Rect(30, 560, 120, 120), TR = new Rect(170, 560, 120, 120), TU = new Rect(100, 430, 120, 110), TD = new Rect(310, 600, 100, 90);
    static readonly Rect TJ = new Rect(1130, 570, 120, 120), TK = new Rect(1000, 600, 110, 100), TX = new Rect(1130, 440, 120, 110);
    bool T(Rect r) { return showTouch && Inp.AnyTouchIn(Ui.R(r)); }

    void ReadIntent()
    {
        bool tl = T(TL), tr = T(TR), tu = T(TU), td = T(TD), tj = T(TJ), tk = T(TK), tx = T(TX);
        bool left = Inp.Down(Inp.A) || Inp.Down(Inp.LEFT) || tl, right = Inp.Down(Inp.D) || Inp.Down(Inp.RIGHT) || tr;
        bool tapL = Inp.Tap(Inp.A) || Inp.Tap(Inp.LEFT) || (tl && !prevTL), tapR = Inp.Tap(Inp.D) || Inp.Tap(Inp.RIGHT) || (tr && !prevTR);
        float now = Time.unscaledTime;
        intent.Clear();
        if (tapL) { if (now - lastL < 0.26f) intent.dash = -1; lastL = now; }
        if (tapR) { if (now - lastR < 0.26f) intent.dash = 1; lastR = now; }
        intent.move = (right ? 1 : 0) - (left ? 1 : 0);
        intent.up = Inp.Tap(Inp.W) || Inp.Tap(Inp.UP) || Inp.Tap(Inp.SPACE) || (tu && !prevTU);
        intent.down = Inp.Down(Inp.S) || Inp.Down(Inp.DOWN) || td;
        intent.j = Inp.Tap(Inp.J) || (tj && !prevTJ);
        intent.k = Inp.Tap(Inp.K) || (tk && !prevTK);
        intent.l = Inp.Tap(Inp.L) || (tx && !prevTX);
        prevTL = tl; prevTR = tr; prevTU = tu; prevTJ = tj; prevTK = tk; prevTX = tx;
    }

    // ---------------- HUD (IMGUI) ----------------
    public void DrawHud()
    {
        if (m == null) return;
        // кино жолақтары
        if (m.lb > 1) { Ui.Fill(0, 0, 1280, m.lb * 0.5f, Color.black); Ui.Fill(0, 720 - m.lb * 0.5f, 1280, m.lb * 0.5f, Color.black); }
        if (m.flash > 0) Ui.Fill(0, 0, 1280, 720, new Color(1, 0.92f, 0.75f, Mathf.Min(0.35f, m.flash * 1.2f)));
        if (m.dark > 0) Ui.Fill(0, 0, 1280, 720, new Color(0.04f, 0.02f, 0.02f, m.dark * 0.5f));
        Fighter P = m.player, E = m.enemy;
        // денсаулық
        Ui.Fill(96, 14, 500, 30, new Color(0, 0, 0, 0.6f));
        Ui.Fill(100, 18, 492 * Mathf.Clamp01(P.hpLag / P.maxHp), 22, new Color(1, 1, 1, 0.6f));
        Ui.Fill(100, 18, 492 * Mathf.Clamp01(P.hp / P.maxHp), 22, Ui.GOLD);
        Ui.Fill(684, 14, 500, 30, new Color(0, 0, 0, 0.6f));
        float ef = Mathf.Clamp01(E.hp / E.maxHp), el = Mathf.Clamp01(E.hpLag / E.maxHp);
        Ui.Fill(688 + 492 * (1 - el), 18, 492 * el, 22, new Color(1, 1, 1, 0.6f));
        Ui.Fill(688 + 492 * (1 - ef), 18, 492 * ef, 22, new Color(0.86f, 0.2f, 0.16f));
        Ui.TextS(P.name, 100, 44, 400, 28, 18, Ui.CREAM, TextAnchor.MiddleLeft);
        Ui.TextS(E.name, 780, 44, 400, 28, 18, Ui.CREAM, TextAnchor.MiddleRight);
        // рух
        Ui.Bar(100, 74, 240, 10, P.rux / 100, P.rux >= 100 ? new Color(1, 0.85f, 0.3f) : new Color(0.55f, 0.8f, 1f), new Color(0, 0, 0, 0.5f));
        Ui.Bar(940, 74, 240, 10, E.rux / 100, new Color(1, 0.5f, 0.4f), new Color(0, 0, 0, 0.5f));
        if (P.rux >= 100) Ui.TextS("L — РУХ СОҚҚЫСЫ!", 100, 86, 300, 24, 15, Ui.GOLD, TextAnchor.MiddleLeft);
        // уақыт пен раунд
        Ui.Panel(596, 6, 88, 70, new Color(0.08f, 0.05f, 0.14f, 0.9f), Ui.GOLD);
        Ui.TextS(U.Int(Mathf.CeilToInt(m.timer)), 596, 6, 88, 56, 34, Ui.CREAM, TextAnchor.MiddleCenter);
        for (int i = 0; i < m.needWins; i++)
        {
            Ui.Fill(560 - i * 22, 52, 14, 14, i < m.pWins ? Ui.GOLD : new Color(0, 0, 0, 0.5f));
            Ui.Fill(706 + i * 22, 52, 14, 14, i < m.eWins ? new Color(0.86f, 0.2f, 0.16f) : new Color(0, 0, 0, 0.5f));
        }
        // сөздер
        for (int i = 0; i < m.says.Count(); i++)
        {
            SayLine s = m.says.Get(i);
            if (m.sayClock < s.t0 || m.sayClock > s.t1) continue;
            float x = s.enemy ? 700 : 80;
            Ui.Panel(x, 150, 500, 96, new Color(0.96f, 0.9f, 0.76f, 0.96f), s.enemy ? new Color(0.86f, 0.2f, 0.16f) : (Color)Ui.GOLD);
            Ui.Text(s.name, x + 16, 156, 470, 26, 16, new Color32(125, 23, 25, 255), TextAnchor.MiddleLeft, true);
            Ui.Text("«" + s.text + "»", x + 16, 182, 470, 60, 18, Ui.INK);
        }
        // фаза жазулары
        if (m.phase == Match.INTRO && m.pt > m.introT - 1.2f)
            Ui.TextS(m.pt > m.introT - 0.5f ? "ҰРЫС!" : (m.needWins == 3 && m.round == 1 ? "БАС ЖАУ · " : "") + m.round + "-РАУНД", 0, 280, 1280, 100, 64, Ui.GOLD, TextAnchor.MiddleCenter);
        if (m.phase == Match.ROUNDEND)
        {
            Ui.TextS(m.roundMsg, 0, 250, 1280, 90, 60, Ui.GOLD, TextAnchor.MiddleCenter);
            Ui.TextS(m.roundWinner, 0, 340, 1280, 40, 26, Ui.CREAM, TextAnchor.MiddleCenter);
        }
        if (m.phase == Match.MATCHEND && m.pt > 0.8f) { DrawResult(); return; }
        // үзіліс
        if (m.phase != Match.MATCHEND && Ui.Btn("II", 1214, 92, 52, 48, false, true, 20)) m.paused = true;
        if (m.paused) { DrawPause(); return; }
        if (showTouch) DrawTouch();
    }

    void DrawTouch()
    {
        Btn(TL, "◀"); Btn(TR, "▶"); Btn(TU, "▲"); Btn(TD, "▼");
        Btn(TJ, "J\nқару"); Btn(TK, "K\nтебу"); Btn(TX, "L\nкүш");
    }
    void Btn(Rect r, string label)
    {
        bool on = T(r);
        Ui.Fill(r.x, r.y, r.width, r.height, new Color(1, 1, 1, on ? 0.35f : 0.16f));
        Ui.TextS(label, r.x, r.y, r.width, r.height, 22, new Color(1, 1, 1, 0.9f), TextAnchor.MiddleCenter);
    }

    void DrawPause()
    {
        Ui.Fill(0, 0, 1280, 720, new Color(0, 0, 0, 0.6f));
        Ui.Panel(440, 180, 400, 360, new Color(0.1f, 0.07f, 0.17f, 0.97f), Ui.GOLD);
        Ui.TextS("ҮЗІЛІС", 440, 196, 400, 60, 40, Ui.GOLD, TextAnchor.MiddleCenter);
        if (Ui.Btn("ЖАЛҒАСТЫРУ", 480, 280, 320, 60, true)) m.paused = false;
        if (Ui.Btn("ҚАЙТА БАСТАУ", 480, 354, 320, 56)) { m.paused = false; m.StartMatch(); Setup(m); }
        if (Ui.Btn("КАРТАҒА", 480, 424, 320, 56)) { m.paused = false; m.live = false; GameRoot.I.OpenMap(m.chapter); }
        Ui.Text("A/D — жүру · W — секіру · S — отыру · кері — қорғану\nJ — қару · K — тебу · L — күшті соққы", 440, 548, 400, 80, 14, Ui.CREAM, TextAnchor.UpperCenter);
    }

    void DrawResult()
    {
        bool won = m.won;
        Ui.Fill(0, 0, 1280, 720, new Color(0, 0, 0, 0.55f));
        Ui.Panel(330, 110, 620, 500, new Color(0.96f, 0.88f, 0.7f, 0.97f), won ? (Color)Ui.GOLD : new Color(0.6f, 0.2f, 0.2f));
        Ui.Text(won ? "ЖЕҢІС!" : "ЖЕҢІЛІС", 330, 126, 620, 70, 54, won ? new Color32(125, 23, 25, 255) : new Color32(70, 40, 40, 255), TextAnchor.MiddleCenter, true);
        float y = 200;
        if (won && m.mode == "campaign")
        {
            string st = ""; for (int k = 0; k < 3; k++) st += (m.starsGot & (1 << k)) != 0 ? "★ " : "☆ ";
            Ui.Text(st, 330, y, 620, 50, 40, new Color32(200, 140, 20, 255), TextAnchor.MiddleCenter, true); y += 54;
            Ui.Text(m.zhWin, 360, y, 560, 60, 19, Ui.INK, TextAnchor.MiddleCenter); y += 62;
        }
        if (m.reward > 0) { Ui.Text("+" + m.reward + " теңге", 330, y, 620, 40, 26, Ui.INK, TextAnchor.MiddleCenter, true); y += 42; }
        Ui.Text("+" + m.xpGot + " тәжірибе" + (m.lvlCoins > 0 ? " · ЖАҢА ДЕҢГЕЙ! +" + m.lvlCoins + " теңге" : ""), 330, y, 620, 32, 19, Ui.INK, TextAnchor.MiddleCenter); y += 36;
        if (U.Len(m.runMsg) > 0) Ui.Text(m.runMsg, 330, y, 620, 32, 18, Ui.INK, TextAnchor.MiddleCenter);
        if (!won) Ui.Text("Ештеңе етпейді! Қорғанып (кері басып), сосын соқ.", 360, 330, 560, 60, 19, Ui.INK, TextAnchor.MiddleCenter);
        bool camp = m.mode == "campaign";
        if (won && camp && Ui.Btn(m.stageIdx == 3 ? "ТАРАУ СОҢЫ ▶" : "КЕЛЕСІ ҰРЫС ▶", 480, 440, 320, 64, true, true, 24)) GameRoot.I.AfterFight("next");
        if (Ui.Btn("ҚАЙТА", 380, 524, 240, 56, !won || !camp)) GameRoot.I.AfterFight("retry");
        if (Ui.Btn("КАРТАҒА", 660, 524, 240, 56)) GameRoot.I.AfterFight("map");
    }
}
