using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;
public record ThongTinDuLich(DiaDiemDuLich? DiaDiem, DichVuDuLich? DichVu, NhaCungCapDichVu? NhaCungCap, List<DichVuDuLich> DichVuTaiDiem);
public partial interface ITimKiemThongTinService
{
    Task<List<string>> MoTimKiemAsync();
    Task<ThongTinDuLich> XemChiTietAsync(string loai, long maMuc);
}

public partial class TimKiemThongTinService
{
    public Task<List<string>> MoTimKiemAsync() => Resolve<IDanhMucService>().LayDanhMucKhuVuc();
    public async Task<ThongTinDuLich> XemChiTietAsync(string loai, long maMuc)
    {
        RequireUseCase(SucAccess.TravelSearch);
        Ensure(loai is "DiaDiem" or "DichVu", "0101-03");
        try
        {
            if (loai == "DiaDiem")
            {
                var place = await Resolve<IDanhMucService>().LayChiTiet(maMuc);
                return new(place, null, null, await PublicServices().Where(x => x.MaDiaDiem == maMuc).OrderBy(x => x.TenDichVu).ThenBy(x => x.MaDichVu).ToListAsync());
            }

            var service = await Resolve<IQuanLyDichVuService>().LayChiTiet(maMuc);
            return new(null, service, await Find<NhaCungCapDichVu>(service.MaNhaCungCap), []);
        }
        catch (BusinessException)
        {
            Ensure(false, "0101-03");
            throw;
        }
    }
}

public partial interface ILapKeHoachService
{
    Task<PlanInput> MoKeHoachMoiAsync();
    Task<long> KhoiTaoKeHoachAsync(PlanInput i);
    Task SuaHoatDongAsync(ActivityInput i);
    Task LuuNhapAsync(CommandInput i);
}

public partial class LapKeHoachService
{
    public Task<PlanInput> MoKeHoachMoiAsync()
    {
        RoleRequired(Traveler);
        return Task.FromResult(new PlanInput());
    }

    public Task<long> KhoiTaoKeHoachAsync(PlanInput i)
    {
        Ensure(i.Id == 0, "0201-01");
        return CapNhatThongTinAsync(i);
    }

    public Task SuaHoatDongAsync(ActivityInput i)
    {
        Require(i.Id > 0, "Chọn hoạt động cần sửa.", 400);
        return ThemHoatDongAsync(i);
    }

    public Task LuuNhapAsync(CommandInput i)
    {
        i.Decision = "Nhap";
        return ChapNhanAsync(i);
    }
}

public partial interface IXuLyTinhHuongService
{
    Task<IncidentInput> MoGhiNhanAsync(long maKeHoach);
    Task GhiPhuongAnAsync(CommandInput i);
    Task<TinhHuongPhatSinh> LayTinhHuongAsync(long maKeHoach, long maTinhHuong);
}

public partial class XuLyTinhHuongService
{
    public async Task<IncidentInput> MoGhiNhanAsync(long maKeHoach)
    {
        await Resolve<ILapKeHoachService>().XemKeHoachAsync(maKeHoach);
        return new()
        {
            PlanId = maKeHoach,
            Type = "ThoiTiet"
        };
    }

    public Task GhiPhuongAnAsync(CommandInput i)
    {
        i.Decision = "PhuongAn";
        return GhiKetQuaAsync(i);
    }

    public async Task<TinhHuongPhatSinh> LayTinhHuongAsync(long maKeHoach, long maTinhHuong)
    {
        await Resolve<ILapKeHoachService>().XemKeHoachAsync(maKeHoach);
        var t = await db.TinhHuongPhatSinh.SingleOrDefaultAsync(x => x.MaTinhHuong == maTinhHuong && x.MaKeHoach == maKeHoach);
        Ensure(t != null, "0501-01");
        return t!;
    }
}

public partial interface ITimNguoiDongHanhService
{
    Task<string[]> MoTimKiemAsync();
}

public partial class TimNguoiDongHanhService
{
    public Task<string[]> MoTimKiemAsync() => Task.FromResult(DanhMuc.HinhThucDi);
}

public partial interface ICongBoNhuCauService
{
    Task<NeedInput> MoBieuMauAsync();
}

public partial class CongBoNhuCauService
{
    public Task<NeedInput> MoBieuMauAsync()
    {
        RoleRequired(Traveler);
        return Task.FromResult(new NeedInput());
    }
}

public partial interface IThoaThuanDongHanhService
{
    Task<ThoaThuanDongHanh> XemDeNghiAsync(long id);
    Task RutAsync(CommandInput i);
    Task DeNghiNgungAsync(CommandInput i);
}

