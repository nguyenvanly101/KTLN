using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

/// <summary>Thoa thuan di cung ngay trong chat + bao cao nguoi dung / tin nhan. Dung lai quy trinh ThoaThuanDongHanh san co.</summary>
public sealed partial class ChatService
{
 static readonly string[] ReportReasons=["Spam","QuayRoi","LuaDao","NoiDungXau","Khac"];

 /// <summary>Hai tai khoan cua mot cuoc tro chuyen nguoi voi nguoi (khong nhom, khong co so).</summary>
 async Task<(CuocTroChuyen C,bool Side1,long Other)> PeerConversation(long id,bool track=false)
 {
  var (c,_,_,side1)=await Open(id,track);
  Require(!c.IsGroup&&c.BranchId==null,"Chỉ dùng trong cuộc trò chuyện giữa hai người.",403);
  return (c,side1,side1?c.PeerId!.Value:c.TravelerId);
 }
 public async Task<(ChatMessageDto Message,long ConversationId)> SendAgreement(ChatAgreementInput i)
 {
  RoleRequired(Traveler);await CheckRate();
  var (c,side1,other)=await PeerConversation(i.ConversationId,true);
  await RequireWritable(c,side1);
  var conn=await db.DeNghiKetNoiDongHanh.AsNoTracking().SingleOrDefaultAsync(x=>x.MaDeNghi==i.ConnectionId&&x.TrangThai==TrangThaiDeNghiKetNoi.DaChapNhan)??throw new BusinessException("Chưa có kết nối đã được đồng ý để lập thỏa thuận.",422);
  var need=await db.NhuCauDongHanh.AsNoTracking().SingleAsync(n=>n.MaNhuCau==conn.MaNhuCau);
  Require((conn.MaNguoiGui==UserId&&need.MaTaiKhoan==other)||(conn.MaNguoiGui==other&&need.MaTaiKhoan==UserId),"Kết nối này không thuộc cuộc trò chuyện.",403);
  var version=await db.ThoaThuanDongHanh.AsNoTracking().Where(x=>x.MaDeNghi==conn.MaDeNghi).MaxAsync(x=>(int?)x.PhienBan)??0;
  await Resolve<IThoaThuanDongHanhService>().GuiDeNghiThoaThuanAsync(new AgreementInput{ConnectionId=conn.MaDeNghi,BaseVersion=version,Kind="NoiDung",Name="Thỏa thuận đồng hành",Destination=i.Destination??"",Start=i.Start,End=i.End,Budget=i.Budget,Scope=string.IsNullOrWhiteSpace(i.Scope)?null:i.Scope.Trim()});
  var t=await db.ThoaThuanDongHanh.AsNoTracking().Where(x=>x.MaDeNghi==conn.MaDeNghi).OrderByDescending(x=>x.PhienBan).FirstAsync();
  var text="Đề xuất thỏa thuận đi cùng: "+t.NoiDen+" · "+t.NgayDi.ToString("dd/MM/yyyy")+" – "+t.NgayVe.ToString("dd/MM/yyyy")+(t.NganSach!=null?" · ngân sách "+Labels.Money(t.NganSach):"");
  ChatMessageDto? msg=null;
  await Execute(async()=>{msg=await AppendCard(c.Id,side1,"Agreement",text,null,null,t.MaThoaThuan);});
  Notify(c.TravelerId,await SecondAccount(c),new{t="msg",c=c.Id,m=msg!.Id});
  return (msg!,c.Id);
 }
 public async Task<List<ChatAgreementCard>> AgreementCards(long conversationId)
 {
  var (c,_,_,_)=await Open(conversationId);
  var ids=await db.Set<TinNhanChat>().AsNoTracking().Where(m=>m.ConversationId==c.Id&&m.Kind=="Agreement"&&m.AgreementId!=null).Select(m=>m.AgreementId!.Value).Distinct().ToListAsync();
  if(ids.Count==0)return [];
  return (await db.ThoaThuanDongHanh.AsNoTracking().Where(t=>ids.Contains(t.MaThoaThuan)).OrderBy(t=>t.MaThoaThuan).ToListAsync()).Select(t=>
  {
   var actions=new List<string>();
   if(t.TrangThai==TrangThaiThoaThuan.ChoXacNhan&&t.MaNguoiDeXuat!=UserId)actions.AddRange(["DaXacNhan","TuChoi"]);
   else if(t.TrangThai==TrangThaiThoaThuan.DaXacNhan)actions.Add("Rut");
   return new ChatAgreementCard(t.MaThoaThuan,t.MaDeNghi,t.PhienBan,t.NoiDen,t.NgayDi,t.NgayVe,t.NganSach,t.PhamViDiCung,t.TrangThai.ToString(),Labels.Status(t.TrangThai.ToString()),t.MaNguoiDeXuat==UserId,actions,"/dong-hanh/de-nghi/xem-de-nghi/"+t.MaDeNghi,t.LyDo);
  }).ToList();
 }
 public async Task AgreementAct(long conversationId,ChatAgreementAction a)
 {
  var (c,side1,_)=await PeerConversation(conversationId,true);await CheckRate();
  Require(await db.Set<TinNhanChat>().AnyAsync(m=>m.ConversationId==c.Id&&m.AgreementId==a.AgreementId&&m.Kind=="Agreement"),"Không tìm thấy thỏa thuận trong cuộc trò chuyện.",404);
  Require(a.Decision is "DaXacNhan" or "TuChoi" or "Rut","Quyết định không hợp lệ.",400);
  var t=await db.ThoaThuanDongHanh.AsNoTracking().SingleAsync(x=>x.MaThoaThuan==a.AgreementId);
  await Resolve<IThoaThuanDongHanhService>().XacNhanAsync(new CommandInput{Id=t.MaThoaThuan,Version=Token(t.RowVersion),BusinessVersion=t.PhienBan,Decision=a.Decision,Reason=a.Reason??""});
  var text=a.Decision switch{"DaXacNhan"=>"Thỏa thuận đi cùng đã được xác nhận: "+t.NoiDen+", "+t.NgayDi.ToString("dd/MM")+"–"+t.NgayVe.ToString("dd/MM")+".","TuChoi"=>"Thỏa thuận chưa được chấp nhận"+(string.IsNullOrWhiteSpace(a.Reason)?"":": "+a.Reason),_=>"Một bên đã kết thúc thỏa thuận đi cùng."};
  ChatMessageDto? msg=null;
  await Execute(async()=>{msg=await AppendCard(c.Id,side1,"Event",text,null,null,t.MaThoaThuan);});
  Notify(c.TravelerId,await SecondAccount(c),new{t="msg",c=c.Id,m=msg!.Id});
 }

