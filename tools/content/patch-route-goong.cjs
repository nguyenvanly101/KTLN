// Tim dich vu doc tuyen: tinh tuyen bang Goong; cach dung moi: o "Di tu" / "Den" co goi y Goong, nut 📍 chon tren ban do,
// them diem ghe, chon phuong tien bang nut, tuy chon nang cao gap lai.
const fs = require('fs'), path = require('path'); const root = path.join(__dirname, '../../src/HoTroDuLich.Web');
const rd = f => fs.readFileSync(path.join(root, f), 'utf8').split('\r\n').join('\n'), wr = (f, s) => fs.writeFileSync(path.join(root, f), s);
const swap = (s, a, b) => { if (!s.includes(a)) { if (s.includes(b)) return s; throw Error('khong thay: ' + a.slice(0, 90)); } return s.replace(a, () => b); };

// ---------- Markup
let v = rd('Views/Discovery/Search.cshtml');
const p0 = v.indexOf('<details class="ds-route-panel">'), p1 = v.indexOf('</details>', p0) + '</details>'.length;
if (p0 < 0) throw Error('khong thay panel');
v = v.slice(0, p0) + `<details class="ds-route-panel"><summary>@Ui.Icon("car") Tìm dịch vụ dọc tuyến</summary>
 <p class="help">Nhập nơi đi và nơi đến (hoặc bấm 📍 rồi chọn trên bản đồ). Hệ thống vẽ đường đi bằng Goong và gợi ý chỗ ghé dọc đường.</p>
 <div class="rt-stops" data-rt-stops></div>
 <div class="rt-tools"><button type="button" class="ds-quiet" data-rt-add>+ Thêm điểm ghé</button><button type="button" class="ds-quiet" data-ds-route-clear>Xóa tuyến</button></div>
 <div class="rt-modes" role="radiogroup" aria-label="Phương tiện"><button type="button" role="radio" aria-checked="true" data-rt-mode="car">🚗 Ô tô</button><button type="button" role="radio" aria-checked="false" data-rt-mode="bike">🛵 Xe máy</button><button type="button" role="radio" aria-checked="false" data-rt-mode="foot">🚶 Đi bộ</button></div>
 <select id="ds-mode" hidden aria-hidden="true"><option value="car">Ô tô</option><option value="bike">Xe máy</option><option value="foot">Đi bộ</option></select>
 <details class="rt-adv"><summary>Tùy chọn tìm chỗ ghé</summary><div class="ds-pair"><div><label for="ds-corridor">Cách đường đi tối đa (km)</label><input id="ds-corridor" type="number" min="0.1" max="20" step="0.1" value="2"/></div><div><label for="ds-detour">Đi thêm tối đa (phút)</label><input id="ds-detour" type="number" min="0" max="120" placeholder="Không giới hạn"/></div></div></details>
 <button type="button" class="btn btn-dark rt-go" data-ds-route-compute>Tìm dọc tuyến</button>
 <div data-ds-route-result role="status"></div><div data-ds-route-nearby></div>
 <div data-ds-save-route hidden><label for="ds-route-name">Tên tuyến muốn lưu</label><input id="ds-route-name" maxlength="100" placeholder="Ví dụ: Đà Nẵng – Hội An"/><button type="button" class="btn btn-outline" data-ds-route-save>Lưu tuyến vào thư viện</button></div>
</details>` + v.slice(p1);
wr('Views/Discovery/Search.cshtml', v);

