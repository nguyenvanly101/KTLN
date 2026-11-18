// Ung dung tin nhan cho hai muc tieu: TIM BAN DONG HANH (goi y phu hop, nhan tin tu ket qua, dong y -> thanh ban + lap nhom chuyen di)
// va DAT DICH VU (kiem tra lich + tam tinh truoc khi gui, loc cho xac nhan, ngu canh co so, lien ket giua chat va trang dat cho).
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5225 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/chat-apps.cjs
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
  return { status: r.status, body, location: r.headers.get('location') };
}
const fd = values => { const f = new FormData(); for (const [k, v] of Object.entries(values)) if (v != null) for (const item of Array.isArray(v) ? v : [v]) f.append(k, String(item)); return f; };
const estimate = (price, unit, people, qty, start, end) => {
  if (price == null) return null; const u = (unit || '').toLowerCase(); let n = Math.max(1, qty);
  if (u.includes('người') || u.includes('khách')) n *= Math.max(1, people);
  if ((u.includes('đêm') || u.includes('ngày')) && end) n *= Math.max(1, Math.round((new Date(end) - new Date(start)) / 86400000));
  return price * n;
};
const stamp = Date.now().toString().slice(-8);

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { /* cho */ } if (n > 45) throw Error('Server not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const id = email => json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${email}' FOR JSON PATH;`)[0].Id;
  const A = id(user('traveler').email);
  const posterRow = json(database, "SELECT TOP(1) a.MaTaiKhoan Id,a.Email FROM dbo.NhuCauDongHanh n JOIN dbo.TaiKhoan a ON a.MaTaiKhoan=n.MaTaiKhoan WHERE a.VaiTro='KHACH_DU_LICH' AND a.TrangThai='HoatDong' ORDER BY n.MaNhuCau FOR JSON PATH;")[0];
  const B = posterRow.Id;
  const providerEmail = user('provider').email;

  const a = new Client(), b = new Client(), provider = new Client(), other = new Client(), guest = new Client();
  await a.login(user('traveler').email, 'BranchTest@2026');
  try { await b.login(posterRow.Email); } catch { await b.login(posterRow.Email, 'HanhTrinhMoi@2026'); }
  await provider.login(providerEmail, 'BranchTest@2026'); await other.login(user('other').email, 'BranchTest@2026');
  const tok = async (c, path = '/tin-nhan') => val(await c.get(path), '__RequestVerificationToken');
  const [tA, tB, tP, tO] = [await tok(a), await tok(b), await tok(provider), await tok(other)];

  // =========================== TIM BAN DONG HANH ===========================
  sql(database, `DELETE FROM dbo.BaoCaoChat; DELETE FROM dbo.PhanUngTin; UPDATE dbo.TinNhanChat SET ReplyToId=NULL WHERE ReplyToId IS NOT NULL; DELETE m FROM dbo.TinNhanChat m JOIN dbo.CuocTroChuyen c ON c.Id=m.ConversationId WHERE c.BranchId IS NULL; DELETE FROM dbo.ThanhVienChat; DELETE FROM dbo.NhomChatChuyenDi WHERE ConversationId IN (SELECT Id FROM dbo.CuocTroChuyen WHERE BranchId IS NULL); DELETE FROM dbo.CuocTroChuyen WHERE BranchId IS NULL; DELETE FROM dbo.KetBan WHERE (RequesterId=${A} AND AddresseeId=${B}) OR (RequesterId=${B} AND AddresseeId=${A});` +
    `DELETE FROM dbo.DeNghiKetNoiDongHanh WHERE MaNguoiGui IN (${A},${B}) AND MaNhuCau IN (SELECT MaNhuCau FROM dbo.NhuCauDongHanh WHERE NoiDung LIKE N'QA-match-%'); DELETE FROM dbo.NhuCauDongHanh WHERE NoiDung LIKE N'QA-match-%';`);
  const dest = 'Đà Lạt';
  const mkNeed = (acc, tag, from, to) => sql(database, `INSERT dbo.NhuCauDongHanh(MaTaiKhoan,NoiDung,DiemDen,NgayBatDau,NgayKetThuc,HinhThucDi,MongMuonDongHanh,PhamViChiaSe,ThoiDiemDang,TrangThaiCongBo) VALUES(${acc},N'QA-match-${tag}-${stamp}',N'${dest}','${iso(from)}','${iso(to)}',N'Nghỉ dưỡng',N'Đi chậm','CongKhai',SYSUTCDATETIME(),'DangCongBo');`);
  mkNeed(B, 'B', 20, 24); mkNeed(A, 'A', 22, 26);
  const needB = json(database, `SELECT TOP(1) MaNhuCau Id FROM dbo.NhuCauDongHanh WHERE NoiDung=N'QA-match-B-${stamp}' FOR JSON PATH;`)[0].Id;

  const m1 = await api(a, tA, 'GET', '/api/mxh/phu-hop');
  const match = m1.body.find(m => m.needId === needB);
  check(m1.status === 200 && match && match.person.id === B && match.because.includes(dest) && match.destination === dest, 'Goi y "phu hop voi chuyen cua ban": cung diem den, giao ngay');
  check(m1.body.every(m => m.person.id !== A), 'Goi y khong co chinh minh');
  check((await api(provider, tP, 'GET', '/api/mxh/phu-hop')).status === 403, 'Nha cung cap khong dung goi y ban dong hanh');
  const noNeed = await api(b, tB, 'GET', '/api/mxh/phu-hop');
  check(noNeed.status === 200 && noNeed.body.some(m => m.person.id === A), 'Phia kia cung nhan duoc goi y nguoc lai');

  const search = await a.get('/dong-hanh/tim-kiem/tim-nhu-cau?noiDen=' + encodeURIComponent(dest));
  check(search.status === 200 && search.html.includes(`/tin-nhan/nguoi/${B}?nhuCau=${needB}`) && search.html.includes('data-companion-matches'), 'Ket qua tim ban co nut "Nhan tin & ru di cung" va khung goi y');
  const g = await guest.get('/dong-hanh/tim-kiem/tim-nhu-cau?noiDen=' + encodeURIComponent(dest));
  check(g.status === 200 && !g.html.includes('/tin-nhan/nguoi/') && !g.html.includes('data-companion-matches'), 'Khach chua dang nhap khong thay nut nhan tin / goi y');
  check((await a.get(`/tin-nhan/nguoi/${B}?nhuCau=${needB}`)).html.includes('data-chat-connect'), 'Nut dan vao chat voi form "Ru di cung" chon san chuyen');

  // Ru di cung trong chat -> hai ben dong y -> thanh ban + nhom chuyen di
  const invite = await api(a, tA, 'POST', '/chat/ru', fd({ PeerId: B, NeedId: needB, Text: 'Mình cũng đi Đà Lạt, đi chung nhé' }));
  check(invite.status === 200 && invite.body.message.kind === 'Connection', 'Ru di cung bang the trong chat');
  const cid = invite.body.conversationId, kid = invite.body.message.connection;
  check((await api(a, tA, 'GET', '/api/mxh/phu-hop')).body.every(m => m.needId !== needB), 'Loi ru da gui khong con trong goi y');
  const fCard = (await api(b, tB, 'GET', '/chat/' + cid)).body.connections.find(c => c.id === kid);
  check(fCard && fCard.otherId === A && fCard.actions.includes('DaChapNhan'), 'The ru co ma nguoi con lai (otherId) de lap nhom');
  check((await api(a, tA, 'GET', '/api/mxh/ban-be')).body.every(p => p.id !== B), 'Truoc khi dong y chua la ban be');
  check((await api(b, tB, 'POST', `/chat/${cid}/ket-noi/hanh-dong`, fd({ ConnectionId: kid, Decision: 'DaChapNhan' }))).status === 200, 'Nguoi kia dong y di cung');
  check((await api(a, tA, 'GET', '/api/mxh/ban-be')).body.some(p => p.id === B && p.relation === 'friends') && (await api(b, tB, 'GET', '/api/mxh/ban-be')).body.some(p => p.id === A), 'Dong y di cung tu dong thanh ban be hai chieu');
  const aCard = (await api(a, tA, 'GET', '/chat/' + cid)).body.connections.find(c => c.id === kid);
  check(aCard.status === 'DaChapNhan' && aCard.otherId === B, 'The hien da ket noi, otherId la nguoi ban dong hanh');
  const group = await api(a, tA, 'POST', '/chat/nhom', fd({ Title: 'Đi ' + dest, MemberIds: [aCard.otherId] }));
  check(group.status === 200, 'Lap nhom chat chuyen di tu the da ket noi');
  const gt = (await api(b, tB, 'GET', '/chat/' + group.body.conversationId)).body;
  check(gt.item.group && gt.item.who === 'Đi ' + dest && gt.members.length === 2, 'Nguoi ban dong hanh co mat trong nhom chuyen di');

  // =========================== DAT DICH VU ===========================
  const pair = json(database, `SELECT TOP(1) l.ServiceId,l.BranchId,s.TenDichVu,s.GiaThamKhao,s.DonViGia,b.Name BranchName,b.Address FROM dbo.DichVuCoSo l JOIN dbo.DichVuDuLich s ON s.MaDichVu=l.ServiceId JOIN dbo.CoSoDichVu b ON b.Id=l.BranchId JOIN dbo.NhaCungCapDichVu p ON p.MaNhaCungCap=b.ProviderId JOIN dbo.TaiKhoan t ON t.MaTaiKhoan=p.MaTaiKhoan WHERE t.Email=N'${providerEmail}' AND b.Status='Approved' AND b.PublishedVersion IS NOT NULL AND s.TrangThaiTiepNhan='TiepNhan' AND s.TrangThaiCongBo='DaDuyet' ORDER BY l.ServiceId FOR JSON PATH;`)[0];
  assert.ok(pair, 'Can mot dich vu da duyet gan co so');
  const { ServiceId: sid, BranchId: bid } = pair;
  sql(database, `DELETE FROM dbo.BaoCaoChat; DELETE FROM dbo.PhanUngTin; UPDATE dbo.TinNhanChat SET ReplyToId=NULL WHERE ReplyToId IS NOT NULL; DELETE m FROM dbo.TinNhanChat m JOIN dbo.CuocTroChuyen c ON c.Id=m.ConversationId WHERE c.BranchId=${bid}; DELETE FROM dbo.NhomChatChuyenDi WHERE ConversationId IN (SELECT Id FROM dbo.CuocTroChuyen WHERE BranchId=${bid}); DELETE FROM dbo.CuocTroChuyen WHERE BranchId=${bid}; DELETE FROM dbo.CaiDatChatCoSo WHERE BranchId=${bid};`);

  const q = (extra = {}) => new URLSearchParams({ BranchId: bid, ServiceId: sid, Start: iso(40), End: iso(42), People: 3, Quantity: 2, ...extra }).toString();
  const chk = await api(a, tA, 'GET', '/chat/dat/kiem-tra?' + q());
  const expected = estimate(pair.GiaThamKhao, pair.DonViGia, 3, 2, iso(40), iso(42));
  check(chk.status === 200 && chk.body.ok && chk.body.estimate === expected && chk.body.price === pair.GiaThamKhao, 'Kiem tra truoc khi dat: tam tinh dung cong thuc (' + expected + ')');
  check(Array.isArray(chk.body.warnings) && chk.body.warnings.every(w => !w.startsWith('Gửi yêu cầu không bảo đảm')), 'Canh bao tra ve khong lap cau chung cua he thong');
  const past = await api(a, tA, 'GET', '/chat/dat/kiem-tra?' + q({ Start: iso(-5), End: iso(-3) }));
  check(past.status === 200 && past.body.ok === false && past.body.message, 'Ngay trong qua khu: bao loi ro rang truoc khi gui');
  check((await api(a, tA, 'GET', '/chat/dat/kiem-tra?' + q({ ServiceId: 99999999 }))).status === 422, 'Dich vu khong thuoc co so bi tu choi');
  check((await api(provider, tP, 'GET', '/chat/dat/kiem-tra?' + q())).status === 403, 'Nha cung cap khong dung kiem tra dat cho');
  check([302, 401].includes((await api(guest, tA, 'GET', '/chat/dat/kiem-tra?' + q())).status), 'Khach vang lai khong kiem tra duoc');

  const sub = crypto.randomUUID();
  const sent = await api(a, tA, 'POST', '/chat/dat', fd({ BranchId: bid, ServiceId: sid, Start: iso(40), End: iso(42), People: 3, Quantity: 2, Notes: 'Gần cửa sổ', SubmissionId: sub }));
  check(sent.status === 200 && sent.body.message.kind === 'Booking', 'Gui yeu cau dat trong chat');
  const bcid = sent.body.conversationId, rid = sent.body.message.booking;
  const cost = json(database, `SELECT ChiPhiDuKien C FROM dbo.YeuCauDatDichVu WHERE MaYeuCau=${rid} FOR JSON PATH;`)[0].C;
  check(cost === expected, 'Chi phi du kien luu theo tam tinh cua may chu (khong tin gia tu trinh duyet)');

  const th = (await api(provider, tP, 'GET', '/chat/' + bcid)).body;
  check(th.context && th.context.id === bid && th.context.name === pair.BranchName && th.context.url === '/co-so/' + bid && th.context.address === pair.Address, 'Ngu canh co so ghim dau cuoc tro chuyen (phia nha cung cap)');
  const tth = (await api(a, tA, 'GET', '/chat/' + bcid)).body;
  check(tth.context && tth.context.name === pair.BranchName, 'Ngu canh co so ghim dau cuoc tro chuyen (phia khach)');
  // Danh sach tin nhan cua nha cung cap chi gom co so dang chon: chon co so cua cuoc tro chuyen truoc.
  { const bid = json(database, `SELECT BranchId B FROM dbo.CuocTroChuyen WHERE Id=${bcid} FOR JSON PATH;`)[0].B; const feedPage = await provider.get('/bang-tin'); await provider.post('/nha-cung-cap/co-so/chon', feedPage, { id: bid, returnUrl: '/nha-cung-cap/tin-nhan' }); }
  const pending = async (c, t, filter) => (await api(c, t, 'GET', '/chat/danh-sach' + (filter ? '?loc=' + filter : ''))).body.find(x => x.id === bcid);
  check((await pending(provider, tP)).pending === 1 && (await pending(a, tA)).pending === 0, 'Nha cung cap thay 1 yeu cau cho xac nhan; khach khong co gi can lam');
  check(!!(await pending(provider, tP, 'booking')), 'Bo loc "Cho xac nhan" giu cuoc tro chuyen nay');
  await api(provider, tP, 'POST', `/chat/${bcid}/dat/hanh-dong`, fd({ RequestId: rid, Decision: 'DeXuatKhac', Text: 'Giảm 10% cho nhóm 3 người', Cost: Math.round(expected * 0.9), People: 3 }));
  check((await pending(provider, tP)).pending === 0 && (await pending(a, tA)).pending === 1, 'Sau de xuat khac: khach la nguoi can phan hoi');
  check(!(await pending(provider, tP, 'booking')), 'Bo loc "Cho xac nhan" khong con giu cuoc tro chuyen khi dang cho khach');
  await api(a, tA, 'POST', `/chat/${bcid}/dat/hanh-dong`, fd({ RequestId: rid, Decision: 'KhachDongY' }));
  check((await pending(provider, tP)).pending === 0 && (await pending(a, tA)).pending === 0, 'Da thong nhat: khong con viec cho');

  // Lien ket giua chat va trang dat cho
  const toChat = await a.get('/tin-nhan/dat-cho/' + rid);
  check(toChat.status === 302 && toChat.location === '/tin-nhan/' + bcid, 'Tu yeu cau dat mo dung cuoc tro chuyen (khach)');
  check((await provider.get('/tin-nhan/dat-cho/' + rid)).location === '/tin-nhan/' + bcid, 'Tu yeu cau dat mo dung cuoc tro chuyen (nha cung cap)');
  check((await other.get('/tin-nhan/dat-cho/' + rid)).location === '/Bookings/Detail/' + rid, 'Nguoi khong lien quan khong duoc dua vao cuoc tro chuyen');
  const detail = await a.get('/Bookings/Detail/' + rid);
  check(detail.status === 200 && (detail.html.includes('/tin-nhan/' + bcid) || detail.html.includes('/tin-nhan/dat-cho/' + rid)), 'Trang chi tiet dat cho co nut "Mo tro chuyen"');
  const list = await a.get('/tin-nhan/' + bcid);
  check(list.status === 200 && list.html.includes('data-chat-context'), 'Trang chat co vung ngu canh');

  console.log(`PASS ${results.length} chat-apps checks.`);
})().catch(e => { console.error(e); process.exit(1); });
