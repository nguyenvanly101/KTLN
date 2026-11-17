// Sinh SQL nhap anh Wikimedia da duyet va (tuy chon) chep anh vao wwwroot/anh-tra-cuu.
// Chay: node tools/wikimedia-photos/build-sql.cjs <thu-muc-ra> [--loai=T12,M345] [--chep]
// --loai: ma anh khop sai can bo (T = tinh/thanh, M = muc tra cuu), lay tu xem-truoc.html.
// Anh trung (cung tinh o dia gioi cu va moi, hoac cung dia diem) duoc giu; loai bang --loai neu khop sai.
const fs = require('fs'), path = require('path');
const args = process.argv.slice(2); const out = args.find(a => !a.startsWith('--'));
if (!out) { console.error('Thieu thu muc ra'); process.exit(1); }
const skip = new Set(((args.find(a => a.startsWith('--loai=')) || '').slice(7)).split(',').map(s => s.trim().toUpperCase()).filter(Boolean));
const copy = args.includes('--chep');
const rows = JSON.parse(fs.readFileSync(path.join(out, 'staging.json'), 'utf8'))
  .filter(r => !skip.has((r.target === 'unit' ? 'T' : 'M') + r.id));
const sq = s => "N'" + String(s).split("'").join("''") + "'";
const web = '/anh-tra-cuu/';
let s = '-- Anh Wikimedia da duyet: ' + rows.length + ' anh. Chi gan cho muc chua co anh (khong de anh do co so cung cap).\r\nSET XACT_ABORT ON;\r\nBEGIN TRANSACTION;\r\n';
for (const r of rows.filter(r => r.target === 'entry'))
  s += `UPDATE dbo.DiscoveryEntry SET ImagePath=${sq(web + r.image)},ImageCredit=${sq(r.credit)},ImageSourceUrl=${sq(r.sourceUrl)} WHERE Id=${+r.id} AND ImagePath IS NULL;\r\n`;
for (const r of rows.filter(r => r.target === 'unit'))
  s += `MERGE dbo.AnhDonVi AS t USING (SELECT ${+r.id} DonViId) AS x ON t.DonViId=x.DonViId WHEN MATCHED THEN UPDATE SET ImagePath=${sq(web + r.image)},Credit=${sq(r.credit)},SourceUrl=${sq(r.sourceUrl)},UpdatedAt=SYSUTCDATETIME() WHEN NOT MATCHED THEN INSERT(DonViId,ImagePath,Credit,SourceUrl) VALUES(${+r.id},${sq(web + r.image)},${sq(r.credit)},${sq(r.sourceUrl)});\r\n`;
s += 'COMMIT TRANSACTION;\r\n';
fs.writeFileSync(path.join(out, 'wiki-anh.sql'), '﻿' + s);
if (copy) {
  const dest = path.join(__dirname, '../../src/HoTroDuLich.Web/wwwroot/anh-tra-cuu'); fs.mkdirSync(dest, { recursive: true });
  for (const r of rows) fs.copyFileSync(path.join(out, 'anh', r.image), path.join(dest, r.image));
  console.log('Da chep ' + rows.length + ' anh vao ' + dest);
}
console.log('Da ghi ' + path.join(out, 'wiki-anh.sql') + ': ' + rows.filter(r => r.target === 'entry').length + ' muc, ' + rows.filter(r => r.target === 'unit').length + ' tinh/thanh.');
