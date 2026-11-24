// SHAIQAS → Unity: ойын суреттерін PNG/JPG етіп шығару (ойынның өз сурет функцияларымен)
const { chromium } = require('playwright');
const fs = require('fs'), path = require('path');
const OUT = '/home/claude/port/out/art';
const save = (rel, dataUrl) => { const f = path.join(OUT, rel); fs.mkdirSync(path.dirname(f), { recursive: true }); fs.writeFileSync(f, Buffer.from(dataUrl.split(',')[1], 'base64')); };
(async () => {
  const b = await chromium.launch(); const p = await b.newPage({ viewport: { width: 1280, height: 720 } });
  const errs = []; p.on('pageerror', e => errs.push(String(e))); p.on('console', m => { if (m.type() === 'error') errs.push(m.text()); });
  await p.goto('file:///home/claude/port/tools/game_ev.html?sktest=1'); await p.waitForTimeout(1500);
  // ойын циклін тоқтатамыз — суреттер тұрақты болсын
  await p.evaluate(() => window.__EV(`window.__PNG = c => c.toDataURL('image/png'); window.__JPG = c => c.toDataURL('image/jpeg', .86); 0`));
  const jobs = (process.argv[2] || 'stages,scenes,maps,portraits,items,icons,parts,lobby').split(',');
  const meta = {};
  for (const job of jobs) {
    const r = await p.evaluate(([job]) => window.__EV(`(() => {
      const files = [], M = {};
      const put = (rel, c, jpg) => files.push([rel, jpg ? __JPG(c) : __PNG(c)]);
      const cv = (w, h) => { const c = makeCanvas(w, h); return [c, c.getContext('2d')]; };
      const job = ${JSON.stringify(job)};
      if (job === 'stages') {
        for (const id of Object.keys(STAGE_BUILD)) {
          const st = stageOf(id), L = st.L;
          put('stages/' + id + '/sky.png', st.sky);
          st.layers.forEach(([img, f], i) => put('stages/' + id + '/layer' + i + '.png', img));
          put('stages/' + id + '/ground.png', st.ground);
          put('stages/' + id + '/fogMid.png', L.fogMid); put('stages/' + id + '/fogLow.png', L.fogLow);
          M[id] = { par: st.layers.map(l => l[1]), top: L.top, layerW: st.layers.map(l => l[0].width), skyW: st.sky.width, skyH: st.sky.height, groundW: st.ground.width, groundH: st.ground.height,
            rim: st.rim, lightX: st.lightX, day: !!st.day, dustCol: st.dustCol || null, fogMidY: 420, fogLowY: 540 };
        }
      }
      if (job === 'scenes') {
        // барлық сюжет беттері: тарау беттері, ұрыс алдындағы/кейінгі беттер, диалогтар, эпилог
        const pages = [];
        CHAPTERS.forEach((C, c) => C.episodes.forEach((ep, i) => pages.push(['ch' + c + '_ep' + i, ep])));
        for (let c = 0; c < CHAPTERS.length; c++) for (let s = 0; s < 4; s++) for (const k of ['pre', 'post']) { const ep = bscPage(c, s, k); if (ep) pages.push(['ch' + c + '_bsc' + s + '_' + k, ep]); }
        for (let c = 0; c < CHAPTERS.length; c++) for (let k = 0; k < 2; k++) { const ep = dlgPage(c, k); if (ep) pages.push(['ch' + c + '_dlg' + k, ep]); }
        SAB_EPILOGUE.forEach((ep, i) => pages.push(['epi' + i, ep]));
        const seen = {};
        for (const [id, ep] of pages) {
          const key = JSON.stringify([ep.scene, ep.cast || null, ep.fire || null, ep.yurt || null, !!ep.sab]);
          if (seen[key]) { M[id] = seen[key]; continue; }
          const [c, g] = cv(W, 560);
          g.drawImage(sceneOf(ep.scene), 0, 0);
          if (ep.sab) { SAB_FIG.last = 0; try { sabSceneLive(g, ep, 10, 10); } catch (e) { M.__err = (M.__err || []).concat(id + ': ' + e); } }
          const fn = 'scenes/' + id + '.jpg'; put(fn, c, true); seen[key] = id; M[id] = id;
        }
      }
      if (job === 'maps') {
        for (let ci = 0; ci < CHAPTERS.length; ci++) { const m = chapterMap(ci); put('maps/chapter' + ci + '.png', m); M[ci] = { w: m.width, h: m.height, nodes: CHAPTERS[ci].nodes }; }
      }
      if (job === 'portraits') {
        for (const id of HERO_ORDER) { const [c, g] = cv(256, 256); drawHeroPortrait(g, id, 128, 128, 120); put('portraits/' + id + '.png', c); }
      }
      if (job === 'items') {
        for (const id of WEAPON_ORDER) { const [c, g] = cv(256, 256); drawWeaponArt(g, id, 128, 128, 220); put('items/weapon_' + id + '.png', c); }
        for (const id of ARMOR_ORDER) { const [c, g] = cv(256, 256); drawArmorArt(g, id, 128, 128, 220); put('items/armor_' + id + '.png', c); }
        for (const id of HELM_ORDER) { const [c, g] = cv(256, 256); drawHelmArt(g, id, 128, 128, 220); put('items/helm_' + id + '.png', c); }
      }
      if (job === 'icons') {
        HIST.forEach((h, i) => { for (const lk of [false, true]) { const [c, g] = cv(160, 160); histIcon(g, i, 80, 80, 70, lk); put('icons/hist' + i + (lk ? '_locked' : '') + '.png', c); } });
        MEDALS.forEach(m => { for (const got of [true, false]) { const [c, g] = cv(160, 160); medalIcon(g, m, 80, 80, 70, got); put('icons/medal_' + m.id + (got ? '' : '_off') + '.png', c); } });
        { const [c, g] = cv(96, 96); coinIcon(g, 48, 48, 40); put('icons/coin.png', c); }
      }
      if (job === 'parts') {
        // бас (бет, шаш, сақал) және бас киім — бас жүйесінде (C нүктесі, p.t бұрышымен); қару — қол жүйесінде (бұрыш 0 = төмен)
        const SCL = 2, heads = {}, hats = {}, weps = {}, looks = {};
        const HB = [-50, -95, 100, 140], WB = [-70, -120, 140, 380];
        const keyOf = (f, P) => ({ head: [P.skin, P.hair, P.must, f.beard, f.heroId, f.boss, f.npc].join('|'),
          hat: f.hat + '|' + JSON.stringify(P.hat) + '|' + f.accent + '|' + JSON.stringify(f.hatV || null),
          wep: f.weapon + '|' + f.accent + '|' + P.wood + P.steel + P.grip });
        const hsh = s => { let h = 2166136261; for (let i = 0; i < s.length; i++) { h ^= s.charCodeAt(i); h = Math.imul(h, 16777619); } return (h >>> 0).toString(36); };
        const add = (lookId, f) => {
          f._hs = null; f._ws = null; f._live = false;
          const P = f.palette(), k = keyOf(f, P), acc = f.accent;
          const hk = 'h_' + hsh(k.head), tk = 't_' + hsh(k.hat), wk = 'w_' + f.weapon + '_' + hsh(k.wep);
          if (!heads[hk]) { const [c, g] = cv(HB[2] * SCL, HB[3] * SCL); g.translate(-HB[0] * SCL, -HB[1] * SCL); g.scale(SCL, SCL); g.lineCap = 'round'; g.lineJoin = 'round'; f.headShape(g, P); put('parts/' + hk + '.png', c); heads[hk] = 1; }
          if (!hats[tk]) { const [c, g] = cv(HB[2] * SCL, HB[3] * SCL); g.translate(-HB[0] * SCL, -HB[1] * SCL); g.scale(SCL, SCL); g.lineCap = 'round'; g.lineJoin = 'round'; g.fillStyle = P.coat; g.strokeStyle = P.coat; f.hatShape(g, P.coat, acc, false, P.hat); put('parts/' + tk + '.png', c); hats[tk] = 1; }
          if (!weps[wk]) { const [c, g] = cv(WB[2] * SCL, WB[3] * SCL); g.translate(-WB[0] * SCL, -WB[1] * SCL); g.scale(SCL, SCL); g.lineCap = 'round'; g.lineJoin = 'round'; f.drawWeapon(g, [0, 0], 0, P.coat, acc, false, P); put('parts/' + wk + '.png', c); weps[wk] = 1; }
          // силуэттер (көлеңке/жиек өтуі үшін) — бір түсті
          looks[lookId] = { head: hk, hat: tk, wep: wk, pal: JSON.stringify(P), weapon: f.weapon, accent: acc, sleeve: !!f.sleeve, bw: f.build ? f.build.wx : 1, bh: f.build ? f.build.wy : 1 };
        };
        const eq0 = SAVE.eq;
        for (const h of HERO_ORDER) for (const a of ARMOR_ORDER) for (const m of HELM_ORDER) for (const w of WEAPON_ORDER) {
          if (w !== 'soyil' && (a !== 'chapan' || m !== 'kalpak')) continue; // қару бас киімге тәуелсіз: бір рет жеткілікті
          SAVE.eq = { weapon: w, armor: a, helm: m }; add('p_' + h + '_' + a + '_' + m + '_' + w, game.makePlayer(0, h));
        }
        SAVE.eq = eq0;
        for (let c = 0; c < CHAPTERS.length; c++) CHAPTERS[c].enemies.forEach((e, i) => add('e_' + c + '_' + i, game.makeEnemy(c, i, 0)));
        add('npc_zhyrau', new Fighter({ npc: true, weapon: 'staff', hat: 'borik', coat: 'robe', beard: true }));
        add('npc_trainer', new Fighter({ name: 'Жаттықтырушы', ai: true, weapon: 'soyil', hat: 'borik', coat: 'chapan', beard: true, accent: '#9fd68a' }));
        for (const [who, L] of Object.entries(DLG_LOOK)) { if (L.foe) continue; add('npc_' + who, new Fighter({ name: L.name, npc: true, weapon: L.weapon || 'staff', hat: L.hat || 'borik', coat: L.coat || 'robe', beard: !!L.beard })); }
        M.looks = looks; M.headBox = HB; M.wepBox = WB; M.scale = SCL;
      }
      if (job === 'lobby') {
        const st = stageOf('steppe');
        { const [c, g] = cv(W, H); const cam = 160; drawStageBack(g, st, cam, 3); g.save(); g.translate(-cam, 0); g.drawImage(st.ground, 0, 0); g.restore();
          g.fillStyle = 'rgba(40,50,20,.35)'; g.beginPath(); g.ellipse(770, GROUND - 2, 250, 20, 0, 0, Math.PI * 2); g.fill();
          drawBigYurt(g, 770, GROUND - 4, 1.08); put('lobby/backdrop.jpg', c, true); }
        { const [c, g] = cv(700, 600); drawBigYurt(g, 350, 560, 1.08); put('lobby/yurt.png', c); }
        for (const h of HERO_ORDER) { const [c, g] = cv(420, 560); const eq0 = SAVE.eq; SAVE.eq = { weapon: 'soyil', armor: 'chapan', helm: 'kalpak' }; const f = game.makePlayer(0, h); SAVE.eq = eq0;
          try { drawFront(g, f, 210, 540, 1.42, 3, st.rim); } catch (e) { M.__err = (M.__err || []).concat(h + ': ' + e); } put('lobby/front_' + h + '.png', c); }
        { const [c, g] = cv(300, 420); const z = new Fighter({ npc: true, weapon: 'staff', hat: 'borik', coat: 'robe', beard: true, x: 150 }); z.update(1 / 60); g.translate(0, -GROUND + 400); z.draw(g, st.rim, 0); put('lobby/zhyrau.png', c); }
      }
      return { files, M };
    })()`), [job]);
    for (const [rel, d] of r.files) save(rel, d);
    meta[job] = r.M; console.log(job, r.files.length, 'files', r.M.__err ? 'ERR ' + r.M.__err.slice(0, 3) : '');
  }
  for (const [k, v] of Object.entries(meta)) fs.writeFileSync(path.join(OUT, 'meta_' + k + '.json'), JSON.stringify(v));
  console.log('pageerrors', errs.length, errs.slice(0, 4));
  await b.close();
})();
