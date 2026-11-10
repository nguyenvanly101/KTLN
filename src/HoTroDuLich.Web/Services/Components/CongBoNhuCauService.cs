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
public partial class CongBoNhuCauService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider) : NghiepVuService(context, accessor, environment, configuration, provider), ICongBoNhuCauService
{
    public IQueryable<NhuCauDongHanh> NguoiXemThayDuoc()
    {
        var viewer = CompanionViewer;
        return db.NhuCauDongHanh.Where(n => n.TrangThaiCongBo == HoTroDuLich.Web.Domain.TrangThaiNhuCau.DangCongBo && n.NgayKetThuc >= HomNay && db.TaiKhoan.Any(a => a.MaTaiKhoan == n.MaTaiKhoan && a.TrangThai == HoTroDuLich.Web.Domain.TrangThaiTaiKhoan.HoatDong) && (viewer > 0 || n.PhamViChiaSe == HoTroDuLich.Web.Domain.PhamViChiaSe.CongKhai) && (viewer==0||!db.ChanDongHanh.Any(b=>b.DaChan&&(b.MaNguoiChan==viewer&&b.MaBiChan==n.MaTaiKhoan||b.MaNguoiChan==n.MaTaiKhoan&&b.MaBiChan==viewer))));
    }

    public async Task<List<NhuCauDongHanh>> DanhSachCuaToiAsync() => await db.NhuCauDongHanh.Where(x => x.MaTaiKhoan == UserId).OrderByDescending(x => x.ThoiDiemDang).ThenByDescending(x => x.MaNhuCau).ToListAsync();
    public void KiemTraDuLieu(NeedInput i)
    {
        i.Style = string.IsNullOrWhiteSpace(i.Style) ? null : DanhMuc.ChuanHoa(i.Style);
        Validate(i, "0302-01");
        Ensure(i.Start >= HomNay && i.End >= i.Start && new[] { "CongKhai", "DaDangNhap", "KhiKetNoi" }.Contains(i.Privacy) && (i.Style == null || DanhMuc.HinhThucDi.Contains(i.Style)), "0302-01");
    }

    public Task XemTruocAsync(NeedInput i)
    {
        RoleRequired(Traveler);
        KiemTraDuLieu(i);
        return Task.CompletedTask;
    }

    public async Task CongBoAsync(NeedInput i)
    {
        RoleRequired(Traveler);
        KiemTraDuLieu(i);
        await Execute(async () =>
        {
            NhuCauDongHanh n;
            if (i.Id == 0)
            {
                n = new()
                {
                    MaTaiKhoan = UserId,
                    TrangThaiCongBo = HoTroDuLich.Web.Domain.TrangThaiNhuCau.DangCongBo
                };
                db.Add(n);
            }
            else
            {
                n = await db.NhuCauDongHanh.SingleOrDefaultAsync(x => x.MaNhuCau == i.Id && x.MaTaiKhoan == UserId) ?? throw new BusinessException("Không tìm thấy nhu cầu.", 404);
                CheckVersion(n, i.Version);
            }

            n.DiemDen = i.Destination.Trim();
            n.NoiDung = i.Content.Trim();
            n.NgayBatDau = i.Start;
            n.NgayKetThuc = i.End;
            n.NganSachDuKien = i.Budget;
            n.HinhThucDi = i.Style;
            n.MongMuonDongHanh = i.Wishes;
            n.PhamViChiaSe = System.Enum.Parse<HoTroDuLich.Web.Domain.PhamViChiaSe>(i.Privacy);
        });
    }

    public async Task DungCongBoAsync(CommandInput i)
    {
        RoleRequired(Traveler);
        await Execute(async () =>
        {
            var n = await db.NhuCauDongHanh.SingleOrDefaultAsync(x => x.MaNhuCau == i.Id && x.MaTaiKhoan == UserId) ?? throw new BusinessException("Không tìm thấy nhu cầu.", 404);
            CheckVersion(n, i.Version);
            n.TrangThaiCongBo = HoTroDuLich.Web.Domain.TrangThaiNhuCau.DungCongBo;
        });
    }

    public async Task<NeedInput> MoBieuMauAsync(long id)
    {
        var n = await Find<NhuCauDongHanh>(id);
        Require(n.MaTaiKhoan == UserId, "Không có quyền.", 403);
        return new()
        {
            Id = id,
            Version = Token(n.RowVersion),
            Destination = n.DiemDen,
            Content = n.NoiDung,
            Start = n.NgayBatDau,
            End = n.NgayKetThuc,
            Budget = n.NganSachDuKien,
            Style = n.HinhThucDi,
            Wishes = n.MongMuonDongHanh,
            Privacy = (n.PhamViChiaSe).ToString()
        };
    }
}

