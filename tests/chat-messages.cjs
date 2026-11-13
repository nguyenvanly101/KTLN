// Thao tac voi tung tin: tra loi, sua, thu hoi, bieu cam, vi tri, tep PDF.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5225 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/chat-messages.cjs
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
  let body = null; const type = r.headers.get('content-type') || '';
  if (type.includes('json')) body = await r.json(); else await r.arrayBuffer();
  return { status: r.status, body, type, disposition: r.headers.get('content-disposition') };
}
const fd = (values, file) => { const f = new FormData(); for (const [k, v] of Object.entries(values)) if (v != null) f.append(k, String(v)); if (file) f.append(file.field, new Blob([file.data], { type: file.type }), file.name); return f; };
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
const PDF = Buffer.from('%PDF-1.4\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF\n');

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { /* cho */ } if (n > 45) throw Error('Server not ready'); await sleep(1000); }
  const posterRow = json(database, "SELECT TOP(1) a.MaTaiKhoan Id,a.Email FROM dbo.NhuCauDongHanh n JOIN dbo.TaiKhoan a ON a.MaTaiKhoan=n.MaTaiKhoan WHERE a.VaiTro='KHACH_DU_LICH' AND a.TrangThai='HoatDong' ORDER BY n.MaNhuCau FOR JSON PATH;")[0];
  const A = json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}' FOR JSON PATH;`)[0].Id;
  sql(database, `DELETE FROM dbo.BaoCaoChat; DELETE FROM dbo.PhanUngTin; UPDATE dbo.TinNhanChat SET ReplyToId=NULL WHERE ReplyToId IS NOT NULL; DELETE m FROM dbo.TinNhanChat m JOIN dbo.CuocTroChuyen c ON c.Id=m.ConversationId WHERE c.BranchId IS NULL; DELETE FROM dbo.ThanhVienChat; DELETE FROM dbo.NhomChatChuyenDi WHERE ConversationId IN (SELECT Id FROM dbo.CuocTroChuyen WHERE BranchId IS NULL); DELETE FROM dbo.CuocTroChuyen WHERE BranchId IS NULL;`);
  const a = new Client(), b = new Client(), provider = new Client();
  await a.login(user('traveler').email, 'BranchTest@2026');
  try { await b.login(posterRow.Email); } catch { await b.login(posterRow.Email, 'HanhTrinhMoi@2026'); }
  await provider.login(user('provider').email, 'BranchTest@2026');
  const tok = async c => val(await c.get('/tin-nhan'), '__RequestVerificationToken');
  const [tA, tB, tP] = [await tok(a), await tok(b), await tok(provider)];
  const sB = stream(b); await sleep(500);

  const m1 = await api(a, tA, 'POST', '/chat/gui', fd({ PeerId: posterRow.Id, Body: 'Mình đi ngày 12, bạn đi hôm nào?' }));
  const cid = m1.body.conversationId, id1 = m1.body.message.id;
  const send = async (client, token, extra, file) => api(client, token, 'POST', '/chat/gui', fd({ ConversationId: cid, ...extra }, file));
  const get = async (client, id) => (await api(client, '', 'GET', `/chat/${cid}/tin/${id}`)).body;
  const msgs = async client => (await api(client, '', 'GET', `/chat/${cid}/tin`)).body;

  // 1. Tra loi
  const r1 = await send(b, tB, { Body: 'Mình đi 13 nhé', ReplyToId: id1 });
  const rd = await get(a, r1.body.message.id);
  check(rd.replyTo === id1 && rd.replyText.includes('ngày 12') && rd.replyAuthor === 'Bạn', 'Tra loi mot tin: trich dan dung noi dung, tac gia "Ban" voi nguoi duoc tra loi');
  check((await get(b, r1.body.message.id)).replyAuthor && (await get(b, r1.body.message.id)).replyAuthor !== 'Bạn', 'Nguoi tra loi thay ten nguoi duoc trich dan');
  check((await send(b, tB, { Body: 'x', ReplyToId: 99999999 })).status === 404, 'Tra loi tin khong ton tai bi tu choi');

  // 2. Sua
  check((await api(b, tB, 'POST', `/chat/${cid}/tin/${id1}/sua`, fd({ body: 'hack' }))).status === 403, 'Khong sua tin cua nguoi khac');
  check((await api(a, tA, 'POST', `/chat/${cid}/tin/${id1}/sua`, fd({ body: '   ' }))).status === 422, 'Sua thanh rong bi tu choi');
  check((await api(a, tA, 'POST', `/chat/${cid}/tin/${id1}/sua`, fd({ body: 'Mình đi ngày 12/12, bạn đi hôm nào?' }))).status === 200, 'Sua tin cua minh');
  await waitFor(() => sB.events.find(e => e.t === 'upd' && e.m === id1), 'su kien cap nhat khi sua');
  const edited = await get(b, id1);
  check(edited.edited && edited.body.includes('12/12'), 'Nguoi kia thay tin da sua kem nhan "da sua"');
  check((await get(b, r1.body.message.id)).replyText.includes('12/12'), 'Tin trich dan hien noi dung da sua');
  sql(database, `UPDATE dbo.TinNhanChat SET CreatedAt=DATEADD(MINUTE,-30,SYSUTCDATETIME()) WHERE Id=${id1};`);
  check((await api(a, tA, 'POST', `/chat/${cid}/tin/${id1}/sua`, fd({ body: 'muộn' }))).status === 409, 'Qua 15 phut khong sua duoc');

  // 3. Bieu cam
  const react = (client, token, id, emoji) => api(client, token, 'POST', `/chat/${cid}/tin/${id}/cam-xuc`, fd({ emoji }));
  check((await react(b, tB, id1, 'like')).status === 200, 'Tha bieu cam');
  const withReact = await get(a, id1);
  check(withReact.reactions.length === 1 && withReact.reactions[0].emoji === 'like' && withReact.reactions[0].count === 1 && withReact.reactions[0].mine === false, 'Nguoi khac thay bieu cam (mine = false)');
  check((await get(b, id1)).reactions[0].mine === true, 'Nguoi tha thay mine = true');
  await react(a, tA, id1, 'love');
  check((await get(a, id1)).reactions.length === 2, 'Hai bieu cam khac nhau cung hien');
  check((await react(b, tB, id1, 'like')).status === 200 && (await get(b, id1)).reactions.length === 1, 'Bam lai cung bieu cam de bo');
  check((await react(b, tB, id1, 'zzz')).status === 400, 'Bieu cam khong hop le bi tu choi');
  check((await react(provider, tP, id1, 'like')).status === 404, 'Nguoi ngoai khong tha duoc bieu cam');

  // 4. Thu hoi
  const t2 = await send(a, tA, { Body: 'Gửi nhầm rồi' });
  const id2 = t2.body.message.id;
  await react(b, tB, id2, 'wow');
  const quote = await send(b, tB, { Body: 'ok', ReplyToId: id2 });
  check((await api(b, tB, 'POST', `/chat/${cid}/tin/${id2}/thu-hoi`)).status === 403, 'Khong thu hoi tin cua nguoi khac');
  check((await api(a, tA, 'POST', `/chat/${cid}/tin/${id2}/thu-hoi`)).status === 200, 'Thu hoi tin cua minh');
  const gone = await get(b, id2);
  check(gone.deleted && gone.body === '' && !gone.reactions, 'Tin thu hoi: nguoi kia khong thay noi dung, bieu cam bi go');
  check((await get(b, quote.body.message.id)).replyText === 'Tin nhắn đã được thu hồi', 'Trich dan toi tin da thu hoi hien thong bao');
  check((await send(b, tB, { Body: 'x', ReplyToId: id2 })).status === 404, 'Khong tra loi tin da thu hoi');
  check((await api(a, tA, 'POST', `/chat/${cid}/tin/${id2}/thu-hoi`)).status === 403, 'Thu hoi hai lan bi tu choi');
  const t3 = await send(a, tA, { Body: 'quá lâu' });
  sql(database, `UPDATE dbo.TinNhanChat SET CreatedAt=DATEADD(MINUTE,-20,SYSUTCDATETIME()) WHERE Id=${t3.body.message.id};`);
  check((await api(a, tA, 'POST', `/chat/${cid}/tin/${t3.body.message.id}/thu-hoi`)).status === 409, 'Qua 10 phut khong thu hoi duoc');
  const last = await send(a, tA, { Body: 'tin cuoi' });
  await api(a, tA, 'POST', `/chat/${cid}/tin/${last.body.message.id}/thu-hoi`);
  check((await api(b, tB, 'GET', '/chat/danh-sach')).body.find(x => x.id === cid).preview === 'Tin nhắn đã được thu hồi', 'Xem truoc hop thu cap nhat khi thu hoi tin cuoi');

  // 5. Vi tri
  const loc = await send(a, tA, { Latitude: 16.0544, Longitude: 108.2022 });
  const ld = await get(b, loc.body.message.id);
  check(loc.status === 200 && ld.kind === 'Location' && Math.abs(ld.lat - 16.0544) < 0.001 && Math.abs(ld.lng - 108.2022) < 0.001, 'Gui vi tri: nguoi kia nhan duoc toa do');
  check((await send(a, tA, { Latitude: 120, Longitude: 108 })).status === 422 && (await send(a, tA, { Latitude: 16 })).status === 422, 'Toa do sai hoac thieu bi tu choi');
  check((await send(a, tA, { Latitude: 16, Longitude: 108 }, { field: 'File', data: PDF, type: 'application/pdf', name: 'a.pdf' })).status === 422, 'Khong gui vi tri kem tep trong cung mot tin');
  check((await api(b, tB, 'GET', '/chat/danh-sach')).body.find(x => x.id === cid).preview === '[Vị trí]', 'Xem truoc hop thu cua tin vi tri');

  // 6. Tep PDF
  const pdf = await send(a, tA, { Body: 'Vé của mình' }, { field: 'File', data: PDF, type: 'application/pdf', name: 've-may-bay.pdf' });
  const pd = await get(b, pdf.body.message.id);
  check(pdf.status === 200 && pd.kind === 'File' && pd.file && pd.fileName === 've-may-bay.pdf', 'Gui tep PDF kem ghi chu');
  const download = await api(b, '', 'GET', `/chat/${cid}/tep/${pdf.body.message.id}`);
  check(download.status === 200 && download.type.includes('application/pdf') && (download.disposition || '').includes('ve-may-bay.pdf'), 'Nguoi kia tai duoc tep PDF dung ten');
  check((await api(provider, '', 'GET', `/chat/${cid}/tep/${pdf.body.message.id}`)).status === 404, 'Nguoi ngoai khong tai duoc tep');
  check((await send(a, tA, {}, { field: 'File', data: Buffer.from('MZ'), type: 'application/x-msdownload', name: 'virus.exe' })).status === 422, 'Tep khong phai PDF bi tu choi');
  check((await send(a, tA, {}, { field: 'File', data: Buffer.from('khong phai pdf that'), type: 'application/pdf', name: 'gia.pdf' })).status >= 400, 'PDF gia (sai noi dung) bi tu choi');
  await api(a, tA, 'POST', `/chat/${cid}/tin/${pdf.body.message.id}/thu-hoi`);
  check((await api(b, '', 'GET', `/chat/${cid}/tep/${pdf.body.message.id}`)).status === 404, 'Thu hoi tin thi tep khong con tai duoc');

  // 7. Danh sach tin co du lieu moi
  const all = await msgs(b);
  check(all.some(m => m.deleted) && all.some(m => m.kind === 'Location') && all.some(m => m.replyTo), 'Danh sach tin tra ve tin thu hoi, vi tri va tra loi');

  sB.close(); await sB.done;
  console.log(`PASS ${results.length} chat-messages checks.`);
})().catch(e => { console.error(e); process.exit(1); });
