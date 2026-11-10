const assert=require('node:assert/strict');
const fs=require('node:fs');
const base=process.env.UI_TEST_BASE||'http://localhost:5216';
const observations=[];
const decode=s=>s.replace(/&#x([\da-f]+);/gi,(_,n)=>String.fromCodePoint(parseInt(n,16))).replace(/&#(\d+);/g,(_,n)=>String.fromCodePoint(+n)).replaceAll('&amp;','&').replaceAll('&quot;','"');
function check(ok,name){assert.ok(ok,name);observations.push({name,passed:true});}
class Session{
 cookies=new Map();
 async request(path,data){
  const response=await fetch(base+path,{method:data?'POST':'GET',headers:{cookie:[...this.cookies].map(([k,v])=>k+'='+v).join('; '),...(data?{'content-type':'application/x-www-form-urlencoded'}:{})},body:data?new URLSearchParams(data):undefined,redirect:'manual'});
  for(const cookie of response.headers.getSetCookie()){const part=cookie.split(';')[0],at=part.indexOf('=');this.cookies.set(part.slice(0,at),part.slice(at+1));}
  const html=await response.text();return {status:response.status,html,text:decode(html),location:response.headers.get('location')};
 }
 async page(path){const result=await this.request(path);check(result.status===200,'GET '+path+' returns 200');check(!/asp-for=|Unhandled exception|Developer Exception Page/.test(result.html),'Razor page renders: '+path);return result;}
 async login(email,password='HanhTrinh@2026'){
  const page=await this.page('/Account/Login');
  const token=decode(page.html.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/)[1]);
  const result=await this.request('/tai-khoan/dang-nhap/dang-nhap',{__RequestVerificationToken:token,dinhDanh:email,matKhau:password});
  check(result.status===302,'Login '+email);
  check(result.location!=='/tai-khoan/doi-mat-khau','Existing demo account can open workspace: '+email);
 }
}
(async()=>{
 const guest=new Session();
 const home=await guest.page('/');
 check(home.text.includes('Việt Nam rộng mở.'),'New discovery hero is rendered');
 check((home.html.match(/class="nav-disclosure/g)||[]).length===3,'Public navigation has three related groups');
 check(home.html.includes('href="/Bookings"')&&home.html.includes('href="/Companions/Mine"'),'Trip and companion workspaces are reachable');
 const search=await guest.page('/?q='+encodeURIComponent('Mây')+'&region='+encodeURIComponent('Đà Lạt')+'&type=LuuTru&min=100000&max=1000000');
 check(!search.html.includes('id="discovery-title"'),'Search results prioritize the filter and results');
 for(const [name,value] of [['q','Mây'],['region','Đà Lạt'],['min','100000'],['max','1000000']])check(search.text.includes(value),'Search preserves '+name);
 const companions=await guest.page('/dong-hanh/tim-kiem/tim-nhu-cau?noiDen='+encodeURIComponent('Đà Lạt')+'&nganSachToiDa=2000000&hinhThucDi=Phuot&tuNgay=2026-11-11&denNgay=2026-11-30');
 check(/class="companion-extra" open/.test(companions.html),'Additional companion filters stay open after use');
 check(!companions.html.includes('class="community-intro"'),'Companion results prioritize filters and invitations');
 check(companions.text.includes('2000000')&&companions.text.includes('2026-11-11'),'Companion filter values are retained');
 for(const path of ['/Companions','/Account/Register','/Explore/Place/1','/Explore/Detail/1'])await guest.page(path);
 for(const path of ['/css/experience.css','/js/site.js','/images/icons.svg','/images/vietnam-bay.jpg','/images/hoian.jpg','/images/dalat.jpg']){const asset=await fetch(base+path);check(asset.status===200,'Asset '+path+' loads');await asset.arrayBuffer();}
 const traveler=new Session();await traveler.login('khach@hanhtrinh.vn');
 const plans=await traveler.page('/Plans');
 const sidebar=plans.text.match(/<aside\b[^>]*class="workspace-sidebar"[\s\S]*?<\/aside>/)[0];
 check(sidebar.includes('Chuyến đi')&&sidebar.includes('Đồng hành'),'Traveler workspace groups trips and companions');
 check(sidebar.includes('/Companions/Mine'),'Traveler can find personal companion posts from the grouped menu');
 check(!sidebar.includes('/Bookings/Adjustments')&&!sidebar.includes('/Bookings/Results'),'Booking operations are nested within their owning workspace');
 for(const path of ['/Plans/Edit','/Plans/Detail/1','/Companions/Mine','/Companions/Connections','/Companions/Edit','/Support','/Account/Profile','/Account/Notifications','/Account/Password'])await traveler.page(path);
 for(const path of ['/Bookings','/Bookings/Adjustments','/Bookings/Results']){const page=await traveler.page(path);check(page.html.includes('aria-label="Quản lý đặt dịch vụ"'),'Shared booking navigation on '+path);}
 const provider=new Session();await provider.login('nhacungcap@hanhtrinh.vn');
 for(const path of ['/Provider','/Provider/Verification','/Provider/Rights','/Bookings','/Bookings/Adjustments','/Bookings/Results'])await provider.page(path);
 const denied=await provider.request('/Plans');check(denied.status===403,'Provider has no traveler write access');
 const admin=new Session();await admin.login('admin@hanhtrinh.vn',process.env.UI_ADMIN_PASSWORD||'HanhTrinh@2026');
 for(const path of ['/Admin','/Admin/ServiceReviews','/Admin/Verifications','/Admin/Providers','/Admin/Audit'])await admin.page(path);
 const adminHome=await admin.page('/Admin');check(!adminHome.html.includes('href="/Plans/Edit"'),'Administrator does not get traveler actions');
 const wrongRole=await traveler.request('/Admin');check(wrongRole.status===403,'Traveler cannot open administration');
 fs.writeFileSync(process.env.UI_TEST_RESULT||'tests/navigation-redesign-results.json',JSON.stringify({passed:true,checks:observations.length,base,date:new Date().toISOString(),observations},null,2));
 console.log('PASS',observations.length,'navigation, role, search and page rendering checks');
})().catch(error=>{fs.writeFileSync(process.env.UI_TEST_RESULT||'tests/navigation-redesign-results.json',JSON.stringify({passed:false,checks:observations.length,base,observations,error:error.stack},null,2));console.error(error);process.exitCode=1;});
