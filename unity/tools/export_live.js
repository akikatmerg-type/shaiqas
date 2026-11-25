// SHAIQAS → Unity: «тірі» фон деректері (бұлттар, сәуле, тұман, алдыңғы заттар, тірі элементтер)
const { chromium } = require('playwright');
const fs = require('fs'), path = require('path');
const OUT = '/home/claude/port/out/live';
const save = (rel, dataUrl) => { const f = path.join(OUT, rel); fs.mkdirSync(path.dirname(f), { recursive: true }); fs.writeFileSync(f, Buffer.from(dataUrl.split(',')[1], 'base64')); };
(async () => {
  const b = await chromium.launch(); const p = await b.newPage({ viewport: { width: 1280, height: 720 } });
  const errs = []; p.on('pageerror', e => errs.push(String(e)));
  await p.goto('file:///home/claude/port/tools/game_ev.html?sktest=1'); await p.waitForTimeout(1500);
  const r = await p.evaluate(() => window.__EV(`(() => {
    const files = [], M = {};
    const png = c => c.toDataURL('image/png');
    const clean = o => JSON.parse(JSON.stringify(o, (k, v) => (v && v.getContext) ? undefined : v));
    for (const id of Object.keys(STAGE_BUILD)) {
      const st = stageOf(id), L = st.L || liveSetup(st, id), R = {};
      const pre = 'stages/' + id + '/live/';
      R.wind = L.wind; R.pk = L.pk; R.leafCol = L.leafCol || null; R.stars = L.stars || [];
      R.sun = L.sun ? { x: L.sun.x, y: L.sun.y, r: L.sun.r, a: L.sun.a, rgb: L.sun.rgb } : null;
      R.clouds = L.clouds.map((c, i) => { files.push([pre + 'cloud' + i + '.png', png(c.spr)]); return { x0: c.x0, y: c.y, v: c.v, par: c.par, a: c.a, w: c.spr.width, h: c.spr.height }; });
      if (L.rays) { files.push([pre + 'rays.png', png(L.rays.c.c)]); R.rays = { x: L.rays.c.x, y: L.rays.c.y, a: L.rays.a, w: L.rays.c.c.width, h: L.rays.c.c.height }; }
      if (L.mist) { files.push([pre + 'mist.png', png(L.mist.c)]); R.mist = { y: L.mist.y, par: L.mist.par, v: L.mist.v, a: L.mist.a, w: L.mist.c.width, h: L.mist.c.height }; }
      R.fg = L.fg.map((f, i) => { files.push([pre + 'fg' + i + '.png', png(f.spr)]); return { x: f.x, par: f.par, ph: f.ph, sway: !!f.sway, flip: !!f.flip, dy: f.dy, w: f.spr.width, h: f.spr.height }; });
      R.corner = L.corner.map((c, i) => { files.push([pre + 'corner' + i + '.png', png(c.spr)]); return { ax: c.ax, ay: c.ay, flip: !!c.flip, ph: c.ph, right: !!c.right, w: c.spr.width, h: c.spr.height }; });
      files.push([pre + 'grade.png', png(L.grade)]);
      if (st.fog) { const fg = mistTex(fxRgb(st.fog[1]).join(','), 777 + id.length * 13, .9); files.push([pre + 'gfog.png', png(fg)]); R.gfog = { w: fg.width, h: fg.height }; }
      R.sky = clean(L.sky); R.lay = clean(L.lay); R.layW = st.layers.map(l => l[0].width);
      R.grass = (st.fg === 'grass' || L.fg.length === 0 && st.fg !== 'none') ? { col: st.fgCol } : null;
      M[id] = R;
    }
    M.__grass = FG_ITEMS;
    return { files, M };
  })()`));
  for (const [rel, d] of r.files) save(rel, d);
  fs.writeFileSync(path.join(OUT, 'meta_live.json'), JSON.stringify(r.M));
  console.log('files', r.files.length, 'errs', errs.slice(0, 3));
  await b.close();
})();
