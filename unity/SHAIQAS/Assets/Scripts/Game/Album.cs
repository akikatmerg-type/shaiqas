// SHAIQAS · Тарих альбомы (веб: drawAlbum, drawAlbumCard, histIcon, ALB_TL, ALB_CARD):
// жоғарыда 9 тараудың уақыт жолы, ортада таңдалған тараудың 4 картасы, ашылған картаның үлкен беті.
using UnityEngine;

public partial class GameRoot
{
    int albumCh; float albumOt;
    void AlbumEnter()
    {
        int c = 0; for (int i = 0; i < Db.NCH; i++) if (Game.S.prog[i] > 0) c = i;
        albumCh = c; albumSel = -1;
    }
    static Rect ACard(int j) { return new Rect(74 + j * 288, 236, 268, 360); }
    static float ATlX(int ch) { return 120 + ch * (1040f / Mathf.Max(1, Db.NCH - 1)); }

    void DrawAlbum()
    {
        bool ru = Game.S.ru; int n = Db.Hist.Count();
        albumOt += Time.unscaledDeltaTime;
        if (Event.current.type == EventType.Layout)
        {
            if (Inp.Tap(Inp.ESC)) { if (albumSel >= 0) { albumSel = -1; Snd.Play("ui_back"); } else { Snd.Play("ui_back"); Go(LOBBY); } return; }
            if (albumSel < 0)
            {
                if (Inp.Tap(Inp.LEFT) || Inp.Tap(Inp.A)) { albumCh = (albumCh + Db.NCH - 1) % Db.NCH; pt = 0; Snd.Play("ui_move"); }
                if (Inp.Tap(Inp.RIGHT) || Inp.Tap(Inp.D)) { albumCh = (albumCh + 1) % Db.NCH; pt = 0; Snd.Play("ui_move"); }
            }
        }
        JNode C = Db.Ch(albumCh);
        Ui.Img("Art/campwide/thumb_" + C.S("stage"), 0, 0, 1280, 720);
        Ui.Fill(0, 0, 1280, 720, new Color32(14, 10, 30, 199));
        Ui.TopBar("ТАРИХ АЛЬБОМЫ");
        Ui.TextO("Ашылған карталар: " + Medals.CardsOpen() + " / " + n, 1280 - 24 - 400, 202, 400, 24, 17, new Color32(255, 227, 160, 255), TextAnchor.MiddleRight, true, 2);
        // уақыт жолы
        float y0 = 150; int last = 0;
        for (int i = 0; i < Db.NCH; i++) { if (Progress.Unlocked(i) && Game.S.prog[i] < 4) { last = i; break; } else if (Game.S.prog[i] >= 4) last = Mathf.Min(Db.NCH - 1, i + 1); }
        Ui.Fill(100, y0 - 2, 1080, 4, new Color32(233, 173, 60, 153));
        for (int ch = 0; ch < Db.NCH; ch++)
        {
            float x = ATlX(ch); JNode Cc = Db.Ch(ch); bool on = ch == albumCh;
            bool hv = albumSel < 0 && Ui.Hot(x - 64, y0 - 40, 128, 90);
            Ui.Circle(x, y0, on ? 17 : 13, new Color32(255, 227, 160, 255));
            Ui.Circle(x, y0, on ? 15 : 11, on ? (Color)new Color32(233, 173, 60, 255) : hv ? new Color32(138, 106, 154, 255) : new Color32(58, 48, 96, 255));
            Ui.Text(Cc.S("roman"), x - 20, y0 - 10, 40, 20, on ? 15 : 12, on ? (Color)new Color32(42, 18, 5, 255) : new Color32(255, 227, 160, 255), TextAnchor.MiddleCenter, true);
            Ui.Text(Cc.S("years"), x - 64, y0 - 36, 128, 18, 13, on ? (Color)new Color32(255, 227, 160, 255) : new Color32(184, 168, 200, 255), TextAnchor.MiddleCenter, true);
            string nm = Cc.S("name");
            Ui.Text(nm, x - 64, y0 + 24, 128, 20, Ui.Fit(nm, 124, 15), on ? (Color)new Color32(255, 243, 220, 255) : new Color32(168, 152, 184, 255), TextAnchor.MiddleCenter, true);
            if (ch == last) Ui.Text("сен осындасың ▼", x - 70, y0 - 60, 140, 16, 12, new Color32(157, 255, 176, 255), TextAnchor.MiddleCenter, true);
            if (albumSel < 0 && Ui.Click(x - 64, y0 - 40, 128, 90) && albumCh != ch) { albumCh = ch; pt = 0; Snd.Play("ui_move"); }
        }
        // карталар
        for (int j = 0; j < 4; j++)
        {
            int i = albumCh * 4 + j; if (i >= n) break;
            Rect r = ACard(j); bool open = Medals.CardOpen(i); JNode c = Db.Hist.At(i), rc = Db.RuCards.At(i);
            bool hv = albumSel < 0 && Ui.Hot(r.x, r.y, r.width, r.height);
            float k = Mathf.Clamp01((pt - j * 0.07f) / 0.35f); k = 1 - (1 - k) * (1 - k) * (1 - k);
            float x = r.x, y = r.y + (1 - k) * 30 + (hv && open ? -6 : 0), w = r.width, h = r.height;
            Color o = GUI.color; GUI.color = new Color(1, 1, 1, k);
            Ui.DzPanel(x, y, w, h, open ? "parch" : "felt");
            Ui.Img("Art/icons/hist" + i + (open ? "" : "_locked"), x + w / 2 - 80, y + 98 - 80, 160, 160);
            if (open)
            {
                string tt = ru && rc.IsArr() ? rc.At(0).Str() : c.S("title");
                Ui.Text(tt, x + 10, y + 186, w - 20, 28, Ui.Fit(tt, w - 30, 24), new Color32(58, 26, 10, 255), TextAnchor.MiddleCenter, true);
                Ui.Text(c.S("date"), x + 10, y + 218, w - 20, 20, 15, new Color32(138, 26, 18, 255), TextAnchor.MiddleCenter, true);
                string f0 = ru && rc.IsArr() ? rc.At(1).At(0).Str() : c.Get("facts").At(0).Str();
                Ui.Text(f0, x + 17, y + 246, w - 34, 84, 15, new Color32(42, 26, 16, 255), TextAnchor.UpperCenter, true);
                Ui.Text("Толығырақ ▸", x, y + h - 32, w, 22, 15, new Color32(138, 90, 42, 255), TextAnchor.MiddleCenter, true);
            }
            else
            {
                Ui.TextO("Жабық карта", x, y + 196, w, 28, 22, new Color32(255, 227, 160, 255), TextAnchor.MiddleCenter, true, 2);
                string nd = C.Get("nodes").At(j).S("n");
                Ui.Text("«" + nd + "» ұрысында жеңсең ашылады", x + 20, y + 232, w - 40, 60, 15, new Color32(200, 184, 216, 255), TextAnchor.UpperCenter, true);
            }
            GUI.color = o;
            if (albumSel < 0 && Ui.Click(r.x, r.y, r.width, r.height))
            {
                if (open) { albumSel = i; albumOt = 0; Snd.Play("ui_select"); }
                else { Snd.Play("ui_error"); Toast("Бұл картаны ашу үшін сол ұрыста жең"); }
            }
        }
        // викторина жолағы
        bool qo = Progress.TestUnlockAll || Game.S.prog[albumCh] >= 4; int best = Game.S.quizBest[albumCh], qn = Mathf.Min(Db.QZ_N, Db.QPool.At(albumCh).Count());
        T(qo ? "Викторина: ең жақсы нәтиже " + best + " / " + qn : "Викторина тарау соңында ашылады (бас жауды жең)", 74, 640, 17, qo ? H("#ffe3a0") : H("#a898b8"), true);
        int stars = best >= qn ? 3 : best >= Progress.QzPassMin() ? 2 : best > 0 ? 1 : 0;
        for (int s2 = 0; s2 < 3; s2++) Ui.Img("Art/camp/star_" + (s2 < stars ? 1 : 0), 80 + s2 * 26 - 11, 668 - 11, 22, 22);
        if (albumSel < 0)
        {
            if (Ui.DzBtn("МӘЗІР", 24, 18, 150, 46, false, true, null, true)) { Go(LOBBY); return; }
            if (Ui.DzBtn("МАРАПАТТАР", 620, 626, 300, 58)) { medalsFrom = ALBUM; Snd.Play("ui_select"); Go(MEDALS); return; }
            if (Ui.DzBtn("ВИКТОРИНА", 1280 - 330, 626, 300, 58, qo, true))
            {
                if (qo) StartQuiz(albumCh, false); else { Snd.Play("ui_error"); Toast("Алдымен тараудың бас жауын жең"); }
            }
        }
        else AlbumCard(albumSel, ru);
    }

