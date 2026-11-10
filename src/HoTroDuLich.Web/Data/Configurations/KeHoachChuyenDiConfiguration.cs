using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class KeHoachChuyenDiConfiguration : IEntityTypeConfiguration<KeHoachChuyenDi> {
 public void Configure(EntityTypeBuilder<KeHoachChuyenDi> e) {
 e.ToTable("KeHoachChuyenDi");
 e.HasKey(x => x.MaKeHoach);
 e.HasOne(x=>x.TaiKhoan).WithMany(x=>x.KeHoachChuyenDi).HasForeignKey(x => x.MaTaiKhoan).OnDelete(DeleteBehavior.Restrict);
 e.Property(x => x.TenKeHoach).HasMaxLength(150);
 e.ToTable(t => t.HasCheckConstraint("CK_KeHoachChuyenDi_TenKeHoach_KhongRong", "LEN(LTRIM([TenKeHoach])) > 0"));
 e.Property(x => x.NoiDen).HasMaxLength(200);
 e.ToTable(t => t.HasCheckConstraint("CK_KeHoachChuyenDi_NoiDen_KhongRong", "LEN(LTRIM([NoiDen])) > 0"));
 e.Property(x => x.NganSachDuKien).HasPrecision(18,2);
 e.ToTable(t => t.HasCheckConstraint("CK_KeHoachChuyenDi_NganSachDuKien", "[NganSachDuKien] >= 0"));
 e.Property(x => x.TrangThai).HasMaxLength(30).IsUnicode(false);
 e.ToTable(t => t.HasCheckConstraint("CK_KeHoachChuyenDi_TrangThai", "[TrangThai] IN ('Nhap','DaChapNhan')"));
 e.Property(x => x.RowVersion).IsRowVersion();
 e.ToTable(t => t.HasCheckConstraint("CK_KeHoachChuyenDi_Ngay", "NgayKetThuc >= NgayBatDau"));
 e.Property(x=>x.MaKeHoach).HasColumnType("bigint");
 e.Property(x=>x.MaTaiKhoan).HasColumnType("bigint");
 e.Property(x=>x.TenKeHoach).HasColumnType("nvarchar(150)");
 e.Property(x=>x.NoiDen).HasColumnType("nvarchar(200)");
 e.Property(x=>x.NgayBatDau).HasColumnType("date");
 e.Property(x=>x.NgayKetThuc).HasColumnType("date");
 e.Property(x=>x.NganSachDuKien).HasColumnType("decimal(18,2)");
 e.Property(x=>x.TrangThai).HasColumnType("varchar(30)");
 e.Property(x=>x.TrangThai).HasDefaultValueSql("'Nhap'", "DF_KeHoachChuyenDi_TrangThai");
 e.Property(x=>x.ThoiDiemTao).HasColumnType("datetime2(0)");
 e.Property(x=>x.ThoiDiemTao).HasDefaultValueSql("SYSUTCDATETIME()", "DF_KeHoachChuyenDi_ThoiDiemTao");
 e.Property(x=>x.ThoiDiemCapNhat).HasColumnType("datetime2(0)");
 e.Property(x=>x.ThoiDiemCapNhat).HasDefaultValueSql("SYSUTCDATETIME()", "DF_KeHoachChuyenDi_ThoiDiemCapNhat");
 e.HasIndex(x=>new {x.MaTaiKhoan,x.NgayBatDau},"IX_KeHoach_TaiKhoan").HasDatabaseName("IX_KeHoach_TaiKhoan").IsDescending(false,true).IncludeProperties("TenKeHoach","NoiDen","TrangThai");
 }
}
