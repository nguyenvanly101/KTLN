using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace HoTroDuLich.Web.Services;

public sealed record DestinationSection(string Key, string Title, string Icon, int Total, List<DiscoveryCard> Items, string Tab);
public sealed record DestinationArea(DonViItem Unit, int Count);
public sealed record DestinationMove(string Province, int Units);
public sealed record MapPoint(long Id, string Title, string Category, string Kind, string Url, decimal Lat, decimal Lng);
public sealed record DestinationTab(string Key, string Label, int Count);
public sealed record DestinationPage(DonViHanhChinh Unit, List<DonViHanhChinh> Path, List<DestinationArea> Areas, List<DestinationSection> Sections, List<DiscoveryEntry> Guides, List<KhuDuLich> Zones, List<DestinationMove> NewProvinces, int Total,
    string Tab, List<DestinationTab> Tabs, List<DiscoveryCard> Items, int ItemsTotal, List<MapPoint> Points, int WithoutCoords, string? Q, long? AreaId, string? AreaName, string[] Chips);
public sealed record DestinationIndexItem(DonViItem Province, int Count);
public sealed record HomeDestination(long UnitId, string Name, string Summary, int Count);

/// <summary>Trang chu tra cuu va trang diem den: gom thong tin da cong bo theo cay dia gioi, chia tab theo nhu cau cua khach.</summary>
public sealed partial class DiscoveryService
{
 public static readonly (string Key,string Label)[] DestinationTabs=[("tong-quan","Tổng quan"),("tham-quan","Tham quan"),("an-uong","Ăn uống"),("luu-tru","Lưu trú"),("di-chuyen","Di chuyển"),("ban-do","Bản đồ")];
 static readonly Dictionary<string,string[]> QuickChips=new(){["tham-quan"]=["Biển","Núi","Bảo tàng","Chợ","Chùa"],["an-uong"]=["Cà phê","Chay","Hải sản","Mì Quảng"],["luu-tru"]=["Khách sạn","Homestay","Resort"],["di-chuyen"]=["Sân bay","Ga","Thuê xe"]};
 /// <summary>Sap xep tren cot hep (chi lay ma) roi moi tai noi dung, de SQL Server khong phai xin cap bo nho lon cho phep sap xep cac dong co Body dai.</summary>
 async Task<List<DiscoveryEntry>> Narrow(IQueryable<long> orderedIds)
 {
  var ids=await orderedIds.ToListAsync();if(ids.Count==0)return [];
  var order=new Dictionary<long,int>();for(var n=0;n<ids.Count;n++)order[ids[n]]=n;
  var list=await db.Set<DiscoveryEntry>().AsNoTracking().Where(e=>orderedIds.Contains(e.Id)).ToListAsync();
  return list.Where(e=>order.ContainsKey(e.Id)).OrderBy(e=>order[e.Id]).ToList();
 }
 IQueryable<DiscoveryEntry> InArea(long unitId){var sub=geo.SubtreeQuery(unitId);return Public().AsNoTracking().Where(e=>e.DonViId!=null&&sub.Contains(e.DonViId.Value));}
 IQueryable<DiscoveryEntry> OfServiceType(IQueryable<DiscoveryEntry> q,string type)
 {
  var legacy=DanhMuc.Ten(type);
  return q.Where(e=>e.Kind=="Service"&&((e.ServiceId==null&&e.Tags.Contains(legacy))||(e.ServiceId!=null&&db.DichVuDuLich.Any(s=>s.MaDichVu==e.ServiceId&&(s.LoaiDichVu==type||s.LoaiDichVu==legacy)))));
 }
 IQueryable<DiscoveryEntry> TabQuery(IQueryable<DiscoveryEntry> area,string tab)=>tab switch
 {
  "tham-quan"=>area.Where(e=>e.Kind=="Place"||e.Kind=="Area").Union(OfServiceType(area,"HoatDong")),
  "an-uong"=>OfServiceType(area,"AnUong").Union(area.Where(e=>e.Kind=="Guide"&&e.Tags.Contains("Ăn uống"))),
  "luu-tru"=>OfServiceType(area,"LuuTru").Union(area.Where(e=>e.Kind=="Guide"&&e.Tags.Contains("Lưu trú"))),
  "di-chuyen"=>area.Where(e=>e.Kind=="Transport").Union(OfServiceType(area,"PhuongTien")),
  _=>area
 };
 /// <summary>Nhom nhu cau cua mot muc, dung de to mau diem tren ban do.</summary>
 static string Category(DiscoveryEntry e)=>e.Kind switch
 {
  "Place" or "Area"=>"tham-quan",
  "Transport"=>"di-chuyen",
  "Service"=>e.Tags.Contains("Ăn uống")?"an-uong":e.Tags.Contains("Lưu trú")?"luu-tru":e.Tags.Contains("Phương tiện")?"di-chuyen":"tham-quan",
  "Guide"=>e.Tags.Contains("Ăn uống")?"an-uong":e.Tags.Contains("Lưu trú")?"luu-tru":"khac",
  _=>"khac"
 };

