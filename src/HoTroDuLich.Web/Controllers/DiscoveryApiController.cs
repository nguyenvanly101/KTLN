using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;

[ApiController,Route("api/discovery")]
public sealed class DiscoveryApiController(DiscoveryService discovery,DiscoveryRouter router,GoongPlacesService goong):ControllerBase
{
 static object Card(DiscoveryCard c)=>new{id=c.Entry.Id,title=c.Entry.Title,kind=c.Entry.Kind,kindLabel=DiscoveryText.Kind(c.Entry.Kind),region=c.Entry.Region,url=DiscoveryText.LinkTo(c.Entry),lat=c.Entry.Latitude,lng=c.Entry.Longitude,summary=c.Entry.Summary,price=c.Entry.PriceFrom,unit=c.Entry.PriceUnit,distanceKm=c.DistanceKm,extraMinutes=c.ExtraMinutes,checkedOn=c.Entry.CheckedOn};
 [HttpGet("suggest"),EnableRateLimiting("discovery")]public async Task<IActionResult> Suggest(string? q)=>Ok(await discovery.Suggest(q));
 [HttpGet("map"),EnableRateLimiting("discovery")]public async Task<IActionResult> Map([FromQuery]DiscoveryFilter f){try{var p=await discovery.Search(f,true);return Ok(new{items=p.Items.Where(x=>x.Entry.Latitude!=null).Select(Card),total=p.Total,limit=1500});}catch(BusinessException e){return StatusCode(e.Status,new{message=e.Message});}}
 [HttpPost("route"),EnableRateLimiting("maps")]public async Task<IActionResult> Route([FromBody]DiscoveryRouteInput i,CancellationToken cancellation){try{var r=await discovery.Route(i,router,cancellation);return Ok(new{r.DistanceKm,r.Minutes,r.Coordinates,r.Points,nearby=r.Nearby.Select(Card),r.Provider,r.Note});}catch(BusinessException e){return StatusCode(e.Status,new{message=e.Message});}}
 // Diem cho tuyen: goi y dia diem Goong (ca nuoc), lay toa do khi chon, doi toa do ra ten khi ghim tren ban do.
 [HttpGet("diem"),EnableRateLimiting("discovery")]public async Task<IActionResult> Point(string? q,CancellationToken ct){if(string.IsNullOrWhiteSpace(q)||q.Trim().Length<2)return Ok(new{hits=Array.Empty<object>(),limited=false});var (hits,ok)=await goong.AutocompleteChecked(q.Trim(),ct);return Ok(new{hits=hits.Take(6).Select(h=>new{name=h.Name,secondary=h.Secondary,placeId=h.PlaceId}),limited=!ok});}
 [HttpGet("diem-chi-tiet"),EnableRateLimiting("discovery")]public async Task<IActionResult> PointDetail(string? placeId,CancellationToken ct){var poi=string.IsNullOrWhiteSpace(placeId)?null:await goong.Detail(placeId,ct);return poi==null?NotFound(new{message="Chưa lấy được vị trí của địa điểm này. Hãy chọn trên bản đồ."}):Ok(new{name=poi.Name,address=poi.Address,lat=poi.Lat,lng=poi.Lng});}
 [HttpGet("ten-vi-tri"),EnableRateLimiting("discovery")]public async Task<IActionResult> PlaceName(double lat,double lng,CancellationToken ct){var g=await goong.Reverse(lat,lng,ct);var name=g==null?null:(!string.IsNullOrWhiteSpace(g.Name)?g.Name:string.Join(", ",new[]{g.Commune,g.Province}.Where(x=>!string.IsNullOrWhiteSpace(x))));return Ok(new{name=string.IsNullOrWhiteSpace(name)?null:name});}
}
