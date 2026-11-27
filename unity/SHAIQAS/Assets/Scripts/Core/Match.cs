// SHAIQAS Core · Ұрыс: раундтар, уақыт, нокаут, марапат, камера. Веб-нұсқадағы game.startMatch/startRound/endRound/endMatch/update.
using UnityEngine;
public class FxEv
{
    public int kind, n, flags;   // flags: 1 — ауыр, 2 — ерекше (рух), 4 — лақтыру
    public float x, y, dir;
    public Fighter who;
}

public class SayLine
{
    public bool enemy;
    public string name, text;
    public float t0, t1;
}

/// <summary>Қарсылас сипаттамасы (веб: enemyCfg).</summary>
public class FoeCfg
{
    public int ch, idx;
    public string special;      // null | trainer | shadow | arena
    public float scale = 1;
    public static FoeCfg Of(int ch, int idx) { FoeCfg c = new FoeCfg(); c.ch = ch; c.idx = idx; return c; }
}

/// <summary>Турнир торындағы орын.</summary>
public class TourSlot { public string name, type; public bool me, boss; public FoeCfg cfg; }

/// <summary>Көп ұрысты режимнің күйі (веб: game.run).</summary>
public class Run
{
    public int wave, acc, i, stage, tier;
    public float hp = -1, time;
    public string rule = "none";
    public bool outd, champ, prize, fighting;
    public Lst<TourSlot> slots = new Lst<TourSlot>();
    public Lst<int[]> lv = new Lst<int[]>();
    public Lst<FoeCfg> list = new Lst<FoeCfg>();
}

public class Match
{
    public const int INTRO = 0, FIGHT = 1, ROUNDEND = 2, MATCHEND = 3;
    public const int FX_DUST = 0, FX_HIT = 1, FX_HITSPEC = 2, FX_BLOCK = 3, FX_GHOST = 4, FX_KO = 5, FX_SPECIAL = 6, FX_PHASE = 7, FX_WAVE = 8, FX_SKILL = 9;
    public const string GOLD = "#f2b840";

    // --- режим ---
    public string mode = "campaign";      // campaign | train | quick | tour | surv | duel | boss
    public Run run; public FoeCfg cfg;
    // бас жау қабілеттері: жебелер [x, y, dir, life, delay, speed, dmg, high(1/0), hit, ownerIsPlayer], ауа райы
    public Lst<float[]> arrows = new Lst<float[]>();
    public string weatherKind; public float weatherT, weatherMax; public Fighter weatherOwner;
    public bool bossIntro;
    // әдістер: снарядтар, тас найзалар [x,h,w,lean,life,t], найзағай [x,t,seed,kind], енгізу буфері, нақты уақыт сағаты, дыбыс кезегі
    public Lst<SkProj> skProj = new Lst<SkProj>();
    public Lst<float[]> spikes = new Lst<float[]>(), bolts = new Lst<float[]>();
    public SkBuf skBuf = new SkBuf();
    public float clock;
    public Lst<string> sndQ = new Lst<string>();
    public string skFirstName; public float skFirstT = 9;
    public float phaseMsgT = 9; public int phaseMsgN; public string phaseMsgName; public bool runOver = true;
    public const float ECO_K = 2;
    public int chapter, stageIdx, needWins = 2, round = 1, pWins, eWins;
    public bool live;                     // ұрыс экраны ашық
    public Fighter player, enemy;
    public string stageId = "steppe";

    // --- фаза ---
    public int phase = INTRO;
    public float pt, timer = 60, introT = 2.1f, matchT, slow, freeze, sayClock;
    public Fighter loser;
    public string roundMsg = "", roundWinner = "", runMsg = "", zhWin = "";
    public int reward, starsGot, starsNew, xpGot, lvlCoins;
    public bool won, paused;
    public Lst<SayLine> says = new Lst<SayLine>();

    // --- камера және әсерлер ---
    public float cam, zoom = 1.22f, lb = 60, shake, flash, punch, dark, pivY = 720 * 0.62f;
    public Lst<FxEv> fx = new Lst<FxEv>();
    public Lst<float[]> waves = new Lst<float[]>();   // [x, dir, life, speed, dmg, hit, ownerIsPlayer]
    public Lst<string> medalsNew = new Lst<string>();

    // ================= жасау =================
    public Fighter MakePlayer(float px, string heroId)
    {
        SaveData S = Game.S;
        JNode hr = Db.Hero(heroId), m = hr.Get("mods");
        JNode arm = Db.Armors.Get(S.eqArmor), hm = Db.Helms.Get(S.eqHelm);
        Fighter f = new Fighter();
        f.name = Game.Fix(hr.S("full")); f.heroId = heroId;
        f.weapon = S.eqWeapon;
        f.coat = U.Eq(S.eqArmor, "chapan") ? hr.S("coat") : arm.S("look");
        f.hat = U.Eq(S.eqHelm, "kalpak") ? hr.S("hat") : hm.S("look");
        f.beard = hr.Bk("beard"); f.big = hr.Bk("big"); f.slim = hr.Bk("slim");
        f.def = Mathf.Min(0.5f, arm.Fk("def") + hm.Fk("def") + m.Fk("def"));
        f.maxHp = PlayerHp(heroId);
        f.speed = 300 * m.Get("move").F(1);
        f.dmgMul = m.Get("dmg").F(1); f.atkSpd = m.Get("atkSpd").F(1); f.ruxMul = m.Get("rux").F(1); f.regen = m.Fk("regen");
        f.accent = U.Eq(S.eqHelm, "altyn") ? "#ffd84d" : GOLD;
        f.x = px;
        f.look = "p_" + heroId + "_" + S.eqArmor + "_" + S.eqHelm;
        f.game = this; f.Init();
        return f;
    }

    /// <summary>Ойыншының денсаулығы: батыр + деңгей (әр деңгей +3) — тәжірибе жинау ұрыста да пайдалы.</summary>
    public static float PlayerHp(string heroId) { return 110 + Db.Hero(heroId).Get("mods").Fk("hp") + (Progress.Level() - 1) * 3; }

    // ---------- жау күші (теңгерім 2.0): кейінгі тараулар қиын, бірақ жақсы жабдықпен жеңуге болады ----------
    // [0] hp, [1] dmgMul, [2] def, [3] speed, [4] aggr, [5] block, [6] crouch, [7] combo, [8] special
    public static float[] EnemyStats(int ch, int idx, bool boss, bool campaign, int stageIdx)
    {
        float t = ch * 0.55f + idx * 0.25f;
        float[] r = new float[9];
        r[0] = Mathf.Round(90 + ch * 10 + idx * 10 + (boss ? 30 : 0));
        r[1] = 0.8f + ch * 0.06f + idx * 0.05f + (boss ? 0.1f : 0);
        r[2] = ch * 0.03f + (boss ? 0.03f : 0);
        r[3] = 270 + ch * 8;
        r[4] = Pz.Clamp(0.45f + t * 0.1f, 0, 0.9f); r[5] = Pz.Clamp(0.22f + t * 0.08f, 0, 0.6f);
        r[6] = Pz.Clamp(0.12f + t * 0.08f, 0, 0.5f); r[7] = Pz.Clamp(0.25f + t * 0.12f, 0, 0.75f); r[8] = 0.5f + ch * 0.12f;
        if (campaign)
        {
            // кезең нөмірі бойынша (веб: campDiff), жұмсартылған
            int s = Mathf.Clamp(ch * 4 + stageIdx, 0, Db.NCH * 4 - 1); bool bs = stageIdx == 3;
            r[0] = Mathf.Round(r[0] * (1 + s * 0.008f + (bs ? 0.06f : 0)));
            r[1] *= 1 + s * 0.006f + (bs ? 0.04f : 0);
            float ai = s * 0.003f;
            r[4] = Pz.Clamp(r[4] + ai, 0.05f, 0.95f); r[5] = Pz.Clamp(r[5] + ai, 0.05f, 0.65f); r[7] = Pz.Clamp(r[7] + ai, 0.05f, 0.8f);
        }
        return r;
    }

