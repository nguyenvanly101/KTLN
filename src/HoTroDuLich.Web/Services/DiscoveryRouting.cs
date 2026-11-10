using System.Globalization;
using System.Text.Json;
using HoTroDuLich.Web.Models;
using Microsoft.Extensions.Caching.Memory;
namespace HoTroDuLich.Web.Services;

// One gate per host, not per user. Public demo limits apply to the whole app.
public sealed class DiscoveryRouter(IHttpClientFactory clients,IMemoryCache cache,IConfiguration config)
{
 static readonly SemaphoreSlim Gate=new(1,1);
 static DateTime LastRequest=DateTime.MinValue;
 public async Task<DiscoveryRouteResult> Compute(List<DiscoveryPoint> points,string mode,CancellationToken cancellation)
 {
  var endpoints=new Dictionary<string,string>{["car"]=config["Discovery:Routing:Car"]??"https://routing.openstreetmap.de/routed-car/",["bike"]=config["Discovery:Routing:Bike"]??"https://routing.openstreetmap.de/routed-bike/",["foot"]=config["Discovery:Routing:Foot"]??"https://routing.openstreetmap.de/routed-foot/"};
  if(!endpoints.TryGetValue(mode,out var endpoint)||!Uri.TryCreate(endpoint,UriKind.Absolute,out var server)||server.Scheme!="https")throw new BusinessException("Nhà cung cấp tuyến đường chưa được cấu hình hợp lệ.",503);
  var coords=string.Join(';',points.Select(p=>p.Lng.ToString("0.######",CultureInfo.InvariantCulture)+","+p.Lat.ToString("0.######",CultureInfo.InvariantCulture)));var key="discovery-route:"+endpoint+coords;
  if(cache.TryGetValue(key,out DiscoveryRouteResult? cached))return cached! with{Points=points};
  await Gate.WaitAsync(cancellation);try{
   if(cache.TryGetValue(key,out cached))return cached! with{Points=points};
   var pause=TimeSpan.FromMilliseconds(1100)-(DateTime.UtcNow-LastRequest);if(pause>TimeSpan.Zero)await Task.Delay(pause,cancellation);
   LastRequest=DateTime.UtcNow;
   using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancellation);timeout.CancelAfter(TimeSpan.FromSeconds(12));
   try{var client=clients.CreateClient("DiscoveryRouting");using var response=await client.GetAsync(new Uri(server,"route/v1/driving/"+coords+"?overview=full&geometries=geojson&steps=false&alternatives=false&radiuses="+string.Join(';',points.Select(_=>"1500"))),timeout.Token);response.EnsureSuccessStatusCode();using var doc=JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));var root=doc.RootElement;if(root.GetProperty("code").GetString()!="Ok"||root.GetProperty("routes").GetArrayLength()==0)throw new BusinessException("Chưa tìm được đường đi cho các vị trí này. Hãy chọn lại lối tiếp cận hoặc phương tiện.",422);
    var route=root.GetProperty("routes")[0];var geometry=route.GetProperty("geometry").GetProperty("coordinates").EnumerateArray().Select(p=>new[]{p[1].GetDouble(),p[0].GetDouble()}).ToList();if(geometry.Count<2||geometry.Any(p=>!double.IsFinite(p[0])||!double.IsFinite(p[1])||Math.Abs(p[0])>90||Math.Abs(p[1])>180))throw new JsonException();if(geometry.Count>20000){var stride=(int)Math.Ceiling(geometry.Count/20000d);geometry=geometry.Where((_,i)=>i%stride==0||i==geometry.Count-1).ToList();}
    var result=new DiscoveryRouteResult(route.GetProperty("distance").GetDouble()/1000,route.GetProperty("duration").GetDouble()/60,geometry,points,[],"OSRM / OpenStreetMap","Thời gian ước tính theo mạng đường; chưa gồm thời gian dừng, chờ và tình hình giao thông hiện tại.");cache.Set(key,result,TimeSpan.FromMinutes(30));return result;
   }catch(OperationCanceledException)when(!cancellation.IsCancellationRequested){throw new BusinessException("Dịch vụ tính tuyến phản hồi chậm. Bạn vẫn có thể xem danh sách và thử tính lại.",503);}catch(HttpRequestException){throw new BusinessException("Hiện chưa kết nối được dịch vụ tính tuyến. Bạn có thể tiếp tục tra cứu và thử lại sau.",503);}catch(Exception error)when(error is JsonException or InvalidOperationException or KeyNotFoundException or FormatException){throw new BusinessException("Dịch vụ tuyến đường trả dữ liệu chưa hợp lệ. Hãy thử lại sau.",503);}
  }finally{Gate.Release();}
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
