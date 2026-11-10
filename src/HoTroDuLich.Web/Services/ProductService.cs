using System.ComponentModel.DataAnnotations;
using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

public sealed class ProductService(AppDbContext db, TravelService travel)
{
    private void Traveler() => travel.RoleRequired(TravelService.Traveler);
    private static void Validate(object input)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new(input), errors, true))
            throw new BusinessException(errors[0].ErrorMessage ?? "Kiểm tra thông tin đã nhập.", 400, field:errors[0].MemberNames.FirstOrDefault());
    }
    public async Task<List<SavedChoice>> Saved()
    {
        Traveler();
        var entries = await db.LuaChonDaLuu.Where(x=>x.MaTaiKhoan==travel.UserId).OrderByDescending(x=>x.ThoiDiemLuu).ThenByDescending(x=>x.MaLuaChon).ToListAsync();
        var serviceIds=entries.Select(x=>x.MaDichVu).ToList();var placeIds=entries.Select(x=>x.MaDiaDiem).ToList();
        var services=await travel.PublicServices().Where(x=>serviceIds.Contains(x.MaDichVu)).ToDictionaryAsync(x=>x.MaDichVu);
        var places=await db.DiaDiemDuLich.Where(x=>placeIds.Contains(x.MaDiaDiem)&&x.DangHienThi).ToDictionaryAsync(x=>x.MaDiaDiem);
        return entries.Select(x=>new SavedChoice(x, x.MaDichVu is long s ? services.GetValueOrDefault(s):null, x.MaDiaDiem is long p ? places.GetValueOrDefault(p):null)).ToList();
    }
    public async Task Save(SavedInput input)
    {
        Traveler();Validate(input);
        travel.Require((input.ServiceId==null)!=(input.PlaceId==null),"Chọn một dịch vụ hoặc địa điểm để lưu.",400);
        await travel.Execute(async()=>
        {
            var name=input.ServiceId is long service ? (await travel.PublicService(service)).TenDichVu : (await travel.Place(input.PlaceId!.Value)).TenDiaDiem;
            if(!await db.LuaChonDaLuu.AnyAsync(x=>x.MaTaiKhoan==travel.UserId&&x.MaDichVu==input.ServiceId&&x.MaDiaDiem==input.PlaceId))
                db.Add(new LuaChonDaLuu {MaTaiKhoan=travel.UserId,MaDichVu=input.ServiceId,MaDiaDiem=input.PlaceId,TenMuc=name});
        });
    }
    public async Task RemoveSaved(long id)
    {
        Traveler();await travel.Execute(async()=>
        {
            var entry=await db.LuaChonDaLuu.SingleOrDefaultAsync(x=>x.MaLuaChon==id&&x.MaTaiKhoan==travel.UserId)??throw new BusinessException("Không tìm thấy lựa chọn của bạn.",404);
            db.Remove(entry);
        });
    }
    public async Task<List<SavedChoice>> Compare(long[] ids)
    {
        Traveler();travel.Require(ids.Distinct().Count() is >=2 and <=3,"Chọn từ 2 đến 3 dịch vụ đã lưu để so sánh.",400);
        var selected=(await Saved()).Where(x=>ids.Contains(x.Entry.MaLuaChon)).ToList();
        travel.Require(selected.Count==ids.Distinct().Count()&&selected.All(x=>x.Service!=null),"Chỉ so sánh dịch vụ bạn đã lưu và còn được công bố.",404);
        return selected;
    }
    public async Task<PreparationPage> Preparation(long planId, ChecklistInput? input=null)
    {
        var plan=await travel.OwnPlan(planId);
        var items=await db.MucChuanBi.Where(x=>x.MaKeHoach==planId).OrderBy(x=>x.DaHoanThanh).ThenBy(x=>x.HanHoanThanh==null).ThenBy(x=>x.HanHoanThanh).ThenBy(x=>x.MaMuc).ToListAsync();
        return new(plan,items,input??new(){PlanId=planId,Due=plan.NgayBatDau.AddDays(-1)});
    }
    public async Task SavePreparation(ChecklistInput input)
    {
        Traveler();Validate(input);
        if(!ProductCategories.Preparation.ContainsKey(input.Category))throw new BusinessException("Chọn nhóm công việc hợp lệ.",400,field:"Category");
        if(input.Remind&&input.Due==null)throw new BusinessException("Chọn hạn hoàn thành để nhận lời nhắc.",422,field:"Due");
        await travel.Execute(async()=>
        {
            await travel.OwnPlan(input.PlanId);
            var title=input.Title.Trim();travel.Require(title.Length>0,"Nhập nội dung cần chuẩn bị.",400);
            travel.Require(!await db.MucChuanBi.AnyAsync(x=>x.MaKeHoach==input.PlanId&&x.NoiDung==title&&x.MaMuc!=input.Id),"Việc này đã có trong danh sách chuẩn bị.");
            var item=input.Id==0 ? new MucChuanBi{MaKeHoach=input.PlanId} : await db.MucChuanBi.SingleOrDefaultAsync(x=>x.MaMuc==input.Id&&x.MaKeHoach==input.PlanId)??throw new BusinessException("Không tìm thấy công việc.",404);
            if(input.Id==0)db.Add(item);else travel.CheckVersion(item,input.Version);
            if(item.HanHoanThanh!=input.Due||!item.NhacHan&&input.Remind)item.NgayDaNhac=null;
            item.NoiDung=title;item.Nhom=input.Category;item.HanHoanThanh=input.Due;item.NhacHan=input.Remind;
        });
    }
    public async Task PreparationAction(long planId, CommandInput input)
    {
        Traveler();await travel.Execute(async()=>
        {
            await travel.OwnPlan(planId);
            var item=await db.MucChuanBi.SingleOrDefaultAsync(x=>x.MaMuc==input.Id&&x.MaKeHoach==planId)??throw new BusinessException("Không tìm thấy công việc.",404);
            travel.CheckVersion(item,input.Version);
            travel.Require(new[]{"Done","Undo","Delete"}.Contains(input.Decision),"Thao tác không hợp lệ.",400);
            if(input.Decision=="Delete")db.Remove(item);else item.DaHoanThanh=input.Decision=="Done";
        });
    }
    public async Task AddPreparationDefaults(long planId)
    {
        Traveler();await travel.Execute(async()=>
        {
            var plan=await travel.OwnPlan(planId);
            foreach(var (title,category) in new[]{("Kiểm tra giấy tờ cần mang theo","GiayTo"),("Chuẩn bị hành lý và vật dụng cá nhân","HanhLy"),("Kiểm tra phương tiện đến và rời điểm đến","DiChuyen"),("Kiểm tra xác nhận của các dịch vụ đã yêu cầu","DatDichVu")})
                if(!await db.MucChuanBi.AnyAsync(x=>x.MaKeHoach==planId&&x.NoiDung==title))db.Add(new MucChuanBi{MaKeHoach=planId,NoiDung=title,Nhom=category,HanHoanThanh=plan.NgayBatDau.AddDays(-1)});
        });
    }
    public async Task<ExpensesPage> Expenses(long planId, ExpenseInput? input=null)
    {
        var plan=await travel.OwnPlan(planId);
        var items=await db.KhoanChiChuyenDi.Where(x=>x.MaKeHoach==planId).OrderByDescending(x=>x.NgayChi).ThenByDescending(x=>x.MaKhoanChi).ToListAsync();
        var estimated=await db.HoatDongLichTrinh.Where(x=>x.MaKeHoach==planId).SumAsync(x=>x.ChiPhiDuKien??0);
        return new(plan,items,estimated,input??new(){PlanId=planId});
    }
    public async Task SaveExpense(ExpenseInput input)
    {
        Traveler();Validate(input);
        if(!ProductCategories.Expenses.ContainsKey(input.Category))throw new BusinessException("Chọn loại khoản chi hợp lệ.",400,field:"Category");
        if(input.SpentOn==default||input.SpentOn>TravelService.Today)throw new BusinessException("Nhập ngày đã chi không sau hôm nay. Khoản dự kiến được ghi trong lịch trình.",422,field:"SpentOn");
        travel.Require(input.SubmissionId!=Guid.Empty,"Mã ghi khoản chi không hợp lệ.",400);
        if(decimal.Round(input.Amount,2)!=input.Amount)throw new BusinessException("Số tiền chỉ nhận tối đa 2 chữ số thập phân.",422,field:"Amount");
        await travel.Execute(async()=>
        {
            await travel.OwnPlan(input.PlanId);
            if(input.Id==0&&await db.KhoanChiChuyenDi.AnyAsync(x=>x.MaKeHoach==input.PlanId&&x.MaLuotGhi==input.SubmissionId))return;
            var item=input.Id==0 ? new KhoanChiChuyenDi{MaKeHoach=input.PlanId,MaLuotGhi=input.SubmissionId} : await db.KhoanChiChuyenDi.SingleOrDefaultAsync(x=>x.MaKhoanChi==input.Id&&x.MaKeHoach==input.PlanId)??throw new BusinessException("Không tìm thấy khoản chi.",404);
            if(input.Id==0)db.Add(item);else travel.CheckVersion(item,input.Version);
            travel.Require(input.Title.Trim().Length>0,"Nhập nội dung khoản chi.",400);
            item.NoiDung=input.Title.Trim();item.LoaiChi=input.Category;item.SoTien=input.Amount;item.NgayChi=input.SpentOn;item.GhiChu=input.Notes?.Trim();
        });
    }
    public async Task DeleteExpense(long planId,CommandInput input)
    {
        Traveler();await travel.Execute(async()=>
        {
            await travel.OwnPlan(planId);var item=await db.KhoanChiChuyenDi.SingleOrDefaultAsync(x=>x.MaKhoanChi==input.Id&&x.MaKeHoach==planId)??throw new BusinessException("Không tìm thấy khoản chi.",404);
            travel.CheckVersion(item,input.Version);db.Remove(item);
        });
    }
    public async Task RemindDue(CancellationToken token=default)
    {
        // Worker only: recipients come from the owning plan, never from a request field.
        await travel.Execute(async()=>
        {
            var today=TravelService.Today;
            var due=await (from item in db.MucChuanBi join plan in db.KeHoachChuyenDi on item.MaKeHoach equals plan.MaKeHoach
                join account in db.TaiKhoan on plan.MaTaiKhoan equals account.MaTaiKhoan
                where item.NhacHan&&!item.DaHoanThanh&&item.HanHoanThanh!=null&&item.HanHoanThanh<=today
                    &&(item.NgayDaNhac==null||item.NgayDaNhac<item.HanHoanThanh)&&account.TrangThai==TrangThaiTaiKhoan.HoatDong
                select new{Item=item,plan.MaTaiKhoan,plan.TenKeHoach}).ToListAsync(token);
            foreach(var record in due)
            {
                travel.Notify(record.MaTaiKhoan,"Đến hạn chuẩn bị: "+record.Item.NoiDung+" · "+record.TenKeHoach,"/Preparation?planId="+record.Item.MaKeHoach,"NhacChuanBi");
                record.Item.NgayDaNhac=today;
            }
        });
    }
}
