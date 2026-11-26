import java.awt.*;
import java.awt.geom.*;
import java.awt.image.*;
import java.io.*;
import java.nio.file.*;
import javax.imageio.*;
// Призрак: бет әлпеттері (норма, жыпылықтау, ашу, ауырсыну, нокаут, қуаныш) + жауынгер контурмен/контурсыз — ірі планда.
public class FaceShot {
  public static void main(String[] a) throws Exception {
    Db.Init(new Ghost.FileSrc()); Pz.Init(); Game.Load(new Ghost.MemStore());
    Looks.Init(JNode.Parse(Files.readString(Path.of(Ghost.DATA + "meta_parts.json"))));
    U.Seed(3); Match m = new Match(); m.StartCampaign(0, 1); m.player.ai = true;
    for (int i = 0; i < 150; i++) { m.Update(1f / 60, null); m.fx.Clear(); }
    Fighter f = m.player; Look L = Looks.For(f);
    String[] ex = { "norm", "blink", "rage", "pain", "ko", "joy" };
    int cell = 300;
    BufferedImage out = new BufferedImage(cell * 6, cell * 2, BufferedImage.TYPE_INT_ARGB);
    Graphics2D g = out.createGraphics();
    g.setColor(new Color(120, 96, 120)); g.fillRect(0, 0, out.getWidth(), out.getHeight());
    for (int k = 0; k < 6; k++) {
      f.state = k == 2 ? Fighter.ATTACK : k == 3 ? Fighter.HIT : k == 4 ? Fighter.DEAD : k == 5 ? Fighter.VICTORY : Fighter.IDLE;
      if (k == 2) { f.StartAttack("l1", 0); f.st = 0; }
      f.guard = false; f.at = k == 1 ? (0.98f - 0.0f) / 0.27f : 0.1f;
      VMesh v = new VMesh(); Painter.Live = true;
      Painter.Draw(v, f, L, 0, 0, 0);
      // бас ортасы — кадр ортасына, ×5
      Geo G = new Geo(); G.Build(f.cur);
      float hx = f.x + f.facing * G.Cx * f.sc, hy = f.y + G.Cy * f.sc;
      AffineTransform keep = g.getTransform();
      g.translate(k * cell + cell / 2, cell / 2); g.scale(5, 5); g.translate(-hx, -hy);
      Render.draw(g, v); g.setTransform(keep);
      g.setColor(Color.white); g.drawString(ex[k], k * cell + 8, 16);
    }
    // төменгі қатар: контурсыз / контурмен
    f.state = Fighter.IDLE; f.at = 0.1f; f.StartAttack("l1", 0); f.st = f.cm.w * 0.9f; f.state = Fighter.ATTACK;
    for (int k = 0; k < 2; k++) {
      VMesh v = new VMesh(); Painter.Live = true;
      if (k == 1) for (int d = 0; d < 8; d++) { double an = d * Math.PI * 2 / 8 + 0.39; Painter.Draw(v, f, L, Col.Pack(16, 9, 7, 235), (float) Math.cos(an) * 2.4f, (float) Math.sin(an) * 2.4f); }
      Painter.Draw(v, f, L, Col.Pack(20, 12, 10, 255), -1.8f, 1.2f);
      Painter.Draw(v, f, L, Col.Pack(255, 190, 120, 255), 2.6f, -1.6f);
      Painter.Draw(v, f, L, 0, 0, 0);
      AffineTransform keep = g.getTransform();
      g.translate(k * cell * 3 + cell * 1.5, cell + cell * 0.95); g.scale(0.85, 0.85); g.translate(-f.x, -f.y);
      Render.draw(g, v); g.setTransform(keep);
    }
    ImageIO.write(out, "png", new File(a[0]));
    System.out.println("faces ok, must=" + L.must);
  }
}
