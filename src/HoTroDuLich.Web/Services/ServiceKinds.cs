using System.Text.Json;
using System.Text.RegularExpressions;
using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

/// <summary>Mot loai dich vu: ma, ten, nhom, kha nang (co/khong dat duoc, co gia, co gio, co vi tri, theo thoi han) va bo truong rieng (khuon).</summary>
public sealed record KindInfo(string Code,string Label,string Group,int Sort,bool Bookable,bool ProviderPostable,bool ForBranch,bool HasPrice,bool HasHours,bool HasLocation,bool TimeBound,bool Active,bool Builtin,TemplateField[] Fields);

/// <summary>Danh muc loai dich vu dung chung cho dich vu, co so, thong tin kham pha. Them loai moi = them mot dong trong bang LoaiDichVu (khong sua ma nguon).</summary>
public static class ServiceKinds
{
 public static readonly (string Key,string Label)[] Groups=[("Place","Tham quan"),("Eat","Ăn uống"),("Stay","Lưu trú"),("Move","Di chuyển"),("Experience","Trải nghiệm"),("Info","Thông tin"),("Local","Dịch vụ địa phương"),("Other","Khác")];
 public static readonly string[] FieldKinds=["select","multi","text","number","time","date","list"];
 static string[] O(params string[] v)=>v;
 static TemplateField[] Spec(string code)=>code switch
 {
  "AnUong"=>[
   new("kieu","Kiểu quán","select",O("Quán ăn","Nhà hàng","Quán cà phê","Quán vỉa hè","Quán nhậu","Tiệm bánh"),Tag:true),
   new("chongoi","Chỗ ngồi","multi",O("Trong nhà","Ngoài trời","Phòng riêng","Sân vườn","Ban công"),Tag:true),
   new("phucvu","Hình thức phục vụ","multi",O("Ăn tại chỗ","Mang đi","Giao tận nơi","Đặt bàn trước"),Tag:true),
   new("chedoan","Chế độ ăn","multi",O("Có món chay","Thuần chay","Halal","Không gluten"),Tag:true),
   new("succhua","Sức chứa (người)","number",Max:2000),
   new("thanhtoan","Thanh toán","multi",O("Tiền mặt","Chuyển khoản / QR","Thẻ")),
   new("dacsac","Món đặc trưng","text",Max:300),
   new("thucdon","Thực đơn nổi bật","list",Max:15,Hint:"Mỗi dòng một món: Tên món | giá (₫)")],
  "LuuTru"=>[
   new("hang","Hạng cơ sở","select",O("Khách sạn","Homestay","Resort","Hostel","Căn hộ / Villa"),Tag:true),
   new("sao","Số sao","select",O("Chưa xếp hạng","1 sao","2 sao","3 sao","4 sao","5 sao"),Tag:true),
   new("nhanphong","Giờ nhận phòng","time"),new("traphong","Giờ trả phòng","time"),
   new("tiennghi","Tiện nghi","multi",O("Wi-Fi","Bữa sáng","Bể bơi","Điều hòa","Đưa đón sân bay","Giữ xe","Gần biển","Cho phép thú cưng","Có thang máy"),Tag:true),
   new("phong","Loại phòng","list",Max:10,Hint:"Mỗi dòng một loại phòng: Tên phòng (số khách, giường) | giá mỗi đêm (₫)"),
   new("huy","Chính sách hủy","select",O("Miễn phí hủy trước 24 giờ","Miễn phí hủy trước 3 ngày","Không hoàn hủy","Liên hệ cơ sở"))],
  "HoatDong"=>[
   new("loai","Loại điểm / hoạt động","select",O("Danh thắng","Bảo tàng","Chùa / đền","Công viên","Bãi biển","Tour / trải nghiệm","Vui chơi giải trí"),Tag:true),
   new("giave","Giá vé","list",Max:8,Hint:"Mỗi dòng một đối tượng: Người lớn | giá (₫)"),
   new("thoigian","Thời gian tham quan gợi ý","text",Max:100),
   new("phuhop","Phù hợp với","multi",O("Trẻ em","Người cao tuổi","Gia đình","Cặp đôi","Nhóm bạn","Đi một mình"),Tag:true),
   new("luuy","Lưu ý","multi",O("Đi bộ nhiều","Cần trang phục kín đáo","Nên đi buổi sáng","Cần đặt trước","Phụ thuộc thời tiết")),
   new("xelan","Lối đi xe lăn","select",O("Có","Một phần","Không"),Tag:true)],
  "PhuongTien"=>[
   new("loai","Hình thức","select",O("Thuê xe máy","Thuê ô tô","Taxi / xe công nghệ","Xe buýt","Tàu hỏa","Máy bay","Đưa đón","Thuyền / tàu"),Tag:true),
   new("tuyen","Tuyến / khu vực phục vụ","text",Max:200),
   new("banggia","Bảng giá","list",Max:10,Hint:"Mỗi dòng một loại xe hoặc chặng: Tên | giá (₫)"),
   new("thutuc","Thủ tục","multi",O("Cần bằng lái","Cần đặt cọc","Giao xe tận nơi","Có bảo hiểm"),Tag:true),
   new("giochay","Giờ chạy / giờ phục vụ","text",Max:200)],
  "Tour"=>[
   new("thoiluong","Thời lượng","text",Max:60,Hint:"Ví dụ: 1 ngày, 2 ngày 1 đêm"),
   new("khoihanh","Lịch khởi hành","text",Max:200),
   new("diemhen","Điểm đón","text",Max:200),
   new("baogom","Giá đã bao gồm","list",Max:12,Hint:"Mỗi dòng một mục"),
   new("khongbaogom","Chưa bao gồm","list",Max:12,Hint:"Mỗi dòng một mục"),
   new("ngonngu","Ngôn ngữ hướng dẫn","multi",O("Tiếng Việt","Tiếng Anh","Tiếng Hàn","Tiếng Trung","Tiếng Nhật"),Tag:true),
   new("toida","Số khách tối đa","number",Max:200),
   new("phuhop","Phù hợp với","multi",O("Trẻ em","Người cao tuổi","Gia đình","Cặp đôi","Nhóm bạn","Đi một mình"),Tag:true)],
  "GiaiTri"=>[
   new("loai","Loại hình","select",O("Công viên chủ đề","Khu vui chơi trẻ em","Rạp / biểu diễn","Karaoke / bar","Trò chơi trải nghiệm","Khác"),Tag:true),
   new("giave","Giá vé","list",Max:8,Hint:"Mỗi dòng một đối tượng: Người lớn | giá (₫)"),
   new("dotuoi","Độ tuổi phù hợp","text",Max:100),
   new("phuhop","Phù hợp với","multi",O("Trẻ em","Gia đình","Cặp đôi","Nhóm bạn"),Tag:true)],
  "SucKhoe"=>[
   new("loai","Loại hình","select",O("Spa","Massage","Yoga / thiền","Phòng khám","Nhà thuốc","Khác"),Tag:true),
   new("banggia","Bảng giá dịch vụ","list",Max:12,Hint:"Mỗi dòng một dịch vụ: Tên | giá (₫)"),
   new("luuy","Lưu ý","text",Max:300)],
  "SuKien"=>[
   new("ngaybatdau","Ngày bắt đầu","date",Tag:true),new("ngayketthuc","Ngày kết thúc","date"),
   new("giobatdau","Giờ bắt đầu","time"),
   new("diadiem","Địa điểm tổ chức","text",Max:200),
   new("donvi","Đơn vị tổ chức","text",Max:150),
   new("vaocua","Hình thức tham dự","select",O("Miễn phí","Có vé","Cần đăng ký trước"),Tag:true),
   new("loaisukien","Loại sự kiện","select",O("Lễ hội","Âm nhạc","Ẩm thực","Văn hóa","Thể thao","Hội chợ / triển lãm","Khác"),Tag:true),
   new("lienhe","Liên hệ / đăng ký","text",Max:200)],
  "CamNang"=>[
   new("thoidiemdep","Thời điểm đẹp","text",Max:100),
   new("khihau","Khí hậu","text",Max:300),
   new("chiphithap","Chi phí/người/ngày: tiết kiệm (₫)","number",Max:100000000),
   new("chiphivua","Chi phí/người/ngày: vừa phải (₫)","number",Max:100000000),
   new("chiphisang","Chi phí/người/ngày: thoải mái (₫)","number",Max:100000000),
   new("nenthu","Nên thử, nên đến","list",Max:12,Hint:"Mỗi dòng một ý"),
   new("luuy","Lưu ý","list",Max:12,Hint:"Mỗi dòng một ý"),
   new("dilai","Đi lại","text",Max:800),
   new("khancap","Số khẩn cấp, bệnh viện địa phương","text",Max:600)],
  "HuongDanVien"=>[
   new("ngonngu","Ngôn ngữ","multi",O("Tiếng Việt","Tiếng Anh","Tiếng Hàn","Tiếng Trung","Tiếng Nhật","Tiếng Pháp"),Tag:true),
   new("chuyenmon","Chuyên môn","multi",O("Văn hóa - lịch sử","Ẩm thực","Thiên nhiên - trekking","Chụp ảnh","Phượt xe máy","Tour gia đình"),Tag:true),
   new("vung","Khu vực hoạt động","text",Max:200),
   new("kinhnghiem","Số năm kinh nghiệm","number",Max:60),
   new("giayphep","Giấy phép hướng dẫn viên","select",O("Có thẻ hướng dẫn viên","Hướng dẫn viên địa phương (chưa có thẻ)"),Tag:true),
   new("banggia","Bảng giá","list",Max:8,Hint:"Mỗi dòng một gói: Tên gói (thời lượng) | giá (₫)")],
  "MuaSam"=>[
   new("nhom","Nhóm hàng","multi",O("Đặc sản ăn được","Quà lưu niệm","Thủ công mỹ nghệ","Thời trang","Chợ / siêu thị"),Tag:true),
   new("thanhtoan","Thanh toán","multi",O("Tiền mặt","Chuyển khoản / QR","Thẻ")),
   new("noibat","Mặt hàng nổi bật","list",Max:12,Hint:"Mỗi dòng một mặt hàng: Tên | giá (₫)")],
  "DichVuHoTro"=>[
   new("loai","Loại dịch vụ","select",O("SIM / Wi-Fi","Đổi tiền","Giặt ủi","Gửi hành lý","Cho thuê thiết bị","Khác"),Tag:true),
   new("banggia","Bảng giá","list",Max:10,Hint:"Mỗi dòng một dịch vụ: Tên | giá (₫)"),
   new("luuy","Lưu ý","text",Max:300)],
  _=>[new("ghichu","Ghi chú thêm cho khách","text",Max:300)]
 };
 static KindInfo K(string code,string label,string group,int sort,bool bk,bool po,bool br,bool pr,bool hr,bool lo,bool tb)=>new(code,label,group,sort,bk,po,br,pr,hr,lo,tb,true,true,Spec(code));
 static KindInfo[] Defaults()=>[
  K("HoatDong","Tham quan & hoạt động","Place",10,true,true,true,true,true,true,false),
  K("AnUong","Ăn uống","Eat",20,true,true,true,true,true,true,false),
  K("LuuTru","Lưu trú","Stay",30,true,true,true,true,true,true,false),
  K("PhuongTien","Di chuyển","Move",40,true,true,true,true,true,true,false),
  K("Tour","Tour trọn gói","Experience",50,true,true,true,true,true,true,false),
  K("GiaiTri","Vui chơi giải trí","Experience",60,true,true,true,true,true,true,false),
  K("SucKhoe","Spa & sức khỏe","Experience",70,true,true,true,true,true,true,false),
  K("SuKien","Sự kiện","Info",80,false,true,false,true,false,true,true),
  K("CamNang","Cẩm nang","Info",90,false,false,false,false,false,false,false),
  K("HuongDanVien","Hướng dẫn viên","Local",100,true,true,false,true,false,false,false),
  K("MuaSam","Mua sắm & đặc sản","Local",110,false,true,true,true,true,true,false),
  K("DichVuHoTro","Dịch vụ hỗ trợ","Local",120,true,true,true,true,true,true,false),
  K("Khac","Khác","Other",999,true,true,true,true,true,true,false)];

