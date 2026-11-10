using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class NhatKyThaoTacConfiguration : IEntityTypeConfiguration<NhatKyThaoTac> {
 public void Configure(EntityTypeBuilder<NhatKyThaoTac> e) {
 e.ToTable("NhatKyThaoTac");
 e.HasKey(x => x.MaNhatKy);
 e.HasOne(x=>x.TaiKhoan).WithMany(x=>x.NhatKyThaoTac).HasForeignKey(x => x.MaTaiKhoan).OnDelete(DeleteBehavior.Restrict);
 e.Property(x => x.LoaiSuKien).HasMaxLength(40);
 e.ToTable(t => t.HasCheckConstraint("CK_NhatKyThaoTac_LoaiSuKien_KhongRong", "LEN(LTRIM([LoaiSuKien])) > 0"));
 e.Property(x => x.DoiTuong).HasMaxLength(40);
 e.Property(x => x.TrangThaiTruoc).HasMaxLength(200);
 e.Property(x => x.TrangThaiSau).HasMaxLength(200);
 e.Property(x => x.LyDo).HasMaxLength(500);
 e.Property(x => x.DiaChiIP).HasMaxLength(45);
 e.Property(x=>x.MaNhatKy).HasColumnType("bigint");
 e.Property(x=>x.MaTaiKhoan).HasColumnType("bigint");
 e.Property(x=>x.LoaiSuKien).HasColumnType("nvarchar(40)");
 e.Property(x=>x.DoiTuong).HasColumnType("nvarchar(40)");
 e.Property(x=>x.MaDoiTuong).HasColumnType("bigint");
 e.Property(x=>x.TrangThaiTruoc).HasColumnType("nvarchar(200)");
 e.Property(x=>x.TrangThaiSau).HasColumnType("nvarchar(200)");
 e.Property(x=>x.LyDo).HasColumnType("nvarchar(500)");
 e.Property(x=>x.DiaChiIP).HasColumnType("nvarchar(45)");
 e.Property(x=>x.ThoiDiem).HasColumnType("datetime2(0)");
 e.Property(x=>x.ThoiDiem).HasDefaultValueSql("SYSUTCDATETIME()", "DF_NhatKyThaoTac_ThoiDiem");
 e.HasIndex(x=>x.ThoiDiem,"IX_NhatKy_ThoiDiem").HasDatabaseName("IX_NhatKy_ThoiDiem").IsDescending(true).IncludeProperties("LoaiSuKien","MaTaiKhoan");
 e.HasIndex(x=>new {x.DoiTuong,x.MaDoiTuong,x.ThoiDiem},"IX_NhatKy_DoiTuong").HasDatabaseName("IX_NhatKy_DoiTuong").IsDescending(false,false,true).HasFilter("DoiTuong IS NOT NULL");
 e.HasIndex(x=>new {x.MaTaiKhoan,x.ThoiDiem},"IX_NhatKy_TaiKhoan").HasDatabaseName("IX_NhatKy_TaiKhoan").IsDescending(false,true).HasFilter("MaTaiKhoan IS NOT NULL");
 }
}
