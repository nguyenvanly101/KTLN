using System.Text.Json;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HoTroDuLich.Web.Controllers;

public sealed class TogetherController(TravelService travel,CompanionService companions):AppController(travel)
{
    private const string Traveler=TravelService.Traveler,Admin=TravelService.AdminRole;
    [AllowAnonymous,HttpGet] public async Task<IActionResult> Index(string? region,string? kind,DateOnly? start,DateOnly? end,decimal? budget,int page=1,bool matching=false)=>View(await companions.Discover(region,kind,start,end,budget,page,matching));
    [AllowAnonymous,HttpGet] public async Task<IActionResult> Detail(long id)=>View(await companions.Public(id));
    [Authorize(Roles=Traveler),HttpGet] public async Task<IActionResult> Mine(int page=1)=>View(await companions.Mine(page));
    [Authorize(Roles=Traveler),HttpGet] public async Task<IActionResult> Thread(long groupId,long after=0){Response.Headers.CacheControl="no-store";return Json(await companions.Thread(groupId,after));}
    [Authorize(Roles=Traveler),HttpGet] public async Task<IActionResult> Profile()=>View(await companions.Settings());
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> Profile(CompanionProfileInput input){await companions.SaveProfile(input);return Done("/Together/Profile","Đã lưu phong cách và lựa chọn hiển thị của bạn.");}
    [Authorize(Roles=Traveler),HttpGet] public async Task<IActionResult> Edit(long id=0,long? planId=null,long? connectionId=null){if(id==0&&connectionId!=null&&await companions.LinkedGroup(connectionId.Value) is string existing)return LocalRedirect(existing);var page=await companions.Form(id,planId,connectionId);ModelState.Clear();return View(page);}
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> Edit(CompanionGroupInput input){var id=await companions.SaveGroup(input);return Done(Link(id),"Đã lưu nhóm đồng hành. Điểm hẹn và trao đổi chỉ dành cho thành viên đã tham gia.");}
    [Authorize(Roles=Traveler),HttpGet] public async Task<IActionResult> Group(long id,string tab="overview",int page=1,int paymentPage=1){Response.Headers.CacheControl="no-store";return View(await companions.Workspace(id,tab,page,paymentPage));}
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> State(CompanionCommand input){await companions.GroupState(input);return Done(Link(input.GroupId));}
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> Join(CompanionMemberInput input){await companions.Join(input);return Done("/Together/Mine","Đã gửi yêu cầu. Bạn được vào không gian nhóm sau khi người tổ chức chấp nhận.");}
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> Invite(CompanionMemberInput input){await companions.Invite(input);return Done(Link(input.GroupId,"members"),"Đã gửi lời mời. Người nhận cần đồng ý để tham gia.");}
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> Member(CompanionMemberInput input){await companions.MemberAction(input);return Done(input.Decision is "Approve" or "Reject" or "Remove"?Link(input.GroupId,"members"):"/Together/Mine");}
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> Attendance(CompanionCommand input){await companions.Attendance(input);return Done(Link(input.GroupId));}
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> Block(CompanionMemberInput input){await companions.Block(input);return Done("/Together/Profile",input.Decision=="Unblock"?"Đã bỏ chặn.":"Đã chặn kết nối và lời mời mới. Tin nhắn của người bị chặn được ẩn trong nhóm chung; lịch, thành viên và chi phí vẫn được giữ.");}
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> Message(CompanionMessageInput input){await companions.Send(input);return Done(Link(input.GroupId,"chat"),"Đã gửi tin nhắn.");}
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> Read(CompanionCommand input){await companions.ReadNotice(input);return Done(Link(input.GroupId,"chat"),"Đã xác nhận đọc thông báo.");}
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> HideMessage(CompanionCommand input){await companions.HideMessage(input);return Done(Link(input.GroupId,"chat"),"Đã ẩn tin nhắn, lịch sử vẫn được lưu.");}
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> Poll(CompanionPollInput input){await companions.CreatePoll(input);return Done(Link(input.GroupId,"schedule"),"Đã mở bình chọn cho nhóm.");}
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> Vote(CompanionVoteInput input){await companions.Vote(input);return Done(Link(input.GroupId,"schedule"),"Đã ghi nhận lựa chọn của bạn.");}
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> ClosePoll(CompanionVoteInput input){await companions.ClosePoll(input);return Done(Link(input.GroupId,"schedule"),"Đã xử lý bình chọn. Lịch riêng và yêu cầu dịch vụ giữ nguyên.");}
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> Activity(CompanionActivityInput input){await companions.SaveActivity(input);return Done(Link(input.GroupId,"schedule"),"Đã lưu hoạt động vào lịch chung.");}
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> CancelActivity(CompanionCommand input){await companions.CancelActivity(input);return Done(Link(input.GroupId,"schedule"));}
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> Task(CompanionTaskInput input){await companions.SaveTask(input);return Done(Link(input.GroupId,"tasks"));}
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> TaskAction(CompanionCommand input){await companions.TaskAction(input);return Done(Link(input.GroupId,"tasks"));}
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> Expense(CompanionExpenseInput input){await companions.AddExpense(input);return Done(Link(input.GroupId,"expenses"),"Đã ghi khoản chi chung và phần chia.");}
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> CancelExpense(CompanionCommand input){await companions.CancelExpense(input);return Done(Link(input.GroupId,"expenses"));}
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> Payment(CompanionPaymentInput input){await companions.ProposePayment(input);return Done(Link(input.GroupId,"expenses"),"Đã ghi khoản hoàn trả ngoài ứng dụng, đang chờ người nhận xác nhận.");}
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> PaymentAction(CompanionCommand input){await companions.PaymentAction(input);return Done(Link(input.GroupId,"expenses"));}
    [Authorize(Roles=Traveler),HttpGet] public async Task<IActionResult> Report(long groupId,long? messageId=null)=>View(await companions.ReportForm(groupId,messageId));
    [Authorize(Roles=Traveler),HttpPost] public async Task<IActionResult> Report(CompanionReportInput input){await companions.SubmitReport(input);return Done("/Together/Reports","Đã gửi báo cáo để quản trị xem xét.");}
    [Authorize(Roles=Traveler),HttpGet] public async Task<IActionResult> Reports(int page=1)=>View("Moderation",await companions.Reports(false,page,""));
    [Authorize(Roles=Admin),HttpGet] public async Task<IActionResult> Moderation(int page=1,string status="ChoXuLy")=>View(await companions.Reports(true,page,status));
    [Authorize(Roles=Admin),HttpPost] public async Task<IActionResult> Moderate(CompanionModerationInput input){await companions.Moderate(input);return Done("/Together/Moderation","Đã xử lý báo cáo đồng hành.");}
    [Authorize(Roles=Admin),HttpPost] public async Task<IActionResult> Restore(CompanionModerationInput input){await companions.Restore(input);return Done("/Together/Moderation?status=DaXuLy","Đã khôi phục hiển thị nhóm.");}
    private static string Link(long id,string tab="overview")=>"/Together/Group/"+id+"?tab="+tab;
    [NonAction] public async Task<IActionResult> Invalid(CompanionCommand input)
    {
        ViewData["CompanionInput"]=input;
        if(Request.HasFormContentType){var form=await Request.ReadFormAsync();ViewData["RejectedForm"]=JsonSerializer.Serialize(form.Where(x=>x.Key!="__RequestVerificationToken").ToDictionary(x=>x.Key,x=>x.Value.ToArray()));}
        return input switch{
            CompanionGroupInput i=>V("Together/Edit",await companions.Form(i.Id,input:i)),
            CompanionProfileInput i=>V("Together/Profile",await companions.Settings(i)),
            CompanionReportInput i=>V("Together/Report",await companions.ReportForm(i.GroupId,i.MessageId,i)),
            CompanionModerationInput i=>V("Together/Moderation",await companions.Reports(true,status:"",focusId:i.Id)),
            CompanionMemberInput when Request.Path=="/Together/Join"=>V("Together/Detail",await companions.Public(input.GroupId)),
            CompanionMemberInput when Request.Path=="/Together/Block"=>V("Together/Profile",await companions.Settings()),
            CompanionMemberInput when Request.Path=="/Together/Member"&&input.Decision is "Accept" or "Decline" or "Withdraw" or "Leave"=>V("Together/Mine",await companions.Mine()),
            _=>V("Together/Group",await companions.Workspace(input.GroupId,CompanionLabels.Tabs.ContainsKey(input.Tab)?input.Tab:"overview"))
        };
    }
}
