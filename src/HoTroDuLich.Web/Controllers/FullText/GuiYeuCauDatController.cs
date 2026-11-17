using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class GuiYeuCauDatController(IDatDichVuService component,TravelService read,TripWorkspaceService trips,ChatService chat):FullTextController<IDatDichVuService>(component,read) {
 [Authorize(Policy="LaKhachDuLich"),HttpGet("/yeu-cau/mo-dat-dich-vu")]
 public async Task<IActionResult> MoDatDichVu([ValidateNever] GuiYeuCauDatViewModel query) { var input=await trips.BookingForm(query.MaDichVu,query.PlanId);ViewBag.Service=await C.MoDatDichVuAsync(query.MaDichVu);return V("Bookings/Create",input); }
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/yeu-cau/kiem-tra-truoc-gui")]
 public async Task<IActionResult> KiemTraTruocGui(GuiYeuCauDatViewModel i) { ViewBag.Warnings=await C.KiemTraTruocGuiAsync(i);ViewBag.ServiceName=(await S.PublicService(i.ServiceId)).TenDichVu;return V("Bookings/Preview",i); }
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/yeu-cau/gui-yeu-cau")]
 public async Task<IActionResult> GuiYeuCau(GuiYeuCauDatViewModel i) { var id=await C.GuiYeuCauAsync(i);try{await chat.AttachBooking(id);}catch(BusinessException){/* yeu cau da gui; chi khong gan duoc vao tro chuyen */}return Done("/yeu-cau/"+id,"Đã gửi yêu cầu, chờ nhà cung cấp phản hồi."); }
}
