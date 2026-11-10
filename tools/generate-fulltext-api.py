"""Generate the 43 MVC controllers from the reviewed FullText action contract."""
import json,re
from pathlib import Path
root=Path(__file__).resolve().parents[1]
actions=json.loads((root/'docs/fulltext-contract.json').read_text(encoding='utf-8'))['actions']
groups={}
def add(controller,component,action,signature,body):
 groups.setdefault(controller,{'component':component,'actions':{}})['actions'][action]=(signature,body)
def a(controller,component,action,signature,body):add(controller+'Controller',component,action,signature,body)
a('TraCuuThongTin','TimKiemThongTin','MoTimKiem','','ViewBag.Regions=await C.MoTimKiemAsync();var r=await C.TraCuuAsync(null,null,null,null,null);ViewBag.Places=r.Places;return V("Explore/Index",r.Services);')
a('TraCuuThongTin','TimKiemThongTin','TraCuu','string? tuKhoa,string? khuVuc,string? loai,decimal? giaTu,decimal? giaDen,int trang=1','ViewBag.Query=tuKhoa;ViewBag.Region=khuVuc;ViewBag.Type=DanhMuc.ChuanHoa(loai);ViewBag.Min=giaTu;ViewBag.Max=giaDen;ViewBag.Regions=await C.MoTimKiemAsync();var r=await C.TraCuuAsync(tuKhoa,khuVuc,loai,giaTu,giaDen,trang);ViewBag.Places=r.Places;ViewBag.SimplePage=trang;ViewBag.SimpleNext=r.Services.Count==20||r.Places.Count==20;return V("Explore/Index",r.Services);')
a('XemChiTietThongTin','TimKiemThongTin','XemChiTiet','string loai,long maMuc','var result=await C.XemChiTietAsync(loai,maMuc);ViewBag.Provider=result.NhaCungCap;ViewBag.Services=result.DichVuTaiDiem;return result.DiaDiem!=null?V("Explore/Place",result.DiaDiem):V("Explore/Detail",result.DichVu);')
a('DanhSachKeHoach','LapKeHoach','DanhSach','int trang=1','return V("Plans/Index",Page(await C.DanhSachAsync(),trang));')
a('DanhSachKeHoach','LapKeHoach','XemKeHoach','long maKeHoach','ViewBag.Activities=await C.LayHoatDong(maKeHoach);return V("Plans/Detail",await C.XemKeHoachAsync(maKeHoach));')
a('KhoiTaoKeHoach','LapKeHoach','MoKeHoachMoi','','return V("Plans/Edit",await C.MoKeHoachMoiAsync());')
a('KhoiTaoKeHoach','LapKeHoach','KhoiTaoKeHoach','PlanInput i','return Done("/ke-hoach/"+await C.KhoiTaoKeHoachAsync(i));')
a('KhoiTaoKeHoach','LapKeHoach','CapNhatThongTin','PlanInput i','S.Ensure(i.Id>0,"0201-02");return Done("/ke-hoach/"+await C.CapNhatThongTinAsync(i));')
for action in ['ThemHoatDong','SuaHoatDong']:
 a('QuanLyLichTrinh','LapKeHoach',action,'ActivityInput i',f'await C.{action}Async(i);return Done("/ke-hoach/"+i.PlanId);')
a('QuanLyLichTrinh','LapKeHoach','XoaHoatDong','CommandInput i','var activity=await S.Find<HoatDongLichTrinh>(i.Id);await C.XoaHoatDongAsync(i);return Done("/ke-hoach/"+activity.MaKeHoach);')
for action,decision in [('ChapNhan','DaChapNhan'),('LuuNhap','Nhap')]:
 a('XacNhanKeHoach','LapKeHoach',action,'CommandInput i',f'i.Decision="{decision}";await C.{action}Async(i);return Done("/ke-hoach/"+i.Id);')
