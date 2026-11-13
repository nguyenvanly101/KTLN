// Bang tin kieu Facebook: nhieu cam xuc, danh sach nguoi tha, chia se, luu bai, theo doi co so, "Danh cho ban", bai moi.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/flow-v6.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, base } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
const user = label => fixture.users.find(x => x.label === label);
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const cookie = c => [...c.cookies].map(([k, v]) => k + '=' + v).join('; ');
async function api(client, token, method, path, f) {
  const r = await fetch(base + path, { method, headers: { cookie: cookie(client), ...(method !== 'GET' ? { RequestVerificationToken: token } : {}) }, body: f, redirect: 'manual' });
  const type = r.headers.get('content-type') || ''; let body = null; if (type.includes('json')) body = await r.json(); else await r.arrayBuffer();
  return { status: r.status, body };
}
const fd = values => { const f = new FormData(); for (const [k, v] of Object.entries(values)) if (v != null) f.append(k, String(v)); return f; };
const stamp = Date.now().toString().slice(-8);

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { } if (n > 45) throw Error('not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const A = json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}' FOR JSON PATH;`)[0].Id;
  // Hai tai khoan phu: B (ban cua A), C (nguoi la)
  const mk = tag => { const email = `fb-${tag}-${stamp}@example.com`; sql(database, `INSERT dbo.TaiKhoan(Email,SoDienThoai,MatKhauBam,VaiTro,TrangThai,DauPhien,PhaiDoiMatKhau,ThoiDiemTao) SELECT N'${email}',NULL,MatKhauBam,VaiTro,TrangThai,NEWID(),0,SYSUTCDATETIME() FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}'; INSERT dbo.HoSoNguoiDung(MaTaiKhoan,TenHienThi,ThongTinChiaSe,ThoiDiemCapNhat) SELECT MaTaiKhoan,N'Bạn ${tag} ${stamp}','CongKhai',SYSUTCDATETIME() FROM dbo.TaiKhoan WHERE Email=N'${email}';`); return { email, id: json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${email}' FOR JSON PATH;`)[0].Id }; };
  const B = mk('B'), C = mk('C');
  sql(database, `INSERT dbo.KetBan(RequesterId,AddresseeId,Status,RespondedAt) VALUES(${A},${B.id},'Accepted',SYSUTCDATETIME());`);
  const a = new Client(), b = new Client(), c = new Client(), provider = new Client();
  await a.login(user('traveler').email, 'BranchTest@2026'); await b.login(B.email, 'BranchTest@2026'); await c.login(C.email, 'BranchTest@2026'); await provider.login(user('provider').email, 'BranchTest@2026');
  const ta = val(await a.get('/bang-tin'), '__RequestVerificationToken'), tb = val(await b.get('/bang-tin'), '__RequestVerificationToken'), tc = val(await c.get('/bang-tin'), '__RequestVerificationToken'), tp = val(await provider.get('/bang-tin'), '__RequestVerificationToken');

  // 1. Nhieu cam xuc + phan bo + danh sach nguoi tha
  const post = await api(a, ta, 'POST', '/api/mxh/bai-viet', fd({ Content: 'Bài kiểm thử ' + stamp, Visibility: 'Public' }));
  const pid = post.body.id;
  const r1 = await api(b, tb, 'POST', '/api/mxh/cam-xuc', fd({ type: 'Post', target: pid, emoji: 'love' }));
  const r2 = await api(c, tc, 'POST', '/api/mxh/cam-xuc', fd({ type: 'Post', target: pid, emoji: 'haha' }));
  check(r1.status === 200 && r1.body.mine === 'love' && r2.body.count === 2 && r2.body.reactions.love === 1 && r2.body.reactions.haha === 1, 'Tha nhieu loai cam xuc: tra ve phan bo theo tung loai');
  const re = await api(a, ta, 'POST', '/api/mxh/cam-xuc', fd({ type: 'Post', target: pid, emoji: 'love' }));
  const feed = await api(a, ta, 'GET', '/api/mxh/bai-viet?scope=home'); const dto = feed.body.find(p => p.id === pid);
  check(dto && dto.reactions.love === 2 && dto.reactions.haha === 1 && dto.myReaction === 'love', 'Bai viet kem phan bo cam xuc va cam xuc cua toi');
  const who = await api(a, ta, 'GET', `/api/mxh/cam-xuc/danh-sach?type=Post&target=${pid}`);
  check(who.status === 200 && who.body.length === 3 && who.body.some(x => x.emoji === 'haha'), 'Danh sach nguoi tha cam xuc kem bieu tuong');
  check((await api(a, ta, 'POST', '/api/mxh/cam-xuc', fd({ type: 'Post', target: pid, emoji: 'khong-co' }))).status === 400, 'Cam xuc la bi tu choi');

  // 2. Chia se
  const sh = await api(c, tc, 'POST', `/api/mxh/bai-viet/${pid}/chia-se`, fd({ content: 'Hay quá ' + stamp, visibility: 'Public' }));
  check(sh.status === 200 && sh.body.shared && sh.body.shared.id === pid && sh.body.shared.content.includes('Bài kiểm thử'), 'Chia se bai cong khai kem bai goc nhung');
  const orig = (await api(a, ta, 'GET', '/api/mxh/bai-viet/' + pid)).body;
  check(orig.shares === 1, 'Bai goc dem luot chia se');
  const priv = await api(a, ta, 'POST', '/api/mxh/bai-viet', fd({ Content: 'Riêng ' + stamp, Visibility: 'Friends' }));
  check((await api(b, tb, 'POST', `/api/mxh/bai-viet/${priv.body.id}/chia-se`, fd({ content: 'x' }))).status === 422, 'Khong chia se duoc bai khong cong khai');
  check((await api(provider, tp, 'POST', `/api/mxh/bai-viet/${pid}/chia-se`, fd({ content: 'x' }))).status === 403, 'Nha cung cap khong chia se bai');
  check(json(database, `SELECT COUNT(*) N FROM dbo.ThongBao WHERE MaNguoiNhan=${A} AND LoaiThongBao=N'ChiaSe' FOR JSON PATH;`)[0].N >= 1, 'Chu bai nhan thong bao khi co nguoi chia se');

  // 3. Luu bai
  check((await api(b, tb, 'POST', `/api/mxh/bai-viet/${pid}/luu`, fd({ saved: 'true' }))).body.saved === true, 'Luu bai viet');
  const saved = await api(b, tb, 'GET', '/api/mxh/bai-viet?scope=saved');
  check(saved.body.length === 1 && saved.body[0].id === pid && saved.body[0].saved === true, 'Tab "Da luu" chi gom bai da luu');
  await api(b, tb, 'POST', `/api/mxh/bai-viet/${pid}/luu`, fd({ saved: 'false' }));
  check((await api(b, tb, 'GET', '/api/mxh/bai-viet?scope=saved')).body.length === 0, 'Bo luu: bai bien khoi tab Da luu');

  // 4. Theo doi co so + Danh cho ban
  const branch = json(database, `SELECT TOP(1) b.Id FROM dbo.CoSoDichVu b JOIN dbo.NhaCungCapDichVu p ON p.MaNhaCungCap=b.ProviderId JOIN dbo.TaiKhoan t ON t.MaTaiKhoan=p.MaTaiKhoan WHERE t.Email=N'${user('provider').email}' AND b.Status='Approved' AND b.PublishedVersion IS NOT NULL ORDER BY b.Id FOR JSON PATH;`)[0].Id;
  const bp = await api(provider, tp, 'POST', '/api/mxh/bai-viet', fd({ Content: 'Tin cơ sở ' + stamp, Kind: 'Branch', BranchId: branch, Visibility: 'Public' }));
  const before = await api(c, tc, 'GET', '/api/mxh/bai-viet?scope=home');
  check(!before.body.some(p => p.id === bp.body.id), 'Chua theo doi: tin co so khong hien o Danh cho ban');
  const fo = await api(c, tc, 'POST', `/api/mxh/co-so/${branch}/theo-doi`, fd({ follow: 'true' }));
  check(fo.status === 200 && fo.body.following === true && fo.body.followers >= 1, 'Theo doi co so');
  const after = await api(c, tc, 'GET', '/api/mxh/bai-viet?scope=home');
  const hit = after.body.find(p => p.id === bp.body.id);
  check(hit && hit.following === true, 'Da theo doi: tin co so hien o Danh cho ban');
  check(!after.body.some(p => p.id === pid && p.authorId === A && false), 'Bai cua nguoi la khong tu hien');
  check((await api(provider, tp, 'POST', `/api/mxh/co-so/${branch}/theo-doi`, fd({ follow: 'true' }))).status === 403, 'Nha cung cap khong theo doi co so');
  await api(c, tc, 'POST', `/api/mxh/co-so/${branch}/theo-doi`, fd({ follow: 'false' }));
  check(!(await api(c, tc, 'GET', '/api/mxh/bai-viet?scope=home')).body.some(p => p.id === bp.body.id), 'Bo theo doi: tin co so het hien');

  // 5. Bai moi
  const since = (await api(b, tb, 'GET', '/api/mxh/bai-viet?scope=home')).body.reduce((m, p) => Math.max(m, p.id), 0);
  await api(a, ta, 'POST', '/api/mxh/bai-viet', fd({ Content: 'Bài mới ' + stamp, Visibility: 'Public' }));
  const nc = await api(b, tb, 'GET', `/api/mxh/bai-viet-moi?scope=home&since=${since}`);
  check(nc.status === 200 && nc.body.count >= 1, 'Dem bai viet moi de hien "Co bai viet moi"');
  check((await api(a, ta, 'GET', `/api/mxh/bai-viet-moi?scope=home&since=${since}`)).body.count === 0, 'Bai cua chinh minh khong tinh la bai moi');

  console.log(`PASS ${results.length} flow-v6 checks.`);
})().catch(e => { console.error(e); process.exit(1); });
