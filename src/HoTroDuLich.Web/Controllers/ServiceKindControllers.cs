using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoTroDuLich.Web.Controllers;

[Authorize(Roles=TravelService.AdminRole)]
public sealed class ServiceKindsController(TravelService travel,ServiceKindAdminService kinds):AppController(travel)
{
 [HttpGet("/quan-tri/loai-dich-vu")] public async Task<IActionResult> Index()=>V("ServiceKinds/Index",await kinds.List());
 [HttpGet("/quan-tri/loai-dich-vu/soan")] public IActionResult Edit(string? code)=>V("ServiceKinds/Edit",kinds.Form(code));
 [HttpPost("/quan-tri/loai-dich-vu/luu")] public async Task<IActionResult> Save(ServiceKindInput input){await kinds.Save(input);return Done("/quan-tri/loai-dich-vu","Đã lưu loại dịch vụ. Form nhà cung cấp và trang khách cập nhật ngay.");}
 [HttpPost("/quan-tri/loai-dich-vu/xoa")] public async Task<IActionResult> Delete(string code){await kinds.Delete(code);return Done("/quan-tri/loai-dich-vu","Đã xóa loại dịch vụ.");}
}
