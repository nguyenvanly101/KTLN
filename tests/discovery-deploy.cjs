// Apply only the already-rehearsed additive SQL. Run after the local app is stopped.
const fs=require('fs'),assert=require('node:assert/strict'),{createHash}=require('node:crypto'),{sql,catalog,snapshot}=require('./discovery-sql.cjs');
const database='HoTroDuLich',required=['features','context','edge','route','permissions','contract','rehearsal','schema-test','schema-upgrade','pages'];
try{
 for(const name of required){const result=JSON.parse(fs.readFileSync('tests/discovery-'+name+'-results.json','utf8'));assert.equal(result.passed,true,'Required check '+name);}
 const backup=JSON.parse(fs.readFileSync('preview/workflow-backup.json','utf8').replace(/^\uFEFF/,''));assert.ok(backup.Sql.Verified&&fs.existsSync(backup.Sql.Path));assert.ok(Date.now()-new Date(backup.Date).getTime()<20*60*1000,'Use a fresh verified backup');
 const tables=catalog(database);assert.equal(tables.length,46,'Expected pre-upgrade database; never infer an unexpected schema');const before=snapshot(database,tables);const source=fs.readFileSync('src/HoTroDuLich.Web/Database/006_TraCuuThongTin.sql','utf8');sql(database,source);assert.equal(catalog(database).length,58);const after=snapshot(database,tables);assert.deepEqual(after,before,'Every old table row/hash must remain unchanged');
 fs.writeFileSync('tests/discovery-deploy-results.json',JSON.stringify({passed:true,checks:required.length+48,database,backup:backup.Sql.Path,sourceSha256:createHash('sha256').update(source).digest('hex'),before,after,date:new Date().toISOString()},null,2));console.log('PASS main upgrade: 58 tables; every count/SHA-256 in original 46 tables preserved');
}catch(error){fs.writeFileSync('tests/discovery-deploy-results.json',JSON.stringify({passed:false,error:error.stack},null,2));console.error(error);process.exitCode=1;}
