const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),{createHash}=require('node:crypto');const{Client}=require('./fulltext-smoke.cjs');const{json,catalog}=require('./branches-sql.cjs');const checks=[];
function check(ok,label){assert.ok(ok,label);checks.push(label);}
(async()=>{
 const tables=catalog('HoTroDuLich');check(tables.length===64,'Main has 64 tables');check(tables.reduce((sum,t)=>sum+t.Columns.length,0)===699,'Main has 699 mapped columns');
 const schema=JSON.parse(fs.readFileSync('docs/branch-schema.json','utf8'));
 for(const name of Object.keys(schema)){const count=json('HoTroDuLich',`SELECT COUNT_BIG(*) n FROM [${name}] FOR JSON PATH;`)[0].n;check(count===0,name+' contains no QA/guessed production records');}
 const guest=new Client();const root=await guest.get('/');check(root.status===302&&root.location?.startsWith('/kham-pha'),'Root opens new exact-place search');
 for(const url of ['/kham-pha','/thong-tin/mo-tim-kiem','/thong-tin/tra-cuu?loai=AnUong','/dieu-khoan-su-dung','/chinh-sach-bao-mat','/css/places.css','/js/google-maps.js','/js/places.js']){const r=await guest.get(url);check(r.status===200,'Main responds '+url+' '+r.status);}
 const cfg=await guest.get('/api/maps/config');const settings=JSON.parse(cfg.html);check(!Object.keys(settings).some(x=>/server|secret/i.test(x)),'Main configuration does not expose server key');check(settings.mapsReady===false&&settings.placesReady===false&&settings.mapProvider==='OpenStreetMap','OpenStreetMap is available without Google keys');
 for(const url of ['/nha-cung-cap/co-so','/quan-tri/co-so','/quan-tri/moc-dia-diem']){const r=await guest.get(url);check(r.status===302,'Main protected route requires login '+url);}
 const first=json('HoTroDuLich',"SELECT TOP(1) d.MaDichVu FROM DichVuDuLich d JOIN NhaCungCapDichVu p ON p.MaNhaCungCap=d.MaNhaCungCap WHERE d.TrangThaiCongBo='DaDuyet' AND p.TrangThaiQuyenHoatDong<>'DaThuHoi' ORDER BY d.MaDichVu FOR JSON PATH;")[0];
 if(first){const r=await guest.get('/Explore/Detail/'+first.MaDichVu);check(r.status===200,'Pre-existing service remains readable');const canonical=await guest.get('/thong-tin/xem-chi-tiet?loai=DichVu&maMuc='+first.MaDichVu);check(canonical.status===200,'Canonical pre-existing service detail remains readable');}
 const backup=JSON.parse(fs.readFileSync('preview/workflow-backup.json','utf8').replace(/^\uFEFF/,''));
 for(const item of backup.PrivateFiles.Files){const file=path.resolve('src/HoTroDuLich.Web/App_Data',item.Path);check(createHash('sha256').update(fs.readFileSync(file)).digest('hex').toUpperCase()===item.Sha256,'Private file unchanged '+item.Path);}
 fs.writeFileSync('tests/branches-main-results.json',JSON.stringify({passed:true,checks:checks.length,observations:checks,database:'HoTroDuLich',liveGoogle:false,date:new Date().toISOString()},null,2));console.log('PASS '+checks.length+' main/read-only/data-file checks.');
})().catch(e=>{fs.writeFileSync('tests/branches-main-results.json',JSON.stringify({passed:false,error:e.stack,checks:checks.length,observations:checks},null,2));console.error(e);process.exitCode=1;});
