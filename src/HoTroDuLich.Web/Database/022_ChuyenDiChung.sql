-- Chuyen di chung voi ban be: thanh vien moi duoc chap nhan cung xem va sua hang muc. Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.ChuyenDiChung.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp chuyến đi chung.',1;
IF OBJECT_ID(N'dbo.ThanhVienChuyenDi',N'U') IS NULL AND OBJECT_ID(N'dbo.KeHoachChuyenDi',N'U') IS NOT NULL
 EXEC sys.sp_executesql N'CREATE TABLE dbo.ThanhVienChuyenDi(
  PlanId bigint NOT NULL CONSTRAINT FK_TvCd_Plan FOREIGN KEY REFERENCES dbo.KeHoachChuyenDi(MaKeHoach),
  AccountId bigint NOT NULL CONSTRAINT FK_TvCd_Account FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  Status varchar(10) NOT NULL CONSTRAINT DF_TvCd_Status DEFAULT ''Pending'',
  InvitedBy bigint NOT NULL CONSTRAINT FK_TvCd_Inviter FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_TvCd_Created DEFAULT SYSUTCDATETIME(),
  RespondedAt datetime2(0) NULL,
  CONSTRAINT PK_ThanhVienChuyenDi PRIMARY KEY (PlanId,AccountId),
  CONSTRAINT CK_TvCd_Status CHECK (Status IN (''Pending'',''Accepted'')));
  CREATE INDEX IX_TvCd_Account ON dbo.ThanhVienChuyenDi(AccountId,Status);';
COMMIT TRANSACTION;
