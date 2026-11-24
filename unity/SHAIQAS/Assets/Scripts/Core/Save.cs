// SHAIQAS Core · Сақтау: веб-нұсқадағы SAVE құрылымы + қосымша кілттер (аты, викторина, медальдар...)
public interface IStore
{
    string Get(string key);
    void Set(string key, string value);
}

public class SaveData
{
    public int coins;
    public int[] prog, tour, surv, bossTime;
    public int[][] stars, tourW;
    public string hero = "sharua";
    public Lst<string> heroes = new Lst<string>();
    public Lst<string> ownWeapon = new Lst<string>(), ownArmor = new Lst<string>(), ownHelm = new Lst<string>();
    public string eqWeapon = "soyil", eqArmor = "chapan", eqHelm = "kalpak";
    public int tourTier, diff = 1, rating = 1000, bossRec, xp, lvlPaid = 1;
    // қосымша (вебте бөлек localStorage кілттері)
    public string name = "";
    public int[] quizBest;
    public Lst<string> dlgDone = new Lst<string>(), medals = new Lst<string>(), tutDone = new Lst<string>(), cards = new Lst<string>();
    public bool ru, kid, tutOff, sound = true;
    public float volMusic = 0.8f, volSfx = 1f;

    public static SaveData Fresh(int nch)
    {
        SaveData s = new SaveData();
        s.prog = new int[nch]; s.tour = new int[nch]; s.surv = new int[nch]; s.bossTime = new int[nch]; s.quizBest = new int[nch];
        s.stars = new int[nch][]; s.tourW = new int[nch][];
        for (int i = 0; i < nch; i++) { s.stars[i] = new int[4]; s.tourW[i] = new int[3]; }
        s.heroes.Add("sharua"); s.ownWeapon.Add("soyil"); s.ownArmor.Add("chapan"); s.ownHelm.Add("kalpak");
        return s;
    }

    // ---------------- JSON ----------------
    static JNode IntArr(int[] a) { JNode j = JNode.NewArr(); for (int i = 0; i < a.Length; i++) j.Add(JNode.Of(a[i])); return j; }
    static JNode IntArr2(int[][] a) { JNode j = JNode.NewArr(); for (int i = 0; i < a.Length; i++) j.Add(IntArr(a[i])); return j; }
    static JNode StrArr(Lst<string> a) { JNode j = JNode.NewArr(); for (int i = 0; i < a.Count(); i++) j.Add(JNode.Of(a.Get(i))); return j; }
    static int[] ReadInts(JNode j, int n) { int[] a = new int[n]; for (int i = 0; i < n; i++) a[i] = j.At(i).I(0); return a; }
    static int[][] ReadInts2(JNode j, int n, int m) { int[][] a = new int[n][]; for (int i = 0; i < n; i++) a[i] = ReadInts(j.At(i), m); return a; }
    static void ReadStrs(JNode j, Lst<string> to) { to.Clear(); for (int i = 0; i < j.Count(); i++) if (j.At(i).IsStr()) to.Add(j.At(i).Str()); }

    public string ToJson()
    {
        JNode o = JNode.NewObj();
        o.Set("v", JNode.Of(3)).Set("eco", JNode.Of(3)).Set("coins", JNode.Of(coins));
        o.Set("prog", IntArr(prog)).Set("stars", IntArr2(stars)).Set("tour", IntArr(tour)).Set("tourW", IntArr2(tourW));
        o.Set("surv", IntArr(surv)).Set("bossTime", IntArr(bossTime));
        o.Set("hero", JNode.Of(hero)).Set("heroes", StrArr(heroes));
        JNode own = JNode.NewObj(); own.Set("weapon", StrArr(ownWeapon)).Set("armor", StrArr(ownArmor)).Set("helm", StrArr(ownHelm));
        JNode eq = JNode.NewObj(); eq.Set("weapon", JNode.Of(eqWeapon)).Set("armor", JNode.Of(eqArmor)).Set("helm", JNode.Of(eqHelm));
        o.Set("owned", own).Set("eq", eq);
        o.Set("xp", JNode.Of(xp)).Set("lvlPaid", JNode.Of(lvlPaid)).Set("tourTier", JNode.Of(tourTier)).Set("diff", JNode.Of(diff)).Set("rating", JNode.Of(rating)).Set("bossRec", JNode.Of(bossRec));
        // қосымша
        o.Set("name", JNode.Of(name)).Set("quizBest", IntArr(quizBest)).Set("dlgDone", StrArr(dlgDone)).Set("medals", StrArr(medals));
        o.Set("tutDone", StrArr(tutDone)).Set("cards", StrArr(cards));
        o.Set("ru", JNode.OfBool(ru)).Set("kid", JNode.OfBool(kid)).Set("tutOff", JNode.OfBool(tutOff)).Set("sound", JNode.OfBool(sound));
        o.Set("volMusic", JNode.Of(volMusic)).Set("volSfx", JNode.Of(volSfx));
        return o.Dump();
    }

