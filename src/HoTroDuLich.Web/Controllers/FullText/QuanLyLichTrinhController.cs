using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class QuanLyLichTrinhController(ILapKeHoachService component,TravelService read):FullTextController<ILapKeHoachService>(component,read) {
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/ke-hoach/them-hoat-dong")]
 public async Task<IActionResult> ThemHoatDong(QuanLyLichTrinhViewModel i) { await C.ThemHoatDongAsync(i);return Done("/ke-hoach/"+i.PlanId); }
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/ke-hoach/sua-hoat-dong")]
 public async Task<IActionResult> SuaHoatDong(QuanLyLichTrinhViewModel i) { await C.SuaHoatDongAsync(i);return Done("/ke-hoach/"+i.PlanId); }
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/ke-hoach/xoa-hoat-dong")]
 public async Task<IActionResult> XoaHoatDong(CommandInput i) { var activity=await S.Find<HoatDongLichTrinh>(i.Id);await C.XoaHoatDongAsync(i);return Done("/ke-hoach/"+activity.MaKeHoach); }
}
