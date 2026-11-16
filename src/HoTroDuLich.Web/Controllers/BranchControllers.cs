using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoTroDuLich.Web.Controllers;

[Authorize(Roles=TravelService.ProviderRole)]
public sealed class BranchesController(TravelService travel,BranchService branches,PostService posts):AppController(travel)
{
 [HttpGet("/nha-cung-cap/co-so")] public async Task<IActionResult> Index()=>V("Branches/Index",await branches.OwnerPage());
 [HttpGet("/nha-cung-cap/co-so/them")] public async Task<IActionResult> New(long? tu=null){var draft=tu is long entry?await branches.ClaimDraft(entry):null;if(draft!=null)ViewData["ClaimedFrom"]=draft.Name;return V("Branches/Edit",await branches.EditPage(0,draft));}
 [HttpGet("/nha-cung-cap/co-so/{id:long}")] public async Task<IActionResult> Edit(long id)=>V("Branches/Edit",await branches.EditPage(id));
 [HttpPost("/nha-cung-cap/co-so/luu")] public async Task<IActionResult> Save(BranchInput input,CancellationToken ct)
 {input.AttributesJson=BranchTemplates.Collect(Request.Form,input.Type);var id=await branches.Save(input,ct);return Done("/nha-cung-cap/co-so/"+id,input.AppliedNow?"Đã cập nhật ngay: giờ mở cửa, giá, liên hệ và tiện ích không cần chờ duyệt.":input.Operation=="Draft"?"Đã lưu bản nháp cơ sở. Hãy bổ sung giờ, giá, ảnh để khách dễ chọn.":"Đã gửi cơ sở chờ duyệt. Thông tin đang công bố được giữ đến khi bản cập nhật được chấp thuận.");}
 [HttpPost("/nha-cung-cap/co-so/chon")] public async Task<IActionResult> Choose(long id,string? returnUrl){await branches.SetCurrentBranch(id);return LocalRedirect(Url.IsLocalUrl(returnUrl)?returnUrl!:"/bang-tin");}
 [HttpPost("/nha-cung-cap/co-so/hien-thi")]public async Task<IActionResult> Toggle(CommandInput input){await branches.Toggle(input);return Done("/nha-cung-cap/co-so");}
 [HttpGet("/nha-cung-cap/co-so/{id:long}/tin")] public async Task<IActionResult> Posts(long id)=>V("Branches/Posts",await posts.BranchPostsPage(id));
 [HttpPost("/nha-cung-cap/co-so/{id:long}/tin"),RequestSizeLimit(24*1024*1024)] public async Task<IActionResult> AddPost(long id,PostInput input){await posts.CreateBranchPost(id,input);return Done("/nha-cung-cap/co-so/"+id+"/tin","Đã đăng tin giới thiệu. Khách thấy tin này trên bảng tin cộng đồng và trang cơ sở.");}
 [HttpPost("/nha-cung-cap/co-so/{id:long}/tin/{postId:long}/xoa")] public async Task<IActionResult> DeletePost(long id,long postId){await posts.DeleteBranchPost(postId);return Done("/nha-cung-cap/co-so/"+id+"/tin","Đã xóa tin.");}
 public async Task<IActionResult> Invalid(BranchInput input)=>V("Branches/Edit",await branches.EditPage(input.Id,input));
}

[Authorize(Roles=TravelService.AdminRole)]
public sealed class BranchAdminController(TravelService travel,BranchService branches):AppController(travel)
{
 [HttpGet("/quan-tri/co-so")]public async Task<IActionResult> Index(string status="Pending"){ViewData["BranchStatus"]=status;return V("Branches/Queue",await branches.Queue(status));}
 [HttpGet("/quan-tri/co-so/{id:long}")]public async Task<IActionResult> Review(long id)=>V("Branches/Review",await branches.ReviewPage(id));
 [HttpPost("/quan-tri/co-so/duyet")]public async Task<IActionResult> Decide(CommandInput input){await branches.Review(input);return Done("/quan-tri/co-so","Đã xử lý phiên bản cơ sở.");}
}

[Authorize(Roles=TravelService.AdminRole)]
public sealed class LandmarksController(TravelService travel,BranchService branches):AppController(travel)
{
 [HttpGet("/quan-tri/moc-dia-diem")]public async Task<IActionResult> Index()=>V("Places/Landmarks",await branches.Landmarks(true));
 [HttpGet("/quan-tri/moc-dia-diem/them")]public async Task<IActionResult> New(){ViewBag.Landmarks=await branches.Landmarks(true);return V("Places/LandmarkEdit",await branches.LandmarkForm());}
 [HttpGet("/quan-tri/moc-dia-diem/{id:long}")]public async Task<IActionResult> Edit(long id){ViewBag.Landmarks=await branches.Landmarks(true);return V("Places/LandmarkEdit",await branches.LandmarkForm(id));}
 [HttpPost("/quan-tri/moc-dia-diem/luu")]public async Task<IActionResult> Save(LandmarkInput input,CancellationToken ct){var id=await branches.SaveLandmark(input,ct);return Done("/quan-tri/moc-dia-diem/"+id,"Đã lưu mốc địa điểm và nguồn thông tin.");}
 public async Task<IActionResult> Invalid(LandmarkInput input){ViewBag.Landmarks=await branches.Landmarks(true);return V("Places/LandmarkEdit",input);}
}

[Authorize(Roles=TravelService.AdminRole)]
public sealed class TravelerAdminController(TravelService travel,TravelerAdminService accounts):AppController(travel)
{
 [HttpGet("/quan-tri/khach-du-lich")] public async Task<IActionResult> Index(string? q,string? status)=>V("Admin/Travelers",await accounts.List(q,status));
 [HttpPost("/quan-tri/khach-du-lich/trang-thai")] public async Task<IActionResult> SetStatus(long id,bool active,string? reason){await accounts.SetActive(id,active,reason);return Done("/quan-tri/khach-du-lich",active?"Đã mở khóa tài khoản.":"Đã khóa tài khoản. Phiên đăng nhập hiện tại bị hủy.");}
}
