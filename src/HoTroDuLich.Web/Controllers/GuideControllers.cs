using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Controllers;

/// <summary>Lich trinh mau va so khan cap cho khach (xem tu do, chep ve khi la khach du lich).</summary>
public sealed class GuideController(TravelService travel,GuideService guide,AppDbContext db):AppController(travel)
{
 [AllowAnonymous,HttpGet("/lich-mau")]
 public async Task<IActionResult> Templates(long? diemDen,string? phongCach,int? soNgay)
 {
  ViewBag.Units=await db.Set<DonViHanhChinh>().AsNoTracking().Where(u=>db.Set<LichTrinhMau>().Any(t=>t.DonViId==u.Id&&t.Status=="Published")).OrderBy(u=>u.Name).Select(u=>new{u.Id,u.Name}).ToListAsync();
  ViewBag.Filter=(diemDen,phongCach,soNgay);
  return V("Guide/Templates",await guide.Published(diemDen,phongCach,soNgay));
 }
 [AllowAnonymous,HttpGet("/lich-mau/{id:long}")]
 public async Task<IActionResult> Template(long id)=>V("Guide/Template",await guide.Detail(id));
 [Authorize(Roles=TravelService.Traveler),HttpPost("/lich-mau/{id:long}/dung")]
 public async Task<IActionResult> Use(long id,DateOnly start){var planId=await guide.UseTemplate(id,start);return Done("/ke-hoach/"+planId,"Đã chép lịch trình mẫu vào kế hoạch của bạn. Hãy chỉnh theo ý mình.");}
 [AllowAnonymous,HttpGet("/khan-cap")]
 public IActionResult Emergency()=>V("Guide/Emergency");
}

[Authorize(Roles=TravelService.AdminRole)]
public sealed class GuideAdminController(TravelService travel,GuideService guide,AppDbContext db):AppController(travel)
{
 // Tai du lieu tho roi sap xep trong ung dung: tranh de SQL Server xin cap bo nho cho phep sap xep/noi bang (LocalDB it bo nho se cho qua gio).
 async Task Units(){var rows=await db.Set<DonViHanhChinh>().AsNoTracking().Where(u=>u.Regime=="Cu"&&u.Level<=2).Select(u=>new{u.Id,u.Level,u.Name,u.FullName,u.ParentId}).ToListAsync();var names=rows.Where(r=>r.Level==1).ToDictionary(r=>r.Id,r=>r.Name);ViewBag.Units=rows.OrderBy(r=>r.Level).ThenBy(r=>r.Name,StringComparer.Create(new System.Globalization.CultureInfo("vi-VN"),false)).Select(r=>new{r.Id,Label=r.Level==1?r.FullName:r.FullName+" — "+(r.ParentId!=null&&names.TryGetValue(r.ParentId.Value,out var p)?p:"")}).ToList();}
 [HttpGet("/quan-tri/lich-mau")] public async Task<IActionResult> Index()=>V("Guide/AdminTemplates",await guide.AllForAdmin());
 [HttpGet("/quan-tri/lich-mau/soan")] public async Task<IActionResult> Edit(long id=0,long? diemDen=null){await Units();return V("Guide/TemplateEdit",await guide.TemplateForm(id,diemDen));}
 [HttpPost("/quan-tri/lich-mau/luu")] public async Task<IActionResult> Save(TemplateInput input){var id=await guide.SaveTemplate(input);return Done("/quan-tri/lich-mau","Đã lưu lịch trình mẫu.");}
 [HttpPost("/quan-tri/lich-mau/xoa")] public async Task<IActionResult> Delete(long id){await guide.DeleteTemplate(id);return Done("/quan-tri/lich-mau","Đã xóa lịch trình mẫu.");}
}