 /// <summary>Cac diem den noi bat cho trang chu: muc "Diem den" da cong bo, kem so muc thong tin trong khu vuc.</summary>
 public async Task<List<HomeDestination>> HomePage()
 {
  var entries=await Narrow(Public().AsNoTracking().Where(e=>e.Kind=="Destination"&&e.DonViId!=null).OrderBy(e=>e.Title).Select(e=>e.Id));
  var list=new List<HomeDestination>();
  foreach(var e in entries.GroupBy(x=>x.DonViId!.Value).Select(g=>g.First()))list.Add(new(e.DonViId!.Value,e.Title,e.Summary,await InArea(e.DonViId!.Value).CountAsync()));
  return list.OrderByDescending(x=>x.Count).ThenBy(x=>x.Name).ToList();
 }

 public async Task<DestinationPage> Destination(long unitId,string? tab=null,string? q=null,long? areaId=null)
 {
  tab=DestinationTabs.Any(t=>t.Key==tab)?tab!:"tong-quan";
  var path=await geo.Path(unitId);Need(path.Count>0,"Không tìm thấy điểm đến này.",404);
  var unit=path[^1];Need(unit.Regime==geo.Regime&&unit.Status=="Active","Điểm đến này không còn được sử dụng.",404);
  var area=InArea(unitId);
  var sub=geo.SubtreeQuery(unitId);

  // khu vuc dang chon (phai nam trong diem den)
  long? chosen=null;string? chosenName=null;
  if(areaId is long a&&a!=unitId&&await sub.AnyAsync(x=>x==a)){chosen=a;chosenName=(await geo.Get(a))?.FullName;}
  var scope=chosen!=null?InArea(chosen.Value):area;

  // so luong theo tab (tren toan bo diem den)
  var counts=new Dictionary<string,int>();
  foreach(var key in new[]{"tham-quan","an-uong","luu-tru","di-chuyen"})counts[key]=await TabQuery(area,key).CountAsync();
  var total=await area.CountAsync();
  var tabs=DestinationTabs.Select(t=>new DestinationTab(t.Key,t.Label,t.Key=="tong-quan"?total:t.Key=="ban-do"?0:counts.GetValueOrDefault(t.Key))).ToList();

  // tong quan: vai muc dau moi nhom
  var sections=new List<DestinationSection>();
  if(tab=="tong-quan")
  {
   foreach(var (key,title,icon) in new[]{("tham-quan","Khu du lịch & địa điểm tham quan","pin"),("an-uong","Ăn uống","food"),("luu-tru","Lưu trú","bed"),("di-chuyen","Di chuyển","car")})
   {
    var items=await Narrow(TabQuery(area,key).OrderBy(e=>e.Kind=="Guide"?1:0).ThenBy(e=>e.Title).Select(e=>e.Id).Take(3));await Hydrate(items);
    sections.Add(new(key,title,icon,counts[key],items.Select(e=>new DiscoveryCard(e)).ToList(),key));
   }
  }
  var guides=tab=="tong-quan"?await Narrow(area.Where(e=>(e.Kind=="Guide"&&!e.Tags.Contains("Ăn uống")&&!e.Tags.Contains("Lưu trú"))||e.Kind=="Event"||e.Kind=="Destination").OrderBy(e=>e.Kind=="Destination"?0:1).ThenBy(e=>e.Title).Select(e=>e.Id).Take(6)):new List<DiscoveryEntry>();
  await Hydrate(guides);

  // tab danh sach + ban do
  var cards=new List<DiscoveryCard>();var itemsTotal=0;var points=new List<MapPoint>();var without=0;
  if(tab is "tham-quan" or "an-uong" or "luu-tru" or "di-chuyen")
  {
   var query=TabQuery(scope,tab);
   if(!string.IsNullOrWhiteSpace(q)){var w=DiscoveryText.Fold(q.Trim());if(w.Length is >=2 and <=40)query=query.Where(e=>EF.Functions.Collate((" "+e.Title+" "+e.Summary+" "+e.Tags+" "+e.Body+" ").Replace("đ","d").Replace("Đ","D").Replace(","," ").Replace("."," ").Replace("-"," ").Replace("("," ").Replace(")"," "),SearchCollation).Contains(" "+w));}
   itemsTotal=await query.CountAsync();
   var list=await Narrow(query.OrderBy(e=>e.Kind=="Guide"?1:0).ThenBy(e=>e.Title).Select(e=>e.Id).Take(60));await Hydrate(list);
   cards=list.Select(e=>new DiscoveryCard(e)).ToList();
   var located=list.Where(e=>e.Latitude!=null&&e.Longitude!=null).ToList();without=list.Count-located.Count;
   points=located.Select(e=>new MapPoint(e.Id,e.Title,tab,DiscoveryText.Kind(e.Kind),DiscoveryText.LinkTo(e),e.Latitude!.Value,e.Longitude!.Value)).ToList();
  }
  else if(tab=="ban-do")
  {
   var located=await Narrow(scope.Where(e=>e.Latitude!=null&&e.Longitude!=null).OrderBy(e=>e.Title).Select(e=>e.Id).Take(300));await Hydrate(located);
   points=located.Select(e=>new MapPoint(e.Id,e.Title,Category(e),DiscoveryText.Kind(e.Kind),DiscoveryText.LinkTo(e),e.Latitude!.Value,e.Longitude!.Value)).ToList();
   without=await scope.CountAsync()-await scope.CountAsync(e=>e.Latitude!=null);
   tabs[^1]=tabs[^1] with{Count=await area.CountAsync(e=>e.Latitude!=null)};
  }
  if(tab!="ban-do")tabs[^1]=tabs[^1] with{Count=await area.CountAsync(e=>e.Latitude!=null)};

  // khu vuc con kem so muc thong tin
  var children=await geo.Children(unitId);
  var parents=await db.Set<DonViHanhChinh>().AsNoTracking().Where(u=>sub.Contains(u.Id)).Select(u=>new{u.Id,u.ParentId}).ToDictionaryAsync(u=>u.Id,u=>u.ParentId);
  var perUnit=await area.GroupBy(e=>e.DonViId).Select(g=>new{Id=g.Key!.Value,N=g.Count()}).ToListAsync();
  var perChild=new Dictionary<long,int>();
  foreach(var row in perUnit)
  {
   var cursor=row.Id;var guard=0;
   while(parents.TryGetValue(cursor,out var up)&&up!=null&&up!=unitId&&guard++<5)cursor=up.Value;
   if(cursor!=unitId)perChild[cursor]=perChild.GetValueOrDefault(cursor)+row.N;
  }
  var areas=children.Select(c=>new DestinationArea(c,perChild.GetValueOrDefault(c.Id))).OrderByDescending(a=>a.Count).ThenBy(a=>a.Unit.Name).ToList();

  // khu du lich (lop chong len dia gioi) - chi cong khai muc da cong bo
  var provinceId=path[0].Id;
  var zones=tab=="tong-quan"?await db.Set<KhuDuLich>().AsNoTracking().Where(z=>z.IsPublished&&(unit.Level==1?z.ProvinceId==provinceId:db.Set<KhuDuLichDonVi>().Any(l=>l.ZoneId==z.Id&&sub.Contains(l.UnitId)))).OrderBy(z=>z.SortOrder).ThenBy(z=>z.Name).Take(12).ToListAsync():new List<KhuDuLich>();

  // don vi cu nay thuoc tinh nao tu 01/07/2025
  var moves=new List<DestinationMove>();
  if(tab=="tong-quan"&&unit.Regime=="Cu")
  {
   var newUnits=db.Set<DonViChuyenDoi>().Where(m=>sub.Contains(m.OldUnitId)).Select(m=>m.NewUnitId);
   var newParents=await db.Set<DonViHanhChinh>().AsNoTracking().Where(u=>newUnits.Contains(u.Id)).Select(u=>u.ParentId).ToListAsync();
   var grouped=newParents.Where(p=>p!=null).GroupBy(p=>p!.Value).ToDictionary(g=>g.Key,g=>g.Count());
   var names=await db.Set<DonViHanhChinh>().AsNoTracking().Where(p=>grouped.Keys.Contains(p.Id)).ToDictionaryAsync(p=>p.Id,p=>p.FullName);
   moves=grouped.Select(g=>new DestinationMove(names.GetValueOrDefault(g.Key,"?"),g.Value)).OrderByDescending(m=>m.Units).ToList();
  }
  return new(unit,path,areas,sections,guides,zones,moves,total,tab,tabs,cards,itemsTotal,points,without,q,chosen,chosenName,QuickChips.GetValueOrDefault(tab)??[]);
 }

