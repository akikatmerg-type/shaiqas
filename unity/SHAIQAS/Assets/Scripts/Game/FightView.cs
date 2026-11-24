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
    Mesh meshP, meshE, meshFx, meshAdd;
    MeshRenderer rP, rE, rFx, rAdd;
    readonly VMesh vm = new VMesh(), vfx = new VMesh(), vadd = new VMesh();
    Material matAdd;
    // фон спрайттары үшін (FighterAtlas uv2-ні оқиды, ал SpriteRenderer оны бермейді → бәрі ағарып кететін)
    public Material matSpr, matSprAdd;
    StageLive live;
    public float ParK(int i) { return parK[i]; }
    readonly BgWar bgw = new BgWar(); readonly VMesh vbg = new VMesh(); Mesh meshBg;
    int dustCol = Col.Pack(160, 130, 100, 150);
    readonly Intent intent = new Intent();
    bool showTouch;
    float lastL = -9, lastR = -9;
    bool prevTL, prevTR, prevTU, prevTJ, prevTK, prevTX;
    int rimCol; float lightX = 1080;

    // ---------------- атлас ----------------
    static Texture2D atlas; static Rect[] rects; static Rect whiteRect; static int packed = -1;

    void Awake()
    {
        // Ассет-материалды өзгертпейміз: ойын өз көшірмесімен жұмыс істейді және араластыруды анық қояды
        // (редакторда ойын кезінде ассетке жазылған өзгеріс сақталып қалады — бәрі «ағарып» кетеді).
        Material src = Resources.Load<Material>("Materials/FighterAtlas");
        if (src != null) mat = new Material(src);
        else
        {
            Shader sh = Shader.Find("Shaiqas/FighterAtlas");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            mat = new Material(sh);
        }
        mat.SetFloat("_SrcBlend", 5); mat.SetFloat("_DstBlend", 10);
        Shader ss = Resources.Load<Shader>("Shaders/ShaiqasSprite");
        if (ss == null || !ss.isSupported) ss = Shader.Find("Sprites/Default");
        matSpr = new Material(ss); matSprAdd = new Material(ss);
        matSprAdd.SetFloat("_SrcBlend", 5); matSprAdd.SetFloat("_DstBlend", 1);
        matAdd = new Material(mat);
        matAdd.SetFloat("_SrcBlend", 5); matAdd.SetFloat("_DstBlend", 1);
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
        mat.mainTexture = atlas; if (matAdd != null) matAdd.mainTexture = atlas;
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
        string dc = meta.S("dustCol"); if (U.Len(dc) > 0) dustCol = Col.Parse(dc + "0.6)");
        ghosts.Clear();
        string b = "Art/stages/" + stageId + "/";
        Spr(b + "sky", 0, 0, StageLive.Order("sky"), world.transform);
        par0 = Spr(b + "layer0", 0, 0, StageLive.Order("layer0"), world.transform);
        par1 = Spr(b + "layer1", 0, 0, StageLive.Order("layer1"), world.transform);
        Transform fogMidT = Spr(b + "fogMid", 0, 420, StageLive.Order("fogMid"), world.transform);
        par2 = Spr(b + "layer2", 0, 0, StageLive.Order("layer2"), world.transform);
        Transform fogLowT = Spr(b + "fogLow", 0, 540, StageLive.Order("fogLow"), world.transform);
        actors = new GameObject("Actors").transform; actors.SetParent(world.transform, false);
        groundT = Spr(b + "ground", 0, 0, StageLive.Order("ground"), actors);
        live = new StageLive(); live.Setup(this, world.transform, stageId, mat, matAdd); live.SetFogs(fogMidT.gameObject, fogLowT.gameObject); live.dbgL2 = par2; live.dbgL1 = par1; live.dbgL0 = par0; live.dbgGround = groundT;
        {
            PostFx pf = GameRoot.I.cam.GetComponent<PostFx>(); if (pf == null) pf = GameRoot.I.cam.gameObject.AddComponent<PostFx>();
            pf.SetStage(stageId);
        }
        bgw.Build(m.chapter);
        {
            GameObject go = new GameObject("BgWar"); go.transform.SetParent(world.transform, false); live.dbgBgw = go.transform;
            meshBg = new Mesh(); meshBg.MarkDynamic(); go.AddComponent<MeshFilter>().sharedMesh = meshBg;
            MeshRenderer mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.sortingOrder = StageLive.Order("bgw");
        }
        meshFx = NewMesh("FxBack", StageLive.Order("fxBack"), out rFx);
        meshAdd = NewMesh("FxGlow", StageLive.Order("fxGlow"), out rAdd); rAdd.sharedMaterial = matAdd;
        meshE = NewMesh("Enemy", StageLive.Order("enemy"), out rE);
        meshP = NewMesh("Player", StageLive.Order("player"), out rP);
        prts.Clear();
        Looks.For(m.player); Looks.For(m.enemy);
        EnsureAtlas();
        Snd.Play("round_start");
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
            sr.sharedMaterial = matSpr;
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
        PostFx.on = false;
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
        int ph0 = m.phase, tc0 = Mathf.CeilToInt(m.timer), r0 = m.round;
        if (!m.paused && !Tut.Active()) m.Update(dt, intent);
        PhaseSounds(ph0, tc0, r0);
        FighterSounds(m.player, sP); FighterSounds(m.enemy, sE);
        EatFx();
        StepParticles(m.paused || Tut.Active() ? 0 : dt);
        // камера: зум (W/2, pivY) нүктесінің айналасында, діріл
        float z = m.zoom, pv = m.pivY;
        float sx = m.shake > 0 ? (U.Rand() - 0.5f) * m.shake * 1.6f : 0, sy = m.shake > 0 ? (U.Rand() - 0.5f) * m.shake * 1.6f : 0;
        Camera c = GameRoot.I.cam;
        z *= 1 + m.punch * 1.4f;
        c.orthographicSize = 360f / z;
        c.transform.position = new Vector3(640 - sx / z, -(pv + (360 - pv) / z) + sy / z, -100);
        par0.localPosition = new Vector3(-m.cam * parK[0], 0, 0);
        par1.localPosition = new Vector3(-m.cam * parK[1], 0, 0);
        par2.localPosition = new Vector3(-m.cam * parK[2], 0, 0);
        actors.localPosition = new Vector3(-m.cam, 0, 0);
        // фондағы шайқас (экран координатында, параллакс)
        vbg.Clear(); bgw.Draw(vbg, m.cam, Time.time); Upload(meshBg, vbg);
        if (live != null) live.Tick(m.cam, Time.time, c, z);
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
    public void Upload(Mesh mesh, VMesh v)
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

    // ---------------- дыбыстар (веб: skPost, endRound, endMatch) ----------------
    class SndSt { public bool og = true; public int st; public float stT, stepT, rux; public Move cm; }
    readonly SndSt sP = new SndSt(), sE = new SndSt();
    void FighterSounds(Fighter f, SndSt T)
    {
        bool og = f.OnGround(); int st = f.state; float pan = Snd.Pan(f.x, m.cam);
        if (T.og && !og && f.vy < -200 && st == Fighter.JUMP) Snd.Play("jump", 0.8f, pan);
        if (!T.og && og && st != Fighter.DOWN && st != Fighter.DEAD) Snd.Play("land", 0.7f, pan);
        if (st == Fighter.DASH && T.st != Fighter.DASH) Snd.Play("dash", 1, pan);
        if ((st == Fighter.DOWN || st == Fighter.DEAD) && T.stT < 0.42f && f.st >= 0.42f && T.st == st) Snd.Play("body_fall", 1, pan);
        if (st == Fighter.WALK && og) { T.stepT -= Time.deltaTime * (f.speed / 300); if (T.stepT <= 0) { T.stepT = 0.34f; Snd.Play("step", 0.35f, pan); } } else T.stepT = 0.12f;
        if (st == Fighter.ATTACK && f.cm != null && T.cm != f.cm && f.st >= f.cm.w * 0.55f)
        {
            T.cm = f.cm; Move mv = f.cm; string w = f.weapon;
            string n = mv.foot ? "kick" : mv.thr ? "swing_heavy" : (mv.heavy || mv.special || w == "shoqpar" || w == "kurzi" || w == "pernach" || w == "balta" || w == "temirshoq") ? "swing_heavy" : (w == "naiza" || w == "sungi") ? "swing_spear" : "swing_light";
            Snd.Play(n, 1, pan, f.big ? 0.9f : 1);
        }
        if (!f.ai && T.rux < 100 && f.rux >= 100 && m.phase == Match.FIGHT) Snd.Play("spirit_full", 1, pan);
        T.rux = f.rux; T.og = og; T.st = st; T.stT = f.st;
    }
    void PhaseSounds(int ph0, int tc0, int r0)
    {
        if (ph0 != Match.INTRO && m.phase == Match.INTRO) Snd.Play("round_start");
        if (ph0 == Match.INTRO && m.phase == Match.FIGHT) Snd.Play("fight_start");
        if (ph0 == Match.FIGHT && m.phase == Match.FIGHT) { int tc = Mathf.CeilToInt(m.timer); if (tc < tc0 && tc >= 1 && tc <= 5) Snd.Play("countdown"); }
        if (ph0 == Match.FIGHT && m.phase == Match.ROUNDEND) Snd.Play("round_bell");
        if (ph0 != Match.MATCHEND && m.phase == Match.MATCHEND) { Snd.Play(m.won ? "victory" : "defeat"); if (m.reward > 0) Snd.Play("coin", 1, 0, 1); }
    }

    // ---------------- әсерлер (веб: fxImpact, fxFlash, fxRing, fxP, fxPuff, drawTrail, ghosts) ----------------
    const int P_HOT = 0, P_GLINT = 1, P_PUFF = 2, P_CHIP = 3, P_FLASH = 4, P_RING = 5, P_EMBER = 6;
    class Prt { public float x, y, vx, vy, life, max, size, g, rot, vr, r1; public int col, kind; }
    readonly List<Prt> prts = new List<Prt>();
    class Ghost { public Fighter f; public float[] pose = new float[Pz.N]; public float x, y, life, max; public int facing, col; }
    readonly List<Ghost> ghosts = new List<Ghost>();
    float lastHitT = -9;
    static int C(int r, int g, int b) { return Col.Pack(r, g, b, 255); }
    static float Rn(float a, float b) { return a + U.Rand() * (b - a); }
    void P(int kind, float x, float y, float vx, float vy, float life, int col, float size, float g) { if (prts.Count < 600) prts.Add(new Prt { kind = kind, x = x, y = y, vx = vx, vy = vy, life = life, max = life, col = col, size = size, g = g, rot = Rn(0, 6.28f), vr = Rn(-8, 8) }); }
    void Flash(float x, float y, float r, int col, float a, float life) { P(P_FLASH, x, y, 0, 0, life, Col.WithA(col, a), r, 0); }
    void RingFx(float x, float y, float r0, float speed, float life, int col, float w) { Prt p = new Prt { kind = P_RING, x = x, y = y, life = life, max = life, col = col, size = w, r1 = r0, vx = speed }; prts.Add(p); }

    void EatFx()
    {
        for (int i = 0; i < m.fx.Count(); i++)
        {
            FxEv e = m.fx.Get(i);
            float pan = Snd.Pan(e.x, m.cam);
            if (e.kind == Match.FX_HIT || e.kind == Match.FX_HITSPEC) Snd.Play((e.flags & 4) != 0 ? "hit_heavy" : (e.flags & 2) != 0 ? "hit_crit" : (e.flags & 1) != 0 ? "hit_heavy" : "hit_light", 1, pan);
            else if (e.kind == Match.FX_BLOCK) Snd.Play((e.flags & 2) != 0 ? "guard_break" : "block", (e.flags & 1) != 0 ? 1 : 0.8f, pan);
            else if (e.kind == Match.FX_KO) Snd.Play("ko");
            else if (e.kind == Match.FX_SPECIAL) Snd.Play("special");
            else if (e.kind == Match.FX_PHASE) Snd.Play("roar");
            switch (e.kind)
            {
                case Match.FX_HIT: Impact(e, e.n >= 20 ? 1 : 0); break;
                case Match.FX_HITSPEC: Impact(e, 2); break;
                case Match.FX_BLOCK: Block(e); break;
                case Match.FX_DUST: Dust(e.x, e.y, e.dir, e.n); break;
                case Match.FX_GHOST: AddGhost(e.who, 0.22f, e.who.ai ? Col.Parse(e.who.accent) : C(255, 200, 110)); break;
                case Match.FX_KO:
                    Flash(e.x, e.y, 360, C(255, 106, 58), 0.55f, 0.35f); Flash(e.x, e.y, 140, C(255, 255, 255), 0.4f, 0.25f);
                    RingFx(e.x, e.y, 20, 1400, 0.5f, C(255, 255, 255), 6);
                    for (int k = 0; k < 30; k++) { float a = Rn(0, 6.28f), sp = Rn(300, 900); P(P_HOT, e.x, e.y, Mathf.Cos(a) * sp, Mathf.Sin(a) * sp - 150, Rn(0.25f, 0.5f), k % 3 == 0 ? C(255, 246, 224) : C(255, 120, 60), Rn(2, 4), 1300); }
                    break;
                case Match.FX_SPECIAL:
                    {
                        int col = e.who != null && e.who.ai ? Col.Parse(e.who.accent) : C(255, 211, 107);
                        RingFx(e.x, e.y, 30, 900, 0.35f, col, 5); RingFx(e.x, Db.GROUND - 2, 30, 700, 0.5f, col, 4);
                        Flash(e.x, e.y, 300, col, 0.35f, 0.4f); Flash(e.x, e.y, 160, C(255, 255, 255), 0.25f, 0.3f);
                        for (int k = 0; k < 30; k++) P(P_EMBER, e.x + Rn(-46, 46), e.y + 160 - Rn(0, 230), Rn(-25, 25), Rn(-240, -90), Rn(0.35f, 0.7f), col, Rn(7, 15), -60);
                        AddGhost(e.who, 0.3f, col);
                        break;
                    }
                case Match.FX_PHASE:
                    Flash(e.x, e.y, 320, C(255, 90, 58), 0.4f, 0.5f);
                    for (int k = 0; k < 40; k++) P(P_EMBER, e.x + Rn(-46, 46), e.y + 120 - Rn(0, 230), Rn(-25, 25), Rn(-240, -90), Rn(0.35f, 0.7f), C(255, 90, 58), Rn(7, 15), -60);
                    break;
            }
        }
        m.fx.Clear();
        // рух толы немесе екінші фаза: айналадағы ұшқындар (веб: aura)
        foreach (Fighter f in new[] { m.player, m.enemy })
        {
            if (f.phase2 && U.Rand() < 0.5f) P(P_EMBER, f.x + Rn(-46, 46), f.y - Rn(0, 230), Rn(-25, 25), Rn(-240, -90), Rn(0.35f, 0.7f), C(255, 90, 58), Rn(7, 15), -60);
            if (f.state == Fighter.ATTACK && f.cm != null && f.cm.special && f.st < f.cm.w + f.cm.a && U.Rand() < 0.45f) P(P_EMBER, f.x + Rn(-46, 46), f.y - Rn(0, 230), Rn(-25, 25), Rn(-240, -90), Rn(0.35f, 0.7f), C(255, 211, 107), Rn(7, 15), -60);
        }
    }

    /// <summary>Соққы: 0 — жеңіл, 1 — ауыр, 2 — ерекше (веб: fxImpact).</summary>
    void Impact(FxEv e, int kind)
    {
        float x = e.x, y = e.y, ad = -e.dir == 0 ? 1 : -e.dir;
        float rp = Time.time - lastHitT < 0.15f ? 0.55f : 1; lastHitT = Time.time;
        bool big = kind > 0, sp = kind == 2;
        int col = sp ? C(255, 224, 138) : C(255, 178, 74);
        Flash(x, y, (big ? 190 : 120) * rp, col, (big ? 0.22f : 0.15f) * rp, 0.22f);
        Flash(x, y, (big ? 66 : 46) * rp, C(255, 255, 255), big ? 0.12f : 0.08f, 0.16f);
        Flash(x, y, (big ? 140 : 90) * rp, sp ? C(255, 244, 208) : C(255, 255, 255), (big ? 0.16f : 0.1f) * rp, 0.12f);
        RingFx(x, y, 12, big ? 980 : 620, big ? 0.26f : 0.18f, sp ? col : C(255, 230, 176), big ? 5 : 3);
        if (big) RingFx(x, y, 4, 560, 0.34f, C(255, 255, 255), 2);
        float power = big ? 780 : 520;
        int n = big ? 26 : 14;
        for (int i = 0; i < n; i++)
        {
            bool back = U.Rand() < 0.25f; float a = Rn(-0.85f, 0.85f) + (ad > 0 ? 0 : Mathf.PI) + (back ? Mathf.PI : 0), s = Rn(0.4f, 1) * power * (back ? 0.6f : 1.25f);
            P(P_HOT, x, y, Mathf.Cos(a) * s, Mathf.Sin(a) * s - Rn(40, 200), Rn(0.16f, 0.4f), sp ? (i % 2 == 1 ? col : C(255, 241, 192)) : (i % 3 != 0 ? C(255, 207, 107) : C(255, 246, 220)), Rn(1.6f, 3.6f), 1500);
        }
        for (int i = 0; i < (big ? 4 : 2); i++) P(P_GLINT, x + Rn(-30, 30), y + Rn(-30, 30), ad * Rn(40, 200), Rn(-120, 20), Rn(0.14f, 0.28f), sp ? C(255, 244, 208) : C(255, 248, 234), Rn(16, 30), 0);
        Fighter atk = e.who != null ? e.who.opp : null;
        bool wood = atk != null && (atk.weapon == "soyil" || atk.weapon == "shoqpar" || atk.weapon == "staff" || atk.weapon == "kurzi");
        if (wood) for (int i = 0; i < (big ? 9 : 4); i++) P(P_CHIP, x, y, ad * Rn(100, 420), Rn(-420, -120), Rn(0.4f, 0.8f), i % 2 == 0 ? C(176, 122, 69) : C(122, 74, 34), Rn(3, 6), 1600);
        if (big && e.who != null) Dust(e.who.x, Db.GROUND, ad, 5);
        m.punch = Mathf.Max(m.punch, sp ? 0.075f : big ? 0.055f : 0.022f);
    }
    void Block(FxEv e)
    {
        float x = e.x, y = e.y, dir = e.dir;
        Flash(x, y, 86, C(159, 216, 255), 0.16f, 0.2f); Flash(x, y, 34, C(255, 255, 255), 0.1f, 0.14f); Flash(x, y, 70, C(234, 246, 255), 0.12f, 0.16f);
        for (int i = 0; i < 18; i++)
        {
            float a = Rn(-1.25f, 1.0f) * (U.Rand() < 0.5f ? 1 : 0.6f) - 0.35f + (dir > 0 ? 0 : Mathf.PI), sp = Rn(0.5f, 1.25f) * 700;
            P(P_HOT, x, y, Mathf.Cos(a) * sp, Mathf.Sin(a) * sp - Rn(60, 240), Rn(0.1f, 0.28f), i % 3 == 0 ? C(255, 255, 255) : i % 3 == 1 ? C(191, 228, 255) : C(255, 231, 168), Rn(1.1f, 2.3f), 1900);
        }
        for (int i = 0; i < 3; i++) P(P_GLINT, x + Rn(-18, 18), y + Rn(-18, 18), Rn(-60, 60), Rn(-80, 20), Rn(0.12f, 0.22f), C(232, 246, 255), Rn(14, 26), 0);
        RingFx(x, y, 8, 520, 0.16f, C(207, 234, 255), 2.5f);
        m.punch = Mathf.Max(m.punch, 0.012f);
    }
    void Dust(float x, float y, float dir, int n)
    {
        for (int i = 0; i < n; i++)
        {
            float vx = dir != 0 ? dir * Rn(60, 240) + Rn(-50, 50) : (i % 2 == 1 ? 1 : -1) * Rn(90, 260);
            P(P_PUFF, x + Rn(-20, 20), Mathf.Min(y, Db.GROUND) - Rn(0, 10), vx, Rn(-90, -15), Rn(0.45f, 0.8f), dustCol, Rn(9, 18), 0);
        }
        if (n >= 6 && y >= Db.GROUND - 2) for (int i = 0; i < (n >= 12 ? 6 : 2); i++) P(P_CHIP, x + Rn(-20, 20), Db.GROUND - 2, Rn(-220, 220), Rn(-420, -160), Rn(0.4f, 0.7f), C(106, 82, 64), Rn(2, 4), 1800);
        if (dir == 0 && n >= 8) RingFx(x, Db.GROUND - 2, 16, n >= 12 ? 620 : 380, 0.3f, Col.Pack(255, 240, 220, 160), 2.5f);
    }
    void AddGhost(Fighter f, float life, int col)
    {
        if (f == null || ghosts.Count > 24) return;
        Ghost g = new Ghost { f = f, x = f.x, y = f.y, life = life, max = life, facing = f.facing, col = col };
        Pz.Copy(g.pose, f.cur); ghosts.Add(g);
    }
    void StepParticles(float dt)
    {
        for (int i = prts.Count - 1; i >= 0; i--)
        {
            Prt p = prts[i];
            p.life -= dt; p.vy += p.g * dt; p.x += p.vx * dt; p.y += p.vy * dt; p.rot += p.vr * dt;
            if (p.kind == P_PUFF) { p.vx *= 1 - 2.2f * dt; p.vy *= 1 - 1.5f * dt; p.size += 16 * dt; }
            if (p.kind == P_CHIP && p.y > Db.GROUND - 2) { p.y = Db.GROUND - 2; p.vy *= -0.3f; p.vx *= 0.6f; }
            if (p.kind == P_RING) p.r1 += p.vx * dt * (p.life / p.max);
            if (p.life <= 0) prts.RemoveAt(i);
        }
        for (int i = ghosts.Count - 1; i >= 0; i--) { ghosts[i].life -= dt; if (ghosts[i].life <= 0) ghosts.RemoveAt(i); }
    }
    static float[] lx = new float[2], ly = new float[2];
    void BuildFx()
    {
        // мөлдір қабат: көлеңкелер, шаң, жаңқалар, сағым (ghost)
        vfx.Clear();
        Shadow(m.enemy); Shadow(m.player);
        foreach (Prt p in prts)
        {
            float k = Mathf.Clamp01(p.life / p.max);
            if (p.kind == P_PUFF) { vfx.FillC(Col.WithA(p.col, k * k * Col.A(p.col) / 255f)); vfx.Circle(p.x, p.y, p.size); }
            else if (p.kind == P_CHIP) { vfx.FillC(Col.WithA(p.col, Mathf.Min(1, k * 2))); vfx.Save(); vfx.Translate(p.x, p.y); vfx.Rotate(p.rot); lx[0] = -p.size; lx[1] = p.size; ly[0] = 0; ly[1] = 0; vfx.Stroke(lx, ly, 2, p.size * 0.7f, false); vfx.Restore(); }
        }
        foreach (Ghost gh in ghosts)
        {
            Fighter f = gh.f; float[] keep = new float[Pz.N]; Pz.Copy(keep, f.cur); float kx = f.x, ky = f.y; int kf = f.facing;
            Pz.Copy(f.cur, gh.pose); f.x = gh.x; f.y = gh.y; f.facing = gh.facing;
            Painter.Draw(vfx, f, Looks.For(f), Col.WithA(gh.col, Mathf.Clamp01(gh.life / gh.max) * 0.45f), 0, 0);
            Pz.Copy(f.cur, keep); f.x = kx; f.y = ky; f.facing = kf;
        }
        Upload(meshFx, vfx);
        // жарық қабаты (additive): жарқыл, сақина, ұшқын, жылт, қару ізі
        vadd.Clear();
        Trail(m.enemy); Trail(m.player);
        foreach (Prt p in prts)
        {
            float k = Mathf.Clamp01(p.life / p.max);
            switch (p.kind)
            {
                case P_FLASH: vadd.Glow(p.x, p.y, p.size * (1 + (1 - k) * 0.4f), Col.WithA(p.col, Col.A(p.col) / 255f * k * 2.2f)); break;
                case P_RING: vadd.FillC(Col.WithA(p.col, k * Col.A(p.col) / 255f)); vadd.Ring(p.x, p.y, p.r1, p.size * k + 0.5f); break;
                case P_HOT: vadd.FillC(Col.WithA(p.col, k)); lx[0] = p.x; ly[0] = p.y; lx[1] = p.x - p.vx * 0.022f; ly[1] = p.y - p.vy * 0.022f; vadd.Stroke(lx, ly, 2, p.size, true); break;
                case P_GLINT: vadd.FillC(Col.WithA(p.col, k)); vadd.Star(p.x, p.y, p.size * (0.5f + k * 0.5f), p.rot * 0.2f); break;
                case P_EMBER: vadd.Glow(p.x, p.y, p.size, Col.WithA(p.col, k * 0.8f)); break;
            }
        }
        Upload(meshAdd, vadd);
    }
    void Trail(Fighter f)
    {
        int n = f.trail.Count(); if (n < 2) return;
        Move cm = f.state == Fighter.ATTACK ? f.cm : null;
        int tint = cm != null && cm.special ? C(255, 224, 150) : cm != null && cm.foot ? C(255, 236, 214) : (f.weapon == "qylysh" || f.weapon == "aibalta" || f.weapon == "naiza" || f.weapon == "semser" || f.weapon == "bulat" || f.weapon == "sungi") ? C(196, 228, 255) : C(255, 206, 140);
        for (int i = 1; i < n; i++)
        {
            float[] a = f.trail.Get(i - 1), b = f.trail.Get(i); if (a[6] != b[6]) continue;
            float fa = Mathf.Clamp01(a[4] / a[5]), fb = Mathf.Clamp01(b[4] / b[5]);
            float ia = 0.2f + 0.8f * fa * fa, ib = 0.2f + 0.8f * fb * fb;
            vadd.FillC(Col.WithA(tint, fb * (b[7] > 0 ? 0.45f : 0.3f)));
            vadd.Quad(a[0], a[1], b[0], b[1], Mathf.Lerp(b[0], b[2], ib), Mathf.Lerp(b[1], b[3], ib), Mathf.Lerp(a[0], a[2], ia), Mathf.Lerp(a[1], a[3], ia));
        }
        for (int i = 1; i < n; i++)
        {
            float[] a = f.trail.Get(i - 1), b = f.trail.Get(i); if (a[6] != b[6]) continue;
            float fb = Mathf.Clamp01(b[4] / b[5]);
            vadd.FillC(Col.WithA(C(255, 250, 240), fb * 0.75f)); lx[0] = a[0]; ly[0] = a[1]; lx[1] = b[0]; ly[1] = b[1];
            vadd.Stroke(lx, ly, 2, 0.8f + 3.2f * fb, true);
        }
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
        float hk = m.phase == Match.INTRO && m.round == 1 ? Mathf.Clamp01(m.pt * 2.4f) : 1; hk = 1 - (1 - hk) * (1 - hk) * (1 - hk);
        float oy = -(1 - hk) * 120;
        HpBar(P, false, oy); HpBar(E, true, oy);
        Ui.Medal(66, 58 + oy, 42, "2f5f9a"); Ui.Medal(1214, 58 + oy, 42, "a32222");
        Ui.Portrait(P.heroId != null ? "Art/portraits/" + P.heroId : "Art/ui/por_trainer", 66, 58 + oy, 42);
        string ep = m.mode == "train" ? "Art/ui/por_trainer" : E.look.StartsWith("e_") ? "Art/ui/por_" + E.look : "Art/ui/por_trainer";
        Ui.Portrait(ep, 1214, 58 + oy, 42);
        Ui.TextO(P.name, 128, 68 + oy, 400, 24, 17, new Color32(255, 243, 224, 255), TextAnchor.MiddleLeft, true, 1.5f);
        Ui.TextO(E.name, 752, 68 + oy, 400, 24, 17, new Color32(255, 243, 224, 255), TextAnchor.MiddleRight, true, 1.5f);
        // таймер медальоны
        bool low = m.timer < 10 && m.phase == Match.FIGHT;
        Ui.Medal(640, 50 + oy, 36, "a32222");
        Ui.Fill(640 - 26, 50 - 26 + oy, 52, 52, low ? new Color32(74, 14, 14, 235) : new Color32(30, 25, 64, 235));
        float ts = low ? 1 + 0.1f * Mathf.Max(0, Mathf.Sin(Time.time * 6)) : 1;
        Ui.TextO(U.Int(Mathf.CeilToInt(m.timer)), 600, 26 + oy, 80, 50, (int)(38 * ts), low ? new Color32(255, 138, 106, 255) : new Color32(255, 214, 120, 255));
        if (m.needWins > 1) Ui.TextO(m.round + "-РАУНД", 560, 96 + oy, 160, 20, 14, new Color32(255, 227, 160, 255), TextAnchor.MiddleCenter, true, 1.5f);
        // жеңіс белгілері (раунд)
        for (int i = 0; i < m.needWins; i++)
        {
            Ui.Img("Art/ui/medal_18", 560 - i * 26 - 15, 70 + oy + 5, 30, 30, i < m.pWins ? 1 : 0.25f);
            Ui.Img("Art/ui/medal_18", 720 + i * 26 - 15, 70 + oy + 5, 30, 30, i < m.eWins ? 1 : 0.25f);
        }
        if (P.rux >= 100) Ui.TextO("L — РУХ СОҚҚЫСЫ!", 128, 92 + oy, 300, 22, 15, Ui.GOLD, TextAnchor.MiddleLeft, true, 1.5f);
        // сөздер (пергамент көпіршіктер)
        for (int i = 0; i < m.says.Count(); i++)
        {
            SayLine sl = m.says.Get(i);
            if (m.sayClock < sl.t0 || m.sayClock > sl.t1) continue;
            float bx = sl.enemy ? 700 : 90, k2 = Mathf.Clamp01((m.sayClock - sl.t0) * 6);
            Ui.DzPanel(bx, 150, 490, 84, "parch");
            Ui.Text(sl.name, bx + 18, 156, 454, 22, 15, new Color32(125, 23, 25, 255), TextAnchor.MiddleLeft, true);
            Ui.Text(sl.text, bx + 18, 178, 454, 52, Ui.FitBox(sl.text, 454, 52, 18), new Color(Ui.INK.r, Ui.INK.g, Ui.INK.b, k2), TextAnchor.UpperLeft, true);
        }
        // кіріспе: «n-РАУНД» + бөлгіш + сипаттама (веб)
        if (m.phase == Match.INTRO)
        {
            float k = Mathf.Clamp01(m.pt * 2.5f), k2 = Mathf.Clamp01(m.pt * 2.5f - 0.5f);
            string ttl = m.needWins > 1 ? m.round + "-РАУНД" : "ҰРЫСҚА ДАЙЫН БОЛ";
            int sz = (int)((m.needWins > 1 ? 82 : 64) * (1.25f - 0.25f * k));
            Ui.TextO(ttl, 0, 236 - sz * 0.75f, 1280, sz * 1.1f, sz, new Color(1, 0.84f, 0.47f, k), TextAnchor.MiddleCenter, true, 4);
            float hw = 230 * k2;
            Ui.Fill(640 - hw, 262, hw * 2, 2.5f, new Color(0.95f, 0.72f, 0.25f, k2));
            Ui.Img("Art/ui/ram", 620, 240, 40, 20, k2);
            JNode C = Db.Ch(m.chapter);
            string sub = m.mode == "campaign" ? C.S("name") + " · " + (m.stageIdx + 1) + "-ұрыс" + (E.boss ? " · БАС ЖАУ" : "") : (m.mode == "train" ? "Жаттығу" : "Еркін ұрыс") + " · " + C.S("name");
            Ui.TextO(sub, 0, 282, 1280, 40, 25, new Color(1, 0.83f, 0.42f, k2), TextAnchor.MiddleCenter, true, 3);
            Ui.TextO(P.name + "   VS   " + E.name, 0, 318, 1280, 30, 18, new Color(1, 0.95f, 0.88f, k2), TextAnchor.MiddleCenter, true, 2);
        }
        if (m.phase == Match.INTRO && m.pt > m.introT - 0.5f)
            Ui.TextO("ҰРЫС!", 0, 330, 1280, 110, 90, Ui.GOLD, TextAnchor.MiddleCenter, true, 5);
        // фаза жазулары
        if (m.phase == Match.ROUNDEND)
        {
            Ui.TextO(m.roundMsg, 0, 230, 1280, 100, 72, new Color32(255, 214, 120, 255), TextAnchor.MiddleCenter, true, 4);
            Ui.Fill(410, 334, 460, 2.5f, Ui.GOLD); Ui.Img("Art/ui/ram", 620, 312, 40, 20);
            Ui.TextO(m.roundWinner, 0, 344, 1280, 40, 26, Ui.CREAM, TextAnchor.MiddleCenter, true, 3);
        }
        if (m.phase == Match.MATCHEND && m.pt > 0.8f) { DrawResult(); return; }
        // үзіліс
        if (m.phase != Match.MATCHEND && Ui.Btn("II", 1214, 92, 52, 48, false, true, 20)) m.paused = true;
        if (m.paused) { DrawPause(); return; }
        if (showTouch) DrawTouch();
    }

    /// <summary>Веб: bar() — денсаулық тақтасы, зақым ізі, бөліктер, рух ұяшықтары.</summary>
    void HpBar(Fighter f, bool right, float oy)
    {
        const float X = 128, Y = 24, Wd = 440, Hh = 26;
        float k = Mathf.Clamp01(f.hp / f.maxHp), kl = Mathf.Clamp01(f.hpLag / f.maxHp);
        if (right) Ui.ImgFlip("Art/ui/hudplate", 1280 - 620, oy, 620, 120); else Ui.Img("Art/ui/hudplate", 0, oy, 620, 120);
        float full = Wd + 9;
        float lx = right ? 1280 - X - full * kl : X, fx = right ? 1280 - X - full * k : X;
        if (kl > k) Ui.Fill(lx, Y + oy, full * kl, Hh, new Color(1, (236 - 40 * Mathf.Abs(Mathf.Sin(Time.time * 10))) / 255f, 0.78f, 0.95f));
        if (right) Ui.Grad3(fx, Y + oy, full * k, Hh, new Color32(255, 138, 100, 255), new Color32(224, 57, 42, 255), new Color32(122, 18, 16, 255));
        else Ui.Grad3(fx, Y + oy, full * k, Hh, new Color32(255, 240, 168, 255), new Color32(242, 182, 60, 255), new Color32(154, 90, 16, 255));
        if (k < 0.25f && k > 0) Ui.Fill(fx, Y + oy, full * k, Hh, new Color(1, 0.16f, 0.12f, 0.25f + 0.25f * Mathf.Sin(Time.time * 9)));
        Ui.Fill(fx, Y + 2 + oy, full * k, Hh * 0.3f, new Color(1, 1, 1, 0.3f));
        for (int i = 1; i < 10; i++) Ui.Fill(right ? 1280 - X - Wd * i / 10f : X + Wd * i / 10f, Y + oy, 1.5f, Hh, new Color(0, 0, 0, 0.28f));
        if (k > 0) Ui.Fill(right ? fx : X + full * k - 2, Y + oy, 2, Hh, new Color(1, 1, 0.94f, 0.9f));
        // рух: 4 ұяшық
        float rk = Mathf.Clamp01(f.rux / 100), rw = 236, cw = rw / 4, ry = Y + Hh + 7 + oy;
        bool fullR = rk >= 1;
        for (int i = 0; i < 4; i++)
        {
            float kk = Mathf.Clamp01(rk * 4 - i); if (kk <= 0) continue;
            float cx = right ? 1280 - X - (i + 1) * cw + (cw - 2) * (1 - kk) : X + i * cw;
            float pz = 0.7f + 0.3f * Mathf.Sin(Time.time * 8);
            Ui.Fill(cx + 1, ry, (cw - 2) * kk, 8, fullR ? new Color(1, (150 + 60 * pz) / 255f, 0.16f) : new Color32(95, 191, 230, 255));
            Ui.Fill(cx + 1, ry, (cw - 2) * kk, 3, fullR ? new Color32(255, 246, 200, 255) : new Color32(191, 239, 255, 255));
        }
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
