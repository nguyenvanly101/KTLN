-- BAN NHAP - CHUA CHAY TREN DATABASE NAO.
-- Goi 1: dia gioi hanh chinh (he cu 63 tinh/huyen/xa va he moi 34 tinh/xa), anh xa cu-moi, ten goi khac, khu du lich, hang cho chuyen doi.
-- Co the chay lai. Khong xoa cot Region/KhuVuc cu; chi them cot DonViId (NULL) de chuyen doi dan.
-- Dung web, sao luu database truoc khi chay. Nap du lieu bang 008c_NapDiaGioi.sql (sinh boi tools/BuildDiaGioi.py).
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
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.DiaGioi.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp địa giới hành chính.',1;

-- 1. Don vi hanh chinh. Hai che do cung ton tai (nguon: bang chuyen doi DVHC moi-cu cua NSO):
--    'Cu'  = truoc 01/07/2025: 63 tinh/thanh -> quan/huyen/thi xa -> xa/phuong/thi tran (3 cap)
--    'Moi' = tu 01/07/2025   : 34 tinh/thanh -> xa/phuong/dac khu (2 cap, khong co cap huyen)
--    Level: 1 = tinh/thanh, 2 = quan/huyen (chi 'Cu'), 3 = xa/phuong/thi tran/dac khu. ParentId cua cap 3 'Moi' tro thang len tinh.
--    Status = don vi co duoc he thong dung de chon hay khong (khong phai trang thai phap ly; phap ly nam o EffectiveFrom/EffectiveTo).
IF OBJECT_ID(N'dbo.DonViHanhChinh',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[DonViHanhChinh] (
  [Id] bigint IDENTITY(1,1) NOT NULL,
  [Regime] varchar(4) NOT NULL,
  [Code] varchar(12) NOT NULL,                -- ma cua nguon (NSO); ma tam co hau to X can doi chieu
  [ParentId] bigint NULL,
  [Level] tinyint NOT NULL,
  [Kind] varchar(20) NOT NULL,
  [Name] nvarchar(100) NOT NULL,              -- ten ngan, bo tien to: "Hai Chau"
  [FullName] nvarchar(150) NOT NULL,          -- "Phuong Hai Chau"
  [NameFolded] nvarchar(150) COLLATE Latin1_General_100_CI_AI NOT NULL, -- ten ngan bo dau, thuong, d -> d
  [Slug] varchar(160) NOT NULL,
  [CenterLat] decimal(9,6) NULL,
  [CenterLng] decimal(9,6) NULL,
  [Boundary] geography NULL,
  [Status] varchar(10) NOT NULL CONSTRAINT [DF_DonViHanhChinh_Status] DEFAULT ('Active'),
  [EffectiveFrom] date NOT NULL,
  [EffectiveTo] date NULL,
  [SourceName] nvarchar(200) NOT NULL,
  [CheckedOn] date NOT NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_DonViHanhChinh] PRIMARY KEY CLUSTERED ([Id]),
  CONSTRAINT [FK_DonViHanhChinh_Parent] FOREIGN KEY ([ParentId]) REFERENCES dbo.[DonViHanhChinh]([Id]),
  CONSTRAINT [UQ_DonViHanhChinh_Code] UNIQUE ([Regime],[Code]),
  CONSTRAINT [UQ_DonViHanhChinh_Slug] UNIQUE ([Regime],[Slug]),
  CONSTRAINT [CK_DonViHanhChinh_Regime] CHECK ([Regime] IN ('Cu','Moi')),
  CONSTRAINT [CK_DonViHanhChinh_Level] CHECK (
    ([Level]=1 AND [ParentId] IS NULL AND [Kind] IN ('Tinh','ThanhPho'))
    OR ([Level]=2 AND [Regime]='Cu' AND [ParentId] IS NOT NULL AND [Kind] IN ('Quan','Huyen','ThiXa','ThanhPhoThuocTinh'))
    OR ([Level]=3 AND [ParentId] IS NOT NULL AND [Kind] IN ('Phuong','Xa','ThiTran','DacKhu'))),
  CONSTRAINT [CK_DonViHanhChinh_Status] CHECK ([Status] IN ('Active','Retired')),
  CONSTRAINT [CK_DonViHanhChinh_Center] CHECK (([CenterLat] IS NULL AND [CenterLng] IS NULL) OR ([CenterLat] BETWEEN -90 AND 90 AND [CenterLng] BETWEEN -180 AND 180)),
  CONSTRAINT [CK_DonViHanhChinh_Effective] CHECK ([EffectiveTo] IS NULL OR [EffectiveTo]>=[EffectiveFrom]),
  CONSTRAINT [CK_DonViHanhChinh_Name] CHECK (LEN(LTRIM([Name]))>0 AND LEN(LTRIM([NameFolded]))>0)
 );
 CREATE INDEX [IX_DonViHanhChinh_Parent] ON dbo.[DonViHanhChinh] ([ParentId],[Status]) INCLUDE ([Name],[Kind],[Slug]);
 CREATE INDEX [IX_DonViHanhChinh_Folded] ON dbo.[DonViHanhChinh] ([Regime],[NameFolded]) INCLUDE ([Level],[ParentId],[Status]);
