using System.Security.Claims;
using System.Data;
using System.ComponentModel.DataAnnotations;
using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using System.Security.Cryptography;

namespace HoTroDuLich.Web.Services;
public partial class LapKeHoachService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider) : NghiepVuService(context, accessor, environment, configuration, provider), ILapKeHoachService
{
    public async Task<List<KeHoachChuyenDi>> DanhSachAsync() => await db.KeHoachChuyenDi.Where(x => x.MaTaiKhoan == UserId).OrderByDescending(x => x.NgayBatDau).ThenByDescending(x => x.MaKeHoach).ToListAsync();
    public async Task<KeHoachChuyenDi> XemKeHoachAsync(long id)
    {
        RoleRequired(Traveler);
        return await db.KeHoachChuyenDi.SingleOrDefaultAsync(x => x.MaKeHoach == id && x.MaTaiKhoan == UserId) ?? throw new BusinessException(MaLoi.TatCa["0201-02"].Message, 404, "0201-02");
    }

    public async Task<List<HoatDongLichTrinh>> LayHoatDong(long id)
    {
        await XemKeHoachAsync(id);
        return await db.HoatDongLichTrinh.Where(x => x.MaKeHoach == id).OrderBy(x => x.Ngay).ThenBy(x => x.ThuTu).ThenBy(x => x.MaHoatDong).ToListAsync();
    }

    public async Task<long> CapNhatThongTinAsync(PlanInput i)
    {
        RoleRequired(Traveler);
        Validate(i, "0201-01");
        if (i.Id == 0) await Resolve<TripWorkspaceService>().ValidateSelection(i.ContinueServiceId, i.ContinuePlaceId);
        Dates(i.Start, i.End);
        long id = 0;
        await Execute(async () =>
        {
            KeHoachChuyenDi p;
            if (i.Id == 0)
            {
                Ensure(i.Start >= HomNay, "0201-01", "Start");
                p = new()
                {
                    MaTaiKhoan = UserId,
                    TrangThai = HoTroDuLich.Web.Domain.TrangThaiKeHoach.Nhap
                };
                db.Add(p);
            }
            else
            {
                p = await XemKeHoachAsync(i.Id);
                CheckVersion(p, i.Version, "0201-05");
                Ensure(i.Start >= HomNay || i.Start == p.NgayBatDau, "0201-01", "Start");
                Ensure(!await db.HoatDongLichTrinh.AnyAsync(x => x.MaKeHoach == i.Id && (x.Ngay < i.Start || x.Ngay > i.End)), "0201-07");
                p.TrangThai = HoTroDuLich.Web.Domain.TrangThaiKeHoach.Nhap;
            }

            p.TenKeHoach = i.Name.Trim();
            p.NoiDen = i.Destination.Trim();
            p.NgayBatDau = i.Start;
            p.NgayKetThuc = i.End;
            p.NganSachDuKien = i.Budget;
            p.ThoiDiemCapNhat = Clock.BayGioUtc;
            await db.SaveChangesAsync();
            id = p.MaKeHoach;
        });
        return id;
    }

    public async Task ChapNhanAsync(CommandInput i)
    {
        RoleRequired(Traveler);
        await Execute(async () =>
        {
            var p = await XemKeHoachAsync(i.Id);
            CheckVersion(p, i.Version, "0201-05");
            Require(new[] { "Nhap", "DaChapNhan" }.Contains(i.Decision), "Trạng thái kế hoạch không hợp lệ.", 400);
            p.TrangThai = System.Enum.Parse<HoTroDuLich.Web.Domain.TrangThaiKeHoach>(i.Decision);
            p.ThoiDiemCapNhat = Clock.BayGioUtc;
        });
    }

