using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class DangDichVuController(IQuanLyDichVuService component,TravelService read):FullTextController<IQuanLyDichVuService>(component,read) {
 [Authorize(Policy="LaNhaCungCap"),HttpGet("/nha-cung-cap/dich-vu/mo-bieu-mau")]
 public async Task<IActionResult> MoBieuMau([ValidateNever] DangDichVuViewModel query) { var maDichVu=query.MaDichVu;ViewBag.CanSubmit=await C.KiemTraDieuKienDangAsync();ViewBag.Places=await S.VisiblePlaces();if(maDichVu.HasValue)ViewBag.History=await C.LayLichSuAsync(maDichVu.Value);return V("Provider/Edit",await C.MoBieuMauAsync(maDichVu??0)); }
 [Authorize(Policy="LaNhaCungCap"),HttpPost("/nha-cung-cap/dich-vu/gui-dang")]
 public async Task<IActionResult> GuiDang(DangDichVuViewModel i) { if(i.Id>0)S.Ensure((await C.XemDichVuAsync(i.Id)).TrangThaiCongBo.ToString()=="Nhap","0601-02");await C.GuiDangAsync(i);return Done("/nha-cung-cap/dich-vu",i.Operation=="LuuNhap"?"Đã lưu bản nháp.":"Đã gửi dịch vụ, chờ xét duyệt."); }
}
