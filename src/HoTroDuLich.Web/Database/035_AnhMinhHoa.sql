-- Anh minh hoa cho muc tra cuu va tinh/thanh: ghi cong tac gia, giay phep, trang nguon (Wikimedia Commons). Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.AnhMinhHoa.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp ảnh minh họa.',1;
IF OBJECT_ID(N'dbo.DiscoveryEntry',N'U') IS NOT NULL AND COL_LENGTH(N'dbo.DiscoveryEntry',N'ImageCredit') IS NULL
 EXEC sys.sp_executesql N'ALTER TABLE dbo.DiscoveryEntry ADD ImageCredit nvarchar(300) NULL, ImageSourceUrl nvarchar(500) NULL;';
IF OBJECT_ID(N'dbo.AnhDonVi',N'U') IS NULL AND OBJECT_ID(N'dbo.DonViHanhChinh',N'U') IS NOT NULL
 EXEC sys.sp_executesql N'CREATE TABLE dbo.AnhDonVi(
  DonViId bigint NOT NULL CONSTRAINT PK_AnhDonVi PRIMARY KEY
   CONSTRAINT FK_AnhDonVi_DonVi FOREIGN KEY REFERENCES dbo.DonViHanhChinh(Id) ON DELETE CASCADE,
  ImagePath nvarchar(300) NOT NULL,
  Credit nvarchar(300) NOT NULL,
  SourceUrl nvarchar(500) NOT NULL,
  UpdatedAt datetime2(7) NOT NULL CONSTRAINT DF_AnhDonVi_Updated DEFAULT SYSUTCDATETIME());';
COMMIT TRANSACTION;
