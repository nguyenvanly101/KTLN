using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class KetQuaCungCapDichVuConfiguration : IEntityTypeConfiguration<KetQuaCungCapDichVu> {
 public void Configure(EntityTypeBuilder<KetQuaCungCapDichVu> e) {
 e.ToTable("KetQuaCungCapDichVu");
 e.HasKey(x => x.MaKetQua);
 e.HasOne(x=>x.YeuCauDatDichVu).WithMany(x=>x.KetQuaCungCapDichVu).HasForeignKey(x => x.MaYeuCau).OnDelete(DeleteBehavior.Restrict);
 e.Property(x => x.HangMuc).HasMaxLength(200);
 e.ToTable(t => t.HasCheckConstraint("CK_KetQuaCungCapDichVu_HangMuc_KhongRong", "LEN(LTRIM([HangMuc])) > 0"));
 e.Property(x => x.KetQuaThucHien).HasMaxLength(30).IsUnicode(false);
 e.ToTable(t => t.HasCheckConstraint("CK_KetQuaCungCapDichVu_KetQuaThucHien", "[KetQuaThucHien] IN ('DayDu','MotPhan','KhongThucHien','KhachKhongDen','ChuaXacDinh')"));
 e.Property(x => x.GhiChu).HasMaxLength(1000);
 e.Property(x => x.NoiDungPhanAnh).HasMaxLength(2000);
 e.Property(x => x.TrangThaiKiemTra).HasMaxLength(30).IsUnicode(false);
 e.ToTable(t => t.HasCheckConstraint("CK_KetQuaCungCapDichVu_TrangThaiKiemTra", "[TrangThaiKiemTra] IN ('DaGhiNhan','ChoKiemTra','DaKiemTra')"));
 e.Property(x => x.RowVersion).IsRowVersion();
 e.ToTable(t => t.HasCheckConstraint("CK_KetQuaCungCapDichVu_PhanAnh", "(NoiDungPhanAnh IS NULL AND ThoiDiemPhanAnh IS NULL) OR (NoiDungPhanAnh IS NOT NULL AND ThoiDiemPhanAnh IS NOT NULL)"));
 e.ToTable(t => t.HasCheckConstraint("CK_KetQuaCungCapDichVu_ChoKiemTra", "TrangThaiKiemTra <> 'ChoKiemTra' OR NoiDungPhanAnh IS NOT NULL OR KetQuaThucHien = 'ChuaXacDinh'"));
 e.ToTable(t => t.HasCheckConstraint("CK_KetQuaCungCapDichVu_ChuaXacDinh", "KetQuaThucHien <> 'ChuaXacDinh' OR TrangThaiKiemTra = 'ChoKiemTra'"));
 e.ToTable(t => t.HasCheckConstraint("CK_KetQuaCungCapDichVu_DaKiemTra", "(TrangThaiKiemTra = 'DaKiemTra' AND ThoiDiemKiemTra IS NOT NULL) OR (TrangThaiKiemTra <> 'DaKiemTra' AND ThoiDiemKiemTra IS NULL)"));
 e.HasIndex(x => new { x.MaYeuCau, x.HangMuc }, "UX_KetQuaCungCapDichVu_HangMuc").IsUnique().HasDatabaseName("UX_KetQuaCungCapDichVu_HangMuc");
 e.Property(x=>x.MaKetQua).HasColumnType("bigint");
 e.Property(x=>x.MaYeuCau).HasColumnType("bigint");
 e.Property(x=>x.HangMuc).HasColumnType("nvarchar(200)");
 e.Property(x=>x.KetQuaThucHien).HasColumnType("varchar(30)");
 e.Property(x=>x.GhiChu).HasColumnType("nvarchar(1000)");
 e.Property(x=>x.ThoiDiemGhi).HasColumnType("datetime2(0)");
 e.Property(x=>x.ThoiDiemGhi).HasDefaultValueSql("SYSUTCDATETIME()", "DF_KetQuaCungCapDichVu_ThoiDiemGhi");
 e.Property(x=>x.NoiDungPhanAnh).HasColumnType("nvarchar(2000)");
 e.Property(x=>x.ThoiDiemPhanAnh).HasColumnType("datetime2(0)");
 e.Property(x=>x.TrangThaiKiemTra).HasColumnType("varchar(30)");
 e.Property(x=>x.TrangThaiKiemTra).HasDefaultValueSql("'DaGhiNhan'", "DF_KetQuaCungCapDichVu_TrangThaiKiemTra");
 e.Property(x=>x.ThoiDiemKiemTra).HasColumnType("datetime2(0)");
 e.HasIndex(x=>x.ThoiDiemPhanAnh,"IX_KetQua_ChoKiemTra").HasDatabaseName("IX_KetQua_ChoKiemTra").IncludeProperties("MaYeuCau").HasFilter("TrangThaiKiemTra = 'ChoKiemTra'");
 }
}
