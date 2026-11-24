// SHAIQAS · Мәзір экрандары (лобби, карта, сюжет, тарау соңы, викторина, альбом, марапаттар, баптаулар, дүкен, батырлар).
// Орналасу вебтегі 1280×720 экранға сай.
using UnityEngine;

public partial class GameRoot
{
    // ================= ЛОББИ =================
    void DrawLobby()
    {
        Ui.Img("Art/lobby/backdrop", 0, 0, 1280, 720);
        Ui.Img("Art/lobby/zhyrau", 980, 250, 300, 420);
        Ui.Img("Art/lobby/front_" + Game.S.hero, 550, 110, 420, 560);
        // сол жақ көлеңке
        for (int i = 0; i < 12; i++) Ui.Fill(i * 40, 0, 40, 720, new Color(0.05f, 0.03f, 0.11f, 0.8f * (1 - i / 12f)));
        Ui.TopBar(null);
        Ui.TextS("ШАЙҚАС", 60, 84, 520, 90, 72, Ui.GOLD, TextAnchor.MiddleLeft);
        Ui.TextS("Қазақ тарихының батырлары", 64, 168, 520, 36, 22, Ui.CREAM, TextAnchor.MiddleLeft, false);
        float y = 226;
        if (Ui.Btn("ЖОРЫҚ", 60, y, 360, 66, true, true, 30)) OpenMap(Progress.LastOpen());
        y += 80;
        if (Ui.Btn("ДҮКЕН", 60, y, 175, 54)) { shopTab = "weapon"; Go(SHOP); }
        if (Ui.Btn("БАТЫРЛАР", 245, y, 175, 54)) Go(HEROES);
        y += 64;
        if (Ui.Btn("ТАРИХ АЛЬБОМЫ", 60, y, 360, 54)) { albumSel = -1; Go(ALBUM); }
        y += 64;
        if (Ui.Btn("МАРАПАТТАР", 60, y, 175, 54)) Go(MEDALS);
        if (Ui.Btn("БАПТАУЛАР", 245, y, 175, 54)) Go(SETTINGS);
        y += 70;
        int L = Progress.Level();
        Ui.TextS(L + "-деңгей", 60, y, 200, 30, 20, Ui.GOLD, TextAnchor.MiddleLeft);
        Ui.Bar(60, y + 34, 360, 12, L >= Progress.MAXL ? 1 : Progress.XpInto() / (float)Progress.Need(L), Ui.GOLD, new Color(0, 0, 0, 0.5f));
    }

