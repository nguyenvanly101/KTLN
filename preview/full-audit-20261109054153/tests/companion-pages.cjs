// HTTP/HTML checks only; this is not a browser or responsive-layout test.
process.env.FULLTEXT_TEST_BASE='http://127.0.0.1:5236';
const {Client,base}=require('./fulltext-smoke.cjs');
const fs=require('node:fs'),assert=require('node:assert/strict');
const fixture=JSON.parse(fs.readFileSync('tests/companion-features-results.json','utf8')).fixture,observations=[],pages=[],assets=new Set();
function check(ok,name){assert.ok(ok,name);observations.push({name,passed:true});}
function save(passed,error){fs.writeFileSync('tests/companion-pages-results.json',JSON.stringify({passed,checks:observations.length,observations,pages,base,scope:'HTTP and server-rendered HTML; no browser interaction or viewport verification',error:error?.stack,date:new Date().toISOString()},null,2));}
(async()=>{
 const guest=new Client(),a=new Client(),admin=new Client();await a.login('khach@hanhtrinh.vn');try{await admin.login('admin@hanhtrinh.vn');}catch{await admin.login('admin@hanhtrinh.vn','HanhTrinhMoi@2026');}
 const routes=[[guest,'/Together'],[guest,'/Together/Detail/'+fixture.group],[a,'/Together/Mine'],[a,'/Together/Profile'],[a,'/Together/Edit'],[a,'/Together/Edit/'+fixture.group],...['overview','members','chat','schedule','tasks','expenses'].map(tab=>[a,`/Together/Group/${fixture.group}?tab=${tab}`]),[a,'/Together/Report?groupId='+fixture.group],[a,'/Together/Reports'],[admin,'/Together/Moderation?status=']];
 for(const [client,route] of routes){
  const r=await client.get(route);check(r.status===200,'Page renders: '+route);check((r.html.match(/<h1\b/gi)||[]).length===1,'One main heading: '+route);check(!/Unhandled exception|Developer Exception Page|asp-for=/.test(r.html),'No rendering exception: '+route);
  const ids=[...r.html.matchAll(/\bid="([^"]+)"/g)].map(m=>m[1]);check(ids.length===new Set(ids).size,'Unique element IDs: '+route);
  for(const label of r.html.matchAll(/<label\b[^>]*\bfor="([^"]+)"/g))check(ids.includes(label[1]),'Label target exists: '+route+' #'+label[1]);
  const forms=[...r.html.matchAll(/<form\b([^>]*)>([\s\S]*?)<\/form>/gi)];check(forms.length===(r.html.match(/<form\b/gi)||[]).length,'Forms have matching closing tags: '+route);
  for(const form of forms)if(/method="post"/i.test(form[1]))check(/name="__RequestVerificationToken"/.test(form[2]),'POST form includes CSRF: '+route);
  if(route.includes('/Group/'))check(/no-store/.test(r.headers.get('cache-control')||''),'Private group is not cached: '+route);
  for(const img of r.html.matchAll(/<img\b[^>]*>/gi)){check(/\balt="/.test(img[0]),'Image text alternative exists: '+route);const src=img[0].match(/\bsrc="([^"]+)"/)?.[1];if(src?.startsWith('/'))assets.add(src);}
  for(const tag of r.html.matchAll(/<(?:script|link)\b[^>]*(?:src|href)="([^" ]+\.(?:js|css)(?:\?[^"]*)?)"/gi))if(tag[1].startsWith('/'))assets.add(tag[1].replaceAll('&amp;','&'));
  pages.push(route);
 }
 for(const asset of assets)check((await guest.get(asset)).status===200,'Local asset exists: '+asset.split('?')[0]);
 const hidden=await a.get(`/Together/Report?groupId=${fixture.group}&messageId=${fixture.message}`);check(hidden.status===404,'Report form does not reveal a hidden message');
 save(true);console.log('PASS',observations.length,'HTTP/HTML page checks across',pages.length,'pages');
})().catch(error=>{save(false,error);console.error(error);process.exitCode=1;});
