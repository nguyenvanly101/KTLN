using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

public sealed class AvailabilityService(AppDbContext db,TravelService travel,IDongHo clock)
{
    private DateOnly Month(string? month)
    {
        var value=string.IsNullOrWhiteSpace(month)?new DateOnly(clock.HomNay.Year,clock.HomNay.Month,1):DateOnly.TryParseExact(month+"-01","yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out var date)?date:throw new BusinessException("Chọn tháng hợp lệ.",400);
        travel.Require(value>=new DateOnly(clock.HomNay.Year,clock.HomNay.Month,1).AddMonths(-12)&&value<=clock.HomNay.AddYears(2),"Tháng nằm ngoài khoảng được xem.",400);return value;
    }
    public async Task<AvailabilityPage> Page(long serviceId,string? month,bool manage,DateOnly? edit=null,AvailabilityInput? input=null)
    {
        var service=manage?await travel.OwnService(serviceId):await travel.PublicService(serviceId);
        var first=Month(month);var end=first.AddMonths(1);
        var days=await db.LichPhucVuDichVu.AsNoTracking().Where(x=>x.MaDichVu==serviceId&&x.Ngay>=first&&x.Ngay<end).OrderBy(x=>x.Ngay).ToListAsync();
        if(manage&&input==null){var selected=edit??(first.Year==clock.HomNay.Year&&first.Month==clock.HomNay.Month?clock.HomNay:first);var old=await db.LichPhucVuDichVu.SingleOrDefaultAsync(x=>x.MaDichVu==serviceId&&x.Ngay==selected);input=old==null?new(){ServiceId=serviceId,Date=selected}:new(){ServiceId=serviceId,Id=old.MaLich,Version=TravelService.Token(old.RowVersion),Date=selected,IsOpen=old.TiepNhan,Capacity=old.SoLuongThamKhao,Note=old.GhiChu};}
        var counts=new Dictionary<DateOnly,int>();
        if(manage){var bookings=await db.YeuCauDatDichVu.Where(x=>x.MaDichVu==serviceId&&x.TrangThai==TrangThaiYeuCau.DaXacNhan&&x.NgayBatDau<end&&(x.NgayKetThuc??x.NgayBatDau)>=first).ToListAsync();foreach(var day in Enumerable.Range(0,end.DayNumber-first.DayNumber).Select(first.AddDays))counts[day]=bookings.Count(x=>x.NgayBatDau<=day&&LastServiceDay(service,x.NgayBatDau,x.NgayKetThuc)>=day);}
        return new(service,first,days,input,manage,counts);
    }
    public async Task Save(AvailabilityInput input)
    {
        travel.RoleRequired(TravelService.ProviderRole);
        if(input.Date<clock.HomNay||input.Date>clock.HomNay.AddYears(1))throw new BusinessException("Chọn ngày từ hôm nay đến một năm tới.",422,field:"Date");
        travel.Require(input.Capacity==null||input.Capacity is >=1 and <=10000,"Số lượng tham khảo không hợp lệ.",400);
        travel.Require((input.Note?.Length??0)<=300,"Ghi chú tối đa 300 ký tự.",400);
        await travel.Execute(async()=>
        {
            var service=await travel.OwnService(input.ServiceId);travel.Require(await travel.ProviderActive(service.MaNhaCungCap),"Nhà cung cấp cần đang hoạt động để cập nhật lịch.",403);
            var entry=await db.LichPhucVuDichVu.SingleOrDefaultAsync(x=>x.MaDichVu==input.ServiceId&&x.Ngay==input.Date);
            if(input.Id==0){travel.Require(entry==null,"Ngày này vừa được cập nhật. Hãy mở lại bản hiện tại.",409);entry=new(){MaDichVu=input.ServiceId,Ngay=input.Date};db.Add(entry);}
            else {travel.Require(entry!=null&&entry.MaLich==input.Id,"Không tìm thấy ngày phục vụ.",404);travel.CheckVersion(entry!,input.Version);}
            entry!.TiepNhan=input.IsOpen;entry.SoLuongThamKhao=input.IsOpen?input.Capacity:null;entry.GhiChu=input.Note?.Trim();entry.ThoiDiemCapNhat=clock.BayGioUtc;
            await db.SaveChangesAsync();travel.Audit("CapNhatLichPhucVu","LichPhucVuDichVu",entry.MaLich,null,input.Date+" "+(input.IsOpen?"TiepNhan":"TamNgung"));
        });
    }
    public async Task Remove(long serviceId,CommandInput input)
    {
        travel.RoleRequired(TravelService.ProviderRole);await travel.Execute(async()=>
        {
            var service=await travel.OwnService(serviceId);travel.Require(await travel.ProviderActive(service.MaNhaCungCap),"Nhà cung cấp cần đang hoạt động để cập nhật lịch.",403);
            var entry=await db.LichPhucVuDichVu.SingleOrDefaultAsync(x=>x.MaLich==input.Id&&x.MaDichVu==serviceId)??throw new BusinessException("Không tìm thấy ngày phục vụ.",404);
            travel.CheckVersion(entry,input.Version);db.Remove(entry);travel.Audit("BoLichPhucVu","LichPhucVuDichVu",input.Id,null,"Trở về chưa cập nhật");
        });
    }
    public static DateOnly LastServiceDay(DichVuDuLich service,DateOnly start,DateOnly? end)=>DanhMuc.ChuanHoa(service.LoaiDichVu)=="LuuTru"&&end>start?end.Value.AddDays(-1):end??start;
    public async Task CheckNewRequest(DichVuDuLich service,DateOnly start,DateOnly? end)
    {
        var last=LastServiceDay(service,start,end);
        var blocked=await db.LichPhucVuDichVu.AsNoTracking().Where(x=>x.MaDichVu==service.MaDichVu&&!x.TiepNhan&&x.Ngay>=start&&x.Ngay<=last).OrderBy(x=>x.Ngay).FirstOrDefaultAsync();
        if(blocked!=null)throw new BusinessException("Nhà cung cấp đã tạm ngừng nhận yêu cầu ngày "+blocked.Ngay.ToString("dd/MM/yyyy")+". Chọn khoảng ngày khác.",422,field:"Start");
    }
}
