// Noi luong chuyen di: tu dien thoi gian di chuyen, thanh vien danh dau xong va ghi su co, bao nhom chat khi khoi hanh/ket thuc, nhac danh gia.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/trip-flow.cjs
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
const mk = tag => {
  const email = `tf-${tag}-${stamp}@example.com`;
  sql(database, `INSERT dbo.TaiKhoan(Email,SoDienThoai,MatKhauBam,VaiTro,TrangThai,DauPhien,PhaiDoiMatKhau,ThoiDiemTao) SELECT N'${email}',NULL,MatKhauBam,VaiTro,TrangThai,NEWID(),0,SYSUTCDATETIME() FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}';`);
  const id = json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${email}' FOR JSON PATH;`)[0].Id;
  sql(database, `INSERT dbo.HoSoNguoiDung(MaTaiKhoan,TenHienThi,ThongTinChiaSe,ThoiDiemCapNhat) VALUES(${id},N'${tag} ${stamp}','CongKhai',SYSUTCDATETIME());`);
  return { email, id };
};

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { } if (n > 45) throw Error('not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const owner = new Client(), member = new Client(), stranger = new Client();
  const M = mk('thanhvien'), S = mk('nguoila');
  const ownerId = json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}' FOR JSON PATH;`)[0].Id;
  await owner.login(user('traveler').email, 'BranchTest@2026'); await member.login(M.email, 'BranchTest@2026'); await stranger.login(S.email, 'BranchTest@2026');
  const tk = val(await owner.get('/lapkehoach'), '__RequestVerificationToken');
  const places = json(database, `SELECT TOP 2 e.Id EId, e.PlaceId Id, e.Title T, e.Latitude La, e.Longitude Lo FROM dbo.DiscoveryEntry e WHERE e.Kind=N'Place' AND e.PlaceId IS NOT NULL ORDER BY e.Id FOR JSON PATH;`);
  places.forEach((p, i) => sql(database, `UPDATE dbo.DiscoveryEntry SET Latitude=${15.87 + i * 0.01},Longitude=${108.33 + i * 0.01},Status='Published' WHERE Id=${p.EId};`));
  const draft = { notes: [], id: 0, name: 'QA Luong ' + stamp, destination: 'Hội An', start: day(0), days: 2, budget: 3000000, origin: { name: 'Khach san thu', lat: 15.88, lng: 108.33 },
    items: [{ id: 0, day: 1, time: '08:00', content: places[0].T, placeId: places[0].Id }, { id: 0, day: 1, time: '10:00', content: places[1].T, placeId: places[1].Id }, { id: 0, day: 1, time: '13:00', content: 'Nghi trua tu do' }] };
  const id = (await api(owner, tk, '/api/lapkehoach', 'POST', draft)).body.id;
  check(id > 0, 'Tao chuyen di bat dau hom nay');
  const acts = () => json(database, `SELECT MaHoatDong Id,NoiDung N,DiChuyenTruocPhut T,HoanThanhLuc H,BoQuaLuc B FROM dbo.HoatDongLichTrinh WHERE MaKeHoach=${id} ORDER BY Gio FOR JSON PATH;`);

  // 1. Tu dien thoi gian di chuyen
  sql(database, `UPDATE dbo.HoatDongLichTrinh SET DiChuyenTruocPhut=45 WHERE MaHoatDong=${acts()[1].Id};`);
  const ap = await api(owner, tk, `/api/lapkehoach/${id}/tuyen/ap-dung?day=1&mode=bike`, 'POST');
  check(ap.status === 200 && ap.body.filled === 1 && ap.body.kept === 1, 'Dien thoi gian di chuyen: 1 hang muc duoc dien, 1 hang muc da nhap duoc giu');
  let a = acts(); check(a[0].T > 0 && a[1].T === 45 && a[2].T == null, 'Hang muc co vi tri duoc dien phut, hang muc nhap tay giu 45, hang muc khong vi tri de trong');
  const ap2 = await api(owner, tk, `/api/lapkehoach/${id}/tuyen/ap-dung?day=1&mode=bike`, 'POST');
  check(ap2.body.filled === 0 && ap2.body.kept === 2, 'Bam lan hai khong ghi de');
  check((await api(owner, '', `/api/lapkehoach/${id}/tuyen/ap-dung?day=1`, 'POST')).status === 400, 'Thieu ma chong gia mao bi tu choi');
  check((await api(stranger, tk, `/api/lapkehoach/${id}/tuyen/ap-dung?day=1`, 'POST')).status >= 400 && acts()[2].T == null, 'Nguoi la khong dien duoc');
  const js = await (await fetch(base + '/js/trip-route.js')).text();
  check(js.includes('/tuyen/ap-dung?day=') && js.includes('Điền thời gian di chuyển vào lịch'), 'Trang chuyen di co nut dien thoi gian di chuyen');

  // 2. Thanh vien chuyen di chung
  sql(database, `INSERT dbo.ThanhVienChuyenDi(PlanId,AccountId,Status,InvitedBy,CreatedAt,RespondedAt) VALUES(${id},${M.id},'Accepted',${ownerId},SYSUTCDATETIME(),SYSUTCDATETIME());`);
  const start = day(0);
  const resave = await api(owner, tk, '/api/lapkehoach', 'POST', { notes: [], id, name: draft.name, destination: 'Hội An', start, days: 2, budget: 3000000, origin: draft.origin,
    items: acts().map((x, i) => ({ id: x.Id, day: 1, time: ['08:00', '10:00', '13:00'][i], content: x.N, placeId: i < 2 ? places[i].Id : null })) });
  check(resave.status === 200, 'Luu lai de tao nhom chat chuyen di');
  const group = json(database, `SELECT ConversationId C FROM dbo.NhomChatChuyenDi WHERE PlanId=${id} FOR JSON PATH;`)[0];
  check(group && group.C > 0, 'Chuyen di chung co nhom chat');
  let page = await member.get('/Journey/Today?planId=' + id); let h = decode(page.html);
  check(page.status === 200 && h.includes('Xong, đến điểm tiếp theo') && !h.includes('action="/Journey/Depart"'), 'Thanh vien thay nut Xong/Bo qua nhung khong co nut Khoi hanh');
  const ver = p => decode(p.html).match(/name="Version" value="([^"]+)"/)[1];
  let r = await member.post('/Journey/Complete', page, { planId: id, Id: acts()[0].Id, Version: ver(page), Decision: 'Done' });
  check(r.status === 302 && acts()[0].H, 'Thanh vien danh dau xong duoc');
  page = await member.get('/Journey/Today?planId=' + id);
  r = await member.post('/Journey/Complete', page, { planId: id, Id: acts()[1].Id, Version: ver(page), Decision: 'Skip' });
  check(acts()[1].B && !acts()[1].H, 'Thanh vien bo qua hang muc duoc');
  const sPage = await stranger.get('/Plans');
  await stranger.post('/Journey/Complete', sPage, { planId: id, Id: acts()[2].Id, Version: ver(page), Decision: 'Done' });
  check(!acts()[2].H, 'Nguoi la khong danh dau duoc');
  const mtk = val(await member.get('/Plans/Detail/' + id), '__RequestVerificationToken');
  check((await api(member, mtk, '/api/lapkehoach/su-co', 'POST', { planId: id, type: 'SucKhoeCaNhan', description: 'Thanh vien bao met' })).status === 200, 'Thanh vien ghi duoc su co');
  check((await api(stranger, mtk, '/api/lapkehoach/su-co', 'POST', { planId: id, type: 'Khac', description: 'x' })).status >= 400, 'Nguoi la van khong ghi duoc su co');
  check(json(database, `SELECT COUNT(*) N FROM dbo.TinhHuongPhatSinh WHERE MaKeHoach=${id} FOR JSON PATH;`)[0].N === 1, 'Chi co mot su co duoc luu');
  const depTry = await member.post('/Journey/Depart', page, { planId: id });
  check(json(database, `SELECT COUNT(*) N FROM dbo.TienDoChuyenDi WHERE PlanId=${id} FOR JSON PATH;`)[0].N === 0, 'Thanh vien khong khoi hanh thay chu chuyen');

  // 3. Bao nhom khi khoi hanh / ket thuc, nhac danh gia
  const msgs = like => json(database, `SELECT COUNT(*) N FROM dbo.TinNhanChat WHERE ConversationId=${group.C} AND Body LIKE N'%${like}%' FOR JSON PATH;`)[0].N;
  let op = await owner.get('/Journey/Today?planId=' + id);
  r = await owner.post('/Journey/Depart', op, { planId: id });
  check(r.status === 302 && msgs('Khởi hành') === 1, 'Khoi hanh: nhom chat nhan mot tin bao');
  op = await owner.get('/Journey/Today?planId=' + id);
  r = await owner.post('/Journey/Finish', op, { planId: id });
  check(r.status === 302 && msgs('đã kết thúc') === 1, 'Ket thuc: nhom chat nhan mot tin bao');
  op = await owner.get('/Journey/Today?planId=' + id); h = decode(op.html);
  check(h.includes('Chuyến đi đã kết thúc') && h.includes('/Expenses?planId=' + id) && h.includes('/Journey/Share?planId=' + id), 'Sau khi ket thuc co loi vao chi tieu va chia se');

  // don dep
  places.forEach(p => sql(database, `UPDATE dbo.DiscoveryEntry SET Latitude=${p.La == null ? 'NULL' : p.La},Longitude=${p.Lo == null ? 'NULL' : p.Lo} WHERE Id=${p.EId};`));
  console.log(`PASS ${results.length} trip-flow checks.`);
})().catch(e => { console.error(e); process.exit(1); });
