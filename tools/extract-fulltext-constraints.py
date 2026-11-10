"""Extract explicit CHECK and unique-index definitions without editing application code."""
from pathlib import Path
from zipfile import ZipFile
import xml.etree.ElementTree as ET
import json, re

root=Path(__file__).resolve().parents[1]
source=Path(r'C:\Users\vanly\Downloads\26271.3-FullText-XayDungHeThongHoTroKhachDuLichTuTuc (4).docx')
ns={'w':'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
with ZipFile(source) as archive:
    body=list(ET.fromstring(archive.read('word/document.xml')).find('w:body',ns))
def text(element):return ''.join(t.text or '' for t in element.findall('.//w:t',ns)).strip()
def rows_after(caption):
    start=next(i for i,e in enumerate(body) if text(e).startswith(caption))
    table=next(e for e in body[start+1:] if e.tag.endswith('}tbl'))
    return [[text(c) for c in row.findall('w:tc',ns)] for row in table.findall('w:tr',ns)][1:]
checks=[dict(zip(['name','expression','rule'],row)) for row in rows_after('Bảng 4.161.')]
unique=[]
for name,target,where,*_ in rows_after('Bảng 4.162.'):
    match=re.fullmatch(r'(\w+)\s*\(([^)]+)\)',target)
    assert match,target
    unique.append(dict(name=name,table=match[1],columns=[c.strip() for c in match[2].split(',')],filter='' if where in ('—','–','-') else where))
assert len(checks)==41 and len(unique)==18
(root/'docs/fulltext-constraints.json').write_text(json.dumps(dict(checks=checks,uniqueIndexes=unique),ensure_ascii=False,indent=2),encoding='utf-8')
print('Extracted 41 explicit CHECK expressions and 18 unique-index definitions from FullText.')
