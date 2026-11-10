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
public partial class ThoaThuanDongHanhService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider, IKetNoiDongHanhService serviceKetNoiDongHanh, IThongBaoService serviceThongBao) : NghiepVuService(context, accessor, environment, configuration, provider), IThoaThuanDongHanhService
{
    public IQueryable<ThoaThuanDongHanh> TruyVanThoaThuan() => db.ThoaThuanDongHanh.Where(t => serviceKetNoiDongHanh.TruyVanKetNoi().Any(c => c.MaDeNghi == t.MaDeNghi));
    public async Task<List<ThoaThuanDongHanh>> LayLichSuAsync(long connection)
    {
        await serviceKetNoiDongHanh.XemDeNghiAsync(connection);
        return await db.ThoaThuanDongHanh.Where(x => x.MaDeNghi == connection).OrderByDescending(x => x.PhienBan).ToListAsync();
    }

    public async Task<AgreementInput> MoTaoThoaThuanAsync(long connection)
    {
        var c = await serviceKetNoiDongHanh.XemDeNghiAsync(connection);
        Ensure(c.TrangThai == HoTroDuLich.Web.Domain.TrangThaiDeNghiKetNoi.DaChapNhan, "0402-01");
        var history = await LayLichSuAsync(connection);
        Ensure(!history.Any(x => x.TrangThai is HoTroDuLich.Web.Domain.TrangThaiThoaThuan.DaNgung or HoTroDuLich.Web.Domain.TrangThaiThoaThuan.MotBenDaRut), "0402-02");
        Ensure(!history.Any(x => x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiThoaThuan.ChoXacNhan && x.LoaiPhienBan == HoTroDuLich.Web.Domain.LoaiPhienBan.DeNghiNgung), "0402-03");
        var current = history.FirstOrDefault(x => x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiThoaThuan.ChoXacNhan) ?? history.FirstOrDefault(x => x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiThoaThuan.DaXacNhan);
        var n = await Find<NhuCauDongHanh>(c.MaNhuCau);
        return new()
        {
            ConnectionId = connection,
            BaseVersion = history.FirstOrDefault()?.PhienBan ?? 0,
            Version = Token(c.RowVersion),
            Destination = current?.NoiDen ?? n.DiemDen,
            Start = current?.NgayDi ?? n.NgayBatDau,
            End = current?.NgayVe ?? n.NgayKetThuc,
            Budget = current?.NganSach,
            Scope = current?.PhamViDiCung
        };
    }

    public async Task GuiDeNghiThoaThuanAsync(AgreementInput i)
    {
        RoleRequired(Traveler);
        if (i.Kind == "NoiDung")
        {
            Validate(i, "0402-04");
            Ensure(i.Start >= HomNay && i.End >= i.Start, "0402-04");
        }

        Require(new[] { "NoiDung", "DeNghiNgung" }.Contains(i.Kind), "Loại phiên bản không hợp lệ.", 400);
        await Execute(async () =>
        {
            var c = await serviceKetNoiDongHanh.XemDeNghiAsync(i.ConnectionId);
            Ensure(c.TrangThai == HoTroDuLich.Web.Domain.TrangThaiDeNghiKetNoi.DaChapNhan, "0402-01");
            await EnsureConnectionActive(c);
            var history = await db.ThoaThuanDongHanh.Where(x => x.MaDeNghi == i.ConnectionId).OrderByDescending(x => x.PhienBan).ToListAsync();
            Ensure(!history.Any(x => x.TrangThai is HoTroDuLich.Web.Domain.TrangThaiThoaThuan.DaNgung or HoTroDuLich.Web.Domain.TrangThaiThoaThuan.MotBenDaRut), "0402-02");
            Ensure((history.FirstOrDefault()?.PhienBan ?? 0) == i.BaseVersion, "0402-06");
            var pending = history.SingleOrDefault(x => x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiThoaThuan.ChoXacNhan);
            var active = history.SingleOrDefault(x => x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiThoaThuan.DaXacNhan);
            if (i.Kind == "DeNghiNgung")
            {
                Ensure(active != null, "0402-10");
                Ensure(pending == null, "0402-11");
                i.Start = active!.NgayDi;
                i.End = active.NgayVe;
                i.Destination = active.NoiDen;
                i.Budget = active.NganSach;
                i.Scope = active.PhamViDiCung;
            }
            else
            {
                Ensure(pending?.LoaiPhienBan != LoaiPhienBan.DeNghiNgung, "0402-03");
                Ensure(active == null || active.NgayDi != i.Start || active.NgayVe != i.End || active.NoiDen != i.Destination.Trim() || active.NganSach != i.Budget || active.PhamViDiCung != i.Scope, "0402-07");
                if (pending != null)
                {
                    pending.TrangThai = TrangThaiThoaThuan.DaThayThe;
                    pending.LyDo = "Đã có đề xuất mới";
                    await db.SaveChangesAsync();
                }
            }

            var t = new ThoaThuanDongHanh
            {
                MaDeNghi = i.ConnectionId,
                PhienBan = i.BaseVersion + 1,
                NgayDi = i.Start,
                NgayVe = i.End,
                NoiDen = i.Destination.Trim(),
                NganSach = i.Budget,
                PhamViDiCung = i.Scope,
                MaNguoiDeXuat = UserId,
                LoaiPhienBan = System.Enum.Parse<HoTroDuLich.Web.Domain.LoaiPhienBan>(i.Kind),
                TrangThai = HoTroDuLich.Web.Domain.TrangThaiThoaThuan.ChoXacNhan
            };
            db.Add(t);
            await db.SaveChangesAsync();
            var n = await Find<NhuCauDongHanh>(c.MaNhuCau);
            serviceThongBao.Tao(UserId == c.MaNguoiGui ? n.MaTaiKhoan : c.MaNguoiGui,i.Kind == "DeNghiNgung" ? "DeNghiCungNgung" : "ThoaThuanChoXacNhan","Có đề xuất đồng hành cần xác nhận.","/dong-hanh/thoa-thuan/xem-de-nghi/" + t.MaThoaThuan);
        });
    }

