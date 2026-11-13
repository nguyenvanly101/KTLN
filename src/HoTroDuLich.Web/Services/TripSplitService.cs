using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

public sealed class GroupExpense
{
 public long Id { get; set; }
 public long PlanId { get; set; }
 public long PaidBy { get; set; }
 public string Title { get; set; }="";
 public decimal Amount { get; set; }
 public DateOnly SpentOn { get; set; }
 public long CreatedBy { get; set; }
 public DateTime CreatedAt { get; set; }
}
public sealed class GroupExpenseShare
{
 public long ExpenseId { get; set; }
 public long AccountId { get; set; }
 public decimal Share { get; set; }
}
public sealed class GroupExpenseInput
{
 public long PlanId { get; set; }
 [System.ComponentModel.DataAnnotations.MaxLength(200)] public string Title { get; set; }="";
 public decimal Amount { get; set; }
 public DateOnly SpentOn { get; set; }=TravelService.Today;
 public long PaidBy { get; set; }
 public List<long> Participants { get; set; }=[];
}
public sealed record ExpenseRow(long Id,string Title,decimal Amount,DateOnly SpentOn,string PaidByName,long PaidBy,List<(string Name,decimal Share)> Shares,bool CanDelete);
public sealed record Balance(long AccountId,string Name,decimal Paid,decimal Owed,decimal Net);
public sealed record Transfer(string From,string To,decimal Amount);
public sealed record SplitPage(KeHoachChuyenDi Plan,List<TripMemberDto> People,List<ExpenseRow> Expenses,List<Balance> Balances,List<Transfer> Transfers,decimal Total);

