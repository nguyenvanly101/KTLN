// Diem xuat phat / ket thuc va duong di tung ngay: luu, doc lai, tinh tuyen (Goong hoac uoc tinh), chan du lieu sai.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/trip-route.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, base, decode } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
const user = label => fixture.users.find(x => x.label === label);
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const day = n => new Date(Date.now() + n * 864e5).toISOString().slice(0, 10);
const stamp = Date.now().toString().slice(-7);
const api = async (c, token, path, method = 'GET', body) => {
  const r = await fetch(base + path, { method, redirect: 'manual', headers: { cookie: [...c.cookies].map(([k, v]) => k + '=' + v).join('; '), ...(token ? { RequestVerificationToken: token } : {}), ...(body ? { 'content-type': 'application/json' } : {}) }, body: body ? JSON.stringify(body) : undefined });
  let j = null; try { j = await r.json(); } catch { } return { status: r.status, body: j };
};

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { } if (n > 45) throw Error('not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const t = new Client(), guest = new Client(), stranger = new Client();
  const email = `tr-nguoila-${stamp}@example.com`;
  sql(database, `INSERT dbo.TaiKhoan(Email,SoDienThoai,MatKhauBam,VaiTro,TrangThai,DauPhien,PhaiDoiMatKhau,ThoiDiemTao) SELECT N'${email}',NULL,MatKhauBam,VaiTro,TrangThai,NEWID(),0,SYSUTCDATETIME() FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}';`);
  const sid = json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${email}' FOR JSON PATH;`)[0].Id;
  sql(database, `INSERT dbo.HoSoNguoiDung(MaTaiKhoan,TenHienThi,ThongTinChiaSe,ThoiDiemCapNhat) VALUES(${sid},N'Nguoi la ${stamp}','CongKhai',SYSUTCDATETIME());`);
  await t.login(user('traveler').email, 'BranchTest@2026'); await stranger.login(email, 'BranchTest@2026');
  const page = await t.get('/lapkehoach'); const tk = val(page, '__RequestVerificationToken');
  check(decode(page.html).includes('id="pe-origin"') && decode(page.html).includes('id="pe-end-same"'), 'Trang lap ke hoach co o diem xuat phat va ket thuc');

  // dia diem co toa do trong co so thu
  const places = json(database, `SELECT TOP 3 e.Id EId, e.PlaceId Id, e.Title T, e.Latitude La, e.Longitude Lo FROM dbo.DiscoveryEntry e WHERE e.Kind=N'Place' AND e.PlaceId IS NOT NULL ORDER BY e.Id FOR JSON PATH;`);
  places.forEach((p, i) => sql(database, `UPDATE dbo.DiscoveryEntry SET Latitude=${15.87 + i * 0.01},Longitude=${108.33 + i * 0.01},Status='Published' WHERE Id=${p.EId};`));
  check(places.length === 3, 'Co du 3 dia diem co toa do de thu');
  const origin = { name: 'Khach san thu', lat: 15.88, lng: 108.33 }, end = { name: 'Ben xe thu', lat: 15.9, lng: 108.3 };
  const draft = { notes: [], id: 0, name: 'QA Tuyen ' + stamp, destination: 'Hội An', start: day(5), days: 2, budget: 3000000, origin, end,
    items: [{ id: 0, day: 1, time: '09:00', content: places[0].T, placeId: places[0].Id }, { id: 0, day: 1, time: '12:00', content: places[1].T, placeId: places[1].Id }, { id: 0, day: 1, time: '15:00', content: 'Ca phe tu do' }, { id: 0, day: 2, time: '09:00', content: places[2].T, placeId: places[2].Id }] };
  const created = await api(t, tk, '/api/lapkehoach', 'POST', draft); const id = created.body && created.body.id;
  check(created.status === 200 && id > 0, 'Luu chuyen di kem diem xuat phat va ket thuc');
  const row = json(database, `SELECT StartName,StartLat,EndName,EndLat FROM dbo.DiemTuyenChuyenDi WHERE PlanId=${id} FOR JSON PATH;`)[0];
  check(row && row.StartName === origin.name && Math.abs(row.StartLat - 15.88) < 1e-6 && row.EndName === end.name, 'Diem xuat phat va ket thuc duoc luu vao co so du lieu');
  const edit = decode((await t.get('/lapkehoach/' + id)).html);
  check(edit.includes('Khach san thu') && edit.includes('Ben xe thu'), 'Mo lai ban nhap thay lai hai diem da luu');

  // tinh tuyen
  const r1 = await api(t, '', `/api/lapkehoach/${id}/tuyen?day=1&mode=bike`); const d = r1.body;
  check(r1.status === 200 && d.points.length === 4 && d.points[0].kind === 'origin' && d.points[3].kind === 'end', 'Tuyen ngay 1: xuat phat, 2 diem dung co toa do, ket thuc');
  check(d.skipped === 1, 'Hang muc khong co toa do duoc bao la chua tinh');
  check(d.legs.length === 3 && d.legs.every(l => l.km > 0 && l.minutes > 0) && d.totalKm > 0 && d.totalMinutes > 0, 'Moi doan co km va phut, co tong cong');
  check(Array.isArray(d.line) && d.line.length >= 2, 'Co duong ve tren ban do');
  const r2 = await api(t, '', `/api/lapkehoach/${id}/tuyen?day=2&mode=car`);
  check(r2.status === 200 && r2.body.points.length === 3 && r2.body.mode === 'car', 'Tuyen ngay 2 voi o to: xuat phat, 1 diem dung, ve diem ket thuc');
  const r3 = await api(t, '', `/api/lapkehoach/${id}/tuyen?day=1&mode=foot`);
  check(r3.status === 200 && r3.body.anyEstimated === true, 'Di bo luon la uoc tinh');
  check((await api(t, '', `/api/lapkehoach/${id}/tuyen?day=1&mode=ten-la`)).body.mode === 'bike', 'Phuong tien la ve mac dinh xe may');
  check((await api(t, '', `/api/lapkehoach/${id}/tuyen?day=9`)).status >= 400, 'Ngay ngoai chuyen di bi tu choi');
  check((await api(stranger, '', `/api/lapkehoach/${id}/tuyen?day=1`)).status >= 400, 'Nguoi la khong xem duoc tuyen cua chuyen di nguoi khac');
  check((await guest.get(`/api/lapkehoach/${id}/tuyen?day=1`)).status >= 300, 'Khach chua dang nhap khong goi duoc tuyen');

  // dat diem xuat phat tu trang Hom nay (vi tri hien tai): giu nguyen diem ket thuc
  const so = await api(t, tk, `/api/lapkehoach/${id}/xuat-phat`, 'POST', { name: 'Vi tri hien tai thu', lat: 15.871234, lng: 108.331234 });
  const row2 = json(database, `SELECT StartName,StartLat,EndName FROM dbo.DiemTuyenChuyenDi WHERE PlanId=${id} FOR JSON PATH;`)[0];
  check(so.status === 200 && row2.StartName === 'Vi tri hien tai thu' && Math.abs(row2.StartLat - 15.871234) < 1e-6 && row2.EndName === end.name, 'Dat diem xuat phat bang vi tri hien tai, diem ket thuc giu nguyen');
  check((await api(t, tk, `/api/lapkehoach/${id}/xuat-phat`, 'POST', { name: 'Paris', lat: 48.85, lng: 2.35 })).status === 422, 'Vi tri hien tai ngoai Viet Nam bi tu choi');
  check((await api(stranger, tk, `/api/lapkehoach/${id}/xuat-phat`, 'POST', { name: 'Ke la', lat: 16, lng: 108 })).status >= 400, 'Nguoi la khong dat duoc diem xuat phat');
  check(json(database, `SELECT StartName FROM dbo.DiemTuyenChuyenDi WHERE PlanId=${id} FOR JSON PATH;`)[0].StartName === 'Vi tri hien tai thu', 'Yeu cau bi tu choi khong lam doi diem xuat phat');

  // ket thuc de trong => quay ve diem xuat phat
  const items = json(database, `SELECT MaHoatDong Id,Ngay,Gio,NoiDung,MaDiaDiem PlaceId FROM dbo.HoatDongLichTrinh WHERE MaKeHoach=${id} ORDER BY Ngay,Gio FOR JSON PATH;`);
  const start = json(database, `SELECT CONVERT(varchar(10),NgayBatDau,23) S FROM dbo.KeHoachChuyenDi WHERE MaKeHoach=${id} FOR JSON PATH;`)[0].S;
  const upd = await api(t, tk, '/api/lapkehoach', 'POST', { notes: [], id, name: draft.name, destination: 'Hội An', start, days: 2, budget: 3000000, origin, end: null,
    items: items.map(i => ({ id: i.Id, day: Math.round((new Date(i.Ngay) - new Date(start)) / 864e5) + 1, time: String(i.Gio).slice(0, 5), content: i.NoiDung, placeId: i.PlaceId })) });
  check(upd.status === 200, 'Luu lai khi bo diem ket thuc (quay ve diem xuat phat)');
  const r4 = await api(t, '', `/api/lapkehoach/${id}/tuyen?day=2`);
  check(r4.body.points.length === 3 && r4.body.points[2].kind === 'end' && r4.body.points[2].name === origin.name, 'Khong co diem ket thuc thi ve lai diem xuat phat');

  // du lieu sai
  const bad = await api(t, tk, '/api/lapkehoach', 'POST', { ...draft, name: 'QA sai ' + stamp, origin: { name: 'Paris', lat: 48.85, lng: 2.35 } });
  check(bad.status === 422, 'Diem xuat phat ngoai Viet Nam bi tu choi');
  check((await api(stranger, tk, '/api/lapkehoach', 'POST', { notes: [], id, name: draft.name, destination: 'Hội An', start, days: 2, budget: 3000000, origin: { name: 'Kẻ lạ', lat: 16, lng: 108 }, items: [] })).status >= 400, 'Nguoi la khong doi duoc diem xuat phat cua chuyen di nguoi khac');
  const gs = await api(t, '', '/api/lapkehoach/diem-goi-y?q=hoi%20an');
  check(gs.status === 200 && Array.isArray(gs.body.hits) && typeof gs.body.limited === 'boolean', 'Goi y diem tra ve danh sach va co bao khi Goong khong tra loi');
  const word = places[0].T.slice(0, Math.min(12, places[0].T.length));
  const ls = await api(t, '', '/api/lapkehoach/diem-goi-y?q=' + encodeURIComponent(word));
  check(ls.body.hits.some(h => h.name === places[0].T && h.lat != null && h.lng != null), 'Dia diem co san tren website duoc goi y kem toa do (khong can Goong)');
  check((await api(t, '', '/api/lapkehoach/diem-goi-y?q=a')).body.hits.length === 0, 'Tu khoa qua ngan khong goi y');
  const ed = decode((await t.get('/lapkehoach/' + id)).html);
  check(ed.includes('id="pe-origin-map"') && ed.includes('id="pe-end-map"') && ed.includes('id="pe-point"') && ed.includes('id="pe-origin-state"'), 'Trang lap ke hoach co nut Chon tren ban do va dong trang thai vi tri');
  const pjs = await (await fetch(base + '/js/plan-editor.js')).text();
  check(pjs.includes('originPick.pending()') && pjs.includes('Chưa có vị trí'), 'Script chan luu khi go ten ma chua chon vi tri');
  check((await api(t, '', '/api/lapkehoach/diem-chi-tiet?placeId=khong-co')).status === 422, 'Chi tiet diem voi ma la bi tu choi');
  check((await guest.get('/api/lapkehoach/diem-goi-y?q=hoi%20an')).status >= 300, 'Khach chua dang nhap khong goi y duoc diem');
  const detail = decode((await t.get('/Plans/Detail/' + id)).html);
  check(detail.includes('/js/trip-route.js'), 'Trang chuyen di nap script duong di');
  const js = await (await fetch(base + '/js/trip-route.js')).text();
  check(js.includes('/tuyen?day=') && js.includes('route-leg') && js.includes('fitBounds'), 'Script ve doan duong va ban do');

  const today = decode((await t.get('/Journey/Today?planId=' + id + '&date=' + day(5))).html);
  check(today.includes('id="today-route"') && today.includes('/js/today-route.js') && today.includes('data-day="1"'), 'Trang Hom nay co khung Duong di hom nay trong khoang ngay cua chuyen di');
  const outside = decode((await t.get('/Journey/Today?planId=' + id + '&date=' + day(30))).html);
  check(!outside.includes('id="today-route"'), 'Ngay ngoai chuyen di khong co khung duong di');
  const tjs = await (await fetch(base + '/js/today-route.js')).text();
  check(tjs.includes('travelmode') && tjs.includes('/tuyen?day='), 'Script co nut chi duong toi diem ke tiep');
  const near = decode((await t.get('/Journey/Today?planId=' + id + '&date=' + day(5))).html);
  check(!near.includes('id="depart"') || near.includes('data-origin-here'), 'Khung San sang khoi hanh (neu hien) co nut dat diem xuat phat bang vi tri hien tai');
  places.forEach(p => sql(database, `UPDATE dbo.DiscoveryEntry SET Latitude=${p.La == null ? 'NULL' : p.La},Longitude=${p.Lo == null ? 'NULL' : p.Lo} WHERE Id=${p.EId};`));
  sql(database, `DELETE FROM dbo.DiemTuyenChuyenDi WHERE PlanId=${id}; DELETE FROM dbo.HoatDongLichTrinh WHERE MaKeHoach=${id}; DELETE FROM dbo.KeHoachChuyenDi WHERE MaKeHoach=${id};`);
  console.log(`PASS ${results.length} trip-route checks.`);
})().catch(e => { console.error(e); process.exit(1); });
