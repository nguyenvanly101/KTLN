using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class GiaiTrinhNhaCungCapConfiguration : IEntityTypeConfiguration<GiaiTrinhNhaCungCap> {
 public void Configure(EntityTypeBuilder<GiaiTrinhNhaCungCap> e) {
 e.ToTable("GiaiTrinhNhaCungCap");
 e.HasKey(x => x.MaGiaiTrinh);
 e.HasOne(x=>x.QuyetDinhQuyenHoatDong).WithMany(x=>x.GiaiTrinhNhaCungCap).HasForeignKey(x => x.MaQuyetDinh).OnDelete(DeleteBehavior.Restrict);
 e.HasOne(x=>x.TaiKhoan).WithMany(x=>x.GiaiTrinhNhaCungCap).HasForeignKey(x => x.MaAdminXemXet).OnDelete(DeleteBehavior.Restrict);
 e.Property(x => x.NoiDung).HasMaxLength(2000);
 e.Property(x => x.TrangThai).HasMaxLength(30).IsUnicode(false);
 e.ToTable(t => t.HasCheckConstraint("CK_GiaiTrinhNhaCungCap_TrangThai", "[TrangThai] IN ('ChoXemXet','DaXemXet')"));
 e.Property(x => x.KetQuaXemXet).HasMaxLength(500);
 e.ToTable(t => t.HasCheckConstraint("CK_GiaiTrinhNhaCungCap_XemXet", "(TrangThai = 'ChoXemXet' AND MaAdminXemXet IS NULL AND ThoiDiemXemXet IS NULL) OR (TrangThai = 'DaXemXet' AND MaAdminXemXet IS NOT NULL AND ThoiDiemXemXet IS NOT NULL AND KetQuaXemXet IS NOT NULL AND LEN(LTRIM(KetQuaXemXet)) > 0)"));
 e.Property(x=>x.MaGiaiTrinh).HasColumnType("bigint");
 e.Property(x=>x.MaQuyetDinh).HasColumnType("bigint");
 e.Property(x=>x.NoiDung).HasColumnType("nvarchar(2000)");
 e.Property(x=>x.ThoiDiemGui).HasColumnType("datetime2(0)");
 e.Property(x=>x.ThoiDiemGui).HasDefaultValueSql("SYSUTCDATETIME()", "DF_GiaiTrinhNhaCungCap_ThoiDiemGui");
 e.Property(x=>x.TrangThai).HasColumnType("varchar(30)");
 e.Property(x=>x.TrangThai).HasDefaultValueSql("'ChoXemXet'", "DF_GiaiTrinhNhaCungCap_TrangThai");
 e.Property(x=>x.KetQuaXemXet).HasColumnType("nvarchar(500)");
 e.Property(x=>x.MaAdminXemXet).HasColumnType("bigint");
 e.Property(x=>x.ThoiDiemXemXet).HasColumnType("datetime2(0)");
 e.HasIndex(x=>x.MaQuyetDinh,"IX_GiaiTrinh_QuyetDinh").HasDatabaseName("IX_GiaiTrinh_QuyetDinh");
 e.HasIndex(x=>x.ThoiDiemGui,"IX_GiaiTrinh_ChoXemXet").HasDatabaseName("IX_GiaiTrinh_ChoXemXet").IncludeProperties("MaQuyetDinh").HasFilter("TrangThai = 'ChoXemXet'");
 }
}
