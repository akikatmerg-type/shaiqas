// SHAIQAS · Мәзір экрандары (лобби, карта, сюжет, тарау соңы, викторина, альбом, марапаттар, баптаулар, дүкен, батырлар).
// Орналасу вебтегі 1280×720 экранға сай.
using UnityEngine;

public partial class GameRoot
{
    // ================= ЛОББИ (веб: drawLobby + buildUI орындары) =================
    void DrawLobby()
    {
        Ui.Img("Art/lobby/backdrop", 0, 0, 1280, 720);
        float br = Mathf.Sin(Time.time * 1.6f) * 1.5f;
        Ui.Img("Art/lobby/zhyrau", 980, 250 + br, 300, 420);
        Ui.Img("Art/lobby/front_" + Game.S.hero, 550, 110 + Mathf.Sin(Time.time * 2.4f) * 1.2f, 420, 560);
        // сол жақ көлеңке (веб: LOBSH)
        for (int i = 0; i < 132; i++) Ui.Fill(i * 4, 0, 4.5f, 720, new Color(0.047f, 0.031f, 0.11f, 0.82f * Mathf.Clamp01(1 - i / 131f)));
        Ui.Fill(0, 600, 1280, 120, new Color(0.04f, 0.024f, 0.08f, 0.35f));
        Ui.TopBar(null, true);
        // логотип
        float lk = Mathf.Clamp01(pt / 0.7f), le = 1 - (1 - lk) * (1 - lk) * (1 - lk);
        Ui.Img("Art/ui/logo", 48 - 350 - (1 - le) * 60, 108 - 120, 700, 320, le);
        // батырмалар (вебтегі орындар)
        if (Ui.DzBtn("МАРАПАТТАР", 392, 16, 172, 46, false, true, null, true, true)) { medalsFrom = LOBBY; Go(MEDALS); }
        if (Ui.DzBtn("ТАРИХ АЛЬБОМЫ", 574, 16, 222, 46, false, true, null, true, true)) { albumSel = -1; Go(ALBUM); }
        if (Ui.DzBtn("⚙ БАПТАУЛАР", 806, 16, 222, 46, false, true, null, true, true)) Go(SETTINGS);
        if (Ui.DzBtn("ЖОРЫҚ", 48, 284, 344, 68, true, true, "map")) { mapSite = 0; OpenMapHub(Progress.LastOpen()); }
        if (Ui.DzBtn("БАТЫРЛАР", 48, 366, 330, 62, false, true, "batyr")) Go(HEROES);
        if (Ui.DzBtn("ҚАРУ-ЖАРАҚ", 48, 442, 330, 62, false, true, "arms")) { shopTab = "weapon"; Go(SHOP); }
        if (Ui.DzBtn("ЕРКІН ҰРЫС", 48, 518, 330, 62, false, true, "fight")) QuickFight();
        if (Ui.DzBtn("ЕРКІН АЛАҢ", 48, 594, 161, 50, false, true, null, true)) StartArena(null);
        if (Ui.DzBtn("ӘДІСТЕР КІТАБЫ", 217, 594, 161, 50, false, true, null, true)) OpenGuide(false);
        // батыр тақтайшасы
        JNode hr = Db.Hero(Game.S.hero);
        Ui.Nine("Art/ui/ribbon_blue", 760 - 165 - 50, 670 - 40, 430, 80, 110, 0, 110, 0);
        Ui.TextO(Game.PlayerName(), 760 - 140, 670 - 22, 280, 44, 24, new Color32(255, 214, 120, 255));
        Ui.TextO("Қабілеті: " + hr.S("ability"), 560, 696, 400, 24, 14, new Color32(255, 211, 107, 255), TextAnchor.MiddleCenter, true, 1.5f);
        // жырау сөзі
        float zk = Mathf.Clamp01((pt - 0.6f) / 0.45f);
        if (zk > 0)
        {
            string line = Db.J("ZH_LINES").At(Mathf.Min(Progress.RankIndex(), Db.J("ZH_LINES").Count() - 1)).Str();
            float bw = 380, bx = 1130 - bw + 60, by = 82;
            Ui.DzPanel(bx, by, bw, 70, "parch");
            Ui.Text("❦ Бұқар жырау", bx + 14, by + 6, bw - 20, 20, 14, new Color32(125, 23, 25, 255), TextAnchor.MiddleLeft, true);
            Ui.Text(line, bx + 14, by + 26, bw - 24, 40, 16, Ui.INK, TextAnchor.UpperLeft, true);
        }
    }

