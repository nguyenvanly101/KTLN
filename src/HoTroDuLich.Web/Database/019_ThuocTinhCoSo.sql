-- Thong tin rieng theo loai co so (an uong, luu tru, tham quan, di chuyen): luu JSON co kiem tra. Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.ThuocTinhCoSo.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp thuộc tính cơ sở.',1;

DECLARE @tables TABLE ([T] sysname NOT NULL);
INSERT @tables VALUES (N'CoSoDichVu'),(N'BanCapNhatCoSo');
DECLARE @t sysname,@sql nvarchar(max);
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT [T] FROM @tables;
OPEN c; FETCH NEXT FROM c INTO @t;
WHILE @@FETCH_STATUS=0
BEGIN
 IF OBJECT_ID(N'dbo.'+QUOTENAME(@t),N'U') IS NOT NULL
 BEGIN
  IF COL_LENGTH(N'dbo.'+@t,N'AttributesJson') IS NULL
  BEGIN SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@t)+N' ADD [AttributesJson] nvarchar(max) NULL;'; EXEC sys.sp_executesql @sql; END;
  IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_'+@t+N'_Attr' AND parent_object_id=OBJECT_ID(N'dbo.'+QUOTENAME(@t)))
  BEGIN SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@t)+N' WITH CHECK ADD CONSTRAINT '+QUOTENAME(N'CK_'+@t+N'_Attr')+N' CHECK ([AttributesJson] IS NULL OR (ISJSON([AttributesJson])=1 AND LEN([AttributesJson])<=6000));'; EXEC sys.sp_executesql @sql; END;
 END;
 FETCH NEXT FROM c INTO @t;
END;
CLOSE c; DEALLOCATE c;
COMMIT TRANSACTION;
