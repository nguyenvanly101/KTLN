// Duyet nhanh website: dang nhap tung vai tro, lay cac lien ket noi bo tu trang chinh roi mo tung trang, bao loi HTTP / trang loi.
// Chay: FULLTEXT_TEST_BASE=http://localhost:5216 node tests/_crawl-review.cjs
const { Client, base, decode } = require('./fulltext-smoke.cjs');
const roles = [
  ['khach', 'khach@hanhtrinh.vn', ['HanhTrinh@2026', 'HanhTrinhMoi@2026'], ['/', '/Plans', '/kham-pha', '/Saved', '/Bookings', '/tin-nhan', '/lapkehoach']],
  ['nha-cung-cap', 'nhacungcap@hanhtrinh.vn', ['HanhTrinh@2026', 'HanhTrinhMoi@2026'], ['/', '/nha-cung-cap/co-so', '/tin-nhan', '/Discovery/Manage']],
  ['quan-tri', 'admin@hanhtrinh.vn', ['HanhTrinh@2026', 'HanhTrinhMoi@2026'], ['/', '/quan-tri/loai-dich-vu', '/Discovery/Manage', '/quan-tri/xac-minh/mo-danh-sach']],
];
const skip = /dang-xuat|logout|Delete|xoa|huy|\/api\/|\.(css|js|png|jpg|svg|ico|woff2?)(\?|$)|^#|mailto:|tel:|javascript:/i;
(async () => {
  const bad = [], seenAll = new Set(); let total = 0;
  for (const [name, email, pws, starts] of roles) {
    const c = new Client(); let ok = false;
    for (const pw of pws) { try { await c.login(email, pw); ok = true; break; } catch { } }
    if (!ok) { bad.push(`[${name}] khong dang nhap duoc`); continue; }
    const links = new Set();
    for (const s of starts) {
      const r = await c.get(s); total++;
      if (r.status >= 400) bad.push(`[${name}] ${r.status} ${s}`);
      for (const m of r.html.matchAll(/href="(\/[^"#?]*(?:\?[^"#]*)?)"/g)) { const h = decode(m[1]); if (!skip.test(h)) links.add(h); }
    }
    for (const l of [...links].slice(0, 90)) {
      if (seenAll.has(name + l)) continue; seenAll.add(name + l);
      const r = await c.get(l); total++;
      const t = r.text || '';
      if (r.status >= 500) bad.push(`[${name}] ${r.status} ${l}`);
      else if (r.status === 404) bad.push(`[${name}] 404 ${l}`);
      else if (r.status >= 400 && r.status !== 403) bad.push(`[${name}] ${r.status} ${l}`);
      else if (/Unhandled exception|NullReferenceException|InvalidOperation|Stack trace/i.test(t)) bad.push(`[${name}] loi hien tren trang ${l}`);
    }
  }
  console.log(`Da mo ${total} trang.`);
  console.log(bad.length ? 'VAN DE:\n' + [...new Set(bad)].join('\n') : 'Khong co trang loi.');
})().catch(e => { console.error(e); process.exit(1); });