// ---------- Script
let j = rd('wwwroot/js/discovery.js');
const lines = j.split('\n');
const iList = lines.findIndex(l => l.startsWith(' function pointList(){'));
const iChoose = lines.findIndex(l => l.startsWith(' function choosePoint(p,target){'));
if (iList < 0 || iChoose !== iList + 1) throw Error('khong thay pointList/choosePoint');
lines.splice(iList, 2, String.raw` // Tuyen: o "Di tu" (A), cac diem ghe (1, 2...), o "Den" (B). Moi o go ten de lay goi y Goong, hoac bam 📍 roi bam len ban do.
 let slots=[null,null],activeSlot=0;
 function syncSlots(){routePoints=slots.filter(Boolean);drawChosen();routeComputed=false;$('[data-ds-save-route]')?.setAttribute('hidden','');}
 async function getJson(url){const r=await fetch(url,{credentials:'same-origin'});const d=await r.json().catch(()=>({}));if(!r.ok)throw new Error(d.message||'Chưa lấy được dữ liệu, thử lại sau.');return d;}
 function goongSuggest(input,list,onPick){let timer=null,seq=0;const close=()=>{list.hidden=true;list.replaceChildren();};
  input.addEventListener('input',()=>{clearTimeout(timer);const q=input.value.trim();if(q.length<2){close();return;}timer=setTimeout(async()=>{const my=++seq;try{const d=await getJson('/api/discovery/diem?q='+encodeURIComponent(q));if(my!==seq)return;list.replaceChildren();
   if(!d.hits.length){list.append(node('p',d.limited?'Goong đang quá tải, hãy bấm 📍 và chọn trên bản đồ.':'Không thấy địa điểm phù hợp. Thử tên khác hoặc chọn trên bản đồ.','rt-empty'));list.hidden=false;return;}
   d.hits.forEach(h=>{const b=node('button',null,'rt-hit');b.type='button';b.append(node('strong',h.name),node('small',h.secondary||''));b.addEventListener('click',async()=>{close();input.value=h.name;try{const pt=await getJson('/api/discovery/diem-chi-tiet?placeId='+encodeURIComponent(h.placeId));onPick({lat:pt.lat,lng:pt.lng,name:h.name});}catch(e){toast(e.message);}});list.append(b);});list.hidden=false;}catch{if(my===seq)close();}},250);});
  input.addEventListener('keydown',e=>{if(e.key==='Escape')close();if(e.key==='Enter'){e.preventDefault();list.querySelector('.rt-hit')?.click();}});
  input.addEventListener('blur',()=>setTimeout(close,200));}
 function pointList(){const box=$('[data-rt-stops]');
  if(routePoints.length>=2&&slots.filter(Boolean).length<routePoints.length)slots=routePoints.slice(); // nap tuyen da luu
  if(!box){syncSlots();return;}box.replaceChildren();
  slots.forEach((p,n)=>{const last=n===slots.length-1,label=n===0?'Đi từ':last?'Đến':'Điểm ghé '+n;
   const field=node('div',null,'rt-field'),row=node('div',null,'rt-row'+(n===activeSlot?' active':''));
   const input=node('input');input.className='form-control';input.placeholder=n===0?'Nơi xuất phát…':last?'Nơi muốn đến…':'Điểm ghé…';input.value=p?p.name:'';input.setAttribute('aria-label',label);
   const list=node('div',null,'rt-suggest');list.hidden=true;
   const pick=node('button','📍','rt-pick');pick.type='button';pick.title='Chọn '+label.toLowerCase()+' trên bản đồ';pick.setAttribute('aria-label',pick.title);
   pick.addEventListener('click',()=>{activeSlot=n;pointList();const area=$('#discovery-map-area');if(area&&area.hidden){area.hidden=false;setTimeout(()=>map?.invalidateSize(),50);}toast('Bấm lên bản đồ để chọn '+label.toLowerCase()+'.');mapElement?.scrollIntoView({block:'center'});});
   input.addEventListener('focus',()=>{activeSlot=n;$$('.rt-row').forEach((r,i)=>r.classList.toggle('active',i===n));});
   goongSuggest(input,list,pt=>{slots[n]=normalizePoint(pt);const empty=slots.findIndex(x=>!x);activeSlot=empty>=0?empty:n;pointList();if(map&&validPoint(slots[n]))map.setView([slots[n].lat,slots[n].lng],Math.max(map.getZoom(),11));});
   row.append(node('span',n===0?'A':last?'B':String(n),'rt-tag'),input,pick);
   if(n>0&&!last){const rm=node('button','×','rt-remove');rm.type='button';rm.title='Bỏ điểm ghé này';rm.addEventListener('click',()=>{slots.splice(n,1);activeSlot=0;pointList();});row.append(rm);}
   field.append(row,list);box.append(field);});
  syncSlots();}
 function choosePoint(p,target){p=normalizePoint(p);if(!validPoint(p)){toast('Mục này chưa có tọa độ đã kiểm tra. Chọn một ghim trên bản đồ.');return;}
  if(target==='start')slots[0]=p;else if(target==='end')slots[slots.length-1]=p;
  else{if(!slots[0]||!slots[slots.length-1]){toast('Chọn nơi đi và nơi đến trước khi thêm điểm ghé.');return;}if(slots.length>=8){toast('Tối đa 8 điểm trên một tuyến.');return;}slots.splice(slots.length-1,0,p);}
  pointList();return true;}
 async function routePick(lat,lng){const n=Math.min(activeSlot,slots.length-1),p={lat,lng,name:'Vị trí trên bản đồ'};slots[n]=p;const empty=slots.findIndex(x=>!x);activeSlot=empty>=0?empty:n;pointList();
  try{const r=await getJson('/api/discovery/ten-vi-tri?lat='+lat.toFixed(6)+'&lng='+lng.toFixed(6));if(r.name&&slots[n]===p){p.name=r.name;pointList();}}catch{}}`);
