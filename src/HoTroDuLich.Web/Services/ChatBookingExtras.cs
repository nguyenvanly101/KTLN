using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

/// <summary>Dat dich vu qua chat: ghi nhan dat coc (khong xu ly tien), them vao chuyen di, thong ke cua co so.</summary>
public sealed partial class ChatService
{
 static readonly string[] DepositMethods=["Chuyển khoản trực tiếp","Tiền mặt tại quầy"];

 async Task<(CuocTroChuyen C,YeuCauDatDichVu Y)> BookingInConversation(long conversationId,long requestId,bool providerSide)
 {
  var (c,_,_,side1)=await Open(conversationId);
  Require(side1!=providerSide,providerSide?"Chỉ cơ sở mới thực hiện thao tác này.":"Chỉ khách mới thực hiện thao tác này.",403);
  Require(await db.Set<TinNhanChat>().AnyAsync(m=>m.ConversationId==c.Id&&m.BookingRequestId==requestId&&m.Kind=="Booking"),"Không tìm thấy yêu cầu trong cuộc trò chuyện.",404);
  var y=await db.YeuCauDatDichVu.AsNoTracking().SingleAsync(x=>x.MaYeuCau==requestId);
  return (c,y);
 }
 /// <summary>Co so de nghi dat coc cho yeu cau dang cho / da xac nhan. Hai ben tu thanh toan truc tiep.</summary>
 public async Task RequestDeposit(long conversationId,ChatDepositInput i)
 {
  var (c,y)=await BookingInConversation(conversationId,i.RequestId,true);await CheckRate();
  Require(y.TrangThai is TrangThaiYeuCau.ChoPhanHoi or TrangThaiYeuCau.DaXacNhan,"Chỉ đề nghị đặt cọc cho yêu cầu đang chờ hoặc đã xác nhận.",409);
  Require(i.Amount is >0 and <=1_000_000_000,"Số tiền cọc phải lớn hơn 0 và tối đa 1 tỷ đồng.",422);
  Require(DepositMethods.Contains(i.Method),"Chọn hình thức đặt cọc.",422);
  var note=string.IsNullOrWhiteSpace(i.Note)?null:Cut(i.Note.Trim(),300);
  var row=await db.Set<DatCocChat>().SingleOrDefaultAsync(x=>x.RequestId==y.MaYeuCau);
  Require(row==null||row.Status!="Paid","Cọc đã được xác nhận nhận tiền.",409);
  if(row==null)db.Add(new DatCocChat{RequestId=y.MaYeuCau,Amount=i.Amount,Method=i.Method,Note=note,Status="Requested",RequestedAt=Clock.BayGioUtc});
  else{row.Amount=i.Amount;row.Method=i.Method;row.Note=note;}
  await db.SaveChangesAsync();
  ChatMessageDto? msg=null;
  await Execute(async()=>{msg=await AppendCard(c.Id,false,"Event","Cơ sở đề nghị đặt cọc "+Labels.Money(i.Amount)+" ("+i.Method+") cho yêu cầu #"+y.MaYeuCau+". Hai bên tự thanh toán với nhau, website không xử lý tiền.",y.MaYeuCau);});
  await PublishUpdate(c,msg!.Id);Notify(c.TravelerId,await SecondAccount(c),new{t="msg",c=c.Id,m=msg.Id});
 }
 public async Task ConfirmDeposit(long conversationId,long requestId)
 {
  var (c,y)=await BookingInConversation(conversationId,requestId,true);await CheckRate();
  var row=await db.Set<DatCocChat>().SingleOrDefaultAsync(x=>x.RequestId==y.MaYeuCau&&x.Status=="Requested")??throw new BusinessException("Chưa có đề nghị đặt cọc đang chờ.",404);
  row.Status="Paid";row.PaidAt=Clock.BayGioUtc;await db.SaveChangesAsync();
  ChatMessageDto? msg=null;
  await Execute(async()=>{msg=await AppendCard(c.Id,false,"Event","Cơ sở đã nhận cọc "+Labels.Money(row.Amount)+" cho yêu cầu #"+y.MaYeuCau+".",y.MaYeuCau);});
  Notify(c.TravelerId,await SecondAccount(c),new{t="msg",c=c.Id,m=msg!.Id});
 }
 /// <summary>Ten hien thi cua khach (trang chi tiet yeu cau cua nha cung cap).</summary>
 public Task<string> NameOf(long account)=>TravelerName(account);
 /// <summary>Yeu cau co ghi co so thi hai ben nhan tin duoc voi nhau.</summary>
 public Task<bool> BookingHasBranch(long requestId)=>db.Set<YeuCauDatCoSo>().AsNoTracking().AnyAsync(x=>x.RequestId==requestId);
 /// <summary>Dua yeu cau gui theo mau vao cuoc tro chuyen giua khach va co so (tao cuoc tro chuyen neu chua co) de hai ben trao doi nhu khi dat qua chat.
 /// Tra ve null khi yeu cau khong ghi co so, co so da bi go hoac cuoc tro chuyen dang bi chan.</summary>
 public async Task<long?> AttachBooking(long requestId)
 {
  if(UserId==0||Role is not (Traveler or ProviderRole))return null;
  var y=await Resolve<IDatDichVuService>().ChiTietAsync(requestId);
  var card=await db.Set<TinNhanChat>().AsNoTracking().Where(m=>m.BookingRequestId==requestId&&m.Kind=="Booking").Select(m=>(long?)m.ConversationId).FirstOrDefaultAsync();
  if(card!=null)return card;
  var snap=await db.Set<YeuCauDatCoSo>().AsNoTracking().SingleOrDefaultAsync(x=>x.RequestId==requestId);
  if(snap==null||!await db.Set<CoSoDichVu>().AsNoTracking().AnyAsync(b=>b.Id==snap.BranchId))return null;
  var owner=await OwnerAccount(snap.BranchId);
  if(owner==y.MaTaiKhoan)return null;
  Task<CuocTroChuyen?> Find()=>db.Set<CuocTroChuyen>().AsNoTracking().FirstOrDefaultAsync(x=>x.BranchId==snap.BranchId&&x.TravelerId==y.MaTaiKhoan);
  var conv=await Find();
  if(conv==null)
  {
   var created=new CuocTroChuyen{BranchId=snap.BranchId,TravelerId=y.MaTaiKhoan,State="Open",LastMessageAt=Clock.BayGioUtc,CreatedAt=Clock.BayGioUtc};
   db.Add(created);
   try{await db.SaveChangesAsync();conv=created;}
   catch(DbUpdateException){db.Entry(created).State=EntityState.Detached;conv=await Find();if(conv==null)throw;}
  }
  if(conv.BlockedById!=null)return null;
  var service=await db.DichVuDuLich.AsNoTracking().Where(s=>s.MaDichVu==y.MaDichVu).Select(s=>s.TenDichVu).SingleAsync();
  var text=service+" · "+y.NgayBatDau.ToString("dd/MM/yyyy")+(y.NgayKetThuc!=null?" – "+y.NgayKetThuc.Value.ToString("dd/MM/yyyy"):"")+" · "+y.SoNguoi+" người · SL "+y.SoLuong+(string.IsNullOrWhiteSpace(y.GhiChu)?"":"\n"+y.GhiChu);
  ChatMessageDto? msg=null;var convId=conv.Id;
  await Execute(async()=>{msg=await AppendCard(convId,true,"Booking",text,requestId,senderId:y.MaTaiKhoan);});
  Notify(y.MaTaiKhoan,owner,new{t="msg",c=convId,m=msg!.Id});
  return convId;
 }
 public async Task<List<(long Id,string Name,DateOnly Start,DateOnly End)>> MyPlans()
 {
  RoleRequired(Traveler);
  return (await Resolve<TravelService>().Plans()).Select(p=>(p.MaKeHoach,p.TenKeHoach,p.NgayBatDau,p.NgayKetThuc)).ToList();
 }
 public async Task AssignPlan(long conversationId,long requestId,long? planId)
 {
  var (_,y)=await BookingInConversation(conversationId,requestId,false);
  await Resolve<TripWorkspaceService>().AssignBooking(y.MaYeuCau,Token(y.RowVersion),planId);
 }
 /// <summary>Thong ke 30 ngay gan nhat cua co so: so yeu cau dat, so da xac nhan, thoi gian tra loi thuc te.</summary>
 public async Task<(int Requests,int Confirmed,string? Actual)> BranchStats(long branchId)
 {
  var since=Clock.BayGioUtc.AddDays(-30);
  var ids=await (from m in db.Set<TinNhanChat>().AsNoTracking() join c in db.Set<CuocTroChuyen>().AsNoTracking() on m.ConversationId equals c.Id
   where c.BranchId==branchId&&m.Kind=="Booking"&&m.CreatedAt>since&&m.BookingRequestId!=null select m.BookingRequestId!.Value).Distinct().ToListAsync();
  var confirmed=ids.Count==0?0:await db.YeuCauDatDichVu.AsNoTracking().CountAsync(y=>ids.Contains(y.MaYeuCau)&&(y.TrangThai==TrangThaiYeuCau.DaXacNhan||y.TrangThai==TrangThaiYeuCau.HoanThanh||y.TrangThai==TrangThaiYeuCau.ThucHienMotPhan));
  return (ids.Count,confirmed,await Actual(branchId));
 }
}

