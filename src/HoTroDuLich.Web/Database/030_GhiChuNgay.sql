-- Ghi chu cho tung ngay cua chuyen di. Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.GhiChuNgay.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp ghi chú ngày.',1;
IF OBJECT_ID(N'dbo.GhiChuNgayChuyenDi',N'U') IS NULL AND OBJECT_ID(N'dbo.KeHoachChuyenDi',N'U') IS NOT NULL
 EXEC sys.sp_executesql N'CREATE TABLE dbo.GhiChuNgayChuyenDi(
  PlanId bigint NOT NULL CONSTRAINT FK_GhiChuNgay_Plan FOREIGN KEY REFERENCES dbo.KeHoachChuyenDi(MaKeHoach),
  DayNo int NOT NULL,
  Note nvarchar(1000) NOT NULL,
  UpdatedAt datetime2(0) NOT NULL CONSTRAINT DF_GhiChuNgay_Upd DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_GhiChuNgayChuyenDi PRIMARY KEY (PlanId,DayNo),
  CONSTRAINT CK_GhiChuNgay_Day CHECK (DayNo BETWEEN 1 AND 30));';
COMMIT TRANSACTION;
