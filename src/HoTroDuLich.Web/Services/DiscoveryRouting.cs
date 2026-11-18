using System.Globalization;
using System.Text.Json;
using HoTroDuLich.Web.Models;
using Microsoft.Extensions.Caching.Memory;
namespace HoTroDuLich.Web.Services;

// Tinh tuyen bang Goong Direction (ban do Viet Nam, cung khoa REST cua website). Khi Goong khong tra duoc thi uoc luong theo duong thang,
// van ve tuyen va tim cho ghe doc duong, kem ghi chu ro la uoc luong. Di bo: dung hinh tuyen xe may, thoi gian theo toc do di bo.
public sealed class DiscoveryRouter(GoongPlacesService goong)
{
 static readonly Dictionary<string,double> Speed=new(){["car"]=35,["bike"]=30,["foot"]=4.5};
 public async Task<DiscoveryRouteResult> Compute(List<DiscoveryPoint> points,string mode,CancellationToken cancellation)
 {
  if(!Speed.ContainsKey(mode))mode="car";
  var pts=points.Select(p=>(p.Lat,p.Lng)).ToList();
  var leg=await goong.RouteMany(pts,mode=="car"?"car":"bike",cancellation);
  if(leg!=null)
  {
   var minutes=mode=="foot"?leg.Km/Speed["foot"]*60:leg.Minutes;
   return new DiscoveryRouteResult(leg.Km,minutes,leg.Line,points,[],"Goong",mode=="foot"?"Đường đi theo Goong; thời gian đi bộ tính theo khoảng 4,5 km/giờ.":"Thời gian theo Goong; chưa gồm thời gian dừng, chờ và tình hình giao thông lúc đi.");
  }
  // Goong khong tra duoc (het han muc, mat mang): uoc luong theo duong thang qua cac diem.
  double km=0;for(var n=1;n<pts.Count;n++)km+=GoongPlacesService.Haversine(pts[n-1].Lat,pts[n-1].Lng,pts[n].Lat,pts[n].Lng)*1.35;
  return new DiscoveryRouteResult(km,km/Speed[mode]*60,pts.Select(p=>new[]{p.Lat,p.Lng}).ToList(),points,[],"Ước lượng","Chưa lấy được đường đi từ Goong nên tuyến là đường nối thẳng các điểm, quãng đường và thời gian chỉ là ước lượng.");
 }
}

public sealed partial class DiscoveryService
{
 public async Task<DiscoveryRouteResult> Route(DiscoveryRouteInput i,DiscoveryRouter router,CancellationToken cancellation)
 {
  Validate(i);ValidatePoints(i.Points,i.Mode);Need(i.CorridorKm is >=.1 and <=20&&(i.MaxDetourMinutes==null||i.MaxDetourMinutes is >=0 and <=120),"Phạm vi hoặc thời gian đi thêm không hợp lệ.",400);
  var route=await router.Compute(i.Points,i.Mode,cancellation);var f=i.Filter;f.Page=1;f.Lat=null;f.Lng=null;f.South=Math.Max(-90,route.Coordinates.Min(p=>p[0])-i.CorridorKm/110);f.North=Math.Min(90,route.Coordinates.Max(p=>p[0])+i.CorridorKm/110);var offset=i.CorridorKm/(111*Math.Max(.1,Math.Cos(route.Coordinates.Average(p=>p[0])*Math.PI/180)));f.West=Math.Max(-180,route.Coordinates.Min(p=>p[1])-offset);f.East=Math.Min(180,route.Coordinates.Max(p=>p[1])+offset);
  var found=await Search(f,true);var candidates=found.Items.Where(c=>c.Entry.Latitude!=null).Select(c=>(card:c,distance:CorridorDistance((double)c.Entry.Latitude!.Value,(double)c.Entry.Longitude!.Value,route.Coordinates))).Where(x=>x.distance<=i.CorridorKm).OrderBy(x=>x.distance).Take(5).ToList();var items=new List<DiscoveryCard>();
  foreach(var c in candidates){cancellation.ThrowIfCancellationRequested();var point=new DiscoveryPoint((double)c.card.Entry.Latitude!.Value,(double)c.card.Entry.Longitude!.Value);DiscoveryRouteResult? best=null;
   // Preserve the supplied stop order; try the closest leg, never reorder fixed stops.
   var leg=Enumerable.Range(0,i.Points.Count-1).OrderBy(n=>DiscoveryText.Distance(i.Points[n].Lat,i.Points[n].Lng,point.Lat,point.Lng)+DiscoveryText.Distance(point.Lat,point.Lng,i.Points[n+1].Lat,i.Points[n+1].Lng)-DiscoveryText.Distance(i.Points[n].Lat,i.Points[n].Lng,i.Points[n+1].Lat,i.Points[n+1].Lng)).First();var points=i.Points.ToList();points.Insert(leg+1,point);
   try{if(points.Count<=8)best=await router.Compute(points,i.Mode,cancellation);}catch(BusinessException){/* a candidate may not have a reachable road entrance */}
   var extra=best==null?(double?)null:Math.Max(0,best.Minutes-route.Minutes);if(i.MaxDetourMinutes!=null&&(extra==null||extra>i.MaxDetourMinutes))continue;items.Add(c.card with{DistanceKm=c.distance,ExtraMinutes=extra});
  }
  return route with{Nearby=items.OrderBy(x=>x.ExtraMinutes??double.MaxValue).ToList(),Note=route.Note+" Tìm tối đa 5 lựa chọn gần hình tuyến; thời gian đi thêm chỉ có khi tính được đường ghé qua. Giữ nguyên thứ tự các điểm đã chọn."};
 }
 static double CorridorDistance(double lat,double lng,List<double[]> route){var best=double.MaxValue;var cos=Math.Cos(lat*Math.PI/180);for(var n=0;n<route.Count-1;n++){var a=route[n];var b=route[n+1];var ax=(a[1]-lng)*cos;var ay=a[0]-lat;var bx=(b[1]-lng)*cos;var by=b[0]-lat;var dx=bx-ax;var dy=by-ay;var t=(dx*dx+dy*dy)==0?0:Math.Clamp(-(ax*dx+ay*dy)/(dx*dx+dy*dy),0,1);best=Math.Min(best,Math.Sqrt(Math.Pow(ax+t*dx,2)+Math.Pow(ay+t*dy,2))*111.195);}return best;}
}
