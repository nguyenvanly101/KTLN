// Chuyen di gon: danh sach (bo hero anh, o so lieu, nut tao trung; the nho, anh dung diem den) va trang chuyen di
// (nut chinh it lai, phan con lai vao menu "Them"; bo khung cuon long; hop "Khi dang tren duong" chi hien khi dang di).
const fs = require('fs'), path = require('path'); const root = path.join(__dirname, '../../src/HoTroDuLich.Web');
const rd = f => fs.readFileSync(path.join(root, f), 'utf8').split('\r\n').join('\n'), wr = (f, s) => fs.writeFileSync(path.join(root, f), s);
const swap = (s, a, b) => { if (!s.includes(a)) { if (s.includes(b)) return s; throw Error('khong thay: ' + a.slice(0, 90)); } return s.replace(a, () => b); };

// ---------- Danh sach chuyen di
let l = rd('Views/Plans/Index.cshtml');
const h0 = l.indexOf(' <section class="workspace-hero"'), h1 = l.indexOf(' <div class="section-head"><div><span class="eyebrow">TỪ Ý TƯỞNG ĐẾN LÊN ĐƯỜNG</span>');
if (h0 >= 0 && h1 > h0) {
  const h2 = l.indexOf('</div></div>', h1) + '</div></div>'.length;
  l = l.slice(0, h0) + ` <div class="page-head plans-head"><div><h1 id="plans-title">Chuyến đi của tôi</h1><p>@Model.Count chuyến đi@(next!=null?" · sắp tới: "+next.TenKeHoach+" ("+next.NgayBatDau.ToString("dd/MM")+")":"")</p></div><nav class="plans-links" aria-label="Liên quan"><a href="/Bookings">@Ui.Icon("ticket") Đặt dịch vụ</a><a href="/Support">@Ui.Icon("help") Hỗ trợ chuyến đi</a></nav></div>
 @if(next!=null){var left=next.NgayBatDau.DayNumber-TravelService.Today.DayNumber;<a class="plans-next" href="/Plans/Detail/@next.MaKeHoach"><span class="plans-next-date"><small>THÁNG @next.NgayBatDau.ToString("MM")</small><b>@next.NgayBatDau.ToString("dd")</b></span><span><strong>@next.TenKeHoach</strong><small>@next.NoiDen · @(left>0?"còn "+left+" ngày":"đang diễn ra")</small></span>@Ui.Icon("arrow")</a>}` + l.slice(h2);
}
l = swap(l, `<div class="plan-card-visual"><img src="@Ui.DestinationImage(p.NoiDen)" alt="Ảnh minh họa @p.NoiDen" loading="lazy" width="600" height="360"/>`,
  `<div class="plan-card-visual">@if(Ui.DestinationPhoto(p.NoiDen) is string photo){<img src="@photo" alt="" loading="lazy" width="600" height="360"/>}else{<span class="plan-card-blank" aria-hidden="true">@Ui.Icon("pin")</span>}`);
l = swap(l, ` <div class="notice">Lịch trình giúp bạn sắp xếp chuyến đi. Dịch vụ được xác nhận sau khi bạn gửi yêu cầu và nhận được phản hồi từ nhà cung cấp.</div>\n`, '');
wr('Views/Plans/Index.cshtml', l);

// ---------- Trang chuyen di
let d = rd('Views/Plans/Detail.cshtml');
const a0 = d.indexOf('   <div class="actions trip-actionbar">'), a1 = d.indexOf('</div>\n  </div>\n </header>', a0);
if (a0 >= 0 && a1 > a0) d = d.slice(0, a0) + `   @{var inWindow=TravelService.Today>=plan.NgayBatDau.AddDays(-1)&&TravelService.Today<=plan.NgayKetThuc;}
   <div class="actions trip-actionbar">
    @if(inWindow){<a class="btn btn-lime" href="/Journey/Today?planId=@plan.MaKeHoach#depart">@Ui.Icon("pin") Khởi hành · Hôm nay</a>}
    <a class="btn @(inWindow?"trip-hero-secondary":"btn-lime")" href="/lapkehoach/@plan.MaKeHoach">@Ui.Icon("plus") Sửa lịch trình</a>
    @if(isOwner){<a class="btn trip-hero-secondary" href="#trip-members">Mời bạn</a>}
    @if(groupId != null){<a class="btn trip-hero-secondary" href="/tin-nhan/@groupId">Nhóm chat</a>}
    <details class="trip-more"><summary class="btn trip-hero-secondary">Thêm ▾</summary><div class="trip-more-menu">
     @if(!inWindow){<a href="/Journey/Today?planId=@plan.MaKeHoach">Xem theo từng ngày</a>}
     @if(isOwner){<form action="/Plans/Decision" method="post">@Html.AntiForgeryToken()<input type="hidden" name="Id" value="@plan.MaKeHoach"/><input type="hidden" name="Version" value="@TravelService.Token(plan.RowVersion)"/><button name="Decision" value="@(plan.TrangThai == TrangThaiKeHoach.Nhap ? "DaChapNhan" : "Nhap")" type="submit">@(plan.TrangThai == TrangThaiKeHoach.Nhap ? "Xác nhận lịch trình" : "Chuyển về bản nháp")</button></form><a href="/Journey/Share?planId=@plan.MaKeHoach">Chia sẻ</a>}
     <a href="/Plans/Split/@plan.MaKeHoach">Chia tiền</a>
     <a href="/Support/Create?planId=@plan.MaKeHoach">Ghi nhận sự cố</a>
    </div></details>
   ` + d.slice(a1);
