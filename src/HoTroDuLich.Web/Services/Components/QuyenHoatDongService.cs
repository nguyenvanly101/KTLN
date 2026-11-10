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
public partial class QuyenHoatDongService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider, INhatKyService serviceNhatKy, IThongBaoService serviceThongBao) : NghiepVuService(context, accessor, environment, configuration, provider), IQuyenHoatDongService
{
    public async Task<List<QuyetDinhQuyenHoatDong>> LayQuyetDinhCuaToiAsync()
    {
        var p = await MyProvider();
        return await db.QuyetDinhQuyenHoatDong.Where(x => x.MaNhaCungCap == p.MaNhaCungCap).OrderByDescending(x => x.ThoiDiem).ThenByDescending(x=>x.MaQuyetDinh).ToListAsync();
    }

    public async Task NhanGiaiTrinhAsync(CommandInput i)
    {
        RoleRequired(ProviderRole);
        Require(!string.IsNullOrWhiteSpace(i.Text) && i.Text.Length <= 2000, "Vui lòng nhập giải trình, tối đa 2000 ký tự.", 400);
        var files = await StoreFiles(i.Documents);
        await Execute(async () =>
        {
            var p = await MyProvider();
            Ensure(new HoTroDuLich.Web.Domain.TrangThaiQuyenHoatDong? [] { HoTroDuLich.Web.Domain.TrangThaiQuyenHoatDong.TamDinhChi, HoTroDuLich.Web.Domain.TrangThaiQuyenHoatDong.DaThuHoi }.Contains(p.TrangThaiQuyenHoatDong), "1301-07");
            var d = await db.QuyetDinhQuyenHoatDong.Where(x => x.MaNhaCungCap == p.MaNhaCungCap).OrderByDescending(x => x.MaQuyetDinh).FirstOrDefaultAsync();
            Ensure(d != null && d.MaQuyetDinh == i.Id, "1301-07");
            Ensure(!await db.GiaiTrinhNhaCungCap.AnyAsync(x => x.MaQuyetDinh == i.Id && x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiGiaiTrinh.ChoXemXet), "1301-08");
            var g = new GiaiTrinhNhaCungCap
            {
                MaQuyetDinh = i.Id,
                NoiDung = i.Text.Trim(),
                TrangThai = HoTroDuLich.Web.Domain.TrangThaiGiaiTrinh.ChoXemXet
            };
            db.Add(g);
            await db.SaveChangesAsync();
            foreach (var f in files)
                db.Add(new TaiLieuGiaiTrinh { MaGiaiTrinh = g.MaGiaiTrinh, TenTepGoc = f.Name, DuongDan = f.Path, LoaiTep = System.Enum.Parse<HoTroDuLich.Web.Domain.LoaiTep>(f.Type), KichThuoc = f.Size });
            foreach (var admin in await db.TaiKhoan.Where(x => x.VaiTro == HoTroDuLich.Web.Domain.VaiTro.QUAN_TRI_VIEN && x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiTaiKhoan.HoatDong).Select(x => x.MaTaiKhoan).ToListAsync())
                serviceThongBao.Tao(admin,"GiaiTrinhMoi","Nhà cung cấp " + p.TenDonVi + " đã gửi giải trình.","/quan-tri/quyen-hoat-dong/xem-chi-tiet/" + p.MaNhaCungCap);
        });
    }

    public async Task<List<NhaCungCapDichVu>> LayDanhSachAsync()
    {
        RoleRequired(AdminRole);
        return await db.NhaCungCapDichVu.OrderBy(x => x.TenDonVi).ThenBy(x => x.MaNhaCungCap).ToListAsync();
    }