a('GhiNhanTinhHuong','XuLyTinhHuong','MoGhiNhan','long maKeHoach','ViewBag.Plan=await S.OwnPlan(maKeHoach);ViewBag.Activities=await S.Activities(maKeHoach);return V("Support/Create",await C.MoGhiNhanAsync(maKeHoach));')
a('GhiNhanTinhHuong','XuLyTinhHuong','GhiNhanTinhHuong','long maKeHoach,IncidentInput i','i.PlanId=maKeHoach;await C.GhiNhanTinhHuongAsync(i);return Done("/ke-hoach/"+maKeHoach+"/tinh-huong");')
a('TheoDoiTinhHuong','XuLyTinhHuong','DanhSach','long maKeHoach,int trang=1','ViewBag.Plan=await S.OwnPlan(maKeHoach);ViewBag.Plans=await S.Plans();return V("Support/Index",Page(await C.DanhSachAsync(maKeHoach),trang));')
a('TheoDoiTinhHuong','XuLyTinhHuong','XemTinhHuong','long maKeHoach,long maTinhHuong','var t=await C.LayTinhHuongAsync(maKeHoach,maTinhHuong);ViewBag.Options=await C.XemTinhHuongAsync(maTinhHuong);return V("Support/Detail",t);')
for action in ['GhiPhuongAn','GhiKetQua']:
 a('XuLyPhuongAn','XuLyTinhHuong',action,'long maKeHoach,CommandInput i',f'await C.LayTinhHuongAsync(maKeHoach,i.Id);await C.{action}Async(i);return Done("/ke-hoach/"+maKeHoach+"/tinh-huong/"+i.Id);')
