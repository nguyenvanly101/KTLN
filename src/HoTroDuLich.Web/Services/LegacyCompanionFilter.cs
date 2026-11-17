using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HoTroDuLich.Web.Services;

/// <summary>
/// Trang Dong hanh cu (nhu cau, de nghi ket noi, thoa thuan) va nhom Cung di da duoc gop ve Bang tin + ket ban + moi vao ke hoach.
/// Moi dia chi cu duoc chuyen huong; du lieu va ma van giu. Dat Features:LegacyCompanion=true de mo lai.
/// </summary>
public sealed class LegacyCompanionFilter(IConfiguration configuration):IActionFilter
{
 static readonly HashSet<string> Legacy=new(StringComparer.OrdinalIgnoreCase){"Companions","Together","CongBoNhuCau","GuiDeNghiKetNoi","KetThucThoaThuan","LapThoaThuan","PhanHoiDeNghi","SoanNhuCau","TimNhuCau","XacNhanThoaThuan","XemNhuCauChiaSe"};
 public const string Target="/bang-tin?tab=dong-hanh";
 public void OnActionExecuting(ActionExecutingContext context)
 {
  if(configuration.GetValue<bool>("Features:LegacyCompanion"))return;
  var name=context.RouteData.Values["controller"] as string;
  if(name==null||!Legacy.Contains(name))return;
  context.Result=new RedirectResult(context.HttpContext.User.IsInRole(TravelService.AdminRole)?"/Admin":Target);
 }
 public void OnActionExecuted(ActionExecutedContext context){}
}
