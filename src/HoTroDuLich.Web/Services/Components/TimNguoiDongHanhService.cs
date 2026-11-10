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
public partial class TimNguoiDongHanhService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider, ICongBoNhuCauService serviceCongBoNhuCau) : NghiepVuService(context, accessor, environment, configuration, provider), ITimNguoiDongHanhService
{
    public async Task<List<NhuCauDongHanh>> TimNhuCauAsync(string? q, DateOnly? start, DateOnly? end, decimal? budget, string? style, int page = 1)
    {
        RequireUseCase(SucAccess.CompanionSearch);
        style = DanhMuc.ChuanHoa(style);
        Ensure((start == null || end == null || end >= start) && (budget == null || budget is >= 0 and <= 1000000000) && page >= 1 && (q?.Length ?? 0) <= 200 && (style == "" || DanhMuc.HinhThucDi.Contains(style)), "0301-01");
        var viewer = CompanionViewer;
        var n = serviceCongBoNhuCau.NguoiXemThayDuoc().Where(x => x.MaTaiKhoan != viewer);
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            n = n.Where(x => x.DiemDen.Contains(q));
        }

        if (start != null)
            n = n.Where(x => x.NgayKetThuc >= start);
        if (end != null)
            n = n.Where(x => x.NgayBatDau <= end);
        if (budget != null)
            n = n.Where(x => x.NganSachDuKien == null || x.NganSachDuKien <= budget);
        if (style != "")
        {
            var legacy = DanhMuc.Ten(style);
            n = n.Where(x => x.HinhThucDi == style || x.HinhThucDi == legacy);
        }

        return await SharedNeeds(n.OrderByDescending(x => x.ThoiDiemDang).ThenByDescending(x => x.MaNhuCau).Skip((page - 1) * 20).Take(20)).ToListAsync();
    }

    public async Task<NhuCauDongHanh> XemChiTietAsync(long id)
    {
        RequireUseCase(SucAccess.CompanionSearch);
        return await SharedNeeds(serviceCongBoNhuCau.NguoiXemThayDuoc().Where(x => x.MaNhuCau == id)).SingleOrDefaultAsync() ?? throw new BusinessException(MaLoi.TatCa["0301-02"].Message, 404, "0301-02");
    }
}
