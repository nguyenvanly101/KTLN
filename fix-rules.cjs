const fs=require('fs'),root='D:/KLTN/src/HoTroDuLich.Web/';
function edit(file,from,to){let s=fs.readFileSync(root+file,'utf8');if(!s.includes(from))throw Error('Missing '+from.slice(0,50));fs.writeFileSync(root+file,s.replace(from,to));}
edit('Services/PageData.cs','||db.DeNghiKetNoiDongHanh.Any(c=>','||(x.ThongTinChiaSe=="KhiKetNoi"&&db.DeNghiKetNoiDongHanh.Any(c=>');
edit('Services/PageData.cs','n.MaTaiKhoan==UserId))))).ToDictionaryAsync','n.MaTaiKhoan==UserId)))))).ToDictionaryAsync');
edit('Services/BookingService.cs','if(i.Decision!="TuChoi")Require','if(i.Decision=="DaXacNhan")Require(y.TrangThai=="ChoPhanHoi","Điều kiện đã đề xuất cần khách xác nhận.");if(i.Decision!="TuChoi")Require');
edit('Services/BookingService.cs','if(i.Decision=="TuChoi")Require(!string.IsNullOrWhiteSpace(i.Reason),"Vui lòng nhập lý do từ chối.",400);if(i.Decision=="DeXuatKhac"){Require(!string.IsNullOrWhiteSpace(i.Text),','if(i.Decision=="ChapNhan")Require(d.TrangThai=="ChoPhanHoi","Điều kiện đề xuất cần khách xác nhận.");if(i.Decision=="TuChoi")Require(!string.IsNullOrWhiteSpace(i.Reason),"Vui lòng nhập lý do từ chối.",400);if(i.Decision=="DeXuatKhac"){Require(!string.IsNullOrWhiteSpace(i.Text),');
edit('Services/AccountService.cs','p.GioiThieu=i.Bio;','Require(i.Bio==null||i.Bio.Length<=1000,"Giới thiệu tối đa 1000 ký tự.",400);Require(i.Interests==null||i.Interests.Length<=500,"Sở thích tối đa 500 ký tự.",400);p.GioiThieu=i.Bio;');
edit('Views/Explore/Index.cshtml','0@s.MaDichVu','@s.MaDichVu.ToString("00")');
edit('Services/CompanionService.cs','CheckVersion(c,i.Version);Require(c.TrangThai=="DaChapNhan"','CheckVersion(c,i.Version);await EnsureConnectionActive(c);Require(c.TrangThai=="DaChapNhan"');
edit('Services/CompanionService.cs','else{Require(t.TrangThai=="ChoXacNhan"','else{await EnsureConnectionActive(c);Require(t.TrangThai=="ChoXacNhan"');
