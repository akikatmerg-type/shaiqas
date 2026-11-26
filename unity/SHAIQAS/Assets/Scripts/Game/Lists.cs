// SHAIQAS · Батырлар мен Қару-жарақ экрандары (веб: drawHeroes, drawShop, SLIST — тік тізім):
// дөңгелекше/сүйреу/пернелермен айналатын тізім, оң жақта (немесе ортада) таңдалған заттың толық сипаты.
using UnityEngine;

public partial class GameRoot
{
    // ================= ТІК ТІЗІМ =================
    class SList { public float x, y, w, h, rowH, gap, pad; public float sy, ty, vel; public bool drag, barDrag, top; public float dragY0, dragS0, moved; }
    readonly SList slHero = new SList { x = 34, y = 98, w = 792, h = 586, rowH = 90, gap = 6, pad = 6 };
    readonly SList slShop = new SList { x = 868, y = 150, w = 390, h = 534, rowH = 82, gap = 6, pad = 6 };
    const float BAR_W = 14;
    static float Stride(SList L) { return L.rowH + L.gap; }
    static float SlMax(SList L, int n) { return Mathf.Max(0, L.pad * 2 + n * Stride(L) - L.gap - L.h); }
    static Rect SlRow(SList L, int i) { return new Rect(L.x + L.pad, L.y + L.pad + i * Stride(L) - L.sy, L.w - L.pad * 2 - BAR_W - 6, L.rowH); }
    static Rect SlBar(SList L) { return new Rect(L.x + L.w - L.pad - BAR_W, L.y + L.pad, BAR_W, L.h - L.pad * 2); }
    static void SlEnsure(SList L, int i, int n)
    {
        float top = i * Stride(L), bot = top + L.rowH + L.pad * 2;
        if (top < L.ty) L.ty = top; else if (bot > L.ty + L.h) L.ty = bot - L.h;
        L.ty = Mathf.Clamp(L.ty, 0, SlMax(L, n));
    }
    /// <summary>Тізім кадры: енгізу (дөңгелекше, сүйреу, жолақ), тегіс айналдыру. Басылған жол индексін қайтарады (−1 — жоқ).</summary>
    int SlInput(SList L, int n)
    {
        Event e = Event.current; int clicked = -1;
        Vector2 mp = new Vector2((e.mousePosition.x - Ui.ox) / Ui.k, (e.mousePosition.y - Ui.oy) / Ui.k);
        bool inside = mp.x >= L.x && mp.x <= L.x + L.w && mp.y >= L.y && mp.y <= L.y + L.h;
        Rect bar = SlBar(L); bool onBar = mp.x >= bar.x - 4 && mp.x <= bar.xMax + 4 && mp.y >= bar.y && mp.y <= bar.yMax;
        float m = SlMax(L, n);
        // үстіңгі терезе (деңгейлер/күй) ашық тұрғанда астындағы тізім айналмайды
        if (Ui.blockInput && !L.top && e.type != EventType.Repaint) return -1;
        if (e.type == EventType.ScrollWheel && inside) { L.ty = Mathf.Clamp(L.ty + e.delta.y * 30, 0, m); e.Use(); }
        else if (e.type == EventType.MouseDown && inside) { L.drag = true; L.barDrag = onBar; L.dragY0 = mp.y; L.dragS0 = L.ty; L.moved = 0; L.vel = 0; if (onBar) BarSet(L, mp.y, n); e.Use(); }
        else if (e.type == EventType.MouseDrag && L.drag)
        {
            if (L.barDrag) BarSet(L, mp.y, n);
            else { float d = mp.y - L.dragY0; L.moved = Mathf.Max(L.moved, Mathf.Abs(d)); float nt = Mathf.Clamp(L.dragS0 - d, 0, m); L.vel = (nt - L.ty) / Mathf.Max(0.001f, Time.unscaledDeltaTime); L.ty = nt; L.sy = nt; }
            e.Use();
        }
        else if (e.type == EventType.MouseUp && L.drag)
        {
            if (!L.barDrag && L.moved < 8) for (int i = 0; i < n; i++) { Rect r = SlRow(L, i); if (r.Contains(mp) && mp.y >= L.y && mp.y <= L.y + L.h) { clicked = i; break; } }
            L.drag = false; L.barDrag = false; e.Use();
        }
        if (e.type == EventType.Repaint)
        {
            float dt = Time.unscaledDeltaTime;
            if (!L.drag && Mathf.Abs(L.vel) > 20) { L.ty = Mathf.Clamp(L.ty + L.vel * dt, 0, m); L.vel *= Mathf.Pow(0.02f, dt); } else if (!L.drag) L.vel = 0;
            L.ty = Mathf.Clamp(L.ty, 0, m); L.sy += (L.ty - L.sy) * Mathf.Min(1, dt * 16); if (Mathf.Abs(L.ty - L.sy) < 0.4f) L.sy = L.ty;
        }
        return clicked;
    }
    static void BarSet(SList L, float y, int n)
    {
        Rect b = SlBar(L); float m = SlMax(L, n); if (m <= 0) return;
        float th = Mathf.Max(44, b.height * L.h / (L.h + m));
        L.ty = L.sy = Mathf.Clamp01((y - b.y - th / 2) / (b.height - th)) * m;
    }
    /// <summary>Тізім қорабының фоны мен жолағы (жолдарды салудан бұрын/кейін).</summary>
    static void SlBack(SList L) { Ui.Fill(L.x, L.y, L.w, L.h, new Color32(12, 8, 20, 158)); Ui.Frame(L.x, L.y, L.w, L.h, new Color32(244, 197, 92, 115), 1.5f); }
    static void SlFront(SList L, int n)
    {
        float m = SlMax(L, n);
        if (L.sy > 2) Ui.Grad3(L.x, L.y, L.w - BAR_W - 8, 26, new Color32(8, 5, 14, 217), new Color32(8, 5, 14, 100), new Color32(8, 5, 14, 0));
        if (L.sy < m - 2) Ui.Grad3(L.x, L.y + L.h - 26, L.w - BAR_W - 8, 26, new Color32(8, 5, 14, 0), new Color32(8, 5, 14, 100), new Color32(8, 5, 14, 217));
        Rect b = SlBar(L); Ui.Fill(b.x, b.y, b.width, b.height, new Color32(0, 0, 0, 115)); Ui.Frame(b.x, b.y, b.width, b.height, new Color32(244, 197, 92, 77), 1);
        if (m > 0)
        {
            float th = Mathf.Max(44, b.height * L.h / (L.h + m)), ty = b.y + (b.height - th) * (L.sy / m);
            Ui.Fill(b.x + 2, ty, b.width - 4, th, new Color32(233, 173, 60, 230));
            for (int k = -1; k <= 1; k++) Ui.Fill(b.x + 4, ty + th / 2 + k * 5 - 0.5f, b.width - 8, 1.5f, new Color32(60, 30, 6, 140));
        }
    }