/// <summary>Nhac lich ngay truoc ngay dung dich vu va moi danh gia sau khi dung xong, gui thang vao cuoc tro chuyen dat cho.</summary>
public sealed class ChatReminderService(AppDbContext db,ChatBroker broker,IDongHo clock,IThongBaoService notifications,IConfiguration config,ILuuTepService files)
{
 public async Task<int> RunAsync()
 {
  var today=clock.HomNay;var tomorrow=today.AddDays(1);var sent=0;
  var due=await (from y in db.YeuCauDatDichVu.AsNoTracking()
   where y.TrangThai==TrangThaiYeuCau.DaXacNhan&&(y.NgayBatDau==tomorrow||(y.NgayKetThuc??y.NgayBatDau)<today)
   select new{y.MaYeuCau,y.NgayBatDau,y.NgayKetThuc}).Take(200).ToListAsync();
  foreach(var d in due)
  {
   var kind=d.NgayBatDau==tomorrow?"Reminder":"Review";
   if(await db.Set<NhacLichChat>().AnyAsync(x=>x.RequestId==d.MaYeuCau&&x.Kind==kind))continue;
   var conv=await (from m in db.Set<TinNhanChat>().AsNoTracking() join c in db.Set<CuocTroChuyen>() on m.ConversationId equals c.Id where m.BookingRequestId==d.MaYeuCau&&m.Kind=="Booking" select c).FirstOrDefaultAsync();
   db.Add(new NhacLichChat{RequestId=d.MaYeuCau,Kind=kind,SentAt=clock.BayGioUtc});
   if(conv==null){await db.SaveChangesAsync();continue;}
   var text=kind=="Reminder"?"Nhắc lịch: ngày mai "+d.NgayBatDau.ToString("dd/MM/yyyy")+" là ngày sử dụng dịch vụ của yêu cầu #"+d.MaYeuCau+". Hai bên kiểm tra lại giờ giấc và liên hệ nhau nếu cần.":"Yêu cầu #"+d.MaYeuCau+" đã diễn ra. Mời khách chia sẻ đánh giá để giúp những người đi sau.";
   var msg=new TinNhanChat{ConversationId=conv.Id,SenderId=null,Side="System",Kind="Event",Body=text,BookingRequestId=d.MaYeuCau,CreatedAt=clock.BayGioUtc};
   db.Add(msg);await db.SaveChangesAsync();
   conv.LastMessageId=msg.Id;conv.LastMessageAt=msg.CreatedAt;conv.LastPreview=text.Length>110?text[..109]+"…":text;
   var owner=await db.Set<CoSoDichVu>().Where(b=>b.Id==conv.BranchId).Join(db.NhaCungCapDichVu,b=>b.ProviderId,p=>p.MaNhaCungCap,(b,p)=>p.MaTaiKhoan).FirstOrDefaultAsync();
   notifications.Tao(conv.TravelerId,"NhacLich",text,"/tin-nhan/dat-cho/"+d.MaYeuCau);
   if(owner!=0&&kind=="Reminder")notifications.Tao(owner,"NhacLich",text,"/tin-nhan/dat-cho/"+d.MaYeuCau);
   await db.SaveChangesAsync();
   var payload=System.Text.Json.JsonSerializer.Serialize(new{t="msg",c=conv.Id,m=msg.Id});
   broker.Publish(conv.TravelerId,payload);if(owner!=0)broker.Publish(owner,payload);
   sent++;
  }
  await ExpireAsync();
  await PurgeOldAsync();
  return sent;
 }
 /// <summary>Yeu cau con cho phan hoi / cho khach tra loi de xuat ma da qua ngay bat dau: tu dong chuyen sang "Khong tiep tuc" va bao cho khach.</summary>
 public async Task<int> ExpireAsync()
 {
  var today=clock.HomNay;var done=0;
  var late=await db.YeuCauDatDichVu.AsNoTracking().Where(y=>(y.TrangThai==TrangThaiYeuCau.ChoPhanHoi||y.TrangThai==TrangThaiYeuCau.DeXuatKhac)&&y.NgayBatDau<today).OrderBy(y=>y.MaYeuCau).Select(y=>y.MaYeuCau).Take(100).ToListAsync();
  foreach(var id in late)
  {
   try
   {
    var y=await db.YeuCauDatDichVu.SingleAsync(x=>x.MaYeuCau==id);
    if(y.TrangThai is not (TrangThaiYeuCau.ChoPhanHoi or TrangThaiYeuCau.DeXuatKhac))continue;
    var before=y.TrangThai.ToString();var now=clock.BayGioUtc;
    y.TrangThai=TrangThaiYeuCau.KhongTiepTuc;y.LyDo="Quá ngày sử dụng mà hai bên chưa thống nhất";y.ThoiDiemPhanHoi=now;y.PhienBan++;
    db.NhatKyThaoTac.Add(new(){LoaiSuKien="PhanHoiDatDichVu",DoiTuong="YeuCauDatDichVu",MaDoiTuong=id,TrangThaiTruoc=before,TrangThaiSau="KhongTiepTuc",LyDo="Tự đóng: quá ngày sử dụng"});
    var text="Yêu cầu #"+id+" đã tự đóng vì quá ngày sử dụng "+y.NgayBatDau.ToString("dd/MM/yyyy")+" mà hai bên chưa thống nhất.";
    notifications.Tao(y.MaTaiKhoan,"YeuCauKhongTiepTuc",text,"/yeu-cau/"+id);
    var conv=await (from m in db.Set<TinNhanChat>().AsNoTracking() join c in db.Set<CuocTroChuyen>() on m.ConversationId equals c.Id where m.BookingRequestId==id&&m.Kind=="Booking" select c).FirstOrDefaultAsync();
    TinNhanChat? msg=null;
    if(conv!=null){msg=new TinNhanChat{ConversationId=conv.Id,SenderId=null,Side="System",Kind="Event",Body=text,BookingRequestId=id,CreatedAt=now};db.Add(msg);}
    await db.SaveChangesAsync();
    if(conv!=null&&msg!=null)
    {
     conv.LastMessageId=msg.Id;conv.LastMessageAt=now;conv.LastPreview=text.Length>110?text[..109]+"…":text;await db.SaveChangesAsync();
     var owner=await db.Set<CoSoDichVu>().Where(b=>b.Id==conv.BranchId).Join(db.NhaCungCapDichVu,b=>b.ProviderId,p=>p.MaNhaCungCap,(b,p)=>p.MaTaiKhoan).FirstOrDefaultAsync();
     var payload=System.Text.Json.JsonSerializer.Serialize(new{t="msg",c=conv.Id,m=msg.Id});
     broker.Publish(conv.TravelerId,payload);if(owner!=0)broker.Publish(owner,payload);
    }
    done++;
   }
   catch(DbUpdateException){/* hai ben vua thao tac: lan sau xet lai */}
   db.ChangeTracker.Clear();
  }
  return done;
 }
 /// <summary>Chinh sach giu tin nhan: cau hinh Chat:RetentionMonths (0 = tat). Tin chu / anh / vi tri / tep cu hon moc bi thu hoi, tep bi xoa khoi dia; the dat cho va thoa thuan duoc giu.</summary>
 public async Task<int> PurgeOldAsync()
 {
  var months=config.GetValue("Chat:RetentionMonths",0);if(months<=0)return 0;
  var cutoff=clock.BayGioUtc.AddMonths(-months);
  var old=await db.Set<TinNhanChat>().Where(m=>m.DeletedAt==null&&m.CreatedAt<cutoff&&(m.Kind=="Text"||m.Kind=="Image"||m.Kind=="Location"||m.Kind=="File")).Take(500).ToListAsync();
  foreach(var m in old)
  {
   foreach(var f in new[]{m.ImagePath,m.FilePath}.Where(x=>x!=null)){try{var path=files.DuongDanAnToan(f!);if(System.IO.File.Exists(path))System.IO.File.Delete(path);}catch{/* se don sau */}}
   m.DeletedAt=clock.BayGioUtc;m.Body="";m.ImagePath=null;m.FilePath=null;m.FileName=null;m.Lat=null;m.Lng=null;
  }
  if(old.Count>0){db.RemoveRange(await db.Set<PhanUngTin>().Where(r=>old.Select(o=>o.Id).Contains(r.MessageId)).ToListAsync());await db.SaveChangesAsync();}
  return old.Count;
 }
}

public sealed class ChatReminderWorker(IServiceProvider services,IConfiguration config,ILogger<ChatReminderWorker> log):BackgroundService
{
 protected override async Task ExecuteAsync(CancellationToken stop)
 {
  var seconds=Math.Max(5,config.GetValue("Chat:ReminderIntervalSeconds",900));
  try{await Task.Delay(TimeSpan.FromSeconds(Math.Min(seconds,20)),stop);}catch(OperationCanceledException){return;}
  while(!stop.IsCancellationRequested)
  {
   try{using var scope=services.CreateScope();await scope.ServiceProvider.GetRequiredService<ChatReminderService>().RunAsync();}
   catch(Exception e)when(e is not OperationCanceledException){log.LogWarning(e,"Nhac lich chat loi");}
   try{await Task.Delay(TimeSpan.FromSeconds(seconds),stop);}catch(OperationCanceledException){return;}
  }
 }
}
