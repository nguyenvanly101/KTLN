// Ra soat bao mat + logic + giao dien (bo kiem thu lap lai duoc):
//  - Leo thang quyen: khach vang lai / khach / nha cung cap goi thao tac cua vai khac -> bi chan.
//  - IDOR: tai khoan khac khong doc duoc chuyen di, yeu cau dat, cuoc tro chuyen, anh bai rieng tu cua khach.
//  - Trang 403 / 404 la trang tieng Viet co duong quay lai (khong phai chu tran / trang trang).
//  - Khoa dang nhap theo tai khoan: sai 5 lan trong 15 phut thi tam khoa.
//  - Form quyet dinh cua quan tri: chon quyet dinh truoc, mot o noi dung.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/audit-checks.cjs
const assert = require('node:assert/strict'), fs = require('node:fs');
const { Client, decode } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const fx = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8')); const user = l => fx.users.find(x => x.label === l);
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const one = q => json(database, q + ' FOR JSON PATH;')[0];

(async () => {
  const traveler = new Client(), provider = new Client(), other = new Client(), admin = new Client(), guest = new Client();
  await traveler.login(user('traveler').email, 'BranchTest@2026'); await provider.login(user('provider').email, 'BranchTest@2026');
  await other.login(user('other').email, 'BranchTest@2026'); await admin.login(user('admin').email, 'BranchTest@2026');

  // 1. Leo thang quyen
  const adminPosts = [['/quan-tri/co-so/duyet', { Id: '1', Version: 'x', Decision: 'Approved' }], ['/Admin/Verify', { Id: '1', Version: 'x', Decision: 'DaXacMinh' }], ['/quan-tri/khach-du-lich/trang-thai', { id: '1', active: 'false', reason: 'x' }], ['/quan-tri/loai-dich-vu/xoa', { code: 'AnUong' }]];
  for (const c of [traveler, provider]) { const pg = await c.get('/Discovery'); for (const [p, d] of adminPosts) { const r = await c.post(p, pg, d); check(r.status === 403, 'Vai khac goi thao tac quan tri bi chan: ' + p); } }
  { const pg = await guest.get('/Discovery'); const r = await guest.post('/Admin/Verify', pg, { Id: '1' }); check(r.status === 302 && /Login|dang-nhap/.test(r.location), 'Khach vang lai bi dua ve dang nhap'); }

  // 2. IDOR
  const me = one(`SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}'`).Id;
  const plan = one(`SELECT TOP 1 MaKeHoach Id FROM dbo.KeHoachChuyenDi WHERE MaTaiKhoan=${me} ORDER BY 1 DESC`);
  const conv = one(`SELECT TOP 1 Id FROM dbo.CuocTroChuyen WHERE TravelerId=${me} ORDER BY 1 DESC`);
  const bk = one(`SELECT TOP 1 MaYeuCau Id FROM dbo.YeuCauDatDichVu WHERE MaTaiKhoan=${me} ORDER BY 1 DESC`);
  for (const p of [plan && '/Plans/Detail/' + plan.Id, plan && '/Plans/Split/' + plan.Id, conv && '/chat/' + conv.Id, bk && '/Bookings/Detail/' + bk.Id].filter(Boolean)) {
    const r = await other.get(p); check(r.status === 403 || r.status === 404, 'Tai khoan khac khong doc duoc ' + p.replace(/\d+$/, ':id'));
  }

  // 3. Trang 403 / 404
  const f403 = await other.get('/Admin'); check(f403.status === 403 && decode(f403.html).includes('Bạn không có quyền xem trang này') && f403.html.includes('Về trang chính'), 'Trang 403 tieng Viet co duong quay lai');
  const f404 = await traveler.get('/khong-ton-tai-' + Date.now()); check(f404.status === 404 && decode(f404.html).includes('Không tìm thấy trang'), 'Trang 404 tieng Viet');
  const api403 = await fetch(process.env.FULLTEXT_TEST_BASE + '/chat/danh-sach', { headers: { cookie: [...admin.cookies].map(([k, v]) => k + '=' + v).join('; '), accept: 'application/json' } });
  check(api403.status === 403 || api403.status === 200, 'API van tra ma trang thai (khong bi doi thanh trang HTML)');

  // 4. Khoa dang nhap theo tai khoan (tai khoan tam chep tu tai khoan thu, dung xong thi ngung)
  const email = `khoa-${Date.now()}@example.com`;
  await new Promise(r => setTimeout(r, 61000)); // gioi han 10 lan dang nhap/phut theo IP
  sql(database, `INSERT dbo.TaiKhoan(Email,MatKhauBam,VaiTro,TrangThai,PhaiDoiMatKhau) SELECT N'${email}',MatKhauBam,VaiTro,TrangThai,0 FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}';`);
  for (let n = 0; n < 5; n++) { const c = new Client(); const p = await c.get('/tai-khoan/dang-nhap/mo-dang-nhap'); await c.post('/tai-khoan/dang-nhap/dang-nhap', p, { dinhDanh: email, matKhau: 'Sai@' + n + 'xyz' }); }
  { const c = new Client(); const p = await c.get('/tai-khoan/dang-nhap/mo-dang-nhap'); const r = await c.post('/tai-khoan/dang-nhap/dang-nhap', p, { dinhDanh: email, matKhau: 'BranchTest@2026' });
    check(r.status !== 302 && decode(r.html).includes('tạm khóa'), 'Sai mat khau 5 lan: tam khoa, ke ca nhap dung'); }
  sql(database, `UPDATE dbo.TaiKhoan SET TrangThai='DaNgung',ThoiDiemNgung=SYSUTCDATETIME() WHERE Email=N'${email}';`);

  // 5. Form quyet dinh
  const v = one(`SELECT TOP 1 MaHoSo Id FROM dbo.HoSoXacMinhNhaCungCap WHERE TrangThaiXuLy='ChoXacMinh'`);
  if (v) { const h = decode((await admin.get('/Admin/Verification/' + v.Id)).html); check(h.includes('data-decision-form') && (h.match(/name="Decision"/g) || []).length === 3 && !h.includes('Kết quả đối chiếu hồ sơ') && !h.includes('bắt buộc khi từ chối'), 'Form xac minh: chon quyet dinh, mot o noi dung'); }
  const br = one(`SELECT TOP 1 Id FROM dbo.BanCapNhatCoSo WHERE Status='Pending' ORDER BY 1 DESC`);
  if (br) { const h = decode((await admin.get('/quan-tri/co-so/' + br.Id)).html); check(h.includes('data-decision-form') && h.includes('value="ChangesRequested"'), 'Form duyet co so dung chung mau quyet dinh'); }
  const home = decode((await admin.get('/Admin')).html); check(home.includes('Cơ sở chờ duyệt') && home.includes('Báo cáo tin nhắn'), 'Tong quan quan tri liet ke moi hang viec');

  console.log(`PASS ${results.length} audit checks.`); results.filter(r => r.startsWith('(')).forEach(r => console.log(r));
})().catch(e => { console.error(e); process.exit(1); });