    // ================= КАРТА =================
    void DrawMap()
    {
        JNode C = Db.Ch(mapCh);
        Ui.Fill(0, 0, 1280, 720, new Color32(24, 18, 34, 255));
        Ui.TopBar(Db.Roman(mapCh) + " ТАРАУ · " + U.Upper(C.S("name")));
        Ui.Img("Art/maps/chapter" + mapCh, 30, 92, 820, 560);
        bool open = Progress.Unlocked(mapCh);
        JNode nodes = C.Get("nodes");
        for (int i = 0; i < nodes.Count(); i++)
        {
            float nx = 30 + nodes.At(i).Fk("x"), ny = 92 + nodes.At(i).Fk("y");
            bool can = open && Game.S.prog[mapCh] >= i;
            bool sel = i == mapStage;
            Ui.Fill(nx - 22, ny - 22, 44, 44, sel ? Ui.GOLD : can ? new Color32(125, 23, 25, 255) : new Color(0.3f, 0.3f, 0.3f));
            Ui.TextS(U.Int(i + 1), nx - 22, ny - 22, 44, 44, 22, sel ? Ui.INK : Ui.CREAM, TextAnchor.MiddleCenter);
            Ui.TextS(nodes.At(i).S("n"), nx - 90, ny + 24, 180, 26, 15, Ui.CREAM, TextAnchor.UpperCenter);
            if (can && Ui.Hot(nx - 30, ny - 30, 60, 60)) mapStage = i;
        }
        // оң жақ панель
        Ui.Panel(870, 92, 380, 560, new Color(0.1f, 0.07f, 0.17f, 0.95f), Ui.GOLD);
        Ui.TextS(Db.Years.At(mapCh).Str(), 890, 104, 340, 30, 20, Ui.GOLD, TextAnchor.MiddleCenter);
        if (!open)
        {
            Ui.TextS(Progress.LockMsg(mapCh), 890, 160, 340, 120, 22, Ui.CREAM, TextAnchor.MiddleCenter);
            if (Progress.QuizGate(mapCh) && Ui.Btn(Db.Roman(mapCh - 1) + " ТАРАУ ВИКТОРИНАСЫ ▶", 890, 300, 340, 58, true, true, 20)) StartQuiz(mapCh - 1, true);
        }
        else
        {
            for (int i = 0; i < 4; i++)
            {
                float y = 146 + i * 62;
                bool can = Game.S.prog[mapCh] >= i, sel = i == mapStage;
                JNode e = C.Get("enemies").At(i);
                Ui.Fill(886, y, 348, 54, sel ? new Color(0.95f, 0.72f, 0.25f, 0.25f) : new Color(0, 0, 0, 0.25f));
                Ui.TextS((i + 1) + ". " + (can ? e.S("name") : "???"), 896, y + 2, 250, 50, 17, can ? Ui.CREAM : new Color(0.6f, 0.6f, 0.6f), TextAnchor.MiddleLeft, e.Bk("boss"));
                int stars = Game.S.stars[mapCh][i];
                string ss = ""; for (int k = 0; k < 3; k++) ss += (stars & (1 << k)) != 0 ? "★" : "☆";
                Ui.TextS(ss, 1150, y + 2, 80, 50, 20, Ui.GOLD, TextAnchor.MiddleRight);
                if (can && Ui.Hot(886, y, 348, 54)) mapStage = i;
            }
            JNode foe = C.Get("enemies").At(mapStage);
            string sub = mapStage == 3 ? "БАС ЖАУ · 3 раундта жең" : "2 раундта жең";
            Ui.TextS(sub, 890, 400, 340, 28, 17, mapStage == 3 ? new Color(1, 0.5f, 0.4f) : Ui.CREAM, TextAnchor.MiddleCenter, false);
            if (Ui.Btn("ЖОРЫҚҚА ▶", 890, 436, 340, 62, true, true, 26)) Campaign(mapCh, mapStage);
            if (Ui.Btn("ТАРИХЫН ОҚУ", 890, 508, 165, 48, false, true, 17)) StartStory(Story.Episodes(mapCh), "back", mapCh, 0);
            if (Ui.Btn("ЖАТТЫҒУ", 1065, 508, 165, 48, false, true, 17)) StartTrain(mapCh);
            bool qOpen = Progress.ChapterDone(mapCh);
            string ql = qOpen ? "ВИКТОРИНА · " + Game.S.quizBest[mapCh] + "/" + Db.QZ_N : "Викторина: тарауды аяқта";
            if (Ui.Btn(ql, 890, 566, 340, 48, false, qOpen, 17)) StartQuiz(mapCh, false);
        }
        if (Ui.Btn("◀ ЛОББИ", 30, 662, 180, 48, false, true, 18)) Go(LOBBY);
        if (Ui.Btn("◀", 600, 662, 60, 48, false, mapCh > 0, 22)) { mapCh--; mapStage = Mathf.Clamp(Game.S.prog[mapCh], 0, 3); }
        Ui.TextS((mapCh + 1) + " / " + Db.NCH, 660, 662, 100, 48, 20, Ui.CREAM, TextAnchor.MiddleCenter);
        if (Ui.Btn("▶", 760, 662, 60, 48, false, mapCh < Db.NCH - 1, 22)) { mapCh++; mapStage = Mathf.Clamp(Game.S.prog[mapCh], 0, 3); }
    }

