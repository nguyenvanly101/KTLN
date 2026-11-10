using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HoTroDuLich.Web.Controllers;
public class CompanionsController(TravelService s):AppController(s){
 [SucAuthorize(SucAccess.CompanionSearch)] public async Task<IActionResult> Index(string? q,DateOnly? start,DateOnly? end,decimal? budget,string? style){ViewBag.Names=await S.DisplayNames();return View(await S.Needs(q,start,end,budget,style));}
 [SucAuthorize(SucAccess.CompanionSearch)] public async Task<IActionResult> Detail(long id){var n=await S.Need(id);ViewBag.Names=await S.DisplayNames();ViewBag.Person=await S.SharedProfile(n.MaTaiKhoan);return View(n);}
 [SucAuthorize(SucAccess.CompanionPost)]
 [Authorize(Roles=TravelService.Traveler)] public async Task<IActionResult> Mine()=>View(await S.MyNeeds());
 [SucAuthorize(SucAccess.CompanionPost)]
 [Authorize(Roles=TravelService.Traveler),HttpGet] public async Task<IActionResult> Edit(long id=0)=>View(id==0?new NeedInput():await S.OwnNeedInput(id));
 [SucAuthorize(SucAccess.CompanionPost)]
 [Authorize(Roles=TravelService.Traveler),HttpPost] public IActionResult Edit(NeedInput i){S.ValidateNeed(i);return View("Preview",i);}
 [SucAuthorize(SucAccess.CompanionPost),Authorize(Roles=TravelService.Traveler),HttpPost] public async Task<IActionResult> Publish(NeedInput i){await S.SaveNeed(i);return Done("/Companions/Mine","Đã công bố nhu cầu với phạm vi chia sẻ đã chọn.");}
 [SucAuthorize(SucAccess.CompanionPost),Authorize(Roles=TravelService.Traveler),HttpPost] public IActionResult Revise(NeedInput i)=>View("Edit",i);
 [SucAuthorize(SucAccess.CompanionPost)]
 [Authorize(Roles=TravelService.Traveler),HttpPost] public async Task<IActionResult> Stop(CommandInput i){await S.StopNeed(i);return Done("/Companions/Mine");}
 [SucAuthorize(SucAccess.CompanionConnection)]
 [Authorize(Roles=TravelService.Traveler),HttpPost] public async Task<IActionResult> Connect(CommandInput i){await S.Connect(i);return Done("/Companions/Connections","Đã gửi đề nghị. Kết nối cần được người nhận chấp nhận.");}
 [SucAuthorize(SucAccess.CompanionConnection)]
 [Authorize(Roles=TravelService.Traveler)] public async Task<IActionResult> Connections(){ViewBag.Names=await S.DisplayNames();var list=await S.Connections();var owners=new Dictionary<long,long>();var destinations=new Dictionary<long,string>();foreach(var c in list){owners[c.MaDeNghi]=await S.ConnectionOwner(c.MaDeNghi);destinations[c.MaDeNghi]=await S.NeedDestination(c.MaDeNghi);}ViewBag.Owners=owners;ViewBag.Destinations=destinations;return View(list);}
 [SucAuthorize(SucAccess.CompanionConnection)]
 [Authorize(Roles=TravelService.Traveler),HttpPost] public async Task<IActionResult> Respond(CommandInput i){await S.ConnectionAction(i);return Done("/Companions/Connections");}
 [SucAuthorize(SucAccess.CompanionAgreement)]
 [Authorize(Roles=TravelService.Traveler)] public async Task<IActionResult> Agreement(long id){ViewBag.Connection=await S.Connection(id);ViewBag.Destination=await S.NeedDestination(id);try{ViewBag.Form=await S.AgreementForm(id);}catch(BusinessException){}return View(await S.Agreements(id));}
 [SucAuthorize(SucAccess.CompanionAgreement),Authorize(Roles=TravelService.Traveler),HttpGet] public async Task<IActionResult> Propose(long id)=>View(await S.AgreementForm(id));
 [SucAuthorize(SucAccess.CompanionAgreement)]
 [Authorize(Roles=TravelService.Traveler),HttpPost] public async Task<IActionResult> Propose(AgreementInput i){await S.ProposeAgreement(i);return Done("/Companions/Agreement/"+i.ConnectionId);}
 [SucAuthorize(SucAccess.CompanionAgreement)]
 [Authorize(Roles=TravelService.Traveler),HttpPost] public async Task<IActionResult> AgreementAction(CommandInput i,long connectionId){await S.AgreementAction(i);return Done("/Companions/Agreement/"+connectionId);}
}
