-- Dat dich vu trong chat: ghi nhan dat coc (khong xu ly tien) va nhac lich / moi danh gia tu dong. Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.DatCocNhacLich.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp đặt cọc và nhắc lịch.',1;

IF OBJECT_ID(N'dbo.DatCocChat',N'U') IS NULL AND OBJECT_ID(N'dbo.YeuCauDatDichVu',N'U') IS NOT NULL
 CREATE TABLE dbo.DatCocChat(
  RequestId bigint NOT NULL CONSTRAINT PK_DatCocChat PRIMARY KEY CONSTRAINT FK_Coc_Request FOREIGN KEY REFERENCES dbo.YeuCauDatDichVu(MaYeuCau),
  Amount decimal(18,0) NOT NULL,
  Method nvarchar(40) NOT NULL,
  Note nvarchar(300) NULL,
  Status varchar(10) NOT NULL CONSTRAINT DF_Coc_Status DEFAULT 'Requested',
  RequestedAt datetime2(0) NOT NULL CONSTRAINT DF_Coc_Req DEFAULT SYSUTCDATETIME(),
  PaidAt datetime2(0) NULL,
  CONSTRAINT CK_Coc_Amount CHECK (Amount>0 AND Amount<=1000000000),
  CONSTRAINT CK_Coc_Status CHECK (Status IN ('Requested','Paid')));

IF OBJECT_ID(N'dbo.NhacLichChat',N'U') IS NULL AND OBJECT_ID(N'dbo.YeuCauDatDichVu',N'U') IS NOT NULL
 CREATE TABLE dbo.NhacLichChat(
  RequestId bigint NOT NULL CONSTRAINT FK_Nhac_Request FOREIGN KEY REFERENCES dbo.YeuCauDatDichVu(MaYeuCau),
  Kind varchar(10) NOT NULL,
  SentAt datetime2(0) NOT NULL CONSTRAINT DF_Nhac_Sent DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_NhacLichChat PRIMARY KEY (RequestId,Kind),
  CONSTRAINT CK_Nhac_Kind CHECK (Kind IN ('Reminder','Review')));
COMMIT TRANSACTION;
