// SHAIQAS Core · Ойын деректері (веб-нұсқадан шығарылған JSON). Unity-де Resources/Data, призракта — файлдар.
public interface ISource
{
    /// <summary>name.json мәтінін қайтарады (жоқ болса null).</summary>
    string Load(string name);
}

public static class Db
{
    public const float W = 1280f, H = 720f, GROUND = 650f, ARENA = 1800f, GRAV = 2300f, S = 1.38f;
    public const float TH = 62f, SH = 60f, TOR = 78f, UA = 46f, FA = 44f;
    public const int QZ_N = 8, BOSS_WINS = 3;
    public const string NAME_DEF = "Бауыржан";

    public static int NCH = 9;
    static ISource src;
    static Lst<string> names = new Lst<string>();
    static Lst<JNode> nodes = new Lst<JNode>();

    public static JNode Chapters, Heroes, HeroOrder, Weapons, WeaponOrder, Armors, ArmorOrder, Helms, HelmOrder;
    public static JNode Moves, Poses, Pace, QPool, QPoolRu, Hist, RuCards, Medals, RuMed, Bsc, BscBg, Dlg, DlgLook;
    public static JNode Epilogue, Lesson, LessonRu, DoneTxt, Tut, Taunts, Replies, Rage, ZhWin, Ranks, Years, EcoRew;

    public static void Init(ISource s)
    {
        src = s; names.Clear(); nodes.Clear();
        Chapters = J("CHAPTERS"); NCH = Chapters.Count();
        Heroes = J("HEROES"); HeroOrder = J("HERO_ORDER");
        Weapons = J("WEAPONS"); WeaponOrder = J("WEAPON_ORDER");
        Armors = J("ARMORS"); ArmorOrder = J("ARMOR_ORDER");
        Helms = J("HELMS"); HelmOrder = J("HELM_ORDER");
        Moves = J("MOVES"); Poses = J("P"); Pace = J("PACE");
        QPool = J("QPOOL"); QPoolRu = J("QPOOL_RU");
        Hist = J("HIST"); RuCards = J("RU_CARDS"); Medals = J("MEDALS"); RuMed = J("RU_MED");
        Bsc = J("BSC"); BscBg = J("BSC_BG"); Dlg = J("DLG"); DlgLook = J("DLG_LOOK");
        Epilogue = J("SAB_EPILOGUE"); Lesson = J("HIST_LESSON"); LessonRu = J("HIST_LESSON_RU"); DoneTxt = J("SAB_DONE_TXT");
        Tut = J("TUT_L"); Taunts = J("TAUNTS"); Replies = J("REPLIES"); Rage = J("RAGE"); ZhWin = J("ZH_WIN");
        Ranks = J("RANKS"); Years = J("FRAME_YEARS"); EcoRew = J("ECO_REW_BASE");
    }

    /// <summary>Атауы бойынша JSON (бір рет оқылады, кейін кэштен).</summary>
    public static JNode J(string name)
    {
        int i = names.IndexOf(name);
        if (i >= 0) return nodes.Get(i);
        string t = src == null ? null : src.Load(name);
        JNode n = t == null ? JNode.Null : JNode.Parse(t);
        if (t == null) U.Warn("Db: деректер табылмады — " + name);
        names.Add(name); nodes.Add(n);
        return n;
    }

    public static JNode Ch(int c) { return Chapters.At(c); }
    public static string ChName(int c) { return Ch(c).S("name"); }
    public static string Roman(int c) { return Ch(c).S("roman"); }
    public static JNode Hero(string id) { JNode h = Heroes.Get(id); return h.IsNull() ? Heroes.Get("sharua") : h; }
    public static JNode Weapon(string id) { JNode w = Weapons.Get(id); return w.IsNull() ? Weapons.Get("soyil") : w; }
    public static float PaceK(string k) { return Pace.Get(k).F(1f); }
}
