using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class KetBanConfiguration:IEntityTypeConfiguration<KetBan>
{
 public void Configure(EntityTypeBuilder<KetBan> e)
 {
  e.ToTable("KetBan");e.HasKey(x=>x.Id);
  e.Property(x=>x.Status).HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).IsRequired();
  e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");e.Property(x=>x.RespondedAt).HasColumnType("datetime2(0)");
 }
}
public sealed class BaiVietConfiguration:IEntityTypeConfiguration<BaiViet>
{
 public void Configure(EntityTypeBuilder<BaiViet> e)
 {
  e.ToTable("BaiViet");e.HasKey(x=>x.Id);
  e.Property(x=>x.Content).HasColumnType("nvarchar(4000)").HasMaxLength(4000).IsRequired();
  e.Property(x=>x.Visibility).HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).IsRequired();
  e.Property(x=>x.PhotosJson).HasColumnType("nvarchar(1500)").HasMaxLength(1500);
  e.Property(x=>x.Kind).HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).HasDefaultValue("Share").IsRequired();e.Property(x=>x.Destination).HasColumnType("nvarchar(200)").HasMaxLength(200);e.Property(x=>x.TripStart).HasColumnType("date");e.Property(x=>x.TripEnd).HasColumnType("date");e.Property(x=>x.ServiceId);e.Property(x=>x.PlaceId);e.Property(x=>x.SharedPostId);
  e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");e.Property(x=>x.UpdatedAt).HasColumnType("datetime2(0)");e.Property(x=>x.DeletedAt).HasColumnType("datetime2(0)");
 }
}
public sealed class BinhLuanConfiguration:IEntityTypeConfiguration<BinhLuan>
{
 public void Configure(EntityTypeBuilder<BinhLuan> e)
 {
  e.ToTable("BinhLuan");e.HasKey(x=>x.Id);
  e.Property(x=>x.Content).HasColumnType("nvarchar(1500)").HasMaxLength(1500).IsRequired();
  e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");e.Property(x=>x.UpdatedAt).HasColumnType("datetime2(0)");e.Property(x=>x.DeletedAt).HasColumnType("datetime2(0)");
 }
}
public sealed class CamXucConfiguration:IEntityTypeConfiguration<CamXuc>
{
 public void Configure(EntityTypeBuilder<CamXuc> e)
 {
  e.ToTable("CamXuc");e.HasKey(x=>x.Id);
  e.Property(x=>x.TargetType).HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).IsRequired();
  e.Property(x=>x.Emoji).HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).IsRequired();
  e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
 }
}
public sealed class ThanhVienChatConfiguration:IEntityTypeConfiguration<ThanhVienChat>
{
 public void Configure(EntityTypeBuilder<ThanhVienChat> e)
 {
  e.ToTable("ThanhVienChat");e.HasKey(x=>new{x.ConversationId,x.AccountId});
  e.Property(x=>x.Role).HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).IsRequired();
  e.Property(x=>x.JoinedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");e.Property(x=>x.LeftAt).HasColumnType("datetime2(0)");
 }
}
public sealed class BaoCaoBaiVietConfiguration:IEntityTypeConfiguration<BaoCaoBaiViet>
{
 public void Configure(EntityTypeBuilder<BaoCaoBaiViet> e)
 {
  e.ToTable("BaoCaoBaiViet");e.HasKey(x=>x.Id);
  e.Property(x=>x.Reason).HasColumnType("varchar(20)").HasMaxLength(20).IsUnicode(false).IsRequired();
  e.Property(x=>x.Note).HasColumnType("nvarchar(500)").HasMaxLength(500);
  e.Property(x=>x.Status).HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).IsRequired();
  e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");e.Property(x=>x.HandledAt).HasColumnType("datetime2(0)");
 }
}

public sealed class ThanhVienChuyenDiConfiguration:IEntityTypeConfiguration<ThanhVienChuyenDi>
{
 public void Configure(EntityTypeBuilder<ThanhVienChuyenDi> e)
 {
  e.ToTable("ThanhVienChuyenDi");e.HasKey(x=>new{x.PlanId,x.AccountId});
  e.Property(x=>x.Status).HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).IsRequired();
  e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");e.Property(x=>x.RespondedAt).HasColumnType("datetime2(0)");
 }
}

public sealed class BaiVietDaLuuConfiguration:IEntityTypeConfiguration<BaiVietDaLuu>
{
 public void Configure(EntityTypeBuilder<BaiVietDaLuu> e)
 {
  e.ToTable("BaiVietDaLuu");e.HasKey(x=>new{x.AccountId,x.PostId});
  e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
 }
}
public sealed class TheoDoiCoSoConfiguration:IEntityTypeConfiguration<TheoDoiCoSo>
{
 public void Configure(EntityTypeBuilder<TheoDoiCoSo> e)
 {
  e.ToTable("TheoDoiCoSo");e.HasKey(x=>new{x.AccountId,x.BranchId});
  e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
 }
}
