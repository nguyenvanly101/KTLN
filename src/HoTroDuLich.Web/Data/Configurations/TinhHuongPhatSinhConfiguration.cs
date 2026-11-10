using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class TinhHuongPhatSinhConfiguration : IEntityTypeConfiguration<TinhHuongPhatSinh> {
 public void Configure(EntityTypeBuilder<TinhHuongPhatSinh> e) {
 e.ToTable("TinhHuongPhatSinh");
 e.HasKey(x => x.MaTinhHuong);
 e.HasOne(x=>x.KeHoachChuyenDi).WithMany(x=>x.TinhHuongPhatSinh).HasForeignKey(x => x.MaKeHoach).OnDelete(DeleteBehavior.Restrict);
 e.Property(x => x.LoaiTinhHuong).HasMaxLength(50);
 e.ToTable(t => t.HasCheckConstraint("CK_TinhHuongPhatSinh_LoaiTinhHuong_KhongRong", "LEN(LTRIM([LoaiTinhHuong])) > 0"));
 e.Property(x => x.MoTa).HasMaxLength(2000);
 e.Property(x => x.ViTri).HasMaxLength(300);
 e.Property(x => x.TrangThaiXuLy).HasMaxLength(30).IsUnicode(false);
 e.ToTable(t => t.HasCheckConstraint("CK_TinhHuongPhatSinh_TrangThaiXuLy", "[TrangThaiXuLy] IN ('ChuaGiaiQuyet','DaChonPhuongAn','GiaiQuyetMotPhan','DaGiaiQuyet','DungXuLy')"));
 e.Property(x => x.KetQuaXuLy).HasMaxLength(2000);
 e.Property(x => x.RowVersion).IsRowVersion();
 e.ToTable(t => t.HasCheckConstraint("CK_TinhHuongPhatSinh_KetQua", "TrangThaiXuLy <> 'DaGiaiQuyet' OR (KetQuaXuLy IS NOT NULL AND LEN(LTRIM(KetQuaXuLy)) > 0)"));
 e.Property(x=>x.MaTinhHuong).HasColumnType("bigint");
 e.Property(x=>x.MaKeHoach).HasColumnType("bigint");
 e.Property(x=>x.LoaiTinhHuong).HasColumnType("nvarchar(50)");
 e.Property(x=>x.MoTa).HasColumnType("nvarchar(2000)");
 e.Property(x=>x.ViTri).HasColumnType("nvarchar(300)");
 e.Property(x=>x.ThoiDiemGhiNhan).HasColumnType("datetime2(0)");
 e.Property(x=>x.ThoiDiemGhiNhan).HasDefaultValueSql("SYSUTCDATETIME()", "DF_TinhHuongPhatSinh_ThoiDiemGhiNhan");
 e.Property(x=>x.TrangThaiXuLy).HasColumnType("varchar(30)");
 e.Property(x=>x.TrangThaiXuLy).HasDefaultValueSql("'ChuaGiaiQuyet'", "DF_TinhHuongPhatSinh_TrangThaiXuLy");
 e.Property(x=>x.KetQuaXuLy).HasColumnType("nvarchar(2000)");
 e.Property(x=>x.ThoiDiemCapNhat).HasColumnType("datetime2(0)");
 e.Property(x=>x.ThoiDiemCapNhat).HasDefaultValueSql("SYSUTCDATETIME()", "DF_TinhHuongPhatSinh_ThoiDiemCapNhat");
 e.HasIndex(x=>new {x.MaKeHoach,x.ThoiDiemGhiNhan},"IX_TinhHuong_KeHoach").HasDatabaseName("IX_TinhHuong_KeHoach").IsDescending(false,true);
 }
}
