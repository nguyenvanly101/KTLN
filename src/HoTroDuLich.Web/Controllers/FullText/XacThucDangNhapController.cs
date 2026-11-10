using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class XacThucDangNhapController(IDangNhapService component,TravelService read):FullTextController<IDangNhapService>(component,read) {
 [AllowAnonymous,HttpGet("/tai-khoan/dang-nhap/mo-dang-nhap")]
 public async Task<IActionResult> MoDangNhap([ValidateNever] XacThucDangNhapViewModel query) { var returnUrl=query.ReturnUrl;ViewBag.Demo=HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue<bool>("Demo:Enabled");return V("Account/Login",await C.MoDangNhapAsync(returnUrl)); }
 [AllowAnonymous,HttpPost("/tai-khoan/dang-nhap/dang-nhap"),EnableRateLimiting("auth")]
 public async Task<IActionResult> DangNhap(XacThucDangNhapViewModel i) { var account=await C.DangNhapAsync(i);await SessionAuthentication.SignIn(HttpContext,account);return LocalRedirect(account.PhaiDoiMatKhau?"/tai-khoan/doi-mat-khau":Url.IsLocalUrl(i.ReturnUrl)?i.ReturnUrl!:SucAccess.Home(account.VaiTro.ToString())); }
}
