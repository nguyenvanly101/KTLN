using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class ThucHienNgungController(INgungTaiKhoanService component,TravelService read):FullTextController<INgungTaiKhoanService>(component,read) {
 [Authorize(Policy="LaKhachHoacNCC"),HttpPost("/tai-khoan/ngung-su-dung/ngung-tai-khoan")]
 public async Task<IActionResult> NgungTaiKhoan(string confirmationToken) { await C.NgungTaiKhoanAsync(confirmationToken);await HttpContext.SignOutAsync();return Done("/tai-khoan/dang-nhap/mo-dang-nhap","Tài khoản đã ngừng sử dụng."); }
}
