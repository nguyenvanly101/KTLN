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
const findings=[];
function record(id,expected,actual,evidence){findings.push({id,expected,actual,evidence});console.log(id,JSON.stringify(actual));}
async function ok(promise){const r=await promise;assert.equal(r.status,302,r.text.replace(/<[^>]+>/g,' ').slice(-3000));return r;}
(async()=>{
const a=new Client(),b=new Client(),p=new Client(),admin=new Client(),guest=new Client();
await a.login('khach@hanhtrinh.vn');await b.login('linh@hanhtrinh.vn');await p.login('nhacungcap@hanhtrinh.vn');await admin.login('admin@hanhtrinh.vn');
const start='2026-12-10',end='2026-12-12',stamp=Date.now();
let r=await ok(a.post('/Plans/Edit',await a.get('/Plans/Edit'),{Name:'Design audit '+stamp,Destination:'Đà Lạt',Start:start,End:end,Status:'DaChapNhan'}));
const planId=r.location.split('/').pop();let page=await a.get('/Plans/Activity/'+planId);
await ok(a.post('/Plans/Activity',page,{PlanId:planId,Version:val(page,'Version'),Date:start,Time:'09:00',Content:'Changed after acceptance',Cost:10}));
page=await a.get('/Plans/Edit/'+planId);record('PLAN_RESET','Nhap',page.html.match(/<option\b[^>]*value="([^"]+)"[^>]*selected[^>]*>/)?.[1],{planId});
r=await a.post('/Plans/Edit',await a.get('/Plans/Edit'),{Name:'Past plan '+stamp,Destination:'Đà Lạt',Start:'2000-01-01',End:'2000-01-02',Status:'Nhap'});
record('PLAN_PAST','400',r.status,{location:r.location});
page=await a.get('/Companions/Edit');await ok(a.post('/Companions/Edit',page,{Destination:'Audit-'+stamp,Content:'Expired need '+stamp,Start:'2000-01-01',End:'2000-01-02',Privacy:'CongKhai'}));
page=await guest.get('/Companions?q=Audit-'+stamp);record('EXPIRED_NEED','not listed',page.text.includes('Expired need '+stamp),{});
await ok(a.post('/Companions/Edit',await a.get('/Companions/Edit'),{Destination:'Audit-connect-'+stamp,Content:'Need '+stamp,Start:start,End:end,Privacy:'CongKhai'}));
let mine=await a.get('/Companions/Mine');let needId=mine.html.match(/href="\/Companions\/Edit\/(\d+)"/)[1];
await ok(b.post('/Companions/Connect',await b.get('/Companions/Detail/'+needId),{Id:needId,Text:'Audit connection'}));
page=await a.get('/Companions/Connections');let f=form(page,'/Companions/Respond');let connId=val(f,'Id');
await ok(a.post('/Companions/Respond',f,{Id:connId,Version:val(f,'Version'),Decision:'DaChapNhan'}));
const proposal=async(client,scope)=>{const ag=await client.get('/Companions/Agreement/'+connId);const pf=form(ag,'/Companions/Propose');return client.post('/Companions/Propose',pf||ag,{ConnectionId:connId,Version:val(pf||ag,'Version'),Name:'Agreement',Destination:'Đà Lạt',Start:start,End:end,Scope:scope,Kind:'NoiDung'});};
await ok(proposal(a,'Version one'));page=await b.get('/Companions/Agreement/'+connId);f=form(page,'/Companions/AgreementAction');const t1=val(f,'Id');
await ok(b.post('/Companions/AgreementAction',f,{Id:t1,Version:val(f,'Version'),connectionId:connId,Decision:'DaXacNhan'}));
await ok(proposal(a,'Version two'));page=await b.get('/Companions/Agreement/'+connId);let pending=form(page,'/Companions/AgreementAction');const t2=val(pending,'Id');
page=await a.get('/Companions/Agreement/'+connId);f=form(page,'/Companions/AgreementAction',t1);
await ok(a.post('/Companions/AgreementAction',f,{Id:t1,Version:val(f,'Version'),connectionId:connId,Decision:'Rut'}));
page=await b.get('/Companions/Agreement/'+connId);pending=form(page,'/Companions/AgreementAction',t2);
record('AGREEMENT_PENDING_AFTER_WITHDRAW','replaced; cannot confirm',!!pending,{connId,t1,t2});
r=pending?await b.post('/Companions/AgreementAction',pending,{Id:t2,Version:val(pending,'Version'),connectionId:connId,Decision:'DaXacNhan'}):{status:'absent'};
record('AGREEMENT_REVIVE','rejected',r.status,{connId});
if(r.status===302){page=await a.get('/Companions/Agreement/'+connId);f=form(page,'/Companions/AgreementAction',t2);await ok(a.post('/Companions/AgreementAction',f,{Id:t2,Version:val(f,'Version'),connectionId:connId,Decision:'Rut'}));}
r=await proposal(a,'After termination');record('AGREEMENT_NEW_AFTER_END','422',r.status,{connId});
if(r.status===302){r=await b.post('/Account/Deactivate',await b.get('/Account/Deactivate'),{password:'HanhTrinh@2026',confirmation:'NGỪNG TÀI KHOẢN'});record('DEACTIVATE_PENDING_ONLY','pending proposal should be replaced; deactivation allowed',r.status,{message:r.text.replace(/<[^>]+>/g,' ').replace(/\s+/g,' ').match(/Còn thỏa thuận[^.]+/)?.[0]});}
for(const [role,client] of [['provider',p],['admin',admin]]){r=await client.get('/Explore/Index');record('PUBLIC_SEARCH_'+role,'200 per chapter 4',r.status,{});}
r=await admin.get('/Account/Profile');record('ADMIN_PROFILE','200 per 4.9.18',r.status,{});
const weakEmail='audit-'+stamp+'@example.test';page=await guest.get('/Account/Register');r=await guest.post('/Account/Register',page,{Email:weakEmail,Password:'abcdefgh',ConfirmPassword:'abcdefgh',Name:'Audit weak password',Role:'KHACH_DU_LICH'});record('WEAK_PASSWORD','400',r.status,{});
const providerEmail='audit-provider-'+stamp+'@example.test';page=await guest.get('/Account/Register');r=await guest.post('/Account/Register',page,{Email:providerEmail,Password:'HanhTrinh@2026',ConfirmPassword:'HanhTrinh@2026',Name:'Audit provider',Role:'NHA_CUNG_CAP',Phone:'091'+String(stamp).slice(-7),Organization:'Audit company',Address:'Audit address'});record('PROVIDER_NO_DOCUMENTS','400',r.status,{});
page=await p.get('/Provider/Edit/1');const fields={Id:1,Version:val(page,'Version'),Name:'Audit pause '+stamp,Type:'Lưu trú',Region:'Đà Lạt',Price:650000,PauseFrom:start,PauseTo:end};r=await p.post('/Provider/Edit',page,fields);record('PAUSE_NO_REASON','400',r.status,{});
page=await p.get('/Provider');record('MANUAL_RECEPTION','absent per BR06.01-5',page.html.includes('/Provider/Reception'),{});
fs.writeFileSync('D:/KLTN/preview/design-audit/probe-results.json',JSON.stringify({date:new Date().toISOString(),base,database:'HoTroDuLich_Audit_20261104_Design',findings},null,2));
})().catch(e=>{console.error(e);process.exitCode=1;});
