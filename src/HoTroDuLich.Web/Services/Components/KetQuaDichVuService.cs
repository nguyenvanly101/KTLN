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
public partial class KetQuaDichVuService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider, IDatDichVuService serviceDatDichVu, INhatKyService serviceNhatKy, IThongBaoService serviceThongBao) : NghiepVuService(context, accessor, environment, configuration, provider), IKetQuaDichVuService
{
    public async Task<List<KetQuaCungCapDichVu>> XemKetQuaAsync(long id)
    {
        await serviceDatDichVu.ChiTietAsync(id);
        return await db.KetQuaCungCapDichVu.Where(x => x.MaYeuCau == id).ToListAsync();
    }

    public async Task LuuKetQuaAsync(CommandInput i)
    {
        RoleRequired(ProviderRole);
        await Execute(async () =>
        {
            var y = await serviceDatDichVu.ChiTietAsync(i.Id);
            var s = await Find<DichVuDuLich>(y.MaDichVu);
            var p = await MyProvider();
            Ensure(p.MaNhaCungCap == s.MaNhaCungCap, "0901-01");
            CheckVersion(y, i.Version, "0901-04");
            Ensure(new HoTroDuLich.Web.Domain.TrangThaiYeuCau[] { HoTroDuLich.Web.Domain.TrangThaiYeuCau.DaXacNhan, HoTroDuLich.Web.Domain.TrangThaiYeuCau.ChoKiemTra }.Contains(y.TrangThai), "0901-02");
            Ensure(y.NgayBatDau <= HomNay, "0901-03");
            var items = await serviceDatDichVu.LayHangMucAsync(i.Id);
            var rows = await db.KetQuaCungCapDichVu.Where(x => x.MaYeuCau == i.Id).ToListAsync();
            var settled = rows.Where(x => x.KetQuaThucHien != HoTroDuLich.Web.Domain.KetQuaThucHien.ChuaXacDinh && x.TrangThaiKiemTra != HoTroDuLich.Web.Domain.TrangThaiKiemTra.ChoKiemTra).Select(x => x.HangMuc).ToHashSet();
            Ensure(!i.Items.Any(x => settled.Contains(x.Item)), "0901-06");
            var pending = items.Where(x => !settled.Contains(x)).ToHashSet();
            Ensure(i.Items.Count == pending.Count && i.Items.Select(x => x.Item).Distinct().Count() == i.Items.Count && i.Items.All(x => pending.Contains(x.Item) && new[] { "DayDu", "MotPhan", "KhongThucHien", "KhachKhongDen", "ChuaXacDinh" }.Contains(x.Result) && (x.Notes?.Length ?? 0) <= 1000), "0901-05");
            Ensure(pending.Count > 0, "0901-06");
            foreach (var entry in i.Items)
            {
                var r = rows.SingleOrDefault(x => x.HangMuc == entry.Item);
                if (r == null)
                {
                    r = new()
                    {
                        MaYeuCau = i.Id,
                        HangMuc = entry.Item
                    };
                    db.Add(r);
                }
                else
                    serviceNhatKy.Ghi("DoiChieuKetQua", "KetQuaCungCapDichVu", r.MaKetQua, (r.KetQuaThucHien).ToString(), entry.Result, entry.Notes);
                r.KetQuaThucHien = System.Enum.Parse<HoTroDuLich.Web.Domain.KetQuaThucHien>(entry.Result);
                r.GhiChu = entry.Notes;
                r.TrangThaiKiemTra = System.Enum.Parse<HoTroDuLich.Web.Domain.TrangThaiKiemTra>(entry.Result == "ChuaXacDinh" ? "ChoKiemTra" : r.NoiDungPhanAnh == null ? "DaGhiNhan" : "DaKiemTra");
                r.ThoiDiemKiemTra = r.TrangThaiKiemTra == HoTroDuLich.Web.Domain.TrangThaiKiemTra.DaKiemTra ? Clock.BayGioUtc : null;
            }

            await db.SaveChangesAsync();
            await AggregateResults(y);
            y.PhienBan++;
            serviceThongBao.Tao(y.MaTaiKhoan,"KetQuaDaGhiNhan","Kết quả từng hạng mục dịch vụ đã được ghi nhận.","/yeu-cau/" + i.Id + "/ket-qua/xem-ket-qua");
        });
    }

