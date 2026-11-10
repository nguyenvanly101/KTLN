using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
namespace HoTroDuLich.Web.Models;

// The thesis uses Vietnamese request names. Older forms keep working during rollout.
public sealed class FullTextValueProviderFactory : IValueProviderFactory
{
 private static readonly Dictionary<string,string[]> Names=new(StringComparer.OrdinalIgnoreCase){
  ["Name"]=["ten","tenKeHoach","tenHienThi","tenDichVu"],["Email"]=["dinhDanh","email"],["Password"]=["matKhau"],["ConfirmPassword"]=["xacNhanMatKhau","xacNhan"],["Role"]=["vaiTro","loaiTaiKhoan"],["Phone"]=["soDienThoai","sdt"],
  ["Organization"]=["tenDonVi"],["OrganizationType"]=["loaiHinh"],["Address"]=["diaChi"],["ContactName"]=["nguoiLienHe"],["ContactPhone"]=["soDienThoaiLienHe","sdtLienHe"],["ContactEmail"]=["emailLienHe"],["RegistrationDescription"]=["noiDungDangKy"],["Representative"]=["nguoiDaiDien"],
  ["Version"]=["rowVersionHoSo","rowVersion","soPhienBan"],["ProviderVersion"]=["rowVersionNCC"],["BusinessVersion"]=["phienBan"],["BaseVersion"]=["phienBanCoSo"],["PlanId"]=["maKeHoach"],["ConnectionId"]=["maKetNoi"],["ServiceId"]=["maDichVu"],["PlaceId"]=["maDiaDiem"],["SubmissionId"]=["maLuotGui"],
  ["Start"]=["ngayBDMoi","ngayBatDauMoi","ngayBD","ngayDi","ngayBatDau"],["End"]=["ngayKTMoi","ngayKetThucMoi","ngayKT","ngayVe","ngayKetThuc"],["Budget"]=["nganSach","nganSachDuKien"],["Destination"]=["noiDen","diemDen"],["Content"]=["noiDung"],["Wishes"]=["mongMuonDongHanh"],["Style"]=["hinhThucDi"],["Privacy"]=["phamViChiaSe","thongTinChiaSe"],
  ["Date"]=["ngay"],["Time"]=["gio"],["Cost"]=["chiPhiDuKien"],["People"]=["soNguoiMoi","soNguoi"],["Quantity"]=["soLuongMoi","soLuong"],["ArrivalDeparture"]=["thoiGianDenDi"],["Selections"]=["noiDungLuaChon"],["Notes"]=["ghiChu"],["Terms"]=["dieuKienMoi","dieuKienPhucVu"],
  ["Type"]=["loaiDichVu","loaiTinhHuong"],["Region"]=["khuVucPhucVu"],["Description"]=["moTa"],["Price"]=["giaThamKhao"],["Hours"]=["thoiGianPhucVu"],["PauseFrom"]=["tamNgungTu"],["PauseTo"]=["tamNgungDen"],["PauseReason"]=["lyDoTamNgung"],
  ["Kind"]=["loai","loaiPhienBan"],["Scope"]=["phamViDiCung"],["Reason"]=["lyDo","ketQuaXemXet"],["Evidence"]=["canCu"],["Until"]=["thoiHan"],["Decision"]=["quyetDinh","thaoTac"],["Operation"]=["thaoTac"],
  ["Text"]=["loiGioiThieu","dieuKienDeXuat","noiDungPhuongAn","ketQuaThucTe","noiDungPhanAnh","noiDungDangKy","ketQuaXacMinh","noiDung"],["Location"]=["viTri"],["ActivityIds"]=["dsMaHoatDong","maHoatDong"],["Bio"]=["gioiThieu"],["City"]=["noiSinhSong"],["Interests"]=["soThich"],["OldPassword"]=["matKhauCu"],["NewPassword"]=["matKhauMoi"],["ConfirmationToken"]=["maXacNhan"],["Agree"]=["dongY"],
 };
 private static string IdName(string controller)=>controller switch{
  "KhoiTaoKeHoach" or "XacNhanKeHoach"=>"maKeHoach","QuanLyLichTrinh"=>"maHoatDong","TheoDoiTinhHuong" or "XuLyPhuongAn"=>"maTinhHuong","CongBoNhuCau" or "GuiDeNghiKetNoi"=>"maNhuCau","PhanHoiDeNghi" or "XuLyDeNghiDieuChinh" or "GuiDeNghiDieuChinh"=>"maDeNghi","XacNhanThoaThuan" or "KetThucThoaThuan"=>"maThoaThuan","DangDichVu" or "CapNhatDichVu"=>"maDichVu","QuyetDinhDuyet"=>"maBanCapNhat","TheoDoiYeuCau" or "XuLyYeuCauDat" or "GhiNhanKetQua"=>"maYeuCau","PhanAnhKetQua"=>"maKetQua","XetHoSoXacMinh" or "BoSungHoSo"=>"maHoSo","QuyetDinhQuyenHoatDong"=>"maNCC","GiaiTrinh"=>"maQuyetDinh",_=>"id"};
 public async Task CreateValueProviderAsync(ValueProviderFactoryContext context){
  var request=context.ActionContext.HttpContext.Request;var data=new Dictionary<string,StringValues>(StringComparer.OrdinalIgnoreCase);
  foreach(var item in context.ActionContext.RouteData.Values)if(item.Value!=null)data[item.Key]=item.Value.ToString();foreach(var item in request.Query)data[item.Key]=item.Value;
  if(request.HasFormContentType){var form=await request.ReadFormAsync();foreach(var item in form)data[item.Key]=item.Value;
   if(form.Files.Any(x=>x.Name is "taiLieu" or "anhDaiDien")){var files=new FormFileCollection();foreach(var f in form.Files){if(f.Name is "taiLieu" or "anhDaiDien"){var stream=f.OpenReadStream();context.ActionContext.HttpContext.Response.RegisterForDispose(stream);files.Add(new FormFile(stream,0,f.Length,f.Name=="taiLieu"?"Documents":"Avatar",f.FileName){Headers=f.Headers,ContentType=f.ContentType});}else files.Add(f);}request.Form=new FormCollection(form.ToDictionary(x=>x.Key,x=>x.Value),files);}
  }
  var aliases=new Dictionary<string,StringValues>(StringComparer.OrdinalIgnoreCase);foreach(var (target,sources) in Names){if(data.TryGetValue(target,out var direct)){if(target is "Phone" or "ContactPhone")aliases[target]=HoTroDuLich.Web.Services.TravelService.NormalizePhone(direct.ToString());continue;}foreach(var source in sources)if(data.TryGetValue(source,out var value)){aliases[target]=target is "Phone" or "ContactPhone"?HoTroDuLich.Web.Services.TravelService.NormalizePhone(value.ToString()):value;break;}}
  var controller=context.ActionContext.RouteData.Values["controller"]?.ToString()??"";
  if(controller is "Discovery" or "DiscoveryApi" or "Explore" or "Places" or "Branches" or "BranchAdmin" or "Landmarks" or "MapsApi")foreach(var pair in data){if(System.Text.RegularExpressions.Regex.IsMatch(pair.Key,@"(^|\.)(Latitude|Longitude|Lat|Lng|Min|Max|PriceFrom|PriceTo|Amount|RadiusKm|South|West|North|East|CorridorKm|MaxDetourMinutes)$",System.Text.RegularExpressions.RegexOptions.IgnoreCase))aliases[pair.Key]=pair.Value;}
  var idName=IdName(controller);if(!data.ContainsKey("Id")&&data.TryGetValue(idName,out var id))aliases["Id"]=id;
  // HTML number controls and the expense API send decimal points regardless of display culture.
  if(controller=="Expenses"&&data.TryGetValue("Amount",out var amount))aliases["Amount"]=amount;
  if(controller=="QuyetDinhQuyenHoatDong"&&context.ActionContext.RouteData.Values["action"]?.ToString()=="XemXetGiaiTrinh"&&data.TryGetValue("maGiaiTrinh",out var g))aliases["Id"]=g;
  foreach(var (key,value) in data){var match=System.Text.RegularExpressions.Regex.Match(key,@"^dsKetQua\[(\d+)\]\.(hangMuc|ketQua|ghiChu)$",System.Text.RegularExpressions.RegexOptions.IgnoreCase);if(match.Success)aliases[$"Items[{match.Groups[1].Value}].{match.Groups[2].Value.ToLowerInvariant() switch{"hangmuc"=>"Item","ketqua"=>"Result",_=>"Notes"}}"]=value;}
  if(aliases.Count>0)context.ValueProviders.Insert(0,new AliasValueProvider(aliases));
 }
 private sealed class AliasValueProvider(Dictionary<string,StringValues> data):IEnumerableValueProvider{
  public bool ContainsPrefix(string prefix)=>data.Keys.Any(k=>k.Equals(prefix,StringComparison.OrdinalIgnoreCase)||k.StartsWith(prefix+".",StringComparison.OrdinalIgnoreCase)||k.StartsWith(prefix+"[",StringComparison.OrdinalIgnoreCase));
  public ValueProviderResult GetValue(string key)=>data.TryGetValue(key,out var value)?new(value,CultureInfo.InvariantCulture):ValueProviderResult.None;
  public IDictionary<string,string> GetKeysFromPrefix(string prefix){var result=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);foreach(var key in data.Keys.Where(k=>k.StartsWith(prefix+"[",StringComparison.OrdinalIgnoreCase))){var end=key.IndexOf(']',prefix.Length);if(end>prefix.Length)result[key[(prefix.Length+1)..end]]=key[..(end+1)];}return result;}
 }
}
