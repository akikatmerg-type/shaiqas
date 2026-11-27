// SHAIQAS Core · Бұқар жырау — ұстаз: ұрыс алдындағы және ұрыстан кейінгі кеңестер (веб: ZH_ADV, zhPreAdvice, zhMatchAdvice, ZH_FS).
using UnityEngine;

/// <summary>Ұрыс статистикасы — кеңес таңдау үшін (веб: ZH_FS).</summary>
public class ZhFs
{
    public bool on, timeout;
    public int blocks, hits, foot, low, thrown, attacks, kicks, specials, skills;
    public float ruxT, minHp = 1;
    public Lst<string> ids = new Lst<string>();
}

public static class Zh
{
    static Lst<string> lastCat = new Lst<string>();
    static Lst<string> lastIdx = new Lst<string>();

    public static JNode A() { return Db.J("ZH_ADV"); }

    /// <summary>Санаттан кездейсоқ жол — соңғы таңдалғандарды қайталамай (веб: zhPick).</summary>
    public static string Pick(string cat)
    {
        JNode L = A().Get(cat); int n = L.Count(); if (n == 0) return "";
        int ci = lastCat.IndexOf(cat); string used = ci >= 0 ? lastIdx.Get(ci) : ",";
        int free = 0; for (int i = 0; i < n; i++) if (!U.Has(used, "," + i + ",")) free++;
        if (free == 0) { used = ","; free = n; }
        int k = Mathf.Min(free - 1, (int)(U.Rand() * free)), pick = 0;
        for (int i = 0; i < n; i++) { if (U.Has(used, "," + i + ",")) continue; if (k == 0) { pick = i; break; } k--; }
        used = used + pick + ",";
        // ең көбі n-1 жол есте сақталады
        int cnt = 0; for (int i = 0; i < U.Len(used); i++) if (U.Ch(used, i) == ',') cnt++;
        while (cnt - 1 > Mathf.Max(0, n - 1)) { int c2 = U.IndexOf(U.From(used, 1), ","); used = U.From(used, c2 + 1); cnt--; }
        if (ci >= 0) lastIdx.Set(ci, used); else { lastCat.Add(cat); lastIdx.Add(used); }
        return L.At(pick).Str();
    }

    public static string Name()
    {
        string id = Game.S != null ? Game.S.hero : "sharua";
        JNode h = Db.Heroes.Get(id);
        return U.Eq(id, "sharua") || h.IsNull() ? "Бауыржан" : h.S("name");
    }
    /// <summary>{N} — батыр аты, {X} — v кестесінен (кілт=мән жұптары).</summary>
    public static string Fill(string s, string[] keys, string[] vals)
    {
        s = U.Replace(s, "{N}", Name());
        if (keys != null) for (int i = 0; i < keys.Length; i++) s = U.Replace(s, "{" + keys[i] + "}", vals[i]);
        s = U.Replace(s, "..", "."); s = U.Replace(s, "!.", "!");
        return Game.Fix(s);
    }
    public static string Dot(string s)
    {
        if (U.Len(s) == 0) return s;
        char c = U.Ch(s, U.Len(s) - 1);
        return c == '.' || c == '!' || c == '?' || c == '»' ? s : s + ".";
    }
    static string Lc(string s) { return U.Len(s) == 0 ? s : U.Lower(U.Sub(s, 0, 1)) + U.From(s, 1); }
    static string Tok(string k)
    {
        if (U.Eq(k, "F")) return "→"; if (U.Eq(k, "B")) return "←"; if (U.Eq(k, "D")) return "↓"; if (U.Eq(k, "U")) return "↑";
        if (U.Eq(k, "DF")) return "↘"; if (U.Eq(k, "DB")) return "↙"; if (U.Eq(k, "UF")) return "↗"; if (U.Eq(k, "UB")) return "↖";
        return k;
    }
    public static string Seq(SkDef d) { string s = ""; for (int i = 0; i < d.seq.Length; i++) s += (i > 0 ? " " : "") + Tok(d.seq[i]); return s; }
    static string Gear(string tpl, int ch, int si)
    {
        string t = Eco.Tip(ch, si);
        while (U.Len(t) > 0 && (U.Ends(t, ".") || U.Ends(t, "!"))) t = U.Sub(t, 0, U.Len(t) - 1);
        return Fill(tpl, new string[] { "TU", "T" }, new string[] { t, Lc(t) });
    }
    static SkDef SkillPick()
    {
        Skills.Init(); Lst<SkDef> L = new Lst<SkDef>(), F = new Lst<SkDef>();
        for (int i = 0; i < Skills.All.Count(); i++) { SkDef d = Skills.All.Get(i); if (!Skills.Unlocked(d)) continue; L.Add(d); if (!Game.S.skUsed.Contains(d.id)) F.Add(d); }
        Lst<SkDef> P = F.Count() > 0 ? F : L; if (P.Count() == 0) return null;
        return P.Get(Mathf.Min(P.Count() - 1, (int)(U.Rand() * P.Count())));
    }

