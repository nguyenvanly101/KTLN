// Ban do Kham pha: hien moi vi tri (khong chi quanh mot diem), danh sach "quanh day" canh ban do cuon trong man hinh, bam ghim thi dua len dau danh sach. Chay mot lan.
const fs = require('fs');
const root = 'src/HoTroDuLich.Web/';
const patch = (file, fn) => { const f = root + file; let s = fs.readFileSync(f, 'utf8'); const crlf = s.includes('\r\n'); s = s.replace(/\r\n/g, '\n'); const b = s; s = fn(s); if (s === b) throw Error('Khong doi: ' + file); if (crlf) s = s.replace(/\n/g, '\r\n'); fs.writeFileSync(f, s); };
const rep = (s, a, b) => { if (!s.includes(a)) throw Error('Khong tim thay: ' + a.slice(0, 80)); return s.replace(a, b); };
const line = (s, prefix, text) => { const rows = s.split('\n'); const i = rows.findIndex(r => r.startsWith(prefix)); if (i < 0) throw Error('Khong thay dong: ' + prefix); rows[i] = text; return rows.join('\n'); };

patch('Services/DiscoveryService.cs', s => rep(s, 'Take(map?300:20)', 'Take(map?1500:20)'));
patch('Controllers/DiscoveryApiController.cs', s => rep(s, 'total=p.Total,limit=300}', 'total=p.Total,limit=1500}'));

patch('Views/Discovery/Search.cshtml', s => rep(s, '<div class="ds-results-grid"><div><div class="ds-card-grid">',
  '<div class="ds-results-grid"><div data-ds-list><div class="ds-near" data-ds-near hidden><div class="ds-near-head"><strong data-ds-near-title></strong><button type="button" class="ds-quiet" data-ds-near-close>Quay lại kết quả tìm</button></div><div class="ds-near-list" data-ds-near-list></div></div><div class="ds-card-grid">'));
patch('Views/Discovery/Search.cshtml', s => rep(s, '<p>Chọn ghim để xem thông tin; bấm trên bản đồ để đặt mốc tìm quanh đây.</p>', '<p>Bản đồ hiện mọi nơi phù hợp. Bấm một ghim để đưa nơi đó lên đầu danh sách; bấm vào bản đồ để xem các nơi quanh điểm đó.</p>'));

