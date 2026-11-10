(() => {
 'use strict';
 const maps=window.HanhTrinhMaps;
 window.addEventListener('ht-map-unavailable',()=>{document.querySelectorAll('[data-location-map],[data-point-map],[data-search-map]').forEach(canvas=>maps.showFailure(canvas,new Error('Google Maps chưa được kích hoạt cho website này. Bạn vẫn có thể xem thông tin và liên kết địa điểm.')));});
 const number=v=>v==null||v===''?null:(Number.isFinite(Number(v))?Number(v):null);
 const pos=v=>v&&number(v.lat)!=null&&number(v.lng)!=null&&Math.abs(v.lat)<=90&&Math.abs(v.lng)<=180?{lat:Number(v.lat),lng:Number(v.lng)}:null;
 const set=(form,name,value)=>{const e=form.elements.namedItem(name);if(e)e.value=value??'';};
 const message=(el,text)=>{if(el)el.textContent=text;};
 const uuid=()=>window.crypto?.randomUUID?.() || '10000000-1000-4000-8000-100000000000'.replace(/[018]/g,c=>(c^crypto.getRandomValues(new Uint8Array(1))[0]&15>>c/4).toString(16));
 const tileWarning='Chưa tải được nền bản đồ. Bạn vẫn có thể tìm theo tên và xem danh sách địa điểm.';
 function watchTiles(canvas,status){
  canvas.addEventListener('ht-tiles-unavailable',()=>message(status,tileWarning));
  canvas.addEventListener('ht-tiles-ready',()=>{if(status?.textContent===tileWarning)message(status,'');});
 }
 function externalLink(url,text) {
  const a=document.createElement('a');a.href=url;a.textContent=text;a.target='_blank';a.rel='noopener noreferrer';return a;
 }

 function autocomplete(root,onSelect) {
  const input=root.querySelector('input[type="search"]'),list=root.querySelector('[role="listbox"]'),status=root.querySelector('[data-autocomplete-status]');
  if(!input||!list)return;
  list.id='places-suggestions-'+uuid();input.setAttribute('aria-controls',list.id);
  let session=uuid(),timer,abort,items=[],active=-1;
  const close=()=>{list.hidden=true;input.setAttribute('aria-expanded','false');input.removeAttribute('aria-activedescendant');active=-1;};
  const choose=async index=>{const item=items[index];if(!item)return;close();input.value=item.name;message(status,'');try{await onSelect(item,session);session=uuid();}catch(e){message(status,e.message);}};
  function highlight(index){const options=[...list.querySelectorAll('[role="option"]')];if(!options.length)return;active=(index+options.length)%options.length;options.forEach((b,i)=>b.setAttribute('aria-selected',String(i===active)));input.setAttribute('aria-activedescendant',options[active].id);options[active].scrollIntoView({block:'nearest'});}
  input.addEventListener('input',()=>{
   clearTimeout(timer);abort?.abort();close();
   if(input.value.trim().length<2){message(status,'Nhập ít nhất hai ký tự để tìm địa điểm.');return;}
   timer=setTimeout(async()=>{
    abort=new AbortController();const captured=input.value.trim();
    try{
     message(status,'Đang tìm địa điểm…');
     const url=new URL('/api/maps/suggest',location.origin);url.search=new URLSearchParams({q:captured,session,mapOnly:String(root.dataset.autocomplete==='location')});
     const r=await fetch(url,{signal:abort.signal,cache:'no-store'});
     if(!r.ok)throw new Error(r.status===429?'Bạn tìm hơi nhanh. Chờ một lát rồi thử lại.':'Chưa lấy được gợi ý địa điểm.');
     const data=await r.json();if(input.value.trim()!==captured)return;
     items=data.items||[];list.replaceChildren();
     items.forEach((item,i)=>{const b=document.createElement('button');b.type='button';b.id=list.id+'-'+i;b.setAttribute('role','option');b.setAttribute('aria-selected','false');b.tabIndex=-1;const name=document.createElement('strong'),address=document.createElement('span'),source=document.createElement('small');name.textContent=item.name;address.textContent=item.address;source.textContent=item.source;b.append(name,address,source);b.addEventListener('mousedown',e=>e.preventDefault());b.addEventListener('click',()=>choose(i));list.append(b);});
     if(items.some(x=>x.source==='Google')){const attribution=document.createElement('div');attribution.className='pl-google-attribution';attribution.textContent='Google Maps';attribution.translate=false;list.append(attribution);}
     if(items.some(x=>x.source==='OpenStreetMap')){const credit=document.createElement('div');credit.className='pl-map-credit';credit.append(externalLink('https://www.openstreetmap.org/copyright','© OpenStreetMap'));list.append(credit);}
     list.hidden=items.length===0;input.setAttribute('aria-expanded',String(items.length>0));message(status,data.notice||(!items.length?'Chưa có kết quả. Thử thêm địa chỉ hoặc chọn ghim trên bản đồ.':''));
    }catch(e){if(e.name!=='AbortError')message(status,e.message);}
   },650);
  });
  input.addEventListener('keydown',e=>{if(e.key==='Escape')close();else if(e.key==='ArrowDown'&&!list.hidden){e.preventDefault();highlight(active+1);}else if(e.key==='ArrowUp'&&!list.hidden){e.preventDefault();highlight(active-1);}else if(e.key==='Enter'&&!list.hidden&&active>=0){e.preventDefault();choose(active);}});
  input.addEventListener('blur',()=>setTimeout(close,150));
 }

 document.querySelectorAll('[data-location-picker]').forEach(async root=>{
  const form=root.closest('form'),canvas=root.querySelector('[data-location-map]'),status=root.querySelector('[data-map-status]');
  const lat=form.elements.namedItem('Latitude'),lng=form.elements.namedItem('Longitude');
  const confirmation=root.querySelector('input[type="checkbox"][name="ConfirmLocation"]'),box=root.querySelector('[data-selected-place]');
  const source=form.elements.namedItem('LocationSource');
  let map,removePin,configuration;
  const current=()=>pos({lat:lat.value,lng:lng.value});
  const displayPosition=()=>source.value==='Google'&&!configuration?.mapsReady?null:current();
  function mark(position){
   removePin?.();
   if(!map||!position)return;
   removePin=map.marker(position,'Vị trí bạn xác nhận','anchor');
   map.focus(position,17);
  }
  function manual(position){
   if(!configuration?.mapsReady)set(form,'GooglePlaceId','');
   source.value='Provider';confirmation.checked=false;box.replaceChildren();
   mark(position);message(status,'Kiểm tra đúng điểm phục vụ trước khi xác nhận vị trí.');
  }
  watchTiles(canvas,status);
  const ac=root.querySelector('[data-autocomplete]');
  autocomplete(ac,async(item,session)=>{
   let data=item;
   if(item.source==='Google'){
    const r=await fetch('/api/maps/place?'+new URLSearchParams({id:item.id,session}),{cache:'no-store'});
    data=await r.json();if(!r.ok)throw new Error(data.message||'Chưa đọc được địa điểm Google.');
   }
   const position=pos(data.position);if(!position)throw new Error('Địa điểm chưa có vị trí.');
   const isGoogle=item.source==='Google';
   set(form,'GooglePlaceId',isGoogle?data.id:'');source.value=isGoogle?'Google':'Provider';
   lat.value=position.lat.toFixed(6);lng.value=position.lng.toFixed(6);mark(position);confirmation.checked=false;
   const strong=document.createElement('strong'),p=document.createElement('p');
   strong.textContent=data.name;p.textContent=data.address;
   box.replaceChildren(strong,p,externalLink(data.mapsUrl||data.mapUrl,isGoogle?'Đối chiếu trên Google Maps':'Đối chiếu trên OpenStreetMap'));
   message(status,'Đã đặt ghim. Kiểm tra đúng cơ sở, điền tên và địa chỉ của bạn rồi xác nhận vị trí.');
  });
  [lat,lng].forEach(input=>input.addEventListener('change',()=>{
   if(source.value==='Google'&&!configuration?.mapsReady)(input===lat?lng:lat).value='';
   manual(current());
  }));
  try{
   configuration=await maps.config();map=await maps.create(canvas,displayPosition());mark(displayPosition());
   map.onClick(point=>{
    if(form.querySelector('fieldset:disabled'))return;
    lat.value=point.lat.toFixed(6);lng.value=point.lng.toFixed(6);manual(current());
   });
  }catch(e){maps.showFailure(canvas,e);message(status,'Bạn có thể chọn địa điểm bằng tên hoặc nhập tọa độ thủ công.');}
 });

 document.querySelectorAll('[data-point-map]').forEach(async canvas=>{
  watchTiles(canvas,canvas.nextElementSibling);
  try{const position={lat:Number(canvas.dataset.lat),lng:Number(canvas.dataset.lng)};const map=await maps.create(canvas,position);map.marker(position,document.title,'anchor');}
  catch(e){maps.showFailure(canvas,e);}
 });

 const page=document.querySelector('[data-place-search]');if(!page)return;
 const form=page.querySelector('[data-place-search-form]'),anchorInput=page.querySelector('#anchor-query'),locationStatus=page.querySelector('[data-search-location-status]');
 let selectedAnchorName=anchorInput.value;
 function clearAnchor(){['AnchorKind','AnchorId','GooglePlaceId','AnchorName','Lat','Lng'].forEach(k=>set(form,k,''));anchorInput.value='';selectedAnchorName='';}
 autocomplete(page.querySelector('[data-autocomplete="anchor"]'),async(item,session)=>{
  if(item.source==='Google'){
   // Complete the autocomplete session with Place Details before applying the chosen anchor.
   const r=await fetch('/api/maps/place?'+new URLSearchParams({id:item.id,session}),{cache:'no-store'});const detail=await r.json();if(!r.ok)throw new Error(detail.message||'Chưa xác định được địa điểm Google.');
   item={...item,name:detail.name,address:detail.address};
  }
  const openPosition=item.source==='OpenStreetMap'?pos(item.position):null;
  const kind=openPosition?'OpenStreetMap':item.source==='Google'?'Google':item.source==='Cơ sở dịch vụ'?'Branch':'Landmark';
  set(form,'AnchorKind',kind);set(form,'AnchorId',item.localId);set(form,'GooglePlaceId',kind==='Google'?item.id:'');set(form,'AnchorName',item.name);
  set(form,'Lat',openPosition?.lat.toFixed(6));set(form,'Lng',openPosition?.lng.toFixed(6));selectedAnchorName=item.name;
  message(locationStatus,'Đã chọn mốc '+item.name+'. Chọn thứ cần tìm rồi bấm Tìm quanh mốc.');
 });
 anchorInput.addEventListener('input',()=>{if(anchorInput.value!==selectedAnchorName){['AnchorKind','AnchorId','GooglePlaceId','AnchorName','Lat','Lng'].forEach(k=>set(form,k,''));}});
 page.querySelector('[data-clear-anchor]').addEventListener('click',()=>{clearAnchor();message(locationStatus,'Đã bỏ mốc. Bạn có thể xem tất cả cơ sở hoặc chọn mốc mới.');anchorInput.focus();});
 form.addEventListener('submit',e=>{if(anchorInput.value.trim()&&!form.elements.namedItem('AnchorKind').value){e.preventDefault();message(locationStatus,'Chọn một địa điểm trong gợi ý để xác định đúng mốc, hoặc đặt ghim trên bản đồ.');anchorInput.focus();}});
 page.querySelector('[data-use-my-location]').addEventListener('click',()=>{
  if(!navigator.geolocation){message(locationStatus,'Trình duyệt chưa hỗ trợ vị trí. Chọn mốc hoặc ghim thủ công.');return;}
  message(locationStatus,'Đang xác định vị trí của bạn…');
  navigator.geolocation.getCurrentPosition(p=>{clearAnchor();set(form,'AnchorKind','Pin');set(form,'Lat',p.coords.latitude.toFixed(6));set(form,'Lng',p.coords.longitude.toFixed(6));set(form,'AnchorName','Vị trí của tôi');anchorInput.value=selectedAnchorName='Vị trí của tôi';form.requestSubmit();},()=>message(locationStatus,'Chưa lấy được vị trí. Bạn có thể chọn địa điểm hoặc đặt ghim trên bản đồ.'),{timeout:10000,maximumAge:60000});
 });
 const compare=[...page.querySelectorAll('[data-compare-branch]')],compareStatus=page.querySelector('[data-compare-status]');
 compare.forEach(input=>input.addEventListener('change',()=>{const selected=compare.filter(x=>x.checked);if(selected.length>3){input.checked=false;message(compareStatus,'Chọn tối đa 3 cơ sở.');}else{message(compareStatus,'');message(page.querySelector('[data-compare-count]'),`(${selected.length}/3)`);}}));
 page.querySelector('#compare-branches').addEventListener('submit',e=>{if(!compare.some(x=>x.checked)){e.preventDefault();message(compareStatus,'Chọn ít nhất một cơ sở để so sánh.');}});
 const scrollKey='places-scroll:'+location.pathname+location.search;
 try{const saved=sessionStorage.getItem(scrollKey);if(saved)requestAnimationFrame(()=>window.scrollTo(0,Number(saved)));}catch{}
 page.querySelectorAll('a[href^="/co-so/"]').forEach(a=>{const u=new URL(a.href);u.searchParams.set('returnUrl',location.pathname+location.search);a.href=u.pathname+u.search;a.addEventListener('click',()=>{try{sessionStorage.setItem(scrollKey,String(window.scrollY));}catch{}});});

 const canvas=page.querySelector('[data-search-map]'),mapStatus=page.querySelector('[data-search-map-status]'),routePanel=page.querySelector('[data-route-panel]');
 let payload;try{payload=JSON.parse(page.querySelector('[data-places-payload]').textContent);}catch{return;}
 const hits=payload.hits||[],anchor=payload.anchor;
 const positions=[pos(anchor?.position),...hits.map(hit=>pos(hit.position))].filter(Boolean);
 let map,removeRoute,removeSelectedPin,fitDone=false,routing=false;
 const toggle=page.querySelector('[data-toggle-map]');
 function refreshMap(){
  if(!map||!canvas.clientWidth)return;
  map.refresh();
  if(!fitDone&&positions.length>1)map.fit(positions);
  fitDone=true;
 }
 function showMap(show){
  page.classList.toggle('pl-show-map',show);toggle.setAttribute('aria-expanded',String(show));
  toggle.textContent=show?'Xem danh sách':'Xem bản đồ';
  if(show)refreshMap();
 }
 watchTiles(canvas,mapStatus);
 toggle.addEventListener('click',()=>showMap(!page.classList.contains('pl-show-map')));
 function select(key,fromMarker=false,focusMap=true){
  const h=hits.find(x=>x.key===key);if(!h)return;
  page.querySelectorAll('[data-result-key]').forEach(c=>c.classList.toggle('is-selected',c.dataset.resultKey===key));
  if(focusMap&&map&&pos(h.position))map.focus(pos(h.position),16);
  message(mapStatus,h.address?h.name+' · '+h.address:h.name);
  if(fromMarker){
   if(matchMedia('(max-width:900px)').matches)showMap(false);
   page.querySelector(`[data-result-key="${CSS.escape(key)}"]`)?.scrollIntoView({behavior:matchMedia('(prefers-reduced-motion: reduce)').matches?'auto':'smooth',block:'nearest'});
  }
 }
 page.querySelectorAll('[data-show-on-map]').forEach(b=>b.addEventListener('click',()=>{showMap(true);select(b.dataset.showOnMap);canvas.scrollIntoView({block:'nearest'});}));
 page.querySelectorAll('[data-route-to]').forEach(b=>b.addEventListener('click',async()=>{
  const h=hits.find(x=>x.key===b.dataset.routeTo);if(routing||!h||!anchor||!pos(h.position))return;
  showMap(true);select(h.key,false,false);routing=true;b.disabled=true;routePanel.hidden=false;
  removeRoute?.();removeRoute=null;
  message(routePanel,'Đang tìm đường đến '+h.name+'…');
  if(matchMedia('(max-width:900px)').matches)canvas.scrollIntoView({block:'nearest'});
  try{
   const r=await fetch('/api/maps/route',{method:'POST',headers:{'Content-Type':'application/json','RequestVerificationToken':page.querySelector('[data-route-token] input').value},body:JSON.stringify({originLat:anchor.position.lat,originLng:anchor.position.lng,destinationLat:h.position.lat,destinationLng:h.position.lng,mode:payload.mode})});
   if(r.status===429)throw new Error('Bạn yêu cầu đường đi hơi nhanh. Chờ một lát rồi thử lại.');const data=await r.json();if(!r.ok)throw new Error(data.message||'Chưa tìm được đường đi.');
   await mapReady;
   if(map)removeRoute=map.drawRoute(data);
   routePanel.replaceChildren();const strong=document.createElement('strong'),p=document.createElement('p');strong.textContent=anchor.name+' → '+h.name;p.textContent=(data.distanceMeters/1000).toFixed(1)+' km đường đi · khoảng '+Math.ceil(data.durationSeconds/60)+' phút';routePanel.append(strong,p);(data.warnings||[]).forEach(w=>{const note=document.createElement('small');note.textContent=w;routePanel.append(note);});
   if(data.provider){
    const credit=document.createElement('small');
    credit.append(data.provider+' · ',externalLink('https://www.openstreetmap.org/fixthemap','Góp ý bản đồ'));
    routePanel.append(credit);
   }
  }catch(e){message(routePanel,e.message);}finally{b.disabled=false;routing=false;}
 }));
 function choosePin(position,title){clearAnchor();set(form,'AnchorKind','Pin');set(form,'Lat',position.lat.toFixed(6));set(form,'Lng',position.lng.toFixed(6));set(form,'AnchorName',title);anchorInput.value=selectedAnchorName=title;removeSelectedPin?.();removeSelectedPin=map.marker(position,title,'anchor');message(locationStatus,'Đã chọn mốc mới. Bấm Tìm quanh mốc để áp dụng.');}
 page.querySelector('[data-search-map-center]').addEventListener('click',()=>{if(!map){message(mapStatus,'Bản đồ chưa sẵn sàng. Hãy chọn mốc bằng tên hoặc dùng vị trí của bạn.');return;}choosePin(map.center(),'Tâm bản đồ đã chọn');form.requestSubmit();});
 const mapReady=(async()=>{
  try{
   map=await maps.create(canvas,positions[0]);
   if(anchor&&pos(anchor.position))removeSelectedPin=map.marker(pos(anchor.position),anchor.name,'anchor');
   hits.forEach(h=>{
    const position=pos(h.position);if(!position)return;
    const kind=h.source==='Google Maps'?'google':h.source==='OpenStreetMap'?'open':'branch';
    map.marker(position,h.name,kind,()=>select(h.key,true));
   });
   refreshMap();
   map.onClick(point=>choosePin(point,'Ghim trên bản đồ'));
  }catch(e){maps.showFailure(canvas,e);message(mapStatus,e.message);}
 })();
})();
