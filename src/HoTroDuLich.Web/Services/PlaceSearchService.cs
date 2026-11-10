using System.Globalization;
using System.Text;
using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

public static class PlaceText
{
 public static readonly Dictionary<string,string> Categories=new(){{"","Tất cả"},{"AnUong","Quán ăn & cà phê"},{"LuuTru","Khách sạn & chỗ ở"},{"PhuongTien","Thuê xe"},{"HoatDong","Tham quan & trải nghiệm"},{"Parking","Bãi đỗ xe"},{"Restroom","Nhà vệ sinh"},{"Fuel","Trạm xăng"}};
 public static string State(string state)=>state switch{"Draft"=>"Bản nháp","Pending"=>"Chờ duyệt","Approved"=>"Đã duyệt","Rejected"=>"Từ chối","ChangesRequested"=>"Cần bổ sung","Hidden"=>"Tạm ẩn",_=>state};
 public static string Known(bool? value)=>value==true?"Có":value==false?"Không":"Chưa công bố";
 public static string Mode(string mode)=>mode switch{"drive"=>"Ô tô","bike"=>"Xe đạp",_=>"Đi bộ"};
 public static string CategoryFromLegacy(string? type)=>type=="DiaDiem"?"HoatDong":Categories.ContainsKey(type??"")?type??"":"";
 public static string Normalize(string? value)=>new((value??"").Normalize(NormalizationForm.FormD).Where(c=>CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark).Select(c=>c is 'đ' or 'Đ'?'d':char.ToLowerInvariant(c)).ToArray());
 public static string GoogleUrl(string name,string? id,MapPosition? pos=null)
 {
  if(id!=null)return "https://www.google.com/maps/search/?api=1&query="+Uri.EscapeDataString(name)+"&query_place_id="+Uri.EscapeDataString(id);
  return "https://www.google.com/maps/search/?api=1&query="+Uri.EscapeDataString(pos!=null?pos.Lat.ToString(CultureInfo.InvariantCulture)+","+pos.Lng.ToString(CultureInfo.InvariantCulture):name);
 }
 public static string Around(string kind,long? id=null,string? googleId=null,string? category=null,MapPosition? position=null,string? name=null,long? planId=null)=>
  QueryHelpers.AddQueryString("/kham-pha",new Dictionary<string,string?>{{"AnchorKind",kind},{"AnchorId",id?.ToString(CultureInfo.InvariantCulture)},
   {"GooglePlaceId",googleId},{"Category",category},{"Lat",position?.Lat.ToString(CultureInfo.InvariantCulture)},
   {"Lng",position?.Lng.ToString(CultureInfo.InvariantCulture)},{"AnchorName",name},{"PlanId",planId?.ToString(CultureInfo.InvariantCulture)}});
 public static string SearchUrl(PlaceSearchFilter f,int page=1)=>QueryHelpers.AddQueryString("/kham-pha",new Dictionary<string,string?>{{"Q",f.Q},{"Category",f.Category},{"AnchorKind",f.AnchorKind},{"AnchorId",f.AnchorId?.ToString(CultureInfo.InvariantCulture)},{"GooglePlaceId",f.GooglePlaceId},{"AnchorName",f.AnchorName},{"Lat",f.Lat?.ToString(CultureInfo.InvariantCulture)},{"Lng",f.Lng?.ToString(CultureInfo.InvariantCulture)},{"RadiusKm",f.RadiusKm.ToString(CultureInfo.InvariantCulture)},{"Mode",f.Mode},{"Parking",f.Parking.ToString()},{"VegetarianOptions",f.VegetarianOptions.ToString()},{"VeganOnly",f.VeganOnly.ToString()},{"AccessibleEntrance",f.AccessibleEntrance.ToString()},{"IncludeGoogle",f.IncludeGoogle.ToString()},{"Sort",f.Sort},{"Page",page.ToString(CultureInfo.InvariantCulture)},{"PlanId",f.PlanId?.ToString(CultureInfo.InvariantCulture)}});
}

