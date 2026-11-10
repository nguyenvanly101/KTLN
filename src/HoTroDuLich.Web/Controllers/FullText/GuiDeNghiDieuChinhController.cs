using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class GuiDeNghiDieuChinhController(IDieuChinhDichVuService component,TravelService read):FullTextController<IDieuChinhDichVuService>(component,read) {
 [Authorize(Policy="LaKhachDuLich"),HttpGet("/yeu-cau/{maYeuCau}/dieu-chinh/mo-thay-doi")]
 public async Task<IActionResult> MoThayDoi([ValidateNever] GuiDeNghiDieuChinhViewModel query) { var maYeuCau=query.MaYeuCau;var y=await C.MoThayDoiAsync(maYeuCau);var pending=(await S.Changes(maYeuCau)).Any(x=>new[]{"ChoPhanHoi","DeXuatKhac"}.Contains(x.TrangThai.ToString()));if(pending||y.TrangThai.ToString()=="DeXuatKhac")return await ChangesPage(maYeuCau);if(y.TrangThai.ToString()=="ChoPhanHoi"){ViewBag.ServiceName=await S.BookingServiceName(maYeuCau);return V("Bookings/Edit",new BookingInput{Id=maYeuCau,ServiceId=y.MaDichVu,Version=TravelService.Token(y.RowVersion),Start=y.NgayBatDau,End=y.NgayKetThuc,People=y.SoNguoi,Quantity=y.SoLuong,ArrivalDeparture=y.ThoiGianDenDi,Selections=y.NoiDungLuaChon,Notes=y.GhiChu,Cost=y.ChiPhiDuKien});}return V("Bookings/Change",new ChangeInput{Id=maYeuCau,Version=TravelService.Token(y.RowVersion)}); }
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/yeu-cau/{maYeuCau}/dieu-chinh/gui-de-nghi")]
 public async Task<IActionResult> GuiDeNghi(long maYeuCau,GuiDeNghiDieuChinhViewModel i,BookingInput booking) { i.Id=maYeuCau;booking.Id=maYeuCau;if(i.Kind=="SuaYeuCau")await C.SuaYeuCauAsync(booking);else if(i.Kind=="RutYeuCau"){var y=await S.Booking(maYeuCau);await S.BookingAction(new(){Id=maYeuCau,Version=i.Version,BusinessVersion=y.PhienBan,Decision="Rut",Reason=i.Reason});}else await C.GuiDeNghiAsync(i);return Done("/yeu-cau/"+maYeuCau); }
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/yeu-cau/{maYeuCau}/dieu-chinh/phan-hoi-de-xuat")]
 public async Task<IActionResult> PhanHoiDeXuat(long maYeuCau,CommandInput i) { var d=await S.Find<DeNghiDieuChinhDichVu>(i.Id);S.Ensure(d.MaYeuCau==maYeuCau,"0801-01");await C.PhanHoiDeXuatAsync(i);return Done("/yeu-cau/"+maYeuCau); }
}
