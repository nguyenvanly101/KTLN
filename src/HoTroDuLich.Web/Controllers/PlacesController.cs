using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Controllers;

[AllowAnonymous]
public sealed class PlacesController(TravelService travel,BranchService branches,PlaceSearchService search):AppController(travel)
{
 [HttpGet("/kham-pha"),EnableRateLimiting("maps-search")]
 public async Task<IActionResult> Index([FromQuery]PlaceSearchFilter filter,CancellationToken ct)
 {
  if(!ModelState.IsValid)throw new BusinessException("Điều kiện tìm kiếm không hợp lệ.",400);
  await LoadTripContext(filter.PlanId);return V("Places/Index",await search.Search(filter,ct));
 }
 [HttpGet("/co-so/{id:long}")]
 public async Task<IActionResult> Branch(long id,long? planId,CancellationToken ct)
 {
  await LoadTripContext(planId);
  return V("Places/Branch",await branches.Detail(id,ct));
 }
 [HttpGet("/dia-diem/{id:long}")]
 public async Task<IActionResult> Landmark(long id,long? planId,CancellationToken ct)
 {
  await LoadTripContext(planId);
  var landmark=await branches.PublicLandmarks().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new BusinessException("Địa điểm không còn được công bố.",404);
  ViewBag.Children=await branches.PublicLandmarks().AsNoTracking().Where(x=>x.ParentId==id).OrderBy(x=>x.Name).ToListAsync(ct);
  ViewBag.Parent=landmark.ParentId!=null?await branches.PublicLandmarks().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==landmark.ParentId,ct):null;
  try{ViewBag.Position=await branches.Position(landmark.Latitude,landmark.Longitude,landmark.LocationSource,landmark.LocationExpiresAt,landmark.GooglePlaceId,ct);}catch(BusinessException){ViewData["MapWarning"]="Vị trí Google đang cần cập nhật.";}
  return V("Places/Landmark",landmark);
 }
 [HttpGet("/kham-pha/so-sanh")]
 public async Task<IActionResult> Compare([FromQuery]long[] ids,long? planId,CancellationToken ct)
 {
  if(ids.Length is <1 or >3||ids.Distinct().Count()!=ids.Length)throw new BusinessException("Chọn từ một đến ba cơ sở khác nhau để so sánh.",400);
  await LoadTripContext(planId);
  var rows=new List<BranchPublicPage>();foreach(var id in ids)rows.Add(await branches.Detail(id,ct));
  return V("Places/Compare",rows);
 }
}

public sealed record MapRouteInput(decimal OriginLat,decimal OriginLng,decimal DestinationLat,decimal DestinationLng,string Mode);

[AllowAnonymous,Route("api/maps"),ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class MapsApiController(GoogleMapsClient maps,PlaceSearchService search,OpenMapPlaces openPlaces,DiscoveryRouter router):Controller
{
 [HttpGet("config")]
 public IActionResult Config()=>Json(new{browserKey=maps.BrowserKey,mapId=maps.MapId,mapsReady=maps.MapsReady,placesReady=maps.PlacesReady,
  mapProvider=maps.MapsReady?"Google Maps":"OpenStreetMap"});
 [HttpGet("suggest"),EnableRateLimiting("maps-lookup")]
 public async Task<IActionResult> Suggest(string? q=null,string? session=null,bool googleOnly=false,bool mapOnly=false,CancellationToken ct=default)
 {
  q=q?.Trim()??"";
  if(q.Length>200)return BadRequest(new{message="Tên địa điểm tối đa 200 ký tự."});
  var local=googleOnly||mapOnly?[]:await search.LocalSuggestions(q,ct);string? notice=null;
  try
  {
   if(googleOnly||maps.MapsReady)
   {
    if(maps.PlacesReady)local.AddRange(await maps.Autocomplete(q,session,ct));
    else notice="Gợi ý Google chưa sẵn sàng. Bạn có thể chọn mốc đã công bố hoặc đặt ghim.";
   }
   else local.AddRange(await openPlaces.Suggest(q,ct));
  }
  catch(BusinessException e){notice=e.Message;}
  return Json(new{items=local,notice});
 }
 [HttpGet("place"),EnableRateLimiting("maps-lookup")]
 public async Task<IActionResult> Place(string id,string? session,CancellationToken ct)
 {try{return Json(await maps.Place(id,ct,session));}catch(BusinessException e){return StatusCode(e.Status,new{message=e.Message});}}
 [HttpPost("route"),EnableRateLimiting("maps")]
 public async Task<IActionResult> Route([FromBody]MapRouteInput? input,CancellationToken ct)
 {
  if(input==null||!ModelState.IsValid)return BadRequest(new{message="Vị trí đường đi không hợp lệ."});
  var origin=new MapPosition(input.OriginLat,input.OriginLng);
  var destination=new MapPosition(input.DestinationLat,input.DestinationLng);
  try
  {
   if(maps.MapsReady)return Json(await maps.Route(origin,destination,input.Mode,ct));
   GoogleMapsClient.CheckPosition(origin.Lat,origin.Lng);
   GoogleMapsClient.CheckPosition(destination.Lat,destination.Lng);
   var mode=input.Mode switch{"walk"=>"foot","drive"=>"car","bike"=>"bike",_=>throw new BusinessException("Phương thức di chuyển không hợp lệ.",400)};
   if(PlaceSearchService.Distance(origin,destination)>50)throw new BusinessException("Chọn điểm đến cách mốc tối đa 50 km để xem đường.",400);
   var result=await router.Compute([new((double)origin.Lat,(double)origin.Lng),new((double)destination.Lat,(double)destination.Lng)],mode,ct);
   return Json(new{distanceMeters=(int)Math.Round(result.DistanceKm*1000),durationSeconds=result.Minutes*60,
    coordinates=result.Coordinates,warnings=new[]{result.Note},provider=result.Provider});
  }
  catch(BusinessException e){return StatusCode(e.Status,new{message=e.Message});}
 }
}
