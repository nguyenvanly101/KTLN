using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HoTroDuLich.Web.Services;

/// <summary>
/// Dat dich vu gon: khong con trang "Thay doi / huy" (de nghi cho duyet) va "Ket qua phuc vu" (ket qua tung hang muc, phan anh).
/// Huy, sua va ghi nhan da phuc vu nam tren trang yeu cau. Dia chi cu chuyen ve trang yeu cau; du lieu va ma van giu.
/// Dat Features:LegacyBooking=true de mo lai (kiem thu cu).
/// </summary>
public sealed class LegacyBookingFilter(IConfiguration configuration):IActionFilter
{
 static readonly HashSet<string> Controllers=new(StringComparer.OrdinalIgnoreCase){"GuiDeNghiDieuChinh","XuLyDeNghiDieuChinh","GhiNhanKetQua","PhanAnhKetQua"};
 static readonly HashSet<string> BookingActions=new(StringComparer.OrdinalIgnoreCase){"Adjustments","Changes","Change","ChangeAction","Results","Outcome","Result","ResultAction"};
 public void OnActionExecuting(ActionExecutingContext context)
 {
  if(configuration.GetValue<bool>("Features:LegacyBooking"))return;
  var controller=context.RouteData.Values["controller"] as string;var action=context.RouteData.Values["action"] as string;
  var legacy=controller!=null&&(Controllers.Contains(controller)||(controller.Equals("Bookings",StringComparison.OrdinalIgnoreCase)&&action!=null&&BookingActions.Contains(action)));
  if(!legacy)return;
  var r=context.RouteData.Values;var q=context.HttpContext.Request.Query;
  var id=r["maYeuCau"]?.ToString()??q["bookingId"].FirstOrDefault()??(action is "Changes" or "Change" or "Outcome"?r["id"]?.ToString():null);
  if(id==null&&context.HttpContext.Request.HasFormContentType)id=context.HttpContext.Request.Form["bookingId"].FirstOrDefault();
  context.Result=new RedirectResult(long.TryParse(id,out var n)?"/Bookings/Detail/"+n:"/Bookings");
 }
 public void OnActionExecuted(ActionExecutedContext context){}
}