    void AlbumCard(int i, bool ru)
    {
        JNode c = Db.Hist.At(i), rc = Db.RuCards.At(i);
        float kk = Mathf.Clamp01(albumOt / 0.3f); kk = 1 + 2.7f * Mathf.Pow(kk - 1, 3) + 1.7f * Mathf.Pow(kk - 1, 2);
        Ui.Fill(0, 0, 1280, 720, new Color32(8, 5, 16, 184));
        float s = 0.85f + 0.15f * kk, x = 640 + (190 - 640) * s, y = 360 + (96 - 360) * s, w = 900 * s, h = 520 * s;
        Color o = GUI.color; GUI.color = new Color(1, 1, 1, Mathf.Clamp01(albumOt * 5));
        Ui.DzPanel(x, y, w, h, "parch", true);
        float u = s;
        Ui.Img("Art/icons/hist" + i, x + (190 - 130 - 18) * u, y + (210 - 130 - 18) * u, 296 * u, 296 * u);
        Ui.Text(Db.ChName(i / 4) + " · " + c.S("date"), x + 40 * u, y + 366 * u, 300 * u, 22 * u, 16, new Color32(138, 26, 18, 255), TextAnchor.MiddleCenter, true);
        string tt = ru && rc.IsArr() ? rc.At(0).Str() : c.S("title");
        Ui.TextO(tt, x + 360 * u, y + 62 * u, 500 * u, 50 * u, Ui.Fit(tt, 500, 40), Ui.GOLD, TextAnchor.MiddleLeft, true, 3);
        JNode facts = ru && rc.IsArr() ? rc.At(1) : c.Get("facts");
        float yy = y + 128 * u;
        for (int k = 0; k < facts.Count(); k++)
        {
            string f = facts.At(k).Str(); float th = Ui.TextHeight(f, 480 * u, 22, true);
            Ui.Circle(x + 368 * u, yy + 14, 5, new Color32(192, 138, 42, 255));
            Ui.Text(f, x + 384 * u, yy, 480 * u, th + 6, 22, new Color32(42, 26, 16, 255), TextAnchor.UpperLeft, true);
            yy += th + 14;
        }
        string fun = ru && rc.IsArr() ? rc.At(2).Str() : c.S("fun");
        if (U.Len(fun) > 0)
        {
            Ui.DzPanel(x + 352 * u, yy + 6, 512 * u, 116 * u, "felt");
            Ui.Text(ru ? "Знаешь ли ты?" : "Білесің бе?", x + 374 * u, yy + 18, 300, 26, 19, new Color32(255, 211, 107, 255), TextAnchor.MiddleLeft, true);
            Ui.Text(fun, x + 374 * u, yy + 48, 470 * u, 70 * u, 18, new Color32(255, 243, 220, 255), TextAnchor.UpperLeft, true);
        }
        GUI.color = o;
        int s0 = albumCh * 4; Lst<int> L = new Lst<int>(); for (int j = 0; j < 4; j++) if (Medals.CardOpen(s0 + j)) L.Add(s0 + j);
        int p = 0; for (int j = 0; j < L.Count(); j++) if (L.Get(j) == i) p = j;
        if (Ui.DzBtn("ЖАБУ", 190 + 900 - 170, 96 + 520 - 66, 150, 46, true, true, null, true)) { albumSel = -1; Snd.Play("ui_back"); return; }
        if (L.Count() > 1)
        {
            if (Ui.DzBtn("◀ АЛДЫҢҒЫ", 190 + 20, 96 + 520 - 66, 150, 46, false, true, null, true)) { albumSel = L.Get((p - 1 + L.Count()) % L.Count()); albumOt = 0; Snd.Play("ui_move"); }
            if (Ui.DzBtn("КЕЛЕСІ ▶", 190 + 180, 96 + 520 - 66, 150, 46, false, true, null, true)) { albumSel = L.Get((p + 1) % L.Count()); albumOt = 0; Snd.Play("ui_move"); }
        }
    }
}
