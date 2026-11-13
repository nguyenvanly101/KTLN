using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

/// <summary>Chat nhom kieu Facebook: tao nhom tu ban be, them / xoa thanh vien, roi nhom, doi ten, da xem theo tung thanh vien.</summary>
public sealed partial class ChatService
{
 public const int MaxGroupMembers=50;
 async Task<List<ThanhVienChat>> ActiveMembers(long conversationId)=>await db.Set<ThanhVienChat>().Where(t=>t.ConversationId==conversationId&&t.LeftAt==null).ToListAsync();
 async Task NotifyGroup(long conversationId,object payload)
 {
  var j=Json(payload);
  foreach(var id in await db.Set<ThanhVienChat>().AsNoTracking().Where(t=>t.ConversationId==conversationId&&t.LeftAt==null).Select(t=>t.AccountId).ToListAsync())broker.Publish(id,j);
 }
 async Task<ChatThreadDto> GroupThread(CuocTroChuyen c)
 {
  var members=await ActiveMembers(c.Id);
  var names=await Resolve<FriendService>().Names(members.Select(m=>m.AccountId));
  var mine=members.First(m=>m.AccountId==UserId);
  var messages=await Messages(c.Id,0,60);
  var item=new ChatItemDto(c.Id,0,"",c.Title??"Nhóm",c.LastPreview,false,Utc(c.LastMessageAt),messages.Count(m=>m.Id>mine.ReadId&&m.SenderId!=UserId),c.State,c.Label,true,members.Count);
  var list=members.OrderBy(m=>m.Role=="Admin"?0:1).ThenBy(m=>names.GetValueOrDefault(m.AccountId)).Select(m=>new ChatMemberDto(m.AccountId,names.GetValueOrDefault(m.AccountId,"Người dùng"),m.Role,m.AccountId==UserId)).ToList();
  return new(item,messages,[],[],0,"Member",false,"",null,true,null,null,"",null,false,null,list,mine.Role);
 }
 async Task MarkGroupRead(CuocTroChuyen c,long last)
 {
  var me=await db.Set<ThanhVienChat>().SingleAsync(t=>t.ConversationId==c.Id&&t.AccountId==UserId&&t.LeftAt==null);
  if(me.ReadId>=last)return;
  me.ReadId=last;await db.SaveChangesAsync();
  await NotifyGroup(c.Id,new{t="read",c=c.Id,r=last,account=UserId});
 }
 async Task GroupTyping(CuocTroChuyen c)
 {
  var name=(await Resolve<FriendService>().Names([UserId]))[UserId];
  var j=Json(new{t="typing",c=c.Id,account=UserId,name});
  foreach(var id in await db.Set<ThanhVienChat>().AsNoTracking().Where(t=>t.ConversationId==c.Id&&t.LeftAt==null&&t.AccountId!=UserId).Select(t=>t.AccountId).ToListAsync())broker.Publish(id,j);
 }
 /// <summary>Ghi mot dong su kien (ai them ai, ai roi nhom, doi ten) vao nhom.</summary>
 async Task GroupEvent(long conversationId,string text)
 {
  var c=await db.Set<CuocTroChuyen>().SingleAsync(x=>x.Id==conversationId);var now=Clock.BayGioUtc;
  var m=new TinNhanChat{ConversationId=c.Id,SenderId=UserId,Side="System",Kind="Event",Body=Cut(text,MaxBody),CreatedAt=now};
  db.Add(m);await db.SaveChangesAsync();
  c.LastMessageId=m.Id;c.LastMessageAt=now;c.LastPreview=Cut(text,110);
  await db.SaveChangesAsync();
  await NotifyGroup(c.Id,new{t="msg",c=c.Id,m=m.Id});
 }
 async Task<List<long>> FriendTargets(List<long> ids)
 {
  ids=ids.Where(x=>x>0&&x!=UserId).Distinct().ToList();
  var friendIds=await Resolve<FriendService>().FriendIds(UserId);
  Require(ids.All(friendIds.Contains),"Chỉ thêm được những người đã là bạn bè của bạn.",422);
  var active=await db.TaiKhoan.AsNoTracking().Where(a=>ids.Contains(a.MaTaiKhoan)&&a.VaiTro==VaiTro.KHACH_DU_LICH&&a.TrangThai==TrangThaiTaiKhoan.HoatDong).Select(a=>a.MaTaiKhoan).ToListAsync();
  Require(active.Count==ids.Count,"Có người không còn hoạt động.",422);
  return ids;
 }
 public async Task<long> CreateGroup(ChatGroupInput i)
 {
  RoleRequired(Traveler);await CheckRate();
  var members=await FriendTargets(i.MemberIds);
  Require(members.Count>=1,"Chọn ít nhất một người bạn để tạo nhóm.",422);
  Require(members.Count+1<=MaxGroupMembers,"Nhóm tối đa "+MaxGroupMembers+" thành viên.",422);
  var day=Clock.BayGioUtc.AddDays(-1);
  Require(await db.Set<CuocTroChuyen>().CountAsync(x=>x.IsGroup&&x.TravelerId==UserId&&x.CreatedAt>day)<10,"Bạn tạo quá nhiều nhóm hôm nay.",429);
  var names=await Resolve<FriendService>().Names(members.Append(UserId));
  var title=(i.Title??"").Trim();
  if(title.Length==0)title=string.Join(", ",members.Take(3).Select(m=>names[m]))+(members.Count>3?"…":"");
  Require(title.Length<=100,"Tên nhóm tối đa 100 ký tự.",400);
  long id=0;
  await Execute(async()=>
  {
   var c=new CuocTroChuyen{IsGroup=true,Title=title,TravelerId=UserId,State="Open",LastMessageAt=Clock.BayGioUtc,CreatedAt=Clock.BayGioUtc};
   db.Add(c);await db.SaveChangesAsync();id=c.Id;
   db.Add(new ThanhVienChat{ConversationId=c.Id,AccountId=UserId,Role="Admin",JoinedAt=Clock.BayGioUtc});
   foreach(var m in members)db.Add(new ThanhVienChat{ConversationId=c.Id,AccountId=m,Role="Member",JoinedAt=Clock.BayGioUtc});
   await db.SaveChangesAsync();
   await GroupEvent(c.Id,names[UserId]+" đã tạo nhóm “"+title+"”.");
  });
  return id;
 }
 async Task<(CuocTroChuyen C,ThanhVienChat Me)> GroupOf(long id)
 {
  RoleRequired(Traveler);
  var c=await db.Set<CuocTroChuyen>().SingleOrDefaultAsync(x=>x.Id==id&&x.IsGroup)??throw new BusinessException("Không tìm thấy nhóm.",404);
  var me=await db.Set<ThanhVienChat>().SingleOrDefaultAsync(t=>t.ConversationId==id&&t.AccountId==UserId&&t.LeftAt==null)??throw new BusinessException("Không tìm thấy nhóm.",404);
  return (c,me);
 }
 public async Task AddGroupMembers(long id,ChatGroupMembersInput i)
 {
  var (c,_)=await GroupOf(id);await CheckRate();
  var ids=await FriendTargets(i.AccountIds);Require(ids.Count>0,"Chọn ít nhất một người bạn.",422);
  var current=await db.Set<ThanhVienChat>().Where(t=>t.ConversationId==id).ToListAsync();
  Require(current.Count(t=>t.LeftAt==null)+ids.Count(x=>!current.Any(t=>t.AccountId==x&&t.LeftAt==null))<=MaxGroupMembers,"Nhóm tối đa "+MaxGroupMembers+" thành viên.",422);
  var names=await Resolve<FriendService>().Names(ids.Append(UserId));
  var added=new List<string>();
  foreach(var x in ids)
  {
   var row=current.FirstOrDefault(t=>t.AccountId==x);
   if(row==null)db.Add(new ThanhVienChat{ConversationId=id,AccountId=x,Role="Member",JoinedAt=Clock.BayGioUtc,ReadId=c.LastMessageId});
   else if(row.LeftAt!=null){row.LeftAt=null;row.Role="Member";row.JoinedAt=Clock.BayGioUtc;row.ReadId=c.LastMessageId;}
   else continue;
   added.Add(names[x]);
  }
  Require(added.Count>0,"Những người này đã ở trong nhóm.",409);
  await db.SaveChangesAsync();
  await GroupEvent(id,names[UserId]+" đã thêm "+string.Join(", ",added)+" vào nhóm.");
 }
 public async Task RemoveGroupMember(long id,long accountId)
 {
  var (_,me)=await GroupOf(id);
  Require(me.Role=="Admin","Chỉ quản trị viên nhóm mới xóa được thành viên.",403);
  Require(accountId!=UserId,"Hãy dùng \"Rời nhóm\" để rời khỏi nhóm.",422);
  var target=await db.Set<ThanhVienChat>().SingleOrDefaultAsync(t=>t.ConversationId==id&&t.AccountId==accountId&&t.LeftAt==null)??throw new BusinessException("Người này không ở trong nhóm.",404);
  var names=await Resolve<FriendService>().Names([UserId,accountId]);
  target.LeftAt=Clock.BayGioUtc;await db.SaveChangesAsync();
  broker.Publish(accountId,Json(new{t="msg",c=id,m=0}));
  await GroupEvent(id,names[UserId]+" đã xóa "+names[accountId]+" khỏi nhóm.");
 }
 public async Task LeaveGroup(long id)
 {
  var (c,me)=await GroupOf(id);
  var names=await Resolve<FriendService>().Names([UserId]);
  me.LeftAt=Clock.BayGioUtc;await db.SaveChangesAsync();
  var rest=await ActiveMembers(id);
  if(rest.Count>0&&me.Role=="Admin"&&!rest.Any(t=>t.Role=="Admin")){var next=rest.OrderBy(t=>t.JoinedAt).First();next.Role="Admin";await db.SaveChangesAsync();}
  if(rest.Count>0)await GroupEvent(id,names[UserId]+" đã rời nhóm.");
 }
 public async Task RenameGroup(long id,string? title)
 {
  var (c,_)=await GroupOf(id);
  var t=(title??"").Trim();Require(t.Length is >=1 and <=100,"Tên nhóm 1–100 ký tự.",422);
  var names=await Resolve<FriendService>().Names([UserId]);
  c.Title=t;await db.SaveChangesAsync();
  await GroupEvent(id,names[UserId]+" đã đổi tên nhóm thành “"+t+"”.");
 }
}
