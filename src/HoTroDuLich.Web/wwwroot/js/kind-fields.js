// Hien bo truong rieng cua loai dich vu dang chon.
(function () {
  'use strict';
  var sel = document.querySelector('[data-kind-select]'); if (!sel) return;
  function sync() { document.querySelectorAll('[data-kind-fields]').forEach(function (fs) { fs.hidden = fs.dataset.kindFields !== sel.value; }); }
  sel.addEventListener('change', sync); sync();
})();
