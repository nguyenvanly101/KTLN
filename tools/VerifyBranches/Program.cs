using System.Net;
using System.Text;
using System.Text.Json;
using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

var checks=new List<string>();
void Check(bool ok,string label){if(!ok)throw new InvalidOperationException(label);checks.Add(label);}
var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"GoogleMaps:ServerApiKey","unit-server-key"},{"GoogleMaps:BrowserApiKey","unit-browser-key"}}).Build();
var factory=new FakeFactory();var client=new GoogleMapsClient(factory,config,new MemoryCache(new MemoryCacheOptions()),NullLogger<GoogleMapsClient>.Instance);
factory.Payload="""{"id":"ChIJ_test","displayName":{"text":"Cơ sở thử"},"formattedAddress":"Địa chỉ trả trực tiếp","location":{"latitude":16.47,"longitude":107.58},"googleMapsUri":"https://maps.google.com/?q=test","attributions":[{"provider":"Nguồn thử","providerUri":"https://example.com/"}]}""";
var place=await client.Place("ChIJ_test",default);
Check(place.Name=="Cơ sở thử"&&place.Position.Lat==16.47m,"Read Google place coordinates and name");
Check(place.Attributions.Count==1&&place.Attributions[0].Name=="Nguồn thử","Keep Google third-party attribution");
Check(factory.Key=="unit-server-key"&&factory.Fields!.Contains("location"),"Send server key in header with explicit field mask");
var count=factory.Calls;await client.Position("ChIJ_test",default);Check(factory.Calls==count,"Position cache reuses coordinates only");
await client.Place("ChIJ_test",default);Check(factory.Calls==count+1,"Text details are read live instead of cached directory copies");
factory.Payload="""{"suggestions":[{"placePrediction":{"placeId":"ChIJ_gate","structuredFormat":{"mainText":{"text":"Cửa thử"},"secondaryText":{"text":"Địa chỉ cửa"}}}}]}""";
var predictions=await client.Autocomplete("Cửa",Guid.NewGuid().ToString(),default);
Check(predictions.Single().Id=="ChIJ_gate","Autocomplete identifies a concrete Google place");
Check(factory.Body!.Contains("includedRegionCodes")&&factory.Body.Contains("sessionToken"),"Autocomplete carries country and session");
factory.Payload="""{"suggestions":[{"placePrediction":{"placeId":"city","types":["locality","political"],"structuredFormat":{"mainText":{"text":"Thành phố"}}}},{"placePrediction":{"placeId":"gate","types":["point_of_interest"],"structuredFormat":{"mainText":{"text":"Cửa vào"}}}}]}""";
Check((await client.Autocomplete("Cửa",null,default)).Single().Id=="gate","Exact-place autocomplete excludes city/province suggestions");
factory.Payload="""{"places":[{"id":"ChIJ_near","displayName":{"text":"Quán gần"},"location":{"latitude":16.4701,"longitude":107.58}},{"id":"ChIJ_far","location":{"latitude":18,"longitude":108}},{"id":"ChIJ_closed","location":{"latitude":16.47,"longitude":107.58},"businessStatus":"CLOSED_PERMANENTLY"}]}""";
var near=await client.Nearby(new(16.47m,107.58m),2,"AnUong",null,default);
Check(near.Count==1&&near[0].Id=="ChIJ_near","Nearby excludes far and permanently closed places");
Check(factory.Body!.Contains("restaurant")&&factory.Body.Contains("locationRestriction"),"Nearby searches category within circle");
await client.Nearby(new(16.47m,107.58m),2,"AnUong","món chay",default);
Check(factory.Uri!.AbsolutePath.EndsWith("searchText")&&factory.Body!.Contains("textQuery"),"Keyword nearby uses Text Search");
factory.Payload="""{"routes":[{"distanceMeters":1234,"duration":"126.5s","polyline":{"encodedPolyline":"_p~iF~ps|U_ulLnnqC_mqNvxq`@"},"warnings":["Lưu ý tuyến"]}]}""";
var route=await client.Route(new(16.47m,107.58m),new(16.48m,107.59m),"walk",default);
Check(route.DistanceMeters==1234&&route.DurationSeconds==126.5,"Routes reads distance, duration and line");
Check(factory.Body!.Contains("WALK")&&factory.Fields!.Contains("polyline"),"Route request preserves chosen travel mode");
Check(route.Warnings.Count==1,"Route warnings retained");
foreach(var payload in new[]{"{}","{\"routes\":[]}","{\"routes\":[{\"distanceMeters\":1,\"duration\":\"-1s\",\"polyline\":{\"encodedPolyline\":\"x\"}}]}"})
{factory.Payload=payload;try{await client.Route(new(16,107),new(16.1m,107.1m),"drive",default);Check(false,"Malformed route rejected");}catch(BusinessException e){Check(e.Status is 422 or 503,"Malformed route is a recoverable failure");}}
factory.Status=HttpStatusCode.Forbidden;factory.Payload="{\"secret\":\"unit-server-key\"}";
try{await client.Place("ChIJ_denied",default);Check(false,"Google denial rejected");}catch(BusinessException e){Check(e.Status==503&&!e.Message.Contains("unit-server-key"),"Google error does not expose provider payload or key");}
factory.Status=HttpStatusCode.OK;
try{await client.Place("../bad",default);Check(false,"Invalid Place ID rejected");}catch(BusinessException e){Check(e.Status==400,"Place ID cannot change the API path");}
Check(GoogleMapsClient.SafeUrl("javascript:alert(1)")==null&&GoogleMapsClient.SafeUrl("https://user:password@example.com")==null,"External URLs reject script scheme and credentials");
Check(PlaceText.Normalize("CỬA ĐÔNG BẮC")=="cua dong bac","Vietnamese alias normalization");
Check(PlaceSearchService.Distance(new(16,107),new(16,107))<0.00001,"Distance at same coordinate is zero");
Check(PlaceSearchService.Distance(new(0,0),new(0,180))>20000,"Distance calculation is stable for distant points");
var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=NeverConnect;Trusted_Connection=True").Options);
var model=db.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model;
using(var doc=JsonDocument.Parse(await File.ReadAllTextAsync("docs/branch-schema.json")))
foreach(var table in doc.RootElement.EnumerateObject())
{
 var entity=model.GetEntityTypes().Single(x=>x.GetTableName()==table.Name);var store=StoreObjectIdentifier.Table(table.Name,entity.GetSchema());
 Check(!entity.GetProperties().Any(x=>x.IsShadowProperty()),table.Name+" has no shadow columns");
 var expected=table.Value.GetProperty("columns").EnumerateArray().Select(x=>x.GetProperty("name").GetString()).Order().ToArray();
 Check(expected.SequenceEqual(entity.GetProperties().Select(x=>x.GetColumnName(store)).Order()),table.Name+" SQL and EF columns agree");
 Check(entity.GetForeignKeys().All(x=>x.DeleteBehavior==DeleteBehavior.NoAction),table.Name+" does not cascade-delete historical records");
 if(entity.FindProperty("GooglePlaceId") is {} googleId)Check(googleId.GetCollation()=="Latin1_General_100_BIN2",table.Name+" preserves case-sensitive Google Place IDs");
}
var connection=Environment.GetEnvironmentVariable("BRANCH_TEST_CONNECTION");
if(args.Contains("--purge-test"))
{
 if(string.IsNullOrEmpty(connection)||!new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection).InitialCatalog.StartsWith("HoTroDuLich_Branches_",StringComparison.Ordinal))throw new Exception("Purge test requires a dedicated Branches database.");
 await using var testDb=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options);
 var expired=await testDb.Set<CoSoDichVu>().FirstAsync(x=>x.Status=="Draft"&&x.GooglePlaceId!=null&&x.GooglePlaceId.StartsWith("ChIJ_DraftClaim_"));
 var own=await testDb.Set<CoSoDichVu>().AsNoTracking().FirstAsync(x=>x.LocationSource=="Provider"&&x.Latitude!=null);var ownLat=own.Latitude;
 expired.Latitude=16.47m;expired.Longitude=107.58m;expired.LocationExpiresAt=DateTime.UtcNow.AddDays(-1);await testDb.SaveChangesAsync();
 await GoogleLocationCleanup.Purge(testDb,default);testDb.ChangeTracker.Clear();
 var after=await testDb.Set<CoSoDichVu>().SingleAsync(x=>x.Id==expired.Id);
 Check(after.Latitude==null&&after.Longitude==null&&after.GooglePlaceId==expired.GooglePlaceId,"Expired Google coordinates removed while Place ID retained");
 Check((await testDb.Set<CoSoDichVu>().AsNoTracking().SingleAsync(x=>x.Id==own.Id)).Latitude==ownLat,"Cleanup preserves supplier-authored locations");
}
if(args.Contains("--fixtures"))
{
 if(string.IsNullOrEmpty(connection)||!new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection).InitialCatalog.StartsWith("HoTroDuLich_Branches_",StringComparison.Ordinal))throw new Exception("Fixtures require a dedicated Branches test database.");
 await using var fixtureDb=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options);
 var suffix=DateTime.UtcNow.ToString("HHmmssfff");var users=new List<object>();
 foreach(var (label,role) in new[]{("provider",VaiTro.NHA_CUNG_CAP),("other",VaiTro.NHA_CUNG_CAP),("traveler",VaiTro.KHACH_DU_LICH),("admin",VaiTro.QUAN_TRI_VIEN)})
 {
  var account=new TaiKhoan{Email=$"branch-{label}-{suffix}@example.com",VaiTro=role,TrangThai=TrangThaiTaiKhoan.HoatDong};account.MatKhauBam=new PasswordHasher<TaiKhoan>().HashPassword(account,"BranchTest@2026");fixtureDb.Add(account);await fixtureDb.SaveChangesAsync();
  fixtureDb.Add(new HoSoNguoiDung{MaTaiKhoan=account.MaTaiKhoan,TenHienThi="Kiểm thử "+label,ThongTinChiaSe=ThongTinChiaSe.RiengTu});
  long? providerId=null;if(role==VaiTro.NHA_CUNG_CAP){var provider=new NhaCungCapDichVu{MaTaiKhoan=account.MaTaiKhoan,TenDonVi="Đối tác thử "+label+suffix,LoaiHinh="DoanhNghiep",NguoiLienHe="Đối tác thử",DiaChi="Địa chỉ hồ sơ thử",SoDienThoai=label=="provider"?"0939000101":"0939000102",EmailLienHe=account.Email,TrangThaiXacMinh=TrangThaiXacMinh.DaXacMinh,TrangThaiQuyenHoatDong=TrangThaiQuyenHoatDong.DangHoatDong};fixtureDb.Add(provider);await fixtureDb.SaveChangesAsync();providerId=provider.MaNhaCungCap;}
  await fixtureDb.SaveChangesAsync();users.Add(new{label,email=account.Email,id=account.MaTaiKhoan,providerId});
 }
 await File.WriteAllTextAsync("preview/branches-fixture.json",JsonSerializer.Serialize(new{database=new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection).InitialCatalog,users},new JsonSerializerOptions{WriteIndented=true}));
 Check(users.Count==4,"Dedicated test identities created without changing real accounts");
}
await File.WriteAllTextAsync("tests/branches-contract-results.json",JsonSerializer.Serialize(new{passed=true,checks=checks.Count,observations=checks,liveGoogle=false,date=DateTime.UtcNow},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"PASS {checks.Count} contract checks; Google responses are mocked, no live Google key used.");

sealed class FakeFactory:IHttpClientFactory
{
 public string Payload="{}";public HttpStatusCode Status=HttpStatusCode.OK;public int Calls;public string? Key,Fields,Body;public Uri? Uri;
 public HttpClient CreateClient(string name)=>new(new FakeHandler(this));
 sealed class FakeHandler(FakeFactory owner):HttpMessageHandler
 {
  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
  {owner.Calls++;owner.Uri=request.RequestUri;owner.Key=request.Headers.GetValues("X-Goog-Api-Key").Single();owner.Fields=request.Headers.TryGetValues("X-Goog-FieldMask",out var fields)?fields.Single():null;owner.Body=request.Content!=null?await request.Content.ReadAsStringAsync(cancellationToken):null;return new(owner.Status){Content=new StringContent(owner.Payload,Encoding.UTF8,"application/json")};}
 }
}
