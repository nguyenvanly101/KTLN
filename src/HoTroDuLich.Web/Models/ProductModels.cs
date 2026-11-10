using System.ComponentModel.DataAnnotations;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Services;

namespace HoTroDuLich.Web.Models;

public sealed class SavedInput : CommandInput
{
    [Range(1, long.MaxValue)] public long? ServiceId { get; set; }
    [Range(1, long.MaxValue)] public long? PlaceId { get; set; }
    public string? ReturnUrl { get; set; }
}
public sealed class ChecklistInput : CommandInput
{
    [Range(1, long.MaxValue)] public long PlanId { get; set; }
    [Required(ErrorMessage="Nhập việc cần chuẩn bị."), MaxLength(200,ErrorMessage="Nội dung tối đa 200 ký tự.")] public string Title { get; set; } = "";
    public string Category { get; set; } = "Khac";
    public DateOnly? Due { get; set; }
    public bool Remind { get; set; }
}
public sealed class ExpenseInput : CommandInput
{
    [Range(1, long.MaxValue)] public long PlanId { get; set; }
    [Required(ErrorMessage="Nhập nội dung khoản chi."), MaxLength(200,ErrorMessage="Nội dung tối đa 200 ký tự.")] public string Title { get; set; } = "";
    public string Category { get; set; } = "Khac";
    [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType=typeof(ExpenseAmountBinder))]
    [Range(typeof(decimal), "0.01", "1000000000",ParseLimitsInInvariantCulture=true,ConvertValueInInvariantCulture=true,ErrorMessage="Số tiền phải lớn hơn 0 và không quá 1 tỷ đồng.")] public decimal Amount { get; set; }
    public DateOnly SpentOn { get; set; } = TravelService.Today;
    [MaxLength(500)] public string? Notes { get; set; }
    public Guid SubmissionId { get; set; } = Guid.NewGuid();
}
public sealed record SavedChoice(LuaChonDaLuu Entry, DichVuDuLich? Service, DiaDiemDuLich? Place);
public sealed record PreparationPage(KeHoachChuyenDi Plan, List<MucChuanBi> Items, ChecklistInput Input);
public sealed record ExpensesPage(KeHoachChuyenDi Plan, List<KhoanChiChuyenDi> Items, decimal Estimated, ExpenseInput Input);
public static class ProductCategories
{
    public static readonly Dictionary<string,string> Preparation = new() { ["GiayTo"]="Giấy tờ", ["HanhLy"]="Hành lý", ["DiChuyen"]="Di chuyển", ["DatDichVu"]="Đặt dịch vụ", ["Khac"]="Việc khác" };
    public static readonly Dictionary<string,string> Expenses = new() { ["LuuTru"]="Lưu trú", ["AnUong"]="Ăn uống", ["DiChuyen"]="Di chuyển", ["HoatDong"]="Hoạt động", ["MuaSam"]="Mua sắm", ["Khac"]="Khoản khác" };
}