    // ---------- ортақ кішкене бөліктер ----------
    static void CoinAt(float cx, float cy, float r) { Ui.Img("Art/icons/coin", cx - r * 1.8f, cy - r * 1.8f, r * 3.6f, r * 3.6f); }
    static string Tg(int v) { string s = U.Int(v); if (v >= 1000) s = U.Sub(s, 0, U.Len(s) - 3) + " " + U.Sub(s, U.Len(s) - 3, 3); return s + " ₸"; }
    /// <summary>Жол ішіндегі оң баған: күйі / бағасы / деңгей талабы (веб: slPriceCol).</summary>
    static void PriceCol(float xr, float y, bool own, bool cur, int price, int lvl, string curLbl)
    {
        bool lvOk = !(lvl > 1) || Progress.Level() >= lvl, can = lvOk && Game.S.coins >= price;
        if (cur)
        {
            float w = Ui.TextW(curLbl, 13) + 22;
            Ui.Fill(xr - w, y + 14, w, 22, new Color32(60, 130, 60, 235)); Ui.Frame(xr - w, y + 14, w, 22, new Color32(191, 232, 160, 255), 1.2f);
            Ui.Text(curLbl, xr - w, y + 14, w, 22, 13, new Color32(239, 255, 224, 255), TextAnchor.MiddleCenter, true);
        }
        else if (own) Ui.TextS("Сенікі ✓", xr - 200, y + 18, 200, 26, 16, new Color32(191, 232, 160, 255), TextAnchor.MiddleRight, true);
        else
        {
            string s = Tg(price); float w = Ui.TextW(s, 20);
            CoinAt(xr - w - 14, y + 26, 10);
            Ui.TextS(s, xr - w - 2, y + 14, w + 4, 26, 20, !lvOk ? (Color)new Color32(154, 140, 128, 255) : can ? new Color32(255, 211, 107, 255) : new Color32(224, 138, 122, 255), TextAnchor.MiddleRight, true);
        }
        if (!own && lvl > 1)
        {
            if (!lvOk) Ui.TextS(lvl + "-деңгей керек", xr - 220, y + 42, 220, 20, 13, new Color32(224, 144, 90, 255), TextAnchor.MiddleRight, true);
            else Ui.TextS(lvl + "-деңгей ✓", xr - 200, y + 42, 200, 20, 13, new Color32(168, 224, 112, 255), TextAnchor.MiddleRight, true);
        }
    }
    /// <summary>Көрсеткіш жолағы (веб: statBar): аты, 10 бөлікті алтын жолақ.</summary>
    static void StatBar(float x, float y, float w, float v, string label, bool dark)
    {
        Ui.Text(label, x, y - 3, 140, 20, 15, dark ? (Color)new Color32(58, 26, 8, 255) : new Color32(255, 243, 224, 255), TextAnchor.MiddleLeft, true);
        float bx = x + 140, bw = w - 140;
        Ui.Fill(bx, y + 1, bw, 11, dark ? new Color32(60, 30, 10, 64) : new Color32(0, 0, 0, 115));
        float k = Mathf.Clamp01(v);
        Ui.Fill(bx + 1, y + 2, (bw - 2) * k, 9, new Color32(242, 184, 64, 255));
        for (int i = 1; i < 10; i++) Ui.Fill(bx + bw * i / 10f, y + 2, 1.2f, 9, new Color32(0, 0, 0, 70));
    }

