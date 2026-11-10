// Additive deployment only. The main database is never restored or replaced.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),{execFileSync}=require('node:child_process');
const {sql,json,catalog,snapshot}=require('./companion-sql.cjs');
const target='HoTroDuLich_CompanionUpgrade_20261105',main='HoTroDuLich',observations=[];
const result='tests/companion-upgrade-results.json',planFile='preview/companion-upgrade-plan.json';
const migration=()=>fs.readFileSync('src/HoTroDuLich.Web/Database/005_KhongGianDongHanh.sql','utf8');
const permissions=()=>fs.readFileSync('src/HoTroDuLich.Web/Database/Quyen_CSDL.sql','utf8');
function check(ok,name){assert.ok(ok,name);observations.push({name,passed:true});}
function compare(database,original,before){for(const row of snapshot(database,original)){const old=before.find(x=>x.name===row.name);check(row.rows===old.rows&&row.sha256===old.sha256,database+': '+row.name+' original values preserved');}}
let plan;
try{
 if(process.argv.includes('--rehearse')){
  check(json('master',`SELECT DB_ID(N'${target}') AS id FOR JSON PATH;`)[0].id==null,'Fresh rehearsal target is absent');
  const original=catalog(main);check(original.length===31,'Main has the expected 31-table schema before 005');
  const before=snapshot(main,original),stamp=new Date().toISOString().replace(/[-:]/g,'').replace(/\.\d+Z$/,'Z').replace('T','_');
  const backup=path.resolve('preview/backups/HoTroDuLich_before_companion_deploy_'+stamp+'.bak'),literal=backup.replaceAll("'","''");
  sql(main,`BACKUP DATABASE [${main}] TO DISK=N'${literal}' WITH COPY_ONLY,CHECKSUM;RESTORE VERIFYONLY FROM DISK=N'${literal}' WITH CHECKSUM;`);check(true,'Fresh main COPY_ONLY/CHECKSUM backup passes VERIFYONLY');
  execFileSync('pwsh.exe',['-NoProfile','-File',path.resolve('tools/BackupCompanionAppData.ps1')],{encoding:'utf8',stdio:['ignore','pipe','pipe']});
  const appData=JSON.parse(fs.readFileSync('tests/companion-appdata-backup-results.json','utf8').replace(/^\uFEFF/,''));check(appData.passed,'Fresh App_Data archive hashes verified');
  const raw=execFileSync('sqlcmd',['-S','(localdb)\\MSSQLLocalDB','-d','master','-E','-C','-b','-l','15','-W','-h','-1','-s','|','-Q',`RESTORE FILELISTONLY FROM DISK=N'${literal}';`],{encoding:'utf8'});
  const files=raw.split(/\r?\n/).map(x=>x.trim().split('|')).filter(x=>['D','L'].includes(x[2]));check(files.length===2,'Backup data/log file list verified');
  const moves=files.map(x=>`MOVE N'${x[0].replaceAll("'","''")}' TO N'${path.resolve('preview/sql-data/'+target+(x[2]==='L'?'_log.ldf':'.mdf')).replaceAll("'","''")}'`).join(',');
  for(const file of [target+'.mdf',target+'_log.ldf'])check(!fs.existsSync(path.resolve('preview/sql-data',file)),'Separate rehearsal file path is unused: '+file);
  sql('master',`RESTORE DATABASE [${target}] FROM DISK=N'${literal}' WITH ${moves},CHECKSUM,RECOVERY;`);check(true,'Restored backup only into separate rehearsal database');compare(target,original,before);
  sql(target,migration());sql(target,migration());sql(target,permissions());check(true,'Final 005 script runs and reruns with permission script');compare(target,original,before);
  const final=catalog(target);check(final.length===46&&final.reduce((n,x)=>n+x.Columns.length,0)===473,'Rehearsal has 46 tables and 473 columns');
  for(const row of snapshot(target,final.filter(x=>!original.some(o=>o.name===x.name))))check(row.rows===0,'Rehearsal new table empty: '+row.name);
  plan={backup,appDataBackup:appData.backup,original,before,target,rehearsalPassed:true,rehearsalDate:new Date().toISOString()};fs.writeFileSync(planFile,JSON.stringify(plan,null,2));
 }else if(process.argv.includes('--apply')){
  plan=JSON.parse(fs.readFileSync(planFile,'utf8'));check(plan.rehearsalPassed===true,'Fresh deployment rehearsal passed');check(Date.now()-Date.parse(plan.rehearsalDate)<30*60000,'Backup/rehearsal is less than 30 minutes old');
  check(catalog(main).length===31,'Main still has the expected pre-upgrade schema');compare(main,plan.original,plan.before);
  sql(main,migration());sql(main,permissions());check(true,'005 and application permissions applied to main');compare(main,plan.original,plan.before);
  const final=catalog(main);check(final.length===46&&final.reduce((n,x)=>n+x.Columns.length,0)===473,'Main has 46 tables and 473 columns');
  for(const row of snapshot(main,final.filter(x=>!plan.original.some(o=>o.name===x.name))))check(row.rows===0,'Main new table empty, no test fixtures: '+row.name);
  plan.applied=true;plan.appliedDate=new Date().toISOString();fs.writeFileSync(planFile,JSON.stringify(plan,null,2));
 }else throw new Error('Use --rehearse or --apply');
 const prior=process.argv.includes('--apply')?JSON.parse(fs.readFileSync(result,'utf8')):{observations:[]};
 const combined=[...prior.observations,...observations];fs.writeFileSync(result,JSON.stringify({passed:true,applied:!!plan.applied,checks:combined.length,observations:combined,backup:plan.backup,appDataBackup:plan.appDataBackup,database:main,target,date:new Date().toISOString()},null,2));console.log('PASS',observations.length,'upgrade checks; main applied:',!!plan.applied);
}catch(error){fs.writeFileSync(result,JSON.stringify({passed:false,checks:observations.length,observations,error:error.stack,date:new Date().toISOString()},null,2));console.error(error);process.exitCode=1;}
