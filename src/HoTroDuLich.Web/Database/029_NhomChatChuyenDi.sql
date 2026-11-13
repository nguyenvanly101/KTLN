-- Moi chuyen di chung co mot nhom chat tu dong. Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.NhomChatChuyenDi.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp nhóm chat chuyến đi.',1;
IF OBJECT_ID(N'dbo.NhomChatChuyenDi',N'U') IS NULL AND OBJECT_ID(N'dbo.KeHoachChuyenDi',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.CuocTroChuyen',N'U') IS NOT NULL
 EXEC sys.sp_executesql N'CREATE TABLE dbo.NhomChatChuyenDi(
  PlanId bigint NOT NULL CONSTRAINT PK_NhomChatChuyenDi PRIMARY KEY CONSTRAINT FK_NhomChatCd_Plan FOREIGN KEY REFERENCES dbo.KeHoachChuyenDi(MaKeHoach),
  ConversationId bigint NOT NULL CONSTRAINT FK_NhomChatCd_Conv FOREIGN KEY REFERENCES dbo.CuocTroChuyen(Id),
  CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_NhomChatCd_Created DEFAULT SYSUTCDATETIME());';
COMMIT TRANSACTION;
