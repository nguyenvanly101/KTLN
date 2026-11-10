// Permission probe on the isolated database; no login or persistent probe user is created.
const fs=require('node:fs'),assert=require('node:assert/strict'),{execFileSync}=require('node:child_process');
const database='HoTroDuLich_Branches_Audit_20261109054153';
execFileSync('sqlcmd',['-S',process.env.AUDIT_SQL_SERVER,'-d',database,'-E','-C','-b','-i','src/HoTroDuLich.Web/Database/Quyen_CSDL.sql'],{encoding:'utf8'});
const expected=[];
for(const table of ['LuaChonDaLuu','MucChuanBi','KhoanChiChuyenDi'])for(const permission of ['SELECT','INSERT','UPDATE','DELETE'])expected.push({table,permission,allowed:1});
for(const [table,permission,allowed]of [['HoatDongLichTrinh','DELETE',1],['TaiKhoan','DELETE',0],['KeHoachChuyenDi','DELETE',0],['NhatKyThaoTac','UPDATE',0],['QuyetDinhXacMinh','UPDATE',0],['QuyetDinhQuyenHoatDong','UPDATE',0],['TaiLieuXacMinh','UPDATE',0],['TaiLieuGiaiTrinh','UPDATE',0],['TinhHuongHoatDong','UPDATE',0],['TaiKhoan','ALTER',0]])expected.push({table,permission,allowed});
const probes=expected.map(x=>`SELECT N'${x.table}' AS [table],N'${x.permission}' AS permission,HAS_PERMS_BY_NAME(N'dbo.${x.table}',N'OBJECT',N'${x.permission}') AS allowed`).join(' UNION ALL ');
const query=`SET NOCOUNT ON; SET XACT_ABORT ON; BEGIN TRANSACTION; CREATE USER [product_permission_probe] WITHOUT LOGIN; ALTER ROLE [r_ungdung] ADD MEMBER [product_permission_probe]; EXECUTE AS USER=N'product_permission_probe'; SELECT * FROM (${probes}) x FOR JSON PATH; REVERT; ROLLBACK TRANSACTION;`;
const raw=execFileSync('sqlcmd',['-S',process.env.AUDIT_SQL_SERVER,'-d',database,'-E','-C','-b','-y','0','-w','65535','-Q',query],{encoding:'utf8'});
const actual=JSON.parse(raw.slice(raw.indexOf('['),raw.lastIndexOf(']')+1).replace(/[\r\n]/g,''));
for(const x of expected)assert.equal(actual.find(y=>y.table===x.table&&y.permission===x.permission)?.allowed,x.allowed,x.table+' '+x.permission);
fs.writeFileSync('tests/product-database-permissions-results.json',JSON.stringify({passed:true,checks:expected.length,database,results:actual,date:new Date().toISOString()},null,2));console.log('PASS',expected.length,'SQL permission checks');
