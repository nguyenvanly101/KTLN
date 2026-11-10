using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace HoTroDuLich.Web.Services;

public sealed partial class CompanionService
{
    public async Task<long> SaveGroup(CompanionGroupInput input)
    {
        travel.RoleRequired(TravelService.Traveler);Validate(input);
        travel.Require(input.Kind is "HoatDong" or "ChuyenDi","Loại lời rủ không hợp lệ.",400);
        if(input.Start<clock.HomNay||input.Start>clock.HomNay.AddYears(1))throw new BusinessException("Ngày bắt đầu phải từ hôm nay đến một năm tới.",400,field:"Start");
        if(input.End<input.Start||input.End>clock.HomNay.AddYears(1))throw new BusinessException("Ngày kết thúc phải từ ngày bắt đầu đến một năm tới.",400,field:"End");
        if(input.Kind=="HoatDong"&&input.Start!=input.End)throw new BusinessException("Lời rủ hoạt động dùng một ngày. Chọn chuyến đi nếu đi nhiều ngày.",400,field:"End");
        travel.Require(CompanionLabels.Styles.ContainsKey(input.Style)&&CompanionLabels.Paces.ContainsKey(input.Pace),"Phong cách hoặc nhịp đi không hợp lệ.",400);
        long id=0;
        await travel.Execute(async()=>
        {
            if(input.PlanId!=null){var plan=await travel.OwnPlan(input.PlanId.Value);travel.Require(input.Start>=plan.NgayBatDau&&input.End<=plan.NgayKetThuc,"Ngày nhóm phải nằm trong chuyến đi được gắn.");}
            if(input.ConnectionId!=null)await ConnectionPeople(input.ConnectionId.Value);
            NhomDongHanh group;
            if(input.Id==0){travel.Require(await db.NhomDongHanh.CountAsync(x=>x.MaChuNhom==Me&&(x.TrangThai=="Nhap"||x.TrangThai=="Mo"||x.TrangThai=="DaChot"))<30,"Bạn đã có 30 nhóm chưa đóng. Hãy kết thúc nhóm cũ trước khi tạo thêm.");group=new(){MaChuNhom=Me,TrangThai=input.Publish?"Mo":"Nhap",PhienDiemHen=1,ThoiDiemTao=clock.BayGioUtc};db.Add(group);}
            else{
                group=await Host(input.Id);Version(group,input.Version);travel.Require(!group.AnBoiQuanTri&&group.TrangThai is "Nhap" or "Mo" or "DaChot","Nhóm đã đóng hoặc đang bị ẩn, không thể sửa.");
                travel.Require(group.MaKeHoach==input.PlanId&&group.MaDeNghi==input.ConnectionId,"Liên kết nguồn của nhóm không được đổi sau khi tạo.",400);
                travel.Require(await db.ThanhVienDongHanh.CountAsync(x=>x.MaNhom==group.MaNhom&&x.TrangThai==Joined)<=input.Capacity,"Sức chứa không được ít hơn số người đã tham gia.");
                travel.Require(!await db.HoatDongNhom.AnyAsync(x=>x.MaNhom==group.MaNhom&&!x.DaHuy&&(x.Ngay<input.Start||x.Ngay>input.End)),"Lịch chung đang có hoạt động ngoài khoảng ngày mới. Sửa lịch trước.");
                var endExclusive=TimeZoneInfo.ConvertTimeToUtc(input.End.AddDays(1).ToDateTime(TimeOnly.MinValue),TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
                if(await db.BinhChonDongHanh.AnyAsync(x=>x.MaNhom==group.MaNhom&&x.TrangThai=="Mo"&&x.HanChot>=endExclusive))throw new BusinessException("Có bình chọn còn mở sau ngày kết thúc mới. Chốt hoặc hủy bình chọn trước khi rút ngắn nhóm.",400,field:"End");
                if(group.DiemHen!=Clean(input.Meeting)||group.GioHen!=input.MeetTime||group.NgayBatDau!=input.Start)group.PhienDiemHen++;
                if(group.TrangThai=="Nhap"&&input.Publish)group.TrangThai="Mo";
            }
            group.TenNhom=input.Name.Trim();group.MoTa=input.Description.Trim();group.KhuVuc=input.Region.Trim();group.Loai=input.Kind;group.NgayBatDau=input.Start;group.NgayKetThuc=input.End;group.GioHen=input.MeetTime;group.DiemHen=Clean(input.Meeting);group.SoNguoiToiDa=input.Capacity;group.NganSachThamKhao=input.Budget;group.PhongCach=input.Style;group.NhipDo=input.Pace;group.MaKeHoach=input.PlanId;group.MaDeNghi=input.ConnectionId;group.ThoiDiemCapNhat=clock.BayGioUtc;
            await db.SaveChangesAsync();id=group.MaNhom;
            if(input.Id==0)db.Add(new ThanhVienDongHanh{MaNhom=id,MaTaiKhoan=Me,TrangThai=Joined,DiemDanh="ChuaXacNhan",PhienDiemHen=0,ThamGiaLuc=clock.BayGioUtc,ThoiDiemCapNhat=clock.BayGioUtc});
            else await NotifyMembers(group,"Thông tin hoặc điểm hẹn của nhóm đã cập nhật. Hãy kiểm tra lại.","overview");
            Audit("LuuNhomDongHanh",id,input.Id==0?"Tạo nhóm":"Cập nhật nhóm");
        });return id;
    }
    public async Task GroupState(CompanionCommand input)
    {
        await travel.Execute(async()=>{var g=await Host(input.GroupId);Version(g,input.Version);travel.Require(!g.AnBoiQuanTri||input.Decision is "DaKetThuc" or "DaHuy","Nhóm đang bị ẩn. Bạn vẫn có thể kết thúc hoặc hủy nhóm.");
            var next=input.Decision;travel.Require(next is "Mo" or "DaChot" or "DaKetThuc" or "DaHuy","Trạng thái không hợp lệ.",400);
            travel.Require(g.TrangThai is "Nhap" or "Mo" or "DaChot","Nhóm đã đóng, không thể đổi trạng thái.");
            travel.Require(next!="DaChot"||g.TrangThai=="Mo","Chỉ chốt nhóm đang nhận thành viên.");travel.Require(next!="DaKetThuc"||clock.HomNay>=g.NgayBatDau,"Chỉ kết thúc từ ngày bắt đầu hoạt động.");travel.Require(next!="Mo"||g.NgayKetThuc>=clock.HomNay,"Nhóm đã qua ngày hoạt động.");
            g.TrangThai=next!;Touch(g);if(next is "DaKetThuc" or "DaHuy"){
                foreach(var m in await db.ThanhVienDongHanh.Where(x=>x.MaNhom==g.MaNhom&&(x.TrangThai=="ChoDuyet"||x.TrangThai=="DuocMoi")).ToListAsync()){m.TrangThai="TuChoi";m.PhanHoi="Nhóm đã đóng.";m.ThoiDiemCapNhat=clock.BayGioUtc;travel.Notify(m.MaTaiKhoan,"Nhóm bạn quan tâm đã đóng.","/Together/Mine","DongHanhNhom");}
            }
            await NotifyMembers(g,"Nhóm đã cập nhật: "+CompanionLabels.State(g.TrangThai)+".","overview");Audit("TrangThaiNhom",g.MaNhom,g.TrangThai);
        });
    }
    public async Task Join(CompanionMemberInput input)
    {
        travel.RoleRequired(TravelService.Traveler);Validate(input);
        await travel.Execute(async()=>{var page=await Public(input.GroupId);var g=await db.NhomDongHanh.SingleAsync(x=>x.MaNhom==input.GroupId);Version(g,input.Version);travel.Require(g.MaChuNhom!=Me,"Bạn đã là người tổ chức.");travel.Require(g.TrangThai=="Mo"&&g.NgayKetThuc>=clock.HomNay,"Nhóm hiện không nhận thêm thành viên.");travel.Require(page.Count<g.SoNguoiToiDa,"Nhóm đã đủ người. Hãy chọn lời rủ khác.");
            var m=await db.ThanhVienDongHanh.SingleOrDefaultAsync(x=>x.MaNhom==g.MaNhom&&x.MaTaiKhoan==Me);
            if(m==null){m=new(){MaNhom=g.MaNhom,MaTaiKhoan=Me,DiemDanh="ChuaXacNhan"};db.Add(m);}else{if(m.TrangThai is "ChoDuyet" or Joined)return;travel.Require(m.TrangThai!="DuocMoi","Bạn đã được mời. Mở Nhóm của tôi để trả lời lời mời.");Version(m,input.MemberVersion);}
            m.TrangThai="ChoDuyet";m.LoiNhan=Clean(input.Message);m.PhanHoi=null;m.ThoiDiemCapNhat=clock.BayGioUtc;travel.Notify(g.MaChuNhom,"Có người muốn tham gia nhóm “"+g.TenNhom+"”.",Url(g.MaNhom,"members"),"DongHanhNhom");Audit("XinVaoNhom",g.MaNhom);
        });
    }
    public async Task Invite(CompanionMemberInput input)
    {
        Validate(input);await travel.Execute(async()=>{var g=await Host(input.GroupId,true);travel.Require(g.TrangThai=="Mo","Mở nhận thành viên trước khi mời.");travel.Require((await Connections()).Any(x=>x.Id==input.UserId),"Chỉ mời người đã chấp nhận kết nối và không chặn nhau.",404);travel.Require((await Active(g.MaNhom)).Count<g.SoNguoiToiDa,"Nhóm đã đủ người.");
            var m=await db.ThanhVienDongHanh.SingleOrDefaultAsync(x=>x.MaNhom==g.MaNhom&&x.MaTaiKhoan==input.UserId);
            if(m==null){m=new(){MaNhom=g.MaNhom,MaTaiKhoan=input.UserId,DiemDanh="ChuaXacNhan"};db.Add(m);}else{travel.Require(m.TrangThai is not (Joined or "DuocMoi" or "ChoDuyet"),"Người này đã tham gia, được mời hoặc đang chờ duyệt.");Version(m,input.MemberVersion);}
            m.TrangThai="DuocMoi";m.LoiNhan=Clean(input.Message);m.PhanHoi=null;m.ThoiDiemCapNhat=clock.BayGioUtc;travel.Notify(m.MaTaiKhoan,"Bạn được mời vào nhóm “"+g.TenNhom+"”.","/Together/Mine","DongHanhNhom");Audit("MoiVaoNhom",g.MaNhom);
        });
    }
    private async Task EnsureCanLeave(long groupId,long userId)
    {
        var balances=await Balances(groupId);travel.Require(balances.GetValueOrDefault(userId)==0,"Cần đối chiếu và hoàn tất số dư chi phí trước khi rời nhóm.");
        travel.Require(!await db.HoanTraNhom.AnyAsync(x=>x.MaNhom==groupId&&(x.MaNguoiGui==userId||x.MaNguoiNhan==userId)&&x.TrangThai=="ChoXacNhan"),"Còn khoản hoàn trả đang chờ xác nhận.");
    }
    public async Task MemberAction(CompanionMemberInput input)
    {
        travel.RoleRequired(TravelService.Traveler);Validate(input);
        await travel.Execute(async()=>{
            var g=await db.NhomDongHanh.SingleOrDefaultAsync(x=>x.MaNhom==input.GroupId)??throw new BusinessException("Không tìm thấy nhóm.",404);
            var target=input.UserId==0?Me:input.UserId;
            var decision=input.Decision;travel.Require(decision is "Approve" or "Reject" or "Accept" or "Decline" or "Leave" or "Remove" or "Withdraw","Thao tác không hợp lệ.",400);
            if(decision is "Approve" or "Reject" or "Remove"){await Host(g.MaNhom);travel.Require(target!=Me,"Người tổ chức không thể tự xóa mình.");}
            else travel.Require(target==Me,"Bạn chỉ được trả lời hoặc rút tham gia của mình.",403);
            var m=await db.ThanhVienDongHanh.SingleOrDefaultAsync(x=>x.MaNhom==g.MaNhom&&x.MaTaiKhoan==target)??throw new BusinessException("Không tìm thấy lời mời hoặc thành viên.",404);Version(m,input.Version);
            if(decision is "Approve" or "Accept"){
                travel.Require(IsWritable(g)&&g.TrangThai=="Mo","Nhóm hiện không nhận thành viên.");travel.Require(!await IsBlocked(target==Me?g.MaChuNhom:target),"Không thể kết nối khi hai bên đang chặn nhau.");
                travel.Require(m.TrangThai==(decision=="Approve"?"ChoDuyet":"DuocMoi"),"Lời mời hoặc yêu cầu đã thay đổi.",409);travel.Require(await db.TaiKhoan.AnyAsync(x=>x.MaTaiKhoan==target&&x.TrangThai==TrangThaiTaiKhoan.HoatDong),"Tài khoản người tham gia hiện không hoạt động.");
                travel.Require((await Active(g.MaNhom)).Count<g.SoNguoiToiDa,"Nhóm đã đủ người, không thể xác nhận thêm.");m.TrangThai=Joined;m.ThamGiaLuc=clock.BayGioUtc;m.DiemDanh="ChuaXacNhan";m.PhienDiemHen=0;Touch(g);
            }else if(decision is "Reject" or "Decline"){
                travel.Require(m.TrangThai==(decision=="Reject"?"ChoDuyet":"DuocMoi"),"Lời mời hoặc yêu cầu đã thay đổi.",409);m.TrangThai="TuChoi";
            }else if(decision=="Withdraw"){travel.Require(m.TrangThai=="ChoDuyet","Yêu cầu không còn chờ duyệt.",409);m.TrangThai="DaRut";}
            else {travel.Require(m.TrangThai==Joined&&target!=g.MaChuNhom,"Thành viên không thể rời theo thao tác này.");await EnsureCanLeave(g.MaNhom,target);m.TrangThai=decision=="Remove"?"DaXoa":"DaRut";Touch(g);}
            m.PhanHoi=Clean(input.Message);m.ThoiDiemCapNhat=clock.BayGioUtc;var receiver=target==Me?g.MaChuNhom:target;if(receiver!=Me)travel.Notify(receiver,"Trạng thái tham gia nhóm đã cập nhật: "+CompanionLabels.State(m.TrangThai)+".",m.TrangThai==Joined?Url(g.MaNhom,"members"):"/Together/Mine","DongHanhNhom");Audit("ThanhVienNhom",g.MaNhom,decision);
        });
    }
    public async Task Attendance(CompanionCommand input)
    {
        await travel.Execute(async()=>{var g=await Member(input.GroupId,writable:true);travel.Require(!string.IsNullOrWhiteSpace(g.DiemHen),"Người tổ chức cần thêm điểm hẹn trước.");travel.Require(input.MeetingRevision==g.PhienDiemHen,"Điểm hẹn vừa thay đổi. Mở lại thông tin mới trước khi xác nhận.",409);travel.Require(input.Decision is "SeDen" or "DaDen" or "DenMuon","Trạng thái điểm hẹn không hợp lệ.",400);travel.Require(input.Decision!="DaDen"||g.NgayBatDau<=clock.HomNay,"Chỉ xác nhận đã đến từ ngày bắt đầu.");var m=await db.ThanhVienDongHanh.SingleAsync(x=>x.MaNhom==g.MaNhom&&x.MaTaiKhoan==Me);Version(m,input.Version);m.DiemDanh=input.Decision!;m.PhienDiemHen=g.PhienDiemHen;m.ThoiDiemCapNhat=clock.BayGioUtc;Audit("DiemHenNhom",g.MaNhom,input.Decision);});
    }
    public async Task Block(CompanionMemberInput input)
    {
        travel.RoleRequired(TravelService.Traveler);travel.Require(input.UserId>0&&input.UserId!=Me,"Không thể chặn tài khoản này.",400);
        await travel.Execute(async()=>{var block=await db.ChanDongHanh.SingleOrDefaultAsync(x=>x.MaNguoiChan==Me&&x.MaBiChan==input.UserId);
            if(input.Decision=="Unblock"){travel.Require(block!=null,"Không tìm thấy mục chặn.",404);Version(block!,input.Version);block!.DaChan=false;}
            else{
                travel.Require(input.Decision=="Block","Thao tác không hợp lệ.",400);bool known=(await Connections()).Any(x=>x.Id==input.UserId);
                if(input.GroupId>0){await Member(input.GroupId);known|=await db.ThanhVienDongHanh.AnyAsync(x=>x.MaNhom==input.GroupId&&x.MaTaiKhoan==input.UserId);}
                travel.Require(known||block!=null,"Chỉ chặn người có trong kết nối hoặc nhóm của bạn.",404);
                if(block==null){block=new(){MaNguoiChan=Me,MaBiChan=input.UserId};db.Add(block);}else if(!block.DaChan)Version(block,input.Version);block.DaChan=true;
            }
            block.ThoiDiemCapNhat=clock.BayGioUtc;travel.Audit("ChanDongHanh","TaiKhoan",input.UserId,null,input.Decision);
        });
    }
}
