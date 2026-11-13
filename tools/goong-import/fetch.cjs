// Lay dia diem tu Goong REST API (khoa REST cua chu website) ra tep tam de kiem tra truoc khi nhap vao co so du lieu.
// Chay: node tools/goong-import/fetch.cjs <thu-muc-ra>
// Khoa doc tu kho bi mat cua du an (Discovery:Goong:RestKey), khong in ra man hinh.
const fs = require('fs'), path = require('path'), { execSync } = require('child_process');
const out = process.argv[2]; if (!out) { console.error('Thieu thu muc ra'); process.exit(1); }
fs.mkdirSync(out, { recursive: true });
const secrets = execSync('dotnet user-secrets list --project src/HoTroDuLich.Web', { encoding: 'utf8' });
const KEY = (/Discovery:Goong:RestKey = (\S+)/.exec(secrets) || [])[1]; if (!KEY) { console.error('Khong co khoa REST'); process.exit(1); }
const sql = q => {
  const f = path.join(require('os').tmpdir(), 'goong-q-' + process.pid + '.sql');
  fs.writeFileSync(f, 'SET NOCOUNT ON; ' + q + ' FOR JSON PATH;');
  const raw = execSync('sqlcmd -I -S "(localdb)' + String.fromCharCode(92) + 'MSSQLLocalDB" -d HoTroDuLich -C -b -y 0 -w 65535 -f 65001 -i "' + f + '"', { encoding: 'utf8', maxBuffer: 1 << 26 });
  const i = raw.indexOf('[');
  return i < 0 ? [] : JSON.parse(raw.slice(i, raw.lastIndexOf(']') + 1).split(String.fromCharCode(13)).join('').split(String.fromCharCode(10)).join(''));
};

const cache = fs.existsSync(path.join(out, 'cache.json')) ? JSON.parse(fs.readFileSync(path.join(out, 'cache.json'), 'utf8')) : {};
let calls = 0, hits = 0, limited = false; const save = () => fs.writeFileSync(path.join(out, 'cache.json'), JSON.stringify(cache));
const sleep = ms => new Promise(r => setTimeout(r, ms));
async function api(p) {
  if (cache[p]) { hits++; return cache[p]; }
  if (limited) return null;
  for (let n = 0; n < 3; n++) {
    try { calls++; const r = await fetch('https://rsapi.goong.io/' + p + (p.includes('?') ? '&' : '?') + 'api_key=' + KEY); if (r.status === 429) { await sleep(1200); if (n === 2) limited = true; continue; } if (!r.ok) return null; const j = await r.json(); cache[p] = j; return j; } catch { await sleep(500); }
  }
  return null;
}
const km = (a, b, c, d) => { const R = 6371, t = Math.PI / 180, x = (c - a) * t, y = (d - b) * t, h = Math.sin(x / 2) ** 2 + Math.cos(a * t) * Math.cos(c * t) * Math.sin(y / 2) ** 2; return 2 * R * Math.asin(Math.sqrt(h)); };

// diem den: 63 tinh (he cu) + cac khu vuc da co trong du lieu
const provinces = sql("SELECT Id id, Name name FROM dbo.DonViHanhChinh WHERE Regime=N'Cu' AND Level=1 AND Status=N'Active'");
const regions = sql("SELECT KhuVuc name, MAX(DonViId) id FROM dbo.DiaDiemDuLich GROUP BY KhuVuc");
const dests = [];
for (const p of provinces) dests.push({ name: p.name, id: p.id, kind: 'province' });
for (const r of regions) if (!dests.some(d => d.name === r.name)) dests.push({ name: r.name, id: r.id, kind: 'region' });

// loai tim kiem: [tu khoa, loai dia diem, so luong toi da, ap dung cho]
const CATS = [
  ['khu du lịch', 'Thiên nhiên', 4, 'all'], ['điểm tham quan', 'Văn hóa', 4, 'all'],
  ['bãi biển', 'Biển đảo', 3, 'all'], ['thác nước', 'Thiên nhiên', 3, 'all'], ['chùa', 'Tâm linh', 3, 'all'],
  ['bảo tàng', 'Văn hóa', 3, 'all'], ['di tích lịch sử', 'Lịch sử', 3, 'all'], ['chợ', 'Văn hóa', 2, 'region'],
  ['nhà hàng đặc sản', 'Ăn uống', 3, 'region'], ['khách sạn', 'Lưu trú', 3, 'region'],
];
const existing = sql("SELECT d.TenDiaDiem name, e.Latitude lat, e.Longitude lng FROM dbo.DiaDiemDuLich d LEFT JOIN dbo.DiscoveryEntry e ON e.PlaceId=d.MaDiaDiem");

(async () => {
  const found = new Map(); let n = 0;
  for (const d of dests) {
    if (limited) { console.log('Het han muc yeu cau trong ngay cua Goong; dung o diem den:', d.name, '(chay lai vao ngay mai, ket qua da lay duoc giu trong cache)'); break; }
    const g = await api('v2/geocode?address=' + encodeURIComponent(d.name));
    if (limited) { console.log('Het han muc yeu cau trong ngay cua Goong; dung o diem den:', d.name, '(chay lai vao ngay mai, ket qua da lay duoc giu trong cache)'); break; }
    const loc = g && g.results && g.results[0] && g.results[0].geometry && g.results[0].geometry.location; if (!loc) { console.log('khong tim thay tam:', d.name); continue; }
    d.lat = loc.lat; d.lng = loc.lng;
    for (const [kw, type, cap, scope] of CATS) {
      if (scope === 'region' && d.kind !== 'region') continue;
      const ac = await api('v2/place/autocomplete?input=' + encodeURIComponent(kw + ' ' + d.name) + '&location=' + loc.lat + ',' + loc.lng + '&radius=30');
      const preds = ((ac && ac.predictions) || []).filter(p => p.place_id).slice(0, cap + 3);
      let kept = 0;
      for (const p of preds) {
        if (kept >= cap) break;
        const dt = await api('v2/place/detail?place_id=' + encodeURIComponent(p.place_id)); const r = dt && dt.result; if (!r || !r.geometry) continue;
        const { lat, lng } = r.geometry.location; if (km(loc.lat, loc.lng, lat, lng) > 35) continue;
        const name = (r.name || '').trim(); if (name.length < 3) continue;
        if (existing.some(e => e.lat != null && km(e.lat, e.lng, lat, lng) < 0.25 && (e.name.toLowerCase().includes(name.toLowerCase().slice(0, 8)) || name.toLowerCase().includes(e.name.toLowerCase().slice(0, 8))))) continue;
        if (found.has(r.place_id)) continue;
        found.set(r.place_id, { place_id: r.place_id, name, address: r.formatted_address || '', lat, lng, type, dest: d.name, destId: d.id, destKind: d.kind, query: kw });
        kept++;
      }
    }
    if (++n % 10 === 0) { save(); console.log(`${n}/${dests.length} diem den, ${found.size} dia diem, ${calls} lan goi moi`); }
  }
  save();
  const list = [...found.values()];
  fs.writeFileSync(path.join(out, 'staging.json'), JSON.stringify(list, null, 1));
  const by = {}; list.forEach(x => by[x.type] = (by[x.type] || 0) + 1);
  console.log('XONG', list.length, 'dia diem;', calls, 'lan goi API moi,', hits, 'lay tu bo nho tam;', JSON.stringify(by));
})().catch(e => { console.error(e); process.exit(1); });
