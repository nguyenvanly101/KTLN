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
public abstract class NghiepVuService(AppDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment, IConfiguration configuration, IServiceProvider provider)
{
    protected readonly AppDbContext db = context;
    protected readonly IHttpContextAccessor http = accessor;
    protected readonly IWebHostEnvironment env = environment;
    protected readonly IConfiguration config = configuration;
    protected T Resolve<T>()
        where T : notnull => provider.GetRequiredService<T>();
    public const string Traveler = "KHACH_DU_LICH", ProviderRole = "NHA_CUNG_CAP", AdminRole = "QUAN_TRI_VIEN";
    public long UserId => long.TryParse(http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    public string Role => http.HttpContext?.User.FindFirstValue(ClaimTypes.Role) ?? "";
    public static DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "SE Asia Standard Time"));
    protected IDongHo Clock => Resolve<IDongHo>();
    protected DateOnly HomNay => Clock.HomNay;

    public static string Token(byte[] v) => Convert.ToBase64String(v);
    public void Require(bool valid, string message, int status = 422)
    {
        if (!valid)
            throw new BusinessException(message, status);
    }

    public void Ensure(bool valid, string code, string? field = null)
    {
        if (valid)
            return;
        var e = MaLoi.TatCa[code];
        throw new BusinessException(e.Message, e.Status, code, field);
    }

    public void Validate(object input, string code)
    {
        var errors = new List<ValidationResult>();
        Ensure(Validator.TryValidateObject(input, new ValidationContext(input), errors, true), code, errors.FirstOrDefault()?.MemberNames.FirstOrDefault());
    }

    public void RoleRequired(params string[] roles) => Require(UserId > 0 && roles.Contains(Role), "Bạn không có quyền thực hiện thao tác này.", 403);
    public void RequireUseCase(string useCase) => Require(SucAccess.Allows(http.HttpContext?.User ?? new ClaimsPrincipal(), useCase), "Vai trò của bạn không được sử dụng chức năng này.", 403);
    public void CheckVersion(object entity, string version, string? code = null)
    {
        var p = entity.GetType().GetProperty("RowVersion");
        var matches = p != null && Token((byte[])p.GetValue(entity)!) == version;
        if (code != null)
            Ensure(matches, code);
        else
            Require(matches, "Dữ liệu vừa thay đổi ở phiên khác. Vui lòng tải lại.", 409);
    }

    public async Task<T> Find<T>(long id)
        where T : class => await db.Set<T>().FindAsync(id) ?? throw new BusinessException("Không tìm thấy dữ liệu.", 404);
    public void Dates(DateOnly start, DateOnly? end) => Require(start != default && (end == null || end >= start), "Ngày kết thúc phải từ ngày bắt đầu trở đi.", 400);
    public async Task Execute(Func<Task> work)
    {
        try
        {
            await Resolve<IGiaoDich>().ThucHienAsync(work);
            Resolve<ILuuTep>().HoanTat();
        }
        catch
        {
            Resolve<ILuuTep>().XoaBuTru();
            throw;
        }
    }

    protected static int? SqlError(Exception e) => e is SqlException s ? s.Number : e.InnerException == null ? null : SqlError(e.InnerException);
    public Task<NhaCungCapDichVu> MyProvider()=>Resolve<INhaCungCap>().LayNhaCungCapCuaTaiKhoan(UserId);
    public Task<bool> ProviderActive(long id)=>Resolve<INhaCungCap>().DangHoatDong(id);
    public IQueryable<DichVuDuLich> PublicServices() => db.DichVuDuLich.Where(x => x.TrangThaiCongBo == HoTroDuLich.Web.Domain.TrangThaiCongBo.DaDuyet && db.NhaCungCapDichVu.Any(p => p.MaNhaCungCap == x.MaNhaCungCap && p.TrangThaiQuyenHoatDong != HoTroDuLich.Web.Domain.TrangThaiQuyenHoatDong.DaThuHoi) && (!db.Set<DichVuCoSo>().Any(l=>l.ServiceId==x.MaDichVu)||db.Set<DichVuCoSo>().Any(l=>l.ServiceId==x.MaDichVu&&db.Set<CoSoDichVu>().Any(b=>b.Id==l.BranchId&&b.ProviderId==x.MaNhaCungCap&&b.Status=="Approved"&&db.NhaCungCapDichVu.Any(p=>p.MaNhaCungCap==b.ProviderId&&p.TrangThaiQuyenHoatDong==TrangThaiQuyenHoatDong.DangHoatDong&&db.TaiKhoan.Any(a=>a.MaTaiKhoan==p.MaTaiKhoan&&a.TrangThai==TrangThaiTaiKhoan.HoatDong))))));
    protected Task<List<(string Name, string Path, string Type, int Size)>> StoreFiles(List<IFormFile> files) => Resolve<ILuuTep>().LuuAsync(files);
    protected long CompanionViewer => Role == Traveler ? UserId : 0;

    protected async Task EnsureConnectionActive(DeNghiKetNoiDongHanh c)
    {
        var n = await Find<NhuCauDongHanh>(c.MaNhuCau);
        Ensure(await db.TaiKhoan.CountAsync(a => (a.MaTaiKhoan == c.MaNguoiGui || a.MaTaiKhoan == n.MaTaiKhoan) && a.TrangThai == HoTroDuLich.Web.Domain.TrangThaiTaiKhoan.HoatDong) == 2, "0402-05");
    }

    protected IQueryable<NhuCauDongHanh> SharedNeeds(IQueryable<NhuCauDongHanh> query)
    {
        var viewer = CompanionViewer;
        return query.AsNoTracking().Select(n => new NhuCauDongHanh { MaNhuCau = n.MaNhuCau, MaTaiKhoan = n.MaTaiKhoan, DiemDen = n.DiemDen, NgayBatDau = n.NgayBatDau, NgayKetThuc = n.NgayKetThuc, HinhThucDi = n.HinhThucDi, PhamViChiaSe = n.PhamViChiaSe, ThoiDiemDang = n.ThoiDiemDang, TrangThaiCongBo = n.TrangThaiCongBo, NoiDung = n.PhamViChiaSe != HoTroDuLich.Web.Domain.PhamViChiaSe.KhiKetNoi || n.MaTaiKhoan == viewer || db.DeNghiKetNoiDongHanh.Any(c => c.MaNhuCau == n.MaNhuCau && c.MaNguoiGui == viewer && c.TrangThai == HoTroDuLich.Web.Domain.TrangThaiDeNghiKetNoi.DaChapNhan) ? n.NoiDung : "", NganSachDuKien = n.PhamViChiaSe != HoTroDuLich.Web.Domain.PhamViChiaSe.KhiKetNoi || n.MaTaiKhoan == viewer || db.DeNghiKetNoiDongHanh.Any(c => c.MaNhuCau == n.MaNhuCau && c.MaNguoiGui == viewer && c.TrangThai == HoTroDuLich.Web.Domain.TrangThaiDeNghiKetNoi.DaChapNhan) ? n.NganSachDuKien : null, MongMuonDongHanh = n.PhamViChiaSe != HoTroDuLich.Web.Domain.PhamViChiaSe.KhiKetNoi || n.MaTaiKhoan == viewer || db.DeNghiKetNoiDongHanh.Any(c => c.MaNhuCau == n.MaNhuCau && c.MaNguoiGui == viewer && c.TrangThai == HoTroDuLich.Web.Domain.TrangThaiDeNghiKetNoi.DaChapNhan) ? n.MongMuonDongHanh : null });
    }

    protected string? NormalizeSelections(string? value, string code)
    {
        var lines = (value ?? "").Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Ensure((value?.Length ?? 0) <= 1000 && lines.Length <= 10 && lines.All(x => x.Length <= 150) && lines.Distinct(StringComparer.OrdinalIgnoreCase).Count() == lines.Length, code, "Selections");
        return lines.Length == 0 ? null : string.Join('\n', lines);
    }

    protected static string BookingSnapshot(YeuCauDatDichVu y) => JsonSerializer.Serialize(new { y.NgayBatDau, y.NgayKetThuc, y.ThoiGianDenDi, y.SoNguoi, y.SoLuong, y.NoiDungLuaChon, y.ChiPhiDuKien, y.GhiChu, y.DieuKienDeXuat, y.PhienBan });
    protected async Task CheckChangeable(YeuCauDatDichVu y)
    {
        Ensure(y.TrangThai == HoTroDuLich.Web.Domain.TrangThaiYeuCau.DaXacNhan && y.NgayBatDau > HomNay && !await db.KetQuaCungCapDichVu.AnyAsync(x => x.MaYeuCau == y.MaYeuCau), "0801-02");
    }

    protected void ValidateChange(YeuCauDatDichVu y, DateOnly? start, DateOnly? end, int? people, int? quantity, string? terms)
    {
        Ensure((start == null || start >= HomNay) && (end == null || end >= HomNay) && ((end ?? y.NgayKetThuc) == null || (end ?? y.NgayKetThuc) >= (start ?? y.NgayBatDau)), "0801-05");
        Ensure((people == null || people is >= 1 and <= 100) && (quantity == null || quantity is >= 1 and <= 100) && (terms?.Length ?? 0) <= 2000, "0801-05");
    }

    protected static void ApplyChange(YeuCauDatDichVu y, DeNghiDieuChinhDichVu d)
    {
        if (d.LoaiDeNghi == HoTroDuLich.Web.Domain.LoaiDeNghi.Huy)
        {
            y.TrangThai = HoTroDuLich.Web.Domain.TrangThaiYeuCau.DaHuy;
            y.LyDo = d.LyDo;
        }
        else
        {
            y.NgayBatDau = d.NgayBatDauMoi ?? y.NgayBatDau;
            y.NgayKetThuc = d.NgayKetThucMoi ?? y.NgayKetThuc;
            y.SoNguoi = d.SoNguoiMoi ?? y.SoNguoi;
            y.SoLuong = d.SoLuongMoi ?? y.SoLuong;
            y.DieuKienDeXuat = d.DieuKienDeXuat ?? d.DieuKienMoi ?? y.DieuKienDeXuat;
        }

        y.PhienBan++;
    }

    protected async Task AggregateResults(YeuCauDatDichVu y)
    {
        var items = await Resolve<IDatDichVuService>().LayHangMucAsync(y.MaYeuCau);
        var rows = await db.KetQuaCungCapDichVu.Where(x => x.MaYeuCau == y.MaYeuCau).ToListAsync();
        y.TrangThai = System.Enum.Parse<HoTroDuLich.Web.Domain.TrangThaiYeuCau>(items.Any(item => !rows.Any(r => r.HangMuc == item)) || rows.Any(r => r.KetQuaThucHien == HoTroDuLich.Web.Domain.KetQuaThucHien.ChuaXacDinh || r.TrangThaiKiemTra == HoTroDuLich.Web.Domain.TrangThaiKiemTra.ChoKiemTra) ? "ChoKiemTra" : rows.All(r => r.KetQuaThucHien == HoTroDuLich.Web.Domain.KetQuaThucHien.DayDu) ? "HoanThanh" : rows.All(r => r.KetQuaThucHien is HoTroDuLich.Web.Domain.KetQuaThucHien.KhongThucHien or HoTroDuLich.Web.Domain.KetQuaThucHien.KhachKhongDen) ? "KhongThucHien" : "ThucHienMotPhan");
    }

    protected static readonly TaiKhoan FakeAccount = new();
    protected static readonly string FakePasswordHash = new PasswordHasher<TaiKhoan>().HashPassword(FakeAccount, Guid.NewGuid().ToString());
    public static string? NormalizePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var phone = new string (value.Where(c => !char.IsWhiteSpace(c) && c != '-' && c != '.').ToArray());
        if (phone.StartsWith("+84"))
            phone = "0" + phone[3..];
        else if (phone.StartsWith("0084"))
            phone = "0" + phone[4..];
        return phone;
    }

    public static bool ValidPassword(string value, string email) => value.Length is >= 8 and <= 100 && value.Any(char.IsLetter) && value.Any(char.IsDigit) && !value.Contains(email, StringComparison.OrdinalIgnoreCase);
    protected ITimeLimitedDataProtector DeactivationProtector => http.HttpContext!.RequestServices.GetRequiredService<IDataProtectionProvider>().CreateProtector("HoTroDuLich.NgungTaiKhoan.v1").ToTimeLimitedDataProtector();

    protected static readonly (string Domain, string Input)[] ServiceFields = [("DiaChiPhucVu", "Address"), ("DonViGia", "PriceUnit"), ("ChinhSachHuy", "CancellationPolicy"), ("AnhJson", "PhotoPaths"), ("TenDichVu", "Name"), ("LoaiDichVu", "Type"), ("KhuVucPhucVu", "Region"), ("MoTa", "Description"), ("MaDiaDiem", "PlaceId"), ("GiaThamKhao", "Price"), ("ThoiGianPhucVu", "Hours"), ("DieuKienPhucVu", "Terms"), ("TamNgungTu", "PauseFrom"), ("TamNgungDen", "PauseTo"), ("LyDoTamNgung", "PauseReason")];
    protected static void CopyServiceInput(object target, ServiceInput input)
    {
        foreach (var field in ServiceFields)
            target.GetType().GetProperty(field.Domain)!.SetValue(target, typeof(ServiceInput).GetProperty(field.Input)!.GetValue(input));
    }

    protected static bool SameService(object current, ServiceInput input) => ServiceFields.All(field => Equals(field.Domain == "LoaiDichVu" ? DanhMuc.ChuanHoa((string? )current.GetType().GetProperty(field.Domain)!.GetValue(current)) : current.GetType().GetProperty(field.Domain)!.GetValue(current), typeof(ServiceInput).GetProperty(field.Input)!.GetValue(input)));
    protected void CheckRightsOperation(NhaCungCapDichVu p, string decision)
    {
        Ensure(p.TrangThaiXacMinh == HoTroDuLich.Web.Domain.TrangThaiXacMinh.DaXacMinh && ((decision == "DangHoatDong" && p.TrangThaiQuyenHoatDong == HoTroDuLich.Web.Domain.TrangThaiQuyenHoatDong.TamDinhChi) || (decision == "TamDinhChi" && p.TrangThaiQuyenHoatDong == HoTroDuLich.Web.Domain.TrangThaiQuyenHoatDong.DangHoatDong) || (decision == "DaThuHoi" && new HoTroDuLich.Web.Domain.TrangThaiQuyenHoatDong? [] { HoTroDuLich.Web.Domain.TrangThaiQuyenHoatDong.DangHoatDong, HoTroDuLich.Web.Domain.TrangThaiQuyenHoatDong.TamDinhChi }.Contains(p.TrangThaiQuyenHoatDong))), "1301-05");
    }

    protected static string SearchText(string? q) => (q ?? "").Trim()[..Math.Min((q ?? "").Trim().Length, 100)];
    protected void ValidFilter(string status, params string[] allowed) => Require(status == "" || allowed.Contains(status), "Bộ lọc trạng thái không hợp lệ.", 400);
    protected async Task<FilteredPage<T>> PageOf<T>(IQueryable<T> rows, string route, string q, string status, int page)
    {
        Require(page>=1,"Số trang phải từ 1 trở lên.",400);
        var total = await rows.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(total / 20d)));
        return new(await rows.Skip((page - 1) * 20).Take(20).ToListAsync(), new(route, q, status, page, total));
    }
}
