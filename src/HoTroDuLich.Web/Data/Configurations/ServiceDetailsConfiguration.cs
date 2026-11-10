using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;
public sealed class ServiceDetailsConfiguration:IEntityTypeConfiguration<DichVuDuLich>,IEntityTypeConfiguration<BanCapNhatDichVu>
{
    public void Configure(EntityTypeBuilder<DichVuDuLich> e){Details(e);e.ToTable(t=>t.HasCheckConstraint("CK_DichVu_AnhJson","AnhJson IS NULL OR ISJSON(AnhJson)=1"));}
    public void Configure(EntityTypeBuilder<BanCapNhatDichVu> e){Details(e);e.ToTable(t=>t.HasCheckConstraint("CK_BanCapNhat_AnhJson","AnhJson IS NULL OR ISJSON(AnhJson)=1"));}
    private static void Details<T>(EntityTypeBuilder<T> e) where T:class
    {e.Property<string>("DiaChiPhucVu").HasMaxLength(300);e.Property<string>("DonViGia").HasMaxLength(50);e.Property<string>("ChinhSachHuy").HasMaxLength(2000);e.Property<string>("AnhJson").HasMaxLength(2000);}
}
