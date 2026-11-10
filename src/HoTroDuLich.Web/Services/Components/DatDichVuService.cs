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
public partial class DatDichVuService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider, INhatKyService serviceNhatKy, IThongBaoService serviceThongBao) : NghiepVuService(context, accessor, environment, configuration, provider), IDatDichVuService
{
    public IQueryable<YeuCauDatDichVu> TruyVanYeuCau() => db.YeuCauDatDichVu.Where(x => x.MaTaiKhoan == UserId || db.DichVuDuLich.Any(s => s.MaDichVu == x.MaDichVu && db.NhaCungCapDichVu.Any(p => p.MaNhaCungCap == s.MaNhaCungCap && p.MaTaiKhoan == UserId)));
    public async Task<List<YeuCauDatDichVu>> DanhSachCuaToiAsync() => await TruyVanYeuCau().OrderByDescending(x => x.ThoiDiemGui).ThenByDescending(x => x.MaYeuCau).ToListAsync();
    public async Task<YeuCauDatDichVu> ChiTietAsync(long id) => await TruyVanYeuCau().SingleOrDefaultAsync(x => x.MaYeuCau == id) ?? throw new BusinessException(MaLoi.TatCa["0701-04"].Message, 404, "0701-04");
    public void KiemTraDuLieu(BookingInput i)
    {
        Validate(i, "0701-03");
        Ensure(i.Start >= HomNay && (i.End == null || i.End >= i.Start) && i.SubmissionId != Guid.Empty, "0701-03");
        i.Selections = NormalizeSelections(i.Selections, "0701-03");
    }

    public async Task<DichVuDuLich> MoDatDichVuAsync(long id)
    {
        var s = await PublicServices().SingleOrDefaultAsync(x => x.MaDichVu == id);
        Ensure(s != null, "0701-01");
        Ensure(s!.TrangThaiTiepNhan == TrangThaiTiepNhan.TiepNhan && await ProviderActive(s.MaNhaCungCap), "0701-02");
        return s;
    }

    public async Task<List<string>> KiemTraTruocGuiAsync(BookingInput i)
    {
        RoleRequired(Traveler);
        KiemTraDuLieu(i);
        var s = await MoDatDichVuAsync(i.ServiceId);
        var branch=await Resolve<BranchService>().BookingBranch(i.ServiceId,i.BranchId);
        i.BranchId=branch?.Id;i.BranchVersion=branch!=null?Token(branch.RowVersion):null;
        await Resolve<AvailabilityService>().CheckNewRequest(s,i.Start,i.End);
        var warnings = new List<string>();
        if (i.PlanId is long planId)
        {
            var plan = await Resolve<ILapKeHoachService>().XemKeHoachAsync(planId);
            if (i.Start < plan.NgayBatDau || (i.End ?? i.Start) > plan.NgayKetThuc)
                warnings.Add("Ngày dịch vụ nằm ngoài khoảng ngày của chuyến đi. Hãy kiểm tra lại trước khi gửi.");
        }
        if (s.TamNgungTu != null && (i.End ?? i.Start) >= s.TamNgungTu && (s.TamNgungDen == null || i.Start <= s.TamNgungDen))
            warnings.Add("Khoảng ngày đã chọn giao với thời gian tạm ngừng được công bố. Hãy trao đổi với nhà cung cấp trước khi xác nhận.");
        if (await db.BanCapNhatDichVu.AnyAsync(x => x.MaDichVu == i.ServiceId && x.TrangThaiDuyet == HoTroDuLich.Web.Domain.TrangThaiDuyet.ChoDuyet))
            warnings.Add("Dịch vụ có bản cập nhật đang chờ duyệt; thông tin có thể thay đổi.");
        warnings.Add("Gửi yêu cầu không bảo đảm còn chỗ. Yêu cầu chỉ được xác nhận khi hai bên đồng ý cùng điều kiện.");
        return warnings;
    }

