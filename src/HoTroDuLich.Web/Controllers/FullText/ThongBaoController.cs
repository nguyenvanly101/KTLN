using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class ThongBaoController(IThongBaoService component,TravelService read):FullTextController<IThongBaoService>(component,read) {
 [Authorize(Policy="DaXacThuc"),HttpGet("/thong-bao")]
 public async Task<IActionResult> DanhSachThongBao([ValidateNever] ThongBaoViewModel query) { var trang=query.Trang;return V("Account/Notifications",Page(await C.DanhSachThongBaoAsync(),trang)); }
 [Authorize(Policy="DaXacThuc"),HttpPost("/thong-bao/{maThongBao}/mo")]
 public async Task<IActionResult> MoThongBao(long maThongBao) { var target=await C.MoThongBaoAsync(maThongBao);return LocalRedirect(Url.IsLocalUrl(target)?target!:"/thong-bao"); }
}
