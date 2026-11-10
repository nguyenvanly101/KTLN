using HoTroDuLich.Web.Services;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
namespace HoTroDuLich.Web.Controllers;

public abstract class AppController(TravelService service):Controller
{
 protected readonly TravelService S=service;
 protected async Task LoadTripContext(long? planId) {
  if(planId is long id && User.IsInRole(TravelService.Traveler)) ViewBag.TripPlan=await S.OwnPlan(id);
 }
 protected ViewResult V(string path,object? model=null){var parts=path.Split('/');ViewData["UiController"]=parts[0];ViewData["UiAction"]=parts.Length>1?parts[1]:"Index";return View("/Views/"+path+".cshtml",model);}
 public override async Task OnActionExecutionAsync(ActionExecutingContext context,ActionExecutionDelegate next){
  if(HttpMethods.IsPost(Request.Method)&&S.Role==TravelService.Traveler&&(Request.Path=="/yeu-cau/gui-yeu-cau"||Request.Path=="/Bookings/Submit")&&context.ActionArguments.Values.OfType<BookingInput>().FirstOrDefault() is BookingInput repeat&&await S.ExistingSubmission(repeat.SubmissionId) is long existing){context.Result=Done("/yeu-cau/"+existing,"Yêu cầu này đã được gửi. Đang mở lại yêu cầu của bạn.");return;}
  // A package reuses its request model for opening an empty form and posting it.
  // GET validates route/query fields; form-field validation belongs to POST.
  if(HttpMethods.IsGet(Request.Method)){foreach(var input in context.ActionArguments.Values.Where(x=>x is LoginInput or RegisterInput or ProfileInput or PlanInput or ActivityInput or NeedInput or AgreementInput or ServiceInput or BookingInput or ChangeInput or IncidentInput or PasswordChangeInput or DeactivationInput or CommandInput or CompanionCommand)){var type=input!.GetType();if(type.Name.EndsWith("ViewModel"))type=type.BaseType!;var fields=type.GetProperties().Select(p=>p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);foreach(var key in ModelState.Keys.Where(k=>fields.Contains(k.Split('.').Last())).ToArray())ModelState.Remove(key);}}
  if(HttpMethods.IsPost(Request.Method)&&ModelState.IsValid){foreach(var input in context.ActionArguments.Values.Where(x=>x is LoginInput or RegisterInput or ProfileInput or PlanInput or ActivityInput or NeedInput or AgreementInput or ServiceInput or BookingInput or ChangeInput or IncidentInput or PasswordChangeInput or DeactivationInput or CommandInput or CompanionCommand)){var errors=new List<ValidationResult>();Validator.TryValidateObject(input!,new ValidationContext(input!),errors,true);foreach(var error in errors)ModelState.AddModelError(error.MemberNames.FirstOrDefault()??"",error.ErrorMessage??"Vui lòng kiểm tra dữ liệu đã nhập.");}}
  if(!ModelState.IsValid){Response.StatusCode=400;context.Result=await InvalidForm(context);return;}
  var executed=await next();
  if(executed.Exception is BusinessException b){Response.StatusCode=b.Status;ViewData["ErrorCode"]=b.Code;ModelState.AddModelError(b.Field??"",b.Message);executed.Result=b.Status is 400 or 422?await InvalidForm(context):Failure(b.Message);executed.ExceptionHandled=true;}
  else if(executed.Exception is DbUpdateConcurrencyException){Response.StatusCode=409;executed.Result=Failure("Dữ liệu vừa được cập nhật. Hãy mở lại phiên bản mới trước khi thực hiện.");executed.ExceptionHandled=true;}
  else if(executed.Exception is DbUpdateException d&&LoiCsdl.So(d) is 547 or 2601 or 2627){var number=LoiCsdl.So(d);Response.StatusCode=number==547?422:409;executed.Result=Failure(number==547?"Dữ liệu chưa đáp ứng điều kiện nghiệp vụ. Thao tác chưa được lưu.":"Dữ liệu đã thay đổi hoặc bị trùng. Hãy tải lại và kiểm tra.");executed.ExceptionHandled=true;}
 }
 private ViewResult Failure(string message){ViewData["Message"]=message;var referer=Request.Headers.Referer.ToString();if(Uri.TryCreate(referer,UriKind.Absolute,out var uri)&&uri.Authority==Request.Host.Value)ViewData["ReloadUrl"]=uri.PathAndQuery;return V("Shared/Failure");}
 private async Task<IActionResult> InvalidForm(ActionExecutingContext context){
  var input=context.ActionArguments.Values.FirstOrDefault(x=>x is LoginInput or RegisterInput or ProfileInput or PlanInput or ActivityInput or NeedInput or AgreementInput or ServiceInput or BookingInput or ChangeInput or IncidentInput or PasswordChangeInput or DeactivationInput or CommandInput or CompanionCommand);
  try{
   switch(input){
    case BranchInput i:if(this is BranchesController branches)return await branches.Invalid(i);break;
    case LandmarkInput i:if(this is LandmarksController landmarks)return await landmarks.Invalid(i);break;
    case DiscoveryCommand i:if(this is DiscoveryController discovery)return await discovery.Invalid(i);break;
    case CompanionCommand i:if(this is TogetherController together)return await together.Invalid(i);break;
    case ShareTripInput i:return V("Journey/Share",await HttpContext.RequestServices.GetRequiredService<JourneyService>().Shares(i.PlanId,i));
    case ServiceReviewInput i:return V("Reviews/Write",await HttpContext.RequestServices.GetRequiredService<ReviewService>().Write(i.BookingId,i));
    case AvailabilityInput i:return V("Availability/Index",await HttpContext.RequestServices.GetRequiredService<AvailabilityService>().Page(i.ServiceId,i.Date.ToString("yyyy-MM"),true,input:i));
    case ReviewReplyInput i:ViewData["ReplyInput"]=i;return V("Reviews/Manage",await HttpContext.RequestServices.GetRequiredService<ReviewService>().Manage(false,status:"",serviceId:i.ServiceId,focusId:i.Id));
    case ReviewModerationInput i:ViewData["ModerationInput"]=i;return V("Reviews/Manage",await HttpContext.RequestServices.GetRequiredService<ReviewService>().Manage(true,status:"",focusId:i.Id));
    case ChecklistInput i:return V("Preparation/Index",await HttpContext.RequestServices.GetRequiredService<ProductService>().Preparation(i.PlanId,i));
    case ExpenseInput i:return V("Expenses/Index",await HttpContext.RequestServices.GetRequiredService<ProductService>().Expenses(i.PlanId,i));
    case SavedInput i:return V("Saved/Index",await HttpContext.RequestServices.GetRequiredService<ProductService>().Saved());
    case LoginInput i:ViewBag.Demo=HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue<bool>("Demo:Enabled");return V("Account/Login",i);
    case RegisterInput i:return V("Account/Register",i);
    case PasswordChangeInput i:return V("Account/Password",i);
    case DeactivationInput i:return V("Account/Deactivate",i);
    case ProfileInput i:ViewBag.Account=await S.Find<TaiKhoan>(S.UserId);if(S.Role==TravelService.ProviderRole)ViewBag.Provider=await S.MyProvider();return V("Account/Profile",i);
    case PlanInput i:return V("Plans/Edit",i);
    case ActivityInput i:ViewBag.Plan=await S.OwnPlan(i.PlanId);return V("Plans/Activity",i);
    case NeedInput i:return V("Companions/Edit",i);
    case AgreementInput i:return V("Companions/Propose",i);
    case ServiceInput i:ViewBag.CanSubmit=await S.CanSubmitService();ViewBag.Places=await S.VisiblePlaces();if(i.Id>0){var serviceHistory=await S.ServiceHistory(i.Id);ViewBag.History=serviceHistory;var liveService=await S.OwnService(i.Id);var lastRevision=serviceHistory.FirstOrDefault();i.PhotoPaths=lastRevision!=null&&new[]{TrangThaiDuyet.CanSua,TrangThaiDuyet.TuChoi}.Contains(lastRevision.TrangThaiDuyet)?lastRevision.AnhJson:liveService.AnhJson;}return V("Provider/Edit",i);
    case BookingInput i:if(i.Id>0){ViewBag.ServiceName=await S.BookingServiceName(i.Id);return V("Bookings/Edit",i);}ViewBag.Service=await S.PublicService(i.ServiceId);return V("Bookings/Create",i);
    case ChangeInput i:return V("Bookings/Change",i);
    case IncidentInput i:ViewBag.Plan=await S.OwnPlan(i.PlanId);ViewBag.Activities=await S.Activities(i.PlanId);return V("Support/Create",i);
    case CommandInput i:return await InvalidCommand(i);
   }
  }catch(BusinessException b){ViewData["Message"]=b.Message;}
  return Failure(ViewData["Message"]?.ToString()??"Vui lòng kiểm tra các thông tin đã nhập: "+string.Join(" · ",ModelState.Values.SelectMany(x=>x.Errors).Select(x=>string.IsNullOrWhiteSpace(x.ErrorMessage)?"Giá trị nhập không hợp lệ":x.ErrorMessage)));
 }
 private async Task<IActionResult> InvalidCommand(CommandInput i){
  var path=Request.Path.Value??"";
  if(this is BranchAdminController&&path=="/quan-tri/co-so/duyet")return V("Branches/Review",await HttpContext.RequestServices.GetRequiredService<BranchService>().ReviewPage(i.Id));
  if(this is BranchesController&&path=="/nha-cung-cap/co-so/hien-thi")return V("Branches/Index",await HttpContext.RequestServices.GetRequiredService<BranchService>().OwnerPage());
  if(Request.HasFormContentType){var form=await Request.ReadFormAsync();ViewData["RejectedForm"]=JsonSerializer.Serialize(form.Where(x=>!x.Key.Contains("password",StringComparison.OrdinalIgnoreCase)&&x.Key!="__RequestVerificationToken").ToDictionary(x=>x.Key,x=>x.Value.ToArray()));}
  if(path.Contains("thoa-thuan",StringComparison.OrdinalIgnoreCase)||path.Contains("AgreementAction",StringComparison.OrdinalIgnoreCase)){var t=await S.Find<ThoaThuanDongHanh>(i.Id);ViewBag.Connection=await S.Connection(t.MaDeNghi);return V("Companions/Agreement",await S.Agreements(t.MaDeNghi));}
  if(path.Contains("dieu-chinh",StringComparison.OrdinalIgnoreCase)||path.Contains("ChangeAction",StringComparison.OrdinalIgnoreCase)){var d=await S.Find<DeNghiDieuChinhDichVu>(i.Id);ViewBag.ServiceName=await S.BookingServiceName(d.MaYeuCau);ViewBag.Changes=await S.Changes(d.MaYeuCau);return V("Bookings/Changes",await S.Booking(d.MaYeuCau));}
  if(path.Contains("ket-qua",StringComparison.OrdinalIgnoreCase)||path.Contains("/Bookings/Result",StringComparison.OrdinalIgnoreCase)){var id=i.Decision=="PhanAnh"?(await S.Find<KetQuaCungCapDichVu>(i.Id)).MaYeuCau:i.Id;ViewBag.ServiceName=await S.BookingServiceName(id);ViewBag.Results=await S.Results(id);ViewBag.Items=await S.BookingItems(id);return V("Bookings/Outcome",await S.Booking(id));}
  if(path.Contains("/Bookings/Respond",StringComparison.OrdinalIgnoreCase)||path is "/yeu-cau/chap-nhan" or "/yeu-cau/phan-hoi-de-xuat"){ViewBag.ServiceName=await S.BookingServiceName(i.Id);return V("Bookings/Detail",await S.Booking(i.Id));}
  if(path.Contains("tinh-huong",StringComparison.OrdinalIgnoreCase)||path.Contains("/Support/Action",StringComparison.OrdinalIgnoreCase)){ViewBag.Options=await S.IncidentOptions(i.Id);return V("Support/Detail",await S.Find<TinhHuongPhatSinh>(i.Id));}
  if(path.Contains("quyen-hoat-dong",StringComparison.OrdinalIgnoreCase)||path.Contains("/Admin/Rights",StringComparison.OrdinalIgnoreCase)){var preview=await S.RightsPreview(i.Id,i.Decision);return V("Admin/Consequences",preview);}
  if(path.Contains("xet-duyet",StringComparison.OrdinalIgnoreCase)||path.Contains("/Admin/Review",StringComparison.OrdinalIgnoreCase))return V("Admin/Review",await S.ReviewDetail(i.Id));
  if(path.Contains("/xac-minh/xac-minh",StringComparison.OrdinalIgnoreCase)||path.Contains("/Admin/Verify",StringComparison.OrdinalIgnoreCase))return V("Admin/Verification",await S.VerificationDetail(i.Id));
  return Failure(string.Join(" · ",ModelState.Values.SelectMany(x=>x.Errors).Select(x=>x.ErrorMessage)));
 }
 protected IActionResult Done(string url,string message="Đã lưu thay đổi thành công."){TempData["Success"]=message;return LocalRedirect(url);}
}

