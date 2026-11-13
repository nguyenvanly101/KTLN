-- Anh bia cho ho so ca nhan (kieu Facebook). Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.AnhBiaHoSo.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp ảnh bìa.',1;
IF OBJECT_ID(N'dbo.HoSoNguoiDung',N'U') IS NOT NULL AND COL_LENGTH(N'dbo.HoSoNguoiDung',N'AnhBia') IS NULL
 EXEC sys.sp_executesql N'ALTER TABLE dbo.HoSoNguoiDung ADD [AnhBia] nvarchar(400) NULL;';
COMMIT TRANSACTION;
