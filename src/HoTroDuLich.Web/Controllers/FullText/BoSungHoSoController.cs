using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class BoSungHoSoController(IXacMinhNhaCungCapService component,TravelService read):FullTextController<IXacMinhNhaCungCapService>(component,read) {
 [Authorize(Policy="LaNhaCungCap"),HttpGet("/nha-cung-cap/ho-so-xac-minh")]
 public async Task<IActionResult> HoSoCuaToi([ValidateNever] BoSungHoSoViewModel query) { ViewBag.Provider=await S.MyProvider();var rows=await C.HoSoCuaToiAsync();var details=new List<(HoSoXacMinhNhaCungCap Profile,NhaCungCapDichVu Provider,List<TaiLieuXacMinh> Files,List<QuyetDinhXacMinh> Decisions)>();foreach(var row in rows)details.Add(await C.XemHoSoAsync(row.MaHoSo));return V("Provider/Verification",details); }
 [Authorize(Policy="LaNhaCungCap"),HttpPost("/quan-tri/xac-minh/nhan-bo-sung")]
 public async Task<IActionResult> NhanBoSung(BoSungHoSoViewModel i) { var p=await S.MyProvider();if(p.TrangThaiXacMinh.ToString()=="DaThuHoi")await C.NopHoSoMoiAsync(i);else await C.NhanBoSungAsync(i);return Done("/nha-cung-cap/ho-so-xac-minh","Đã gửi hồ sơ, chờ xét duyệt."); }
}
