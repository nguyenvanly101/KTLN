-- Gan the dich vu vao bai viet va bao cao bai viet / binh luan. Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.TheBaiViet.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp thẻ bài viết.',1;
IF OBJECT_ID(N'dbo.BaiViet',N'U') IS NOT NULL
BEGIN
 IF COL_LENGTH(N'dbo.BaiViet',N'ServiceId') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.BaiViet ADD [ServiceId] bigint NULL;';
 IF COL_LENGTH(N'dbo.BaiViet',N'PlaceId') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.BaiViet ADD [PlaceId] bigint NULL;';
END;
IF OBJECT_ID(N'dbo.BaoCaoBaiViet',N'U') IS NULL AND OBJECT_ID(N'dbo.BaiViet',N'U') IS NOT NULL
 EXEC sys.sp_executesql N'CREATE TABLE dbo.BaoCaoBaiViet(
  Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_BaoCaoBaiViet PRIMARY KEY,
  ReporterId bigint NOT NULL CONSTRAINT FK_RepPost_Reporter FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  PostId bigint NOT NULL CONSTRAINT FK_RepPost_Post FOREIGN KEY REFERENCES dbo.BaiViet(Id),
  CommentId bigint NULL,
  Reason varchar(20) NOT NULL,
  Note nvarchar(500) NULL,
  Status varchar(10) NOT NULL CONSTRAINT DF_RepPost_Status DEFAULT ''Open'',
  CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_RepPost_Created DEFAULT SYSUTCDATETIME(),
  HandledAt datetime2(0) NULL,
  CONSTRAINT CK_RepPost_Reason CHECK (Reason IN (''Spam'',''QuayRoi'',''LuaDao'',''NoiDungXau'',''Khac'')),
  CONSTRAINT CK_RepPost_Status CHECK (Status IN (''Open'',''Done'',''Hidden'')));
  CREATE INDEX IX_RepPost_Status ON dbo.BaoCaoBaiViet(Status,Id DESC);';
COMMIT TRANSACTION;
