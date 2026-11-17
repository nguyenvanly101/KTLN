// Xuat du lieu Kham pha cong khai (dia diem, muc tra cuu khong thuoc nha cung cap) tu co so du lieu ra script nap tu dong
// Database/036_DuLieuKhamPha.sql, de ban clone tu GitHub co du dia diem nhu may dang chay.
// Chay: node tools/export-places/export.cjs [--db=HoTroDuLich]
// Khong xuat: tai khoan, co so, dich vu, anh tai len, anh Wikimedia (anh khong nam trong git).
// Khoa ngoai dia gioi duoc doi sang (Regime, Code) de dung dung don vi tren co so du lieu moi.
const fs = require('fs'), path = require('path'), os = require('os'), { execSync } = require('child_process');
const DB = ((process.argv.find(a => a.startsWith('--db=')) || '').slice(5)) || 'HoTroDuLich';
const sql = q => {
  const f = path.join(os.tmpdir(), 'export-q-' + process.pid + '.sql');
  fs.writeFileSync(f, 'SET NOCOUNT ON; ' + q + ' FOR JSON PATH, INCLUDE_NULL_VALUES;');
  const raw = execSync('sqlcmd -I -S "(localdb)' + String.fromCharCode(92) + 'MSSQLLocalDB" -d ' + DB + ' -C -b -y 0 -w 65535 -f 65001 -i "' + f + '"', { encoding: 'utf8', maxBuffer: 1 << 28 });
  const i = raw.indexOf('[');
  return i < 0 ? [] : JSON.parse(raw.slice(i, raw.lastIndexOf(']') + 1).split('\r').join('').split('\n').join(''));
};
const s = v => v == null ? 'NULL' : "N'" + String(v).split("'").join("''") + "'";
const a = v => v == null ? 'NULL' : "'" + String(v).split("'").join("''") + "'";
const n = v => v == null ? 'NULL' : String(+v);

const places = sql(`SELECT p.TenDiaDiem, p.LoaiDiaDiem, p.KhuVuc, p.DiaChi, p.MoTa, CAST(p.DangHienThi AS int) DangHienThi, d.Regime, d.Code
  FROM dbo.DiaDiemDuLich p LEFT JOIN dbo.DonViHanhChinh d ON d.Id=p.DonViId ORDER BY p.MaDiaDiem`);
const entries = sql(`SELECT e.Slug, e.Kind, par.Slug ParentSlug, pl.TenDiaDiem PlaceTen, pl.KhuVuc PlaceVung, e.Title, e.Region, e.Summary, e.Body, e.Address,
   CAST(e.Latitude AS varchar(30)) Lat, CAST(e.Longitude AS varchar(30)) Lng, e.LocationNote, e.SourceName, e.SourceUrl,
   CONVERT(varchar(10),e.CheckedOn,23) CheckedOn, CONVERT(varchar(10),e.ValidFrom,23) ValidFrom, CONVERT(varchar(10),e.ValidTo,23) ValidTo,
   CAST(e.PriceFrom AS varchar(30)) PriceFrom, CAST(e.PriceTo AS varchar(30)) PriceTo, e.PriceUnit, e.Includes, e.Excludes, e.Hours, e.OpenDays,
   CONVERT(varchar(8),e.OpenTime,108) OpenTime, CONVERT(varchar(8),e.CloseTime,108) CloseTime, e.DurationMinutes, e.Tags, e.TransportMode, e.Origin, e.Destination,
   e.Contact, e.Status, e.AttributesJson, CASE WHEN e.ImagePath LIKE '/images/%' THEN e.ImagePath END ImagePath, d.Regime, d.Code
  FROM dbo.DiscoveryEntry e LEFT JOIN dbo.DiscoveryEntry par ON par.Id=e.ParentId LEFT JOIN dbo.DiaDiemDuLich pl ON pl.MaDiaDiem=e.PlaceId
  LEFT JOIN dbo.DonViHanhChinh d ON d.Id=e.DonViId
  WHERE e.ServiceId IS NULL AND e.BranchId IS NULL ORDER BY CASE WHEN e.ParentId IS NULL THEN 0 ELSE 1 END, e.Id`);