d = swap(d, `<button type="button" class="trip-issue-btn"`, `@if(inWindow){<button type="button" class="trip-issue-btn"`);
d = swap(d, `>Có vấn đề</button><p class="help">`, `>Có vấn đề</button>}<p class="help">`);
d = swap(d, `  <partial name="/Views/Plans/_TripHelp.cshtml"/>`, `  @if((bool)ViewData["OnTrip"]!){<partial name="/Views/Plans/_TripHelp.cshtml"/>}`);
wr('Views/Plans/Detail.cshtml', d);

let css = rd('wwwroot/css/social.css');
if (!css.includes('/* Chuyen di gon */')) css += `
/* Chuyen di gon */
.plans-head{display:flex;justify-content:space-between;align-items:flex-end;gap:16px;flex-wrap:wrap;margin-bottom:12px}
.plans-head h1{margin:0}.plans-head p{margin:4px 0 0}
.plans-links{display:flex;gap:16px;flex-wrap:wrap}.plans-links a{display:inline-flex;gap:6px;align-items:center;color:var(--teal);font-weight:600;font-size:14px;text-decoration:none}
.plans-next{display:flex;align-items:center;gap:14px;padding:12px 16px;border-radius:16px;background:var(--teal-light);color:var(--ink);text-decoration:none;margin:0 0 20px;max-width:560px}
.plans-next strong{display:block}.plans-next small{color:var(--muted)}
.plans-next>.icon{margin-left:auto;color:var(--teal)}
.plans-next-date{display:grid;place-items:center;width:52px;height:56px;border-radius:12px;background:#fff;line-height:1.1}
.plans-next-date small{font-size:9px;letter-spacing:.06em;color:var(--muted)}.plans-next-date b{font-size:22px}
.plan-grid{grid-template-columns:repeat(auto-fill,minmax(280px,1fr))!important;gap:16px!important}
.plan-card-visual{height:150px!important;background:linear-gradient(160deg,#2447e8,#142447)}
.plan-card-visual img{height:150px!important}
.plan-card-blank{position:absolute;inset:0;display:grid;place-items:end start;padding:14px;color:rgba(255,255,255,.3)}
.plan-card-blank .icon{width:44px;height:44px}
.plan-card .card-content{padding:14px 16px 16px}
.plan-card-actions .btn{min-height:38px;padding:7px 14px;font-size:13px}
/* Trang chuyen di: lich trinh khong nam trong khung cuon long */
.trip-scroll{max-height:none!important;overflow:visible!important;border:0;padding:0;background:transparent}
.trip-more{position:relative;margin:0!important;padding:0!important;border:0!important;display:inline-block}
.trip-more>summary{list-style:none;cursor:pointer}
.trip-more>summary::-webkit-details-marker{display:none}
.trip-more-menu{position:absolute;left:0;top:calc(100% + 6px);z-index:30;background:#fff;border:1px solid var(--line);border-radius:12px;box-shadow:0 12px 30px #15213e33;min-width:210px;padding:6px;display:grid}
.trip-more-menu form{margin:0}
.trip-more-menu a,.trip-more-menu button{display:block;width:100%;text-align:left;background:none;border:0;cursor:pointer;padding:9px 12px;border-radius:8px;font-size:14px;color:var(--ink);text-decoration:none;font-weight:500}
.trip-more-menu a:hover,.trip-more-menu button:hover{background:var(--teal-light)}
`;
wr('wwwroot/css/social.css', css);
console.log('ok');
