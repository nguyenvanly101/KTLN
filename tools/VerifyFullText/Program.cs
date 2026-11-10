using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

var root=Path.GetFullPath(args.FirstOrDefault()??"D:/KLTN");var errors=new List<string>();var checks=0;
var companionExtension=args.Contains("--companion-extension");
var journeyExtension=args.Contains("--journey-extension")||companionExtension;
var productExtension=args.Contains("--product-extension")||journeyExtension;
var tripExtension=args.Contains("--trip-extension")||productExtension;
void Check(bool ok,string message){checks++;if(!ok)errors.Add(message);}
using var contract=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"docs/fulltext-contract.json")));
using var metadata=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"domain-metadata.json")));
var assembly=typeof(TaiKhoan).Assembly;
using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=HoTroDuLich_FullText_20261104;Trusted_Connection=True;TrustServerCertificate=True").Options);
var model=db.GetService<IDesignTimeModel>().Model;var entities=model.GetEntityTypes().ToArray();
Check(entities.Length==(companionExtension?46:journeyExtension?31:productExtension?28:25),"25 entity types");Check(entities.Sum(x=>x.GetProperties().Count())==(companionExtension?473:journeyExtension?335:productExtension?302:tripExtension?268:267),"baseline scalar columns + declared trip extension; no shadow columns");Check(entities.Sum(x=>x.GetForeignKeys().Count())==(companionExtension?83:journeyExtension?47:productExtension?44:tripExtension?39:38),"baseline foreign keys + declared trip extension");Check(entities.All(x=>x.GetForeignKeys().All(f=>f.DeleteBehavior==DeleteBehavior.NoAction)),"NO ACTION foreign keys");Check(entities.Sum(x=>x.GetCheckConstraints().Count())==(companionExtension?161:journeyExtension?132:productExtension?123:117),"declared CHECK constraints");
Check(entities.Sum(x=>x.GetIndexes().Count(i=>i.IsUnique))==(companionExtension?31:journeyExtension?25:productExtension?22:18),"declared unique indexes");Check(entities.Sum(x=>x.GetIndexes().Count(i=>!i.IsUnique))==(companionExtension?50:journeyExtension?40:productExtension?38:tripExtension?35:34),"baseline query indexes + declared trip extension");
if(tripExtension){
 var request=model.FindEntityType(typeof(YeuCauDatDichVu))!;
 var property=request.FindProperty("MaKeHoach");
 Check(property!=null&&property.ClrType==typeof(long?)&&property.IsNullable&&property.GetColumnType()=="bigint"&&!property.IsShadowProperty(),"declared nullable bigint trip association");
 var key=request.GetForeignKeys().SingleOrDefault(f=>f.Properties.Select(p=>p.Name).SequenceEqual(new[]{"MaKeHoach"}));
 Check(key!=null&&key.PrincipalEntityType.ClrType==typeof(KeHoachChuyenDi)&&key.PrincipalKey.Properties.Single().Name=="MaKeHoach"&&key.DeleteBehavior==DeleteBehavior.NoAction,"declared trip association FK");
 var index=request.GetIndexes().SingleOrDefault(i=>i.GetDatabaseName()=="IX_YeuCau_KeHoach");
 Check(index!=null&&index.Properties.Select(p=>p.Name).SequenceEqual(new[]{"MaKeHoach","ThoiDiemGui"})&&!index.IsUnique&&index.IsDescending!.SequenceEqual(new[]{false,true}),"declared trip query index");
}
if(productExtension){
 foreach(var manifest in companionExtension?new[]{"product","journey","companion"}:journeyExtension?new[]{"product","journey"}:new[]{"product"}) {
 using var additions=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"docs/"+manifest+"-extension-schema.json")));
 foreach(var group in new[]{"tables","additionalColumns"})foreach(var table in additions.RootElement.GetProperty(group).EnumerateObject()){
  var entity=entities.SingleOrDefault(x=>x.GetTableName()==table.Name);Check(entity!=null,"declared product entity "+table.Name);if(entity==null)continue;
  foreach(var column in table.Value.EnumerateArray()){
   var name=column.GetProperty("name").GetString()!;var property=entity.FindProperty(name);Check(property!=null&&!property.IsShadowProperty(),table.Name+"."+name+" CLR property");if(property==null)continue;
   Check(property.GetColumnType()?.ToLowerInvariant()==column.GetProperty("sqlType").GetString()!.Replace(" IDENTITY","").ToLowerInvariant(),table.Name+"."+name+" type");
   Check(property.IsNullable==(column.GetProperty("nullable").GetString()=="x"),table.Name+"."+name+" nullability");
  }
 }
}
}
foreach(var table in contract.RootElement.GetProperty("tables").EnumerateObject()){
 var entity=entities.Single(x=>x.GetTableName()==table.Name);
 foreach(var column in table.Value.EnumerateArray()){
  var name=column.GetProperty("name").GetString()!;var property=entity.FindProperty(name);Check(property!=null,table.Name+"."+name+" exists");if(property==null)continue;
  var expected=column.GetProperty("sqlType").GetString()!.Replace(" IDENTITY","").ToLowerInvariant();var actual=property.GetColumnType()?.ToLowerInvariant();Check(expected==actual,$"{table.Name}.{name} type {actual}, expected {expected}");Check(property.IsNullable==(column.GetProperty("nullable").GetString()=="x"),table.Name+"."+name+" nullability");Check(!property.IsShadowProperty(),table.Name+"."+name+" CLR property");
  if(name=="RowVersion")Check(property.IsConcurrencyToken&&property.ValueGenerated==ValueGenerated.OnAddOrUpdate,table.Name+" RowVersion concurrency");
 }
}
foreach(var entity in metadata.RootElement.GetProperty("entities").EnumerateArray()){
 var type=assembly.GetType("HoTroDuLich.Web.Domain."+entity.GetProperty("name").GetString())!;
 foreach(var prop in entity.GetProperty("props").EnumerateArray()){
  var expected=prop.GetProperty("type").GetString()!;var enumName=expected.TrimEnd('?');if(!metadata.RootElement.GetProperty("enums").TryGetProperty(enumName,out _))continue;
  var actual=type.GetProperty(prop.GetProperty("name").GetString()!)!.PropertyType;Check((Nullable.GetUnderlyingType(actual)??actual).Name==enumName&&actual.IsGenericType==expected.EndsWith('?'),type.Name+"."+prop.GetProperty("name").GetString()+" enum type");
 }
}
foreach(var entry in metadata.RootElement.GetProperty("enums").EnumerateObject())Check(Enum.GetNames(assembly.GetType("HoTroDuLich.Web.Domain."+entry.Name)!).SequenceEqual(entry.Value.EnumerateArray().Select(x=>x.GetString()!)),entry.Name+" values");
foreach(var action in contract.RootElement.GetProperty("actions").EnumerateArray()){
 var controller=action.GetProperty("controller").GetString()!;var name=action.GetProperty("action").GetString()!;var type=assembly.GetType("HoTroDuLich.Web.Controllers."+controller);Check(type!=null,controller+" exists");if(type==null)continue;
 var method=type.GetMethods(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly).SingleOrDefault(x=>x.Name==name);Check(method!=null,controller+"."+name+" exists");if(method==null)continue;
 var verb=method.GetCustomAttributes<HttpMethodAttribute>().ToArray();Check(verb.Any(v=>v.HttpMethods.Contains(action.GetProperty("method").GetString()!)&&v.Template==action.GetProperty("route").GetString()),controller+"."+name+" route and verb");
 var policy=action.GetProperty("policy").GetString();Check(policy is "Ẩn danh" or "Công khai"?method.GetCustomAttribute<AllowAnonymousAttribute>()!=null:method.GetCustomAttributes<AuthorizeAttribute>().Any(a=>a.Policy==policy),controller+"."+name+" policy");
 Check(assembly.GetType("HoTroDuLich.Web.Models."+controller.Replace("Controller","ViewModel"))!=null,controller+" request model");
}
var text=File.ReadAllLines(Path.Combine(root,"preview/design-audit/document-extracted.txt")).Single(x=>x.StartsWith("[01840]"));
foreach(Match method in Regex.Matches(text,@"\b(\w+Service) \| (\w+Async) \|")){
 var type=assembly.GetType("HoTroDuLich.Web.Services."+method.Groups[1].Value);Check(type!=null&&type.GetMethods().Any(m=>m.Name==method.Groups[2].Value),method.Groups[1].Value+"."+method.Groups[2].Value);
}
foreach(var name in new[]{"IDiaDiem","IDichVu","IKeHoach","INhuCauDongHanh","IDeNghiKetNoi","IThoaThuanDongHanh","IYeuCauDatDichVu","ITaiKhoan","IHoSoNguoiDung","INhaCungCap","IHoSoXacMinh"})Check(assembly.GetType("HoTroDuLich.Web.Services."+name)?.IsInterface==true,name+" contract");
Check(MaLoi.TatCa.Count==98,"98 business error codes");
foreach(var routeManifest in companionExtension?new[]{"journey","companion"}:journeyExtension?new[]{"journey"}:Array.Empty<string>()){
 using var routes=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"docs/"+routeManifest+"-routes.json")));
 foreach(var route in routes.RootElement.EnumerateArray()){
  var type=assembly.GetType("HoTroDuLich.Web.Controllers."+route.GetProperty("controller").GetString());Check(type!=null,"journey controller "+route.GetProperty("controller").GetString());if(type==null)continue;
  var method=type.GetMethods(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly).SingleOrDefault(m=>m.Name==route.GetProperty("action").GetString()&&m.GetCustomAttributes<HttpMethodAttribute>().Any(a=>a.HttpMethods.Contains(route.GetProperty("method").GetString()!)));Check(method!=null,"journey method and verb "+route.GetProperty("action").GetString());if(method==null)continue;
  Check(method.GetCustomAttributes<HttpMethodAttribute>().Any(a=>a.Template==route.GetProperty("template").GetString()),"journey route template");
  var role=route.GetProperty("role").GetString();Check(role==null?method.GetCustomAttribute<AllowAnonymousAttribute>()!=null:method.GetCustomAttribute<AllowAnonymousAttribute>()==null&&type.GetCustomAttributes<AuthorizeAttribute>().Concat(method.GetCustomAttributes<AuthorizeAttribute>()).Any(a=>a.Roles==role),"journey role "+method.Name);
 }
}
var output=new{passed=errors.Count==0,checks,errors,profile=companionExtension?"FullText + trip + product + journey + companion":journeyExtension?"FullText + trip + product + journey":productExtension?"FullText + trip + product features":tripExtension?"FullText + optional trip association":"FullText baseline",date=DateTime.UtcNow};File.WriteAllText(Path.Combine(root,companionExtension?"tests/companion-contract-results.json":journeyExtension?"tests/journey-contract-results.json":productExtension?"tests/product-contract-results.json":tripExtension?"tests/trip-contract-results.json":"tests/fulltext-contract-results.json"),JsonSerializer.Serialize(output,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine(JsonSerializer.Serialize(output));if(errors.Count>0)Environment.ExitCode=1;
