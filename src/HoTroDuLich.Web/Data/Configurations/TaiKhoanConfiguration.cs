using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class TaiKhoanConfiguration : IEntityTypeConfiguration<TaiKhoan> {
 public void Configure(EntityTypeBuilder<TaiKhoan> e) {
 e.ToTable("TaiKhoan");
 e.HasKey(x => x.MaTaiKhoan);
 e.Property(x => x.Email).HasMaxLength(256);
 e.ToTable(t => t.HasCheckConstraint("CK_TaiKhoan_Email", "[Email] LIKE '%_@_%._%' AND [Email] NOT LIKE '% %'"));
 e.Property(x => x.SoDienThoai).HasMaxLength(15);
 e.ToTable(t => t.HasCheckConstraint("CK_TaiKhoan_SoDienThoai", "[SoDienThoai] LIKE '0%' AND [SoDienThoai] NOT LIKE '%[^0-9]%' AND LEN([SoDienThoai]) BETWEEN 10 AND 11"));
 e.Property(x => x.MatKhauBam).HasMaxLength(500);
 e.Property(x => x.VaiTro).HasMaxLength(30).IsUnicode(false);
 e.ToTable(t => t.HasCheckConstraint("CK_TaiKhoan_VaiTro", "[VaiTro] IN ('KHACH_DU_LICH','NHA_CUNG_CAP','QUAN_TRI_VIEN')"));
 e.Property(x => x.TrangThai).HasMaxLength(30).IsUnicode(false);
 e.ToTable(t => t.HasCheckConstraint("CK_TaiKhoan_TrangThai", "[TrangThai] IN ('HoatDong','DaNgung')"));
 e.Property(x => x.RowVersion).IsRowVersion();
 e.ToTable(t => t.HasCheckConstraint("CK_TaiKhoan_Ngung", "(TrangThai = 'DaNgung' AND ThoiDiemNgung IS NOT NULL) OR (TrangThai = 'HoatDong' AND ThoiDiemNgung IS NULL)"));
 e.HasIndex(x => x.Email, "UX_TaiKhoan_Email").IsUnique().HasDatabaseName("UX_TaiKhoan_Email");
 e.HasIndex(x => x.SoDienThoai, "UX_TaiKhoan_SoDienThoai").IsUnique().HasDatabaseName("UX_TaiKhoan_SoDienThoai").HasFilter("SoDienThoai IS NOT NULL");
 e.Property(x=>x.MaTaiKhoan).HasColumnType("bigint");
 e.Property(x=>x.Email).HasColumnType("nvarchar(256)");
 e.Property(x=>x.SoDienThoai).HasColumnType("varchar(15)");
 e.Property(x=>x.MatKhauBam).HasColumnType("nvarchar(500)");
 e.Property(x=>x.VaiTro).HasColumnType("varchar(30)");
 e.Property(x=>x.TrangThai).HasColumnType("varchar(30)");
 e.Property(x=>x.TrangThai).HasDefaultValueSql("'HoatDong'", "DF_TaiKhoan_TrangThai");
 e.Property(x=>x.DauPhien).HasColumnType("uniqueidentifier");
 e.Property(x=>x.DauPhien).HasDefaultValueSql("NEWID()", "DF_TaiKhoan_DauPhien");
 e.Property(x=>x.PhaiDoiMatKhau).HasColumnType("bit");
 e.Property(x=>x.PhaiDoiMatKhau).HasSentinel(false).HasDefaultValueSql("0", "DF_TaiKhoan_PhaiDoiMatKhau");
 e.Property(x=>x.ThoiDiemTao).HasColumnType("datetime2(0)");
 e.Property(x=>x.ThoiDiemTao).HasDefaultValueSql("SYSUTCDATETIME()", "DF_TaiKhoan_ThoiDiemTao");
 e.Property(x=>x.ThoiDiemDangNhapCuoi).HasColumnType("datetime2(0)");
 e.Property(x=>x.ThoiDiemNgung).HasColumnType("datetime2(0)");
 }
}
