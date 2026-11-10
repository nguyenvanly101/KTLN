-- Product extension approved after the FullText baseline: optional trip association.
-- No historical request is assigned automatically. Re-runnable; never deletes data.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lockResult int;
EXEC @lockResult = sys.sp_getapplock @Resource = N'HoTroDuLich.TripAssociation.v1',
    @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 30000;
IF @lockResult < 0 THROW 51000, N'Không lấy được khóa cập nhật liên kết chuyến đi.', 1;

IF COL_LENGTH(N'dbo.YeuCauDatDichVu', N'MaKeHoach') IS NULL
    EXEC(N'ALTER TABLE dbo.YeuCauDatDichVu ADD MaKeHoach bigint NULL;');
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_YeuCauDatDichVu_MaKeHoach')
    EXEC(N'ALTER TABLE dbo.YeuCauDatDichVu WITH CHECK ADD CONSTRAINT FK_YeuCauDatDichVu_MaKeHoach FOREIGN KEY (MaKeHoach) REFERENCES dbo.KeHoachChuyenDi(MaKeHoach) ON DELETE NO ACTION;');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.YeuCauDatDichVu') AND name = N'IX_YeuCau_KeHoach')
    EXEC(N'CREATE INDEX IX_YeuCau_KeHoach ON dbo.YeuCauDatDichVu(MaKeHoach, ThoiDiemGui DESC) INCLUDE (MaDichVu, TrangThai);');
COMMIT TRANSACTION;
