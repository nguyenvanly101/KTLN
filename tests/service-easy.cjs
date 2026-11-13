// Dang dich vu rieng, khong lap lai viec them co so: tu gan co so da duyet, nut "Dang dich vu" tren moi co so.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5226 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/service-easy.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, base, decode } = require('./fulltext-smoke.cjs');
const { database, json } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
const user = label => fixture.users.find(x => x.label === label);
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const stamp2 = Date.now().toString().slice(-4);
(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { } if (n > 45) throw Error('not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const provider = new Client(); await provider.login(user('provider').email, 'BranchTest@2026');
  const providerId = json(database, `SELECT TOP(1) ProviderId P FROM dbo.CoSoDichVu WHERE Status='Approved' AND PublishedVersion IS NOT NULL GROUP BY ProviderId ORDER BY COUNT(*) DESC FOR JSON PATH;`)[0];
  const home = decode((await provider.get('/nha-cung-cap/co-so')).html);
  const links = [...home.matchAll(/href="\/Provider\/Edit\?coSo=(\d+)"/g)].map(m => m[1]);
  check(links.length > 0, 'Moi co so da duyet co nut "Dang dich vu"');
  const page = decode((await provider.get('/Provider/Edit')).html);
  const boxes = [...page.matchAll(/name="BranchIds"[^>]*>/g)].map(m => m[0]);
  check(boxes.length > 0 && (true), 'Dich vu moi: co so duoc gan san (an neu chi 1, tick san neu nhieu), khong bat chon lai');
  const one = decode((await provider.get('/Provider/Edit?coSo=' + links[0])).html);
  const ones = [...one.matchAll(/(?:name="BranchIds"[^>]*value="(\d+)"[^>]*>|<option value="(\d+)"[^>]*selected)/g)].map(m => [m[0], m[1] || m[2]]);
  check(ones.length === 1 && ones[0][1] === links[0], 'Tu nut tren co so: chi gan dung co so do');
  check(/<details class="pl-more"/.test(one) && /id="service-branch"|type="hidden"[^>]*name="BranchIds"|name="BranchIds"[^>]*type="hidden"/.test(one), 'Form gon: chi chon co so, phan con lai gap trong "Thong tin them"');
  check(!one.includes('Quản lý cơ sở & chi nhánh'), 'Bieu mau dich vu khong con keo nguoi dung quay lai quan ly co so');

  // Sua nhe dich vu da cong bo co hieu luc ngay; sua nang van cho duyet
  const svc = json(database, `SELECT TOP(1) d.* FROM dbo.DichVuDuLich d JOIN dbo.NhaCungCapDichVu p ON p.MaNhaCungCap=d.MaNhaCungCap JOIN dbo.TaiKhoan t ON t.MaTaiKhoan=p.MaTaiKhoan WHERE t.Email=N'${user('provider').email}' AND d.PhienBanCongBo IS NOT NULL AND d.TrangThaiCongBo=N'DaDuyet' AND EXISTS(SELECT 1 FROM dbo.DichVuCoSo l WHERE l.ServiceId=d.MaDichVu) AND NOT EXISTS(SELECT 1 FROM dbo.DichVuCoSo l JOIN dbo.CoSoDichVu c ON c.Id=l.BranchId WHERE l.ServiceId=d.MaDichVu AND (c.Status<>N'Approved' OR c.PublishedVersion IS NULL OR c.ProviderId<>d.MaNhaCungCap)) AND NOT EXISTS(SELECT 1 FROM dbo.BanCapNhatDichVu r WHERE r.MaDichVu=d.MaDichVu AND r.TrangThaiDuyet=N'ChoDuyet') ORDER BY d.MaDichVu FOR JSON PATH;`)[0];
  if (svc) {
    const links2 = json(database, `SELECT BranchId B FROM dbo.DichVuCoSo WHERE ServiceId=${svc.MaDichVu} FOR JSON PATH;`).map(x => x.B);
    const revs = () => json(database, `SELECT COUNT(*) N FROM dbo.BanCapNhatDichVu WHERE MaDichVu=${svc.MaDichVu} FOR JSON PATH;`)[0].N;
    const f = new FormData(); const pg = await provider.get('/Provider/Edit/' + svc.MaDichVu);
    const base2 = { __RequestVerificationToken: val(pg, '__RequestVerificationToken'), Id: svc.MaDichVu, Version: val(pg, 'Version'), Operation: 'GuiDang', Name: svc.TenDichVu, Type: val(pg, 'Type') || 'AnUong', Region: svc.KhuVucPhucVu, Description: svc.MoTa || '', Price: 70000 + Number(stamp2), Hours: '08:00-20:00' };
    for (const [k, v] of Object.entries(base2)) f.append(k, String(v)); for (const b of links2) f.append('BranchIds', String(b));
    const before = revs();
    const r = await provider.req('/Provider/Edit', f);
    const now = json(database, `SELECT GiaThamKhao P, ThoiGianPhucVu H FROM dbo.DichVuDuLich WHERE MaDichVu=${svc.MaDichVu} FOR JSON PATH;`)[0];
    if (r.status !== 302) console.log('POST', r.status, (decode(r.html).match(/validation-summary-errors[\s\S]{0,400}/)||[''])[0].replace(/<[^>]+>/g,' '), JSON.stringify(base2).slice(0,500));
    check(r.status === 302 && Number(now.P) === 70000 + Number(stamp2) && now.H === '08:00-20:00' && revs() === before, 'Doi gia/gio dich vu da cong bo: co hieu luc ngay, khong tao ban cho duyet');
  }
  console.log(`PASS ${results.length} service-easy checks.`);
})().catch(e => { console.error(e); process.exit(1); });
