-- Loai bai viet: Share (chia se), Companion (tim ban dong hanh), Branch (tin gioi thieu co so cua nha cung cap). Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.LoaiBaiViet.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp loại bài viết.',1;
IF OBJECT_ID(N'dbo.BaiViet',N'U') IS NOT NULL
BEGIN
 IF COL_LENGTH(N'dbo.BaiViet',N'Kind') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.BaiViet ADD [Kind] varchar(10) NOT NULL CONSTRAINT DF_BaiViet_Kind DEFAULT ''Share'';';
 IF COL_LENGTH(N'dbo.BaiViet',N'BranchId') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.BaiViet ADD [BranchId] bigint NULL;';
 IF COL_LENGTH(N'dbo.BaiViet',N'Destination') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.BaiViet ADD [Destination] nvarchar(200) NULL;';
 IF COL_LENGTH(N'dbo.BaiViet',N'TripStart') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.BaiViet ADD [TripStart] date NULL;';
 IF COL_LENGTH(N'dbo.BaiViet',N'TripEnd') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.BaiViet ADD [TripEnd] date NULL;';
 IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_BaiViet_Kind' AND parent_object_id=OBJECT_ID(N'dbo.BaiViet'))
  EXEC sys.sp_executesql N'ALTER TABLE dbo.BaiViet WITH CHECK ADD CONSTRAINT CK_BaiViet_Kind CHECK ([Kind] IN (''Share'',''Companion'',''Branch'') AND ([Kind]<>''Branch'' OR [BranchId] IS NOT NULL) AND ([TripEnd] IS NULL OR [TripStart] IS NULL OR [TripEnd]>=[TripStart]));';
 IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_BaiViet_Kind' AND object_id=OBJECT_ID(N'dbo.BaiViet'))
  EXEC sys.sp_executesql N'CREATE INDEX IX_BaiViet_Kind ON dbo.BaiViet([Kind],[BranchId],[Id] DESC);';
END;
COMMIT TRANSACTION;
