// SHAIQAS · Үстіңгі терезе: ДЕҢГЕЙЛЕР тізімі (веб: LV — деңгей белгісін басқанда).
using UnityEngine;

public partial class GameRoot
{
    // ================= ДЕҢГЕЙЛЕР (веб: lvOpen / lvDraw / lvUnlocks / lvReward) =================
    bool lvOn; float lvT0; int lvSel;
    readonly SList slLv = new SList { x = 168, y = 190, w = 944, h = 392, rowH = 104, gap = 8, pad = 4, top = true };
    static readonly string[] LV_SCREENS = { LOBBY, SHOP, HEROES, MAP, GUIDE, BRACKET, DUELSEL };

    bool LvBadgeScreen() { for (int i = 0; i < LV_SCREENS.Length; i++) if (screen == LV_SCREENS[i]) return true; return false; }
    Rect LvRect() { return screen == LOBBY ? new Rect(24, 12, 300, 56) : new Rect(192, 10, 236, 54); }

    /// <summary>OnGUI басында: деңгей белгісін басу → тізімді ашу; ашық болса — астындағы экран басылмайды.</summary>
    void OverlaysPre()
    {
        if (lvOn && !LvBadgeScreen()) lvOn = false;
        Event e = Event.current;
        if (!lvOn && LvBadgeScreen() && !Tut.Active() && e.type == EventType.MouseDown)
        {
            Vector2 p = Ui.ToVirtual(e.mousePosition);
            if (LvRect().Contains(p)) { LvOpen(); e.Use(); }
        }
        if (lvOn)
        {
            Ui.blockInput = true;
            // пернелерді осында оқып, «жұтамыз» — астындағы экран (мәзір, баптаулар) оларды өңдемесін
            if (e.type == EventType.Layout)
            {
                ovEsc = Inp.Tap(Inp.ESC); ovOk = Inp.Tap(Inp.ENTER) || Inp.Tap(Inp.SPACE);
                ovDir = (Inp.Tap(Inp.S) || Inp.Tap(Inp.DOWN) ? 1 : 0) - (Inp.Tap(Inp.W) || Inp.Tap(Inp.UP) ? 1 : 0);
                int[] eat = { Inp.ESC, Inp.ENTER, Inp.SPACE, Inp.W, Inp.S, Inp.UP, Inp.DOWN, Inp.A, Inp.D, Inp.LEFT, Inp.RIGHT, Inp.J };
                for (int i = 0; i < eat.Length; i++) Inp.Eat(eat[i]);
            }
        }
    }
    bool ovEsc, ovOk; int ovDir;
    void OverlaysPost()
    {
        if (lvOn) LvDraw();
    }

    void LvOpen()
    {
        lvOn = true; lvT0 = Time.unscaledTime; lvSel = Mathf.Min(Progress.MAXL - 1, Progress.Level() - 1);
        slLv.ty = slLv.sy = 0; SlEnsure(slLv, lvSel, Progress.MAXL); slLv.sy = slLv.ty;
        Snd.Play("ui_select");
    }
    static int LvTotal(int L) { int s = 0; for (int i = 1; i < L; i++) s += Progress.Need(i); return s; }
    static string TgS(int v) { string s = U.Int(v); if (v >= 1000) s = U.Sub(s, 0, U.Len(s) - 3) + " " + U.Sub(s, U.Len(s) - 3, 3); return s + " ₸"; }
    /// <summary>Осы деңгейде ашылатындар: әдістер, қару-жарақ, батырлар.</summary>
    static Lst<string> LvUnlocks(int L, Lst<string> kinds)
    {
        Lst<string> o = new Lst<string>(); Skills.Init();
        for (int i = 0; i < Skills.All.Count(); i++) { SkDef d = Skills.All.Get(i); if (d.lvl == L) { o.Add("Әдіс: " + d.name); kinds.Add("skill"); } }
        string[] cat = { "weapon", "armor", "helm" }, lab = { "Қару", "Сауыт", "Бас киім" };
        JNode[] T = { Db.Weapons, Db.Armors, Db.Helms }, O = { Db.WeaponOrder, Db.ArmorOrder, Db.HelmOrder };
        for (int c = 0; c < 3; c++)
            for (int i = 0; i < O[c].Count(); i++)
            {
                JNode it = T[c].Get(O[c].At(i).Str());
                if (it.IsNull() || it.Ik("lvl") != L || it.Ik("price") <= 0 || it.Bk("prize")) continue;
                o.Add(lab[c] + ": " + it.S("name") + " (" + TgS(it.Ik("price")) + ")"); kinds.Add(cat[c]);
            }
        for (int i = 0; i < Db.HeroOrder.Count(); i++)
        {
            JNode h = Db.Heroes.Get(Db.HeroOrder.At(i).Str());
            if (!h.IsNull() && h.Ik("lvl") == L && h.Ik("price") > 0) { o.Add("Батыр: " + h.S("name") + " (" + TgS(h.Ik("price")) + ")"); kinds.Add("hero"); }
        }
        return o;
    }
    static Color LvKindCol(string k)
    {
        if (k == "skill") return new Color32(138, 208, 255, 255);
        if (k == "weapon") return new Color32(255, 211, 107, 255);
        if (k == "armor") return new Color32(200, 212, 224, 255);
        if (k == "helm") return new Color32(255, 176, 112, 255);
        return new Color32(255, 138, 122, 255);
    }

