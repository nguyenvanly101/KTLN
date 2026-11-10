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
public partial class XacMinhNhaCungCapService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider, INhatKyService serviceNhatKy, IThongBaoService serviceThongBao) : NghiepVuService(context, accessor, environment, configuration, provider), IXacMinhNhaCungCapService
{
    public async Task<List<HoSoXacMinhNhaCungCap>> HoSoCuaToiAsync()
    {
        var p = await MyProvider();
        return await db.HoSoXacMinhNhaCungCap.Where(x => x.MaNhaCungCap == p.MaNhaCungCap).OrderByDescending(x => x.ThoiDiemNop).ThenByDescending(x=>x.MaHoSo).ToListAsync();
    }

    public async Task NopHoSoMoiAsync(CommandInput i)
    {
        RoleRequired(ProviderRole);
        Require(!string.IsNullOrWhiteSpace(i.Text)&&i.Text.Length<=2000&&!string.IsNullOrWhiteSpace(i.Representative)&&i.Representative.Length<=100&&i.Documents.Count is >=1 and <=5,"Hồ sơ mới cần nội dung đăng ký, người đại diện và 1–5 tài liệu.",400);
        var files = await StoreFiles(i.Documents);
        await Execute(async () =>
        {
            var p = await MyProvider();
            if(!string.IsNullOrEmpty(i.Version))CheckVersion(p,i.Version);
            Ensure(p.TrangThaiXacMinh == HoTroDuLich.Web.Domain.TrangThaiXacMinh.DaThuHoi, "1002-05");
            p.TrangThaiXacMinh = HoTroDuLich.Web.Domain.TrangThaiXacMinh.ChoXacMinh;
            p.TrangThaiQuyenHoatDong = null;
            var h = new HoSoXacMinhNhaCungCap
            {
                MaNhaCungCap = p.MaNhaCungCap,
                NoiDungDangKy = i.Text,
                NguoiDaiDien = i.Representative!,
                TrangThaiXuLy = HoTroDuLich.Web.Domain.TrangThaiHoSoXacMinh.ChoXacMinh
            };
            db.Add(h);
            await db.SaveChangesAsync();
            foreach (var f in files)
                db.Add(new TaiLieuXacMinh { MaHoSo = h.MaHoSo, TenTepGoc = f.Name, DuongDan = f.Path, LoaiTep = System.Enum.Parse<HoTroDuLich.Web.Domain.LoaiTep>(f.Type), KichThuoc = f.Size });
        });
    }

    public async Task NhanBoSungAsync(CommandInput i)
    {
        RoleRequired(ProviderRole);
        Require(i.Text.Length<=2000&&i.Documents.Count is >=1 and <=5,"Cần từ 1 đến 5 tài liệu xác minh hợp lệ.",400);
        var files = await StoreFiles(i.Documents);
        await Execute(async () =>
        {
            var p = await MyProvider();
            var h = await db.HoSoXacMinhNhaCungCap.SingleOrDefaultAsync(x => x.MaHoSo == i.Id && x.MaNhaCungCap == p.MaNhaCungCap) ?? throw new BusinessException("Không tìm thấy hồ sơ.", 404);
            CheckVersion(h, i.Version, "1002-03");
            Ensure(new HoTroDuLich.Web.Domain.TrangThaiHoSoXacMinh[] { HoTroDuLich.Web.Domain.TrangThaiHoSoXacMinh.CanBoSung, HoTroDuLich.Web.Domain.TrangThaiHoSoXacMinh.TuChoi }.Contains(h.TrangThaiXuLy), "1002-05");
            if (h.TrangThaiXuLy == HoTroDuLich.Web.Domain.TrangThaiHoSoXacMinh.TuChoi)
            {
                Require(!string.IsNullOrWhiteSpace(i.Text)&&!string.IsNullOrWhiteSpace(i.Representative),"Hồ sơ mới cần nội dung đăng ký và người đại diện.",400);
                h = new()
                {
                    MaNhaCungCap = p.MaNhaCungCap,
                    NguoiDaiDien = i.Representative!,
                    NoiDungDangKy = i.Text,
                    TrangThaiXuLy = HoTroDuLich.Web.Domain.TrangThaiHoSoXacMinh.ChoXacMinh
                };
                db.Add(h);
                await db.SaveChangesAsync();
            }
            else
            {
                h.SoLanBoSung++;
                h.TrangThaiXuLy = HoTroDuLich.Web.Domain.TrangThaiHoSoXacMinh.ChoXacMinh;
                h.ThoiDiemNop = Clock.BayGioUtc;
            }

            p.TrangThaiXacMinh = HoTroDuLich.Web.Domain.TrangThaiXacMinh.ChoXacMinh;
            p.TrangThaiQuyenHoatDong = null;
            foreach (var f in files)
                db.Add(new TaiLieuXacMinh { MaHoSo = h.MaHoSo, TenTepGoc = f.Name, DuongDan = f.Path, LoaiTep = System.Enum.Parse<HoTroDuLich.Web.Domain.LoaiTep>(f.Type), KichThuoc = f.Size, LanNop = h.SoLanBoSung });
        });
    }

