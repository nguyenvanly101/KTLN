using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

// Journey composition is an extension of the FullText components, not a second booking workflow.
public sealed class TripWorkspaceService(AppDbContext db, TravelService travel)
{
    public async Task<TripWorkspace> Open(long id)
    {
        var plan = await travel.OwnPlan(id);
        var activities = await travel.Activities(id);
        var bookings = await db.YeuCauDatDichVu.AsNoTracking()
            .Where(x => x.MaKeHoach == id && x.MaTaiKhoan == travel.UserId)
            .OrderByDescending(x => x.ThoiDiemGui).ThenByDescending(x => x.MaYeuCau).ToListAsync();
        var serviceIds = bookings.Select(x => x.MaDichVu).Distinct().ToList();
        var names = await db.DichVuDuLich.Where(x => serviceIds.Contains(x.MaDichVu))
            .ToDictionaryAsync(x => x.MaDichVu, x => x.TenDichVu);
        var incidents = await travel.Incidents(id);
        var tasks = new List<TripTask>();
        foreach (var booking in bookings.Where(x => x.TrangThai == TrangThaiYeuCau.DeXuatKhac))
            tasks.Add(new("Có điều kiện dịch vụ cần bạn xem lại", names[booking.MaDichVu],
                "/Bookings/Detail/" + booking.MaYeuCau, "Xem đề xuất", true));
        foreach (var incident in incidents.Where(x => x.TrangThaiXuLy is not (TrangThaiXuLyTinhHuong.DaGiaiQuyet or TrangThaiXuLyTinhHuong.DungXuLy)))
            tasks.Add(new("Tình huống cần xử lý", DanhMuc.Ten(incident.LoaiTinhHuong),
                "/Support/Detail/" + incident.MaTinhHuong, "Tiếp tục xử lý", true));
        foreach (var booking in bookings.Where(x => x.TrangThai is not (TrangThaiYeuCau.DaHuy or TrangThaiYeuCau.TuChoi or TrangThaiYeuCau.KhongTiepTuc)
            && (x.NgayBatDau < plan.NgayBatDau || (x.NgayKetThuc ?? x.NgayBatDau) > plan.NgayKetThuc)))
            tasks.Add(new("Ngày dịch vụ nằm ngoài chuyến đi", names[booking.MaDichVu],
                "/Bookings/Detail/" + booking.MaYeuCau, "Kiểm tra ngày", true));
        if (activities.Count == 0)
            tasks.Add(new("Thêm điểm dừng đầu tiên", "Chọn một nơi muốn đến hoặc tự thêm hoạt động.",
                "/Plans/Activity/" + id, "Thêm hoạt động"));
        if (activities.Where(x => x.Gio != null).GroupBy(x => new { x.Ngay, x.Gio }).Any(x => x.Count() > 1))
            tasks.Add(new("Có hoạt động trùng giờ", "Kiểm tra lại thứ tự và thời gian trong lịch trình.", "#itinerary", "Xem lịch trình", true));
        var total = activities.Sum(x => x.ChiPhiDuKien ?? 0);
        if (plan.NganSachDuKien is decimal budget && total > budget)
            tasks.Add(new("Dự trù đang vượt ngân sách", "Vượt " + Labels.Money(total - budget) + ". Bạn vẫn có thể lưu và điều chỉnh.",
                "#trip-budget", "Xem ngân sách", true));
        if (activities.Count > 0 && plan.TrangThai == TrangThaiKeHoach.Nhap)
            tasks.Add(new("Lịch trình đang là bản nháp", "Kiểm tra các hoạt động rồi xác nhận khi bạn sẵn sàng.", "#plan-decision", "Kiểm tra lịch trình"));
        var activityServices = activities.Where(x => x.MaDichVu != null).Select(x => x.MaDichVu!.Value).Distinct().ToList();
        var requestable = await travel.PublicServices().Where(x => activityServices.Contains(x.MaDichVu)
            && x.TrangThaiTiepNhan == TrangThaiTiepNhan.TiepNhan
            && db.NhaCungCapDichVu.Any(p => p.MaNhaCungCap == x.MaNhaCungCap && p.TrangThaiQuyenHoatDong == TrangThaiQuyenHoatDong.DangHoatDong))
            .Select(x => x.MaDichVu).ToListAsync();
        var preparation=await db.MucChuanBi.Where(x=>x.MaKeHoach==id).ToListAsync();
        var spent=await db.KhoanChiChuyenDi.Where(x=>x.MaKeHoach==id).SumAsync(x=>x.SoTien);
        var overdue=preparation.Count(x=>!x.DaHoanThanh&&x.HanHoanThanh<TravelService.Today);
        if(overdue>0)tasks.Insert(0,new("Có việc chuẩn bị đã quá hạn",overdue+" việc cần bạn kiểm tra.","/Preparation?planId="+id,"Xem danh sách",true));
        if(plan.NganSachDuKien is decimal limit&&spent>limit)tasks.Insert(0,new("Thực chi đã vượt ngân sách",Ui.ActualMoney(spent-limit)+" vượt ngân sách đặt ra.","/Expenses?planId="+id,"Xem khoản chi",true));
        return new(plan, activities, bookings, names, await Locations(activities), requestable.ToHashSet(), incidents, tasks, preparation.Count, preparation.Count(x=>x.DaHoanThanh),spent);
    }

