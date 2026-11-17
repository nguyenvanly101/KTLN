// Hien anh tinh/thanh tu AnhDonVi (kem ghi cong), anh cua muc tra cuu kem ghi cong, va o go anh cho quan tri.
const fs=require('fs'),path=require('path');const root=path.join(__dirname,'../../src/HoTroDuLich.Web');
const rd=f=>fs.readFileSync(path.join(root,f),'utf8').split('\r\n').join('\n'),wr=(f,s)=>fs.writeFileSync(path.join(root,f),s);
const swap=(s,a,b)=>{if(s.includes(b))return s;if(!s.includes(a))throw Error('khong thay: '+a.slice(0,80));return s.replace(a,()=>b);};
let h=rd('Views/Discovery/Home.cshtml');
h=swap(h,'@model List<HomeDestination>\n','@model List<HomeDestination>\n@inject DestinationPhotos Photos\n');
h=swap(h,' ViewData["Title"]="Khám phá & tra cứu";\n',' ViewData["Title"]="Khám phá & tra cứu";\n var photos=await Photos.For(Model.Select(d=>(d.UnitId,d.Name)));\n');
h=swap(h,'@foreach(var d in Model){var photo=Ui.DestinationPhoto(d.Name);<a class="hm-dest" href="/diem-den/@d.UnitId">@if(photo!=null){<img src="@photo" alt="" width="600" height="800" loading="lazy"/>}',
 '@foreach(var d in Model){var photo=photos.GetValueOrDefault(d.UnitId);<a class="hm-dest" href="/diem-den/@d.UnitId">@if(photo!=null){<img src="@photo.Path" alt="" title="@photo.Credit" width="600" height="800" loading="lazy"/>}');
wr('Views/Discovery/Home.cshtml',h);
let x=rd('Views/Discovery/DestinationIndex.cshtml');
x=swap(x,'@model List<DestinationIndexItem>\n','@model List<DestinationIndexItem>\n@inject DestinationPhotos Photos\n');
x=swap(x,' var tab=viec!=null',' var photos=await Photos.For(Model.Where(m=>m.Count>0).Select(m=>(m.Province.Id,m.Province.Name)));\n var tab=viec!=null');
x=swap(x,'@if(Ui.DestinationPhoto(x.Province.Name) is string photo){<img src="@photo" alt="" width="600" height="800" loading="lazy"/>}','@if(photos.GetValueOrDefault(x.Province.Id) is UnitPhoto photo){<img src="@photo.Path" alt="" title="@photo.Credit" width="600" height="800" loading="lazy"/>}');
wr('Views/Discovery/DestinationIndex.cshtml',x);
let d=rd('Views/Discovery/Destination.cshtml');
d=swap(d,'@model DestinationPage\n','@model DestinationPage\n@inject DestinationPhotos Photos\n');
d=swap(d,' var photo=Ui.DestinationPhoto(u.Name);',' var unitPhoto=await Photos.For(u.Id,u.Name);var photo=unitPhoto?.Path;');
d=swap(d,'   <h1>@u.FullName</h1>\n   <p>@(Model.Total>0','   <h1>@u.FullName</h1>\n   <p>@(Model.Total>0');
d=swap(d,'  </div>\n </section>\n\n <nav class="dest-tabs"','  </div>\n  @if(unitPhoto?.Credit!=null){<a class="photo-credit" href="@unitPhoto.SourceUrl" target="_blank" rel="noopener noreferrer">@unitPhoto.Credit</a>}\n </section>\n\n <nav class="dest-tabs"');
wr('Views/Discovery/Destination.cshtml',d);
let c=rd('Views/Discovery/_Card.cshtml');
c=swap(c,'<img src="@photo" alt="@(e.ImagePath!=null?"Ảnh thực tế: "+e.Title:"")" width="480" height="320" loading="lazy"/>','<img src="@photo" alt="@(e.ImagePath!=null?"Ảnh: "+e.Title:"")" title="@(e.ImagePath==null?null:e.ImageCredit??"Ảnh do cơ sở cung cấp")" width="480" height="320" loading="lazy"/>');
wr('Views/Discovery/_Card.cshtml',c);
let css=rd('wwwroot/css/dia-gioi.css');
if(!css.includes('.photo-credit{'))css+='\n.photo-credit{position:absolute;right:10px;bottom:8px;z-index:2;max-width:70%;font-size:11px;line-height:1.3;color:#e8ecf7!important;text-decoration:none;text-align:right;opacity:.85}\n.photo-credit:hover,.photo-credit:focus-visible{opacity:1;text-decoration:underline}\n.entry-photo{margin:0 0 22px;display:grid;gap:6px}\n.entry-photo img{width:100%;max-height:420px;object-fit:cover;border-radius:18px}\n.entry-photo figcaption{font-size:12px;color:var(--ds-muted,#5c667b)}\n';
wr('wwwroot/css/dia-gioi.css',css);
console.log('ok');
