using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HoTroDuLich.Web.Controllers;
[Authorize(Roles=TravelService.Traveler+","+TravelService.ProviderRole)]
[SucAuthorize(SucAccess.Booking)]
public class BookingsController(TravelService s,TripWorkspaceService trips):AppController(s){
 public async Task<IActionResult> Index(){ViewBag.ServiceNames=await S.BookingServiceNames();return View(await S.Bookings());}
 public async Task<IActionResult> Detail(long id){ViewBag.ServiceName=await S.BookingServiceName(id);return View(await S.Booking(id));}
 [SucAuthorize(SucAccess.BookingChange)]
 public async Task<IActionResult> Adjustments(){ViewBag.ServiceNames=await S.BookingServiceNames();return View(await S.AdjustmentBookings());}
 [SucAuthorize(SucAccess.BookingChange)]
 public async Task<IActionResult> Changes(long id){ViewBag.ServiceName=await S.BookingServiceName(id);ViewBag.Changes=await S.Changes(id);return View(await S.Booking(id));}
 [SucAuthorize(SucAccess.ServiceResult)]
 public async Task<IActionResult> Results(){ViewBag.ServiceNames=await S.BookingServiceNames();return View(await S.ResultBookings());}
 [SucAuthorize(SucAccess.ServiceResult)]
 public async Task<IActionResult> Outcome(long id){ViewBag.ServiceName=await S.BookingServiceName(id);ViewBag.Results=await S.Results(id);ViewBag.Items=await S.BookingItems(id);return View(await S.Booking(id));}
 [Authorize(Roles=TravelService.Traveler),HttpGet] public async Task<IActionResult> Create(long id,long? planId=null,long? activityId=null,long? branchId=null){var input=await trips.BookingForm(id,planId,activityId);input.BranchId=branchId;ViewBag.Service=await S.BookableService(id);return View(input);}
 [Authorize(Roles=TravelService.Traveler),HttpPost] public async Task<IActionResult> Create(BookingInput i){ViewBag.Warnings=await S.CheckBooking(i);ViewBag.ServiceName=(await S.PublicService(i.ServiceId)).TenDichVu;return View("Preview",i);}
 [Authorize(Roles=TravelService.Traveler),HttpPost] public async Task<IActionResult> Submit(BookingInput i)=>Done("/Bookings/Detail/"+await S.Book(i),"Đã gửi yêu cầu, chờ nhà cung cấp phản hồi.");
 [Authorize(Roles=TravelService.Traveler),HttpPost] public async Task<IActionResult> Revise(BookingInput i){ViewBag.Service=await S.BookableService(i.ServiceId);return View("Create",i);}
 [Authorize(Roles=TravelService.Traveler),HttpPost] public async Task<IActionResult> Trip(long id,string version,long? planId){await trips.AssignBooking(id,version,planId);return Done("/Bookings/Detail/"+id,"Đã cập nhật chuyến đi cho yêu cầu.");}
 [HttpPost] public async Task<IActionResult> Respond(CommandInput i){await S.BookingAction(i);return Done("/Bookings/Detail/"+i.Id);}
 [SucAuthorize(SucAccess.BookingChange)]
 [Authorize(Roles=TravelService.Traveler),HttpGet] public async Task<IActionResult> Edit(long id){var y=await S.Booking(id);S.Require(y.MaTaiKhoan==S.UserId,"Không có quyền.",403);ViewBag.ServiceName=await S.BookingServiceName(id);return View(new BookingInput{Id=id,ServiceId=y.MaDichVu,Version=TravelService.Token(y.RowVersion),Start=y.NgayBatDau,End=y.NgayKetThuc,People=y.SoNguoi,Quantity=y.SoLuong,Notes=y.GhiChu});}
 [SucAuthorize(SucAccess.BookingChange)]
 [Authorize(Roles=TravelService.Traveler),HttpPost] public async Task<IActionResult> Edit(BookingInput i){await S.EditPendingBooking(i);return Done("/Bookings/Detail/"+i.Id);}
 [SucAuthorize(SucAccess.BookingChange)]
 [Authorize(Roles=TravelService.Traveler),HttpGet] public async Task<IActionResult> Change(long id){var y=await S.Booking(id);S.Require(y.MaTaiKhoan==S.UserId,"Không có quyền.",403);return View(new ChangeInput{Id=id,Version=TravelService.Token(y.RowVersion),Start=y.NgayBatDau,End=y.NgayKetThuc,People=y.SoNguoi,Quantity=y.SoLuong});}
 [SucAuthorize(SucAccess.BookingChange)]
 [Authorize(Roles=TravelService.Traveler),HttpPost] public async Task<IActionResult> Change(ChangeInput i){await S.ChangeBooking(i);return Done("/Bookings/Changes/"+i.Id,"Đề nghị đã gửi. Điều kiện hiện tại giữ hiệu lực cho đến khi hai bên chấp nhận.");}
 [SucAuthorize(SucAccess.BookingChange)]
 [HttpPost] public async Task<IActionResult> ChangeAction(CommandInput i,long bookingId){await S.ChangeAction(i);return Done("/Bookings/Changes/"+bookingId);}
 [SucAuthorize(SucAccess.ServiceResult)]
 [Authorize(Roles=TravelService.ProviderRole),HttpPost] public async Task<IActionResult> Result(CommandInput i){await S.RecordResult(i);return Done("/Bookings/Outcome/"+i.Id);}
 [SucAuthorize(SucAccess.ServiceResult)]
 [HttpPost] public async Task<IActionResult> ResultAction(CommandInput i,long bookingId){await S.ResultAction(i);return Done("/Bookings/Outcome/"+bookingId);}
}
