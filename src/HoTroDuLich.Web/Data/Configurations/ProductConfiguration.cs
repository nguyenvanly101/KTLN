using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoTroDuLich.Web.Data.Configurations;

public sealed class SavedChoiceConfiguration : IEntityTypeConfiguration<LuaChonDaLuu>
{
    public void Configure(EntityTypeBuilder<LuaChonDaLuu> e)
    {
        e.ToTable("LuaChonDaLuu", t => t.HasCheckConstraint("CK_LuaChonDaLuu_MotMuc", "(MaDichVu IS NOT NULL AND MaDiaDiem IS NULL) OR (MaDichVu IS NULL AND MaDiaDiem IS NOT NULL)"));
        e.HasKey(x=>x.MaLuaChon); e.Property(x=>x.TenMuc).HasMaxLength(200);
        e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.MaTaiKhoan);
        e.HasOne<DichVuDuLich>().WithMany().HasForeignKey(x=>x.MaDichVu);
        e.HasOne<DiaDiemDuLich>().WithMany().HasForeignKey(x=>x.MaDiaDiem);
        e.HasIndex(x=>new{x.MaTaiKhoan,x.MaDichVu}, "UX_LuaChon_DichVu").IsUnique().HasFilter("[MaDichVu] IS NOT NULL");
        e.HasIndex(x=>new{x.MaTaiKhoan,x.MaDiaDiem}, "UX_LuaChon_DiaDiem").IsUnique().HasFilter("[MaDiaDiem] IS NOT NULL");
        e.HasIndex(x=>new{x.MaTaiKhoan,x.ThoiDiemLuu}, "IX_LuaChon_TaiKhoan").IsDescending(false,true);
    }
}
public sealed class PreparationConfiguration : IEntityTypeConfiguration<MucChuanBi>
{
    public void Configure(EntityTypeBuilder<MucChuanBi> e)
    {
        e.ToTable("MucChuanBi",t=>t.HasCheckConstraint("CK_MucChuanBi_Nhom", "Nhom IN ('GiayTo','HanhLy','DiChuyen','DatDichVu','Khac')"));
        e.HasKey(x=>x.MaMuc);e.Property(x=>x.NoiDung).HasMaxLength(200);e.Property(x=>x.Nhom).HasColumnType("varchar(40)");e.Property(x=>x.RowVersion).IsRowVersion();
        e.HasOne<KeHoachChuyenDi>().WithMany().HasForeignKey(x=>x.MaKeHoach);
        e.HasIndex(x=>new{x.MaKeHoach,x.NoiDung}, "UX_ChuanBi_NoiDung").IsUnique();
        e.HasIndex(x=>new{x.MaKeHoach,x.HanHoanThanh}, "IX_ChuanBi_KeHoach");
    }
}
public sealed class ExpenseConfiguration : IEntityTypeConfiguration<KhoanChiChuyenDi>
{
    public void Configure(EntityTypeBuilder<KhoanChiChuyenDi> e)
    {
        e.ToTable("KhoanChiChuyenDi", t=> {t.HasCheckConstraint("CK_KhoanChi_SoTien", "SoTien > 0 AND SoTien <= 1000000000");t.HasCheckConstraint("CK_KhoanChi_Loai", "LoaiChi IN ('LuuTru','AnUong','DiChuyen','HoatDong','MuaSam','Khac')");});
        e.HasKey(x=>x.MaKhoanChi);e.Property(x=>x.NoiDung).HasMaxLength(200);e.Property(x=>x.LoaiChi).HasColumnType("varchar(40)");e.Property(x=>x.GhiChu).HasMaxLength(500);e.Property(x=>x.SoTien).HasPrecision(18,2);e.Property(x=>x.RowVersion).IsRowVersion();
        e.HasOne<KeHoachChuyenDi>().WithMany().HasForeignKey(x=>x.MaKeHoach);
        e.HasIndex(x=>new{x.MaKeHoach,x.MaLuotGhi}, "UX_KhoanChi_LuotGhi").IsUnique();
        e.HasIndex(x=>new{x.MaKeHoach,x.NgayChi}, "IX_KhoanChi_KeHoach").IsDescending(false,true);
    }
}
