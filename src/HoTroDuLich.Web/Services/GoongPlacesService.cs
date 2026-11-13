using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace HoTroDuLich.Web.Services;

public sealed record GoongHit(string Name,string Secondary,string Description,string PlaceId);
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
   foreach(var p in arr.EnumerateArray().Take(8))
   {
    var sf=p.TryGetProperty("structured_formatting",out var s)?s:default;
    string Str(JsonElement e,string n)=>e.ValueKind==JsonValueKind.Object&&e.TryGetProperty(n,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString()??"":"";
    var name=Str(sf,"main_text");if(name.Length==0)name=Str(p,"description");
    list.Add(new(name,Str(sf,"secondary_text"),Str(p,"description"),Str(p,"place_id")));
   }
  if(doc!=null)cache.Set(cacheKey,list,TimeSpan.FromMinutes(30));
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
