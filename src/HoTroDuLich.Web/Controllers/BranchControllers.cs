using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoTroDuLich.Web.Controllers;

[Authorize(Roles=TravelService.ProviderRole)]
public sealed class BranchesController(TravelService travel,BranchService branches):AppController(travel)
{
 [HttpGet("/nha-cung-cap/co-so")] public async Task<IActionResult> Index()=>V("Branches/Index",await branches.OwnerPage());
 [HttpGet("/nha-cung-cap/co-so/them")] public async Task<IActionResult> New()=>V("Branches/Edit",await branches.EditPage());
 [HttpGet("/nha-cung-cap/co-so/{id:long}")] public async Task<IActionResult> Edit(long id)=>V("Branches/Edit",await branches.EditPage(id));
 [HttpPost("/nha-cung-cap/co-so/luu")] public async Task<IActionResult> Save(BranchInput input,CancellationToken ct)
 {var id=await branches.Save(input,ct);return Done("/nha-cung-cap/co-so/"+id,input.Operation=="Draft"?"Đã lưu bản nháp cơ sở.":"Đã gửi cơ sở chờ duyệt. Thông tin đang công bố được giữ đến khi bản cập nhật được chấp thuận.");}
 [HttpPost("/nha-cung-cap/co-so/hien-thi")]public async Task<IActionResult> Toggle(CommandInput input){await branches.Toggle(input);return Done("/nha-cung-cap/co-so");}
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
