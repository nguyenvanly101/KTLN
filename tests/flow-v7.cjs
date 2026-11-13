// Trang ca nhan kieu Facebook: anh bia, tab Gioi thieu / Ban be / Anh, quyen xem.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/flow-v7.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, base, decode } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
const user = label => fixture.users.find(x => x.label === label);
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const cookie = c => [...c.cookies].map(([k, v]) => k + '=' + v).join('; ');
const PNG = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==', 'base64');
async function api(client, token, method, path, f) {
  const r = await fetch(base + path, { method, headers: { cookie: cookie(client), ...(method !== 'GET' ? { RequestVerificationToken: token } : {}) }, body: f, redirect: 'manual' });
  const type = r.headers.get('content-type') || ''; let body = null; if (type.includes('json')) body = await r.json(); else await r.arrayBuffer();
  return { status: r.status, body, type };
}
const stamp = Date.now().toString().slice(-8);

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { } if (n > 45) throw Error('not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const A = json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}' FOR JSON PATH;`)[0].Id;
  const mk = tag => { const email = `pf-${tag}-${stamp}@example.com`; sql(database, `INSERT dbo.TaiKhoan(Email,SoDienThoai,MatKhauBam,VaiTro,TrangThai,DauPhien,PhaiDoiMatKhau,ThoiDiemTao) SELECT N'${email}',NULL,MatKhauBam,VaiTro,TrangThai,NEWID(),0,SYSUTCDATETIME() FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}'; INSERT dbo.HoSoNguoiDung(MaTaiKhoan,TenHienThi,ThongTinChiaSe,ThoiDiemCapNhat) SELECT MaTaiKhoan,N'Bạn ${tag} ${stamp}','CongKhai',SYSUTCDATETIME() FROM dbo.TaiKhoan WHERE Email=N'${email}';`); return { email, id: json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${email}' FOR JSON PATH;`)[0].Id }; };
  const B = mk('B');
  sql(database, `DELETE FROM dbo.KetBan WHERE (RequesterId=${A} AND AddresseeId=${B.id}) OR (RequesterId=${B.id} AND AddresseeId=${A}); INSERT dbo.KetBan(RequesterId,AddresseeId,Status,RespondedAt) VALUES(${A},${B.id},'Accepted',SYSUTCDATETIME()); UPDATE dbo.HoSoNguoiDung SET ThongTinChiaSe='CongKhai' WHERE MaTaiKhoan=${A};`);
  const a = new Client(), b = new Client(); await a.login(user('traveler').email, 'BranchTest@2026'); await b.login(B.email, 'BranchTest@2026');

  // 1. Doi anh bia
  const pg = await a.get('/Account/Profile'); const ph = decode(pg.html);
  check(ph.includes('name="Cover"'), 'Form ho so co o chon anh bia');
  const f = new FormData(); f.append('__RequestVerificationToken', val(pg, '__RequestVerificationToken')); f.append('Version', val(pg, 'Version'));
  f.append('Name', 'Kiểm thử traveler'); f.append('Privacy', 'CongKhai'); f.append('Bio', 'Thích đi biển ' + stamp);
  f.append('Cover', new Blob([PNG], { type: 'image/png' }), 'bia.png');
  const save = await a.req('/Account/Profile', f);
  check(save.status === 302, 'Luu ho so kem anh bia');
  check(json(database, `SELECT AnhBia B FROM dbo.HoSoNguoiDung WHERE MaTaiKhoan=${A} FOR JSON PATH;`)[0].B != null, 'Anh bia duoc luu');

  // 2. Trang ca nhan hien anh bia + tab
  const prof = await b.get('/nguoi/' + A); const h = decode(prof.html);
  check(prof.status === 200 && h.includes('fb-cover') && h.includes(`/mxh/bia/${A}`) && h.includes('data-tab="friends"') && h.includes('data-tab="photos"') && h.includes('Thích đi biển ' + stamp), 'Trang ca nhan: anh bia, cac tab Bai viet/Gioi thieu/Ban be/Anh');
  const cover = await fetch(base + `/mxh/bia/${A}`, { headers: { cookie: cookie(b) } });
  check(cover.status === 200 && (cover.headers.get('content-type') || '').startsWith('image/'), 'Tai duoc anh bia');
  const guestCover = await fetch(base + `/mxh/bia/${A}`, { redirect: 'manual' });
  check(guestCover.status >= 300, 'Khach chua dang nhap khong xem duoc anh bia');

  // 3. Danh sach ban be va anh
  const tb = val(await b.get('/bang-tin'), '__RequestVerificationToken'), ta = val(await a.get('/bang-tin'), '__RequestVerificationToken');
  const fr = await api(b, tb, 'GET', `/api/mxh/nguoi/${A}/ban-be`);
  check(fr.status === 200 && fr.body.some(p => p.id === B.id), 'Tab Ban be liet ke ban be cua nguoi do');
  const pf = new FormData(); pf.append('Content', 'Có ảnh ' + stamp); pf.append('Visibility', 'Public'); pf.append('Photos', new Blob([PNG], { type: 'image/png' }), 'a.png'); pf.append('Photos', new Blob([PNG], { type: 'image/png' }), 'b.png');
  const post = await api(a, ta, 'POST', '/api/mxh/bai-viet', pf);
  check(post.status === 200 && post.body.photos === 2, 'Dang bai kem 2 anh');
  const ph2 = await api(b, tb, 'GET', `/api/mxh/nguoi/${A}/anh`);
  check(ph2.status === 200 && ph2.body.length >= 2 && ph2.body.some(u => u.includes(`/mxh/bai-viet/${post.body.id}/anh/`)), 'Tab Anh liet ke anh trong bai viet');
  const img = await fetch(base + ph2.body[0], { headers: { cookie: cookie(b) } });
  check(img.status === 200, 'Mo duoc anh trong bai viet');

  // 4. Quyen xem: ho so rieng tu thi khong lo ban be / anh bia
  sql(database, `UPDATE dbo.HoSoNguoiDung SET ThongTinChiaSe='RiengTu' WHERE MaTaiKhoan=${A};`);
  const C = mk('C'); const c = new Client(); await c.login(C.email, 'BranchTest@2026'); const tc = val(await c.get('/bang-tin'), '__RequestVerificationToken');
  check((await api(c, tc, 'GET', `/api/mxh/nguoi/${A}/ban-be`)).status === 403, 'Ho so rieng tu: nguoi la khong xem duoc danh sach ban be');
  check((await fetch(base + `/mxh/bia/${A}`, { headers: { cookie: cookie(c) } })).status === 404, 'Ho so rieng tu: nguoi la khong xem duoc anh bia');
  sql(database, `UPDATE dbo.HoSoNguoiDung SET ThongTinChiaSe='CongKhai' WHERE MaTaiKhoan=${A};`);

  console.log(`PASS ${results.length} flow-v7 checks.`);
})().catch(e => { console.error(e); process.exit(1); });
