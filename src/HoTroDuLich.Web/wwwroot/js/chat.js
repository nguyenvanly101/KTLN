// Trang tin nhan khach <-> co so: danh sach, khung chat thoi gian thuc (su kien tu chat-badge.js), da xem, dang soan, tin mau.
(function () {
  'use strict';
  var app = document.querySelector('[data-chat-app]');
  if (!app) return;
  var me = app.dataset.me; // Traveler | Branch
  var isProvider = me === 'Branch';
  function iAmFirst() { return !st.thread || st.thread.me === 'Traveler'; }
  var myAccount = Number(app.dataset.account || 0);
  function isMine(m) { return m.mine !== undefined && m.mine !== null ? m.mine : (m.side === 'Traveler') === iAmFirst(); }
  function inGroup() { return !!(st.thread && st.thread.item && st.thread.item.group); }
  var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
  var token = tokenInput ? tokenInput.value : '';
  function q(sel) { return app.querySelector(sel); }
  var el = {
    list: q('[data-chat-list]'), listEmpty: q('[data-chat-list-empty]'), placeholder: q('[data-chat-placeholder]'), pane: q('[data-chat-pane]'),
    title: q('[data-chat-title]'), sub: q('[data-chat-sub]'), meta: q('[data-chat-meta]'), messages: q('[data-chat-messages]'),
    typing: q('[data-chat-typing]'), blocked: q('[data-chat-blocked]'), quick: q('[data-chat-quick]'), form: q('[data-chat-form]'),
    input: q('[data-chat-input]'), send: q('[data-chat-send]'), image: q('[data-chat-image]'), file: q('[data-chat-file]'),
    fileName: q('[data-chat-file-name]'), fileClear: q('[data-chat-file-clear]'), error: q('[data-chat-error]'), back: q('[data-chat-back]'),
    branchLink: q('[data-chat-branch-link]'), blockBtn: q('[data-chat-block]'), stateBtn: q('[data-chat-state]'), note: q('[data-chat-note]'), noteBox: q('[data-chat-notebox]'), label: q('[data-chat-label]'),
    branchSel: q('[data-chat-branch]'), search: q('[data-chat-search]'),
    context: q('[data-chat-context]'), connect: q('[data-chat-connect]'), connectToggle: q('[data-chat-connect-toggle]'), connectClose: q('[data-chat-connect-close]'),
    book: q('[data-chat-book]'), bookToggle: q('[data-chat-book-toggle]'), bookClose: q('[data-chat-book-close]')
  };
  var base = '/tin-nhan';
  var mEl = { replyBar: q('[data-chat-replybar]'), replyText: q('[data-chat-reply-text]'), replyCancel: q('[data-chat-reply-cancel]'), pdf: q('[data-chat-pdf]'), geo: q('[data-chat-location]'), emojiBtn: q('[data-chat-emoji]'), emojiPop: q('[data-chat-emoji-pop]') };
  var GLYPH = { like: '👍', love: '❤️', haha: '😂', wow: '😮', sad: '😢', angry: '😡' };
  var EMOJIS = ['😀', '😂', '😍', '🥰', '😊', '😉', '😎', '🤔', '😢', '😡', '👍', '👏', '🙏', '❤️', '🔥', '🎉', '✈️', '🏖️', '🏔️', '🍜', '☕', '📍', '🚗', '🏨'];
  var st = { openId: null, startBranch: null, startPeer: null, thread: null, items: [], lastId: 0, otherRead: 0, filter: '', branch: app.dataset.filterBranch || '', term: '', typingTimer: null, lastTypingSent: 0, sending: false, firstId: 0, more: false, loadingOlder: false, cards: {}, conns: {}, agrees: {}, lastMessages: [], peerId: null, needs: [], pickNeed: new URLSearchParams(location.search).get('nhuCau'), services: [], pickService: new URLSearchParams(location.search).get('dichVu') };

  function h(tag, cls, text) { var n = document.createElement(tag); if (cls) n.className = cls; if (text != null) n.textContent = text; return n; }
  function two(n) { return n < 10 ? '0' + n : '' + n; }
  function timeOf(iso) { var d = new Date(iso); return two(d.getHours()) + ':' + two(d.getMinutes()); }
  function dayOf(iso) { var d = new Date(iso); return two(d.getDate()) + '/' + two(d.getMonth() + 1) + '/' + d.getFullYear(); }
  function ago(iso) {
    var d = new Date(iso), s = (Date.now() - d.getTime()) / 1000;
    if (s < 60) return 'Vừa xong'; if (s < 3600) return Math.floor(s / 60) + ' phút';
    if (s < 86400 && new Date().getDate() === d.getDate()) return timeOf(iso);
    return two(d.getDate()) + '/' + two(d.getMonth() + 1);
  }
  function showError(msg) { el.error.textContent = msg || ''; el.error.hidden = !msg; }
  function api(path, opts) {
    opts = opts || {}; opts.credentials = 'same-origin'; opts.headers = opts.headers || {};
    if (opts.method && opts.method !== 'GET') opts.headers['RequestVerificationToken'] = token;
    return fetch(path, opts).then(function (r) {
      return r.json().catch(function () { return {}; }).then(function (d) {
        if (!r.ok) { var e = new Error(d.message || 'Không thực hiện được. Hãy thử lại.'); e.status = r.status; throw e; }
        return d;
      });
    });
  }

  // ---------------------------------------------------------------- danh sach
  function loadList() {
    var qs = [];
    if (isProvider) { if (st.branch) qs.push('coSo=' + encodeURIComponent(st.branch)); if (st.filter) qs.push('loc=' + st.filter); if (st.term) qs.push('q=' + encodeURIComponent(st.term)); }
    return api('/chat/danh-sach' + (qs.length ? '?' + qs.join('&') : '')).then(function (items) { st.items = items; renderList(); }).catch(function () {});
  }
  function renderList() {
    el.list.replaceChildren();
    st.items.forEach(function (c) {
      var li = h('li', 'chat-item' + (c.id === st.openId ? ' active' : '') + (c.unread > 0 ? ' unread' : ''));
      var a = h('a'); a.href = base + '/' + c.id; a.dataset.id = c.id;
      var top = h('div', 'chat-item-top'); top.appendChild(h('strong', null, c.who)); top.appendChild(h('time', null, ago(c.at)));
      a.appendChild(top);
      if (c.group) a.appendChild(h('small', 'chat-item-branch', 'Nhóm · ' + c.members + ' thành viên'));
      else if (c.branchName && c.branchName !== c.who) a.appendChild(h('small', 'chat-item-branch', c.branchName));
      var prev = h('p', null, (c.lastFromMe ? 'Bạn: ' : '') + (c.preview || ''));
      var row = h('div', 'chat-item-row'); row.appendChild(prev);
      if (c.unread > 0) row.appendChild(h('span', 'chat-dot', c.unread > 99 ? '99+' : String(c.unread)));
      a.appendChild(row);
      if (c.pending > 0) a.appendChild(h('div', 'chat-pending', isProvider ? c.pending + ' yêu cầu đặt chờ xác nhận' : 'Cơ sở đề xuất điều kiện khác — cần bạn phản hồi'));
      if (c.label || c.state === 'Done') { var tags = h('div', 'chat-tags'); if (c.label) tags.appendChild(h('span', 'chat-tag', c.label)); if (c.state === 'Done') tags.appendChild(h('span', 'chat-tag done', 'Đã xử lý')); a.appendChild(tags); }
      a.addEventListener('click', function (e) { if (e.metaKey || e.ctrlKey) return; e.preventDefault(); openThread(c.id); });
      li.appendChild(a); el.list.appendChild(li);
    });
    el.listEmpty.hidden = st.items.length > 0 || !!st.startBranch || !!st.startPeer;
  }

  // ---------------------------------------------------------------- khung chat
  function setPane(on) { el.pane.hidden = !on; el.placeholder.hidden = on; app.classList.toggle('chat-open', on); }
  function renderMessages(list, append) {
    var box = el.messages, stick = box.scrollHeight - box.scrollTop - box.clientHeight < 80 || !append;
    if (!append) box.replaceChildren();
    var lastDay = append && box.lastElementChild ? box.lastElementChild.dataset.day : null;
    list.forEach(function (m) {
      var day = dayOf(m.at);
      if (day !== lastDay) { var sep = h('div', 'chat-day', day); sep.dataset.day = day; box.appendChild(sep); lastDay = day; }
      box.appendChild(messageNode(m));
    });
    renderCards(); renderConnections(); renderAgreements();
    if (stick) box.scrollTop = box.scrollHeight;
    updateReceipts();
  }
  var MONEY = new Intl.NumberFormat('vi-VN');
  function dmy(iso) { var p = String(iso).split('-'); return p.length === 3 ? p[2] + '/' + p[1] + '/' + p[0] : iso; }
  function renderCards() {
    el.messages.querySelectorAll('.chat-card[data-booking]').forEach(function (holder) {
      var c = st.cards[holder.dataset.booking]; holder.replaceChildren();
      if (!c) { holder.appendChild(h('small', null, 'Đang tải yêu cầu…')); return; }
      holder.className = 'chat-card status-' + c.status;
      var top = h('div', 'chat-card-top'); top.appendChild(h('strong', null, c.service)); top.appendChild(h('span', 'chat-card-status', c.statusLabel)); holder.appendChild(top);
      var rows = [['Ngày', dmy(c.start) + (c.end ? ' – ' + dmy(c.end) : '')], ['Số người', c.people], ['Số lượng', c.quantity]];
      if (c.arrivalDeparture) rows.push(['Giờ đến/đi', c.arrivalDeparture]);
      if (c.cost != null) rows.push(['Chi phí dự kiến', MONEY.format(c.cost) + ' ₫']);
      var dl = h('dl', 'chat-card-facts'); rows.forEach(function (r) { dl.appendChild(h('dt', null, r[0])); dl.appendChild(h('dd', null, String(r[1]))); }); holder.appendChild(dl);
      if (c.notes) holder.appendChild(h('p', 'chat-card-note', 'Ghi chú: ' + c.notes));
      if (c.terms) holder.appendChild(h('p', 'chat-card-terms', 'Đề xuất của cơ sở: ' + c.terms));
      if (c.reason && c.status !== 'DaXacNhan') holder.appendChild(h('p', 'chat-card-note', 'Lý do: ' + c.reason));
      if (c.deposit != null) holder.appendChild(h('p', 'chat-card-deposit', 'Đặt cọc: ' + MONEY.format(c.deposit) + ' ₫ · ' + c.depositMethod + ' · ' + (c.depositStatus === 'Paid' ? 'Đã nhận cọc' : 'Chờ thanh toán') + (c.depositNote ? '\n' + c.depositNote : '')));
      if (c.planId) holder.appendChild(h('small', 'soc-meta', 'Đã gắn vào một chuyến đi của bạn'));
      if (c.status === 'DeXuatKhac' && c.terms && /Phương án \d/.test(c.terms) && c.actions.indexOf('KhachDongY') >= 0) {
        var picks = h('div', 'chat-card-actions');
        (c.terms.match(/Phương án \d/g) || []).forEach(function (label) {
          var n = label.slice(-1), pb = h('button', 'btn btn-dark', 'Chọn ' + label.toLowerCase()); pb.type = 'button';
          pb.addEventListener('click', function () {
            var fd = new FormData(); fd.append('Body', 'Mình chọn ' + label + '.'); fd.append('ConversationId', st.openId);
            api('/chat/gui', { method: 'POST', body: fd }).then(function () { return doActionPromise(c, { Decision: 'KhachDongY' }); }).then(function () { fetchNew(); refreshCards(); loadList(); }).catch(function (e) { showError(e.message); });
          });
          picks.appendChild(pb);
        });
        holder.appendChild(picks);
      }
      if (c.canReview) { var rv = h('a', 'btn btn-outline', 'Viết đánh giá'); rv.href = '/Reviews/Write?bookingId=' + c.id; holder.appendChild(rv); }
      var det = h('a', 'text-link', c.status === 'DaXacNhan' ? 'Chi tiết đặt chỗ & thêm vào chuyến đi' : 'Chi tiết đặt chỗ'); det.href = '/Bookings/Detail/' + c.id; holder.appendChild(det);
      if (c.actions && c.actions.length) holder.appendChild(cardActions(c));
    });
  }
  function renderConnections() {
    el.messages.querySelectorAll('.chat-card[data-connection]').forEach(function (holder) {
      var c = st.conns[holder.dataset.connection]; holder.replaceChildren();
      if (!c) { holder.appendChild(h('small', null, 'Đang tải lời rủ…')); return; }
      holder.className = 'chat-card status-' + (c.status === 'DaChapNhan' ? 'DaXacNhan' : c.status === 'ChoPhanHoi' ? 'ChoPhanHoi' : 'TuChoi');
      var top = h('div', 'chat-card-top'); top.appendChild(h('strong', null, 'Rủ đi cùng · ' + c.destination)); top.appendChild(h('span', 'chat-card-status', c.statusLabel)); holder.appendChild(top);
      var dl = h('dl', 'chat-card-facts'); dl.appendChild(h('dt', null, 'Ngày')); dl.appendChild(h('dd', null, dmy(c.start) + ' – ' + dmy(c.end))); holder.appendChild(dl);
      if (c.intro) holder.appendChild(h('p', 'chat-card-note', c.intro));
      if (c.status === 'DaChapNhan') {
        var a = h('button', 'btn btn-outline', 'Lập thỏa thuận đi cùng'); a.type = 'button'; holder.appendChild(a);
        var af = h('div', 'chat-card-form'); af.hidden = true; holder.appendChild(af);
        a.addEventListener('click', function () {
          af.hidden = !af.hidden; if (af.children.length) return;
          function fld(label, type, name, val) { var l = h('label', null, label); var i = h(type === 'textarea' ? 'textarea' : 'input', 'form-control'); if (type !== 'textarea') i.type = type; else i.rows = 2; i.name = name; i.value = val == null ? '' : val; l.appendChild(i); af.appendChild(l); }
          fld('Điểm đến', 'text', 'Destination', c.destination); fld('Ngày đi', 'date', 'Start', c.start); fld('Ngày về', 'date', 'End', c.end); fld('Ngân sách mỗi người (₫, tùy chọn)', 'number', 'Budget', ''); fld('Phạm vi đi cùng (đi chung xe, ở chung phòng, chia chi phí…)', 'textarea', 'Scope', '');
          var go = h('button', 'btn btn-dark', 'Gửi đề xuất'); go.type = 'button';
          go.addEventListener('click', function () {
            var fd = new FormData(); fd.append('ConversationId', st.openId); fd.append('ConnectionId', c.id);
            af.querySelectorAll('[name]').forEach(function (i) { if (i.value !== '') fd.append(i.name, i.value); }); go.disabled = true; showError('');
            api('/chat/thoa-thuan', { method: 'POST', body: fd }).then(function () { af.hidden = true; fetchNew(); refreshAgrees(); loadList(); }).catch(function (e) { showError(e.message); }).then(function () { go.disabled = false; });
          });
          af.appendChild(go);
        });
        var more = h('a', 'text-link', 'Xem chi tiết kết nối'); more.href = c.link; holder.appendChild(more);
        if (c.otherId) {
          var g = h('button', 'btn btn-dark', 'Lập nhóm chat chuyến đi'); g.type = 'button';
          g.addEventListener('click', function () {
            var fd = new FormData(); fd.append('Title', 'Đi ' + c.destination); fd.append('MemberIds', c.otherId);
            api('/chat/nhom', { method: 'POST', body: fd }).then(function (r) { return openThread(r.conversationId).then(loadList); }).catch(function (e) { showError(e.message); });
          });
          holder.appendChild(g);
        }
      }
      if (c.actions && c.actions.length) {
        var box = h('div', 'chat-card-actions'); var L = { DaChapNhan: 'Đồng ý đi cùng', TuChoi: 'Từ chối', DaRut: 'Rút lời rủ' };
        c.actions.forEach(function (act) {
          var b = h('button', 'btn ' + (act === 'DaChapNhan' ? 'btn-dark' : 'btn-outline'), L[act]); b.type = 'button';
          b.addEventListener('click', function () {
            if (act !== 'DaChapNhan' && !confirm(act === 'DaRut' ? 'Rút lời rủ này?' : 'Từ chối lời rủ này?')) return;
            var fd = new FormData(); fd.append('ConnectionId', c.id); fd.append('Decision', act); showError('');
            api('/chat/' + st.openId + '/ket-noi/hanh-dong', { method: 'POST', body: fd }).then(function () { fetchNew(); refreshConns(); loadList(); }).catch(function (e) { showError(e.message); });
          });
          box.appendChild(b);
        });
        holder.appendChild(box);
      }
    });
  }
  function renderAgreements() {
    el.messages.querySelectorAll('.chat-card[data-agreement]').forEach(function (holder) {
      var c = st.agrees[holder.dataset.agreement]; holder.replaceChildren();
      if (!c) { holder.appendChild(h('small', null, 'Đang tải thỏa thuận…')); return; }
      holder.className = 'chat-card status-' + (c.status === 'DaXacNhan' ? 'DaXacNhan' : c.status === 'ChoXacNhan' ? 'ChoPhanHoi' : 'TuChoi');
      var top = h('div', 'chat-card-top'); top.appendChild(h('strong', null, 'Thỏa thuận đi cùng · ' + c.destination)); top.appendChild(h('span', 'chat-card-status', c.statusLabel)); holder.appendChild(top);
      var dl = h('dl', 'chat-card-facts');
      [['Ngày', dmy(c.start) + ' – ' + dmy(c.end)]].concat(c.budget != null ? [['Ngân sách', MONEY.format(c.budget) + ' ₫']] : []).forEach(function (r) { dl.appendChild(h('dt', null, r[0])); dl.appendChild(h('dd', null, r[1])); });
      holder.appendChild(dl);
      if (c.scope) holder.appendChild(h('p', 'chat-card-note', 'Phạm vi đi cùng: ' + c.scope));
      if (c.reason && c.status !== 'DaXacNhan') holder.appendChild(h('p', 'chat-card-note', 'Lý do: ' + c.reason));
      var box = h('div', 'chat-card-actions'), L = { DaXacNhan: 'Xác nhận thỏa thuận', TuChoi: 'Chưa phù hợp', Rut: 'Kết thúc thỏa thuận' };
      (c.actions || []).forEach(function (act) {
        var b = h('button', 'btn ' + (act === 'DaXacNhan' ? 'btn-dark' : 'btn-outline'), L[act]); b.type = 'button';
        b.addEventListener('click', function () {
          var reason = '';
          if (act === 'TuChoi') { reason = prompt('Lý do (bắt buộc):') || ''; if (!reason.trim()) return; }
          if (act === 'Rut' && !confirm('Kết thúc thỏa thuận đi cùng này?')) return;
          var fd = new FormData(); fd.append('AgreementId', c.id); fd.append('Decision', act); if (reason) fd.append('Reason', reason); showError('');
          api('/chat/' + st.openId + '/thoa-thuan/hanh-dong', { method: 'POST', body: fd }).then(function () { fetchNew(); refreshAgrees(); loadList(); }).catch(function (e) { showError(e.message); });
        });
        box.appendChild(b);
      });
      if (box.children.length) holder.appendChild(box);
      var more = h('a', 'text-link', 'Xem chi tiết kết nối'); more.href = c.link; holder.appendChild(more);
    });
  }
  function refreshAgrees() {
    if (!st.openId) return;
    api('/chat/' + st.openId + '/thoa-thuan').then(function (list) { st.agrees = {}; list.forEach(function (c) { st.agrees[c.id] = c; }); renderAgreements(); }).catch(function () {});
  }
  function refreshConns() {
    if (!st.openId) return;
    api('/chat/' + st.openId + '/ket-noi').then(function (list) { st.conns = {}; list.forEach(function (c) { st.conns[c.id] = c; }); renderConnections(); }).catch(function () {});
  }
  var ACTION_LABEL = { YeuCauCoc: 'Đề nghị đặt cọc', DaNhanCoc: 'Đã nhận cọc', ThemVaoChuyenDi: 'Thêm vào chuyến đi', DaXacNhan: 'Xác nhận', TuChoi: 'Từ chối', DeXuatKhac: 'Đề xuất khác', Rut: 'Rút yêu cầu', KhachDongY: 'Đồng ý đề xuất', KhachKhongDongY: 'Không đồng ý' };
  function cardActions(c) {
    var box = h('div', 'chat-card-actions'), form = h('div', 'chat-card-form'); form.hidden = true;
    c.actions.forEach(function (a) {
      var b = h('button', 'btn ' + (a === 'DaXacNhan' || a === 'KhachDongY' ? 'btn-dark' : 'btn-outline'), ACTION_LABEL[a]); b.type = 'button';
      b.addEventListener('click', function () {
        if (a === 'TuChoi' || a === 'DeXuatKhac' || a === 'KhachKhongDongY' || a === 'YeuCauCoc' || a === 'ThemVaoChuyenDi') { openCardForm(form, c, a); return; }
        if (a === 'DaNhanCoc') { if (!confirm('Xác nhận bạn đã nhận khoản cọc này?')) return; api('/chat/' + st.openId + '/dat/coc/xac-nhan', { method: 'POST', body: form2({ requestId: c.id }) }).then(function () { fetchNew(); refreshCards(); }).catch(function (e) { showError(e.message); }); return; }
        if (a === 'Rut' && !confirm('Rút yêu cầu này?')) return;
        doAction(c, { Decision: a });
      });
      box.appendChild(b);
    });
    var wrap = h('div'); wrap.appendChild(box); wrap.appendChild(form); return wrap;
  }
  function form2(values) { var f = new FormData(); Object.keys(values).forEach(function (k) { f.append(k, values[k]); }); return f; }
  function openCardForm(form, c, a) {
    form.replaceChildren(); form.hidden = false;
    function field(label, type, name, val) { var l = h('label', null, label); var i = h(type === 'textarea' ? 'textarea' : 'input', 'form-control'); if (type !== 'textarea') i.type = type; else i.rows = 2; i.name = name; if (val != null) i.value = val; l.appendChild(i); form.appendChild(l); return i; }
    var optRows = null;
    if (a === 'YeuCauCoc') {
      field('Số tiền cọc (₫)', 'number', 'Amount', c.cost != null ? Math.round(c.cost * 0.3 / 1000) * 1000 : '');
      var ml = h('label', null, 'Hình thức'); var sel = h('select', 'form-select'); sel.name = 'Method'; ['Chuyển khoản trực tiếp', 'Tiền mặt tại quầy'].forEach(function (m) { var o = h('option', null, m); o.value = m; sel.appendChild(o); }); ml.appendChild(sel); form.appendChild(ml);
      field('Ghi chú (số tài khoản, hạn chuyển…)', 'textarea', 'Note', '');
      form.appendChild(h('small', null, 'Hai bên tự thanh toán với nhau. Website không xử lý và không lưu số tài khoản.'));
    } else if (a === 'ThemVaoChuyenDi') {
      var pl = h('label', null, 'Chọn chuyến đi'); var ps = h('select', 'form-select'); ps.name = 'PlanId'; pl.appendChild(ps); form.appendChild(pl);
      api('/chat/ke-hoach').then(function (plans) { if (!plans.length) { ps.appendChild(h('option', null, 'Bạn chưa có chuyến đi nào')); ps.disabled = true; return; } plans.forEach(function (p) { var o = h('option', null, p.name + ' · ' + dmy(p.start) + '–' + dmy(p.end)); o.value = p.id; if (c.planId === p.id) o.selected = true; ps.appendChild(o); }); }).catch(function () {});
    } else if (a === 'DeXuatKhac') {
      field('Ngày bắt đầu', 'date', 'Start', c.start); field('Ngày kết thúc', 'date', 'End', c.end || ''); field('Số người', 'number', 'People', c.people); field('Số lượng', 'number', 'Quantity', c.quantity); field('Chi phí đề xuất (₫)', 'number', 'Cost', c.cost == null ? '' : c.cost);
      optRows = h('div', 'chat-options'); form.appendChild(h('strong', null, 'Phương án đề xuất'));
      function addOpt() { if (optRows.children.length >= 3) return; var row = h('div', 'chat-option'); var t = h('textarea', 'form-control'); t.rows = 2; t.maxLength = 400; t.placeholder = 'Phương án ' + (optRows.children.length + 1) + ': hạng phòng, giờ khác, điều kiện…'; var cst = h('input', 'form-control'); cst.type = 'number'; cst.min = 0; cst.placeholder = 'Chi phí (₫)'; row.appendChild(t); row.appendChild(cst); optRows.appendChild(row); }
      addOpt(); form.appendChild(optRows);
      var more = h('button', 'text-link', '+ Thêm phương án (tối đa 3)'); more.type = 'button'; more.addEventListener('click', addOpt); form.appendChild(more);
    } else field(a === 'TuChoi' ? 'Lý do từ chối' : 'Lý do (tùy chọn)', 'textarea', 'Reason', '');
    var go = h('button', 'btn btn-dark', 'Gửi'); go.type = 'button';
    go.addEventListener('click', function () {
      var v = { Decision: a }; form.querySelectorAll('[name]').forEach(function (i) { if (i.value !== '') v[i.name] = i.value; });
      if (a === 'YeuCauCoc') { var fd1 = form2({ RequestId: c.id, Amount: v.Amount || '', Method: v.Method || '', Note: v.Note || '' }); api('/chat/' + st.openId + '/dat/coc', { method: 'POST', body: fd1 }).then(function () { fetchNew(); refreshCards(); }).catch(function (e) { showError(e.message); }); return; }
      if (a === 'ThemVaoChuyenDi') { var fd2 = form2({ requestId: c.id }); if (v.PlanId) fd2.append('planId', v.PlanId); api('/chat/' + st.openId + '/dat/chuyen-di', { method: 'POST', body: fd2 }).then(function () { form.hidden = true; refreshCards(); toastLite('Đã thêm vào chuyến đi.'); }).catch(function (e) { showError(e.message); }); return; }
      if (optRows) {
        var rows = Array.prototype.map.call(optRows.children, function (r) { return { t: r.querySelector('textarea').value.trim(), c: r.querySelector('input').value }; }).filter(function (r) { return r.t; });
        if (!rows.length) { showError('Nhập ít nhất một phương án.'); return; }
        v.Text = rows.length === 1 ? rows[0].t : rows.map(function (r, n) { return 'Phương án ' + (n + 1) + ': ' + r.t + (r.c ? ' — ' + MONEY.format(Number(r.c)) + ' ₫' : ''); }).join('\n');
        if (rows[0].c) v.Cost = rows[0].c;
      }
      doAction(c, v);
    });
    form.appendChild(go);
  }
  function doActionPromise(c, v) { var fd = new FormData(); fd.append('RequestId', c.id); Object.keys(v).forEach(function (k) { fd.append(k, v[k]); }); return api('/chat/' + st.openId + '/dat/hanh-dong', { method: 'POST', body: fd }); }
  function doAction(c, v) {
    var fd = new FormData(); fd.append('RequestId', c.id); Object.keys(v).forEach(function (k) { fd.append(k, v[k]); });
    showError('');
    api('/chat/' + st.openId + '/dat/hanh-dong', { method: 'POST', body: fd }).then(function () { fetchNew(); refreshCards(); loadList(); }).catch(function (e) { showError(e.message); });
  }
  function refreshCards() {
    if (!st.openId) return;
    api('/chat/' + st.openId + '/the').then(function (list) { st.cards = {}; list.forEach(function (c) { st.cards[c.id] = c; }); renderCards(); }).catch(function () {});
  }
  function messageNode(m) {
    if (m.kind === 'Event') { var ev = h('div', 'chat-event', m.body); ev.dataset.id = m.id; ev.dataset.day = dayOf(m.at); ev.dataset.booking = m.booking || ''; return ev; }
    if (m.kind === 'Connection') {
      var cw = h('div', 'chat-msg chat-cardwrap ' + (isMine(m) ? 'mine' : 'theirs')); cw.dataset.id = m.id; cw.dataset.day = dayOf(m.at); cw.dataset.mine = '';
      var ch = h('div', 'chat-card'); ch.dataset.connection = m.connection; cw.appendChild(ch);
      cw.appendChild(h('small', 'chat-time', timeOf(m.at)));
      return cw;
    }
    if (m.kind === 'Agreement') {
      var aw = h('div', 'chat-msg chat-cardwrap ' + (isMine(m) ? 'mine' : 'theirs')); aw.dataset.id = m.id; aw.dataset.day = dayOf(m.at); aw.dataset.mine = '';
      var ah = h('div', 'chat-card'); ah.dataset.agreement = m.agreement; aw.appendChild(ah);
      aw.appendChild(h('small', 'chat-time', timeOf(m.at)));
      return aw;
    }
    if (m.kind === 'Booking') {
      var wrap = h('div', 'chat-msg chat-cardwrap ' + (isMine(m) ? 'mine' : 'theirs')); wrap.dataset.id = m.id; wrap.dataset.day = dayOf(m.at); wrap.dataset.mine = '';
      var holder = h('div', 'chat-card'); holder.dataset.booking = m.booking; wrap.appendChild(holder);
      wrap.appendChild(h('small', 'chat-time', timeOf(m.at)));
      return wrap;
    }
    var mine = isMine(m);
    var w = h('div', 'chat-msg ' + (mine ? 'mine' : 'theirs') + (m.kind === 'Auto' ? ' auto' : '')); w.dataset.id = m.id; w.dataset.day = dayOf(m.at); w.dataset.mine = mine ? '1' : '';
    if (inGroup() && !mine && m.sender) w.appendChild(h('small', 'chat-sender', m.sender));
    if (m.deleted) { w.appendChild(h('div', 'chat-bubble chat-gone', 'Tin nhắn đã được thu hồi')); w.appendChild(h('small', 'chat-time', timeOf(m.at))); return w; }
    var b = h('div', 'chat-bubble');
    if (m.replyTo) { var qt = h('button', 'chat-quote'); qt.type = 'button'; qt.appendChild(h('strong', null, m.replyAuthor || 'Tin nhắn')); qt.appendChild(h('span', null, m.replyText || '')); qt.addEventListener('click', function () { jumpTo(m.replyTo); }); b.appendChild(qt); }
    if (m.kind === 'Auto') b.appendChild(h('small', 'chat-auto-tag', 'Trả lời tự động'));
    if (m.image) { var a = h('a'); a.href = '/chat/' + st.openId + '/anh/' + m.id; a.target = '_blank'; a.rel = 'noopener'; a.addEventListener('click', function (ev) { ev.preventDefault(); lightbox(a.href); }); var img = h('img'); img.src = a.href; img.alt = 'Ảnh đã gửi'; img.loading = 'lazy'; img.width = 220; a.appendChild(img); b.appendChild(a); }
    if (m.body) b.appendChild(h('p', null, m.body));
    if (m.kind === 'Location' && m.lat != null) { var geo = h('a', 'chat-geo', '📍 Vị trí được chia sẻ · mở bản đồ'); geo.href = 'https://www.openstreetmap.org/?mlat=' + m.lat + '&mlon=' + m.lng + '#map=17/' + m.lat + '/' + m.lng; geo.target = '_blank'; geo.rel = 'noopener'; b.appendChild(geo); }
    if (m.file) { var pdf = h('a', 'chat-pdf', '📄 ' + (m.fileName || 'Tệp PDF')); pdf.href = '/chat/' + st.openId + '/tep/' + m.id; pdf.target = '_blank'; pdf.rel = 'noopener'; b.appendChild(pdf); }
    w.appendChild(b);
    if (m.reactions && m.reactions.length) {
      var rb = h('div', 'chat-reacts');
      m.reactions.forEach(function (r) { var c = h('button', r.mine ? 'on' : '', (GLYPH[r.emoji] || r.emoji) + ' ' + r.count); c.type = 'button'; c.addEventListener('click', function () { reactTo(m, r.emoji); }); rb.appendChild(c); });
      w.appendChild(rb);
    }
    var meta = h('small', 'chat-time', timeOf(m.at) + (m.edited ? ' · đã sửa' : '')); meta.appendChild(h('span', 'chat-receipt')); w.appendChild(meta);
    w.appendChild(msgTools(m, w, b));
    return w;
  }
  function loadOlder() {
    if (!st.openId || !st.more || st.loadingOlder) return;
    st.loadingOlder = true; var box = el.messages, prev = box.scrollHeight;
    api('/chat/' + st.openId + '/tin?truoc=' + st.firstId).then(function (list) {
      if (!list.length) { st.more = false; return; }
      st.more = list.length >= 60; st.firstId = list[0].id;
      var frag = document.createDocumentFragment(), lastDay = null;
      list.forEach(function (m) { var day = dayOf(m.at); if (day !== lastDay) { var sep = h('div', 'chat-day', day); sep.dataset.day = day; frag.appendChild(sep); lastDay = day; } frag.appendChild(messageNode(m)); });
      var first = box.firstElementChild; if (first && first.classList.contains('chat-day') && first.dataset.day === lastDay) first.remove();
      box.insertBefore(frag, box.firstChild); renderCards(); renderConnections(); renderAgreements(); updateReceipts();
      box.scrollTop = box.scrollHeight - prev;
    }).catch(function () {}).then(function () { st.loadingOlder = false; });
  }
  el.messages.addEventListener('scroll', function () { if (el.messages.scrollTop < 40) loadOlder(); });
  function lightbox(src) {
    var o = h('div', 'chat-lightbox'); o.setAttribute('role', 'dialog'); o.setAttribute('aria-label', 'Xem ảnh'); var img = h('img'); img.src = src; img.alt = 'Ảnh phóng to'; o.appendChild(img);
    function close() { o.remove(); document.removeEventListener('keydown', onKey); } function onKey(e) { if (e.key === 'Escape') close(); }
    o.addEventListener('click', close); document.addEventListener('keydown', onKey); document.body.appendChild(o);
  }
  // ban nhap tin theo tung cuoc tro chuyen
  function draftKey() { return 'chat-draft:' + (st.openId ? 'c' + st.openId : st.startBranch ? 'b' + st.startBranch : st.startPeer ? 'p' + st.startPeer : ''); }
  function saveDraft() { var k = draftKey(); if (!k.endsWith(':')) { try { if (el.input.value) localStorage.setItem(k, el.input.value); else localStorage.removeItem(k); } catch (e) { /* bo qua */ } } }
  function loadDraft() { try { var v = localStorage.getItem(draftKey()); el.input.value = v || ''; autosize(); } catch (e) { /* bo qua */ } }
  function jumpTo(id) {
    var n = el.messages.querySelector('[data-id="' + id + '"]'); if (!n) { toastLite('Tin gốc nằm ngoài phần đang hiển thị.'); return; }
    n.scrollIntoView({ block: 'center', behavior: 'smooth' }); n.classList.add('chat-flash'); setTimeout(function () { n.classList.remove('chat-flash'); }, 1600);
  }
  function toastLite(t) { showError(t); setTimeout(function () { showError(''); }, 3000); }
  function reactTo(m, emoji) {
    var fd = new FormData(); fd.append('emoji', emoji);
    api('/chat/' + st.openId + '/tin/' + m.id + '/cam-xuc', { method: 'POST', body: fd }).then(function () { replaceMessage(m.id); }).catch(function (e) { showError(e.message); });
  }
  function replaceMessage(id) {
    if (!st.openId) return Promise.resolve();
    return api('/chat/' + st.openId + '/tin/' + id).then(function (m) {
      var old = el.messages.querySelector('.chat-msg[data-id="' + id + '"]'); if (!old) return;
      var fresh = messageNode(m); old.replaceWith(fresh); renderCards(); renderConnections(); updateReceipts();
    }).catch(function () {});
  }
  function setReply(m) {
    var text = m.deleted ? 'Tin nhắn đã được thu hồi' : (m.body || (m.kind === 'Image' ? '[Ảnh]' : m.kind === 'Location' ? '[Vị trí]' : m.file ? '[Tệp] ' + m.fileName : ''));
    st.replyTo = m.id; mEl.replyText.textContent = 'Đang trả lời ' + (isMine(m) ? 'chính bạn' : (m.sender || 'tin nhắn')) + ': ' + text.slice(0, 80);
    mEl.replyBar.hidden = false; el.input.focus();
  }
  function clearReply() { st.replyTo = null; if (mEl.replyBar) mEl.replyBar.hidden = true; }
  function sendReport(messageId) {
    var pick = prompt('Lý do báo cáo:\n1 = Spam\n2 = Quấy rối\n3 = Lừa đảo\n4 = Nội dung xấu\n5 = Khác');
    var map = { '1': 'Spam', '2': 'QuayRoi', '3': 'LuaDao', '4': 'NoiDungXau', '5': 'Khac' }; if (!pick || !map[pick.trim()]) return;
    var note = prompt('Mô tả thêm (tùy chọn):') || '';
    var fd = new FormData(); fd.append('Reason', map[pick.trim()]); if (note) fd.append('Note', note); if (messageId) fd.append('MessageId', messageId);
    api('/chat/' + st.openId + '/bao-cao', { method: 'POST', body: fd }).then(function () { toastLite('Đã gửi báo cáo. Quản trị viên sẽ xem xét.'); }).catch(function (e) { showError(e.message); });
  }
  function msgTools(m, w, bubble) {
    var tools = h('div', 'chat-msg-tools');
    function tool(label, title, fn) { var b = h('button', null, label); b.type = 'button'; b.title = title; b.setAttribute('aria-label', title); b.addEventListener('click', fn); tools.appendChild(b); return b; }
    var pop = h('div', 'chat-react-pop'); pop.hidden = true;
    Object.keys(GLYPH).forEach(function (k) { var b = h('button', null, GLYPH[k]); b.type = 'button'; b.addEventListener('click', function () { pop.hidden = true; reactTo(m, k); }); pop.appendChild(b); });
    tool('😊', 'Thả biểu cảm', function () { pop.hidden = !pop.hidden; });
    tool('↩', 'Trả lời', function () { setReply(m); });
    if (!isMine(m) && ['Text', 'Image', 'Location', 'File'].indexOf(m.kind) >= 0) tool('⚑', 'Báo cáo tin nhắn', function () { sendReport(m.id); });
    var age = (Date.now() - new Date(m.at).getTime()) / 60000;
    if (isMine(m) && m.kind === 'Text' && age <= 15) tool('✎', 'Sửa tin', function () {
      var p = bubble.querySelector('p'); if (!p) return;
      var inp = h('input', 'form-control'); inp.value = m.body; inp.maxLength = 2000; p.replaceWith(inp); inp.focus();
      function done(save) {
        if (!save) { replaceMessage(m.id); return; }
        var fd = new FormData(); fd.append('body', inp.value);
        api('/chat/' + st.openId + '/tin/' + m.id + '/sua', { method: 'POST', body: fd }).then(function () { replaceMessage(m.id); }).catch(function (e) { showError(e.message); replaceMessage(m.id); });
      }
      inp.addEventListener('keydown', function (e) { if (e.key === 'Enter') { e.preventDefault(); done(true); } else if (e.key === 'Escape') done(false); });
    });
    if (isMine(m) && ['Text', 'Image', 'Location', 'File'].indexOf(m.kind) >= 0 && age <= 10) tool('🗑', 'Thu hồi', function () {
      if (!confirm('Thu hồi tin nhắn này với mọi người?')) return;
      api('/chat/' + st.openId + '/tin/' + m.id + '/thu-hoi', { method: 'POST' }).then(function () { replaceMessage(m.id); }).catch(function (e) { showError(e.message); });
    });
    tools.appendChild(pop); return tools;
  }
  function updateReceipts() {
    if (inGroup()) return;
    var nodes = el.messages.querySelectorAll('.chat-msg[data-mine="1"]');
    nodes.forEach(function (n) { var r = n.querySelector('.chat-receipt'); if (r) r.textContent = ''; });
    var last = nodes.length ? nodes[nodes.length - 1] : null;
    if (last) { var r = last.querySelector('.chat-receipt'); r.textContent = Number(last.dataset.id) <= st.otherRead ? ' · Đã xem' : ' · Đã gửi'; }
  }
  function renderContext(c) {
    if (!el.context) return;
    el.context.replaceChildren(); el.context.hidden = !c; if (!c) return;
    if (c.photo) { var img = h('img'); img.src = c.photo; img.alt = ''; img.width = 56; img.height = 56; el.context.appendChild(img); }
    var box = h('div'); var a = h('a'); a.href = c.url; a.appendChild(h('strong', null, c.name)); box.appendChild(a);
    box.appendChild(h('small', null, c.address));
    var facts = [c.hours, c.price].filter(Boolean).join(' · '); if (facts) box.appendChild(h('small', null, facts));
    el.context.appendChild(box);
  }
  function applyThread(t) {
    clearReply();
    st.thread = t; st.openId = t.item.id; st.otherRead = t.otherReadId;
    setPane(true); showError('');
    el.title.textContent = t.item.who;
    var group = !!t.item.group;
    var first = t.me === 'Traveler', hasBranch = t.item.branchId > 0;
    if (group) { first = false; hasBranch = false; }
    var sub = first ? (hasBranch ? [t.eta, t.actual].filter(Boolean).join(' · ') : '') : (hasBranch ? t.item.branchName : '');
    if (group) sub = t.item.members + ' thành viên';
    else if (t.otherOnline) sub = 'Đang hoạt động' + (sub ? ' · ' + sub : '');
    el.sub.textContent = sub;
    el.meta.replaceChildren();
    if (first && t.phone) el.meta.appendChild(h('span', null, 'Cần gấp? Gọi ' + t.phone));
    el.branchLink.hidden = !hasBranch; el.branchLink.href = '/co-so/' + t.item.branchId; el.branchLink.textContent = 'Xem cơ sở';
    var rb = q('[data-chat-report]'); if (rb) rb.hidden = group;
    el.blockBtn.hidden = group; el.blockBtn.textContent = t.blockedByMe ? 'Bỏ chặn' : 'Chặn';
    el.stateBtn.hidden = true;
    if (!first && hasBranch) { el.stateBtn.hidden = false; el.stateBtn.textContent = t.item.state === 'Done' ? 'Mở lại' : 'Đánh dấu đã xử lý'; if (el.note) el.note.value = t.note || ''; if (el.label) el.label.value = t.item.label || ''; loadQuick(t.item.branchId); } else { el.quick.hidden = true; }
    if (el.noteBox) el.noteBox.hidden = first || !hasBranch;
    renderMembers(t); renderContext(t.context);
    el.blocked.hidden = t.canSend; el.blocked.textContent = t.blocked || '';
    el.form.hidden = !t.canSend;
    st.lastId = t.messages.length ? t.messages[t.messages.length - 1].id : 0;
    st.firstId = t.messages.length ? t.messages[0].id : 0; st.more = t.messages.length >= 60;
    st.lastMessages = t.messages || [];
    st.cards = {}; (t.cards || []).forEach(function (c) { st.cards[c.id] = c; });
    st.conns = {}; (t.connections || []).forEach(function (c) { st.conns[c.id] = c; });
    st.agrees = {}; (t.agreements || []).forEach(function (c) { st.agrees[c.id] = c; });
    setupConnect(t.peerId, t.canSend);
    renderMessages(t.messages, false);
    setupBooking(t.item.branchId, t.canSend && first && hasBranch); loadDraft();
  }
  function openThread(id, noPush) {
    st.startBranch = null;
    return api('/chat/' + id).then(function (t) {
      applyThread(t);
      if (!noPush) history.pushState({ id: id }, '', base + '/' + id);
      renderList(); markRead(); loadMoreIfShort();
    }).catch(function (e) { setPane(false); showError(e.message); });
  }
  function loadMoreIfShort() { /* 60 tin gan nhat da du cho cuoc tro chuyen thong thuong */ }
  function markRead() {
    if (!st.openId || document.hidden) return;
    api('/chat/' + st.openId + '/da-xem', { method: 'POST' }).then(function () {
      var it = st.items.filter(function (x) { return x.id === st.openId; })[0]; if (it && it.unread) { it.unread = 0; renderList(); }
      if (window.chatBadge) window.chatBadge.refresh();
    }).catch(function () {});
  }
  function fetchNew() {
    if (!st.openId) return;
    api('/chat/' + st.openId + '/tin?sau=' + st.lastId).then(function (list) {
      list = list.filter(function (m) { return !el.messages.querySelector('.chat-msg[data-id="' + m.id + '"]'); });
      if (!list.length) return;
      st.lastId = list[list.length - 1].id; renderMessages(list, true);
      if (list.some(function (m) { return m.booking; })) refreshCards();
      if (list.some(function (m) { return m.connection; })) refreshConns();
      if (list.some(function (m) { return m.agreement; })) refreshAgrees();
      if (list.some(function (m) { return !isMine(m) && m.side !== 'System'; })) { el.typing.hidden = true; markRead(); }
    }).catch(function () {});
  }

  // ---------------------------------------------------------------- bat dau tu trang co so
  function startMode(info) {
    st.startBranch = info.branchId; st.startPeer = null; st.openId = null; st.thread = null; st.lastId = 0; st.otherRead = 0;
    setPane(true); showError('');
    el.title.textContent = info.branchName; el.sub.textContent = [info.eta, info.actual].filter(Boolean).join(' · ');
    el.branchLink.hidden = false; el.branchLink.href = '/co-so/' + info.branchId; el.branchLink.textContent = 'Xem cơ sở';
    el.stateBtn.hidden = true; el.blockBtn.hidden = true; if (el.noteBox) el.noteBox.hidden = true; el.meta.replaceChildren(); el.messages.replaceChildren();
    if (info.greeting) el.messages.appendChild(h('div', 'chat-greeting', info.greeting));
    else el.messages.appendChild(h('div', 'chat-greeting', 'Hỏi cơ sở điều bạn muốn biết: còn chỗ không, giá thế nào, đi đường nào… Cơ sở sẽ trả lời tại đây.'));
    setupBooking(info.branchId, !info.ownBranch && info.accepting); loadDraft();
    var blocked = info.ownBranch ? 'Đây là cơ sở của bạn.' : (!info.accepting ? 'Cơ sở đang tạm ngưng nhận tin nhắn.' : '');
    el.blocked.hidden = !blocked; el.blocked.textContent = blocked; el.form.hidden = !!blocked;
  }

  function startPeerMode(info) {
    st.startPeer = info.peerId; st.startBranch = null; st.openId = null; st.thread = null; st.lastId = 0; st.otherRead = 0;
    setPane(true); showError('');
    el.title.textContent = info.peerName; el.sub.textContent = 'Trao đổi trước khi cùng đi';
    el.branchLink.hidden = true; el.stateBtn.hidden = true; el.blockBtn.hidden = true; if (el.noteBox) el.noteBox.hidden = true; el.quick.hidden = true;
    el.bookToggle.hidden = true; el.book.hidden = true; el.meta.replaceChildren(); el.messages.replaceChildren();
    el.messages.appendChild(h('div', 'chat-greeting', 'Giới thiệu bản thân và nói về chuyến đi bạn dự định. Hai bạn trò chuyện tại đây trước khi quyết định đi cùng.'));
    el.blocked.hidden = true; el.form.hidden = false;
    setupConnect(info.peerId, true); loadDraft();
  }

  // ---------------------------------------------------------------- gui tin
  function send(e) {
    e.preventDefault(); if (st.sending) return;
    var body = el.input.value.trim(), file = el.image.files[0], pdf = mEl.pdf && mEl.pdf.files[0];
    if (!body && !file && !pdf) return;
    var fd = new FormData(); fd.append('Body', body);
    if (file) fd.append('Image', file);
    if (pdf) fd.append('File', pdf);
    if (st.replyTo) fd.append('ReplyToId', st.replyTo);
    if (st.openId) fd.append('ConversationId', st.openId); else if (st.startBranch) fd.append('BranchId', st.startBranch); else if (st.startPeer) fd.append('PeerId', st.startPeer); else return;
    st.sending = true; el.send.disabled = true; showError('');
    api('/chat/gui', { method: 'POST', body: fd }).then(function (r) {
      var oldKey = draftKey(); try { localStorage.removeItem(oldKey); } catch (e) { /* bo qua */ }
      el.input.value = ''; autosize(); clearFile(); clearReply();
      if (!st.openId) { st.openId = r.conversationId; st.startBranch = null; st.startPeer = null; history.replaceState({ id: r.conversationId }, '', base + '/' + r.conversationId); return openThread(r.conversationId, true).then(loadList); }
      fetchNew(); loadList();
    }).catch(function (err) { showError(err.message); }).then(function () { st.sending = false; el.send.disabled = false; el.input.focus(); });
  }
  function autosize() { el.input.style.height = 'auto'; el.input.style.height = Math.min(el.input.scrollHeight, 140) + 'px'; }
  function clearFile() { el.image.value = ''; if (mEl.pdf) mEl.pdf.value = ''; el.file.hidden = true; }
  el.form.addEventListener('submit', send);
  el.input.addEventListener('input', function () {
    autosize(); saveDraft();
    if (st.openId && Date.now() - st.lastTypingSent > 2500) { st.lastTypingSent = Date.now(); api('/chat/' + st.openId + '/dang-soan', { method: 'POST' }).catch(function () {}); }
  });
  el.input.addEventListener('keydown', function (e) { if (e.key === 'Enter' && !e.shiftKey && !e.isComposing) { e.preventDefault(); el.form.requestSubmit(); } });
  el.image.addEventListener('change', function () { var f = el.image.files[0]; if (!f) return clearFile(); if (f.size > 5 * 1024 * 1024) { showError('Ảnh tối đa 5 MB.'); return clearFile(); } el.fileName.textContent = f.name; el.file.hidden = false; });
  el.fileClear.addEventListener('click', clearFile);
  if (mEl.pdf) mEl.pdf.addEventListener('change', function () {
    var f = mEl.pdf.files[0]; if (!f) return clearFile();
    if (f.size > 5 * 1024 * 1024) { showError('Tệp PDF tối đa 5 MB.'); return clearFile(); }
    el.image.value = ''; el.fileName.textContent = f.name; el.file.hidden = false;
  });
  if (mEl.replyCancel) mEl.replyCancel.addEventListener('click', clearReply);
  if (mEl.geo) mEl.geo.addEventListener('click', function () {
    if (!navigator.geolocation) { showError('Trình duyệt không hỗ trợ chia sẻ vị trí.'); return; }
    if (!confirm('Gửi vị trí hiện tại của bạn vào cuộc trò chuyện này (một lần)?')) return;
    navigator.geolocation.getCurrentPosition(function (pos) {
      var fd = new FormData(); fd.append('Latitude', pos.coords.latitude.toFixed(5)); fd.append('Longitude', pos.coords.longitude.toFixed(5));
      if (st.replyTo) fd.append('ReplyToId', st.replyTo);
      if (st.openId) fd.append('ConversationId', st.openId); else if (st.startBranch) fd.append('BranchId', st.startBranch); else if (st.startPeer) fd.append('PeerId', st.startPeer); else return;
      api('/chat/gui', { method: 'POST', body: fd }).then(function (r) {
        clearReply();
        if (!st.openId) { st.openId = r.conversationId; st.startBranch = null; st.startPeer = null; history.replaceState({ id: r.conversationId }, '', base + '/' + r.conversationId); return openThread(r.conversationId, true).then(loadList); }
        fetchNew(); loadList();
      }).catch(function (e) { showError(e.message); });
    }, function () { showError('Không lấy được vị trí. Hãy cho phép trình duyệt chia sẻ vị trí.'); }, { timeout: 10000 });
  });
  if (mEl.emojiBtn) {
    EMOJIS.forEach(function (e) { var b = h('button', null, e); b.type = 'button'; b.addEventListener('click', function () { var s = el.input.selectionStart || el.input.value.length; el.input.value = el.input.value.slice(0, s) + e + el.input.value.slice(s); el.input.focus(); el.input.selectionStart = el.input.selectionEnd = s + e.length; autosize(); }); mEl.emojiPop.appendChild(b); });
    mEl.emojiBtn.addEventListener('click', function () { mEl.emojiPop.hidden = !mEl.emojiPop.hidden; });
    document.addEventListener('click', function (e) { if (!mEl.emojiPop.hidden && !mEl.emojiPop.contains(e.target) && e.target !== mEl.emojiBtn) mEl.emojiPop.hidden = true; });
  }
  el.back.addEventListener('click', function () { app.classList.remove('chat-open'); st.openId = null; history.pushState({}, '', base); renderList(); });

  // ---------------------------------------------------------------- dat dich vu ngay trong chat
  function setupBooking(branchId, canSend) {
    el.bookToggle.hidden = true; el.book.hidden = true;
    if (!canSend) return;
    api('/chat/co-so/' + branchId + '/dich-vu').then(function (list) {
      st.services = list; if (!list.length) return;
      var sel = el.book.querySelector('[data-book="service"]'); sel.replaceChildren();
      list.forEach(function (x) { var o = h('option', null, x.name + (x.price != null ? ' · ' + MONEY.format(x.price) + ' ₫' + (x.unit ? '/' + x.unit : '') : '')); o.value = x.id; sel.appendChild(o); });
      var start = el.book.querySelector('[data-book="start"]'), today = new Date(), iso = today.getFullYear() + '-' + two(today.getMonth() + 1) + '-' + two(today.getDate());
      start.min = iso; el.book.querySelector('[data-book="end"]').min = iso;
      if (!start.value) { var d = new Date(Date.now() + 7 * 86400000); start.value = d.getFullYear() + '-' + two(d.getMonth() + 1) + '-' + two(d.getDate()); }
      el.bookToggle.hidden = false;
      if (st.pickService && list.some(function (x) { return String(x.id) === st.pickService; })) { sel.value = st.pickService; el.book.hidden = false; st.pickService = null; }
    }).catch(function () {});
  }
  function bookValue(n) { return el.book.querySelector('[data-book="' + n + '"]').value; }
  var checkTimer;
  function bookingCheck() {
    clearTimeout(checkTimer);
    checkTimer = setTimeout(function () {
      var box = el.book.querySelector('[data-book-check]'); if (!box || el.book.hidden) return;
      var qs = new URLSearchParams({ ServiceId: bookValue('service'), Start: bookValue('start'), People: bookValue('people') || '1', Quantity: bookValue('quantity') || '1' });
      if (!bookValue('start')) { box.replaceChildren(); return; }
      if (bookValue('end')) qs.set('End', bookValue('end'));
      if (st.openId) qs.set('ConversationId', st.openId); else if (st.startBranch) qs.set('BranchId', st.startBranch); else return;
      api('/chat/dat/kiem-tra?' + qs.toString()).then(function (r) {
        box.replaceChildren();
        if (!r.ok) { box.appendChild(h('p', 'chat-check-bad', r.message)); return; }
        if (r.estimate != null) box.appendChild(h('p', 'chat-check-est', 'Tạm tính khoảng ' + MONEY.format(r.estimate) + ' ₫' + (r.unit ? ' (giá ' + MONEY.format(r.price) + ' ₫/' + r.unit + ')' : '')));
        (r.warnings || []).forEach(function (w) { box.appendChild(h('p', 'chat-check-warn', w)); });
        if (r.note) box.appendChild(h('small', null, r.note));
      }).catch(function () {});
    }, 400);
  }
  if (el.bookToggle) {
    el.book.addEventListener('input', bookingCheck); el.book.addEventListener('change', bookingCheck);
    el.bookToggle.addEventListener('click', function () { el.book.hidden = !el.book.hidden; if (!el.book.hidden) bookingCheck(); });
    el.bookClose.addEventListener('click', function () { el.book.hidden = true; });
    el.book.addEventListener('submit', function (e) {
      e.preventDefault(); if (st.sending) return;
      var fd = new FormData();
      fd.append('ServiceId', bookValue('service')); fd.append('Start', bookValue('start')); if (bookValue('end')) fd.append('End', bookValue('end'));
      fd.append('People', bookValue('people')); fd.append('Quantity', bookValue('quantity')); fd.append('Notes', bookValue('notes')); fd.append('ArrivalDeparture', bookValue('arrival'));
      fd.append('SubmissionId', (window.crypto && crypto.randomUUID) ? crypto.randomUUID() : '00000000-0000-4000-8000-' + String(Date.now()).padStart(12, '0'));
      if (st.openId) fd.append('ConversationId', st.openId); else if (st.startBranch) fd.append('BranchId', st.startBranch); else return;
      st.sending = true; showError('');
      api('/chat/dat', { method: 'POST', body: fd }).then(function (r) {
        el.book.hidden = true; el.book.querySelector('[data-book="notes"]').value = '';
        if (!st.openId) { st.openId = r.conversationId; st.startBranch = null; st.startPeer = null; history.replaceState({ id: r.conversationId }, '', base + '/' + r.conversationId); return openThread(r.conversationId, true).then(loadList); }
        fetchNew(); refreshCards(); loadList();
      }).catch(function (err) { showError(err.message); }).then(function () { st.sending = false; });
    });
  }

  // ---------------------------------------------------------------- ru di cung (ket noi dong hanh) ngay trong chat
  function setupConnect(peerId, canSend) {
    el.connectToggle.hidden = true; el.connect.hidden = true; st.peerId = peerId || null; st.invitable = [];
    if (!peerId || !canSend) return;
    api('/chat/nguoi/' + peerId + '/chuyen-di').then(function (list) {
      if (st.peerId !== peerId) return;
      var open = list.filter(function (p) { return p.state === 'can'; }); st.invitable = open;
      var sel = el.connect.querySelector('[data-connect="need"]'); sel.replaceChildren();
      open.forEach(function (p) { var o = h('option', null, p.name + ' · ' + p.destination + ' · ' + dmy(p.start) + ' – ' + dmy(p.end)); o.value = p.id; sel.appendChild(o); });
      el.connect.querySelectorAll('[data-connect-pick]').forEach(function (x) { x.hidden = !open.length; });
      el.connect.querySelector('[data-connect-send]').hidden = !open.length;
      el.connect.querySelector('[data-connect-empty]').hidden = !!open.length;
      var waiting = list.filter(function (p) { return p.state === 'pending'; }), joined = list.filter(function (p) { return p.state === 'accepted'; });
      if (joined.length) el.meta.appendChild(h('span', null, 'Cùng chuyến đi: ' + joined.map(function (p) { return p.name; }).join(' · ')));
      if (waiting.length) el.meta.appendChild(h('span', null, 'Đã mời, chờ trả lời: ' + waiting.map(function (p) { return p.name; }).join(' · ')));
      el.connectToggle.hidden = false;
    }).catch(function () {});
  }
  if (el.connectToggle) {
    el.connectToggle.addEventListener('click', function () { el.connect.hidden = !el.connect.hidden; });
    el.connectClose.addEventListener('click', function () { el.connect.hidden = true; });
    el.connect.addEventListener('submit', function (e) {
      e.preventDefault(); if (st.sending || !st.peerId) return;
      var planId = el.connect.querySelector('[data-connect="need"]').value, note = el.connect.querySelector('[data-connect="text"]').value.trim();
      var plan = (st.invitable || []).filter(function (p) { return String(p.id) === String(planId); })[0]; if (!plan) return;
      var fd = new FormData(); fd.append('planId', planId); fd.append('accountId', st.peerId);
      st.sending = true; showError('');
      api('/chat/moi-chuyen-di', { method: 'POST', body: fd }).then(function () {
        // bao ngay trong cuoc tro chuyen de nguoi nhan biet can mo muc Chuyen di
        var msg = new FormData(); msg.append('Body', 'Mình vừa mời bạn vào chuyến đi “' + plan.name + '” (' + plan.destination + ', ' + dmy(plan.start) + ' – ' + dmy(plan.end) + '). Bạn mở mục Chuyến đi để chấp nhận nhé.' + (note ? '\n' + note : ''));
        if (st.openId) msg.append('ConversationId', st.openId); else if (st.startPeer) msg.append('PeerId', st.startPeer);
        return api('/chat/gui', { method: 'POST', body: msg });
      }).then(function (r) {
        el.connect.hidden = true; el.connect.querySelector('[data-connect="text"]').value = '';
        if (!st.openId) { st.openId = r.conversationId; st.startPeer = null; history.replaceState({ id: r.conversationId }, '', base + '/' + r.conversationId); return openThread(r.conversationId, true).then(loadList); }
        fetchNew(); loadList(); setupConnect(st.peerId, true);
      }).catch(function (err) { showError(err.message); }).then(function () { st.sending = false; });
    });
  }

  // ---------------------------------------------------------------- phia co so
  function score(r, text) { if (!text) return 0; return r.title.toLowerCase().split(/\s+/).filter(function (w) { return w.length >= 3 && text.indexOf(w) >= 0; }).length; }
  function loadQuick(branchId) {
    api('/chat/tin-mau/' + branchId).then(function (rows) {
      el.quick.replaceChildren(); el.quick.hidden = !rows.length;
      var lastIn = ''; for (var k = st.lastMessages.length - 1; k >= 0; k--) { if (!st.lastMessages[k].mine && st.lastMessages[k].body) { lastIn = st.lastMessages[k].body.toLowerCase(); break; } }
      rows.sort(function (x, y) { return score(y, lastIn) - score(x, lastIn); });
      rows.forEach(function (r) { var b = h('button', 'chat-chip' + (score(r, lastIn) > 0 ? ' suggest' : ''), (score(r, lastIn) > 0 ? '✨ ' : '') + r.title); b.type = 'button'; b.title = r.body; b.addEventListener('click', function () { el.input.value = (el.input.value ? el.input.value + '\n' : '') + r.body; autosize(); el.input.focus(); }); el.quick.appendChild(b); });
    }).catch(function () { el.quick.hidden = true; });
  }
  var reportBtn = q('[data-chat-report]'); if (reportBtn) reportBtn.addEventListener('click', function () { if (st.openId) sendReport(null); });
  el.blockBtn.addEventListener('click', function () {
    if (!st.thread) return; var fd = new FormData(); fd.append('block', st.thread.blockedByMe ? 'false' : 'true');
    api('/chat/' + st.openId + '/chan', { method: 'POST', body: fd }).then(function () { openThread(st.openId, true); }).catch(function (e) { showError(e.message); });
  });
  if (isProvider) {
    el.stateBtn.addEventListener('click', function () {
      if (!st.thread) return; var next = st.thread.item.state === 'Done' ? 'Open' : 'Done'; var fd = new FormData(); fd.append('State', next);
      api('/chat/' + st.openId + '/trang-thai', { method: 'POST', body: fd }).then(function () { st.thread.item.state = next; el.stateBtn.textContent = next === 'Done' ? 'Mở lại' : 'Đánh dấu đã xử lý'; loadList(); }).catch(function (e) { showError(e.message); });
    });
    function saveMeta() { if (!st.openId) return; var fd = new FormData(); fd.append('Note', el.note.value); fd.append('Label', el.label.value); api('/chat/' + st.openId + '/trang-thai', { method: 'POST', body: fd }).then(loadList).catch(function (e) { showError(e.message); }); }
    el.note.addEventListener('change', saveMeta); el.label.addEventListener('change', saveMeta);
    app.querySelectorAll('[data-chat-filter]').forEach(function (b) { b.addEventListener('click', function () { app.querySelectorAll('[data-chat-filter]').forEach(function (x) { x.classList.toggle('active', x === b); }); st.filter = b.dataset.chatFilter; loadList(); }); });
    el.branchSel.addEventListener('change', function () { st.branch = el.branchSel.value; loadList(); });
    var timer; el.search.addEventListener('input', function () { clearTimeout(timer); timer = setTimeout(function () { st.term = el.search.value.trim(); loadList(); }, 300); });
  }

  // ---------------------------------------------------------------- su kien thoi gian thuc
  document.addEventListener('chat:event', function (ev) {
    var d = ev.detail || {};
    if (d.t === 'upd') { if (d.c === st.openId) replaceMessage(d.m); loadList(); }
    else if (d.t === 'msg') { if (d.c === st.openId) fetchNew(); loadList(); }
    else if (d.t === 'read' && d.c === st.openId) { if (!inGroup() && (d.side === 'Traveler') !== iAmFirst()) { st.otherRead = d.r; updateReceipts(); } }
    else if (d.t === 'typing' && d.c === st.openId && (inGroup() ? d.account !== myAccount : (d.side === 'Traveler') !== iAmFirst())) {
      el.typing.textContent = (inGroup() && d.name ? d.name + ' đang soạn tin…' : 'Đang soạn tin…'); el.typing.hidden = false; clearTimeout(st.typingTimer); st.typingTimer = setTimeout(function () { el.typing.hidden = true; }, 4000);
    }
  });
  document.addEventListener('visibilitychange', function () { if (!document.hidden) { fetchNew(); markRead(); loadList(); } });
  // Du phong khi ket noi su kien bi chan: lam moi dinh ky
  setInterval(function () { if (!document.hidden) { loadList(); fetchNew(); } }, 20000);
  window.addEventListener('popstate', function () {
    var m = location.pathname.match(/\/(\d+)$/);
    if (m) openThread(Number(m[1]), true); else { setPane(false); st.openId = null; renderList(); }
  });

  // ---------------------------------------------------------------- chat nhom
  var gEl = { create: q('[data-chat-create-group]'), open: q('[data-chat-open-create]'), close: q('[data-chat-create-close]'), title: q('[data-chat-group-title]'), friends: q('[data-chat-group-friends]'), go: q('[data-chat-group-go]'),
    panel: q('[data-chat-members]'), toggle: q('[data-chat-members-toggle]'), list: q('[data-chat-members-list]'), rename: q('[data-chat-rename]'), renameBtn: q('[data-chat-rename-go]'), leave: q('[data-chat-leave]'), addBox: q('[data-chat-add-box]'), addBtn: q('[data-chat-add-go]') };
  function friendChecklist(box, exclude, name) {
    box.replaceChildren();
    return api('/api/mxh/ban-be').then(function (list) {
      var free = list.filter(function (p) { return exclude.indexOf(p.id) < 0; });
      if (!free.length) { box.appendChild(h('p', 'soc-empty', exclude.length ? 'Không còn người bạn nào để thêm.' : 'Bạn chưa có bạn bè nào. Hãy kết bạn trước ở mục Bạn bè.')); return 0; }
      free.forEach(function (p) { var lab = h('label', 'chat-check'); var cb = h('input'); cb.type = 'checkbox'; cb.value = p.id; cb.name = name; lab.appendChild(cb); lab.appendChild(document.createTextNode(' ' + p.name)); box.appendChild(lab); });
      return free.length;
    });
  }
  function checked(box) { return Array.prototype.map.call(box.querySelectorAll('input:checked'), function (i) { return i.value; }); }
  if (gEl.open) {
    gEl.open.addEventListener('click', function () { setPane(false); gEl.create.hidden = false; el.placeholder.hidden = true; gEl.title.value = ''; friendChecklist(gEl.friends, [], 'm'); });
    gEl.close.addEventListener('click', function () { gEl.create.hidden = true; el.placeholder.hidden = false; });
    gEl.go.addEventListener('click', function () {
      var ids = checked(gEl.friends); if (!ids.length) { showError('Chọn ít nhất một người bạn.'); gEl.create.hidden = false; return; }
      var fd = new FormData(); fd.append('Title', gEl.title.value); ids.forEach(function (i) { fd.append('MemberIds', i); }); gEl.go.disabled = true; showError('');
      api('/chat/nhom', { method: 'POST', body: fd }).then(function (r) { gEl.create.hidden = true; return openThread(r.conversationId).then(loadList); }).catch(function (e) { alert(e.message); }).then(function () { gEl.go.disabled = false; });
    });
  }
  function renderMembers(t) {
    var group = !!t.item.group; gEl.toggle.hidden = !group; if (!group) { gEl.panel.hidden = true; return; }
    gEl.rename.value = t.item.who; var admin = t.myRole === 'Admin'; gEl.list.replaceChildren();
    (t.members || []).forEach(function (m) {
      var li = h('li', 'chat-member'); li.appendChild(h('span', null, m.name + (m.me ? ' (bạn)' : '') + (m.role === 'Admin' ? ' · Quản trị' : '')));
      if (admin && !m.me) { var rm = h('button', 'text-link', 'Xóa'); rm.type = 'button'; rm.addEventListener('click', function () { if (!confirm('Xóa ' + m.name + ' khỏi nhóm?')) return; var fd = new FormData(); fd.append('accountId', m.id); api('/chat/' + st.openId + '/nhom/xoa', { method: 'POST', body: fd }).then(function () { openThread(st.openId, true); }).catch(function (e) { showError(e.message); }); }); li.appendChild(rm); }
      gEl.list.appendChild(li);
    });
    friendChecklist(gEl.addBox, (t.members || []).map(function (m) { return m.id; }), 'a');
  }
  if (gEl.toggle) {
    gEl.toggle.addEventListener('click', function () { gEl.panel.hidden = !gEl.panel.hidden; });
    gEl.renameBtn.addEventListener('click', function () { var fd = new FormData(); fd.append('title', gEl.rename.value); api('/chat/' + st.openId + '/nhom/doi-ten', { method: 'POST', body: fd }).then(function () { openThread(st.openId, true); loadList(); }).catch(function (e) { showError(e.message); }); });
    gEl.addBtn.addEventListener('click', function () {
      var ids = checked(gEl.addBox); if (!ids.length) { showError('Chọn người cần thêm.'); return; }
      var fd = new FormData(); ids.forEach(function (i) { fd.append('AccountIds', i); });
      api('/chat/' + st.openId + '/nhom/them', { method: 'POST', body: fd }).then(function () { openThread(st.openId, true); }).catch(function (e) { showError(e.message); });
    });
    gEl.leave.addEventListener('click', function () {
      if (!confirm('Rời nhóm này? Bạn sẽ không nhận tin nhắn của nhóm nữa.')) return;
      api('/chat/' + st.openId + '/nhom/roi', { method: 'POST' }).then(function () { st.openId = null; st.thread = null; setPane(false); history.pushState({}, '', base); loadList(); }).catch(function (e) { showError(e.message); });
    });
  }

  // ---------------------------------------------------------------- tin nhan moi: tim ban be / co so
  var cEl = { open: q('[data-chat-open-compose]'), box: q('[data-chat-compose]'), input: q('[data-chat-compose-q]'), results: q('[data-chat-compose-results]') };
  if (cEl.open) {
    var composeTimer;
    cEl.open.addEventListener('click', function () { cEl.box.hidden = !cEl.box.hidden; if (!cEl.box.hidden) { cEl.input.focus(); } });
    cEl.input.addEventListener('input', function () {
      clearTimeout(composeTimer);
      composeTimer = setTimeout(function () {
        var term = cEl.input.value.trim(); cEl.results.replaceChildren();
        if (term.length < 2) return;
        api('/chat/tim?q=' + encodeURIComponent(term)).then(function (r) {
          function item(label, sub, href) { var a = h('a', 'chat-compose-item'); a.href = href; a.appendChild(h('strong', null, label)); if (sub) a.appendChild(h('small', null, sub)); cEl.results.appendChild(a); }
          r.people.forEach(function (p) { item(p.name, p.relation === 'friends' ? 'Bạn bè' : 'Có lời rủ đi cùng', '/tin-nhan/nguoi/' + p.id); });
          r.branches.forEach(function (b) { item(b.name, 'Cơ sở · ' + b.address, '/tin-nhan/co-so/' + b.id); });
          if (!r.people.length && !r.branches.length) cEl.results.appendChild(h('p', 'soc-empty', 'Không tìm thấy bạn bè hoặc cơ sở phù hợp.'));
        }).catch(function (e) { cEl.results.appendChild(h('p', 'soc-empty', e.message)); });
      }, 300);
    });
  }

  var notifyBtn = q('[data-chat-notify]');
  if (notifyBtn && 'Notification' in window && Notification.permission === 'default') {
    notifyBtn.hidden = false;
    notifyBtn.addEventListener('click', function () { Notification.requestPermission().then(function () { notifyBtn.hidden = true; }); });
  }

  // ---------------------------------------------------------------- khoi tao
  var startNode = document.getElementById('chat-start');
  loadList().then(function () {
    if (app.dataset.open) return openThread(Number(app.dataset.open), true);
    if (startNode) { try { var info = JSON.parse(startNode.textContent); if (info.peerId) startPeerMode(info); else startMode(info); } catch (x) { /* bo qua */ } }
  });
})();
