// Tin nhan giua hai khach du lich (tim ban dong hanh): mo tu loi ru, nhan tin hai chieu thoi gian thuc, da xem, chan / bo chan, quyen truy cap.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5225 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/chat-people.cjs
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

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { /* cho */ } if (n > 45) throw Error('Server not ready'); await sleep(1000); }
  const poster = json(database, "SELECT TOP(1) n.MaTaiKhoan Id,a.Email FROM dbo.NhuCauDongHanh n JOIN dbo.TaiKhoan a ON a.MaTaiKhoan=n.MaTaiKhoan WHERE n.TrangThaiCongBo='DangCongBo' AND a.VaiTro='KHACH_DU_LICH' AND a.TrangThai='HoatDong' ORDER BY n.MaNhuCau FOR JSON PATH;")[0];
  assert.ok(poster, 'Can mot khach co loi ru cong khai');
  const me = json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}' FOR JSON PATH;`)[0].Id;
  const providerAccount = json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${user('provider').email}' FOR JSON PATH;`)[0].Id;
  sql(database, `DELETE FROM dbo.BaoCaoChat; DELETE FROM dbo.PhanUngTin; UPDATE dbo.TinNhanChat SET ReplyToId=NULL WHERE ReplyToId IS NOT NULL; DELETE m FROM dbo.TinNhanChat m JOIN dbo.CuocTroChuyen c ON c.Id=m.ConversationId WHERE c.BranchId IS NULL; DELETE FROM dbo.ThanhVienChat; DELETE FROM dbo.NhomChatChuyenDi WHERE ConversationId IN (SELECT Id FROM dbo.CuocTroChuyen WHERE BranchId IS NULL); DELETE FROM dbo.CuocTroChuyen WHERE BranchId IS NULL;`);

  const traveler = new Client(), friend = new Client(), provider = new Client(), guest = new Client();
  await traveler.login(user('traveler').email, 'BranchTest@2026');
  try { await friend.login(poster.Email); } catch { await friend.login(poster.Email, 'HanhTrinhMoi@2026'); }
  await provider.login(user('provider').email, 'BranchTest@2026');
  const tokenT = val(await traveler.get('/tin-nhan'), '__RequestVerificationToken');
  const tokenF = val(await friend.get('/tin-nhan'), '__RequestVerificationToken');
  const tokenP = val(await provider.get('/tin-nhan'), '__RequestVerificationToken');

  // 1. Mot hop thu chung cho moi vai tro
  check((await provider.get('/tin-nhan')).status === 200, 'Nha cung cap cung dung hop thu /tin-nhan');
  check((await traveler.get('/tin-nhan')).status === 200 && (await friend.get('/tin-nhan')).status === 200, 'Khach dung hop thu /tin-nhan');

  // 2. Mo tu loi ru di cung
  const detail = await traveler.get('/dong-hanh/tim-kiem/xem-chi-tiet/' + json(database, `SELECT TOP(1) MaNhuCau Id FROM dbo.NhuCauDongHanh WHERE MaTaiKhoan=${poster.Id} AND TrangThaiCongBo='DangCongBo' FOR JSON PATH;`)[0].Id);
  check(detail.status === 200 && detail.html.includes(`/tin-nhan/nguoi/${poster.Id}`), 'Trang loi ru co nut "Nhan tin"');
  const start = await traveler.get('/tin-nhan/nguoi/' + poster.Id);
  check(start.status === 200 && start.html.includes('id="chat-start"') && start.html.includes('"peerId":' + poster.Id), 'Mo khung nhan tin voi nguoi dang loi ru');
  check((await traveler.get('/tin-nhan/nguoi/' + me)).status === 422, 'Khong nhan tin cho chinh minh');
  check((await traveler.get('/tin-nhan/nguoi/' + providerAccount)).status === 404, 'Khong nhan tin truc tiep toi tai khoan nha cung cap');
  check((await traveler.get('/tin-nhan/nguoi/99999999')).status === 404, 'Nguoi khong ton tai');
  check((await provider.get('/tin-nhan/nguoi/' + poster.Id)).status === 403, 'Nha cung cap khong bat dau cuoc tro chuyen voi khach');
  check([302, 401].includes((await guest.get('/tin-nhan/nguoi/' + poster.Id)).status), 'Khach vang lai bi chan');
  check((await api(traveler, tokenT, 'POST', '/chat/gui', fd({ PeerId: me, Body: 'x' }))).status === 422, 'Gui cho chinh minh bi tu choi');
  check((await api(traveler, tokenT, 'POST', '/chat/gui', fd({ PeerId: providerAccount, Body: 'x' }))).status === 404, 'Gui toi nha cung cap bang PeerId bi tu choi');
  check((await api(traveler, tokenT, 'POST', '/chat/gui', fd({ PeerId: poster.Id, Body: 'a https://a.vn b https://b.vn c https://c.vn' }))).status === 422, 'Qua nhieu lien ket bi tu choi');

  // 3. Nhan tin hai chieu thoi gian thuc
  const sF = stream(friend), sT = stream(traveler); await sleep(500);
  const first = await api(traveler, tokenT, 'POST', '/chat/gui', fd({ PeerId: poster.Id, Body: 'Chào bạn, mình cũng đi Đà Lạt tháng này, mình đi cùng được không?' }));
  check(first.status === 200 && first.body.conversationId > 0, 'Gui tin dau tien tao cuoc tro chuyen giua hai nguoi');
  const cid = first.body.conversationId;
  await waitFor(() => sF.events.find(e => e.t === 'msg' && e.c === cid), 'ban nhan tin ngay');
  check(true, 'Nguoi kia nhan tin ngay qua luong su kien');
  check((await traveler.get('/tin-nhan/nguoi/' + poster.Id)).location === '/tin-nhan/' + cid, 'Mo lai tu loi ru chuyen vao cuoc tro chuyen cu');
  const flist = (await api(friend, tokenF, 'GET', '/chat/danh-sach')).body.find(x => x.id === cid);
  check(flist && flist.unread === 1 && flist.branchId === 0 && !flist.who.includes('@'), 'Nguoi kia thay 1 tin chua doc, khong co co so, khong lo email');
  check((await api(friend, tokenF, 'GET', '/chat/chua-doc')).body.count >= 1, 'So tin chua doc cua nguoi kia');
  const ft = (await api(friend, tokenF, 'GET', '/chat/' + cid)).body;
  check(ft.me === 'Branch' && ft.messages.length === 1 && ft.item.branchId === 0 && ft.cards.length === 0, 'Nguoi kia mo cuoc tro chuyen o phia thu hai');
  await api(friend, tokenF, 'POST', `/chat/${cid}/da-xem`);
  await waitFor(() => sT.events.find(e => e.t === 'read' && e.c === cid && e.side === 'Branch'), 'nguoi gui thay da xem');
  check(true, 'Nguoi gui thay "da xem"');
  await api(friend, tokenF, 'POST', `/chat/${cid}/dang-soan`);
  await waitFor(() => sT.events.find(e => e.t === 'typing' && e.c === cid), 'dang soan');
  check(true, 'Nguoi gui thay "dang soan tin"');
  const reply = await api(friend, tokenF, 'POST', '/chat/gui', fd({ ConversationId: cid, Body: 'Được chứ! Mình đi ngày 12, bạn đi hôm nào?' }));
  check(reply.status === 200 && reply.body.message.side === 'Branch', 'Nguoi kia tra loi');
  await waitFor(() => sT.events.find(e => e.t === 'msg' && e.m === reply.body.message.id), 'nhan tra loi');
  const tt = (await api(traveler, tokenT, 'GET', '/chat/' + cid)).body;
  check(tt.me === 'Traveler' && tt.messages.length === 2 && tt.item.branchId === 0, 'Nguoi bat dau thay du hai tin');

  // 4. Quyen truy cap va gioi han
  check((await api(provider, tokenP, 'GET', '/chat/' + cid)).status === 404, 'Nha cung cap khong doc duoc cuoc tro chuyen nguoi voi nguoi');
  check((await api(provider, tokenP, 'POST', '/chat/gui', fd({ ConversationId: cid, Body: 'chen' }))).status === 404, 'Nguoi ngoai khong gui xen vao');
  check((await api(provider, tokenP, 'GET', '/chat/danh-sach')).body.every(x => x.id !== cid), 'Hop thu nha cung cap khong lan cuoc tro chuyen cua nguoi khac');
  check((await api(traveler, tokenT, 'POST', '/chat/dat', fd({ ConversationId: cid, ServiceId: 1, Start: '2099-01-01', People: 1, Quantity: 1 }))).status === 403, 'Khong dat dich vu trong cuoc tro chuyen nguoi voi nguoi');
  check((await api(traveler, tokenT, 'POST', `/chat/${cid}/trang-thai`, fd({ State: 'Done' }))).status === 403, 'Trang thai xu ly chi danh cho co so');

  // 5. Chan / bo chan
  check((await api(friend, tokenF, 'POST', `/chat/${cid}/chan`, fd({ block: 'true' }))).status === 200, 'Chan nguoi kia');
  check((await api(traveler, tokenT, 'POST', '/chat/gui', fd({ ConversationId: cid, Body: 'còn đó không?' }))).status === 409, 'Nguoi bi chan khong gui them duoc');
  check((await api(friend, tokenF, 'POST', '/chat/gui', fd({ ConversationId: cid, Body: 'tôi cũng không gửi được' }))).status === 409, 'Nguoi chan cung khong gui khi dang chan');
  const bt = (await api(traveler, tokenT, 'GET', '/chat/' + cid)).body;
  check(!bt.canSend && bt.blocked && !bt.blockedByMe, 'Nguoi bi chan thay ly do, khong thay nut bo chan');
  check((await api(traveler, tokenT, 'POST', `/chat/${cid}/chan`, fd({ block: 'false' }))).status === 403, 'Nguoi bi chan khong tu bo chan');
  check((await api(traveler, tokenT, 'GET', `/chat/${cid}/tin`)).status === 200, 'Van xem lai lich su khi bi chan');
  check((await api(friend, tokenF, 'POST', `/chat/${cid}/chan`, fd({ block: 'false' }))).status === 200, 'Bo chan');
  check((await api(traveler, tokenT, 'POST', '/chat/gui', fd({ ConversationId: cid, Body: 'Cảm ơn bạn!' }))).status === 200, 'Sau khi bo chan nhan tin lai binh thuong');

  // 6. Anh trong cuoc tro chuyen nguoi voi nguoi
  const form = fd({ ConversationId: cid }); form.append('Image', new Blob([Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==', 'base64')], { type: 'image/png' }), 'a.png');
  const img = await api(traveler, tokenT, 'POST', '/chat/gui', form);
  check(img.status === 200 && img.body.message.image, 'Gui anh cho nguoi kia');
  const getImg = async c => fetch(base + `/chat/${cid}/anh/${img.body.message.id}`, { headers: { cookie: cookie(c) }, redirect: 'manual' });
  check((await getImg(friend)).status === 200 && (await getImg(provider)).status === 404, 'Chi hai ben xem duoc anh');

  sT.close(); sF.close(); await Promise.all([sT.done, sF.done]);
  console.log(`PASS ${results.length} chat-people checks.`);
})().catch(e => { console.error(e); process.exit(1); });
