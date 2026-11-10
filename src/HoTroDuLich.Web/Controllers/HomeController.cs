using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;

namespace HoTroDuLich.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return LocalRedirect(SucAccess.Home(User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? ""));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet("/chinh-sach-bao-mat")]
    public IActionResult PrivacyPolicy()=>View("Privacy");

    [HttpGet("/dieu-khoan-su-dung")]
    public IActionResult Terms()=>View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [Route("/loi")]
    [IgnoreAntiforgeryToken]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
