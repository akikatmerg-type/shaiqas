import java.awt.*;
import java.awt.geom.*;
import java.awt.image.*;
import java.io.*;
import java.nio.file.*;
import javax.imageio.*;
// Призрачный рендер: VMesh → PNG (Unity-дегі Mesh-тің дәл баламасы)
public class Render {
  static java.util.Map<Integer, BufferedImage> imgs = new java.util.HashMap<>();
  static BufferedImage img(int slot) throws Exception {
    if (!imgs.containsKey(slot)) imgs.put(slot, ImageIO.read(new File(Ghost.ART + "parts/" + Atlas.keys.Get(slot) + ".png")));
    return imgs.get(slot);
  }
  static Color col(int c) { return new Color((c >> 16) & 255, (c >> 8) & 255, c & 255, (c >>> 24) & 255); }
  public static void draw(Graphics2D g, VMesh m) throws Exception {
    g.setRenderingHint(RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_ON);
    g.setRenderingHint(RenderingHints.KEY_INTERPOLATION, RenderingHints.VALUE_INTERPOLATION_BILINEAR);
    for (int t = 0; t < m.nt; t += 3) {
      int a = m.tri[t], b = m.tri[t + 1], c = m.tri[t + 2];
      if (m.slot[a] >= 0) {
        if (b != a + 1 || c != a + 2) continue;   // квадраттың екінші үшбұрышы
        BufferedImage im = img(m.slot[a]); int W = im.getWidth(), H = im.getHeight(), d = a + 3;
        AffineTransform at = new AffineTransform((m.vx[b] - m.vx[a]) / W, (m.vy[b] - m.vy[a]) / W, (m.vx[d] - m.vx[a]) / H, (m.vy[d] - m.vy[a]) / H, m.vx[a], m.vy[a]);
        if (m.col[a] != -1) { // бір түсті силуэт
          BufferedImage s = new BufferedImage(W, H, BufferedImage.TYPE_INT_ARGB);
          int cc = m.col[a] & 0xFFFFFF;
          for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) { int al = (im.getRGB(x, y) >>> 24) * ((m.col[a] >>> 24)) / 255; s.setRGB(x, y, (al << 24) | cc); }
          im = s;
        }
        g.drawImage(im, at, null);
        continue;
      }
      g.setColor(col(m.col[a]));
      Path2D p = new Path2D.Float(); p.moveTo(m.vx[a], m.vy[a]); p.lineTo(m.vx[b], m.vy[b]); p.lineTo(m.vx[c], m.vy[c]); p.closePath();
      g.fill(p);
    }
  }
  public static void main(String[] args) throws Exception {
    Db.Init(new Ghost.FileSrc()); Pz.Init(); Game.Load(new Ghost.MemStore());
    Looks.Init(JNode.Parse(Files.readString(Path.of(Ghost.DATA + "meta_parts.json"))));
    String[] poses = args[0].split(",");
    String[] looks = args[1].split(",");
    int cw = 260, ch = 330;
    BufferedImage out = new BufferedImage(cw * poses.length, ch * looks.length, BufferedImage.TYPE_INT_ARGB);
    Graphics2D g = out.createGraphics(); g.setColor(new Color(90, 90, 110)); g.fillRect(0, 0, out.getWidth(), out.getHeight());
    Match mt = new Match();
    for (int r = 0; r < looks.length; r++) for (int i = 0; i < poses.length; i++) {
      String[] lk = looks[r].split(":");   // look:weapon:coat:accent
      Fighter f = new Fighter(); f.look = lk[0]; f.weapon = lk[1]; f.coat = lk[2]; f.accent = lk[3]; f.Init();
      Pz.Copy(f.cur, Pz.Get(poses[i])); f.x = 0; f.y = 0; f.facing = 1; f.at = 0; f.vx = 0;
      VMesh m = new VMesh(); Look L = Looks.For(f);
      Painter.Draw(m, f, L, 0, 0, 0);
      Graphics2D g2 = (Graphics2D) g.create(); g2.translate(i * cw + cw / 2 - 20, r * ch + ch - 20); g2.scale(0.62, 0.62);
      draw(g2, m); g2.dispose();
    }
    ImageIO.write(out, "png", new File(args[2]));
    System.out.println("tris for last: ok, atlas slots=" + Atlas.keys.Count());
  }
}
