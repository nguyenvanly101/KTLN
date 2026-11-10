-- Source of truth: FullText 4.8, tables 4.135–4.164. Do not generate this file from EF.
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO
CREATE TABLE [DiaDiemDuLich] (
    [MaDiaDiem] BIGINT IDENTITY(1,1) NOT NULL,
    [TenDiaDiem] NVARCHAR(200) NOT NULL,
    [LoaiDiaDiem] NVARCHAR(50) NOT NULL,
    [KhuVuc] NVARCHAR(100) NOT NULL,
    [DiaChi] NVARCHAR(300) NULL,
    [MoTa] NVARCHAR(4000) NULL,
    [DangHienThi] BIT NOT NULL CONSTRAINT [DF_DiaDiemDuLich_DangHienThi] DEFAULT (1),
    CONSTRAINT [PK_DiaDiemDuLich] PRIMARY KEY ([MaDiaDiem]),
    CONSTRAINT [CK_DiaDiemDuLich_KhuVuc_KhongRong] CHECK (LEN(LTRIM([KhuVuc])) > 0),
    CONSTRAINT [CK_DiaDiemDuLich_LoaiDiaDiem_KhongRong] CHECK (LEN(LTRIM([LoaiDiaDiem])) > 0),
    CONSTRAINT [CK_DiaDiemDuLich_TenDiaDiem_KhongRong] CHECK (LEN(LTRIM([TenDiaDiem])) > 0)
);
GO
CREATE TABLE [TaiKhoan] (
    [MaTaiKhoan] BIGINT IDENTITY(1,1) NOT NULL,
    [Email] NVARCHAR(256) NOT NULL,
    [SoDienThoai] VARCHAR(15) NULL,
    [MatKhauBam] NVARCHAR(500) NOT NULL,
    [VaiTro] VARCHAR(30) NOT NULL,
    [TrangThai] VARCHAR(30) NOT NULL CONSTRAINT [DF_TaiKhoan_TrangThai] DEFAULT ('HoatDong'),
    [DauPhien] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_TaiKhoan_DauPhien] DEFAULT (NEWID()),
    [PhaiDoiMatKhau] BIT NOT NULL CONSTRAINT [DF_TaiKhoan_PhaiDoiMatKhau] DEFAULT (0),
    [ThoiDiemTao] DATETIME2(0) NOT NULL CONSTRAINT [DF_TaiKhoan_ThoiDiemTao] DEFAULT (SYSUTCDATETIME()),
    [ThoiDiemDangNhapCuoi] DATETIME2(0) NULL,
    [ThoiDiemNgung] DATETIME2(0) NULL,
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_TaiKhoan] PRIMARY KEY ([MaTaiKhoan]),
    CONSTRAINT [CK_TaiKhoan_Email] CHECK ([Email] LIKE '%_@_%._%' AND [Email] NOT LIKE '% %'),
    CONSTRAINT [CK_TaiKhoan_Ngung] CHECK ((TrangThai = 'DaNgung' AND ThoiDiemNgung IS NOT NULL) OR (TrangThai = 'HoatDong' AND ThoiDiemNgung IS NULL)),
    CONSTRAINT [CK_TaiKhoan_SoDienThoai] CHECK ([SoDienThoai] LIKE '0%' AND [SoDienThoai] NOT LIKE '%[^0-9]%' AND LEN([SoDienThoai]) BETWEEN 10 AND 11),
    CONSTRAINT [CK_TaiKhoan_TrangThai] CHECK ([TrangThai] IN ('HoatDong','DaNgung')),
    CONSTRAINT [CK_TaiKhoan_VaiTro] CHECK ([VaiTro] IN ('KHACH_DU_LICH','NHA_CUNG_CAP','QUAN_TRI_VIEN'))
);
GO
CREATE TABLE [HoSoNguoiDung] (
    [MaHoSo] BIGINT IDENTITY(1,1) NOT NULL,
    [MaTaiKhoan] BIGINT NOT NULL,
    [TenHienThi] NVARCHAR(100) NOT NULL,
    [AnhDaiDien] NVARCHAR(400) NULL,
    [GioiThieu] NVARCHAR(1000) NULL,
    [NoiSinhSong] NVARCHAR(200) NULL,
    [SoThich] NVARCHAR(500) NULL,
    [ThongTinChiaSe] VARCHAR(30) NOT NULL CONSTRAINT [DF_HoSoNguoiDung_ThongTinChiaSe] DEFAULT ('RiengTu'),
    [ThoiDiemCapNhat] DATETIME2(0) NOT NULL CONSTRAINT [DF_HoSoNguoiDung_ThoiDiemCapNhat] DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_HoSoNguoiDung] PRIMARY KEY ([MaHoSo]),
    CONSTRAINT [CK_HoSoNguoiDung_TenHienThi_KhongRong] CHECK (LEN(LTRIM([TenHienThi])) > 0),
    CONSTRAINT [CK_HoSoNguoiDung_ThongTinChiaSe] CHECK ([ThongTinChiaSe] IN ('CongKhai','KhiKetNoi','RiengTu')),
    CONSTRAINT [FK_HoSoNguoiDung_MaTaiKhoan] FOREIGN KEY ([MaTaiKhoan]) REFERENCES [TaiKhoan] ([MaTaiKhoan]) ON DELETE NO ACTION
);
GO
CREATE TABLE [KeHoachChuyenDi] (
    [MaKeHoach] BIGINT IDENTITY(1,1) NOT NULL,
    [MaTaiKhoan] BIGINT NOT NULL,
    [TenKeHoach] NVARCHAR(150) NOT NULL,
    [NoiDen] NVARCHAR(200) NOT NULL,
    [NgayBatDau] DATE NOT NULL,
    [NgayKetThuc] DATE NOT NULL,
    [NganSachDuKien] DECIMAL(18,2) NULL,
    [TrangThai] VARCHAR(30) NOT NULL CONSTRAINT [DF_KeHoachChuyenDi_TrangThai] DEFAULT ('Nhap'),
    [ThoiDiemTao] DATETIME2(0) NOT NULL CONSTRAINT [DF_KeHoachChuyenDi_ThoiDiemTao] DEFAULT (SYSUTCDATETIME()),
    [ThoiDiemCapNhat] DATETIME2(0) NOT NULL CONSTRAINT [DF_KeHoachChuyenDi_ThoiDiemCapNhat] DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_KeHoachChuyenDi] PRIMARY KEY ([MaKeHoach]),
    CONSTRAINT [CK_KeHoachChuyenDi_NganSachDuKien] CHECK ([NganSachDuKien] >= 0),
    CONSTRAINT [CK_KeHoachChuyenDi_Ngay] CHECK (NgayKetThuc >= NgayBatDau),
    CONSTRAINT [CK_KeHoachChuyenDi_NoiDen_KhongRong] CHECK (LEN(LTRIM([NoiDen])) > 0),
    CONSTRAINT [CK_KeHoachChuyenDi_TenKeHoach_KhongRong] CHECK (LEN(LTRIM([TenKeHoach])) > 0),
    CONSTRAINT [CK_KeHoachChuyenDi_TrangThai] CHECK ([TrangThai] IN ('Nhap','DaChapNhan')),
    CONSTRAINT [FK_KeHoachChuyenDi_MaTaiKhoan] FOREIGN KEY ([MaTaiKhoan]) REFERENCES [TaiKhoan] ([MaTaiKhoan]) ON DELETE NO ACTION
);
GO
CREATE TABLE [NhaCungCapDichVu] (
    [MaNhaCungCap] BIGINT IDENTITY(1,1) NOT NULL,
    [MaTaiKhoan] BIGINT NOT NULL,
    [TenDonVi] NVARCHAR(200) NOT NULL,
    [LoaiHinh] NVARCHAR(50) NOT NULL,
    [NguoiLienHe] NVARCHAR(100) NOT NULL,
    [DiaChi] NVARCHAR(300) NOT NULL,
    [SoDienThoai] VARCHAR(15) NOT NULL,
    [EmailLienHe] NVARCHAR(256) NOT NULL,
    [TrangThaiXacMinh] VARCHAR(30) NOT NULL CONSTRAINT [DF_NhaCungCapDichVu_TrangThaiXacMinh] DEFAULT ('ChoXacMinh'),
    [TrangThaiQuyenHoatDong] VARCHAR(30) NULL,
    [ThoiDiemTao] DATETIME2(0) NOT NULL CONSTRAINT [DF_NhaCungCapDichVu_ThoiDiemTao] DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_NhaCungCapDichVu] PRIMARY KEY ([MaNhaCungCap]),
    CONSTRAINT [CK_NhaCungCapDichVu_EmailLienHe] CHECK ([EmailLienHe] LIKE '%_@_%._%' AND [EmailLienHe] NOT LIKE '% %'),
    CONSTRAINT [CK_NhaCungCapDichVu_LoaiHinh_KhongRong] CHECK (LEN(LTRIM([LoaiHinh])) > 0),
    CONSTRAINT [CK_NhaCungCapDichVu_NguoiLienHe_KhongRong] CHECK (LEN(LTRIM([NguoiLienHe])) > 0),
    CONSTRAINT [CK_NhaCungCapDichVu_SoDienThoai] CHECK ([SoDienThoai] LIKE '0%' AND [SoDienThoai] NOT LIKE '%[^0-9]%' AND LEN([SoDienThoai]) BETWEEN 10 AND 11),
    CONSTRAINT [CK_NhaCungCapDichVu_TenDonVi_KhongRong] CHECK (LEN(LTRIM([TenDonVi])) > 0),
    CONSTRAINT [CK_NhaCungCapDichVu_TrangThai] CHECK ((TrangThaiXacMinh IN ('ChoXacMinh', 'CanBoSung', 'TuChoi') AND TrangThaiQuyenHoatDong IS NULL) OR (TrangThaiXacMinh = 'DaXacMinh' AND TrangThaiQuyenHoatDong IS NOT NULL AND TrangThaiQuyenHoatDong IN ('DangHoatDong', 'TamDinhChi')) OR (TrangThaiXacMinh = 'DaThuHoi' AND TrangThaiQuyenHoatDong IS NOT NULL AND TrangThaiQuyenHoatDong = 'DaThuHoi')),
    CONSTRAINT [CK_NhaCungCapDichVu_TrangThaiQuyenHoatDong] CHECK ([TrangThaiQuyenHoatDong] IN ('DangHoatDong','TamDinhChi','DaThuHoi')),
    CONSTRAINT [CK_NhaCungCapDichVu_TrangThaiXacMinh] CHECK ([TrangThaiXacMinh] IN ('ChoXacMinh','CanBoSung','DaXacMinh','TuChoi','DaThuHoi')),
    CONSTRAINT [FK_NhaCungCapDichVu_MaTaiKhoan] FOREIGN KEY ([MaTaiKhoan]) REFERENCES [TaiKhoan] ([MaTaiKhoan]) ON DELETE NO ACTION
);
GO
CREATE TABLE [NhatKyThaoTac] (
    [MaNhatKy] BIGINT IDENTITY(1,1) NOT NULL,
    [MaTaiKhoan] BIGINT NULL,
    [LoaiSuKien] NVARCHAR(40) NOT NULL,
    [DoiTuong] NVARCHAR(40) NULL,
    [MaDoiTuong] BIGINT NULL,
    [TrangThaiTruoc] NVARCHAR(200) NULL,
    [TrangThaiSau] NVARCHAR(200) NULL,
    [LyDo] NVARCHAR(500) NULL,
    [DiaChiIP] NVARCHAR(45) NULL,
    [ThoiDiem] DATETIME2(0) NOT NULL CONSTRAINT [DF_NhatKyThaoTac_ThoiDiem] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_NhatKyThaoTac] PRIMARY KEY ([MaNhatKy]),
    CONSTRAINT [CK_NhatKyThaoTac_LoaiSuKien_KhongRong] CHECK (LEN(LTRIM([LoaiSuKien])) > 0),
    CONSTRAINT [FK_NhatKyThaoTac_MaTaiKhoan] FOREIGN KEY ([MaTaiKhoan]) REFERENCES [TaiKhoan] ([MaTaiKhoan]) ON DELETE NO ACTION
);
GO
CREATE TABLE [NhuCauDongHanh] (
    [MaNhuCau] BIGINT IDENTITY(1,1) NOT NULL,
    [MaTaiKhoan] BIGINT NOT NULL,
    [NoiDung] NVARCHAR(2000) NOT NULL,
    [DiemDen] NVARCHAR(200) NOT NULL,
    [NgayBatDau] DATE NOT NULL,
    [NgayKetThuc] DATE NOT NULL,
    [NganSachDuKien] DECIMAL(18,2) NULL,
    [HinhThucDi] NVARCHAR(50) NULL,
    [MongMuonDongHanh] NVARCHAR(1000) NULL,
    [PhamViChiaSe] VARCHAR(30) NOT NULL,
    [ThoiDiemDang] DATETIME2(0) NOT NULL CONSTRAINT [DF_NhuCauDongHanh_ThoiDiemDang] DEFAULT (SYSUTCDATETIME()),
    [TrangThaiCongBo] VARCHAR(30) NOT NULL CONSTRAINT [DF_NhuCauDongHanh_TrangThaiCongBo] DEFAULT ('DangCongBo'),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_NhuCauDongHanh] PRIMARY KEY ([MaNhuCau]),
    CONSTRAINT [CK_NhuCauDongHanh_DiemDen_KhongRong] CHECK (LEN(LTRIM([DiemDen])) > 0),
    CONSTRAINT [CK_NhuCauDongHanh_NganSachDuKien] CHECK ([NganSachDuKien] >= 0),
    CONSTRAINT [CK_NhuCauDongHanh_Ngay] CHECK (NgayKetThuc >= NgayBatDau),
    CONSTRAINT [CK_NhuCauDongHanh_PhamViChiaSe] CHECK ([PhamViChiaSe] IN ('CongKhai','DaDangNhap','KhiKetNoi')),
    CONSTRAINT [CK_NhuCauDongHanh_TrangThaiCongBo] CHECK ([TrangThaiCongBo] IN ('DangCongBo','DungCongBo')),
    CONSTRAINT [FK_NhuCauDongHanh_MaTaiKhoan] FOREIGN KEY ([MaTaiKhoan]) REFERENCES [TaiKhoan] ([MaTaiKhoan]) ON DELETE NO ACTION
);
GO
CREATE TABLE [ThongBao] (
    [MaThongBao] BIGINT IDENTITY(1,1) NOT NULL,
    [MaNguoiNhan] BIGINT NOT NULL,
    [LoaiThongBao] NVARCHAR(40) NOT NULL,
    [NoiDung] NVARCHAR(500) NOT NULL,
    [DuongDan] NVARCHAR(300) NULL,
    [DaDoc] BIT NOT NULL CONSTRAINT [DF_ThongBao_DaDoc] DEFAULT (0),
    [ThoiDiemTao] DATETIME2(0) NOT NULL CONSTRAINT [DF_ThongBao_ThoiDiemTao] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_ThongBao] PRIMARY KEY ([MaThongBao]),
    CONSTRAINT [CK_ThongBao_LoaiThongBao_KhongRong] CHECK (LEN(LTRIM([LoaiThongBao])) > 0),
    CONSTRAINT [FK_ThongBao_MaNguoiNhan] FOREIGN KEY ([MaNguoiNhan]) REFERENCES [TaiKhoan] ([MaTaiKhoan]) ON DELETE NO ACTION
);
GO
CREATE TABLE [TinhHuongPhatSinh] (
    [MaTinhHuong] BIGINT IDENTITY(1,1) NOT NULL,
    [MaKeHoach] BIGINT NOT NULL,
    [LoaiTinhHuong] NVARCHAR(50) NOT NULL,
    [MoTa] NVARCHAR(2000) NOT NULL,
    [ViTri] NVARCHAR(300) NULL,
    [ThoiDiemGhiNhan] DATETIME2(0) NOT NULL CONSTRAINT [DF_TinhHuongPhatSinh_ThoiDiemGhiNhan] DEFAULT (SYSUTCDATETIME()),
    [TrangThaiXuLy] VARCHAR(30) NOT NULL CONSTRAINT [DF_TinhHuongPhatSinh_TrangThaiXuLy] DEFAULT ('ChuaGiaiQuyet'),
    [KetQuaXuLy] NVARCHAR(2000) NULL,
    [ThoiDiemCapNhat] DATETIME2(0) NOT NULL CONSTRAINT [DF_TinhHuongPhatSinh_ThoiDiemCapNhat] DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_TinhHuongPhatSinh] PRIMARY KEY ([MaTinhHuong]),
    CONSTRAINT [CK_TinhHuongPhatSinh_KetQua] CHECK (TrangThaiXuLy <> 'DaGiaiQuyet' OR (KetQuaXuLy IS NOT NULL AND LEN(LTRIM(KetQuaXuLy)) > 0)),
    CONSTRAINT [CK_TinhHuongPhatSinh_LoaiTinhHuong_KhongRong] CHECK (LEN(LTRIM([LoaiTinhHuong])) > 0),
    CONSTRAINT [CK_TinhHuongPhatSinh_TrangThaiXuLy] CHECK ([TrangThaiXuLy] IN ('ChuaGiaiQuyet','DaChonPhuongAn','GiaiQuyetMotPhan','DaGiaiQuyet','DungXuLy')),
    CONSTRAINT [FK_TinhHuongPhatSinh_MaKeHoach] FOREIGN KEY ([MaKeHoach]) REFERENCES [KeHoachChuyenDi] ([MaKeHoach]) ON DELETE NO ACTION
);
GO
CREATE TABLE [HoSoXacMinhNhaCungCap] (
    [MaHoSo] BIGINT IDENTITY(1,1) NOT NULL,
    [MaNhaCungCap] BIGINT NOT NULL,
    [NoiDungDangKy] NVARCHAR(2000) NOT NULL,
    [NguoiDaiDien] NVARCHAR(100) NOT NULL,
    [ThoiDiemNop] DATETIME2(0) NOT NULL CONSTRAINT [DF_HoSoXacMinhNhaCungCap_ThoiDiemNop] DEFAULT (SYSUTCDATETIME()),
    [SoLanBoSung] INT NOT NULL CONSTRAINT [DF_HoSoXacMinhNhaCungCap_SoLanBoSung] DEFAULT (0),
    [TrangThaiXuLy] VARCHAR(30) NOT NULL CONSTRAINT [DF_HoSoXacMinhNhaCungCap_TrangThaiXuLy] DEFAULT ('ChoXacMinh'),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_HoSoXacMinhNhaCungCap] PRIMARY KEY ([MaHoSo]),
    CONSTRAINT [CK_HoSoXacMinhNhaCungCap_NguoiDaiDien_KhongRong] CHECK (LEN(LTRIM([NguoiDaiDien])) > 0),
    CONSTRAINT [CK_HoSoXacMinhNhaCungCap_SoLanBoSung] CHECK ([SoLanBoSung] >= 0),
    CONSTRAINT [CK_HoSoXacMinhNhaCungCap_TrangThaiXuLy] CHECK ([TrangThaiXuLy] IN ('ChoXacMinh','CanBoSung','DaXacMinh','TuChoi')),
    CONSTRAINT [FK_HoSoXacMinhNhaCungCap_MaNhaCungCap] FOREIGN KEY ([MaNhaCungCap]) REFERENCES [NhaCungCapDichVu] ([MaNhaCungCap]) ON DELETE NO ACTION
);
GO
CREATE TABLE [QuyetDinhQuyenHoatDong] (
    [MaQuyetDinh] BIGINT IDENTITY(1,1) NOT NULL,
    [MaNhaCungCap] BIGINT NOT NULL,
    [TrangThaiTruoc] VARCHAR(30) NOT NULL,
    [TrangThaiSau] VARCHAR(30) NOT NULL,
    [LyDo] NVARCHAR(500) NOT NULL,
    [CanCu] NVARCHAR(500) NOT NULL,
    [ThoiHan] DATE NULL,
    [MaAdmin] BIGINT NOT NULL,
    [ThoiDiem] DATETIME2(0) NOT NULL CONSTRAINT [DF_QuyetDinhQuyenHoatDong_ThoiDiem] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_QuyetDinhQuyenHoatDong] PRIMARY KEY ([MaQuyetDinh]),
    CONSTRAINT [CK_QuyetDinhQuyenHoatDong_DoiTrangThai] CHECK (TrangThaiTruoc <> TrangThaiSau),
    CONSTRAINT [CK_QuyetDinhQuyenHoatDong_LyDoCanCu] CHECK ((LyDo IS NOT NULL AND LEN(LTRIM(LyDo)) > 0) AND (CanCu IS NOT NULL AND LEN(LTRIM(CanCu)) > 0)),
    CONSTRAINT [CK_QuyetDinhQuyenHoatDong_ThoiHan] CHECK (ThoiHan IS NULL OR TrangThaiSau = 'TamDinhChi'),
    CONSTRAINT [CK_QuyetDinhQuyenHoatDong_TrangThaiSau] CHECK ([TrangThaiSau] IN ('DangHoatDong','TamDinhChi','DaThuHoi')),
    CONSTRAINT [CK_QuyetDinhQuyenHoatDong_TrangThaiTruoc] CHECK ([TrangThaiTruoc] IN ('DangHoatDong','TamDinhChi','DaThuHoi')),
    CONSTRAINT [FK_QuyetDinhQuyenHoatDong_MaNhaCungCap] FOREIGN KEY ([MaNhaCungCap]) REFERENCES [NhaCungCapDichVu] ([MaNhaCungCap]) ON DELETE NO ACTION,
    CONSTRAINT [FK_QuyetDinhQuyenHoatDong_MaAdmin] FOREIGN KEY ([MaAdmin]) REFERENCES [TaiKhoan] ([MaTaiKhoan]) ON DELETE NO ACTION
);
GO
CREATE TABLE [DeNghiKetNoiDongHanh] (
    [MaDeNghi] BIGINT IDENTITY(1,1) NOT NULL,
    [MaNhuCau] BIGINT NOT NULL,
    [MaNguoiGui] BIGINT NOT NULL,
    [LoiGioiThieu] NVARCHAR(1000) NULL,
    [ThoiDiemGui] DATETIME2(0) NOT NULL CONSTRAINT [DF_DeNghiKetNoiDongHanh_ThoiDiemGui] DEFAULT (SYSUTCDATETIME()),
    [ThoiDiemPhanHoi] DATETIME2(0) NULL,
    [TrangThai] VARCHAR(30) NOT NULL CONSTRAINT [DF_DeNghiKetNoiDongHanh_TrangThai] DEFAULT ('ChoPhanHoi'),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_DeNghiKetNoiDongHanh] PRIMARY KEY ([MaDeNghi]),
    CONSTRAINT [CK_DeNghiKetNoiDongHanh_PhanHoi] CHECK ((TrangThai = 'ChoPhanHoi' AND ThoiDiemPhanHoi IS NULL) OR (TrangThai <> 'ChoPhanHoi' AND ThoiDiemPhanHoi IS NOT NULL)),
    CONSTRAINT [CK_DeNghiKetNoiDongHanh_TrangThai] CHECK ([TrangThai] IN ('ChoPhanHoi','DaChapNhan','TuChoi','DaRut','HetHieuLuc')),
    CONSTRAINT [FK_DeNghiKetNoiDongHanh_MaNhuCau] FOREIGN KEY ([MaNhuCau]) REFERENCES [NhuCauDongHanh] ([MaNhuCau]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DeNghiKetNoiDongHanh_MaNguoiGui] FOREIGN KEY ([MaNguoiGui]) REFERENCES [TaiKhoan] ([MaTaiKhoan]) ON DELETE NO ACTION
);
GO
CREATE TABLE [PhuongAnXuLy] (
    [MaPhuongAn] BIGINT IDENTITY(1,1) NOT NULL,
    [MaTinhHuong] BIGINT NOT NULL,
    [NoiDung] NVARCHAR(1000) NOT NULL,
    [ThoiDiemChon] DATETIME2(0) NOT NULL CONSTRAINT [DF_PhuongAnXuLy_ThoiDiemChon] DEFAULT (SYSUTCDATETIME()),
    [LaHienHanh] BIT NOT NULL CONSTRAINT [DF_PhuongAnXuLy_LaHienHanh] DEFAULT (1),
    CONSTRAINT [PK_PhuongAnXuLy] PRIMARY KEY ([MaPhuongAn]),
    CONSTRAINT [FK_PhuongAnXuLy_MaTinhHuong] FOREIGN KEY ([MaTinhHuong]) REFERENCES [TinhHuongPhatSinh] ([MaTinhHuong]) ON DELETE NO ACTION
);
GO
CREATE TABLE [QuyetDinhXacMinh] (
    [MaQuyetDinh] BIGINT IDENTITY(1,1) NOT NULL,
    [MaHoSo] BIGINT NOT NULL,
    [LanXuLy] INT NOT NULL,
    [KetQua] VARCHAR(30) NOT NULL,
    [KetQuaXacMinh] NVARCHAR(500) NULL,
    [LyDo] NVARCHAR(500) NULL,
    [MaAdminDuyet] BIGINT NOT NULL,
    [ThoiDiemDuyet] DATETIME2(0) NOT NULL CONSTRAINT [DF_QuyetDinhXacMinh_ThoiDiemDuyet] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_QuyetDinhXacMinh] PRIMARY KEY ([MaQuyetDinh]),
    CONSTRAINT [CK_QuyetDinhXacMinh_KetQua] CHECK ([KetQua] IN ('CanBoSung','DaXacMinh','TuChoi')),
    CONSTRAINT [CK_QuyetDinhXacMinh_LanXuLy] CHECK ([LanXuLy] >= 1),
    CONSTRAINT [CK_QuyetDinhXacMinh_LyDo] CHECK (KetQua = 'DaXacMinh' OR (LyDo IS NOT NULL AND LEN(LTRIM(LyDo)) > 0)),
    CONSTRAINT [FK_QuyetDinhXacMinh_MaHoSo] FOREIGN KEY ([MaHoSo]) REFERENCES [HoSoXacMinhNhaCungCap] ([MaHoSo]) ON DELETE NO ACTION,
    CONSTRAINT [FK_QuyetDinhXacMinh_MaAdminDuyet] FOREIGN KEY ([MaAdminDuyet]) REFERENCES [TaiKhoan] ([MaTaiKhoan]) ON DELETE NO ACTION
);
GO
CREATE TABLE [TaiLieuXacMinh] (
    [MaTaiLieu] BIGINT IDENTITY(1,1) NOT NULL,
    [MaHoSo] BIGINT NOT NULL,
    [TenTepGoc] NVARCHAR(255) NOT NULL,
    [DuongDan] NVARCHAR(400) NOT NULL,
    [LoaiTep] VARCHAR(30) NOT NULL,
    [KichThuoc] INT NOT NULL,
    [LanNop] INT NOT NULL CONSTRAINT [DF_TaiLieuXacMinh_LanNop] DEFAULT (0),
    [ThoiDiemTai] DATETIME2(0) NOT NULL CONSTRAINT [DF_TaiLieuXacMinh_ThoiDiemTai] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_TaiLieuXacMinh] PRIMARY KEY ([MaTaiLieu]),
    CONSTRAINT [CK_TaiLieuXacMinh_KichThuoc] CHECK ([KichThuoc] BETWEEN 1 AND 5242880),
    CONSTRAINT [CK_TaiLieuXacMinh_LanNop] CHECK ([LanNop] >= 0),
    CONSTRAINT [CK_TaiLieuXacMinh_LoaiTep] CHECK ([LoaiTep] IN ('pdf','jpg','png')),
    CONSTRAINT [CK_TaiLieuXacMinh_TenTepGoc_KhongRong] CHECK (LEN(LTRIM([TenTepGoc])) > 0),
    CONSTRAINT [FK_TaiLieuXacMinh_MaHoSo] FOREIGN KEY ([MaHoSo]) REFERENCES [HoSoXacMinhNhaCungCap] ([MaHoSo]) ON DELETE NO ACTION
);
GO
CREATE TABLE [DichVuDuLich] (
    [MaDichVu] BIGINT IDENTITY(1,1) NOT NULL,
    [MaNhaCungCap] BIGINT NOT NULL,
    [TenDichVu] NVARCHAR(200) NOT NULL,
    [LoaiDichVu] NVARCHAR(50) NOT NULL,
    [MoTa] NVARCHAR(4000) NULL,
    [KhuVucPhucVu] NVARCHAR(200) NOT NULL,
    [MaDiaDiem] BIGINT NULL,
    [GiaThamKhao] DECIMAL(18,2) NULL,
    [ThoiGianPhucVu] NVARCHAR(200) NULL,
    [DieuKienPhucVu] NVARCHAR(2000) NULL,
    [TamNgungTu] DATE NULL,
    [TamNgungDen] DATE NULL,
    [LyDoTamNgung] NVARCHAR(500) NULL,
    [TrangThaiCongBo] VARCHAR(30) NOT NULL,
    [TrangThaiTiepNhan] VARCHAR(30) NOT NULL CONSTRAINT [DF_DichVuDuLich_TrangThaiTiepNhan] DEFAULT ('TiepNhan'),
    [MaQuyetDinhKhongTiepNhan] BIGINT NULL,
    [PhienBanCongBo] INT NULL,
    [ThoiDiemTao] DATETIME2(0) NOT NULL CONSTRAINT [DF_DichVuDuLich_ThoiDiemTao] DEFAULT (SYSUTCDATETIME()),
    [ThoiDiemCapNhat] DATETIME2(0) NOT NULL CONSTRAINT [DF_DichVuDuLich_ThoiDiemCapNhat] DEFAULT (SYSUTCDATETIME()),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_DichVuDuLich] PRIMARY KEY ([MaDichVu]),
    CONSTRAINT [CK_DichVuDuLich_CongBo] CHECK (TrangThaiCongBo <> 'DaDuyet' OR PhienBanCongBo IS NOT NULL),
    CONSTRAINT [CK_DichVuDuLich_GiaThamKhao] CHECK ([GiaThamKhao] >= 0),
    CONSTRAINT [CK_DichVuDuLich_KhongTiepNhan] CHECK (MaQuyetDinhKhongTiepNhan IS NULL OR TrangThaiTiepNhan = 'KhongTiepNhanMoi'),
    CONSTRAINT [CK_DichVuDuLich_KhuVucPhucVu_KhongRong] CHECK (LEN(LTRIM([KhuVucPhucVu])) > 0),
    CONSTRAINT [CK_DichVuDuLich_LoaiDichVu_KhongRong] CHECK (LEN(LTRIM([LoaiDichVu])) > 0),
    CONSTRAINT [CK_DichVuDuLich_PhienBanCongBo] CHECK ([PhienBanCongBo] >= 1),
    CONSTRAINT [CK_DichVuDuLich_TamNgung] CHECK ((TamNgungTu IS NULL AND TamNgungDen IS NULL AND LyDoTamNgung IS NULL) OR (TamNgungTu IS NOT NULL AND (TamNgungDen IS NULL OR TamNgungDen >= TamNgungTu))),
    CONSTRAINT [CK_DichVuDuLich_TenDichVu_KhongRong] CHECK (LEN(LTRIM([TenDichVu])) > 0),
    CONSTRAINT [CK_DichVuDuLich_TrangThaiCongBo] CHECK ([TrangThaiCongBo] IN ('Nhap','ChoDuyet','DaDuyet','TuChoi','CanSua','NgungCongBo')),
    CONSTRAINT [CK_DichVuDuLich_TrangThaiTiepNhan] CHECK ([TrangThaiTiepNhan] IN ('TiepNhan','KhongTiepNhanMoi')),
    CONSTRAINT [FK_DichVuDuLich_MaDiaDiem] FOREIGN KEY ([MaDiaDiem]) REFERENCES [DiaDiemDuLich] ([MaDiaDiem]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DichVuDuLich_MaNhaCungCap] FOREIGN KEY ([MaNhaCungCap]) REFERENCES [NhaCungCapDichVu] ([MaNhaCungCap]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DichVuDuLich_MaQuyetDinhKhongTiepNhan] FOREIGN KEY ([MaQuyetDinhKhongTiepNhan]) REFERENCES [QuyetDinhQuyenHoatDong] ([MaQuyetDinh]) ON DELETE NO ACTION
);
GO
CREATE TABLE [GiaiTrinhNhaCungCap] (
    [MaGiaiTrinh] BIGINT IDENTITY(1,1) NOT NULL,
    [MaQuyetDinh] BIGINT NOT NULL,
    [NoiDung] NVARCHAR(2000) NOT NULL,
    [ThoiDiemGui] DATETIME2(0) NOT NULL CONSTRAINT [DF_GiaiTrinhNhaCungCap_ThoiDiemGui] DEFAULT (SYSUTCDATETIME()),
    [TrangThai] VARCHAR(30) NOT NULL CONSTRAINT [DF_GiaiTrinhNhaCungCap_TrangThai] DEFAULT ('ChoXemXet'),
    [KetQuaXemXet] NVARCHAR(500) NULL,
    [MaAdminXemXet] BIGINT NULL,
    [ThoiDiemXemXet] DATETIME2(0) NULL,
    CONSTRAINT [PK_GiaiTrinhNhaCungCap] PRIMARY KEY ([MaGiaiTrinh]),
    CONSTRAINT [CK_GiaiTrinhNhaCungCap_TrangThai] CHECK ([TrangThai] IN ('ChoXemXet','DaXemXet')),
    CONSTRAINT [CK_GiaiTrinhNhaCungCap_XemXet] CHECK ((TrangThai = 'ChoXemXet' AND MaAdminXemXet IS NULL AND ThoiDiemXemXet IS NULL) OR (TrangThai = 'DaXemXet' AND MaAdminXemXet IS NOT NULL AND ThoiDiemXemXet IS NOT NULL AND KetQuaXemXet IS NOT NULL AND LEN(LTRIM(KetQuaXemXet)) > 0)),
    CONSTRAINT [FK_GiaiTrinhNhaCungCap_MaQuyetDinh] FOREIGN KEY ([MaQuyetDinh]) REFERENCES [QuyetDinhQuyenHoatDong] ([MaQuyetDinh]) ON DELETE NO ACTION,
    CONSTRAINT [FK_GiaiTrinhNhaCungCap_MaAdminXemXet] FOREIGN KEY ([MaAdminXemXet]) REFERENCES [TaiKhoan] ([MaTaiKhoan]) ON DELETE NO ACTION
);
GO
CREATE TABLE [ThoaThuanDongHanh] (
    [MaThoaThuan] BIGINT IDENTITY(1,1) NOT NULL,
    [MaDeNghi] BIGINT NOT NULL,
    [PhienBan] INT NOT NULL,
    [NgayDi] DATE NOT NULL,
    [NgayVe] DATE NOT NULL,
    [NoiDen] NVARCHAR(200) NOT NULL,
    [NganSach] DECIMAL(18,2) NULL,
    [PhamViDiCung] NVARCHAR(1000) NULL,
    [MaNguoiDeXuat] BIGINT NOT NULL,
    [ThoiDiemDeXuat] DATETIME2(0) NOT NULL CONSTRAINT [DF_ThoaThuanDongHanh_ThoiDiemDeXuat] DEFAULT (SYSUTCDATETIME()),
    [MaNguoiXacNhan] BIGINT NULL,
    [ThoiDiemXacNhan] DATETIME2(0) NULL,
    [LoaiPhienBan] VARCHAR(30) NOT NULL CONSTRAINT [DF_ThoaThuanDongHanh_LoaiPhienBan] DEFAULT ('NoiDung'),
    [TrangThai] VARCHAR(30) NOT NULL CONSTRAINT [DF_ThoaThuanDongHanh_TrangThai] DEFAULT ('ChoXacNhan'),
    [LyDo] NVARCHAR(500) NULL,
    [MaNguoiKetThuc] BIGINT NULL,
    [ThoiDiemKetThuc] DATETIME2(0) NULL,
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_ThoaThuanDongHanh] PRIMARY KEY ([MaThoaThuan]),
    CONSTRAINT [CK_ThoaThuanDongHanh_DaXacNhan] CHECK (TrangThai <> 'DaXacNhan' OR MaNguoiXacNhan IS NOT NULL),
    CONSTRAINT [CK_ThoaThuanDongHanh_DeNghiNgung] CHECK (LoaiPhienBan = 'NoiDung' OR TrangThai IN ('ChoXacNhan', 'TuChoi', 'DaNgung', 'DaThayThe')),
    CONSTRAINT [CK_ThoaThuanDongHanh_HaiBen] CHECK (MaNguoiXacNhan IS NULL OR MaNguoiXacNhan <> MaNguoiDeXuat),
    CONSTRAINT [CK_ThoaThuanDongHanh_LoaiPhienBan] CHECK ([LoaiPhienBan] IN ('NoiDung','DeNghiNgung')),
    CONSTRAINT [CK_ThoaThuanDongHanh_LyDo] CHECK (TrangThai <> 'TuChoi' OR (LyDo IS NOT NULL AND LEN(LTRIM(LyDo)) > 0)),
    CONSTRAINT [CK_ThoaThuanDongHanh_NganSach] CHECK ([NganSach] >= 0),
    CONSTRAINT [CK_ThoaThuanDongHanh_Ngay] CHECK (NgayVe >= NgayDi),
    CONSTRAINT [CK_ThoaThuanDongHanh_Ngung] CHECK (TrangThai <> 'DaNgung' OR ThoiDiemKetThuc IS NOT NULL),
    CONSTRAINT [CK_ThoaThuanDongHanh_NoiDen_KhongRong] CHECK (LEN(LTRIM([NoiDen])) > 0),
    CONSTRAINT [CK_ThoaThuanDongHanh_PhienBan] CHECK ([PhienBan] >= 1),
    CONSTRAINT [CK_ThoaThuanDongHanh_Rut] CHECK (TrangThai <> 'MotBenDaRut' OR (MaNguoiKetThuc IS NOT NULL AND ThoiDiemKetThuc IS NOT NULL)),
    CONSTRAINT [CK_ThoaThuanDongHanh_TrangThai] CHECK ([TrangThai] IN ('ChoXacNhan','DaXacNhan','TuChoi','DaThayThe','DaNgung','MotBenDaRut')),
    CONSTRAINT [CK_ThoaThuanDongHanh_XacNhan] CHECK ((MaNguoiXacNhan IS NULL AND ThoiDiemXacNhan IS NULL) OR (MaNguoiXacNhan IS NOT NULL AND ThoiDiemXacNhan IS NOT NULL)),
    CONSTRAINT [FK_ThoaThuanDongHanh_MaDeNghi] FOREIGN KEY ([MaDeNghi]) REFERENCES [DeNghiKetNoiDongHanh] ([MaDeNghi]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ThoaThuanDongHanh_MaNguoiDeXuat] FOREIGN KEY ([MaNguoiDeXuat]) REFERENCES [TaiKhoan] ([MaTaiKhoan]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ThoaThuanDongHanh_MaNguoiKetThuc] FOREIGN KEY ([MaNguoiKetThuc]) REFERENCES [TaiKhoan] ([MaTaiKhoan]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ThoaThuanDongHanh_MaNguoiXacNhan] FOREIGN KEY ([MaNguoiXacNhan]) REFERENCES [TaiKhoan] ([MaTaiKhoan]) ON DELETE NO ACTION
);
GO
CREATE TABLE [BanCapNhatDichVu] (
    [MaBanCapNhat] BIGINT IDENTITY(1,1) NOT NULL,
    [MaDichVu] BIGINT NOT NULL,
    [SoPhienBan] INT NOT NULL,
    [TenDichVu] NVARCHAR(200) NOT NULL,
    [LoaiDichVu] NVARCHAR(50) NOT NULL,
    [MoTa] NVARCHAR(4000) NULL,
    [KhuVucPhucVu] NVARCHAR(200) NOT NULL,
    [MaDiaDiem] BIGINT NULL,
    [GiaThamKhao] DECIMAL(18,2) NULL,
    [ThoiGianPhucVu] NVARCHAR(200) NULL,
    [DieuKienPhucVu] NVARCHAR(2000) NULL,
    [TamNgungTu] DATE NULL,
    [TamNgungDen] DATE NULL,
    [LyDoTamNgung] NVARCHAR(500) NULL,
    [TrangThaiDuyet] VARCHAR(30) NOT NULL CONSTRAINT [DF_BanCapNhatDichVu_TrangThaiDuyet] DEFAULT ('ChoDuyet'),
    [LyDo] NVARCHAR(500) NULL,
    [MaAdminDuyet] BIGINT NULL,
    [ThoiDiemGui] DATETIME2(0) NOT NULL CONSTRAINT [DF_BanCapNhatDichVu_ThoiDiemGui] DEFAULT (SYSUTCDATETIME()),
    [ThoiDiemDuyet] DATETIME2(0) NULL,
    CONSTRAINT [PK_BanCapNhatDichVu] PRIMARY KEY ([MaBanCapNhat]),
    CONSTRAINT [CK_BanCapNhatDichVu_Duyet] CHECK ((TrangThaiDuyet = 'ChoDuyet' AND MaAdminDuyet IS NULL AND ThoiDiemDuyet IS NULL) OR (TrangThaiDuyet <> 'ChoDuyet' AND MaAdminDuyet IS NOT NULL AND ThoiDiemDuyet IS NOT NULL)),
    CONSTRAINT [CK_BanCapNhatDichVu_GiaThamKhao] CHECK ([GiaThamKhao] >= 0),
    CONSTRAINT [CK_BanCapNhatDichVu_KhuVucPhucVu_KhongRong] CHECK (LEN(LTRIM([KhuVucPhucVu])) > 0),
    CONSTRAINT [CK_BanCapNhatDichVu_LoaiDichVu_KhongRong] CHECK (LEN(LTRIM([LoaiDichVu])) > 0),
    CONSTRAINT [CK_BanCapNhatDichVu_LyDo] CHECK (TrangThaiDuyet NOT IN ('TuChoi', 'CanSua') OR (LyDo IS NOT NULL AND LEN(LTRIM(LyDo)) > 0)),
    CONSTRAINT [CK_BanCapNhatDichVu_SoPhienBan] CHECK ([SoPhienBan] >= 1),
    CONSTRAINT [CK_BanCapNhatDichVu_TamNgung] CHECK ((TamNgungTu IS NULL AND TamNgungDen IS NULL AND LyDoTamNgung IS NULL) OR (TamNgungTu IS NOT NULL AND (TamNgungDen IS NULL OR TamNgungDen >= TamNgungTu))),
    CONSTRAINT [CK_BanCapNhatDichVu_TenDichVu_KhongRong] CHECK (LEN(LTRIM([TenDichVu])) > 0),
    CONSTRAINT [CK_BanCapNhatDichVu_TrangThaiDuyet] CHECK ([TrangThaiDuyet] IN ('ChoDuyet','DaDuyet','TuChoi','CanSua')),
    CONSTRAINT [FK_BanCapNhatDichVu_MaDiaDiem] FOREIGN KEY ([MaDiaDiem]) REFERENCES [DiaDiemDuLich] ([MaDiaDiem]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BanCapNhatDichVu_MaDichVu] FOREIGN KEY ([MaDichVu]) REFERENCES [DichVuDuLich] ([MaDichVu]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BanCapNhatDichVu_MaAdminDuyet] FOREIGN KEY ([MaAdminDuyet]) REFERENCES [TaiKhoan] ([MaTaiKhoan]) ON DELETE NO ACTION
);
GO
CREATE TABLE [HoatDongLichTrinh] (
    [MaHoatDong] BIGINT IDENTITY(1,1) NOT NULL,
    [MaKeHoach] BIGINT NOT NULL,
    [Ngay] DATE NOT NULL,
    [Gio] TIME(0) NULL,
    [ThuTu] INT NOT NULL,
    [MaDiaDiem] BIGINT NULL,
    [MaDichVu] BIGINT NULL,
    [NoiDung] NVARCHAR(300) NOT NULL,
    [ChiPhiDuKien] DECIMAL(18,2) NULL,
    CONSTRAINT [PK_HoatDongLichTrinh] PRIMARY KEY ([MaHoatDong]),
    CONSTRAINT [CK_HoatDongLichTrinh_ChiPhiDuKien] CHECK ([ChiPhiDuKien] >= 0),
    CONSTRAINT [CK_HoatDongLichTrinh_MotMuc] CHECK (MaDiaDiem IS NULL OR MaDichVu IS NULL),
    CONSTRAINT [CK_HoatDongLichTrinh_ThuTu] CHECK ([ThuTu] >= 1),
    CONSTRAINT [FK_HoatDongLichTrinh_MaDiaDiem] FOREIGN KEY ([MaDiaDiem]) REFERENCES [DiaDiemDuLich] ([MaDiaDiem]) ON DELETE NO ACTION,
    CONSTRAINT [FK_HoatDongLichTrinh_MaDichVu] FOREIGN KEY ([MaDichVu]) REFERENCES [DichVuDuLich] ([MaDichVu]) ON DELETE NO ACTION,
    CONSTRAINT [FK_HoatDongLichTrinh_MaKeHoach] FOREIGN KEY ([MaKeHoach]) REFERENCES [KeHoachChuyenDi] ([MaKeHoach]) ON DELETE NO ACTION
);
GO
CREATE TABLE [YeuCauDatDichVu] (
    [MaYeuCau] BIGINT IDENTITY(1,1) NOT NULL,
    [MaTaiKhoan] BIGINT NOT NULL,
    [MaDichVu] BIGINT NOT NULL,
    [NgayBatDau] DATE NOT NULL,
    [NgayKetThuc] DATE NULL,
    [ThoiGianDenDi] NVARCHAR(100) NULL,
    [SoNguoi] INT NOT NULL,
    [SoLuong] INT NOT NULL,
    [NoiDungLuaChon] NVARCHAR(1000) NULL,
    [GhiChu] NVARCHAR(1000) NULL,
    [ChiPhiDuKien] DECIMAL(18,2) NULL,
    [DieuKienDeXuat] NVARCHAR(2000) NULL,
    [PhienBan] INT NOT NULL CONSTRAINT [DF_YeuCauDatDichVu_PhienBan] DEFAULT (1),
    [ThoiDiemGui] DATETIME2(0) NOT NULL CONSTRAINT [DF_YeuCauDatDichVu_ThoiDiemGui] DEFAULT (SYSUTCDATETIME()),
    [ThoiDiemPhanHoi] DATETIME2(0) NULL,
    [LyDo] NVARCHAR(500) NULL,
    [TrangThai] VARCHAR(30) NOT NULL CONSTRAINT [DF_YeuCauDatDichVu_TrangThai] DEFAULT ('ChoPhanHoi'),
    [MaLuotGui] UNIQUEIDENTIFIER NOT NULL,
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_YeuCauDatDichVu] PRIMARY KEY ([MaYeuCau]),
    CONSTRAINT [CK_YeuCauDatDichVu_ChiPhiDuKien] CHECK ([ChiPhiDuKien] >= 0),
    CONSTRAINT [CK_YeuCauDatDichVu_DeXuat] CHECK (TrangThai <> 'DeXuatKhac' OR (DieuKienDeXuat IS NOT NULL AND LEN(LTRIM(DieuKienDeXuat)) > 0)),
    CONSTRAINT [CK_YeuCauDatDichVu_LyDo] CHECK (TrangThai <> 'TuChoi' OR (LyDo IS NOT NULL AND LEN(LTRIM(LyDo)) > 0)),
    CONSTRAINT [CK_YeuCauDatDichVu_Ngay] CHECK (NgayKetThuc IS NULL OR NgayKetThuc >= NgayBatDau),
    CONSTRAINT [CK_YeuCauDatDichVu_PhanHoi] CHECK (TrangThai NOT IN ('DeXuatKhac', 'DaXacNhan', 'TuChoi') OR ThoiDiemPhanHoi IS NOT NULL),
    CONSTRAINT [CK_YeuCauDatDichVu_PhienBan] CHECK ([PhienBan] >= 1),
    CONSTRAINT [CK_YeuCauDatDichVu_SoLuong] CHECK ([SoLuong] BETWEEN 1 AND 100),
    CONSTRAINT [CK_YeuCauDatDichVu_SoNguoi] CHECK ([SoNguoi] BETWEEN 1 AND 100),
    CONSTRAINT [CK_YeuCauDatDichVu_TrangThai] CHECK ([TrangThai] IN ('ChoPhanHoi','DeXuatKhac','DaXacNhan','TuChoi','KhongTiepTuc','DaHuy','HoanThanh','ThucHienMotPhan','KhongThucHien','ChoKiemTra')),
    CONSTRAINT [FK_YeuCauDatDichVu_MaDichVu] FOREIGN KEY ([MaDichVu]) REFERENCES [DichVuDuLich] ([MaDichVu]) ON DELETE NO ACTION,
    CONSTRAINT [FK_YeuCauDatDichVu_MaTaiKhoan] FOREIGN KEY ([MaTaiKhoan]) REFERENCES [TaiKhoan] ([MaTaiKhoan]) ON DELETE NO ACTION
);
GO
CREATE TABLE [TaiLieuGiaiTrinh] (
    [MaTaiLieu] BIGINT IDENTITY(1,1) NOT NULL,
    [MaGiaiTrinh] BIGINT NOT NULL,
    [TenTepGoc] NVARCHAR(255) NOT NULL,
    [DuongDan] NVARCHAR(400) NOT NULL,
    [LoaiTep] VARCHAR(30) NOT NULL,
    [KichThuoc] INT NOT NULL,
    [ThoiDiemTai] DATETIME2(0) NOT NULL CONSTRAINT [DF_TaiLieuGiaiTrinh_ThoiDiemTai] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_TaiLieuGiaiTrinh] PRIMARY KEY ([MaTaiLieu]),
    CONSTRAINT [CK_TaiLieuGiaiTrinh_KichThuoc] CHECK ([KichThuoc] BETWEEN 1 AND 5242880),
    CONSTRAINT [CK_TaiLieuGiaiTrinh_LoaiTep] CHECK ([LoaiTep] IN ('pdf','jpg','png')),
    CONSTRAINT [CK_TaiLieuGiaiTrinh_TenTepGoc_KhongRong] CHECK (LEN(LTRIM([TenTepGoc])) > 0),
    CONSTRAINT [FK_TaiLieuGiaiTrinh_MaGiaiTrinh] FOREIGN KEY ([MaGiaiTrinh]) REFERENCES [GiaiTrinhNhaCungCap] ([MaGiaiTrinh]) ON DELETE NO ACTION
);
GO
CREATE TABLE [TinhHuongHoatDong] (
    [MaTinhHuong] BIGINT NOT NULL,
    [MaHoatDong] BIGINT NOT NULL,
    CONSTRAINT [PK_TinhHuongHoatDong] PRIMARY KEY ([MaTinhHuong], [MaHoatDong]),
    CONSTRAINT [FK_TinhHuongHoatDong_MaHoatDong] FOREIGN KEY ([MaHoatDong]) REFERENCES [HoatDongLichTrinh] ([MaHoatDong]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TinhHuongHoatDong_MaTinhHuong] FOREIGN KEY ([MaTinhHuong]) REFERENCES [TinhHuongPhatSinh] ([MaTinhHuong]) ON DELETE NO ACTION
);
GO
CREATE TABLE [DeNghiDieuChinhDichVu] (
    [MaDeNghi] BIGINT IDENTITY(1,1) NOT NULL,
    [MaYeuCau] BIGINT NOT NULL,
    [LoaiDeNghi] VARCHAR(30) NOT NULL,
    [DieuKienCu] NVARCHAR(2000) NOT NULL,
    [NgayBatDauMoi] DATE NULL,
    [NgayKetThucMoi] DATE NULL,
    [SoNguoiMoi] INT NULL,
    [SoLuongMoi] INT NULL,
    [DieuKienMoi] NVARCHAR(2000) NULL,
    [LyDo] NVARCHAR(500) NOT NULL,
    [ThoiDiemGui] DATETIME2(0) NOT NULL CONSTRAINT [DF_DeNghiDieuChinhDichVu_ThoiDiemGui] DEFAULT (SYSUTCDATETIME()),
    [QuyetDinh] NVARCHAR(500) NULL,
    [DieuKienDeXuat] NVARCHAR(2000) NULL,
    [ThoiDiemPhanHoi] DATETIME2(0) NULL,
    [TrangThai] VARCHAR(30) NOT NULL CONSTRAINT [DF_DeNghiDieuChinhDichVu_TrangThai] DEFAULT ('ChoPhanHoi'),
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_DeNghiDieuChinhDichVu] PRIMARY KEY ([MaDeNghi]),
    CONSTRAINT [CK_DeNghiDieuChinhDichVu_DeXuat] CHECK (TrangThai <> 'DeXuatKhac' OR (DieuKienDeXuat IS NOT NULL AND LEN(LTRIM(DieuKienDeXuat)) > 0)),
    CONSTRAINT [CK_DeNghiDieuChinhDichVu_DieuKienMoi] CHECK (LoaiDeNghi <> 'ThayDoi' OR NgayBatDauMoi IS NOT NULL OR NgayKetThucMoi IS NOT NULL OR SoNguoiMoi IS NOT NULL OR SoLuongMoi IS NOT NULL OR (DieuKienMoi IS NOT NULL AND LEN(LTRIM(DieuKienMoi)) > 0)),
    CONSTRAINT [CK_DeNghiDieuChinhDichVu_Huy] CHECK (LoaiDeNghi <> 'Huy' OR (NgayBatDauMoi IS NULL AND NgayKetThucMoi IS NULL AND SoNguoiMoi IS NULL AND SoLuongMoi IS NULL)),
    CONSTRAINT [CK_DeNghiDieuChinhDichVu_LoaiDeNghi] CHECK ([LoaiDeNghi] IN ('ThayDoi','Huy')),
    CONSTRAINT [CK_DeNghiDieuChinhDichVu_LyDo] CHECK ((LyDo IS NOT NULL AND LEN(LTRIM(LyDo)) > 0)),
    CONSTRAINT [CK_DeNghiDieuChinhDichVu_NgayMoi] CHECK (NgayBatDauMoi IS NULL OR NgayKetThucMoi IS NULL OR NgayKetThucMoi >= NgayBatDauMoi),
    CONSTRAINT [CK_DeNghiDieuChinhDichVu_PhanHoi] CHECK ((TrangThai = 'ChoPhanHoi' AND ThoiDiemPhanHoi IS NULL) OR (TrangThai <> 'ChoPhanHoi' AND ThoiDiemPhanHoi IS NOT NULL)),
    CONSTRAINT [CK_DeNghiDieuChinhDichVu_SoLuongMoi] CHECK ([SoLuongMoi] BETWEEN 1 AND 100),
    CONSTRAINT [CK_DeNghiDieuChinhDichVu_SoNguoiMoi] CHECK ([SoNguoiMoi] BETWEEN 1 AND 100),
    CONSTRAINT [CK_DeNghiDieuChinhDichVu_TrangThai] CHECK ([TrangThai] IN ('ChoPhanHoi','DeXuatKhac','ChapNhan','TuChoi','DaRut')),
    CONSTRAINT [CK_DeNghiDieuChinhDichVu_TuChoi] CHECK (TrangThai <> 'TuChoi' OR (QuyetDinh IS NOT NULL AND LEN(LTRIM(QuyetDinh)) > 0)),
    CONSTRAINT [FK_DeNghiDieuChinhDichVu_MaYeuCau] FOREIGN KEY ([MaYeuCau]) REFERENCES [YeuCauDatDichVu] ([MaYeuCau]) ON DELETE NO ACTION
);
GO
CREATE TABLE [KetQuaCungCapDichVu] (
    [MaKetQua] BIGINT IDENTITY(1,1) NOT NULL,
    [MaYeuCau] BIGINT NOT NULL,
    [HangMuc] NVARCHAR(200) NOT NULL,
    [KetQuaThucHien] VARCHAR(30) NOT NULL,
    [GhiChu] NVARCHAR(1000) NULL,
    [ThoiDiemGhi] DATETIME2(0) NOT NULL CONSTRAINT [DF_KetQuaCungCapDichVu_ThoiDiemGhi] DEFAULT (SYSUTCDATETIME()),
    [NoiDungPhanAnh] NVARCHAR(2000) NULL,
    [ThoiDiemPhanAnh] DATETIME2(0) NULL,
    [TrangThaiKiemTra] VARCHAR(30) NOT NULL CONSTRAINT [DF_KetQuaCungCapDichVu_TrangThaiKiemTra] DEFAULT ('DaGhiNhan'),
    [ThoiDiemKiemTra] DATETIME2(0) NULL,
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_KetQuaCungCapDichVu] PRIMARY KEY ([MaKetQua]),
    CONSTRAINT [CK_KetQuaCungCapDichVu_ChoKiemTra] CHECK (TrangThaiKiemTra <> 'ChoKiemTra' OR NoiDungPhanAnh IS NOT NULL OR KetQuaThucHien = 'ChuaXacDinh'),
    CONSTRAINT [CK_KetQuaCungCapDichVu_ChuaXacDinh] CHECK (KetQuaThucHien <> 'ChuaXacDinh' OR TrangThaiKiemTra = 'ChoKiemTra'),
    CONSTRAINT [CK_KetQuaCungCapDichVu_DaKiemTra] CHECK ((TrangThaiKiemTra = 'DaKiemTra' AND ThoiDiemKiemTra IS NOT NULL) OR (TrangThaiKiemTra <> 'DaKiemTra' AND ThoiDiemKiemTra IS NULL)),
    CONSTRAINT [CK_KetQuaCungCapDichVu_HangMuc_KhongRong] CHECK (LEN(LTRIM([HangMuc])) > 0),
    CONSTRAINT [CK_KetQuaCungCapDichVu_KetQuaThucHien] CHECK ([KetQuaThucHien] IN ('DayDu','MotPhan','KhongThucHien','KhachKhongDen','ChuaXacDinh')),
    CONSTRAINT [CK_KetQuaCungCapDichVu_PhanAnh] CHECK ((NoiDungPhanAnh IS NULL AND ThoiDiemPhanAnh IS NULL) OR (NoiDungPhanAnh IS NOT NULL AND ThoiDiemPhanAnh IS NOT NULL)),
    CONSTRAINT [CK_KetQuaCungCapDichVu_TrangThaiKiemTra] CHECK ([TrangThaiKiemTra] IN ('DaGhiNhan','ChoKiemTra','DaKiemTra')),
    CONSTRAINT [FK_KetQuaCungCapDichVu_MaYeuCau] FOREIGN KEY ([MaYeuCau]) REFERENCES [YeuCauDatDichVu] ([MaYeuCau]) ON DELETE NO ACTION
);
GO
CREATE UNIQUE INDEX [UX_BanCapNhatDichVu_ChoDuyet] ON [BanCapNhatDichVu] ([MaDichVu]) WHERE TrangThaiDuyet = 'ChoDuyet';
GO
CREATE UNIQUE INDEX [UX_BanCapNhatDichVu_PhienBan] ON [BanCapNhatDichVu] ([MaDichVu], [SoPhienBan]);
GO
CREATE UNIQUE INDEX [UX_DeNghiDieuChinhDichVu_Cho] ON [DeNghiDieuChinhDichVu] ([MaYeuCau]) WHERE TrangThai IN ('ChoPhanHoi', 'DeXuatKhac');
GO
CREATE UNIQUE INDEX [UX_DeNghiKetNoiDongHanh_HieuLuc] ON [DeNghiKetNoiDongHanh] ([MaNguoiGui], [MaNhuCau]) WHERE TrangThai IN ('ChoPhanHoi', 'DaChapNhan');
GO
CREATE UNIQUE INDEX [UX_HoSoNguoiDung_MaTaiKhoan] ON [HoSoNguoiDung] ([MaTaiKhoan]);
GO
CREATE UNIQUE INDEX [UX_HoSoXacMinh_DangXuLy] ON [HoSoXacMinhNhaCungCap] ([MaNhaCungCap]) WHERE TrangThaiXuLy IN ('ChoXacMinh', 'CanBoSung');
GO
CREATE UNIQUE INDEX [UX_KetQuaCungCapDichVu_HangMuc] ON [KetQuaCungCapDichVu] ([MaYeuCau], [HangMuc]);
GO
CREATE UNIQUE INDEX [UX_NhaCungCapDichVu_MaTaiKhoan] ON [NhaCungCapDichVu] ([MaTaiKhoan]);
GO
CREATE UNIQUE INDEX [UX_PhuongAnXuLy_HienHanh] ON [PhuongAnXuLy] ([MaTinhHuong]) WHERE LaHienHanh = 1;
GO
CREATE UNIQUE INDEX [UX_QuyetDinhXacMinh_LanXuLy] ON [QuyetDinhXacMinh] ([MaHoSo], [LanXuLy]);
GO
CREATE UNIQUE INDEX [UX_TaiKhoan_Email] ON [TaiKhoan] ([Email]);
GO
CREATE UNIQUE INDEX [UX_TaiKhoan_SoDienThoai] ON [TaiKhoan] ([SoDienThoai]) WHERE SoDienThoai IS NOT NULL;
GO
CREATE UNIQUE INDEX [UX_TaiLieuGiaiTrinh_DuongDan] ON [TaiLieuGiaiTrinh] ([DuongDan]);
GO
CREATE UNIQUE INDEX [UX_TaiLieuXacMinh_DuongDan] ON [TaiLieuXacMinh] ([DuongDan]);
GO
CREATE UNIQUE INDEX [UX_ThoaThuanDongHanh_ChoXacNhan] ON [ThoaThuanDongHanh] ([MaDeNghi]) WHERE TrangThai = 'ChoXacNhan';
GO
CREATE UNIQUE INDEX [UX_ThoaThuanDongHanh_DaXacNhan] ON [ThoaThuanDongHanh] ([MaDeNghi]) WHERE TrangThai = 'DaXacNhan';
GO
CREATE UNIQUE INDEX [UX_ThoaThuanDongHanh_PhienBan] ON [ThoaThuanDongHanh] ([MaDeNghi], [PhienBan]);
GO
CREATE UNIQUE INDEX [UX_YeuCauDatDichVu_LuotGui] ON [YeuCauDatDichVu] ([MaTaiKhoan], [MaLuotGui]);
GO
CREATE INDEX [IX_HoSoXacMinh_HangDoi] ON [HoSoXacMinhNhaCungCap] ([TrangThaiXuLy], [ThoiDiemNop]) INCLUDE ([MaNhaCungCap]);
GO
CREATE INDEX [IX_HoSoXacMinh_NhaCungCap] ON [HoSoXacMinhNhaCungCap] ([MaNhaCungCap], [ThoiDiemNop] DESC);
GO
CREATE INDEX [IX_TaiLieuXacMinh_HoSo] ON [TaiLieuXacMinh] ([MaHoSo], [LanNop]);
GO
CREATE INDEX [IX_NhaCungCap_TrangThai] ON [NhaCungCapDichVu] ([TrangThaiXacMinh], [TrangThaiQuyenHoatDong]) INCLUDE ([TenDonVi]);
GO
CREATE INDEX [IX_QuyetDinhQuyen_NhaCungCap] ON [QuyetDinhQuyenHoatDong] ([MaNhaCungCap], [ThoiDiem] DESC);
GO
CREATE INDEX [IX_GiaiTrinh_QuyetDinh] ON [GiaiTrinhNhaCungCap] ([MaQuyetDinh]);
GO
CREATE INDEX [IX_GiaiTrinh_ChoXemXet] ON [GiaiTrinhNhaCungCap] ([ThoiDiemGui]) INCLUDE ([MaQuyetDinh]) WHERE TrangThai = 'ChoXemXet';
GO
CREATE INDEX [IX_TaiLieuGiaiTrinh_GiaiTrinh] ON [TaiLieuGiaiTrinh] ([MaGiaiTrinh]);
GO
CREATE INDEX [IX_DiaDiem_TimKiem] ON [DiaDiemDuLich] ([KhuVuc], [LoaiDiaDiem]) INCLUDE ([TenDiaDiem]) WHERE DangHienThi = 1;
GO
CREATE INDEX [IX_DichVu_TimKiem] ON [DichVuDuLich] ([KhuVucPhucVu], [LoaiDichVu]) INCLUDE ([TenDichVu], [GiaThamKhao], [TrangThaiTiepNhan], [MaNhaCungCap]) WHERE TrangThaiCongBo = 'DaDuyet';
GO
CREATE INDEX [IX_DichVu_NhaCungCap] ON [DichVuDuLich] ([MaNhaCungCap], [TrangThaiCongBo]);
GO
CREATE INDEX [IX_DichVu_QuyetDinh] ON [DichVuDuLich] ([MaQuyetDinhKhongTiepNhan]) WHERE MaQuyetDinhKhongTiepNhan IS NOT NULL;
GO
CREATE INDEX [IX_DichVu_DiaDiem] ON [DichVuDuLich] ([MaDiaDiem]) WHERE MaDiaDiem IS NOT NULL;
GO
CREATE INDEX [IX_BanCapNhat_HangDoi] ON [BanCapNhatDichVu] ([ThoiDiemGui]) INCLUDE ([MaDichVu], [SoPhienBan]) WHERE TrangThaiDuyet = 'ChoDuyet';
GO
CREATE INDEX [IX_BanCapNhat_DiaDiem] ON [BanCapNhatDichVu] ([MaDiaDiem]) WHERE MaDiaDiem IS NOT NULL;
GO
CREATE INDEX [IX_KeHoach_TaiKhoan] ON [KeHoachChuyenDi] ([MaTaiKhoan], [NgayBatDau] DESC) INCLUDE ([TenKeHoach], [NoiDen], [TrangThai]);
GO
CREATE INDEX [IX_HoatDong_KeHoach] ON [HoatDongLichTrinh] ([MaKeHoach], [Ngay], [ThuTu]);
GO
CREATE INDEX [IX_HoatDong_DiaDiem] ON [HoatDongLichTrinh] ([MaDiaDiem]) WHERE MaDiaDiem IS NOT NULL;
GO
CREATE INDEX [IX_HoatDong_DichVu] ON [HoatDongLichTrinh] ([MaDichVu]) WHERE MaDichVu IS NOT NULL;
GO
CREATE INDEX [IX_TinhHuong_KeHoach] ON [TinhHuongPhatSinh] ([MaKeHoach], [ThoiDiemGhiNhan] DESC);
GO
CREATE INDEX [IX_TinhHuongHoatDong_HoatDong] ON [TinhHuongHoatDong] ([MaHoatDong]);
GO
CREATE INDEX [IX_PhuongAn_TinhHuong] ON [PhuongAnXuLy] ([MaTinhHuong], [ThoiDiemChon] DESC);
GO
CREATE INDEX [IX_NhuCau_TimKiem] ON [NhuCauDongHanh] ([DiemDen], [NgayBatDau]) INCLUDE ([NgayKetThuc], [PhamViChiaSe], [MaTaiKhoan]) WHERE TrangThaiCongBo = 'DangCongBo';
GO
CREATE INDEX [IX_NhuCau_TaiKhoan] ON [NhuCauDongHanh] ([MaTaiKhoan], [TrangThaiCongBo]);
GO
CREATE INDEX [IX_DeNghiKetNoi_NhuCau] ON [DeNghiKetNoiDongHanh] ([MaNhuCau], [TrangThai]) INCLUDE ([MaNguoiGui], [ThoiDiemGui]);
GO
CREATE INDEX [IX_DeNghiKetNoi_NguoiGui] ON [DeNghiKetNoiDongHanh] ([MaNguoiGui], [ThoiDiemGui] DESC);
GO
CREATE INDEX [IX_YeuCau_TaiKhoan] ON [YeuCauDatDichVu] ([MaTaiKhoan], [ThoiDiemGui] DESC) INCLUDE ([MaDichVu], [TrangThai]);
GO
CREATE INDEX [IX_YeuCau_DichVu] ON [YeuCauDatDichVu] ([MaDichVu], [TrangThai]) INCLUDE ([ThoiDiemGui]);
GO
CREATE INDEX [IX_DeNghiDieuChinh_YeuCau] ON [DeNghiDieuChinhDichVu] ([MaYeuCau], [ThoiDiemGui] DESC);
GO
CREATE INDEX [IX_KetQua_ChoKiemTra] ON [KetQuaCungCapDichVu] ([ThoiDiemPhanAnh]) INCLUDE ([MaYeuCau]) WHERE TrangThaiKiemTra = 'ChoKiemTra';
GO
CREATE INDEX [IX_ThongBao_NguoiNhan] ON [ThongBao] ([MaNguoiNhan], [DaDoc], [ThoiDiemTao] DESC) INCLUDE ([LoaiThongBao]);
GO
CREATE INDEX [IX_NhatKy_ThoiDiem] ON [NhatKyThaoTac] ([ThoiDiem] DESC) INCLUDE ([LoaiSuKien], [MaTaiKhoan]);
GO
CREATE INDEX [IX_NhatKy_DoiTuong] ON [NhatKyThaoTac] ([DoiTuong], [MaDoiTuong], [ThoiDiem] DESC) WHERE DoiTuong IS NOT NULL;
GO
CREATE INDEX [IX_NhatKy_TaiKhoan] ON [NhatKyThaoTac] ([MaTaiKhoan], [ThoiDiem] DESC) WHERE MaTaiKhoan IS NOT NULL;
GO
CREATE OR ALTER VIEW [dbo].[vw_DeNghiKetNoi_NguoiNhan] AS
SELECT d.MaDeNghi, d.MaNhuCau, d.MaNguoiGui, n.MaTaiKhoan AS MaNguoiNhan, d.TrangThai, d.ThoiDiemGui, d.ThoiDiemPhanHoi FROM dbo.DeNghiKetNoiDongHanh AS d JOIN dbo.NhuCauDongHanh AS n ON n.MaNhuCau = d.MaNhuCau;
GO
CREATE OR ALTER VIEW [dbo].[vw_TinhHuong_NguoiGhi] AS
SELECT t.MaTinhHuong, t.MaKeHoach, k.MaTaiKhoan AS MaNguoiGhi, t.LoaiTinhHuong, t.TrangThaiXuLy, t.ThoiDiemGhiNhan FROM dbo.TinhHuongPhatSinh AS t JOIN dbo.KeHoachChuyenDi AS k ON k.MaKeHoach = t.MaKeHoach;
GO
CREATE OR ALTER VIEW [dbo].[vw_GiaiTrinh_NhaCungCap] AS
SELECT g.MaGiaiTrinh, g.MaQuyetDinh, q.MaNhaCungCap, g.TrangThai, g.ThoiDiemGui FROM dbo.GiaiTrinhNhaCungCap AS g JOIN dbo.QuyetDinhQuyenHoatDong AS q ON q.MaQuyetDinh = g.MaQuyetDinh;
GO
CREATE OR ALTER VIEW [dbo].[vw_YeuCau_NhaCungCap] AS
SELECT y.MaYeuCau, y.MaTaiKhoan AS MaKhach, y.MaDichVu, d.MaNhaCungCap, c.MaTaiKhoan AS MaTaiKhoanNCC, y.TrangThai, y.ThoiDiemGui FROM dbo.YeuCauDatDichVu AS y JOIN dbo.DichVuDuLich AS d ON d.MaDichVu = y.MaDichVu JOIN dbo.NhaCungCapDichVu AS c ON c.MaNhaCungCap = d.MaNhaCungCap;
GO
