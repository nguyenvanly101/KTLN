// Bo chon dia gioi: tinh -> quan/huyen -> xa. Gia tri cuoi cung (cap sau nhat da chon) nam trong input an [data-dg-value].
(function () {
  'use strict';
  function getJson(url) {
    return fetch(url, { headers: { Accept: 'application/json' }, credentials: 'same-origin' }).then(function (r) {
      if (!r.ok) throw new Error('HTTP ' + r.status);
      return r.json();
    });
  }
  function fill(select, items, placeholder, selected) {
    select.innerHTML = '';
    var first = document.createElement('option');
    first.value = '';
    first.textContent = placeholder;
    select.appendChild(first);
    items.forEach(function (item) {
      var o = document.createElement('option');
      o.value = item.id;
      o.textContent = item.fullName || item.name;
      o.dataset.hasChildren = item.hasChildren ? '1' : '';
      if (String(item.id) === String(selected)) o.selected = true;
      select.appendChild(o);
    });
    select.disabled = items.length === 0;
  }
  function init(root) {
    if (root.dataset.dgReady) return;
    root.dataset.dgReady = '1';
    var field = root.querySelector('[data-dg-value]');
    var selects = Array.prototype.slice.call(root.querySelectorAll('select[data-dg-level]'));
    var status = root.querySelector('[data-dg-status]');
    var leafOnly = root.dataset.dgLeaf === '1';
    var placeholders = selects.map(function (s) { return s.options[0].textContent; });
    var hint = status ? status.textContent : '';

    function sync() {
      var value = '';
      selects.forEach(function (s) { if (s.value) value = s.value; });
      field.value = value;
      field.dispatchEvent(new Event('change', { bubbles: true }));
      if (!status) return;
      status.classList.remove('dg-error');
      if (!value) { status.textContent = hint; return; }
      var last = selects.filter(function (s) { return s.value; }).pop();
      var pending = leafOnly && last && last.selectedOptions[0].dataset.hasChildren === '1';
      if (pending) { status.textContent = 'Chọn tiếp cấp nhỏ hơn.'; status.classList.add('dg-error'); return; }
      getJson('/api/dia-gioi/' + encodeURIComponent(value) + '/duong-dan').then(function (data) { status.textContent = data.text; }).catch(function () { status.textContent = ''; });
    }
    function loadChildren(level, parentId, selected) {
      var next = selects[level + 1];
      if (!next) return Promise.resolve();
      for (var i = level + 1; i < selects.length; i++) fill(selects[i], [], placeholders[i], null);
      if (!parentId) return Promise.resolve();
      return getJson('/api/dia-gioi/' + encodeURIComponent(parentId) + '/con').then(function (data) {
        fill(next, data.items, placeholders[level + 1], selected);
      });
    }
    selects.forEach(function (select, level) {
      select.addEventListener('change', function () {
        loadChildren(level, select.value, null).then(sync);
        if (!select.value) sync();
      });
    });

    getJson('/api/dia-gioi/tinh').then(function (data) {
      var initial = root.dataset.dgInitial;
      if (!initial) { fill(selects[0], data.items, placeholders[0], null); return; }
      return getJson('/api/dia-gioi/' + encodeURIComponent(initial) + '/duong-dan').then(function (path) {
        var chain = path.items;
        fill(selects[0], data.items, placeholders[0], chain[0] && chain[0].id);
        var step = Promise.resolve();
        chain.slice(0, selects.length - 1).forEach(function (unit, i) {
          step = step.then(function () { return loadChildren(i, unit.id, chain[i + 1] && chain[i + 1].id); });
        });
        return step.then(function () { if (status) status.textContent = path.text; });
      });
    }).catch(function () {
      if (status) { status.textContent = 'Không tải được danh sách địa giới. Hãy tải lại trang.'; status.classList.add('dg-error'); }
    });
  }
  function boot() { document.querySelectorAll('[data-dg]').forEach(init); }
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', boot); else boot();
})();
