-- Danh gia hien ngay (bo buoc quan tri duyet truoc): danh gia va phan hoi dang "cho duyet" chuyen sang hien. Chay lai an toan.
SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.DanhGiaDichVu',N'U') IS NOT NULL
BEGIN
 UPDATE dbo.DanhGiaDichVu SET TrangThai=N'DaDuyet' WHERE TrangThai=N'ChoDuyet';
 UPDATE dbo.DanhGiaDichVu SET TrangThaiPhanHoi=N'DaDuyet' WHERE PhanHoiNCC IS NOT NULL AND TrangThaiPhanHoi=N'ChoDuyet';
END
