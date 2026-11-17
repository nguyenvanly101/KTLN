using System.Text.Json;
using System.Text.RegularExpressions;
using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

/// <summary>Tin nhan truc tiep khach &lt;-&gt; co so. Hop thu thuoc co so (nha cung cap tra loi); khach chi thay cuoc tro chuyen cua minh.</summary>
public sealed partial class ChatService(AppDbContext context,IHttpContextAccessor accessor,IWebHostEnvironment environment,IConfiguration configuration,IServiceProvider provider,BranchService branches,ChatBroker broker)
 : NghiepVuService(context,accessor,environment,configuration,provider)
{
 public const int MaxBody=2000,PerMinute=20;
 static readonly string[] Waits=["Minutes","Hour","Day"];
 [GeneratedRegex(@"https?://|www\.",RegexOptions.IgnoreCase)] private static partial Regex Link();

 public static string EtaLabel(string w)=>w switch{"Minutes"=>"Thường trả lời trong vài phút","Day"=>"Thường trả lời trong ngày",_=>"Thường trả lời trong vòng 1 giờ"};
 static decimal? Coord(string? v)=>decimal.TryParse((v??"").Trim().Replace(',','.'),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var d)?d:null;
 static DateTime Utc(DateTime d)=>DateTime.SpecifyKind(d,DateTimeKind.Utc);
 static string Cut(string s,int n)=>s.Length<=n?s:s[..(n-1)]+"…";
 static string Preview(string body,bool image)=>string.IsNullOrWhiteSpace(body)?(image?"[Ảnh]":""):Cut(body.Replace('\n',' ').Trim(),110);
 static string Json(object o)=>JsonSerializer.Serialize(o);

 // ------------------------------------------------------------------ truy cap
 async Task<(CuocTroChuyen C,CoSoDichVu? B,NhaCungCapDichVu? P,bool Traveler)> Open(long id,bool track=false)
 {
  RoleRequired(Traveler,ProviderRole);
  var q=track?db.Set<CuocTroChuyen>():db.Set<CuocTroChuyen>().AsNoTracking();
  var c=await q.SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("Không tìm thấy cuộc trò chuyện.",404);
  if(c.IsGroup)
  {
   Require(await db.Set<ThanhVienChat>().AnyAsync(m=>m.ConversationId==c.Id&&m.AccountId==UserId&&m.LeftAt==null),"Không tìm thấy cuộc trò chuyện.",404);
   return (c,null,null,true);
  }
  CoSoDichVu? b=null;NhaCungCapDichVu? p=null;
  if(c.BranchId is long bid)
  {
   b=await db.Set<CoSoDichVu>().AsNoTracking().SingleAsync(x=>x.Id==bid);
   p=await db.NhaCungCapDichVu.AsNoTracking().SingleAsync(x=>x.MaNhaCungCap==b.ProviderId);
  }
  var side1=c.TravelerId==UserId;
  Require(side1||c.PeerId==UserId||(p!=null&&p.MaTaiKhoan==UserId),"Không tìm thấy cuộc trò chuyện.",404);
  return (c,b,p,side1);
 }
 /// <summary>Tai khoan o phia con lai (chu co so hoac nguoi duoc nhan tin).</summary>
 async Task<long> SecondAccount(CuocTroChuyen c)=>c.PeerId??await OwnerAccount(c.BranchId!.Value);
 /// <summary>Khong cho gui khi bi chan hoac (voi khach) co so dang ngung nhan tin.</summary>
 async Task RequireWritable(CuocTroChuyen c,bool side1)
 {
  Require(c.BlockedById==null,"Không thể gửi tin nhắn trong cuộc trò chuyện này.",409);
  if(side1&&c.BranchId is long b)await RequireAccepting(b,false);
 }
 async Task<Dictionary<long,string>> friendsNames(IEnumerable<long> ids)=>await Resolve<FriendService>().Names(ids);
 async Task<string> TravelerName(long account)
 {
  var n=await db.Set<HoSoNguoiDung>().AsNoTracking().Where(x=>x.MaTaiKhoan==account).Select(x=>x.TenHienThi).FirstOrDefaultAsync();
  return string.IsNullOrWhiteSpace(n)?"Người dùng #"+account:n;
 }
 async Task<CaiDatChatCoSo> SettingsOf(long branchId)=>await db.Set<CaiDatChatCoSo>().AsNoTracking().SingleOrDefaultAsync(x=>x.BranchId==branchId)??new CaiDatChatCoSo{BranchId=branchId};

 // ------------------------------------------------------------------ khach bat dau
 /// <summary>Thong tin de mo khung nhan tin tu trang co so. Cuoc tro chuyen chi duoc tao khi khach gui tin dau tien.</summary>
 public async Task<ChatStartDto> StartInfo(long branchId)
 {
  RoleRequired(Traveler,ProviderRole);
  var b=await branches.PublicBranches().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==branchId)??throw new BusinessException("Không tìm thấy cơ sở.",404);
  var owner=await db.NhaCungCapDichVu.AsNoTracking().Where(p=>p.MaNhaCungCap==b.ProviderId).Select(p=>p.MaTaiKhoan).SingleAsync();
  var s=await SettingsOf(branchId);
  long? existing=Role==Traveler?await db.Set<CuocTroChuyen>().AsNoTracking().Where(x=>x.BranchId==branchId&&x.TravelerId==UserId).Select(x=>(long?)x.Id).FirstOrDefaultAsync():null;
  return new(b.Id,b.Name,b.Type,existing,EtaLabel(s.ReplyWithin),await Actual(branchId),s.Greeting,s.Accepting,owner==UserId);
 }

 /// <summary>Mo khung nhan tin voi mot nguoi (vi du nguoi dang loi ru di cung). Chi gui duoc tin khi doi phuong dang co loi ru cong khai hoac da co cuoc tro chuyen.</summary>
 public async Task<ChatStartDto> StartPeer(long accountId)
 {
  RoleRequired(Traveler);
  Require(accountId!=UserId,"Bạn không thể nhắn tin cho chính mình.",422);
  Require(await db.TaiKhoan.AsNoTracking().AnyAsync(a=>a.MaTaiKhoan==accountId&&a.VaiTro==VaiTro.KHACH_DU_LICH&&a.TrangThai==TrangThaiTaiKhoan.HoatDong),"Không tìm thấy người này.",404);
  var existing=await db.Set<CuocTroChuyen>().AsNoTracking().Where(x=>x.BranchId==null&&((x.TravelerId==UserId&&x.PeerId==accountId)||(x.TravelerId==accountId&&x.PeerId==UserId))).Select(x=>(long?)x.Id).FirstOrDefaultAsync();
  if(existing==null)await RequirePeerReachable(accountId);
  return new(0,"","",existing,"",null,null,true,false,accountId,await TravelerName(accountId));
 }
 async Task RequirePeerReachable(long accountId)
 {
  Require(await Resolve<FriendService>().AreFriends(UserId,accountId)||await db.NhuCauDongHanh.AsNoTracking().AnyAsync(n=>n.MaTaiKhoan==accountId&&n.TrangThaiCongBo==TrangThaiNhuCau.DangCongBo)
   ||await db.Set<BaiViet>().AsNoTracking().AnyAsync(p=>p.AuthorId==accountId&&p.Kind=="Companion"&&p.Visibility=="Public"&&p.DeletedAt==null&&p.TripEnd>=TravelService.Today),"Chỉ nhắn tin được với bạn bè hoặc người đang có bài tìm bạn đồng hành công khai.",403);
 }

 /// <summary>Tim nguoi hoac co so de bat dau tro chuyen: chi tra ve nguoi nhan tin duoc (ban be / co loi ru cong khai / da tung nhan tin).</summary>
 public async Task<ChatFindResult> Find(string? q)
 {
  RoleRequired(Traveler);
  var term=(q??"").Trim();Require(term.Length>=2,"Nhập ít nhất 2 ký tự để tìm.",422);
  var found=await Resolve<FriendService>().Search(term);
  var ids=found.Select(p=>p.Id).ToList();
  var withNeed=(await db.NhuCauDongHanh.AsNoTracking().Where(n=>ids.Contains(n.MaTaiKhoan)&&n.TrangThaiCongBo==TrangThaiNhuCau.DangCongBo).Select(n=>n.MaTaiKhoan).Distinct().ToListAsync()).ToHashSet();
  withNeed.UnionWith(await db.Set<BaiViet>().AsNoTracking().Where(p=>ids.Contains(p.AuthorId)&&p.Kind=="Companion"&&p.Visibility=="Public"&&p.DeletedAt==null&&p.TripEnd>=TravelService.Today).Select(p=>p.AuthorId).Distinct().ToListAsync());
  var talked=(await db.Set<CuocTroChuyen>().AsNoTracking().Where(x=>x.BranchId==null&&!x.IsGroup&&(x.TravelerId==UserId||x.PeerId==UserId)).Select(x=>x.TravelerId==UserId?x.PeerId!.Value:x.TravelerId).ToListAsync()).ToHashSet();
  var people=found.Where(p=>p.Relation=="friends"||withNeed.Contains(p.Id)||talked.Contains(p.Id)).Take(8).Select(p=>new PersonRef(p.Id,p.Name,p.Relation)).ToList();
  var shops=await branches.PublicBranches().AsNoTracking().Where(b=>b.Name.Contains(term)).OrderBy(b=>b.Name).Take(8).Select(b=>new ChatFindBranch(b.Id,b.Name,b.Type,b.Address)).ToListAsync();
  return new(people,shops);
 }

 /// <summary>Thong tin hien tren trang co so cho moi nguoi xem (khong can dang nhap).</summary>
 public async Task<(string Eta,string? Actual,bool Accepting)> PublicInfo(long branchId)
 {
  var s=await SettingsOf(branchId);return (EtaLabel(s.ReplyWithin),await Actual(branchId),s.Accepting);
 }

 // ------------------------------------------------------------------ gui tin
 public async Task<(ChatMessageDto Message,long ConversationId)> Send(ChatSendInput i)
 {
  RoleRequired(Traveler,ProviderRole);
  var body=(i.Body??"").Replace("\r\n","\n").Trim(); var lat=Coord(i.Latitude);var lng=Coord(i.Longitude);
  Require(body.Length<=MaxBody,"Tin nhắn tối đa "+MaxBody+" ký tự.",400);
  Require(body.Length>0||i.Image!=null||i.File!=null||lat!=null,"Hãy nhập nội dung, chọn ảnh, tệp hoặc vị trí.",422);
  Require((i.Image!=null?1:0)+(i.File!=null?1:0)+(lat!=null?1:0)<=1,"Mỗi tin chỉ gửi một loại: ảnh, tệp hoặc vị trí.",422);
  if(i.File!=null)Require(Path.GetExtension(i.File.FileName).Equals(".pdf",StringComparison.OrdinalIgnoreCase)&&i.File.Length is >0 and <=5*1024*1024,"Chỉ gửi tệp PDF, tối đa 5 MB.",422);
  if(!string.IsNullOrWhiteSpace(i.Latitude)||!string.IsNullOrWhiteSpace(i.Longitude))Require(lat is >=-90 and <=90&&lng is >=-180 and <=180,"Vị trí không hợp lệ.",422);
  if(i.Image!=null)Require(Path.GetExtension(i.Image.FileName).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png"&&i.Image.Length is > 0 and <= 5*1024*1024,"Chỉ gửi ảnh JPG hoặc PNG, tối đa 5 MB.",422);
  var since=Clock.BayGioUtc.AddMinutes(-1);
  Require(await db.Set<TinNhanChat>().CountAsync(m=>m.SenderId==UserId&&m.CreatedAt>since)<PerMinute,"Bạn gửi quá nhanh. Hãy thử lại sau ít phút.",429);

  CuocTroChuyen conv;bool side1;
  if(i.ConversationId is long cid)
  {
   var opened=await Open(cid,true);conv=opened.C;side1=opened.Traveler;
   await RequireWritable(conv,side1);
  }
  else if(i.PeerId is long peer)
  {
   Require(Role==Traveler,"Chỉ khách du lịch mới bắt đầu cuộc trò chuyện này.",403);
   conv=await GetOrCreatePeer(peer);side1=conv.TravelerId==UserId;
   await RequireWritable(conv,side1);
  }
  else
  {
   Require(Role==Traveler,"Chỉ khách mới bắt đầu cuộc trò chuyện mới.",403);
   var branchId=i.BranchId??throw new BusinessException("Thiếu người nhận tin.",400);
   await RequireAccepting(branchId,true);
   conv=await GetOrCreate(branchId);side1=true;
  }
  var traveler=side1;
  if(i.ReplyToId is long replyId)Require(await db.Set<TinNhanChat>().AnyAsync(x=>x.Id==replyId&&x.ConversationId==conv.Id&&x.DeletedAt==null),"Không tìm thấy tin cần trả lời.",404);
  if(Role==Traveler)Require(!(body.Length>0&&Link().Matches(body).Count>2),"Tin nhắn có quá nhiều liên kết. Hãy bỏ bớt liên kết.",422);

  ChatMessageDto? sent=null,auto=null;
  await Execute(async()=>
  {
   var c=await db.Set<CuocTroChuyen>().SingleAsync(x=>x.Id==conv.Id);
   string? image=null;
   if(i.Image!=null)image=(await StoreFiles([i.Image]))[0].Path;
   string? filePath=null,fileName=null;
   if(i.File!=null){filePath=(await StoreFiles([i.File]))[0].Path;fileName=Cut(Path.GetFileName(i.File.FileName),150);}
   var now=Clock.BayGioUtc;
   var first=!await db.Set<TinNhanChat>().AnyAsync(m=>m.ConversationId==c.Id&&m.Side=="Traveler");
   var m1=new TinNhanChat{ConversationId=c.Id,SenderId=UserId,Side=c.IsGroup?"Member":traveler?"Traveler":"Branch",Kind=lat!=null?"Location":filePath!=null?"File":image!=null&&body.Length==0?"Image":"Text",Body=body,ImagePath=image,ReplyToId=i.ReplyToId,Lat=lat,Lng=lng,FileName=fileName,FilePath=filePath,CreatedAt=now};
   db.Add(m1);await db.SaveChangesAsync();
   c.LastMessageId=m1.Id;c.LastMessageAt=now;c.LastPreview=lat!=null?"[Vị trí]":filePath!=null?"[Tệp] "+fileName:Preview(body,image!=null);c.LastFromTraveler=traveler;
   if(traveler){c.State="Open";c.TravelerReadId=m1.Id;c.TravelerReadAt=now;}else{c.BranchReadId=m1.Id;c.BranchReadAt=now;}
   if(c.IsGroup){var mem=await db.Set<ThanhVienChat>().SingleAsync(x=>x.ConversationId==c.Id&&x.AccountId==UserId);mem.ReadId=m1.Id;}
   sent=new(m1.Id,m1.Side,m1.Kind,m1.Body,image!=null,Utc(now));
   if(traveler&&c.BranchId!=null&&await ShouldAutoReply(c,first))
   {
    var s=await SettingsOf(c.BranchId!.Value);
    var m2=new TinNhanChat{ConversationId=c.Id,SenderId=null,Side="Branch",Kind="Auto",Body=s.AutoReply!,CreatedAt=now.AddMilliseconds(5)};
    db.Add(m2);await db.SaveChangesAsync();
    c.LastMessageId=m2.Id;c.LastPreview=Preview(m2.Body,false);c.LastFromTraveler=false;c.BranchReadId=m2.Id;
    auto=new(m2.Id,m2.Side,m2.Kind,m2.Body,false,Utc(m2.CreatedAt));
   }
   await db.SaveChangesAsync();
  });
  var last=(auto??sent!).Id;
  if(conv.IsGroup)await NotifyGroup(conv.Id,new{t="msg",c=conv.Id,m=last});else Notify(conv.TravelerId,await SecondAccount(conv),new{t="msg",c=conv.Id,m=last});
  return (sent!,conv.Id);
 }
 async Task RequireAccepting(long branchId,bool starting)
 {
  Require(await branches.PublicBranches().AnyAsync(x=>x.Id==branchId),"Cơ sở hiện không nhận tin nhắn.",404);
  var owner=await db.Set<CoSoDichVu>().AsNoTracking().Where(b=>b.Id==branchId).Join(db.NhaCungCapDichVu,b=>b.ProviderId,p=>p.MaNhaCungCap,(b,p)=>p.MaTaiKhoan).SingleAsync();
  Require(owner!=UserId,"Bạn không thể nhắn tin cho chính cơ sở của mình.",422);
  Require((await SettingsOf(branchId)).Accepting,"Cơ sở đang tạm ngưng nhận tin nhắn. Hãy xem số điện thoại trên trang cơ sở.",409);
 }
 async Task<CuocTroChuyen> GetOrCreate(long branchId)
 {
  var found=await db.Set<CuocTroChuyen>().AsNoTracking().FirstOrDefaultAsync(x=>x.BranchId==branchId&&x.TravelerId==UserId);
  if(found!=null)return found;
  var created=new CuocTroChuyen{BranchId=branchId,TravelerId=UserId,State="Open",LastMessageAt=Clock.BayGioUtc,CreatedAt=Clock.BayGioUtc};
  db.Add(created);
  try{await db.SaveChangesAsync();return created;}
  catch(DbUpdateException)
  {
   db.Entry(created).State=EntityState.Detached;
   return await db.Set<CuocTroChuyen>().AsNoTracking().FirstAsync(x=>x.BranchId==branchId&&x.TravelerId==UserId);
  }
 }
 async Task<CuocTroChuyen> GetOrCreatePeer(long peer)
 {
  Require(peer!=UserId,"Bạn không thể nhắn tin cho chính mình.",422);
  Require(await db.TaiKhoan.AsNoTracking().AnyAsync(a=>a.MaTaiKhoan==peer&&a.VaiTro==VaiTro.KHACH_DU_LICH&&a.TrangThai==TrangThaiTaiKhoan.HoatDong),"Không tìm thấy người này.",404);
  Task<CuocTroChuyen?> Find()=>db.Set<CuocTroChuyen>().AsNoTracking().FirstOrDefaultAsync(x=>x.BranchId==null&&((x.TravelerId==UserId&&x.PeerId==peer)||(x.TravelerId==peer&&x.PeerId==UserId)));
  var found=await Find();if(found!=null)return found;
  await RequirePeerReachable(peer);
  var created=new CuocTroChuyen{BranchId=null,TravelerId=UserId,PeerId=peer,State="Open",LastMessageAt=Clock.BayGioUtc,CreatedAt=Clock.BayGioUtc};
  db.Add(created);
  try{await db.SaveChangesAsync();return created;}
  catch(DbUpdateException){db.Entry(created).State=EntityState.Detached;return (await Find())!;}
 }
 /// <summary>Tra loi tu dong: khi co noi dung, khach vua nhan tin dau tien (hoac sau 6 gio im lang) va co so dang dong cua/chua khai gio.</summary>
 async Task<bool> ShouldAutoReply(CuocTroChuyen c,bool first)
 {
  var s=await SettingsOf(c.BranchId!.Value);
  if(string.IsNullOrWhiteSpace(s.AutoReply))return false;
  if(!first)
  {
   var lastBranch=await db.Set<TinNhanChat>().AsNoTracking().Where(m=>m.ConversationId==c.Id&&m.Side=="Branch").OrderByDescending(m=>m.Id).Select(m=>(DateTime?)m.CreatedAt).FirstOrDefaultAsync();
   if(lastBranch!=null&&Clock.BayGioUtc-lastBranch.Value<TimeSpan.FromHours(6))return false;
  }
  if(!s.AutoReplyOffHours)return true;
  var b=await db.Set<CoSoDichVu>().AsNoTracking().SingleAsync(x=>x.Id==c.BranchId!.Value);
  return !OpenNow(b);
 }
 bool OpenNow(CoSoDichVu b)
 {
  if(string.IsNullOrEmpty(b.OpenDays)||b.OpenTime==null||b.CloseTime==null)return false;
  var vn=Clock.DoiSangGioVN(Clock.BayGioUtc);var weekday=((int)vn.DayOfWeek+6)%7+1;var time=TimeOnly.FromDateTime(vn);
  var today=b.OpenDays.Contains(weekday.ToString());var yesterday=b.OpenDays.Contains((weekday==1?7:weekday-1).ToString());
  return b.OpenTime<=b.CloseTime?today&&time>=b.OpenTime&&time<b.CloseTime:(today&&time>=b.OpenTime)||(yesterday&&time<b.CloseTime);
 }
 void Notify(long traveler,long owner,object payload){var j=Json(payload);broker.Publish(traveler,j);broker.Publish(owner,j);}

 // ------------------------------------------------------------------ doc
 public async Task<List<ChatItemDto>> List(long? branchId=null,string? filter=null,string? q=null)
 {
  RoleRequired(Traveler,ProviderRole);
  long? providerId=Role==ProviderRole?(await MyProvider()).MaNhaCungCap:null;
  IQueryable<CuocTroChuyen> query=db.Set<CuocTroChuyen>().AsNoTracking().Where(c=>(!c.IsGroup&&(c.TravelerId==UserId||c.PeerId==UserId))||(c.IsGroup&&db.Set<ThanhVienChat>().Any(t=>t.ConversationId==c.Id&&t.AccountId==UserId&&t.LeftAt==null))||(providerId!=null&&c.BranchId!=null&&db.Set<CoSoDichVu>().Any(b=>b.Id==c.BranchId&&b.ProviderId==providerId)));
  if(branchId!=null)query=query.Where(c=>c.BranchId==branchId);
  if(filter=="done")query=query.Where(c=>c.State=="Done");else if(filter=="open")query=query.Where(c=>c.State=="Open");
  var rows=await query.Where(c=>c.LastMessageId>0).OrderByDescending(c=>c.LastMessageAt).Take(100).ToListAsync();
  var branchIds=rows.Where(r=>r.BranchId!=null).Select(r=>r.BranchId!.Value).Distinct().ToList();
  var names=await db.Set<CoSoDichVu>().AsNoTracking().Where(b=>branchIds.Contains(b.Id)).Select(b=>new{b.Id,b.Name}).ToDictionaryAsync(x=>x.Id,x=>x.Name);
  var ids=rows.Select(r=>r.Id).ToList();
  var unreadOneToOne=await (from m in db.Set<TinNhanChat>().AsNoTracking() join c in db.Set<CuocTroChuyen>().AsNoTracking() on m.ConversationId equals c.Id
   where ids.Contains(c.Id)&&!c.IsGroup group new{m,c} by c.Id into g
   select new{Id=g.Key,First=g.Count(x=>x.m.Id>x.c.TravelerReadId&&x.m.Side!="Traveler"),Second=g.Count(x=>x.m.Id>x.c.BranchReadId&&x.m.Side!="Branch")}).ToDictionaryAsync(x=>x.Id);
  var unreadGroups=await (from m in db.Set<TinNhanChat>().AsNoTracking() join t in db.Set<ThanhVienChat>().AsNoTracking() on m.ConversationId equals t.ConversationId
   where ids.Contains(t.ConversationId)&&t.AccountId==UserId&&t.LeftAt==null group new{m,t} by t.ConversationId into g
   select new{Id=g.Key,N=g.Count(x=>x.m.Id>x.t.ReadId&&x.m.SenderId!=UserId)}).ToDictionaryAsync(x=>x.Id,x=>x.N);
  var pendingRows=await (from m in db.Set<TinNhanChat>().AsNoTracking() join y in db.YeuCauDatDichVu.AsNoTracking() on m.BookingRequestId equals y.MaYeuCau
   where ids.Contains(m.ConversationId)&&m.Kind=="Booking"&&(y.TrangThai==TrangThaiYeuCau.ChoPhanHoi||y.TrangThai==TrangThaiYeuCau.DeXuatKhac) select new{m.ConversationId,y.TrangThai}).ToListAsync();
  var groupRows=await db.Set<ThanhVienChat>().AsNoTracking().Where(t=>ids.Contains(t.ConversationId)).Select(t=>new{t.ConversationId,t.AccountId,t.ReadId,t.LeftAt}).ToListAsync();
  var people=await TravelerNames(rows.SelectMany(r=>new long?[]{r.TravelerId,r.PeerId}).Where(x=>x!=null).Select(x=>x!.Value).Distinct().ToList());
  var result=new List<ChatItemDto>();
  foreach(var c in rows)
  {
   if(c.IsGroup)
   {
    var myRead=groupRows.FirstOrDefault(t=>t.ConversationId==c.Id&&t.AccountId==UserId)?.ReadId??0;
    result.Add(new ChatItemDto(c.Id,0,"",c.Title??"Nhóm",c.LastPreview,false,Utc(c.LastMessageAt),unreadGroups.GetValueOrDefault(c.Id),c.State,c.Label,true,groupRows.Count(t=>t.ConversationId==c.Id&&t.LeftAt==null)));
    continue;
   }
   var side1=c.TravelerId==UserId;
   var unread=unreadOneToOne.TryGetValue(c.Id,out var u)?(side1?u.First:u.Second):0;
   var branchName=c.BranchId is long bid?names.GetValueOrDefault(bid,"Cơ sở"):"";
   var who=side1?(c.BranchId!=null?branchName:people.GetValueOrDefault(c.PeerId!.Value,"Người dùng")):people.GetValueOrDefault(c.TravelerId,"Khách");
   var awaiting=side1?TrangThaiYeuCau.DeXuatKhac:TrangThaiYeuCau.ChoPhanHoi;
   result.Add(new ChatItemDto(c.Id,c.BranchId??0,branchName,who,c.LastPreview,side1==c.LastFromTraveler,Utc(c.LastMessageAt),unread,c.State,c.Label,false,0,pendingRows.Count(p=>p.ConversationId==c.Id&&p.TrangThai==awaiting)));
  }
  if(filter=="unread")result=result.Where(x=>x.Unread>0).ToList();
  if(filter=="booking")result=result.Where(x=>x.Pending>0).ToList();
  if(!string.IsNullOrWhiteSpace(q)){var t=q.Trim();result=result.Where(x=>x.Who.Contains(t,StringComparison.OrdinalIgnoreCase)||(x.Preview??"").Contains(t,StringComparison.OrdinalIgnoreCase)).ToList();}
  return result;
 }
 async Task<Dictionary<long,string>> TravelerNames(List<long> accounts)
 {
  var rows=await db.Set<HoSoNguoiDung>().AsNoTracking().Where(h=>accounts.Contains(h.MaTaiKhoan)).Select(h=>new{h.MaTaiKhoan,h.TenHienThi}).ToListAsync();
  return accounts.ToDictionary(a=>a,a=>rows.FirstOrDefault(r=>r.MaTaiKhoan==a)?.TenHienThi is {Length:>0} n?n:"Người dùng #"+a);
 }
 public async Task<ChatThreadDto> Thread(long id)
 {
  var (c,b,p,traveler)=await Open(id);
  if(c.IsGroup)return await GroupThread(c);
  var s=b!=null?await SettingsOf(b.Id):new CaiDatChatCoSo();
  var messages=await Messages(id,0,60);
  var name=traveler?(b?.Name??await TravelerName(c.PeerId!.Value)):await TravelerName(c.TravelerId);
  var read=traveler?c.TravelerReadId:c.BranchReadId;
  var unread=messages.Count(m=>m.Id>read&&m.Side!=(traveler?"Traveler":"Branch"));
  var item=new ChatItemDto(c.Id,b?.Id??0,b?.Name??"",name,c.LastPreview,traveler==c.LastFromTraveler,Utc(c.LastMessageAt),unread,c.State,c.Label);
  string? blocked=null;
  if(c.BlockedById!=null)blocked=c.BlockedById==UserId?"Bạn đã chặn người này. Bỏ chặn để nhắn tin lại.":"Bạn không thể nhắn tin trong cuộc trò chuyện này.";
  else if(traveler&&b!=null){if(!await branches.PublicBranches().AnyAsync(x=>x.Id==b.Id))blocked="Cơ sở hiện không còn công bố nên không nhận thêm tin nhắn.";else if(!s.Accepting)blocked="Cơ sở đang tạm ngưng nhận tin nhắn.";}
  var otherAccount=traveler?(p!=null?p.MaTaiKhoan:c.PeerId!.Value):c.TravelerId;
  var context=b!=null?BranchCard(b):null;
  return new(item,messages,await Cards(c.Id),await ConnectionCards(c.Id),traveler?c.BranchReadId:c.TravelerReadId,traveler?"Traveler":"Branch",broker.IsOnline(otherAccount),b!=null?EtaLabel(s.ReplyWithin):"",traveler&&b!=null?await Actual(b.Id):null,blocked==null,blocked,!traveler&&b!=null?c.InternalNote:null,b?.Type??"",traveler?b?.Phone:null,c.BlockedById==UserId,b==null?otherAccount:null,null,null,context,await AgreementCards(c.Id));
 }
 /// <summary>Tin nhan sau moc <paramref name="after"/> (tang dan); khi after = 0 lay <paramref name="limit"/> tin moi nhat.</summary>
 public async Task<List<ChatMessageDto>> Messages(long id,long after,int limit=60,long before=0)
 {
  var (cv,_,_,side1)=await Open(id);
  limit=Math.Clamp(limit,1,100);
  var q=db.Set<TinNhanChat>().AsNoTracking().Where(m=>m.ConversationId==id);
  List<TinNhanChat> rows;
  if(after>0)rows=await q.Where(m=>m.Id>after).OrderBy(m=>m.Id).Take(limit).ToListAsync();
  else{if(before>0)q=q.Where(m=>m.Id<before);rows=await q.OrderByDescending(m=>m.Id).Take(limit).ToListAsync();rows.Reverse();}
  return await ToDtos(cv,side1,rows);
 }

 static ChatBranchCard BranchCard(CoSoDichVu b)
 {
  string? hours=null;
  if(!string.IsNullOrEmpty(b.OpenDays)&&b.OpenTime!=null&&b.CloseTime!=null)
  {
   var d=b.OpenDays.Length==7?"Mỗi ngày":string.Join(", ",b.OpenDays.Select(x=>x=='7'?"CN":"T"+(int.Parse(x.ToString())+1)));
   hours=d+" · "+b.OpenTime.Value.ToString("HH:mm")+"–"+b.CloseTime.Value.ToString("HH:mm");
  }
  else if(!string.IsNullOrWhiteSpace(b.Hours))hours=b.Hours;
  string? price=null;
  if(b.PriceFrom!=null)price=(b.PriceTo!=null&&b.PriceTo!=b.PriceFrom?Labels.Money(b.PriceFrom)+" – "+Labels.Money(b.PriceTo):"từ "+Labels.Money(b.PriceFrom))+(string.IsNullOrWhiteSpace(b.PriceUnit)?"":" / "+b.PriceUnit);
  var photo=Ui.ServicePhotos(b.PhotosJson).Length>0?"/co-so/"+b.Id+"/anh/0":null;
  return new(b.Id,b.Name,b.Type,b.Address,hours,price,photo,"/co-so/"+b.Id,b.Phone);
 }
 /// <summary>Tam tinh theo gia tham khao cua dich vu: theo nguoi, theo dem/ngay (khi co ngay ket thuc) va so luong.</summary>
 static decimal? Estimate(decimal? price,string? unit,int people,int quantity,DateOnly start,DateOnly? end)
 {
  if(price==null)return null;
  var u=(unit??"").ToLowerInvariant();var n=(decimal)Math.Max(1,quantity);
  if(u.Contains("người")||u.Contains("khách"))n*=Math.Max(1,people);
  if((u.Contains("đêm")||u.Contains("ngày"))&&end!=null)n*=Math.Max(1,end.Value.DayNumber-start.DayNumber);
  return price*n;
 }
 /// <summary>Kiem tra truoc khi gui yeu cau dat: canh bao lich phuc vu / tam ngung va tam tinh chi phi.</summary>
 public async Task<ChatBookingCheck> BookingCheck(ChatBookingInput i)
 {
  RoleRequired(Traveler);
  long branchId;
  if(i.ConversationId is long cid){var o=await Open(cid);Require(o.Traveler&&o.C.BranchId!=null,"Chỉ khách kiểm tra yêu cầu đặt tới cơ sở.",403);branchId=o.C.BranchId!.Value;}
  else branchId=i.BranchId??throw new BusinessException("Thiếu cơ sở.",400);
  Require(await db.Set<DichVuCoSo>().AnyAsync(x=>x.ServiceId==i.ServiceId&&x.BranchId==branchId),"Dịch vụ này không thuộc cơ sở đang trò chuyện.",422);
  var service=await db.DichVuDuLich.AsNoTracking().Where(s=>s.MaDichVu==i.ServiceId).Select(s=>new{s.GiaThamKhao,s.DonViGia}).SingleAsync();
  var estimate=Estimate(service.GiaThamKhao,service.DonViGia,i.People,i.Quantity,i.Start,i.End);
  try
  {
   var warnings=await Resolve<TravelService>().CheckBooking(new BookingInput{ServiceId=i.ServiceId,BranchId=branchId,Start=i.Start,End=i.End,People=i.People,Quantity=i.Quantity,SubmissionId=Guid.NewGuid()});
   return new(true,null,warnings.Where(w=>!w.StartsWith("Gửi yêu cầu không bảo đảm")).ToList(),service.GiaThamKhao,service.DonViGia,estimate,"Tạm tính theo giá tham khảo, cơ sở sẽ xác nhận giá cuối cùng trong cuộc trò chuyện.");
  }
  catch(BusinessException e){return new(false,e.Message,[],service.GiaThamKhao,service.DonViGia,estimate,null);}
 }
 /// <summary>Cuoc tro chuyen chua yeu cau dat nay (chi hai ben cua cuoc tro chuyen).</summary>
 public async Task<long?> ConversationOfBooking(long requestId)
 {
  if(UserId==0||Role is not (Traveler or ProviderRole))return null;
  var cid=await db.Set<TinNhanChat>().AsNoTracking().Where(m=>m.BookingRequestId==requestId&&m.Kind=="Booking").Select(m=>(long?)m.ConversationId).FirstOrDefaultAsync();
  if(cid==null)return null;
  try{await Open(cid.Value);return cid;}catch(BusinessException){return null;}
 }

 // ------------------------------------------------------------------ dat dich vu ngay trong chat
 /// <summary>Co so nhan tin dat cho mot dich vu: co so duoc chon (neu hop le) hoac co so dau tien dang cong bo; null neu dich vu chua gan co so.</summary>
 public async Task<long?> BranchForService(long serviceId,long? preferred)
 {
  RoleRequired(Traveler);
  var linked=await db.Set<DichVuCoSo>().AsNoTracking().Where(x=>x.ServiceId==serviceId).Select(x=>x.BranchId).ToListAsync();
  var open=await branches.PublicBranches().AsNoTracking().Where(b=>linked.Contains(b.Id)).OrderBy(b=>b.Name).Select(b=>b.Id).ToListAsync();
  if(open.Count==0)return null;
  return preferred!=null&&open.Contains(preferred.Value)?preferred:open[0];
 }
 /// <summary>Dich vu co the dat cua mot co so (de khach chon trong khung chat).</summary>
 public async Task<List<ChatServiceDto>> Services(long branchId)
 {
  RoleRequired(Traveler,ProviderRole);
  Require(await branches.PublicBranches().AnyAsync(b=>b.Id==branchId),"Không tìm thấy cơ sở.",404);
  var ids=db.Set<DichVuCoSo>().Where(x=>x.BranchId==branchId).Select(x=>x.ServiceId);
  return await Resolve<TravelService>().PublicServices().AsNoTracking().Where(s=>ids.Contains(s.MaDichVu)&&s.TrangThaiTiepNhan==TrangThaiTiepNhan.TiepNhan).OrderBy(s=>s.TenDichVu)
   .Select(s=>new ChatServiceDto(s.MaDichVu,s.TenDichVu,s.LoaiDichVu,s.GiaThamKhao,s.DonViGia)).ToListAsync();
 }
 async Task CheckRate()
 {
  var since=Clock.BayGioUtc.AddMinutes(-1);
  Require(await db.Set<TinNhanChat>().CountAsync(m=>m.SenderId==UserId&&m.CreatedAt>since)<PerMinute,"Bạn gửi quá nhanh. Hãy thử lại sau ít phút.",429);
 }
 /// <summary>Them mot tin the/su kien vao cuoc tro chuyen va cap nhat dau cuoc tro chuyen. Goi trong Execute.</summary>
 async Task<ChatMessageDto> AppendCard(long conversationId,bool traveler,string kind,string body,long? requestId,long? connectionId=null,long? agreementId=null)
 {
  var c=await db.Set<CuocTroChuyen>().SingleAsync(x=>x.Id==conversationId);var now=Clock.BayGioUtc;
  var m=new TinNhanChat{ConversationId=c.Id,SenderId=UserId,Side=traveler?"Traveler":"Branch",Kind=kind,Body=Cut(body,MaxBody),BookingRequestId=requestId,ConnectionId=connectionId,AgreementId=agreementId,CreatedAt=now};
  db.Add(m);await db.SaveChangesAsync();
  c.LastMessageId=m.Id;c.LastMessageAt=now;c.LastPreview=Cut((agreementId!=null?(kind=="Agreement"?"[Thỏa thuận đi cùng] ":"[Đi cùng] "):connectionId!=null?(kind=="Connection"?"[Lời rủ đi cùng] ":"[Đi cùng] "):kind=="Booking"?"[Yêu cầu đặt] ":"[Đặt dịch vụ] ")+body.Replace('\n',' '),110);c.LastFromTraveler=traveler;
  if(traveler){c.State="Open";c.TravelerReadId=m.Id;c.TravelerReadAt=now;}else{c.BranchReadId=m.Id;c.BranchReadAt=now;}
  await db.SaveChangesAsync();
  return new(m.Id,m.Side,m.Kind,m.Body,false,Utc(now),requestId,connectionId,null,null,null,null,null,null,false,false,null,null,null,false,null,agreementId);
 }
 public async Task<(ChatMessageDto Message,long ConversationId)> SendBooking(ChatBookingInput i)
 {
  RoleRequired(Traveler);await CheckRate();
  CuocTroChuyen conv;
  if(i.ConversationId is long cid){var opened=await Open(cid,true);Require(opened.Traveler&&opened.C.BranchId!=null,"Chỉ khách gửi yêu cầu đặt tới cơ sở.",403);conv=opened.C;await RequireWritable(conv,true);}
  else
  {
   var branchId=i.BranchId??throw new BusinessException("Thiếu cơ sở nhận yêu cầu.",400);
   await RequireAccepting(branchId,true);conv=await GetOrCreate(branchId);
  }
  Require(await db.Set<DichVuCoSo>().AnyAsync(x=>x.ServiceId==i.ServiceId&&x.BranchId==conv.BranchId!.Value),"Dịch vụ này không thuộc cơ sở đang trò chuyện.",422);
  var svc=await db.DichVuDuLich.AsNoTracking().Where(x=>x.MaDichVu==i.ServiceId).Select(x=>new{x.GiaThamKhao,x.DonViGia}).SingleAsync();
  var input=new BookingInput{Cost=Estimate(svc.GiaThamKhao,svc.DonViGia,i.People,i.Quantity,i.Start,i.End),ServiceId=i.ServiceId,BranchId=conv.BranchId!.Value,Start=i.Start,End=i.End,People=i.People,Quantity=i.Quantity,Notes=string.IsNullOrWhiteSpace(i.Notes)?null:i.Notes.Trim(),ArrivalDeparture=string.IsNullOrWhiteSpace(i.ArrivalDeparture)?null:i.ArrivalDeparture.Trim(),SubmissionId=i.SubmissionId==Guid.Empty?Guid.NewGuid():i.SubmissionId};
  var booking=Resolve<IDatDichVuService>();
  await Resolve<TravelService>().CheckBooking(input);
  var requestId=await booking.GuiYeuCauAsync(input);
  var existing=await db.Set<TinNhanChat>().AsNoTracking().FirstOrDefaultAsync(m=>m.ConversationId==conv.Id&&m.BookingRequestId==requestId&&m.Kind=="Booking");
  if(existing!=null)return (new(existing.Id,existing.Side,existing.Kind,existing.Body,false,Utc(existing.CreatedAt),requestId),conv.Id);
  var service=await db.DichVuDuLich.AsNoTracking().Where(s=>s.MaDichVu==i.ServiceId).Select(s=>s.TenDichVu).SingleAsync();
  var text=service+" · "+i.Start.ToString("dd/MM/yyyy")+(i.End!=null?" – "+i.End.Value.ToString("dd/MM/yyyy"):"")+" · "+i.People+" người · SL "+i.Quantity+(input.Notes!=null?"\n"+input.Notes:"");
  ChatMessageDto? msg=null;
  await Execute(async()=>{msg=await AppendCard(conv.Id,true,"Booking",text,requestId);});
  Notify(conv.TravelerId,await SecondAccount(conv),new{t="msg",c=conv.Id,m=msg!.Id});
  return (msg!,conv.Id);
 }
 async Task<long> OwnerAccount(long branchId)=>await db.Set<CoSoDichVu>().AsNoTracking().Where(b=>b.Id==branchId).Join(db.NhaCungCapDichVu,b=>b.ProviderId,p=>p.MaNhaCungCap,(b,p)=>p.MaTaiKhoan).SingleAsync();
 /// <summary>The yeu cau dat (trang thai hien tai + hanh dong moi ben duoc phep) cua cuoc tro chuyen.</summary>
 public async Task<List<ChatBookingCard>> Cards(long conversationId)
 {
  var (c,_,_,traveler)=await Open(conversationId);
  var ids=await db.Set<TinNhanChat>().AsNoTracking().Where(m=>m.ConversationId==c.Id&&m.Kind=="Booking"&&m.BookingRequestId!=null).Select(m=>m.BookingRequestId!.Value).Distinct().ToListAsync();
  if(ids.Count==0)return [];
  var rows=await (from y in db.YeuCauDatDichVu.AsNoTracking() join s in db.DichVuDuLich.AsNoTracking() on y.MaDichVu equals s.MaDichVu where ids.Contains(y.MaYeuCau) orderby y.MaYeuCau select new{y,s.TenDichVu}).ToListAsync();
  var deposits=await db.Set<DatCocChat>().AsNoTracking().Where(d=>ids.Contains(d.RequestId)).ToListAsync();
  var today=HomNay;
  return rows.Select(r=>
  {
   var y=r.y;var actions=new List<string>();
   if(traveler){if(y.TrangThai==TrangThaiYeuCau.ChoPhanHoi)actions.Add("Rut");else if(y.TrangThai==TrangThaiYeuCau.DeXuatKhac)actions.AddRange(["KhachDongY","KhachKhongDongY","Rut"]);}
   else if(y.TrangThai==TrangThaiYeuCau.ChoPhanHoi)actions.AddRange(["DaXacNhan","DeXuatKhac","TuChoi"]);
   var dep=deposits.FirstOrDefault(d=>d.RequestId==y.MaYeuCau);
   if(!traveler&&y.TrangThai is TrangThaiYeuCau.ChoPhanHoi or TrangThaiYeuCau.DaXacNhan&&(dep==null||dep.Status=="Requested"))actions.Add(dep==null?"YeuCauCoc":"DaNhanCoc");
   if(traveler&&y.TrangThai==TrangThaiYeuCau.DaXacNhan)actions.Add("ThemVaoChuyenDi");
   return new ChatBookingCard(y.MaYeuCau,y.MaDichVu,r.TenDichVu,y.TrangThai.ToString(),Labels.Status(y.TrangThai.ToString()),y.NgayBatDau,y.NgayKetThuc,y.SoNguoi,y.SoLuong,y.ChiPhiDuKien,y.GhiChu,y.ThoiGianDenDi,y.DieuKienDeXuat,y.LyDo,actions,dep?.Amount,dep?.Method,dep?.Status,dep?.Note,traveler&&y.TrangThai==TrangThaiYeuCau.DaXacNhan&&(y.NgayKetThuc??y.NgayBatDau)<today,y.MaKeHoach);
  }).ToList();
 }
 /// <summary>Hai ben thoa thuan ngay trong chat: moi hanh dong dung quy trinh yeu cau dat san co va ghi mot dong su kien vao cuoc tro chuyen.</summary>
 public async Task BookingAct(long conversationId,ChatBookingAction a)
 {
  var (c,_,_,traveler)=await Open(conversationId,true);await CheckRate();
  Require(await db.Set<TinNhanChat>().AnyAsync(m=>m.ConversationId==c.Id&&m.BookingRequestId==a.RequestId&&m.Kind=="Booking"),"Không tìm thấy yêu cầu trong cuộc trò chuyện.",404);
  var allowed=traveler?new[]{"Rut","KhachDongY","KhachKhongDongY"}:new[]{"DaXacNhan","TuChoi","DeXuatKhac"};
  Require(allowed.Contains(a.Decision),"Bạn không thể thực hiện thao tác này ở vai trò hiện tại.",403);
  var y=await db.YeuCauDatDichVu.AsNoTracking().SingleAsync(x=>x.MaYeuCau==a.RequestId);
  var cmd=new CommandInput{Id=y.MaYeuCau,Version=Token(y.RowVersion),BusinessVersion=y.PhienBan,Decision=a.Decision,Reason=a.Reason??"",Text=a.Text??"",Start=a.Start,End=a.End,People=a.People,Quantity=a.Quantity,Cost=a.Cost};
  await Resolve<IDatDichVuService>().ChapNhanAsync(cmd);
  var after=await db.YeuCauDatDichVu.AsNoTracking().SingleAsync(x=>x.MaYeuCau==a.RequestId);
  var text=after.TrangThai switch
  {
   TrangThaiYeuCau.DaXacNhan=>"Đã xác nhận: hai bên thống nhất yêu cầu #"+after.MaYeuCau+".",
   TrangThaiYeuCau.TuChoi=>"Cơ sở không nhận yêu cầu #"+after.MaYeuCau+(string.IsNullOrWhiteSpace(after.LyDo)?"":": "+after.LyDo),
   TrangThaiYeuCau.DeXuatKhac=>"Cơ sở đề xuất điều kiện khác cho yêu cầu #"+after.MaYeuCau+": "+after.DieuKienDeXuat,
   TrangThaiYeuCau.KhongTiepTuc=>a.Decision=="Rut"?"Khách đã rút yêu cầu #"+after.MaYeuCau+".":"Khách không đồng ý đề xuất, yêu cầu #"+after.MaYeuCau+" không tiếp tục.",
   _=>(traveler?"Khách":"Cơ sở")+" đã cập nhật yêu cầu #"+after.MaYeuCau+"."
  };
  ChatMessageDto? msg=null;
  await Execute(async()=>{msg=await AppendCard(c.Id,traveler,"Event",text,after.MaYeuCau);});
  Notify(c.TravelerId,await SecondAccount(c),new{t="msg",c=c.Id,m=msg!.Id});
 }

 // ------------------------------------------------------------------ ket noi dong hanh ngay trong chat
 /// <summary>Loi ru di cung cong khai cua mot nguoi ma toi chua gui/chua duoc chap nhan (de chon trong chat).</summary>
 public async Task<List<ChatNeedDto>> PeerNeeds(long accountId)
 {
  RoleRequired(Traveler);
  var pending=db.DeNghiKetNoiDongHanh.Where(x=>x.MaNguoiGui==UserId&&(x.TrangThai==TrangThaiDeNghiKetNoi.ChoPhanHoi||x.TrangThai==TrangThaiDeNghiKetNoi.DaChapNhan)).Select(x=>x.MaNhuCau);
  return await Resolve<ICongBoNhuCauService>().NguoiXemThayDuoc().AsNoTracking().Where(n=>n.MaTaiKhoan==accountId&&!pending.Contains(n.MaNhuCau)).OrderBy(n=>n.NgayBatDau)
   .Select(n=>new ChatNeedDto(n.MaNhuCau,n.DiemDen,n.NgayBatDau,n.NgayKetThuc,n.HinhThucDi)).ToListAsync();
 }
 public async Task<(ChatMessageDto Message,long ConversationId)> SendConnection(ChatConnectInput i)
 {
  RoleRequired(Traveler);await CheckRate();
  CuocTroChuyen conv;bool side1;
  if(i.ConversationId is long cid)
  {
   var o=await Open(cid,true);Require(!o.C.IsGroup&&o.C.BranchId==null,"Lời rủ đi cùng chỉ gửi trong cuộc trò chuyện giữa hai người.",403);
   conv=o.C;side1=o.Traveler;
  }
  else if(i.PeerId is long peer){conv=await GetOrCreatePeer(peer);side1=conv.TravelerId==UserId;}
  else throw new BusinessException("Thiếu người nhận lời rủ.",400);
  await RequireWritable(conv,side1);
  var other=side1?conv.PeerId!.Value:conv.TravelerId;
  var need=await db.NhuCauDongHanh.AsNoTracking().SingleOrDefaultAsync(n=>n.MaNhuCau==i.NeedId&&n.MaTaiKhoan==other)??throw new BusinessException("Lời rủ này không thuộc người bạn đang trò chuyện.",422);
  var intro=string.IsNullOrWhiteSpace(i.Text)?"":i.Text.Trim();
  var dayStart=Clock.BayGioUtc.AddHours(-24);
  Require(await db.DeNghiKetNoiDongHanh.CountAsync(x=>x.MaNguoiGui==UserId&&x.ThoiDiemGui>dayStart)<10,"Bạn đã gửi tối đa 10 lời rủ trong 24 giờ. Hãy thử lại sau.",429);
  await Resolve<IKetNoiDongHanhService>().GuiDeNghiAsync(new CommandInput{Id=need.MaNhuCau,Text=intro});
  var request=await db.DeNghiKetNoiDongHanh.AsNoTracking().Where(x=>x.MaNguoiGui==UserId&&x.MaNhuCau==need.MaNhuCau).OrderByDescending(x=>x.MaDeNghi).FirstAsync();
  var text="Rủ đi cùng: "+need.DiemDen+" · "+need.NgayBatDau.ToString("dd/MM/yyyy")+" – "+need.NgayKetThuc.ToString("dd/MM/yyyy")+(intro.Length>0?"\n"+intro:"");
  ChatMessageDto? msg=null;
  await Execute(async()=>{msg=await AppendCard(conv.Id,side1,"Connection",text,null,request.MaDeNghi);});
  Notify(conv.TravelerId,await SecondAccount(conv),new{t="msg",c=conv.Id,m=msg!.Id});
  return (msg!,conv.Id);
 }
 public async Task<List<ChatConnectionCard>> ConnectionCards(long conversationId)
 {
  var (c,_,_,_)=await Open(conversationId);
  var ids=await db.Set<TinNhanChat>().AsNoTracking().Where(m=>m.ConversationId==c.Id&&m.Kind=="Connection"&&m.ConnectionId!=null).Select(m=>m.ConnectionId!.Value).Distinct().ToListAsync();
  if(ids.Count==0)return [];
  var rows=await (from d in db.DeNghiKetNoiDongHanh.AsNoTracking() join n in db.NhuCauDongHanh.AsNoTracking() on d.MaNhuCau equals n.MaNhuCau where ids.Contains(d.MaDeNghi) orderby d.MaDeNghi select new{d,n}).ToListAsync();
  return rows.Select(r=>
  {
   var actions=new List<string>();
   if(r.d.TrangThai==TrangThaiDeNghiKetNoi.ChoPhanHoi){if(r.n.MaTaiKhoan==UserId)actions.AddRange(["DaChapNhan","TuChoi"]);else if(r.d.MaNguoiGui==UserId)actions.Add("DaRut");}
   return new ChatConnectionCard(r.d.MaDeNghi,r.n.MaNhuCau,r.n.DiemDen,r.n.NgayBatDau,r.n.NgayKetThuc,r.d.LoiGioiThieu,r.d.TrangThai.ToString(),Labels.Status(r.d.TrangThai.ToString()),actions,"/dong-hanh/de-nghi/xem-de-nghi/"+r.d.MaDeNghi,r.d.MaNguoiGui==UserId,r.n.MaTaiKhoan==UserId?r.d.MaNguoiGui:r.n.MaTaiKhoan);
  }).ToList();
 }
 /// <summary>Chap nhan / tu choi / rut loi ru ngay trong chat, dung quy trinh ket noi dong hanh san co.</summary>
 public async Task ConnectionAct(long conversationId,ChatConnectAction a)
 {
  var (c,_,_,side1)=await Open(conversationId,true);await CheckRate();
  Require(await db.Set<TinNhanChat>().AnyAsync(m=>m.ConversationId==c.Id&&m.ConnectionId==a.ConnectionId&&m.Kind=="Connection"),"Không tìm thấy lời rủ trong cuộc trò chuyện.",404);
  var req=await db.DeNghiKetNoiDongHanh.AsNoTracking().SingleAsync(x=>x.MaDeNghi==a.ConnectionId);
  var need=await db.NhuCauDongHanh.AsNoTracking().SingleAsync(n=>n.MaNhuCau==req.MaNhuCau);
  var allowed=need.MaTaiKhoan==UserId?new[]{"DaChapNhan","TuChoi"}:req.MaNguoiGui==UserId?new[]{"DaRut"}:Array.Empty<string>();
  Require(allowed.Contains(a.Decision),"Bạn không thể thực hiện thao tác này với lời rủ.",403);
  await Resolve<IKetNoiDongHanhService>().PhanHoiAsync(new CommandInput{Id=req.MaDeNghi,Version=Token(req.RowVersion),Decision=a.Decision});
  if(a.Decision=="DaChapNhan")await Resolve<FriendService>().EnsureFriends(need.MaTaiKhoan,req.MaNguoiGui);
  var text=a.Decision switch
  {
   "DaChapNhan"=>"Đã kết nối: hai bạn sẽ cùng đi "+need.DiemDen+". Bạn có thể lập thỏa thuận đi cùng ở thẻ phía trên.",
   "TuChoi"=>"Lời rủ đi "+need.DiemDen+" chưa phù hợp, không được nhận.",
   _=>"Người rủ đã rút lời rủ đi "+need.DiemDen+"."
  };
  ChatMessageDto? msg=null;
  await Execute(async()=>{msg=await AppendCard(c.Id,side1,"Event",text,null,req.MaDeNghi);});
  Notify(c.TravelerId,await SecondAccount(c),new{t="msg",c=c.Id,m=msg!.Id});
 }

 // ------------------------------------------------------------------ da xem / dang soan / trang thai
 public async Task MarkRead(long id)
 {
  var (c,b,p,traveler)=await Open(id,true);
  var last=await db.Set<TinNhanChat>().Where(m=>m.ConversationId==id).MaxAsync(m=>(long?)m.Id)??0;
  if(c.IsGroup){await MarkGroupRead(c,last);return;}
  var now=Clock.BayGioUtc;
  if(traveler){if(c.TravelerReadId>=last)return;c.TravelerReadId=last;c.TravelerReadAt=now;}
  else{if(c.BranchReadId>=last)return;c.BranchReadId=last;c.BranchReadAt=now;}
  await db.SaveChangesAsync();
  Notify(c.TravelerId,await SecondAccount(c),new{t="read",c=id,r=last,side=traveler?"Traveler":"Branch"});
 }
 public async Task Typing(long id)
 {
  var (c,b,p,traveler)=await Open(id);
  if(c.IsGroup){await GroupTyping(c);return;}
  broker.Publish(traveler?await SecondAccount(c):c.TravelerId,Json(new{t="typing",c=id,side=traveler?"Traveler":"Branch"}));
 }
 public async Task SetState(long id,ChatStateInput i)
 {
  RoleRequired(ProviderRole);
  var (c,_,_,_)=await Open(id,true);
  Require(c.BranchId!=null,"Chỉ cuộc trò chuyện với cơ sở mới có trạng thái xử lý.",422);
  if(i.State!=null){Require(i.State is "Open" or "Done","Trạng thái không hợp lệ.",400);c.State=i.State;}
  if(i.Label!=null)c.Label=string.IsNullOrWhiteSpace(i.Label)?null:i.Label.Trim();
  if(i.Note!=null)c.InternalNote=string.IsNullOrWhiteSpace(i.Note)?null:i.Note.Trim();
  await db.SaveChangesAsync();
 }
 /// <summary>Chan / bo chan nguoi con lai: bi chan thi khong ben nao gui them duoc tin.</summary>
 public async Task SetBlock(long id,bool block)
 {
  var (c,_,_,_)=await Open(id,true);
  Require(!c.IsGroup,"Nhóm không có chức năng chặn. Hãy rời nhóm.",422);
  if(block)c.BlockedById=UserId;
  else{Require(c.BlockedById==UserId,"Chỉ người đã chặn mới bỏ chặn được.",403);c.BlockedById=null;}
  await db.SaveChangesAsync();
 }
 public async Task<int> Unread()
 {
  if(UserId==0||Role is not (Traveler or ProviderRole))return 0;
  long? providerId=Role==ProviderRole?(await MyProvider()).MaNhaCungCap:null;
  var asFirst=await db.Set<CuocTroChuyen>().AsNoTracking().Where(c=>c.TravelerId==UserId&&c.LastMessageId>c.TravelerReadId&&!c.LastFromTraveler)
   .SumAsync(c=>db.Set<TinNhanChat>().Count(m=>m.ConversationId==c.Id&&m.Id>c.TravelerReadId&&m.Side!="Traveler"));
  var asSecond=await db.Set<CuocTroChuyen>().AsNoTracking().Where(c=>c.LastMessageId>c.BranchReadId&&c.LastFromTraveler&&(c.PeerId==UserId||(providerId!=null&&c.BranchId!=null&&db.Set<CoSoDichVu>().Any(b=>b.Id==c.BranchId&&b.ProviderId==providerId))))
   .SumAsync(c=>db.Set<TinNhanChat>().Count(m=>m.ConversationId==c.Id&&m.Id>c.BranchReadId&&m.Side=="Traveler"));
  var asMember=await db.Set<ThanhVienChat>().AsNoTracking().Where(t=>t.AccountId==UserId&&t.LeftAt==null).SumAsync(t=>db.Set<TinNhanChat>().Count(m=>m.ConversationId==t.ConversationId&&m.Id>t.ReadId&&m.SenderId!=UserId));
  return asFirst+asSecond+asMember;
 }
 public long Account=>UserId;
 public ChatBroker Broker=>broker;

 // ------------------------------------------------------------------ anh
 public async Task<string> ImageFile(long conversationId,long messageId)
 {
  await Open(conversationId);
  var m=await db.Set<TinNhanChat>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==messageId&&x.ConversationId==conversationId&&x.ImagePath!=null)??throw new BusinessException("Không tìm thấy ảnh.",404);
  var path=Resolve<ILuuTepService>().DuongDanAnToan(m.ImagePath!);
  Require(System.IO.File.Exists(path)&&Path.GetExtension(path).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png","Không tìm thấy ảnh.",404);
  return path;
 }

 // ------------------------------------------------------------------ thoi gian tra loi thuc te
 public async Task<string?> Actual(long branchId)
 {
  var since=Clock.BayGioUtc.AddDays(-30);
  var rows=await (from m in db.Set<TinNhanChat>().AsNoTracking() join c in db.Set<CuocTroChuyen>() on m.ConversationId equals c.Id
   where c.BranchId==branchId&&m.CreatedAt>since&&m.Kind!="Auto" orderby m.Id select new{m.ConversationId,m.Side,m.CreatedAt}).Take(2000).ToListAsync();
  var waits=new List<double>();
  foreach(var g in rows.GroupBy(r=>r.ConversationId))
  {
   var t=g.FirstOrDefault(r=>r.Side=="Traveler");if(t==null)continue;
   var r1=g.FirstOrDefault(r=>r.Side=="Branch"&&r.CreatedAt>t.CreatedAt);
   if(r1!=null)waits.Add((r1.CreatedAt-t.CreatedAt).TotalMinutes);
  }
  if(waits.Count<3)return null;
  waits.Sort();var median=waits[waits.Count/2];
  return median<60?"Trả lời thực tế khoảng "+Math.Max(1,(int)Math.Round(median))+" phút":median<24*60?"Trả lời thực tế khoảng "+(int)Math.Round(median/60)+" giờ":"Trả lời thực tế trong vài ngày";
 }

 // ------------------------------------------------------------------ cai dat cua co so
 async Task<CoSoDichVu> OwnBranch(long branchId)
 {
  RoleRequired(ProviderRole);var p=await MyProvider();
  return await db.Set<CoSoDichVu>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==branchId&&x.ProviderId==p.MaNhaCungCap)??throw new BusinessException("Không tìm thấy cơ sở.",404);
 }
 public async Task<List<(long Id,string Name)>> MyBranches()
 {
  RoleRequired(ProviderRole);var p=await MyProvider();
  return (await db.Set<CoSoDichVu>().AsNoTracking().Where(b=>b.ProviderId==p.MaNhaCungCap).OrderBy(b=>b.Name).Select(b=>new{b.Id,b.Name}).ToListAsync()).Select(x=>(x.Id,x.Name)).ToList();
 }
 public async Task<ChatSettingsDto> SettingsPage(long branchId)
 {
  var b=await OwnBranch(branchId);var s=await SettingsOf(branchId);
  var quick=await db.Set<TinMauChat>().AsNoTracking().Where(x=>x.BranchId==branchId).OrderBy(x=>x.Id).Select(x=>new TinMauDto(x.Id,x.Title,x.Body)).ToListAsync();
  var stats=await BranchStats(b.Id);
  return new(b.Id,b.Name,s.Accepting,s.ReplyWithin,s.AutoReply,s.AutoReplyOffHours,s.Greeting,quick,stats.Requests,stats.Confirmed,stats.Actual);
 }
 public async Task SaveSettings(ChatSettingsInput i)
 {
  var b=await OwnBranch(i.BranchId);
  Require(Waits.Contains(i.ReplyWithin),"Thời gian trả lời không hợp lệ.",400);
  var row=await db.Set<CaiDatChatCoSo>().SingleOrDefaultAsync(x=>x.BranchId==b.Id);
  if(row==null){row=new CaiDatChatCoSo{BranchId=b.Id};db.Add(row);}
  row.Accepting=i.Accepting;row.ReplyWithin=i.ReplyWithin;row.AutoReplyOffHours=i.AutoReplyOffHours;
  row.AutoReply=string.IsNullOrWhiteSpace(i.AutoReply)?null:i.AutoReply.Trim();row.Greeting=string.IsNullOrWhiteSpace(i.Greeting)?null:i.Greeting.Trim();row.UpdatedAt=Clock.BayGioUtc;
  await db.SaveChangesAsync();
 }
 public async Task SaveQuick(QuickReplyInput i)
 {
  var b=await OwnBranch(i.BranchId);
  if(i.Id is long id)
  {
   var row=await db.Set<TinMauChat>().SingleOrDefaultAsync(x=>x.Id==id&&x.BranchId==b.Id)??throw new BusinessException("Không tìm thấy tin mẫu.",404);
   if(i.Delete){db.Remove(row);await db.SaveChangesAsync();return;}
   Require(i.Title.Trim().Length>0&&i.Body.Trim().Length>0,"Nhập tiêu đề và nội dung tin mẫu.",422);row.Title=i.Title.Trim();row.Body=i.Body.Trim();
  }
  else
  {
   Require(i.Title.Trim().Length>0&&i.Body.Trim().Length>0,"Nhập tiêu đề và nội dung tin mẫu.",422);
   Require(await db.Set<TinMauChat>().CountAsync(x=>x.BranchId==b.Id)<20,"Mỗi cơ sở có tối đa 20 tin mẫu.",422);
   db.Add(new TinMauChat{BranchId=b.Id,Title=i.Title.Trim(),Body=i.Body.Trim(),CreatedAt=Clock.BayGioUtc});
  }
  await db.SaveChangesAsync();
 }
 public async Task<List<TinMauDto>> Quick(long branchId)
 {
  await OwnBranch(branchId);
  return await db.Set<TinMauChat>().AsNoTracking().Where(x=>x.BranchId==branchId).OrderBy(x=>x.Id).Select(x=>new TinMauDto(x.Id,x.Title,x.Body)).ToListAsync();
 }
}
