using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class QuanLyPhienController(IDangNhapService component,TravelService read):FullTextController<IDangNhapService>(component,read) {
 [Authorize(Policy="DaXacThuc"),HttpPost("/tai-khoan/dang-xuat")]
 public async Task<IActionResult> DangXuat() { await C.DangXuatAsync();await HttpContext.SignOutAsync();return LocalRedirect("/thong-tin/mo-tim-kiem"); }
 [Authorize(Policy="DaXacThuc"),HttpPost("/tai-khoan/doi-mat-khau")]
 public async Task<IActionResult> DoiMatKhau(QuanLyPhienViewModel i) { var account=await C.DoiMatKhauAsync(i);await SessionAuthentication.SignIn(HttpContext,account);return Done(SucAccess.Home(S.Role),"Đã đổi mật khẩu và kết thúc các phiên đăng nhập cũ."); }
}
