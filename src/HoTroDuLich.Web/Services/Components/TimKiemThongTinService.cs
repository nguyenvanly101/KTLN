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
public partial class TimKiemThongTinService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider, IDanhMucService serviceDanhMuc, IQuanLyDichVuService serviceQuanLyDichVu) : NghiepVuService(context, accessor, environment, configuration, provider), ITimKiemThongTinService
{
    public async Task<(List<DichVuDuLich> Services, List<DiaDiemDuLich> Places)> TraCuuAsync(string? q, string? region, string? type, decimal? min, decimal? max, int page = 1)
    {
        RequireUseCase(SucAccess.TravelSearch);
        type = DanhMuc.ChuanHoa(type);
        Ensure((q?.Length ?? 0) <= 100 && page >= 1 && (min == null || min is >= 0 and <= 1000000000) && (max == null || max is >= 0 and <= 1000000000) && (min == null || max == null || min <= max) && (string.IsNullOrEmpty(type) || type == "DiaDiem" || DanhMuc.LoaiDichVu.Contains(type)) && (string.IsNullOrEmpty(region) || (await serviceDanhMuc.LayDanhMucKhuVuc()).Contains(region)), "0101-01");
        var criteria=new TieuChiTraCuu(q?.Trim(),region,type,min,max,page);
        return(await Resolve<IDichVu>().TimDichVu(criteria),await Resolve<IDiaDiem>().TimDiaDiem(criteria));
    }

    public async Task<NhaCungCapDichVu> LayNhaCungCapAsync(long id)
    {
        var s = await serviceQuanLyDichVu.LayChiTiet(id);
        return await Find<NhaCungCapDichVu>(s.MaNhaCungCap);
    }
}