    public Fighter MakeEnemy(int ch, int idx, float px)
    {
        JNode C = Db.Ch(ch), e = C.Get("enemies").At(idx);
        bool boss = e.Bk("boss");
        Fighter f = new Fighter();
        f.name = e.S("name"); f.ai = true; f.weapon = e.S("weapon"); f.hat = e.S("hat"); f.coat = e.S("coat"); f.accent = C.S("accent");
        f.boss = boss; f.beard = boss; f.x = px;
        float[] r = EnemyStats(ch, idx, boss, U.Eq(mode, "campaign"), stageIdx);
        f.maxHp = r[0]; f.dmgMul = r[1]; f.def = r[2]; f.speed = r[3];
        f.aiAggr = r[4]; f.aiBlock = r[5]; f.aiCrouch = r[6]; f.aiComboP = r[7]; f.aiSpecial = r[8];
        f.look = "e_" + ch + "_" + idx;
        f.game = this; f.Init();
        return f;
    }

    public Fighter MakeTrainer(float px)
    {
        Fighter f = new Fighter();
        f.name = "Жаттықтырушы"; f.ai = true; f.weapon = "soyil"; f.hat = "borik"; f.coat = "chapan"; f.beard = true; f.accent = "#9fd68a";
        f.maxHp = 80 + chapter * 10; f.dmgMul = 0.45f + chapter * 0.05f;
        f.aiAggr = 0.35f; f.aiBlock = 0.25f; f.aiCrouch = 0.15f; f.aiComboP = 0.2f; f.aiSpecial = 0.3f;
        f.x = px; f.look = "npc_trainer"; f.game = this; f.Init();
        return f;
    }

    /// <summary>Веб: makeFoe — режимге қарай күшейтілген қарсылас.</summary>
    public Fighter MakeFoe(FoeCfg c, float px)
    {
        if (U.Eq(c.special, "trainer")) return MakeTrainer(px);
        if (U.Eq(c.special, "arena"))
        {
            // веб: skArenaFoe — жаттығу серігі
            Fighter a = new Fighter();
            a.name = "Жаттығу серігі"; a.ai = true; a.weapon = "soyil"; a.hat = "borik"; a.coat = "chapan"; a.beard = true; a.accent = "#9fd68a";
            a.maxHp = 160; a.dmgMul = 0.55f; a.speed = 280;
            a.aiAggr = 0.55f; a.aiBlock = 0.35f; a.aiCrouch = 0.2f; a.aiComboP = 0.35f; a.aiSpecial = 0.3f;
            a.x = px; a.look = "npc_trainer"; a.game = this; a.Init();
            return a;
        }
        if (U.Eq(c.special, "shadow"))
        {
            Fighter s = new Fighter();
            s.name = "Қара Көлеңке"; s.ai = true; s.boss = true; s.weapon = Game.S.eqWeapon; s.hat = "altyn"; s.coat = "sharaina"; s.accent = "#ff3a2a";
            s.maxHp = 330; s.dmgMul = 1.7f; s.def = 0.25f; s.speed = 330; s.atkSpd = 1.15f; s.canPhase3 = true; s.abInfo = "shadow";
            s.aiAggr = 1; s.aiBlock = 0.75f; s.aiCrouch = 0.5f; s.aiComboP = 0.95f; s.aiSpecial = 1;
            s.x = px; s.look = "e_" + (Db.NCH - 1) + "_3"; s.game = this; s.Init();
            return s;
        }
        int keepSi = stageIdx; if (U.Eq(mode, "campaign")) stageIdx = c.idx;
        Fighter f = MakeEnemy(c.ch, c.idx, px);
        stageIdx = keepSi;
        if (U.Eq(mode, "tour") && run != null)
        {
            JNode T = Db.J("TOUR_TIERS").At(run.tier);
            Boost(f, T.Fk("hp"), T.Fk("dmg"), T.Fk("ai"));
        }
        if (U.Eq(mode, "duel"))
        {
            float k = Pz.Clamp((Game.S.rating - 1000) / 600f, 0, 1);
            Boost(f, 1 + 0.25f * k, 1 + 0.2f * k, 0.2f * k);
        }
        if (U.Eq(mode, "boss") && c.idx == 3)
        {
            Boost(f, 1.2f, 1.2f, 0);
            f.bossAb = Db.J("BOSS_AB").At(c.ch).Str(); f.abInfo = f.bossAb; f.abT = 3.5f;
        }
        if (c.scale != 1) { f.maxHp = Mathf.Round(f.maxHp * c.scale); f.hp = f.hpLag = f.maxHp; f.dmgMul *= 1 + (c.scale - 1) * 0.6f; }
        return f;
    }
    static void Boost(Fighter f, float hp, float dmg, float ai)
    {
        f.maxHp = Mathf.Round(f.maxHp * hp); f.hp = f.hpLag = f.maxHp; f.dmgMul *= dmg;
        f.aiAggr = Pz.Clamp(f.aiAggr + ai, 0.05f, 1); f.aiBlock = Pz.Clamp(f.aiBlock + ai, 0.05f, 1); f.aiComboP = Pz.Clamp(f.aiComboP + ai, 0.05f, 1);
    }

    // ================= режимдер (веб: startMode / nextFight / makeBracket) =================
    public static string TourRule(int ch, int t) { return t == 0 ? "none" : Db.J("RULE_CYCLE").At((ch + t * 2) % 4).Str(); }
    public static bool TierOpen(int ch, int t) { return Progress.TestUnlockAll || t == 0 || Game.S.tourW[ch][t - 1] > 0; }
    public static int RatingTier(int r) { JNode R = Db.J("RATING_TIERS"); int t = 0; for (int i = 0; i < R.Count(); i++) if (r >= R.At(i).Ik("min")) t = i; return t; }
    public static string FmtTime(float t) { int s = (int)t; int sec = s % 60; return (s / 60) + ":" + (sec < 10 ? "0" : "") + sec; }
    public static Lst<FoeCfg> DuelFoes() { Lst<FoeCfg> L = new Lst<FoeCfg>(); for (int c = 0; c < Db.NCH; c++) if (Progress.Unlocked(c)) for (int i = 0; i < 4; i++) L.Add(FoeCfg.Of(c, i)); return L; }
    /// <summary>Бастар шабуылына ашық бас жаулар (тараудың бас жауы жеңілген болса).</summary>
    public static Lst<FoeCfg> BossList(int ch)
    {
        Lst<FoeCfg> L = new Lst<FoeCfg>();
        for (int i = 0; i <= ch; i++) if (Progress.TestUnlockAll || Game.S.prog[i] >= 4) L.Add(FoeCfg.Of(i, 3));
        if (ch == Db.NCH - 1 && Progress.RankIndex() == Db.NCH) { FoeCfg s = FoeCfg.Of(ch, 3); s.special = "shadow"; L.Add(s); }
        return L;
    }

