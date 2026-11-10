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
public partial class XetDuyetDichVuService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider, INhatKyService serviceNhatKy, IThongBaoService serviceThongBao) : NghiepVuService(context, accessor, environment, configuration, provider), IXetDuyetDichVuService
{
    public async Task<List<BanCapNhatDichVu>> LayHangDoiAsync()
    {
        RoleRequired(AdminRole);
        return await db.BanCapNhatDichVu.Where(x => x.TrangThaiDuyet == HoTroDuLich.Web.Domain.TrangThaiDuyet.ChoDuyet).OrderBy(x => x.ThoiDiemGui).ThenBy(x => x.MaBanCapNhat).ToListAsync();
    }

    public async Task DuyetAsync(CommandInput i)
    {
        RoleRequired(AdminRole);
        await Execute(async () =>
        {
            var v = await Find<BanCapNhatDichVu>(i.Id);
            Ensure(v.TrangThaiDuyet == HoTroDuLich.Web.Domain.TrangThaiDuyet.ChoDuyet && i.Version == v.SoPhienBan.ToString() && v.SoPhienBan == await db.BanCapNhatDichVu.Where(x => x.MaDichVu == v.MaDichVu).MaxAsync(x => x.SoPhienBan), "0602-03");
            var s = await Find<DichVuDuLich>(v.MaDichVu);
            Require(new[] { "DaDuyet", "TuChoi", "CanSua" }.Contains(i.Decision), "Quyết định không hợp lệ.", 400);
            if (i.Decision == "DaDuyet")
                Ensure(await ProviderActive(s.MaNhaCungCap), "0602-04");
            else
                Ensure(!string.IsNullOrWhiteSpace(i.Reason), "0602-02", "Reason");
            v.TrangThaiDuyet = System.Enum.Parse<HoTroDuLich.Web.Domain.TrangThaiDuyet>(i.Decision);
            v.LyDo = string.IsNullOrWhiteSpace(i.Reason) ? null : i.Reason;
            v.MaAdminDuyet = UserId;
            v.ThoiDiemDuyet = Clock.BayGioUtc;
            if (i.Decision == "DaDuyet")
            {
                var branches=Resolve<BranchService>();
                var branchIds=await db.Set<BanCapNhatDichVuCoSo>().Where(x=>x.RevisionId==v.MaBanCapNhat).Select(x=>x.BranchId).ToListAsync();
                // Revisions created before 007 have no branch snapshot; retain their legacy publication path.
                if(branchIds.Count>0)await branches.ValidateServiceBranches(s.MaNhaCungCap,branchIds,true);
                await branches.ReplaceServiceBranches(s.MaDichVu,branchIds);
                foreach (var field in ServiceFields)
                    s.GetType().GetProperty(field.Domain)!.SetValue(s, v.GetType().GetProperty(field.Domain)!.GetValue(v));
                s.PhienBanCongBo = v.SoPhienBan;
                s.TrangThaiCongBo = HoTroDuLich.Web.Domain.TrangThaiCongBo.DaDuyet;
                s.TrangThaiTiepNhan = HoTroDuLich.Web.Domain.TrangThaiTiepNhan.TiepNhan;
                s.MaQuyetDinhKhongTiepNhan = null;
            }
            else if (s.PhienBanCongBo == null)
                s.TrangThaiCongBo = System.Enum.Parse<HoTroDuLich.Web.Domain.TrangThaiCongBo>(i.Decision);
            s.ThoiDiemCapNhat = Clock.BayGioUtc;
            var p = await Find<NhaCungCapDichVu>(s.MaNhaCungCap);
            serviceThongBao.Tao(p.MaTaiKhoan,i.Decision=="DaDuyet"?"DichVuDaDuyet":i.Decision=="TuChoi"?"DichVuBiTuChoi":"DichVuCanSua","Dịch vụ " + v.TenDichVu + ": " + Labels.Status(i.Decision),"/nha-cung-cap/dich-vu/" + s.MaDichVu);
            serviceNhatKy.Ghi("DuyetDichVu", "BanCapNhatDichVu", v.MaBanCapNhat, "ChoDuyet", i.Decision, i.Reason);
        });
    }

    public async Task<(BanCapNhatDichVu Pending, DichVuDuLich Current, NhaCungCapDichVu Provider)> XemNoiDungAsync(long id)
    {
        RoleRequired(AdminRole);
        var v = await Find<BanCapNhatDichVu>(id);
        var s = await Find<DichVuDuLich>(v.MaDichVu);
        return (v, s, await Find<NhaCungCapDichVu>(s.MaNhaCungCap));
    }

    public async Task<FilteredPage<AdminReviewRow>> MoDanhSachChoDuyetAsync(string? q, string status, int page)
    {
        RoleRequired(AdminRole);
        q = SearchText(q);
        ValidFilter(status, "ChoDuyet", "DaDuyet", "CanSua", "TuChoi");
        var rows =
            from v in db.BanCapNhatDichVu.AsNoTracking()join s in db.DichVuDuLich on v.MaDichVu equals s.MaDichVu
            join p in db.NhaCungCapDichVu on s.MaNhaCungCap equals p.MaNhaCungCap
            where (status == "" || (v.TrangThaiDuyet).ToString() == status) && (q == "" || v.TenDichVu.Contains(q) || p.TenDonVi.Contains(q))orderby v.ThoiDiemGui descending, v.MaBanCapNhat descending
            select new AdminReviewRow(v, p.TenDonVi);
        return await PageOf(rows, "/Admin/ServiceReviews", q, status, page);
    }
}


