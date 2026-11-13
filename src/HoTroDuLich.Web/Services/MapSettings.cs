namespace HoTroDuLich.Web.Services;

/// <summary>Cau hinh nen ban do vector dung chung cho giao dien va chinh sach bao mat. Chon nha cung cap theo Discovery:Map:Provider
/// (Goong, TrackAsia, VietMap, hoac OSM de tat ban do vector); de trong thi lay nha cung cap dau tien co khoa theo thu tu Goong, TrackAsia, VietMap.</summary>
public sealed class MapSettings
{
 public string Provider { get; }
 /// <summary>Duong dan kieu (style) ban do vector da kem khoa; rong khi dung anh ban do cau hinh san.</summary>
 public string StyleUrl { get; }
 /// <summary>Cac may chu ban do vector can mo trong CSP (img-src, connect-src), cach nhau dau cach, co dau cach cuoi.</summary>
 public string Origins { get; }
 /// <summary>Muc phong to (Leaflet) nho nhat ma nha cung cap co o ban do; Goong chi co o tu muc 6, duoi do ban do vector trong.</summary>
 public int MinZoom { get; }
 /// <summary>Khung nhin toi da: nam, tay, bac, dong. Goong ve dung ten Hoang Sa, Truong Sa, Bien Dong (da kiem tra) nen mo rong ra dong de xem hai quan dao; cac nen khac giu trong dat lien va ven bo.</summary>
 public string Bounds { get; } = "7.5,101.5,24.0,110.5";

 public MapSettings(IConfiguration c)
 {
  string? Key(string name)=>string.IsNullOrWhiteSpace(c[name])?null:c[name]!.Trim();
  var goong=Key("Discovery:Goong:MapKey")??Key("Discovery:Map:GoongKey");
  var track=Key("Discovery:Map:TrackAsiaKey");var viet=Key("Discovery:Map:VietMapKey");
  var want=(c["Discovery:Map:Provider"]??"").Trim();
  string[] order=string.Equals(want,"OSM",StringComparison.OrdinalIgnoreCase)?[]
   :want.Length>0&&!string.Equals(want,"auto",StringComparison.OrdinalIgnoreCase)?[want,"Goong","TrackAsia","VietMap"]:["Goong","TrackAsia","VietMap"];
  foreach(var name in order)
  {
   if(name.Equals("Goong",StringComparison.OrdinalIgnoreCase)&&goong!=null){Provider="Goong";MinZoom=6;Bounds="5.0,101.5,24.0,118.5";StyleUrl="https://tiles.goong.io/assets/goong_map_web.json?api_key="+Uri.EscapeDataString(goong);Origins="https://tiles.goong.io ";return;}
   if(name.Equals("TrackAsia",StringComparison.OrdinalIgnoreCase)&&track!=null){Provider="TrackAsia";StyleUrl="https://maps.track-asia.com/styles/v2/streets.json?key="+Uri.EscapeDataString(track);Origins="https://maps.track-asia.com https://tiles.track-asia.com ";return;}
   if(name.Equals("VietMap",StringComparison.OrdinalIgnoreCase)&&viet!=null){Provider="VietMap";StyleUrl="https://maps.vietmap.vn/maps/styles/"+(c["Discovery:Map:VietMapStyle"]??"tm")+"/style.json?apikey="+Uri.EscapeDataString(viet);Origins="https://maps.vietmap.vn ";return;}
  }
  Provider="OSM";StyleUrl="";Origins="";
 }
}
