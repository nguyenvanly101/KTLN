using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;

/// <summary>API doc cong khai cho cay dia gioi hanh chinh (chon tinh -> huyen -> xa).</summary>
[AllowAnonymous, Route("api/dia-gioi"), ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
[EnableRateLimiting("discovery")]
public sealed class DiaGioiApiController(DiaGioiService geo) : Controller
{
    [HttpGet("tinh")]
    public async Task<IActionResult> Provinces() => Json(new { regime = geo.Regime, items = await geo.Provinces() });

    [HttpGet("{id:long}/con")]
    public async Task<IActionResult> Children(long id) => Json(new { items = await geo.Children(id) });

    [HttpGet("{id:long}/duong-dan")]
    public async Task<IActionResult> Path(long id)
    {
        var path = await geo.Path(id);
        if (path.Count == 0) return NotFound(new { message = "Không tìm thấy đơn vị hành chính." });
        return Json(new { items = path.Select(x => new { id = x.Id, name = x.Name, fullName = x.FullName, kind = x.Kind, level = x.Level }), text = string.Join(", ", path.AsEnumerable().Reverse().Select(x => x.FullName)) });
    }

    [HttpGet("tra-cuu")]
    public async Task<IActionResult> Search(string? q) => Json(new { items = await geo.Search(q) });
}
