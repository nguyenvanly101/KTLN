using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class KiemTraDieuKienNgungController(INgungTaiKhoanService component,TravelService read):FullTextController<INgungTaiKhoanService>(component,read) {
 [Authorize(Policy="LaKhachHoacNCC"),HttpGet("/tai-khoan/ngung-su-dung/mo-ngung")]
 public async Task<IActionResult> MoNgung([ValidateNever] KiemTraDieuKienNgungViewModel query) { return V("Account/Deactivate",await C.MoNgungAsync()); }
 [Authorize(Policy="LaKhachHoacNCC"),HttpPost("/tai-khoan/ngung-su-dung/xac-thuc-lai")]
 public async Task<IActionResult> XacThucLai(KiemTraDieuKienNgungViewModel i) { return V("Account/DeactivateReview",await C.XacThucLaiAsync(i.Password)); }
}
