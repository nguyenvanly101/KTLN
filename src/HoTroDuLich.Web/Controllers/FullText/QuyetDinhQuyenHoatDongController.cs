using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class QuyetDinhQuyenHoatDongController(IQuyenHoatDongService component,TravelService read):FullTextController<IQuyenHoatDongService>(component,read) {
 [Authorize(Policy="LaQuanTriVien"),HttpGet("/quan-tri/quyen-hoat-dong/xem-he-qua")]
 public async Task<IActionResult> XemHeQua([ValidateNever] QuyetDinhQuyenHoatDongViewModel query) { var maNCC=query.MaNCC;var thaoTac=query.ThaoTac;var decision=thaoTac=="KhoiPhuc"?"DangHoatDong":thaoTac=="ThuHoi"?"DaThuHoi":thaoTac;return V("Admin/Consequences",await C.XemHeQuaAsync(maNCC,decision)); }
 [Authorize(Policy="LaQuanTriVien"),HttpPost("/quan-tri/quyen-hoat-dong/tam-dinh-chi")]
 public async Task<IActionResult> TamDinhChi(QuyetDinhQuyenHoatDongViewModel i) { i.Decision="TamDinhChi";await C.TamDinhChiAsync(i);return Done("/quan-tri/quyen-hoat-dong/xem-chi-tiet/"+i.Id); }
 [Authorize(Policy="LaQuanTriVien"),HttpPost("/quan-tri/quyen-hoat-dong/khoi-phuc")]
 public async Task<IActionResult> KhoiPhuc(QuyetDinhQuyenHoatDongViewModel i) { i.Decision="DangHoatDong";await C.KhoiPhucAsync(i);return Done("/quan-tri/quyen-hoat-dong/xem-chi-tiet/"+i.Id); }
 [Authorize(Policy="LaQuanTriVien"),HttpPost("/quan-tri/quyen-hoat-dong/thu-hoi")]
 public async Task<IActionResult> ThuHoi(QuyetDinhQuyenHoatDongViewModel i) { i.Decision="DaThuHoi";await C.ThuHoiAsync(i);return Done("/quan-tri/quyen-hoat-dong/xem-chi-tiet/"+i.Id); }
 [Authorize(Policy="LaQuanTriVien"),HttpPost("/quan-tri/quyen-hoat-dong/xem-xet-giai-trinh")]
 public async Task<IActionResult> XemXetGiaiTrinh(QuyetDinhQuyenHoatDongViewModel i) { var g=await S.Find<GiaiTrinhNhaCungCap>(i.Id);var d=await S.Find<QuyetDinhQuyenHoatDong>(g.MaQuyetDinh);await C.XemXetGiaiTrinhAsync(i);return Done("/quan-tri/quyen-hoat-dong/xem-chi-tiet/"+d.MaNhaCungCap); }
}
