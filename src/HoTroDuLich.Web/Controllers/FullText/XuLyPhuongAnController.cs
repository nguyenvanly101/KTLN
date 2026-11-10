using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class XuLyPhuongAnController(IXuLyTinhHuongService component,TravelService read):FullTextController<IXuLyTinhHuongService>(component,read) {
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/ke-hoach/{maKeHoach}/tinh-huong/ghi-phuong-an")]
 public async Task<IActionResult> GhiPhuongAn(long maKeHoach,XuLyPhuongAnViewModel i) { await C.LayTinhHuongAsync(maKeHoach,i.Id);await C.GhiPhuongAnAsync(i);return Done("/ke-hoach/"+maKeHoach+"/tinh-huong/"+i.Id); }
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/ke-hoach/{maKeHoach}/tinh-huong/ghi-ket-qua")]
 public async Task<IActionResult> GhiKetQua(long maKeHoach,XuLyPhuongAnViewModel i) { await C.LayTinhHuongAsync(maKeHoach,i.Id);await C.GhiKetQuaAsync(i);return Done("/ke-hoach/"+maKeHoach+"/tinh-huong/"+i.Id); }
}
