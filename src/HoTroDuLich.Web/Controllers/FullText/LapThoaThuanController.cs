using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class LapThoaThuanController(IThoaThuanDongHanhService component,TravelService read):FullTextController<IThoaThuanDongHanhService>(component,read) {
 [Authorize(Policy="LaKhachDuLich"),HttpGet("/dong-hanh/thoa-thuan/mo-tao-thoa-thuan")]
 public async Task<IActionResult> MoTaoThoaThuan([ValidateNever] LapThoaThuanViewModel query) { var maKetNoi=query.MaKetNoi;return V("Companions/Propose",await C.MoTaoThoaThuanAsync(maKetNoi)); }
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/dong-hanh/thoa-thuan/gui-de-nghi-thoa-thuan")]
 public async Task<IActionResult> GuiDeNghiThoaThuan(LapThoaThuanViewModel i) { i.Kind="NoiDung";await C.GuiDeNghiThoaThuanAsync(i);return Done("/dong-hanh/thoa-thuan/xem-de-nghi/"+(await S.Agreements(i.ConnectionId)).First().MaThoaThuan); }
}