    public async Task<RightsConsequences> XemHeQuaAsync(long id, string decision)
    {
        RoleRequired(AdminRole);
        var p = await Find<NhaCungCapDichVu>(id);
        CheckRightsOperation(p, decision);
        var services = db.DichVuDuLich.Where(x => x.MaNhaCungCap == id);
        var booking = db.YeuCauDatDichVu.Where(x => services.Any(s => s.MaDichVu == x.MaDichVu));
        var latest = await db.QuyetDinhQuyenHoatDong.Where(x => x.MaNhaCungCap == id && x.TrangThaiSau == HoTroDuLich.Web.Domain.TrangThaiQuyenHoatDong.TamDinhChi).OrderByDescending(x => x.MaQuyetDinh).FirstOrDefaultAsync();
        return new(p, decision, await services.CountAsync(x => decision == "DangHoatDong" ? latest != null && x.MaQuyetDinhKhongTiepNhan == latest.MaQuyetDinh : decision == "DaThuHoi" || x.TrangThaiTiepNhan == HoTroDuLich.Web.Domain.TrangThaiTiepNhan.TiepNhan), decision == "DaThuHoi" ? await services.CountAsync(x => x.TrangThaiCongBo == HoTroDuLich.Web.Domain.TrangThaiCongBo.DaDuyet) : 0, decision == "DangHoatDong" ? 0 : await booking.CountAsync(x => x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiYeuCau.ChoPhanHoi || x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiYeuCau.DeXuatKhac), await booking.CountAsync(x => x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiYeuCau.DaXacNhan || x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiYeuCau.ChoKiemTra));
    }