 // ------------------------------------------------------------------ bao cao
 public async Task Report(long conversationId,ChatReportInput i)
 {
  var (c,_,_,side1)=await Open(conversationId);
  Require(ReportReasons.Contains(i.Reason),"Chọn lý do báo cáo.",422);
  long? target=null;
  if(i.MessageId is long mid)
  {
   var m=await db.Set<TinNhanChat>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==mid&&x.ConversationId==c.Id)??throw new BusinessException("Không tìm thấy tin nhắn.",404);
   Require(m.SenderId!=UserId,"Bạn không thể báo cáo tin của chính mình.",422);target=m.SenderId;
  }
  else if(!c.IsGroup)target=c.BranchId!=null?(side1?await OwnerAccount(c.BranchId.Value):c.TravelerId):(side1?c.PeerId:c.TravelerId);
  Require(target==null||target!=UserId,"Không thể báo cáo chính mình.",422);
  var since=Clock.BayGioUtc.AddDays(-1);
  Require(await db.Set<BaoCaoChat>().CountAsync(r=>r.ReporterId==UserId&&r.CreatedAt>since)<10,"Bạn đã gửi quá nhiều báo cáo hôm nay.",429);
  Require(!await db.Set<BaoCaoChat>().AnyAsync(r=>r.ReporterId==UserId&&r.ConversationId==c.Id&&r.MessageId==i.MessageId&&r.Status=="Open"),"Bạn đã báo cáo mục này, quản trị viên đang xử lý.",409);
  db.Add(new BaoCaoChat{ReporterId=UserId,TargetId=target,ConversationId=c.Id,MessageId=i.MessageId,Reason=i.Reason,Note=string.IsNullOrWhiteSpace(i.Note)?null:Cut(i.Note.Trim(),500),Status="Open",CreatedAt=Clock.BayGioUtc});
  await db.SaveChangesAsync();
 }
 /// <summary>Hang cho bao cao cho quan tri: chi hien tin bi bao cao va vai tin ngay truoc do de hieu ngu canh.</summary>
 public async Task<List<ChatReportDto>> ReportQueue(string status)
 {
  RoleRequired(AdminRole);
  var rows=await db.Set<BaoCaoChat>().AsNoTracking().Where(r=>r.Status==status).OrderByDescending(r=>r.Id).Take(100).ToListAsync();
  var names=await Resolve<FriendService>().Names(rows.SelectMany(r=>new long?[]{r.ReporterId,r.TargetId}).Where(x=>x!=null).Select(x=>x!.Value));
  var result=new List<ChatReportDto>();
  foreach(var r in rows)
  {
   var ctx=new List<string>();
   if(r.MessageId is long mid)
   {
    var around=await db.Set<TinNhanChat>().AsNoTracking().Where(m=>m.ConversationId==r.ConversationId&&m.Id<=mid&&m.DeletedAt==null).OrderByDescending(m=>m.Id).Take(4).ToListAsync();
    ctx=around.OrderBy(m=>m.Id).Select(m=>(m.Id==mid?"▶ ":"")+(m.SenderId!=null?names.GetValueOrDefault(m.SenderId.Value,"#"+m.SenderId):"Hệ thống")+": "+Cut(m.Body,200)).ToList();
   }
   result.Add(new ChatReportDto(r.Id,r.ReporterId,names.GetValueOrDefault(r.ReporterId,"#"+r.ReporterId),r.TargetId,r.TargetId==null?null:names.GetValueOrDefault(r.TargetId.Value,"#"+r.TargetId),r.ConversationId,r.Reason,r.Note,Utc(r.CreatedAt),ctx));
  }
  return result;
 }
 public async Task CloseReport(long id)
 {
  RoleRequired(AdminRole);
  var r=await db.Set<BaoCaoChat>().SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("Không tìm thấy báo cáo.",404);
  r.Status="Done";r.HandledAt=Clock.BayGioUtc;await db.SaveChangesAsync();
 }
}
