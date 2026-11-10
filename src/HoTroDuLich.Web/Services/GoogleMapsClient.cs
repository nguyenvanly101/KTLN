using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using HoTroDuLich.Web.Models;
using Microsoft.Extensions.Caching.Memory;

namespace HoTroDuLich.Web.Services;

public sealed class GoogleMapsClient(IHttpClientFactory clients,IConfiguration config,IMemoryCache cache,ILogger<GoogleMapsClient> logger)
{
 public bool MapsReady => !string.IsNullOrWhiteSpace(config["GoogleMaps:BrowserApiKey"]);
 public bool PlacesReady => !string.IsNullOrWhiteSpace(config["GoogleMaps:ServerApiKey"]);
 public string BrowserKey => config["GoogleMaps:BrowserApiKey"] ?? "";
 public string MapId => string.IsNullOrWhiteSpace(config["GoogleMaps:MapId"])?"DEMO_MAP_ID":config["GoogleMaps:MapId"]!;
 public static bool ValidId(string? id) => id is {Length:>=1 and <=256} && Regex.IsMatch(id,@"^[A-Za-z0-9_\-]+$",RegexOptions.CultureInvariant);
 public static void CheckPosition(decimal lat,decimal lng)
 { if(lat is < -90 or >90 || lng is < -180 or >180)throw new BusinessException("Vị trí bản đồ không hợp lệ.",400); }

 async Task<JsonDocument> Send(string path,object? body,string fields,CancellationToken ct)
 {
  if(!PlacesReady)throw new BusinessException("Tìm địa điểm Google chưa được cấu hình. Bạn có thể chọn mốc đã công bố hoặc đặt ghim.",503);
  using var request=new HttpRequestMessage(body==null?HttpMethod.Get:HttpMethod.Post,path);
  request.Headers.Add("X-Goog-Api-Key",config["GoogleMaps:ServerApiKey"]);
  if(fields.Length>0)request.Headers.Add("X-Goog-FieldMask",fields);
  if(body!=null)request.Content=JsonContent.Create(body);
  try
  {
   using var response=await clients.CreateClient("GoogleMaps").SendAsync(request,ct);
   if(!response.IsSuccessStatusCode){logger.LogWarning("Google Maps request failed with status {Status}",(int)response.StatusCode);throw new BusinessException("Google Maps chưa trả được thông tin. Vui lòng thử lại hoặc chọn một mốc khác.",503);}
   var bytes=await response.Content.ReadAsByteArrayAsync(ct);
   if(bytes.Length>2_000_000)throw new BusinessException("Phản hồi bản đồ vượt giới hạn.",503);
   return JsonDocument.Parse(bytes);
  }
  catch(OperationCanceledException) when(!ct.IsCancellationRequested){throw new BusinessException("Google Maps phản hồi chậm. Vui lòng thử lại.",503);}
  catch(HttpRequestException){throw new BusinessException("Chưa kết nối được Google Maps. Vui lòng thử lại.",503);}
  catch(JsonException){throw new BusinessException("Thông tin bản đồ chưa đọc được. Vui lòng thử lại.",503);}
 }

