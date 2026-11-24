// SHAIQAS Core · Тарих викторинасы: әр тараудан 8 сұрақ, 3 жауап нұсқасы; 6-дан (балалар режимінде 5-тен) — келесі тарау ашылады
using UnityEngine;
public class Quiz
{
    public int ch, i, picked = -1, score, reward;
    public bool done;
    public int[] opts = new int[3];        // көрсетілетін ретпен: 1 — дұрыс жауап, 2/3 — қате
    public Lst<JNode> qs = new Lst<JNode>(), qsRu = new Lst<JNode>();

    public static Quiz Start(int ch)
    {
        Quiz q = new Quiz(); q.ch = ch;
        JNode pool = Db.QPool.At(ch), poolRu = Db.QPoolRu.At(ch);
        int n = pool.Count();
        int[] idx = new int[n];
        for (int k = 0; k < n; k++) idx[k] = k;
        for (int k = n - 1; k > 0; k--) { int j = (int)(U.Rand() * (k + 1)); int t = idx[k]; idx[k] = idx[j]; idx[j] = t; }
        for (int k = 0; k < Mathf.Min(Db.QZ_N, n); k++) { q.qs.Add(pool.At(idx[k])); q.qsRu.Add(poolRu.At(idx[k])); }
        q.Shuffle();
        return q;
    }
    public int Total() { return qs.Count(); }
    public void Shuffle()
    {
        opts[0] = 1; opts[1] = 2; opts[2] = 3;
        for (int k = 2; k > 0; k--) { int j = (int)(U.Rand() * (k + 1)); int t = opts[k]; opts[k] = opts[j]; opts[j] = t; }
        picked = -1;
    }
    public string Question(bool ru) { JNode q = ru && !qsRu.Get(i).IsNull() ? qsRu.Get(i) : qs.Get(i); return Game.Fix(q.At(0).Str()); }
    public string Option(int k, bool ru) { JNode q = ru && !qsRu.Get(i).IsNull() ? qsRu.Get(i) : qs.Get(i); return Game.Fix(q.At(opts[k]).Str()); }
    public bool IsRight(int k) { return opts[k] == 1; }
    public int RightIndex() { for (int k = 0; k < 3; k++) if (opts[k] == 1) return k; return 0; }

    public bool Pick(int k)
    {
        if (picked >= 0 || done) return false;
        picked = k;
        if (opts[k] == 1) { score++; return true; }
        return false;
    }
    /// <summary>Келесі сұрақ; соңғысы болса — нәтижені сақтайды (рекорд үшін әр жаңа ұпайға 25 теңге).</summary>
    public void Next()
    {
        if (picked < 0) return;
        if (i < qs.Count() - 1) { i++; Shuffle(); return; }
        done = true;
        int old = Game.S.quizBest[ch];
        if (score > old) { reward = (score - old) * 25; Game.S.quizBest[ch] = score; Game.S.coins += reward; Game.Persist(); }
    }
    public bool Passed() { return score >= Progress.QzPassMin(); }
    public string Verdict()
    {
        int n = Total();
        if (score == n) return "ТАМАША!";
        if (score >= n - 2) return "ЖАРАЙСЫҢ!";
        if (score >= n / 2) return "ЖАҚСЫ!";
        return "ТАҒЫ БАЙҚАП КӨР!";
    }
}
