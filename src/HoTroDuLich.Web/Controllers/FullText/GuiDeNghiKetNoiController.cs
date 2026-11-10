using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class GuiDeNghiKetNoiController(IKetNoiDongHanhService component,TravelService read):FullTextController<IKetNoiDongHanhService>(component,read) {
 [Authorize(Policy="LaKhachDuLich"),HttpGet("/dong-hanh/de-nghi/mo-de-nghi")]
 public async Task<IActionResult> MoDeNghi([ValidateNever] GuiDeNghiKetNoiViewModel query) { var maNhuCau=query.MaNhuCau;await C.MoDeNghiAsync(maNhuCau);var n=await S.Need(maNhuCau);ViewBag.Person=await S.SharedProfile(n.MaTaiKhoan);ViewBag.Names=await S.DisplayNames();return V("Companions/Detail",n); }
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/dong-hanh/de-nghi/gui-de-nghi")]
 public async Task<IActionResult> GuiDeNghi(GuiDeNghiKetNoiViewModel i) { await C.GuiDeNghiAsync(i);return Done("/dong-hanh/de-nghi","Đã gửi đề nghị kết nối, chờ người nhận phản hồi."); }
}
