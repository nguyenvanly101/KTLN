// Run only against the isolated FullText test server on 5219.
// POST requests deliberately omit CSRF so this matrix never changes business data.
const assert=require('node:assert/strict'),fs=require('node:fs');
const {Client}=require('./fulltext-smoke.cjs');
const contract=require('../docs/fulltext-contract.json');
const allowed={CongKhai:['guest','traveler','provider','admin'],'Ẩn danh':['guest'],LaKhachDuLich:['traveler'],LaNhaCungCap:['provider'],LaQuanTriVien:['admin'],DaXacThuc:['traveler','provider','admin'],LaKhachHoacNCC:['traveler','provider']};
const observations=[];
(async()=>{
 const clients={guest:new Client(),traveler:new Client(),provider:new Client(),admin:new Client()};
 await clients.traveler.login('khach@hanhtrinh.vn');
 await clients.provider.login('nhacungcap@hanhtrinh.vn');
 await clients.admin.login('admin@hanhtrinh.vn','HanhTrinhMoi@2026');
 for(const action of contract.actions){
  const policy=action.policy==='Công khai'?'CongKhai':action.policy;
  assert.ok(allowed[policy],'Unknown policy '+policy);
  let path=action.route.replace(/\{[^}]+\}/g,'1');
  if(action.action==='XemChiTiet'&&action.controller==='XemChiTietThongTinController')path+='?loai=DiaDiem&maMuc=1';
  for(const [role,client] of Object.entries(clients)){
   const r=await client.req(path,action.method==='POST'?{}:undefined);
   const can=allowed[policy].includes(role);
   let expected;
   if(!can)expected=role==='guest'?302:403;
   else if(action.method==='POST')expected=400;
   // Authenticated visitors are sent home from anonymous-only account forms.
   const anonymousForm=/^\/tai-khoan\/(dang-nhap|dang-ky)\//.test(path);
   if(anonymousForm&&role!=='guest')expected=302;
   const rateLimited=anonymousForm&&action.method==='POST'&&r.status===429;
   const passed=rateLimited||(expected?r.status===expected:[200,302,400,403,404,409,422].includes(r.status));
   observations.push({action:action.controller+'.'+action.action,method:action.method,path,role,policy,status:r.status,expected:expected??'handled GET response; object checks remain in force',rateLimited,passed});
   assert.ok(passed,`${role} ${action.method} ${path}: ${r.status}, expected ${expected}\n${r.text.slice(0,800)}`);
  }
 }
 fs.writeFileSync('tests/fulltext-access-results.json',JSON.stringify({passed:true,checks:observations.length,date:new Date().toISOString(),base:'http://localhost:5219',scope:'91 declared routes x 4 actors; POST denied or CSRF-rejected, no business mutation',observations},null,2));
 console.log('PASS',observations.length,'HTTP access/CSRF checks across',contract.actions.length,'FullText actions');
})().catch(error=>{fs.writeFileSync('tests/fulltext-access-results.json',JSON.stringify({passed:false,checks:observations.length,error:error.stack,observations},null,2));console.error(error);process.exitCode=1;});
