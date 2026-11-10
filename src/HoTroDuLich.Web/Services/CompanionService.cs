using System.ComponentModel.DataAnnotations;
using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace HoTroDuLich.Web.Services;

public sealed partial class CompanionService(AppDbContext db,TravelService travel,IDongHo clock)
{
    private const string Joined="DaThamGia";
    private long Me=>travel.UserId;
    private static string? Clean(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
    private static string Url(long id,string tab="overview")=>"/Together/Group/"+id+"?tab="+tab;
    private void Validate(object input)
    {
        var errors=new List<ValidationResult>();
        if(!Validator.TryValidateObject(input,new(input),errors,true)){var error=errors[0];throw new BusinessException(error.ErrorMessage??"Kiểm tra thông tin đã nhập.",400,field:error.MemberNames.FirstOrDefault());}
    }
    private void Version(object item,string? version)=>travel.CheckVersion(item,version??"");
    private IQueryable<long> Blocked()=>db.ChanDongHanh.Where(x=>x.DaChan&&(x.MaNguoiChan==Me||x.MaBiChan==Me)).Select(x=>x.MaNguoiChan==Me?x.MaBiChan:x.MaNguoiChan);
    private async Task<bool> IsBlocked(long other)=>await db.ChanDongHanh.AnyAsync(x=>x.DaChan&&(x.MaNguoiChan==Me&&x.MaBiChan==other||x.MaNguoiChan==other&&x.MaBiChan==Me));
    private void Touch(NhomDongHanh group){group.ThoiDiemCapNhat=clock.BayGioUtc;db.Entry(group).Property(x=>x.ThoiDiemCapNhat).IsModified=true;}
    private bool IsWritable(NhomDongHanh group)=>!group.AnBoiQuanTri&&(group.TrangThai is "Mo" or "DaChot")&&group.NgayKetThuc>=clock.HomNay;
    private async Task<NhomDongHanh> Member(long id,bool host=false,bool writable=false)
    {
        travel.RoleRequired(TravelService.Traveler);
        var group=await db.NhomDongHanh.SingleOrDefaultAsync(x=>x.MaNhom==id)??throw new BusinessException("Không tìm thấy nhóm đồng hành.",404);
        travel.Require(await db.ThanhVienDongHanh.AnyAsync(x=>x.MaNhom==id&&x.MaTaiKhoan==Me&&x.TrangThai==Joined),"Bạn chưa là thành viên của nhóm này.",404);
        travel.Require(!host||group.MaChuNhom==Me,"Chỉ người tổ chức được thực hiện thao tác này.",403);
        if(writable){travel.Require(IsWritable(group),"Nhóm đã đóng, hết ngày hoạt động hoặc đang bị ẩn. Bạn vẫn có thể xem lịch sử.");travel.Require(await db.TaiKhoan.AnyAsync(x=>x.MaTaiKhoan==group.MaChuNhom&&x.TrangThai==TrangThaiTaiKhoan.HoatDong),"Người tổ chức hiện không hoạt động.");}
        return group;
    }
    private async Task<NhomDongHanh> Host(long id,bool writable=false)=>await Member(id,true,writable);
    private async Task<List<long>> Active(long groupId)=>await db.ThanhVienDongHanh.Where(x=>x.MaNhom==groupId&&x.TrangThai==Joined).Select(x=>x.MaTaiKhoan).ToListAsync();
    private async Task<Dictionary<long,string>> Names(IEnumerable<long> ids){var wanted=ids.Distinct().ToList();return await db.HoSoNguoiDung.Where(x=>wanted.Contains(x.MaTaiKhoan)).ToDictionaryAsync(x=>x.MaTaiKhoan,x=>x.TenHienThi);}
    private async Task NotifyMembers(NhomDongHanh group,string message,string tab)
    {
        var blocked=await Blocked().ToListAsync();foreach(var id in await Active(group.MaNhom))if(id!=Me&&!blocked.Contains(id))travel.Notify(id,message,Url(group.MaNhom,tab),"DongHanhNhom");
    }
    public async Task<object> Thread(long groupId,long after=0)
    {
        await Member(groupId);travel.Require(after>=0,"Mốc tin nhắn không hợp lệ.",400);var blocked=Blocked();var query=db.TinNhanDongHanh.AsNoTracking().Where(x=>x.MaNhom==groupId&&!x.DaAn&&!blocked.Contains(x.MaNguoiGui)&&x.MaTinNhan>after);return new{newCount=await query.CountAsync()};
    }
    public async Task<List<AccountObligation>> AccountObligations()
    {
        travel.RoleRequired(TravelService.Traveler);var groups=await(from m in db.ThanhVienDongHanh join g in db.NhomDongHanh on m.MaNhom equals g.MaNhom where m.MaTaiKhoan==Me&&m.TrangThai==Joined select g).ToListAsync();var result=new List<AccountObligation>();
        foreach(var g in groups){if(g.TrangThai is "Nhap" or "Mo" or "DaChot")result.Add(new("Nhóm đồng hành đang hoạt động: "+g.TenNhom,Url(g.MaNhom)));else if((await Balances(g.MaNhom)).GetValueOrDefault(Me)!=0||await db.HoanTraNhom.AnyAsync(x=>x.MaNhom==g.MaNhom&&x.TrangThai=="ChoXacNhan"&&(x.MaNguoiGui==Me||x.MaNguoiNhan==Me)))result.Add(new("Đối chiếu chi phí nhóm: "+g.TenNhom,Url(g.MaNhom,"expenses")));}return result;
    }
    private void Audit(string action,long id,string? detail=null)=>travel.Audit(action,"NhomDongHanh",id,null,detail);
    public async Task<List<(long Id,string Name)>> Connections()
    {
        travel.RoleRequired(TravelService.Traveler);var blocked=Blocked();
        var people=await(from c in db.DeNghiKetNoiDongHanh join n in db.NhuCauDongHanh on c.MaNhuCau equals n.MaNhuCau
            where c.TrangThai==TrangThaiDeNghiKetNoi.DaChapNhan&&(c.MaNguoiGui==Me||n.MaTaiKhoan==Me)
            select new {Id=c.MaNguoiGui==Me?n.MaTaiKhoan:c.MaNguoiGui}).Distinct().ToListAsync();
        var ids=people.Select(x=>x.Id).ToList();var names=await(from a in db.TaiKhoan join p in db.HoSoNguoiDung on a.MaTaiKhoan equals p.MaTaiKhoan where ids.Contains(a.MaTaiKhoan)&&a.TrangThai==TrangThaiTaiKhoan.HoatDong&&!blocked.Contains(a.MaTaiKhoan) orderby p.TenHienThi select new{Id=a.MaTaiKhoan,Name=p.TenHienThi}).ToListAsync();
        return names.Select(x=>(x.Id,x.Name)).ToList();
    }
    public async Task<CompanionSettingsPage> Settings(CompanionProfileInput? input=null)
    {
        travel.RoleRequired(TravelService.Traveler);var p=await db.HoSoDongHanh.SingleOrDefaultAsync(x=>x.MaTaiKhoan==Me);
        var blocks=await db.ChanDongHanh.Where(x=>x.MaNguoiChan==Me&&x.DaChan).OrderByDescending(x=>x.ThoiDiemCapNhat).ToListAsync();
        return new(input??(p==null?new():new(){Style=p.PhongCach,Pace=p.NhipDo,Budget=p.NganSachToiDa,Bio=p.GioiThieu,Visible=p.HienThi,Version=TravelService.Token(p.RowVersion)}),blocks,await Names(blocks.Select(x=>x.MaBiChan)));
    }
    public async Task SaveProfile(CompanionProfileInput input)
    {
        travel.RoleRequired(TravelService.Traveler);Validate(input);travel.Require(CompanionLabels.Styles.ContainsKey(input.Style)&&CompanionLabels.Paces.ContainsKey(input.Pace),"Phong cách hoặc nhịp đi không hợp lệ.",400);
        await travel.Execute(async()=>{var p=await db.HoSoDongHanh.SingleOrDefaultAsync(x=>x.MaTaiKhoan==Me);if(p==null){p=new(){MaTaiKhoan=Me};db.Add(p);}else Version(p,input.Version);p.PhongCach=input.Style;p.NhipDo=input.Pace;p.NganSachToiDa=input.Budget;p.GioiThieu=Clean(input.Bio);p.HienThi=input.Visible;p.ThoiDiemCapNhat=clock.BayGioUtc;travel.Audit("HoSoDongHanh","TaiKhoan",Me,null,"Cập nhật tùy chọn đồng hành");});
    }
    private IQueryable<NhomDongHanh> PublicGroups()=>db.NhomDongHanh.AsNoTracking().Where(g=>!g.AnBoiQuanTri&&(g.TrangThai=="Mo"||g.TrangThai=="DaChot")&&g.NgayKetThuc>=clock.HomNay&&db.TaiKhoan.Any(a=>a.MaTaiKhoan==g.MaChuNhom&&a.TrangThai==TrangThaiTaiKhoan.HoatDong));
    public async Task<CompanionDiscoverPage> Discover(string? region,string? kind,DateOnly? start,DateOnly? end,decimal? budget,int page=1,bool matching=false)
    {
        travel.Require(page is >=1 and <=100000&&(region?.Length??0)<=100&&(kind==null||kind is "" or "HoatDong" or "ChuyenDi")&&(start==null||end==null||start<=end)&&(budget==null||budget is >=0 and <=1000000000),"Kiểm tra tiêu chí tìm đồng hành.",400);
        var query=PublicGroups();if(Me>0){var blocked=Blocked();query=query.Where(g=>!blocked.Contains(g.MaChuNhom));}
        if(Clean(region) is string area)query=query.Where(x=>x.KhuVuc.Contains(area));if(!string.IsNullOrEmpty(kind))query=query.Where(x=>x.Loai==kind);if(start!=null)query=query.Where(x=>x.NgayKetThuc>=start);if(end!=null)query=query.Where(x=>x.NgayBatDau<=end);if(budget!=null)query=query.Where(x=>x.NganSachThamKhao!=null&&x.NganSachThamKhao<=budget);
        var prefs=travel.Role==TravelService.Traveler?await db.HoSoDongHanh.AsNoTracking().SingleOrDefaultAsync(x=>x.MaTaiKhoan==Me):null;
        var ordered=matching&&prefs!=null?query.OrderByDescending(g=>(g.PhongCach==prefs.PhongCach?2:0)+(g.NhipDo==prefs.NhipDo?1:0)+(prefs.NganSachToiDa!=null&&g.NganSachThamKhao!=null&&g.NganSachThamKhao<=prefs.NganSachToiDa?1:0)).ThenBy(g=>g.NgayBatDau).ThenByDescending(g=>g.MaNhom):query.OrderBy(g=>g.NgayBatDau).ThenByDescending(g=>g.MaNhom);
        var total=await query.CountAsync();var groups=await ordered.Skip((page-1)*20).Take(20).ToListAsync();var ids=groups.Select(g=>g.MaNhom).ToList();var names=await Names(groups.Select(g=>g.MaChuNhom));
        var counts=await db.ThanhVienDongHanh.Where(x=>ids.Contains(x.MaNhom)&&x.TrangThai==Joined).GroupBy(x=>x.MaNhom).Select(x=>new{Id=x.Key,Count=x.Count()}).ToDictionaryAsync(x=>x.Id,x=>x.Count);
        return new(groups.Select(g=>{var reasons=new List<string>();if(prefs!=null){if(g.PhongCach==prefs.PhongCach&&g.PhongCach!="Khac")reasons.Add("Cùng thích "+CompanionLabels.Styles[g.PhongCach].ToLower());if(g.NhipDo==prefs.NhipDo)reasons.Add("Cùng nhịp đi "+CompanionLabels.Paces[g.NhipDo].ToLower());if(prefs.NganSachToiDa!=null&&g.NganSachThamKhao!=null&&g.NganSachThamKhao<=prefs.NganSachToiDa)reasons.Add("Trong ngân sách bạn chọn");}return new CompanionCard(g,names.GetValueOrDefault(g.MaChuNhom,"Người tổ chức"),counts.GetValueOrDefault(g.MaNhom),reasons);}).ToList(),total,page,region,kind,start,end,budget,matching&&prefs!=null);
    }
    public async Task<CompanionPublicPage> Public(long id)
    {
        var group=await PublicGroups().SingleOrDefaultAsync(x=>x.MaNhom==id)??throw new BusinessException("Lời rủ không còn được công bố.",404);
        if(Me>0)travel.Require(!await IsBlocked(group.MaChuNhom),"Không tìm thấy lời rủ này.",404);
        var member=Me>0?await db.ThanhVienDongHanh.AsNoTracking().SingleOrDefaultAsync(x=>x.MaNhom==id&&x.MaTaiKhoan==Me):null;
        var profile=await(from p in db.HoSoDongHanh join basic in db.HoSoNguoiDung on p.MaTaiKhoan equals basic.MaTaiKhoan where p.MaTaiKhoan==@group.MaChuNhom&&p.HienThi&&basic.ThongTinChiaSe==ThongTinChiaSe.CongKhai select new HoSoDongHanh{PhongCach=p.PhongCach,NhipDo=p.NhipDo,GioiThieu=p.GioiThieu}).SingleOrDefaultAsync();
        return new(group,(await Names([group.MaChuNhom])).GetValueOrDefault(group.MaChuNhom,"Người tổ chức"),await db.ThanhVienDongHanh.CountAsync(x=>x.MaNhom==id&&x.TrangThai==Joined),member,profile);
    }
    public async Task<CompanionMinePage> Mine(int page=1)
    {
        travel.RoleRequired(TravelService.Traveler);travel.Require(page is >=1 and <=100000,"Trang không hợp lệ.",400);var query=db.ThanhVienDongHanh.AsNoTracking().Where(x=>x.MaTaiKhoan==Me);var total=await query.CountAsync();var members=await query.OrderByDescending(x=>x.ThoiDiemCapNhat).ThenByDescending(x=>x.MaNhom).Skip((page-1)*20).Take(20).ToDictionaryAsync(x=>x.MaNhom);var ids=members.Keys.ToList();
        return new(await db.NhomDongHanh.AsNoTracking().Where(x=>ids.Contains(x.MaNhom)).OrderByDescending(x=>x.ThoiDiemCapNhat).ToListAsync(),members,page,total);
    }
    public async Task<CompanionFormPage> Form(long id=0,long? planId=null,long? connectionId=null,CompanionGroupInput? input=null)
    {
        travel.RoleRequired(TravelService.Traveler);var plans=await travel.Plans();
        if(input==null&&id>0){var g=await Host(id);input=new(){Id=id,GroupId=id,Version=TravelService.Token(g.RowVersion),Name=g.TenNhom,Description=g.MoTa,Region=g.KhuVuc,Kind=g.Loai,Start=g.NgayBatDau,End=g.NgayKetThuc,MeetTime=g.GioHen,Meeting=g.DiemHen,Capacity=g.SoNguoiToiDa,Budget=g.NganSachThamKhao,Style=g.PhongCach,Pace=g.NhipDo,PlanId=g.MaKeHoach,ConnectionId=g.MaDeNghi,Publish=g.TrangThai!="Nhap"};}
        if(input==null){input=new(){PlanId=planId,ConnectionId=connectionId};if(planId!=null){var p=await travel.OwnPlan(planId.Value);input.Name="Cùng khám phá "+p.NoiDen;input.Region=p.NoiDen;input.Start=p.NgayBatDau;input.End=p.NgayKetThuc;input.Kind="ChuyenDi";}if(connectionId!=null)await ConnectionPeople(connectionId.Value);}
        return new(input,plans,await Connections());
    }
    private async Task<(long First,long Second)> ConnectionPeople(long id)
    {
        var c=await travel.Connection(id);travel.Require(c.TrangThai==TrangThaiDeNghiKetNoi.DaChapNhan,"Chỉ gắn nhóm với kết nối đã được hai bên chấp nhận.");var n=await db.NhuCauDongHanh.SingleAsync(x=>x.MaNhuCau==c.MaNhuCau);return(c.MaNguoiGui,n.MaTaiKhoan);
    }
    public async Task<string?> LinkedGroup(long connectionId)
    {
        await ConnectionPeople(connectionId);var g=await db.NhomDongHanh.AsNoTracking().SingleOrDefaultAsync(x=>x.MaDeNghi==connectionId);if(g==null)return null;var m=await db.ThanhVienDongHanh.AsNoTracking().SingleOrDefaultAsync(x=>x.MaNhom==g.MaNhom&&x.MaTaiKhoan==Me);return m?.TrangThai==Joined?Url(g.MaNhom):m!=null?"/Together/Mine":!g.AnBoiQuanTri&&g.TrangThai is "Mo" or "DaChot"?"/Together/Detail/"+g.MaNhom:"/Together/Mine";
    }
    public async Task<CompanionWorkspace> Workspace(long id,string tab="overview",int page=1,int paymentPage=1)
    {
        var g=await Member(id);travel.Require(CompanionLabels.Tabs.ContainsKey(tab)&&page is >=1 and <=100000&&paymentPage is >=1 and <=100000,"Trang nhóm không hợp lệ.",400);var host=g.MaChuNhom==Me;
        var allMembers=await db.ThanhVienDongHanh.AsNoTracking().Where(x=>x.MaNhom==id&&(x.TrangThai==Joined||x.MaTaiKhoan==Me)).ToListAsync();
        var moreMembers=false;
        if(tab=="members"&&host){var pending=await db.ThanhVienDongHanh.AsNoTracking().Where(x=>x.MaNhom==id&&x.TrangThai!=Joined&&x.MaTaiKhoan!=Me).OrderByDescending(x=>x.ThoiDiemCapNhat).Skip((page-1)*20).Take(21).ToListAsync();moreMembers=pending.Count>20;allMembers.AddRange(pending.Take(20));}
        var messages=new List<TinNhanDongHanh>();var read=new HashSet<long>();var blocked=await Blocked().ToListAsync();var polls=new List<CompanionPoll>();var activities=new List<HoatDongNhom>();var tasks=new List<ViecNhom>();var costs=new List<CompanionCost>();var payments=new List<HoanTraNhom>();var balances=new Dictionary<long,decimal>();bool more=moreMembers;bool morePayments=false;
        if(tab=="chat"){
            messages=await db.TinNhanDongHanh.AsNoTracking().Where(x=>x.MaNhom==id&&!x.DaAn&&!blocked.Contains(x.MaNguoiGui)).OrderByDescending(x=>x.MaTinNhan).Skip((page-1)*30).Take(31).ToListAsync();more=messages.Count>30;messages=messages.Take(30).OrderBy(x=>x.MaTinNhan).ToList();var mids=messages.Select(x=>x.MaTinNhan).ToList();read=(await db.XacNhanThongBaoNhom.Where(x=>mids.Contains(x.MaTinNhan)&&x.MaTaiKhoan==Me).Select(x=>x.MaTinNhan).ToListAsync()).ToHashSet();
        }
        if(tab=="schedule"){
            activities=await db.HoatDongNhom.AsNoTracking().Where(x=>x.MaNhom==id&&!x.DaHuy).OrderBy(x=>x.Ngay).ThenBy(x=>x.Gio).ThenBy(x=>x.MaHoatDong).ToListAsync();
            var pages=await db.BinhChonDongHanh.AsNoTracking().Where(x=>x.MaNhom==id).OrderByDescending(x=>x.ThoiDiemTao).ThenByDescending(x=>x.MaBinhChon).Skip((page-1)*10).Take(11).ToListAsync();more=pages.Count>10;
            foreach(var p in pages.Take(10)){var options=await db.LuaChonBinhChon.AsNoTracking().Where(x=>x.MaBinhChon==p.MaBinhChon).OrderBy(x=>x.ThuTu).ToListAsync();var counts=await db.PhieuBinhChon.Where(x=>x.MaBinhChon==p.MaBinhChon).GroupBy(x=>x.ThuTu).Select(x=>new{Choice=x.Key,Count=x.Count()}).ToDictionaryAsync(x=>x.Choice,x=>x.Count);var vote=await db.PhieuBinhChon.AsNoTracking().SingleOrDefaultAsync(x=>x.MaBinhChon==p.MaBinhChon&&x.MaTaiKhoan==Me);polls.Add(new(p,options,counts,vote?.ThuTu,vote!=null?TravelService.Token(vote.RowVersion):null));}
        }
        if(tab=="tasks"){tasks=await db.ViecNhom.AsNoTracking().Where(x=>x.MaNhom==id&&!x.DaHuy).OrderBy(x=>x.DaXong).ThenBy(x=>x.Han).ThenByDescending(x=>x.MaViec).Skip((page-1)*30).Take(31).ToListAsync();more=tasks.Count>30;tasks=tasks.Take(30).ToList();}
        if(tab=="expenses"){
            var entries=await db.KhoanChiNhom.AsNoTracking().Where(x=>x.MaNhom==id).OrderByDescending(x=>x.NgayChi).ThenByDescending(x=>x.MaKhoanChi).Skip((page-1)*20).Take(21).ToListAsync();more=entries.Count>20;foreach(var cost in entries.Take(20))costs.Add(new(cost,await db.PhanBoChiNhom.AsNoTracking().Where(x=>x.MaKhoanChi==cost.MaKhoanChi).ToListAsync()));
            payments=await db.HoanTraNhom.AsNoTracking().Where(x=>x.MaNhom==id).OrderByDescending(x=>x.ThoiDiemTao).ThenByDescending(x=>x.MaHoanTra).Skip((paymentPage-1)*20).Take(21).ToListAsync();morePayments=payments.Count>20;payments=payments.Take(20).ToList();balances=await Balances(id);
        }
        var ids=allMembers.Select(x=>x.MaTaiKhoan).Concat(messages.Select(x=>x.MaNguoiGui)).Concat(tasks.Where(x=>x.MaPhuTrach!=null).Select(x=>x.MaPhuTrach!.Value)).Concat(costs.SelectMany(x=>x.Shares.Select(y=>y.MaTaiKhoan).Append(x.Cost.MaNguoiTra))).Concat(payments.SelectMany(x=>new[]{x.MaNguoiGui,x.MaNguoiNhan})).Concat(balances.Keys).ToHashSet();
        var profiles=await db.HoSoDongHanh.AsNoTracking().Where(x=>ids.Contains(x.MaTaiKhoan)&&x.HienThi).Select(x=>new HoSoDongHanh{MaTaiKhoan=x.MaTaiKhoan,PhongCach=x.PhongCach,NhipDo=x.NhipDo,GioiThieu=x.GioiThieu}).ToDictionaryAsync(x=>x.MaTaiKhoan);
        long? visibleConnection=g.MaDeNghi;if(visibleConnection!=null&&!await(from c in db.DeNghiKetNoiDongHanh join n in db.NhuCauDongHanh on c.MaNhuCau equals n.MaNhuCau where c.MaDeNghi==visibleConnection&&(c.MaNguoiGui==Me||n.MaTaiKhoan==Me) select c).AnyAsync())visibleConnection=null;
        return new(){Group=g,Me=allMembers.Single(x=>x.MaTaiKhoan==Me),Host=host,Writable=IsWritable(g),Tab=tab,Members=allMembers,Names=await Names(ids),Profiles=profiles,BlockVersions=await db.ChanDongHanh.Where(x=>x.MaNguoiChan==Me).ToDictionaryAsync(x=>x.MaBiChan,x=>TravelService.Token(x.RowVersion)),Messages=messages,ReadNotices=read,Polls=polls,Activities=activities,Tasks=tasks,Expenses=costs,Payments=payments,Balances=balances,Page=page,HasMore=more,ConnectionId=visibleConnection,PaymentPage=paymentPage,MorePayments=morePayments,Invitees=host&&tab=="members"?await Connections():[],Importable=host&&tab=="schedule"&&g.MaKeHoach!=null?await travel.Activities(g.MaKeHoach.Value):[]};
    }
}


