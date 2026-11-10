using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class NhuCauDongHanhConfiguration : IEntityTypeConfiguration<NhuCauDongHanh> {
 public void Configure(EntityTypeBuilder<NhuCauDongHanh> e) {
 e.ToTable("NhuCauDongHanh");
 e.HasKey(x => x.MaNhuCau);
 e.HasOne(x=>x.TaiKhoan).WithMany(x=>x.NhuCauDongHanh).HasForeignKey(x => x.MaTaiKhoan).OnDelete(DeleteBehavior.Restrict);
 e.Property(x => x.NoiDung).HasMaxLength(2000);
 e.Property(x => x.DiemDen).HasMaxLength(200);
 e.ToTable(t => t.HasCheckConstraint("CK_NhuCauDongHanh_DiemDen_KhongRong", "LEN(LTRIM([DiemDen])) > 0"));
 e.Property(x => x.NganSachDuKien).HasPrecision(18,2);
 e.ToTable(t => t.HasCheckConstraint("CK_NhuCauDongHanh_NganSachDuKien", "[NganSachDuKien] >= 0"));
 e.Property(x => x.HinhThucDi).HasMaxLength(50);
 e.Property(x => x.MongMuonDongHanh).HasMaxLength(1000);
 e.Property(x => x.PhamViChiaSe).HasMaxLength(30).IsUnicode(false);
 e.ToTable(t => t.HasCheckConstraint("CK_NhuCauDongHanh_PhamViChiaSe", "[PhamViChiaSe] IN ('CongKhai','DaDangNhap','KhiKetNoi')"));
 e.Property(x => x.TrangThaiCongBo).HasMaxLength(30).IsUnicode(false);
 e.ToTable(t => t.HasCheckConstraint("CK_NhuCauDongHanh_TrangThaiCongBo", "[TrangThaiCongBo] IN ('DangCongBo','DungCongBo')"));
 e.Property(x => x.RowVersion).IsRowVersion();
 e.ToTable(t => t.HasCheckConstraint("CK_NhuCauDongHanh_Ngay", "NgayKetThuc >= NgayBatDau"));
 e.Property(x=>x.MaNhuCau).HasColumnType("bigint");
 e.Property(x=>x.MaTaiKhoan).HasColumnType("bigint");
 e.Property(x=>x.NoiDung).HasColumnType("nvarchar(2000)");
 e.Property(x=>x.DiemDen).HasColumnType("nvarchar(200)");
 e.Property(x=>x.NgayBatDau).HasColumnType("date");
 e.Property(x=>x.NgayKetThuc).HasColumnType("date");
 e.Property(x=>x.NganSachDuKien).HasColumnType("decimal(18,2)");
 e.Property(x=>x.HinhThucDi).HasColumnType("nvarchar(50)");
 e.Property(x=>x.MongMuonDongHanh).HasColumnType("nvarchar(1000)");
 e.Property(x=>x.PhamViChiaSe).HasColumnType("varchar(30)");
 e.Property(x=>x.ThoiDiemDang).HasColumnType("datetime2(0)");
 e.Property(x=>x.ThoiDiemDang).HasDefaultValueSql("SYSUTCDATETIME()", "DF_NhuCauDongHanh_ThoiDiemDang");
 e.Property(x=>x.TrangThaiCongBo).HasColumnType("varchar(30)");
 e.Property(x=>x.TrangThaiCongBo).HasDefaultValueSql("'DangCongBo'", "DF_NhuCauDongHanh_TrangThaiCongBo");
 e.HasIndex(x=>new {x.DiemDen,x.NgayBatDau},"IX_NhuCau_TimKiem").HasDatabaseName("IX_NhuCau_TimKiem").IncludeProperties("NgayKetThuc","PhamViChiaSe","MaTaiKhoan").HasFilter("TrangThaiCongBo = 'DangCongBo'");
 e.HasIndex(x=>new {x.MaTaiKhoan,x.TrangThaiCongBo},"IX_NhuCau_TaiKhoan").HasDatabaseName("IX_NhuCau_TaiKhoan");
 }
}
