// SHAIQAS Core · Жауынгер: күйлер, соққылар, қорғаныс, лақтыру, құлау, жасанды ақыл және серіппелі анимация.
// Веб-нұсқадағы Fighter класының тікелей аудармасы (әдістер жүйесі мен бас жау қабілеттері — келесі кезеңде).
using UnityEngine;
public class Fighter
{
    public const int IDLE = 0, WALK = 1, JUMP = 2, CROUCH = 3, DASH = 4, ATTACK = 5, HIT = 6, DOWN = 7, DEAD = 8, VICTORY = 9;

    // --- баптау ---
    public string name = "", weapon = "soyil", hat = "kalpak", coat = "chapan", accent = "#f2b840", heroId = null, look = "";
    public bool ai, active, npc, boss, big, slim, beard, canPhase3, phase2, phase3;
    public float speed = 300, maxHp = 100, dmgMul = 1, def = 0, atkSpd = 1, ruxMul = 1, regen = 0;
    public float aiAggr = 0.5f, aiBlock = 0.3f, aiCrouch = 0.2f, aiComboP = 0.3f, aiSpecial = 0.5f;
    public float buildWx = 1, buildWy = 1;

    // --- күй ---
    public float x, y = Db.GROUND, vx, vy, push, hp, hpLag, rux, sc;
    public int facing = 1, state = IDLE, comboIdx = -1, hitIdx, hitsTaken, blocks;
    public float st, at, flash, inv, dashDir, ghostT;
    public Move cm;
    public bool hitDone, queued, lunged, waved, airUsed, guard, wasAir, lowHit;
    public Fighter opp;
    public Match game;

    // --- қару ---
    public float wpDmg = 1, wpSpd = 1, wpLen = 92;
    public string[] combo = { "l1", "l2", "l3" };
    public string special = "spin";

    // --- ЖИ ---
    public float aiT;
    public string aiA = "wait";
    public int aiCombo;
    bool reacted, punished;

    // --- анимация ---
    public float[] cur, sp, sv, tg, lastPose;
    public float gaitPh, gaitW, gaitL = 46, gaitDir = 1, squash = 1, landT, blockT, regrip, regripT;
    int prevState; float prevSt, prevPush; bool prevOg = true;
    int reactH; bool reactHeavy, downRoll;
    float[] rollHw;
    public float[] fp;      // құлап-аунау кезіндегі айналу нүктесі (null — жоқ)
    public float rot;       // ағымдағы құлау бұрышы (сурет үшін)
    public float pw = 1;    // PACE жергілікті уақыт

    public Fighter() { }

    /// <summary>Барлық параметр берілгеннен кейін бір рет шақырылады.</summary>
    public void Init()
    {
        JNode W = Db.Weapon(weapon);
        wpDmg = W.Get("dmg").F(1); wpSpd = W.Get("spd").F(1); wpLen = W.Get("len").F(92);
        JNode cb = W.Get("combo");
        if (cb.Count() > 0) { combo = new string[cb.Count()]; for (int i = 0; i < cb.Count(); i++) combo[i] = cb.At(i).Str(); }
        string spc = W.S("special"); if (U.Len(spc) > 0) special = spc;
        sc = Db.S * (boss ? 1.08f : 1f) * (big ? 1.06f : 1f) * (slim ? 0.97f : 1f);
        hp = maxHp; hpLag = maxHp; rux = 0;
        at = Pz.Rand(0, 3);
        float[] start = Pz.Get(npc ? "stand" : "idle");
        cur = Pz.New(); sp = Pz.New(); sv = Pz.New(); tg = Pz.New(); lastPose = Pz.New();
        Pz.Copy(cur, start); Pz.Copy(sp, start); Pz.Copy(tg, start); Pz.Copy(lastPose, start);
        regripT = Pz.Rand(3, 6);
        prevState = state;
    }

    public void Set(int s) { if (state != s) { state = s; st = 0; } }
    public bool OnGround() { return y >= Db.GROUND - 0.5f; }
    public bool IsCrouching() { return state == CROUCH || (state == ATTACK && cm != null && cm.low); }
    public void AddRux(float v) { rux = Mathf.Min(100, rux + v * ruxMul * (v > 0 ? Db.PaceK("rux") : 1)); }
    public float Fierce() { return Pz.Clamp((aiAggr - 0.45f) / 0.5f, 0, 1); }

    float PaceWarp()
    {
        if (npc || game == null || !game.live) return 1;
        if (state == ATTACK) return y < Db.GROUND - 0.5f && cm != null && cm.air ? Db.PaceK("jump") : Db.PaceK("anim");
        if (state == HIT || state == DOWN || state == DASH) return Db.PaceK("anim");
        if (state == JUMP) return Db.PaceK("jump");
        return 1;
    }