    public static void Reset(ZhFs F)
    {
        F.on = true; F.timeout = false; F.blocks = 0; F.hits = 0; F.foot = 0; F.low = 0; F.thrown = 0; F.attacks = 0; F.kicks = 0;
        F.specials = 0; F.skills = 0; F.ruxT = 0; F.minHp = 1; F.ids.Clear();
    }

    /// <summary>Ұрыс алдындағы кеңес (веб: zhPreAdvice). null — кеңес жоқ.</summary>
    public static string Pre(Match G)
    {
        if (U.Eq(G.mode, "arena")) return null;
        int ch = G.cfg != null ? G.cfg.ch : G.chapter;
        string s = null;
        if (U.Eq(G.mode, "campaign"))
        {
            DiffInfo D = Eco.Stage(G.chapter, G.stageIdx);
            if (D.weak) s = Gear(Pick("tGear"), G.chapter, G.stageIdx);
            else if (G.stageIdx == 3) s = Pick("preBoss");
            else if (G.stageIdx == 0) { JNode pc = A().Get("preCh").At(ch); s = pc.IsStr() ? pc.Str() : Pick("pre"); }
            else s = Pick("pre");
            // барлық тарау — жыраудың тарихи жыры (веб: frameLayerOfCh = "past")
            if (!D.weak && G.stageIdx != 0 && U.Rand() < 0.5f) s = Pick("preJyr");
        }
        else if (U.Eq(G.mode, "boss")) s = Pick("preBoss");
        else if (U.Eq(G.mode, "tour")) s = G.run != null && G.run.stage == 0 ? Pick("preTour") : Pick("pre");
        else if (U.Eq(G.mode, "surv")) s = G.run != null && G.run.wave == 0 ? Pick("preSurv") : null;
        else if (U.Eq(G.mode, "train")) s = Pick("preTrain");
        else s = Pick("pre");
        return s == null ? null : Dot(Fill(s, null, null));
    }

    /// <summary>Ұрыстан кейінгі мақтау/жұбату мен кеңес (веб: zhMatchAdvice). [0] — басы, [1] — кеңес.</summary>
    public static string[] After(Match G, ZhFs F)
    {
        bool won = G.won; Fighter e = G.enemy; bool boss = e != null && e.boss;
        string head;
        if (won) head = F.hits == 0 ? Pick("perfect") : F.minHp < 0.25f ? Pick("comeback") : boss ? Pick("bossWin") : G.matchT <= 40 ? Pick("quick") : Pick("win");
        else head = boss ? Pick("lossBoss") : (e != null && e.hp / e.maxHp < 0.25f) ? Pick("lossClose") : Pick("loss");
        if (U.Eq(G.mode, "campaign") && U.Rand() < 0.45f) head = Pick(won ? "jyrWin" : "jyrLoss");
        Lst<string> tips = new Lst<string>();
        if (U.Eq(G.mode, "campaign") && Eco.Stage(G.chapter, G.stageIdx).weak) tips.Add(Gear(Pick("tGear"), G.chapter, G.stageIdx));
        if (!won && e != null && U.Len(e.abInfo) > 0)
        {
            JNode ab = Db.J("AB_INFO").Get(e.abInfo);
            if (!ab.IsNull()) tips.Add(Fill(Pick("tBossAb"), new string[] { "A", "TIP" }, new string[] { ab.S("name"), Lc(ab.S("tip")) }));
        }
        if (F.ruxT > 5 && F.specials == 0) tips.Add(Pick("tRux"));
        if (F.hits >= 5 && F.blocks <= 1) tips.Add(Pick("tBlock"));
        if (F.low >= 3 && F.low >= F.hits * 0.4f) tips.Add(Pick("tLow"));
        if (F.foot >= 3 && F.foot >= F.hits * 0.4f) tips.Add(Pick("tKick"));
        if (F.thrown >= 2) tips.Add(Pick("tThrow"));
        if (F.skills == 0) { SkDef d = SkillPick(); if (d != null) tips.Add(Fill(Pick("tSkill"), new string[] { "S", "Q" }, new string[] { d.name, Seq(d) })); }
        if (F.timeout) tips.Add(Pick("tTime"));
        if (F.attacks < 8) tips.Add(Pick("tAttack"));
        else if (F.ids.Count() <= 2) tips.Add(Pick("tVariety"));
        if (tips.Count() == 0) tips.Add(Pick("tGood"));
        return new string[] { Fill(head, null, null), Dot(Fill(tips.Get(0), null, null)) };
    }

