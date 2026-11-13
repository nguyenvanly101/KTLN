// Chat nhom kieu Facebook: tao nhom tu ban be, tin nhan nhom thoi gian thuc, da xem theo tung nguoi, them / xoa / roi / doi ten, quyen truy cap.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5225 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/chat-group.cjs
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
const PNG = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==', 'base64');
async function api(client, token, method, path, form) {
  const r = await fetch(base + path, { method, headers: { cookie: cookie(client), ...(method !== 'GET' ? { RequestVerificationToken: token } : {}) }, body: form, redirect: 'manual' });
  let body = null; if ((r.headers.get('content-type') || '').includes('json')) body = await r.json(); else await r.arrayBuffer();
  return { status: r.status, body };
}
function fd(values, image) {
  const f = new FormData();
  for (const [k, v] of Object.entries(values)) if (v != null) for (const item of Array.isArray(v) ? v : [v]) f.append(k, String(item));
  if (image) f.append('Image', new Blob([PNG], { type: 'image/png' }), 'a.png');
  return f;
}
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
const stamp = Date.now().toString().slice(-8);

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { /* cho */ } if (n > 45) throw Error('Server not ready'); await sleep(1000); }
  const id = email => json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${email}' FOR JSON PATH;`)[0].Id;
  const A = id(user('traveler').email);
  const posterRow = json(database, "SELECT TOP(1) a.MaTaiKhoan Id,a.Email FROM dbo.NhuCauDongHanh n JOIN dbo.TaiKhoan a ON a.MaTaiKhoan=n.MaTaiKhoan WHERE a.VaiTro='KHACH_DU_LICH' AND a.TrangThai='HoatDong' ORDER BY n.MaNhuCau FOR JSON PATH;")[0];
  const B = posterRow.Id;
  const mkUser = tag => {
    const email = `grp-${tag}-${stamp}@example.com`;
    sql(database, `INSERT dbo.TaiKhoan(Email,SoDienThoai,MatKhauBam,VaiTro,TrangThai,DauPhien,PhaiDoiMatKhau,ThoiDiemTao) SELECT N'${email}',NULL,MatKhauBam,VaiTro,TrangThai,NEWID(),0,SYSUTCDATETIME() FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}';`);
    const uid = id(email);
    sql(database, `INSERT dbo.HoSoNguoiDung(MaTaiKhoan,TenHienThi,ThongTinChiaSe,ThoiDiemCapNhat) VALUES(${uid},N'Thành viên ${tag.toUpperCase()} ${stamp}','CongKhai',SYSUTCDATETIME());`);
    return { id: uid, email };
  };
  const Cu = mkUser('c'), Du = mkUser('d');
  const ids = `${A},${B},${Cu.id},${Du.id}`;
  sql(database, `DELETE FROM dbo.BaoCaoChat; DELETE FROM dbo.PhanUngTin; UPDATE dbo.TinNhanChat SET ReplyToId=NULL WHERE ReplyToId IS NOT NULL; DELETE m FROM dbo.TinNhanChat m JOIN dbo.CuocTroChuyen c ON c.Id=m.ConversationId WHERE c.IsGroup=1 OR c.BranchId IS NULL; DELETE t FROM dbo.ThanhVienChat t; DELETE FROM dbo.NhomChatChuyenDi WHERE ConversationId IN (SELECT Id FROM dbo.CuocTroChuyen WHERE IsGroup=1 OR BranchId IS NULL); DELETE FROM dbo.CuocTroChuyen WHERE IsGroup=1 OR BranchId IS NULL;`);
  sql(database, `DELETE FROM dbo.KetBan WHERE RequesterId IN (${ids}) OR AddresseeId IN (${ids});` +
    `INSERT dbo.KetBan(RequesterId,AddresseeId,Status,RespondedAt) VALUES (${A},${B},'Accepted',SYSUTCDATETIME()),(${A},${Cu.id},'Accepted',SYSUTCDATETIME()),(${B},${Cu.id},'Accepted',SYSUTCDATETIME()),(${A},${Du.id},'Accepted',SYSUTCDATETIME());`);

  const a = new Client(), b = new Client(), c = new Client(), d = new Client(), provider = new Client();
  await a.login(user('traveler').email, 'BranchTest@2026');
  try { await b.login(posterRow.Email); } catch { await b.login(posterRow.Email, 'HanhTrinhMoi@2026'); }
  await c.login(Cu.email, 'BranchTest@2026'); await d.login(Du.email, 'BranchTest@2026');
  await provider.login(user('provider').email, 'BranchTest@2026');
  const tok = async (client, path = '/tin-nhan') => val(await client.get(path), '__RequestVerificationToken');
  const [tA, tB, tC, tD, tP] = [await tok(a), await tok(b), await tok(c), await tok(d), await tok(provider, '/tin-nhan')];

  // 1. Tao nhom
  check((await api(provider, tP, 'POST', '/chat/nhom', fd({ Title: 'x', MemberIds: [A] }))).status === 403, 'Nha cung cap khong tao nhom');
  check((await api(a, tA, 'POST', '/chat/nhom', fd({ Title: 'Không bạn', MemberIds: [] }))).status === 422, 'Nhom can it nhat mot thanh vien');
  check((await api(b, tB, 'POST', '/chat/nhom', fd({ Title: 'Có người lạ', MemberIds: [Du.id] }))).status === 422, 'Chi them duoc nguoi la ban be');
  check((await api(a, tA, 'POST', '/chat/nhom', fd({ Title: 'x'.repeat(101), MemberIds: [B] }))).status >= 400, 'Ten nhom qua 100 ky tu bi tu choi');
  const sB = stream(b), sC = stream(c); await sleep(500);
  const created = await api(a, tA, 'POST', '/chat/nhom', fd({ Title: 'Đà Lạt tháng 12 ' + stamp, MemberIds: [B, Cu.id] }));
  check(created.status === 200 && created.body.conversationId > 0, 'Tao nhom voi 2 nguoi ban');
  const gid = created.body.conversationId;
  await waitFor(() => sB.events.find(e => e.t === 'msg' && e.c === gid) && sC.events.find(e => e.t === 'msg' && e.c === gid), 'thanh vien nhan su kien tao nhom');
  check(true, 'Thanh vien nhan su kien tao nhom ngay');

  // 2. Danh sach va thread
  const bList = (await api(b, tB, 'GET', '/chat/danh-sach')).body.find(x => x.id === gid);
  check(bList && bList.group && bList.members === 3 && bList.who.includes('Đà Lạt') && bList.unread === 1, 'Nhom hien trong hop thu cua thanh vien: 3 nguoi, 1 tin chua doc');
  const tB1 = (await api(b, tB, 'GET', '/chat/' + gid)).body;
  check(tB1.item.group && tB1.members.length === 3 && tB1.myRole === 'Member' && tB1.me === 'Member' && tB1.canSend, 'Thread nhom: danh sach thanh vien va vai tro');
  check(tB1.members.filter(m => m.role === 'Admin').length === 1 && tB1.messages[0].kind === 'Event' && tB1.messages[0].body.includes('đã tạo nhóm'), 'Nguoi tao la quan tri, co dong su kien tao nhom');
  check((await api(a, tA, 'GET', '/chat/' + gid)).body.myRole === 'Admin', 'Nguoi tao la Admin');
  check((await api(provider, tP, 'GET', '/chat/' + gid)).status === 404 && (await api(d, tD, 'GET', '/chat/' + gid)).status === 404, 'Nguoi ngoai nhom khong doc duoc');
  check((await api(d, tD, 'POST', '/chat/gui', fd({ ConversationId: gid, Body: 'chen' }))).status === 404, 'Nguoi ngoai nhom khong gui duoc');

  // 3. Nhan tin nhom
  const sent = await api(a, tA, 'POST', '/chat/gui', fd({ ConversationId: gid, Body: 'Mọi người thấy lịch trình này ổn không?' }));
  check(sent.status === 200 && sent.body.message.side === 'Member', 'Gui tin vao nhom');
  await waitFor(() => sB.events.find(e => e.t === 'msg' && e.m === sent.body.message.id) && sC.events.find(e => e.t === 'msg' && e.m === sent.body.message.id), 'thanh vien nhan tin nhom');
  check(true, 'Moi thanh vien nhan tin nhom ngay');
  const tC1 = (await api(c, tC, 'GET', '/chat/' + gid)).body;
  const last = tC1.messages[tC1.messages.length - 1];
  check(last.body.includes('lịch trình') && last.mine === false && last.sender && last.sender.includes('Thành viên') === false && last.senderId === A, 'Tin nhom co ten nguoi gui va co "mine" theo nguoi xem');
  check((await api(a, tA, 'GET', '/chat/' + gid)).body.messages.at(-1).mine === true, 'Tin cua chinh minh duoc danh dau mine');
  await api(c, tC, 'POST', `/chat/${gid}/da-xem`);
  await waitFor(() => sB.events.find(e => e.t === 'read' && e.c === gid && e.account === Cu.id), 'su kien da xem cua thanh vien');
  check(true, 'Moc "da xem" theo tung thanh vien');
  check((await api(c, tC, 'GET', '/chat/danh-sach')).body.find(x => x.id === gid).unread === 0, 'Da xem thi het chua doc');
  check((await api(b, tB, 'GET', '/chat/chua-doc')).body.count >= 2, 'Tong chua doc tinh ca tin nhom');
  await api(b, tB, 'POST', `/chat/${gid}/dang-soan`);
  const typing = await waitFor(() => sC.events.find(e => e.t === 'typing' && e.c === gid), 'dang soan trong nhom');
  check(typing.account === B && typing.name, 'Thanh vien khac thay ai dang soan');
  const reply = await api(b, tB, 'POST', '/chat/gui', fd({ ConversationId: gid, Body: 'Ổn đó!' }));
  await waitFor(() => sC.events.find(e => e.t === 'msg' && e.m === reply.body.message.id), 'tra loi nhom');
  const img = await api(c, tC, 'POST', '/chat/gui', fd({ ConversationId: gid }, true));
  check(img.status === 200 && img.body.message.image, 'Gui anh vao nhom');
  const getImg = async client => fetch(base + `/chat/${gid}/anh/${img.body.message.id}`, { headers: { cookie: cookie(client) }, redirect: 'manual' });
  check((await getImg(a)).status === 200 && (await getImg(d)).status === 404, 'Chi thanh vien xem duoc anh trong nhom');

  // 4. Khong dung chuc nang danh cho cuoc tro chuyen 1-1
  check((await api(a, tA, 'POST', `/chat/${gid}/chan`, fd({ block: 'true' }))).status === 422, 'Nhom khong co chuc nang chan');
  check((await api(a, tA, 'POST', '/chat/dat', fd({ ConversationId: gid, ServiceId: 1, Start: '2099-01-01', People: 1, Quantity: 1 }))).status === 403, 'Khong dat dich vu trong nhom');
  check((await api(a, tA, 'POST', '/chat/ru', fd({ ConversationId: gid, NeedId: 1 }))).status === 403, 'Khong gui loi ru di cung trong nhom');

  // 5. Quan ly nhom
  check((await api(b, tB, 'POST', `/chat/${gid}/nhom/them`, fd({ AccountIds: [Du.id] }))).status === 422, 'Thanh vien chi them duoc ban be cua chinh minh');
  check((await api(a, tA, 'POST', `/chat/${gid}/nhom/them`, fd({ AccountIds: [Du.id] }))).status === 200, 'Them ban vao nhom');
  check((await api(a, tA, 'POST', `/chat/${gid}/nhom/them`, fd({ AccountIds: [Du.id] }))).status === 409, 'Them nguoi da co trong nhom bi tu choi');
  const dT = (await api(d, tD, 'GET', '/chat/' + gid)).body;
  check(dT.members.length === 4 && dT.messages.some(m => m.body.includes('lịch trình')), 'Nguoi moi them xem duoc nhom va lich su');
  check((await api(d, tD, 'GET', '/chat/danh-sach')).body.find(x => x.id === gid).unread <= 1, 'Nguoi moi them chi co dong "duoc them" la chua doc, khong bi tinh tin cu');
  check((await api(b, tB, 'POST', `/chat/${gid}/nhom/xoa`, fd({ accountId: Cu.id }))).status === 403, 'Thanh vien thuong khong xoa duoc nguoi khac');
  check((await api(a, tA, 'POST', `/chat/${gid}/nhom/xoa`, fd({ accountId: A }))).status === 422, 'Khong tu xoa minh, phai dung "Roi nhom"');
  check((await api(a, tA, 'POST', `/chat/${gid}/nhom/xoa`, fd({ accountId: Cu.id }))).status === 200, 'Quan tri xoa thanh vien');
  check((await api(c, tC, 'GET', '/chat/' + gid)).status === 404 && (await api(c, tC, 'POST', '/chat/gui', fd({ ConversationId: gid, Body: 'còn không' }))).status === 404, 'Nguoi bi xoa khong doc / gui duoc');
  check((await api(c, tC, 'GET', '/chat/danh-sach')).body.every(x => x.id !== gid), 'Nhom khong con trong hop thu cua nguoi bi xoa');
  check((await api(a, tA, 'POST', `/chat/${gid}/nhom/them`, fd({ AccountIds: [Cu.id] }))).status === 200 && (await api(c, tC, 'GET', '/chat/' + gid)).status === 200, 'Them lai nguoi da bi xoa');
  check((await api(b, tB, 'POST', `/chat/${gid}/nhom/doi-ten`, fd({ title: '   ' }))).status === 422, 'Ten nhom rong bi tu choi');
  check((await api(b, tB, 'POST', `/chat/${gid}/nhom/doi-ten`, fd({ title: 'Nhóm Đà Lạt mới' }))).status === 200 && (await api(a, tA, 'GET', '/chat/' + gid)).body.item.who === 'Nhóm Đà Lạt mới', 'Thanh vien doi ten nhom');
  check((await api(a, tA, 'GET', '/chat/' + gid)).body.messages.some(m => m.kind === 'Event' && m.body.includes('đổi tên nhóm')), 'Co dong su kien doi ten');

  // 6. Roi nhom
  check((await api(a, tA, 'POST', `/chat/${gid}/nhom/roi`)).status === 200, 'Quan tri roi nhom');
  check((await api(a, tA, 'GET', '/chat/' + gid)).status === 404 && (await api(a, tA, 'GET', '/chat/danh-sach')).body.every(x => x.id !== gid), 'Nguoi da roi khong con thay nhom');
  const after = (await api(b, tB, 'GET', '/chat/' + gid)).body;
  check(after.members.length === 3 && after.members.filter(m => m.role === 'Admin').length === 1, 'Nhom van co dung mot quan tri sau khi quan tri roi');
  check(after.messages.some(m => m.kind === 'Event' && m.body.includes('đã rời nhóm')), 'Co dong su kien roi nhom');
  check((await api(b, tB, 'POST', `/chat/${gid}/nhom/roi`)).status === 200 && (await api(c, tC, 'POST', `/chat/${gid}/nhom/roi`)).status === 200 && (await api(d, tD, 'POST', `/chat/${gid}/nhom/roi`)).status === 200, 'Tat ca roi nhom khong gay loi');

  sB.close(); sC.close(); await Promise.all([sB.done, sC.done]);
  console.log(`PASS ${results.length} chat-group checks.`);
})().catch(e => { console.error(e); process.exit(1); });
