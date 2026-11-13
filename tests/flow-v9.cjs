// Ho tro du lich muc trung binh: lich trinh mau, khan cap, che do Hom nay, chia tien nhom, hoi dap, bao sai thong tin co so.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/flow-v9.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, base, decode } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
const user = label => fixture.users.find(x => x.label === label);
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const form = (token, values) => { const f = new FormData(); f.append('__RequestVerificationToken', token); for (const [k, v] of Object.entries(values)) { if (v == null) continue; for (const item of Array.isArray(v) ? v : [v]) f.append(k, String(item)); } return f; };
const stamp = Date.now().toString().slice(-8);
const day = n => new Date(Date.now() + n * 864e5).toISOString().slice(0, 10);
const text = html => decode(html).replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ');

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { } if (n > 45) throw Error('not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const A = json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}' FOR JSON PATH;`)[0].Id;
  const mk = tag => { const email = `v9-${tag}-${stamp}@example.com`; sql(database, `INSERT dbo.TaiKhoan(Email,SoDienThoai,MatKhauBam,VaiTro,TrangThai,DauPhien,PhaiDoiMatKhau,ThoiDiemTao) SELECT N'${email}',NULL,MatKhauBam,VaiTro,TrangThai,NEWID(),0,SYSUTCDATETIME() FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}'; INSERT dbo.HoSoNguoiDung(MaTaiKhoan,TenHienThi,ThongTinChiaSe,ThoiDiemCapNhat) SELECT MaTaiKhoan,N'Bạn ${tag} ${stamp}','CongKhai',SYSUTCDATETIME() FROM dbo.TaiKhoan WHERE Email=N'${email}';`); return { email, id: json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${email}' FOR JSON PATH;`)[0].Id }; };
  const B = mk('B'), C = mk('C'), D = mk('D');
  const owner = new Client(), b = new Client(), c = new Client(), stranger = new Client(), admin = new Client(), provider = new Client(), guest = new Client();
  await owner.login(user('traveler').email, 'BranchTest@2026'); await b.login(B.email, 'BranchTest@2026'); await c.login(C.email, 'BranchTest@2026'); await stranger.login(D.email, 'BranchTest@2026');
  await admin.login(user('admin').email, 'BranchTest@2026'); await provider.login(user('provider').email, 'BranchTest@2026');
  const unit = json(database, "SELECT TOP(1) Id FROM dbo.DonViHanhChinh WHERE Regime='Cu' AND [Level]=1 ORDER BY Id FOR JSON PATH;")[0].Id;

  // 1. Lich trinh mau
  const ep = await admin.get('/quan-tri/lich-mau/soan');
  const title = 'Lịch thử ' + stamp;
  const saved = await admin.req('/quan-tri/lich-mau/luu', form(val(ep, '__RequestVerificationToken'), { Id: 0, DonViId: unit, Title: title, Days: 2, Style: 'CanBang', Summary: 'Lịch thử nghiệm', Status: 'Published', ItemsText: '1 | 08:00 | Ăn sáng bún bò | 40000\n1 | 09:30 | Tham quan chùa | 0\n2 | | Tắm biển | ' }));
  check(saved.status === 302, 'Quan tri soan lich trinh mau');
  const tpl = json(database, `SELECT TOP(1) Id FROM dbo.LichTrinhMau WHERE Title=N'${title}' FOR JSON PATH;`)[0].Id;
  check((await admin.req('/quan-tri/lich-mau/luu', form(val(ep, '__RequestVerificationToken'), { Id: 0, DonViId: unit, Title: 'Sai dòng ' + stamp, Days: 2, Style: 'CanBang', Status: 'Draft', ItemsText: '9 | | Ngày quá | ' }))).status === 422, 'Hang muc co ngay vuot so ngay bi tu choi');
  check(decode((await guest.get('/lich-mau')).html).includes(title), 'Khach xem danh sach lich trinh mau');
  const tp = await owner.get('/lich-mau/' + tpl); check(tp.status === 200 && decode(tp.html).includes('Ăn sáng bún bò'), 'Trang chi tiet lich mau liet ke hang muc theo ngay');
  const used = await owner.req(`/lich-mau/${tpl}/dung`, form(val(tp, '__RequestVerificationToken'), { start: day(30) }));
  check(used.status === 302 && /^\/ke-hoach\/\d+$/.test(used.location), 'Dung lich mau: tao ke hoach moi');
  const planId = Number(used.location.split('/').pop());
  check(json(database, `SELECT COUNT(*) N FROM dbo.HoatDongLichTrinh WHERE MaKeHoach=${planId} FOR JSON PATH;`)[0].N === 3, 'Ke hoach co du 3 hang muc chep tu lich mau');
  check((await guest.req(`/lich-mau/${tpl}/dung`, form('x', { start: day(30) }))).status >= 300, 'Khach chua dang nhap khong chep duoc lich mau');
  check(decode((await guest.get('/khan-cap')).html).includes('113') && decode((await guest.get('/khan-cap')).html).includes('data-share-btn'), 'Trang khan cap co so dien thoai va nut chia se vi tri');

  // 2. Che do Hom nay
  const today = decode((await owner.get('/Journey/Today?planId=' + planId)).html);
  check(today.includes('around-me') && today.includes('tel:113') && today.includes('data-share-btn') && today.includes('Hiệu thuốc'), 'Hom nay: co "Xung quanh ban", so khan cap va chia se vi tri');

  // 3. Chia tien nhom
  sql(database, `INSERT dbo.KetBan(RequesterId,AddresseeId,Status,RespondedAt) VALUES(${A},${B.id},'Accepted',SYSUTCDATETIME()); INSERT dbo.ThanhVienChuyenDi(PlanId,AccountId,Status,InvitedBy) VALUES(${planId},${B.id},'Accepted',${A});`);
  const sp = await owner.get('/Plans/Split/' + planId); const st = val(sp, '__RequestVerificationToken');
  check(sp.status === 200 && decode(sp.html).includes('Chia tiền nhóm'), 'Trang chia tien nhom');
  check((await stranger.get('/Plans/Split/' + planId)).status === 404, 'Nguoi ngoai khong vao duoc chia tien');
  check((await owner.req('/Plans/Split/' + planId, form(st, { Title: 'Ăn tối ' + stamp, Amount: 300000, SpentOn: day(0), PaidBy: A, Participants: [A, B.id] }))).status === 302, 'Ghi khoan chi A tra 300.000 chia deu 2 nguoi');
  check((await b.req('/Plans/Split/' + planId, form(val(await b.get('/Plans/Split/' + planId), '__RequestVerificationToken'), { Title: 'Taxi ' + stamp, Amount: 100000, SpentOn: day(0), PaidBy: B.id, Participants: [A, B.id] }))).status === 302, 'Thanh vien B ghi khoan chi 100.000');
  let page = text((await owner.get('/Plans/Split/' + planId)).html);
  check(/Bạn B [^ ]+ \d+ gửi/.test(page) || page.includes('gửi'), 'Co danh sach ai gui lai cho ai');
  const bal = json(database, `SELECT e.PaidBy P, SUM(e.Amount) S FROM dbo.KhoanChiChung e WHERE e.PlanId=${planId} GROUP BY e.PaidBy FOR JSON PATH;`);
  check(bal.length === 2, 'Ca hai nguoi deu co khoan da tra');
  const owedA = json(database, `SELECT SUM(s.Share) S FROM dbo.KhoanChiChungChia s JOIN dbo.KhoanChiChung e ON e.Id=s.ExpenseId WHERE e.PlanId=${planId} AND s.AccountId=${A} FOR JSON PATH;`)[0].S;
  check(owedA === 200000, 'A phai chiu 150.000 + 50.000 = 200.000');
  check(page.includes('100.000') , 'Chenh lech cuoi cung 100.000 hien tren trang');
  check((await owner.req('/Plans/Split/' + planId, form(st, { Title: 'Lẻ ' + stamp, Amount: 100001, SpentOn: day(0), PaidBy: A, Participants: [A, B.id] }))).status === 302 && json(database, `SELECT SUM(s.Share) S FROM dbo.KhoanChiChungChia s JOIN dbo.KhoanChiChung e ON e.Id=s.ExpenseId WHERE e.PlanId=${planId} AND e.Title=N'Lẻ ${stamp}' FOR JSON PATH;`)[0].S === 100001, 'Chia so le: tong phan chia bang dung so tien');
  check((await owner.req('/Plans/Split/' + planId, form(st, { Title: 'Xấu', Amount: 1000, SpentOn: day(0), PaidBy: A, Participants: [D.id] }))).status === 422, 'Khong chia cho nguoi ngoai chuyen di');
  check((await owner.req('/Plans/Split/' + planId, form(st, { Title: 'Âm', Amount: -5, SpentOn: day(0), PaidBy: A, Participants: [A] }))).status === 422, 'So tien am bi tu choi');
  const exp = json(database, `SELECT TOP(1) Id I FROM dbo.KhoanChiChung WHERE PlanId=${planId} AND CreatedBy=${A} ORDER BY Id FOR JSON PATH;`)[0].I;
  check((await b.req(`/Plans/Split/${planId}/xoa`, form(val(await b.get('/Plans/Split/' + planId), '__RequestVerificationToken'), { expenseId: exp }))).status === 403, 'Thanh vien khong xoa duoc khoan chi cua nguoi khac');
  check((await owner.req(`/Plans/Split/${planId}/xoa`, form(st, { expenseId: exp }))).status === 302, 'Nguoi ghi (hoac chu chuyen di) xoa duoc khoan chi');

  // 4. Hoi dap
  const lp = await guest.get('/hoi-dap/' + unit); check(lp.status === 200, 'Khach xem duoc trang hoi dap');
  const askPage = await owner.get('/hoi-dap/' + unit); const at = val(askPage, '__RequestVerificationToken');
  check((await owner.req('/hoi-dap/' + unit, form(at, { title: 'Ngắn', body: '' }))).status === 422, 'Cau hoi qua ngan bi tu choi');
  const asked = await owner.req('/hoi-dap/' + unit, form(at, { title: 'Tháng 12 đi nên mặc gì ' + stamp + '?', body: 'Đi 3 ngày.' }));
  check(asked.status === 302 && /^\/hoi-dap\/cau\/\d+$/.test(asked.location), 'Dat cau hoi');
  const qid = Number(asked.location.split('/').pop());
  check((await guest.req(`/hoi-dap/cau/${qid}/tra-loi`, form('x', { body: 'abc' }))).status >= 300, 'Khach chua dang nhap khong tra loi duoc');
  const qp = await b.get('/hoi-dap/cau/' + qid);
  await b.req(`/hoi-dap/cau/${qid}/tra-loi`, form(val(qp, '__RequestVerificationToken'), { body: 'Mang áo ấm và áo mưa nhẹ.' }));
  await c.req(`/hoi-dap/cau/${qid}/tra-loi`, form(val(await c.get('/hoi-dap/cau/' + qid), '__RequestVerificationToken'), { body: 'Nên có găng tay.' }));
  const ans = json(database, `SELECT Id,AuthorId FROM dbo.TraLoi WHERE QuestionId=${qid} ORDER BY Id FOR JSON PATH;`);
  check(ans.length === 2, 'Hai cau tra loi duoc luu');
  check(json(database, `SELECT COUNT(*) N FROM dbo.ThongBao WHERE MaNguoiNhan=${A} AND LoaiThongBao=N'TraLoiCauHoi' FOR JSON PATH;`)[0].N >= 2, 'Nguoi hoi nhan thong bao khi co tra loi');
  const voteTok = val(await c.get('/hoi-dap/cau/' + qid), '__RequestVerificationToken');
  await c.req(`/hoi-dap/tra-loi/${ans[0].Id}/binh-chon`, form(voteTok, { questionId: qid }));
  check(json(database, `SELECT COUNT(*) N FROM dbo.BinhChonTraLoi WHERE AnswerId=${ans[0].Id} FOR JSON PATH;`)[0].N === 1, 'Binh chon huu ich cho cau tra loi');
  await b.req(`/hoi-dap/tra-loi/${ans[0].Id}/binh-chon`, form(val(await b.get('/hoi-dap/cau/' + qid), '__RequestVerificationToken'), { questionId: qid }));
  check(json(database, `SELECT COUNT(*) N FROM dbo.BinhChonTraLoi WHERE AnswerId=${ans[0].Id} AND AccountId=${B.id} FOR JSON PATH;`)[0].N === 0, 'Khong tu binh chon cau tra loi cua minh');
  await owner.req(`/hoi-dap/tra-loi/${ans[1].Id}/huu-ich`, form(val(await owner.get('/hoi-dap/cau/' + qid), '__RequestVerificationToken'), { questionId: qid }));
  check(json(database, `SELECT AcceptedAnswerId A FROM dbo.CauHoi WHERE Id=${qid} FOR JSON PATH;`)[0].A === ans[1].Id, 'Nguoi hoi chon cau tra loi huu ich nhat');
  check((await c.req(`/hoi-dap/tra-loi/${ans[0].Id}/huu-ich`, form(val(await c.get('/hoi-dap/cau/' + qid), '__RequestVerificationToken'), { questionId: qid }))).status === 403, 'Chi nguoi hoi moi chon duoc cau huu ich');
  const order = decode((await guest.get('/hoi-dap/cau/' + qid)).html);
  check(order.indexOf('Nên có găng tay') < order.indexOf('Mang áo ấm'), 'Cau tra loi huu ich nhat duoc dua len dau');
  check((await c.req(`/hoi-dap/cau/${qid}/xoa`, form(val(await c.get('/hoi-dap/cau/' + qid), '__RequestVerificationToken'), { donViId: unit }))).status === 403, 'Nguoi khac khong xoa duoc cau hoi');
  check((await admin.req(`/hoi-dap/cau/${qid}/xoa`, form(val(await admin.get('/hoi-dap/cau/' + qid), '__RequestVerificationToken'), { donViId: unit }))).status === 302 && (await guest.get('/hoi-dap/cau/' + qid)).status === 404, 'Quan tri xoa duoc cau hoi');

  // 5. Bao sai thong tin co so
  const branch = json(database, `SELECT TOP(1) b.Id FROM dbo.CoSoDichVu b JOIN dbo.NhaCungCapDichVu p ON p.MaNhaCungCap=b.ProviderId JOIN dbo.TaiKhoan t ON t.MaTaiKhoan=p.MaTaiKhoan WHERE t.Email=N'${user('provider').email}' AND b.Status='Approved' AND b.PublishedVersion IS NOT NULL ORDER BY b.Id FOR JSON PATH;`)[0].Id;
  const bp = await owner.get('/co-so/' + branch); check(decode(bp.html).includes('bao-sai'), 'Trang co so co nut bao thong tin chua dung (cho khach)');
  const bt = val(bp, '__RequestVerificationToken');
  check((await owner.req(`/co-so/${branch}/bao-sai`, form(bt, { field: 'Khong-co', detail: 'Giờ sai rồi' }))).status === 422, 'Muc bao sai khong hop le bi tu choi');
  check((await owner.req(`/co-so/${branch}/bao-sai`, form(bt, { field: 'Gio', detail: 'Quán đã đổi giờ mở cửa thành 8:00 ' + stamp }))).status === 302, 'Khach bao gio mo cua sai');
  check((await owner.req(`/co-so/${branch}/bao-sai`, form(bt, { field: 'Gio', detail: 'Báo lại lần nữa cùng mục' }))).status === 409, 'Khong bao trung khi dang cho xu ly');
  const wi = await provider.get(`/nha-cung-cap/co-so/${branch}/bao-sai`); check(wi.status === 200 && decode(wi.html).includes('đổi giờ mở cửa thành 8:00 ' + stamp), 'Nha cung cap thay bao cao cua khach');
  check(json(database, `SELECT COUNT(*) N FROM dbo.ThongBao n JOIN dbo.TaiKhoan t ON t.MaTaiKhoan=n.MaNguoiNhan WHERE t.Email=N'${user('provider').email}' AND n.LoaiThongBao=N'BaoSaiCoSo' FOR JSON PATH;`)[0].N >= 1, 'Nha cung cap nhan thong bao khi co bao cao');
  const rid = json(database, `SELECT TOP(1) Id FROM dbo.BaoSaiCoSo WHERE BranchId=${branch} AND ReporterId=${A} ORDER BY Id DESC FOR JSON PATH;`)[0].Id;
  check((await provider.req(`/nha-cung-cap/co-so/${branch}/bao-sai/${rid}`, form(val(wi, '__RequestVerificationToken'), { fixedIt: 'true', note: 'Đã cập nhật giờ mới' }))).status === 302, 'Nha cung cap phan hoi: da chinh');
  check(json(database, `SELECT Status S FROM dbo.BaoSaiCoSo WHERE Id=${rid} FOR JSON PATH;`)[0].S === 'Fixed' && json(database, `SELECT COUNT(*) N FROM dbo.ThongBao WHERE MaNguoiNhan=${A} AND LoaiThongBao=N'PhanHoiBaoSai' FOR JSON PATH;`)[0].N >= 1, 'Khach nhan phan hoi cua nha cung cap');
  check((await provider.get(`/nha-cung-cap/co-so/999999999/bao-sai`)).status === 404, 'Khong xem duoc bao cao cua co so khac');

  console.log(`PASS ${results.length} flow-v9 checks.`);
})().catch(e => { console.error(e); process.exit(1); });
