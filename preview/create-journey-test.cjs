const fs=require('node:fs'),{execFileSync}=require('node:child_process'),assert=require('node:assert/strict'),{sql,json,catalog,snapshot}=require('../tests/journey-sql.cjs');
const database='HoTroDuLich_Journey_20261105',backup=fs.readFileSync('preview/journey-backup-path.txt','utf8').trim().replace(/^\uFEFF/,''),literal=backup.replaceAll("'","''");
assert.equal(json('master',`SELECT DB_ID(N'${database}') AS id FOR JSON PATH;`)[0].id,undefined,'Test restore never overwrites an existing database');
const raw=execFileSync('sqlcmd',['-S','(localdb)\\MSSQLLocalDB','-d','master','-E','-C','-b','-W','-h','-1','-s','|','-Q',`RESTORE FILELISTONLY FROM DISK=N'${literal}';`],{encoding:'utf8'});
const files=raw.split(/\r?\n/).map(x=>x.trim().split('|')).filter(x=>['D','L'].includes(x[2]));assert.equal(files.length,2);fs.mkdirSync('preview/sql-data',{recursive:true});
sql('master',`RESTORE DATABASE [${database}] FROM DISK=N'${literal}' WITH ${files.map(x=>`MOVE N'${x[0].replaceAll("'","''")}' TO N'D:\\KLTN\\preview\\sql-data\\${database}${x[2]==='L'?'_log.ldf':'.mdf'}'`).join(',')},CHECKSUM,RECOVERY;`);
const tables=catalog(database);assert.equal(tables.length,28);const before=snapshot(database,tables);
fs.writeFileSync('preview/journey-main-values-before.json',JSON.stringify({database:'HoTroDuLich',backup,catalog:tables,rows:before,date:new Date().toISOString()},null,2));
sql(database,'SET QUOTED_IDENTIFIER OFF;\n'+fs.readFileSync('src/HoTroDuLich.Web/Database/004_HanhTrinh_ChiaSe_DanhGia_LichPhucVu.sql','utf8'));
const after=snapshot(database,tables);for(const old of before){const row=after.find(x=>x.name===old.name);assert.equal(row.rows,old.rows);assert.equal(row.sha256,old.sha256);}
const totals=json(database,'SELECT (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0) AS tables,(SELECT COUNT(*) FROM sys.columns c JOIN sys.tables t ON c.object_id=t.object_id WHERE t.is_ms_shipped=0) AS columns FOR JSON PATH;')[0];assert.equal(totals.tables,31);assert.equal(totals.columns,335);
fs.writeFileSync('tests/journey-rehearsal-results.json',JSON.stringify({passed:true,checks:58,database,backup,before,after,totals,date:new Date().toISOString()},null,2));console.log('PASS: restored main backup, original 28 tables preserved, 31 tables/335 columns after fresh migration.');