    // ================= БАТЫРЛАР =================
    string heroSel;
    void DrawHeroes()
    {
        JNode order = Db.HeroOrder; int n = order.Count();
        if (heroSel == null || Db.Heroes.Get(heroSel).IsNull()) { heroSel = Game.S.hero; SlEnsure(slHero, IndexOf(order, heroSel), n); slHero.sy = slHero.ty; }
        int si = IndexOf(order, heroSel);
        if (Event.current.type == EventType.Layout)
        {
            int i0 = si;
            if (Inp.Tap(Inp.UP) || Inp.Tap(Inp.W) || Inp.Tap(Inp.LEFT) || Inp.Tap(Inp.A)) si = Mathf.Max(0, si - 1);
            if (Inp.Tap(Inp.DOWN) || Inp.Tap(Inp.S) || Inp.Tap(Inp.RIGHT) || Inp.Tap(Inp.D)) si = Mathf.Min(n - 1, si + 1);
            if (si != i0) { heroSel = order.At(si).Str(); SlEnsure(slHero, si, n); Snd.Play("ui_move", 0.5f, 0); }
            if (Inp.Tap(Inp.ENTER) || Inp.Tap(Inp.SPACE)) HeroAction();
            if (Inp.Tap(Inp.ESC)) { Go(LOBBY); return; }
        }
        // фон: лобби сахнасы күңгірттеліп
        Ui.Img("Art/lobby/backdrop", 0, 0, 1280, 720);
        Ui.Fill(0, 0, 1280, 720, new Color32(12, 8, 24, 158));
        int clicked = SlInput(slHero, n);
        if (clicked >= 0) { heroSel = order.At(clicked).Str(); Snd.Play("ui_move", 0.5f, 0); }
        SlBack(slHero);
        Ui.BeginClip(slHero.x + 2, slHero.y + 2, slHero.w - 4, slHero.h - 4);
        for (int i = 0; i < n; i++)
        {
            Rect r = SlRow(slHero, i); if (r.yMax < slHero.y || r.y > slHero.y + slHero.h) continue;
            HeroRow(order.At(i).Str(), i, r.x, r.y, r.width, r.height);
        }
        Ui.EndClip();
        SlFront(slHero, n);
        // оң панель
        string id = heroSel; JNode hr = Db.Hero(id); float px = 840, pw = 396;
        float pk = Mathf.Clamp01((pt - 0.15f) / 0.45f); pk = 1 - (1 - pk) * (1 - pk) * (1 - pk);
        Color o = GUI.color; GUI.color = new Color(1, 1, 1, pk);
        float sh = (1 - pk) * 50;
        Ui.DzPanel(px + sh, 100, pw, 598, "felt", true);
        string hn = Game.Fix(hr.S("full")); if (U.Len(hn) == 0) hn = Game.Fix(hr.S("name"));
        TO(hn, px + sh + pw / 2, 156, Ui.Fit(hn, pw - 60, 32), Ui.GOLD);
        Ui.Fill(px + sh + 60, 167, pw - 120, 1.5f, Ui.GOLD); Ui.Img("Art/ui/ram", px + sh + pw / 2 - 14, 165, 28, 14);
        T(hr.S("kind") + " · " + hr.Get("lvl").I(1) + "-деңгей", px + sh + pw / 2, 196, 15, H("#c9b8b0"));
        Ui.ImgC("Art/camp/glow", px + sh + pw / 2 - 170, 300 - 130, 340, 260, new Color(1, 0.78f, 0.5f, 0.3f));
        float mx = px + sh + pw / 2 - 70;
        Ui.Img("Art/ui/medal_42_2f5f9a", mx - (92 + 20) * 1, 298 - (92 + 20), (92 + 20) * 2, (92 + 20) * 2);
        Ui.Portrait("Art/portraits/" + id, mx, 298, 92);
        Ui.Img("Art/camp/shadow", px + sh + pw - 80 - 46, 414 - 7, 92, 14);
        Ui.Img("Art/lobby/front_" + id, px + sh + pw - 80 - 84, 411 - 224, 168, 224);
        TO("Қабілеті: " + hr.S("ability"), px + sh + pw / 2, 444, 21, H("#ffd36b"));
        Wrap(hr.S("desc"), px + sh + pw / 2, 468, 356, 14, H("#e8dccb"), 64);
        JNode st = hr.Get("st"); string[] SN = { "Күш", "Жылдамдық", "Төзімділік", "Рух" };
        for (int i = 0; i < 4; i++) StatBar(px + sh + 34, 532 + i * 18, pw - 68, st.At(i).F(), SN[i], false);
        bool own = Game.S.heroes.Contains(id), cur = U.Eq(Game.S.hero, id); int price = hr.Ik("price"), lvl = hr.Get("lvl").I(1);
        bool lk = !own && Progress.Level() < lvl;
        if (!own)
        {
            string s2 = lk ? "Ашылу шарты: " + lvl + "-деңгей · " + Tg(price) : Game.S.coins >= price ? "Бағасы: " + Tg(price) + " — алуға болады!" : "Бағасы: " + Tg(price) + " · тағы " + Tg(price - Game.S.coins) + " жина";
            T(s2, px + sh + pw / 2, 616, Ui.Fit(s2, pw - 40, 14), lk ? H("#f0a070") : Game.S.coins >= price ? H("#a8e070") : H("#e8c8a0"));
        }
        GUI.color = o;
        string lbl = cur ? "ТАҢДАЛҒАН" : own ? "ТАҢДАУ" : lk ? lvl + "-ДЕҢГЕЙ КЕРЕК" : "САТЫП АЛУ · " + Tg(price);
        if (Ui.DzBtn(lbl, 858, 626, 360, 58, !cur && !lk, !cur && !lk)) HeroAction();
        Ui.TopBar("БАТЫРЛАР");
        if (Ui.DzBtn("МӘЗІР", 24, 18, 150, 46, false, true, null, true)) Go(LOBBY);
    }
    static int IndexOf(JNode order, string id) { for (int i = 0; i < order.Count(); i++) if (U.Eq(order.At(i).Str(), id)) return i; return 0; }
    void HeroAction()
    {
        string id = heroSel; JNode hr = Db.Hero(id); bool own = Game.S.heroes.Contains(id);
        int price = hr.Ik("price"), lvl = hr.Get("lvl").I(1);
        if (U.Eq(Game.S.hero, id)) return;
        if (own) { Game.S.hero = id; Game.Persist(); Snd.Play("ui_select"); Toast(Game.Fix(hr.S("name")) + " — таңдалды"); return; }
        if (Progress.Level() < lvl) { Snd.Play("ui_error"); Toast(lvl + "-деңгейге жет"); return; }
        if (Game.S.coins < price) { Snd.Play("ui_error"); Toast("Теңге жетпейді: тағы " + Tg(price - Game.S.coins)); return; }
        Game.S.coins -= price; Game.S.heroes.Add(id); Game.S.hero = id; Game.Persist(); Snd.Play("buy");
        Toast(Game.Fix(hr.S("name")) + " сенің қосыныңда!");
    }
    void HeroRow(string id, int i, float x, float y, float w, float h)
    {
        JNode hr = Db.Hero(id); bool sel = U.Eq(id, heroSel), own = Game.S.heroes.Contains(id), cur = U.Eq(Game.S.hero, id);
        int lvl = hr.Get("lvl").I(1), price = hr.Ik("price");
        bool lk = !own && Progress.Level() < lvl, afford = !own && !lk && Game.S.coins >= price; float T0 = Time.unscaledTime;
        float ek = Mathf.Clamp01((pt - Mathf.Min(i, 8) * 0.04f) / 0.4f); ek = 1 - (1 - ek) * (1 - ek) * (1 - ek);
        Color o = GUI.color; GUI.color = new Color(1, 1, 1, o.a * ek); x += (1 - ek) * -40;
        if (sel) Ui.Fill(x - 4, y - 4, w + 8, h + 8, new Color(1, 0.75f, 0.35f, 0.35f));
        Ui.DzPanel(x + 6, y + 6, w - 12, h - 12, sel ? "red" : "felt");
        if (lk) Ui.Fill(x, y, w, h, new Color32(30, 26, 36, 115));
        if (afford && !sel) Ui.Frame(x + 1.5f, y + 1.5f, w - 3, h - 3, new Color(1, 0.83f, 0.42f, 0.55f + 0.35f * Mathf.Sin(T0 * 3 + i)), 2.2f);
        float pr = 36, pcx = x + 14 + pr, pcy = y + h / 2;
        Ui.Circle(pcx, pcy, pr + 4, new Color32(42, 18, 5, 255)); Ui.Circle(pcx, pcy, pr + 2, Ui.GOLD); Ui.Circle(pcx, pcy, pr, new Color32(42, 18, 5, 255));
        Ui.Portrait("Art/portraits/" + id, pcx, pcy, pr);
        if (lk) Ui.Circle(pcx, pcy, pr, new Color32(14, 10, 24, 115));
        float nx = pcx + pr + 16, colW = w - (nx - x) - 150;
        string nm = Game.Fix(hr.S("name")); int fs = Ui.Fit(nm, colW, 24);
        if (sel) Ui.TextO(nm, nx, y + 10, colW, 30, fs, Ui.GOLD, TextAnchor.MiddleLeft, true, 2);
        else Ui.TextS(nm, nx, y + 10, colW, 30, fs, lk ? (Color)new Color32(207, 196, 184, 255) : new Color32(255, 243, 224, 255), TextAnchor.MiddleLeft, true);
        string kind = hr.S("kind"); float kw = Ui.TextW(kind, 13) + 16;
        Color kc = kind == "Батыр" || kind == "Ақын-батыр" || kind == "Батыр, шешен" ? new Color32(163, 34, 34, 217) : kind == "Қолбасшы" || kind == "Би" ? new Color32(47, 95, 154, 217) : kind == "Жауынгер" ? new Color32(90, 96, 110, 230) : new Color32(90, 70, 40, 230);
        Ui.Fill(nx, y + 41, kw, 19, kc); Ui.Text(kind, nx, y + 41, kw, 19, 13, new Color32(255, 243, 224, 255), TextAnchor.MiddleCenter, true);
        string ab = "Қабілеті: " + hr.S("ability");
        Ui.Text(ab, nx + kw + 10, y + 41, colW - kw - 10, 19, Ui.Fit(ab, colW - kw - 10, 14), lk ? (Color)new Color32(200, 176, 128, 255) : new Color32(255, 211, 107, 255), TextAnchor.MiddleLeft, true);
        JNode st = hr.Get("st"); string[] SN = { "Күш", "Жылдамдық", "Төзімділік", "Рух" }; float sw = Mathf.Min(70, (colW - 18) / 4);
        for (int k = 0; k < 4; k++)
        {
            float bx = nx + k * (sw + 6), by = y + 68;
            Ui.Text(SN[k], bx, by - 8, 90, 14, 10, new Color(0.91f, 0.84f, 0.72f, 0.75f), TextAnchor.MiddleLeft, true);
            Ui.Fill(bx, by + 8, sw, 5, new Color32(0, 0, 0, 115)); Ui.Fill(bx, by + 8, sw * Mathf.Clamp01(st.At(k).F()), 5, Ui.GOLD);
        }
        PriceCol(x + w - 14, y + 8, own, cur, price, lvl, "ТАҢДАЛҒАН");
        GUI.color = o;
    }

