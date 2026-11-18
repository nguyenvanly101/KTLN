using System.Text.RegularExpressions;
namespace HoTroDuLich.Web.Services;

/// <summary>Chuan hoa chu do nguoi dung nhap (ten dich vu, ten co so): bo khoang trang thua, sua loi giu Shift kieu "HOàng".</summary>
public static partial class TextClean
{
 [GeneratedRegex(@"\s+")] private static partial Regex Spaces();
 // Tu bat dau bang 2 chu hoa tro len roi toan chu thuong: loi go (HOàng, GIa). Khong dung toi VIP, FLC, iPhone, McDonald's.
 [GeneratedRegex(@"^(\P{L}*)(\p{Lu})(\p{Lu}+)(\p{Ll}+\P{L}*)$")] private static partial Regex HeldShift();
 /// <summary>"Khách Sạn HOàng GIa" -> "Khách Sạn Hoàng Gia".</summary>
 public static string Name(string? input)
 {
  var s=Spaces().Replace((input??"").Trim()," ");
  return string.Join(' ',s.Split(' ').Select(w=>HeldShift().Replace(w,m=>m.Groups[1].Value+m.Groups[2].Value+m.Groups[3].Value.ToLowerInvariant()+m.Groups[4].Value)));
 }
}
