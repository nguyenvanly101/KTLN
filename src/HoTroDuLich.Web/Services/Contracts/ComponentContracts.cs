using HoTroDuLich.Web.Domain;
using HoTroDuLich.Web.Models;
namespace HoTroDuLich.Web.Services;

public record TieuChiTraCuu(string? TuKhoa=null,string? KhuVuc=null,string? Loai=null,decimal? GiaTu=null,decimal? GiaDen=null,int Trang=1);
public interface IDiaDiem {Task<List<DiaDiemDuLich>> TimDiaDiem(TieuChiTraCuu tieuChi);Task<DiaDiemDuLich> LayChiTiet(long maDiaDiem);Task<bool> KiemTraConHienThi(long maDiaDiem);Task<List<string>> LayDanhMucKhuVuc();}
public interface IDichVu {
 Task<List<DichVuDuLich>> TimDichVu(TieuChiTraCuu tieuChi);Task<DichVuDuLich> LayChiTiet(long maDichVu);Task<bool> DangTiepNhan(long maDichVu);Task<bool> KiemTraConCongBo(long maDichVu);Task<BanCapNhatDichVu> LayBanCapNhat(long maBanCapNhat);
 Task CongBo(long maDichVu,BanCapNhatDichVu ban);Task TuChoiBanChoDuyet(long maNCC,long maAdmin,string lyDo);Task KhoaTiepNhan(long maNCC,long maQuyetDinh);Task MoTiepNhan(long maQuyetDinh);Task NgungCongBo(long maNCC);Task<int> DemDichVuTiepNhan(long maNCC);
}
public interface IKeHoach {Task<bool> LaChuKeHoach(long maTaiKhoan,long maKeHoach);Task<List<HoatDongLichTrinh>> LayHoatDong(long maKeHoach);}
public interface INhuCauDongHanh {IQueryable<NhuCauDongHanh> NguoiXemThayDuoc(long? maNguoiXem,DateOnly homNay);Task<(NhuCauDongHanh NhuCau,TaiKhoan Chu)> LayNhuCauVaChu(long maNhuCau);Task DungCongBoNhuCau(long maTaiKhoan);}
public interface IDeNghiKetNoi {Task<DeNghiKetNoiDongHanh> LayKetNoi(long maKetNoi,long maTaiKhoan);Task HetHieuLucDeNghiCho(long maTaiKhoan);Task<bool> DaGuiDeNghi(long maNguoiGui,long maNhuCau);}
public interface IThoaThuanDongHanh {Task<bool> CoThoaThuanHieuLuc(long maTaiKhoan);Task DungDeXuatCho(long maTaiKhoan);}
public interface IYeuCauDatDichVu {
 Task<YeuCauDatDichVu> LayYeuCauCuaKhach(long maYeuCau,long maTaiKhoan);Task<YeuCauDatDichVu> LayYeuCauCuaNCC(long maYeuCau,long maNCC);Task CapNhatTrangThai(long maYeuCau,TrangThaiYeuCau trangThai,string? lyDo);Task CapNhatDieuKien(long maYeuCau,BookingInput dieuKien);Task ApDungDieuKienMoi(long maYeuCau,DeNghiDieuChinhDichVu dieuKien);Task DungYeuCauDangCho(long maNCC,string lyDo);Task<int> DemYeuCauDangCho(long maNCC);Task<bool> CoNghiaVuMo(long maTaiKhoan,VaiTro vaiTro);
}
public interface ITaiKhoan {Task<TaiKhoan?> TimTheoDinhDanh(string dinhDanh);Task<TaiKhoan> LayTaiKhoan(long maTaiKhoan);Task<bool> DangHoatDong(long maTaiKhoan);Task<bool> SoDienThoaiDaDung(string sdt,long? ngoaiTru=null);Task CapNhatSoDienThoai(long maTaiKhoan,string? sdt);Task NgungTaiKhoan(long maTaiKhoan);}
public interface IHoSoNguoiDung {Task<HoSoNguoiDung> LayHoSo(long maTaiKhoan);Task<List<SharedPerson>> LayThongTinChiaSe(IEnumerable<long> dsMaTaiKhoan,long? maNguoiXem);Task CapNhatTruong(long maTaiKhoan,ProfileInput giaTri);}
public interface INhaCungCap {Task<NhaCungCapDichVu> LayNhaCungCapCuaTaiKhoan(long maTaiKhoan);Task KiemTraTrangThaiNCC(long maTaiKhoan,string thaoTac);Task<bool> DangHoatDong(long maNCC);Task CapNhatTrangThaiXacMinh(long maNCC,TrangThaiXacMinh trangThai);Task CapNhatQuyen(long maNCC,TrangThaiQuyenHoatDong? trangThai);Task<List<NhaCungCapDichVu>> LayDanhSach(string? boLoc);Task<NhaCungCapDichVu> LayThongTinCongKhai(long maNCC);}
public interface IHoSoXacMinh {Task<List<HoSoXacMinhNhaCungCap>> LayHangDoi(TrangThaiHoSoXacMinh trangThai);Task<HoSoXacMinhNhaCungCap> LayHoSo(long maHoSo);Task CapNhatTrangThai(long maHoSo,TrangThaiHoSoXacMinh trangThai);Task ThemTaiLieu(long maHoSo,IEnumerable<TepDaLuu> dsTep,int lanNop);Task<HoSoXacMinhNhaCungCap> TaoHoSoMoi(long maNCC,string noiDung,IEnumerable<TepDaLuu> dsTep);}
public record TepDaLuu(string Name,string Path,string Type,int Size);
