// SHAIQAS · «Тірі» фон (веб: liveSetup / drawStageBack / drawParticles / drawStageFront):
// жұлдыздар, күн, жылжитын бұлттар, күн сәулесі, тұман, тірі элементтер (тулар, түтін, от, құстар,
// бүркіт, салт аттылар, керуен, қайық, құйын, қамыс, шөп), ауа бөлшектері (шаң, қар, жапырақ, ұшқын),
// алдыңғы қатардағы бұлыңғыр заттар, бұрыштағы бұтақтар және түс беру (виньетка).
using UnityEngine;

public class StageLive
{
    const float W = 1280, H = 720;
    /// <summary>Тексеру: 1 пост, 2 grade, 3 gfog+bokeh, 4 mist, 5 rays+beams+күн, 6 fogMid/fogLow, 7 бөлшектер.</summary>
    public static int dbg; static bool dbgWas14;
    GameObject fogMidGo, fogLowGo;
    public void SetFogs(GameObject a, GameObject b) { fogMidGo = a; fogLowGo = b; }
    public Transform dbgL2, dbgBgw, dbgGround, dbgL1, dbgL0;
    static void Vis(Transform t, bool on) { if (t != null && t.gameObject.activeSelf != on) t.gameObject.SetActive(on); }
    void DebugVis()
    {
        PostFx.dbgOff = dbg == 1;
        if (grade != null) Vis(grade.t, dbg != 2);
        for (int i = 0; i < gfog.Length; i++) Vis(gfog[i].t, dbg != 3);
        if (mAmbGo != null) Vis(mAmbGo, dbg != 3);
        for (int i = 0; i < mist.Length; i++) Vis(mist[i].t, dbg != 4);
        if (rays != null) Vis(rays.t, dbg != 5); if (mBeamGo != null) Vis(mBeamGo, dbg != 5); if (mSkyAddGo != null) Vis(mSkyAddGo, dbg != 5);
        if (fogMidGo != null) Vis(fogMidGo.transform, dbg != 6); if (fogLowGo != null) Vis(fogLowGo.transform, dbg != 6);
        if (mPkGo != null) { Vis(mPkGo, dbg != 7); Vis(mPkAddGo, dbg != 7); }
        if (dbg == 14 || dbgWas14)
        {
            Renderer[] rs = scr.parent.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++) { string n = rs[i].gameObject.name; rs[i].enabled = dbg != 14 || n.EndsWith("/layer2") || n.EndsWith("/sky"); }
            PostFx.dbgOff = dbg == 14;
            dbgWas14 = dbg == 14;
        }
        Vis(dbgL2, dbg != 8); Vis(mLayGo2, dbg != 9); Vis(dbgBgw, dbg != 10); Vis(dbgGround, dbg != 11); Vis(dbgL1, dbg != 12); Vis(dbgL0, dbg != 13);
    }
    Transform mLayGo2, mAmbGo, mBeamGo, mSkyAddGo, mPkGo, mPkAddGo;
    FightView fv; Material mat, matAdd; JNode L; string pre;
    float wind = 1;
    // спрайттар
    class Spr { public Transform t; public SpriteRenderer r; public JNode o; public float w, h; }
    Spr[] clouds = new Spr[0], fgs = new Spr[0], corners = new Spr[0], mist = new Spr[0];
    Spr rays, grade; Spr[] gfog = new Spr[0];
    Transform scr; int stIdx;
    readonly VMesh vBeam = new VMesh(), vAmb = new VMesh(); Mesh mBeam, mAmb;
    static float[] ambX, ambY, ambD, ambPh, ambV;
    // торлар (mesh): аспан, 3 қабат (қалыпты + жарқын), бөлшектер, алдыңғы шөп
    readonly VMesh vSky = new VMesh(), vSkyAdd = new VMesh(), vPk = new VMesh(), vPkAdd = new VMesh(), vFront = new VMesh();
    readonly VMesh[] vLay = { new VMesh(), new VMesh(), new VMesh() }, vLayAdd = { new VMesh(), new VMesh(), new VMesh() };
    Mesh mSky, mSkyAdd, mPk, mPkAdd, mFront; readonly Mesh[] mLay = new Mesh[3], mLayAdd = new Mesh[3];
    float[] layW = { 2400, 2400, 2400 };
    // бөлшектер (веб: PK)
    static float[] pkx, pky, pkd, pkph, pkv;
    static float[] xs = new float[64], ys = new float[64], tx = new float[64], ty = new float[64], bx = new float[64], by = new float[64];

    public static int Order(string k)
    {
        switch (k)
        {
            case "sky": return 0; case "stars": return 1; case "clouds": return 2; case "skyItems": return 3;
            case "layer0": return 4; case "lay0": return 5; case "layer1": return 6; case "rays": return 7; case "lay1": return 8;
            case "fogMid": return 9; case "mist": return 10; case "layer2": return 11; case "lay2": return 12; case "fogLow": return 13;
            case "beams": return 14; case "bgw": return 15; case "ground": return 16; case "fxBack": return 20; case "enemy": return 21; case "player": return 22;
            case "fxGlow": return 30; case "pk": return 31; case "amb": return 32; case "fg": return 33; case "grass": return 34; case "corner": return 35; case "grade": return 36;
        }
        return 0;
    }

    public void Setup(FightView view, Transform world, string stageId, Material m, Material mAdd)
    {
        fv = view; mat = m; matAdd = mAdd;
        L = Db.J("meta_live").Get(stageId);
        pre = "Art/stages/" + stageId + "/live/";
        if (pkx == null)
        {
            System.Random r = new System.Random(4242); int n = 90;
            pkx = new float[n]; pky = new float[n]; pkd = new float[n]; pkph = new float[n]; pkv = new float[n];
            for (int i = 0; i < n; i++) { pkx[i] = (float)r.NextDouble() * W; pky[i] = (float)r.NextDouble() * H; pkd[i] = 0.2f + (float)r.NextDouble() * 0.8f; pkph[i] = (float)r.NextDouble() * 6.2832f; pkv[i] = (float)r.NextDouble(); }
        }
        if (L.IsNull()) return;
        wind = L.Get("wind").F(1);
        JNode lw = L.Get("layW"); for (int i = 0; i < 3 && i < lw.Count(); i++) layW[i] = lw.At(i).F();
        JNode cl = L.Get("clouds"); clouds = new Spr[cl.Count()];
        for (int i = 0; i < clouds.Length; i++) clouds[i] = MakeSpr(world, pre + "cloud" + i, cl.At(i), Order("clouds"), false);
        if (!L.Get("rays").IsNull()) rays = MakeSpr(world, pre + "rays", L.Get("rays"), Order("rays"), true);
        if (!L.Get("mist").IsNull())
        {
            mist = new Spr[3];
            for (int i = 0; i < 3; i++) mist[i] = MakeSpr(world, pre + "mist", L.Get("mist"), Order("mist"), false);
        }
        scr = new GameObject("LiveScreen").transform; scr.SetParent(world, false);
        stIdx = PostFx.Idx(stageId);
        JNode fg = L.Get("fg"); fgs = new Spr[fg.Count()];
        for (int i = 0; i < fgs.Length; i++) fgs[i] = MakeSpr(scr, pre + "fg" + i, fg.At(i), Order("fg"), false, true);
        JNode co = L.Get("corner"); corners = new Spr[co.Count()];
        for (int i = 0; i < corners.Length; i++) corners[i] = MakeSpr(scr, pre + "corner" + i, co.At(i), Order("corner"), false, false);
        JNode gd = JNode.NewObj(); gd.Set("w", JNode.Of(W)); gd.Set("h", JNode.Of(H));
        grade = MakeSpr(scr, pre + "grade", gd, Order("grade"), false);
        if (!L.Get("gfog").IsNull())
        {
            gfog = new Spr[4];
            for (int i = 0; i < 4; i++) gfog[i] = MakeSpr(scr, pre + "gfog", L.Get("gfog"), Order("amb"), false);
        }
        mSky = Mk(world, "LiveSky", Order("skyItems"), mat); mSkyAdd = Mk(world, "LiveSun", Order("stars"), matAdd);
        for (int i = 0; i < 3; i++) { mLay[i] = Mk(world, "LiveLay" + i, Order("lay" + i), mat); mLayAdd[i] = Mk(world, "LiveLayAdd" + i, Order("lay" + i), matAdd); }
        mPk = Mk(scr, "LivePk", Order("pk"), mat); mPkAdd = Mk(scr, "LivePkAdd", Order("pk"), matAdd);
        mFront = Mk(scr, "LiveGrass", Order("grass"), mat);
        mBeam = Mk(world, "LiveBeams", Order("beams"), matAdd); mAmb = Mk(scr, "LiveAmb", Order("amb"), matAdd);
        if (ambX == null)
        {
            System.Random r = new System.Random(911); ambX = new float[16]; ambY = new float[16]; ambD = new float[16]; ambPh = new float[16]; ambV = new float[16];
            for (int i = 0; i < 16; i++) { ambX[i] = (float)r.NextDouble() * 1700; ambY[i] = (float)r.NextDouble() * H; ambD[i] = (float)r.NextDouble(); ambPh[i] = (float)r.NextDouble() * 6.283f; ambV[i] = (float)r.NextDouble(); }
        }
    }

    Spr MakeSpr(Transform parent, string path, JNode o, int order, bool add, bool bottomPivot = false)
    {
        Spr sp = new Spr(); sp.o = o; sp.w = o.Get("w").F(1); sp.h = o.Get("h").F(1);
        GameObject go = new GameObject(path); go.transform.SetParent(parent, false); sp.t = go.transform;
        Texture2D t = Ui.Tex(path);
        if (t != null)
        {
            sp.r = go.AddComponent<SpriteRenderer>();
            sp.r.sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), bottomPivot ? new Vector2(0.5f, 0) : new Vector2(0, 1), 1f);
            sp.r.sharedMaterial = add ? fv.matSprAdd : fv.matSpr; sp.r.sortingOrder = order;
            go.transform.localScale = new Vector3(sp.w / t.width, sp.h / t.height, 1);
        }
        return sp;
    }
    Mesh Mk(Transform parent, string name, int order, Material m)
    {
        GameObject go = new GameObject(name); go.transform.SetParent(parent, false);
        if (name == "LiveAmb") mAmbGo = go.transform; if (name == "LiveBeams") mBeamGo = go.transform; if (name == "LiveSun") mSkyAddGo = go.transform;
        if (name == "LivePk") mPkGo = go.transform; if (name == "LiveLay2") mLayGo2 = go.transform; if (name == "LivePkAdd") mPkAddGo = go.transform;
        Mesh mesh = new Mesh(); mesh.MarkDynamic(); go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.sortingOrder = order;
        return mesh;
    }
    static void Pos(Spr sp, float x, float y, float a)
    {
        if (sp == null || sp.r == null) return;
        sp.t.localPosition = new Vector3(x, -y, 0);
        Color c = sp.r.color; c.a = a; sp.r.color = c;
    }

    public static float Wrap(float v, float a, float b) { float w = b - a; return a + (((v - a) % w) + w) % w; }
    public static float Gust(float t, float x) { float s = Mathf.Sin(t * 0.42f - x * 0.0021f); return 0.3f * Mathf.Sin(t * 1.7f + x * 0.013f) + (s > 0 ? s * s * s : 0) * 1.3f; }

    // ================= кадр =================
    public void Tick(float cam, float t, Camera c, float z)
    {
        if (L == null || L.IsNull()) return;
        if (c != null)
        {
            Vector3 cp = c.transform.position;
            scr.localPosition = new Vector3(cp.x - 640f / z, cp.y + 360f / z, 0); scr.localScale = new Vector3(1 / z, 1 / z, 1);
        }
        Beams(t);
        DebugVis();
        // аспан
        vSky.Clear(); vSkyAdd.Clear();
        JNode st = L.Get("stars");
        for (int i = 0; i < st.Count(); i++)
        {
            JNode js = st.At(i); float k = Mathf.Sin(t * js.Fk("sp") + js.Fk("ph")), sz = js.Fk("s");
            vSkyAdd.FillC(Col.WithA(Col.Pack(255, 246, 232, 255), js.Fk("a") * (0.35f + 0.65f * k * k)));
            float x = js.Fk("x"), y = js.Fk("y"); vSkyAdd.Quad(x, y, x + sz, y, x + sz, y + sz, x, y + sz);
        }
        JNode sun = L.Get("sun");
        if (!sun.IsNull())
        {
            float R = sun.Fk("r") * (1 + 0.045f * Mathf.Sin(t * 0.9f)), a = sun.Fk("a") * (0.85f + 0.15f * Mathf.Sin(t * 0.6f));
            int rgb = Col.Parse("rgb(" + sun.S("rgb") + ")");
            vSkyAdd.Glow(sun.Fk("x"), sun.Fk("y"), R, Col.WithA(rgb, a * 0.55f));
            vSkyAdd.Glow(sun.Fk("x"), sun.Fk("y"), R * 0.4f, Col.WithA(rgb, a * 0.5f));
        }
        for (int i = 0; i < clouds.Length; i++)
        {
            Spr sp = clouds[i]; JNode o = sp.o;
            float x = Wrap(o.Fk("x0") + t * o.Fk("v") - cam * o.Fk("par"), -sp.w - 40, W + 40);
            Pos(sp, x, o.Fk("y"), o.Get("a").F(1));
        }
        JNode sk = L.Get("sky");
        for (int i = 0; i < sk.Count(); i++) Item(vSky, vSkyAdd, sk.At(i), -cam * sk.At(i).Get("par").F(0), t, W);
        fv.Upload(mSky, vSky); fv.Upload(mSkyAdd, vSkyAdd);
        // қабаттар
        JNode lay = L.Get("lay");
        for (int li = 0; li < 3; li++)
        {
            VMesh v = vLay[li], va = vLayAdd[li]; v.Clear(); va.Clear();
            float ox = -cam * fv.ParK(li);
            JNode list = lay.At(li);
            for (int i = 0; i < list.Count(); i++) Item(v, va, list.At(i), ox, t, layW[li]);
            fv.Upload(mLay[li], v); fv.Upload(mLayAdd[li], va);
        }
        if (rays != null)
        {
            JNode R = rays.o;
            Pos(rays, R.Fk("x") + Mathf.Sin(t * 0.11f) * 6, R.Fk("y"), R.Fk("a") * (0.7f + 0.3f * Mathf.Sin(t * 0.31f) * Mathf.Sin(t * 0.17f + 1)));
        }
        if (mist.Length > 0)
        {
            JNode M = mist[0].o; float mw = mist[0].w, off = Wrap(-(cam * M.Fk("par")) - t * M.Fk("v"), -mw, 0);
            for (int i = 0; i < mist.Length; i++) Pos(mist[i], off + i * mw, M.Fk("y"), M.Fk("a"));
        }
        // бөлшектер
        vPk.Clear(); vPkAdd.Clear();
        JNode pk = L.Get("pk");
        for (int i = 0; i < pk.Count(); i++) Particles(pk.At(i).Str(), cam, t);
        fv.Upload(mPk, vPk); fv.Upload(mPkAdd, vPkAdd);
        // алдыңғы заттар
        for (int i = 0; i < fgs.Length; i++)
        {
            Spr f = fgs[i]; if (f.r == null) continue; JNode o = f.o;
            float w = f.w, sx = o.Fk("x") - cam * o.Fk("par");
            if (sx < -w || sx > W) { f.r.enabled = false; continue; }
            f.r.enabled = true;
            float d = Mathf.Abs(sx + w / 2 - W / 2) / (W / 2);
            Pos(f, sx + w / 2, H + o.Fk("dy"), d < 0.6f ? 0.3f + 0.7f * (d / 0.6f) : 1);
            float rot = o.Bk("sway") ? (Gust(t, sx) * 0.07f + 0.02f) * wind + Mathf.Sin(t * 1.5f + o.Fk("ph")) * 0.018f : 0;
            f.t.localRotation = Quaternion.Euler(0, 0, -rot * Mathf.Rad2Deg);
            Vector3 sc = f.t.localScale; sc.x = Mathf.Abs(sc.x) * (o.Bk("flip") ? -1 : 1); f.t.localScale = sc;
        }
        vFront.Clear();
        JNode gr = L.Get("grass");
        if (!gr.IsNull()) Grass(vFront, Col.Parse(gr.S("col")), cam, t);
        fv.Upload(mFront, vFront);
        for (int i = 0; i < corners.Length; i++)
        {
            Spr sp = corners[i]; if (sp.r == null) continue; JNode o = sp.o;
            float ax = o.Fk("ax") - (cam - 260) * 0.12f, rot = Mathf.Sin(t * 0.9f + o.Fk("ph")) * 0.02f + Gust(t, o.Fk("ax")) * 0.02f * wind;
            bool flip = o.Bk("flip"), right = o.Bk("right");
            if (flip || right) rot = -rot;
            sp.t.localPosition = new Vector3(ax, -(o.Fk("ay") - 42), 0);
            sp.t.localRotation = Quaternion.Euler(0, 0, -rot * Mathf.Rad2Deg);
            float k = sp.w / sp.r.sprite.texture.width;
            sp.t.localScale = new Vector3(flip ? -k : k, sp.h / sp.r.sprite.texture.height, 1);
            // right: сурет оң жақ шетімен бекітіледі
            if (right && !flip) sp.t.localPosition = new Vector3(ax - sp.w * Mathf.Cos(rot), -(o.Fk("ay") - 42) + sp.w * Mathf.Sin(rot), 0);
        }
        // түс беру — камера көрінісіне бекітілген
        if (grade != null && grade.r != null) grade.t.localPosition = Vector3.zero;
        Ambient(cam, t, z);
    }

    // ================= күн арқандары (веб: gfxAfterBack) =================
    void Beams(float t)
    {
        vBeam.Clear();
        JNode sun = L.Get("sun");
        if (!sun.IsNull())
        {
            float sx = sun.Fk("x"), sy = sun.Fk("y"), bas = Mathf.Atan2(H * 0.8f - sy, W / 2 - sx);
            int rgb = Col.Parse("rgb(" + sun.S("rgb") + ")");
            for (int i = 0; i < 7; i++)
            {
                float a = bas + (i - 3) * 0.3f + Mathf.Sin(t * 0.13f + i * 1.7f) * 0.07f, len = 1000 + 300 * Mathf.Sin(i * 3.1f), w = 150 + 90 * Mathf.Sin(i * 1.9f + t * 0.2f);
                float al = (0.05f + 0.035f * Mathf.Sin(t * 0.35f + i * 2.2f)) * (stIdx == 0 ? 0.8f : 1);
                if (al <= 0) continue;
                vBeam.Save(); vBeam.Translate(sx, sy); vBeam.Rotate(a - Mathf.PI / 2);
                Beam(vBeam, w, len, rgb, al);
                vBeam.Restore();
            }
        }
        fv.Upload(mBeam, vBeam);
    }
    static readonly float[] BU = { 0, 0.06f, 0.45f, 1 }, BA = { 0, 0.9f, 0.35f, 0 };
    static void Beam(VMesh v, float w, float len, int rgb, float al)
    {
        // трапеция: жоғарыда тар (6/64), төменде толық ені; ортасы жарық, шеттері мөлдір
        for (int k = 0; k < 3; k++)
        {
            float u0 = BU[k], u1 = BU[k + 1], h0 = w * (0.047f + 0.453f * u0), h1 = w * (0.047f + 0.453f * u1);
            int c0 = Col.WithA(rgb, BA[k] * al), c1 = Col.WithA(rgb, BA[k + 1] * al), e = Col.WithA(rgb, 0);
            float y0 = u0 * len, y1 = u1 * len;
            GradQuad(v, -h0, y0, 0, y0, 0, y1, -h1, y1, e, c0, c1, e);
            GradQuad(v, 0, y0, h0, y0, h1, y1, 0, y1, c0, e, e, c1);
        }
    }
    static void GradQuad(VMesh v, float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3, int a, int b, int c, int d)
    {
        v.QuadC(x0, y0, x1, y1, x2, y2, x3, y3, a, b, c, d);
    }

    // ================= алдыңғы атмосфера (веб: gfxAmbient + анаморфты жарық) =================
    static readonly string[] AMB = { "pollen", "pollen", "pollen", "snow", "ember", "sand", "sand" };
    static readonly string[] AMBRGB = { "255,236,185", "255,205,175", "255,225,160", "240,248,255", "255,150,70", "245,215,160", "250,225,170" };
    void Ambient(float cam, float t, float z)
    {
        // жер бетіндегі тұман
        if (gfog.Length > 0)
        {
            float w = 1500;
            for (int i = 0; i < 2; i++)
            {
                float par = 1.1f + i * 0.35f, x0 = Wrap(-cam * par - t * (12 + i * 9) * wind, -w, 0), a = (i == 1 ? 0.14f : 0.2f) * (0.75f + 0.25f * Mathf.Sin(t * 0.4f + i * 2));
                for (int j = 0; j < 2; j++)
                {
                    Spr g = gfog[i * 2 + j]; if (g.r == null) continue;
                    Pos(g, x0 + j * w, 575 + i * 42, a);
                    g.t.localScale = new Vector3(w / g.r.sprite.texture.width, (150 + i * 30) / g.r.sprite.texture.height, 1);
                }
            }
        }
        vAmb.Clear();
        string kind = AMB[stIdx]; int rgb = Col.Parse("rgb(" + AMBRGB[stIdx] + ")");
        for (int i = 0; i < 12; i++)
        {
            float par = 1.5f + ambD[i] * 0.9f, R = 12 + ambD[i] * 30, x, y, a;
            if (kind == "snow") { x = Wrap(ambX[i] - cam * par + t * (30 + ambV[i] * 50) * wind + Mathf.Sin(t * 0.8f + ambPh[i]) * 30, -80, W + 80); y = Wrap(ambY[i] + t * (70 + ambV[i] * 90), -80, H + 80); a = 0.22f + ambD[i] * 0.15f; }
            else if (kind == "ember") { x = Wrap(ambX[i] - cam * par + Mathf.Sin(t * 1.1f + ambPh[i]) * 40 + t * 12 * wind, -80, W + 80); y = Wrap(ambY[i] - t * (50 + ambV[i] * 70), -80, H + 80); a = (0.18f + 0.2f * Mathf.Max(0, Mathf.Sin(t * 5 + ambPh[i] * 3))) * (y / H * 0.6f + 0.4f); }
            else if (kind == "sand") { x = Wrap(ambX[i] - cam * par + t * (160 + ambV[i] * 220) * wind, -80, W + 80); y = 380 + (ambY[i] % 340) + Mathf.Sin(t * 2 + ambPh[i]) * 8; a = 0.05f + ambD[i] * 0.06f; }
            else { x = Wrap(ambX[i] - cam * par + t * (8 + ambV[i] * 18) * wind, -80, W + 80); y = Wrap(ambY[i] + Mathf.Sin(t * 0.5f + ambPh[i]) * 34 - t * 7, -80, H + 80); a = (0.1f + 0.1f * Mathf.Sin(t * 0.9f + ambPh[i])) + 0.06f; }
            if (a <= 0) continue;
            Bokeh(vAmb, x, y, R, rgb, a * 0.7f);
        }
        // анаморфты жарық сызығы (күн биік болса)
        JNode sun = L.Get("sun");
        if (!sun.IsNull() && sun.Fk("y") < 300)
        {
            float pv = 720 * 0.62f, sx = (sun.Fk("x") - W / 2) * z + W / 2, sy = (sun.Fk("y") - pv) * z + pv;
            int srgb = Col.Parse("rgb(" + sun.S("rgb") + ")");
            vAmb.Save(); vAmb.Translate(sx, sy); vAmb.Scale(1, 18f / 1040f); GlowSpr(vAmb, 0, 0, 520, srgb, 0.22f + 0.04f * Mathf.Sin(t * 0.7f)); vAmb.Restore();
            float vx = W / 2 - sx, vy = H / 2 - sy;
            GlowSpr(vAmb, sx + vx * 0.55f, sy + vy * 0.55f, 36, Col.Parse("#ffe0a0"), 0.07f);
            GlowSpr(vAmb, sx + vx * 1.25f, sy + vy * 1.25f, 70, Col.Parse("#a0d0ff"), 0.05f);
            GlowSpr(vAmb, sx + vx * 1.7f, sy + vy * 1.7f, 26, Col.Parse("#ffffff"), 0.08f);
        }
        fv.Upload(mAmb, vAmb);
    }
    // радиалды градиент сақиналармен (веб: createRadialGradient)
    static void Radial(VMesh v, float x, float y, float R, int rgb, float[] rs, float[] al, float k)
    {
        int seg = 20;
        for (int r = 0; r < rs.Length - 1; r++)
        {
            float r0 = rs[r] * R, r1 = rs[r + 1] * R; int c0 = Col.WithA(rgb, al[r] * k), c1 = Col.WithA(rgb, al[r + 1] * k);
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * 6.2832f / seg, a1 = (i + 1) * 6.2832f / seg;
                float x00 = x + Mathf.Cos(a0) * r0, y00 = y + Mathf.Sin(a0) * r0, x01 = x + Mathf.Cos(a1) * r0, y01 = y + Mathf.Sin(a1) * r0;
                float x10 = x + Mathf.Cos(a0) * r1, y10 = y + Mathf.Sin(a0) * r1, x11 = x + Mathf.Cos(a1) * r1, y11 = y + Mathf.Sin(a1) * r1;
                v.QuadC(x00, y00, x01, y01, x11, y11, x10, y10, c0, c0, c1, c1);
            }
        }
    }
    static readonly float[] BK_S = { 0, 0.7f, 0.86f, 1 }, BK_A = { 0.42f, 0.55f, 0.8f, 0 };
    static readonly float[] GL_S = { 0, 0.16f, 0.42f, 1 }, GL_A = { 1, 0.78f, 0.24f, 0 };
    static void Bokeh(VMesh v, float x, float y, float R, int rgb, float a) { Radial(v, x, y, R, rgb, BK_S, BK_A, a); }
    static void GlowSpr(VMesh v, float x, float y, float R, int rgb, float a) { Radial(v, x, y, R, rgb, GL_S, GL_A, a); }

    // ================= тірі элементтер =================
    static int qn;
    static void QStart(float x, float y) { xs[0] = x; ys[0] = y; qn = 1; }
    static void QLine(float x, float y) { if (qn < 63) { xs[qn] = x; ys[qn] = y; qn++; } }
    static void QCurve(float cx, float cy, float x, float y, int seg = 5)
    {
        float x0 = xs[qn - 1], y0 = ys[qn - 1];
        for (int i = 1; i <= seg && qn < 63; i++)
        {
            float u = i / (float)seg, a = (1 - u) * (1 - u), b = 2 * (1 - u) * u, c = u * u;
            xs[qn] = a * x0 + b * cx + c * x; ys[qn] = a * y0 + b * cy + c * y; qn++;
        }
    }
    static void P4(VMesh v, float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3)
    { xs[0] = x0; ys[0] = y0; xs[1] = x1; ys[1] = y1; xs[2] = x2; ys[2] = y2; xs[3] = x3; ys[3] = y3; v.Poly(xs, ys, 4); }
    static void P3(VMesh v, float x0, float y0, float x1, float y1, float x2, float y2)
    { xs[0] = x0; ys[0] = y0; xs[1] = x1; ys[1] = y1; xs[2] = x2; ys[2] = y2; v.Poly(xs, ys, 3); }
    static void Ln(VMesh v, float w, float x0, float y0, float x1, float y1)
    { xs[0] = x0; ys[0] = y0; xs[1] = x1; ys[1] = y1; v.Stroke(xs, ys, 2, w, true); }
    static void Ln3(VMesh v, float w, float x0, float y0, float x1, float y1, float x2, float y2)
    { xs[0] = x0; ys[0] = y0; xs[1] = x1; ys[1] = y1; xs[2] = x2; ys[2] = y2; v.Stroke(xs, ys, 3, w, true); }
    static void Rect(VMesh v, float x, float y, float w, float h) { v.Quad(x, y, x + w, y, x + w, y + h, x, y + h); }
    static int Rgb(string s, float a) { return Col.WithA(Col.Parse("rgb(" + s + ")"), a); }
    static void Soft(VMesh v, float x, float y, float r, int rgb, float a) { v.Glow(x, y, r, Col.WithA(rgb, a)); }

    void Item(VMesh v, VMesh va, JNode e, float ox, float t, float lw)
    {
        string k = e.S("k");
        switch (k)
        {
            case "flag":
                {
                    float sx = ox + e.Fk("x"), y = e.Fk("y"), h = e.Fk("h"), pw = e.Get("pw").F(2.4f); if (sx < -120 || sx > W + 120) return;
                    v.FillC(Col.Parse(e.S("pole"))); Ln(v, pw, sx, y, sx, y - h);
                    Cloth(v, sx, y - h + 1, e.Fk("len"), e.Fk("w"), t, e.Get("ph").F(0), wind, Col.Parse(e.S("col")), e.Bk("tri"), 0.55f + 0.55f * Gust(t, sx));
                    if (e.Has("top")) { v.FillC(Col.Parse(e.S("top"))); v.Circle(sx, y - h - 1, pw * 1.3f); }
                    return;
                }
            case "tug": { float sx = ox + e.Fk("x"); if (sx < -100 || sx > W + 100) return; Tug(v, sx, e.Fk("y"), e.Fk("h"), Col.Parse(e.S("col")), t, e.Get("ph").F(0), wind, e.Get("s").F(1)); return; }
            case "smoke":
                {
                    float sx = ox + e.Fk("x"); if (sx < -250 || sx > W + 250) return;
                    float s = e.Fk("s"), hgt = e.Get("hgt").F(160), a = e.Get("a").F(0.4f), ph = e.Get("ph").F(0); int n = e.Get("n").I(8);
                    int rgb = Col.Parse("rgb(" + e.S("rgb") + ")");
                    for (int i = 0; i < n; i++)
                    {
                        float q = (t * 0.085f + i / (float)n + ph) % 1, r = (7 + q * 34) * s;
                        float px = sx + wind * q * q * hgt * 0.6f + Mathf.Sin(t * 0.8f + i * 1.9f + ph * 7) * 7 * s * q, py = e.Fk("y") - q * hgt;
                        Soft(v, px, py, r * 0.9f, rgb, 0.62f * a * Mathf.Min(1, q * 7) * (1 - q));
                    }
                    return;
                }
            case "fire": { float sx = ox + e.Fk("x"); if (sx < -80 || sx > W + 80) return; Fire(v, va, sx, e.Fk("y"), e.Fk("s"), t, e.Get("ph").F(0), true); return; }
            case "torch":
                {
                    float sx = ox + e.Fk("x"); if (sx < -60 || sx > W + 60) return;
                    float y = e.Fk("y"), h = e.Fk("h"), s = e.Get("s").F(1);
                    v.FillC(Col.Parse("#1a0c08")); Ln(v, 3.5f * s, sx, y, sx, y - h);
                    v.FillC(Col.Parse("#2a160c")); Rect(v, sx - 5 * s, y - h - 4 * s, 10 * s, 7 * s);
                    Fire(v, va, sx, y - h - 3 * s, s * 0.62f, t, e.Get("ph").F(0), false); return;
                }
            case "flock":
                {
                    float ev = e.Fk("v"), s = e.Fk("s"), dir = ev > 0 ? 1 : -1, x0 = e.Fk("x0");
                    float bx0 = Wrap(x0 + t * ev, -500, W + 500) + ox, by0 = e.Fk("y") + Mathf.Sin(t * 0.5f + x0) * 12;
                    v.FillC(Col.Parse(e.S("col"))); int n = e.Ik("n");
                    for (int i = 0; i < n; i++)
                    {
                        int rank = (i + 1) >> 1; float side = i % 2 == 1 ? 1 : -1;
                        float x = bx0 - dir * rank * 17 * s + Mathf.Sin(t * 1.3f + i) * 2, y = by0 + side * rank * 9 * s + Mathf.Sin(t * 2 + i * 1.7f) * 2;
                        float f = Mathf.Sin(t * 7.5f + i * 1.1f) * 4.5f * s, w = 8 * s;
                        QStart(x - w, y - f); QCurve(x - w * 0.4f, y - 2.5f * s, x, y, 3); QCurve(x + w * 0.4f, y - 2.5f * s, x + w, y - f, 3);
                        v.Thin(xs, ys, qn, 1.7f * s);
                    }
                    return;
                }
            case "eagle":
                {
                    float a = t * e.Fk("v") + e.Fk("ph"), rx = e.Fk("rx"), ry = e.Fk("ry");
                    float x = e.Fk("cx") + Mathf.Cos(a) * rx + ox, y = e.Fk("cy") + Mathf.Sin(a) * ry;
                    float vx = -Mathf.Sin(a) * rx, vy = Mathf.Cos(a) * ry;
                    float fl = Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * 0.6f + e.Fk("ph"))), 12) * Mathf.Sin(t * 11);
                    Eagle(v, x, y, e.Fk("s") * (1 + Mathf.Sin(a) * 0.12f), fl, Mathf.Clamp(vx / (rx + 1), -1, 1) * -0.22f + vy / (ry + 1) * 0.1f, Col.Parse(e.S("col")));
                    return;
                }
            case "rider":
                {
                    float ev = e.Fk("v"), s = e.Fk("s"), x0 = e.Fk("x0");
                    float x = Wrap(x0 + t * ev, -160, lw + 160) + ox; if (x < -160 || x > W + 160) return;
                    float dir = ev > 0 ? 1 : -1, ph = t * e.Get("gait").F(9) + x0, y = e.Fk("y");
                    if (e.Has("dust"))
                    {
                        int rgb = Col.Parse("rgb(" + e.S("dust") + ")");
                        for (int q4 = 0; q4 < 4; q4++) { float q = (t * 1.6f + q4 / 4f) % 1, R = (8 + q * 22) * s * 2; Soft(v, x - dir * (24 + q * 70) * s * 2, y - (4 + q * 14) * s * 2, R, rgb, (1 - q) * 0.45f * 0.6f); }
                    }
                    v.FillC(Col.Parse(e.S("col")));
                    Rider(v, x, y - Mathf.Abs(Mathf.Sin(ph)) * 4 * s, s, dir, ph, e.Get("hat").Str("kalpak"), e.Bk("spear"), e.Bk("tugF"));
                    return;
                }
            case "walker":
                {
                    float ev = e.Fk("v"), s = e.Fk("s"), x0 = e.Fk("x0");
                    float x = Wrap(x0 + t * ev, -60, lw + 60) + ox; if (x < -60 || x > W + 60) return;
                    float ph = t * 5 + x0; v.FillC(Col.Parse(e.S("col")));
                    Walker(v, x, e.Fk("y") - Mathf.Abs(Mathf.Sin(ph)) * 1.5f * s, s, ev > 0 ? 1 : -1, ph, e.Bk("load")); return;
                }
            case "caravan":
                {
                    float ev = e.Fk("v"), s = e.Fk("s"), dir = ev > 0 ? 1 : -1, x0 = Wrap(e.Fk("x0") + t * ev, -500, lw + 500) + ox;
                    v.FillC(Col.Parse(e.S("col"))); int n = e.Ik("n");
                    for (int i = 0; i < n; i++) { float x = x0 - dir * i * e.Fk("gap"); if (x < -100 || x > W + 100) continue; Camel(v, x, e.Fk("y"), s, dir, t * 3 + i * 1.3f, i > 0 || e.Bk("lead")); }
                    if (e.Bk("driver") && x0 > -60 && x0 < W + 60) Walker(v, x0 + dir * 78 * s, e.Fk("y"), s * 1.25f, dir, t * 5, false);
                    return;
                }
            case "glint":
                {
                    JNode list = e.Get("list"); float flow = e.Get("flow").F(0), ea = e.Get("a").F(1), lwd = e.Get("lw").F(1.4f); int col = Col.Parse(e.S("col"));
                    for (int i = 0; i < list.Count(); i++)
                    {
                        JNode q = list.At(i); float x = Wrap(q.Fk("x") + t * flow, 0, lw) + ox; if (x < -40 || x > W + 40) continue;
                        float a = Mathf.Sin(t * q.Fk("sp") + q.Fk("ph")); if (a < 0.15f) continue;
                        va.FillC(Col.WithA(col, (a - 0.15f) * ea * Col.A(col) / 255f));
                        float y = q.Fk("y"), l = q.Fk("len") * (0.5f + a * 0.5f);
                        Rect(va, x, y - lwd / 2, l, lwd);
                    }
                    return;
                }
            case "floes":
                {
                    JNode list = e.Get("list"); float flow = e.Get("flow").F(0);
                    for (int i = 0; i < list.Count(); i++)
                    {
                        JNode q = list.At(i); float x = Wrap(q.Fk("x") + t * flow, -60, lw + 60) + ox; if (x < -60 || x > W + 60) continue;
                        float y = q.Fk("y") + Mathf.Sin(t * 0.9f + q.Fk("ph")) * 0.7f, l = q.Fk("len");
                        v.FillC(Col.Pack(90, 120, 145, 115)); v.Ellipse(x + 2, y + 2, l * 0.5f, l * 0.1f, 0);
                        v.FillC(Col.Parse("#eef5fa")); v.Ellipse(x, y, l * 0.5f, l * 0.11f, 0);
                    }
                    return;
                }
            case "boat":
                {
                    float x = Wrap(e.Fk("x0") + t * e.Fk("v"), -200, lw + 200) + ox; if (x < -120 || x > W + 120) return;
                    float s = e.Fk("s"), y = e.Fk("y") + Mathf.Sin(t * 1.4f) * 1.4f, pa = Mathf.Sin(t * 0.8f) * 0.25f;
                    v.FillC(Col.Pack(0, 0, 0, 46)); v.Ellipse(x, y + 4 * s, 58 * s, 5 * s, 0);
                    v.Save(); v.Translate(x, y); v.Rotate(Mathf.Sin(t * 1.1f) * 0.025f); v.FillC(Col.Parse(e.S("col")));
                    QStart(-56 * s, -12 * s); QCurve(-30 * s, 4 * s, 0, 4 * s); QCurve(34 * s, 4 * s, 58 * s, -14 * s); QLine(52 * s, -6 * s); QLine(-50 * s, -6 * s); v.Poly(xs, ys, qn);
                    P4(v, -6 * s, -6 * s, 6 * s, -6 * s, 5 * s, -40 * s, -5 * s, -40 * s); v.Circle(1 * s, -47 * s, 6 * s);
                    Ln(v, 2.4f * s, 2 * s, -36 * s, -24 * s + pa * 40 * s, 10 * s); Ln(v, 2.4f * s, -10 * s - pa * 30 * s, -78 * s, -36 * s + pa * 50 * s, 14 * s);
                    v.Restore(); return;
                }
            case "devil":
                {
                    float ph = e.Get("ph").F(0), life = Mathf.Sin(t * 0.13f + ph); if (life < 0) return;
                    float s = e.Fk("s"), fade = Mathf.Min(1, life * 3), x = Wrap(e.Fk("x0") + t * e.Fk("v"), -120, lw + 120) + ox + Mathf.Sin(t * 0.4f + ph) * 40;
                    if (x < -120 || x > W + 120) return;
                    int rgb = Col.Parse("rgb(" + e.S("rgb") + ")"); float y = e.Fk("y"), h = e.Fk("h"), a = e.Fk("a");
                    for (int j = 0; j < 12; j++)
                    {
                        float u = j / 11f, yy = y - u * h, rw = (5 + u * u * 34) * s, xx = x + Mathf.Sin(t * 2.3f + u * 3.5f + ph) * 9 * s * u + u * 24 * s;
                        v.FillC(Col.WithA(rgb, fade * a * (1 - u * 0.55f) * 0.6f)); v.Save(); v.Translate(xx, yy); v.Scale(1, 0.45f); v.Glow(0, 0, rw, Col.WithA(rgb, fade * a * (1 - u * 0.55f) * 0.6f)); v.Restore();
                    }
                    for (int q = 0; q < 9; q++)
                    {
                        float an = t * 7 + q * 0.8f, u = (q * 0.37f + t * 0.3f) % 1, rw = (6 + u * u * 34) * s;
                        v.FillC(Col.WithA(rgb, fade * 0.7f * (1 - u))); Rect(v, x + Mathf.Cos(an) * rw + u * 24 * s, y - u * h + Mathf.Sin(an) * rw * 0.25f, 2, 2);
                    }
                    return;
                }
            case "crowd":
                {
                    JNode list = e.Get("list"); int col = Col.Parse(e.S("col")), tip = Col.Parse(e.Has("tip") ? e.S("tip") : "#8a6a5a"); float ey = e.Fk("y");
                    for (int i = 0; i < list.Count(); i++)
                    {
                        JNode q = list.At(i); float sx = ox + q.Fk("x"); if (sx < -30 || sx > W + 30) continue;
                        float s = q.Fk("s"), y = ey + q.Fk("dy"), hg = 64 * s, ph = q.Fk("ph"), sw = Mathf.Sin(t * 1.3f + ph) * 2.6f * s + Mathf.Sin(t * 0.5f + ph * 2) * 1.5f;
                        v.FillC(col);
                        P4(v, sx - 10 * s, y, sx + 10 * s, y, sx + 8 * s + sw, y - hg * 0.74f, sx - 8 * s + sw, y - hg * 0.74f);
                        float hx = sx + sw * 1.15f, hy = y - hg * 0.84f;
                        v.Circle(hx, hy, 6.5f * s); P3(v, hx - 7 * s, hy - 2 * s, hx + sw * 0.1f, hy - 17 * s, hx + 7 * s, hy - 2 * s);
                        if (q.Bk("spear"))
                        {
                            float lift = Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * 0.7f + ph * 3)), 8) * 16 * s;
                            float x1 = sx + 13 * s + sw * 1.4f, y1 = y - hg * 1.55f - lift;
                            Ln(v, 2.2f, sx + 9 * s + sw * 0.6f, y - 30 * s - lift, x1, y1);
                            v.FillC(tip); P3(v, x1 - 2.5f, y1 + 2, x1 + 0.6f, y1 - 9, x1 + 3, y1 + 2);
                        }
                    }
                    return;
                }
            case "blades":
                {
                    JNode list = e.Get("list"); float ey = e.Fk("y"), lwd = e.Get("lw").F(1.4f); v.FillC(Col.Parse(e.S("col")));
                    for (int i = 0; i < list.Count(); i++)
                    {
                        JNode b = list.At(i); float sx = ox + b.Fk("x"); if (sx < -12 || sx > W + 12) continue;
                        float bend = b.Fk("lean") + wind * (Gust(t, sx) * 7 + 2) + Mathf.Sin(t * 2.2f + b.Fk("ph")) * 1.5f, h = b.Fk("h");
                        QStart(sx, ey); QCurve(sx + bend * 0.25f, ey - h * 0.55f, sx + bend, ey - h + Mathf.Abs(bend) * 0.25f, 3);
                        v.Thin(xs, ys, qn, lwd);
                    }
                    return;
                }
            case "reeds":
                {
                    JNode list = e.Get("list"); float ey = e.Fk("y"); v.FillC(Col.Parse(e.S("col")));
                    for (int i = 0; i < list.Count(); i++)
                    {
                        JNode q = list.At(i); float sx = ox + q.Fk("x"); if (sx < -30 || sx > W + 30) continue;
                        float bend = q.Fk("lean") + wind * (Gust(t, sx) * 12 + 4) + Mathf.Sin(t * 1.8f + q.Fk("ph")) * 2.5f, h = q.Fk("h");
                        float tx0 = sx + bend, ty0 = ey - h + Mathf.Abs(bend) * 0.2f;
                        QStart(sx, ey); QCurve(sx + bend * 0.2f, ey - h * 0.5f, tx0, ty0, 4); v.Thin(xs, ys, qn, 1.6f);
                        v.Ellipse(tx0, ty0, 2.6f, 8, bend * 0.025f);
                    }
                    return;
                }
        }
    }

    void Cloth(VMesh v, float x, float y, float len, float w, float t, float ph, float dir, int col, bool tri, float amp)
    {
        int N = 8;
        for (int i = 0; i <= N; i++)
        {
            float u = i / (float)N, a = t * 6.2f - u * 6 + ph;
            float wv = (Mathf.Sin(a) * 5.5f + Mathf.Sin(t * 2.1f - u * 2.5f + ph) * 3) * u * amp * (len / 40);
            float droop = u * u * len * 0.3f * Mathf.Max(0, 1.1f - amp);
            float px = x + dir * u * len * (0.9f + 0.1f * Mathf.Min(1, amp)), hw = tri ? w * (1 - u * 0.88f) : w;
            tx[i] = px; ty[i] = y + wv + droop + (tri ? (w - hw) * 0.5f : 0);
            bx[i] = px; by[i] = y + wv + droop + (tri ? (w + hw) * 0.5f : w) + Mathf.Sin(a + 0.9f) * u * 2.2f * amp;
        }
        v.FillC(col);
        for (int i = 0; i < N; i++) v.Quad(tx[i], ty[i], tx[i + 1], ty[i + 1], bx[i + 1], by[i + 1], bx[i], by[i]);
        for (int i = 0; i < N; i++)
        {
            float d = Mathf.Cos(t * 6.2f - (i + 0.5f) / N * 6 + ph); if (Mathf.Abs(d) < 0.3f) continue;
            v.FillC(d > 0 ? Col.Pack(255, 240, 220, 36) : Col.Pack(0, 0, 0, 61));
            v.Quad(tx[i], ty[i], tx[i + 1], ty[i + 1], bx[i + 1], by[i + 1], bx[i], by[i]);
        }
    }

    void Tug(VMesh v, float x, float y, float h, int col, float t, float ph, float dir, float s)
    {
        float tx0 = x + 4 * s, ty0 = y - h, gs = 0.5f + 0.5f * Gust(t, x), L0 = 58 * s;
        v.FillC(col); Ln(v, 4 * s, x, y, tx0, ty0);
        for (int k = 0; k < 12; k++)
        {
            float a = dir * (0.1f + gs * 0.55f) + Mathf.Sin(t * 3.1f + k * 0.8f + ph) * (0.07f + gs * 0.12f) + (k - 5.5f) * 0.035f, ln = L0 * (0.8f + (k % 3) * 0.1f);
            QStart(tx0, ty0 + 9 * s); QCurve(tx0 + Mathf.Sin(a * 0.4f) * ln * 0.5f, ty0 + 9 * s + ln * 0.5f, tx0 + Mathf.Sin(a) * ln, ty0 + 9 * s + Mathf.Cos(a) * ln, 4);
            v.Thin(xs, ys, qn, 2.6f * s);
        }
        v.Circle(tx0, ty0 + 9 * s, 5.5f * s);
        v.FillC(Col.Parse("#f2b840"));
        QStart(tx0 - 6 * s, ty0 + 2 * s); QCurve(tx0 - 2 * s, ty0 - 10 * s, tx0, ty0 - 24 * s); QCurve(tx0 + 2 * s, ty0 - 10 * s, tx0 + 6 * s, ty0 + 2 * s); v.Poly(xs, ys, qn);
        v.Circle(tx0, ty0 + 2 * s, 3.4f * s);
    }

    void Fire(VMesh v, VMesh va, float x, float y, float s, float t, float ph, bool logs)
    {
        float f = Mathf.Sin(t * 13 + ph) * 0.5f + Mathf.Sin(t * 23.7f + ph * 2) * 0.3f + Mathf.Sin(t * 7.1f + ph) * 0.2f;
        float R = (64 + f * 7) * s;
        va.Glow(x, y - 12 * s, R, Col.Pack(255, 130, 45, (int)(255 * 0.9f * (0.5f + f * 0.1f) * 0.7f)));
        if (logs) { v.FillC(Col.Parse("#1a0c08")); Ln(v, 4 * s, x - 14 * s, y + 2 * s, x + 12 * s, y - 4 * s); Ln(v, 4 * s, x + 14 * s, y + 2 * s, x - 12 * s, y - 4 * s); }
        float sw = Mathf.Sin(t * 9 + ph) * 3 * s;
        Flame(v, x, y, sw, 11 * s, (32 + f * 8) * s, Col.Parse("#e4501c"), 1.3f);
        Flame(v, x, y, sw, 7.5f * s, (23 + f * 6) * s, Col.Parse("#ffa733"), 1);
        Flame(v, x, y, sw, 4 * s, (12 + f * 3) * s, Col.Parse("#fff2b8"), 0.6f);
        for (int k = 0; k < 3; k++)
        {
            float q = (t * 0.9f + k / 3f + ph) % 1;
            v.FillC(Col.WithA(Col.Parse("#ffd27a"), 1 - q)); Rect(v, x + Mathf.Sin(t * 3 + k * 2 + ph) * 9 * s * q, y - (22 + q * 70) * s, 2 * s, 2 * s);
        }
    }
    static void Flame(VMesh v, float x, float y, float sw, float w, float h, int col, float k)
    {
        v.FillC(col); QStart(x - w, y); QCurve(x - w * 0.9f, y - h * 0.55f, x + sw * k, y - h); QCurve(x + w * 0.9f, y - h * 0.55f, x + w, y); v.Poly(xs, ys, qn);
    }

    void Eagle(VMesh v, float x, float y, float s, float flap, float rot, int col)
    {
        v.Save(); v.Translate(x, y); v.Rotate(rot); v.FillC(col);
        float sp = 64 * s, wy = flap * 14 * s;
        for (int d = -1; d <= 1; d += 2)
        {
            QStart(0, -12 * s); QLine(d * 5 * s, -4 * s);
            QCurve(d * sp * 0.45f, -9 * s - wy * 0.5f, d * sp, -3 * s - wy);
            QLine(d * sp * 0.93f, 3 * s - wy); QLine(d * sp * 0.97f, 6 * s - wy); QLine(d * sp * 0.85f, 6 * s - wy * 0.95f);
            QLine(d * sp * 0.87f, 10 * s - wy * 0.9f); QLine(d * sp * 0.74f, 9 * s - wy * 0.85f);
            QCurve(d * sp * 0.36f, 13 * s - wy * 0.3f, d * 6 * s, 5 * s);
            QLine(d * 7 * s, 19 * s); QLine(0, 21 * s);
            v.Poly(xs, ys, qn);
        }
        v.Circle(0, -12 * s, 4 * s);
        v.Restore();
    }

    static void HorseLeg(VMesh v, float s, float x0, float a1, float a2)
    {
        float kx = x0 * s + Mathf.Sin(a1) * 20 * s, ky = -38 * s + Mathf.Cos(a1) * 20 * s;
        Ln3(v, 5 * s, x0 * s, -42 * s, kx, ky, kx + Mathf.Sin(a2) * 20 * s, ky + Mathf.Cos(a2) * 20 * s);
    }
    public static void Horse(VMesh v, float s, float ph)
    {
        v.Ellipse(0, -46 * s, 36 * s, 14 * s, -0.05f);
        P4(v, 22 * s, -56 * s, 34 * s, -50 * s, 52 * s, -82 * s, 44 * s, -90 * s);
        P4(v, 44 * s, -92 * s, 66 * s, -80 * s, 64 * s, -73 * s, 46 * s, -76 * s);
        P3(v, 40 * s, -94 * s, 44 * s, -104 * s, 47 * s, -92 * s);
        P4(v, 28 * s, -60 * s, 46 * s, -92 * s, 38 * s, -96 * s, 22 * s, -66 * s);
        HorseLeg(v, s, 24, 0.5f + Mathf.Sin(ph) * 0.6f, -0.2f + Mathf.Sin(ph) * 0.5f); HorseLeg(v, s, 18, 0.3f + Mathf.Sin(ph + 1) * 0.6f, 0.1f + Mathf.Sin(ph + 1) * 0.4f);
        HorseLeg(v, s, -24, -0.4f + Mathf.Sin(ph + 2) * 0.6f, 0.2f + Mathf.Sin(ph + 2) * 0.4f); HorseLeg(v, s, -18, -0.2f + Mathf.Sin(ph + 3) * 0.6f, 0.3f + Mathf.Sin(ph + 3) * 0.4f);
        P4(v, -34 * s, -52 * s, -40 * s, -50 * s, -60 * s, -26 * s + Mathf.Sin(ph) * 4 * s, -48 * s, -30 * s);
    }
    public static void Rider(VMesh v, float x, float y, float s, float dir, float ph, string hat, bool spear, bool tug)
    {
        v.Save(); v.Translate(x, y); v.Scale(dir, 1);
        Horse(v, s, ph);
        P4(v, -8 * s, -58 * s, 10 * s, -58 * s, 12 * s, -92 * s, -2 * s, -97 * s);
        Ln3(v, 6 * s, 2 * s, -60 * s, 12 * s, -46 * s, 8 * s, -32 * s);
        v.Circle(6 * s, -105 * s, 7.5f * s);
        if (hat == "kalpak") P4(v, -3 * s, -109 * s, 15 * s, -109 * s, 9 * s, -126 * s, 2 * s, -127 * s);
        else if (hat == "fur") v.Circle(6 * s, -111 * s, 9 * s);
        else if (hat == "helmet") P3(v, -2 * s, -108 * s, 14 * s, -108 * s, 6 * s, -130 * s);
        else if (hat == "turban") v.Ellipse(6 * s, -112 * s, 10 * s, 7 * s, 0);
        Ln(v, 5 * s, 8 * s, -90 * s, 20 * s, -80 * s);
        if (spear) Ln(v, 3 * s, 0, -70 * s, 80 * s, -128 * s);
        if (tug) { Ln(v, 3 * s, -6 * s, -60 * s, -10 * s, -175 * s); P4(v, -10 * s, -170 * s, -4 * s, -166 * s, 4 * s, -128 * s, -6 * s, -124 * s); }
        v.Restore();
    }
    static void Walker(VMesh v, float x, float y, float s, float dir, float ph, bool load)
    {
        v.Save(); v.Translate(x, y); v.Scale(dir, 1);
        float a = Mathf.Sin(ph) * 0.4f;
        Ln3(v, 5 * s, 0, -40 * s, Mathf.Sin(a) * 18 * s, -20 * s, Mathf.Sin(a) * 22 * s, 0);
        Ln3(v, 5 * s, 0, -40 * s, -Mathf.Sin(a) * 18 * s, -20 * s, -Mathf.Sin(a) * 22 * s, 0);
        P4(v, -8 * s, -40 * s, 8 * s, -40 * s, 6 * s, -72 * s, -6 * s, -72 * s);
        v.Circle(2 * s, -80 * s, 7 * s);
        if (load) v.Ellipse(-10 * s, -64 * s, 10 * s, 13 * s, 0);
        Ln(v, 4 * s, 4 * s, -66 * s, 14 * s, -50 * s);
        v.Restore();
    }
    static void Camel(VMesh v, float x, float y, float s, float dir, float ph, bool load)
    {
        v.Save(); v.Translate(x, y); v.Scale(dir, 1);
        float bob = Mathf.Sin(ph * 2) * 1.6f * s, nk = Mathf.Sin(ph) * 3 * s;
        v.Ellipse(0, -60 * s + bob, 40 * s, 16 * s, 0);
        v.Circle(-14 * s, -74 * s + bob, 14 * s); v.Circle(14 * s, -74 * s + bob, 14 * s);
        P4(v, 32 * s, -66 * s + bob, 44 * s, -56 * s + bob, 60 * s + nk, -94 * s + bob, 51 * s + nk, -101 * s + bob);
        v.Ellipse(62 * s + nk, -99 * s + bob, 12 * s, 7 * s, 0.2f);
        if (load) { Rect(v, -30 * s, -94 * s + bob, 60 * s, 14 * s); Rect(v, -36 * s, -80 * s + bob, 12 * s, 26 * s); Rect(v, 24 * s, -80 * s + bob, 12 * s, 26 * s); }
        for (int i = 0; i < 4; i++)
        {
            float lx = i == 0 ? -28 : i == 1 ? -18 : i == 2 ? 20 : 30, o = i % 2 == 1 ? Mathf.PI : 0;
            float a = Mathf.Sin(ph + o) * 0.38f, kx = lx * s + Mathf.Sin(a) * 24 * s;
            Ln3(v, 5 * s, lx * s, -50 * s + bob, kx, -26 * s, kx + Mathf.Sin(a * 1.4f) * 22 * s, 0);
        }
        v.Restore();
    }

    // ================= алдыңғы шөп (веб: FG_ITEMS) =================
    void Grass(VMesh v, int col, float cam, float t)
    {
        JNode G = Db.J("meta_live").Get("__grass"); v.FillC(col);
        for (int ci = 0; ci < G.Count(); ci++)
        {
            JNode gc = G.At(ci); float cx = gc.Fk("x"), ch = gc.Fk("h"); int n = gc.Ik("n");
            float sx = cx - cam * 1.3f; if (sx < -80 || sx > W + 80) continue;
            float gs = Gust(t, sx) * wind * 9;
            for (int i = 0; i < n; i++)
            {
                float b0 = sx + i * 7 - n * 3.5f, sway = Mathf.Sin(t * 1.4f + cx * 0.01f + i) * 5 + gs;
                float tx0 = b0 + sway + (i - n / 2f) * 6, ty0 = H - ch - (i % 3) * 8 + Mathf.Abs(gs) * 0.5f;
                QStart(b0, H + 4); QCurve(b0 + sway * 0.3f, H - ch * 0.5f, tx0, ty0, 4); v.Thin(xs, ys, qn, 2.2f);
                v.Ellipse(tx0, ty0, 2.2f, 9, sway * 0.03f);
            }
        }
    }

    // ================= ауа бөлшектері (веб: PK_DRAW) =================
    void Particles(string k, float cam, float t)
    {
        VMesh v = vPk, va = vPkAdd;
        switch (k)
        {
            case "motes":
                {
                    int c = Col.Pack(255, 220, 160, 255);
                    for (int i = 0; i < 44; i++) { float x = Wrap(pkx[i] - cam * 0.4f * pkd[i] + t * (4 + pkv[i] * 10) * wind, -10, W + 10), y = 80 + (pky[i] % 520) + Mathf.Sin(t * 0.7f + pkph[i]) * 9, R = 2 + pkd[i] * 4; Soft(va, x, y, R, c, (0.25f + 0.25f * Mathf.Sin(t + pkph[i])) * 0.8f); }
                    return;
                }
            case "dust":
                {
                    int c = Col.Pack(255, 225, 170, 255);
                    for (int i = 20; i < 70; i++) { float x = Wrap(pkx[i] - cam * 0.5f * pkd[i] + t * (8 + pkv[i] * 30) * wind, -10, W + 10), y = 300 + (pky[i] % 380) + Mathf.Sin(t + pkph[i]) * 6, R = 2 + pkd[i] * 3.5f; Soft(va, x, y, R, c, (0.18f + 0.12f * Mathf.Sin(t + pkph[i])) * 0.8f); }
                    return;
                }
            case "embers":
                {
                    int c = Col.Pack(255, 140, 50, 255), core = Col.Parse("#ffd890");
                    for (int i = 0; i < 55; i++)
                    {
                        float d = pkd[i], x = Wrap(pkx[i] - cam * 0.6f * d + Mathf.Sin(t * 1.3f + pkph[i]) * 18 + t * wind * 14 * d, -20, W + 20), y = H + 20 - Wrap(pky[i] + t * (22 + 50 * d), 0, H + 40);
                        float fl = 0.5f + 0.5f * Mathf.Sin(t * 6 + pkph[i] * 3), R = 3 + d * 5, a = (0.3f + 0.55f * fl) * (y / H * 0.6f + 0.4f);
                        Soft(va, x, y, R, c, a * 0.8f);
                        va.FillC(Col.WithA(core, a)); float q = 0.8f * d + 0.4f; Rect(va, x - q, y - q, q * 2, q * 2);
                    }
                    return;
                }
            case "ash":
                {
                    v.FillC(Col.Pack(58, 42, 44, 115));
                    for (int i = 55; i < 80; i++)
                    {
                        float x = Wrap(pkx[i] + t * wind * 20 - cam * 0.7f + Mathf.Sin(t + pkph[i]) * 20, -10, W + 10), y = Wrap(pky[i] + t * (14 + pkv[i] * 16), -10, H + 10);
                        v.Save(); v.Translate(x, y); v.Rotate(t * 2 + pkph[i]); Rect(v, -2.5f, -1, 5, 2); v.Restore();
                    }
                    return;
                }
            case "fluff":
                {
                    v.FillC(Col.Pack(255, 255, 250, 191));
                    for (int i = 60; i < 76; i++)
                    {
                        float gs = Gust(t, pkx[i]), x = Wrap(pkx[i] + t * wind * (18 + pkv[i] * 20) + gs * 30 * wind - cam * (0.5f + 0.5f * pkd[i]), -20, W + 20), y = 250 + (pky[i] % 380) + Mathf.Sin(t * 0.9f + pkph[i]) * 16 - gs * 10;
                        float s = 1.5f + pkd[i] * 2.5f;
                        for (int q = 0; q < 5; q++) { float a = q / 5f * 6.2832f + t * 0.5f; xs[0] = x; ys[0] = y; xs[1] = x + Mathf.Cos(a) * s * 1.6f; ys[1] = y + Mathf.Sin(a) * s * 1.6f; v.Thin(xs, ys, 2, 0.8f); }
                        Rect(v, x - 0.8f, y - 0.8f, 1.6f, 1.6f);
                    }
                    return;
                }
            case "leaves":
                {
                    JNode lc = L.Get("leafCol"); int nc = Mathf.Max(1, lc.Count());
                    for (int i = 0; i < 16; i++)
                    {
                        int j = i + 30; float d = pkd[j], gs = Gust(t, pkx[j]);
                        float x = Wrap(pkx[j] + t * wind * (30 + d * 40) + gs * 40 * wind + Mathf.Sin(t * 1.2f + pkph[j]) * 26 - cam * (0.4f + 0.7f * d), -30, W + 30), y = Wrap(pky[j] + t * (24 + d * 30) + Mathf.Sin(t * 2.1f + pkph[j]) * 10, -20, H + 20);
                        float rot = t * (1.5f + pkv[j] * 2) + pkph[j], sz = 3 + d * 4;
                        int c = lc.Count() > 0 ? Col.Parse(lc.At(i % nc).Str()) : Col.Parse("#c89a3a");
                        v.FillC(Col.WithA(c, 0.55f + d * 0.4f)); v.Ellipse(x, y, sz * (0.25f + 0.75f * Mathf.Abs(Mathf.Cos(rot))), sz * 0.55f, rot * 0.5f);
                    }
                    return;
                }
            case "snow":
                {
                    float sway = Mathf.Sin(t * 0.37f) * 50 + Mathf.Sin(t * 0.13f) * 30;
                    for (int i = 0; i < 90; i++)
                    {
                        float d = pkd[i], x = Wrap(pkx[i] + t * wind * (12 + 30 * d) + sway * d + Mathf.Sin(t * 1.1f + pkph[i]) * 10 * d - cam * (0.3f + 0.6f * d), -10, W + 10), y = Wrap(pky[i] + t * (20 + 55 * d), -10, H + 10);
                        v.FillC(Col.WithA(Col.Pack(255, 255, 255, 255), 0.35f + d * 0.55f));
                        if (d < 0.55f) Rect(v, x - d * 1.8f, y - d * 1.8f, d * 3.6f, d * 3.6f); else v.Circle(x, y, 0.6f + d * 2);
                    }
                    for (int i = 0; i < 9; i++)
                    {
                        float x = Wrap(pkx[i] * 1.3f + t * wind * 70 + sway * 1.6f - cam * 1.4f, -40, W + 40), y = Wrap(pky[i] + t * (110 + pkv[i] * 60), -40, H + 40), R = 7 + pkv[i] * 8;
                        Soft(v, x, y, R, Col.Pack(255, 255, 255, 255), 0.3f);
                    }
                    return;
                }
            case "sand":
                {
                    int c = Col.Pack(240, 214, 165, 255);
                    for (int i = 0; i < 42; i++)
                    {
                        float d = pkd[i], gs = 0.5f + 0.5f * Gust(t, pkx[i]), x = Wrap(pkx[i] + t * wind * (260 + 380 * d) - cam * (0.6f + d), -120, W + 120), y = 540 + (pky[i] % 176) + Mathf.Sin(t * 3 + pkph[i]) * 3;
                        v.FillC(Col.WithA(c, Mathf.Clamp01((0.08f + 0.22f * d) * gs)));
                        xs[0] = x; ys[0] = y; xs[1] = x - wind * (16 + 46 * d); ys[1] = y + 1; v.Thin(xs, ys, 2, 0.8f + d * 1.4f);
                    }
                    return;
                }
        }
    }
}
