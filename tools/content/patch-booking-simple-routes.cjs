// Dat dich vu gon: danh sach chung cho moi co so, trang cu chuyen ve trang yeu cau, tab cu bo, CSS trang yeu cau.
const fs=require('fs'),path=require('path');const root=path.join(__dirname,'../../src/HoTroDuLich.Web');
const rd=f=>fs.readFileSync(path.join(root,f),'utf8').split('\r\n').join('\n'),wr=(f,s)=>fs.writeFileSync(path.join(root,f),s);
const swap=(s,a,b)=>{if(!s.includes(a)){if(s.includes(b))return s;throw Error('khong thay: '+a.slice(0,90));}return s.replace(a,()=>b);};
let c=rd('Controllers/BookingsController.cs');
c=swap(c,'public async Task<IActionResult> Index(bool tatCa=false){ViewBag.ServiceNames=await S.BookingServiceNames();return View(await ForBranch(await S.Bookings(),tatCa));}',
 'public async Task<IActionResult> Index(){ViewBag.ServiceNames=await S.BookingServiceNames();return View(await S.Bookings());}');
wr('Controllers/BookingsController.cs',c);
let x=rd('Controllers/FullText/XuLyYeuCauDatController.cs');
x=swap(x,'{ var trangThai=query.TrangThai;var trang=query.Trang;ViewBag.ServiceNames=await C.LayTenDichVuAsync();return V("Bookings/Index",Page(FilterBookings(await C.DanhSachNCCAsync(),trangThai),trang)); }',
 '{ await Task.CompletedTask;return Redirect("/Bookings"+Request.QueryString); }');
wr('Controllers/FullText/XuLyYeuCauDatController.cs',x);
// tab cu: khong con
wr('Views/Shared/_BookingWorkspaceTabs.cshtml','@* Dat dich vu gon: khong con tab "Thay doi / huy", "Ket qua phuc vu"; moi viec nam tren trang yeu cau. *@\n');
wr('Views/Bookings/_BookingTabs.cshtml','@model YeuCauDatDichVu\n@* Khong con tab rieng: thong tin, huy va ket qua nam chung tren trang yeu cau. *@\n');
let css=rd('wwwroot/css/social.css');
if(!css.includes('.bk-page{'))css+=`
/* Dat dich vu gon: trang yeu cau, danh sach */
.bk-page{display:grid;gap:18px}
.bk-main{display:grid;gap:16px}
.bk-title{display:flex;justify-content:space-between;align-items:flex-start;gap:12px;flex-wrap:wrap}
.bk-title h1{margin:2px 0 0;font-size:clamp(1.4rem,3vw,1.9rem)}
.bk-pill{display:inline-flex;align-items:center;border-radius:999px;padding:4px 12px;font-size:12px;font-weight:700;white-space:nowrap}
.bk-wait{background:#fff4dc;color:#8a5a00}.bk-prop{background:#efe7f7;color:#6a3f95}.bk-ok{background:#e3f4ea;color:#1d6a4f}.bk-used{background:#e6ebff;color:#2447e8}.bk-noshow,.bk-off{background:#eef0f4;color:#5c667b}
.bk-steps{list-style:none;margin:0;padding:0;display:grid;grid-template-columns:repeat(4,1fr);gap:6px}
.bk-steps li{display:grid;gap:6px;font-size:12px;color:var(--muted)}
.bk-steps li::before{content:"";height:6px;border-radius:6px;background:var(--line)}
.bk-steps li.done::before{background:var(--teal)}.bk-steps li.now::before{background:linear-gradient(90deg,var(--teal) 50%,var(--line) 50%)}
.bk-steps li.done,.bk-steps li.now{color:var(--ink);font-weight:600}
.bk-facts{display:grid;grid-template-columns:max-content 1fr;gap:8px 18px;margin:0}
.bk-facts dt{color:var(--muted)}.bk-facts dd{margin:0}.bk-facts small{display:block;color:var(--muted)}
.bk-changes{border-radius:12px;background:#f6f1fb;padding:12px 14px}.bk-changes ul{margin:6px 0 0;padding-left:18px}.bk-changes s{color:var(--muted)}
.bk-note{margin:0;border-radius:12px;background:var(--teal-light);padding:10px 14px}
.bk-actions{display:flex;flex-wrap:wrap;gap:10px;align-items:center}.bk-actions form{display:flex;flex-wrap:wrap;gap:10px}
.bk-more{border-top:1px solid var(--line);padding-top:12px}
.bk-more>summary{cursor:pointer;font-weight:650;color:var(--teal);min-height:36px;display:flex;align-items:center}
.bk-more form{display:grid;gap:12px;margin-top:10px}
.bk-stop>summary{color:#b3362f}
.bk-reason{display:grid;gap:4px;font-size:13px;font-weight:600;color:var(--muted)}
.bk-chips{display:flex;flex-wrap:wrap;gap:8px}
.bk-chips a{border-radius:999px;padding:7px 14px;font-size:14px;font-weight:600;border:1px solid var(--line);background:#fff;color:var(--muted);text-decoration:none}
.bk-chips a.on{background:var(--ink);border-color:var(--ink);color:#fff}
.bk-chips em{font-style:normal;opacity:.7;margin-left:6px}
.bk-list{list-style:none;margin:0;padding:0;display:grid;gap:10px}
.bk-item{background:#fff;border:1px solid var(--line);border-radius:14px;padding:14px 16px;display:flex;justify-content:space-between;gap:12px;align-items:center;flex-wrap:wrap}
.bk-item-main{display:grid;gap:2px;color:inherit;text-decoration:none;min-width:0;flex:1 1 320px}
.bk-item-main:hover b{color:var(--teal)}
.bk-item-side{display:flex;gap:8px;align-items:center;flex-wrap:wrap}
.bk-item-side form{margin:0}
@media(max-width:560px){.bk-facts{grid-template-columns:1fr}.bk-facts dd{margin-bottom:6px}.bk-steps{grid-template-columns:repeat(2,1fr)}}
`;
wr('wwwroot/css/social.css',css);
console.log('ok');
