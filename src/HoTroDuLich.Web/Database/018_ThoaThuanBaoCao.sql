-- Tim ban dong hanh qua chat: the thoa thuan di cung, bo qua goi y, bao cao nguoi dung / tin nhan. Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.ThoaThuanBaoCao.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp thỏa thuận và báo cáo.',1;

IF OBJECT_ID(N'dbo.TinNhanChat',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.ThoaThuanDongHanh',N'U') IS NOT NULL
BEGIN
 IF COL_LENGTH(N'dbo.TinNhanChat',N'AgreementId') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat ADD AgreementId bigint NULL;';
 IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Msg_Agreement')
  EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat ADD CONSTRAINT FK_Msg_Agreement FOREIGN KEY (AgreementId) REFERENCES dbo.ThoaThuanDongHanh(MaThoaThuan);';
 IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Msg_Kind' AND definition NOT LIKE N'%Agreement%')
  EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat DROP CONSTRAINT CK_Msg_Kind;';
 IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Msg_Kind')
  EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat ADD CONSTRAINT CK_Msg_Kind CHECK (Kind IN (''Text'',''Image'',''Auto'',''Booking'',''Event'',''Connection'',''Location'',''File'',''Agreement''));';
 IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Msg_Content' AND definition NOT LIKE N'%AgreementId%')
  EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat DROP CONSTRAINT CK_Msg_Content;';
 IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Msg_Content')
  EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat ADD CONSTRAINT CK_Msg_Content CHECK (LEN(Body)>0 OR ImagePath IS NOT NULL OR BookingRequestId IS NOT NULL OR ConnectionId IS NOT NULL OR AgreementId IS NOT NULL OR Lat IS NOT NULL OR FilePath IS NOT NULL OR DeletedAt IS NOT NULL);';
END;

IF OBJECT_ID(N'dbo.BoQuaGoiY',N'U') IS NULL AND OBJECT_ID(N'dbo.NhuCauDongHanh',N'U') IS NOT NULL
 CREATE TABLE dbo.BoQuaGoiY(
  AccountId bigint NOT NULL CONSTRAINT FK_Bo_Account FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  NeedId bigint NOT NULL CONSTRAINT FK_Bo_Need FOREIGN KEY REFERENCES dbo.NhuCauDongHanh(MaNhuCau),
  CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Bo_Created DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_BoQuaGoiY PRIMARY KEY (AccountId,NeedId));

IF OBJECT_ID(N'dbo.BaoCaoChat',N'U') IS NULL AND OBJECT_ID(N'dbo.CuocTroChuyen',N'U') IS NOT NULL
 CREATE TABLE dbo.BaoCaoChat(
  Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_BaoCaoChat PRIMARY KEY,
  ReporterId bigint NOT NULL CONSTRAINT FK_Rep_Reporter FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  TargetId bigint NULL CONSTRAINT FK_Rep_Target FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  ConversationId bigint NOT NULL CONSTRAINT FK_Rep_Conv FOREIGN KEY REFERENCES dbo.CuocTroChuyen(Id),
  MessageId bigint NULL CONSTRAINT FK_Rep_Msg FOREIGN KEY REFERENCES dbo.TinNhanChat(Id),
  Reason varchar(20) NOT NULL,
  Note nvarchar(500) NULL,
  Status varchar(10) NOT NULL CONSTRAINT DF_Rep_Status DEFAULT 'Open',
  CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Rep_Created DEFAULT SYSUTCDATETIME(),
  HandledAt datetime2(0) NULL,
  CONSTRAINT CK_Rep_Reason CHECK (Reason IN ('Spam','QuayRoi','LuaDao','NoiDungXau','Khac')),
  CONSTRAINT CK_Rep_Status CHECK (Status IN ('Open','Done')));
COMMIT TRANSACTION;
