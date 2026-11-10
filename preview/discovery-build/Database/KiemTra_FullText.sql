-- FullText PHAN 9: fail deployment if the physical contract is incomplete.
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
