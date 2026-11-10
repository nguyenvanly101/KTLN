using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HoTroDuLich.Web.Data.Configurations;

public sealed class HoSoDongHanhConfiguration : IEntityTypeConfiguration<HoSoDongHanh>
{
    public void Configure(EntityTypeBuilder<HoSoDongHanh> e)
    {
        e.ToTable("HoSoDongHanh",t=>{t.HasCheckConstraint("CK_HSDH_PhongCach","PhongCach IN ('KhamPha','NghiDuong','AmThuc','ThienNhien','VanHoa','Khac')");t.HasCheckConstraint("CK_HSDH_NhipDo","NhipDo IN ('ThuThai','CanBang','NangDong')");t.HasCheckConstraint("CK_HSDH_NganSach","NganSachToiDa IS NULL OR NganSachToiDa BETWEEN 0 AND 1000000000");});
        e.HasKey(x=>x.MaTaiKhoan);
        e.Property(x=>x.MaTaiKhoan).HasColumnType("bigint").ValueGeneratedNever();
        e.Property(x=>x.PhongCach).HasColumnType("varchar(30)");
        e.Property(x=>x.NhipDo).HasColumnType("varchar(30)");
        e.Property(x=>x.NganSachToiDa).HasColumnType("decimal(18,2)");
        e.Property(x=>x.GioiThieu).HasColumnType("nvarchar(500)");
        e.Property(x=>x.HienThi).HasColumnType("bit");
        e.Property(x=>x.ThoiDiemCapNhat).HasColumnType("datetime2(0)");
        e.Property(x=>x.RowVersion).HasColumnType("rowversion").IsRowVersion();
        e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.MaTaiKhoan).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class NhomDongHanhConfiguration : IEntityTypeConfiguration<NhomDongHanh>
{
    public void Configure(EntityTypeBuilder<NhomDongHanh> e)
    {
        e.ToTable("NhomDongHanh",t=>{t.HasCheckConstraint("CK_NhomDH_Ngay","NgayKetThuc>=NgayBatDau");t.HasCheckConstraint("CK_NhomDH_Loai","Loai IN ('ChuyenDi','HoatDong')");t.HasCheckConstraint("CK_NhomDH_SoNguoi","SoNguoiToiDa BETWEEN 2 AND 30");t.HasCheckConstraint("CK_NhomDH_NganSach","NganSachThamKhao IS NULL OR NganSachThamKhao BETWEEN 0 AND 1000000000");t.HasCheckConstraint("CK_NhomDH_PhongCach","PhongCach IN ('KhamPha','NghiDuong','AmThuc','ThienNhien','VanHoa','Khac')");t.HasCheckConstraint("CK_NhomDH_NhipDo","NhipDo IN ('ThuThai','CanBang','NangDong')");t.HasCheckConstraint("CK_NhomDH_TrangThai","TrangThai IN ('Nhap','Mo','DaChot','DaKetThuc','DaHuy')");t.HasCheckConstraint("CK_NhomDH_Phien","PhienDiemHen>=1");t.HasCheckConstraint("CK_NhomDH_An","AnBoiQuanTri=0 OR LyDoAn IS NOT NULL");});
        e.HasKey(x=>x.MaNhom);
        e.Property(x=>x.MaNhom).HasColumnType("bigint").UseIdentityColumn();
        e.Property(x=>x.MaChuNhom).HasColumnType("bigint");
        e.Property(x=>x.MaKeHoach).HasColumnType("bigint");
        e.Property(x=>x.MaDeNghi).HasColumnType("bigint");
        e.Property(x=>x.TenNhom).HasColumnType("nvarchar(150)");
        e.Property(x=>x.MoTa).HasColumnType("nvarchar(2000)");
        e.Property(x=>x.KhuVuc).HasColumnType("nvarchar(100)");
        e.Property(x=>x.Loai).HasColumnType("varchar(30)");
        e.Property(x=>x.NgayBatDau).HasColumnType("date");
        e.Property(x=>x.NgayKetThuc).HasColumnType("date");
        e.Property(x=>x.GioHen).HasColumnType("time(0)");
        e.Property(x=>x.DiemHen).HasColumnType("nvarchar(300)");
        e.Property(x=>x.SoNguoiToiDa).HasColumnType("int");
        e.Property(x=>x.NganSachThamKhao).HasColumnType("decimal(18,2)");
        e.Property(x=>x.PhongCach).HasColumnType("varchar(30)");
        e.Property(x=>x.NhipDo).HasColumnType("varchar(30)");
        e.Property(x=>x.TrangThai).HasColumnType("varchar(30)");
        e.Property(x=>x.AnBoiQuanTri).HasColumnType("bit");
        e.Property(x=>x.LyDoAn).HasColumnType("nvarchar(500)");
        e.Property(x=>x.PhienDiemHen).HasColumnType("int");
        e.Property(x=>x.ThoiDiemTao).HasColumnType("datetime2(0)");
        e.Property(x=>x.ThoiDiemCapNhat).HasColumnType("datetime2(0)");
        e.Property(x=>x.RowVersion).HasColumnType("rowversion").IsRowVersion();
        e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.MaChuNhom).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<KeHoachChuyenDi>().WithMany().HasForeignKey(x=>x.MaKeHoach).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<DeNghiKetNoiDongHanh>().WithMany().HasForeignKey(x=>x.MaDeNghi).OnDelete(DeleteBehavior.NoAction);
        e.HasIndex(x=>new{x.TrangThai,x.NgayBatDau},"IX_NhomDH_TimKiem");
        e.HasIndex(x=>new{x.MaChuNhom,x.ThoiDiemCapNhat},"IX_NhomDH_Chu");
        e.HasIndex(x=>x.MaDeNghi,"UX_NhomDH_KetNoi").IsUnique().HasFilter("MaDeNghi IS NOT NULL");
    }
}

