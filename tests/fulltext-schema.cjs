const {execFileSync}=require('node:child_process'),assert=require('node:assert/strict'),fs=require('node:fs');
const companionExtension=process.env.TRAVEL_COMPANION_EXTENSION==='1';
const journeyExtension=process.env.TRAVEL_JOURNEY_EXTENSION==='1'||companionExtension;
const productExtension=process.env.TRAVEL_PRODUCT_EXTENSION==='1'||journeyExtension;
const tripExtension=process.env.TRAVEL_TRIP_EXTENSION==='1'||productExtension;
const database=process.env.TRAVEL_SCHEMA_DB||'HoTroDuLich_FullText_20261104';assert.ok(database==='HoTroDuLich'||database.startsWith('HoTroDuLich_FullText')||(tripExtension&&(database==='HoTroDuLich_TripFlow_20261104'||(productExtension&&(database==='HoTroDuLich_Product_20261105'||journeyExtension&&['HoTroDuLich_Journey_20261105','HoTroDuLich_JourneyUpgrade_20261105',...(companionExtension?['HoTroDuLich_Companion_20261105','HoTroDuLich_CompanionUpgrade_20261105']:[])].includes(database))))),'Expected a verified local application database');
const raw=execFileSync('sqlcmd',['-S','(localdb)\\MSSQLLocalDB','-d',database,'-E','-C','-b','-y','0','-w','65535','-f','65001','-i','tests/fulltext-schema.sql'],{encoding:'utf8',maxBuffer:8*1024*1024});
const schema=JSON.parse(raw.slice(raw.indexOf('{'),raw.lastIndexOf('}')+1).replace(/[\r\n]/g,'')),contract=JSON.parse(fs.readFileSync('docs/fulltext-contract.json','utf8'));let checks=0;const check=(ok,message)=>{assert.ok(ok,message);checks++};
check(new Set(schema.columns.map(c=>c.table)).size===(companionExtension?46:journeyExtension?31:productExtension?28:25),'declared tables');check(schema.columns.length===(companionExtension?473:journeyExtension?335:productExtension?302:tripExtension?268:267),'baseline columns + declared trip extension');check(schema.checks.length===(companionExtension?161:journeyExtension?132:productExtension?123:117),'117 CHECK');check(schema.checks.every(c=>!c.is_disabled&&!c.is_not_trusted),'trusted enabled CHECK constraints');check(schema.foreignKeys.length===(companionExtension?83:journeyExtension?47:productExtension?44:tripExtension?39:38),'baseline FKs + declared trip extension');check(schema.foreignKeys.every(f=>f.delete_referential_action_desc==='NO_ACTION'&&!f.is_disabled&&!f.is_not_trusted),'trusted NO ACTION FKs');check(schema.indexes.filter(i=>i.is_unique&&!i.is_primary_key).length===(companionExtension?31:journeyExtension?25:productExtension?22:18),'declared unique indexes');check(schema.indexes.filter(i=>!i.is_unique).length===(companionExtension?50:journeyExtension?40:productExtension?38:tripExtension?35:34),'baseline indexes + declared trip extension');check(schema.views.length===4,'4 views');
if(productExtension)for(const manifest of companionExtension?['product','journey','companion']:journeyExtension?['product','journey']:['product']){const additions=JSON.parse(fs.readFileSync('docs/'+manifest+'-extension-schema.json','utf8'));Object.assign(contract.tables,additions.tables);for(const [table,columns]of Object.entries(additions.additionalColumns))contract.tables[table].push(...columns);}
const settings=schema.settings;check(settings.collation_name==='Vietnamese_100_CI_AS_SC','database collation');check(settings.is_read_committed_snapshot_on===true,'RCSI');check(settings.snapshot_isolation_state_desc==='OFF','snapshot isolation OFF');check(settings.recovery_model_desc==='FULL','FULL recovery');check(settings.page_verify_option_desc==='CHECKSUM','page checksum');check(settings.is_auto_close_on===false,'AUTO_CLOSE OFF');
for(const [table,columns] of Object.entries(contract.tables))for(const column of columns){const c=schema.columns.find(c=>c.table===table&&c.name===column.name);check(!!c,table+'.'+column.name);if(!c)continue;let type=c.type==='timestamp'?'ROWVERSION':c.type.toUpperCase();if(['nvarchar','varchar','char','nchar'].includes(c.type))type+='('+(c.max_length<0?'MAX':c.max_length/(['nvarchar','nchar'].includes(c.type)?2:1))+')';if(['datetime2','time'].includes(c.type))type+='('+c.scale+')';if(c.type==='decimal')type+='('+c.precision+','+c.scale+')';if(c.is_identity)type+=' IDENTITY';check(type.toUpperCase()===column.sqlType.toUpperCase(),table+'.'+column.name+' type: '+type+' vs '+column.sqlType);check(c.is_nullable===(column.nullable==='x'),table+'.'+column.name+' nullable');if(column.default){check(c.default_name==='DF_'+table+'_'+column.name,'named default '+table+'.'+column.name);const normalize=s=>s.replace(/[()\s]/g,'').toLowerCase();check(normalize(c.default_sql)===normalize(column.default),'default SQL '+table+'.'+column.name);}}
for(const index of contract.queryIndexes)check(schema.indexes.some(i=>i.name===index.name&&i.table===index.table),'designed index '+index.name);
// Compare actual index keys, INCLUDE, filters, FK endpoints and view SELECTs.
const constraints=JSON.parse(fs.readFileSync('docs/fulltext-constraints.json','utf8'));
const compact=s=>(s||'').replace(/[\[\]\s;]/g,'').toLowerCase();
const filter=s=>compact(s).replace(/[()]/g,'');
function checkIndex(expected){
 const actual=schema.indexes.find(i=>i.name===expected.name&&i.table===expected.table);
 check(!!actual,'index '+expected.name);if(!actual)return;
 check(actual.is_unique===!!expected.unique,'unique '+expected.name);
 check(JSON.stringify(actual.columns.filter(c=>!c.is_included_column).map(c=>[c.name,c.is_descending_key]))===JSON.stringify(expected.keys),'ordered keys '+expected.name);
 check(JSON.stringify(actual.columns.filter(c=>c.is_included_column).map(c=>c.name).sort())===JSON.stringify((expected.include||[]).slice().sort()),'INCLUDE '+expected.name);
 check(filter(actual.filter_definition)===filter(expected.filter),'filter '+expected.name);
}
if(tripExtension){
 const tripColumn=schema.columns.find(c=>c.table==='YeuCauDatDichVu'&&c.name==='MaKeHoach');
 check(tripColumn?.type==='bigint'&&tripColumn.is_nullable,'declared nullable trip association column');
 const tripKey=schema.foreignKeys.find(f=>f.name==='FK_YeuCauDatDichVu_MaKeHoach');
 check(tripKey?.table==='YeuCauDatDichVu'&&tripKey.columns.length===1&&tripKey.columns[0].column==='MaKeHoach'&&tripKey.columns[0].reference_table==='KeHoachChuyenDi'&&tripKey.columns[0].reference_column==='MaKeHoach','declared trip FK endpoints');
 checkIndex({name:'IX_YeuCau_KeHoach',table:'YeuCauDatDichVu',keys:[['MaKeHoach',false],['ThoiDiemGui',true]],include:['MaDichVu','TrangThai']});
}
if(productExtension)for(const manifest of companionExtension?['product','journey','companion']:journeyExtension?['product','journey']:['product'])for(const index of JSON.parse(fs.readFileSync('docs/'+manifest+'-extension-schema.json','utf8')).indexes)checkIndex(index);
for(const index of contract.queryIndexes){const [keys,include]=index.columns.split('(');checkIndex({...index,keys:keys.trim().split(',').map(c=>[c.trim().split(/\s+/)[0],/\bDESC\b/i.test(c)]),include:include?include.replace(/\)$/,'').split(',').map(c=>c.trim()):[],filter:/^[—–-]$/.test(index.filter)?'':index.filter});}
for(const index of constraints.uniqueIndexes)checkIndex({...index,unique:true,keys:index.columns.map(c=>[c,false])});
for(const [table,columns] of Object.entries(contract.tables)){
 const primary=columns.filter(c=>/\bPK\b/.test(c.rules)).map(c=>c.name);
 const actualPrimary=schema.indexes.find(i=>i.table===table&&i.is_primary_key);
 check(!!actualPrimary&&JSON.stringify(actualPrimary.columns.map(c=>c.name))===JSON.stringify(primary),'primary key '+table);
 for(const column of columns){const target=column.rules.match(/FK\s*→\s*(\w+)/);if(!target)continue;
  const parent=contract.tables[target[1]].filter(c=>/\bPK\b/.test(c.rules));
  const fk=schema.foreignKeys.find(f=>f.name==='FK_'+table+'_'+column.name);
  check(!!fk&&fk.table===table&&fk.columns.length===1&&fk.columns[0].column===column.name&&fk.columns[0].reference_table===target[1]&&fk.columns[0].reference_column===parent[0].name,'foreign key endpoints '+table+'.'+column.name);
  check(fk?.update_referential_action_desc==='NO_ACTION','NO ACTION update '+table+'.'+column.name);
 }
}
if(companionExtension)for(const expected of JSON.parse(fs.readFileSync('docs/companion-extension-schema.json','utf8')).foreignKeys){const actual=schema.foreignKeys.find(f=>f.name===expected.name);check(!!actual&&actual.table===expected.table&&JSON.stringify(actual.columns.map(c=>[c.column,c.reference_table,c.reference_column]))===JSON.stringify(expected.columns.map((c,n)=>[c,expected.referenceTable,expected.keys[n]])),'companion FK endpoints '+expected.name);}
for(const view of contract.views){const actual=schema.views.find(v=>v.name===view.name);check(!!actual&&compact(actual.definition.slice(actual.definition.search(/\bSELECT\b/i)))===compact(view.sql),'view SELECT '+view.name);}
// Compile expected CHECKs on empty #temp tables. SQL Server normalizes IN and
// parentheses itself, so comparison retains Boolean grouping and NULL semantics.
// No table or row in the application database is changed by this probe.
const sqlSource=fs.readFileSync('src/HoTroDuLich.Web/Database/schema.sql','utf8').replace(/\r\n/g,'\n');
const expectedChecks=[];let probe='SET NOCOUNT ON;\n';
for(const match of sqlSource.matchAll(/CREATE TABLE \[(\w+)\] \(\n([\s\S]*?)\n\);/g)){
 for(const line of match[2].split('\n')){const item=line.match(/^\s*CONSTRAINT \[(CK_[^\]]+)\] CHECK \((.*)\),?\s*$/);if(item)expectedChecks.push({table:match[1],name:item[1],expression:item[2]});}
}
check(expectedChecks.length===117,'117 CHECK expressions parsed from reviewed schema');
if(productExtension)for(const manifest of companionExtension?['product','journey','companion']:journeyExtension?['product','journey']:['product'])expectedChecks.push(...JSON.parse(fs.readFileSync('docs/'+manifest+'-extension-schema.json','utf8')).checks);
check(expectedChecks.length===(companionExtension?161:journeyExtension?132:productExtension?123:117),'Every declared CHECK has an expected expression');
for(const [table,columns]of Object.entries(contract.tables)){
 const declarations=columns.map(c=>'['+c.name+'] '+c.sqlType.replace(/ IDENTITY\b/,'')+' NULL');
 for(const item of expectedChecks.filter(c=>c.table===table)){
  assert.ok(!/;|--|\/\*/.test(item.expression),'Reviewed CHECK expression');
  declarations.push('CONSTRAINT [SC_'+item.name+'] CHECK ('+item.expression+')');
  const explicit=constraints.checks.find(c=>c.name===item.name);if(explicit)declarations.push('CONSTRAINT [FT_'+item.name+'] CHECK ('+explicit.expression+')');
 }
 probe+='CREATE TABLE [#ft_'+table+'] (\n'+declarations.join(',\n')+'\n);\n';
}
probe+="SELECT name,definition FROM tempdb.sys.check_constraints WHERE parent_object_id IN ("+Object.keys(contract.tables).map(t=>"OBJECT_ID(N'tempdb..#ft_"+t+"')").join(',')+") FOR JSON PATH;\n";
fs.writeFileSync('tests/fulltext-constraint-probe.sql',probe);
const compiledRaw=execFileSync('sqlcmd',['-S','(localdb)\\MSSQLLocalDB','-d',database,'-E','-C','-b','-l','15','-t','30','-y','0','-w','65535','-f','65001','-i','tests/fulltext-constraint-probe.sql'],{encoding:'utf8',maxBuffer:8*1024*1024});
const compiled=JSON.parse(compiledRaw.slice(compiledRaw.indexOf('['),compiledRaw.lastIndexOf(']')+1).replace(/[\r\n]/g,''));
for(const expected of expectedChecks){const actual=schema.checks.find(c=>c.name===expected.name);const normalized=compiled.find(c=>c.name==='SC_'+expected.name);check(!!actual&&actual.table===expected.table&&compact(actual.definition)===compact(normalized?.definition),'physical CHECK expression '+expected.name);}
for(const explicit of constraints.checks)check(compact(compiled.find(c=>c.name==='SC_'+explicit.name)?.definition)===compact(compiled.find(c=>c.name==='FT_'+explicit.name)?.definition),'FullText table 4.161 expression '+explicit.name);
const resultFile=process.env.TRAVEL_SCHEMA_RESULT||'tests/fulltext-schema-results.json';assert.ok(resultFile.startsWith('tests/'),'Results must stay inside tests');fs.writeFileSync(resultFile,JSON.stringify({passed:true,checks,profile:companionExtension?'FullText + trip + product + journey + companion':journeyExtension?'FullText + trip + product + journey':productExtension?'FullText + trip + product features':tripExtension?'FullText + optional trip association':'FullText baseline',schema,date:new Date().toISOString()},null,2));console.log('PASS',checks,'physical schema checks on',database);
