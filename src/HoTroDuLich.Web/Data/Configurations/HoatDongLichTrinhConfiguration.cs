using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class HoatDongLichTrinhConfiguration : IEntityTypeConfiguration<HoatDongLichTrinh> {
 public void Configure(EntityTypeBuilder<HoatDongLichTrinh> e) {
 e.ToTable("HoatDongLichTrinh");
 e.HasKey(x => x.MaHoatDong);
 e.HasOne(x=>x.KeHoachChuyenDi).WithMany(x=>x.HoatDongLichTrinh).HasForeignKey(x => x.MaKeHoach).OnDelete(DeleteBehavior.Restrict);
 e.HasOne(x=>x.DiaDiemDuLich).WithMany(x=>x.HoatDongLichTrinh).HasForeignKey(x => x.MaDiaDiem).OnDelete(DeleteBehavior.Restrict);
 e.HasOne(x=>x.DichVuDuLich).WithMany(x=>x.HoatDongLichTrinh).HasForeignKey(x => x.MaDichVu).OnDelete(DeleteBehavior.Restrict);
 e.ToTable(t => t.HasCheckConstraint("CK_HoatDongLichTrinh_ThuTu", "[ThuTu] >= 1"));
 e.Property(x => x.NoiDung).HasMaxLength(300);
 e.Property(x => x.ChiPhiDuKien).HasPrecision(18,2);
 e.ToTable(t => t.HasCheckConstraint("CK_HoatDongLichTrinh_ChiPhiDuKien", "[ChiPhiDuKien] >= 0"));
 e.ToTable(t => t.HasCheckConstraint("CK_HoatDongLichTrinh_MotMuc", "MaDiaDiem IS NULL OR MaDichVu IS NULL"));
 e.Property(x=>x.MaHoatDong).HasColumnType("bigint");
 e.Property(x=>x.MaKeHoach).HasColumnType("bigint");
 e.Property(x=>x.Ngay).HasColumnType("date");
 e.Property(x=>x.Gio).HasColumnType("time(0)");
 e.Property(x=>x.ThuTu).HasColumnType("int");
 e.Property(x=>x.MaDiaDiem).HasColumnType("bigint");
 e.Property(x=>x.MaDichVu).HasColumnType("bigint");
 e.Property(x=>x.NoiDung).HasColumnType("nvarchar(300)");
 e.Property(x=>x.ChiPhiDuKien).HasColumnType("decimal(18,2)");
 e.HasIndex(x=>new {x.MaKeHoach,x.Ngay,x.ThuTu},"IX_HoatDong_KeHoach").HasDatabaseName("IX_HoatDong_KeHoach");
 e.HasIndex(x=>x.MaDiaDiem,"IX_HoatDong_DiaDiem").HasDatabaseName("IX_HoatDong_DiaDiem").HasFilter("MaDiaDiem IS NOT NULL");
 e.HasIndex(x=>x.MaDichVu,"IX_HoatDong_DichVu").HasDatabaseName("IX_HoatDong_DichVu").HasFilter("MaDichVu IS NOT NULL");
 }
}
