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
public partial class DangNhapService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider, INhatKyService serviceNhatKy) : NghiepVuService(context, accessor, environment, configuration, provider), IDangNhapService
{
    public async Task<TaiKhoan> DangNhapAsync(LoginInput i)
    {
        Validate(i, "1101-01");
        var identity = i.Email.Trim();
        var email = identity.ToLowerInvariant();
        var phone = NormalizePhone(identity);
        var a=await Resolve<ITaiKhoan>().TimTheoDinhDanh(identity);
        var hasher = new PasswordHasher<TaiKhoan>();
        var check = hasher.VerifyHashedPassword(a ?? FakeAccount, a?.MatKhauBam ?? FakePasswordHash, i.Password);
        if (a == null || check == PasswordVerificationResult.Failed)
        {
            await serviceNhatKy.GhiRiengAsync("DangNhapThatBai", "TaiKhoan", 0, null, "ThatBai", "Định danh: " + identity[..Math.Min(identity.Length, 2)] + "***; sai thông tin");
            Ensure(false, "1101-02", "Password");
        }

        if(a!.TrangThai!=TrangThaiTaiKhoan.HoatDong)await serviceNhatKy.GhiRiengAsync("DangNhapThatBai","TaiKhoan",a.MaTaiKhoan,null,"DaNgung","Định danh: "+identity[..Math.Min(identity.Length,2)]+"***; đã ngừng");
        Ensure(a.TrangThai == HoTroDuLich.Web.Domain.TrangThaiTaiKhoan.HoatDong, "1101-03");
        await Execute(async () =>
        {
            a=await Resolve<ITaiKhoan>().LayTaiKhoan(a!.MaTaiKhoan);
            Ensure(a.TrangThai==TrangThaiTaiKhoan.HoatDong,"1101-03");
            Ensure(hasher.VerifyHashedPassword(a,a.MatKhauBam,i.Password)!=PasswordVerificationResult.Failed,"1101-02");
            if (check == PasswordVerificationResult.SuccessRehashNeeded)
                a.MatKhauBam = hasher.HashPassword(a, i.Password);
            a.ThoiDiemDangNhapCuoi = Clock.BayGioUtc;
            serviceNhatKy.Ghi("DangNhapThanhCong", "TaiKhoan", a.MaTaiKhoan, null, "ThanhCong");
        });
        return a;
    }

    public async Task<TaiKhoan> DoiMatKhauAsync(PasswordChangeInput i)
    {
        Validate(i, "1001-03");
        var a = await Find<TaiKhoan>(UserId);
        var hasher = new PasswordHasher<TaiKhoan>();
        Ensure(hasher.VerifyHashedPassword(a, a.MatKhauBam, i.OldPassword) != PasswordVerificationResult.Failed, "1101-02", "OldPassword");
        Ensure(ValidPassword(i.NewPassword, a.Email) && i.NewPassword == i.ConfirmPassword && i.NewPassword != i.OldPassword, "1001-03", "NewPassword");
        await Execute(async () =>
        {
            a=await Resolve<ITaiKhoan>().LayTaiKhoan(UserId);
            Ensure(hasher.VerifyHashedPassword(a,a.MatKhauBam,i.OldPassword)!=PasswordVerificationResult.Failed,"1101-02");
            a.MatKhauBam = hasher.HashPassword(a, i.NewPassword);
            a.PhaiDoiMatKhau = false;
            a.DauPhien = Guid.NewGuid();
            serviceNhatKy.Ghi("DoiMatKhau", "TaiKhoan", a.MaTaiKhoan, null, "DaDoi");
        });
        return a;
    }

    public async Task DangXuatAsync()
    {
        serviceNhatKy.Ghi("DangXuat", "TaiKhoan", UserId, null, "KetThucPhien");
        await db.SaveChangesAsync();
    }
}
