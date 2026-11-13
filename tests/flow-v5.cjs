// Chuyen di chung voi ban be: moi (chi ban be), chap nhan/tu choi, thanh vien xem + sua hang muc, gioi han quyen, roi/go.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/flow-v5.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, base, decode } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
const user = label => fixture.users.find(x => x.label === label);
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const form = (token, values) => { const f = new FormData(); f.append('__RequestVerificationToken', token); for (const [k, v] of Object.entries(values)) { if (v == null) continue; for (const item of Array.isArray(v) ? v : [v]) f.append(k, String(item)); } return f; };
const stamp = Date.now().toString().slice(-8);
const day = n => new Date(Date.now() + n * 864e5).toISOString().slice(0, 10);

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { } if (n > 45) throw Error('not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const A = json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}' FOR JSON PATH;`)[0].Id;
  // Hai tai khoan phu: B la ban be cua A, D khong phai ban be
  const mk = (tag) => { const email = `trip-${tag}-${stamp}@example.com`; sql(database, `INSERT dbo.TaiKhoan(Email,SoDienThoai,MatKhauBam,VaiTro,TrangThai,DauPhien,PhaiDoiMatKhau,ThoiDiemTao) SELECT N'${email}',NULL,MatKhauBam,VaiTro,TrangThai,NEWID(),0,SYSUTCDATETIME() FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}'; INSERT dbo.HoSoNguoiDung(MaTaiKhoan,TenHienThi,ThongTinChiaSe,ThoiDiemCapNhat) SELECT MaTaiKhoan,N'Bạn ${tag} ${stamp}','CongKhai',SYSUTCDATETIME() FROM dbo.TaiKhoan WHERE Email=N'${email}';`); return { email, id: json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${email}' FOR JSON PATH;`)[0].Id }; };
  const B = mk('B'), D = mk('D');
  sql(database, `INSERT dbo.KetBan(RequesterId,AddresseeId,Status,RespondedAt) VALUES(${A},${B.id},'Accepted',SYSUTCDATETIME());`);
  const owner = new Client(), friend = new Client(), stranger = new Client();
  await owner.login(user('traveler').email, 'BranchTest@2026'); await friend.login(B.email, 'BranchTest@2026'); await stranger.login(D.email, 'BranchTest@2026');

  // Chuyen di cua A
  const pe = await owner.get('/Plans/Edit');
  const created = await owner.req('/Plans/Edit', form(val(pe, '__RequestVerificationToken'), { Id: 0, Version: '0', Name: 'Chung ' + stamp, Destination: 'Đà Nẵng', Start: day(20), End: day(23) }));
  const plan = json(database, `SELECT TOP(1) MaKeHoach Id FROM dbo.KeHoachChuyenDi WHERE TenKeHoach=N'Chung ${stamp}' ORDER BY MaKeHoach DESC FOR JSON PATH;`)[0];
  check(created.status === 302 && plan, 'Chu tao chuyen di');

  // Moi: chi ban be
  const det = await owner.get('/Plans/Detail/' + plan.Id); const dh = decode(det.html);
  check(dh.includes('trip-members') && dh.includes(`Bạn B ${stamp}`) && !dh.includes(`Bạn D ${stamp}`), 'Danh sach moi chi gom ban be');
  const tk = val(det, '__RequestVerificationToken');
  check((await owner.req('/Plans/Invite', form(tk, { planId: plan.Id, accountId: D.id }))).status === 403, 'Khong moi duoc nguoi khong phai ban be');
  check((await owner.req('/Plans/Invite', form(tk, { planId: plan.Id, accountId: B.id }))).status === 302, 'Moi ban be');
  check((await owner.req('/Plans/Invite', form(tk, { planId: plan.Id, accountId: B.id }))).status === 409, 'Khong moi trung');

  // Chua chap nhan thi chua xem duoc
  check((await friend.get('/Plans/Detail/' + plan.Id)).status === 404, 'Loi moi chua chap nhan: chua xem duoc chuyen di');
  const idx = await friend.get('/Plans'); const ih = decode(idx.html);
  check(ih.includes('Lời mời vào chuyến đi') && ih.includes('Chung ' + stamp), 'Ban be thay loi moi o trang Chuyen di');
  check(json(database, `SELECT COUNT(*) N FROM dbo.ThongBao WHERE MaNguoiNhan=${B.id} AND LoaiThongBao=N'MoiChuyenDi' FOR JSON PATH;`)[0].N >= 1, 'Ban be nhan thong bao loi moi');
  check((await stranger.get('/Plans/Detail/' + plan.Id)).status === 404, 'Nguoi la khong xem duoc');

  // Chap nhan
  const acc = await friend.req('/Plans/RespondInvite', form(val(idx, '__RequestVerificationToken'), { planId: plan.Id, accept: 'true' }));
  check(acc.status === 302, 'Chap nhan loi moi');
  const view = await friend.get('/Plans/Detail/' + plan.Id); const vh = decode(view.html);
  check(view.status === 200 && vh.includes('chuyến đi chung') && vh.includes('Rời chuyến đi'), 'Thanh vien xem duoc chuyen di chung');
  check(/hidden="[^"]*"[^>]*href="\/Plans\/Edit\/|class="quiet-link" hidden/.test(vh) || !vh.includes('href="/Plans/Edit/' + plan.Id + '"') || vh.includes('hidden'), 'Muc sua thong tin chuyen di bi an voi thanh vien');

  // Thanh vien them hang muc
  const ap = await friend.get('/Plans/Activity/' + plan.Id); check(ap.status === 200, 'Thanh vien mo form hang muc');
  const add = await friend.req('/Plans/Activity', form(val(ap, '__RequestVerificationToken'), { Id: 0, PlanId: plan.Id, Version: val(ap, 'Version'), UpdateTiming: 'true', Date: day(20), Time: '09:00', Content: 'Hạng mục của bạn ' + stamp, Cost: 100000 }));
  check(add.status === 302 && json(database, `SELECT COUNT(*) N FROM dbo.HoatDongLichTrinh WHERE MaKeHoach=${plan.Id} AND NoiDung=N'Hạng mục của bạn ${stamp}' FOR JSON PATH;`)[0].N === 1, 'Thanh vien them hang muc vao chuyen di chung');
  check(decode((await owner.get('/Plans/Detail/' + plan.Id)).html).includes('Hạng mục của bạn ' + stamp), 'Chu chuyen di thay hang muc do ban be them');

  // Gioi han quyen cua thanh vien
  check((await friend.get('/Plans/Edit/' + plan.Id)).status === 404, 'Thanh vien khong sua duoc thong tin chuyen di');
  check((await friend.req('/Plans/Invite', form(val(view, '__RequestVerificationToken'), { planId: plan.Id, accountId: D.id }))).status >= 400, 'Thanh vien khong moi them nguoi');
  check((await stranger.get('/Plans/Activity/' + plan.Id)).status === 404, 'Nguoi la khong mo duoc form hang muc');

  // Roi chuyen di
  const leave = await friend.req('/Plans/RemoveMember', form(val(view, '__RequestVerificationToken'), { planId: plan.Id, accountId: B.id }));
  check(leave.status === 302 && (await friend.get('/Plans/Detail/' + plan.Id)).status === 404, 'Thanh vien roi chuyen di: mat quyen truy cap');

  // Tu choi
  await owner.req('/Plans/Invite', form(tk, { planId: plan.Id, accountId: B.id }));
  const idx2 = await friend.get('/Plans');
  await friend.req('/Plans/RespondInvite', form(val(idx2, '__RequestVerificationToken'), { planId: plan.Id, accept: 'false' }));
  check(json(database, `SELECT COUNT(*) N FROM dbo.ThanhVienChuyenDi WHERE PlanId=${plan.Id} AND AccountId=${B.id} FOR JSON PATH;`)[0].N === 0, 'Tu choi loi moi: xoa loi moi');

  console.log(`PASS ${results.length} flow-v5 checks.`);
})().catch(e => { console.error(e); process.exit(1); });
