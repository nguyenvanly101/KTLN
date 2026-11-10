using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class TinhHuongHoatDongConfiguration : IEntityTypeConfiguration<TinhHuongHoatDong> {
 public void Configure(EntityTypeBuilder<TinhHuongHoatDong> e) {
 e.ToTable("TinhHuongHoatDong");
 e.HasKey(x => new { x.MaTinhHuong, x.MaHoatDong });
 e.HasOne(x=>x.TinhHuongPhatSinh).WithMany(x=>x.TinhHuongHoatDong).HasForeignKey(x => x.MaTinhHuong).OnDelete(DeleteBehavior.Restrict);
 e.HasOne(x=>x.HoatDongLichTrinh).WithMany(x=>x.TinhHuongHoatDong).HasForeignKey(x => x.MaHoatDong).OnDelete(DeleteBehavior.Restrict);
 e.Property(x=>x.MaTinhHuong).HasColumnType("bigint");
 e.Property(x=>x.MaHoatDong).HasColumnType("bigint");
 e.HasIndex(x=>x.MaHoatDong,"IX_TinhHuongHoatDong_HoatDong").HasDatabaseName("IX_TinhHuongHoatDong_HoatDong");
 }
}
