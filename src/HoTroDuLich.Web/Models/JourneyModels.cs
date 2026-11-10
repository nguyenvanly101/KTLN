using System.ComponentModel.DataAnnotations;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Services;

namespace HoTroDuLich.Web.Models;

public sealed record ScheduleWarning(long ActivityId,string Message);
public sealed record TripDayPage(TripWorkspace Trip,DateOnly Day,List<HoatDongLichTrinh> Items,List<ScheduleWarning> Warnings);
public sealed class ShareTripInput : CommandInput
{
    [Range(1,long.MaxValue)] public long PlanId { get; set; }
    [Range(1,30,ErrorMessage="Liên kết có hiệu lực từ 1 đến 30 ngày.")] public int Days { get; set; } = 7;
    public bool IncludeBudget { get; set; }
    public bool IncludeServices { get; set; }
    public bool AgreeToShare { get; set; }
}
public sealed record ShareTripPage(KeHoachChuyenDi Plan,List<ChiaSeChuyenDi> Links,ShareTripInput Input);
public sealed record SharedActivity(DateOnly Date,string? Time,string Content,int? Duration,int? Transfer);
public sealed record SharedBooking(string Service,DateOnly Start,DateOnly? End,string Status);
public sealed record SharedTrip(string Name,string Destination,DateOnly Start,DateOnly End,List<SharedActivity> Activities,bool IncludeBudget,decimal? Budget,bool IncludeServices,List<SharedBooking> Services,DateTime Created);

public sealed class ServiceReviewInput : CommandInput
{
    [Range(1,long.MaxValue)] public long BookingId { get; set; }
    [Range(1,5,ErrorMessage="Chọn từ 1 đến 5 sao.")] public int Stars { get; set; } = 5;
    [Required(ErrorMessage="Chia sẻ trải nghiệm của bạn."),MaxLength(2000,ErrorMessage="Đánh giá tối đa 2000 ký tự.")] public string Content { get; set; } = "";
}
public sealed class ReviewReplyInput : CommandInput
{
    public long ServiceId { get; set; }
    [Required(ErrorMessage="Nhập phản hồi của bạn."),MaxLength(2000,ErrorMessage="Phản hồi tối đa 2000 ký tự.")]
    public new string Text { get; set; } = "";
}
public sealed class ReviewModerationInput : CommandInput
{
    public string Part { get; set; } = "Review";
    [MaxLength(500,ErrorMessage="Lý do tối đa 500 ký tự.")] public new string? Reason { get; set; }
}
public sealed record ReviewEntry(DanhGiaDichVu Review,string ServiceName,long ServiceId);
public sealed record ReviewFeed(DichVuDuLich Service,List<DanhGiaDichVu> Items,int Total,double? Average,int Page);
public sealed record ReviewWritePage(YeuCauDatDichVu Booking,string ServiceName,ServiceReviewInput Input);
public sealed record MyReviewsPage(List<YeuCauDatDichVu> Bookings,Dictionary<long,string> Names,Dictionary<long,DanhGiaDichVu> Reviews);
public sealed record ReviewManagementPage(List<ReviewEntry> Items,int Total,int Page,string Status,long ServiceId=0);

public sealed class AvailabilityInput : CommandInput
{
    [Range(1,long.MaxValue)] public long ServiceId { get; set; }
    public DateOnly Date { get; set; } = TravelService.Today;
    public bool IsOpen { get; set; } = true;
    [Range(1,10000,ErrorMessage="Số lượng tham khảo từ 1 đến 10.000.")] public int? Capacity { get; set; }
    [MaxLength(300,ErrorMessage="Ghi chú tối đa 300 ký tự.")] public string? Note { get; set; }
}
public sealed record AvailabilityPage(DichVuDuLich Service,DateOnly Month,List<LichPhucVuDichVu> Days,AvailabilityInput? Input,bool Manage,Dictionary<DateOnly,int> Confirmed);

public static class ReviewStates
{
    public static string Label(string value)=>value switch {"ChoDuyet"=>"Chờ duyệt","DaDuyet"=>"Đã công bố","An"=>"Đã ẩn",_=>value};
}
