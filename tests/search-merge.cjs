// Kiem thu chi muc tra cuu chung: co so da cong bo xuat hien cung dia diem/dich vu trong mot danh sach, loc theo khu vuc/loai,
// goi y gom co so, trang diem den, lien ket qua lai giua tra cuu va ban do. Chi doc.
// Can DB co it nhat mot co so da duyet (vd HoTroDuLich_Branches_20261108_Final).
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5225 node tests/search-merge.cjs
const assert = require('node:assert/strict');
const base = process.env.FULLTEXT_TEST_BASE || 'http://127.0.0.1:5225';
const results = [];
function check(ok, name) { assert.ok(ok, name); results.push(name); }
async function get(path) {
  const r = await fetch(base + path, { redirect: 'manual' });
  const text = await r.text();
  let json = null; try { json = JSON.parse(text); } catch { /* html */ }
  return { status: r.status, text, json, location: r.headers.get('location') };
}
const decode = t => t.replace(/&#x([0-9a-f]+);/gi, (_, h) => String.fromCodePoint(parseInt(h, 16)));
const branchCards = html => [...html.matchAll(/<h3><a href="(\/co-so\/\d+)">([^<]*)<\/a><\/h3>/g)].map(m => ({ href: m[1], title: decode(m[2]) }));

(async () => {
  const provinces = await get('/api/dia-gioi/tinh');
  let province = null, shown = [], absentType = null;
  for (const p of provinces.json.items) { // tinh dau tien co co so da duyet trong chi muc
    const page = await get(`/Discovery?Area=${p.id}`);
    shown = branchCards(page.text);
    if (page.status === 200 && shown.length) {
      // chon tinh co quan an va it nhat mot loai khac khong co co so (de thu "khac loai"); neu khong co thi dung tinh co quan an dau tien
      const cnt = async t => branchCards((await get(`/Discovery?Area=${p.id}&Type=${t}`)).text).length;
      const eat = await cnt('AnUong');
      if (eat >= 1) {
        const absent = (await cnt('LuuTru')) === 0 ? 'LuuTru' : (await cnt('PhuongTien')) === 0 ? 'PhuongTien' : null;
        if (!province || (absent && !absentType)) { province = p; absentType = absent; }
        if (absentType) break;
      }
    }
  }
  check(!!province, 'Co it nhat mot co so da duyet nam trong danh sach tra cuu chung: ' + (province && province.fullName));
  check(shown.every(c => /^\/co-so\/\d+$/.test(c.href)), 'The co so dan toi trang co so');

  const typed = await get(`/Discovery?Area=${province.id}&Type=AnUong`);
  check(branchCards(typed.text).length >= 1, 'Loc Quan an giu lai co so quan an');
  const mapLink = decode((typed.text.match(/class="ds-map-link"><a href="([^"]*)"/) || [])[1] || '');
  check(mapLink.startsWith('/kham-pha?') && mapLink.includes(`Area=${province.id}`) && mapLink.includes('Category=AnUong'), 'Co lien ket sang ban do kem khu vuc va loai');
  if (absentType) check(branchCards((await get(`/Discovery?Area=${province.id}&Type=${absentType}`)).text).length === 0, 'Khac loai thi khong hien co so');
  else results.push('(bo qua: moi loai deu co co so trong tinh duoc chon)');
  const hanoi = provinces.json.items.find(p => p.id !== province.id && p.name === 'Hà Nội');
  check(branchCards((await get('/Discovery?Area=' + hanoi.id)).text).length === 0, 'Khu vuc khac khong co co so thi khong hien');

  const byName = await get('/Discovery?Q=' + encodeURIComponent('QA Quán A1'));
  const named = branchCards(byName.text);
  check(named.length >= 1 && named.every(c => c.title.includes('QA Quán A1')), 'Tim theo ten co so');

  const sug = await get('/api/discovery/suggest?q=' + encodeURIComponent('QA Qu'));
  check(sug.json.some(x => x.url.startsWith('/co-so/')), 'Goi y khi go co ca co so');

  const unit = await get(`/diem-den/${province.id}?tab=an-uong`); // tab danh sach day du (Tong quan chi hien 3 muc dau moi nhom)
  check(unit.status === 200 && /\/co-so\/\d+/.test(unit.text), 'Tab An uong cua trang diem den co chua co so');

  const back = await get(`/kham-pha?Area=${province.id}`);
  check(back.status === 200 && back.text.includes('/Discovery?Q=') && back.text.includes(`Area=${province.id}`), '/kham-pha co lien ket quay lai tra cuu');
  console.log(`PASS ${results.length} unified-index checks.`);
})().catch(e => { console.error(e); process.exit(1); });
