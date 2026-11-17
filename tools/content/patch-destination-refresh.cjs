// Trang diem den: hero anh, noi dung that len dau, hoi dap gap xuong cuoi, the dia diem nen sang theo loai.
const fs=require('fs'),path=require('path');const root=path.join(__dirname,'../../src/HoTroDuLich.Web');
const rd=f=>fs.readFileSync(path.join(root,f),'utf8').split(String.fromCharCode(13,10)).join(String.fromCharCode(10)),wr=(f,s)=>fs.writeFileSync(path.join(root,f),s);
const swap=(s,a,b)=>{if(s.includes(b))return s;if(!s.includes(a))throw Error('khong thay: '+a.slice(0,70));return s.replace(a,()=>b);};
let v=rd('Views/Discovery/Destination.cshtml');
v=swap(v,` <section class="dest-hero dest-hero-compact">
  <div>
   <span class="eyebrow">ĐIỂM ĐẾN</span>
   <h1>@u.FullName</h1>
   <p>@(Model.Total>0?$"{Model.Total} mục thông tin đã công bố, gồm cả các khu vực nhỏ hơn bên trong.":"Chưa có thông tin nào được công bố cho khu vực này.")</p>
  </div>
  @if(photo!=null){<img src="@photo" alt="" width="420" height="280"/>}
 </section>`,` <section class="dest-hero dest-hero-photo">
  @if(photo!=null){<img src="@photo" alt="" width="1200" height="480" fetchpriority="high"/>}
  <div class="dest-hero-in">
   <h1>@u.FullName</h1>
   <p>@(Model.Total>0?$"{Model.Total} mục thông tin đã công bố, gồm cả các khu vực nhỏ hơn bên trong.":"Chưa có thông tin nào được công bố cho khu vực này.")</p>
  </div>
 </section>`);
// hoi dap + ghi chu hanh chinh: chuyen xuong cuoi, hoi dap gap lai
v=swap(v,`  <partial name="/Views/Shared/_QaBox.cshtml" model="u.Id"/>
  @if(Model.NewProvinces.Count>0)
  {
   <aside class="dest-note" role="note"><strong>Thay đổi hành chính từ 01/07/2025.</strong> Các xã/phường thuộc khu vực này nay thuộc: @string.Join("; ",Model.NewProvinces.Select(m=>$"{m.Province} ({m.Units} đơn vị)")). Dữ liệu theo bảng chuyển đổi của Tổng cục Thống kê.</aside>
  }
`,'');
v=swap(v,`   <section class="dest-section" id="guides"><div class="ds-section-head"><div><h2>Cẩm nang & sự kiện</h2></div></div>
    <div class="ds-card-grid">@foreach(var g in Model.Guides){<partial name="_Card" model="new DiscoveryCard(g)"/>}</div>
   </section>
  }
`,`   <section class="dest-section" id="guides"><div class="ds-section-head"><div><h2>Cẩm nang & sự kiện</h2></div></div>
    <div class="ds-card-grid">@foreach(var g in Model.Guides){<partial name="_Card" model="new DiscoveryCard(g)"/>}</div>
   </section>
  }
  @if(Model.NewProvinces.Count>0)
  {
   <p class="dest-note" role="note"><strong>Thay đổi hành chính từ 01/07/2025.</strong> Các xã/phường thuộc khu vực này nay thuộc: @string.Join("; ",Model.NewProvinces.Select(m=>$"{m.Province} ({m.Units} đơn vị)")). Dữ liệu theo bảng chuyển đổi của Tổng cục Thống kê.</p>
  }
  <details class="dest-fold"><summary>Câu hỏi từ du khách <span>Hỏi hoặc xem tất cả</span></summary><partial name="/Views/Shared/_QaBox.cshtml" model="u.Id"/></details>
`);
v=swap(v,'<div><span class="eyebrow">THU HẸP THEO KHU VỰC</span><h2>','<div><h2>');
v=swap(v,'<div><span class="eyebrow">KHU DU LỊCH</span><h2>','<div><h2>');
wr('Views/Discovery/Destination.cshtml',v);
let css=rd('wwwroot/css/dia-gioi.css');
const lines=css.split('\n');
const first=lines.findIndex(l=>l.startsWith('.ds-card-photo .ds-card-blank{')),last=lines.findIndex(l=>l.startsWith('.ds-card-photo .ds-tone-info{'));
if(first<0||last<first)throw Error('khong thay khoi the');
lines.splice(first,last-first+1,
'.ds-card-photo .ds-card-blank{position:absolute;inset:0;width:auto;height:auto;margin:0;padding:0;border-radius:0;display:grid;place-items:center;color:#2447e8;background:#e6ebff}',
'.ds-card-photo .ds-card-blank svg{width:44px;height:44px;color:inherit}',
'.ds-card-photo .ds-tone-eat{background:#fdebd9;color:#9a4a12}',
'.ds-card-photo .ds-tone-stay{background:#e3f1ec;color:#1d6a4f}',
'.ds-card-photo .ds-tone-move{background:#efe7f7;color:#6a3f95}',
'.ds-card-photo .ds-tone-info{background:#eef0f4;color:#495057}');
css=lines.join('\n');
css=swap(css,'.dest-hero-compact{padding:0;gap:18px}',`.dest-hero-compact{padding:0;gap:18px}
.dest-hero-photo{position:relative;display:flex;align-items:flex-end;min-height:clamp(200px,28vw,300px);border-radius:20px;overflow:hidden;background:linear-gradient(160deg,#2447e8,#142447);color:#fff}
.dest-hero-photo>img{position:absolute;inset:0;width:100%;height:100%;max-width:none;object-fit:cover;border-radius:0;aspect-ratio:auto}
.dest-hero-photo::after{content:"";position:absolute;inset:0;background:linear-gradient(180deg,rgba(20,36,71,.1) 25%,rgba(20,36,71,.82))}
.dest-hero-in{position:relative;z-index:1;padding:clamp(18px,3vw,30px);display:grid;gap:6px}
body .dest-hero-photo h1{margin:0;color:#fff;font-size:clamp(1.8rem,4.4vw,2.8rem);letter-spacing:-.03em}
body .dest-hero-photo p{margin:0;color:#e8ecf7;max-width:58ch}
.dest-fold{border:1px solid var(--ds-line,#dbe1ee);border-radius:14px;padding:4px 18px;background:#fff}
.dest-fold>summary{display:flex;justify-content:space-between;gap:12px;align-items:center;cursor:pointer;min-height:52px;font-weight:650;list-style:none}
.dest-fold>summary span{color:#2447e8;font-weight:500;font-size:.9rem}
.dest-fold>summary::-webkit-details-marker{display:none}
.dest-fold .qa-box{border:0;box-shadow:none;padding:0 0 14px}`);
css=swap(css,'.dest-tabs{display:flex;gap:6px;overflow-x:auto;border-bottom:1px solid var(--ds-line,#dbe1ee);position:sticky;top:0;','.dest-tabs{display:flex;gap:6px;overflow-x:auto;border-bottom:1px solid var(--ds-line,#dbe1ee);position:sticky;top:80px;');
wr('wwwroot/css/dia-gioi.css',css);
console.log('ok');
