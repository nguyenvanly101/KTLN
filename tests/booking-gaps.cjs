// Dat dich vu giua khach va nha cung cap: yeu cau gui theo mau duoc gan vao tro chuyen, nha cung cap thay ten khach va yeu cau cho o co so khac,
// khach thay ro de xuat doi gi, yeu cau qua ngay tu dong.
// Chay (may chu can Chat__ReminderIntervalSeconds=5): FULLTEXT_TEST_BASE=http://127.0.0.1:5227 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/booking-gaps.cjs
const fs = require('node:fs'), assert = require('node:assert/strict'), crypto = require('node:crypto');
const { Client, val, base, decode } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
const user = label => fixture.users.find(x => x.label === label);
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const day = n => new Date(Date.now() + n * 864e5 + 7 * 36e5).toISOString().slice(0, 10); // ngay theo gio Viet Nam
const wait = ms => new Promise(r => setTimeout(r, ms));

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { } if (n > 45) throw Error('not ready'); await wait(1000); }
  const t = new Client(), p = new Client();
  await t.login(user('traveler').email, 'BranchTest@2026'); await p.login(user('provider').email, 'BranchTest@2026');
  const tid = user('traveler').id;
  const svc = json(database, `SELECT TOP 1 s.MaDichVu Id, x.BranchId FROM dbo.DichVuDuLich s JOIN dbo.DichVuCoSo x ON x.ServiceId=s.MaDichVu WHERE s.MaNhaCungCap=${user('provider').providerId} AND s.TrangThaiCongBo='DaDuyet' AND s.TrangThaiTiepNhan='TiepNhan' ORDER BY s.MaDichVu DESC FOR JSON PATH;`)[0];
  const name = json(database, `SELECT TenHienThi N FROM dbo.HoSoNguoiDung WHERE MaTaiKhoan=${tid} FOR JSON PATH;`)[0].N;
  const status = id => json(database, `SELECT TrangThai S, LyDo L FROM dbo.YeuCauDatDichVu WHERE MaYeuCau=${id} FOR JSON PATH;`)[0];
  const cards = id => json(database, `SELECT Id, ConversationId C, SenderId S FROM dbo.TinNhanChat WHERE BookingRequestId=${id} AND Kind='Booking' FOR JSON PATH;`) || [];

  // 1. Khach gui yeu cau theo mau (hai buoc)
  const form = await t.get('/yeu-cau/mo-dat-dich-vu?maDichVu=' + svc.Id); // bieu mau day du (duong dan tieng Viet), /Bookings/Create nay chuyen sang bang dat
  check(form.status === 200, 'Khach mo bieu mau dat dich vu');
  const fields = { ServiceId: svc.Id, BranchId: svc.BranchId, SubmissionId: crypto.randomUUID(), Start: day(6), People: 3, Quantity: 1, Notes: 'Ban gan cua so' };
  const preview = await t.post('/yeu-cau/kiem-tra-truoc-gui', form, fields);
  check(preview.status === 200 && val(preview, 'BranchVersion'), 'Buoc kiem tra truoc khi gui ghi nhan co so');
  const sent = await t.post('/yeu-cau/gui-yeu-cau', preview, { ...fields, BranchVersion: val(preview, 'BranchVersion') });
  const id = +(sent.location || '').split('/').pop();
  check(sent.status === 302 && id > 0, 'Gui yeu cau theo mau thanh cong');

  // 2. Yeu cau tu gan vao cuoc tro chuyen giua khach va co so
  let c = cards(id);
  check(c.length === 1 && c[0].S === tid, 'Yeu cau gui theo mau sinh the dat cho trong tro chuyen, dung ten khach');
  const conv = c[0].C;
  const tDetail = decode((await t.get('/Bookings/Detail/' + id)).html);
  check(tDetail.includes('/tin-nhan/dat-cho/' + id) && tDetail.includes('Nhắn tin với cơ sở'), 'Khach co nut nhan tin voi co so tren trang yeu cau');
  check((await t.get('/tin-nhan/dat-cho/' + id)).location === '/tin-nhan/' + conv, 'Nut nhan tin dua khach vao dung cuoc tro chuyen');
  check(!tDetail.includes('Khách đặt') && tDetail.includes('Ghi chú của khách'), 'Trang cua khach khong hien muc Khach dat, ghi chu co nhan');

  // 3. Nha cung cap thay ten khach va nhan tin duoc
  const pDetail = await p.get('/nha-cung-cap/yeu-cau/' + id), pHtml = decode(pDetail.html);
  check(pDetail.status === 200 && pHtml.includes('id="booking-customer"') && pHtml.includes(name), 'Nha cung cap thay ten khach dat');
  check(pHtml.includes('Nhắn tin với khách') && (await p.get('/tin-nhan/dat-cho/' + id)).location === '/tin-nhan/' + conv, 'Nha cung cap mo duoc tro chuyen voi khach tu trang yeu cau');
  check((await p.get('/tin-nhan/' + conv)).status === 200, 'Nha cung cap vao duoc cuoc tro chuyen do');
  // yeu cau cu chua co the: nha cung cap bam nhan tin thi the duoc tao lai
  sql(database, `DELETE FROM dbo.TinNhanChat WHERE Id=${c[0].Id};`);
  check((await p.get('/tin-nhan/dat-cho/' + id)).location === '/tin-nhan/' + conv, 'Yeu cau cu chua co tro chuyen: nha cung cap van mo duoc');
  c = cards(id);
  check(c.length === 1 && c[0].C === conv && c[0].S === tid, 'The dat cho duoc tao lai mot lan, van dung ten khach');
  await t.get('/tin-nhan/dat-cho/' + id);
  check(cards(id).length === 1, 'Mo lai nhieu lan khong sinh the trung');

  // 4. Danh sach cua nha cung cap: khong an lang le yeu cau o co so khac
  const all = decode((await p.get('/Bookings?loc=can-tra-loi')).html), mine = decode((await p.get('/Bookings')).html);
  check(all.includes('#' + id), 'Xem tat ca co so thay yeu cau vua gui');
  check(mine.includes('#' + id), 'Danh sach chung hien yeu cau cua moi co so, khong an theo co so dang chon');

  // 5. De xuat dieu kien khac: khach thay ro doi gi
  const propose = await p.post('/yeu-cau/chap-nhan', pDetail, { Id: id, Version: val(pDetail, 'Version'), BusinessVersion: val(pDetail, 'BusinessVersion'), Decision: 'DeXuatKhac', People: 4, Cost: 250000, Text: 'Chi con ban 4 nguoi, gia tron goi' });
  check(propose.status === 302 && status(id).S === 'DeXuatKhac', 'Nha cung cap gui de xuat dieu kien khac');
  const after = decode((await t.get('/Bookings/Detail/' + id)).html);
  const block = after.slice(after.indexOf('id="proposal-changes"'));
  check(after.includes('id="proposal-changes"') && /<s>3 người<\/s>\s*→\s*<strong>4 người<\/strong>/.test(block), 'Khach thay so nguoi doi tu 3 thanh 4');
  check(/Chi phí dự kiến:<\/b>\s*<s>Chưa có giá<\/s>/.test(block) && block.includes('250.000'), 'Khach thay chi phi doi tu chua co gia thanh 250.000');
  check(!block.slice(0, block.indexOf('</ul>')).includes('Ngày bắt đầu'), 'Dieu kien khong doi thi khong liet ke');

  // 6. Qua ngay su dung ma chua thong nhat: tu dong
  sql(database, `UPDATE dbo.YeuCauDatDichVu SET NgayBatDau=DATEADD(day,-1,CAST(DATEADD(hour,7,SYSUTCDATETIME()) AS date)) WHERE MaYeuCau=${id};`);
  let s = status(id);
  for (let n = 0; n < 45 && s.S !== 'KhongTiepTuc'; n++) { await wait(1000); s = status(id); }
  check(s.S === 'KhongTiepTuc' && (s.L || '').includes('Quá ngày sử dụng'), 'Yeu cau qua ngay tu chuyen sang Khong tiep tuc kem ly do');
  check(json(database, `SELECT COUNT(*) N FROM dbo.ThongBao WHERE MaNguoiNhan=${tid} AND DuongDan=N'/yeu-cau/${id}' AND NoiDung LIKE N'%tự đóng%' FOR JSON PATH;`)[0].N === 1, 'Khach nhan mot thong bao yeu cau tu dong');
  check(json(database, `SELECT COUNT(*) N FROM dbo.TinNhanChat WHERE BookingRequestId=${id} AND Kind='Event' AND Side='System' AND Body LIKE N'%tự đóng%' FOR JSON PATH;`)[0].N === 1, 'Cuoc tro chuyen co dong su kien tu dong');
  const closed = decode((await t.get('/Bookings/Detail/' + id)).html);
  check(!closed.includes('id="proposal-changes"') && !closed.includes('value="KhachDongY"'), 'Yeu cau da dong khong con nut dong y de xuat');
  check(json(database, `SELECT COUNT(*) N FROM dbo.YeuCauDatDichVu WHERE TrangThai IN ('ChoPhanHoi','DeXuatKhac') AND NgayBatDau<DATEADD(day,-1,CAST(GETDATE() AS date)) FOR JSON PATH;`)[0].N === 0, 'Khong con yeu cau cho nao qua ngay');
  console.log(`PASS ${results.length} booking-gaps checks.`);
})().catch(e => { console.error(e); process.exit(1); });
