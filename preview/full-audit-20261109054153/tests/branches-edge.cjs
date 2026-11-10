const fs=require('node:fs'),assert=require('node:assert/strict');
const {Client,val}=require('./fulltext-smoke.cjs');const {database,sql,json,literal}=require('./branches-sql.cjs');
const fixture=JSON.parse(fs.readFileSync('preview/branches-fixture.json','utf8')),core=JSON.parse(fs.readFileSync('tests/branches-features-results.json','utf8'));
assert.equal(core.passed,true);assert.equal(fixture.database,database);const c=core.context,checks=[];
function check(ok,label){assert.ok(ok,label);checks.push(label);}
const row=(t,id,key='Id')=>json(database,`SELECT * FROM [${t}] WHERE [${key}]=${Number(id)} FOR JSON PATH;`)[0];
async function status(promise,expected,label){const r=await promise;check(r.status===expected,label+' ['+r.status+'] '+(r.status!==expected?r.text.replace(/<[^>]+>/g,' ').replace(/\s+/g,' ').slice(0,900):''));return r;}
async function post(client,path,page,data){const body=new URLSearchParams({__RequestVerificationToken:val(page,'__RequestVerificationToken')});for(const[k,v]of Object.entries(data))if(v!=null)body.append(k,String(v));return client.req(path,body);}
function expectSqlError(command,error,label){const result=sql(database,`SET XACT_ABORT ON; BEGIN TRY BEGIN TRANSACTION; ${command}; ROLLBACK; PRINT 'UNEXPECTED_SUCCESS'; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; IF ERROR_NUMBER()<>${error} THROW; PRINT 'EXPECTED_ERROR'; END CATCH;`);check(result.includes('EXPECTED_ERROR')&&!result.includes('UNEXPECTED_SUCCESS'),label);}
(async()=>{
 const base=process.env.FULLTEXT_TEST_BASE;for(let n=0;;n++){try{if((await fetch(base+'/api/maps/config')).ok)break;}catch{}if(n>=45)throw Error('Test server not ready');await new Promise(r=>setTimeout(r,1000));}
 const guest=new Client(),owner=new Client(),admin=new Client(),other=new Client();
 await owner.login(fixture.users.find(x=>x.label==='provider').email,'BranchTest@2026');await admin.login(fixture.users.find(x=>x.label==='admin').email,'BranchTest@2026');
 await other.login(fixture.users.find(x=>x.label==='other').email,'BranchTest@2026');
 const around=await status(guest.get('/kham-pha?AnchorKind=Branch&AnchorId='+c.a+'&Category=AnUong&IncludeGoogle=false'),200,'Search around a business');
 const payload=JSON.parse(around.html.match(/data-places-payload>([\s\S]*?)<\/script>/)[1]);check(!payload.hits.some(x=>x.key==='b:'+c.a),'Anchor branch is not repeated as its own nearby alternative');
 const entrance=row('MocDiaDiem',c.entrance);const term=entrance.Name.normalize('NFD').replace(/\p{M}/gu,'').replaceAll('Đ','D').replaceAll('đ','d');
 const suggestions=await status(guest.get('/api/maps/suggest?q='+encodeURIComponent(term)),200,'Search Vietnamese without accents');check(JSON.parse(suggestions.html).items.some(x=>x.localId===c.entrance),'All Vietnamese vowel marks and Đ normalize correctly');
 await status(guest.get('/api/maps/place?id=..%2Fbad'),400,'Place ID cannot escape REST path');
 await status(guest.get('/kham-pha?AnchorKind=Unknown'),400,'Unknown anchor kind rejected');
 await status(guest.get('/kham-pha?AnchorKind=Pin&Lat=16.5'),400,'Incomplete anchor coordinate rejected');
 await status(guest.get('/kham-pha?AnchorKind=Pin&Lat=95&Lng=107'),400,'Latitude boundary enforced');
 await status(guest.get('/kham-pha/so-sanh?ids='+c.a+'&ids='+c.a),400,'Duplicate comparison branch rejected');
 const b=row('CoSoDichVu',c.a),edit=await owner.get('/nha-cung-cap/co-so/'+c.a);
 const input={Id:b.Id,Version:b.RowVersion,Name:b.Name,Address:b.Address,Region:b.Region,Type:b.Type,LocationSource:'Provider',Latitude:b.Latitude,Longitude:b.Longitude,ConfirmLocation:true,OwnershipEvidence:'Nguồn thử',Operation:'Submit'};
 const before=pendingCount();
 await status(post(owner,'/nha-cung-cap/co-so/luu',edit,{...input,Version:'AAAAAAAAAAA='}),409,'Stale branch update rejected');check(pendingCount()===before,'Stale branch save creates no revision');
 await status(post(owner,'/nha-cung-cap/co-so/luu',edit,{...input,Website:'javascript:alert(1)'}),422,'Unsafe provider website rejected');
 await status(post(owner,'/nha-cung-cap/co-so/luu',edit,{...input,Parking:false,VeganOnly:true,VegetarianOptions:false}),422,'Contradictory vegan declarations rejected');
 const claim='ChIJ_DraftClaim_'+Date.now();const newOwner=await owner.get('/nha-cung-cap/co-so/them');
 const draft={Id:0,Name:'QA bản nháp Google '+Date.now(),Address:'Địa chỉ bản nháp thử',Region:'Khu vực thử',Type:'AnUong',LocationSource:'Google',GooglePlaceId:claim,Operation:'Draft'};
 await status(post(owner,'/nha-cung-cap/co-so/luu',newOwner,draft),302,'Unpublished Google draft can be saved without fabricating coordinates');
 const newOther=await other.get('/nha-cung-cap/co-so/them');await status(post(other,'/nha-cung-cap/co-so/luu',newOther,{...draft,Name:'QA hồ sơ yêu cầu khác '+Date.now()}),302,'An unreviewed draft cannot reserve a place against another provider');
 const draftRow=json(database,`SELECT * FROM CoSoDichVu WHERE GooglePlaceId=${literal(claim)} FOR JSON PATH;`);check(draftRow.length===2&&draftRow.every(x=>x.PublishedVersion==null&&x.Latitude==null),'Draft Google claims remain private and have no invented coordinates');
 const lm=await admin.get('/quan-tri/moc-dia-diem/'+c.parent),p=row('MocDiaDiem',c.parent);
 const landmark={Id:p.Id,Version:p.RowVersion,Name:p.Name,Address:p.Address,Region:p.Region,Type:'Point',ParentId:c.entrance,SourceName:p.SourceName,CheckedOn:p.CheckedOn,IsPublished:true,LocationSource:'Provider',Latitude:p.Latitude,Longitude:p.Longitude,ConfirmLocation:true};
 await status(post(admin,'/quan-tri/moc-dia-diem/luu',lm,landmark),422,'Landmark hierarchy cannot form a cycle or reverse entrance ownership');
 expectSqlError(`INSERT DichVuCoSo(ServiceId,BranchId) VALUES(${c.service},${c.a2})`,2627,'Service/branch association unique key');
 expectSqlError(`INSERT DichVuCoSo(ServiceId,BranchId) VALUES(${c.service},9223372036854775806)`,547,'Association branch foreign key');
 expectSqlError(`UPDATE CoSoDichVu SET Latitude=NULL,Longitude=107 WHERE Id=${c.a}`,547,'SQL rejects a half coordinate');
 expectSqlError(`UPDATE CoSoDichVu SET Latitude=91 WHERE Id=${c.a}`,547,'SQL rejects out-of-range latitude');
 expectSqlError(`UPDATE BanCapNhatCoSo SET Status='Unexpected' WHERE Id=(SELECT MIN(Id) FROM BanCapNhatCoSo)`,547,'SQL rejects unknown review status');
 const collations=json(database,"SELECT OBJECT_NAME(object_id) tableName,collation_name FROM sys.columns WHERE name=N'GooglePlaceId' AND OBJECT_NAME(object_id) IN (N'CoSoDichVu',N'BanCapNhatCoSo',N'MocDiaDiem',N'YeuCauDatCoSo') FOR JSON PATH;");check(collations.length===4&&collations.every(x=>x.collation_name==='Latin1_General_100_BIN2'),'Google Place IDs use case-sensitive binary collation');
 const keyTest=sql(database,`BEGIN TRANSACTION; UPDATE CoSoDichVu SET GooglePlaceId=N'ChIJ_CaseTest' WHERE Id=${c.a}; UPDATE CoSoDichVu SET GooglePlaceId=N'ChIJ_casetest' WHERE Id=${c.a2}; SELECT COUNT(*) FROM CoSoDichVu WHERE GooglePlaceId=N'ChIJ_CaseTest'; ROLLBACK;`);check(/\b1\b/.test(keyTest),'Different-case Google IDs remain distinct');
 expectSqlError(`UPDATE CoSoDichVu SET GooglePlaceId=N'ChIJ_PublishedUnique' WHERE Id IN (${c.a},${c.a2})`,2601,'Two published branches cannot claim the same Google place');
 for(const path of ['/dieu-khoan-su-dung','/chinh-sach-bao-mat','/Home/Privacy']){const r=await status(guest.get(path),200,'Public information '+path);check((r.html.match(/<h1(?:\s|>)/g)||[]).length===1,'Single page heading '+path);}
 const request=row('YeuCauDatCoSo',c.booking,'RequestId');check(request.BranchId===c.a&&request.Address!==row('CoSoDichVu',c.a).Address,'Booking address snapshot remains independent of current branch data');
 function pendingCount(){return json(database,`SELECT COUNT(*) n FROM BanCapNhatCoSo WHERE BranchId=${c.a} FOR JSON PATH;`)[0].n;}
 fs.writeFileSync('tests/branches-edge-results.json',JSON.stringify({passed:true,checks:checks.length,observations:checks,database,liveGoogle:false,date:new Date().toISOString()},null,2));console.log('PASS '+checks.length+' boundary/SQL/privacy checks.');
})().catch(e=>{fs.writeFileSync('tests/branches-edge-results.json',JSON.stringify({passed:false,checks:checks.length,observations:checks,error:e.stack,database},null,2));console.error(e);process.exitCode=1;});
