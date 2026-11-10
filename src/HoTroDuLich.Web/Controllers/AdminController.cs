using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HoTroDuLich.Web.Controllers;
[Authorize(Roles=TravelService.AdminRole)]
public class AdminController(TravelService s):AppController(s){
 public async Task<IActionResult> Index()=>View(await S.AdminOverview());
 [SucAuthorize(SucAccess.ServiceReview)]
 public async Task<IActionResult> ServiceReviews(string? q,string? status=null,int page=1)=>View(await S.ServiceReviews(q,status??(Request.Query.ContainsKey("status")?"":"ChoDuyet"),page));
 [SucAuthorize(SucAccess.ProviderVerification)]
 public async Task<IActionResult> Verifications(string? q,string? status=null,int page=1)=>View(await S.Verifications(q,status??(Request.Query.ContainsKey("status")?"":"ChoXacMinh"),page));
 [SucAuthorize(SucAccess.ProviderRights)]
 public async Task<IActionResult> Providers(string? q,string? status="",int page=1)=>View(await S.ProviderRightsList(q,status??"",page));
 [SucAuthorize(SucAccess.ServiceReview)]
 public async Task<IActionResult> Review(long id)=>View(await S.ReviewDetail(id));
 [SucAuthorize(SucAccess.ServiceReview)]
 [HttpPost] public async Task<IActionResult> Review(CommandInput i){await S.ReviewService(i);return Done("/Admin/ServiceReviews");}
 [SucAuthorize(SucAccess.ProviderVerification)]
 public async Task<IActionResult> Verification(long id)=>View(await S.VerificationDetail(id));
 [SucAuthorize(SucAccess.ProviderVerification)]
 [HttpPost] public async Task<IActionResult> Verify(CommandInput i){await S.VerifyProvider(i);return Done("/Admin/Verifications");}
 [SucAuthorize(SucAccess.ProviderRights)]
 public async Task<IActionResult> Provider(long id){ViewBag.Assessment=await HttpContext.RequestServices.GetRequiredService<IQuyenHoatDongService>().LayDanhGiaAsync(id);return View(await S.RightsDetail(id));}
 [SucAuthorize(SucAccess.ProviderRights),HttpGet] public async Task<IActionResult> Consequences(long id,string decision)=>View(await S.RightsPreview(id,decision));
 [SucAuthorize(SucAccess.ProviderRights)]
 [HttpPost] public async Task<IActionResult> Rights(CommandInput i){await S.SetProviderRights(i);return Done("/Admin/Provider/"+i.Id);}
 [SucAuthorize(SucAccess.ProviderRights)]
 [HttpPost] public async Task<IActionResult> Explanation(CommandInput i,long providerId){await S.ReviewExplanation(i);return Done("/Admin/Provider/"+providerId);}
 public async Task<IActionResult> Audit()=>View(await S.AuditLog());
}
