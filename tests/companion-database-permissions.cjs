const fs=require('node:fs'),assert=require('node:assert/strict'),{sql,json}=require('./companion-sql.cjs');
const database='HoTroDuLich_Companion_20261105',manifest=JSON.parse(fs.readFileSync('docs/companion-extension-schema.json','utf8'));
sql(database,fs.readFileSync('src/HoTroDuLich.Web/Database/Quyen_CSDL.sql','utf8'));
const expected=[];
for(const table of Object.keys(manifest.tables))for(const permission of ['SELECT','INSERT','UPDATE','DELETE','ALTER'])expected.push({table,permission,allowed:['DELETE','ALTER'].includes(permission)||permission==='UPDATE'&&['XacNhanThongBaoNhom','LuaChonBinhChon','PhanBoChiNhom'].includes(table)?0:1});
for(const table of ['ChiaSeChuyenDi','DanhGiaDichVu','LichPhucVuDichVu']){for(const permission of ['SELECT','INSERT','UPDATE'])expected.push({table,permission,allowed:1});expected.push({table,permission:'DELETE',allowed:table==='LichPhucVuDichVu'?1:0},{table,permission:'ALTER',allowed:0});}
for(const table of ['LuaChonDaLuu','MucChuanBi','KhoanChiChuyenDi','HoatDongLichTrinh'])expected.push({table,permission:'DELETE',allowed:1});
for(const table of ['TaiKhoan','KeHoachChuyenDi','YeuCauDatDichVu'])expected.push({table,permission:'DELETE',allowed:0});
for(const table of ['NhatKyThaoTac','QuyetDinhXacMinh','QuyetDinhQuyenHoatDong','TaiLieuXacMinh','TaiLieuGiaiTrinh','TinhHuongHoatDong'])expected.push({table,permission:'UPDATE',allowed:0});
const probes=expected.map(x=>`SELECT N'${x.table}' AS [table],N'${x.permission}' AS permission,HAS_PERMS_BY_NAME(N'dbo.${x.table}',N'OBJECT',N'${x.permission}') AS allowed`).join(' UNION ALL ');
const actual=json(database,`SET XACT_ABORT ON;BEGIN TRANSACTION;CREATE USER [companion_permission_probe] WITHOUT LOGIN;ALTER ROLE [r_ungdung] ADD MEMBER [companion_permission_probe];EXECUTE AS USER=N'companion_permission_probe';SELECT * FROM (${probes}) p FOR JSON PATH;REVERT;ROLLBACK TRANSACTION;`);
for(const item of expected)assert.equal(actual.find(x=>x.table===item.table&&x.permission===item.permission)?.allowed,item.allowed,item.table+' '+item.permission);
assert.equal(json(database,"SELECT USER_ID(N'companion_permission_probe') AS id FOR JSON PATH;")[0].id,undefined);
fs.writeFileSync('tests/companion-permissions-results.json',JSON.stringify({passed:true,checks:expected.length+1,database,results:actual,date:new Date().toISOString()},null,2));console.log('PASS',expected.length+1,'SQL application permission checks');