    /// <summary>Бір қадам. it — ойыншының ниеті (ЖИ үшін null).</summary>
    public void Update(float dt0, Intent it0)
    {
        pw = PaceWarp();
        float dt = dt0 * pw, rdt = dt0;
        st += dt; at += dt;
        flash = Mathf.Max(0, flash - dt);
        hpLag = hpLag > hp ? Mathf.Max(hp, hpLag - 40 * dt) : hp;
        if (npc) { Animate(dt); return; }
        if (regen > 0 && active && hp > 0) hp = Mathf.Min(maxHp, hp + regen * rdt);
        if (boss && ai && !phase2 && hp > 0 && hp < maxHp * 0.5f)
        {
            phase2 = true; speed *= 1.15f; atkSpd *= 1.18f; rux = 100;
            aiAggr = Mathf.Min(1, aiAggr + 0.2f); aiBlock = Mathf.Min(0.85f, aiBlock + 0.15f); aiComboP = Mathf.Min(1, aiComboP + 0.2f);
            if (game != null) game.PhaseFx(this);
        }
        bool og = OnGround();
        guard = false;
        switch (state)
        {
            case DEAD: case VICTORY: vx = 0; break;
            case DOWN: vx = 0; if (st > 1.25f) Set(IDLE); break;
            case HIT: vx = 0; if (st > 0.3f) Set(IDLE); break;
            case DASH:
                vx = dashDir * (dashDir * facing > 0 ? 640 : 560) * (1 - st / 0.26f);
                if (st > 0.24f) Set(IDLE);
                break;
            case ATTACK: UpdAttack(og, dt, it0); break;
            default:
                {
                    Intent it = active ? (ai ? AiIntent(dt) : (it0 != null ? it0 : Empty())) : Empty();
                    if (opp != null && og) facing = opp.x >= x ? 1 : -1;
                    bool fwd = it.move * facing > 0, back = it.move * facing < 0;
                    float dist = opp != null ? Mathf.Abs(opp.x - x) : 999;
                    if (og && it.dash != 0)
                    {
                        dashDir = it.dash; Set(DASH); st = 0;
                        if (it.dash * facing < 0) inv = 0.24f;
                        Fx(Match.FX_DUST, x, y, -it.dash, 6);
                    }
                    else if (!og)
                    {
                        if (it.j && !airUsed) { airUsed = true; StartAttack("air", -1); }
                        else if (it.k && !airUsed) { airUsed = true; StartAttack("akick", -1); }
                        else Set(JUMP);
                    }
                    else if (it.l)
                    {
                        if (rux >= 100) { rux = 0; StartAttack(special, -1); if (game != null) game.SpecialFx(this); }
                        else StartAttack(fwd ? "spinslash" : "heavy", -1);
                    }
                    else if (it.down)
                    {
                        if (it.j) StartAttack("cslash", -1);
                        else if (it.k) StartAttack("ckick", -1);
                        else { vx = 0; guard = back; Set(CROUCH); }
                    }
                    else if (it.j)
                    {
                        if (fwd) StartAttack("thrust", -1); else if (back) StartAttack("rise", -1); else StartAttack(combo[0], 0);
                    }
                    else if (it.k)
                    {
                        if (back && dist < 150 && opp != null && opp.OnGround() && opp.state != DOWN) StartAttack("throw", -1);
                        else StartAttack(fwd ? "round" : "kick", -1);
                    }
                    else
                    {
                        vx = it.move * speed * Db.PaceK("move") * (back ? 0.62f : 1f);
                        guard = back;
                        if (it.up) { vy = -920; airUsed = false; og = false; Set(JUMP); }
                        else Set(it.move != 0 ? WALK : IDLE);
                    }
                    break;
                }
        }
        vy += Db.GRAV * dt;
        x += (vx + push) * dt;
        y += vy * dt;
        push = Pz.Toward(push, 0, 1900 * dt);
        if (y >= Db.GROUND) { if (wasAir) Fx(Match.FX_DUST, x, Db.GROUND, 0, 8); y = Db.GROUND; vy = 0; }
        wasAir = y < Db.GROUND - 30;
        inv = Mathf.Max(0, inv - dt);
        x = Pz.Clamp(x, 90, Db.ARENA - 90);
        Separate();
        Animate(dt);
    }

    static Intent emptyIt = new Intent();
    static Intent Empty() { emptyIt.Clear(); return emptyIt; }

    void Fx(int kind, float fx, float fy, float dir, int n) { if (game != null) game.Fx(kind, fx, fy, dir, n, this); }

    void Separate()
    {
        Fighter o = opp;
        if (o == null || o.state == DEAD || state == DEAD) return;
        float dx = o.x - x, ad = Mathf.Abs(dx), sg = dx >= 0 ? 1 : -1;
        // денелер бір-біріне кірмейді — жамбастың нақты орнымен
        float MIN = 112 * (sc + o.sc) / (2 * Db.S);
        float vd = sg * ((o.x + o.cur[Pz.HX] * o.facing * o.sc) - (x + cur[Pz.HX] * facing * sc));
        if (vd < MIN && Mathf.Abs(o.y - y) < 150) x -= sg * Mathf.Min(MIN - vd, MIN) * 0.5f;
        if (ad > 900) x = o.x - sg * 900;
    }

