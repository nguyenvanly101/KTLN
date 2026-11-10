using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoTroDuLich.Web.Controllers;

[Authorize(Roles=TravelService.Traveler),SucAuthorize(SucAccess.TripPlan)]
public sealed class JourneyController(TravelService travel,JourneyService journey):AppController(travel)
{
    [HttpGet] public async Task<IActionResult> Today(long planId,DateOnly? date=null)=>View(await journey.Day(planId,date));
    [HttpPost] public async Task<IActionResult> Complete(long planId,CommandInput input){await journey.Complete(planId,input);return Done("/Journey/Today?planId="+planId,"Đã cập nhật tiến độ hoạt động.");}
    [HttpGet] public async Task<IActionResult> Share(long planId)=>View(await journey.Shares(planId));
    [HttpPost] public async Task<IActionResult> Share(ShareTripInput input){TempData["NewSharePath"]=await journey.CreateShare(input);return Done("/Journey/Share?planId="+input.PlanId,"Đã tạo bản chia sẻ. Sao chép liên kết ở khung bên dưới.");}
    [HttpPost] public async Task<IActionResult> Revoke(long planId,CommandInput input){await journey.Revoke(planId,input);return Done("/Journey/Share?planId="+planId,"Đã thu hồi liên kết chia sẻ.");}
    [HttpGet] public async Task<IActionResult> Print(long planId,bool includeBudget=false,bool includeServices=false){TripShareController.PrivateHeaders(Response);ViewData["PrivatePrint"]=true;ViewData["PlanId"]=planId;return View("/Views/Journey/SharedTrip.cshtml",await journey.Snapshot(planId,includeBudget,includeServices));}
}
public sealed class TripShareController(TravelService travel,JourneyService journey):AppController(travel)
{
    internal static void PrivateHeaders(HttpResponse response){response.Headers.CacheControl="no-store, private";response.Headers["Referrer-Policy"]="no-referrer";response.Headers["X-Robots-Tag"]="noindex, nofollow, noarchive";}
    [AllowAnonymous,HttpGet("/s/{token}")]
    public async Task<IActionResult> Open(string token){PrivateHeaders(Response);return View("/Views/Journey/SharedTrip.cshtml",await journey.PublicShare(token));}
}
