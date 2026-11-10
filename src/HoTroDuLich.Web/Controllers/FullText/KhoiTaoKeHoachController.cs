using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class KhoiTaoKeHoachController(ILapKeHoachService component,TravelService read,TripWorkspaceService trips):FullTextController<ILapKeHoachService>(component,read) {
 [Authorize(Policy="LaKhachDuLich"),HttpGet("/ke-hoach/mo-ke-hoach-moi")]
 public async Task<IActionResult> MoKeHoachMoi([ValidateNever] KhoiTaoKeHoachViewModel query) { return V("Plans/Edit",await trips.NewPlan(query.ContinueServiceId,query.ContinuePlaceId)); }
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/ke-hoach/khoi-tao-ke-hoach")]
 public async Task<IActionResult> KhoiTaoKeHoach(KhoiTaoKeHoachViewModel i) { return Done(TripWorkspaceService.AfterPlanSaved(await C.KhoiTaoKeHoachAsync(i),i)); }
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/ke-hoach/cap-nhat-thong-tin")]
 public async Task<IActionResult> CapNhatThongTin(KhoiTaoKeHoachViewModel i) { S.Ensure(i.Id>0,"0201-02");return Done("/ke-hoach/"+await C.CapNhatThongTinAsync(i)); }
}
