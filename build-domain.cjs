const fs=require('fs'), path=require('path');
const root='D:/KLTN/src/HoTroDuLich.Web';
const lines=fs.readFileSync('D:/KLTN/document-1.txt','utf8').split(/\r?\n/);
const domain=lines.slice(3384,4171).join('\n');
const enums={}; const enumSection=domain.slice(domain.indexOf('KetQuaThucHien'),domain.indexOf('Bảng 4.61'));
for(const m of enumSection.matchAll(/^(\w+)\n([\w, ]+)\n/gm)) if(m[2].includes(',')) enums[m[1]]=m[2].split(',').map(s=>s.trim());
const entities=[];
for(const m of domain.matchAll(/Đặc tả lớp (\w+)\n[\s\S]*?Thuộc tính\n([^\n]+)[\s\S]*?Khóa\n([^\n]+)/g)) {
 const props=m[2].split(';').map(s=>{const [name,type]=s.trim().split(': ');return {name,type}});
 const keys=[...m[3].matchAll(/(\w+) «([^»]+)»/g)].map(x=>({name:x[1],pk:x[2].includes('PK'),fk:x[2].match(/FK→(\w+)/)?.[1]}));
 entities.push({name:m[1],props,keys});
}
fs.mkdirSync(root+'/Domain',{recursive:true});fs.mkdirSync(root+'/Data',{recursive:true});
let cs='using System.ComponentModel.DataAnnotations;\nnamespace HoTroDuLich.Web.Domain;\n';
for(const e of entities){cs+=`public class ${e.name}\n{\n`;for(const p of e.props){const base=p.type.replace('?','');const type=enums[base]?'string'+(p.type.endsWith('?')?'?':''):p.type;const init=type==='string'?' = "";':type==='byte[]'?' = [];':type==='DateTime'?' = DateTime.UtcNow;':type==='Guid'?' = Guid.NewGuid();':'';cs+=` public ${type} ${p.name} { get; set; }${init}\n`;}cs+='}\n';}
fs.writeFileSync(root+'/Domain/Entities.cs',cs);
let ctx='using Microsoft.EntityFrameworkCore;\nusing HoTroDuLich.Web.Domain;\nnamespace HoTroDuLich.Web.Data;\npublic class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)\n{\n';
for(const e of entities)ctx+=` public DbSet<${e.name}> ${e.name} => Set<${e.name}>();\n`;
ctx+=' protected override void OnModelCreating(ModelBuilder b)\n {\n';
const physical=lines.slice(8513,10064).join('\n');
for(const e of entities){const n=e.name;ctx+=` b.Entity<${n}>(e => {\n e.ToTable("${n}");\n e.HasKey(x => ${e.keys.filter(x=>x.pk).length>1?'new { '+e.keys.filter(x=>x.pk).map(x=>'x.'+x.name).join(', ')+' }':'x.'+e.keys.find(x=>x.pk).name});\n`;
 for(const k of e.keys.filter(x=>x.fk))ctx+=` e.HasOne<${k.fk}>().WithMany().HasForeignKey(x => x.${k.name}).OnDelete(DeleteBehavior.Restrict);\n`;
 const block=physical.split('Bảng '+n+'\n')[1]?.split(/Bảng 4\./)[0]||'';
 for(const p of e.props){const type=p.type.replace('?',''); const col=block.split('\n'+p.name+'\n')[1]?.split(/\n\n/)[0]||''; const max=col.match(/(?:N?VARCHAR)\((\d+)\)/i)?.[1];
 if(type==='string'||enums[type])ctx+=` e.Property(x => x.${p.name}).HasMaxLength(${max|| (enums[type]?30:2000)})${enums[type]?'.IsUnicode(false)':''};\n`;
 if(type==='decimal')ctx+=` e.Property(x => x.${p.name}).HasPrecision(18,2);\n`;
 if(p.name==='RowVersion')ctx+=` e.Property(x => x.RowVersion).IsRowVersion();\n`;
 if(enums[type])ctx+=` e.ToTable(t => t.HasCheckConstraint("CK_${n}_${p.name}", "[${p.name}] IN (${enums[type].map(v=>"'"+v+"'").join(',')})"));\n`;
 const start=block.indexOf('\n'+p.name+'\n'); const next=e.props[e.props.indexOf(p)+1];const section=start<0?'':block.slice(start,next?block.indexOf('\n'+next.name+'\n',start+1):undefined);
 const constraint=(suffix,sql)=>{ctx+=` e.ToTable(t => t.HasCheckConstraint("CK_${n}_${p.name}${suffix}", ${JSON.stringify(sql)}));\n`};
 if(type==='decimal')constraint('',`[${p.name}] >= 0`);
 if(section.includes('CK không rỗng'))constraint('_KhongRong',`LEN(LTRIM([${p.name}])) > 0`);
 if(section.includes('CK định dạng email'))constraint('',`[${p.name}] LIKE '%_@_%._%' AND [${p.name}] NOT LIKE '% %'`);
 if(section.includes('CK 0 + 9–10 số'))constraint('',`[${p.name}] LIKE '0%' AND [${p.name}] NOT LIKE '%[^0-9]%' AND LEN([${p.name}]) BETWEEN 10 AND 11`);
 const range=section.match(/CK BETWEEN (\d+) AND (\d+)/);if(range)constraint('',`[${p.name}] BETWEEN ${range[1]} AND ${range[2]}`);
 const lower=section.match(/CK >= (\d+)/);if(lower&&type!=='decimal')constraint('',`[${p.name}] >= ${lower[1]}`);
 }
 ctx+=' });\n';
}
const checks=lines.slice(10064,10353).join('\n');
for(const m of checks.matchAll(/^(CK_(\w+))\n([^\n]+)\n(?:RB-|BS)/gm)){
 const table=entities.find(e=>m[1].startsWith('CK_'+e.name+'_'))?.name;
 if(table)ctx+=` b.Entity<${table}>().ToTable(t => t.HasCheckConstraint("${m[1]}", ${JSON.stringify(m[3])}));\n`;
}
for(const m of checks.matchAll(/^(UX_\w+)\n(\w+) \(([^)]+)\)\n([^\n]+)/gm)){
const cols=m[3].split(',').map(s=>'x.'+s.trim());ctx+=` b.Entity<${m[2]}>().HasIndex(x => ${cols.length===1?cols[0]:'new { '+cols.join(', ')+' }'}, "${m[1]}").IsUnique().HasDatabaseName("${m[1]}")${m[4]==='—'?'':'.HasFilter('+JSON.stringify(m[4])+')'};\n`;
}
ctx+=' }\n}\n';fs.writeFileSync(root+'/Data/AppDbContext.cs',ctx);
fs.writeFileSync('D:/KLTN/domain-metadata.json',JSON.stringify({entities,enums},null,2));
console.log('Generated',entities.length,'entities,',Object.keys(enums).length,'state domains');
