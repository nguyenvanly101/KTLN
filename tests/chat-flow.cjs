// Kiem thu tin nhan khach <-> co so: quyen truy cap, gui/nhan thoi gian thuc (SSE), da xem, dang soan, anh, tin mau,
// tra loi tu dong, tam ngung nhan tin, gioi han toc do.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5225 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/chat-flow.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, base } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
assert.equal(fixture.database, database);
const user = label => fixture.users.find(x => x.label === label);
const results = [];
function check(ok, name) { assert.ok(ok, name); results.push(name); }
const sleep = ms => new Promise(r => setTimeout(r, ms));
const PNG = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==', 'base64');
const cookie = c => [...c.cookies].map(([k, v]) => k + '=' + v).join('; ');

async function api(client, token, method, path, form) {
  const r = await fetch(base + path, { method, headers: { cookie: cookie(client), ...(method !== 'GET' ? { RequestVerificationToken: token } : {}) }, body: form, redirect: 'manual' });
  let body = null; const type = r.headers.get('content-type') || '';
  if (type.includes('json')) body = await r.json(); else await r.arrayBuffer();
  return { status: r.status, body, type };
}
function fd(values, file) {
  const f = new FormData();
  for (const [k, v] of Object.entries(values)) if (v != null) f.append(k, String(v));
  if (file) f.append('Image', new Blob([PNG], { type: 'image/png' }), 'anh.png');
  return f;
}
/** Mo luong su kien cua mot tai khoan va thu cac su kien den. */
function stream(client) {
  const controller = new AbortController(), events = [];
  const done = (async () => {
    const r = await fetch(base + '/chat/luong', { headers: { cookie: cookie(client) }, signal: controller.signal });
    assert.equal(r.status, 200); assert.ok((r.headers.get('content-type') || '').startsWith('text/event-stream'));
    const reader = r.body.getReader(), dec = new TextDecoder(); let buf = '';
    try { for (;;) { const { value, done } = await reader.read(); if (done) break; buf += dec.decode(value, { stream: true }); let i; while ((i = buf.indexOf('\n\n')) >= 0) { const block = buf.slice(0, i); buf = buf.slice(i + 2); const line = block.split('\n').find(l => l.startsWith('data: ')); if (line) events.push(JSON.parse(line.slice(6))); } } } catch { /* da dong */ }
  })();
  return { events, close: () => controller.abort(), done };
}
async function waitFor(fn, name, ms = 6000) { const end = Date.now() + ms; while (Date.now() < end) { const v = fn(); if (v) return v; await sleep(100); } assert.fail('Het thoi gian cho: ' + name); }

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { /* cho may chu */ } if (n > 45) throw Error('Server not ready'); await sleep(1000); }
  const providerEmail = user('provider').email, travelerEmail = user('traveler').email;
  const row = json(database, `SELECT TOP(1) b.Id,b.Name FROM dbo.CoSoDichVu b JOIN dbo.NhaCungCapDichVu p ON p.MaNhaCungCap=b.ProviderId JOIN dbo.TaiKhoan a ON a.MaTaiKhoan=p.MaTaiKhoan WHERE a.Email=N'${providerEmail}' AND b.Status='Approved' AND b.PublishedVersion IS NOT NULL ORDER BY b.Id FOR JSON PATH;`)[0];
  assert.ok(row, 'Can mot co so da duyet cua nha cung cap thu');
  const branchId = row.Id;
  // dam bao khong con du lieu chat cu cua cap nay
  sql(database, `DELETE FROM dbo.BaoCaoChat; DELETE FROM dbo.PhanUngTin; UPDATE dbo.TinNhanChat SET ReplyToId=NULL WHERE ReplyToId IS NOT NULL; DELETE m FROM dbo.TinNhanChat m JOIN dbo.CuocTroChuyen c ON c.Id=m.ConversationId WHERE c.BranchId=${branchId}; DELETE FROM dbo.NhomChatChuyenDi WHERE ConversationId IN (SELECT Id FROM dbo.CuocTroChuyen WHERE BranchId=${branchId}); DELETE FROM dbo.CuocTroChuyen WHERE BranchId=${branchId}; DELETE FROM dbo.TinMauChat WHERE BranchId=${branchId}; DELETE FROM dbo.CaiDatChatCoSo WHERE BranchId=${branchId};`);

  const guest = new Client(), traveler = new Client(), provider = new Client(), other = new Client();
  await traveler.login(travelerEmail, 'BranchTest@2026');
  await provider.login(providerEmail, 'BranchTest@2026');
  { const pg = await provider.get('/bang-tin'); await provider.post('/nha-cung-cap/co-so/chon', pg, { id: branchId, returnUrl: '/bang-tin' }); } // nha cung cap chi thay co so dang chon
  await other.login(user('other').email, 'BranchTest@2026');

  // 1. Quyen truy cap
  for (const path of ['/chat/danh-sach', '/chat/chua-doc', '/tin-nhan', '/nha-cung-cap/tin-nhan'])
    check([302, 401].includes((await guest.get(path)).status), 'Khach chua dang nhap bi chan: ' + path);
  check((await traveler.get('/nha-cung-cap/tin-nhan')).status === 403, 'Khach du lich khong vao hop thu co so');
  check((await provider.get('/tin-nhan')).status === 200, 'Nha cung cap cung dung hop thu chung /tin-nhan');

  // 2. Trang co so co nut nhan tin va thoi gian tra loi
  const pub = await guest.get('/co-so/' + branchId);
  check(pub.status === 200 && pub.html.includes('data-branch-chat') && pub.text.includes('Thường trả lời'), 'Trang co so co nut "Nhan tin" va thoi gian tra loi');
  check((await provider.get('/co-so/' + branchId)).html.includes('data-branch-chat') === false, 'Nha cung cap khong thay nut nhan tin tren trang co so');

  // 3. Khach mo khung nhan tin tu trang co so
  const inbox = await traveler.get('/tin-nhan/co-so/' + branchId);
  check(inbox.status === 200 && inbox.html.includes('id="chat-start"') && inbox.html.includes('data-chat-app'), 'Mo khung nhan tin moi tu trang co so');
  const tokenT = val(inbox, '__RequestVerificationToken');
  const pInbox = await provider.get('/nha-cung-cap/tin-nhan');
  check(pInbox.status === 200 && pInbox.html.includes('data-chat-list'), 'Hop thu co so mo duoc');
  const tokenP = val(pInbox, '__RequestVerificationToken');
  const tokenO = val(await other.get('/nha-cung-cap/tin-nhan'), '__RequestVerificationToken');
  const startInfo = await api(traveler, tokenT, 'GET', '/chat/co-so/' + branchId);
  check(startInfo.status === 200 && startInfo.body.conversationId == null && startInfo.body.accepting, 'Thong tin bat dau: chua co cuoc tro chuyen');
  check((await api(traveler, tokenT, 'GET', '/chat/danh-sach')).body.filter(x => x.branchId === branchId).length === 0, 'Chua co cuoc tro chuyen nao khi chua gui tin');

  // 4. Kiem tra du lieu gui
  check((await api(traveler, tokenT, 'POST', '/chat/gui', fd({ BranchId: branchId, Body: '   ' }))).status === 422, 'Tin nhan rong bi tu choi');
  check((await api(traveler, tokenT, 'POST', '/chat/gui', fd({ BranchId: branchId, Body: 'x'.repeat(2001) }))).status === 400, 'Tin nhan qua 2000 ky tu bi tu choi');
  check((await api(traveler, tokenT, 'POST', '/chat/gui', fd({ BranchId: branchId, Body: 'a https://a.vn b https://b.vn c https://c.vn' }))).status === 422, 'Qua nhieu lien ket bi tu choi');
  check((await api(traveler, null, 'POST', '/chat/gui', fd({ BranchId: branchId, Body: 'khong co token' }))).status === 400, 'Thieu ma chong gia mao bi tu choi');
  check((await api(provider, tokenP, 'POST', '/chat/gui', fd({ BranchId: branchId, Body: 'toi nhan chinh toi' }))).status === 403, 'Nha cung cap khong tu bat dau cuoc tro chuyen');

  // 5. Gui tin dau tien va nhan thoi gian thuc
  const sP = stream(provider), sT = stream(traveler);
  await sleep(500);
  const first = await api(traveler, tokenT, 'POST', '/chat/gui', fd({ BranchId: branchId, Body: 'Chào quán, tối nay còn bàn cho 4 người không ạ?' }));
  check(first.status === 200 && first.body.conversationId > 0 && first.body.message.side === 'Traveler', 'Khach gui tin dau tien tao cuoc tro chuyen');
  const cid = first.body.conversationId;
  await waitFor(() => sP.events.find(e => e.t === 'msg' && e.c === cid), 'co so nhan su kien tin moi');
  check(true, 'Co so nhan tin ngay qua luong su kien');
  check(json(database, `SELECT COUNT(*) n FROM dbo.CuocTroChuyen WHERE BranchId=${branchId} FOR JSON PATH;`)[0].n === 1, 'Chi co mot cuoc tro chuyen cho moi cap khach-co so');
  const again = await api(traveler, tokenT, 'POST', '/chat/gui', fd({ BranchId: branchId, Body: 'Mình đi lúc 19:00.' }));
  check(again.status === 200 && again.body.conversationId === cid, 'Gui tiep dung cuoc tro chuyen cu');
  check(json(database, `SELECT COUNT(*) n FROM dbo.CuocTroChuyen WHERE BranchId=${branchId} FOR JSON PATH;`)[0].n === 1, 'Khong tao cuoc tro chuyen thu hai');
  check((await traveler.get('/tin-nhan/co-so/' + branchId)).location === '/tin-nhan/' + cid, 'Mo trang co so lan sau chuyen thang vao cuoc tro chuyen');

  // 6. Hop thu co so: chua doc, quyen truy cap
  const plist = (await api(provider, tokenP, 'GET', '/chat/danh-sach')).body.filter(x => x.branchId === branchId);
  check(plist.length === 1 && plist[0].unread === 2 && plist[0].branchId === branchId, 'Hop thu co so thay 1 cuoc tro chuyen, 2 tin chua doc');
  check((await api(provider, tokenP, 'GET', '/chat/danh-sach')).body.reduce((n, x) => n + (x.branchId === branchId ? x.unread : 0), 0) === 2, 'Tong tin chua doc cua co so = 2');
  check(!plist[0].who.includes('@'), 'Khong lo email cua khach');
  check((await api(other, tokenP, 'GET', '/chat/' + cid)).status === 404, 'Nha cung cap khac khong doc duoc cuoc tro chuyen');
  check((await api(other, tokenP, 'GET', '/chat/' + cid + '/tin')).status === 404, 'Nha cung cap khac khong doc duoc tin nhan');
  check((await api(other, tokenO, 'POST', '/chat/gui', fd({ ConversationId: cid, Body: 'chen ngang' }))).status === 404, 'Nha cung cap khac khong gui xen vao');
  check([302, 401].includes((await api(guest, tokenT, 'GET', '/chat/' + cid)).status), 'Khach vang lai khong doc duoc');

  // 7. Co so doc -> khach thay "da xem"; dang soan
  const thread = await api(provider, tokenP, 'GET', '/chat/' + cid);
  check(thread.status === 200 && thread.body.messages.length === 2 && thread.body.me === 'Branch', 'Co so mo cuoc tro chuyen, thay 2 tin');
  await api(provider, tokenP, 'POST', '/chat/' + cid + '/da-xem');
  const read = await waitFor(() => sT.events.find(e => e.t === 'read' && e.c === cid && e.side === 'Branch'), 'khach nhan su kien da xem');
  check(read.r === thread.body.messages[1].id, 'Khach nhan "da xem" den dung tin cuoi');
  check((await api(provider, tokenP, 'GET', '/chat/danh-sach')).body.reduce((n, x) => n + (x.branchId === branchId ? x.unread : 0), 0) === 0, 'Sau khi doc, so chua doc ve 0');
  await api(provider, tokenP, 'POST', '/chat/' + cid + '/dang-soan');
  await waitFor(() => sT.events.find(e => e.t === 'typing' && e.side === 'Branch'), 'khach thay dang soan');
  check(true, 'Khach thay co so dang soan tin');

  // 8. Co so tra loi; khach nhan ngay
  const reply = await api(provider, tokenP, 'POST', '/chat/gui', fd({ ConversationId: cid, Body: 'Chào bạn, còn bàn lúc 19:00 nhé.' }));
  check(reply.status === 200 && reply.body.message.side === 'Branch', 'Co so tra loi');
  await waitFor(() => sT.events.find(e => e.t === 'msg' && e.m === reply.body.message.id), 'khach nhan tra loi');
  check(true, 'Khach nhan tra loi ngay qua luong su kien');
  const tlist = (await api(traveler, tokenT, 'GET', '/chat/danh-sach')).body.filter(x => x.branchId === branchId);
  check(tlist.length === 1 && tlist[0].unread === 1 && tlist[0].who === row.Name, 'Khach thay 1 tin chua doc tu co so');
  const after = await api(traveler, tokenT, 'GET', `/chat/${cid}/tin?sau=${again.body.message.id}`);
  check(after.body.length === 1 && after.body[0].id === reply.body.message.id, 'Lay tin moi theo moc sau');
  const tthread = (await api(traveler, tokenT, 'GET', '/chat/' + cid)).body;
  check(tthread.otherReadId >= tthread.messages[1].id && tthread.eta.includes('Thường trả lời') && tthread.canSend, 'Khach thay moc da xem, thoi gian tra loi va co the gui');

  // 9. Anh
  const img = await api(traveler, tokenT, 'POST', '/chat/gui', fd({ ConversationId: cid }, true));
  check(img.status === 200 && img.body.message.image && img.body.message.kind === 'Image', 'Khach gui anh PNG');
  const imgUrl = `/chat/${cid}/anh/${img.body.message.id}`;
  const getImg = async c => fetch(base + imgUrl, { headers: { cookie: cookie(c) }, redirect: 'manual' });
  const okImg = await getImg(provider); check(okImg.status === 200 && okImg.headers.get('content-type') === 'image/png', 'Co so xem duoc anh khach gui');
  check((await getImg(traveler)).status === 200, 'Khach xem duoc anh minh gui');
  check((await getImg(other)).status === 404, 'Nguoi ngoai khong xem duoc anh');
  check([302, 401].includes((await getImg(guest)).status), 'Khach vang lai khong xem duoc anh');
  const bad = new FormData(); bad.append('ConversationId', cid); bad.append('Image', new Blob(['MZ'], { type: 'application/x-msdownload' }), 'x.exe');
  check((await api(traveler, tokenT, 'POST', '/chat/gui', bad)).status === 422, 'Tep khong phai anh bi tu choi');

  // 10. Trang thai, nhan, ghi chu noi bo
  check((await api(traveler, tokenT, 'POST', `/chat/${cid}/trang-thai`, fd({ State: 'Done' }))).status === 403, 'Khach khong doi trang thai cuoc tro chuyen');
  await api(provider, tokenP, 'POST', `/chat/${cid}/trang-thai`, fd({ State: 'Done', Label: 'Đã giữ bàn', Note: 'Khách quen' }));
  const done = (await api(provider, tokenP, 'GET', '/chat/danh-sach?loc=done')).body.filter(x => x.branchId === branchId);
  check(done.length === 1 && done[0].label === 'Đã giữ bàn', 'Loc "da xu ly" va nhan hien dung');
  check((await api(provider, tokenP, 'GET', '/chat/' + cid)).body.note === 'Khách quen', 'Ghi chu noi bo luu duoc');
  check((await api(traveler, tokenT, 'GET', '/chat/' + cid)).body.note == null, 'Khach khong thay ghi chu noi bo');
  await api(traveler, tokenT, 'POST', '/chat/gui', fd({ ConversationId: cid, Body: 'Cảm ơn quán!' }));
  check((await api(provider, tokenP, 'GET', '/chat/danh-sach?loc=open')).body.filter(x => x.branchId === branchId).length === 1, 'Khach nhan tiep thi cuoc tro chuyen mo lai');
  check((await api(provider, tokenP, 'GET', '/chat/danh-sach?q=' + encodeURIComponent('Cảm ơn'))).body.filter(x => x.branchId === branchId).length === 1, 'Tim theo noi dung');
  check((await api(provider, tokenP, 'GET', '/chat/danh-sach?q=khongco')).body.length === 0, 'Tim khong co ket qua');

  // 11. Cai dat + tin mau
  const set = await provider.get('/nha-cung-cap/tin-nhan/cai-dat?coSo=' + branchId);
  check(set.status === 200 && set.text.includes('Tin mẫu'), 'Trang cai dat tin nhan mo duoc');
  let r = await provider.post('/nha-cung-cap/tin-nhan/mau', set, { BranchId: branchId, Title: 'Giờ mở cửa', Body: 'Quán mở 07:00–21:30 mỗi ngày.' });
  check(r.status === 302, 'Them tin mau');
  const quick = (await api(provider, tokenP, 'GET', '/chat/tin-mau/' + branchId)).body;
  check(quick.length === 1 && quick[0].title === 'Giờ mở cửa', 'Danh sach tin mau co tin vua them');
  check((await api(other, tokenP, 'GET', '/chat/tin-mau/' + branchId)).status === 404, 'Nha cung cap khac khong xem tin mau');
  r = await provider.post('/nha-cung-cap/tin-nhan/mau', set, { BranchId: branchId, Id: quick[0].id, Title: 'x', Body: 'y', Delete: 'true' });
  check(r.status === 302 && (await api(provider, tokenP, 'GET', '/chat/tin-mau/' + branchId)).body.length === 0, 'Xoa tin mau');

  // 12. Tra loi tu dong cho cuoc tro chuyen moi
  sql(database, `DELETE FROM dbo.BaoCaoChat; DELETE FROM dbo.PhanUngTin; UPDATE dbo.TinNhanChat SET ReplyToId=NULL WHERE ReplyToId IS NOT NULL; DELETE m FROM dbo.TinNhanChat m JOIN dbo.CuocTroChuyen c ON c.Id=m.ConversationId WHERE c.BranchId=${branchId}; DELETE FROM dbo.NhomChatChuyenDi WHERE ConversationId IN (SELECT Id FROM dbo.CuocTroChuyen WHERE BranchId=${branchId}); DELETE FROM dbo.CuocTroChuyen WHERE BranchId=${branchId};`);
  r = await provider.post('/nha-cung-cap/tin-nhan/cai-dat', set, { BranchId: branchId, Accepting: 'true', ReplyWithin: 'Minutes', AutoReply: 'Cảm ơn bạn! Chúng tôi sẽ trả lời sớm.', AutoReplyOffHours: 'false', Greeting: 'Chào bạn!' });
  check(r.status === 302, 'Luu cai dat: tra loi tu dong');
  const info2 = (await api(traveler, tokenT, 'GET', '/chat/co-so/' + branchId)).body;
  check(info2.greeting === 'Chào bạn!' && info2.eta.includes('vài phút'), 'Loi chao va thoi gian tra loi hien cho khach');
  const f2 = await api(traveler, tokenT, 'POST', '/chat/gui', fd({ BranchId: branchId, Body: 'Cho mình hỏi giá phòng?' }));
  const cid2 = f2.body.conversationId;
  const t2 = (await api(traveler, tokenT, 'GET', '/chat/' + cid2)).body;
  check(t2.messages.length === 2 && t2.messages[1].kind === 'Auto' && t2.messages[1].side === 'Branch', 'Co tra loi tu dong cho tin dau tien');
  await api(traveler, tokenT, 'POST', '/chat/gui', fd({ ConversationId: cid2, Body: 'Hỏi thêm một câu.' }));
  check((await api(traveler, tokenT, 'GET', '/chat/' + cid2)).body.messages.length === 3, 'Khong tra loi tu dong lap lai');

  // 13. Tam ngung nhan tin
  r = await provider.post('/nha-cung-cap/tin-nhan/cai-dat', set, { BranchId: branchId, Accepting: 'false', ReplyWithin: 'Hour', AutoReplyOffHours: 'true' });
  check(r.status === 302, 'Tat nhan tin nhan');
  check((await api(traveler, tokenT, 'POST', '/chat/gui', fd({ ConversationId: cid2, Body: 'còn nhận không?' }))).status === 409, 'Cuoc tro chuyen cu khong gui them khi co so tam ngung');
  const blocked = (await api(traveler, tokenT, 'GET', '/chat/' + cid2)).body;
  check(!blocked.canSend && blocked.blocked, 'Khach thay ly do khong gui duoc');
  check((await api(traveler, tokenT, 'GET', '/chat/' + cid2 + '/tin')).status === 200, 'Van xem lai lich su khi tam ngung');
  await provider.post('/nha-cung-cap/tin-nhan/cai-dat', set, { BranchId: branchId, Accepting: 'true', ReplyWithin: 'Hour', AutoReplyOffHours: 'true' });

  // 14. Gioi han toc do (cuoi cung)
  let limited = 0;
  for (let n = 0; n < 24; n++) { const x = await api(traveler, tokenT, 'POST', '/chat/gui', fd({ ConversationId: cid2, Body: 'spam ' + n })); if (x.status === 429) limited++; }
  check(limited > 0, 'Gui qua nhanh bi gioi han (429): ' + limited);

  sP.close(); sT.close(); await Promise.all([sP.done, sT.done]);
  console.log(`PASS ${results.length} chat-flow checks.`);
})().catch(e => { console.error(e); process.exit(1); });
