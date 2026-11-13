using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HoTroDuLich.Web.Controllers;

/// <summary>Lap ke hoach mot trang: /lapkehoach (tao moi) va /lapkehoach/{id} (sua). Luu ca chuyen di mot lan qua API.</summary>
[Authorize(Roles=TravelService.Traveler)]
public sealed class PlanBoardController(TravelService travel,PlanBoardService board,GoongPlacesService goong):AppController(travel)
{
 [HttpGet("/lapkehoach")] public IActionResult Create(string? diemDen)
 {
  var draft=new PlanDraft{Destination=diemDen??"",Start=TravelService.Today.AddDays(7)};
  return V("PlanBoard/Editor",new PlanEditor(draft,true,null,[]));
 }
 [HttpGet("/lapkehoach/{id:long}")] public async Task<IActionResult> Edit(long id)=>V("PlanBoard/Editor",await board.Load(id));

 [HttpGet("/lap-ke-hoach")] public IActionResult OldCreate()=>RedirectPermanent("/lapkehoach");
 [HttpGet("/ke-hoach/{id:long}/lap")] public IActionResult OldEdit(long id)=>RedirectPermanent("/lapkehoach/"+id);

 [HttpPost("/api/lapkehoach")] public async Task<IActionResult> Save([FromBody]PlanDraft draft)=>await Api(async()=>new{id=await board.Save(draft)});
 [HttpGet("/api/lapkehoach/goi-y"),EnableRateLimiting("discovery")] public async Task<IActionResult> Suggest([FromQuery]Models.DiscoveryFilter f,bool daLuu=false)=>await Api(()=>board.Explore(f,daLuu));
 [HttpGet("/api/lapkehoach/chi-tiet/{entryId:long}"),EnableRateLimiting("discovery")] public async Task<IActionResult> EntryDetail(long entryId)=>await Api(()=>board.EntryDetail(entryId));
 [HttpGet("/api/lapkehoach/diem-den"),EnableRateLimiting("discovery")] public async Task<IActionResult> Destinations(string? q)=>await Api(()=>board.DestinationSearch(q));
 [HttpGet("/api/lapkehoach/ban-do"),EnableRateLimiting("discovery")] public async Task<IActionResult> Map()=>await Api(()=>board.MapPoints());

 [HttpGet("/api/lapkehoach/dia-diem"),EnableRateLimiting("discovery")] public async Task<IActionResult> OtherPlaces(string? q,CancellationToken ct)=>await Api(()=>goong.Autocomplete(q,ct));
 /// <summary>Doi vi tri bam tren ban do thanh ten khu vuc (chi trong khung Viet Nam).</summary>
 [HttpGet("/api/lapkehoach/dia-chi"),EnableRateLimiting("discovery")] public async Task<IActionResult> AddressAt(double lat,double lng,CancellationToken ct)
 {
  if(lat<5.0||lat>24.0||lng<101.5||lng>118.5)return StatusCode(422,new{message="Vị trí nằm ngoài khu vực hỗ trợ."});
  return await Api(async()=>(object?)await goong.Reverse(lat,lng,ct)??new{});
 }

 [HttpPost("/Plans/Delete")] public async Task<IActionResult> Delete(long id){await board.DeletePlan(id);return Done("/Plans","Đã xóa chuyến đi.");}

 async Task<IActionResult> Api<T>(Func<Task<T>> run)
 {
  try{return Ok(await run());}
  catch(BusinessException e){return StatusCode(e.Status,new{message=e.Message});}
  catch(Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException){return StatusCode(409,new{message="Lịch vừa được người khác cập nhật. Hãy tải lại trang rồi sửa lại."});}
 }
}