a('TimNhuCau','TimNguoiDongHanh','MoTimKiem','','await C.MoTimKiemAsync();ViewBag.Names=await S.DisplayNames();return V("Companions/Index",await C.TimNhuCauAsync(null,null,null,null,null));')
a('TimNhuCau','TimNguoiDongHanh','TimNhuCau','string? noiDen,DateOnly? tuNgay,DateOnly? denNgay,decimal? nganSachToiDa,string? hinhThucDi,int trang=1','ViewBag.Names=await S.DisplayNames();ViewBag.SimplePage=trang;var rows=await C.TimNhuCauAsync(noiDen,tuNgay,denNgay,nganSachToiDa,hinhThucDi,trang);ViewBag.SimpleNext=rows.Count==20;return V("Companions/Index",rows);')
a('XemNhuCauChiaSe','TimNguoiDongHanh','XemChiTiet','long maNhuCau','var n=await C.XemChiTietAsync(maNhuCau);ViewBag.Person=await S.SharedProfile(n.MaTaiKhoan);ViewBag.Names=await S.DisplayNames();return V("Companions/Detail",n);')
a('SoanNhuCau','CongBoNhuCau','MoBieuMau','','return V("Companions/Edit",await C.MoBieuMauAsync());')
a('SoanNhuCau','CongBoNhuCau','XemTruoc','NeedInput i','await C.XemTruocAsync(i);return V("Companions/Preview",i);')
a('CongBoNhuCau','CongBoNhuCau','CongBo','NeedInput i','await C.CongBoAsync(i);return Done("/Companions/Mine","Đã công bố nhu cầu với phạm vi chia sẻ đã chọn.");')
a('GuiDeNghiKetNoi','KetNoiDongHanh','MoDeNghi','long maNhuCau','await C.MoDeNghiAsync(maNhuCau);var n=await S.Need(maNhuCau);ViewBag.Person=await S.SharedProfile(n.MaTaiKhoan);ViewBag.Names=await S.DisplayNames();return V("Companions/Detail",n);')
a('GuiDeNghiKetNoi','KetNoiDongHanh','GuiDeNghi','CommandInput i','await C.GuiDeNghiAsync(i);return Done("/dong-hanh/de-nghi","Đã gửi đề nghị kết nối, chờ người nhận phản hồi.");')
a('PhanHoiDeNghi','KetNoiDongHanh','DanhSach','string loai="nhan",int trang=1','S.Require(loai is "nhan" or "gui","Bộ lọc không hợp lệ.",400);var rows=await C.DanhSachAsync();rows=rows.Where(x=>loai=="gui"?x.MaNguoiGui==S.UserId:x.MaNguoiGui!=S.UserId).ToList();ViewBag.Names=await S.DisplayNames();var owners=new Dictionary<long,long>();var destinations=new Dictionary<long,string>();foreach(var item in rows){owners[item.MaDeNghi]=await C.LayChuNhuCauAsync(item.MaDeNghi);destinations[item.MaDeNghi]=await C.LayNoiDenAsync(item.MaDeNghi);}ViewBag.Owners=owners;ViewBag.Destinations=destinations;return V("Companions/Connections",Page(rows,trang));')
a('PhanHoiDeNghi','KetNoiDongHanh','XemDeNghi','long maDeNghi','var connection=await C.XemDeNghiAsync(maDeNghi);ViewBag.Sender=await S.SharedProfile(connection.MaNguoiGui);ViewBag.Owner=await S.SharedProfile(await C.LayChuNhuCauAsync(maDeNghi));ViewBag.Destination=await C.LayNoiDenAsync(maDeNghi);return V("Companions/Connection",connection);')
a('PhanHoiDeNghi','KetNoiDongHanh','PhanHoi','CommandInput i','if(i.Decision=="ChapNhan")i.Decision="DaChapNhan";if(i.Decision=="Rut")i.Decision="DaRut";await C.PhanHoiAsync(i);return Done("/dong-hanh/de-nghi/xem-de-nghi/"+i.Id);')
a('LapThoaThuan','ThoaThuanDongHanh','MoTaoThoaThuan','long maKetNoi','return V("Companions/Propose",await C.MoTaoThoaThuanAsync(maKetNoi));')
a('LapThoaThuan','ThoaThuanDongHanh','GuiDeNghiThoaThuan','AgreementInput i','i.Kind="NoiDung";await C.GuiDeNghiThoaThuanAsync(i);return Done("/dong-hanh/thoa-thuan/xem-de-nghi/"+(await S.Agreements(i.ConnectionId)).First().MaThoaThuan);')
a('XacNhanThoaThuan','ThoaThuanDongHanh','XemDeNghi','long maThoaThuan','var t=await C.XemDeNghiAsync(maThoaThuan);return await AgreementPage(t.MaDeNghi);')
a('XacNhanThoaThuan','ThoaThuanDongHanh','XacNhan','CommandInput i','if(i.Decision=="XacNhan")i.Decision="DaXacNhan";await C.XacNhanAsync(i);var t=await C.XemDeNghiAsync(i.Id);return Done("/dong-hanh/thoa-thuan/xem-de-nghi/"+t.MaThoaThuan);')
a('KetThucThoaThuan','ThoaThuanDongHanh','Rut','CommandInput i','await C.RutAsync(i);return Done("/dong-hanh/thoa-thuan/xem-de-nghi/"+i.Id);')
a('KetThucThoaThuan','ThoaThuanDongHanh','DeNghiNgung','CommandInput i','await C.DeNghiNgungAsync(i);return Done("/dong-hanh/thoa-thuan/xem-de-nghi/"+(await S.Agreements(i.ConnectionId)).First().MaThoaThuan);')
a('DanhSachDichVu','QuanLyDichVu','DanhSach','int trang=1','ViewBag.Provider=await S.MyProvider();ViewBag.CanSubmit=await C.KiemTraDieuKienDangAsync();return V("Provider/Index",Page(await C.DanhSachAsync(),trang));')
a('DanhSachDichVu','QuanLyDichVu','XemDichVu','long maDichVu','await C.XemDichVuAsync(maDichVu);ViewBag.CanSubmit=await C.KiemTraDieuKienDangAsync();ViewBag.Places=await S.VisiblePlaces();ViewBag.History=await C.LayLichSuAsync(maDichVu);return V("Provider/Edit",await C.MoBieuMauAsync(maDichVu,false));')
a('DangDichVu','QuanLyDichVu','MoBieuMau','long? maDichVu','ViewBag.CanSubmit=await C.KiemTraDieuKienDangAsync();ViewBag.Places=await S.VisiblePlaces();if(maDichVu.HasValue)ViewBag.History=await C.LayLichSuAsync(maDichVu.Value);return V("Provider/Edit",await C.MoBieuMauAsync(maDichVu??0));')
a('DangDichVu','QuanLyDichVu','GuiDang','ServiceInput i','if(i.Id>0)S.Ensure((await C.XemDichVuAsync(i.Id)).TrangThaiCongBo.ToString()=="Nhap","0601-02");await C.GuiDangAsync(i);return Done("/nha-cung-cap/dich-vu",i.Operation=="LuuNhap"?"Đã lưu bản nháp.":"Đã gửi dịch vụ, chờ xét duyệt.");')
a('CapNhatDichVu','QuanLyDichVu','CapNhatDichVu','ServiceInput i','await C.CapNhatDichVuAsync(i);return Done("/nha-cung-cap/dich-vu/"+i.Id,"Đã gửi bản cập nhật chờ duyệt.");')
a('DanhSachChoDuyet','XetDuyetDichVu','MoDanhSachChoDuyet','string? q,string status="ChoDuyet",int trang=1','return V("Admin/ServiceReviews",await C.MoDanhSachChoDuyetAsync(q,status,trang));')
a('DanhSachChoDuyet','XetDuyetDichVu','XemNoiDung','long maBanCapNhat','return V("Admin/Review",await C.XemNoiDungAsync(maBanCapNhat));')
a('QuyetDinhDuyet','XetDuyetDichVu','Duyet','CommandInput i','if(i.Decision=="Duyet")i.Decision="DaDuyet";await C.DuyetAsync(i);return Done("/quan-tri/xet-duyet-dich-vu/mo-danh-sach-cho-duyet");')
a('GuiYeuCauDat','DatDichVu','MoDatDichVu','long maDichVu','var d=await C.MoDatDichVuAsync(maDichVu);ViewBag.Service=d;return V("Bookings/Create",new BookingInput{ServiceId=maDichVu,Version=TravelService.Token(d.RowVersion)});')
a('GuiYeuCauDat','DatDichVu','KiemTraTruocGui','BookingInput i','ViewBag.Warnings=await C.KiemTraTruocGuiAsync(i);ViewBag.ServiceName=(await S.PublicService(i.ServiceId)).TenDichVu;return V("Bookings/Preview",i);')
a('GuiYeuCauDat','DatDichVu','GuiYeuCau','BookingInput i','return Done("/yeu-cau/"+await C.GuiYeuCauAsync(i),"Đã gửi yêu cầu, chờ nhà cung cấp phản hồi.");')
a('TheoDoiYeuCau','DatDichVu','DanhSachCuaToi','string? trangThai,int trang=1','ViewBag.ServiceNames=await C.LayTenDichVuAsync();return V("Bookings/Index",Page(FilterBookings(await C.DanhSachCuaToiAsync(),trangThai),trang));')
a('TheoDoiYeuCau','DatDichVu','ChiTiet','long maYeuCau','ViewBag.ServiceName=await C.LayTenDichVuCuaYeuCauAsync(maYeuCau);return V("Bookings/Detail",await C.ChiTietAsync(maYeuCau));')
a('TheoDoiYeuCau','DatDichVu','PhanHoiDeXuat','CommandInput i','await C.PhanHoiDeXuatAsync(i);return Done("/yeu-cau/"+i.Id);')
a('XuLyYeuCauDat','DatDichVu','DanhSachNCC','string? trangThai,int trang=1','ViewBag.ServiceNames=await C.LayTenDichVuAsync();return V("Bookings/Index",Page(FilterBookings(await C.DanhSachNCCAsync(),trangThai),trang));')
a('XuLyYeuCauDat','DatDichVu','XemYeuCau','long maYeuCau','ViewBag.ServiceName=await C.LayTenDichVuCuaYeuCauAsync(maYeuCau);return V("Bookings/Detail",await C.XemYeuCauAsync(maYeuCau));')
a('XuLyYeuCauDat','DatDichVu','ChapNhan','CommandInput i','if(i.Decision=="ChapNhan")i.Decision="DaXacNhan";await C.ChapNhanAsync(i);return Done("/nha-cung-cap/yeu-cau/"+i.Id);')
a('GuiDeNghiDieuChinh','DieuChinhDichVu','MoThayDoi','long maYeuCau','var y=await C.MoThayDoiAsync(maYeuCau);var pending=(await S.Changes(maYeuCau)).Any(x=>new[]{"ChoPhanHoi","DeXuatKhac"}.Contains(x.TrangThai.ToString()));if(pending||y.TrangThai.ToString()=="DeXuatKhac")return await ChangesPage(maYeuCau);if(y.TrangThai.ToString()=="ChoPhanHoi"){ViewBag.ServiceName=await S.BookingServiceName(maYeuCau);return V("Bookings/Edit",new BookingInput{Id=maYeuCau,ServiceId=y.MaDichVu,Version=TravelService.Token(y.RowVersion),Start=y.NgayBatDau,End=y.NgayKetThuc,People=y.SoNguoi,Quantity=y.SoLuong,ArrivalDeparture=y.ThoiGianDenDi,Selections=y.NoiDungLuaChon,Notes=y.GhiChu,Cost=y.ChiPhiDuKien});}return V("Bookings/Change",new ChangeInput{Id=maYeuCau,Version=TravelService.Token(y.RowVersion)});')
a('GuiDeNghiDieuChinh','DieuChinhDichVu','GuiDeNghi','long maYeuCau,ChangeInput i,BookingInput booking','i.Id=maYeuCau;booking.Id=maYeuCau;if(i.Kind=="SuaYeuCau")await C.SuaYeuCauAsync(booking);else if(i.Kind=="RutYeuCau"){var y=await S.Booking(maYeuCau);await S.BookingAction(new(){Id=maYeuCau,Version=i.Version,BusinessVersion=y.PhienBan,Decision="Rut",Reason=i.Reason});}else await C.GuiDeNghiAsync(i);return Done("/yeu-cau/"+maYeuCau);')
a('GuiDeNghiDieuChinh','DieuChinhDichVu','PhanHoiDeXuat','long maYeuCau,CommandInput i','var d=await S.Find<DeNghiDieuChinhDichVu>(i.Id);S.Ensure(d.MaYeuCau==maYeuCau,"0801-01");await C.PhanHoiDeXuatAsync(i);return Done("/yeu-cau/"+maYeuCau);')
a('XuLyDeNghiDieuChinh','DieuChinhDichVu','XemDeNghi','long maYeuCau,long maDeNghi','await C.XemDeNghiAsync(maYeuCau,maDeNghi);return await ChangesPage(maYeuCau);')
a('XuLyDeNghiDieuChinh','DieuChinhDichVu','PhanHoi','long maYeuCau,CommandInput i','await C.XemDeNghiAsync(maYeuCau,i.Id);await C.PhanHoiAsync(i);return Done("/nha-cung-cap/yeu-cau/"+maYeuCau);')
a('GhiNhanKetQua','KetQuaDichVu','MoGhiNhan','long maYeuCau','await C.MoGhiNhanAsync(maYeuCau);return await OutcomePage(maYeuCau);')
a('GhiNhanKetQua','KetQuaDichVu','LuuKetQua','long maYeuCau,CommandInput i','i.Id=maYeuCau;await C.LuuKetQuaAsync(i);return Done("/nha-cung-cap/yeu-cau/"+maYeuCau);')
a('PhanAnhKetQua','KetQuaDichVu','XemKetQua','long maYeuCau','var y=await S.Booking(maYeuCau);S.Ensure(y.MaTaiKhoan==S.UserId,"0901-01");await C.XemKetQuaAsync(maYeuCau);return await OutcomePage(maYeuCau);')
a('PhanAnhKetQua','KetQuaDichVu','GuiPhanAnh','long maYeuCau,CommandInput i','var r=await S.Find<KetQuaCungCapDichVu>(i.Id);S.Ensure(r.MaYeuCau==maYeuCau,"0901-01");i.Decision="PhanAnh";await C.GuiPhanAnhAsync(i);return Done("/yeu-cau/"+maYeuCau+"/ket-qua/xem-ket-qua");')
a('DangKyTaiKhoan','DangKy','MoDangKy','','return V("Account/Register",await C.MoDangKyAsync());')
a('DangKyTaiKhoan','DangKy','ChonVaiTro','string role','return V("Account/Register",await C.ChonVaiTroAsync(role));')
a('DangKyTaiKhoan','DangKy','DangKy','RegisterInput i','await C.DangKyAsync(i);return Done("/tai-khoan/dang-nhap/mo-dang-nhap","Đăng ký thành công. Nhà cung cấp cần chờ xác minh trước khi đăng dịch vụ.");')
a('XetHoSoXacMinh','XacMinhNhaCungCap','MoDanhSach','string tab="cho",int trang=1','S.Require(tab is "cho" or "bo-sung","Bộ lọc không hợp lệ.",400);return V("Admin/Verifications",await C.MoDanhSachAsync(null,tab=="cho"?"ChoXacMinh":"CanBoSung",trang));')
a('XetHoSoXacMinh','XacMinhNhaCungCap','XemHoSo','long maHoSo','return V("Admin/Verification",await C.XemHoSoAsync(maHoSo));')
a('XetHoSoXacMinh','XacMinhNhaCungCap','XacMinh','CommandInput i','await C.XacMinhAsync(i);return Done("/quan-tri/xac-minh/mo-danh-sach");')
a('BoSungHoSo','XacMinhNhaCungCap','HoSoCuaToi','','ViewBag.Provider=await S.MyProvider();var rows=await C.HoSoCuaToiAsync();var details=new List<(HoSoXacMinhNhaCungCap Profile,NhaCungCapDichVu Provider,List<TaiLieuXacMinh> Files,List<QuyetDinhXacMinh> Decisions)>();foreach(var row in rows)details.Add(await C.XemHoSoAsync(row.MaHoSo));return V("Provider/Verification",details);')
a('BoSungHoSo','XacMinhNhaCungCap','NhanBoSung','CommandInput i','var p=await S.MyProvider();if(p.TrangThaiXacMinh.ToString()=="DaThuHoi")await C.NopHoSoMoiAsync(i);else await C.NhanBoSungAsync(i);return Done("/nha-cung-cap/ho-so-xac-minh","Đã gửi hồ sơ, chờ xét duyệt.");')
a('XacThucDangNhap','DangNhap','MoDangNhap','string? returnUrl','ViewBag.Demo=HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue<bool>("Demo:Enabled");return V("Account/Login",await C.MoDangNhapAsync(returnUrl));')
a('XacThucDangNhap','DangNhap','DangNhap','LoginInput i','var account=await C.DangNhapAsync(i);await SessionAuthentication.SignIn(HttpContext,account);return LocalRedirect(account.PhaiDoiMatKhau?"/tai-khoan/doi-mat-khau":Url.IsLocalUrl(i.ReturnUrl)?i.ReturnUrl!:SucAccess.Home(account.VaiTro.ToString()));')
a('QuanLyPhien','DangNhap','DangXuat','','await C.DangXuatAsync();await HttpContext.SignOutAsync();return LocalRedirect("/thong-tin/mo-tim-kiem");')
a('QuanLyPhien','DangNhap','DoiMatKhau','PasswordChangeInput i','var account=await C.DoiMatKhauAsync(i);await SessionAuthentication.SignIn(HttpContext,account);return Done(SucAccess.Home(S.Role),"Đã đổi mật khẩu và kết thúc các phiên đăng nhập cũ.");')
a('CapNhatHoSo','HoSoNguoiDung','XemHoSo','','await C.XemHoSoAsync();ViewBag.Account=await S.Find<TaiKhoan>(S.UserId);if(S.Role==TravelService.ProviderRole)ViewBag.Provider=await S.MyProvider();return V("Account/Profile",await C.MoBieuMauAsync());')
a('CapNhatHoSo','HoSoNguoiDung','LuuThayDoi','ProfileInput i','await C.LuuThayDoiAsync(i);return Done("/ho-so/xem-ho-so");')
a('TraCuuNhaCungCap','QuyenHoatDong','MoDanhSach','string loc="TatCa",int trang=1','return V("Admin/Providers",await C.MoDanhSachAsync(null,loc=="TatCa"?"":loc,trang));')
a('TraCuuNhaCungCap','QuyenHoatDong','XemChiTiet','long maNCC','ViewBag.Assessment=await C.LayDanhGiaAsync(maNCC);return V("Admin/Provider",await C.XemChiTietAsync(maNCC));')
a('QuyetDinhQuyenHoatDong','QuyenHoatDong','XemHeQua','long maNCC,string thaoTac','var decision=thaoTac=="KhoiPhuc"?"DangHoatDong":thaoTac=="ThuHoi"?"DaThuHoi":thaoTac;return V("Admin/Consequences",await C.XemHeQuaAsync(maNCC,decision));')
for action,decision in [('TamDinhChi','TamDinhChi'),('KhoiPhuc','DangHoatDong'),('ThuHoi','DaThuHoi')]:
 a('QuyetDinhQuyenHoatDong','QuyenHoatDong',action,'CommandInput i',f'i.Decision="{decision}";await C.{action}Async(i);return Done("/quan-tri/quyen-hoat-dong/xem-chi-tiet/"+i.Id);')
