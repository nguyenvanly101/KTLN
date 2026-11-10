using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class CapNhatDichVuController(IQuanLyDichVuService component,TravelService read):FullTextController<IQuanLyDichVuService>(component,read) {
 [Authorize(Policy="LaNhaCungCap"),HttpPost("/nha-cung-cap/dich-vu/cap-nhat-dich-vu")]
 public async Task<IActionResult> CapNhatDichVu(CapNhatDichVuViewModel i) { await C.CapNhatDichVuAsync(i);return Done("/nha-cung-cap/dich-vu/"+i.Id,"Đã gửi bản cập nhật chờ duyệt."); }
}
