using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class GiaiTrinhController(IQuyenHoatDongService component,TravelService read):FullTextController<IQuyenHoatDongService>(component,read) {
 [Authorize(Policy="LaNhaCungCap"),HttpGet("/nha-cung-cap/quyen-hoat-dong")]
 public async Task<IActionResult> QuyenCuaToi([ValidateNever] GiaiTrinhViewModel query) { return V("Provider/Rights",await C.QuyenCuaToiAsync()); }
 [Authorize(Policy="LaNhaCungCap"),HttpPost("/quan-tri/quyen-hoat-dong/nhan-giai-trinh")]
 public async Task<IActionResult> NhanGiaiTrinh(GiaiTrinhViewModel i) { if(i.Id==0)i.Id=(await C.LayQuyetDinhCuaToiAsync()).FirstOrDefault()?.MaQuyetDinh??0;await C.NhanGiaiTrinhAsync(i);return Done("/nha-cung-cap/quyen-hoat-dong","Đã gửi giải trình, chờ xem xét."); }
}
