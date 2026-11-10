from pathlib import Path
import re,json,collections
root=Path(__file__).resolve().parents[1]/'src/HoTroDuLich.Web'
meta=json.loads((root.parents[1]/'domain-metadata.json').read_text(encoding='utf-8'))
types={e['name']:{p['name']:p['type'] for p in e['props']} for e in meta['entities']}
relations=[]
pattern=r'e\.HasOne<(\w+)>\(\)\.WithMany\(\)\.HasForeignKey\(x => x\.(\w+)\)'
for p in (root/'Data/Configurations').glob('*.cs'):
 child=p.stem.removesuffix('Configuration')
 for parent,fk in re.findall(pattern,p.read_text(encoding='utf-8')):relations.append((child,parent,fk,p))
assert len(relations)==38,len(relations)
counts=collections.Counter((child,parent) for child,parent,_,_ in relations)
props=collections.defaultdict(list)
for child,parent,fk,p in relations:
 nav=parent if counts[(child,parent)]==1 else fk.removeprefix('Ma')
 collection=child if counts[(child,parent)]==1 else child+'_'+nav
 optional=types[child][fk].endswith('?')
 props[child].append(f' public {parent}{"?" if optional else ""} {nav} {{get;set;}}'+('' if optional else '=null!;'))
 props[parent].append(f' public ICollection<{child}> {collection} {{get;set;}}=new List<{child}>();')
 s=p.read_text(encoding='utf-8');old=f'e.HasOne<{parent}>().WithMany().HasForeignKey(x => x.{fk})';new=f'e.HasOne(x=>x.{nav}).WithMany(x=>x.{collection}).HasForeignKey(x => x.{fk})'
 assert old in s,old;s=s.replace(old,new);p.write_text(s,encoding='utf-8')
p=root/'Domain/Entities.cs';s=p.read_text(encoding='utf-8');s=re.sub(r'public class (\w+)','public partial class \\1',s);p.write_text(s,encoding='utf-8')
(root/'Domain/Relationships.cs').write_text('namespace HoTroDuLich.Web.Domain;\n'+'\n'.join('public partial class '+name+' {\n'+'\n'.join(fields)+'\n}' for name,fields in props.items()),encoding='utf-8')
print('Mapped 38 relationships without shadow foreign keys.')
