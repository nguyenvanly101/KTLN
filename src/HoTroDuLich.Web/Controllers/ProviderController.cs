using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoTroDuLich.Web.Controllers;
[Authorize(Roles = TravelService.ProviderRole)]
public class ProviderController(TravelService s,BranchService branches) : AppController(s)
{
    [SucAuthorize(SucAccess.ProviderVerification)]
    [HttpPost]
    public async Task<IActionResult> Reapply(CommandInput i)
    {
        await S.Reapply(i);
        return Done("/Provider/Verification", "Đã gửi hồ sơ xác minh lại. Dịch vụ cũ chưa tự được công bố trở lại.");
    }

    [SucAuthorize(SucAccess.ServiceManagement)]
    public async Task<IActionResult> Index()
    {
        ViewBag.Provider = await S.MyProvider();
        ViewBag.CanSubmit = await S.CanSubmitService();
        return View(await S.MyServices());
    }

    [SucAuthorize(SucAccess.ServiceManagement)]
    [HttpGet]
    public async Task<IActionResult> Edit(long id = 0)
    {
        ViewBag.CanSubmit = await S.CanSubmitService();
        ViewBag.Places = await S.VisiblePlaces();
        if (id == 0)
            return View(new ServiceInput());
        var d = await S.OwnService(id);
        var history = await S.ServiceHistory(id);
        ViewBag.History = history;
        var input = new ServiceInput
        {
            Id = id,
            Version = TravelService.Token(d.RowVersion),
            Address=d.DiaChiPhucVu,PriceUnit=d.DonViGia,CancellationPolicy=d.ChinhSachHuy,PhotoPaths=d.AnhJson,
            Name = d.TenDichVu,
            Type = EnumType(d.LoaiDichVu),
            Region = d.KhuVucPhucVu,
            Description = d.MoTa,
            Price = d.GiaThamKhao,
            Hours = d.ThoiGianPhucVu,
            Terms = d.DieuKienPhucVu,
            PlaceId = d.MaDiaDiem,
            PauseFrom = d.TamNgungTu,
            PauseTo = d.TamNgungDen,
            PauseReason = d.LyDoTamNgung
        };
        var last = history.FirstOrDefault();
        if (last != null && new HoTroDuLich.Web.Domain.TrangThaiDuyet[]
        {
            HoTroDuLich.Web.Domain.TrangThaiDuyet.CanSua,
            HoTroDuLich.Web.Domain.TrangThaiDuyet.TuChoi
        }.Contains(last.TrangThaiDuyet))
        {
            input.Address=last.DiaChiPhucVu;input.PriceUnit=last.DonViGia;input.CancellationPolicy=last.ChinhSachHuy;input.PhotoPaths=last.AnhJson;
            input.Name = last.TenDichVu;
            input.Type = EnumType(last.LoaiDichVu);
            input.Region = last.KhuVucPhucVu;
            input.Description = last.MoTa;
            input.Price = last.GiaThamKhao;
            input.Hours = last.ThoiGianPhucVu;
            input.Terms = last.DieuKienPhucVu;
            input.PlaceId = last.MaDiaDiem;
            input.PauseFrom = last.TamNgungTu;
            input.PauseTo = last.TamNgungDen;
            input.PauseReason = last.LyDoTamNgung;
        }

        input.BranchIds=await branches.ServiceBranches(id,last!=null&&new[]{HoTroDuLich.Web.Domain.TrangThaiDuyet.CanSua,HoTroDuLich.Web.Domain.TrangThaiDuyet.TuChoi}.Contains(last.TrangThaiDuyet)?last.MaBanCapNhat:null);
        return View(input);
    }

    private static string EnumType(string value) => DanhMuc.ChuanHoa(value);
    [SucAuthorize(SucAccess.ServiceManagement)]
    [HttpPost]
    public async Task<IActionResult> Edit(ServiceInput i)
    {
        await S.SaveService(i);
        return Done("/Provider", i.Operation == "LuuNhap" ? "Đã lưu bản nháp dịch vụ." : "Đã gửi phiên bản chờ duyệt. Nội dung đang công bố chỉ thay đổi sau khi Admin chấp thuận.");
    }

    [SucAuthorize(SucAccess.ProviderVerification)]
    public async Task<IActionResult> Verification()
    {
        var p = await S.MyProvider();
        ViewBag.Provider = p;
        var hs = await S.MyVerification();
        var data = new List<(HoTroDuLich.Web.Domain.HoSoXacMinhNhaCungCap Profile, HoTroDuLich.Web.Domain.NhaCungCapDichVu Provider, List<HoTroDuLich.Web.Domain.TaiLieuXacMinh> Files, List<HoTroDuLich.Web.Domain.QuyetDinhXacMinh> Decisions)>();
        foreach (var h in hs)
            data.Add(await S.VerificationDetail(h.MaHoSo));
        return View(data);
    }

    [SucAuthorize(SucAccess.ProviderVerification)]
    [HttpPost]
    public async Task<IActionResult> Supplement(CommandInput i)
    {
        await S.Supplement(i);
        return Done("/Provider/Verification");
    }

    [SucAuthorize(SucAccess.ProviderRights)]
    [HttpPost]
    public async Task<IActionResult> Explain(CommandInput i)
    {
        await S.Explain(i);
        return Done("/Provider/Rights", "Đã gửi giải trình. Quyền hoạt động chỉ thay đổi khi Admin quyết định.");
    }

    [SucAuthorize(SucAccess.ProviderRights)]
    public async Task<IActionResult> Rights()
    {
        var p = await S.MyProvider();
        return View(await S.RightsDetail(p.MaNhaCungCap));
    }
}
