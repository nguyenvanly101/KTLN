// Nha cung cap chi thay va dung du lieu cua co so dang chon: yeu cau dat, tin nhan, dich vu.
// Mo yeu cau / cuoc tro chuyen cua co so khac (vd tu thong bao) thi tu chuyen sang co so do.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/branch-scope.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, base, decode } = require('./fulltext-smoke.cjs');
const { database, json } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
const prov = fixture.users.find(x => x.label === 'provider');
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const cookie = c => [...c.cookies].map(([k, v]) => k + '=' + v).join('; ');

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { } if (n > 60) throw Error('not ready'); await new Promise(r => setTimeout(r, 1000)); }
  const p = new Client(); await p.login(prov.email, 'BranchTest@2026');
  // Hai co so cua cung nha cung cap, moi co so co yeu cau dat rieng
  const two = json(database, `SELECT TOP 2 s.BranchId Id, MAX(s.RequestId) Req FROM dbo.YeuCauDatCoSo s JOIN dbo.CoSoDichVu b ON b.Id=s.BranchId WHERE b.ProviderId=${prov.providerId} GROUP BY s.BranchId ORDER BY COUNT(*) DESC FOR JSON PATH;`);
  check(two.length === 2, 'Co du lieu thu: hai co so cung nha cung cap, moi co so co yeu cau');
  const [A, B] = two;
  const choose = async id => { const page = await p.get('/bang-tin'); return p.post('/nha-cung-cap/co-so/chon', page, { id, returnUrl: '/Bookings' }); };

  // 1. Danh sach yeu cau chi co co so dang chon
  await choose(A.Id);
  const all = loc => p.get('/Bookings?loc=' + loc).then(r => decode(r.html));
  const listA = (await Promise.all(['can-tra-loi', 'sap-toi', 'da-dung', 'da-huy'].map(all))).join('');
  check(listA.includes('#' + A.Req) && !listA.includes('#' + B.Req + '<') && !listA.includes('#' + B.Req + ' '), 'Dang chon co so A: danh sach chi co yeu cau cua A');
  const shownA = [...listA.matchAll(/#(\d+)<\/span>/g)].map(m => +m[1]);
  const otherBranch = shownA.length ? json(database, `SELECT COUNT(*) N FROM dbo.YeuCauDatCoSo WHERE RequestId IN (${shownA.join(',')}) AND BranchId<>${A.Id} FOR JSON PATH;`)[0].N : 0;
  check(shownA.length > 0 && otherBranch === 0, 'Khong yeu cau nao cua co so khac lot vao danh sach');
  check(decode((await p.get('/Bookings')).html).includes('cơ sở'), 'Tieu de danh sach noi ro dang xem co so nao');

  // 2. Mo yeu cau cua co so B (vd tu thong bao) -> tu chuyen sang B
  const open = decode((await p.get('/Bookings/Detail/' + B.Req)).html);
  check(open.includes('Đã chuyển sang cơ sở'), 'Mo yeu cau cua co so khac: website bao da chuyen sang co so do');
  const listB = (await Promise.all(['can-tra-loi', 'sap-toi', 'da-dung', 'da-huy'].map(all))).join('');
  check(listB.includes('#' + B.Req) && !listB.includes('#' + A.Req + '<'), 'Sau khi chuyen, danh sach la cua co so B');
  check(!decode((await p.get('/Bookings/Detail/' + B.Req)).html).includes('Đã chuyển sang cơ sở'), 'Mo lai yeu cau cua co so dang chon thi khong chuyen nua');

  // 3. Tin nhan: chi cuoc tro chuyen cua co so dang chon, khong con o chon "Moi co so"
  const chats = await (await fetch(base + '/chat/danh-sach', { headers: { cookie: cookie(p) } })).json();
  check(Array.isArray(chats) && chats.every(c => c.branchId === B.Id || c.group), 'Danh sach tin nhan chi gom cuoc tro chuyen cua co so dang chon');
  const inbox = decode((await p.get('/nha-cung-cap/tin-nhan')).html);
  check(!inbox.includes('Mọi cơ sở'), 'Hop thu nha cung cap khong con lua chon "Moi co so"');

  // 4. Dich vu: trang dich vu chi co dich vu gan voi co so dang chon
  const svcB = json(database, `SELECT x.ServiceId Id FROM dbo.DichVuCoSo x WHERE x.BranchId=${B.Id} FOR JSON PATH;`).map(r => r.Id);
  const svcOther = json(database, `SELECT TOP 1 x.ServiceId Id FROM dbo.DichVuCoSo x JOIN dbo.CoSoDichVu b ON b.Id=x.BranchId WHERE b.ProviderId=${prov.providerId} AND x.BranchId<>${B.Id} AND x.ServiceId NOT IN (SELECT ServiceId FROM dbo.DichVuCoSo WHERE BranchId=${B.Id}) FOR JSON PATH;`)[0];
  const svcPage = decode((await p.get('/Provider')).html);
  check(!svcOther || !svcPage.includes('/Provider/Edit/' + svcOther.Id + '"'), 'Trang dich vu khong hien dich vu cua co so khac');
  console.log(`PASS ${results.length} branch-scope checks.` + (svcB.length ? '' : ' (co so B chua co dich vu)'));
})().catch(e => { console.error(e); process.exit(1); });
