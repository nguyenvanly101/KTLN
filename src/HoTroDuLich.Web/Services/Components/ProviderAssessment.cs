using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace HoTroDuLich.Web.Services;
public partial interface IQuyenHoatDongService {Task<ProviderAssessment> LayDanhGiaAsync(long maNCC);}
public partial class QuyenHoatDongService {
 public async Task<ProviderAssessment> LayDanhGiaAsync(long maNCC){RoleRequired(AdminRole);var services=await db.DichVuDuLich.Where(x=>x.MaNhaCungCap==maNCC).OrderBy(x=>x.TenDichVu).ToListAsync();var ids=services.Select(x=>x.MaDichVu).ToArray();var requests=db.YeuCauDatDichVu.Where(x=>ids.Contains(x.MaDichVu));var waiting=await requests.Where(x=>x.TrangThai==TrangThaiYeuCau.ChoPhanHoi||x.TrangThai==TrangThaiYeuCau.DeXuatKhac).ToListAsync();var confirmed=await requests.Where(x=>x.TrangThai==TrangThaiYeuCau.DaXacNhan||x.TrangThai==TrangThaiYeuCau.ChoKiemTra).ToListAsync();var complaints=await db.KetQuaCungCapDichVu.Where(x=>x.TrangThaiKiemTra==TrangThaiKiemTra.ChoKiemTra&&requests.Any(y=>y.MaYeuCau==x.MaYeuCau)).ToListAsync();return new(services,waiting,confirmed,complaints);}
}
