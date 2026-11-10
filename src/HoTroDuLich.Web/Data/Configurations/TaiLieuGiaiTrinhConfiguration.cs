using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class TaiLieuGiaiTrinhConfiguration : IEntityTypeConfiguration<TaiLieuGiaiTrinh> {
 public void Configure(EntityTypeBuilder<TaiLieuGiaiTrinh> e) {
 e.ToTable("TaiLieuGiaiTrinh");
 e.HasKey(x => x.MaTaiLieu);
 e.HasOne(x=>x.GiaiTrinhNhaCungCap).WithMany(x=>x.TaiLieuGiaiTrinh).HasForeignKey(x => x.MaGiaiTrinh).OnDelete(DeleteBehavior.Restrict);
 e.Property(x => x.TenTepGoc).HasMaxLength(255);
 e.ToTable(t => t.HasCheckConstraint("CK_TaiLieuGiaiTrinh_TenTepGoc_KhongRong", "LEN(LTRIM([TenTepGoc])) > 0"));
 e.Property(x => x.DuongDan).HasMaxLength(400);
 e.Property(x => x.LoaiTep).HasMaxLength(30).IsUnicode(false);
 e.ToTable(t => t.HasCheckConstraint("CK_TaiLieuGiaiTrinh_LoaiTep", "[LoaiTep] IN ('pdf','jpg','png')"));
 e.ToTable(t => t.HasCheckConstraint("CK_TaiLieuGiaiTrinh_KichThuoc", "[KichThuoc] BETWEEN 1 AND 5242880"));
 e.HasIndex(x => x.DuongDan, "UX_TaiLieuGiaiTrinh_DuongDan").IsUnique().HasDatabaseName("UX_TaiLieuGiaiTrinh_DuongDan");
 e.Property(x=>x.MaTaiLieu).HasColumnType("bigint");
 e.Property(x=>x.MaGiaiTrinh).HasColumnType("bigint");
 e.Property(x=>x.TenTepGoc).HasColumnType("nvarchar(255)");
 e.Property(x=>x.DuongDan).HasColumnType("nvarchar(400)");
 e.Property(x=>x.LoaiTep).HasColumnType("varchar(30)");
 e.Property(x=>x.KichThuoc).HasColumnType("int");
 e.Property(x=>x.ThoiDiemTai).HasColumnType("datetime2(0)");
 e.Property(x=>x.ThoiDiemTai).HasDefaultValueSql("SYSUTCDATETIME()", "DF_TaiLieuGiaiTrinh_ThoiDiemTai");
 e.HasIndex(x=>x.MaGiaiTrinh,"IX_TaiLieuGiaiTrinh_GiaiTrinh").HasDatabaseName("IX_TaiLieuGiaiTrinh_GiaiTrinh");
 }
}
