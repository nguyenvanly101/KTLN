// Anh cho Kham pha: anh co so / dich vu do nha cung cap tai duoc dua vao the tra cuu, anh Wikimedia hien kem ghi cong,
// anh tinh/thanh lay tu AnhDonVi, quan tri go duoc anh khop sai, nha cung cap duoc nhac them anh.
// Chay: FULLTEXT_TEST_BASE=http://127.0.0.1:5227 BRANCH_TEST_DB=HoTroDuLich_Branches_20261108_Final node tests/photo-sources.cjs
const fs = require('node:fs'), assert = require('node:assert/strict');
const { Client, val, base, decode } = require('./fulltext-smoke.cjs');
const { database, json, sql } = require('./branches-sql.cjs');
const fixture = JSON.parse(fs.readFileSync('preview/branches-fixture.json', 'utf8'));
const user = label => fixture.users.find(x => x.label === label);
const results = []; const check = (ok, n) => { assert.ok(ok, n); results.push(n); };
const wait = ms => new Promise(r => setTimeout(r, ms));
const today = new Date(Date.now() + 7 * 36e5).toISOString().slice(0, 10);

(async () => {
  for (let n = 0; ; n++) { try { if ((await fetch(base + '/api/maps/config')).ok) break; } catch { } if (n > 45) throw Error('not ready'); await wait(1000); }
  const guest = new Client(), admin = new Client(), provider = new Client();
  await admin.login(user('admin').email, 'BranchTest@2026'); await provider.login(user('provider').email, 'BranchTest@2026');

  // 1. Co so co anh -> muc tra cuu cua co so dung anh dau tien (worker chay moi phut)
  const branch = json(database, "SELECT TOP 1 e.Id EntryId, e.BranchId, e.Title FROM dbo.DiscoveryEntry e JOIN dbo.CoSoDichVu b ON b.Id=e.BranchId JOIN dbo.NhaCungCapDichVu p ON p.MaNhaCungCap=b.ProviderId JOIN dbo.TaiKhoan t ON t.MaTaiKhoan=p.MaTaiKhoan WHERE e.Status='Published' AND b.Status='Approved' AND b.PublishedVersion IS NOT NULL AND p.TrangThaiQuyenHoatDong='DangHoatDong' AND p.TrangThaiXacMinh='DaXacMinh' AND t.TrangThai='HoatDong' AND b.PhotosJson IS NOT NULL AND b.PhotosJson<>'[]' ORDER BY e.Id FOR JSON PATH;")[0];
  check(!!branch, 'Co du lieu thu: co so cong khai co anh');
  const stale = json(database, "SELECT TOP 1 e.Id FROM dbo.DiscoveryEntry e JOIN dbo.CoSoDichVu b ON b.Id=e.BranchId WHERE b.Status<>'Approved' AND b.PhotosJson IS NOT NULL AND b.PhotosJson<>'[]' ORDER BY e.Id FOR JSON PATH;")[0];
  let img = null;
  for (let n = 0; n < 90 && img !== '/co-so/' + branch.BranchId + '/anh/0'; n++) { img = json(database, `SELECT ImagePath P FROM dbo.DiscoveryEntry WHERE Id=${branch.EntryId} FOR JSON PATH;`)[0].P; if (img !== '/co-so/' + branch.BranchId + '/anh/0') await wait(1000); }
  check(img === '/co-so/' + branch.BranchId + '/anh/0', 'Muc tra cuu cua co so dung anh co so tai len');
  if (stale) check(json(database, `SELECT ImagePath P FROM dbo.DiscoveryEntry WHERE Id=${stale.Id} FOR JSON PATH;`)[0].P == null, 'Co so khong con cong khai thi muc tra cuu khong giu anh (tranh anh hong)');
  const photo = await fetch(base + '/co-so/' + branch.BranchId + '/anh/0');
  check(photo.status === 200 && (photo.headers.get('content-type') || '').startsWith('image/'), 'Duong dan anh cua the tra cuu tra ve anh that cua co so'); await photo.arrayBuffer();

  // 2. Anh Wikimedia tren mot dia diem: trang chi tiet hien anh va ghi cong
  const place = json(database, "SELECT TOP 1 Id FROM dbo.DiscoveryEntry WHERE Status='Published' AND Kind='Place' AND ImagePath IS NULL ORDER BY Id FOR JSON PATH;")[0];
  const credit = 'Ảnh: Người chụp thử · CC BY-SA 4.0 · Wikimedia Commons';
  sql(database, `UPDATE dbo.DiscoveryEntry SET ImagePath=N'/images/dalat.jpg',ImageCredit=N'${credit}',ImageSourceUrl=N'https://commons.wikimedia.org/wiki/File:Thu.jpg' WHERE Id=${place.Id};`);
  try {
    const detail = decode((await guest.get('/Discovery/Entry/' + place.Id)).html);
    check(detail.includes('class="entry-photo"') && detail.includes('/images/dalat.jpg') && detail.includes(credit) && detail.includes('https://commons.wikimedia.org/wiki/File:Thu.jpg'), 'Trang chi tiet hien anh Wikimedia kem tac gia, giay phep va lien ket nguon');

    // 3. Quan tri go anh khop sai
    const edit = await admin.get('/Discovery/Edit/' + place.Id); const eh = decode(edit.html);
    check(eh.includes('id="ds-photo"') && eh.includes(credit), 'Trang sua cua quan tri hien anh hien tai va ghi cong');
    const e = json(database, `SELECT Title,Kind,Region,Summary,Body,SourceName,SourceUrl FROM dbo.DiscoveryEntry WHERE Id=${place.Id} FOR JSON PATH;`)[0];
    const saved = await admin.post('/Discovery/Save', edit, { Id: place.Id, Version: val(edit, 'Version'), Title: e.Title, Kind: e.Kind, Region: e.Region, Summary: e.Summary, Content: e.Body || '', SourceName: e.SourceName, SourceUrl: e.SourceUrl || '', CheckedOn: today, Status: 'Published', Reason: 'Gỡ ảnh khớp sai', RemovePhoto: 'true' });
    const after = json(database, `SELECT ImagePath P, ImageCredit C FROM dbo.DiscoveryEntry WHERE Id=${place.Id} FOR JSON PATH;`)[0];
    check(saved.status === 302 && after.P == null && after.C == null, 'Quan tri go anh: anh va ghi cong bi xoa');
    check(!decode((await guest.get('/Discovery/Entry/' + place.Id)).html).includes('class="entry-photo"'), 'Trang chi tiet khong con anh da go');
  } finally { sql(database, `UPDATE dbo.DiscoveryEntry SET ImagePath=NULL,ImageCredit=NULL,ImageSourceUrl=NULL WHERE Id=${place.Id};`); }

  // 4. Anh tinh/thanh tu AnhDonVi: hero va the diem den
  const unit = json(database, "SELECT TOP 1 d.Id FROM dbo.DonViHanhChinh d WHERE d.[Level]=1 AND d.Status='Active' AND EXISTS (SELECT 1 FROM dbo.DiscoveryEntry e WHERE e.Status='Published' AND e.DonViId=d.Id) AND NOT EXISTS (SELECT 1 FROM dbo.AnhDonVi a WHERE a.DonViId=d.Id) ORDER BY d.Id FOR JSON PATH;")[0];
  const uc = 'Ảnh: Tỉnh thử · CC BY 2.0 · Wikimedia Commons';
  sql(database, `INSERT dbo.AnhDonVi(DonViId,ImagePath,Credit,SourceUrl) VALUES(${unit.Id},N'/images/stay.jpg',N'${uc}',N'https://commons.wikimedia.org/wiki/File:Tinh.jpg');`);
  try {
    const page = decode((await guest.get('/diem-den/' + unit.Id)).html);
    check(page.includes('/images/stay.jpg') && page.includes('class="photo-credit"') && page.includes(uc), 'Hero tinh/thanh dung anh AnhDonVi va hien ghi cong');
    const list = decode((await guest.get('/diem-den')).html);
    check(list.includes('href="/diem-den/' + unit.Id + '"') && list.includes('src="/images/stay.jpg"'), 'The tinh/thanh trong danh sach diem den dung anh AnhDonVi');
  } finally { sql(database, `DELETE FROM dbo.AnhDonVi WHERE DonViId=${unit.Id};`); }

  // 5. Nha cung cap duoc nhac them anh khi co so dang chon chua co anh
  const feed = decode((await provider.get('/bang-tin')).html);
  const cur = /Đang quản lý cơ sở<\/small><strong>([^<]*)</.exec(feed);
  check(feed.includes('Thêm ảnh thực tế cho cơ sở'), 'Viec can lam cua nha cung cap co muc "Them anh thuc te cho co so"' + (cur ? ' (' + cur[1] + ')' : ''));
  console.log(`PASS ${results.length} photo-sources checks.`);
})().catch(e => { console.error(e); process.exit(1); });
