// Read-only navigation/filters regression checks. The write workflow suite supplies its own test data.
const assert=require('node:assert/strict'),fs=require('node:fs');
const base='http://localhost:5217';let checks=0;
const decode=s=>s.replace(/&#x([\da-f]+);/gi,(_,n)=>String.fromCodePoint(parseInt(n,16))).replace(/&#(\d+);/g,(_,n)=>String.fromCodePoint(+n)).replaceAll('&amp;','&').replaceAll('&quot;','"');
const check=(v,msg)=>{assert.ok(v,msg);checks++;};
class Client {
 jar=new Map();
 async request(path,data){const r=await fetch(base+path,{method:data?'POST':'GET',redirect:'manual',headers:{cookie:[...this.jar].map(([k,v])=>k+'='+v).join('; '),...(data?{'content-type':'application/x-www-form-urlencoded'}:{})},body:data?new URLSearchParams(data):undefined});for(const c of r.headers.getSetCookie()){const i=c.indexOf('=');this.jar.set(c.slice(0,i),c.slice(i+1).split(';')[0]);}return{status:r.status,html:decode(await r.text())};}
 async get(path,status=200){const r=await this.request(path);check(r.status===status,`${path}: expected ${status}, got ${r.status}`);return r.html;}
 async login(email){const p=await this.get('/Account/Login');const token=p.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/)[1];const r=await this.request('/Account/Login',{__RequestVerificationToken:token,Email:email,Password:'HanhTrinh@2026'});check(r.status===302,'Sign in '+email);}
}
const main=s=>s.match(/<main\b[\s\S]*?<\/main>/)[0];
const rows=s=>[...(s.match(/<tbody>[\s\S]*?<\/tbody>/)?.[0]??'').matchAll(/<tr>[\s\S]*?<\/tr>/g)].map(m=>m[0]);
const active=(s,href)=>{const nav=s.match(/<nav\b[^>]*class="sidebar-nav"[^>]*>[\s\S]*?<\/nav>/)?.[0];check(!!nav,'Sidebar present: '+href);const links=[...nav.matchAll(/<a\b[^>]*class="([^"]*)"[^>]*href="([^"]*)"/g)].filter(m=>m[1].includes('active'));check(links.length===1&&links[0][2]===href,'One active sidebar function: '+href);};
(async()=>{
 const admin=new Client(),provider=new Client(),traveler=new Client();await admin.login('admin@hanhtrinh.vn');await provider.login('nhacungcap@hanhtrinh.vn');await traveler.login('khach@hanhtrinh.vn');
 const overview=await admin.get('/Admin');check(!main(overview).includes('<table')&&!main(overview).includes('<form'),'Overview contains no business tables or forms');check(!overview.includes('/Admin#'),'No hash links substitute for function pages');active(overview,'/Admin');
 const configs=[
  ['/Admin/ServiceReviews','/Admin/Review/','/Admin/Verification/','/Admin/Provider/','DaDuyet','Đã duyệt'],
  ['/Admin/Verifications','/Admin/Verification/','/Admin/Review/','/Admin/Provider/','DaXacMinh','Đã xác minh'],
  ['/Admin/Providers','/Admin/Provider/','/Admin/Review/','/Admin/Verification/','DangHoatDong','Đang hoạt động']
 ];
 for(const [route,own,other1,other2,status,label]of configs){
  const all=await admin.get(route+'?status=');active(all,route);check(rows(all).length>0,'All statuses actually loads rows: '+route);check(!main(all).includes(other1)&&!main(all).includes(other2),'List contains only its own records: '+route);
  const filtered=await admin.get(route+'?status='+status);check(rows(filtered).every(r=>r.includes(label)),'Status filter matches every returned row: '+route);
  const empty=await admin.get(route+'?status=&q=no-match-function-928374');check(rows(empty).length===0&&main(empty).includes('0 kết quả'),'Search filters actual data: '+route);
  await admin.get(route+'?status=unknown',400);
  const bounded=await admin.get(route+'?status=&page=-8');check(main(bounded).includes('Trang 1 /'),'Invalid page number is bounded: '+route);
  const detail=await admin.get(own+'1');active(detail,route);check(main(detail).includes('class="back" href="'+route+'"'),'Detail returns to correct list: '+route);
 }
 const verification=await provider.get('/Provider/Verification');active(verification,'/Provider/Verification');check(!main(verification).includes('action="/Provider/Explain"')&&!main(verification).includes('Quyết định và giải trình'),'Verification page excludes rights management');
 const rights=await provider.get('/Provider/Rights');active(rights,'/Provider/Rights');check(!main(rights).includes('action="/Provider/Supplement"')&&!main(rights).includes('action="/Provider/Reapply"'),'Rights page excludes verification forms');
 for(const client of [provider,traveler])for(const route of ['/Bookings','/Bookings/Adjustments','/Bookings/Results'])active(await client.get(route),route);
 const incidents=await traveler.get('/Support?planId=1');check(!main(incidents).includes('action="/Support/Create"'),'Incident list excludes creation form');const create=await traveler.get('/Support/Create?planId=1');check(main(create).includes('action="/Support/Create"'),'Dedicated incident creation form exists');
 fs.writeFileSync('D:/KLTN/tests/function-pages-results.json',JSON.stringify({passed:true,checks,date:new Date().toISOString()},null,2));console.log(`PASS: ${checks} independent function page checks (filters, isolation, active navigation, backlinks).`);
})().catch(e=>{console.error(e);process.exitCode=1});
