using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class QuyetDinhQuyenHoatDongConfiguration : IEntityTypeConfiguration<QuyetDinhQuyenHoatDong> {
 public void Configure(EntityTypeBuilder<QuyetDinhQuyenHoatDong> e) {
 e.ToTable("QuyetDinhQuyenHoatDong");
 e.HasKey(x => x.MaQuyetDinh);
 e.HasOne(x=>x.NhaCungCapDichVu).WithMany(x=>x.QuyetDinhQuyenHoatDong).HasForeignKey(x => x.MaNhaCungCap).OnDelete(DeleteBehavior.Restrict);
 e.HasOne(x=>x.TaiKhoan).WithMany(x=>x.QuyetDinhQuyenHoatDong).HasForeignKey(x => x.MaAdmin).OnDelete(DeleteBehavior.Restrict);
 e.Property(x => x.TrangThaiTruoc).HasMaxLength(30).IsUnicode(false);
 e.ToTable(t => t.HasCheckConstraint("CK_QuyetDinhQuyenHoatDong_TrangThaiTruoc", "[TrangThaiTruoc] IN ('DangHoatDong','TamDinhChi','DaThuHoi')"));
 e.Property(x => x.TrangThaiSau).HasMaxLength(30).IsUnicode(false);
 e.ToTable(t => t.HasCheckConstraint("CK_QuyetDinhQuyenHoatDong_TrangThaiSau", "[TrangThaiSau] IN ('DangHoatDong','TamDinhChi','DaThuHoi')"));
 e.Property(x => x.LyDo).HasMaxLength(500);
 e.Property(x => x.CanCu).HasMaxLength(500);
 e.ToTable(t => t.HasCheckConstraint("CK_QuyetDinhQuyenHoatDong_LyDoCanCu", "(LyDo IS NOT NULL AND LEN(LTRIM(LyDo)) > 0) AND (CanCu IS NOT NULL AND LEN(LTRIM(CanCu)) > 0)"));
 e.ToTable(t => t.HasCheckConstraint("CK_QuyetDinhQuyenHoatDong_DoiTrangThai", "TrangThaiTruoc <> TrangThaiSau"));
 e.ToTable(t => t.HasCheckConstraint("CK_QuyetDinhQuyenHoatDong_ThoiHan", "ThoiHan IS NULL OR TrangThaiSau = 'TamDinhChi'"));
 e.Property(x=>x.MaQuyetDinh).HasColumnType("bigint");
 e.Property(x=>x.MaNhaCungCap).HasColumnType("bigint");
 e.Property(x=>x.TrangThaiTruoc).HasColumnType("varchar(30)");
 e.Property(x=>x.TrangThaiSau).HasColumnType("varchar(30)");
 e.Property(x=>x.LyDo).HasColumnType("nvarchar(500)");
 e.Property(x=>x.CanCu).HasColumnType("nvarchar(500)");
 e.Property(x=>x.ThoiHan).HasColumnType("date");
 e.Property(x=>x.MaAdmin).HasColumnType("bigint");
 e.Property(x=>x.ThoiDiem).HasColumnType("datetime2(0)");
 e.Property(x=>x.ThoiDiem).HasDefaultValueSql("SYSUTCDATETIME()", "DF_QuyetDinhQuyenHoatDong_ThoiDiem");
 e.HasIndex(x=>new {x.MaNhaCungCap,x.ThoiDiem},"IX_QuyetDinhQuyen_NhaCungCap").HasDatabaseName("IX_QuyetDinhQuyen_NhaCungCap").IsDescending(false,true);
 }
}
