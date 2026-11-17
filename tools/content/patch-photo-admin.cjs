// Quan tri go anh minh hoa khop sai tren trang sua muc tra cuu.
const fs=require('fs'),path=require('path');const root=path.join(__dirname,'../../src/HoTroDuLich.Web');
const rd=f=>fs.readFileSync(path.join(root,f),'utf8').split('\r\n').join('\n'),wr=(f,s)=>fs.writeFileSync(path.join(root,f),s);
const swap=(s,a,b)=>{if(s.includes(b))return s;if(!s.includes(a))throw Error('khong thay: '+a.slice(0,80));return s.replace(a,()=>b);};
let m=rd('Models/DiscoveryModels.cs');
m=swap(m,' [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever] public string? AttributesJson { get; set; }\n',' [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever] public string? AttributesJson { get; set; }\n [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever] public string? CurrentPhoto { get; set; }\n [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever] public string? CurrentPhotoCredit { get; set; }\n [Display(Name="Gỡ ảnh minh họa")] public bool RemovePhoto { get; set; }\n');
wr('Models/DiscoveryModels.cs',m);
let s=rd('Services/DiscoveryContent.cs');
s=swap(s,'LinkedKind=e.ServiceId!=null?"Service":e.PlaceId!=null?"Place":null,AttributesJson=e.AttributesJson};','LinkedKind=e.ServiceId!=null?"Service":e.PlaceId!=null?"Place":null,AttributesJson=e.AttributesJson,CurrentPhoto=e.ImagePath,CurrentPhotoCredit=e.ImageCredit};');
s=swap(s,'if(i.Id>0){e=await Editable(i.Id);Version(e,i.Version);}','if(i.Id>0){e=await Editable(i.Id);Version(e,i.Version);if(i.RemovePhoto){e.ImagePath=null;e.ImageCredit=null;e.ImageSourceUrl=null;}}');
wr('Services/DiscoveryContent.cs',s);
let v=rd('Views/Discovery/Edit.cshtml');
v=swap(v,' <section class="ds-panel"><h2>@(admin?"Lưu và công bố"',' @if(admin&&Model.CurrentPhoto!=null){<section class="ds-panel" id="ds-photo"><h2>Ảnh minh họa</h2><figure class="entry-photo"><img src="@Model.CurrentPhoto" alt="Ảnh hiện tại" width="480" height="240"/><figcaption>@(Model.CurrentPhotoCredit??"Ảnh do cơ sở cung cấp")</figcaption></figure><label><input type="checkbox" asp-for="RemovePhoto"/> Gỡ ảnh này khi lưu (ảnh khớp sai nơi)</label></section>}\n <section class="ds-panel"><h2>@(admin?"Lưu và công bố"');
wr('Views/Discovery/Edit.cshtml',v);
console.log('ok');
