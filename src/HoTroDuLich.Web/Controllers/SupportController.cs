using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HoTroDuLich.Web.Controllers;
[Authorize(Roles=TravelService.Traveler)]
[SucAuthorize(SucAccess.Incident)]
public class SupportController(TravelService s):AppController(s){
 public async Task<IActionResult> Index(long? planId){ViewBag.Plans=await S.Plans();if(planId==null)return View(new List<HoTroDuLich.Web.Domain.TinhHuongPhatSinh>());ViewBag.Plan=await S.OwnPlan(planId.Value);return View(await S.Incidents(planId.Value));}
 [HttpGet] public async Task<IActionResult> Create(long planId){ViewBag.Plan=await S.OwnPlan(planId);ViewBag.Activities=await S.Activities(planId);return View(new IncidentInput{PlanId=planId});}
 [HttpPost] public async Task<IActionResult> Create(IncidentInput i){await S.CreateIncident(i);return Done("/Support?planId="+i.PlanId);}
 public async Task<IActionResult> Detail(long id){ViewBag.Options=await S.IncidentOptions(id);return View(await S.Find<HoTroDuLich.Web.Domain.TinhHuongPhatSinh>(id));}
 [HttpPost] public async Task<IActionResult> Action(CommandInput i){await S.IncidentAction(i);return Done("/Support/Detail/"+i.Id);}
}
