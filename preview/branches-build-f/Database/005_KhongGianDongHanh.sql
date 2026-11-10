-- Approved companion extension 005. New tables only; original rows remain unchanged.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lockResult int;
EXEC @lockResult=sys.sp_getapplock @Resource=N'HoTroDuLich.Companion.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lockResult<0 THROW 51000,N'Không lấy được khóa nâng cấp đồng hành.',1;
IF OBJECT_ID(N'dbo.HoSoDongHanh',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[HoSoDongHanh] (
  [MaTaiKhoan] bigint NOT NULL,
  [PhongCach] varchar(30) NOT NULL,
  [NhipDo] varchar(30) NOT NULL,
  [NganSachToiDa] decimal(18,2) NULL,
  [GioiThieu] nvarchar(500) NULL,
  [HienThi] bit NOT NULL,
  [ThoiDiemCapNhat] datetime2(0) NOT NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_HoSoDongHanh] PRIMARY KEY ([MaTaiKhoan]),
  CONSTRAINT [CK_HSDH_PhongCach] CHECK (PhongCach IN ('KhamPha','NghiDuong','AmThuc','ThienNhien','VanHoa','Khac')),
  CONSTRAINT [CK_HSDH_NhipDo] CHECK (NhipDo IN ('ThuThai','CanBang','NangDong')),
  CONSTRAINT [CK_HSDH_NganSach] CHECK (NganSachToiDa IS NULL OR NganSachToiDa BETWEEN 0 AND 1000000000),
  CONSTRAINT [FK_HoSoDongHanh_MaTaiKhoan] FOREIGN KEY (MaTaiKhoan) REFERENCES dbo.[TaiKhoan] (MaTaiKhoan)
 );
END;
IF OBJECT_ID(N'dbo.NhomDongHanh',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[NhomDongHanh] (
  [MaNhom] bigint IDENTITY(1,1) NOT NULL,
  [MaChuNhom] bigint NOT NULL,
  [MaKeHoach] bigint NULL,
  [MaDeNghi] bigint NULL,
  [TenNhom] nvarchar(150) NOT NULL,
  [MoTa] nvarchar(2000) NOT NULL,
  [KhuVuc] nvarchar(100) NOT NULL,
  [Loai] varchar(30) NOT NULL,
  [NgayBatDau] date NOT NULL,
  [NgayKetThuc] date NOT NULL,
  [GioHen] time(0) NULL,
  [DiemHen] nvarchar(300) NULL,
  [SoNguoiToiDa] int NOT NULL,
  [NganSachThamKhao] decimal(18,2) NULL,
  [PhongCach] varchar(30) NOT NULL,
  [NhipDo] varchar(30) NOT NULL,
  [TrangThai] varchar(30) NOT NULL,
  [AnBoiQuanTri] bit NOT NULL,
  [LyDoAn] nvarchar(500) NULL,
  [PhienDiemHen] int NOT NULL,
  [ThoiDiemTao] datetime2(0) NOT NULL,
  [ThoiDiemCapNhat] datetime2(0) NOT NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_NhomDongHanh] PRIMARY KEY ([MaNhom]),
  CONSTRAINT [CK_NhomDH_Ngay] CHECK (NgayKetThuc>=NgayBatDau),
  CONSTRAINT [CK_NhomDH_Loai] CHECK (Loai IN ('ChuyenDi','HoatDong')),
  CONSTRAINT [CK_NhomDH_SoNguoi] CHECK (SoNguoiToiDa BETWEEN 2 AND 30),
  CONSTRAINT [CK_NhomDH_NganSach] CHECK (NganSachThamKhao IS NULL OR NganSachThamKhao BETWEEN 0 AND 1000000000),
  CONSTRAINT [CK_NhomDH_PhongCach] CHECK (PhongCach IN ('KhamPha','NghiDuong','AmThuc','ThienNhien','VanHoa','Khac')),
  CONSTRAINT [CK_NhomDH_NhipDo] CHECK (NhipDo IN ('ThuThai','CanBang','NangDong')),
  CONSTRAINT [CK_NhomDH_TrangThai] CHECK (TrangThai IN ('Nhap','Mo','DaChot','DaKetThuc','DaHuy')),
  CONSTRAINT [CK_NhomDH_Phien] CHECK (PhienDiemHen>=1),
  CONSTRAINT [CK_NhomDH_An] CHECK (AnBoiQuanTri=0 OR LyDoAn IS NOT NULL),
  CONSTRAINT [FK_NhomDongHanh_MaChuNhom] FOREIGN KEY (MaChuNhom) REFERENCES dbo.[TaiKhoan] (MaTaiKhoan),
  CONSTRAINT [FK_NhomDongHanh_MaKeHoach] FOREIGN KEY (MaKeHoach) REFERENCES dbo.[KeHoachChuyenDi] (MaKeHoach),
  CONSTRAINT [FK_NhomDongHanh_MaDeNghi] FOREIGN KEY (MaDeNghi) REFERENCES dbo.[DeNghiKetNoiDongHanh] (MaDeNghi)
 );
 CREATE INDEX [IX_NhomDH_TimKiem] ON dbo.[NhomDongHanh] (TrangThai,NgayBatDau);
 CREATE INDEX [IX_NhomDH_Chu] ON dbo.[NhomDongHanh] (MaChuNhom,ThoiDiemCapNhat);
 CREATE UNIQUE INDEX [UX_NhomDH_KetNoi] ON dbo.[NhomDongHanh] (MaDeNghi) WHERE MaDeNghi IS NOT NULL;
