using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace HoTroDuLich.Web.Services;

public sealed partial class CompanionService
{
    public async Task Send(CompanionMessageInput input)
    {
        Validate(input);travel.Require(input.SubmissionId!=Guid.Empty,"Mã lượt gửi không hợp lệ.",400);
        await travel.Execute(async()=>{var g=await Member(input.GroupId,writable:true);travel.Require(!input.Notice||g.MaChuNhom==Me,"Chỉ người tổ chức được ghim thông báo.",403);travel.Require(!string.IsNullOrWhiteSpace(input.Content),"Nhập nội dung tin nhắn.",400);
            var previous=await db.TinNhanDongHanh.SingleOrDefaultAsync(x=>x.MaNhom==g.MaNhom&&x.MaNguoiGui==Me&&x.SubmissionId==input.SubmissionId);if(previous!=null){travel.Require(previous.NoiDung==input.Content.Trim()&&previous.LaThongBao==input.Notice,"Mã lượt gửi đã được dùng cho nội dung khác.",409);return;}
            travel.Require(await db.TinNhanDongHanh.CountAsync(x=>x.MaNhom==g.MaNhom&&x.MaNguoiGui==Me&&x.ThoiDiemTao>clock.BayGioUtc.AddMinutes(-1))<20,"Bạn gửi quá nhanh. Hãy chờ một chút rồi tiếp tục.",429);
            var m=new TinNhanDongHanh{MaNhom=g.MaNhom,MaNguoiGui=Me,NoiDung=input.Content.Trim(),LaThongBao=input.Notice,SubmissionId=input.SubmissionId,ThoiDiemTao=clock.BayGioUtc};db.Add(m);await db.SaveChangesAsync();
            if(input.Notice)await NotifyMembers(g,"Nhóm “"+g.TenNhom+"” có thông báo cần xác nhận đã đọc.","chat");Audit("TinNhanNhom",g.MaNhom,input.Notice?"Thông báo mới":"Tin nhắn mới");
        });
    }
    public async Task ReadNotice(CompanionCommand input)
    {
        await travel.Execute(async()=>{await Member(input.GroupId);var blocked=Blocked();var message=await db.TinNhanDongHanh.SingleOrDefaultAsync(x=>x.MaTinNhan==input.Id&&x.MaNhom==input.GroupId&&!x.DaAn&&x.LaThongBao&&!blocked.Contains(x.MaNguoiGui))??throw new BusinessException("Không tìm thấy thông báo.",404);
            if(!await db.XacNhanThongBaoNhom.AnyAsync(x=>x.MaTinNhan==message.MaTinNhan&&x.MaTaiKhoan==Me))db.Add(new XacNhanThongBaoNhom{MaTinNhan=message.MaTinNhan,MaTaiKhoan=Me,ThoiDiemDoc=clock.BayGioUtc});
        });
    }
    public async Task HideMessage(CompanionCommand input)
    {
        await travel.Execute(async()=>{var g=await Member(input.GroupId);var message=await db.TinNhanDongHanh.SingleOrDefaultAsync(x=>x.MaTinNhan==input.Id&&x.MaNhom==g.MaNhom)??throw new BusinessException("Không tìm thấy tin nhắn.",404);travel.Require(message.MaNguoiGui==Me||g.MaChuNhom==Me,"Chỉ tác giả hoặc người tổ chức được ẩn tin nhắn.",403);Version(message,input.Version);message.DaAn=true;message.MaNguoiAn=Me;Audit("AnTinNhanNhom",g.MaNhom,"Tin nhắn #"+message.MaTinNhan);});
    }
    public async Task CreatePoll(CompanionPollInput input)
    {
        Validate(input);var options=input.Options.Replace("\r","").Split('\n',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);travel.Require(options.Length is >=2 and <=5&&options.All(x=>x.Length<=200)&&options.Distinct(StringComparer.OrdinalIgnoreCase).Count()==options.Length,"Nhập 2–5 lựa chọn khác nhau, mỗi lựa chọn tối đa 200 ký tự.",400);
        var deadline=TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(input.Deadline,DateTimeKind.Unspecified),TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        if(deadline<=clock.BayGioUtc||deadline>clock.BayGioUtc.AddDays(30))throw new BusinessException("Hạn bình chọn phải trong 30 ngày tới.",400,field:"Deadline");
        await travel.Execute(async()=>{var g=await Member(input.GroupId,writable:true);if(DateOnly.FromDateTime(input.Deadline)>g.NgayKetThuc)throw new BusinessException("Hạn bình chọn phải nằm trong thời gian hoạt động của nhóm.",400,field:"Deadline");travel.Require(await db.BinhChonDongHanh.CountAsync(x=>x.MaNhom==g.MaNhom&&x.TrangThai=="Mo"&&x.HanChot>clock.BayGioUtc)<20,"Nhóm đã có 20 bình chọn đang mở. Chốt bình chọn cũ trước.");var poll=new BinhChonDongHanh{MaNhom=g.MaNhom,MaNguoiTao=Me,TieuDe=input.Title.Trim(),HanChot=deadline,TrangThai="Mo",ThoiDiemTao=clock.BayGioUtc,ThoiDiemCapNhat=clock.BayGioUtc};db.Add(poll);await db.SaveChangesAsync();for(var n=0;n<options.Length;n++)db.Add(new LuaChonBinhChon{MaBinhChon=poll.MaBinhChon,ThuTu=n+1,NoiDung=options[n]});await NotifyMembers(g,"Nhóm có bình chọn mới: "+poll.TieuDe,"schedule");Audit("TaoBinhChonNhom",g.MaNhom,"Bình chọn #"+poll.MaBinhChon);});
    }
    public async Task Vote(CompanionVoteInput input)
    {
        await travel.Execute(async()=>{await Member(input.GroupId,writable:true);var poll=await db.BinhChonDongHanh.SingleOrDefaultAsync(x=>x.MaBinhChon==input.Id&&x.MaNhom==input.GroupId)??throw new BusinessException("Không tìm thấy bình chọn.",404);
            travel.Require(poll.TrangThai=="Mo"&&poll.HanChot>clock.BayGioUtc,"Bình chọn đã hết hạn hoặc được chốt.",409);travel.Require(await db.LuaChonBinhChon.AnyAsync(x=>x.MaBinhChon==poll.MaBinhChon&&x.ThuTu==input.Choice),"Lựa chọn không thuộc bình chọn này.",400);
            var vote=await db.PhieuBinhChon.SingleOrDefaultAsync(x=>x.MaBinhChon==poll.MaBinhChon&&x.MaTaiKhoan==Me);
            if(vote==null){vote=new(){MaBinhChon=poll.MaBinhChon,MaTaiKhoan=Me};db.Add(vote);}else Version(vote,input.Version);
            vote.ThuTu=input.Choice;vote.ThoiDiemCapNhat=clock.BayGioUtc;poll.ThoiDiemCapNhat=clock.BayGioUtc;db.Entry(poll).Property(x=>x.ThoiDiemCapNhat).IsModified=true;Audit("BoPhieuNhom",input.GroupId,"Bình chọn #"+poll.MaBinhChon);
        });
    }
    public async Task ClosePoll(CompanionVoteInput input)
    {
        await travel.Execute(async()=>{var g=await Host(input.GroupId,true);var poll=await db.BinhChonDongHanh.SingleOrDefaultAsync(x=>x.MaBinhChon==input.Id&&x.MaNhom==g.MaNhom)??throw new BusinessException("Không tìm thấy bình chọn.",404);Version(poll,input.Version);travel.Require(poll.TrangThai=="Mo","Bình chọn đã được xử lý.",409);
            if(input.Decision=="Cancel"){poll.TrangThai="DaHuy";poll.LuaChonChot=null;}
            else {travel.Require(input.Decision=="Close","Thao tác không hợp lệ.",400);var option=await db.LuaChonBinhChon.SingleOrDefaultAsync(x=>x.MaBinhChon==poll.MaBinhChon&&x.ThuTu==input.Choice)??throw new BusinessException("Chọn kết quả để chốt.",400);
                poll.TrangThai="DaChot";poll.LuaChonChot=input.Choice;
                if(input.AddToSchedule){travel.Require(input.Date>=g.NgayBatDau&&input.Date<=g.NgayKetThuc,"Ngày hoạt động phải nằm trong khoảng ngày của nhóm.");travel.Require(await db.HoatDongNhom.CountAsync(x=>x.MaNhom==g.MaNhom&&!x.DaHuy)<200,"Lịch chung đã có 200 hoạt động.");db.Add(new HoatDongNhom{MaNhom=g.MaNhom,MaNguoiTao=Me,MaBinhChon=poll.MaBinhChon,NoiDung=option.NoiDung,Ngay=input.Date,Gio=input.Time,ThoiDiemCapNhat=clock.BayGioUtc});}
            }
            poll.ThoiDiemCapNhat=clock.BayGioUtc;await NotifyMembers(g,"Bình chọn “"+poll.TieuDe+"” đã được xử lý. Xem quyết định và lịch chung.","schedule");Audit("ChotBinhChonNhom",g.MaNhom,poll.TrangThai);
        });
    }
    public async Task SaveActivity(CompanionActivityInput input)
    {
        Validate(input);await travel.Execute(async()=>{var g=await Host(input.GroupId,true);travel.Require(input.Date>=g.NgayBatDau&&input.Date<=g.NgayKetThuc,"Ngày hoạt động phải nằm trong khoảng ngày của nhóm.");
            if(input.SourceActivityId!=null){travel.Require(input.AgreeToShare&&g.MaKeHoach!=null,"Xác nhận sao chép nội dung/ngày/giờ từ chuyến riêng sang nhóm.");var source=await db.HoatDongLichTrinh.SingleOrDefaultAsync(x=>x.MaHoatDong==input.SourceActivityId&&x.MaKeHoach==g.MaKeHoach)??throw new BusinessException("Không tìm thấy hoạt động nguồn của bạn.",404);await travel.OwnPlan(source.MaKeHoach);input.Content=source.NoiDung;input.Date=source.Ngay;input.Time=source.Gio;travel.Require(input.Date>=g.NgayBatDau&&input.Date<=g.NgayKetThuc,"Hoạt động nguồn nằm ngoài khoảng ngày của nhóm.");}
            HoatDongNhom activity;
            if(input.Id==0){travel.Require(await db.HoatDongNhom.CountAsync(x=>x.MaNhom==g.MaNhom&&!x.DaHuy)<200,"Lịch chung đã có 200 hoạt động.");activity=new(){MaNhom=g.MaNhom,MaNguoiTao=Me};db.Add(activity);}else{activity=await db.HoatDongNhom.SingleOrDefaultAsync(x=>x.MaHoatDong==input.Id&&x.MaNhom==g.MaNhom&&!x.DaHuy)??throw new BusinessException("Không tìm thấy hoạt động.",404);Version(activity,input.Version);}
            travel.Require(!string.IsNullOrWhiteSpace(input.Content)&&input.Content.Trim().Length<=200,"Nội dung hoạt động chung tối đa 200 ký tự.",400);activity.NoiDung=input.Content.Trim();activity.Ngay=input.Date;activity.Gio=input.Time;activity.ThoiDiemCapNhat=clock.BayGioUtc;await NotifyMembers(g,"Lịch chung của nhóm đã cập nhật.","schedule");Audit("LichChungNhom",g.MaNhom);
        });
    }
    public async Task CancelActivity(CompanionCommand input)
    {
        await travel.Execute(async()=>{await Host(input.GroupId,true);var item=await db.HoatDongNhom.SingleOrDefaultAsync(x=>x.MaHoatDong==input.Id&&x.MaNhom==input.GroupId)??throw new BusinessException("Không tìm thấy hoạt động.",404);Version(item,input.Version);item.DaHuy=true;item.ThoiDiemCapNhat=clock.BayGioUtc;Audit("HuyLichChungNhom",input.GroupId);});
    }
    public async Task SaveTask(CompanionTaskInput input)
    {
        Validate(input);await travel.Execute(async()=>{var g=await Member(input.GroupId,writable:true);travel.Require(input.Due==null||input.Due>=clock.HomNay&&input.Due<=g.NgayKetThuc,"Hạn chuẩn bị phải từ hôm nay đến ngày kết thúc nhóm.");travel.Require(input.AssigneeId==null||await db.ThanhVienDongHanh.AnyAsync(x=>x.MaNhom==g.MaNhom&&x.MaTaiKhoan==input.AssigneeId&&x.TrangThai==Joined),"Người phụ trách phải đang tham gia nhóm.");travel.Require(g.MaChuNhom==Me||input.AssigneeId==null||input.AssigneeId==Me,"Chỉ người tổ chức được phân công cho thành viên khác.",403);
            ViecNhom task;if(input.Id==0){travel.Require(await db.ViecNhom.CountAsync(x=>x.MaNhom==g.MaNhom&&!x.DaHuy)<300,"Nhóm đã có 300 việc chuẩn bị.");task=new(){MaNhom=g.MaNhom,MaNguoiTao=Me};db.Add(task);}else{task=await db.ViecNhom.SingleOrDefaultAsync(x=>x.MaViec==input.Id&&x.MaNhom==g.MaNhom&&!x.DaHuy)??throw new BusinessException("Không tìm thấy việc chuẩn bị.",404);travel.Require(g.MaChuNhom==Me||task.MaNguoiTao==Me,"Bạn không được sửa việc chuẩn bị này.",403);Version(task,input.Version);}
            task.NoiDung=input.Title.Trim();task.MaPhuTrach=input.AssigneeId;task.Han=input.Due;task.ThoiDiemCapNhat=clock.BayGioUtc;if(input.AssigneeId!=null&&input.AssigneeId!=Me&&!await IsBlocked(input.AssigneeId.Value))travel.Notify(input.AssigneeId.Value,"Bạn được phân công việc chuẩn bị trong nhóm “"+g.TenNhom+"”.",Url(g.MaNhom,"tasks"),"DongHanhNhom");Audit("ViecChungNhom",g.MaNhom);
        });
    }
    public async Task TaskAction(CompanionCommand input)
    {
        await travel.Execute(async()=>{var g=await Member(input.GroupId,writable:true);var task=await db.ViecNhom.SingleOrDefaultAsync(x=>x.MaViec==input.Id&&x.MaNhom==g.MaNhom&&!x.DaHuy)??throw new BusinessException("Không tìm thấy việc chuẩn bị.",404);travel.Require(g.MaChuNhom==Me||task.MaPhuTrach==Me||task.MaNguoiTao==Me,"Bạn không được cập nhật việc này.",403);Version(task,input.Version);travel.Require(input.Decision is "Done" or "Undo" or "Cancel","Thao tác không hợp lệ.",400);
            if(input.Decision=="Cancel"){travel.Require(g.MaChuNhom==Me||task.MaNguoiTao==Me,"Chỉ người tổ chức hoặc tác giả được hủy việc.",403);task.DaHuy=true;}else task.DaXong=input.Decision=="Done";task.ThoiDiemCapNhat=clock.BayGioUtc;Audit("TrangThaiViecNhom",g.MaNhom,input.Decision);
        });
    }
}
