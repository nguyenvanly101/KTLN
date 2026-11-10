-- Authorized product extension. No original rows are rewritten or assigned fabricated data.
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
EXEC @lockResult=sys.sp_getapplock @Resource=N'HoTroDuLich.Journey.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lockResult<0 THROW 51000,N'Không lấy được khóa nâng cấp hành trình.',1;
IF COL_LENGTH(N'dbo.HoatDongLichTrinh',N'ThoiLuongPhut') IS NULL
 EXEC(N'ALTER TABLE dbo.HoatDongLichTrinh ADD ThoiLuongPhut int NULL,DiChuyenTruocPhut int NULL,HoanThanhLuc datetime2(0) NULL;');
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_HoatDong_ThoiLuong')
 EXEC(N'ALTER TABLE dbo.HoatDongLichTrinh WITH CHECK ADD CONSTRAINT CK_HoatDong_ThoiLuong CHECK (ThoiLuongPhut IS NULL OR ThoiLuongPhut BETWEEN 1 AND 1440);');
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_HoatDong_DiChuyen')
 EXEC(N'ALTER TABLE dbo.HoatDongLichTrinh WITH CHECK ADD CONSTRAINT CK_HoatDong_DiChuyen CHECK (DiChuyenTruocPhut IS NULL OR DiChuyenTruocPhut BETWEEN 0 AND 720);');
IF OBJECT_ID(N'dbo.ChiaSeChuyenDi',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.ChiaSeChuyenDi (
  MaChiaSe bigint IDENTITY PRIMARY KEY,MaKeHoach bigint NOT NULL,TokenHash char(64) NOT NULL,SnapshotJson nvarchar(max) NOT NULL,
  HanSuDung datetime2(0) NOT NULL,DaThuHoi bit NOT NULL,ChiaSeNganSach bit NOT NULL,ChiaSeDichVu bit NOT NULL,ThoiDiemTao datetime2(0) NOT NULL,RowVersion rowversion NOT NULL,
  CONSTRAINT CK_ChiaSe_Han CHECK(HanSuDung>ThoiDiemTao),CONSTRAINT CK_ChiaSe_Snapshot CHECK(ISJSON(SnapshotJson)=1),
  CONSTRAINT FK_ChiaSeChuyenDi_MaKeHoach FOREIGN KEY(MaKeHoach) REFERENCES dbo.KeHoachChuyenDi(MaKeHoach));
 CREATE UNIQUE INDEX UX_ChiaSe_Token ON dbo.ChiaSeChuyenDi(TokenHash);
 CREATE INDEX IX_ChiaSe_KeHoach ON dbo.ChiaSeChuyenDi(MaKeHoach,ThoiDiemTao DESC);
END;
IF OBJECT_ID(N'dbo.DanhGiaDichVu',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.DanhGiaDichVu (
  MaDanhGia bigint IDENTITY PRIMARY KEY,MaYeuCau bigint NOT NULL,SoSao int NOT NULL,NoiDung nvarchar(2000) NOT NULL,TrangThai varchar(30) NOT NULL,
  LyDoKiemDuyet nvarchar(500) NULL,PhanHoiNCC nvarchar(2000) NULL,TrangThaiPhanHoi varchar(30) NOT NULL,LyDoPhanHoi nvarchar(500) NULL,
  ThoiDiemTao datetime2(0) NOT NULL,ThoiDiemCapNhat datetime2(0) NOT NULL,RowVersion rowversion NOT NULL,
  CONSTRAINT CK_DanhGia_Sao CHECK(SoSao BETWEEN 1 AND 5),CONSTRAINT CK_DanhGia_TrangThai CHECK(TrangThai IN ('ChoDuyet','DaDuyet','An')),
  CONSTRAINT CK_DanhGia_PhanHoi CHECK(TrangThaiPhanHoi IN ('ChoDuyet','DaDuyet','An')),
  CONSTRAINT CK_DanhGia_PhanHoiRong CHECK(PhanHoiNCC IS NOT NULL OR TrangThaiPhanHoi='ChoDuyet'),
  CONSTRAINT FK_DanhGiaDichVu_MaYeuCau FOREIGN KEY(MaYeuCau) REFERENCES dbo.YeuCauDatDichVu(MaYeuCau));
 CREATE UNIQUE INDEX UX_DanhGia_YeuCau ON dbo.DanhGiaDichVu(MaYeuCau);
 CREATE INDEX IX_DanhGia_TrangThai ON dbo.DanhGiaDichVu(TrangThai,ThoiDiemCapNhat DESC);
END;
IF OBJECT_ID(N'dbo.LichPhucVuDichVu',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.LichPhucVuDichVu (
  MaLich bigint IDENTITY PRIMARY KEY,MaDichVu bigint NOT NULL,Ngay date NOT NULL,TiepNhan bit NOT NULL,SoLuongThamKhao int NULL,GhiChu nvarchar(300) NULL,ThoiDiemCapNhat datetime2(0) NOT NULL,RowVersion rowversion NOT NULL,
  CONSTRAINT CK_LichPhucVu_SoLuong CHECK(SoLuongThamKhao IS NULL OR SoLuongThamKhao BETWEEN 1 AND 10000),
  CONSTRAINT FK_LichPhucVuDichVu_MaDichVu FOREIGN KEY(MaDichVu) REFERENCES dbo.DichVuDuLich(MaDichVu));
 CREATE UNIQUE INDEX UX_LichPhucVu_Ngay ON dbo.LichPhucVuDichVu(MaDichVu,Ngay);
END;
COMMIT TRANSACTION;