public sealed class PlaceSearchService(AppDbContext db,BranchService branches,TravelService travel,GoogleMapsClient google,OpenMapPlaces openPlaces)
{
 public static double Distance(MapPosition a,MapPosition b)
 {
  var lat1=(double)a.Lat*Math.PI/180;var lat2=(double)b.Lat*Math.PI/180;
  var dlat=lat2-lat1;var dlng=(double)(b.Lng-a.Lng)*Math.PI/180;
  var h=Math.Pow(Math.Sin(dlat/2),2)+Math.Cos(lat1)*Math.Cos(lat2)*Math.Pow(Math.Sin(dlng/2),2);
  return 6371*2*Math.Asin(Math.Sqrt(Math.Clamp(h,0,1)));
 }
 public async Task<MapAnchor?> Anchor(PlaceSearchFilter f,CancellationToken ct)
 {
  switch(f.AnchorKind)
  {
   case null or "":return null;
   case "Google":
    if(!google.MapsReady)throw new BusinessException("Mốc liên kết Google Maps cần được chọn lại trên bản đồ đang sử dụng.",503);
    var g=await google.Place(f.GooglePlaceId??"",ct);return new("Google",null,g.Id,g.Name,g.Address,g.Position,g.Attributions);
   case "Landmark":
    var l=await branches.PublicLandmarks().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==f.AnchorId,ct)??throw new BusinessException("Mốc địa điểm không còn được công bố.",404);
    var lp=await branches.Position(l.Latitude,l.Longitude,l.LocationSource,l.LocationExpiresAt,l.GooglePlaceId,ct)??throw new BusinessException("Mốc chưa có vị trí sử dụng được.",422);
    return new("Landmark",l.Id,l.GooglePlaceId,l.Name,l.Address,lp);
   case "Branch":
    var b=await branches.PublicBranches().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==f.AnchorId,ct)??throw new BusinessException("Cơ sở làm mốc không còn được công bố.",404);
    var bp=await branches.Position(b.Latitude,b.Longitude,b.LocationSource,b.LocationExpiresAt,b.GooglePlaceId,ct)??throw new BusinessException("Cơ sở chưa có vị trí sử dụng được.",422);
    return new("Branch",b.Id,b.GooglePlaceId,b.Name,b.Address,bp);
   case "Pin" or "OpenStreetMap":
    if(f.Lat==null||f.Lng==null)throw new BusinessException("Chọn vị trí mốc trên bản đồ hoặc dùng vị trí của bạn.",400);
    GoogleMapsClient.CheckPosition(f.Lat.Value,f.Lng.Value);
    return new(f.AnchorKind,null,null,string.IsNullOrWhiteSpace(f.AnchorName)?"Ghim đã chọn":f.AnchorName.Trim(),
     f.AnchorKind=="OpenStreetMap"?"OpenStreetMap":"Mốc do bạn chọn",new(f.Lat.Value,f.Lng.Value));
   default:throw new BusinessException("Loại mốc tìm kiếm không hợp lệ.",400);
  }
 }
 public async Task<List<PlaceSuggestion>> LocalSuggestions(string q,CancellationToken ct)
 {
  q=PlaceText.Normalize(q.Trim());if(q.Length<2)return [];if(q.Length>200)throw new BusinessException("Tên địa điểm tối đa 200 ký tự.",400);
  var l=await branches.PublicLandmarks().AsNoTracking().Where(x=>EF.Functions.Collate(x.Name.Replace("đ","d").Replace("Đ","D"),"Latin1_General_100_CI_AI").Contains(q)||EF.Functions.Collate(x.Address.Replace("đ","d").Replace("Đ","D"),"Latin1_General_100_CI_AI").Contains(q)).OrderBy(x=>x.Name).Take(5).Select(x=>new PlaceSuggestion("",x.Name,x.Address,x.Type=="Entrance"?"Cửa vào":"Mốc địa điểm",x.Id)).ToListAsync(ct);
  var b=await branches.PublicBranches().AsNoTracking().Where(x=>EF.Functions.Collate(x.Name.Replace("đ","d").Replace("Đ","D"),"Latin1_General_100_CI_AI").Contains(q)||EF.Functions.Collate(x.Address.Replace("đ","d").Replace("Đ","D"),"Latin1_General_100_CI_AI").Contains(q)).OrderBy(x=>x.Name).Take(5).Select(x=>new PlaceSuggestion("",x.Name,x.Address,"Cơ sở dịch vụ",x.Id)).ToListAsync(ct);
  return l.Concat(b).Take(8).ToList();
 }
 public async Task<PlaceSearchPage> Search(PlaceSearchFilter f,CancellationToken ct)
 {
  if(!PlaceText.Categories.ContainsKey(f.Category)||f.RadiusKm is <0.1m or >50||f.Page is <1 or >1000||f.Mode is not ("walk" or "drive" or "bike")||f.Sort is not ("distance" or "name")||(f.Q?.Length??0)>200||(f.AnchorName?.Length??0)>200)throw new BusinessException("Điều kiện tìm kiếm không hợp lệ.",400);
  f.Q=string.IsNullOrWhiteSpace(f.Q)?null:f.Q.Trim();
  if(f.Category==""&&f.Q!=null){var text=PlaceText.Normalize(f.Q);f.Category=text switch{"quan an" or "an uong" or "nha hang" or "ca phe"=>"AnUong","khach san" or "cho o" or "luu tru"=>"LuuTru","thue xe"=>"PhuongTien",_=>""};if(f.Category!="")f.Q=null;}
  MapAnchor? anchor=null;
  var notices=new List<string>();
  try{anchor=await Anchor(f,ct);}
  catch(BusinessException e) when((f.AnchorKind is "Google" or "Landmark" or "Branch")&&(e.Status is 422 or 503))
  {
   notices.Add(e.Message+" Hãy chọn mốc khác hoặc đặt ghim trên bản đồ.");
   f.AnchorKind=null;f.AnchorId=null;f.GooglePlaceId=null;f.AnchorName=null;f.Lat=null;f.Lng=null;
  }
  if(anchor!=null){f.AnchorName=anchor.Name;f.GooglePlaceId=anchor.GooglePlaceId;f.AnchorId=anchor.Id;f.Lat=anchor.Position.Lat;f.Lng=anchor.Position.Lng;}
  var source=branches.PublicBranches().AsNoTracking();
  var baseSource=source;
  if(anchor?.Kind=="Branch")source=source.Where(x=>x.Id!=anchor.Id);
  if(anchor?.GooglePlaceId!=null)source=source.Where(x=>x.GooglePlaceId==null||x.GooglePlaceId!=anchor.GooglePlaceId);
  var visibleServices=travel.PublicServices();
  if(f.Category!="")source=source.Where(x=>x.Type==f.Category||db.Set<DichVuCoSo>().Any(l=>l.BranchId==x.Id&&visibleServices.Any(s=>s.MaDichVu==l.ServiceId&&s.LoaiDichVu==f.Category)));
  if(f.Q!=null)
  {
   var q=PlaceText.Normalize(f.Q);
   source=source.Where(x=>EF.Functions.Collate(x.Name.Replace("đ","d").Replace("Đ","D"),"Latin1_General_100_CI_AI").Contains(q)
    ||EF.Functions.Collate(x.Address.Replace("đ","d").Replace("Đ","D"),"Latin1_General_100_CI_AI").Contains(q)
    ||EF.Functions.Collate(x.Region.Replace("đ","d").Replace("Đ","D"),"Latin1_General_100_CI_AI").Contains(q)
    ||(x.Summary!=null&&EF.Functions.Collate(x.Summary.Replace("đ","d").Replace("Đ","D"),"Latin1_General_100_CI_AI").Contains(q))
    ||db.Set<DichVuCoSo>().Any(link=>link.BranchId==x.Id&&visibleServices.Any(s=>s.MaDichVu==link.ServiceId&&EF.Functions.Collate(s.TenDichVu.Replace("đ","d").Replace("Đ","D"),"Latin1_General_100_CI_AI").Contains(q))));
  }
  if(f.Parking)source=source.Where(x=>x.Parking==true);
  if(f.VegetarianOptions)source=source.Where(x=>x.VegetarianOptions==true);
  if(f.VeganOnly)source=source.Where(x=>x.VeganOnly==true);
  if(f.AccessibleEntrance)source=source.Where(x=>x.AccessibleEntrance==true);
  var rows=await source.OrderBy(x=>x.Name).Take(501).ToListAsync(ct);
  if(rows.Count>500){rows=rows.Take(500).ToList();notices.Add("Có nhiều cơ sở phù hợp. Hãy thêm tên hoặc chọn một mốc gần nơi bạn quan tâm để thu hẹp kết quả.");}
  var ids=rows.Select(x=>x.Id).ToArray();
  var serviceLinks=await db.Set<DichVuCoSo>().AsNoTracking().Where(x=>ids.Contains(x.BranchId)).ToListAsync(ct);
  var serviceIds=serviceLinks.Select(x=>x.ServiceId).Distinct().ToArray();
  var services=await visibleServices.AsNoTracking().Where(x=>serviceIds.Contains(x.MaDichVu)).ToDictionaryAsync(x=>x.MaDichVu,ct);
  var hits=new List<PlaceHit>();var missing=0;
  // Bound paid location refreshes after an expired coordinate cache; do not crawl the whole catalog.
  var refreshes=0;
  foreach(var b in rows)
  {
   MapPosition? pos=null;
   try
   {
    var needsRefresh=b.LocationSource=="Google"&&(b.LocationExpiresAt<=DateTime.UtcNow||b.Latitude==null);
    if(!needsRefresh||refreshes++<10)pos=await branches.Position(b.Latitude,b.Longitude,b.LocationSource,b.LocationExpiresAt,b.GooglePlaceId,ct);
   }
   catch(BusinessException){ }
   if(pos==null)missing++;
   var distance=anchor!=null&&pos!=null?Distance(anchor.Position,pos):(double?)null;
   if(anchor!=null&&(distance==null||distance>(double)f.RadiusKm))continue;
   var offers=serviceLinks.Where(x=>x.BranchId==b.Id&&services.ContainsKey(x.ServiceId)).Select(x=>services[x.ServiceId]).OrderBy(x=>x.TenDichVu).ToList();
   hits.Add(new("b:"+b.Id,b.Name,b.Address,f.Category!=""?f.Category:b.Type,pos,distance,b,offers,null,"/co-so/"+b.Id));
  }
  var strict=f.Parking||f.VegetarianOptions||f.VeganOnly||f.AccessibleEntrance;
  if(anchor!=null&&f.IncludeGoogle&&strict)
   notices.Add("Khi lọc theo tiện ích, kết quả chỉ gồm cơ sở đã công bố thông tin phù hợp.");
  else if(anchor!=null&&f.IncludeGoogle&&google.MapsReady&&google.PlacesReady)
  {
   try
   {
    var found=await google.Nearby(anchor.Position,f.RadiusKm,f.Category,f.Q,ct);
    var googleIds=found.Select(x=>x.Id).ToArray();
    var owned=await baseSource.Where(x=>x.GooglePlaceId!=null&&googleIds.Contains(x.GooglePlaceId)).Select(x=>x.GooglePlaceId).ToListAsync(ct);
    foreach(var g in found.Where(g=>!owned.Contains(g.Id)&&g.Id!=anchor.GooglePlaceId))hits.Add(new("g:"+g.Id,g.Name,g.Address,f.Category,g.Position,Distance(anchor.Position,g.Position),null,[],g,g.MapsUrl));
    notices.Add("Địa điểm Google là các gợi ý được trả về trong lần tìm này; thông tin dịch vụ trên website được ghi riêng theo từng cơ sở.");
   }
   catch(BusinessException){notices.Add("Chưa lấy được địa điểm Google. Các cơ sở đã công bố trên website vẫn được hiển thị.");}
  }
  else if(anchor!=null&&f.IncludeGoogle&&!google.MapsReady)
  {
   try
   {
    var found=await openPlaces.Nearby(anchor.Position,f.RadiusKm,f.Category,f.Q,ct);
    var anchorName=PlaceText.Normalize(anchor.Name);
    foreach(var place in found)
    {
     var distance=Distance(anchor.Position,place.Position);
     if(distance<0.025&&PlaceText.Normalize(place.Name)==anchorName)continue;
     hits.Add(new("osm:"+place.Id,place.Name,place.Address,place.Category,place.Position,distance,null,[],null,place.Url));
    }
    if(f.RadiusKm>OpenMapPlaces.MaxRadiusKm)
     notices.Add($"Gợi ý OpenStreetMap được tra trong {OpenMapPlaces.MaxRadiusKm} km. Các cơ sở đã công bố vẫn dùng phạm vi bạn chọn.");
   }
   catch(BusinessException){notices.Add("Chưa tải được địa điểm OpenStreetMap quanh mốc. Các cơ sở đã công bố vẫn hiển thị.");}
  }
  if(missing>0)notices.Add(anchor==null?"Một số cơ sở đang cần xác nhận lại vị trí bản đồ.":"Cơ sở chưa có vị trí sử dụng được chưa được tính vào kết quả quanh mốc.");
  var ordered=f.Sort=="distance"&&anchor!=null?hits.OrderBy(x=>x.DistanceKm).ThenBy(x=>x.Name).ToList():hits.OrderBy(x=>x.Name).ToList();
  var landmarkQuery=branches.PublicLandmarks().AsNoTracking();
  if(f.Q!=null)
  {
   var q=PlaceText.Normalize(f.Q);
   landmarkQuery=landmarkQuery.Where(x=>EF.Functions.Collate(x.Name.Replace("đ","d").Replace("Đ","D"),"Latin1_General_100_CI_AI").Contains(q)
    ||EF.Functions.Collate(x.Region.Replace("đ","d").Replace("Đ","D"),"Latin1_General_100_CI_AI").Contains(q));
  }
  var landmarks=await landmarkQuery.OrderBy(x=>x.Name).Take(8).ToListAsync(ct);
  return new(f,anchor,ordered.Skip((f.Page-1)*20).Take(20).ToList(),landmarks,ordered.Count,missing,notices,google.MapsReady,google.PlacesReady);
 }
}
