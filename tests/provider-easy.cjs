// Nha cung cap de dung: dang nhanh (ten + vi tri roi bo sung sau) va cap nhat nhanh (gio, gia, lien he co hieu luc ngay, khong cho duyet).
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5225 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/provider-easy.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, base, decode } = require('./fulltext-smoke.cjs');
const { database, json } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
assert.equal(fixture.database, database);
const user = label => fixture.users.find(x => x.label === label);
const results = [];
function check(ok, name) { assert.ok(ok, name); results.push(name); }
const stamp = Date.now().toString().slice(-8);
const geoUnit = json(database, "SELECT TOP(1) Id FROM dbo.DonViHanhChinh WHERE Regime='Cu' AND [Level]=3 ORDER BY Id FOR JSON PATH;")[0].Id;
function form(token, values) { const f = new FormData(); f.append('__RequestVerificationToken', token); for (const [k, v] of Object.entries(values)) { if (v == null) continue; for (const item of Array.isArray(v) ? v : [v]) f.append(k, String(item)); } return f; }
const row = id => json(database, `SELECT * FROM dbo.CoSoDichVu WHERE Id=${id} FOR JSON PATH;`)[0];
const pending = id => json(database, `SELECT COUNT(*) N FROM dbo.BanCapNhatCoSo WHERE BranchId=${id} AND Status='Pending' FOR JSON PATH;`)[0].N;
const revisions = id => json(database, `SELECT COUNT(*) N FROM dbo.BanCapNhatCoSo WHERE BranchId=${id} FOR JSON PATH;`)[0].N;

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { /* cho */ } if (n > 45) throw Error('Server not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const provider = new Client(), admin = new Client(), guest = new Client();
  await provider.login(user('provider').email, 'BranchTest@2026'); await admin.login(user('admin').email, 'BranchTest@2026');

  // 1. Dang nhanh
  const page = await provider.get('/nha-cung-cap/co-so/them');
  const html = decode(page.html);
  check(html.includes('Lưu và tiếp tục') && /id="pv-s2"[^>]*hidden/.test(html) && /id="pv-s4"[^>]*hidden/.test(html), 'Co so moi: chi hien phan vi tri + nut "Luu va tiep tuc", cac phan con lai an');
  const token = val(page, '__RequestVerificationToken');
  const name = 'QA Dang nhanh ' + stamp;
  const quick = await provider.req('/nha-cung-cap/co-so/luu', form(token, { Id: 0, Version: '0', Operation: 'Draft', Name: name, Address: 'Địa chỉ thử ' + stamp, DonViId: geoUnit, Type: 'AnUong' }));
  check(quick.status === 302 && /^\/nha-cung-cap\/co-so\/\d+$/.test(quick.location), 'Luu nhanh chi voi ten, dia chi, khu vuc, loai: chuyen sang trang hoan thien');
  const id = Number(quick.location.split('/').pop());
  check(row(id).Status === 'Draft' && row(id).PublishedVersion == null && revisions(id) === 0, 'Co so o dang ban nhap, chua cong bo, chua gui duyet');
  check((await guest.get('/co-so/' + id)).status === 404, 'Ban nhap khong lo ra cong khai');
  const next = await provider.get(quick.location);
  check(next.status === 200 && decode(next.html).includes('Còn thiếu') && !/id="pv-s2"[^>]*hidden/.test(decode(next.html)), 'Trang hoan thien hien du 4 phan va danh sach "Con thieu"');

  // 2. Hoan thien + gui duyet + duyet
  const t2 = val(next, '__RequestVerificationToken'), version = val(next, 'Version');
  const full = (extra = {}) => ({ Id: id, Version: version, Operation: 'Submit', Name: name, Address: 'Địa chỉ thử ' + stamp, DonViId: geoUnit, Type: 'AnUong', Summary: 'Quán thử nghiệm để kiểm tra cập nhật nhanh và duyệt theo mức độ của nhà cung cấp.', OpenDayList: ['1', '2', '3', '4', '5', '6', '7'], OpenTime: '07:00', CloseTime: '21:00', PriceFrom: 30000, PriceUnit: 'người', Phone: '0939000444', LocationSource: 'Provider', Latitude: 16.07, Longitude: 108.21, ConfirmLocation: 'true', OwnershipRole: 'Chủ cơ sở', ...extra });
  check((await provider.req('/nha-cung-cap/co-so/luu', form(t2, full()))).status === 302, 'Bo sung va gui duyet lan dau');
  const revision = json(database, `SELECT TOP(1) Id FROM BanCapNhatCoSo WHERE BranchId=${id} ORDER BY Number DESC FOR JSON PATH;`)[0].Id;
  const review = await admin.get('/quan-tri/co-so/' + revision);
  await admin.post('/quan-tri/co-so/duyet', review, { Id: revision, Version: val(review, 'Version'), Decision: 'Approved' });
  check(row(id).Status === 'Approved' && row(id).PriceFrom === 30000, 'Quan tri duyet lan dau: co so duoc cong bo');

  // 3. Cap nhat nhanh: chi gio / gia / lien he / tien ich
  const before = revisions(id);
  const e1 = await provider.get('/nha-cung-cap/co-so/' + id);
  const fast = await provider.req('/nha-cung-cap/co-so/luu', form(val(e1, '__RequestVerificationToken'), { ...full({ PriceFrom: 45000, PriceTo: 90000, OpenTime: '06:30', CloseTime: '22:00', Phone: '0939000555', Parking: 'true' }), Version: val(e1, 'Version'), OwnershipRole: 'Chủ cơ sở' }));
  check(fast.status === 302, 'Gui thay doi gio/gia/lien he cho co so da duyet');
  const r1 = row(id);
  check(r1.PriceFrom === 45000 && r1.PriceTo === 90000 && r1.OpenTime.startsWith('06:30') && r1.Phone === '0939000555' && r1.Parking === true, 'Thay doi nhe co hieu luc ngay trong co so da cong bo');
  check(revisions(id) === before && pending(id) === 0, 'Khong tao ban cho duyet moi');
  const toast = await provider.get(fast.location);
  check(decode(toast.html).includes('Đã cập nhật ngay'), 'Thong bao "Da cap nhat ngay" thay vi "cho duyet"');
  const pub = await guest.get('/co-so/' + id);
  check(pub.text.includes('45.000') && pub.text.includes('06:30'), 'Khach thay ngay gia va gio moi');
  let entry = null;
  for (let n = 0; n < 45; n++) { entry = json(database, `SELECT TOP(1) PriceFrom P FROM dbo.DiscoveryEntry WHERE BranchId=${id} FOR JSON PATH;`)[0]; if (entry && entry.P === 45000) break; await new Promise(r => setTimeout(r, 3000)); }
  check(entry && entry.P === 45000, 'Chi muc tim kiem cap nhat gia moi');

  // 4. Thay doi nang: van phai duyet, ban cong bo khong doi
  const e2 = await provider.get('/nha-cung-cap/co-so/' + id);
  const heavy = await provider.req('/nha-cung-cap/co-so/luu', form(val(e2, '__RequestVerificationToken'), { ...full({ PriceFrom: 50000, Summary: 'Phần giới thiệu mới của quán, cần quản trị viên xem lại trước khi công bố cho khách.' }), PriceTo: 90000, OpenTime: '06:30', CloseTime: '22:00', Phone: '0939000555', Version: val(e2, 'Version') }));
  check(heavy.status === 302 && pending(id) === 1, 'Doi phan gioi thieu: tao ban cho duyet');
  const r2 = row(id);
  check(r2.Summary.includes('Quán thử nghiệm') && r2.PriceFrom === 45000, 'Ban dang cong bo giu nguyen (ke ca gia di kem) cho den khi duyet');
  const e3 = await provider.get('/nha-cung-cap/co-so/' + id);
  check((await provider.req('/nha-cung-cap/co-so/luu', form(val(e3, '__RequestVerificationToken'), { ...full({ PriceFrom: 60000 }), Version: val(e3, 'Version') }))).status >= 400, 'Dang cho duyet thi khong gui them duoc (quy tac cu giu nguyen)');
  const rev2 = json(database, `SELECT TOP(1) Id FROM BanCapNhatCoSo WHERE BranchId=${id} ORDER BY Number DESC FOR JSON PATH;`)[0].Id;
  const review2 = await admin.get('/quan-tri/co-so/' + rev2);
  await admin.post('/quan-tri/co-so/duyet', review2, { Id: rev2, Version: val(review2, 'Version'), Decision: 'Approved' });
  const r3 = row(id);
  check(r3.Summary.includes('Phần giới thiệu mới') && r3.PriceFrom === 50000, 'Sau khi duyet: gioi thieu va gia moi cung co hieu luc');

  console.log(`PASS ${results.length} provider-easy checks.`);
})().catch(e => { console.error(e); process.exit(1); });
