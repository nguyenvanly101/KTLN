using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class TepController(ILuuTepService component,TravelService read):FullTextController<ILuuTepService>(component,read) {
 [Authorize(Policy="DaXacThuc"),HttpGet("/tep/{loai}/{maTaiLieu}")]
 public async Task<IActionResult> TaiTep([ValidateNever] TepViewModel query) { var loai=query.Loai;var maTaiLieu=query.MaTaiLieu;var kind=loai=="XacMinh"?"verification":loai=="GiaiTrinh"?"explanation":loai;var file=await C.TaiTepAsync(kind,maTaiLieu);if(!System.IO.File.Exists(file.Path))return NotFound();return PhysicalFile(file.Path,file.ContentType,file.Name); }
}
