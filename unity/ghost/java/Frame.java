import java.awt.*;
import java.awt.geom.*;
import java.awt.image.*;
import java.io.*;
import java.nio.file.*;
import javax.imageio.*;
// Призрак: ұрыс кадрын Unity-дегі FightView ретімен жинау (фон → параллакс → тұман → жер → көлеңке → жауынгерлер)
public class Frame {
  static BufferedImage im(String p) throws Exception { return ImageIO.read(new File(Ghost.ART + "" + p)); }
  public static void main(String[] a) throws Exception {
    Db.Init(new Ghost.FileSrc()); Pz.Init(); Game.Load(new Ghost.MemStore());
    Looks.Init(JNode.Parse(Files.readString(Path.of(Ghost.DATA + "meta_parts.json"))));
    int ch = Integer.parseInt(a[0]); float t = Float.parseFloat(a[1]);
    U.Seed(11); Match m = new Match(); m.StartCampaign(ch, 3); m.player.ai = true;
    for (int i = 0; i < (int) (t * 60); i++) { m.Update(1f / 60, null); m.fx.Clear(); }
    JNode meta = JNode.Parse(Files.readString(Path.of(Ghost.DATA + "meta_stages.json"))).Get(m.stageId);
    String b = "stages/" + m.stageId + "/";
    BufferedImage out = new BufferedImage(1280, 720, BufferedImage.TYPE_INT_ARGB);
    Graphics2D g = out.createGraphics();
    g.setRenderingHint(RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_ON);
    float z = m.zoom, pv = m.pivY;
    g.translate(640, pv); g.scale(z, z); g.translate(-640, -pv);
    float[] par = { meta.Get("par").At(0).F(), meta.Get("par").At(1).F(), meta.Get("par").At(2).F() };
    g.drawImage(im(b + "sky.png"), 0, 0, null);
    g.drawImage(im(b + "layer0.png"), (int) (-m.cam * par[0]), 0, null);
    g.drawImage(im(b + "layer1.png"), (int) (-m.cam * par[1]), 0, null);
    g.drawImage(im(b + "fogMid.png"), 0, 420, null);
    g.drawImage(im(b + "layer2.png"), (int) (-m.cam * par[2]), 0, null);
    g.drawImage(im(b + "fogLow.png"), 0, 540, null);
    g.translate(-m.cam, 0);
    g.drawImage(im(b + "ground.png"), 0, 0, null);
    int rim = Col.Parse(meta.S("rim"));
    for (Fighter f : new Fighter[] { m.enemy, m.player }) {
      float lift = Math.max(0, Math.min(1, (Db.GROUND - f.y) / 260)); float r = (58 - lift * 18) * f.sc;
      g.setColor(new Color(10, 4, 4, (int) ((0.45f - lift * 0.25f) * 255))); g.fill(new Ellipse2D.Float(f.x - r, Db.GROUND - r * .18f, 2 * r, 2 * r * .18f));
      VMesh v = new VMesh(); Look L = Looks.For(f); float side = meta.Get("lightX").F() + m.cam >= f.x ? 1 : -1;
      Painter.Live = a.length > 3;
      Painter.Draw(v, f, L, Col.Pack(20, 12, 10, 255), -side * 1.8f, 1.2f);
      Painter.Draw(v, f, L, rim, side * 2.6f, -1.6f);
      Painter.Draw(v, f, L, 0, 0, 0);
      Render.draw(g, v);
    }
    ImageIO.write(out, "png", new File(a[2]));
    System.out.println("ch" + ch + " stage=" + m.stageId + " t=" + t + " P " + m.player.state + " hp " + (int) m.player.hp + " | E " + m.enemy.state + " hp " + (int) m.enemy.hp + " cam=" + (int) m.cam + " zoom=" + m.zoom);
  }
}
