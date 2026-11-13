-- Co so tro thanh ho so day du cho khach: gia tham khao (tu-den-don vi), gio mo cua co cau truc, anh thuc te.
-- Them cot vao CoSoDichVu va BanCapNhatCoSo (ban cap nhat cho duyet) va DiscoveryEntry.ImagePath. Chay lap an toan, khong xoa du lieu.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.CoSoThongTin.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp thông tin cơ sở.',1;

DECLARE @tables TABLE ([T] sysname NOT NULL);
INSERT @tables VALUES (N'CoSoDichVu'),(N'BanCapNhatCoSo');
DECLARE @t sysname,@sql nvarchar(max);
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT [T] FROM @tables;
OPEN c; FETCH NEXT FROM c INTO @t;
WHILE @@FETCH_STATUS=0
BEGIN
 IF OBJECT_ID(N'dbo.'+QUOTENAME(@t),N'U') IS NOT NULL
 BEGIN
  IF COL_LENGTH(N'dbo.'+@t,N'PriceFrom') IS NULL BEGIN SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@t)+N' ADD [PriceFrom] decimal(18,0) NULL;'; EXEC sys.sp_executesql @sql; END;
  IF COL_LENGTH(N'dbo.'+@t,N'PriceTo') IS NULL BEGIN SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@t)+N' ADD [PriceTo] decimal(18,0) NULL;'; EXEC sys.sp_executesql @sql; END;
  IF COL_LENGTH(N'dbo.'+@t,N'PriceUnit') IS NULL BEGIN SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@t)+N' ADD [PriceUnit] nvarchar(50) NULL;'; EXEC sys.sp_executesql @sql; END;
  IF COL_LENGTH(N'dbo.'+@t,N'OpenDays') IS NULL BEGIN SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@t)+N' ADD [OpenDays] varchar(7) NULL;'; EXEC sys.sp_executesql @sql; END;
  IF COL_LENGTH(N'dbo.'+@t,N'OpenTime') IS NULL BEGIN SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@t)+N' ADD [OpenTime] time(0) NULL;'; EXEC sys.sp_executesql @sql; END;
  IF COL_LENGTH(N'dbo.'+@t,N'CloseTime') IS NULL BEGIN SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@t)+N' ADD [CloseTime] time(0) NULL;'; EXEC sys.sp_executesql @sql; END;
  IF COL_LENGTH(N'dbo.'+@t,N'PhotosJson') IS NULL BEGIN SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@t)+N' ADD [PhotosJson] nvarchar(1500) NULL;'; EXEC sys.sp_executesql @sql; END;
  IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_'+@t+N'_Info' AND parent_object_id=OBJECT_ID(N'dbo.'+QUOTENAME(@t)))
  BEGIN
   SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@t)+N' WITH CHECK ADD CONSTRAINT '+QUOTENAME(N'CK_'+@t+N'_Info')+N' CHECK ('
    +N'([PriceFrom] IS NULL OR [PriceFrom]>=0) AND ([PriceTo] IS NULL OR [PriceTo]>=0) AND ([PriceFrom] IS NULL OR [PriceTo] IS NULL OR [PriceTo]>=[PriceFrom]) AND (([PriceFrom] IS NULL AND [PriceTo] IS NULL) OR [PriceUnit] IS NOT NULL)'
    +N' AND (([OpenDays] IS NULL AND [OpenTime] IS NULL AND [CloseTime] IS NULL) OR ([OpenDays] IS NOT NULL AND [OpenTime] IS NOT NULL AND [CloseTime] IS NOT NULL AND LEN([OpenDays])>0 AND [OpenDays] NOT LIKE ''%[^1234567]%'')));';
   EXEC sys.sp_executesql @sql;
  END;
 END;
 FETCH NEXT FROM c INTO @t;
END;
CLOSE c; DEALLOCATE c;

IF COL_LENGTH(N'dbo.DiscoveryEntry',N'ImagePath') IS NULL
 EXEC sys.sp_executesql N'ALTER TABLE dbo.[DiscoveryEntry] ADD [ImagePath] nvarchar(300) NULL;';
COMMIT TRANSACTION;
