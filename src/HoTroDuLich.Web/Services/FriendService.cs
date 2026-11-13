using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

public sealed record PersonDto(long Id,string Name,string? Avatar,string? Bio,string Relation,int Mutual,long? RequestId=null);
public sealed record CompanionMatchDto(PersonDto Person,long NeedId,string Destination,DateOnly Start,DateOnly End,string? Mode,string Because,List<string>? Reasons=null);
public sealed record ProfileDto(long Id,string Name,string? Avatar,string? Bio,string? City,string? Interests,string Relation,long? RequestId,int Friends,int Mutual,bool Self,DateTime Joined,string? Cover=null);

/// <summary>Ket ban kieu Facebook giua cac khach du lich: gui / chap nhan / tu choi / huy loi moi, xoa ban, goi y va tim nguoi.</summary>
public sealed class FriendService(AppDbContext context,IHttpContextAccessor accessor,IWebHostEnvironment environment,IConfiguration configuration,IServiceProvider provider,IThongBaoService notifications)
 : NghiepVuService(context,accessor,environment,configuration,provider)
{
 const int Page=40;
 static DateTime Utc(DateTime d)=>DateTime.SpecifyKind(d,DateTimeKind.Utc);

 // ------------------------------------------------------------------ quan he
 /// <summary>Tai khoan khach du lich dang hoat dong.</summary>
 IQueryable<TaiKhoan> ActiveTravelers()=>db.TaiKhoan.AsNoTracking().Where(a=>a.VaiTro==VaiTro.KHACH_DU_LICH&&a.TrangThai==TrangThaiTaiKhoan.HoatDong);
 void Me(){RoleRequired(Traveler);}
 string RelationOf(KetBan? row,long me)
 {
  if(row==null)return "none";
  return row.Status switch{"Accepted"=>"friends","Pending"=>row.RequesterId==me?"sent":"received",_=>row.RequesterId==me?"declined-by-them":"none"};
 }
 public async Task<KetBan?> PairRow(long a,long b)
 {
  var lo=Math.Min(a,b);var hi=Math.Max(a,b);
  return await db.Set<KetBan>().SingleOrDefaultAsync(x=>(x.RequesterId==lo&&x.AddresseeId==hi)||(x.RequesterId==hi&&x.AddresseeId==lo));
 }
 public async Task<string> RelationTo(long other)
 {
  if(UserId==0)return "none";if(other==UserId)return "self";
  return RelationOf(await PairRow(UserId,other),UserId);
 }
 public async Task<bool> AreFriends(long a,long b)=>a!=b&&await db.Set<KetBan>().AnyAsync(x=>x.Status=="Accepted"&&((x.RequesterId==a&&x.AddresseeId==b)||(x.RequesterId==b&&x.AddresseeId==a)));
 /// <summary>Id ban be (da chap nhan) cua mot tai khoan.</summary>
 public async Task<List<long>> FriendIds(long account)
  =>await db.Set<KetBan>().AsNoTracking().Where(x=>x.Status=="Accepted"&&(x.RequesterId==account||x.AddresseeId==account)).Select(x=>x.RequesterId==account?x.AddresseeId:x.RequesterId).ToListAsync();
 async Task<Dictionary<long,(string Name,bool HasAvatar,string? Bio,ThongTinChiaSe Share)>> Profiles(IEnumerable<long> ids)
 {
  var list=ids.Distinct().ToList();
  var rows=await db.Set<HoSoNguoiDung>().AsNoTracking().Where(h=>list.Contains(h.MaTaiKhoan)).Select(h=>new{h.MaTaiKhoan,h.TenHienThi,h.AnhDaiDien,h.GioiThieu,h.ThongTinChiaSe}).ToListAsync();
  var providerNames=await db.NhaCungCapDichVu.AsNoTracking().Where(p=>list.Contains(p.MaTaiKhoan)).ToDictionaryAsync(p=>p.MaTaiKhoan,p=>p.TenDonVi);
  return list.ToDictionary(id=>id,id=>{var r=rows.FirstOrDefault(x=>x.MaTaiKhoan==id);return (string.IsNullOrWhiteSpace(r?.TenHienThi)?providerNames.GetValueOrDefault(id,"Người dùng #"+id):r!.TenHienThi,r?.AnhDaiDien!=null,r?.GioiThieu,r?.ThongTinChiaSe??ThongTinChiaSe.CongKhai);});
 }
 /// <summary>Ten hien thi cong khai cua nhieu tai khoan (dung cho bai viet, binh luan, chat).</summary>
 public async Task<Dictionary<long,string>> Names(IEnumerable<long> ids)=>(await Profiles(ids)).ToDictionary(x=>x.Key,x=>x.Value.Name);
 /// <summary>Nguoi xem co duoc thay anh va gioi thieu cua <paramref name="target"/> khong (chinh minh, cong khai hoac ban be).</summary>
 public async Task<bool> CanSeeDetails(long target)
 {
  if(UserId==target)return true;
  var share=await db.Set<HoSoNguoiDung>().AsNoTracking().Where(h=>h.MaTaiKhoan==target).Select(h=>(ThongTinChiaSe?)h.ThongTinChiaSe).FirstOrDefaultAsync()??ThongTinChiaSe.CongKhai;
  if(share==ThongTinChiaSe.CongKhai)return true;
  return share==ThongTinChiaSe.KhiKetNoi&&UserId>0&&await AreFriends(UserId,target);
 }
 async Task<List<PersonDto>> Cards(List<long> ids,Dictionary<long,KetBan>? rows=null,Dictionary<long,int>? mutual=null)
 {
  var profiles=await Profiles(ids);
  rows??=(await db.Set<KetBan>().AsNoTracking().Where(x=>x.RequesterId==UserId||x.AddresseeId==UserId).ToListAsync()).ToDictionary(x=>x.RequesterId==UserId?x.AddresseeId:x.RequesterId);
  var friendsOfMe=mutual==null?null:mutual;
  var result=new List<PersonDto>();
  foreach(var id in ids)
  {
   var p=profiles[id];rows.TryGetValue(id,out var row);
   var visible=p.Share==ThongTinChiaSe.CongKhai||(p.Share==ThongTinChiaSe.KhiKetNoi&&row?.Status=="Accepted");
   result.Add(new PersonDto(id,p.Name,p.HasAvatar&&visible?"/mxh/anh/"+id:null,visible?p.Bio:null,RelationOf(row,UserId),friendsOfMe?.GetValueOrDefault(id)??0,row?.Id));
  }
  return result;
 }

 // ------------------------------------------------------------------ danh sach
 public async Task<List<PersonDto>> Friends(string? q=null)
 {
  Me();var ids=await FriendIds(UserId);
  var active=await ActiveTravelers().Where(a=>ids.Contains(a.MaTaiKhoan)).Select(a=>a.MaTaiKhoan).ToListAsync();
  var cards=await Cards(active);
  if(!string.IsNullOrWhiteSpace(q))cards=cards.Where(c=>c.Name.Contains(q.Trim(),StringComparison.OrdinalIgnoreCase)).ToList();
  return cards.OrderBy(c=>c.Name,StringComparer.CurrentCultureIgnoreCase).ToList();
 }
 /// <summary>Loi moi dang cho: den toi (Incoming) va toi da gui (Outgoing).</summary>
 public async Task<(List<PersonDto> Incoming,List<PersonDto> Outgoing)> Requests()
 {
  Me();
  var rows=await db.Set<KetBan>().AsNoTracking().Where(x=>x.Status=="Pending"&&(x.RequesterId==UserId||x.AddresseeId==UserId)).OrderByDescending(x=>x.CreatedAt).ToListAsync();
  var map=rows.ToDictionary(x=>x.RequesterId==UserId?x.AddresseeId:x.RequesterId);
  var all=await Cards(map.Keys.ToList(),map);
  return (all.Where(c=>c.Relation=="received").ToList(),all.Where(c=>c.Relation=="sent").ToList());
 }
 public async Task<int> IncomingCount()=>UserId==0||Role!=Traveler?0:await db.Set<KetBan>().CountAsync(x=>x.Status=="Pending"&&x.AddresseeId==UserId);
 /// <summary>Goi y ket ban: nhieu ban chung nhat truoc, bo sung bang nguoi dang co loi ru cong khai.</summary>
 public async Task<List<PersonDto>> Suggestions()
 {
  Me();
  var related=(await db.Set<KetBan>().AsNoTracking().Where(x=>x.RequesterId==UserId||x.AddresseeId==UserId).Select(x=>x.RequesterId==UserId?x.AddresseeId:x.RequesterId).ToListAsync()).ToHashSet();
  related.Add(UserId);
  var mine=await FriendIds(UserId);
  var mutual=new Dictionary<long,int>();
  if(mine.Count>0)
  {
   var edges=await db.Set<KetBan>().AsNoTracking().Where(x=>x.Status=="Accepted"&&(mine.Contains(x.RequesterId)||mine.Contains(x.AddresseeId))).Select(x=>new{x.RequesterId,x.AddresseeId}).Take(2000).ToListAsync();
   foreach(var e in edges)
   {
    var friend=mine.Contains(e.RequesterId)?e.RequesterId:e.AddresseeId;var other=friend==e.RequesterId?e.AddresseeId:e.RequesterId;
    if(related.Contains(other))continue;mutual[other]=mutual.GetValueOrDefault(other)+1;
   }
  }
  var ids=mutual.OrderByDescending(x=>x.Value).Select(x=>x.Key).Take(Page).ToList();
  if(ids.Count<20)
  {
   var more=await db.NhuCauDongHanh.AsNoTracking().Where(n=>n.TrangThaiCongBo==TrangThaiNhuCau.DangCongBo).OrderByDescending(n=>n.ThoiDiemDang).Select(n=>n.MaTaiKhoan).Take(200).ToListAsync();
   foreach(var m in more){if(ids.Count>=20)break;if(!related.Contains(m)&&!ids.Contains(m))ids.Add(m);}
  }
  var active=await ActiveTravelers().Where(a=>ids.Contains(a.MaTaiKhoan)).Select(a=>a.MaTaiKhoan).ToListAsync();
  ids=ids.Where(active.Contains).ToList();
  return await Cards(ids,null,mutual);
 }
 public async Task<List<PersonDto>> Search(string? q)
 {
  Me();var term=(q??"").Trim();
  Require(term.Length>=2,"Nhập ít nhất 2 ký tự để tìm.",422);
  var ids=await db.Set<HoSoNguoiDung>().AsNoTracking().Where(h=>h.TenHienThi.Contains(term)&&h.MaTaiKhoan!=UserId&&db.TaiKhoan.Any(a=>a.MaTaiKhoan==h.MaTaiKhoan&&a.VaiTro==VaiTro.KHACH_DU_LICH&&a.TrangThai==TrangThaiTaiKhoan.HoatDong)).OrderBy(h=>h.TenHienThi).Select(h=>h.MaTaiKhoan).Take(20).ToListAsync();
  return await Cards(ids);
 }

 // ------------------------------------------------------------------ ho so
 public async Task<ProfileDto> Profile(long id)
 {
  Me();
  var account=await ActiveTravelers().SingleOrDefaultAsync(a=>a.MaTaiKhoan==id)??throw new BusinessException("Không tìm thấy người dùng.",404);
  var p=await db.Set<HoSoNguoiDung>().AsNoTracking().SingleOrDefaultAsync(h=>h.MaTaiKhoan==id);
  var row=id==UserId?null:await PairRow(UserId,id);
  var relation=id==UserId?"self":RelationOf(row,UserId);
  var detail=id==UserId||p==null||p.ThongTinChiaSe==ThongTinChiaSe.CongKhai||(p.ThongTinChiaSe==ThongTinChiaSe.KhiKetNoi&&relation=="friends");
  var theirs=await FriendIds(id);var mine=id==UserId?theirs:await FriendIds(UserId);
  var name=string.IsNullOrWhiteSpace(p?.TenHienThi)?"Người dùng #"+id:p!.TenHienThi;
  return new(id,name,detail&&p?.AnhDaiDien!=null?"/mxh/anh/"+id:null,detail?p?.GioiThieu:null,detail?p?.NoiSinhSong:null,detail?p?.SoThich:null,relation,row?.Id,theirs.Count,id==UserId?0:theirs.Intersect(mine).Count(),id==UserId,Utc(account.ThoiDiemTao),detail&&p?.AnhBia!=null?"/mxh/bia/"+id:null);
 }
 public async Task<string> CoverPath(long id)
 {
  var p=await db.Set<HoSoNguoiDung>().AsNoTracking().SingleOrDefaultAsync(h=>h.MaTaiKhoan==id)??throw new BusinessException("Không có ảnh.",404);
  Require(p.AnhBia!=null&&await CanSeeDetails(id),"Không có ảnh.",404);
  return Resolve<ILuuTepService>().DuongDanAnToan(p.AnhBia!);
 }
 /// <summary>Danh sach ban be cua mot nguoi (chi khi xem duoc chi tiet ho so), toi da 60.</summary>
 public async Task<List<PersonDto>> FriendsOf(long id)
 {
  RoleRequired(Traveler);
  Require(await CanSeeDetails(id),"Hồ sơ này không chia sẻ danh sách bạn bè.",403);
  var ids=(await FriendIds(id)).Take(60).ToList();
  var profiles=await Profiles(ids);var mine=await FriendIds(UserId);
  return ids.Select(x=>new PersonDto(x,profiles[x].Name,profiles[x].HasAvatar?"/mxh/anh/"+x:null,profiles[x].Bio,x==UserId?"self":mine.Contains(x)?"friends":"none",0)).ToList();
 }
 public async Task<string> AvatarPath(long id)
 {
  var p=await db.Set<HoSoNguoiDung>().AsNoTracking().SingleOrDefaultAsync(h=>h.MaTaiKhoan==id)??throw new BusinessException("Không có ảnh.",404);
  Require(p.AnhDaiDien!=null&&await CanSeeDetails(id),"Không có ảnh.",404);
  return Resolve<ILuuTepService>().DuongDanAnToan(p.AnhDaiDien!);
 }

 /// <summary>Dong y di cung la dong y ket ban: tao quan he ban be da chap nhan neu chua co.</summary>
 public async Task EnsureFriends(long a,long b)
 {
  if(a==b)return;
  var row=await db.Set<KetBan>().SingleOrDefaultAsync(x=>(x.RequesterId==a&&x.AddresseeId==b)||(x.RequesterId==b&&x.AddresseeId==a));
  var now=Clock.BayGioUtc;
  if(row==null)db.Add(new KetBan{RequesterId=a,AddresseeId=b,Status="Accepted",CreatedAt=now,RespondedAt=now});
  else if(row.Status!="Accepted"){row.Status="Accepted";row.RespondedAt=now;}
  else return;
  await db.SaveChangesAsync();
 }
 /// <summary>Nguoi co loi ru cong khai trung diem den va giao ngay voi cac loi ru dang cong bo cua toi.</summary>
 public async Task<List<CompanionMatchDto>> CompanionMatches()
 {
  Me();
  var mine=await db.NhuCauDongHanh.AsNoTracking().Where(n=>n.MaTaiKhoan==UserId&&n.TrangThaiCongBo==TrangThaiNhuCau.DangCongBo&&n.NgayKetThuc>=HomNay).OrderBy(n=>n.NgayBatDau).Take(5).ToListAsync();
  if(mine.Count==0)return [];
  var dismissed=await db.Set<BoQuaGoiY>().AsNoTracking().Where(b=>b.AccountId==UserId).Select(b=>b.NeedId).ToListAsync();
  var asked=await db.DeNghiKetNoiDongHanh.AsNoTracking().Where(x=>x.MaNguoiGui==UserId&&(x.TrangThai==TrangThaiDeNghiKetNoi.ChoPhanHoi||x.TrangThai==TrangThaiDeNghiKetNoi.DaChapNhan)).Select(x=>x.MaNhuCau).ToListAsync();
  var found=new List<(NhuCauDongHanh Need,NhuCauDongHanh Mine)>();
  foreach(var m in mine)
  {
   var dest=m.DiemDen.Trim();if(dest.Length<2)continue;
   var rows=await Resolve<ICongBoNhuCauService>().NguoiXemThayDuoc().AsNoTracking().Where(n=>n.MaTaiKhoan!=UserId&&!asked.Contains(n.MaNhuCau)&&!dismissed.Contains(n.MaNhuCau)&&n.NgayKetThuc>=m.NgayBatDau&&n.NgayBatDau<=m.NgayKetThuc&&(n.DiemDen.Contains(dest)||dest.Contains(n.DiemDen))).OrderBy(n=>n.NgayBatDau).Take(8).ToListAsync();
   foreach(var n in rows)if(!found.Any(f=>f.Need.MaNhuCau==n.MaNhuCau))found.Add((n,m));
  }
  found=found.Take(8).ToList();
  var cards=await Cards(found.Select(f=>f.Need.MaTaiKhoan).Distinct().ToList());
  return found.Select(f=>
  {
   var overlap=Math.Min(f.Need.NgayKetThuc.DayNumber,f.Mine.NgayKetThuc.DayNumber)-Math.Max(f.Need.NgayBatDau.DayNumber,f.Mine.NgayBatDau.DayNumber)+1;
   var person=cards.First(c=>c.Id==f.Need.MaTaiKhoan);
   var reasons=new List<string>{"Cùng đi "+f.Mine.DiemDen+", trùng "+overlap+" ngày ("+f.Mine.NgayBatDau.ToString("dd/MM")+"–"+f.Mine.NgayKetThuc.ToString("dd/MM")+")"};
   if(!string.IsNullOrWhiteSpace(f.Need.HinhThucDi)&&f.Need.HinhThucDi==f.Mine.HinhThucDi)reasons.Add("Cùng phong cách đi: "+DanhMuc.Ten(f.Need.HinhThucDi));
   if(person.Mutual>0)reasons.Add(person.Mutual+" bạn chung");
   if(f.Need.NganSachDuKien!=null&&f.Mine.NganSachDuKien!=null&&Math.Abs(f.Need.NganSachDuKien.Value-f.Mine.NganSachDuKien.Value)<=0.3m*Math.Max(f.Need.NganSachDuKien.Value,f.Mine.NganSachDuKien.Value))reasons.Add("Ngân sách gần nhau");
   return new CompanionMatchDto(person,f.Need.MaNhuCau,f.Need.DiemDen,f.Need.NgayBatDau,f.Need.NgayKetThuc,f.Need.HinhThucDi,reasons[0],reasons);
  }).OrderByDescending(m=>m.Reasons!.Count).ToList();
 }

 public async Task DismissMatch(long needId)
 {
  Me();
  Require(await db.NhuCauDongHanh.AsNoTracking().AnyAsync(n=>n.MaNhuCau==needId&&n.MaTaiKhoan!=UserId),"Không tìm thấy lời rủ.",404);
  if(!await db.Set<BoQuaGoiY>().AnyAsync(b=>b.AccountId==UserId&&b.NeedId==needId)){db.Add(new BoQuaGoiY{AccountId=UserId,NeedId=needId,CreatedAt=Clock.BayGioUtc});await db.SaveChangesAsync();}
 }

 // ------------------------------------------------------------------ hanh dong

 public async Task<string> Send(long other)
 {
  Me();
  Require(other!=UserId,"Bạn không thể kết bạn với chính mình.",422);
  Require(await ActiveTravelers().AnyAsync(a=>a.MaTaiKhoan==other),"Không tìm thấy người dùng.",404);
  var row=await db.Set<KetBan>().SingleOrDefaultAsync(x=>(x.RequesterId==UserId&&x.AddresseeId==other)||(x.RequesterId==other&&x.AddresseeId==UserId));
  var now=Clock.BayGioUtc;
  if(row==null){db.Add(new KetBan{RequesterId=UserId,AddresseeId=other,Status="Pending",CreatedAt=now});}
  else if(row.Status=="Accepted")throw new BusinessException("Hai bạn đã là bạn bè.",409);
  else if(row.Status=="Pending")
  {
   Require(row.RequesterId!=UserId,"Bạn đã gửi lời mời rồi.",409);
   row.Status="Accepted";row.RespondedAt=now;await db.SaveChangesAsync();Notify(other,"Đã đồng ý kết bạn với bạn.");await db.SaveChangesAsync();return "friends";
  }
  else
  {
   Require(row.AddresseeId==UserId||(row.RespondedAt!=null&&now-row.RespondedAt.Value>TimeSpan.FromDays(30)),"Lời mời trước đó đã bị từ chối. Hãy thử lại sau.",409);
   row.RequesterId=UserId;row.AddresseeId=other;row.Status="Pending";row.CreatedAt=now;row.RespondedAt=null;
  }
  await db.SaveChangesAsync();Notify(other,"Gửi cho bạn lời mời kết bạn.");await db.SaveChangesAsync();return "sent";
 }
 void Notify(long to,string text)=>notifications.Tao(to,"KetBan",text,"/ban-be?tab=loi-moi");
 async Task<KetBan> Pending(long id)=>await db.Set<KetBan>().SingleOrDefaultAsync(x=>x.Id==id&&x.Status=="Pending"&&(x.RequesterId==UserId||x.AddresseeId==UserId))??throw new BusinessException("Không tìm thấy lời mời.",404);
 public async Task Respond(long requestId,bool accept)
 {
  Me();var row=await Pending(requestId);
  Require(row.AddresseeId==UserId,"Chỉ người nhận mới phản hồi được lời mời.",403);
  row.Status=accept?"Accepted":"Declined";row.RespondedAt=Clock.BayGioUtc;await db.SaveChangesAsync();
  if(accept){Notify(row.RequesterId,"Đã chấp nhận lời mời kết bạn của bạn.");await db.SaveChangesAsync();}
 }
 public async Task Cancel(long requestId)
 {
  Me();var row=await Pending(requestId);
  Require(row.RequesterId==UserId,"Chỉ người gửi mới hủy được lời mời.",403);
  db.Remove(row);await db.SaveChangesAsync();
 }
 public async Task Unfriend(long other)
 {
  Me();var row=await PairRow(UserId,other);
  Require(row is {Status:"Accepted"},"Hai bạn chưa là bạn bè.",404);
  db.Remove(row!);await db.SaveChangesAsync();
 }
}