    public static SaveData FromJson(string text, int nch)
    {
        SaveData s = Fresh(nch);
        if (text == null || U.Len(text) == 0) return s;
        JNode d;
        try { d = JNode.Parse(text); } catch { return s; }
        if (!d.IsObj()) return s;
        s.coins = d.Ik("coins");
        s.prog = ReadInts(d.Get("prog"), nch); s.tour = ReadInts(d.Get("tour"), nch); s.surv = ReadInts(d.Get("surv"), nch);
        s.bossTime = ReadInts(d.Get("bossTime"), nch); s.quizBest = ReadInts(d.Get("quizBest"), nch);
        s.stars = ReadInts2(d.Get("stars"), nch, 4); s.tourW = ReadInts2(d.Get("tourW"), nch, 3);
        string h = d.S("hero"); if (!Db.Heroes.Get(h).IsNull()) s.hero = h;
        ReadStrs(d.Get("heroes"), s.heroes); if (!s.heroes.Contains("sharua")) s.heroes.Insert(0, "sharua");
        JNode own = d.Get("owned");
        ReadStrs(own.Get("weapon"), s.ownWeapon); if (!s.ownWeapon.Contains("soyil")) s.ownWeapon.Insert(0, "soyil");
        ReadStrs(own.Get("armor"), s.ownArmor); if (!s.ownArmor.Contains("chapan")) s.ownArmor.Insert(0, "chapan");
        ReadStrs(own.Get("helm"), s.ownHelm); if (!s.ownHelm.Contains("kalpak")) s.ownHelm.Insert(0, "kalpak");
        JNode eq = d.Get("eq");
        s.eqWeapon = Pick(eq.S("weapon"), s.ownWeapon, "soyil"); s.eqArmor = Pick(eq.S("armor"), s.ownArmor, "chapan"); s.eqHelm = Pick(eq.S("helm"), s.ownHelm, "kalpak");
        s.tourTier = Mathf.Clamp(d.Ik("tourTier"), 0, 2); s.diff = Mathf.Clamp(d.Get("diff").I(1), 0, 2);
        s.rating = d.Get("rating").I(1000); s.bossRec = d.Ik("bossRec"); s.xp = d.Ik("xp"); s.lvlPaid = d.Get("lvlPaid").I(1);
        s.name = d.S("name");
        ReadStrs(d.Get("dlgDone"), s.dlgDone); ReadStrs(d.Get("medals"), s.medals); ReadStrs(d.Get("tutDone"), s.tutDone); ReadStrs(d.Get("cards"), s.cards);
        s.ru = d.Bk("ru"); s.kid = d.Bk("kid"); s.tutOff = d.Bk("tutOff"); s.sound = !d.Has("sound") || d.Bk("sound");
        s.volMusic = d.Get("volMusic").F(0.8f); s.volSfx = d.Get("volSfx").F(1f);
        return s;
    }
    static string Pick(string v, Lst<string> owned, string def) { return owned.Contains(v) ? v : def; }
}

/// <summary>Ағымдағы сақтау + дискке жазу.</summary>
public static class Game
{
    public const string KEY = "shaiqas-unity-v1";
    public static SaveData S;
    static IStore store;

    public static void Load(IStore st)
    {
        store = st;
        string t = st == null ? null : st.Get(KEY);
        S = SaveData.FromJson(t, Db.NCH);
    }
    public static void Persist() { if (store != null) store.Set(KEY, S.ToJson()); }

    // ----- ойыншының аты (сюжеттегі «Бауыржан» орнына) -----
    public static string PlayerName() { return U.Len(S.name) > 0 ? S.name : Db.NAME_DEF; }
    public static string Fix(string s) { if (s == null) return ""; return U.Len(S.name) > 0 ? U.Replace(s, Db.NAME_DEF, S.name) : s; }
}
