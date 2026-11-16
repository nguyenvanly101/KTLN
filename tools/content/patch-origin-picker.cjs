// O diem xuat phat/ket thuc trong trang lap ke hoach: goi y tu du lieu website + Goong, bao ro khi Goong qua tai,
// chon tren ban do, hien trang thai da co vi tri, chan luu khi go ten ma chua chon vi tri. Chay mot lan.
const fs = require('fs');
const f = 'src/HoTroDuLich.Web/wwwroot/js/plan-editor.js';
let s = fs.readFileSync(f, 'utf8'); const crlf = s.includes('\r\n'); s = s.replace(/\r\n/g, '\n');
const a = s.indexOf('  // ---------- diem xuat phat / ket thuc'), b = s.indexOf('  // ---------- khoi tao');
if (a < 0 || b < a) throw Error('Khong tim thay khoi diem xuat phat');
const block = `  // ---------- diem xuat phat / ket thuc
  var pointPicker = function (key, inputId, listId, stateId) {
    var input = $(inputId), list = $(listId), state = $(stateId), timer = 0, token = 0, hits = [];
    var close = function () { list.hidden = true; list.innerHTML = ''; hits = []; };
    var paint = function () {
      var typed = input.value.trim();
      if (st[key]) state.innerHTML = '<span class="pe-ok">✓ Đã có vị trí, dùng được để tính đường đi.</span>' + (isOwner ? ' <button type="button" class="pe-link" data-clear>Xóa</button>' : '');
      else if (typed) state.innerHTML = '<span class="pe-warn">Chưa có vị trí. Chọn một dòng gợi ý, hoặc bấm Chọn trên bản đồ.</span>';
      state.hidden = !st[key] && !typed;
    };
    var setPoint = function (p) { st[key] = p; input.value = p ? p.name : ''; close(); touch(); paint(); };
    var choose = function (h) {
      var name = (h.name + (h.secondary ? ', ' + h.secondary : '')).slice(0, 300);
      if (h.lat != null && h.lng != null) return setPoint({ name: name, lat: h.lat, lng: h.lng });
      input.disabled = true;
      plain('/api/lapkehoach/diem-chi-tiet?placeId=' + encodeURIComponent(h.placeId)).then(function (d) { setPoint({ name: name, lat: d.lat, lng: d.lng }); }).catch(function (er) { showError(er.message); }).then(function () { input.disabled = !isOwner; });
    };
    var note = function (m) { hits = []; list.innerHTML = '<li class="pe-suggest-empty">' + m + '</li>'; list.hidden = false; };
    input.addEventListener('input', function () {
      if (st[key]) { st[key] = null; touch(); }
      paint(); clearTimeout(timer); var q = input.value.trim(); var mine = ++token; if (q.length < 2) return close();
      timer = setTimeout(function () {
        plain('/api/lapkehoach/diem-goi-y?q=' + encodeURIComponent(q)).then(function (r) {
          if (mine !== token) return;
          var found = (r && r.hits) || [];
          if (!found.length) return note(r && r.limited ? 'Dịch vụ tìm địa chỉ đang quá tải nên chưa gợi ý được. Bấm Chọn trên bản đồ hoặc Vị trí của tôi.' : 'Chưa tìm thấy. Thử gõ rõ hơn hoặc bấm Chọn trên bản đồ.');
          hits = found;
          list.innerHTML = found.map(function (h, n) { return '<li><button type="button" data-n="' + n + '"><strong>' + esc(h.name) + '</strong><small>' + esc(h.secondary || '') + '</small></button></li>'; }).join('') + (r.limited ? '<li class="pe-suggest-empty">Tìm địa chỉ đang quá tải, chỉ có địa điểm sẵn trên website. Nơi khác: bấm Chọn trên bản đồ.</li>' : '');
          list.hidden = false;
        }).catch(function () { if (mine === token) note('Không tải được gợi ý. Bấm Chọn trên bản đồ hoặc Vị trí của tôi.'); });
      }, 350);
    });
    input.addEventListener('keydown', function (e) {
      if (e.key === 'Escape') return close();
      if (e.key === 'Enter' && hits.length) { e.preventDefault(); choose(hits[0]); }
    });
    list.addEventListener('click', function (e) { var b = e.target.closest('[data-n]'); if (b && hits[+b.dataset.n]) choose(hits[+b.dataset.n]); });
    state.addEventListener('click', function (e) { if (e.target.closest('[data-clear]')) { setPoint(null); input.focus(); } });
    document.addEventListener('click', function (e) { if (!list.contains(e.target) && e.target !== input) close(); });
    return { set: setPoint, paint: paint, pending: function () { return !st[key] && !!input.value.trim(); }, input: input };
  };
  var originPick = pointPicker('origin', 'pe-origin', 'pe-origin-list', 'pe-origin-state'), endPick = pointPicker('end', 'pe-end', 'pe-end-list', 'pe-end-state');
  // ten cho mot toa do (neu tra duoc), khong chan thao tac khi dich vu khong tra loi
  var nameAt = function (lat, lng) { return plain('/api/lapkehoach/dia-chi?lat=' + lat + '&lng=' + lng).then(function (r) { return (r && (r.address || [r.commune, r.province].filter(Boolean).join(', '))) || ''; }).catch(function () { return ''; }); };
  $('pe-origin-here').addEventListener('click', function () {
    var btn = this;
    if (!navigator.geolocation) return showError('Trình duyệt này không hỗ trợ định vị. Hãy bấm Chọn trên bản đồ.');
    btn.disabled = true; btn.textContent = 'Đang lấy vị trí…';
    var done = function () { btn.disabled = false; btn.textContent = 'Vị trí của tôi'; };
    navigator.geolocation.getCurrentPosition(function (p) {
      done();
      var lat = +p.coords.latitude.toFixed(6), lng = +p.coords.longitude.toFixed(6);
      if (lat < 5 || lat > 24 || lng < 101.5 || lng > 118.5) return showError('Vị trí của bạn nằm ngoài khu vực hỗ trợ. Hãy gõ tên nơi xuất phát hoặc chọn trên bản đồ.');
      var point = { name: 'Vị trí của tôi', lat: lat, lng: lng };
      showError(''); originPick.set(point);
      nameAt(lat, lng).then(function (n) { if (n && st.origin === point) { point.name = n.slice(0, 300); originPick.input.value = point.name; } });
    }, function (er) { done(); showError(er && er.code === 1 ? 'Trình duyệt đang chặn định vị cho trang này. Hãy cho phép định vị, hoặc bấm Chọn trên bản đồ.' : 'Không lấy được vị trí lúc này. Hãy bấm Chọn trên bản đồ.'); }, { enableHighAccuracy: false, timeout: 10000, maximumAge: 60000 });
  });
  // chon diem tren ban do: khong phu thuoc dich vu tim dia chi
  var pmap = null, pmark = null, ppick = null, ptarget = null;
  var openPointMap = function (picker, key, title) {
    ptarget = picker; ppick = null; $('pe-point-title').textContent = title; $('pe-point-use').disabled = true;
    $('pe-point-status').textContent = 'Bấm vào bản đồ để đặt điểm.'; $('pe-point').showModal();
    if (!window.L) { $('pe-point-canvas').textContent = 'Không tải được bản đồ.'; return; }
    if (pmap) { pmap.remove(); pmap = null; } pmark = null;
    pmap = L.map('pe-point-canvas', { maxBounds: VN_BOUNDS, maxBoundsViscosity: 1 }); addTiles(pmap);
    var cur = st[key], from = cur || st.origin || st.end;
    if (from) pmap.setView([from.lat, from.lng], cur ? 15 : 12); else pmap.setView([16.1, 106.3], 5);
    limitToVn(pmap);
    var put = function (lat, lng, name) {
      if (pmark) pmark.setLatLng([lat, lng]); else pmark = L.marker([lat, lng]).addTo(pmap);
      var mine = ppick = { name: name || '', lat: +lat.toFixed(6), lng: +lng.toFixed(6) };
      $('pe-point-use').disabled = false;
      if (name) { $('pe-point-status').textContent = 'Đang chọn: ' + name; return; }
      $('pe-point-status').textContent = 'Đã đặt điểm. Đang tìm tên vị trí…';
      nameAt(mine.lat, mine.lng).then(function (n) { if (ppick !== mine) return; mine.name = n.slice(0, 300); $('pe-point-status').textContent = n ? 'Đã chọn: ' + n : 'Đã đặt điểm trên bản đồ.'; });
    };
    if (cur) put(cur.lat, cur.lng, cur.name || 'điểm đã lưu');
    pmap.on('click', function (ev) { put(ev.latlng.lat, ev.latlng.lng); });
    setTimeout(function () { pmap.invalidateSize(); limitToVn(pmap); }, 80);
  };
  $('pe-point-use').addEventListener('click', function () {
    if (!ppick || !ptarget) return;
    ptarget.set({ name: ppick.name || 'Điểm đã chọn trên bản đồ', lat: ppick.lat, lng: ppick.lng }); showError(''); $('pe-point').close();
  });
  $('pe-origin-map').addEventListener('click', function () { openPointMap(originPick, 'origin', 'Chọn điểm xuất phát trên bản đồ'); });
  $('pe-end-map').addEventListener('click', function () { openPointMap(endPick, 'end', 'Chọn điểm kết thúc trên bản đồ'); });
  $('pe-end-same').addEventListener('change', function () { $('pe-end-wrap').hidden = this.checked; if (this.checked) { st.end = null; $('pe-end').value = ''; endPick.paint(); } touch(); });
  if (st.origin) $('pe-origin').value = st.origin.name || 'Điểm đã lưu';
  if (st.end) { $('pe-end-same').checked = false; $('pe-end-wrap').hidden = false; $('pe-end').value = st.end.name || 'Điểm đã lưu'; }
  originPick.paint(); endPick.paint();

`;
s = s.slice(0, a) + block + s.slice(b);
// chan luu khi da go ten ma chua co vi tri (truoc day am tham bo mat diem xuat phat)
const guard = "    if (!start) { $('pe-start').focus(); return showError('Chọn ngày đi.'); }\n";
if (!s.includes(guard)) throw Error('Khong tim thay cho kiem tra luu');
s = s.replace(guard, guard +
  "    if (originPick.pending()) { $('pe-origin').focus(); return showError('Điểm xuất phát chưa có vị trí. Chọn một dòng gợi ý, bấm Chọn trên bản đồ, hoặc xóa trống ô này.'); }\n" +
  "    if (!$('pe-end-same').checked && endPick.pending()) { $('pe-end').focus(); return showError('Điểm kết thúc chưa có vị trí. Chọn một dòng gợi ý, bấm Chọn trên bản đồ, hoặc tích “Kết thúc tại điểm xuất phát”.'); }\n");
if (crlf) s = s.replace(/\n/g, '\r\n');
fs.writeFileSync(f, s);
console.log('ok');
