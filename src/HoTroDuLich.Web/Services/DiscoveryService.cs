using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace HoTroDuLich.Web.Services;

public static class DiscoveryText
{
 public static readonly Dictionary<string,string> Kinds=new(){["Destination"]="Điểm đến",["Area"]="Khu vực",["Place"]="Tham quan",["Service"]="Dịch vụ",["Guide"]="Cẩm nang",["Transport"]="Di chuyển",["Event"]="Sự kiện",["Amenity"]="Tiện ích"};
 public static readonly Dictionary<string,string> Features=new(){["AccessibleEntrance"]="Lối vào cho xe lăn",["AccessibleRestroom"]="Nhà vệ sinh tiếp cận",["Parking"]="Có chỗ gửi xe",["Seating"]="Có chỗ ngồi nghỉ",["Children"]="Tiện ích cho trẻ em",["Indoor"]="Trong nhà",["PublicTransport"]="Tiếp cận phương tiện công cộng",["BookingRequired"]="Cần đặt trước",["Luggage"]="Gửi hành lý",["Charging"]="Điểm sạc",["FreeEntry"]="Vào cửa miễn phí",["Vegetarian"]="Có lựa chọn ăn chay"};
 public static readonly Dictionary<string,string> Topics=new(){["Access"]="Đường đi",["Tickets"]="Vé vào cửa",["Queue"]="Xếp hàng",["Parking"]="Gửi xe",["Price"]="Chi phí",["Cleanliness"]="Vệ sinh",["Family"]="Đi cùng gia đình"};
 public static readonly Dictionary<string,string> States=new(){["Draft"]="Bản nháp",["Published"]="Đã công bố",["Hidden"]="Đã ẩn",["Pending"]="Chờ kiểm tra",["Answered"]="Đã trả lời",["Checking"]="Đang xác minh",["Resolved"]="Đã xử lý",["Rejected"]="Không chấp thuận",["Accepted"]="Đã chấp thuận",["Yes"]="Đã xác nhận có",["No"]="Đã xác nhận không có",["Unknown"]="Chưa có thông tin"};
 public static string Kind(string s)=>Kinds.GetValueOrDefault(s,s);
 public static string FieldLabel(string s)=>new Dictionary<string,string>{{"Title","Tiêu đề"},{"Kind","Loại thông tin"},{"Region","Khu vực"},{"Summary","Tóm tắt"},{"Content","Nội dung"},{"Address","Địa chỉ"},{"Latitude","Vĩ độ"},{"Longitude","Kinh độ"},{"LocationNote","Giải thích vị trí"},{"SourceName","Nguồn"},{"SourceUrl","Đường dẫn nguồn"},{"CheckedOn","Ngày kiểm tra"},{"ValidFrom","Áp dụng từ"},{"ValidTo","Áp dụng đến"},{"PriceFrom","Giá từ"},{"PriceTo","Giá đến"},{"PriceUnit","Đơn vị giá"},{"Includes","Bao gồm"},{"Excludes","Trả thêm"},{"Hours","Giờ phục vụ"},{"OpenDays","Ngày mở cửa (1–7)"},{"OpenTime","Giờ mở"},{"CloseTime","Giờ đóng"},{"DurationMinutes","Thời lượng phút"},{"Tags","Chủ đề"},{"TransportMode","Phương tiện"},{"Origin","Xuất phát"},{"Destination","Điểm đến"},{"Contact","Liên hệ"},{"Reason","Lý do"},{"ParentId","Điểm đến/khu vực cha"},{"Status","Trạng thái"}}.GetValueOrDefault(s,s);
 public static string SearchUrl(DiscoveryFilter f,string? kind=null,int? page=null){var values=new Dictionary<string,string?>();foreach(var p in typeof(DiscoveryFilter).GetProperties()){var value=p.GetValue(f);if(value!=null&&value.ToString()!="")values[p.Name]=value is DateOnly d?d.ToString("yyyy-MM-dd"):value is TimeOnly t?t.ToString("HH:mm"):Convert.ToString(value,CultureInfo.InvariantCulture);}if(kind!=null){values["Kind"]=kind=="All"?"":kind;values.Remove("Type");values["Page"]="1";}if(page!=null)values["Page"]=page.ToString();return Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString("/Discovery",values);}
 public static string Without(DiscoveryFilter f,string field,string? feature=null){var source=Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(SearchUrl(f).Split('?').ElementAtOrDefault(1)??"");var values=source.ToDictionary(x=>x.Key,x=>(string?)x.Value.ToString(),StringComparer.OrdinalIgnoreCase);values.Remove(field);values["Page"]="1";if(feature!=null)values["Features"]=string.Join(',',(f.Features??"").Split(',').Where(x=>x!=feature));if(field=="Date")values.Remove("At");if(field=="Lat"){values.Remove("Lng");}if(field=="South")foreach(var k in new[]{"West","North","East"})values.Remove(k);return Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString("/Discovery",values);}
 public static string State(string s)=>States.GetValueOrDefault(s,s);
 public static (List<string> Words,string? Intent) Question(string? input)
 {
  var text=Fold(input);string? intent=null;var skip=new HashSet<string>();
  if(Regex.IsMatch(text,@"\bgia\b(?! (dinh|lai)\b)")||text.Contains("bao nhieu tien")){intent="Price";skip.UnionWith(new[]{"gia","ve","phi","tien","het","ton"});}
  else if(Regex.IsMatch(text,@"\bgio\b")||text.Contains("mo cua")||text.Contains("dong cua")){intent="Hours";skip.UnionWith(new[]{"gio","may","mo","dong","cua","hoat","dong"});}
  else if(text.Contains("dia chi")||text.Contains("o dau")){intent="Address";skip.UnionWith(new[]{"dia","chi","nam","dau"});}
  else if(text.Contains("xe lan")){intent="AccessibleEntrance";skip.UnionWith(new[]{"xe","lan","loi","vao","tiep","can"});}
  else if(text.Contains("gui xe")||text.Contains("do xe")){intent="Parking";skip.UnionWith(new[]{"gui","do","xe","bai","cho"});}
  else if(text.Contains("gui hanh ly")){intent="Luggage";skip.UnionWith(new[]{"gui","hanh","ly","cho"});}
  return(Words(input).Where(x=>!skip.Contains(x)).ToList(),intent);
 }
 public static string Fold(string? s)=>string.Concat((s??"").Normalize(NormalizationForm.FormD).Where(c=>CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark)).Replace('đ','d').Replace('Đ','D').ToLowerInvariant().Normalize(NormalizationForm.FormC);
 public static List<string> Words(string? s){var text=Fold(s);foreach(var pair in new[]{("khach san","luu tru"),("quan an","an uong"),("nha hang","an uong"),("tham quan","tham quan"),("xe buyt","buyt")})text=text.Replace(pair.Item1,pair.Item2);var stop="toi minh ban co nao nhung cac mot nhieu o tai la thi de duoc khong the nao can muon tim kiem thong tin ve gi voi va hay hoac khi cho cua tu den lam sao bao nhieu nhu the nao".Split(' ').ToHashSet();return Regex.Matches(text,@"[\p{L}\p{N}]+").Select(m=>m.Value).Where(w=>w.Length>1&&!stop.Contains(w)).Distinct().Take(10).ToList();}
 public static double Distance(double a,double b,double c,double d){var r=Math.PI/180;var h=Math.Pow(Math.Sin((c-a)*r/2),2)+Math.Cos(a*r)*Math.Cos(c*r)*Math.Pow(Math.Sin((d-b)*r/2),2);return 6371*2*Math.Atan2(Math.Sqrt(h),Math.Sqrt(Math.Max(0,1-h)));}
 public static bool HttpUrl(string? s)=>string.IsNullOrWhiteSpace(s)||(Uri.TryCreate(s,UriKind.Absolute,out var u)&&u.Scheme=="https"&&!string.IsNullOrWhiteSpace(u.Host)&&u.UserInfo.Length==0);
 public static string Link(long id)=>"/Discovery/Entry/"+id;
 public static bool ActiveOn(DiscoveryEntry e,DateOnly day)=> (e.ValidFrom==null||e.ValidFrom<=day)&&(e.ValidTo==null||e.ValidTo>=day);
 public static bool OpenAt(DiscoveryEntry e,DateOnly day,TimeOnly time){if(e.OpenDays==null||e.OpenTime==null||e.CloseTime==null)return false;var weekday=((int)day.DayOfWeek+6)%7+1;return e.OpenTime<=e.CloseTime?e.OpenDays.Contains(weekday.ToString())&&time>=e.OpenTime&&time<e.CloseTime:(e.OpenDays.Contains(weekday.ToString())&&time>=e.OpenTime)||(e.OpenDays.Contains((weekday==1?7:weekday-1).ToString())&&time<e.CloseTime);}
}

