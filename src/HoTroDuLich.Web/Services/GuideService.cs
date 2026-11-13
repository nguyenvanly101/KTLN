using System.Globalization;
using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

public sealed class TemplateInput
{
 public long Id { get; set; }
 public long DonViId { get; set; }
 [System.ComponentModel.DataAnnotations.MaxLength(150)] public string Title { get; set; }="";
 public int Days { get; set; }=3;
 public string Style { get; set; }="CanBang";
 [System.ComponentModel.DataAnnotations.MaxLength(600)] public string? Summary { get; set; }
 public string Status { get; set; }="Draft";
 /// <summary>Moi dong mot hang muc: "ngay | gio (HH:mm, co the de trong) | noi dung | chi phi (co the de trong)".</summary>
 [System.ComponentModel.DataAnnotations.MaxLength(8000)] public string? ItemsText { get; set; }
}
public sealed record TemplateCard(LichTrinhMau Template,string Destination);
public sealed record TemplateDetail(LichTrinhMau Template,DonViHanhChinh Unit,List<LichTrinhMauMuc> Items,decimal Total);

/// <summary>Lich trinh mau: quan tri soan, khach xem va chep lich ve ke hoach cua minh. (Cam nang la mot loai dich vu "CamNang", xem ServiceKinds.)</summary>
public sealed class GuideService(AppDbContext context,IHttpContextAccessor accessor,IWebHostEnvironment environment,IConfiguration configuration,IServiceProvider provider,INhatKyService audit)
 : NghiepVuService(context,accessor,environment,configuration,provider)
{
 public static readonly (string Key,string Label)[] Styles=[("TietKiem","Tiết kiệm"),("CanBang","Cân bằng"),("TraiNghiem","Trải nghiệm")];
 string? Clean(string? s,int max){s=string.IsNullOrWhiteSpace(s)?null:s.Replace("\r\n","\n").Trim();Require(s==null||s.Length<=max,"Nội dung quá dài.",400);return s;}

 // ------------------------------------------------------------------ lich mau
 public async Task<List<TemplateCard>> Published(long? donViId=null,string? style=null,int? days=null)
 {
  var q=db.Set<LichTrinhMau>().AsNoTracking().Where(t=>t.Status=="Published");
  if(donViId!=null)q=q.Where(t=>t.DonViId==donViId);
  if(!string.IsNullOrEmpty(style))q=q.Where(t=>t.Style==style);
  if(days!=null)q=q.Where(t=>t.Days==days);
  var rows=await q.OrderByDescending(t=>t.Uses).ThenByDescending(t=>t.Id).Take(60).ToListAsync();
  var ids=rows.Select(t=>t.DonViId).Distinct().ToList();
  var names=await db.Set<DonViHanhChinh>().AsNoTracking().Where(u=>ids.Contains(u.Id)).ToDictionaryAsync(u=>u.Id,u=>u.Name);
  return rows.Select(t=>new TemplateCard(t,names.GetValueOrDefault(t.DonViId,""))).ToList();
 }
 public async Task<TemplateDetail> Detail(long id,bool admin=false)
 {
  var t=await db.Set<LichTrinhMau>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id&&(admin||x.Status=="Published"))??throw new BusinessException("Không tìm thấy lịch trình mẫu.",404);
  var unit=await db.Set<DonViHanhChinh>().AsNoTracking().SingleAsync(u=>u.Id==t.DonViId);
  var items=await db.Set<LichTrinhMauMuc>().AsNoTracking().Where(m=>m.TemplateId==id).OrderBy(m=>m.DayNo).ThenBy(m=>m.SortOrder).ToListAsync();
  return new(t,unit,items,items.Sum(m=>m.Cost??0));
 }
 public async Task<List<TemplateCard>> AllForAdmin()
 {
  RoleRequired(AdminRole);
  var rows=await db.Set<LichTrinhMau>().AsNoTracking().OrderByDescending(t=>t.UpdatedAt).Take(200).ToListAsync();
  var ids=rows.Select(t=>t.DonViId).Distinct().ToList();
  var names=await db.Set<DonViHanhChinh>().AsNoTracking().Where(u=>ids.Contains(u.Id)).ToDictionaryAsync(u=>u.Id,u=>u.Name);
  return rows.Select(t=>new TemplateCard(t,names.GetValueOrDefault(t.DonViId,""))).ToList();
 }
 public async Task<TemplateInput> TemplateForm(long id,long? donViId=null)
 {
  RoleRequired(AdminRole);
  if(id==0)return new TemplateInput{DonViId=donViId??0};
  var d=await Detail(id,true);
  var lines=d.Items.Select(m=>string.Join(" | ",m.DayNo,m.Time?.ToString("HH:mm")??"",m.Content,m.Cost?.ToString("0",CultureInfo.InvariantCulture)??""));
  return new TemplateInput{Id=id,DonViId=d.Template.DonViId,Title=d.Template.Title,Days=d.Template.Days,Style=d.Template.Style,Summary=d.Template.Summary,Status=d.Template.Status,ItemsText=string.Join("\n",lines)};
 }
 static List<(int Day,TimeOnly? Time,string Content,decimal? Cost)> ParseItems(string? text,int days)
 {
  var result=new List<(int,TimeOnly?,string,decimal?)>();
  foreach(var raw in (text??"").Replace("\r","").Split('\n',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries))
  {
   var p=raw.Split('|',4,StringSplitOptions.TrimEntries);
   if(p.Length<3)throw new BusinessException("Dòng \""+raw+"\" cần dạng: ngày | giờ | nội dung | chi phí.",422);
   if(!int.TryParse(p[0],NumberStyles.None,CultureInfo.InvariantCulture,out var day)||day<1||day>days)throw new BusinessException("Dòng \""+raw+"\": ngày phải từ 1 đến "+days+".",422);
   TimeOnly? time=null;if(p[1].Length>0){if(!TimeOnly.TryParseExact(p[1],"HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out var t))throw new BusinessException("Dòng \""+raw+"\": giờ cần dạng HH:mm.",422);time=t;}
   if(p[2].Length is 0 or >300)throw new BusinessException("Dòng \""+raw+"\": nội dung từ 1 đến 300 ký tự.",422);
   decimal? cost=null;if(p.Length==4&&p[3].Length>0){var digits=System.Text.RegularExpressions.Regex.Replace(p[3],@"[\.\s,₫đ]|vnđ|vnd","",System.Text.RegularExpressions.RegexOptions.IgnoreCase);if(!decimal.TryParse(digits,NumberStyles.None,CultureInfo.InvariantCulture,out var c)||c>1_000_000_000)throw new BusinessException("Dòng \""+raw+"\": chi phí không hợp lệ.",422);cost=c;}
   result.Add((day,time,p[2],cost));
  }
  if(result.Count>60)throw new BusinessException("Tối đa 60 hạng mục cho một lịch mẫu.",422);
  return result;
 }
 public async Task<long> SaveTemplate(TemplateInput i)
 {
  RoleRequired(AdminRole);
  var title=(i.Title??"").Trim();Require(title.Length is >=3 and <=150,"Tên lịch trình từ 3 đến 150 ký tự.",422);
  Require(i.Days is >=1 and <=14,"Số ngày từ 1 đến 14.",422);
  Require(Styles.Any(s=>s.Key==i.Style),"Chọn phong cách.",422);
  Require(i.Status is "Draft" or "Published","Trạng thái không hợp lệ.",422);
  Require(await db.Set<DonViHanhChinh>().AnyAsync(u=>u.Id==i.DonViId),"Chọn điểm đến hợp lệ.",422);
  var items=ParseItems(i.ItemsText,i.Days);
  Require(i.Status=="Draft"||items.Count>=2,"Lịch trình công bố cần ít nhất 2 hạng mục.",422);
  long id=i.Id;
  await Execute(async()=>
  {
   LichTrinhMau t;
   if(i.Id==0){t=new LichTrinhMau{CreatedBy=UserId,CreatedAt=Clock.BayGioUtc};db.Add(t);}
   else{t=await db.Set<LichTrinhMau>().SingleOrDefaultAsync(x=>x.Id==i.Id)??throw new BusinessException("Không tìm thấy lịch trình mẫu.",404);db.RemoveRange(db.Set<LichTrinhMauMuc>().Where(m=>m.TemplateId==t.Id));}
   t.DonViId=i.DonViId;t.Title=title;t.Days=i.Days;t.Style=i.Style;t.Summary=Clean(i.Summary,600);t.Status=i.Status;t.UpdatedAt=Clock.BayGioUtc;
   await db.SaveChangesAsync();
   var order=0;foreach(var it in items)db.Add(new LichTrinhMauMuc{TemplateId=t.Id,DayNo=it.Day,SortOrder=++order,Time=it.Time,Content=it.Content,Cost=it.Cost});
   audit.Ghi("SoanLichMau","LichTrinhMau",t.Id,null,t.Status,null);
   id=t.Id;
  });
  return id;
 }
 public async Task DeleteTemplate(long id)
 {
  RoleRequired(AdminRole);
  await Execute(async()=>
  {
   var t=await db.Set<LichTrinhMau>().SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("Không tìm thấy lịch trình mẫu.",404);
   db.RemoveRange(db.Set<LichTrinhMauMuc>().Where(m=>m.TemplateId==id));await db.SaveChangesAsync();db.Remove(t);
   audit.Ghi("XoaLichMau","LichTrinhMau",id,t.Status,null,null);
  });
 }
 /// <summary>Khach chep lich mau thanh mot ke hoach moi (ngay bat dau do khach chon). Tra ve ma ke hoach.</summary>
 public async Task<long> UseTemplate(long id,DateOnly start)
 {
  RoleRequired(Traveler);
  var d=await Detail(id);
  Require(start>=TravelService.Today,"Ngày bắt đầu không được nằm trong quá khứ.",422);
  var travel=Resolve<TravelService>();
  var planId=await travel.SavePlan(new PlanInput{Id=0,Version="0",Name=d.Template.Title,Destination=d.Unit.Name,Start=start,End=start.AddDays(d.Template.Days-1),Budget=d.Total>0?d.Total:null});
  foreach(var m in d.Items)
  {
   var plan=await travel.OwnPlan(planId);
   await travel.SaveActivity(new ActivityInput{Id=0,PlanId=planId,Version=TravelService.Token(plan.RowVersion),Date=start.AddDays(m.DayNo-1),Time=m.Time,Content=m.Content,Cost=m.Cost});
  }
  await Execute(async()=>{var t=await db.Set<LichTrinhMau>().SingleAsync(x=>x.Id==id);t.Uses++;});
  return planId;
 }
}
