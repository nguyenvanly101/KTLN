-- Diem xuat phat va diem ket thuc cua chuyen di (toa do de tinh khoang cach va duong di). Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.DiemTuyen.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp điểm xuất phát.',1;
IF OBJECT_ID(N'dbo.DiemTuyenChuyenDi',N'U') IS NULL AND OBJECT_ID(N'dbo.KeHoachChuyenDi',N'U') IS NOT NULL
 EXEC sys.sp_executesql N'CREATE TABLE dbo.DiemTuyenChuyenDi(
  PlanId bigint NOT NULL CONSTRAINT PK_DiemTuyenChuyenDi PRIMARY KEY
   CONSTRAINT FK_DiemTuyen_Plan FOREIGN KEY REFERENCES dbo.KeHoachChuyenDi(MaKeHoach) ON DELETE CASCADE,
  StartName nvarchar(300) NULL,
  StartLat decimal(9,6) NULL,
  StartLng decimal(9,6) NULL,
  EndName nvarchar(300) NULL,
  EndLat decimal(9,6) NULL,
  EndLng decimal(9,6) NULL,
  UpdatedAt datetime2(0) NOT NULL CONSTRAINT DF_DiemTuyen_Upd DEFAULT SYSUTCDATETIME(),
  CONSTRAINT CK_DiemTuyen_Start CHECK ((StartLat IS NULL AND StartLng IS NULL) OR (StartLat BETWEEN 5 AND 24 AND StartLng BETWEEN 101.5 AND 118.5)),
  CONSTRAINT CK_DiemTuyen_End CHECK ((EndLat IS NULL AND EndLng IS NULL) OR (EndLat BETWEEN 5 AND 24 AND EndLng BETWEEN 101.5 AND 118.5)));';
COMMIT TRANSACTION;
