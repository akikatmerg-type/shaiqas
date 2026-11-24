// SHAIQAS → Unity: ойынның барлық деректерін JSON-ға шығару (Playwright арқылы тірі ойыннан)
const { chromium } = require('playwright');
const fs = require('fs'), path = require('path');
const SRC = process.argv[2], OUT = process.argv[3];
const NAMES = process.argv[4].split(',');
(async () => {
  const b = await chromium.launch(); const p = await b.newPage({ viewport: { width: 1280, height: 720 } });
  const errs = []; p.on('pageerror', e => errs.push(String(e)));
  await p.goto('file://' + SRC + '?sktest=1'); await p.waitForTimeout(1500);
  const res = await p.evaluate((NAMES) => {
    const seen = new WeakSet(), out = {}, skipped = {};
    function clean(v, d) {
      if (d > 14) return null;
      if (v === null || v === undefined) return null;
      const t = typeof v;
      if (t === 'number') return Number.isFinite(v) ? v : null;
      if (t === 'string' || t === 'boolean') return v;
      if (t === 'function') return undefined;
      if (v instanceof Float32Array || v instanceof Array) { if (seen.has(v)) return null; seen.add(v); const a = Array.from(v, x => { const c = clean(x, d + 1); return c === undefined ? null : c; }); seen.delete(v); return a; }
      if (t === 'object') {
        if (typeof HTMLElement !== 'undefined' && v instanceof HTMLElement) return undefined;
        if (v instanceof CanvasRenderingContext2D || v instanceof HTMLCanvasElement || v instanceof Image) return undefined;
        if (seen.has(v)) return null; seen.add(v);
        const o = {};
        for (const k of Object.keys(v)) { let x; try { x = v[k]; } catch (_) { continue; } const c = clean(x, d + 1); if (c !== undefined) o[k] = c; }
        // getter-лер (мысалы HEROES.sharua.name)
        const pr = Object.getPrototypeOf(v);
        seen.delete(v); return o;
      }
      return undefined;
    }
    for (const n of NAMES) {
      let v; try { v = window.__EV(n); } catch (e) { skipped[n] = 'noexist'; continue; }
      const c = clean(v, 0);
      if (c === undefined) { skipped[n] = 'fn'; continue; }
      out[n] = c;
    }
    out.__CONST = window.__EV("({ W, H, GROUND, ARENA, GRAV, S, TH, SH, TOR, UA, FA, ECO_K, BOSS_WINS, QZ_N, NCH: CHAPTERS.length })");
    return { out, skipped };
  }, NAMES);
  fs.mkdirSync(OUT, { recursive: true });
  let total = 0;
  for (const [k, v] of Object.entries(res.out)) { const s = JSON.stringify(v, null, 1); total += s.length; fs.writeFileSync(path.join(OUT, k + '.json'), s); }
  console.log('written', Object.keys(res.out).length, 'files,', (total / 1024 | 0), 'KB');
  console.log('skipped', JSON.stringify(res.skipped));
  console.log('pageerrors', errs.length, errs.slice(0, 3));
  await b.close();
})();
