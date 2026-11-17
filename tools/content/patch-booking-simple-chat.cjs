// The dat cho trong chat: them Huy (hai ben), Da phuc vu / Khach khong den (co so) dung quy tac dat gon.
const fs=require('fs'),path=require('path');const root=path.join(__dirname,'../../src/HoTroDuLich.Web');
const rd=f=>fs.readFileSync(path.join(root,f),'utf8').split('\r\n').join('\n'),wr=(f,s)=>fs.writeFileSync(path.join(root,f),s);
const swap=(s,a,b)=>{if(!s.includes(a)){if(s.includes(b))return s;throw Error('khong thay: '+a.slice(0,90));}return s.replace(a,()=>b);};
let c=rd('Services/ChatService.cs');
c=swap(c,`   if(traveler){if(y.TrangThai==TrangThaiYeuCau.ChoPhanHoi)actions.Add("Rut");else if(y.TrangThai==TrangThaiYeuCau.DeXuatKhac)actions.AddRange(["KhachDongY","KhachKhongDongY","Rut"]);}
   else if(y.TrangThai==TrangThaiYeuCau.ChoPhanHoi)actions.AddRange(["DaXacNhan","DeXuatKhac","TuChoi"]);`,
`   if(traveler){if(y.TrangThai==TrangThaiYeuCau.ChoPhanHoi)actions.Add("Rut");else if(y.TrangThai==TrangThaiYeuCau.DeXuatKhac)actions.AddRange(["KhachDongY","KhachKhongDongY","Rut"]);}
   else if(y.TrangThai==TrangThaiYeuCau.ChoPhanHoi)actions.AddRange(["DaXacNhan","DeXuatKhac","TuChoi"]);
   if(y.TrangThai==TrangThaiYeuCau.DaXacNhan&&y.NgayBatDau>today)actions.Add("Huy");
   if(!traveler&&y.TrangThai==TrangThaiYeuCau.DaXacNhan&&y.NgayBatDau<=today)actions.AddRange(["DaPhucVu","KhachKhongDen"]);`);
c=swap(c,`  var allowed=traveler?new[]{"Rut","KhachDongY","KhachKhongDongY"}:new[]{"DaXacNhan","TuChoi","DeXuatKhac"};`,`  var allowed=traveler?new[]{"Rut","KhachDongY","KhachKhongDongY","Huy"}:new[]{"DaXacNhan","TuChoi","DeXuatKhac","Huy","DaPhucVu","KhachKhongDen"};`);
c=swap(c,`   TrangThaiYeuCau.KhongTiepTuc=>a.Decision=="Rut"?`,`   TrangThaiYeuCau.DaHuy=>"Yêu cầu #"+after.MaYeuCau+" đã hủy. "+after.LyDo,
   TrangThaiYeuCau.HoanThanh=>"Cơ sở ghi nhận đã phục vụ yêu cầu #"+after.MaYeuCau+". Mời khách đánh giá.",
   TrangThaiYeuCau.KhongThucHien=>"Cơ sở ghi nhận khách không đến theo yêu cầu #"+after.MaYeuCau+".",
   TrangThaiYeuCau.KhongTiepTuc=>a.Decision=="Rut"?`);
wr('Services/ChatService.cs',c);
let j=rd('wwwroot/js/chat.js');
j=swap(j,`KhachDongY: 'Đồng ý đề xuất', KhachKhongDongY: 'Không đồng ý' };`,`KhachDongY: 'Đồng ý đề xuất', KhachKhongDongY: 'Không đồng ý', Huy: 'Hủy yêu cầu', DaPhucVu: 'Đã phục vụ', KhachKhongDen: 'Khách không đến' };`);
j=swap(j,`        if (a === 'TuChoi' || a === 'DeXuatKhac' || a === 'KhachKhongDongY' || a === 'YeuCauCoc' || a === 'ThemVaoChuyenDi') { openCardForm(form, c, a); return; }`,`        if (a === 'TuChoi' || a === 'Huy' || a === 'DeXuatKhac' || a === 'KhachKhongDongY' || a === 'YeuCauCoc' || a === 'ThemVaoChuyenDi') { openCardForm(form, c, a); return; }
        if (a === 'KhachKhongDen' && !confirm('Ghi nhận khách không đến?')) return;`);
j=swap(j,`    } else field(a === 'TuChoi' ? 'Lý do từ chối' : 'Lý do (tùy chọn)', 'textarea', 'Reason', '');`,`    } else field(a === 'TuChoi' ? 'Lý do từ chối' : a === 'Huy' ? 'Lý do hủy' : 'Lý do (tùy chọn)', 'textarea', 'Reason', '');`);
wr('wwwroot/js/chat.js',j);
console.log('ok');
