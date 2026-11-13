-- Chia tien nhom trong chuyen di chung: ai tra, chia cho ai. Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.ChiaTienNhom.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp chia tiền nhóm.',1;
IF OBJECT_ID(N'dbo.KhoanChiChung',N'U') IS NULL AND OBJECT_ID(N'dbo.KeHoachChuyenDi',N'U') IS NOT NULL
 EXEC sys.sp_executesql N'CREATE TABLE dbo.KhoanChiChung(
  Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_KhoanChiChung PRIMARY KEY,
  PlanId bigint NOT NULL CONSTRAINT FK_KcChung_Plan FOREIGN KEY REFERENCES dbo.KeHoachChuyenDi(MaKeHoach),
  PaidBy bigint NOT NULL CONSTRAINT FK_KcChung_Paid FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  Title nvarchar(200) NOT NULL,
  Amount decimal(18,0) NOT NULL,
  SpentOn date NOT NULL,
  CreatedBy bigint NOT NULL CONSTRAINT FK_KcChung_By FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_KcChung_Created DEFAULT SYSUTCDATETIME(),
  CONSTRAINT CK_KcChung_Amount CHECK (Amount>0 AND Amount<=1000000000));
  CREATE INDEX IX_KcChung_Plan ON dbo.KhoanChiChung(PlanId,Id DESC);';
IF OBJECT_ID(N'dbo.KhoanChiChungChia',N'U') IS NULL AND OBJECT_ID(N'dbo.KhoanChiChung',N'U') IS NOT NULL
 EXEC sys.sp_executesql N'CREATE TABLE dbo.KhoanChiChungChia(
  ExpenseId bigint NOT NULL CONSTRAINT FK_KcChia_Exp FOREIGN KEY REFERENCES dbo.KhoanChiChung(Id),
  AccountId bigint NOT NULL CONSTRAINT FK_KcChia_Acc FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  Share decimal(18,0) NOT NULL,
  CONSTRAINT PK_KhoanChiChungChia PRIMARY KEY (ExpenseId,AccountId),
  CONSTRAINT CK_KcChia_Share CHECK (Share>=0));';
COMMIT TRANSACTION;
