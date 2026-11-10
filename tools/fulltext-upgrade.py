"""Build a numbered, data-preserving ALTER script from the reviewed SQL contract."""
from pathlib import Path
import json,re
root=Path(__file__).resolve().parents[1]
folder=root/'src/HoTroDuLich.Web/Database'
contract=json.loads((root/'docs/fulltext-contract.json').read_text(encoding='utf-8'))
schema=(folder/'schema.sql').read_text(encoding='utf-8')
blocks=re.findall(r'CREATE TABLE \[(\w+)\] \(\n(.*?)\n\);',schema,re.S)
assert len(blocks)==25
constraints=[]
for table,body in blocks:
 for line in body.splitlines():
  line=line.strip().removesuffix(',')
  if line.startswith('CONSTRAINT [CK_') or line.startswith('CONSTRAINT [FK_'):
   constraints.append(f'ALTER TABLE [dbo].[{table}] WITH CHECK ADD {line};')
assert sum('[CK_' in x for x in constraints)==117
assert sum('[FK_' in x for x in constraints)==38
indexes=re.findall(r'^CREATE (?:UNIQUE )?INDEX .*?;$',schema,re.M)
assert len(indexes)==52,len(indexes)
views=re.findall(r'CREATE OR ALTER VIEW .*?(?=\nGO|\Z)',schema,re.S)
assert len(views)==4
preflight=[];alter=[];defaults=[]
for table,columns in contract['tables'].items():
 for c in columns:
  name,t=c['name'],c['sqlType']
  if 'IDENTITY' in t or t=='ROWVERSION' or re.search(r'\bPK\b',c['rules']):continue
  match=re.fullmatch(r'(N?VARCHAR)\((\d+)\)',t)
  if match:
   maximum=int(match[2]);expr=f'DATALENGTH([{name}])' if match[1]=='NVARCHAR' else f'DATALENGTH(CONVERT(VARCHAR(MAX),[{name}] COLLATE Vietnamese_100_CI_AS_SC))'
   limit=maximum*(2 if match[1]=='NVARCHAR' else 1)
   preflight.append(f"IF EXISTS(SELECT 1 FROM [dbo].[{table}] WHERE {expr}>{limit}) THROW 51002, N'Du lieu {table}.{name} vuot gioi han FullText; da huy nang cap, khong cat du lieu.',1;")
  if c['nullable']!='x':preflight.append(f"IF EXISTS(SELECT 1 FROM [dbo].[{table}] WHERE [{name}] IS NULL) THROW 51003, N'Du lieu NULL tai {table}.{name}; da huy nang cap.',1;")
  collate=' COLLATE Vietnamese_100_CI_AS_SC' if t.startswith(('NVARCHAR','VARCHAR')) else ''
  alter.append(f'ALTER TABLE [dbo].[{table}] ALTER COLUMN [{name}] {t}{collate} '+('NULL' if c['nullable']=='x' else 'NOT NULL')+';')
  if c['default']:defaults.append(f'ALTER TABLE [dbo].[{table}] ADD CONSTRAINT [DF_{table}_{name}] DEFAULT ({c["default"]}) FOR [{name}];')
