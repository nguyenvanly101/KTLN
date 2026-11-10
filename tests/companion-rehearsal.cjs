// Creates a new isolated database; never replaces/restores/drops the main database.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),{execFileSync}=require('node:child_process');
const {sql,json,catalog,snapshot}=require('./companion-sql.cjs');
const database='HoTroDuLich_Companion_20261105',observations=[];
function check(ok,name){assert.ok(ok,name);observations.push({name,passed:true});}
(async()=>{let backup;try{
 check(json('master',`SELECT DB_ID(N'${database}') AS id FOR JSON PATH;`)[0].id==null,'Isolated restore target does not exist');
 const original=catalog('HoTroDuLich');check(original.length===31,'Expected existing journey schema');const before=snapshot('HoTroDuLich',original);
 fs.writeFileSync('preview/companion-main-before-rehearsal.json',JSON.stringify({catalog:original,rows:before},null,2));
 const stamp=new Date().toISOString().replace(/[-:]/g,'').replace(/\.\d+Z$/,'Z').replace('T','_');backup=path.resolve('preview/backups/HoTroDuLich_before_companion_'+stamp+'.bak');
 const literal=backup.replaceAll("'","''");sql('HoTroDuLich',`BACKUP DATABASE [HoTroDuLich] TO DISK=N'${literal}' WITH COPY_ONLY,CHECKSUM; RESTORE VERIFYONLY FROM DISK=N'${literal}' WITH CHECKSUM;`);check(true,'Main COPY_ONLY CHECKSUM backup verified');fs.writeFileSync('preview/companion-backup-path.txt',backup);
 const raw=execFileSync('sqlcmd',['-S','(localdb)\\MSSQLLocalDB','-d','master','-E','-C','-b','-W','-h','-1','-s','|','-Q',`RESTORE FILELISTONLY FROM DISK=N'${literal}';`],{encoding:'utf8'});
 const files=raw.split(/\r?\n/).map(x=>x.trim().split('|')).filter(x=>['D','L'].includes(x[2]));check(files.length===2,'Backup contains data and log files');fs.mkdirSync('preview/sql-data',{recursive:true});
 const moves=files.map(x=>`MOVE N'${x[0].replaceAll("'","''")}' TO N'${path.resolve('preview/sql-data/'+database+(x[2]==='L'?'_log.ldf':'.mdf')).replaceAll("'","''")}'`).join(',');
 sql('master',`RESTORE DATABASE [${database}] FROM DISK=N'${literal}' WITH ${moves},CHECKSUM,RECOVERY;`);check(true,'Backup restored into separate data/log paths');
 for(const row of snapshot(database,original)){const old=before.find(x=>x.name===row.name);check(row.rows===old.rows&&row.sha256===old.sha256,row.name+': restored values match main');}
 const migration=fs.readFileSync('src/HoTroDuLich.Web/Database/005_KhongGianDongHanh.sql','utf8');sql(database,'SET QUOTED_IDENTIFIER OFF;\n'+migration);check(true,'Fresh 005 migration owns required SET options');sql(database,migration);check(true,'005 rerun succeeds');
 for(const row of snapshot(database,original)){const old=before.find(x=>x.name===row.name);check(row.rows===old.rows&&row.sha256===old.sha256,row.name+': original values preserved after both runs');}
 const final=catalog(database);check(final.length===46&&final.reduce((n,x)=>n+x.Columns.length,0)===473,'46 tables and 473 columns');
 fs.writeFileSync('tests/companion-rehearsal-results.json',JSON.stringify({passed:true,checks:observations.length,observations,database,backup,date:new Date().toISOString()},null,2));console.log('PASS',observations.length,'backup, restore and migration checks');
 }catch(e){fs.writeFileSync('tests/companion-rehearsal-results.json',JSON.stringify({passed:false,checks:observations.length,observations,database,backup,error:e.stack,date:new Date().toISOString()},null,2));throw e;}})().catch(e=>{console.error(e);process.exitCode=1;});
