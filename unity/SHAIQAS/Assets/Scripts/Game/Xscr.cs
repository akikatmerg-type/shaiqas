// SHAIQAS · Марапаттар мен Баптаулар экрандары (веб: XSCR.medals, XSCR.settings, xBack).
using UnityEngine;

public partial class GameRoot
{
    /// <summary>Веб: xBack — бірінші тараудың аренасы, үстінен қою күңгірт қабат.</summary>
    static void XBack()
    {
        Ui.Img("Art/campwide/thumb_" + Db.Ch(0).S("stage"), 0, 0, 1280, 720);
        Ui.Fill(0, 0, 1280, 720, new Color32(14, 10, 30, 184));
    }

    // ================= МАРАПАТТАР =================
    public string medalsFrom = LOBBY;
    void DrawMedals()
    {
        if (Event.current.type == EventType.Layout && Inp.Tap(Inp.ESC)) { MedalsBack(); return; }
        XBack();
        Ui.TopBar("МАРАПАТТАР");
        if (Ui.DzBtn(medalsFrom == ALBUM ? "АЛЬБОМ" : "МӘЗІР", 24, 18, 150, 46, false, true, null, true)) { MedalsBack(); return; }
        int n = Db.Medals.Count(), got = 0;
        for (int i = 0; i < n; i++) if (Medals.Has(Db.Medals.At(i).S("id"))) got++;
        Ui.TextO("Жиналды: " + got + " / " + n, 1280 - 24 - 400, 100, 400, 26, 18, new Color32(255, 227, 160, 255), TextAnchor.MiddleRight, true, 2);
        bool ru = Game.S.ru;
        for (int i = 0; i < n; i++)
        {
            JNode m = Db.Medals.At(i); string id = m.S("id"); bool has = Medals.Has(id);
            float x = 22 + (i % 5) * 248, y = 128 + (i / 5) * 114;
            float k = Mathf.Clamp01((pt - i * 0.02f) / 0.3f); k = 1 - (1 - k) * (1 - k) * (1 - k);
            Color o = GUI.color; GUI.color = new Color(1, 1, 1, k);
            Ui.DzPanel(x, y, 236, 104, has ? "parch" : "felt");
            Ui.Img("Art/icons/medal_" + id + (has ? "" : "_off"), x + 38 - 36, y + 46 - 36, 72, 72);
            JNode rm = Db.RuMed.Get(id);
            string nm = m.S("name"), ds = m.S("desc");
            bool hr = ru && rm.IsArr();
            Ui.Text(nm, x + 72, y + 22, 156, 24, Ui.Fit(nm, 156, 17), has ? (Color)new Color32(58, 26, 10, 255) : new Color32(255, 243, 220, 255), TextAnchor.MiddleLeft, true);
            Ui.Text(ds, x + 72, y + 46, 156, hr ? 34 : 50, 12, has ? (Color)new Color32(106, 74, 42, 255) : new Color32(184, 168, 200, 255), TextAnchor.UpperLeft, true);
            // орысша аударма — астында, кішірек (веб: RU_MED)
            if (hr) Ui.Text(rm.At(0).Str(), x + 72, y + 80, 156, 16, 11, has ? (Color)new Color32(138, 106, 74, 255) : new Color32(154, 138, 168, 255), TextAnchor.MiddleLeft, true);
            GUI.color = o;
        }
    }
    void MedalsBack() { Snd.Play("ui_back"); if (medalsFrom == ALBUM) { albumSel = -1; Go(ALBUM); } else Go(LOBBY); }

