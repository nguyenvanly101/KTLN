using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class DangKyTaiKhoanController(IDangKyService component,TravelService read):FullTextController<IDangKyService>(component,read) {
 [AllowAnonymous,HttpGet("/tai-khoan/dang-ky/mo-dang-ky")]
 public async Task<IActionResult> MoDangKy([ValidateNever] DangKyTaiKhoanViewModel query) { return V("Account/Register",await C.MoDangKyAsync()); }
 [AllowAnonymous,HttpPost("/tai-khoan/dang-ky/chon-vai-tro"),EnableRateLimiting("auth")]
 public async Task<IActionResult> ChonVaiTro(string role) { return V("Account/Register",await C.ChonVaiTroAsync(role)); }
 [AllowAnonymous,HttpPost("/tai-khoan/dang-ky/dang-ky"),EnableRateLimiting("auth")]
 public async Task<IActionResult> DangKy(DangKyTaiKhoanViewModel i) { await C.DangKyAsync(i);return Done("/tai-khoan/dang-nhap/mo-dang-nhap","Đăng ký thành công. Nhà cung cấp cần chờ xác minh trước khi đăng dịch vụ."); }
}
