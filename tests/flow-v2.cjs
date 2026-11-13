// Luong moi: dang ky nha cung cap don gian -> tao co so (duyet = xac minh) -> dich vu CRUD; bang tin cho nha cung cap; admin quan ly khach.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5226 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/flow-v2.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, base, decode } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const PNG = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==', 'base64');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
const user = label => fixture.users.find(x => x.label === label);
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const stamp = Date.now().toString().slice(-8);
const geoUnit = json(database, "SELECT TOP(1) Id FROM dbo.DonViHanhChinh WHERE Regime='Cu' AND [Level]=3 ORDER BY Id FOR JSON PATH;")[0].Id;
const form = (token, values) => { const f = new FormData(); f.append('__RequestVerificationToken', token); for (const [k, v] of Object.entries(values)) { if (v == null) continue; for (const item of Array.isArray(v) ? v : [v]) f.append(k, String(item)); } return f; };
const cookie = c => [...c.cookies].map(([k, v]) => k + '=' + v).join('; ');
async function api(client, token, method, path, f) {
  const r = await fetch(base + path, { method, headers: { cookie: cookie(client), ...(method !== 'GET' ? { RequestVerificationToken: token } : {}) }, body: f, redirect: 'manual' });
  const type = r.headers.get('content-type') || ''; let body = null; if (type.includes('json')) body = await r.json(); else await r.arrayBuffer();
  return { status: r.status, body };
}
const fd = values => { const f = new FormData(); for (const [k, v] of Object.entries(values)) if (v != null) f.append(k, String(v)); return f; };

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { } if (n > 45) throw Error('not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const guest = new Client(), traveler = new Client(), admin = new Client(), prov = new Client();
  await traveler.login(user('traveler').email, 'BranchTest@2026'); await admin.login(user('admin').email, 'BranchTest@2026');

  // 1. Menu
  const t1 = decode((await traveler.get('/Plans')).html);
  check(/class="icon-button chat-link[^"]*" href="\/tin-nhan"/.test(t1), 'Khach: Tin nhan nam canh thong bao o thanh tren');
  const side = (t1.match(/<nav[^>]*class="sidebar-nav"[\s\S]*?<\/nav>/) || [''])[0];
  check(side.includes('Khám phá') && side.includes('Bảng tin') && side.includes('Chuyến đi') && !side.includes('Tin nhắn') && !side.includes('Hỗ trợ chuyến đi'), 'Khach: menu ben gon (Kham pha, Bang tin, Chuyen di), Tin nhan khong nam trong menu ben');

  // 2. Dang ky nha cung cap don gian
  const email = `ncc-${stamp}@example.com`;
  const reg = await guest.get('/Account/Register?accountType=provider');
  const r = await guest.req('/Account/Register', form(val(reg, '__RequestVerificationToken'), { Role: 'NHA_CUNG_CAP', Name: 'Quán Mới ' + stamp, Email: email, Phone: '09' + stamp, Password: 'HanhTrinh@2026', ConfirmPassword: 'HanhTrinh@2026' }));
  check(r.status === 302, 'Dang ky nha cung cap chi can ten, email, so dien thoai, mat khau (khong giay to)');
  const acc = json(database, `SELECT p.TrangThaiXacMinh S FROM dbo.NhaCungCapDichVu p JOIN dbo.TaiKhoan t ON t.MaTaiKhoan=p.MaTaiKhoan WHERE t.Email=N'${email}' FOR JSON PATH;`)[0];
  check(acc && acc.S === 'ChoXacMinh', 'Nha cung cap moi o trang thai cho xac minh');
  const login = await prov.req('/Account/Login', form(val(await prov.get('/Account/Login'), '__RequestVerificationToken'), { Identity: email, Email: email, Password: 'HanhTrinh@2026' }));
  check(login.status === 302 && login.location === '/nha-cung-cap/co-so/them', 'Dang nhap lan dau dan thang toi form co so dau tien (' + login.location + ')');

  // 3. Tao co so khi chua xac minh, admin duyet = xac minh
  const page = await prov.get('/nha-cung-cap/co-so/them');
  const tk = val(page, '__RequestVerificationToken'); const name = 'QA Quán Mới ' + stamp;
  const savedForm = form(tk, { Id: 0, Version: '0', Operation: 'Submit', Name: name, Address: 'Địa chỉ thử ' + stamp, DonViId: geoUnit, Type: 'AnUong', Summary: 'Quán thử nghiệm luồng mới: đăng ký xong tạo cơ sở, duyệt cơ sở cũng là xác minh nhà cung cấp.', OpenDayList: ['1', '2', '3', '4', '5', '6', '7'], OpenTime: '07:00', CloseTime: '21:00', PriceFrom: 30000, PriceUnit: 'người', Phone: '09' + stamp, LocationSource: 'Provider', Latitude: 16.071, Longitude: 108.212, ConfirmLocation: 'true', OwnershipRole: 'Chủ cơ sở' });
  savedForm.append('Documents', new Blob([PNG], { type: 'image/png' }), 'giay-phep-'+stamp+'.png');
  const saved = await prov.req('/nha-cung-cap/co-so/luu', savedForm);
  check(saved.status === 302, 'Nha cung cap chua xac minh van gui duoc co so dau tien');
  const branch = json(database, `SELECT TOP(1) Id FROM dbo.CoSoDichVu WHERE Name=N'${name}' ORDER BY Id DESC FOR JSON PATH;`)[0].Id;
  const rev = json(database, `SELECT TOP(1) Id FROM dbo.BanCapNhatCoSo WHERE BranchId=${branch} ORDER BY Number DESC FOR JSON PATH;`)[0].Id;
  const review = await admin.get('/quan-tri/co-so/' + rev);
  check(decode(review.html).includes('giay-phep-' + stamp + '.png'), 'Quan tri vien thay tai lieu xac minh dinh kem khi duyet co so');
  const dec = await admin.post('/quan-tri/co-so/duyet', review, { Id: rev, Version: val(review, 'Version'), Decision: 'Approved' });
  if (dec.status !== 302) console.log('DECIDE', dec.status, decode(dec.html).replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').slice(300, 900));
  const after = json(database, `SELECT b.Status B,p.TrangThaiXacMinh V,p.TrangThaiQuyenHoatDong Q FROM dbo.CoSoDichVu b JOIN dbo.NhaCungCapDichVu p ON p.MaNhaCungCap=b.ProviderId WHERE b.Id=${branch} FOR JSON PATH;`)[0];
  console.log(JSON.stringify(after));
  check(after.B === 'Approved' && after.V === 'DaXacMinh' && after.Q === 'DangHoatDong', 'Duyet co so dau tien = xac minh nha cung cap luon');

  // 4. Dich vu CRUD
  const sp = await prov.get('/Provider/Edit?coSo=' + branch); check(sp.status === 200, 'Mo form dich vu moi');
  const svcName = 'QA Dịch vụ ' + stamp;
  const draft = await prov.req('/Provider/Edit', form(val(sp, '__RequestVerificationToken'), { Id: 0, Version: '0', Operation: 'LuuNhap', Name: svcName, Type: 'AnUong', Region: 'Theo cơ sở áp dụng', Price: 50000, Description: 'Mô tả thử', BranchIds: [branch] }));
  if (draft.status !== 302) console.log('DRAFT', draft.status, (decode(draft.html).match(/validation-summary-errors[\s\S]{0,300}/) || [''])[0].replace(/<[^>]+>/g, ' '));
  check(draft.status === 302, 'Tao dich vu (luu nhap)');
  const svc = json(database, `SELECT TOP(1) MaDichVu M FROM dbo.DichVuDuLich WHERE TenDichVu=N'${svcName}' FOR JSON PATH;`)[0].M;
  sql(database, `DELETE FROM dbo.DichVuCoSo WHERE ServiceId=${svc}`);
  const unl = await prov.get('/Provider'); check(!decode(unl.html).includes(svcName) && decode(unl.html).includes('/Provider/AttachUnlinked'), 'Dich vu chua gan co so khong hien, co nut gan vao co so dang chon');
  await prov.req('/Provider/AttachUnlinked', form(val(unl, '__RequestVerificationToken'), {}));
  const list = await prov.get('/Provider'); check(decode(list.html).includes(svcName) && decode(list.html).includes('/Provider/Delete'), 'Danh sach dich vu co nut Go / xoa');
  const del = await prov.req('/Provider/Delete', form(val(list, '__RequestVerificationToken'), { id: svc }));
  check(del.status === 302 && json(database, `SELECT COUNT(*) N FROM dbo.DichVuDuLich WHERE MaDichVu=${svc} FOR JSON PATH;`)[0].N === 0, 'Xoa dich vu nhap: bien mat han');

  // 5. Bang tin cho nha cung cap
  const feed = await prov.get('/bang-tin'); check(feed.status === 200 && decode(feed.html).includes('data-role="provider"'), 'Nha cung cap vao duoc Bang tin');
  const ptk = val(feed, '__RequestVerificationToken'); const text = 'Khai trương ' + stamp;
  const bad = await api(prov, ptk, 'POST', '/api/mxh/bai-viet', fd({ Content: text, Kind: 'Share', Visibility: 'Public' }));
  check(bad.status === 400, 'Nha cung cap khong dang bai chia se thuong');
  const ok = await api(prov, ptk, 'POST', '/api/mxh/bai-viet', fd({ Content: text, Kind: 'Branch', BranchId: branch, Visibility: 'Public' }));
  check(ok.status === 200 && ok.body.kind === 'Branch' && ok.body.author === name, 'Nha cung cap dang tin gioi thieu co so ngay tren Bang tin');
  const ttk = val(await traveler.get('/bang-tin'), '__RequestVerificationToken');
  const pub = await api(traveler, ttk, 'GET', '/api/mxh/bai-viet?scope=public');
  check(pub.body.some(p => p.id === ok.body.id), 'Khach thay tin cua nha cung cap tren Bang tin cong dong');
  const cm = await api(prov, ptk, 'POST', `/api/mxh/bai-viet/${ok.body.id}/binh-luan`, fd({ Content: 'Cảm ơn bạn!' }));
  check(cm.status === 200, 'Nha cung cap tra loi binh luan duoi tin cua minh');

  // 6. Admin quan ly khach du lich
  const list2 = await admin.get('/quan-tri/khach-du-lich?q=' + encodeURIComponent(user('traveler').email));
  check(list2.status === 200 && decode(list2.html).includes(user('traveler').email), 'Admin tim thay tai khoan khach du lich');
  const tid = json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}' FOR JSON PATH;`)[0].Id;
  const lock = await admin.req('/quan-tri/khach-du-lich/trang-thai', form(val(list2, '__RequestVerificationToken'), { id: tid, active: 'false', reason: 'Thử khóa ' + stamp }));
  check(lock.status === 302 && json(database, `SELECT TrangThai S FROM dbo.TaiKhoan WHERE MaTaiKhoan=${tid} FOR JSON PATH;`)[0].S === 'DaNgung', 'Admin khoa tai khoan khach');
  const noReason = await admin.req('/quan-tri/khach-du-lich/trang-thai', form(val(list2, '__RequestVerificationToken'), { id: tid, active: 'false', reason: '' }));
  check(noReason.status === 422 || noReason.status === 400, 'Khoa phai co ly do');
  const unlock = await admin.req('/quan-tri/khach-du-lich/trang-thai', form(val(list2, '__RequestVerificationToken'), { id: tid, active: 'true' }));
  check(unlock.status === 302 && json(database, `SELECT TrangThai S FROM dbo.TaiKhoan WHERE MaTaiKhoan=${tid} FOR JSON PATH;`)[0].S === 'HoatDong', 'Admin mo khoa lai');
  check((await traveler.get('/quan-tri/khach-du-lich')).status >= 300, 'Khach khong vao duoc trang quan tri khach');

  console.log(`PASS ${results.length} flow-v2 checks.`);
})().catch(e => { console.error(e); process.exit(1); });
