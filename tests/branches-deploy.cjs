// Explicit deployment of the reviewed additive schema; no fixture data or database restore here.
const fs=require('node:fs'),assert=require('node:assert/strict'),{createHash}=require('node:crypto');
const {sql,json,catalog,snapshot}=require('./branches-sql.cjs');
try{
 for(const file of ['tests/branches-features-results.json','tests/branches-edge-results.json','tests/branches-contract-results.json','tests/branches-legacy-contract-results.json'])assert.equal(JSON.parse(fs.readFileSync(file,'utf8')).passed,true,file);
 const backup=JSON.parse(fs.readFileSync('preview/workflow-backup.json','utf8').replace(/^\uFEFF/,''));assert.equal(backup.Sql.Verified,true);assert.equal(backup.Source.Verified,true);assert.equal(backup.PrivateFiles.Verified,true);
 assert.ok(Date.now()-new Date(backup.Date).getTime()<30*60*1000,'Use a verified backup less than 30 minutes old');
 const tables=catalog('HoTroDuLich');assert.equal(tables.length,58,'Deploy expects the pre-007 database; never replay this deployment after completion');
 const before=snapshot('HoTroDuLich',tables);fs.writeFileSync('preview/branches-main-before.json',JSON.stringify({tables,before},null,2));
 const script=fs.readFileSync('src/HoTroDuLich.Web/Database/007_CoSoDichVu_GoogleMaps.sql','utf8');sql('HoTroDuLich',script);const after=snapshot('HoTroDuLich',tables);assert.deepEqual(after,before,'All pre-existing rows and counts must be retained');
 const current=catalog('HoTroDuLich');assert.equal(current.length,64);sql('HoTroDuLich','DBCC CHECKDB WITH NO_INFOMSGS;');
 const schema=json('HoTroDuLich',"SELECT COUNT(*) tables,(SELECT COUNT(*) FROM sys.columns c JOIN sys.tables s ON s.object_id=c.object_id WHERE s.is_ms_shipped=0) columns FROM sys.tables WHERE is_ms_shipped=0 FOR JSON PATH;")[0];
 fs.writeFileSync('tests/branches-deploy-results.json',JSON.stringify({passed:true,backup:backup.Sql.Path,scriptSha256:createHash('sha256').update(script).digest('hex'),before,after,schema,checkdb:true,date:new Date().toISOString()},null,2));
 console.log(`PASS main deployment: ${schema.tables} tables/${schema.columns} columns; every row/hash in 58 previous tables retained.`);
}catch(e){fs.writeFileSync('tests/branches-deploy-results.json',JSON.stringify({passed:false,error:e.stack,date:new Date().toISOString()},null,2));console.error(e);process.exitCode=1;}
