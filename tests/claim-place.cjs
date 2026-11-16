// Nha cung cap nhan mot dia diem co san trong Kham pha: trang dia diem co loi moi, bieu mau Them co so duoc dien san,
// va khi co so duoc duyet thi dia diem goc tu an (neu vi tri khop).
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/claim-place.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, decode, val } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
const user = label => fixture.users.find(x => x.label === label);
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const attr = (html, name) => { const m = new RegExp('<(?:input|textarea)[^>]*name="' + name + '"[^>]*>').exec(html); if (!m) return null; const v = /value="([^"]*)"/.exec(m[0]); return v ? v[1] : ''; };
const q = v => v == null ? 'NULL' : "N'" + String(v).replace(/'/g, "''") + "'";
const fd = (token, values) => { const f = new FormData(); f.append('__RequestVerificationToken', token); for (const [k, v] of Object.entries(values)) { if (v == null) continue; for (const item of Array.isArray(v) ? v : [v]) f.append(k, String(item)); } return f; };

(async () => {
  const guest = new Client(), provider = new Client(), traveler = new Client(), admin = new Client();
  await provider.login(user('provider').email, 'BranchTest@2026'); await traveler.login(user('traveler').email, 'BranchTest@2026'); await admin.login(user('admin').email, 'BranchTest@2026');
  const place = json(database, "SELECT TOP 1 Id,PlaceId,Title,Address,Latitude La,Longitude Lo,Region,Tags FROM dbo.DiscoveryEntry WHERE Kind=N'Place' AND Status='Published' AND ServiceId IS NULL AND BranchId IS NULL AND PlaceId IS NOT NULL ORDER BY Id FOR JSON PATH;")[0];
  check(place && place.Id > 0, 'Co dia diem da cong bo de thu');
  const restore = () => {
    sql(database, 'UPDATE dbo.DiscoveryEntry SET Address=' + q(place.Address) + ',Latitude=' + (place.La == null ? 'NULL' : place.La) + ',Longitude=' + (place.Lo == null ? 'NULL' : place.Lo) + ',Tags=' + (place.Tags == null ? "N''" : q(place.Tags)) + ' WHERE Id=' + place.Id + ';');
    sql(database, 'UPDATE dbo.DiaDiemDuLich SET DangHienThi=1 WHERE MaDiaDiem=' + place.PlaceId + ';');
  };
  try {
    sql(database, "UPDATE dbo.DiscoveryEntry SET Address=N'12 Duong Thu, Hoi An',Latitude=15.877000,Longitude=108.335000,Tags=N'Tham quan lưu trú khách sạn' WHERE Id=" + place.Id + ';');
    const service = json(database, "SELECT TOP 1 Id FROM dbo.DiscoveryEntry WHERE Kind=N'Service' AND Status='Published' ORDER BY Id FOR JSON PATH;")[0];

    // 1. Trang dia diem
    const url = '/Discovery/Entry/' + place.Id;
    const pv = decode((await provider.get(url)).html);
    check(pv.includes('id="claim"') && pv.includes('/nha-cung-cap/co-so/them?tu=' + place.Id) && pv.includes('Nhận địa điểm này'), 'Nha cung cap thay nut Nhan dia diem nay');
    const gv = decode((await guest.get(url)).html);
    check(gv.includes('id="claim"') && gv.includes('Đăng ký nhà cung cấp') && !gv.includes('/nha-cung-cap/co-so/them?tu='), 'Khach chua dang nhap duoc moi dang ky nha cung cap');
    const tv = decode((await traveler.get(url)).html);
    check(tv.includes('id="claim"') && tv.includes('Cần tài khoản nhà cung cấp') && !tv.includes('/nha-cung-cap/co-so/them?tu='), 'Khach du lich khong co nut nhan');
    if (service) { const sv = decode((await provider.get('/Discovery/Entry/' + service.Id)).html); check(!sv.includes('id="claim"'), 'Muc dich vu khong co khung nhan dia diem'); }

    // 2. Bieu mau dien san
    const form = await provider.get('/nha-cung-cap/co-so/them?tu=' + place.Id); const fh = decode(form.html);
    check(form.status === 200 && fh.includes('data-claimed') && fh.includes(place.Title), 'Bieu mau Them co so bao da dien san tu dia diem');
    check(attr(fh, 'Name') === place.Title, 'Ten co so duoc dien san');
    check(attr(fh, 'Address') === '12 Duong Thu, Hoi An', 'Dia chi duoc dien san');
    check(String(attr(fh, 'Latitude')).startsWith('15.877') && String(attr(fh, 'Longitude')).startsWith('108.335'), 'Toa do duoc dien san');
    check(attr(fh, 'ClaimedEntryId') === String(place.Id), 'Bieu mau mang theo ma dia diem dang nhan');
    check(fh.includes('Nhận từ địa điểm đã có trong Khám phá (mục #' + place.Id), 'Can cu ghi ro nguon tu Kham pha de quan tri vien doi chieu');
    const blank = decode((await provider.get('/nha-cung-cap/co-so/them')).html);
    check(!blank.includes('data-claimed') && (attr(blank, 'Name') || '') === '' && attr(blank, 'ClaimedEntryId') == null, 'Khong co tham so thi bieu mau de trong');
    const wrong = decode((await provider.get('/nha-cung-cap/co-so/them?tu=999999999')).html);
    check(!wrong.includes('data-claimed'), 'Ma dia diem la thi bieu mau de trong');
    if (service) { const sf = decode((await provider.get('/nha-cung-cap/co-so/them?tu=' + service.Id)).html); check(!sf.includes('data-claimed'), 'Khong nhan duoc muc dich vu cua nguoi khac'); }
    check((await traveler.get('/nha-cung-cap/co-so/them?tu=' + place.Id)).status >= 300, 'Khach du lich khong mo duoc bieu mau nhan dia diem');
    check((await guest.get('/nha-cung-cap/co-so/them?tu=' + place.Id)).status >= 300, 'Khach chua dang nhap bi chuyen sang dang nhap');
    check(json(database, "SELECT COUNT(*) N FROM dbo.DiscoveryEntry WHERE Id=" + place.Id + " AND Status='Published' FOR JSON PATH;")[0].N === 1, 'Mo bieu mau khong lam thay doi dia diem goc');

    // 3. Nhan, gui duyet, duyet: dia diem goc tu an khi vi tri khop
    const stamp = Date.now().toString().slice(-7);
    const geoUnit = json(database, "SELECT TOP(1) Id FROM dbo.DonViHanhChinh WHERE Regime='Cu' AND [Level]=3 ORDER BY Id FOR JSON PATH;")[0].Id;
    const shown = () => json(database, 'SELECT DangHienThi D FROM dbo.DiaDiemDuLich WHERE MaDiaDiem=' + place.PlaceId + ' FOR JSON PATH;')[0].D;
    const claimRow = id => json(database, 'SELECT EntryId E,HiddenAt H FROM dbo.NhanDiaDiemKhamPha WHERE BranchId=' + id + ' FOR JSON PATH;')[0];
    const submitAndApprove = async (name, lat, lng, claimed) => {
      const page = await provider.get('/nha-cung-cap/co-so/them' + (claimed ? '?tu=' + claimed : ''));
      const quick = await provider.req('/nha-cung-cap/co-so/luu', fd(val(page, '__RequestVerificationToken'), { Id: 0, Version: '0', Operation: 'Draft', Name: name, Address: 'Dia chi thu ' + stamp, DonViId: geoUnit, Type: 'LuuTru', ClaimedEntryId: claimed }));
      assert.equal(quick.status, 302, 'Luu nhap co so'); const id = Number(quick.location.split('/').pop());
      const next = await provider.get(quick.location);
      const sub = await provider.req('/nha-cung-cap/co-so/luu', fd(val(next, '__RequestVerificationToken'), { Id: id, Version: val(next, 'Version'), Operation: 'Submit', Name: name, Address: 'Dia chi thu ' + stamp, DonViId: geoUnit, Type: 'LuuTru', Summary: 'Co so thu nghiem de kiem tra viec nhan dia diem tu Kham pha va an dia diem goc sau khi duyet.', OpenDayList: ['1', '2', '3', '4', '5', '6', '7'], OpenTime: '07:00', CloseTime: '21:00', PriceFrom: 300000, PriceUnit: 'phòng/đêm', Phone: '0939000555', LocationSource: 'Provider', Latitude: lat, Longitude: lng, ConfirmLocation: 'true', OwnershipRole: 'Chủ cơ sở' }));
      assert.equal(sub.status, 302, 'Gui duyet co so');
      const revision = json(database, 'SELECT TOP(1) Id FROM BanCapNhatCoSo WHERE BranchId=' + id + ' ORDER BY Number DESC FOR JSON PATH;')[0].Id;
      const review = await admin.get('/quan-tri/co-so/' + revision);
      await admin.post('/quan-tri/co-so/duyet', review, { Id: revision, Version: val(review, 'Version'), Decision: 'Approved' });
      return id;
    };
    const statusOf = id => json(database, 'SELECT Status S FROM dbo.CoSoDichVu WHERE Id=' + id + ' FOR JSON PATH;')[0].S;
    // a) vi tri lech xa (khoang 11 km): duyet nhung khong an dia diem goc
    const far = await submitAndApprove('QA Nhan xa ' + stamp, 15.977, 108.335, place.Id);
    check(statusOf(far) === 'Approved' && claimRow(far) && claimRow(far).E === place.Id && !claimRow(far).H && shown() === true, 'Nhan dia diem nhung vi tri lech xa: co so duoc duyet, dia diem goc van hien');
    // b) vi tri khop: dia diem goc tu an
    const near = await submitAndApprove('QA Nhan gan ' + stamp, 15.8772, 108.3351, place.Id);
    check(statusOf(near) === 'Approved', 'Co so nhan dia diem duoc duyet');
    check(claimRow(near) && claimRow(near).H && shown() === false, 'Vi tri khop: dia diem goc tu an sau khi co so duoc duyet');
    check((await guest.get(url)).status === 404, 'Trang dia diem goc khong con cong khai');
    // c) co so tao binh thuong khong ghi nhan gi
    const plain = await submitAndApprove('QA Khong nhan ' + stamp, 15.8772, 108.3351, null);
    check(statusOf(plain) === 'Approved' && claimRow(plain) === undefined, 'Co so tao binh thuong khong co ban ghi nhan dia diem');
  } finally { restore(); }
  console.log(`PASS ${results.length} claim-place checks.`);
})().catch(e => { console.error(e); process.exit(1); });
