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
    [Route("/loi/{code:int?}")]
    [IgnoreAntiforgeryToken]
    public IActionResult Error(int? code)
    {
        var status = code ?? (HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>() != null ? 500 : Response.StatusCode);
        if (status < 400) status = 500;
        Response.StatusCode = status;
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, Status = status });
    }
}
