// Lap ke hoach mot trang (/lapkehoach, /lapkehoach/{id}): luu mot lan, sua, thanh vien cung sua, nhom chat tu dong, xoa, danh sach va trang xem.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/flow-v10.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, base, decode } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
const user = label => fixture.users.find(x => x.label === label);
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const day = n => new Date(Date.now() + n * 864e5).toISOString().slice(0, 10);
const stamp = Date.now().toString().slice(-7);
const guestForApi = () => ({ cookies: new Map() });
const form = (token, values) => { const f = new FormData(); f.append('__RequestVerificationToken', token); for (const [k, v] of Object.entries(values)) if (v != null) f.append(k, String(v)); return f; };
const api = async (c, token, path, method = 'GET', body) => {
  const r = await fetch(base + path, { method, redirect: 'manual', headers: { cookie: [...c.cookies].map(([k, v]) => k + '=' + v).join('; '), RequestVerificationToken: token, ...(body ? { 'content-type': 'application/json' } : {}) }, body: body ? JSON.stringify(body) : undefined });
  let j = null; try { j = await r.json(); } catch { } return { status: r.status, body: j };
};

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { } if (n > 45) throw Error('not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const t = new Client(), other = new Client(), stranger = new Client(), guest = new Client();
  const mk = (tag) => { const email = `pe-${tag}-${stamp}@example.com`; sql(database, `INSERT dbo.TaiKhoan(Email,SoDienThoai,MatKhauBam,VaiTro,TrangThai,DauPhien,PhaiDoiMatKhau,ThoiDiemTao) SELECT N'${email}',NULL,MatKhauBam,VaiTro,TrangThai,NEWID(),0,SYSUTCDATETIME() FROM dbo.TaiKhoan WHERE Email=N'${user('traveler').email}';`); const id = json(database, `SELECT MaTaiKhoan Id FROM dbo.TaiKhoan WHERE Email=N'${email}' FOR JSON PATH;`)[0].Id; sql(database, `INSERT dbo.HoSoNguoiDung(MaTaiKhoan,TenHienThi,ThongTinChiaSe,ThoiDiemCapNhat) VALUES(${id},N'Bạn thử ${tag} ${stamp}','CongKhai',SYSUTCDATETIME());`); return { email, id }; };
  const memAcc = mk('thanhvien'), strAcc = mk('nguoila');
  await t.login(user('traveler').email, 'BranchTest@2026'); await other.login(memAcc.email, 'BranchTest@2026'); await stranger.login(strAcc.email, 'BranchTest@2026');

  // 1. Trang tao /lapkehoach
  const page = await t.get('/lapkehoach'); const ph = decode(page.html);
  check(page.status === 200 && ph.indexOf('id="pe-name"') < ph.indexOf('id="pe-dest"') && ph.includes('Chọn trên bản đồ') && ph.includes('Chọn chi tiết') && ph.includes('Phường / Xã') && ph.includes('Khu du lịch và địa điểm') && ph.includes('Lịch trình theo ngày'), 'Trang tao mot trang: Ten o dau, diem den co chon ban do / xem khu du lich, lich theo ngay');
  check(!ph.includes('Bước 1') && !ph.includes('Tiếp tục'), 'Khong con chia buoc');
  check((await guest.get('/lapkehoach')).status >= 300, 'Khach chua dang nhap khong vao duoc');
  check((await t.get('/lap-ke-hoach')).status === 301 && (await t.get('/ke-hoach/1/lap')).status === 301, 'Duong dan cu chuyen sang /lapkehoach');
  const tk = val(page, '__RequestVerificationToken');

  // 2. Luu moi
  const name = 'QA Mot trang ' + stamp;
  const draft = { notes: [{ day: 1, text: 'Nhớ mang áo mưa' }, { day: 2, text: '  ' }], id: 0, name, destination: 'Hội An', start: day(6), days: 3, budget: 5000000, items: [
    { id: 0, day: 1, time: '14:00', content: 'Nhận phòng homestay', cost: 1440000 },
    { id: 0, day: 1, time: '12:00', content: 'Ăn trưa cao lầu', cost: 60000 },
    { id: 0, day: 2, time: '09:00', content: 'Tham quan Mỹ Sơn', cost: 150000 },
    { id: 0, day: 3, time: null, content: 'Mua quà', cost: null }] };
  const bad = [
    [{ ...draft, name: '  ' }, 'Thieu ten'], [{ ...draft, destination: '' }, 'Thieu diem den'], [{ ...draft, days: 40 }, 'So ngay > 30'], [{ ...draft, start: day(-4) }, 'Ngay di trong qua khu'],
    [{ ...draft, items: [{ id: 0, day: 9, content: 'X' }] }, 'Hang muc ngoai cac ngay'], [{ ...draft, items: [{ id: 0, day: 1, content: 'X', time: '25:99' }] }, 'Gio sai dinh dang'], [{ ...draft, items: [{ id: 0, day: 1, content: '' }] }, 'Hang muc khong ten'], [{ ...draft, budget: -5 }, 'Ngan sach am'] ];
  for (const [b, n] of bad) check((await api(t, tk, '/api/lapkehoach', 'POST', b)).status === 422, n + ' bi tu choi');
  check((await api(t, '', '/api/lapkehoach', 'POST', draft)).status === 400, 'Thieu ma chong gia mao bi tu choi');
  const created = await api(t, tk, '/api/lapkehoach', 'POST', draft);
  check(created.status === 200 && created.body.id > 0, 'Luu chuyen di cung lich trinh trong mot lan');
  const id = created.body.id;
  const plan = json(database, `SELECT TenKeHoach T, NoiDen D, NganSachDuKien B, DATEDIFF(day,NgayBatDau,NgayKetThuc) N FROM dbo.KeHoachChuyenDi WHERE MaKeHoach=${id} FOR JSON PATH;`)[0];
  const cnt = json(database, `SELECT COUNT(*) N FROM dbo.HoatDongLichTrinh WHERE MaKeHoach=${id} FOR JSON PATH;`)[0].N;
  check(plan.T === name && plan.D === 'Hội An' && plan.B === 5000000 && plan.N === 2 && cnt === 4, 'Du lieu luu dung: ten, diem den, ngan sach, 3 ngay, 4 hang muc');

  check(json(database, `SELECT COUNT(*) N FROM dbo.GhiChuNgayChuyenDi WHERE PlanId=${id} AND DayNo=1 AND Note=N'Nhớ mang áo mưa' FOR JSON PATH;`)[0].N === 1 && json(database, `SELECT COUNT(*) N FROM dbo.GhiChuNgayChuyenDi WHERE PlanId=${id} FOR JSON PATH;`)[0].N === 1, 'Ghi chu theo ngay duoc luu (ghi chu trong bi bo qua)');
  check((await api(t, tk, '/api/lapkehoach', 'POST', { ...draft, notes: [{ day: 9, text: 'x' }] })).status === 422, 'Ghi chu ngoai cac ngay bi tu choi');

  // 3. Trang sua /lapkehoach/{id}
  const edit = await t.get('/lapkehoach/' + id); const eh = decode(edit.html);
  check(edit.status === 200 && eh.includes('Sửa kế hoạch') && eh.includes('Trưởng nhóm'), 'Trang sua nap lai noi dung da luu va hien truong nhom');
  const loaded = JSON.parse(/data-draft="([^"]*)"/.exec(edit.html)[1].replace(/&quot;/g, '"').replace(/&amp;/g, '&'));
  check(loaded.notes.length === 1 && loaded.notes[0].day === 1, 'Ban nhap nap lai ghi chu ngay');
  check(!eh.includes('pe-daytabs') && eh.includes('pe-days-list'), 'Trang sua liet ke tung ngay, khong chia tab ngay');
  check(loaded.items.length === 4 && loaded.days === 3 && loaded.items.every(i => i.id > 0), 'Ban nhap co day du hang muc va ma tung hang muc');
  const lunch = loaded.items.find(i => i.content.includes('cao lầu')), moa = loaded.items.find(i => i.content.includes('Mỹ Sơn')), gift = loaded.items.find(i => i.content === 'Mua quà');
  const edited = { ...loaded, items: [
    { ...lunch, cost: 80000 },
    { ...moa, day: 3, time: '08:30' },
    { id: 0, day: 2, time: '19:00', content: 'Tối dạo sông Hoài', cost: 100000 },
    ...loaded.items.filter(i => i.id !== lunch.id && i.id !== moa.id && i.id !== gift.id && i.content.includes('Nhận phòng')) ] };
  const sv = await api(t, tk, '/api/lapkehoach', 'POST', edited);
  check(sv.status === 200 && sv.body.id === id, 'Sua: doi chi phi, chuyen ngay, them moi, xoa hang muc trong mot lan luu');
  const rows = json(database, `SELECT NoiDung C, ChiPhiDuKien P, DATEDIFF(day,(SELECT NgayBatDau FROM dbo.KeHoachChuyenDi WHERE MaKeHoach=${id}),Ngay)+1 D FROM dbo.HoatDongLichTrinh WHERE MaKeHoach=${id} FOR JSON PATH;`);
  check(rows.length === 4 && rows.find(r => r.C.includes('cao lầu')).P === 80000 && rows.find(r => r.C.includes('Mỹ Sơn')).D === 3 && rows.some(r => r.C === 'Tối dạo sông Hoài' && r.D === 2) && !rows.some(r => r.C === 'Mua quà'), 'Du lieu sau khi sua dung');
  check((await api(t, tk, '/api/lapkehoach', 'POST', { ...edited, days: 2 })).status === 422, 'Giam so ngay khi con hang muc o ngay cuoi bi tu choi');
  const renamed = await api(t, tk, '/api/lapkehoach', 'POST', { ...edited, name: name + ' moi', budget: null });
  check(renamed.status === 200 && json(database, `SELECT NganSachDuKien B FROM dbo.KeHoachChuyenDi WHERE MaKeHoach=${id} FOR JSON PATH;`)[0].B == null, 'Doi ten va bo ngan sach duoc');
  check([403, 404].includes((await stranger.get('/lapkehoach/' + id)).status), 'Nguoi la khong mo duoc trang sua');
  check([403, 404].includes((await api(stranger, val(await stranger.get('/lapkehoach'), '__RequestVerificationToken'), '/api/lapkehoach', 'POST', { ...edited, name: 'Hack' })).status), 'Nguoi la khong luu de len chuyen di cua nguoi khac');

  // 4. Chon thay vi nhap: danh sach goi y
  const sug = await api(t, tk, '/api/lapkehoach/goi-y?destination=' + encodeURIComponent('Hội An') + '&tab=explore&group=Stay');
  check(sug.status === 200 && Array.isArray(sug.body.items) && sug.body.items.every(s => s.group === 'Stay'), 'Goi y noi luu tru gan diem den dung nhom');
  const all = await api(t, tk, '/api/lapkehoach/goi-y?destination=' + encodeURIComponent('Hội An'));
  const firstUnsaved = all.body.items.findIndex(x => !x.saved);
  check(all.status === 200 && all.body.items.every(x => typeof x.saved === 'boolean' && 'lat' in x && 'summary' in x && 'priceFrom' in x) && (firstUnsaved < 0 || all.body.items.slice(firstUnsaved).every(x => !x.saved)), 'Mot danh sach duy nhat: muc da luu len truoc, co truong toa do cho ban do');
  check(all.body.items.every(x => (x.serviceId || x.placeId) && x.kind !== 'Destination' && x.kind !== 'Area'), 'Chi cho chon dich vu that hoac dia diem, khong co muc diem den / thong tin tham khao');
  check((await api(t, tk, '/api/lapkehoach/goi-y?destination=' + encodeURIComponent('Hội An') + '&q=zzzkhongco')).body.items.length === 0, 'Tim theo ten loc ra danh sach rong khi khong khop');
  check((await api(t, tk, '/api/lapkehoach/goi-y?Group=Stay&Min=0&Max=900000&Sort=price&Page=1')).status === 200, 'Loc theo gia va sap xep nhu Kham pha');
  check((await api(t, tk, '/api/lapkehoach/goi-y?daLuu=true')).status === 200, 'Chi muc da luu');
  check((await api(t, tk, '/api/lapkehoach/goi-y?Group=Khong')).status === 400, 'Nhom goi y khong hop le bi tu choi');
  const dst = await api(t, tk, '/api/lapkehoach/diem-den');
  check(dst.status === 200 && dst.body.length > 0 && dst.body.some(d => d.name === 'Hội An' || d.name.includes('Hội An')), 'Diem den: co khu du lich va dia diem cua Kham pha');
  const area = await api(t, tk, '/api/lapkehoach/diem-den?q=' + encodeURIComponent('Hà Nội'));
  check(area.status === 200 && area.body.some(d => d.source === 'area' && d.areaId > 0), 'Diem den: tim duoc don vi hanh chinh (co ma khu vuc)');
  check((await api(t, tk, '/api/lapkehoach/dia-diem?q=')).status === 200 && Array.isArray((await api(t, tk, '/api/lapkehoach/dia-diem?q=a')).body), 'Dia diem khac (Goong): chuoi ngan tra ve danh sach rong, khong goi ngoai');
  check((await api(t, tk, '/api/lapkehoach/dia-chi?lat=0&lng=0')).status === 422 && (await api(guestForApi(), '', '/api/lapkehoach/dia-chi?lat=16&lng=106')).status >= 300, 'Dia chi tu toa do: ngoai khung Viet Nam bi tu choi, khach chua dang nhap khong goi duoc');
  const gs = await api(t, tk, '/api/lapkehoach/goong-goi-y?group=Info&destination=x&q=');
  check(gs.status === 200 && Array.isArray(gs.body) && gs.body.length === 0 && (await api(guestForApi(), '', '/api/lapkehoach/goong-goi-y?group=Stay&destination=a')).status >= 300, 'Goi y tu Goong: nhom khong co tu khoa tra ve rong (khong goi ngoai), khach chua dang nhap khong goi duoc');
  const mp = await api(t, tk, '/api/lapkehoach/ban-do');
  check(mp.status === 200 && Array.isArray(mp.body) && mp.body.every(p => typeof p.lat === 'number' && p.name), 'Ban do tra ve cac ghim co ten khu vuc');

  // 5. Thanh vien cung sua + nhom chat tu dong
  const other_id = memAcc.id;
  check(json(database, `SELECT COUNT(*) N FROM dbo.NhomChatChuyenDi WHERE PlanId=${id} FOR JSON PATH;`)[0].N === 0, 'Chuyen di mot nguoi chua co nhom chat');
  const owner_id = json(database, `SELECT MaTaiKhoan M FROM dbo.KeHoachChuyenDi WHERE MaKeHoach=${id} FOR JSON PATH;`)[0].M;
  sql(database, `INSERT dbo.ThanhVienChuyenDi(PlanId,AccountId,InvitedBy,Status,CreatedAt,RespondedAt) VALUES(${id},${other_id},${owner_id},N'Accepted',SYSUTCDATETIME(),SYSUTCDATETIME())`);
  const oe = await other.get('/lapkehoach/' + id);
  if (oe.status !== 200) console.log('DEBUG', oe.status, oe.text.slice(0, 400));
  check(oe.status === 200 && decode(oe.html).includes('Bạn là thành viên'), 'Thanh vien mo duoc trang sua, truong thong tin khoa');
  const otk = val(oe, '__RequestVerificationToken');
  const od = JSON.parse(/data-draft="([^"]*)"/.exec(oe.html)[1].replace(/&quot;/g, '"').replace(/&amp;/g, '&'));
  const mem = await api(other, otk, '/api/lapkehoach', 'POST', { ...od, items: [...od.items, { id: 0, day: 1, time: '20:00', content: 'Thành viên thêm quán', cost: 50000 }] });
  check(mem.status === 200 && json(database, `SELECT COUNT(*) N FROM dbo.HoatDongLichTrinh WHERE MaKeHoach=${id} AND NoiDung=N'Thành viên thêm quán' FOR JSON PATH;`)[0].N === 1, 'Thanh vien sua duoc lich trinh');
  check((await api(other, otk, '/api/lapkehoach', 'POST', { ...od, name: 'Thanh vien doi ten' })).status === 403, 'Thanh vien khong doi duoc ten / ngay / ngan sach');
  const group = json(database, `SELECT l.ConversationId C, c.Title T, c.IsGroup G FROM dbo.NhomChatChuyenDi l JOIN dbo.CuocTroChuyen c ON c.Id=l.ConversationId WHERE l.PlanId=${id} FOR JSON PATH;`)[0];
  check(!!group && group.G === true, 'Nhom chat duoc tao khi co thanh vien');
  const members = json(database, `SELECT AccountId A, Role R FROM dbo.ThanhVienChat WHERE ConversationId=${group.C} AND LeftAt IS NULL FOR JSON PATH;`);
  check(members.length === 2 && members.find(m => m.A === owner_id).R === 'Admin' && members.some(m => m.A === other_id), 'Nhom chat co truong nhom (Admin) va thanh vien');
  check(group.T.includes(name), 'Ten nhom chat theo ten chuyen di');
  const det = decode((await t.get('/Plans/Detail/' + id)).html);
  check(det.includes('Trưởng nhóm') && det.includes('Nhóm chat') && det.includes(`/tin-nhan/${group.C}`), 'Trang xem co truong nhom va nut mo nhom chat');
  // thanh vien tu roi -> roi nhom
  const mtk = val(await other.get('/Plans'), '__RequestVerificationToken');
  const stk = val(await stranger.get('/Plans'), '__RequestVerificationToken');
  const leave = await other.req('/Plans/RemoveMember', form(mtk, { planId: id, accountId: other_id }));
  check(leave.status === 302 && json(database, `SELECT COUNT(*) N FROM dbo.ThanhVienChat WHERE ConversationId=${group.C} AND AccountId=${other_id} AND LeftAt IS NULL FOR JSON PATH;`)[0].N === 0, 'Thanh vien roi chuyen di thi roi luon nhom chat');

  // 6. Trang xem chi tiet + danh sach
  check(det.includes('Sửa') && det.includes('Xác nhận lịch trình') && det.includes('Chia sẻ') && det.includes('Mời bạn') && det.includes('Chia tiền'), 'Trang xem: cac nut Sua, Xac nhan, Chia se, Moi ban, Chia tien o tren cung');
  check(det.indexOf('Xác nhận lịch trình') < det.indexOf('trip-scroll') && det.includes('trip-scroll'), 'Cac nut o tren, lich trinh va thanh vien nam trong lop cuon rieng');
  check(!det.includes('Thêm hạng mục vào lịch trình') && !det.includes('/Plans/DeleteActivity'), 'Trang xem chi doc, khong co thao tac sua tren trang');
  const list = decode((await t.get('/Plans')).html);
  check(list.includes('Xem chi tiết') && list.includes('>Sửa</a>') && list.includes('action="/Plans/Delete"') && list.includes('href="/lapkehoach"'), 'Danh sach: moi the co Xem chi tiet va Xoa, nut tao tro toi /lapkehoach');

  // 7. Xoa
  const ptk = val(await t.get('/Plans'), '__RequestVerificationToken');
  check([403, 404].includes((await stranger.req('/Plans/Delete', form(stk, { id }))).status), 'Nguoi khac khong xoa duoc chuyen di');
  const del = await t.req('/Plans/Delete', form(ptk, { id }));
  check(del.status === 302 && json(database, `SELECT COUNT(*) N FROM dbo.KeHoachChuyenDi WHERE MaKeHoach=${id} FOR JSON PATH;`)[0].N === 0 && json(database, `SELECT COUNT(*) N FROM dbo.HoatDongLichTrinh WHERE MaKeHoach=${id} FOR JSON PATH;`)[0].N === 0, 'Xoa chuyen di cung lich trinh');
  sql(database, `DELETE FROM dbo.ThanhVienChat WHERE ConversationId=${group.C}; DELETE FROM dbo.TinNhanChat WHERE ConversationId=${group.C}; DELETE FROM dbo.CuocTroChuyen WHERE Id=${group.C}`);
  console.log(`PASS ${results.length} flow-v10 checks.`);
})().catch(e => { console.error(e); process.exit(1); });
