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
public partial class NgungTaiKhoanService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider, IDatDichVuService serviceDatDichVu, IKetNoiDongHanhService serviceKetNoiDongHanh, INhatKyService serviceNhatKy, IThoaThuanDongHanhService serviceThoaThuanDongHanh) : NghiepVuService(context, accessor, environment, configuration, provider), INgungTaiKhoanService
{
    public async Task<DeactivationReview> KiemTraDieuKienNgung()
    {
        RoleRequired(Traveler, ProviderRole);
        var obligations = new List<AccountObligation>();
        var hasOpen=await Resolve<IYeuCauDatDichVu>().CoNghiaVuMo(UserId,Enum.Parse<VaiTro>(Role));
        List<YeuCauDatDichVu> bookings = hasOpen?await serviceDatDichVu.TruyVanYeuCau().Where(x => new HoTroDuLich.Web.Domain.TrangThaiYeuCau[] { HoTroDuLich.Web.Domain.TrangThaiYeuCau.ChoPhanHoi, HoTroDuLich.Web.Domain.TrangThaiYeuCau.DeXuatKhac, HoTroDuLich.Web.Domain.TrangThaiYeuCau.DaXacNhan, HoTroDuLich.Web.Domain.TrangThaiYeuCau.ChoKiemTra }.Contains(x.TrangThai)).ToListAsync():[];
        obligations.AddRange(bookings.Select(x => new AccountObligation("Yêu cầu #" + x.MaYeuCau + " · " + Labels.Status(x.TrangThai.ToString()), "/Bookings/Detail/" + x.MaYeuCau)));
        if (Role == Traveler&&await Resolve<IThoaThuanDongHanh>().CoThoaThuanHieuLuc(UserId))
        {
            var agreements = await serviceThoaThuanDongHanh.TruyVanThoaThuan().Where(x => x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiThoaThuan.DaXacNhan).ToListAsync();
            obligations.AddRange(agreements.Select(x => new AccountObligation("Thỏa thuận đồng hành #" + x.MaThoaThuan, "/Companions/Agreement/" + x.MaDeNghi)));
        }
        else if(Role==ProviderRole)
        {
            var changes = await db.DeNghiDieuChinhDichVu.Where(x => new HoTroDuLich.Web.Domain.TrangThaiDeNghiDieuChinh[] { HoTroDuLich.Web.Domain.TrangThaiDeNghiDieuChinh.ChoPhanHoi, HoTroDuLich.Web.Domain.TrangThaiDeNghiDieuChinh.DeXuatKhac }.Contains(x.TrangThai) && serviceDatDichVu.TruyVanYeuCau().Any(y => y.MaYeuCau == x.MaYeuCau)).ToListAsync();
            obligations.AddRange(changes.Select(x => new AccountObligation("Đề nghị điều chỉnh #" + x.MaDeNghi, "/Bookings/Changes/" + x.MaYeuCau)));
        }

        var needs = await db.NhuCauDongHanh.CountAsync(x => x.MaTaiKhoan == UserId && x.TrangThaiCongBo == HoTroDuLich.Web.Domain.TrangThaiNhuCau.DangCongBo);
        if(Role==Traveler)obligations.AddRange(await Resolve<CompanionService>().AccountObligations());
        var connections = await serviceKetNoiDongHanh.TruyVanKetNoi().CountAsync(x => x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiDeNghiKetNoi.ChoPhanHoi);
        var services = Role == ProviderRole ? await db.DichVuDuLich.CountAsync(x => x.TrangThaiTiepNhan == HoTroDuLich.Web.Domain.TrangThaiTiepNhan.TiepNhan && db.NhaCungCapDichVu.Any(p => p.MaNhaCungCap == x.MaNhaCungCap && p.MaTaiKhoan == UserId)) : 0;
        return new(obligations, needs, connections, services, null);
    }

    public async Task<DeactivationReview> XacThucLaiAsync(string password)
    {
        RoleRequired(Traveler, ProviderRole);
        var a = await Find<TaiKhoan>(UserId);
        if (new PasswordHasher<TaiKhoan>().VerifyHashedPassword(a, a.MatKhauBam, password) != PasswordVerificationResult.Failed)
        {
            var review = await KiemTraDieuKienNgung();
            return review with
            {
                ConfirmationToken = review.Obligations.Count == 0 ? DeactivationProtector.Protect(UserId + "|" + a.DauPhien, TimeSpan.FromMinutes(5)) : null
            };
        }

        await serviceNhatKy.GhiRiengAsync("DangNhapThatBai", "TaiKhoan", UserId, null, "ThatBai", "Xác thực lại trước khi ngừng tài khoản thất bại");
        Ensure(false, "1401-01", "Password");
        throw new InvalidOperationException();
    }

    public async Task NgungTaiKhoanAsync(string token)
    {
        RoleRequired(Traveler, ProviderRole);
        string? evidence = null;
        try
        {
            evidence = DeactivationProtector.Unprotect(token);
        }
        catch (CryptographicException)
        {
            Ensure(false, "1401-02");
        }
        catch (ArgumentException)
        {
            Ensure(false, "1401-02");
        }

        await Execute(async () =>
        {
            var a = await Find<TaiKhoan>(UserId);
            Ensure(evidence == UserId + "|" + a.DauPhien, "1401-02");
            var review = await KiemTraDieuKienNgung();
            Ensure(review.Obligations.Count == 0, "1401-03");
            a.TrangThai = HoTroDuLich.Web.Domain.TrangThaiTaiKhoan.DaNgung;
            a.ThoiDiemNgung = Clock.BayGioUtc;
            a.DauPhien = Guid.NewGuid();
            await Resolve<INhuCauDongHanh>().DungCongBoNhuCau(UserId);
            await Resolve<IDeNghiKetNoi>().HetHieuLucDeNghiCho(UserId);
            await Resolve<IThoaThuanDongHanh>().DungDeXuatCho(UserId);
            if (Role == ProviderRole)
            {
                var p = await MyProvider();
                foreach (var s in await db.DichVuDuLich.Where(x => x.MaNhaCungCap == p.MaNhaCungCap && x.TrangThaiTiepNhan == HoTroDuLich.Web.Domain.TrangThaiTiepNhan.TiepNhan).ToListAsync())
                {
                    s.TrangThaiTiepNhan = TrangThaiTiepNhan.KhongTiepNhanMoi;
                    s.MaQuyetDinhKhongTiepNhan = null;
                }
            }

            serviceNhatKy.Ghi("NgungTaiKhoan", "TaiKhoan", UserId, "HoatDong", "DaNgung", $"{review.Needs} nhu cầu; {review.Connections} đề nghị; {review.Services} dịch vụ");
        });
    }
}
