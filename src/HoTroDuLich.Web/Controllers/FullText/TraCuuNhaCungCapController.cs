using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class TraCuuNhaCungCapController(IQuyenHoatDongService component,TravelService read):FullTextController<IQuyenHoatDongService>(component,read) {
 [Authorize(Policy="LaQuanTriVien"),HttpGet("/quan-tri/quyen-hoat-dong/mo-danh-sach")]
 public async Task<IActionResult> MoDanhSach([ValidateNever] TraCuuNhaCungCapViewModel query) { var loc=query.Loc;var trang=query.Trang;return V("Admin/Providers",await C.MoDanhSachAsync(null,loc=="TatCa"?"":loc,trang)); }
 [Authorize(Policy="LaQuanTriVien"),HttpGet("/quan-tri/quyen-hoat-dong/xem-chi-tiet/{maNCC}")]
 public async Task<IActionResult> XemChiTiet([ValidateNever] TraCuuNhaCungCapViewModel query) { var maNCC=query.MaNCC;ViewBag.Assessment=await C.LayDanhGiaAsync(maNCC);return V("Admin/Provider",await C.XemChiTietAsync(maNCC)); }
}
