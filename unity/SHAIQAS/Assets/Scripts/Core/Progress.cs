// SHAIQAS Core · Тараулардың ашылуы: алдыңғы тараудың 4 ұрысы + викторинадан өту (веб-нұсқадағыдай)
using UnityEngine;
public static class Progress
{
    public static bool TestUnlockAll = false;

    public static int QzPassMin() { return Game.S.kid ? 5 : 6; }
    public static bool QuizPassed(int ch) { return ch >= 0 && ch < Db.NCH && Game.S.quizBest[ch] >= QzPassMin(); }
    public static bool ChapterDone(int ch) { return Game.S.prog[ch] >= 4; }

    public static bool Unlocked(int c)
    {
        if (TestUnlockAll || c == 0) return true;
        if (c < 0 || c >= Db.NCH) return false;
        return Game.S.prog[c - 1] >= 4 && (QuizPassed(c - 1) || Game.S.prog[c] > 0);
    }
    /// <summary>Алдыңғы тарау бітті, бірақ викторинадан әлі өтпеген — тарау «викторина құлпында».</summary>
    public static bool QuizGate(int c) { return !TestUnlockAll && c > 0 && Game.S.prog[c - 1] >= 4 && !Unlocked(c); }
    public static string LockMsg(int c)
    {
        if (QuizGate(c)) return "Алдымен " + Db.Roman(c - 1) + " тараудың викторинасынан өт";
        return "Алдыңғы тарауды аяқта";
    }
    public static int RankIndex()
    {
        int n = 0;
        for (int i = 0; i < Db.NCH; i++) if (Game.S.prog[i] >= 4) n++;
        return n;
    }
    public static string RankName() { return Db.Ranks.At(Mathf.Min(RankIndex(), Db.Ranks.Count() - 1)).Str(); }
    /// <summary>Картада ашылатын соңғы тарау.</summary>
    public static int LastOpen()
    {
        int last = 0;
        for (int i = 0; i < Db.NCH; i++) if (Unlocked(i)) last = i;
        return last;
    }
    public static int StarCount(int bits) { return (bits & 1) + ((bits >> 1) & 1) + ((bits >> 2) & 1); }
    public static int ChapterStars(int ch) { int n = 0; for (int i = 0; i < 4; i++) n += StarCount(Game.S.stars[ch][i]); return n; }

    // ----- деңгей мен тәжірибе (веб: skLevelOf, lvPay) -----
    public const int MAXL = 20;
    public static int Need(int L) { return Mathf.RoundToInt(30 + 15 * (L - 1) + (L - 1) * (L - 1)); }
    public static int LevelOf(int xp) { int L = 1, acc = 0; while (L < MAXL && xp >= acc + Need(L)) { acc += Need(L); L++; } return L; }
    public static int Level() { return TestUnlockAll ? MAXL : LevelOf(Game.S.xp); }
    public static int XpInto() { int L = 1, acc = 0, xp = Game.S.xp; while (L < MAXL && xp >= acc + Need(L)) { acc += Need(L); L++; } return xp - acc; }
    public static int LvReward(int L) { return L <= 1 ? 0 : Mathf.RoundToInt((30f * L + 8f * L * L) / 10f) * 10; }
    /// <summary>Тәжірибе қосу. Қайтарады: жаңа деңгей үшін берілген теңге (0 — деңгей өзгермеді).</summary>
    public static int GainXp(int n)
    {
        if (n <= 0) return 0;
        Game.S.xp += n;
        int L = Level(), sum = 0;
        for (int k = Game.S.lvlPaid + 1; k <= L; k++) sum += LvReward(k);
        if (L > Game.S.lvlPaid) Game.S.lvlPaid = L;
        Game.S.coins += sum;
        return sum;
    }
    /// <summary>Затты сатып алу шарты орындалмаса — қазақша кеңес, әйтпесе null (веб: gearLock).</summary>
    public static string GearLock(JNode it)
    {
        JNode r = it.Get("req");
        if (r.IsNull() || TestUnlockAll) return null;
        if (r.Has("ch") && Game.S.prog[r.Ik("ch")] < 4) return "«" + Db.ChName(r.Ik("ch")) + "» тарауын аяқта";
        if (r.Has("lvl") && Level() < r.Ik("lvl")) return r.Ik("lvl") + "-деңгейге жет";
        if (r.Has("tour")) { bool any = false; for (int i = 0; i < Db.NCH; i++) if (Game.S.tourW[i][2] > 0) any = true; if (!any) return "Кез келген АЛТЫН турнирді жең"; }
        return null;
    }
}
