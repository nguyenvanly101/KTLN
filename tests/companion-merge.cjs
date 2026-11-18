// Tim ban dong hanh gop ve Bang tin: bai "Tim ban dong hanh" -> nhan tin -> moi vao chuyen di; trang Dong hanh va nhom Cung di cu duoc chuyen huong.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/companion-merge.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, base, decode } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
const user = label => fixture.users.find(x => x.label === label);
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const day = n => new Date(Date.now() + n * 864e5 + 7 * 36e5).toISOString().slice(0, 10); // ngay theo gio Viet Nam
const stamp = Date.now().toString().slice(-7);
const cookie = c => [...c.cookies].map(([k, v]) => k + '=' + v).join('; ');
const fd = v => { const f = new FormData(); for (const [k, x] of Object.entries(v)) f.append(k, String(x)); return f; };
const api = async (c, token, method, path, body, isJson) => {
  const r = await fetch(base + path, { method, redirect: 'manual', headers: { cookie: cookie(c), ...(method !== 'GET' ? { RequestVerificationToken: token } : {}), ...(isJson ? { 'content-type': 'application/json' } : {}) }, body: isJson ? JSON.stringify(body) : body });
  let j = null; if ((r.headers.get('content-type') || '').includes('json')) j = await r.json(); else await r.arrayBuffer();
  return { status: r.status, body: j, location: r.headers.get('location') };
};

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { } if (n > 45) throw Error('not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const t = new Client(), other = new Client(), guest = new Client(), admin = new Client();
  const email = `cm-nguoila-${stamp}@example.com`;
  sql(database, `INSERT dbo.TaiKhoan(Email,SoDienThoai,MatKhauBam,VaiTro,TrangThai,DauPhien,PhaiDoiMatKhau,ThoiDiemTao) SELECT N'${email}',NULL,MatKhauBam,VaiTro,TrangThai,NEWID(),0,SYSUTCDATETIME() FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}';`);
  const sid = json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${email}' FOR JSON PATH;`)[0].Id;
  sql(database, `INSERT dbo.HoSoNguoiDung(MaTaiKhoan,TenHienThi,ThongTinChiaSe,ThoiDiemCapNhat) VALUES(${sid},N'Ban dong hanh ${stamp}','CongKhai',SYSUTCDATETIME());`);
  await t.login(user('traveler').email, 'BranchTest@2026'); await other.login(email, 'BranchTest@2026'); await admin.login(user('admin').email, 'BranchTest@2026');
  const tk = val(await t.get('/lapkehoach'), '__RequestVerificationToken'), ok = val(await other.get('/lapkehoach'), '__RequestVerificationToken');
  let planId = 0, postId = 0;
  try {
    // 1. Chua co bai tim ban, chua la ban be: khong nhan tin duoc
    check((await t.get('/tin-nhan/nguoi/' + sid)).status === 403, 'Nguoi la chua co bai tim ban thi khong nhan tin duoc');

    // 2. Dang bai "Tim ban dong hanh" tren Bang tin
    const dest = 'Dong hanh thu ' + stamp;
    const post = await api(other, ok, 'POST', '/api/mxh/bai-viet', fd({ Kind: 'Companion', Visibility: 'Public', Destination: dest, TripStart: day(10), TripEnd: day(12), Content: 'Tim ban di cung, thich di bo va an uong.' }));
    postId = post.body && post.body.id;
    check(post.status === 200 && postId > 0, 'Dang bai Tim ban dong hanh tren Bang tin');
    const feed = await api(t, tk, 'GET', '/api/mxh/bai-viet?scope=companion&q=' + encodeURIComponent(dest));
    check(feed.status === 200 && JSON.stringify(feed.body).includes(dest), 'Tab tim ban dong hanh cua Bang tin tim thay bai theo diem den');

    // 3. Nhan tin duoc voi nguoi dang bai du chua la ban be
    check((await t.get('/tin-nhan/nguoi/' + sid)).status === 200, 'Nhan tin duoc voi nguoi dang co bai tim ban cong khai');
    const found = await api(t, tk, 'GET', '/chat/tim?q=' + encodeURIComponent('Ban dong hanh ' + stamp));
    check(found.status === 200 && found.body.people.some(p => p.id === sid), 'Tim nguoi de nhan tin thay nguoi dang bai tim ban');

    // 4. Moi vao chuyen di ngay trong chat
    // Danh sach chon chuyen trong chat toi da 30 chuyen sap toi: day cac chuyen thu con sot tu lan chay truoc ve qua khu.
    sql(database, `UPDATE k SET NgayBatDau=DATEADD(year,-1,NgayBatDau),NgayKetThuc=DATEADD(year,-1,NgayKetThuc) FROM dbo.KeHoachChuyenDi k JOIN dbo.TaiKhoan a ON a.MaTaiKhoan=k.MaTaiKhoan WHERE a.Email=N'${user('traveler').email}' AND (k.TenKeHoach LIKE N'QA %' OR k.TenKeHoach LIKE N'Lịch thử %') AND k.NgayKetThuc>=CAST(GETDATE() AS date);`);
    const created = await api(t, tk, 'POST', '/api/lapkehoach', { notes: [], id: 0, name: 'QA Moi ' + stamp, destination: 'Hội An', start: day(10), days: 2, budget: 2000000, items: [] }, true);
    planId = created.body && created.body.id; check(created.status === 200 && planId > 0, 'Tao chuyen di de moi');
    const opts = await api(t, tk, 'GET', `/chat/nguoi/${sid}/chuyen-di`);
    check(opts.status === 200 && opts.body.some(p => p.id === planId && p.state === 'can'), 'Chat liet ke chuyen di co the moi nguoi nay');
    check((await api(t, tk, 'POST', '/chat/moi-chuyen-di', fd({ planId, accountId: sid }))).status === 403, 'Chua tro chuyen va chua la ban be thi chua moi duoc');
    const first = await api(t, tk, 'POST', '/chat/gui', fd({ PeerId: sid, Body: 'Chao ban, minh cung di dip nay.' }));
    check(first.status === 200 && first.body.conversationId > 0, 'Gui tin nhan dau tien cho nguoi dang bai');
    check((await api(t, tk, 'POST', '/chat/moi-chuyen-di', fd({ planId, accountId: sid }))).status === 200, 'Sau khi tro chuyen thi moi duoc vao chuyen di');
    check(json(database, `SELECT Status S FROM dbo.ThanhVienChuyenDi WHERE PlanId=${planId} AND AccountId=${sid} FOR JSON PATH;`)[0].S === 'Pending', 'Loi moi duoc ghi o trang thai cho tra loi');
    check((await api(t, tk, 'GET', `/chat/nguoi/${sid}/chuyen-di`)).body.some(p => p.id === planId && p.state === 'pending'), 'Chuyen di da moi hien la dang cho tra loi');
    check((await api(t, tk, 'POST', '/chat/moi-chuyen-di', fd({ planId, accountId: sid }))).status === 409, 'Moi lan hai bi tu choi');
    check((await api(other, ok, 'POST', '/chat/moi-chuyen-di', fd({ planId, accountId: sid }))).status >= 400, 'Nguoi khong phai chu chuyen di khong moi duoc');
    const plans = await other.get('/Plans');
    check(decode(plans.html).includes('QA Moi ' + stamp), 'Nguoi duoc moi thay loi moi trong muc Chuyen di');
    await other.post('/Plans/RespondInvite', plans, { planId, accept: 'true' });
    check(json(database, `SELECT Status S FROM dbo.ThanhVienChuyenDi WHERE PlanId=${planId} AND AccountId=${sid} FOR JSON PATH;`)[0].S === 'Accepted', 'Chap nhan loi moi thi thanh thanh vien chuyen di');
    check((await other.get('/Plans/Detail/' + planId)).status === 200, 'Thanh vien mo duoc chuyen di chung');

    // 5. Hai luong cu duoc chuyen huong ve Bang tin
    const target = '/bang-tin?tab=dong-hanh';
    for (const path of ['/Companions', '/Companions/Mine', '/Companions/Connections', '/Together', '/Together/Mine', '/dong-hanh/tim-kiem/mo-tim-kiem'])
      { const r = await t.get(path); check(r.status === 302 && r.location === target, 'Dia chi cu ' + path + ' chuyen ve Bang tin'); }
    check((await guest.get('/Companions')).location === target, 'Khach vang lai mo trang Dong hanh cu cung duoc chuyen huong');
    check((await admin.get('/Together/Moderation')).location === '/Admin', 'Quan tri vien mo trang bao cao dong hanh cu duoc dua ve Tong quan');
    check((await api(t, tk, 'POST', '/Companions/Connect', fd({ Id: 1 }))).status === 302, 'Thao tac ghi cua luong cu khong con chay');

    // 6. Giao dien khong con loi vao cu
    const home = decode((await guest.get('/')).html + (await t.get('/Explore')).html);
    check(!home.includes('href="/Companions') && !home.includes('href="/Together'), 'Trang chu khong con lien ket toi trang Dong hanh cu');
    const mine = decode((await t.get('/Plans')).html);
    check(!mine.includes('/Companions') && mine.includes(target), 'Muc Chuyen di tro toi tab tim ban cua Bang tin');
    const detail = decode((await t.get('/Plans/Detail/' + planId)).html);
    check(!detail.includes('/Together/Edit') && detail.includes('#trip-members'), 'Trang chuyen di moi ban qua phan thanh vien, khong qua nhom Cung di');
    const inbox = decode((await t.get('/tin-nhan')).html);
    check(inbox.includes('Mời vào chuyến đi') && !inbox.includes('Gửi lời rủ'), 'Khung chat co nut Moi vao chuyen di');
    const pjs = await (await fetch(base + '/js/posts.js')).text(), cjs = await (await fetch(base + '/js/chat.js')).text();
    check(pjs.includes('tab=dong-hanh') && cjs.includes('/chat/moi-chuyen-di') && !cjs.includes("'/chat/ru'"), 'Script mo thang tab tim ban va gui loi moi chuyen di');
  } finally {
    if (planId) sql(database, `DELETE FROM dbo.ThanhVienChuyenDi WHERE PlanId=${planId};`);
    if (postId) sql(database, `UPDATE dbo.BaiViet SET DeletedAt=SYSUTCDATETIME() WHERE Id=${postId};`);
  }
  console.log(`PASS ${results.length} companion-merge checks.`);
})().catch(e => { console.error(e); process.exit(1); });
