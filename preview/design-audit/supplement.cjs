// Targeted design audit. This port is bound to a separate disposable audit database.
const fs = require('node:fs');
const assert = require('node:assert/strict');
const base = 'http://localhost:5218';
const decode = s => s.replace(/&#x([\da-f]+);/gi, (_,n)=>String.fromCodePoint(parseInt(n,16))).replace(/&#(\d+);/g,(_,n)=>String.fromCodePoint(+n)).replaceAll('&amp;','&').replaceAll('&quot;','"');
const val=(s,n)=>decode((typeof s==='string'?s:s.html).match(new RegExp('name="'+n+'"[^>]*value="([^"]*)"'))?.[1]||'');
const form=(s,a,id)=>[...s.html.matchAll(/<form\b[\s\S]*?<\/form>/g)].map(m=>m[0]).find(f=>f.includes('action="'+a+'"')&&(id==null||val(f,'Id')===String(id)));
class Client {
 cookies=new Map();
 async req(path,data) {
  const r=await fetch(base+path,{method:data?'POST':'GET',redirect:'manual',headers:{cookie:[...this.cookies].map(([k,v])=>k+'='+v).join('; '),...(data?{'content-type':'application/x-www-form-urlencoded'}:{})},body:data?new URLSearchParams(data):undefined});
  for(const c of r.headers.getSetCookie()){const s=c.split(';')[0],i=s.indexOf('=');this.cookies.set(s.slice(0,i),s.slice(i+1));}
  const html=await r.text();return {status:r.status,location:r.headers.get('location'),html,text:decode(html)};
 }
 get(p){return this.req(p);}
 post(p,page,data){assert.ok(val(page,'__RequestVerificationToken'),'Missing CSRF '+p);return this.req(p,{__RequestVerificationToken:val(page,'__RequestVerificationToken'),...data});}
 async login(email){const r=await this.post('/Account/Login',await this.get('/Account/Login'),{Email:email,Password:'HanhTrinh@2026'});assert.equal(r.status,302);}
}
(async()=>{
const a=new Client(),p=new Client();await a.login('khach@hanhtrinh.vn');await p.login('nhacungcap@hanhtrinh.vn');
const file='D:/KLTN/preview/design-audit/probe-results.json',report=JSON.parse(fs.readFileSync(file));
const add=(id,expected,actual,evidence={})=>{report.findings.push({id,expected,actual,evidence});console.log(id,actual);};
let page=await a.get('/Plans/Edit/2');let selected=page.html.match(/<option\b[^>]*value="([^"]+)"[^>]*selected[^>]*>/)?.[1];report.findings.find(x=>x.id==='PLAN_RESET').actual=selected;console.log('PLAN_RESET',selected);
page=await a.get('/Account/Profile');add('PROFILE_DEFAULT','RiengTu',page.html.match(/<option\b[^>]*value="([^"]+)"[^>]*selected[^>]*>/)?.[1]);
let book=await a.get('/Bookings/Create/2');const today='2026-11-04';let r=await a.post('/Bookings/Create',book,{ServiceId:2,Version:val(book,'Version'),SubmissionId:val(book,'SubmissionId'),Start:today,End:today,People:1,Quantity:1});assert.equal(r.status,302);const bid=r.location.split('/').pop();page=await p.get('/Bookings/Detail/'+bid);let f=form(page,'/Bookings/Respond');r=await p.post('/Bookings/Respond',f,{Id:bid,Version:val(f,'Version'),Decision:'DaXacNhan'});assert.equal(r.status,302);page=await a.get('/Bookings/Change/'+bid);r=await a.post('/Bookings/Change',page,{Id:bid,Version:val(page,'Version'),Start:today,End:today,People:2,Quantity:1,Kind:'ThayDoi',Reason:'Audit deadline',Terms:'Two people'});add('CHANGE_ON_SERVICE_DAY','422',r.status,{bookingId:bid});
page=await a.get('/Plans/Edit/2');r=await a.post('/Plans/Edit',page,{Id:2,Version:val(page,'Version'),Name:'',Destination:'Đà Lạt',Start:'2026-12-10',End:'2026-12-12',Status:'Nhap'});add('INVALID_FORM_PRESERVES_INPUT','400 with form and entered data',{status:r.status,hasForm:r.html.includes('action="/Plans/Edit"'),hasDestinationInput:r.html.includes('name="Destination"')});
fs.writeFileSync(file,JSON.stringify(report,null,2));
})().catch(e=>{console.error(e);process.exitCode=1;});