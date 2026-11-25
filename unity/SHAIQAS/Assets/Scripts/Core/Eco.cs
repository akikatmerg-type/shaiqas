// SHAIQAS Core · Экономика 3.0: жабдық күші, ұсынылған деңгей және «сен үшін» қиындық белгісі (веб: gearPower, ecoKit, stageDiffInfo, ecoTips).
using UnityEngine;

public class DiffInfo
{
    public int stars, k;
    public string label, col, colL, why;
    public bool weak;
    public float pay, d, rec, power;
}

public static class Eco
{
    public static readonly string[] NAMES = { "ӨТЕ ЖЕҢІЛ", "ЖЕҢІЛ", "ҚАЛЫПТЫ", "ҚИЫН", "ӨТЕ ҚИЫН" };
    public static readonly string[] COL = { "#2a8a2e", "#3f9a2a", "#b07a12", "#c8401c", "#b01414" };
    public static readonly string[] COL_L = { "#7ee06a", "#a8e070", "#ffd36b", "#ff9a5a", "#ff6a5a" };
    public static readonly string[] WHY = { "жабдығың өте мықты", "жабдығың мықты", "жабдығың лайық", "жабдығың әлсіз", "жабдығың тым әлсіз" };
    static readonly float[] PAY = { 1, 1, 1, 1.15f, 1.3f };
    static float[] kp;

    static float Num(JNode o, string k, float d) { JNode v = o.Get(k); return v.IsNum() ? v.F() : d; }
    public static float WpnScore(float dmg, float spd, float len, float mDmg, float mSpd)
    {
        return dmg * mDmg * Mathf.Pow(Mathf.Max(0.3f, spd * mSpd), 0.6f) * (1 + (len - 95) / 1000f) - 1;
    }
    public static float GearPower(string w, string a, string h, string hero)
    {
        JNode wp = Db.Weapons.Get(w), ar = Db.Armors.Get(a), hm = Db.Helms.Get(h);
        if (wp.IsNull()) wp = Db.Weapons.Get("soyil"); if (ar.IsNull()) ar = Db.Armors.Get("chapan"); if (hm.IsNull()) hm = Db.Helms.Get("kalpak");
        JNode m = Db.Hero(hero).Get("mods");
        float p = WpnScore(Num(wp, "dmg", 1), Num(wp, "spd", 1), Num(wp, "len", 95), Num(m, "dmg", 1), Num(m, "atkSpd", 1))
            + (Num(ar, "def", 0) + Num(hm, "def", 0) + Num(m, "def", 0)) * 1.6f
            + Num(m, "hp", 0) / 110f * 0.5f + (Num(m, "move", 1) - 1) * 0.3f + (Num(m, "rux", 1) - 1) * 0.2f + Num(m, "regen", 0) * 0.07f;
        return Mathf.Max(0, p);
    }
    public static float GearPower() { SaveData S = Game.S; return GearPower(S.eqWeapon, S.eqArmor, S.eqHelm, S.hero); }

