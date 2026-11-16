// Khi co so duoc tao tu viec "nhan dia diem" duoc duyet, dia diem goc trong Kham pha tu an (neu vi tri khop). Chay mot lan.
const fs = require('fs');
const root = 'src/HoTroDuLich.Web/';
const patch = (file, fn, create) => { const f = root + file; if (create != null) { fs.writeFileSync(f, create); return; } let s = fs.readFileSync(f, 'utf8'); const crlf = s.includes('\r\n'); s = s.replace(/\r\n/g, '\n'); const b = s; s = fn(s); if (s === b) throw Error('Khong doi: ' + file); if (crlf) s = s.replace(/\n/g, '\r\n'); fs.writeFileSync(f, s); };
const rep = (s, a, b) => { if (!s.includes(a)) throw Error('Khong tim thay: ' + a.slice(0, 80)); return s.replace(a, b); };

patch('Database/034_NhanDiaDiem.sql', null, `-- Ghi nhan co so duoc tao tu viec nhan mot dia diem co san trong Kham pha. Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.NhanDiaDiem.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp nhận địa điểm.',1;
IF OBJECT_ID(N'dbo.NhanDiaDiemKhamPha',N'U') IS NULL AND OBJECT_ID(N'dbo.CoSoDichVu',N'U') IS NOT NULL
 EXEC sys.sp_executesql N'CREATE TABLE dbo.NhanDiaDiemKhamPha(
  BranchId bigint NOT NULL CONSTRAINT PK_NhanDiaDiemKhamPha PRIMARY KEY
   CONSTRAINT FK_NhanDiaDiem_Branch FOREIGN KEY REFERENCES dbo.CoSoDichVu(Id) ON DELETE CASCADE,
  EntryId bigint NOT NULL,
  CreatedAt datetime2(7) NOT NULL CONSTRAINT DF_NhanDiaDiem_Created DEFAULT SYSUTCDATETIME(),
  HiddenAt datetime2(7) NULL);';
COMMIT TRANSACTION;
`);
patch('Data/SchemaDatabase.cs', s => rep(s, '"033_KhoiHanh.sql"));', '"033_KhoiHanh.sql"));\n            await ExecuteScript(db, Path.Combine(root, "Database", "034_NhanDiaDiem.sql"));'));
patch('Domain/GuideEntities.cs', s => s + `
/// <summary>Co so duoc tao tu viec nhan mot dia diem trong Kham pha; HiddenAt la luc dia diem goc duoc an sau khi co so duoc duyet.</summary>
public sealed class PlaceClaim
{
 public long BranchId { get; set; }
 public long EntryId { get; set; }
 public DateTime CreatedAt { get; set; }
 public DateTime? HiddenAt { get; set; }
}
`);
patch('Data/Configurations/GuideConfiguration.cs', s => s + `
public sealed class PlaceClaimConfiguration:IEntityTypeConfiguration<PlaceClaim>
{
 public void Configure(EntityTypeBuilder<PlaceClaim> e){e.ToTable("NhanDiaDiemKhamPha");e.HasKey(x=>x.BranchId);e.Property(x=>x.BranchId).ValueGeneratedNever();}
}
`);
patch('Models/BranchModels.cs', s => rep(s, ' public List<IFormFile> Documents { get; set; }=[];\n public string Type { get; set; } = "AnUong";', ' public List<IFormFile> Documents { get; set; }=[];\n /// <summary>Ma muc Kham pha ma nha cung cap dang nhan (chi dung khi tao co so moi).</summary>\n public long? ClaimedEntryId { get; set; }\n public string Type { get; set; } = "AnUong";'));
patch('Views/Branches/Edit.cshtml', s => rep(s, 'data-pv-form>@Html.AntiForgeryToken()', 'data-pv-form>@Html.AntiForgeryToken()@if(i.Id==0&&i.ClaimedEntryId!=null){<input type="hidden" name="ClaimedEntryId" value="@i.ClaimedEntryId"/>}'));

patch('Services/BranchService.cs', s => {
  s = rep(s, 'DonViId=e.DonViId,Latitude=e.Latitude,Longitude=e.Longitude,Type=type,', 'DonViId=e.DonViId,Latitude=e.Latitude,Longitude=e.Longitude,Type=type,ClaimedEntryId=e.Id,');
  s = rep(s, '   if(i.Id==0){b=new(){ProviderId=p.MaNhaCungCap,Status="Draft",CreatedAt=Clock.BayGioUtc};db.Add(b);}', '   var created=i.Id==0;\n   if(i.Id==0){b=new(){ProviderId=p.MaNhaCungCap,Status="Draft",CreatedAt=Clock.BayGioUtc};db.Add(b);}');
  s = rep(s, '   b.UpdatedAt=Clock.BayGioUtc;await db.SaveChangesAsync();savedId=b.Id;\n   if(i.Operation=="Submit")', `   b.UpdatedAt=Clock.BayGioUtc;await db.SaveChangesAsync();savedId=b.Id;
   // Co so moi tao tu viec nhan dia diem: ghi lai muc Kham pha goc de an khi co so duoc duyet.
   if(created&&i.ClaimedEntryId is long claimed&&await db.Set<DiscoveryEntry>().AnyAsync(x=>x.Id==claimed&&x.Status=="Published"&&x.Kind=="Place"&&x.PlaceId!=null&&x.ServiceId==null&&x.BranchId==null))
    db.Add(new PlaceClaim{BranchId=b.Id,EntryId=claimed,CreatedAt=Clock.BayGioUtc});
   if(i.Operation=="Submit")`);
  s = rep(s, '    b.LocationExpiresAt=r.LocationExpiresAt;b.PublishedVersion=r.Number;b.Status="Approved";', `    b.LocationExpiresAt=r.LocationExpiresAt;b.PublishedVersion=r.Number;b.Status="Approved";
    // Co so nhan tu mot dia diem trong Kham pha: an dia diem goc de khong hien trung, chi khi vi tri co so cach dia diem goc khong qua 500 m.
    var claim=await db.Set<PlaceClaim>().FirstOrDefaultAsync(x=>x.BranchId==b.Id&&x.HiddenAt==null);
    if(claim!=null)
    {
     var source=await db.Set<DiscoveryEntry>().FirstOrDefaultAsync(x=>x.Id==claim.EntryId&&x.Kind=="Place"&&x.PlaceId!=null);
     if(source?.Latitude!=null&&b.Latitude!=null&&GoongPlacesService.Haversine((double)source.Latitude,(double)source.Longitude!,(double)b.Latitude,(double)b.Longitude!)<=0.5)
     {
      var place=await db.DiaDiemDuLich.FirstOrDefaultAsync(x=>x.MaDiaDiem==source.PlaceId);
      if(place!=null&&place.DangHienThi){place.DangHienThi=false;claim.HiddenAt=Clock.BayGioUtc;audit.Ghi("AnDiaDiemDaNhan","DiaDiemDuLich",place.MaDiaDiem,"HienThi","An","Cơ sở #"+b.Id+" nhận địa điểm này đã được duyệt");}
     }
    }`);
  return s;
});
console.log('ok');
