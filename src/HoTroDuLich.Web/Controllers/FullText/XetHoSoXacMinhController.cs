using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class XetHoSoXacMinhController(IXacMinhNhaCungCapService component,TravelService read):FullTextController<IXacMinhNhaCungCapService>(component,read) {
 [Authorize(Policy="LaQuanTriVien"),HttpGet("/quan-tri/xac-minh/mo-danh-sach")]
 public async Task<IActionResult> MoDanhSach([ValidateNever] XetHoSoXacMinhViewModel query) { var tab=query.Tab;var trang=query.Trang;S.Require(tab is "cho" or "bo-sung","Bộ lọc không hợp lệ.",400);return V("Admin/Verifications",await C.MoDanhSachAsync(null,tab=="cho"?"ChoXacMinh":"CanBoSung",trang)); }
 [Authorize(Policy="LaQuanTriVien"),HttpGet("/quan-tri/xac-minh/xem-ho-so/{maHoSo}")]
 public async Task<IActionResult> XemHoSo([ValidateNever] XetHoSoXacMinhViewModel query) { var maHoSo=query.MaHoSo;return V("Admin/Verification",await C.XemHoSoAsync(maHoSo)); }
 [Authorize(Policy="LaQuanTriVien"),HttpPost("/quan-tri/xac-minh/xac-minh")]
 public async Task<IActionResult> XacMinh(XetHoSoXacMinhViewModel i) { await C.XacMinhAsync(i);return Done("/quan-tri/xac-minh/mo-danh-sach"); }
}
