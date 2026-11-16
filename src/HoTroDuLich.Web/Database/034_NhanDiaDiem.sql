-- Ghi nhan co so duoc tao tu viec nhan mot dia diem co san trong Kham pha. Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.NhanDiaDiem.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp nhận địa điểm.',1;
IF OBJECT_ID(N'dbo.NhanDiaDiemKhamPha',N'U') IS NULL AND OBJECT_ID(N'dbo.CoSoDichVu',N'U') IS NOT NULL
 EXEC sys.sp_executesql N'CREATE TABLE dbo.NhanDiaDiemKhamPha(
  BranchId bigint NOT NULL CONSTRAINT PK_NhanDiaDiemKhamPha PRIMARY KEY
   CONSTRAINT FK_NhanDiaDiem_Branch FOREIGN KEY REFERENCES dbo.CoSoDichVu(Id) ON DELETE CASCADE,
  EntryId bigint NOT NULL,
  CreatedAt datetime2(7) NOT NULL CONSTRAINT DF_NhanDiaDiem_Created DEFAULT SYSUTCDATETIME(),
  HiddenAt datetime2(7) NULL);';
COMMIT TRANSACTION;
