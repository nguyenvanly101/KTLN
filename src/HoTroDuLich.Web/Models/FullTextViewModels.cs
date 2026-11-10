using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace HoTroDuLich.Web.Models;
/// <summary>Request model for the TraCuuThongTin package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class TraCuuThongTinViewModel {
 public long? PlanId {get;set;}
 [MaxLength(100)] public string? TuKhoa {get;set;}
 public string? KhuVuc {get;set;}
 public string? Loai {get;set;}
 public decimal? GiaTu {get;set;}
 public decimal? GiaDen {get;set;}
 [Range(1,int.MaxValue)] public int Trang {get;set;}=1;
}
/// <summary>Request model for the XemChiTietThongTin package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class XemChiTietThongTinViewModel {
 public long? PlanId {get;set;}
 public string Loai {get;set;}="";
 public long MaMuc {get;set;}
}
/// <summary>Request model for the DanhSachKeHoach package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class DanhSachKeHoachViewModel {
 [Range(1,int.MaxValue)] public int Trang {get;set;}=1;
 public long MaKeHoach {get;set;}
}
/// <summary>Request model for the KhoiTaoKeHoach package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class KhoiTaoKeHoachViewModel:PlanInput {

}
/// <summary>Request model for the QuanLyLichTrinh package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class QuanLyLichTrinhViewModel:ActivityInput {

}
/// <summary>Request model for the XacNhanKeHoach package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class XacNhanKeHoachViewModel:CommandInput {

}
/// <summary>Request model for the GhiNhanTinhHuong package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class GhiNhanTinhHuongViewModel:IncidentInput {
 public long MaKeHoach {get;set;}
}
/// <summary>Request model for the TheoDoiTinhHuong package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class TheoDoiTinhHuongViewModel {
 public long MaKeHoach {get;set;}
 [Range(1,int.MaxValue)] public int Trang {get;set;}=1;
 public long MaTinhHuong {get;set;}
}
/// <summary>Request model for the XuLyPhuongAn package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class XuLyPhuongAnViewModel:CommandInput {

}
/// <summary>Request model for the TimNhuCau package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class TimNhuCauViewModel {
 public string? NoiDen {get;set;}
 public DateOnly? TuNgay {get;set;}
 public DateOnly? DenNgay {get;set;}
 public decimal? NganSachToiDa {get;set;}
 public string? HinhThucDi {get;set;}
 [Range(1,int.MaxValue)] public int Trang {get;set;}=1;
}
/// <summary>Request model for the XemNhuCauChiaSe package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class XemNhuCauChiaSeViewModel {
 public long MaNhuCau {get;set;}
}
/// <summary>Request model for the SoanNhuCau package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class SoanNhuCauViewModel:NeedInput {

}
/// <summary>Request model for the CongBoNhuCau package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class CongBoNhuCauViewModel:NeedInput {

}
/// <summary>Request model for the GuiDeNghiKetNoi package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class GuiDeNghiKetNoiViewModel:CommandInput {
 public long MaNhuCau {get;set;}
}
/// <summary>Request model for the PhanHoiDeNghi package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class PhanHoiDeNghiViewModel:CommandInput {
 public string Loai {get;set;}="nhan";
 [Range(1,int.MaxValue)] public int Trang {get;set;}=1;
 public long MaDeNghi {get;set;}
}
/// <summary>Request model for the LapThoaThuan package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class LapThoaThuanViewModel:AgreementInput {
 public long MaKetNoi {get;set;}
}
/// <summary>Request model for the XacNhanThoaThuan package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class XacNhanThoaThuanViewModel:CommandInput {
 public long MaThoaThuan {get;set;}
}
/// <summary>Request model for the KetThucThoaThuan package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class KetThucThoaThuanViewModel:CommandInput {

}
/// <summary>Request model for the DanhSachDichVu package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class DanhSachDichVuViewModel {
 [Range(1,int.MaxValue)] public int Trang {get;set;}=1;
 public long MaDichVu {get;set;}
}
/// <summary>Request model for the DangDichVu package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class DangDichVuViewModel:ServiceInput {
 public long? MaDichVu {get;set;}
}
/// <summary>Request model for the CapNhatDichVu package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class CapNhatDichVuViewModel:ServiceInput {

}
/// <summary>Request model for the DanhSachChoDuyet package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class DanhSachChoDuyetViewModel {
 public string? Q {get;set;}
 public string Status {get;set;}="ChoDuyet";
 [Range(1,int.MaxValue)] public int Trang {get;set;}=1;
 public long MaBanCapNhat {get;set;}
}
/// <summary>Request model for the QuyetDinhDuyet package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class QuyetDinhDuyetViewModel:CommandInput {

}
/// <summary>Request model for the GuiYeuCauDat package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class GuiYeuCauDatViewModel:BookingInput {
 public long MaDichVu {get;set;}
}
/// <summary>Request model for the TheoDoiYeuCau package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class TheoDoiYeuCauViewModel:CommandInput {
 public string? TrangThai {get;set;}
 [Range(1,int.MaxValue)] public int Trang {get;set;}=1;
 public long MaYeuCau {get;set;}
}
/// <summary>Request model for the XuLyYeuCauDat package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class XuLyYeuCauDatViewModel:CommandInput {
 public string? TrangThai {get;set;}
 [Range(1,int.MaxValue)] public int Trang {get;set;}=1;
 public long MaYeuCau {get;set;}
}
/// <summary>Request model for the GuiDeNghiDieuChinh package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class GuiDeNghiDieuChinhViewModel:ChangeInput {
 public long MaYeuCau {get;set;}
}
/// <summary>Request model for the XuLyDeNghiDieuChinh package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class XuLyDeNghiDieuChinhViewModel:CommandInput {
 public long MaYeuCau {get;set;}
 public long MaDeNghi {get;set;}
}
/// <summary>Request model for the GhiNhanKetQua package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class GhiNhanKetQuaViewModel:CommandInput {
 public long MaYeuCau {get;set;}
}
/// <summary>Request model for the PhanAnhKetQua package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class PhanAnhKetQuaViewModel:CommandInput {
 public long MaYeuCau {get;set;}
}
/// <summary>Request model for the DangKyTaiKhoan package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class DangKyTaiKhoanViewModel:RegisterInput {

}
/// <summary>Request model for the XetHoSoXacMinh package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class XetHoSoXacMinhViewModel:CommandInput {
 public string Tab {get;set;}="cho";
 [Range(1,int.MaxValue)] public int Trang {get;set;}=1;
 public long MaHoSo {get;set;}
}
/// <summary>Request model for the BoSungHoSo package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class BoSungHoSoViewModel:CommandInput {

}
/// <summary>Request model for the XacThucDangNhap package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class XacThucDangNhapViewModel:LoginInput {

}
/// <summary>Request model for the QuanLyPhien package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class QuanLyPhienViewModel:PasswordChangeInput {

}
/// <summary>Request model for the CapNhatHoSo package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class CapNhatHoSoViewModel:ProfileInput {

}
/// <summary>Request model for the TraCuuNhaCungCap package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class TraCuuNhaCungCapViewModel {
 public string Loc {get;set;}="TatCa";
 [Range(1,int.MaxValue)] public int Trang {get;set;}=1;
 public long MaNCC {get;set;}
}
/// <summary>Request model for the QuyetDinhQuyenHoatDong package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class QuyetDinhQuyenHoatDongViewModel:CommandInput {
 public long MaNCC {get;set;}
 public string ThaoTac {get;set;}="";
}
/// <summary>Request model for the GiaiTrinh package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class GiaiTrinhViewModel:CommandInput {

}
/// <summary>Request model for the KiemTraDieuKienNgung package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class KiemTraDieuKienNgungViewModel:DeactivationInput {

}
/// <summary>Request model for the ThucHienNgung package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class ThucHienNgungViewModel {

}
/// <summary>Request model for the ThongBao package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class ThongBaoViewModel {
 [Range(1,int.MaxValue)] public int Trang {get;set;}=1;
}
/// <summary>Request model for the Tep package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>
[ValidateNever]
public class TepViewModel {
 public string Loai {get;set;}="";
 public long MaTaiLieu {get;set;}
}
