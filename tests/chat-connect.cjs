// Ket noi dong hanh ngay trong chat: khach "ru di cung" bang the trong cuoc tro chuyen, nguoi kia dong y / tu choi, nguoi ru co the rut,
// dung quy trinh ket noi dong hanh san co. Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5225 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/chat-connect.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, base } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
assert.equal(fixture.database, database);
const user = label => fixture.users.find(x => x.label === label);
const results = [];
function check(ok, name) { assert.ok(ok, name); results.push(name); }
const sleep = ms => new Promise(r => setTimeout(r, ms));
const cookie = c => [...c.cookies].map(([k, v]) => k + '=' + v).join('; ');
async function api(client, token, method, path, form) {
  const r = await fetch(base + path, { method, headers: { cookie: cookie(client), ...(method !== 'GET' ? { RequestVerificationToken: token } : {}) }, body: form, redirect: 'manual' });
  let body = null; if ((r.headers.get('content-type') || '').includes('json')) body = await r.json(); else await r.arrayBuffer();
  return { status: r.status, body };
}
const fd = values => { const f = new FormData(); for (const [k, v] of Object.entries(values)) if (v != null) f.append(k, String(v)); return f; };
function stream(client) {
  const controller = new AbortController(), events = [];
  const done = (async () => {
    const r = await fetch(base + '/chat/luong', { headers: { cookie: cookie(client) }, signal: controller.signal });
    const reader = r.body.getReader(), dec = new TextDecoder(); let buf = '';
    try { for (;;) { const { value, done } = await reader.read(); if (done) break; buf += dec.decode(value, { stream: true }); let i; while ((i = buf.indexOf('\n\n')) >= 0) { const block = buf.slice(0, i); buf = buf.slice(i + 2); const line = block.split('\n').find(l => l.startsWith('data: ')); if (line) events.push(JSON.parse(line.slice(6))); } } } catch { /* da dong */ }
  })();
  return { events, close: () => controller.abort(), done };
}
async function waitFor(fn, name, ms = 6000) { const end = Date.now() + ms; while (Date.now() < end) { const v = fn(); if (v) return v; await sleep(100); } assert.fail('Het thoi gian cho: ' + name); }
const conn = id => json(database, `SELECT TrangThai FROM dbo.DeNghiKetNoiDongHanh WHERE MaDeNghi=${id} FOR JSON PATH;`)[0];

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { /* cho */ } if (n > 45) throw Error('Server not ready'); await sleep(1000); }
  const poster = json(database, "SELECT TOP(1) n.MaTaiKhoan Id,n.MaNhuCau NeedId,a.Email FROM dbo.NhuCauDongHanh n JOIN dbo.TaiKhoan a ON a.MaTaiKhoan=n.MaTaiKhoan WHERE n.TrangThaiCongBo='DangCongBo' AND n.PhamViChiaSe='CongKhai' AND a.VaiTro='KHACH_DU_LICH' AND a.TrangThai='HoatDong' ORDER BY n.MaNhuCau FOR JSON PATH;")[0];
  assert.ok(poster, 'Can mot loi ru cong khai');
  const me = json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}' FOR JSON PATH;`)[0].Id;
  sql(database, `DELETE FROM dbo.BaoCaoChat; DELETE FROM dbo.PhanUngTin; UPDATE dbo.TinNhanChat SET ReplyToId=NULL WHERE ReplyToId IS NOT NULL; DELETE m FROM dbo.TinNhanChat m JOIN dbo.CuocTroChuyen c ON c.Id=m.ConversationId WHERE c.BranchId IS NULL; DELETE FROM dbo.ThanhVienChat; DELETE FROM dbo.NhomChatChuyenDi WHERE ConversationId IN (SELECT Id FROM dbo.CuocTroChuyen WHERE BranchId IS NULL); DELETE FROM dbo.CuocTroChuyen WHERE BranchId IS NULL; UPDATE dbo.DeNghiKetNoiDongHanh SET TrangThai='DaRut' WHERE MaNguoiGui=${me} AND MaNhuCau=${poster.NeedId} AND TrangThai IN ('ChoPhanHoi','DaChapNhan');`);

  const traveler = new Client(), friend = new Client(), provider = new Client();
  await traveler.login(user('traveler').email, 'BranchTest@2026');
  try { await friend.login(poster.Email); } catch { await friend.login(poster.Email, 'HanhTrinhMoi@2026'); }
  await provider.login(user('provider').email, 'BranchTest@2026');
  const tokenT = val(await traveler.get('/tin-nhan'), '__RequestVerificationToken');
  const tokenF = val(await friend.get('/tin-nhan'), '__RequestVerificationToken');
  const tokenP = val(await provider.get('/tin-nhan'), '__RequestVerificationToken');

  // 1. Loi vao tu trang loi ru
  const page = await traveler.get(`/tin-nhan/nguoi/${poster.Id}?nhuCau=${poster.NeedId}`);
  check(page.status === 200 && page.html.includes('data-chat-connect') && page.html.includes('id="chat-start"'), 'Khung chat co form "Ru di cung"');
  const needs = await api(traveler, tokenT, 'GET', `/chat/nguoi/${poster.Id}/loi-ru`);
  check(needs.status === 200 && needs.body.some(n => n.id === poster.NeedId), 'Danh sach loi ru cua nguoi kia hien de chon');
  check((await api(provider, tokenP, 'GET', `/chat/nguoi/${poster.Id}/loi-ru`)).status === 403, 'Nha cung cap khong gui loi ru');

  // 2. Gui loi ru
  const sF = stream(friend), sT = stream(traveler); await sleep(500);
  check((await api(traveler, tokenT, 'POST', '/chat/ru', fd({ PeerId: poster.Id, NeedId: 99999999 }))).status === 422, 'Loi ru khong thuoc nguoi dang tro chuyen bi tu choi');
  const sent = await api(traveler, tokenT, 'POST', '/chat/ru', fd({ PeerId: poster.Id, NeedId: poster.NeedId, Text: 'Mình hay đi chậm, thích cà phê sáng. Đi cùng nhé?' }));
  check(sent.status === 200 && sent.body.message.kind === 'Connection' && sent.body.message.connection > 0, 'Khach gui the loi ru trong chat: ' + JSON.stringify(sent.body));
  const cid = sent.body.conversationId, kid = sent.body.message.connection;
  check(conn(kid).TrangThai === 'ChoPhanHoi', 'De nghi ket noi duoc tao o trang thai cho phan hoi');
  await waitFor(() => sF.events.find(e => e.t === 'msg' && e.c === cid), 'nguoi kia nhan the');
  check(true, 'Nguoi kia nhan the loi ru ngay');
  check((await api(traveler, tokenT, 'POST', '/chat/ru', fd({ ConversationId: cid, NeedId: poster.NeedId }))).status >= 400, 'Gui trung loi ru dang cho bi tu choi');
  check((await api(traveler, tokenT, 'GET', `/chat/nguoi/${poster.Id}/loi-ru`)).body.every(n => n.id !== poster.NeedId), 'Loi ru da gui khong con trong danh sach chon');

  // 3. The va hanh dong theo vai tro
  const ft = (await api(friend, tokenF, 'GET', '/chat/' + cid)).body;
  const fc = ft.connections.find(c => c.id === kid);
  check(fc && fc.actions.join() === 'DaChapNhan,TuChoi' && !fc.mine && fc.intro.includes('cà phê') && fc.destination, 'Nguoi nhan thay the voi Dong y / Tu choi');
  const tc = (await api(traveler, tokenT, 'GET', `/chat/${cid}/ket-noi`)).body.find(c => c.id === kid);
  check(tc && tc.actions.join() === 'DaRut' && tc.mine, 'Nguoi ru chi co the rut');
  check((await api(provider, tokenP, 'GET', `/chat/${cid}/ket-noi`)).status === 404, 'Nguoi ngoai khong xem duoc the');
  check((await api(traveler, tokenT, 'POST', `/chat/${cid}/ket-noi/hanh-dong`, fd({ ConnectionId: kid, Decision: 'DaChapNhan' }))).status === 403, 'Nguoi ru khong tu dong y');
  check((await api(friend, tokenF, 'POST', `/chat/${cid}/ket-noi/hanh-dong`, fd({ ConnectionId: kid, Decision: 'DaRut' }))).status === 403, 'Nguoi nhan khong the "rut"');

  // 4. Hai ben nhan tin roi dong y
  await api(friend, tokenF, 'POST', '/chat/gui', fd({ ConversationId: cid, Body: 'Nghe hợp lý, mình thích đi chậm. Bạn đi mấy ngày?' }));
  const agree = await api(friend, tokenF, 'POST', `/chat/${cid}/ket-noi/hanh-dong`, fd({ ConnectionId: kid, Decision: 'DaChapNhan' }));
  check(agree.status === 200 && conn(kid).TrangThai === 'DaChapNhan', 'Nguoi kia dong y di cung: ket noi duoc chap nhan');
  await waitFor(() => sT.events.filter(e => e.t === 'msg' && e.c === cid).length >= 3, 'nguoi ru nhan su kien dong y');
  const tt = (await api(traveler, tokenT, 'GET', '/chat/' + cid)).body;
  const c2 = tt.connections.find(c => c.id === kid);
  check(c2.status === 'DaChapNhan' && c2.actions.length === 0 && c2.link === '/dong-hanh/de-nghi/xem-de-nghi/' + kid, 'The cho nguoi ru hien da ket noi kem duong dan lap thoa thuan');
  check(tt.messages.some(m => m.kind === 'Event' && m.connection === kid && m.body.includes('Đã kết nối')), 'Co dong su kien da ket noi trong chat');
  const agreement = await traveler.get(c2.link);
  check(agreement.status === 200, 'Trang ket noi / thoa thuan dong hanh mo duoc tu the');
  check((await api(traveler, tokenT, 'GET', `/chat/nguoi/${poster.Id}/loi-ru`)).body.every(n => n.id !== poster.NeedId), 'Loi ru da ket noi khong gui lai duoc');
  check((await api(friend, tokenF, 'POST', `/chat/${cid}/ket-noi/hanh-dong`, fd({ ConnectionId: kid, Decision: 'TuChoi' }))).status >= 400, 'Khong doi y sau khi da dong y');

  // 5. Tu choi va rut (dua de nghi cu ve trang thai da rut de gui lai)
  sql(database, `UPDATE dbo.DeNghiKetNoiDongHanh SET TrangThai='DaRut' WHERE MaDeNghi=${kid};`);
  const second = await api(traveler, tokenT, 'POST', '/chat/ru', fd({ ConversationId: cid, NeedId: poster.NeedId, Text: 'Gửi lại nhé' }));
  check(second.status === 200, 'Gui lai loi ru sau khi de nghi cu ket thuc');
  const k2 = second.body.message.connection;
  check((await api(friend, tokenF, 'POST', `/chat/${cid}/ket-noi/hanh-dong`, fd({ ConnectionId: k2, Decision: 'TuChoi' }))).status === 200 && conn(k2).TrangThai === 'TuChoi', 'Nguoi kia tu choi');
  const third = await api(traveler, tokenT, 'POST', '/chat/ru', fd({ ConversationId: cid, NeedId: poster.NeedId }));
  const k3 = third.body.message.connection;
  check((await api(traveler, tokenT, 'POST', `/chat/${cid}/ket-noi/hanh-dong`, fd({ ConnectionId: k3, Decision: 'DaRut' }))).status === 200 && conn(k3).TrangThai === 'DaRut', 'Nguoi ru rut loi ru');
  const list = (await api(friend, tokenF, 'GET', '/chat/danh-sach')).body.find(x => x.id === cid);
  check(list && /Đi cùng|Rủ đi cùng/.test(list.preview || ''), 'Hop thu hien xem truoc cua the ket noi');

  sT.close(); sF.close(); await Promise.all([sT.done, sF.done]);
  console.log(`PASS ${results.length} chat-connect checks.`);
})().catch(e => { console.error(e); process.exit(1); });