    private async Task<Dictionary<long, TripLocation>> Locations(List<HoatDongLichTrinh> activities)
    {
        var placeIds = activities.Where(x => x.MaDiaDiem != null).Select(x => x.MaDiaDiem!.Value).ToList();
        var serviceIds = activities.Where(x => x.MaDichVu != null).Select(x => x.MaDichVu!.Value).ToList();
        var services = await travel.PublicServices().Where(x => serviceIds.Contains(x.MaDichVu)).ToListAsync();
        placeIds.AddRange(services.Where(x => x.MaDiaDiem != null).Select(x => x.MaDiaDiem!.Value));
        var places = await db.DiaDiemDuLich.Where(x => placeIds.Contains(x.MaDiaDiem) && x.DangHienThi).ToDictionaryAsync(x => x.MaDiaDiem);
        var result = new Dictionary<long, TripLocation>();
        foreach (var activity in activities)
        {
            var service = services.FirstOrDefault(x => x.MaDichVu == activity.MaDichVu);
            var placeId = activity.MaDiaDiem ?? service?.MaDiaDiem;
            if (placeId is long key && places.TryGetValue(key, out var place))
                result[activity.MaHoatDong] = new(Ui.PlaceQuery(place), !string.IsNullOrWhiteSpace(place.DiaChi));
            else if (service != null)
                result[activity.MaHoatDong] = !string.IsNullOrWhiteSpace(service.DiaChiPhucVu) ? new(service.DiaChiPhucVu+", "+service.KhuVucPhucVu+", Việt Nam",true) : new(service.KhuVucPhucVu + ", Việt Nam", false);
        }
        return result;
    }

    public async Task<PlanInput> NewPlan(long? serviceId, long? placeId)
    {
        travel.RoleRequired(TravelService.Traveler);
        await ValidateSelection(serviceId, placeId);
        var input = new PlanInput { ContinueServiceId = serviceId, ContinuePlaceId = placeId };
        if (serviceId is long service)
            input.Destination = (await travel.PublicService(service)).KhuVucPhucVu;
        if (placeId is long place)
            input.Destination = (await travel.Place(place)).KhuVuc;
        if (!string.IsNullOrWhiteSpace(input.Destination)) input.Name = ("Khám phá " + input.Destination)[..Math.Min(150, input.Destination.Length + 9)];
        return input;
    }

    public async Task ValidateSelection(long? serviceId, long? placeId)
    {
        travel.Require(serviceId is null or > 0 && placeId is null or > 0 && (serviceId == null || placeId == null),
            "Vui lòng chọn một dịch vụ hoặc địa điểm hợp lệ.", 400);
        if (serviceId is long service) await travel.PublicService(service);
        if (placeId is long place) await travel.Place(place);
    }

    public static string AfterPlanSaved(long id, PlanInput input) => input.Id == 0 && (input.ContinueServiceId != null || input.ContinuePlaceId != null)
        ? "/Plans/Activity/" + id + (input.ContinueServiceId is long service ? "?serviceId=" + service : "?placeId=" + input.ContinuePlaceId)
        : "/ke-hoach/" + id;

    public async Task<BookingInput> BookingForm(long serviceId, long? planId = null, long? activityId = null)
    {
        var service = await travel.BookableService(serviceId);
        var input = new BookingInput { ServiceId = serviceId, Version = TravelService.Token(service.RowVersion), PlanId = planId };
        travel.Require(activityId == null || planId != null, "Chọn chuyến đi trước khi chọn hoạt động.", 400);
        if (planId is long id)
        {
            var plan = await travel.OwnPlan(id);
            var date = plan.NgayBatDau;
            if (activityId is long activity)
            {
                var item = await db.HoatDongLichTrinh.SingleOrDefaultAsync(x => x.MaHoatDong == activity && x.MaKeHoach == id && x.MaDichVu == serviceId)
                    ?? throw new BusinessException("Hoạt động không thuộc dịch vụ và chuyến đi đã chọn.", 404);
                date = item.Ngay;
            }
            input.Start = date < TravelService.Today ? TravelService.Today : date;
            // A lodging request can span the trip; other experiences start on their activity day.
            if (DanhMuc.ChuanHoa(service.LoaiDichVu) == "LuuTru" && plan.NgayKetThuc >= input.Start)
                input.End = plan.NgayKetThuc;
        }
        return input;
    }

    public async Task AssignBooking(long id, string version, long? planId)
    {
        travel.RoleRequired(TravelService.Traveler);
        await travel.Execute(async () =>
        {
            var booking = await db.YeuCauDatDichVu.SingleOrDefaultAsync(x => x.MaYeuCau == id && x.MaTaiKhoan == travel.UserId)
                ?? throw new BusinessException("Không tìm thấy yêu cầu của bạn.", 404);
            travel.CheckVersion(booking, version);
            if (planId is long plan) await travel.OwnPlan(plan);
            var before = booking.MaKeHoach;
            booking.MaKeHoach = planId;
            travel.Audit("GanYeuCauVaoChuyenDi", "YeuCauDatDichVu", id, before?.ToString(), planId?.ToString());
        });
    }
}
