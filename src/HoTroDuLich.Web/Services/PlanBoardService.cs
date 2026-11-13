using System.Globalization;
using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

public sealed class DraftItem
{
 public long Id { get; set; }
 public int Day { get; set; }
 public string? Time { get; set; }
 public string Content { get; set; }="";
 public decimal? Cost { get; set; }
 public long? ServiceId { get; set; }
 public long? PlaceId { get; set; }
 public string? Kind { get; set; }
}
public sealed class DayNoteDto
{
 public int Day { get; set; }
 public string Text { get; set; }="";
}
public sealed class PlanDraft
{
 public List<DayNoteDto> Notes { get; set; }=[];
 public long Id { get; set; }
 public string Name { get; set; }="";
 public string Destination { get; set; }="";
 public DateOnly? Start { get; set; }
 public int Days { get; set; }=3;
 public decimal? Budget { get; set; }
 public List<DraftItem> Items { get; set; }=[];
}
public sealed record PlanEditor(PlanDraft Draft,bool IsOwner,long? GroupId,List<string> MemberNames);
public sealed record PlanMapPoint(string Name,string Title,double Lat,double Lng);
public sealed record PickItem(long? EntryId,long? ServiceId,long? PlaceId,string Name,string Kind,string KindLabel,string Group,string Region,string? Summary,string? Address,string? Hours,string? Contact,decimal? PriceFrom,decimal? PriceTo,string? Unit,string? Photo,double? Lat,double? Lng,double? Rating,int Reviews,bool Saved,string? Url);
public sealed record PickPage(List<PickItem> Items,int Total,int Page,int PageSize,bool HasMore=false);
public sealed record DestHit(string Name,string? Path,long? AreaId,int Level,string Source,string? Summary,string? Photo,double? Lat,double? Lng);

