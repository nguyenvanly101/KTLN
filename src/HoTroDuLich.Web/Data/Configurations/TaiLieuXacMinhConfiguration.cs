using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class TaiLieuXacMinhConfiguration : IEntityTypeConfiguration<TaiLieuXacMinh> {
 public void Configure(EntityTypeBuilder<TaiLieuXacMinh> e) {
 e.ToTable("TaiLieuXacMinh");
 e.HasKey(x => x.MaTaiLieu);
 e.HasOne(x=>x.HoSoXacMinhNhaCungCap).WithMany(x=>x.TaiLieuXacMinh).HasForeignKey(x => x.MaHoSo).OnDelete(DeleteBehavior.Restrict);
 e.Property(x => x.TenTepGoc).HasMaxLength(255);
 e.ToTable(t => t.HasCheckConstraint("CK_TaiLieuXacMinh_TenTepGoc_KhongRong", "LEN(LTRIM([TenTepGoc])) > 0"));
 e.Property(x => x.DuongDan).HasMaxLength(400);
 e.Property(x => x.LoaiTep).HasMaxLength(30).IsUnicode(false);
 e.ToTable(t => t.HasCheckConstraint("CK_TaiLieuXacMinh_LoaiTep", "[LoaiTep] IN ('pdf','jpg','png')"));
 e.ToTable(t => t.HasCheckConstraint("CK_TaiLieuXacMinh_KichThuoc", "[KichThuoc] BETWEEN 1 AND 5242880"));
 e.ToTable(t => t.HasCheckConstraint("CK_TaiLieuXacMinh_LanNop", "[LanNop] >= 0"));
 e.HasIndex(x => x.DuongDan, "UX_TaiLieuXacMinh_DuongDan").IsUnique().HasDatabaseName("UX_TaiLieuXacMinh_DuongDan");
 e.Property(x=>x.MaTaiLieu).HasColumnType("bigint");
 e.Property(x=>x.MaHoSo).HasColumnType("bigint");
 e.Property(x=>x.TenTepGoc).HasColumnType("nvarchar(255)");
 e.Property(x=>x.DuongDan).HasColumnType("nvarchar(400)");
 e.Property(x=>x.LoaiTep).HasColumnType("varchar(30)");
 e.Property(x=>x.KichThuoc).HasColumnType("int");
 e.Property(x=>x.LanNop).HasColumnType("int");
 e.Property(x=>x.LanNop).HasSentinel(false).HasDefaultValueSql("0", "DF_TaiLieuXacMinh_LanNop");
 e.Property(x=>x.ThoiDiemTai).HasColumnType("datetime2(0)");
 e.Property(x=>x.ThoiDiemTai).HasDefaultValueSql("SYSUTCDATETIME()", "DF_TaiLieuXacMinh_ThoiDiemTai");
 e.HasIndex(x=>new {x.MaHoSo,x.LanNop},"IX_TaiLieuXacMinh_HoSo").HasDatabaseName("IX_TaiLieuXacMinh_HoSo");
 }
}