    // ================= КАРТА =================
    // ================= СЮЖЕТ (веб: drawPrologue) =================
    void DrawStory()
    {
        Page p = pages.Get(page);
        bool ru = Ru();
        Ui.Fill(0, 0, 1280, 720, Color.black);
        float k = Mathf.Min(1, pt / 10), zoom = 1 + 0.05f * k;
        float sw = 1280 * zoom, sh = 560 * zoom, ox = (page % 2 == 1 ? -12 : 12) * k;
        Ui.Img("Art/scenes/" + p.pic, 640 - 640 * zoom + ox, 280 - 280 * zoom, sw, sh);
        Ui.Img("Art/ui/prtop", 0, 0, 1280, 84);
        string head = storyMode == "epilogue" ? "ЭПИЛОГ" : Db.Roman(storyCh) + " ТАРАУ · " + U.Upper(Db.ChName(storyCh));
        Ui.TextO(head, 40, 18, 900, 40, 28, new Color32(255, 214, 120, 255), TextAnchor.MiddleLeft);
        Ui.TextO("Бет " + (page + 1) + " / " + pages.Count(), 1040, 18, 200, 40, 20, new Color32(255, 211, 107, 255), TextAnchor.MiddleRight);
        int n = pages.Count();
        for (int i = 0; i < n; i++)
        {
            float dx = 640 + (i - (n - 1) / 2f) * 24, r = i == page ? 7 : 4.5f;
            Ui.Fill(dx - r * 0.7f, 34 - r * 0.7f + 30, r * 1.4f, r * 1.4f, i == page ? Ui.GOLD : i < page ? new Color(0.91f, 0.68f, 0.24f, 0.7f) : new Color(0.91f, 0.68f, 0.24f, 0.3f));
        }
        // төменгі панель
        Ui.Img("Art/ui/prbot", 0, 532, 1280, 190);
        float a = Mathf.Clamp01(pt * 2), ax = (1 - a) * -20;
        Ui.Img("Art/ui/medal_44_crest", 78 - 70 + ax, 632 - 70 - 26, 140, 160, a);
        Ui.Portrait("Art/ui/por_zhyrau", 78 + ax, 632, 44);
        Ui.TextO(p.date, 142 + ax, 566, 820, 36, 28, new Color32(255, 214, 120, 255), TextAnchor.MiddleLeft);
        bool dlg = p.dlg != null;
        float tw = dlg ? 700 : 820;
        Ui.TextO(p.Title(ru), 142 + ax, 600, tw, 30, 21, new Color32(255, 243, 224, 255), TextAnchor.MiddleLeft, true, 1.5f);
        // мәтін біртіндеп пайда болады
        string body = p.Body(ru);
        int shown = Mathf.Clamp(Mathf.FloorToInt((pt - 0.2f) * 90), 0, body.Length);
        Ui.Text(body.Substring(0, shown), 142, 634, tw, 90, 17, new Color32(236, 226, 210, 255), TextAnchor.UpperLeft);
        if (dlg)
        {
            JNode o = p.dlg.Get("o");
            if (p.dlgAns < 0) Ui.TextO(ru ? "Выбери ответ ▼" : "Жауабын таңда ▼", 860, 540, 380, 20, 15, Ui.GOLD, TextAnchor.MiddleCenter, true, 1);
            for (int i = 0; i < o.Count(); i++)
            {
                bool right = i == p.dlg.Ik("ok"), picked = i == p.dlgAns;
                if (p.dlgAns >= 0 && (right || picked)) Ui.Fill(866, 562 + i * 50, 368, 42, right ? new Color(0.2f, 0.6f, 0.25f, 0.85f) : new Color(0.7f, 0.18f, 0.15f, 0.85f));
                if (Ui.DzBtn(o.At(i).At(ru ? 1 : 0).Str(), 870, 564 + i * 50, 360, 38, false, p.dlgAns < 0, null, true, false, true))
                {
                    int got = Story.Answer(p, i); pt = 0.2f;
                    Toast(Story.AnswerOk(p) ? (got > 0 ? "Дұрыс жауап! +15 теңге" : "Дұрыс жауап!") : "Есіңде сақта!");
                    Snd.Play(Story.AnswerOk(p) ? "coin" : "ui_error");
                }
            }
        }
        bool last = page == pages.Count() - 1, canNext = !dlg || p.dlgAns >= 0;
        if (!dlg || p.dlgAns >= 0)
        {
            if (Ui.DzBtn("ӨТКІЗУ ▶▶", 1000, 560, 240, 44, false, true, null, true)) StoryFinished();
            string nextLbl = !last ? "КЕЛЕСІ ▶" : storyMode == "fight" ? "ҰРЫСҚА! ⚔" : "ДАЙЫН ✔";
            bool tapNext = Inp.Tap(Inp.ENTER) || Inp.Tap(Inp.RIGHT) || Inp.Tap(Inp.D) || Inp.Tap(Inp.SPACE);
            if (Ui.DzBtn(nextLbl, 1000, 618, 240, 60, true, canNext) || (canNext && tapNext))
            {
                if (shown < body.Length && !tapNext) pt = 10; // бірінші басу — мәтінді толық ашу
                else if (last) StoryFinished(); else { page++; pt = 0; }
            }
        }
        if (page > 0 && Ui.DzBtn("◀", 940, 618, 52, 60, false, true, null, true)) { page--; pt = 1; }
        if (pt < 0.4f) Ui.Fill(0, 0, 1280, 720, new Color(0, 0, 0, 1 - pt / 0.4f));
    }

