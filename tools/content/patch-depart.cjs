// Khoi hanh / ket thuc chuyen di va bo qua hang muc: migration, entity, service, controller, view. Chay mot lan.
const fs = require('fs');
const root = 'src/HoTroDuLich.Web/';
const patch = (file, fn, create) => {
  const f = root + file;
  if (create) { fs.writeFileSync(f, create); return; }
  let s = fs.readFileSync(f, 'utf8'); const crlf = s.includes('\r\n'); s = s.replace(/\r\n/g, '\n'); const before = s; s = fn(s);
  if (s === before) throw Error('Khong thay doi: ' + file);
  if (crlf) s = s.replace(/\n/g, '\r\n'); fs.writeFileSync(f, s);
};
const rep = (s, a, b) => { if (!s.includes(a)) throw Error('Khong tim thay: ' + a.slice(0, 70)); return s.replace(a, b); };

patch('Database/033_KhoiHanh.sql', null, `-- Khoi hanh va ket thuc chuyen di, danh dau bo qua hang muc. Chay lap an toan.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.KhoiHanh.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;
IF @lock<0 THROW 51000,N'Không lấy được khóa nâng cấp khởi hành.',1;
IF OBJECT_ID(N'dbo.HoatDongLichTrinh',N'U') IS NOT NULL AND COL_LENGTH(N'dbo.HoatDongLichTrinh',N'BoQuaLuc') IS NULL
 EXEC sys.sp_executesql N'ALTER TABLE dbo.HoatDongLichTrinh ADD BoQuaLuc datetime2(7) NULL;';
IF OBJECT_ID(N'dbo.TienDoChuyenDi',N'U') IS NULL AND OBJECT_ID(N'dbo.KeHoachChuyenDi',N'U') IS NOT NULL
 EXEC sys.sp_executesql N'CREATE TABLE dbo.TienDoChuyenDi(
  PlanId bigint NOT NULL CONSTRAINT PK_TienDoChuyenDi PRIMARY KEY
   CONSTRAINT FK_TienDo_Plan FOREIGN KEY REFERENCES dbo.KeHoachChuyenDi(MaKeHoach) ON DELETE CASCADE,
  DepartedAt datetime2(7) NULL,
  FinishedAt datetime2(7) NULL,
  CONSTRAINT CK_TienDo_Order CHECK (FinishedAt IS NULL OR (DepartedAt IS NOT NULL AND FinishedAt>=DepartedAt)));';
COMMIT TRANSACTION;
`);

patch('Data/SchemaDatabase.cs', s => rep(s, '"032_DiemTuyenChuyenDi.sql"));', '"032_DiemTuyenChuyenDi.sql"));\n            await ExecuteScript(db, Path.Combine(root, "Database", "033_KhoiHanh.sql"));'));

patch('Domain/JourneyEntities.cs', s => rep(s, '    public DateTime? HoanThanhLuc { get; set; }\n', '    public DateTime? HoanThanhLuc { get; set; }\n    public DateTime? BoQuaLuc { get; set; }\n'));
patch('Domain/GuideEntities.cs', s => s + `
/// <summary>Tien do cua chuyen di: luc khoi hanh va luc ket thuc (chi chu chuyen di dat).</summary>
public sealed class TripProgress
{
 public long PlanId { get; set; }
 public DateTime? DepartedAt { get; set; }
 public DateTime? FinishedAt { get; set; }
}
`);
patch('Data/Configurations/GuideConfiguration.cs', s => s + `
public sealed class TripProgressConfiguration:IEntityTypeConfiguration<TripProgress>
{
 public void Configure(EntityTypeBuilder<TripProgress> e){e.ToTable("TienDoChuyenDi");e.HasKey(x=>x.PlanId);}
}
`);

patch('Services/Components/LapKeHoachService.cs', s => rep(s, 'if(rescheduled)a.HoanThanhLuc=null;', 'if(rescheduled){a.HoanThanhLuc=null;a.BoQuaLuc=null;}'));

patch('Models/JourneyModels.cs', s => rep(s, 'public sealed record TripDayPage(TripWorkspace Trip,DateOnly Day,List<HoatDongLichTrinh> Items,List<ScheduleWarning> Warnings);', 'public sealed record TripDayPage(TripWorkspace Trip,DateOnly Day,List<HoatDongLichTrinh> Items,List<ScheduleWarning> Warnings,TripProgress? Progress=null,bool IsOwner=false,string? OriginName=null);'));