public sealed class ThanhVienDongHanhConfiguration : IEntityTypeConfiguration<ThanhVienDongHanh>
{
    public void Configure(EntityTypeBuilder<ThanhVienDongHanh> e)
    {
        e.ToTable("ThanhVienDongHanh",t=>{t.HasCheckConstraint("CK_TVDH_TrangThai","TrangThai IN ('ChoDuyet','DuocMoi','DaThamGia','TuChoi','DaRut','DaXoa')");t.HasCheckConstraint("CK_TVDH_DiemDanh","DiemDanh IN ('ChuaXacNhan','SeDen','DaDen','DenMuon')");t.HasCheckConstraint("CK_TVDH_Phien","PhienDiemHen>=0");});
        e.HasKey(x=>new{x.MaNhom,x.MaTaiKhoan});
        e.Property(x=>x.MaNhom).HasColumnType("bigint").ValueGeneratedNever();
        e.Property(x=>x.MaTaiKhoan).HasColumnType("bigint").ValueGeneratedNever();
        e.Property(x=>x.TrangThai).HasColumnType("varchar(30)");
        e.Property(x=>x.LoiNhan).HasColumnType("nvarchar(500)");
        e.Property(x=>x.PhanHoi).HasColumnType("nvarchar(500)");
        e.Property(x=>x.ThamGiaLuc).HasColumnType("datetime2(0)");
        e.Property(x=>x.DiemDanh).HasColumnType("varchar(30)");
        e.Property(x=>x.PhienDiemHen).HasColumnType("int");
        e.Property(x=>x.ThoiDiemCapNhat).HasColumnType("datetime2(0)");
        e.Property(x=>x.RowVersion).HasColumnType("rowversion").IsRowVersion();
        e.HasOne<NhomDongHanh>().WithMany().HasForeignKey(x=>x.MaNhom).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.MaTaiKhoan).OnDelete(DeleteBehavior.NoAction);
        e.HasIndex(x=>new{x.MaTaiKhoan,x.TrangThai},"IX_TVDH_CaNhan");
    }
}

public sealed class TinNhanDongHanhConfiguration : IEntityTypeConfiguration<TinNhanDongHanh>
{
    public void Configure(EntityTypeBuilder<TinNhanDongHanh> e)
    {
        e.ToTable("TinNhanDongHanh",t=>{t.HasCheckConstraint("CK_TNDH_NoiDung","LEN(LTRIM(RTRIM(NoiDung)))>0");});
        e.HasKey(x=>x.MaTinNhan);
        e.Property(x=>x.MaTinNhan).HasColumnType("bigint").UseIdentityColumn();
        e.Property(x=>x.MaNhom).HasColumnType("bigint");
        e.Property(x=>x.MaNguoiGui).HasColumnType("bigint");
        e.Property(x=>x.NoiDung).HasColumnType("nvarchar(2000)");
        e.Property(x=>x.LaThongBao).HasColumnType("bit");
        e.Property(x=>x.DaAn).HasColumnType("bit");
        e.Property(x=>x.MaNguoiAn).HasColumnType("bigint");
        e.Property(x=>x.SubmissionId).HasColumnType("uniqueidentifier");
        e.Property(x=>x.ThoiDiemTao).HasColumnType("datetime2(0)");
        e.Property(x=>x.RowVersion).HasColumnType("rowversion").IsRowVersion();
        e.HasOne<NhomDongHanh>().WithMany().HasForeignKey(x=>x.MaNhom).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.MaNguoiGui).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.MaNguoiAn).OnDelete(DeleteBehavior.NoAction);
        e.HasIndex(x=>new{x.MaNhom,x.MaTinNhan},"IX_TNDH_Nhom");
        e.HasIndex(x=>new{x.MaNhom,x.MaNguoiGui,x.SubmissionId},"UX_TNDH_Gui").IsUnique();
    }
}

