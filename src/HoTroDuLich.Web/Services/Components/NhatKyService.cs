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
public partial class NhatKyService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider) : NghiepVuService(context, accessor, environment, configuration, provider), INhatKyService
{
    public void Ghi(string kind, string target, long id, string? before, string? after, string? reason = null)
    {
        // Keep complete condition snapshots within the thesis's 200/200/500 columns.
        // Long snapshots are consecutive numbered fragments in the same transaction.
        var count=Math.Max(1,Math.Max(((before?.Length??0)+199)/200,Math.Max(((after?.Length??0)+199)/200,((reason?.Length??0)+479)/480)));
        static string? Part(string? value,int index,int size)=>value==null||index*size>=value.Length?null:value.Substring(index*size,Math.Min(size,value.Length-index*size));
        for(var index=0;index<count;index++)db.NhatKyThaoTac.Add(new(){MaTaiKhoan=UserId==0?(kind=="DangNhapThanhCong"?id:null):UserId,LoaiSuKien=kind,DoiTuong=target,MaDoiTuong=id,TrangThaiTruoc=Part(before,index,200),TrangThaiSau=Part(after,index,200),LyDo=count==1?reason:$"[{index+1}/{count}] "+Part(reason,index,480),DiaChiIP=http.HttpContext?.Connection.RemoteIpAddress?.ToString()});
    }
    public async Task GhiRiengAsync(string kind,string target,long id,string? before,string? after,string? reason=null)
    {
        try
        {
            await using var scope=Resolve<IServiceScopeFactory>().CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<INhatKyService>().Ghi(kind,target,id,before,after,reason);
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().SaveChangesAsync();
        }
        catch(Exception error)
        {
            Resolve<ILogger<NhatKyService>>().LogError("Không thể ghi nhật ký {Kind}; tham chiếu {Reference}; loại lỗi {Type}",kind,http.HttpContext?.TraceIdentifier,error.GetType().Name);
        }
    }
    public async Task<List<NhatKyThaoTac>> LayDanhSachAsync()
    {
        RoleRequired(AdminRole);
        return await db.NhatKyThaoTac.OrderByDescending(x => x.ThoiDiem).ThenByDescending(x => x.MaNhatKy).Take(200).ToListAsync();
    }

    public async Task<AdminOverview> TongQuanAsync()
    {
        RoleRequired(AdminRole);
        return new(await db.BanCapNhatDichVu.CountAsync(x => x.TrangThaiDuyet == HoTroDuLich.Web.Domain.TrangThaiDuyet.ChoDuyet), await db.HoSoXacMinhNhaCungCap.CountAsync(x => x.TrangThaiXuLy == HoTroDuLich.Web.Domain.TrangThaiHoSoXacMinh.ChoXacMinh), await db.NhaCungCapDichVu.CountAsync(), await db.GiaiTrinhNhaCungCap.CountAsync(x => x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiGiaiTrinh.ChoXemXet));
    }
}

