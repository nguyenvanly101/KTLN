using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
namespace HoTroDuLich.Web.Controllers;
[SucAuthorize(SucAccess.TravelSearch)]
public class ExploreController(TravelService s):AppController(s){
 public IActionResult Index([FromQuery]HoTroDuLich.Web.Models.DiscoveryFilter f)=>LocalRedirect(PlaceText.SearchUrl(new(){Q=f.Q??f.Region,Category=PlaceText.CategoryFromLegacy(f.Type),PlanId=f.PlanId}));
 public async Task<IActionResult> Detail(long id,long? planId=null){await LoadTripContext(planId);ViewBag.Provider=await S.ServiceProvider(id);if(planId!=null&&User.IsInRole(TravelService.Traveler))ViewBag.Activities=(await S.Activities(planId.Value)).Where(x=>x.MaDichVu==id).ToList();return View(await S.PublicService(id));}
 public async Task<IActionResult> Place(long id,long? planId=null){await LoadTripContext(planId);var result=await HttpContext.RequestServices.GetRequiredService<ITimKiemThongTinService>().XemChiTietAsync("DiaDiem",id);ViewBag.Services=result.DichVuTaiDiem;return View(result.DiaDiem);}
}