public sealed class XacNhanThongBaoNhomConfiguration : IEntityTypeConfiguration<XacNhanThongBaoNhom>
{
    public void Configure(EntityTypeBuilder<XacNhanThongBaoNhom> e)
    {
        e.ToTable("XacNhanThongBaoNhom");
        e.HasKey(x=>new{x.MaTinNhan,x.MaTaiKhoan});
        e.Property(x=>x.MaTinNhan).HasColumnType("bigint").ValueGeneratedNever();
        e.Property(x=>x.MaTaiKhoan).HasColumnType("bigint").ValueGeneratedNever();
        e.Property(x=>x.ThoiDiemDoc).HasColumnType("datetime2(0)");
        e.HasOne<TinNhanDongHanh>().WithMany().HasForeignKey(x=>x.MaTinNhan).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.MaTaiKhoan).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class BinhChonDongHanhConfiguration : IEntityTypeConfiguration<BinhChonDongHanh>
{
    public void Configure(EntityTypeBuilder<BinhChonDongHanh> e)
    {
        e.ToTable("BinhChonDongHanh",t=>{t.HasCheckConstraint("CK_BCDH_TrangThai","TrangThai IN ('Mo','DaChot','DaHuy')");t.HasCheckConstraint("CK_BCDH_KetQua","(TrangThai='DaChot' AND LuaChonChot IS NOT NULL AND LuaChonChot BETWEEN 1 AND 5) OR (TrangThai<>'DaChot' AND LuaChonChot IS NULL)");t.HasCheckConstraint("CK_BCDH_Han","HanChot>ThoiDiemTao");});
        e.HasKey(x=>x.MaBinhChon);
        e.Property(x=>x.MaBinhChon).HasColumnType("bigint").UseIdentityColumn();
        e.Property(x=>x.MaNhom).HasColumnType("bigint");
        e.Property(x=>x.MaNguoiTao).HasColumnType("bigint");
        e.Property(x=>x.TieuDe).HasColumnType("nvarchar(200)");
        e.Property(x=>x.HanChot).HasColumnType("datetime2(0)");
        e.Property(x=>x.TrangThai).HasColumnType("varchar(30)");
        e.Property(x=>x.LuaChonChot).HasColumnType("int");
        e.Property(x=>x.ThoiDiemTao).HasColumnType("datetime2(0)");
        e.Property(x=>x.ThoiDiemCapNhat).HasColumnType("datetime2(0)");
        e.Property(x=>x.RowVersion).HasColumnType("rowversion").IsRowVersion();
        e.HasOne<NhomDongHanh>().WithMany().HasForeignKey(x=>x.MaNhom).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.MaNguoiTao).OnDelete(DeleteBehavior.NoAction);
        e.HasIndex(x=>new{x.MaNhom,x.ThoiDiemTao},"IX_BCDH_Nhom");
    }
}