    public async Task TamDinhChiAsync(CommandInput i)
    {
        RoleRequired(AdminRole);
        Ensure(!string.IsNullOrWhiteSpace(i.Reason) && i.Reason.Length <= 500 && !string.IsNullOrWhiteSpace(i.Evidence) && i.Evidence.Length <= 500 && (i.Decision != "TamDinhChi" || i.Until == null || i.Until > HomNay), "1301-02");
        await Execute(async () =>
        {
            var p = await Find<NhaCungCapDichVu>(i.Id);
            CheckVersion(p, i.Version, "1301-03");
            Ensure((await Find<TaiKhoan>(p.MaTaiKhoan)).TrangThai == HoTroDuLich.Web.Domain.TrangThaiTaiKhoan.HoatDong, "1301-04");
            CheckRightsOperation(p, i.Decision);
            var before = p.TrangThaiQuyenHoatDong!;
            var previous = await db.QuyetDinhQuyenHoatDong.Where(x => x.MaNhaCungCap == p.MaNhaCungCap).OrderByDescending(x => x.MaQuyetDinh).FirstOrDefaultAsync();
            var d = new QuyetDinhQuyenHoatDong
            {
                MaNhaCungCap = i.Id,
                TrangThaiTruoc = before.Value,
                TrangThaiSau = System.Enum.Parse<HoTroDuLich.Web.Domain.TrangThaiQuyenHoatDong>(i.Decision),
                LyDo = i.Reason.Trim(),
                CanCu = i.Evidence.Trim(),
                ThoiHan = i.Decision == "TamDinhChi" ? i.Until : null,
                MaAdmin = UserId
            };
            db.Add(d);
            await db.SaveChangesAsync();
            p.TrangThaiQuyenHoatDong = System.Enum.Parse<HoTroDuLich.Web.Domain.TrangThaiQuyenHoatDong>(i.Decision);
            if (i.Decision == "DaThuHoi")
                p.TrangThaiXacMinh = HoTroDuLich.Web.Domain.TrangThaiXacMinh.DaThuHoi;
            foreach (var s in await db.DichVuDuLich.Where(x => x.MaNhaCungCap == p.MaNhaCungCap).ToListAsync())
            {
                if (i.Decision == "DangHoatDong")
                {
                    if (previous != null && s.MaQuyetDinhKhongTiepNhan == previous.MaQuyetDinh)
                    {
                        s.MaQuyetDinhKhongTiepNhan = null;
                        s.TrangThaiTiepNhan = HoTroDuLich.Web.Domain.TrangThaiTiepNhan.TiepNhan;
                    }
                }
                else
                {
                    if (i.Decision == "DaThuHoi" || s.TrangThaiTiepNhan == HoTroDuLich.Web.Domain.TrangThaiTiepNhan.TiepNhan)
                    {
                        s.TrangThaiTiepNhan = HoTroDuLich.Web.Domain.TrangThaiTiepNhan.KhongTiepNhanMoi;
                        s.MaQuyetDinhKhongTiepNhan = d.MaQuyetDinh;
                    }

                    if (i.Decision == "DaThuHoi")
                    {
                        if (s.TrangThaiCongBo == HoTroDuLich.Web.Domain.TrangThaiCongBo.DaDuyet)
                            s.TrangThaiCongBo = HoTroDuLich.Web.Domain.TrangThaiCongBo.NgungCongBo;
                        else if (s.TrangThaiCongBo is HoTroDuLich.Web.Domain.TrangThaiCongBo.ChoDuyet or HoTroDuLich.Web.Domain.TrangThaiCongBo.CanSua)
                            s.TrangThaiCongBo = HoTroDuLich.Web.Domain.TrangThaiCongBo.TuChoi;
                        foreach (var v in await db.BanCapNhatDichVu.Where(x => x.MaDichVu == s.MaDichVu && x.TrangThaiDuyet == HoTroDuLich.Web.Domain.TrangThaiDuyet.ChoDuyet).ToListAsync())
                        {
                            v.TrangThaiDuyet = TrangThaiDuyet.TuChoi;
                            v.LyDo = "Nhà cung cấp bị thu hồi quyền hoạt động";
                            v.MaAdminDuyet = UserId;
                            v.ThoiDiemDuyet = Clock.BayGioUtc;
                        }
                    }

                    foreach (var y in await db.YeuCauDatDichVu.Where(x => x.MaDichVu == s.MaDichVu && (x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiYeuCau.ChoPhanHoi || x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiYeuCau.DeXuatKhac)).ToListAsync())
                    {
                        y.TrangThai = TrangThaiYeuCau.KhongTiepTuc;
                        y.LyDo = i.Decision == "TamDinhChi" ? "Nhà cung cấp bị tạm đình chỉ" : "Nhà cung cấp bị thu hồi quyền hoạt động";
                        y.ThoiDiemPhanHoi = Clock.BayGioUtc;
                        y.PhienBan++;
                        serviceThongBao.Tao(y.MaTaiKhoan,"YeuCauKhongTiepTuc","Yêu cầu #" + y.MaYeuCau + " không tiếp tục: " + y.LyDo,"/yeu-cau/" + y.MaYeuCau);
                    }
                }

                s.ThoiDiemCapNhat = Clock.BayGioUtc;
            }

            if (i.Decision is "DangHoatDong" or "DaThuHoi")
                foreach (var g in await db.GiaiTrinhNhaCungCap.Where(x => x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiGiaiTrinh.ChoXemXet && db.QuyetDinhQuyenHoatDong.Any(q => q.MaQuyetDinh == x.MaQuyetDinh && q.MaNhaCungCap == p.MaNhaCungCap) && (i.Decision == "DaThuHoi" || (previous != null && x.MaQuyetDinh == previous.MaQuyetDinh))).ToListAsync())
                {
                    g.TrangThai = TrangThaiGiaiTrinh.DaXemXet;
                    g.KetQuaXemXet = i.Decision == "DangHoatDong" ? "Đã khôi phục quyền hoạt động" : "Đã thu hồi quyền hoạt động";
                    g.MaAdminXemXet = UserId;
                    g.ThoiDiemXemXet = Clock.BayGioUtc;
                }

            serviceNhatKy.Ghi("DoiQuyenHoatDong", "NhaCungCapDichVu", p.MaNhaCungCap, (before).ToString(), i.Decision, i.Reason+"; Căn cứ: "+i.Evidence);
            serviceThongBao.Tao(p.MaTaiKhoan,i.Decision=="TamDinhChi"?"QuyenHoatDongTamDinhChi":i.Decision=="DangHoatDong"?"QuyenHoatDongKhoiPhuc":"QuyenHoatDongThuHoi","Quyền hoạt động: " + Labels.Status(i.Decision) + ". " + i.Reason + ". Căn cứ: " + i.Evidence + (i.Until == null ? "" : ". Thời hạn: " + i.Until),"/nha-cung-cap/quyen-hoat-dong");
        });
    }