    public void StartAttack(string id, int ci)
    {
        Move bs = Move.Base(id);
        float spd = wpSpd * atkSpd;
        Move m = bs.Copy();
        m.w = bs.w / spd; m.a = bs.a / spd; m.r = bs.r / spd;
        m.dmg = bs.dmg * (bs.foot ? 1 : wpDmg) * dmgMul;
        m.reach = bs.reach * (sc / Db.S) + (bs.foot ? 0 : (wpLen - 92) * sc * 0.75f);
        cm = m; comboIdx = ci;
        hitDone = false; hitIdx = 0; queued = false; lunged = false; waved = false;
        if (OnGround() && !m.air) vx = 0;
        if (ai && ci == 0)
        {
            float fz = Fierce();
            aiCombo = U.Rand() < aiComboP + Db.PaceK("aiPush") * fz ? combo.Length - 1 : (U.Rand() < 0.4f + 0.3f * fz ? 1 : 0);
        }
        state = ATTACK; st = 0;
    }

    void UpdAttack(bool og, float dt, Intent it)
    {
        Move m = cm;
        if (og && !m.air && m.leap == 0) vx = Pz.Toward(vx, 0, 2000 * dt);
        if (m.leap > 0 && og && lunged && vy >= 0) vx = Pz.Toward(vx, 0, 2400 * dt);
        if (m.lunge > 0 && !lunged && st >= m.w - 0.03f)
        {
            vx = facing * m.lunge; lunged = true;
            if (m.leap > 0) vy = -m.leap;
        }
        if (m.after && st < m.w + m.a) { ghostT -= dt; if (ghostT <= 0) { ghostT = 0.035f; Fx(Match.FX_GHOST, x, y, facing, 0); } }
        if (comboIdx >= 0 && st > m.w * 0.5f)
        {
            if (!ai && active && it != null && it.j) queued = true;
            if (ai && aiCombo > 0) queued = true;
        }
        int hits = m.hits < 1 ? 1 : m.hits;
        float sub = m.a / hits;
        if (st >= m.w && st < m.w + m.a)
        {
            int idx = Mathf.Min(hits - 1, Mathf.FloorToInt((st - m.w) / sub));
            if (idx != hitIdx) { hitIdx = idx; hitDone = false; }
            if (!hitDone) TryHit(m, idx == hits - 1);
        }
        if (m.wave && !waved && st >= m.w && (m.leap == 0 || og))
        {
            waved = true;
            if (game != null) game.SpawnWave(this, x + facing * 60 * sc, facing, m.dmg * 0.7f);
        }
        if (queued && comboIdx >= 0 && st >= m.w + m.a + m.r * 0.3f)
        {
            if (comboIdx + 1 < combo.Length) { if (ai) aiCombo--; StartAttack(combo[comboIdx + 1], comboIdx + 1); return; }
        }
        if (m.air)
        {
            if (og && st > 0.08f) Set(IDLE);
            else if (st >= m.w + m.a + m.r) Set(JUMP);
        }
        else if (st >= m.w + m.a + m.r && (m.leap == 0 || og)) Set(m.low ? CROUCH : IDLE);
    }

    void TryHit(Move m, bool last)
    {
        Fighter o = opp;
        if (o == null || o.state == DEAD || o.state == DOWN) return;
        float dx = o.x - x;
        bool front = (dx >= 0 && facing == 1) || (dx < 0 && facing == -1);
        if (!front || Mathf.Abs(dx) > m.reach || Mathf.Abs(o.y - y) > 190) return;
        if (m.IsHigh() && o.IsCrouching()) return;
        if (m.thr && !o.OnGround()) return;
        o.TakeHit(m, facing, last, this);
        hitDone = true;
    }

    public void TakeHit(Move m, int dir, bool last, Fighter from)
    {
        if (state == DEAD || state == DOWN) return;
        if (inv > 0) return;
        float pd = Db.PaceK("dmg"), pk = Db.PaceK("knock");
        if (m.thr)
        {
            hp -= m.dmg * pd * (1 - def); flash = 0.15f; hitsTaken++;
            x = Pz.Clamp(from.x - from.facing * 120, 90, Db.ARENA - 90); facing = from.facing;
            state = DOWN; st = 0; push = -from.facing * 200;
            if (game != null) { game.Hitstop(0.12f, 12); game.punch = 0.06f; }
            Fx(Match.FX_DUST, x, Db.GROUND, 0, 14);
            AddRux(6); from.AddRux(10);
            if (hp <= 0) Die();
            return;
        }
        bool airborne = !OnGround();
        float cx = x - dir * 30, cy = y - (m.IsLow() ? 80 : m.foot ? 165 : 205);
        bool crouched = state == CROUCH;
        bool blocked = guard && dir == -facing && (crouched ? !m.IsHigh() : !m.IsLow());
        if (blocked)
        {
            float chip = m.special ? 0.35f : m.heavy ? 0.25f : 0.08f;
            hp = Mathf.Max(0, hp - m.dmg * pd * chip * (1 - def));
            push = dir * m.knock * 0.5f * pk;
            AddRux(3); blocks++;
            if (game != null) { game.Fx(Match.FX_BLOCK, cx, cy, -dir, 10, this); game.Hitstop(0.04f, m.heavy ? 4 : 0); }
            if (hp <= 0) Die();
            return;
        }
        hp -= m.dmg * pd * (1 - def) * (m.anti && airborne ? 1.5f : 1f); flash = 0.15f; hitsTaken++;
        if (m.heavy && game != null) { game.punch = Mathf.Max(game.punch, 0.045f * Db.PaceK("shake")); game.flash = Mathf.Max(game.flash, 0.05f * (Db.PaceK("shake") - 1) * 5); }
        push = dir * m.knock * (last ? 1f : 0.35f) * pk;
        AddRux(6);
        if (from != null) from.AddRux(9);
        aiCombo = 0; lowHit = m.IsLow();
        if (game != null) { game.Fx(m.special ? Match.FX_HITSPEC : Match.FX_HIT, cx, cy, -dir, m.heavy ? 24 : 14, this); game.Hitstop(m.hitstop, m.heavy ? 10 : 4); }
        if (hp <= 0) Die();
        else if ((m.down && last) || (m.anti && airborne))
        {
            state = DOWN; st = 0; push = dir * Mathf.Max(300, m.knock) * 1.2f * pk; vy = 0; y = Db.GROUND;
            Fx(Match.FX_DUST, x, Db.GROUND, dir, 10);
        }
        else { state = HIT; st = 0; }
    }