public sealed class LuaChonBinhChonConfiguration : IEntityTypeConfiguration<LuaChonBinhChon>
{
    public void Configure(EntityTypeBuilder<LuaChonBinhChon> e)
    {
        e.ToTable("LuaChonBinhChon",t=>{t.HasCheckConstraint("CK_LCBDH_ThuTu","ThuTu BETWEEN 1 AND 5");});
        e.HasKey(x=>new{x.MaBinhChon,x.ThuTu});
        e.Property(x=>x.MaBinhChon).HasColumnType("bigint").ValueGeneratedNever();
        e.Property(x=>x.ThuTu).HasColumnType("int").ValueGeneratedNever();
        e.Property(x=>x.NoiDung).HasColumnType("nvarchar(200)");
        e.HasOne<BinhChonDongHanh>().WithMany().HasForeignKey(x=>x.MaBinhChon).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class PhieuBinhChonConfiguration : IEntityTypeConfiguration<PhieuBinhChon>
{
    public void Configure(EntityTypeBuilder<PhieuBinhChon> e)
    {
        e.ToTable("PhieuBinhChon");
        e.HasKey(x=>new{x.MaBinhChon,x.MaTaiKhoan});
        e.Property(x=>x.MaBinhChon).HasColumnType("bigint").ValueGeneratedNever();
        e.Property(x=>x.MaTaiKhoan).HasColumnType("bigint").ValueGeneratedNever();
        e.Property(x=>x.ThuTu).HasColumnType("int");
        e.Property(x=>x.ThoiDiemCapNhat).HasColumnType("datetime2(0)");
        e.Property(x=>x.RowVersion).HasColumnType("rowversion").IsRowVersion();
        e.HasOne<LuaChonBinhChon>().WithMany().HasForeignKey(x=>new{x.MaBinhChon,x.ThuTu}).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.MaTaiKhoan).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class HoatDongNhomConfiguration : IEntityTypeConfiguration<HoatDongNhom>
{
    public void Configure(EntityTypeBuilder<HoatDongNhom> e)
    {
        e.ToTable("HoatDongNhom");
        e.HasKey(x=>x.MaHoatDong);
        e.Property(x=>x.MaHoatDong).HasColumnType("bigint").UseIdentityColumn();
        e.Property(x=>x.MaNhom).HasColumnType("bigint");
        e.Property(x=>x.MaNguoiTao).HasColumnType("bigint");
        e.Property(x=>x.MaBinhChon).HasColumnType("bigint");
        e.Property(x=>x.NoiDung).HasColumnType("nvarchar(200)");
        e.Property(x=>x.Ngay).HasColumnType("date");
        e.Property(x=>x.Gio).HasColumnType("time(0)");
        e.Property(x=>x.DaHuy).HasColumnType("bit");
        e.Property(x=>x.ThoiDiemCapNhat).HasColumnType("datetime2(0)");
        e.Property(x=>x.RowVersion).HasColumnType("rowversion").IsRowVersion();
        e.HasOne<NhomDongHanh>().WithMany().HasForeignKey(x=>x.MaNhom).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.MaNguoiTao).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<BinhChonDongHanh>().WithMany().HasForeignKey(x=>x.MaBinhChon).OnDelete(DeleteBehavior.NoAction);
        e.HasIndex(x=>new{x.MaNhom,x.Ngay},"IX_HDNhom_Ngay");
        e.HasIndex(x=>x.MaBinhChon,"UX_HDNhom_BinhChon").IsUnique().HasFilter("MaBinhChon IS NOT NULL");
    }
}

public sealed class ViecNhomConfiguration : IEntityTypeConfiguration<ViecNhom>
{
    public void Configure(EntityTypeBuilder<ViecNhom> e)
    {
        e.ToTable("ViecNhom");
        e.HasKey(x=>x.MaViec);
        e.Property(x=>x.MaViec).HasColumnType("bigint").UseIdentityColumn();
        e.Property(x=>x.MaNhom).HasColumnType("bigint");
        e.Property(x=>x.MaNguoiTao).HasColumnType("bigint");
        e.Property(x=>x.MaPhuTrach).HasColumnType("bigint");
        e.Property(x=>x.NoiDung).HasColumnType("nvarchar(200)");
        e.Property(x=>x.Han).HasColumnType("date");
        e.Property(x=>x.DaXong).HasColumnType("bit");
        e.Property(x=>x.DaHuy).HasColumnType("bit");
        e.Property(x=>x.ThoiDiemCapNhat).HasColumnType("datetime2(0)");
        e.Property(x=>x.RowVersion).HasColumnType("rowversion").IsRowVersion();
        e.HasOne<NhomDongHanh>().WithMany().HasForeignKey(x=>x.MaNhom).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.MaNguoiTao).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.MaPhuTrach).OnDelete(DeleteBehavior.NoAction);
        e.HasIndex(x=>new{x.MaNhom,x.Han},"IX_ViecNhom_Nhom");
    }
}

