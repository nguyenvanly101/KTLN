const {Client,base,val,decode}=require('./fulltext-smoke.cjs'),fs=require('node:fs'),assert=require('node:assert/strict'),{execFileSync}=require('node:child_process');
const database='HoTroDuLich_Branches_Audit_20261109054153';let checks=[];
function check(ok,name){assert.ok(ok,name);checks.push({name,passed:true});}
function sql(query){assert.match(query.trim(),/^SELECT\b/i);const raw=execFileSync('sqlcmd',['-S',process.env.AUDIT_SQL_SERVER,'-d',database,'-E','-C','-b','-l','15','-t','30','-y','0','-w','65535','-f','65001','-Q','SET NOCOUNT ON; '+query+' FOR JSON PATH;'],{encoding:'utf8',maxBuffer:4*1024*1024});const start=raw.indexOf('[');return start<0?[]:JSON.parse(raw.slice(start,raw.lastIndexOf(']')+1).replace(/[\r\n]/g,''));}
const literal=s=>"N'"+String(s).replaceAll("'","''")+"'";
const row=(table,key,id)=>sql(`SELECT * FROM ${table} WHERE ${key}=${Number(id)}`)[0];
const rv=(table,key,id)=>sql(`SELECT CONVERT(varbinary(8),RowVersion) AS Version FROM ${table} WHERE ${key}=${Number(id)}`)[0].Version;
async function status(promise,expected,name){const r=await promise;check(r.status===expected,name+': '+r.status+' expected '+expected+' '+(r.status!==expected?r.text.replace(/<[^>]+>/g,' ').replace(/\s+/g,' ').slice(0,2200):''));return r;}
async function upload(client,path,page,values,files=[{name:'hoso.pdf',type:'application/pdf',content:'%PDF-1.4\n%%EOF'}]){const form=new FormData();form.append('__RequestVerificationToken',val(page,'__RequestVerificationToken'));for(const [k,v] of Object.entries(values))form.append(k,String(v));for(const file of files)form.append('taiLieu',new Blob([file.content],{type:file.type}),file.name);return client.req(path,form);}
const today=new Date(Date.now()+7*3600000).toISOString().slice(0,10),day=n=>new Date(new Date(today+'T00:00:00Z').getTime()+n*86400000).toISOString().slice(0,10);
function save(passed,error,fixture){fs.writeFileSync('tests/fulltext-workflow-results.json',JSON.stringify({passed,checks:checks.length,observations:checks,error:error?.stack,fixture,date:new Date().toISOString(),database,base},null,2));}
module.exports={Client,base,val,decode,sql,literal,row,rv,status,check,upload,today,day,save};