public partial class ThoaThuanDongHanhService
{
    public async Task<ThoaThuanDongHanh> XemDeNghiAsync(long id)
    {
        var t = await Find<ThoaThuanDongHanh>(id);
        await Resolve<IKetNoiDongHanhService>().XemDeNghiAsync(t.MaDeNghi);
        return t;
    }

    public async Task RutAsync(CommandInput i)
    {
        if (i.Id == 0)
        {
            var active = await db.ThoaThuanDongHanh.SingleOrDefaultAsync(x => x.MaDeNghi == i.ConnectionId && x.TrangThai == HoTroDuLich.Web.Domain.TrangThaiThoaThuan.DaXacNhan);
            Ensure(active != null, "0402-10");
            i.Id = active!.MaThoaThuan;
        }

        i.Decision = "Rut";
        await XacNhanAsync(i);
    }

    public Task DeNghiNgungAsync(CommandInput i) => GuiDeNghiThoaThuanAsync(new() { ConnectionId = i.ConnectionId, BaseVersion = i.BaseVersion ?? 0, Kind = "DeNghiNgung" });
}

public partial interface IQuanLyDichVuService
{
    Task<ServiceInput> MoBieuMauAsync(long id = 0, bool chiNhap = true);
    Task CapNhatDichVuAsync(ServiceInput i);
}

public partial class QuanLyDichVuService
{
    public async Task<ServiceInput> MoBieuMauAsync(long id = 0, bool chiNhap = true)
    {
        RoleRequired(ProviderRole);
        if (id == 0)
        {
            Ensure(await KiemTraDieuKienDangAsync(), "0601-01");
            return new()
            {
                Type = "LuuTru"
            };
        }

        var s = await XemDichVuAsync(id);
        if (chiNhap)
        {
            Ensure(await KiemTraDieuKienDangAsync(), "0601-01");
            Ensure(s.TrangThaiCongBo == HoTroDuLich.Web.Domain.TrangThaiCongBo.Nhap, "0601-02");
        }

        var input = new ServiceInput
        {
            Id = id,
            Version = Token(s.RowVersion)
        };
        object value = s;
        var last = (await LayLichSuAsync(id)).FirstOrDefault();
        if (last != null && new HoTroDuLich.Web.Domain.TrangThaiDuyet[]
        {
            HoTroDuLich.Web.Domain.TrangThaiDuyet.CanSua,
            HoTroDuLich.Web.Domain.TrangThaiDuyet.TuChoi
        }.Contains(last.TrangThaiDuyet))
            value = last;
        foreach (var field in ServiceFields)
            typeof(ServiceInput).GetProperty(field.Input)!.SetValue(input, value.GetType().GetProperty(field.Domain)!.GetValue(value));
        input.Type = DanhMuc.ChuanHoa(input.Type);
        input.BranchIds=await Resolve<BranchService>().ServiceBranches(id,value is BanCapNhatDichVu revision?revision.MaBanCapNhat:null);
        return input;
    }

    public Task CapNhatDichVuAsync(ServiceInput i)
    {
        Ensure(i.Id > 0, "0601-02");
        return GuiDangAsync(i);
    }
}

public partial interface IDatDichVuService
{
    Task<List<YeuCauDatDichVu>> DanhSachNCCAsync();
    Task<YeuCauDatDichVu> XemYeuCauAsync(long id);
    Task PhanHoiDeXuatAsync(CommandInput i);
}

public partial class DatDichVuService
{
    public Task<List<YeuCauDatDichVu>> DanhSachNCCAsync()
    {
        RoleRequired(ProviderRole);
        return DanhSachCuaToiAsync();
    }

    public Task<YeuCauDatDichVu> XemYeuCauAsync(long id)
    {
        RoleRequired(ProviderRole);
        return ChiTietAsync(id);
    }

    public Task PhanHoiDeXuatAsync(CommandInput i)
    {
        RoleRequired(Traveler);
        i.Decision = i.Agree == false || i.Decision is "KhongDongY" or "KhachKhongDongY" ? "KhachKhongDongY" : "KhachDongY";
        return ChapNhanAsync(i);
    }
}

public partial interface IDieuChinhDichVuService
{
    Task<YeuCauDatDichVu> MoThayDoiAsync(long id);
    Task<DeNghiDieuChinhDichVu> XemDeNghiAsync(long bookingId, long id);
    Task PhanHoiDeXuatAsync(CommandInput i);
}

