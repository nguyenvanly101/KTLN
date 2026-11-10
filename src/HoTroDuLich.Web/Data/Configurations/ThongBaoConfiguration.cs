using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class ThongBaoConfiguration : IEntityTypeConfiguration<ThongBao> {
 public void Configure(EntityTypeBuilder<ThongBao> e) {
 e.ToTable("ThongBao");
 e.HasKey(x => x.MaThongBao);
 e.HasOne(x=>x.TaiKhoan).WithMany(x=>x.ThongBao).HasForeignKey(x => x.MaNguoiNhan).OnDelete(DeleteBehavior.Restrict);
 e.Property(x => x.LoaiThongBao).HasMaxLength(40);
 e.ToTable(t => t.HasCheckConstraint("CK_ThongBao_LoaiThongBao_KhongRong", "LEN(LTRIM([LoaiThongBao])) > 0"));
 e.Property(x => x.NoiDung).HasMaxLength(500);
 e.Property(x => x.DuongDan).HasMaxLength(300);
 e.Property(x=>x.MaThongBao).HasColumnType("bigint");
 e.Property(x=>x.MaNguoiNhan).HasColumnType("bigint");
 e.Property(x=>x.LoaiThongBao).HasColumnType("nvarchar(40)");
 e.Property(x=>x.NoiDung).HasColumnType("nvarchar(500)");
 e.Property(x=>x.DuongDan).HasColumnType("nvarchar(300)");
 e.Property(x=>x.DaDoc).HasColumnType("bit");
 e.Property(x=>x.DaDoc).HasSentinel(false).HasDefaultValueSql("0", "DF_ThongBao_DaDoc");
 e.Property(x=>x.ThoiDiemTao).HasColumnType("datetime2(0)");
 e.Property(x=>x.ThoiDiemTao).HasDefaultValueSql("SYSUTCDATETIME()", "DF_ThongBao_ThoiDiemTao");
 e.HasIndex(x=>new {x.MaNguoiNhan,x.DaDoc,x.ThoiDiemTao},"IX_ThongBao_NguoiNhan").HasDatabaseName("IX_ThongBao_NguoiNhan").IsDescending(false,false,true).IncludeProperties("LoaiThongBao");
 }
}
