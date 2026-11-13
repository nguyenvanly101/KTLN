// Ban do cua trang diem den: hien cac muc co toa do cua tab dang xem (danh sach hoac ban do tong hop).
(function () {
  'use strict';
  var el = document.querySelector('[data-dest-map]');
  if (!el || !window.L) return;

  var data = [];
  try { data = JSON.parse(document.getElementById('dest-points').textContent || '[]'); } catch (e) { data = []; }

  var url = (document.querySelector('meta[name="ds-tile-url"]') || {}).content || 'https://tile.openstreetmap.de/{z}/{x}/{y}.png';
  var attribution = (document.querySelector('meta[name="ds-tile-attribution"]') || {}).content;
  var maxNative = (function (u) { try { return new URL(u, location.href).hostname === 'tile.openstreetmap.de' ? 16 : 19; } catch (e) { return 19; } })(url);
  var colors = { 'tham-quan': '#2447e8', 'an-uong': '#d9480f', 'luu-tru': '#2b8a3e', 'di-chuyen': '#7048e8', khac: '#495057' };

  var VN = L.latLngBounds([[7.5, 101.5], [24.0, 110.5]]);
  var map = L.map(el, { scrollWheelZoom: false, maxBounds: VN, maxBoundsViscosity: 1 });
  try { map.setMinZoom(Math.max(5, Math.min(7, map.getBoundsZoom(VN, false)))); } catch (e) { }
  if (window.htBaseMap) window.htBaseMap(map); else L.tileLayer(url, { maxZoom: 19, maxNativeZoom: maxNative, referrerPolicy: 'strict-origin-when-cross-origin', attribution: attribution }).addTo(map);

  var markers = {};
  var layers = [];
  data.forEach(function (p) {
    var marker = L.circleMarker([p.lat, p.lng], { radius: 10, color: '#fff', weight: 2, fillColor: colors[p.c] || colors.khac, fillOpacity: 0.95 }).addTo(map);
    var box = document.createElement('div');
    var title = document.createElement('strong'); title.textContent = p.t;
    var kind = document.createElement('div'); kind.textContent = p.k;
    var link = document.createElement('a'); link.href = p.u; link.textContent = 'Xem chi tiết';
    box.append(title, kind, link);
    marker.bindPopup(box);
    markers[p.id] = marker;
    layers.push(marker);
  });

  if (layers.length) {
    map.fitBounds(L.featureGroup(layers).getBounds(), { padding: [30, 30], maxZoom: 15 });
  } else {
    var lat = parseFloat(el.dataset.lat), lng = parseFloat(el.dataset.lng);
    if (isFinite(lat) && isFinite(lng)) map.setView([lat, lng], 11); else map.setView([16.0, 106.0], 5);
  }

  // Re chuot / bam vao the trong danh sach -> mo ghim tuong ung tren ban do.
  document.querySelectorAll('[data-entry-card]').forEach(function (card) {
    var id = card.getAttribute('data-entry-card');
    function show() { var m = markers[id]; if (m) { m.openPopup(); m.setStyle({ radius: 13 }); } }
    function hide() { var m = markers[id]; if (m) m.setStyle({ radius: 10 }); }
    card.addEventListener('mouseenter', show);
    card.addEventListener('mouseleave', hide);
    card.addEventListener('focusin', show);
    card.addEventListener('focusout', hide);
  });
})();