    // ================= БАПТАУЛАР =================
    void DrawSettings()
    {
        SaveData S = Game.S;
        if (Event.current.type == EventType.Layout && Inp.Tap(Inp.ESC)) { Snd.Play("ui_back"); Go(LOBBY); return; }
        XBack();
        Ui.TopBar("БАПТАУЛАР");
        if (Ui.DzBtn("МӘЗІР", 24, 18, 150, 46, false, true, null, true)) { Go(LOBBY); return; }
        // дыбыс
        Ui.DzPanel(80, 120, 560, 300, "felt", true);
        T("ДЫБЫС", 110, 176, 24, H("#ffd36b"), true);
        if (Ui.DzBtn(S.sound ? "ДЫБЫС ҚОСУЛЫ" : "ДЫБЫС ӨШІК", 110, 200, 230, 50, false, true, null, true, true, false, S.sound)) { S.sound = !S.sound; Game.Persist(); Snd.Play("ui_select"); }
        T("Дыбысты қосу/өшіру", 360, 232, 14, H("#b8a8c8"), true, false);
        T("Әуен", 180, 311, 18, H("#fff3dc"), true);
        SetBar(232, 296, 200, S.volMusic);
        T("Әсерлер", 180, 375, 18, H("#fff3dc"), true);
        SetBar(252, 360, 180, S.volSfx);
        if (Ui.DzBtn("−", 110, 280, 54, 46, false, true, null, true)) { S.volMusic = Mathf.Clamp01(Mathf.Round((S.volMusic - 0.1f) * 10) / 10f); Game.Persist(); }
        if (Ui.DzBtn("+", 500, 280, 54, 46, false, true, null, true)) { S.volMusic = Mathf.Clamp01(Mathf.Round((S.volMusic + 0.1f) * 10) / 10f); Game.Persist(); }
        if (Ui.DzBtn("−", 110, 344, 54, 46, false, true, null, true)) { S.volSfx = Mathf.Clamp01(Mathf.Round((S.volSfx - 0.1f) * 10) / 10f); Game.Persist(); Snd.Play("ui_move"); }
        if (Ui.DzBtn("+", 500, 344, 54, 46, false, true, null, true)) { S.volSfx = Mathf.Clamp01(Mathf.Round((S.volSfx + 0.1f) * 10) / 10f); Game.Persist(); Snd.Play("ui_move"); }
        // тіл
        Ui.DzPanel(670, 120, 550, 300, "felt", true);
        T("ТІЛ", 700, 176, 24, H("#ffd36b"), true);
        if (Ui.DzBtn("ҚАЗАҚША", 700, 200, 240, 50, false, true, null, true, true, false, !S.ru)) { S.ru = false; Game.Persist(); Snd.Play("ui_select"); }
        if (Ui.DzBtn("ҚАЗ + РУС", 950, 200, 240, 50, false, true, null, true, true, false, S.ru)) { S.ru = true; Game.Persist(); Snd.Play("ui_select"); }
        Ui.Text("«ҚАЗ + РУС» — сюжет, тарих карталары, викторина мен ойындардың астында орысша аударма шығады.", 700, 262, 500, 50, 14, H("#c8b8d8"), TextAnchor.UpperLeft, true);
        if (Ui.DzBtn("✎ АТЫМДЫ ӨЗГЕРТУ", 700, 330, 250, 50, false, true, null, true)) { Snd.Play("ui_select"); AskName(); return; }
        T("Атың:", 984, 350, 15, H("#b8a8c8"), true, false);
        string pn = Game.PlayerName();
        T(pn, 984, 376, Ui.Fit(pn, 200, 20), H("#ffd36b"), true);
        // балалар режимі
        Ui.DzPanel(80, 462, 1140, 190, "felt", true);
        T("БАЛАЛАР РЕЖИМІ", 110, 506, 24, H("#ffd36b"), true);
        if (Ui.DzBtn(S.kid ? "ҚОСУЛЫ ✓" : "ӨШІРУЛІ", 110, 530, 300, 54, false, true, null, true, true, false, S.kid)) { S.kid = !S.kid; Game.Persist(); Snd.Play("ui_select"); }
        if (Ui.DzBtn(S.tutOff ? "ҰСТАЗ: ӨШІК" : "ҰСТАЗ: ҚОСУЛЫ ✓", 110, 598, 300, 46, false, true, null, true, true, false, !S.tutOff))
        {
            S.tutOff = !S.tutOff; if (!S.tutOff) { S.tutDone.Clear(); Toast("Бұқар жырау қайтадан басынан үйретеді"); } else Toast("Ұстаздың сабақтары өшірілді");
            Game.Persist(); Snd.Play("ui_select");
        }
        Ui.Text("Кішкентай балаларға арналған: жау баяу әрі әлсіз соғады, викторинада 5 дұрыс жауап жеткілікті, рух тезірек толады.", 440, 514, 720, 120, 16, H("#fff3dc"), TextAnchor.UpperLeft, true);
    }
    /// <summary>Дыбыс деңгейі жолағы (веб: setBar).</summary>
    static void SetBar(float x, float y, float w, float v)
    {
        Ui.Fill(x, y, w, 16, new Color32(0, 0, 0, 115)); Ui.Frame(x, y, w, 16, new Color32(244, 197, 92, 100), 1);
        Ui.Grad3(x + 2, y + 2, (w - 4) * Mathf.Clamp01(v), 12, new Color32(255, 232, 150, 255), new Color32(242, 184, 64, 255), new Color32(168, 110, 30, 255));
        Ui.TextS(Mathf.RoundToInt(v * 100) + "%", x + w + 4, y - 4, 56, 24, 14, new Color32(255, 227, 160, 255), TextAnchor.MiddleLeft, true);
    }
}
