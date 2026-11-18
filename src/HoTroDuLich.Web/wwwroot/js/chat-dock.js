// Khung chat noi o moi trang: bam "Nhan tin" mo cua so nho o goc man hinh (khong roi trang). Nhung trang /tin-nhan?embed=1 trong iframe cung nguon.
(function () {
  'use strict';
  if (window.top !== window.self) return;
  if (/^\/(tin-nhan|nha-cung-cap\/tin-nhan)(\/|$)/.test(location.pathname)) return;
  var KEY = 'chat-dock', ROOT = /^\/(tin-nhan|nha-cung-cap\/tin-nhan)(\/|$)/;
  function read() { try { return JSON.parse(sessionStorage.getItem(KEY) || '{}'); } catch (e) { return {}; } }
  function save(s) { try { sessionStorage.setItem(KEY, JSON.stringify(s)); } catch (e) { /* bo qua */ } }
  var state = read();
  function h(tag, cls, text) { var n = document.createElement(tag); if (cls) n.className = cls; if (text != null) n.textContent = text; return n; }

  var bubble = h('button', 'chat-dock-bubble'); bubble.type = 'button'; bubble.setAttribute('aria-label', 'Mở tin nhắn');
  bubble.innerHTML = '<svg class="icon" aria-hidden="true"><use href="/images/icons.svg#chat"></use></svg>';
  var badge = h('span', 'chat-badge'); badge.setAttribute('data-chat-badge', ''); badge.hidden = true; bubble.appendChild(badge);

  var panel = h('section', 'chat-dock'); panel.hidden = true; panel.setAttribute('aria-label', 'Tin nhắn');
  var head = h('div', 'chat-dock-head'); head.appendChild(h('strong', null, 'Tin nhắn'));
  var tools = h('div');
  var expand = h('button', null, 'Mở rộng'); expand.type = 'button'; var min = h('button', null, 'Thu nhỏ'); min.type = 'button';
  tools.appendChild(expand); tools.appendChild(min); head.appendChild(tools); panel.appendChild(head);
  var frame = h('iframe'); frame.setAttribute('data-chat-dock-frame', ''); frame.title = 'Hộp thư'; panel.appendChild(frame);
  document.body.appendChild(panel); document.body.appendChild(bubble);

  // Dia chi trong khung: bo/them embed=1 bang URL de khong ghep sai khi co tham so khac.
  function plainUrl(l) { var u = new URL(l.href); u.searchParams.delete('embed'); return u.pathname + u.search; }
  function embedUrl(path) { var u = new URL(path, location.origin); u.searchParams.set('embed', '1'); return u.pathname + u.search; }
  function open(src) {
    var target = src || state.src || '/tin-nhan?embed=1';
    if (!frame.getAttribute('src') || (src && frame.getAttribute('src') !== target)) frame.setAttribute('src', target);
    panel.hidden = false; bubble.hidden = true; state.open = true; state.src = target; save(state);
  }
  function close() { panel.hidden = true; bubble.hidden = false; state.open = false; save(state); }
  bubble.addEventListener('click', function () { open(); });
  min.addEventListener('click', close);
  expand.addEventListener('click', function () {
    var path = '/tin-nhan';
    try { path = plainUrl(frame.contentWindow.location); } catch (e) { /* giu mac dinh */ }
    location.href = path;
  });
  frame.addEventListener('load', function () {
    try { var l = frame.contentWindow.location; if (l.pathname.indexOf('/tin-nhan') === 0 || l.pathname.indexOf('/nha-cung-cap/tin-nhan') === 0) { state.src = embedUrl(plainUrl(l)); save(state); } else if (l.href !== 'about:blank') { frame.setAttribute('src', '/tin-nhan?embed=1'); } } catch (e) { /* khac nguon */ }
    if (window.chatBadge) window.chatBadge.refresh();
  });

  // Bam moi lien ket "Nhan tin" (khong nam trong menu dieu huong) se mo khung noi thay vi chuyen trang
  document.addEventListener('click', function (e) {
    var a = e.target.closest && e.target.closest('a[href]');
    if (!a || e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || a.target === '_blank') return;
    if (a.closest('[data-no-dock],.sidebar-nav,.mainnav,.topbar,.chat-dock')) return;
    var url; try { url = new URL(a.href, location.href); } catch (x) { return; }
    if (url.origin !== location.origin || !ROOT.test(url.pathname) || url.pathname.indexOf('/cai-dat') >= 0) return;
    e.preventDefault(); open(embedUrl(url.pathname + url.search));
  }, true);

  if (state.open) open();
})();
