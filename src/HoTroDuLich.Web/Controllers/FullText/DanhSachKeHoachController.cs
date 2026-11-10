using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class DanhSachKeHoachController(ILapKeHoachService component,TravelService read,TripWorkspaceService trips):FullTextController<ILapKeHoachService>(component,read) {
 [Authorize(Policy="LaKhachDuLich"),HttpGet("/ke-hoach")]
 public async Task<IActionResult> DanhSach([ValidateNever] DanhSachKeHoachViewModel query) { var trang=query.Trang;return V("Plans/Index",Page(await C.DanhSachAsync(),trang)); }
 [Authorize(Policy="LaKhachDuLich"),HttpGet("/ke-hoach/{maKeHoach}")]
 public async Task<IActionResult> XemKeHoach([ValidateNever] DanhSachKeHoachViewModel query) { return V("Plans/Detail",await trips.Open(query.MaKeHoach)); }
}
