namespace HoTroDuLich.Web.Domain;

public partial class HoatDongLichTrinh
{
    public int? ThoiLuongPhut { get; set; }
    public int? DiChuyenTruocPhut { get; set; }
    public DateTime? HoanThanhLuc { get; set; }
}

public sealed class ChiaSeChuyenDi
{
    public long MaChiaSe { get; set; }
    public long MaKeHoach { get; set; }
    public string TokenHash { get; set; } = "";
    public string SnapshotJson { get; set; } = "";
    public DateTime HanSuDung { get; set; }
    public bool DaThuHoi { get; set; }
    public bool ChiaSeNganSach { get; set; }
    public bool ChiaSeDichVu { get; set; }
    public DateTime ThoiDiemTao { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];
}

public sealed class DanhGiaDichVu
{
    public long MaDanhGia { get; set; }
    public long MaYeuCau { get; set; }
    public int SoSao { get; set; }
    public string NoiDung { get; set; } = "";
    public string TrangThai { get; set; } = "ChoDuyet";
    public string? LyDoKiemDuyet { get; set; }
    public string? PhanHoiNCC { get; set; }
    public string TrangThaiPhanHoi { get; set; } = "ChoDuyet";
    public string? LyDoPhanHoi { get; set; }
    public DateTime ThoiDiemTao { get; set; } = DateTime.UtcNow;
    public DateTime ThoiDiemCapNhat { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];
}

public sealed class LichPhucVuDichVu
{
    public long MaLich { get; set; }
    public long MaDichVu { get; set; }
    public DateOnly Ngay { get; set; }
    public bool TiepNhan { get; set; } = true;
    public int? SoLuongThamKhao { get; set; }
    public string? GhiChu { get; set; }
    public DateTime ThoiDiemCapNhat { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];
}
