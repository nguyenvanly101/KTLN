using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

var root=Path.GetFullPath(args.FirstOrDefault()??"D:/KLTN");var errors=new List<string>();int checks=0;
void Check(bool ok,string name){checks++;if(!ok)errors.Add(name);}
using var archive=ZipFile.OpenRead(Path.Combine(root,"preview/before-companion-features-20261105_180318.zip"));
var entry=archive.Entries.Single(x=>x.FullName.Replace('\\','/').EndsWith("docs/fulltext-contract.json"));using var stream=entry.Open();using var contract=JsonDocument.Parse(stream);
using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=HoTroDuLich_Discovery_20261108;Trusted_Connection=True;TrustServerCertificate=True").Options);
var entities=db.GetService<IDesignTimeModel>().Model.GetEntityTypes().ToArray();var assembly=typeof(TaiKhoan).Assembly;
var branchExtension=args.Contains("--branch-extension");var extraTables=0;var extraColumns=0;
if(branchExtension){using var branches=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"docs/branch-schema.json")));extraTables=branches.RootElement.EnumerateObject().Count();extraColumns=branches.RootElement.EnumerateObject().Sum(t=>t.Value.GetProperty("columns").GetArrayLength());}
Check(entities.Length==58+extraTables,"Discovery tables plus requested extensions");Check(entities.Sum(e=>e.GetProperties().Count())==616+extraColumns,"Original and extension scalar columns");Check(entities.All(e=>e.GetForeignKeys().All(f=>f.DeleteBehavior==DeleteBehavior.NoAction)),"Every FK NO ACTION");Check(entities.All(e=>e.GetProperties().All(p=>!p.IsShadowProperty())),"No shadow columns");
using var previous=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"preview/discovery-main-before.json")));
foreach(var t in previous.RootElement.GetProperty("tables").EnumerateArray()){var name=t.GetProperty("name").GetString();var entity=entities.SingleOrDefault(e=>e.GetTableName()==name);Check(entity!=null,name+" retained");if(entity==null)continue;var names=t.GetProperty("Columns").EnumerateArray().Select(c=>c.GetProperty("name").GetString()).OrderBy(x=>x);Check(names.SequenceEqual(entity.GetProperties().Select(p=>p.Name).OrderBy(x=>x)),name+" columns retained");}
foreach(var table in contract.RootElement.GetProperty("tables").EnumerateObject()){var entity=entities.Single(e=>e.GetTableName()==table.Name);foreach(var c in table.Value.EnumerateArray()){var name=c.GetProperty("name").GetString()!;var p=entity.FindProperty(name);Check(p!=null,table.Name+"."+name+" exists");if(p==null)continue;Check(p.GetColumnType()?.ToLowerInvariant()==c.GetProperty("sqlType").GetString()!.Replace(" IDENTITY","").ToLowerInvariant(),table.Name+"."+name+" original SQL type");Check(p.IsNullable==(c.GetProperty("nullable").GetString()=="x"),table.Name+"."+name+" original nullability");}}
var actions=contract.RootElement.GetProperty("actions").EnumerateArray().ToArray();Check(actions.Length==91,"91 original actions in reviewed contract");
foreach(var a in actions){var controller=a.GetProperty("controller").GetString()!;var action=a.GetProperty("action").GetString()!;var type=assembly.GetType("HoTroDuLich.Web.Controllers."+controller);var method=type?.GetMethods(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly).SingleOrDefault(x=>x.Name==action);Check(method!=null,controller+"."+action+" retained");if(method==null)continue;Check(method.GetCustomAttributes<HttpMethodAttribute>().Any(v=>v.HttpMethods.Contains(a.GetProperty("method").GetString()!)&&v.Template==a.GetProperty("route").GetString()),controller+"."+action+" verb/route");var policy=a.GetProperty("policy").GetString();Check(policy is "Ẩn danh" or "Công khai"?method.GetCustomAttribute<AllowAnonymousAttribute>()!=null:method.GetCustomAttributes<AuthorizeAttribute>().Any(x=>x.Policy==policy),controller+"."+action+" original policy");}
using var schema=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"docs/discovery-schema.json")));
foreach(var t in schema.RootElement.GetProperty("tables").EnumerateObject()){var e=entities.Single(x=>x.GetTableName()==t.Name);foreach(var c in t.Value.GetProperty("columns").EnumerateArray()){var p=e.FindProperty(c.GetProperty("name").GetString()!)!;Check(p!=null,t.Name+" property exists");if(p==null)continue;Check(p.GetColumnType()==c.GetProperty("sqlType").GetString(),t.Name+"."+p.Name+" mapped type");Check(p.IsNullable==(c.GetProperty("nullable").GetString()=="x"),t.Name+"."+p.Name+" mapped nullability");if(p.Name=="RowVersion")Check(p.IsConcurrencyToken,t.Name+" concurrency");}}
Check(DiscoveryText.Question("Giá vé Phố cổ Hội An bao nhiêu?").Intent=="Price","Price question intent");Check(!DiscoveryText.Question("Giá vé Phố cổ Hội An bao nhiêu?").Words.Contains("gia"),"Price words separated from entity lookup");Check(DiscoveryText.Question("đi cùng gia đình").Intent==null,"Family not misclassified as price");Check(DiscoveryText.Question("Gia Lai").Intent==null,"Place Gia Lai not misclassified as price");
var friday=new DateOnly(2026,11,6);var hours=new DiscoveryEntry{OpenDays="5",OpenTime=new(22,0),CloseTime=new(2,0)};Check(DiscoveryText.OpenAt(hours,friday,new(23,0)),"Open overnight on start day");Check(DiscoveryText.OpenAt(hours,friday.AddDays(1),new(1,0)),"Overnight previous weekday");Check(!DiscoveryText.OpenAt(hours,friday.AddDays(1),new(12,0)),"Closed after overnight end");Check(!DiscoveryText.OpenAt(new(),friday,new(12,0)),"Unknown hours not treated as open");Check(!DiscoveryText.HttpUrl("javascript:alert(1)"),"Unsafe source scheme rejected");
var parameter=assembly.GetType("HoTroDuLich.Web.Controllers.DiscoveryApiController")!.GetMethod("Route")!.GetParameters()[0];Check(parameter.GetCustomAttribute<FromBodyAttribute>()!=null,"Route JSON body explicitly bound");
var config=new ConfigurationBuilder().Build();using var cache=new MemoryCache(new MemoryCacheOptions());
foreach(var payload in new[]{"{}","{\"code\":\"Ok\",\"routes\":[{}]}"}){var router=new DiscoveryRouter(new FakeFactory(payload),cache,config);try{await router.Compute([new(15,108),new(15.1,108.1)],"car",default);Check(false,"Malformed provider payload rejected");}catch(BusinessException ex){Check(ex.Status==503,"Malformed provider becomes recoverable error");}}
var result=new{passed=errors.Count==0,checks,errors,date=DateTime.UtcNow};File.WriteAllText(Path.Combine(root,branchExtension?"tests/branches-legacy-contract-results.json":"tests/discovery-contract-results.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"{(result.passed?"PASS":"FAIL")} {checks} contract/model checks");foreach(var error in errors)Console.Error.WriteLine(error);Environment.ExitCode=result.passed?0:1;

sealed class FakeFactory(string payload):IHttpClientFactory{public HttpClient CreateClient(string name)=>new(new Handler(payload));sealed class Handler(string payload):HttpMessageHandler{protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)=>Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new StringContent(payload)});}}
