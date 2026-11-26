import java.awt.*;
import java.awt.image.*;
import java.io.*;
import java.nio.file.*;
import javax.imageio.*;
// Призрак: бір әдістің кадрлары (SkArt + жауынгерлер) — суретті тексеру үшін
public class SkShot {
  public static void main(String[] a) throws Exception {
    Db.Init(new Ghost.FileSrc()); Pz.Init(); Game.Load(new Ghost.MemStore());
    Looks.Init(JNode.Parse(Files.readString(Path.of(Ghost.DATA + "meta_parts.json"))));
    String id = a[0]; String out = a[1]; int cols = 4; String[] ts = a[2].split(",");
    U.Seed(3); Match m = new Match(); m.StartTrain(0); m.phase = Match.FIGHT; m.player.active = true; m.enemy.active = true;
    m.enemy.ai = false; m.player.ai = false; m.player.x = 620; m.enemy.x = a.length > 3 ? Float.parseFloat(a[3]) : 900; m.player.facing = 1; m.enemy.facing = -1; m.cam = 300;
    Skills.TestUnlock = true; m.player.rux = 100;
    int rows = (ts.length + cols - 1) / cols; int W = 640, H = 360;
    BufferedImage sheet = new BufferedImage(W * cols, H * rows, BufferedImage.TYPE_INT_ARGB);
    Graphics2D sg = sheet.createGraphics();
    float tNow = 0; int idx = 0; boolean cast = false;
    for (String tsI : ts) {
      float target = Float.parseFloat(tsI);
      while (tNow < target) {
        if (!cast && tNow >= 0.2f) { cast = true; SkDef dd = Skills.By(id); if (dd.air) { m.player.y = Db.GROUND - 260; m.player.vy = -200; m.player.state = Fighter.JUMP; } Skills.Start(m.player, dd, false); }
        m.Update(1f / 60, null); m.fx.Clear(); tNow += 1f / 60;
      }
      BufferedImage im = new BufferedImage(1280, 720, BufferedImage.TYPE_INT_ARGB); Graphics2D g = im.createGraphics();
      g.setPaint(new GradientPaint(0, 0, new Color(120, 120, 170), 0, 650, new Color(230, 170, 140))); g.fillRect(0, 0, 1280, 720);
      g.setColor(new Color(92, 78, 52)); g.fillRect(0, (int) Db.GROUND, 1280, 80);
      g.translate(-m.cam, 0);
      VMesh back = new VMesh(), add = new VMesh(), front = new VMesh(), fa = new VMesh();
      SkArt.Spikes(back, add, m);
      for (int i = 0; i < m.skProj.Count(); i++) { SkProj p = m.skProj.Get(i); if (U.Eq(p.kind, "earth")) SkArt.EarthWave(back, add, p, tNow); if (U.Eq(p.kind, "ice")) SkArt.IceLance(front, fa, p, tNow); }
      for (Fighter f : new Fighter[] { m.enemy, m.player }) { SkArt.Frost(back, add, f, tNow); SkArt.AuraBack(back, add, f, tNow); }
      for (int i = 0; i < m.skProj.Count(); i++) { SkProj p = m.skProj.Get(i); if (U.Eq(p.kind, "wind")) SkArt.Tornado(front, fa, p, tNow); if (U.Eq(p.kind, "lasso")) SkArt.Lasso(front, p, tNow); }
      for (int i = 0; i < m.arrows.Count(); i++) { float[] ar = m.arrows.Get(i); if (ar[4] <= 0) SkArt.ArrowTrail(fa, ar, tNow); }
      Render.draw(g, back); Render.draw(g, add);
      for (Fighter f : new Fighter[] { m.enemy, m.player }) { VMesh v = new VMesh(); Painter.Live = true; Painter.Draw(v, f, Looks.For(f), 0, 0, 0); Render.draw(g, v); }
      for (Fighter f : new Fighter[] { m.enemy, m.player }) { SkArt.IceBlock(front, fa, f, tNow); SkArt.AuraFront(front, fa, f, tNow); }
      Render.draw(g, front); Render.draw(g, fa);
      g.dispose();
      sg.drawImage(im.getSubimage(160, 180, 960, 540).getScaledInstance(W, H, Image.SCALE_SMOOTH), (idx % cols) * W, (idx / cols) * H, null);
      sg.setColor(Color.white); sg.drawString(id + " t=" + tsI, (idx % cols) * W + 8, (idx / cols) * H + 16);
      idx++;
    }
    ImageIO.write(sheet, "png", new File(out));
  }
}
