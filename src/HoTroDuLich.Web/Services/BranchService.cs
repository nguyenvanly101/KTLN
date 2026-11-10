using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

public sealed class BranchService(AppDbContext context,IHttpContextAccessor accessor,IWebHostEnvironment environment,IConfiguration configuration,IServiceProvider provider,GoogleMapsClient maps,INhatKyService audit)
 : NghiepVuService(context,accessor,environment,configuration,provider)
{
 static readonly string[] Fields=["Name","Type","Region","Address","Summary","Phone","Website","Hours","Parking","VegetarianOptions","VeganOnly","AccessibleEntrance","GooglePlaceId","Latitude","Longitude","LocationSource"];
 public IQueryable<CoSoDichVu> PublicBranches()=>db.Set<CoSoDichVu>().Where(b=>b.Status=="Approved"&&b.PublishedVersion!=null&&db.NhaCungCapDichVu.Any(p=>p.MaNhaCungCap==b.ProviderId&&p.TrangThaiQuyenHoatDong==TrangThaiQuyenHoatDong.DangHoatDong&&p.TrangThaiXacMinh==TrangThaiXacMinh.DaXacMinh&&db.TaiKhoan.Any(a=>a.MaTaiKhoan==p.MaTaiKhoan&&a.TrangThai==TrangThaiTaiKhoan.HoatDong)));
 public IQueryable<MocDiaDiem> PublicLandmarks()=>db.Set<MocDiaDiem>().Where(x=>x.IsPublished&&(x.ParentId==null||db.Set<MocDiaDiem>().Any(p=>p.Id==x.ParentId&&p.IsPublished)));

 public async Task<List<BranchChoice>> Choices()
 {
  RoleRequired(ProviderRole);var p=await MyProvider();
  return await PublicBranches().AsNoTracking().Where(b=>b.ProviderId==p.MaNhaCungCap).OrderBy(b=>b.Name).Select(b=>new BranchChoice(b.Id,b.Name,b.Address,b.Type)).ToListAsync();
 }
 public async Task<List<long>> ServiceBranches(long serviceId,long? revisionId=null)
 {
  RoleRequired(ProviderRole,AdminRole);
  if(Role==ProviderRole){var p=await MyProvider();Require(await db.DichVuDuLich.AnyAsync(s=>s.MaDichVu==serviceId&&s.MaNhaCungCap==p.MaNhaCungCap),"Không tìm thấy dịch vụ.",404);}
  if(revisionId!=null){Require(await db.BanCapNhatDichVu.AnyAsync(r=>r.MaBanCapNhat==revisionId&&r.MaDichVu==serviceId),"Phiên bản không thuộc dịch vụ.",404);return await db.Set<BanCapNhatDichVuCoSo>().Where(x=>x.RevisionId==revisionId).Select(x=>x.BranchId).ToListAsync();}
  return await db.Set<DichVuCoSo>().Where(x=>x.ServiceId==serviceId).Select(x=>x.BranchId).ToListAsync();
 }
 public async Task ValidateServiceBranches(long providerId,List<long> ids,bool publishing)
 {
  Require(ids.Count<=20&&ids.All(x=>x>0)&&ids.Distinct().Count()==ids.Count,"Chọn tối đa 20 cơ sở khác nhau.",400);
  Require(!publishing||ids.Count>0,"Chọn ít nhất một cơ sở đã duyệt để đăng dịch vụ.",422);
  Require(await PublicBranches().CountAsync(b=>b.ProviderId==providerId&&ids.Contains(b.Id))==ids.Count,"Cơ sở phải thuộc bạn, đã được duyệt và đang công bố.",422);
 }
 public async Task ReplaceServiceBranches(long serviceId,IEnumerable<long> ids)
 {
  var old=await db.Set<DichVuCoSo>().Where(x=>x.ServiceId==serviceId).ToListAsync();
  var next=ids.ToHashSet();db.RemoveRange(old.Where(x=>!next.Contains(x.BranchId)));
  foreach(var id in next.Except(old.Select(x=>x.BranchId)))db.Add(new DichVuCoSo{ServiceId=serviceId,BranchId=id});
 }
 public async Task SnapshotServiceBranches(BanCapNhatDichVu revision,IEnumerable<long> ids)
 {
  await db.SaveChangesAsync();
  foreach(var id in ids)db.Add(new BanCapNhatDichVuCoSo{RevisionId=revision.MaBanCapNhat,BranchId=id});
 }

 public async Task<BranchOwnerPage> OwnerPage()
 {
  RoleRequired(ProviderRole);var p=await MyProvider();
  var rows=await db.Set<CoSoDichVu>().AsNoTracking().Where(x=>x.ProviderId==p.MaNhaCungCap).OrderByDescending(x=>x.UpdatedAt).ToListAsync();
  var ids=rows.Select(x=>x.Id).ToList();
  var pending=await db.Set<BanCapNhatCoSo>().AsNoTracking().Where(x=>ids.Contains(x.BranchId)&&x.Status=="Pending").ToDictionaryAsync(x=>x.BranchId);
  var counts=await db.Set<DichVuCoSo>().Where(x=>ids.Contains(x.BranchId)).GroupBy(x=>x.BranchId).Select(x=>new{Id=x.Key,Count=x.Count()}).ToDictionaryAsync(x=>x.Id,x=>x.Count);
  return new(rows,pending,counts,await ProviderActive(p.MaNhaCungCap));
 }
 public async Task<BranchEditPage> EditPage(long id=0,BranchInput? rejected=null)
 {
  RoleRequired(ProviderRole);var p=await MyProvider();
  if(id==0)return new(rejected??new(),null,[],await ProviderActive(p.MaNhaCungCap));
  var branch=await Own(id,p.MaNhaCungCap);
  var history=await db.Set<BanCapNhatCoSo>().AsNoTracking().Where(x=>x.BranchId==id).OrderByDescending(x=>x.Number).ToListAsync();
  if(rejected!=null)return new(rejected,branch,history,await ProviderActive(p.MaNhaCungCap));
  var input=new BranchInput{Id=id,Version=Token(branch.RowVersion),ConfirmLocation=true};
  object source=history.FirstOrDefault() is {Status:"Rejected" or "ChangesRequested"} last?last:branch;
  foreach(var key in Fields)typeof(BranchInput).GetProperty(key)!.SetValue(input,source.GetType().GetProperty(key)!.GetValue(source));
  input.OwnershipEvidence=history.FirstOrDefault()?.OwnershipEvidence??"";
  return new(input,branch,history,await ProviderActive(p.MaNhaCungCap));
 }
 async Task<CoSoDichVu> Own(long id,long providerId)=>await db.Set<CoSoDichVu>().SingleOrDefaultAsync(x=>x.Id==id&&x.ProviderId==providerId)??throw new BusinessException("Không tìm thấy cơ sở của bạn.",404);

 public async Task<long> Save(BranchInput i,CancellationToken ct=default)
 {
  RoleRequired(ProviderRole);var p=await MyProvider();Require(await ProviderActive(p.MaNhaCungCap),"Nhà cung cấp cần được xác minh và có quyền hoạt động để gửi cơ sở.",403);
  Require(i.Operation is "Draft" or "Submit","Thao tác cơ sở không hợp lệ.",400);
  Require(DanhMuc.LoaiDichVu.Contains(i.Type),"Loại cơ sở không hợp lệ.",400);
  var errors=new List<System.ComponentModel.DataAnnotations.ValidationResult>();
  Require(System.ComponentModel.DataAnnotations.Validator.TryValidateObject(i,new(i),errors,true),errors.FirstOrDefault()?.ErrorMessage??"Kiểm tra thông tin cơ sở.",422);
  i.Name=i.Name.Trim();i.Address=i.Address.Trim();i.Region=i.Region.Trim();i.Website=CleanUrl(i.Website);
  Require(i.Name.Length>0&&i.Address.Length>0&&i.Region.Length>0,"Điền tên cơ sở, địa chỉ và khu vực.",422);
  Require(i.VeganOnly!=true||i.VegetarianOptions!=false,"Quán thuần chay không thể khai không có món chay.",422);
  if(i.VeganOnly==true)i.VegetarianOptions=true;
  if(i.Operation=="Submit")Require(!string.IsNullOrWhiteSpace(i.OwnershipEvidence),"Cung cấp căn cứ bạn được quản lý cơ sở để quản trị viên kiểm tra.",422);
  if(i.Id>0)await Own(i.Id,p.MaNhaCungCap);
  var expires=await ResolveLocation(i,i.Operation=="Submit",ct);
  long savedId=0;
  await Execute(async()=>
  {
   Require(await ProviderActive(p.MaNhaCungCap),"Quyền hoạt động của nhà cung cấp đã thay đổi.",409);
   CoSoDichVu b;
   if(i.Id==0){b=new(){ProviderId=p.MaNhaCungCap,Status="Draft",CreatedAt=Clock.BayGioUtc};db.Add(b);}
   else{b=await Own(i.Id,p.MaNhaCungCap);CheckVersion(b,i.Version);Require(!await db.Set<BanCapNhatCoSo>().AnyAsync(x=>x.BranchId==b.Id&&x.Status=="Pending"),"Cơ sở đang có phiên bản chờ duyệt.",409);}
   Require(i.Operation!="Draft"||b.PublishedVersion==null,"Cơ sở đã công bố cần gửi bản cập nhật xét duyệt.",422);
   if(i.GooglePlaceId!=null)Require(!await db.Set<CoSoDichVu>().AnyAsync(x=>x.GooglePlaceId==i.GooglePlaceId&&x.Id!=b.Id&&(x.ProviderId==p.MaNhaCungCap||x.PublishedVersion!=null)),"Địa điểm Google này đã được liên kết với một cơ sở đã duyệt hoặc một hồ sơ của bạn. Vui lòng kiểm tra cơ sở hoặc liên hệ quản trị viên.",409);
   if(b.PublishedVersion==null){Copy(i,b);b.LocationExpiresAt=expires;b.Status=i.Operation=="Submit"?"Pending":"Draft";}
   b.UpdatedAt=Clock.BayGioUtc;await db.SaveChangesAsync();savedId=b.Id;
   if(i.Operation=="Submit")
   {
    var number=(await db.Set<BanCapNhatCoSo>().Where(x=>x.BranchId==b.Id).MaxAsync(x=>(int?)x.Number)??0)+1;
    var revision=new BanCapNhatCoSo{BranchId=b.Id,Number=number,Status="Pending",OwnershipEvidence=i.OwnershipEvidence.Trim(),SubmittedAt=Clock.BayGioUtc,LocationExpiresAt=expires};
    Copy(i,revision);db.Add(revision);
   }
   audit.Ghi(i.Operation=="Submit"?"GuiCoSo":"LuuNhapCoSo","CoSoDichVu",b.Id,null,b.Status,null);
  });
  return savedId;
 }

 public async Task<List<BranchReviewPage>> Queue(string status="Pending")
 {
  RoleRequired(AdminRole);Require(new[]{"Pending","Approved","Rejected","ChangesRequested","All"}.Contains(status),"Trạng thái không hợp lệ.",400);
  return await (from r in db.Set<BanCapNhatCoSo>().AsNoTracking() join b in db.Set<CoSoDichVu>() on r.BranchId equals b.Id join p in db.NhaCungCapDichVu on b.ProviderId equals p.MaNhaCungCap where status=="All"||r.Status==status orderby r.SubmittedAt descending select new BranchReviewPage(r,b,p)).Take(100).ToListAsync();
 }
 public async Task<BranchReviewPage> ReviewPage(long id)
 {
  RoleRequired(AdminRole);var r=await Find<BanCapNhatCoSo>(id);var b=await Find<CoSoDichVu>(r.BranchId);return new(r,b,await Find<NhaCungCapDichVu>(b.ProviderId));
 }
 public async Task Review(CommandInput i)
 {
  RoleRequired(AdminRole);Require(i.Decision is "Approved" or "Rejected" or "ChangesRequested","Quyết định không hợp lệ.",400);
  Require(i.Decision=="Approved"||!string.IsNullOrWhiteSpace(i.Reason),"Nhập lý do từ chối hoặc yêu cầu bổ sung.",422);
  await Execute(async()=>
  {
   var r=await Find<BanCapNhatCoSo>(i.Id);CheckVersion(r,i.Version);Require(r.Status=="Pending","Phiên bản đã được xử lý.",409);
   var b=await Find<CoSoDichVu>(r.BranchId);
   if(i.Decision=="Approved")
   {
    Require(await ProviderActive(b.ProviderId),"Nhà cung cấp chưa có quyền hoạt động.",409);
    if(r.GooglePlaceId!=null)Require(!await db.Set<CoSoDichVu>().AnyAsync(x=>x.Id!=b.Id&&x.GooglePlaceId==r.GooglePlaceId&&x.PublishedVersion!=null),"Địa điểm Google đang thuộc một cơ sở khác đã được duyệt.",409);
    foreach(var key in Fields)typeof(CoSoDichVu).GetProperty(key)!.SetValue(b,typeof(BanCapNhatCoSo).GetProperty(key)!.GetValue(r));
    b.LocationExpiresAt=r.LocationExpiresAt;b.PublishedVersion=r.Number;b.Status="Approved";
   }
   else if(b.PublishedVersion==null)b.Status=i.Decision;
   b.UpdatedAt=Clock.BayGioUtc;r.Status=i.Decision;r.Reason=string.IsNullOrWhiteSpace(i.Reason)?null:i.Reason.Trim();r.AdminId=UserId;r.ReviewedAt=Clock.BayGioUtc;
   audit.Ghi("DuyetCoSo","BanCapNhatCoSo",r.Id,"Pending",r.Status,r.Reason);
  });
 }
 public async Task Toggle(CommandInput i)
 {
  RoleRequired(ProviderRole);var p=await MyProvider();
  await Execute(async()=>
  {
   var b=await Own(i.Id,p.MaNhaCungCap);CheckVersion(b,i.Version);
   Require(b.PublishedVersion!=null&&b.Status is "Approved" or "Hidden","Chỉ thay đổi hiển thị cơ sở đã được duyệt.",422);
   Require(!await db.Set<BanCapNhatCoSo>().AnyAsync(x=>x.BranchId==b.Id&&x.Status=="Pending"),"Xử lý phiên bản chờ duyệt trước khi đổi hiển thị.",409);
   if(b.Status=="Hidden")Require(await ProviderActive(p.MaNhaCungCap),"Nhà cung cấp chưa có quyền hoạt động.",403);
   var before=b.Status;b.Status=b.Status=="Approved"?"Hidden":"Approved";b.UpdatedAt=Clock.BayGioUtc;
   audit.Ghi("HienThiCoSo","CoSoDichVu",b.Id,before,b.Status,null);
  });
 }
 public async Task<BranchPublicPage> Detail(long id,CancellationToken ct=default)
 {
  var b=await PublicBranches().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("Cơ sở không còn được công bố.",404);
  var ids=db.Set<DichVuCoSo>().Where(x=>x.BranchId==id).Select(x=>x.ServiceId);
  var services=await PublicServices().AsNoTracking().Where(x=>ids.Contains(x.MaDichVu)).OrderBy(x=>x.TenDichVu).ToListAsync();
  string? warning=null;MapPosition? pos=null;
  try{pos=await Position(b.Latitude,b.Longitude,b.LocationSource,b.LocationExpiresAt,b.GooglePlaceId,ct);}catch(BusinessException){warning="Vị trí liên kết Google cần được cập nhật. Bạn có thể mở địa điểm trên Google Maps.";}
  if(pos==null&&b.LocationSource=="Google"&&!maps.MapsReady)warning="Vị trí của cơ sở này có trong liên kết Google Maps.";
  return new(b,await db.NhaCungCapDichVu.AsNoTracking().SingleAsync(x=>x.MaNhaCungCap==b.ProviderId),services,pos,warning);
 }

 public async Task<MapPosition?> Position(decimal? lat,decimal? lng,string source,DateTime? expires,string? googleId,CancellationToken ct)
 {
  if(!maps.MapsReady&&source=="Google")return null;
  if(lat!=null&&lng!=null&&(source=="Provider"||expires>DateTime.UtcNow))return new(lat.Value,lng.Value);
  return maps.MapsReady&&googleId!=null?await maps.Position(googleId,ct):null;
 }
 async Task<DateTime?> ResolveLocation(MapLocationInput i,bool required,CancellationToken ct)
 {
  Require(i.LocationSource is "Google" or "Provider","Nguồn vị trí không hợp lệ.",400);
  Require(!required||i.ConfirmLocation,"Xác nhận đúng cơ sở và vị trí trước khi gửi.",422);
  if(i.LocationSource=="Google")
  {
   Require(GoogleMapsClient.ValidId(i.GooglePlaceId),"Chọn đúng địa điểm trong gợi ý Google.",422);
   if(!required&&!maps.PlacesReady){i.Latitude=null;i.Longitude=null;return null;}
   var position=await maps.Position(i.GooglePlaceId!,ct);i.Latitude=position.Lat;i.Longitude=position.Lng;
   return Clock.BayGioUtc.AddDays(29);
  }
  if(!string.IsNullOrWhiteSpace(i.GooglePlaceId))
  {
   Require(GoogleMapsClient.ValidId(i.GooglePlaceId),"Liên kết Google không hợp lệ.",422);
   await maps.Position(i.GooglePlaceId,ct);
  }
  else i.GooglePlaceId=null;
  Require((i.Latitude==null)==(i.Longitude==null),"Vị trí cần đủ vĩ độ và kinh độ.",422);
  if(i.Latitude!=null)GoogleMapsClient.CheckPosition(i.Latitude.Value,i.Longitude!.Value);
  Require(!required||i.Latitude!=null,"Đặt ghim vị trí cơ sở trên bản đồ.",422);return null;
 }
 static void Copy(BranchInput i,object target){foreach(var key in Fields)target.GetType().GetProperty(key)!.SetValue(target,typeof(BranchInput).GetProperty(key)!.GetValue(i));}
 public async Task<CoSoDichVu?> BookingBranch(long serviceId,long? branchId)
 {
  var links=await db.Set<DichVuCoSo>().Where(x=>x.ServiceId==serviceId).Select(x=>x.BranchId).ToListAsync();
  if(links.Count==0){Require(branchId==null,"Dịch vụ chưa được gắn cơ sở này.",422);return null;}
  if(branchId==null&&links.Count==1)branchId=links[0];
  Require(branchId!=null&&links.Contains(branchId.Value),"Chọn đúng cơ sở áp dụng dịch vụ trước khi gửi yêu cầu.",422);
  return await PublicBranches().SingleOrDefaultAsync(x=>x.Id==branchId)??throw new BusinessException("Cơ sở hiện không tiếp nhận yêu cầu mới qua website.",409);
 }
 static string? CleanUrl(string? value){if(string.IsNullOrWhiteSpace(value))return null;return GoogleMapsClient.SafeUrl(value.Trim())??throw new BusinessException("Đường dẫn phải dùng HTTPS và không chứa thông tin đăng nhập.",422);}

 public async Task<List<MocDiaDiem>> Landmarks(bool admin=false)
 {
  if(admin)RoleRequired(AdminRole);
  return await (admin?db.Set<MocDiaDiem>():PublicLandmarks()).AsNoTracking().OrderBy(x=>x.Name).ToListAsync();
 }
 public async Task<LandmarkInput> LandmarkForm(long id=0)
 {
  RoleRequired(AdminRole);if(id==0)return new();var l=await Find<MocDiaDiem>(id);
  var i=new LandmarkInput{Id=id,Version=Token(l.RowVersion),ConfirmLocation=true};
  foreach(var property in typeof(LandmarkInput).GetProperties())if(typeof(MocDiaDiem).GetProperty(property.Name) is {} p&&p.PropertyType==property.PropertyType)property.SetValue(i,p.GetValue(l));
  return i;
 }
 public async Task<long> SaveLandmark(LandmarkInput i,CancellationToken ct=default)
 {
  RoleRequired(AdminRole);Require(i.Type is "Point" or "Entrance","Loại mốc không hợp lệ.",400);
  Require(i.CheckedOn<=HomNay,"Ngày kiểm tra không được ở tương lai.",422);
  Require(!string.IsNullOrWhiteSpace(i.Name)&&!string.IsNullOrWhiteSpace(i.Address)&&!string.IsNullOrWhiteSpace(i.Region)&&!string.IsNullOrWhiteSpace(i.SourceName),"Điền tên, địa chỉ, khu vực và nguồn thông tin.",422);
  i.SourceUrl=CleanUrl(i.SourceUrl);var expires=await ResolveLocation(i,i.IsPublished,ct);long savedId=0;
  await Execute(async()=>
  {
   var seen=new HashSet<long>{i.Id};var cursor=i.ParentId;var depth=0;
   while(cursor!=null){Require(seen.Add(cursor.Value)&&depth++<20,"Quan hệ địa điểm chứa vòng lặp hoặc quá nhiều cấp.",422);var parent=await Find<MocDiaDiem>(cursor.Value);Require(i.Type=="Entrance"&&parent.Type=="Point"&&parent.ParentId==null,"Cửa vào chỉ liên kết trực tiếp với một địa điểm chính.",422);Require(!i.IsPublished||parent.IsPublished,"Địa điểm cha cần được công bố trước.",422);cursor=parent.ParentId;}
   if(i.Id>0&&i.Type!="Point")Require(!await db.Set<MocDiaDiem>().AnyAsync(x=>x.ParentId==i.Id),"Địa điểm đang có cửa vào liên kết cần giữ loại địa điểm chính.",422);
   MocDiaDiem l;
   if(i.Id==0){l=new();db.Add(l);}else{l=await Find<MocDiaDiem>(i.Id);CheckVersion(l,i.Version);}
   if(i.GooglePlaceId!=null)Require(!await db.Set<MocDiaDiem>().AnyAsync(x=>x.Id!=l.Id&&x.GooglePlaceId==i.GooglePlaceId),"Địa điểm Google này đã được liên kết với một mốc.",409);
   foreach(var property in typeof(LandmarkInput).GetProperties())if(property.Name is not ("Id" or "Version")&&typeof(MocDiaDiem).GetProperty(property.Name) is {} p&&p.PropertyType==property.PropertyType)p.SetValue(l,property.GetValue(i));
   l.Name=l.Name.Trim();l.Address=l.Address.Trim();l.Region=l.Region.Trim();l.SourceName=l.SourceName.Trim();l.AdminId=UserId;l.UpdatedAt=Clock.BayGioUtc;l.LocationExpiresAt=expires;
   await db.SaveChangesAsync();savedId=l.Id;audit.Ghi("CapNhatMocDiaDiem","MocDiaDiem",l.Id,null,l.IsPublished?"Published":"Hidden",null);
  });return savedId;
 }
}
