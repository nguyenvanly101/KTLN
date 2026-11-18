// Tim dich vu doc tuyen bang Goong: goi y diem, lay toa do, doi toa do ra ten, tinh tuyen nhieu diem (khong con OSRM).
// Can khoa Goong (Discovery:Goong:RestKey). Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 node tests/route-goong.cjs
const assert = require('node:assert/strict');
const { Client, val, base, decode } = require('./fulltext-smoke.cjs');
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const json = async (c, path, opt) => { const r = await fetch(base + path, { ...(opt || {}), headers: { cookie: [...c.cookies].map(([k, v]) => k + '=' + v).join('; '), ...((opt || {}).headers || {}) } }); return { status: r.status, body: await r.json().catch(() => null) }; };

(async () => {
  const g = new Client();
  const page = await g.get('/Discovery?Q=');
  const html = decode(page.html);
  check(html.includes('data-rt-stops') && html.includes('data-rt-mode="bike"') && !html.includes('OSRM') && !html.includes('ds-route-target'), 'Bang tuyen moi: o Di tu / Den, nut phuong tien, khong con nhac OSRM');
  const hits = await json(g, '/api/discovery/diem?q=' + encodeURIComponent('Phố cổ Hội An'));
  check(hits.status === 200 && hits.body.hits.length > 0, 'Goi y diem tu Goong cho khach chua dang nhap');
  const a = await json(g, '/api/discovery/diem-chi-tiet?placeId=' + encodeURIComponent(hits.body.hits[0].placeId));
  check(a.status === 200 && Math.abs(a.body.lat - 15.88) < 0.3 && Math.abs(a.body.lng - 108.33) < 0.3, 'Chon goi y lay duoc toa do dung khu vuc');
  const name = await json(g, '/api/discovery/ten-vi-tri?lat=16.0611&lng=108.2272');
  check(name.status === 200 && typeof name.body.name === 'string' && name.body.name.length > 0, 'Ghim tren ban do doi ra ten dia diem');
  const token = val(page, '__RequestVerificationToken');
  const route = await json(g, '/api/discovery/route', { method: 'POST', headers: { 'content-type': 'application/json', RequestVerificationToken: token }, body: JSON.stringify({ points: [{ lat: 16.0611, lng: 108.2272, name: 'Cầu Rồng' }, { lat: a.body.lat, lng: a.body.lng, name: 'Hội An' }], mode: 'bike', corridorKm: 2, maxDetourMinutes: null, filter: {} }) });
  check(route.status === 200 && route.body.provider === 'Goong' && route.body.distanceKm > 20 && route.body.distanceKm < 45 && route.body.coordinates.length > 10, 'Tinh tuyen Da Nang - Hoi An bang Goong (hinh duong that, khong phai duong thang)');
  const multi = await json(g, '/api/discovery/route', { method: 'POST', headers: { 'content-type': 'application/json', RequestVerificationToken: token }, body: JSON.stringify({ points: [{ lat: 16.0611, lng: 108.2272 }, { lat: 15.8801, lng: 108.3380 }, { lat: 15.5736, lng: 108.4740 }], mode: 'car', corridorKm: 1, filter: {} }) });
  check(multi.status === 200 && multi.body.provider === 'Goong' && multi.body.distanceKm > 60, 'Tuyen nhieu diem giu thu tu, tinh mot lan bang Goong');
  console.log(`PASS ${results.length} route-goong checks.`);
})().catch(e => { console.error(e); process.exit(1); });