head='''-- 001: Align an existing 25-table database to FullText 4.8.
-- Stop the web application, make a CHECKSUM backup and VERIFYONLY first.
-- No business table is dropped; no business row is deleted or truncated.
-- SQLCMD: sqlcmd ... -d <database> -i 001_CanChinh_FullText.sql -b
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
IF DB_ID()<=4 THROW 51000,'Do not run against a system database.',1;
IF (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0)<>25 OR OBJECT_ID('dbo.TaiKhoan') IS NULL THROW 51001,'Expected the existing travel application schema.',1;
'''
drop='''
-- Database collation cannot change inside a user transaction. The web app must
-- stay stopped until all three phases and the checks at the end have succeeded.
BEGIN TRANSACTION;
DECLARE @drop nvarchar(max)=N'';
SELECT @drop+=N'DROP VIEW '+QUOTENAME(SCHEMA_NAME(schema_id))+N'.'+QUOTENAME(name)+N';' FROM sys.views WHERE is_ms_shipped=0;
SELECT @drop+=N'ALTER TABLE '+QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id))+N'.'+QUOTENAME(OBJECT_NAME(parent_object_id))+N' DROP CONSTRAINT '+QUOTENAME(name)+N';' FROM sys.foreign_keys;
SELECT @drop+=N'ALTER TABLE '+QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id))+N'.'+QUOTENAME(OBJECT_NAME(parent_object_id))+N' DROP CONSTRAINT '+QUOTENAME(name)+N';' FROM sys.check_constraints;
SELECT @drop+=N'ALTER TABLE '+QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id))+N'.'+QUOTENAME(OBJECT_NAME(parent_object_id))+N' DROP CONSTRAINT '+QUOTENAME(name)+N';' FROM sys.default_constraints;
SELECT @drop+=N'ALTER TABLE '+QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id))+N'.'+QUOTENAME(OBJECT_NAME(parent_object_id))+N' DROP CONSTRAINT '+QUOTENAME(name)+N';' FROM sys.key_constraints WHERE type='UQ';
SELECT @drop+=N'DROP INDEX '+QUOTENAME(i.name)+N' ON '+QUOTENAME(OBJECT_SCHEMA_NAME(i.object_id))+N'.'+QUOTENAME(OBJECT_NAME(i.object_id))+N';' FROM sys.indexes i JOIN sys.tables t ON t.object_id=i.object_id WHERE t.is_ms_shipped=0 AND i.index_id>0 AND i.is_primary_key=0 AND i.is_unique_constraint=0;
EXEC sys.sp_executesql @drop;
COMMIT;
GO
DECLARE @collation nvarchar(max)=N'ALTER DATABASE '+QUOTENAME(DB_NAME())+N' COLLATE Vietnamese_100_CI_AS_SC;';
EXEC sys.sp_executesql @collation;
GO
BEGIN TRANSACTION;
'''
tail='''
COMMIT;
GO
DECLARE @options nvarchar(max)=N'ALTER DATABASE '+QUOTENAME(DB_NAME())+N' SET READ_COMMITTED_SNAPSHOT ON; ALTER DATABASE '+QUOTENAME(DB_NAME())+N' SET ALLOW_SNAPSHOT_ISOLATION OFF; ALTER DATABASE '+QUOTENAME(DB_NAME())+N' SET RECOVERY FULL; ALTER DATABASE '+QUOTENAME(DB_NAME())+N' SET PAGE_VERIFY CHECKSUM; ALTER DATABASE '+QUOTENAME(DB_NAME())+N' SET AUTO_CLOSE OFF;';
EXEC sys.sp_executesql @options;
GO
'''
result=head+'\n'.join(preflight)+drop+'\n'.join(alter+defaults+constraints+indexes)+tail+'\nGO\n'.join(views)+'\nGO\n:r KiemTra_FullText.sql\n'
(folder/'001_CanChinh_FullText.sql').write_text(result,encoding='utf-8')
checks='''-- FullText PHAN 9: fail deployment if the physical contract is incomplete.
SET NOCOUNT ON;
IF (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0)<>25 THROW 51010,'Expected 25 tables.',1;
IF (SELECT COUNT(*) FROM sys.columns c JOIN sys.tables t ON c.object_id=t.object_id WHERE t.is_ms_shipped=0)<>267 THROW 51011,'Expected 267 columns.',1;
IF (SELECT COUNT(*) FROM sys.foreign_keys)<>38 THROW 51012,'Expected 38 foreign keys.',1;
IF (SELECT COUNT(*) FROM sys.check_constraints)<>117 THROW 51013,'Expected 117 CHECK constraints.',1;
IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1) OR EXISTS(SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1) THROW 51014,'Constraints must be enabled and trusted.',1;
IF (SELECT COUNT(*) FROM sys.indexes i JOIN sys.tables t ON t.object_id=i.object_id WHERE t.is_ms_shipped=0 AND i.is_unique=1 AND i.is_primary_key=0)<>18 THROW 51015,'Expected 18 unique indexes.',1;
IF (SELECT COUNT(*) FROM sys.indexes i JOIN sys.tables t ON t.object_id=i.object_id WHERE t.is_ms_shipped=0 AND i.is_unique=0 AND i.index_id>0)<>34 THROW 51016,'Expected 34 query indexes.',1;
IF (SELECT COUNT(*) FROM sys.views WHERE is_ms_shipped=0)<>4 THROW 51017,'Expected four derived views.',1;
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE delete_referential_action<>0 OR update_referential_action<>0) THROW 51018,'Foreign keys must use NO ACTION.',1;
PRINT 'PASS FullText: 25 tables / 267 columns / 38 FK / 117 CHECK / 18 UNIQUE / 34 indexes / 4 views';
'''
(folder/'KiemTra_FullText.sql').write_text(checks,encoding='utf-8')
master=folder/'CSDL_HoTroDuLichTuTuc.sql';s=master.read_text(encoding='utf-8')
if ':r KiemTra_FullText.sql' not in s:master.write_text(s+'\n:r KiemTra_FullText.sql\n',encoding='utf-8')
print(f'Wrote numbered ALTER script: {len(alter)} column definitions, {len(defaults)} defaults, 155 constraints and 52 indexes.')
