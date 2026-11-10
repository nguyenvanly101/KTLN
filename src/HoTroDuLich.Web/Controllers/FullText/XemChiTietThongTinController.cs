using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class XemChiTietThongTinController(ITimKiemThongTinService component,TravelService read):FullTextController<ITimKiemThongTinService>(component,read) {
 [AllowAnonymous,HttpGet("/thong-tin/xem-chi-tiet")]
 public async Task<IActionResult> XemChiTiet([ValidateNever] XemChiTietThongTinViewModel query) { await LoadTripContext(query.PlanId);var loai=query.Loai;var maMuc=query.MaMuc;var result=await C.XemChiTietAsync(loai,maMuc);ViewBag.Provider=result.NhaCungCap;ViewBag.Services=result.DichVuTaiDiem;return result.DiaDiem!=null?V("Explore/Place",result.DiaDiem):V("Explore/Detail",result.DichVu); }
}
