using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

/// <summary>Thao tac voi tung tin: tra loi, sua, thu hoi, bieu cam, vi tri, tep PDF.</summary>
public sealed partial class ChatService
{
 public const int EditMinutes=15,RecallMinutes=10;
 static readonly string[] Reactions=["like","love","haha","wow","sad","angry"];

 /// <summary>Chuyen tin thanh DTO: nguoi gui, "mine", tin duoc tra loi, bieu cam.</summary>
 async Task<List<ChatMessageDto>> ToDtos(CuocTroChuyen cv,bool side1,List<TinNhanChat> rows)
 {
  var replyIds=rows.Where(m=>m.ReplyToId!=null).Select(m=>m.ReplyToId!.Value).Distinct().ToList();
  var replied=replyIds.Count==0?new List<TinNhanChat>():await db.Set<TinNhanChat>().AsNoTracking().Where(m=>replyIds.Contains(m.Id)&&m.ConversationId==cv.Id).ToListAsync();
  var wantNames=cv.IsGroup||replied.Count>0;
  var senderIds=rows.Where(m=>m.SenderId!=null).Select(m=>m.SenderId!.Value).Concat(replied.Where(m=>m.SenderId!=null).Select(m=>m.SenderId!.Value)).Distinct().ToList();
  var names=wantNames?await friendsNames(senderIds):new Dictionary<long,string>();
  var ids=rows.Select(m=>m.Id).ToList();
  var reacts=await db.Set<PhanUngTin>().AsNoTracking().Where(r=>ids.Contains(r.MessageId)).Select(r=>new{r.MessageId,r.AccountId,r.Emoji}).ToListAsync();
  bool Mine(TinNhanChat m)=>cv.IsGroup?m.SenderId==UserId:(m.Side=="System"?false:(m.Side=="Traveler")==side1);
  string ReplyText(TinNhanChat r)=>r.DeletedAt!=null?"Tin nhắn đã được thu hồi":r.Kind switch{"Image"=>"[Ảnh]","Location"=>"[Vị trí]","File"=>"[Tệp] "+r.FileName,"Booking"=>"[Yêu cầu đặt]","Connection"=>"[Lời rủ đi cùng]",_=>Cut(r.Body.Replace('\n',' '),80)};
  return rows.Select(m=>
  {
   var r=m.ReplyToId==null?null:replied.FirstOrDefault(x=>x.Id==m.ReplyToId);
   var rAuthor=r==null?null:r.SenderId==UserId?"Bạn":cv.IsGroup||cv.BranchId==null?(r.SenderId!=null?names.GetValueOrDefault(r.SenderId.Value):null):(r.Side=="Branch"?"Cơ sở":"Khách");
   var mine=reacts.Where(x=>x.MessageId==m.Id&&x.AccountId==UserId).Select(x=>x.Emoji).FirstOrDefault();
   var counts=reacts.Where(x=>x.MessageId==m.Id).GroupBy(x=>x.Emoji).Select(g=>new ChatReactionDto(g.Key,g.Count(),g.Key==mine)).ToList();
   var deleted=m.DeletedAt!=null;
   return new ChatMessageDto(m.Id,m.Side,m.Kind,deleted?"":m.Body,!deleted&&m.ImagePath!=null,Utc(m.CreatedAt),m.BookingRequestId,m.ConnectionId,m.SenderId,m.SenderId!=null&&cv.IsGroup?names.GetValueOrDefault(m.SenderId.Value):null,Mine(m),
    m.ReplyToId,r==null?null:ReplyText(r),rAuthor,m.EditedAt!=null&&!deleted,deleted,deleted?null:m.Lat,deleted?null:m.Lng,deleted?null:m.FileName,!deleted&&m.FilePath!=null,counts.Count==0?null:counts,m.AgreementId);
  }).ToList();
 }
 public async Task<ChatMessageDto> GetMessage(long conversationId,long messageId)
 {
  var (cv,_,_,side1)=await Open(conversationId);
  var m=await db.Set<TinNhanChat>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==messageId&&x.ConversationId==conversationId)??throw new BusinessException("Không tìm thấy tin nhắn.",404);
  return (await ToDtos(cv,side1,[m]))[0];
 }
 async Task PublishUpdate(CuocTroChuyen c,long messageId)
 {
  var payload=new{t="upd",c=c.Id,m=messageId};
  if(c.IsGroup)await NotifyGroup(c.Id,payload);else Notify(c.TravelerId,await SecondAccount(c),payload);
 }
 async Task<(CuocTroChuyen C,TinNhanChat M)> OwnMessage(long conversationId,long messageId,int minutes,params string[] kinds)
 {
  var (c,_,_,_)=await Open(conversationId,true);
  var m=await db.Set<TinNhanChat>().SingleOrDefaultAsync(x=>x.Id==messageId&&x.ConversationId==conversationId)??throw new BusinessException("Không tìm thấy tin nhắn.",404);
  Require(m.SenderId==UserId&&m.DeletedAt==null,"Bạn chỉ thao tác được với tin chưa thu hồi do chính bạn gửi.",403);
  Require(kinds.Contains(m.Kind),"Loại tin này không thao tác được.",422);
  Require(Clock.BayGioUtc-m.CreatedAt<=TimeSpan.FromMinutes(minutes),"Đã quá "+minutes+" phút, không thể thực hiện.",409);
  return (c,m);
 }
 public async Task EditMessage(long conversationId,long messageId,string? text)
 {
  var (c,m)=await OwnMessage(conversationId,messageId,EditMinutes,"Text");
  var body=(text??"").Replace("\r\n","\n").Trim();
  Require(body.Length>0&&body.Length<=MaxBody,"Tin nhắn 1–"+MaxBody+" ký tự.",422);
  m.Body=body;m.EditedAt=Clock.BayGioUtc;
  if(c.LastMessageId==m.Id)c.LastPreview=Preview(body,false);
  await db.SaveChangesAsync();await PublishUpdate(c,m.Id);
 }
 public async Task RecallMessage(long conversationId,long messageId)
 {
  var (c,m)=await OwnMessage(conversationId,messageId,RecallMinutes,"Text","Image","Location","File");
  var files=new[]{m.ImagePath,m.FilePath}.Where(x=>x!=null).Select(x=>x!).ToList();
  m.DeletedAt=Clock.BayGioUtc;m.Body="";m.ImagePath=null;m.FilePath=null;m.FileName=null;m.Lat=null;m.Lng=null;
  db.RemoveRange(await db.Set<PhanUngTin>().Where(r=>r.MessageId==m.Id).ToListAsync());
  if(c.LastMessageId==m.Id)c.LastPreview="Tin nhắn đã được thu hồi";
  await db.SaveChangesAsync();
  foreach(var f in files){try{var path=Resolve<ILuuTepService>().DuongDanAnToan(f);if(System.IO.File.Exists(path))System.IO.File.Delete(path);}catch{/* tep se duoc don sau */}}
  await PublishUpdate(c,m.Id);
 }
 /// <summary>Tha / doi / bo bieu cam voi mot tin. Chon lai cung bieu cam thi bo.</summary>
 public async Task React(long conversationId,long messageId,string? emoji)
 {
  var (c,_,_,_)=await Open(conversationId);
  emoji=string.IsNullOrWhiteSpace(emoji)?"like":emoji;Require(Reactions.Contains(emoji),"Biểu cảm không hợp lệ.",400);
  var m=await db.Set<TinNhanChat>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==messageId&&x.ConversationId==conversationId&&x.DeletedAt==null)??throw new BusinessException("Không tìm thấy tin nhắn.",404);
  Require(m.Kind!="Event"&&m.Kind!="Auto","Không thả biểu cảm với tin này.",422);
  var row=await db.Set<PhanUngTin>().SingleOrDefaultAsync(x=>x.MessageId==messageId&&x.AccountId==UserId);
  if(row==null)db.Add(new PhanUngTin{MessageId=messageId,AccountId=UserId,Emoji=emoji,CreatedAt=Clock.BayGioUtc});
  else if(row.Emoji==emoji)db.Remove(row);
  else row.Emoji=emoji;
  try{await db.SaveChangesAsync();}catch(DbUpdateException){}
  await PublishUpdate(c,messageId);
 }
 public async Task<(string Path,string Name)> FileOf(long conversationId,long messageId)
 {
  await Open(conversationId);
  var m=await db.Set<TinNhanChat>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==messageId&&x.ConversationId==conversationId&&x.FilePath!=null&&x.DeletedAt==null)??throw new BusinessException("Không tìm thấy tệp.",404);
  var path=Resolve<ILuuTepService>().DuongDanAnToan(m.FilePath!);
  Require(System.IO.File.Exists(path)&&Path.GetExtension(path).Equals(".pdf",StringComparison.OrdinalIgnoreCase),"Không tìm thấy tệp.",404);
  return (path,m.FileName??"tep.pdf");
 }
}
