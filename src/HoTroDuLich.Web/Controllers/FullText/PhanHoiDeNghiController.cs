using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class PhanHoiDeNghiController(IKetNoiDongHanhService component,TravelService read):FullTextController<IKetNoiDongHanhService>(component,read) {
 [Authorize(Policy="LaKhachDuLich"),HttpGet("/dong-hanh/de-nghi")]
 public async Task<IActionResult> DanhSach([ValidateNever] PhanHoiDeNghiViewModel query) { var loai=query.Loai;var trang=query.Trang;S.Require(loai is "nhan" or "gui","Bộ lọc không hợp lệ.",400);var rows=await C.DanhSachAsync();rows=rows.Where(x=>loai=="gui"?x.MaNguoiGui==S.UserId:x.MaNguoiGui!=S.UserId).ToList();ViewBag.Names=await S.DisplayNames();var owners=new Dictionary<long,long>();var destinations=new Dictionary<long,string>();foreach(var item in rows){owners[item.MaDeNghi]=await C.LayChuNhuCauAsync(item.MaDeNghi);destinations[item.MaDeNghi]=await C.LayNoiDenAsync(item.MaDeNghi);}ViewBag.Owners=owners;ViewBag.Destinations=destinations;return V("Companions/Connections",Page(rows,trang)); }
 [Authorize(Policy="LaKhachDuLich"),HttpGet("/dong-hanh/de-nghi/xem-de-nghi/{maDeNghi}")]
 public async Task<IActionResult> XemDeNghi([ValidateNever] PhanHoiDeNghiViewModel query) { var maDeNghi=query.MaDeNghi;var connection=await C.XemDeNghiAsync(maDeNghi);ViewBag.Sender=await S.SharedProfile(connection.MaNguoiGui);ViewBag.Owner=await S.SharedProfile(await C.LayChuNhuCauAsync(maDeNghi));ViewBag.Destination=await C.LayNoiDenAsync(maDeNghi);return V("Companions/Connection",connection); }
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/dong-hanh/de-nghi/phan-hoi")]
 public async Task<IActionResult> PhanHoi(PhanHoiDeNghiViewModel i) { if(i.Decision=="ChapNhan")i.Decision="DaChapNhan";if(i.Decision=="Rut")i.Decision="DaRut";await C.PhanHoiAsync(i);return Done("/dong-hanh/de-nghi/xem-de-nghi/"+i.Id); }
}
