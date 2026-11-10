using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class DanhSachChoDuyetController(IXetDuyetDichVuService component,TravelService read):FullTextController<IXetDuyetDichVuService>(component,read) {
 [Authorize(Policy="LaQuanTriVien"),HttpGet("/quan-tri/xet-duyet-dich-vu/mo-danh-sach-cho-duyet")]
 public async Task<IActionResult> MoDanhSachChoDuyet([ValidateNever] DanhSachChoDuyetViewModel query) { var q=query.Q;var status=query.Status;var trang=query.Trang;return V("Admin/ServiceReviews",await C.MoDanhSachChoDuyetAsync(q,status,trang)); }
 [Authorize(Policy="LaQuanTriVien"),HttpGet("/quan-tri/xet-duyet-dich-vu/xem-noi-dung/{maBanCapNhat}")]
 public async Task<IActionResult> XemNoiDung([ValidateNever] DanhSachChoDuyetViewModel query) { var maBanCapNhat=query.MaBanCapNhat;return V("Admin/Review",await C.XemNoiDungAsync(maBanCapNhat)); }
}
