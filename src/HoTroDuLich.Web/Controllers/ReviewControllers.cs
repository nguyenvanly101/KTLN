using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoTroDuLich.Web.Controllers;

public sealed class ReviewsController(TravelService travel,ReviewService reviews):AppController(travel)
{
    [AllowAnonymous,HttpGet] public async Task<IActionResult> Service(long id,int page=1)=>View(await reviews.PublicFeed(id,page));
    [Authorize(Roles=TravelService.Traveler),HttpGet] public async Task<IActionResult> Mine()=>View(await reviews.Mine());
    [Authorize(Roles=TravelService.Traveler),HttpGet] public async Task<IActionResult> Write(long bookingId)=>View(await reviews.Write(bookingId));
    [Authorize(Roles=TravelService.Traveler),HttpPost] public async Task<IActionResult> Write(ServiceReviewInput input){await reviews.Save(input);return Done("/Reviews/Mine","Đã gửi đánh giá. Nội dung được công bố sau khi xét duyệt.");}
    [Authorize(Roles=TravelService.ProviderRole),HttpGet] public async Task<IActionResult> Partner(long serviceId=0,int page=1,string status="")=>View("Manage",await reviews.Manage(false,page,status,serviceId));
    [Authorize(Roles=TravelService.ProviderRole),HttpPost] public async Task<IActionResult> Reply(ReviewReplyInput input){await reviews.Reply(input);return Done("/Reviews/Partner?serviceId="+input.ServiceId,"Đã gửi phản hồi để xét duyệt.");}
    [Authorize(Roles=TravelService.AdminRole),HttpGet] public async Task<IActionResult> Moderation(int page=1,string status="ChoDuyet")=>View("Manage",await reviews.Manage(true,page,status));
    [Authorize(Roles=TravelService.AdminRole),HttpPost] public async Task<IActionResult> Moderate(ReviewModerationInput input){await reviews.Moderate(input);return Done("/Reviews/Moderation","Đã cập nhật kết quả kiểm duyệt.");}
}

public sealed class AvailabilityController(TravelService travel,AvailabilityService availability):AppController(travel)
{
    [AllowAnonymous,HttpGet] public async Task<IActionResult> Index(long serviceId,string? month=null,long? planId=null){await LoadTripContext(planId);return View(await availability.Page(serviceId,month,false));}
    [Authorize(Roles=TravelService.ProviderRole),SucAuthorize(SucAccess.ServiceManagement),HttpGet] public async Task<IActionResult> Manage(long serviceId,string? month=null,DateOnly? edit=null)=>View("Index",await availability.Page(serviceId,month,true,edit));
    [Authorize(Roles=TravelService.ProviderRole),SucAuthorize(SucAccess.ServiceManagement),HttpPost] public async Task<IActionResult> Save(AvailabilityInput input){await availability.Save(input);return Done("/Availability/Manage?serviceId="+input.ServiceId+"&month="+input.Date.ToString("yyyy-MM")+"&edit="+input.Date.ToString("yyyy-MM-dd"),"Đã cập nhật lịch phục vụ. Yêu cầu đã xác nhận vẫn được giữ.");}
    [Authorize(Roles=TravelService.ProviderRole),SucAuthorize(SucAccess.ServiceManagement),HttpPost] public async Task<IActionResult> Remove(long serviceId,CommandInput input){await availability.Remove(serviceId,input);return Done("/Availability/Manage?serviceId="+serviceId,"Đã đưa ngày phục vụ về trạng thái chưa cập nhật.");}
}
