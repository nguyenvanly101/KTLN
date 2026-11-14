using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoTroDuLich.Web.Data.Configurations;

public sealed class LichTrinhMauConfiguration:IEntityTypeConfiguration<LichTrinhMau>
{
 public void Configure(EntityTypeBuilder<LichTrinhMau> e)
 {
  e.ToTable("LichTrinhMau");e.HasKey(x=>x.Id);
  e.Property(x=>x.Title).HasColumnType("nvarchar(150)").HasMaxLength(150).IsRequired();
  e.Property(x=>x.Style).HasColumnType("varchar(12)").HasMaxLength(12).IsUnicode(false).IsRequired();
  e.Property(x=>x.Summary).HasColumnType("nvarchar(600)").HasMaxLength(600);
  e.Property(x=>x.Status).HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).IsRequired();
  e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");e.Property(x=>x.UpdatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
 }
}
public sealed class LichTrinhMauMucConfiguration:IEntityTypeConfiguration<LichTrinhMauMuc>
{
 public void Configure(EntityTypeBuilder<LichTrinhMauMuc> e)
 {
  e.ToTable("LichTrinhMauMuc");e.HasKey(x=>x.Id);
  e.Property(x=>x.Time).HasColumnType("time(0)");
  e.Property(x=>x.Content).HasColumnType("nvarchar(300)").HasMaxLength(300).IsRequired();
  e.Property(x=>x.Cost).HasColumnType("decimal(18,0)");
 }
}

public sealed class LoaiDichVuRowConfiguration:IEntityTypeConfiguration<LoaiDichVuRow>
{
 public void Configure(EntityTypeBuilder<LoaiDichVuRow> e)
 {
  e.ToTable("LoaiDichVu");e.HasKey(x=>x.Code);
  e.Property(x=>x.Code).HasColumnType("varchar(30)").HasMaxLength(30).IsUnicode(false);
  e.Property(x=>x.Label).HasColumnType("nvarchar(60)").HasMaxLength(60).IsRequired();
  e.Property(x=>x.GroupCode).HasColumnType("varchar(12)").HasMaxLength(12).IsUnicode(false).IsRequired();
  e.Property(x=>x.Icon).HasColumnType("varchar(20)").HasMaxLength(20).IsUnicode(false);
  e.Property(x=>x.FieldsJson).HasColumnType("nvarchar(max)");
  e.Property(x=>x.UpdatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
 }
}

public sealed class GroupExpenseConfiguration:IEntityTypeConfiguration<GroupExpense>
{
 public void Configure(EntityTypeBuilder<GroupExpense> e)
 {
  e.ToTable("KhoanChiChung");e.HasKey(x=>x.Id);
  e.Property(x=>x.Title).HasColumnType("nvarchar(200)").HasMaxLength(200).IsRequired();
  e.Property(x=>x.Amount).HasColumnType("decimal(18,0)");e.Property(x=>x.SpentOn).HasColumnType("date");
  e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
 }
}
public sealed class GroupExpenseShareConfiguration:IEntityTypeConfiguration<GroupExpenseShare>
{
 public void Configure(EntityTypeBuilder<GroupExpenseShare> e)
 {
  e.ToTable("KhoanChiChungChia");e.HasKey(x=>new{x.ExpenseId,x.AccountId});
  e.Property(x=>x.Share).HasColumnType("decimal(18,0)");
 }
}

public sealed class CauHoiConfiguration:IEntityTypeConfiguration<CauHoi>
{
 public void Configure(EntityTypeBuilder<CauHoi> e)
 {
  e.ToTable("CauHoi");e.HasKey(x=>x.Id);
  e.Property(x=>x.Title).HasColumnType("nvarchar(200)").HasMaxLength(200).IsRequired();e.Property(x=>x.Body).HasColumnType("nvarchar(2000)").HasMaxLength(2000);
  e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");e.Property(x=>x.DeletedAt).HasColumnType("datetime2(0)");
 }
}
public sealed class TraLoiConfiguration:IEntityTypeConfiguration<TraLoi>
{
 public void Configure(EntityTypeBuilder<TraLoi> e)
 {
  e.ToTable("TraLoi");e.HasKey(x=>x.Id);
  e.Property(x=>x.Body).HasColumnType("nvarchar(2000)").HasMaxLength(2000).IsRequired();
  e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");e.Property(x=>x.DeletedAt).HasColumnType("datetime2(0)");
 }
}
public sealed class BinhChonTraLoiConfiguration:IEntityTypeConfiguration<BinhChonTraLoi>
{
 public void Configure(EntityTypeBuilder<BinhChonTraLoi> e)
 {
  e.ToTable("BinhChonTraLoi");e.HasKey(x=>new{x.AnswerId,x.AccountId});
  e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
 }
}
public sealed class BaoSaiCoSoConfiguration:IEntityTypeConfiguration<BaoSaiCoSo>
{
 public void Configure(EntityTypeBuilder<BaoSaiCoSo> e)
 {
  e.ToTable("BaoSaiCoSo");e.HasKey(x=>x.Id);
  e.Property(x=>x.Field).HasColumnType("varchar(12)").HasMaxLength(12).IsUnicode(false).IsRequired();
  e.Property(x=>x.Detail).HasColumnType("nvarchar(500)").HasMaxLength(500).IsRequired();
  e.Property(x=>x.Status).HasColumnType("varchar(10)").HasMaxLength(10).IsUnicode(false).IsRequired();
  e.Property(x=>x.ResponseNote).HasColumnType("nvarchar(300)").HasMaxLength(300);
  e.Property(x=>x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");e.Property(x=>x.ResolvedAt).HasColumnType("datetime2(0)");
 }
}
public sealed class TripChatLinkConfiguration:IEntityTypeConfiguration<TripChatLink>
{
 public void Configure(EntityTypeBuilder<TripChatLink> e){e.ToTable("NhomChatChuyenDi");e.HasKey(x=>x.PlanId);}
}
public sealed class TripDayNoteConfiguration:IEntityTypeConfiguration<TripDayNote>
{
 public void Configure(EntityTypeBuilder<TripDayNote> e){e.ToTable("GhiChuNgayChuyenDi");e.HasKey(x=>new{x.PlanId,x.DayNo});}
}

public sealed class TripRoutePointsConfiguration:IEntityTypeConfiguration<TripRoutePoints>
{
 public void Configure(EntityTypeBuilder<TripRoutePoints> e){e.ToTable("DiemTuyenChuyenDi");e.HasKey(x=>x.PlanId);e.Property(x=>x.StartLat).HasColumnType("decimal(9,6)");e.Property(x=>x.StartLng).HasColumnType("decimal(9,6)");e.Property(x=>x.EndLat).HasColumnType("decimal(9,6)");e.Property(x=>x.EndLng).HasColumnType("decimal(9,6)");}
}
