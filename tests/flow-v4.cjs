// Bang tin v4: tim ban dong hanh co loc, gan the dich vu, bao cao bai viet, binh luan/thong bao cho nha cung cap, danh sach viec cua nha cung cap.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/flow-v4.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, base, decode } = require('./fulltext-smoke.cjs');
const { database, json } = require('./branches-sql.cjs');
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
const form = (token, values) => { const f = fd(values); f.append('__RequestVerificationToken', token); return f; };
const day = n => new Date(Date.now() + n * 864e5).toISOString().slice(0, 10);
const stamp = Date.now().toString().slice(-8);

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { } if (n > 45) throw Error('not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const traveler = new Client(), provider = new Client(), admin = new Client();
  await traveler.login(user('traveler').email, 'BranchTest@2026'); await provider.login(user('provider').email, 'BranchTest@2026'); await admin.login(user('admin').email, 'BranchTest@2026');
  const tk = val(await traveler.get('/bang-tin'), '__RequestVerificationToken');
  const pk = val(await provider.get('/bang-tin'), '__RequestVerificationToken');
  const svc = json(database, "SELECT TOP(1) MaDichVu Id,TenDichVu N FROM dbo.DichVuDuLich WHERE TrangThaiCongBo=N'DaDuyet' ORDER BY MaDichVu FOR JSON PATH;")[0];

  // 1. Tim ban dong hanh co loc + the dich vu
  const dest = 'Hội An ' + stamp;
  const comp = await api(traveler, tk, 'POST', '/api/mxh/bai-viet', fd({ Content: 'Cần bạn đi cùng ' + stamp, Visibility: 'Public', Kind: 'Companion', Destination: dest, TripStart: day(5), TripEnd: day(8), ServiceId: svc.Id }));
  check(comp.status === 200 && comp.body.serviceId === svc.Id && comp.body.serviceName === svc.N, 'Bai co gan the dich vu (ten dich vu tra ve kem)');
  check((await api(traveler, tk, 'POST', '/api/mxh/bai-viet', fd({ Content: 'x', Visibility: 'Public', ServiceId: 999999999 }))).status === 422, 'Gan dich vu khong ton tai bi tu choi');
  const normal = await api(traveler, tk, 'POST', '/api/mxh/bai-viet', fd({ Content: 'Bài thường ' + stamp, Visibility: 'Public' }));
  const filt = await api(traveler, tk, 'GET', '/api/mxh/bai-viet?scope=companion&q=' + encodeURIComponent(stamp));
  check(filt.status === 200 && filt.body.some(p => p.id === comp.body.id) && !filt.body.some(p => p.id === normal.body.id), 'Loc "Tim ban dong hanh" theo diem den: chi hien tin dong hanh phu hop');
  check((await api(traveler, tk, 'GET', '/api/mxh/bai-viet?scope=companion&q=khong-co-' + stamp)).body.length === 0, 'Tu khoa khong khop thi khong co ket qua');

  // 2. Nha cung cap binh luan -> link toi co so; khach nhan thong bao
  const tid = json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}' FOR JSON PATH;`)[0].Id;
  const cm = await api(provider, pk, 'POST', `/api/mxh/bai-viet/${normal.body.id}/binh-luan`, fd({ Content: 'Quán xin chào ' + stamp }));
  check(cm.status === 200 && /^\/co-so\/\d+$/.test(cm.body.link || ''), 'Binh luan cua nha cung cap co lien ket toi trang co so');
  const note = json(database, `SELECT COUNT(*) N FROM dbo.ThongBao WHERE MaNguoiNhan=${tid} AND NoiDung LIKE N'%Quán xin chào ${stamp}%' FOR JSON PATH;`)[0].N;
  check(note === 1, 'Khach nhan thong bao khi nha cung cap binh luan');

  // 3. Khach binh luan bai cua co so -> nha cung cap nhan thong bao
  const branch = json(database, `SELECT TOP(1) b.Id FROM dbo.CoSoDichVu b JOIN dbo.NhaCungCapDichVu p ON p.MaNhaCungCap=b.ProviderId JOIN dbo.TaiKhoan t ON t.MaTaiKhoan=p.MaTaiKhoan WHERE t.Email=N'${user('provider').email}' AND b.Status='Approved' AND b.PublishedVersion IS NOT NULL ORDER BY b.Id FOR JSON PATH;`)[0].Id;
  const post = await api(provider, pk, 'POST', '/api/mxh/bai-viet', fd({ Content: 'Ưu đãi ' + stamp, Kind: 'Branch', BranchId: branch, Visibility: 'Public' }));
  const pid = json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${user('provider').email}' FOR JSON PATH;`)[0].Id;
  await api(traveler, tk, 'POST', `/api/mxh/bai-viet/${post.body.id}/binh-luan`, fd({ Content: 'Còn bàn không ' + stamp }));
  check(json(database, `SELECT COUNT(*) N FROM dbo.ThongBao WHERE MaNguoiNhan=${pid} AND NoiDung LIKE N'%Còn bàn không ${stamp}%' FOR JSON PATH;`)[0].N === 1, 'Nha cung cap nhan thong bao khi khach binh luan tin cua co so');
  const pcm = await api(provider, pk, 'GET', `/api/mxh/bai-viet/${post.body.id}/binh-luan`);
  check(pcm.status === 200 && pcm.body.some(c => c.canDelete), 'Nha cung cap xem va quan ly binh luan duoi tin cua minh');

  // 4. Bao cao bai viet
  check((await api(traveler, tk, 'POST', '/api/mxh/bao-cao', fd({ postId: normal.body.id, reason: 'Spam' }))).status === 400, 'Khong tu bao cao bai cua minh');
  check((await api(provider, pk, 'POST', '/api/mxh/bao-cao', fd({ postId: normal.body.id, reason: 'Khong-hop-le' }))).status === 400, 'Ly do bao cao khong hop le bi tu choi');
  check((await api(provider, pk, 'POST', '/api/mxh/bao-cao', fd({ postId: normal.body.id, reason: 'Spam', note: 'Thử ' + stamp }))).status === 200, 'Gui bao cao bai viet');
  check((await api(provider, pk, 'POST', '/api/mxh/bao-cao', fd({ postId: normal.body.id, reason: 'Spam' }))).status === 409, 'Khong bao cao trung khi con cho xu ly');
  const queue = await admin.get('/quan-tri/bao-cao-bai-viet'); const qh = decode(queue.html);
  check(queue.status === 200 && qh.includes('Bài thường ' + stamp) && qh.includes('Thử ' + stamp), 'Admin thay bao cao trong hang doi');
  const rid = json(database, `SELECT TOP(1) Id FROM dbo.BaoCaoBaiViet WHERE PostId=${normal.body.id} ORDER BY Id DESC FOR JSON PATH;`)[0].Id;
  const hide = await admin.req('/quan-tri/bao-cao-bai-viet/xu-ly', form(val(queue, '__RequestVerificationToken'), { id: rid, hide: 'true' }));
  check(hide.status === 302 && (await api(traveler, tk, 'GET', '/api/mxh/bai-viet/' + normal.body.id)).status === 404, 'Admin an bai bi bao cao: bai khong con xem duoc');
  check((await traveler.get('/quan-tri/bao-cao-bai-viet')).status >= 300, 'Khach khong vao duoc hang doi bao cao');

  // 5. Danh sach viec nha cung cap
  const pf = decode((await provider.get('/bang-tin')).html);
  check(!pf.includes('data-provider-checklist') || pf.includes('Việc cần làm'), 'Bang tin nha cung cap hien danh sach viec can lam khi con thieu buoc');

  console.log(`PASS ${results.length} flow-v4 checks.`);
})().catch(e => { console.error(e); process.exit(1); });
