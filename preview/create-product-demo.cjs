// Readable UI fixture created through HTTP, only on the isolated product server.
process.env.FULLTEXT_TEST_BASE='http://localhost:5221';
const {Client,val,base}=require('../tests/fulltext-smoke.cjs');
const assert=require('node:assert/strict'),fs=require('node:fs');
assert.equal(base,'http://localhost:5221');
const date=n=>new Date(Date.now()+n*86400000+7*3600000).toISOString().slice(0,10);
async function ok(promise){const r=await promise;assert.equal(r.status,302,r.text.split('HEADERS')[0].replace(/<[^>]+>/g,' ').slice(0,1800));return r;}
(async()=>{
 const c=new Client();await c.login('khach@hanhtrinh.vn');
 if(fs.existsSync('preview/product-demo.json')){const old=JSON.parse(fs.readFileSync('preview/product-demo.json'));if((await c.get('/Plans/Detail/'+old.plan)).status===200){console.log('Existing demo',old.plan);return;}}
 let page=await c.get('/Plans/Edit');
 const r=await ok(c.post('/Plans/Edit',page,{Name:'Đà Lạt — nhẹ nhàng cuối tuần',Destination:'Đà Lạt',Start:date(7),End:date(9),Budget:3000000}));
 const plan=Number(r.location.match(/\d+/)[0]);assert.ok(plan>0);
 for(const [day,time,content,cost,service]of [[7,'14:00','Nhận phòng giữa thung lũng',650000,1],[8,'05:00','Săn mây đón bình minh',350000,2],[8,'16:00','Dạo quanh hồ Xuân Hương',0,null]]){
  page=await c.get('/Plans/Activity/'+plan);
  await ok(c.post('/Plans/Activity',page,{PlanId:plan,Version:val(page,'Version'),Date:date(day),Time:time,Content:content,Cost:cost,...(service?{ServiceId:service}:{})}));
 }
 page=await c.get('/Preparation?planId='+plan);await ok(c.post('/Preparation/Defaults',page,{planId:plan}));
 await ok(c.post('/Preparation/Save',page,{PlanId:plan,Title:'Kiểm tra giờ xe và điểm đón',Category:'DiChuyen',Due:date(5),Remind:true}));
 await ok(c.post('/Preparation/Save',page,{PlanId:plan,Title:'Mang áo ấm cho buổi săn mây',Category:'HanhLy',Due:date(6),Remind:true}));
 for(const [Title,Category,Amount]of [['Đặt cọc phòng','LuuTru',500000],['Vé xe đến Đà Lạt','DiChuyen',350000],['Chuẩn bị vật dụng','Khac',120000]]){
  page=await c.get('/Expenses?planId='+plan);
  await ok(c.post('/Expenses/Save',page,{PlanId:plan,Title,Category,Amount,SpentOn:date(0),SubmissionId:val(page,'SubmissionId'),Notes:'Đã thanh toán trước chuyến đi'}));
 }
 fs.writeFileSync('preview/product-demo.json',JSON.stringify({plan,base,database:'HoTroDuLich_Product_20261105',created:new Date().toISOString()},null,2));console.log('Created isolated demo trip',plan);
})().catch(e=>{console.error(e);process.exitCode=1});
