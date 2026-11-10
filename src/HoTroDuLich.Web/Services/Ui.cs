using Microsoft.AspNetCore.Html;
using HoTroDuLich.Web.Domain;
namespace HoTroDuLich.Web.Services;

public static class Ui
{
    public static string ActualMoney(decimal amount)=>amount.ToString(amount==decimal.Truncate(amount)?"N0":"N2",System.Globalization.CultureInfo.GetCultureInfo("vi-VN"))+" ₫";
    public static string[] ServicePhotos(string? json) {try{return System.Text.Json.JsonSerializer.Deserialize<string[]>(json??"[]")??[];}catch(System.Text.Json.JsonException){return [];}}
    public static string ServiceImageLabel(DichVuDuLich service)=>ServicePhotos(service.AnhJson).Length>0?"Ảnh dịch vụ":"Ảnh minh họa";
    public static string ProviderImage(DichVuDuLich service)=>ServicePhotos(service.AnhJson).Length>0?"/service-photos/review/"+service.MaDichVu+"/0":ServiceImage(service);
    public static string PlaceQuery(DiaDiemDuLich place) => string.Join(", ", new[] { place.TenDiaDiem, place.DiaChi, place.KhuVuc, "Việt Nam" }.Where(x => !string.IsNullOrWhiteSpace(x)));
    public static string MapSearch(string query) => "https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString(query);
    public static string Directions(string destination) => "https://www.google.com/maps/dir/?api=1&destination=" + Uri.EscapeDataString(destination);
    public static DateTime LocalTime(DateTime utc)=>TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc,DateTimeKind.Utc),TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows()?"SE Asia Standard Time":"Asia/Ho_Chi_Minh"));
    public static string Initial(string name)=>string.IsNullOrWhiteSpace(name)?"?":System.Globalization.StringInfo.GetNextTextElement(name.Trim());

    public static IHtmlContent Icon(string name) => new HtmlString($"<svg class=\"icon\" aria-hidden=\"true\"><use href=\"/images/icons.svg#{name}\"></use></svg>");
    public static string DestinationImage(string region) => region.Contains("Hội An") ? "/images/hoian.jpg" : region.Contains("Hải Phòng") || region.Contains("Lan Hạ") || region.Contains("Hạ Long") ? "/images/vietnam-bay.jpg" : "/images/dalat.jpg";
    public static string ServiceImage(DichVuDuLich s) => ServicePhotos(s.AnhJson).Length>0 ? "/service-photos/"+s.MaDichVu+"/0" : DanhMuc.ChuanHoa(s.LoaiDichVu) == "LuuTru" ? "/images/stay.jpg" : DanhMuc.ChuanHoa(s.LoaiDichVu) == "AnUong" ? "/images/food.jpg" : DestinationImage(s.KhuVucPhucVu);
    public static string CategoryIcon(string type) => DanhMuc.ChuanHoa(type) switch { "LuuTru" => "bed", "AnUong" => "food", "PhuongTien" => "car", "HoatDong" => "compass", _ => "pin" };
}
