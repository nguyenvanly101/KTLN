const assert=require('node:assert/strict'),fs=require('node:fs');
const {Client,val}=require('./fulltext-smoke.cjs');
let checks=0;const observations=[];
function check(ok,name){assert.ok(ok,name);checks++;observations.push({name,passed:true});}
(async()=>{
 const guest=new Client();
 const login=await guest.get('/tai-khoan/dang-nhap/mo-dang-nhap');
 check(login.status===200,'login page');
 check(/asp-for/.test(login.html)===false,'Razor rendered');
 check(/name="Email"[^>]*type="text"|type="text"[^>]*name="Email"/.test(login.html),'identifier allows email or phone in browser');
 const traveler=new Client(),provider=new Client(),admin=new Client();
 await traveler.login('khach@hanhtrinh.vn');await provider.login('nhacungcap@hanhtrinh.vn');await admin.login('admin@hanhtrinh.vn','HanhTrinhMoi@2026');
 for(const [role,client] of Object.entries({guest,traveler,provider,admin})){
  const service=await client.get('/thong-tin/xem-chi-tiet?loai=DichVu&maMuc=1');
  check(service.status===200,role+' can read public service');
  check(service.html.includes('/Plans/Select?serviceId=')===(role==='guest'||role==='traveler'),role+' plan action matches role');
  check(service.html.includes('/Bookings/Create/')===(role==='guest'||role==='traveler'),role+' booking action matches role');
  const needs=await client.get('/dong-hanh/tim-kiem/mo-tim-kiem');
  check(needs.status===200,role+' can search companions');
  check(needs.html.includes('href="/Companions/Edit"')===(role==='guest'||role==='traveler'),role+' publish action matches role');
  check(needs.html.includes('action="/dong-hanh/tim-kiem/tim-nhu-cau"'),'companion form targets search action from '+role+' page');
 }
 const filtered=await traveler.get('/dong-hanh/tim-kiem/tim-nhu-cau?noiDen='+encodeURIComponent('Đà Lạt')+'&nganSachToiDa=2000000&hinhThucDi=Phuot&tuNgay=2026-11-11&denNgay=2026-11-30');
 check(filtered.status===200,'canonical companion filter');
 check(val(filtered,'noiDen')==='Đà Lạt','destination retained after search');
 check(val(filtered,'nganSachToiDa')==='2000000','budget retained after search');
 check(val(filtered,'tuNgay')==='2026-11-11'&&val(filtered,'denNgay')==='2026-11-30','dates retained after search');
 check(/value="Phuot"[^>]*selected/.test(filtered.html),'travel style retained after search');
 const plan=await traveler.get('/ke-hoach/mo-ke-hoach-moi');
 check(plan.html.includes('action="/ke-hoach/khoi-tao-ke-hoach"'),'new plan uses designed command');
 check(!plan.html.includes('name="Status"'),'plan confirmation is a separate action');
 const notifications=await traveler.get('/thong-bao');
 check(/action="\/thong-bao\/\d+\/mo"/.test(notifications.html),'opening a notification posts to mark it read');
 fs.writeFileSync('tests/fulltext-ui-flow-results.json',JSON.stringify({passed:true,checks,observations,date:new Date().toISOString(),base:'http://localhost:5219'},null,2));
 console.log('PASS',checks,'UI flow and role visibility checks');
})().catch(e=>{console.error(e);process.exitCode=1;});