    public async Task ThemHoatDongAsync(ActivityInput i)
    {
        RoleRequired(Traveler);
        Validate(i, "0201-01");
        await Execute(async () =>
        {
            var p = await XemKeHoachAsync(i.PlanId);
            CheckVersion(p, i.Version, "0201-05");
            Ensure(i.Date >= p.NgayBatDau && i.Date <= p.NgayKetThuc, "0201-03", "Date");
            Require(i.PlaceId == null || i.ServiceId == null, "Chỉ chọn một địa điểm hoặc dịch vụ.", 400);
            if (i.ServiceId != null)
                Ensure(await PublicServices().AnyAsync(x => x.MaDichVu == i.ServiceId), "0201-04");
            if (i.PlaceId != null)
                Ensure(await db.DiaDiemDuLich.AnyAsync(x => x.MaDiaDiem == i.PlaceId && x.DangHienThi), "0201-04");
            HoatDongLichTrinh a;
            if (i.Id == 0)
            {
                a = new()
                {
                    MaKeHoach = i.PlanId
                };
                db.Add(a);
            }
            else
                a = await db.HoatDongLichTrinh.SingleOrDefaultAsync(x => x.MaHoatDong == i.Id && x.MaKeHoach == i.PlanId) ?? throw new BusinessException("Không tìm thấy hoạt động.", 404);
            var rescheduled=i.Id!=0&&(a.Ngay!=i.Date||a.Gio!=i.Time||a.NoiDung!=i.Content.Trim()||a.MaDichVu!=i.ServiceId||a.MaDiaDiem!=i.PlaceId);
            if (i.Id == 0 || a.Ngay != i.Date)
                a.ThuTu = (await db.HoatDongLichTrinh.Where(x => x.MaKeHoach == i.PlanId && x.Ngay == i.Date).MaxAsync(x => (int? )x.ThuTu) ?? 0) + 1;
            a.Ngay = i.Date;
            a.Gio = i.Time;
            a.NoiDung = i.Content.Trim();
            a.ChiPhiDuKien = i.Cost;
            a.MaDichVu = i.ServiceId;
            a.MaDiaDiem = i.PlaceId;
            if(i.UpdateTiming||i.DurationMinutes!=null||i.TravelMinutes!=null){a.ThoiLuongPhut=i.DurationMinutes;a.DiChuyenTruocPhut=i.TravelMinutes;}
            if(rescheduled)a.HoanThanhLuc=null;
            p.TrangThai = HoTroDuLich.Web.Domain.TrangThaiKeHoach.Nhap;
            p.ThoiDiemCapNhat = Clock.BayGioUtc;
        });
    }

    public async Task SapXepHoatDongAsync(CommandInput i)
    {
        await Execute(async () =>
        {
            var a = await Find<HoatDongLichTrinh>(i.Id);
            var p = await XemKeHoachAsync(a.MaKeHoach);
            CheckVersion(p, i.Version, "0201-05");
            var items = await db.HoatDongLichTrinh.Where(x => x.MaKeHoach == p.MaKeHoach && x.Ngay == a.Ngay).OrderBy(x => x.ThuTu).ThenBy(x => x.MaHoatDong).ToListAsync();
            var index = items.IndexOf(a);
            var target = i.Decision == "Len" ? index - 1 : index + 1;
            Require(target >= 0 && target < items.Count, "Hoạt động đã ở đầu hoặc cuối ngày.", 422);
            (items[index], items[target]) = (items[target], items[index]);
            for (var n = 0; n < items.Count; n++)
                items[n].ThuTu = n + 1;
            p.TrangThai = HoTroDuLich.Web.Domain.TrangThaiKeHoach.Nhap;
            p.ThoiDiemCapNhat = Clock.BayGioUtc;
        });
    }

    public async Task XoaHoatDongAsync(CommandInput i)
    {
        await Execute(async () =>
        {
            var a = await Find<HoatDongLichTrinh>(i.Id);
            var p = await XemKeHoachAsync(a.MaKeHoach);
            CheckVersion(p, i.Version, "0201-05");
            Ensure(!await db.TinhHuongHoatDong.AnyAsync(x => x.MaHoatDong == i.Id), "0201-06");
            db.Remove(a);
            p.TrangThai = HoTroDuLich.Web.Domain.TrangThaiKeHoach.Nhap;
            p.ThoiDiemCapNhat = Clock.BayGioUtc;
        });
    }
}
