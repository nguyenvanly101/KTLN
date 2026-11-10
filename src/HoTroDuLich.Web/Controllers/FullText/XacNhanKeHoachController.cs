using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class XacNhanKeHoachController(ILapKeHoachService component,TravelService read):FullTextController<ILapKeHoachService>(component,read) {
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/ke-hoach/chap-nhan")]
 public async Task<IActionResult> ChapNhan(XacNhanKeHoachViewModel i) { i.Decision="DaChapNhan";await C.ChapNhanAsync(i);return Done("/ke-hoach/"+i.Id); }
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/ke-hoach/luu-nhap")]
 public async Task<IActionResult> LuuNhap(XacNhanKeHoachViewModel i) { i.Decision="Nhap";await C.LuuNhapAsync(i);return Done("/ke-hoach/"+i.Id); }
}
