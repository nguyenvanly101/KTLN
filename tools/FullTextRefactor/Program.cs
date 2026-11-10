using System.IO.Compression;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var root=Path.GetFullPath(args.FirstOrDefault()??"D:/KLTN");
if(args.Contains("--enums")){EnumMigration.Run(root);return;}
if(args.Contains("--notifications")){NotificationMigration.Run(root);return;}
var serviceRoot=Path.Combine(root,"src/HoTroDuLich.Web/Services");
var filenames=new[]{"TravelService.cs","PlanService.cs","CompanionService.cs","BookingService.cs","AccountService.cs","ProviderService.cs","PageData.cs","FunctionPagesService.cs"};
var map=new Dictionary<string,(string Component,string Name)>();
void Group(string component,string names){foreach(var item in names.Split(' ',StringSplitOptions.RemoveEmptyEntries)){var pair=item.Split(':');map.Add(pair[0],(component,pair.Length>1?pair[1]:pair[0]+"Async"));}}
Group("DanhMuc","Regions:LayDanhMucKhuVuc VisiblePlaces:LayDiaDiemHienThiAsync Place:LayChiTiet");
Group("TimKiemThongTin","Explore:TraCuuAsync ServiceProvider:LayNhaCungCapAsync");
Group("LapKeHoach","Plans:DanhSachAsync OwnPlan:XemKeHoachAsync Activities:LayHoatDong SavePlan:CapNhatThongTinAsync PlanDecision:ChapNhanAsync SaveActivity:ThemHoatDongAsync ReorderActivity:SapXepHoatDongAsync DeleteActivity:XoaHoatDongAsync");
Group("XuLyTinhHuong","Incidents:DanhSachAsync CreateIncident:GhiNhanTinhHuongAsync IncidentAction:GhiKetQuaAsync IncidentOptions:XemTinhHuongAsync");
Group("TimNguoiDongHanh","Needs:TimNhuCauAsync Need:XemChiTietAsync");
Group("CongBoNhuCau","VisibleNeeds:NguoiXemThayDuoc MyNeeds:DanhSachCuaToiAsync ValidateNeed:XemTruocAsync SaveNeed:CongBoAsync StopNeed:DungCongBoAsync OwnNeedInput:MoBieuMauAsync");
Group("KetNoiDongHanh","CheckConnect:MoDeNghiAsync Connect:GuiDeNghiAsync AccessibleConnections:TruyVanKetNoi Connections:DanhSachAsync Connection:XemDeNghiAsync ConnectionAction:PhanHoiAsync NeedDestination:LayNoiDenAsync ConnectionOwner:LayChuNhuCauAsync");
Group("ThoaThuanDongHanh","AccessibleAgreements:TruyVanThoaThuan Agreements:LayLichSuAsync AgreementForm:MoTaoThoaThuanAsync ProposeAgreement:GuiDeNghiThoaThuanAsync AgreementAction:XacNhanAsync");
Group("QuanLyDichVu","CanSubmitService:KiemTraDieuKienDangAsync MyServices:DanhSachAsync OwnService:XemDichVuAsync ServiceHistory:LayLichSuAsync SaveService:GuiDangAsync PublicService:LayChiTiet");
Group("XetDuyetDichVu","ReviewQueue:LayHangDoiAsync ReviewService:DuyetAsync ReviewDetail:XemNoiDungAsync ServiceReviews:MoDanhSachChoDuyetAsync");
Group("DatDichVu","AccessibleBookings:TruyVanYeuCau Bookings:DanhSachCuaToiAsync Booking:ChiTietAsync ValidateBooking:KiemTraDuLieu BookableService:MoDatDichVuAsync CheckBooking:KiemTraTruocGuiAsync Book:GuiYeuCauAsync BookingAction:ChapNhanAsync BookingItems:LayHangMucAsync BookingServiceNames:LayTenDichVuAsync BookingServiceName:LayTenDichVuCuaYeuCauAsync");
Group("DieuChinhDichVu","EditPendingBooking:SuaYeuCauAsync Changes:LayLichSuAsync ChangeBooking:GuiDeNghiAsync ChangeAction:PhanHoiAsync AdjustmentBookings:DanhSachAsync");
Group("KetQuaDichVu","Results:XemKetQuaAsync RecordResult:LuuKetQuaAsync ResultAction:GuiPhanAnhAsync ResultBookings:DanhSachAsync");
Group("DangKy","Register:DangKyAsync DisplayNames:LayTenHienThiAsync SharedProfile:LayThongTinChiaSe ProfileOf:LayHoSo");
Group("XacMinhNhaCungCap","MyVerification:HoSoCuaToiAsync Reapply:NopHoSoMoiAsync Supplement:NhanBoSungAsync VerificationQueue:LayHangDoiAsync VerifyProvider:XacMinhAsync VerificationDetail:XemHoSoAsync Verifications:MoDanhSachAsync");
Group("DangNhap","Authenticate:DangNhapAsync ChangePassword:DoiMatKhauAsync LogLogout:DangXuatAsync");
Group("HoSoNguoiDung","Profile:XemHoSoAsync ProfileForm:MoBieuMauAsync SaveProfile:LuuThayDoiAsync");
Group("QuyenHoatDong","MyDecisions:LayQuyetDinhCuaToiAsync Explain:NhanGiaiTrinhAsync Providers:LayDanhSachAsync RightsPreview:XemHeQuaAsync SetProviderRights:TamDinhChiAsync ReviewExplanation:XemXetGiaiTrinhAsync RightsDetail:XemChiTietAsync ProviderRightsList:MoDanhSachAsync");
Group("NgungTaiKhoan","DeactivationObligations:KiemTraDieuKienNgung Reauthenticate:XacThucLaiAsync Deactivate:NgungTaiKhoanAsync");
Group("ThongBao","Notify:Tao Notifications:DanhSachThongBaoAsync MarkRead:MoThongBaoAsync UnreadCount:DemChuaDocAsync");
Group("NhatKy","Audit:Ghi AuditLog:LayDanhSachAsync AdminOverview:TongQuanAsync");
Group("LuuTep","AuthorizedFile:TaiTepAsync UploadPath:DuongDanAnToan");

