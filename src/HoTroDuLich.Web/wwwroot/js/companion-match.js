// "Phu hop voi chuyen cua ban": nguoi cung diem den va giao ngay voi loi ru cua minh, kem nut Nhan tin & ru di cung.
(function () {
  'use strict';
  var section = document.querySelector('[data-companion-matches]');
  if (!section) return;
  var grid = section.querySelector('[data-match-grid]');
  function h(tag, cls, text) { var n = document.createElement(tag); if (cls) n.className = cls; if (text != null) n.textContent = text; return n; }
  function dmy(iso) { var p = String(iso).split('-'); return p.length === 3 ? p[2] + '/' + p[1] : iso; }
  fetch('/api/mxh/phu-hop', { credentials: 'same-origin' }).then(function (r) { return r.ok ? r.json() : []; }).then(function (list) {
    if (!list.length) return;
    list.forEach(function (m) {
      var card = h('article', 'match-card');
      var who = h('a'); who.href = '/nguoi/' + m.person.id; who.appendChild(h('strong', null, m.person.name)); card.appendChild(who);
      card.appendChild(h('small', null, m.destination + ' · ' + dmy(m.start) + '–' + dmy(m.end) + (m.mode ? ' · ' + m.mode : '')));
      var ul = h('ul', 'match-reasons'); (m.reasons || [m.because]).forEach(function (r) { ul.appendChild(h('li', null, r)); }); card.appendChild(ul);
      var go = h('a', 'btn btn-dark', 'Nhắn tin & rủ đi cùng'); go.href = '/tin-nhan/nguoi/' + m.person.id + '?nhuCau=' + m.needId; card.appendChild(go);
      var skip = h('button', 'text-link', 'Không quan tâm'); skip.type = 'button';
      skip.addEventListener('click', function () {
        var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]'); var fd = new FormData(); fd.append('needId', m.needId);
        fetch('/api/mxh/phu-hop/bo-qua', { method: 'POST', credentials: 'same-origin', headers: { RequestVerificationToken: tokenInput ? tokenInput.value : '' }, body: fd }).then(function (r) { if (r.ok) { card.remove(); if (!grid.children.length) section.hidden = true; } });
      });
      card.appendChild(skip);
      grid.appendChild(card);
    });
    section.hidden = false;
  }).catch(function () {});
})();