 public async Task<List<DestinationIndexItem>> DestinationIndex()
 {
  var provinces=await geo.Provinces();
  var perUnit=await Public().AsNoTracking().Where(e=>e.DonViId!=null).GroupBy(e=>e.DonViId).Select(g=>new{Id=g.Key!.Value,N=g.Count()}).ToListAsync();
  var perProvince=new Dictionary<long,int>();
  foreach(var row in perUnit){var chain=await geo.Path(row.Id);if(chain.Count>0)perProvince[chain[0].Id]=perProvince.GetValueOrDefault(chain[0].Id)+row.N;}
  return provinces.Select(p=>new DestinationIndexItem(p,perProvince.GetValueOrDefault(p.Id))).OrderByDescending(x=>x.Count).ThenBy(x=>x.Province.Name).ToList();
 }

 /// <summary>Diem do lien quan: ten khop chinh xac &gt; bat dau bang &gt; chua nguyen cum &gt; chua tung tu; muc "diem den" chi cong nhe.</summary>
 static int Relevance(DiscoveryEntry e,string? q,List<string> words)
 {
  var query=DiscoveryText.Canon(DiscoveryText.Fold(q)).Trim();var title=DiscoveryText.Fold(e.Title);if(query.Length==0)return 0;
  var score=0;
  if(title==query)score+=1000;
  else if(title.StartsWith(query))score+=700;
  else if(System.Text.RegularExpressions.Regex.IsMatch(title,@"\b"+System.Text.RegularExpressions.Regex.Escape(query)+@"\b"))score+=500;
  else if(title.Contains(query))score+=300;
  var inTitle=words.Count(w=>System.Text.RegularExpressions.Regex.IsMatch(title,@"\b"+System.Text.RegularExpressions.Regex.Escape(w)+@"\b"));
  score+=words.Count>0&&inTitle==words.Count?200:inTitle*50;
  if(e.Kind=="Destination")score+=score>0?30:10;
  return score;
 }

