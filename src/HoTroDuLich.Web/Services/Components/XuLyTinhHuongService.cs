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
public partial class XuLyTinhHuongService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider, ILapKeHoachService serviceLapKeHoach) : NghiepVuService(context, accessor, environment, configuration, provider), IXuLyTinhHuongService
{
    public async Task<List<TinhHuongPhatSinh>> DanhSachAsync(long plan)
    {
        await serviceLapKeHoach.XemKeHoachAsync(plan);
        return await db.TinhHuongPhatSinh.Where(x => x.MaKeHoach == plan).OrderByDescending(x => x.ThoiDiemGhiNhan).ThenByDescending(x => x.MaTinhHuong).ToListAsync();
    }

    public async Task GhiNhanTinhHuongAsync(IncidentInput i)
    {
        RoleRequired(Traveler);
        i.Type = DanhMuc.ChuanHoa(i.Type);
        Validate(i, "0501-02");
        Ensure(DanhMuc.LoaiTinhHuong.Contains(i.Type), "0501-02", "Type");
        var ids = i.ActivityIds.Concat(i.ActivityId.HasValue ? [i.ActivityId.Value] : Array.Empty<long>()).Distinct().ToArray();
        await Execute(async () =>
        {
            Ensure(await Resolve<IKeHoach>().LaChuKeHoach(UserId,i.PlanId),"0501-01");
            Ensure(await db.HoatDongLichTrinh.CountAsync(x => ids.Contains(x.MaHoatDong) && x.MaKeHoach == i.PlanId) == ids.Length, "0501-03");
            var t = new TinhHuongPhatSinh
            {
                MaKeHoach = i.PlanId,
                LoaiTinhHuong = i.Type,
                MoTa = i.Description.Trim(),
                ViTri = i.Location,
                TrangThaiXuLy = HoTroDuLich.Web.Domain.TrangThaiXuLyTinhHuong.ChuaGiaiQuyet
            };
            db.Add(t);
            await db.SaveChangesAsync();
            foreach (var id in ids)
                db.Add(new TinhHuongHoatDong { MaTinhHuong = t.MaTinhHuong, MaHoatDong = id });
        });
    }

    public async Task GhiKetQuaAsync(CommandInput i)
    {
        await Execute(async () =>
        {
            var t = await Find<TinhHuongPhatSinh>(i.Id);
            await serviceLapKeHoach.XemKeHoachAsync(t.MaKeHoach);
            CheckVersion(t, i.Version, "0501-05");
            Ensure(!new HoTroDuLich.Web.Domain.TrangThaiXuLyTinhHuong[] { HoTroDuLich.Web.Domain.TrangThaiXuLyTinhHuong.DaGiaiQuyet, HoTroDuLich.Web.Domain.TrangThaiXuLyTinhHuong.DungXuLy }.Contains(t.TrangThaiXuLy), "0501-04");
            if (i.Decision == "PhuongAn")
            {
                Require(!string.IsNullOrWhiteSpace(i.Text) && i.Text.Length <= 2000, "Vui lòng nhập phương án, tối đa 2000 ký tự.", 400);
                foreach (var a in await db.PhuongAnXuLy.Where(x => x.MaTinhHuong == i.Id && x.LaHienHanh).ToListAsync())
                    a.LaHienHanh = false;
                await db.SaveChangesAsync();
                db.Add(new PhuongAnXuLy { MaTinhHuong = i.Id, NoiDung = i.Text.Trim(), LaHienHanh = true });
                t.TrangThaiXuLy = HoTroDuLich.Web.Domain.TrangThaiXuLyTinhHuong.DaChonPhuongAn;
            }
            else
            {
                Require(new[] { "ChuaGiaiQuyet", "GiaiQuyetMotPhan", "DaGiaiQuyet", "DungXuLy" }.Contains(i.Decision), "Kết quả không hợp lệ.", 400);
                if (i.Decision is "GiaiQuyetMotPhan" or "DaGiaiQuyet")
                    Ensure(!string.IsNullOrWhiteSpace(i.Text) && i.Text.Length <= 2000, "0501-06", "Text");
                t.KetQuaXuLy = string.IsNullOrWhiteSpace(i.Text) ? null : i.Text.Trim();
                t.TrangThaiXuLy = System.Enum.Parse<HoTroDuLich.Web.Domain.TrangThaiXuLyTinhHuong>(i.Decision == "ChuaGiaiQuyet" && await db.PhuongAnXuLy.AnyAsync(x => x.MaTinhHuong == i.Id && x.LaHienHanh) ? "DaChonPhuongAn" : i.Decision);
            }

            t.ThoiDiemCapNhat = Clock.BayGioUtc;
        });
    }

    public async Task<List<PhuongAnXuLy>> XemTinhHuongAsync(long id)
    {
        var t = await Find<TinhHuongPhatSinh>(id);
        await serviceLapKeHoach.XemKeHoachAsync(t.MaKeHoach);
        return await db.PhuongAnXuLy.Where(x => x.MaTinhHuong == id).OrderByDescending(x => x.ThoiDiemChon).ThenByDescending(x => x.MaPhuongAn).ToListAsync();
    }
}

