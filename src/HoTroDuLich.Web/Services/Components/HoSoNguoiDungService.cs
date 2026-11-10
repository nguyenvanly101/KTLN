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
public partial class HoSoNguoiDungService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider, ILuuTepService serviceLuuTep) : NghiepVuService(context, accessor, environment, configuration, provider), IHoSoNguoiDungService
{
    public Task<HoSoNguoiDung> XemHoSoAsync()=>Resolve<IHoSoNguoiDung>().LayHoSo(UserId);
    public async Task<ProfileInput> MoBieuMauAsync()
    {
        var p = await XemHoSoAsync();
        var a = await Find<TaiKhoan>(UserId);
        var n = await db.NhaCungCapDichVu.SingleOrDefaultAsync(x => x.MaTaiKhoan == UserId);
        return new()
        {
            Version = Token(p.RowVersion),
            Name = p.TenHienThi,
            Phone = a.SoDienThoai,
            Bio = p.GioiThieu,
            City = p.NoiSinhSong,
            Interests = p.SoThich,
            Privacy = (p.ThongTinChiaSe).ToString(),
            ProviderVersion = n == null ? "" : Token(n.RowVersion),
            ContactName = n?.NguoiLienHe,
            Address = n?.DiaChi,
            ContactPhone = n?.SoDienThoai,
            ContactEmail = n?.EmailLienHe
        };
    }

    public async Task LuuThayDoiAsync(ProfileInput i)
    {
        RequireUseCase(SucAccess.Profile);
        i.Phone = NormalizePhone(i.Phone);
        i.ContactPhone = NormalizePhone(i.ContactPhone);
        Validate(i, "1201-01");
        Ensure(new[] { "CongKhai", "KhiKetNoi", "RiengTu" }.Contains(i.Privacy), "1201-01", "Privacy");
        var files = new List<(string Name, string Path, string Type, int Size)>();
        if (i.Avatar != null)
        {
            Ensure(new[] { ".jpg", ".jpeg", ".png" }.Contains(Path.GetExtension(i.Avatar.FileName).ToLowerInvariant()), "1201-01", "Avatar");
            files = await StoreFiles([i.Avatar]);
        }

        string? previousAvatar = null;
        await Execute(async () =>
        {
            var p = await XemHoSoAsync();
            CheckVersion(p, i.Version, "1201-03");
            var a = await Find<TaiKhoan>(UserId);
            Ensure(i.Phone == null || !await db.TaiKhoan.AnyAsync(x => x.MaTaiKhoan != UserId && x.SoDienThoai == i.Phone), "1201-02", "Phone");
            p.TenHienThi = i.Name.Trim();
            p.GioiThieu = i.Bio;
            p.NoiSinhSong = i.City;
            p.SoThich = i.Interests;
            p.ThongTinChiaSe = System.Enum.Parse<HoTroDuLich.Web.Domain.ThongTinChiaSe>(i.Privacy);
            p.ThoiDiemCapNhat = Clock.BayGioUtc;
            a.SoDienThoai = i.Phone;
            if (files.Count > 0)
            {
                previousAvatar = p.AnhDaiDien;
                p.AnhDaiDien = files[0].Path;
            }

            if (Role == ProviderRole)
            {
                var n = await MyProvider();
                CheckVersion(n, i.ProviderVersion, "1201-03");
                Ensure(!string.IsNullOrWhiteSpace(i.ContactName) && !string.IsNullOrWhiteSpace(i.Address) && !string.IsNullOrWhiteSpace(i.ContactPhone) && !string.IsNullOrWhiteSpace(i.ContactEmail), "1201-01");
                n.NguoiLienHe = i.ContactName!.Trim();
                n.DiaChi = i.Address!.Trim();
                n.SoDienThoai = i.ContactPhone!;
                n.EmailLienHe = i.ContactEmail!.Trim().ToLowerInvariant();
            }
        });
        if (previousAvatar != null && files.Count > 0)
        {
            var old = serviceLuuTep.DuongDanAnToan(previousAvatar);
            if (File.Exists(old))
                File.Delete(old);
        }
    }
}
