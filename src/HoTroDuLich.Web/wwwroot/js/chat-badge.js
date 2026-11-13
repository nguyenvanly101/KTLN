// Mot ket noi su kien thoi gian thuc cho moi tab: cap nhat so tin chua doc va chuyen su kien cho trang tin nhan.
(function () {
  'use strict';
  var last = null, original = document.title, flashTimer = null;
  function flash(n) {
    if (!flashTimer) {
      var on = false; flashTimer = setInterval(function () { on = !on; document.title = on ? '(' + n + ') Tin nhắn mới' : original; }, 1000);
    }
    if ('Notification' in window && Notification.permission === 'granted') {
      try { var note = new Notification('Hành Trình', { body: 'Bạn có tin nhắn mới', tag: 'chat-new' }); note.onclick = function () { window.focus(); note.close(); }; } catch (e) { /* bo qua */ }
    }
  }
  document.addEventListener('visibilitychange', function () { if (!document.hidden && flashTimer) { clearInterval(flashTimer); flashTimer = null; document.title = original; } });
  function set(n) {
    if (last !== null && n > last && document.hidden) flash(n);
    last = n; document.querySelectorAll('[data-chat-badge]').forEach(function (badge) { badge.textContent = n > 99 ? '99+' : String(n); badge.hidden = !(n > 0); }); }
  function refresh() {
    fetch('/chat/chua-doc', { credentials: 'same-origin' }).then(function (r) { return r.ok ? r.json() : null; })
      .then(function (d) { if (d) set(d.count); }).catch(function () {});
  }
  window.chatBadge = { set: set, refresh: refresh };
  // Trong khung chat nhung: khong mo ket noi rieng (tranh het so ket noi cua trinh duyet), nhan su kien tu trang cha.
  if (window.top !== window.self) {
    window.addEventListener('message', function (e) {
      if (e.origin !== location.origin || !e.data || e.data.type !== 'chat:event') return;
      document.dispatchEvent(new CustomEvent('chat:event', { detail: e.data.detail }));
    });
    return;
  }
  if (!window.EventSource) { setInterval(refresh, 30000); return; }
  var es = new EventSource('/chat/luong');
  es.onmessage = function (e) {
    var d; try { d = JSON.parse(e.data); } catch (x) { return; }
    document.dispatchEvent(new CustomEvent('chat:event', { detail: d }));
    var frame = document.querySelector('[data-chat-dock-frame]');
    if (frame && frame.contentWindow) frame.contentWindow.postMessage({ type: 'chat:event', detail: d }, location.origin);
    if (d.t === 'msg' || d.t === 'read') refresh();
  };
  window.addEventListener('beforeunload', function () { es.close(); });
})();
