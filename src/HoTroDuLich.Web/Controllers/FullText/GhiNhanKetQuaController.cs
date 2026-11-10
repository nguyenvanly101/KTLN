using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class GhiNhanKetQuaController(IKetQuaDichVuService component,TravelService read):FullTextController<IKetQuaDichVuService>(component,read) {
 [Authorize(Policy="LaNhaCungCap"),HttpGet("/yeu-cau/{maYeuCau}/ket-qua/mo-ghi-nhan")]
 public async Task<IActionResult> MoGhiNhan([ValidateNever] GhiNhanKetQuaViewModel query) { var maYeuCau=query.MaYeuCau;await C.MoGhiNhanAsync(maYeuCau);return await OutcomePage(maYeuCau); }
 [Authorize(Policy="LaNhaCungCap"),HttpPost("/yeu-cau/{maYeuCau}/ket-qua/luu-ket-qua")]
 public async Task<IActionResult> LuuKetQua(long maYeuCau,GhiNhanKetQuaViewModel i) { i.Id=maYeuCau;await C.LuuKetQuaAsync(i);return Done("/nha-cung-cap/yeu-cau/"+maYeuCau); }
}
