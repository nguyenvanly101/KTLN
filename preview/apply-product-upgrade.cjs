// One local handover upgrade. Only the explicitly named main database is targeted.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),{execFileSync}=require('node:child_process');
assert.equal(process.argv[2],'--apply');assert.equal(process.cwd().toLowerCase(),'d:\\kltn');
const database='HoTroDuLich',observations=[];
const check=(value,name)=>{assert.ok(value,name);observations.push({name,passed:true});};
const output='tests/product-upgrade-results.json';
function sql(query){fs.writeFileSync('preview/product-upgrade-query.sql',query,'utf8');return execFileSync('sqlcmd',['-S','(localdb)\\MSSQLLocalDB','-d',database,'-E','-C','-b','-t','120','-y','0','-w','65535','-f','65001','-i','preview/product-upgrade-query.sql'],{encoding:'utf8',maxBuffer:16*1024*1024});}
function json(query){const raw=sql('SET NOCOUNT ON; '+query);return JSON.parse(raw.slice(raw.indexOf('['),raw.lastIndexOf(']')+1).replace(/[\r\n]/g,''));}
function script(file){execFileSync('sqlcmd',['-S','(localdb)\\MSSQLLocalDB','-d',database,'-E','-C','-b','-t','120','-i',file],{encoding:'utf8'});}
function snapshot(catalog){const queries=catalog.map(t=>{const cols=t.Columns.map(x=>'['+x.name+']').join(','),keys=t.Keys.map(x=>'['+x.name+']').join(',');assert.ok(keys);return `SELECT N'${t.name}' AS name,(SELECT COUNT_BIG(*) FROM dbo.[${t.name}]) AS rows,CONVERT(varchar(64),HASHBYTES('SHA2_256',(SELECT ${cols} FROM dbo.[${t.name}] ORDER BY ${keys} FOR JSON PATH,INCLUDE_NULL_VALUES)),2) AS sha256`;});return json('SELECT * FROM ('+queries.join(' UNION ALL ')+') s ORDER BY name FOR JSON PATH;');}
(async()=>{
 const backup=fs.readFileSync('preview/product-backup-path.txt','utf8').trim().replace(/^\uFEFF/,'');assert.ok(fs.existsSync(backup));
 sql("RESTORE VERIFYONLY FROM DISK=N'"+backup.replaceAll("'","''")+"' WITH CHECKSUM;");check(true,'SQL backup passed VERIFYONLY WITH CHECKSUM');
 const appBackup=JSON.parse(fs.readFileSync('preview/product-appdata-backup.json','utf8').replace(/^\uFEFF/,''));check(appBackup.passed&&fs.existsSync(appBackup.path),'Application files and keys were backed up and archive counted');
 const catalog=json("SELECT t.name,JSON_QUERY((SELECT c.name FROM sys.columns c WHERE c.object_id=t.object_id ORDER BY c.column_id FOR JSON PATH)) AS Columns,JSON_QUERY((SELECT c.name FROM sys.indexes i JOIN sys.index_columns ic ON i.object_id=ic.object_id AND i.index_id=ic.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE i.object_id=t.object_id AND i.is_primary_key=1 ORDER BY ic.key_ordinal FOR JSON PATH)) AS Keys FROM sys.tables t WHERE t.is_ms_shipped=0 ORDER BY t.name FOR JSON PATH;");
 assert.equal(catalog.length,25,'Upgrade must begin on the previous 25-table schema');
 const before=snapshot(catalog);fs.writeFileSync('preview/product-main-values-before.json',JSON.stringify({database,catalog,rows:before,date:new Date().toISOString()},null,2));
 script('src/HoTroDuLich.Web/Database/003_ChuanBi_ChiTieu_LuaChon.sql');
 script('src/HoTroDuLich.Web/Database/Quyen_CSDL.sql');
 const after=snapshot(catalog);fs.writeFileSync('preview/product-main-values-after.json',JSON.stringify({database,rows:after,date:new Date().toISOString()},null,2));
 for(const old of before){const current=after.find(x=>x.name===old.name);check(current.rows===old.rows,old.name+' original row count preserved');check(current.sha256===old.sha256,old.name+' original values and identifiers preserved (SHA-256)');}
 for(const table of ['LuaChonDaLuu','MucChuanBi','KhoanChiChuyenDi'])check(json(`SELECT COUNT(*) AS rows FROM dbo.${table} FOR JSON PATH;`)[0].rows===0,table+' starts empty; test records were not copied');
 for(const table of ['DichVuDuLich','BanCapNhatDichVu'])for(const column of ['DiaChiPhucVu','DonViGia','ChinhSachHuy','AnhJson'])check(json(`SELECT COUNT(*) AS rows FROM dbo.${table} WHERE ${column} IS NOT NULL FOR JSON PATH;`)[0].rows===0,table+'.'+column+' preserves unspecified historical data');
 fs.writeFileSync(output,JSON.stringify({passed:true,checks:observations.length,observations,database,before,after,backup,appBackup,date:new Date().toISOString()},null,2));console.log('PASS',observations.length,'main database upgrade checks');
})().catch(error=>{fs.writeFileSync(output,JSON.stringify({passed:false,checks:observations.length,observations,database,error:error.stack,date:new Date().toISOString()},null,2));console.error(error);process.exitCode=1;});