    // ================= СЮЖЕТ =================
    void DrawStory()
    {
        Page p = pages.Get(page);
        bool ru = Ru();
        Ui.Fill(0, 0, 1280, 720, Color.black);
        float zoom = 1 + 0.05f * Mathf.Min(1, pt / 10);
        float sw = 1280 * zoom, sh = 560 * zoom;
        Ui.Img("Art/scenes/" + p.pic, (1280 - sw) / 2 + (page % 2 == 1 ? -12 : 12) * Mathf.Min(1, pt / 10), 280 - 280 * zoom, sw, sh);
        Ui.Fill(0, 0, 1280, 84, new Color(0.04f, 0.03f, 0.09f, 0.75f));
        string head = storyMode == "epilogue" ? "ЭПИЛОГ" : Db.Roman(storyCh) + " ТАРАУ · " + U.Upper(Db.ChName(storyCh));
        Ui.TextS(head, 40, 16, 800, 40, 26, Ui.GOLD, TextAnchor.MiddleLeft);
        Ui.TextS("Бет " + (page + 1) + " / " + pages.Count(), 1040, 16, 200, 40, 20, Ui.GOLD, TextAnchor.MiddleRight, false);
        Ui.TextS(p.date, 40, 52, 800, 26, 16, Ui.CREAM, TextAnchor.MiddleLeft, false);
        // төменгі панель
        Ui.Fill(0, 520, 1280, 200, new Color(0.08f, 0.06f, 0.16f, 0.97f));
        Ui.Fill(0, 520, 1280, 4, Ui.GOLD);
        bool dlg = p.dlg != null;
        float tw = dlg ? 900 : 1200;
        Ui.TextS(p.Title(ru), 40, 532, tw, 34, 24, Ui.GOLD, TextAnchor.MiddleLeft);
        Ui.Text(p.Body(ru), 40, 570, tw, 100, 19, Ui.CREAM, TextAnchor.UpperLeft);
        if (dlg)
        {
            JNode o = p.dlg.Get("o");
            if (p.dlgAns < 0) Ui.TextS(ru ? "Выбери ответ ▼" : "Жауабын таңда ▼", 990, 528, 270, 22, 15, Ui.GOLD, TextAnchor.MiddleCenter, false);
            for (int k = 0; k < o.Count(); k++)
            {
                bool right = k == p.dlg.Ik("ok"), picked = k == p.dlgAns;
                string lbl = o.At(k).At(ru ? 1 : 0).Str();
                if (p.dlgAns >= 0 && (right || picked))
                    Ui.Fill(990, 552 + k * 54, 270, 46, right ? new Color(0.2f, 0.6f, 0.25f) : new Color(0.7f, 0.18f, 0.15f));
                if (Ui.Btn(lbl, 990, 552 + k * 54, 270, 46, false, p.dlgAns < 0, 15))
                {
                    int got = Story.Answer(p, k);
                    Toast(Story.AnswerOk(p) ? (got > 0 ? "Дұрыс жауап! +15 теңге" : "Дұрыс жауап!") : "Есіңде сақта!");
                }
            }
        }
        bool last = page == pages.Count() - 1;
        bool canNext = !dlg || p.dlgAns >= 0;
        if (page > 0 && Ui.Btn("◀ АРТҚА", 40, 668, 170, 44, false, true, 17)) { page--; pt = 0; }
        if (Ui.Btn("ӨТКІЗУ", 220, 668, 130, 44, false, true, 16)) StoryFinished();
        string nextLbl = !last ? "КЕЛЕСІ ▶" : storyMode == "fight" ? "ҰРЫСҚА! ⚔" : "ДАЙЫН ✔";
        if (Ui.Btn(nextLbl, dlg ? 990 : 1040, dlg ? 714 - 46 : 668, dlg ? 270 : 200, 44, true, canNext, 18)
            || (canNext && (Inp.Tap(Inp.ENTER) || Inp.Tap(Inp.RIGHT) || Inp.Tap(Inp.D))))
        {
            if (last) StoryFinished(); else { page++; pt = 0; }
        }
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
    void DrawQuiz()
    {
        bool ru = Ru(); Quiz q = quiz;
        Ui.Fill(0, 0, 1280, 720, new Color32(30, 22, 40, 255));
        Ui.TopBar("ТАРИХ ВИКТОРИНАСЫ");
        Ui.TextS(Db.Roman(q.ch) + " тарау · " + Db.ChName(q.ch), 0, 76, 1280, 34, 20, Ui.CREAM, TextAnchor.MiddleCenter);
        if (q.done)
        {
            Ui.Panel(340, 150, 600, 420, new Color(0.96f, 0.88f, 0.7f, 0.97f), Ui.GOLD);
            Ui.Text(q.Verdict(), 340, 170, 600, 60, 44, new Color32(125, 23, 25, 255), TextAnchor.MiddleCenter, true);
            Ui.Text(q.score + " / " + q.Total(), 340, 240, 600, 70, 54, Ui.INK, TextAnchor.MiddleCenter, true);
            if (q.reward > 0) Ui.Text("Жаңа рекорд! +" + q.reward + " теңге", 340, 316, 600, 36, 22, Ui.INK, TextAnchor.MiddleCenter);
            bool pass = q.Passed();
            string msg = quizFromDone ? (pass ? (q.ch == Db.NCH - 1 ? "Барлық тарау аяқталды!" : Db.Roman(q.ch + 1) + " тарау ашылды!") : "Келесі тарау үшін кемі " + Progress.QzPassMin() + " дұрыс жауап керек")
                                      : (pass ? "Жарайсың!" : "Тағы байқап көр!");
            Ui.Text(msg, 360, 360, 560, 60, 21, pass ? new Color(0.15f, 0.45f, 0.15f) : new Color32(125, 23, 25, 255), TextAnchor.MiddleCenter, true);
            if (!pass && Ui.Btn("ҚАЙТА ТАПСЫРУ", 370, 470, 260, 60, true, true, 20)) StartQuiz(q.ch, quizFromDone);
            if (Ui.Btn(pass && quizFromDone ? "ӘРІ ҚАРАЙ ▶" : "КАРТАҒА", pass ? 490 : 650, 470, pass ? 300 : 260, 60, pass, true, 20)) QuizExit();
            return;
        }
        Ui.TextS((q.i + 1) + " / " + q.Total() + "   ·   ұпай: " + q.score, 0, 116, 1280, 30, 18, Ui.GOLD, TextAnchor.MiddleCenter, false);
        Ui.Panel(180, 156, 920, 150, new Color(0.96f, 0.88f, 0.7f, 0.97f), Ui.GOLD);
        Ui.Text(q.Question(ru), 210, 166, 860, 130, 27, Ui.INK, TextAnchor.MiddleCenter, true);
        for (int k = 0; k < 3; k++)
        {
            float y = 330 + k * 86;
            if (q.picked >= 0)
            {
                if (q.IsRight(k)) Ui.Fill(276, y - 4, 728, 76, new Color(0.25f, 0.7f, 0.3f));
                else if (k == q.picked) Ui.Fill(276, y - 4, 728, 76, new Color(0.8f, 0.2f, 0.15f));
            }
            if (Ui.Btn(q.Option(k, ru), 280, y, 720, 68, false, q.picked < 0, 22)) q.Pick(k);
        }
        if (q.picked >= 0 && Ui.Btn(q.i < q.Total() - 1 ? "КЕЛЕСІ ▶" : "НӘТИЖЕ ▶", 490, 610, 300, 60, true, true, 22)) q.Next();
        if (Ui.Btn("◀ ШЫҒУ", 30, 662, 160, 48, false, true, 16)) OpenMap(q.ch);
    }

    // ================= АЛЬБОМ =================
    void DrawAlbum()
    {
        bool ru = Ru();
        Ui.Fill(0, 0, 1280, 720, new Color32(34, 26, 20, 255));
        Ui.TopBar("ТАРИХ АЛЬБОМЫ · " + Medals.CardsOpen() + "/" + Db.Hist.Count());
        int n = Db.Hist.Count();
        for (int i = 0; i < n; i++)
        {
            int cx = i % 9, cy = i / 9;
            float x = 40 + cx * 74, y = 90 + cy * 140;
            bool open = Medals.CardOpen(i);
            Ui.Img("Art/icons/hist" + i + (open ? "" : "_locked"), x, y, 70, 70);
            Ui.Text(open ? Db.Hist.At(i).S("title") : "?", x - 4, y + 72, 78, 60, 12, open ? Ui.CREAM : new Color(0.5f, 0.5f, 0.5f), TextAnchor.UpperCenter);
            if (open && Ui.Hot(x, y, 70, 130)) albumSel = i;
        }
        Ui.Panel(720, 90, 530, 560, new Color(0.96f, 0.88f, 0.7f, 0.97f), Ui.GOLD);
        if (albumSel < 0) Ui.Text("Картаны таңда. Әр жеңілген ұрыс — бір жаңа тарих картасы!", 750, 300, 470, 120, 22, Ui.INK, TextAnchor.MiddleCenter);
        else
        {
            JNode h = Db.Hist.At(albumSel), r = Db.RuCards.At(albumSel);
            Ui.Img("Art/icons/hist" + albumSel, 745, 110, 110, 110);
            Ui.Text(ru ? r.At(0).Str() : h.S("title"), 870, 112, 360, 60, 28, new Color32(125, 23, 25, 255), TextAnchor.MiddleLeft, true);
            Ui.Text(h.S("date"), 870, 172, 360, 36, 18, Ui.INK, TextAnchor.MiddleLeft);
            float y = 236;
            JNode facts = ru ? r.At(1) : h.Get("facts");
            for (int k = 0; k < facts.Count(); k++)
            {
                string t = "• " + facts.At(k).Str();
                float th = Ui.TextHeight(t, 470, 19);
                Ui.Text(t, 750, y, 470, th + 6, 19, Ui.INK);
                y += th + 12;
            }
            string fun = ru ? r.At(2).Str() : h.S("fun");
            if (U.Len(fun) > 0) { Ui.Text((ru ? "Интересно: " : "Қызық дерек: ") + fun, 750, y + 10, 470, 120, 18, new Color32(125, 23, 25, 255), TextAnchor.UpperLeft, true); }
        }
        if (Ui.Btn("◀ ЛОББИ", 30, 662, 180, 48, false, true, 18)) Go(LOBBY);
    }

    // ================= МАРАПАТТАР =================
    void DrawMedals()
    {
        bool ru = Ru();
        Ui.Fill(0, 0, 1280, 720, new Color32(30, 22, 40, 255));
        Ui.TopBar("МАРАПАТТАР");
        for (int i = 0; i < Db.Medals.Count(); i++)
        {
            JNode m = Db.Medals.At(i); string id = m.S("id"); bool got = Medals.Has(id);
            float x = 60 + (i % 4) * 295, y = 90 + (i / 4) * 140;
            Ui.Fill(x, y, 280, 128, new Color(0, 0, 0, 0.3f));
            Ui.Img("Art/icons/medal_" + id + (got ? "" : "_off"), x + 6, y + 14, 100, 100);
            JNode rm = Db.RuMed.Get(id);
            Ui.TextS(ru && rm.IsArr() ? rm.At(0).Str() : m.S("name"), x + 112, y + 10, 162, 50, 17, got ? Ui.GOLD : new Color(0.6f, 0.6f, 0.6f), TextAnchor.UpperLeft);
            Ui.Text(ru && rm.IsArr() ? rm.At(1).Str() : m.S("desc"), x + 112, y + 60, 162, 66, 14, Ui.CREAM);
        }
        if (Ui.Btn("◀ ЛОББИ", 30, 662, 180, 48, false, true, 18)) Go(LOBBY);
    }

    // ================= БАПТАУЛАР =================
    void DrawSettings()
    {
        Ui.Img("Art/lobby/backdrop", 0, 0, 1280, 720);
        Ui.Fill(0, 0, 1280, 720, new Color(0.04f, 0.03f, 0.1f, 0.65f));
        Ui.TopBar("БАПТАУЛАР");
        Ui.Panel(340, 110, 600, 500, new Color(0.1f, 0.07f, 0.17f, 0.96f), Ui.GOLD);
        SaveData S = Game.S;
        if (Ui.Btn("ОРЫСША АУДАРМА: " + (S.ru ? "ҚОСУЛЫ" : "ӨШІК"), 380, 140, 520, 56)) { S.ru = !S.ru; Game.Persist(); }
        if (Ui.Btn("БАЛАЛАР РЕЖИМІ: " + (S.kid ? "ҚОСУЛЫ" : "ӨШІК"), 380, 210, 520, 56)) { S.kid = !S.kid; Game.Persist(); }
        if (Ui.Btn("ҰСТАЗ: " + (S.tutOff ? "ӨШІК" : "ҚОСУЛЫ"), 380, 280, 520, 56)) { S.tutOff = !S.tutOff; if (!S.tutOff) S.tutDone.Clear(); Game.Persist(); }
        if (Ui.Btn("✎ АТЫМДЫ ӨЗГЕРТУ", 380, 350, 520, 56)) AskName();
        Ui.Text("Балалар режимі: жау әлсіздеу, викторинада 5 дұрыс жауап жеткілікті.", 380, 420, 520, 60, 16, Ui.CREAM, TextAnchor.UpperCenter);
        Ui.Text("SHAIQAS · Unity нұсқасы (алғашқы порт)", 380, 520, 520, 30, 14, new Color(0.7f, 0.7f, 0.8f), TextAnchor.MiddleCenter);
        if (Ui.Btn("◀ ЛОББИ", 30, 662, 180, 48, false, true, 18)) Go(LOBBY);
    }

    // ================= ДҮКЕН =================
    string shopTab = "weapon"; int shopScroll;
    void DrawShop()
    {
        Ui.Fill(0, 0, 1280, 720, new Color32(40, 28, 20, 255));
        Ui.TopBar("ДҮКЕН");
        string[] tabs = { "weapon", "armor", "helm" }, names = { "ҚАРУ", "САУЫТ", "БАС КИІМ" };
        for (int t = 0; t < 3; t++) if (Ui.Btn(names[t], 40 + t * 210, 80, 200, 48, shopTab == tabs[t])) { shopTab = tabs[t]; shopScroll = 0; }
        JNode table = shopTab == "weapon" ? Db.Weapons : shopTab == "armor" ? Db.Armors : Db.Helms;
        JNode order = shopTab == "weapon" ? Db.WeaponOrder : shopTab == "armor" ? Db.ArmorOrder : Db.HelmOrder;
        Lst<string> owned = shopTab == "weapon" ? Game.S.ownWeapon : shopTab == "armor" ? Game.S.ownArmor : Game.S.ownHelm;
        string eq = shopTab == "weapon" ? Game.S.eqWeapon : shopTab == "armor" ? Game.S.eqArmor : Game.S.eqHelm;
        int per = 4, n = order.Count();
        shopScroll = Mathf.Clamp(shopScroll, 0, Mathf.Max(0, n - per));
        for (int i = 0; i < per && shopScroll + i < n; i++)
        {
            string id = order.At(shopScroll + i).Str(); JNode it = table.Get(id);
            float y = 140 + i * 128;
            bool own = owned.Contains(id), on = U.Eq(eq, id);
            Ui.Fill(40, y, 1200, 118, on ? new Color(0.95f, 0.72f, 0.25f, 0.22f) : new Color(0, 0, 0, 0.35f));
            Ui.Img("Art/items/" + shopTab + "_" + id, 50, y + 4, 110, 110);
            Ui.TextS(it.S("name"), 176, y + 6, 500, 36, 24, Ui.GOLD, TextAnchor.MiddleLeft);
            Ui.Text(it.S("desc"), 176, y + 44, 640, 70, 16, Ui.CREAM);
            string lockMsg = own ? null : Progress.GearLock(it);
            int price = it.Ik("price");
            if (on) Ui.TextS("КИІЛГЕН ✔", 1000, y + 30, 220, 56, 20, new Color(0.5f, 0.9f, 0.5f), TextAnchor.MiddleCenter);
            else if (own) { if (Ui.Btn("КИЮ", 1010, y + 30, 210, 56, true)) Equip(id); }
            else if (lockMsg != null) Ui.TextS(lockMsg, 880, y + 20, 340, 76, 16, new Color(1, 0.6f, 0.5f), TextAnchor.MiddleCenter);
            else if (Ui.Btn("САТЫП АЛУ · " + price + " ₸", 960, y + 30, 260, 56, true, Game.S.coins >= price, 18))
            {
                Game.S.coins -= price; owned.Add(id); Equip(id); Toast(it.S("name") + " — сенікі!");
            }
        }
        if (Ui.Btn("▲", 1180, 80, 60, 48, false, shopScroll > 0)) shopScroll--;
        if (Ui.Btn("▼", 1110, 80, 60, 48, false, shopScroll < n - per)) shopScroll++;
        if (Ui.Btn("◀ ЛОББИ", 30, 662, 180, 48, false, true, 18)) Go(LOBBY);
    }
    void Equip(string id)
    {
        if (shopTab == "weapon") Game.S.eqWeapon = id; else if (shopTab == "armor") Game.S.eqArmor = id; else Game.S.eqHelm = id;
        Game.Persist();
    }

    // ================= БАТЫРЛАР =================
    int heroScroll;
    void DrawHeroes()
    {
        Ui.Fill(0, 0, 1280, 720, new Color32(26, 30, 44, 255));
        Ui.TopBar("БАТЫРЛАР");
        int n = Db.HeroOrder.Count(), per = 8;
        heroScroll = Mathf.Clamp(heroScroll, 0, Mathf.Max(0, n - per));
        for (int i = 0; i < per && heroScroll + i < n; i++)
        {
            string id = Db.HeroOrder.At(heroScroll + i).Str(); JNode h = Db.Hero(id);
            float x = 40 + (i % 4) * 300, y = 84 + (i / 4) * 284;
            bool own = Game.S.heroes.Contains(id), on = U.Eq(Game.S.hero, id);
            Ui.Fill(x, y, 290, 274, on ? new Color(0.95f, 0.72f, 0.25f, 0.25f) : new Color(0, 0, 0, 0.35f));
            Ui.Img("Art/portraits/" + id, x + 75, y + 6, 140, 140);
            Ui.TextS(Game.Fix(h.S("name")), x, y + 148, 290, 30, 20, Ui.GOLD, TextAnchor.MiddleCenter);
            Ui.Text(h.S("kind") + " · " + h.S("ability"), x + 6, y + 178, 278, 40, 14, Ui.CREAM, TextAnchor.UpperCenter);
            int price = h.Ik("price"), lvl = h.Get("lvl").I(1);
            if (on) Ui.TextS("ТАҢДАЛҒАН ✔", x, y + 222, 290, 44, 18, new Color(0.5f, 0.9f, 0.5f), TextAnchor.MiddleCenter);
            else if (own) { if (Ui.Btn("ТАҢДАУ", x + 45, y + 222, 200, 44, true)) { Game.S.hero = id; Game.Persist(); } }
            else if (Progress.Level() < lvl) Ui.TextS(lvl + "-деңгей", x, y + 222, 290, 44, 17, new Color(1, 0.6f, 0.5f), TextAnchor.MiddleCenter);
            else if (Ui.Btn(price + " ₸", x + 45, y + 222, 200, 44, true, Game.S.coins >= price))
            {
                Game.S.coins -= price; Game.S.heroes.Add(id); Game.S.hero = id; Game.Persist(); Toast(Game.Fix(h.S("name")) + " сенің қосыныңда!");
            }
        }
        if (Ui.Btn("◀", 1100, 662, 60, 48, false, heroScroll > 0)) heroScroll -= per;
        if (Ui.Btn("▶", 1170, 662, 60, 48, false, heroScroll < n - per)) heroScroll += per;
        if (Ui.Btn("◀ ЛОББИ", 30, 662, 180, 48, false, true, 18)) Go(LOBBY);
    }
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
