using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class KetThucThoaThuanController(IThoaThuanDongHanhService component,TravelService read):FullTextController<IThoaThuanDongHanhService>(component,read) {
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/dong-hanh/thoa-thuan/rut")]
 public async Task<IActionResult> Rut(KetThucThoaThuanViewModel i) { await C.RutAsync(i);return Done("/dong-hanh/thoa-thuan/xem-de-nghi/"+i.Id); }
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/dong-hanh/thoa-thuan/de-nghi-ngung")]
 public async Task<IActionResult> DeNghiNgung(KetThucThoaThuanViewModel i) { await C.DeNghiNgungAsync(i);return Done("/dong-hanh/thoa-thuan/xem-de-nghi/"+(await S.Agreements(i.ConnectionId)).First().MaThoaThuan); }
}
