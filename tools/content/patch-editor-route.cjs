// Them o chon diem xuat phat / ket thuc vao trang lap ke hoach (markup, script, kieu dang). Chay mot lan.
const fs = require('fs');
const root = 'src/HoTroDuLich.Web/';
const patch = (file, fn) => { const f = root + file; let s = fs.readFileSync(f, 'utf8'); const crlf = s.includes('\r\n'); s = s.replace(/\r\n/g, '\n'); const before = s; s = fn(s); if (s === before) throw Error('Khong thay doi: ' + file); if (crlf) s = s.replace(/\n/g, '\r\n'); fs.writeFileSync(f, s); };

patch('Views/PlanBoard/Editor.cshtml', s => s.replace(
  `placeholder="Nhập số tiền (₫)" hidden/></div>\n </section>`,
  `placeholder="Nhập số tiền (₫)" hidden/></div>
  <div class="field pe-route-field"><label for="pe-origin">Điểm xuất phát</label>
   <div class="pe-pt"><input class="form-control" id="pe-origin" type="text" maxlength="300" placeholder="Khách sạn, nhà bạn, bến xe, sân bay..." autocomplete="off" @dis/><button type="button" class="btn btn-sm btn-outline-secondary" id="pe-origin-here" @dis>Vị trí của tôi</button></div>
   <ul class="pe-suggest" id="pe-origin-list" hidden></ul>
   <label class="pe-check"><input type="checkbox" id="pe-end-same" checked @dis/> Kết thúc tại điểm xuất phát</label>
   <div id="pe-end-wrap" hidden><label for="pe-end">Điểm kết thúc</label>
    <div class="pe-pt"><input class="form-control" id="pe-end" type="text" maxlength="300" placeholder="Nơi bạn về cuối chuyến" autocomplete="off" @dis/></div>
    <ul class="pe-suggest" id="pe-end-list" hidden></ul></div>
   <small class="pe-hint" id="pe-route-hint">Dùng để tính khoảng cách và đường đi giữa các điểm mỗi ngày.</small></div>
 </section>`));

patch('wwwroot/js/plan-editor.js', s => {
  s = s.replace(`notes: Object.keys(st.notes)`, `origin: st.origin || null, end: $('pe-end-same').checked ? null : (st.end || null), notes: Object.keys(st.notes)`);
  s = s.replace(`  // ---------- khoi tao\n`, `  // ---------- diem xuat phat / ket thuc
  var pointPicker = function (key, inputId, listId) {
    var input = $(inputId), list = $(listId), timer = 0, token = 0;
    var close = function () { list.hidden = true; list.innerHTML = ''; };
    var setPoint = function (p) { st[key] = p; input.value = p ? p.name : ''; close(); touch(); };
    input.addEventListener('input', function () {
      if (st[key]) { st[key] = null; touch(); }
      clearTimeout(timer); var q = input.value.trim(); if (q.length < 3) return close();
      var mine = ++token;
      timer = setTimeout(function () {
        plain('/api/lapkehoach/diem-goi-y?q=' + encodeURIComponent(q)).then(function (hits) {
          if (mine !== token) return;
          if (!hits.length) { list.innerHTML = '<li class="pe-suggest-empty">Chưa tìm thấy. Thử gõ rõ hơn hoặc bấm "Vị trí của tôi".</li>'; list.hidden = false; return; }
          list.innerHTML = hits.slice(0, 6).map(function (h, n) { return '<li><button type="button" data-n="' + n + '"><strong>' + esc(h.name) + '</strong><small>' + esc(h.secondary || '') + '</small></button></li>'; }).join(''); list.hidden = false;
          list.onclick = function (e) {
            var b = e.target.closest('[data-n]'); if (!b) return; var h = hits[+b.dataset.n]; input.disabled = true;
            plain('/api/lapkehoach/diem-chi-tiet?placeId=' + encodeURIComponent(h.placeId)).then(function (d) { setPoint({ name: h.name + (h.secondary ? ', ' + h.secondary : ''), lat: d.lat, lng: d.lng }); }).catch(function (er) { showError(er.message); }).then(function () { input.disabled = !isOwner; });
          };
        }).catch(function () { close(); });
      }, 350);
    });
    document.addEventListener('click', function (e) { if (!list.contains(e.target) && e.target !== input) close(); });
    return setPoint;
  };
  var esc = window.htEsc || function (v) { return String(v).replace(/[&<>"']/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]; }); };
  var setOrigin = pointPicker('origin', 'pe-origin', 'pe-origin-list'), setEnd = pointPicker('end', 'pe-end', 'pe-end-list');
  $('pe-origin-here').addEventListener('click', function () {
    if (!navigator.geolocation) return showError('Trình duyệt này không hỗ trợ định vị.');
    navigator.geolocation.getCurrentPosition(function (p) {
      var lat = +p.coords.latitude.toFixed(6), lng = +p.coords.longitude.toFixed(6);
      if (lat < 5 || lat > 24 || lng < 101.5 || lng > 118.5) return showError('Vị trí của bạn nằm ngoài khu vực hỗ trợ. Hãy gõ tên nơi xuất phát.');
      setOrigin({ name: 'Vị trí của tôi', lat: lat, lng: lng });
    }, function () { showError('Không lấy được vị trí. Hãy cho phép định vị hoặc gõ tên nơi xuất phát.'); }, { enableHighAccuracy: false, timeout: 10000 });
  });
  $('pe-end-same').addEventListener('change', function () { $('pe-end-wrap').hidden = this.checked; if (this.checked) { st.end = null; $('pe-end').value = ''; } touch(); });
  if (st.origin) $('pe-origin').value = st.origin.name || '';
  if (st.end) { $('pe-end-same').checked = false; $('pe-end-wrap').hidden = false; $('pe-end').value = st.end.name || ''; }

  // ---------- khoi tao\n`);
  return s;
});

patch('wwwroot/css/social.css', s => s + `
.pe-route-field{position:relative}
.pe-pt{display:flex;gap:8px;align-items:center}.pe-pt .form-control{flex:1;min-width:0}
.pe-suggest{list-style:none;margin:4px 0 0;padding:4px;border:1px solid var(--bs-border-color,#dfe3ea);border-radius:10px;background:#fff;box-shadow:0 6px 18px rgba(15,23,42,.12);max-height:260px;overflow:auto}
.pe-suggest button{display:block;width:100%;text-align:left;background:none;border:0;padding:8px 10px;border-radius:8px}
.pe-suggest button:hover,.pe-suggest button:focus-visible{background:#eef2ff}
.pe-suggest small{display:block;color:#64748b}.pe-suggest-empty{padding:8px 10px;color:#64748b;font-size:.9rem}
.pe-check{display:flex;gap:8px;align-items:center;margin:8px 0 4px;font-weight:400}.pe-hint{display:block;color:#64748b;margin-top:4px}
`);
console.log('ok');
