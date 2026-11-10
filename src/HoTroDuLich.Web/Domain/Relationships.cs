namespace HoTroDuLich.Web.Domain;
public partial class BanCapNhatDichVu {
 public DichVuDuLich DichVuDuLich {get;set;}=null!;
 public DiaDiemDuLich? DiaDiemDuLich {get;set;}
 public TaiKhoan? TaiKhoan {get;set;}
}
public partial class DichVuDuLich {
 public ICollection<BanCapNhatDichVu> BanCapNhatDichVu {get;set;}=new List<BanCapNhatDichVu>();
 public NhaCungCapDichVu NhaCungCapDichVu {get;set;}=null!;
 public DiaDiemDuLich? DiaDiemDuLich {get;set;}
 public QuyetDinhQuyenHoatDong? QuyetDinhQuyenHoatDong {get;set;}
 public ICollection<HoatDongLichTrinh> HoatDongLichTrinh {get;set;}=new List<HoatDongLichTrinh>();
 public ICollection<YeuCauDatDichVu> YeuCauDatDichVu {get;set;}=new List<YeuCauDatDichVu>();
}
public partial class DiaDiemDuLich {
 public ICollection<BanCapNhatDichVu> BanCapNhatDichVu {get;set;}=new List<BanCapNhatDichVu>();
 public ICollection<DichVuDuLich> DichVuDuLich {get;set;}=new List<DichVuDuLich>();
 public ICollection<HoatDongLichTrinh> HoatDongLichTrinh {get;set;}=new List<HoatDongLichTrinh>();
}
public partial class TaiKhoan {
 public ICollection<BanCapNhatDichVu> BanCapNhatDichVu {get;set;}=new List<BanCapNhatDichVu>();
 public ICollection<DeNghiKetNoiDongHanh> DeNghiKetNoiDongHanh {get;set;}=new List<DeNghiKetNoiDongHanh>();
 public ICollection<GiaiTrinhNhaCungCap> GiaiTrinhNhaCungCap {get;set;}=new List<GiaiTrinhNhaCungCap>();
 public ICollection<HoSoNguoiDung> HoSoNguoiDung {get;set;}=new List<HoSoNguoiDung>();
 public ICollection<KeHoachChuyenDi> KeHoachChuyenDi {get;set;}=new List<KeHoachChuyenDi>();
 public ICollection<NhaCungCapDichVu> NhaCungCapDichVu {get;set;}=new List<NhaCungCapDichVu>();
 public ICollection<NhatKyThaoTac> NhatKyThaoTac {get;set;}=new List<NhatKyThaoTac>();
 public ICollection<NhuCauDongHanh> NhuCauDongHanh {get;set;}=new List<NhuCauDongHanh>();
 public ICollection<QuyetDinhQuyenHoatDong> QuyetDinhQuyenHoatDong {get;set;}=new List<QuyetDinhQuyenHoatDong>();
 public ICollection<QuyetDinhXacMinh> QuyetDinhXacMinh {get;set;}=new List<QuyetDinhXacMinh>();
 public ICollection<ThoaThuanDongHanh> ThoaThuanDongHanh_NguoiDeXuat {get;set;}=new List<ThoaThuanDongHanh>();
 public ICollection<ThoaThuanDongHanh> ThoaThuanDongHanh_NguoiXacNhan {get;set;}=new List<ThoaThuanDongHanh>();
 public ICollection<ThoaThuanDongHanh> ThoaThuanDongHanh_NguoiKetThuc {get;set;}=new List<ThoaThuanDongHanh>();
 public ICollection<ThongBao> ThongBao {get;set;}=new List<ThongBao>();
 public ICollection<YeuCauDatDichVu> YeuCauDatDichVu {get;set;}=new List<YeuCauDatDichVu>();
}
public partial class DeNghiDieuChinhDichVu {
 public YeuCauDatDichVu YeuCauDatDichVu {get;set;}=null!;
}
public partial class YeuCauDatDichVu {
 public ICollection<DeNghiDieuChinhDichVu> DeNghiDieuChinhDichVu {get;set;}=new List<DeNghiDieuChinhDichVu>();
 public ICollection<KetQuaCungCapDichVu> KetQuaCungCapDichVu {get;set;}=new List<KetQuaCungCapDichVu>();
 public TaiKhoan TaiKhoan {get;set;}=null!;
 public DichVuDuLich DichVuDuLich {get;set;}=null!;
}
public partial class DeNghiKetNoiDongHanh {
 public NhuCauDongHanh NhuCauDongHanh {get;set;}=null!;
 public TaiKhoan TaiKhoan {get;set;}=null!;
 public ICollection<ThoaThuanDongHanh> ThoaThuanDongHanh {get;set;}=new List<ThoaThuanDongHanh>();
}
public partial class NhuCauDongHanh {
 public ICollection<DeNghiKetNoiDongHanh> DeNghiKetNoiDongHanh {get;set;}=new List<DeNghiKetNoiDongHanh>();
 public TaiKhoan TaiKhoan {get;set;}=null!;
}
public partial class NhaCungCapDichVu {
 public ICollection<DichVuDuLich> DichVuDuLich {get;set;}=new List<DichVuDuLich>();
 public ICollection<HoSoXacMinhNhaCungCap> HoSoXacMinhNhaCungCap {get;set;}=new List<HoSoXacMinhNhaCungCap>();
 public TaiKhoan TaiKhoan {get;set;}=null!;
 public ICollection<QuyetDinhQuyenHoatDong> QuyetDinhQuyenHoatDong {get;set;}=new List<QuyetDinhQuyenHoatDong>();
}
public partial class QuyetDinhQuyenHoatDong {
 public ICollection<DichVuDuLich> DichVuDuLich {get;set;}=new List<DichVuDuLich>();
 public ICollection<GiaiTrinhNhaCungCap> GiaiTrinhNhaCungCap {get;set;}=new List<GiaiTrinhNhaCungCap>();
 public NhaCungCapDichVu NhaCungCapDichVu {get;set;}=null!;
 public TaiKhoan TaiKhoan {get;set;}=null!;
}
public partial class GiaiTrinhNhaCungCap {
 public QuyetDinhQuyenHoatDong QuyetDinhQuyenHoatDong {get;set;}=null!;
 public TaiKhoan? TaiKhoan {get;set;}
 public ICollection<TaiLieuGiaiTrinh> TaiLieuGiaiTrinh {get;set;}=new List<TaiLieuGiaiTrinh>();
}
public partial class HoatDongLichTrinh {
 public KeHoachChuyenDi KeHoachChuyenDi {get;set;}=null!;
 public DiaDiemDuLich? DiaDiemDuLich {get;set;}
 public DichVuDuLich? DichVuDuLich {get;set;}
 public ICollection<TinhHuongHoatDong> TinhHuongHoatDong {get;set;}=new List<TinhHuongHoatDong>();
}
public partial class KeHoachChuyenDi {
 public ICollection<HoatDongLichTrinh> HoatDongLichTrinh {get;set;}=new List<HoatDongLichTrinh>();
 public TaiKhoan TaiKhoan {get;set;}=null!;
 public ICollection<TinhHuongPhatSinh> TinhHuongPhatSinh {get;set;}=new List<TinhHuongPhatSinh>();
}
public partial class HoSoNguoiDung {
 public TaiKhoan TaiKhoan {get;set;}=null!;
}
public partial class HoSoXacMinhNhaCungCap {
 public NhaCungCapDichVu NhaCungCapDichVu {get;set;}=null!;
 public ICollection<QuyetDinhXacMinh> QuyetDinhXacMinh {get;set;}=new List<QuyetDinhXacMinh>();
 public ICollection<TaiLieuXacMinh> TaiLieuXacMinh {get;set;}=new List<TaiLieuXacMinh>();
}
public partial class KetQuaCungCapDichVu {
 public YeuCauDatDichVu YeuCauDatDichVu {get;set;}=null!;
}
public partial class NhatKyThaoTac {
 public TaiKhoan? TaiKhoan {get;set;}
}
public partial class PhuongAnXuLy {
 public TinhHuongPhatSinh TinhHuongPhatSinh {get;set;}=null!;
}
public partial class TinhHuongPhatSinh {
 public ICollection<PhuongAnXuLy> PhuongAnXuLy {get;set;}=new List<PhuongAnXuLy>();
 public ICollection<TinhHuongHoatDong> TinhHuongHoatDong {get;set;}=new List<TinhHuongHoatDong>();
 public KeHoachChuyenDi KeHoachChuyenDi {get;set;}=null!;
}
public partial class QuyetDinhXacMinh {
 public HoSoXacMinhNhaCungCap HoSoXacMinhNhaCungCap {get;set;}=null!;
 public TaiKhoan TaiKhoan {get;set;}=null!;
}
public partial class TaiLieuGiaiTrinh {
 public GiaiTrinhNhaCungCap GiaiTrinhNhaCungCap {get;set;}=null!;
}
public partial class TaiLieuXacMinh {
 public HoSoXacMinhNhaCungCap HoSoXacMinhNhaCungCap {get;set;}=null!;
}
public partial class ThoaThuanDongHanh {
 public DeNghiKetNoiDongHanh DeNghiKetNoiDongHanh {get;set;}=null!;
 public TaiKhoan NguoiDeXuat {get;set;}=null!;
 public TaiKhoan? NguoiXacNhan {get;set;}
 public TaiKhoan? NguoiKetThuc {get;set;}
}
public partial class ThongBao {
 public TaiKhoan TaiKhoan {get;set;}=null!;
}
public partial class TinhHuongHoatDong {
 public TinhHuongPhatSinh TinhHuongPhatSinh {get;set;}=null!;
 public HoatDongLichTrinh HoatDongLichTrinh {get;set;}=null!;
}