// Ho tro khi dang di tren trang chuyen di: dai "Dang di", khung giup, nut "Co van de" tung hang muc, ghi nhanh su co, tim noi giup theo toa do.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/trip-help.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, base, decode } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
const user = label => fixture.users.find(x => x.label === label);
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const day = n => new Date(Date.now() + n * 864e5).toISOString().slice(0, 10);
const stamp = Date.now().toString().slice(-7);
const api = async (c, token, path, method = 'GET', body) => {
  const r = await fetch(base + path, { method, redirect: 'manual', headers: { cookie: [...c.cookies].map(([k, v]) => k + '=' + v).join('; '), RequestVerificationToken: token, ...(body ? { 'content-type': 'application/json' } : {}) }, body: body ? JSON.stringify(body) : undefined });
  let j = null; try { j = await r.json(); } catch { } return { status: r.status, body: j };
};

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { } if (n > 45) throw Error('not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const t = new Client(), guest = new Client(), stranger = new Client();
  const email = `th-nguoila-${stamp}@example.com`;
  sql(database, `INSERT dbo.TaiKhoan(Email,SoDienThoai,MatKhauBam,VaiTro,TrangThai,DauPhien,PhaiDoiMatKhau,ThoiDiemTao) SELECT N'${email}',NULL,MatKhauBam,VaiTro,TrangThai,NEWID(),0,SYSUTCDATETIME() FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}';`);
  const sid = json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${email}' FOR JSON PATH;`)[0].Id;
  sql(database, `INSERT dbo.HoSoNguoiDung(MaTaiKhoan,TenHienThi,ThongTinChiaSe,ThoiDiemCapNhat) VALUES(${sid},N'Nguoi la ${stamp}','CongKhai',SYSUTCDATETIME());`);
  await t.login(user('traveler').email, 'BranchTest@2026'); await stranger.login(email, 'BranchTest@2026');
  const page = await t.get('/lapkehoach'); const tk = val(page, '__RequestVerificationToken');
  const draft = { notes: [], id: 0, name: 'QA Ho tro ' + stamp, destination: 'Hội An', start: day(0), days: 2, budget: 3000000, items: [
    { id: 0, day: 1, time: '09:00', content: 'Nhận phòng homestay', cost: 500000 }, { id: 0, day: 1, time: '12:00', content: 'Ăn trưa cao lầu', cost: 60000 }] };
  const created = await api(t, tk, '/api/lapkehoach', 'POST', draft); const id = created.body.id;
  check(created.status === 200 && id > 0, 'Tao chuyen di dang di (bat dau hom nay)');

  // 1. Trang chuyen di
  const d = await t.get('/Plans/Detail/' + id); const h = decode(d.html);
  check(d.status === 200 && h.includes('class="trip-sos"') && h.includes('Bạn đang trong chuyến đi · Ngày 1') && h.includes('data-help-open'), 'Dai Dang di hien tren trang chuyen di');
  check(h.includes('id="th-dialog"') && h.includes('/js/trip-help.js') && h.includes('__RequestVerificationToken'), 'Co khung giup, tep script va ma chong gia mao');
  check((h.match(/class="trip-issue-btn"/g) || []).length === 2 && h.includes('data-name="Ăn trưa cao lầu"'), 'Moi hang muc co nut Co van de');
  const js = await fetch(base + '/js/trip-help.js'); const jt = await js.text();
  check(js.status === 200 && ['matvi', 'matgiayto', 'lac', 'thientai', 'lacnhom', 'BenhVien', 'NhaThuoc', 'CongAn', 'Atm', 'SuaXe'].every(k => jt.includes(k)), 'Script co du tinh huong va noi giup');
  const future = await api(t, tk, '/api/lapkehoach', 'POST', { ...draft, name: 'QA Tuong lai ' + stamp, start: day(10) });
  const fh = decode((await t.get('/Plans/Detail/' + future.body.id)).html);
  check(fh.includes('Khi đang trên đường') && !fh.includes('Bạn đang trong chuyến đi'), 'Chuyen chua toi ngay dung tieu de khac');

  // 2. Ghi nhanh su co
  const types = ['DiChuyenGapSuCo', 'LuuTruCoVanDe', 'SucKhoeCaNhan', 'MatGiayToTaiSan', 'AnToanCaNhan', 'NhomDiCung', 'HetTienNganSach', 'ThoiTiet', 'DichVuCoVanDe', 'Khac'];
  for (const type of types) check((await api(t, tk, '/api/lapkehoach/su-co', 'POST', { planId: id, type, description: 'Thu ' + type })).status === 200, 'Ghi su co loai ' + type);
  const act = json(database, `SELECT TOP 1 MaHoatDong A FROM dbo.HoatDongLichTrinh WHERE MaKeHoach=${id} ORDER BY MaHoatDong FOR JSON PATH;`)[0].A;
  check((await api(t, tk, '/api/lapkehoach/su-co', 'POST', { planId: id, type: 'Khac', description: 'Gan hang muc', activityId: act })).status === 200 && json(database, `SELECT COUNT(*) N FROM dbo.TinhHuongHoatDong x JOIN dbo.TinhHuongPhatSinh s ON s.MaTinhHuong=x.MaTinhHuong WHERE s.MaKeHoach=${id} FOR JSON PATH;`)[0].N === 1, 'Su co gan voi hang muc cu the');
  check(json(database, `SELECT COUNT(*) N FROM dbo.TinhHuongPhatSinh WHERE MaKeHoach=${id} FOR JSON PATH;`)[0].N === 11, 'Cac su co da luu vao chuyen di');
  const sup = decode((await t.get('/Support?planId=' + id)).html);
  check(sup.includes('Sức khỏe, tai nạn') && sup.includes('Mất giấy tờ, tiền, đồ đạc') && sup.includes('Nhóm đi cùng'), 'Danh sach su co hien ten loai moi');
  check((await api(t, tk, '/api/lapkehoach/su-co', 'POST', { planId: id, type: 'LoaiLa', description: 'x' })).status >= 400, 'Loai su co la bi tu choi');
  check((await api(t, tk, '/api/lapkehoach/su-co', 'POST', { planId: id, type: 'Khac', description: '' })).status >= 400, 'Mo ta rong bi tu choi');
  check((await api(t, '', '/api/lapkehoach/su-co', 'POST', { planId: id, type: 'Khac', description: 'x' })).status === 400, 'Thieu ma chong gia mao bi tu choi');
  check((await api(stranger, tk, '/api/lapkehoach/su-co', 'POST', { planId: id, type: 'Khac', description: 'x' })).status >= 400, 'Nguoi la khong ghi duoc su co vao chuyen di cua nguoi khac');
  check((await api(t, tk, '/api/lapkehoach/su-co', 'POST', { planId: id, type: 'Khac', description: 'x', activityId: 999999999 })).status >= 400, 'Hang muc khong thuoc chuyen di bi tu choi');

  // 3. Tim noi giup theo toa do
  const near = await api(t, '', '/api/lapkehoach/gan-day?kind=BenhVien&lat=15.88&lng=108.33');
  check(near.status === 200 && Array.isArray(near.body), 'API gan-day tra ve danh sach (rong neu chua co khoa Goong)');
  check((await api(t, '', '/api/lapkehoach/gan-day?kind=BenhVien&lat=50&lng=10')).body.length === 0, 'Toa do ngoai Viet Nam tra rong');
  check((await api(t, '', '/api/lapkehoach/gan-day?kind=KhongCo&lat=15.88&lng=108.33')).body.length === 0, 'Loai la tra rong');
  check((await guest.get('/api/lapkehoach/gan-day?kind=BenhVien&lat=15.88&lng=108.33')).status >= 300, 'Khach chua dang nhap khong goi duoc gan-day');

  // don dep
  const ids = [id, future.body.id];
  sql(database, `DELETE FROM dbo.TinhHuongHoatDong WHERE MaTinhHuong IN (SELECT MaTinhHuong FROM dbo.TinhHuongPhatSinh WHERE MaKeHoach IN (${ids})); DELETE FROM dbo.TinhHuongPhatSinh WHERE MaKeHoach IN (${ids}); DELETE FROM dbo.HoatDongLichTrinh WHERE MaKeHoach IN (${ids}); DELETE FROM dbo.KeHoachChuyenDi WHERE MaKeHoach IN (${ids});`);
  console.log(`PASS ${results.length} trip-help checks.`);
})().catch(e => { console.error(e); process.exit(1); });
