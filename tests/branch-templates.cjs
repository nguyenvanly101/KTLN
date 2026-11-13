// Thong tin rieng theo loai co so: kiem tra du lieu, luu JSON, duyet, hien thi cho khach, tu khoa tim kiem, dien lai khi sua.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5225 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/branch-templates.cjs
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

function form(token, values) {
  const f = new FormData(); f.append('__RequestVerificationToken', token);
  for (const [k, v] of Object.entries(values)) { if (v == null) continue; for (const item of Array.isArray(v) ? v : [v]) f.append(k, String(item)); }
  return f;
}
const row = name => json(database, `SELECT TOP(1) * FROM dbo.CoSoDichVu WHERE Name=N'${name}' ORDER BY Id DESC FOR JSON PATH;`)[0];

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { /* cho */ } if (n > 45) throw Error('Server not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const provider = new Client(), admin = new Client(), guest = new Client();
  await provider.login(user('provider').email, 'BranchTest@2026'); await admin.login(user('admin').email, 'BranchTest@2026');
  const page = await provider.get('/nha-cung-cap/co-so/them');
  check(['AnUong', 'LuuTru', 'HoatDong', 'PhuongTien', 'Khac'].every(t => page.html.includes(`data-type-fields="${t}"`)) && page.html.includes('name="attr.AnUong.kieu"') && page.html.includes('name="attr.LuuTru.phong"'), 'Bieu mau co nhom truong rieng cho tung loai');
  const token = val(page, '__RequestVerificationToken');
  const base0 = name => ({ Id: 0, Version: '0', Operation: 'Submit', Name: name, Address: 'Địa chỉ thử ' + stamp, DonViId: geoUnit, Type: 'AnUong', Summary: 'Quán thử nghiệm có thông tin riêng theo loại cơ sở để khách xem trước khi đến.', OpenDayList: ['1', '2', '3', '4', '5', '6', '7'], OpenTime: '07:00', CloseTime: '21:00', PriceFrom: 30000, PriceUnit: 'người', Phone: '0939000333', LocationSource: 'Provider', Latitude: 16.07, Longitude: 108.21, ConfirmLocation: 'true', OwnershipRole: 'Chủ cơ sở' });
  const post = (extra, name = 'QA Mẫu ' + stamp) => provider.req('/nha-cung-cap/co-so/luu', form(token, { ...base0(name), ...extra }));

  // 1. Kiem tra du lieu
  check((await post({ 'attr.AnUong.kieu': 'Quán không có' }, 'QA bad1 ' + stamp)).status === 422, 'Gia tri ngoai danh sach bi tu choi');
  check((await post({ 'attr.AnUong.chongoi': ['Ngoài trời', 'Trên mây'] }, 'QA bad2 ' + stamp)).status === 422, 'Lua chon da chon sai bi tu choi');
  check((await post({ 'attr.AnUong.succhua': '99999' }, 'QA bad3 ' + stamp)).status === 422 && (await post({ 'attr.AnUong.succhua': 'abc' }, 'QA bad3b ' + stamp)).status === 422, 'Suc chua ngoai khoang hoac khong phai so bi tu choi');
  check((await post({ Type: 'LuuTru', 'attr.LuuTru.nhanphong': '25:99' }, 'QA bad4 ' + stamp)).status === 422, 'Gio nhan phong sai dinh dang bi tu choi');
  check((await post({ 'attr.AnUong.thucdon': Array.from({ length: 16 }, (_, n) => `Món ${n} | 10000`).join('\n') }, 'QA bad5 ' + stamp)).status === 422, 'Qua 15 dong thuc don bi tu choi');
  check((await post({ 'attr.AnUong.thucdon': 'Phở bò | gia cao' }, 'QA bad6 ' + stamp)).status === 422, 'Gia mon khong hop le bi tu choi');
  check((await post({ 'attr.AnUong.dacsac': 'x'.repeat(301) }, 'QA bad7 ' + stamp)).status === 422, 'Mo ta qua dai bi tu choi');

  // 2. Luu hop le (an uong) + khoa la va nhom loai khac bi bo qua
  const name = 'QA Mẫu ' + stamp;
  const ok = await post({ 'attr.AnUong.kieu': 'Quán cà phê', 'attr.AnUong.chongoi': ['Ngoài trời', 'Sân vườn'], 'attr.AnUong.chedoan': 'Có món chay', 'attr.AnUong.succhua': '40', 'attr.AnUong.thucdon': 'Cà phê muối | 35.000\nBạc xỉu | 30000\nBánh flan', 'attr.AnUong.hack': 'x', 'attr.LuuTru.hang': 'Resort' }, name);
  check(ok.status === 302, 'Luu co so an uong kem thong tin rieng');
  const saved = row(name); const attrs = JSON.parse(saved.AttributesJson);
  check(attrs.kieu === 'Quán cà phê' && attrs.chongoi.length === 2 && attrs.succhua === 40 && attrs.thucdon.length === 3 && attrs.thucdon[0].p === 35000 && attrs.thucdon[2].p === null, 'JSON luu dung kieu, nhieu lua chon, so, danh sach co gia');
  check(!('hack' in attrs) && !('hang' in attrs), 'Khoa la va truong cua loai khac khong duoc luu');
  const revision = json(database, `SELECT TOP(1) Id FROM BanCapNhatCoSo WHERE BranchId=${saved.Id} ORDER BY Number DESC FOR JSON PATH;`)[0].Id;
  check(JSON.parse(json(database, `SELECT AttributesJson A FROM BanCapNhatCoSo WHERE Id=${revision} FOR JSON PATH;`)[0].A).kieu === 'Quán cà phê', 'Ban cho duyet giu thong tin rieng');
  const review = await admin.get('/quan-tri/co-so/' + revision);
  check(review.status === 200 && review.text.includes('Kiểu quán') && review.text.includes('Quán cà phê'), 'Trang duyet cua quan tri hien thong tin rieng');
  await admin.post('/quan-tri/co-so/duyet', review, { Id: revision, Version: val(review, 'Version'), Decision: 'Approved' });

  // 3. Khach thay thong tin
  const pub = await guest.get('/co-so/' + saved.Id);
  check(pub.status === 200 && pub.text.includes('Kiểu quán') && pub.text.includes('Quán cà phê') && pub.text.includes('Sân vườn') && pub.text.includes('Cà phê muối — 35.000 ₫') && pub.text.includes('40 người'), 'Trang cong khai hien: kieu quan, cho ngoi, thuc don co gia, suc chua');
  check(!pub.text.includes('Resort') && !pub.text.includes('Hạng cơ sở'), 'Khong hien truong cua loai khac');

  // 4. Tu khoa tim kiem tu thuoc tinh
  let tags = '';
  for (let n = 0; n < 45 && !tags.includes('Sân vườn'); n++) { tags = (json(database, `SELECT TOP(1) Tags T FROM dbo.DiscoveryEntry WHERE BranchId=${saved.Id} FOR JSON PATH;`)[0] || { T: '' }).T; if (!tags.includes('Sân vườn')) await new Promise(r => setTimeout(r, 3000)); }
  check(tags.includes('Sân vườn') && tags.includes('Quán cà phê') && tags.includes('Có món chay'), 'Chi muc tim kiem nhan cac lua chon lam tu khoa: ' + tags);

  // 5. Sua: dien lai gia tri da luu
  const edit = await provider.get('/nha-cung-cap/co-so/' + saved.Id);
  const ed = decode(edit.html);
  check(edit.status === 200 && /<option value="Quán cà phê"[^>]*selected/.test(ed) && /value="Sân vườn"[^>]*checked/.test(ed) && ed.includes('Cà phê muối | 35000'), 'Bieu mau sua dien lai gia tri da luu (chon, tick, danh sach)');

  // 6. Luu tru: chi luu truong cua loai da chon
  const hotel = 'QA Khách sạn ' + stamp;
  const h = await post({ Type: 'LuuTru', 'attr.LuuTru.hang': 'Homestay', 'attr.LuuTru.sao': '3 sao', 'attr.LuuTru.nhanphong': '14:00', 'attr.LuuTru.tiennghi': ['Wi-Fi', 'Bữa sáng'], 'attr.LuuTru.phong': 'Phòng đôi (2 khách) | 450000\nPhòng gia đình | 800.000', 'attr.AnUong.kieu': 'Quán ăn' }, hotel);
  const hAttrs = JSON.parse(row(hotel).AttributesJson);
  check(h.status === 302 && hAttrs.hang === 'Homestay' && hAttrs.nhanphong === '14:00' && hAttrs.phong[1].p === 800000 && !('kieu' in hAttrs), 'Luu tru: luu hang, gio nhan phong, loai phong co gia; khong luu truong an uong');
  // 7. Khong thong tin rieng van luu duoc
  const bare = 'QA Trống ' + stamp;
  check((await post({}, bare)).status === 302 && row(bare).AttributesJson == null, 'De trong het thi AttributesJson la NULL');

  console.log(`PASS ${results.length} branch-templates checks.`);
})().catch(e => { console.error(e); process.exit(1); });
