// SHAIQAS Core · ӘДІСТЕР 1.0 (веб: SKILLS, skMatch, SK_IMPL, skPre/skPost, skAiTry): 18 әдіс,
// бағыт+батырма тізбектері, салқындау, рух құны, снарядтар, күйлер (мұз, жалын, баяулау, тартылу,
// есеңгіреу), қайтару (parry), аруақ-көлеңке, ЖИ әдістері, алғашқы қолдану тәжірибесі.
using UnityEngine;

public class SkDef
{
    public string id, kind, name, desc;
    public string[] seq;
    public int lvl, idx;
    public float cd, cost;
    public bool air, ult;
}

/// <summary>Белсенді әдіс (веб: f.sk) — барлық әдістің жалпы өрістері.</summary>
public class SkRun
{
    public SkDef def; public Move cm;
    public float t, ct, hcd, tx;
    public int n, ph, k;
    public bool hit, shot, landed, counter, cHit, struck, fin, roar, slam, cast, hitDone;
    public Fighter target;
}

public class SkProj
{
    public string kind; public Fighter owner;
    public float x, y, vx, vy, t, life = 1.5f, r = 34, gap = 0.15f, hcd, dmg = 5, knock = 260, lastKnock, hitstop = 0.05f, lastSpk = -99999;
    public int hits = 1;
    public string height = "mid";
    public bool down, heavy, unblock;
    public int onHit;   // 0 — жоқ, 1 — мұз, 2 — арқан
}

/// <summary>Жауынгердің әдіс күйі (веб: skS).</summary>
public class SkState
{
    public float[] cd = new float[24];
    public float stun, slow, burn, hot, frost, ice, parry, wantT, aiGap, refill;
    public bool slowOn;
    public float pullTo, pullT;
    public float cloneT, cloneFade; public int cloneN, cloneQ; public float[] cloneAt = new float[8]; public Move[] cloneCm = new Move[8];
    public string callText, callKind; public float callT = 9; public bool callFail;
    public bool og = true; public float rux;
}

/// <summary>Енгізу буфері (веб: SKB): бағыт белгілері мен батырмалар уақытымен.</summary>
public class SkBuf
{
    public const int CAP = 48;
    public float[] t = new float[CAP]; public string[] d = new string[CAP], b = new string[CAP]; public int n;
    public string last = "N"; public float freshT = -9; public bool fresh;
    public void Clear() { n = 0; last = "N"; fresh = false; }
    public void Shift() { for (int i = 1; i < n; i++) { t[i - 1] = t[i]; d[i - 1] = d[i]; b[i - 1] = b[i]; } n--; }
    public void Push(float tt, string dd, string bb) { if (n >= CAP) Shift(); t[n] = tt; d[n] = dd; b[n] = bb; n++; }
}

public static class Skills
{
    public static Lst<SkDef> All;
    static SkDef[] sorted;
    public static readonly string[] KINDS = { "wind", "fire", "thunder", "earth", "ice", "spirit", "blade", "heal" };
    public static readonly string[] KIND_COL = { "#9ff0dc", "#ff8a3a", "#fff27a", "#d6a060", "#b4ecff", "#c8a8ff", "#ffe2a0", "#8dff9a" };
    public static readonly string[] KIND_NAME = { "Жел", "От", "Найзағай", "Жер", "Мұз", "Рух", "Жүз", "Шипа" };
    public static bool TestUnlock = false;

    public static void Init()
    {
        if (All != null) return;
        All = new Lst<SkDef>();
        JNode S = Db.J("SKILLS");
        for (int i = 0; i < S.Count(); i++)
        {
            JNode j = S.At(i); SkDef d = new SkDef();
            d.id = j.S("id"); d.kind = j.S("kind"); d.name = j.S("name"); d.desc = j.S("desc"); d.lvl = j.Ik("lvl"); d.cd = j.Fk("cd"); d.cost = j.Fk("cost");
            d.air = j.Bk("air"); d.ult = j.Bk("ult"); d.idx = i;
            JNode q = j.Get("seq"); d.seq = new string[q.Count()]; for (int k = 0; k < d.seq.Length; k++) d.seq[k] = q.At(k).Str();
            All.Add(d);
        }
        sorted = new SkDef[All.Count()];
        for (int i = 0; i < sorted.Length; i++) sorted[i] = All.Get(i);
        for (int i = 1; i < sorted.Length; i++) { SkDef v = sorted[i]; int k = i - 1; while (k >= 0 && sorted[k].seq.Length < v.seq.Length) { sorted[k + 1] = sorted[k]; k--; } sorted[k + 1] = v; }
    }
    public static SkDef By(string id) { Init(); for (int i = 0; i < All.Count(); i++) if (U.Eq(All.Get(i).id, id)) return All.Get(i); return null; }
    public static int KindIdx(string k) { for (int i = 0; i < KINDS.Length; i++) if (U.Eq(KINDS[i], k)) return i; return 6; }
    public static bool Unlocked(SkDef d) { return TestUnlock || Progress.TestUnlockAll || Progress.Level() >= d.lvl; }
    static bool IsBtn(string s) { return U.Eq(s, "J") || U.Eq(s, "K") || U.Eq(s, "L"); }
    static bool TokOk(string tok, string want, bool exact)
    {
        if (U.Eq(tok, want)) return true;
        return !exact && U.Len(want) == 1 && U.Len(tok) == 2 && U.IndexOf(tok, want) >= 0;
    }

    // ================= енгізу =================
    /// <summary>Әр кадр: ағымдағы бағыт белгісі (N, F, B, D, U, DF, DB, UF, UB) және басылған батырмалар.</summary>
    public static void Feed(SkBuf B, float now, string tok, bool j, bool k, bool l)
    {
        if (!U.Eq(tok, B.last)) { B.last = tok; Push(B, now, tok, null); }
        if (j) { Push(B, now, tok, "J"); B.fresh = true; B.freshT = now; }
        if (k) { Push(B, now, tok, "K"); B.fresh = true; B.freshT = now; }
        if (l) { Push(B, now, tok, "L"); B.fresh = true; B.freshT = now; }
        while (B.n > 40 || (B.n > 0 && now - B.t[0] > 3)) B.Shift();
    }
    static void Push(SkBuf B, float now, string d, string b) { B.Push(now, d, b); }

