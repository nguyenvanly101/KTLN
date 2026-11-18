using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HoTroDuLich.Web.Controllers;

public sealed class DiscoveryController(TravelService travel,DiscoveryService discovery):AppController(travel)
{
 [HttpGet]
 public async Task<IActionResult> Index([FromQuery]DiscoveryFilter f)
 {
  await LoadTripContext(f.PlanId);
  // Trang chu: khong co dieu kien tim kiem nao -> man hinh bat dau theo nhu cau; co dieu kien -> ket qua.
  if(Request.Query.Keys.All(k=>k.Equals("PlanId",StringComparison.OrdinalIgnoreCase)))return V("Discovery/Home",await discovery.HomePage());
  return V("Discovery/Search",await discovery.Search(f));
 }
 [HttpGet("/diem-den")] public async Task<IActionResult> Destinations(string? viec=null){ViewData["Viec"]=viec;return V("Discovery/DestinationIndex",await discovery.DestinationIndex());}
 [HttpGet("/diem-den/{id:long}")] public async Task<IActionResult> Destination(long id,string? tab=null,string? q=null,long? area=null,long? planId=null){await LoadTripContext(planId);return V("Discovery/Destination",await discovery.Destination(id,tab,q,area));}
 [HttpGet] public async Task<IActionResult> Entry(long id,string? topic=null,long? planId=null){await LoadTripContext(planId);ViewData["ExperienceTopic"]=topic;var page=await discovery.Detail(id,topic:topic);if(page.Entry.BranchId!=null)return LocalRedirect("/co-so/"+page.Entry.BranchId);if(page.Entry.ServiceId!=null)return LocalRedirect("/Explore/Detail/"+page.Entry.ServiceId+(planId!=null?"?planId="+planId:""));return V("Discovery/Entry",page);}
 [HttpGet] public async Task<IActionResult> Compare([FromQuery]long[] ids){if(ids.Distinct().Count()<2){TempData["Success"]="Đánh dấu ít nhất 2 lựa chọn cùng loại rồi bấm So sánh.";return Redirect("/Discovery");}return V("Discovery/Compare",await discovery.Compare(ids));}
 [Authorize,HttpGet] public async Task<IActionResult> Library()=>V("Discovery/Library",await discovery.Library());
 [Authorize(Roles=TravelService.AdminRole+","+TravelService.ProviderRole),HttpGet]public async Task<IActionResult> Manage(string tab="entries")=>V("Discovery/Manage",await discovery.Manage(tab));
 [Authorize(Roles=TravelService.AdminRole+","+TravelService.ProviderRole),HttpGet]public async Task<IActionResult> Edit(long id=0){ViewBag.DsParents=await discovery.ParentChoices();return V("Discovery/Edit",await discovery.EditForm(id));}
 [Authorize(Roles=TravelService.AdminRole),HttpPost]public async Task<IActionResult> Save(DiscoveryEditInput i){i.AttributesJson=BranchTemplates.Collect(Request.Form,ServiceKinds.ForDiscoveryKind(i.Kind));var id=await discovery.Save(i);return Done(i.Status=="Published"?DiscoveryText.Link(id):"/Discovery/Manage","Đã lưu thông tin và lịch sử cập nhật.");}
 [Authorize(Roles=TravelService.ProviderRole),HttpPost]public async Task<IActionResult> Propose(DiscoveryEditInput i){await discovery.Propose(i);return Done("/Discovery/Manage?tab=proposals","Đã gửi đề nghị bổ sung. Nội dung được công bố sau khi quản trị viên kiểm tra.");}
 [Authorize(Roles=TravelService.AdminRole),HttpPost]public async Task<IActionResult> ReviewProposal(DiscoveryCommand i){await discovery.ReviewProposal(i);return Done("/Discovery/Manage?tab=proposals");}
 [Authorize,HttpPost]public async Task<IActionResult> Ask(DiscoveryCommand i){await discovery.Ask(i);return Done(DiscoveryText.Link(i.EntryId)+"#questions","Đã gửi câu hỏi. Câu trả lời được công bố sau khi kiểm tra.");}
 [Authorize(Roles=TravelService.AdminRole+","+TravelService.ProviderRole),HttpPost]public async Task<IActionResult> Answer(DiscoveryCommand i){await discovery.Answer(i);return Done("/Discovery/Manage?tab=questions");}
 [Authorize(Roles=TravelService.Traveler),HttpPost]public async Task<IActionResult> Experience(DiscoveryCommand i){await discovery.Experience(i);return Done(DiscoveryText.Link(i.EntryId)+"#experiences","Đã gửi kinh nghiệm để kiểm duyệt.");}
 [Authorize(Roles=TravelService.AdminRole),HttpPost]public async Task<IActionResult> ModerateExperience(DiscoveryCommand i){await discovery.ModerateExperience(i);return Done("/Discovery/Manage?tab=experiences");}
 [Authorize,HttpPost]public async Task<IActionResult> Report(DiscoveryCommand i){await discovery.Report(i);return Done(DiscoveryText.Link(i.EntryId)+"#report","Đã gửi phản ánh. Theo dõi kết quả trong Thư viện tra cứu.");}
 [Authorize(Roles=TravelService.AdminRole),HttpPost]public async Task<IActionResult> HandleReport(DiscoveryCommand i){await discovery.HandleReport(i);return Done("/Discovery/Manage?tab=reports");}
 [Authorize,HttpPost]public async Task<IActionResult> Follow(DiscoveryCommand i){await discovery.Follow(i);return Done(Url.IsLocalUrl(i.ReturnUrl)?i.ReturnUrl!:DiscoveryText.Link(i.EntryId));}
 [Authorize,HttpPost]public async Task<IActionResult> Collection(DiscoveryCommand i){await discovery.Collection(i);return Done("/Discovery/Library","Đã cập nhật danh sách của bạn.");}
 [Authorize,HttpPost]public async Task<IActionResult> SaveRoute(DiscoveryRouteInput i){await discovery.SaveRoute(i);return Done("/Discovery/Library","Đã lưu tuyến. Thời gian đường đi được tính lại khi mở.");}
 [Authorize,HttpPost]public async Task<IActionResult> ArchiveRoute(DiscoveryCommand i){await discovery.ArchiveRoute(i);return Done("/Discovery/Library");}
 internal async Task<IActionResult> Invalid(DiscoveryCommand i)
 {
  if(Request.HasFormContentType){var f=await Request.ReadFormAsync();ViewData["RejectedForm"]=System.Text.Json.JsonSerializer.Serialize(f.Where(x=>x.Key!="__RequestVerificationToken").ToDictionary(x=>x.Key,x=>x.Value.ToArray()));}
  if(i is DiscoveryEditInput edit){if(edit.Id>0)edit.LinkedKind=(await discovery.EditForm(edit.Id)).LinkedKind;ViewBag.DsParents=await discovery.ParentChoices();ViewData["UiController"]="Discovery";return V("Discovery/Edit",edit);}
  var action=RouteData.Values["action"]?.ToString();if(action is "Ask" or "Experience" or "Report" or "Follow")return V("Discovery/Entry",await discovery.Detail(i.EntryId,i));
  if(action is "Collection" or "SaveRoute" or "ArchiveRoute")return V("Discovery/Library",await discovery.Library(i));
  var tab=action switch{"Answer"=>"questions","HandleReport"=>"reports","ModerateExperience"=>"experiences","ReviewProposal"=>"proposals",_=>"entries"};return V("Discovery/Manage",await discovery.Manage(tab,i));
 }
}
