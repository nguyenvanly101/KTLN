const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {database,literal,sql,json,catalog,snapshot}=require('./branches-sql.cjs');
try{
 assert.equal(json('master',`SELECT DB_ID(${literal(database)}) id FOR JSON PATH;`)[0]?.id,undefined,'Use a new database; never overwrite a database');
 const backup=JSON.parse(fs.readFileSync('preview/workflow-backup.json','utf8').replace(/^\uFEFF/,''));assert.equal(backup.Sql.Verified,true);
 const directory=path.resolve('preview/branches-db',database);fs.mkdirSync(directory,{recursive:true});
 const files=json('master',"SELECT name,type FROM sys.master_files WHERE database_id=DB_ID(N'HoTroDuLich') FOR JSON PATH;");assert.equal(files.length,2);
 const moves=files.map(f=>{const destination=path.join(directory,f.type===0?'branches.mdf':'branches.ldf');assert.ok(destination.startsWith(directory+path.sep));assert.ok(!fs.existsSync(destination));return 'MOVE '+literal(f.name)+' TO '+literal(destination);});
 sql('master',`RESTORE DATABASE [${database}] FROM DISK=${literal(backup.Sql.Path)} WITH ${moves.join(',')},CHECKSUM;`);
 const tables=catalog(database),before=snapshot(database,tables);assert.equal(tables.length,58);
 const migration=fs.readFileSync('src/HoTroDuLich.Web/Database/007_CoSoDichVu_GoogleMaps.sql','utf8');sql(database,migration);sql(database,migration);
 const after=snapshot(database,tables);assert.deepEqual(after,before);assert.equal(catalog(database).length,64);sql(database,'DBCC CHECKDB WITH NO_INFOMSGS;');
 fs.writeFileSync('tests/branches-upgrade-results.json',JSON.stringify({passed:true,database,oldTables:58,newTables:6,idempotent:true,checkdb:true,before,after,backup:backup.Sql.Path,date:new Date().toISOString()},null,2));
 console.log('PASS upgrade: restored verified backup, applied 007 twice, 58 existing tables retain every row/hash.');
}catch(e){fs.writeFileSync('tests/branches-upgrade-results.json',JSON.stringify({passed:false,database,error:e.stack},null,2));console.error(e);process.exitCode=1;}