END;
IF OBJECT_ID(N'dbo.ThanhVienDongHanh',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[ThanhVienDongHanh] (
  [MaNhom] bigint NOT NULL,
  [MaTaiKhoan] bigint NOT NULL,
  [TrangThai] varchar(30) NOT NULL,
  [LoiNhan] nvarchar(500) NULL,
  [PhanHoi] nvarchar(500) NULL,
  [ThamGiaLuc] datetime2(0) NULL,
  [DiemDanh] varchar(30) NOT NULL,
  [PhienDiemHen] int NOT NULL,
  [ThoiDiemCapNhat] datetime2(0) NOT NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_ThanhVienDongHanh] PRIMARY KEY ([MaNhom],[MaTaiKhoan]),
  CONSTRAINT [CK_TVDH_TrangThai] CHECK (TrangThai IN ('ChoDuyet','DuocMoi','DaThamGia','TuChoi','DaRut','DaXoa')),
  CONSTRAINT [CK_TVDH_DiemDanh] CHECK (DiemDanh IN ('ChuaXacNhan','SeDen','DaDen','DenMuon')),
  CONSTRAINT [CK_TVDH_Phien] CHECK (PhienDiemHen>=0),
  CONSTRAINT [FK_ThanhVienDongHanh_MaNhom] FOREIGN KEY (MaNhom) REFERENCES dbo.[NhomDongHanh] (MaNhom),
  CONSTRAINT [FK_ThanhVienDongHanh_MaTaiKhoan] FOREIGN KEY (MaTaiKhoan) REFERENCES dbo.[TaiKhoan] (MaTaiKhoan)
 );
 CREATE INDEX [IX_TVDH_CaNhan] ON dbo.[ThanhVienDongHanh] (MaTaiKhoan,TrangThai);
END;
IF OBJECT_ID(N'dbo.TinNhanDongHanh',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[TinNhanDongHanh] (
  [MaTinNhan] bigint IDENTITY(1,1) NOT NULL,
  [MaNhom] bigint NOT NULL,
  [MaNguoiGui] bigint NOT NULL,
  [NoiDung] nvarchar(2000) NOT NULL,
  [LaThongBao] bit NOT NULL,
  [DaAn] bit NOT NULL,
  [MaNguoiAn] bigint NULL,
  [SubmissionId] uniqueidentifier NOT NULL,
  [ThoiDiemTao] datetime2(0) NOT NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_TinNhanDongHanh] PRIMARY KEY ([MaTinNhan]),
  CONSTRAINT [CK_TNDH_NoiDung] CHECK (LEN(LTRIM(RTRIM(NoiDung)))>0),
  CONSTRAINT [FK_TinNhanDongHanh_MaNhom] FOREIGN KEY (MaNhom) REFERENCES dbo.[NhomDongHanh] (MaNhom),
  CONSTRAINT [FK_TinNhanDongHanh_MaNguoiGui] FOREIGN KEY (MaNguoiGui) REFERENCES dbo.[TaiKhoan] (MaTaiKhoan),
  CONSTRAINT [FK_TinNhanDongHanh_MaNguoiAn] FOREIGN KEY (MaNguoiAn) REFERENCES dbo.[TaiKhoan] (MaTaiKhoan)
 );
 CREATE INDEX [IX_TNDH_Nhom] ON dbo.[TinNhanDongHanh] (MaNhom,MaTinNhan);
 CREATE UNIQUE INDEX [UX_TNDH_Gui] ON dbo.[TinNhanDongHanh] (MaNhom,MaNguoiGui,SubmissionId);
