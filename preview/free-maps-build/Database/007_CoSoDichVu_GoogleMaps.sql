-- Branches, exact landmarks and service-to-branch publication, extension 007.
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
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.Branches.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp cơ sở dịch vụ.',1;
IF OBJECT_ID(N'dbo.CoSoDichVu',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[CoSoDichVu] (
  [Id] bigint IDENTITY(1,1) NOT NULL,
  [ProviderId] bigint NOT NULL,
  [Name] nvarchar(200) NOT NULL,
  [Type] varchar(30) NOT NULL,
  [Region] nvarchar(200) NOT NULL,
  [Address] nvarchar(400) NOT NULL,
  [Summary] nvarchar(2000) NULL,
  [Phone] nvarchar(30) NULL,
  [Website] nvarchar(500) NULL,
  [Hours] nvarchar(300) NULL,
  [Parking] bit NULL,
  [VegetarianOptions] bit NULL,
  [VeganOnly] bit NULL,
  [AccessibleEntrance] bit NULL,
  [GooglePlaceId] nvarchar(256) COLLATE Latin1_General_100_BIN2 NULL,
  [Latitude] decimal(9,6) NULL,
  [Longitude] decimal(9,6) NULL,
  [LocationSource] varchar(15) NOT NULL,
  [LocationExpiresAt] datetime2(0) NULL,
  [Status] varchar(25) NOT NULL,
  [PublishedVersion] int NULL,
  [CreatedAt] datetime2(0) NOT NULL CONSTRAINT [DF_CoSoDichVu_CreatedAt] DEFAULT (SYSUTCDATETIME()),
  [UpdatedAt] datetime2(0) NOT NULL CONSTRAINT [DF_CoSoDichVu_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_CoSoDichVu] PRIMARY KEY ([Id]),
  CONSTRAINT [CK_CoSoDichVu_Location] CHECK (((Latitude IS NULL AND Longitude IS NULL) OR (Latitude IS NOT NULL AND Longitude IS NOT NULL AND Latitude BETWEEN -90 AND 90 AND Longitude BETWEEN -180 AND 180)) AND ((LocationSource='Provider' AND LocationExpiresAt IS NULL) OR (LocationSource='Google' AND GooglePlaceId IS NOT NULL))),
  CONSTRAINT [CK_CoSoDichVu_Type] CHECK (Type IN ('LuuTru','AnUong','PhuongTien','HoatDong','Khac')),
  CONSTRAINT [CK_CoSoDichVu_Status] CHECK (Status IN ('Draft','Pending','Approved','Rejected','ChangesRequested','Hidden')),
  CONSTRAINT [FK_CoSoDichVu_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES dbo.[NhaCungCapDichVu]([MaNhaCungCap])
 );
 CREATE INDEX [IX_CoSo_Provider] ON dbo.[CoSoDichVu] ([ProviderId],[Status]);
 CREATE INDEX [IX_CoSo_Search] ON dbo.[CoSoDichVu] ([Status],[Type],[Region]);
 CREATE UNIQUE INDEX [UX_CoSo_GooglePlace] ON dbo.[CoSoDichVu] ([GooglePlaceId]) WHERE GooglePlaceId IS NOT NULL AND PublishedVersion IS NOT NULL;
END;
IF OBJECT_ID(N'dbo.BanCapNhatCoSo',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[BanCapNhatCoSo] (
  [Id] bigint IDENTITY(1,1) NOT NULL,
  [BranchId] bigint NOT NULL,
  [Number] int NOT NULL,
  [Name] nvarchar(200) NOT NULL,
  [Type] varchar(30) NOT NULL,
  [Region] nvarchar(200) NOT NULL,
  [Address] nvarchar(400) NOT NULL,
  [Summary] nvarchar(2000) NULL,
  [Phone] nvarchar(30) NULL,
  [Website] nvarchar(500) NULL,
  [Hours] nvarchar(300) NULL,
  [Parking] bit NULL,
  [VegetarianOptions] bit NULL,
  [VeganOnly] bit NULL,
  [AccessibleEntrance] bit NULL,
  [GooglePlaceId] nvarchar(256) COLLATE Latin1_General_100_BIN2 NULL,
  [Latitude] decimal(9,6) NULL,
  [Longitude] decimal(9,6) NULL,
  [LocationSource] varchar(15) NOT NULL,
  [LocationExpiresAt] datetime2(0) NULL,
  [Status] varchar(25) NOT NULL,
  [OwnershipEvidence] nvarchar(2000) NOT NULL,
  [Reason] nvarchar(1000) NULL,
  [AdminId] bigint NULL,
  [SubmittedAt] datetime2(0) NOT NULL CONSTRAINT [DF_BanCapNhatCoSo_SubmittedAt] DEFAULT (SYSUTCDATETIME()),
  [ReviewedAt] datetime2(0) NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_BanCapNhatCoSo] PRIMARY KEY ([Id]),
  CONSTRAINT [CK_BanCapNhatCoSo_Location] CHECK (((Latitude IS NULL AND Longitude IS NULL) OR (Latitude IS NOT NULL AND Longitude IS NOT NULL AND Latitude BETWEEN -90 AND 90 AND Longitude BETWEEN -180 AND 180)) AND ((LocationSource='Provider' AND LocationExpiresAt IS NULL) OR (LocationSource='Google' AND GooglePlaceId IS NOT NULL))),
  CONSTRAINT [CK_BanCapNhatCoSo_Number] CHECK (Number>0),
  CONSTRAINT [CK_BanCapNhatCoSo_Status] CHECK (Status IN ('Pending','Approved','Rejected','ChangesRequested')),
  CONSTRAINT [CK_BanCapNhatCoSo_Type] CHECK (Type IN ('LuuTru','AnUong','PhuongTien','HoatDong','Khac')),
  CONSTRAINT [FK_BanCapNhatCoSo_BranchId] FOREIGN KEY ([BranchId]) REFERENCES dbo.[CoSoDichVu]([Id]),
  CONSTRAINT [FK_BanCapNhatCoSo_AdminId] FOREIGN KEY ([AdminId]) REFERENCES dbo.[TaiKhoan]([MaTaiKhoan])
 );
 CREATE UNIQUE INDEX [UX_CoSoRevision_Number] ON dbo.[BanCapNhatCoSo] ([BranchId],[Number]);
 CREATE UNIQUE INDEX [UX_CoSoRevision_Pending] ON dbo.[BanCapNhatCoSo] ([BranchId]) WHERE Status='Pending';
 CREATE INDEX [IX_CoSoRevision_Queue] ON dbo.[BanCapNhatCoSo] ([Status],[SubmittedAt]);
END;
IF OBJECT_ID(N'dbo.DichVuCoSo',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[DichVuCoSo] (
  [ServiceId] bigint NOT NULL,
  [BranchId] bigint NOT NULL,
  CONSTRAINT [PK_DichVuCoSo] PRIMARY KEY ([ServiceId],[BranchId]),
  CONSTRAINT [FK_DichVuCoSo_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES dbo.[DichVuDuLich]([MaDichVu]),
  CONSTRAINT [FK_DichVuCoSo_BranchId] FOREIGN KEY ([BranchId]) REFERENCES dbo.[CoSoDichVu]([Id])
 );
 CREATE INDEX [IX_DichVuCoSo_Branch] ON dbo.[DichVuCoSo] ([BranchId],[ServiceId]);
END;
IF OBJECT_ID(N'dbo.BanCapNhatDichVuCoSo',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[BanCapNhatDichVuCoSo] (
  [RevisionId] bigint NOT NULL,
  [BranchId] bigint NOT NULL,
  CONSTRAINT [PK_BanCapNhatDichVuCoSo] PRIMARY KEY ([RevisionId],[BranchId]),
  CONSTRAINT [FK_BanCapNhatDichVuCoSo_RevisionId] FOREIGN KEY ([RevisionId]) REFERENCES dbo.[BanCapNhatDichVu]([MaBanCapNhat]),
  CONSTRAINT [FK_BanCapNhatDichVuCoSo_BranchId] FOREIGN KEY ([BranchId]) REFERENCES dbo.[CoSoDichVu]([Id])
 );
 CREATE INDEX [IX_BanCapNhatDichVuCoSo_Branch] ON dbo.[BanCapNhatDichVuCoSo] ([BranchId],[RevisionId]);
END;
IF OBJECT_ID(N'dbo.MocDiaDiem',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[MocDiaDiem] (
  [Id] bigint IDENTITY(1,1) NOT NULL,
  [ParentId] bigint NULL,
  [Name] nvarchar(200) NOT NULL,
  [Type] varchar(15) NOT NULL,
  [Region] nvarchar(200) NOT NULL,
  [Address] nvarchar(400) NOT NULL,
  [Summary] nvarchar(2000) NULL,
  [Hours] nvarchar(300) NULL,
  [Fees] nvarchar(1000) NULL,
  [GooglePlaceId] nvarchar(256) COLLATE Latin1_General_100_BIN2 NULL,
  [Latitude] decimal(9,6) NULL,
  [Longitude] decimal(9,6) NULL,
  [LocationSource] varchar(15) NOT NULL,
  [LocationExpiresAt] datetime2(0) NULL,
  [SourceName] nvarchar(200) NOT NULL,
  [SourceUrl] nvarchar(500) NULL,
  [CheckedOn] date NOT NULL,
  [IsPublished] bit NOT NULL,
  [AdminId] bigint NOT NULL,
  [UpdatedAt] datetime2(0) NOT NULL CONSTRAINT [DF_MocDiaDiem_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_MocDiaDiem] PRIMARY KEY ([Id]),
  CONSTRAINT [CK_MocDiaDiem_Location] CHECK (((Latitude IS NULL AND Longitude IS NULL) OR (Latitude IS NOT NULL AND Longitude IS NOT NULL AND Latitude BETWEEN -90 AND 90 AND Longitude BETWEEN -180 AND 180)) AND ((LocationSource='Provider' AND LocationExpiresAt IS NULL) OR (LocationSource='Google' AND GooglePlaceId IS NOT NULL))),
  CONSTRAINT [CK_MocDiaDiem_Type] CHECK (Type IN ('Point','Entrance')),
  CONSTRAINT [CK_MocDiaDiem_Parent] CHECK (ParentId IS NULL OR ParentId<>Id),
  CONSTRAINT [FK_MocDiaDiem_ParentId] FOREIGN KEY ([ParentId]) REFERENCES dbo.[MocDiaDiem]([Id]),
  CONSTRAINT [FK_MocDiaDiem_AdminId] FOREIGN KEY ([AdminId]) REFERENCES dbo.[TaiKhoan]([MaTaiKhoan])
 );
 CREATE INDEX [IX_MocDiaDiem_Parent] ON dbo.[MocDiaDiem] ([ParentId]);
 CREATE INDEX [IX_MocDiaDiem_Search] ON dbo.[MocDiaDiem] ([IsPublished],[Region]);
 CREATE UNIQUE INDEX [UX_MocDiaDiem_GooglePlace] ON dbo.[MocDiaDiem] ([GooglePlaceId]) WHERE GooglePlaceId IS NOT NULL;
END;
IF OBJECT_ID(N'dbo.YeuCauDatCoSo',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[YeuCauDatCoSo] (
  [RequestId] bigint NOT NULL,
  [BranchId] bigint NOT NULL,
  [BranchName] nvarchar(200) NOT NULL,
  [Address] nvarchar(400) NOT NULL,
  [GooglePlaceId] nvarchar(256) COLLATE Latin1_General_100_BIN2 NULL,
  [ProviderLatitude] decimal(9,6) NULL,
  [ProviderLongitude] decimal(9,6) NULL,
  CONSTRAINT [PK_YeuCauDatCoSo] PRIMARY KEY ([RequestId]),
  CONSTRAINT [FK_YeuCauDatCoSo_RequestId] FOREIGN KEY ([RequestId]) REFERENCES dbo.[YeuCauDatDichVu]([MaYeuCau]),
  CONSTRAINT [FK_YeuCauDatCoSo_BranchId] FOREIGN KEY ([BranchId]) REFERENCES dbo.[CoSoDichVu]([Id])
 );
 CREATE INDEX [IX_YeuCauDatCoSo_Branch] ON dbo.[YeuCauDatCoSo] ([BranchId],[RequestId]);
END;
IF (SELECT collation_name FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.CoSoDichVu') AND name=N'GooglePlaceId')<>N'Latin1_General_100_BIN2'
BEGIN
 ALTER TABLE dbo.[CoSoDichVu] DROP CONSTRAINT [CK_CoSoDichVu_Location];
 DROP INDEX [UX_CoSo_GooglePlace] ON dbo.[CoSoDichVu];
 ALTER TABLE dbo.[CoSoDichVu] ALTER COLUMN [GooglePlaceId] nvarchar(256) COLLATE Latin1_General_100_BIN2 NULL;
 ALTER TABLE dbo.[CoSoDichVu] WITH CHECK ADD CONSTRAINT [CK_CoSoDichVu_Location] CHECK (((Latitude IS NULL AND Longitude IS NULL) OR (Latitude IS NOT NULL AND Longitude IS NOT NULL AND Latitude BETWEEN -90 AND 90 AND Longitude BETWEEN -180 AND 180)) AND ((LocationSource='Provider' AND LocationExpiresAt IS NULL) OR (LocationSource='Google' AND GooglePlaceId IS NOT NULL)));
 CREATE UNIQUE INDEX [UX_CoSo_GooglePlace] ON dbo.[CoSoDichVu] ([GooglePlaceId]) WHERE GooglePlaceId IS NOT NULL AND PublishedVersion IS NOT NULL;
END;
IF (SELECT collation_name FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.BanCapNhatCoSo') AND name=N'GooglePlaceId')<>N'Latin1_General_100_BIN2'
BEGIN
 ALTER TABLE dbo.[BanCapNhatCoSo] DROP CONSTRAINT [CK_BanCapNhatCoSo_Location];
 ALTER TABLE dbo.[BanCapNhatCoSo] ALTER COLUMN [GooglePlaceId] nvarchar(256) COLLATE Latin1_General_100_BIN2 NULL;
 ALTER TABLE dbo.[BanCapNhatCoSo] WITH CHECK ADD CONSTRAINT [CK_BanCapNhatCoSo_Location] CHECK (((Latitude IS NULL AND Longitude IS NULL) OR (Latitude IS NOT NULL AND Longitude IS NOT NULL AND Latitude BETWEEN -90 AND 90 AND Longitude BETWEEN -180 AND 180)) AND ((LocationSource='Provider' AND LocationExpiresAt IS NULL) OR (LocationSource='Google' AND GooglePlaceId IS NOT NULL)));
END;
IF (SELECT collation_name FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.MocDiaDiem') AND name=N'GooglePlaceId')<>N'Latin1_General_100_BIN2'
BEGIN
 ALTER TABLE dbo.[MocDiaDiem] DROP CONSTRAINT [CK_MocDiaDiem_Location];
 DROP INDEX [UX_MocDiaDiem_GooglePlace] ON dbo.[MocDiaDiem];
 ALTER TABLE dbo.[MocDiaDiem] ALTER COLUMN [GooglePlaceId] nvarchar(256) COLLATE Latin1_General_100_BIN2 NULL;
 ALTER TABLE dbo.[MocDiaDiem] WITH CHECK ADD CONSTRAINT [CK_MocDiaDiem_Location] CHECK (((Latitude IS NULL AND Longitude IS NULL) OR (Latitude IS NOT NULL AND Longitude IS NOT NULL AND Latitude BETWEEN -90 AND 90 AND Longitude BETWEEN -180 AND 180)) AND ((LocationSource='Provider' AND LocationExpiresAt IS NULL) OR (LocationSource='Google' AND GooglePlaceId IS NOT NULL)));
 CREATE UNIQUE INDEX [UX_MocDiaDiem_GooglePlace] ON dbo.[MocDiaDiem] ([GooglePlaceId]) WHERE GooglePlaceId IS NOT NULL;
END;
IF (SELECT collation_name FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.YeuCauDatCoSo') AND name=N'GooglePlaceId')<>N'Latin1_General_100_BIN2'
BEGIN
 ALTER TABLE dbo.[YeuCauDatCoSo] ALTER COLUMN [GooglePlaceId] nvarchar(256) COLLATE Latin1_General_100_BIN2 NULL;
END;
IF EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CoSoDichVu') AND name=N'UX_CoSo_GooglePlace' AND filter_definition NOT LIKE N'%PublishedVersion%')
BEGIN
 DROP INDEX UX_CoSo_GooglePlace ON dbo.CoSoDichVu;
 CREATE UNIQUE INDEX UX_CoSo_GooglePlace ON dbo.CoSoDichVu(GooglePlaceId) WHERE GooglePlaceId IS NOT NULL AND PublishedVersion IS NOT NULL;
END;
COMMIT TRANSACTION;