patch('wwwroot/js/discovery.js', s => {
  s = line(s, ' function nearby(lat,lng){', ` let nearPoint=null,pickedId=null;
 const listCol=()=>$('[data-ds-list]');
 const kmBetween=(a,b,c,d)=>{const R=6371,t=Math.PI/180,x=(c-a)*t,y=(d-b)*t,h=Math.sin(x/2)**2+Math.cos(a*t)*Math.cos(c*t)*Math.sin(y/2)**2;return 2*R*Math.asin(Math.sqrt(h));};
 const fmtKm=v=>v<1?Math.round(v*1000)+' m':(Math.round(v*10)/10).toLocaleString('vi-VN')+' km';
 function showOnMap(item){if(!map)return;map.setView([item.lat,item.lng],Math.max(map.getZoom(),16),{animate:false});drawMarkers();markers.find(x=>x.id===item.id)?.marker.openPopup();}
 function nearRow(item,dist){const row=node('article',null,'ds-near-item');row.dataset.nearId=item.id;const a=node('a',item.title);a.href=item.url;const h=node('h3');h.append(a);row.append(h,node('p',(item.kindLabel||item.kind||'')+(item.region?' · '+item.region:'')+(dist!=null?' · cách '+fmtKm(dist):'')));if(item.summary)row.append(node('p',item.summary.length>140?item.summary.slice(0,139)+'…':item.summary,'ds-near-sum'));const b=node('button','Xem trên bản đồ','ds-quiet');b.type='button';b.addEventListener('click',()=>showOnMap(item));row.append(b);return row;}
 function drawNearCircle(){if(!nearLayer)return;nearLayer.clearLayers();if(nearPoint)L.circle([nearPoint.lat,nearPoint.lng],{radius:Number($('#ds-radius')?.value||5)*1000,color:'#2447e8',weight:1,fillOpacity:.06}).addTo(nearLayer);}
 function renderNear(){const box=$('[data-ds-near]'),col=listCol();if(!box||!col)return;if(!nearPoint){box.hidden=true;col.classList.remove('near-on');return;}const radius=Number($('#ds-radius')?.value||5);const rows=mapItems.filter(i=>i.lat!=null&&i.lng!=null).map(i=>({i,d:kmBetween(nearPoint.lat,nearPoint.lng,Number(i.lat),Number(i.lng))})).filter(x=>x.d<=radius||x.i.id===pickedId).sort((a,b)=>a.d-b.d);if(pickedId!=null){const n=rows.findIndex(x=>x.i.id===pickedId);if(n>0)rows.unshift(rows.splice(n,1)[0]);}$('[data-ds-near-title]').textContent=rows.length?\`\${rows.length} nơi trong \${radius} km quanh điểm đã chọn, gần nhất trước\`:\`Chưa có nơi nào trong \${radius} km quanh điểm đã chọn\`;const list=$('[data-ds-near-list]');list.replaceChildren(...rows.slice(0,200).map(x=>{const r=nearRow(x.i,x.d);if(x.i.id===pickedId)r.classList.add('is-picked');return r;}));if(!rows.length)list.append(node('p','Thử tăng phạm vi (km) trong Bộ lọc hoặc bấm một điểm khác trên bản đồ.','ds-note'));box.hidden=false;col.classList.add('near-on');col.scrollTop=0;}
 function pick(item){pickedId=item.id;const col=listCol();if(!col)return;if(nearPoint){renderNear();return;}const grid=$('.ds-card-grid',col);if(!grid)return;$$('.is-picked',grid).forEach(e=>e.classList.remove('is-picked'));$$('.ds-near-item',grid).forEach(e=>e.remove());let card=$('[data-entry-card="'+item.id+'"]',grid);if(!card)card=nearRow(item,null);grid.prepend(card);card.classList.add('is-picked');col.scrollTop=0;}
 function nearby(lat,lng){if(!searchForm)return;$('#ds-lat').value=Number(lat).toFixed(6);$('#ds-lng').value=Number(lng).toFixed(6);for(const k of ['South','West','North','East']){const field=$(\`[name=\${k}]\`,searchForm);if(field)field.value='';}nearPoint={lat:Number(lat),lng:Number(lng)};pickedId=null;drawNearCircle();renderNear();const status=$('[data-ds-location-status]');if(status)status.textContent='Đã chọn mốc. Danh sách cạnh bản đồ đang hiện các nơi quanh điểm này, gần nhất trước.';}`);
  s = line(s, ' async function loadMap(bounds=false){', ` async function loadMap(){if(!map)return;const status=$('[data-ds-map-status]');try{const params=new URLSearchParams(location.search);const lat=Number(params.get('Lat')??params.get('lat')),lng=Number(params.get('Lng')??params.get('lng'));const hadPoint=(params.has('Lat')||params.has('lat'))&&Number.isFinite(lat)&&Number.isFinite(lng);for(const k of ['Lat','Lng','lat','lng','RadiusKm','radiusKm','South','North','West','East','south','north','west','east','Page','page'])params.delete(k);if((params.get('Sort')||params.get('sort'))==='distance'){params.delete('Sort');params.delete('sort');}const data=await json('/api/discovery/map?'+params);mapItems=data.items;drawMarkers();if(hadPoint){nearPoint={lat,lng};drawNearCircle();map.setView([lat,lng],13,{animate:false});renderNear();}else if(mapItems.length){const points=mapItems.map(i=>[Number(i.lat),Number(i.lng)]);map.fitBounds(L.latLngBounds(points),{padding:[35,35],maxZoom:14,animate:false});}if(status)status.textContent=\`\${mapItems.length} vị trí đang hiển thị trên bản đồ\${data.total>1500?' (tối đa 1.500 mục; thêm điều kiện để thu hẹp)':''}. Các kết quả chưa có tọa độ vẫn xem được trong danh sách.\`;}catch(e){if(status)status.textContent=e.message;}}
 $('[data-ds-near-close]')?.addEventListener('click',()=>{nearPoint=null;pickedId=null;drawNearCircle();renderNear();for(const id of ['#ds-lat','#ds-lng']){const f=$(id);if(f)f.value='';}});
 $('#ds-radius')?.addEventListener('change',()=>{drawNearCircle();renderNear();});`);
  s = rep(s, "title:first.title}).bindPopup(popup(first)).addTo(markerLayer);", "title:first.title}).bindPopup(popup(first)).on('click',()=>pick(first)).addTo(markerLayer);");
  return s;
});

patch('wwwroot/css/discovery.css', s => s + `
/* Danh sach canh ban do: cuon trong man hinh, ban do dung yen */
@media(min-width:761px){
 .ds-results-grid:not(:has(#discovery-map-area[hidden]))>[data-ds-list]{max-height:calc(100vh - 96px);overflow-y:auto;overscroll-behavior:contain;padding:2px 8px 2px 2px;scrollbar-gutter:stable}
 .ds-results-grid>.ds-map-area{position:sticky;top:84px;max-height:calc(100vh - 96px);overflow-y:auto}
 .ds-results-grid .ds-map{height:clamp(300px,calc(100vh - 330px),620px)}
}
.near-on>.ds-card-grid,.near-on>.ds-pagination,.near-on>.ds-empty,.near-on>.ds-note{display:none}
.ds-near-head{display:flex;flex-wrap:wrap;gap:10px;align-items:center;justify-content:space-between;margin:0 0 12px;padding:12px 14px;border:1px solid var(--ds-line);border-radius:14px;background:var(--ds-soft)}
.ds-near-list{display:grid;gap:10px}
.ds-near-item{border:1px solid var(--ds-line);border-radius:14px;padding:12px 14px;background:#fff;min-width:0}
.ds-near-item h3{font-size:1rem;margin:0 0 4px}.ds-near-item p{margin:0 0 4px;font-size:.86rem;color:var(--ds-muted)}.ds-near-sum{color:var(--ds-ink)!important}
.ds-card.is-picked,.ds-near-item.is-picked{border-color:var(--ds-blue);box-shadow:0 0 0 3px rgba(36,71,232,.16)}
`);
console.log('ok');
