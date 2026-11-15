// Khoi hanh / ket thuc chuyen di va bo qua hang muc tren trang Hom nay.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/trip-depart.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, base, decode } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
const user = label => fixture.users.find(x => x.label === label);
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const day = n => new Date(Date.now() + 7 * 36e5 + n * 864e5).toISOString().slice(0, 10);
const stamp = Date.now().toString().slice(-7);
const api = async (c, token, path, method = 'GET', body) => {
  const r = await fetch(base + path, { method, redirect: 'manual', headers: { cookie: [...c.cookies].map(([k, v]) => k + '=' + v).join('; '), ...(token ? { RequestVerificationToken: token } : {}), ...(body ? { 'content-type': 'application/json' } : {}) }, body: body ? JSON.stringify(body) : undefined });
  let j = null; try { j = await r.json(); } catch { } return { status: r.status, body: j };
};

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { } if (n > 45) throw Error('not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const t = new Client(), stranger = new Client();
  const email = `kh-nguoila-${stamp}@example.com`;
  sql(database, `INSERT dbo.TaiKhoan(Email,SoDienThoai,MatKhauBam,VaiTro,TrangThai,DauPhien,PhaiDoiMatKhau,ThoiDiemTao) SELECT N'${email}',NULL,MatKhauBam,VaiTro,TrangThai,NEWID(),0,SYSUTCDATETIME() FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}';`);
  const sid = json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${email}' FOR JSON PATH;`)[0].Id;
  sql(database, `INSERT dbo.HoSoNguoiDung(MaTaiKhoan,TenHienThi,ThongTinChiaSe,ThoiDiemCapNhat) VALUES(${sid},N'Nguoi la ${stamp}','CongKhai',SYSUTCDATETIME());`);
  await t.login(user('traveler').email, 'BranchTest@2026'); await stranger.login(email, 'BranchTest@2026');
  const tk = val(await t.get('/lapkehoach'), '__RequestVerificationToken');
  const draft = { notes: [], id: 0, name: 'QA Khoi hanh ' + stamp, destination: 'Hội An', start: day(0), days: 2, budget: 3000000, origin: { name: 'Khach san thu', lat: 15.88, lng: 108.33 },
    items: [{ id: 0, day: 1, time: '08:00', content: 'Diem dau', cost: 0 }, { id: 0, day: 1, time: '10:00', content: 'Diem hai', cost: 0 }, { id: 0, day: 1, time: '12:00', content: 'Diem ba', cost: 0 }] };
  const id = (await api(t, tk, '/api/lapkehoach', 'POST', draft)).body.id;
  const future = (await api(t, tk, '/api/lapkehoach', 'POST', { ...draft, name: 'QA Tuong lai ' + stamp, start: day(10), items: [] })).body.id;
  const today = path => t.get(path);
  const row = pid => json(database, `SELECT DepartedAt D,FinishedAt F FROM dbo.TienDoChuyenDi WHERE PlanId=${pid} FOR JSON PATH;`)[0];
  const act = () => json(database, `SELECT MaHoatDong Id,NoiDung N,HoanThanhLuc H,BoQuaLuc B FROM dbo.HoatDongLichTrinh WHERE MaKeHoach=${id} ORDER BY Gio FOR JSON PATH;`);

  // 1. Truoc khi khoi hanh
  let page = await today('/Journey/Today?planId=' + id); let h = decode(page.html);
  check(page.status === 200 && h.includes('Sẵn sàng khởi hành?') && h.includes('action="/Journey/Depart"') && h.includes('Xuất phát từ Khach san thu') && h.includes('Điểm dừng đầu tiên: Diem dau'), 'Truoc khi di: hien khung Khoi hanh kem diem xuat phat va diem dung dau');
  check(!h.includes('Kết thúc chuyến'), 'Chua khoi hanh thi chua co nut Ket thuc chuyen');
  const fh = decode((await today('/Journey/Today?planId=' + future + '&date=' + day(10))).html);
  check(!fh.includes('action="/Journey/Depart"'), 'Chuyen con xa chua duoc khoi hanh');
  const earlyTry = await t.post('/Journey/Depart', page, { planId: future });
  check(earlyTry.status >= 400 || !row(future), 'Khoi hanh chuyen chua toi ngay bi tu choi');
  check(row(future) === undefined, 'Chuyen con xa khong bi ghi tien do');
  check((await t.post('/Journey/Finish', page, { planId: id })).status >= 300 && row(id) === undefined, 'Ket thuc khi chua khoi hanh khong ghi gi');

  // 2. Khoi hanh
  const strangerPage = await stranger.get('/Plans');
  const sTry = await stranger.post('/Journey/Depart', strangerPage, { planId: id });
  check(row(id) === undefined, 'Nguoi la khong khoi hanh duoc chuyen cua nguoi khac');
  const go = await t.post('/Journey/Depart', page, { planId: id });
  check(go.status === 302 && row(id) && row(id).D, 'Chu chuyen bam Khoi hanh: ghi lai gio di');
  page = await today('/Journey/Today?planId=' + id); h = decode(page.html);
  check(h.includes('Đang trên đường') && h.includes('action="/Journey/Finish"') && !h.includes('Sẵn sàng khởi hành?'), 'Sau khi khoi hanh: hien Dang tren duong va nut Ket thuc chuyen');
  const dep1 = row(id).D;
  await t.post('/Journey/Depart', page, { planId: id });
  check(row(id).D === dep1, 'Khoi hanh lan hai khong ghi de gio di');

  // 3. Bo qua hang muc
  const first = act()[0], second = act()[1];
  const ver = () => decode((page.html)).match(/name="Version" value="([^"]+)"/)[1];
  check(h.includes('Diem dau') && h.includes('Xong, đến điểm tiếp theo'), 'Diem dung tiep theo co nut Xong va Bo qua');
  let r = await t.post('/Journey/Complete', page, { planId: id, Id: first.Id, Version: ver(), Decision: 'Skip' });
  let a1 = act()[0];
  check(r.status === 302 && a1.B && !a1.H, 'Bo qua: ghi BoQuaLuc, khong tinh la hoan thanh');
  page = await today('/Journey/Today?planId=' + id); h = decode(page.html);
  check(h.includes('Đã bỏ qua') && /ĐIỂM DỪNG TIẾP THEO[\s\S]{0,80}Diem hai/.test(h.replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ')) , 'Hang muc bo qua duoc danh dau va diem tiep theo chuyen sang hang muc sau');
  check(/0 \/ 3/.test(h.replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ')) && h.includes('1 bỏ qua'), 'Tien do dem 0/3 xong va 1 bo qua');
  r = await t.post('/Journey/Complete', page, { planId: id, Id: second.Id, Version: ver(), Decision: 'Done' });
  check(act()[1].H && !act()[1].B, 'Danh dau xong hang muc hai');
  page = await today('/Journey/Today?planId=' + id);
  r = await t.post('/Journey/Complete', page, { planId: id, Id: first.Id, Version: decode(page.html).match(/name="Version" value="([^"]+)"/)[1], Decision: 'Undo' });
  a1 = act()[0]; check(!a1.B && !a1.H, 'Mo lai hang muc da bo qua');
  page = await today('/Journey/Today?planId=' + id);
  r = await t.post('/Journey/Complete', page, { planId: id, Id: first.Id, Version: decode(page.html).match(/name="Version" value="([^"]+)"/)[1], Decision: 'Skip' });
  page = await today('/Journey/Today?planId=' + id);
  r = await t.post('/Journey/Complete', page, { planId: id, Id: first.Id, Version: decode(page.html).match(/name="Version" value="([^"]+)"/)[1], Decision: 'Done' });
  a1 = act()[0]; check(a1.H && !a1.B, 'Bo qua roi danh dau xong thi mat dau bo qua');
  page = await today('/Journey/Today?planId=' + id);
  r = await t.post('/Journey/Complete', page, { planId: id, Id: first.Id, Version: decode(page.html).match(/name="Version" value="([^"]+)"/)[1], Decision: 'Khac' });
  check(r.status >= 400, 'Thao tac la bi tu choi');

  // 4. Ket thuc
  page = await today('/Journey/Today?planId=' + id);
  check((await stranger.post('/Journey/Finish', strangerPage, { planId: id })).status >= 300 && !row(id).F, 'Nguoi la khong ket thuc duoc chuyen cua nguoi khac');
  const fin = await t.post('/Journey/Finish', page, { planId: id });
  check(fin.status === 302 && row(id).F, 'Chu chuyen ket thuc chuyen di');
  const finAt = row(id).F; page = await today('/Journey/Today?planId=' + id); h = decode(page.html);
  check(h.includes('Chuyến đi đã kết thúc') && !h.includes('action="/Journey/Finish"') && !h.includes('Sẵn sàng khởi hành?'), 'Sau khi ket thuc: hien trang thai da ket thuc, khong con nut');
  await t.post('/Journey/Finish', page, { planId: id });
  check(row(id).F === finAt, 'Ket thuc lan hai khong ghi de');

  sql(database, `DELETE FROM dbo.TienDoChuyenDi WHERE PlanId IN (${id},${future}); DELETE FROM dbo.DiemTuyenChuyenDi WHERE PlanId IN (${id},${future}); DELETE FROM dbo.HoatDongLichTrinh WHERE MaKeHoach IN (${id},${future}); DELETE FROM dbo.KeHoachChuyenDi WHERE MaKeHoach IN (${id},${future});`);
  console.log(`PASS ${results.length} trip-depart checks.`);
})().catch(e => { console.error(e); process.exit(1); });
