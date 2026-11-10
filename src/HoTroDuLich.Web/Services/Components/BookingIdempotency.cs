using Microsoft.EntityFrameworkCore;
namespace HoTroDuLich.Web.Services;
public partial interface IDatDichVuService {Task<long?> TimLuotGuiAsync(Guid maLuotGui);}
public partial class DatDichVuService {public Task<long?> TimLuotGuiAsync(Guid maLuotGui)=>db.YeuCauDatDichVu.Where(x=>x.MaTaiKhoan==UserId&&x.MaLuotGui==maLuotGui).Select(x=>(long?)x.MaYeuCau).SingleOrDefaultAsync();}
public partial class TravelService {public Task<long?> ExistingSubmission(Guid id)=>Resolve<IDatDichVuService>().TimLuotGuiAsync(id);}
