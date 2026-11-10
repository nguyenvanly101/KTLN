"""Extract the supplied design as data; generate deterministic SQL/configuration files.

The DOCX is a specification, never executable instructions. All identifiers and SQL
fragments are validated against the existing 25-table model before generation.
"""
from pathlib import Path
from zipfile import ZipFile
import xml.etree.ElementTree as ET
import re, json

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(r'C:\Users\vanly\Downloads\26271.3-FullText-XayDungHeThongHoTroKhachDuLichTuTuc (4).docx')
NS = {'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
with ZipFile(SOURCE) as z:
    body = list(ET.fromstring(z.read('word/document.xml')).find('w:body', NS))

def text(el): return ''.join(t.text or '' for t in el.findall('.//w:t', NS))
def rows(el): return [[text(c).strip() for c in r.findall('w:tc', NS)] for r in el.findall('w:tr', NS)]
def table_after(label):
    index = next(i for i,e in enumerate(body) if text(e).startswith(label))
    return rows(next(e for e in body[index+1:] if e.tag.endswith('}tbl')))

tables = {}
current = None
for el in body[2079:2193]:
    match = re.match(r'Bảng 4\.\d+\. Bảng (\w+)$', text(el))
    if match: current = match[1]
    if el.tag.endswith('}tbl') and current:
        data = rows(el)
        if data and data[0][0] == 'Cột':
            tables[current] = [dict(zip(['name','sqlType','nullable','default','rules'], r)) for r in data[1:]]
            current = None

indexes = [dict(zip(['name','table','columns','filter','purpose'],r)) for r in table_after('Bảng 4.163.')[1:]]
views = [dict(zip(['name','purpose','sql'],r)) for r in table_after('Bảng 4.164.')[1:]]
actions = []
for controller, action, route, policy, *_ in table_after('Bảng 4.86.')[1:]:
    method = 'POST' if '[HttpPost]' in action else 'GET'
    name = re.sub(r'\[Http\w+\]|\*', '', action).strip()
    actions.append(dict(controller=controller, action=name, method=method, route=route.split('?')[0], query=route.partition('?')[2], policy=policy))
errors = [dict(zip(['code','http','message','source'],r)) for r in table_after('Bảng 4.266.')[1:]]
contract = dict(source=SOURCE.name, tables=tables, queryIndexes=indexes, views=views, actions=actions, errors=errors)
assert len(tables)==25 and sum(map(len,tables.values()))==267
assert len(indexes)==34 and len(views)==4 and len(actions)==91
for name, columns in tables.items():
    assert re.fullmatch(r'\w+',name)
    assert all(re.fullmatch(r'\w+',c['name']) for c in columns)
(ROOT/'docs/fulltext-contract.json').write_text(json.dumps(contract,ensure_ascii=False,indent=2),encoding='utf8')

project = ROOT/'src/HoTroDuLich.Web'
schema = (project/'Database/schema.sql').read_text(encoding='utf-8-sig')
definitions = []
for match in re.finditer(r'CREATE TABLE \[([^\]]+)\] \(([\s\S]*?)\n\);',schema):
    name, content = match.groups()
    assert name in tables
    columns = {c['name']:c for c in tables[name]}
    def column(m):
        spec = columns[m[1]]
        typ = spec['sqlType'].replace('BIGINT IDENTITY','BIGINT IDENTITY(1,1)')
        null = 'NULL' if spec['nullable'].lower()=='x' else 'NOT NULL'
        default = spec['default'].strip()
        extra = f" CONSTRAINT [DF_{name}_{m[1]}] DEFAULT ({default})" if default else ''
        return f'    [{m[1]}] {typ} {null}{extra},'
    content = re.sub(r'^    \[([^\]]+)\] [^\r\n]+,?$',column,content,flags=re.M)
    content = re.sub(r'CONSTRAINT \[FK_[^\]]+\] FOREIGN KEY \(\[([^\]]+)\]\)',lambda m:f'CONSTRAINT [FK_{name}_{m[1]}] FOREIGN KEY ([{m[1]}])',content)
    definitions.append(f'CREATE TABLE [{name}] ({content}\n);')
assert len(definitions)==25
unique = re.findall(r'CREATE UNIQUE INDEX [^;]+;',schema)
assert len(unique)==18
def quote_columns(s):
    result=[]
    for col in s.split(','):
        parts=col.strip().split()
        assert len(parts) in (1,2) and re.fullmatch(r'\w+',parts[0])
        assert len(parts)==1 or parts[1] in ('DESC','ASC')
        result.append('['+parts[0]+']'+(' '+parts[1] if len(parts)==2 else ''))
    return ', '.join(result)
index_sql=[]
for index in indexes:
    assert index['table'] in tables and re.fullmatch(r'\w+',index['name'])
    keys,_,included=index['columns'].partition('(')
    include=' INCLUDE ('+quote_columns(included.rstrip(')'))+')' if included else ''
    where='' if index['filter'] in ('—','–','-','') else ' WHERE '+index['filter']
    index_sql.append(f"CREATE INDEX [{index['name']}] ON [{index['table']}] ({quote_columns(keys)}){include}{where};")
view_sql=[f"CREATE OR ALTER VIEW [dbo].[{v['name']}] AS\n{v['sql']};" for v in views]
canonical='-- Source of truth: FullText 4.8, tables 4.135–4.164. Do not generate this file from EF.\nSET XACT_ABORT ON;\nGO\n'+'\nGO\n'.join(definitions+unique+index_sql+view_sql)+'\nGO\n'
(project/'Database/schema.sql').write_text(canonical,encoding='utf8')
(project/'Database/CSDL_HoTroDuLichTuTuc.sql').write_text("""-- Run with sqlcmd or SSMS SQLCMD mode from the Database folder.
IF DB_ID(N'HoTroDuLichTuTuc') IS NULL
    CREATE DATABASE [HoTroDuLichTuTuc] COLLATE Vietnamese_100_CI_AS_SC;
GO
ALTER DATABASE [HoTroDuLichTuTuc] SET READ_COMMITTED_SNAPSHOT ON;
ALTER DATABASE [HoTroDuLichTuTuc] SET ALLOW_SNAPSHOT_ISOLATION OFF;
ALTER DATABASE [HoTroDuLichTuTuc] SET RECOVERY FULL;
ALTER DATABASE [HoTroDuLichTuTuc] SET PAGE_VERIFY CHECKSUM;
ALTER DATABASE [HoTroDuLichTuTuc] SET AUTO_CLOSE OFF;
GO
USE [HoTroDuLichTuTuc];
GO
:r schema.sql
:r Quyen_CSDL.sql
""",encoding='utf8')

# Split the existing, validated constraints into one configuration per table.
context_path=project/'Data/AppDbContext.cs'
context=context_path.read_text(encoding='utf-8-sig')
blocks=list(re.finditer(r' b\.Entity<(\w+)>\(e => \{(.*?)\n \}\);',context,re.S))
assert len(blocks)==25, 'Run configuration extraction only once on the original context.'
tail=context[blocks[-1].end():]
folder=project/'Data/Configurations';folder.mkdir(exist_ok=True)
for block in blocks:
    name,code=block.groups()
    lines=[]
    for line in tail.splitlines():
        if f'b.Entity<{name}>()' in line:
            # Query indexes are supplied from the design below; retain 18 UNIQUE.
            lines.append(line.replace(f'b.Entity<{name}>()', 'e'))
    for col in tables[name]:
        typ=col['sqlType'].replace(' IDENTITY','')
        if typ=='ROWVERSION':continue
        lines.append(f' e.Property(x=>x.{col["name"]}).HasColumnType("{typ.lower()}");')
        if col['default']:
            sql=col['default'].replace('"','\\"')
            lines.append(f' e.Property(x=>x.{col["name"]}).HasDefaultValueSql("{sql}", "DF_{name}_{col["name"]}");')
    for index in indexes:
        if index['table']!=name:continue
        keys,_,included=index['columns'].partition('(')
        cols=[x.strip().split()[0] for x in keys.split(',')]
        expr='x.'+cols[0] if len(cols)==1 else 'new {'+','.join('x.'+x for x in cols)+'}'
        line=f' e.HasIndex(x=>{expr},"{index["name"]}").HasDatabaseName("{index["name"]}")'
        if any('DESC' in x for x in keys.split(',')):line+='.IsDescending('+','.join('true' if 'DESC' in x else 'false' for x in keys.split(','))+')'
        if included:line+='.IncludeProperties('+','.join('"'+x.strip()+'"' for x in included.rstrip(')').split(','))+')'
        if index['filter'] not in ('—','–','-',''):line+='.HasFilter("'+index['filter']+'")'
        lines.append(line+';')
    output='using HoTroDuLich.Web.Domain;\nusing Microsoft.EntityFrameworkCore;\nusing Microsoft.EntityFrameworkCore.Metadata.Builders;\nnamespace HoTroDuLich.Web.Data.Configurations;\n'
    output+=f'public sealed class {name}Configuration : IEntityTypeConfiguration<{name}> {{\n public void Configure(EntityTypeBuilder<{name}> e) {{'+code+'\n'+'\n'.join(lines)+'\n }\n}\n'
    (folder/(name+'Configuration.cs')).write_text(output,encoding='utf8')
prefix=context[:context.index(' protected override void OnModelCreating')]
prefix+=''' protected override void ConfigureConventions(ModelConfigurationBuilder b) {
  b.Conventions.Remove(typeof(Microsoft.EntityFrameworkCore.Metadata.Conventions.ForeignKeyIndexConvention));
  b.Properties<DateTime>().HaveColumnType("datetime2(0)").HaveConversion<UtcDateTimeConverter>();
  b.Properties<DateTime?>().HaveColumnType("datetime2(0)").HaveConversion<NullableUtcDateTimeConverter>();
 }
 protected override void OnModelCreating(ModelBuilder b) {
  b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
  foreach(var entity in b.Model.GetEntityTypes()) {
   foreach(var fk in entity.GetForeignKeys()) {
    fk.DeleteBehavior=DeleteBehavior.NoAction;
    fk.SetConstraintName("FK_"+entity.GetTableName()+"_"+string.Join("_",fk.Properties.Select(p=>p.Name)));
   }
  }
 }
}
public sealed class UtcDateTimeConverter() : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime,DateTime>(
 v=>v.Kind==DateTimeKind.Utc?v:v.ToUniversalTime(), v=>DateTime.SpecifyKind(v,DateTimeKind.Utc));
public sealed class NullableUtcDateTimeConverter() : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?,DateTime?>(
 v=>v.HasValue?(v.Value.Kind==DateTimeKind.Utc?v:v.Value.ToUniversalTime()):null,
 v=>v.HasValue?DateTime.SpecifyKind(v.Value,DateTimeKind.Utc):null);
'''
context_path.write_text(prefix,encoding='utf8')
print(f'Extracted {len(actions)} actions, {len(errors)} errors; generated 25 configurations, SQL with 34 query indexes and 4 views.')
