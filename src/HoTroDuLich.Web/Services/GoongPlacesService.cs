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

 static readonly Dictionary<string,string> KindKeywords=new(StringComparer.OrdinalIgnoreCase){["HoatDong"]="khu du lịch",["AnUong"]="nhà hàng",["LuuTru"]="khách sạn",["PhuongTien"]="bến xe",["Tour"]="công ty du lịch",["GiaiTri"]="khu vui chơi",["SucKhoe"]="spa",["MuaSam"]="chợ",["DichVuHoTro"]="dịch vụ",["CaPhe"]="quán cà phê",["QuanChay"]="quán chay",["ChoAmThuc"]="chợ",["NhaNghi"]="nhà nghỉ",["CamTrai"]="khu cắm trại",["BenXe"]="bến xe",["ThueXe"]="thuê xe máy",["SanBay"]="sân bay",["GaTau"]="ga tàu",["BenTau"]="bến tàu",["TramXang"]="trạm xăng",["SuaXe"]="sửa xe",["BaiDoXe"]="bãi đỗ xe",["BenhVien"]="bệnh viện",["PhongKham"]="phòng khám",["NhaThuoc"]="nhà thuốc",["CongAn"]="công an",["CuuHo"]="cứu hộ",["Atm"]="ATM",["DoiTien"]="đổi tiền",["BuuDien"]="bưu điện",["SieuThi"]="siêu thị",["TrungTamTM"]="trung tâm thương mại",["GiatUi"]="giặt ủi",["NhaVeSinh"]="nhà vệ sinh công cộng",["SimWifi"]="cửa hàng sim",["GuiHanhLy"]="gửi hành lý",["QuaLuuNiem"]="quà lưu niệm"};
 static string? Keyword(string? group,string? kind=null)=>!string.IsNullOrWhiteSpace(kind)?(KindKeywords.TryGetValue(kind,out var k)?k:ServiceKinds.Get(kind)?.Label.ToLowerInvariant()):group switch{"Stay"=>"khách sạn","Eat"=>"nhà hàng","Place"=>"khu du lịch","Move"=>"bến xe","Experience"=>"trải nghiệm","Health"=>"bệnh viện","Utility"=>"siêu thị",_=>null};
 /// <summary>Chi tiet mot dia diem (toa do, dia chi), luu dem 24 gio.</summary>
 public async Task<GoongPoi?> Detail(string placeId,CancellationToken ct)
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
 public async Task<List<GoongPoi>> Nearby(string? group,string? destination,string? q,CancellationToken ct,string? kind=null)
 {
  if(!Enabled)return [];
  var term=(q??"").Trim();var kw=Keyword(group,kind);var dest=(destination??"").Trim();if(dest.Length>80)dest=dest[..80];
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

 /// <summary>Dia diem theo loai quanh mot toa do (trong 10 km), xep theo khoang cach; dung khi khach can giup ngay.</summary>
 public async Task<List<GoongPoi>> Around(string? kind,double lat,double lng,CancellationToken ct)
 {
  if(!Enabled||lat is < 5.0 or > 24.0||lng is < 101.5 or > 118.5)return [];
  var kw=Keyword(null,kind);if(kw==null)return [];
  var cacheKey=$"goong.ar.{kw}.{lat:0.00},{lng:0.00}";
  if(cache.TryGetValue(cacheKey,out List<GoongPoi>? hit)&&hit!=null)return hit;
  var loc=lat.ToString("0.#####",System.Globalization.CultureInfo.InvariantCulture)+","+lng.ToString("0.#####",System.Globalization.CultureInfo.InvariantCulture);
  using var doc=await Get("v2/place/autocomplete?input="+Uri.EscapeDataString(kw)+"&location="+loc+"&radius=10",ct);
  var ids=new List<string>();
  if(doc!=null&&doc.RootElement.TryGetProperty("predictions",out var arr)&&arr.ValueKind==JsonValueKind.Array)
   foreach(var p in arr.EnumerateArray().Take(8))if(p.TryGetProperty("place_id",out var pid)&&pid.ValueKind==JsonValueKind.String)ids.Add(pid.GetString()!);
  var details=await Task.WhenAll(ids.Select(i=>Detail(i,ct)));
  var list=details.Where(d=>d!=null&&Km(lat,lng,d.Lat,d.Lng)<=10).Select(d=>d!).GroupBy(d=>d.PlaceId).Select(g=>g.First()).OrderBy(d=>Km(lat,lng,d.Lat,d.Lng)).ToList();
  if(doc!=null)cache.Set(cacheKey,list,TimeSpan.FromMinutes(15));
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

 /// <summary>Mot doan duong giua hai diem: km, phut va duong ve (toa do lat,lng). Estimated=true khi Goong khong tra loi, khi do chi la uoc tinh theo duong chim bay.</summary>
 public sealed record Leg(double Km,double Minutes,List<double[]> Line,bool Estimated);
 public static double Haversine(double la1,double lo1,double la2,double lo2)
 {
  const double R=6371,T=Math.PI/180;var x=(la2-la1)*T;var y=(lo2-lo1)*T;
  var h=Math.Sin(x/2)*Math.Sin(x/2)+Math.Cos(la1*T)*Math.Cos(la2*T)*Math.Sin(y/2)*Math.Sin(y/2);
  return 2*R*Math.Asin(Math.Sqrt(h));
 }
 static readonly Dictionary<string,double> Speed=new(){["car"]=35,["bike"]=30,["foot"]=4.5};
 static List<double[]> DecodePolyline(string s)
 {
  var res=new List<double[]>();int i=0,lat=0,lng=0;
  while(i<s.Length)
  {
   int b,shift=0,r=0;do{b=s[i++]-63;r|=(b&31)<<shift;shift+=5;}while(b>=32&&i<s.Length);lat+=(r&1)!=0?~(r>>1):r>>1;
   shift=0;r=0;do{b=s[i++]-63;r|=(b&31)<<shift;shift+=5;}while(b>=32&&i<s.Length);lng+=(r&1)!=0?~(r>>1):r>>1;
   res.Add([lat/1e5,lng/1e5]);
  }
  return res;
 }
 /// <summary>Tinh doan duong bang Goong Direction (khoa REST). vehicle: car, bike hoac foot (foot luon la uoc tinh). Luu dem 7 ngay; moi doan moi la mot luot goi Goong.</summary>
 public async Task<Leg> Route(double la1,double lo1,double la2,double lo2,string vehicle,bool allowCall,CancellationToken ct)
 {
  vehicle=vehicle is "car" or "bike" or "foot"?vehicle:"car";
  var ci=System.Globalization.CultureInfo.InvariantCulture;
  string P(double d)=>d.ToString("0.0000",ci);
  var cacheKey=$"goong.dir.{vehicle}.{P(la1)},{P(lo1)}>{P(la2)},{P(lo2)}";
  if(cache.TryGetValue(cacheKey,out Leg? hit)&&hit!=null)return hit;
  if(Enabled&&allowCall&&vehicle!="foot"&&Haversine(la1,lo1,la2,lo2)>0.05)
  {
   using var doc=await Get($"Direction?origin={la1.ToString("0.######",ci)},{lo1.ToString("0.######",ci)}&destination={la2.ToString("0.######",ci)},{lo2.ToString("0.######",ci)}&vehicle={vehicle}",ct);
   if(doc!=null&&doc.RootElement.TryGetProperty("routes",out var rs)&&rs.ValueKind==JsonValueKind.Array&&rs.GetArrayLength()>0)
   {
    var r=rs[0];double m=0,sec=0;
    if(r.TryGetProperty("legs",out var legs)&&legs.ValueKind==JsonValueKind.Array)
     foreach(var l in legs.EnumerateArray()){if(l.TryGetProperty("distance",out var d)&&d.TryGetProperty("value",out var dv)&&dv.TryGetDouble(out var dm))m+=dm;if(l.TryGetProperty("duration",out var u)&&u.TryGetProperty("value",out var uv)&&uv.TryGetDouble(out var us))sec+=us;}
    var line=r.TryGetProperty("overview_polyline",out var op)&&op.TryGetProperty("points",out var pts)&&pts.ValueKind==JsonValueKind.String?DecodePolyline(pts.GetString()!):[[la1,lo1],[la2,lo2]];
    if(m>0){var leg=new Leg(Math.Round(m/1000,1),Math.Max(1,Math.Round(sec/60)),line,false);cache.Set(cacheKey,leg,TimeSpan.FromDays(7));return leg;}
   }
  }
  var km=Haversine(la1,lo1,la2,lo2)*1.35;
  var est=new Leg(Math.Round(km,1),Math.Max(1,Math.Round(km/Speed[vehicle]*60)),[[la1,lo1],[la2,lo2]],true);
  if(!allowCall||!Enabled||vehicle=="foot")return est;
  cache.Set(cacheKey,est,TimeSpan.FromMinutes(10));
  return est;
 }
}
