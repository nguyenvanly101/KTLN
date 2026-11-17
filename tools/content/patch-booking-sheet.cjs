// Trang dich vu: mot nut "Dat dich vu" mo bang noi, thay cho hai loi "Nhan tin de dat" + "Gui yeu cau theo mau".
const fs=require('fs'),path=require('path');const root=path.join(__dirname,'../../src/HoTroDuLich.Web');
const rd=f=>fs.readFileSync(path.join(root,f),'utf8').split('\r\n').join('\n'),wr=(f,s)=>fs.writeFileSync(path.join(root,f),s);
let v=rd('Views/Explore/Detail.cshtml');
const a=v.indexOf('<a class="btn btn-dark w-100 mt-3" href="/tin-nhan/dich-vu/@Model.MaDichVu');
const b=v.indexOf('Hoặc gửi yêu cầu theo mẫu</a></p>}',a);
if(a<0||b<0){if(v.includes('_BookSheet'))console.log('da sua');else throw Error('khong thay nut dat');}
else{v=v.slice(0,a)+'<partial name="/Views/Explore/_BookSheet.cshtml" model="Model"/>}'+v.slice(b+'Hoặc gửi yêu cầu theo mẫu</a></p>}'.length);}
v=v.replace('<div class="notice">Thêm vào lịch trình để dự kiến hoạt động. Nhắn tin cho cơ sở khi bạn muốn đặt dịch vụ.</div>','');
if(!v.includes('booking-sheet.js'))v=v.trimEnd()+'\n@section Scripts{<script src="~/js/booking-sheet.js" asp-append-version="true"></script>}\n';
wr('Views/Explore/Detail.cshtml',v);
let css=rd('wwwroot/css/social.css');
if(!css.includes('.book-sheet{'))css+=`
/* Bang dat dich vu */
.book-sheet{border:0;border-radius:20px;padding:0;width:min(520px,calc(100vw - 32px));max-height:calc(100dvh - 32px);box-shadow:0 24px 70px #15213e40}
.book-sheet::backdrop{background:#15213e66}
.book-form{display:grid;gap:14px;padding:22px}
.book-head{display:flex;justify-content:space-between;gap:12px;align-items:flex-start}
.book-head h2{margin:0;font-size:1.3rem}.book-head p{margin:2px 0 0;color:var(--muted)}
.book-fields{display:grid;grid-template-columns:1fr 1fr;gap:12px}
.book-fields label{display:grid;gap:4px;font-size:13px;font-weight:600;color:var(--muted)}
.book-fields .full{grid-column:1/-1}
.book-warn{font-size:13px;border-radius:10px;padding:8px 12px;background:#fff8eb;color:#8a5a00}
.book-warn.bad{background:#fdecea;color:#b3362f}
.book-est{display:flex;justify-content:space-between;align-items:baseline;gap:12px;border-top:1px dashed var(--line);padding-top:12px;color:var(--muted);font-size:13px}
.book-est strong{font-size:1.3rem;color:var(--ink)}
@media(max-width:480px){.book-fields{grid-template-columns:1fr}}
`;
wr('wwwroot/css/social.css',css);
console.log('ok');
