from pathlib import Path
import json
root=Path('D:/KLTN')
def col(name,typ,nullable=False,rules=''):return dict(name=name,sqlType=typ,nullable='x' if nullable else '',rules=rules,default=None)
tables={
 'LuaChonDaLuu':[col('MaLuaChon','BIGINT IDENTITY',rules='PK'),col('MaTaiKhoan','BIGINT',rules='FK → TaiKhoan'),col('MaDichVu','BIGINT',True,'FK → DichVuDuLich'),col('MaDiaDiem','BIGINT',True,'FK → DiaDiemDuLich'),col('TenMuc','NVARCHAR(200)'),col('ThoiDiemLuu','DATETIME2(0)')],
 'MucChuanBi':[col('MaMuc','BIGINT IDENTITY',rules='PK'),col('MaKeHoach','BIGINT',rules='FK → KeHoachChuyenDi'),col('NoiDung','NVARCHAR(200)'),col('Nhom','VARCHAR(40)'),col('HanHoanThanh','DATE',True),col('DaHoanThanh','BIT'),col('NhacHan','BIT'),col('NgayDaNhac','DATE',True),col('ThoiDiemTao','DATETIME2(0)'),col('RowVersion','ROWVERSION')],
 'KhoanChiChuyenDi':[col('MaKhoanChi','BIGINT IDENTITY',rules='PK'),col('MaKeHoach','BIGINT',rules='FK → KeHoachChuyenDi'),col('NoiDung','NVARCHAR(200)'),col('LoaiChi','VARCHAR(40)'),col('SoTien','DECIMAL(18,2)'),col('NgayChi','DATE'),col('GhiChu','NVARCHAR(500)',True),col('MaLuotGhi','UNIQUEIDENTIFIER'),col('ThoiDiemTao','DATETIME2(0)'),col('RowVersion','ROWVERSION')]
}
extras={table:[col('DiaChiPhucVu','NVARCHAR(300)',True),col('DonViGia','NVARCHAR(50)',True),col('ChinhSachHuy','NVARCHAR(2000)',True),col('AnhJson','NVARCHAR(2000)',True)] for table in ['DichVuDuLich','BanCapNhatDichVu']}
indexes=[
 dict(table='LuaChonDaLuu',name='UX_LuaChon_DichVu',unique=True,keys=[['MaTaiKhoan',False],['MaDichVu',False]],filter='[MaDichVu] IS NOT NULL'),
 dict(table='LuaChonDaLuu',name='UX_LuaChon_DiaDiem',unique=True,keys=[['MaTaiKhoan',False],['MaDiaDiem',False]],filter='[MaDiaDiem] IS NOT NULL'),
 dict(table='LuaChonDaLuu',name='IX_LuaChon_TaiKhoan',keys=[['MaTaiKhoan',False],['ThoiDiemLuu',True]]),
 dict(table='MucChuanBi',name='UX_ChuanBi_NoiDung',unique=True,keys=[['MaKeHoach',False],['NoiDung',False]]),
 dict(table='MucChuanBi',name='IX_ChuanBi_KeHoach',keys=[['MaKeHoach',False],['HanHoanThanh',False]]),
 dict(table='KhoanChiChuyenDi',name='UX_KhoanChi_LuotGhi',unique=True,keys=[['MaKeHoach',False],['MaLuotGhi',False]]),
 dict(table='KhoanChiChuyenDi',name='IX_KhoanChi_KeHoach',keys=[['MaKeHoach',False],['NgayChi',True]])]
