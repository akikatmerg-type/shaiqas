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
    // --- әдістер: тізбекті тану және әр әдістің әсері (манекенге қарсы) ---
    { Skills.Init(); Skills.TestUnlock = true; int okSeq = 0, okFx = 0; StringBuilder bad = new StringBuilder();
      for (int i = 0; i < Skills.All.Count(); i++) {
        SkDef sk = Skills.All.Get(i);
        // тізбекті енгізу: әр қадам 0.08 с
        SkBuf B = new SkBuf(); float tt = 10;
        for (int k = 0; k < sk.seq.length; k++) {
          String qq = sk.seq[k]; boolean btn = qq.equals("J") || qq.equals("K") || qq.equals("L");
          if (btn) Skills.Feed(B, tt, "N", qq.equals("J"), qq.equals("K"), qq.equals("L")); else { Skills.Feed(B, tt, qq, false, false, false); tt += 0.04f; Skills.Feed(B, tt, "N", false, false, false); }
          tt += 0.08f;
        }
        if (Skills.SeqMatch(B, sk.seq) >= 0) okSeq++; else bad.append(" seq:" + sk.id);
        // әсер
        Match m = new Match(); m.StartTrain(0); m.phase = Match.FIGHT; m.player.active = true; m.enemy.active = true;
        Fighter P = m.player, E = m.enemy; E.ai = false; E.x = P.x + 160; E.facing = -1; P.facing = 1; P.rux = 100; P.hp = P.maxHp * 0.5f;
        if (sk.air) { P.y = Db.GROUND - 200; P.vy = -100; P.state = Fighter.JUMP; }
        if (sk.id.equals("qalqan")) { Skills.Start(P, sk, false); Move hit = Move.Base("l1"); E.TakeHit(hit, 1, true, P); P.TakeHit(hit, -1, true, E); }
        else Skills.Start(P, sk, false);
        float e0 = E.hp, p0 = P.hp; boolean eff = false;
        for (int f = 0; f < 240; f++) { m.Update(1f / 60, null); m.fx.Clear(); if (m.phase != Match.FIGHT) break;
          if (E.hp < e0 - 0.5f || P.hp > p0 + 0.5f || Skills.S(P).cloneT > 0 || Skills.S(P).parry > 0 || m.weatherT > 0) eff = true;
          if (Float.isNaN(P.x) || Float.isNaN(E.x)) { check(false, "NaN әдіс " + sk.id); break; } }
        if (sk.id.equals("aruaq")) { P.StartAttack("l1", 0); for (int f = 0; f < 60; f++) { m.Update(1f / 60, null); m.fx.Clear(); } eff = eff || E.hp < e0; }
        if (eff) okFx++; else bad.append(" fx:" + sk.id);
      }
      check(okSeq == 18 && okFx == 18, "әдістер: тізбек " + okSeq + "/18, әсер " + okFx + "/18" + bad);
      System.out.println("[skills] тізбек " + okSeq + "/18 · әсер " + okFx + "/18" + bad);
      Skills.TestUnlock = false; }
    { Match am = new Match(); am.StartMode("arena", 0);
      check(am.phase == Match.FIGHT && am.enemy.name.equals("Жаттығу серігі"), "алаң: бірден ұрыс, серік бар");
      Move big = Move.Base("l1"); big.dmg = 9999; am.player.TakeHit(big, -1, true, am.enemy);
      check(am.player.state != Fighter.DEAD && am.player.hp >= 1, "алаң: өлмейді (hp " + am.player.hp + ")");
      am.enemy.active = false; am.player.active = false; float h0 = am.player.hp;
      for (int f = 0; f < 240; f++) { am.Update(1f / 60, null); am.fx.Clear(); }
      check(am.player.hp > h0 + 10 && am.phase == Match.FIGHT && am.timer > 90, "алаң: денсаулық қалпына келеді, уақыт шексіз (" + h0 + "→" + am.player.hp + ")");
      System.out.println("[arena] өлмейді, қалпына келеді: " + h0 + " → " + am.player.hp); }
    { Game.S.kid = true; Match km = new Match(); km.StartQuick(2, 1);
      check(km.enemy.kid && Math.abs(km.enemy.dmgMul - 0.6f * 1f) < 0.5f && km.player.ruxMul > 1.3f, "балалар: жау әлсіз, рух тез");
      km.phase = Match.FIGHT; km.player.active = true; km.enemy.active = true;
      km.enemy.StartAttack("l1", 0); float kw = km.enemy.cm.w;
      Match nm = new Match(); Game.S.kid = false; nm.StartQuick(2, 1); nm.enemy.StartAttack("l1", 0);
      check(kw > nm.enemy.cm.w * 1.3f, "балалар: жау соққыға ұзақ дайындалады (" + nm.enemy.cm.w + "→" + kw + ")");
      Game.S.kid = true; km.enemy.Update(0.01f, null); check(km.KidWarn() > 0, "балалар: «!» белгісі көрінеді");
      Skills.TestUnlock = true; km.enemy.state = Fighter.IDLE; km.player.rux = 100; km.player.state = Fighter.IDLE;
      Intent ki = new Intent(); ki.u = true; km.Update(1f / 60, ki); km.fx.Clear();
      check(km.player.sk != null, "балалар: U — бір батырмамен әдіс");
      Skills.TestUnlock = false; Game.S.kid = false;
      System.out.println("[kid] жау соққысы " + nm.enemy.cm.w + "→" + kw + " с, U-әдіс: " + (km.player.sk != null ? km.player.sk.def.name : "жоқ")); }
    { U.Seed(5); Match cm = new Match(); cm.StartCampaign(2, 1); cm.player.ai = true; boolean okc = true; float maxd = 0;
      for (int f = 0; f < 60 * 40 && cm.phase != Match.MATCHEND; f++) { cm.Update(1f / 60, null); cm.fx.Clear();
        for (Fighter fq : new Fighter[] { cm.player, cm.enemy }) for (Chain c : new Chain[] { fq.belt1, fq.belt2, fq.hem }) { if (c == null) continue;
          for (int i = 0; i < c.n; i++) { if (Float.isNaN(c.x[i]) || Float.isNaN(c.y[i])) okc = false; maxd = Math.max(maxd, Math.abs(c.x[i] - fq.x)); } } }
      check(okc && maxd < 400, "мата: NaN жоқ, тізбек денеден алыс кетпейді (max " + (int) maxd + ")");
      System.out.println("[cloth] 40 с ұрыс: тізбектер тұрақты, ең алыс ауытқу " + (int) maxd + " px"); }
    { U.Seed(7); Match hm = new Match(); hm.StartCampaign(0, 0); hm.player.ai = true; hm.player.weapon = "naiza"; hm.player.hat = "altyn"; hm.player.Init();
      hm.player.hp = hm.player.hpLag = hm.player.maxHp; boolean okh = true; float md = 0;
      for (int f = 0; f < 60 * 20 && hm.phase != Match.MATCHEND; f++) { hm.Update(1f / 60, null); hm.fx.Clear();
        for (Fighter fq : new Fighter[] { hm.player, hm.enemy }) for (Chain c : new Chain[] { fq.plume, fq.hatc, fq.tas }) { if (c == null || !c.ok) continue;
          for (int i = 0; i < c.n; i++) { if (Float.isNaN(c.x[i]) || Float.isNaN(c.y[i])) okh = false; md = Math.max(md, Math.abs(c.x[i] - fq.x)); } } }
      boolean live3 = hm.player.plume != null && hm.player.plume.ok && hm.player.tas.ok && hm.enemy.hatc.ok;
      check(okh && md < 520 && live3, "үкі/сәлде/шашақ тізбектері: іске қосылды, NaN жоқ (max " + (int) md + ")");
      System.out.println("[tassel] үкі (" + Cloth.HatBase(hm.player.hat) + "), найза шашағы, сәлде ұшы (" + Cloth.HatBase(hm.enemy.hat) + "): тұрақты, ең алыс " + (int) md + " px"); }
    { Match km2 = new Match(); km2.StartQuick(0, 0); km2.phase = Match.FIGHT; km2.player.active = true; km2.enemy.active = true;
      Move kb = Move.Base("l1"); kb.dmg = 9999; km2.enemy.TakeHit(kb, -1, true, km2.player);
      boolean flew = km2.enemy.hatOff != null; float topY = 9999;
      for (int f = 0; f < 240; f++) { km2.Update(1f / 60, null); km2.fx.Clear(); if (km2.enemy.hatOff != null) topY = Math.min(topY, km2.enemy.hatOff[1]); }
      boolean landed = flew && km2.enemy.hatOff[7] > 0 && Math.abs(km2.enemy.hatOff[1] - (Db.GROUND - 19 * km2.enemy.sc)) < 1;
      check(flew && landed && topY < km2.enemy.y - 250, "нокаут: бас киім ұшып, жерге түседі");
      System.out.println("[hat] нокаутта бас киім ұшты (ең биік " + (int) topY + "), жерге түсті: " + landed); }
    { Match cr = new Match(); cr.StartQuick(0, 0); cr.phase = Match.FIGHT; cr.player.active = true; cr.enemy.active = true;
      Move kb2 = Move.Base("l1"); kb2.dmg = 9999; cr.enemy.TakeHit(kb2, -1, true, cr.player);
      float d0 = Math.abs(cr.enemy.x - cr.player.x); int f0 = cr.enemy.facing; boolean nan = false;
      for (int f = 0; f < 60 * 4; f++) { cr.Update(1f / 60, null); cr.fx.Clear(); if (Float.isNaN(cr.enemy.x) || Float.isNaN(cr.enemy.rot)) nan = true; }
      float d1 = Math.abs(cr.enemy.x - cr.player.x);
      check(!nan && cr.enemy.crawlFlip && cr.enemy.facing == -f0 && d1 > d0 + 40 && cr.enemy.rot * cr.enemy.facing != 0, "жеңілген: аударылып, жаудан алыстай жорғалайды");
      System.out.println("[crawl] жеңілген ішімен аударылды, " + (int) (d1 - d0) + " px жорғалап алыстады, бұрыш " + cr.enemy.rot); }
    System.out.println("[modes] турнир/төзім/бастар/жекпе-жек/жаттығу тексерілді");
    // --- геометрия: позалар ақылға қонымды ---
    Geo g = new Geo(); g.Build(Pz.Get("idle"));
    System.out.println(String.format("[rig] idle: head C=(%.1f,%.1f) hand=(%.1f,%.1f) footF=(%.1f,%.1f)", g.Cx, g.Cy, g.hFx, g.hFy, g.legF.fx, g.legF.fy));
    check(g.Cy < -150 && g.Cy > -260, "бас биіктігі");
    System.out.println(fails == 0 ? "GHOST OK ✔" : "GHOST FAILS: " + fails);
  }
}