    public async Task GuiPhanAnhAsync(CommandInput i)
    {
        RoleRequired(i.Decision == "PhanAnh" ? Traveler : ProviderRole);
        await Execute(async () =>
        {
            var r = await Find<KetQuaCungCapDichVu>(i.Id);
            var y = await serviceDatDichVu.ChiTietAsync(r.MaYeuCau);
            CheckVersion(r, i.Version);
            if (i.Decision == "PhanAnh")
            {
                Ensure(y.MaTaiKhoan == UserId, "0901-01");
                Ensure(r.NoiDungPhanAnh == null && Clock.BayGioUtc <= r.ThoiDiemGhi.AddDays(config.GetValue<int>("Business:ComplaintDays", 30)), "0901-07");
                Require(!string.IsNullOrWhiteSpace(i.Text) && i.Text.Length <= 1000, "Vui lòng nhập phản ánh, tối đa 1000 ký tự.", 400);
                r.NoiDungPhanAnh = i.Text.Trim();
                r.ThoiDiemPhanAnh = Clock.BayGioUtc;
                r.TrangThaiKiemTra = HoTroDuLich.Web.Domain.TrangThaiKiemTra.ChoKiemTra;
                r.ThoiDiemKiemTra = null;
                y.TrangThai = HoTroDuLich.Web.Domain.TrangThaiYeuCau.ChoKiemTra;
                y.PhienBan++;
                var s = await Find<DichVuDuLich>(y.MaDichVu);
                var p = await Find<NhaCungCapDichVu>(s.MaNhaCungCap);
                serviceThongBao.Tao(p.MaTaiKhoan,"PhanAnhKetQua","Có phản ánh kết quả dịch vụ.","/yeu-cau/" + y.MaYeuCau + "/ket-qua/mo-ghi-nhan");
            }
            else
            {
                var p = await MyProvider();
                Ensure(await db.DichVuDuLich.AnyAsync(x => x.MaDichVu == y.MaDichVu && x.MaNhaCungCap == p.MaNhaCungCap), "0901-01");
                Ensure(r.TrangThaiKiemTra == HoTroDuLich.Web.Domain.TrangThaiKiemTra.ChoKiemTra, "0901-06");
                Require(new[] { "DayDu", "MotPhan", "KhongThucHien", "KhachKhongDen" }.Contains(i.Decision), "Chọn kết quả sau đối chiếu.", 400);
                serviceNhatKy.Ghi("DoiChieuKetQua", "KetQuaCungCapDichVu", r.MaKetQua, (r.KetQuaThucHien).ToString(), i.Decision, i.Reason);
                r.KetQuaThucHien = System.Enum.Parse<HoTroDuLich.Web.Domain.KetQuaThucHien>(i.Decision);
                r.TrangThaiKiemTra = System.Enum.Parse<HoTroDuLich.Web.Domain.TrangThaiKiemTra>(r.NoiDungPhanAnh == null ? "DaGhiNhan" : "DaKiemTra");
                r.ThoiDiemKiemTra = Clock.BayGioUtc;
                await db.SaveChangesAsync();
                await AggregateResults(y);
                y.PhienBan++;
                serviceThongBao.Tao(y.MaTaiKhoan,"KetQuaDaGhiNhan","Kết quả phản ánh đã được đối chiếu.","/yeu-cau/" + y.MaYeuCau + "/ket-qua/xem-ket-qua");
            }
        });
    }

    public async Task<List<YeuCauDatDichVu>> DanhSachAsync()
    {
        RoleRequired(Traveler, ProviderRole);
        return await serviceDatDichVu.TruyVanYeuCau().Where(y => y.TrangThai == HoTroDuLich.Web.Domain.TrangThaiYeuCau.DaXacNhan || db.KetQuaCungCapDichVu.Any(r => r.MaYeuCau == y.MaYeuCau)).OrderByDescending(y => y.ThoiDiemGui).ThenByDescending(y => y.MaYeuCau).ToListAsync();
    }
}