    void Die() { hp = 0; state = DEAD; st = 0; if (game != null) game.Ko(this); }

    // ---------------- жасанды ақыл ----------------
    Intent aiIt = new Intent();
    Intent AiIntent(float dt)
    {
        Intent it = aiIt; it.Clear();
        Fighter o = opp;
        if (o == null || o.state == DEAD) return it;
        float dx = o.x - x, d = Mathf.Abs(dx); int dir = dx >= 0 ? 1 : -1;
        if (o.state == ATTACK && o.cm != null && !reacted && d < o.cm.reach + 60)
        {
            reacted = true;
            float r = U.Rand();
            if (o.cm.IsHigh() && r < aiCrouch) { aiA = "duck"; aiT = 0.35f; }
            else if (r > 0.9f && aiAggr > 0.5f) { aiA = "dodge"; aiT = 0.1f; }
            else if (r < aiBlock) { aiA = o.cm.IsLow() ? "cguard" : "guard"; aiT = 0.4f; }
        }
        if (o.state != ATTACK) reacted = false;
        float fz = Fierce(), aiK = 1 + (Db.PaceK("ai") - 1) * (0.25f + 0.75f * fz);
        if (o.state == ATTACK && o.cm != null && o.st > o.cm.w + o.cm.a && !punished && OnGround())
        {
            punished = true;
            float reach = 205 * (sc / Db.S) + (wpLen - 92) * sc * 0.75f;
            if (U.Rand() < (0.15f + 0.55f * fz) * (Db.PaceK("ai") > 1 ? 1 : 0))
            {
                if (d < reach + 20) { aiA = U.Rand() < 0.3f + 0.3f * fz ? "l" : "j"; aiT = 0.3f; }
                else if (d < reach + 200) { aiA = "approach"; aiT = 0.18f; }
            }
        }
        if (o.state != ATTACK) punished = false;
        if (o.state == DOWN && !U.Eq(aiA, "retreat") && !U.Eq(aiA, "wait"))
        {
            aiA = d < 260 ? (fz > 0.5f ? "wait" : "retreat") : (fz > 0.3f ? "approach" : "wait"); aiT = 0.4f / aiK;
        }
        aiT -= dt * aiK;
        if (aiT <= 0) AiChoose(d);
        switch (aiA)
        {
            case "approach": it.move = dir; break;
            case "retreat": case "guard": it.move = -dir; break;
            case "cguard": it.down = true; it.move = -dir; break;
            case "duck": it.down = true; break;
            case "j": it.j = true; aiA = "wait"; break;
            case "fwdj": it.j = true; it.move = dir; aiA = "wait"; break;
            case "k": it.k = true; aiA = "wait"; break;
            case "ck": it.down = true; it.k = true; aiA = "wait"; break;
            case "cj": it.down = true; it.j = true; aiA = "wait"; break;
            case "l": it.l = true; aiA = "wait"; break;
            case "dodge": it.dash = -dir; aiA = "wait"; aiT = 0.3f; break;
            case "round": it.k = true; it.move = dir; aiA = "wait"; break;
            case "throw": it.k = true; it.move = -dir; aiA = "wait"; break;
            case "rise": it.j = true; it.move = -dir; aiA = "wait"; break;
            case "jumpatk": it.up = true; it.move = dir; aiA = "airj"; aiT = 0.6f; break;
            case "airj": it.move = dir; if (d < 230 && !OnGround()) { it.j = true; aiA = "wait"; } break;
            default: break;
        }
        return it;
    }

