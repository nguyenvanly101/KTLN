// Mang xa hoi kieu Facebook: ket ban, ho so, bai viet (quyen xem), binh luan, cam xuc, phan trang.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5225 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/social.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, base } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
assert.equal(fixture.database, database);
const user = label => fixture.users.find(x => x.label === label);
const results = [];
function check(ok, name) { assert.ok(ok, name); results.push(name); }
const cookie = c => [...c.cookies].map(([k, v]) => k + '=' + v).join('; ');
const PNG = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==', 'base64');
async function api(client, token, method, path, form) {
  const r = await fetch(base + path, { method, headers: { cookie: cookie(client), ...(method !== 'GET' ? { RequestVerificationToken: token } : {}) }, body: form, redirect: 'manual' });
  let body = null; const type = r.headers.get('content-type') || '';
  if (type.includes('json')) body = await r.json(); else await r.arrayBuffer();
  return { status: r.status, body, type };
}
const fd = (values, photos = 0) => { const f = new FormData(); for (const [k, v] of Object.entries(values)) if (v != null) f.append(k, String(v)); for (let n = 0; n < photos; n++) f.append('Photos', new Blob([PNG], { type: 'image/png' }), `a${n}.png`); return f; };
const stamp = Date.now().toString().slice(-8);

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { /* cho */ } if (n > 45) throw Error('Server not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const A = json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}' FOR JSON PATH;`)[0].Id;
  const posterRow = json(database, "SELECT TOP(1) a.MaTaiKhoan Id,a.Email FROM dbo.NhuCauDongHanh n JOIN dbo.TaiKhoan a ON a.MaTaiKhoan=n.MaTaiKhoan WHERE a.VaiTro='KHACH_DU_LICH' AND a.TrangThai='HoatDong' ORDER BY n.MaNhuCau FOR JSON PATH;")[0];
  const B = posterRow.Id;
  const providerAccount = json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${user('provider').email}' FOR JSON PATH;`)[0].Id;
  // Tai khoan thu thu ba (khach la voi B), mat khau giong khach thu
  const cEmail = `social-c-${stamp}@example.com`, cName = 'Bạn Thử C ' + stamp;
  sql(database, `INSERT dbo.TaiKhoan(Email,SoDienThoai,MatKhauBam,VaiTro,TrangThai,DauPhien,PhaiDoiMatKhau,ThoiDiemTao) SELECT N'${cEmail}',NULL,MatKhauBam,VaiTro,TrangThai,NEWID(),0,SYSUTCDATETIME() FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}';`);
  const C = json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${cEmail}' FOR JSON PATH;`)[0].Id;
  sql(database, `INSERT dbo.HoSoNguoiDung(MaTaiKhoan,TenHienThi,ThongTinChiaSe,ThoiDiemCapNhat) VALUES(${C},N'${cName}','CongKhai',SYSUTCDATETIME());`);
  const ids = `${A},${B},${C}`;
  sql(database, `DELETE FROM dbo.BaoCaoBaiViet; DELETE FROM dbo.BaoCaoChat; DELETE FROM dbo.PhanUngTin; UPDATE dbo.TinNhanChat SET ReplyToId=NULL WHERE ReplyToId IS NOT NULL; DELETE m FROM dbo.TinNhanChat m JOIN dbo.CuocTroChuyen c ON c.Id=m.ConversationId WHERE c.BranchId IS NULL; DELETE FROM dbo.ThanhVienChat; DELETE FROM dbo.NhomChatChuyenDi WHERE ConversationId IN (SELECT Id FROM dbo.CuocTroChuyen WHERE BranchId IS NULL); DELETE FROM dbo.CuocTroChuyen WHERE BranchId IS NULL; DELETE FROM dbo.KetBan WHERE RequesterId IN (${ids}) OR AddresseeId IN (${ids}); DELETE FROM dbo.CamXuc WHERE AccountId IN (${ids}); DELETE FROM dbo.BinhLuan WHERE ParentId IS NOT NULL AND (AuthorId IN (${ids}) OR PostId IN (SELECT Id FROM dbo.BaiViet WHERE AuthorId IN (${ids}))); DELETE FROM dbo.BinhLuan WHERE AuthorId IN (${ids}); DELETE FROM dbo.BinhLuan WHERE PostId IN (SELECT Id FROM dbo.BaiViet WHERE AuthorId IN (${ids})); DELETE FROM dbo.BaiViet WHERE AuthorId IN (${ids});`);

  const a = new Client(), b = new Client(), c = new Client(), provider = new Client(), guest = new Client();
  await a.login(user('traveler').email, 'BranchTest@2026');
  try { await b.login(posterRow.Email); } catch { await b.login(posterRow.Email, 'HanhTrinhMoi@2026'); }
  await c.login(cEmail, 'BranchTest@2026');
  await provider.login(user('provider').email, 'BranchTest@2026');
  const tok = async client => val(await client.get('/ban-be'), '__RequestVerificationToken');
  const tA = await tok(a), tB = await tok(b), tC = await tok(c);
  const tP = val(await provider.get('/tin-nhan'), '__RequestVerificationToken');
  const bName = json(database, `SELECT TenHienThi N FROM dbo.HoSoNguoiDung WHERE MaTaiKhoan=${B} FOR JSON PATH;`)[0].N;

  // 1. Truy cap
  check([302, 401].includes((await guest.get('/ban-be')).status), 'Khach vang lai khong vao Ban be');
  check((await provider.get('/ban-be')).status === 403 && (await provider.get('/bang-tin')).status === 200, 'Nha cung cap khong vao Ban be, nhung vao duoc Bang tin');
  check((await api(provider, tP, 'GET', '/api/mxh/ban-be')).status === 403, 'API ban be tu choi nha cung cap');
  check((await a.get('/ban-be')).status === 200 && (await a.get('/bang-tin')).html.includes('data-soc-feed'), 'Trang Ban be va Bang tin mo duoc');

  // 2. Tim nguoi + ket ban
  check((await api(a, tA, 'GET', '/api/mxh/tim?q=a')).status === 422, 'Tim voi it hon 2 ky tu bi tu choi');
  const found = await api(a, tA, 'GET', '/api/mxh/tim?q=' + encodeURIComponent(cName.slice(0, 12)));
  check(found.status === 200 && found.body.some(p => p.id === C && p.relation === 'none') && found.body.every(p => p.id !== A), 'Tim theo ten thay nguoi la, khong thay chinh minh');
  check((await api(a, tA, 'POST', '/api/mxh/ban-be/gui', fd({ accountId: A }))).status === 422, 'Khong tu ket ban');
  check((await api(a, tA, 'POST', '/api/mxh/ban-be/gui', fd({ accountId: providerAccount }))).status === 404, 'Khong ket ban voi tai khoan nha cung cap');
  check((await api(a, tA, 'POST', '/api/mxh/ban-be/gui', fd({ accountId: B }))).body.relation === 'sent', 'Gui loi moi ket ban');
  check((await api(a, tA, 'POST', '/api/mxh/ban-be/gui', fd({ accountId: B }))).status === 409, 'Gui trung loi moi bi tu choi');
  const bReq = (await api(b, tB, 'GET', '/api/mxh/loi-moi')).body;
  const incoming = bReq.incoming.find(p => p.id === A);
  check(incoming && incoming.relation === 'received' && incoming.requestId > 0, 'Nguoi nhan thay loi moi');
  check((await api(a, tA, 'POST', `/api/mxh/ban-be/${incoming.requestId}/chap-nhan`)).status === 403, 'Nguoi gui khong tu chap nhan');
  check((await api(c, tC, 'POST', `/api/mxh/ban-be/${incoming.requestId}/chap-nhan`)).status === 404, 'Nguoi ngoai khong xu ly duoc loi moi');
  check((await api(b, tB, 'POST', `/api/mxh/ban-be/${incoming.requestId}/chap-nhan`)).status === 200, 'Chap nhan ket ban');
  check((await api(a, tA, 'GET', '/api/mxh/ban-be')).body.some(p => p.id === B && p.relation === 'friends') && (await api(b, tB, 'GET', '/api/mxh/ban-be')).body.some(p => p.id === A), 'Hai ben deu thay nhau trong danh sach ban be');
  check((await api(a, tA, 'POST', '/api/mxh/ban-be/gui', fd({ accountId: B }))).status === 409, 'Da la ban thi khong gui lai');

  // 3. Tu choi, gui lai, huy, xoa ban
  const sentC = await api(a, tA, 'POST', '/api/mxh/ban-be/gui', fd({ accountId: C }));
  const cIncoming = (await api(c, tC, 'GET', '/api/mxh/loi-moi')).body.incoming.find(p => p.id === A);
  check(sentC.body.relation === 'sent' && cIncoming, 'A gui loi moi cho C');
  check((await api(c, tC, 'POST', `/api/mxh/ban-be/${cIncoming.requestId}/tu-choi`)).status === 200, 'C tu choi loi moi');
  check((await api(a, tA, 'POST', '/api/mxh/ban-be/gui', fd({ accountId: C }))).status === 409, 'A khong gui lai ngay sau khi bi tu choi');
  check((await api(c, tC, 'POST', '/api/mxh/ban-be/gui', fd({ accountId: A }))).body.relation === 'sent', 'C co the chu dong gui loi moi cho A');
  const aIn = (await api(a, tA, 'GET', '/api/mxh/loi-moi')).body.incoming.find(p => p.id === C);
  check((await api(a, tA, 'POST', `/api/mxh/ban-be/${aIn.requestId}/chap-nhan`)).status === 200, 'A chap nhan, A va C thanh ban');
  check((await api(a, tA, 'POST', '/api/mxh/ban-be/xoa', fd({ accountId: C }))).status === 200, 'Huy ket ban');
  check((await api(a, tA, 'POST', '/api/mxh/ban-be/xoa', fd({ accountId: C }))).status === 404, 'Huy ket ban hai lan bi tu choi');
  const again = await api(a, tA, 'POST', '/api/mxh/ban-be/gui', fd({ accountId: C }));
  const sentRows = (await api(a, tA, 'GET', '/api/mxh/loi-moi')).body.outgoing.find(p => p.id === C);
  check(again.status === 200 && sentRows, 'Gui lai sau khi huy ket ban');
  check((await api(c, tC, 'POST', `/api/mxh/ban-be/${sentRows.requestId}/huy`)).status === 403, 'Nguoi nhan khong huy ho loi moi');
  check((await api(a, tA, 'POST', `/api/mxh/ban-be/${sentRows.requestId}/huy`)).status === 200, 'Nguoi gui huy loi moi');

  // 4. Goi y ban chung + ho so
  check((await api(c, tC, 'POST', '/api/mxh/ban-be/gui', fd({ accountId: B }))).body.relation === 'sent', 'C gui loi moi cho B');
  const bIn = (await api(b, tB, 'GET', '/api/mxh/loi-moi')).body.incoming.find(p => p.id === C);
  check((await api(b, tB, 'POST', `/api/mxh/ban-be/${bIn.requestId}/chap-nhan`)).status === 200, 'B chap nhan C');
  const sug = (await api(a, tA, 'GET', '/api/mxh/goi-y')).body;
  check(sug.some(p => p.id === C && p.mutual === 1), 'Goi y ket ban uu tien nguoi co ban chung');
  check(sug.every(p => p.relation === 'none' && p.id !== A), 'Goi y khong co ban be, loi moi dang cho hay chinh minh');
  const profile = await a.get('/nguoi/' + B);
  check(profile.status === 200 && profile.text.includes(bName) && profile.html.includes('data-soc-profile'), 'Trang ho so hien ten nguoi dung');
  check((await a.get('/nguoi/99999999')).status === 404 && (await provider.get('/nguoi/' + B)).status === 403, 'Ho so khong ton tai 404, nha cung cap 403');
  const hoSo = (await api(a, tA, 'GET', '/api/mxh/ho-so/' + C)).body;
  check(hoSo.relation === 'none' && hoSo.mutual === 1 && !('email' in hoSo), 'Ho so co so ban chung, khong lo email');
  check((await api(a, tA, 'GET', '/mxh/anh/' + B)).status === 404, 'Khong co anh dai dien thi 404');

  // 5. Chat 1-1 cho ban be
  check((await api(a, tA, 'POST', '/chat/gui', fd({ PeerId: C, Body: 'chào C' }))).status === 403, 'Chua la ban thi khong nhan tin (khi khong co loi ru cong khai)');
  const sentFriend = await api(a, tA, 'POST', '/api/mxh/ban-be/gui', fd({ accountId: C })); const cIn2 = (await api(c, tC, 'GET', '/api/mxh/loi-moi')).body.incoming.find(p => p.id === A);
  await api(c, tC, 'POST', `/api/mxh/ban-be/${cIn2.requestId}/chap-nhan`);
  check(sentFriend.status === 200 && (await api(a, tA, 'POST', '/chat/gui', fd({ PeerId: C, Body: 'chào C' }))).status === 200, 'Ban be nhan tin duoc cho nhau');

  // 6. Bai viet va quyen xem
  const mk = async (client, token, extra, photos = 0) => api(client, token, 'POST', '/api/mxh/bai-viet', fd(extra, photos));
  check((await mk(b, tB, { Content: '', Visibility: 'Public' })).status === 422, 'Bai viet rong bi tu choi');
  check((await mk(b, tB, { Content: 'x'.repeat(4001), Visibility: 'Public' })).status === 400, 'Bai viet qua 4000 ky tu bi tu choi');
  check((await mk(b, tB, { Content: 'ảnh', Visibility: 'Public' }, 5)).status === 400, 'Hon 4 anh bi tu choi');
  check((await mk(b, tB, { Content: 'sai', Visibility: 'Secret' })).status === 400, 'Quyen xem khong hop le bi tu choi');
  const pub = (await mk(b, tB, { Content: 'Bài công khai ' + stamp, Visibility: 'Public' })).body;
  const fri = (await mk(b, tB, { Content: 'Bài chỉ bạn bè ' + stamp, Visibility: 'Friends' }, 2)).body;
  const pri = (await mk(b, tB, { Content: 'Bài riêng tư ' + stamp, Visibility: 'Private' })).body;
  check(pub.id && fri.photos === 2 && fri.mine && pri.id, 'Tao bai cong khai / ban be / rieng tu (co anh)');
  // C da la ban cua B tu buoc 4; dung D (tai khoan nguoi la) = chinh A sau khi huy? -> tao nguoi la bang cach xoa ban B-C
  const feedA = (await api(a, tA, 'GET', '/api/mxh/bai-viet?scope=friends')).body;
  check(feedA.some(p => p.id === pub.id) && feedA.some(p => p.id === fri.id) && feedA.every(p => p.id !== pri.id), 'Ban be thay bai cong khai va bai ban be, khong thay bai rieng tu');
  check((await api(a, tA, 'GET', '/api/mxh/bai-viet/' + pri.id)).status === 404, 'Bai rieng tu 404 voi nguoi khac');
  check((await api(b, tB, 'GET', '/api/mxh/bai-viet/' + pri.id)).status === 200, 'Tac gia xem duoc bai rieng tu');
  sql(database, `DELETE FROM dbo.KetBan WHERE (RequesterId=${B} AND AddresseeId=${C}) OR (RequesterId=${C} AND AddresseeId=${B});`);
  check((await api(c, tC, 'GET', '/api/mxh/bai-viet/' + fri.id)).status === 404, 'Nguoi la khong xem duoc bai ban be');
  check((await api(c, tC, 'GET', `/mxh/bai-viet/${fri.id}/anh/0`)).status === 404, 'Nguoi la khong xem duoc anh cua bai ban be');
  check((await api(a, tA, 'GET', `/mxh/bai-viet/${fri.id}/anh/0`)).type.includes('image/png'), 'Ban be xem duoc anh bai viet');
  check((await api(a, tA, 'GET', `/mxh/bai-viet/${fri.id}/anh/5`)).status === 404, 'Anh ngoai pham vi 404');
  check((await api(c, tC, 'GET', '/api/mxh/bai-viet/' + pub.id)).status === 200, 'Bai cong khai ai cung xem');
  const byB = (await api(c, tC, 'GET', `/api/mxh/nguoi/${B}/bai-viet`)).body;
  check(byB.some(p => p.id === pub.id) && byB.every(p => p.id !== fri.id && p.id !== pri.id), 'Trang ho so chi hien bai nguoi xem duoc');
  const community = (await api(c, tC, 'GET', '/api/mxh/bai-viet?scope=public')).body;
  check(community.some(p => p.id === pub.id) && community.every(p => p.visibility === 'Public'), 'Tab Cong dong chi co bai cong khai');
  check((await a.get('/bai-viet/' + pub.id)).status === 200 && (await c.get('/bai-viet/' + fri.id)).status === 404, 'Trang mot bai viet theo quyen xem');
  check((await provider.get('/bai-viet/' + pub.id)).status === 200, 'Nha cung cap xem duoc bai viet cong khai');

  // 7. Sua / xoa bai viet
  check((await api(a, tA, 'POST', `/api/mxh/bai-viet/${pub.id}/sua`, fd({ Content: 'hack', Visibility: 'Public' }))).status === 403, 'Khong sua bai cua nguoi khac');
  const edited = (await api(b, tB, 'POST', `/api/mxh/bai-viet/${pub.id}/sua`, fd({ Content: 'Bài đã sửa ' + stamp, Visibility: 'Friends' }))).body;
  check(edited.content.includes('đã sửa') && edited.visibility === 'Friends' && edited.updatedAt, 'Tac gia sua noi dung va quyen xem');
  check((await api(c, tC, 'GET', '/api/mxh/bai-viet/' + pub.id)).status === 404, 'Doi sang ban be thi nguoi la khong con thay');
  await api(b, tB, 'POST', `/api/mxh/bai-viet/${pub.id}/sua`, fd({ Content: 'Bài đã sửa ' + stamp, Visibility: 'Public' }));
  check((await api(b, tB, 'POST', `/api/mxh/bai-viet/${fri.id}/sua`, fd({ Content: '', Visibility: 'Friends', RemovePhotos: 'true' }))).status === 422, 'Bo het noi dung va anh bi tu choi');
  check((await api(b, tB, 'POST', `/api/mxh/bai-viet/${fri.id}/sua`, fd({ Content: 'Bỏ ảnh', Visibility: 'Friends', RemovePhotos: 'true' }))).body.photos === 0, 'Xoa anh khi sua bai');

  // 8. Binh luan
  const cm = async (client, token, post, content, parent) => api(client, token, 'POST', `/api/mxh/bai-viet/${post}/binh-luan`, fd({ Content: content, ParentId: parent }));
  check((await cm(a, tA, pub.id, '   ')).status === 422 && (await cm(a, tA, pub.id, 'x'.repeat(1501))).status === 400, 'Binh luan rong / qua dai bi tu choi');
  check((await cm(c, tC, fri.id, 'lén')).status === 404, 'Nguoi la khong binh luan duoc bai ban be');
  const c1 = (await cm(a, tA, pub.id, 'Bình luận của A')).body;
  check(c1.id && c1.author && c1.mine && !c1.parentId, 'Binh luan bai viet');
  const r1 = (await cm(c, tC, pub.id, 'Trả lời A', c1.id)).body;
  check(r1.parentId === c1.id, 'Tra loi binh luan');
  check((await cm(a, tA, pub.id, 'lồng sâu', r1.id)).status === 422, 'Khong tra loi long qua mot cap');
  check((await cm(a, tA, pub.id, 'sai bài', 99999999)).status === 404, 'Tra loi binh luan khong ton tai');
  const notified = json(database, `SELECT COUNT(*) N FROM dbo.ThongBao WHERE MaNguoiNhan=${B} AND LoaiThongBao='BinhLuan' FOR JSON PATH;`)[0].N;
  check(notified >= 1, 'Tac gia bai nhan thong bao co binh luan');
  const thread = (await api(b, tB, 'GET', `/api/mxh/bai-viet/${pub.id}/binh-luan`)).body;
  check(thread.length === 2 && thread.find(x => x.id === c1.id).canDelete && !thread.find(x => x.id === c1.id).mine, 'Tac gia bai xoa duoc binh luan cua nguoi khac, khong sua duoc');
  check((await api(b, tB, 'POST', `/api/mxh/binh-luan/${c1.id}/sua`, fd({ Content: 'sửa hộ' }))).status === 403, 'Khong sua binh luan cua nguoi khac');
  check((await api(a, tA, 'POST', `/api/mxh/binh-luan/${c1.id}/sua`, fd({ Content: 'A đã sửa' }))).status === 200 && (await api(a, tA, 'GET', `/api/mxh/bai-viet/${pub.id}/binh-luan`)).body.find(x => x.id === c1.id).updatedAt, 'Sua binh luan cua minh');
  check((await api(c, tC, 'POST', `/api/mxh/binh-luan/${c1.id}/xoa`)).status === 403, 'Nguoi khac khong xoa duoc binh luan');
  check((await api(a, tA, 'POST', `/api/mxh/binh-luan/${c1.id}/xoa`)).status === 200, 'Xoa binh luan cua minh');
  const after = (await api(b, tB, 'GET', `/api/mxh/bai-viet/${pub.id}/binh-luan`)).body;
  check(after.find(x => x.id === c1.id).deleted && after.find(x => x.id === c1.id).content === '' && after.some(x => x.id === r1.id), 'Binh luan co tra loi duoc giu cho trong, tra loi van hien');
  check((await api(b, tB, 'GET', '/api/mxh/bai-viet/' + pub.id)).body.comments === 1, 'So binh luan khong tinh binh luan da xoa');

  // 9. Cam xuc
  const like = async (client, token, type, target, emoji) => api(client, token, 'POST', '/api/mxh/cam-xuc', fd({ type, target, emoji }));
  const l1 = (await like(a, tA, 'Post', pub.id)).body;
  check(l1.count === 1 && l1.mine === 'like', 'Thich bai viet');
  const l2 = (await like(a, tA, 'Post', pub.id)).body;
  check(l2.count === 0 && l2.mine === null, 'Bam Thich lan nua de bo');
  check((await like(a, tA, 'Post', pub.id, 'love')).body.mine === 'love' && (await like(c, tC, 'Post', pub.id)).body.count === 2, 'Doi cam xuc va nhieu nguoi cung thich');
  check((await like(a, tA, 'Post', pub.id, 'zzz')).status === 400 && (await like(a, tA, 'Other', pub.id)).status === 400, 'Cam xuc hoac loai khong hop le bi tu choi');
  check((await like(c, tC, 'Post', fri.id)).status === 404, 'Khong thich duoc bai ban be khi khong phai ban');
  check((await like(a, tA, 'Comment', r1.id)).body.count === 1, 'Thich binh luan');
  const listed = (await api(a, tA, 'GET', '/api/mxh/bai-viet?scope=friends')).body.find(p => p.id === pub.id);
  check(listed.likes === 2 && listed.myReaction === 'love' && listed.comments === 1, 'Bang tin tra ve so luot thich, cam xuc cua toi va so binh luan');
  check(json(database, `SELECT COUNT(*) N FROM dbo.ThongBao WHERE MaNguoiNhan=${B} AND LoaiThongBao='CamXuc' FOR JSON PATH;`)[0].N >= 1, 'Tac gia nhan thong bao co cam xuc');

  // 10. Xoa bai, phan trang, gioi han
  check((await api(a, tA, 'POST', `/api/mxh/bai-viet/${pub.id}/xoa`)).status === 403, 'Khong xoa bai cua nguoi khac');
  check((await api(b, tB, 'POST', `/api/mxh/bai-viet/${pub.id}/xoa`)).status === 200 && (await api(a, tA, 'GET', '/api/mxh/bai-viet/' + pub.id)).status === 404, 'Tac gia xoa bai, nguoi khac khong thay nua');
  const values = Array.from({ length: 12 }, (_, n) => `(${B},N'Bài phân trang ${n}','Public',DATEADD(MINUTE,-${n + 2},SYSUTCDATETIME()))`).join(',');
  sql(database, `INSERT dbo.BaiViet(AuthorId,Content,Visibility,CreatedAt) VALUES ${values};`);
  const p1 = (await api(a, tA, 'GET', '/api/mxh/bai-viet?scope=friends')).body;
  check(p1.length === 10 && p1.every((p, n) => n === 0 || p.id < p1[n - 1].id), 'Trang dau 10 bai moi nhat, thu tu giam dan');
  const p2 = (await api(a, tA, 'GET', '/api/mxh/bai-viet?scope=friends&before=' + p1[9].id)).body;
  check(p2.length >= 3 && p2.every(p => p.id < p1[9].id), 'Trang tiep theo dung moc "before"');
  const rate = [];
  for (let n = 0; n < 11; n++) rate.push((await mk(c, tC, { Content: 'spam ' + n, Visibility: 'Private' })).status);
  check(rate.includes(429), 'Dang qua 10 bai moi gio bi gioi han: ' + rate.join());

  console.log(`PASS ${results.length} social checks.`);
})().catch(e => { console.error(e); process.exit(1); });