    void LvDraw()
    {
        int n = Progress.MAXL;
        // пернелер (веб: lvKeys)
        if (Event.current.type == EventType.Layout)
        {
            if (ovEsc || ovOk) { ovEsc = ovOk = false; lvOn = false; Snd.Play("ui_back"); return; }
            if (ovDir != 0) { lvSel = Mathf.Clamp(lvSel + ovDir, 0, n - 1); SlEnsure(slLv, lvSel, n); Snd.Play("ui_move", 0.5f); ovDir = 0; }
        }
        float k = Mathf.Clamp01((Time.unscaledTime - lvT0) / 0.3f); float kb = EaseBackG(k);
        int cur = Progress.Level(), xp = Game.S.xp; float t = Time.unscaledTime;
        Ui.Fill(0, 0, 1280, 720, new Color(0.03f, 0.02f, 0.06f, 0.66f * k));
        Color o = GUI.color; GUI.color = new Color(1, 1, 1, Mathf.Clamp01(k * 1.5f));
        float px = 140, py = 40 + (1 - kb) * 24, pw = 1000, ph = 640;
        Ui.DzPanel(px, py, pw, ph, "felt", true);
        Ui.Ribbon(640, py + 48, 380, "ДЕҢГЕЙЛЕР", 30);
        Ui.Text("Сен қазір " + cur + "-деңгейдесің · жалпы тәжірибе: " + xp, px, py + 80, pw, 24, 18, new Color32(255, 227, 160, 255), TextAnchor.MiddleCenter, true);
        Ui.Text("Тәжірибе жинау: кез келген ұрыста жеңу (бас жау мен қиын ұрыс — көбірек) · әр әдісті алғаш рет орындау +20", px + 30, py + 106, pw - 60, 22, 13, new Color(1, 0.9f, 0.75f, 0.72f), TextAnchor.MiddleCenter, true);
        slLv.y = py + 150;
        int clicked = SlInput(slLv, n);
        if (clicked >= 0) { lvSel = clicked; Snd.Play("ui_move", 0.5f); }
        SlBack(slLv);
        Ui.BeginClip(slLv.x, slLv.y, slLv.w, slLv.h);
        for (int i = 0; i < n; i++)
        {
            Rect r = SlRow(slLv, i); float rx = r.x, ry = r.y, rw = r.width, rh = r.height;
            if (ry + rh < slLv.y - 4 || ry > slLv.y + slLv.h + 4) continue;
            int N = i + 1; bool done = N <= cur, isCur = N == cur, isNext = N == cur + 1, sel = lvSel == i;
            Ui.Fill(rx, ry, rw, rh, isCur ? new Color(0.95f, 0.72f, 0.25f, 0.2f) : done ? new Color(0.24f, 0.47f, 0.24f, 0.18f) : isNext ? new Color(1, 0.84f, 0.47f, 0.1f) : new Color(0, 0, 0, 0.3f));
            Ui.Frame(rx, ry, rw, rh, sel ? new Color(1, 0.91f, 0.69f, 0.7f + 0.3f * Mathf.Sin(t * 5)) : isCur ? new Color(1, 0.84f, 0.47f, 0.8f) : new Color(0.96f, 0.77f, 0.36f, 0.28f), sel ? 2.4f : 1.4f);
            float cx = rx + 50, cy = ry + rh / 2;
            Ui.Circle(cx, cy, 30, done ? (Color)Ui.GOLD : new Color32(90, 80, 72, 255));
            Ui.Circle(cx, cy, 26, done ? new Color32(122, 22, 22, 255) : new Color32(36, 32, 30, 255));
            Ui.Circle(cx - 4, cy - 5, 18, done ? new Color32(170, 44, 34, 255) : new Color32(58, 52, 48, 255));
            Ui.TextO(U.Int(N), cx - 30, cy - 20, 60, 40, 26, done ? new Color32(255, 214, 107, 255) : new Color32(168, 154, 138, 255), TextAnchor.MiddleCenter, true, 2);
            string st = isCur ? "ҚАЗІРГІ ДЕҢГЕЙ" : done ? "АШЫЛДЫ ✓" : isNext ? "КЕЛЕСІ ДЕҢГЕЙ" : "ЖАБЫҚ";
            Ui.TextO(N + "-деңгей", rx + 96, ry + 10, 130, 28, 22, done ? new Color32(255, 233, 176, 255) : new Color32(232, 220, 203, 255), TextAnchor.MiddleLeft, true, 1.5f);
            Ui.Text(st, rx + 226, ry + 12, 180, 24, 13, isCur ? new Color32(255, 211, 107, 255) : done ? new Color32(155, 224, 138, 255) : isNext ? new Color32(255, 227, 160, 255) : new Color32(168, 154, 138, 255), TextAnchor.MiddleLeft, true);
            string req;
            if (N == 1) req = "Ойынды бастағанда беріледі";
            else { int need = LvTotal(N), left = Mathf.Max(0, need - xp); req = done ? "Жетті: " + need + " тәжірибе жиналды" : "Жету үшін: жалпы " + need + " тәжірибе (" + (N - 1) + "-деңгейден кейін тағы " + Progress.Need(N - 1) + ") · қалды: " + left; }
            Ui.Text(req, rx + 96, ry + 38, rw - 290, 20, 13, new Color(1, 0.9f, 0.75f, 0.8f), TextAnchor.MiddleLeft, true);
            // ашылатындар
            Lst<string> kinds = new Lst<string>(); Lst<string> un = LvUnlocks(N, kinds);
            float ux = rx + 96, uy = ry + 62, umax = rx + rw - 190;
            if (un.Count() == 0) Ui.Text(N == 1 ? "Бастапқы жабдық, " + Db.Hero("sharua").S("name") + " және алғашқы әдіс" : "Жаңа зат жоқ — тек теңге сыйлығы", ux, uy, rw - 300, 18, 13, new Color(1, 0.9f, 0.75f, 0.55f), TextAnchor.MiddleLeft, true);
            for (int j = 0; j < un.Count(); j++)
            {
                string s = un.Get(j); float w = Ui.TextW(s, 13) + 24;
                if (ux + w > umax)
                {
                    if (uy > ry + 70) { Ui.Text("+" + (un.Count() - j) + " тағы", ux, uy, 120, 18, 13, new Color32(255, 211, 160, 255), TextAnchor.MiddleLeft, true); break; }
                    ux = rx + 96; uy += 20;
                }
                Ui.Circle(ux + 6, uy + 9, 5, LvKindCol(kinds.Get(j)));
                Ui.Text(s, ux + 16, uy, w, 18, 13, done ? new Color32(246, 234, 210, 255) : new Color(0.96f, 0.92f, 0.82f, 0.7f), TextAnchor.MiddleLeft, true);
                ux += w + 6;
            }
            // теңге сыйлығы
            int rew = Progress.LvReward(N); float bx = rx + rw - 170;
            if (rew > 0)
            {
                Ui.Fill(bx, ry + 16, 150, 60, done ? new Color(0.16f, 0.35f, 0.16f, 0.5f) : new Color(0, 0, 0, 0.4f));
                Ui.Frame(bx, ry + 16, 150, 60, new Color(1, 0.84f, 0.47f, 0.5f), 1.2f);
                Ui.Text("СЫЙЛЫҚ", bx, ry + 20, 150, 16, 11, new Color(1, 0.9f, 0.75f, 0.65f), TextAnchor.MiddleCenter, true);
                Ui.TextO("+" + TgS(rew), bx, ry + 38, 150, 30, 22, new Color32(255, 214, 107, 255), TextAnchor.MiddleCenter, true, 2);
                if (done && N <= Game.S.lvlPaid) Ui.Text("алынды ✓", bx, ry + 78, 150, 16, 11, new Color32(155, 224, 138, 255), TextAnchor.MiddleCenter, true);
            }
        }
        Ui.EndClip();
        SlFront(slLv, n);
        Ui.blockInput = false;
        if (Ui.DzBtn("ЖАБУ", 640 - 160, py + ph - 76, 320, 52, true)) { lvOn = false; Snd.Play("ui_back"); }
        if (!Application.isMobilePlatform) Ui.Text("↑↓ — жылжу   ·   Esc — жабу", px, py + ph - 22, pw, 18, 13, new Color(1, 0.9f, 0.75f, 0.55f), TextAnchor.MiddleCenter, true);
        // панельден тыс басу — жабу
        Event e = Event.current;
        if (e.type == EventType.MouseDown && !new Rect(px, py, pw, ph).Contains(Ui.ToVirtual(e.mousePosition))) { lvOn = false; Snd.Play("ui_back"); e.Use(); }
        GUI.color = o;
    }
    static float EaseBackG(float k) { float c1 = 1.70158f, c3 = c1 + 1; return 1 + c3 * Mathf.Pow(k - 1, 3) + c1 * Mathf.Pow(k - 1, 2); }
}
