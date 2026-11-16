// Nha cung cap nhan mot dia diem co san trong Kham pha: trang dia diem co loi moi, bieu mau Them co so duoc dien san.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/claim-place.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, decode } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
const user = label => fixture.users.find(x => x.label === label);
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const attr = (html, name) => { const m = new RegExp('<(?:input|textarea)[^>]*name="' + name + '"[^>]*>').exec(html); if (!m) return null; const v = /value="([^"]*)"/.exec(m[0]); return v ? v[1] : ''; };

(async () => {
  const guest = new Client(), provider = new Client(), traveler = new Client();
  await provider.login(user('provider').email, 'BranchTest@2026'); await traveler.login(user('traveler').email, 'BranchTest@2026');
  const place = json(database, `SELECT TOP 1 Id,Title,Address,Latitude La,Longitude Lo,Region,Tags FROM dbo.DiscoveryEntry WHERE Kind=N'Place' AND Status='Published' AND ServiceId IS NULL AND BranchId IS NULL AND PlaceId IS NOT NULL ORDER BY Id FOR JSON PATH;`)[0];
  check(place && place.Id > 0, 'Co dia diem da cong bo de thu');
  const had = { A: place.Address, La: place.La, Lo: place.Lo, T: place.Tags };
  sql(database, `UPDATE dbo.DiscoveryEntry SET Address=N'12 Duong Thu, Hoi An',Latitude=15.877000,Longitude=108.335000,Tags=N'Tham quan lưu trú khách sạn' WHERE Id=${place.Id};`);
  const service = json(database, `SELECT TOP 1 Id FROM dbo.DiscoveryEntry WHERE Kind=N'Service' AND Status='Published' ORDER BY Id FOR JSON PATH;`)[0];

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
  check(/<option[^>]*value="LuuTru"[^>]*selected/.test(fh) || /value="LuuTru"[^>]*checked/.test(fh) || fh.includes('Nhận từ địa điểm đã có trong Khám phá'), 'Loai co so va can cu duoc goi y');
  check(fh.includes('Nhận từ địa điểm đã có trong Khám phá (mục #' + place.Id), 'Can cu ghi ro nguon tu Kham pha de quan tri vien doi chieu');
  const blank = decode((await provider.get('/nha-cung-cap/co-so/them')).html);
  check(!blank.includes('data-claimed') && (attr(blank, 'Name') || '') === '', 'Khong co tham so thi bieu mau de trong');
  const wrong = decode((await provider.get('/nha-cung-cap/co-so/them?tu=999999999')).html);
  check(!wrong.includes('data-claimed'), 'Ma dia diem la thi bieu mau de trong');
  if (service) { const sf = decode((await provider.get('/nha-cung-cap/co-so/them?tu=' + service.Id)).html); check(!sf.includes('data-claimed'), 'Khong nhan duoc muc dich vu cua nguoi khac'); }
  check((await traveler.get('/nha-cung-cap/co-so/them?tu=' + place.Id)).status >= 300, 'Khach du lich khong mo duoc bieu mau nhan dia diem');
  check((await guest.get('/nha-cung-cap/co-so/them?tu=' + place.Id)).status >= 300, 'Khach chua dang nhap bi chuyen sang dang nhap');
  check(json(database, `SELECT COUNT(*) N FROM dbo.DiscoveryEntry WHERE Id=${place.Id} AND Status='Published' FOR JSON PATH;`)[0].N === 1, 'Mo bieu mau khong lam thay doi dia diem goc');

  sql(database, `UPDATE dbo.DiscoveryEntry SET Address=${had.A == null ? 'NULL' : "N'" + String(had.A).replace(/'/g, "''") + "'"},Latitude=${had.La == null ? 'NULL' : had.La},Longitude=${had.Lo == null ? 'NULL' : had.Lo},Tags=${had.T == null ? "N''" : "N'" + String(had.T).replace(/'/g, "''") + "'"} WHERE Id=${place.Id};`);
  console.log(`PASS ${results.length} claim-place checks.`);
})().catch(e => { console.error(e); process.exit(1); });
