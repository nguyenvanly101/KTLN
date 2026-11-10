using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class XuLyDeNghiDieuChinhController(IDieuChinhDichVuService component,TravelService read):FullTextController<IDieuChinhDichVuService>(component,read) {
 [Authorize(Policy="LaNhaCungCap"),HttpGet("/yeu-cau/{maYeuCau}/dieu-chinh/xem-de-nghi/{maDeNghi}")]
 public async Task<IActionResult> XemDeNghi([ValidateNever] XuLyDeNghiDieuChinhViewModel query) { var maYeuCau=query.MaYeuCau;var maDeNghi=query.MaDeNghi;await C.XemDeNghiAsync(maYeuCau,maDeNghi);return await ChangesPage(maYeuCau); }
 [Authorize(Policy="LaNhaCungCap"),HttpPost("/yeu-cau/{maYeuCau}/dieu-chinh/phan-hoi")]
 public async Task<IActionResult> PhanHoi(long maYeuCau,XuLyDeNghiDieuChinhViewModel i) { await C.XemDeNghiAsync(maYeuCau,i.Id);await C.PhanHoiAsync(i);return Done("/nha-cung-cap/yeu-cau/"+maYeuCau); }
}
