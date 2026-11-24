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

public class Match
{
    public const int INTRO = 0, FIGHT = 1, ROUNDEND = 2, MATCHEND = 3;
    public const int FX_DUST = 0, FX_HIT = 1, FX_HITSPEC = 2, FX_BLOCK = 3, FX_GHOST = 4, FX_KO = 5, FX_SPECIAL = 6, FX_PHASE = 7, FX_WAVE = 8;
    public const string GOLD = "#f2b840";

    // --- режим ---
    public string mode = "campaign";      // campaign | train | quick
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

    // ================= ағын =================
    public void StartCampaign(int ch, int si)
    {
        mode = "campaign"; chapter = ch; stageIdx = si;
        needWins = si == 3 ? Db.BOSS_WINS : 2;
        StartMatch();
    }
    public void StartTrain(int ch) { mode = "train"; chapter = ch; stageIdx = 0; needWins = 1; StartMatch(); }

    public void StartMatch()
    {
        round = 1; pWins = 0; eWins = 0; reward = 0; runMsg = ""; matchT = 0; starsGot = 0; starsNew = 0; won = false;
        StartRound();
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
        stageId = Db.Ch(chapter).S("stage");
        player = MakePlayer(mid - 250, Game.S.hero);
        enemy = U.Eq(mode, "train") ? MakeTrainer(mid + 250) : MakeEnemy(chapter, stageIdx, mid + 250);
        enemy.facing = -1;
        player.opp = enemy; enemy.opp = player;
        phase = INTRO; pt = 0; timer = 60; introT = 2.1f; says.Clear(); sayClock = 0;
        slow = 0; freeze = 0; flash = 0; dark = 0; loser = null;
        zoom = 1.22f; lb = 60;
        cam = Pz.Clamp(enemy.x - Db.W / 2, 0, Db.ARENA - Db.W);
        waves.Clear(); fx.Clear();
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
            reward = (int)(Db.Ch(ch).Get("rewards").At(si).F() * (first ? 1f : 0.5f));
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
        else if (U.Eq(mode, "quick") && won) reward = (25 + chapter * 10) * 2;
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
    public void PhaseFx(Fighter f)
    {
        if (U.Eq(mode, "campaign") && f == enemy)
        {
            SayLine a = new SayLine(); a.enemy = true; a.name = f.name; a.text = Db.Rage.At(chapter).Str(); a.t0 = sayClock + 0.3f; a.t1 = sayClock + 2.6f; says.Add(a);
        }
        flash = 0.3f; dark = 0.5f; Hitstop(0.25f, 14); punch = 0.08f;
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
        if (U.Eq(next, "retry")) { StartMatch(); return "fight"; }
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
