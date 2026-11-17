// Dat dich vu ngay trong chat: khach gui yeu cau, hai ben trao doi va thoa thuan (xac nhan / de xuat khac / dong y / tu choi / rut)
// ngay trong cuoc tro chuyen. Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5225 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/chat-booking.cjs
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
const iso = days => new Date(Date.now() + days * 86400000).toISOString().slice(0, 10);

async function api(client, token, method, path, form) {
  const r = await fetch(base + path, { method, headers: { cookie: cookie(client), ...(method !== 'GET' ? { RequestVerificationToken: token } : {}) }, body: form, redirect: 'manual' });
  let body = null; if ((r.headers.get('content-type') || '').includes('json')) body = await r.json(); else await r.arrayBuffer();
  return { status: r.status, body, location: r.headers.get('location') };
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
const request = id => json(database, `SELECT TrangThai,PhienBan,SoNguoi,ChiPhiDuKien FROM dbo.YeuCauDatDichVu WHERE MaYeuCau=${id} FOR JSON PATH;`)[0];

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { /* cho */ } if (n > 45) throw Error('Server not ready'); await sleep(1000); }
  const providerEmail = user('provider').email;
  const pair = json(database, `SELECT TOP(1) l.ServiceId,l.BranchId,s.TenDichVu FROM dbo.DichVuCoSo l JOIN dbo.DichVuDuLich s ON s.MaDichVu=l.ServiceId JOIN dbo.CoSoDichVu b ON b.Id=l.BranchId JOIN dbo.NhaCungCapDichVu p ON p.MaNhaCungCap=b.ProviderId JOIN dbo.TaiKhoan a ON a.MaTaiKhoan=p.MaTaiKhoan WHERE a.Email=N'${providerEmail}' AND b.Status='Approved' AND b.PublishedVersion IS NOT NULL AND s.TrangThaiTiepNhan='TiepNhan' AND s.TrangThaiCongBo='DaDuyet' ORDER BY l.ServiceId FOR JSON PATH;`)[0];
  assert.ok(pair, 'Can mot dich vu da duyet gan co so da duyet cua nha cung cap thu');
  const { ServiceId: sid, BranchId: bid } = pair;
  const otherService = json(database, `SELECT TOP(1) l.ServiceId FROM dbo.DichVuCoSo l WHERE l.BranchId<>${bid} AND NOT EXISTS (SELECT 1 FROM dbo.DichVuCoSo x WHERE x.ServiceId=l.ServiceId AND x.BranchId=${bid}) FOR JSON PATH;`)[0];
  sql(database, `DELETE FROM dbo.BaoCaoChat; DELETE FROM dbo.PhanUngTin; UPDATE dbo.TinNhanChat SET ReplyToId=NULL WHERE ReplyToId IS NOT NULL; DELETE m FROM dbo.TinNhanChat m JOIN dbo.CuocTroChuyen c ON c.Id=m.ConversationId WHERE c.BranchId=${bid}; DELETE FROM dbo.NhomChatChuyenDi WHERE ConversationId IN (SELECT Id FROM dbo.CuocTroChuyen WHERE BranchId=${bid}); DELETE FROM dbo.CuocTroChuyen WHERE BranchId=${bid}; DELETE FROM dbo.CaiDatChatCoSo WHERE BranchId=${bid};`);

  const guest = new Client(), traveler = new Client(), provider = new Client(), other = new Client();
  await traveler.login(user('traveler').email, 'BranchTest@2026');
  await provider.login(providerEmail, 'BranchTest@2026');
  await other.login(user('other').email, 'BranchTest@2026');
  const tokenT = val(await traveler.get('/tin-nhan'), '__RequestVerificationToken');
  const tokenP = val(await provider.get('/nha-cung-cap/tin-nhan'), '__RequestVerificationToken');
  const tokenO = val(await other.get('/nha-cung-cap/tin-nhan'), '__RequestVerificationToken');

  // 1. Loi vao tu trang dich vu / co so
  const go = await traveler.get(`/tin-nhan/dich-vu/${sid}?branchId=${bid}`);
  check(go.status === 302 && go.location === `/tin-nhan/co-so/${bid}?dichVu=${sid}`, 'Nut "Nhan tin de dat" dan vao chat cua dung co so: ' + go.location);
  const page = await traveler.get(go.location);
  check(page.status === 200 && page.html.includes('data-chat-book'), 'Khung chat co bieu mau dat dich vu');
  check((await guest.get(`/tin-nhan/dich-vu/${sid}`)).status === 302, 'Khach vang lai duoc dua toi dang nhap');
  const branchPage = await guest.get('/co-so/' + bid);
  check(branchPage.html.includes(`/tin-nhan/co-so/${bid}?dichVu=${sid}`), 'Trang co so co nut "Nhan tin de dat" tung dich vu');
  const services = await api(traveler, tokenT, 'GET', `/chat/co-so/${bid}/dich-vu`);
  check(services.status === 200 && services.body.some(x => x.id === sid), 'Danh sach dich vu dat duoc cua co so');

  // 2. Gui yeu cau dat trong chat
  const sP = stream(provider), sT = stream(traveler); await sleep(500);
  const sub = crypto.randomUUID();
  const body = (extra = {}) => fd({ BranchId: bid, ServiceId: sid, Start: iso(30), End: iso(32), People: 3, Quantity: 1, Notes: 'Gần cửa sổ giúp mình', SubmissionId: sub, ...extra });
  if (otherService) check((await api(traveler, tokenT, 'POST', '/chat/dat', fd({ BranchId: bid, ServiceId: otherService.ServiceId, Start: iso(30), People: 1, Quantity: 1 }))).status === 422, 'Dich vu khong thuoc co so bi tu choi');
  check([400, 422].includes((await api(traveler, tokenT, 'POST', '/chat/dat', fd({ BranchId: bid, ServiceId: sid, Start: iso(-5), People: 1, Quantity: 1 }))).status), 'Ngay trong qua khu bi tu choi');
  check((await api(provider, tokenP, 'POST', '/chat/dat', body())).status === 403, 'Nha cung cap khong gui yeu cau dat');
  const sent = await api(traveler, tokenT, 'POST', '/chat/dat', body());
  check(sent.status === 200 && sent.body.message.kind === 'Booking' && sent.body.message.booking > 0, 'Khach gui yeu cau dat trong chat: ' + JSON.stringify(sent.body));
  const cid = sent.body.conversationId, rid = sent.body.message.booking;
  check(request(rid).TrangThai === 'ChoPhanHoi', 'Yeu cau dat duoc tao o trang thai cho phan hoi');
  const dup = await api(traveler, tokenT, 'POST', '/chat/dat', body());
  check(dup.status === 200 && dup.body.message.booking === rid, 'Gui lai cung ma luot gui khong tao yeu cau moi');
  await waitFor(() => sP.events.find(e => e.t === 'msg' && e.c === cid), 'co so nhan the dat ngay');
  check(true, 'Co so nhan the yeu cau ngay qua luong su kien');

  // 3. The yeu cau va hanh dong theo vai tro
  const pt = (await api(provider, tokenP, 'GET', '/chat/' + cid)).body;
  const pc = pt.cards.find(c => c.id === rid);
  check(pc && pc.people === 3 && pc.service === pair.TenDichVu && pc.actions.filter(x => !['YeuCauCoc', 'DaNhanCoc', 'ThemVaoChuyenDi'].includes(x)).join() === 'DaXacNhan,DeXuatKhac,TuChoi', 'Co so thay the voi 3 hanh dong: xac nhan / de xuat khac / tu choi');
  const tc = (await api(traveler, tokenT, 'GET', `/chat/${cid}/the`)).body.find(c => c.id === rid);
  check(tc && tc.actions.filter(x => !['YeuCauCoc', 'DaNhanCoc', 'ThemVaoChuyenDi'].includes(x)).join() === 'Rut', 'Khach chi co the rut yeu cau khi dang cho');
  check((await api(provider, tokenP, 'POST', `/chat/${cid}/dat/hanh-dong`, fd({ RequestId: rid, Decision: 'KhachDongY' }))).status === 403, 'Co so khong the dong y thay khach');
  check((await api(traveler, tokenT, 'POST', `/chat/${cid}/dat/hanh-dong`, fd({ RequestId: rid, Decision: 'DaXacNhan' }))).status === 403, 'Khach khong the tu xac nhan');
  check((await api(other, tokenO, 'POST', `/chat/${cid}/dat/hanh-dong`, fd({ RequestId: rid, Decision: 'DaXacNhan' }))).status === 404, 'Nha cung cap khac khong xu ly duoc');
  check((await api(other, tokenO, 'GET', `/chat/${cid}/the`)).status === 404, 'Nha cung cap khac khong xem duoc the');

  // 4. Hai ben tu thoa thuan: nhan tin + de xuat khac
  await api(traveler, tokenT, 'POST', '/chat/gui', fd({ ConversationId: cid, Body: 'Cho mình hỏi có giảm giá cho nhóm không?' }));
  await api(provider, tokenP, 'POST', '/chat/gui', fd({ ConversationId: cid, Body: 'Nhóm 3 người mình giảm 10% nhé.' }));
  check((await api(provider, tokenP, 'POST', `/chat/${cid}/dat/hanh-dong`, fd({ RequestId: rid, Decision: 'DeXuatKhac' }))).status >= 400, 'De xuat khac thieu dieu kien bi tu choi');
  const offer = await api(provider, tokenP, 'POST', `/chat/${cid}/dat/hanh-dong`, fd({ RequestId: rid, Decision: 'DeXuatKhac', Text: 'Giảm 10% cho nhóm 3 người, giữ bàn gần cửa sổ.', Cost: 123000, People: 3 }));
  check(offer.status === 200, 'Co so de xuat dieu kien khac trong chat: ' + JSON.stringify(offer.body));
  await waitFor(() => sT.events.find(e => e.t === 'msg' && e.c === cid && e.m > sent.body.message.id + 2), 'khach nhan de xuat');
  const thread2 = (await api(traveler, tokenT, 'GET', '/chat/' + cid)).body;
  const c2 = thread2.cards.find(c => c.id === rid);
  check(c2.status === 'DeXuatKhac' && c2.cost === 123000 && c2.terms.includes('Giảm 10%') && c2.actions.filter(x => !['YeuCauCoc', 'DaNhanCoc', 'ThemVaoChuyenDi'].includes(x)).join() === 'KhachDongY,KhachKhongDongY,Rut', 'Khach thay de xuat va hanh dong dong y / khong dong y');
  check(thread2.messages.some(m => m.kind === 'Event' && m.booking === rid && m.body.includes('đề xuất')), 'Cuoc tro chuyen co dong su kien de xuat ' + JSON.stringify(thread2.messages.slice(-3)));
  check(request(rid).TrangThai === 'DeXuatKhac', 'Trang thai yeu cau cap nhat theo de xuat');
  check((await api(provider, tokenP, 'GET', `/chat/${cid}/the`)).body.find(c => c.id === rid).actions.filter(x => !['YeuCauCoc', 'DaNhanCoc', 'ThemVaoChuyenDi'].includes(x)).length === 0, 'Co so het hanh dong trong luc cho khach tra loi');

  // 5. Khach dong y -> xac nhan
  const agree = await api(traveler, tokenT, 'POST', `/chat/${cid}/dat/hanh-dong`, fd({ RequestId: rid, Decision: 'KhachDongY' }));
  check(agree.status === 200 && request(rid).TrangThai === 'DaXacNhan', 'Khach dong y de xuat: yeu cau duoc xac nhan');
  const done = (await api(provider, tokenP, 'GET', '/chat/' + cid)).body;
  check(done.cards.find(c => c.id === rid).status === 'DaXacNhan' && done.cards.find(c => c.id === rid).actions.filter(x => !['YeuCauCoc', 'DaNhanCoc', 'ThemVaoChuyenDi', 'Huy'].includes(x)).length === 0, 'The cho hai ben deu o trang thai da xac nhan, chi con Huy (truoc ngay dung) va viec phu');
  check(done.messages.some(m => m.kind === 'Event' && m.body.includes('Đã xác nhận')), 'Co dong su kien da xac nhan');
  check((await api(traveler, tokenT, 'POST', `/chat/${cid}/dat/hanh-dong`, fd({ RequestId: rid, Decision: 'Rut' }))).status >= 400, 'Khong the rut yeu cau da xac nhan');
  const detail = await traveler.get('/Bookings/Detail/' + rid);
  check(detail.status === 200 && detail.text.includes(pair.TenDichVu), 'Yeu cau van xem duoc trong muc "Dat dich vu" cua khach');

  // 6. Tu choi va rut
  const r2 = (await api(traveler, tokenT, 'POST', '/chat/dat', fd({ ConversationId: cid, ServiceId: sid, Start: iso(40), People: 2, Quantity: 1 }))).body.message.booking;
  check((await api(provider, tokenP, 'POST', `/chat/${cid}/dat/hanh-dong`, fd({ RequestId: r2, Decision: 'TuChoi' }))).status >= 400, 'Tu choi can ly do');
  check((await api(provider, tokenP, 'POST', `/chat/${cid}/dat/hanh-dong`, fd({ RequestId: r2, Decision: 'TuChoi', Reason: 'Hôm đó quán nghỉ lễ.' }))).status === 200 && request(r2).TrangThai === 'TuChoi', 'Co so tu choi kem ly do');
  check((await api(traveler, tokenT, 'GET', '/chat/' + cid)).body.messages.some(m => m.kind === 'Event' && m.body.includes('nghỉ lễ')), 'Ly do tu choi hien trong chat');
  const r3 = (await api(traveler, tokenT, 'POST', '/chat/dat', fd({ ConversationId: cid, ServiceId: sid, Start: iso(50), People: 1, Quantity: 1 }))).body.message.booking;
  check((await api(traveler, tokenT, 'POST', `/chat/${cid}/dat/hanh-dong`, fd({ RequestId: r3, Decision: 'Rut' }))).status === 200 && request(r3).TrangThai === 'KhongTiepTuc', 'Khach rut yeu cau dang cho');
  const r4 = (await api(traveler, tokenT, 'POST', '/chat/dat', fd({ ConversationId: cid, ServiceId: sid, Start: iso(60), People: 1, Quantity: 1 }))).body.message.booking;
  await api(provider, tokenP, 'POST', `/chat/${cid}/dat/hanh-dong`, fd({ RequestId: r4, Decision: 'DeXuatKhac', Text: 'Đổi sang buổi tối.', People: 2 }));
  check((await api(traveler, tokenT, 'POST', `/chat/${cid}/dat/hanh-dong`, fd({ RequestId: r4, Decision: 'KhachKhongDongY' }))).status === 200 && request(r4).TrangThai === 'KhongTiepTuc', 'Khach khong dong y de xuat: yeu cau ket thuc');

  // 7. Tin nhan chua doc va xem truoc co ghi nhan dat cho
  const list = (await api(provider, tokenP, 'GET', '/chat/danh-sach')).body;
  const mine = list.find(x => x.id === cid);
  check(mine && /Đặt dịch vụ|Yêu cầu đặt/.test(mine.preview || ''), 'Danh sach hop thu hien xem truoc cua the dat');

  sP.close(); sT.close(); await Promise.all([sP.done, sT.done]);
  console.log(`PASS ${results.length} chat-booking checks.`);
})().catch(e => { console.error(e); process.exit(1); });
