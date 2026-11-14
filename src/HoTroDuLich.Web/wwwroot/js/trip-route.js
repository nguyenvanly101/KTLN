// Khoang cach va duong di trong tung ngay cua chuyen di: xuat phat -> cac hang muc -> ket thuc.
// Chi goi API khi khach bam "Xem duong di" (moi doan duong la mot luot goi Goong).
(function () {
  'use strict';
  var dlg = document.getElementById('th-dialog'); if (!dlg) return;
  var planId = dlg.dataset.plan;
  var esc = function (s) { var d = document.createElement('div'); d.textContent = s == null ? '' : s; return d.innerHTML; };
  var fmtKm = function (v) { return (Math.round(v * 10) / 10).toLocaleString('vi-VN') + ' km'; };
  var fmtMin = function (m) { m = Math.round(m); if (m < 60) return m + ' phút'; var h = Math.floor(m / 60), r = m % 60; return h + ' giờ' + (r ? ' ' + r + ' phút' : ''); };
  var loadLeaflet = function () {
    if (window.L) return Promise.resolve();
    return new Promise(function (ok, fail) {
      var l = document.createElement('link'); l.rel = 'stylesheet'; l.href = '/lib/leaflet/leaflet.css'; document.head.appendChild(l);
      var s = document.createElement('script'); s.src = '/lib/leaflet/leaflet.js'; s.onload = ok; s.onerror = fail; document.head.appendChild(s);
    });
  };

  document.querySelectorAll('.timeline-day').forEach(function (sec) {
    var marker = sec.querySelector('.day-marker b'); if (!marker) return;
    var day = +marker.textContent, body = sec.children[1]; if (!body) return;
    var bar = document.createElement('div'); bar.className = 'route-bar';
    bar.innerHTML = '<button type="button" class="btn btn-sm btn-outline-secondary" data-route>Xem đường đi</button>' +
      '<label class="route-mode">Đi bằng <select data-mode><option value="bike">Xe máy</option><option value="car">Ô tô</option><option value="foot">Đi bộ</option></select></label>' +
      '<span class="route-status" role="status"></span>';
    body.insertBefore(bar, body.firstChild);
    var status = bar.querySelector('.route-status'), btn = bar.querySelector('[data-route]'), modeSel = bar.querySelector('[data-mode]');
    var map = null;
    var clear = function () { sec.querySelectorAll('.route-injected').forEach(function (e) { e.remove(); }); if (map) { map.remove(); map = null; } };

    var run = function () {
      clear(); status.textContent = 'Đang tính đường đi…'; btn.disabled = true;
      fetch('/api/lapkehoach/' + planId + '/tuyen?day=' + day + '&mode=' + modeSel.value, { credentials: 'same-origin' })
        .then(function (r) { return r.json().then(function (j) { if (!r.ok) throw new Error(j.message || 'Không tính được đường đi.'); return j; }); })
        .then(function (d) { render(d); })
        .catch(function (e) { status.textContent = e.message; })
        .then(function () { btn.disabled = false; });
    };
    var render = function (d) {
      var pts = d.points || [];
      if (pts.length < 2) { status.textContent = pts.length === 0 ? 'Ngày này chưa có hạng mục nào có vị trí để tính. Bấm Sửa để thêm điểm xuất phát và các điểm dừng từ danh sách gợi ý.' : 'Cần thêm ít nhất một điểm nữa để tính đường đi.'; return; }
      var hasOrigin = pts[0].kind === 'origin';
      status.innerHTML = '<strong>' + fmtKm(d.totalKm) + '</strong> · khoảng ' + esc(fmtMin(d.totalMinutes)) + ' di chuyển' + (d.anyEstimated ? ' <em>(một phần là ước tính theo đường chim bay)</em>' : '');
      var note = [];
      if (!hasOrigin) note.push('Chưa đặt điểm xuất phát, bấm Sửa để thêm.');
      if (d.skipped) note.push(d.skipped + ' hạng mục chưa có vị trí nên chưa được tính.');
      if (d.mode === 'foot') note.push('Đi bộ chỉ là ước tính theo đường chim bay.');
      if (note.length) { var n = document.createElement('p'); n.className = 'route-injected route-note'; n.textContent = note.join(' '); bar.after(n); }
      d.legs.forEach(function (leg) {
        var from = pts[leg.from], to = pts[leg.to];
        var el = document.createElement('div'); el.className = 'route-injected route-leg';
        var txt = fmtKm(leg.km) + ' · ' + fmtMin(leg.minutes) + (leg.estimated ? ' (ước tính)' : '');
        if (to.kind === 'item') { el.innerHTML = '↓ ' + (from.kind === 'origin' ? 'Từ ' + esc(from.name) + ': ' : '') + '<strong>' + txt + '</strong>'; var target = document.getElementById('activity-' + to.activityId); if (target) target.parentNode.insertBefore(el, target); }
        else {
          el.innerHTML = '↓ Về ' + esc(to.name) + ': <strong>' + txt + '</strong>';
          var last = from.activityId ? document.getElementById('activity-' + from.activityId) : null;
          if (last) last.parentNode.insertBefore(el, last.nextSibling); else body.appendChild(el);
        }
      });
      var box = document.createElement('div'); box.className = 'route-injected route-map'; bar.after(box);
      loadLeaflet().then(function () {
        map = L.map(box, { scrollWheelZoom: false }); if (window.htBaseMap) window.htBaseMap(map); else L.tileLayer('https://tile.openstreetmap.de/{z}/{x}/{y}.png', { maxZoom: 19, attribution: '© OpenStreetMap' }).addTo(map);
        var latlngs = d.line.length ? d.line : pts.map(function (p) { return [p.lat, p.lng]; });
        L.polyline(latlngs, { color: '#2f5bd8', weight: 5, opacity: .85 }).addTo(map);
        pts.forEach(function (p, i) {
          var label = p.kind === 'origin' ? 'A' : p.kind === 'end' ? 'B' : String(i);
          L.marker([p.lat, p.lng], { icon: L.divIcon({ className: 'route-pin route-pin-' + p.kind, html: '<span>' + label + '</span>', iconSize: [26, 26], iconAnchor: [13, 13] }) }).addTo(map).bindPopup(esc(p.name));
        });
        map.fitBounds(L.latLngBounds(latlngs.concat(pts.map(function (p) { return [p.lat, p.lng]; }))), { padding: [24, 24] });
      }).catch(function () { box.remove(); });
    };
    btn.addEventListener('click', run);
    modeSel.addEventListener('change', function () { if (sec.querySelector('.route-injected')) run(); });
  });
})();