public sealed partial class DiscoveryService(AppDbContext db,TravelService travel,IDongHo clock,INhatKyService audit,IThongBaoService notices)
{
 const string SearchCollation="Latin1_General_100_CI_AI";
 long Me=>travel.UserId;
 bool Admin=>travel.Role==TravelService.AdminRole;
 IQueryable<DiscoveryEntry> Public(){var services=travel.PublicServices();return db.Set<DiscoveryEntry>().Where(e=>e.Status=="Published"&&(e.PlaceId==null||db.DiaDiemDuLich.Any(p=>p.MaDiaDiem==e.PlaceId&&p.DangHienThi))&&(e.ServiceId==null||services.Any(s=>s.MaDichVu==e.ServiceId)));}
 public async Task<DiscoveryEntry> PublicEntry(long id)=>await Public().AsNoTracking().SingleOrDefaultAsync(e=>e.Id==id)??throw new BusinessException("Thông tin này chưa được công bố hoặc đã được ẩn.",404);
 public async Task<long?> LegacyId(long? place=null,long? service=null)=>await Public().Where(x=>(place!=null&&x.PlaceId==place)||(service!=null&&x.ServiceId==service)).Select(x=>(long?)x.Id).FirstOrDefaultAsync();
 void Signed()=>travel.RoleRequired(TravelService.Traveler,TravelService.ProviderRole,TravelService.AdminRole);
 static string? Clean(string? s)=>string.IsNullOrWhiteSpace(s)?null:s.Trim();
 void Need(bool v,string message,int status=422)=>travel.Require(v,message,status);
 void Version(object obj,string v)=>travel.CheckVersion(obj,v);
 async Task Hydrate(List<DiscoveryEntry> entries)
 {
  var pids=entries.Where(x=>x.PlaceId!=null).Select(x=>x.PlaceId!.Value).ToList();var sids=entries.Where(x=>x.ServiceId!=null).Select(x=>x.ServiceId!.Value).ToList();
  var places=await db.DiaDiemDuLich.AsNoTracking().Where(x=>pids.Contains(x.MaDiaDiem)).ToDictionaryAsync(x=>x.MaDiaDiem);
  var services=await travel.PublicServices().AsNoTracking().Where(x=>sids.Contains(x.MaDichVu)).ToDictionaryAsync(x=>x.MaDichVu);
  foreach(var e in entries){if(e.PlaceId is long pid&&places.TryGetValue(pid,out var p)){e.Title=p.TenDiaDiem;e.Region=p.KhuVuc;e.Address=p.DiaChi??e.Address;e.Summary=p.MoTa??e.Summary;}if(e.ServiceId is long sid&&services.TryGetValue(sid,out var s)){e.Title=s.TenDichVu;e.Region=s.KhuVucPhucVu;e.Address=s.DiaChiPhucVu??e.Address;e.Summary=s.MoTa??e.Summary;e.PriceFrom=s.GiaThamKhao;e.PriceTo=null;e.PriceUnit=s.DonViGia;e.Hours=s.ThoiGianPhucVu??e.Hours;e.Includes=s.DieuKienPhucVu??e.Includes;e.Tags+=" "+DanhMuc.Ten(s.LoaiDichVu);}}
 }
 public async Task<DiscoverySearchPage> Search(DiscoveryFilter f,bool map=false)
 {
  Need(f.Page is >=1 and <=100000&&(f.Q?.Length??0)<=200&&(f.Region?.Length??0)<=100&&(f.Min==null||f.Min>=0)&&(f.Max==null||f.Max>=0),"Điều kiện tìm kiếm không hợp lệ.",400);
  Need(f.Min==null||f.Max==null||f.Min<=f.Max,"Giá đến phải lớn hơn hoặc bằng giá từ.",400);
  var phrase=DiscoveryText.Fold(f.Q);var knownRegions=await Public().Select(e=>e.Region).Distinct().ToListAsync();var mentioned=knownRegions.Where(r=>DiscoveryText.Fold(r)!="viet nam"&&Regex.IsMatch(phrase,@"\b"+Regex.Escape(DiscoveryText.Fold(r))+@"\b")).ToList();
  if(string.IsNullOrWhiteSpace(f.Region)&&mentioned.Count==1)f.Region=mentioned[0];
  var categoryWords=new HashSet<string>();
  if(string.IsNullOrWhiteSpace(f.Kind)&&string.IsNullOrWhiteSpace(f.Type)){
   if(phrase.Contains("khach san")||phrase.Contains("luu tru")){f.Type="LuuTru";categoryWords.UnionWith(new[]{"luu","tru"});}
   else if(phrase.Contains("quan an")||phrase.Contains("an uong")||phrase.Contains("nha hang")){f.Type="AnUong";categoryWords.UnionWith(new[]{"an","uong"});}
   else if(phrase.Contains("di lai")||phrase.Contains("di chuyen")){f.Kind="Transport";categoryWords.UnionWith(new[]{"di","lai","chuyen"});}
   else if(phrase.Contains("tham quan")){f.Kind="Place";categoryWords.UnionWith(new[]{"tham","quan"});}
  }
  if(phrase.Contains("trong nha")&&!phrase.Contains("khong")){f.Features=string.Join(',',((f.Features??"")+",Indoor").Split(',',StringSplitOptions.RemoveEmptyEntries).Distinct());categoryWords.UnionWith(new[]{"trong","nha"});}
  categoryWords.UnionWith(new[]{"noi","dia","diem","gan","nhat"});if(mentioned.Count==1)categoryWords.UnionWith(DiscoveryText.Words(mentioned[0]));
  if(!string.IsNullOrEmpty(f.Type)){var type=DanhMuc.ChuanHoa(f.Type);if(type=="DiaDiem")f.Kind="Place";else if(DanhMuc.LoaiDichVu.Contains(type))f.Kind="Service";else Need(false,"Loại dịch vụ không hợp lệ.",400);}
  Need(string.IsNullOrEmpty(f.Kind)||DiscoveryText.Kinds.ContainsKey(f.Kind),"Loại thông tin không hợp lệ.",400);Need(new[]{"relevance","updated","distance","price"}.Contains(f.Sort),"Cách sắp xếp không hợp lệ.",400);
  var wanted=(f.Features??"").Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Distinct().ToArray();Need(wanted.Length<=12&&wanted.All(DiscoveryText.Features.ContainsKey),"Tiện ích tìm kiếm không hợp lệ.",400);
  Need((f.Lat==null)==(f.Lng==null)&&(f.Lat==null||(f.Lat>=-90&&f.Lat<=90&&f.Lng>=-180&&f.Lng<=180))&&f.RadiusKm is >=.1 and <=200,"Vị trí hoặc phạm vi tìm kiếm không hợp lệ.",400);
  var bounds=new[]{f.South,f.West,f.North,f.East};Need(bounds.All(x=>x==null)||(bounds.All(x=>x!=null)&&f.South>=-90&&f.North<=90&&f.West>=-180&&f.East<=180&&f.South<f.North&&f.West<f.East),"Khung bản đồ không hợp lệ.",400);
  var query=Public().AsNoTracking();var question=DiscoveryText.Question(f.Q);var words=question.Words.Where(x=>!categoryWords.Contains(x)).ToList();var all=Public().AsNoTracking();
  if(!string.IsNullOrWhiteSpace(f.Region)){var region=f.Region.Trim();query=query.Where(e=>EF.Functions.Collate(e.Region,SearchCollation)==region);}
  if(!string.IsNullOrWhiteSpace(f.Kind))query=query.Where(e=>e.Kind==f.Kind);
  if(!string.IsNullOrWhiteSpace(f.Type)&&f.Kind=="Service"){var type=DanhMuc.ChuanHoa(f.Type);var legacy=DanhMuc.Ten(type);query=query.Where(e=>(e.ServiceId==null&&e.Tags.Contains(legacy))||(e.ServiceId!=null&&db.DichVuDuLich.Any(s=>s.MaDichVu==e.ServiceId&&(s.LoaiDichVu==type||s.LoaiDichVu==legacy))));}
  foreach(var code in wanted)query=query.Where(e=>db.Set<DiscoveryFeature>().Any(a=>a.EntryId==e.Id&&a.Code==code&&a.State=="Yes"));
  foreach(var word in words)query=query.Where(e=>EF.Functions.Collate((e.Title+" "+e.Region+" "+e.Summary+" "+e.Tags+" "+e.Body).Replace("đ","d").Replace("Đ","D"),SearchCollation).Contains(word)||db.Set<DiscoveryQuestion>().Any(a=>a.EntryId==e.Id&&a.Status=="Answered"&&EF.Functions.Collate((a.Question+" "+a.Answer).Replace("đ","d").Replace("Đ","D"),SearchCollation).Contains(word)));
  if(f.Date is DateOnly date)query=query.Where(e=>(e.ValidFrom==null||e.ValidFrom<=date)&&(e.ValidTo==null||e.ValidTo>=date)&&(e.ServiceId==null||!db.DichVuDuLich.Any(s=>s.MaDichVu==e.ServiceId&&s.TamNgungTu!=null&&s.TamNgungTu<=date&&(s.TamNgungDen==null||s.TamNgungDen>=date))));
  // Published service prices remain owned by the original service, never by the enrichment copy.
  if(f.Min!=null)query=query.Where(e=>e.ServiceId!=null?db.DichVuDuLich.Any(s=>s.MaDichVu==e.ServiceId&&s.GiaThamKhao>=f.Min):e.PriceFrom>=f.Min);
  if(f.Max!=null)query=query.Where(e=>e.ServiceId!=null?db.DichVuDuLich.Any(s=>s.MaDichVu==e.ServiceId&&s.GiaThamKhao<=f.Max):e.PriceFrom<=f.Max);
  if(!string.IsNullOrWhiteSpace(f.Unit))query=query.Where(e=>e.ServiceId!=null?db.DichVuDuLich.Any(s=>s.MaDichVu==e.ServiceId&&s.DonViGia==f.Unit):e.PriceUnit==f.Unit);
  if(f.South!=null)query=query.Where(e=>e.Latitude>=(decimal)f.South&&e.Latitude<=(decimal)f.North!&&e.Longitude>=(decimal)f.West!&&e.Longitude<=(decimal)f.East!);
  if(f.Lat!=null){var lat=f.Lat.Value;var lng=f.Lng!.Value;var dy=f.RadiusKm/110.5;var dx=f.RadiusKm/(111.32*Math.Max(.01,Math.Cos(lat*Math.PI/180)));query=query.Where(e=>e.Latitude>=(decimal)(lat-dy)&&e.Latitude<=(decimal)(lat+dy)&&e.Longitude>=(decimal)(lng-dx)&&e.Longitude<=(decimal)(lng+dx));}
  if(map)query=query.Where(e=>e.Latitude!=null);
  var total=await query.CountAsync();var list=await query.OrderByDescending(e=>e.Title==f.Q).ThenByDescending(e=>e.Kind=="Destination").ThenByDescending(e=>e.UpdatedAt).ThenBy(e=>e.Id).Take(1500).ToListAsync();await Hydrate(list);
  var cards=list.Select(e=>new DiscoveryCard(e,f.Lat!=null&&e.Latitude!=null?DiscoveryText.Distance(f.Lat.Value,f.Lng!.Value,(double)e.Latitude.Value,(double)e.Longitude!.Value):null)).ToList();
  if(f.Lat!=null)cards=cards.Where(c=>c.DistanceKm<=f.RadiusKm).ToList();if(f.At!=null){Need(f.Date!=null,"Chọn ngày để tìm theo giờ mở cửa.",400);cards=cards.Where(c=>DiscoveryText.OpenAt(c.Entry,f.Date!.Value,f.At.Value)).ToList();}
  if(f.Lat!=null||f.At!=null||total<=1500)total=cards.Count;
  var mixed=cards.Where(c=>c.Entry.PriceFrom!=null).Select(c=>c.Entry.PriceUnit??"Chưa công bố").Distinct().Count()>1;
  if(f.Sort=="price")Need(!mixed,"Chọn cùng đơn vị giá trước khi sắp xếp theo giá.",400);
  cards=f.Sort switch{"distance"=>cards.OrderBy(c=>c.DistanceKm??double.MaxValue).ThenBy(c=>c.Entry.Id).ToList(),"price"=>cards.OrderBy(c=>c.Entry.PriceFrom??decimal.MaxValue).ThenBy(c=>c.Entry.Id).ToList(),"updated"=>cards.OrderByDescending(c=>c.Entry.CheckedOn).ThenBy(c=>c.Entry.Id).ToList(),_=>cards.OrderByDescending(c=>DiscoveryText.Fold(c.Entry.Title)==DiscoveryText.Fold(f.Q)).ThenByDescending(c=>c.Entry.Kind=="Destination").ThenBy(c=>c.Entry.Title).ThenBy(c=>c.Entry.Id).ToList()};
  var answerQuery=db.Set<DiscoveryQuestion>().AsNoTracking().Where(a=>a.Status=="Answered"&&query.Any(e=>e.Id==a.EntryId));
  if(!string.IsNullOrWhiteSpace(f.Region))answerQuery=answerQuery.Where(a=>all.Any(e=>e.Id==a.EntryId&&e.Region==f.Region));
  foreach(var word in words)answerQuery=answerQuery.Where(a=>EF.Functions.Collate((a.Question+" "+a.Answer).Replace("đ","d").Replace("Đ","D"),SearchCollation).Contains(word));
  var snippets=new List<DiscoverySnippet>();
  if(!map&&question.Intent!=null){foreach(var card in cards.Take(3)){var e=card.Entry;string answer;if(question.Intent=="Price")answer=e.PriceFrom!=null?$"Giá tham khảo: {Labels.Money(e.PriceFrom)} / {e.PriceUnit??"chưa công bố đơn vị"}. Đối chiếu hạng mục và điều kiện áp dụng trước khi chọn.":"Chưa có mức giá xác nhận trong thông tin đang công bố. Bạn có thể hỏi đơn vị quản lý tại mục Hỏi đáp.";else if(question.Intent=="Hours")answer=e.Hours??"Chưa có giờ phục vụ đã được xác nhận. Kiểm tra nguồn hoặc gửi câu hỏi trước khi đến.";else if(question.Intent=="Address")answer=(e.Address??"Chưa công bố địa chỉ chi tiết.")+" "+e.LocationNote;else{var feature=await db.Set<DiscoveryFeature>().AsNoTracking().SingleOrDefaultAsync(x=>x.EntryId==e.Id&&x.Code==question.Intent);answer=DiscoveryText.Features[question.Intent]+": "+DiscoveryText.State(feature?.State??"Unknown")+". "+(feature?.Note??"Gửi câu hỏi tại địa điểm để làm rõ điều kiện cụ thể.");}snippets.Add(new(e.Id,e.Title,answer,e.ServiceId!=null?"Thông tin dịch vụ đang công bố":e.SourceName,e.ServiceId!=null?"/Explore/Detail/"+e.ServiceId:e.SourceUrl,e.ServiceId!=null?null:e.CheckedOn));}}
  return new(f,cards.Skip(map?0:(f.Page-1)*20).Take(map?300:20).ToList(),Math.Min(total,1500),knownRegions.OrderBy(x=>x).ToList(),words,words.Count>0?await answerQuery.OrderByDescending(a=>a.AnsweredAt).Take(3).ToListAsync():[],await all.Where(e=>e.Kind=="Destination").OrderBy(e=>e.Title).Take(6).ToListAsync(),mixed,question.Intent,snippets,cards.Count==0?await all.Where(e=>e.Kind=="Guide"||e.Kind=="Transport").OrderBy(e=>e.Id).Take(3).ToListAsync():[]);
 }
 public async Task<object> Suggest(string? q)
 {
  if(string.IsNullOrWhiteSpace(q)||q.Length<2||q.Length>100)return Array.Empty<object>();var word=DiscoveryText.Fold(q.Trim());var list=await Public().AsNoTracking().Where(e=>EF.Functions.Collate((e.Title+" "+e.Region+" "+e.Tags).Replace("đ","d").Replace("Đ","D"),SearchCollation).Contains(word)).OrderByDescending(e=>e.Kind=="Destination").ThenBy(e=>e.Title).Take(8).ToListAsync();await Hydrate(list);return list.Select(e=>new{id=e.Id,title=e.Title,kind=DiscoveryText.Kind(e.Kind),url=DiscoveryText.Link(e.Id),lat=e.Latitude,lng=e.Longitude});
 }
 public async Task<DiscoveryDetailPage> Detail(long id,DiscoveryCommand? input=null,string? topic=null)
 {
  var e=await PublicEntry(id);await Hydrate([e]);Need(string.IsNullOrEmpty(topic)||DiscoveryText.Topics.ContainsKey(topic),"Chủ đề kinh nghiệm không hợp lệ.",400);
  var related=await Public().AsNoTracking().Where(r=>r.Id!=id&&(r.ParentId==id||(e.ParentId!=null&&r.ParentId==e.ParentId)||r.Region==e.Region)).OrderByDescending(r=>r.ParentId==id).ThenBy(r=>r.Id).Take(100).ToListAsync();await Hydrate(related);
  var cards=related.Select(r=>new DiscoveryCard(r,e.Latitude!=null&&r.Latitude!=null?DiscoveryText.Distance((double)e.Latitude.Value,(double)e.Longitude!.Value,(double)r.Latitude.Value,(double)r.Longitude!.Value):null)).OrderBy(r=>r.DistanceKm??double.MaxValue).Take(8).ToList();
  var questions=await db.Set<DiscoveryQuestion>().AsNoTracking().Where(q=>q.EntryId==id&&(q.Status=="Answered"||q.OwnerId==Me&&Me>0)).OrderByDescending(q=>q.CreatedAt).Take(30).ToListAsync();
  var experiences=await db.Set<DiscoveryExperience>().AsNoTracking().Where(q=>q.EntryId==id&&(q.Status=="Published"||q.OwnerId==Me&&Me>0)&&(topic==null||q.Topics.Contains(topic))).OrderByDescending(q=>q.VisitedOn).Take(30).ToListAsync();
  var ids=questions.Select(q=>q.OwnerId).Concat(questions.Where(q=>q.AnsweredBy!=null).Select(q=>q.AnsweredBy!.Value)).Concat(experiences.Select(q=>q.OwnerId)).Distinct().ToList();
  return new(e,await db.Set<DiscoveryFeature>().AsNoTracking().Where(a=>a.EntryId==id).ToListAsync(),await db.Set<DiscoveryPrice>().AsNoTracking().Where(a=>a.EntryId==id&&a.Active).OrderBy(a=>a.Id).ToListAsync(),cards,questions,experiences,await Names(ids),await db.Set<DiscoveryRevision>().AsNoTracking().Where(r=>r.EntryId==id&&r.Public).OrderByDescending(r=>r.Id).Take(5).Select(r=>new DiscoveryRevision{Id=r.Id,EntryId=r.EntryId,Summary=r.Summary,CreatedAt=r.CreatedAt,Public=true}).ToListAsync(),Me>0&&await db.Set<DiscoveryFollow>().AnyAsync(x=>x.EntryId==id&&x.OwnerId==Me&&x.Active),input??new(){EntryId=id});
 }
 async Task<Dictionary<long,string>> Names(List<long> ids)=>await db.HoSoNguoiDung.AsNoTracking().Where(p=>ids.Contains(p.MaTaiKhoan)).ToDictionaryAsync(p=>p.MaTaiKhoan,p=>p.TenHienThi);
 public async Task<List<DiscoveryDetailPage>> Compare(IEnumerable<long> ids){var keys=ids.Distinct().ToList();Need(keys.Count is >=2 and <=4,"Chọn từ 2 đến 4 lựa chọn để so sánh.",400);var rows=new List<DiscoveryDetailPage>();foreach(var id in keys)rows.Add(await Detail(id));Need(rows.Select(x=>x.Entry.Kind).Distinct().Count()==1,"Chỉ so sánh các lựa chọn cùng loại.",400);return rows;}
}
