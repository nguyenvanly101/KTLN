/* Use the configured Google map or the default OpenStreetMap tiles. */
(() => {
 'use strict';
 let configPromise, loadPromise;
 async function config() {
  if (!configPromise) configPromise = fetch('/api/maps/config', {cache:'no-store', credentials:'same-origin'})
   .then(r => { if (!r.ok) throw new Error('Chưa đọc được cấu hình bản đồ.'); return r.json(); });
  return configPromise;
 }
 async function load() {
  if (window.google?.maps?.importLibrary) return window.google.maps;
  if (!loadPromise) loadPromise = (async () => {
   const c = await config();
   if (!c.mapsReady) throw new Error('Bản đồ Google chưa sẵn sàng. Các thông tin trong danh sách vẫn có thể sử dụng.');
   await new Promise((resolve, reject) => {
    const timer = setTimeout(() => reject(new Error('Google Maps phản hồi chậm. Vui lòng thử tải lại trang.')), 15000);
    window.htGoogleMapsReady = () => { clearTimeout(timer); resolve(); };
    window.gm_authFailure = () => { clearTimeout(timer); reject(new Error('Google Maps chưa được kích hoạt cho website này.')); window.dispatchEvent(new Event('ht-map-unavailable')); };
    const script = document.createElement('script');
    const url = new URL('https://maps.googleapis.com/maps/api/js');
    url.search = new URLSearchParams({key:c.browserKey, v:'weekly', loading:'async', callback:'htGoogleMapsReady', language:'vi', region:'VN'}).toString();
    script.src = url.href; script.async = true; script.referrerPolicy = 'strict-origin-when-cross-origin';
    script.nonce = document.querySelector('meta[name="maps-nonce"]')?.content || '';
    script.onerror = () => {clearTimeout(timer); reject(new Error('Chưa kết nối được Google Maps.'));};
    document.head.append(script);
   });
   await Promise.all([google.maps.importLibrary('maps'),google.maps.importLibrary('marker'),google.maps.importLibrary('geometry')]);
   return google.maps;
  })();
  return loadPromise;
 }
 const centerOfVietnam = {lat:16.46,lng:107.58};
 function pinContent(kind) {
  const pin = document.createElement('span');
  pin.className = 'pl-map-pin pl-map-pin-' + kind;
  pin.textContent = kind === 'google' ? 'G' : kind === 'open' ? '○' : '●';
  return pin;
 }

 function openMap(element, position, zoom) {
  if (!window.L) throw new Error('Chưa tải được trình hiển thị bản đồ.');
  const map = L.map(element, {scrollWheelZoom:false}).setView(position || centerOfVietnam, position ? zoom : 6);
  const url = document.querySelector('meta[name="ds-tile-url"]')?.content || 'https://tile.openstreetmap.org/{z}/{x}/{y}.png';
  const attribution = document.querySelector('meta[name="ds-tile-attribution"]')?.content;
  const maxNativeZoom = new URL(url, location.href).hostname === 'tile.openstreetmap.de' ? 16 : 19;
  const tiles = L.tileLayer(url, {maxZoom:19, maxNativeZoom, updateWhenZooming:false, referrerPolicy:'strict-origin-when-cross-origin',
   attribution}).addTo(map);
  const fit = points => map.fitBounds(points, {padding:[40,40],maxZoom:17});
  tiles.on('load', () => {
   const hasImage = [...element.querySelectorAll('img.leaflet-tile-loaded')].some(tile => tile.naturalWidth > 0);
   element.dispatchEvent(new Event(hasImage ? 'ht-tiles-ready' : 'ht-tiles-unavailable'));
  });
  return {
   focus: (point, level) => map.setView(point, level),
   fit,
   center: () => map.getCenter().wrap(),
   onClick: handler => map.on('click', event => handler(event.latlng.wrap())),
   refresh: () => map.invalidateSize({pan:false}),
   marker(point, title, kind, onClick) {
    const size = kind === 'anchor' ? 35 : 29;
    const icon = L.divIcon({html:pinContent(kind), className:'pl-leaflet-pin', iconSize:[size,size], iconAnchor:[size/2,size]});
    const marker = L.marker(point, {icon,title,alt:title,keyboard:!!onClick,interactive:!!onClick}).addTo(map);
    if (onClick) {
     marker.getElement()?.setAttribute('aria-label',title);
     marker.on('click', onClick);
    }
    return () => marker.remove();
   },
   drawRoute(data) {
    const line = L.polyline(data.coordinates, {color:'#2447e8',opacity:.9,weight:5,interactive:false}).addTo(map);
    fit(data.coordinates);
    return () => line.remove();
   }
  };
 }

 async function create(element, position, zoom=15) {
  const c = await config();
  if (!c.mapsReady) {
   element.replaceChildren();
   return openMap(element, position, zoom);
  }
  const maps = await load();
  element.replaceChildren();
  const map = new maps.Map(element, {mapId:c.mapId, center:position || centerOfVietnam, zoom:position?zoom:6, mapTypeControl:false, streetViewControl:false, gestureHandling:'cooperative', clickableIcons:false});
  const fit = points => {const bounds = new maps.LatLngBounds(); points.forEach(point => bounds.extend(point)); map.fitBounds(bounds,45);};
  return {
   focus: (point, level) => {map.panTo(point);map.setZoom(level);},
   fit,
   center: () => map.getCenter().toJSON(),
   onClick: handler => map.addListener('click', event => {if(event.latLng)handler(event.latLng.toJSON());}),
   refresh: () => {},
   marker(point,title,kind,onClick) {
    const marker = new maps.marker.AdvancedMarkerElement({map,position:point,content:pinContent(kind),title});
    if (onClick) marker.addListener('click',onClick);
    return () => {marker.map = null;};
   },
   drawRoute(data) {
    const path = maps.geometry.encoding.decodePath(data.encodedPolyline);
    const line = new maps.Polyline({path,strokeColor:'#2447e8',strokeOpacity:.9,strokeWeight:5,map});
    fit(path);
    return () => line.setMap(null);
   }
  };
 }
 function showFailure(element,error) {
  const box=document.createElement('div');box.className='pl-map-fallback';
  const title=document.createElement('strong');title.textContent='Bản đồ địa điểm';
  const description=document.createElement('p');description.textContent=error.message||'Bản đồ chưa sẵn sàng.';
  box.append(title,description);element.replaceChildren(box);
 }
 window.HanhTrinhMaps={config,create,showFailure};
})();
