// M5: tai them lich su (phan trang), dem chua doc bang truy van gop, ban nhap / lightbox / thong bao o phia giao dien, chinh sach giu tin nhan.
// Chay (may chu can Chat__ReminderIntervalSeconds=5 va Chat__RetentionMonths=1): FULLTEXT_TEST_BASE=http://127.0.0.1:5225 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/chat-m5.cjs
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
async function waitFor(fn, name, ms = 45000) { const end = Date.now() + ms; while (Date.now() < end) { const v = await fn(); if (v) return v; await sleep(1000); } assert.fail('Het thoi gian cho: ' + name); }

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { /* cho */ } if (n > 45) throw Error('Server not ready'); await sleep(1000); }
  const posterRow = json(database, "SELECT TOP(1) a.MaTaiKhoan Id,a.Email FROM dbo.NhuCauDongHanh n JOIN dbo.TaiKhoan a ON a.MaTaiKhoan=n.MaTaiKhoan WHERE a.VaiTro='KHACH_DU_LICH' AND a.TrangThai='HoatDong' ORDER BY n.MaNhuCau FOR JSON PATH;")[0];
  const A = json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}' FOR JSON PATH;`)[0].Id;
  const B = posterRow.Id;
  sql(database, `DELETE FROM dbo.BaoCaoChat; DELETE FROM dbo.PhanUngTin; UPDATE dbo.TinNhanChat SET ReplyToId=NULL WHERE ReplyToId IS NOT NULL; DELETE m FROM dbo.TinNhanChat m JOIN dbo.CuocTroChuyen c ON c.Id=m.ConversationId WHERE c.BranchId IS NULL; DELETE FROM dbo.ThanhVienChat; DELETE FROM dbo.NhomChatChuyenDi WHERE ConversationId IN (SELECT Id FROM dbo.CuocTroChuyen WHERE BranchId IS NULL); DELETE FROM dbo.CuocTroChuyen WHERE BranchId IS NULL;`);
  const a = new Client(), b = new Client();
  await a.login(user('traveler').email, 'BranchTest@2026');
  try { await b.login(posterRow.Email); } catch { await b.login(posterRow.Email, 'HanhTrinhMoi@2026'); }
  const tok = async c => val(await c.get('/tin-nhan'), '__RequestVerificationToken');
  const [tA, tB] = [await tok(a), await tok(b)];
  const fd = v => { const f = new FormData(); for (const [k, x] of Object.entries(v)) f.append(k, String(x)); return f; };
  const first = await api(a, tA, 'POST', '/chat/gui', fd({ PeerId: B, Body: 'tin dau' }));
  const cid = first.body.conversationId;

  // 1. Phan trang lich su: 130 tin, xen ke hai ben
  const rows = Array.from({ length: 130 }, (_, n) => `(${cid},${n % 2 ? B : A},'${n % 2 ? 'Branch' : 'Traveler'}','Text',N'Tin so ${n}',DATEADD(SECOND,${n - 400},SYSUTCDATETIME()))`).join(',');
  sql(database, `INSERT dbo.TinNhanChat(ConversationId,SenderId,Side,Kind,Body,CreatedAt) VALUES ${rows}; UPDATE dbo.CuocTroChuyen SET LastMessageId=(SELECT MAX(Id) FROM dbo.TinNhanChat WHERE ConversationId=${cid}),LastMessageAt=SYSUTCDATETIME(),LastPreview=N'Tin so 129',LastFromTraveler=0 WHERE Id=${cid};`);
  const t = (await api(a, tA, 'GET', '/chat/' + cid)).body;
  check(t.messages.length === 60 && t.messages.at(-1).body === 'Tin so 129', 'Thread tra ve 60 tin moi nhat');
  const page2 = (await api(a, tA, 'GET', `/chat/${cid}/tin?truoc=${t.messages[0].id}`)).body;
  check(page2.length === 60 && page2.every(m => m.id < t.messages[0].id) && page2.every((m, n) => n === 0 || m.id > page2[n - 1].id) && page2.at(-1).id < t.messages[0].id, 'Trang truoc: 60 tin cu hon, xep tang dan, lien mach');
  const page3 = (await api(a, tA, 'GET', `/chat/${cid}/tin?truoc=${page2[0].id}`)).body;
  check(page3.length === 11 && page3.every(m => m.id < page2[0].id), 'Trang cuoi con 11 tin (130 + 1 tin dau - 120)');
  check((await api(a, tA, 'GET', `/chat/${cid}/tin?truoc=${page3[0].id}`)).body.length === 0, 'Het lich su thi tra ve rong');
  const all = [...page3, ...page2, ...t.messages];
  check(new Set(all.map(m => m.id)).size === 131, 'Khong trung / sot tin giua cac trang');

  // 2. Dem chua doc bang truy van gop (sau khi chen 130 tin)
  const bItem = (await api(b, tB, 'GET', '/chat/danh-sach')).body.find(x => x.id === cid);
  const aItem = (await api(a, tA, 'GET', '/chat/danh-sach')).body.find(x => x.id === cid);
  check(bItem.unread === 66 && aItem.unread === 65, 'Chua doc dung: B nhan 66 tin tu A, A nhan 65 tin tu B (' + bItem.unread + '/' + aItem.unread + ')');
  await api(b, tB, 'POST', `/chat/${cid}/da-xem`);
  check((await api(b, tB, 'GET', '/chat/danh-sach')).body.find(x => x.id === cid).unread === 0 && (await api(b, tB, 'GET', '/chat/chua-doc')).body.count === 0, 'Doc xong thi ve 0 o ca danh sach va tong');

  // 3. Giao dien: ban nhap, anh phong to, bat thong bao, nap them lich su
  const inbox = await a.get('/tin-nhan');
  check(inbox.html.includes('data-chat-notify'), 'Co nut "Bat thong bao"');
  const js = await (await fetch(base + '/js/chat.js')).text(), badge = await (await fetch(base + '/js/chat-badge.js')).text();
  check(js.includes('loadOlder') && js.includes('chat-draft:') && js.includes('lightbox') && js.includes('requestPermission'), 'Ma giao dien: tai them lich su, ban nhap, anh phong to, xin quyen thong bao');
  check(badge.includes('Notification') && badge.includes('visibilitychange') && badge.includes('Tin nhắn mới'), 'Thanh thong bao: nhap nhay tieu de + thong bao trinh duyet khi tab an');

  // 4. Chinh sach giu tin nhan (Chat:RetentionMonths=1)
  const img = new FormData(); img.append('ConversationId', cid); img.append('Image', new Blob([PNG], { type: 'image/png' }), 'cu.png');
  const sentImg = await api(a, tA, 'POST', '/chat/gui', img);
  const imgId = sentImg.body.message.id;
  const fresh = (await api(a, tA, 'POST', '/chat/gui', fd({ ConversationId: cid, Body: 'tin moi giu lai' }))).body.message.id;
  sql(database, `UPDATE dbo.TinNhanChat SET CreatedAt=DATEADD(DAY,-45,SYSUTCDATETIME()) WHERE Id=${imgId};`);
  await waitFor(async () => (await api(a, tA, 'GET', `/chat/${cid}/tin/${imgId}`)).body.deleted, 'don tin cu theo chinh sach giu tin');
  const purged = (await api(a, tA, 'GET', `/chat/${cid}/tin/${imgId}`)).body;
  check(purged.deleted && !purged.image && purged.body === '', 'Tin anh qua han bi thu hoi, noi dung xoa');
  check((await fetch(base + `/chat/${cid}/anh/${imgId}`, { headers: { cookie: cookie(a) }, redirect: 'manual' })).status === 404, 'Tep anh qua han khong con tai duoc');
  check(!(await api(a, tA, 'GET', `/chat/${cid}/tin/${fresh}`)).body.deleted, 'Tin con han van duoc giu');
  const kept = json(database, `SELECT COUNT(*) N FROM dbo.TinNhanChat WHERE ConversationId=${cid} AND DeletedAt IS NULL FOR JSON PATH;`)[0].N;
  check(kept >= 131, 'Cac tin khac khong bi anh huong');

  console.log(`PASS ${results.length} chat-m5 checks.`);
})().catch(e => { console.error(e); process.exit(1); });
