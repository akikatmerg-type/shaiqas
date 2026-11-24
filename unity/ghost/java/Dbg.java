public class Dbg {
  public static void main(String[] a) throws Exception {
    Db.Init(new Ghost.FileSrc()); Pz.Init(); Game.Load(new Ghost.MemStore());
    int pw = 0, N = 200; float dP = 0, dE = 0;
    for (int k = 0; k < N; k++) {
      U.Seed(100 + k);
      Match m = new Match(); m.mode = "quick"; m.chapter = 0; m.needWins = 1; m.StartMatch();
      // айна: екі жақ та бірдей
      Fighter p = m.player, e = m.MakePlayer(1150, "sharua"); e.ai = true; e.facing = -1; p.ai = true;
      m.enemy = e; p.opp = e; e.opp = p;
      int g = 0; while (m.phase != Match.MATCHEND && g < 60 * 200) { m.Update(1f / 60, null); m.fx.Clear(); g++; }
      if (m.won) pw++; dP += 110 - e.hp; dE += 110 - p.hp;
    }
    System.out.println("mirror: player wins " + pw + "/" + N + "  avg dmg dealt P=" + dP / N + " E=" + dE / N);
  }
}