/// <summary>Lap ke hoach tren mot trang: tai ban nhap, luu ca chuyen di va lich trinh mot lan, xoa chuyen di, goi y dich vu va diem den.</summary>
public sealed class PlanBoardService(AppDbContext context,IHttpContextAccessor accessor,IWebHostEnvironment environment,IConfiguration configuration,IServiceProvider provider,ReviewService reviews,DiscoveryService discovery,DiaGioiService geo)
 : NghiepVuService(context,accessor,environment,configuration,provider)
{
 TravelService Travel=>Resolve<TravelService>();
 static readonly string[] Junk=["Theo cơ sở","kiểm thử","Xã ","Huyện ","Phường "];

 // ------------------------------------------------------------ tai ban nhap
 public async Task<PlanEditor> Load(long id)
 {
  RoleRequired(Traveler);
  var plan=await Travel.SharedPlan(id);var acts=await Travel.Activities(id);
  var serviceIds=acts.Where(a=>a.MaDichVu!=null).Select(a=>a.MaDichVu!.Value).Distinct().ToList();
  var kinds=serviceIds.Count==0?new Dictionary<long,string>():await db.DichVuDuLich.AsNoTracking().Where(d=>serviceIds.Contains(d.MaDichVu)).ToDictionaryAsync(d=>d.MaDichVu,d=>d.LoaiDichVu);
  var draft=new PlanDraft{Id=plan.MaKeHoach,Name=plan.TenKeHoach,Destination=plan.NoiDen,Start=plan.NgayBatDau,Days=plan.NgayKetThuc.DayNumber-plan.NgayBatDau.DayNumber+1,Budget=plan.NganSachDuKien,
   Items=acts.OrderBy(a=>a.Ngay).ThenBy(a=>a.Gio).ThenBy(a=>a.ThuTu).Select(a=>new DraftItem{Id=a.MaHoatDong,Day=a.Ngay.DayNumber-plan.NgayBatDau.DayNumber+1,Time=a.Gio?.ToString("HH:mm"),Content=a.NoiDung,Cost=a.ChiPhiDuKien,ServiceId=a.MaDichVu,PlaceId=a.MaDiaDiem,Kind=a.MaDichVu!=null&&kinds.TryGetValue(a.MaDichVu.Value,out var k)?ServiceKinds.Get(k)?.Group:null}).ToList()};
  draft.Notes=(await db.Set<TripDayNote>().AsNoTracking().Where(n=>n.PlanId==id).OrderBy(n=>n.DayNo).ToListAsync()).Select(n=>new DayNoteDto{Day=n.DayNo,Text=n.Note}).ToList();
  var members=await Resolve<TripShareService>().Members(id);
  return new(draft,plan.MaTaiKhoan==UserId,await Resolve<ChatService>().TripGroupFor(id),members.Members.Where(m=>m.IsOwner||m.Accepted).Select(m=>m.Name).ToList());
 }

 // ------------------------------------------------------------ luu mot lan
 public async Task<long> Save(PlanDraft d)
 {
  RoleRequired(Traveler);
  var name=(d.Name??"").Trim();Require(name.Length is >=1 and <=150,"Nhập tên chuyến đi (tối đa 150 ký tự).",422);
  var dest=(d.Destination??"").Trim();Require(dest.Length is >=2 and <=200,"Chọn điểm đến.",422);
  Require(d.Days is >=1 and <=30,"Số ngày từ 1 đến 30.",422);
  Require(d.Start!=null,"Chọn ngày đi.",422);
  Require(d.Budget==null||d.Budget is >=0 and <=1_000_000_000,"Ngân sách từ 0 đến 1.000.000.000 ₫.",422);
  Require(d.Items.Count<=300,"Mỗi chuyến đi tối đa 300 hạng mục.",422);
  foreach(var n in d.Notes){Require(n.Day>=1&&n.Day<=d.Days,"Ghi chú nằm ngoài các ngày của chuyến đi.",422);Require((n.Text??"").Trim().Length<=1000,"Ghi chú mỗi ngày tối đa 1000 ký tự.",422);}
  var start=d.Start!.Value;var end=start.AddDays(d.Days-1);
  var clean=new List<(DraftItem Item,TimeOnly? Time,string Content)>();
  foreach(var i in d.Items)
  {
   var content=(i.Content??"").Trim();
   Require(content.Length is >=1 and <=300,"Mỗi hạng mục cần có tên (tối đa 300 ký tự).",422);
   Require(i.Day>=1&&i.Day<=d.Days,"Hạng mục \""+content+"\" nằm ngoài các ngày của chuyến đi.",422);
   Require(i.Cost==null||i.Cost is >=0 and <=1_000_000_000,"Chi phí của \""+content+"\" không hợp lệ.",422);
   TimeOnly? time=null;
   if(!string.IsNullOrWhiteSpace(i.Time)){Require(TimeOnly.TryParseExact(i.Time,"HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out var t),"Giờ của \""+content+"\" cần dạng HH:mm.",422);time=TimeOnly.ParseExact(i.Time,"HH:mm",CultureInfo.InvariantCulture);}
   clean.Add((i,time,content));
  }
  long id=d.Id;
  if(id==0)
  {
   Require(start>=TravelService.Today,"Ngày đi không được trong quá khứ.",422);
   id=await Travel.SavePlan(new PlanInput{Id=0,Version="0",Name=name,Destination=dest,Start=start,End=end,Budget=d.Budget});
  }
  else
  {
   var plan=await Travel.SharedPlan(id);var owner=plan.MaTaiKhoan==UserId;
   var existing=await Travel.Activities(id);
   var keep=clean.Where(c=>c.Item.Id>0).Select(c=>c.Item.Id).ToHashSet();
   Require(keep.All(k=>existing.Any(e=>e.MaHoatDong==k)),"Có hạng mục không còn trong chuyến đi. Hãy tải lại trang.",409);
   foreach(var gone in existing.Where(e=>!keep.Contains(e.MaHoatDong)).ToList())
    await Travel.DeleteActivity(new CommandInput{Id=gone.MaHoatDong,Version=TravelService.Token((await Travel.SharedPlan(id)).RowVersion)});
   plan=await Travel.SharedPlan(id);
   var infoChanged=name!=plan.TenKeHoach||dest!=plan.NoiDen||start!=plan.NgayBatDau||end!=plan.NgayKetThuc||d.Budget!=plan.NganSachDuKien;
   if(infoChanged)
   {
    Require(owner,"Chỉ chủ chuyến đi mới đổi được tên, điểm đến, ngày và ngân sách.",403);
    await Travel.SavePlan(new PlanInput{Id=id,Version=TravelService.Token(plan.RowVersion),Name=name,Destination=dest,Start=start,End=end,Budget=d.Budget,Status=plan.TrangThai.ToString()});
   }
  }
  var current=(await Travel.Activities(id)).ToDictionary(a=>a.MaHoatDong);
  var planStart=(await Travel.SharedPlan(id)).NgayBatDau;
  foreach(var (item,time,content) in clean)
  {
   var date=planStart.AddDays(item.Day-1);
   if(item.Id>0&&current.TryGetValue(item.Id,out var a))
   {
    if(a.Ngay==date&&a.Gio==time&&a.NoiDung==content&&a.ChiPhiDuKien==item.Cost&&a.MaDichVu==item.ServiceId&&a.MaDiaDiem==item.PlaceId)continue;
    await Travel.SaveActivity(new ActivityInput{Id=a.MaHoatDong,PlanId=id,Version=TravelService.Token((await Travel.SharedPlan(id)).RowVersion),Date=date,Time=time,Content=content,Cost=item.Cost,ServiceId=item.ServiceId,PlaceId=item.PlaceId,DurationMinutes=a.ThoiLuongPhut,TravelMinutes=a.DiChuyenTruocPhut});
   }
   else await Travel.SaveActivity(new ActivityInput{Id=0,PlanId=id,Version=TravelService.Token((await Travel.SharedPlan(id)).RowVersion),Date=date,Time=time,Content=content,Cost=item.Cost,ServiceId=item.ServiceId,PlaceId=item.PlaceId});
  }
  await SaveNotes(id,d);
  await Resolve<ChatService>().SyncTripGroup(id);
  return id;
 }

 async Task SaveNotes(long id,PlanDraft d)
 {
  var want=d.Notes.Select(n=>(n.Day,Text:(n.Text??"").Trim())).Where(n=>n.Text.Length>0).GroupBy(n=>n.Day).ToDictionary(g=>g.Key,g=>g.Last().Text);
  await Execute(async()=>
  {
   var rows=await db.Set<TripDayNote>().Where(n=>n.PlanId==id).ToListAsync();
   foreach(var r in rows.Where(r=>!want.ContainsKey(r.DayNo)||r.DayNo>d.Days))db.Remove(r);
   foreach(var (day,text) in want.Where(w=>w.Key<=d.Days))
   {
    var r=rows.FirstOrDefault(x=>x.DayNo==day);
    if(r==null)db.Add(new TripDayNote{PlanId=id,DayNo=day,Note=text,UpdatedAt=Clock.BayGioUtc});
    else if(r.Note!=text){r.Note=text;r.UpdatedAt=Clock.BayGioUtc;}
   }
  });
 }
 public async Task<Dictionary<int,string>> Notes(long planId)
 {
  RoleRequired(Traveler);await Travel.SharedPlan(planId);
  return await db.Set<TripDayNote>().AsNoTracking().Where(n=>n.PlanId==planId).ToDictionaryAsync(n=>n.DayNo,n=>n.Note);
 }

 // ------------------------------------------------------------ xoa chuyen di
 /// <summary>Xoa han chuyen di cua minh cung moi thu rieng cua no. Tu choi khi da co yeu cau dat dich vu, nhom dong hanh hoac tinh huong phat sinh.</summary>
 public async Task DeletePlan(long planId)
 {
  RoleRequired(Traveler);
  var plan=await Travel.OwnPlan(planId);
  Require(!await db.YeuCauDatDichVu.AnyAsync(x=>x.MaKeHoach==planId),"Chuyến đi đã có yêu cầu đặt dịch vụ nên không xóa được. Hãy hủy các yêu cầu trước.",409);
  Require(!await db.NhomDongHanh.AnyAsync(x=>x.MaKeHoach==planId),"Chuyến đi đang gắn với nhóm đồng hành nên không xóa được.",409);
  Require(!await db.TinhHuongPhatSinh.AnyAsync(x=>x.MaKeHoach==planId),"Chuyến đi đã ghi nhận tình huống phát sinh nên không xóa được.",409);
  await Execute(async()=>
  {
   var expenseIds=await db.Set<GroupExpense>().Where(e=>e.PlanId==planId).Select(e=>e.Id).ToListAsync();
   db.RemoveRange(db.Set<GroupExpenseShare>().Where(s=>expenseIds.Contains(s.ExpenseId)));await db.SaveChangesAsync();
   db.RemoveRange(db.Set<GroupExpense>().Where(e=>e.PlanId==planId));
   db.RemoveRange(db.Set<TripChatLink>().Where(x=>x.PlanId==planId));
   db.RemoveRange(db.Set<TripDayNote>().Where(x=>x.PlanId==planId));
   db.RemoveRange(db.ChiaSeChuyenDi.Where(x=>x.MaKeHoach==planId));
   db.RemoveRange(db.Set<ThanhVienChuyenDi>().Where(x=>x.PlanId==planId));
   db.RemoveRange(db.MucChuanBi.Where(x=>x.MaKeHoach==planId));
   db.RemoveRange(db.KhoanChiChuyenDi.Where(x=>x.MaKeHoach==planId));
   db.RemoveRange(db.HoatDongLichTrinh.Where(x=>x.MaKeHoach==planId));
   await db.SaveChangesAsync();
   db.Remove(plan);
  });
 }

 // ------------------------------------------------------------ diem den chi tiet
 /// <summary>Tim diem den o nhieu cap: don vi hanh chinh (tinh, quan/huyen, phuong/xa) va khu du lich / dia diem / diem den cua Kham pha.</summary>
 public async Task<List<DestHit>> DestinationSearch(string? q)
 {
  RoleRequired(Traveler);
  var term=(q??"").Trim();if(term.Length>100)term=term[..100];
  var list=new List<DestHit>();
  if(term.Length>0)foreach(var h in (await geo.Search(term,12)))list.Add(new(h.Name,h.Path,h.Id,h.Level,"area",null,null,null,null));
  var entries=db.Set<DiscoveryEntry>().AsNoTracking().Where(e=>e.Status=="Published"&&(e.Kind=="Destination"||e.Kind=="Place"||e.Kind=="Area"));
  if(term.Length>0)entries=entries.Where(e=>e.Title.Contains(term)||e.Region.Contains(term));
  foreach(var e in await entries.OrderByDescending(e=>e.Kind=="Destination").ThenBy(e=>e.Title).Take(200).ToListAsync())
   list.Add(new(e.Title,e.Region,e.DonViId,0,e.Kind=="Destination"?"destination":e.Kind=="Area"?"area-entry":"place",Cut(e.Summary,160),e.ImagePath??Ui.DestinationPhoto(e.Region+" "+e.Title),(double?)e.Latitude,(double?)e.Longitude));
  return list;
 }
 public async Task<List<PlanMapPoint>> MapPoints()
 {
  RoleRequired(Traveler);
  var rows=await db.Set<DiscoveryEntry>().AsNoTracking().Where(e=>e.Status=="Published"&&e.Latitude!=null&&e.Longitude!=null&&(e.Kind=="Destination"||e.Kind=="Place"||e.Kind=="Area")).Select(e=>new{e.Title,e.Region,e.Latitude,e.Longitude,e.DonViId}).ToListAsync();
  var ids=rows.Where(r=>r.DonViId!=null).Select(r=>r.DonViId!.Value).Distinct().ToList();
  var units=await db.Set<DonViHanhChinh>().AsNoTracking().Where(u=>ids.Contains(u.Id)).ToDictionaryAsync(u=>u.Id,u=>u.Name);
  return rows.Select(r=>new PlanMapPoint(r.DonViId!=null&&units.TryGetValue(r.DonViId.Value,out var n)?n:r.Title,r.Title,(double)r.Latitude!.Value,(double)r.Longitude!.Value)).ToList();
 }
 static string? Cut(string? v,int n)=>string.IsNullOrWhiteSpace(v)?null:v.Length<=n?v:v[..(n-1)]+"…";

 // ------------------------------------------------------------ them dich vu: day du bo loc cua Kham pha
 string GroupOf(DiscoveryEntry e,Dictionary<long,string> serviceTypes)
 {
  if(e.ServiceId!=null&&serviceTypes.TryGetValue(e.ServiceId.Value,out var t))return ServiceKinds.Get(t)?.Group??"Other";
  return e.Kind switch{"Place" or "Destination" or "Area"=>"Place","Transport"=>"Move","Guide"=>"Local",_=>"Other"};
 }
 /// <summary>Tim dich vu / dia diem bang chinh bo may tim kiem cua Kham pha (tu khoa, nhom, khu vuc hanh chinh, gia, sap xep, khung ban do), muc DA LUU cua toi len truoc.</summary>
 public async Task<PickPage> Explore(DiscoveryFilter f,bool savedOnly)
 {
  RoleRequired(Traveler);
  f.Page=Math.Max(1,f.Page);
  var ratings=await reviews.Ratings();
  var items=new List<PickItem>();
  var seenS=new HashSet<long>();var seenP=new HashSet<long>();
  var term=(f.Q??"").Trim();
  if(f.Page==1)
  {
   foreach(var s in await Resolve<ProductService>().Saved())
   {
    if(!string.IsNullOrEmpty(f.Group)&&(s.Service==null?f.Group!="Place":(ServiceKinds.Get(s.Service.LoaiDichVu)?.Group??"Other")!=f.Group))continue;
    if(s.Service!=null)
    {
     if(term.Length>0&&!s.Service.TenDichVu.Contains(term,StringComparison.OrdinalIgnoreCase))continue;
     if(f.Min!=null&&(s.Service.GiaThamKhao??0)<f.Min)continue;if(f.Max!=null&&s.Service.GiaThamKhao!=null&&s.Service.GiaThamKhao>f.Max)continue;
     seenS.Add(s.Service.MaDichVu);var sv=s.Service;
     items.Add(new(null,sv.MaDichVu,null,sv.TenDichVu,"Service","Dịch vụ",ServiceKinds.Get(sv.LoaiDichVu)?.Group??"Other",sv.KhuVucPhucVu,Cut(sv.MoTa,200),sv.DiaChiPhucVu,sv.ThoiGianPhucVu,null,sv.GiaThamKhao,null,sv.DonViGia,Ui.DestinationPhoto(sv.KhuVucPhucVu+" "+sv.TenDichVu),null,null,ratings.TryGetValue(sv.MaDichVu,out var r)?r.Average:null,ratings.TryGetValue(sv.MaDichVu,out var r2)?r2.Count:0,true,"/Explore/Detail/"+sv.MaDichVu));
    }
    else if(s.Place!=null)
    {
     if(term.Length>0&&!s.Place.TenDiaDiem.Contains(term,StringComparison.OrdinalIgnoreCase))continue;
     if(f.Min!=null||f.Max!=null)continue;
     seenP.Add(s.Place.MaDiaDiem);var pl=s.Place;
     items.Add(new(null,null,pl.MaDiaDiem,pl.TenDiaDiem,"Place","Địa điểm","Place",pl.KhuVuc,Cut(pl.MoTa,200),pl.DiaChi,null,null,null,null,null,Ui.DestinationPhoto(pl.KhuVuc+" "+pl.TenDiaDiem),null,null,null,0,true,"/Places/Detail/"+pl.MaDiaDiem));
    }
   }
  }
  var savedCount=items.Count;
  if(savedOnly)return new(items,items.Count,1,Math.Max(items.Count,1));
  DiscoverySearchPage page;
  try{page=await discovery.Search(f);}catch(BusinessException){throw;}
  var sIds=page.Items.Where(c=>c.Entry.ServiceId!=null).Select(c=>c.Entry.ServiceId!.Value).ToList();
  var types=sIds.Count==0?new Dictionary<long,string>():await db.DichVuDuLich.AsNoTracking().Where(d=>sIds.Contains(d.MaDichVu)).ToDictionaryAsync(d=>d.MaDichVu,d=>d.LoaiDichVu);
  var pIds=page.Items.Where(c=>c.Entry.PlaceId!=null).Select(c=>c.Entry.PlaceId!.Value).ToList();
  var placeTypes=pIds.Count==0?new Dictionary<long,string>():await db.DiaDiemDuLich.AsNoTracking().Where(p=>pIds.Contains(p.MaDiaDiem)).ToDictionaryAsync(p=>p.MaDiaDiem,p=>p.LoaiDiaDiem);
  foreach(var c in page.Items)
  {
   var e=c.Entry;
   // chi cho chon dich vu that (dat duoc) hoac dia diem; bo muc diem den, khu vuc va thong tin tham khao khong gan dich vu
   if(e.Kind is "Destination" or "Area"||(e.ServiceId==null&&e.PlaceId==null))continue;
   if(e.ServiceId!=null&&seenS.Contains(e.ServiceId.Value))continue;if(e.PlaceId!=null&&seenP.Contains(e.PlaceId.Value))continue;
   var sid=e.ServiceId;
   items.Add(new(e.Id,e.ServiceId,e.PlaceId,e.Title,e.Kind,e.PlaceId!=null&&placeTypes.TryGetValue(e.PlaceId.Value,out var loai)&&!string.IsNullOrWhiteSpace(loai)?"Địa điểm · "+loai:DiscoveryText.Kind(e.Kind),string.IsNullOrEmpty(f.Group)?GroupOf(e,types):f.Group,e.Region,Cut(e.Summary,200),e.Address,e.Hours??(e.OpenTime!=null?e.OpenTime.Value.ToString("HH:mm")+(e.CloseTime!=null?"–"+e.CloseTime.Value.ToString("HH:mm"):""):null),e.Contact,e.PriceFrom,e.PriceTo,e.PriceUnit,e.ImagePath??Ui.DestinationPhoto(e.Region+" "+e.Title),(double?)e.Latitude,(double?)e.Longitude,sid!=null&&ratings.TryGetValue(sid.Value,out var r)?r.Average:null,sid!=null&&ratings.TryGetValue(sid.Value,out var r2)?r2.Count:0,false,DiscoveryText.LinkTo(e)));
  }
  return new(items,items.Count,f.Page,20,page.Items.Count>=20);
 }

 /// <summary>Thong tin day du cua mot muc Kham pha de xem truoc khi them vao ngay.</summary>
 public async Task<object> EntryDetail(long entryId)
 {
  RoleRequired(Traveler);
  var d=await discovery.Detail(entryId);var e=d.Entry;
  return new{id=e.Id,title=e.Title,kind=DiscoveryText.Kind(e.Kind),region=e.Region,summary=e.Summary,body=Cut(e.Body,1500),address=e.Address,hours=e.Hours,openDays=e.OpenDays,duration=e.DurationMinutes,contact=e.Contact,includes=e.Includes,excludes=e.Excludes,source=e.SourceName,checkedOn=e.CheckedOn?.ToString("dd/MM/yyyy"),url=DiscoveryText.LinkTo(e),
   prices=d.Prices.Select(p=>new{p.Label,p.Amount,p.Unit,p.Conditions,p.Included}),features=d.Features.Select(x=>x.ToString()).Take(0)};
 }
}
