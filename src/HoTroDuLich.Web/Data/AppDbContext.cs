using Microsoft.EntityFrameworkCore;
using HoTroDuLich.Web.Domain;
namespace HoTroDuLich.Web.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
 public DbSet<ChiaSeChuyenDi> ChiaSeChuyenDi => Set<ChiaSeChuyenDi>();
 public DbSet<DanhGiaDichVu> DanhGiaDichVu => Set<DanhGiaDichVu>();
 public DbSet<LichPhucVuDichVu> LichPhucVuDichVu => Set<LichPhucVuDichVu>();
 public DbSet<LuaChonDaLuu> LuaChonDaLuu => Set<LuaChonDaLuu>();
 public DbSet<MucChuanBi> MucChuanBi => Set<MucChuanBi>();
 public DbSet<KhoanChiChuyenDi> KhoanChiChuyenDi => Set<KhoanChiChuyenDi>();
 public DbSet<TaiKhoan> TaiKhoan => Set<TaiKhoan>();
 public DbSet<HoSoNguoiDung> HoSoNguoiDung => Set<HoSoNguoiDung>();
 public DbSet<NhaCungCapDichVu> NhaCungCapDichVu => Set<NhaCungCapDichVu>();
 public DbSet<HoSoXacMinhNhaCungCap> HoSoXacMinhNhaCungCap => Set<HoSoXacMinhNhaCungCap>();
 public DbSet<QuyetDinhXacMinh> QuyetDinhXacMinh => Set<QuyetDinhXacMinh>();
 public DbSet<TaiLieuXacMinh> TaiLieuXacMinh => Set<TaiLieuXacMinh>();
 public DbSet<QuyetDinhQuyenHoatDong> QuyetDinhQuyenHoatDong => Set<QuyetDinhQuyenHoatDong>();
 public DbSet<GiaiTrinhNhaCungCap> GiaiTrinhNhaCungCap => Set<GiaiTrinhNhaCungCap>();
 public DbSet<TaiLieuGiaiTrinh> TaiLieuGiaiTrinh => Set<TaiLieuGiaiTrinh>();
 public DbSet<DiaDiemDuLich> DiaDiemDuLich => Set<DiaDiemDuLich>();
 public DbSet<DichVuDuLich> DichVuDuLich => Set<DichVuDuLich>();
 public DbSet<BanCapNhatDichVu> BanCapNhatDichVu => Set<BanCapNhatDichVu>();
 public DbSet<KeHoachChuyenDi> KeHoachChuyenDi => Set<KeHoachChuyenDi>();
 public DbSet<HoatDongLichTrinh> HoatDongLichTrinh => Set<HoatDongLichTrinh>();
 public DbSet<TinhHuongPhatSinh> TinhHuongPhatSinh => Set<TinhHuongPhatSinh>();
 public DbSet<TinhHuongHoatDong> TinhHuongHoatDong => Set<TinhHuongHoatDong>();
 public DbSet<PhuongAnXuLy> PhuongAnXuLy => Set<PhuongAnXuLy>();
 public DbSet<NhuCauDongHanh> NhuCauDongHanh => Set<NhuCauDongHanh>();
 public DbSet<DeNghiKetNoiDongHanh> DeNghiKetNoiDongHanh => Set<DeNghiKetNoiDongHanh>();
 public DbSet<ThoaThuanDongHanh> ThoaThuanDongHanh => Set<ThoaThuanDongHanh>();
 public DbSet<YeuCauDatDichVu> YeuCauDatDichVu => Set<YeuCauDatDichVu>();
 public DbSet<DeNghiDieuChinhDichVu> DeNghiDieuChinhDichVu => Set<DeNghiDieuChinhDichVu>();
 public DbSet<KetQuaCungCapDichVu> KetQuaCungCapDichVu => Set<KetQuaCungCapDichVu>();
 public DbSet<ThongBao> ThongBao => Set<ThongBao>();
 public DbSet<NhatKyThaoTac> NhatKyThaoTac => Set<NhatKyThaoTac>();
 public DbSet<HoSoDongHanh> HoSoDongHanh => Set<HoSoDongHanh>();
 public DbSet<NhomDongHanh> NhomDongHanh => Set<NhomDongHanh>();
 public DbSet<ThanhVienDongHanh> ThanhVienDongHanh => Set<ThanhVienDongHanh>();
 public DbSet<TinNhanDongHanh> TinNhanDongHanh => Set<TinNhanDongHanh>();
 public DbSet<XacNhanThongBaoNhom> XacNhanThongBaoNhom => Set<XacNhanThongBaoNhom>();
 public DbSet<BinhChonDongHanh> BinhChonDongHanh => Set<BinhChonDongHanh>();
 public DbSet<LuaChonBinhChon> LuaChonBinhChon => Set<LuaChonBinhChon>();
 public DbSet<PhieuBinhChon> PhieuBinhChon => Set<PhieuBinhChon>();
 public DbSet<HoatDongNhom> HoatDongNhom => Set<HoatDongNhom>();
 public DbSet<ViecNhom> ViecNhom => Set<ViecNhom>();
 public DbSet<KhoanChiNhom> KhoanChiNhom => Set<KhoanChiNhom>();
 public DbSet<PhanBoChiNhom> PhanBoChiNhom => Set<PhanBoChiNhom>();
 public DbSet<HoanTraNhom> HoanTraNhom => Set<HoanTraNhom>();
 public DbSet<ChanDongHanh> ChanDongHanh => Set<ChanDongHanh>();
 public DbSet<BaoCaoDongHanh> BaoCaoDongHanh => Set<BaoCaoDongHanh>();
 protected override void ConfigureConventions(ModelConfigurationBuilder b) {
  // FullText enum conversions
  b.Properties<KetQuaThucHien>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<KetQuaXacMinh>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<LoaiDeNghi>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<LoaiPhienBan>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<LoaiTep>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<PhamViChiaSe>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<ThongTinChiaSe>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<TrangThaiCongBo>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<TrangThaiDeNghiDieuChinh>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<TrangThaiDeNghiKetNoi>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<TrangThaiDuyet>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<TrangThaiGiaiTrinh>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<TrangThaiHoSoXacMinh>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<TrangThaiKeHoach>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<TrangThaiKiemTra>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<TrangThaiNhuCau>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<TrangThaiQuyenHoatDong>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<TrangThaiTaiKhoan>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<TrangThaiThoaThuan>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<TrangThaiTiepNhan>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<TrangThaiXacMinh>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<TrangThaiXuLyTinhHuong>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<TrangThaiYeuCau>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Properties<VaiTro>().HaveConversion<string>().HaveMaxLength(30).AreUnicode(false);
  b.Conventions.Remove(typeof(Microsoft.EntityFrameworkCore.Metadata.Conventions.ForeignKeyIndexConvention));
  b.Properties<DateTime>().HaveColumnType("datetime2(0)").HaveConversion<UtcDateTimeConverter>();
  b.Properties<DateTime?>().HaveColumnType("datetime2(0)").HaveConversion<NullableUtcDateTimeConverter>();
 }
 protected override void OnModelCreating(ModelBuilder b) {
  b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
  foreach(var entity in b.Model.GetEntityTypes()) {
   foreach(var property in entity.GetProperties().Where(p=>(Nullable.GetUnderlyingType(p.ClrType)??p.ClrType).IsEnum)) property.ValueGenerated=Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
   foreach(var fk in entity.GetForeignKeys()) {
    fk.DeleteBehavior=DeleteBehavior.NoAction;
    fk.SetConstraintName("FK_"+entity.GetTableName()+"_"+string.Join("_",fk.Properties.Select(p=>p.Name)));
   }
  }
 }
}
public sealed class UtcDateTimeConverter() : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime,DateTime>(
 v=>v.Kind==DateTimeKind.Utc?v:v.ToUniversalTime(), v=>DateTime.SpecifyKind(v,DateTimeKind.Utc));
public sealed class NullableUtcDateTimeConverter() : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?,DateTime?>(
 v=>v.HasValue?(v.Value.Kind==DateTimeKind.Utc?v:v.Value.ToUniversalTime()):null,
 v=>v.HasValue?DateTime.SpecifyKind(v.Value,DateTimeKind.Utc):null);
