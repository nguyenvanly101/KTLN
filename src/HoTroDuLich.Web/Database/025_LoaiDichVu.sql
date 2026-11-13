-- Danh muc LOAI DICH VU thong nhat: moi loai co nhom, co kha nang va bo truong rieng (khuon). Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.LoaiDichVu.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp loại dịch vụ.',1;

IF OBJECT_ID(N'dbo.LoaiDichVu',N'U') IS NULL
 EXEC sys.sp_executesql N'CREATE TABLE dbo.LoaiDichVu(
  Code varchar(30) NOT NULL CONSTRAINT PK_LoaiDichVu PRIMARY KEY,
  Label nvarchar(60) NOT NULL,
  GroupCode varchar(12) NOT NULL,
  SortOrder int NOT NULL CONSTRAINT DF_LoaiDV_Sort DEFAULT 100,
  Icon varchar(20) NULL,
  Bookable bit NOT NULL CONSTRAINT DF_LoaiDV_Bookable DEFAULT 0,
  ProviderPostable bit NOT NULL CONSTRAINT DF_LoaiDV_Post DEFAULT 1,
  ForBranch bit NOT NULL CONSTRAINT DF_LoaiDV_Branch DEFAULT 0,
  HasPrice bit NOT NULL CONSTRAINT DF_LoaiDV_Price DEFAULT 1,
  HasHours bit NOT NULL CONSTRAINT DF_LoaiDV_Hours DEFAULT 0,
  HasLocation bit NOT NULL CONSTRAINT DF_LoaiDV_Loc DEFAULT 1,
  TimeBound bit NOT NULL CONSTRAINT DF_LoaiDV_Time DEFAULT 0,
  Active bit NOT NULL CONSTRAINT DF_LoaiDV_Active DEFAULT 1,
  Builtin bit NOT NULL CONSTRAINT DF_LoaiDV_Builtin DEFAULT 0,
  FieldsJson nvarchar(max) NULL,
  UpdatedAt datetime2(0) NOT NULL CONSTRAINT DF_LoaiDV_Updated DEFAULT SYSUTCDATETIME(),
  CONSTRAINT CK_LoaiDV_Code CHECK (Code NOT LIKE ''%[^A-Za-z0-9]%''),
  CONSTRAINT CK_LoaiDV_Group CHECK (GroupCode IN (''Place'',''Eat'',''Stay'',''Move'',''Experience'',''Info'',''Local'',''Other'')),
  CONSTRAINT CK_LoaiDV_Fields CHECK (FieldsJson IS NULL OR (ISJSON(FieldsJson)=1 AND LEN(FieldsJson)<=20000)));';

-- Cac loai co san (ma cu giu nguyen de khong anh huong du lieu dang co).
DECLARE @seed TABLE(Code varchar(20),Label nvarchar(60),G varchar(12),S int,Bk bit,Po bit,Br bit,Pr bit,Hr bit,Lo bit,Tb bit);
INSERT @seed VALUES
 ('HoatDong',N'Tham quan & hoạt động','Place',10,1,1,1,1,1,1,0),
 ('AnUong',N'Ăn uống','Eat',20,1,1,1,1,1,1,0),
 ('LuuTru',N'Lưu trú','Stay',30,1,1,1,1,1,1,0),
 ('PhuongTien',N'Di chuyển','Move',40,1,1,1,1,1,1,0),
 ('Tour',N'Tour trọn gói','Experience',50,1,1,1,1,1,1,0),
 ('GiaiTri',N'Vui chơi giải trí','Experience',60,1,1,1,1,1,1,0),
 ('SucKhoe',N'Spa & sức khỏe','Experience',70,1,1,1,1,1,1,0),
 ('SuKien',N'Sự kiện','Info',80,0,1,0,1,0,1,1),
 ('CamNang',N'Cẩm nang','Info',90,0,0,0,0,0,0,0),
 ('HuongDanVien',N'Hướng dẫn viên','Local',100,1,1,0,1,0,0,0),
 ('MuaSam',N'Mua sắm & đặc sản','Local',110,0,1,1,1,1,1,0),
 ('DichVuHoTro',N'Dịch vụ hỗ trợ','Local',120,1,1,1,1,1,1,0),
 ('Khac',N'Khác','Other',999,1,1,1,1,1,1,0);
INSERT dbo.LoaiDichVu(Code,Label,GroupCode,SortOrder,Bookable,ProviderPostable,ForBranch,HasPrice,HasHours,HasLocation,TimeBound,Builtin)
 SELECT s.Code,s.Label,s.G,s.S,s.Bk,s.Po,s.Br,s.Pr,s.Hr,s.Lo,s.Tb,1 FROM @seed s WHERE NOT EXISTS (SELECT 1 FROM dbo.LoaiDichVu x WHERE x.Code=s.Code);

