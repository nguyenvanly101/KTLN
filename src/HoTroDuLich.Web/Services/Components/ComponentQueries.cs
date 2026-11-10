using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace HoTroDuLich.Web.Services;

public partial class DanhMucService:IDiaDiem {
 public Task<bool> KiemTraConHienThi(long id)=>db.DiaDiemDuLich.AnyAsync(x=>x.MaDiaDiem==id&&x.DangHienThi);
 public async Task<List<DiaDiemDuLich>> TimDiaDiem(TieuChiTraCuu t){var q=db.DiaDiemDuLich.AsNoTracking().Where(x=>x.DangHienThi);if(!string.IsNullOrWhiteSpace(t.TuKhoa))q=q.Where(x=>x.TenDiaDiem.Contains(t.TuKhoa)||(x.MoTa!=null&&x.MoTa.Contains(t.TuKhoa)));if(!string.IsNullOrEmpty(t.KhuVuc))q=q.Where(x=>x.KhuVuc==t.KhuVuc);if(!string.IsNullOrEmpty(t.Loai)&&t.Loai!="DiaDiem"||t.GiaTu!=null||t.GiaDen!=null)q=q.Where(x=>false);return await q.OrderBy(x=>x.TenDiaDiem).ThenBy(x=>x.MaDiaDiem).Skip((t.Trang-1)*20).Take(20).ToListAsync();}
}
public partial class QuanLyDichVuService:IDichVu {
 public async Task<List<DichVuDuLich>> TimDichVu(TieuChiTraCuu t){var q=PublicServices().AsNoTracking();if(!string.IsNullOrWhiteSpace(t.TuKhoa))q=q.Where(x=>x.TenDichVu.Contains(t.TuKhoa)||(x.MoTa!=null&&x.MoTa.Contains(t.TuKhoa)));if(!string.IsNullOrEmpty(t.KhuVuc))q=q.Where(x=>x.KhuVucPhucVu==t.KhuVuc);if(!string.IsNullOrEmpty(t.Loai)){var legacy=DanhMuc.Ten(t.Loai);q=q.Where(x=>x.LoaiDichVu==t.Loai||x.LoaiDichVu==legacy);}if(t.GiaTu!=null)q=q.Where(x=>x.GiaThamKhao>=t.GiaTu);if(t.GiaDen!=null)q=q.Where(x=>x.GiaThamKhao<=t.GiaDen);return await q.OrderBy(x=>x.TenDichVu).ThenBy(x=>x.MaDichVu).Skip((t.Trang-1)*20).Take(20).ToListAsync();}
 public Task<bool> KiemTraConCongBo(long id)=>PublicServices().AnyAsync(x=>x.MaDichVu==id);
 public async Task<bool> DangTiepNhan(long id){var s=await db.DichVuDuLich.FindAsync(id);return s!=null&&s.TrangThaiCongBo==TrangThaiCongBo.DaDuyet&&s.TrangThaiTiepNhan==TrangThaiTiepNhan.TiepNhan&&await ProviderActive(s.MaNhaCungCap);}
 public Task<BanCapNhatDichVu> LayBanCapNhat(long id)=>Find<BanCapNhatDichVu>(id);
 public async Task CongBo(long id,BanCapNhatDichVu ban){var s=await Find<DichVuDuLich>(id);Require(ban.MaDichVu==id,"Phiên bản không thuộc dịch vụ.",422);foreach(var field in ServiceFields)s.GetType().GetProperty(field.Domain)!.SetValue(s,ban.GetType().GetProperty(field.Domain)!.GetValue(ban));s.PhienBanCongBo=ban.SoPhienBan;s.TrangThaiCongBo=TrangThaiCongBo.DaDuyet;s.TrangThaiTiepNhan=TrangThaiTiepNhan.TiepNhan;s.MaQuyetDinhKhongTiepNhan=null;s.ThoiDiemCapNhat=Clock.BayGioUtc;}
 public async Task TuChoiBanChoDuyet(long id,long admin,string reason){await db.BanCapNhatDichVu.Where(x=>x.TrangThaiDuyet==TrangThaiDuyet.ChoDuyet&&db.DichVuDuLich.Any(s=>s.MaDichVu==x.MaDichVu&&s.MaNhaCungCap==id)).ExecuteUpdateAsync(u=>u.SetProperty(x=>x.TrangThaiDuyet,TrangThaiDuyet.TuChoi).SetProperty(x=>x.LyDo,reason).SetProperty(x=>x.MaAdminDuyet,admin).SetProperty(x=>x.ThoiDiemDuyet,Clock.BayGioUtc));}
 public async Task KhoaTiepNhan(long id,long decision){await db.DichVuDuLich.Where(x=>x.MaNhaCungCap==id&&x.TrangThaiTiepNhan==TrangThaiTiepNhan.TiepNhan).ExecuteUpdateAsync(u=>u.SetProperty(x=>x.TrangThaiTiepNhan,TrangThaiTiepNhan.KhongTiepNhanMoi).SetProperty(x=>x.MaQuyetDinhKhongTiepNhan,decision));}
 public async Task MoTiepNhan(long decision){await db.DichVuDuLich.Where(x=>x.MaQuyetDinhKhongTiepNhan==decision).ExecuteUpdateAsync(u=>u.SetProperty(x=>x.TrangThaiTiepNhan,TrangThaiTiepNhan.TiepNhan).SetProperty(x=>x.MaQuyetDinhKhongTiepNhan,(long?)null));}
 public async Task NgungCongBo(long id){await db.DichVuDuLich.Where(x=>x.MaNhaCungCap==id&&x.TrangThaiCongBo==TrangThaiCongBo.DaDuyet).ExecuteUpdateAsync(u=>u.SetProperty(x=>x.TrangThaiCongBo,TrangThaiCongBo.NgungCongBo));await db.DichVuDuLich.Where(x=>x.MaNhaCungCap==id&&(x.TrangThaiCongBo==TrangThaiCongBo.ChoDuyet||x.TrangThaiCongBo==TrangThaiCongBo.CanSua)).ExecuteUpdateAsync(u=>u.SetProperty(x=>x.TrangThaiCongBo,TrangThaiCongBo.TuChoi));}
 public Task<int> DemDichVuTiepNhan(long id)=>db.DichVuDuLich.CountAsync(x=>x.MaNhaCungCap==id&&x.TrangThaiTiepNhan==TrangThaiTiepNhan.TiepNhan);
}
public partial class LapKeHoachService:IKeHoach {public Task<bool> LaChuKeHoach(long user,long plan)=>db.KeHoachChuyenDi.AnyAsync(x=>x.MaKeHoach==plan&&x.MaTaiKhoan==user);}
public partial class CongBoNhuCauService:INhuCauDongHanh {
 public IQueryable<NhuCauDongHanh> NguoiXemThayDuoc(long? viewer,DateOnly today)=>db.NhuCauDongHanh.Where(x=>x.TrangThaiCongBo==TrangThaiNhuCau.DangCongBo&&x.NgayKetThuc>=today&&db.TaiKhoan.Any(a=>a.MaTaiKhoan==x.MaTaiKhoan&&a.TrangThai==TrangThaiTaiKhoan.HoatDong)&&(viewer==null?x.PhamViChiaSe==PhamViChiaSe.CongKhai:x.MaTaiKhoan!=viewer));
 public async Task<(NhuCauDongHanh NhuCau,TaiKhoan Chu)> LayNhuCauVaChu(long id){var n=await Find<NhuCauDongHanh>(id);return(n,await Find<TaiKhoan>(n.MaTaiKhoan));}
 public async Task DungCongBoNhuCau(long id){await db.NhuCauDongHanh.Where(x=>x.MaTaiKhoan==id&&x.TrangThaiCongBo==TrangThaiNhuCau.DangCongBo).ExecuteUpdateAsync(u=>u.SetProperty(x=>x.TrangThaiCongBo,TrangThaiNhuCau.DungCongBo));}
}
public partial class KetNoiDongHanhService:IDeNghiKetNoi {
 public async Task<DeNghiKetNoiDongHanh> LayKetNoi(long id,long user)=>await db.DeNghiKetNoiDongHanh.SingleOrDefaultAsync(x=>x.MaDeNghi==id&&(x.MaNguoiGui==user||db.NhuCauDongHanh.Any(n=>n.MaNhuCau==x.MaNhuCau&&n.MaTaiKhoan==user)))??throw new BusinessException("Không tìm thấy kết nối.",404,"0401-05");
 public Task<bool> DaGuiDeNghi(long user,long need)=>db.DeNghiKetNoiDongHanh.AnyAsync(x=>x.MaNguoiGui==user&&x.MaNhuCau==need&&(x.TrangThai==TrangThaiDeNghiKetNoi.ChoPhanHoi||x.TrangThai==TrangThaiDeNghiKetNoi.DaChapNhan));
 public async Task HetHieuLucDeNghiCho(long user){foreach(var c in await db.DeNghiKetNoiDongHanh.Where(x=>x.TrangThai==TrangThaiDeNghiKetNoi.ChoPhanHoi&&(x.MaNguoiGui==user||db.NhuCauDongHanh.Any(n=>n.MaNhuCau==x.MaNhuCau&&n.MaTaiKhoan==user))).ToListAsync()){var n=await Find<NhuCauDongHanh>(c.MaNhuCau);c.TrangThai=TrangThaiDeNghiKetNoi.HetHieuLuc;c.ThoiDiemPhanHoi=Clock.BayGioUtc;Resolve<IThongBaoService>().Tao(user==c.MaNguoiGui?n.MaTaiKhoan:c.MaNguoiGui,"DeNghiHetHieuLuc","Đề nghị hết hiệu lực vì một bên ngừng tài khoản.","/dong-hanh/de-nghi/xem-de-nghi/"+c.MaDeNghi);}}
}
public partial class ThoaThuanDongHanhService:IThoaThuanDongHanh {
 private IQueryable<ThoaThuanDongHanh> ThoaThuanCua(long user)=>db.ThoaThuanDongHanh.Where(x=>db.DeNghiKetNoiDongHanh.Any(c=>c.MaDeNghi==x.MaDeNghi&&(c.MaNguoiGui==user||db.NhuCauDongHanh.Any(n=>n.MaNhuCau==c.MaNhuCau&&n.MaTaiKhoan==user))));
 public Task<bool> CoThoaThuanHieuLuc(long user)=>ThoaThuanCua(user).AnyAsync(x=>x.TrangThai==TrangThaiThoaThuan.DaXacNhan);
 public async Task DungDeXuatCho(long user){await ThoaThuanCua(user).Where(x=>x.TrangThai==TrangThaiThoaThuan.ChoXacNhan).ExecuteUpdateAsync(u=>u.SetProperty(x=>x.TrangThai,TrangThaiThoaThuan.DaThayThe).SetProperty(x=>x.LyDo,"Một bên ngừng tài khoản"));}
}
public partial class DatDichVuService:IYeuCauDatDichVu {
 public async Task<YeuCauDatDichVu> LayYeuCauCuaKhach(long id,long user)=>await db.YeuCauDatDichVu.SingleOrDefaultAsync(x=>x.MaYeuCau==id&&x.MaTaiKhoan==user)??throw new BusinessException("Không tìm thấy yêu cầu.",404,"0701-04");
 public async Task<YeuCauDatDichVu> LayYeuCauCuaNCC(long id,long provider)=>await db.YeuCauDatDichVu.SingleOrDefaultAsync(x=>x.MaYeuCau==id&&db.DichVuDuLich.Any(s=>s.MaDichVu==x.MaDichVu&&s.MaNhaCungCap==provider))??throw new BusinessException("Không tìm thấy yêu cầu.",404,"0701-04");
 public async Task CapNhatTrangThai(long id,TrangThaiYeuCau status,string? reason){var y=await Find<YeuCauDatDichVu>(id);y.TrangThai=status;y.LyDo=reason;y.PhienBan++;}
 public async Task CapNhatDieuKien(long id,BookingInput i){var y=await Find<YeuCauDatDichVu>(id);y.NgayBatDau=i.Start;y.NgayKetThuc=i.End;y.ThoiGianDenDi=i.ArrivalDeparture;y.SoNguoi=i.People;y.SoLuong=i.Quantity;y.NoiDungLuaChon=i.Selections;y.ChiPhiDuKien=i.Cost;y.GhiChu=i.Notes;y.PhienBan++;}
 public async Task ApDungDieuKienMoi(long id,DeNghiDieuChinhDichVu d){var y=await Find<YeuCauDatDichVu>(id);ApplyChange(y,d);}
 public async Task DungYeuCauDangCho(long id,string reason){foreach(var y in await db.YeuCauDatDichVu.Where(x=>(x.TrangThai==TrangThaiYeuCau.ChoPhanHoi||x.TrangThai==TrangThaiYeuCau.DeXuatKhac)&&db.DichVuDuLich.Any(s=>s.MaDichVu==x.MaDichVu&&s.MaNhaCungCap==id)).ToListAsync()){y.TrangThai=TrangThaiYeuCau.KhongTiepTuc;y.LyDo=reason;y.ThoiDiemPhanHoi=Clock.BayGioUtc;y.PhienBan++;Resolve<IThongBaoService>().Tao(y.MaTaiKhoan,"YeuCauKhongTiepTuc","Yêu cầu #"+y.MaYeuCau+" không tiếp tục: "+reason,"/yeu-cau/"+y.MaYeuCau);}}
 public Task<int> DemYeuCauDangCho(long id)=>db.YeuCauDatDichVu.CountAsync(x=>(x.TrangThai==TrangThaiYeuCau.ChoPhanHoi||x.TrangThai==TrangThaiYeuCau.DeXuatKhac)&&db.DichVuDuLich.Any(s=>s.MaDichVu==x.MaDichVu&&s.MaNhaCungCap==id));
 public Task<bool> CoNghiaVuMo(long user,VaiTro role)=>db.YeuCauDatDichVu.AnyAsync(x=>(x.TrangThai==TrangThaiYeuCau.ChoPhanHoi||x.TrangThai==TrangThaiYeuCau.DeXuatKhac||x.TrangThai==TrangThaiYeuCau.DaXacNhan||x.TrangThai==TrangThaiYeuCau.ChoKiemTra)&&(role==VaiTro.KHACH_DU_LICH?x.MaTaiKhoan==user:db.DichVuDuLich.Any(s=>s.MaDichVu==x.MaDichVu&&db.NhaCungCapDichVu.Any(p=>p.MaNhaCungCap==s.MaNhaCungCap&&p.MaTaiKhoan==user))));
}
public partial class DangKyService:ITaiKhoan,IHoSoNguoiDung,INhaCungCap,IHoSoXacMinh {
 public Task<TaiKhoan?> TimTheoDinhDanh(string value){value=value.Trim();if(value.Contains('@')){var email=value.ToLowerInvariant();return db.TaiKhoan.SingleOrDefaultAsync(x=>x.Email==email);}var phone=NormalizePhone(value);return db.TaiKhoan.SingleOrDefaultAsync(x=>x.SoDienThoai==phone);}
 public Task<TaiKhoan> LayTaiKhoan(long id)=>Find<TaiKhoan>(id);
 public Task<bool> DangHoatDong(long id)=>db.TaiKhoan.AnyAsync(x=>x.MaTaiKhoan==id&&x.TrangThai==TrangThaiTaiKhoan.HoatDong);
 public Task<bool> SoDienThoaiDaDung(string phone,long? except=null)=>db.TaiKhoan.AnyAsync(x=>x.SoDienThoai==phone&&(except==null||x.MaTaiKhoan!=except));
 public async Task CapNhatSoDienThoai(long id,string? phone){var a=await LayTaiKhoan(id);a.SoDienThoai=NormalizePhone(phone);}
 public async Task NgungTaiKhoan(long id){var a=await LayTaiKhoan(id);a.TrangThai=TrangThaiTaiKhoan.DaNgung;a.ThoiDiemNgung=Clock.BayGioUtc;a.DauPhien=Guid.NewGuid();}
 public async Task<List<SharedPerson>> LayThongTinChiaSe(IEnumerable<long> ids,long? viewer){Require(viewer==null||viewer==UserId,"Không có quyền dùng danh tính khác.",403);var result=new List<SharedPerson>();foreach(var id in ids.Distinct())result.Add(await LayThongTinChiaSe(id));return result;}
 public async Task CapNhatTruong(long id,ProfileInput i){var p=await LayHoSo(id);p.TenHienThi=i.Name;p.GioiThieu=i.Bio;p.NoiSinhSong=i.City;p.SoThich=i.Interests;p.ThongTinChiaSe=Enum.Parse<ThongTinChiaSe>(i.Privacy);p.ThoiDiemCapNhat=Clock.BayGioUtc;}
 public async Task<NhaCungCapDichVu> LayNhaCungCapCuaTaiKhoan(long user)=>await db.NhaCungCapDichVu.SingleOrDefaultAsync(x=>x.MaTaiKhoan==user)??throw new BusinessException("Không tìm thấy nhà cung cấp.",404);
 public async Task KiemTraTrangThaiNCC(long user,string operation){var p=await LayNhaCungCapCuaTaiKhoan(user);Require(await ((ITaiKhoan)this).DangHoatDong(user),"Tài khoản đã ngừng hoạt động.",422);if(operation is "DangDichVu" or "TiepNhanYeuCau")Ensure(await ((INhaCungCap)this).DangHoatDong(p.MaNhaCungCap),"0601-01");else if(operation=="GuiGiaiTrinh")Ensure(p.TrangThaiQuyenHoatDong is TrangThaiQuyenHoatDong.TamDinhChi or TrangThaiQuyenHoatDong.DaThuHoi,"1301-07");else if(operation=="BoSungHoSo")Ensure(p.TrangThaiXacMinh is TrangThaiXacMinh.CanBoSung or TrangThaiXacMinh.TuChoi or TrangThaiXacMinh.DaThuHoi,"1002-05");}
 Task<bool> INhaCungCap.DangHoatDong(long id)=>db.NhaCungCapDichVu.AnyAsync(x=>x.MaNhaCungCap==id&&x.TrangThaiXacMinh==TrangThaiXacMinh.DaXacMinh&&x.TrangThaiQuyenHoatDong==TrangThaiQuyenHoatDong.DangHoatDong&&db.TaiKhoan.Any(a=>a.MaTaiKhoan==x.MaTaiKhoan&&a.TrangThai==TrangThaiTaiKhoan.HoatDong));
 public async Task CapNhatTrangThaiXacMinh(long id,TrangThaiXacMinh state){var p=await Find<NhaCungCapDichVu>(id);p.TrangThaiXacMinh=state;}
 public async Task CapNhatQuyen(long id,TrangThaiQuyenHoatDong? state){var p=await Find<NhaCungCapDichVu>(id);p.TrangThaiQuyenHoatDong=state;}
 public Task<List<NhaCungCapDichVu>> LayDanhSach(string? filter)=>db.NhaCungCapDichVu.Where(x=>filter==null||filter=="TatCa"||x.TrangThaiQuyenHoatDong.ToString()==filter).OrderBy(x=>x.TenDonVi).ToListAsync();
 public Task<NhaCungCapDichVu> LayThongTinCongKhai(long id)=>Find<NhaCungCapDichVu>(id);
 public Task<List<HoSoXacMinhNhaCungCap>> LayHangDoi(TrangThaiHoSoXacMinh state)=>db.HoSoXacMinhNhaCungCap.Where(x=>x.TrangThaiXuLy==state).OrderBy(x=>x.ThoiDiemNop).ToListAsync();
 Task<HoSoXacMinhNhaCungCap> IHoSoXacMinh.LayHoSo(long id)=>Find<HoSoXacMinhNhaCungCap>(id);
 public async Task CapNhatTrangThai(long id,TrangThaiHoSoXacMinh state){var h=await Find<HoSoXacMinhNhaCungCap>(id);h.TrangThaiXuLy=state;}
 public Task ThemTaiLieu(long id,IEnumerable<TepDaLuu> files,int turn){foreach(var f in files)db.Add(new TaiLieuXacMinh{MaHoSo=id,TenTepGoc=f.Name,DuongDan=f.Path,LoaiTep=Enum.Parse<LoaiTep>(f.Type),KichThuoc=f.Size,LanNop=turn});return Task.CompletedTask;}
 public async Task<HoSoXacMinhNhaCungCap> TaoHoSoMoi(long id,string content,IEnumerable<TepDaLuu> files){var p=await Find<NhaCungCapDichVu>(id);var h=new HoSoXacMinhNhaCungCap{MaNhaCungCap=id,NoiDungDangKy=content,NguoiDaiDien=p.NguoiLienHe,TrangThaiXuLy=TrangThaiHoSoXacMinh.ChoXacMinh};db.Add(h);await db.SaveChangesAsync();await ThemTaiLieu(h.MaHoSo,files,0);return h;}
}
