using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

public sealed class ServiceKindInput
{
 public string Code { get; set; }="";
 public bool IsNew { get; set; }
 [System.ComponentModel.DataAnnotations.MaxLength(60)] public string Label { get; set; }="";
 public string Group { get; set; }="Other";
 public int Sort { get; set; }=200;
 public bool Bookable { get; set; }
 public bool ProviderPostable { get; set; }=true;
 public bool ForBranch { get; set; }
 public bool HasPrice { get; set; }=true;
 public bool HasHours { get; set; }
 public bool HasLocation { get; set; }=true;
 public bool TimeBound { get; set; }
 public bool Active { get; set; }=true;
 [System.ComponentModel.DataAnnotations.MaxLength(20000)] public string? FieldsText { get; set; }
}
public sealed record ServiceKindRow(KindInfo Kind,int Services,int Branches);

/// <summary>Quan tri vien tao / sua loai dich vu va bo truong rieng ma khong can sua ma nguon.</summary>
public sealed class ServiceKindAdminService(AppDbContext context,IHttpContextAccessor accessor,IWebHostEnvironment environment,IConfiguration configuration,IServiceProvider provider,INhatKyService audit)
 : NghiepVuService(context,accessor,environment,configuration,provider)
{
 public async Task<List<ServiceKindRow>> List()
 {
  RoleRequired(AdminRole);
  var services=await db.DichVuDuLich.AsNoTracking().GroupBy(d=>d.LoaiDichVu).Select(g=>new{g.Key,N=g.Count()}).ToListAsync();
  var branches=await db.Set<CoSoDichVu>().AsNoTracking().GroupBy(b=>b.Type).Select(g=>new{g.Key,N=g.Count()}).ToListAsync();
  return ServiceKinds.All.OrderBy(k=>k.Sort).Select(k=>new ServiceKindRow(k,services.Where(s=>DanhMuc.ChuanHoa(s.Key)==k.Code).Sum(s=>s.N),branches.FirstOrDefault(b=>b.Key==k.Code)?.N??0)).ToList();
 }
 public ServiceKindInput Form(string? code)
 {
  RoleRequired(AdminRole);
  if(string.IsNullOrEmpty(code))return new ServiceKindInput{IsNew=true,FieldsText="ghichu | Ghi chú thêm cho khách | text | | 300"};
  var k=ServiceKinds.Get(code)??throw new BusinessException("Không tìm thấy loại dịch vụ.",404);
  return new ServiceKindInput{Code=k.Code,Label=k.Label,Group=k.Group,Sort=k.Sort,Bookable=k.Bookable,ProviderPostable=k.ProviderPostable,ForBranch=k.ForBranch,HasPrice=k.HasPrice,HasHours=k.HasHours,HasLocation=k.HasLocation,TimeBound=k.TimeBound,Active=k.Active,FieldsText=ServiceKinds.ToLines(k.Fields)};
 }
 public async Task Save(ServiceKindInput i)
 {
  RoleRequired(AdminRole);
  var code=(i.Code??"").Trim();var label=(i.Label??"").Trim();
  Require(label.Length is >=2 and <=60,"Tên loại từ 2 đến 60 ký tự.",422);
  Require(ServiceKinds.Groups.Any(g=>g.Key==i.Group),"Chọn nhóm hợp lệ.",422);
  Require(i.Sort is >=0 and <=10000,"Thứ tự từ 0 đến 10000.",422);
  var fields=ServiceKinds.ParseLines(i.FieldsText);
  if(i.IsNew)
  {
   Require(System.Text.RegularExpressions.Regex.IsMatch(code,"^[A-Za-z][A-Za-z0-9]{2,19}$"),"Mã loại gồm chữ và số, bắt đầu bằng chữ, 3 đến 20 ký tự.",422);
   Require(!await db.Set<LoaiDichVuRow>().AnyAsync(r=>r.Code==code),"Mã loại đã tồn tại.",409);
  }
  await Execute(async()=>
  {
   LoaiDichVuRow row;
   if(i.IsNew){row=new LoaiDichVuRow{Code=code,Builtin=false};db.Add(row);}
   else row=await db.Set<LoaiDichVuRow>().SingleOrDefaultAsync(r=>r.Code==code)??throw new BusinessException("Không tìm thấy loại dịch vụ.",404);
   if(row.Builtin&&!i.Active&&row.Code=="Khac")throw new BusinessException("Không tắt được loại \"Khác\".",422);
   row.Label=label;row.GroupCode=i.Group;row.SortOrder=i.Sort;row.Bookable=i.Bookable;row.ProviderPostable=i.ProviderPostable;row.ForBranch=i.ForBranch;
   row.HasPrice=i.HasPrice;row.HasHours=i.HasHours;row.HasLocation=i.HasLocation;row.TimeBound=i.TimeBound;row.Active=i.Active;
   row.FieldsJson=ServiceKinds.FieldsJson(fields);row.UpdatedAt=Clock.BayGioUtc;
   audit.Ghi(i.IsNew?"TaoLoaiDichVu":"SuaLoaiDichVu","LoaiDichVu",0,null,row.Code,null);
  });
  await ServiceKinds.LoadAsync(db);
 }
 public async Task Delete(string code)
 {
  RoleRequired(AdminRole);
  await Execute(async()=>
  {
   var row=await db.Set<LoaiDichVuRow>().SingleOrDefaultAsync(r=>r.Code==code)??throw new BusinessException("Không tìm thấy loại dịch vụ.",404);
   Require(!row.Builtin,"Không xóa được loại có sẵn của hệ thống. Bạn có thể tắt loại này.",422);
   Require(!await db.DichVuDuLich.AnyAsync(d=>d.LoaiDichVu==code)&&!await db.Set<CoSoDichVu>().AnyAsync(b=>b.Type==code),"Đã có dịch vụ hoặc cơ sở dùng loại này. Hãy tắt thay vì xóa.",409);
   db.Remove(row);
   audit.Ghi("XoaLoaiDichVu","LoaiDichVu",0,code,null,null);
  });
  await ServiceKinds.LoadAsync(db);
 }
}