    public void StartMode(string md, int ch)
    {
        mode = md; chapter = ch; run = null; needWins = 1; cfg = null;
        if (U.Eq(md, "arena"))
        {
            // веб: skArenaStart — шексіз жаттығу алаңы (әдістерді үйрену)
            stageIdx = 0; cfg = new FoeCfg(); cfg.ch = ch; cfg.special = "arena"; StartMatch();
            phase = FIGHT; pt = 1; introT = 0; bossIntro = false; says.Clear(); zoom = 1.06f; lb = 10;
            player.active = true; enemy.active = true; arenaN = 0; arenaOk = null;
            return;
        }
        if (U.Eq(md, "train")) { stageIdx = 0; cfg = new FoeCfg(); cfg.ch = ch; cfg.special = "trainer"; StartMatch(); return; }
        if (U.Eq(md, "duel")) { needWins = 2; run = new Run(); cfg = FoeCfg.Of(duelCh >= 0 ? duelCh : ch, duelIdx); StartMatch(); return; }
        if (U.Eq(md, "tour"))
        {
            // веб: tourSaved — басталған турнир картадан шыққанда да сақталады
            Run sv = TourSaved(ch); tourNote = null;
            if (sv != null && sv.fighting)
            {   // ұрыс ортасында ойын жабылған — жеңіліс
                Game.S.tour[ch] = Mathf.Max(Game.S.tour[ch], sv.stage); TourStore(ch, null); Game.Persist();
                tourNote = "Алдыңғы турнир ұрыс ортасында тоқтады — жеңіліс есептелді";
            }
            else if (sv != null)
            {
                run = sv; if (Game.S.tourTier != sv.tier) { Game.S.tourTier = sv.tier; Game.Persist(); }
                return;
            }
            run = MakeBracket(ch, TierOpen(ch, Game.S.tourTier) ? Game.S.tourTier : 0); TourStore(ch, run); return;   // торды экран көрсетеді
        }
        if (U.Eq(md, "surv")) run = new Run();
        if (U.Eq(md, "boss")) { run = new Run(); run.list = BossList(ch); }
        NextFight();
    }
    public int duelIdx, duelCh = -1;
    public int newCard = -1;
    // еркін алаң: мақсатты әдіс, орындалу саны, соңғы соққы уақыты (денсаулық қалпына келуі үшін)
    public string arenaSel, arenaOk; public int arenaN; public float arenaOkT = 9;
    float pHp0, eHp0, pHitT = -9, eHitT = -9;
    void ArenaStep(float rdt)
    {
        timer = 99;
        if (player.hp < pHp0 - 0.01f) pHitT = clock; if (enemy.hp < eHp0 - 0.01f) eHitT = clock;
        if (clock - pHitT > 1.2f && player.state != Fighter.DOWN && player.hp < player.maxHp) player.hp = Mathf.Min(player.maxHp, player.hp + player.maxHp * 1.4f * rdt);
        if (clock - eHitT > 1.2f && enemy.state != Fighter.DOWN && enemy.hp < enemy.maxHp) enemy.hp = Mathf.Min(enemy.maxHp, enemy.hp + enemy.maxHp * 1.4f * rdt);
        pHp0 = player.hp; eHp0 = enemy.hp; arenaOkT += rdt;
    }

    public void NextFight()
    {
        Run r = run;
        if (U.Eq(mode, "tour")) cfg = r.slots.Get(r.lv.Get(r.stage)[1]).cfg;
        else if (U.Eq(mode, "surv")) { cfg = FoeCfg.Of(chapter, (r.wave + 1) % 5 == 0 ? 3 : (int)(U.Rand() * 3)); cfg.scale = 1 + r.wave * 0.07f; }
        else if (U.Eq(mode, "boss")) { if (r.list.Count() == 0) return; cfg = r.list.Get(r.i); }
        StartMatch();
    }

    public Run MakeBracket(int ch, int tier)
    {
        Run r = new Run(); r.tier = tier; r.rule = TourRule(ch, tier);
        JNode C = Db.Ch(ch), names = Db.J("TOUR_NAMES").At(ch);
        int[] ord = new int[names.Count()]; for (int i = 0; i < ord.Length; i++) ord[i] = i;
        for (int i = ord.Length - 1; i > 0; i--) { int j = (int)(U.Rand() * (i + 1)); int t = ord[i]; ord[i] = ord[j]; ord[j] = t; }
        int[][] pick = { new int[] { 0, 1 }, new int[] { 1, 2 }, new int[] { 0, 1 }, new int[] { 0, 1 }, new int[] { 1, 2 }, new int[] { 0, 2 } };
        TourSlot me = new TourSlot(); me.name = Game.Fix(Db.Hero(Game.S.hero).S("full")); me.me = true; r.slots.Add(me);
        for (int i = 0; i < 6; i++)
        {
            int idx = pick[i][(int)(U.Rand() * 2)];
            TourSlot s = new TourSlot(); s.name = names.At(ord[i]).Str(); s.type = C.Get("enemies").At(idx).S("name"); s.cfg = FoeCfg.Of(ch, idx); r.slots.Add(s);
        }
        TourSlot b = new TourSlot(); b.name = C.Get("enemies").At(3).S("name"); b.type = "бас жау"; b.cfg = FoeCfg.Of(ch, 3); b.boss = true; r.slots.Add(b);
        r.lv.Add(new int[] { 0, 1, 2, 3, 4, 5, 6, 7 });
        return r;
    }
    // ---------- турнирді сақтау (веб: tourStore / tourSaved / tourForfeit) ----------
    public string tourNote;
    public static void TourStore(int ch, Run r)
    {
        JNode d = JNode.Parse(U.Len(Game.S.tourRuns) > 0 ? Game.S.tourRuns : "{}"); if (!d.IsObj()) d = JNode.NewObj();
        JNode o = JNode.NewObj();
        string key = "c" + ch;
        for (int i = 0; i < d.Count(); i++) if (!U.Eq(d.KeyAt(i), key)) o.Set(d.KeyAt(i), d.At(i));
        if (r != null && !r.outd && !r.champ)
        {
            JNode j = JNode.NewObj();
            j.Set("tier", JNode.Of(r.tier)).Set("stage", JNode.Of(r.stage)).Set("rule", JNode.Of(r.rule)).Set("hp", JNode.Of(r.hp)).Set("fighting", JNode.OfBool(r.fighting));
            JNode sl = JNode.NewArr();
            for (int i = 0; i < r.slots.Count(); i++)
            {
                TourSlot s = r.slots.Get(i); JNode q = JNode.NewObj();
                q.Set("name", JNode.Of(s.name)).Set("type", JNode.Of(s.type == null ? "" : s.type)).Set("me", JNode.OfBool(s.me)).Set("boss", JNode.OfBool(s.boss));
                q.Set("ch", JNode.Of(s.cfg != null ? s.cfg.ch : -1)).Set("idx", JNode.Of(s.cfg != null ? s.cfg.idx : -1));
                sl.Add(q);
            }
            JNode lv = JNode.NewArr();
            for (int i = 0; i < r.lv.Count(); i++) { int[] a = r.lv.Get(i); JNode q = JNode.NewArr(); for (int k = 0; k < a.Length; k++) q.Add(JNode.Of(a[k])); lv.Add(q); }
            j.Set("slots", sl).Set("lv", lv);
            o.Set(key, j);
        }
        Game.S.tourRuns = o.Dump();
    }
    public static Run TourSaved(int ch)
    {
        JNode d = JNode.Parse(U.Len(Game.S.tourRuns) > 0 ? Game.S.tourRuns : "{}"); if (!d.IsObj()) return null;
        JNode j = d.Get("c" + ch); if (!j.IsObj()) return null;
        JNode sl = j.Get("slots"), lv = j.Get("lv");
        if (sl.Count() != 8 || lv.Count() == 0) return null;
        Run r = new Run(); r.tier = j.Ik("tier"); r.stage = j.Ik("stage"); r.rule = j.Get("rule").Str("none"); r.hp = j.Get("hp").F(-1); r.fighting = j.Bk("fighting");
        for (int i = 0; i < sl.Count(); i++)
        {
            JNode q = sl.At(i); TourSlot s = new TourSlot(); s.name = q.S("name"); s.type = q.S("type"); s.me = q.Bk("me"); s.boss = q.Bk("boss");
            if (q.Ik("ch") >= 0) s.cfg = FoeCfg.Of(q.Ik("ch"), q.Ik("idx"));
            if (s.me) s.name = Game.Fix(Db.Hero(Game.S.hero).S("full"));
            r.slots.Add(s);
        }
        for (int i = 0; i < lv.Count(); i++) { JNode q = lv.At(i); int[] a = new int[q.Count()]; for (int k = 0; k < a.Length; k++) a[k] = q.At(k).I(); r.lv.Add(a); }
        if (r.stage >= r.lv.Count()) return null;
        return r;
    }
    /// <summary>Үзілістен шығу — турнирдегі жеңіліс.</summary>
    public void TourForfeit()
    {
        if (run == null) return;
        run.outd = true; run.fighting = false; Game.S.tour[chapter] = Mathf.Max(Game.S.tour[chapter], run.stage);
        TourStore(chapter, run); Game.Persist();
    }
    int SimWin(Run r, int a, int b)
    {
        TourSlot A = r.slots.Get(a), B = r.slots.Get(b);
        if (A.boss) return a; if (B.boss) return b;
        return U.Rand() < 0.5f + (A.cfg.idx - B.cfg.idx) * 0.15f ? a : b;
    }
    void TourAdvance(Run r)
    {
        int[] cur = r.lv.Get(r.stage); int[] nxt = new int[cur.Length / 2]; nxt[0] = 0;
        for (int k = 1; k < cur.Length / 2; k++) nxt[k] = SimWin(r, cur[2 * k], cur[2 * k + 1]);
        r.lv.Add(nxt); r.stage++;
    }
    public string ModeLabel()
    {
        Run r = run;
        if (U.Eq(mode, "train")) return "Жаттығу";
        if (U.Eq(mode, "tour") && r != null)
        {
            string tn = Db.J("TOUR_TIERS").At(r.tier).S("name"), st = Db.J("TOUR_STAGES").At(Mathf.Min(r.stage, 2)).Str();
            return U.Sub(tn, 0, 1) + U.Lower(U.Sub(tn, 1, U.Len(tn) - 1)) + " турнир · " + U.Lower(st);
        }
        if (U.Eq(mode, "surv") && r != null) return "Төзім · " + (r.wave + 1) + "-толқын";
        if (U.Eq(mode, "duel")) return "Жекпе-жек";
        if (U.Eq(mode, "boss") && r != null) return "Бастар шабуылы · " + (r.i + 1) + "/" + r.list.Count();
        if (U.Eq(mode, "quick")) return "Еркін ұрыс";
        return "";
    }

