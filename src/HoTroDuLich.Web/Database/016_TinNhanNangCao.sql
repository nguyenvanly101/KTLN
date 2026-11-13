-- Tin nhan nang cao: tra loi mot tin, sua, thu hoi, vi tri, tep PDF, bieu cam. Chay lap an toan, khong xoa du lieu.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.TinNhanNangCao.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp tin nhắn nâng cao.',1;

IF OBJECT_ID(N'dbo.TinNhanChat',N'U') IS NOT NULL
BEGIN
 IF COL_LENGTH(N'dbo.TinNhanChat',N'ReplyToId') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat ADD ReplyToId bigint NULL;';
 IF COL_LENGTH(N'dbo.TinNhanChat',N'EditedAt') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat ADD EditedAt datetime2(3) NULL;';
 IF COL_LENGTH(N'dbo.TinNhanChat',N'DeletedAt') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat ADD DeletedAt datetime2(3) NULL;';
 IF COL_LENGTH(N'dbo.TinNhanChat',N'Lat') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat ADD Lat decimal(9,6) NULL;';
 IF COL_LENGTH(N'dbo.TinNhanChat',N'Lng') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat ADD Lng decimal(9,6) NULL;';
 IF COL_LENGTH(N'dbo.TinNhanChat',N'FileName') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat ADD FileName nvarchar(150) NULL;';
 IF COL_LENGTH(N'dbo.TinNhanChat',N'FilePath') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat ADD FilePath nvarchar(300) NULL;';
 IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Msg_Reply')
  EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat ADD CONSTRAINT FK_Msg_Reply FOREIGN KEY (ReplyToId) REFERENCES dbo.TinNhanChat(Id);';
 IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Msg_Kind' AND definition NOT LIKE N'%Location%')
  EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat DROP CONSTRAINT CK_Msg_Kind;';
 IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Msg_Kind')
  EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat ADD CONSTRAINT CK_Msg_Kind CHECK (Kind IN (''Text'',''Image'',''Auto'',''Booking'',''Event'',''Connection'',''Location'',''File''));';
 IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Msg_Content' AND definition NOT LIKE N'%DeletedAt%')
  EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat DROP CONSTRAINT CK_Msg_Content;';
 IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Msg_Content')
  EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat ADD CONSTRAINT CK_Msg_Content CHECK (LEN(Body)>0 OR ImagePath IS NOT NULL OR BookingRequestId IS NOT NULL OR ConnectionId IS NOT NULL OR Lat IS NOT NULL OR FilePath IS NOT NULL OR DeletedAt IS NOT NULL);';
 IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Msg_Geo')
  EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat ADD CONSTRAINT CK_Msg_Geo CHECK ((Lat IS NULL AND Lng IS NULL) OR (Lat IS NOT NULL AND Lng IS NOT NULL AND Lat BETWEEN -90 AND 90 AND Lng BETWEEN -180 AND 180));';
END;

IF OBJECT_ID(N'dbo.PhanUngTin',N'U') IS NULL AND OBJECT_ID(N'dbo.TinNhanChat',N'U') IS NOT NULL
BEGIN
 CREATE TABLE dbo.PhanUngTin(
  MessageId bigint NOT NULL CONSTRAINT FK_React_Msg FOREIGN KEY REFERENCES dbo.TinNhanChat(Id),
  AccountId bigint NOT NULL CONSTRAINT FK_React_Account FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  Emoji varchar(10) NOT NULL,
  CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_React_Created DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_PhanUngTin PRIMARY KEY (MessageId,AccountId),
  CONSTRAINT CK_React_Emoji CHECK (Emoji IN ('like','love','haha','wow','sad','angry')));
END;
COMMIT TRANSACTION;
