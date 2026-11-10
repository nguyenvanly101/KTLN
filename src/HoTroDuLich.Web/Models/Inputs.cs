using System.ComponentModel.DataAnnotations;
namespace HoTroDuLich.Web.Models;
public class LoginInput {
 [Required,MaxLength(256)] [Display(Name="Email hoặc số điện thoại")] public string Email {get;set;}="";
 [Required,DataType(DataType.Password)] [Display(Name="Mật khẩu")] public string Password {get;set;}="";
 public string? ReturnUrl {get;set;}
}
public class RegisterInput : IValidatableObject {
 [Required,EmailAddress,MaxLength(256)] [Display(Name="Email")] public string Email {get;set;}="";
 [Required,MinLength(8),MaxLength(100)] [Display(Name="Mật khẩu")] public string Password {get;set;}="";
 [Compare(nameof(Password))] [Display(Name="Xác nhận mật khẩu")] public string ConfirmPassword {get;set;}="";
 [Required,MaxLength(100)] [Display(Name="Tên")] public string Name {get;set;}="";
 [RegularExpression("^0[0-9]{9,10}$")] [Display(Name="Số điện thoại")] public string? Phone {get;set;}
 public string Role {get;set;}="KHACH_DU_LICH";
 [MaxLength(200)] [Display(Name="Tên đơn vị")] public string? Organization {get;set;}
 [MaxLength(300)] [Display(Name="Địa chỉ")] public string? Address {get;set;}
 [MaxLength(50)] public string? OrganizationType {get;set;}
 [MaxLength(100)] public string? ContactName {get;set;}
 [RegularExpression("^0[0-9]{9,10}$")] public string? ContactPhone {get;set;}
 [EmailAddress,MaxLength(256)] public string? ContactEmail {get;set;}
 [MaxLength(2000)] public string? RegistrationDescription {get;set;}
 [MaxLength(100)] public string? Representative {get;set;}
 public List<IFormFile> Documents {get;set;}=[];
 public IEnumerable<ValidationResult> Validate(ValidationContext context) {
  if(!Password.Any(char.IsLetter)||!Password.Any(char.IsDigit)||(!string.IsNullOrWhiteSpace(Email)&&Password.Contains(Email,StringComparison.OrdinalIgnoreCase)))
   yield return new("Mật khẩu phải có chữ và số, không chứa email.", [nameof(Password)]);
  if(Role=="NHA_CUNG_CAP") {
   foreach(var (name,value) in new[]{(nameof(Organization),Organization),(nameof(Address),Address),(nameof(OrganizationType),OrganizationType),(nameof(ContactName),ContactName),(nameof(ContactPhone),ContactPhone),(nameof(ContactEmail),ContactEmail),(nameof(RegistrationDescription),RegistrationDescription),(nameof(Representative),Representative)})
    if(string.IsNullOrWhiteSpace(value))yield return new("Vui lòng nhập thông tin nhà cung cấp.",[name]);
   if(Documents.Count is <1 or >5)yield return new("Hồ sơ cần từ 1 đến 5 tài liệu xác minh.",[nameof(Documents)]);
  }
 }
}
public class CommandInput {
 public long Id {get;set;} public string Version {get;set;}="";
 [MaxLength(4000)] [Display(Name="Nội dung")] public string Text {get;set;}="";
 [MaxLength(500)] [Display(Name="Lý do")] public string Reason {get;set;}="";
 [MaxLength(500)] [Display(Name="Căn cứ")] public string Evidence {get;set;}="";
 [MaxLength(40)] public string Decision {get;set;}="";
 public int? BusinessVersion {get;set;}
 public int? BaseVersion {get;set;}
 public long ConnectionId {get;set;}
 [MaxLength(100)] public string? Representative {get;set;}
 public bool? Agree {get;set;}
 public DateOnly? Start {get;set;}
 public DateOnly? End {get;set;}
 [Range(1,100)] public int? People {get;set;}
 [Range(1,100)] public int? Quantity {get;set;}
 [Range(0,1000000000)] public decimal? Cost {get;set;}
 [MaxLength(100)] public string? ArrivalDeparture {get;set;}
 [MaxLength(1000)] public string? Selections {get;set;}
 public List<ResultItemInput> Items {get;set;}=[];
 public DateOnly? Until {get;set;}
 public List<IFormFile> Documents {get;set;}=[];
}
public class PlanInput : IValidatableObject {
 public long Id {get;set;} public string Version {get;set;}="";
 [Range(1,long.MaxValue)] public long? ContinueServiceId {get;set;}
 [Range(1,long.MaxValue)] public long? ContinuePlaceId {get;set;}
 [Required,MaxLength(150)] [Display(Name="Tên")] public string Name {get;set;}="";
 [Required,MaxLength(200)] [Display(Name="Điểm đến")] public string Destination {get;set;}="";
 [Display(Name="Ngày bắt đầu")] public DateOnly Start {get;set;}=HoTroDuLich.Web.Services.TravelService.Today.AddDays(7);
 [Display(Name="Ngày kết thúc")] public DateOnly End {get;set;}=HoTroDuLich.Web.Services.TravelService.Today.AddDays(9);
 [Range(0,1000000000)] [Display(Name="Ngân sách")] public decimal? Budget {get;set;}
 public string Status {get;set;}="Nhap";
 public IEnumerable<ValidationResult> Validate(ValidationContext context) {
  if(Start==default)yield return new("Vui lòng nhập ngày bắt đầu.",[nameof(Start)]);
  if(End<Start)yield return new("Ngày kết thúc phải từ ngày bắt đầu trở đi.",[nameof(End)]);
  if(Id==0&&Start<HoTroDuLich.Web.Services.TravelService.Today)yield return new("Ngày bắt đầu không được nằm trong quá khứ.",[nameof(Start)]);
 }
}
public class ActivityInput {
 [Range(1,1440,ErrorMessage="Thời lượng từ 1 đến 1440 phút.")] public int? DurationMinutes {get;set;}
 [Range(0,720,ErrorMessage="Di chuyển từ 0 đến 720 phút.")] public int? TravelMinutes {get;set;}
 public bool UpdateTiming {get;set;}
 public long Id {get;set;} public long PlanId {get;set;} public string Version {get;set;}="";
 public DateOnly Date {get;set;} public TimeOnly? Time {get;set;}
 [Required,MaxLength(300)] [Display(Name="Nội dung")] public string Content {get;set;}="";
 [Range(0,1000000000)] [Display(Name="Chi phí")] public decimal? Cost {get;set;}
 public long? ServiceId {get;set;} public long? PlaceId {get;set;}
}
public class NeedInput {
 public long Id {get;set;} public string Version {get;set;}="";
 [Required,MaxLength(200)] [Display(Name="Điểm đến")] public string Destination {get;set;}="";
 [Required,MaxLength(2000)] [Display(Name="Nội dung")] public string Content {get;set;}="";
 [Display(Name="Ngày bắt đầu")] public DateOnly Start {get;set;}=HoTroDuLich.Web.Services.TravelService.Today.AddDays(7);
 [Display(Name="Ngày kết thúc")] public DateOnly End {get;set;}=HoTroDuLich.Web.Services.TravelService.Today.AddDays(9);
 [Range(0,1000000000)] [Display(Name="Ngân sách")] public decimal? Budget {get;set;}
 [MaxLength(50)] [Display(Name="Hình thức du lịch")] public string? Style {get;set;}
 [MaxLength(1000)] [Display(Name="Mong muốn đồng hành")] public string? Wishes {get;set;}
 public string Privacy {get;set;}="KhiKetNoi";
}
public class AgreementInput {
 public long Id {get;set;} public string Version {get;set;}="";
 public string Name {get;set;}="Thỏa thuận đồng hành";
 [Required,MaxLength(200)] public string Destination {get;set;}="";
 public DateOnly Start {get;set;}=HoTroDuLich.Web.Services.TravelService.Today.AddDays(7);
 public DateOnly End {get;set;}=HoTroDuLich.Web.Services.TravelService.Today.AddDays(9);
 [Range(0,1000000000)] public decimal? Budget {get;set;}
 public long ConnectionId {get;set;}
 public int BaseVersion {get;set;}
 [MaxLength(1000)] [Display(Name="Phạm vi đi cùng")] public string? Scope {get;set;}
 public string Kind {get;set;}="NoiDung";
 [MaxLength(500)] [Display(Name="Lý do")] public string? Reason {get;set;}
}
public class ServiceInput {
 [Display(Name="Cơ sở áp dụng")] public List<long> BranchIds {get;set;}=[];
 [MaxLength(300)] [Display(Name="Địa chỉ phục vụ")] public string? Address {get;set;}
 [MaxLength(50)] [Display(Name="Đơn vị tính giá")] public string? PriceUnit {get;set;}
 [MaxLength(2000)] [Display(Name="Chính sách thay đổi/hủy")] public string? CancellationPolicy {get;set;}
 [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever] public string? PhotoPaths {get;set;}
 public List<IFormFile> Photos {get;set;}=[];
 public bool RemovePhotos {get;set;}
 public long Id {get;set;} public string Version {get;set;}="";
 [Required,MaxLength(200)] [Display(Name="Tên")] public string Name {get;set;}="";
 [Required,MaxLength(50)] [Display(Name="Loại hình")] public string Type {get;set;}="Lưu trú";
 [MaxLength(200)] [Display(Name="Khu vực")] public string? Region {get;set;}="";
 [MaxLength(4000)] [Display(Name="Mô tả")] public string? Description {get;set;}
 [Range(0,1000000000)] [Display(Name="Giá")] public decimal? Price {get;set;}
 [MaxLength(200)] [Display(Name="Thời gian phục vụ")] public string? Hours {get;set;}
 [MaxLength(2000)] [Display(Name="Điều kiện phục vụ")] public string? Terms {get;set;}
 public long? PlaceId {get;set;}
 public string Operation {get;set;}="GuiDang";
 public DateOnly? PauseFrom {get;set;} public DateOnly? PauseTo {get;set;}
 [MaxLength(500)] [Display(Name="Lý do tạm ngừng")] public string? PauseReason {get;set;}
}
public class BookingInput {
 [Range(1,long.MaxValue)] public long? BranchId {get;set;}
 [MaxLength(100)] public string? BranchVersion {get;set;}
 public long Id {get;set;} public long ServiceId {get;set;} public string Version {get;set;}="";
 [Range(1,long.MaxValue)] public long? PlanId {get;set;}
 public Guid SubmissionId {get;set;}=Guid.NewGuid();
 [Display(Name="Ngày bắt đầu")] public DateOnly Start {get;set;}=HoTroDuLich.Web.Services.TravelService.Today.AddDays(7);
 [Display(Name="Ngày kết thúc")] public DateOnly? End {get;set;}
 [Range(1,100)] [Display(Name="Số người")] public int People {get;set;}=1;
 [Range(1,100)] [Display(Name="Số lượng")] public int Quantity {get;set;}=1;
 [MaxLength(1000)] public string? Notes {get;set;}
 [MaxLength(100)] public string? ArrivalDeparture {get;set;}
 [MaxLength(1000)] public string? Selections {get;set;}
 [Range(0,1000000000)] public decimal? Cost {get;set;}
}
public class ChangeInput {
 public long Id {get;set;} public string Version {get;set;}="";
 public DateOnly? Start {get;set;} public DateOnly? End {get;set;}
 [Range(1,100)] public int? People {get;set;}
 [Range(1,100)] public int? Quantity {get;set;}
 public string Kind {get;set;}="ThayDoi";
 [MaxLength(500)] [Display(Name="Lý do")] public string Reason {get;set;}="";
 [MaxLength(2000)] [Display(Name="Điều kiện phục vụ")] public string? Terms {get;set;}
}
public class IncidentInput {
 public long PlanId {get;set;}
 [Required,MaxLength(50)] [Display(Name="Loại hình")] public string Type {get;set;}="Thời tiết";
 [Required,MaxLength(2000)] [Display(Name="Mô tả")] public string Description {get;set;}="";
 [MaxLength(300)] [Display(Name="Vị trí")] public string? Location {get;set;}
 public long? ActivityId {get;set;}
 public List<long> ActivityIds {get;set;}=[];
}
public class ProfileInput {
 public string Version {get;set;}="";
 [Required,MaxLength(100)] [Display(Name="Tên")] public string Name {get;set;}="";
 [RegularExpression("^0[0-9]{9,10}$")] [Display(Name="Số điện thoại")] public string? Phone {get;set;}
 [MaxLength(1000)] [Display(Name="Giới thiệu")] public string? Bio {get;set;}
 [MaxLength(200)] [Display(Name="Nơi sinh sống")] public string? City {get;set;}
 [MaxLength(500)] [Display(Name="Sở thích")] public string? Interests {get;set;}
 public string Privacy {get;set;}="RiengTu";
 public IFormFile? Avatar {get;set;}
 public string ProviderVersion {get;set;}="";
 [MaxLength(100)] public string? ContactName {get;set;}
 [MaxLength(300)] public string? Address {get;set;}
 [RegularExpression("^0[0-9]{9,10}$")] public string? ContactPhone {get;set;}
 [EmailAddress,MaxLength(256)] public string? ContactEmail {get;set;}

}

public class ResultItemInput {
 [Required,MaxLength(200)] public string Item {get;set;}="";
 [Required] public string Result {get;set;}="";
 [MaxLength(1000)] public string? Notes {get;set;}
}
public class PasswordChangeInput {
 [Required,DataType(DataType.Password)] public string OldPassword {get;set;}="";
 [Required,MinLength(8),MaxLength(100),DataType(DataType.Password)] public string NewPassword {get;set;}="";
 [Compare(nameof(NewPassword))] public string ConfirmPassword {get;set;}="";
}
public class DeactivationInput {
 [Required,DataType(DataType.Password)] public string Password {get;set;}="";
 public string? ConfirmationToken {get;set;}
}
