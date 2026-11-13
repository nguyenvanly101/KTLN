// Loai tin: chia se, tim ban dong hanh (khach) va tin gioi thieu co so (nha cung cap).
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5226 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/post-kinds.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, base, decode } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
const user = label => fixture.users.find(x => x.label === label);
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const cookie = c => [...c.cookies].map(([k, v]) => k + '=' + v).join('; ');
async function api(client, token, method, path, form) {
  const r = await fetch(base + path, { method, headers: { cookie: cookie(client), ...(method !== 'GET' ? { RequestVerificationToken: token } : {}) }, body: form, redirect: 'manual' });
  const type = r.headers.get('content-type') || ''; let body = null; if (type.includes('json')) body = await r.json(); else await r.arrayBuffer();
  return { status: r.status, body };
}
const fd = values => { const f = new FormData(); for (const [k, v] of Object.entries(values)) if (v != null) f.append(k, String(v)); return f; };
const form = (token, values) => { const f = fd(values); f.append('__RequestVerificationToken', token); return f; };
const day = n => new Date(Date.now() + n * 864e5).toISOString().slice(0, 10);
const stamp = Date.now().toString().slice(-8);

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { } if (n > 45) throw Error('not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const traveler = new Client(), provider = new Client(), guest = new Client();
  await traveler.login(user('traveler').email, 'BranchTest@2026'); await provider.login(user('provider').email, 'BranchTest@2026');
  const feed = await traveler.get('/bang-tin'); const tk = val(feed, '__RequestVerificationToken');

  // 1. Tin tim ban dong hanh
  const ok = await api(traveler, tk, 'POST', '/api/mxh/bai-viet', fd({ Content: 'Tìm bạn đi biển ' + stamp, Visibility: 'Public', Kind: 'Companion', Destination: 'Đà Nẵng', TripStart: day(10), TripEnd: day(13) }));
  check(ok.status === 200 && ok.body.kind === 'Companion' && ok.body.destination === 'Đà Nẵng' && ok.body.tripStart === day(10), 'Tao tin tim ban dong hanh voi diem den va ngay');
  check((await api(traveler, tk, 'POST', '/api/mxh/bai-viet', fd({ Content: 'x', Visibility: 'Public', Kind: 'Companion', Destination: '', TripStart: day(10), TripEnd: day(13) }))).status === 422, 'Thieu diem den bi tu choi');
  check((await api(traveler, tk, 'POST', '/api/mxh/bai-viet', fd({ Content: 'x', Visibility: 'Public', Kind: 'Companion', Destination: 'Huế', TripStart: day(13), TripEnd: day(10) }))).status === 422, 'Ngay ve truoc ngay di bi tu choi');
  check((await api(traveler, tk, 'POST', '/api/mxh/bai-viet', fd({ Content: 'x', Visibility: 'Public', Kind: 'Companion', Destination: 'Huế', TripStart: day(-3), TripEnd: day(2) }))).status === 422, 'Ngay di trong qua khu bi tu choi');
  check((await api(traveler, tk, 'POST', '/api/mxh/bai-viet', fd({ Content: 'x', Visibility: 'Public', Kind: 'Branch' }))).status === 400, 'Khach khong the tu tao loai "Branch"');
  const normal = await api(traveler, tk, 'POST', '/api/mxh/bai-viet', fd({ Content: 'Bài thường ' + stamp, Visibility: 'Public' }));
  check(normal.status === 200 && normal.body.kind === 'Share', 'Bai thuong mac dinh la Chia se');

  // 2. Tin gioi thieu co so cua nha cung cap
  const branch = json(database, `SELECT TOP(1) b.Id FROM dbo.CoSoDichVu b JOIN dbo.NhaCungCapDichVu p ON p.MaNhaCungCap=b.ProviderId JOIN dbo.TaiKhoan t ON t.MaTaiKhoan=p.MaTaiKhoan WHERE t.Email=N'${user('provider').email}' AND b.Status='Approved' AND b.PublishedVersion IS NOT NULL ORDER BY b.Id FOR JSON PATH;`)[0].Id;
  const other = json(database, `SELECT TOP(1) b.Id FROM dbo.CoSoDichVu b JOIN dbo.NhaCungCapDichVu p ON p.MaNhaCungCap=b.ProviderId JOIN dbo.TaiKhoan t ON t.MaTaiKhoan=p.MaTaiKhoan WHERE t.Email<>N'${user('provider').email}' ORDER BY b.Id FOR JSON PATH;`)[0];
  const pg = await provider.get(`/nha-cung-cap/co-so/${branch}/tin`); check(pg.status === 200 && decode(pg.html).includes('Đăng tin'), 'Nha cung cap mo trang dang tin cua co so');
  const pt = val(pg, '__RequestVerificationToken'); const text = 'Ưu đãi mùa hè ' + stamp;
  const posted = await provider.req(`/nha-cung-cap/co-so/${branch}/tin`, form(pt, { Content: text }));
  check(posted.status === 302, 'Dang tin gioi thieu co so');
  const row = json(database, `SELECT TOP(1) * FROM dbo.BaiViet WHERE Content=N'${text}' FOR JSON PATH;`)[0];
  check(row && row.Kind === 'Branch' && row.BranchId === branch && row.Visibility === 'Public', 'Luu dung loai Branch, cong khai, gan co so');
  if (other) check((await provider.req(`/nha-cung-cap/co-so/${other.Id}/tin`, form(pt, { Content: 'xam nhap' }))).status === 404, 'Khong dang tin cho co so cua nguoi khac');
  check((await provider.req(`/nha-cung-cap/co-so/${branch}/tin`, form(pt, { Content: '' }))).status === 422, 'Tin rong bi tu choi');
  check((await traveler.get(`/nha-cung-cap/co-so/${branch}/tin`)).status >= 300, 'Khach khong vao duoc trang dang tin cua nha cung cap');

  // 3. Khach thay tin tren bang tin va trang co so
  const list = await api(traveler, tk, 'GET', '/api/mxh/bai-viet?scope=public');
  const mine = list.body.find(p => p.id === row.Id);
  check(mine && mine.kind === 'Branch' && mine.branchId === branch && mine.branchName && mine.author === mine.branchName, 'Bang tin cong dong hien tin co so voi ten co so');
  const page = await guest.get('/co-so/' + branch);
  check(page.text.includes(text), 'Trang cong khai cua co so hien tin moi');
  const like = await api(traveler, tk, 'POST', '/api/mxh/cam-xuc', fd({ type: 'Post', target: row.Id }));
  check(like.status === 200, 'Khach thich tin cua co so');

  // 4. Xoa
  check((await provider.req(`/nha-cung-cap/co-so/${branch}/tin/${row.Id}/xoa`, form(pt, {}))).status === 302, 'Nha cung cap xoa tin');
  check(!(await guest.get('/co-so/' + branch)).text.includes(text), 'Tin da xoa khong con hien');

  console.log(`PASS ${results.length} post-kinds checks.`);
})().catch(e => { console.error(e); process.exit(1); });