patch('Services/JourneyService.cs', s => {
  s = rep(s, 'return new(trip,day,trip.Activities.Where(x=>x.Ngay==day).ToList(),Warnings(trip.Activities).Where(x=>trip.Activities.Any(a=>a.MaHoatDong==x.ActivityId&&a.Ngay==day)).ToList());',
    `var progress=await db.Set<TripProgress>().AsNoTracking().FirstOrDefaultAsync(p=>p.PlanId==planId);
        var origin=await db.Set<TripRoutePoints>().AsNoTracking().Where(p=>p.PlanId==planId).Select(p=>p.StartName).FirstOrDefaultAsync();
        return new(trip,day,trip.Activities.Where(x=>x.Ngay==day).ToList(),Warnings(trip.Activities).Where(x=>trip.Activities.Any(a=>a.MaHoatDong==x.ActivityId&&a.Ngay==day)).ToList(),progress,trip.Plan.MaTaiKhoan==travel.UserId,origin);`);
  s = rep(s, 'travel.Require(input.Decision is "Done" or "Undo","Thao tác không hợp lệ.",400);', 'travel.Require(input.Decision is "Done" or "Undo" or "Skip","Thao tác không hợp lệ.",400);');
  s = rep(s, 'activity.HoanThanhLuc=input.Decision=="Done"?clock.BayGioUtc:null;', 'activity.HoanThanhLuc=input.Decision=="Done"?clock.BayGioUtc:null;\n            activity.BoQuaLuc=input.Decision=="Skip"?clock.BayGioUtc:null;');
  s = rep(s, '    public async Task<SharedTrip> Snapshot(long planId,bool budget,bool services)', `    /// <summary>Chu chuyen di bam Khoi hanh: ghi lai luc di. Cho phep tu 1 ngay truoc ngay di den het ngay ket thuc.</summary>
    public async Task Depart(long planId)
    {
        travel.RoleRequired(TravelService.Traveler);
        await travel.Execute(async()=>
        {
            var plan=await travel.OwnPlan(planId);
            travel.Require(clock.HomNay>=plan.NgayBatDau.AddDays(-1)&&clock.HomNay<=plan.NgayKetThuc,"Chỉ khởi hành được từ một ngày trước ngày đi đến hết ngày kết thúc của chuyến.",409);
            var row=await db.Set<TripProgress>().FirstOrDefaultAsync(p=>p.PlanId==planId);
            travel.Require(row?.DepartedAt==null,"Chuyến đi đã khởi hành rồi.",409);
            if(row==null){row=new TripProgress{PlanId=planId};db.Add(row);}
            row.DepartedAt=clock.BayGioUtc;
            travel.Audit("KhoiHanh","KeHoachChuyenDi",planId,null,"Depart");
        });
    }
    /// <summary>Ket thuc chuyen di sau khi da khoi hanh.</summary>
    public async Task Finish(long planId)
    {
        travel.RoleRequired(TravelService.Traveler);
        await travel.Execute(async()=>
        {
            var plan=await travel.OwnPlan(planId);
            var row=await db.Set<TripProgress>().FirstOrDefaultAsync(p=>p.PlanId==planId);
            travel.Require(row?.DepartedAt!=null,"Chuyến đi chưa khởi hành.",409);
            travel.Require(row!.FinishedAt==null,"Chuyến đi đã kết thúc rồi.",409);
            row.FinishedAt=clock.BayGioUtc;
            travel.Audit("KetThucChuyen","KeHoachChuyenDi",planId,null,"Finish");
            await Task.CompletedTask;
        });
    }
    public async Task<SharedTrip> Snapshot(long planId,bool budget,bool services)`);
  return s;
});

patch('Controllers/JourneyControllers.cs', s => rep(s, '    [HttpGet] public async Task<IActionResult> Share(long planId)=>View(await journey.Shares(planId));',
  `    [HttpPost] public async Task<IActionResult> Depart(long planId){await journey.Depart(planId);return Done("/Journey/Today?planId="+planId,"Chúc bạn một chuyến đi vui vẻ. Đã ghi lại giờ khởi hành.");}
    [HttpPost] public async Task<IActionResult> Finish(long planId){await journey.Finish(planId);return Done("/Journey/Today?planId="+planId,"Chuyến đi đã kết thúc. Cảm ơn bạn đã đi cùng.");}
    [HttpGet] public async Task<IActionResult> Share(long planId)=>View(await journey.Shares(planId));`));
console.log('ok');
