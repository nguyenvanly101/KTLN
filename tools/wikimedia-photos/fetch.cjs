// Tim anh tu do (Wikimedia Commons) cho tinh/thanh va muc tra cuu, ra thu muc tam de duyet truoc khi nhap.
// Chay: node tools/wikimedia-photos/fetch.cjs <thu-muc-ra> [--limit=N] [--db=HoTroDuLich]
// - Dia diem co toa do: tim tren OpenTripMap trong ban kinh 400 m, ten phai khop; lay anh Commons tu chi tiet OpenTripMap.
//   Khong khop thi thu bai Wikipedia tieng Viet trung ten, bai phai co toa do cach muc <= 3 km.
// - Tinh/thanh: bai Wikipedia tieng Viet trung ten, noi dung nhac Viet Nam.
// - Chi nhan anh tren Commons voi giay phep CC0 / Public domain / CC BY / CC BY-SA; ghi tac gia, giay phep, trang nguon.
// Chi DOC co so du lieu. Khoa OpenTripMap doc tu kho bi mat cua du an (Discovery:OpenTripMap:Key), khong in ra.
const fs = require('fs'), path = require('path'), os = require('os'), { execSync } = require('child_process');
const args = process.argv.slice(2); const out = args.find(a => !a.startsWith('--'));
if (!out) { console.error('Thieu thu muc ra'); process.exit(1); }
const opt = (k, d) => { const a = args.find(x => x.startsWith('--' + k + '=')); return a ? a.slice(k.length + 3) : d; };
const LIMIT = +opt('limit', '0') || Infinity, DB = opt('db', 'HoTroDuLich');
fs.mkdirSync(path.join(out, 'anh'), { recursive: true });
const secrets = execSync('dotnet user-secrets list --project src/HoTroDuLich.Web', { encoding: 'utf8' });
const OTM = (/Discovery:OpenTripMap:Key = (\S+)/.exec(secrets) || [])[1];
if (!OTM) console.warn('Khong co khoa OpenTripMap: chi dung Wikipedia theo ten.');
const sql = q => {
  const f = path.join(os.tmpdir(), 'wiki-q-' + process.pid + '.sql');
  fs.writeFileSync(f, 'SET NOCOUNT ON; ' + q + ' FOR JSON PATH;');
  const raw = execSync('sqlcmd -I -S "(localdb)' + String.fromCharCode(92) + 'MSSQLLocalDB" -d ' + DB + ' -C -b -y 0 -w 65535 -f 65001 -i "' + f + '"', { encoding: 'utf8', maxBuffer: 1 << 26 });
  const i = raw.indexOf('[');
  return i < 0 ? [] : JSON.parse(raw.slice(i, raw.lastIndexOf(']') + 1).split('\r').join('').split('\n').join(''));
};

const UA = { 'User-Agent': 'HanhTrinh-KLTN/1.0 (local thesis project; Node fetch)' };
const cacheFile = path.join(out, 'cache.json');
const cache = fs.existsSync(cacheFile) ? JSON.parse(fs.readFileSync(cacheFile, 'utf8')) : {};
const save = () => fs.writeFileSync(cacheFile, JSON.stringify(cache));
const sleep = ms => new Promise(r => setTimeout(r, ms));
let calls = 0;
async function getJson(url, key, wait) {
  const k = key || url; if (k in cache) return cache[k];
  for (let n = 0; n < 3; n++) {
    try {
      calls++; await sleep(wait);
      const r = await fetch(url, { headers: UA });
      if (r.status === 404) { cache[k] = null; return null; }
      if (r.status === 429 || r.status >= 500) { await sleep(3000 * (n + 1)); continue; }
      const j = await r.json(); cache[k] = j; if (calls % 25 === 0) save(); return j;
    } catch { await sleep(2000); }
  }
  return null;
}
const wikiSummary = title => getJson('https://vi.wikipedia.org/api/rest_v1/page/summary/' + encodeURIComponent(title.split(' ').join('_')), null, 700);
const otm = (p, cacheKey) => OTM ? getJson('https://api.opentripmap.com/0.1/en/places/' + p + (p.includes('?') ? '&' : '?') + 'apikey=' + OTM, 'otm:' + cacheKey, 250) : null;

