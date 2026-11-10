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
public partial class DieuChinhDichVuService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider, IDatDichVuService serviceDatDichVu, IThongBaoService serviceThongBao) : NghiepVuService(context, accessor, environment, configuration, provider), IDieuChinhDichVuService
{
    public async Task SuaYeuCauAsync(BookingInput i)
    {
        RoleRequired(Traveler);
        serviceDatDichVu.KiemTraDuLieu(i);
        await Execute(async () =>
        {
            var y = await serviceDatDichVu.ChiTietAsync(i.Id);
            Ensure(y.MaTaiKhoan == UserId && y.TrangThai == HoTroDuLich.Web.Domain.TrangThaiYeuCau.ChoPhanHoi, "0801-02");
            CheckVersion(y, i.Version, "0801-03");
            y.NgayBatDau = i.Start;
            y.NgayKetThuc = i.End;
            y.ThoiGianDenDi = i.ArrivalDeparture;
            y.SoNguoi = i.People;
            y.SoLuong = i.Quantity;
            y.NoiDungLuaChon = i.Selections;
            y.GhiChu = i.Notes;
            y.ChiPhiDuKien = i.Cost;
            y.PhienBan++;
            var s = await Find<DichVuDuLich>(y.MaDichVu);
            var p = await Find<NhaCungCapDichVu>(s.MaNhaCungCap);
            serviceThongBao.Tao(p.MaTaiKhoan,"YeuCauDaSua","Khách vừa sửa yêu cầu #" + y.MaYeuCau,"/nha-cung-cap/yeu-cau/" + y.MaYeuCau);
        });
    }

    public async Task<List<DeNghiDieuChinhDichVu>> LayLichSuAsync(long id)
    {
        await serviceDatDichVu.ChiTietAsync(id);
        return await db.DeNghiDieuChinhDichVu.Where(x => x.MaYeuCau == id).OrderByDescending(x => x.ThoiDiemGui).ThenByDescending(x => x.MaDeNghi).ToListAsync();
    }

    public async Task GuiDeNghiAsync(ChangeInput i)
    {
        RoleRequired(Traveler);
        Ensure(!string.IsNullOrWhiteSpace(i.Reason) && i.Reason.Length <= 500, "0801-05", "Reason");
        Validate(i, "0801-05");
        Require(new[] { "ThayDoi", "Huy" }.Contains(i.Kind), "Loại đề nghị không hợp lệ.", 400);
        await Execute(async () =>
        {
            var y = await serviceDatDichVu.ChiTietAsync(i.Id);
            Ensure(y.MaTaiKhoan == UserId, "0801-01");
            CheckVersion(y, i.Version, "0801-03");
            await CheckChangeable(y);
            Ensure(!await db.DeNghiDieuChinhDichVu.AnyAsync(x => x.MaYeuCau == i.Id && new HoTroDuLich.Web.Domain.TrangThaiDeNghiDieuChinh[] { HoTroDuLich.Web.Domain.TrangThaiDeNghiDieuChinh.ChoPhanHoi, HoTroDuLich.Web.Domain.TrangThaiDeNghiDieuChinh.DeXuatKhac }.Contains(x.TrangThai)), "0801-04");
            if (i.Kind == "ThayDoi")
            {
                ValidateChange(y, i.Start, i.End, i.People, i.Quantity, i.Terms);
                Ensure((i.Start != null && i.Start != y.NgayBatDau) || (i.End != null && i.End != y.NgayKetThuc) || (i.People != null && i.People != y.SoNguoi) || (i.Quantity != null && i.Quantity != y.SoLuong) || (!string.IsNullOrWhiteSpace(i.Terms) && i.Terms != y.DieuKienDeXuat), "0801-05");
            }

            db.Add(new DeNghiDieuChinhDichVu { MaYeuCau = i.Id, LoaiDeNghi = System.Enum.Parse<HoTroDuLich.Web.Domain.LoaiDeNghi>(i.Kind), DieuKienCu = BookingSnapshot(y), NgayBatDauMoi = i.Kind == "ThayDoi" ? i.Start : null, NgayKetThucMoi = i.Kind == "ThayDoi" ? i.End : null, SoNguoiMoi = i.Kind == "ThayDoi" ? i.People : null, SoLuongMoi = i.Kind == "ThayDoi" ? i.Quantity : null, DieuKienMoi = i.Kind == "ThayDoi" ? i.Terms : null, LyDo = i.Reason.Trim(), TrangThai = HoTroDuLich.Web.Domain.TrangThaiDeNghiDieuChinh.ChoPhanHoi });
            var s = await Find<DichVuDuLich>(y.MaDichVu);
            var p = await Find<NhaCungCapDichVu>(s.MaNhaCungCap);
            serviceThongBao.Tao(p.MaTaiKhoan,"DeNghiDieuChinhMoi","Có đề nghị điều chỉnh yêu cầu #" + i.Id,"/Bookings/Changes/" + i.Id);
        });
    }

