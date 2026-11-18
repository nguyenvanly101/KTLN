// Dat dich vu gon: dat bang bang noi tren trang dich vu (vao thang chat), mot trang cho moi yeu cau, huy kem ly do,
// sua sau khi xac nhan thi quay lai cho phan hoi, co so ghi nhan "Da phuc vu" / "Khach khong den", danh sach loc theo viec, trang cu chuyen huong.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/booking-simple.cjs
const fs = require('node:fs'), assert = require('node:assert/strict'), crypto = require('node:crypto');
const { Client, val, base, decode } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
const user = label => fixture.users.find(x => x.label === label);
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const day = n => new Date(Date.now() + n * 864e5 + 7 * 36e5).toISOString().slice(0, 10);
const cookie = c => [...c.cookies].map(([k, v]) => k + '=' + v).join('; ');
const state = id => json(database, `SELECT TrangThai S, SoNguoi P, LyDo L FROM dbo.YeuCauDatDichVu WHERE MaYeuCau=${id} FOR JSON PATH;`)[0];

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { } if (n > 60) throw Error('not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const t = new Client(), p = new Client(), guest = new Client();
  await t.login(user('traveler').email, 'BranchTest@2026'); await p.login(user('provider').email, 'BranchTest@2026');
  const svc = json(database, `SELECT TOP 1 s.MaDichVu Id, x.BranchId FROM dbo.DichVuDuLich s JOIN dbo.DichVuCoSo x ON x.ServiceId=s.MaDichVu JOIN dbo.CoSoDichVu b ON b.Id=x.BranchId
    WHERE s.MaNhaCungCap=${user('provider').providerId} AND s.TrangThaiCongBo='DaDuyet' AND s.TrangThaiTiepNhan='TiepNhan' AND b.Status='Approved' AND b.PublishedVersion IS NOT NULL ORDER BY s.MaDichVu DESC FOR JSON PATH;`)[0];
  check(!!svc, 'Co dich vu thu gan voi co so cong khai');
  { const pg = await p.get('/bang-tin'); await p.post('/nha-cung-cap/co-so/chon', pg, { id: svc.BranchId, returnUrl: '/Bookings' }); } // nha cung cap chi thay co so dang chon

  // 1. Mot loi dat tren trang dich vu
  const page = decode((await t.get('/Explore/Detail/' + svc.Id)).html);
  check(page.includes('data-book-open') && page.includes('id="book-sheet"') && page.includes('booking-sheet.js') && !page.includes('Hoặc gửi yêu cầu theo mẫu'), 'Trang dich vu co mot nut Dat dich vu mo bang noi, bo loi gui theo mau');
  check(decode((await guest.get('/Explore/Detail/' + svc.Id)).html).includes('/Account/Login?ReturnUrl='), 'Khach chua dang nhap duoc dua toi dang nhap khi bam Dat dich vu');
  const old = await t.get('/Bookings/Create/' + svc.Id);
  check(old.status === 302 && /\/Explore\/Detail\/\d+\?.*#dat$/.test(old.location), 'Bieu mau 2 buoc cu chuyen ve bang dat tren trang dich vu');

  const token = val(page, '__RequestVerificationToken');
  const book = async (start, people, notes) => {
    const q = new URLSearchParams({ BranchId: svc.BranchId, ServiceId: svc.Id, Start: start, People: people, Quantity: 1 });
    const chk = await (await fetch(base + '/chat/dat/kiem-tra?' + q, { headers: { cookie: cookie(t) } })).json();
    const f = new FormData(); for (const [k, v] of Object.entries({ BranchId: svc.BranchId, ServiceId: svc.Id, Start: start, People: people, Quantity: 1, Notes: notes, SubmissionId: crypto.randomUUID() })) f.append(k, String(v));
    const r = await fetch(base + '/chat/dat', { method: 'POST', body: f, headers: { cookie: cookie(t), RequestVerificationToken: token } });
    const j = await r.json(); return { chk, status: r.status, cid: j.conversationId, id: j.message && j.message.booking };
  };
  const respond = async (who, id, data) => { const d = await who.get('/Bookings/Detail/' + id); return who.post('/Bookings/Respond', d, { Id: id, Version: val(d, 'Version'), BusinessVersion: val(d, 'BusinessVersion'), ...data }); };

  const a = await book(day(8), 3, 'Ban gan cua so');
  check(a.chk.ok === true && a.chk.estimate != null, 'Bang dat tam tinh gia truoc khi gui');
  check(a.status === 200 && a.cid > 0 && a.id > 0 && state(a.id).S === 'ChoPhanHoi', 'Gui tu bang dat: yeu cau vao cuoc tro chuyen, cho phan hoi');

  // 2. Danh sach loc theo viec, nha cung cap xac nhan ngay tren the
  const plist = decode((await p.get('/Bookings?loc=can-tra-loi')).html);
  check(plist.includes('#' + a.id) && plist.includes('class="bk-chips"') && plist.includes('value="DaXacNhan"'), 'Nha cung cap thay yeu cau o "Can tra loi", co nut Xac nhan tren the');
  check((await p.get('/nha-cung-cap/yeu-cau')).location === '/Bookings', 'Duong dan danh sach cu cua nha cung cap chuyen ve danh sach chung');
  check((await respond(p, a.id, { Decision: 'DaXacNhan' })).status === 302 && state(a.id).S === 'DaXacNhan', 'Nha cung cap xac nhan');

  // 3. Mot trang cho moi yeu cau
  const td = decode((await t.get('/Bookings/Detail/' + a.id)).html);
  check(td.includes('class="bk-steps"') && td.includes('Đã xác nhận') && td.includes('Hủy đặt dịch vụ') && td.includes('Đổi ngày hoặc số người') && !td.includes('booking-tabs'), 'Trang yeu cau: tien trinh, nut Huy va Doi ngay tren mot man, khong con tab');
  const cards = await (await fetch(base + '/chat/' + a.cid + '/the', { headers: { cookie: cookie(t) } })).json();
  check(cards.find(c => c.id === a.id).actions.includes('Huy'), 'The trong chat co nut Huy cho yeu cau da xac nhan');

  // 4. Sua sau khi xac nhan -> quay lai cho phan hoi
  const ed = await t.get('/Bookings/Detail/' + a.id);
  const edited = await t.post('/Bookings/Edit', ed, { Id: a.id, ServiceId: svc.Id, Version: val(ed, 'Version'), Start: day(9), People: 4, Quantity: 1, Notes: 'Doi sang 4 nguoi' });
  check(edited.status === 302 && state(a.id).S === 'ChoPhanHoi' && state(a.id).P === 4, 'Khach doi so nguoi sau khi xac nhan: yeu cau quay lai cho phan hoi');
  check(json(database, `SELECT COUNT(*) N FROM dbo.ThongBao WHERE NoiDung LIKE N'%cần xác nhận lại%' AND DuongDan LIKE N'%${a.id}' FOR JSON PATH;`)[0].N >= 1, 'Co so nhan thong bao can xac nhan lai');
  await respond(p, a.id, { Decision: 'DaXacNhan' });

  // 5. Huy kem ly do, khong can ben kia duyet
  check((await respond(t, a.id, { Decision: 'Huy', Reason: '' })).status !== 302 && state(a.id).S === 'DaXacNhan', 'Huy khong ghi ly do bi chan');
  check((await respond(t, a.id, { Decision: 'Huy', Reason: 'Doi lich bay' })).status === 302 && state(a.id).S === 'DaHuy' && state(a.id).L.startsWith('Khách hủy: Doi lich bay'), 'Khach huy kem ly do, co hieu luc ngay');
  check(decode((await t.get('/Bookings?loc=da-huy')).html).includes('#' + a.id), 'Yeu cau da huy nam o muc "Da huy"');

  // 6. Sau ngay dung: co so ghi nhan Da phuc vu / Khach khong den
  const b = await book(day(5), 2, 'Thu ghi nhan'); await respond(p, b.id, { Decision: 'DaXacNhan' });
  const c = await book(day(5), 2, 'Thu khong den'); await respond(p, c.id, { Decision: 'DaXacNhan' });
  sql(database, `UPDATE dbo.YeuCauDatDichVu SET NgayBatDau=DATEADD(day,-1,CAST(DATEADD(hour,7,SYSUTCDATETIME()) AS date)) WHERE MaYeuCau IN (${b.id},${c.id});`);
  check((await respond(t, b.id, { Decision: 'Huy', Reason: 'Muon' })).status !== 302 && state(b.id).S === 'DaXacNhan', 'Qua ngay dung thi khong huy duoc nua');
  check(decode((await p.get('/Bookings/Detail/' + b.id)).html).includes('value="DaPhucVu"'), 'Sau ngay dung, co so thay nut Da phuc vu');
  check((await respond(p, b.id, { Decision: 'DaPhucVu' })).status === 302 && state(b.id).S === 'HoanThanh', 'Co so ghi nhan da phuc vu');
  check((await t.get('/Reviews/Write?bookingId=' + b.id)).status === 200, 'Khach danh gia duoc ngay sau khi co so ghi nhan, khong can ket qua tung hang muc');
  check(decode((await t.get('/Bookings?loc=da-dung')).html).includes('#' + b.id), 'Yeu cau da phuc vu nam o muc "Da dung"');
  check((await respond(p, c.id, { Decision: 'KhachKhongDen' })).status === 302 && state(c.id).S === 'KhongThucHien', 'Co so ghi nhan khach khong den');
  check(decode((await t.get('/Bookings/Detail/' + c.id)).html).includes('Khách không đến') && (await t.get('/Reviews/Write?bookingId=' + c.id)).status !== 200, 'Khach khong den: trang hien ro, khong mo danh gia');

  // 7. Trang cu chuyen ve trang yeu cau
  check((await t.get('/Bookings/Changes/' + b.id)).location === '/Bookings/Detail/' + b.id, 'Trang Thay doi / huy cu chuyen ve trang yeu cau');
  check((await p.get('/Bookings/Outcome/' + b.id)).location === '/Bookings/Detail/' + b.id, 'Trang Ket qua phuc vu cu chuyen ve trang yeu cau');
  check((await t.get('/Bookings/Adjustments')).location === '/Bookings' && (await t.get('/Bookings/Results')).location === '/Bookings', 'Danh sach Thay doi / Ket qua cu chuyen ve danh sach chung');
  check((await t.get('/yeu-cau/' + b.id + '/ket-qua/xem-ket-qua')).location === '/Bookings/Detail/' + b.id, 'Duong dan ket qua dang tieng Viet cu cung chuyen huong');
  console.log(`PASS ${results.length} booking-simple checks.`);
})().catch(e => { console.error(e); process.exit(1); });
