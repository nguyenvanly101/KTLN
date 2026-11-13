// Luong v3: bang tin la trang chu, kham pha gom da luu, nha cung cap chuyen doi co so, hang muc ke hoach them dich vu.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/flow-v3.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, base, decode } = require('./fulltext-smoke.cjs');
const { database, json } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
const user = label => fixture.users.find(x => x.label === label);
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const form = (token, values) => { const f = new FormData(); f.append('__RequestVerificationToken', token); for (const [k, v] of Object.entries(values)) { if (v == null) continue; for (const item of Array.isArray(v) ? v : [v]) f.append(k, String(item)); } return f; };
const text = html => decode(html).replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ');
const sidebarOf = html => (decode(html).match(/<nav[^>]*class="sidebar-nav"[\s\S]*?<\/nav>/) || [''])[0];

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { } if (n > 45) throw Error('not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const traveler = new Client(), provider = new Client(), guest = new Client();
  await traveler.login(user('traveler').email, 'BranchTest@2026'); await provider.login(user('provider').email, 'BranchTest@2026');

  // Khach du lich
  const home = await traveler.req('/');
  check(home.status === 302 && home.location === '/bang-tin', 'Trang chu cua khach du lich la Bang tin');
  const feed = await traveler.get('/bang-tin'); const fh = decode(feed.html);
  check(feed.status === 200 && fh.includes('fb-layout') && fh.includes('fb-left') && fh.includes('fb-right') && fh.includes('data-soc-suggest'), 'Bang tin co 3 cot kieu Facebook (loi tat, bang tin, goi y ket ban)');
  const side = text(sidebarOf(feed.html));
  check(side.indexOf('Bảng tin') >= 0 && side.indexOf('Bảng tin') < side.indexOf('Khám phá') && side.indexOf('Khám phá') < side.indexOf('Chuyến đi') && !side.includes('Dịch vụ đã lưu'), 'Menu khach: Bang tin dau tien, Kham pha (da gom Da luu), Chuyen di');
  check(!/Hỗ trợ chuyến đi|Đặt chỗ của tôi/.test((fh.match(/class="account-dropdown"[\s\S]*?<\/div>/) || [''])[0]), 'Menu tai khoan chi con quan ly tai khoan');
  for (const [path, label] of [['/Discovery', 'Tim kiem'], ['/kham-pha', 'Ban do'], ['/Saved', 'Da luu']]) {
    const r = await traveler.get(path); check(r.status === 200 && decode(r.html).includes('explore-tabs'), `Trang ${label} co thanh chuyen Tim kiem / Ban do / Da luu`);
  }
  check(!decode((await guest.get('/Discovery')).html).includes('explore-tabs'), 'Khach chua dang nhap khong thay thanh tab rieng');

  // Ke hoach: hang muc them dich vu
  const plan = json(database, `SELECT TOP(1) k.MaKeHoach Id FROM dbo.KeHoachChuyenDi k JOIN dbo.TaiKhoan t ON t.MaTaiKhoan=k.MaTaiKhoan WHERE t.Email=N'${user('traveler').email}' ORDER BY k.MaKeHoach DESC FOR JSON PATH;`)[0];
  const svc = json(database, "SELECT TOP(1) d.MaDichVu Id,p.SoDienThoai Phone,p.EmailLienHe Mail FROM dbo.DichVuDuLich d JOIN dbo.NhaCungCapDichVu p ON p.MaNhaCungCap=d.MaNhaCungCap WHERE d.TrangThaiCongBo=N'DaDuyet' ORDER BY d.MaDichVu FOR JSON PATH;")[0];
  if (plan) {
    const blank = await traveler.get('/Plans/Activity/' + plan.Id); const bh = decode(blank.html);
    check(blank.status === 200 && bh.includes('Thêm dịch vụ') && bh.includes('/Discovery?planId=' + plan.Id) && bh.includes('/kham-pha?planId=' + plan.Id), 'Hang muc: co muc Them dich vu (chon da luu, tim kiem, tim tren ban do)');
    const withSvc = await traveler.get(`/Plans/Activity/${plan.Id}?serviceId=${svc.Id}`); const wh = decode(withSvc.html);
    check(withSvc.status === 200 && wh.includes(svc.Phone) && wh.includes(svc.Mail) && wh.includes('/tin-nhan/dich-vu/' + svc.Id), 'Dich vu da chon hien so dien thoai, email va nut nhan tin nha cung cap');
  }

  // Nha cung cap
  const pf = await provider.get('/bang-tin'); const ph = decode(pf.html);
  const ps = text(sidebarOf(pf.html));
  check(ps.indexOf('Bảng tin') < ps.indexOf('Dịch vụ') && ps.indexOf('Dịch vụ') < ps.indexOf('Cơ sở của tôi'), 'Menu nha cung cap: Bang tin, Dich vu, ... Co so cua toi o duoi cung');
  const owned = json(database, `SELECT b.Id,b.Name FROM dbo.CoSoDichVu b JOIN dbo.NhaCungCapDichVu p ON p.MaNhaCungCap=b.ProviderId JOIN dbo.TaiKhoan t ON t.MaTaiKhoan=p.MaTaiKhoan WHERE t.Email=N'${user('provider').email}' AND b.Status='Approved' ORDER BY b.Id FOR JSON PATH;`);
  check(owned.length >= 2 && ph.includes('branch-switch'), 'Co bo chuyen doi co so ben ngoai (nha cung cap co nhieu co so)');
  const crumbs = id => json(database, `SELECT ServiceId S FROM dbo.DichVuCoSo WHERE BranchId=${id} FOR JSON PATH;`).map(x => x.S);
  for (const b of owned.slice(0, 2)) {
    const sw = await provider.req('/nha-cung-cap/co-so/chon', form(val(pf, '__RequestVerificationToken'), { id: b.Id, returnUrl: '/Provider' }));
    check(sw.status === 302 && sw.location === '/Provider', 'Chuyen sang co so ' + b.Name);
    const list = decode((await provider.get('/Provider')).html);
    check(list.includes(b.Name) && list.includes('&gt;'), 'Duong dan hien: Nha cung cap > ' + b.Name);
    const shown = [...list.matchAll(/href="\/Provider\/Edit\/(\d+)"/g)].map(m => Number(m[1]));
    const mine = crumbs(b.Id);
    const other = owned.filter(x => x.Id !== b.Id).flatMap(x => crumbs(x.Id)).filter(s => !mine.includes(s));
    check(mine.every(s => shown.includes(s)) && other.every(s => !shown.includes(s)), 'Danh sach dich vu chi gom dich vu cua co so dang chon');
    const nf = decode((await provider.get('/Provider/Edit')).html);
    check(new RegExp(`name="BranchIds"[^>]*value="${b.Id}"|value="${b.Id}"[^>]*name="BranchIds"`).test(nf) && !nf.includes('pl-branch-option'), 'Form dich vu moi tu gan co so dang chon, khong bat chon lai');
  }
  check((await provider.req('/nha-cung-cap/co-so/chon', form(val(pf, '__RequestVerificationToken'), { id: 999999999, returnUrl: '/' }))).status === 404, 'Khong chon duoc co so cua nguoi khac');

  console.log(`PASS ${results.length} flow-v3 checks.`);
})().catch(e => { console.error(e); process.exit(1); });
