using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class HoSoNguoiDungConfiguration : IEntityTypeConfiguration<HoSoNguoiDung> {
 public void Configure(EntityTypeBuilder<HoSoNguoiDung> e) {
 e.ToTable("HoSoNguoiDung");
 e.HasKey(x => x.MaHoSo);
 e.HasOne(x=>x.TaiKhoan).WithMany(x=>x.HoSoNguoiDung).HasForeignKey(x => x.MaTaiKhoan).OnDelete(DeleteBehavior.Restrict);
 e.Property(x => x.TenHienThi).HasMaxLength(100);
 e.ToTable(t => t.HasCheckConstraint("CK_HoSoNguoiDung_TenHienThi_KhongRong", "LEN(LTRIM([TenHienThi])) > 0"));
 e.Property(x => x.AnhDaiDien).HasMaxLength(400);
 e.Property(x => x.GioiThieu).HasMaxLength(1000);
 e.Property(x => x.NoiSinhSong).HasMaxLength(200);
 e.Property(x => x.SoThich).HasMaxLength(500);
 e.Property(x => x.ThongTinChiaSe).HasMaxLength(30).IsUnicode(false);
 e.ToTable(t => t.HasCheckConstraint("CK_HoSoNguoiDung_ThongTinChiaSe", "[ThongTinChiaSe] IN ('CongKhai','KhiKetNoi','RiengTu')"));
 e.Property(x => x.RowVersion).IsRowVersion();
 e.HasIndex(x => x.MaTaiKhoan, "UX_HoSoNguoiDung_MaTaiKhoan").IsUnique().HasDatabaseName("UX_HoSoNguoiDung_MaTaiKhoan");
 e.Property(x=>x.MaHoSo).HasColumnType("bigint");
 e.Property(x=>x.MaTaiKhoan).HasColumnType("bigint");
 e.Property(x=>x.TenHienThi).HasColumnType("nvarchar(100)");
 e.Property(x=>x.AnhDaiDien).HasColumnType("nvarchar(400)");
 e.Property(x=>x.GioiThieu).HasColumnType("nvarchar(1000)");
 e.Property(x=>x.NoiSinhSong).HasColumnType("nvarchar(200)");
 e.Property(x=>x.SoThich).HasColumnType("nvarchar(500)");
 e.Property(x=>x.ThongTinChiaSe).HasColumnType("varchar(30)");
 e.Property(x=>x.ThongTinChiaSe).HasDefaultValueSql("'RiengTu'", "DF_HoSoNguoiDung_ThongTinChiaSe");
 e.Property(x=>x.ThoiDiemCapNhat).HasColumnType("datetime2(0)");
 e.Property(x=>x.ThoiDiemCapNhat).HasDefaultValueSql("SYSUTCDATETIME()", "DF_HoSoNguoiDung_ThoiDiemCapNhat");
 }
}
