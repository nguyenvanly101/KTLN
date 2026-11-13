// Khung chat noi o moi trang + che do nhung + "Tin nhan moi" (tim ban be / co so).
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5225 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/chat-dock.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, base } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
assert.equal(fixture.database, database);
const user = label => fixture.users.find(x => x.label === label);
const results = [];
function check(ok, name) { assert.ok(ok, name); results.push(name); }
const cookie = c => [...c.cookies].map(([k, v]) => k + '=' + v).join('; ');
async function api(client, method, path) {
  const r = await fetch(base + path, { method, headers: { cookie: cookie(client) }, redirect: 'manual' });
  let body = null; if ((r.headers.get('content-type') || '').includes('json')) body = await r.json(); else await r.arrayBuffer();
  return { status: r.status, body };
}
const stamp = Date.now().toString().slice(-8);

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { /* cho */ } if (n > 45) throw Error('Server not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const id = email => json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${email}' FOR JSON PATH;`)[0].Id;
  const A = id(user('traveler').email);
  const posterRow = json(database, "SELECT TOP(1) a.MaTaiKhoan Id,a.Email FROM dbo.NhuCauDongHanh n JOIN dbo.TaiKhoan a ON a.MaTaiKhoan=n.MaTaiKhoan WHERE a.VaiTro='KHACH_DU_LICH' AND a.TrangThai='HoatDong' ORDER BY n.MaNhuCau FOR JSON PATH;")[0];
  const B = posterRow.Id;
  const bName = json(database, `SELECT TenHienThi N FROM dbo.HoSoNguoiDung WHERE MaTaiKhoan=${B} FOR JSON PATH;`)[0].N;
  const strangerEmail = `dock-x-${stamp}@example.com`, strangerName = 'Người Xa Lạ ' + stamp;
  sql(database, `INSERT dbo.TaiKhoan(Email,SoDienThoai,MatKhauBam,VaiTro,TrangThai,DauPhien,PhaiDoiMatKhau,ThoiDiemTao) SELECT N'${strangerEmail}',NULL,MatKhauBam,VaiTro,TrangThai,NEWID(),0,SYSUTCDATETIME() FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}';`);
  const X = id(strangerEmail);
  sql(database, `INSERT dbo.HoSoNguoiDung(MaTaiKhoan,TenHienThi,ThongTinChiaSe,ThoiDiemCapNhat) VALUES(${X},N'${strangerName}','CongKhai',SYSUTCDATETIME());` +
    `DELETE FROM dbo.KetBan WHERE (RequesterId=${A} AND AddresseeId=${B}) OR (RequesterId=${B} AND AddresseeId=${A}); INSERT dbo.KetBan(RequesterId,AddresseeId,Status,RespondedAt) VALUES(${A},${B},'Accepted',SYSUTCDATETIME());`);
  const branch = json(database, "SELECT TOP(1) Id,Name FROM dbo.CoSoDichVu WHERE Status='Approved' AND PublishedVersion IS NOT NULL ORDER BY Id FOR JSON PATH;")[0];

  const a = new Client(), provider = new Client(), guest = new Client();
  await a.login(user('traveler').email, 'BranchTest@2026');
  await provider.login(user('provider').email, 'BranchTest@2026');

  // 1. Khung chat noi
  const home = await a.get('/Discovery');
  check(home.html.includes('chat-dock.js') && home.html.includes('chat-badge.js'), 'Khach da dang nhap: moi trang nap khung chat noi');
  check((await provider.get('/nha-cung-cap/co-so')).html.includes('chat-dock.js'), 'Nha cung cap cung co khung chat noi');
  check(!(await guest.get('/Discovery')).html.includes('chat-dock.js'), 'Khach vang lai khong co khung chat noi');
  const branchPage = await guest.get('/co-so/' + branch.Id);
  check(branchPage.html.includes('data-branch-chat') && !branchPage.html.includes('chat-dock.js'), 'Trang co so cong khai van co nut Nhan tin (khong khung noi cho khach vang lai)');
  const aBranch = await a.get('/co-so/' + branch.Id);
  check(aBranch.html.includes(`href="/tin-nhan/co-so/${branch.Id}"`) && aBranch.html.includes('chat-dock.js'), 'Nut "Nhan tin" tren trang co so tro toi /tin-nhan/co-so (se duoc khung noi chan lai)');
  const js = await fetch(base + '/js/chat-dock.js'); const code = await js.text();
  check(js.status === 200 && code.includes('data-chat-dock-frame') && code.includes("embed=1") && code.includes('sessionStorage'), 'Ma khung noi: iframe nhung, nho trang thai trong phien');

  // 2. Che do nhung
  const full = await a.get('/tin-nhan');
  const emb = await a.get('/tin-nhan?embed=1');
  check(full.html.includes('workspace-sidebar') && !full.html.includes('is-embed'), 'Trang /tin-nhan day du co thanh ben');
  check(emb.status === 200 && emb.html.includes('is-embed') && emb.html.includes('data-chat-app') && emb.html.includes('<base target="_top"') && !emb.html.includes('workspace-sidebar') && !emb.html.includes('chat-dock.js'), 'Che do nhung: khong thanh ben, co hop thu, lien ket mo ra trang goc');
  check(emb.html.includes('data-chat-open-compose') && emb.html.includes('data-chat-compose-q'), 'Hop thu co nut "Tin nhan moi" va o tim');
  const embBranch = await a.get(`/tin-nhan/co-so/${branch.Id}?embed=1`);
  check(embBranch.status === 200 && embBranch.html.includes('is-embed') && embBranch.html.includes('id="chat-start"'), 'Mo chat voi co so trong khung nhung');
  const probe = await fetch(base + '/tin-nhan?embed=1', { headers: { cookie: cookie(a) } });
  const xfo = probe.headers.get('x-frame-options'), csp = probe.headers.get('content-security-policy') || '';
  check(xfo === 'SAMEORIGIN' && csp.includes("frame-ancestors 'self'") && csp.includes("frame-src 'self'"), 'Chi cho nhung cung nguon (SAMEORIGIN, frame-ancestors self)');

  // 3. Tin nhan moi: tim ban be / co so
  check((await api(a, 'GET', '/chat/tim?q=a')).status === 422, 'Tim voi it hon 2 ky tu bi tu choi');
  const byBranch = (await api(a, 'GET', '/chat/tim?q=' + encodeURIComponent(branch.Name))).body;
  check(byBranch.branches.some(b => b.id === branch.Id), 'Tim theo ten co so');
  const byFriend = (await api(a, 'GET', '/chat/tim?q=' + encodeURIComponent(bName.slice(0, 4)))).body;
  check(byFriend.people.some(p => p.id === B && p.relation === 'friends'), 'Tim theo ten ban be');
  const byStranger = (await api(a, 'GET', '/chat/tim?q=' + encodeURIComponent('Xa Lạ ' + stamp))).body;
  check(byStranger.people.every(p => p.id !== X), 'Nguoi la khong ban be, khong co loi ru thi khong nhan tin duoc nen khong hien');
  check(!JSON.stringify(byFriend).includes('@'), 'Ket qua tim khong lo email');
  check((await api(provider, 'GET', '/chat/tim?q=abc')).status === 403, 'Nha cung cap khong dung "Tin nhan moi"');
  check([302, 401].includes((await api(guest, 'GET', '/chat/tim?q=abc')).status), 'Khach vang lai khong tim duoc');

  console.log(`PASS ${results.length} chat-dock checks.`);
})().catch(e => { console.error(e); process.exit(1); });
