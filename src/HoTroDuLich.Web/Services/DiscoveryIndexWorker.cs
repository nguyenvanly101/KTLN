using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;
namespace HoTroDuLich.Web.Services;

public sealed class DiscoveryIndexWorker(IServiceScopeFactory scopes,ILogger<DiscoveryIndexWorker> logger):BackgroundService
{
 public static async Task Initialize(IServiceProvider services)
 {
  var db=services.GetRequiredService<AppDbContext>();var clock=services.GetRequiredService<IDongHo>();
  await services.GetRequiredService<IGiaoDich>().ThucHienAsync(async()=>{
   var index=await db.Set<DiscoveryEntry>().Where(x=>x.PlaceId!=null||x.ServiceId!=null).ToListAsync();
   var places=await db.DiaDiemDuLich.AsNoTracking().Where(x=>x.DangHienThi).ToListAsync();
   var serviceQuery=services.GetRequiredService<TravelService>().PublicServices();var publicServices=await serviceQuery.AsNoTracking().ToListAsync();
   async Task Sync(DiscoveryEntry? e,long id,string kind,string title,string region,string? description,string tags,string raw){
    var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));if(e==null){e=new(){Slug=kind.ToLowerInvariant()+"-"+id,Kind=kind,PlaceId=kind=="Place"?id:null,ServiceId=kind=="Service"?id:null,Title=title,Region=region,Summary=description??"",Body="",Tags=tags,SourceName="Thông tin đã công bố trong hệ thống",Status="Published",SourceStamp=hash,UpdatedAt=clock.BayGioUtc};db.Add(e);return;}
    if(e.SourceStamp==hash)return;var before=JsonSerializer.Serialize(e);e.Title=title;e.Region=region;e.Summary=description??"";e.SourceStamp=hash;e.UpdatedAt=clock.BayGioUtc;if(kind=="Service")e.Tags=tags;
    db.Add(new DiscoveryRevision{EntryId=e.Id,BeforeJson=before,AfterJson=JsonSerializer.Serialize(e),Summary="Thông tin gốc của địa điểm/dịch vụ vừa thay đổi. Kiểm tra giá và điều kiện mới trước khi lựa chọn.",Public=e.Status=="Published",CreatedAt=clock.BayGioUtc});
    if(e.Status=="Published")foreach(var owner in await db.Set<DiscoveryFollow>().Where(x=>x.EntryId==e.Id&&x.Active).Select(x=>x.OwnerId).ToListAsync())services.GetRequiredService<IThongBaoService>().Tao(owner,"ThongTinCapNhat",title+": thông tin công bố vừa thay đổi.",DiscoveryText.Link(e.Id));
   }
   foreach(var p in places)await Sync(index.SingleOrDefault(x=>x.PlaceId==p.MaDiaDiem),p.MaDiaDiem,"Place",p.TenDiaDiem,p.KhuVuc,p.MoTa,p.LoaiDiaDiem,JsonSerializer.Serialize(p));
   foreach(var s in publicServices)await Sync(index.SingleOrDefault(x=>x.ServiceId==s.MaDichVu),s.MaDichVu,"Service",s.TenDichVu,s.KhuVucPhucVu,s.MoTa,DanhMuc.Ten(s.LoaiDichVu)+" "+s.LoaiDichVu,JsonSerializer.Serialize(new{s.TenDichVu,s.KhuVucPhucVu,s.MoTa,s.GiaThamKhao,s.DonViGia,s.DieuKienPhucVu,s.ThoiGianPhucVu,s.TamNgungTu,s.TamNgungDen,s.TrangThaiTiepNhan,s.TrangThaiCongBo,s.DiaChiPhucVu,s.PhienBanCongBo}));
   await db.SaveChangesAsync();
  });
  await DiscoveryStarterContent.Initialize(db,clock);
 }
 protected override async Task ExecuteAsync(CancellationToken stoppingToken)
 {
  using var timer=new PeriodicTimer(TimeSpan.FromMinutes(1));while(await timer.WaitForNextTickAsync(stoppingToken)){try{await using var scope=scopes.CreateAsyncScope();await Initialize(scope.ServiceProvider);}catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){return;}catch(Exception e){logger.LogWarning("Chưa cập nhật chỉ mục tra cứu: {Type}",e.GetType().Name);}}
 }
}