j = lines.join('\n');
// Bam ban do khi dang mo tuyen: dien vao o dang chon
j = swap(j, `if($('.ds-route-panel')?.open)choosePoint({lat:e.latlng.lat,lng:e.latlng.lng,name:'Ghim trên bản đồ'},$('#ds-route-target').value);`, `if($('.ds-route-panel')?.open)routePick(e.latlng.lat,e.latlng.lng);`);
// O tim cu -> nut them diem ghe, nut phuong tien
const iFind = j.indexOf(` if($('#ds-route-find')){`);
if (iFind >= 0) { const e = j.indexOf('\n', iFind); j = j.slice(0, iFind) + String.raw` pointList();
 $('[data-rt-add]')?.addEventListener('click',()=>{if(slots.length>=8){toast('Tối đa 8 điểm trên một tuyến.');return;}slots.splice(slots.length-1,0,null);activeSlot=slots.length-2;pointList();$$('.rt-row input')[activeSlot]?.focus();});
 $$('[data-rt-mode]').forEach(b=>b.addEventListener('click',()=>{$$('[data-rt-mode]').forEach(x=>x.setAttribute('aria-checked',String(x===b)));const s=$('#ds-mode');if(s)s.value=b.dataset.rtMode;}));` + j.slice(e); }
j = swap(j, `$('[data-ds-route-clear]')?.addEventListener('click',()=>{routePoints=[];pointList();`, `$('[data-ds-route-clear]')?.addEventListener('click',()=>{slots=[null,null];activeSlot=0;routePoints=[];pointList();`);
j = swap(j, `if(routePoints.length<2){toast('Chọn điểm xuất phát và điểm đến trên bản đồ hoặc từ gợi ý.');return;}`, `if(!slots[0]||!slots[slots.length-1]){toast('Nhập nơi đi và nơi đến: gõ tên rồi chọn gợi ý, hoặc bấm 📍 và chọn trên bản đồ.');return;}`);
j = swap(j, `out.textContent='Đang tính đường đi và các lựa chọn ghé qua. Có thể mất một lúc…';`, `out.textContent='Đang tính đường đi bằng Goong và tìm chỗ ghé…';`);
j = swap(j, "out.replaceChildren(node('strong',`${data.distanceKm.toFixed(1)} km · khoảng ${Math.ceil(data.minutes)} phút`),node('p',data.note));",
  "const mins=Math.ceil(data.minutes),dur=mins>=60?Math.floor(mins/60)+' giờ '+(mins%60?mins%60+' phút':''):mins+' phút';out.replaceChildren(node('strong',`${data.distanceKm.toFixed(1)} km · khoảng ${dur}`+(data.provider?` · ${data.provider}`:'')),node('p',data.note));");