 static volatile KindInfo[] items=Defaults();
 public static IReadOnlyList<KindInfo> All=>items;
 public static KindInfo? Get(string? code){code=DanhMuc.ChuanHoaMa(code);return items.FirstOrDefault(k=>string.Equals(k.Code,code,StringComparison.OrdinalIgnoreCase));}
 public static string Label(string? code)=>Get(code)?.Label??code??"";
 public static TemplateField[] Fields(string? code)=>Get(code)?.Fields??[];
 /// <summary>Ma cac loai nha cung cap duoc dang (dich vu).</summary>
 public static string[] ServiceCodes=>items.Where(k=>k.Active&&k.ProviderPostable).OrderBy(k=>k.Sort).Select(k=>k.Code).ToArray();
 /// <summary>Ma cac loai co so (dia diem kinh doanh).</summary>
 public static string[] BranchCodes=>items.Where(k=>k.Active&&k.ForBranch).OrderBy(k=>k.Sort).Select(k=>k.Code).ToArray();
 public static string[] AllCodes=>items.Select(k=>k.Code).ToArray();
 /// <summary>Loai thong tin kham pha (Guide, Event, Transport, Place...) tuong ung loai dich vu de dung chung khuon.</summary>
 public static string ForDiscoveryKind(string? kind)=>kind switch{"Guide"=>"CamNang","Event"=>"SuKien","Transport"=>"PhuongTien","Place"=>"HoatDong","Amenity"=>"DichVuHoTro",_=>"Khac"};
 public static string GroupLabel(string key)=>Groups.FirstOrDefault(g=>g.Key==key).Label??key;

