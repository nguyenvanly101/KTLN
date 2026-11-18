// Danh gia dich vu tu dau den cuoi theo 3 vai:
//  khach viet danh gia (yeu cau da xac nhan va qua ngay dung, hoac da dung) -> hien ngay -> nha cung cap phan hoi (hien ngay)
//  -> quan tri chi an noi dung khong phu hop (kem ly do) hoac hien lai; sua danh gia thi noi dung moi hien ngay.
// Kiem ca quyen: nha cung cap / nha cung cap khac khong viet hay phan hoi thay, khong danh gia yeu cau cua nguoi khac, khong danh gia trung.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/review-flow.cjs
const assert = require('node:assert/strict'), fs = require('node:fs');
const { Client, decode, base } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const fx = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8')); const user = l => fx.users.find(x => x.label === l);
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const one = q => json(database, q + ' FOR JSON PATH;')[0];
const iso = d => { const t = new Date(Date.now() + 7 * 3600000 + d * 86400000); return t.toISOString().slice(0, 10); };
const val = (html, name) => (decode(html).match(new RegExp('name="' + name + '" value="([^"]*)"')) || [])[1];

(async () => {
  const t = new Client(), p = new Client(), o = new Client(), a = new Client(), g = new Client();
  await t.login(user('traveler').email, 'BranchTest@2026'); await p.login(user('provider').email, 'BranchTest@2026');
  await o.login(user('other').email, 'BranchTest@2026'); await a.login(user('admin').email, 'BranchTest@2026');
  const me = one(`SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}'`).Id;
  const prov = one(`SELECT p.MaNhaCungCap Id FROM dbo.NhaCungCapDichVu p JOIN dbo.TaiKhoan t ON t.MaTaiKhoan=p.MaTaiKhoan WHERE t.Email=N'${user('provider').email}'`).Id;
  // Mot yeu cau cua khach cho dich vu da cong bo cua nha cung cap thu; dua ve "da xac nhan, da qua ngay dung", xoa danh gia cu.
  const bk = one(`SELECT TOP 1 y.MaYeuCau Id, y.MaDichVu S FROM dbo.YeuCauDatDichVu y JOIN dbo.DichVuDuLich s ON s.MaDichVu=y.MaDichVu WHERE y.MaTaiKhoan=${me} AND s.MaNhaCungCap=${prov} AND s.TrangThaiCongBo='DaDuyet' ORDER BY y.MaYeuCau DESC`);
  assert.ok(bk, 'Can mot yeu cau cua khach thu cho dich vu da cong bo');
  sql(database, `DELETE FROM dbo.DanhGiaDichVu WHERE MaYeuCau=${bk.Id}; UPDATE dbo.YeuCauDatDichVu SET TrangThai='DaXacNhan', NgayBatDau='${iso(-3)}', NgayKetThuc='${iso(-2)}' WHERE MaYeuCau=${bk.Id};`);

  // 1. Khach thay duoc moi danh gia o moi noi
  const list = decode((await t.get('/Bookings?loc=da-dung')).html); check(list.includes('#' + bk.Id), 'Yeu cau da xac nhan va qua ngay nam o "Da dung", khong con o "Sap toi"');
  const det = decode((await t.get('/Bookings/Detail/' + bk.Id)).html); check(det.includes('/Reviews/Write?bookingId=' + bk.Id), 'Trang yeu cau co nut Danh gia dich vu');
  const conv = one(`SELECT TOP 1 m.ConversationId C FROM dbo.TinNhanChat m WHERE m.BookingRequestId=${bk.Id} AND m.Kind='Booking'`);
  if (conv) { const th = await t.get('/chat/' + conv.C); const body = JSON.parse(th.html); const card = (body.cards || []).find(x => x.id === bk.Id); check(card && card.canReview === true, 'The dat cho trong chat hien nut Viet danh gia'); }
  const mine = decode((await t.get('/Reviews/Mine')).html); check(mine.includes('/Reviews/Write?bookingId=' + bk.Id) && mine.includes('Chờ bạn đánh giá'), 'Danh sach "Danh gia cua toi" co yeu cau nay o muc cho danh gia');

  // 2. Viet danh gia + kiem tra du lieu nhap
  const form = await t.get('/Reviews/Write?bookingId=' + bk.Id); check(form.status === 200, 'Mo form viet danh gia');
  const bad = await t.post('/Reviews/Write', form, { BookingId: bk.Id, Id: 0, Stars: 0, Content: 'x' }); check(bad.status !== 302, 'Sao ngoai 1..5 bi tu choi');
  const empty = await t.post('/Reviews/Write', form, { BookingId: bk.Id, Id: 0, Stars: 4, Content: '   ' }); check(empty.status !== 302, 'Noi dung rong bi tu choi');
  const ok = await t.post('/Reviews/Write', form, { BookingId: bk.Id, Id: 0, Stars: 4, Content: 'Phục vụ nhanh, món ngon, sẽ quay lại.' });
  check(ok.status === 302, 'Gui danh gia thanh cong');
  const rv = one(`SELECT MaDanhGia Id, TrangThai S FROM dbo.DanhGiaDichVu WHERE MaYeuCau=${bk.Id}`); check(rv && rv.S === 'DaDuyet', 'Danh gia hien ngay, khong cho quan tri duyet');
  const dup = await t.post('/Reviews/Write', form, { BookingId: bk.Id, Id: 0, Stars: 2, Content: 'Gửi lần hai' }); check(dup.status !== 302 || one(`SELECT COUNT(*) N FROM dbo.DanhGiaDichVu WHERE MaYeuCau=${bk.Id}`).N === 1, 'Khong tao danh gia thu hai cho cung yeu cau');
  check(decode((await g.get('/Reviews/Service/' + bk.S)).html).includes('Phục vụ nhanh, món ngon'), 'Danh gia hien cong khai ngay');
  const notes = json(database, `SELECT MaNguoiNhan R, NoiDung N FROM dbo.ThongBao WHERE LoaiThongBao='DanhGia' AND ThoiDiemTao>DATEADD(minute,-5,SYSUTCDATETIME()) FOR JSON PATH;`) || [];
  check(notes.some(n => n.R !== me && n.N.includes('Bạn có thể phản hồi')), 'Nha cung cap duoc bao co danh gia moi');

  // 3. Quyen
  check([403, 404].includes((await p.get('/Reviews/Write?bookingId=' + bk.Id)).status), 'Nha cung cap khong mo duoc form danh gia cua khach');
  const otherBk = one(`SELECT TOP 1 MaYeuCau Id FROM dbo.YeuCauDatDichVu WHERE MaTaiKhoan<>${me} ORDER BY 1 DESC`);
  if (otherBk) check((await t.get('/Reviews/Write?bookingId=' + otherBk.Id)).status === 404, 'Khach khong danh gia yeu cau cua nguoi khac');
  check([403, 404].includes((await t.get('/Reviews/Moderation')).status), 'Khach khong vao duoc trang quan tri danh gia');
  const ver = () => Buffer.from(one(`SELECT CONVERT(varchar(100),CAST(RowVersion AS varbinary(8)),2) V FROM dbo.DanhGiaDichVu WHERE MaDanhGia=${rv.Id}`).V, 'hex').toString('base64');

  // 4. Phan hoi cua nha cung cap: hien ngay
  const part = await p.get('/Reviews/Partner?serviceId=' + bk.S); check(part.status === 200 && decode(part.html).includes('Phục vụ nhanh, món ngon'), 'Nha cung cap thay danh gia cua dich vu minh');
  { const hack = await o.post('/Reviews/Reply', part, { Id: rv.Id, Version: ver(), ServiceId: bk.S, Text: 'Hack' }); check(hack.status !== 302 && hack.status < 500 && one(`SELECT PhanHoiNCC R FROM dbo.DanhGiaDichVu WHERE MaDanhGia=${rv.Id}`).R == null, 'Nha cung cap khac khong phan hoi thay duoc (bi chan, khong ghi gi) [' + hack.status + ']'); }
  const rep = await p.post('/Reviews/Reply', part, { Id: rv.Id, Version: ver(), ServiceId: bk.S, Text: 'Cảm ơn bạn, hẹn gặp lại!' }); check(rep.status === 302, 'Nha cung cap gui phan hoi');
  check(decode((await g.get('/Reviews/Service/' + bk.S)).html).includes('Cảm ơn bạn, hẹn gặp lại'), 'Phan hoi hien ngay duoi danh gia');
  check((json(database, `SELECT COUNT(*) N FROM dbo.ThongBao WHERE MaNguoiNhan=${me} AND NoiDung LIKE N'%đã phản hồi đánh giá%' AND ThoiDiemTao>DATEADD(minute,-5,SYSUTCDATETIME()) FOR JSON PATH;`)[0].N) >= 1, 'Khach duoc bao khi co so phan hoi');

  // 5. Quan tri: chi an (kem ly do) hoac hien lai
  const mod = await a.get('/Reviews/Moderation'); check(mod.status === 200 && decode(mod.html).includes('Phục vụ nhanh, món ngon'), 'Quan tri thay danh sach tat ca danh gia');
  check((await a.post('/Reviews/Moderate', mod, { Id: rv.Id, Version: ver(), Part: 'Review', Decision: 'An', Reason: '' })).status !== 302, 'An danh gia bat buoc co ly do');
  check((await a.post('/Reviews/Moderate', mod, { Id: rv.Id, Version: ver(), Part: 'Review', Decision: 'An', Reason: 'Có thông tin cá nhân' })).status === 302, 'Quan tri an danh gia');
  check(!decode((await g.get('/Reviews/Service/' + bk.S)).html).includes('Phục vụ nhanh, món ngon'), 'Danh gia bi an khong hien cong khai');
  check((json(database, `SELECT COUNT(*) N FROM dbo.ThongBao WHERE MaNguoiNhan=${me} AND NoiDung LIKE N'%đã bị ẩn%Có thông tin cá nhân%' FOR JSON PATH;`)[0].N) >= 1, 'Khach duoc bao danh gia bi an kem ly do');
  check((await a.post('/Reviews/Moderate', mod, { Id: rv.Id, Version: ver(), Part: 'Review', Decision: 'DaDuyet', Reason: '' })).status === 302 && decode((await g.get('/Reviews/Service/' + bk.S)).html).includes('Phục vụ nhanh, món ngon'), 'Quan tri hien lai danh gia');

  // 6. Sua danh gia: hien ngay noi dung moi
  const ef = await t.get('/Reviews/Write?bookingId=' + bk.Id);
  const edit = await t.post('/Reviews/Write', ef, { BookingId: bk.Id, Id: rv.Id, Version: val(ef.html, 'Version'), Stars: 5, Content: 'Sửa lại: rất hài lòng.' });
  check(edit.status === 302 && decode((await g.get('/Reviews/Service/' + bk.S)).html).includes('Sửa lại: rất hài lòng'), 'Sua danh gia thi noi dung moi hien ngay');

  // 7. Yeu cau "sap toi" (chua toi ngay) thi chua danh gia duoc
  sql(database, `UPDATE dbo.YeuCauDatDichVu SET NgayBatDau='${iso(5)}', NgayKetThuc='${iso(6)}' WHERE MaYeuCau=${bk.Id};`);
  check((await t.get('/Reviews/Write?bookingId=' + bk.Id)).status === 404, 'Chua toi ngay dung thi chua danh gia duoc');
  sql(database, `UPDATE dbo.YeuCauDatDichVu SET NgayBatDau='${iso(-3)}', NgayKetThuc='${iso(-2)}' WHERE MaYeuCau=${bk.Id};`);

  console.log(`PASS ${results.length} review-flow checks.`);
})().catch(e => { console.error(e); process.exit(1); });