-- Don dep: dich vu tung luu nhan tieng Viet thay vi ma -> thong nhat ve ma.
IF OBJECT_ID(N'dbo.DichVuDuLich',N'U') IS NOT NULL
BEGIN
 UPDATE dbo.DichVuDuLich SET LoaiDichVu=CASE LoaiDichVu WHEN N'Lưu trú' THEN 'LuuTru' WHEN N'Ăn uống' THEN 'AnUong' WHEN N'Phương tiện' THEN 'PhuongTien' WHEN N'Di chuyển' THEN 'PhuongTien' WHEN N'Hoạt động' THEN 'HoatDong' WHEN N'Tham quan' THEN 'HoatDong' WHEN N'Khác' THEN 'Khac' ELSE LoaiDichVu END WHERE LoaiDichVu IN (N'Lưu trú',N'Ăn uống',N'Phương tiện',N'Di chuyển',N'Hoạt động',N'Tham quan',N'Khác');
 IF COL_LENGTH(N'dbo.DichVuDuLich',N'AttributesJson') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.DichVuDuLich ADD [AttributesJson] nvarchar(max) NULL;';
END;
IF OBJECT_ID(N'dbo.BanCapNhatDichVu',N'U') IS NOT NULL
BEGIN
 UPDATE dbo.BanCapNhatDichVu SET LoaiDichVu=CASE LoaiDichVu WHEN N'Lưu trú' THEN 'LuuTru' WHEN N'Ăn uống' THEN 'AnUong' WHEN N'Phương tiện' THEN 'PhuongTien' WHEN N'Di chuyển' THEN 'PhuongTien' WHEN N'Hoạt động' THEN 'HoatDong' WHEN N'Tham quan' THEN 'HoatDong' WHEN N'Khác' THEN 'Khac' ELSE LoaiDichVu END WHERE LoaiDichVu IN (N'Lưu trú',N'Ăn uống',N'Phương tiện',N'Di chuyển',N'Hoạt động',N'Tham quan',N'Khác');
 IF COL_LENGTH(N'dbo.BanCapNhatDichVu',N'AttributesJson') IS NULL EXEC sys.sp_executesql N'ALTER TABLE dbo.BanCapNhatDichVu ADD [AttributesJson] nvarchar(max) NULL;';
END;

-- Bo danh sach loai khoa cung (CHECK) cua co so, thay bang khoa ngoai toi danh muc de them loai moi khong can sua co so du lieu.
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_CoSoDichVu_Type' AND parent_object_id=OBJECT_ID(N'dbo.CoSoDichVu'))
 EXEC sys.sp_executesql N'ALTER TABLE dbo.CoSoDichVu DROP CONSTRAINT CK_CoSoDichVu_Type;';
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_BanCapNhatCoSo_Type' AND parent_object_id=OBJECT_ID(N'dbo.BanCapNhatCoSo'))
 EXEC sys.sp_executesql N'ALTER TABLE dbo.BanCapNhatCoSo DROP CONSTRAINT CK_BanCapNhatCoSo_Type;';
IF OBJECT_ID(N'dbo.CoSoDichVu',N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_CoSoDichVu_LoaiDV')
 EXEC sys.sp_executesql N'ALTER TABLE dbo.CoSoDichVu WITH CHECK ADD CONSTRAINT FK_CoSoDichVu_LoaiDV FOREIGN KEY ([Type]) REFERENCES dbo.LoaiDichVu(Code);';
IF OBJECT_ID(N'dbo.BanCapNhatCoSo',N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_BanCapNhatCoSo_LoaiDV')
 EXEC sys.sp_executesql N'ALTER TABLE dbo.BanCapNhatCoSo WITH CHECK ADD CONSTRAINT FK_BanCapNhatCoSo_LoaiDV FOREIGN KEY ([Type]) REFERENCES dbo.LoaiDichVu(Code);';
-- Thong tin kham pha (cam nang, su kien, di chuyen, tham quan...) dung chung khuon thuoc tinh theo loai.
IF OBJECT_ID(N'dbo.DiscoveryEntry',N'U') IS NOT NULL AND COL_LENGTH(N'dbo.DiscoveryEntry',N'AttributesJson') IS NULL
 EXEC sys.sp_executesql N'ALTER TABLE dbo.DiscoveryEntry ADD [AttributesJson] nvarchar(max) NULL;';
COMMIT TRANSACTION;
