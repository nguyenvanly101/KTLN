-- Bang tin nang cao: chia se bai viet, luu bai viet, theo doi co so (Trang). Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.BangTinNangCao.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp bảng tin.',1;
IF OBJECT_ID(N'dbo.BaiViet',N'U') IS NOT NULL
BEGIN
 IF COL_LENGTH(N'dbo.BaiViet',N'SharedPostId') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.BaiViet ADD [SharedPostId] bigint NULL;';
 IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_BaiViet_Shared' AND object_id=OBJECT_ID(N'dbo.BaiViet'))
  EXEC sys.sp_executesql N'CREATE INDEX IX_BaiViet_Shared ON dbo.BaiViet([SharedPostId]) WHERE [SharedPostId] IS NOT NULL;';
END;
IF OBJECT_ID(N'dbo.BaiVietDaLuu',N'U') IS NULL AND OBJECT_ID(N'dbo.BaiViet',N'U') IS NOT NULL
 EXEC sys.sp_executesql N'CREATE TABLE dbo.BaiVietDaLuu(
  AccountId bigint NOT NULL CONSTRAINT FK_BvLuu_Account FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  PostId bigint NOT NULL CONSTRAINT FK_BvLuu_Post FOREIGN KEY REFERENCES dbo.BaiViet(Id),
  CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_BvLuu_Created DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_BaiVietDaLuu PRIMARY KEY (AccountId,PostId));';
IF OBJECT_ID(N'dbo.TheoDoiCoSo',N'U') IS NULL AND OBJECT_ID(N'dbo.TaiKhoan',N'U') IS NOT NULL
 EXEC sys.sp_executesql N'CREATE TABLE dbo.TheoDoiCoSo(
  AccountId bigint NOT NULL CONSTRAINT FK_TdCs_Account FOREIGN KEY REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  BranchId bigint NOT NULL,
  CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_TdCs_Created DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_TheoDoiCoSo PRIMARY KEY (AccountId,BranchId));
  CREATE INDEX IX_TdCs_Branch ON dbo.TheoDoiCoSo(BranchId);';
COMMIT TRANSACTION;