var trees=filenames.Select(f=>CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(serviceRoot,f)),path:f)).ToArray();
var members=trees.SelectMany(t=>t.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c=>c.Identifier.Text=="TravelService").SelectMany(c=>c.Members)).ToArray();
var methods=members.OfType<MethodDeclarationSyntax>().ToArray();
var common=new HashSet<string>{"Require","Ensure","Validate","RoleRequired","RequireUseCase","CheckVersion","Find","Dates","Execute","MyProvider","ProviderActive","PublicServices","NormalizePhone","ValidPassword","Token"};
var unassigned=methods.Where(m=>m.Modifiers.Any(SyntaxKind.PublicKeyword)&&!common.Contains(m.Identifier.Text)&&!map.ContainsKey(m.Identifier.Text)).Select(m=>m.Identifier.Text).ToArray();
if(unassigned.Length>0)throw new Exception("Unassigned methods: "+string.Join(",",unassigned));
foreach(var name in map.Keys)if(!methods.Any(m=>m.Identifier.Text==name))throw new Exception("Missing original method "+name);
var backup=Path.Combine(root,"preview/before-component-refactor.zip");
if(!File.Exists(backup)){using var zip=ZipFile.Open(backup,ZipArchiveMode.Create);foreach(var file in filenames)zip.CreateEntryFromFile(Path.Combine(serviceRoot,file),file);}
var usingText="using System.Security.Claims;\nusing System.Data;\nusing System.ComponentModel.DataAnnotations;\nusing HoTroDuLich.Web.Data;\nusing HoTroDuLich.Web.Domain;\nusing HoTroDuLich.Web.Models;\nusing Microsoft.EntityFrameworkCore;\nusing Microsoft.AspNetCore.Identity;\nusing Microsoft.AspNetCore.DataProtection;\nusing Microsoft.Data.SqlClient;\nusing Microsoft.Extensions.Caching.Memory;\nusing System.Text.Json;\nusing System.Security.Cryptography;\nnamespace HoTroDuLich.Web.Services;\n";
string Format(string text)=>CSharpSyntaxTree.ParseText(text).GetRoot().NormalizeWhitespace().ToFullString()+"\n";
var baseMembers=members.Where(m=>m is not MethodDeclarationSyntax method||!map.ContainsKey(method.Identifier.Text)).ToArray();
var baseText=string.Join('\n',baseMembers.Select(m=>new Calls(map,null).Visit(m)!.ToFullString()));
baseText=System.Text.RegularExpressions.Regex.Replace(baseText,@"\bprivate\b","protected");
// Unqualified helper members originally used C#'s default private visibility.
baseText=string.Join('\n',CSharpSyntaxTree.ParseText("class X {"+baseText+"}").GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single().Members.Select(m=>m is MethodDeclarationSyntax method&&!method.Modifiers.Any(SyntaxKind.PublicKeyword)&&!method.Modifiers.Any(SyntaxKind.ProtectedKeyword)?method.WithModifiers(method.Modifiers.Insert(0,SyntaxFactory.Token(SyntaxKind.ProtectedKeyword))).ToFullString():m.ToFullString()));
var baseSource=usingText+"\npublic abstract class NghiepVuService(AppDbContext context,IHttpContextAccessor accessor,IWebHostEnvironment environment,IConfiguration configuration,IServiceProvider provider) {\nprotected readonly AppDbContext db=context; protected readonly IHttpContextAccessor http=accessor; protected readonly IWebHostEnvironment env=environment; protected readonly IConfiguration config=configuration; protected T Resolve<T>() where T:notnull => provider.GetRequiredService<T>();\n"+baseText+"\n}";
File.WriteAllText(Path.Combine(serviceRoot,"NghiepVuService.cs"),Format(baseSource));
var componentDir=Path.Combine(serviceRoot,"Components");var contractDir=Path.Combine(serviceRoot,"Contracts");Directory.CreateDirectory(componentDir);Directory.CreateDirectory(contractDir);
var facade=new List<string>();var registrations=new List<string>();
foreach(var group in map.GroupBy(p=>p.Value.Component)){
 var component=group.Key;var own=methods.Where(m=>map.TryGetValue(m.Identifier.Text,out var entry)&&entry.Component==component).ToArray();
 var visitor=new Calls(map,component);var output=own.Select(m=>(MethodDeclarationSyntax)visitor.Visit(m)!).ToArray();
 var dependencies=visitor.Dependencies.OrderBy(x=>x).ToArray();
 var parameters=string.Join("",dependencies.Select(d=>",I"+d+"Service "+Calls.Field(d)));
 var source=usingText+$"public partial class {component}Service(AppDbContext context,IHttpContextAccessor accessor,IWebHostEnvironment environment,IConfiguration configuration,IServiceProvider provider{parameters}):NghiepVuService(context,accessor,environment,configuration,provider),I{component}Service {{\n"+string.Join('\n',output.Select(m=>m.ToFullString()))+"\n}";
 File.WriteAllText(Path.Combine(componentDir,component+"Service.cs"),Format(source));
 var declarations=output.Select(m=>m.WithBody(null).WithExpressionBody(null).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)).WithModifiers(SyntaxFactory.TokenList(m.Modifiers.Where(t=>!t.IsKind(SyntaxKind.AsyncKeyword)&&!t.IsKind(SyntaxKind.PublicKeyword)))).ToFullString());
 File.WriteAllText(Path.Combine(contractDir,"I"+component+"Service.cs"),Format(usingText+$"public partial interface I{component}Service {{\n"+string.Join('\n',declarations)+"\n}"));
 registrations.Add($"services.AddScoped<I{component}Service,{component}Service>();");
 foreach(var original in own){var target=map[original.Identifier.Text].Name;var argsText=string.Join(',',original.ParameterList.Parameters.Select(p=>p.Identifier.Text));var wrapper=original.WithModifiers(SyntaxFactory.TokenList(original.Modifiers.Where(t=>!t.IsKind(SyntaxKind.AsyncKeyword)))).WithBody(null).WithExpressionBody(SyntaxFactory.ArrowExpressionClause(SyntaxFactory.ParseExpression($"Resolve<I{component}Service>().{target}({argsText})"))).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));facade.Add(wrapper.ToFullString());}
}
File.WriteAllText(Path.Combine(serviceRoot,"TravelService.cs"),Format(usingText+"// Compatibility facade for the original UI; business logic lives in component services.\npublic class TravelService(AppDbContext context,IHttpContextAccessor accessor,IWebHostEnvironment environment,IConfiguration configuration,IServiceProvider provider):NghiepVuService(context,accessor,environment,configuration,provider) {\n"+string.Join('\n',facade)+"\n}"));
var business=trees.SelectMany(t=>t.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()).Single(c=>c.Identifier.Text=="BusinessException");
File.WriteAllText(Path.Combine(serviceRoot,"BusinessException.cs"),Format(usingText+business.ToFullString()));
File.WriteAllText(Path.Combine(serviceRoot,"ComponentRegistration.cs"),Format("namespace HoTroDuLich.Web.Services; public static class ComponentRegistration { public static IServiceCollection AddFullTextComponents(this IServiceCollection services) {"+string.Join('\n',registrations)+"return services;} }"));
foreach(var file in filenames.Where(f=>f!="TravelService.cs"))File.Delete(Path.Combine(serviceRoot,file));
File.WriteAllText(Path.Combine(root,"docs/service-implementation-map.json"),JsonSerializer.Serialize(map.ToDictionary(p=>p.Key,p=>new{component=p.Value.Component,method=p.Value.Name}),new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"Moved {map.Count} methods into {map.Values.Select(x=>x.Component).Distinct().Count()} components; generated interfaces and compatibility facade.");

sealed class Calls(Dictionary<string,(string Component,string Name)> map,string? component):CSharpSyntaxRewriter{
 public HashSet<string> Dependencies{get;}=[];
 public static string Field(string name)=>"service"+name;
 public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node){var result=(MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;if(map.TryGetValue(node.Identifier.Text,out var entry))result=result.WithIdentifier(SyntaxFactory.Identifier(entry.Name));return result;}
 public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node){
  var result=(InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;
  if(node.Expression is IdentifierNameSyntax name&&map.TryGetValue(name.Identifier.Text,out var entry)){
   if(entry.Component==component)return result.WithExpression(SyntaxFactory.IdentifierName(entry.Name));
   if(component!=null)Dependencies.Add(entry.Component);
   var owner=component==null?$"Resolve<I{entry.Component}Service>()":Field(entry.Component);
   return result.WithExpression(SyntaxFactory.ParseExpression(owner+"."+entry.Name));
  }return result;
 }
}