    /// <summary>Тізбекті буфердің соңынан іздейді (веб: skMatch). Табылса — уақыт аралығы, әйтпесе −1.</summary>
    public static float Match(SkBuf E, string[] seq)
    {
        int n = seq.Length, L = E.n;
        if (L == 0) return -1;
        string lb = E.b[L - 1];
        if (lb == null || !U.Eq(lb, seq[n - 1])) return -1;
        float tNow = E.t[L - 1], win = n <= 3 ? 0.95f : 0.95f + (n - 3) * 0.2f;
        if (n == 2 && !IsBtn(seq[0]) && TokOk(E.d[L - 1], seq[0], false)) return 0;
        int k = L - 2; float first = tNow;
        for (int i = n - 2; i >= 0; i--)
        {
            string want = seq[i]; bool isB = IsBtn(want), exact = (i > 0 && U.Eq(seq[i - 1], want)) || U.Eq(seq[i + 1], want);
            bool found = false;
            for (; k >= 0; k--)
            {
                if (tNow - E.t[k] > win) return -1;
                string eb = E.b[k];
                if (isB) { if (eb != null) { if (U.Eq(eb, want)) { found = true; first = E.t[k]; k--; break; } return -1; } continue; }
                if (eb != null) return -1;
                if (TokOk(E.d[k], want, exact)) { found = true; first = E.t[k]; k--; break; }
            }
            if (!found) return -1;
        }
        return tNow - first;
    }

    // ================= көмекшілер =================
    public static SkState S(Fighter f) { if (f.sks == null) f.sks = new SkState(); return f.sks; }
    public static bool Castable(Fighter f, SkDef d)
    {
        if (f.charging || f.state == Fighter.DEAD) return false;
        int st = f.state; bool og = f.OnGround();
        if (d.air) return !og && (st == Fighter.JUMP || (st == Fighter.ATTACK && f.cm != null && f.cm.air && !f.cm.sk && f.st >= f.cm.w));
        if (!og) return false;
        if (st == Fighter.IDLE || st == Fighter.WALK || st == Fighter.CROUCH || st == Fighter.DASH) return true;
        return st == Fighter.ATTACK && f.cm != null && !f.cm.sk && !f.cm.special && f.st >= f.cm.w * 0.8f;
    }
    /// <summary>Анимацияға арналған жалған соққы (веб: skAnim).</summary>
    public static Move Anim(Fighter f, string bs, float w, float a, float r)
    {
        Move m = Move.Base(bs).Copy();
        m.sk = true; m.leap = 1; m.lunge = 0; m.wave = false; m.thr = false; m.special = false; m.dmg = 0; m.hits = 1; m.reach = 220;
        m.w = w; m.a = a; m.r = r;
        f.cm = m; f.comboIdx = -1; f.hitDone = false; f.hitIdx = 0; f.queued = false; f.lunged = false; f.waved = false;
        f.state = Fighter.ATTACK; f.st = 0;
        if (f.sk != null) f.sk.cm = m;
        return m;
    }
    static bool Near(Fighter f, Fighter o, float reach, float dy, float back)
    {
        if (o == null || o.state == Fighter.DEAD || o.state == Fighter.DOWN) return false;
        float dx = o.x - f.x;
        if (dx * f.facing < -back) return false;
        return Mathf.Abs(dx) <= reach && Mathf.Abs(o.y - f.y) < dy;
    }
    static Move tmpM = new Move();
    /// <summary>Әдіс соққысы. Қайтарады: "hit" | "block" | "parry" | "evade" | "none".</summary>
    static string Deal(Fighter f, Fighter o, float dmg, float knock, bool heavy, bool down, float hitstop, bool unblock, bool special, string height, bool anti, int dir)
    {
        if (o == null || o.state == Fighter.DEAD) return "none";
        if (dir == 0) dir = o.x >= f.x ? 1 : -1;
        Move m = new Move(); m.sk = true; m.height = height; m.knock = knock; m.hitstop = hitstop; m.heavy = heavy; m.down = down; m.unblock = unblock; m.special = special; m.anti = anti;
        m.dmg = dmg * (f.ai ? Mathf.Min(f.dmgMul, 1.3f) * 0.85f : f.dmgMul);
        o.skRes = "none";
        o.TakeHit(m, dir, true, f);
        return o.skRes;
    }
    static string DealS(Fighter f, Fighter o, float dmg, float knock, bool heavy, bool down, float hitstop) { return Deal(f, o, dmg, knock, heavy, down, hitstop, false, false, "mid", false, 0); }
    public static void Stun(Fighter o, float t) { if (o != null && o.state != Fighter.DEAD) { SkState T = S(o); T.stun = Mathf.Max(T.stun, t); } }
    public static void Call(Fighter f, string text, string kind, bool fail) { SkState T = S(f); T.callText = text; T.callKind = kind; T.callT = 0; T.callFail = fail; }
    static void Fx(Fighter f, string kind, float x, float y, float dir, float power) { if (f.game != null) f.game.FxF(Match.FX_SKILL, x, y, dir, Mathf.RoundToInt(power * 100), f, KindIdx(kind)); }
    static SkProj Proj(Fighter f, string kind, float x, float y, float vx, float life, float r, float dmg, float knock, string height)
    {
        SkProj p = new SkProj(); float pj = Db.PaceK("proj");
        p.kind = kind; p.owner = f; p.x = x; p.y = y; p.vx = vx * pj; p.life = life / pj; p.r = r; p.dmg = dmg; p.knock = knock; p.height = height; p.gap = 0.15f / pj;
        if (f.game != null) f.game.skProj.Add(p);
        return p;
    }
    static float Toward(float v, float t, float d) { return Pz.Toward(v, t, d); }
    static readonly string[] OTTY_B = { "l1", "l2", "heavy" };
    static readonly float[] OTTY_W = { 0.06f, 0.05f, 0.12f }, OTTY_A = { 0.08f, 0.08f, 0.12f }, OTTY_R = { 0.07f, 0.07f, 0.42f }, OTTY_D = { 6, 6, 14 }, OTTY_K = { 160, 160, 580 };
    static void OttyPhase(Fighter f, int i) { Move m = Anim(f, OTTY_B[i], OTTY_W[i], OTTY_A[i], OTTY_R[i]); m.after = i == 2; m.heavy = i == 2; }