    public async Task XacNhanAsync(CommandInput i)
    {
        RoleRequired(Traveler);
        await Execute(async () =>
        {
            var t = await Find<ThoaThuanDongHanh>(i.Id);
            var c = await serviceKetNoiDongHanh.XemDeNghiAsync(t.MaDeNghi);
            CheckVersion(t, i.Version, i.Decision == "Rut" ? "0402-10" : "0402-06");
            var n = await Find<NhuCauDongHanh>(c.MaNhuCau);
            var other = UserId == c.MaNguoiGui ? n.MaTaiKhoan : c.MaNguoiGui;
            string type;
            if (i.Decision == "Rut")
            {
                Ensure(t.TrangThai == HoTroDuLich.Web.Domain.TrangThaiThoaThuan.DaXacNhan, "0402-10");
                t.TrangThai = HoTroDuLich.Web.Domain.TrangThaiThoaThuan.MotBenDaRut;
                t.MaNguoiKetThuc = UserId;
                t.ThoiDiemKetThuc = Clock.BayGioUtc;
                t.LyDo = string.IsNullOrWhiteSpace(i.Reason) ? null : i.Reason;
                foreach (var pending in await db.ThoaThuanDongHanh.Where(x => x.MaDeNghi == t.MaDeNghi && x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiThoaThuan.ChoXacNhan).ToListAsync())
                {
                    pending.TrangThai = TrangThaiThoaThuan.DaThayThe;
                    pending.LyDo = "Thỏa thuận đã kết thúc";
                }

                type = "ThoaThuanMotBenRut";
            }
            else
            {
                Ensure(t.MaNguoiDeXuat != UserId, "0402-08");
                Ensure(t.TrangThai == HoTroDuLich.Web.Domain.TrangThaiThoaThuan.ChoXacNhan && i.BusinessVersion == t.PhienBan, "0402-06");
                Require(new[] { "DaXacNhan", "TuChoi" }.Contains(i.Decision), "Quyết định không hợp lệ.", 400);
                t.MaNguoiXacNhan = UserId;
                t.ThoiDiemXacNhan = Clock.BayGioUtc;
                if (i.Decision == "TuChoi")
                {
                    Ensure(!string.IsNullOrWhiteSpace(i.Reason), "0402-09", "Reason");
                    t.TrangThai = HoTroDuLich.Web.Domain.TrangThaiThoaThuan.TuChoi;
                    t.LyDo = i.Reason;
                    type = "ThoaThuanBiTuChoi";
                }
                else
                {
                    await EnsureConnectionActive(c);
                    var old = await db.ThoaThuanDongHanh.SingleOrDefaultAsync(x => x.MaDeNghi == t.MaDeNghi && x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiThoaThuan.DaXacNhan);
                    if (old != null)
                    {
                        old.TrangThai = t.LoaiPhienBan == HoTroDuLich.Web.Domain.LoaiPhienBan.DeNghiNgung ? TrangThaiThoaThuan.DaNgung : TrangThaiThoaThuan.DaThayThe;
                        if (t.LoaiPhienBan == HoTroDuLich.Web.Domain.LoaiPhienBan.DeNghiNgung)
                        {
                            old.ThoiDiemKetThuc = Clock.BayGioUtc;
                            old.MaNguoiKetThuc = UserId;
                        }

                        await db.SaveChangesAsync();
                    }

                    t.TrangThai = System.Enum.Parse<HoTroDuLich.Web.Domain.TrangThaiThoaThuan>(t.LoaiPhienBan == HoTroDuLich.Web.Domain.LoaiPhienBan.DeNghiNgung ? "DaNgung" : "DaXacNhan");
                    if (t.LoaiPhienBan == HoTroDuLich.Web.Domain.LoaiPhienBan.DeNghiNgung)
                    {
                        t.ThoiDiemKetThuc = Clock.BayGioUtc;
                        t.MaNguoiKetThuc = UserId;
                    }

                    type = t.LoaiPhienBan == HoTroDuLich.Web.Domain.LoaiPhienBan.DeNghiNgung ? "ThoaThuanDaNgung" : "ThoaThuanDaXacNhan";
                    serviceThongBao.Tao(UserId,type,"Thỏa thuận: " + Labels.Status((t.TrangThai).ToString()),"/dong-hanh/thoa-thuan/xem-de-nghi/" + t.MaThoaThuan);
                }
            }

            serviceThongBao.Tao(other,type,"Thỏa thuận: " + Labels.Status((t.TrangThai).ToString()),"/dong-hanh/thoa-thuan/xem-de-nghi/" + t.MaThoaThuan);
        });
    }
}
