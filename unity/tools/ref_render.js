const { chromium } = require('playwright'); const fs = require('fs');
(async () => {
  const b = await chromium.launch(); const p = await b.newPage();
  await p.goto('file:///home/claude/port/tools/game_ev.html?sktest=1'); await p.waitForTimeout(1200);
  const url = await p.evaluate(() => window.__EV(`(() => {
    const poses = ['idle','heavyS','kickS','crouch','hitHeavy'], cw = 260, ch = 330;
    const c = makeCanvas(cw * poses.length, ch * 2), g = c.getContext('2d'); g.fillStyle = 'rgb(90,90,110)'; g.fillRect(0, 0, c.width, c.height);
    const eq0 = SAVE.eq; SAVE.eq = { weapon: 'soyil', armor: 'chapan', helm: 'kalpak' };
    const fs = [game.makePlayer(0, 'sharua'), game.makeEnemy(0, 3, 0)]; SAVE.eq = eq0;
    fs.forEach((f, r) => poses.forEach((ps, i) => {
      f._hs = null; f.sec = null; f.vx = 0; f.at = 0;
      g.save(); g.translate(i * cw + cw / 2 - 20, r * ch + ch - 20); g.scale(.62, .62);
      f.drawBody(g, 'color', false, Object.assign({}, P[ps]), 0, 0, 1, 0); g.restore();
    }));
    return c.toDataURL();
  })()`));
  fs.writeFileSync(process.argv[2], Buffer.from(url.split(',')[1], 'base64')); await b.close();
})();