a('QuyetDinhQuyenHoatDong','QuyenHoatDong','XemXetGiaiTrinh','CommandInput i','var g=await S.Find<GiaiTrinhNhaCungCap>(i.Id);var d=await S.Find<QuyetDinhQuyenHoatDong>(g.MaQuyetDinh);await C.XemXetGiaiTrinhAsync(i);return Done("/quan-tri/quyen-hoat-dong/xem-chi-tiet/"+d.MaNhaCungCap);')
a('GiaiTrinh','QuyenHoatDong','QuyenCuaToi','','return V("Provider/Rights",await C.QuyenCuaToiAsync());')
a('GiaiTrinh','QuyenHoatDong','NhanGiaiTrinh','CommandInput i','if(i.Id==0)i.Id=(await C.LayQuyetDinhCuaToiAsync()).FirstOrDefault()?.MaQuyetDinh??0;await C.NhanGiaiTrinhAsync(i);return Done("/nha-cung-cap/quyen-hoat-dong","Đã gửi giải trình, chờ xem xét.");')
a('KiemTraDieuKienNgung','NgungTaiKhoan','MoNgung','','return V("Account/Deactivate",await C.MoNgungAsync());')
a('KiemTraDieuKienNgung','NgungTaiKhoan','XacThucLai','DeactivationInput i','return V("Account/DeactivateReview",await C.XacThucLaiAsync(i.Password));')
a('ThucHienNgung','NgungTaiKhoan','NgungTaiKhoan','string confirmationToken','await C.NgungTaiKhoanAsync(confirmationToken);await HttpContext.SignOutAsync();return Done("/tai-khoan/dang-nhap/mo-dang-nhap","Tài khoản đã ngừng sử dụng.");')
a('ThongBao','ThongBao','DanhSachThongBao','int trang=1','return V("Account/Notifications",Page(await C.DanhSachThongBaoAsync(),trang));')
a('ThongBao','ThongBao','MoThongBao','long maThongBao','var target=await C.MoThongBaoAsync(maThongBao);return LocalRedirect(Url.IsLocalUrl(target)?target!:"/thong-bao");')
a('Tep','LuuTep','TaiTep','string loai,long maTaiLieu','var kind=loai=="XacMinh"?"verification":loai=="GiaiTrinh"?"explanation":loai;var file=await C.TaiTepAsync(kind,maTaiLieu);if(!System.IO.File.Exists(file.Path))return NotFound();return PhysicalFile(file.Path,file.ContentType,file.Name);')

