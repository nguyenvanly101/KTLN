// One-time local deployment, not a test fixture runner. Requires an explicit --apply.
// Close the main web server before running. This never drops/restores the main database.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {sql,json,catalog,snapshot}=require('../tests/journey-sql.cjs');
assert.ok(process.argv.includes('--apply'),'Use node tools/UpgradeJourney.cjs --apply only after backup/rehearsal checks');
const database='HoTroDuLich',observations=[],output='tests/journey-upgrade-results.json';
const check=(ok,name)=>{assert.ok(ok,name);observations.push({name,passed:true});};
let backup,before,after;
try{
 const columns=catalog(database);
 check(columns.length===28&&columns.reduce((n,t)=>n+t.Columns.length,0)===302,'Expected existing product schema: 28 tables and 302 columns');
 const filesBackup=fs.readFileSync('preview/journey-deploy-appdata-backup-path.txt','utf8').replace(/^\uFEFF/,'').trim();
 check(fs.statSync(filesBackup).size>0,'Fresh App_Data archive exists');
 before={catalog:columns,rows:snapshot(database,columns),date:new Date().toISOString()};
 fs.writeFileSync('preview/journey-main-values-before-deploy.json',JSON.stringify(before,null,2));
 const stamp=new Date().toISOString().replace(/[-:]/g,'').replace(/\.\d+Z$/,'Z').replace('T','_');
 backup=path.resolve('preview/backups/HoTroDuLich_before_journey_deploy_'+stamp+'.bak');
 const literal=backup.replaceAll("'","''");
 sql(database,`BACKUP DATABASE [HoTroDuLich] TO DISK=N'${literal}' WITH COPY_ONLY,CHECKSUM; RESTORE VERIFYONLY FROM DISK=N'${literal}' WITH CHECKSUM;`);
 check(fs.statSync(backup).size>0,'COPY_ONLY CHECKSUM backup created and RESTORE VERIFYONLY succeeded');
 fs.writeFileSync('preview/journey-deploy-backup-path.txt',backup);
 const migration=fs.readFileSync('src/HoTroDuLich.Web/Database/004_HanhTrinh_ChiaSe_DanhGia_LichPhucVu.sql','utf8');
 sql(database,migration);check(true,'Migration 004 applied to main database');
 sql(database,migration);check(true,'Migration 004 rerun is idempotent');
 sql(database,fs.readFileSync('src/HoTroDuLich.Web/Database/Quyen_CSDL.sql','utf8'));check(true,'Application SQL role updated with declared calendar permission');
 after=snapshot(database,columns);
 for(const row of after){const old=before.rows.find(x=>x.name===row.name);check(row.rows===old.rows,row.name+': original row count preserved');check(row.sha256===old.sha256,row.name+': original column values preserved');}
 const finalCatalog=catalog(database);
 check(finalCatalog.length===31&&finalCatalog.reduce((n,t)=>n+t.Columns.length,0)===335,'Final schema has 31 tables and 335 columns');
 for(const table of ['ChiaSeChuyenDi','DanhGiaDichVu','LichPhucVuDichVu'])check(json(database,`SELECT COUNT(*) AS n FROM dbo.[${table}] FOR JSON PATH;`)[0].n===0,table+': no test/demo records copied to main');
 check(json(database,'SELECT COUNT(*) AS n FROM dbo.HoatDongLichTrinh WHERE ThoiLuongPhut IS NOT NULL OR DiChuyenTruocPhut IS NOT NULL OR HoanThanhLuc IS NOT NULL FOR JSON PATH;')[0].n===0,'Original activities have no fabricated durations or completion timestamps');
 fs.writeFileSync(output,JSON.stringify({passed:true,checks:observations.length,observations,database,backup,filesBackup,before:before.rows,after,date:new Date().toISOString()},null,2));
 console.log('PASS',observations.length,'main upgrade, backup and original data preservation checks');
 console.log('Backup:',backup);
}catch(error){fs.writeFileSync(output,JSON.stringify({passed:false,checks:observations.length,observations,database,backup,before:before?.rows,after,error:error.stack,date:new Date().toISOString()},null,2));console.error(error);process.exitCode=1;}