public static class DiscoveryStarterContent
{
 public static async Task Initialize(AppDbContext db,IDongHo clock)
 {
  var checkedOn=new DateOnly(2026,11,8);
  var entries=new List<DiscoveryEntry>{
   new(){Slug="destination-da-lat",Kind="Destination",Title="Đà Lạt",Region="Đà Lạt",Summary="Khám phá địa điểm, dịch vụ và thông tin cần biết cho chuyến đi tại khu vực Đà Lạt.",Body="Bắt đầu từ những nơi bạn muốn ghé, sau đó xem khoảng cách và cách di chuyển giữa chúng. Kiểm tra giá, đơn vị tính và thời gian phục vụ ở từng địa điểm.\n\nTọa độ trên bản đồ là mốc tham chiếu khu vực, không phải lối vào một điểm tham quan. Chọn địa điểm hoặc ghim cụ thể khi tính tuyến.",Latitude=11.946460m,Longitude=108.441930m,LocationNote="Mốc tham chiếu khu vực theo UNT Digital Library; không phải lối vào.",SourceName="UNT Digital Library — tọa độ địa danh Đà Lạt",SourceUrl="https://digital.library.unt.edu/explore/locations/p19502/",Tags="thiên nhiên cà phê tham quan lưu trú khám phá"},
   new(){Slug="guide-vietnam-transport",Kind="Transport",Title="Tìm hiểu cách di chuyển khi du lịch tự túc",Region="Việt Nam",Summary="Tra cứu các phương án đường hàng không, tàu và đường bộ; kiểm tra lịch, điểm đón trả và điều kiện hành lý trước khi chọn.",Body="So sánh điểm xuất phát và điểm đến thực tế, đoạn đi tiếp sau khi xuống phương tiện, thời gian chờ và đổi chặng. Kiểm tra giá và lịch tại đơn vị vận hành trước khi mua vé.\n\nChức năng tính tuyến trên bản đồ cung cấp tham khảo cho ô tô, đi bộ và xe đạp. Lịch tàu, xe buýt và giá vé cần đối chiếu nguồn của đơn vị khai thác; không suy ra từ tuyến đường bộ.",SourceName="Vietnam Tourism — Transport within Vietnam",SourceUrl="https://vietnam.travel/plan-your-trip/transport-within-vietnam/",Tags="di chuyển đi lại tàu xe buýt sân bay bến xe phương tiện hành lý",TransportMode="Mixed",Origin="Chọn nơi xuất phát",Destination="Chọn điểm đến"},
   new(){Slug="guide-weather",Kind="Guide",Title="Tìm hiểu thời tiết theo mùa trước khi đi",Region="Việt Nam",Summary="Tham khảo đặc điểm khí hậu theo vùng, rồi kiểm tra dự báo gần ngày đi và thông báo của điểm đến.",Body="Thông tin khí hậu theo mùa giúp chuẩn bị phương án trải nghiệm. Dự báo thời tiết là thông tin theo thời điểm và cần kiểm tra lại gần ngày đi.\n\nKhi muốn tìm trải nghiệm trong nhà, chọn bộ lọc Trong nhà. Những nơi chưa có thông tin xác nhận sẽ không được tính là đáp ứng điều kiện này.",SourceName="Vietnam Tourism — Weather and climate in Vietnam",SourceUrl="https://vietnam.travel/things-to-do/weather-and-climate-vietnam/",Tags="thời tiết trời mưa trong nhà mùa khí hậu dự báo"},
   new(){Slug="guide-events",Kind="Guide",Title="Tra cứu lễ hội và sự kiện theo thời gian",Region="Việt Nam",Summary="Tìm lịch công bố của đơn vị tổ chức và kiểm tra lại ngày, địa điểm, vé hoặc yêu cầu đăng ký.",Body="Mở nhóm Sự kiện, chọn ngày dự định tham gia để xem các sự kiện đã công bố trên hệ thống. Khi chưa có kết quả, bạn có thể đối chiếu lịch của Vietnam Tourism tại nguồn bên dưới.\n\nMột mục lịch tổng hợp không xác nhận còn vé hay việc sự kiện chắc chắn diễn ra. Kiểm tra thông báo mới nhất của đơn vị tổ chức trước khi đến.",SourceName="Vietnam Tourism — Festivals & events",SourceUrl="https://vietnam.travel/event/",Tags="lễ hội sự kiện hôm nay cuối tuần lịch hoạt động"},
   new(){Slug="guide-accessibility",Kind="Guide",Title="Kiểm tra khả năng tiếp cận của địa điểm",Region="Việt Nam",Summary="Tìm thông tin cụ thể về lối vào, nhà vệ sinh, chỗ ngồi và bãi đỗ xe trước khi chọn địa điểm.",Body="Chọn từng điều kiện bạn cần trong bộ lọc tiện ích. Kết quả chỉ đáp ứng khi thuộc tính được xác nhận có; dữ liệu còn thiếu được ghi rõ.\n\nVới yêu cầu cụ thể như chiều rộng lối vào hoặc việc sử dụng xe lăn, hãy hỏi đơn vị vận hành tại mục Hỏi đáp trước khi quyết định. Khả năng tiếp cận cần được kiểm tra cho cả lối vào và không gian sử dụng.",SourceName="Google Maps Help — Accessibility information",SourceUrl="https://support.google.com/maps/answer/6396990",Tags="tiếp cận xe lăn người lớn tuổi trẻ em bậc thang nhà vệ sinh lối vào"}
  };
  entries.Add(new(){Slug="destination-hoi-an",Kind="Destination",Title="Hội An",Region="Hội An",Summary="Tìm hiểu phố cổ, các khu vực xung quanh và thông tin thực tế trước khi ghé thăm.",Body="Phố cổ Hội An là di sản được UNESCO ghi danh. Tìm địa điểm và dịch vụ theo khu vực, sau đó kiểm tra vị trí, điều kiện tiếp cận và thông tin phục vụ.\n\nMốc bản đồ lấy theo hồ sơ di sản; khi tính đường đi hãy chọn đúng lối tiếp cận. Giá vé và giờ tham quan cần được xác nhận từ đơn vị quản lý.",Latitude=15.883333m,Longitude=108.333333m,LocationNote="Mốc tham chiếu khu di sản theo UNESCO; không xác nhận lối vào.",SourceName="UNESCO World Heritage Centre — Hoi An Ancient Town",SourceUrl="https://whc.unesco.org/en/list/948/",Tags="phố cổ văn hóa di sản tham quan đi bộ"});
  entries.Add(new(){Slug="area-hoi-an-old-town",Kind="Area",Title="Khu phố cổ Hội An",Region="Hội An",Summary="Khu vực để tìm hiểu các điểm tham quan và dịch vụ gần phố cổ.",Body="Dùng bản đồ để xem tương quan giữa nơi lưu trú, địa điểm muốn ghé và điểm đón trả. Kiểm tra điều kiện tiếp cận từng điểm khi đi cùng trẻ nhỏ hoặc người cần hỗ trợ di chuyển.\n\nThông tin về tiếng ồn, mức đông đúc, giá lưu trú và quy định phương tiện cần được kiểm tra theo thời điểm; hồ sơ này chưa xác nhận các điều kiện đó.",Latitude=15.883333m,Longitude=108.333333m,LocationNote="Mốc tham chiếu khu di sản, không phải ranh giới hay cổng vào.",SourceName="UNESCO World Heritage Centre — vị trí di sản",SourceUrl="https://whc.unesco.org/en/list/948/",Tags="khu vực ở đâu lưu trú phố cổ đi bộ văn hóa"});
  entries.Add(new(){Slug="reference-anantara-hoi-an",Kind="Service",Title="Anantara Hội An — thông tin tham khảo",Region="Hội An",Summary="Thông tin lưu trú từ trang của đơn vị vận hành. Tra cứu vị trí và liên hệ nguồn để kiểm tra điều kiện theo ngày.",Body="Dịch vụ này được giới thiệu bằng nguồn bên ngoài và chưa có nhà cung cấp nhận yêu cầu đặt trên hệ thống. Xem trang của đơn vị vận hành để kiểm tra giá, phòng, điều kiện và các thông tin mới nhất.",Address="1 Phạm Hồng Thái, Hội An",Latitude=15.880058m,Longitude=108.338047m,LocationNote="Tọa độ theo thông tin của đơn vị vận hành; kiểm tra lối vào khi di chuyển.",SourceName="Anantara Hội An — trang của đơn vị vận hành",SourceUrl="https://www.anantara.com/vi/hoi-an",Tags="lưu trú khách sạn nghỉ dưỡng anantara"});
  foreach(var e in entries){if(await db.Set<DiscoveryEntry>().AnyAsync(x=>x.Slug==e.Slug))continue;e.Status="Published";e.CheckedOn=checkedOn;e.UpdatedAt=clock.BayGioUtc;db.Add(e);await db.SaveChangesAsync();}
  var hoi=await db.Set<DiscoveryEntry>().SingleAsync(x=>x.Slug=="destination-hoi-an");
  foreach(var e in await db.Set<DiscoveryEntry>().Where(x=>(x.Slug=="area-hoi-an-old-town"||x.Slug=="reference-anantara-hoi-an")&&x.ParentId==null&&x.UpdatedBy==null).ToListAsync())e.ParentId=hoi.Id;
  var legacy=await db.Set<DiscoveryEntry>().SingleOrDefaultAsync(x=>x.PlaceId!=null&&x.Title=="Phố cổ Hội An"&&x.SourceName=="Thông tin đã công bố trong hệ thống"&&x.CheckedOn==null);
  if(legacy!=null){legacy.Latitude=15.883333m;legacy.Longitude=108.333333m;legacy.LocationNote="Mốc tham chiếu khu di sản theo UNESCO; không phải lối vào cụ thể.";legacy.SourceName="UNESCO World Heritage Centre — vị trí di sản";legacy.SourceUrl="https://whc.unesco.org/en/list/948/";legacy.CheckedOn=checkedOn;legacy.ParentId=hoi.Id;}
  await db.SaveChangesAsync();
 }
}
