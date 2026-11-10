using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class DiaDiemDuLichConfiguration : IEntityTypeConfiguration<DiaDiemDuLich> {
 public void Configure(EntityTypeBuilder<DiaDiemDuLich> e) {
 e.ToTable("DiaDiemDuLich");
 e.HasKey(x => x.MaDiaDiem);
 e.Property(x => x.TenDiaDiem).HasMaxLength(200);
 e.ToTable(t => t.HasCheckConstraint("CK_DiaDiemDuLich_TenDiaDiem_KhongRong", "LEN(LTRIM([TenDiaDiem])) > 0"));
 e.Property(x => x.LoaiDiaDiem).HasMaxLength(50);
 e.ToTable(t => t.HasCheckConstraint("CK_DiaDiemDuLich_LoaiDiaDiem_KhongRong", "LEN(LTRIM([LoaiDiaDiem])) > 0"));
 e.Property(x => x.KhuVuc).HasMaxLength(100);
 e.ToTable(t => t.HasCheckConstraint("CK_DiaDiemDuLich_KhuVuc_KhongRong", "LEN(LTRIM([KhuVuc])) > 0"));
 e.Property(x => x.DiaChi).HasMaxLength(300);
 e.Property(x => x.MoTa).HasMaxLength(4000);
 e.Property(x=>x.MaDiaDiem).HasColumnType("bigint");
 e.Property(x=>x.TenDiaDiem).HasColumnType("nvarchar(200)");
 e.Property(x=>x.LoaiDiaDiem).HasColumnType("nvarchar(50)");
 e.Property(x=>x.KhuVuc).HasColumnType("nvarchar(100)");
 e.Property(x=>x.DiaChi).HasColumnType("nvarchar(300)");
 e.Property(x=>x.MoTa).HasColumnType("nvarchar(4000)");
 e.Property(x=>x.DangHienThi).HasColumnType("bit");
 e.Property(x=>x.DangHienThi).HasSentinel(true).HasDefaultValueSql("1", "DF_DiaDiemDuLich_DangHienThi");
 e.HasIndex(x=>new {x.KhuVuc,x.LoaiDiaDiem},"IX_DiaDiem_TimKiem").HasDatabaseName("IX_DiaDiem_TimKiem").IncludeProperties("TenDiaDiem").HasFilter("DangHienThi = 1");
 }
}