    // ================= ағын =================
    public void StartCampaign(int ch, int si)
    {
        mode = "campaign"; chapter = ch; stageIdx = si;
        needWins = si == 3 ? Db.BOSS_WINS : 2;
        StartMatch();
    }
    public void StartTrain(int ch) { mode = "train"; chapter = ch; stageIdx = 0; needWins = 1; StartMatch(); }
    /// <summary>Еркін ұрыс: ашық тараулардан кездейсоқ жау, 1 раунд (веб: quickFight).</summary>
    public void StartQuick(int ch, int idx) { mode = "quick"; chapter = ch; stageIdx = idx; needWins = 1; StartMatch(); }

    public void StartMatch()
    {
        round = 1; pWins = 0; eWins = 0; reward = 0; runMsg = ""; matchT = 0; starsGot = 0; starsNew = 0; won = false; runOver = true;
        if (U.Eq(mode, "campaign") || U.Eq(mode, "quick")) cfg = null;
        StartRound();
        Zh.Reset(zhFs); zhHead = null; zhTip = null;
        if (U.Eq(mode, "tour") && run != null && !run.outd && !run.champ) { run.fighting = true; TourStore(chapter, run); Game.Persist(); }
        zhPre = Zh.Pre(this);
        bossIntro = U.Eq(mode, "boss");
        if (bossIntro) introT = 3.6f;
        if (U.Eq(mode, "campaign"))
        {
            introT = 3.7f;
            says.Clear();
            SayLine a = new SayLine(); a.enemy = true; a.name = enemy.name; a.text = Game.Fix(Db.Taunts.At(chapter).At(stageIdx).Str()); a.t0 = 0.35f; a.t1 = 1.95f; says.Add(a);
            SayLine b = new SayLine(); b.enemy = false; b.name = player.name;
            b.text = Game.Fix(Db.Replies.At((int)(U.Rand() * Db.Replies.Count())).Str()); b.t0 = 2.0f; b.t1 = 3.6f; says.Add(b);
            // веб: Бауыржан кейде жыр батырларына сілтеме жасайды (FRAME_REPLIES.past)
            if (U.Eq(player.heroId, "sharua") && U.Rand() < 0.35f) b.text = Game.Fix("Жырдағы батырлардай шайқасамын!");
        }
    }

    public void StartRound()
    {
        float mid = Db.ARENA / 2;
        live = true; paused = false;
        int stCh = U.Eq(mode, "boss") && cfg != null && cfg.special == null ? cfg.ch : chapter;
        stageId = Db.Ch(stCh).S("stage");
        player = MakePlayer(mid - 250, Game.S.hero);
        if (run != null && run.hp >= 0) { player.hp = player.hpLag = run.hp; }
        enemy = cfg != null ? MakeFoe(cfg, mid + 250) : U.Eq(mode, "train") ? MakeTrainer(mid + 250) : MakeEnemy(chapter, stageIdx, mid + 250);
        enemy.facing = -1;
        player.opp = enemy; enemy.opp = player;
        phase = INTRO; pt = 0; timer = 60; introT = 2.1f; says.Clear(); sayClock = 0;
        slow = 0; freeze = 0; flash = 0; dark = 0; loser = null;
        zoom = 1.22f; lb = 60;
        cam = Pz.Clamp(enemy.x - Db.W / 2, 0, Db.ARENA - Db.W);
        phaseMsgT = 9; waves.Clear(); fx.Clear(); arrows.Clear(); skProj.Clear(); spikes.Clear(); bolts.Clear(); skBuf.Clear();
        Skills.AssignAi(enemy, this, cfg != null ? cfg : FoeCfg.Of(chapter, stageIdx)); weatherT = 0; weatherKind = null;
        KidApply();
        pops.Clear(); shouts.Clear(); sbSlots = Skills.SbSlots(); sbFailT = 0;
        if (U.Eq(mode, "tour") && run != null)
        {
            string ru = run.rule; TourSlot sl = run.slots.Get(run.lv.Get(run.stage)[1]);
            if (!sl.boss) enemy.name = sl.name;
            if (U.Eq(ru, "sword")) { player.weapon = "qylysh"; enemy.weapon = "qylysh"; float ph = player.hp, eh = enemy.maxHp; player.Init(); enemy.Init(); player.hp = player.hpLag = ph; enemy.hp = enemy.hpLag = eh; }
            if (U.Eq(ru, "rux")) { player.ruxMul *= 2; enemy.ruxMul *= 2; }
            if (U.Eq(ru, "half")) { player.hp = player.hpLag = player.maxHp * 0.5f; enemy.hp = enemy.hpLag = enemy.maxHp * 0.5f; }
            if (U.Eq(ru, "fast")) timer = 30;
        }
    }

    void EndRound(bool ko)
    {
        if (!ko && zhFs.on) zhFs.timeout = true;
        Fighter p = player, e = enemy;
        int winner = 0;
        if (p.hp > e.hp) winner = 1; else if (e.hp > p.hp) winner = 2;
        if (winner == 1) pWins++; else if (winner == 2) eWins++;
        if (!ko) roundMsg = "УАҚЫТ БІТТІ!";
        else if (winner == 1) roundMsg = p.hp >= p.maxHp ? "МІНСІЗ!" : "СОЙҚАНДЫ ЖЕҢІС!";
        else roundMsg = "ҚҰЛАДЫҢ...";
        roundWinner = winner == 1 ? p.name + " раундты ұтты" : winner == 2 ? e.name + " раундты ұтты" : "Тең түсті";
        loser = winner == 1 ? e : winner == 2 ? p : null;
        if (winner == 1) Shout(p, Pk(WIN_P), 4, true); else if (winner == 2) Shout(e, Pk(WIN_E), 4, true);
        if (winner == 1 && p.hitsTaken == 0) Medal("perfect");
        p.active = false; e.active = false;
        phase = ROUNDEND; pt = 0;
    }

