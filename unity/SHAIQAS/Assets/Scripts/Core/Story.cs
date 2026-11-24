// SHAIQAS Core · Сюжет беттері: тарау тарихы, ұрыс алдындағы/кейінгі сахналар, тарихи кейіпкерлермен диалог, эпилог
public class Page
{
    public string id;          // сурет кілті (art: scenes/<pic>.jpg)
    public string pic;
    public string title, date, text;
    public string ruTitle, ruText;
    public bool zh;            // Бұқар жырау бұрышта
    public JNode dlg;          // диалог болса: { who, q:[kz,ru], o:[[kz,ru]..], ok, yes:[..], no:[..] }
    public string dlgKey;
    public int dlgAns = -1;
    public string npcName;

    public string Title(bool ru) { return Game.Fix(ru && ruTitle != null ? ruTitle : title); }
    public string Body(bool ru) { return Game.Fix(ru && ruText != null ? ruText : text); }
}

public static class Story
{
    static JNode scenesMeta;
    public static void SetSceneMeta(JNode m) { scenesMeta = m; }
    static string PicOf(string id)
    {
        if (scenesMeta == null) return id;
        JNode v = scenesMeta.Get(id);
        return v.IsStr() ? v.Str() : id;
    }

    static Page FromEp(string id, JNode ep)
    {
        Page p = new Page();
        p.id = id; p.pic = PicOf(id);
        p.title = ep.S("title"); p.date = ep.S("date"); p.text = ep.S("text");
        JNode ru = ep.Get("ru");
        if (ru.IsArr()) { p.ruTitle = ru.At(0).Str(); p.ruText = ru.At(1).Str(); }
        p.zh = ep.Bk("zh");
        return p;
    }

    /// <summary>Тараудың толық тарихы (картадағы «ТАРИХЫН ОҚУ»).</summary>
    public static Lst<Page> Episodes(int ch)
    {
        Lst<Page> L = new Lst<Page>();
        JNode E = Db.Ch(ch).Get("episodes");
        for (int i = 0; i < E.Count(); i++) L.Add(FromEp("ch" + ch + "_ep" + i, E.At(i)));
        return L;
    }

    /// <summary>Тарау тарихын 4 ұрысқа бөлу (веб: bscPart).</summary>
    public static Lst<Page> Part(int ch, int si)
    {
        Lst<Page> all = Episodes(ch), L = new Lst<Page>();
        int n = all.Count(), a = (si * n) / 4, b = ((si + 1) * n) / 4;
        for (int i = a; i < b; i++) L.Add(all.Get(i));
        return L;
    }

    public static Page Bsc(int ch, int si, bool pre)
    {
        JNode d = Db.Bsc.At(ch).At(si);
        if (d.IsNull()) return null;
        JNode C = Db.Ch(ch);
        string node = C.Get("nodes").At(si).S("n");
        if (U.Len(node) == 0) node = C.S("name");
        JNode foe = C.Get("enemies").At(si);
        Page p = new Page();
        p.id = "ch" + ch + "_bsc" + si + (pre ? "_pre" : "_post"); p.pic = PicOf(p.id); p.zh = true;
        p.date = (si + 1) + "-ұрыс · " + node;
        if (pre)
        {
            p.title = "Ұрыс алдында: " + foe.S("name"); p.text = d.Get("pre").At(0).Str();
            p.ruTitle = "Перед битвой: " + (si + 1) + "-й бой"; p.ruText = d.Get("pre").At(1).Str();
        }
        else
        {
            if (d.Get("post").IsNull()) return null;
            p.title = "Жеңістен кейін"; p.text = d.Get("post").At(0).Str();
            p.ruTitle = "После победы"; p.ruText = d.Get("post").At(1).Str();
        }
        return p;
    }

    public static Page Dialog(int ch, int k)
    {
        JNode d = Db.Dlg.At(ch).At(k);
        if (d.IsNull()) return null;
        JNode L = Db.DlgLook.Get(d.S("who"));
        string nm = L.Get("foe").IsArr() ? Db.Ch(L.Get("foe").At(0).I()).Get("enemies").At(L.Get("foe").At(1).I()).S("name") : L.S("name");
        Page p = new Page();
        p.id = "ch" + ch + "_dlg" + k; p.pic = PicOf(p.id); p.zh = true;
        p.dlg = d; p.dlgKey = ch + ":" + k; p.npcName = nm;
        p.date = nm; p.title = "Сұрақ: жауабын таңда"; p.text = d.Get("q").At(0).Str();
        p.ruTitle = nm; p.ruText = d.Get("q").At(1).Str();
        return p;
    }

    /// <summary>Жауап: дұрыс болса бірінші ретте +15 теңге. Қайтарады: берілген теңге.</summary>
    public static int Answer(Page p, int k)
    {
        if (p.dlg == null || p.dlgAns >= 0) return 0;
        p.dlgAns = k;
        bool ok = k == p.dlg.Ik("ok");
        JNode r = p.dlg.Get(ok ? "yes" : "no");
        p.text = r.At(0).Str(); p.ruText = r.At(1).Str();
        p.title = ok ? "Дұрыс жауап!" : "Түсіндіру"; p.ruTitle = ok ? "Правильно!" : "Объяснение";
        if (ok && !Game.S.dlgDone.Contains(p.dlgKey)) { Game.S.dlgDone.Add(p.dlgKey); Game.S.coins += 15; Game.Persist(); return 15; }
        return 0;
    }
    public static bool AnswerOk(Page p) { return p.dlg != null && p.dlgAns == p.dlg.Ik("ok"); }

    /// <summary>Ұрыс алдындағы беттер тізімі (веб: bscList + диалог).</summary>
    public static Lst<Page> BeforeFight(int ch, int si)
    {
        Lst<Page> L = new Lst<Page>();
        if (si > 0) { Page post = Bsc(ch, si - 1, false); if (post != null) L.Add(post); }
        Lst<Page> part = Part(ch, si);
        for (int i = 0; i < part.Count(); i++) L.Add(part.Get(i));
        if (si == 1 || si == 2) { Page dp = Dialog(ch, si - 1); if (dp != null) L.Add(dp); }
        Page pre = Bsc(ch, si, true); if (pre != null) L.Add(pre);
        return L;
    }

    public static Lst<Page> EpilogueList()
    {
        Lst<Page> L = new Lst<Page>();
        for (int i = 0; i < Db.Epilogue.Count(); i++) L.Add(FromEp("epi" + i, Db.Epilogue.At(i)));
        return L;
    }

    /// <summary>Тарау аяқталғандағы мәтін және «Есте сақта» сабағы.</summary>
    public static string DoneText(int ch, bool ru) { return Game.Fix(Db.DoneTxt.At(ch).Str()); }
    public static int LessonCount(int ch) { return Db.Lesson.At(ch).Count(); }
    public static string LessonHead(int ch, int i) { return Db.Lesson.At(ch).At(i).At(0).Str(); }
    public static string LessonText(int ch, int i, bool ru) { return ru ? Db.LessonRu.At(ch).At(i).Str() : Db.Lesson.At(ch).At(i).At(1).Str(); }
}
