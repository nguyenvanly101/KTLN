-- Tin nhan kieu Messenger: cuoc tro chuyen co the la khach <-> co so HOAC nguoi <-> nguoi (vi du tim ban dong hanh).
-- BranchId cho phep rong; PeerId la nguoi con lai khi khong co co so; BlockedById la nguoi da chan. Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.TinNhanNguoiVoiNguoi.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp tin nhắn giữa mọi người.',1;

IF OBJECT_ID(N'dbo.CuocTroChuyen',N'U') IS NOT NULL
BEGIN
 IF COL_LENGTH(N'dbo.CuocTroChuyen',N'PeerId') IS NULL
  EXEC sys.sp_executesql N'ALTER TABLE dbo.CuocTroChuyen ADD PeerId bigint NULL;';
 IF COL_LENGTH(N'dbo.CuocTroChuyen',N'BlockedById') IS NULL
  EXEC sys.sp_executesql N'ALTER TABLE dbo.CuocTroChuyen ADD BlockedById bigint NULL;';
 IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.CuocTroChuyen') AND name=N'BranchId' AND is_nullable=0)
 BEGIN
  IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name=N'UX_Chat_Pair') EXEC sys.sp_executesql N'ALTER TABLE dbo.CuocTroChuyen DROP CONSTRAINT UX_Chat_Pair;';
  IF EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_Chat_Pair' AND object_id=OBJECT_ID(N'dbo.CuocTroChuyen')) EXEC sys.sp_executesql N'DROP INDEX UX_Chat_Pair ON dbo.CuocTroChuyen;';
  IF EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Chat_Branch' AND object_id=OBJECT_ID(N'dbo.CuocTroChuyen')) EXEC sys.sp_executesql N'DROP INDEX IX_Chat_Branch ON dbo.CuocTroChuyen;';
  EXEC sys.sp_executesql N'ALTER TABLE dbo.CuocTroChuyen ALTER COLUMN BranchId bigint NULL;';
 END;
 IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Chat_Peer')
  EXEC sys.sp_executesql N'ALTER TABLE dbo.CuocTroChuyen ADD CONSTRAINT FK_Chat_Peer FOREIGN KEY (PeerId) REFERENCES dbo.TaiKhoan(MaTaiKhoan);';
 IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Chat_Blocked')
  EXEC sys.sp_executesql N'ALTER TABLE dbo.CuocTroChuyen ADD CONSTRAINT FK_Chat_Blocked FOREIGN KEY (BlockedById) REFERENCES dbo.TaiKhoan(MaTaiKhoan);';
 IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Chat_Kind')
  EXEC sys.sp_executesql N'ALTER TABLE dbo.CuocTroChuyen ADD CONSTRAINT CK_Chat_Kind CHECK ((BranchId IS NOT NULL AND PeerId IS NULL) OR (BranchId IS NULL AND PeerId IS NOT NULL AND PeerId<>TravelerId));';
 IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_Chat_BranchPair' AND object_id=OBJECT_ID(N'dbo.CuocTroChuyen'))
  EXEC sys.sp_executesql N'CREATE UNIQUE INDEX UX_Chat_BranchPair ON dbo.CuocTroChuyen(BranchId,TravelerId) WHERE BranchId IS NOT NULL;';
 IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_Chat_PeerPair' AND object_id=OBJECT_ID(N'dbo.CuocTroChuyen'))
  EXEC sys.sp_executesql N'CREATE UNIQUE INDEX UX_Chat_PeerPair ON dbo.CuocTroChuyen(TravelerId,PeerId) WHERE PeerId IS NOT NULL;';
 IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Chat_Branch' AND object_id=OBJECT_ID(N'dbo.CuocTroChuyen'))
  EXEC sys.sp_executesql N'CREATE INDEX IX_Chat_Branch ON dbo.CuocTroChuyen(BranchId,State,LastMessageAt DESC) WHERE BranchId IS NOT NULL;';
 IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Chat_Peer' AND object_id=OBJECT_ID(N'dbo.CuocTroChuyen'))
  EXEC sys.sp_executesql N'CREATE INDEX IX_Chat_Peer ON dbo.CuocTroChuyen(PeerId,LastMessageAt DESC) WHERE PeerId IS NOT NULL;';
END;
COMMIT TRANSACTION;
