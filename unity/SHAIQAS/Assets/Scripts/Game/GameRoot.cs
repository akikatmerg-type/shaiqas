// SHAIQAS · Басты объект: ойын өзі іске қосылады (сахнаны қолмен баптау қажет емес),
// экрандар арасындағы өту және жорық тізбегі: сюжет → ұрыс → тарау соңы → викторина → келесі тарау.
using UnityEngine;

public static class Boot
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Start()
    {
        if (Object.FindAnyObjectByType<GameRoot>() != null) return;
        GameObject go = new GameObject("SHAIQAS");
        go.AddComponent<GameRoot>();
        Object.DontDestroyOnLoad(go);
    }
}

public partial class GameRoot : MonoBehaviour
{
    public const string NAME = "name", LOBBY = "lobby", MAP = "map", STORY = "story", FIGHT = "fight", DONE = "done", QUIZ = "quiz",
        ALBUM = "album", MEDALS = "medals", SETTINGS = "settings", SHOP = "shop", HEROES = "heroes";

    public static GameRoot I;
    public string screen = LOBBY;
    public float pt;
    public Match match = new Match();
    public FightView fv;
    public Camera cam, bars;

    // экран күйлері
    public int mapCh, mapStage;
    public Lst<Page> pages; public int page; public string storyMode = "fight"; public int storyCh, storySi;
    public int doneCh;
    public Quiz quiz; public bool quizFromDone;
    public int albumSel = -1;
    public string toast = ""; public float toastT;
    public Lst<string> medalPop = new Lst<string>(); public float medalT;
    string nameDraft = "";

    void Awake()
    {
        I = this;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Ui.Init();
        Db.Init(new ResSource()); Pz.Init();
        Game.Load(new PrefsStore());
        Story.SetSceneMeta(Db.J("meta_scenes"));
        Looks.Init(Db.J("meta_parts"));
        // камералар: артқы қара жолақтар + 16:9 ойын камерасы
        bars = new GameObject("Bars").AddComponent<Camera>();
        bars.clearFlags = CameraClearFlags.SolidColor; bars.backgroundColor = Color.black; bars.cullingMask = 0; bars.depth = -10; bars.orthographic = true;
        bars.transform.SetParent(transform);
        Camera main = Camera.main;
        if (main == null) { main = new GameObject("Main Camera").AddComponent<Camera>(); main.tag = "MainCamera"; }
        cam = main; cam.orthographic = true; cam.orthographicSize = 360; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        cam.transform.position = new Vector3(640, -360, -100); cam.nearClipPlane = 0.1f; cam.farClipPlane = 1000; cam.depth = 0;
        fv = gameObject.AddComponent<FightView>();
        gameObject.AddComponent<Snd>();
        mapCh = Progress.LastOpen();
        Go(U.Len(Game.S.name) == 0 ? NAME : LOBBY);
    }