END;
IF OBJECT_ID(N'dbo.XacNhanThongBaoNhom',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[XacNhanThongBaoNhom] (
  [MaTinNhan] bigint NOT NULL,
  [MaTaiKhoan] bigint NOT NULL,
  [ThoiDiemDoc] datetime2(0) NOT NULL,
  CONSTRAINT [PK_XacNhanThongBaoNhom] PRIMARY KEY ([MaTinNhan],[MaTaiKhoan]),
  CONSTRAINT [FK_XacNhanThongBaoNhom_MaTinNhan] FOREIGN KEY (MaTinNhan) REFERENCES dbo.[TinNhanDongHanh] (MaTinNhan),
  CONSTRAINT [FK_XacNhanThongBaoNhom_MaTaiKhoan] FOREIGN KEY (MaTaiKhoan) REFERENCES dbo.[TaiKhoan] (MaTaiKhoan)
 );
END;
IF OBJECT_ID(N'dbo.BinhChonDongHanh',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[BinhChonDongHanh] (
  [MaBinhChon] bigint IDENTITY(1,1) NOT NULL,
  [MaNhom] bigint NOT NULL,
  [MaNguoiTao] bigint NOT NULL,
  [TieuDe] nvarchar(200) NOT NULL,
  [HanChot] datetime2(0) NOT NULL,
  [TrangThai] varchar(30) NOT NULL,
  [LuaChonChot] int NULL,
  [ThoiDiemTao] datetime2(0) NOT NULL,
  [ThoiDiemCapNhat] datetime2(0) NOT NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_BinhChonDongHanh] PRIMARY KEY ([MaBinhChon]),
  CONSTRAINT [CK_BCDH_TrangThai] CHECK (TrangThai IN ('Mo','DaChot','DaHuy')),
  CONSTRAINT [CK_BCDH_KetQua] CHECK ((TrangThai='DaChot' AND LuaChonChot IS NOT NULL AND LuaChonChot BETWEEN 1 AND 5) OR (TrangThai<>'DaChot' AND LuaChonChot IS NULL)),
  CONSTRAINT [CK_BCDH_Han] CHECK (HanChot>ThoiDiemTao),
  CONSTRAINT [FK_BinhChonDongHanh_MaNhom] FOREIGN KEY (MaNhom) REFERENCES dbo.[NhomDongHanh] (MaNhom),
  CONSTRAINT [FK_BinhChonDongHanh_MaNguoiTao] FOREIGN KEY (MaNguoiTao) REFERENCES dbo.[TaiKhoan] (MaTaiKhoan)
 );
 CREATE INDEX [IX_BCDH_Nhom] ON dbo.[BinhChonDongHanh] (MaNhom,ThoiDiemTao);
END;
IF OBJECT_ID(N'dbo.LuaChonBinhChon',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[LuaChonBinhChon] (
  [MaBinhChon] bigint NOT NULL,
  [ThuTu] int NOT NULL,
  [NoiDung] nvarchar(200) NOT NULL,
  CONSTRAINT [PK_LuaChonBinhChon] PRIMARY KEY ([MaBinhChon],[ThuTu]),
  CONSTRAINT [CK_LCBDH_ThuTu] CHECK (ThuTu BETWEEN 1 AND 5),
  CONSTRAINT [FK_LuaChonBinhChon_MaBinhChon] FOREIGN KEY (MaBinhChon) REFERENCES dbo.[BinhChonDongHanh] (MaBinhChon)
 );
END;
IF OBJECT_ID(N'dbo.PhieuBinhChon',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[PhieuBinhChon] (
  [MaBinhChon] bigint NOT NULL,
  [MaTaiKhoan] bigint NOT NULL,
  [ThuTu] int NOT NULL,
  [ThoiDiemCapNhat] datetime2(0) NOT NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_PhieuBinhChon] PRIMARY KEY ([MaBinhChon],[MaTaiKhoan]),
  CONSTRAINT [FK_PhieuBinhChon_MaBinhChon_ThuTu] FOREIGN KEY (MaBinhChon,ThuTu) REFERENCES dbo.[LuaChonBinhChon] (MaBinhChon,ThuTu),
  CONSTRAINT [FK_PhieuBinhChon_MaTaiKhoan] FOREIGN KEY (MaTaiKhoan) REFERENCES dbo.[TaiKhoan] (MaTaiKhoan)
 );
END;
IF OBJECT_ID(N'dbo.HoatDongNhom',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[HoatDongNhom] (
  [MaHoatDong] bigint IDENTITY(1,1) NOT NULL,
  [MaNhom] bigint NOT NULL,
  [MaNguoiTao] bigint NOT NULL,
  [MaBinhChon] bigint NULL,
  [NoiDung] nvarchar(200) NOT NULL,
  [Ngay] date NOT NULL,
  [Gio] time(0) NULL,
  [DaHuy] bit NOT NULL,
  [ThoiDiemCapNhat] datetime2(0) NOT NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_HoatDongNhom] PRIMARY KEY ([MaHoatDong]),
  CONSTRAINT [FK_HoatDongNhom_MaNhom] FOREIGN KEY (MaNhom) REFERENCES dbo.[NhomDongHanh] (MaNhom),
  CONSTRAINT [FK_HoatDongNhom_MaNguoiTao] FOREIGN KEY (MaNguoiTao) REFERENCES dbo.[TaiKhoan] (MaTaiKhoan),
  CONSTRAINT [FK_HoatDongNhom_MaBinhChon] FOREIGN KEY (MaBinhChon) REFERENCES dbo.[BinhChonDongHanh] (MaBinhChon)
 );
 CREATE INDEX [IX_HDNhom_Ngay] ON dbo.[HoatDongNhom] (MaNhom,Ngay);
 CREATE UNIQUE INDEX [UX_HDNhom_BinhChon] ON dbo.[HoatDongNhom] (MaBinhChon) WHERE MaBinhChon IS NOT NULL;
END;
IF OBJECT_ID(N'dbo.ViecNhom',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[ViecNhom] (
  [MaViec] bigint IDENTITY(1,1) NOT NULL,
  [MaNhom] bigint NOT NULL,
  [MaNguoiTao] bigint NOT NULL,
  [MaPhuTrach] bigint NULL,
  [NoiDung] nvarchar(200) NOT NULL,
  [Han] date NULL,
  [DaXong] bit NOT NULL,
  [DaHuy] bit NOT NULL,
  [ThoiDiemCapNhat] datetime2(0) NOT NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_ViecNhom] PRIMARY KEY ([MaViec]),
  CONSTRAINT [FK_ViecNhom_MaNhom] FOREIGN KEY (MaNhom) REFERENCES dbo.[NhomDongHanh] (MaNhom),
  CONSTRAINT [FK_ViecNhom_MaNguoiTao] FOREIGN KEY (MaNguoiTao) REFERENCES dbo.[TaiKhoan] (MaTaiKhoan),
  CONSTRAINT [FK_ViecNhom_MaPhuTrach] FOREIGN KEY (MaPhuTrach) REFERENCES dbo.[TaiKhoan] (MaTaiKhoan)
 );
 CREATE INDEX [IX_ViecNhom_Nhom] ON dbo.[ViecNhom] (MaNhom,Han);
END;
IF OBJECT_ID(N'dbo.KhoanChiNhom',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[KhoanChiNhom] (
  [MaKhoanChi] bigint IDENTITY(1,1) NOT NULL,
  [MaNhom] bigint NOT NULL,
  [MaNguoiTao] bigint NOT NULL,
  [MaNguoiTra] bigint NOT NULL,
  [NoiDung] nvarchar(200) NOT NULL,
  [SoTien] decimal(18,2) NOT NULL,
  [NgayChi] date NOT NULL,
  [ChiaDeu] bit NOT NULL,
  [DaHuy] bit NOT NULL,
  [SubmissionId] uniqueidentifier NOT NULL,
  [ThoiDiemTao] datetime2(0) NOT NULL,
  [ThoiDiemCapNhat] datetime2(0) NOT NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_KhoanChiNhom] PRIMARY KEY ([MaKhoanChi]),
  CONSTRAINT [CK_ChiNhom_Tien] CHECK (SoTien>0 AND SoTien<=1000000000),
  CONSTRAINT [FK_KhoanChiNhom_MaNhom] FOREIGN KEY (MaNhom) REFERENCES dbo.[NhomDongHanh] (MaNhom),
  CONSTRAINT [FK_KhoanChiNhom_MaNguoiTao] FOREIGN KEY (MaNguoiTao) REFERENCES dbo.[TaiKhoan] (MaTaiKhoan),
  CONSTRAINT [FK_KhoanChiNhom_MaNguoiTra] FOREIGN KEY (MaNguoiTra) REFERENCES dbo.[TaiKhoan] (MaTaiKhoan)
 );
 CREATE INDEX [IX_ChiNhom_Nhom] ON dbo.[KhoanChiNhom] (MaNhom,NgayChi);
 CREATE UNIQUE INDEX [UX_ChiNhom_Gui] ON dbo.[KhoanChiNhom] (MaNhom,MaNguoiTao,SubmissionId);
END;
IF OBJECT_ID(N'dbo.PhanBoChiNhom',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[PhanBoChiNhom] (
  [MaKhoanChi] bigint NOT NULL,
  [MaTaiKhoan] bigint NOT NULL,
  [SoTien] decimal(18,2) NOT NULL,
  CONSTRAINT [PK_PhanBoChiNhom] PRIMARY KEY ([MaKhoanChi],[MaTaiKhoan]),
  CONSTRAINT [CK_PhanBoNhom_Tien] CHECK (SoTien>0 AND SoTien<=1000000000),
  CONSTRAINT [FK_PhanBoChiNhom_MaKhoanChi] FOREIGN KEY (MaKhoanChi) REFERENCES dbo.[KhoanChiNhom] (MaKhoanChi),
  CONSTRAINT [FK_PhanBoChiNhom_MaTaiKhoan] FOREIGN KEY (MaTaiKhoan) REFERENCES dbo.[TaiKhoan] (MaTaiKhoan)
 );
END;
IF OBJECT_ID(N'dbo.HoanTraNhom',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[HoanTraNhom] (
  [MaHoanTra] bigint IDENTITY(1,1) NOT NULL,
  [MaNhom] bigint NOT NULL,
  [MaNguoiGui] bigint NOT NULL,
  [MaNguoiNhan] bigint NOT NULL,
  [SoTien] decimal(18,2) NOT NULL,
  [TrangThai] varchar(30) NOT NULL,
  [GhiChu] nvarchar(300) NULL,
  [SubmissionId] uniqueidentifier NOT NULL,
  [ThoiDiemTao] datetime2(0) NOT NULL,
  [ThoiDiemCapNhat] datetime2(0) NOT NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_HoanTraNhom] PRIMARY KEY ([MaHoanTra]),
  CONSTRAINT [CK_HoanTraNhom_Tien] CHECK (SoTien>0 AND SoTien<=1000000000),
  CONSTRAINT [CK_HoanTraNhom_Nguoi] CHECK (MaNguoiGui<>MaNguoiNhan),
  CONSTRAINT [CK_HoanTraNhom_TT] CHECK (TrangThai IN ('ChoXacNhan','DaXacNhan','DaHuy','TuChoi')),
  CONSTRAINT [FK_HoanTraNhom_MaNhom] FOREIGN KEY (MaNhom) REFERENCES dbo.[NhomDongHanh] (MaNhom),
  CONSTRAINT [FK_HoanTraNhom_MaNguoiGui] FOREIGN KEY (MaNguoiGui) REFERENCES dbo.[TaiKhoan] (MaTaiKhoan),
  CONSTRAINT [FK_HoanTraNhom_MaNguoiNhan] FOREIGN KEY (MaNguoiNhan) REFERENCES dbo.[TaiKhoan] (MaTaiKhoan)
 );
 CREATE INDEX [IX_HoanTraNhom_Nhom] ON dbo.[HoanTraNhom] (MaNhom,ThoiDiemTao);
 CREATE UNIQUE INDEX [UX_HoanTraNhom_Gui] ON dbo.[HoanTraNhom] (MaNhom,MaNguoiGui,SubmissionId);
 CREATE UNIQUE INDEX [UX_HoanTraNhom_Cho] ON dbo.[HoanTraNhom] (MaNhom,MaNguoiGui,MaNguoiNhan) WHERE TrangThai='ChoXacNhan';
END;
IF OBJECT_ID(N'dbo.ChanDongHanh',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[ChanDongHanh] (
  [MaNguoiChan] bigint NOT NULL,
  [MaBiChan] bigint NOT NULL,
  [DaChan] bit NOT NULL,
  [ThoiDiemCapNhat] datetime2(0) NOT NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_ChanDongHanh] PRIMARY KEY ([MaNguoiChan],[MaBiChan]),
  CONSTRAINT [CK_ChanDH_Nguoi] CHECK (MaNguoiChan<>MaBiChan),
  CONSTRAINT [FK_ChanDongHanh_MaNguoiChan] FOREIGN KEY (MaNguoiChan) REFERENCES dbo.[TaiKhoan] (MaTaiKhoan),
  CONSTRAINT [FK_ChanDongHanh_MaBiChan] FOREIGN KEY (MaBiChan) REFERENCES dbo.[TaiKhoan] (MaTaiKhoan)
 );
END;
IF OBJECT_ID(N'dbo.BaoCaoDongHanh',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.[BaoCaoDongHanh] (
  [MaBaoCao] bigint IDENTITY(1,1) NOT NULL,
  [MaNguoiGui] bigint NOT NULL,
  [MaNhom] bigint NOT NULL,
  [MaTinNhan] bigint NULL,
  [Loai] varchar(30) NOT NULL,
  [MoTa] nvarchar(2000) NOT NULL,
  [BangChung] nvarchar(2000) NOT NULL,
  [TrangThai] varchar(30) NOT NULL,
  [QuyetDinh] varchar(30) NULL,
  [LyDo] nvarchar(500) NULL,
  [MaAdmin] bigint NULL,
  [ThoiDiemTao] datetime2(0) NOT NULL,
  [ThoiDiemCapNhat] datetime2(0) NOT NULL,
  [RowVersion] rowversion NOT NULL,
  CONSTRAINT [PK_BaoCaoDongHanh] PRIMARY KEY ([MaBaoCao]),
  CONSTRAINT [CK_BaoCaoDH_Loai] CHECK (Loai IN ('Spam','QuayRoi','LuaDao','Khac')),
  CONSTRAINT [CK_BaoCaoDH_TT] CHECK (TrangThai IN ('ChoXuLy','DaXuLy')),
  CONSTRAINT [CK_BaoCaoDH_QuyetDinh] CHECK ((TrangThai='ChoXuLy' AND QuyetDinh IS NULL AND MaAdmin IS NULL) OR (TrangThai='DaXuLy' AND QuyetDinh IN ('KhongViPham','AnNoiDung','AnNhom','DaNhacNho') AND MaAdmin IS NOT NULL AND LyDo IS NOT NULL)),
  CONSTRAINT [FK_BaoCaoDongHanh_MaNguoiGui] FOREIGN KEY (MaNguoiGui) REFERENCES dbo.[TaiKhoan] (MaTaiKhoan),
  CONSTRAINT [FK_BaoCaoDongHanh_MaNhom] FOREIGN KEY (MaNhom) REFERENCES dbo.[NhomDongHanh] (MaNhom),
  CONSTRAINT [FK_BaoCaoDongHanh_MaTinNhan] FOREIGN KEY (MaTinNhan) REFERENCES dbo.[TinNhanDongHanh] (MaTinNhan),
  CONSTRAINT [FK_BaoCaoDongHanh_MaAdmin] FOREIGN KEY (MaAdmin) REFERENCES dbo.[TaiKhoan] (MaTaiKhoan)
 );
 CREATE INDEX [IX_BaoCaoDH_TT] ON dbo.[BaoCaoDongHanh] (TrangThai,ThoiDiemTao);
END;
IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_BCDH_KetQua' AND definition NOT LIKE '%IS NOT NULL%')
BEGIN
 ALTER TABLE dbo.BinhChonDongHanh DROP CONSTRAINT CK_BCDH_KetQua;
 ALTER TABLE dbo.BinhChonDongHanh WITH CHECK ADD CONSTRAINT CK_BCDH_KetQua CHECK ((TrangThai='DaChot' AND LuaChonChot IS NOT NULL AND LuaChonChot BETWEEN 1 AND 5) OR (TrangThai<>'DaChot' AND LuaChonChot IS NULL));
END;
COMMIT TRANSACTION;
