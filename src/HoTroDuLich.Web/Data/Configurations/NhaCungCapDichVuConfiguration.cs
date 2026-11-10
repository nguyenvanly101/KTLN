using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class NhaCungCapDichVuConfiguration : IEntityTypeConfiguration<NhaCungCapDichVu> {
 public void Configure(EntityTypeBuilder<NhaCungCapDichVu> e) {
 e.ToTable("NhaCungCapDichVu");
 e.HasKey(x => x.MaNhaCungCap);
 e.HasOne(x=>x.TaiKhoan).WithMany(x=>x.NhaCungCapDichVu).HasForeignKey(x => x.MaTaiKhoan).OnDelete(DeleteBehavior.Restrict);
 e.Property(x => x.TenDonVi).HasMaxLength(200);
 e.ToTable(t => t.HasCheckConstraint("CK_NhaCungCapDichVu_TenDonVi_KhongRong", "LEN(LTRIM([TenDonVi])) > 0"));
 e.Property(x => x.LoaiHinh).HasMaxLength(50);
 e.ToTable(t => t.HasCheckConstraint("CK_NhaCungCapDichVu_LoaiHinh_KhongRong", "LEN(LTRIM([LoaiHinh])) > 0"));
 e.Property(x => x.NguoiLienHe).HasMaxLength(100);
 e.ToTable(t => t.HasCheckConstraint("CK_NhaCungCapDichVu_NguoiLienHe_KhongRong", "LEN(LTRIM([NguoiLienHe])) > 0"));
 e.Property(x => x.DiaChi).HasMaxLength(300);
 e.Property(x => x.SoDienThoai).HasMaxLength(15);
 e.ToTable(t => t.HasCheckConstraint("CK_NhaCungCapDichVu_SoDienThoai", "[SoDienThoai] LIKE '0%' AND [SoDienThoai] NOT LIKE '%[^0-9]%' AND LEN([SoDienThoai]) BETWEEN 10 AND 11"));
 e.Property(x => x.EmailLienHe).HasMaxLength(256);
 e.ToTable(t => t.HasCheckConstraint("CK_NhaCungCapDichVu_EmailLienHe", "[EmailLienHe] LIKE '%_@_%._%' AND [EmailLienHe] NOT LIKE '% %'"));
 e.Property(x => x.TrangThaiXacMinh).HasMaxLength(30).IsUnicode(false);
 e.ToTable(t => t.HasCheckConstraint("CK_NhaCungCapDichVu_TrangThaiXacMinh", "[TrangThaiXacMinh] IN ('ChoXacMinh','CanBoSung','DaXacMinh','TuChoi','DaThuHoi')"));
 e.Property(x => x.TrangThaiQuyenHoatDong).HasMaxLength(30).IsUnicode(false);
 e.ToTable(t => t.HasCheckConstraint("CK_NhaCungCapDichVu_TrangThaiQuyenHoatDong", "[TrangThaiQuyenHoatDong] IN ('DangHoatDong','TamDinhChi','DaThuHoi')"));
 e.Property(x => x.RowVersion).IsRowVersion();
 e.ToTable(t => t.HasCheckConstraint("CK_NhaCungCapDichVu_TrangThai", "(TrangThaiXacMinh IN ('ChoXacMinh', 'CanBoSung', 'TuChoi') AND TrangThaiQuyenHoatDong IS NULL) OR (TrangThaiXacMinh = 'DaXacMinh' AND TrangThaiQuyenHoatDong IS NOT NULL AND TrangThaiQuyenHoatDong IN ('DangHoatDong', 'TamDinhChi')) OR (TrangThaiXacMinh = 'DaThuHoi' AND TrangThaiQuyenHoatDong IS NOT NULL AND TrangThaiQuyenHoatDong = 'DaThuHoi')"));
 e.HasIndex(x => x.MaTaiKhoan, "UX_NhaCungCapDichVu_MaTaiKhoan").IsUnique().HasDatabaseName("UX_NhaCungCapDichVu_MaTaiKhoan");
 e.Property(x=>x.MaNhaCungCap).HasColumnType("bigint");
 e.Property(x=>x.MaTaiKhoan).HasColumnType("bigint");
 e.Property(x=>x.TenDonVi).HasColumnType("nvarchar(200)");
 e.Property(x=>x.LoaiHinh).HasColumnType("nvarchar(50)");
 e.Property(x=>x.NguoiLienHe).HasColumnType("nvarchar(100)");
 e.Property(x=>x.DiaChi).HasColumnType("nvarchar(300)");
 e.Property(x=>x.SoDienThoai).HasColumnType("varchar(15)");
 e.Property(x=>x.EmailLienHe).HasColumnType("nvarchar(256)");
 e.Property(x=>x.TrangThaiXacMinh).HasColumnType("varchar(30)");
 e.Property(x=>x.TrangThaiXacMinh).HasDefaultValueSql("'ChoXacMinh'", "DF_NhaCungCapDichVu_TrangThaiXacMinh");
 e.Property(x=>x.TrangThaiQuyenHoatDong).HasColumnType("varchar(30)");
 e.Property(x=>x.ThoiDiemTao).HasColumnType("datetime2(0)");
 e.Property(x=>x.ThoiDiemTao).HasDefaultValueSql("SYSUTCDATETIME()", "DF_NhaCungCapDichVu_ThoiDiemTao");
 e.HasIndex(x=>new {x.TrangThaiXacMinh,x.TrangThaiQuyenHoatDong},"IX_NhaCungCap_TrangThai").HasDatabaseName("IX_NhaCungCap_TrangThai").IncludeProperties("TenDonVi");
 }
}
