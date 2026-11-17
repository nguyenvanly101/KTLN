// Dat dich vu gon: huy kem ly do (ca hai ben, truoc ngay dung), sua sau khi xac nhan (quay lai cho phan hoi),
// co so bam "Da phuc vu" / "Khach khong den" sau ngay dung; danh gia khong con can ket qua tung hang muc.
const fs=require('fs'),path=require('path');const root=path.join(__dirname,'../../src/HoTroDuLich.Web');
const rd=f=>fs.readFileSync(path.join(root,f),'utf8').split('\r\n').join('\n'),wr=(f,s)=>fs.writeFileSync(path.join(root,f),s);
const swap=(s,a,b)=>{if(!s.includes(a)){if(s.includes(b))return s;throw Error('khong thay: '+a.slice(0,90));}return s.replace(a,()=>b);};
let d=rd('Services/Components/DatDichVuService.cs');
d=swap(d,`        RoleRequired(new[] { "Rut", "KhachDongY", "KhachKhongDongY" }.Contains(i.Decision) ? Traveler : ProviderRole);`,
`        if (i.Decision == "Huy") RoleRequired(Traveler, ProviderRole);
        else RoleRequired(new[] { "Rut", "KhachDongY", "KhachKhongDongY" }.Contains(i.Decision) ? Traveler : ProviderRole);`);
d=swap(d,`            string type;
            if (i.Decision == "Rut")`,`            string type;
            if (i.Decision == "Huy")
            {
                // Huy truc tiep kem ly do, khong can ben kia duyet; yeu cau da xac nhan chi huy duoc truoc ngay dung.
                var customer = y.MaTaiKhoan == UserId;
                Require(customer || p.MaTaiKhoan == UserId, "Không có quyền hủy yêu cầu này.", 404);
                Require(!string.IsNullOrWhiteSpace(i.Reason), "Vui lòng ghi lý do hủy.", 422);
                Require(y.TrangThai is HoTroDuLich.Web.Domain.TrangThaiYeuCau.ChoPhanHoi or HoTroDuLich.Web.Domain.TrangThaiYeuCau.DeXuatKhac or HoTroDuLich.Web.Domain.TrangThaiYeuCau.DaXacNhan, "Yêu cầu này không còn hủy được.", 409);
                Require(y.TrangThai != HoTroDuLich.Web.Domain.TrangThaiYeuCau.DaXacNhan || y.NgayBatDau > HomNay, "Đã tới ngày sử dụng, không hủy được nữa. Hãy nhắn tin với bên kia.", 409);
                y.TrangThai = HoTroDuLich.Web.Domain.TrangThaiYeuCau.DaHuy;
                var why = i.Reason.Trim();
                y.LyDo = ((customer ? "Khách hủy: " : "Cơ sở hủy: ") + why)[..Math.Min(500, (customer ? 11 : 11) + why.Length)];
                type = "YeuCauDaHuy";
            }
            else if (i.Decision is "DaPhucVu" or "KhachKhongDen")
            {
                Require(p.MaTaiKhoan == UserId, "Không có quyền.", 404);
                Require(y.TrangThai == HoTroDuLich.Web.Domain.TrangThaiYeuCau.DaXacNhan && y.NgayBatDau <= HomNay, "Chỉ ghi nhận sau khi tới ngày sử dụng của yêu cầu đã xác nhận.", 409);
                y.TrangThai = i.Decision == "DaPhucVu" ? HoTroDuLich.Web.Domain.TrangThaiYeuCau.HoanThanh : HoTroDuLich.Web.Domain.TrangThaiYeuCau.KhongThucHien;
                y.LyDo = i.Decision == "KhachKhongDen" ? "Khách không đến" : null;
                type = i.Decision == "DaPhucVu" ? "YeuCauDaPhucVu" : "YeuCauKhachKhongDen";
            }
            else if (i.Decision == "Rut")`);
d=swap(d,`            serviceThongBao.Tao(UserId == y.MaTaiKhoan ? p.MaTaiKhoan : y.MaTaiKhoan,type,"Yêu cầu #" + y.MaYeuCau + ": " + Labels.Status((y.TrangThai).ToString()),`,
`            serviceThongBao.Tao(UserId == y.MaTaiKhoan ? p.MaTaiKhoan : y.MaTaiKhoan,type,"Yêu cầu #" + y.MaYeuCau + " (" + s.TenDichVu + "): " + BookingUi.Label(y.TrangThai) + (y.TrangThai == HoTroDuLich.Web.Domain.TrangThaiYeuCau.DaHuy && y.LyDo != null ? " · " + y.LyDo : ""),`);
wr('Services/Components/DatDichVuService.cs',d);
let e=rd('Services/Components/DieuChinhDichVuService.cs');
e=swap(e,`            Ensure(y.MaTaiKhoan == UserId && y.TrangThai == HoTroDuLich.Web.Domain.TrangThaiYeuCau.ChoPhanHoi, "0801-02");
            CheckVersion(y, i.Version, "0801-03");`,`            // Sua khi dang cho phan hoi, hoac sau khi da xac nhan (truoc ngay dung): yeu cau quay lai cho co so xac nhan lai.
            var reconfirm = y.TrangThai == HoTroDuLich.Web.Domain.TrangThaiYeuCau.DaXacNhan && y.NgayBatDau > HomNay;
            Ensure(y.MaTaiKhoan == UserId && (y.TrangThai == HoTroDuLich.Web.Domain.TrangThaiYeuCau.ChoPhanHoi || reconfirm), "0801-02");
            CheckVersion(y, i.Version, "0801-03");
            if (reconfirm) { y.TrangThai = HoTroDuLich.Web.Domain.TrangThaiYeuCau.ChoPhanHoi; y.DieuKienDeXuat = null; }`);
e=swap(e,`            serviceThongBao.Tao(p.MaTaiKhoan,"YeuCauDaSua","Khách vừa sửa yêu cầu #" + y.MaYeuCau,`,`            serviceThongBao.Tao(p.MaTaiKhoan,"YeuCauDaSua",(reconfirm ? "Khách đổi yêu cầu đã xác nhận #" : "Khách vừa sửa yêu cầu #") + y.MaYeuCau + " (" + s.TenDichVu + "), cần xác nhận lại",`);
wr('Services/Components/DieuChinhDichVuService.cs',e);
let r=rd('Services/ReviewService.cs');
r=swap(r,`(x.TrangThai==TrangThaiYeuCau.HoanThanh||x.TrangThai==TrangThaiYeuCau.ThucHienMotPhan||x.TrangThai==TrangThaiYeuCau.KhongThucHien)&&(x.NgayKetThuc??x.NgayBatDau)<=clock.HomNay&&db.KetQuaCungCapDichVu.Any(r=>r.MaYeuCau==x.MaYeuCau)&&`,
`(x.TrangThai==TrangThaiYeuCau.HoanThanh||x.TrangThai==TrangThaiYeuCau.ThucHienMotPhan)&&x.NgayBatDau<=clock.HomNay&&`);
wr('Services/ReviewService.cs',r);
console.log('ok');
