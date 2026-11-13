using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace HoTroDuLich.Web.Services;

public sealed record GoongHit(string Name,string Secondary,string Description,string PlaceId);
public sealed record GoongPoi(string Name,string Address,double Lat,double Lng,string PlaceId);
public sealed record GoongPlace(string Name,string Province,string Commune,string Address,double Lat,double Lng);

/// <summary>Tim dia diem va doi toa do thanh ten khu vuc bang Goong REST API. Khoa chi nam o may chu (Discovery:Goong:RestKey), trinh duyet goi qua cac API cua website.</summary>
public sealed class GoongPlacesService(HttpClient http,IConfiguration configuration,IMemoryCache cache,ILogger<GoongPlacesService> logger)
{
 string? Key=>configuration["Discovery:Goong:RestKey"];
 public bool Enabled=>!string.IsNullOrWhiteSpace(Key);

 async Task<JsonDocument?> Get(string path,CancellationToken ct)
 {
  try
  {
   using var response=await http.GetAsync(path+(path.Contains('?')?"&":"?")+"api_key="+Uri.EscapeDataString(Key!),ct);
   if(!response.IsSuccessStatusCode){logger.LogWarning("Goong tra ve {Status} cho {Path}",(int)response.StatusCode,path.Split('?')[0]);return null;}
   return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct),cancellationToken:ct);
  }
  catch(Exception e) when(e is HttpRequestException or TaskCanceledException or JsonException){logger.LogWarning("Khong goi duoc Goong: {Type}",e.GetType().Name);return null;}
 }

 public async Task<List<GoongHit>> Autocomplete(string? q,CancellationToken ct)
 {
  var term=(q??"").Trim();if(!Enabled||term.Length<2)return [];if(term.Length>100)term=term[..100];
  var cacheKey="goong.ac."+term.ToLowerInvariant();
  if(cache.TryGetValue(cacheKey,out List<GoongHit>? hit)&&hit!=null)return hit;
  using var doc=await Get("v2/place/autocomplete?input="+Uri.EscapeDataString(term),ct);
  var list=new List<GoongHit>();
  if(doc!=null&&doc.RootElement.TryGetProperty("predictions",out var arr)&&arr.ValueKind==JsonValueKind.Array)
   foreach(var p in arr.EnumerateArray().Take(10))
   {
    var sf=p.TryGetProperty("structured_formatting",out var s)?s:default;
    string Str(JsonElement e,string n)=>e.ValueKind==JsonValueKind.Object&&e.TryGetProperty(n,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString()??"":"";
    var name=Str(sf,"main_text");if(name.Length==0)name=Str(p,"description");
    list.Add(new(name,Str(sf,"secondary_text"),Str(p,"description"),Str(p,"place_id")));
   }
  if(doc!=null)cache.Set(cacheKey,list,TimeSpan.FromMinutes(30));
  return list;
 }

 static string? Keyword(string? group)=>group switch{"Stay"=>"khách sạn","Eat"=>"nhà hàng","Place"=>"khu du lịch","Move"=>"bến xe","Experience"=>"trải nghiệm",_=>null};
 /// <summary>Chi tiet mot dia diem (toa do, dia chi), luu dem 24 gio.</summary>
 async Task<GoongPoi?> Detail(string placeId,CancellationToken ct)
 {
  if(string.IsNullOrWhiteSpace(placeId)||placeId.Length>400)return null;
  var cacheKey="goong.dt."+placeId;
  if(cache.TryGetValue(cacheKey,out GoongPoi? hit))return hit;
  using var doc=await Get("v2/place/detail?place_id="+Uri.EscapeDataString(placeId),ct);
  if(doc==null||!doc.RootElement.TryGetProperty("result",out var r)||!r.TryGetProperty("geometry",out var g)||!g.TryGetProperty("location",out var loc))return null;
  string Str(JsonElement e,string n)=>e.ValueKind==JsonValueKind.Object&&e.TryGetProperty(n,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString()??"":"";
  if(!loc.TryGetProperty("lat",out var la)||!loc.TryGetProperty("lng",out var lo)||!la.TryGetDouble(out var lat)||!lo.TryGetDouble(out var lng))return null;
  var poi=new GoongPoi(Str(r,"name"),Str(r,"formatted_address"),lat,lng,placeId);
  cache.Set(cacheKey,poi,TimeSpan.FromHours(24));
  return poi;
 }
 /// <summary>Tam cua mot diem den (geocode), luu dem 24 gio; null neu khong tim duoc.</summary>
 async Task<(double Lat,double Lng)?> Center(string dest,CancellationToken ct)
 {
  var cacheKey="goong.ct."+dest.ToLowerInvariant();
  if(cache.TryGetValue(cacheKey,out (double,double)? hit))return hit;
  using var doc=await Get("v2/geocode?address="+Uri.EscapeDataString(dest),ct);
  (double,double)? center=null;
  if(doc!=null&&doc.RootElement.TryGetProperty("results",out var rs)&&rs.ValueKind==JsonValueKind.Array&&rs.GetArrayLength()>0&&rs[0].TryGetProperty("geometry",out var g)&&g.TryGetProperty("location",out var l)&&l.TryGetProperty("lat",out var la)&&l.TryGetProperty("lng",out var lo)&&la.TryGetDouble(out var lat)&&lo.TryGetDouble(out var lng))center=(lat,lng);
  if(doc!=null)cache.Set(cacheKey,center,TimeSpan.FromHours(24));
  return center;
 }
 static double Km(double la1,double lo1,double la2,double lo2)
 {
  const double r=6371;var dLa=(la2-la1)*Math.PI/180;var dLo=(lo2-lo1)*Math.PI/180;
  var a=Math.Sin(dLa/2)*Math.Sin(dLa/2)+Math.Cos(la1*Math.PI/180)*Math.Cos(la2*Math.PI/180)*Math.Sin(dLo/2)*Math.Sin(dLo/2);
  return 2*r*Math.Asin(Math.Sqrt(a));
 }
 /// <summary>Goi y dia diem truc tiep tu Goong cho nhom dich vu va diem den, kem toa do de ve ghim. Khong luu vao co so du lieu; chi luu dem trong bo nho.</summary>
 public async Task<List<GoongPoi>> Nearby(string? group,string? destination,string? q,CancellationToken ct)
 {
  if(!Enabled)return [];
  var term=(q??"").Trim();var kw=Keyword(group);var dest=(destination??"").Trim();if(dest.Length>80)dest=dest[..80];
  var text=term.Length>=2?(dest.Length>0&&!term.Contains(dest,StringComparison.OrdinalIgnoreCase)?term+" "+dest:term):kw==null?"":(kw+(dest.Length>0?" "+dest:""));
  if(text.Length<2)return [];
  var cacheKey="goong.nb."+text.ToLowerInvariant();
  if(cache.TryGetValue(cacheKey,out List<GoongPoi>? hit)&&hit!=null)return hit;
  var hits=(await Autocomplete(text,ct)).Where(h=>h.PlaceId.Length>0).Take(8).ToList();
  var details=await Task.WhenAll(hits.Select(h=>Detail(h.PlaceId,ct)));
  var all=details.Where(d=>d!=null&&d.Lat is >=5.0 and <=24.0&&d.Lng is >=101.5 and <=118.5).Select(d=>d!).GroupBy(d=>d.PlaceId).Select(g=>g.First()).ToList();
  // chi giu dia diem gan diem den (tru khi khong xac dinh duoc tam), xep theo khoang cach
  var center=dest.Length>0?await Center(dest,ct):null;
  var list=center==null?all:all.Where(d=>Km(center.Value.Lat,center.Value.Lng,d.Lat,d.Lng)<=30).OrderBy(d=>Km(center.Value.Lat,center.Value.Lng,d.Lat,d.Lng)).ToList();
  cache.Set(cacheKey,list,TimeSpan.FromMinutes(30));
  return list;
 }

 /// <summary>Doi toa do (trong khung Viet Nam) thanh ten khu vuc gan nhat.</summary>
 public async Task<GoongPlace?> Reverse(double lat,double lng,CancellationToken ct)
 {
  if(!Enabled)return null;
  var cacheKey=$"goong.rv.{lat:0.000},{lng:0.000}";
  if(cache.TryGetValue(cacheKey,out GoongPlace? hit))return hit;
  using var doc=await Get($"v2/geocode?latlng={lat.ToString("0.######",System.Globalization.CultureInfo.InvariantCulture)},{lng.ToString("0.######",System.Globalization.CultureInfo.InvariantCulture)}",ct);
  if(doc==null||!doc.RootElement.TryGetProperty("results",out var results)||results.ValueKind!=JsonValueKind.Array||results.GetArrayLength()==0)return null;
  var r=results[0];
  string Str(JsonElement e,string n)=>e.ValueKind==JsonValueKind.Object&&e.TryGetProperty(n,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString()??"":"";
  var compound=r.TryGetProperty("compound",out var c)?c:default;
  var place=new GoongPlace(Str(r,"name"),Str(compound,"province"),Str(compound,"commune"),Str(r,"formatted_address"),lat,lng);
  cache.Set(cacheKey,place,TimeSpan.FromHours(6));
  return place;
 }
}
