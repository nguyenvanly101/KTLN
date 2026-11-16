using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

public sealed class BranchService(AppDbContext context,IHttpContextAccessor accessor,IWebHostEnvironment environment,IConfiguration configuration,IServiceProvider provider,GoogleMapsClient maps,INhatKyService audit)
 : NghiepVuService(context,accessor,environment,configuration,provider)
{
 static readonly string[] Fields=["Name","Type","Region","DonViId","Address","Summary","Phone","Website","Hours","Parking","VegetarianOptions","VeganOnly","AccessibleEntrance","GooglePlaceId","Latitude","Longitude","LocationSource","PriceFrom","PriceTo","PriceUnit","OpenDays","OpenTime","CloseTime","PhotosJson","AttributesJson"];
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

 async Task<bool> CanSubmitBranch(NhaCungCapDichVu p)=>p.TrangThaiXacMinh is TrangThaiXacMinh.ChoXacMinh or TrangThaiXacMinh.CanBoSung or TrangThaiXacMinh.DaXacMinh&&(p.TrangThaiQuyenHoatDong==null||p.TrangThaiQuyenHoatDong==TrangThaiQuyenHoatDong.DangHoatDong)&&await db.TaiKhoan.AnyAsync(a=>a.MaTaiKhoan==p.MaTaiKhoan&&a.TrangThai==TrangThaiTaiKhoan.HoatDong);
 /// <summary>Xoa han dich vu chua tung gui/duoc dat; con lai chuyen sang "Ngung cong bo". Tra ve true neu da xoa han.</summary>
 public async Task<bool> DeleteService(long id)
 {
  RoleRequired(ProviderRole);var p=await MyProvider();var removed=false;
  await Execute(async()=>
  {
   var s=await db.DichVuDuLich.SingleOrDefaultAsync(x=>x.MaDichVu==id&&x.MaNhaCungCap==p.MaNhaCungCap)??throw new BusinessException("Không tìm thấy dịch vụ của bạn.",404);
   var used=await db.YeuCauDatDichVu.AnyAsync(x=>x.MaDichVu==id)||await db.BanCapNhatDichVu.AnyAsync(x=>x.MaDichVu==id)||s.PhienBanCongBo!=null;
   if(!used)
   {
    db.Set<DichVuCoSo>().RemoveRange(db.Set<DichVuCoSo>().Where(x=>x.ServiceId==id));
    db.DichVuDuLich.Remove(s);removed=true;
    audit.Ghi("XoaDichVu","DichVuDuLich",id,s.TrangThaiCongBo.ToString(),null,null);
   }
   else
   {
    Require(s.TrangThaiCongBo!=TrangThaiCongBo.NgungCongBo,"Dịch vụ đã được gỡ.",409);
    audit.Ghi("GoDichVu","DichVuDuLich",id,s.TrangThaiCongBo.ToString(),"NgungCongBo",null);
    s.TrangThaiCongBo=TrangThaiCongBo.NgungCongBo;s.ThoiDiemCapNhat=Clock.BayGioUtc;
   }
  });
  return removed;
 }
 // ------------------------------------------------------------------ co so dang chon (nha cung cap)
 public const string CurrentCookie="cs";
 public sealed record OwnedBranch(long Id,string Name,string Status);
 /// <summary>Moi co so cua nha cung cap (ke ca dang cho duyet) de chuyen doi qua lai.</summary>
 public async Task<List<OwnedBranch>> OwnedBranches()
 {
  RoleRequired(ProviderRole);
  var ctx=http.HttpContext;
  if(ctx!=null&&ctx.Items.TryGetValue("OwnedBranches",out var cached)&&cached is List<OwnedBranch> hit)return hit;
  var p=await MyProvider();
  var list=await db.Set<CoSoDichVu>().AsNoTracking().Where(b=>b.ProviderId==p.MaNhaCungCap).OrderBy(b=>b.Name).Select(b=>new OwnedBranch(b.Id,b.Name,b.Status)).ToListAsync();
  if(ctx!=null)ctx.Items["OwnedBranches"]=list;
  return list;
 }
 /// <summary>Co so dang tro toi: lay tu cookie neu thuoc nha cung cap, neu khong thi co so da duyet dau tien.</summary>
 public async Task<OwnedBranch?> CurrentBranch()
 {
  var ctx=http.HttpContext!;
  if(ctx.Items.TryGetValue("CurrentBranch",out var cached))return cached as OwnedBranch;
  var all=await OwnedBranches();OwnedBranch? pick=null;
  if(long.TryParse(ctx.Request.Cookies[CurrentCookie],out var id))pick=all.FirstOrDefault(b=>b.Id==id);
  pick??=all.FirstOrDefault(b=>b.Status=="Approved")??all.FirstOrDefault();
  ctx.Items["CurrentBranch"]=pick;return pick;
 }
 public async Task SetCurrentBranch(long id)
 {
  var all=await OwnedBranches();Require(all.Any(b=>b.Id==id),"Không tìm thấy cơ sở của bạn.",404);
  http.HttpContext!.Response.Cookies.Append(CurrentCookie,id.ToString(),new CookieOptions{HttpOnly=true,SameSite=SameSiteMode.Lax,Secure=http.HttpContext.Request.IsHttps,MaxAge=TimeSpan.FromDays(90),IsEssential=true});
  http.HttpContext.Items.Remove("CurrentBranch");http.HttpContext.Items.Remove("OwnedBranches");
 }
 /// <summary>Ma dich vu dang gan voi co so (de loc danh sach dich vu theo co so dang chon).</summary>
 public async Task<HashSet<long>> ServiceIdsOf(long branchId)=>(await db.Set<DichVuCoSo>().AsNoTracking().Where(x=>x.BranchId==branchId).Select(x=>x.ServiceId).ToListAsync()).ToHashSet();
 /// <summary>Gan moi dich vu cua nha cung cap chua thuoc co so nao vao co so dang chon. Tra ve so dich vu da gan.</summary>
 public async Task<int> AttachUnlinkedServices()
 {
  RoleRequired(ProviderRole);var p=await MyProvider();var current=await CurrentBranch()??throw new BusinessException("Chưa có cơ sở để gắn dịch vụ.",409);
  var count=0;
  await Execute(async()=>
  {
   var ids=await db.DichVuDuLich.Where(d=>d.MaNhaCungCap==p.MaNhaCungCap&&!db.Set<DichVuCoSo>().Any(l=>l.ServiceId==d.MaDichVu)).Select(d=>d.MaDichVu).ToListAsync();
   foreach(var sid in ids)db.Add(new DichVuCoSo{ServiceId=sid,BranchId=current.Id});
   count=ids.Count;
   if(count>0)audit.Ghi("GanDichVuVaoCoSo","CoSoDichVu",current.Id,null,count.ToString(),null);
  });
  return count;
 }
 public async Task<int> UnlinkedServiceCount()
 {
  RoleRequired(ProviderRole);var p=await MyProvider();
  return await db.DichVuDuLich.CountAsync(d=>d.MaNhaCungCap==p.MaNhaCungCap&&!db.Set<DichVuCoSo>().Any(l=>l.ServiceId==d.MaDichVu));
 }
 public async Task<HashSet<long>> LinkedServiceIds(IEnumerable<long> ids){var list=ids.ToList();return (await db.Set<DichVuCoSo>().AsNoTracking().Where(x=>list.Contains(x.ServiceId)).Select(x=>x.ServiceId).Distinct().ToListAsync()).ToHashSet();}
 public async Task<bool> HasAnyBranch(long accountId)=>await db.Set<CoSoDichVu>().AnyAsync(b=>db.NhaCungCapDichVu.Any(p=>p.MaNhaCungCap==b.ProviderId&&p.MaTaiKhoan==accountId));
 public async Task<BranchOwnerPage> OwnerPage()
 {
  RoleRequired(ProviderRole);var p=await MyProvider();
  var rows=await db.Set<CoSoDichVu>().AsNoTracking().Where(x=>x.ProviderId==p.MaNhaCungCap).OrderByDescending(x=>x.UpdatedAt).ToListAsync();
  var ids=rows.Select(x=>x.Id).ToList();
  var pending=await db.Set<BanCapNhatCoSo>().AsNoTracking().Where(x=>ids.Contains(x.BranchId)&&x.Status=="Pending").ToDictionaryAsync(x=>x.BranchId);
  var counts=await db.Set<DichVuCoSo>().Where(x=>ids.Contains(x.BranchId)).GroupBy(x=>x.BranchId).Select(x=>new{Id=x.Key,Count=x.Count()}).ToDictionaryAsync(x=>x.Id,x=>x.Count);
  var done=rows.ToDictionary(x=>x.Id,x=>Completeness(pending.TryGetValue(x.Id,out var pr)?pr:x));
  var links=await (from l in db.Set<DichVuCoSo>() join d in db.Set<DichVuDuLich>() on l.ServiceId equals d.MaDichVu where ids.Contains(l.BranchId) orderby d.TenDichVu select new{l.BranchId,d.MaDichVu,d.TenDichVu,d.GiaThamKhao,d.TrangThaiCongBo}).AsNoTracking().ToListAsync();
 var services=links.GroupBy(x=>x.BranchId).ToDictionary(g=>g.Key,g=>g.Select(x=>(x.MaDichVu,x.TenDichVu,x.GiaThamKhao,x.TrangThaiCongBo.ToString())).ToList());
 return new(rows,pending,counts,await CanSubmitBranch(p),done,p.TrangThaiXacMinh==TrangThaiXacMinh.DaXacMinh,services);
 }
 /// <summary>Bieu mau Them co so dien san tu mot dia diem da cong bo trong Kham pha (nha cung cap "nhan" dia diem). Chi lay ten, dia chi, khu vuc, toa do; nha cung cap van phai xac nhan vi tri va gui xet duyet nhu binh thuong.</summary>
 public async Task<BranchInput?> ClaimDraft(long entryId)
 {
  RoleRequired(ProviderRole);
  var e=await db.Set<DiscoveryEntry>().AsNoTracking().FirstOrDefaultAsync(x=>x.Id==entryId&&x.Status=="Published"&&x.Kind=="Place"&&x.ServiceId==null&&x.BranchId==null);
  if(e==null)return null;
  var tags=e.Tags??"";
  var type=tags.Contains("Lưu trú",StringComparison.OrdinalIgnoreCase)||tags.Contains("lưu trú")?"LuuTru":tags.Contains("Ăn uống",StringComparison.OrdinalIgnoreCase)||tags.Contains("ăn uống")?"AnUong":"HoatDong";
  static string Cut(string? v,int n)=>string.IsNullOrWhiteSpace(v)?"":v.Trim().Length<=n?v.Trim():v.Trim()[..n];
  return new BranchInput{Name=Cut(e.Title,200),Address=Cut(e.Address,400),Region=Cut(e.Region,200),DonViId=e.DonViId,Latitude=e.Latitude,Longitude=e.Longitude,Type=type,
   OwnershipEvidence=Cut("Nhận từ địa điểm đã có trong Khám phá (mục #"+e.Id+": "+e.Title+").",2000)};
 }
 public async Task<BranchEditPage> EditPage(long id=0,BranchInput? rejected=null)
 {
  RoleRequired(ProviderRole);var p=await MyProvider();
  if(id==0){var blank=rejected??new();return new(blank,null,[],await CanSubmitBranch(p),Completeness(blank));}
  var branch=await Own(id,p.MaNhaCungCap);
  var history=await db.Set<BanCapNhatCoSo>().AsNoTracking().Where(x=>x.BranchId==id).OrderByDescending(x=>x.Number).ToListAsync();
  if(rejected!=null)return new(rejected,branch,history,await CanSubmitBranch(p));
  var input=new BranchInput{Id=id,Version=Token(branch.RowVersion),ConfirmLocation=true};
  object source=history.FirstOrDefault() is {Status:"Rejected" or "ChangesRequested"} last?last:branch;
  foreach(var key in Fields)typeof(BranchInput).GetProperty(key)!.SetValue(input,source.GetType().GetProperty(key)!.GetValue(source));
  input.OwnershipEvidence=history.FirstOrDefault()?.OwnershipEvidence??"";
  return new(input,branch,history,await CanSubmitBranch(p),Completeness(input));
 }
 async Task<CoSoDichVu> Own(long id,long providerId)=>await db.Set<CoSoDichVu>().SingleOrDefaultAsync(x=>x.Id==id&&x.ProviderId==providerId)??throw new BusinessException("Không tìm thấy cơ sở của bạn.",404);

 public async Task<long> Save(BranchInput i,CancellationToken ct=default)
 {
  RoleRequired(ProviderRole);var p=await MyProvider();Require(await CanSubmitBranch(p),"Tài khoản nhà cung cấp đang bị hạn chế nên chưa gửi được cơ sở.",403);
  if(i.Documents.Count>0&&p.TrangThaiXacMinh!=TrangThaiXacMinh.DaXacMinh)
  {
   Require(i.Documents.Count<=5,"Đính kèm tối đa 5 tài liệu.",400);
   var stored=await StoreFiles(i.Documents);
   var profile=await db.HoSoXacMinhNhaCungCap.Where(h=>h.MaNhaCungCap==p.MaNhaCungCap).OrderByDescending(h=>h.MaHoSo).FirstOrDefaultAsync();
   if(profile!=null)
   {
    foreach(var f in stored)db.Add(new TaiLieuXacMinh{MaHoSo=profile.MaHoSo,TenTepGoc=f.Name,DuongDan=f.Path,LoaiTep=System.Enum.Parse<LoaiTep>(f.Type),KichThuoc=f.Size,LanNop=0});
    await db.SaveChangesAsync();
   }
  }
  Require(i.Operation is "Draft" or "Submit","Thao tác cơ sở không hợp lệ.",400);
  Require(ServiceKinds.BranchCodes.Contains(i.Type),"Loại cơ sở không hợp lệ.",400);
  var errors=new List<System.ComponentModel.DataAnnotations.ValidationResult>();
  Require(System.ComponentModel.DataAnnotations.Validator.TryValidateObject(i,new(i),errors,true),errors.FirstOrDefault()?.ErrorMessage??"Kiểm tra thông tin cơ sở.",422);
  i.Region=await ResolveRegion(i.DonViId,i.Region,i.Operation=="Submit");
  i.Name=i.Name.Trim();i.Address=i.Address.Trim();i.Website=CleanUrl(i.Website);
  Require(i.Name.Length>0&&i.Address.Length>0&&i.Region.Length>0,"Điền tên cơ sở, địa chỉ và khu vực.",422);
  Require(i.VeganOnly!=true||i.VegetarianOptions!=false,"Quán thuần chay không thể khai không có món chay.",422);
  if(i.VeganOnly==true)i.VegetarianOptions=true;
  NormalizeInfo(i);
  i.OwnershipEvidence=string.Join(": ",new[]{i.OwnershipRole?.Trim(),i.OwnershipEvidence?.Trim()}.Where(x=>!string.IsNullOrEmpty(x)));
  if(i.Operation=="Submit")Require(!string.IsNullOrWhiteSpace(i.OwnershipEvidence),"Cho biết vai trò của bạn tại cơ sở để quản trị viên đối chiếu quyền quản lý.",422);
  if(i.Id>0)await Own(i.Id,p.MaNhaCungCap);
  var expires=await ResolveLocation(i,i.Operation=="Submit",ct);
  long savedId=0;
  await Execute(async()=>
  {
   Require(await CanSubmitBranch(p),"Quyền hoạt động của nhà cung cấp đã thay đổi.",409);
   CoSoDichVu b;
   if(i.Id==0){b=new(){ProviderId=p.MaNhaCungCap,Status="Draft",CreatedAt=Clock.BayGioUtc};db.Add(b);}
   else{b=await Own(i.Id,p.MaNhaCungCap);CheckVersion(b,i.Version);Require(!await db.Set<BanCapNhatCoSo>().AnyAsync(x=>x.BranchId==b.Id&&x.Status=="Pending"),"Cơ sở đang có phiên bản chờ duyệt.",409);}
   Require(i.Operation!="Draft"||b.PublishedVersion==null,"Cơ sở đã công bố cần gửi bản cập nhật xét duyệt.",422);
   if(i.GooglePlaceId!=null)Require(!await db.Set<CoSoDichVu>().AnyAsync(x=>x.GooglePlaceId==i.GooglePlaceId&&x.Id!=b.Id&&(x.ProviderId==p.MaNhaCungCap||x.PublishedVersion!=null)),"Địa điểm Google này đã được liên kết với một cơ sở đã duyệt hoặc một hồ sơ của bạn. Vui lòng kiểm tra cơ sở hoặc liên hệ quản trị viên.",409);
   await PreparePhotos(i,b);
   if(b.PublishedVersion!=null&&i.Operation=="Submit"&&b.Status=="Approved"&&!HeavyChanged(i,b))
   {
    // Thong tin "nhe" (gio, gia, lien he, tien ich): co hieu luc ngay, khong cho duyet.
    foreach(var key in LightFields)b.GetType().GetProperty(key)!.SetValue(b,typeof(BranchInput).GetProperty(key)!.GetValue(i));
    b.UpdatedAt=Clock.BayGioUtc;await db.SaveChangesAsync();savedId=b.Id;i.AppliedNow=true;
    audit.Ghi("CapNhatNhanhCoSo","CoSoDichVu",b.Id,null,b.Status,null);
    return;
   }
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
  RoleRequired(AdminRole);var r=await Find<BanCapNhatCoSo>(id);var b=await Find<CoSoDichVu>(r.BranchId);var owner=await Find<NhaCungCapDichVu>(b.ProviderId);
  var docs=await (from d in db.Set<TaiLieuXacMinh>().AsNoTracking() join h in db.HoSoXacMinhNhaCungCap.AsNoTracking() on d.MaHoSo equals h.MaHoSo where h.MaNhaCungCap==owner.MaNhaCungCap orderby d.MaTaiLieu descending select d).Take(20).ToListAsync();
  return new(r,b,owner,docs);
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
    var owner=await Find<NhaCungCapDichVu>(b.ProviderId);
    // Duyet co so dau tien cung la xac minh nha cung cap: mot lan duyet.
    if(owner.TrangThaiXacMinh is TrangThaiXacMinh.ChoXacMinh or TrangThaiXacMinh.CanBoSung&&owner.TrangThaiQuyenHoatDong is null or TrangThaiQuyenHoatDong.DangHoatDong)
    {
     owner.TrangThaiXacMinh=TrangThaiXacMinh.DaXacMinh;owner.TrangThaiQuyenHoatDong=TrangThaiQuyenHoatDong.DangHoatDong;
     foreach(var profile in await db.HoSoXacMinhNhaCungCap.Where(x=>x.MaNhaCungCap==owner.MaNhaCungCap&&x.TrangThaiXuLy!=TrangThaiHoSoXacMinh.DaXacMinh).ToListAsync())profile.TrangThaiXuLy=TrangThaiHoSoXacMinh.DaXacMinh;
     audit.Ghi("XacMinhQuaCoSo","NhaCungCapDichVu",owner.MaNhaCungCap,"ChoXacMinh","DaXacMinh","Duyệt cơ sở đầu tiên");
     await db.SaveChangesAsync();
    }
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
 /// <summary>Khu vuc chon tu cay dia gioi (den cap xa/phuong); khi gui duyet/cong bo bat buoc phai chon. Chuoi Region duoc sinh tu don vi da chon.</summary>
 async Task<string> ResolveRegion(long? unitId,string? legacy,bool required)
 {
  if(unitId is long id){var geo=Resolve<DiaGioiService>();var unit=await geo.RequireLeaf(id);return await geo.PathText(unit.Id);}
  Require(!required,"Chọn khu vực đến cấp xã/phường.",422);
  return (legacy??"").Trim();
 }
 /// <summary>Chuan hoa va kiem tra gia tham khao, gio mo cua co cau truc.</summary>
 void NormalizeInfo(BranchInput i)
 {
  i.PriceUnit=string.IsNullOrWhiteSpace(i.PriceUnit)?null:i.PriceUnit.Trim();
  Require(i.PriceFrom!=null||i.PriceTo==null,"Nhập giá \"từ\" trước khi nhập giá \"đến\".",422);
  if(i.PriceFrom!=null){Require(i.PriceUnit!=null,"Chọn đơn vị của giá (người, phòng/đêm, suất…).",422);Require(i.PriceTo==null||i.PriceTo>=i.PriceFrom,"Giá đến phải lớn hơn hoặc bằng giá từ.",422);}
  else i.PriceUnit=null;
  var days=new string((string.Concat(i.OpenDayList??[])+(i.OpenDays??"")).Where(c=>c is >='1' and <='7').Distinct().OrderBy(c=>c).ToArray());
  var any=days.Length>0||i.OpenTime!=null||i.CloseTime!=null;
  if(any){Require(days.Length>0&&i.OpenTime!=null&&i.CloseTime!=null,"Giờ mở cửa cần đủ ngày mở, giờ mở và giờ đóng (hoặc để trống cả ba).",422);i.OpenDays=days;}
  else{i.OpenDays=null;i.OpenTime=null;i.CloseTime=null;}
 }
 /// <summary>Anh co so: tai len thay bo anh hien co; khong tai thi giu bo cu (tru khi chon xoa). Toi da 5 anh JPG/PNG.</summary>
 async Task PreparePhotos(BranchInput i,CoSoDichVu b)
 {
  string? existing=b.PhotosJson;
  if(b.Id>0){var last=await db.Set<BanCapNhatCoSo>().AsNoTracking().Where(x=>x.BranchId==b.Id).OrderByDescending(x=>x.Number).FirstOrDefaultAsync();if(last!=null&&last.Status is "Rejected" or "ChangesRequested")existing=last.PhotosJson;}
  Require(i.Photos.Count<=5,"Chọn tối đa 5 ảnh.",400);
  if(i.Photos.Count>0)i.PhotosJson=System.Text.Json.JsonSerializer.Serialize((await StoreFiles(i.Photos)).Select(x=>x.Path));
  else i.PhotosJson=i.RemovePhotos?null:existing;
 }
 /// <summary>Do day du cua ho so co so theo tieu chi khach can: khu vuc, vi tri, gioi thieu, gio mo cua, gia, anh, lien he.</summary>
 public static BranchCompleteness Completeness(object src)
 {
  T? G<T>(string name)=>src.GetType().GetProperty(name)?.GetValue(src) is T v?v:default;
  var missing=new List<string>();var score=0;
  void Add(bool ok,int points,string need){if(ok)score+=points;else missing.Add(need);}
  Add(!string.IsNullOrWhiteSpace(G<string>("Name")),10,"Tên cơ sở");
  Add(G<long?>("DonViId")!=null,15,"Khu vực đến cấp xã/phường");
  Add(!string.IsNullOrWhiteSpace(G<string>("Address")),10,"Địa chỉ");
  Add(G<decimal?>("Latitude")!=null,10,"Ghim vị trí trên bản đồ");
  Add((G<string>("Summary")?.Trim().Length??0)>=40,10,"Giới thiệu ít nhất 40 ký tự");
  Add(!string.IsNullOrEmpty(G<string>("OpenDays")),10,"Giờ mở cửa");
  Add(G<decimal?>("PriceFrom")!=null,10,"Giá tham khảo");
  var photos=Ui.ServicePhotos(G<string>("PhotosJson")).Length;
  Add(photos>=1,15,"Ít nhất một ảnh thực tế");if(photos>=3)score+=5;else if(photos>=1)missing.Add("Thêm ảnh (nên có từ 3 ảnh)");else score+=0;
  Add(!string.IsNullOrWhiteSpace(G<string>("Phone"))||!string.IsNullOrWhiteSpace(G<string>("Website")),5,"Số điện thoại hoặc website");
  return new(Math.Min(100,score),missing);
 }
 /// <summary>Duong dan anh co so (da cong bo, hoac ban nhap/ban cho duyet cho chu co so va quan tri).</summary>
 public async Task<string> PhotoFile(long id,int index,long? revision)
 {
  var branch=await db.Set<CoSoDichVu>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("Không tìm thấy ảnh.",404);
  string? json;
  if(revision==null&&await PublicBranches().AnyAsync(x=>x.Id==id))json=branch.PhotosJson;
  else
  {
   RoleRequired(ProviderRole,AdminRole);
   if(Role==ProviderRole){var owner=await MyProvider();Require(branch.ProviderId==owner.MaNhaCungCap,"Không tìm thấy ảnh.",404);}
   json=revision==null?branch.PhotosJson:(await db.Set<BanCapNhatCoSo>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==revision&&x.BranchId==id)??throw new BusinessException("Không tìm thấy ảnh.",404)).PhotosJson;
  }
  var images=Ui.ServicePhotos(json);Require(index>=0&&index<images.Length,"Không tìm thấy ảnh.",404);
  var path=Resolve<ILuuTepService>().DuongDanAnToan(images[index]);Require(System.IO.File.Exists(path)&&Path.GetExtension(path).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png","Không tìm thấy ảnh.",404);
  return path;
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
 /// <summary>Truong thay doi duoc co hieu luc ngay (khong qua duyet): gio mo cua, gia, lien he, tien ich, thong tin rieng theo loai.</summary>
 static readonly string[] LightFields=["Hours","OpenDays","OpenTime","CloseTime","PriceFrom","PriceTo","PriceUnit","Phone","Parking","VegetarianOptions","VeganOnly","AccessibleEntrance","AttributesJson"];
 /// <summary>Truong anh huong uy tin / dinh danh: ten, loai, khu vuc, dia chi, gioi thieu, vi tri, anh, website - van phai duoc quan tri duyet.</summary>
 static readonly string[] HeavyFields=["Name","Type","Region","DonViId","Address","Summary","Website","GooglePlaceId","Latitude","Longitude","LocationSource","PhotosJson"];
 static bool HeavyChanged(BranchInput i,CoSoDichVu b)
 {
  foreach(var key in HeavyFields)
  {
   var a=typeof(BranchInput).GetProperty(key)!.GetValue(i);var c=typeof(CoSoDichVu).GetProperty(key)!.GetValue(b);
   if(a is string sa)a=string.IsNullOrWhiteSpace(sa)?null:sa.Trim();if(c is string sc)c=string.IsNullOrWhiteSpace(sc)?null:sc.Trim();
   if(!Equals(a,c))return true;
  }
  return false;
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
  i.Region=await ResolveRegion(i.DonViId,i.Region,i.IsPublished);
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
