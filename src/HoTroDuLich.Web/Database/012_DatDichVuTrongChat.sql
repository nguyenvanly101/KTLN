-- Dat dich vu ngay trong cuoc tro chuyen: tin nhan co the la the "yeu cau dat" hoac su kien cap nhat the. Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.DatTrongChat.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp đặt dịch vụ trong chat.',1;

IF OBJECT_ID(N'dbo.TinNhanChat',N'U') IS NOT NULL
BEGIN
 IF COL_LENGTH(N'dbo.TinNhanChat',N'BookingRequestId') IS NULL
  EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat ADD BookingRequestId bigint NULL;';
 IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Msg_Booking')
  EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat ADD CONSTRAINT FK_Msg_Booking FOREIGN KEY (BookingRequestId) REFERENCES dbo.YeuCauDatDichVu(MaYeuCau);';
 IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Msg_Booking' AND object_id=OBJECT_ID(N'dbo.TinNhanChat'))
  EXEC sys.sp_executesql N'CREATE INDEX IX_Msg_Booking ON dbo.TinNhanChat(BookingRequestId) WHERE BookingRequestId IS NOT NULL;';
 IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Msg_Kind' AND definition NOT LIKE N'%Booking%')
  EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat DROP CONSTRAINT CK_Msg_Kind;';
 IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Msg_Kind')
  EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat ADD CONSTRAINT CK_Msg_Kind CHECK (Kind IN (''Text'',''Image'',''Auto'',''Booking'',''Event''));';
 IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Msg_Content' AND definition NOT LIKE N'%BookingRequestId%')
  EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat DROP CONSTRAINT CK_Msg_Content;';
 IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Msg_Content')
  EXEC sys.sp_executesql N'ALTER TABLE dbo.TinNhanChat ADD CONSTRAINT CK_Msg_Content CHECK (LEN(Body)>0 OR ImagePath IS NOT NULL OR BookingRequestId IS NOT NULL);';
END;
COMMIT TRANSACTION;