 public static async Task LoadAsync(AppDbContext db)
 {
  var rows=await db.Set<LoaiDichVuRow>().AsNoTracking().OrderBy(r=>r.SortOrder).ThenBy(r=>r.Code).ToListAsync();
  if(rows.Count==0)return;
  var defaults=Defaults().ToDictionary(k=>k.Code);
  items=rows.Select(r=>new KindInfo(r.Code,r.Label,r.GroupCode,r.SortOrder,r.Bookable,r.ProviderPostable,r.ForBranch,r.HasPrice,r.HasHours,r.HasLocation,r.TimeBound,r.Active,r.Builtin,
   string.IsNullOrWhiteSpace(r.FieldsJson)?(defaults.TryGetValue(r.Code,out var d)?d.Fields:Spec("")):ParseFieldsJson(r.FieldsJson))).ToArray();
 }
 /// <summary>Kiem tra dich vu theo kha nang cua loai: loai co han (su kien) bat buoc co ngay, ngay ket thuc khong truoc ngay bat dau.</summary>
 public static void ValidateService(KindInfo kind,Models.ServiceInput i)
 {
  if(!kind.HasPrice&&i.Price!=null)throw new BusinessException("Loại \""+kind.Label+"\" không có giá.",422);
  if(!kind.TimeBound)return;
  var data=BranchTemplates.Parse(i.AttributesJson);
  if(!data.TryGetValue("ngaybatdau",out var start)||start.ValueKind!=JsonValueKind.String)throw new BusinessException("Loại \""+kind.Label+"\" cần ngày bắt đầu.",422);
  if(data.TryGetValue("ngayketthuc",out var end)&&end.ValueKind==JsonValueKind.String&&string.CompareOrdinal(end.GetString(),start.GetString())<0)throw new BusinessException("Ngày kết thúc không được trước ngày bắt đầu.",422);
  var last=data.TryGetValue("ngayketthuc",out var e2)&&e2.ValueKind==JsonValueKind.String?e2.GetString():start.GetString();
  if(string.CompareOrdinal(last,TravelService.Today.ToString("yyyy-MM-dd"))<0)throw new BusinessException("Thời gian diễn ra đã qua.",422);
 }
 /// <summary>Loai co han: da ket thuc chua (dung de an khoi danh sach khach).</summary>
 public static bool Expired(string? kindCode,string? attributesJson,DateOnly today)
 {
  var kind=Get(kindCode);if(kind==null||!kind.TimeBound)return false;
  var data=BranchTemplates.Parse(attributesJson);
  string? last=null;
  if(data.TryGetValue("ngayketthuc",out var end)&&end.ValueKind==JsonValueKind.String)last=end.GetString();
  else if(data.TryGetValue("ngaybatdau",out var start)&&start.ValueKind==JsonValueKind.String)last=start.GetString();
  return last!=null&&string.CompareOrdinal(last,today.ToString("yyyy-MM-dd"))<0;
 }
 static readonly JsonSerializerOptions Json=new(){PropertyNameCaseInsensitive=true};
 public static TemplateField[] ParseFieldsJson(string json){try{return JsonSerializer.Deserialize<TemplateField[]>(json,Json)??[];}catch(JsonException){return [];}}
 public static string FieldsJson(TemplateField[] f)=>JsonSerializer.Serialize(f);