// Nap tuyen da luu: dong bo nut phuong tien
j = swap(j, `$('#ds-mode').value=value.mode||'car';pointList();`, `$('#ds-mode').value=value.mode||'car';$$('[data-rt-mode]').forEach(x=>x.setAttribute('aria-checked',String(x.dataset.rtMode===(value.mode||'car'))));pointList();`);
j = swap(j, "choosePoint({lat:item.lat,lng:item.lng,name:item.title},$('#ds-route-target').value);", "{const n=Math.min(activeSlot,slots.length-1);slots[n]=normalizePoint({lat:item.lat,lng:item.lng,name:item.title});const e=slots.findIndex(x=>!x);activeSlot=e>=0?e:n;pointList();}");
wr('wwwroot/js/discovery.js', j);

let css = rd('wwwroot/css/dia-gioi.css');
if (!css.includes('/* Tuyen doc duong */')) css += `
/* Tuyen doc duong */
.rt-stops{display:grid;gap:8px;margin:10px 0 6px}
.rt-field{position:relative}
.rt-row{display:flex;align-items:center;gap:8px;border:1px solid var(--ds-line,#dbe1ee);border-radius:12px;padding:4px 6px 4px 8px;background:#fff}
.rt-row.active{border-color:#2447e8;box-shadow:0 0 0 3px #2447e81f}
.rt-row .form-control{border:0;box-shadow:none;padding:8px 4px;min-height:40px;flex:1;min-width:0;background:transparent}
.rt-tag{width:26px;height:26px;border-radius:50%;display:grid;place-items:center;font-size:12px;font-weight:700;background:#142447;color:#def69b;flex-shrink:0}
.rt-pick,.rt-remove{width:34px;height:34px;border:0;border-radius:9px;background:#f3f6fc;cursor:pointer;flex-shrink:0;font-size:16px;line-height:1}
.rt-pick:hover,.rt-remove:hover{background:#edf1ff}
.rt-suggest{position:absolute;left:0;right:0;top:calc(100% + 4px);z-index:40;background:#fff;border:1px solid var(--ds-line,#dbe1ee);border-radius:12px;box-shadow:0 12px 28px rgba(21,33,62,.16);max-height:280px;overflow:auto;padding:4px}
.rt-hit{display:grid;gap:1px;width:100%;text-align:left;border:0;background:none;padding:8px 10px;border-radius:8px;cursor:pointer}
.rt-hit:hover,.rt-hit:focus-visible{background:#edf1ff}
.rt-hit small{color:var(--ds-muted,#5c667b)}
.rt-empty{margin:0;padding:8px 10px;font-size:13px;color:var(--ds-muted,#5c667b)}
.rt-tools{display:flex;justify-content:space-between;gap:8px;margin-bottom:10px}
.rt-modes{display:flex;gap:6px;flex-wrap:wrap;margin:6px 0 10px}
.rt-modes button{border:1px solid var(--ds-line,#dbe1ee);background:#fff;border-radius:999px;padding:7px 14px;font-weight:600;font-size:14px;cursor:pointer}
.rt-modes button[aria-checked="true"]{background:#142447;border-color:#142447;color:#fff}
.rt-adv{margin:0 0 12px!important;padding:0!important;border:0!important}
.rt-adv>summary{cursor:pointer;color:#2447e8;font-weight:600;font-size:14px}
.rt-go{width:100%}
`;
wr('wwwroot/css/dia-gioi.css', css);
console.log('ok');