const L = [];
L.push('-- Du lieu Kham pha cong khai: ' + places.length + ' dia diem va ' + entries.length + ' muc tra cuu (diem den, khu vuc, dia diem, cam nang, di chuyen).');
L.push('-- Sinh tu dong boi tools/export-places/export.cjs ngay ' + new Date().toISOString().slice(0, 10) + '. Khong sua tay: chay lai cong cu de cap nhat.');
L.push('-- Nap mot lan tren co so du lieu moi (danh dau bang extended property). Bo qua dia diem trung ten + khu vuc va muc trung Slug,');
L.push('-- nen chay tren co so du lieu da co du lieu cung khong tao ban sao.');
L.push('SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET XACT_ABORT ON; SET NOCOUNT ON;');
L.push("IF OBJECT_ID(N'dbo.DiscoveryEntry',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.DiaDiemDuLich',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.DonViHanhChinh',N'U') IS NOT NULL");
L.push("AND NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'HanhTrinh.DuLieuKhamPha' AND CAST(value AS nvarchar(20))=N'v1')");
L.push('BEGIN');
L.push(' BEGIN TRANSACTION;');
L.push(' CREATE TABLE #p(Ten nvarchar(200) COLLATE DATABASE_DEFAULT,Loai nvarchar(100) COLLATE DATABASE_DEFAULT,Vung nvarchar(200) COLLATE DATABASE_DEFAULT,DiaChi nvarchar(500) COLLATE DATABASE_DEFAULT,MoTa nvarchar(max) COLLATE DATABASE_DEFAULT,Hien bit,Regime varchar(10) COLLATE DATABASE_DEFAULT,Code varchar(20) COLLATE DATABASE_DEFAULT);');
for (let i = 0; i < places.length; i += 300)
  L.push(' INSERT #p VALUES\r\n' + places.slice(i, i + 300).map(p => `  (${s(p.TenDiaDiem)},${s(p.LoaiDiaDiem)},${s(p.KhuVuc)},${s(p.DiaChi)},${s(p.MoTa)},${n(p.DangHienThi)},${a(p.Regime)},${a(p.Code)})`).join(',\r\n') + ';');