public sealed class KhoanChiNhomConfiguration : IEntityTypeConfiguration<KhoanChiNhom>
{
    public void Configure(EntityTypeBuilder<KhoanChiNhom> e)
    {
        e.ToTable("KhoanChiNhom",t=>{t.HasCheckConstraint("CK_ChiNhom_Tien","SoTien>0 AND SoTien<=1000000000");});
        e.HasKey(x=>x.MaKhoanChi);
        e.Property(x=>x.MaKhoanChi).HasColumnType("bigint").UseIdentityColumn();
        e.Property(x=>x.MaNhom).HasColumnType("bigint");
        e.Property(x=>x.MaNguoiTao).HasColumnType("bigint");
        e.Property(x=>x.MaNguoiTra).HasColumnType("bigint");
        e.Property(x=>x.NoiDung).HasColumnType("nvarchar(200)");
        e.Property(x=>x.SoTien).HasColumnType("decimal(18,2)");
        e.Property(x=>x.NgayChi).HasColumnType("date");
        e.Property(x=>x.ChiaDeu).HasColumnType("bit");
        e.Property(x=>x.DaHuy).HasColumnType("bit");
        e.Property(x=>x.SubmissionId).HasColumnType("uniqueidentifier");
        e.Property(x=>x.ThoiDiemTao).HasColumnType("datetime2(0)");
        e.Property(x=>x.ThoiDiemCapNhat).HasColumnType("datetime2(0)");
        e.Property(x=>x.RowVersion).HasColumnType("rowversion").IsRowVersion();
        e.HasOne<NhomDongHanh>().WithMany().HasForeignKey(x=>x.MaNhom).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.MaNguoiTao).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.MaNguoiTra).OnDelete(DeleteBehavior.NoAction);
        e.HasIndex(x=>new{x.MaNhom,x.NgayChi},"IX_ChiNhom_Nhom");
        e.HasIndex(x=>new{x.MaNhom,x.MaNguoiTao,x.SubmissionId},"UX_ChiNhom_Gui").IsUnique();
    }
}

public sealed class PhanBoChiNhomConfiguration : IEntityTypeConfiguration<PhanBoChiNhom>
{
    public void Configure(EntityTypeBuilder<PhanBoChiNhom> e)
    {
        e.ToTable("PhanBoChiNhom",t=>{t.HasCheckConstraint("CK_PhanBoNhom_Tien","SoTien>0 AND SoTien<=1000000000");});
        e.HasKey(x=>new{x.MaKhoanChi,x.MaTaiKhoan});
        e.Property(x=>x.MaKhoanChi).HasColumnType("bigint").ValueGeneratedNever();
        e.Property(x=>x.MaTaiKhoan).HasColumnType("bigint").ValueGeneratedNever();
        e.Property(x=>x.SoTien).HasColumnType("decimal(18,2)");
        e.HasOne<KhoanChiNhom>().WithMany().HasForeignKey(x=>x.MaKhoanChi).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.MaTaiKhoan).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class HoanTraNhomConfiguration : IEntityTypeConfiguration<HoanTraNhom>
{
    public void Configure(EntityTypeBuilder<HoanTraNhom> e)
    {
        e.ToTable("HoanTraNhom",t=>{t.HasCheckConstraint("CK_HoanTraNhom_Tien","SoTien>0 AND SoTien<=1000000000");t.HasCheckConstraint("CK_HoanTraNhom_Nguoi","MaNguoiGui<>MaNguoiNhan");t.HasCheckConstraint("CK_HoanTraNhom_TT","TrangThai IN ('ChoXacNhan','DaXacNhan','DaHuy','TuChoi')");});
        e.HasKey(x=>x.MaHoanTra);
        e.Property(x=>x.MaHoanTra).HasColumnType("bigint").UseIdentityColumn();
        e.Property(x=>x.MaNhom).HasColumnType("bigint");
        e.Property(x=>x.MaNguoiGui).HasColumnType("bigint");
        e.Property(x=>x.MaNguoiNhan).HasColumnType("bigint");
        e.Property(x=>x.SoTien).HasColumnType("decimal(18,2)");
        e.Property(x=>x.TrangThai).HasColumnType("varchar(30)");
        e.Property(x=>x.GhiChu).HasColumnType("nvarchar(300)");
        e.Property(x=>x.SubmissionId).HasColumnType("uniqueidentifier");
        e.Property(x=>x.ThoiDiemTao).HasColumnType("datetime2(0)");
        e.Property(x=>x.ThoiDiemCapNhat).HasColumnType("datetime2(0)");
        e.Property(x=>x.RowVersion).HasColumnType("rowversion").IsRowVersion();
        e.HasOne<NhomDongHanh>().WithMany().HasForeignKey(x=>x.MaNhom).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.MaNguoiGui).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.MaNguoiNhan).OnDelete(DeleteBehavior.NoAction);
        e.HasIndex(x=>new{x.MaNhom,x.ThoiDiemTao},"IX_HoanTraNhom_Nhom");
        e.HasIndex(x=>new{x.MaNhom,x.MaNguoiGui,x.SubmissionId},"UX_HoanTraNhom_Gui").IsUnique();
        e.HasIndex(x=>new{x.MaNhom,x.MaNguoiGui,x.MaNguoiNhan},"UX_HoanTraNhom_Cho").IsUnique().HasFilter("TrangThai='ChoXacNhan'");
    }
}

