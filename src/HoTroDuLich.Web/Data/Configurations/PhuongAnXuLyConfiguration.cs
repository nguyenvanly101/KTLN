using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class PhuongAnXuLyConfiguration : IEntityTypeConfiguration<PhuongAnXuLy> {
 public void Configure(EntityTypeBuilder<PhuongAnXuLy> e) {
 e.ToTable("PhuongAnXuLy");
 e.HasKey(x => x.MaPhuongAn);
 e.HasOne(x=>x.TinhHuongPhatSinh).WithMany(x=>x.PhuongAnXuLy).HasForeignKey(x => x.MaTinhHuong).OnDelete(DeleteBehavior.Restrict);
 e.Property(x => x.NoiDung).HasMaxLength(1000);
 e.HasIndex(x => x.MaTinhHuong, "UX_PhuongAnXuLy_HienHanh").IsUnique().HasDatabaseName("UX_PhuongAnXuLy_HienHanh").HasFilter("LaHienHanh = 1");
 e.Property(x=>x.MaPhuongAn).HasColumnType("bigint");
 e.Property(x=>x.MaTinhHuong).HasColumnType("bigint");
 e.Property(x=>x.NoiDung).HasColumnType("nvarchar(1000)");
 e.Property(x=>x.ThoiDiemChon).HasColumnType("datetime2(0)");
 e.Property(x=>x.ThoiDiemChon).HasDefaultValueSql("SYSUTCDATETIME()", "DF_PhuongAnXuLy_ThoiDiemChon");
 e.Property(x=>x.LaHienHanh).HasColumnType("bit");
 e.Property(x=>x.LaHienHanh).HasSentinel(true).HasDefaultValueSql("1", "DF_PhuongAnXuLy_LaHienHanh");
 e.HasIndex(x=>new {x.MaTinhHuong,x.ThoiDiemChon},"IX_PhuongAn_TinhHuong").HasDatabaseName("IX_PhuongAn_TinhHuong").IsDescending(false,true);
 }
}
