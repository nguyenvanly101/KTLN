// Dat dich vu hoan chinh trong chat: dat coc (ghi nhan), them vao chuyen di, nhac lich tu dong, moi danh gia, thong ke co so.
// Chay (can nhac lich nhanh): Chat__ReminderIntervalSeconds=5 cho may chu; FULLTEXT_TEST_BASE=http://127.0.0.1:5225 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/booking-m3.cjs
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
const vnDay = days => new Date(Date.now() + 7 * 3600000 + days * 86400000).toISOString().slice(0, 10);
async function api(client, token, method, path, form) {
  const r = await fetch(base + path, { method, headers: { cookie: cookie(client), ...(method !== 'GET' ? { RequestVerificationToken: token } : {}) }, body: form, redirect: 'manual' });
  let body = null; if ((r.headers.get('content-type') || '').includes('json')) body = await r.json(); else await r.arrayBuffer();
  return { status: r.status, body };
}
const fd = values => { const f = new FormData(); for (const [k, v] of Object.entries(values)) if (v != null) f.append(k, String(v)); return f; };
async function waitFor(fn, name, ms = 40000) { const end = Date.now() + ms; while (Date.now() < end) { const v = await fn(); if (v) return v; await sleep(1000); } assert.fail('Het thoi gian cho: ' + name); }

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { /* cho */ } if (n > 45) throw Error('Server not ready'); await sleep(1000); }
  const providerEmail = user('provider').email;
  const pair = json(database, `SELECT TOP(1) l.ServiceId,l.BranchId FROM dbo.DichVuCoSo l JOIN dbo.DichVuDuLich s ON s.MaDichVu=l.ServiceId JOIN dbo.CoSoDichVu b ON b.Id=l.BranchId JOIN dbo.NhaCungCapDichVu p ON p.MaNhaCungCap=b.ProviderId JOIN dbo.TaiKhoan t ON t.MaTaiKhoan=p.MaTaiKhoan WHERE t.Email=N'${providerEmail}' AND b.Status='Approved' AND b.PublishedVersion IS NOT NULL AND s.TrangThaiTiepNhan='TiepNhan' AND s.TrangThaiCongBo='DaDuyet' ORDER BY l.ServiceId FOR JSON PATH;`)[0];
  assert.ok(pair);
  const { ServiceId: sid, BranchId: bid } = pair;
  sql(database, `DELETE FROM dbo.NhacLichChat; DELETE FROM dbo.DatCocChat; DELETE FROM dbo.BaoCaoChat; DELETE FROM dbo.PhanUngTin; UPDATE dbo.TinNhanChat SET ReplyToId=NULL WHERE ReplyToId IS NOT NULL; DELETE m FROM dbo.TinNhanChat m JOIN dbo.CuocTroChuyen c ON c.Id=m.ConversationId WHERE c.BranchId=${bid}; DELETE FROM dbo.NhomChatChuyenDi WHERE ConversationId IN (SELECT Id FROM dbo.CuocTroChuyen WHERE BranchId=${bid}); DELETE FROM dbo.CuocTroChuyen WHERE BranchId=${bid}; DELETE FROM dbo.CaiDatChatCoSo WHERE BranchId=${bid};`);
  const a = new Client(), provider = new Client(), other = new Client();
  await a.login(user('traveler').email, 'BranchTest@2026'); await provider.login(providerEmail, 'BranchTest@2026'); await other.login(user('other').email, 'BranchTest@2026');
  const tok = async c => val(await c.get('/tin-nhan'), '__RequestVerificationToken');
  const [tA, tP] = [await tok(a), await tok(provider)];
  const book = async (days, extra = {}) => api(a, tA, 'POST', '/chat/dat', fd({ BranchId: bid, ServiceId: sid, Start: vnDay(days), End: vnDay(days + 1), People: 2, Quantity: 1, SubmissionId: crypto.randomUUID(), ...extra }));
  const b1 = await book(40); const cid = b1.body.conversationId, r1 = b1.body.message.booking;
  const b2 = (await book(50)).body.message.booking;
  const cards = async (c, t) => (await api(c, t, 'GET', `/chat/${cid}/the`)).body;
  const act = (c, t, rid, decision, extra = {}) => api(c, t, 'POST', `/chat/${cid}/dat/hanh-dong`, fd({ RequestId: rid, Decision: decision, ...extra }));

  // 1. Dat coc
  let card = (await cards(provider, tP)).find(x => x.id === r1);
  check(card.actions.includes('YeuCauCoc') && !card.actions.includes('ThemVaoChuyenDi'), 'Co so thay hanh dong "De nghi dat coc" khi yeu cau dang cho');
  check((await api(a, tA, 'POST', `/chat/${cid}/dat/coc`, fd({ RequestId: r1, Amount: 100000, Method: 'Chuyển khoản trực tiếp' }))).status === 403, 'Khach khong de nghi dat coc');
  const dep = (amount, method) => api(provider, tP, 'POST', `/chat/${cid}/dat/coc`, fd({ RequestId: r1, Amount: amount, Method: method, Note: 'STK 0123 - Hạn 24h' }));
  check((await dep(0, 'Chuyển khoản trực tiếp')).status >= 400 && (await dep(2e9, 'Chuyển khoản trực tiếp')).status >= 400, 'So tien coc khong hop le bi tu choi');
  check((await dep(100000, 'Bitcoin')).status === 422, 'Hinh thuc coc la bi tu choi');
  check((await dep(150000, 'Chuyển khoản trực tiếp')).status === 200, 'Co so de nghi dat coc');
  check((await dep(200000, 'Tiền mặt tại quầy')).status === 200, 'De nghi coc lai (doi so tien) khi chua nhan tien');
  card = (await cards(a, tA)).find(x => x.id === r1);
  check(card.deposit === 200000 && card.depositMethod === 'Tiền mặt tại quầy' && card.depositStatus === 'Requested' && card.depositNote.includes('STK'), 'Khach thay yeu cau coc: so tien, hinh thuc, ghi chu, dang cho');
  check(!card.actions.includes('YeuCauCoc'), 'Khach khong co nut de nghi coc');
  check((await act(provider, tP, r1, 'DaXacNhan')).status === 200, 'Co so xac nhan yeu cau');
  card = (await cards(provider, tP)).find(x => x.id === r1);
  check(card.actions.includes('DaNhanCoc') && !card.actions.includes('YeuCauCoc'), 'Sau xac nhan, co so co nut "Da nhan coc"');
  check((await api(a, tA, 'POST', `/chat/${cid}/dat/coc/xac-nhan`, fd({ requestId: r1 }))).status === 403, 'Khach khong tu xac nhan da tra coc');
  check((await api(provider, tP, 'POST', `/chat/${cid}/dat/coc/xac-nhan`, fd({ requestId: b2 }))).status === 404, 'Yeu cau chua de nghi coc khong xac nhan duoc');
  check((await api(provider, tP, 'POST', `/chat/${cid}/dat/coc/xac-nhan`, fd({ requestId: r1 }))).status === 200, 'Co so xac nhan da nhan coc');
  card = (await cards(a, tA)).find(x => x.id === r1);
  check(card.depositStatus === 'Paid' && card.actions.includes('ThemVaoChuyenDi'), 'Khach thay coc da nhan va co nut "Them vao chuyen di"');
  check((await dep(300000, 'Tiền mặt tại quầy')).status === 409, 'Khong doi coc sau khi da nhan tien');
  const events = (await api(a, tA, 'GET', `/chat/${cid}`)).body.messages.filter(m => m.kind === 'Event').map(m => m.body);
  check(events.some(e => e.includes('đề nghị đặt cọc')) && events.some(e => e.includes('đã nhận cọc')), 'Cuoc tro chuyen ghi lai de nghi coc va xac nhan nhan coc');
  check((await api(other, await tok(other), 'GET', `/chat/${cid}/the`)).status === 404, 'Nguoi ngoai khong xem duoc the dat coc');

  // 2. Them vao chuyen di
  const plans = await api(a, tA, 'GET', '/chat/ke-hoach');
  check(plans.status === 200 && Array.isArray(plans.body), 'Danh sach chuyen di cua khach de gan');
  check((await api(provider, tP, 'GET', '/chat/ke-hoach')).status === 403, 'Nha cung cap khong dung danh sach chuyen di');
  if (plans.body.length) {
    check((await api(a, tA, 'POST', `/chat/${cid}/dat/chuyen-di`, fd({ requestId: r1, planId: plans.body[0].id }))).status === 200 && (await cards(a, tA)).find(x => x.id === r1).planId === plans.body[0].id, 'Gan yeu cau da xac nhan vao chuyen di');
  } else check(true, 'Khach chua co chuyen di (bo qua buoc gan)');
  check((await api(provider, tP, 'POST', `/chat/${cid}/dat/chuyen-di`, fd({ requestId: r1 }))).status === 403, 'Nha cung cap khong gan yeu cau vao chuyen di cua khach');

  // 3. Nhac lich + moi danh gia
  await act(provider, tP, b2, 'DaXacNhan');
  sql(database, `UPDATE dbo.YeuCauDatDichVu SET NgayBatDau='${vnDay(1)}',NgayKetThuc='${vnDay(2)}' WHERE MaYeuCau=${r1}; UPDATE dbo.YeuCauDatDichVu SET NgayBatDau='${vnDay(-6)}',NgayKetThuc='${vnDay(-5)}' WHERE MaYeuCau=${b2};`);
  const reminder = await waitFor(async () => (await api(a, tA, 'GET', `/chat/${cid}`)).body.messages.find(m => m.kind === 'Event' && m.body.startsWith('Nhắc lịch') && m.booking === r1), 'nhac lich ngay truoc ngay dung dich vu');
  check(reminder.body.includes('#' + r1), 'Nhac lich tu dong: tin he thong trong cuoc tro chuyen dat cho');
  const review = await waitFor(async () => (await api(a, tA, 'GET', `/chat/${cid}`)).body.messages.find(m => m.kind === 'Event' && m.body.includes('đã diễn ra') && m.booking === b2), 'moi danh gia sau khi dung xong');
  check(!!review, 'Moi danh gia tu dong sau khi dich vu da dien ra');
  await sleep(12000);
  const after = (await api(a, tA, 'GET', `/chat/${cid}`)).body;
  check(after.messages.filter(m => m.body.startsWith('Nhắc lịch') && m.booking === r1).length === 1 && after.messages.filter(m => m.body.includes('đã diễn ra') && m.booking === b2).length === 1, 'Moi loai nhac chi gui mot lan cho moi yeu cau');
  check(json(database, `SELECT COUNT(*) N FROM dbo.NhacLichChat WHERE RequestId IN (${r1},${b2}) FOR JSON PATH;`)[0].N === 2, 'Da ghi nhan moc nhac de khong gui lai');
  const cA = after.cards.find(x => x.id === b2), cP = (await cards(provider, tP)).find(x => x.id === b2);
  check(cA.canReview === true && cP.canReview === false && !after.cards.find(x => x.id === r1).canReview, 'Chi khach, chi voi yeu cau da dien ra, co nut "Viet danh gia"');
  const bell = json(database, `SELECT COUNT(*) N FROM dbo.ThongBao WHERE LoaiThongBao='NhacLich' FOR JSON PATH;`)[0].N;
  check(bell >= 2, 'Co chuong thong bao cho nhac lich');

  // 4. Thong ke co so
  const settings = await provider.get('/nha-cung-cap/tin-nhan/cai-dat?coSo=' + bid);
  check(settings.status === 200 && settings.text.includes('Hiệu quả 30 ngày') && /2 yêu cầu đặt qua tin nhắn · 2 đã chốt/.test(settings.text), 'Trang cai dat co thong ke 30 ngay: so yeu cau va ty le chot');

  console.log(`PASS ${results.length} booking-m3 checks.`);
})().catch(e => { console.error(e); process.exit(1); });
