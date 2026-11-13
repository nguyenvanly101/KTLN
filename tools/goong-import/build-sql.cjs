// Tao hai tep SQL tu du lieu tam da lay tu Goong: (1) them vao bang DiaDiemDuLich, (2) bo sung toa do/dia chi/nguon cho muc Kham pha.
// Chay: node tools/goong-import/build-sql.cjs <thu-muc-staging> <thu-muc-ra>
const fs = require('fs'), path = require('path');
const [stage, outDir] = process.argv.slice(2); if (!stage || !outDir) { console.error('Dung: build-sql.cjs <staging> <ra>'); process.exit(1); }
const raw = JSON.parse(fs.readFileSync(path.join(stage, 'staging.json'), 'utf8'));
const km = (a, b, c, d) => { const R = 6371, t = Math.PI / 180, x = (c - a) * t, y = (d - b) * t, h = Math.sin(x / 2) ** 2 + Math.cos(a * t) * Math.cos(c * t) * Math.sin(y / 2) ** 2; return 2 * R * Math.asin(Math.sqrt(h)); };
const fold = s => s.normalize('NFD').replace(/[̀-ͯ]/g, '').replace(/đ/g, 'd').replace(/Đ/g, 'D').toLowerCase().replace(/[^a-z0-9 ]/g, ' ').replace(/ +/g, ' ').trim();
const GENERIC = new Set(['khu du lich', 'diem tham quan', 'bai bien', 'thac nuoc', 'chua', 'bao tang', 'di tich lich su', 'cho', 'nha hang', 'khach san', 'quan an', 'ca phe', 'cafe']);
// ten phai chua tu khoa dung loai da tim (Goong khop theo tu nen co nhieu ket qua khong lien quan)
const NEED = {
  'khu du lịch': /du lich/, 'điểm tham quan': /(tham quan|du lich)/, 'bãi biển': /(bai bien|bai tam)/, 'thác nước': /\bthac\b/,
  'chùa': /(chua|tu vien|thien vien|pagoda)/, 'bảo tàng': /bao tang/, 'di tích lịch sử': /(di tich|dinh|den|thanh co|dia dao|nha luu niem|khu luu niem)/,
  'chợ': /cho/, 'nhà hàng đặc sản': /(nha hang|dac san|buffet)/, 'khách sạn': /(khach san|hotel|resort|homestay|villa)/,
};
const BAD = /(bai xe|bai go|ban ve|cong sau|cong truoc|nha xe|khai thac|cung cap|bql|ban quan ly|cua hang|nuoc da|san bong|dai ly|tap hoa|sieu thi|thach cao|son nuoc|quan ca phe|tiem |spa|karaoke|nha nghi)/;
const kept = [];
for (const x of raw) {
  const f = fold(x.name);
  if (f.length < 4 || GENERIC.has(f) || f === fold(x.dest)) continue;
  if (NEED[x.query] && !NEED[x.query].test(f)) continue;
  if (BAD.test(f)) continue;                       // ten qua chung chung
  if (/\b(cong ty|van phong|chi nhanh|dai ly|showroom|truong|benh vien|ngan hang)\b/.test(f)) continue; // khong phai diem du lich
  if (kept.some(k => k.dest === x.dest && (fold(k.name) === f || km(k.lat, k.lng, x.lat, x.lng) < 0.08))) continue; // trung ten hoac trung vi tri
  if (kept.some(k => k.dest === x.dest && k.name === x.name)) continue;
  kept.push(x);
}
const sq = s => "N'" + String(s).replace(/'/g, "''") + "'";
const TAGS = { 'Thiên nhiên': 'thiên nhiên khu du lịch', 'Văn hóa': 'văn hóa tham quan', 'Biển đảo': 'biển bãi tắm', 'Tâm linh': 'chùa tâm linh', 'Lịch sử': 'di tích lịch sử', 'Ăn uống': 'ăn uống nhà hàng', 'Lưu trú': 'lưu trú khách sạn' };
const today = new Date().toISOString().slice(0, 10);
const desc = x => `${x.type === 'Ăn uống' ? 'Nơi ăn uống' : x.type === 'Lưu trú' ? 'Cơ sở lưu trú' : 'Địa điểm ' + x.type.toLowerCase()} tại ${x.dest}. Địa chỉ: ${x.address || 'chưa rõ'}. Thông tin tham khảo từ Goong Maps; giờ mở cửa và giá chưa được xác nhận.`.slice(0, 1400);
let a = '-- Nhap dia diem lay tu Goong Maps (thong tin tham khao). Chay lap an toan: bo qua dia diem da co cung ten va khu vuc.\r\nSET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET XACT_ABORT ON;\r\nBEGIN TRANSACTION;\r\n';
a += 'CREATE TABLE #g(Ten nvarchar(200) COLLATE DATABASE_DEFAULT,Vung nvarchar(200) COLLATE DATABASE_DEFAULT,DonVi bigint,Loai nvarchar(50) COLLATE DATABASE_DEFAULT,DiaChi nvarchar(800) COLLATE DATABASE_DEFAULT,MoTa nvarchar(1400) COLLATE DATABASE_DEFAULT);\r\n';
for (let i = 0; i < kept.length; i += 400) a += 'INSERT #g VALUES\r\n' + kept.slice(i, i + 400).map(x => `(${sq(x.name.slice(0, 200))},${sq(x.dest)},${x.destId || 'NULL'},${sq(x.type)},${sq((x.address || '').slice(0, 800))},${sq(desc(x))})`).join(',\r\n') + ';\r\n';
a += `INSERT dbo.DiaDiemDuLich(TenDiaDiem,LoaiDiaDiem,KhuVuc,DonViId,DiaChi,MoTa,DangHienThi)
SELECT g.Ten,g.Loai,g.Vung,g.DonVi,NULLIF(g.DiaChi,N''),g.MoTa,1 FROM #g g WHERE NOT EXISTS(SELECT 1 FROM dbo.DiaDiemDuLich d WHERE d.TenDiaDiem=g.Ten AND d.KhuVuc=g.Vung);
DROP TABLE #g;
COMMIT TRANSACTION;
`;
let b = '-- Bo sung muc Kham pha cho dia diem lay tu Goong: toa do, dia chi, tu khoa, nguon. Chay sau khi may chu da tao muc Kham pha (khoang 1-3 phut).\r\nSET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET XACT_ABORT ON;\r\nBEGIN TRANSACTION;\r\n';
b += 'CREATE TABLE #b(Ten nvarchar(200) COLLATE DATABASE_DEFAULT,Vung nvarchar(200) COLLATE DATABASE_DEFAULT,Lat decimal(9,6),Lng decimal(9,6),DiaChi nvarchar(800) COLLATE DATABASE_DEFAULT,Tags nvarchar(1000) COLLATE DATABASE_DEFAULT);\r\n';
for (let i = 0; i < kept.length; i += 400) b += 'INSERT #b VALUES\r\n' + kept.slice(i, i + 400).map(x => `(${sq(x.name.slice(0, 200))},${sq(x.dest)},${x.lat.toFixed(6)},${x.lng.toFixed(6)},${sq((x.address || '').slice(0, 800))},${sq('Tham quan ' + (TAGS[x.type] || ''))})`).join(',\r\n') + ';\r\n';
b += `UPDATE e SET e.Latitude=s.Lat,e.Longitude=s.Lng,e.Address=NULLIF(s.DiaChi,N''),e.Tags=s.Tags,
 e.LocationNote=N'Vị trí theo dữ liệu Goong Maps; nên kiểm tra lại trước khi đến.',
 e.SourceName=N'Goong Maps (thông tin tham khảo)',e.CheckedOn='${today}',e.UpdatedAt=SYSUTCDATETIME()
FROM dbo.DiscoveryEntry e JOIN dbo.DiaDiemDuLich d ON d.MaDiaDiem=e.PlaceId JOIN #b s ON s.Ten=d.TenDiaDiem AND s.Vung=d.KhuVuc
WHERE e.Kind=N'Place' AND e.Latitude IS NULL;
DROP TABLE #b;
COMMIT TRANSACTION;
`;
fs.mkdirSync(outDir, { recursive: true });
fs.writeFileSync(path.join(outDir, 'goong-1-nguon.sql'), '﻿' + a);
fs.writeFileSync(path.join(outDir, 'goong-2-bosung.sql'), '﻿' + b);
const by = {}; kept.forEach(x => by[x.type] = (by[x.type] || 0) + 1);
console.log(`Giu ${kept.length} / ${raw.length} dia diem`, JSON.stringify(by));
