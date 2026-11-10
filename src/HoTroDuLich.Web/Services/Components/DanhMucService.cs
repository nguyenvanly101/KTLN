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
public partial class DanhMucService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider) : NghiepVuService(context, accessor, environment, configuration, provider), IDanhMucService
{
    public async Task<List<string>> LayDanhMucKhuVuc()
    {
        var cache = http.HttpContext!.RequestServices.GetRequiredService<IMemoryCache>();
        return (await cache.GetOrCreateAsync("DanhMuc.KhuVuc", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return await db.DiaDiemDuLich.Where(x => x.DangHienThi).Select(x => x.KhuVuc).Union(PublicServices().Select(x => x.KhuVucPhucVu)).Distinct().OrderBy(x => x).ToListAsync();
        }))!;
    }

    public async Task<List<DiaDiemDuLich>> LayDiaDiemHienThiAsync() => await db.DiaDiemDuLich.Where(x => x.DangHienThi).OrderBy(x => x.TenDiaDiem).ThenBy(x => x.MaDiaDiem).ToListAsync();
    public async Task<DiaDiemDuLich> LayChiTiet(long id) => await db.DiaDiemDuLich.SingleOrDefaultAsync(x => x.MaDiaDiem == id && x.DangHienThi) ?? throw new BusinessException("Địa điểm không khả dụng.", 404);
}

