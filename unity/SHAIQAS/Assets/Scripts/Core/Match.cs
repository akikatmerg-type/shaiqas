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
    public bool outd, champ, prize;
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
        f.maxHp = 110 + m.Fk("hp");
        f.speed = 300 * m.Get("move").F(1);
        f.dmgMul = m.Get("dmg").F(1); f.atkSpd = m.Get("atkSpd").F(1); f.ruxMul = m.Get("rux").F(1); f.regen = m.Fk("regen");
        f.accent = U.Eq(S.eqHelm, "altyn") ? "#ffd84d" : GOLD;
        f.x = px;
        f.look = "p_" + heroId + "_" + S.eqArmor + "_" + S.eqHelm;
        f.game = this; f.Init();
        return f;
    }

    public Fighter MakeEnemy(int ch, int idx, float px)
    {
        JNode C = Db.Ch(ch), e = C.Get("enemies").At(idx);
        bool boss = e.Bk("boss");
        float t = ch * 0.55f + idx * 0.25f;
        Fighter f = new Fighter();
        f.name = e.S("name"); f.ai = true; f.weapon = e.S("weapon"); f.hat = e.S("hat"); f.coat = e.S("coat"); f.accent = C.S("accent");
        f.boss = boss; f.beard = boss; f.x = px;
        f.maxHp = Mathf.Round(90 + ch * 24 + idx * 12 + (boss ? 40 : 0));
        f.dmgMul = 0.8f + ch * 0.14f + idx * 0.06f + (boss ? 0.15f : 0);
        f.def = ch * 0.045f + (boss ? 0.05f : 0);
        f.speed = 270 + ch * 8;
        f.aiAggr = Pz.Clamp(0.45f + t * 0.14f, 0, 1); f.aiBlock = Pz.Clamp(0.22f + t * 0.13f, 0, 0.8f);
        f.aiCrouch = Pz.Clamp(0.12f + t * 0.1f, 0, 0.6f); f.aiComboP = Pz.Clamp(0.25f + t * 0.18f, 0, 0.9f); f.aiSpecial = 0.5f + ch * 0.2f;
        f.look = "e_" + ch + "_" + idx;
        if (U.Eq(mode, "campaign"))
        {
            // қиындық: кезең нөмірі бойынша (веб: campDiff)
            int s = Mathf.Clamp(ch * 4 + stageIdx, 0, Db.NCH * 4 - 1); bool bs = stageIdx == 3;
            f.maxHp = Mathf.Round(f.maxHp * (1 + s * 0.012f + (bs ? 0.06f : 0)));
            f.dmgMul *= 1 + s * 0.01f + (bs ? 0.04f : 0);
            float ai = s * 0.004f;
            f.aiAggr = Pz.Clamp(f.aiAggr + ai, 0.05f, 1); f.aiBlock = Pz.Clamp(f.aiBlock + ai, 0.05f, 1); f.aiComboP = Pz.Clamp(f.aiComboP + ai, 0.05f, 1);
        }
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
        if (U.Eq(md, "tour")) { run = MakeBracket(ch, TierOpen(ch, Game.S.tourTier) ? Game.S.tourTier : 0); return; }   // торды экран көрсетеді
        if (U.Eq(md, "surv")) run = new Run();
        if (U.Eq(md, "boss")) { run = new Run(); run.list = BossList(ch); }
        NextFight();
    }
    public int duelIdx, duelCh = -1;
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
        bossIntro = U.Eq(mode, "boss");
        if (bossIntro) introT = 3.6f;
        if (U.Eq(mode, "campaign"))
        {
            introT = 3.7f;
            says.Clear();
            SayLine a = new SayLine(); a.enemy = true; a.name = enemy.name; a.text = Game.Fix(Db.Taunts.At(chapter).At(stageIdx).Str()); a.t0 = 0.35f; a.t1 = 1.95f; says.Add(a);
            SayLine b = new SayLine(); b.enemy = false; b.name = player.name;
            b.text = Game.Fix(Db.Replies.At((int)(U.Rand() * Db.Replies.Count())).Str()); b.t0 = 2.0f; b.t1 = 3.6f; says.Add(b);
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
        Fighter p = player, e = enemy;
        int winner = 0;
        if (p.hp > e.hp) winner = 1; else if (e.hp > p.hp) winner = 2;
        if (winner == 1) pWins++; else if (winner == 2) eWins++;
        if (!ko) roundMsg = "УАҚЫТ БІТТІ!";
        else if (winner == 1) roundMsg = p.hp >= p.maxHp ? "МІНСІЗ!" : "СОЙҚАНДЫ ЖЕҢІС!";
        else roundMsg = "ҚҰЛАДЫҢ...";
        roundWinner = winner == 1 ? p.name + " раундты ұтты" : winner == 2 ? e.name + " раундты ұтты" : "Тең түсті";
        loser = winner == 1 ? e : winner == 2 ? p : null;
        if (winner == 1 && p.hitsTaken == 0) Medal("perfect");
        p.active = false; e.active = false;
        phase = ROUNDEND; pt = 0;
    }

    void EndMatch()
    {
        phase = MATCHEND; pt = 0;
        won = pWins >= needWins;
        Fighter w = won ? player : enemy;
        if (w.state != Fighter.DEAD) w.Set(Fighter.VICTORY);
        reward = 0; runMsg = "";
        SaveData S = Game.S;
        if (U.Eq(mode, "campaign") && won)
        {
            int ch = chapter, si = stageIdx;
            bool first = S.prog[ch] <= si;
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
        else if (U.Eq(mode, "quick") && won) reward = (int)((25 + chapter * 10) * ECO_K);
        else if (U.Eq(mode, "tour")) EndTour(S);
        else if (U.Eq(mode, "surv")) EndSurv(S);
        else if (U.Eq(mode, "duel")) EndDuel(S);
        else if (U.Eq(mode, "boss")) EndBoss(S);
        if (won) Medal("first");
        if (player.blocks >= 10) Medal("block");
        if (reward > 0) S.coins += reward;
        // тәжірибе (веб: skMatchXp)
        xpGot = !won ? 8 : Mathf.RoundToInt(24 + chapter * 6 + stageIdx * 4 + (enemy.boss ? 30 : 0));
        if (U.Eq(mode, "train") && won) xpGot = Mathf.RoundToInt(xpGot * 0.6f);
        lvlCoins = Progress.GainXp(xpGot);
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
        slow = 1.1f * Db.PaceK("ko");
        flash = Mathf.Max(flash, 0.12f);
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
        sndQ.Add("skill_cast"); sndQ.Add("skill_" + d.kind);
        if (U.Eq(d.id, "tulpar")) sndQ.Add("horse");
        if (U.Eq(d.id, "boran")) sndQ.Add("blizzard");
        if (!isAi && f == player && Skills.FirstUse(d)) { skFirstName = d.name; skFirstT = 0; }
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

        float dt = rdt;
        if (slow > 0) { slow -= rdt; dt *= 0.28f; }
        if (freeze > 0) { freeze -= rdt; dt = 0; }

        if (phase == INTRO && pt > introT) { phase = FIGHT; pt = 0; player.active = true; enemy.active = true; }
        else if (phase == FIGHT)
        {
            timer -= dt; matchT += dt;
            if (player.state == Fighter.DEAD || enemy.state == Fighter.DEAD) { if (slow <= 0) EndRound(true); }
            else if (timer <= 0) { timer = 0; EndRound(false); }
        }
        else if (phase == ROUNDEND && pt > 2.6f)
        {
            if (pWins >= needWins || eWins >= needWins) EndMatch();
            else { int kp = pWins, ke = eWins, r = round + 1; StartRound(); pWins = kp; eWins = ke; round = r; }
        }

        if (dt > 0)
        {
            player.Update(dt, input); enemy.Update(dt, null);
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
        if (player.state == Fighter.DEAD || enemy.state == Fighter.DEAD) { zoomT = Mathf.Max(zoomT, 1.2f); lbT = 60; }
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