    public async Task XemXetGiaiTrinhAsync(CommandInput i)
    {
        RoleRequired(AdminRole);
        Ensure(!string.IsNullOrWhiteSpace(i.Reason) && i.Reason.Length <= 500, "1301-02");
        await Execute(async () =>
        {
            var g = await Find<GiaiTrinhNhaCungCap>(i.Id);
            Ensure(g.TrangThai == HoTroDuLich.Web.Domain.TrangThaiGiaiTrinh.ChoXemXet, "1301-06");
            g.TrangThai = HoTroDuLich.Web.Domain.TrangThaiGiaiTrinh.DaXemXet;
            g.KetQuaXemXet = i.Reason;
            g.MaAdminXemXet = UserId;
            g.ThoiDiemXemXet = Clock.BayGioUtc;
            var d = await Find<QuyetDinhQuyenHoatDong>(g.MaQuyetDinh);
            var p = await Find<NhaCungCapDichVu>(d.MaNhaCungCap);
            serviceThongBao.Tao(p.MaTaiKhoan,"GiaiTrinhDaXemXet","Giải trình đã được xem xét: " + i.Reason,"/nha-cung-cap/quyen-hoat-dong");
        });
    }

    public async Task<(NhaCungCapDichVu Provider, List<QuyetDinhQuyenHoatDong> Decisions, List<GiaiTrinhNhaCungCap> Explanations, List<TaiLieuGiaiTrinh> Files)> XemChiTietAsync(long id)
    {
        RoleRequired(AdminRole, ProviderRole);
        var p = await Find<NhaCungCapDichVu>(id);
        Require(Role == AdminRole || p.MaTaiKhoan == UserId, "Không có quyền.", 403);
        var decisions = await db.QuyetDinhQuyenHoatDong.Where(x => x.MaNhaCungCap == id).OrderByDescending(x => x.ThoiDiem).ThenByDescending(x=>x.MaQuyetDinh).ToListAsync();
        var ids = decisions.Select(x => x.MaQuyetDinh).ToArray();
        var gs = await db.GiaiTrinhNhaCungCap.Where(x => ids.Contains(x.MaQuyetDinh)).ToListAsync();
        var gids = gs.Select(x => x.MaGiaiTrinh).ToArray();
        return (p, decisions, gs, await db.TaiLieuGiaiTrinh.Where(x => gids.Contains(x.MaGiaiTrinh)).ToListAsync());
    }

    public async Task<FilteredPage<NhaCungCapDichVu>> MoDanhSachAsync(string? q, string status, int page)
    {
        RoleRequired(AdminRole);
        q = SearchText(q);
        ValidFilter(status,"DangHoatDong","TamDinhChi","DaThuHoi","CanXemXet");
        var candidates=db.NhaCungCapDichVu.AsNoTracking().Where(p=>p.TrangThaiXacMinh==TrangThaiXacMinh.DaXacMinh||p.TrangThaiXacMinh==TrangThaiXacMinh.DaThuHoi);
        if(status=="CanXemXet")candidates=candidates.Where(p=>db.GiaiTrinhNhaCungCap.Any(g=>g.TrangThai==TrangThaiGiaiTrinh.ChoXemXet&&db.QuyetDinhQuyenHoatDong.Any(d=>d.MaQuyetDinh==g.MaQuyetDinh&&d.MaNhaCungCap==p.MaNhaCungCap))||(p.TrangThaiQuyenHoatDong==TrangThaiQuyenHoatDong.TamDinhChi&&db.QuyetDinhQuyenHoatDong.Any(d=>d.MaNhaCungCap==p.MaNhaCungCap&&d.TrangThaiSau==TrangThaiQuyenHoatDong.TamDinhChi&&d.ThoiHan<=HomNay&&!db.QuyetDinhQuyenHoatDong.Any(newer=>newer.MaNhaCungCap==p.MaNhaCungCap&&newer.MaQuyetDinh>d.MaQuyetDinh)))||db.KetQuaCungCapDichVu.Any(r=>r.TrangThaiKiemTra==TrangThaiKiemTra.ChoKiemTra&&db.YeuCauDatDichVu.Any(y=>y.MaYeuCau==r.MaYeuCau&&db.DichVuDuLich.Any(s=>s.MaDichVu==y.MaDichVu&&s.MaNhaCungCap==p.MaNhaCungCap))));
        else if(status!="")candidates=candidates.Where(p=>p.TrangThaiQuyenHoatDong.ToString()==status);
        var rows=candidates.Where(p=>q==""||p.TenDonVi.Contains(q)||p.EmailLienHe.Contains(q)).OrderBy(p=>p.TenDonVi).ThenBy(p=>p.MaNhaCungCap);
        return await PageOf(rows, "/Admin/Providers", q, status, page);
    }
}