 // ---- bien tap bo truong bang van ban (moi dong mot truong): key | Nhan | kieu | lua chon (a;b;c) | toi da | goi y | tag (x)
 public static string ToLines(TemplateField[] fields)=>string.Join("\n",fields.Select(f=>string.Join(" | ",f.Key,f.Label,f.Kind,f.Options==null?"":string.Join(";",f.Options),f.Kind is "number" or "text" or "list"?f.Max.ToString():"",f.Hint??"",f.Tag?"x":"")));
 public static TemplateField[] ParseLines(string? text)
 {
  var result=new List<TemplateField>();var keys=new HashSet<string>();
  foreach(var raw in (text??"").Replace("\r","").Split('\n',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries))
  {
   var p=raw.Split('|',StringSplitOptions.TrimEntries);
   if(p.Length<3)throw new BusinessException("Dòng \""+raw+"\" cần dạng: khóa | nhãn | kiểu | lựa chọn | tối đa | gợi ý | tag.",422);
   if(!Regex.IsMatch(p[0],"^[a-z0-9]{1,20}$"))throw new BusinessException("Khóa \""+p[0]+"\" chỉ gồm chữ thường không dấu và số (tối đa 20 ký tự).",422);
   if(!keys.Add(p[0]))throw new BusinessException("Khóa \""+p[0]+"\" bị trùng.",422);
   if(p[1].Length is 0 or >60)throw new BusinessException("Nhãn của \""+p[0]+"\" từ 1 đến 60 ký tự.",422);
   if(!FieldKinds.Contains(p[2]))throw new BusinessException("Kiểu \""+p[2]+"\" không hợp lệ (chọn: "+string.Join(", ",FieldKinds)+").",422);
   string[]? options=null;
   if(p[2] is "select" or "multi")
   {
    options=(p.Length>3?p[3]:"").Split(';',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
    if(options.Length is < 2 or > 20||options.Any(o=>o.Length>60))throw new BusinessException("Trường \""+p[0]+"\" cần từ 2 đến 20 lựa chọn, mỗi lựa chọn tối đa 60 ký tự.",422);
   }
   var max=300;if(p.Length>4&&p[4].Length>0&&!int.TryParse(p[4],out max))throw new BusinessException("Giá trị tối đa của \""+p[0]+"\" phải là số.",422);
   if(max is < 1 or > 1000000000)throw new BusinessException("Giá trị tối đa của \""+p[0]+"\" không hợp lệ.",422);
   result.Add(new TemplateField(p[0],p[1],p[2],options,max,p.Length>5&&p[5].Length>0?p[5]:null,p.Length>6&&p[6].Equals("x",StringComparison.OrdinalIgnoreCase)));
  }
  if(result.Count>30)throw new BusinessException("Tối đa 30 trường cho một loại dịch vụ.",422);
  return result.ToArray();
 }
}