    public async Task<long> GuiYeuCauAsync(BookingInput i)
    {
        RoleRequired(Traveler);
        var duplicate = await db.YeuCauDatDichVu.AsNoTracking().SingleOrDefaultAsync(x => x.MaTaiKhoan == UserId && x.MaLuotGui == i.SubmissionId);
        if (duplicate != null)
            return duplicate.MaYeuCau;
        KiemTraDuLieu(i);
        long id = 0;
        try
        {
            await Execute(async () =>
            {
                var existing = await db.YeuCauDatDichVu.SingleOrDefaultAsync(x => x.MaTaiKhoan == UserId && x.MaLuotGui == i.SubmissionId);
                if (existing != null)
                {
                    id = existing.MaYeuCau;
                    return;
                }

                var s = await MoDatDichVuAsync(i.ServiceId);
                var branch=await Resolve<BranchService>().BookingBranch(i.ServiceId,i.BranchId);
                if(branch!=null)Require(Token(branch.RowVersion)==i.BranchVersion,"Thông tin cơ sở đã thay đổi. Kiểm tra lại yêu cầu trước khi gửi.",409);
        await Resolve<AvailabilityService>().CheckNewRequest(s,i.Start,i.End);
                if (i.PlanId is long planId) await Resolve<ILapKeHoachService>().XemKeHoachAsync(planId);
                var y = new YeuCauDatDichVu
                {
                    MaTaiKhoan = UserId,
                    MaDichVu = i.ServiceId,
                    MaKeHoach = i.PlanId,
                    NgayBatDau = i.Start,
                    NgayKetThuc = i.End,
                    ThoiGianDenDi = i.ArrivalDeparture,
                    SoNguoi = i.People,
                    SoLuong = i.Quantity,
                    NoiDungLuaChon = i.Selections,
                    GhiChu = i.Notes,
                    ChiPhiDuKien = i.Cost,
                    PhienBan = 1,
                    TrangThai = HoTroDuLich.Web.Domain.TrangThaiYeuCau.ChoPhanHoi,
                    MaLuotGui = i.SubmissionId
                };
                db.Add(y);
                await db.SaveChangesAsync();
                id = y.MaYeuCau;
                if(branch!=null)db.Add(new YeuCauDatCoSo{RequestId=id,BranchId=branch.Id,BranchName=branch.Name,Address=branch.Address,GooglePlaceId=branch.GooglePlaceId,ProviderLatitude=branch.LocationSource=="Provider"?branch.Latitude:null,ProviderLongitude=branch.LocationSource=="Provider"?branch.Longitude:null});
                var p = await Find<NhaCungCapDichVu>(s.MaNhaCungCap);
                serviceThongBao.Tao(p.MaTaiKhoan,"YeuCauDatMoi","Có yêu cầu đặt dịch vụ mới #" + id,"/nha-cung-cap/yeu-cau/" + id);
            });
        }
        catch (DbUpdateException e)when (SqlError(e)is 2601 or 2627)
        {
            var saved = await db.YeuCauDatDichVu.AsNoTracking().SingleOrDefaultAsync(x => x.MaTaiKhoan == UserId && x.MaLuotGui == i.SubmissionId);
            if (saved == null)
                throw;
            id = saved.MaYeuCau;
        }

        return id;
    }

