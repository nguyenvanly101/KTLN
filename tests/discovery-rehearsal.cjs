const fs=require('fs'),path=require('path'),assert=require('node:assert/strict'),{sql,json,catalog,snapshot}=require('./discovery-sql.cjs');const db='HoTroDuLich_DiscoveryUpgrade_20261108';
const literal=s=>"N'"+String(s).replaceAll("'","''")+"'";
try{
 assert.equal(json('master',`SELECT DB_ID('${db}') AS id FOR JSON PATH`)[0]?.id,undefined,'Rehearsal requires a new database; never overwrite an existing one');
 const backup=JSON.parse(fs.readFileSync('preview/workflow-backup.json','utf8').replace(/^\uFEFF/,''));assert.ok(backup.Sql.Verified);const dir=path.resolve('preview/discovery-upgrade');fs.mkdirSync(dir,{recursive:true});const files=json('master',"SELECT name,type FROM sys.master_files WHERE database_id=DB_ID(N'HoTroDuLich') FOR JSON PATH");assert.equal(files.length,2);const moves=files.map(f=>{const dest=path.resolve(dir,f.type===0?'data.mdf':'log.ldf');assert.ok(dest.startsWith(dir+path.sep));assert.ok(!fs.existsSync(dest),'Never overwrite an existing restore file');return 'MOVE '+literal(f.name)+' TO '+literal(dest);});
 sql('master',`RESTORE DATABASE [${db}] FROM DISK=${literal(backup.Sql.Path)} WITH ${moves.join(',')},CHECKSUM;`);
 const before=catalog(db);assert.equal(before.length,46);const rowsBefore=snapshot(db,before);
 const source=fs.readFileSync('src/HoTroDuLich.Web/Database/006_TraCuuThongTin.sql','utf8');sql(db,source);sql(db,source);const after=snapshot(db,before);assert.deepEqual(after,rowsBefore,'Original 46 tables must retain every count/hash');assert.equal(catalog(db).length,58);
 sql(db,'DBCC CHECKDB WITH NO_INFOMSGS;');
 fs.writeFileSync('tests/discovery-rehearsal-results.json',JSON.stringify({passed:true,checks:51,database:db,backup:backup.Sql.Path,before:rowsBefore,after,migrationRepeated:true,checkdb:true,date:new Date().toISOString()},null,2));console.log('PASS rehearsal: additive upgrade, repeat-safe, 46 old table hashes unchanged');
}catch(error){fs.writeFileSync('tests/discovery-rehearsal-results.json',JSON.stringify({passed:false,error:error.stack},null,2));console.error(error);process.exitCode=1;}
