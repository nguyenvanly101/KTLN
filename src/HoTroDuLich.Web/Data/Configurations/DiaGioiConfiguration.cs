using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;

public sealed class DonViHanhChinhConfiguration : IEntityTypeConfiguration<DonViHanhChinh>
{
    public void Configure(EntityTypeBuilder<DonViHanhChinh> e)
    {
        e.ToTable("DonViHanhChinh");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).UseIdentityColumn();
        e.Property(x => x.Regime).HasColumnType("varchar(4)");
        e.Property(x => x.Code).HasColumnType("varchar(12)");
        e.Property(x => x.Level).HasColumnType("tinyint");
        e.Property(x => x.Kind).HasColumnType("varchar(20)");
        e.Property(x => x.Name).HasColumnType("nvarchar(100)");
        e.Property(x => x.FullName).HasColumnType("nvarchar(150)");
        e.Property(x => x.NameFolded).HasColumnType("nvarchar(150)");
        e.Property(x => x.Slug).HasColumnType("varchar(160)");
        e.Property(x => x.CenterLat).HasColumnType("decimal(9,6)");
        e.Property(x => x.CenterLng).HasColumnType("decimal(9,6)");
        e.Property(x => x.Status).HasColumnType("varchar(10)");
        e.Property(x => x.SourceName).HasColumnType("nvarchar(200)");
        e.Property(x => x.RowVersion).HasColumnType("rowversion").IsRowVersion();
        e.HasOne<DonViHanhChinh>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class DonViChuyenDoiConfiguration : IEntityTypeConfiguration<DonViChuyenDoi>
{
    public void Configure(EntityTypeBuilder<DonViChuyenDoi> e)
    {
        e.ToTable("DonViChuyenDoi");
        e.HasKey(x => new { x.OldUnitId, x.NewUnitId });
        e.Property(x => x.Note).HasColumnType("nvarchar(200)");
        e.HasOne<DonViHanhChinh>().WithMany().HasForeignKey(x => x.OldUnitId).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<DonViHanhChinh>().WithMany().HasForeignKey(x => x.NewUnitId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class TenGoiKhacDonViConfiguration : IEntityTypeConfiguration<TenGoiKhacDonVi>
{
    public void Configure(EntityTypeBuilder<TenGoiKhacDonVi> e)
    {
        e.ToTable("TenGoiKhacDonVi");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).UseIdentityColumn();
        e.Property(x => x.Alias).HasColumnType("nvarchar(150)");
        e.Property(x => x.NameFolded).HasColumnType("nvarchar(150)");
        e.Property(x => x.AliasKind).HasColumnType("varchar(15)");
        e.Property(x => x.Note).HasColumnType("nvarchar(300)");
        e.HasOne<DonViHanhChinh>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class KhuDuLichConfiguration : IEntityTypeConfiguration<KhuDuLich>
{
    public void Configure(EntityTypeBuilder<KhuDuLich> e)
    {
        e.ToTable("KhuDuLich");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).UseIdentityColumn();
        e.Property(x => x.Slug).HasColumnType("varchar(120)");
        e.Property(x => x.Name).HasColumnType("nvarchar(150)");
        e.Property(x => x.NameFolded).HasColumnType("nvarchar(150)");
        e.Property(x => x.Kind).HasColumnType("varchar(15)");
        e.Property(x => x.Summary).HasColumnType("nvarchar(700)");
        e.Property(x => x.CenterLat).HasColumnType("decimal(9,6)");
        e.Property(x => x.CenterLng).HasColumnType("decimal(9,6)");
        e.Property(x => x.SourceName).HasColumnType("nvarchar(200)");
        e.Property(x => x.RowVersion).HasColumnType("rowversion").IsRowVersion();
        e.HasOne<DonViHanhChinh>().WithMany().HasForeignKey(x => x.ProvinceId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class KhuDuLichDonViConfiguration : IEntityTypeConfiguration<KhuDuLichDonVi>
{
    public void Configure(EntityTypeBuilder<KhuDuLichDonVi> e)
    {
        e.ToTable("KhuDuLichDonVi");
        e.HasKey(x => new { x.ZoneId, x.UnitId });
        e.HasOne<KhuDuLich>().WithMany().HasForeignKey(x => x.ZoneId).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<DonViHanhChinh>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ChuyenDoiDiaGioiChoConfiguration : IEntityTypeConfiguration<ChuyenDoiDiaGioiCho>
{
    public void Configure(EntityTypeBuilder<ChuyenDoiDiaGioiCho> e)
    {
        e.ToTable("ChuyenDoiDiaGioiCho");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).UseIdentityColumn();
        e.Property(x => x.TableName).HasColumnType("varchar(60)");
        e.Property(x => x.OldText).HasColumnType("nvarchar(200)");
        e.Property(x => x.Reason).HasColumnType("varchar(15)");
        e.Property(x => x.Status).HasColumnType("varchar(10)");
        e.HasOne<DonViHanhChinh>().WithMany().HasForeignKey(x => x.ResolvedUnitId).OnDelete(DeleteBehavior.NoAction);
    }
}
