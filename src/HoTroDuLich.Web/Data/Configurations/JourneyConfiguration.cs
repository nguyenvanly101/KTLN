using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoTroDuLich.Web.Data.Configurations;

public sealed class ActivityJourneyConfiguration : IEntityTypeConfiguration<HoatDongLichTrinh>
{
    public void Configure(EntityTypeBuilder<HoatDongLichTrinh> e) => e.ToTable("HoatDongLichTrinh", t =>
    {
        t.HasCheckConstraint("CK_HoatDong_ThoiLuong", "ThoiLuongPhut IS NULL OR ThoiLuongPhut BETWEEN 1 AND 1440");
        t.HasCheckConstraint("CK_HoatDong_DiChuyen", "DiChuyenTruocPhut IS NULL OR DiChuyenTruocPhut BETWEEN 0 AND 720");
    });
}
public sealed class TripShareConfiguration : IEntityTypeConfiguration<ChiaSeChuyenDi>
{
    public void Configure(EntityTypeBuilder<ChiaSeChuyenDi> e)
    {
        e.ToTable("ChiaSeChuyenDi", t => { t.HasCheckConstraint("CK_ChiaSe_Han", "HanSuDung > ThoiDiemTao"); t.HasCheckConstraint("CK_ChiaSe_Snapshot", "ISJSON(SnapshotJson)=1"); });
        e.HasKey(x => x.MaChiaSe);
        e.Property(x => x.TokenHash).HasColumnType("char(64)");
        e.Property(x => x.SnapshotJson).HasColumnType("nvarchar(max)");
        e.Property(x => x.RowVersion).IsRowVersion();
        e.HasOne<KeHoachChuyenDi>().WithMany().HasForeignKey(x => x.MaKeHoach);
        e.HasIndex(x => x.TokenHash, "UX_ChiaSe_Token").IsUnique();
        e.HasIndex(x => new { x.MaKeHoach, x.ThoiDiemTao }, "IX_ChiaSe_KeHoach").IsDescending(false, true);
    }
}
public sealed class ServiceReviewConfiguration : IEntityTypeConfiguration<DanhGiaDichVu>
{
    public void Configure(EntityTypeBuilder<DanhGiaDichVu> e)
    {
        e.ToTable("DanhGiaDichVu", t =>
        {
            t.HasCheckConstraint("CK_DanhGia_Sao", "SoSao BETWEEN 1 AND 5");
            t.HasCheckConstraint("CK_DanhGia_TrangThai", "TrangThai IN ('ChoDuyet','DaDuyet','An')");
            t.HasCheckConstraint("CK_DanhGia_PhanHoi", "TrangThaiPhanHoi IN ('ChoDuyet','DaDuyet','An')");
            t.HasCheckConstraint("CK_DanhGia_PhanHoiRong", "PhanHoiNCC IS NOT NULL OR TrangThaiPhanHoi='ChoDuyet'");
        });
        e.HasKey(x => x.MaDanhGia);
        e.Property(x => x.NoiDung).HasMaxLength(2000);
        e.Property(x => x.PhanHoiNCC).HasMaxLength(2000);
        e.Property(x => x.LyDoKiemDuyet).HasMaxLength(500);
        e.Property(x => x.LyDoPhanHoi).HasMaxLength(500);
        e.Property(x => x.TrangThai).HasColumnType("varchar(30)");
        e.Property(x => x.TrangThaiPhanHoi).HasColumnType("varchar(30)");
        e.Property(x => x.RowVersion).IsRowVersion();
        e.HasOne<YeuCauDatDichVu>().WithMany().HasForeignKey(x => x.MaYeuCau);
        e.HasIndex(x => x.MaYeuCau, "UX_DanhGia_YeuCau").IsUnique();
        e.HasIndex(x => new { x.TrangThai, x.ThoiDiemCapNhat }, "IX_DanhGia_TrangThai").IsDescending(false, true);
    }
}
public sealed class ServiceCalendarConfiguration : IEntityTypeConfiguration<LichPhucVuDichVu>
{
    public void Configure(EntityTypeBuilder<LichPhucVuDichVu> e)
    {
        e.ToTable("LichPhucVuDichVu", t => t.HasCheckConstraint("CK_LichPhucVu_SoLuong", "SoLuongThamKhao IS NULL OR SoLuongThamKhao BETWEEN 1 AND 10000"));
        e.HasKey(x => x.MaLich);
        e.Property(x => x.GhiChu).HasMaxLength(300);
        e.Property(x => x.RowVersion).IsRowVersion();
        e.HasOne<DichVuDuLich>().WithMany().HasForeignKey(x => x.MaDichVu);
        e.HasIndex(x => new { x.MaDichVu, x.Ngay }, "UX_LichPhucVu_Ngay").IsUnique();
    }
}
