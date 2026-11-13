// Ket ban kieu Facebook: trang Ban be (ban be / loi moi / goi y / tim) va nut ket ban tren ho so.
(function () {
  'use strict';
  var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
  var token = tokenInput ? tokenInput.value : '';
  function h(tag, cls, text) { var n = document.createElement(tag); if (cls) n.className = cls; if (text != null) n.textContent = text; return n; }
  function api(path, opts) {
    opts = opts || {}; opts.credentials = 'same-origin'; opts.headers = opts.headers || {};
    if (opts.method && opts.method !== 'GET') opts.headers['RequestVerificationToken'] = token;
    return fetch(path, opts).then(function (r) {
      return r.json().catch(function () { return {}; }).then(function (d) { if (!r.ok) { var e = new Error(d.message || 'Không thực hiện được. Hãy thử lại.'); e.status = r.status; throw e; } return d; });
    });
  }
  function form(values) { var f = new FormData(); Object.keys(values).forEach(function (k) { f.append(k, values[k]); }); return f; }
  var toastBox;
  function toast(msg) {
    if (!msg) return; if (!toastBox) { toastBox = h('div', 'soc-toast'); toastBox.setAttribute('role', 'status'); document.body.appendChild(toastBox); }
    toastBox.textContent = msg; toastBox.hidden = false; clearTimeout(toastBox._t); toastBox._t = setTimeout(function () { toastBox.hidden = true; }, 4000);
  }
  function avatar(person, size) {
    var box = h('span', 'soc-avatar' + (size ? ' ' + size : ''));
    var initial = (person.name || '?').trim().charAt(0).toUpperCase();
    if (person.avatar) { var img = h('img'); img.src = person.avatar; img.alt = ''; img.width = 48; img.height = 48; img.loading = 'lazy'; img.onerror = function () { img.remove(); box.textContent = initial; }; box.appendChild(img); }
    else box.textContent = initial;
    return box;
  }
  window.Soc = { h: h, api: api, form: form, token: function () { return token; }, toast: toast, avatar: avatar };

  /** Cac nut theo quan he; onChange duoc goi sau moi hanh dong thanh cong. */
  function actionsFor(p, onChange) {
    var wrap = h('div', 'soc-card-actions');
    function btn(label, cls, fn) {
      var b = h('button', 'btn ' + cls, label); b.type = 'button';
      b.addEventListener('click', function () { b.disabled = true; fn().then(function () { onChange(); }).catch(function (e) { b.disabled = false; if (e && e.message) toast(e.message); }); });
      wrap.appendChild(b); return b;
    }
    if (p.relation === 'friends') {
      var m = h('a', 'btn btn-dark', 'Nhắn tin'); m.href = '/tin-nhan/nguoi/' + p.id; wrap.appendChild(m);
      btn('Hủy kết bạn', 'btn-outline', function () { if (!confirm('Hủy kết bạn với ' + p.name + '?')) return Promise.reject(new Error('')); return api('/api/mxh/ban-be/xoa', { method: 'POST', body: form({ accountId: p.id }) }); });
    } else if (p.relation === 'sent') {
      btn('Hủy lời mời', 'btn-outline', function () { return api('/api/mxh/ban-be/' + p.requestId + '/huy', { method: 'POST' }); });
    } else if (p.relation === 'received') {
      btn('Chấp nhận', 'btn-dark', function () { return api('/api/mxh/ban-be/' + p.requestId + '/chap-nhan', { method: 'POST' }); });
      btn('Từ chối', 'btn-outline', function () { return api('/api/mxh/ban-be/' + p.requestId + '/tu-choi', { method: 'POST' }); });
    } else if (p.relation === 'none') {
      btn('Thêm bạn bè', 'btn-dark', function () { return api('/api/mxh/ban-be/gui', { method: 'POST', body: form({ accountId: p.id }) }); });
    } else if (p.relation === 'declined-by-them') {
      wrap.appendChild(h('span', 'soc-meta', 'Lời mời trước đó chưa được nhận'));
    }
    return wrap;
  }
  window.Soc.actionsFor = actionsFor;

  // ---------------------------------------------------------------- trang Ban be
  var tabs = document.querySelector('[data-soc-tabs]');
  if (tabs) {
    var sections = document.querySelector('[data-soc-sections]'), empty = document.querySelector('[data-soc-empty]'), searchBox = document.querySelector('[data-soc-search]'), q = document.querySelector('[data-soc-q]');
    var tab = tabs.dataset.tab || 'ban-be';
    var personCard = function (p) {
      var card = h('article', 'soc-card panel');
      var a = h('a', 'soc-card-main'); a.href = '/nguoi/' + p.id; a.appendChild(avatar(p));
      var info = h('div'); info.appendChild(h('strong', null, p.name));
      if (p.mutual > 0) info.appendChild(h('small', null, p.mutual + ' bạn chung'));
      if (p.bio) info.appendChild(h('small', 'soc-bio', p.bio));
      a.appendChild(info); card.appendChild(a); card.appendChild(actionsFor(p, load));
      return card;
    };
    var section = function (title, list) {
      var s = h('section'); if (title) s.appendChild(h('h2', 'soc-h2', title));
      var grid = h('div', 'soc-grid'); list.forEach(function (p) { grid.appendChild(personCard(p)); }); s.appendChild(grid); sections.appendChild(s);
    };
    var show = function (msg) { empty.hidden = !msg; empty.textContent = msg || ''; };
    var counts = function () {
      api('/api/mxh/loi-moi').then(function (d) { var c = tabs.querySelector('[data-count="loi-moi"]'); c.textContent = d.incoming.length ? String(d.incoming.length) : ''; }).catch(function () {});
    };
    function load() {
      sections.replaceChildren(); show(''); searchBox.hidden = !(tab === 'tim' || tab === 'ban-be');
      tabs.querySelectorAll('button').forEach(function (b) { b.classList.toggle('active', b.dataset.tab === tab); b.setAttribute('aria-selected', b.dataset.tab === tab ? 'true' : 'false'); });
      counts();
      var term = q.value.trim();
      if (tab === 'ban-be') api('/api/mxh/ban-be?q=' + encodeURIComponent(term)).then(function (list) { if (!list.length) show(term ? 'Không có bạn bè nào khớp.' : 'Bạn chưa có bạn bè nào. Xem Gợi ý hoặc Tìm người để kết bạn.'); else section(list.length + ' bạn bè', list); }).catch(function (e) { show(e.message); });
      else if (tab === 'loi-moi') api('/api/mxh/loi-moi').then(function (d) { if (!d.incoming.length && !d.outgoing.length) show('Không có lời mời nào đang chờ.'); if (d.incoming.length) section('Lời mời kết bạn', d.incoming); if (d.outgoing.length) section('Lời mời bạn đã gửi', d.outgoing); }).catch(function (e) { show(e.message); });
      else if (tab === 'goi-y') api('/api/mxh/goi-y').then(function (list) { if (!list.length) show('Chưa có gợi ý nào. Hãy thử Tìm người.'); else section('Những người bạn có thể biết', list); }).catch(function (e) { show(e.message); });
      else if (tab === 'tim') { if (term.length < 2) { show('Nhập ít nhất 2 ký tự để tìm người.'); return; } api('/api/mxh/tim?q=' + encodeURIComponent(term)).then(function (list) { if (!list.length) show('Không tìm thấy ai.'); else section('Kết quả', list); }).catch(function (e) { show(e.message); }); }
    }
    tabs.querySelectorAll('button').forEach(function (b) { b.addEventListener('click', function () { tab = b.dataset.tab; history.replaceState(null, '', '/ban-be?tab=' + tab); load(); }); });
    var timer; q.addEventListener('input', function () { clearTimeout(timer); timer = setTimeout(load, 300); });
    load();
  }

  // ---------------------------------------------------------------- ho so
  var profile = document.querySelector('[data-soc-profile]');
  if (profile) {
    var box = profile.querySelector('[data-soc-actions]');
    var p = { id: Number(profile.dataset.id), name: profile.querySelector('h1').textContent, relation: profile.dataset.relation, requestId: profile.dataset.request ? Number(profile.dataset.request) : null };
    if (profile.dataset.self) { var edit = h('a', 'btn btn-outline', 'Chỉnh sửa hồ sơ'); edit.href = '/Account/Profile'; box.appendChild(edit); }
    else {
      box.appendChild(actionsFor(p, function () { location.reload(); }));
      if (p.relation !== 'friends') { var m = h('a', 'btn btn-outline', 'Nhắn tin'); m.href = '/tin-nhan/nguoi/' + p.id; box.appendChild(m); }
    }
  }
})();