    static readonly string[] ACTS = { "j", "k", "round", "ck", "cj", "l", "guard", "retreat", "wait", "dodge" };
    static readonly float[] ACTW = { 0.28f, 0.09f, 0.06f, 0.10f, 0.07f, 0.08f, 0.12f, 0.07f, 0.10f, 0.03f };

    void AiChoose(float d)
    {
        float reach = 205 * (sc / Db.S) + (wpLen - 92) * sc * 0.75f;
        aiT = Pz.Rand(0.18f, 0.5f) * (1.3f - aiAggr * 0.6f);
        float r = U.Rand(), fz = Fierce();
        if (d > reach + 30)
        {
            if (r < 0.06f) aiA = "jumpatk";
            else if (r < 0.28f && d < reach + 140) aiA = "fwdj";
            else aiA = U.Rand() < aiAggr + 0.3f + Db.PaceK("aiPush") * (0.4f + fz) ? "approach" : "wait";
            return;
        }
        if (rux >= 100 && U.Rand() < aiSpecial) { aiA = "l"; return; }
        if (!opp.OnGround() && d < 230 && U.Rand() < 0.5f + aiAggr * 0.3f) { aiA = "rise"; return; }
        if (d < 140 && U.Rand() < 0.18f) { aiA = "throw"; return; }
        float sum = 0; for (int i = 0; i < ACTW.Length; i++) sum += ACTW[i];
        float xx = U.Rand() * sum;
        for (int i = 0; i < ACTS.Length; i++) { xx -= ACTW[i]; if (xx <= 0) { aiA = ACTS[i]; break; } }
    }

