// Do day du cua ho so co so (cung trong so voi BranchService.Completeness) cap nhat ngay khi nha cung cap nhap.
(function () {
  'use strict';
  var form = document.querySelector('[data-pv-form]');
  if (!form) return;
  var scoreBox = form.querySelector('[data-pv-score]');
  if (!scoreBox) return;
  var percentEl = scoreBox.querySelector('[data-pv-percent]');
  var bar = scoreBox.querySelector('[data-pv-bar]');
  var list = scoreBox.querySelector('[data-pv-missing]');
  var ok = scoreBox.querySelector('[data-pv-ok]');
  var title = scoreBox.querySelector('.pv-score-title');
  var summary = form.querySelector('[data-pv-summary]');
  var counter = form.querySelector('[data-pv-summary-count]');
  var photos = form.querySelector('[data-pv-photos]');

  function val(name) { var el = form.querySelector('[name="' + name + '"]'); return el ? String(el.value || '').trim() : ''; }

  function compute() {
    var missing = [], score = 0;
    function add(cond, points, need) { if (cond) score += points; else missing.push(need); }
    add(val('Name').length > 0, 10, 'Tên cơ sở');
    add(val('DonViId').length > 0, 15, 'Khu vực đến cấp xã/phường');
    add(val('Address').length > 0, 10, 'Địa chỉ');
    add(val('Latitude').length > 0, 10, 'Ghim vị trí trên bản đồ');
    add(val('Summary').length >= 40, 10, 'Giới thiệu ít nhất 40 ký tự');
    var days = form.querySelectorAll('[data-pv-day]:checked').length;
    add(days > 0 && val('OpenTime') && val('CloseTime'), 10, 'Giờ mở cửa');
    add(val('PriceFrom').length > 0, 10, 'Giá tham khảo');
    var existing = parseInt((photos && photos.dataset.existing) || '0', 10) || 0;
    var removing = !!form.querySelector('[name="RemovePhotos"]:checked');
    var chosen = photos && photos.files ? photos.files.length : 0;
    var count = chosen > 0 ? chosen : (removing ? 0 : existing);
    add(count >= 1, 15, 'Ít nhất một ảnh thực tế');
    if (count >= 3) score += 5; else if (count >= 1) missing.push('Thêm ảnh (nên có từ 3 ảnh)');
    add(val('Phone').length > 0 || val('Website').length > 0, 5, 'Số điện thoại hoặc website');
    return { percent: Math.min(100, score), missing: missing };
  }

  function render() {
    var r = compute();
    percentEl.textContent = r.percent + '%';
    bar.style.width = r.percent + '%';
    list.replaceChildren.apply(list, r.missing.map(function (m) { var li = document.createElement('li'); li.textContent = m; return li; }));
    var done = r.missing.length === 0;
    ok.hidden = !done; list.hidden = done; if (title) title.hidden = done;
    if (summary && counter) counter.textContent = String(summary.value.length);
  }

  function typeFields() {
    var sel = form.querySelector('[name="Type"]'); if (!sel) return;
    form.querySelectorAll('[data-type-fields]').forEach(function (fs) { fs.hidden = fs.dataset.typeFields !== sel.value; });
  }
  var typeSel = form.querySelector('[name="Type"]'); if (typeSel) typeSel.addEventListener('change', typeFields); typeFields();
  form.addEventListener('input', render);
  form.addEventListener('change', render);
  // Vi tri/khu vuc duoc dat boi bo chon dia gioi va ban do (su kien tu script khac)
  document.addEventListener('change', render);
  setTimeout(render, 600);
  setTimeout(render, 2500);
  render();
})();