    static string Best(string cat, JNode D, int lv)
    {
        JNode L = Db.J("ECO_LVL").Get(cat); string b = Db.J("ECO_START").S(cat); float bv = -1;
        for (int i = 0; i < L.Count(); i++)
        {
            string id = L.KeyAt(i); if (L.At(i).I() > lv) continue;
            JNode it = D.Get(id); if (it.IsNull()) continue;
            float v = U.Eq(cat, "weapon") ? WpnScore(Num(it, "dmg", 1), Num(it, "spd", 1), Num(it, "len", 95), 1, 1) : Num(it, "def", 0);
            if (v > bv) { bv = v; b = id; }
        }
        return b;
    }
    /// <summary>L-деңгейдегі «үлгі жабдықтың» күші.</summary>
    public static float Kit(float L)
    {
        if (kp == null)
        {
            kp = new float[21]; kp[0] = 0;
            for (int l = 1; l <= 20; l++)
                kp[l] = Mathf.Max(kp[l - 1], GearPower(Best("weapon", Db.Weapons, l), Best("armor", Db.Armors, l), Best("helm", Db.Helms, l), "sharua"));
        }
        float c = Pz.Clamp(L, 0, 20); int i = Mathf.FloorToInt(c); float f = c - i;
        return i >= 20 ? kp[20] : kp[i] + (kp[i + 1] - kp[i]) * f;
    }
    public static float GearLevel(float p)
    {
        Kit(0);
        if (p >= kp[20]) return 20 + (p - kp[20]) / 0.08f;
        for (int L = 0; L < 20; L++) if (p < kp[L + 1]) { float d = kp[L + 1] - kp[L]; return L + (d > 1e-6f ? (p - kp[L]) / d : 0); }
        return 20;
    }
    public static DiffInfo Stage(int ch, int si)
    {
        ch = Mathf.Clamp(ch, 0, Db.NCH - 1); si = Mathf.Clamp(si, 0, 3);
        DiffInfo I = new DiffInfo();
        I.rec = Db.J("ECO_REC").At(ch * 4 + si).F(); I.power = GearPower(); I.d = GearLevel(I.power) - I.rec;
        float d = I.d; int k = d >= 3 ? 0 : d >= 1.25f ? 1 : d > -1.5f ? 2 : d > -3.5f ? 3 : 4;
        I.k = k; I.stars = k + 1; I.label = NAMES[k]; I.weak = k >= 3; I.col = COL[k]; I.colL = COL_L[k]; I.why = WHY[k]; I.pay = PAY[k];
        return I;
    }
    /// <summary>Ең арзан жақсарту кеңесі (веб: ecoTips, бірінші жол).</summary>
    /// <summary>Веб: ecoTips сұрыптауы — алдымен тегін, сосын жеткілікті ең арзаны, болмаса бағасына шаққанда ең пайдалысы.</summary>
    static bool Before(int c1, bool ok1, float g1, int c2, bool ok2, float g2)
    {
        if ((c1 == 0) != (c2 == 0)) return c1 == 0;
        if (ok1 != ok2) return ok1;
        if (ok1) return c1 < c2;
        return g1 / (c1 + 50) > g2 / (c2 + 50);
    }
    public static string Tip(int ch, int si)
    {
        DiffInfo I = Stage(ch, si); SaveData S = Game.S; int lv = Progress.Level();
        float gp0 = GearPower(); string best = null; int bestCost = 0; bool bestOwn = false, bestOk = false; float bestGain = 0; string bestCat = null;
        string[] cats = { "weapon", "armor", "helm" };
        for (int c = 0; c < 3; c++)
        {
            string cat = cats[c]; JNode L = Db.J("ECO_LVL").Get(cat), D = c == 0 ? Db.Weapons : c == 1 ? Db.Armors : Db.Helms;
            Lst<string> own = c == 0 ? S.ownWeapon : c == 1 ? S.ownArmor : S.ownHelm;
            string eq = c == 0 ? S.eqWeapon : c == 1 ? S.eqArmor : S.eqHelm;
            for (int i = 0; i < L.Count(); i++)
            {
                string id = L.KeyAt(i); JNode it = D.Get(id); if (it.IsNull() || L.At(i).I() > lv || U.Eq(eq, id)) continue;
                float gp = GearPower(c == 0 ? id : S.eqWeapon, c == 1 ? id : S.eqArmor, c == 2 ? id : S.eqHelm, S.hero); if (gp <= gp0 + 1e-4f) continue;
                bool o = own.Contains(id); int cost = o ? 0 : it.Ik("price"); bool ok = GearLevel(gp) - I.rec > -1.5f; float gain = gp - gp0;
                bool better = best == null || Before(cost, ok, gain, bestCost, bestOk, bestGain);
                if (better) { best = it.S("name"); bestCost = cost; bestOwn = o; bestOk = ok; bestGain = gain; bestCat = cat; }
            }
        }
        if (best == null) return lv < 20 ? "Деңгейіңді көтер: жаңа заттар ашылады" : "Мықты батыр таңда";
        string slot = U.Eq(bestCat, "weapon") ? "Қаруыңды" : U.Eq(bestCat, "armor") ? "Сауытыңды" : "Бас киіміңді";
        return bestOwn ? slot + " ауыстыр: «" + best + "» (сенде бар)" : slot + " жақсарт: «" + best + "» — " + bestCost + " теңге";
    }
}