profile=dict(version=1,tables=tables,additionalColumns=extras,indexes=indexes,counts=dict(tables=28,columns=302,foreignKeys=44,checks=123,uniqueIndexes=22,queryIndexes=38))
(root/'docs/product-extension-schema.json').write_text(json.dumps(profile,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
p=root/'tools/VerifyFullText/Program.cs';s=p.read_text(encoding='utf-8-sig')
if 'var productExtension=' not in s:
 s=s.replace('var tripExtension=args.Contains("--trip-extension");','var productExtension=args.Contains("--product-extension");\nvar tripExtension=args.Contains("--trip-extension")||productExtension;')
 s=s.replace('entities.Length==25','entities.Length==(productExtension?28:25)').replace('(tripExtension?268:267)','(productExtension?302:tripExtension?268:267)').replace('(tripExtension?39:38)','(productExtension?44:tripExtension?39:38)').replace('==117,"117 CHECK constraints"','==(productExtension?123:117),"declared CHECK constraints"').replace('==18,"18 unique indexes"','==(productExtension?22:18),"declared unique indexes"').replace('(tripExtension?35:34)','(productExtension?38:tripExtension?35:34)')
 start='foreach(var table in contract.RootElement.GetProperty("tables").EnumerateObject())'
 inserted='''if(productExtension){
 using var additions=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"docs/product-extension-schema.json")));
 foreach(var group in new[]{"tables","additionalColumns"})foreach(var table in additions.RootElement.GetProperty(group).EnumerateObject()){
  var entity=entities.SingleOrDefault(x=>x.GetTableName()==table.Name);Check(entity!=null,"declared product entity "+table.Name);if(entity==null)continue;
  foreach(var column in table.Value.EnumerateArray()){
   var name=column.GetProperty("name").GetString()!;var property=entity.FindProperty(name);Check(property!=null&&!property.IsShadowProperty(),table.Name+"."+name+" CLR property");if(property==null)continue;
   Check(property.GetColumnType()?.ToLowerInvariant()==column.GetProperty("sqlType").GetString()!.Replace(" IDENTITY","").ToLowerInvariant(),table.Name+"."+name+" type");
   Check(property.IsNullable==(column.GetProperty("nullable").GetString()=="x"),table.Name+"."+name+" nullability");
  }
 }
}
'''
 s=s.replace(start,inserted+start,1).replace('profile=tripExtension?','profile=productExtension?"FullText + trip + product features":tripExtension?').replace('tripExtension?"tests/trip-contract-results.json"','productExtension?"tests/product-contract-results.json":tripExtension?"tests/trip-contract-results.json"')
 p.write_text(s,encoding='utf-8')
p=root/'tests/fulltext-schema.cjs';s=p.read_text(encoding='utf-8-sig')
if 'const productExtension=' not in s:
 s=s.replace("const tripExtension=process.env.TRAVEL_TRIP_EXTENSION==='1';","const productExtension=process.env.TRAVEL_PRODUCT_EXTENSION==='1';\nconst tripExtension=process.env.TRAVEL_TRIP_EXTENSION==='1'||productExtension;")
 s=s.replace("database==='HoTroDuLich_TripFlow_20261104'","(database==='HoTroDuLich_TripFlow_20261104'||(productExtension&&database==='HoTroDuLich_Product_20261105'))")
 s=s.replace("size===25,'25 tables'","size===(productExtension?28:25),'declared tables'").replace('(tripExtension?268:267)','(productExtension?302:tripExtension?268:267)').replace('schema.checks.length===117','schema.checks.length===(productExtension?123:117)').replace('(tripExtension?39:38)','(productExtension?44:tripExtension?39:38)').replace("length===18,'18 unique indexes'","length===(productExtension?22:18),'declared unique indexes'").replace('(tripExtension?35:34)','(productExtension?38:tripExtension?35:34)')
 marker='const settings=schema.settings;'
 s=s.replace(marker,"if(productExtension){const additions=JSON.parse(fs.readFileSync('docs/product-extension-schema.json','utf8'));Object.assign(contract.tables,additions.tables);for(const [table,columns]of Object.entries(additions.additionalColumns))contract.tables[table].push(...columns);}\n"+marker,1)
 marker='for(const index of contract.queryIndexes){'
 s=s.replace(marker,"if(productExtension){for(const index of JSON.parse(fs.readFileSync('docs/product-extension-schema.json','utf8')).indexes)checkIndex(index);}\n"+marker,1)
 s=s.replace("profile:tripExtension?","profile:productExtension?'FullText + trip + product features':tripExtension?")
 p.write_text(s,encoding='utf-8')
print('Declared product schema and verification profiles saved.')