    void EndMatch()
    {
        phase = MATCHEND; pt = 0; newCard = -1;
        won = pWins >= needWins;
        if (!U.Eq(mode, "arena") && zhFs.on) { string[] za = Zh.After(this, zhFs); zhHead = za[0]; zhTip = za[1]; }
        zhFs.on = false;
        Fighter w = won ? player : enemy;
        if (w.state != Fighter.DEAD) w.Set(Fighter.VICTORY);
        reward = 0; runMsg = "";
        SaveData S = Game.S;
        if (U.Eq(mode, "campaign") && won)
        {
            int ch = chapter, si = stageIdx;
            bool first = S.prog[ch] <= si;
            if (first) newCard = ch * 4 + si;   // жаңа тарих картасы ашылды (веб: ALB.newCard)
            reward = (int)(Db.Ch(ch).Get("rewards").At(si).F() * (first ? 1f : 0.5f) * Eco.Stage(ch, si).pay);
            if (first) S.prog[ch] = si + 1;
            float hpK = player.hp / player.maxHp;
            int got = 1 | (matchT <= 60 ? 2 : 0) | (eWins == 0 && hpK >= 0.5f ? 4 : 0);
            int old = S.stars[ch][si], fresh = got & ~old;
            starsGot = got; starsNew = fresh;
            S.stars[ch][si] = old | got;
            int bonus = Progress.StarCount(fresh) * (15 + ch * 15);
            if (bonus > 0) { reward += bonus; runMsg = "Жаңа жұлдыз: +" + bonus + " теңге бонус"; }
            zhWin = Game.Fix(Db.ZhWin.At((int)(U.Rand() * Db.ZhWin.Count())).Str());
        }
        else if (U.Eq(mode, "train") && won) { reward = (15 + chapter * 6) * 2; runMsg = "Жаттығу сәтті өтті!"; }
        // еркін ұрыс — тек ойын үшін: теңге де, тәжірибе де берілмейді
        else if (U.Eq(mode, "tour")) { EndTour(S); run.fighting = false; TourStore(chapter, run); }
        else if (U.Eq(mode, "surv")) EndSurv(S);
        else if (U.Eq(mode, "duel")) EndDuel(S);
        else if (U.Eq(mode, "boss")) EndBoss(S);
        if (won) Medal("first");
        if (player.blocks >= 10) Medal("block");
        if (!won && !U.Eq(mode, "surv") && !U.Eq(mode, "boss")) reward = 0;
        if (reward > 0) S.coins += reward;
        // тәжірибе (веб: skMatchXp)
        // жеңілгенге ештеңе берілмейді (тәжірибе де, теңге де)
        xpGot = !won ? 0 : Mathf.RoundToInt(24 + chapter * 6 + stageIdx * 4 + (enemy.boss ? 30 : 0));
        if (U.Eq(mode, "train") && won) xpGot = Mathf.RoundToInt(xpGot * 0.6f);
        if (U.Eq(mode, "quick")) xpGot = 0;
        lvlCoins = xpGot > 0 ? Progress.GainXp(xpGot) : 0;
        Medals.Check(medalsNew);
        Game.Persist();
    }

    void EndTour(SaveData S)
    {
        Run r = run; int ch = chapter; JNode T = Db.J("TOUR_TIERS").At(r.tier); float pay = T.Fk("pay");
        if (won)
        {
            TourAdvance(r);
            if (r.stage >= 3)
            {
                r.champ = true; reward = Mathf.RoundToInt((120 + ch * 60) * pay * ECO_K); S.tour[ch] = 3; S.tourW[ch][r.tier] = 1;
                runMsg = T.S("name") + " ТУРНИРДІҢ ЧЕМПИОНЫ!";
                if (r.tier == 2 && !S.ownHelm.Contains("bayge")) { S.ownHelm.Add("bayge"); r.prize = true; runMsg = runMsg + " Сыйлық: Бәйге бөркі!"; }
            }
            else { runOver = false; runMsg = r.stage == 1 ? "Жартылай финалға өттің!" : "Финалға өттің!"; }
        }
        else
        {
            r.outd = true; S.tour[ch] = Mathf.Max(S.tour[ch], r.stage); reward = Mathf.RoundToInt(20 * r.stage * pay * ECO_K);
            runMsg = "Турнир аяқталды: " + (r.stage == 0 ? "ширек финал" : r.stage == 1 ? "жартылай финал" : "финал");
        }
    }
    void EndSurv(SaveData S)
    {
        Run r = run; int ch = chapter;
        if (won) { r.wave++; r.acc += 8 + ch * 4; r.hp = Mathf.Min(player.maxHp, player.hp + player.maxHp * 0.25f); runOver = false; runMsg = r.wave + " толқын жеңілді! Денсаулық сәл қалпына келді."; }
        else { S.surv[ch] = Mathf.Max(S.surv[ch], r.wave); reward = (int)(r.acc * ECO_K); runMsg = "Нәтиже: " + r.wave + " толқын · рекорд " + S.surv[ch]; }
    }
    void EndDuel(SaveData S)
    {
        int ch = chapter, t0 = RatingTier(S.rating);
        if (won) { S.rating += 25; reward = (int)((20 + ch * 5 + RatingTier(S.rating) * 10) * ECO_K); } else S.rating = Mathf.Max(0, S.rating - 15);
        int t1 = RatingTier(S.rating);
        runMsg = "Рейтинг: " + S.rating + " (" + (won ? "+25" : "−15") + ") · " + Db.J("RATING_TIERS").At(t1).S("name") + (t1 > t0 ? " · ЖАҢА ДЕҢГЕЙ!" : t1 < t0 ? " · деңгей төмендеді" : "");
    }
    void EndBoss(SaveData S)
    {
        Run r = run; int ch = chapter;
        if (won)
        {
            r.i++; r.hp = Mathf.Min(player.maxHp, player.hp + player.maxHp * 0.4f); r.time += matchT;
            if (r.i >= r.list.Count())
            {
                reward = (int)(80 * r.list.Count() * ECO_K); S.bossRec = Mathf.Max(S.bossRec, r.i);
                int best = S.bossTime[ch]; bool rec = best == 0 || r.time < best; if (rec) S.bossTime[ch] = Mathf.RoundToInt(r.time);
                runMsg = "Барлық бас жау жеңілді! Уақыт " + FmtTime(r.time) + (rec ? " · ЖАҢА РЕКОРД!" : " · рекорд " + FmtTime(best));
            }
            else { runOver = false; runMsg = r.i + "/" + r.list.Count() + " бас жау жеңілді · уақыт " + FmtTime(r.time); }
        }
        else { S.bossRec = Mathf.Max(S.bossRec, r.i); reward = (int)(40 * r.i * ECO_K); runMsg = "Нәтиже: " + r.i + " бас жау"; }
    }

    void Medal(string id) { if (Medals.Give(id)) medalsNew.Add(id); }