    System.Collections.IEnumerator Shot()
    {
        yield return new WaitForEndOfFrame();
        Texture2D t = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        t.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); t.Apply();
        string dir = Application.dataPath + "/../Shots";
        System.IO.Directory.CreateDirectory(dir);
        System.IO.File.WriteAllBytes(dir + "/shot_" + System.DateTime.Now.ToString("HHmmss") + "_d" + StageLive.dbg + ".png", t.EncodeToPNG());
        Destroy(t);
    }

    bool benchOn;
    System.Collections.IEnumerator Bench()
    {
        benchOn = true;
        Match mm = match; Fighter P = mm.player, E = mm.enemy; SkDef d = Skills.By(mm.arenaSel);
        if (d == null) { benchOn = false; yield break; }
        // сахна: жау алдында 260 бірлікте тыныш тұрады, ойыншы әдісті бірден бастайды
        E.active = false; E.state = Fighter.IDLE; E.hp = E.hpLag = E.maxHp; E.x = Mathf.Clamp(P.x + P.facing * 260, 120, Db.ARENA - 120); E.facing = -P.facing; E.vx = 0; E.push = 0;
        SkState T = Skills.S(P); for (int i = 0; i < T.cd.Length; i++) T.cd[i] = 0;
        P.rux = 100; P.sk = null; P.state = Fighter.IDLE; P.vx = 0; P.push = 0; P.y = Db.GROUND; P.vy = 0;
        if (d.air) { P.y = Db.GROUND - 200; P.vy = -100; P.state = Fighter.JUMP; }
        yield return null;
        Skills.Start(P, d, false);
        string dir = Application.dataPath + "/../Shots/burst";
        System.IO.Directory.CreateDirectory(dir);
        for (int f = 0; f < 24; f++)
        {
            yield return new WaitForEndOfFrame();
            Texture2D t = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); t.Apply();
            System.IO.File.WriteAllBytes(dir + "/" + d.id + "_" + (f < 10 ? "0" : "") + f + ".png", t.EncodeToPNG());
            Destroy(t);
            float until = Time.unscaledTime + 0.06f;
            while (Time.unscaledTime < until) yield return null;
        }
        E.active = true; benchOn = false;
        Toast("стенд жазылды: " + d.name);
    }

    void Update()
    {
        Inp.Poll();
        // F12 — скриншот (жоба папкасындағы Shots/ ішіне): веб-нұсқамен пиксель бойынша салыстыру үшін
        if (Application.isEditor && Inp.Tap(Inp.F12)) StartCoroutine(Shot());
        if (Application.isEditor && Inp.Tap(Inp.F10)) { Skills.TestUnlock = !Skills.TestUnlock; Toast(Skills.TestUnlock ? "тест: барлық әдіс ашық, рух толы" : "тест: әдістер қалыпты"); }
        // F9 — әдіс стенді (еркін алаңда): таңдалған әдісті қолданып, 24 кадрды Shots/burst/ ішіне жазады; F8 — келесі әдіс
        if (Application.isEditor && screen == FIGHT && match != null && U.Eq(match.mode, "arena"))
        {
            if (Inp.Tap(Inp.F8)) { Skills.Init(); int ci = 0; for (int i = 0; i < Skills.All.Count(); i++) if (Skills.All.Get(i).id == match.arenaSel) ci = i; match.arenaSel = Skills.All.Get((ci + 1) % Skills.All.Count()).id; Toast("стенд: " + Skills.By(match.arenaSel).name); }
            if (Inp.Tap(Inp.F9) && !benchOn) StartCoroutine(Bench());
        }
        if (Application.isEditor && Inp.Tap(Inp.F11)) { StageLive.dbg = (StageLive.dbg + 1) % 15; Toast("debug: " + StageLive.dbg); }
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        LevelWatch(dt);
        pt += dt; toastT = Mathf.Max(0, toastT - dt);
        if (medalPop.Count() > 0) { medalT += dt; if (medalT > 2.6f) { medalPop.RemoveAt(0); medalT = 0; } }
        FitCamera();
        if (screen == FIGHT) fv.Tick(dt);
        PickMusic();
    }

    /// <summary>Экранға сай музыка (веб: skMusic).</summary>
    void PickMusic()
    {
        string tr = "menu"; int ch = mapCh;
        if (screen == MAP || screen == STORY || screen == DONE || screen == QUIZ) { tr = "map"; ch = screen == STORY ? storyCh : screen == QUIZ && quiz != null ? quiz.ch : mapCh; }
        else if (screen == FIGHT && match.player != null)
        {
            ch = match.chapter;
            if (match.phase == Match.MATCHEND) tr = match.won ? "victory" : "defeat";
            else if (match.enemy != null && match.enemy.boss) tr = "boss";
            else tr = "battle";
        }
        Snd.Music(tr, ch);
    }

    void FitCamera()
    {
        float sw = Screen.width, sh = Screen.height, kk = Mathf.Min(sw / 1280f, sh / 720f);
        float w = 1280f * kk / sw, h = 720f * kk / sh;
        cam.rect = new Rect((1 - w) / 2, (1 - h) / 2, w, h);
    }

    public void Go(string s)
    {
        if (screen == STORY && s != STORY) Ui.Forget("Art/scenes/");
        if (screen == FIGHT && s != FIGHT) fv.Teardown();
        if (s == ALBUM && screen != ALBUM) AlbumEnter();
        screen = s; pt = 0;
        Tut.Enter(s);
    }
    public void Toast(string t, float time = 2.2f) { toast = t; toastT = time; }
    public void PopMedals(Lst<string> fresh) { for (int i = 0; i < fresh.Count(); i++) medalPop.Add(fresh.Get(i)); fresh.Clear(); }

    // ================= жорық тізбегі =================
    /// <summary>Картаға оралу (веб: campMapEnter): жорықтан — кезеңдер көрінісі, басқадан — режимдер картасы.</summary>
    public void OpenMap(int ch)
    {
        mapCh = Mathf.Clamp(ch, 0, Db.NCH - 1); mapStage = Mathf.Clamp(Game.S.prog[mapCh], 0, 3); mapPop = null;
        bool camp = (screen == FIGHT || screen == STORY) && match.mode == "campaign";
        mapView = camp ? "stages" : "map";
        int si = System.Array.IndexOf(SITE_ID, match.mode == "campaign" ? "story" : match.mode);
        mapSite = screen != LOBBY && si >= 0 ? si : 0;
        Go(MAP);
    }

    public void StartStory(Lst<Page> list, string mode, int ch, int si)
    {
        pages = list; page = 0; storyMode = mode; storyCh = ch; storySi = si;
        if (pages == null || pages.Count() == 0) { StoryFinished(); return; }
        Go(STORY);
    }
    /// <summary>Беттер біткенде: ұрысқа, картаға немесе лоббиға.</summary>
    public void StoryFinished()
    {
        if (storyMode == "fight") StartFight(storyCh, storySi);
        else if (storyMode == "epilogue") Go(LOBBY);
        else OpenMap(storyCh);
    }
    public void Campaign(int ch, int si) { StartStory(Story.BeforeFight(ch, si), "fight", ch, si); }

    public void StartFight(int ch, int si)
    {
        match.StartCampaign(ch, si);
        fv.Setup(match);
        Go(FIGHT);
    }
    public void StartTrain(int ch)
    {
        match.chapter = ch; match.StartTrain(ch);
        fv.Setup(match);
        Go(FIGHT);
    }
    public void QuickFight()
    {
        int last = Progress.LastOpen(), ch = (int)(U.Rand() * (last + 1)); if (ch > last) ch = last;
        int idx = (int)(U.Rand() * 3);
        match.StartQuick(ch, idx); fv.Setup(match); Go(FIGHT);
    }
    public void AfterFight(string next)
    {
        PopMedals(match.medalsNew);
        string r = match.After(next);
        if (r == "fight") { fv.Setup(match); pt = 0; return; }
        if (r == "bracket") { Go(BRACKET); return; }
        if (r == "duelsel") { Go(DUELSEL); return; }
        if (match.mode == "quick") { Go(LOBBY); return; }
        if (r == "nextStage") { Campaign(match.chapter, match.stageIdx + 1); return; }
        if (r == "chapterDone") { doneCh = match.chapter; Go(DONE); return; }
        OpenMap(match.chapter);
    }

    public void StartQuiz(int ch, bool fromDone) { quiz = Quiz.Start(ch); quizFromDone = fromDone; Go(QUIZ); }
    public void QuizExit()
    {
        PopMedals(CheckMedals());
        int ch = quiz.ch;
        if (quizFromDone && quiz.Passed())
        {
            if (ch == Db.NCH - 1) { StartStory(Story.EpilogueList(), "epilogue", ch, 0); return; }
            OpenMap(ch + 1); Toast((Db.Roman(ch + 1)) + " ТАРАУ АШЫЛДЫ!", 2.6f); return;
        }
        OpenMap(ch);
    }
    Lst<string> CheckMedals() { Lst<string> f = new Lst<string>(); Medals.Check(f); if (f.Count() > 0) Game.Persist(); return f; }

    // ================= сурет =================
    void OnGUI()
    {
        Ui.Begin();
        GUI.depth = 0;
        GUI.enabled = true;
        Ui.blockInput = Tut.Active();   // астындағы экран толық көрінеді, бірақ басылмайды
        OverlaysPre();
        switch (screen)
        {
            case NAME: DrawName(); break;
            case LOBBY: DrawLobby(); break;
            case MAP: DrawMap(); break;
            case STORY: DrawStory(); break;
            case FIGHT: if (guideOverlay) DrawGuide(); else fv.DrawHud(); break;
            case GUIDE: DrawGuide(); break;
            case BRACKET: DrawBracket(); break;
            case VS: DrawVs(); break;
            case DUELSEL: DrawDuelSel(); break;
            case DONE: DrawDone(); break;
            case QUIZ: DrawQuiz(); break;
            case ALBUM: DrawAlbum(); break;
            case MEDALS: DrawMedals(); break;
            case SETTINGS: DrawSettings(); break;
            case SHOP: DrawShop(); break;
            case HEROES: DrawHeroes(); break;
        }
        Ui.blockInput = false;
        OverlaysPost();
        Tut.Draw(screen);
        if (toastT > 0)
        {
            float a = Mathf.Clamp01(toastT * 2);
            Ui.Fill(340, 600, 600, 54, new Color(0.08f, 0.05f, 0.14f, 0.9f * a));
            Ui.TextS(toast, 340, 600, 600, 54, 22, new Color(1, 0.86f, 0.5f, a), TextAnchor.MiddleCenter);
        }
        if (medalPop.Count() > 0) DrawMedalPop(medalPop.Get(0));
        DrawLevelBanner();
    }

    void DrawMedalPop(string id)
    {
        JNode m = MedalById(id); if (m.IsNull()) return;
        float y = 90 - Mathf.Max(0, 0.3f - medalT) * 300;
        Ui.Panel(400, y, 480, 96, new Color(0.1f, 0.07f, 0.18f, 0.95f), Ui.GOLD);
        Ui.Img("Art/icons/medal_" + id, 412, y + 8, 80, 80);
        Ui.TextS("ЖАҢА МАРАПАТ!", 500, y + 10, 370, 30, 18, Ui.GOLD);
        Ui.TextS(m.S("name"), 500, y + 42, 370, 40, 24, Ui.CREAM);
    }
    public static JNode MedalById(string id)
    {
        for (int i = 0; i < Db.Medals.Count(); i++) if (U.Eq(Db.Medals.At(i).S("id"), id)) return Db.Medals.At(i);
        return JNode.Null;
    }
    public static bool Ru() { return Game.S.ru; }

    // ================= аты =================
    void DrawName()
    {
        Ui.Img("Art/lobby/backdrop", 0, 0, 1280, 720);
        Ui.Fill(0, 0, 1280, 720, new Color(0.04f, 0.03f, 0.1f, 0.55f));
        Ui.Panel(290, 150, 700, 420, new Color(0.96f, 0.88f, 0.7f, 0.97f), Ui.GOLD);
        Ui.Img("Art/lobby/zhyrau", 300, 190, 180, 252);
        Ui.Text("Сәлем, балам! Мен — Бұқар жырау.\nАтың кім? Жырымда сен батыр боласың!", 490, 190, 480, 110, 24, Ui.INK, TextAnchor.UpperLeft, true);
        if (Ru()) Ui.Text("Здравствуй! Я — Бухар-жырау. Как тебя зовут?", 490, 300, 480, 40, 18, new Color(0.3f, 0.2f, 0.1f));
        Ui.Fill(490, 350, 470, 60, Ui.INK);
        GUIStyle tf = new GUIStyle(GUI.skin.textField); tf.font = Ui.fontBold; tf.fontSize = Mathf.RoundToInt(30 * Ui.k); tf.alignment = TextAnchor.MiddleCenter;
        tf.normal.textColor = Ui.CREAM; tf.focused.textColor = Ui.CREAM;
        nameDraft = GUI.TextField(Ui.R(494, 354, 462, 52), nameDraft, 16, tf);
        string nm = U.Trim(nameDraft);
        if (Ui.Btn("ДАЙЫН ▶", 600, 450, 260, 64, true, U.Len(nm) >= 2, 26))
        {
            Game.S.name = nm; Game.Persist(); Go(LOBBY);
        }
    }
    public void AskName() { nameDraft = Game.S.name; Go(NAME); }
}
