using HoTroDuLich.Web.Data;
using HoTroDuLich.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace HoTroDuLich.Web.Services;

public sealed record TravelerRow(long Id,string Email,string? Phone,string Name,DateTime CreatedAt,bool Active,int Posts,int Reports);
public sealed record TravelerPage(List<TravelerRow> Items,string Query,string Status,int Total);

/// <summary>Quan tri vien quan ly tai khoan khach du lich: tim, xem, khoa / mo khoa.</summary>
public sealed class TravelerAdminService(AppDbContext context,IHttpContextAccessor accessor,IWebHostEnvironment environment,IConfiguration configuration,IServiceProvider provider,INhatKyService audit)
 : NghiepVuService(context,accessor,environment,configuration,provider)
{
 public async Task<TravelerPage> List(string? q,string? status)
 {
  RoleRequired(AdminRole);
  q=(q??"").Trim();if(q.Length>100)q=q[..100];
  var query=db.TaiKhoan.AsNoTracking().Where(a=>a.VaiTro==VaiTro.KHACH_DU_LICH);
  if(status=="DaNgung")query=query.Where(a=>a.TrangThai==TrangThaiTaiKhoan.DaNgung);
  else if(status=="HoatDong")query=query.Where(a=>a.TrangThai==TrangThaiTaiKhoan.HoatDong);
  if(q.Length>0)query=query.Where(a=>a.Email.Contains(q)||(a.SoDienThoai!=null&&a.SoDienThoai.Contains(q))||db.HoSoNguoiDung.Any(h=>h.MaTaiKhoan==a.MaTaiKhoan&&h.TenHienThi.Contains(q)));
  var total=await query.CountAsync();
  var rows=await query.OrderByDescending(a=>a.MaTaiKhoan).Take(100).Select(a=>new{a.MaTaiKhoan,a.Email,a.SoDienThoai,a.ThoiDiemTao,a.TrangThai,
   Name=db.HoSoNguoiDung.Where(h=>h.MaTaiKhoan==a.MaTaiKhoan).Select(h=>h.TenHienThi).FirstOrDefault(),
   Posts=db.Set<BaiViet>().Count(p=>p.AuthorId==a.MaTaiKhoan&&p.DeletedAt==null),
   Reports=db.Set<BaoCaoChat>().Count(r=>r.TargetId==a.MaTaiKhoan)}).ToListAsync();
  return new(rows.Select(r=>new TravelerRow(r.MaTaiKhoan,r.Email,r.SoDienThoai,r.Name??"(chưa đặt tên)",DateTime.SpecifyKind(r.ThoiDiemTao,DateTimeKind.Utc),r.TrangThai==TrangThaiTaiKhoan.HoatDong,r.Posts,r.Reports)).ToList(),q,status??"",total);
 }
 public async Task SetActive(long id,bool active,string? reason)
 {
  RoleRequired(AdminRole);
  Require(active||!string.IsNullOrWhiteSpace(reason),"Nhập lý do khóa tài khoản.",422);
  await Execute(async()=>
  {
   var a=await db.TaiKhoan.SingleOrDefaultAsync(x=>x.MaTaiKhoan==id&&x.VaiTro==VaiTro.KHACH_DU_LICH)??throw new BusinessException("Không tìm thấy tài khoản khách du lịch.",404);
   if(active){a.TrangThai=TrangThaiTaiKhoan.HoatDong;a.ThoiDiemNgung=null;}
   else{a.TrangThai=TrangThaiTaiKhoan.DaNgung;a.ThoiDiemNgung=Clock.BayGioUtc;a.DauPhien=Guid.NewGuid();}
   audit.Ghi(active?"MoKhoaTaiKhoan":"KhoaTaiKhoan","TaiKhoan",a.MaTaiKhoan,active?"DaNgung":"HoatDong",active?"HoatDong":"DaNgung",reason);
  });
 }
}
