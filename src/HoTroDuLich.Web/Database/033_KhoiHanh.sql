-- Khoi hanh va ket thuc chuyen di, danh dau bo qua hang muc. Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.KhoiHanh.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp khởi hành.',1;
IF OBJECT_ID(N'dbo.HoatDongLichTrinh',N'U') IS NOT NULL AND COL_LENGTH(N'dbo.HoatDongLichTrinh',N'BoQuaLuc') IS NULL
 EXEC sys.sp_executesql N'ALTER TABLE dbo.HoatDongLichTrinh ADD BoQuaLuc datetime2(7) NULL;';
IF OBJECT_ID(N'dbo.TienDoChuyenDi',N'U') IS NULL AND OBJECT_ID(N'dbo.KeHoachChuyenDi',N'U') IS NOT NULL
 EXEC sys.sp_executesql N'CREATE TABLE dbo.TienDoChuyenDi(
  PlanId bigint NOT NULL CONSTRAINT PK_TienDoChuyenDi PRIMARY KEY
   CONSTRAINT FK_TienDo_Plan FOREIGN KEY REFERENCES dbo.KeHoachChuyenDi(MaKeHoach) ON DELETE CASCADE,
  DepartedAt datetime2(7) NULL,
  FinishedAt datetime2(7) NULL,
  CONSTRAINT CK_TienDo_Order CHECK (FinishedAt IS NULL OR (DepartedAt IS NOT NULL AND FinishedAt>=DepartedAt)));';
COMMIT TRANSACTION;