header='using HoTroDuLich.Web.Domain;\nusing HoTroDuLich.Web.Models;\nusing HoTroDuLich.Web.Services;\nusing Microsoft.AspNetCore.Mvc;\nusing Microsoft.AspNetCore.Mvc.ModelBinding.Validation;\nusing Microsoft.AspNetCore.Authorization;\nusing Microsoft.AspNetCore.Authentication;\nusing Microsoft.AspNetCore.RateLimiting;\nnamespace HoTroDuLich.Web.Controllers;\n'
output=root/'src/HoTroDuLich.Web/Controllers/FullText';output.mkdir(exist_ok=True)
done=0
viewmodels=['using System.ComponentModel.DataAnnotations;','using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;','namespace HoTroDuLich.Web.Models;']
for controller,group in groups.items():
 rows=[x for x in actions if x['controller']==controller]
 assert len(rows)==len(group['actions']),(controller,len(rows),len(group['actions']))
 inputtypes=[m for row in rows if row['method']=='POST' for m in re.findall(r'\b(\w+Input)\s+i\b',group['actions'][row['action']][0])]
 primary=next((x for x in inputtypes if x!='CommandInput'),inputtypes[0] if inputtypes else None)
 vm=controller.removesuffix('Controller')+'ViewModel'
 properties={}
 for row in rows:
  if row['method']!='GET':continue
  signature=group['actions'][row['action']][0]
  for part in signature.split(','):
   match=re.fullmatch(r'\s*(string\??|long\??|int|decimal\?|DateOnly\?)\s+(\w+)(?:=(.*))?\s*',part)
   if match:
    datatype,name,default=match.groups();properties[name]=(datatype,default)
 fields=[]
 for name,(datatype,default) in properties.items():
  if primary=='LoginInput' and name=='returnUrl':continue
  prop=name[0].upper()+name[1:]
  initializer='='+default if default is not None else '=""' if datatype=='string' else ''
  validation='[Range(1,int.MaxValue)] ' if name=='trang' else '[MaxLength(100)] ' if name=='tuKhoa' else ''
  fields.append(f' {validation}public {datatype} {prop} {{get;set;}}{initializer};' if initializer else f' {validation}public {datatype} {prop} {{get;set;}}')
 viewmodels.append(f'/// <summary>Request model for the {controller.removesuffix("Controller")} package (FullText 4.87). POST validation runs in AppController; GET may use an empty form.</summary>\n[ValidateNever]\npublic class {vm}'+(':'+primary if primary else '')+' {\n'+'\n'.join(fields)+'\n}')
 text=header+f'public class {controller}(I{group["component"]}Service component,TravelService read):FullTextController<I{group["component"]}Service>(component,read) {{\n'
 for row in rows:
  signature,body=group['actions'][row['action']]
  if row['method']=='GET':
   locals=[]
   for part in signature.split(','):
    match=re.fullmatch(r'\s*(string\??|long\??|int|decimal\?|DateOnly\?)\s+(\w+)(?:=(.*))?\s*',part)
    if match:
     name=match[2];locals.append(f'var {name}=query.{name[0].upper()+name[1:]};')
   signature=f'[ValidateNever] {vm} query';body=''.join(locals)+body
  elif primary:signature=re.sub(r'\b'+primary+r'\s+i\b',vm+' i',signature)
  auth='AllowAnonymous' if row['policy'] in ['Công khai','Ẩn danh'] else f'Authorize(Policy="{row["policy"]}")'
  rate=',EnableRateLimiting("auth")' if row['method']=='POST' and row['policy']=='Ẩn danh' else ''
  text+=f' [{auth},Http{row["method"].title()}("{row["route"]}"){rate}]\n public async Task<IActionResult> {row["action"]}({signature}) {{ {body} }}\n'
  done+=1
 text+='}\n'
 (output/(controller+'.cs')).write_text(text,encoding='utf-8')
assert done==91 and len(groups)==43
(root/'src/HoTroDuLich.Web/Models/FullTextViewModels.cs').write_text('\n'.join(viewmodels)+'\n',encoding='utf-8')
print(f'Generated {done} actions across {len(groups)} canonical controllers.')
