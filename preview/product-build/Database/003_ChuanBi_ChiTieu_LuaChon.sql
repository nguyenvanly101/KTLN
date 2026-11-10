-- Approved product additions. Leaves existing FullText rows and identifiers intact.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lockResult int;
EXEC @lockResult=sys.sp_getapplock @Resource=N'HoTroDuLich.Product.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lockResult<0 THROW 51000,N'Không lấy được khóa cập nhật chức năng chuyến đi.',1;
IF OBJECT_ID(N'dbo.LuaChonDaLuu',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.LuaChonDaLuu (
  MaLuaChon bigint IDENTITY PRIMARY KEY, MaTaiKhoan bigint NOT NULL, MaDichVu bigint NULL, MaDiaDiem bigint NULL,
  TenMuc nvarchar(200) NOT NULL, ThoiDiemLuu datetime2(0) NOT NULL,
  CONSTRAINT CK_LuaChonDaLuu_MotMuc CHECK ((MaDichVu IS NOT NULL AND MaDiaDiem IS NULL) OR (MaDichVu IS NULL AND MaDiaDiem IS NOT NULL)),
  CONSTRAINT FK_LuaChonDaLuu_MaTaiKhoan FOREIGN KEY(MaTaiKhoan) REFERENCES dbo.TaiKhoan(MaTaiKhoan),
  CONSTRAINT FK_LuaChonDaLuu_MaDichVu FOREIGN KEY(MaDichVu) REFERENCES dbo.DichVuDuLich(MaDichVu),
  CONSTRAINT FK_LuaChonDaLuu_MaDiaDiem FOREIGN KEY(MaDiaDiem) REFERENCES dbo.DiaDiemDuLich(MaDiaDiem));
 CREATE UNIQUE INDEX UX_LuaChon_DichVu ON dbo.LuaChonDaLuu(MaTaiKhoan,MaDichVu) WHERE MaDichVu IS NOT NULL;
 CREATE UNIQUE INDEX UX_LuaChon_DiaDiem ON dbo.LuaChonDaLuu(MaTaiKhoan,MaDiaDiem) WHERE MaDiaDiem IS NOT NULL;
 CREATE INDEX IX_LuaChon_TaiKhoan ON dbo.LuaChonDaLuu(MaTaiKhoan,ThoiDiemLuu DESC);
END;
IF OBJECT_ID(N'dbo.MucChuanBi',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.MucChuanBi (
  MaMuc bigint IDENTITY PRIMARY KEY, MaKeHoach bigint NOT NULL, NoiDung nvarchar(200) NOT NULL, Nhom varchar(40) NOT NULL,
  HanHoanThanh date NULL, DaHoanThanh bit NOT NULL, NhacHan bit NOT NULL, NgayDaNhac date NULL, ThoiDiemTao datetime2(0) NOT NULL, RowVersion rowversion NOT NULL,
  CONSTRAINT CK_MucChuanBi_Nhom CHECK (Nhom IN ('GiayTo','HanhLy','DiChuyen','DatDichVu','Khac')),
  CONSTRAINT FK_MucChuanBi_MaKeHoach FOREIGN KEY(MaKeHoach) REFERENCES dbo.KeHoachChuyenDi(MaKeHoach));
 CREATE UNIQUE INDEX UX_ChuanBi_NoiDung ON dbo.MucChuanBi(MaKeHoach,NoiDung);
 CREATE INDEX IX_ChuanBi_KeHoach ON dbo.MucChuanBi(MaKeHoach,HanHoanThanh);
END;
IF OBJECT_ID(N'dbo.KhoanChiChuyenDi',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.KhoanChiChuyenDi (
  MaKhoanChi bigint IDENTITY PRIMARY KEY, MaKeHoach bigint NOT NULL, NoiDung nvarchar(200) NOT NULL, LoaiChi varchar(40) NOT NULL,
  SoTien decimal(18,2) NOT NULL, NgayChi date NOT NULL, GhiChu nvarchar(500) NULL, MaLuotGhi uniqueidentifier NOT NULL, ThoiDiemTao datetime2(0) NOT NULL, RowVersion rowversion NOT NULL,
  CONSTRAINT CK_KhoanChi_SoTien CHECK (SoTien>0 AND SoTien<=1000000000),
  CONSTRAINT CK_KhoanChi_Loai CHECK (LoaiChi IN ('LuuTru','AnUong','DiChuyen','HoatDong','MuaSam','Khac')),
  CONSTRAINT FK_KhoanChiChuyenDi_MaKeHoach FOREIGN KEY(MaKeHoach) REFERENCES dbo.KeHoachChuyenDi(MaKeHoach));
 CREATE UNIQUE INDEX UX_KhoanChi_LuotGhi ON dbo.KhoanChiChuyenDi(MaKeHoach,MaLuotGhi);
 CREATE INDEX IX_KhoanChi_KeHoach ON dbo.KhoanChiChuyenDi(MaKeHoach,NgayChi DESC);
END;
IF COL_LENGTH(N'dbo.DichVuDuLich',N'DiaChiPhucVu') IS NULL
 EXEC(N'ALTER TABLE dbo.DichVuDuLich ADD DiaChiPhucVu nvarchar(300) NULL, DonViGia nvarchar(50) NULL, ChinhSachHuy nvarchar(2000) NULL, AnhJson nvarchar(2000) NULL;');
IF COL_LENGTH(N'dbo.BanCapNhatDichVu',N'DiaChiPhucVu') IS NULL
 EXEC(N'ALTER TABLE dbo.BanCapNhatDichVu ADD DiaChiPhucVu nvarchar(300) NULL, DonViGia nvarchar(50) NULL, ChinhSachHuy nvarchar(2000) NULL, AnhJson nvarchar(2000) NULL;');
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_DichVu_AnhJson')
 EXEC(N'ALTER TABLE dbo.DichVuDuLich WITH CHECK ADD CONSTRAINT CK_DichVu_AnhJson CHECK (AnhJson IS NULL OR ISJSON(AnhJson)=1);');
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_BanCapNhat_AnhJson')
 EXEC(N'ALTER TABLE dbo.BanCapNhatDichVu WITH CHECK ADD CONSTRAINT CK_BanCapNhat_AnhJson CHECK (AnhJson IS NULL OR ISJSON(AnhJson)=1);');
COMMIT TRANSACTION;
