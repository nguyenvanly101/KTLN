// Khuon dich vu thong nhat: danh muc loai, them loai moi khong sua ma, thuoc tinh theo loai, loai co han, loai khong dat duoc, du lieu cu.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/flow-v8.cjs
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
  const admin = new Client(), provider = new Client(), guest = new Client();
  await admin.login(user('admin').email, 'BranchTest@2026'); await provider.login(user('provider').email, 'BranchTest@2026');

  // 1. Danh muc + du lieu cu da thong nhat
  const list = await admin.get('/quan-tri/loai-dich-vu'); const lh = decode(list.html);
  check(list.status === 200 && ['Tham quan &amp; hoạt động', 'Ăn uống', 'Lưu trú', 'Di chuyển', 'Sự kiện', 'Cẩm nang', 'Hướng dẫn viên'].every(l => lh.includes(l) || lh.includes(l.replace('&amp;', '&'))), 'Danh muc co du loai: tham quan, an uong, luu tru, di chuyen, su kien, cam nang, huong dan vien');
  const legacy = json(database, "SELECT COUNT(*) N FROM dbo.DichVuDuLich WHERE LoaiDichVu NOT IN (SELECT Code FROM dbo.LoaiDichVu) FOR JSON PATH;")[0].N;
  check(legacy === 0, 'Du lieu dich vu cu da chuyen ve ma loai thong nhat');
  check((await guest.get('/quan-tri/loai-dich-vu')).status >= 300, 'Khach khong vao duoc quan ly loai dich vu');

  // 2. Them loai moi (khong sua ma nguon) -> hien trong form nha cung cap
  const code = 'Tx' + stamp; const label = 'Cho thuê xe ' + stamp;
  const ep = await admin.get('/quan-tri/loai-dich-vu/soan');
  const created = await admin.req('/quan-tri/loai-dich-vu/luu', form(val(ep, '__RequestVerificationToken'), { IsNew: 'true', Code: code, Label: label, Group: 'Move', Sort: 150, ProviderPostable: ['true', 'false'], Bookable: ['true', 'false'], ForBranch: 'false', HasPrice: ['true', 'false'], HasHours: 'false', HasLocation: ['true', 'false'], TimeBound: 'false', Active: ['true', 'false'], FieldsText: 'loaixe | Loại xe | select | Xe máy;Ô tô;Xe đạp | | | x\nsocho | Số chỗ | number | | 50' }));
  check(created.status === 302, 'Quan tri them loai dich vu moi');
  check(json(database, `SELECT COUNT(*) N FROM dbo.LoaiDichVu WHERE Code=N'${code}' FOR JSON PATH;`)[0].N === 1, 'Loai moi duoc luu trong danh muc');
  check((await admin.req('/quan-tri/loai-dich-vu/luu', form(val(ep, '__RequestVerificationToken'), { IsNew: 'true', Code: code, Label: 'Trung', Group: 'Move', FieldsText: 'a | A | text' }))).status === 409, 'Ma loai trung bi tu choi');
  check((await admin.req('/quan-tri/loai-dich-vu/luu', form(val(ep, '__RequestVerificationToken'), { IsNew: 'true', Code: 'Xx' + stamp, Label: 'Sai', Group: 'Move', FieldsText: 'khóa sai | A | text' }))).status === 422, 'Khoa truong sai dinh dang bi tu choi');
  check((await admin.req('/quan-tri/loai-dich-vu/luu', form(val(ep, '__RequestVerificationToken'), { IsNew: 'true', Code: 'Yy' + stamp, Label: 'Sai kieu', Group: 'Move', FieldsText: 'a | A | khongco' }))).status === 422, 'Kieu truong khong hop le bi tu choi');

  // 3. Nha cung cap dang dich vu thuoc loai moi voi thuoc tinh rieng
  const branch = json(database, `SELECT TOP(1) b.Id FROM dbo.CoSoDichVu b JOIN dbo.NhaCungCapDichVu p ON p.MaNhaCungCap=b.ProviderId JOIN dbo.TaiKhoan t ON t.MaTaiKhoan=p.MaTaiKhoan WHERE t.Email=N'${user('provider').email}' AND b.Status='Approved' AND b.PublishedVersion IS NOT NULL ORDER BY b.Id FOR JSON PATH;`)[0].Id;
  const pg = await provider.get('/Provider/Edit?coSo=' + branch); const ph = decode(pg.html);
  check(ph.includes(`value="${code}"`) && ph.includes(`name="attr.${code}.loaixe"`), 'Form nha cung cap co loai moi va truong rieng cua no');
  check(!ph.includes('value="CamNang"') && ph.includes('value="SuKien"'), 'Cam nang do quan tri soan (khong cho nha cung cap dang); Su kien thi duoc');
  const tk = val(pg, '__RequestVerificationToken'); const name = 'QA Thuê xe ' + stamp;
  const post = await provider.req('/Provider/Edit', form(tk, { Id: 0, Version: '0', Operation: 'LuuNhap', Name: name, Type: code, Region: 'Theo cơ sở áp dụng', Price: 150000, Description: 'Xe đời mới', BranchIds: [branch], [`attr.${code}.loaixe`]: 'Xe máy', [`attr.${code}.socho`]: '2' }));
  check(post.status === 302, 'Luu dich vu thuoc loai moi');
  const svc = json(database, `SELECT TOP(1) MaDichVu M, LoaiDichVu L, AttributesJson A FROM dbo.DichVuDuLich WHERE TenDichVu=N'${name}' FOR JSON PATH;`)[0];
  check(svc.L === code && JSON.parse(svc.A).loaixe === 'Xe máy' && JSON.parse(svc.A).socho === 2, 'Thuoc tinh rieng duoc luu dang JSON da kiem tra');
  const bad = await provider.req('/Provider/Edit', form(tk, { Id: 0, Version: '0', Operation: 'LuuNhap', Name: name + ' x', Type: code, Region: 'Theo cơ sở áp dụng', Price: 150000, BranchIds: [branch], [`attr.${code}.loaixe`]: 'Xe tăng' }));
  check(bad.status === 422, 'Gia tri ngoai danh sach lua chon bi tu choi');
  const refill = decode((await provider.get('/Provider/Edit/' + svc.M)).html);
  check(/<option value="Xe máy"[^>]*selected/.test(refill), 'Sua dich vu dien lai gia tri thuoc tinh rieng');

  // 4. Loai co han (su kien): bat buoc ngay, khong nhan ngay da qua
  const ev = n => ({ Id: 0, Version: '0', Operation: 'LuuNhap', Name: 'QA Sự kiện ' + stamp + n, Type: 'SuKien', Region: 'Theo cơ sở áp dụng', Price: 0, BranchIds: [branch] });
  check((await provider.req('/Provider/Edit', form(tk, ev('a')))).status === 422, 'Su kien thieu ngay bat dau bi tu choi');
  check((await provider.req('/Provider/Edit', form(tk, { ...ev('b'), 'attr.SuKien.ngaybatdau': day(-5) }))).status === 422, 'Su kien da dien ra bi tu choi');
  check((await provider.req('/Provider/Edit', form(tk, { ...ev('c'), 'attr.SuKien.ngaybatdau': day(10), 'attr.SuKien.ngayketthuc': day(5) }))).status === 422, 'Ngay ket thuc truoc ngay bat dau bi tu choi');
  const okEv = await provider.req('/Provider/Edit', form(tk, { ...ev('d'), 'attr.SuKien.ngaybatdau': day(10), 'attr.SuKien.ngayketthuc': day(11), 'attr.SuKien.diadiem': 'Quảng trường', 'attr.SuKien.vaocua': 'Miễn phí' }));
  check(okEv.status === 302, 'Dang su kien hop le (co ngay, dia diem, hinh thuc tham du)');

  // 5. Loai khong dat duoc: trang dich vu khong co nut dat
  const pub = json(database, "SELECT TOP(1) MaDichVu M FROM dbo.DichVuDuLich WHERE TrangThaiCongBo=N'DaDuyet' ORDER BY MaDichVu FOR JSON PATH;")[0].M;
  const was = json(database, `SELECT LoaiDichVu L FROM dbo.DichVuDuLich WHERE MaDichVu=${pub} FOR JSON PATH;`)[0].L;
  sql(database, `UPDATE dbo.DichVuDuLich SET LoaiDichVu=N'SuKien' WHERE MaDichVu=${pub}`);
  const traveler = new Client(); await traveler.login(user('traveler').email, 'BranchTest@2026');
  const dp = decode((await traveler.get('/Explore/Detail/' + pub)).html);
  check(dp.includes('chỉ cung cấp thông tin') && !dp.includes('data-service-chat'), 'Loai khong dat duoc: trang khong co nut dat, chi thong tin lien he');
  sql(database, `UPDATE dbo.DichVuDuLich SET LoaiDichVu=N'${was}' WHERE MaDichVu=${pub}`);

  // 6. Loai co so moi dung duoc; loai khong phai co so bi tu choi
  const pb = await provider.get('/nha-cung-cap/co-so/them'); const bh = decode(pb.html);
  check(bh.includes('value="GiaiTri"') && bh.includes('name="attr.GiaiTri.loai"') && !bh.includes('value="SuKien"'), 'Form co so: co loai moi (Vui choi giai tri), khong co loai chi danh cho dich vu');

  // 7. Don dep: go dich vu va loai thu
  await provider.req('/Provider/Delete', form(val(await provider.get('/Provider'), '__RequestVerificationToken'), { id: svc.M }));
  check((await admin.req('/quan-tri/loai-dich-vu/xoa', form(val(list, '__RequestVerificationToken'), { code: 'HoatDong' }))).status === 422, 'Khong xoa duoc loai co san cua he thong');
  const del = await admin.req('/quan-tri/loai-dich-vu/xoa', form(val(list, '__RequestVerificationToken'), { code }));
  check(del.status === 302 && json(database, `SELECT COUNT(*) N FROM dbo.LoaiDichVu WHERE Code=N'${code}' FOR JSON PATH;`)[0].N === 0, 'Xoa duoc loai tu tao khi khong con dich vu dung');

  // 8. Kham pha loc theo nhom loai dich vu
  const gEat = await guest.get('/Discovery?Group=Eat'); const gh = decode(gEat.html);
  check(gEat.status === 200 && gh.includes('ds-groups') && /class="active"[^>]*href="\/Discovery\?Group=Eat/.test(gh), 'Kham pha co hang chon nhom loai (Tham quan, An uong, Luu tru, Di chuyen...) va nhom dang chon duoc danh dau');
  check((await guest.get('/Discovery?Group=Khong')).status === 400, 'Nhom loai khong hop le bi tu choi');
  const gAll = await guest.get('/Discovery?Group=Stay'); check(gAll.status === 200, 'Loc theo nhom Luu tru tra ve ket qua');
  console.log(`PASS ${results.length} flow-v8 checks.`);
})().catch(e => { console.error(e); process.exit(1); });
