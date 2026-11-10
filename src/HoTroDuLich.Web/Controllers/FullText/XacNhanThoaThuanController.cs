using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class XacNhanThoaThuanController(IThoaThuanDongHanhService component,TravelService read):FullTextController<IThoaThuanDongHanhService>(component,read) {
 [Authorize(Policy="LaKhachDuLich"),HttpGet("/dong-hanh/thoa-thuan/xem-de-nghi/{maThoaThuan}")]
 public async Task<IActionResult> XemDeNghi([ValidateNever] XacNhanThoaThuanViewModel query) { var maThoaThuan=query.MaThoaThuan;var t=await C.XemDeNghiAsync(maThoaThuan);return await AgreementPage(t.MaDeNghi); }
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/dong-hanh/thoa-thuan/xac-nhan")]
 public async Task<IActionResult> XacNhan(XacNhanThoaThuanViewModel i) { if(i.Decision=="XacNhan")i.Decision="DaXacNhan";await C.XacNhanAsync(i);var t=await C.XemDeNghiAsync(i.Id);return Done("/dong-hanh/thoa-thuan/xem-de-nghi/"+t.MaThoaThuan); }
}
