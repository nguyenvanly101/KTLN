IF DB_NAME() NOT LIKE N'HoTroDuLich_FullText%' THROW 51030,'Permission probe only runs in an isolated verification database.',1;
GO
:r Quyen_CSDL.sql
DECLARE @clearControl nvarchar(max)=N'REVOKE CONTROL ON DATABASE::'+QUOTENAME(DB_NAME())+N' FROM [r_ungdung];';
EXEC sys.sp_executesql @clearControl;
REVOKE CONTROL ON SCHEMA::dbo FROM [r_ungdung];
IF DATABASE_PRINCIPAL_ID(N'fulltext_permission_probe') IS NOT NULL DROP USER [fulltext_permission_probe];
CREATE USER [fulltext_permission_probe] WITHOUT LOGIN;
ALTER ROLE [r_ungdung] ADD MEMBER [fulltext_permission_probe];
EXECUTE AS USER=N'fulltext_permission_probe';
SELECT HAS_PERMS_BY_NAME(N'dbo.TaiKhoan',N'OBJECT',N'SELECT') AS CanSelect,
 HAS_PERMS_BY_NAME(N'dbo.TaiKhoan',N'OBJECT',N'INSERT') AS CanInsert,
 HAS_PERMS_BY_NAME(N'dbo.TaiKhoan',N'OBJECT',N'DELETE') AS CanDeleteAccount,
 HAS_PERMS_BY_NAME(N'dbo.HoatDongLichTrinh',N'OBJECT',N'DELETE') AS CanDeleteActivity,
 HAS_PERMS_BY_NAME(N'dbo.NhatKyThaoTac',N'OBJECT',N'UPDATE') AS CanChangeLog,
 HAS_PERMS_BY_NAME(N'dbo',N'SCHEMA',N'CONTROL') AS CanControl;
IF HAS_PERMS_BY_NAME(N'dbo.TaiKhoan',N'OBJECT',N'SELECT')<>1 OR HAS_PERMS_BY_NAME(N'dbo.TaiKhoan',N'OBJECT',N'INSERT')<>1 OR HAS_PERMS_BY_NAME(N'dbo.TaiKhoan',N'OBJECT',N'DELETE')<>0 OR HAS_PERMS_BY_NAME(N'dbo.HoatDongLichTrinh',N'OBJECT',N'DELETE')<>1 OR HAS_PERMS_BY_NAME(N'dbo.NhatKyThaoTac',N'OBJECT',N'UPDATE')<>0 OR HAS_PERMS_BY_NAME(N'dbo',N'SCHEMA',N'CONTROL')<>0 THROW 51031,'Application permissions do not match the intended FullText access.',1;
REVERT;
DROP USER [fulltext_permission_probe];
