using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace HoTroDuLich.Web.Services;

public sealed partial class DiscoveryService
{
 void Validate(object input){var errors=new List<ValidationResult>();if(!Validator.TryValidateObject(input,new(input),errors,true))throw new BusinessException(errors[0].ErrorMessage??"Kiểm tra dữ liệu đã nhập.",400,field:errors[0].MemberNames.FirstOrDefault());}
 public Task<List<DiscoveryEntry>> ParentChoices()=>db.Set<DiscoveryEntry>().AsNoTracking().Where(e=>(e.Kind=="Destination"||e.Kind=="Area")&&(Admin||e.Status=="Published")).OrderBy(e=>e.Title).Take(300).ToListAsync();
 async Task<DiscoveryEntry> Editable(long id){Signed();var e=await db.Set<DiscoveryEntry>().SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("Không tìm thấy thông tin.",404);if(!Admin)Need(e.ServiceId!=null&&await db.DichVuDuLich.AnyAsync(s=>s.MaDichVu==e.ServiceId&&db.NhaCungCapDichVu.Any(p=>p.MaNhaCungCap==s.MaNhaCungCap&&p.MaTaiKhoan==Me)),"Chỉ đề nghị bổ sung thông tin cho dịch vụ của bạn.",403);return e;}
 public async Task<DiscoveryEditInput> EditForm(long id=0)
 {
  if(id==0){travel.RoleRequired(TravelService.AdminRole);return new(){FeatureRows=DiscoveryText.Features.Select(x=>new DiscoveryFeatureInput{Code=x.Key}).ToList(),Prices=[new()]};}
  var e=await Editable(id);var result=new DiscoveryEditInput{Id=e.Id,EntryId=e.Id,Version=TravelService.Token(e.RowVersion),LinkedKind=e.ServiceId!=null?"Service":e.PlaceId!=null?"Place":null};
  var display=JsonSerializer.Deserialize<DiscoveryEntry>(JsonSerializer.Serialize(e))!;await Hydrate([display]);
  foreach(var p in typeof(DiscoveryEditInput).GetProperties()){var source=typeof(DiscoveryEntry).GetProperty(p.Name=="Content"?"Body":p.Name);if(source!=null&&source.PropertyType==p.PropertyType&&p.Name!="Id")p.SetValue(result,source.GetValue(display));}
  result.Body="";result.Content=e.Body;var features=await db.Set<DiscoveryFeature>().AsNoTracking().Where(x=>x.EntryId==id).ToDictionaryAsync(x=>x.Code);
  result.FeatureRows=DiscoveryText.Features.Select(x=>new DiscoveryFeatureInput{Code=x.Key,State=features.GetValueOrDefault(x.Key)?.State??"Unknown",Note=features.GetValueOrDefault(x.Key)?.Note}).ToList();
  result.Prices=(await db.Set<DiscoveryPrice>().AsNoTracking().Where(x=>x.EntryId==id&&x.Active).ToListAsync()).Select(x=>new DiscoveryPriceInput{Label=x.Label,Amount=x.Amount,Unit=x.Unit,Conditions=x.Conditions,Included=x.Included,Extra=x.Extra,From=x.From,Until=x.Until}).ToList();result.Prices.Add(new());return result;
 }
 async Task ValidateEdit(DiscoveryEditInput i,DiscoveryEntry? current)
 {
  Validate(i);Need(DiscoveryText.Kinds.ContainsKey(i.Kind)&&new[]{"Draft","Published","Hidden"}.Contains(i.Status),"Loại hoặc trạng thái thông tin không hợp lệ.",400);
  Need((i.Latitude==null)==(i.Longitude==null),"Nhập đủ vĩ độ và kinh độ.");Need(i.CheckedOn==null||i.CheckedOn<=clock.HomNay,"Ngày kiểm tra không được ở tương lai.");Need(i.ValidFrom==null||i.ValidTo==null||i.ValidTo>=i.ValidFrom,"Ngày kết thúc hiệu lực không hợp lệ.");Need(i.PriceFrom==null||i.PriceTo==null||i.PriceTo>=i.PriceFrom,"Khoảng giá không hợp lệ.");Need(DiscoveryText.HttpUrl(i.SourceUrl),"Nguồn tham khảo cần là địa chỉ HTTPS hợp lệ.");
  Need(i.ParentId!=i.Id||i.Id==0,"Thông tin không thể tự làm mục cha.");
  if(i.ParentId!=null){var seen=new HashSet<long>();var next=i.ParentId;while(next!=null){Need(seen.Add(next.Value)&&next!=i.Id,"Quan hệ điểm đến/khu vực tạo thành vòng lặp.");var parent=await db.Set<DiscoveryEntry>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==next);Need(parent!=null,"Không tìm thấy mục cha.");next=parent!.ParentId;}}
  Need(i.FeatureRows.Count<=12&&i.FeatureRows.Select(x=>x.Code).Distinct().Count()==i.FeatureRows.Count&&i.FeatureRows.All(x=>DiscoveryText.Features.ContainsKey(x.Code)&&new[]{"Yes","No","Unknown"}.Contains(x.State)&&(x.Note?.Length??0)<=500),"Thuộc tính tiện ích không hợp lệ.");
  Need(i.Prices.Count<=12,"Tối đa 12 dòng giá.");foreach(var p in i.Prices.Where(x=>!string.IsNullOrWhiteSpace(x.Label))){Validate(p);Need(!string.IsNullOrWhiteSpace(p.Unit)&&(p.Amount==null||p.Amount>=0),"Nhập đơn vị và số tiền hợp lệ (hoặc để trống nếu chưa biết).");Need(p.From==null||p.Until==null||p.Until>=p.From,"Ngày áp dụng dòng giá không hợp lệ.");}
  var noHours=string.IsNullOrWhiteSpace(i.OpenDays)&&i.OpenTime==null&&i.CloseTime==null;Need(noHours||(!string.IsNullOrWhiteSpace(i.OpenDays)&&i.OpenDays.All(c=>c is >= '1' and <= '7')&&i.OpenTime!=null&&i.CloseTime!=null),"Giờ mở cửa cần đủ ngày (1–7), giờ mở và giờ đóng. Để cả ba trống khi chưa xác nhận.");
  if(i.Status=="Published"){Need(!string.IsNullOrWhiteSpace(i.Summary)&&i.CheckedOn!=null&&!string.IsNullOrWhiteSpace(i.SourceName),"Công bố cần phần tóm tắt, nguồn và ngày kiểm tra.");if(i.Kind=="Event")Need(i.ValidFrom!=null&&i.ValidTo!=null,"Sự kiện công bố cần đủ ngày bắt đầu và kết thúc. Với sự kiện một ngày, chọn cùng ngày.");}
  if(current?.PlaceId!=null)Need(i.Kind=="Place","Không đổi loại thông tin liên kết địa điểm.");if(current?.ServiceId!=null)Need(i.Kind=="Service","Không đổi loại thông tin liên kết dịch vụ.");
 }
 async Task ApplyEdit(DiscoveryEntry e,DiscoveryEditInput i,string summary)
 {
  var before=JsonSerializer.Serialize(new{Entry=e,Features=await db.Set<DiscoveryFeature>().AsNoTracking().Where(x=>x.EntryId==e.Id).ToListAsync(),Prices=await db.Set<DiscoveryPrice>().AsNoTracking().Where(x=>x.EntryId==e.Id&&x.Active).ToListAsync()});
  var originalFields=e.ServiceId!=null?new[]{"Title","Region","Summary","PriceFrom","PriceTo","PriceUnit","Includes","Hours"}:e.PlaceId!=null?new[]{"Title","Region","Summary"}:Array.Empty<string>();
  foreach(var name in new[]{"Title","Kind","Region","Summary","ParentId","Address","Latitude","Longitude","LocationNote","SourceName","SourceUrl","CheckedOn","ValidFrom","ValidTo","PriceFrom","PriceTo","PriceUnit","Includes","Excludes","Hours","OpenDays","OpenTime","CloseTime","DurationMinutes","Tags","TransportMode","Origin","Destination","Contact","Status"})if(!originalFields.Contains(name))typeof(DiscoveryEntry).GetProperty(name)!.SetValue(e,typeof(DiscoveryEditInput).GetProperty(name)!.GetValue(i));
  e.Body=i.Content;e.UpdatedAt=clock.BayGioUtc;e.UpdatedBy=Me;e.SourceUrl=Clean(e.SourceUrl);e.OpenDays=Clean(e.OpenDays);e.Title=e.Title.Trim();e.Region=e.Region.Trim();
  await db.SaveChangesAsync();
  var oldFeatures=await db.Set<DiscoveryFeature>().Where(x=>x.EntryId==e.Id).ToListAsync();foreach(var row in oldFeatures){row.State="Unknown";row.Note=null;}
  foreach(var f in i.FeatureRows){var row=oldFeatures.SingleOrDefault(x=>x.Code==f.Code);if(row==null){row=new(){EntryId=e.Id,Code=f.Code};db.Add(row);}row.State=f.State;row.Note=Clean(f.Note);}
  foreach(var p in await db.Set<DiscoveryPrice>().Where(x=>x.EntryId==e.Id&&x.Active).ToListAsync())p.Active=false;
  foreach(var p in i.Prices.Where(x=>!string.IsNullOrWhiteSpace(x.Label)))db.Add(new DiscoveryPrice{EntryId=e.Id,Label=p.Label.Trim(),Amount=p.Amount,Unit=p.Unit.Trim(),Conditions=p.Conditions,Included=p.Included,Extra=p.Extra,From=p.From,Until=p.Until,Active=true});
  var after=JsonSerializer.Serialize(new{Entry=e,Features=i.FeatureRows,Prices=i.Prices});
  db.Add(new DiscoveryRevision{EntryId=e.Id,ActorId=Me,BeforeJson=before,AfterJson=after,Summary=summary,Public=e.Status=="Published",CreatedAt=clock.BayGioUtc});
  audit.Ghi("CapNhatTraCuu","DiscoveryEntry",e.Id,null,e.Status,summary);
  var readers=await db.Set<DiscoveryFollow>().Where(f=>f.EntryId==e.Id&&f.Active&&f.OwnerId!=Me&&db.TaiKhoan.Any(a=>a.MaTaiKhoan==f.OwnerId&&a.TrangThai==TrangThaiTaiKhoan.HoatDong)).Select(f=>f.OwnerId).ToListAsync();
  foreach(var owner in readers)notices.Tao(owner,"ThongTinCapNhat",e.Status=="Published"?($"{e.Title}: {summary}")[..Math.Min(480,($"{e.Title}: {summary}").Length)]:"Một mục bạn theo dõi đã được ẩn. Bạn có thể xem danh sách theo dõi để kiểm tra.",e.Status=="Published"?DiscoveryText.Link(e.Id):"/Discovery/Library");
 }
 public async Task<long> Save(DiscoveryEditInput i)
 {
  travel.RoleRequired(TravelService.AdminRole);long id=0;await travel.Execute(async()=>{DiscoveryEntry e;if(i.Id>0){e=await Editable(i.Id);Version(e,i.Version);}else{e=new(){Slug="entry-"+Guid.NewGuid().ToString("N"),CreatedBy=Me,Status="Draft",Kind=i.Kind,Title=i.Title,Region=i.Region,SourceName=i.SourceName,UpdatedAt=clock.BayGioUtc};db.Add(e);await db.SaveChangesAsync();}await ValidateEdit(i,e);await ApplyEdit(e,i,Clean(i.Reason)??"Cập nhật thông tin tra cứu");id=e.Id;});return id;
 }
 public async Task Propose(DiscoveryEditInput i)
 {
  travel.RoleRequired(TravelService.ProviderRole);Need(i.Id>0&&i.SubmissionId!=Guid.Empty,"Đề nghị bổ sung không hợp lệ.");await travel.Execute(async()=>{if(await db.Set<DiscoveryProposal>().AnyAsync(x=>x.OwnerId==Me&&x.SubmissionId==i.SubmissionId))return;var e=await Editable(i.Id);Version(e,i.Version);i.Status=e.Status;await ValidateEdit(i,e);Need(!await db.Set<DiscoveryProposal>().AnyAsync(x=>x.EntryId==e.Id&&x.Status=="Pending"),"Mục này đang có đề nghị chờ kiểm tra.",409);db.Add(new DiscoveryProposal{EntryId=e.Id,OwnerId=Me,BaseVersion=i.Version,Payload=JsonSerializer.Serialize(i),Status="Pending",SubmissionId=i.SubmissionId,CreatedAt=clock.BayGioUtc,UpdatedAt=clock.BayGioUtc});audit.Ghi("GuiBoSungTraCuu","DiscoveryEntry",e.Id,null,"Pending");});
 }
 public async Task ReviewProposal(DiscoveryCommand i)
 {
  travel.RoleRequired(TravelService.AdminRole);Need(new[]{"Accepted","Rejected"}.Contains(i.Decision)&&!string.IsNullOrWhiteSpace(i.Reason),"Chọn kết quả và ghi lý do.");await travel.Execute(async()=>{var p=await db.Set<DiscoveryProposal>().SingleOrDefaultAsync(x=>x.Id==i.Id)??throw new BusinessException("Không tìm thấy đề nghị.",404);Version(p,i.Version);Need(p.Status=="Pending","Đề nghị đã được xử lý.",409);if(i.Decision=="Accepted"){var e=await Editable(p.EntryId);Version(e,p.BaseVersion);var edit=JsonSerializer.Deserialize<DiscoveryEditInput>(p.Payload)!;await ValidateEdit(edit,e);await ApplyEdit(e,edit,i.Reason);}p.Status=i.Decision;p.Reason=i.Reason;p.ReviewedBy=Me;p.UpdatedAt=clock.BayGioUtc;notices.Tao(p.OwnerId,"BoSungTraCuu","Đề nghị bổ sung thông tin đã được xem xét: "+DiscoveryText.State(p.Status),"/Discovery/Manage?tab=proposals");});
 }
 public async Task<DiscoveryManagePage> Manage(string tab="entries",DiscoveryCommand? input=null)
 {
  travel.RoleRequired(TravelService.AdminRole,TravelService.ProviderRole);Need(new[]{"entries","questions","experiences","reports","proposals"}.Contains(tab),"Trang quản lý không hợp lệ.",400);
  var entries=db.Set<DiscoveryEntry>().AsNoTracking();if(!Admin)entries=entries.Where(e=>e.ServiceId!=null&&db.DichVuDuLich.Any(s=>s.MaDichVu==e.ServiceId&&db.NhaCungCapDichVu.Any(p=>p.MaNhaCungCap==s.MaNhaCungCap&&p.MaTaiKhoan==Me)));
  var questions=await db.Set<DiscoveryQuestion>().AsNoTracking().Where(x=>entries.Any(e=>e.Id==x.EntryId)).OrderByDescending(x=>x.Status=="Pending").ThenByDescending(x=>x.Id).Take(100).ToListAsync();
  var reports=Admin?await db.Set<DiscoveryReport>().AsNoTracking().OrderByDescending(x=>x.Status=="Pending").ThenByDescending(x=>x.Id).Take(100).ToListAsync():[];
  var experiences=Admin?await db.Set<DiscoveryExperience>().AsNoTracking().OrderByDescending(x=>x.Status=="Pending").ThenByDescending(x=>x.Id).Take(100).ToListAsync():[];
  var proposals=await db.Set<DiscoveryProposal>().AsNoTracking().Where(x=>Admin||x.OwnerId==Me).OrderByDescending(x=>x.Status=="Pending").ThenByDescending(x=>x.Id).Take(100).ToListAsync();
  var refs=questions.Select(x=>x.EntryId).Concat(reports.Select(x=>x.EntryId)).Concat(experiences.Select(x=>x.EntryId)).Concat(proposals.Select(x=>x.EntryId)).Distinct().ToList();
  return new(await entries.OrderByDescending(x=>x.UpdatedAt).Take(200).ToListAsync(),questions,experiences,reports,proposals,await Names(questions.Select(x=>x.OwnerId).Concat(experiences.Select(x=>x.OwnerId)).Distinct().ToList()),await db.Set<DiscoveryEntry>().AsNoTracking().Where(x=>refs.Contains(x.Id)).ToDictionaryAsync(x=>x.Id),tab,input??new());
 }
}