    public async Task<List<HoSoXacMinhNhaCungCap>> LayHangDoiAsync()
    {
        RoleRequired(AdminRole);
        return await db.HoSoXacMinhNhaCungCap.OrderBy(x => x.TrangThaiXuLy == HoTroDuLich.Web.Domain.TrangThaiHoSoXacMinh.ChoXacMinh ? 0 : 1).ThenByDescending(x => x.ThoiDiemNop).ThenByDescending(x => x.MaHoSo).ToListAsync();
    }

    public async Task XacMinhAsync(CommandInput i)
    {
        RoleRequired(AdminRole);
        await Execute(async () =>
        {
            var h = await Resolve<IHoSoXacMinh>().LayHoSo(i.Id);
            CheckVersion(h, i.Version, "1002-03");
            Ensure(h.TrangThaiXuLy == HoTroDuLich.Web.Domain.TrangThaiHoSoXacMinh.ChoXacMinh, "1002-03");
            var p = await Find<NhaCungCapDichVu>(h.MaNhaCungCap);
            Ensure((await Find<TaiKhoan>(p.MaTaiKhoan)).TrangThai == HoTroDuLich.Web.Domain.TrangThaiTaiKhoan.HoatDong, "1002-04");
            Require(new[] { "DaXacMinh", "TuChoi", "CanBoSung" }.Contains(i.Decision), "Quyết định không hợp lệ.", 400);
            if (i.Decision != "DaXacMinh")
                Ensure(!string.IsNullOrWhiteSpace(i.Reason), "1002-02", "Reason");
            Require(i.Text.Length <= 500, "Nhận xét xác minh tối đa 500 ký tự.", 400);
            var turn = (await db.QuyetDinhXacMinh.Where(x => x.MaHoSo == i.Id).MaxAsync(x => (int? )x.LanXuLy) ?? 0) + 1;
            db.Add(new QuyetDinhXacMinh { MaHoSo = i.Id, LanXuLy = turn, KetQua = System.Enum.Parse<HoTroDuLich.Web.Domain.KetQuaXacMinh>(i.Decision), LyDo = string.IsNullOrWhiteSpace(i.Reason) ? null : i.Reason, KetQuaXacMinh = i.Text, MaAdminDuyet = UserId });
            h.TrangThaiXuLy = System.Enum.Parse<HoTroDuLich.Web.Domain.TrangThaiHoSoXacMinh>(i.Decision);
            p.TrangThaiXacMinh = System.Enum.Parse<HoTroDuLich.Web.Domain.TrangThaiXacMinh>(i.Decision);
            p.TrangThaiQuyenHoatDong = i.Decision == "DaXacMinh" ? TrangThaiQuyenHoatDong.DangHoatDong : null;
            serviceNhatKy.Ghi("XacMinhNhaCungCap", "HoSoXacMinhNhaCungCap", h.MaHoSo, "ChoXacMinh", i.Decision, i.Reason);
            serviceThongBao.Tao(p.MaTaiKhoan,"XacMinh"+i.Decision,"Hồ sơ nhà cung cấp: " + Labels.Status(i.Decision),"/nha-cung-cap/ho-so-xac-minh");
        });
    }

    public async Task<(HoSoXacMinhNhaCungCap Profile, NhaCungCapDichVu Provider, List<TaiLieuXacMinh> Files, List<QuyetDinhXacMinh> Decisions)> XemHoSoAsync(long id)
    {
        RoleRequired(AdminRole, ProviderRole);
        var h = await Resolve<IHoSoXacMinh>().LayHoSo(id);
        var p = await Find<NhaCungCapDichVu>(h.MaNhaCungCap);
        Require(Role == AdminRole || p.MaTaiKhoan == UserId, "Không có quyền.", 403);
        return (h, p, await db.TaiLieuXacMinh.Where(x => x.MaHoSo == id).ToListAsync(), await db.QuyetDinhXacMinh.Where(x => x.MaHoSo == id).OrderByDescending(x => x.ThoiDiemDuyet).ThenByDescending(x => x.MaQuyetDinh).ToListAsync());
    }

    public async Task<FilteredPage<AdminVerificationRow>> MoDanhSachAsync(string? q, string status, int page)
    {
        RoleRequired(AdminRole);
        q = SearchText(q);
        ValidFilter(status, "ChoXacMinh", "CanBoSung", "DaXacMinh", "TuChoi");
        var rows =
            from h in db.HoSoXacMinhNhaCungCap.AsNoTracking()join p in db.NhaCungCapDichVu on h.MaNhaCungCap equals p.MaNhaCungCap
            where (status == "" || (h.TrangThaiXuLy).ToString() == status) && (q == "" || p.TenDonVi.Contains(q) || h.NguoiDaiDien.Contains(q))orderby h.ThoiDiemNop descending, h.MaHoSo descending
            select new AdminVerificationRow(h, p.TenDonVi);
        return await PageOf(rows, "/Admin/Verifications", q, status, page);
    }
}


