-- Dua co so da cong bo vao chi muc tra cuu chung: DiscoveryEntry.BranchId (NULL) + chi muc duy nhat + rang buoc lien ket.
-- Co the chay lai. Khong xoa du lieu. Muc tra cuu cua co so co Kind='Service', PlaceId va ServiceId deu NULL.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.CoSoVaoTraCuu.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp chỉ mục cơ sở.',1;

IF COL_LENGTH(N'dbo.DiscoveryEntry',N'BranchId') IS NULL
 EXEC sys.sp_executesql N'ALTER TABLE dbo.[DiscoveryEntry] ADD [BranchId] bigint NULL;';

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_DiscoveryEntry_Branch')
 EXEC sys.sp_executesql N'ALTER TABLE dbo.[DiscoveryEntry] ADD CONSTRAINT [FK_DiscoveryEntry_Branch] FOREIGN KEY ([BranchId]) REFERENCES dbo.[CoSoDichVu]([Id]);';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_DS_Branch' AND object_id=OBJECT_ID(N'dbo.DiscoveryEntry'))
 EXEC sys.sp_executesql N'CREATE UNIQUE INDEX [UX_DS_Branch] ON dbo.[DiscoveryEntry] ([BranchId]) WHERE BranchId IS NOT NULL;';

-- Rang buoc lien ket moi: co so chi di voi Kind='Service' va khong dong thoi gan dia diem/dich vu.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_DS_Link2' AND parent_object_id=OBJECT_ID(N'dbo.DiscoveryEntry'))
BEGIN
 IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_DS_Link' AND parent_object_id=OBJECT_ID(N'dbo.DiscoveryEntry'))
  EXEC sys.sp_executesql N'ALTER TABLE dbo.[DiscoveryEntry] DROP CONSTRAINT [CK_DS_Link];';
 EXEC sys.sp_executesql N'ALTER TABLE dbo.[DiscoveryEntry] WITH CHECK ADD CONSTRAINT [CK_DS_Link2] CHECK ((PlaceId IS NULL OR ServiceId IS NULL) AND (PlaceId IS NULL OR Kind=''Place'') AND (ServiceId IS NULL OR Kind=''Service'') AND (BranchId IS NULL OR (PlaceId IS NULL AND ServiceId IS NULL AND Kind=''Service'')));';
END;
COMMIT TRANSACTION;