 public async Task<List<PlaceSuggestion>> Autocomplete(string input,string? session,CancellationToken ct)
 {
  input=input.Trim();if(input.Length<2)return [];
  if(input.Length>200)throw new BusinessException("Tên địa điểm tối đa 200 ký tự.",400);
  var body=new Dictionary<string,object>{{"input",input},{"languageCode","vi"},{"includedRegionCodes",new[]{"vn"}}};
  if(Guid.TryParse(session,out var token))body["sessionToken"]=token.ToString();
  using var doc=await Send("https://places.googleapis.com/v1/places:autocomplete",body,"suggestions.placePrediction.placeId,suggestions.placePrediction.structuredFormat,suggestions.placePrediction.types",ct);
  if(!doc.RootElement.TryGetProperty("suggestions",out var rows))return [];
  var result=new List<PlaceSuggestion>();
  foreach(var row in rows.EnumerateArray())if(row.TryGetProperty("placePrediction",out var p)&&p.TryGetProperty("placeId",out var id)&&p.TryGetProperty("structuredFormat",out var text))
  {
   var types=p.TryGetProperty("types",out var typeArray)?typeArray.EnumerateArray().Select(x=>x.GetString()??"").ToArray():[];
   // A city/province is not the exact landmark or business the traveler is selecting here.
   if(types.Any(t=>t is "locality" or "country" or "postal_code"||t.StartsWith("administrative_area_",StringComparison.Ordinal))&&!types.Any(t=>t is "point_of_interest" or "establishment" or "premise" or "street_address"))continue;
   result.Add(new(id.GetString()!,Text(text,"mainText","text"),Text(text,"secondaryText","text"),"Google"));
  }
  return result.Take(5).ToList();
 }

 public async Task<GooglePlace> Place(string id,CancellationToken ct,string? session=null)
 {
  if(!ValidId(id))throw new BusinessException("Liên kết địa điểm Google không hợp lệ.",400);
  // Names/addresses are returned live, not stored as a permanent copy of Google's directory.
  var uri="https://places.googleapis.com/v1/places/"+Uri.EscapeDataString(id)+"?languageCode=vi";
  if(Guid.TryParse(session,out var token))uri+="&sessionToken="+token;
  using var doc=await Send(uri,null,"id,displayName,formattedAddress,location,googleMapsUri,businessStatus,attributions",ct);
  var result=ReadPlace(doc.RootElement);
  if(result==null)throw new BusinessException("Địa điểm chưa có vị trí có thể sử dụng.",422);
  cache.Set("Google.Position.v1."+id,result.Position,TimeSpan.FromMinutes(15));return result;
 }

 public async Task<MapPosition> Position(string id,CancellationToken ct)
 {
  if(!ValidId(id))throw new BusinessException("Liên kết địa điểm Google không hợp lệ.",400);
  if(cache.TryGetValue<MapPosition>("Google.Position.v1."+id,out var pos)&&pos!=null)return pos;
  return (await Place(id,ct)).Position;
 }

 public async Task<List<GooglePlace>> Nearby(MapPosition center,decimal radius,string category,string? q,CancellationToken ct)
 {
  CheckPosition(center.Lat,center.Lng);
  if(radius is <0.1m or >50)throw new BusinessException("Phạm vi tìm quanh từ 0,1 đến 50 km.",400);
  var types=category switch {"AnUong"=>new[]{"restaurant","cafe","bakery"},"LuuTru"=>["lodging"],"PhuongTien"=>["car_rental"],"HoatDong"=>["tourist_attraction"],"Parking"=>["parking"],"Restroom"=>["public_bathroom"],"Fuel"=>["gas_station"],_=>new[]{"restaurant","cafe","lodging","car_rental","tourist_attraction","parking"}};
  object body;string uri;
  var circle=new {center=new{latitude=center.Lat,longitude=center.Lng},radius=(double)radius*1000};
  if(!string.IsNullOrWhiteSpace(q))
  {
   var data=new Dictionary<string,object>{{"textQuery",q.Trim()},{"languageCode","vi"},{"maxResultCount",20},{"locationBias",new{circle}},{"rankPreference","DISTANCE"}};
   if(category!="")data["includedType"]=types[0];
   body=data;uri="https://places.googleapis.com/v1/places:searchText";
  }
  else{body=new{includedTypes=types,maxResultCount=20,languageCode="vi",rankPreference="DISTANCE",locationRestriction=new{circle}};uri="https://places.googleapis.com/v1/places:searchNearby";}
  using var doc=await Send(uri,body,"places.id,places.displayName,places.formattedAddress,places.location,places.googleMapsUri,places.businessStatus,places.attributions",ct);
  if(!doc.RootElement.TryGetProperty("places",out var rows))return [];
  return rows.EnumerateArray().Select(ReadPlace).OfType<GooglePlace>().Where(p=>p.BusinessStatus!="CLOSED_PERMANENTLY"&&PlaceSearchService.Distance(center,p.Position)<=(double)radius).ToList();
 }