 /// <summary>Khi khong co ket qua: nhan ra dia danh trong cau hoi (hoac khu vuc dang chon) de goi y trang diem den.</summary>
 async Task<DestinationHint?> NearbyHint(DiscoveryFilter f,string phrase,HashSet<string> intentWords)
 {
  long? id=f.Area;string? path=null;
  if(id==null)
  {
   var tokens=DiscoveryText.Words(phrase).Where(w=>!intentWords.Contains(w)&&!w.All(char.IsDigit)).Take(6).ToList();
   var candidates=new List<string>();
   for(var len=Math.Min(4,tokens.Count);len>=1;len--)for(var start=0;start+len<=tokens.Count;start++){var c=string.Join(' ',tokens.Skip(start).Take(len));if(c.Length>=2)candidates.Add(c);}
   if(candidates.Count==0)return null;
   var set=candidates.ToHashSet();var regime=geo.Regime;
   var hits=await db.Set<DonViHanhChinh>().AsNoTracking().Where(u=>u.Regime==regime&&u.Status=="Active"&&u.Level<=2&&set.Contains(u.NameFolded)).Select(u=>new{u.Id,u.NameFolded,u.Level}).ToListAsync();
   var best=hits.OrderByDescending(h=>h.NameFolded.Length).ThenBy(h=>h.Level).FirstOrDefault();
   if(best==null)return null;
   id=best.Id;
  }
  path=await geo.PathText(id.Value);if(path.Length==0)return null;
  return new(id.Value,path,await InArea(id.Value).CountAsync());
 }

