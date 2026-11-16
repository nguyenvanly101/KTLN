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
    [HttpPost] public async Task<IActionResult> Depart(long planId){await journey.Depart(planId);await TellGroup(planId,"Mình vừa bấm Khởi hành cho chuyến đi. Mọi người xem lịch hôm nay và đường đi trong trang chuyến đi nhé.");return Done("/Journey/Today?planId="+planId,"Chúc bạn một chuyến đi vui vẻ. Đã ghi lại giờ khởi hành.");}
    [HttpPost] public async Task<IActionResult> Finish(long planId){await journey.Finish(planId);await TellGroup(planId,"Chuyến đi đã kết thúc. Cảm ơn mọi người đã đi cùng; nhớ chia tiền và đánh giá dịch vụ đã dùng nhé.");return Done("/Journey/Today?planId="+planId,"Chuyến đi đã kết thúc. Cảm ơn bạn đã đi cùng.");}
    /// <summary>Gui mot tin vao nhom chat cua chuyen di (neu co). Loi gui tin khong lam hong thao tac chinh.</summary>
    async Task TellGroup(long planId,string text)
    {
        try
        {
            var chat=HttpContext.RequestServices.GetRequiredService<ChatService>();
            if(await chat.TripGroupFor(planId) is long group)await chat.Send(new ChatSendInput{ConversationId=group,Body=text});
        }
        catch(Exception e) when(e is BusinessException or Microsoft.EntityFrameworkCore.DbUpdateException){}
    }
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
