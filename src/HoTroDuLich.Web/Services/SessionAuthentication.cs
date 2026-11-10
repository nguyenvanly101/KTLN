using System.Security.Claims;
using HoTroDuLich.Web.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
namespace HoTroDuLich.Web.Services;
public static class SessionAuthentication
{
 public static Task SignIn(HttpContext context,TaiKhoan a){var claims=new[]{new Claim(ClaimTypes.NameIdentifier,a.MaTaiKhoan.ToString()),new Claim(ClaimTypes.Name,a.Email),new Claim(ClaimTypes.Role,a.VaiTro.ToString()),new Claim("stamp",a.DauPhien.ToString()),new Claim("issued",DateTime.UtcNow.Ticks.ToString())};return context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)));}
}
