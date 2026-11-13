using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class CuocTroChuyenConfiguration:IEntityTypeConfiguration<CuocTroChuyen>
{
 public void Configure(EntityTypeBuilder<CuocTroChuyen> e)
 {
  e.ToTable("CuocTroChuyen");e.HasKey(x=>x.Id);
  e.Property(x=>x.State).HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).IsRequired();
  e.Property(x=>x.Label).HasColumnType("nvarchar(30)").HasMaxLength(30);
  e.Property(x=>x.InternalNote).HasColumnType("nvarchar(500)").HasMaxLength(500);
  e.Property(x=>x.Title).HasColumnType("nvarchar(100)").HasMaxLength(100);
  e.Property(x=>x.LastPreview).HasColumnType("nvarchar(120)").HasMaxLength(120);
  e.Property(x=>x.LastMessageAt).HasColumnType("datetime2(3)");e.Property(x=>x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
  e.Property(x=>x.TravelerReadAt).HasColumnType("datetime2(3)");e.Property(x=>x.BranchReadAt).HasColumnType("datetime2(3)");
 }
}
public sealed class TinNhanChatConfiguration:IEntityTypeConfiguration<TinNhanChat>
{
 public void Configure(EntityTypeBuilder<TinNhanChat> e)
 {
  e.ToTable("TinNhanChat");e.HasKey(x=>x.Id);
  e.Property(x=>x.Side).HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).IsRequired();
  e.Property(x=>x.Kind).HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).IsRequired();
  e.Property(x=>x.Body).HasColumnType("nvarchar(2000)").HasMaxLength(2000).IsRequired();
  e.Property(x=>x.ImagePath).HasColumnType("nvarchar(300)").HasMaxLength(300);
  e.Property(x=>x.FileName).HasColumnType("nvarchar(150)").HasMaxLength(150);e.Property(x=>x.FilePath).HasColumnType("nvarchar(300)").HasMaxLength(300);
  e.Property(x=>x.Lat).HasColumnType("decimal(9,6)");e.Property(x=>x.Lng).HasColumnType("decimal(9,6)");
  e.Property(x=>x.EditedAt).HasColumnType("datetime2(3)");e.Property(x=>x.DeletedAt).HasColumnType("datetime2(3)");
  e.Property(x=>x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
  e.HasIndex(x=>new{x.ConversationId,x.Id}).HasDatabaseName("IX_Msg_Chat");
 }
}
public sealed class TinMauChatConfiguration:IEntityTypeConfiguration<TinMauChat>
{
 public void Configure(EntityTypeBuilder<TinMauChat> e)
 {
  e.ToTable("TinMauChat");e.HasKey(x=>x.Id);
  e.Property(x=>x.Title).HasColumnType("nvarchar(40)").HasMaxLength(40).IsRequired();
  e.Property(x=>x.Body).HasColumnType("nvarchar(1000)").HasMaxLength(1000).IsRequired();
  e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
 }
}
public sealed class CaiDatChatCoSoConfiguration:IEntityTypeConfiguration<CaiDatChatCoSo>
{
 public void Configure(EntityTypeBuilder<CaiDatChatCoSo> e)
 {
  e.ToTable("CaiDatChatCoSo");e.HasKey(x=>x.BranchId);e.Property(x=>x.BranchId).ValueGeneratedNever();
  e.Property(x=>x.ReplyWithin).HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).IsRequired();
  e.Property(x=>x.AutoReply).HasColumnType("nvarchar(500)").HasMaxLength(500);
  e.Property(x=>x.Greeting).HasColumnType("nvarchar(300)").HasMaxLength(300);
  e.Property(x=>x.UpdatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
 }
}

public sealed class PhanUngTinConfiguration:IEntityTypeConfiguration<PhanUngTin>
{
 public void Configure(EntityTypeBuilder<PhanUngTin> e)
 {
  e.ToTable("PhanUngTin");e.HasKey(x=>new{x.MessageId,x.AccountId});
  e.Property(x=>x.Emoji).HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).IsRequired();
  e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
 }
}

public sealed class DatCocChatConfiguration:IEntityTypeConfiguration<DatCocChat>
{
 public void Configure(EntityTypeBuilder<DatCocChat> e)
 {
  e.ToTable("DatCocChat");e.HasKey(x=>x.RequestId);e.Property(x=>x.RequestId).ValueGeneratedNever();
  e.Property(x=>x.Amount).HasColumnType("decimal(18,0)");
  e.Property(x=>x.Method).HasColumnType("nvarchar(40)").HasMaxLength(40).IsRequired();
  e.Property(x=>x.Note).HasColumnType("nvarchar(300)").HasMaxLength(300);
  e.Property(x=>x.Status).HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).IsRequired();
  e.Property(x=>x.RequestedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");e.Property(x=>x.PaidAt).HasColumnType("datetime2(0)");
 }
}
public sealed class NhacLichChatConfiguration:IEntityTypeConfiguration<NhacLichChat>
{
 public void Configure(EntityTypeBuilder<NhacLichChat> e)
 {
  e.ToTable("NhacLichChat");e.HasKey(x=>new{x.RequestId,x.Kind});
  e.Property(x=>x.Kind).HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).IsRequired();
  e.Property(x=>x.SentAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
 }
}

public sealed class BoQuaGoiYConfiguration:IEntityTypeConfiguration<BoQuaGoiY>
{
 public void Configure(EntityTypeBuilder<BoQuaGoiY> e)
 {
  e.ToTable("BoQuaGoiY");e.HasKey(x=>new{x.AccountId,x.NeedId});e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
 }
}
public sealed class BaoCaoChatConfiguration:IEntityTypeConfiguration<BaoCaoChat>
{
 public void Configure(EntityTypeBuilder<BaoCaoChat> e)
 {
  e.ToTable("BaoCaoChat");e.HasKey(x=>x.Id);
  e.Property(x=>x.Reason).HasColumnType("varchar(20)").HasMaxLength(20).IsUnicode(false).IsRequired();
  e.Property(x=>x.Note).HasColumnType("nvarchar(500)").HasMaxLength(500);
  e.Property(x=>x.Status).HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).IsRequired();
  e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");e.Property(x=>x.HandledAt).HasColumnType("datetime2(0)");
 }
}
