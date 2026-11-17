// Bon diem ho cua luong dat dich vu: bao yeu cau cho o co so khac, ten khach + nhan tin, hien thay doi trong de xuat.
const fs=require('fs'),path=require('path');const root=path.join(__dirname,'../../src/HoTroDuLich.Web');
const edit=(file,pairs)=>{const p=path.join(root,file);let s=fs.readFileSync(p,'utf8');for(const [a,b] of pairs){if(s.includes(b))continue;if(!s.includes(a))throw Error(file+': khong thay '+a.slice(0,60));s=s.replace(a,()=>b);}fs.writeFileSync(p,s);};
edit('Controllers/BookingsController.cs',[
 ['TripWorkspaceService trips,BranchService branches):','TripWorkspaceService trips,BranchService branches,ChatService chat):'],
 [' async Task<List<HoTroDuLich.Web.Domain.YeuCauDatDichVu>> ForBranch(List<HoTroDuLich.Web.Domain.YeuCauDatDichVu> all)\n {',' async Task<List<HoTroDuLich.Web.Domain.YeuCauDatDichVu>> ForBranch(List<HoTroDuLich.Web.Domain.YeuCauDatDichVu> all,bool everything=false)\n {'],
 ['  var linked = await branches.ServiceIdsOf(current.Id);\n  return all.Where(x => linked.Contains(x.MaDichVu)).ToList();','  var linked = await branches.ServiceIdsOf(current.Id);\n  // Yeu cau dang cho o co so khac khong duoc an lang le: bao so luong va cho xem tat ca.\n  ViewBag.BranchName=current.Name;ViewBag.AllBranches=everything;\n  ViewBag.OtherWaiting=all.Count(x=>!linked.Contains(x.MaDichVu)&&(x.TrangThai is HoTroDuLich.Web.Domain.TrangThaiYeuCau.ChoPhanHoi or HoTroDuLich.Web.Domain.TrangThaiYeuCau.DeXuatKhac));\n  if(everything)return all;\n  return all.Where(x => linked.Contains(x.MaDichVu)).ToList();'],
 ['public async Task<IActionResult> Index(){ViewBag.ServiceNames=await S.BookingServiceNames();return View(await ForBranch(await S.Bookings()));}','public async Task<IActionResult> Index(bool tatCa=false){ViewBag.ServiceNames=await S.BookingServiceNames();return View(await ForBranch(await S.Bookings(),tatCa));}'],
 ['public async Task<IActionResult> Submit(BookingInput i)=>Done("/Bookings/Detail/"+await S.Book(i),"Đã gửi yêu cầu, chờ nhà cung cấp phản hồi.");','public async Task<IActionResult> Submit(BookingInput i){var id=await S.Book(i);try{await chat.AttachBooking(id);}catch(BusinessException){/* yeu cau da gui; chi khong gan duoc vao tro chuyen */}return Done("/Bookings/Detail/"+id,"Đã gửi yêu cầu, chờ nhà cung cấp phản hồi.");}'],
]);
edit('Controllers/FullText/GuiYeuCauDatController.cs',[
 ['TravelService read,TripWorkspaceService trips):','TravelService read,TripWorkspaceService trips,ChatService chat):'],
 ['{ return Done("/yeu-cau/"+await C.GuiYeuCauAsync(i),"Đã gửi yêu cầu, chờ nhà cung cấp phản hồi."); }','{ var id=await C.GuiYeuCauAsync(i);try{await chat.AttachBooking(id);}catch(BusinessException){/* yeu cau da gui; chi khong gan duoc vao tro chuyen */}return Done("/yeu-cau/"+id,"Đã gửi yêu cầu, chờ nhà cung cấp phản hồi."); }'],
]);
edit('Controllers/ChatControllers.cs',[
 ['  var id=await chat.ConversationOfBooking(requestId);\n  return id==null?','  var id=await chat.ConversationOfBooking(requestId);\n  if(id==null){try{id=await chat.AttachBooking(requestId);}catch(BusinessException){id=null;}}\n  return id==null?'],
]);
edit('Views/Bookings/Index.cshtml',[
 ['<partial name="_BookingWorkspaceTabs"/>','<partial name="_BookingWorkspaceTabs"/>\n@if(provider&&ViewBag.AllBranches is true){<div class="notice">Đang xem yêu cầu của tất cả cơ sở. <a href="/Bookings">Chỉ xem cơ sở @ViewBag.BranchName</a></div>}\nelse if(provider&&ViewBag.OtherWaiting is int otherWaiting&&otherWaiting>0){<div class="notice warning" id="other-branch-waiting">Còn <b>@otherWaiting</b> yêu cầu đang chờ ở cơ sở khác của bạn. <a href="/Bookings?tatCa=true">Xem tất cả cơ sở</a></div>}'],
]);
edit('Views/Bookings/Detail.cshtml',[
 ['@inject ChatService Chat\n','@inject ChatService Chat\n@inject IDatDichVuService BookingDiff\n'],
 ['canReceive=true;}catch(BusinessException){}}}','canReceive=true;}catch(BusinessException){}}var chatId=await Chat.ConversationOfBooking(Model.MaYeuCau);var canChat=chatId!=null||await Chat.BookingHasBranch(Model.MaYeuCau);var proposed=await BookingDiff.ThayDoiDeXuatAsync(Model.MaYeuCau);}'],
 ['@if(await Chat.ConversationOfBooking(Model.MaYeuCau) is long chatId){<a class="btn btn-outline" href="/tin-nhan/@chatId">@Ui.Icon("chat") Mở trò chuyện</a>}','@if(canChat){<a class="btn btn-outline" id="booking-chat" href="/tin-nhan/dat-cho/@Model.MaYeuCau">@Ui.Icon("chat") @(customer?"Nhắn tin với cơ sở":"Nhắn tin với khách")</a>}'],
 ['<dl class="facts"><div><dt>Thời gian phục vụ</dt><dd>@Model.NgayBatDau.ToString("dd/MM/yyyy") — @Model.NgayKetThuc?.ToString("dd/MM/yyyy")</dd></div>','<dl class="facts">@if(!customer){<div><dt>Khách đặt</dt><dd id="booking-customer">@(await Chat.NameOf(Model.MaTaiKhoan))</dd></div>}<div><dt>Thời gian phục vụ</dt><dd>@Model.NgayBatDau.ToString("dd/MM/yyyy")@(Model.NgayKetThuc is DateOnly lastDay?" — "+lastDay.ToString("dd/MM/yyyy"):"")</dd></div>'],
 ['<p class="prose">@Model.GhiChu</p>','@if(!string.IsNullOrWhiteSpace(Model.GhiChu)){<p class="prose"><b>Ghi chú của khách:</b> @Model.GhiChu</p>}'],
 ['@if(Model.DieuKienDeXuat!=null){<div class="notice warning">Điều kiện nhà cung cấp đề xuất: @Model.DieuKienDeXuat</div>}','@if(Model.DieuKienDeXuat!=null){<div class="notice warning">Điều kiện nhà cung cấp đề xuất: @Model.DieuKienDeXuat</div>}@if(proposed.Count>0){<div class="panel" id="proposal-changes"><h2>Nhà cung cấp đã đổi so với yêu cầu ban đầu</h2><ul>@foreach(var change in proposed){<li><b>@change.Label:</b> <s>@change.Old</s> → <strong>@change.New</strong></li>}</ul></div>}'],
]);
console.log('ok');