END;

-- 1b. Anh xa don vi cu -> moi (mot xa cu co the nhap mot phan vao nhieu xa moi, mot xa moi gom nhieu xa cu).
IF OBJECT_ID(N'dbo.DonViChuyenDoi',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[DonViChuyenDoi] (
  [OldUnitId] bigint NOT NULL,
  [NewUnitId] bigint NOT NULL,
  [Note] nvarchar(200) NOT NULL,
  CONSTRAINT [PK_DonViChuyenDoi] PRIMARY KEY CLUSTERED ([OldUnitId],[NewUnitId]),
  CONSTRAINT [FK_DonViChuyenDoi_Old] FOREIGN KEY ([OldUnitId]) REFERENCES dbo.[DonViHanhChinh]([Id]),
  CONSTRAINT [FK_DonViChuyenDoi_New] FOREIGN KEY ([NewUnitId]) REFERENCES dbo.[DonViHanhChinh]([Id])
 );
 CREATE INDEX [IX_DonViChuyenDoi_New] ON dbo.[DonViChuyenDoi] ([NewUnitId]) INCLUDE ([OldUnitId]);
END;

-- 2. Ten goi khac: ten cu (quan Hai Chau), viet tat (TP.HCM), khong dau, ten nuoc ngoai (Da Nang).
-- Mot ten co the tro toi nhieu don vi (vd "Phuong 1"); khi tra cuu gap nhieu ket qua thi hoi lai nguoi dung.
IF OBJECT_ID(N'dbo.TenGoiKhacDonVi',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[TenGoiKhacDonVi] (
  [Id] bigint IDENTITY(1,1) NOT NULL,
  [UnitId] bigint NOT NULL,
  [Alias] nvarchar(150) NOT NULL,
  [NameFolded] nvarchar(150) COLLATE Latin1_General_100_CI_AI NOT NULL,
  [AliasKind] varchar(15) NOT NULL,           -- TenCu, CapCu, VietTat, NuocNgoai, Khac
  [Note] nvarchar(300) NULL,
  CONSTRAINT [PK_TenGoiKhacDonVi] PRIMARY KEY CLUSTERED ([Id]),
  CONSTRAINT [FK_TenGoiKhacDonVi_Unit] FOREIGN KEY ([UnitId]) REFERENCES dbo.[DonViHanhChinh]([Id]),
  CONSTRAINT [UQ_TenGoiKhacDonVi] UNIQUE ([UnitId],[NameFolded]),
  CONSTRAINT [CK_TenGoiKhacDonVi_Kind] CHECK ([AliasKind] IN ('TenCu','CapCu','VietTat','NuocNgoai','Khac')),
  CONSTRAINT [CK_TenGoiKhacDonVi_Alias] CHECK (LEN(LTRIM([Alias]))>0)
 );
 CREATE INDEX [IX_TenGoiKhacDonVi_Folded] ON dbo.[TenGoiKhacDonVi] ([NameFolded]) INCLUDE ([UnitId]);
END;

-- 3. Khu du lich: lop chong len dia gioi (Ban dao Son Tra, Pho co Hoi An...). Mot khu co the trai nhieu phuong/xa.
IF OBJECT_ID(N'dbo.KhuDuLich',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[KhuDuLich] (
  [Id] bigint IDENTITY(1,1) NOT NULL,
  [ProvinceId] bigint NOT NULL,
  [Slug] varchar(120) NOT NULL,
  [Name] nvarchar(150) NOT NULL,
  [NameFolded] nvarchar(150) COLLATE Latin1_General_100_CI_AI NOT NULL,
  [Kind] varchar(15) NOT NULL,                -- Beach, Mountain, Heritage, OldTown, Island, Park, Resort, Other
  [Summary] nvarchar(700) NULL,
  [CenterLat] decimal(9,6) NOT NULL,
  [CenterLng] decimal(9,6) NOT NULL,
  [RadiusM] int NULL,
  [SortOrder] int NOT NULL CONSTRAINT [DF_KhuDuLich_Sort] DEFAULT (100),
  [IsPublished] bit NOT NULL CONSTRAINT [DF_KhuDuLich_Pub] DEFAULT (0),
  [SourceName] nvarchar(200) NOT NULL,
  [CheckedOn] date NOT NULL,
  [UpdatedAt] datetime2(0) NOT NULL CONSTRAINT [DF_KhuDuLich_Upd] DEFAULT (SYSUTCDATETIME()),
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_KhuDuLich] PRIMARY KEY CLUSTERED ([Id]),
  CONSTRAINT [FK_KhuDuLich_Province] FOREIGN KEY ([ProvinceId]) REFERENCES dbo.[DonViHanhChinh]([Id]),
  CONSTRAINT [UQ_KhuDuLich_Slug] UNIQUE ([Slug]),
  CONSTRAINT [CK_KhuDuLich_Kind] CHECK ([Kind] IN ('Beach','Mountain','Heritage','OldTown','Island','Park','Resort','Other')),
  CONSTRAINT [CK_KhuDuLich_Center] CHECK ([CenterLat] BETWEEN -90 AND 90 AND [CenterLng] BETWEEN -180 AND 180),
  CONSTRAINT [CK_KhuDuLich_Radius] CHECK ([RadiusM] IS NULL OR [RadiusM] BETWEEN 50 AND 200000)
 );
 CREATE INDEX [IX_KhuDuLich_Province] ON dbo.[KhuDuLich] ([ProvinceId],[IsPublished],[SortOrder]) INCLUDE ([Name],[Kind],[Slug]);
END;
IF OBJECT_ID(N'dbo.KhuDuLichDonVi',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[KhuDuLichDonVi] (
  [ZoneId] bigint NOT NULL,
  [UnitId] bigint NOT NULL,
  CONSTRAINT [PK_KhuDuLichDonVi] PRIMARY KEY CLUSTERED ([ZoneId],[UnitId]),
  CONSTRAINT [FK_KhuDuLichDonVi_Zone] FOREIGN KEY ([ZoneId]) REFERENCES dbo.[KhuDuLich]([Id]),
  CONSTRAINT [FK_KhuDuLichDonVi_Unit] FOREIGN KEY ([UnitId]) REFERENCES dbo.[DonViHanhChinh]([Id])
 );
 CREATE INDEX [IX_KhuDuLichDonVi_Unit] ON dbo.[KhuDuLichDonVi] ([UnitId]);
END;

-- 4. Hang cho: gia tri khu vuc cu khong tu gan duoc (khong khop, hoac khop nhieu don vi) de quan tri xu ly tay.
IF OBJECT_ID(N'dbo.ChuyenDoiDiaGioiCho',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[ChuyenDoiDiaGioiCho] (
  [Id] bigint IDENTITY(1,1) NOT NULL,
  [TableName] varchar(60) NOT NULL,
  [RecordId] bigint NOT NULL,
  [OldText] nvarchar(200) NOT NULL,
  [Reason] varchar(15) NOT NULL,              -- NoMatch, Ambiguous
  [Status] varchar(10) NOT NULL CONSTRAINT [DF_ChuyenDoiDiaGioiCho_Status] DEFAULT ('Pending'),
  [ResolvedUnitId] bigint NULL,
  [ResolvedBy] bigint NULL,
  [ResolvedAt] datetime2(0) NULL,
  [CreatedAt] datetime2(0) NOT NULL CONSTRAINT [DF_ChuyenDoiDiaGioiCho_Created] DEFAULT (SYSUTCDATETIME()),
  CONSTRAINT [PK_ChuyenDoiDiaGioiCho] PRIMARY KEY CLUSTERED ([Id]),
  CONSTRAINT [FK_ChuyenDoiDiaGioiCho_Unit] FOREIGN KEY ([ResolvedUnitId]) REFERENCES dbo.[DonViHanhChinh]([Id]),
  CONSTRAINT [UQ_ChuyenDoiDiaGioiCho_Record] UNIQUE ([TableName],[RecordId]),
  CONSTRAINT [CK_ChuyenDoiDiaGioiCho_Table] CHECK ([TableName] IN ('DiaDiemDuLich','DichVuDuLich','BanCapNhatDichVu','CoSoDichVu','BanCapNhatCoSo','MocDiaDiem','DiscoveryEntry')),
  CONSTRAINT [CK_ChuyenDoiDiaGioiCho_Reason] CHECK ([Reason] IN ('NoMatch','Ambiguous')),
  CONSTRAINT [CK_ChuyenDoiDiaGioiCho_Status] CHECK ([Status] IN ('Pending','Resolved','Rejected')),
  CONSTRAINT [CK_ChuyenDoiDiaGioiCho_Resolved] CHECK ([Status]<>'Resolved' OR [ResolvedUnitId] IS NOT NULL)
 );
 CREATE INDEX [IX_ChuyenDoiDiaGioiCho_Status] ON dbo.[ChuyenDoiDiaGioiCho] ([Status],[TableName]);
END;

-- 5. Them cot DonViId (NULL, giu cot Region cu de chuyen doi dan) vao cac bang dang co khu vuc dang chuoi.
DECLARE @targets TABLE ([T] sysname NOT NULL);
INSERT @targets VALUES (N'DiaDiemDuLich'),(N'DichVuDuLich'),(N'BanCapNhatDichVu'),(N'CoSoDichVu'),(N'BanCapNhatCoSo'),(N'MocDiaDiem'),(N'DiscoveryEntry');
DECLARE @t sysname, @sql nvarchar(max);
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT [T] FROM @targets;
OPEN c; FETCH NEXT FROM c INTO @t;
WHILE @@FETCH_STATUS=0
BEGIN
 IF OBJECT_ID(N'dbo.'+QUOTENAME(@t),N'U') IS NOT NULL
 BEGIN
  IF COL_LENGTH(N'dbo.'+@t,N'DonViId') IS NULL
  BEGIN
   SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@t)+N' ADD [DonViId] bigint NULL;';
   EXEC sys.sp_executesql @sql;
  END;
  IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_'+@t+N'_DonVi')
  BEGIN
   SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@t)+N' ADD CONSTRAINT '+QUOTENAME(N'FK_'+@t+N'_DonVi')+N' FOREIGN KEY ([DonViId]) REFERENCES dbo.[DonViHanhChinh]([Id]);';
   EXEC sys.sp_executesql @sql;
  END;
  IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_'+@t+N'_DonVi' AND object_id=OBJECT_ID(N'dbo.'+QUOTENAME(@t)))
  BEGIN
   SET @sql=N'CREATE INDEX '+QUOTENAME(N'IX_'+@t+N'_DonVi')+N' ON dbo.'+QUOTENAME(@t)+N' ([DonViId]);';
   EXEC sys.sp_executesql @sql;
  END;
 END;
 FETCH NEXT FROM c INTO @t;
END;
CLOSE c; DEALLOCATE c;
COMMIT TRANSACTION;