/// <summary>Chia tien nhom: ghi khoan chi, chia deu cho nguoi tham gia, tinh ai tra ai it lan chuyen nhat. Chi ghi nhan, khong xu ly tien.</summary>
public sealed class TripSplitService(AppDbContext context,IHttpContextAccessor accessor,IWebHostEnvironment environment,IConfiguration configuration,IServiceProvider provider,TripShareService share,INhatKyService audit)
 : NghiepVuService(context,accessor,environment,configuration,provider)
{
 /// <summary>Chia deu so tien nguyen (dong) cho n nguoi; phan du chia cho nhung nguoi dau.</summary>
 public static List<decimal> EqualShares(decimal amount,int people)
 {
  var baseShare=Math.Floor(amount/people);var remainder=(int)(amount-baseShare*people);
  return Enumerable.Range(0,people).Select(i=>baseShare+(i<remainder?1:0)).ToList();
 }
 /// <summary>Rut gon cong no: ghep nguoi no nhieu nhat voi nguoi duoc tra nhieu nhat cho den khi het.</summary>
 public static List<(long From,long To,decimal Amount)> Settle(Dictionary<long,decimal> net)
 {
  var debtors=net.Where(x=>x.Value<0).Select(x=>(Id:x.Key,Amt:-x.Value)).OrderByDescending(x=>x.Amt).ToList();
  var creditors=net.Where(x=>x.Value>0).Select(x=>(Id:x.Key,Amt:x.Value)).OrderByDescending(x=>x.Amt).ToList();
  var result=new List<(long,long,decimal)>();int i=0,j=0;
  while(i<debtors.Count&&j<creditors.Count)
  {
   var pay=Math.Min(debtors[i].Amt,creditors[j].Amt);
   if(pay>0)result.Add((debtors[i].Id,creditors[j].Id,pay));
   debtors[i]=(debtors[i].Id,debtors[i].Amt-pay);creditors[j]=(creditors[j].Id,creditors[j].Amt-pay);
   if(debtors[i].Amt==0)i++;if(j<creditors.Count&&creditors[j].Amt==0)j++;
  }
  return result;
 }

 public async Task<SplitPage> Page(long planId)
 {
  RoleRequired(Traveler);
  var members=await share.Members(planId);
  var accepted=members.Members.Where(m=>m.IsOwner||m.Accepted).ToList();
  var plan=await db.KeHoachChuyenDi.AsNoTracking().SingleAsync(x=>x.MaKeHoach==planId);
  var expenses=await db.Set<GroupExpense>().AsNoTracking().Where(e=>e.PlanId==planId).OrderByDescending(e=>e.SpentOn).ThenByDescending(e=>e.Id).Take(200).ToListAsync();
  var ids=expenses.Select(e=>e.Id).ToList();
  var shares=await db.Set<GroupExpenseShare>().AsNoTracking().Where(s=>ids.Contains(s.ExpenseId)).ToListAsync();
  var names=accepted.ToDictionary(m=>m.AccountId,m=>m.Name);
  string Name(long id)=>names.GetValueOrDefault(id,"Thành viên đã rời");
  var rows=expenses.Select(e=>new ExpenseRow(e.Id,e.Title,e.Amount,e.SpentOn,Name(e.PaidBy),e.PaidBy,shares.Where(s=>s.ExpenseId==e.Id).Select(s=>(Name(s.AccountId),s.Share)).ToList(),e.CreatedBy==UserId||plan.MaTaiKhoan==UserId)).ToList();
  var paid=expenses.GroupBy(e=>e.PaidBy).ToDictionary(g=>g.Key,g=>g.Sum(e=>e.Amount));
  var owed=shares.GroupBy(s=>s.AccountId).ToDictionary(g=>g.Key,g=>g.Sum(s=>s.Share));
  var allIds=paid.Keys.Concat(owed.Keys).Concat(accepted.Select(m=>m.AccountId)).Distinct().ToList();
  var balances=allIds.Select(id=>new Balance(id,Name(id),paid.GetValueOrDefault(id),owed.GetValueOrDefault(id),paid.GetValueOrDefault(id)-owed.GetValueOrDefault(id))).OrderByDescending(b=>b.Net).ToList();
  var transfers=Settle(balances.ToDictionary(b=>b.AccountId,b=>b.Net)).Select(t=>new Transfer(Name(t.From),Name(t.To),t.Amount)).ToList();
  return new(plan,accepted,rows,balances,transfers,expenses.Sum(e=>e.Amount));
 }

 public async Task Add(GroupExpenseInput i)
 {
  RoleRequired(Traveler);
  var members=await share.Members(i.PlanId);
  var accepted=members.Members.Where(m=>m.IsOwner||m.Accepted).Select(m=>m.AccountId).ToList();
  var title=(i.Title??"").Trim();Require(title.Length is >=1 and <=200,"Nhập nội dung khoản chi (tối đa 200 ký tự).",422);
  Require(i.Amount==Math.Floor(i.Amount)&&i.Amount is >=1 and <=1000000000,"Số tiền là số nguyên từ 1 đến 1.000.000.000 ₫.",422);
  Require(accepted.Contains(i.PaidBy),"Người trả phải là thành viên của chuyến đi.",422);
  var who=i.Participants.Distinct().ToList();Require(who.Count>=1&&who.All(accepted.Contains),"Chọn ít nhất một người cùng chia, chỉ trong thành viên chuyến đi.",422);
  Require(await db.Set<GroupExpense>().CountAsync(e=>e.PlanId==i.PlanId)<200,"Mỗi chuyến đi ghi tối đa 200 khoản chi nhóm.",422);
  await Execute(async()=>
  {
   var e=new GroupExpense{PlanId=i.PlanId,PaidBy=i.PaidBy,Title=title,Amount=i.Amount,SpentOn=i.SpentOn,CreatedBy=UserId,CreatedAt=Clock.BayGioUtc};
   db.Add(e);await db.SaveChangesAsync();
   var parts=who.OrderBy(x=>x).ToList();var amounts=EqualShares(i.Amount,parts.Count);
   for(var n=0;n<parts.Count;n++)db.Add(new GroupExpenseShare{ExpenseId=e.Id,AccountId=parts[n],Share=amounts[n]});
   audit.Ghi("GhiChiNhom","KhoanChiNhom",e.Id,null,i.Amount.ToString("0"),null);
  });
 }

 public async Task Delete(long planId,long id)
 {
  RoleRequired(Traveler);
  await share.Members(planId);
  await Execute(async()=>
  {
   var e=await db.Set<GroupExpense>().SingleOrDefaultAsync(x=>x.Id==id&&x.PlanId==planId)??throw new BusinessException("Không tìm thấy khoản chi.",404);
   var ownerId=(await db.KeHoachChuyenDi.AsNoTracking().SingleAsync(x=>x.MaKeHoach==planId)).MaTaiKhoan;
   Require(e.CreatedBy==UserId||ownerId==UserId,"Chỉ người ghi khoản chi hoặc chủ chuyến đi mới xóa được.",403);
   db.RemoveRange(db.Set<GroupExpenseShare>().Where(s=>s.ExpenseId==id));await db.SaveChangesAsync();db.Remove(e);
   audit.Ghi("XoaChiNhom","KhoanChiNhom",id,e.Amount.ToString("0"),null,null);
  });
 }
}