public sealed class ChanDongHanhConfiguration : IEntityTypeConfiguration<ChanDongHanh>
{
    public void Configure(EntityTypeBuilder<ChanDongHanh> e)
    {
        e.ToTable("ChanDongHanh",t=>{t.HasCheckConstraint("CK_ChanDH_Nguoi","MaNguoiChan<>MaBiChan");});
        e.HasKey(x=>new{x.MaNguoiChan,x.MaBiChan});
        e.Property(x=>x.MaNguoiChan).HasColumnType("bigint").ValueGeneratedNever();
        e.Property(x=>x.MaBiChan).HasColumnType("bigint").ValueGeneratedNever();
        e.Property(x=>x.DaChan).HasColumnType("bit");
        e.Property(x=>x.ThoiDiemCapNhat).HasColumnType("datetime2(0)");
        e.Property(x=>x.RowVersion).HasColumnType("rowversion").IsRowVersion();
        e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.MaNguoiChan).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.MaBiChan).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class BaoCaoDongHanhConfiguration : IEntityTypeConfiguration<BaoCaoDongHanh>
{
    public void Configure(EntityTypeBuilder<BaoCaoDongHanh> e)
    {
        e.ToTable("BaoCaoDongHanh",t=>{t.HasCheckConstraint("CK_BaoCaoDH_Loai","Loai IN ('Spam','QuayRoi','LuaDao','Khac')");t.HasCheckConstraint("CK_BaoCaoDH_TT","TrangThai IN ('ChoXuLy','DaXuLy')");t.HasCheckConstraint("CK_BaoCaoDH_QuyetDinh","(TrangThai='ChoXuLy' AND QuyetDinh IS NULL AND MaAdmin IS NULL) OR (TrangThai='DaXuLy' AND QuyetDinh IN ('KhongViPham','AnNoiDung','AnNhom','DaNhacNho') AND MaAdmin IS NOT NULL AND LyDo IS NOT NULL)");});
        e.HasKey(x=>x.MaBaoCao);
        e.Property(x=>x.MaBaoCao).HasColumnType("bigint").UseIdentityColumn();
        e.Property(x=>x.MaNguoiGui).HasColumnType("bigint");
        e.Property(x=>x.MaNhom).HasColumnType("bigint");
        e.Property(x=>x.MaTinNhan).HasColumnType("bigint");
        e.Property(x=>x.Loai).HasColumnType("varchar(30)");
        e.Property(x=>x.MoTa).HasColumnType("nvarchar(2000)");
        e.Property(x=>x.BangChung).HasColumnType("nvarchar(2000)");
        e.Property(x=>x.TrangThai).HasColumnType("varchar(30)");
        e.Property(x=>x.QuyetDinh).HasColumnType("varchar(30)");
        e.Property(x=>x.LyDo).HasColumnType("nvarchar(500)");
        e.Property(x=>x.MaAdmin).HasColumnType("bigint");
        e.Property(x=>x.ThoiDiemTao).HasColumnType("datetime2(0)");
        e.Property(x=>x.ThoiDiemCapNhat).HasColumnType("datetime2(0)");
        e.Property(x=>x.RowVersion).HasColumnType("rowversion").IsRowVersion();
        e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.MaNguoiGui).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<NhomDongHanh>().WithMany().HasForeignKey(x=>x.MaNhom).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<TinNhanDongHanh>().WithMany().HasForeignKey(x=>x.MaTinNhan).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<TaiKhoan>().WithMany().HasForeignKey(x=>x.MaAdmin).OnDelete(DeleteBehavior.NoAction);
        e.HasIndex(x=>new{x.TrangThai,x.ThoiDiemTao},"IX_BaoCaoDH_TT");
    }
}

