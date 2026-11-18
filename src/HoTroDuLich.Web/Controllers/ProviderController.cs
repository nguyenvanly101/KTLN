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
        ViewBag.Unlinked = await branches.UnlinkedServiceCount();
        var all = await S.MyServices();
        var current = await branches.CurrentBranch();
        if (current == null) return View(all);
        // Chi hien dich vu gan voi co so dang chon, khong hien moi dich vu cua tai khoan.
        var linked = await branches.ServiceIdsOf(current.Id);
        return View(all.Where(x => linked.Contains(x.MaDichVu)).ToList());
    }

    [SucAuthorize(SucAccess.ServiceManagement)]
    [HttpGet]
    public async Task<IActionResult> Edit(long id = 0, long coSo = 0)
    {
        ViewBag.CanSubmit = await S.CanSubmitService();
        if (id == 0)
        {
            // Dich vu moi: tu gan vao co so da duyet (hoac co so duoc chon san), khong bat chon lai.
            var fresh = new ServiceInput();
            var mine = (await branches.Choices()).Select(x => x.Id).ToList();
            var current = await branches.CurrentBranch();
            fresh.BranchIds = coSo > 0 && mine.Contains(coSo) ? [coSo] : current != null && mine.Contains(current.Id) ? [current.Id] : mine.Take(1).ToList();
            return View(fresh);
        }
        var d = await S.OwnService(id);
        var history = await S.ServiceHistory(id);
        ViewBag.History = history;
        var input = new ServiceInput
        {
            Id = id,
            Version = TravelService.Token(d.RowVersion),
            Address=d.DiaChiPhucVu,PriceUnit=d.DonViGia,CancellationPolicy=d.ChinhSachHuy,PhotoPaths=d.AnhJson,AttributesJson=d.AttributesJson,
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
            input.Address=last.DiaChiPhucVu;input.PriceUnit=last.DonViGia;input.CancellationPolicy=last.ChinhSachHuy;input.PhotoPaths=last.AnhJson;input.AttributesJson=last.AttributesJson;
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
        i.AttributesJson = BranchTemplates.Collect(Request.Form, DanhMuc.ChuanHoa(i.Type));
        await S.SaveService(i);
        return Done("/Provider", i.AppliedNow ? "Đã cập nhật ngay: giá, giờ phục vụ, điều kiện và tạm ngừng không cần chờ duyệt." : i.Operation == "LuuNhap" ? "Đã lưu bản nháp dịch vụ." : "Đã gửi phiên bản chờ duyệt. Nội dung đang công bố chỉ thay đổi sau khi Admin chấp thuận.");
    }

    [SucAuthorize(SucAccess.ServiceManagement)]
    [HttpPost]
    public async Task<IActionResult> AttachUnlinked()
    {
        var n = await branches.AttachUnlinkedServices();
        return Done("/Provider", n > 0 ? "Đã gắn " + n + " dịch vụ vào cơ sở đang chọn." : "Không có dịch vụ nào cần gắn.");
    }

    [SucAuthorize(SucAccess.ServiceManagement)]
    [HttpPost]
    public async Task<IActionResult> Delete(long id)
    {
        var removed = await branches.DeleteService(id);
        return Done("/Provider", removed ? "Đã xóa dịch vụ." : "Dịch vụ đã có yêu cầu của khách nên được gỡ khỏi danh mục khách thấy. Bạn có thể gửi lại sau.");
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
