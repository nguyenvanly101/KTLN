// Chup man hinh cac trang bang Chrome chay ngam (giao thuc DevTools, khong can thu vien ngoai).
// Dung: node tools/screenshots/shot.cjs <thu-muc-anh> <vai: guest|traveler|provider|admin> <duong-dan...>
// Bien moi truong: FULLTEXT_TEST_BASE (vd http://127.0.0.1:5227), SHOT_WIDTHS (vd "1366,390"), CHROME (duong dan chrome.exe).
// Tai khoan lay tu preview/branches-fixture.json (chi dung cho may chu thu).
const fs = require('fs'), path = require('path'), { spawn } = require('child_process');
process.chdir(path.join(__dirname, '../..'));
const { Client, base } = require('../../tests/fulltext-smoke.cjs');
const [outDir, role, ...pages] = process.argv.slice(2);
const widths = (process.env.SHOT_WIDTHS || '1366,390').split(',').map(Number);
const chrome = process.env.CHROME || 'C:/Program Files/Google/Chrome/Application/chrome.exe';
const sleep = ms => new Promise(r => setTimeout(r, ms));

(async () => {
  fs.mkdirSync(outDir, { recursive: true });
  const c = new Client();
  if (role && role !== 'guest') { const fx = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8')); await c.login(fx.users.find(x => x.label === role).email, 'BranchTest@2026'); }
  const profile = path.join(require('os').tmpdir(), 'ht-shot-' + process.pid);
  const proc = spawn(chrome, ['--headless=new', '--remote-debugging-port=9333', '--user-data-dir=' + profile, '--no-first-run', '--hide-scrollbars', 'about:blank'], { stdio: 'ignore' });
  let target; for (let n = 0; n < 50 && !target; n++) { await sleep(200); try { target = (await (await fetch('http://127.0.0.1:9333/json')).json()).find(t => t.type === 'page'); } catch { } }
  const ws = new WebSocket(target.webSocketDebuggerUrl); await new Promise(r => ws.onopen = r);
  let id = 0; const wait = new Map(); const events = [];
  ws.onmessage = m => { const d = JSON.parse(m.data); if (d.id && wait.has(d.id)) { wait.get(d.id)(d); wait.delete(d.id); } else if (d.method) events.push(d.method); };
  const send = (method, params = {}) => new Promise(r => { const i = ++id; wait.set(i, r); ws.send(JSON.stringify({ id: i, method, params })); });
  await send('Page.enable'); await send('Network.enable');
  const host = new URL(base).hostname;
  for (const [name, value] of c.cookies) await send('Network.setCookie', { name, value, domain: host, path: '/' });
  for (const p of pages) for (const w of widths) {
    await send('Emulation.setDeviceMetricsOverride', { width: w, height: w < 600 ? 844 : 900, deviceScaleFactor: 1, mobile: w < 600 });
    events.length = 0; await send('Page.navigate', { url: base + p });
    for (let n = 0; n < 60 && !events.includes('Page.loadEventFired'); n++) await sleep(250);
    await sleep(1200);
    const full = w < 600 ? true : false;
    const shot = await send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: full, ...(full ? { clip: { x: 0, y: 0, width: w, height: Math.min(2400, (await send('Runtime.evaluate', { expression: 'document.documentElement.scrollHeight', returnByValue: true })).result.result.value), scale: 1 } } : {}) });
    const ov = (await send('Runtime.evaluate', { expression: 'document.documentElement.scrollWidth>innerWidth+1', returnByValue: true })).result.result.value;
    if (process.env.SHOT_EVAL) console.log('eval:', JSON.stringify((await send('Runtime.evaluate', { expression: process.env.SHOT_EVAL, returnByValue: true })).result.result.value));
    const file = path.join(outDir, (role || 'guest') + '_' + p.replace(/[^a-z0-9]+/gi, '_').replace(/^_|_$/g, '') + '_' + w + '.png');
    fs.writeFileSync(file, Buffer.from(shot.result.data, 'base64')); console.log(file, ov ? 'TRAN NGANG' : '');
  }
  ws.close(); proc.kill();
})().catch(e => { console.error(e); process.exit(1); });
