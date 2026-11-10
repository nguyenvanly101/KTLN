using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class GhiNhanTinhHuongController(IXuLyTinhHuongService component,TravelService read):FullTextController<IXuLyTinhHuongService>(component,read) {
 [Authorize(Policy="LaKhachDuLich"),HttpGet("/ke-hoach/{maKeHoach}/tinh-huong/mo-ghi-nhan")]
 public async Task<IActionResult> MoGhiNhan([ValidateNever] GhiNhanTinhHuongViewModel query) { var maKeHoach=query.MaKeHoach;ViewBag.Plan=await S.OwnPlan(maKeHoach);ViewBag.Activities=await S.Activities(maKeHoach);return V("Support/Create",await C.MoGhiNhanAsync(maKeHoach)); }
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/ke-hoach/{maKeHoach}/tinh-huong/ghi-nhan-tinh-huong")]
 public async Task<IActionResult> GhiNhanTinhHuong(long maKeHoach,GhiNhanTinhHuongViewModel i) { i.PlanId=maKeHoach;await C.GhiNhanTinhHuongAsync(i);return Done("/ke-hoach/"+maKeHoach+"/tinh-huong"); }
}
