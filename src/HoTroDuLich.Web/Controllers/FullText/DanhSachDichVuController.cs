using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class DanhSachDichVuController(IQuanLyDichVuService component,TravelService read):FullTextController<IQuanLyDichVuService>(component,read) {
 [Authorize(Policy="LaNhaCungCap"),HttpGet("/nha-cung-cap/dich-vu")]
 public async Task<IActionResult> DanhSach([ValidateNever] DanhSachDichVuViewModel query) { var trang=query.Trang;ViewBag.Provider=await S.MyProvider();ViewBag.CanSubmit=await C.KiemTraDieuKienDangAsync();return V("Provider/Index",Page(await C.DanhSachAsync(),trang)); }
 [Authorize(Policy="LaNhaCungCap"),HttpGet("/nha-cung-cap/dich-vu/{maDichVu}")]
 public async Task<IActionResult> XemDichVu([ValidateNever] DanhSachDichVuViewModel query) { var maDichVu=query.MaDichVu;await C.XemDichVuAsync(maDichVu);ViewBag.CanSubmit=await C.KiemTraDieuKienDangAsync();ViewBag.Places=await S.VisiblePlaces();ViewBag.History=await C.LayLichSuAsync(maDichVu);return V("Provider/Edit",await C.MoBieuMauAsync(maDichVu,false)); }
}
