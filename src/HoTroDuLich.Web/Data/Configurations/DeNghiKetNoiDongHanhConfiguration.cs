using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class DeNghiKetNoiDongHanhConfiguration : IEntityTypeConfiguration<DeNghiKetNoiDongHanh> {
 public void Configure(EntityTypeBuilder<DeNghiKetNoiDongHanh> e) {
 e.ToTable("DeNghiKetNoiDongHanh");
 e.HasKey(x => x.MaDeNghi);
 e.HasOne(x=>x.NhuCauDongHanh).WithMany(x=>x.DeNghiKetNoiDongHanh).HasForeignKey(x => x.MaNhuCau).OnDelete(DeleteBehavior.Restrict);
 e.HasOne(x=>x.TaiKhoan).WithMany(x=>x.DeNghiKetNoiDongHanh).HasForeignKey(x => x.MaNguoiGui).OnDelete(DeleteBehavior.Restrict);
 e.Property(x => x.LoiGioiThieu).HasMaxLength(1000);
 e.Property(x => x.TrangThai).HasMaxLength(30).IsUnicode(false);
 e.ToTable(t => t.HasCheckConstraint("CK_DeNghiKetNoiDongHanh_TrangThai", "[TrangThai] IN ('ChoPhanHoi','DaChapNhan','TuChoi','DaRut','HetHieuLuc')"));
 e.Property(x => x.RowVersion).IsRowVersion();
 e.ToTable(t => t.HasCheckConstraint("CK_DeNghiKetNoiDongHanh_PhanHoi", "(TrangThai = 'ChoPhanHoi' AND ThoiDiemPhanHoi IS NULL) OR (TrangThai <> 'ChoPhanHoi' AND ThoiDiemPhanHoi IS NOT NULL)"));
 e.HasIndex(x => new { x.MaNguoiGui, x.MaNhuCau }, "UX_DeNghiKetNoiDongHanh_HieuLuc").IsUnique().HasDatabaseName("UX_DeNghiKetNoiDongHanh_HieuLuc").HasFilter("TrangThai IN ('ChoPhanHoi', 'DaChapNhan')");
 e.Property(x=>x.MaDeNghi).HasColumnType("bigint");
 e.Property(x=>x.MaNhuCau).HasColumnType("bigint");
 e.Property(x=>x.MaNguoiGui).HasColumnType("bigint");
 e.Property(x=>x.LoiGioiThieu).HasColumnType("nvarchar(1000)");
 e.Property(x=>x.ThoiDiemGui).HasColumnType("datetime2(0)");
 e.Property(x=>x.ThoiDiemGui).HasDefaultValueSql("SYSUTCDATETIME()", "DF_DeNghiKetNoiDongHanh_ThoiDiemGui");
 e.Property(x=>x.ThoiDiemPhanHoi).HasColumnType("datetime2(0)");
 e.Property(x=>x.TrangThai).HasColumnType("varchar(30)");
 e.Property(x=>x.TrangThai).HasDefaultValueSql("'ChoPhanHoi'", "DF_DeNghiKetNoiDongHanh_TrangThai");
 e.HasIndex(x=>new {x.MaNhuCau,x.TrangThai},"IX_DeNghiKetNoi_NhuCau").HasDatabaseName("IX_DeNghiKetNoi_NhuCau").IncludeProperties("MaNguoiGui","ThoiDiemGui");
 e.HasIndex(x=>new {x.MaNguoiGui,x.ThoiDiemGui},"IX_DeNghiKetNoi_NguoiGui").HasDatabaseName("IX_DeNghiKetNoi_NguoiGui").IsDescending(false,true);
 }
}
