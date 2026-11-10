const fs=require('fs'),path=require('path');
const root='D:/KLTN/src/HoTroDuLich.Web';
function write(p,s){fs.mkdirSync(path.dirname(root+'/'+p),{recursive:true});fs.writeFileSync(root+'/'+p,s);}
write('Views/Shared/_Layout.cshtml',`@inject TravelService Travel
@{
 var role=Travel.Role;
 var route=ViewContext.RouteData.Values["controller"]?.ToString()??"Explore";
 var action=ViewContext.RouteData.Values["action"]?.ToString()??"Index";
 var signed=User.Identity?.IsAuthenticated==true;
 var profile=signed?await Travel.Profile():null;
 var name=profile?.TenHienThi??"Tài khoản";
 var workspace=signed && (new[]{"Plans","Bookings","Support","Provider","Admin"}.Contains(route) || (route=="Account" && !new[]{"Login","Register"}.Contains(action)) || (route=="Companions" && !new[]{"Index","Detail"}.Contains(action)));
 var roleName=role==TravelService.AdminRole?"Quản trị hệ thống":role==TravelService.ProviderRole?"Dành cho đối tác":"Không gian cá nhân";
}
<!DOCTYPE html>
<html lang="vi">
<head><meta charset="utf-8"/><meta name="viewport" content="width=device-width,initial-scale=1"/><meta name="theme-color" content="#0b6b69"/><title>@ViewData["Title"] · Hành Trình</title><link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css"/><link rel="stylesheet" href="~/css/site.css" asp-append-version="true"/></head>
<body class="@(workspace?"is-workspace":"is-public")">
<a class="skip-link" href="#main-content">Đến nội dung chính</a>
<header class="topbar">
 <a class="brand" href="/" aria-label="Hành Trình — Trang chủ"><span class="brandmark">@Ui.Icon("compass")</span><span>Hành Trình<span class="brand-sub">Khám phá theo cách của bạn</span></span></a>
 <nav class="mainnav" id="main-navigation" aria-label="Điều hướng chính">
  <a class="@(route=="Explore"?"active":"")" href="/">Khám phá</a>
  <a class="@(route=="Companions"?"active":"")" href="/Companions">Tìm bạn đồng hành</a>
  @if(role!=TravelService.AdminRole && role!=TravelService.ProviderRole){<a class="@(route=="Plans"?"active":"")" href="/Plans">Chuyến đi của tôi</a>}
  @if(role==TravelService.AdminRole){<a class="@(route=="Admin"?"active":"")" href="/Admin">Trang quản trị</a>}
  @if(role==TravelService.ProviderRole){<a class="@(route=="Provider"?"active":"")" href="/Provider">Quản lý dịch vụ</a>}
 </nav>
 <div class="account-nav">
 @if(signed){
  <a class="icon-button" href="/Account/Notifications" aria-label="Thông báo">@Ui.Icon("bell")</a>
  <details class="account-menu"><summary><span class="avatar">@name[..1]</span><span class="account-name">@name</span><span class="caret">⌄</span></summary><div class="account-dropdown"><span class="dropdown-label">@Labels.Status(role)</span><a href="/Account/Profile">@Ui.Icon("user") Thông tin cá nhân</a><a href="/Account/Notifications">@Ui.Icon("bell") Thông báo</a><form action="/Account/Logout" method="post">@Html.AntiForgeryToken()<button type="submit">@Ui.Icon("logout") Đăng xuất</button></form></div></details>
 }else{<a class="login-link" href="/Account/Login">Đăng nhập</a><a class="btn btn-dark signup-link" href="/Account/Register">Đăng ký</a>}
 <button class="menu-toggle icon-button" aria-label="Mở menu" aria-controls="@(workspace?"workspace-navigation":"main-navigation")" aria-expanded="false">@Ui.Icon("menu")</button>
 </div>
</header>
<div class="app-frame">
@if(workspace){
 <aside class="workspace-sidebar" id="workspace-navigation" aria-label="Menu @roleName">
  <div class="sidebar-intro"><span class="sidebar-eyebrow">@roleName</span><strong>@name</strong></div>
  <nav class="sidebar-nav">
  @if(role==TravelService.Traveler){
   <span class="nav-group-label">CHUYẾN ĐI</span>
   <a class="@(route=="Plans"?"active":"")" href="/Plans">@Ui.Icon("calendar") Chuyến đi của tôi</a>
   <a class="@(route=="Bookings"?"active":"")" href="/Bookings">@Ui.Icon("ticket") Đặt dịch vụ</a>
   <a class="@(route=="Companions"?"active":"")" href="/Companions/Connections">@Ui.Icon("users") Kết nối đồng hành</a>
   <a class="@(route=="Support"?"active":"")" href="/Support">@Ui.Icon("help") Hỗ trợ chuyến đi</a>
   <span class="nav-group-label">KHÁM PHÁ</span>
   <a href="/">@Ui.Icon("compass") Điểm đến & dịch vụ</a><a href="/Companions">@Ui.Icon("search") Tìm bạn đồng hành</a>
  }else if(role==TravelService.ProviderRole){
   <span class="nav-group-label">QUẢN LÝ DỊCH VỤ</span>
   <a class="@(route=="Provider"&&action!="Verification"?"active":"")" href="/Provider">@Ui.Icon("grid") Dịch vụ của tôi</a>
   <a class="@(route=="Bookings"?"active":"")" href="/Bookings">@Ui.Icon("ticket") Yêu cầu đặt dịch vụ</a>
   <a class="@(route=="Provider"&&action=="Verification"?"active":"")" href="/Provider/Verification">@Ui.Icon("shield") Hồ sơ nhà cung cấp</a>
  }else if(role==TravelService.AdminRole){
   <span class="nav-group-label">QUẢN TRỊ</span>
   <a class="@(route=="Admin"&&action=="Index"?"active":"")" href="/Admin">@Ui.Icon("grid") Tổng quan</a>
   <a href="/Admin#service-reviews">@Ui.Icon("check") Xét duyệt dịch vụ</a><a href="/Admin#verification">@Ui.Icon("shield") Xác minh nhà cung cấp</a><a href="/Admin#provider-rights">@Ui.Icon("users") Quyền hoạt động</a>
   <a class="@(action=="Audit"?"active":"")" href="/Admin/Audit">@Ui.Icon("clock") Nhật ký hoạt động</a>
  }
  <span class="nav-group-label">TÀI KHOẢN</span>
  <a class="@(route=="Account"&&action=="Notifications"?"active":"")" href="/Account/Notifications">@Ui.Icon("bell") Thông báo</a>
  <a class="@(route=="Account"&&action=="Profile"?"active":"")" href="/Account/Profile">@Ui.Icon("user") Thông tin cá nhân</a>
  </nav>
  @if(role==TravelService.Traveler){<div class="sidebar-promo">@Ui.Icon("compass")<h3>Sẵn sàng cho chuyến đi mới?</h3><p>Tạo lịch trình và sắp xếp những trải nghiệm bạn muốn thử.</p><a href="/Plans/Edit">Tạo chuyến đi @Ui.Icon("arrow")</a></div>}
  <a class="sidebar-home" href="/">← Về trang khám phá</a>
 </aside>
}
<div class="app-content">
 @if(workspace){<div class="workspace-topline"><span>@roleName <span class="crumb-separator">/</span> <b>@ViewData["Title"]</b></span><span class="workspace-date">@TravelService.Today.ToString("dd/MM/yyyy")</span></div>}
 <main id="main-content">@if(TempData["Success"] is string success){<div class="toast-message" role="status">@Ui.Icon("check")<span>@success</span><button class="dismiss" aria-label="Đóng thông báo">×</button></div>}@RenderBody()</main>
 @if(!workspace){<footer class="site-footer"><div class="footer-main"><div><a class="brand" href="/">@Ui.Icon("compass") Hành Trình</a><p>Tìm điểm đến, lên lịch trình<br/>và kết nối bạn đồng hành.</p></div><div><h3>Khám phá</h3><a href="/">Điểm đến & dịch vụ</a><a href="/Companions">Tìm bạn đồng hành</a><a href="/Plans">Chuyến đi của tôi</a></div><div><h3>Dành cho bạn</h3><a href="/Bookings">Quản lý đặt dịch vụ</a><a href="/Support">Hỗ trợ chuyến đi</a><a href="/Account/Register">Đăng ký nhà cung cấp</a></div></div><div class="footer-bottom"><span>© @DateTime.Now.Year Hành Trình</span><span>Bản trải nghiệm · Dữ liệu và ảnh dịch vụ mang tính minh họa</span></div></footer>}
</div></div><div class="nav-scrim" hidden></div>
<script src="~/js/site.js" asp-append-version="true"></script>@await RenderSectionAsync("Scripts",required:false)
</body></html>`);
write('Views/Shared/_ServiceCard.cshtml',`@model DichVuDuLich
<article class="service-card">
 <a class="service-image" href="/Explore/Detail/@Model.MaDichVu" aria-label="Xem @Model.TenDichVu"><img src="@Ui.ServiceImage(Model)" alt="Ảnh minh họa cho @Model.LoaiDichVu" loading="lazy" width="600" height="440"/><span class="image-label">@Ui.Icon(Ui.CategoryIcon(Model.LoaiDichVu)) @Model.LoaiDichVu</span></a>
 <div class="service-body"><div class="location">@Ui.Icon("pin") @Model.KhuVucPhucVu</div><h3><a href="/Explore/Detail/@Model.MaDichVu">@Model.TenDichVu</a></h3><p>@Model.MoTa</p>
 @if(Model.TrangThaiTiepNhan!="TiepNhan"){<span class="badge @Labels.Badge(Model.TrangThaiTiepNhan)">@Labels.Status(Model.TrangThaiTiepNhan)</span>}
 <div class="service-bottom"><div><span class="price-caption">Giá tham khảo</span><strong>@Labels.Money(Model.GiaThamKhao)</strong></div><a class="card-link" href="/Explore/Detail/@Model.MaDichVu" aria-label="Chi tiết @Model.TenDichVu">@Ui.Icon("arrow")</a></div></div>
</article>`);
write('Views/Explore/Index.cshtml',`@model List<DichVuDuLich>
@{
 ViewData["Title"]="Khám phá Việt Nam";
 var places=(List<DiaDiemDuLich>)ViewBag.Places;
 var filtered=Context.Request.Query.Any(x=>!string.IsNullOrWhiteSpace(x.Value));
 var currentType=(string?)ViewBag.Type;
}
<div class="discovery-page">
@if(!filtered){
 <section class="discovery-hero" aria-label="Khám phá Việt Nam"><img class="hero-photo" src="~/images/vietnam-bay.jpg" alt="Toàn cảnh những đảo đá vôi giữa làn nước xanh ở vịnh Hạ Long" fetchpriority="high" width="1920" height="1440"/>
 <div class="hero-shade"></div><div class="discovery-hero-content"><span class="hero-kicker"><span></span> LÊN KẾ HOẠCH CHO CHUYẾN ĐI TIẾP THEO</span><h1>Việt Nam còn nhiều<br/>điều để khám phá.</h1><p>Tìm điểm đến yêu thích, tự lên lịch trình<br class="desktop-break"/> và gặp những người bạn cùng chung sở thích.</p><a href="/Plans/Edit" class="btn btn-white">Tạo chuyến đi @Ui.Icon("arrow")</a></div>
 <div class="hero-location">@Ui.Icon("pin")<span>Vịnh Hạ Long<small>Quảng Ninh, Việt Nam</small></span></div><span class="hero-photo-credit">Ảnh: Ryan Le / Unsplash</span></section>
}else{
 <div class="search-page-heading"><span class="eyebrow">KHÁM PHÁ VIỆT NAM</span><h1>@(string.IsNullOrWhiteSpace((string?)ViewBag.Query)?"Tìm điểm đến và dịch vụ":"Kết quả cho “"+ViewBag.Query+"”")</h1><p>Có @(Model.Count+places.Count) kết quả phù hợp với bộ lọc của bạn.</p></div>
}
<div class="discovery-inner">
 <div class="search-box @(filtered?"search-box-results":"")"><div class="search-box-tabs"><span class="active">@Ui.Icon("compass") Điểm đến & dịch vụ</span><a href="/Companions">@Ui.Icon("users") Tìm bạn đồng hành</a></div>
 <form action="/" method="get" class="discovery-search">
  <div class="search-input search-keyword">@Ui.Icon("search")<div><label for="q">Bạn muốn đi đâu?</label><input id="q" name="q" value="@ViewBag.Query" placeholder="Điểm đến hoặc tên dịch vụ"/></div></div>
  <div class="search-input">@Ui.Icon("pin")<div><label for="region">Điểm đến</label><select id="region" name="region"><option value="">Tất cả khu vực</option>@foreach(var region in new[]{"Đà Lạt","Hội An","Hải Phòng"}){<option selected="@(ViewBag.Region==region)" value="@region">@region</option>}</select></div></div>
  <div class="search-input">@Ui.Icon("grid")<div><label for="type">Loại hình</label><select id="type" name="type"><option value="">Tất cả dịch vụ</option>@foreach(var type in new[]{"Địa điểm","Lưu trú","Ăn uống","Phương tiện","Hoạt động"}){<option selected="@(currentType==type)" value="@type">@type</option>}</select></div></div>
  @if(ViewBag.Min!=null){<input type="hidden" name="min" value="@ViewBag.Min"/>}@if(ViewBag.Max!=null){<input type="hidden" name="max" value="@ViewBag.Max"/>}
  <button class="btn btn-dark" type="submit">@Ui.Icon("search") Tìm kiếm</button>
 </form></div>
 @if(!filtered){
 <section class="value-strip" aria-label="Lên kế hoạch cùng Hành Trình"><div>@Ui.Icon("pin")<span><b>Chọn điểm đến</b><small>Tra cứu địa điểm và dịch vụ</small></span></div><div>@Ui.Icon("calendar")<span><b>Chủ động lịch trình</b><small>Sắp xếp hoạt động, dự trù chi phí</small></span></div><div>@Ui.Icon("users")<span><b>Kết nối đồng hành</b><small>Tìm người phù hợp với chuyến đi</small></span></div></section>
 <section class="destination-section" id="destinations"><div class="section-head"><div><span class="eyebrow">BẠN MUỐN ĐI ĐÂU?</span><h2>Khám phá những điểm đến</h2><p>Chọn một nơi để bắt đầu lên kế hoạch.</p></div><a class="quiet-link" href="/?type=Địa điểm">Xem địa điểm @Ui.Icon("arrow")</a></div>
 <div class="destination-grid">
  <a class="destination-tile destination-dalat" href="/?region=Đà%20Lạt"><img src="~/images/dalat.jpg" alt="Hồ và rừng thông trong sương sớm ở Đà Lạt" loading="lazy"/><div><span>CAO NGUYÊN</span><h3>Đà Lạt</h3><p>Hồ, rừng thông và những buổi sớm yên bình</p></div><span class="destination-arrow">@Ui.Icon("arrow")</span></a>
  <a class="destination-tile" href="/?region=Hội%20An"><img src="~/images/hoian.jpg" alt="Con phố với giàn hoa và những ngôi nhà cổ ở Hội An" loading="lazy"/><div><span>VĂN HÓA & DI SẢN</span><h3>Hội An</h3><p>Khám phá phố cổ và ẩm thực địa phương</p></div><span class="destination-arrow">@Ui.Icon("arrow")</span></a>
  <a class="destination-tile" href="/?region=Hải%20Phòng"><img src="~/images/vietnam-bay.jpg" alt="Ảnh minh họa cảnh quan vùng vịnh miền Bắc" loading="lazy"/><div><span>BIỂN ĐẢO</span><h3>Hải Phòng</h3><p>Khám phá Cát Bà và vịnh Lan Hạ</p></div><span class="destination-arrow">@Ui.Icon("arrow")</span></a>
 </div></section>
 }
 <section class="experiences-section" id="experiences"><div class="section-head"><div><span class="eyebrow">@(filtered?"KẾT QUẢ TÌM KIẾM":"TRẢI NGHIỆM CHO CHUYẾN ĐI")</span><h2>@(filtered?"Điểm đến và dịch vụ phù hợp":"Tìm dịch vụ phù hợp với bạn")</h2><p>@(filtered?"Điều chỉnh bộ lọc để tìm thêm lựa chọn.":"Lưu trú, ăn uống, di chuyển và các hoạt động tại điểm đến.")</p></div>@if(filtered){<a class="quiet-link" href="/">Xóa bộ lọc @Ui.Icon("filter")</a>}</div>
 <nav class="category-tabs" aria-label="Loại hình dịch vụ"><a class="@(string.IsNullOrEmpty(currentType)?"active":"")" href="/?region=@ViewBag.Region&q=@ViewBag.Query">@Ui.Icon("grid") Tất cả</a>@foreach(var type in new[]{"Lưu trú","Hoạt động","Ăn uống","Phương tiện","Địa điểm"}){<a class="@(currentType==type?"active":"")" href="/?type=@type&region=@ViewBag.Region&q=@ViewBag.Query">@Ui.Icon(Ui.CategoryIcon(type)) @type</a>}</nav>
 <div class="@(filtered?"search-results-layout":"")">
 @if(filtered){<aside class="filter-panel"><h3>@Ui.Icon("filter") Bộ lọc</h3><form action="/" method="get"><input type="hidden" name="q" value="@ViewBag.Query"/><input type="hidden" name="region" value="@ViewBag.Region"/><input type="hidden" name="type" value="@currentType"/><label for="min-price">Giá từ (₫)</label><input id="min-price" class="form-control" type="number" min="0" name="min" value="@ViewBag.Min" placeholder="Không giới hạn"/><label for="max-price">Giá đến (₫)</label><input id="max-price" class="form-control" type="number" min="0" name="max" value="@ViewBag.Max" placeholder="Không giới hạn"/><button class="btn btn-dark w-100" type="submit">Áp dụng</button></form><p>Giá hiển thị là giá tham khảo. Nhà cung cấp sẽ xác nhận chi phí khi phản hồi yêu cầu.</p></aside>}
 <div><div class="service-grid">@foreach(var service in Model){<partial name="_ServiceCard" model="service"/>}
 @if(filtered){@foreach(var place in places){<article class="service-card"><a class="service-image" href="/Explore/Place/@place.MaDiaDiem"><img src="@Ui.DestinationImage(place.KhuVuc)" alt="Ảnh minh họa @place.KhuVuc" loading="lazy"/><span class="image-label">@Ui.Icon("pin") @place.LoaiDiaDiem</span></a><div class="service-body"><div class="location">@Ui.Icon("pin") @place.KhuVuc</div><h3><a href="/Explore/Place/@place.MaDiaDiem">@place.TenDiaDiem</a></h3><p>@place.MoTa</p><a class="text-link" href="/Explore/Place/@place.MaDiaDiem">Xem địa điểm @Ui.Icon("arrow")</a></div></article>}}
 </div>@if(Model.Count==0&&(!filtered||places.Count==0)){<div class="empty">@Ui.Icon("search")<h3>Chưa tìm thấy kết quả phù hợp</h3><p>Thử từ khóa khác hoặc mở rộng bộ lọc để tìm thêm lựa chọn.</p><a class="btn btn-dark" href="/">Xem tất cả</a></div>}</div></div>
 </section>
 @if(!filtered){<section class="companion-feature"><div class="companion-feature-photo"><img src="~/images/dalat.jpg" alt="Bình minh trên hồ ở Đà Lạt" loading="lazy"/><span class="feature-note">Một chuyến đi, thêm những người bạn.</span></div><div class="companion-feature-copy"><span class="eyebrow">KẾT NỐI ĐỒNG HÀNH</span><h2>Tìm người cùng bạn<br/>khám phá.</h2><p>Xem những chuyến đi có cùng điểm đến và thời gian. Kết nối, trao đổi kế hoạch và thống nhất trước khi lên đường.</p><a href="/Companions" class="btn btn-dark">Tìm bạn đồng hành @Ui.Icon("arrow")</a><a href="/Companions/Edit" class="text-link">Đăng tin tìm bạn đồng hành</a></div></section>}
 </div></div>`);
