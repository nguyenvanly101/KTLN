namespace HoTroDuLich.Web.Domain;

public sealed class HoSoDongHanh
{
    public long MaTaiKhoan { get; set; }
    public string PhongCach { get; set; } = "";
    public string NhipDo { get; set; } = "";
    public decimal? NganSachToiDa { get; set; }
    public string? GioiThieu { get; set; }
    public bool HienThi { get; set; }
    public DateTime ThoiDiemCapNhat { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class NhomDongHanh
{
    public long MaNhom { get; set; }
    public long MaChuNhom { get; set; }
    public long? MaKeHoach { get; set; }
    public long? MaDeNghi { get; set; }
    public string TenNhom { get; set; } = "";
    public string MoTa { get; set; } = "";
    public string KhuVuc { get; set; } = "";
    public string Loai { get; set; } = "";
    public DateOnly NgayBatDau { get; set; }
    public DateOnly NgayKetThuc { get; set; }
    public TimeOnly? GioHen { get; set; }
    public string? DiemHen { get; set; }
    public int SoNguoiToiDa { get; set; }
    public decimal? NganSachThamKhao { get; set; }
    public string PhongCach { get; set; } = "";
    public string NhipDo { get; set; } = "";
    public string TrangThai { get; set; } = "";
    public bool AnBoiQuanTri { get; set; }
    public string? LyDoAn { get; set; }
    public int PhienDiemHen { get; set; }
    public DateTime ThoiDiemTao { get; set; }
    public DateTime ThoiDiemCapNhat { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ThanhVienDongHanh
{
    public long MaNhom { get; set; }
    public long MaTaiKhoan { get; set; }
    public string TrangThai { get; set; } = "";
    public string? LoiNhan { get; set; }
    public string? PhanHoi { get; set; }
    public DateTime? ThamGiaLuc { get; set; }
    public string DiemDanh { get; set; } = "";
    public int PhienDiemHen { get; set; }
    public DateTime ThoiDiemCapNhat { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class TinNhanDongHanh
{
    public long MaTinNhan { get; set; }
    public long MaNhom { get; set; }
    public long MaNguoiGui { get; set; }
    public string NoiDung { get; set; } = "";
    public bool LaThongBao { get; set; }
    public bool DaAn { get; set; }
    public long? MaNguoiAn { get; set; }
    public Guid SubmissionId { get; set; }
    public DateTime ThoiDiemTao { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class XacNhanThongBaoNhom
{
    public long MaTinNhan { get; set; }
    public long MaTaiKhoan { get; set; }
    public DateTime ThoiDiemDoc { get; set; }
}

public sealed class BinhChonDongHanh
{
    public long MaBinhChon { get; set; }
    public long MaNhom { get; set; }
    public long MaNguoiTao { get; set; }
    public string TieuDe { get; set; } = "";
    public DateTime HanChot { get; set; }
    public string TrangThai { get; set; } = "";
    public int? LuaChonChot { get; set; }
    public DateTime ThoiDiemTao { get; set; }
    public DateTime ThoiDiemCapNhat { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class LuaChonBinhChon
{
    public long MaBinhChon { get; set; }
    public int ThuTu { get; set; }
    public string NoiDung { get; set; } = "";
}

public sealed class PhieuBinhChon
{
    public long MaBinhChon { get; set; }
    public long MaTaiKhoan { get; set; }
    public int ThuTu { get; set; }
    public DateTime ThoiDiemCapNhat { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class HoatDongNhom
{
    public long MaHoatDong { get; set; }
    public long MaNhom { get; set; }
    public long MaNguoiTao { get; set; }
    public long? MaBinhChon { get; set; }
    public string NoiDung { get; set; } = "";
    public DateOnly Ngay { get; set; }
    public TimeOnly? Gio { get; set; }
    public bool DaHuy { get; set; }
    public DateTime ThoiDiemCapNhat { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ViecNhom
{
    public long MaViec { get; set; }
    public long MaNhom { get; set; }
    public long MaNguoiTao { get; set; }
    public long? MaPhuTrach { get; set; }
    public string NoiDung { get; set; } = "";
    public DateOnly? Han { get; set; }
    public bool DaXong { get; set; }
    public bool DaHuy { get; set; }
    public DateTime ThoiDiemCapNhat { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class KhoanChiNhom
{
    public long MaKhoanChi { get; set; }
    public long MaNhom { get; set; }
    public long MaNguoiTao { get; set; }
    public long MaNguoiTra { get; set; }
    public string NoiDung { get; set; } = "";
    public decimal SoTien { get; set; }
    public DateOnly NgayChi { get; set; }
    public bool ChiaDeu { get; set; }
    public bool DaHuy { get; set; }
    public Guid SubmissionId { get; set; }
    public DateTime ThoiDiemTao { get; set; }
    public DateTime ThoiDiemCapNhat { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class PhanBoChiNhom
{
    public long MaKhoanChi { get; set; }
    public long MaTaiKhoan { get; set; }
    public decimal SoTien { get; set; }
}

public sealed class HoanTraNhom
{
    public long MaHoanTra { get; set; }
    public long MaNhom { get; set; }
    public long MaNguoiGui { get; set; }
    public long MaNguoiNhan { get; set; }
    public decimal SoTien { get; set; }
    public string TrangThai { get; set; } = "";
    public string? GhiChu { get; set; }
    public Guid SubmissionId { get; set; }
    public DateTime ThoiDiemTao { get; set; }
    public DateTime ThoiDiemCapNhat { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ChanDongHanh
{
    public long MaNguoiChan { get; set; }
    public long MaBiChan { get; set; }
    public bool DaChan { get; set; }
    public DateTime ThoiDiemCapNhat { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class BaoCaoDongHanh
{
    public long MaBaoCao { get; set; }
    public long MaNguoiGui { get; set; }
    public long MaNhom { get; set; }
    public long? MaTinNhan { get; set; }
    public string Loai { get; set; } = "";
    public string MoTa { get; set; } = "";
    public string BangChung { get; set; } = "";
    public string TrangThai { get; set; } = "";
    public string? QuyetDinh { get; set; }
    public string? LyDo { get; set; }
    public long? MaAdmin { get; set; }
    public DateTime ThoiDiemTao { get; set; }
    public DateTime ThoiDiemCapNhat { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

