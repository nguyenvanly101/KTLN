using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class TraCuuThongTinController(ITimKiemThongTinService component,TravelService read,PlaceSearchService places):FullTextController<ITimKiemThongTinService>(component,read) {
 [AllowAnonymous,HttpGet("/thong-tin/mo-tim-kiem")]
 public async Task<IActionResult> MoTimKiem([ValidateNever] TraCuuThongTinViewModel query) { await LoadTripContext(query.PlanId);await C.MoTimKiemAsync();return V("Places/Index",await places.Search(new(){PlanId=query.PlanId},HttpContext.RequestAborted)); }
 [AllowAnonymous,HttpGet("/thong-tin/tra-cuu")]
 public async Task<IActionResult> TraCuu([ValidateNever] TraCuuThongTinViewModel query) { await LoadTripContext(query.PlanId);await C.MoTimKiemAsync();await C.TraCuuAsync(query.TuKhoa,query.KhuVuc,query.Loai,query.GiaTu,query.GiaDen,query.Trang);var category=PlaceText.Categories.ContainsKey(query.Loai??"")?query.Loai??"":"";return V("Places/Index",await places.Search(new(){Q=query.TuKhoa,Category=category,Page=query.Trang,PlanId=query.PlanId},HttpContext.RequestAborted)); }
}