 public async Task<GoogleRoute> Route(MapPosition origin,MapPosition destination,string mode,CancellationToken ct)
 {
  CheckPosition(origin.Lat,origin.Lng);CheckPosition(destination.Lat,destination.Lng);
  var travelMode=mode switch{"walk"=>"WALK","drive"=>"DRIVE","bike"=>"BICYCLE",_=>throw new BusinessException("Cách di chuyển không hợp lệ.",400)};
  var body=new{origin=new{location=new{latLng=new{latitude=origin.Lat,longitude=origin.Lng}}},destination=new{location=new{latLng=new{latitude=destination.Lat,longitude=destination.Lng}}},travelMode,languageCode="vi",units="METRIC"};
  using var doc=await Send("https://routes.googleapis.com/directions/v2:computeRoutes",body,"routes.distanceMeters,routes.duration,routes.polyline.encodedPolyline,routes.warnings",ct);
  if(!doc.RootElement.TryGetProperty("routes",out var routes)||routes.GetArrayLength()==0)throw new BusinessException("Chưa tìm được đường đi với phương thức này.",422);
  var r=routes[0];
  if(!r.TryGetProperty("distanceMeters",out var distance)||!r.TryGetProperty("duration",out var duration)||!r.TryGetProperty("polyline",out var poly)||!poly.TryGetProperty("encodedPolyline",out var encoded))throw new BusinessException("Đường đi chưa có đủ thông tin.",503);
  if(!double.TryParse((duration.GetString()??"").TrimEnd('s'),NumberStyles.Float,CultureInfo.InvariantCulture,out var seconds)||!double.IsFinite(seconds)||seconds<0)throw new BusinessException("Thời gian đường đi không hợp lệ.",503);
  return new(distance.GetInt32(),seconds,encoded.GetString()??"",r.TryGetProperty("warnings",out var warnings)?warnings.EnumerateArray().Select(x=>x.GetString()??"").ToList():[]);
 }

 static GooglePlace? ReadPlace(JsonElement p)
 {
  if(!p.TryGetProperty("id",out var id)||!p.TryGetProperty("location",out var location)||!location.TryGetProperty("latitude",out var lat)||!location.TryGetProperty("longitude",out var lng))return null;
  if(!lat.TryGetDecimal(out var latitude)||!lng.TryGetDecimal(out var longitude)||latitude is <-90 or >90||longitude is <-180 or >180)return null;
  var attrs=new List<PlaceAttribution>();
  if(p.TryGetProperty("attributions",out var attributions))foreach(var a in attributions.EnumerateArray())attrs.Add(new(Text(a,"provider"),SafeUrl(Text(a,"providerUri"))));
  return new(id.GetString()!,Text(p,"displayName","text"),Text(p,"formattedAddress"),new(latitude,longitude),SafeUrl(Text(p,"googleMapsUri"))??("https://www.google.com/maps/search/?api=1&query=Google&query_place_id="+Uri.EscapeDataString(id.GetString()!)),p.TryGetProperty("businessStatus",out var status)?status.GetString():null,attrs);
 }
 static string Text(JsonElement p,params string[] path){foreach(var key in path){if(!p.TryGetProperty(key,out var next))return "";p=next;}return p.ValueKind==JsonValueKind.String?p.GetString()??"":"";}
 public static string? SafeUrl(string? url)=>Uri.TryCreate(url,UriKind.Absolute,out var uri)&&uri.Scheme=="https"&&string.IsNullOrEmpty(uri.UserInfo)?uri.AbsoluteUri:null;
}
