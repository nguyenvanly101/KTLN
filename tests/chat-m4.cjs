// Tim ban dong hanh hoan chinh qua chat: the thoa thuan di cung, goi y co ly do + bo qua, gioi han loi ru, bao cao.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5225 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/chat-m4.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, base } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
assert.equal(fixture.database, database);
const user = label => fixture.users.find(x => x.label === label);
const results = [];
function check(ok, name) { assert.ok(ok, name); results.push(name); }
const cookie = c => [...c.cookies].map(([k, v]) => k + '=' + v).join('; ');
const iso = days => new Date(Date.now() + days * 86400000).toISOString().slice(0, 10);
async function api(client, token, method, path, form) {
  const r = await fetch(base + path, { method, headers: { cookie: cookie(client), ...(method !== 'GET' ? { RequestVerificationToken: token } : {}) }, body: form, redirect: 'manual' });
  let body = null; if ((r.headers.get('content-type') || '').includes('json')) body = await r.json(); else await r.arrayBuffer();
  return { status: r.status, body };
}
const fd = values => { const f = new FormData(); for (const [k, v] of Object.entries(values)) if (v != null) for (const item of Array.isArray(v) ? v : [v]) f.append(k, String(item)); return f; };
const stamp = Date.now().toString().slice(-8);

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { /* cho */ } if (n > 45) throw Error('Server not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const id = email => json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${email}' FOR JSON PATH;`)[0].Id;
  const A = id(user('traveler').email);
  const posterRow = json(database, "SELECT TOP(1) a.MaTaiKhoan Id,a.Email FROM dbo.NhuCauDongHanh n JOIN dbo.TaiKhoan a ON a.MaTaiKhoan=n.MaTaiKhoan WHERE a.VaiTro='KHACH_DU_LICH' AND a.TrangThai='HoatDong' ORDER BY n.MaNhuCau FOR JSON PATH;")[0];
  const B = posterRow.Id;
  sql(database, `DELETE FROM dbo.BaoCaoChat; DELETE FROM dbo.PhanUngTin; UPDATE dbo.TinNhanChat SET ReplyToId=NULL WHERE ReplyToId IS NOT NULL; DELETE m FROM dbo.TinNhanChat m JOIN dbo.CuocTroChuyen c ON c.Id=m.ConversationId WHERE c.BranchId IS NULL; DELETE FROM dbo.ThanhVienChat; DELETE FROM dbo.NhomChatChuyenDi WHERE ConversationId IN (SELECT Id FROM dbo.CuocTroChuyen WHERE BranchId IS NULL); DELETE FROM dbo.CuocTroChuyen WHERE BranchId IS NULL;` +
    `DELETE FROM dbo.BoQuaGoiY WHERE AccountId=${A}; DELETE FROM dbo.ThoaThuanDongHanh WHERE MaDeNghi IN (SELECT MaDeNghi FROM dbo.DeNghiKetNoiDongHanh WHERE MaNguoiGui IN (${A},${B})); DELETE FROM dbo.DeNghiKetNoiDongHanh WHERE MaNguoiGui IN (${A},${B});` +
    `DELETE FROM dbo.NhuCauDongHanh WHERE NoiDung LIKE N'QA-m4-%';`);
  const mkNeed = (acc, tag, from, to, style) => sql(database, `INSERT dbo.NhuCauDongHanh(MaTaiKhoan,NoiDung,DiemDen,NgayBatDau,NgayKetThuc,NganSachDuKien,HinhThucDi,MongMuonDongHanh,PhamViChiaSe,ThoiDiemDang,TrangThaiCongBo) VALUES(${acc},N'QA-m4-${tag}-${stamp}',N'Phú Quốc','${iso(from)}','${iso(to)}',5000000,N'${style}',N'Đi chậm','CongKhai',SYSUTCDATETIME(),'DangCongBo');`);
  mkNeed(B, 'B', 60, 65, 'Nghỉ dưỡng'); mkNeed(A, 'A', 62, 67, 'Nghỉ dưỡng');
  const needB = json(database, `SELECT TOP(1) MaNhuCau Id FROM dbo.NhuCauDongHanh WHERE NoiDung=N'QA-m4-B-${stamp}' FOR JSON PATH;`)[0].Id;

  const a = new Client(), b = new Client(), provider = new Client(), admin = new Client();
  await a.login(user('traveler').email, 'BranchTest@2026');
  try { await b.login(posterRow.Email); } catch { await b.login(posterRow.Email, 'HanhTrinhMoi@2026'); }
  await provider.login(user('provider').email, 'BranchTest@2026'); await admin.login(user('admin').email, 'BranchTest@2026');
  const tok = async (c, p = '/tin-nhan') => val(await c.get(p), '__RequestVerificationToken');
  const [tA, tB, tP] = [await tok(a), await tok(b), await tok(provider)];
  const tAdmin = await tok(admin, '/quan-tri/bao-cao-chat');

  // 1. Goi y co ly do + bo qua
  const m1 = (await api(a, tA, 'GET', '/api/mxh/phu-hop')).body.find(m => m.needId === needB);
  check(m1 && m1.reasons.length >= 2 && m1.reasons[0].includes('trùng') && m1.reasons.some(r => r.includes('phong cách')) && m1.reasons.some(r => r.includes('Ngân sách')), 'Goi y co nhieu ly do: trung ngay, cung phong cach, ngan sach gan nhau');
  check((await api(provider, tP, 'POST', '/api/mxh/phu-hop/bo-qua', fd({ needId: needB }))).status === 403, 'Nha cung cap khong dung goi y');
  check((await api(a, tA, 'POST', '/api/mxh/phu-hop/bo-qua', fd({ needId: 99999999 }))).status === 404, 'Bo qua loi ru khong ton tai bi tu choi');
  const ownNeed = json(database, `SELECT TOP(1) MaNhuCau Id FROM dbo.NhuCauDongHanh WHERE NoiDung=N'QA-m4-A-${stamp}' FOR JSON PATH;`)[0].Id;
  check((await api(a, tA, 'POST', '/api/mxh/phu-hop/bo-qua', fd({ needId: ownNeed }))).status === 404, 'Khong bo qua loi ru cua chinh minh');
  check((await api(a, tA, 'POST', '/api/mxh/phu-hop/bo-qua', fd({ needId: needB }))).status === 200 && (await api(a, tA, 'GET', '/api/mxh/phu-hop')).body.every(m => m.needId !== needB), 'Bam "Khong quan tam": goi y bien mat');
  sql(database, `DELETE FROM dbo.BoQuaGoiY WHERE AccountId=${A};`);

  // 2. Gioi han loi ru moi ngay
  const rows = Array.from({ length: 10 }, () => `(${needB},${A},SYSUTCDATETIME(),SYSUTCDATETIME(),'DaRut')`).join(',');
  sql(database, `INSERT dbo.DeNghiKetNoiDongHanh(MaNhuCau,MaNguoiGui,ThoiDiemGui,ThoiDiemPhanHoi,TrangThai) VALUES ${rows};`);
  const limited = await api(a, tA, 'POST', '/chat/ru', fd({ PeerId: B, NeedId: needB }));
  check(limited.status === 429, 'Gui qua 10 loi ru trong 24 gio bi gioi han (429)');
  sql(database, `DELETE FROM dbo.DeNghiKetNoiDongHanh WHERE MaNguoiGui=${A};`);

  // 3. Ket noi -> the thoa thuan
  const inv = await api(a, tA, 'POST', '/chat/ru', fd({ PeerId: B, NeedId: needB, Text: 'Đi Phú Quốc chung nhé' }));
  const cid = inv.body.conversationId, kid = inv.body.message.connection;
  check((await api(a, tA, 'POST', '/chat/thoa-thuan', fd({ ConversationId: cid, ConnectionId: kid, Destination: 'Phú Quốc', Start: iso(60), End: iso(65) }))).status === 422, 'Chua dong y ket noi thi chua lap duoc thoa thuan');
  await api(b, tB, 'POST', `/chat/${cid}/ket-noi/hanh-dong`, fd({ ConnectionId: kid, Decision: 'DaChapNhan' }));
  const agr = (extra = {}) => fd({ ConversationId: cid, ConnectionId: kid, Destination: 'Phú Quốc', Start: iso(60), End: iso(65), Budget: 4000000, Scope: 'Đi chung xe, chia chi phí ăn ở', ...extra });
  check((await api(a, tA, 'POST', '/chat/thoa-thuan', agr({ Start: iso(-3) }))).status >= 400, 'Ngay di trong qua khu bi tu choi');
  check((await api(a, tA, 'POST', '/chat/thoa-thuan', agr({ Destination: '' }))).status >= 400, 'Thieu diem den bi tu choi');
  check((await api(provider, tP, 'POST', '/chat/thoa-thuan', agr())).status === 403, 'Nha cung cap khong lap thoa thuan');
  const sent = await api(a, tA, 'POST', '/chat/thoa-thuan', agr());
  check(sent.status === 200 && sent.body.message.kind === 'Agreement' && sent.body.message.agreement > 0, 'Lap de xuat thoa thuan di cung ngay trong chat');
  const aid = sent.body.message.agreement;
  const cardB = (await api(b, tB, 'GET', `/chat/${cid}/thoa-thuan`)).body.find(c => c.id === aid);
  const cardA = (await api(a, tA, 'GET', `/chat/${cid}/thoa-thuan`)).body.find(c => c.id === aid);
  check(cardB.actions.join() === 'DaXacNhan,TuChoi' && cardB.budget === 4000000 && cardB.scope.includes('chia chi phí') && !cardB.mine, 'Nguoi nhan thay the voi Xac nhan / Chua phu hop');
  check(cardA.actions.length === 0 && cardA.mine, 'Nguoi de xuat khong tu xac nhan duoc');
  check((await api(a, tA, 'POST', `/chat/${cid}/thoa-thuan/hanh-dong`, fd({ AgreementId: aid, Decision: 'DaXacNhan' }))).status >= 400, 'Nguoi de xuat khong the xac nhan');
  check((await api(provider, tP, 'GET', `/chat/${cid}/thoa-thuan`)).status === 404, 'Nguoi ngoai khong xem duoc the thoa thuan');
  check((await api(b, tB, 'POST', `/chat/${cid}/thoa-thuan/hanh-dong`, fd({ AgreementId: aid, Decision: 'TuChoi' }))).status >= 400, 'Tu choi thieu ly do bi tu choi');
  check((await api(b, tB, 'POST', `/chat/${cid}/thoa-thuan/hanh-dong`, fd({ AgreementId: aid, Decision: 'TuChoi', Reason: 'Mình chỉ rảnh 4 ngày' }))).status === 200, 'Nguoi kia tu choi kem ly do');
  check((await api(a, tA, 'GET', `/chat/${cid}/thoa-thuan`)).body.find(c => c.id === aid).status === 'TuChoi', 'The cap nhat trang thai tu choi');
  const second = await api(a, tA, 'POST', '/chat/thoa-thuan', agr({ End: iso(64), Scope: 'Đi 4 ngày' }));
  check(second.status === 200, 'De xuat lai phien ban moi sau khi bi tu choi');
  const aid2 = second.body.message.agreement;
  check((await api(b, tB, 'POST', `/chat/${cid}/thoa-thuan/hanh-dong`, fd({ AgreementId: aid2, Decision: 'DaXacNhan' }))).status === 200, 'Nguoi kia xac nhan thoa thuan');
  const thread = (await api(a, tA, 'GET', '/chat/' + cid)).body;
  check(thread.agreements.find(c => c.id === aid2).status === 'DaXacNhan' && thread.agreements.find(c => c.id === aid2).actions.join() === 'Rut', 'Thoa thuan da xac nhan: ca hai chi con "Ket thuc thoa thuan"');
  check(thread.messages.some(m => m.kind === 'Event' && m.body.includes('đã được xác nhận')), 'Co dong su kien xac nhan thoa thuan');
  check((await api(a, tA, 'POST', `/chat/${cid}/thoa-thuan/hanh-dong`, fd({ AgreementId: aid2, Decision: 'Rut' }))).status === 200 && (await api(b, tB, 'GET', `/chat/${cid}/thoa-thuan`)).body.find(c => c.id === aid2).status === 'MotBenDaRut', 'Ket thuc thoa thuan');
  check(json(database, `SELECT COUNT(*) N FROM dbo.ThoaThuanDongHanh WHERE MaDeNghi=${kid} FOR JSON PATH;`)[0].N === 2, 'Du lieu thoa thuan di vao bang ThoaThuanDongHanh co san (2 phien ban)');

  // 4. Bao cao
  const bMsg = (await api(b, tB, 'POST', '/chat/gui', fd({ ConversationId: cid, Body: 'Cho mình xin số tài khoản để chuyển trước nhé' }))).body.message.id;
  const aMsg = (await api(a, tA, 'POST', '/chat/gui', fd({ ConversationId: cid, Body: 'tin của A' }))).body.message.id;
  const rep = (c, t, extra) => api(c, t, 'POST', `/chat/${cid}/bao-cao`, fd(extra));
  check((await rep(a, tA, { Reason: 'Zzz', MessageId: bMsg })).status === 422, 'Ly do bao cao khong hop le bi tu choi');
  check((await rep(a, tA, { Reason: 'LuaDao', MessageId: aMsg })).status === 422, 'Khong bao cao tin cua chinh minh');
  check((await rep(provider, tP, { Reason: 'Spam', MessageId: bMsg })).status === 404, 'Nguoi ngoai cuoc tro chuyen khong bao cao duoc');
  check((await rep(a, tA, { Reason: 'LuaDao', MessageId: bMsg, Note: 'Đòi chuyển khoản trước' })).status === 200, 'Bao cao mot tin nhan');
  check((await rep(a, tA, { Reason: 'LuaDao', MessageId: bMsg })).status === 409, 'Bao cao trung khi dang cho xu ly bi tu choi');
  check((await rep(a, tA, { Reason: 'QuayRoi' })).status === 200, 'Bao cao ca nguoi dung (khong kem tin)');
  const page = await admin.get('/quan-tri/bao-cao-chat');
  check(page.status === 200 && page.text.includes('LuaDao') && page.text.includes('số tài khoản') && page.text.includes('Đòi chuyển khoản trước'), 'Quan tri thay hang cho: ly do, ghi chu va tin bi bao cao kem ngu canh');
  check((await a.get('/quan-tri/bao-cao-chat')).status === 403 && (await provider.get('/quan-tri/bao-cao-chat')).status === 403, 'Chi quan tri xem hang cho bao cao');
  const reportId = json(database, "SELECT TOP(1) Id FROM dbo.BaoCaoChat WHERE Reason='LuaDao' ORDER BY Id DESC FOR JSON PATH;")[0].Id;
  const closed = await admin.post('/quan-tri/bao-cao-chat/xu-ly', page, { id: reportId });
  check(closed.status === 302 && json(database, `SELECT Status S FROM dbo.BaoCaoChat WHERE Id=${reportId} FOR JSON PATH;`)[0].S === 'Done', 'Quan tri danh dau da xu ly');
  check(!(await admin.get('/quan-tri/bao-cao-chat')).text.includes('Đòi chuyển khoản trước') && (await admin.get('/quan-tri/bao-cao-chat?status=Done')).text.includes('Đòi chuyển khoản trước'), 'Bao cao da xu ly chuyen sang tab "Da xu ly"');

  console.log(`PASS ${results.length} chat-m4 checks.`);
})().catch(e => { console.error(e); process.exit(1); });
