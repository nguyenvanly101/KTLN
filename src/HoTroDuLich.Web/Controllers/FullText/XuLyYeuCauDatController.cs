using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class XuLyYeuCauDatController(IDatDichVuService component,TravelService read):FullTextController<IDatDichVuService>(component,read) {
 [Authorize(Policy="LaNhaCungCap"),HttpGet("/nha-cung-cap/yeu-cau")]
 public async Task<IActionResult> DanhSachNCC([ValidateNever] XuLyYeuCauDatViewModel query) { var trangThai=query.TrangThai;var trang=query.Trang;ViewBag.ServiceNames=await C.LayTenDichVuAsync();return V("Bookings/Index",Page(FilterBookings(await C.DanhSachNCCAsync(),trangThai),trang)); }
 [Authorize(Policy="LaNhaCungCap"),HttpGet("/nha-cung-cap/yeu-cau/{maYeuCau}")]
 public async Task<IActionResult> XemYeuCau([ValidateNever] XuLyYeuCauDatViewModel query) { var maYeuCau=query.MaYeuCau;ViewBag.ServiceName=await C.LayTenDichVuCuaYeuCauAsync(maYeuCau);return V("Bookings/Detail",await C.XemYeuCauAsync(maYeuCau)); }
 [Authorize(Policy="LaNhaCungCap"),HttpPost("/yeu-cau/chap-nhan")]
 public async Task<IActionResult> ChapNhan(XuLyYeuCauDatViewModel i) { if(i.Decision=="ChapNhan")i.Decision="DaXacNhan";await C.ChapNhanAsync(i);return Done("/nha-cung-cap/yeu-cau/"+i.Id); }
}
