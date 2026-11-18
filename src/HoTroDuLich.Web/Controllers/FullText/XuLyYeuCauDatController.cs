using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class XuLyYeuCauDatController(IDatDichVuService component,TravelService read,BranchService branches):FullTextController<IDatDichVuService>(component,read) {
 [Authorize(Policy="LaNhaCungCap"),HttpGet("/nha-cung-cap/yeu-cau")]
 public async Task<IActionResult> DanhSachNCC([ValidateNever] XuLyYeuCauDatViewModel query) { await Task.CompletedTask;return Redirect("/Bookings"+Request.QueryString); }
 [Authorize(Policy="LaNhaCungCap"),HttpGet("/nha-cung-cap/yeu-cau/{maYeuCau}")]
 public async Task<IActionResult> XemYeuCau([ValidateNever] XuLyYeuCauDatViewModel query) { var maYeuCau=query.MaYeuCau;if(await branches.Focus(await branches.BranchOfRequest(maYeuCau)) is {} moved)TempData["Success"]="Đã chuyển sang cơ sở "+moved.Name+" để xử lý yêu cầu này.";ViewBag.ServiceName=await C.LayTenDichVuCuaYeuCauAsync(maYeuCau);return V("Bookings/Detail",await C.XemYeuCauAsync(maYeuCau)); }
 [Authorize(Policy="LaNhaCungCap"),HttpPost("/yeu-cau/chap-nhan")]
 public async Task<IActionResult> ChapNhan(XuLyYeuCauDatViewModel i) { if(i.Decision=="ChapNhan")i.Decision="DaXacNhan";await C.ChapNhanAsync(i);return Done("/nha-cung-cap/yeu-cau/"+i.Id); }
}
