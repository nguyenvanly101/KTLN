using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using HoTroDuLich.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
namespace HoTroDuLich.Web.Controllers;
public class PhanAnhKetQuaController(IKetQuaDichVuService component,TravelService read):FullTextController<IKetQuaDichVuService>(component,read) {
 [Authorize(Policy="LaKhachDuLich"),HttpGet("/yeu-cau/{maYeuCau}/ket-qua/xem-ket-qua")]
 public async Task<IActionResult> XemKetQua([ValidateNever] PhanAnhKetQuaViewModel query) { var maYeuCau=query.MaYeuCau;var y=await S.Booking(maYeuCau);S.Ensure(y.MaTaiKhoan==S.UserId,"0901-01");await C.XemKetQuaAsync(maYeuCau);return await OutcomePage(maYeuCau); }
 [Authorize(Policy="LaKhachDuLich"),HttpPost("/yeu-cau/{maYeuCau}/ket-qua/gui-phan-anh")]
 public async Task<IActionResult> GuiPhanAnh(long maYeuCau,PhanAnhKetQuaViewModel i) { var r=await S.Find<KetQuaCungCapDichVu>(i.Id);S.Ensure(r.MaYeuCau==maYeuCau,"0901-01");i.Decision="PhanAnh";await C.GuiPhanAnhAsync(i);return Done("/yeu-cau/"+maYeuCau+"/ket-qua/xem-ket-qua"); }
}
