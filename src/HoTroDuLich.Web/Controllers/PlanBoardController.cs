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
 /// <summary>Goi y dia diem truc tiep tu Goong theo nhom dich vu va diem den, kem toa do (khong luu vao co so du lieu).</summary>
 /// <summary>Ghi nhanh một sự cố vào chuyến đi từ khung hỗ trợ khi đang đi.</summary>
 [HttpPost("/api/lapkehoach/su-co")] public async Task<IActionResult> Incident([FromBody]Models.IncidentInput i)=>await Api(async()=>{await travel.CreateIncident(i);return new{planId=i.PlanId};});
 /// <summary>Nơi giúp theo loại quanh vị trí hiện tại của khách (bệnh viện, nhà thuốc, ATM...).</summary>
 [HttpGet("/api/lapkehoach/gan-day"),EnableRateLimiting("discovery")] public async Task<IActionResult> Around(string? kind,double lat,double lng,CancellationToken ct)=>await Api(()=>goong.Around(kind,lat,lng,ct));
 [HttpGet("/api/lapkehoach/goong-goi-y"),EnableRateLimiting("discovery")] public async Task<IActionResult> GoongSuggest(string? group,string? kind,string? destination,string? q,CancellationToken ct)=>await Api(()=>goong.Nearby(group,destination,q,ct,kind));
 /// <summary>Goi y dia chi/dia diem de chon diem xuat phat va ket thuc (khong gioi han theo nhom dich vu).</summary>
 [HttpGet("/api/lapkehoach/diem-goi-y"),EnableRateLimiting("discovery")] public async Task<IActionResult> PointSuggest(string? q,string? near,CancellationToken ct)=>await Api(async()=>{var term=(q??"").Trim();var area=(near??"").Trim();if(area.Length>0&&area.Length<=80&&term.IndexOf(area,StringComparison.OrdinalIgnoreCase)<0)term+=" "+area;return (object)await goong.Autocomplete(term,ct);});
 /// <summary>Toa do cua mot dia diem da chon trong danh sach goi y (chi trong khung Viet Nam).</summary>
 [HttpGet("/api/lapkehoach/diem-chi-tiet"),EnableRateLimiting("discovery")] public async Task<IActionResult> PointDetail(string? placeId,CancellationToken ct)
 {
  var poi=await goong.Detail(placeId??"",ct);
  if(poi==null||poi.Lat<5.0||poi.Lat>24.0||poi.Lng<101.5||poi.Lng>118.5)return StatusCode(422,new{message="Không lấy được vị trí này. Hãy chọn điểm khác."});
  return Json(poi);
 }
 /// <summary>Khoang cach va duong di trong mot ngay cua chuyen di (xuat phat, cac hang muc co toa do, ket thuc).</summary>
 [HttpGet("/api/lapkehoach/{id:long}/tuyen"),EnableRateLimiting("discovery")] public async Task<IActionResult> DayRoute(long id,int day,string? mode,CancellationToken ct)=>await Api(()=>board.Route(id,day,mode,ct));
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
