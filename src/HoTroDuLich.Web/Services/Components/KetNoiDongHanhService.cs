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
public partial class KetNoiDongHanhService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider, ICongBoNhuCauService serviceCongBoNhuCau, IThongBaoService serviceThongBao) : NghiepVuService(context, accessor, environment, configuration, provider), IKetNoiDongHanhService
{
    public async Task MoDeNghiAsync(long id)
    {
        RoleRequired(Traveler);
        var n = await serviceCongBoNhuCau.NguoiXemThayDuoc().SingleOrDefaultAsync(x => x.MaNhuCau == id);
        Ensure(n != null, "0401-01");
        Ensure(n!.MaTaiKhoan != UserId, "0401-02");
        Require(!await db.ChanDongHanh.AnyAsync(x=>x.DaChan&&(x.MaNguoiChan==UserId&&x.MaBiChan==n.MaTaiKhoan||x.MaNguoiChan==n.MaTaiKhoan&&x.MaBiChan==UserId)),"Hai bên đang chặn kết nối đồng hành.",404);
        Ensure(!await db.DeNghiKetNoiDongHanh.AnyAsync(x => x.MaNguoiGui == UserId && x.MaNhuCau == id && x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiDeNghiKetNoi.ChoPhanHoi), "0401-03");
        Ensure(!await db.DeNghiKetNoiDongHanh.AnyAsync(x => x.MaNguoiGui == UserId && x.MaNhuCau == id && x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiDeNghiKetNoi.DaChapNhan), "0401-04");
    }

    public async Task GuiDeNghiAsync(CommandInput i)
    {
        RoleRequired(Traveler);
        Require(i.Text.Length <= 1000, "Lời giới thiệu tối đa 1000 ký tự.", 400);
        await Execute(async () =>
        {
            await MoDeNghiAsync(i.Id);
            var n = await Find<NhuCauDongHanh>(i.Id);
            db.Add(new DeNghiKetNoiDongHanh { MaNhuCau = i.Id, MaNguoiGui = UserId, LoiGioiThieu = string.IsNullOrWhiteSpace(i.Text) ? null : i.Text.Trim(), TrangThai = HoTroDuLich.Web.Domain.TrangThaiDeNghiKetNoi.ChoPhanHoi });
            serviceThongBao.Tao(n.MaTaiKhoan,"DeNghiKetNoiMoi","Bạn nhận được đề nghị kết nối đồng hành.","/dong-hanh/de-nghi");
        });
    }

    public IQueryable<DeNghiKetNoiDongHanh> TruyVanKetNoi() => db.DeNghiKetNoiDongHanh.Where(x => x.MaNguoiGui == UserId || db.NhuCauDongHanh.Any(n => n.MaNhuCau == x.MaNhuCau && n.MaTaiKhoan == UserId));
    public async Task<List<DeNghiKetNoiDongHanh>> DanhSachAsync() => await TruyVanKetNoi().OrderByDescending(x => x.ThoiDiemGui).ThenByDescending(x => x.MaDeNghi).ToListAsync();
    public async Task<DeNghiKetNoiDongHanh> XemDeNghiAsync(long id)
    {
        RoleRequired(Traveler);
        return await TruyVanKetNoi().SingleOrDefaultAsync(x => x.MaDeNghi == id) ?? throw new BusinessException(MaLoi.TatCa["0401-05"].Message, 404, "0401-05");
    }

    public async Task PhanHoiAsync(CommandInput i)
    {
        RoleRequired(Traveler);
        await Execute(async () =>
        {
            var c = await XemDeNghiAsync(i.Id);
            CheckVersion(c, i.Version, "0401-07");
            Ensure(c.TrangThai == HoTroDuLich.Web.Domain.TrangThaiDeNghiKetNoi.ChoPhanHoi, "0401-07");
            var n = await Find<NhuCauDongHanh>(c.MaNhuCau);
            if (i.Decision == "DaRut")
                Ensure(c.MaNguoiGui == UserId, "0401-06");
            else
            {
                Ensure(n.MaTaiKhoan == UserId, "0401-06");
                Require(new[] { "DaChapNhan", "TuChoi" }.Contains(i.Decision), "Quyết định không hợp lệ.", 400);
                Ensure(await db.TaiKhoan.CountAsync(x => (x.MaTaiKhoan == c.MaNguoiGui || x.MaTaiKhoan == n.MaTaiKhoan) && x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiTaiKhoan.HoatDong) == 2, "0401-08");
            }

            c.TrangThai = System.Enum.Parse<HoTroDuLich.Web.Domain.TrangThaiDeNghiKetNoi>(i.Decision);
            c.ThoiDiemPhanHoi = Clock.BayGioUtc;
            serviceThongBao.Tao(UserId == c.MaNguoiGui ? n.MaTaiKhoan : c.MaNguoiGui,i.Decision=="DaChapNhan"?"DeNghiDuocChapNhan":i.Decision=="TuChoi"?"DeNghiBiTuChoi":"DeNghiDaRut","Đề nghị kết nối: " + Labels.Status(i.Decision),"/dong-hanh/de-nghi/xem-de-nghi/" + c.MaDeNghi);
        });
    }

    public async Task<string> LayNoiDenAsync(long id)
    {
        var c = await XemDeNghiAsync(id);
        return (await Find<NhuCauDongHanh>(c.MaNhuCau)).DiemDen;
    }

    public async Task<long> LayChuNhuCauAsync(long id)
    {
        var c = await XemDeNghiAsync(id);
        return (await Find<NhuCauDongHanh>(c.MaNhuCau)).MaTaiKhoan;
    }
}

