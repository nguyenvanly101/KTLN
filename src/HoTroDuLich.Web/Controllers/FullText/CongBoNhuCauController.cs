using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class CongBoNhuCauController(ICongBoNhuCauService component,TravelService read):FullTextController<ICongBoNhuCauService>(component,read) {
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/dong-hanh/nhu-cau/cong-bo")]
 public async Task<IActionResult> CongBo(CongBoNhuCauViewModel i) { await C.CongBoAsync(i);return Done("/Companions/Mine","Đã công bố nhu cầu với phạm vi chia sẻ đã chọn."); }
}