    // ---------------- анимация ----------------
    public void Animate(float dt)
    {
        dt = Mathf.Min(dt, 0.05f);
        int s0 = state; bool og = OnGround();
        Pz.Copy(lastPose, cur);
        bool G0 = game != null && game.live;
        float rate = 16, zeta = 0.9f;
        if (s0 == HIT && (prevState != HIT || st < prevSt)) OnHitReact();
        if ((s0 == DOWN || s0 == DEAD) && prevState != s0)
        {
            Move omv = opp != null && opp.state == ATTACK ? opp.cm : null;
            downRoll = s0 == DOWN && ((omv != null && omv.heavy) || U.Rand() < 0.45f);
            sv[Pz.T] -= 7; sv[Pz.HX] -= 240; sv[Pz.FA1] -= 6; sv[Pz.W] -= 8;
        }
        if (s0 != DOWN) rollHw = null;
        if ((s0 == DOWN && prevSt < 0.38f && st >= 0.38f) || (s0 == DEAD && prevSt < 0.42f && st >= 0.42f))
        {
            sv[Pz.FFY] -= 260; sv[Pz.FA1] += 7; sv[Pz.FA2] += 9; sv[Pz.W] += 9;
            if (G0) Fx(Match.FX_DUST, x - facing * 70 * sc, Db.GROUND, -facing, 7);
        }
        if (og && !prevOg && s0 != DOWN && s0 != DEAD) { landT = 0.26f; sv[Pz.HY] += 220; sv[Pz.T] += 2.5f; sv[Pz.FA1] -= 3; }
        if (guard && s0 != HIT && s0 != DOWN && Mathf.Abs(push) > Mathf.Abs(prevPush) + 60)
        {
            blockT = 0.22f; sv[Pz.HX] -= 260; sv[Pz.T] -= 3.5f; sv[Pz.W] += 7; sv[Pz.FA2] += 5;
        }
        landT = Mathf.Max(0, landT - dt); blockT = Mathf.Max(0, blockT - dt);

        if (npc)
        {
            Pz.Copy(tg, Pz.Get("stand")); float b = Mathf.Sin(at * 1.6f);
            tg[Pz.HY] += b * 1.6f; tg[Pz.T] += b * 0.012f; tg[Pz.FA2] += b * 0.04f; tg[Pz.BA2] += b * 0.03f; rate = 7; zeta = 1;
        }
        else if (s0 == ATTACK)
        {
            Move m = cm; float s = st, Wd = m.w, Ac = m.a, Rc = m.r;
            float[] PW = Pz.Get(m.pw);
            string psName = m.hits > 1 && U.Len(m.alt) > 0 && s < Wd + Ac ? (hitIdx % 2 == 1 ? m.alt : m.ps) : m.ps;
            float[] PS = Pz.Get(psName);
            if (s < Wd)
            {
                float k = s / Wd;
                Pz.Extrap(tg, PS, PW, (m.heavy || m.special ? 0.17f : 0.1f) * Pz.Smooth01((k - 0.15f) / 0.85f));
                if (m.heavy || m.special) tg[Pz.HY] += 8 * Pz.Smooth01(k);
                rate = m.heavy ? 20 : 28; zeta = 0.8f;
            }
            else if (s < Wd + Ac) { Pz.Extrap(tg, PW, PS, 0.09f); rate = m.hits > 1 ? 52 : 46; zeta = 0.48f; }
            else
            {
                float k = Pz.Clamp((s - Wd - Ac) / Rc, 0, 1);
                Pz.Extrap(tg, PW, PS, 0.09f * (1 - Pz.Smooth01(k * 2.2f)));
                float[] rest = m.low ? Pz.Get(guard ? "cguard" : "crouch") : m.air ? Pz.Get("jump") : Pz.Get("idle");
                Pz.Mix(tg, tg, rest, 0.6f * Pz.Smooth01((k - 0.4f) / 0.6f));
                rate = 22; zeta = 0.6f;
            }
        }
        else if (s0 == CROUCH)
        {
            Pz.Copy(tg, Pz.Get(guard ? "cguard" : "crouch"));
            float b = Mathf.Sin(at * 2.6f); tg[Pz.HY] += b * 1.5f; tg[Pz.W] += b * 0.04f; tg[Pz.FA2] += b * 0.03f;
            rate = 26; zeta = 0.72f;
        }
        else if (s0 == DASH)
        {
            bool fwd = dashDir * facing > 0; float per = 0.26f, k = (st % per) / per, hop = Mathf.Sin(k * Mathf.PI);
            Pz.Copy(tg, Pz.Get(fwd ? "dashF" : "dashB"));
            tg[Pz.HY] -= hop * 12; tg[Pz.FFY] -= hop * 16; tg[Pz.BFY] -= hop * 18;
            rate = 30; zeta = 0.7f;
        }
        else if (s0 == HIT)
        {
            float k = st / 0.3f;
            Pz.Copy(tg, Pz.Get(reactH == 2 ? "hitLow" : reactH == 1 ? "hitHigh" : "hitMid"));
            if (reactHeavy)
            {
                Pz.Mix(tg, tg, Pz.Get("hitHeavy"), 0.55f);
                float s2 = Mathf.Sin(Pz.Clamp(k, 0, 1) * Mathf.PI); tg[Pz.BFX] -= 18 * s2; tg[Pz.BFY] -= 12 * s2;
            }
            Pz.Mix(tg, tg, Pz.Get("guard"), 0.55f * Pz.Smooth01((k - 0.5f) / 0.5f));
            rate = 26; zeta = 0.42f;
        }
        else if (s0 == DOWN)
        {
            float t = st;
            if (t < 0.38f) Pz.Mix(tg, Pz.Get("hitHeavy"), Pz.Get("fallen"), Pz.Smooth01(t / 0.3f));
            else if (downRoll)
            {
                if (t < 0.74f) Pz.Copy(tg, Pz.Get("fallen"));
                else if (t < 0.9f) Pz.Copy(tg, Pz.Get("tuck"));
                else { Pz.Mix(tg, Pz.Get("tuck"), Pz.Get("crouch"), Pz.Smooth01((t - 0.9f) / 0.18f)); Pz.Mix(tg, tg, Pz.Get("guard"), Pz.Smooth01((t - 1.08f) / 0.17f)); }
            }
            else
            {
                if (t < 0.8f) Pz.Copy(tg, Pz.Get("fallen"));
                else { Pz.Mix(tg, Pz.Get("fallen"), Pz.Get("getup"), Pz.Smooth01((t - 0.8f) / 0.14f)); Pz.Mix(tg, tg, Pz.Get("guard"), Pz.Smooth01((t - 1.0f) / 0.22f)); }
            }
            if (t > 0.45f && t < 0.8f) tg[Pz.HY] += Mathf.Sin(at * 7) * 1.5f;
            rate = 18; zeta = 0.6f;
        }
        else if (s0 == DEAD) { Pz.Mix(tg, Pz.Get("hitHeavy"), Pz.Get("dead"), Pz.Smooth01(st / 0.32f)); rate = 14; zeta = 0.5f; }
        else if (s0 == JUMP)
        {
            Pz.Mix(tg, Pz.Get("jumpUp"), Pz.Get("jump"), Pz.Smooth01((vy + 650) / 600));
            Pz.Mix(tg, tg, Pz.Get("jumpFall"), Pz.Smooth01((vy - 150) / 450));
            tg[Pz.T] += Pz.Clamp(vx * facing / 2200, -0.14f, 0.14f);
            rate = 16; zeta = 0.75f;
        }
        else if (s0 == VICTORY)
        {
            Pz.Copy(tg, Pz.Get("victory")); float b = Mathf.Sin(at * 2.2f);
            tg[Pz.HY] += b * 1.8f; tg[Pz.T] += b * 0.015f; tg[Pz.W] += Mathf.Sin(at * 3) * 0.05f; tg[Pz.BA2] += b * 0.05f;
            rate = 9; zeta = 0.8f;
        }
        else if (s0 == WALK)
        {
            Pz.Copy(tg, Pz.Get(guard ? "guard" : "idle"));
            float d = vx * facing >= 0 ? 1 : -1;
            tg[Pz.T] += d > 0 ? 0.08f : -0.035f; tg[Pz.HX] += d * 3;
            rate = 20; zeta = 0.85f;
        }
        else
        {
            Pz.Copy(tg, Pz.Get(guard ? "guard" : "idle"));
            float b = Mathf.Sin(at * 2.4f), b2 = Mathf.Sin(at * 0.7f);
            tg[Pz.HY] += b * 2.2f; tg[Pz.T] += b * 0.016f - b2 * 0.02f; tg[Pz.HX] += b2 * 3.5f;
            tg[Pz.FA1] += b * 0.03f; tg[Pz.FA2] += b * 0.05f + Mathf.Sin(at * 1.3f) * 0.04f;
            tg[Pz.W] += b * 0.04f + Mathf.Sin(at * 1.3f + 0.8f) * 0.07f; tg[Pz.BA2] += b * 0.04f;
            rate = 14; zeta = 0.9f;
            if (G0)
            {
                int ph = game.phase;
                if ((ph == Match.ROUNDEND || ph == Match.MATCHEND) && game.loser == this)
                {
                    Pz.Copy(tg, Pz.Get("slump")); float hb = Mathf.Sin(at * 5.5f);
                    tg[Pz.HY] += hb * 2.6f; tg[Pz.T] += hb * 0.035f; tg[Pz.BA2] += hb * 0.05f; rate = 6;
                }
                else if (ph == Match.ROUNDEND && game.loser != null && game.loser != this)
                {
                    Pz.Mix(tg, tg, Pz.Get("victory"), 0.75f * Pz.Smooth01((game.pt - 0.5f) / 0.6f)); rate = 9;
                }
            }
        }
        if (landT > 0 && (s0 == IDLE || s0 == WALK || s0 == CROUCH)) Pz.Mix(tg, tg, Pz.Get("land"), 0.85f * Pz.Smooth01(landT / 0.26f));

        // серіппелі интеграция
        int n = Mathf.Max(1, Mathf.CeilToInt(dt * 120)); float h = dt / n, om = rate * 1.5f;
        for (int i = 0; i < Pz.N; i++)
        {
            float w0 = om * Pz.GRP0[i], K = w0 * w0, C = 2 * zeta * Pz.GRP1[i] * w0, T = tg[i];
            float xx = sp[i], v = sv[i];
            for (int j = 0; j < n; j++) { v += (K * (T - xx) - C * v) * h; xx += v * h; }
            if (!(xx == xx)) { xx = T; v = 0; }
            sp[i] = xx; sv[i] = v; cur[i] = xx;
        }

        // қадам циклы
        bool walking = s0 == WALK && og && !npc;
        gaitW = Pz.Toward(gaitW, walking ? 1 : 0, dt * 7);
        if (walking)
        {
            float d = vx * facing >= 0 ? 1 : -1;
            gaitDir = d; gaitL = d > 0 ? 46 : 36;
            gaitPh = (gaitPh + Mathf.Abs(vx) / sc * dt / (2 * gaitL)) % 1f;
        }
        if (gaitW > 0.001f)
        {
            float gw = gaitW, L = gaitL, d = gaitDir, ph = gaitPh, pb = (ph + 0.5f) % 1f;
            float offF, liftF, offB, liftB;
            if (ph < 0.5f) { offF = d * L * (0.5f - 2 * ph); liftF = 0; } else { float s = (ph - 0.5f) * 2; offF = d * L * (-0.5f + Pz.Smooth01(s)); liftF = Mathf.Sin(s * Mathf.PI) * 14; }
            if (pb < 0.5f) { offB = d * L * (0.5f - 2 * pb); liftB = 0; } else { float s = (pb - 0.5f) * 2; offB = d * L * (-0.5f + Pz.Smooth01(s)); liftB = Mathf.Sin(s * Mathf.PI) * 14; }
            cur[Pz.FFX] += offF * gw; cur[Pz.FFY] -= liftF * gw; cur[Pz.BFX] += offB * gw; cur[Pz.BFY] -= liftB * gw;
            float c2 = Mathf.Cos(ph * 4 * Mathf.PI), s1 = Mathf.Sin(ph * 2 * Mathf.PI);
            cur[Pz.HY] += (c2 * 2.8f + 0.6f) * gw; cur[Pz.T] += c2 * 0.014f * gw;
            cur[Pz.BA1] += s1 * 0.26f * gw * d; cur[Pz.BA2] += s1 * 0.32f * gw * d; cur[Pz.FA1] -= s1 * 0.07f * gw * d;
            cur[Pz.W] += Mathf.Sin(ph * 4 * Mathf.PI) * 0.04f * gw;
        }
        // қаруды қайта қысу
        if (s0 == IDLE && og && !npc && !(G0 && game.loser == this))
        {
            regripT -= dt;
            if (regripT <= 0) { regripT = Pz.Rand(3.5f, 7); regrip = 0.6f; }
        }
        else regrip = 0;
        if (regrip > 0)
        {
            regrip = Mathf.Max(0, regrip - dt);
            float k = 1 - regrip / 0.6f, s = Mathf.Sin(k * Mathf.PI);
            cur[Pz.W] -= s * 0.42f - Mathf.Sin(k * Mathf.PI * 2) * 0.12f; cur[Pz.FA2] -= s * 0.2f; cur[Pz.FA1] += s * 0.06f;
        }
        float twirl = 0;
        if (s0 == VICTORY) twirl = -(1 - Pz.EaseOut3(st / 1.2f)) * Mathf.PI * 4;
        else if (G0 && !npc && game.phase == Match.INTRO) { float k = Pz.Clamp((game.pt - 0.5f) / 0.9f, 0, 1); if (k > 0 && k < 1) twirl = Pz.Smooth01(k) * Mathf.PI * 2; }
        else if (G0 && s0 == IDLE && game.phase == Match.ROUNDEND && game.loser != null && game.loser != this) { float k = Pz.Clamp((game.pt - 0.7f) / 1, 0, 1); if (k > 0 && k < 1) twirl = -Pz.EaseOut3(k) * Mathf.PI * 4; }
        if (twirl != 0) { float s = Mathf.Sin((Mathf.Abs(twirl) % (Mathf.PI * 2)) * 0.5f); cur[Pz.W] += twirl; cur[Pz.FA2] += s * 0.35f; cur[Pz.FA1] += s * 0.12f; }

        float sq = 1;
        if (landT > 0) sq -= 0.085f * Mathf.Sin(Mathf.Min(1, (1 - landT / 0.26f) * 1.7f) * Mathf.PI);
        if (!og && s0 != DOWN && s0 != DEAD && vy < 0) sq += 0.06f * Pz.Smooth01((-vy - 300) / 500);
        squash = sq;
        rot = FallAngle();

        prevState = s0; prevSt = st; prevOg = og; prevPush = push;
    }

