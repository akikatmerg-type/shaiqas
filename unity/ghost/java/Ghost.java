import java.nio.file.*;
import java.util.*;
public class Ghost {
  static class FileSrc implements ISource { public String Load(String n) { try { return Files.readString(Path.of(Ghost.DATA + n + ".json")); } catch (Exception e) { return null; } } }
  static class MemStore implements IStore { Map<String,String> m = new HashMap<>(); public String Get(String k) { return m.get(k); } public void Set(String k, String v) { m.put(k, v); } }
  static int fails = 0;
  static final String RES = System.getProperty("res", "../SHAIQAS/Assets/Resources/"), DATA = RES + "Data/", ART = RES + "Art/";
  static void check(boolean ok, String what) { if (!ok) { fails++; System.out.println("  FAIL: " + what); } }
  public static void main(String[] a) throws Exception {
    U.Seed(7);
    Db.Init(new FileSrc()); Pz.Init();
    MemStore store = new MemStore(); Game.Load(store);
    System.out.println("[data] chapters=" + Db.NCH + " heroes=" + Db.HeroOrder.Count() + " moves=" + Db.Moves.Count() + " poses=" + Db.Poses.Count());
    // --- прогресс және викторина құлпы ---
    check(Progress.Unlocked(0) && !Progress.Unlocked(1), "басында тек I тарау ашық");
    Game.S.prog[0] = 4; check(!Progress.Unlocked(1) && Progress.QuizGate(1), "викторинасыз II тарау жабық");
    Quiz q = Quiz.Start(0); check(q.Total() == 8, "викторинада 8 сұрақ");
    for (int i = 0; i < q.Total(); i++) { q.Pick(q.RightIndex()); q.Next(); }
    check(q.done && q.score == 8 && q.reward == 200 && Progress.Unlocked(1), "викторина 8/8 → II ашық, +200");
    Game.S.name = "Әлия"; check(Game.Fix("Жарайсың, Бауыржан!").equals("Жарайсың, Әлия!"), "ойыншы аты ауыстырылады");
    // --- сюжет ---
    int pages = 0;
    for (int c = 0; c < Db.NCH; c++) for (int s = 0; s < 4; s++) { Lst<Page> L = Story.BeforeFight(c, s); pages += L.Count(); check(L.Count() > 0, "беттер ch" + c + " s" + s); }
    Lst<Page> d = Story.BeforeFight(0, 1); Page dp = null; for (int i = 0; i < d.Count(); i++) if (d.Get(i).dlg != null) dp = d.Get(i);
    check(dp != null, "2-ұрыс алдында диалог бар");
    int c0 = Game.S.coins; Story.Answer(dp, dp.dlg.Ik("ok")); check(Game.S.coins == c0 + 15 && Story.AnswerOk(dp), "дұрыс жауап +15");
    System.out.println("[story] ұрыс алдындағы беттер барлығы=" + pages + ", эпилог=" + Story.EpilogueList().Count());
    // --- сақтау: жазу/оқу ---
    Game.Persist(); SaveData back = SaveData.FromJson(store.Get(Game.KEY), Db.NCH);
    check(back.coins == Game.S.coins && back.prog[0] == 4 && back.quizBest[0] == 8 && back.name.equals("Әлия"), "сақтау қайта оқылады");
    // --- ұрыстар: ЖИ ↔ ЖИ ---
    int pw = 0, n = 0; long frames = 0; float avgT = 0;
    for (int ch = 0; ch < Db.NCH; ch++) for (int si = 0; si < 4; si++) {
      Match m = new Match(); m.StartCampaign(ch, si);
      m.player.ai = true;  // призрак: ойыншыны да ЖИ басқарады
      int guard = 0;
      while (m.phase != Match.MATCHEND && guard < 60 * 60 * 6) {
        m.Update(1f / 60, null); guard++; m.fx.Clear();
        if (m.phase == Match.FIGHT && m.player.ai == true && m.round == 1 && guard == 1) {}
        if (Float.isNaN(m.player.x) || Float.isNaN(m.enemy.cur[0]) || Float.isNaN(m.player.cur[11])) { check(false, "NaN ch" + ch + " s" + si); break; }
      }
      frames += guard; n++; if (m.won) pw++; avgT += m.matchT;
      check(m.phase == Match.MATCHEND, "ұрыс аяқталды ch" + ch + " s" + si + " (раунд " + m.round + ")");
      if (si == 3) check(m.needWins == 3, "бас жау 3 раунд");
    }
    System.out.println("[fight] " + n + " ұрыс, ойыншы(ЖИ) жеңді " + pw + ", орташа ұзақтығы " + String.format("%.1f", avgT / n) + " с, кадр " + frames);
    // --- геометрия: позалар ақылға қонымды ---
    Geo g = new Geo(); g.Build(Pz.Get("idle"));
    System.out.println(String.format("[rig] idle: head C=(%.1f,%.1f) hand=(%.1f,%.1f) footF=(%.1f,%.1f)", g.Cx, g.Cy, g.hFx, g.hFy, g.legF.fx, g.legF.fy));
    check(g.Cy < -150 && g.Cy > -260, "бас биіктігі");
    System.out.println(fails == 0 ? "GHOST OK ✔" : "GHOST FAILS: " + fails);
  }
}
