// Kiem thu goi 1: dia gioi hanh chinh (API, bo loc theo khu vuc, trang diem den). Chi doc, khong ghi du lieu.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5224 node tests/geography-features.cjs   (server phai dung DB da nang cap 008/008c)
const assert = require('node:assert/strict');
const base = process.env.FULLTEXT_TEST_BASE || 'http://127.0.0.1:5224';
const results = [];
function check(ok, name) { assert.ok(ok, name); results.push(name); }
async function get(path) {
  const r = await fetch(base + path, { redirect: 'manual' });
  const text = await r.text();
  let json = null; try { json = JSON.parse(text); } catch { /* html */ }
  return { status: r.status, text, json };
}
const decode = t => t.replace(/&#x([0-9a-f]+);/gi, (_, h) => String.fromCodePoint(parseInt(h, 16))).replace(/&#(\d+);/g, (_, d) => String.fromCodePoint(+d));
const cards = html => (html.match(/class="ds-card"/g) || []).length;

(async () => {
  const provinces = await get('/api/dia-gioi/tinh');
  check(provinces.status === 200 && provinces.json.items.length === 63 && provinces.json.regime === 'Cu', 'API tinh tra 63 tinh/thanh cua he cu');
  const danang = provinces.json.items.find(x => x.name === 'Đà Nẵng');
  check(danang && danang.level === 1 && danang.hasChildren, 'Co Da Nang o cap tinh');

  const districts = await get(`/api/dia-gioi/${danang.id}/con`);
  check(districts.status === 200 && districts.json.items.length === 8, 'Da Nang cu co 8 quan/huyen');
  const haiChau = districts.json.items.find(x => x.fullName === 'Quận Hải Châu');
  check(haiChau && haiChau.level === 2, 'Co Quan Hai Chau');
  const wards = await get(`/api/dia-gioi/${haiChau.id}/con`);
  check(wards.json.items.length > 5 && wards.json.items.every(x => x.level === 3), 'Quan Hai Chau co cac phuong cap 3');

  const path = await get(`/api/dia-gioi/${wards.json.items[0].id}/duong-dan`);
  check(path.json.items.length === 3 && path.json.text.endsWith('Thành phố Đà Nẵng'), 'Duong dan tu phuong len tinh');
  check((await get('/api/dia-gioi/999999999/duong-dan')).status === 404, 'Don vi khong ton tai tra 404');

  const noDiacritics = await get('/api/dia-gioi/tra-cuu?q=hai%20chau');
  check(noDiacritics.json.items.some(x => x.fullName === 'Quận Hải Châu'), 'Tim khong dau ra Quan Hai Chau');
  check((await get('/api/dia-gioi/tra-cuu?q=h')).json.items.length === 0, 'Tu khoa qua ngan khong tra ket qua');

  // bo loc theo khu vuc
  const all = await get('/Discovery?Sort=relevance'); // trang ket qua (trang chu khong con danh sach)
  const inDaNang = await get(`/Discovery?Area=${danang.id}`);
  const inHaiChau = await get(`/Discovery?Area=${haiChau.id}`); // trang chi hien toi da 20 the, nen so sanh voi khu vuc nho hon
  check(inDaNang.status === 200 && cards(inDaNang.text) >= 1 && cards(inHaiChau.text) >= 1 && cards(inHaiChau.text) < cards(all.text), 'Loc theo khu vuc thu hep ket qua');
  check(inDaNang.text.includes('Bỏ khu vực') && inDaNang.text.includes(`/diem-den/${danang.id}`), 'Chip khu vuc va lien ket trang diem den');
  check((await get('/Discovery?Area=999999999')).status === 400, 'Khu vuc khong ton tai bi tu choi (400)');
  const districtOnly = await get(`/Discovery?Area=${haiChau.id}`);
  check(cards(districtOnly.text) <= cards(inDaNang.text), 'Loc theo quan khong nhieu hon loc theo tinh');
  check((await get(`/kham-pha?Area=${danang.id}`)).status === 200, 'Tim co so co tham so khu vuc');

  // trang diem den
  const index = await get('/diem-den');
  check(index.status === 200 && index.text.includes(`/diem-den/${danang.id}`), 'Trang danh sach diem den');
  const page = await get(`/diem-den/${danang.id}`);
  check(page.status === 200 && page.text.includes('Thay đổi hành chính'), 'Trang Da Nang co ghi chu thay doi hanh chinh');
  check(['tham-quan', 'an-uong', 'luu-tru', 'di-chuyen'].every(k => page.text.includes(`id="${k}"`)), 'Tong quan co du 4 nhom nhu cau');
  check(page.text.includes('Chưa có thông tin được công bố ở mục này') || cards(page.text) > 0, 'Muc trong hien thong bao thay vi bo trong');
  const sub = await get(`/diem-den/${haiChau.id}`);
  check(sub.status === 200 && decode(sub.text).includes('Quận Hải Châu'), 'Trang quan co duong dan phan cap');
  check((await get('/diem-den/999999999')).status === 404, 'Diem den khong ton tai tra 404');

  // luong tim kiem: cua vao, dong nghia, xep hang, goi y khi khong co ket qua
  const home = await get('/');
  check(home.status === 302 && home.text === '' || home.status === 302, 'Trang chu mo thang trang tra cuu');
  const homeRes = await fetch(base + '/', { redirect: 'manual' });
  check((homeRes.headers.get('location') || '').endsWith('/Discovery'), 'Trang chu chuyen toi /Discovery');
  check((await get('/Discovery')).text.includes('href="/diem-den'), 'Trang chu dan toi Diem den');
  const eat = await get('/Discovery?Q=' + encodeURIComponent('quán ăn Đà Nẵng'));
  check(cards(eat.text) >= 1, 'Quan an Da Nang khong con bo trong (khop huong dan am thuc)');
  const stay = await get('/Discovery?Q=' + encodeURIComponent('homestay Đà Nẵng'));
  check(cards(stay.text) >= 1, 'Homestay duoc hieu la luu tru');
  const duration = await get('/Discovery?Q=' + encodeURIComponent('đi Đà Nẵng 3 ngày'));
  check(cards(duration.text) >= 5, 'So ngay trong cau hoi khong lam mat ket qua');
  const exact = await get('/Discovery?Q=' + encodeURIComponent('bà nà'));
  const firstTitle = decode((exact.text.match(/class="ds-card"[\s\S]*?<h3><a [^>]*>([^<]*)/) || [])[1] || '');
  check(firstTitle === 'Bà Nà Hills', 'Ten khop chinh xac xep truoc: ' + firstTitle);
  const none = await get('/Discovery?Q=' + encodeURIComponent('cà phê view đẹp Đà Lạt'));
  check(none.text.includes('dest-hint') && /\/diem-den\/\d+/.test(none.text), 'Khong co ket qua van goi y trang diem den cua noi nhan ra');
  const unknown = await get('/Discovery?Q=xyzabc');
  check(unknown.status === 200 && !unknown.text.includes('dest-hint'), 'Tu khoa vo nghia khong goi y sai');
  const sug = await get('/api/discovery/suggest?q=' + encodeURIComponent('da n'));
  check(sug.json.length > 1 && sug.json[0].url.startsWith('/diem-den/'), 'Goi y khi go bat dau bang diem den');
  check((await get('/api/discovery/suggest?q=' + encodeURIComponent('mi qu'))).json.length >= 1, 'Goi y tim ca trong noi dung (mi quang)');

  // trang chu theo nhu cau
  const homePage = await get('/Discovery');
  check(homePage.status === 200 && !homePage.text.includes('data-dg'), 'Trang chu: o nhu cau, khong co 3 o chon hanh chinh');
  const tileLinks = ['an-uong', 'luu-tru', 'tham-quan', 'di-chuyen', 'ban-do'].every(v => homePage.text.includes(`/diem-den?viec=${v}`));
  check(tileLinks, 'Trang chu co du cac o nhu cau An gi / O dau / Choi o dau / Di lai / Ban do');
  check(homePage.text.includes(`/diem-den/${danang.id}`), 'Trang chu co the diem den Da Nang');
  const picker = await get('/diem-den?viec=an-uong');
  check(picker.status === 200 && picker.text.includes(`/diem-den/${danang.id}?tab=an-uong`), 'Chon viec "An gi" dan toi tab An uong cua diem den');

  // trung tam diem den: tab, danh sach + ban do, chip
  const tabs = { 'tham-quan': 'Tham quan', 'an-uong': 'Ăn uống', 'luu-tru': 'Lưu trú', 'di-chuyen': 'Di chuyển', 'ban-do': 'Bản đồ' };
  const hub = await get(`/diem-den/${danang.id}`);
  check(Object.keys(tabs).every(t => hub.text.includes(`href="/diem-den/${danang.id}?tab=${t}"`)), 'Thanh tab theo nhu cau co du 5 tab + Tong quan');
  for (const t of Object.keys(tabs)) check((await get(`/diem-den/${danang.id}?tab=${t}`)).status === 200, 'Tab ' + t + ' mo duoc');
  check((await get(`/diem-den/${danang.id}?tab=khong-co`)).status === 200, 'Tab khong hop le quay ve Tong quan');
  const eatTab = await get(`/diem-den/${danang.id}?tab=an-uong`);
  check(cards(eatTab.text) >= 1 && eatTab.text.includes('dest-split') && eatTab.text.includes('Lọc nhanh'), 'Tab An uong co danh sach, bo cuc danh sach + ban do va chip loc nhanh');
  const eatCafe = await get(`/diem-den/${danang.id}?tab=an-uong&q=` + encodeURIComponent('Cà phê'));
  check(cards(eatCafe.text) >= 1 && cards(eatCafe.text) < cards(eatTab.text), 'Chip Ca phe loc danh sach');
  const eatArea = await get(`/diem-den/${danang.id}?tab=an-uong&area=${haiChau.id}`);
  check(eatArea.status === 200 && cards(eatArea.text) <= cards(eatTab.text), 'Chip khu vuc loc danh sach theo quan');
  const mapTab = await get(`/diem-den/${danang.id}?tab=tham-quan`);
  const points = JSON.parse((mapTab.text.match(/<script type="application\/json" id="dest-points">([\s\S]*?)<\/script>/) || [, '[]'])[1]);
  check(points.length >= 1 && points.every(p => Number.isFinite(p.lat) && Number.isFinite(p.lng) && p.u.startsWith('/')), 'Tab tham quan co ghim ban do voi toa do va lien ket');
  check(mapTab.text.includes('data-dest-map') && mapTab.text.includes('/js/destination.js'), 'Ban do cua trung tam diem den duoc nap');
  const bigMap = await get(`/diem-den/${danang.id}?tab=ban-do`);
  check(bigMap.text.includes('dest-legend') && bigMap.text.includes('data-dest-map'), 'Tab Ban do co chu giai va ban do lon');
  check(!eatTab.text.includes('/images/dalat.jpg'), 'The khong dung anh cua noi khac khi chua co anh that');

  // bo chon xuat hien tren cac bieu mau
  check(all.text.includes('data-dg') && all.text.includes('name="Area"'), 'Trang tim kiem co bo chon dia gioi');
  console.log(`PASS ${results.length} geography checks.`);
})().catch(e => { console.error(e); process.exit(1); });
