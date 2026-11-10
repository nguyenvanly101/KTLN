const fs=require('node:fs'),assert=require('node:assert/strict'),{sql,json}=require('./journey-sql.cjs');
const database='HoTroDuLich_Branches_Audit_20261109054153';
sql(database,fs.readFileSync('src/HoTroDuLich.Web/Database/Quyen_CSDL.sql','utf8'));
const expected=[];
for(const table of ['ChiaSeChuyenDi','DanhGiaDichVu','LichPhucVuDichVu']){
 for(const permission of ['SELECT','INSERT','UPDATE'])expected.push({table,permission,allowed:1});
 expected.push({table,permission:'DELETE',allowed:table==='LichPhucVuDichVu'?1:0},{table,permission:'ALTER',allowed:0});
}
for(const table of ['LuaChonDaLuu','MucChuanBi','KhoanChiChuyenDi','HoatDongLichTrinh'])expected.push({table,permission:'DELETE',allowed:1});
for(const table of ['TaiKhoan','KeHoachChuyenDi','YeuCauDatDichVu'])expected.push({table,permission:'DELETE',allowed:0});
for(const table of ['NhatKyThaoTac','QuyetDinhXacMinh','QuyetDinhQuyenHoatDong','TaiLieuXacMinh','TaiLieuGiaiTrinh','TinhHuongHoatDong'])expected.push({table,permission:'UPDATE',allowed:0});
const probes=expected.map(x=>`SELECT N'${x.table}' AS [table],N'${x.permission}' AS permission,HAS_PERMS_BY_NAME(N'dbo.${x.table}',N'OBJECT',N'${x.permission}') AS allowed`).join(' UNION ALL ');
const actual=json(database,`SET XACT_ABORT ON;BEGIN TRANSACTION;CREATE USER [journey_permission_probe] WITHOUT LOGIN;ALTER ROLE [r_ungdung] ADD MEMBER [journey_permission_probe];EXECUTE AS USER=N'journey_permission_probe';SELECT * FROM (${probes}) p FOR JSON PATH;REVERT;ROLLBACK TRANSACTION;`);
for(const check of expected)assert.equal(actual.find(x=>x.table===check.table&&x.permission===check.permission)?.allowed,check.allowed,check.table+' '+check.permission);
assert.equal(json(database,"SELECT USER_ID(N'journey_permission_probe') AS id FOR JSON PATH;")[0].id,undefined);
fs.writeFileSync('tests/journey-database-permissions-results.json',JSON.stringify({passed:true,checks:expected.length+1,database,results:actual,date:new Date().toISOString()},null,2));console.log('PASS',expected.length+1,'SQL application permission checks');