    // ================= әсер шақырулары (Fighter-ден) =================
    public void Fx(int kind, float x, float y, float dir, int n, Fighter who) { FxF(kind, x, y, dir, n, who, 0); }
    public void FxF(int kind, float x, float y, float dir, int n, Fighter who, int flags)
    {
        FxEv e = new FxEv(); e.kind = kind; e.x = x; e.y = y; e.dir = dir; e.n = n; e.who = who; e.flags = flags;
        if (fx.Count() < 256) fx.Add(e);
    }
    public void Hitstop(float t, float sh)
    {
        t *= Db.PaceK("hitstop");
        if (sh >= 8) sh *= Db.PaceK("shake");
        freeze = Mathf.Max(freeze, t); shake = Mathf.Max(shake, sh);
    }
    public void Ko(Fighter f)
    {
        if (phase != FIGHT) return;
        // соңғы соққы: сәл тоқтап қалу, экран сілкінісі, ұзағырақ баяу қозғалыс (Shadow Fight 2 сияқты)
        Hitstop(0.09f, 12); punch = Mathf.Max(punch, 0.07f);
        slow = 1.45f * Db.PaceK("ko");
        flash = Mathf.Max(flash, 0.18f);
        Fx(FX_KO, f.x, f.y - 150, f.facing, 0, f);
    }
    public void SpecialFx(Fighter f) { flash = 0.25f; dark = 0.55f; Hitstop(0.1f, 6); Fx(FX_SPECIAL, f.x, f.y - 160, f.facing, 30, f); }
    public void EnemySay(Fighter f, string text, float life)
    {
        SayLine a = new SayLine(); a.enemy = f == enemy; a.name = f.name; a.text = text; a.t0 = sayClock; a.t1 = sayClock + life; says.Add(a);
    }
    public void AbilityCall(Fighter f) { EnemySay(f, Db.J("AB_INFO").Get(f.bossAb).S("shout"), 1.5f); flash = 0.08f; }
    public void Phase3Fx(Fighter f)
    {
        flash = 0.35f; dark = 0.7f; Hitstop(0.3f, 16); punch = 0.1f;
        phaseMsgT = 0; phaseMsgN = 3; phaseMsgName = f.name;
        Fx(FX_PHASE, f.x, f.y - 120, f.facing, 50, f);
        SayLine a = new SayLine(); a.enemy = true; a.name = f.name; a.text = "Енді шын күшімді көресің!"; a.t0 = sayClock + 0.3f; a.t1 = sayClock + 2.6f; says.Add(a);
    }
    public void Shoot(Fighter f, float h, float dl)
    {
        float pj = Db.PaceK("proj");
        arrows.Add(new float[] { f.x + f.facing * 70 * f.sc, f.y - h * f.sc, f.facing, (1.8f + dl) / pj, dl, 1000 * pj, 10 * f.dmgMul, h > 100 ? 1 : 0, 0, f == player ? 1 : 0 });
    }
    public void ShootRaw(Fighter f, float x, float y, float life, float delay, float speed, float dmg, bool high)
    {
        arrows.Add(new float[] { x, y, f.facing, life, delay, speed, dmg, high ? 1 : 0, 0, f == player ? 1 : 0 });
        sndQ.Add("arrow");
    }
    public void Bolt(float x, int kind) { bolts.Add(new float[] { x, 0, U.Rand() * 1000, kind }); flash = Mathf.Max(flash, 0.16f); shake = Mathf.Max(shake, 9); sndQ.Add("skill_thunder"); FxF(FX_SKILL, x, Db.GROUND - 30, 1, 100, player, 2); }
    public void Spike(float x, float h, float w, float lean) { spikes.Add(new float[] { x, h, w, lean, 0.62f, 0 }); }
    public void SkillCast(Fighter f, SkDef d, bool isAi)
    {
        if (!isAi && f == player && ZhTrack()) zhFs.skills++;
        sndQ.Add("skill_cast"); sndQ.Add("skill_" + d.kind);
        if (U.Eq(d.id, "tulpar")) sndQ.Add("horse");
        if (U.Eq(d.id, "boran")) sndQ.Add("blizzard");
        if (!isAi && f == player && !U.Eq(mode, "quick") && Skills.FirstUse(d)) { skFirstName = d.name; skFirstT = 0; }
        if (!isAi && f == player && U.Eq(mode, "arena") && arenaSel != null && U.Eq(d.id, arenaSel)) { arenaOk = d.id; arenaN++; arenaOkT = 0; }
    }
    public void SkillFail(Fighter f) { sndQ.Add("skill_fail"); }
    public void SkillParry(Fighter f) { sndQ.Add("parry"); }
    public void SkillRoar(Fighter f) { sndQ.Add("roar"); }
    public void SetWeather(string kind, Fighter owner) { weatherKind = kind; weatherT = 5; weatherMax = 5; weatherOwner = owner; }

    public void PhaseFx(Fighter f)
    {
        if (U.Eq(mode, "campaign") && f == enemy)
        {
            SayLine a = new SayLine(); a.enemy = true; a.name = f.name; a.text = Db.Rage.At(chapter).Str(); a.t0 = sayClock + 0.3f; a.t1 = sayClock + 2.6f; says.Add(a);
        }
        flash = 0.3f; dark = 0.5f; Hitstop(0.25f, 14); punch = 0.08f;
        if (f == enemy) { phaseMsgT = 0; phaseMsgN = 2; phaseMsgName = f.name; }
        Fx(FX_PHASE, f.x, f.y - 120, f.facing, 40, f);
    }
    public void SpawnWave(Fighter owner, float x, int dir, float dmg)
    {
        float proj = Db.PaceK("proj");
        waves.Add(new float[] { x, dir, 0.75f / proj, 760 * proj, dmg, 0, owner == player ? 1 : 0 });
        Hitstop(0.02f, 12);
    }

    static Move waveMove;

    // ================= әдіс тақтасы және соққы сандары (веб: ГЕЙМПЛЕЙ 2.0) =================
    public Lst<SkDef> sbSlots = new Lst<SkDef>(); public float sbFailT;
    public Lst<DmgPop> pops = new Lst<DmgPop>();
    // Бұқар жырау: ұрыс алдындағы кеңес, ұрыстан кейінгі мақтау/кеңес, статистика
    public string zhPre, zhHead, zhTip; public ZhFs zhFs = new ZhFs();
    bool ZhTrack() { return zhFs.on && live && !U.Eq(mode, "arena"); }
    public void ZhAttack(Fighter f, string id)
    {
        if (f != player || !ZhTrack()) return;
        zhFs.attacks++; if (!zhFs.ids.Contains(id)) zhFs.ids.Add(id);
        if (U.Eq(id, "kick") || U.Eq(id, "round") || U.Eq(id, "ckick") || U.Eq(id, "akick")) zhFs.kicks++;
        if (f.cm != null && f.cm.special) zhFs.specials++;
    }
    public bool SbShown() { return !U.Eq(mode, "arena") && player != null && !player.ai && (phase == FIGHT || phase == INTRO); }
    void SbStep(float rdt, Intent input)
    {
        sbFailT = Mathf.Max(0, sbFailT - rdt);
        if (input == null || input.sb <= 0 || input.sb > sbSlots.Count() || phase != FIGHT || U.Eq(mode, "arena")) return;
        if (player == null || !player.active || player.ai) return;
        SkDef d = sbSlots.Get(input.sb - 1);
        int r = Skills.SbCast(player, d);
        if ((r == 1 || r == 2) && sbFailT <= 0) { Skills.Call(player, r == 1 ? "Әлі дайын емес" : "Рух жетпейді", d.kind, true); SkillFail(player); sbFailT = 0.5f; }
    }
    public void Pop(float x, float y, string txt, string col, float size)
    {
        if (pops.Count() > 30) pops.RemoveAt(0);
        DmgPop d = new DmgPop(); d.x = x + Pz.Rand(-14, 14); d.y = y; d.txt = txt; d.col = col; d.size = size; d.vx = Pz.Rand(-40, 40);
        pops.Add(d);
    }
    /// <summary>Fighter.TakeHit соңында: зақым саны, «ҚОРҒАДЫ», «ЖЕҢІС!».</summary>
    public void HitPop(Fighter f, Move m, float dmg)
    {
        if (!live) return;
        if (m != null && f == player && ZhTrack())
        {
            if (U.Eq(f.skRes, "block")) zhFs.blocks++;
            else if (U.Eq(f.skRes, "hit")) { zhFs.hits++; if (m.foot) zhFs.foot++; if (m.IsLow()) zhFs.low++; if (m.thr) zhFs.thrown++; }
        }
        float hy = f.y - 250 * f.sc / Db.S;
        if (dmg > 0.5f)
        {
            bool big = (m != null && (m.heavy || m.special)) || dmg >= 14;
            Pop(f.x, hy, "" + (int)Mathf.Round(dmg), big ? "#ffd04a" : "#ffffff", big ? 40 : 30);
            if (f.state == Fighter.DEAD) Pop(f.x, hy - 50, "ЖЕҢІС!", "#ff6a4a", 46);
            ShoutHit(f, dmg);
        }
        else if (U.Eq(f.skRes, "block") || (m != null && f.guard)) Pop(f.x, hy, "ҚОРҒАДЫ", "#9dd0ff", 24);
    }
    void PopStep(float rdt)
    {
        for (int i = 0; i < pops.Count(); i++) { DmgPop d = pops.Get(i); d.t += rdt; d.y -= (70 - d.t * 40) * rdt; d.x += d.vx * rdt; }
        for (int i = pops.Count() - 1; i >= 0; i--) if (pops.Get(i).t > 1.1f) pops.RemoveAt(i);
    }

