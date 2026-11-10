using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class TheoDoiTinhHuongController(IXuLyTinhHuongService component,TravelService read):FullTextController<IXuLyTinhHuongService>(component,read) {
 [Authorize(Policy="LaKhachDuLich"),HttpGet("/ke-hoach/{maKeHoach}/tinh-huong")]
 public async Task<IActionResult> DanhSach([ValidateNever] TheoDoiTinhHuongViewModel query) { var maKeHoach=query.MaKeHoach;var trang=query.Trang;ViewBag.Plan=await S.OwnPlan(maKeHoach);ViewBag.Plans=await S.Plans();return V("Support/Index",Page(await C.DanhSachAsync(maKeHoach),trang)); }
 [Authorize(Policy="LaKhachDuLich"),HttpGet("/ke-hoach/{maKeHoach}/tinh-huong/{maTinhHuong}")]
 public async Task<IActionResult> XemTinhHuong([ValidateNever] TheoDoiTinhHuongViewModel query) { var maKeHoach=query.MaKeHoach;var maTinhHuong=query.MaTinhHuong;var t=await C.LayTinhHuongAsync(maKeHoach,maTinhHuong);ViewBag.Options=await C.XemTinhHuongAsync(maTinhHuong);return V("Support/Detail",t); }
}