    // ================= бастау / аяқтау =================
    public static void Start(Fighter f, SkDef d, bool isAi)
    {
        SkState T = S(f);
        SkRun s = new SkRun(); s.def = d; f.sk = s;
        if (!isAi) f.rux = Mathf.Max(0, f.rux - d.cost);
        T.cd[d.idx] = d.cd * (isAi ? 1.8f : 1);
        f.charging = false; f.push = 0;
        Begin(f, s);
        Call(f, d.name, d.kind, false);
        Fx(f, d.kind, f.x + f.facing * 30 * f.sc / Db.S, f.y - 140 * f.sc / Db.S, f.facing, 0.7f);
        if (f.game != null) f.game.SkillCast(f, d, isAi);
    }
    public static void End(Fighter f, bool natural)
    {
        SkRun s = f.sk; if (s == null) return;
        f.sk = null; f.charging = false; S(f).parry = 0;
        if (U.Eq(s.def.id, "alpamys") || U.Eq(s.def.id, "tulpar") || U.Eq(s.def.id, "jalyn")) f.inv = Mathf.Min(f.inv, 0.08f);
        if (natural && f.state == Fighter.ATTACK && f.cm == s.cm) { f.Set(f.OnGround() ? Fighter.IDLE : Fighter.JUMP); if (f.OnGround()) f.vx = 0; }
    }
    /// <summary>Балалар режимі: бір батырмамен әдіс (веб: kidPick) — дайын тұрған ең мықты үш әдістің бірі, жаны аз болса — шипа.</summary>
    public static SkDef KidPick(Fighter f)
    {
        Init();
        SkState T = S(f);
        SkDef heal = null; SkDef[] top = new SkDef[3]; int tn = 0;
        for (int i = 0; i < All.Count(); i++)
        {
            SkDef d = All.Get(i);
            if (!Unlocked(d) || !Castable(f, d) || T.cd[d.idx] > 0 || f.rux < d.cost || U.Eq(d.id, "qalqan")) continue;
            if (U.Eq(d.kind, "heal")) { if (heal == null) heal = d; continue; }
            // ең жоғары деңгейлі үшеуін сақтаймыз
            if (tn < 3) { top[tn++] = d; }
            else
            {
                int lo = 0; for (int q2 = 1; q2 < 3; q2++) if (top[q2].lvl < top[lo].lvl) lo = q2;
                if (d.lvl > top[lo].lvl) top[lo] = d;
            }
        }
        if (heal != null && f.hp < f.maxHp * 0.45f) return heal;
        if (tn == 0) return null;
        return top[Mathf.Min(tn - 1, (int)(U.Rand() * tn))];
    }
    public static bool KidCast(Fighter f)
    {
        if (f == null || f.sk != null || f.state == Fighter.DEAD) return false;
        SkDef d = KidPick(f);
        if (d != null) { Start(f, d, false); return true; }
        return false;
    }

