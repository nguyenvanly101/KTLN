using HoTroDuLich.Web.Domain;

namespace HoTroDuLich.Web.Services;

/// <summary>Cach hien thi gon cho yeu cau dat dich vu: 10 trang thai luu trong co so du lieu gop thanh 6 trang thai nguoi dung thay.</summary>
public static class BookingUi
{
    public static string Group(TrangThaiYeuCau s) => s switch
    {
        TrangThaiYeuCau.ChoPhanHoi or TrangThaiYeuCau.ChoKiemTra => "wait",
        TrangThaiYeuCau.DeXuatKhac => "prop",
        TrangThaiYeuCau.DaXacNhan => "ok",
        TrangThaiYeuCau.HoanThanh or TrangThaiYeuCau.ThucHienMotPhan => "used",
        TrangThaiYeuCau.KhongThucHien => "noshow",
        _ => "off"
    };
    public static string Label(TrangThaiYeuCau s) => Group(s) switch
    {
        "wait" => "Chờ phản hồi", "prop" => "Cơ sở đề xuất", "ok" => "Đã xác nhận", "used" => "Đã dùng", "noshow" => "Khách không đến", _ => "Đã hủy"
    };
    /// <summary>Buoc tren thanh tien trinh: 0 da gui, 1 co so phan hoi, 2 da xac nhan, 3 da dung; -1 khi da huy.</summary>
    public static int Step(TrangThaiYeuCau s) => Group(s) switch { "wait" => 1, "prop" => 1, "ok" => 2, "used" or "noshow" => 3, _ => -1 };
    /// <summary>Bo loc tren danh sach: can-tra-loi, sap-toi, da-dung, da-huy.</summary>
    public static string Filter(YeuCauDatDichVu y, bool provider, DateOnly today) => Group(y.TrangThai) switch
    {
        "wait" => provider ? "can-tra-loi" : "sap-toi",
        "prop" => provider ? "sap-toi" : "can-tra-loi",
        "ok" => provider && y.NgayBatDau <= today ? "can-tra-loi" : "sap-toi",
        "used" or "noshow" => "da-dung",
        _ => "da-huy"
    };
    public static readonly (string Key, string Label)[] Filters = [("can-tra-loi", "Cần trả lời"), ("sap-toi", "Sắp tới"), ("da-dung", "Đã dùng"), ("da-huy", "Đã hủy")];
    public static string Dates(YeuCauDatDichVu y) => y.NgayBatDau.ToString("dd/MM/yyyy") + (y.NgayKetThuc is DateOnly e && e != y.NgayBatDau ? " – " + e.ToString("dd/MM/yyyy") : "");
}
