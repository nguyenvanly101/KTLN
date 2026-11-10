using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class QuyetDinhXacMinhConfiguration : IEntityTypeConfiguration<QuyetDinhXacMinh> {
 public void Configure(EntityTypeBuilder<QuyetDinhXacMinh> e) {
 e.ToTable("QuyetDinhXacMinh");
 e.HasKey(x => x.MaQuyetDinh);
 e.HasOne(x=>x.HoSoXacMinhNhaCungCap).WithMany(x=>x.QuyetDinhXacMinh).HasForeignKey(x => x.MaHoSo).OnDelete(DeleteBehavior.Restrict);
 e.HasOne(x=>x.TaiKhoan).WithMany(x=>x.QuyetDinhXacMinh).HasForeignKey(x => x.MaAdminDuyet).OnDelete(DeleteBehavior.Restrict);
 e.ToTable(t => t.HasCheckConstraint("CK_QuyetDinhXacMinh_LanXuLy", "[LanXuLy] >= 1"));
 e.Property(x => x.KetQua).HasMaxLength(30).IsUnicode(false);
 e.ToTable(t => t.HasCheckConstraint("CK_QuyetDinhXacMinh_KetQua", "[KetQua] IN ('CanBoSung','DaXacMinh','TuChoi')"));
 e.Property(x => x.KetQuaXacMinh).HasMaxLength(500);
 e.Property(x => x.LyDo).HasMaxLength(500);
 e.ToTable(t => t.HasCheckConstraint("CK_QuyetDinhXacMinh_LyDo", "KetQua = 'DaXacMinh' OR (LyDo IS NOT NULL AND LEN(LTRIM(LyDo)) > 0)"));
 e.HasIndex(x => new { x.MaHoSo, x.LanXuLy }, "UX_QuyetDinhXacMinh_LanXuLy").IsUnique().HasDatabaseName("UX_QuyetDinhXacMinh_LanXuLy");
 e.Property(x=>x.MaQuyetDinh).HasColumnType("bigint");
 e.Property(x=>x.MaHoSo).HasColumnType("bigint");
 e.Property(x=>x.LanXuLy).HasColumnType("int");
 e.Property(x=>x.KetQua).HasColumnType("varchar(30)");
 e.Property(x=>x.KetQuaXacMinh).HasColumnType("nvarchar(500)");
 e.Property(x=>x.LyDo).HasColumnType("nvarchar(500)");
 e.Property(x=>x.MaAdminDuyet).HasColumnType("bigint");
 e.Property(x=>x.ThoiDiemDuyet).HasColumnType("datetime2(0)");
 e.Property(x=>x.ThoiDiemDuyet).HasDefaultValueSql("SYSUTCDATETIME()", "DF_QuyetDinhXacMinh_ThoiDiemDuyet");
 }
}
