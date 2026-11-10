using System.ComponentModel.DataAnnotations;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
namespace HoTroDuLich.Web.Models;

public class CompanionCommand
{
    public long Id { get; set; }
    public long GroupId { get; set; }
    public string? Version { get; set; }
    public string? Decision { get; set; }
    public string Tab { get; set; } = "overview";
    public int? MeetingRevision { get; set; }
}
public sealed class CompanionProfileInput : CompanionCommand
{
    public string Style { get; set; } = "Khac";
    public string Pace { get; set; } = "CanBang";
    [ModelBinder(BinderType=typeof(CompanionBudgetBinder)),Range(typeof(decimal),"0","1000000000")] public decimal? Budget { get; set; }
    [MaxLength(500,ErrorMessage="Giới thiệu tối đa 500 ký tự.")] public string? Bio { get; set; }
    public bool Visible { get; set; }
}
public sealed class CompanionGroupInput : CompanionCommand
{
    [Required(ErrorMessage="Đặt tên cho lời rủ hoặc chuyến đi."),MaxLength(150)] public string Name { get; set; } = "";
    [Required(ErrorMessage="Giới thiệu nội dung cùng tham gia."),MaxLength(2000)] public string Description { get; set; } = "";
    [Required(ErrorMessage="Nhập khu vực gặp gỡ."),MaxLength(100)] public string Region { get; set; } = "";
    public string Kind { get; set; } = "HoatDong";
    public DateOnly Start { get; set; } = TravelService.Today.AddDays(1);
    public DateOnly End { get; set; } = TravelService.Today.AddDays(1);
    public TimeOnly? MeetTime { get; set; }
    [MaxLength(300)] public string? Meeting { get; set; }
    [Range(2,30,ErrorMessage="Nhóm có từ 2 đến 30 người, gồm người tổ chức.")] public int Capacity { get; set; } = 6;
    [ModelBinder(BinderType=typeof(CompanionBudgetBinder)),Range(typeof(decimal),"0","1000000000")] public decimal? Budget { get; set; }
    public string Style { get; set; } = "Khac";
    public string Pace { get; set; } = "CanBang";
    public long? PlanId { get; set; }
    public long? ConnectionId { get; set; }
    public bool Publish { get; set; }
}
public sealed class CompanionMemberInput : CompanionCommand
{
    public long UserId { get; set; }
    [MaxLength(500)] public string? Message { get; set; }
    public string? MemberVersion { get; set; }
}
public sealed class CompanionMessageInput : CompanionCommand
{
    [Required(ErrorMessage="Nhập nội dung tin nhắn."),MaxLength(2000,ErrorMessage="Tin nhắn tối đa 2000 ký tự.")] public string Content { get; set; } = "";
    public bool Notice { get; set; }
    public Guid SubmissionId { get; set; } = Guid.NewGuid();
}
public sealed class CompanionPollInput : CompanionCommand
{
    [Required(ErrorMessage="Nhập câu hỏi bình chọn."),MaxLength(200)] public string Title { get; set; } = "";
    [Required(ErrorMessage="Nhập từ 2 đến 5 lựa chọn, mỗi dòng một lựa chọn."),MaxLength(1100)] public string Options { get; set; } = "";
    public DateTime Deadline { get; set; } = DateTime.SpecifyKind(DateTime.Today.AddDays(1).AddHours(20),DateTimeKind.Unspecified);
}
public sealed class CompanionVoteInput : CompanionCommand
{
    [Range(1,5)] public int Choice { get; set; }
    public bool AddToSchedule { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? Time { get; set; }
}
public sealed class CompanionActivityInput : CompanionCommand
{
    [Required(ErrorMessage="Nhập hoạt động chung."),MaxLength(200)] public string Content { get; set; } = "";
    public DateOnly Date { get; set; } = TravelService.Today;
    public TimeOnly? Time { get; set; }
    public long? SourceActivityId { get; set; }
    public bool AgreeToShare { get; set; }
}
public sealed class CompanionTaskInput : CompanionCommand
{
    [Required(ErrorMessage="Nhập việc cần chuẩn bị."),MaxLength(200)] public string Title { get; set; } = "";
    public long? AssigneeId { get; set; }
    public DateOnly? Due { get; set; }
}
public sealed class CompanionExpenseInput : CompanionCommand
{
    [Required(ErrorMessage="Nhập nội dung khoản chi."),MaxLength(200)] public string Title { get; set; } = "";
    public long PayerId { get; set; }
    [ModelBinder(BinderType=typeof(ExpenseAmountBinder)),Range(typeof(decimal),"0.01","1000000000",ParseLimitsInInvariantCulture=true,ConvertValueInInvariantCulture=true)] public decimal Amount { get; set; }
    public DateOnly Date { get; set; } = TravelService.Today;
    public bool Equal { get; set; } = true;
    public List<long> Participants { get; set; } = [];
    public List<CompanionShareInput> Shares { get; set; } = [];
    public Guid SubmissionId { get; set; } = Guid.NewGuid();
}
public sealed class CompanionShareInput
{
    public long UserId { get; set; }
    [MaxLength(30)] public string? Amount { get; set; }
}
public sealed class CompanionPaymentInput : CompanionCommand
{
    public long ReceiverId { get; set; }
    [ModelBinder(BinderType=typeof(ExpenseAmountBinder)),Range(typeof(decimal),"0.01","1000000000",ParseLimitsInInvariantCulture=true,ConvertValueInInvariantCulture=true)] public decimal Amount { get; set; }
    [MaxLength(300)] public string? Note { get; set; }
    public Guid SubmissionId { get; set; } = Guid.NewGuid();
}
public sealed class CompanionReportInput : CompanionCommand
{
    public long? MessageId { get; set; }
    public string Category { get; set; } = "Khac";
    [Required(ErrorMessage="Mô tả nội dung cần báo cáo."),MaxLength(2000)] public string Description { get; set; } = "";
}
public sealed class CompanionModerationInput : CompanionCommand
{
    [Required(ErrorMessage="Nhập lý do xử lý."),MaxLength(500)] public string Reason { get; set; } = "";
    public string? TargetVersion { get; set; }
}
public sealed record CompanionCard(NhomDongHanh Group,string Host,int Count,List<string> Reasons);
public sealed record CompanionDiscoverPage(List<CompanionCard> Items,int Total,int Page,string? Region,string? Kind,DateOnly? Start,DateOnly? End,decimal? Budget,bool Matching);
public sealed record CompanionFormPage(CompanionGroupInput Input,List<KeHoachChuyenDi> Plans,List<(long Id,string Name)> Connections);
public sealed record CompanionPublicPage(NhomDongHanh Group,string Host,int Count,ThanhVienDongHanh? Membership,HoSoDongHanh? HostProfile);
public sealed record CompanionSettingsPage(CompanionProfileInput Input,List<ChanDongHanh> Blocks,Dictionary<long,string> Names);
public sealed record CompanionMinePage(List<NhomDongHanh> Groups,Dictionary<long,ThanhVienDongHanh> Memberships,int Page,int Total);
public sealed record CompanionPoll(BinhChonDongHanh Poll,List<LuaChonBinhChon> Options,Dictionary<int,int> Counts,int? MyChoice,string? VoteVersion);
public sealed record CompanionCost(KhoanChiNhom Cost,List<PhanBoChiNhom> Shares);
public sealed class CompanionWorkspace
{
    public required NhomDongHanh Group { get; init; }
    public required ThanhVienDongHanh Me { get; init; }
    public string Tab { get; init; } = "overview";
    public bool Host { get; init; }
    public bool Writable { get; init; }
    public List<ThanhVienDongHanh> Members { get; init; } = [];
    public Dictionary<long,string> Names { get; init; } = [];
    public Dictionary<long,HoSoDongHanh> Profiles { get; init; } = [];
    public Dictionary<long,string> BlockVersions { get; init; } = [];
    public List<TinNhanDongHanh> Messages { get; init; } = [];
    public HashSet<long> ReadNotices { get; init; } = [];
    public List<CompanionPoll> Polls { get; init; } = [];
    public List<HoatDongNhom> Activities { get; init; } = [];
    public List<HoatDongLichTrinh> Importable { get; init; } = [];
    public List<ViecNhom> Tasks { get; init; } = [];
    public List<CompanionCost> Expenses { get; init; } = [];
    public List<HoanTraNhom> Payments { get; init; } = [];
    public Dictionary<long,decimal> Balances { get; init; } = [];
    public List<(long Id,string Name)> Invitees { get; init; } = [];
    public long? ConnectionId { get; init; }
    public int Page { get; init; } = 1;
    public bool HasMore { get; init; }
    public int PaymentPage { get; init; } = 1;
    public bool MorePayments { get; init; }
}
public sealed record CompanionReportPage(CompanionReportInput Input,string GroupName,string Evidence);
public sealed record CompanionModerationRow(BaoCaoDongHanh Report,string GroupName,string GroupVersion,bool Hidden);
public sealed record CompanionModerationPage(List<CompanionModerationRow> Items,int Total,int Page,string Status);
public static class CompanionLabels
{
    public static readonly Dictionary<string,string> Styles = new(){["Khac"]="Linh hoạt",["KhamPha"]="Khám phá",["NghiDuong"]="Nghỉ dưỡng",["AmThuc"]="Ẩm thực",["ThienNhien"]="Thiên nhiên",["VanHoa"]="Văn hóa"};
    public static readonly Dictionary<string,string> Paces = new(){["ThuThai"]="Thư thả",["CanBang"]="Cân bằng",["NangDong"]="Năng động"};
    public static readonly Dictionary<string,string> Tabs = new(){["overview"]="Tổng quan",["members"]="Thành viên",["chat"]="Trao đổi",["schedule"]="Lịch chung",["tasks"]="Chuẩn bị",["expenses"]="Chi phí"};
    public static string State(string value)=>value switch{"Nhap"=>"Bản nháp","Mo"=>"Đang nhận thành viên","DaChot"=>"Đã chốt nhóm","DaKetThuc"=>"Đã kết thúc","DaHuy"=>"Đã hủy","ChoDuyet"=>"Chờ duyệt","DuocMoi"=>"Được mời","DaThamGia"=>"Đã tham gia","TuChoi"=>"Đã từ chối","DaRut"=>"Đã rút","DaXoa"=>"Đã rời nhóm","ChuaXacNhan"=>"Chưa xác nhận","SeDen"=>"Sẽ đến","DaDen"=>"Đã đến","DenMuon"=>"Đến muộn","ChoXacNhan"=>"Chờ người nhận xác nhận","DaXacNhan"=>"Đã xác nhận","ChoXuLy"=>"Chờ xử lý","DaXuLy"=>"Đã xử lý",_=>value};
}
