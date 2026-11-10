using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class DeNghiDieuChinhDichVuConfiguration : IEntityTypeConfiguration<DeNghiDieuChinhDichVu> {
 public void Configure(EntityTypeBuilder<DeNghiDieuChinhDichVu> e) {
 e.ToTable("DeNghiDieuChinhDichVu");
 e.HasKey(x => x.MaDeNghi);
 e.HasOne(x=>x.YeuCauDatDichVu).WithMany(x=>x.DeNghiDieuChinhDichVu).HasForeignKey(x => x.MaYeuCau).OnDelete(DeleteBehavior.Restrict);
 e.Property(x => x.LoaiDeNghi).HasMaxLength(30).IsUnicode(false);
 e.ToTable(t => t.HasCheckConstraint("CK_DeNghiDieuChinhDichVu_LoaiDeNghi", "[LoaiDeNghi] IN ('ThayDoi','Huy')"));
 e.Property(x => x.DieuKienCu).HasMaxLength(2000);
 e.ToTable(t => t.HasCheckConstraint("CK_DeNghiDieuChinhDichVu_SoNguoiMoi", "[SoNguoiMoi] BETWEEN 1 AND 100"));
 e.ToTable(t => t.HasCheckConstraint("CK_DeNghiDieuChinhDichVu_SoLuongMoi", "[SoLuongMoi] BETWEEN 1 AND 100"));
 e.Property(x => x.DieuKienMoi).HasMaxLength(2000);
 e.Property(x => x.LyDo).HasMaxLength(500);
 e.Property(x => x.QuyetDinh).HasMaxLength(500);
 e.Property(x => x.DieuKienDeXuat).HasMaxLength(2000);
 e.Property(x => x.TrangThai).HasMaxLength(30).IsUnicode(false);
 e.ToTable(t => t.HasCheckConstraint("CK_DeNghiDieuChinhDichVu_TrangThai", "[TrangThai] IN ('ChoPhanHoi','DeXuatKhac','ChapNhan','TuChoi','DaRut')"));
 e.Property(x => x.RowVersion).IsRowVersion();
 e.ToTable(t => t.HasCheckConstraint("CK_DeNghiDieuChinhDichVu_LyDo", "(LyDo IS NOT NULL AND LEN(LTRIM(LyDo)) > 0)"));
 e.ToTable(t => t.HasCheckConstraint("CK_DeNghiDieuChinhDichVu_DieuKienMoi", "LoaiDeNghi <> 'ThayDoi' OR NgayBatDauMoi IS NOT NULL OR NgayKetThucMoi IS NOT NULL OR SoNguoiMoi IS NOT NULL OR SoLuongMoi IS NOT NULL OR (DieuKienMoi IS NOT NULL AND LEN(LTRIM(DieuKienMoi)) > 0)"));
 e.ToTable(t => t.HasCheckConstraint("CK_DeNghiDieuChinhDichVu_Huy", "LoaiDeNghi <> 'Huy' OR (NgayBatDauMoi IS NULL AND NgayKetThucMoi IS NULL AND SoNguoiMoi IS NULL AND SoLuongMoi IS NULL)"));
 e.ToTable(t => t.HasCheckConstraint("CK_DeNghiDieuChinhDichVu_NgayMoi", "NgayBatDauMoi IS NULL OR NgayKetThucMoi IS NULL OR NgayKetThucMoi >= NgayBatDauMoi"));
 e.ToTable(t => t.HasCheckConstraint("CK_DeNghiDieuChinhDichVu_DeXuat", "TrangThai <> 'DeXuatKhac' OR (DieuKienDeXuat IS NOT NULL AND LEN(LTRIM(DieuKienDeXuat)) > 0)"));
 e.ToTable(t => t.HasCheckConstraint("CK_DeNghiDieuChinhDichVu_TuChoi", "TrangThai <> 'TuChoi' OR (QuyetDinh IS NOT NULL AND LEN(LTRIM(QuyetDinh)) > 0)"));
 e.ToTable(t => t.HasCheckConstraint("CK_DeNghiDieuChinhDichVu_PhanHoi", "(TrangThai = 'ChoPhanHoi' AND ThoiDiemPhanHoi IS NULL) OR (TrangThai <> 'ChoPhanHoi' AND ThoiDiemPhanHoi IS NOT NULL)"));
 e.HasIndex(x => x.MaYeuCau, "UX_DeNghiDieuChinhDichVu_Cho").IsUnique().HasDatabaseName("UX_DeNghiDieuChinhDichVu_Cho").HasFilter("TrangThai IN ('ChoPhanHoi', 'DeXuatKhac')");
 e.Property(x=>x.MaDeNghi).HasColumnType("bigint");
 e.Property(x=>x.MaYeuCau).HasColumnType("bigint");
 e.Property(x=>x.LoaiDeNghi).HasColumnType("varchar(30)");
 e.Property(x=>x.DieuKienCu).HasColumnType("nvarchar(2000)");
 e.Property(x=>x.NgayBatDauMoi).HasColumnType("date");
 e.Property(x=>x.NgayKetThucMoi).HasColumnType("date");
 e.Property(x=>x.SoNguoiMoi).HasColumnType("int");
 e.Property(x=>x.SoLuongMoi).HasColumnType("int");
 e.Property(x=>x.DieuKienMoi).HasColumnType("nvarchar(2000)");
 e.Property(x=>x.LyDo).HasColumnType("nvarchar(500)");
 e.Property(x=>x.ThoiDiemGui).HasColumnType("datetime2(0)");
 e.Property(x=>x.ThoiDiemGui).HasDefaultValueSql("SYSUTCDATETIME()", "DF_DeNghiDieuChinhDichVu_ThoiDiemGui");
 e.Property(x=>x.QuyetDinh).HasColumnType("nvarchar(500)");
 e.Property(x=>x.DieuKienDeXuat).HasColumnType("nvarchar(2000)");
 e.Property(x=>x.ThoiDiemPhanHoi).HasColumnType("datetime2(0)");
 e.Property(x=>x.TrangThai).HasColumnType("varchar(30)");
 e.Property(x=>x.TrangThai).HasDefaultValueSql("'ChoPhanHoi'", "DF_DeNghiDieuChinhDichVu_TrangThai");
 e.HasIndex(x=>new {x.MaYeuCau,x.ThoiDiemGui},"IX_DeNghiDieuChinh_YeuCau").HasDatabaseName("IX_DeNghiDieuChinh_YeuCau").IsDescending(false,true);
 }
}
