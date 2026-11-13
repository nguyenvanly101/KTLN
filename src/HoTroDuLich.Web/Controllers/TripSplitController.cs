using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoTroDuLich.Web.Controllers;

[Authorize(Roles=TravelService.Traveler)]
public sealed class TripSplitController(TravelService travel,TripSplitService split):AppController(travel)
{
 [HttpGet("/Plans/Split/{id:long}")] public async Task<IActionResult> Index(long id)=>V("TripSplit/Index",await split.Page(id));
 [HttpPost("/Plans/Split/{id:long}")] public async Task<IActionResult> Add(long id,GroupExpenseInput input){input.PlanId=id;await split.Add(input);return Done("/Plans/Split/"+id,"Đã ghi khoản chi và chia cho những người được chọn.");}
 [HttpPost("/Plans/Split/{id:long}/xoa")] public async Task<IActionResult> Delete(long id,long expenseId){await split.Delete(id,expenseId);return Done("/Plans/Split/"+id,"Đã xóa khoản chi.");}
}
