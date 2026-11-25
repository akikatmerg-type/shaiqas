// SHAIQAS → Unity: жорық картасының графикасы (веб: drawCampMap, campSite, campStory, stageMedallion, trophy)
const { chromium } = require('playwright'); const fs = require('fs'), path = require('path');
const OUT = '/home/claude/port/out/camp';
(async () => {
  const b = await chromium.launch(); const p = await b.newPage({ viewport: { width: 1280, height: 720 } });
  await p.addInitScript(() => { try { localStorage.setItem('shaiqas-name', 'Тест'); localStorage.setItem('shaiqas-tut-off', '1'); } catch (_) {} });
  await p.goto('file:///home/claude/port/tools/game_ev.html?sktest=1'); await p.waitForTimeout(1500);
  const r = await p.evaluate(() => window.__EV(`(() => {
    const F = {}, J = {}, png = c => c.toDataURL('image/png'), jpg = c => c.toDataURL('image/jpeg', .88);
    const cv = (w, h) => { const c = makeCanvas(w, h); return [c, c.getContext('2d')]; };
    for (let ci = 0; ci < CHAPTERS.length; ci++) J['wide_' + ci] = jpg(chapterMapWide(ci));
    // режим орындары (r = 56)
    for (const s of CAMP_SITES.slice(1)) for (const locked of [0, 1]) {
      const R = 56, [c, g] = cv(R * 2 + 40, R * 2 + 40), x = R + 20, y = R + 20;
      medallion(g, x, y, R, { col: locked ? '#6a6058' : s.col });
      const gr = g.createRadialGradient(x, y - R * .45, 4, x, y, R);
      if (locked) { gr.addColorStop(0, '#d8d0c4'); gr.addColorStop(1, '#8a8074'); } else { gr.addColorStop(0, '#fff4d6'); gr.addColorStop(1, hexMix(s.col, '#f2d6a0', .55)); }
      g.fillStyle = gr; circle(g, x, y, R);
      campIcon(g, s.id, x, y + 2, R / 56, !!locked);
      if (locked) campLock(g, x + R * .62, y + R * .5, 1.1);
      F['site_' + s.id + '_' + locked] = png(c);
    }
    // орталық медальон: арена суреті + бас жау сұлбасы (r = 84 + 6)
    for (let ci = 0; ci < CHAPTERS.length; ci++) {
      const C = CHAPTERS[ci], r = 84, RR = r + 6, [c, g] = cv(RR * 2 + 40, RR * 2 + 40), x = RR + 20, y = RR + 20;
      medallion(g, x, y, RR, { col: '#a32222' });
      g.save(); g.beginPath(); g.arc(x, y, RR, 0, Math.PI * 2); g.clip();
      const th = thumbOf(C.stage), hh = RR * 2, ww = hh * 16 / 9; g.drawImage(th, x - ww / 2, y - hh / 2, ww, hh);
      const vg = g.createRadialGradient(x, y, r * .3, x, y, RR); vg.addColorStop(0, 'rgba(0,0,0,0)'); vg.addColorStop(1, 'rgba(30,10,0,.55)'); g.fillStyle = vg; g.fillRect(x - RR, y - RR, hh, hh);
      const f = enemyFig(ci, 3); g.translate(x, y + r * .78); g.scale(-r / 54, r / 54);
      g.fillStyle = 'rgba(20,10,6,.8)'; g.strokeStyle = 'rgba(20,10,6,.8)'; poly(g, [[-30, 46], [-16, 13], [12, 13], [30, 46]]); f.headShape(g); f.hatShape(g, 'rgba(20,10,6,.8)', C.accent, false);
      g.restore();
      F['story_' + ci] = png(c);
    }
    { const r = 84, [c, g] = cv(400, 400); g.translate(200, 200); g.fillStyle = 'rgba(255,205,110,1)';
      for (let i = 0; i < 18; i++) { g.rotate(Math.PI * 2 / 18); poly(g, [[-7, r + 6], [0, r * 1.9 + (i % 2) * 18], [7, r + 6]]); } F['rays'] = png(c); }
    // кезең медальондары
    for (let ci = 0; ci < CHAPTERS.length; ci++) for (let si = 0; si < 4; si++) for (const st of ['done', 'open', 'locked']) {
      const R = si === 3 ? 42 : 34, [c, g] = cv(R * 2 + 40, R * 2 + 64), x = R + 20, y = R + 44;
      stageMedallion(g, x, y, R, ci, si, st, false);
      F['stage_' + ci + '_' + si + '_' + st] = png(c);
    }
    for (let t = 0; t < 3; t++) for (const on of [0, 1]) { const [c, g] = cv(80, 80); trophy(g, 40, 44, TOUR_TIERS[t].col, !!on, 1.15); F['trophy_' + t + '_' + on] = png(c); }
    for (const on of [0, 1]) { const [c, g] = cv(32, 32); starShape(g, 16, 16, 14, !!on); F['star_' + on] = png(c); }
    { const [c, g] = cv(32, 32); coinIcon(g, 16, 16, 14); F['coin'] = png(c); }
    F['glow'] = png(glowSpr('#ffcf6a'));
    for (const [n, col] of [['red', 'rgba(150,20,14,.94)'], ['dark', 'rgba(30,24,34,.94)'], ['boss', 'rgba(110,10,10,.92)'], ['p2', 'rgba(120,10,10,.9)'], ['p3', 'rgba(50,10,80,.92)']]) {
      const [c, g] = cv(1280, 190); const kg = goldText, kp = game.pt; goldText = () => {}; game.pt = 2;
      try { brushBanner(g, 'ЖЕҢІЛДІҢ...', 100, col, '#ffe2a0'); } finally { goldText = kg; game.pt = kp; } F['brush_' + n] = png(c);
    }
    { const [c, g] = cv(220, 220); g.strokeStyle = '#fff'; g.lineWidth = 4; g.beginPath(); g.arc(110, 110, 100, 0, Math.PI * 2); g.stroke(); F['ring'] = png(c); }
    { const [c, g] = cv(64, 64); campLock(g, 32, 30, 2.2); F['lock'] = png(c); }
    // тақталар (9-slice үшін)
    for (const sel of [0, 1]) { const [c, g] = cv(120, 60); g.fillStyle = sel ? '#fff1c8' : 'rgba(245,232,200,.96)'; roundRect(g, 4, 4, 112, 52, 8); g.fill(); g.strokeStyle = sel ? '#c47a10' : 'rgba(100,60,20,.65)'; g.lineWidth = sel ? 2.4 : 1.3; roundRect(g, 4, 4, 112, 52, 8); g.stroke(); F['plate_' + sel] = png(c); }
    for (const sel of [0, 1]) { const [c, g] = cv(450, 90); dzPoint(g, 20, 14, 410, 62, 16); g.fillStyle = sel ? 'rgba(70,20,12,.95)' : 'rgba(42,20,10,.92)'; g.fill(); dzPoint(g, 20, 14, 410, 62, 16); g.strokeStyle = dzGold(g, 14, 76); g.lineWidth = 2; g.stroke(); F['point_' + sel] = png(c); }
    { const [c, g] = cv(200, 50); dzPoint(g, 6, 7, 188, 36, 14); g.fillStyle = 'rgba(42,20,6,.88)'; g.fill(); g.strokeStyle = dzGold(g, 7, 43); g.lineWidth = 2; g.stroke(); F['pill'] = png(c); }
    { const [c, g] = cv(64, 64); g.fillStyle = 'rgba(40,20,8,.35)'; g.beginPath(); g.ellipse(32, 32, 30, 8, 0, 0, Math.PI * 2); g.fill(); F['shadow'] = png(c); }
    for (const id of Object.keys(STAGE_BUILD)) J['thumb_' + id] = jpg(thumbOf(id));
    const E = {};
    for (let ci = 0; ci < CHAPTERS.length; ci++) for (let si = 0; si < 4; si++) {
      const [c, g] = cv(300, 380); const f = enemyFig(ci, si); f.at = 1.3;
      try { drawFront(g, f, 150, 360, .78, 1.3, 'rgba(255,170,130,.8)'); } catch (e) { E.__err = String(e); }
      E['efront_' + ci + '_' + si] = png(c);
    }
    { const [c, g] = cv(300, 380); const f = game.makeFoe({ special: 'trainer', ch: 0 }, 0); try { drawFront(g, f, 150, 360, .78, 1.3, 'rgba(255,200,140,.8)'); } catch (e) { E.__err2 = String(e); } E['efront_trainer'] = png(c); }
    return { F, J, E };
  })()`));
  for (const [k, v] of Object.entries(r.F)) fs.writeFileSync(path.join(OUT, k + '.png'), Buffer.from(v.split(',')[1], 'base64'));
  fs.mkdirSync(OUT + '_wide', { recursive: true });
  for (const [k, v] of Object.entries(r.J)) fs.writeFileSync(path.join(OUT + '_wide', k + '.jpg'), Buffer.from(v.split(',')[1], 'base64'));
  fs.mkdirSync(OUT + '_front', { recursive: true });
  for (const [k, v] of Object.entries(r.E)) if (!k.startsWith('__')) fs.writeFileSync(path.join(OUT + '_front', k + '.png'), Buffer.from(v.split(',')[1], 'base64'));
  console.log(Object.keys(r.F).length, Object.keys(r.J).length, Object.keys(r.E).length, r.E.__err || '', r.E.__err2 || '');
  await b.close();
})();
