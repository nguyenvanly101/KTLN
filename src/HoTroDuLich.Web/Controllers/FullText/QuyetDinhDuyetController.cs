using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class QuyetDinhDuyetController(IXetDuyetDichVuService component,TravelService read):FullTextController<IXetDuyetDichVuService>(component,read) {
 [Authorize(Policy="LaQuanTriVien"),HttpPost("/quan-tri/xet-duyet-dich-vu/duyet")]
 public async Task<IActionResult> Duyet(QuyetDinhDuyetViewModel i) { if(i.Decision=="Duyet")i.Decision="DaDuyet";await C.DuyetAsync(i);return Done("/quan-tri/xet-duyet-dich-vu/mo-danh-sach-cho-duyet"); }
}