    // ================= ҚАРУ-ЖАРАҚ =================
    string shopSel;
    JNode ShopTable() { return shopTab == "weapon" ? Db.Weapons : shopTab == "armor" ? Db.Armors : Db.Helms; }
    JNode ShopOrder() { return shopTab == "weapon" ? Db.WeaponOrder : shopTab == "armor" ? Db.ArmorOrder : Db.HelmOrder; }
    Lst<string> ShopOwned() { return shopTab == "weapon" ? Game.S.ownWeapon : shopTab == "armor" ? Game.S.ownArmor : Game.S.ownHelm; }
    string ShopEq() { return shopTab == "weapon" ? Game.S.eqWeapon : shopTab == "armor" ? Game.S.eqArmor : Game.S.eqHelm; }
    static readonly string[] RAR_N = { "Қарапайым", "Сирек", "Асыл", "Аңыз" }, RAR_C = { "#c8b898", "#5aa8f0", "#c07af0", "#ffb52e" };
    static int Rar(JNode it) { int t = it.Get("tier").I(0); return t >= 5 ? 3 : t >= 4 ? 2 : t >= 2 ? 1 : 0; }

    void DrawShop()
    {
        JNode table = ShopTable(), order = ShopOrder(); int n = order.Count();
        if (shopSel == null || table.Get(shopSel).IsNull()) { shopSel = ShopEq(); slShop.ty = 0; SlEnsure(slShop, IndexOf(order, shopSel), n); slShop.sy = slShop.ty; }
        int si = IndexOf(order, shopSel);
        if (Event.current.type == EventType.Layout)
        {
            int i0 = si;
            if (Inp.Tap(Inp.UP) || Inp.Tap(Inp.W)) si = Mathf.Max(0, si - 1);
            if (Inp.Tap(Inp.DOWN) || Inp.Tap(Inp.S)) si = Mathf.Min(n - 1, si + 1);
            if (Inp.Tap(Inp.LEFT) || Inp.Tap(Inp.A)) { ShopTab(-1); return; }
            if (Inp.Tap(Inp.RIGHT) || Inp.Tap(Inp.D)) { ShopTab(1); return; }
            if (si != i0) { shopSel = order.At(si).Str(); SlEnsure(slShop, si, n); Snd.Play("ui_move", 0.5f, 0); }
            if (Inp.Tap(Inp.ENTER) || Inp.Tap(Inp.SPACE)) ShopAction();
            if (Inp.Tap(Inp.ESC)) { Go(LOBBY); return; }
        }
        Ui.Img("Art/lobby/yurt_in", 0, 0, 1280, 720);
        Ui.Fill(0, 0, 1280, 720, new Color32(14, 8, 20, 71));
        Ui.ImgC("Art/camp/glow", 260 - 300, 420 - 300, 600, 600, new Color(1, 0.78f, 0.47f, 0.18f));
        Ui.Img("Art/camp/shadow", 260 - 160, 652 - 28, 320, 56, 0.6f);
        Ui.Img("Art/lobby/front_" + Game.S.hero, 260 - 210, 652 - 560 + 20, 420, 560);
        // орама қағаз
        float sx = 470, sw = 390, pk = Mathf.Clamp01(pt / 0.45f); pk = 1 - (1 - pk) * (1 - pk) * (1 - pk);
        float shh = 516 * pk, sy = 114 + (516 - shh) / 2;
        Texture2D sc = Ui.Tex("Art/lobby/scroll");
        if (sc != null) GUI.DrawTextureWithTexCoords(Ui.R(sx, sy, sw, shh), sc, new Rect(0, (516 - shh) / 2 / 516f, 1, shh / 516f));
        Rod(sx, sw, sy - 18); Rod(sx, sw, sy + shh - 4);
        JNode it = table.Get(shopSel); Lst<string> owned = ShopOwned(); string eq = ShopEq();
        if (pk > 0.6f)
        {
            Color o = GUI.color; GUI.color = new Color(1, 1, 1, Mathf.Clamp01((pk - 0.6f) * 2.5f));
            string nm = it.S("name");
            Ui.Text(nm, sx, 162 - 34, sw, 44, Ui.Fit(nm, sw - 60, 36), new Color32(58, 26, 8, 255), TextAnchor.MiddleCenter, true);
            Ui.Fill(sx + 80, 174, sw - 160, 1.5f, new Color32(122, 74, 20, 153)); Ui.Img("Art/ui/ram", sx + sw / 2 - 12, 172, 24, 12, 0.9f);
            int ra = Rar(it); string rn = RAR_N[ra]; int stars = Mathf.Min(6, it.Get("tier").I(0) + 1);
            float rw = Ui.TextW(rn, 14) + 22 + 13 * stars;
            Ui.Fill(sx + sw / 2 - rw / 2, 200 - 12, rw, 22, H(RAR_C[ra])); Ui.Frame(sx + sw / 2 - rw / 2, 200 - 12, rw, 22, new Color32(40, 20, 8, 140), 1.2f);
            Ui.Text(rn + " " + new string('★', stars), sx + sw / 2 - rw / 2, 200 - 12, rw, 22, 14, new Color32(32, 18, 10, 255), TextAnchor.MiddleCenter, true);
            Ui.ImgC("Art/camp/glow", sx + sw / 2 - 150, 275 - 110, 300, 220, new Color(1, 0.97f, 0.86f, 0.9f));
            float bob = Mathf.Sin(Time.unscaledTime * 1000 / 700f) * 4;
            Ui.ImgFit("Art/items/" + shopTab + "_" + shopSel, sx + sw / 2 - 150, 210 + bob, 300, 150);
            Ui.Text(it.S("desc"), sx + 27, 384, 336, 60, 15, new Color32(58, 36, 16, 255), TextAnchor.UpperCenter);
            JNode cur = table.Get(eq); if (cur.IsNull()) cur = it;
            string lk = !owned.Contains(shopSel) && !it.Bk("prize") ? Progress.GearLock(it) : null;
            if (shopTab == "weapon")
            {
                ShopStat(sx + 30, 456, sw - 60, it.Fk("dmg"), cur.Fk("dmg"), 1.8f, "Зақым");
                ShopStat(sx + 30, 478, sw - 60, it.Fk("spd"), cur.Fk("spd"), 1.5f, "Жылдамдық");
                ShopStat(sx + 30, 500, sw - 60, it.Fk("len"), cur.Fk("len"), 170, "Қашықтық");
                string sp = "Ерекше қабілет: " + it.S("sname");
                Ui.Text(sp, sx, (lk != null ? 530 : 536) - 16, sw, 22, Ui.Fit(sp, sw - 50, lk != null ? 15 : 17), new Color32(138, 26, 18, 255), TextAnchor.MiddleCenter, true);
            }
            else
            {
                ShopStat(sx + 30, 472, sw - 60, it.Fk("def"), cur.Fk("def"), shopTab == "armor" ? 0.4f : 0.16f, "Қорғаныс");
                Ui.Text("Саған тиетін зақым −" + Mathf.RoundToInt(it.Fk("def") * 100) + "%", sx, (lk != null ? 516 : 526) - 16, sw, 22, 17, new Color32(138, 26, 18, 255), TextAnchor.MiddleCenter, true);
            }
            if (lk != null) { string s2 = "Ашылу шарты: " + lk; Ui.Text(s2, sx, 549 - 16, sw, 22, Ui.Fit(s2, sw - 50, 15), new Color32(163, 34, 26, 255), TextAnchor.MiddleCenter, true); }
            GUI.color = o;
        }
        // әрекет батырмасы (орама астында)
        {
            bool own = owned.Contains(shopSel), on = U.Eq(eq, shopSel); int price = it.Ik("price");
            string lk = own || it.Bk("prize") ? null : Progress.GearLock(it);
            string lbl = on ? "КИІЛГЕН" : own ? "КИЮ" : lk != null ? "ЖАБЫҚ" : "САТЫП АЛУ · " + Tg(price);
            if (Ui.DzBtn(lbl, sx + 32, 566, sw - 64, 56, !on && lk == null, !on && lk == null)) ShopAction();
        }
        // қойындылар + тізім
        string[] tabs = { "weapon", "armor", "helm" }, names = { "ҚАРУ", "САУЫТ", "БАС КИІМ" };
        for (int t = 0; t < 3; t++)
            if (Ui.DzBtn(names[t], 872 + t * 128, 104, 120, 38, false, true, null, true, true, false, shopTab == tabs[t]) && shopTab != tabs[t]) { shopTab = tabs[t]; shopSel = null; return; }
        int clicked = SlInput(slShop, n);
        if (clicked >= 0) { shopSel = order.At(clicked).Str(); Snd.Play("ui_move", 0.5f, 0); }
        SlBack(slShop);
        Ui.BeginClip(slShop.x + 2, slShop.y + 2, slShop.w - 4, slShop.h - 4);
        for (int i = 0; i < n; i++)
        {
            Rect r = SlRow(slShop, i); if (r.yMax < slShop.y || r.y > slShop.y + slShop.h) continue;
            ShopRow(order.At(i).Str(), i, r.x, r.y, r.width, r.height, table, owned, eq);
        }
        Ui.EndClip();
        SlFront(slShop, n);
        Ui.TopBar("ҚАРУ-ЖАРАҚ");
        if (Ui.DzBtn("МӘЗІР", 24, 18, 150, 46, false, true, null, true)) Go(LOBBY);
    }
    void ShopTab(int d)
    {
        string[] tabs = { "weapon", "armor", "helm" }; int k = 0; for (int i = 0; i < 3; i++) if (tabs[i] == shopTab) k = i;
        shopTab = tabs[(k + d + 3) % 3]; shopSel = null; Snd.Play("ui_move", 0.5f, 0);
    }
    void ShopAction()
    {
        JNode it = ShopTable().Get(shopSel); Lst<string> owned = ShopOwned();
        if (U.Eq(ShopEq(), shopSel)) return;
        if (owned.Contains(shopSel)) { Equip(shopSel); Snd.Play("ui_select"); Toast(it.S("name") + " — киілді"); return; }
        string lk = it.Bk("prize") ? null : Progress.GearLock(it);
        if (lk != null) { Snd.Play("ui_error"); Toast("Ашылу шарты: " + lk); return; }
        int price = it.Ik("price");
        if (Game.S.coins < price) { Snd.Play("ui_error"); Toast("Теңге жетпейді: тағы " + Tg(price - Game.S.coins)); return; }
        Game.S.coins -= price; owned.Add(shopSel); Equip(shopSel); Snd.Play("buy"); Toast(it.S("name") + " — сенікі!");
    }
    static void Rod(float sx, float sw, float y)
    {
        Ui.Grad3(sx - 8, y, sw + 16, 22, new Color32(138, 90, 48, 255), new Color32(90, 52, 24, 255), new Color32(46, 26, 10, 255));
        Ui.Fill(sx, y + 4, sw, 2, new Color32(255, 230, 190, 64));
        Ui.Fill(sx - 22, y - 3, 16, 28, Ui.GOLD); Ui.Frame(sx - 22, y - 3, 16, 28, new Color32(58, 30, 6, 255), 1.2f);
        Ui.Fill(sx + sw + 6, y - 3, 16, 28, Ui.GOLD); Ui.Frame(sx + sw + 6, y - 3, 16, 28, new Color32(58, 30, 6, 255), 1.2f);
    }
    static void ShopStat(float x, float y, float w, float v, float cur, float max, string label)
    {
        StatBar(x, y, w - 54, v / max, label, true);
        float bx = x + 140, bw = w - 54 - 140, cx = bx + 2 + Mathf.Clamp01(cur / max) * (bw - 4);
        Ui.Fill(cx - 1, y - 3, 2, 18, new Color32(60, 30, 10, 217));
        float d = v - cur, tx = x + w - 46;
        if (Mathf.Abs(d) < 1e-6f) { Ui.Text("=", tx, y - 4, 40, 22, 16, new Color32(122, 90, 58, 255), TextAnchor.MiddleCenter, true); return; }
        bool up = d > 0; Color c = up ? (Color)new Color32(47, 138, 42, 255) : new Color32(184, 38, 26, 255);
        string s = max < 1 ? Mathf.RoundToInt(Mathf.Abs(d) * 100) + "%" : max > 10 ? U.Int(Mathf.RoundToInt(Mathf.Abs(d))) : Mathf.RoundToInt(Mathf.Abs(d) * 100) + "%";
        Ui.Text((up ? "▲ " : "▼ ") + s, tx - 4, y - 4, 60, 22, 14, c, TextAnchor.MiddleLeft, true);
    }
    void ShopRow(string id, int i, float x, float y, float w, float h, JNode table, Lst<string> owned, string eq)
    {
        JNode it = table.Get(id); bool sel = U.Eq(id, shopSel), own = owned.Contains(id), on = U.Eq(eq, id);
        int price = it.Ik("price"), lvl = it.Get("lvl").I(1), ra = Rar(it);
        float ek = Mathf.Clamp01((pt - Mathf.Min(i, 8) * 0.04f) / 0.4f); ek = 1 - (1 - ek) * (1 - ek) * (1 - ek);
        Color o = GUI.color; GUI.color = new Color(1, 1, 1, o.a * ek); x += (1 - ek) * 40;
        if (sel) Ui.Fill(x - 4, y - 4, w + 8, h + 8, new Color(1, 0.75f, 0.35f, 0.35f));
        Ui.DzPanel(x + 6, y + 6, w - 12, h - 12, on ? "red" : "felt");
        Color rc = H(RAR_C[ra]); Ui.Frame(x + 1, y + 1, w - 2, h - 2, new Color(rc.r, rc.g, rc.b, sel ? 1 : 0.55f), sel ? 2.4f : 1.4f);
        Ui.Fill(x + 8, y + 8, 64, h - 16, new Color32(246, 234, 210, 240)); Ui.Frame(x + 8, y + 8, 64, h - 16, new Color32(120, 80, 30, 160), 1);
        Ui.ImgFit("Art/items/" + shopTab + "_" + id, x + 10, y + 10, 60, h - 20);
        float nx = x + 80;
        Ui.TextS(it.S("name"), nx, y + 6, w - 200, 26, Ui.Fit(it.S("name"), w - 200, 19), on ? Ui.GOLD : (Color)new Color32(255, 243, 224, 255), TextAnchor.MiddleLeft, true);
        Ui.Text(RAR_N[ra] + " " + new string('•', Mathf.Min(6, it.Get("tier").I(0) + 1)), nx, y + 30, 160, 18, 12, new Color(rc.r, rc.g, rc.b, 0.95f), TextAnchor.MiddleLeft, true);
        string meta = shopTab == "weapon" ? "Зақым " + Mathf.RoundToInt(it.Fk("dmg") * 100) + "% · Жылд. " + Mathf.RoundToInt(it.Fk("spd") * 100) + "%" : "Қорғаныс −" + Mathf.RoundToInt(it.Fk("def") * 100) + "%";
        Ui.Text(meta, nx, y + 50, 200, 18, 12, new Color(0.91f, 0.84f, 0.72f, 0.85f), TextAnchor.MiddleLeft, true);
        PriceCol(x + w - 30, y + 2, own, on, price, lvl, "КИІЛГЕН");   // панельдің бұрыш өрнегіне тимейді
        GUI.color = o;
    }
}
