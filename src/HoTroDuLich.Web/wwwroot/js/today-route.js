// Khung "Duong di hom nay" tren trang Hom nay: xuat phat -> cac diem dung -> ket thuc, km/phut tung doan, chi duong toi diem ke tiep.
(function () {
  'use strict';
  var box = document.getElementById('today-route'); if (!box) return;
  var out = box.querySelector('.route-out'), btn = box.querySelector('[data-route]'), modeSel = box.querySelector('[data-mode]');
  var esc = function (s) { var d = document.createElement('div'); d.textContent = s == null ? '' : s; return d.innerHTML; };
  var fmtKm = function (v) { return (Math.round(v * 10) / 10).toLocaleString('vi-VN') + ' km'; };
  var fmtMin = function (m) { m = Math.round(m); if (m < 60) return m + ' phút'; var h = Math.floor(m / 60), r = m % 60; return h + ' giờ' + (r ? ' ' + r + ' phút' : ''); };
  var travel = { car: 'driving', bike: 'two-wheeler', foot: 'walking' };
  var dir = function (to, from, mode) { return 'https://www.google.com/maps/dir/?api=1' + (from ? '&origin=' + from.lat + ',' + from.lng : '') + '&destination=' + to.lat + ',' + to.lng + '&travelmode=' + (travel[mode] || 'driving'); };
  var map = null;
  var loadLeaflet = function () {
    if (window.L) return Promise.resolve();
    return new Promise(function (ok, fail) {
      var l = document.createElement('link'); l.rel = 'stylesheet'; l.href = '/lib/leaflet/leaflet.css'; document.head.appendChild(l);
      var s = document.createElement('script'); s.src = '/lib/leaflet/leaflet.js'; s.onload = ok; s.onerror = fail; document.head.appendChild(s);
    });
  };
  var nextId = +box.dataset.next || 0;

  var render = function (d) {
    var pts = d.points || [];
    if (map) { map.remove(); map = null; }
    if (pts.length < 2) { out.innerHTML = '<p class="help">' + (pts.length === 0 ? 'Hôm nay chưa có điểm dừng nào có vị trí. Thêm điểm xuất phát và chọn dịch vụ hoặc địa điểm từ danh sách gợi ý trong phần Sửa kế hoạch.' : 'Cần thêm ít nhất một điểm nữa để tính đường đi.') + '</p>'; return; }
    var hasOrigin = pts[0].kind === 'origin', h = '<p class="route-total"><strong>' + fmtKm(d.totalKm) + '</strong> · khoảng ' + esc(fmtMin(d.totalMinutes)) + (d.anyEstimated ? ' <em>(một phần là ước tính)</em>' : '') + '</p>';
    if (!hasOrigin) h += '<p class="route-note">Chưa đặt điểm xuất phát nên đoạn đầu tiên chưa được tính. Bấm Sửa kế hoạch để thêm.</p>';
    if (d.skipped) h += '<p class="route-note">' + d.skipped + ' hạng mục chưa có vị trí nên chưa được tính.</p>';
    h += '<ol class="route-stops">';
    pts.forEach(function (p, i) {
      var label = p.kind === 'origin' ? 'Xuất phát' : p.kind === 'end' ? 'Kết thúc' : 'Điểm dừng';
      var isNext = p.kind === 'item' && p.activityId === nextId;
      h += '<li class="route-stop' + (isNext ? ' is-next' : '') + '"><span class="route-dot">' + (p.kind === 'origin' ? 'A' : p.kind === 'end' ? 'B' : i) + '</span><div><small>' + label + (isNext ? ' · tiếp theo' : '') + '</small><div>' + esc(p.name) + '</div>';
      var leg = d.legs[i];
      if (leg) { var to = pts[i + 1]; h += '<div class="route-leg-line">↓ ' + fmtKm(leg.km) + ' · ' + fmtMin(leg.minutes) + (leg.estimated ? ' (ước tính)' : '') + ' <a href="' + dir(to, p, d.mode) + '" target="_blank" rel="noopener noreferrer">Chỉ đường</a></div>'; }
      h += '</div></li>';
    });
    h += '</ol>';
    var nx = pts.find(function (p) { return p.kind === 'item' && p.activityId === nextId; });
    if (nx) h += '<a class="btn btn-lime route-go" href="' + dir(nx, null, d.mode) + '" target="_blank" rel="noopener noreferrer">Chỉ đường từ vị trí của tôi đến điểm kế tiếp</a>';
    h += '<div class="route-map" id="today-map"></div>';
    out.innerHTML = h;
    loadLeaflet().then(function () {
      var el = document.getElementById('today-map'); if (!el) return;
      map = L.map(el, { scrollWheelZoom: false }); if (window.htBaseMap) window.htBaseMap(map); else L.tileLayer('https://tile.openstreetmap.de/{z}/{x}/{y}.png', { maxZoom: 19, attribution: '© OpenStreetMap' }).addTo(map);
      var line = d.line.length ? d.line : pts.map(function (p) { return [p.lat, p.lng]; });
      L.polyline(line, { color: '#2f5bd8', weight: 5, opacity: .85 }).addTo(map);
      pts.forEach(function (p, i) { L.marker([p.lat, p.lng], { icon: L.divIcon({ className: 'route-pin route-pin-' + p.kind, html: '<span>' + (p.kind === 'origin' ? 'A' : p.kind === 'end' ? 'B' : i) + '</span>', iconSize: [26, 26], iconAnchor: [13, 13] }) }).addTo(map).bindPopup(esc(p.name)); });
      map.fitBounds(L.latLngBounds(line.concat(pts.map(function (p) { return [p.lat, p.lng]; }))), { padding: [24, 24] });
    }).catch(function () { var m = document.getElementById('today-map'); if (m) m.remove(); });
  };
  var run = function () {
    out.innerHTML = '<p class="help" role="status">Đang tính đường đi…</p>'; btn.disabled = true;
    fetch('/api/lapkehoach/' + box.dataset.plan + '/tuyen?day=' + box.dataset.day + '&mode=' + modeSel.value, { credentials: 'same-origin' })
      .then(function (r) { return r.json().then(function (j) { if (!r.ok) throw new Error(j.message || 'Không tính được đường đi.'); return j; }); })
      .then(render).catch(function (e) { out.innerHTML = '<p class="route-note">' + esc(e.message) + '</p>'; }).then(function () { btn.disabled = false; });
  };
  btn.addEventListener('click', run);
  modeSel.addEventListener('change', function () { if (out.innerHTML) run(); });
})();
