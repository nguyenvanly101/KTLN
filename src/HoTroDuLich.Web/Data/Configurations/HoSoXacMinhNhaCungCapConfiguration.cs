using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class HoSoXacMinhNhaCungCapConfiguration : IEntityTypeConfiguration<HoSoXacMinhNhaCungCap> {
 public void Configure(EntityTypeBuilder<HoSoXacMinhNhaCungCap> e) {
 e.ToTable("HoSoXacMinhNhaCungCap");
 e.HasKey(x => x.MaHoSo);
 e.HasOne(x=>x.NhaCungCapDichVu).WithMany(x=>x.HoSoXacMinhNhaCungCap).HasForeignKey(x => x.MaNhaCungCap).OnDelete(DeleteBehavior.Restrict);
 e.Property(x => x.NoiDungDangKy).HasMaxLength(2000);
 e.Property(x => x.NguoiDaiDien).HasMaxLength(100);
 e.ToTable(t => t.HasCheckConstraint("CK_HoSoXacMinhNhaCungCap_NguoiDaiDien_KhongRong", "LEN(LTRIM([NguoiDaiDien])) > 0"));
 e.ToTable(t => t.HasCheckConstraint("CK_HoSoXacMinhNhaCungCap_SoLanBoSung", "[SoLanBoSung] >= 0"));
 e.Property(x => x.TrangThaiXuLy).HasMaxLength(30).IsUnicode(false);
 e.ToTable(t => t.HasCheckConstraint("CK_HoSoXacMinhNhaCungCap_TrangThaiXuLy", "[TrangThaiXuLy] IN ('ChoXacMinh','CanBoSung','DaXacMinh','TuChoi')"));
 e.Property(x => x.RowVersion).IsRowVersion();
 e.HasIndex(x => x.MaNhaCungCap, "UX_HoSoXacMinh_DangXuLy").IsUnique().HasDatabaseName("UX_HoSoXacMinh_DangXuLy").HasFilter("TrangThaiXuLy IN ('ChoXacMinh', 'CanBoSung')");
 e.Property(x=>x.MaHoSo).HasColumnType("bigint");
 e.Property(x=>x.MaNhaCungCap).HasColumnType("bigint");
 e.Property(x=>x.NoiDungDangKy).HasColumnType("nvarchar(2000)");
 e.Property(x=>x.NguoiDaiDien).HasColumnType("nvarchar(100)");
 e.Property(x=>x.ThoiDiemNop).HasColumnType("datetime2(0)");
 e.Property(x=>x.ThoiDiemNop).HasDefaultValueSql("SYSUTCDATETIME()", "DF_HoSoXacMinhNhaCungCap_ThoiDiemNop");
 e.Property(x=>x.SoLanBoSung).HasColumnType("int");
 e.Property(x=>x.SoLanBoSung).HasSentinel(false).HasDefaultValueSql("0", "DF_HoSoXacMinhNhaCungCap_SoLanBoSung");
 e.Property(x=>x.TrangThaiXuLy).HasColumnType("varchar(30)");
 e.Property(x=>x.TrangThaiXuLy).HasDefaultValueSql("'ChoXacMinh'", "DF_HoSoXacMinhNhaCungCap_TrangThaiXuLy");
 e.HasIndex(x=>new {x.TrangThaiXuLy,x.ThoiDiemNop},"IX_HoSoXacMinh_HangDoi").HasDatabaseName("IX_HoSoXacMinh_HangDoi").IncludeProperties("MaNhaCungCap");
 e.HasIndex(x=>new {x.MaNhaCungCap,x.ThoiDiemNop},"IX_HoSoXacMinh_NhaCungCap").HasDatabaseName("IX_HoSoXacMinh_NhaCungCap").IsDescending(false,true);
 }
}
