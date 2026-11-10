using System.ComponentModel.DataAnnotations;

namespace HoTroDuLich.Web.Domain;
public partial class TaiKhoan
{
    public long MaTaiKhoan { get; set; }
    public string Email { get; set; } = "";
    public string? SoDienThoai { get; set; }
    public string MatKhauBam { get; set; } = "";
    public VaiTro VaiTro { get; set; } = default;
    public TrangThaiTaiKhoan TrangThai { get; set; } = default;
    public Guid DauPhien { get; set; } = Guid.NewGuid();
    public bool PhaiDoiMatKhau { get; set; }
    public DateTime ThoiDiemTao { get; set; } = DateTime.UtcNow;
    public DateTime? ThoiDiemDangNhapCuoi { get; set; }
    public DateTime? ThoiDiemNgung { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public partial class HoSoNguoiDung
{
    public long MaHoSo { get; set; }
    public long MaTaiKhoan { get; set; }
    public string TenHienThi { get; set; } = "";
    public string? AnhDaiDien { get; set; }
    public string? GioiThieu { get; set; }
    public string? NoiSinhSong { get; set; }
    public string? SoThich { get; set; }
    public ThongTinChiaSe ThongTinChiaSe { get; set; } = default;
    public DateTime ThoiDiemCapNhat { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];
}

public partial class NhaCungCapDichVu
{
    public long MaNhaCungCap { get; set; }
    public long MaTaiKhoan { get; set; }
    public string TenDonVi { get; set; } = "";
    public string LoaiHinh { get; set; } = "";
    public string NguoiLienHe { get; set; } = "";
    public string DiaChi { get; set; } = "";
    public string SoDienThoai { get; set; } = "";
    public string EmailLienHe { get; set; } = "";
    public TrangThaiXacMinh TrangThaiXacMinh { get; set; } = default;
    public TrangThaiQuyenHoatDong? TrangThaiQuyenHoatDong { get; set; }
    public DateTime ThoiDiemTao { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];
}

public partial class HoSoXacMinhNhaCungCap
{
    public long MaHoSo { get; set; }
    public long MaNhaCungCap { get; set; }
    public string NoiDungDangKy { get; set; } = "";
    public string NguoiDaiDien { get; set; } = "";
    public DateTime ThoiDiemNop { get; set; } = DateTime.UtcNow;
    public int SoLanBoSung { get; set; }
    public TrangThaiHoSoXacMinh TrangThaiXuLy { get; set; } = default;
    public byte[] RowVersion { get; set; } = [];
}

public partial class QuyetDinhXacMinh
{
    public long MaQuyetDinh { get; set; }
    public long MaHoSo { get; set; }
    public int LanXuLy { get; set; }
    public KetQuaXacMinh KetQua { get; set; } = default;
    public string? KetQuaXacMinh { get; set; }
    public string? LyDo { get; set; }
    public long MaAdminDuyet { get; set; }
    public DateTime ThoiDiemDuyet { get; set; } = DateTime.UtcNow;
}

public partial class TaiLieuXacMinh
{
    public long MaTaiLieu { get; set; }
    public long MaHoSo { get; set; }
    public string TenTepGoc { get; set; } = "";
    public string DuongDan { get; set; } = "";
    public LoaiTep LoaiTep { get; set; } = default;
    public int KichThuoc { get; set; }
    public int LanNop { get; set; }
    public DateTime ThoiDiemTai { get; set; } = DateTime.UtcNow;
}

public partial class QuyetDinhQuyenHoatDong
{
    public long MaQuyetDinh { get; set; }
    public long MaNhaCungCap { get; set; }
    public TrangThaiQuyenHoatDong TrangThaiTruoc { get; set; } = default;
    public TrangThaiQuyenHoatDong TrangThaiSau { get; set; } = default;
    public string LyDo { get; set; } = "";
    public string CanCu { get; set; } = "";
    public DateOnly? ThoiHan { get; set; }
    public long MaAdmin { get; set; }
    public DateTime ThoiDiem { get; set; } = DateTime.UtcNow;
}

public partial class GiaiTrinhNhaCungCap
{
    public long MaGiaiTrinh { get; set; }
    public long MaQuyetDinh { get; set; }
    public string NoiDung { get; set; } = "";
    public DateTime ThoiDiemGui { get; set; } = DateTime.UtcNow;
    public TrangThaiGiaiTrinh TrangThai { get; set; } = default;
    public string? KetQuaXemXet { get; set; }
    public long? MaAdminXemXet { get; set; }
    public DateTime? ThoiDiemXemXet { get; set; }
}

public partial class TaiLieuGiaiTrinh
{
    public long MaTaiLieu { get; set; }
    public long MaGiaiTrinh { get; set; }
    public string TenTepGoc { get; set; } = "";
    public string DuongDan { get; set; } = "";
    public LoaiTep LoaiTep { get; set; } = default;
    public int KichThuoc { get; set; }
    public DateTime ThoiDiemTai { get; set; } = DateTime.UtcNow;
}

public partial class DiaDiemDuLich
{
    public long MaDiaDiem { get; set; }
    public string TenDiaDiem { get; set; } = "";
    public string LoaiDiaDiem { get; set; } = "";
    public string KhuVuc { get; set; } = "";
    public string? DiaChi { get; set; }
    public string? MoTa { get; set; }
    public bool DangHienThi { get; set; }
}

public partial class DichVuDuLich
{
    public long MaDichVu { get; set; }
    public long MaNhaCungCap { get; set; }
    public string TenDichVu { get; set; } = "";
    public string LoaiDichVu { get; set; } = "";
    public string? MoTa { get; set; }
    public string KhuVucPhucVu { get; set; } = "";
    public long? MaDiaDiem { get; set; }
    public decimal? GiaThamKhao { get; set; }
    public string? ThoiGianPhucVu { get; set; }
    public string? DieuKienPhucVu { get; set; }
    public DateOnly? TamNgungTu { get; set; }
    public DateOnly? TamNgungDen { get; set; }
    public string? LyDoTamNgung { get; set; }
    public TrangThaiCongBo TrangThaiCongBo { get; set; } = default;
    public TrangThaiTiepNhan TrangThaiTiepNhan { get; set; } = default;
    public long? MaQuyetDinhKhongTiepNhan { get; set; }
    public int? PhienBanCongBo { get; set; }
    public DateTime ThoiDiemTao { get; set; } = DateTime.UtcNow;
    public DateTime ThoiDiemCapNhat { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];
}

public partial class BanCapNhatDichVu
{
    public long MaBanCapNhat { get; set; }
    public long MaDichVu { get; set; }
    public int SoPhienBan { get; set; }
    public string TenDichVu { get; set; } = "";
    public string LoaiDichVu { get; set; } = "";
    public string? MoTa { get; set; }
    public string KhuVucPhucVu { get; set; } = "";
    public long? MaDiaDiem { get; set; }
    public decimal? GiaThamKhao { get; set; }
    public string? ThoiGianPhucVu { get; set; }
    public string? DieuKienPhucVu { get; set; }
    public DateOnly? TamNgungTu { get; set; }
    public DateOnly? TamNgungDen { get; set; }
    public string? LyDoTamNgung { get; set; }
    public TrangThaiDuyet TrangThaiDuyet { get; set; } = default;
    public string? LyDo { get; set; }
    public long? MaAdminDuyet { get; set; }
    public DateTime ThoiDiemGui { get; set; } = DateTime.UtcNow;
    public DateTime? ThoiDiemDuyet { get; set; }
}

public partial class KeHoachChuyenDi
{
    public long MaKeHoach { get; set; }
    public long MaTaiKhoan { get; set; }
    public string TenKeHoach { get; set; } = "";
    public string NoiDen { get; set; } = "";
    public DateOnly NgayBatDau { get; set; }
    public DateOnly NgayKetThuc { get; set; }
    public decimal? NganSachDuKien { get; set; }
    public TrangThaiKeHoach TrangThai { get; set; } = default;
    public DateTime ThoiDiemTao { get; set; } = DateTime.UtcNow;
    public DateTime ThoiDiemCapNhat { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];
}

public partial class HoatDongLichTrinh
{
    public long MaHoatDong { get; set; }
    public long MaKeHoach { get; set; }
    public DateOnly Ngay { get; set; }
    public TimeOnly? Gio { get; set; }
    public int ThuTu { get; set; }
    public long? MaDiaDiem { get; set; }
    public long? MaDichVu { get; set; }
    public string NoiDung { get; set; } = "";
    public decimal? ChiPhiDuKien { get; set; }
}

public partial class TinhHuongPhatSinh
{
    public long MaTinhHuong { get; set; }
    public long MaKeHoach { get; set; }
    public string LoaiTinhHuong { get; set; } = "";
    public string MoTa { get; set; } = "";
    public string? ViTri { get; set; }
    public DateTime ThoiDiemGhiNhan { get; set; } = DateTime.UtcNow;
    public TrangThaiXuLyTinhHuong TrangThaiXuLy { get; set; } = default;
    public string? KetQuaXuLy { get; set; }
    public DateTime ThoiDiemCapNhat { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];
}

public partial class TinhHuongHoatDong
{
    public long MaTinhHuong { get; set; }
    public long MaHoatDong { get; set; }
}

public partial class PhuongAnXuLy
{
    public long MaPhuongAn { get; set; }
    public long MaTinhHuong { get; set; }
    public string NoiDung { get; set; } = "";
    public DateTime ThoiDiemChon { get; set; } = DateTime.UtcNow;
    public bool LaHienHanh { get; set; }
}

public partial class NhuCauDongHanh
{
    public long MaNhuCau { get; set; }
    public long MaTaiKhoan { get; set; }
    public string NoiDung { get; set; } = "";
    public string DiemDen { get; set; } = "";
    public DateOnly NgayBatDau { get; set; }
    public DateOnly NgayKetThuc { get; set; }
    public decimal? NganSachDuKien { get; set; }
    public string? HinhThucDi { get; set; }
    public string? MongMuonDongHanh { get; set; }
    public PhamViChiaSe PhamViChiaSe { get; set; } = default;
    public DateTime ThoiDiemDang { get; set; } = DateTime.UtcNow;
    public TrangThaiNhuCau TrangThaiCongBo { get; set; } = default;
    public byte[] RowVersion { get; set; } = [];
}

public partial class DeNghiKetNoiDongHanh
{
    public long MaDeNghi { get; set; }
    public long MaNhuCau { get; set; }
    public long MaNguoiGui { get; set; }
    public string? LoiGioiThieu { get; set; }
    public DateTime ThoiDiemGui { get; set; } = DateTime.UtcNow;
    public DateTime? ThoiDiemPhanHoi { get; set; }
    public TrangThaiDeNghiKetNoi TrangThai { get; set; } = default;
    public byte[] RowVersion { get; set; } = [];
}

public partial class ThoaThuanDongHanh
{
    public long MaThoaThuan { get; set; }
    public long MaDeNghi { get; set; }
    public int PhienBan { get; set; }
    public DateOnly NgayDi { get; set; }
    public DateOnly NgayVe { get; set; }
    public string NoiDen { get; set; } = "";
    public decimal? NganSach { get; set; }
    public string? PhamViDiCung { get; set; }
    public long MaNguoiDeXuat { get; set; }
    public DateTime ThoiDiemDeXuat { get; set; } = DateTime.UtcNow;
    public long? MaNguoiXacNhan { get; set; }
    public DateTime? ThoiDiemXacNhan { get; set; }
    public LoaiPhienBan LoaiPhienBan { get; set; } = default;
    public TrangThaiThoaThuan TrangThai { get; set; } = default;
    public string? LyDo { get; set; }
    public long? MaNguoiKetThuc { get; set; }
    public DateTime? ThoiDiemKetThuc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public partial class YeuCauDatDichVu
{
    public long MaYeuCau { get; set; }
    public long MaTaiKhoan { get; set; }
    public long MaDichVu { get; set; }
    public long? MaKeHoach { get; set; }
    public DateOnly NgayBatDau { get; set; }
    public DateOnly? NgayKetThuc { get; set; }
    public string? ThoiGianDenDi { get; set; }
    public int SoNguoi { get; set; }
    public int SoLuong { get; set; }
    public string? NoiDungLuaChon { get; set; }
    public string? GhiChu { get; set; }
    public decimal? ChiPhiDuKien { get; set; }
    public string? DieuKienDeXuat { get; set; }
    public int PhienBan { get; set; }
    public DateTime ThoiDiemGui { get; set; } = DateTime.UtcNow;
    public DateTime? ThoiDiemPhanHoi { get; set; }
    public string? LyDo { get; set; }
    public TrangThaiYeuCau TrangThai { get; set; } = default;
    public Guid MaLuotGui { get; set; } = Guid.NewGuid();
    public byte[] RowVersion { get; set; } = [];
}

public partial class DeNghiDieuChinhDichVu
{
    public long MaDeNghi { get; set; }
    public long MaYeuCau { get; set; }
    public LoaiDeNghi LoaiDeNghi { get; set; } = default;
    public string DieuKienCu { get; set; } = "";
    public DateOnly? NgayBatDauMoi { get; set; }
    public DateOnly? NgayKetThucMoi { get; set; }
    public int? SoNguoiMoi { get; set; }
    public int? SoLuongMoi { get; set; }
    public string? DieuKienMoi { get; set; }
    public string LyDo { get; set; } = "";
    public DateTime ThoiDiemGui { get; set; } = DateTime.UtcNow;
    public string? QuyetDinh { get; set; }
    public string? DieuKienDeXuat { get; set; }
    public DateTime? ThoiDiemPhanHoi { get; set; }
    public TrangThaiDeNghiDieuChinh TrangThai { get; set; } = default;
    public byte[] RowVersion { get; set; } = [];
}

public partial class KetQuaCungCapDichVu
{
    public long MaKetQua { get; set; }
    public long MaYeuCau { get; set; }
    public string HangMuc { get; set; } = "";
    public KetQuaThucHien KetQuaThucHien { get; set; } = default;
    public string? GhiChu { get; set; }
    public DateTime ThoiDiemGhi { get; set; } = DateTime.UtcNow;
    public string? NoiDungPhanAnh { get; set; }
    public DateTime? ThoiDiemPhanAnh { get; set; }
    public TrangThaiKiemTra TrangThaiKiemTra { get; set; } = default;
    public DateTime? ThoiDiemKiemTra { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public partial class ThongBao
{
    public long MaThongBao { get; set; }
    public long MaNguoiNhan { get; set; }
    public string LoaiThongBao { get; set; } = "";
    public string NoiDung { get; set; } = "";
    public string? DuongDan { get; set; }
    public bool DaDoc { get; set; }
    public DateTime ThoiDiemTao { get; set; } = DateTime.UtcNow;
}

public partial class NhatKyThaoTac
{
    public long MaNhatKy { get; set; }
    public long? MaTaiKhoan { get; set; }
    public string LoaiSuKien { get; set; } = "";
    public string? DoiTuong { get; set; }
    public long? MaDoiTuong { get; set; }
    public string? TrangThaiTruoc { get; set; }
    public string? TrangThaiSau { get; set; }
    public string? LyDo { get; set; }
    public string? DiaChiIP { get; set; }
    public DateTime ThoiDiem { get; set; } = DateTime.UtcNow;
}
