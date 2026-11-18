// Bo qua moi duong dan noi bo theo tung vai (khach vang lai, khach du lich, nha cung cap, quan tri) de bat trang loi.
// Ghi lai: ma 5xx, 404, trang bao loi, trang cham (> 5 giay), duong dan vao trang dang nhap khi da dang nhap.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 node tests/crawl-roles.cjs [so-trang-toi-da-moi-vai]
const fs = require('node:fs');
const { Client, base, decode } = require('./fulltext-smoke.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
const user = l => fixture.users.find(x => x.label === l);
const LIMIT = +(process.argv[2] || 220);
const ONLY = process.argv[3] ? process.argv[3].split(',') : null;
const SKIP = /\/(Account\/Logout|logout|dang-xuat)|\/api\/|\/xoa|\/delete|\/Delete|download|tai-xuong|\/anh\/|service-photos|\.(jpg|png|pdf|css|js|svg|webp)(\?|$)|^\/lib\/|^\/images\/|^\/anh-tra-cuu\/|mailto:|tel:|#/i;
const pattern = p => p.split('?')[0].replace(/\/\d+(?=\/|$)/g, '/:id').replace(/\/[0-9a-f]{20,}(?=\/|$)/gi, '/:key');

async function crawl(role, client, starts) {
  const seen = new Set(), perPattern = new Map(), queue = [...starts], issues = [], visited = [];
  while (queue.length && visited.length < LIMIT) {
    const url = queue.shift(); if (seen.has(url)) continue; seen.add(url);
    const pat = pattern(url); const n = perPattern.get(pat) || 0; if (n >= 2) continue; perPattern.set(pat, n + 1);
    const t0 = Date.now(); let r;
    try { r = await client.get(url); } catch (e) { issues.push({ role, url, kind: 'network', detail: e.message }); continue; }
    const ms = Date.now() - t0; visited.push({ url, status: r.status, ms }); const before = issues.length;
    const html = r.html || '';
    if (r.status >= 500) issues.push({ role, url, kind: 'HTTP ' + r.status, detail: decode(html).replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').slice(0, 220) });
    else if (r.status === 404) issues.push({ role, url, kind: '404', detail: 'Không tìm thấy' });
    else if (r.status === 400 || r.status === 409 || r.status === 422) issues.push({ role, url, kind: 'HTTP ' + r.status, detail: decode(html).replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').slice(0, 220) });
    else if (r.status === 302 && role !== 'guest' && /Account\/Login|dang-nhap/i.test(r.location || '')) issues.push({ role, url, kind: 'chuyen ve dang nhap', detail: r.location });
    if (ms > 5000) issues.push({ role, url, kind: 'cham', detail: ms + ' ms' });
    if (r.status === 200 && /Không thể hoàn tất yêu cầu|Đã có lỗi xảy ra|An unhandled exception|NullReferenceException|SqlException/.test(decode(html))) issues.push({ role, url, kind: 'trang bao loi', detail: (decode(html).match(/<h1[^>]*>([^<]*)/) || [])[1] || '' });
    if (r.status === 200) for (const m of html.matchAll(/href="([^"]+)"/g)) {
      let href = decode(m[1]); if (!href.startsWith('/') || href.startsWith('//')) continue; href = href.split('#')[0]; if (!href || SKIP.test(href)) continue;
      if (!seen.has(href)) queue.push(href);
    }
    for (const i of issues.slice(before)) console.log(`[${i.role}] ${i.kind} ${i.url} :: ${String(i.detail).slice(0, 160)}`);
  }
  return { role, visited, issues };
}

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { } if (n > 90) throw Error('not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const roles = [
    ['guest', null, ['/', '/Discovery', '/diem-den', '/Discovery?Q=', '/kham-pha', '/lich-mau', '/Account/Login', '/Account/Register']],
    ['traveler', 'traveler', ['/bang-tin', '/Discovery', '/diem-den', '/Plans', '/lapkehoach', '/Bookings', '/tin-nhan', '/thong-bao', '/Account/Profile', '/ban-be', '/Saved', '/Reviews/Mine', '/Support', '/lich-mau', '/bang-tin?tab=da-luu']],
    ['provider', 'provider', ['/bang-tin', '/Provider', '/Bookings', '/nha-cung-cap/co-so', '/nha-cung-cap/tin-nhan', '/nha-cung-cap/tin-nhan/cai-dat', '/thong-bao', '/Account/Profile', '/Discovery/Manage']],
    ['admin', 'admin', ['/Admin', '/Admin/ServiceReviews', '/Admin/Verifications', '/Admin/Providers', '/Admin/Audit', '/quan-tri/co-so/duyet', '/quan-tri/bao-cao-bai-viet', '/quan-tri/bao-cao-chat', '/quan-tri/lich-mau', '/quan-tri/loai-dich-vu', '/quan-tri/khach-du-lich', '/Discovery/Manage', '/thong-bao']]
  ];
  const report = [];
  for (const [role, label, starts] of roles) {
    if (ONLY && !ONLY.includes(role)) continue;
    const c = new Client(); if (label) { await c.login(user(label).email, 'BranchTest@2026'); }
    const res = await crawl(role, c, starts); report.push(res);
    console.log(`${role}: ${res.visited.length} trang, ${res.issues.length} van de`);
    await new Promise(r => setTimeout(r, 15000));
  }
  fs.writeFileSync(process.env.CRAWL_OUT || 'tests/crawl-roles-results.json', JSON.stringify(report.map(r => ({ role: r.role, pages: r.visited.length, issues: r.issues, slow: r.visited.filter(v => v.ms > 3000) })), null, 1));
})().catch(e => { console.error(e); process.exit(1); });
