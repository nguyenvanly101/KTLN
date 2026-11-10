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
public partial class ThongBaoService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider) : NghiepVuService(context, accessor, environment, configuration, provider), IThongBaoService
{
    public void Tao(long maNguoiNhan, string loai, string noiDung, string duongDan) => db.ThongBao.Add(new() { MaNguoiNhan = maNguoiNhan, LoaiThongBao = loai, NoiDung = noiDung, DuongDan = duongDan });
    public async Task<List<ThongBao>> DanhSachThongBaoAsync() => await db.ThongBao.Where(x => x.MaNguoiNhan == UserId).OrderByDescending(x => x.ThoiDiemTao).ThenByDescending(x => x.MaThongBao).ToListAsync();
    public async Task<string?> MoThongBaoAsync(long id)
    {
        var n = await db.ThongBao.SingleOrDefaultAsync(x => x.MaThongBao == id && x.MaNguoiNhan == UserId) ?? throw new BusinessException("Không tìm thấy thông báo.", 404);
        n.DaDoc = true;
        await db.SaveChangesAsync();
        return n.DuongDan;
    }

    public async Task<int> DemChuaDocAsync() => UserId == 0 ? 0 : await db.ThongBao.CountAsync(x => x.MaNguoiNhan == UserId && !x.DaDoc);
}

