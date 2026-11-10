using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class SoanNhuCauController(ICongBoNhuCauService component,TravelService read):FullTextController<ICongBoNhuCauService>(component,read) {
 [Authorize(Policy="LaKhachDuLich"),HttpGet("/dong-hanh/nhu-cau/mo-bieu-mau")]
 public async Task<IActionResult> MoBieuMau([ValidateNever] SoanNhuCauViewModel query) { return V("Companions/Edit",await C.MoBieuMauAsync()); }
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/dong-hanh/nhu-cau/xem-truoc")]
 public async Task<IActionResult> XemTruoc(SoanNhuCauViewModel i) { await C.XemTruocAsync(i);return V("Companions/Preview",i); }
}
