// Khung chung: khach du lich dung thanh tren duy nhat (khong thanh ben, khong duong dan); nha cung cap / quan tri giu menu ben.
const fs=require('fs'),path=require('path');const root=path.join(__dirname,'../../src/HoTroDuLich.Web');
const edit=(file,pairs)=>{const p=path.join(root,file);let s=fs.readFileSync(p,'utf8');for(const [a,b] of pairs){if(s.includes(b))continue;if(!s.includes(a))throw Error(file+': khong thay '+a.slice(0,70));s=s.replace(a,()=>b);}fs.writeFileSync(p,s);};
edit('Views/Shared/_Layout.cshtml',[
 [' var tripArea=new[]',' if(traveler)workspace=false; // khach: thanh tren duy nhat, khong thanh ben\n var tripArea=new[]'],
 ['  @if(traveler||provider){<a class="@(route=="Social"?"active":"")" href="/bang-tin">Bảng tin</a>}\n  @if(!provider){<a class="@((route is "Explore" or "Discovery" or "Places")?"active":"")" href="@(traveler?"/Discovery":"/")">Khám phá</a>}\n  @if(canPlan){<a class="@(tripArea?"active":"")" href="/Plans">Chuyến đi</a>}',
  '  @if(!provider){<a class="@((route is "Explore" or "Discovery" or "Places" or "Saved")?"active":"")" href="@(traveler?"/Discovery":"/")">Khám phá</a>}\n  @if(canPlan){<a class="@(tripArea?"active":"")" href="/Plans">Chuyến đi</a>}\n  @if(traveler||provider){<a class="@(route=="Social"?"active":"")" href="/bang-tin">Bảng tin</a>}'],
 [' @if(signed){\n  @if(traveler||provider){<a class="icon-button chat-link',' @if(signed){\n  @if(traveler&&!workspace){<a class="btn btn-dark nav-create" href="/lapkehoach">@Ui.Icon("plus") Tạo chuyến đi</a>}\n  @if(traveler||provider){<a class="icon-button chat-link'],
 ['<footer class="site-footer"><div class="footer-invitation">','<footer class="site-footer">@if(!signed){<div class="footer-invitation">'],
 ['@(canPlan?"Lên kế hoạch của bạn":"Khám phá Việt Nam") @Ui.Icon("arrow")</a></div><div class="footer-main">','@(canPlan?"Lên kế hoạch của bạn":"Khám phá Việt Nam") @Ui.Icon("arrow")</a></div>}<div class="footer-main">'],
]);
console.log('ok');
