using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class XemNhuCauChiaSeController(ITimNguoiDongHanhService component,TravelService read):FullTextController<ITimNguoiDongHanhService>(component,read) {
 [AllowAnonymous,HttpGet("/dong-hanh/tim-kiem/xem-chi-tiet/{maNhuCau}")]
 public async Task<IActionResult> XemChiTiet([ValidateNever] XemNhuCauChiaSeViewModel query) { var maNhuCau=query.MaNhuCau;var n=await C.XemChiTietAsync(maNhuCau);ViewBag.Person=await S.SharedProfile(n.MaTaiKhoan);ViewBag.Names=await S.DisplayNames();return V("Companions/Detail",n); }
}
