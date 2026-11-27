// SHAIQAS · Мәзір экрандары (лобби, карта, сюжет, тарау соңы, викторина, альбом, марапаттар, баптаулар, дүкен, батырлар).
// Орналасу вебтегі 1280×720 экранға сай.
using UnityEngine;

public partial class GameRoot
{
    // ================= ЛОББИ (веб: drawLobby + buildUI орындары) =================
    void DrawLobby()
    {
        Ui.Img("Art/lobby/backdrop", 0, 0, 1280, 720);
        float lt = Time.unscaledTime, br = Mathf.Sin(lt * 1.6f) * 1.5f;
        LobbyBack(lt);
        LobbyZhyrau(lt);
        LobbyHero(lt);
        LobbyFront(lt);
        // сол жақ көлеңке (веб: LOBSH)
        Ui.FadeH(0, 0, 528, 720, new Color(0.047f, 0.031f, 0.11f, 0.82f));
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
            string line = ZhLine();
            zk = Mathf.Min(zk, Mathf.Clamp01((lt - zhT0) / 0.35f));   // жаңа сөз — бұлт қайта ашылады
            // веб: zhLobbyBubble — сөз бұлты жыраудың дәл басының үстінде, құйрығы аузына қарай
            float zx = 1130, ease = 1 + 2.7f * Mathf.Pow(zk - 1, 3) + 1.7f * Mathf.Pow(zk - 1, 2);
            float tw = Mathf.Min(380, Ui.TextW(line, 17)), bw = Mathf.Max(260, tw + 40);
            float th = Ui.TextHeight(line, bw - 36, 17, true), bh = 42 + th + 4;
            float bx = Mathf.Min(1280 - 16 - bw, zx - 190), by = 268 - bh + (1 - ease) * 24 + br * 0.6f;
            Color o = GUI.color; GUI.color = new Color(1, 1, 1, Mathf.Clamp01(zk * 1.5f));
            Color edge = new Color32(138, 84, 24, 255), paper = new Color32(248, 236, 210, 255);
            Ui.Fill(bx + 3, by + 5, bw, bh, new Color(0, 0, 0, 0.28f));                 // көлеңке
            Ui.Fill(bx, by, bw, bh, edge); Ui.Fill(bx + 2.5f, by + 2.5f, bw - 5, bh - 5, paper);
            // құйрығы: жыраудың басына қарай сүйірленеді
            float tx = Mathf.Clamp(zx - 10, bx + 40, bx + bw - 40);
            for (int i = 0; i < 22; i++)
            {
                float k = i / 22f, w = 28 * (1 - k), cx = tx - 9 + 21 * k;
                Ui.Fill(cx - w / 2 - 2.5f, by + bh - 3 + i, w + 5, 1.2f, edge);
                if (w > 5) Ui.Fill(cx - w / 2 + 0.5f, by + bh - 3 + i, w - 1, 1.2f, paper);
            }
            Ui.Img("Art/ui/ram", bx + 12, by + 13, 24, 12);
            Ui.Text("Бұқар жырау", bx + 42, by + 8, bw - 52, 24, 16, new Color32(138, 26, 18, 255), TextAnchor.MiddleLeft, true);
            Ui.Text(line, bx + 18, by + 36, bw - 36, th + 6, 17, new Color32(42, 26, 16, 255), TextAnchor.UpperLeft, true);
            GUI.color = o;
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
        Ui.BeginClip(0, 0, 1280, 720);   // үлкейтілген сурет қара жолақтарға шықпайды
        StoryPicFade(p.pic); float pa = StoryPrevA();
        if (pa > 0 && storyPrevPic != null) Ui.Img("Art/scenes/" + storyPrevPic, 640 - 640 * 1.05f, 280 - 280 * 1.05f, 1280 * 1.05f, 560 * 1.05f);
        Ui.Img("Art/scenes/" + p.pic, 640 - 640 * zoom + ox, 280 - 280 * zoom, sw, sh, 1 - pa);
        StoryAir(storyCh);
        Ui.EndClip();
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
        float tw = dlg ? 700 : 780;   // ◀ батырмасына (x 940) тимейді
        Ui.TextO(p.Title(false), 142 + ax, 600, tw, 30, 21, new Color32(255, 243, 224, 255), TextAnchor.MiddleLeft, true, 1.5f);
        // мәтін біртіндеп пайда болады — әрқашан қазақша
        string body = p.Body(false);
        int shown = Mathf.Clamp(Mathf.FloorToInt((pt - 0.2f) * 90), 0, body.Length);
        Ui.Text(body.Substring(0, shown), 142, 634, tw, 90, 17, new Color32(236, 226, 210, 255), TextAnchor.UpperLeft);
        // ҚАЗ + РУС: орысша аударма — сурет үстіндегі жеке жолақта, қазақ мәтінін жаппайды
        if (ru && p.ruText != null) Ui.RuStrip(Game.Fix((p.ruTitle != null ? p.ruTitle + ". " : "") + p.ruText), 142, 524, dlg ? 700 : 820);
        if (dlg)
        {
            JNode o = p.dlg.Get("o");
            if (p.dlgAns < 0) Ui.TextO(ru ? "Жауабын таңда · Выбери ответ ▼" : "Жауабын таңда ▼", 860, 540, 380, 20, 15, Ui.GOLD, TextAnchor.MiddleCenter, true, 1);
            for (int i = 0; i < o.Count(); i++)
            {
                bool right = i == p.dlg.Ik("ok"), picked = i == p.dlgAns;
                if (p.dlgAns >= 0 && (right || picked)) Ui.Fill(866, 562 + i * 50, 368, 42, right ? new Color(0.2f, 0.6f, 0.25f, 0.85f) : new Color(0.7f, 0.18f, 0.15f, 0.85f));
                bool hasRu = ru && o.At(i).Count() > 1 && o.At(i).At(1).Str() != o.At(i).At(0).Str();
                if (hasRu) Ui.Text(Game.Fix(o.At(i).At(1).Str()), 874, 600 + i * 50, 352, 14, 11, new Color(0.85f, 0.8f, 0.95f, 0.9f), TextAnchor.MiddleCenter, true);
                if (Ui.DzBtn(o.At(i).At(0).Str(), 870, 564 + i * 50, 360, hasRu ? 34 : 38, false, p.dlgAns < 0, null, true, false, true))
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
        if (pt < 0.4f && StoryPrevA() <= 0 && page == 0) Ui.Fill(0, 0, 1280, 720, new Color(0, 0, 0, 1 - pt / 0.4f));
    }

    // ================= ТАРАУ СОҢЫ =================
    void DrawDone()
    {
        bool ru = Ru();
        Ui.Img("Art/lobby/backdrop", 0, 0, 1280, 720);
        Ui.Fill(0, 0, 1280, 720, new Color(0.04f, 0.03f, 0.1f, 0.6f));
        Ui.DzPanel(160, 60, 960, 600, "parch", true);
        Ui.Text("Атағың: " + Progress.RankName(), 160, 76, 960, 50, 34, new Color32(125, 23, 25, 255), TextAnchor.MiddleCenter, true);
        Ui.Text(Story.DoneText(doneCh, false), 200, 136, 880, 110, 21, Ui.INK, TextAnchor.UpperLeft);
        Ui.Fill(200, 250, 880, 3, new Color32(125, 23, 25, 255));
        Ui.Text("ЕСТЕ САҚТА", 200, 260, 880, 40, 26, new Color32(125, 23, 25, 255), TextAnchor.MiddleCenter, true);
        for (int i = 0; i < Story.LessonCount(doneCh); i++)
        {
            float y = 310 + i * 76;
            Ui.Fill(200, y, 880, 66, new Color(1, 1, 1, 0.45f));
            Ui.Text(Story.LessonHead(doneCh, i), 214, y, 230, 66, 20, new Color32(125, 23, 25, 255), TextAnchor.MiddleLeft, true);
            if (ru)
            {
                Ui.Text(Story.LessonText(doneCh, i, false), 450, y + 2, 620, 40, 17, Ui.INK, TextAnchor.MiddleLeft);
                Ui.Text(Game.Fix(Story.LessonText(doneCh, i, true)), 450, y + 42, 620, 22, 13, new Color32(110, 80, 50, 255), TextAnchor.MiddleLeft);
            }
            else Ui.Text(Story.LessonText(doneCh, i, false), 450, y, 620, 66, 19, Ui.INK, TextAnchor.MiddleLeft);
        }
        Ui.Text("Келесі тарауды ашу үшін — викторинадан өт (" + Progress.QzPassMin() + "/" + Db.QZ_N + ")", 160, 548, 960, 30, 18, Ui.INK, TextAnchor.MiddleCenter);
        if (Ui.DzBtn("ВИКТОРИНАҒА ▶", 470, 590, 340, 60, true)) StartQuiz(doneCh, true);
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

    // панельдің ағымдағы орны (нысанаға қарай жылжиды)
    static float pxCur = -1, pyCur = -1; static string lastKey; static int lastStep = -1; static float stepT;
    static Rect[] rs = new Rect[8]; static int rn;

    /// <summary>Нысаналар (веб: tutRects): батырма белгісі немесе [x,y,w,h].</summary>
    static void Targets(JNode tg)
    {
        rn = 0; if (tg.IsNull() || tg.Count() == 0) return;
        if (tg.At(0).IsNum()) { if (tg.Count() == 4) rs[rn++] = new Rect(tg.At(0).F(), tg.At(1).F(), tg.At(2).F(), tg.At(3).F()); return; }
        for (int i = 0; i < tg.Count() && rn < rs.Length; i++)
        {
            JNode it = tg.At(i);
            if (it.IsArr() && it.Count() == 4 && it.At(0).IsNum()) rs[rn++] = new Rect(it.At(0).F(), it.At(1).F(), it.At(2).F(), it.At(3).F());
            else if (it.IsArr()) { for (int j = 0; j < it.Count() && rn < rs.Length; j++) { JNode q = it.At(j); if (q.IsArr() && q.Count() == 4) rs[rn++] = new Rect(q.At(0).F(), q.At(1).F(), q.At(2).F(), q.At(3).F()); } }
            else { Rect r; if (Ui.FindReg(it.Str(), out r)) rs[rn++] = r; }
        }
    }

    public static void Draw(string screen)
    {
        if (key == null) { pxCur = -1; return; }
        JNode s = lesson.At(step);
        bool ru = Game.S.ru;
        if (key != lastKey || step != lastStep) { lastKey = key; lastStep = step; stepT = 0; }
        stepT += Time.unscaledDeltaTime;
        float T = Time.unscaledTime, a = Pz.EaseOut3(Mathf.Clamp01(stepT / 0.3f));
        Targets(s.Get("tg"));
        bool hasU = rn > 0; Rect UR = hasU ? rs[0] : new Rect();
        for (int i = 1; i < rn; i++) UR = Rect.MinMaxRect(Mathf.Min(UR.xMin, rs[i].xMin), Mathf.Min(UR.yMin, rs[i].yMin), Mathf.Max(UR.xMax, rs[i].xMax), Mathf.Max(UR.yMax, rs[i].yMax));

        // панель өлшемі
        // телефонда — сенсорлық батырмаларға арналған мәтін (txT/ruT), {N}/{M} — викторина сандары
        bool tch = Application.isMobilePlatform;
        string tx = Game.Fix(tch && U.Len(s.S("txT")) > 0 ? s.S("txT") : s.S("tx")), rt = ru ? Game.Fix(tch && U.Len(s.S("ruT")) > 0 ? s.S("ruT") : s.S("ru")) : null;
        if (U.Len(tx) == 0) { if (step < lesson.Count() - 1) step++; else { Game.S.tutDone.Add(key); key = null; } return; }   // бос қадам — өткізіп жібереміз
        tx = tx.Replace("{N}", U.Int(Db.QZ_N)).Replace("{M}", U.Int(Progress.QzPassMin())); if (rt != null) rt = rt.Replace("{N}", U.Int(Db.QZ_N)).Replace("{M}", U.Int(Progress.QzPassMin()));
        float w = 620, th = Ui.TextHeight(tx, w - 56, 19, true), rh = U.Len(rt) > 0 ? Ui.TextHeight(rt, w - 56, 14, true) + 10 : 0;
        // кейбір кадрларда (ұрыс экраны) CalcHeight 0 қайтарады — мәтін жоғалып кетпесін: әріп санымен бағалаймыз
        float estT = Mathf.Ceil(U.Len(tx) * 10.2f / (w - 56)) * 25, estR = U.Len(rt) > 0 ? Mathf.Ceil(U.Len(rt) * 7.4f / (w - 56)) * 18 + 10 : 0;
        th = Mathf.Max(th, estT); rh = Mathf.Max(rh, estR);
        float h = 98 + th + rh + 60;
        // мақсатты орын: нысананың жанында (оң, сол, асты, үсті), онымен қабаттаспай
        float gx = 640 - w / 2, gy = 720 - h - 22;
        if (hasU)
        {
            float cy = UR.center.y - h / 2, cx = UR.center.x - w / 2;
            float[] cxs = { UR.xMax + 34, UR.xMin - 34 - w, cx, cx, 640 - w / 2, 640 - w / 2 };
            float[] cys = { cy, cy, UR.yMax + 34, UR.yMin - 34 - h, 720 - h - 22, 92 };
            for (int i = 0; i < cxs.Length; i++)
            {
                float qx = Mathf.Clamp(cxs[i], 16, 1280 - w - 16), qy = Mathf.Clamp(cys[i], 16, 720 - h - 16);
                bool hit = !(qx + w < UR.xMin - 14 || qx > UR.xMax + 14 || qy + h < UR.yMin - 14 || qy > UR.yMax + 14);
                if (!hit) { gx = qx; gy = qy; break; }
            }
        }
        if (pxCur < 0) { pxCur = gx; pyCur = gy; }
        float sp = Mathf.Min(1, Time.unscaledDeltaTime * 7);
        pxCur += (gx - pxCur) * sp; pyCur += (gy - pyCur) * sp;
        float px = pxCur, py = pyCur;

        // қараңғылау: нысананың «терезесі» ашық қалады
        Color dim = new Color(0.03f, 0.02f, 0.07f, 0.66f * a);
        if (hasU)
        {
            float p = 10, x0 = UR.xMin - p, y0 = UR.yMin - p, x1 = UR.xMax + p, y1 = UR.yMax + p;
            Ui.Fill(0, 0, 1280, y0, dim); Ui.Fill(0, y1, 1280, 720 - y1, dim);
            Ui.Fill(0, y0, x0, y1 - y0, dim); Ui.Fill(x1, y0, 1280 - x1, y1 - y0, dim);
            float pul = 0.55f + 0.45f * Mathf.Sin(T * 5);
            for (int i = 0; i < rn; i++)
            {
                Rect r = rs[i]; Color gc = new Color(1, 0.84f, 0.42f, (0.6f + 0.4f * pul) * a);
                Ui.Frame(r.x - 8, r.y - 8, r.width + 16, r.height + 16, gc, 3.5f);
                Ui.Frame(r.x - 12, r.y - 12, r.width + 24, r.height + 24, new Color(1, 0.84f, 0.42f, 0.25f * pul * a), 2);
            }
            // нұсқағыш: панельден нысанаға қарай қозғалатын нүктелі жебе
            float tx0 = UR.center.x, ty0 = UR.center.y, ax = Mathf.Clamp(tx0, px + 40, px + w - 40), ay = ty0 < py ? py : ty0 > py + h ? py + h : py + h / 2;
            if (ty0 >= py && ty0 <= py + h) { ax = tx0 < px ? px : px + w; ay = Mathf.Clamp(ty0, py + 30, py + h - 30); }
            float dx = tx0 - ax, dy = ty0 - ay, d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d > 1)
            {
                float ux = dx / d, uy = dy / d, hw = UR.width / 2 + 16, hh = UR.height / 2 + 16;
                float inside = Mathf.Min(Mathf.Abs(ux) > 1e-4f ? hw / Mathf.Abs(ux) : 1e9f, Mathf.Abs(uy) > 1e-4f ? hh / Mathf.Abs(uy) : 1e9f);
                float edge = Mathf.Max(0, d - inside), bob = 6 * Mathf.Sin(T * 6);
                if (edge > 36)
                {
                    float ex = ax + ux * (edge - 10 + bob), ey = ay + uy * (edge - 10 + bob);
                    Color ac = new Color(1, 0.84f, 0.42f, 0.9f * a);
                    float off = (T * 30) % 19;
                    for (float q = 14 + off; q < edge - 24 + bob; q += 19) Ui.Line(ax + ux * q, ay + uy * q, ax + ux * Mathf.Min(q + 10, edge - 24 + bob), ay + uy * Mathf.Min(q + 10, edge - 24 + bob), 4, ac);
                    float nx = -uy, ny = ux;
                    Ui.Line(ex, ey, ex - ux * 18 + nx * 12, ey - uy * 18 + ny * 12, 5, ac);
                    Ui.Line(ex, ey, ex - ux * 18 - nx * 12, ey - uy * 18 - ny * 12, 5, ac);
                }
            }
        }
        else Ui.Fill(0, 0, 1280, 720, dim);

        // панель
        Color o = GUI.color; GUI.color = new Color(1, 1, 1, a);
        float yy = py + (1 - a) * 16;
        Ui.DzPanel(px, yy, w, h, "parch", true);
        Ui.Medal(px + 58, yy + 60, 36, "");
        Ui.Portrait("Art/ui/por_zhyrau", px + 58, yy + 60, 32);
        Ui.Text("Бұқар жырау", px + 108, yy + 34, 300, 30, 22, new Color32(138, 26, 18, 255), TextAnchor.MiddleLeft, true);
        int n = lesson.Count();
        for (int i = 0; i < n; i++) Ui.Circle(px + w - 30 - (n - 1 - i) * 16, yy + 48, i == step ? 5 : 3.5f, i == step ? (Color)new Color32(138, 26, 18, 255) : new Color32(138, 90, 42, 120));
        Ui.Text(tx, px + 28, yy + 92, w - 56, th + 6, 19, new Color32(42, 26, 16, 255), TextAnchor.UpperLeft, true);
        if (rh > 0) Ui.Text(rt, px + 28, yy + 98 + th, w - 56, rh, 14, new Color32(122, 90, 58, 255), TextAnchor.UpperLeft, true);
        bool last = step >= n - 1;
        float by = yy + h - 56;
        if (Ui.DzBtn(last ? "ТҮСІНДІМ ✔" : "КЕЛЕСІ ▶", px + w - 250, by, 228, 46, true, true, null, true))
        {
            if (last) { Game.S.tutDone.Add(key); Game.Persist(); key = null; } else step++;
        }
        else if (!last && Ui.DzBtn("ӨТКІЗУ", px + 22, by + 4, 150, 40, false, true, null, true, false, true)) { Game.S.tutDone.Add(key); Game.Persist(); key = null; }
        GUI.color = o;
        if (key != null && Event.current.type == EventType.Layout && (Inp.Tap(Inp.ENTER) || Inp.Tap(Inp.SPACE))) { if (last) { Game.S.tutDone.Add(key); Game.Persist(); key = null; } else step++; }
    }
}
