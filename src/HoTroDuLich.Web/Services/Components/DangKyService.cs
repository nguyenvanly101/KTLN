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
public partial class DangKyService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider, INhatKyService serviceNhatKy) : NghiepVuService(context, accessor, environment, configuration, provider), IDangKyService
{
    public async Task DangKyAsync(RegisterInput i)
    {
        Ensure(new[] { Traveler, ProviderRole }.Contains(i.Role), "1001-01");
        i.Phone = NormalizePhone(i.Phone);
        i.ContactPhone = NormalizePhone(i.ContactPhone);
        Validate(i, "1001-02");
        var email = i.Email.Trim().ToLowerInvariant();
        Ensure(ValidPassword(i.Password, email) && i.Password == i.ConfirmPassword, "1001-03", "Password");
        Ensure(!await db.TaiKhoan.AnyAsync(x => x.Email == email || (i.Phone != null && x.SoDienThoai == i.Phone)), "1001-04");
        var files = await StoreFiles(i.Role == ProviderRole ? i.Documents : []);
        await Execute(async () =>
        {
            Ensure(!await db.TaiKhoan.AnyAsync(x => x.Email == email || (i.Phone != null && x.SoDienThoai == i.Phone)), "1001-04");
            var a = new TaiKhoan
            {
                Email = email,
                SoDienThoai = i.Phone,
                VaiTro = System.Enum.Parse<HoTroDuLich.Web.Domain.VaiTro>(i.Role),
                TrangThai = HoTroDuLich.Web.Domain.TrangThaiTaiKhoan.HoatDong
            };
            a.MatKhauBam = new PasswordHasher<TaiKhoan>().HashPassword(a, i.Password);
            db.Add(a);
            await db.SaveChangesAsync();
            db.Add(new HoSoNguoiDung { MaTaiKhoan = a.MaTaiKhoan, TenHienThi = i.Name.Trim(), ThongTinChiaSe = HoTroDuLich.Web.Domain.ThongTinChiaSe.RiengTu });
            if (i.Role == ProviderRole)
            {
                var p = new NhaCungCapDichVu
                {
                    MaTaiKhoan = a.MaTaiKhoan,
                    TenDonVi = i.Organization!.Trim(),
                    DiaChi = i.Address!.Trim(),
                    SoDienThoai = i.ContactPhone!,
                    EmailLienHe = i.ContactEmail!.Trim().ToLowerInvariant(),
                    NguoiLienHe = i.ContactName!.Trim(),
                    LoaiHinh = i.OrganizationType!.Trim(),
                    TrangThaiXacMinh = HoTroDuLich.Web.Domain.TrangThaiXacMinh.ChoXacMinh
                };
                db.Add(p);
                await db.SaveChangesAsync();
                var h = new HoSoXacMinhNhaCungCap
                {
                    MaNhaCungCap = p.MaNhaCungCap,
                    NguoiDaiDien = i.Representative!.Trim(),
                    NoiDungDangKy = i.RegistrationDescription!.Trim(),
                    TrangThaiXuLy = HoTroDuLich.Web.Domain.TrangThaiHoSoXacMinh.ChoXacMinh
                };
                db.Add(h);
                await db.SaveChangesAsync();
                foreach (var f in files)
                    db.Add(new TaiLieuXacMinh { MaHoSo = h.MaHoSo, TenTepGoc = f.Name, DuongDan = f.Path, LoaiTep = System.Enum.Parse<HoTroDuLich.Web.Domain.LoaiTep>(f.Type), KichThuoc = f.Size, LanNop = 0 });
            }

            serviceNhatKy.Ghi("DangKyTaiKhoan", "TaiKhoan", a.MaTaiKhoan, null, i.Role);
        });
    }

    public async Task<HoSoNguoiDung> LayHoSo(long id) => await db.HoSoNguoiDung.SingleOrDefaultAsync(x => x.MaTaiKhoan == id) ?? throw new BusinessException("Không tìm thấy hồ sơ.", 404);
    public async Task<Dictionary<long, string>> LayTenHienThiAsync() => await db.HoSoNguoiDung.ToDictionaryAsync(x => x.MaTaiKhoan, x => x.TenHienThi);
    public async Task<SharedPerson> LayThongTinChiaSe(long accountId)
    {
        var p = await db.HoSoNguoiDung.AsNoTracking().SingleOrDefaultAsync(x => x.MaTaiKhoan == accountId) ?? throw new BusinessException("Không tìm thấy hồ sơ.", 404);
        var viewer = Role == Traveler ? UserId : 0;
        var connected = viewer > 0 && await db.DeNghiKetNoiDongHanh.AnyAsync(c => c.TrangThai == HoTroDuLich.Web.Domain.TrangThaiDeNghiKetNoi.DaChapNhan && ((c.MaNguoiGui == viewer && db.NhuCauDongHanh.Any(n => n.MaNhuCau == c.MaNhuCau && n.MaTaiKhoan == accountId)) || (c.MaNguoiGui == accountId && db.NhuCauDongHanh.Any(n => n.MaNhuCau == c.MaNhuCau && n.MaTaiKhoan == viewer))));
        var details = p.MaTaiKhoan == UserId || p.ThongTinChiaSe == HoTroDuLich.Web.Domain.ThongTinChiaSe.CongKhai || (p.ThongTinChiaSe == HoTroDuLich.Web.Domain.ThongTinChiaSe.KhiKetNoi && connected);
        var a = connected ? await db.TaiKhoan.AsNoTracking().SingleAsync(x => x.MaTaiKhoan == accountId) : null;
        return new(accountId, p.TenHienThi, details && p.AnhDaiDien != null ? "/ho-so/anh-dai-dien/" + accountId : null, details ? p.GioiThieu : null, details ? p.NoiSinhSong : null, details ? p.SoThich : null, a?.Email, a?.SoDienThoai, connected);
    }
}

