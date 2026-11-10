using System.Globalization;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace HoTroDuLich.Web.Services;

public sealed partial class CompanionService
{
    private async Task<Dictionary<long,decimal>> Balances(long groupId)
    {
        var balances=new Dictionary<long,decimal>();void Add(long user,decimal value)=>balances[user]=balances.GetValueOrDefault(user)+value;
        var costs=await db.KhoanChiNhom.AsNoTracking().Where(x=>x.MaNhom==groupId&&!x.DaHuy).Select(x=>new{x.MaKhoanChi,x.MaNguoiTra,x.SoTien}).ToListAsync();foreach(var c in costs)Add(c.MaNguoiTra,c.SoTien);
        var shares=await(from share in db.PhanBoChiNhom.AsNoTracking() join cost in db.KhoanChiNhom.AsNoTracking() on share.MaKhoanChi equals cost.MaKhoanChi where cost.MaNhom==groupId&&!cost.DaHuy select share).ToListAsync();foreach(var s in shares)Add(s.MaTaiKhoan,-s.SoTien);
        var payments=await db.HoanTraNhom.AsNoTracking().Where(x=>x.MaNhom==groupId&&x.TrangThai=="DaXacNhan").ToListAsync();foreach(var p in payments){Add(p.MaNguoiGui,p.SoTien);Add(p.MaNguoiNhan,-p.SoTien);}return balances;
    }
    private static bool Money(decimal value)=>value>0&&value<=1000000000&&decimal.Round(value,2)==value;
    public async Task AddExpense(CompanionExpenseInput input)
    {
        Validate(input);travel.Require(Money(input.Amount)&&input.SubmissionId!=Guid.Empty,"Số tiền phải lớn hơn 0, tối đa 1 tỷ và có tối đa hai chữ số thập phân.",400);
        await travel.Execute(async()=>{var g=await Member(input.GroupId);travel.Require(!g.AnBoiQuanTri&&g.TrangThai is "Mo" or "DaChot" or "DaKetThuc","Nhóm chưa hoạt động, bị ẩn hoặc đã hủy.");travel.Require(input.PayerId==Me,"Mỗi thành viên ghi khoản tiền do chính mình đã trả.",403);travel.Require(input.Date>=g.NgayBatDau&&input.Date<=g.NgayKetThuc&&input.Date<=clock.HomNay,"Ngày chi phải thuộc chuyến đi và không ở tương lai.");
            var participants=input.Participants.Distinct().Order().ToList();var active=await Active(g.MaNhom);travel.Require(participants.Count is >=1 and <=30&&participants.All(active.Contains),"Chọn từ 1 đến 30 thành viên đang tham gia để chia khoản chi.");
            var allocations=new Dictionary<long,decimal>();
            if(input.Equal){var cents=decimal.ToInt64(input.Amount*100);var each=cents/participants.Count;var rest=cents%participants.Count;travel.Require(each>0,"Số tiền quá nhỏ để chia cho số người đã chọn.");for(var n=0;n<participants.Count;n++)allocations[participants[n]]=(each+(n<rest?1:0))/100m;}
            else{
                travel.Require(input.Shares.Count<=30&&input.Shares.Select(x=>x.UserId).Distinct().Count()==input.Shares.Count,"Danh sách chia tiền không hợp lệ.",400);
                foreach(var user in participants){var text=input.Shares.SingleOrDefault(x=>x.UserId==user)?.Amount;travel.Require(decimal.TryParse(text,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var amount)&&Money(amount),"Nhập số tiền hợp lệ cho từng người được chọn.",400);allocations[user]=amount;}
                travel.Require(allocations.Values.Sum()==input.Amount,"Tổng phần chia phải bằng đúng số tiền đã trả.");
            }
            var previous=await db.KhoanChiNhom.SingleOrDefaultAsync(x=>x.MaNhom==g.MaNhom&&x.MaNguoiTao==Me&&x.SubmissionId==input.SubmissionId);
            if(previous!=null){var old=await db.PhanBoChiNhom.Where(x=>x.MaKhoanChi==previous.MaKhoanChi).ToDictionaryAsync(x=>x.MaTaiKhoan,x=>x.SoTien);travel.Require(previous.NoiDung==input.Title.Trim()&&previous.SoTien==input.Amount&&previous.NgayChi==input.Date&&previous.ChiaDeu==input.Equal&&old.Count==allocations.Count&&old.All(x=>allocations.GetValueOrDefault(x.Key)==x.Value),"Mã lượt ghi đã được dùng cho khoản chi khác.",409);return;}
            var cost=new KhoanChiNhom{MaNhom=g.MaNhom,MaNguoiTao=Me,MaNguoiTra=Me,NoiDung=input.Title.Trim(),SoTien=input.Amount,NgayChi=input.Date,ChiaDeu=input.Equal,SubmissionId=input.SubmissionId,ThoiDiemTao=clock.BayGioUtc,ThoiDiemCapNhat=clock.BayGioUtc};db.Add(cost);await db.SaveChangesAsync();foreach(var a in allocations)db.Add(new PhanBoChiNhom{MaKhoanChi=cost.MaKhoanChi,MaTaiKhoan=a.Key,SoTien=a.Value});await NotifyMembers(g,"Nhóm có khoản chi chung mới. Xem phần chia và số dư của bạn.","expenses");Audit("KhoanChiNhom",g.MaNhom,"Khoản #"+cost.MaKhoanChi);
        });
    }
    public async Task CancelExpense(CompanionCommand input)
    {
        await travel.Execute(async()=>{var g=await Member(input.GroupId);var cost=await db.KhoanChiNhom.SingleOrDefaultAsync(x=>x.MaKhoanChi==input.Id&&x.MaNhom==g.MaNhom)??throw new BusinessException("Không tìm thấy khoản chi.",404);travel.Require(cost.MaNguoiTra==Me,"Chỉ người đã ghi khoản trả của mình được hủy khoản chi.",403);Version(cost,input.Version);travel.Require(!cost.DaHuy,"Khoản chi đã được hủy.",409);var active=await Active(g.MaNhom);travel.Require(!await db.PhanBoChiNhom.AnyAsync(x=>x.MaKhoanChi==cost.MaKhoanChi&&!active.Contains(x.MaTaiKhoan)),"Có người trong khoản chi đã rời nhóm sau đối chiếu. Giữ bản ghi lịch sử để bảo toàn số dư đã chốt.");cost.DaHuy=true;cost.ThoiDiemCapNhat=clock.BayGioUtc;await NotifyMembers(g,"Một khoản chi chung đã được hủy ghi nhận. Kiểm tra lại số dư.","expenses");Audit("HuyKhoanChiNhom",g.MaNhom,"Khoản #"+cost.MaKhoanChi);});
    }
    public async Task ProposePayment(CompanionPaymentInput input)
    {
        Validate(input);travel.Require(Money(input.Amount)&&input.SubmissionId!=Guid.Empty,"Số tiền hoàn trả không hợp lệ.",400);
        await travel.Execute(async()=>{var g=await Member(input.GroupId);travel.Require(input.ReceiverId!=Me&&await db.ThanhVienDongHanh.AnyAsync(x=>x.MaNhom==g.MaNhom&&x.MaTaiKhoan==input.ReceiverId&&x.TrangThai==Joined),"Chọn người nhận đang tham gia nhóm.");
            var previous=await db.HoanTraNhom.SingleOrDefaultAsync(x=>x.MaNhom==g.MaNhom&&x.MaNguoiGui==Me&&x.SubmissionId==input.SubmissionId);if(previous!=null){travel.Require(previous.MaNguoiNhan==input.ReceiverId&&previous.SoTien==input.Amount&&previous.GhiChu==Clean(input.Note),"Mã lượt ghi đã được dùng cho nội dung khác.",409);return;}
            var balances=await Balances(g.MaNhom);var pending=await db.HoanTraNhom.Where(x=>x.MaNhom==g.MaNhom&&x.TrangThai=="ChoXacNhan").ToListAsync();
            travel.Require(!pending.Any(x=>x.MaNguoiGui==Me&&x.MaNguoiNhan==input.ReceiverId),"Hai người đã có khoản hoàn trả chờ xác nhận.");
            travel.Require(input.Amount<=-balances.GetValueOrDefault(Me)-pending.Where(x=>x.MaNguoiGui==Me).Sum(x=>x.SoTien)&&input.Amount<=balances.GetValueOrDefault(input.ReceiverId)-pending.Where(x=>x.MaNguoiNhan==input.ReceiverId).Sum(x=>x.SoTien),"Số tiền vượt số dư có thể đối chiếu. Kiểm tra các khoản đang chờ trước.");
            var payment=new HoanTraNhom{MaNhom=g.MaNhom,MaNguoiGui=Me,MaNguoiNhan=input.ReceiverId,SoTien=input.Amount,TrangThai="ChoXacNhan",GhiChu=Clean(input.Note),SubmissionId=input.SubmissionId,ThoiDiemTao=clock.BayGioUtc,ThoiDiemCapNhat=clock.BayGioUtc};db.Add(payment);await db.SaveChangesAsync();travel.Notify(input.ReceiverId,"Có khoản hoàn trả ngoài ứng dụng cần bạn xác nhận đã nhận.",Url(g.MaNhom,"expenses"),"DongHanhNhom");Audit("DeNghiHoanTraNhom",g.MaNhom,"Khoản #"+payment.MaHoanTra);
        });
    }
    public async Task PaymentAction(CompanionCommand input)
    {
        await travel.Execute(async()=>{var g=await Member(input.GroupId);var p=await db.HoanTraNhom.SingleOrDefaultAsync(x=>x.MaHoanTra==input.Id&&x.MaNhom==g.MaNhom)??throw new BusinessException("Không tìm thấy khoản hoàn trả.",404);travel.Require(input.Decision is "Confirm" or "Reject" or "Cancel","Thao tác không hợp lệ.",400);
            travel.Require(input.Decision=="Cancel"?p.MaNguoiGui==Me:p.MaNguoiNhan==Me,"Bạn không được phản hồi khoản hoàn trả này.",403);Version(p,input.Version);travel.Require(p.TrangThai=="ChoXacNhan","Khoản hoàn trả đã được xử lý.",409);
            if(input.Decision=="Confirm"){var b=await Balances(g.MaNhom);travel.Require(p.SoTien<=-b.GetValueOrDefault(p.MaNguoiGui)&&p.SoTien<=b.GetValueOrDefault(p.MaNguoiNhan),"Số dư đã thay đổi. Hãy từ chối và đối chiếu lại khoản ghi nhận.");p.TrangThai="DaXacNhan";}else p.TrangThai=input.Decision=="Cancel"?"DaHuy":"TuChoi";
            p.ThoiDiemCapNhat=clock.BayGioUtc;travel.Notify(Me==p.MaNguoiGui?p.MaNguoiNhan:p.MaNguoiGui,"Khoản hoàn trả của nhóm đã cập nhật: "+CompanionLabels.State(p.TrangThai)+".",Url(g.MaNhom,"expenses"),"DongHanhNhom");Audit("PhanHoiHoanTraNhom",g.MaNhom,p.TrangThai);
        });
    }
}