    public async Task PhanHoiAsync(CommandInput i)
    {
        var customer = new[]
        {
            "DaRut",
            "KhachDongY"
        }.Contains(i.Decision);
        RoleRequired(customer ? Traveler : ProviderRole);
        await Execute(async () =>
        {
            var d = await Find<DeNghiDieuChinhDichVu>(i.Id);
            var y = await serviceDatDichVu.ChiTietAsync(d.MaYeuCau);
            CheckVersion(d, i.Version, "0801-06");
            Ensure(y.TrangThai == HoTroDuLich.Web.Domain.TrangThaiYeuCau.DaXacNhan, "0801-06");
            var s = await Find<DichVuDuLich>(y.MaDichVu);
            var p = await Find<NhaCungCapDichVu>(s.MaNhaCungCap);
            if (customer)
            {
                Ensure(y.MaTaiKhoan == UserId, "0801-01");
                if (i.Decision == "DaRut")
                {
                    Ensure(new HoTroDuLich.Web.Domain.TrangThaiDeNghiDieuChinh[] { HoTroDuLich.Web.Domain.TrangThaiDeNghiDieuChinh.ChoPhanHoi, HoTroDuLich.Web.Domain.TrangThaiDeNghiDieuChinh.DeXuatKhac }.Contains(d.TrangThai), "0801-06");
                    d.TrangThai = HoTroDuLich.Web.Domain.TrangThaiDeNghiDieuChinh.DaRut;
                }
                else
                {
                    Ensure(d.TrangThai == HoTroDuLich.Web.Domain.TrangThaiDeNghiDieuChinh.DeXuatKhac, "0801-06");
                    ApplyChange(y, d);
                    d.TrangThai = HoTroDuLich.Web.Domain.TrangThaiDeNghiDieuChinh.ChapNhan;
                }
            }
            else
            {
                Ensure(p.MaTaiKhoan == UserId, "0801-01");
                Ensure(d.TrangThai == HoTroDuLich.Web.Domain.TrangThaiDeNghiDieuChinh.ChoPhanHoi, "0801-06");
                Require(new[] { "ChapNhan", "TuChoi", "DeXuatKhac" }.Contains(i.Decision), "Quyết định không hợp lệ.", 400);
                if (i.Decision == "TuChoi")
                    Ensure(!string.IsNullOrWhiteSpace(i.Reason), "0801-07", "Reason");
                if (i.Decision == "DeXuatKhac")
                {
                    Ensure(d.LoaiDeNghi == HoTroDuLich.Web.Domain.LoaiDeNghi.ThayDoi && !string.IsNullOrWhiteSpace(i.Text) && i.Text.Length <= 2000, "0801-05");
                    ValidateChange(y, i.Start, i.End, i.People, i.Quantity, i.Text);
                    d.NgayBatDauMoi = i.Start ?? d.NgayBatDauMoi;
                    d.NgayKetThucMoi = i.End ?? d.NgayKetThucMoi;
                    d.SoNguoiMoi = i.People ?? d.SoNguoiMoi;
                    d.SoLuongMoi = i.Quantity ?? d.SoLuongMoi;
                    ValidateChange(y, d.NgayBatDauMoi, d.NgayKetThucMoi, d.SoNguoiMoi, d.SoLuongMoi, i.Text);
                    d.DieuKienDeXuat = i.Text.Trim();
                }

                if (i.Decision == "ChapNhan")
                    ApplyChange(y, d);
                d.TrangThai = System.Enum.Parse<HoTroDuLich.Web.Domain.TrangThaiDeNghiDieuChinh>(i.Decision);
                d.QuyetDinh = string.IsNullOrWhiteSpace(i.Reason) ? null : i.Reason;
            }

            d.ThoiDiemPhanHoi = Clock.BayGioUtc;
            serviceThongBao.Tao(customer ? p.MaTaiKhoan : y.MaTaiKhoan,d.TrangThai==TrangThaiDeNghiDieuChinh.ChapNhan?"DeNghiDieuChinhDuocChapNhan":d.TrangThai==TrangThaiDeNghiDieuChinh.TuChoi?"DeNghiDieuChinhBiTuChoi":d.TrangThai==TrangThaiDeNghiDieuChinh.DeXuatKhac?"DeNghiDieuChinhCoDeXuatKhac":"DeNghiDieuChinhDaRut","Đề nghị điều chỉnh #" + d.MaDeNghi + ": " + Labels.Status((d.TrangThai).ToString()),"/Bookings/Changes/" + y.MaYeuCau);
        });
    }

    public async Task<List<YeuCauDatDichVu>> DanhSachAsync()
    {
        RoleRequired(Traveler, ProviderRole);
        return await serviceDatDichVu.TruyVanYeuCau().Where(y => y.TrangThai == HoTroDuLich.Web.Domain.TrangThaiYeuCau.DaXacNhan || db.DeNghiDieuChinhDichVu.Any(d => d.MaYeuCau == y.MaYeuCau)).OrderByDescending(y => y.ThoiDiemGui).ThenByDescending(y => y.MaYeuCau).ToListAsync();
    }
}

