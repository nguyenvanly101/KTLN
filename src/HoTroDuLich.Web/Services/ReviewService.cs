using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

public sealed class ReviewService(AppDbContext db,TravelService travel,IDongHo clock)
{
    /// <summary>Yeu cau duoc danh gia: cua chinh khach, da dung (co so bam "Da phuc vu") hoac da xac nhan va da qua ngay dung
    /// (nhieu co so khong bam "Da phuc vu"; bo nhac lich cung moi khach danh gia dung cac yeu cau nay).</summary>
    private IQueryable<YeuCauDatDichVu> Eligible(){var today=clock.HomNay;return db.YeuCauDatDichVu.Where(x=>x.MaTaiKhoan==travel.UserId&&(x.TrangThai==TrangThaiYeuCau.HoanThanh||x.TrangThai==TrangThaiYeuCau.ThucHienMotPhan||(x.TrangThai==TrangThaiYeuCau.DaXacNhan&&(x.NgayKetThuc??x.NgayBatDau)<today))&&x.NgayBatDau<=today&&!db.KetQuaCungCapDichVu.Any(r=>r.MaYeuCau==x.MaYeuCau&&r.TrangThaiKiemTra==TrangThaiKiemTra.ChoKiemTra));}
    public async Task<MyReviewsPage> Mine()
    {
        travel.RoleRequired(TravelService.Traveler);var bookings=await Eligible().OrderByDescending(x=>x.NgayBatDau).ThenByDescending(x=>x.MaYeuCau).ToListAsync();
        var ids=bookings.Select(x=>x.MaYeuCau).ToList();var serviceIds=bookings.Select(x=>x.MaDichVu).Distinct().ToList();
        return new(bookings,await db.DichVuDuLich.Where(x=>serviceIds.Contains(x.MaDichVu)).ToDictionaryAsync(x=>x.MaDichVu,x=>x.TenDichVu),await db.DanhGiaDichVu.Where(x=>ids.Contains(x.MaYeuCau)).ToDictionaryAsync(x=>x.MaYeuCau));
    }
    public async Task<ReviewWritePage> Write(long bookingId,ServiceReviewInput? input=null)
    {
        travel.RoleRequired(TravelService.Traveler);var booking=await Eligible().SingleOrDefaultAsync(x=>x.MaYeuCau==bookingId)??throw new BusinessException("Chỉ đánh giá yêu cầu của bạn đã có kết quả, qua ngày phục vụ và không còn đối chiếu đang chờ.",404);
        var entry=await db.DanhGiaDichVu.SingleOrDefaultAsync(x=>x.MaYeuCau==bookingId);
        return new(booking,(await db.DichVuDuLich.SingleAsync(x=>x.MaDichVu==booking.MaDichVu)).TenDichVu,input??(entry==null?new(){BookingId=bookingId}:new(){Id=entry.MaDanhGia,BookingId=bookingId,Version=TravelService.Token(entry.RowVersion),Stars=entry.SoSao,Content=entry.NoiDung}));
    }
    public async Task Save(ServiceReviewInput input)
    {
        travel.RoleRequired(TravelService.Traveler);
        var validation=new List<ValidationResult>();travel.Require(Validator.TryValidateObject(input,new(input),validation,true),validation.FirstOrDefault()?.ErrorMessage??"Kiểm tra đánh giá.",400);
        await travel.Execute(async()=>
        {
            await Write(input.BookingId);var entry=await db.DanhGiaDichVu.SingleOrDefaultAsync(x=>x.MaYeuCau==input.BookingId);string? before=null;
            if(entry==null){travel.Require(input.Id==0,"Không tìm thấy đánh giá.",404);entry=new(){MaYeuCau=input.BookingId,ThoiDiemTao=clock.BayGioUtc};db.Add(entry);}
            else {if(input.Id==0&&entry.SoSao==input.Stars&&entry.NoiDung==input.Content.Trim())return;travel.Require(entry.MaDanhGia==input.Id,"Đánh giá đã tồn tại. Mở bản hiện tại để chỉnh sửa.",409);travel.CheckVersion(entry,input.Version);before=JsonSerializer.Serialize(new{entry.SoSao,entry.NoiDung,entry.TrangThai});}
            travel.Require(input.Content.Trim().Length>0,"Chia sẻ trải nghiệm của bạn.",400);
            // Hien ngay, khong cho quan tri duyet. Neu truoc do bi an, khach sua lai thi hien lai (quan tri van an duoc).
            var isNew=entry.MaDanhGia==0;
            entry.SoSao=input.Stars;entry.NoiDung=input.Content.Trim();entry.TrangThai="DaDuyet";entry.LyDoKiemDuyet=null;entry.ThoiDiemCapNhat=clock.BayGioUtc;
            await db.SaveChangesAsync();
            if(isNew)
            {
             var bk=await db.YeuCauDatDichVu.AsNoTracking().SingleAsync(x=>x.MaYeuCau==entry.MaYeuCau);var svc=await db.DichVuDuLich.AsNoTracking().SingleAsync(x=>x.MaDichVu==bk.MaDichVu);
             var owner=await db.NhaCungCapDichVu.AsNoTracking().Where(x=>x.MaNhaCungCap==svc.MaNhaCungCap).Select(x=>x.MaTaiKhoan).SingleAsync();
             travel.Notify(owner,"Có đánh giá "+entry.SoSao+" sao mới cho "+svc.TenDichVu+". Bạn có thể phản hồi khách.","/Reviews/Partner?serviceId="+svc.MaDichVu,"DanhGia");
            }travel.Audit("GuiDanhGiaDichVu","DanhGiaDichVu",entry.MaDanhGia,before,JsonSerializer.Serialize(new{entry.SoSao,entry.NoiDung,entry.TrangThai}));
        });
    }
    /// <summary>Diem trung binh va so danh gia da duyet cua moi dich vu (mot truy van, nho trong yeu cau hien tai).</summary>
    public async Task<Dictionary<long,(double Average,int Count)>> Ratings()
    {
        var ctx=travel.HttpContextOrNull;
        if(ctx!=null&&ctx.Items.TryGetValue("AllRatings",out var cached)&&cached is Dictionary<long,(double,int)> hit)return hit;
        var rows=await (from review in db.DanhGiaDichVu.AsNoTracking() join booking in db.YeuCauDatDichVu.AsNoTracking() on review.MaYeuCau equals booking.MaYeuCau
            where review.TrangThai=="DaDuyet"&&(booking.TrangThai==TrangThaiYeuCau.HoanThanh||booking.TrangThai==TrangThaiYeuCau.ThucHienMotPhan||booking.TrangThai==TrangThaiYeuCau.DaXacNhan)
            group review by booking.MaDichVu into g select new{Id=g.Key,Avg=g.Average(x=>(double)x.SoSao),N=g.Count()}).ToListAsync();
        var map=rows.ToDictionary(x=>x.Id,x=>(x.Avg,x.N));
        if(ctx!=null)ctx.Items["AllRatings"]=map;
        return map;
    }
    public async Task<ReviewFeed> PublicFeed(long serviceId,int page=1)
    {
        var service=await travel.PublicService(serviceId);travel.Require(page is >=1 and <=100000,"Trang không hợp lệ.",400);
        var query=from review in db.DanhGiaDichVu.AsNoTracking() join booking in db.YeuCauDatDichVu on review.MaYeuCau equals booking.MaYeuCau where booking.MaDichVu==serviceId&&review.TrangThai=="DaDuyet"&&(booking.TrangThai==TrangThaiYeuCau.HoanThanh||booking.TrangThai==TrangThaiYeuCau.ThucHienMotPhan||booking.TrangThai==TrangThaiYeuCau.DaXacNhan)&&!db.KetQuaCungCapDichVu.Any(r=>r.MaYeuCau==booking.MaYeuCau&&r.TrangThaiKiemTra==TrangThaiKiemTra.ChoKiemTra) select review;
        var total=await query.CountAsync();var average=await query.Select(x=>(double?)x.SoSao).AverageAsync();
        // Project only approved reply text. Pending/hidden fields never leave the service.
        var items=await query.OrderByDescending(x=>x.ThoiDiemTao).ThenByDescending(x=>x.MaDanhGia).Skip((page-1)*20).Take(20).Select(x=>new DanhGiaDichVu{MaDanhGia=x.MaDanhGia,SoSao=x.SoSao,NoiDung=x.NoiDung,ThoiDiemTao=x.ThoiDiemTao,TrangThai="DaDuyet",PhanHoiNCC=x.TrangThaiPhanHoi=="DaDuyet"?x.PhanHoiNCC:null,TrangThaiPhanHoi=x.TrangThaiPhanHoi=="DaDuyet"?"DaDuyet":"ChoDuyet"}).ToListAsync();
        return new(service,items,total,average,page);
    }
    public async Task<ReviewManagementPage> Manage(bool admin,int page=1,string status="ChoDuyet",long serviceId=0,long focusId=0)
    {
        travel.RoleRequired(admin?TravelService.AdminRole:TravelService.ProviderRole);travel.Require(page is >=1 and <=100000,"Trang không hợp lệ.",400);travel.Require(new[]{"","ChoDuyet","DaDuyet","An"}.Contains(status),"Bộ lọc không hợp lệ.",400);
        var providerId=admin?0:(await travel.MyProvider()).MaNhaCungCap;
        if(!admin&&serviceId>0)await travel.OwnService(serviceId);
        var query=from review in db.DanhGiaDichVu.AsNoTracking() join booking in db.YeuCauDatDichVu on review.MaYeuCau equals booking.MaYeuCau join service in db.DichVuDuLich on booking.MaDichVu equals service.MaDichVu
            where (focusId==0||review.MaDanhGia==focusId)&&(admin||service.MaNhaCungCap==providerId)&&(serviceId==0||service.MaDichVu==serviceId)&&(status==""||review.TrangThai==status||review.PhanHoiNCC!=null&&review.TrangThaiPhanHoi==status)
            select new {Review=review,ServiceName=service.TenDichVu,ServiceId=service.MaDichVu};
        return new(await query.OrderByDescending(x=>x.Review.ThoiDiemCapNhat).ThenByDescending(x=>x.Review.MaDanhGia).Skip((page-1)*20).Take(20).Select(x=>new ReviewEntry(x.Review,x.ServiceName,x.ServiceId)).ToListAsync(),await query.CountAsync(),page,status,serviceId);
    }
    public async Task Reply(ReviewReplyInput input)
    {
        travel.RoleRequired(TravelService.ProviderRole);travel.Require(!string.IsNullOrWhiteSpace(input.Text)&&input.Text.Length<=2000,"Nhập phản hồi từ 1 đến 2000 ký tự.",400);
        await travel.Execute(async()=>
        {
            var entry=await db.DanhGiaDichVu.SingleOrDefaultAsync(x=>x.MaDanhGia==input.Id)??throw new BusinessException("Không tìm thấy đánh giá.",404);
            var booking=await db.YeuCauDatDichVu.SingleAsync(x=>x.MaYeuCau==entry.MaYeuCau);await travel.OwnService(booking.MaDichVu);travel.CheckVersion(entry,input.Version);
            travel.Require(entry.TrangThai!="An","Đánh giá này đang bị ẩn nên chưa phản hồi được.");
            // Phan hoi hien ngay duoi danh gia; bao cho khach biet.
            var before=entry.PhanHoiNCC;entry.PhanHoiNCC=input.Text.Trim();entry.TrangThaiPhanHoi="DaDuyet";entry.LyDoPhanHoi=null;entry.ThoiDiemCapNhat=clock.BayGioUtc;
            if(before==null){var svcName=await db.DichVuDuLich.AsNoTracking().Where(x=>x.MaDichVu==booking.MaDichVu).Select(x=>x.TenDichVu).SingleAsync();travel.Notify(booking.MaTaiKhoan,"Cơ sở đã phản hồi đánh giá của bạn về "+svcName+".","/Reviews/Service/"+booking.MaDichVu,"DanhGia");}
            travel.Audit("PhanHoiDanhGia","DanhGiaDichVu",entry.MaDanhGia,before,entry.PhanHoiNCC);
        });
    }
    public async Task Moderate(ReviewModerationInput input)
    {
        travel.RoleRequired(TravelService.AdminRole);travel.Require((input.Part is "Review" or "Reply")&&(input.Decision is "DaDuyet" or "An"),"Quyết định không hợp lệ.",400);
        if(input.Decision=="An"&&string.IsNullOrWhiteSpace(input.Reason))throw new BusinessException("Nhập lý do ẩn nội dung.",422,field:"Reason");
        await travel.Execute(async()=>
        {
            var entry=await db.DanhGiaDichVu.SingleOrDefaultAsync(x=>x.MaDanhGia==input.Id)??throw new BusinessException("Không tìm thấy đánh giá.",404);travel.CheckVersion(entry,input.Version);
            var before=JsonSerializer.Serialize(new{entry.TrangThai,entry.TrangThaiPhanHoi,entry.LyDoKiemDuyet,entry.LyDoPhanHoi});
            if(input.Part=="Reply"){travel.Require(entry.PhanHoiNCC!=null,"Chưa có phản hồi để xét duyệt.");entry.TrangThaiPhanHoi=input.Decision;entry.LyDoPhanHoi=input.Reason?.Trim();}
            else {entry.TrangThai=input.Decision;entry.LyDoKiemDuyet=input.Reason?.Trim();}
            entry.ThoiDiemCapNhat=clock.BayGioUtc;travel.Audit("KiemDuyetDanhGia","DanhGiaDichVu",entry.MaDanhGia,before,JsonSerializer.Serialize(new{input.Part,input.Decision,input.Reason}));
            var booking=await db.YeuCauDatDichVu.SingleAsync(x=>x.MaYeuCau==entry.MaYeuCau);
            var svc=await db.DichVuDuLich.AsNoTracking().SingleAsync(x=>x.MaDichVu==booking.MaDichVu);
            var ownerAccount=await db.NhaCungCapDichVu.AsNoTracking().Where(x=>x.MaNhaCungCap==svc.MaNhaCungCap).Select(x=>x.MaTaiKhoan).SingleAsync();
            string Why()=>string.IsNullOrWhiteSpace(input.Reason)?"":" Lý do: "+input.Reason.Trim();
            // Quan tri chi an noi dung khong phu hop (kem ly do) hoac hien lai.
            if(input.Part=="Review")travel.Notify(booking.MaTaiKhoan,input.Decision=="An"?"Đánh giá của bạn về "+svc.TenDichVu+" đã bị ẩn."+Why()+" Bạn có thể sửa lại để hiện lại.":"Đánh giá của bạn về "+svc.TenDichVu+" đã được hiện lại.","/Reviews/Mine","DanhGia");
            else travel.Notify(ownerAccount,input.Decision=="An"?"Phản hồi đánh giá của bạn cho "+svc.TenDichVu+" đã bị ẩn."+Why():"Phản hồi đánh giá của bạn cho "+svc.TenDichVu+" đã được hiện lại.","/Reviews/Partner?serviceId="+svc.MaDichVu,"DanhGia");
        });
    }
}
