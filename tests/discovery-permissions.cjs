const fs=require('fs'),assert=require('node:assert/strict'),{sql,json}=require('./discovery-sql.cjs');const db='HoTroDuLich_Discovery_20261108';
try{
 sql(db,fs.readFileSync('src/HoTroDuLich.Web/Database/Quyen_CSDL.sql','utf8'));
 sql(db,"IF DATABASE_PRINCIPAL_ID(N'discovery_qa') IS NULL CREATE USER [discovery_qa] WITHOUT LOGIN; ALTER ROLE r_ungdung ADD MEMBER discovery_qa;");
 const rows=json(db,"EXECUTE AS USER=N'discovery_qa'; SELECT t.name,HAS_PERMS_BY_NAME(N'dbo.'+t.name,N'OBJECT',N'SELECT') AS CanRead,HAS_PERMS_BY_NAME(N'dbo.'+t.name,N'OBJECT',N'INSERT') AS CanInsert,HAS_PERMS_BY_NAME(N'dbo.'+t.name,N'OBJECT',N'UPDATE') AS CanUpdate,HAS_PERMS_BY_NAME(N'dbo.'+t.name,N'OBJECT',N'DELETE') AS CanDelete,HAS_PERMS_BY_NAME(N'dbo.'+t.name,N'OBJECT',N'ALTER') AS CanAlter FROM sys.tables t WHERE t.name LIKE 'Discovery%' FOR JSON PATH; REVERT;");
 assert.equal(rows.length,12);for(const row of rows){assert.equal(row.CanRead,1,row.name);assert.equal(row.CanInsert,1,row.name);assert.equal(row.CanUpdate,row.name==='DiscoveryRevision'?0:1,row.name);assert.equal(row.CanDelete,0,row.name);assert.equal(row.CanAlter,0,row.name);}
 fs.writeFileSync('tests/discovery-permissions-results.json',JSON.stringify({passed:true,checks:61,rows,date:new Date().toISOString()},null,2));console.log('PASS discovery application role permissions');
}catch(error){console.error(error);process.exitCode=1;}