    // ================= ТАРАУ СОҢЫ =================
    void DrawDone()
    {
        bool ru = Ru();
        Ui.Img("Art/lobby/backdrop", 0, 0, 1280, 720);
        Ui.Fill(0, 0, 1280, 720, new Color(0.04f, 0.03f, 0.1f, 0.6f));
        Ui.Panel(160, 60, 960, 600, new Color(0.96f, 0.88f, 0.7f, 0.97f), Ui.GOLD);
        Ui.Text("Атағың: " + Progress.RankName(), 160, 76, 960, 50, 34, new Color32(125, 23, 25, 255), TextAnchor.MiddleCenter, true);
        Ui.Text(Story.DoneText(doneCh, ru), 200, 136, 880, 110, 21, Ui.INK, TextAnchor.UpperLeft);
        Ui.Fill(200, 250, 880, 3, new Color32(125, 23, 25, 255));
        Ui.Text("ЕСТЕ САҚТА", 200, 260, 880, 40, 26, new Color32(125, 23, 25, 255), TextAnchor.MiddleCenter, true);
        for (int i = 0; i < Story.LessonCount(doneCh); i++)
        {
            float y = 310 + i * 76;
            Ui.Fill(200, y, 880, 66, new Color(1, 1, 1, 0.45f));
            Ui.Text(Story.LessonHead(doneCh, i), 214, y, 230, 66, 20, new Color32(125, 23, 25, 255), TextAnchor.MiddleLeft, true);
            Ui.Text(Story.LessonText(doneCh, i, ru), 450, y, 620, 66, 19, Ui.INK, TextAnchor.MiddleLeft);
        }
        Ui.Text("Келесі тарауды ашу үшін — викторинадан өт (" + Progress.QzPassMin() + "/" + Db.QZ_N + ")", 160, 548, 960, 30, 18, Ui.INK, TextAnchor.MiddleCenter);
        if (Ui.Btn("ВИКТОРИНАҒА ▶", 470, 590, 340, 56, true, true, 24)) StartQuiz(doneCh, true);
    }