public partial class DieuChinhDichVuService
{
    public async Task<YeuCauDatDichVu> MoThayDoiAsync(long id)
    {
        var y = await Resolve<IDatDichVuService>().ChiTietAsync(id);
        Ensure(y.MaTaiKhoan == UserId, "0801-01");
        if (y.TrangThai == HoTroDuLich.Web.Domain.TrangThaiYeuCau.DaXacNhan)
            await CheckChangeable(y);
        else
            Ensure(new HoTroDuLich.Web.Domain.TrangThaiYeuCau[] { HoTroDuLich.Web.Domain.TrangThaiYeuCau.ChoPhanHoi, HoTroDuLich.Web.Domain.TrangThaiYeuCau.DeXuatKhac }.Contains(y.TrangThai), "0801-02");
        return y;
    }

    public async Task<DeNghiDieuChinhDichVu> XemDeNghiAsync(long bookingId, long id)
    {
        var y = await Resolve<IDatDichVuService>().ChiTietAsync(bookingId);
        var s = await Find<DichVuDuLich>(y.MaDichVu);
        var p = await MyProvider();
        Ensure(s.MaNhaCungCap == p.MaNhaCungCap, "0801-01");
        var d = await db.DeNghiDieuChinhDichVu.SingleOrDefaultAsync(x => x.MaDeNghi == id && x.MaYeuCau == bookingId);
        Ensure(d != null, "0801-01");
        return d!;
    }

    public Task PhanHoiDeXuatAsync(CommandInput i)
    {
        RoleRequired(Traveler);
        i.Decision = i.Decision is "Rut" or "DaRut" ? "DaRut" : "KhachDongY";
        return PhanHoiAsync(i);
    }
}

public partial interface IKetQuaDichVuService
{
    Task<YeuCauDatDichVu> MoGhiNhanAsync(long id);
}

public partial class KetQuaDichVuService
{
    public async Task<YeuCauDatDichVu> MoGhiNhanAsync(long id)
    {
        RoleRequired(ProviderRole);
        var y = await Resolve<IDatDichVuService>().ChiTietAsync(id);
        Ensure(new HoTroDuLich.Web.Domain.TrangThaiYeuCau[] { HoTroDuLich.Web.Domain.TrangThaiYeuCau.DaXacNhan, HoTroDuLich.Web.Domain.TrangThaiYeuCau.ChoKiemTra }.Contains(y.TrangThai), "0901-02");
        Ensure(y.NgayBatDau <= HomNay, "0901-03");
        return y;
    }
}

public partial interface IDangKyService
{
    Task<RegisterInput> MoDangKyAsync();
    Task<RegisterInput> ChonVaiTroAsync(string vaiTro);
}

public partial class DangKyService
{
    public Task<RegisterInput> MoDangKyAsync() => Task.FromResult(new RegisterInput());
    public Task<RegisterInput> ChonVaiTroAsync(string vaiTro)
    {
        Ensure(new[] { Traveler, ProviderRole }.Contains(vaiTro), "1001-01");
        return Task.FromResult(new RegisterInput { Role = vaiTro });
    }
}

public partial interface IDangNhapService
{
    Task<LoginInput> MoDangNhapAsync(string? returnUrl);
}

public partial class DangNhapService
{
    public Task<LoginInput> MoDangNhapAsync(string? returnUrl) => Task.FromResult(new LoginInput { ReturnUrl = returnUrl });
}

public partial interface IQuyenHoatDongService
{
    Task KhoiPhucAsync(CommandInput i);
    Task ThuHoiAsync(CommandInput i);
    Task<(NhaCungCapDichVu Provider, List<QuyetDinhQuyenHoatDong> Decisions, List<GiaiTrinhNhaCungCap> Explanations, List<TaiLieuGiaiTrinh> Files)> QuyenCuaToiAsync();
}

public partial class QuyenHoatDongService
{
    public Task KhoiPhucAsync(CommandInput i)
    {
        i.Decision = "DangHoatDong";
        return TamDinhChiAsync(i);
    }

    public Task ThuHoiAsync(CommandInput i)
    {
        i.Decision = "DaThuHoi";
        return TamDinhChiAsync(i);
    }

    public async Task<(NhaCungCapDichVu Provider, List<QuyetDinhQuyenHoatDong> Decisions, List<GiaiTrinhNhaCungCap> Explanations, List<TaiLieuGiaiTrinh> Files)> QuyenCuaToiAsync()
    {
        var p = await MyProvider();
        return await XemChiTietAsync(p.MaNhaCungCap);
    }
}

public partial interface INgungTaiKhoanService
{
    Task<DeactivationInput> MoNgungAsync();
}

public partial class NgungTaiKhoanService
{
    public Task<DeactivationInput> MoNgungAsync()
    {
        RoleRequired(Traveler, ProviderRole);
        return Task.FromResult(new DeactivationInput());
    }
}

