namespace HoTroDuLich.Web.Services;

public static class DanhMuc
{
    public static readonly string[] LoaiDichVu = ["LuuTru", "AnUong", "PhuongTien", "HoatDong", "Khac"];
    public static readonly string[] LoaiTinhHuong = ["DiemDenKhongPhucVu", "ThoiTiet", "DichVuCoVanDe", "Khac"];
    public static readonly string[] HinhThucDi = ["TuTuc", "Phuot", "NghiDuong", "Khac"];
    public const int KichThuocTrang = 20;
    public static string ChuanHoa(string? value) => value?.Trim() switch
    {
        "Lưu trú" => "LuuTru", "Ăn uống" => "AnUong", "Di chuyển" or "Phương tiện" => "PhuongTien",
        "Hoạt động" or "Tham quan" => "HoatDong", "Địa điểm" => "DiaDiem",
        "Thời tiết" => "ThoiTiet", "Dịch vụ" or "Dịch vụ có vấn đề" => "DichVuCoVanDe",
        "Điểm đến" or "Điểm đến không phục vụ" => "DiemDenKhongPhucVu",
        "Tự túc" => "TuTuc", "Phượt" => "Phuot", "Nghỉ dưỡng" => "NghiDuong", "Khác" => "Khac",
        _ => value?.Trim() ?? ""
    };
    public static string Ten(string? value) => ChuanHoa(value) switch
    {
        "LuuTru" => "Lưu trú", "AnUong" => "Ăn uống", "PhuongTien" => "Phương tiện",
        "HoatDong" => "Hoạt động", "DiaDiem" => "Địa điểm", "ThoiTiet" => "Thời tiết",
        "DichVuCoVanDe" => "Dịch vụ có vấn đề", "DiemDenKhongPhucVu" => "Điểm đến không phục vụ",
        "TuTuc" => "Tự túc", "Phuot" => "Phượt", "NghiDuong" => "Nghỉ dưỡng", "Khac" => "Khác", _ => value ?? ""
    };
}
