using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

public sealed record TripMemberDto(long AccountId,string Name,bool Accepted,bool IsOwner);
public sealed record TripInviteDto(long PlanId,string PlanName,string Destination,DateOnly Start,DateOnly End,string Inviter);
public sealed record TripMembersPage(List<TripMemberDto> Members,List<PersonDto> InvitableFriends,bool IsOwner,bool IsMember);

/// <summary>Chuyen di chung: chu chuyen di moi ban be (chi ban be), ban be chap nhan thi cung xem va sua hang muc.</summary>
public sealed class TripShareService(AppDbContext context,IHttpContextAccessor accessor,IWebHostEnvironment environment,IConfiguration configuration,IServiceProvider provider,FriendService friends,IThongBaoService notifications)
 : NghiepVuService(context,accessor,environment,configuration,provider)
{
 public const int MaxMembers=5;
 static DateTime Utc(DateTime d)=>DateTime.SpecifyKind(d,DateTimeKind.Utc);

 async Task<KeHoachChuyenDi> OwnerPlan(long planId)
 {
  RoleRequired(Traveler);
  return await db.KeHoachChuyenDi.SingleOrDefaultAsync(x=>x.MaKeHoach==planId&&x.MaTaiKhoan==UserId)??throw new BusinessException("Không tìm thấy chuyến đi của bạn.",404);
 }

 public async Task<TripMembersPage> Members(long planId)
 {
  RoleRequired(Traveler);
  var plan=await db.KeHoachChuyenDi.AsNoTracking().SingleOrDefaultAsync(x=>x.MaKeHoach==planId)??throw new BusinessException("Không tìm thấy chuyến đi.",404);
  var rows=await db.Set<ThanhVienChuyenDi>().AsNoTracking().Where(m=>m.PlanId==planId).ToListAsync();
  var isOwner=plan.MaTaiKhoan==UserId;var isMember=rows.Any(m=>m.AccountId==UserId&&m.Status=="Accepted");
  if(!isOwner&&!isMember)throw new BusinessException("Không tìm thấy chuyến đi.",404);
  var ids=rows.Select(m=>m.AccountId).Append(plan.MaTaiKhoan).Distinct().ToList();
  var names=await friends.Names(ids);
  var list=new List<TripMemberDto>{new(plan.MaTaiKhoan,names.GetValueOrDefault(plan.MaTaiKhoan,"Chủ chuyến đi"),true,true)};
  list.AddRange(rows.OrderBy(m=>m.CreatedAt).Select(m=>new TripMemberDto(m.AccountId,names.GetValueOrDefault(m.AccountId,"Người dùng"),m.Status=="Accepted",false)));
  var invitable=new List<PersonDto>();
  if(isOwner)
  {
   var taken=ids.ToHashSet();
   invitable=(await friends.Friends(null)).Where(f=>!taken.Contains(f.Id)).ToList();
  }
  return new(list,invitable,isOwner,isMember);
 }

 public async Task Invite(long planId,long accountId)
 {
  var plan=await OwnerPlan(planId);
  Require(accountId!=UserId,"Bạn đã là chủ chuyến đi.",400);
  Require(plan.NgayKetThuc>=TravelService.Today,"Chuyến đi đã kết thúc.",422);
  Require(await friends.AreFriends(UserId,accountId),"Chỉ mời được bạn bè của bạn.",403);
  Require(await db.Set<ThanhVienChuyenDi>().CountAsync(m=>m.PlanId==planId)<MaxMembers,"Mỗi chuyến đi chung tối đa "+MaxMembers+" thành viên được mời.",422);
  Require(!await db.Set<ThanhVienChuyenDi>().AnyAsync(m=>m.PlanId==planId&&m.AccountId==accountId),"Người này đã được mời.",409);
  await Execute(async()=>
  {
   db.Add(new ThanhVienChuyenDi{PlanId=planId,AccountId=accountId,InvitedBy=UserId,Status="Pending",CreatedAt=Clock.BayGioUtc});
   var who=(await friends.Names([UserId]))[UserId];
   notifications.Tao(accountId,"MoiChuyenDi",who+" mời bạn cùng lên kế hoạch chuyến đi \""+plan.TenKeHoach+"\".","/Plans");
  });
 }

 public async Task Respond(long planId,bool accept)
 {
  RoleRequired(Traveler);
  await Execute(async()=>
  {
   var m=await db.Set<ThanhVienChuyenDi>().SingleOrDefaultAsync(x=>x.PlanId==planId&&x.AccountId==UserId&&x.Status=="Pending")??throw new BusinessException("Không còn lời mời này.",404);
   var plan=await db.KeHoachChuyenDi.AsNoTracking().SingleAsync(x=>x.MaKeHoach==planId);
   var who=(await friends.Names([UserId]))[UserId];
   if(accept){m.Status="Accepted";m.RespondedAt=Clock.BayGioUtc;notifications.Tao(plan.MaTaiKhoan,"ChuyenDiChap",who+" đã tham gia chuyến đi \""+plan.TenKeHoach+"\".","/Plans/Detail/"+planId);}
   else{db.Remove(m);notifications.Tao(plan.MaTaiKhoan,"ChuyenDiTuChoi",who+" từ chối lời mời vào chuyến đi \""+plan.TenKeHoach+"\".","/Plans/Detail/"+planId);}
  });
  if(accept)await Resolve<ChatService>().SyncTripGroup(planId);
 }

 /// <summary>Chu chuyen di go thanh vien hoac thanh vien tu roi chuyen di.</summary>
 public async Task Remove(long planId,long accountId)
 {
  RoleRequired(Traveler);
  await Execute(async()=>
  {
   var plan=await db.KeHoachChuyenDi.AsNoTracking().SingleOrDefaultAsync(x=>x.MaKeHoach==planId)??throw new BusinessException("Không tìm thấy chuyến đi.",404);
   Require(plan.MaTaiKhoan==UserId||accountId==UserId,"Bạn không có quyền với chuyến đi này.",403);
   var m=await db.Set<ThanhVienChuyenDi>().SingleOrDefaultAsync(x=>x.PlanId==planId&&x.AccountId==accountId)??throw new BusinessException("Không tìm thấy thành viên.",404);
   db.Remove(m);
   if(plan.MaTaiKhoan!=UserId){var who=(await friends.Names([UserId]))[UserId];notifications.Tao(plan.MaTaiKhoan,"ChuyenDiRoi",who+" đã rời chuyến đi \""+plan.TenKeHoach+"\".","/Plans/Detail/"+planId);}
  });
  await Resolve<ChatService>().SyncTripGroup(planId);
 }

 public async Task<List<TripInviteDto>> Invites()
 {
  RoleRequired(Traveler);
  var rows=await (from m in db.Set<ThanhVienChuyenDi>().AsNoTracking() join p in db.KeHoachChuyenDi.AsNoTracking() on m.PlanId equals p.MaKeHoach
   where m.AccountId==UserId&&m.Status=="Pending"&&p.NgayKetThuc>=TravelService.Today orderby m.CreatedAt descending select new{p.MaKeHoach,p.TenKeHoach,p.NoiDen,p.NgayBatDau,p.NgayKetThuc,m.InvitedBy}).ToListAsync();
  var names=await friends.Names(rows.Select(r=>r.InvitedBy));
  return rows.Select(r=>new TripInviteDto(r.MaKeHoach,r.TenKeHoach,r.NoiDen,r.NgayBatDau,r.NgayKetThuc,names.GetValueOrDefault(r.InvitedBy,"Bạn bè"))).ToList();
 }

 public async Task<List<KeHoachChuyenDi>> SharedWithMe()
 {
  RoleRequired(Traveler);
  return await (from m in db.Set<ThanhVienChuyenDi>().AsNoTracking() join p in db.KeHoachChuyenDi.AsNoTracking() on m.PlanId equals p.MaKeHoach
   where m.AccountId==UserId&&m.Status=="Accepted" orderby p.NgayBatDau descending select p).ToListAsync();
 }
}