 /// <summary>Tim don vi hanh chinh (tinh/huyen) duoc goi ten trong cau hoi, vd "quan an Da Nang" -> Da Nang. Ten dai nhat, cap cao nhat thang.</summary>
 async Task<(long Id,string Name)?> FindPlaceUnit(string phrase)
 {
  var ignore=new HashSet<string>{"luu","tru","an","uong","di","lai","chuyen","tham","quan","noi","gan","nhat","trong","nha"};
  var tokens=DiscoveryText.Words(phrase).Where(w=>!ignore.Contains(w)&&!w.All(char.IsDigit)).Take(6).ToList();
  var candidates=new HashSet<string>();
  for(var len=Math.Min(4,tokens.Count);len>=1;len--)for(var start=0;start+len<=tokens.Count;start++){var c=string.Join(' ',tokens.Skip(start).Take(len));if(c.Length>=3)candidates.Add(c);}
  if(candidates.Count==0)return null;
  var regime=geo.Regime;
  var hits=await db.Set<DonViHanhChinh>().AsNoTracking().Where(u=>u.Regime==regime&&u.Status=="Active"&&u.Level<=2&&candidates.Contains(u.NameFolded)).Select(u=>new{u.Id,u.Name,u.NameFolded,u.Level}).ToListAsync();
  var best=hits.OrderByDescending(h=>h.NameFolded.Length).ThenBy(h=>h.Level).FirstOrDefault();
  return best==null?null:(best.Id,best.Name);
 }
}

/// <summary>Anh cua mot tinh/thanh de hien tren the va hero; Credit/SourceUrl null voi anh minh hoa co san cua website.</summary>
public sealed record UnitPhoto(string Path, string? Credit, string? SourceUrl);

/// <summary>Tra anh tinh/thanh: anh Wikimedia da duyet (bang AnhDonVi) truoc, roi den nam anh minh hoa co san theo ten.</summary>
public sealed class DestinationPhotos(HoTroDuLich.Web.Data.AppDbContext db)
{
 public async Task<Dictionary<long,UnitPhoto>> For(IEnumerable<(long Id,string Name)> units)
 {
  var list=units.ToList();var ids=list.Select(x=>x.Id).Distinct().ToList();
  var rows=ids.Count==0?new List<AnhDonVi>():await db.Set<AnhDonVi>().AsNoTracking().Where(x=>ids.Contains(x.DonViId)).ToListAsync();
  var map=new Dictionary<long,UnitPhoto>();
  foreach(var (id,name) in list)
  {
   if(map.ContainsKey(id))continue;
   var row=rows.FirstOrDefault(r=>r.DonViId==id);
   if(row!=null)map[id]=new(row.ImagePath,row.Credit,row.SourceUrl);
   else if(Ui.DestinationPhoto(name) is string local)map[id]=new(local,null,null);
  }
  return map;
 }
 public async Task<UnitPhoto?> For(long id,string name)=>(await For([(id,name)])).GetValueOrDefault(id);
}