    // ================= ВИКТОРИНА =================


    // ================= АЛЬБОМ =================


    // ================= МАРАПАТТАР =================


    // ================= БАПТАУЛАР =================


    // ================= ДҮКЕН =================
    string shopTab = "weapon"; int shopScroll;
    void Equip(string id)
    {
        if (shopTab == "weapon") Game.S.eqWeapon = id; else if (shopTab == "armor") Game.S.eqArmor = id; else Game.S.eqHelm = id;
        Game.Persist();
    }

    // ================= БАТЫРЛАР =================

}

/// <summary>Ұстаз: Бұқар жырау әр экранда бірінші рет қысқаша түсіндіреді (TUT_L).</summary>
public static class Tut
{
    static string key; static int step; static JNode lesson = JNode.Null;
    static string KeyOf(string screen)
    {
        switch (screen)
        {
            case GameRoot.LOBBY: return "lobby";
            case GameRoot.MAP: return "map";
            case GameRoot.STORY: return "prolog";
            case GameRoot.FIGHT: return "fight";
            case GameRoot.SHOP: return "shop";
            case GameRoot.HEROES: return "heroes";
            case GameRoot.ALBUM: return "album";
            case GameRoot.QUIZ: return "quiz";
            case GameRoot.MEDALS: return "medals";
            default: return null;
        }
    }
    public static void Enter(string screen)
    {
        key = KeyOf(screen); step = 0; lesson = JNode.Null;
        if (key == null || Game.S == null || Game.S.tutOff || Game.S.tutDone.Contains(key) || U.Len(Game.S.name) == 0) { key = null; return; }
        lesson = Db.Tut.Get(key);
        if (lesson.Count() == 0) key = null;
    }
    public static bool Active() { return key != null; }

    public static void Draw(string screen)
    {
        if (key == null) return;
        JNode s = lesson.At(step);
        bool ru = Game.S.ru;
        JNode tg = s.Get("tg");
        Ui.Fill(0, 0, 1280, 720, new Color(0, 0, 0, 0.45f));
        if (tg.Count() == 4)
        {
            float x = tg.At(0).F(), y = tg.At(1).F(), w = tg.At(2).F(), h = tg.At(3).F();
            Ui.Fill(x - 4, y - 4, w + 8, 4, Ui.GOLD); Ui.Fill(x - 4, y + h, w + 8, 4, Ui.GOLD);
            Ui.Fill(x - 4, y, 4, h, Ui.GOLD); Ui.Fill(x + w, y, 4, h, Ui.GOLD);
        }
        float by = tg.Count() == 4 && tg.At(1).F() > 360 ? 40 : 470;
        Ui.Panel(150, by, 980, 200, new Color(0.96f, 0.88f, 0.7f, 0.98f), Ui.GOLD);
        Ui.Img("Art/lobby/zhyrau", 160, by + 8, 130, 182);
        Ui.Text("Бұқар жырау", 300, by + 10, 600, 30, 20, new Color32(125, 23, 25, 255), TextAnchor.MiddleLeft, true);
        Ui.Text(Game.Fix(s.S("tx")), 300, by + 42, 810, 110, 19, Ui.INK);
        if (ru) Ui.Text(Game.Fix(s.S("ru")), 300, by + 150, 600, 44, 14, new Color(0.35f, 0.25f, 0.15f));
        bool last = step >= lesson.Count() - 1;
        if (Ui.Btn(last ? "ТҮСІНДІМ ✔" : "КЕЛЕСІ ▶", 930, by + 146, 180, 44, true, true, 17))
        {
            if (last) { Game.S.tutDone.Add(key); Game.Persist(); key = null; } else step++;
        }
        if (Ui.Btn("ӨТКІЗУ", 820, by + 146, 100, 44, false, true, 15)) { Game.S.tutDone.Add(key); Game.Persist(); key = null; }
        // ұстаз ашық тұрғанда астындағы батырмалар басылмасын
        GUI.Button(Ui.R(0, 0, 1280, 720), GUIContent.none, GUIStyle.none);
    }
}
