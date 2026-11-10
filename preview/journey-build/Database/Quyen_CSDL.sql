-- FullText 4.8.7. Provision the server login separately, using a secret outside source control.
IF DATABASE_PRINCIPAL_ID(N'r_ungdung') IS NULL CREATE ROLE [r_ungdung];
GO
GRANT SELECT, INSERT, UPDATE ON SCHEMA::dbo TO [r_ungdung];
DENY ALTER ON SCHEMA::dbo TO [r_ungdung];
-- CONTROL is never granted. A broad DENY CONTROL also denies SELECT/INSERT
-- (and at database scope CONNECT), making the stated application grants unusable.
-- Explicit DML denials below and DENY ALTER enforce the intended least privilege.
GO
DECLARE @table sysname, @sql nvarchar(max);
DECLARE business_tables CURSOR LOCAL FAST_FORWARD FOR
 SELECT name FROM sys.tables WHERE schema_id=SCHEMA_ID(N'dbo') AND is_ms_shipped=0
  AND name NOT IN(N'HoatDongLichTrinh',N'LuaChonDaLuu',N'MucChuanBi',N'KhoanChiChuyenDi',N'LichPhucVuDichVu');
OPEN business_tables;
FETCH NEXT FROM business_tables INTO @table;
WHILE @@FETCH_STATUS=0
BEGIN
 SET @sql=N'DENY DELETE ON OBJECT::dbo.'+QUOTENAME(@table)+N' TO [r_ungdung];';
 EXEC sys.sp_executesql @sql;
 FETCH NEXT FROM business_tables INTO @table;
END;
CLOSE business_tables;
DEALLOCATE business_tables;
GRANT DELETE ON OBJECT::dbo.HoatDongLichTrinh TO [r_ungdung];
-- The approved product extension lets owners remove their personal working data.
-- Access to an individual owner's rows is checked by the application service.
IF OBJECT_ID(N'dbo.LuaChonDaLuu',N'U') IS NOT NULL GRANT DELETE ON OBJECT::dbo.LuaChonDaLuu TO [r_ungdung];
IF OBJECT_ID(N'dbo.MucChuanBi',N'U') IS NOT NULL GRANT DELETE ON OBJECT::dbo.MucChuanBi TO [r_ungdung];
IF OBJECT_ID(N'dbo.KhoanChiChuyenDi',N'U') IS NOT NULL GRANT DELETE ON OBJECT::dbo.KhoanChiChuyenDi TO [r_ungdung];
IF OBJECT_ID(N'dbo.LichPhucVuDichVu',N'U') IS NOT NULL GRANT DELETE ON OBJECT::dbo.LichPhucVuDichVu TO [r_ungdung];
DENY UPDATE ON OBJECT::dbo.NhatKyThaoTac TO [r_ungdung];
DENY UPDATE ON OBJECT::dbo.QuyetDinhXacMinh TO [r_ungdung];
DENY UPDATE ON OBJECT::dbo.QuyetDinhQuyenHoatDong TO [r_ungdung];
DENY UPDATE ON OBJECT::dbo.TaiLieuXacMinh TO [r_ungdung];
DENY UPDATE ON OBJECT::dbo.TaiLieuGiaiTrinh TO [r_ungdung];
DENY UPDATE ON OBJECT::dbo.TinhHuongHoatDong TO [r_ungdung];
GO
IF SUSER_ID(N'htdl_app') IS NOT NULL
BEGIN
 IF DATABASE_PRINCIPAL_ID(N'htdl_app') IS NULL CREATE USER [htdl_app] FOR LOGIN [htdl_app];
 ALTER ROLE [r_ungdung] ADD MEMBER [htdl_app];
END;
GO