    public async Task ChapNhanAsync(CommandInput i)
    {
        RoleRequired(new[] { "Rut", "KhachDongY", "KhachKhongDongY" }.Contains(i.Decision) ? Traveler : ProviderRole);
        await Execute(async () =>
        {
            var y = await ChiTietAsync(i.Id);
            CheckVersion(y, i.Version, "0701-05");
            Ensure(i.BusinessVersion == y.PhienBan, "0701-05");
            var s = await Find<DichVuDuLich>(y.MaDichVu);
            var p = await Find<NhaCungCapDichVu>(s.MaNhaCungCap);
            var before = BookingSnapshot(y);
            string type;
            if (i.Decision == "Rut")
            {
                Ensure(y.MaTaiKhoan == UserId && new HoTroDuLich.Web.Domain.TrangThaiYeuCau[] { HoTroDuLich.Web.Domain.TrangThaiYeuCau.ChoPhanHoi, HoTroDuLich.Web.Domain.TrangThaiYeuCau.DeXuatKhac }.Contains(y.TrangThai), "0801-02");
                y.TrangThai = HoTroDuLich.Web.Domain.TrangThaiYeuCau.KhongTiepTuc;
                y.LyDo = i.Reason;
                type = "YeuCauDaRut";
            }
            else if (i.Decision is "KhachDongY" or "KhachKhongDongY")
            {
                Ensure(y.MaTaiKhoan == UserId && y.TrangThai == HoTroDuLich.Web.Domain.TrangThaiYeuCau.DeXuatKhac, "0701-05");
                var available = await ProviderActive(p.MaNhaCungCap) && s.TrangThaiTiepNhan == HoTroDuLich.Web.Domain.TrangThaiTiepNhan.TiepNhan && s.TrangThaiCongBo == HoTroDuLich.Web.Domain.TrangThaiCongBo.DaDuyet;
                y.TrangThai = System.Enum.Parse<HoTroDuLich.Web.Domain.TrangThaiYeuCau>(i.Decision == "KhachDongY" && available ? "DaXacNhan" : "KhongTiepTuc");
                if (!available)
                    y.LyDo = "Nhà cung cấp không còn tiếp nhận yêu cầu mới";
                type = y.TrangThai == HoTroDuLich.Web.Domain.TrangThaiYeuCau.DaXacNhan ? "YeuCauDaXacNhan" : "YeuCauKhongTiepTuc";
            }
            else
            {
                Require(p.MaTaiKhoan == UserId, "Không có quyền phản hồi.", 404);
                Ensure(y.TrangThai == HoTroDuLich.Web.Domain.TrangThaiYeuCau.ChoPhanHoi, "0701-05");
                Require(new[] { "DaXacNhan", "TuChoi", "DeXuatKhac" }.Contains(i.Decision), "Quyết định không hợp lệ.", 400);
                if (i.Decision != "TuChoi")
                    await MoDatDichVuAsync(y.MaDichVu);
                else
                    Ensure(!string.IsNullOrWhiteSpace(i.Reason), "0701-06", "Reason");
                if (i.Decision == "DeXuatKhac")
                {
                    Require(!string.IsNullOrWhiteSpace(i.Text) && i.Text.Length <= 2000, "Vui lòng giải thích điều kiện đề xuất, tối đa 2000 ký tự.", 400);
                    var start = i.Start ?? y.NgayBatDau;
                    var end = i.End ?? y.NgayKetThuc;
                    var people = i.People ?? y.SoNguoi;
                    var quantity = i.Quantity ?? y.SoLuong;
                    var cost = i.Cost ?? y.ChiPhiDuKien;
                    var selections = i.Selections == null ? y.NoiDungLuaChon : NormalizeSelections(i.Selections, "0701-03");
                    var arrival = i.ArrivalDeparture ?? y.ThoiGianDenDi;
                    Ensure(start >= HomNay && (end == null || end >= start) && people is >= 1 and <= 100 && quantity is >= 1 and <= 100 && (cost == null || cost is >= 0 and <= 1000000000) && (arrival?.Length ?? 0) <= 100, "0701-03");
                    Ensure(start != y.NgayBatDau || end != y.NgayKetThuc || people != y.SoNguoi || quantity != y.SoLuong || cost != y.ChiPhiDuKien || selections != y.NoiDungLuaChon || arrival != y.ThoiGianDenDi, "0701-07");
                    y.NgayBatDau = start;
                    y.NgayKetThuc = end;
                    y.SoNguoi = people;
                    y.SoLuong = quantity;
                    y.ChiPhiDuKien = cost;
                    y.NoiDungLuaChon = selections;
                    y.ThoiGianDenDi = arrival;
                    y.DieuKienDeXuat = i.Text.Trim();
                }

                y.TrangThai = System.Enum.Parse<HoTroDuLich.Web.Domain.TrangThaiYeuCau>(i.Decision);
                y.LyDo = string.IsNullOrWhiteSpace(i.Reason) ? null : i.Reason;
                type = i.Decision == "DaXacNhan" ? "YeuCauDaXacNhan" : i.Decision == "TuChoi" ? "YeuCauBiTuChoi" : "YeuCauCoDeXuatKhac";
            }

            y.ThoiDiemPhanHoi = Clock.BayGioUtc;
            y.PhienBan++;
            serviceNhatKy.Ghi(i.Decision=="DeXuatKhac"?"DeXuatDieuKien":"PhanHoiDatDichVu", "YeuCauDatDichVu", y.MaYeuCau, before, BookingSnapshot(y), i.Reason);
            serviceThongBao.Tao(UserId == y.MaTaiKhoan ? p.MaTaiKhoan : y.MaTaiKhoan,type,"Yêu cầu #" + y.MaYeuCau + ": " + Labels.Status((y.TrangThai).ToString()),UserId == y.MaTaiKhoan ? "/nha-cung-cap/yeu-cau/" + y.MaYeuCau : "/yeu-cau/" + y.MaYeuCau);
        });
    }

    public async Task<List<string>> LayHangMucAsync(long id)
    {
        var y = await ChiTietAsync(id);
        var s = await Find<DichVuDuLich>(y.MaDichVu);
        var suffix = " × " + y.SoLuong;
        var main = s.TenDichVu[..Math.Min(s.TenDichVu.Length, 200 - suffix.Length)] + suffix;
        return new[]
        {
            main
        }.Concat((y.NoiDungLuaChon ?? "").Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).ToList();
    }

    public async Task<Dictionary<long, string>> LayTenDichVuAsync() => await TruyVanYeuCau().Join(db.DichVuDuLich, y => y.MaDichVu, s => s.MaDichVu, (y, s) => new { y.MaYeuCau, s.TenDichVu }).ToDictionaryAsync(x => x.MaYeuCau, x => x.TenDichVu);
    public async Task<string> LayTenDichVuCuaYeuCauAsync(long id)
    {
        var y = await ChiTietAsync(id);
        return (await Find<DichVuDuLich>(y.MaDichVu)).TenDichVu;
    }
}

