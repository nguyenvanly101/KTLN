using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace HoTroDuLich.Web.Services;

public sealed partial class CompanionService
{
    public async Task<CompanionReportPage> ReportForm(long groupId,long? messageId=null,CompanionReportInput? input=null)
    {
        travel.RoleRequired(TravelService.Traveler);var membership=await db.ThanhVienDongHanh.AnyAsync(x=>x.MaNhom==groupId&&x.MaTaiKhoan==Me&&x.TrangThai==Joined);
        var group=membership?await Member(groupId):(await Public(groupId)).Group;string evidence=group.TenNhom+"\n"+group.MoTa;
        if(messageId!=null){travel.Require(membership,"Chỉ thành viên được báo cáo nội dung trao đổi trong nhóm.",404);var blocked=Blocked();var message=await db.TinNhanDongHanh.AsNoTracking().SingleOrDefaultAsync(x=>x.MaTinNhan==messageId&&x.MaNhom==groupId&&!x.DaAn&&!blocked.Contains(x.MaNguoiGui))??throw new BusinessException("Không tìm thấy tin nhắn trong nhóm.",404);evidence=message.NoiDung;}
        return new(input??new(){GroupId=groupId,MessageId=messageId},group.TenNhom,evidence.Length>2000?evidence[..2000]:evidence);
    }
    public async Task SubmitReport(CompanionReportInput input)
    {
        Validate(input);travel.Require(input.Category is "Spam" or "QuayRoi" or "LuaDao" or "Khac","Loại báo cáo không hợp lệ.",400);
        await travel.Execute(async()=>{var page=await ReportForm(input.GroupId,input.MessageId,input);
            if(await db.BaoCaoDongHanh.AnyAsync(x=>x.MaNguoiGui==Me&&x.MaNhom==input.GroupId&&x.MaTinNhan==input.MessageId&&x.TrangThai=="ChoXuLy"))return;
            travel.Require(await db.BaoCaoDongHanh.CountAsync(x=>x.MaNguoiGui==Me&&x.ThoiDiemTao>clock.BayGioUtc.AddDays(-1))<10,"Bạn đã gửi 10 báo cáo trong 24 giờ. Hãy theo dõi các báo cáo đã gửi.",429);
            db.Add(new BaoCaoDongHanh{MaNguoiGui=Me,MaNhom=input.GroupId,MaTinNhan=input.MessageId,Loai=input.Category,MoTa=input.Description.Trim(),BangChung=page.Evidence,TrangThai="ChoXuLy",ThoiDiemTao=clock.BayGioUtc,ThoiDiemCapNhat=clock.BayGioUtc});Audit("BaoCaoDongHanh",input.GroupId,"Báo cáo "+input.Category);
        });
    }
    public async Task<CompanionModerationPage> Reports(bool admin,int page=1,string status="ChoXuLy",long focusId=0)
    {
        travel.RoleRequired(admin?TravelService.AdminRole:TravelService.Traveler);travel.Require(page is >=1 and <=100000&&status is "" or "ChoXuLy" or "DaXuLy","Bộ lọc không hợp lệ.",400);
        var query=from r in db.BaoCaoDongHanh.AsNoTracking() join g in db.NhomDongHanh.AsNoTracking() on r.MaNhom equals g.MaNhom where (admin||r.MaNguoiGui==Me)&&(status==""||r.TrangThai==status)&&(focusId==0||r.MaBaoCao==focusId) select new{Report=r,GroupName=g.TenNhom,GroupVersion=g.RowVersion,Hidden=g.AnBoiQuanTri};
        var rows=await query.OrderByDescending(x=>x.Report.ThoiDiemCapNhat).ThenByDescending(x=>x.Report.MaBaoCao).Skip((page-1)*20).Take(20).ToListAsync();return new(rows.Select(x=>new CompanionModerationRow(x.Report,x.GroupName,TravelService.Token(x.GroupVersion),x.Hidden)).ToList(),await query.CountAsync(),page,status);
    }
    public async Task Moderate(CompanionModerationInput input)
    {
        travel.RoleRequired(TravelService.AdminRole);Validate(input);travel.Require(input.Decision is "KhongViPham" or "AnNoiDung" or "AnNhom" or "DaNhacNho","Quyết định không hợp lệ.",400);
        await travel.Execute(async()=>{var report=await db.BaoCaoDongHanh.SingleOrDefaultAsync(x=>x.MaBaoCao==input.Id)??throw new BusinessException("Không tìm thấy báo cáo.",404);Version(report,input.Version);travel.Require(report.TrangThai=="ChoXuLy","Báo cáo đã được xử lý.",409);var group=await db.NhomDongHanh.SingleAsync(x=>x.MaNhom==report.MaNhom);
            if(input.Decision=="AnNoiDung"){travel.Require(report.MaTinNhan!=null,"Báo cáo này không gắn tin nhắn cụ thể.");var message=await db.TinNhanDongHanh.SingleAsync(x=>x.MaTinNhan==report.MaTinNhan&&x.MaNhom==report.MaNhom);message.DaAn=true;message.MaNguoiAn=Me;}
            if(input.Decision=="AnNhom"){Version(group,input.TargetVersion);group.AnBoiQuanTri=true;group.LyDoAn=input.Reason.Trim();Touch(group);}
            report.TrangThai="DaXuLy";report.QuyetDinh=input.Decision;report.LyDo=input.Reason.Trim();report.MaAdmin=Me;report.ThoiDiemCapNhat=clock.BayGioUtc;
            travel.Notify(report.MaNguoiGui,"Báo cáo đồng hành của bạn đã được xử lý.","/Together/Reports","DongHanhNhom");travel.Notify(group.MaChuNhom,"Nội dung nhóm đã được quản trị xem xét. Kiểm tra thông báo và trạng thái nhóm.",Url(group.MaNhom),"DongHanhNhom");travel.Audit("XuLyBaoCaoDongHanh","BaoCaoDongHanh",report.MaBaoCao,null,input.Decision+": "+input.Reason.Trim());
        });
    }
    public async Task Restore(CompanionModerationInput input)
    {
        travel.RoleRequired(TravelService.AdminRole);Validate(input);await travel.Execute(async()=>{var g=await db.NhomDongHanh.SingleOrDefaultAsync(x=>x.MaNhom==input.GroupId)??throw new BusinessException("Không tìm thấy nhóm.",404);Version(g,input.TargetVersion);travel.Require(g.AnBoiQuanTri,"Nhóm không bị ẩn.",409);g.AnBoiQuanTri=false;g.LyDoAn=null;Touch(g);travel.Notify(g.MaChuNhom,"Nhóm đồng hành đã được quản trị khôi phục hiển thị.",Url(g.MaNhom),"DongHanhNhom");travel.Audit("KhoiPhucNhomDongHanh","NhomDongHanh",g.MaNhom,null,input.Reason.Trim());});
    }
}