    // ================= жауынгерлердің айқайы мен сөздері (комикс бұлты, бас үстінде) =================
    public Lst<ShoutL> shouts = new Lst<ShoutL>();
    static readonly string[] CRY_P = { "Аруақ!", "Алаш!", "Айт!", "Ұр!", "Хап!" }, CRY_E = { "Ха!", "Кел!", "Мә!", "Ұр!" };
    static readonly string[] OUCH = { "Ах!", "Ой!", "Ыһ!", "Әй!" };
    static readonly string[] TAUNT_P = { "Тұр енді!", "Тағы кел!" }, TAUNT_E = { "Тұр!", "Әлсізсің!" };
    static readonly string[] LOW_P = { "Беріспеймін!", "Әлі тұрмын!" }, LOW_E = { "Қап!", "Әттең!" };
    static readonly string[] WIN_P = { "Аруақ!", "Жеңіс біздікі!" }, WIN_E = { "Ха-ха!", "Жеңдім!" };
    static string Pk(string[] a) { return a[Mathf.Min(a.Length - 1, (int)(U.Rand() * a.Length))]; }
    /// <summary>Сөз айту (әр жауынгерге 1.8 с үзіліспен; маңыздысы — force).</summary>
    public void Shout(Fighter f, string txt, int kind, bool force)
    {
        if (f == null || !live || U.Eq(mode, "arena")) return;
        if (!force && clock - f.shoutT < 1.8f) return;
        f.shoutT = clock;
        for (int i = shouts.Count() - 1; i >= 0; i--) if (shouts.Get(i).f == f) shouts.RemoveAt(i);
        ShoutL s = new ShoutL(); s.f = f; s.txt = txt; s.kind = kind; shouts.Add(s);
    }
    /// <summary>Соққыны бастағанда: ауыр не рух соққысында айқай.</summary>
    public void ShoutAttack(Fighter f, Move m)
    {
        if (phase != FIGHT || m == null || !(m.heavy || m.special) || U.Rand() > (m.special ? 0.85f : 0.4f)) return;
        Shout(f, Pk(f == player ? CRY_P : CRY_E), 0, m.special);
    }
    void ShoutHit(Fighter f, float dmg)
    {
        Fighter o = f.opp;
        if (f.state == Fighter.DEAD) return;
        if (f.state == Fighter.DOWN && o != null && U.Rand() < 0.6f) { Shout(o, Pk(o == player ? TAUNT_P : TAUNT_E), 2, false); return; }
        if (!f.lowSaid && f.hp < f.maxHp * 0.3f) { f.lowSaid = true; Shout(f, Pk(f == player ? LOW_P : LOW_E), 3, true); return; }
        if (dmg >= 12 && U.Rand() < 0.5f) Shout(f, Pk(OUCH), 1, false);
    }
    void ShoutStep(float rdt)
    {
        for (int i = shouts.Count() - 1; i >= 0; i--) { ShoutL s = shouts.Get(i); s.t += rdt; if (s.t > 1.25f) shouts.RemoveAt(i); }
    }

    // ================= балалар режимі (веб: kidApply) =================
    public float kidFailT;
    public static bool KidOn() { return Game.S != null && Game.S.kid; }
    public bool KidActive() { return KidOn() && !U.Eq(mode, "arena"); }
    void KidApply()
    {
        if (!KidActive()) return;
        Fighter e = enemy, p = player;
        if (e != null && e.ai && !e.kid)
        {
            e.kid = true; e.dmgMul *= 0.6f; e.speed *= 0.88f;
            e.aiAggr *= 0.7f; e.aiBlock *= 0.55f; e.aiComboP *= 0.45f; e.aiCrouch *= 0.6f; e.aiSpecial *= 0.5f;
        }
        if (p != null && !p.ai && !p.kid) { p.kid = true; p.ruxMul *= 1.4f; }
    }
    void KidStep(float rdt, Intent input)
    {
        kidFailT = Mathf.Max(0, kidFailT - rdt);
        if (input == null || !input.u || !KidActive() || phase != FIGHT || player == null || !player.active || player.ai) return;
        if (player.sk != null || player.state == Fighter.DEAD) return;
        if (!Skills.KidCast(player) && kidFailT <= 0)
        {
            Skills.Call(player, "Әлі дайын емес", "spirit", true); SkillFail(player); kidFailT = 0.6f;
        }
    }
    /// <summary>Жау соққы дайындап жатыр ма (балалар режиміндегі «!» белгісі үшін): 0 — жоқ, әйтпесе 0..1 үлесі.</summary>
    public float KidWarn()
    {
        Fighter e = enemy;
        if (!KidActive() || e == null || !e.kid || e.state != Fighter.ATTACK || e.cm == null || e.cm.sk || !(e.st < e.cm.w)) return 0;
        return Mathf.Clamp(e.st / Mathf.Max(0.05f, e.cm.w), 0.001f, 1);
    }

    public bool KidWarnLow() { return enemy != null && enemy.cm != null && U.Eq(enemy.cm.height, "low"); }

