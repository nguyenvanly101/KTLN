// Gan dia diem dang o cap tinh xuong cap quan/huyen hoac xa/phuong, dua vao ten trong dia chi (khop chinh xac, duy nhat trong tinh).
// Chay: node tools/content/assign-units.cjs [--apply]   (khong co --apply thi chi in thong ke va ghi tep SQL)
const fs = require('fs'), path = require('path'), os = require('os'), { execFileSync } = require('child_process');
const DB = process.env.GAN_DB || 'HoTroDuLich';
const run = (q, file) => execFileSync('sqlcmd', ['-I', '-S', '(localdb)\\MSSQLLocalDB', '-d', DB, '-C', '-b', '-y', '0', '-w', '65535', '-f', '65001', ...(file ? ['-i', file] : ['-Q', q])], { encoding: 'utf8', maxBuffer: 1 << 28 });
const json = q => { const f = path.join(os.tmpdir(), 'gan-' + process.pid + '.sql'); fs.writeFileSync(f, 'SET NOCOUNT ON; ' + q + ' FOR JSON PATH;'); const raw = run(null, f); const i = raw.indexOf('['); return i < 0 ? [] : JSON.parse(raw.slice(i, raw.lastIndexOf(']') + 1).replace(/[\r\n]/g, '')); };
const fold = s => s.normalize('NFD').replace(/[̀-ͯ]/g, '').replace(/đ/g, 'd').replace(/Đ/g, 'D').toLowerCase().replace(/^(thanh pho|thi xa|thi tran|quan|huyen|phuong|xa|tp\.?|tx\.?)\s+/, '').replace(/[^a-z0-9 ]/g, ' ').replace(/ +/g, ' ').trim();

const units = json("SELECT Id id, Name name, Level lv, ParentId pid FROM dbo.DonViHanhChinh WHERE Regime=N'Cu' AND Status=N'Active'");
const byId = new Map(units.map(u => [u.id, u]));
const provinceOf = u => { while (u && u.lv > 1) u = byId.get(u.pid); return u; };
// moi tinh: ten da gap hoa -> danh sach don vi (quan/huyen, xa/phuong)
const idx = new Map();
for (const u of units) { if (u.lv < 2) continue; const p = provinceOf(u); if (!p) continue; const k = p.id + '|' + fold(u.name); (idx.get(k) || idx.set(k, []).get(k)).push(u); }
// ten chung chung de bo qua
const SKIP = new Set(['viet nam', 'vietnam']);

const places = json("SELECT d.MaDiaDiem id, d.DiaChi addr, d.DonViId unit FROM dbo.DiaDiemDuLich d JOIN dbo.DonViHanhChinh u ON u.Id=d.DonViId AND u.Level=1 WHERE d.DiaChi IS NOT NULL");
const ups = []; let dist = 0, ward = 0;
for (const p of places) {
  const segs = p.addr.split(',').map(s => s.trim()).filter(Boolean).slice(1, -1);
  let best = null;
  for (const s of segs) {
    const f = fold(s); if (!f || SKIP.has(f)) continue;
    const c = idx.get(p.unit + '|' + f); if (!c || c.length !== 1) continue; // phai duy nhat trong tinh
    const u = c[0];
    if (!best || u.lv > best.lv) { if (best && u.lv === 3 && u.pid !== best.id && byId.get(u.pid) && best.lv === 2) continue; best = u; }
  }
  if (best) { ups.push([p.id, best.id]); best.lv === 2 ? dist++ : ward++; }
}
for (let i=0;i<ups.length;i+=Math.ceil(ups.length/18)){const p=places.find(x=>x.id===ups[i][0]);const u=byId.get(ups[i][1]);console.log('  ',p.addr,'=>',u.name,'(L'+u.lv+', cha:',(byId.get(u.pid)||{}).name+')');}
console.log(`${places.length} dia diem o cap tinh; gan duoc ${ups.length} (quan/huyen ${dist}, xa/phuong ${ward})`);
let o = '-- Gan dia diem xuong cap quan/huyen hoac xa/phuong theo ten trong dia chi. Chi doi dia diem dang o cap tinh.\r\nSET XACT_ABORT ON; BEGIN TRANSACTION;\r\nCREATE TABLE #u(P bigint,U bigint);\r\n';
for (let i = 0; i < ups.length; i += 500) o += 'INSERT #u VALUES ' + ups.slice(i, i + 500).map(x => `(${x[0]},${x[1]})`).join(',') + ';\r\n';
o += `UPDATE d SET d.DonViId=t.U FROM dbo.DiaDiemDuLich d JOIN #u t ON t.P=d.MaDiaDiem JOIN dbo.DonViHanhChinh c ON c.Id=d.DonViId AND c.Level=1;
UPDATE e SET e.DonViId=t.U FROM dbo.DiscoveryEntry e JOIN #u t ON t.P=e.PlaceId JOIN dbo.DonViHanhChinh c ON c.Id=e.DonViId AND c.Level=1;
DROP TABLE #u;
COMMIT TRANSACTION;
`;
const out = path.join(__dirname, '..', '..', 'preview', 'gan-don-vi.sql'); fs.mkdirSync(path.dirname(out), { recursive: true }); fs.writeFileSync(out, '﻿' + o);
if (process.argv.includes('--apply')) { run(null, out); console.log('Da ap dung.'); } else console.log('Tep SQL:', out);
