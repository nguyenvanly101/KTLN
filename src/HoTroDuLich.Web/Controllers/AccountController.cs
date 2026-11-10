using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace HoTroDuLich.Web.Controllers;
public class AccountController(TravelService s, IConfiguration config) : AppController(s)
{
    [SucAuthorize(SucAccess.Login), HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        ViewBag.Demo = config.GetValue<bool>("Demo:Enabled");
        return View(new LoginInput { ReturnUrl = returnUrl });
    }

    [SucAuthorize(SucAccess.Login), HttpPost, EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginInput i)
    {
        var a = await S.Authenticate(i);
        await SessionAuthentication.SignIn(HttpContext, a);
        return LocalRedirect(a.PhaiDoiMatKhau ? "/tai-khoan/doi-mat-khau" : Url.IsLocalUrl(i.ReturnUrl) ? i.ReturnUrl! : SucAccess.Home((a.VaiTro).ToString()));
    }

    [SucAuthorize(SucAccess.Registration), HttpGet]
    public IActionResult Register(string? accountType) => View(new RegisterInput { Role = accountType == "provider" ? TravelService.ProviderRole : TravelService.Traveler });
    [SucAuthorize(SucAccess.Registration), HttpPost, EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterInput i)
    {
        await S.Register(i);
        return Done("/Account/Login", "Đăng ký thành công. Bạn có thể đăng nhập; nhà cung cấp cần chờ xác minh để gửi dịch vụ.");
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> Logout()
    {
        await S.LogLogout();
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index", "Explore");
    }

    [SucAuthorize(SucAccess.Profile), HttpGet]
    public async Task<IActionResult> Profile()
    {
        ViewBag.Account = await S.Find<HoTroDuLich.Web.Domain.TaiKhoan>(S.UserId);
        if (S.Role == TravelService.ProviderRole)
            ViewBag.Provider = await S.MyProvider();
        return View(await S.ProfileForm());
    }

    [SucAuthorize(SucAccess.Profile), HttpPost]
    public async Task<IActionResult> Profile(ProfileInput i)
    {
        await S.SaveProfile(i);
        return Done("/Account/Profile");
    }

    [Authorize, HttpGet("/tai-khoan/doi-mat-khau"), HttpGet("/Account/Password")]
    public IActionResult Password() => View(new PasswordChangeInput());
    [Authorize, HttpPost("/Account/Password")]
    public async Task<IActionResult> Password(PasswordChangeInput i)
    {
        var a = await S.ChangePassword(i);
        await SessionAuthentication.SignIn(HttpContext, a);
        return Done(SucAccess.Home(S.Role), "Đã đổi mật khẩu và kết thúc các phiên đăng nhập cũ.");
    }

    [SucAuthorize(SucAccess.Deactivation), HttpGet]
    public IActionResult Deactivate() => View(new DeactivationInput());
    [SucAuthorize(SucAccess.Deactivation), HttpPost]
    public async Task<IActionResult> Deactivate(DeactivationInput i) => View("DeactivateReview", await S.Reauthenticate(i.Password));
    [SucAuthorize(SucAccess.Deactivation), HttpPost]
    public async Task<IActionResult> ConfirmDeactivate(string confirmationToken)
    {
        await S.Deactivate(confirmationToken);
        await HttpContext.SignOutAsync();
        return Done("/Account/Login", "Đã ngừng sử dụng tài khoản và kết thúc phiên.");
    }

    public IActionResult Denied()
    {
        Response.StatusCode = 403;
        ViewBag.Message = "Bạn không có quyền truy cập chức năng này.";
        return View("Failure");
    }

    [Authorize]
    public async Task<IActionResult> Notifications() => View(await S.Notifications());
    [Authorize, HttpPost]
    public async Task<IActionResult> Read(long id)
    {
        var target = await S.MarkRead(id);
        return LocalRedirect(Url.IsLocalUrl(target) ? target! : "/thong-bao");
    }

    [Authorize]
    public async Task<IActionResult> Download(string kind, long id)
    {
        var f = await S.AuthorizedFile(kind, id);
        return PhysicalFile(f.Path, f.ContentType, f.Name);
    }
}
