// Restore the verified main backup to a new isolated database; never overwrite an existing DB.
const fs=require('node:fs'),assert=require('node:assert/strict'),{execFileSync}=require('node:child_process');
const database='HoTroDuLich_ProductUpgrade_20261105',observations=[],output='tests/product-upgrade-rehearsal-results.json';
const check=(value,name)=>{assert.ok(value,name);observations.push({name,passed:true});};
function sql(query,db=database){fs.writeFileSync('preview/product-rehearsal-query.sql',query);return execFileSync('sqlcmd',['-S','(localdb)\\MSSQLLocalDB','-d',db,'-E','-C','-b','-t','120','-y','0','-w','65535','-f','65001','-i','preview/product-rehearsal-query.sql'],{encoding:'utf8',maxBuffer:16*1024*1024});}
function json(query,db=database){const raw=sql('SET NOCOUNT ON; '+query,db);return JSON.parse(raw.slice(raw.indexOf('['),raw.lastIndexOf(']')+1).replace(/[\r\n]/g,''));}
function snapshot(catalog){return json('SELECT * FROM ('+catalog.map(t=>`SELECT N'${t.name}' AS name,(SELECT COUNT_BIG(*) FROM dbo.[${t.name}]) AS rows,CONVERT(varchar(64),HASHBYTES('SHA2_256',(SELECT ${t.Columns.map(c=>'['+c.name+']').join(',')} FROM dbo.[${t.name}] ORDER BY ${t.Keys.map(c=>'['+c.name+']').join(',')} FOR JSON PATH,INCLUDE_NULL_VALUES)),2) AS sha256`).join(' UNION ALL ')+') s ORDER BY name FOR JSON PATH;');}
try{
 const backup=fs.readFileSync('preview/product-backup-path.txt','utf8').trim().replace(/^\uFEFF/,''),literal=backup.replaceAll("'","''");
 check(json(`SELECT DB_ID(N'${database}') AS id FOR JSON PATH;`,'master')[0].id==null,'Restore destination does not exist');
 const raw=execFileSync('sqlcmd',['-S','(localdb)\\MSSQLLocalDB','-d','master','-E','-C','-b','-W','-h','-1','-s','|','-Q',`RESTORE FILELISTONLY FROM DISK=N'${literal}';`],{encoding:'utf8'});
 const files=raw.split(/\r?\n/).map(x=>x.trim().split('|')).filter(x=>['D','L'].includes(x[2]));assert.equal(files.length,2);
 fs.mkdirSync('preview/sql-data',{recursive:true});
 const moves=files.map(x=>`MOVE N'${x[0].replaceAll("'","''")}' TO N'D:\\KLTN\\preview\\sql-data\\${database}${x[2]==='L'?'_log.ldf':'.mdf'}'`).join(',');
 sql(`RESTORE DATABASE [${database}] FROM DISK=N'${literal}' WITH ${moves},CHECKSUM,RECOVERY;`,'master');check(true,'Verified backup restored into separate MDF/LDF files');
 const source=JSON.parse(fs.readFileSync('preview/product-main-values-before.json','utf8'));
 for(const row of snapshot(source.catalog)){const old=source.rows.find(x=>x.name===row.name);check(row.rows===old.rows&&row.sha256===old.sha256,row.name+' backup matches main pre-upgrade values');}
 const migration=fs.readFileSync('src/HoTroDuLich.Web/Database/003_ChuanBi_ChiTieu_LuaChon.sql','utf8');
 sql('SET QUOTED_IDENTIFIER OFF;\n'+migration);check(true,'Fresh migration creates filtered indexes from QUOTED_IDENTIFIER OFF');
 sql(migration);check(true,'Re-running migration succeeds');
 for(const row of snapshot(source.catalog)){const old=source.rows.find(x=>x.name===row.name);check(row.rows===old.rows,row.name+' original row count preserved');check(row.sha256===old.sha256,row.name+' original values preserved after both migration runs');}
 const totals=json('SELECT (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0) AS tables,(SELECT COUNT(*) FROM sys.columns c JOIN sys.tables t ON t.object_id=c.object_id WHERE t.is_ms_shipped=0) AS columns FOR JSON PATH;')[0];check(totals.tables===28&&totals.columns===302,'Restored copy has the declared 28 tables and 302 columns');
 fs.writeFileSync(output,JSON.stringify({passed:true,checks:observations.length,observations,database,backup,date:new Date().toISOString()},null,2));console.log('PASS',observations.length,'restore and migration rehearsal checks');
}catch(error){fs.writeFileSync(output,JSON.stringify({passed:false,checks:observations.length,observations,database,error:error.stack,date:new Date().toISOString()},null,2));console.error(error);process.exitCode=1;}
