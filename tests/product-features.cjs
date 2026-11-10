// All mutations target the explicitly isolated local product test database/server.
const companionExtension=process.env.TRAVEL_COMPANION_EXTENSION==='1';
const journeyExtension=process.env.TRAVEL_JOURNEY_EXTENSION==='1'||companionExtension;
process.env.FULLTEXT_TEST_BASE=companionExtension?'http://localhost:5223':journeyExtension?'http://localhost:5222':'http://localhost:5221';
const {Client,val,decode,base}=require('./fulltext-smoke.cjs');
const fs=require('node:fs'),assert=require('node:assert/strict'),{execFileSync}=require('node:child_process');
const database=companionExtension?'HoTroDuLich_Companion_20261105':journeyExtension?'HoTroDuLich_Journey_20261105':'HoTroDuLich_Product_20261105', observations=[], fixture={stamp:Date.now()};
function check(ok,name){assert.ok(ok,name);observations.push({name,passed:true});}
function sql(query){assert.match(query,/^SELECT\b/i);const raw=execFileSync('sqlcmd',['-S','(localdb)\\MSSQLLocalDB','-d',database,'-E','-C','-b','-y','0','-w','65535','-f','65001','-Q','SET NOCOUNT ON; '+query+' FOR JSON PATH;'],{encoding:'utf8',maxBuffer:4*1024*1024});return raw.includes('[')?JSON.parse(raw.slice(raw.indexOf('['),raw.lastIndexOf(']')+1).replace(/[\r\n]/g,'')):[];}
const row=(table,key,id)=>sql(`SELECT *,CONVERT(varbinary(8),RowVersion) AS Version FROM ${table} WHERE ${key}=${Number(id)}`)[0];
async function status(promise,expected,name){const r=await promise;check(r.status===expected,`${name}: HTTP ${r.status}, expected ${expected} ${r.status!==expected?r.text.split('HEADERS')[0].replace(/<[^>]+>/g,' ').slice(0,1700):''}`);return r;}
const today=new Date(Date.now()+7*3600000).toISOString().slice(0,10),future=new Date(Date.now()+15*86400000).toISOString().slice(0,10);
function save(passed,error){fs.writeFileSync(companionExtension?'tests/companion-product-regression-results.json':journeyExtension?'tests/journey-product-regression-results.json':'tests/product-features-results.json',JSON.stringify({passed,checks:observations.length,observations,fixture,database,base,date:new Date().toISOString(),error:error?.stack},null,2));}
async function multipart(client,path,csrf,values,photo=true){const form=new FormData();form.append('__RequestVerificationToken',val(csrf,'__RequestVerificationToken'));for(const [key,value]of Object.entries(values))form.append(key,String(value));if(photo)form.append('Photos',new Blob([Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=','base64')],{type:'image/png'}),'anh.png');return client.req(path,form);}
(async()=>{
 check(sql('SELECT DB_NAME() AS Name')[0].Name===database,'Expected isolated database');
 const a=new Client(),b=new Client(),p=new Client(),admin=new Client(),guest=new Client();
 await a.login('khach@hanhtrinh.vn');await b.login('linh@hanhtrinh.vn');await p.login('nhacungcap@hanhtrinh.vn');
 try{await admin.login('admin@hanhtrinh.vn');}catch{await admin.login('admin@hanhtrinh.vn','HanhTrinhMoi@2026');}
 const token=await a.get('/Plans'),btoken=await b.get('/Plans');
 for(const route of ['/Saved','/Preparation?planId=1','/Expenses?planId=1']){await status(guest.get(route),302,'Guest cannot open '+route);await status(p.get(route),403,'Provider cannot open '+route);}
 await status(b.get('/Preparation?planId=1'),404,'Foreign preparation page denied');await status(b.get('/Expenses?planId=1'),404,'Foreign expenses page denied');
 await status(a.post('/Saved/Add',token,{ServiceId:1,ReturnUrl:'https://example.com'}),302,'Save a service');
 let page=await status(a.get('/Saved'),200,'Saved page renders');check(page.text.includes('Mây trong thung lũng'),'Saved item uses published name');
 await status(a.post('/Saved/Add',token,{ServiceId:1}),302,'Repeated save is safe');
 check(sql('SELECT MaLuaChon FROM LuaChonDaLuu WHERE MaTaiKhoan=1 AND MaDichVu=1').length===1,'Repeated saves do not duplicate records');
 await status(a.post('/Saved/Add',token,{ServiceId:2}),302,'Save second service');await status(a.post('/Saved/Add',token,{PlaceId:1}),302,'Save a place');
 const choices=sql('SELECT * FROM LuaChonDaLuu WHERE MaTaiKhoan=1');const first=choices.find(x=>x.MaDichVu===1).MaLuaChon,second=choices.find(x=>x.MaDichVu===2).MaLuaChon,place=choices.find(x=>x.MaDiaDiem===1).MaLuaChon;
 fixture.saved=first;
 page=await status(a.get(`/Saved/Compare?ids=${first}&ids=${second}`),200,'Compare two owned services');check(page.text.includes('Mây trong thung lũng')&&page.text.includes('Săn mây đón bình minh'),'Comparison includes both services');
 await status(a.get(`/Saved/Compare?ids=${first}`),400,'Comparison requires two services');await status(b.get(`/Saved/Compare?ids=${first}&ids=${second}`),404,'Foreign saved comparison denied');await status(a.get(`/Saved/Compare?ids=${first}&ids=${place}`),404,'Place cannot masquerade as a service comparison');
 await status(a.post('/Saved/Add',token,{ServiceId:1,PlaceId:1}),400,'Ambiguous saved item rejected');await status(a.post('/Saved/Add',token,{ServiceId:999999}),404,'Unpublished service cannot be saved');
 let r=await a.post('/Saved/Add',token,{PlaceId:2,ReturnUrl:'//example.com'});check(r.location==='/Saved','External return redirect rejected');
 await status(b.post('/Saved/Remove',btoken,{Id:first}),404,'Cannot remove someone else saved choice');await status(a.req('/Saved/Remove',{Id:first}),400,'Saved removal requires CSRF');
 const removable=sql('SELECT MaLuaChon FROM LuaChonDaLuu WHERE MaTaiKhoan=1 AND MaDiaDiem=2')[0];await status(a.post('/Saved/Remove',token,{Id:removable.MaLuaChon}),302,'Owner can remove saved choice');check(sql('SELECT MaLuaChon FROM LuaChonDaLuu WHERE MaTaiKhoan=1 AND MaDiaDiem=2').length===0,'Removed saved choice is no longer listed');
 page=await status(a.get('/Preparation?planId=1'),200,'Preparation empty page renders');check(!page.html.includes('name="Version" value=""'),'New preparation form does not submit an empty concurrency token');
 await status(a.post('/Preparation/Defaults',page,{planId:1}),302,'Add suggested preparation');await status(a.post('/Preparation/Defaults',page,{planId:1}),302,'Add suggested preparation repeatedly');
 check(sql('SELECT * FROM MucChuanBi WHERE MaKeHoach=1').filter(x=>x.NoiDung==='Kiểm tra giấy tờ cần mang theo').length===1,'Template does not duplicate tasks');
 const checklist={PlanId:1,Title:'Kiểm tra vé '+fixture.stamp,Category:'GiayTo',Due:today,Remind:true};
 page=await status(a.post('/Preparation/Save',page,{...checklist,Title:''}),400,'Preparation validates title');check(page.text.includes(today)&&page.text.includes('selected="selected"'),'Preparation validation retains date and category');
 await status(a.post('/Preparation/Save',page,{...checklist,Due:''}),422,'Reminder requires due date');await status(a.post('/Preparation/Save',page,{...checklist,Category:'Unknown'}),400,'Unknown preparation category rejected');
 await status(a.post('/Preparation/Save',page,checklist),302,'Save personal preparation task');
 const task=fixture.task=sql(`SELECT MaMuc FROM MucChuanBi WHERE NoiDung=N'${checklist.Title}'`)[0].MaMuc;
 let item=row('MucChuanBi','MaMuc',task);
 await status(a.post('/Preparation/Action',token,{planId:1,Id:task,Version:item.Version,Decision:'Done'}),302,'Complete preparation task');check(row('MucChuanBi','MaMuc',task).DaHoanThanh===true,'Completed state stored');
 await status(a.post('/Preparation/Action',token,{planId:1,Id:task,Version:item.Version,Decision:'Undo'}),409,'Stale preparation change denied');
 await status(a.post('/Preparation/Action',token,{planId:1,Id:task,Version:row('MucChuanBi','MaMuc',task).Version,Decision:'Undo'}),302,'Reopen preparation task');
 await status(b.post('/Preparation/Action',btoken,{planId:1,Id:task,Version:row('MucChuanBi','MaMuc',task).Version,Decision:'Delete'}),404,'Cannot change foreign preparation');
 page=await a.get('/Preparation?planId=1&edit='+task);check(val(page,'Title')===checklist.Title,'Edit preparation prefilled');
 await status(a.post('/Preparation/Save',page,{...checklist,Id:task,Version:row('MucChuanBi','MaMuc',task).Version,Title:checklist.Title+' đã sửa'}),302,'Edit preparation task');
 page=await status(a.get('/Expenses?planId=1'),200,'Expense empty page renders');check(!page.html.includes('name="Version" value=""'),'New expense form does not submit an empty concurrency token');
 const expense={PlanId:1,Title:'Vé xe '+fixture.stamp,Category:'DiChuyen',Amount:250000,SpentOn:today,SubmissionId:val(page,'SubmissionId'),Notes:'Đã thanh toán trước chuyến đi'};
 let invalid=await status(a.post('/Expenses/Save',page,{...expense,Amount:-1}),400,'Negative expense rejected');check(val(invalid,'Title')===expense.Title&&invalid.text.includes(expense.Notes),'Expense validation retains input');
 await status(a.post('/Expenses/Save',page,{...expense,SpentOn:future}),422,'Future date cannot be recorded as actual expense');
 await status(a.post('/Expenses/Save',page,{...expense,Category:'Unknown'}),400,'Unknown expense category rejected');await status(a.post('/Expenses/Save',page,{...expense,Amount:'1.001'}),422,'More than two decimal places rejected');
 await status(a.post('/Expenses/Save',page,{...expense,Amount:'1,25'}),400,'Comma cannot silently become a different amount');
 await status(a.post('/Expenses/Save',page,{...expense,Amount:'1,000.25'}),400,'Thousands separators rejected by the expense API');
 await status(a.post('/Expenses/Save',page,expense),302,'Record actual expense');await status(a.post('/Expenses/Save',page,expense),302,'Repeat same expense submission');
 const costs=sql(`SELECT * FROM KhoanChiChuyenDi WHERE MaLuotGhi='${expense.SubmissionId}'`);check(costs.length===1&&costs[0].SoTien===250000,'Duplicate submission charges once');
 const cost=fixture.cost=costs[0].MaKhoanChi;item=row('KhoanChiChuyenDi','MaKhoanChi',cost);
 await status(a.post('/Expenses/Save',page,{...expense,Id:cost,Version:item.Version,Amount:300000.25}),302,'Edit actual expense');
 await status(a.post('/Expenses/Save',page,{...expense,Id:cost,Version:item.Version,Amount:400000}),409,'Stale expense update rejected');
 await status(b.post('/Expenses/Delete',btoken,{planId:1,Id:cost,Version:row('KhoanChiChuyenDi','MaKhoanChi',cost).Version}),404,'Cannot delete foreign expense');
 const disposable={...expense,Title:'Khoản nhập nhầm '+fixture.stamp,SubmissionId:require('node:crypto').randomUUID(),Amount:'1.25'};await status(a.post('/Expenses/Save',token,disposable),302,'Record small fractional amount');const disposal=sql(`SELECT MaKhoanChi,SoTien FROM KhoanChiChuyenDi WHERE MaLuotGhi='${disposable.SubmissionId}'`)[0];check(disposal.SoTien===1.25,'Decimal point is parsed independently of Vietnamese display culture');await status(a.post('/Expenses/Delete',token,{planId:1,Id:disposal.MaKhoanChi,Version:row('KhoanChiChuyenDi','MaKhoanChi',disposal.MaKhoanChi).Version}),302,'Owner can remove mistaken expense');check(sql(`SELECT MaKhoanChi FROM KhoanChiChuyenDi WHERE MaKhoanChi=${disposal.MaKhoanChi}`).length===0,'Deleted expense no longer contributes to totals');
 page=await a.get('/Expenses?planId=1');check(page.text.includes('300.000,25'),'Expense page shows updated actual amount');
 const totalActual=sql('SELECT SUM(SoTien) AS Total FROM KhoanChiChuyenDi WHERE MaKeHoach=1')[0].Total; const expectedActual=Number(totalActual).toLocaleString('vi-VN',{minimumFractionDigits:Number.isInteger(Number(totalActual))?0:2,maximumFractionDigits:2}); page=await status(a.get('/Plans/Detail/1'),200,'Trip hub renders extended data');check(page.text.includes(expectedActual)&&page.text.includes('Chuẩn bị chuyến đi'),'Hub shows real preparation and actual expenses');
 const providerPage=await status(p.get('/Provider/Edit'),200,'Provider can enter service details');check(providerPage.html.includes('enctype="multipart/form-data"')&&providerPage.html.includes('name="Photos"'),'Provider form accepts photo uploads');
 const details={Name:'Trải nghiệm địa phương '+fixture.stamp,Type:'HoatDong',Region:'Đà Lạt',Price:120000,Description:'Mô tả để kiểm tra xét duyệt',Terms:'Xác nhận khả năng phục vụ khi phản hồi',Hours:'08:00–17:00',Operation:'GuiDang',Address:'15 Đường Hoa, Đà Lạt',PriceUnit:'người/lượt',CancellationPolicy:'Báo trước 24 giờ để đổi lịch'};
 await status(multipart(p,'/Provider/Edit',providerPage,details),302,'Submit service details and real photo for review');
 const service=fixture.service=sql(`SELECT MaDichVu FROM DichVuDuLich WHERE TenDichVu=N'${details.Name}'`)[0].MaDichVu;
 const revision=fixture.revision=sql(`SELECT * FROM BanCapNhatDichVu WHERE MaDichVu=${service} ORDER BY SoPhienBan DESC`)[0];
 check(revision.DiaChiPhucVu===details.Address&&revision.DonViGia===details.PriceUnit&&JSON.parse(revision.AnhJson).length===1,'Revision stores extended fields and uploaded photo');
 await status(guest.get('/Explore/Detail/'+service),404,'Pending service remains private');await status(guest.get(`/service-photos/${service}/0`),404,'Pending service photo is not public');
 await status(a.get(`/service-photos/review/${service}/0?revision=${revision.MaBanCapNhat}`),403,'Traveler cannot read review photos');
 await status(p.get(`/service-photos/review/${service}/0?revision=${revision.MaBanCapNhat}`),200,'Owner can review uploaded image');
 let review=await status(admin.get('/Admin/Review/'+revision.MaBanCapNhat),200,'Admin sees extended review');check(review.text.includes(details.Address)&&review.text.includes(details.PriceUnit),'Admin can compare submitted address and price unit');
 await status(admin.post('/quan-tri/xet-duyet-dich-vu/duyet',review,{Id:revision.MaBanCapNhat,Version:revision.SoPhienBan,Decision:'DaDuyet'}),302,'Approve exact service revision');
 page=await status(guest.get('/Explore/Detail/'+service),200,'Approved details are public');check(page.text.includes(details.Address)&&page.text.includes(details.PriceUnit)&&page.text.includes(details.CancellationPolicy)&&page.text.includes('Ảnh dịch vụ'),'Public detail shows approved information and image label');
 const photo=await fetch(base+`/service-photos/${service}/0`);check(photo.status===200&&photo.headers.get('content-type').includes('image/png'),'Approved photo served with image MIME');await photo.arrayBuffer();
 const values={...details,Id:service,Version:sql(`SELECT CONVERT(varbinary(8),RowVersion) AS Version FROM DichVuDuLich WHERE MaDichVu=${service}`)[0].Version,Address:'Địa chỉ mới chưa được duyệt',PhotoPaths:'["../private.pdf"]'};
 await status(multipart(p,'/Provider/Edit',providerPage,values,false),302,'Submit address-only revision');
 const next=sql(`SELECT * FROM BanCapNhatDichVu WHERE MaDichVu=${service} ORDER BY SoPhienBan DESC`)[0];check(next.AnhJson===revision.AnhJson,'Client cannot inject photo paths; existing images preserved');
 page=await guest.get('/Explore/Detail/'+service);check(page.text.includes(details.Address)&&!page.text.includes(values.Address),'Unapproved edit does not replace published address');
 check(sql('SELECT COUNT(*) AS Total FROM sys.tables WHERE is_ms_shipped=0')[0].Total===(companionExtension?46:journeyExtension?31:28),'Schema has only declared added tables');
 await status(guest.get(`/service-photos/${service}/99`),404,'Invalid image index rejected');await status(guest.get(`/service-photos/review/${service}/0?revision=${revision.MaBanCapNhat}`),302,'Anonymous review photo requires login');
 const outsider=new Client(),outsideEmail='provider-'+fixture.stamp+'@example.test',register=await outsider.get('/Account/Register');const form=new FormData();form.append('__RequestVerificationToken',val(register,'__RequestVerificationToken'));for(const[k,v]of Object.entries({Email:outsideEmail,Password:'HanhTrinh@2026',ConfirmPassword:'HanhTrinh@2026',Name:'Đối tác kiểm thử khác',Role:'NHA_CUNG_CAP',Organization:'Nhà cung cấp độc lập',Address:'Địa chỉ đăng ký thử',OrganizationType:'Dịch vụ du lịch',ContactName:'Người đại diện',ContactPhone:'0909988776',ContactEmail:outsideEmail,RegistrationDescription:'Hồ sơ thử quyền sở hữu',Representative:'Người đại diện'}))form.append(k,v);form.append('Documents',new Blob(['%PDF-1.4\n%%EOF'],{type:'application/pdf'}),'hoso.pdf');await status(outsider.req('/Account/Register',form),302,'Register second provider through public workflow');await outsider.login(outsideEmail);await status(outsider.get(`/service-photos/review/${service}/0?revision=${revision.MaBanCapNhat}`),404,'Provider cannot read another provider revision photos');
 const before=sql('SELECT COUNT(*) AS Total FROM KhoanChiChuyenDi')[0].Total;
 for(let i=0;i<2;i++)execFileSync('sqlcmd',['-S','(localdb)\\MSSQLLocalDB','-d',database,'-E','-C','-b','-i','src/HoTroDuLich.Web/Database/003_ChuanBi_ChiTieu_LuaChon.sql'],{encoding:'utf8'});
 check(sql('SELECT COUNT(*) AS Total FROM KhoanChiChuyenDi')[0].Total===before,'Product migration is repeatable without changing costs');
 save(true);console.log('PASS',observations.length,'product feature checks');
})().catch(error=>{save(false,error);console.error(error);process.exitCode=1});
