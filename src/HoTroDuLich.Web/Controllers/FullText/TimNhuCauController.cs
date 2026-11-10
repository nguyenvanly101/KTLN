using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class TimNhuCauController(ITimNguoiDongHanhService component,TravelService read):FullTextController<ITimNguoiDongHanhService>(component,read) {
 [AllowAnonymous,HttpGet("/dong-hanh/tim-kiem/mo-tim-kiem")]
 public async Task<IActionResult> MoTimKiem([ValidateNever] TimNhuCauViewModel query) { await C.MoTimKiemAsync();ViewBag.Names=await S.DisplayNames();return V("Companions/Index",await C.TimNhuCauAsync(null,null,null,null,null)); }
 [AllowAnonymous,HttpGet("/dong-hanh/tim-kiem/tim-nhu-cau")]
 public async Task<IActionResult> TimNhuCau([ValidateNever] TimNhuCauViewModel query) { var noiDen=query.NoiDen;var tuNgay=query.TuNgay;var denNgay=query.DenNgay;var nganSachToiDa=query.NganSachToiDa;var hinhThucDi=query.HinhThucDi;var trang=query.Trang;ViewBag.Names=await S.DisplayNames();ViewBag.SimplePage=trang;var rows=await C.TimNhuCauAsync(noiDen,tuNgay,denNgay,nganSachToiDa,hinhThucDi,trang);ViewBag.SimpleNext=rows.Count==20;return V("Companions/Index",rows); }
}
