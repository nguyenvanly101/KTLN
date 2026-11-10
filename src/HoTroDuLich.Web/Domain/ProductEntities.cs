namespace HoTroDuLich.Web.Domain;

public sealed class LuaChonDaLuu
{
    public long MaLuaChon { get; set; }
    public long MaTaiKhoan { get; set; }
    public long? MaDichVu { get; set; }
    public long? MaDiaDiem { get; set; }
    public string TenMuc { get; set; } = "";
    public DateTime ThoiDiemLuu { get; set; } = DateTime.UtcNow;
}
public sealed class MucChuanBi
{
    public long MaMuc { get; set; }
    public long MaKeHoach { get; set; }
    public string NoiDung { get; set; } = "";
    public string Nhom { get; set; } = "Khac";
    public DateOnly? HanHoanThanh { get; set; }
    public bool DaHoanThanh { get; set; }
    public bool NhacHan { get; set; }
    public DateOnly? NgayDaNhac { get; set; }
    public DateTime ThoiDiemTao { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];
}
public sealed class KhoanChiChuyenDi
{
    public long MaKhoanChi { get; set; }
    public long MaKeHoach { get; set; }
    public string NoiDung { get; set; } = "";
    public string LoaiChi { get; set; } = "Khac";
    public decimal SoTien { get; set; }
    public DateOnly NgayChi { get; set; }
    public string? GhiChu { get; set; }
    public Guid MaLuotGhi { get; set; } = Guid.NewGuid();
    public DateTime ThoiDiemTao { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];
}
