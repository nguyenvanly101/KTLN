using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoTroDuLich.Web.Controllers;

[Authorize(Roles=TravelService.Traveler)]
public sealed class SavedController(TravelService travel,ProductService product):AppController(travel)
{
    public async Task<IActionResult> Index()=>View(await product.Saved());
    [HttpPost] public async Task<IActionResult> Add(SavedInput input){await product.Save(input);return Done(Url.IsLocalUrl(input.ReturnUrl)?input.ReturnUrl!:"/Saved","Đã lưu lựa chọn của bạn.");}
    [HttpPost] public async Task<IActionResult> Remove(CommandInput input){await product.RemoveSaved(input.Id);return Done("/Saved","Đã bỏ mục khỏi danh sách lưu.");}
    [HttpGet] public async Task<IActionResult> Compare(long[] ids)=>View(await product.Compare(ids));
}
[Authorize(Roles=TravelService.Traveler)]
[SucAuthorize(SucAccess.TripPlan)]
public sealed class PreparationController(TravelService travel,ProductService product):AppController(travel)
{
    public async Task<IActionResult> Index(long planId,long edit=0){var model=await product.Preparation(planId);if(edit>0){var item=model.Items.SingleOrDefault(x=>x.MaMuc==edit)??throw new BusinessException("Không tìm thấy công việc.",404);model=model with{Input=new(){Id=item.MaMuc,PlanId=planId,Version=TravelService.Token(item.RowVersion),Title=item.NoiDung,Category=item.Nhom,Due=item.HanHoanThanh,Remind=item.NhacHan}};}return View(model);}
    [HttpPost] public async Task<IActionResult> Save(ChecklistInput input){await product.SavePreparation(input);return Done("/Preparation?planId="+input.PlanId);}
    [HttpPost] public async Task<IActionResult> Action(long planId,CommandInput input){await product.PreparationAction(planId,input);return Done("/Preparation?planId="+planId);}
    [HttpPost] public async Task<IActionResult> Defaults(long planId){await product.AddPreparationDefaults(planId);return Done("/Preparation?planId="+planId,"Đã thêm những việc gợi ý còn thiếu.");}
}
[Authorize(Roles=TravelService.Traveler)]
[SucAuthorize(SucAccess.TripPlan)]
public sealed class ExpensesController(TravelService travel,ProductService product):AppController(travel)
{
    public async Task<IActionResult> Index(long planId,long edit=0){var model=await product.Expenses(planId);if(edit>0){var item=model.Items.SingleOrDefault(x=>x.MaKhoanChi==edit)??throw new BusinessException("Không tìm thấy khoản chi.",404);model=model with{Input=new(){Id=item.MaKhoanChi,PlanId=planId,Version=TravelService.Token(item.RowVersion),SubmissionId=item.MaLuotGhi,Title=item.NoiDung,Category=item.LoaiChi,Amount=item.SoTien,SpentOn=item.NgayChi,Notes=item.GhiChu}};}return View(model);}
    [HttpPost] public async Task<IActionResult> Save(ExpenseInput input){await product.SaveExpense(input);return Done("/Expenses?planId="+input.PlanId,"Đã ghi khoản chi thực tế.");}
    [HttpPost] public async Task<IActionResult> Delete(long planId,CommandInput input){await product.DeleteExpense(planId,input);return Done("/Expenses?planId="+planId,"Đã xóa khoản chi.");}
}
