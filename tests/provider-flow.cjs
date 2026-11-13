// Kiem thu luong nha cung cap dang thong tin cho khach: trang chu "Co so cua toi", bieu mau co anh/gio/gia,
// kiem tra du lieu, quan tri duyet, trang cong khai, anh, dong bo vao tim kiem (the + bo loc dang mo cua).
// Chay tren DB co so thu: FULLTEXT_TEST_BASE=http://127.0.0.1:5225 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/provider-flow.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, decode, base } = require('./fulltext-smoke.cjs');
const { database, json } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
assert.equal(fixture.database, database);
const user = label => fixture.users.find(x => x.label === label);
const results = [];
function check(ok, name) { assert.ok(ok, name); results.push(name); }
const stamp = Date.now().toString();
const PNG = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==', 'base64');
const geoUnit = json(database, "SELECT TOP(1) Id FROM dbo.DonViHanhChinh WHERE Regime='Cu' AND [Level]=3 ORDER BY Id FOR JSON PATH;")[0].Id;
const row = name => json(database, `SELECT TOP(1) * FROM dbo.CoSoDichVu WHERE Name=N'${name}' ORDER BY Id DESC FOR JSON PATH;`)[0];

function form(token, values, photos = 0) {
  const f = new FormData();
  f.append('__RequestVerificationToken', token);
  for (const [k, v] of Object.entries(values)) {
    if (v == null) continue;
    for (const item of Array.isArray(v) ? v : [v]) f.append(k, String(item));
  }
  for (let n = 0; n < photos; n++) f.append('Photos', new Blob([PNG], { type: 'image/png' }), `anh-${n}.png`);
  return f;
}
async function status(promise, expected, name) {
  const r = await promise;
  check(r.status === expected, `${name} [${r.status}]` + (r.status !== expected ? ' ' + r.text.replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').slice(0, 400) : ''));
  return r;
}

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { /* cho may chu */ } if (n > 45) throw Error('Server not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const provider = new Client(), admin = new Client(), guest = new Client();
  const login = await provider.login(user('provider').email, 'BranchTest@2026');
  check(login.location === '/bang-tin', 'Dang nhap nha cung cap vao thang Bang tin (trang chu): ' + login.location);
  await admin.login(user('admin').email, 'BranchTest@2026');

  // 1. Trang chu nha cung cap va bieu mau
  const home = await status(provider.get('/nha-cung-cap/co-so'), 200, 'Trang chu nha cung cap');
  check(home.text.includes('Cơ sở của tôi') && home.html.includes('pv-steps') && home.text.includes('Thêm cơ sở đầu tiên') || home.html.includes('pv-cards'), 'Trang chu co cac buoc bat dau va the co so');
  check(home.html.includes('ĐĂNG THÔNG TIN CHO KHÁCH') && /class="workspace-sidebar"[\s\S]*?Cơ sở của tôi/.test(home.html), 'Thanh ben co muc "Co so cua toi" va trang noi ro "Dang thong tin cho khach"');
  const page = await status(provider.get('/nha-cung-cap/co-so/them'), 200, 'Mo bieu mau them co so');
  for (const marker of ['enctype="multipart/form-data"', 'data-pv-photos', 'name="OpenDayList"', 'name="PriceFrom"', 'name="OwnershipRole"', 'data-pv-score'])
    check(page.html.includes(marker), 'Bieu mau co ' + marker);
  const token = val(page, '__RequestVerificationToken');

  // 2. Kiem tra du lieu
  const name = 'QA Hồ sơ đầy đủ ' + stamp;
  const good = {
    Id: 0, Version: '0', Operation: 'Submit', Name: name, Address: 'Địa chỉ thử ' + stamp, DonViId: geoUnit, Type: 'AnUong',
    Summary: 'Quán thử nghiệm cho khách du lịch, có món địa phương và chỗ ngồi thoáng, phục vụ cả ngày.',
    OpenDayList: ['1', '2', '3', '4', '5', '6', '7'], OpenTime: '07:00', CloseTime: '21:30', Hours: 'Nghỉ lễ theo thông báo',
    PriceFrom: 30000, PriceTo: 90000, PriceUnit: 'người', Phone: '0939000222', LocationSource: 'Provider', Latitude: 16.06, Longitude: 108.22,
    ConfirmLocation: 'true', OwnershipRole: 'Chủ cơ sở', Parking: 'true'
  };
  await status(provider.req('/nha-cung-cap/co-so/luu', form(token, { ...good, Name: 'QA giá sai ' + stamp, PriceFrom: 90000, PriceTo: 30000 })), 422, 'Giá đến nhỏ hơn giá từ bị từ chối');
  await status(provider.req('/nha-cung-cap/co-so/luu', form(token, { ...good, Name: 'QA thiếu đơn vị ' + stamp, PriceUnit: '' })), 422, 'Có giá nhưng thiếu đơn vị bị từ chối');
  await status(provider.req('/nha-cung-cap/co-so/luu', form(token, { ...good, Name: 'QA giờ thiếu ' + stamp, CloseTime: '' })), 422, 'Giờ mở cửa thiếu giờ đóng bị từ chối');
  await status(provider.req('/nha-cung-cap/co-so/luu', form(token, { ...good, Name: 'QA nhiều ảnh ' + stamp }, 6)), 400, 'Hơn 5 ảnh bị từ chối');
  await status(provider.req('/nha-cung-cap/co-so/luu', form(token, { ...good, Name: 'QA thiếu vai trò ' + stamp, OwnershipRole: '' })), 422, 'Gửi duyệt thiếu vai trò bị từ chối');

  // 3. Gui ho so day du kem anh
  await status(provider.req('/nha-cung-cap/co-so/luu', form(token, good, 2)), 302, 'Gửi hồ sơ đầy đủ kèm 2 ảnh');
  const saved = row(name);
  check(saved && saved.PriceFrom === 30000 && saved.PriceTo === 90000 && saved.PriceUnit === 'người', 'Lưu đúng giá tham khảo');
  check(saved.OpenDays === '1234567' && saved.OpenTime.startsWith('07:00') && saved.CloseTime.startsWith('21:30'), 'Lưu đúng giờ mở cửa có cấu trúc');
  check(JSON.parse(saved.PhotosJson).length === 2, 'Lưu 2 ảnh');
  check(saved.Status === 'Pending', 'Cơ sở chờ duyệt');
  const revision = json(database, `SELECT TOP(1) Id FROM BanCapNhatCoSo WHERE BranchId=${saved.Id} ORDER BY Number DESC FOR JSON PATH;`)[0].Id;
  const revRow = json(database, `SELECT PhotosJson,OwnershipEvidence FROM BanCapNhatCoSo WHERE Id=${revision} FOR JSON PATH;`)[0];
  check(JSON.parse(revRow.PhotosJson).length === 2 && revRow.OwnershipEvidence.startsWith('Chủ cơ sở'), 'Bản gửi duyệt giữ ảnh và vai trò');

  // 4. Cua so anh: chua duyet thi khach khong xem duoc, chu co so/quan tri xem duoc
  check((await guest.get(`/co-so/${saved.Id}/anh/0`)).status !== 200, 'Ảnh của cơ sở chưa duyệt không công khai');
  const ownerPhoto = await provider.req(`/co-so/${saved.Id}/anh/0?revision=${revision}`);
  check(ownerPhoto.status === 200 && ownerPhoto.headers.get('content-type') === 'image/png', 'Chủ cơ sở xem được ảnh bản chờ duyệt');
  const review = await status(admin.get('/quan-tri/co-so/' + revision), 200, 'Quản trị mở trang duyệt');
  check(review.html.includes(`/co-so/${saved.Id}/anh/0?revision=${revision}`) && review.text.includes('Mỗi ngày · 07:00–21:30') && review.text.includes('30.000'), 'Trang duyệt hiện ảnh, giờ và giá');
  check((await admin.req(`/co-so/${saved.Id}/anh/1?revision=${revision}`)).status === 200, 'Quản trị xem được ảnh bản chờ duyệt');
  await status(admin.post('/quan-tri/co-so/duyet', review, { Id: revision, Version: val(review, 'Version'), Decision: 'Approved' }), 302, 'Quản trị duyệt cơ sở');

  // 5. Trang cong khai
  const pub = await status(guest.get('/co-so/' + saved.Id), 200, 'Trang công khai của cơ sở');
  check(pub.html.includes(`/co-so/${saved.Id}/anh/0`) && pub.text.includes('Mỗi ngày · 07:00–21:30') && pub.text.includes('30.000') && pub.text.includes('90.000'), 'Trang công khai hiện ảnh, giờ và khoảng giá');
  const publicPhoto = await guest.req(`/co-so/${saved.Id}/anh/0`);
  check(publicPhoto.status === 200 && publicPhoto.headers.get('content-type') === 'image/png', 'Ảnh đã duyệt công khai');
  check((await guest.get(`/co-so/${saved.Id}/anh/9`)).status === 404, 'Ảnh ngoài phạm vi trả 404');

  // 6. Dong bo vao tim kiem (chi muc cap nhat moi phut)
  let card = '';
  for (let n = 0; n < 40; n++) {
    const found = await guest.get('/Discovery?Q=' + encodeURIComponent(name));
    card = found.html;
    if (card.includes(`/co-so/${saved.Id}/anh/0`)) break;
    await new Promise(r => setTimeout(r, 3000));
  }
  check(card.includes(`/co-so/${saved.Id}/anh/0`), 'Thẻ tìm kiếm dùng ảnh thực tế của cơ sở');
  check(decode(card).includes('30.000'), 'Thẻ tìm kiếm hiện giá tham khảo');
  const today = new Date(Date.now() + 7 * 3600000).toISOString().slice(0, 10);
  const open = await guest.get(`/Discovery?Q=${encodeURIComponent(name)}&Date=${today}&At=12:00`);
  const closed = await guest.get(`/Discovery?Q=${encodeURIComponent(name)}&Date=${today}&At=23:45`);
  check(open.html.includes(`/co-so/${saved.Id}`), 'Bộ lọc "đang mở cửa" 12:00 giữ cơ sở');
  check(!closed.html.includes(`/co-so/${saved.Id}`), 'Bộ lọc "đang mở cửa" 23:45 loại cơ sở (đã đóng)');

  // 7. Do day du tren trang chu nha cung cap
  const after = await provider.get('/nha-cung-cap/co-so');
  const m = decode(after.html).match(new RegExp(`${name}[\\s\\S]*?(\\d+)%`));
  check(m && Number(m[1]) >= 90, 'Hồ sơ đầy đủ đạt từ 90%: ' + (m && m[1]) + '%');
  console.log(`PASS ${results.length} provider-flow checks.`);
})().catch(e => { console.error(e); process.exit(1); });