    void OnHitReact()
    {
        Move om = opp != null && opp.state == ATTACK ? opp.cm : null;
        reactH = lowHit ? 2 : om != null && om.IsHigh() ? 1 : 0;
        reactHeavy = (om != null && om.heavy) || Mathf.Abs(push) > 380;
        float s = reactHeavy ? 1.7f : 1f;
        if (reactH == 1) { sv[Pz.T] -= 10 * s; sv[Pz.HX] -= 260 * s; sv[Pz.FA1] -= 7 * s; sv[Pz.W] -= 8 * s; sv[Pz.HY] -= 60 * s; }
        else if (reactH == 2) { sv[Pz.HY] += 200 * s; sv[Pz.FFY] -= 320 * s; sv[Pz.T] += 5 * s; sv[Pz.FA1] += 3 * s; }
        else { sv[Pz.T] += 9 * s; sv[Pz.HX] -= 340 * s; sv[Pz.FA2] += 6 * s; sv[Pz.W] += 5 * s; sv[Pz.HY] += 80 * s; }
        sv[Pz.BA1] -= 5 * s; sv[Pz.BA2] -= 6 * s;
    }

    float FallAngle()
    {
        fp = null;
        float A = -Mathf.PI / 2 * 0.93f;
        if (state == DEAD)
        {
            float t = st;
            if (t < 0.42f) return A * Mathf.Pow(t / 0.42f, 1.7f);
            return A * (1 - 0.1f * Mathf.Sin(Pz.Clamp((t - 0.42f) / 0.2f, 0, 1) * Mathf.PI) - 0.03f * Mathf.Sin(Pz.Clamp((t - 0.62f) / 0.12f, 0, 1) * Mathf.PI));
        }
        if (state == DOWN)
        {
            float t = st;
            if (t < 0.38f) return A * Mathf.Pow(t / 0.38f, 1.7f);
            if (t < 0.56f) return A * (1 - 0.1f * Mathf.Sin((t - 0.38f) / 0.18f * Mathf.PI));
            if (!downRoll) { if (t < 0.8f) return A; return A * (1 - Pz.Smooth01((t - 0.8f) / 0.4f)); }
            if (t < 0.74f) return A;
            float q0 = cur[Pz.HX] + Mathf.Sin(cur[Pz.T]) * Db.TOR * 0.62f, q1 = cur[Pz.HY] - Mathf.Cos(cur[Pz.T]) * Db.TOR * 0.62f;
            if (rollHw == null) { float cs = Mathf.Cos(A), sn = Mathf.Sin(A); rollHw = new float[] { cs * q0 - sn * q1, sn * q0 + cs * q1 }; }
            if (t < 0.88f) { float e = Pz.Smooth01((t - 0.74f) / 0.14f); fp = new float[] { rollHw[0] - 6 * e, rollHw[1], q0, q1 }; return A - 0.8f * e; }
            float e2 = Pz.Smooth01((t - 0.88f) / 0.32f);
            fp = new float[] { Pz.Lerp(rollHw[0] - 6, q0, e2), Pz.Lerp(rollHw[1], q1, e2) - Mathf.Sin(e2 * Mathf.PI) * 12, q0, q1 };
            return (A - 0.8f) * (1 - e2);
        }
        return 0;
    }

    public float SpinFacing()
    {
        if (state != ATTACK || cm == null || !cm.spin) return facing;
        float k = Pz.Clamp(st / (cm.w + cm.a * 0.8f), 0, 1), c = Mathf.Cos(k * Mathf.PI * 2);
        return facing * (Mathf.Abs(c) < 0.14f ? 0.14f * (c < 0 ? -1 : 1) : c);
    }
}
