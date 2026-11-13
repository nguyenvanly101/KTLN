(function () {
  'use strict';
  var root = document.getElementById('pe'); if (!root) return;
  var $ = function (id) { return document.getElementById(id); };
  var esc = function (s) { var d = document.createElement('div'); d.textContent = s == null ? '' : s; return d.innerHTML; };
  var money = function (n) { return (Math.round(n || 0)).toLocaleString('vi-VN') + ' ₫'; };
  var token = function () { var i = root.querySelector('input[name="__RequestVerificationToken"]'); return i ? i.value : ''; };
  var isOwner = root.dataset.owner === '1', today = root.dataset.today;
  var st = JSON.parse(root.dataset.draft); st.items = (st.items || []).map(function (i, n) { i.k = 'k' + n; return i; });
  var noteMap = {}; (st.notes || []).forEach(function (n) { noteMap[n.day] = n.text; }); st.notes = noteMap;
  st.dests = (st.destination || '').split(' · ').map(function (x) { return x.trim(); }).filter(Boolean).map(function (n) { return { name: n, areaId: null }; });
  var seq = st.items.length, cur = 1, dirty = false, mode = 'add', swapKey = null, picked = null, pickTimer = null, pickGroup = '', pickKind = '', pickPage = 1, pickTotal = 0;
  var SLOTS = [['Sáng', '08:00'], ['Trưa', '12:00'], ['Chiều', '15:00'], ['Tối', '19:00']];
  var GROUPS = { Stay: 'Lưu trú', Eat: 'Ăn uống', Place: 'Tham quan', Move: 'Di chuyển', Experience: 'Trải nghiệm', Info: 'Thông tin', Local: 'Dịch vụ địa phương', Health: 'Y tế và an toàn', Utility: 'Tiện ích và mua sắm', Other: 'Khác' };
  var local = function (d) { var m = d.getMonth() + 1, dd = d.getDate(); return d.getFullYear() + '-' + (m < 10 ? '0' : '') + m + '-' + (dd < 10 ? '0' : '') + dd; };
  var shift = function (iso, n) { var d = new Date(iso + 'T00:00:00'); d.setDate(d.getDate() + n); return local(d); };
  var fmt = function (iso) { return new Date(iso + 'T00:00:00').toLocaleDateString('vi-VN', { weekday: 'short', day: '2-digit', month: '2-digit' }); };
  var api = function (path, method, body) {
    var o = { method: method || 'GET', headers: { 'RequestVerificationToken': token() } };
    if (body !== undefined) { o.headers['Content-Type'] = 'application/json'; o.body = JSON.stringify(body); }
    return fetch(path, o).then(function (r) { return r.json().catch(function () { return {}; }).then(function (j) { if (!r.ok) throw new Error(j.message || 'Có lỗi xảy ra, hãy thử lại.'); return j; }); });
  };
  var plain = function (path) { return fetch(path, { credentials: 'same-origin' }).then(function (r) { return r.json().then(function (j) { if (!r.ok) throw new Error(j.message || 'Không tải được dữ liệu.'); return j; }); }); };
  var showError = function (m) { var e = $('pe-error'); e.textContent = m; e.hidden = !m; if (m) window.scrollTo({ top: 0, behavior: 'smooth' }); };
  var touch = function () { dirty = true; };
  var toast = function (m) { var t = document.createElement('div'); t.className = 'pe-toast'; t.textContent = m; document.body.appendChild(t); setTimeout(function () { t.remove(); }, 2600); };
  var undoBar = function (msg, fn) { var t = document.createElement('div'); t.className = 'pe-toast'; t.innerHTML = esc(msg) + ' <button type="button">Hoàn tác</button>'; document.body.appendChild(t); t.querySelector('button').onclick = function () { fn(); t.remove(); }; setTimeout(function () { t.remove(); }, 6500); };
  var VN_BOUNDS = L.latLngBounds(window.htVnBounds || [[7.5, 101.5], [24.0, 110.5]]);
  var limitToVn = function (m) { try { m.setMinZoom(Math.max(window.htMapMinZoom || 0, 5, Math.min(7, m.getBoundsZoom(VN_BOUNDS, false)))); } catch (e) { } };
  var addTiles = function (m) { if (window.htBaseMap) return window.htBaseMap(m); var url = (document.querySelector('meta[name="ds-tile-url"]') || {}).content || 'https://tile.openstreetmap.de/{z}/{x}/{y}.png'; return L.tileLayer(url, { maxZoom: 19, maxNativeZoom: 16, referrerPolicy: 'strict-origin-when-cross-origin' }).addTo(m); };

  // ---------- gio
  var timeOptions = function (sel, value) {
    var h = '<option value="">Không đặt giờ</option>', found = !value;
    for (var m = 300; m <= 1410; m += 30) { var t = ('0' + Math.floor(m / 60)).slice(-2) + ':' + ('0' + (m % 60)).slice(-2); if (t === value) found = true; h += '<option value="' + t + '">' + t + '</option>'; }
    if (!found) h += '<option value="' + value + '">' + value + '</option>';
    sel.innerHTML = h; sel.value = value || '';
  };
  var slotChips = function (box, sel) {
    box.innerHTML = SLOTS.map(function (s) { return '<button type="button" class="pe-chip" data-t="' + s[1] + '">' + s[0] + '</button>'; }).join('');
    box.onclick = function (e) { var b = e.target.closest('[data-t]'); if (b) sel.value = b.dataset.t; };
  };

  // ---------- diem den: nhieu diem, nhieu cap
  var destText = function () { return st.dests.map(function (d) { return d.name; }).join(' · '); };
  var renderDests = function () {
    $('pe-destchips').innerHTML = st.dests.map(function (d, i) { return '<span class="pe-destchip" title="' + esc(d.path || '') + '"><b>' + esc(d.name) + '</b>' + (d.path ? '<small>' + esc(d.path) + '</small>' : '') + (isOwner ? '<button type="button" data-rm="' + i + '" aria-label="Bỏ ' + esc(d.name) + '">✕</button>' : '') + '</span>'; }).join('');
    refreshInfo(); fillPickDest();
  };
  var addDest = function (d) {
    if (!isOwner) return; var name = (d.name || '').trim(); if (!name) return;
    if (st.dests.some(function (x) { return x.name.toLowerCase() === name.toLowerCase(); })) return toast('Điểm đến này đã có trong danh sách.');
    if (st.dests.length >= 5) return toast('Tối đa 5 điểm đến cho một chuyến đi.');
    if ((destText() + ' · ' + name).length > 200) return toast('Danh sách điểm đến quá dài.');
    st.dests.push({ name: name, areaId: d.areaId || null, path: d.path || null }); touch(); renderDests(); renderDay();
  };
  $('pe-destchips').addEventListener('click', function (e) { var b = e.target.closest('[data-rm]'); if (!b) return; st.dests.splice(+b.dataset.rm, 1); touch(); renderDests(); });
  // goi y khi go
  var sugTimer = null, sugData = [];
  var LEVEL = { 1: 'Tỉnh / Thành phố', 2: 'Quận / Huyện / Thị xã', 3: 'Phường / Xã' };
  var levelLabel = function (r) { return r.source === 'goong' ? 'Địa điểm khác' : r.source === 'area' ? (LEVEL[r.level] || 'Khu vực') : r.source === 'destination' ? 'Điểm đến du lịch' : 'Khu du lịch / địa điểm'; };
  var hideSug = function () { $('pe-dest-sug').hidden = true; $('pe-dest').setAttribute('aria-expanded', 'false'); };
  $('pe-dest').addEventListener('input', function () {
    clearTimeout(sugTimer); var q = this.value.trim(); if (q.length < 1) return hideSug();
    sugTimer = setTimeout(function () {
      Promise.all([plain('/api/lapkehoach/diem-den?q=' + encodeURIComponent(q)), plain('/api/lapkehoach/dia-diem?q=' + encodeURIComponent(q)).catch(function () { return []; })]).then(function (both) {
        var rows = both[0].slice(0, 8).concat((both[1] || []).slice(0, 5).map(function (g) { return { name: g.name, path: g.secondary || g.description, source: 'goong', level: 0 }; }));
        sugData = rows;
        $('pe-dest-sug').innerHTML = sugData.length ? sugData.map(function (r, n) { return '<li role="option" data-n="' + n + '"><b>' + esc(r.name) + '</b><small>' + esc(levelLabel(r)) + ' · ' + esc(r.path || '') + '</small></li>'; }).join('') : '<li class="none">Không có gợi ý. Bấm Enter để dùng nguyên chữ vừa gõ.</li>';
        $('pe-dest-sug').hidden = false; $('pe-dest').setAttribute('aria-expanded', 'true');
      }).catch(hideSug);
    }, 250);
  });
  $('pe-dest').addEventListener('keydown', function (e) { if (e.key === 'Enter') { e.preventDefault(); var v = this.value.trim(); if (v) { addDest({ name: v }); this.value = ''; hideSug(); } } if (e.key === 'Escape') hideSug(); });
  $('pe-dest-sug').addEventListener('mousedown', function (e) { var li = e.target.closest('[data-n]'); if (!li) return; e.preventDefault(); var r = sugData[+li.dataset.n]; addDest({ name: r.name, areaId: r.areaId, path: r.path }); $('pe-dest').value = ''; hideSug(); });
  $('pe-dest').addEventListener('blur', function () { setTimeout(hideSug, 150); });

  // hop thoai chon diem den chi tiet
  var provLoaded = false;
  var fillSel = function (sel, items, ph) { sel.innerHTML = '<option value="">' + ph + '</option>' + items.map(function (x) { return '<option value="' + x.id + '" data-n="' + esc(x.name || x.fullName) + '">' + esc(x.name || x.fullName) + '</option>'; }).join(''); sel.disabled = items.length === 0; };
  var adminPick = function () { var w = $('pe-d-ward'), d = $('pe-d-dist'), p = $('pe-d-prov'); var s = w.value ? w : d.value ? d : p.value ? p : null; return s ? { id: +s.value, name: s.options[s.selectedIndex].dataset.n, path: [p, d, w].filter(function (x) { return x.value; }).map(function (x) { return x.options[x.selectedIndex].dataset.n; }).reverse().join(', ') } : null; };
  var refreshAdmin = function () { var a = adminPick(); $('pe-d-picked').textContent = a ? 'Đã chọn: ' + a.path : ''; $('pe-d-add').disabled = !a; };
  var children = function (id, sel, ph) { return plain('/api/dia-gioi/' + id + '/con').then(function (r) { fillSel(sel, r.items || [], ph); }); };
  $('pe-open-dest').addEventListener('click', function () {
    $('pe-destdlg').showModal();
    if (!provLoaded) plain('/api/dia-gioi/tinh').then(function (r) { fillSel($('pe-d-prov'), r.items || [], 'Chọn…'); provLoaded = true; }).catch(function (e) { toast(e.message); });
  });
  $('pe-d-prov').addEventListener('change', function () { fillSel($('pe-d-dist'), [], 'Chọn…'); fillSel($('pe-d-ward'), [], 'Chọn…'); if (this.value) children(this.value, $('pe-d-dist'), 'Tất cả quận/huyện'); refreshAdmin(); });
  $('pe-d-dist').addEventListener('change', function () { fillSel($('pe-d-ward'), [], 'Chọn…'); if (this.value) children(this.value, $('pe-d-ward'), 'Tất cả phường/xã'); refreshAdmin(); });
  $('pe-d-ward').addEventListener('change', refreshAdmin);
  $('pe-d-add').addEventListener('click', function () { var a = adminPick(); if (!a) return; addDest({ name: a.name, areaId: a.id, path: a.path }); toast('Đã thêm “' + a.name + '”.'); });
  $('pe-dest-tabs').addEventListener('click', function (e) {
    var b = e.target.closest('[data-dtab]'); if (!b) return;
    [].forEach.call($('pe-dest-tabs').children, function (c) { c.classList.toggle('on', c === b); });
    $('pe-dtab-admin').hidden = b.dataset.dtab !== 'admin'; $('pe-dtab-places').hidden = b.dataset.dtab !== 'places';
    if (b.dataset.dtab === 'places') loadPlaces();
  });
  var placeTimer = null;
  var loadPlaces = function () {
    var q = $('pe-places-q').value.trim(), g = $('pe-places-grid'); g.innerHTML = '<p class="help">Đang tải…</p>';
    plain('/api/lapkehoach/diem-den?q=' + encodeURIComponent(q)).then(function (rows) {
      rows = rows.filter(function (r) { return r.source !== 'area'; });
      g.innerHTML = rows.length ? rows.map(function (r, n) { return '<button type="button" class="pe-place" data-pn="' + n + '">' + (r.photo ? '<img src="' + esc(r.photo) + '" alt="" loading="lazy"/>' : '') + '<strong>' + esc(r.name) + '</strong><small>' + esc(r.path || '') + '</small>' + (r.summary ? '<span>' + esc(r.summary) + '</span>' : '') + '</button>'; }).join('') : '<p class="help">Không tìm thấy khu du lịch phù hợp.</p>';
      g._rows = rows;
    }).catch(function (e) { g.innerHTML = '<p class="help">' + esc(e.message) + '</p>'; });
  };
  $('pe-places-q').addEventListener('input', function () { clearTimeout(placeTimer); placeTimer = setTimeout(loadPlaces, 300); });
  $('pe-places-grid').addEventListener('click', function (e) { var b = e.target.closest('[data-pn]'); if (!b) return; var r = this._rows[+b.dataset.pn]; addDest({ name: r.name, areaId: r.areaId, path: r.path }); toast('Đã thêm “' + r.name + '”.'); });
  var dmap = null;
  $('pe-open-map').addEventListener('click', function () {
    $('pe-map').showModal();
    plain('/api/lapkehoach/ban-do').then(function (pts) {
      if (!window.L) { $('pe-map-canvas').textContent = 'Không tải được bản đồ.'; return; }
      if (dmap) { dmap.remove(); dmap = null; }
      dmap = L.map('pe-map-canvas', { maxBounds: VN_BOUNDS, maxBoundsViscosity: 1 }).setView([16.1, 106.3], 5); addTiles(dmap); limitToVn(dmap);
      var b = [];
      pts.forEach(function (p) { var m = L.marker([p.lat, p.lng]).addTo(dmap); m.bindTooltip(p.name + ' · ' + p.title); m.on('click', function () { addDest({ name: p.name }); toast('Đã thêm “' + p.name + '”.'); }); b.push([p.lat, p.lng]); });
      if (b.length) dmap.fitBounds(b, { padding: [30, 30], maxZoom: 9 });
      dmap.on('click', function (ev) {
        var pop = L.popup({ minWidth: 200 }).setLatLng(ev.latlng).setContent('<div class="pe-pop">Đang tìm tên khu vực…</div>').openOn(dmap);
        plain('/api/lapkehoach/dia-chi?lat=' + ev.latlng.lat.toFixed(5) + '&lng=' + ev.latlng.lng.toFixed(5)).then(function (r) {
          var name = (r && (r.commune || r.province)) || '', path = r && r.province && r.commune ? r.province : '';
          if (!name) { pop.setContent('<div class="pe-pop">Không tìm được tên khu vực ở đây.</div>'); return; }
          var box = document.createElement('div'); box.className = 'pe-pop';
          box.innerHTML = '<b>' + esc(name) + '</b>' + (path ? '<br><small>' + esc(path) + '</small>' : '') + '<br><button type="button" class="btn btn-dark" style="margin-top:6px">Thêm điểm đến này</button>';
          box.querySelector('button').onclick = function () { addDest({ name: name, path: path }); dmap.closePopup(); toast('Đã thêm “' + name + '”.'); };
          pop.setContent(box);
        }).catch(function (e) { pop.setContent('<div class="pe-pop">' + esc(e.message) + '</div>'); });
      });
      setTimeout(function () { dmap.invalidateSize(); limitToVn(dmap); }, 80);
    }).catch(function (e) { $('pe-map-canvas').textContent = e.message; });
  });
  [].forEach.call(document.querySelectorAll('dialog [data-close]'), function (b) { b.addEventListener('click', function () { b.closest('dialog').close(); }); });

  // ---------- thong tin chung
  var days = function () { return Math.max(1, Math.min(30, parseInt($('pe-days').value, 10) || 1)); };
  var refreshInfo = function () {
    var n = days(), s = $('pe-start').value;
    $('pe-range').textContent = s ? 'Đi ' + fmt(s) + ' → về ' + fmt(shift(s, n - 1)) + ' · ' + (n - 1) + ' đêm' : '';
    var dest = st.dests.length ? st.dests[0].name + (st.dests.length > 1 ? ' và ' + (st.dests.length - 1) + ' nơi khác' : '') : '', sug = $('pe-name-suggest');
    if (!$('pe-name').value.trim() && st.dests.length) { var label = (st.dests.length > 1 ? st.dests.map(function (d) { return d.name; }).slice(0, 3).join(' – ') : st.dests[0].name) + ' ' + n + ' ngày'; $('pe-name-chip').textContent = 'Dùng tên: ' + label; $('pe-name-chip').dataset.v = label; sug.hidden = false; } else sug.hidden = true;
  };
  var budgetChips = function () {
    var opts = [['Chưa đặt', ''], ['2 triệu', 2000000], ['5 triệu', 5000000], ['10 triệu', 10000000], ['Khác', 'x']], other = $('pe-budget');
    var b = st.budget, preset = opts.some(function (o) { return o[1] === b; });
    $('pe-budget-chips').innerHTML = opts.map(function (o) { var on = o[1] === 'x' ? (b != null && !preset) : (b == null ? o[1] === '' : o[1] === b); return '<button type="button" class="pe-chip' + (on ? ' on' : '') + '" data-b="' + o[1] + '"' + (isOwner ? '' : ' disabled') + '>' + o[0] + '</button>'; }).join('');
    other.hidden = !(b != null && !preset); if (!other.hidden) other.value = b;
  };
  var startChips = function () {
    var t = new Date(today + 'T00:00:00'), toSat = (6 - t.getDay() + 7) % 7 || 7, first = new Date(t.getFullYear(), t.getMonth() + 1, 1);
    var opts = [['Ngày mai', shift(today, 1), 0], ['Cuối tuần này', shift(today, toSat), 2], ['Tuần sau', shift(today, 7), 0], ['Đầu tháng sau', local(first), 0]];
    $('pe-start-chips').innerHTML = opts.map(function (o) { return '<button type="button" class="pe-chip" data-s="' + o[1] + '" data-d="' + o[2] + '"' + (isOwner ? '' : ' disabled') + '>' + o[0] + '</button>'; }).join('');
  };
  $('pe-start-chips').addEventListener('click', function (e) { var b = e.target.closest('[data-s]'); if (!b) return; $('pe-start').value = b.dataset.s; if (+b.dataset.d && st.items.length === 0) $('pe-days').value = b.dataset.d; touch(); syncDays(); });
  $('pe-budget-chips').addEventListener('click', function (e) {
    var b = e.target.closest('[data-b]'); if (!b) return; var v = b.dataset.b;
    if (v === 'x') { st.budget = st.budget == null ? 0 : st.budget; } else st.budget = v === '' ? null : +v;
    touch(); budgetChips(); if (v === 'x') { $('pe-budget').hidden = false; $('pe-budget').focus(); } summary();
  });
  $('pe-budget').addEventListener('input', function () { st.budget = this.value === '' ? null : Math.max(0, +this.value); touch(); summary(); });
  $('pe-name-chip').addEventListener('click', function () { $('pe-name').value = this.dataset.v; touch(); refreshInfo(); });
  $('pe-name').addEventListener('input', function () { touch(); refreshInfo(); });
  var syncDays = function () {
    var n = days(), over = st.items.filter(function (i) { return i.day > n; });
    if (over.length) { showError('Ngày ' + (n + 1) + ' trở đi còn ' + over.length + ' hạng mục. Hãy chuyển hoặc xóa chúng trước khi giảm số ngày.'); $('pe-days').value = st.days; return; }
    showError(''); $('pe-days').value = n; st.days = n; touch(); refreshInfo(); renderDay(); summary();
  };
  $('pe-days').addEventListener('change', syncDays);
  $('pe-start').addEventListener('change', function () { touch(); refreshInfo(); renderDay(); });
  $('pe-days-minus').addEventListener('click', function () { $('pe-days').value = Math.max(1, days() - 1); syncDays(); });
  $('pe-days-plus').addEventListener('click', function () { $('pe-days').value = Math.min(30, days() + 1); syncDays(); });

  // ---------- cac ngay
  var sorted = function (d) { return st.items.filter(function (i) { return i.day === d; }).sort(function (a, b) { var x = a.time || '99:99', y = b.time || '99:99'; return x < y ? -1 : x > y ? 1 : 0; }); };
  var find = function (k) { return st.items.filter(function (i) { return i.k === k; })[0]; };
  var renderDay = function () {
    var n = days(), s0 = $('pe-start').value, h = '';
    for (var d = 1; d <= n; d++) {
      var items = sorted(d), total = items.reduce(function (s, i) { return s + (i.cost || 0); }, 0), note = st.notes[d] || '';
      var others = []; for (var o = 1; o <= n; o++) if (o !== d) others.push(o);
      h += '<article class="pe-daycard" data-day="' + d + '" id="pe-day-' + d + '"><header><h3>Ngày ' + d + (s0 ? ' <small>' + fmt(shift(s0, d - 1)) + '</small>' : '') + '</h3><span class="pe-daytotal">' + (items.length ? items.length + ' mục · ' + money(total) : 'Chưa có gì') + '</span></header>';
      if (items.length) h += '<ul class="pe-items">' + items.map(function (i) {
        return '<li data-k="' + i.k + '"><time>' + (i.time || '—') + '</time><span class="pe-c"><b>' + esc(i.content) + '</b>' + (i.kind && GROUPS[i.kind] ? '<em>' + GROUPS[i.kind] + '</em>' : (i.placeId ? '<em>Địa điểm</em>' : '')) + '</span><span class="pe-cost">' + (i.cost ? money(i.cost) : '') + '</span><button type="button" class="pe-ib" data-edit aria-label="Sửa ' + esc(i.content) + '">Sửa</button><button type="button" class="pe-ib pe-del" data-del aria-label="Xóa ' + esc(i.content) + '">Xóa</button></li>';
      }).join('') + '</ul>';
      h += '<div class="pe-note-wrap"' + (note ? '' : ' hidden') + '><label class="visually-hidden" for="pe-note-' + d + '">Ghi chú ngày ' + d + '</label><textarea class="form-control pe-note" id="pe-note-' + d + '" data-note maxlength="1000" rows="2" placeholder="Ghi chú cho ngày này: lưu ý, việc cần nhớ…">' + esc(note) + '</textarea></div>';
      var more = '';
      if (items.length) more = '<details class="pe-more"><summary>Khác</summary><div class="pe-more-body">' +
        (others.length ? '<label>Chép sang <select data-copyto>' + others.map(function (x) { return '<option value="' + x + '">Ngày ' + x + '</option>'; }).join('') + '</select></label><button type="button" class="btn btn-outline" data-act="copy">Chép</button>' : '') +
        '<button type="button" class="btn btn-outline" data-act="clear">Xóa hết ngày này</button></div></details>';
      h += '<div class="pe-dayacts"><button type="button" class="btn btn-outline" data-act="service">+ Thêm dịch vụ</button><button type="button" class="btn btn-outline" data-act="manual">+ Thêm hoạt động</button><button type="button" class="btn btn-outline" data-act="note">' + (note ? 'Ghi chú' : '+ Ghi chú') + '</button>' + more + '</div></article>';
    }
    $('pe-days-list').innerHTML = h;
  };
  $('pe-days-list').addEventListener('input', function (e) { var t = e.target.closest('[data-note]'); if (!t) return; st.notes[+t.closest('.pe-daycard').dataset.day] = t.value; touch(); });
  $('pe-days-list').addEventListener('click', function (e) {
    var t = e.target.closest('button'); if (!t) return; var card = t.closest('.pe-daycard'); if (!card) return; var d = +card.dataset.day; cur = d;
    var act = t.dataset.act;
    if (act === 'service') return openPicker('add');
    if (act === 'manual') return openPicker('manual');
    if (act === 'note') { var w = card.querySelector('.pe-note-wrap'); w.hidden = !w.hidden; if (!w.hidden) w.querySelector('textarea').focus(); return; }
    if (act === 'copy') { var to = +card.querySelector('[data-copyto]').value; sorted(d).forEach(function (i) { st.items.push({ k: 'k' + (++seq), id: 0, day: to, time: i.time, content: i.content, cost: i.cost, serviceId: i.serviceId, placeId: i.placeId, kind: i.kind }); }); touch(); toast('Đã chép sang Ngày ' + to + '.'); return refresh(); }
    if (act === 'clear') { if (!confirm('Xóa hết hạng mục trong Ngày ' + d + '?')) return; st.items = st.items.filter(function (i) { return i.day !== d; }); touch(); return refresh(); }
    var li = t.closest('li'); if (!li) return; var it = find(li.dataset.k); if (!it) return;
    if (t.hasAttribute('data-del')) { var idx = st.items.indexOf(it); st.items.splice(idx, 1); touch(); refresh(); undoBar('Đã xóa “' + it.content + '”.', function () { st.items.splice(idx, 0, it); refresh(); }); }
    if (t.hasAttribute('data-edit')) openEdit(it);
  });

  // ---------- them dich vu: day du bo loc cua Kham pha, tren ban do
  var nightsFrom = function (day) { return Math.max(days() - day, 1); };
  var pmap = null, pLayer = null, pMarkers = [], pickData = [];
  var COL_SAVED = '#e67e22', COL_EXPLORE = '#2447e8', COL_GOONG = '#2b8a3e', COL_SEL = '#c0392b';
  var fillPickDest = function () {
    var sel = $('pe-pick-dest'), cur0 = sel.value;
    sel.innerHTML = st.dests.length ? st.dests.map(function (d, i) { return '<option value="' + i + '">' + esc(d.name) + '</option>'; }).join('') : '<option value="">Chưa chọn điểm đến (tìm toàn quốc)</option>';
    if (cur0 && sel.querySelector('option[value="' + cur0 + '"]')) sel.value = cur0;
  };
  var openPicker = function (m, key) {
    mode = m; swapKey = key || null; var dlg = $('pe-picker'); picked = null;
    $('pe-picker-title').textContent = m === 'swap' ? 'Đổi dịch vụ khác' : m === 'manual' ? 'Thêm hoạt động vào Ngày ' + cur : 'Thêm dịch vụ vào Ngày ' + cur;
    var manual = m === 'manual';
    $('pe-pick-grid').hidden = manual; $('pe-manual').hidden = !manual; dlg.classList.toggle('pe-narrow', manual);
    $('pe-confirm').hidden = true; $('pe-pick-list-wrap').hidden = false;
    if (!dlg.open) dlg.showModal();
    if (manual) { timeOptions($('pe-m-time'), ''); $('pe-m-name').value = ''; $('pe-m-cost').value = ''; slotChips($('pe-m-costchips'), $('pe-m-time')); $('pe-m-name').focus(); return; }
    fillPickDest(); $('pe-pick-q').value = ''; initPickMap(); loadPick(true); $('pe-pick-q').focus();
  };
  var initPickMap = function () {
    if (!window.L) return;
    if (!pmap) { pmap = L.map('pe-pick-map', { maxBounds: VN_BOUNDS, maxBoundsViscosity: 1 }).setView([16.1, 106.3], 5); addTiles(pmap); pLayer = L.layerGroup().addTo(pmap); }
    setTimeout(function () { pmap.invalidateSize(); limitToVn(pmap); }, 80);
  };
  var styleMarker = function (mk, on) { mk.setStyle({ color: on ? COL_SEL : '#fff', fillColor: on ? COL_SEL : mk.baseColor, fillOpacity: .95, weight: on ? 3 : 2, radius: on ? 12 : 9 }); };
  var buildQuery = function (page) {
    var p = new URLSearchParams(); var q = $('pe-pick-q').value.trim(); if (q) p.set('Q', q);
    if (pickKind) p.set('Type', pickKind); else if (pickGroup) p.set('Group', pickGroup);
    var di = $('pe-pick-dest').value; if (di !== '' && st.dests[+di]) { var d = st.dests[+di]; if (d.areaId) p.set('Area', d.areaId); else p.set('Region', d.name); }
    if ($('pe-f-min').value) p.set('Min', $('pe-f-min').value); if ($('pe-f-max').value) p.set('Max', $('pe-f-max').value);
    var sort = $('pe-f-sort').value; if (sort && sort !== 'relevance') p.set('Sort', sort);
    if ($('pe-f-view').checked && pmap) { var b = pmap.getBounds(); p.set('South', b.getSouth()); p.set('West', b.getWest()); p.set('North', b.getNorth()); p.set('East', b.getEast()); }
    if ($('pe-f-saved').checked) p.set('daLuu', 'true');
    p.set('Page', page); return p.toString();
  };
  $('pe-groups').addEventListener('click', function (e) { var b = e.target.closest('[data-group]'); if (!b) return; pickGroup = b.dataset.group; pickKind = ''; [].forEach.call($('pe-groups').children, function (c) { c.classList.toggle('on', c === b); }); renderKinds(); loadPick(true); });
  var allKinds = []; try { allKinds = JSON.parse($('pe-kinds').dataset.kinds || '[]'); } catch (x) { allKinds = []; }
  var renderKinds = function () {
    var box = $('pe-kinds'), list = pickGroup ? allKinds.filter(function (k) { return k.group === pickGroup; }) : [];
    box.hidden = list.length < 2; if (box.hidden) { box.innerHTML = ''; return; }
    box.innerHTML = '<button type="button" class="pe-chip on" data-kind="">Tất cả loại</button>' + list.map(function (k) { return '<button type="button" class="pe-chip" data-kind="' + esc(k.code) + '">' + esc(k.label) + '</button>'; }).join('');
  };
  $('pe-kinds').addEventListener('click', function (e) { var b = e.target.closest('[data-kind]'); if (!b) return; pickKind = b.dataset.kind; [].forEach.call($('pe-kinds').children, function (c) { c.classList.toggle('on', c === b); }); loadPick(true); });
  $('pe-pick-q').addEventListener('input', function () { clearTimeout(pickTimer); pickTimer = setTimeout(function () { loadPick(true); }, 350); });
  ['pe-pick-dest', 'pe-f-min', 'pe-f-max', 'pe-f-sort', 'pe-f-saved'].forEach(function (id) { $(id).addEventListener('change', function () { loadPick(true); }); });
  $('pe-f-view').addEventListener('change', function () { loadPick(true, true); });
  $('pe-pick-more').addEventListener('click', function () { loadPick(false); });
  var card = function (s, n) {
    var price = s.priceFrom != null ? money(s.priceFrom) + (s.priceTo != null && s.priceTo > s.priceFrom ? ' – ' + money(s.priceTo) : '') + (s.unit ? '/' + esc(s.unit) : '') : 'Liên hệ';
    return '<article class="pe-card-sm' + (s.saved ? ' saved' : '') + '" data-n="' + n + '">' + (s.photo ? '<img class="pe-thumb" src="' + esc(s.photo) + '" alt="" loading="lazy"/>' : '') +
      '<div class="pe-card-body"><strong>' + esc(s.name) + '</strong><small>' + esc(s.kindLabel) + (GROUPS[s.group] ? ' · ' + GROUPS[s.group] : '') + ' · ' + esc(s.region) + (s.rating ? ' · ★ ' + s.rating.toFixed(1) + ' (' + s.reviews + ')' : '') + (s.lat == null ? ' · chưa có vị trí trên bản đồ' : '') + '</small>' +
      (s.summary ? '<p>' + esc(s.summary) + '</p>' : '') + (s.hours ? '<small>Giờ: ' + esc(s.hours) + '</small>' : '') + (s.address ? '<small>Địa chỉ: ' + esc(s.address) + '</small>' : '') + (s.contact ? '<small>Liên hệ: ' + esc(s.contact) + '</small>' : '') + '<b>' + price + '</b>' +
      '<div class="pe-detail" hidden></div></div><div class="pe-card-act"><button type="button" class="btn btn-dark" data-pick="' + n + '">Chọn</button><button type="button" class="btn btn-outline" data-detail="' + n + '">Chi tiết</button></div></article>';
  };
  var loadOther = function () {
    if ($('pe-f-saved').checked) return;
    var q = $('pe-pick-q').value.trim(), di = $('pe-pick-dest').value, dest = di !== '' && st.dests[+di] ? st.dests[+di].name : '';
    if (q.length < 2 && !pickGroup) return;
    plain('/api/lapkehoach/goong-goi-y?group=' + encodeURIComponent(pickGroup) + '&kind=' + encodeURIComponent(pickKind) + '&destination=' + encodeURIComponent(dest) + '&q=' + encodeURIComponent(q)).then(function (rows) {
      if (!rows || !rows.length || $('pe-pick-q').value.trim() !== q) return;
      var box = $('pe-pick-list'), h = '<h4 class="pe-pick-sec goong" data-sec="goong">Gợi ý từ bản đồ Goong (chưa có trong dữ liệu website, thêm như hoạt động tự nhập)</h4>';
      rows.forEach(function (g) {
        var item = { entryId: null, serviceId: null, placeId: null, name: g.name, kind: 'Place', kindLabel: 'Địa điểm', group: pickGroup && pickGroup !== 'Info' ? pickGroup : 'Place', region: g.address || '', summary: null, address: g.address || null, hours: null, contact: null, priceFrom: null, priceTo: null, unit: null, photo: null, lat: g.lat, lng: g.lng, rating: null, reviews: 0, saved: false, goong: true, url: null };
        pickData.push(item); var n = pickData.length - 1;
        h += '<article class="pe-card-sm goong" data-n="' + n + '"><div class="pe-card-body"><strong>' + esc(g.name) + '</strong><small>' + esc(g.address || '') + '</small></div><div class="pe-card-act"><button type="button" class="btn btn-outline" data-pick="' + n + '">Chọn</button></div></article>';
      });
      box.insertAdjacentHTML('beforeend', h + '<p class="help pe-credit">Dữ liệu địa điểm từ Goong. Giờ mở cửa và giá chưa được xác nhận.</p>');
      drawPins(true);
    }).catch(function () { });
  };
  var loadPick = function (reset, keepView) {
    var box = $('pe-pick-list'); var page = reset ? 1 : pickPage + 1;
    if (reset) { box.innerHTML = '<p class="help">Đang tải…</p>'; pickData = []; }
    plain('/api/lapkehoach/goi-y?' + buildQuery(page)).then(function (res) {
      pickPage = res.page; pickTotal = res.total; var start = pickData.length; pickData = pickData.concat(res.items);
      var h = '', sec = '';
      if (reset) box.innerHTML = '';
      res.items.forEach(function (s, i) {
        var n = start + i, want = s.saved ? 'Đã lưu của bạn' : 'Khám phá';
        var last = box.querySelector('.pe-pick-sec:last-of-type'); var lastSec = last ? last.dataset.sec : '';
        if (want !== lastSec && want !== sec) { sec = want; h += '<h4 class="pe-pick-sec' + (s.saved ? ' saved' : '') + '" data-sec="' + want + '">' + want + '</h4>'; }
        h += card(s, n);
      });
      box.insertAdjacentHTML('beforeend', h);
      if (!pickData.length) box.innerHTML = '<p class="help">Chưa có kết quả phù hợp. Thử bỏ bớt từ khóa hoặc bộ lọc, đổi nhóm, hoặc đóng lại và chọn “Thêm hoạt động” để tự nhập.</p>';
      $('pe-pick-count').textContent = pickData.length ? pickData.length + ' kết quả' + (res.hasMore ? '+' : '') : '';
      $('pe-pick-more').hidden = !res.hasMore;
      drawPins(keepView);
      if (reset) loadOther();
    }).catch(function (e) { box.innerHTML = '<p class="help">' + esc(e.message) + '</p>'; });
  };
  var drawPins = function (keepView) {
    if (!pmap) return; pLayer.clearLayers(); pMarkers = []; var b = [];
    pickData.forEach(function (s, n) {
      if (s.lat == null) return;
      var mk = L.circleMarker([s.lat, s.lng], { radius: 9 }); mk.baseColor = s.saved ? COL_SAVED : s.goong ? COL_GOONG : COL_EXPLORE; styleMarker(mk, false);
      mk.bindTooltip(s.name + (s.priceFrom != null ? ' · ' + money(s.priceFrom) : '')); mk.on('click', function () { selectItem(n); }); mk.addTo(pLayer); pMarkers[n] = mk; b.push([s.lat, s.lng]);
    });
    if (b.length && !keepView) pmap.fitBounds(b, { padding: [30, 30], maxZoom: 14 });
    $('pe-pick-nomap').hidden = b.length > 0 || pickData.length === 0;
  };
  $('pe-pick-list').addEventListener('mouseover', function (e) { var c = e.target.closest('[data-n]'); if (!c) return; var mk = pMarkers[+c.dataset.n]; if (mk) { styleMarker(mk, true); mk.bringToFront(); } });
  $('pe-pick-list').addEventListener('mouseout', function (e) { var c = e.target.closest('[data-n]'); if (!c) return; var mk = pMarkers[+c.dataset.n]; if (mk && (!picked || pickData[+c.dataset.n] !== picked)) styleMarker(mk, false); });
  var selectItem = function (n) {
    picked = pickData[n]; if (!picked) return;
    pMarkers.forEach(function (mk, i) { if (mk) styleMarker(mk, i === n); });
    if (pMarkers[n] && pmap) pmap.panTo(pMarkers[n].getLatLng());
    var day = mode === 'swap' ? find(swapKey).day : cur, stay = picked.group === 'Stay';
    $('pe-c-name').textContent = picked.name; $('pe-c-meta').textContent = (picked.saved ? 'Đã lưu · ' : '') + picked.kindLabel + (picked.priceFrom != null ? ' · ' + money(picked.priceFrom) + (picked.unit ? '/' + picked.unit : '') : '') + (stay ? ' · tính ' + nightsFrom(day) + ' đêm' : '');
    var dsel = $('pe-c-day'); dsel.innerHTML = ''; for (var d = 1; d <= days(); d++) dsel.innerHTML += '<option value="' + d + '">Ngày ' + d + '</option>'; dsel.value = day; dsel.disabled = mode === 'swap';
    timeOptions($('pe-c-time'), mode === 'swap' ? (find(swapKey).time || '') : (stay ? '14:00' : '')); slotChips($('pe-c-slots'), $('pe-c-time'));
    $('pe-c-cost').value = picked.priceFrom != null ? (stay ? picked.priceFrom * nightsFrom(day) : picked.priceFrom) : '';
    $('pe-c-day').onchange = function () { if (stay && picked.priceFrom != null) $('pe-c-cost').value = picked.priceFrom * nightsFrom(+this.value); };
    $('pe-confirm').hidden = false; [].forEach.call($('pe-pick-list').querySelectorAll('.pe-card-sm'), function (c) { c.classList.toggle('picked', +c.dataset.n === n); });
  };
  $('pe-pick-list').addEventListener('click', function (e) {
    var b = e.target.closest('[data-pick]'); if (b) return selectItem(+b.dataset.pick);
    var dtl = e.target.closest('[data-detail]'); if (!dtl) return;
    var n = +dtl.dataset.detail, s = pickData[n], panel = dtl.closest('.pe-card-sm').querySelector('.pe-detail');
    if (!panel.hidden) { panel.hidden = true; dtl.textContent = 'Chi tiết'; return; }
    panel.hidden = false; dtl.textContent = 'Thu gọn';
    if (panel.dataset.loaded) return;
    var basic = function () { panel.innerHTML = '<p>' + esc(s.summary || 'Chưa có mô tả chi tiết.') + '</p>' + (s.url ? '<a href="' + esc(s.url) + '" target="_blank" rel="noopener">Mở trang đầy đủ</a>' : ''); panel.dataset.loaded = '1'; };
    if (!s.entryId) return basic();
    panel.innerHTML = '<p class="help">Đang tải…</p>';
    plain('/api/lapkehoach/chi-tiet/' + s.entryId).then(function (d) {
      var rows = [['Giới thiệu', d.body || d.summary], ['Giờ mở cửa', [d.hours, d.openDays].filter(Boolean).join(' · ')], ['Thời lượng gợi ý', d.duration ? d.duration + ' phút' : ''], ['Địa chỉ', d.address], ['Liên hệ', d.contact], ['Đã gồm', d.includes], ['Chưa gồm', d.excludes], ['Nguồn', d.source ? d.source + (d.checkedOn ? ' · kiểm tra ' + d.checkedOn : '') : '']];
      var h = rows.filter(function (r) { return r[1]; }).map(function (r) { return '<p><b>' + r[0] + ':</b> ' + esc(r[1]) + '</p>'; }).join('');
      if (d.prices && d.prices.length) h += '<p><b>Bảng giá:</b></p><ul>' + d.prices.map(function (p) { return '<li>' + esc(p.label) + (p.amount != null ? ': ' + money(p.amount) + (p.unit ? '/' + esc(p.unit) : '') : '') + (p.conditions ? ' (' + esc(p.conditions) + ')' : '') + '</li>'; }).join('') + '</ul>';
      if (d.url) h += '<a href="' + esc(d.url) + '" target="_blank" rel="noopener">Mở trang đầy đủ</a>';
      panel.innerHTML = h || '<p class="help">Chưa có thêm thông tin.</p>'; panel.dataset.loaded = '1';
    }).catch(basic);
  });
  $('pe-c-back').addEventListener('click', function () { picked = null; $('pe-confirm').hidden = true; [].forEach.call($('pe-pick-list').querySelectorAll('.picked'), function (c) { c.classList.remove('picked'); }); pMarkers.forEach(function (mk) { if (mk) styleMarker(mk, false); }); });
  $('pe-c-add').addEventListener('click', function () {
    var cost = $('pe-c-cost').value === '' ? null : Math.max(0, +$('pe-c-cost').value), time = $('pe-c-time').value || null, day = +$('pe-c-day').value;
    var kind = picked.group === 'Other' ? null : picked.group;
    if (mode === 'swap') { var it = find(swapKey); it.content = picked.name; it.serviceId = picked.serviceId || null; it.placeId = picked.placeId || null; it.kind = kind; it.cost = cost; it.time = time; $('pe-picker').close(); toast('Đã đổi dịch vụ.'); }
    else { st.items.push({ k: 'k' + (++seq), id: 0, day: day, time: time, content: picked.name, cost: cost, serviceId: picked.serviceId || null, placeId: picked.placeId || null, kind: kind }); toast('Đã thêm “' + picked.name + '” vào Ngày ' + day + '.'); cur = day; picked = null; $('pe-confirm').hidden = true; [].forEach.call($('pe-pick-list').querySelectorAll('.picked'), function (c) { c.classList.remove('picked'); }); pMarkers.forEach(function (mk) { if (mk) styleMarker(mk, false); }); }
    touch(); refresh();
  });
  $('pe-manual').addEventListener('submit', function (e) {
    e.preventDefault(); var name = $('pe-m-name').value.trim(); if (!name) { $('pe-m-name').focus(); return toast('Nhập tên hoạt động.'); }
    st.items.push({ k: 'k' + (++seq), id: 0, day: cur, time: $('pe-m-time').value || null, content: name, cost: $('pe-m-cost').value === '' ? null : Math.max(0, +$('pe-m-cost').value), serviceId: null, placeId: null, kind: null });
    touch(); toast('Đã thêm “' + name + '” vào Ngày ' + cur + '.'); $('pe-picker').close(); refresh();
  });

  // ---------- sua hang muc
  var editing = null;
  var openEdit = function (it) {
    editing = it; var dsel = $('pe-e-day'); dsel.innerHTML = ''; for (var d = 1; d <= days(); d++) dsel.innerHTML += '<option value="' + d + '">Ngày ' + d + '</option>';
    dsel.value = it.day; timeOptions($('pe-e-time'), it.time || ''); $('pe-e-name').value = it.content; $('pe-e-cost').value = it.cost == null ? '' : it.cost;
    var linked = !!(it.serviceId || it.placeId); $('pe-e-name').disabled = linked; $('pe-e-note').textContent = linked ? 'Hạng mục lấy từ dịch vụ/địa điểm. Muốn đổi, bấm “Đổi dịch vụ khác”.' : '';
    $('pe-edit').showModal();
  };
  $('pe-edit-form').addEventListener('submit', function (e) {
    e.preventDefault(); var name = $('pe-e-name').value.trim(); if (!name) return toast('Tên hạng mục không được trống.');
    editing.content = name; editing.day = +$('pe-e-day').value; editing.time = $('pe-e-time').value || null; editing.cost = $('pe-e-cost').value === '' ? null : Math.max(0, +$('pe-e-cost').value);
    cur = editing.day; $('pe-edit').close(); touch(); refresh();
  });
  $('pe-e-swap').addEventListener('click', function () { $('pe-edit').close(); openPicker('swap', editing.k); });

  // ---------- tam tinh + canh bao
  var warnings = function () {
    var w = [], n = days(), total = st.items.reduce(function (s, i) { return s + (i.cost || 0); }, 0);
    if (st.items.length === 0) return { list: [], total: 0 };
    if (st.budget != null && st.budget > 0 && total > st.budget) w.push({ t: 'Tạm tính vượt ngân sách ' + money(total - st.budget) + '.', d: 0 });
    for (var d = 1; d <= n; d++) if (!st.items.some(function (i) { return i.day === d; })) w.push({ t: 'Ngày ' + d + ' chưa có hạng mục nào.', d: d });
    for (var d2 = 1; d2 <= n; d2++) { var seen = {}; st.items.filter(function (i) { return i.day === d2 && i.time; }).forEach(function (i) { if (seen[i.time]) w.push({ t: 'Ngày ' + d2 + ': hai hạng mục cùng lúc ' + i.time + '.', d: d2 }); seen[i.time] = 1; }); }
    if (n >= 2) for (var night = 1; night < n; night++) if (!st.items.some(function (i) { return i.kind === 'Stay' && i.day <= night; })) w.push({ t: 'Đêm ' + night + ' (Ngày ' + night + ' sang Ngày ' + (night + 1) + ') chưa có chỗ ở.', d: night });
    return { list: w, total: total };
  };
  var summary = function () {
    var r = warnings(), b = st.budget;
    $('pe-total').textContent = money(r.total); $('pe-of').textContent = b ? 'trên ' + money(b) : 'chưa đặt ngân sách';
    var f = $('pe-meter-fill'); f.style.width = b ? Math.min(100, r.total / b * 100) + '%' : '0%'; f.className = b && r.total > b ? 'over' : '';
    var wb = $('pe-warnbtn'); wb.hidden = r.list.length === 0; wb.textContent = '⚠ ' + r.list.length + ' cảnh báo';
    $('pe-warnlist').innerHTML = r.list.map(function (x) { return '<li' + (x.d ? ' data-d="' + x.d + '"' : '') + '>' + esc(x.t) + '</li>'; }).join('');
    if (!r.list.length) $('pe-warnlist').hidden = true;
  };
  $('pe-warnbtn').addEventListener('click', function () { var l = $('pe-warnlist'); l.hidden = !l.hidden; this.setAttribute('aria-expanded', String(!l.hidden)); });
  $('pe-warnlist').addEventListener('click', function (e) { var li = e.target.closest('li[data-d]'); if (!li) return; var c = $('pe-day-' + li.dataset.d); if (c) c.scrollIntoView({ behavior: 'smooth', block: 'center' }); $('pe-warnlist').hidden = true; });
  var refresh = function () { renderDay(); summary(); };

  // ---------- luu
  $('pe-save').addEventListener('click', function () {
    var btn = this; showError('');
    var pending = $('pe-dest').value.trim(); if (pending) { addDest({ name: pending }); $('pe-dest').value = ''; }
    var name = $('pe-name').value.trim(), dest = destText(), start = $('pe-start').value;
    if (!name) { $('pe-name').focus(); return showError('Nhập tên chuyến đi.'); }
    if (!dest) { $('pe-dest').focus(); return showError('Chọn ít nhất một điểm đến.'); }
    if (!start) { $('pe-start').focus(); return showError('Chọn ngày đi.'); }
    var body = { id: st.id, name: name, destination: dest, start: start, days: days(), budget: st.budget, notes: Object.keys(st.notes).filter(function (k) { return +k <= days() && (st.notes[k] || '').trim(); }).map(function (k) { return { day: +k, text: st.notes[k] }; }), items: st.items.map(function (i) { return { id: i.id || 0, day: i.day, time: i.time || null, content: i.content, cost: i.cost == null ? null : i.cost, serviceId: i.serviceId || null, placeId: i.placeId || null }; }) };
    btn.disabled = true; btn.textContent = 'Đang lưu…';
    api('/api/lapkehoach', 'POST', body).then(function (r) { dirty = false; location.href = '/Plans/Detail/' + r.id; }).catch(function (e) { btn.disabled = false; btn.textContent = 'Lưu'; showError(e.message); });
  });
  window.addEventListener('beforeunload', function (e) { if (dirty) { e.preventDefault(); e.returnValue = ''; } });
  $('pe-cancel').addEventListener('click', function (e) { if (dirty && !confirm('Bỏ các thay đổi chưa lưu?')) e.preventDefault(); else dirty = false; });

  // ---------- khoi tao
  $('pe-name').value = st.name || ''; $('pe-start').value = st.start || ''; $('pe-days').value = st.days || 3; st.days = days();
  if (st.budget === undefined) st.budget = null;
  budgetChips(); startChips(); renderDests(); refresh();
})();
