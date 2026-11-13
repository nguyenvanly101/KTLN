// Chia se vi tri hien tai: tao lien ket Google Maps, khong gui ve may chu.
(function () {
  'use strict';
  var box = document.querySelector('[data-share-location]'); if (!box) return;
  var btn = box.querySelector('[data-share-btn]'), status = box.querySelector('[data-share-status]'), result = box.querySelector('[data-share-result]');
  var link = box.querySelector('[data-share-link]'), copy = box.querySelector('[data-share-copy]'), sms = box.querySelector('[data-share-sms]');
  btn.addEventListener('click', function () {
    if (!navigator.geolocation) { status.textContent = 'Trình duyệt này không hỗ trợ lấy vị trí.'; return; }
    status.textContent = 'Đang lấy vị trí…';
    navigator.geolocation.getCurrentPosition(function (p) {
      var url = 'https://www.google.com/maps?q=' + p.coords.latitude.toFixed(6) + ',' + p.coords.longitude.toFixed(6);
      link.value = url; sms.href = 'sms:?&body=' + encodeURIComponent('Vị trí của tôi: ' + url); result.hidden = false; status.textContent = 'Đã lấy vị trí (độ chính xác khoảng ' + Math.round(p.coords.accuracy) + ' m).';
    }, function () { status.textContent = 'Không lấy được vị trí. Hãy cho phép truy cập vị trí cho trang này.'; }, { enableHighAccuracy: true, timeout: 15000 });
  });
  copy.addEventListener('click', function () { link.select(); try { document.execCommand('copy'); status.textContent = 'Đã sao chép liên kết.'; } catch (e) { status.textContent = 'Hãy giữ và sao chép liên kết ở ô trên.'; } });
})();