L.push(' INSERT dbo.DiaDiemDuLich(TenDiaDiem,LoaiDiaDiem,KhuVuc,DiaChi,MoTa,DangHienThi,DonViId)');
L.push('  SELECT p.Ten,p.Loai,p.Vung,p.DiaChi,p.MoTa,p.Hien,d.Id FROM #p p LEFT JOIN dbo.DonViHanhChinh d ON d.Regime=p.Regime AND d.Code=p.Code');
L.push('  WHERE NOT EXISTS (SELECT 1 FROM dbo.DiaDiemDuLich x WHERE x.TenDiaDiem=p.Ten AND x.KhuVuc=p.Vung);');
const cols = ['Slug varchar(160)', 'Kind varchar(25)', 'ParentSlug varchar(160)', 'PlaceTen nvarchar(200)', 'PlaceVung nvarchar(200)', 'Title nvarchar(200)', 'Region nvarchar(100)', 'Summary nvarchar(700)', 'Body nvarchar(max)', 'Address nvarchar(500)', 'Lat decimal(9,6)', 'Lng decimal(9,6)', 'LocationNote nvarchar(500)', 'SourceName nvarchar(300)', 'SourceUrl nvarchar(500)', 'CheckedOn date', 'ValidFrom date', 'ValidTo date', 'PriceFrom decimal(18,2)', 'PriceTo decimal(18,2)', 'PriceUnit nvarchar(100)', 'Includes nvarchar(max)', 'Excludes nvarchar(max)', 'Hours nvarchar(700)', 'OpenDays varchar(50)', 'OpenTime time', 'CloseTime time', 'DurationMinutes int', 'Tags nvarchar(1000)', 'TransportMode varchar(50)', 'Origin nvarchar(200)', 'Destination nvarchar(200)', 'Contact nvarchar(300)', 'Status varchar(20)', 'AttributesJson nvarchar(max)', 'ImagePath nvarchar(300)', 'Regime varchar(10)', 'Code varchar(20)'];
L.push(' CREATE TABLE #e(' + cols.map(c => c.replace(/(n?varchar\([^)]*\))/, '$1 COLLATE DATABASE_DEFAULT')).join(',') + ');');
const row = e => '  (' + [a(e.Slug), a(e.Kind), a(e.ParentSlug), s(e.PlaceTen), s(e.PlaceVung), s(e.Title), s(e.Region), s(e.Summary), s(e.Body), s(e.Address), n(e.Lat), n(e.Lng), s(e.LocationNote), s(e.SourceName), s(e.SourceUrl), a(e.CheckedOn), a(e.ValidFrom), a(e.ValidTo), n(e.PriceFrom), n(e.PriceTo), s(e.PriceUnit), s(e.Includes), s(e.Excludes), s(e.Hours), a(e.OpenDays), a(e.OpenTime), a(e.CloseTime), n(e.DurationMinutes), s(e.Tags), a(e.TransportMode), s(e.Origin), s(e.Destination), s(e.Contact), a(e.Status), s(e.AttributesJson), s(e.ImagePath), a(e.Regime), a(e.Code)].join(',') + ')';
for (let i = 0; i < entries.length; i += 200) L.push(' INSERT #e VALUES\r\n' + entries.slice(i, i + 200).map(row).join(',\r\n') + ';');
// Muc dia diem: noi voi dia diem vua nap theo ten + khu vuc; bo qua neu dia diem do da co muc (buoc lap chi muc da tao).
L.push(' INSERT dbo.DiscoveryEntry(Slug,Kind,PlaceId,Title,Region,Summary,Body,Address,Latitude,Longitude,LocationNote,SourceName,SourceUrl,CheckedOn,ValidFrom,ValidTo,PriceFrom,PriceTo,PriceUnit,Includes,Excludes,Hours,OpenDays,OpenTime,CloseTime,DurationMinutes,Tags,TransportMode,Origin,Destination,Contact,Status,AttributesJson,ImagePath,DonViId,UpdatedAt)');
L.push('  SELECT e.Slug,e.Kind,pl.MaDiaDiem,e.Title,e.Region,e.Summary,e.Body,e.Address,e.Lat,e.Lng,e.LocationNote,e.SourceName,e.SourceUrl,e.CheckedOn,e.ValidFrom,e.ValidTo,e.PriceFrom,e.PriceTo,e.PriceUnit,e.Includes,e.Excludes,e.Hours,e.OpenDays,e.OpenTime,e.CloseTime,e.DurationMinutes,e.Tags,e.TransportMode,e.Origin,e.Destination,e.Contact,e.Status,e.AttributesJson,e.ImagePath,d.Id,SYSUTCDATETIME()');
L.push('  FROM #e e LEFT JOIN dbo.DiaDiemDuLich pl ON e.PlaceTen IS NOT NULL AND pl.TenDiaDiem=e.PlaceTen AND pl.KhuVuc=e.PlaceVung LEFT JOIN dbo.DonViHanhChinh d ON d.Regime=e.Regime AND d.Code=e.Code');
L.push('  WHERE NOT EXISTS (SELECT 1 FROM dbo.DiscoveryEntry x WHERE x.Slug=e.Slug) AND (e.PlaceTen IS NULL OR (pl.MaDiaDiem IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.DiscoveryEntry x WHERE x.PlaceId=pl.MaDiaDiem)));');
L.push(' UPDATE c SET c.ParentId=p.Id FROM dbo.DiscoveryEntry c JOIN #e e ON e.Slug=c.Slug JOIN dbo.DiscoveryEntry p ON p.Slug=e.ParentSlug WHERE c.ParentId IS NULL AND e.ParentSlug IS NOT NULL;');
L.push(" EXEC sys.sp_addextendedproperty @name=N'HanhTrinh.DuLieuKhamPha',@value=N'v1';");
L.push(' COMMIT TRANSACTION;');
L.push('END');
const out = path.join(__dirname, '../../src/HoTroDuLich.Web/Database/036_DuLieuKhamPha.sql');
fs.writeFileSync(out, '﻿' + L.join('\r\n') + '\r\n');
console.log('Da ghi ' + out + ': ' + places.length + ' dia diem, ' + entries.length + ' muc, ' + (fs.statSync(out).size / 1048576).toFixed(2) + ' MB');
