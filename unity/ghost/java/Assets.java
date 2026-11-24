import java.io.*;
import java.nio.file.*;
// Призрак: Unity коды сұрайтын әр сурет пен деректер файлы бар ма?
public class Assets {
  static int miss = 0, ok = 0;
  static void need(String rel) { File f1 = new File(Ghost.ART + "" + rel + ".png"), f2 = new File(Ghost.ART + "" + rel + ".jpg");
    if (f1.exists() || f2.exists()) ok++; else { miss++; if (miss < 30) System.out.println("  ЖОҚ: Art/" + rel); } }
  public static void main(String[] a) throws Exception {
    Db.Init(new Ghost.FileSrc()); Pz.Init(); Game.Load(new Ghost.MemStore());
    Story.SetSceneMeta(JNode.Parse(Files.readString(Path.of(Ghost.DATA + "meta_scenes.json"))));
    Looks.Init(JNode.Parse(Files.readString(Path.of(Ghost.DATA + "meta_parts.json"))));
    need("lobby/backdrop"); need("lobby/zhyrau"); need("icons/coin");
    for (int i = 0; i < Db.HeroOrder.Count(); i++) { String h = Db.HeroOrder.At(i).Str(); need("lobby/front_" + h); need("portraits/" + h); }
    for (int c = 0; c < Db.NCH; c++) { need("maps/chapter" + c); String st = Db.Ch(c).S("stage"); for (String L : new String[]{"sky","layer0","layer1","layer2","fogMid","fogLow","ground"}) need("stages/" + st + "/" + L); }
    for (int i = 0; i < Db.Hist.Count(); i++) { need("icons/hist" + i); need("icons/hist" + i + "_locked"); }
    for (int i = 0; i < Db.Medals.Count(); i++) { String id = Db.Medals.At(i).S("id"); need("icons/medal_" + id); need("icons/medal_" + id + "_off"); }
    String[][] tabs = {{"weapon","WEAPON_ORDER"},{"armor","ARMOR_ORDER"},{"helm","HELM_ORDER"}};
    for (String[] t : tabs) { JNode o = Db.J(t[1]); for (int i = 0; i < o.Count(); i++) need("items/" + t[0] + "_" + o.At(i).Str()); }
    int pages = 0;
    for (int c = 0; c < Db.NCH; c++) {
      Lst<Page> e = Story.Episodes(c); for (int i = 0; i < e.Count(); i++) { need("scenes/" + e.Get(i).pic); pages++; }
      for (int s = 0; s < 4; s++) { Lst<Page> L = Story.BeforeFight(c, s); for (int i = 0; i < L.Count(); i++) { need("scenes/" + L.Get(i).pic); pages++; } }
    }
    Lst<Page> ep = Story.EpilogueList(); for (int i = 0; i < ep.Count(); i++) need("scenes/" + ep.Get(i).pic);
    // жауынгерлер: барлық батыр × сауыт × бас киім × қару және жаулар
    Match m = new Match(); int looks = 0;
    for (int h = 0; h < Db.HeroOrder.Count(); h++) for (int ar = 0; ar < Db.ArmorOrder.Count(); ar++) for (int hm = 0; hm < Db.HelmOrder.Count(); hm++) for (int w = 0; w < Db.WeaponOrder.Count(); w++) {
      Game.S.hero = Db.HeroOrder.At(h).Str(); Game.S.eqArmor = Db.ArmorOrder.At(ar).Str(); Game.S.eqHelm = Db.HelmOrder.At(hm).Str(); Game.S.eqWeapon = Db.WeaponOrder.At(w).Str();
      Fighter f = m.MakePlayer(0, Game.S.hero); Look L = Looks.For(f); looks++;
      if (L.headSlot < 0 || L.hatSlot < 0 || L.wepSlot < 0) { miss++; if (miss < 30) System.out.println("  LOOK жоқ: " + f.look + " " + f.weapon); }
    }
    for (int c = 0; c < Db.NCH; c++) for (int i = 0; i < 4; i++) { m.mode = "campaign"; m.chapter = c; m.stageIdx = i; Fighter f = m.MakeEnemy(c, i, 0); Look L = Looks.For(f); looks++; if (L.headSlot < 0 || L.wepSlot < 0) { miss++; System.out.println("  ЖАУ LOOK жоқ: " + f.look); } }
    { Fighter f = m.MakeTrainer(0); Look L = Looks.For(f); if (L.headSlot < 0 || L.wepSlot < 0) { miss++; System.out.println("  жаттықтырушы LOOK жоқ"); } }
    for (int i = 0; i < Atlas.keys.Count(); i++) need("parts/" + Atlas.keys.Get(i));
    System.out.println("[assets] файл/облик тексерілді: " + ok + " бар, " + miss + " жоқ · беттер " + pages + " · облик " + looks + " · атлас ұяшығы " + Atlas.keys.Count());
  }
}