    /// <summary>Лоббидегі жырау сөздері (веб: zhLobbyLines): келесі жорық, жабдық, жаңа әдіс, теңге, тарих дерегі, мақал.</summary>
    public static Lst<string> LobbyLines()
    {
        Lst<string> L = new Lst<string>(); SaveData S = Game.S;
        int c = -1; for (int i = 0; i < Db.NCH; i++) if (Progress.Unlocked(i) && S.prog[i] < 4) { c = i; break; }
        if (c >= 0)
        {
            int si = Mathf.Min(3, S.prog[c]); JNode C = Db.Ch(c);
            JNode nd = C.Get("nodes").At(si), en = C.Get("enemies").At(si);
            string P = nd.IsObj() ? nd.S("n") : C.S("name"), E = en.IsObj() ? en.S("name") : "жау";
            L.Add(Fill(Pick("lGoalPast"), new string[] { "P", "E" }, new string[] { P, E }));
            if (Eco.Stage(c, si).weak) L.Add(Dot(Gear(A().Get("lGear").At(0).Str(), c, si)));
        }
        else L.Add(Fill(A().Get("lDone").At(0).Str(), null, null));
        Skills.Init();
        for (int i = 0; i < Skills.All.Count(); i++)
        {
            SkDef d = Skills.All.Get(i);
            if (Skills.Unlocked(d) && !S.skUsed.Contains(d.id)) { L.Add(Fill(A().Get("lSkill").At(0).Str(), new string[] { "S", "Q" }, new string[] { d.name, Seq(d) })); break; }
        }
        int mn = 999999; JNode[] T = { Db.Weapons, Db.Armors, Db.Helms }, O = { Db.WeaponOrder, Db.ArmorOrder, Db.HelmOrder };
        for (int q = 0; q < 3; q++) for (int i = 0; i < O[q].Count(); i++)
        {
            string id = O[q].At(i).Str(); JNode it = T[q].Get(id);
            Lst<string> ow = q == 0 ? S.ownWeapon : q == 1 ? S.ownArmor : S.ownHelm;
            if (it.IsNull() || it.Bk("prize") || ow.Contains(id) || U.Len(Progress.GearLock(it)) > 0) continue;
            int pr = it.Ik("price"); if (pr > 0 && pr < mn) mn = pr;
        }
        if (S.coins >= mn) L.Add(Fill(A().Get("lCoins").At(0).Str(), new string[] { "C" }, new string[] { "" + S.coins }));
        int done = 0; for (int i = 0; i < Db.NCH; i++) if (S.prog[i] >= 4) done++;
        JNode sab = A().Get("lSab"); L.Add(Fill(sab.At(Mathf.Min(done, sab.Count() - 1)).Str(), null, null));
        L.Add(Fill(Pick("lProv"), null, null));
        if (L.Count() < 4) L.Add(Fill(A().Get("lGuide").At(0).Str(), null, null));
        return L;
    }
}
