const fs=require('node:fs'),assert=require('node:assert/strict');
const {Client,sql}=require('./fulltext-helpers.cjs');
const observations=[];let checks=0;
function verify(ok,name){assert.ok(ok,name);checks++;observations.push({name,passed:true});}
async function status(request,expected,name){const r=await request;verify(r.status===expected,name+': '+r.status+' expected '+expected);return r;}
(async()=>{
 const marker=sql('SELECT COALESCE(MAX(MaNhatKy),0) AS last FROM NhatKyThaoTac')[0].last;
 const traveler=new Client(),provider=new Client(),unknown=new Client();
 await traveler.login('khach@hanhtrinh.vn');await provider.login('nhacungcap@hanhtrinh.vn');
 const login=await unknown.get('/tai-khoan/dang-nhap/mo-dang-nhap');
 const identity='security-probe-'+Date.now()+'@example.invalid',password='NeverLogThis@2026';
 await status(unknown.post('/tai-khoan/dang-nhap/dang-nhap',login,{dinhDanh:identity,matKhau:password}),400,'unknown login');
 await status(provider.get('/ke-hoach'),403,'provider denied traveler plan');
 await status(traveler.req('/ke-hoach/cap-nhat-thong-tin',{}),400,'missing CSRF');
 const plan=await traveler.get('/ke-hoach');
 verify(!plan.headers.has('server'),'Kestrel Server header disabled');
 verify(plan.headers.get('x-frame-options')==='DENY','frame protection');
 verify(plan.headers.get('x-content-type-options')==='nosniff','MIME sniffing protection');
 verify(plan.headers.get('content-security-policy')?.includes("form-action 'self'"),'same-origin form policy');
 let logs=sql('SELECT MaTaiKhoan,LoaiSuKien,LyDo FROM NhatKyThaoTac WHERE MaNhatKy>'+marker);
 verify(logs.some(n=>n.LoaiSuKien==='DangNhapThanhCong'&&n.MaTaiKhoan===1),'successful login identifies account in audit');
 verify(logs.some(n=>n.LoaiSuKien==='DangNhapThatBai'&&n.LyDo?.includes('***')),'failed login independently persists masked identity');
 verify(!JSON.stringify(logs).includes(identity)&&!JSON.stringify(logs).includes(password),'audit excludes raw identifier and password');
 verify(logs.some(n=>n.LoaiSuKien==='TuChoiQuyen'&&n.MaTaiKhoan===2),'SK07 refusal persists outside rejected request');
 verify(logs.some(n=>n.LoaiSuKien==='AntiforgeryKhongHopLe'&&n.MaTaiKhoan===1),'SK08 records actual antiforgery rejection');
 // A fresh server has exactly three auth attempts above. The limiter includes
 // successful login attempts as specified (10 attempts/minute/IP).
 for(let attempt=4;attempt<=11;attempt++){
  const r=await unknown.post('/tai-khoan/dang-nhap/dang-nhap',login,{dinhDanh:identity,matKhau:password});
  verify(r.status===(attempt===11?429:400),'auth attempt '+attempt+' has expected status');
 }
 logs=sql('SELECT LoaiSuKien,LyDo FROM NhatKyThaoTac WHERE MaNhatKy>'+marker);
 verify(logs.some(n=>n.LoaiSuKien==='VuotGioiHanTanSuat'),'SK08 rate-limit rejection persists');
 await status(traveler.post('/tai-khoan/dang-xuat',plan,{}),302,'logout');
 verify(sql("SELECT COUNT(*) AS n FROM NhatKyThaoTac WHERE MaNhatKy>"+marker+" AND LoaiSuKien=N'DangXuat'")[0].n===1,'SK02 logout audit');
 fs.writeFileSync('tests/fulltext-security-results.json',JSON.stringify({passed:true,checks,observations,date:new Date().toISOString(),database:'HoTroDuLich_Branches_Audit_20261109054153',base:'http://127.0.0.1:5236'},null,2));
 console.log('PASS',checks,'security and durable-audit checks');
})().catch(e=>{console.error(e);process.exitCode=1;});
