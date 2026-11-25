import java.nio.file.*;
import java.util.*;
public class Ghost {
  static class FileSrc implements ISource { public String Load(String n) { try { return Files.readString(Path.of(Ghost.DATA + n + ".json")); } catch (Exception e) { return null; } } }
  static class MemStore implements IStore { Map<String,String> m = new HashMap<>(); public String Get(String k) { return m.get(k); } public void Set(String k, String v) { m.put(k, v); } }
  static int fails = 0;
  static final String RES = System.getProperty("res", "../SHAIQAS/Assets/Resources/"), DATA = RES + "Data/", ART = RES + "Art/";
  static void check(boolean ok, String what) { if (!ok) { fails++; System.out.println("  FAIL: " + what); } }
  /** Ұрысты соңына дейін ойнату: win=true — жау әр раундта әлсіз (ойыншы жеңеді), false — керісінше. */
  static void playOut(Match m, boolean win) {
    m.player.ai = true; int guard = 0;
    while (m.phase != Match.MATCHEND && guard < 60 * 60 * 10) {
      if (m.phase == Match.INTRO) { Fighter loseF = win ? m.enemy : m.player; loseF.hp = loseF.hpLag = 1; loseF.dmgMul = 0; m.player.ai = true; }
      m.Update(1f / 60, null); guard++; m.fx.Clear();
    }
    check(m.phase == Match.MATCHEND, "режим ұрысы аяқталды " + m.mode);
  }
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
    // --- режимдер: турнир, төзім, бастар, жекпе-жек ---
    Progress.TestUnlockAll = true;
    Match t = new Match(); t.StartMode("tour", 2);
    check(t.run != null && t.run.slots.Count() == 8 && t.run.slots.Get(7).boss, "турнир торы: 8 орын, соңғысы бас жау");
    for (int st = 0; st < 3; st++) {
      t.NextFight(); playOut(t, true);
      check(t.won, "турнир " + st + " жеңіс");
      String nx = t.After("next");
      if (st < 2) check(nx.equals("bracket") && t.run.stage == st + 1 && t.run.lv.Get(t.run.stage).length == (st == 0 ? 4 : 2), "турнир келесі кезең " + st);
    }
    check(t.run.champ && Game.S.tourW[2][t.run.tier] == 1, "турнир чемпионы");
    Match sv = new Match(); sv.StartMode("surv", 1); playOut(sv, true); check(sv.run.wave == 1 && sv.After("next").equals("fight"), "төзім: 1 толқын → келесі");
    playOut(sv, true); check(sv.run.wave == 2 && sv.enemy.maxHp > 0, "төзім: 2 толқын");
    sv.After("next"); playOut(sv, false); check(!sv.won && Game.S.surv[1] == 2 && sv.runOver, "төзім: жеңіліс → рекорд 2");
    Match bs = new Match(); bs.StartMode("boss", 3); int nb = bs.run.list.Count();
    check(nb == 4, "бастар: 4 бас жау (" + nb + ")");
    for (int i = 0; i < nb; i++) { check(bs.enemy.bossAb != null, "бастар: қабілет " + i); playOut(bs, true); String nx = bs.After("next"); if (i < nb - 1) check(nx.equals("fight") && bs.run.i == i + 1, "бастар келесі " + i); }
    check(bs.runOver && Game.S.bossRec == 4, "бастар аяқталды");
    int r0 = Game.S.rating; Match du = new Match(); du.duelIdx = 2; du.StartMode("duel", 0); playOut(du, true);
    check(Game.S.rating == r0 + 25 && du.After("map").equals("duelsel"), "жекпе-жек: рейтинг +25");
    Match tr = new Match(); tr.StartMode("train", 0); check(tr.enemy.name.equals("Жаттықтырушы"), "жаттығу: жаттықтырушы");
    // бас жау қабілеттері: әр қабілетті ЖИ ↔ ЖИ ұрысында тексеру
    { Match em = new Match(); em.StartMode("boss", 8); Fighter E = em.enemy; E.bossAb = "evade"; E.abT = 0; E.active = true; int ev = 0;
      for (int i = 0; i < 40; i++) { E.Set(Fighter.IDLE); E.inv = 0; E.abT = 0; float h0 = E.hp; Move mv = Move.Base("l1"); mv.dmg = 1; E.TakeHit(mv, 1, true, em.player); if (E.hp == h0 && E.state == Fighter.DASH) ev++; E.hp = E.maxHp; }
      check(ev > 8 && ev < 32, "жалтару ~45%: " + ev + "/40"); }
    String[] abs = {"arrow","horse","blizzard","twin","sand"};
    for (String ab : abs) {
      Match bm = new Match(); bm.StartMode("boss", 8); bm.enemy.bossAb = ab; bm.enemy.abT = 0.5f; bm.player.ai = true;
      int fired = 0, g2 = 0;
      while (bm.phase != Match.MATCHEND && g2 < 60 * 40) { bm.Update(1f / 60, null); g2++; bm.fx.Clear();
        if (bm.arrows.Count() > 0 || bm.enemy.charging || bm.weatherT > 0 || bm.enemy.twinT > 0 || bm.enemy.inv > 0.3f) fired++;
        if (Float.isNaN(bm.player.x) || Float.isNaN(bm.enemy.x)) { check(false, "NaN қабілет " + ab); break; } }
      if (fired == 0) System.out.println("   dbg " + ab + " hitsTaken=" + bm.enemy.hitsTaken + " abT=" + bm.enemy.abT + " phase=" + bm.phase + " pAI=" + bm.player.ai + " frames=" + g2);
      check(fired > 0, "қабілет іске асты: " + ab);
    }
    Progress.TestUnlockAll = false;
    { DiffInfo d0 = Eco.Stage(0, 0), d9 = Eco.Stage(8, 3);
      check(d0.label.equals("ҚАЛЫПТЫ") && d9.stars >= 4, "қиындық: I.1 жеңіл (" + d0.label + "), IX.4 қиын (" + d9.label + ")");
      check(Eco.Kit(20) > Eco.Kit(1) && Eco.GearLevel(Eco.Kit(5)) > 4.9f && Eco.GearLevel(Eco.Kit(5)) < 5.1f, "жабдық деңгейі кері функция");
      System.out.println("[eco] I.1: " + d0.label + " · IX.4: " + d9.label + " · кеңес: " + Eco.Tip(8, 3)); }
    System.out.println("[modes] турнир/төзім/бастар/жекпе-жек/жаттығу тексерілді");
    // --- геометрия: позалар ақылға қонымды ---
    Geo g = new Geo(); g.Build(Pz.Get("idle"));
    System.out.println(String.format("[rig] idle: head C=(%.1f,%.1f) hand=(%.1f,%.1f) footF=(%.1f,%.1f)", g.Cx, g.Cy, g.hFx, g.hFy, g.legF.fx, g.legF.fy));
    check(g.Cy < -150 && g.Cy > -260, "бас биіктігі");
    System.out.println(fails == 0 ? "GHOST OK ✔" : "GHOST FAILS: " + fails);
  }
}