    // ================= кадр =================
    public void Update(float rdt, Intent input)
    {
        if (paused) return;
        float pause = Db.PaceK("pause");
        float pacePt = phase == INTRO ? 1f / (U.Eq(mode, "campaign") && says.Count() > 0 ? Db.PaceK("talk") : pause) : phase == ROUNDEND ? 1f / pause : 1f;
        pt += rdt * pacePt;
        shake = Mathf.Max(0, shake - 40 * rdt);
        flash = Mathf.Max(0, flash - rdt);
        punch = Mathf.Max(0, punch - rdt * (0.06f + punch * 7));
        sayClock += rdt * (phase == INTRO ? pacePt : 1);
        phaseMsgT += rdt; clock += rdt; skFirstT += rdt;
        if (U.Eq(mode, "arena") && player != null && enemy != null) ArenaStep(rdt);
        KidStep(rdt, input);
        SbStep(rdt, input); PopStep(rdt); ShoutStep(rdt);

        float dt = rdt;
        if (slow > 0) { slow -= rdt; dt *= 0.28f; }
        if (freeze > 0) { freeze -= rdt; dt = 0; }

        if (phase == INTRO && pt > introT) { phase = FIGHT; pt = 0; player.active = true; enemy.active = true; }
        else if (phase == FIGHT)
        {
            timer -= dt; matchT += dt;
            if (ZhTrack() && player != null) { if (player.rux >= 100) zhFs.ruxT += rdt; if (player.hp > 0) zhFs.minHp = Mathf.Min(zhFs.minHp, player.hp / player.maxHp); }
            if (player.state == Fighter.DEAD || enemy.state == Fighter.DEAD) { if (slow <= 0) EndRound(true); }
            else if (timer <= 0) { timer = 0; EndRound(false); }
        }
        // соңғы раунд: жеңілгеннің ұшып құлағаны толық көрінсін — нәтиже терезесі сәл кешірек шығады
        else if (phase == ROUNDEND && pt > ((pWins >= needWins || eWins >= needWins) && (player.state == Fighter.DEAD || enemy.state == Fighter.DEAD) ? 3.4f : 2.6f))
        {
            if (pWins >= needWins || eWins >= needWins) EndMatch();
            else { int kp = pWins, ke = eWins, r = round + 1; StartRound(); pWins = kp; eWins = ke; round = r; }
        }

        if (dt > 0)
        {
            player.Update(dt, input); enemy.Update(dt, null);
            player.HatStep(dt); enemy.HatStep(dt); player.WepStep(dt); enemy.WepStep(dt);
            Skills.Step(this, dt);
            for (int i = arrows.Count() - 1; i >= 0; i--)
            {
                float[] a = arrows.Get(i);
                a[3] -= dt;
                if (a[4] > 0) { a[4] -= dt; continue; }
                a[0] += a[2] * a[5] * dt;
                Fighter own = a[9] > 0 ? player : enemy, o = own.opp;
                if (a[8] == 0 && o != null && o.state != Fighter.DEAD && o.state != Fighter.DOWN && Mathf.Abs(o.x - a[0]) < 40)
                {
                    bool ducked = a[7] > 0 && o.IsCrouching(), jumped = a[7] == 0 && o.y < Db.GROUND - 70;
                    if (!ducked && !jumped)
                    {
                        a[8] = 1; a[3] = 0;
                        Move am = new Move(); am.dmg = a[6]; am.height = a[7] > 0 ? "mid" : "low"; am.knock = 240; am.hitstop = 0.05f;
                        o.TakeHit(am, (int)a[2], true, own);
                    }
                }
                if (a[3] <= 0 || a[0] < 0 || a[0] > Db.ARENA) arrows.RemoveAt(i);
            }
            if (weatherT > 0)
            {
                weatherT -= dt;
                if (U.Eq(weatherKind, "sand") && weatherOwner != null && weatherOwner.opp != null && weatherOwner.opp.hp > 1 && phase == FIGHT)
                    weatherOwner.opp.hp = Mathf.Max(1, weatherOwner.opp.hp - 2.6f * dt);
                if (phase != FIGHT) weatherT = 0;
            }
            for (int i = waves.Count() - 1; i >= 0; i--)
            {
                float[] w = waves.Get(i);
                w[2] -= dt; w[0] += w[1] * w[3] * dt;
                Fighter owner = w[6] > 0 ? player : enemy, o = owner.opp;
                if (w[5] == 0 && o != null && Mathf.Abs(o.x - w[0]) < 70 && o.y > Db.GROUND - 70)
                {
                    w[5] = 1;
                    if (waveMove == null) { waveMove = new Move(); waveMove.height = "low"; waveMove.down = true; waveMove.knock = 420; waveMove.heavy = true; waveMove.special = true; waveMove.hitstop = 0.08f; }
                    waveMove.dmg = w[4];
                    o.TakeHit(waveMove, (int)w[1], true, owner);
                }
                if (w[2] <= 0) waves.RemoveAt(i);
            }
        }
        // кинематографиялық камера
        float mid = (player.x + enemy.x) / 2, focus = mid, zoomT = 1, lbT = 0;
        Fighter spec = null;
        if (player.state == Fighter.ATTACK && player.cm != null && player.cm.special && player.st < player.cm.w + player.cm.a) spec = player;
        else if (enemy.state == Fighter.ATTACK && enemy.cm != null && enemy.cm.special && enemy.st < enemy.cm.w + enemy.cm.a) spec = enemy;
        if (phase == INTRO) { float k = Pz.Clamp(pt / 2, 0, 1), e = k * k * (3 - 2 * k); focus = Pz.Lerp(enemy.x - 60, mid, e); zoomT = Pz.Lerp(1.22f, 1, e); lbT = 60; }
        else if (phase == ROUNDEND) { Fighter L = loser; if (L != null) focus = Pz.Lerp(mid, L.x, 0.6f); zoomT = 1.18f; lbT = 60; }
        else if (phase == MATCHEND) { Fighter w = won ? player : enemy; focus = Pz.Lerp(mid, w.x, 0.7f); zoomT = 1.14f; lbT = 60; }
        else if (spec != null) { bool wind = spec.st < spec.cm.w; focus = Pz.Lerp(mid, spec.x, wind ? 0.7f : 0.5f); zoomT = wind ? 1.24f : 1.15f; lbT = 68; if (wind) dark = Mathf.Max(dark, 0.42f); }
        if (player.state == Fighter.DEAD || enemy.state == Fighter.DEAD) { zoomT = Mathf.Max(zoomT, slow > 0 ? 1.3f : 1.2f); lbT = slow > 0 ? 72 : 60; }
        float ez = 1 - Mathf.Exp(-3.5f * rdt);
        zoom += (zoomT - zoom) * ez; lb += (lbT - lb) * ez;
        dark = Mathf.Max(0, dark - rdt * 1.2f);
        float target = Pz.Clamp(focus - Db.W / 2, 0, Db.ARENA - Db.W);
        cam += (target - cam) * (1 - Mathf.Exp(-5 * rdt));
    }

    /// <summary>Ұрыс аяқталғаннан кейін: "next" | "retry" | "map". Қайтарады: келесі экран атауы.</summary>
    public string After(string next)
    {
        live = false;
        bool multi = U.Eq(mode, "tour") || U.Eq(mode, "surv") || U.Eq(mode, "boss");
        if (U.Eq(next, "retry")) { if (multi) { StartMode(mode, chapter); return U.Eq(mode, "tour") ? "bracket" : "fight"; } StartMatch(); return "fight"; }
        if (U.Eq(next, "next") && multi && !runOver) { if (U.Eq(mode, "tour")) return "bracket"; NextFight(); return "fight"; }
        if (U.Eq(mode, "duel")) return "duelsel";
        if (U.Eq(mode, "arena")) return "lobby";
        if (U.Eq(mode, "campaign") && won && U.Eq(next, "next")) return stageIdx == 3 ? "chapterDone" : "nextStage";
        return "map";
    }
}

public static class Medals
{
    /// <summary>Жаңа медаль берілсе true.</summary>
    public static bool Give(string id)
    {
        if (Game.S.medals.Contains(id)) return false;
        Game.S.medals.Add(id);
        return true;
    }
    public static bool Has(string id) { return Game.S.medals.Contains(id); }
    public static void Check(Lst<string> fresh)
    {
        SaveData S = Game.S;
        for (int i = 0; i < Db.NCH; i++) if (S.prog[i] >= 4 && Give("ch" + i)) fresh.Add("ch" + i);
        int full = 0;
        for (int i = 0; i < Db.NCH; i++) if (S.quizBest[i] >= Db.QZ_N) full++;
        if (full >= 1 && Give("quiz1")) fresh.Add("quiz1");
        if (full >= Db.NCH && Give("quizall")) fresh.Add("quizall");
        if (CardsOpen() >= Db.Hist.Count() && Give("cards")) fresh.Add("cards");
    }
    /// <summary>Тарих картасы ашық па: әр тарауда 4 карта, ұрыс жеңілген сайын бір карта.</summary>
    public static bool CardOpen(int i) { return Progress.TestUnlockAll || Game.S.prog[i / 4] > i % 4; }
    public static int CardsOpen() { int n = 0; for (int i = 0; i < Db.Hist.Count(); i++) if (CardOpen(i)) n++; return n; }
}

/// <summary>Ұшып шығатын соққы саны / сөз (веб: DMGP).</summary>
public class DmgPop
{
    public float x, y, vx, t, size;
    public string txt, col;
}

/// <summary>Жауынгердің айқайы (комикс бұлты): 0 — айқай, 1 — ауырсыну, 2 — мазақ, 3 — жаны аз, 4 — жеңіс.</summary>
public class ShoutL
{
    public Fighter f; public string txt; public int kind; public float t;
}
