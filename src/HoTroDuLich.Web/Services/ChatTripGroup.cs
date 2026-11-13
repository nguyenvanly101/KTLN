using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

/// <summary>Nhom chat tu dong cua chuyen di chung: co tu 2 nguoi tro len (chu chuyen + thanh vien da tham gia) thi tao nhom, dong bo thanh vien va ten.</summary>
public sealed partial class ChatService
{
 /// <summary>Dong bo nhom chat cua chuyen di. Goi sau khi co nguoi tham gia, roi di, hoac doi ten chuyen di. Tra ve ma cuoc tro chuyen (neu co nhom).</summary>
 public async Task<long?> SyncTripGroup(long planId)
 {
  var plan=await db.KeHoachChuyenDi.AsNoTracking().SingleOrDefaultAsync(x=>x.MaKeHoach==planId);
  if(plan==null)return null;
  var accepted=await db.Set<ThanhVienChuyenDi>().AsNoTracking().Where(m=>m.PlanId==planId&&m.Status=="Accepted").Select(m=>m.AccountId).ToListAsync();
  var wanted=accepted.Append(plan.MaTaiKhoan).Distinct().ToList();
  var link=await db.Set<TripChatLink>().SingleOrDefaultAsync(x=>x.PlanId==planId);
  if(link==null&&wanted.Count<2)return null;
  var title=Cut("Chuyến đi: "+plan.TenKeHoach,100);
  var names=await Resolve<FriendService>().Names(wanted);
  var now=Clock.BayGioUtc;
  CuocTroChuyen c;
  var events=new List<string>();
  if(link==null)
  {
   c=new CuocTroChuyen{IsGroup=true,Title=title,TravelerId=plan.MaTaiKhoan,State="Open",LastMessageAt=now,CreatedAt=now};
   db.Add(c);await db.SaveChangesAsync();
   db.Add(new TripChatLink{PlanId=planId,ConversationId=c.Id,CreatedAt=now});
   db.Add(new ThanhVienChat{ConversationId=c.Id,AccountId=plan.MaTaiKhoan,Role="Admin",JoinedAt=now});
   foreach(var m in wanted.Where(x=>x!=plan.MaTaiKhoan))db.Add(new ThanhVienChat{ConversationId=c.Id,AccountId=m,Role="Member",JoinedAt=now});
   await db.SaveChangesAsync();
   events.Add("Nhóm được tạo tự động cho chuyến đi “"+plan.TenKeHoach+"”. Trưởng nhóm: "+names.GetValueOrDefault(plan.MaTaiKhoan,"chủ chuyến đi")+".");
  }
  else
  {
   c=await db.Set<CuocTroChuyen>().SingleAsync(x=>x.Id==link.ConversationId);
   var current=await db.Set<ThanhVienChat>().Where(t=>t.ConversationId==c.Id).ToListAsync();
   foreach(var m in wanted)
   {
    var row=current.FirstOrDefault(t=>t.AccountId==m);
    if(row==null){db.Add(new ThanhVienChat{ConversationId=c.Id,AccountId=m,Role=m==plan.MaTaiKhoan?"Admin":"Member",JoinedAt=now,ReadId=c.LastMessageId});events.Add(names.GetValueOrDefault(m,"Một thành viên")+" đã tham gia nhóm.");}
    else if(row.LeftAt!=null){row.LeftAt=null;row.JoinedAt=now;row.ReadId=c.LastMessageId;events.Add(names.GetValueOrDefault(m,"Một thành viên")+" đã tham gia lại nhóm.");}
   }
   foreach(var row in current.Where(t=>t.LeftAt==null&&!wanted.Contains(t.AccountId))){row.LeftAt=now;events.Add("Một thành viên đã rời chuyến đi và nhóm.");}
   if(c.Title!=title){c.Title=title;events.Add("Tên nhóm đổi theo tên chuyến đi: “"+plan.TenKeHoach+"”.");}
   await db.SaveChangesAsync();
  }
  foreach(var text in events)await GroupEvent(c.Id,text);
  return c.Id;
 }

 /// <summary>Ma nhom chat cua chuyen di neu nguoi xem la thanh vien dang hoat dong cua nhom.</summary>
 public async Task<long?> TripGroupFor(long planId)
 {
  var id=await db.Set<TripChatLink>().AsNoTracking().Where(x=>x.PlanId==planId).Select(x=>(long?)x.ConversationId).SingleOrDefaultAsync();
  if(id==null)return null;
  return await db.Set<ThanhVienChat>().AsNoTracking().AnyAsync(t=>t.ConversationId==id&&t.AccountId==UserId&&t.LeftAt==null)?id:null;
 }
}
