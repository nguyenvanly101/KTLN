using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class TheoDoiYeuCauController(IDatDichVuService component,TravelService read):FullTextController<IDatDichVuService>(component,read) {
 [Authorize(Policy="LaKhachDuLich"),HttpGet("/yeu-cau/cua-toi")]
 public async Task<IActionResult> DanhSachCuaToi([ValidateNever] TheoDoiYeuCauViewModel query) { var trangThai=query.TrangThai;var trang=query.Trang;ViewBag.ServiceNames=await C.LayTenDichVuAsync();return V("Bookings/Index",Page(FilterBookings(await C.DanhSachCuaToiAsync(),trangThai),trang)); }
 [Authorize(Policy="LaKhachDuLich"),HttpGet("/yeu-cau/{maYeuCau}")]
 public async Task<IActionResult> ChiTiet([ValidateNever] TheoDoiYeuCauViewModel query) { var maYeuCau=query.MaYeuCau;ViewBag.ServiceName=await C.LayTenDichVuCuaYeuCauAsync(maYeuCau);return V("Bookings/Detail",await C.ChiTietAsync(maYeuCau)); }
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/yeu-cau/phan-hoi-de-xuat")]
 public async Task<IActionResult> PhanHoiDeXuat(TheoDoiYeuCauViewModel i) { await C.PhanHoiDeXuatAsync(i);return Done("/yeu-cau/"+i.Id); }
}
