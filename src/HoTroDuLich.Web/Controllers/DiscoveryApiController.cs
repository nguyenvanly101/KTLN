using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;

[ApiController,Route("api/discovery")]
public sealed class DiscoveryApiController(DiscoveryService discovery,DiscoveryRouter router):ControllerBase
{
 static object Card(DiscoveryCard c)=>new{id=c.Entry.Id,title=c.Entry.Title,kind=c.Entry.Kind,kindLabel=DiscoveryText.Kind(c.Entry.Kind),region=c.Entry.Region,url=DiscoveryText.Link(c.Entry.Id),lat=c.Entry.Latitude,lng=c.Entry.Longitude,summary=c.Entry.Summary,price=c.Entry.PriceFrom,unit=c.Entry.PriceUnit,distanceKm=c.DistanceKm,extraMinutes=c.ExtraMinutes,checkedOn=c.Entry.CheckedOn};
 [HttpGet("suggest"),EnableRateLimiting("discovery")]public async Task<IActionResult> Suggest(string? q)=>Ok(await discovery.Suggest(q));
 [HttpGet("map"),EnableRateLimiting("discovery")]public async Task<IActionResult> Map([FromQuery]DiscoveryFilter f){try{var p=await discovery.Search(f,true);return Ok(new{items=p.Items.Where(x=>x.Entry.Latitude!=null).Select(Card),total=p.Total,limit=300});}catch(BusinessException e){return StatusCode(e.Status,new{message=e.Message});}}
 [HttpPost("route"),EnableRateLimiting("maps")]public async Task<IActionResult> Route([FromBody]DiscoveryRouteInput i,CancellationToken cancellation){try{var r=await discovery.Route(i,router,cancellation);return Ok(new{r.DistanceKm,r.Minutes,r.Coordinates,r.Points,nearby=r.Nearby.Select(Card),r.Provider,r.Note});}catch(BusinessException e){return StatusCode(e.Status,new{message=e.Message});}}
}