    /// <summary>Ойыншының енгізуі (веб: skTryInput).</summary>
    public static bool TryInput(Fighter f, SkBuf B)
    {
        Init();
        SkDef best = null; float bestSpan = 99; SkDef firstC = null;
        SkState T = S(f);
        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < sorted.Length; i++)
            {
                SkDef d = sorted[i];
                if (!Unlocked(d) || !Castable(f, d)) continue;
                float span = Match(B, d.seq); if (span < 0) continue;
                if (pass == 0) { if (firstC == null || d.seq.Length > firstC.seq.Length || (d.seq.Length == firstC.seq.Length && span < bestSpan)) { firstC = d; bestSpan = span; } continue; }
                if (T.cd[d.idx] > 0 || f.rux < d.cost) continue;
                if (best == null || d.seq.Length > best.seq.Length) best = d;
            }
        }
        if (best != null) { Start(f, best, false); return true; }
        if (firstC != null && (firstC.seq.Length >= 3 || firstC.ult))
        {
            Call(f, f.rux < firstC.cost ? (firstC.ult ? "Рух толмаған" : "Рух жетпейді") : "Әлі дайын емес", firstC.kind, true);
            if (f.game != null) f.game.SkillFail(f);
        }
        return false;
    }
    public static void Parry(Fighter f, Fighter from)
    {
        SkState T = S(f); SkRun s = f.sk; T.parry = 0; f.inv = 0.35f;
        float x = f.x + f.facing * 60 * f.sc / Db.S, y = f.y - 170 * f.sc / Db.S;
        Fx(f, "blade", x, y, f.facing, 1);
        if (f.game != null) { f.game.Hitstop(0.12f, 6); f.game.SkillParry(f); }
        Call(f, "ҚАЙТАРЫЛДЫ!", "blade", false);
        if (s != null && U.Eq(s.def.id, "qalqan")) { s.counter = true; s.ct = 0; s.cHit = false; s.target = from; Anim(f, "heavy", 0.07f, 0.1f, 0.32f); }
    }

    // ================= әр әдістің механикасы (веб: SK_IMPL) =================
    static void Begin(Fighter f, SkRun s)
    {
        string id = s.def.id; Match g = f.game; Fighter o = f.opp; Move m;
        if (U.Eq(id, "kokzhal")) { m = Anim(f, "akick", 0.08f, 0.44f, 0.2f); m.air = true; m.after = true; m.foot = true; f.vx = f.facing * 780; f.vy = -660; f.y -= 2; Dust(f, f.x, -f.facing, 8); }
        else if (U.Eq(id, "sadaq")) { Anim(f, "thrust", 0.16f, 0.1f, 0.26f); f.vx = -f.facing * 300; }
        else if (U.Eq(id, "qalqan")) { m = Anim(f, "throw", 0.04f, 0.42f, 0.16f); m.pw = "guard"; m.ps = "guard"; S(f).parry = 0.46f; f.vx = 0; }
        else if (U.Eq(id, "burkit")) { m = Anim(f, "akick", 0.05f, 0.7f, 0.1f); m.air = true; m.after = true; m.foot = true; f.vx = f.facing * 560; f.vy = 1150; }
        else if (U.Eq(id, "shapagat")) { m = Anim(f, "rise", 0.2f, 0.3f, 0.3f); m.pw = "guard"; m.ps = "victory"; f.vx = 0; S(f).hot = 3; if (g != null) g.flash = Mathf.Max(g.flash, 0.08f); }
        else if (U.Eq(id, "dauyl") || U.Eq(id, "ayaz")) { Anim(f, "thrust", 0.14f, U.Eq(id, "dauyl") ? 0.12f : 0.1f, 0.3f); f.vx = 0; }
        else if (U.Eq(id, "tulpar")) { m = Anim(f, "thrust", 0.08f, 0.44f, 0.26f); m.pw = "dashF"; m.ps = "thrustS"; m.after = true; f.charging = true; f.inv = 0.62f; s.n = 0; Dust(f, f.x, -f.facing, 12); }
        else if (U.Eq(id, "arqan")) { Anim(f, "thrust", 0.12f, 0.3f, 0.3f); f.vx = 0; }
        else if (U.Eq(id, "jer")) { Anim(f, "heavy", 0.22f, 0.1f, 0.42f); f.vx = 0; }
        else if (U.Eq(id, "jalyn")) { m = Anim(f, "rise", 0.04f, 0.28f, 0.3f); m.air = true; m.anti = true; m.after = true; f.inv = 0.22f; f.vy = -840; f.vx = f.facing * 260; f.y -= 2; }
        else if (U.Eq(id, "naizagai")) { m = Anim(f, "rise", 0.3f, 0.3f, 0.32f); m.pw = "guard"; m.ps = "victory"; f.vx = 0; s.tx = o != null ? o.x : f.x + f.facing * 300; }
        else if (U.Eq(id, "quiyn")) { m = Anim(f, "spin", 0.1f, 0.78f, 0.3f); m.pw = "heavyW"; m.ps = "lightS"; m.alt = "light2S"; m.hits = 5; m.spin = true; m.after = true; f.vx = 0; s.n = 0; }
        else if (U.Eq(id, "aiqai")) { m = Anim(f, "heavy", 0.22f, 0.32f, 0.36f); m.pw = "heavyW"; m.ps = "victory"; f.vx = 0; }
        else if (U.Eq(id, "aruaq")) { m = Anim(f, "rise", 0.25f, 0.2f, 0.3f); m.pw = "guard"; m.ps = "victory"; f.vx = 0; SkState T = S(f); T.cloneT = 9; T.cloneN = 3; T.cloneFade = 0; T.cloneQ = 0; if (g != null) g.flash = Mathf.Max(g.flash, 0.1f); }
        else if (U.Eq(id, "otty")) { s.ph = 0; OttyPhase(f, 0); }
        else if (U.Eq(id, "boran")) { m = Anim(f, "rise", 0.3f, 0.3f, 0.32f); m.pw = "guard"; m.ps = "victory"; f.vx = 0; }
        else if (U.Eq(id, "alpamys"))
        {
            m = Anim(f, "thrust", 0.2f, 0.3f, 0.2f); m.pw = "dashF"; m.ps = "thrustS"; m.special = true; m.after = true;
            f.inv = 3.5f; s.ph = 0; if (g != null) { g.SpecialFx(f); g.dark = Mathf.Max(g.dark, 0.7f); }
        }
    }
    static void Dust(Fighter f, float x, float dir, int n) { if (f.game != null) f.game.Fx(Match.FX_DUST, x, Db.GROUND, dir, n, f); }

    /// <summary>false — әдіс аяқталды.</summary>
    static bool Tick(Fighter f, SkRun s, float dt)
    {
        string id = s.def.id; Fighter o = f.opp; Match g = f.game; float sc = f.sc / Db.S;
        if (U.Eq(id, "kokzhal"))
        {
            if (!s.hit && s.t > 0.05f && Near(f, o, 150 * sc, 240, 30))
            {
                s.hit = true; string r = DealS(f, o, 12, 470, true, true, 0.09f);
                if (U.Eq(r, "hit")) Fx(f, "spirit", o.x, o.y - 160 * o.sc / Db.S, f.facing, 1);
            }
            return true;
        }
        if (U.Eq(id, "sadaq"))
        {
            f.vx = Toward(f.vx, 0, 1500 * dt);
            if (!s.shot && s.t >= 0.15f && g != null)
            {
                s.shot = true; float mul = f.ai ? Mathf.Min(f.dmgMul, 1.3f) * 0.85f : f.dmgMul;
                g.ShootRaw(f, f.x + f.facing * 70 * sc, f.y - 168 * sc, 1.8f, 0, 1150, 7 * mul, true);
                g.ShootRaw(f, f.x + f.facing * 70 * sc, f.y - 46 * sc, 1.9f, 0.12f, 1150, 7 * mul, false);
                Fx(f, "wind", f.x + f.facing * 80 * sc, f.y - 150 * sc, f.facing, 0.5f);
            }
            return true;
        }
        if (U.Eq(id, "qalqan"))
        {
            SkState T0 = S(f);
            if (!s.counter) { T0.parry = Mathf.Max(0, 0.46f - s.t); return true; }
            s.ct += dt;
            if (!s.cHit && s.ct >= 0.07f)
            {
                s.cHit = true; Fighter tg = s.target;
                if (tg != null && Mathf.Abs(tg.x - f.x) < 300 * sc)
                {
                    string r = DealS(f, tg, 14, 520, true, false, 0.1f);
                    if (U.Eq(r, "hit")) { Stun(tg, 0.7f); Fx(f, "blade", tg.x, tg.y - 170 * tg.sc / Db.S, f.facing, 1); }
                }
            }
            return true;
        }
        if (U.Eq(id, "burkit"))
        {
            if (!s.hit) { f.vx = f.facing * 560; f.vy = 1150; }
            if (!s.hit && Near(f, o, 140 * sc, 250, 30))
            {
                s.hit = true; string r = DealS(f, o, 11, 400, true, true, 0.08f);
                if (!U.Eq(r, "none")) { f.vy = -560; f.vx = -f.facing * 240; }
                if (U.Eq(r, "hit")) Fx(f, "wind", o.x, o.y - 150 * o.sc / Db.S, f.facing, 0.9f);
            }
            if (!s.landed && f.OnGround() && s.t > 0.03f) { s.landed = true; Dust(f, f.x, 0, 10); Fx(f, "wind", f.x, Db.GROUND - 10, f.facing, 0.5f); }
            return true;
        }
        if (U.Eq(id, "shapagat")) return true;
        if (U.Eq(id, "dauyl"))
        {
            if (!s.shot && s.t >= 0.13f)
            {
                s.shot = true; SkProj p = Proj(f, "wind", f.x + f.facing * 90 * sc, Db.GROUND - 120, f.facing * 540, 1.7f, 50, 4.4f, 140, "mid");
                p.hits = 3; p.gap = 0.17f / Db.PaceK("proj"); p.lastKnock = 440; p.down = true; p.heavy = true;
            }
            return true;
        }
        if (U.Eq(id, "ayaz"))
        {
            if (!s.shot && s.t >= 0.13f) { s.shot = true; SkProj p = Proj(f, "ice", f.x + f.facing * 80 * sc, f.y - 150 * sc, f.facing * 980, 1.3f, 24, 7, 160, "mid"); p.onHit = 1; }
            return true;
        }
        if (U.Eq(id, "tulpar"))
        {
            if (s.t < 0.5f) { f.vx = f.facing * 1180; f.inv = Mathf.Max(f.inv, 0.06f); if (U.Rand() < 0.7f) Dust(f, f.x, -f.facing, 2); }
            else { f.charging = false; f.vx = Toward(f.vx, 0, 4200 * dt); }
            s.hcd -= dt;
            if (o != null && s.t > 0.05f && s.t < 0.56f && s.n < 3 && s.hcd <= 0 && Mathf.Abs(o.x - f.x) < 100 * sc && Mathf.Abs(o.y - f.y) < 170 && o.state != Fighter.DEAD && o.state != Fighter.DOWN)
            {
                s.n++; s.hcd = 0.05f; bool last = s.n == 3;
                string r = Deal(f, o, last ? 7 : 4, last ? 520 : 90, last, last, last ? 0.08f : 0.03f, false, false, "mid", false, f.facing);
                if (U.Eq(r, "hit")) Fx(f, "spirit", o.x, o.y - 140 * o.sc / Db.S, f.facing, last ? 1 : 0.5f);
                if (U.Eq(r, "block") || U.Eq(r, "parry")) s.n = 3;
            }
            return true;
        }
        if (U.Eq(id, "arqan"))
        {
            if (!s.shot && s.t >= 0.11f)
            {
                s.shot = true; SkProj p = Proj(f, "lasso", f.x + f.facing * 58 * sc, f.y - 175 * sc, f.facing * 1450, 0.4f, 30, 5, 0, "high");
                p.unblock = true; p.onHit = 2;
            }
            return true;
        }
        if (U.Eq(id, "jer"))
        {
            if (!s.slam && s.t >= 0.21f)
            {
                s.slam = true; float x = f.x + f.facing * 70 * sc;
                Dust(f, x, 0, 12); Fx(f, "earth", x, Db.GROUND - 10, f.facing, 1); if (g != null) g.Hitstop(0.03f, 10);
                SkProj p = Proj(f, "earth", x + f.facing * 20, Db.GROUND - 40, f.facing * 700, 1.1f, 44, 13, 440, "low");
                p.lastKnock = 440; p.down = true; p.heavy = true;
            }
            return true;
        }
        if (U.Eq(id, "jalyn"))
        {
            if (!s.hit && s.t > 0.02f && s.t < 0.34f && Near(f, o, 175 * sc, 300, 30))
            {
                s.hit = true; string r = Deal(f, o, 13, 430, true, true, 0.09f, false, false, "mid", true, 0);
                if (U.Eq(r, "hit")) { S(o).burn = 3; Fx(f, "fire", o.x, o.y - 170 * o.sc / Db.S, f.facing, 1); }
            }
            return true;
        }
        if (U.Eq(id, "naizagai"))
        {
            if (s.t < 0.4f && o != null) s.tx = Pz.Lerp(s.tx, o.x, 1 - Mathf.Exp(-5 * dt));
            if (!s.struck && s.t >= 0.56f)
            {
                s.struck = true; if (g != null) g.Bolt(s.tx, 2);
                if (o != null && Mathf.Abs(o.x - s.tx) < 100 && o.state != Fighter.DEAD && o.state != Fighter.DOWN)
                {
                    string r = Deal(f, o, 16, 220, true, false, 0.1f, false, true, "mid", false, o.x >= f.x ? 1 : -1);
                    if (U.Eq(r, "hit")) Stun(o, 0.7f);
                }
            }
            return true;
        }
        if (U.Eq(id, "quiyn"))
        {
            if (s.t > 0.08f && s.t < 0.9f)
            {
                if (o != null && o.state != Fighter.DEAD && Mathf.Abs(o.x - f.x) < 360 && Mathf.Abs(o.x - f.x) > 70) o.x += (f.x > o.x ? 1 : -1) * 300 * dt;
                s.hcd -= dt;
                if (s.hcd <= 0 && s.n < 5 && s.t > 0.12f)
                {
                    s.hcd = 0.15f;
                    if (o != null && Mathf.Abs(o.x - f.x) < 205 * sc && Mathf.Abs(o.y - f.y) < 220 && o.state != Fighter.DEAD && o.state != Fighter.DOWN)
                    {
                        s.n++; bool last = s.n == 5;
                        string r = DealS(f, o, last ? 7 : 3.4f, last ? 540 : 50, last, last, last ? 0.08f : 0.03f);
                        if (U.Eq(r, "hit")) Fx(f, "wind", o.x, o.y - 150 * o.sc / Db.S, f.facing, last ? 0.9f : 0.35f);
                    }
                }
            }
            return true;
        }
        if (U.Eq(id, "aiqai"))
        {
            if (!s.roar && s.t >= 0.21f)
            {
                s.roar = true; if (g != null) { g.Hitstop(0.08f, 12); g.Fx(Match.FX_PHASE, f.x, f.y - 150, f.facing, 30, f); g.SkillRoar(f); }
                if (o != null && Mathf.Abs(o.x - f.x) < 440 && Mathf.Abs(o.y - f.y) < 300 && o.state != Fighter.DEAD && o.state != Fighter.DOWN)
                {
                    string r = Deal(f, o, 8, 760, true, false, 0.05f, true, false, "mid", false, 0);
                    if (U.Eq(r, "hit")) Stun(o, 1.0f);
                }
            }
            return true;
        }
        if (U.Eq(id, "aruaq")) return true;
        if (U.Eq(id, "otty"))
        {
            int i = s.ph;
            f.vx = f.st < OTTY_W[i] + OTTY_A[i] ? f.facing * 140 : Toward(f.vx, 0, 2000 * dt);
            if (!s.hitDone && f.st >= OTTY_W[i])
            {
                s.hitDone = true;
                if (i == 2) { float x = f.x + f.facing * 120 * sc; Fx(f, "fire", x, f.y - 120, f.facing, 1.3f); if (g != null) g.Hitstop(0.06f, 12); }
                if (Near(f, o, (i == 2 ? 270 : 235) * sc, 220, i == 2 ? 200 : 30))
                {
                    string r = DealS(f, o, OTTY_D[i], OTTY_K[i], i == 2, i == 2, i == 2 ? 0.1f : 0.05f);
                    if (U.Eq(r, "hit")) { Fx(f, "fire", o.x, o.y - 160 * o.sc / Db.S, f.facing, i == 2 ? 1 : 0.5f); if (i == 2) S(o).burn = 3.2f; }
                }
            }
            if (i < 2 && f.st >= OTTY_W[i] + OTTY_A[i] + OTTY_R[i]) { s.ph++; s.hitDone = false; OttyPhase(f, s.ph); }
            return true;
        }
        if (U.Eq(id, "boran"))
        {
            if (!s.cast && s.t >= 0.3f)
            {
                s.cast = true;
                if (g != null) { g.SetWeather("snow", f); g.weatherT = 4.5f; g.weatherMax = 4.5f; }
                Fx(f, "ice", f.x, f.y - 200, f.facing, 1.2f);
                if (o != null && o.state != Fighter.DEAD)
                {
                    string r = Deal(f, o, 6, 120, false, false, 0.06f, true, false, "mid", false, 0);
                    SkState T = S(o); T.slow = Mathf.Max(T.slow, 4.5f); T.frost = 4.5f;
                    if (U.Eq(r, "hit")) { T.ice = 0.6f; Stun(o, 0.6f); }
                    Fx(f, "ice", o.x, o.y - 150 * o.sc / Db.S, -f.facing, 1);
                }
            }
            return true;
        }
        if (U.Eq(id, "alpamys"))
        {
            if (s.ph == 0)
            {
                if (s.t > 0.18f) f.vx = f.facing * 1350;
                if (o != null && s.t > 0.18f && Mathf.Abs(o.x - f.x) < 150 * sc && o.state != Fighter.DEAD)
                {
                    f.vx = 0; s.ph = 1; s.k = 0; s.hcd = 0;
                    string r = Deal(f, o, 5, 30, false, false, 0.06f, false, true, "mid", false, 0);
                    if (U.Eq(r, "block") || U.Eq(r, "parry") || U.Eq(r, "none")) { s.ph = 9; f.vx = -f.facing * 300; Anim(f, "heavy", 0.05f, 0.1f, 0.35f); return true; }
                    Stun(o, 0.5f);
                }
                else if (s.t > 0.62f) { s.ph = 9; f.vx = 0; Anim(f, "heavy", 0.05f, 0.1f, 0.3f); }
                return true;
            }
            if (s.ph == 1)
            {
                if (o != null) { f.x = Pz.Lerp(f.x, o.x - f.facing * 125 * sc, 0.5f); o.push = 0; }
                s.hcd -= dt;
                if (s.hcd <= 0)
                {
                    s.hcd = 0.11f; s.k++;
                    Move m = Anim(f, s.k % 2 == 1 ? "l1" : "l2", 0.03f, 0.06f, 0.04f); m.special = true; m.after = true;
                    if (o != null && o.state != Fighter.DEAD)
                    {
                        Deal(f, o, 4, 30, false, false, 0.035f, true, true, "mid", false, 0);
                        Stun(o, 0.4f); Fx(f, s.k % 2 == 1 ? "blade" : "thunder", o.x, o.y - Pz.Rand(120, 200) * o.sc / Db.S, f.facing, 0.45f);
                    }
                    if (s.k >= 7) { s.ph = 2; Move cm2 = Anim(f, "crush", 0.22f, 0.1f, 0.5f); cm2.pw = "airW"; cm2.ps = "heavyS"; cm2.special = true; s.fin = false; }
                }
                return true;
            }
            if (s.ph == 2)
            {
                if (!s.fin && f.st >= 0.2f)
                {
                    s.fin = true; float x = f.x + f.facing * 110 * sc;
                    if (g != null) { g.Bolt(o != null ? o.x : x, 6); g.Hitstop(0.16f, 18); }
                    Dust(f, x, 0, 16); Fx(f, "earth", x, Db.GROUND - 10, f.facing, 1.4f); Fx(f, "blade", x, f.y - 150, f.facing, 1.4f);
                    if (o != null && o.state != Fighter.DEAD) Deal(f, o, 18, 780, true, true, 0.18f, true, true, "mid", false, 0);
                }
                return true;
            }
            f.vx = Toward(f.vx, 0, 2400 * dt);
            return true;
        }
        return true;
    }

    // ================= әр кадр (веб: skPre / skPost / skStep) =================
    public static void Pre(Fighter f, float dt)
    {
        Match g = f.game; if (g == null) return;
        SkState T = S(f);
        for (int i = 0; i < T.cd.Length; i++) if (T.cd[i] > 0) T.cd[i] -= dt;
        T.callT += dt;
        bool alive = f.state != Fighter.DEAD && f.hp > 0;
        if (T.hot > 0) { T.hot -= dt; if (alive) f.hp = Mathf.Min(f.maxHp, f.hp + 9 * dt); }
        if (T.burn > 0) { T.burn -= dt; if (alive && f.hp > 1) f.hp = Mathf.Max(1, f.hp - 2.4f * dt); }
        if (T.frost > 0) { T.frost -= dt; if (alive && f.hp > 1) f.hp = Mathf.Max(1, f.hp - 2.6f * dt); }
        if (T.ice > 0) T.ice -= dt;
        if (T.slow > 0 && alive)
        {
            if (!T.slowOn) { T.slowOn = true; f.speed *= 0.55f; f.atkSpd *= 0.75f; }
            T.slow -= dt;
        }
        if (T.slowOn && (T.slow <= 0 || !alive)) { T.slowOn = false; T.slow = 0; f.speed /= 0.55f; f.atkSpd /= 0.75f; }
        if (T.pullT > 0) { T.pullT -= dt; if (alive) f.x = Pz.Lerp(f.x, T.pullTo, 1 - Mathf.Exp(-14 * dt)); }
        if (T.stun > 0)
        {
            T.stun -= dt;
            if (alive && f.state != Fighter.DOWN) { if (f.sk != null) End(f, false); if (f.state != Fighter.HIT) { f.state = Fighter.HIT; f.st = 0; } if (f.st > 0.2f) f.st = 0.2f; f.vx = 0; }
        }
        if (T.cloneT > 0 || T.cloneQ > 0)
        {
            T.cloneT -= dt;
            for (int i = T.cloneQ - 1; i >= 0; i--)
            {
                T.cloneAt[i] -= dt;
                if (T.cloneAt[i] <= 0)
                {
                    CloneStrike(f, T.cloneCm[i]);
                    for (int q = i + 1; q < T.cloneQ; q++) { T.cloneAt[q - 1] = T.cloneAt[q]; T.cloneCm[q - 1] = T.cloneCm[q]; }
                    T.cloneQ--;
                }
            }
            if (T.cloneT <= 0 || (T.cloneN <= 0 && T.cloneQ == 0)) { T.cloneFade += dt; if (T.cloneFade > 0.5f) { T.cloneT = 0; T.cloneN = 0; T.cloneQ = 0; } }
        }
        if (f.sk != null)
        {
            if (f.state != Fighter.ATTACK || f.cm != f.sk.cm) End(f, false);
            else { float sdt = dt * Db.PaceK("skill"); f.sk.t += sdt; if (!Tick(f, f.sk, sdt)) End(f, true); }
        }
        if (f.sk == null && f.active && g.phase == Match.FIGHT && T.stun <= 0 && alive)
        {
            if (f.ai)
            {
                if (f.skWant != null)
                {
                    SkDef d = By(f.skWant);
                    if (d != null && (f.state == Fighter.IDLE || f.state == Fighter.WALK || f.state == Fighter.CROUCH) && f.OnGround()) { f.skWant = null; Start(f, d, true); }
                    else if ((T.wantT += dt) > 0.6f) { f.skWant = null; T.wantT = 0; }
                }
            }
            else if (f == g.player && g.skBuf.fresh && g.clock - g.skBuf.freshT < 0.15f) { TryInput(f, g.skBuf); g.skBuf.fresh = false; }
        }
    }
    static void CloneStrike(Fighter f, Move cm)
    {
        Fighter o = f.opp; if (o == null || o.state == Fighter.DEAD) return;
        float cx = f.x - f.facing * 50 * f.sc / Db.S;
        Fx(f, "spirit", cx + f.facing * 100, f.y - 160 * f.sc / Db.S, f.facing, 0.6f);
        if (Mathf.Abs(o.x - cx) < (cm.reach > 0 ? cm.reach : 220) + 70 && Mathf.Abs(o.y - f.y) < 200 && (o.x - cx) * f.facing > -20)
        {
            string r = Deal(f, o, Mathf.Max(4, (cm.dmg > 0 ? cm.dmg : 6) / Mathf.Max(0.5f, f.dmgMul) * 0.6f), cm.knock > 0 ? cm.knock : 200, cm.heavy, cm.down, 0.05f, false, false, cm.thr ? "mid" : cm.height, false, 0);
            if (U.Eq(r, "hit")) Fx(f, "spirit", o.x, o.y - 150 * o.sc / Db.S, f.facing, 0.7f);
        }
    }
    /// <summary>Аруақ: әр қалыпты соққыдан кейін көлеңке қайталайды (веб: startAttack орамасы).</summary>
    public static void OnAttack(Fighter f)
    {
        SkState T = f.sks; if (T == null || T.cloneT <= 0 || T.cloneN <= 0 || f.cm == null || f.cm.sk) return;
        if (T.cloneQ >= 8) return;
        T.cloneN--; T.cloneAt[T.cloneQ] = 0.24f; T.cloneCm[T.cloneQ] = f.cm; T.cloneQ++;
    }

    public static void Step(Match g, float dt)
    {
        for (int i = g.skProj.Count() - 1; i >= 0; i--)
        {
            SkProj p = g.skProj.Get(i);
            p.t += dt; p.life -= dt; p.hcd -= dt; p.x += p.vx * dt; p.y += p.vy * dt;
            if (U.Eq(p.kind, "earth"))
            {
                if (p.lastSpk < -9999) p.lastSpk = p.x - (p.vx > 0 ? 1 : -1) * 50;
                if (Mathf.Abs(p.x - p.lastSpk) > 46) { p.lastSpk = p.x; g.Spike(p.x + Pz.Rand(-8, 8), Pz.Rand(64, 104), Pz.Rand(18, 28), Pz.Rand(-0.25f, 0.25f) * (p.vx > 0 ? 1 : -1)); }
            }
            Fighter o = p.owner != null ? p.owner.opp : null;
            if (o != null && p.hits > 0 && p.hcd <= 0 && o.state != Fighter.DEAD && o.state != Fighter.DOWN)
            {
                float sc = o.sc / Db.S, top = o.y - 250 * sc;
                bool inside = Mathf.Abs(o.x - p.x) <= p.r + 34 * sc && p.y >= top - p.r && p.y <= o.y + 20;
                if (inside && !(U.Eq(p.height, "high") && o.IsCrouching()) && !(U.Eq(p.height, "low") && o.y < Db.GROUND - 70))
                {
                    p.hits--; p.hcd = p.gap; bool last = p.hits <= 0; int dir = p.vx > 0 ? 1 : p.vx < 0 ? -1 : p.owner.facing;
                    string res = Deal(p.owner, o, p.dmg, last && p.lastKnock > 0 ? p.lastKnock : p.knock, last && p.heavy, last && p.down, p.hitstop, p.unblock, false, U.Eq(p.height, "high") ? "mid" : p.height, false, dir);
                    Fx(p.owner, U.Eq(p.kind, "lasso") ? "blade" : p.kind, p.x, p.y, dir, last ? 0.8f : 0.45f);
                    if (U.Eq(res, "hit"))
                    {
                        if (p.onHit == 1) { SkState T = S(o); T.ice = 0.85f; Stun(o, 0.85f); T.slow = Mathf.Max(T.slow, 3.2f); }
                        else if (p.onHit == 2) { SkState T = S(o); T.pullTo = p.owner.x + p.owner.facing * 125 * p.owner.sc / Db.S; T.pullT = 0.3f; Stun(o, 0.75f); }
                    }
                    if (last || U.Eq(res, "parry")) p.life = 0;
                }
            }
            if (p.life <= 0 || p.x < -100 || p.x > Db.ARENA + 100) g.skProj.RemoveAt(i);
        }
        for (int i = g.spikes.Count() - 1; i >= 0; i--) { float[] s = g.spikes.Get(i); s[5] += dt; if (s[5] >= s[4]) g.spikes.RemoveAt(i); }
        for (int i = g.bolts.Count() - 1; i >= 0; i--) { float[] b = g.bolts.Get(i); b[1] += dt; if (b[1] >= 0.34f) g.bolts.RemoveAt(i); }
    }

    // ================= ЖИ (веб: SK_BOSS, SK_WEAP, skAssignAi, skAiTry) =================
    static readonly string[][] BOSS = {
        new string[] { "sadaq", "kokzhal" }, new string[] { "arqan", "dauyl" }, new string[] { "ayaz", "jer" },
        new string[] { "tulpar", "naizagai" }, new string[] { "jalyn", "dauyl", "arqan" }, new string[] { "jer", "quiyn", "naizagai" } };
    static string WeapSkill(string w)
    {
        string sh = Db.Weapon(w).S("shape"); if (U.Len(sh) == 0) sh = w;
        if (U.Eq(sh, "qylysh") || U.Eq(sh, "semser") || U.Eq(sh, "qanzhar")) return "kokzhal";
        if (U.Eq(sh, "naiza") || U.Eq(sh, "sungi")) return "sadaq";
        if (U.Eq(sh, "aibalta") || U.Eq(sh, "balta") || U.Eq(sh, "ai") || U.Eq(sh, "shoqpar") || U.Eq(sh, "kurzi") || U.Eq(sh, "pernach")) return "jer";
        return "dauyl";
    }
    public static void AssignAi(Fighter f, Match g, FoeCfg cfg)
    {
        if (f == null || !f.ai) return;
        if (cfg != null && U.Eq(cfg.special, "shadow")) { f.skAi = new string[] { "tulpar", "jalyn", "ayaz", "quiyn", "naizagai", "dauyl" }; f.skAiP = 0.2f; return; }
        if (cfg != null && cfg.special != null) return;
        int ch = cfg != null ? cfg.ch : g.chapter; Run r = g.run;
        bool hard = (U.Eq(g.mode, "campaign") && Eco.Stage(g.chapter, g.stageIdx).stars >= 4) || (U.Eq(g.mode, "tour") && r != null && r.tier >= 1)
            || (U.Eq(g.mode, "surv") && r != null && r.wave >= 4) || (U.Eq(g.mode, "duel") && Game.S.rating >= 1300) || U.Eq(g.mode, "boss");
        if (f.boss) { f.skAi = BOSS[Mathf.Clamp(ch, 0, BOSS.Length - 1)]; f.skAiP = hard ? 0.16f : 0.1f; }
        else if (hard) { f.skAi = new string[] { WeapSkill(f.weapon) }; f.skAiP = 0.07f; }
    }
    public static bool AiTry(Fighter f, float d)
    {
        Match g = f.game; if (g == null || f.skAi == null) return false;
        SkState T = S(f);
        if (g.phase != Match.FIGHT || f.sk != null || f.skWant != null || T.aiGap > g.clock || f.opp == null || (!f.opp.OnGround() && U.Rand() < 0.5f)) return false;
        if (U.Rand() > f.skAiP * (1 + 0.6f * f.Fierce())) return false;
        Lst<string> opts = new Lst<string>();
        for (int i = 0; i < f.skAi.Length; i++)
        {
            string id = f.skAi[i]; SkDef sd = By(id); if (sd == null || T.cd[sd.idx] > 0) continue;
            bool fits;
            if (U.Eq(id, "sadaq") || U.Eq(id, "dauyl") || U.Eq(id, "ayaz") || U.Eq(id, "jer") || U.Eq(id, "naizagai")) fits = d > 260 || U.Rand() < 0.25f;
            else if (U.Eq(id, "kokzhal")) fits = d > 180 && d < 520;
            else if (U.Eq(id, "tulpar")) fits = d < 620;
            else if (U.Eq(id, "arqan")) fits = d > 230 && d < 560;
            else if (U.Eq(id, "jalyn")) fits = d < 200 || !f.opp.OnGround();
            else if (U.Eq(id, "quiyn") || U.Eq(id, "aiqai") || U.Eq(id, "otty")) fits = d < 260;
            else fits = true;
            if (fits) opts.Add(id);
        }
        if (opts.Count() == 0) return false;
        f.skWant = opts.Get((int)(U.Rand() * opts.Count()) % opts.Count()); T.wantT = 0; T.aiGap = g.clock + Pz.Rand(3, 5.5f) * Db.PaceK("cd");
        return true;
    }

    /// <summary>Алғашқы қолдану: +20 тәжірибе (веб: skFirstUse).</summary>
    public static bool FirstUse(SkDef d)
    {
        if (Game.S.skUsed.Contains(d.id)) return false;
        Game.S.skUsed.Add(d.id); Progress.GainXp(20); return true;
    }
}
