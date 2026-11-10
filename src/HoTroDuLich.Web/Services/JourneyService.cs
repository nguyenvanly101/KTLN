using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

public sealed class JourneyService(AppDbContext db,TravelService travel,TripWorkspaceService trips,IDongHo clock)
{
    public static List<ScheduleWarning> Warnings(IEnumerable<HoatDongLichTrinh> activities)
    {
        var result=new List<ScheduleWarning>();
        DateTime? latestEnd=null;
        HoatDongLichTrinh? blocking=null;
        foreach(var activity in activities.OrderBy(x=>x.Ngay).ThenBy(x=>x.ThuTu).ThenBy(x=>x.MaHoatDong))
        {
            if(activity.Gio is not TimeOnly time)continue;
            var start=activity.Ngay.ToDateTime(time);
            if(latestEnd is DateTime end&&start<end.AddMinutes(activity.DiChuyenTruocPhut??0))
                result.Add(new(activity.MaHoatDong,$"{activity.NoiDung}: giờ bắt đầu chưa đủ khoảng trống sau “{blocking!.NoiDung}” và thời gian di chuyển đã nhập."));
            if(activity.ThoiLuongPhut is int duration)
            {
                var finish=start.AddMinutes(duration);
                if(latestEnd==null||finish>latestEnd){latestEnd=finish;blocking=activity;}
            }
        }
        return result;
    }
    public async Task<TripDayPage> Day(long planId,DateOnly? date=null)
    {
        var trip=await trips.Open(planId);var day=date??clock.HomNay;
        travel.Require(day==clock.HomNay||day>=trip.Plan.NgayBatDau&&day<=trip.Plan.NgayKetThuc,"Chọn ngày trong chuyến đi.",400);
        return new(trip,day,trip.Activities.Where(x=>x.Ngay==day).ToList(),Warnings(trip.Activities).Where(x=>trip.Activities.Any(a=>a.MaHoatDong==x.ActivityId&&a.Ngay==day)).ToList());
    }
    public async Task Complete(long planId,CommandInput input)
    {
        travel.RoleRequired(TravelService.Traveler);
        await travel.Execute(async()=>
        {
            var plan=await travel.OwnPlan(planId);travel.CheckVersion(plan,input.Version);
            var activity=await db.HoatDongLichTrinh.SingleOrDefaultAsync(x=>x.MaHoatDong==input.Id&&x.MaKeHoach==planId)??throw new BusinessException("Không tìm thấy hoạt động trong chuyến đi.",404);
            travel.Require(input.Decision is "Done" or "Undo","Thao tác không hợp lệ.",400);
            travel.Require(activity.Ngay<=clock.HomNay,"Chỉ đánh dấu hoạt động của hôm nay hoặc ngày đã qua.");
            activity.HoanThanhLuc=input.Decision=="Done"?clock.BayGioUtc:null;
            // Completion is independent of the FullText draft/confirmed plan state.
            plan.ThoiDiemCapNhat=clock.BayGioUtc;db.Entry(plan).Property(x=>x.ThoiDiemCapNhat).IsModified=true;
            travel.Audit("TienDoHoatDong","HoatDongLichTrinh",activity.MaHoatDong,null,input.Decision);
        });
    }
    public async Task<SharedTrip> Snapshot(long planId,bool budget,bool services)
    {
        var trip=await trips.Open(planId);var p=trip.Plan;
        return new(p.TenKeHoach,p.NoiDen,p.NgayBatDau,p.NgayKetThuc,
            trip.Activities.Select(x=>new SharedActivity(x.Ngay,x.Gio?.ToString("HH:mm"),x.NoiDung,x.ThoiLuongPhut,x.DiChuyenTruocPhut)).ToList(),budget,budget?p.NganSachDuKien:null,services,
            services?trip.Bookings.Select(x=>new SharedBooking(trip.ServiceNames[x.MaDichVu],x.NgayBatDau,x.NgayKetThuc,Labels.Status(x.TrangThai.ToString()))).ToList():[],clock.BayGioUtc);
    }
    public async Task<ShareTripPage> Shares(long planId,ShareTripInput? input=null)
    {
        var p=await travel.OwnPlan(planId);
        return new(p,await db.ChiaSeChuyenDi.Where(x=>x.MaKeHoach==planId).OrderByDescending(x=>x.ThoiDiemTao).ThenByDescending(x=>x.MaChiaSe).ToListAsync(),input??new(){PlanId=planId});
    }
    private static string Hash(string token)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    public async Task<string> CreateShare(ShareTripInput input)
    {
        travel.RoleRequired(TravelService.Traveler);
        travel.Require(input.Days is >=1 and <=30,"Chọn thời hạn từ 1 đến 30 ngày.",400);
        if(!input.AgreeToShare)throw new BusinessException("Xác nhận nội dung trước khi tạo liên kết chia sẻ.",422,field:"AgreeToShare");
        var token=WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        await travel.Execute(async()=>
        {
            var snapshot=await Snapshot(input.PlanId,input.IncludeBudget,input.IncludeServices);
            travel.Require(await db.ChiaSeChuyenDi.CountAsync(x=>x.MaKeHoach==input.PlanId&&!x.DaThuHoi&&x.HanSuDung>clock.BayGioUtc)<20,"Chuyến đi đã có 20 liên kết còn hiệu lực. Thu hồi liên kết cũ trước khi tạo thêm.");
            var json=JsonSerializer.Serialize(snapshot);travel.Require(json.Length<=256000,"Lịch trình quá dài để chia sẻ. Hãy rút gọn trước khi tạo liên kết.");
            var entry=new ChiaSeChuyenDi{MaKeHoach=input.PlanId,TokenHash=Hash(token),SnapshotJson=json,ChiaSeNganSach=input.IncludeBudget,ChiaSeDichVu=input.IncludeServices,ThoiDiemTao=clock.BayGioUtc,HanSuDung=clock.BayGioUtc.AddDays(input.Days)};
            db.Add(entry);await db.SaveChangesAsync();travel.Audit("TaoChiaSeChuyenDi","ChiaSeChuyenDi",entry.MaChiaSe,null,"Tạo bản chia sẻ "+input.Days+" ngày");
        });
        return "/s/"+token;
    }
    public async Task Revoke(long planId,CommandInput input)
    {
        await travel.Execute(async()=>
        {
            await travel.OwnPlan(planId);
            var entry=await db.ChiaSeChuyenDi.SingleOrDefaultAsync(x=>x.MaChiaSe==input.Id&&x.MaKeHoach==planId)??throw new BusinessException("Không tìm thấy liên kết chia sẻ.",404);
            travel.CheckVersion(entry,input.Version);entry.DaThuHoi=true;travel.Audit("ThuHoiChiaSeChuyenDi","ChiaSeChuyenDi",entry.MaChiaSe,null,"DaThuHoi");
        });
    }
    public async Task<SharedTrip> PublicShare(string token)
    {
        if(token.Length!=43||token.Any(c=>!(char.IsAsciiLetterOrDigit(c)||c is '-' or '_')))throw new BusinessException("Liên kết không còn khả dụng.",404);
        var hash=Hash(token);
        var entry=await (from share in db.ChiaSeChuyenDi.AsNoTracking() join plan in db.KeHoachChuyenDi on share.MaKeHoach equals plan.MaKeHoach join account in db.TaiKhoan on plan.MaTaiKhoan equals account.MaTaiKhoan
            where share.TokenHash==hash&&!share.DaThuHoi&&share.HanSuDung>clock.BayGioUtc&&account.TrangThai==TrangThaiTaiKhoan.HoatDong select share).SingleOrDefaultAsync()??throw new BusinessException("Liên kết không còn khả dụng.",404);
        return JsonSerializer.Deserialize<SharedTrip>(entry.SnapshotJson)??throw new BusinessException("Liên kết không còn khả dụng.",404);
    }
}
