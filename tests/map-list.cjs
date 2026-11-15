// Ban do Kham pha: hien moi vi tri phu hop, danh sach quanh day canh ban do, bam ghim dua len dau danh sach.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/map-list.cjs
const assert = require('node:assert/strict');
const { base, decode } = require('./fulltext-smoke.cjs');
const { database, json } = require('./branches-sql.cjs');
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const get = async p => { const r = await fetch(base + p, { redirect: 'manual' }); const t = await r.text(); let j = null; try { j = JSON.parse(t); } catch { } return { status: r.status, text: t, body: j }; };

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { } if (n > 45) throw Error('not ready'); await new Promise(r => setTimeout(r, 1000)); }
  // 1. API ban do tra moi vi tri (khong con gioi han 300)
  const all = await get('/api/discovery/map?Q=');
  check(all.status === 200 && all.body.limit === 1500 && Array.isArray(all.body.items), 'API ban do tra danh sach voi gioi han 1.500');
  check(all.body.items.length === Math.min(all.body.total, 1500) && all.body.items.every(i => i.lat != null && i.lng != null), 'Moi muc tren ban do deu co toa do va khong bi cat o 300');
  const withCoords = json(database, `SELECT COUNT(*) N FROM dbo.DiscoveryEntry WHERE Status='Published' AND Latitude IS NOT NULL FOR JSON PATH;`)[0].N;
  check(all.body.items.length > 0 && all.body.items.length <= withCoords, 'So vi tri khong vuot qua so muc da cong bo co toa do');
  const ids = all.body.items.map(i => i.id); check(new Set(ids).size === ids.length, 'Khong co muc trung lap');
  // thu tu on dinh giua hai lan goi (sap xep tren cot hep)
  const again = await get('/api/discovery/map?Q=');
  check(JSON.stringify(again.body.items.map(i => i.id)) === JSON.stringify(ids), 'Thu tu ket qua on dinh giua cac lan goi');
  // loc theo nhom van ap dung cho ban do
  const stay = await get('/api/discovery/map?Group=Stay');
  check(stay.status === 200 && stay.body.items.length <= all.body.items.length, 'Bo loc nhom van ap dung cho ban do');
  // quanh mot diem: API van ho tro, nhung trang khong con gui Lat/Lng khi tai ban do
  const sample = all.body.items[0];
  const near = await get(`/api/discovery/map?Lat=${sample.lat}&Lng=${sample.lng}&RadiusKm=2`);
  check(near.status === 200 && near.body.items.some(i => i.id === sample.id) && near.body.items.length <= all.body.items.length, 'Tim quanh mot diem van tra dung muc o gan');

  // 2. Trang ket qua co cot danh sach va khung quanh day
  const page = decode((await get('/Discovery?Group=Stay')).text);
  check(page.includes('data-ds-list') && page.includes('data-ds-near') && page.includes('data-ds-near-list') && page.includes('data-ds-near-close'), 'Trang ket qua co cot danh sach va khung quanh day');
  check(page.includes('Bấm một ghim để đưa nơi đó lên đầu danh sách'), 'Co huong dan bam ghim');
  check(page.includes('data-entry-card='), 'The ket qua co ma de dua len dau khi bam ghim');

  // 3. Script va kieu dang
  const js = (await get('/js/discovery.js')).text;
  check(js.includes("for(const k of ['Lat','Lng','lat','lng','RadiusKm'") && !js.includes('tối đa 300'), 'Ban do khong con gui dieu kien vi tri khi tai, bo gioi han 300');
  check(js.includes(".on('click',()=>pick(first))") && js.includes('grid.prepend(card)'), 'Bam ghim dua muc len dau danh sach');
  check(js.includes('function renderNear()') && js.includes('sort((a,b)=>a.d-b.d)') && js.includes('data-ds-near-close'), 'Danh sach quanh day xep gan nhat truoc va dong lai duoc');
  const css = (await get('/css/discovery.css')).text;
  check(css.includes('[data-ds-list]{max-height:calc(100vh - 96px);overflow-y:auto') && css.includes('.ds-map-area{position:sticky'), 'Danh sach cuon trong man hinh, ban do dung yen');

  // 4. Trang tim kiem van phan trang 20 muc
  const list = decode((await get('/Discovery?Q=')).text);
  check((list.match(/data-entry-card=/g) || []).length <= 20, 'Danh sach van phan trang toi da 20 muc');
  console.log(`PASS ${results.length} map-list checks.`);
})().catch(e => { console.error(e); process.exit(1); });