const fold = s => (s || '').normalize('NFD').replace(/[̀-ͯ]/g, '').replace(/[đĐ]/g, 'd').toLowerCase().replace(/[^a-z0-9 ]+/g, ' ').replace(/\s+/g, ' ').trim();
const sameName = (a, b) => { const x = fold(a), y = fold(b); if (x.length < 4 || y.length < 4) return x === y && x.length > 0; return x === y || x.includes(y) || y.includes(x); };
const km = (a, b, c, d) => { const R = 6371, t = Math.PI / 180, x = (c - a) * t, y = (d - b) * t, h = Math.sin(x / 2) ** 2 + Math.cos(a * t) * Math.cos(c * t) * Math.sin(y / 2) ** 2; return 2 * R * Math.asin(Math.sqrt(h)); };

// Ten tep Commons tu link anh: trang File:..., hoac upload.wikimedia.org/wikipedia/commons/[thumb/]a/ab/Ten.jpg[/..px-Ten.jpg]
function commonsFile(url) {
  if (!url) return null; url = url.split('?')[0].split('#')[0];
  let m = /commons\.wikimedia\.org\/wiki\/File:(.+)$/.exec(url); if (m) return decodeURIComponent(m[1]).split('_').join(' ');
  m = /(?:upload|thumb)\.wikimedia\.org\/wikipedia\/commons\/(.+)$/.exec(url); if (!m) return null; // anh luu rieng tren vi.wikipedia (thuong khong tu do): bo
  const parts = m[1].split('/'); const name = parts[0] === 'thumb' ? parts[3] : parts[2];
  return name ? decodeURIComponent(name).split('_').join(' ') : null;
}
const FREE = /^(cc0|cc[- ]?zero|public domain|pd\b|pd-|cc[- ]by(-sa)?[- ]\d)/i;
const strip = h => (h || '').replace(/<[^>]+>/g, ' ').replace(/&amp;/g, '&').replace(/&quot;/g, '"').replace(/&#0?39;/g, "'").replace(/\s+/g, ' ').trim();
async function license(file) {
  const j = await getJson('https://commons.wikimedia.org/w/api.php?action=query&format=json&prop=imageinfo&iiprop=url|extmetadata|mime&iiurlwidth=1280&titles=' + encodeURIComponent('File:' + file), null, 700);
  const page = j && j.query && Object.values(j.query.pages || {})[0]; const info = page && page.imageinfo && page.imageinfo[0];
  if (!info) return { reason: 'Không đọc được tệp trên Commons' };
  const md = info.extmetadata || {}; const lic = strip(md.LicenseShortName && md.LicenseShortName.value);
  if (!FREE.test(lic) || /\bnc\b|\bnd\b|non-?commercial|no ?deriv/i.test(lic)) return { reason: 'Giấy phép không phù hợp: ' + (lic || 'không rõ') };
  if (!/^image\/(jpeg|png|webp|tiff)$/.test(info.mime || '')) return { reason: 'Không phải ảnh chụp (' + info.mime + ')' };
  const artist = strip(md.Artist && md.Artist.value).slice(0, 120) || 'Không rõ tác giả';
  return { thumb: info.thumburl || info.url, page: info.descriptionurl, license: lic, artist };
}
async function download(url, name) {
  const dest = path.join(out, 'anh', name); if (fs.existsSync(dest)) return name;
  for (let n = 0; n < 3; n++) {
    try { await sleep(700); const r = await fetch(url, { headers: UA }); if (!r.ok) { await sleep(3000); continue; } fs.writeFileSync(dest, Buffer.from(await r.arrayBuffer())); return name; } catch { await sleep(2000); }
  }
  return null;
}

// Tim anh cho mot dia diem co toa do qua OpenTripMap
async function viaOtm(t) {
  if (t.Lat == null || t.Lng == null) return null;
  const list = await otm('radius?radius=400&limit=30&format=json&lon=' + t.Lng + '&lat=' + t.Lat, 'r:' + t.Lat + ',' + t.Lng);
  if (!Array.isArray(list)) return null;
  const hit = list.filter(p => p.name && sameName(p.name, t.Title)).sort((a, b) => a.dist - b.dist)[0];
  if (!hit) return null;
  const d = await otm('xid/' + hit.xid, 'x:' + hit.xid);
  if (!d || !d.image) return null;
  return { file: commonsFile(d.image), how: 'OpenTripMap (' + hit.name + ', cách ' + Math.round(hit.dist) + ' m)', distKm: hit.dist / 1000, wiki: d.wikipedia || null };
}
// Bai Wikipedia trung ten; dia diem co toa do thi bai phai o gan
async function viaWiki(t, titles, needVn) {
  for (const title of titles) {
    const s = await wikiSummary(title);
    if (!s || s.type !== 'standard' || !s.originalimage) continue;
    let dist = null;
    if (t.Lat != null && t.Lng != null) { if (!s.coordinates) continue; dist = km(+t.Lat, +t.Lng, s.coordinates.lat, s.coordinates.lon); if (dist > 3) continue; }
    else if (needVn && !/Việt Nam/.test((s.extract || '') + ' ' + (s.description || ''))) continue;
    return { file: commonsFile(s.originalimage.source), how: 'Wikipedia "' + s.title + '"' + (dist != null ? ', cách ' + dist.toFixed(1) + ' km' : ''), distKm: dist, wiki: s.content_urls && s.content_urls.desktop && s.content_urls.desktop.page };
  }
  return null;
}

(async () => {
  // Bang AnhDonVi chi co sau khi website chay script 035; truoc do coi nhu chua tinh nao co anh.
  const hasTable = sql("SELECT CASE WHEN OBJECT_ID(N'dbo.AnhDonVi',N'U') IS NULL THEN 0 ELSE 1 END Has")[0].Has === 1;
  const units = sql("SELECT d.Id, d.Name, d.FullName FROM dbo.DonViHanhChinh d WHERE d.[Level]=1 AND d.Status='Active'" + (hasTable ? " AND NOT EXISTS (SELECT 1 FROM dbo.AnhDonVi a WHERE a.DonViId=d.Id)" : "") + " ORDER BY d.Id");
  const entries = sql("SELECT e.Id, e.Kind, e.Title, e.Region, CAST(e.Latitude AS float) Lat, CAST(e.Longitude AS float) Lng FROM dbo.DiscoveryEntry e WHERE e.Status='Published' AND e.Kind IN ('Destination','Area','Place') AND e.ImagePath IS NULL ORDER BY CASE e.Kind WHEN 'Destination' THEN 0 WHEN 'Area' THEN 1 ELSE 2 END, e.Id");
  const targets = [...units.map(u => ({ ...u, target: 'unit', Title: u.Name, Lat: null, Lng: null })), ...entries.map(e => ({ ...e, target: 'entry' }))].slice(0, LIMIT);
  console.log('Tim anh cho ' + targets.length + ' muc (' + units.length + ' tinh/thanh, ' + entries.length + ' muc tra cuu).');
  const kept = [], rejected = []; const seenFile = new Map();
  for (const [i, t] of targets.entries()) {
    try {
      let m = null;
      if (t.target === 'unit') m = await viaWiki(t, [t.Name, t.FullName], true);
      else { m = await viaOtm(t); if (!m || !m.file) m = await viaWiki(t, [t.Title], t.Kind !== 'Place'); }
      if (!m) { rejected.push({ ...t, reason: 'Không tìm thấy bài/ảnh khớp' }); continue; }
      if (!m.file) { rejected.push({ ...t, how: m.how, reason: 'Ảnh không nằm trên Wikimedia Commons' }); continue; }
      const lic = await license(m.file);
      if (lic.reason) { rejected.push({ ...t, how: m.how, file: m.file, reason: lic.reason }); continue; }
      const ext = (/\.(jpe?g|png|webp)(\?|$)/i.exec(lic.thumb) || [, 'jpg'])[1].toLowerCase().replace('jpeg', 'jpg');
      const name = (t.target === 'unit' ? 'tinh-' : 'muc-') + t.Id + '.' + ext;
      if (!await download(lic.thumb, name)) { rejected.push({ ...t, how: m.how, reason: 'Tải ảnh thất bại' }); continue; }
      const credit = ('Ảnh: ' + lic.artist + ' · ' + lic.license + ' · Wikimedia Commons').slice(0, 300);
      const row = { target: t.target, id: t.Id, kind: t.Kind || 'Tỉnh/thành', title: t.Title, region: t.Region || '', how: m.how, file: m.file, image: name, credit, sourceUrl: lic.page, wiki: m.wiki };
      if (seenFile.has(m.file)) row.duplicateOf = seenFile.get(m.file); else seenFile.set(m.file, t.target + '-' + t.Id);
      kept.push(row);
    } finally {
      if ((i + 1) % 20 === 0) { save(); console.log((i + 1) + '/' + targets.length + ' · có ảnh ' + kept.length + ' · gọi mạng ' + calls); }
    }
  }
  save();
  fs.writeFileSync(path.join(out, 'staging.json'), JSON.stringify(kept, null, 1));
  fs.writeFileSync(path.join(out, 'rejected.json'), JSON.stringify(rejected, null, 1));
  const esc = s => String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const card = r => `<figure><img src="anh/${esc(r.image)}" loading="lazy" alt=""><figcaption><b>${r.target === 'unit' ? 'T' : 'M'}${r.id} · ${esc(r.title)}</b><span>${esc(r.kind)}${r.region ? ' · ' + esc(r.region) : ''}</span><span>${esc(r.how)}</span>${r.duplicateOf ? `<span class="dup">Trùng ảnh với ${esc(r.duplicateOf)}</span>` : ''}<a href="${esc(r.sourceUrl)}" target="_blank" rel="noopener">${esc(r.credit)}</a></figcaption></figure>`;
  fs.writeFileSync(path.join(out, 'xem-truoc.html'), `<!doctype html><meta charset="utf-8"><title>Ảnh Wikimedia chờ duyệt</title><style>body{font:14px/1.5 "Segoe UI",sans-serif;margin:24px;color:#15213e;background:#f5f7fc}h1{margin:0 0 4px}p{color:#5c667b}.g{display:grid;grid-template-columns:repeat(auto-fill,minmax(240px,1fr));gap:14px}figure{margin:0;background:#fff;border:1px solid #dde3ef;border-radius:12px;overflow:hidden}img{width:100%;height:170px;object-fit:cover;display:block}figcaption{padding:10px;display:grid;gap:3px;font-size:12px}figcaption b{font-size:13px}figcaption span{color:#5c667b}.dup{color:#b3362f!important}a{color:#2447e8;word-break:break-word}</style><h1>Ảnh Wikimedia chờ duyệt</h1><p>${kept.length} ảnh tìm được, ${rejected.length} mục không có ảnh phù hợp. Mã T = tỉnh/thành, M = mục tra cứu. Ghi lại mã của ảnh khớp sai để loại khi nhập.</p><div class="g">${kept.map(card).join('')}</div>`);
  console.log('Xong: ' + kept.length + ' anh, ' + rejected.length + ' bo qua, ' + calls + ' lan goi mang. Xem ' + path.join(out, 'xem-truoc.html'));
})().catch(e => { save(); console.error(e); process.exit(1); });
