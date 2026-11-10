using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class CapNhatHoSoController(IHoSoNguoiDungService component,TravelService read):FullTextController<IHoSoNguoiDungService>(component,read) {
 [Authorize(Policy="DaXacThuc"),HttpGet("/ho-so/xem-ho-so")]
 public async Task<IActionResult> XemHoSo([ValidateNever] CapNhatHoSoViewModel query) { await C.XemHoSoAsync();ViewBag.Account=await S.Find<TaiKhoan>(S.UserId);if(S.Role==TravelService.ProviderRole)ViewBag.Provider=await S.MyProvider();return V("Account/Profile",await C.MoBieuMauAsync()); }
 [Authorize(Policy="DaXacThuc"),HttpPost("/ho-so/luu-thay-doi")]
 public async Task<IActionResult> LuuThayDoi(CapNhatHoSoViewModel i) { await C.LuuThayDoiAsync(i);return Done("/ho-so/xem-ho-so"); }
}
