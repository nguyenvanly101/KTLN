// Bang dat dich vu tren trang dich vu: tam tinh gia, canh bao ngay, gui yeu cau vao cuoc tro chuyen voi co so.
(function () {
  var dlg = document.getElementById('book-sheet'), open = document.querySelector('[data-book-open]');
  if (!dlg || !open) return;
  var form = dlg.querySelector('[data-book-form]'), warn = dlg.querySelector('[data-book-warn]'), total = dlg.querySelector('[data-book-total]'), basis = dlg.querySelector('[data-book-basis]'), send = dlg.querySelector('[data-book-send]');
  var token = (form.querySelector('[name="__RequestVerificationToken"]') || {}).value || '';
  var money = new Intl.NumberFormat('vi-VN');
  function values() { var o = {}; new FormData(form).forEach(function (v, k) { if (k !== '__RequestVerificationToken' && v !== '') o[k] = v; }); return o; }
  function say(msg, bad) { warn.hidden = !msg; warn.className = 'book-warn' + (bad ? ' bad' : ''); warn.textContent = msg || ''; }
  var timer = null, seq = 0;
  function check() {
    clearTimeout(timer);
    timer = setTimeout(function () {
      var my = ++seq, q = new URLSearchParams(values()); q.delete('Notes'); q.delete('SubmissionId');
      fetch('/chat/dat/kiem-tra?' + q.toString(), { credentials: 'same-origin' }).then(function (r) { return r.json(); }).then(function (j) {
        if (my !== seq) return;
        total.textContent = j.estimate != null ? money.format(j.estimate) + ' ₫' : 'Cơ sở báo giá';
        basis.textContent = j.price != null ? 'Tạm tính: ' + money.format(j.price) + ' ₫ / ' + (j.unit || 'đơn vị') : 'Chưa có giá tham khảo';
        if (!j.ok) { say(j.message || 'Khoảng ngày này chưa đặt được.', true); send.disabled = true; return; }
        send.disabled = false; say((j.warnings || []).join(' '), false);
      }).catch(function () { if (my === seq) { total.textContent = '…'; } });
    }, 250);
  }
  open.addEventListener('click', function () { dlg.showModal(); check(); });
  dlg.querySelector('[data-book-close]').addEventListener('click', function () { dlg.close(); });
  form.addEventListener('input', check);
  form.addEventListener('submit', function (ev) {
    ev.preventDefault(); if (send.disabled) return;
    var v = values(), fd = new FormData(); Object.keys(v).forEach(function (k) { fd.append(k, v[k]); });
    send.disabled = true; send.textContent = 'Đang gửi…';
    fetch('/chat/dat', { method: 'POST', body: fd, credentials: 'same-origin', headers: { RequestVerificationToken: token } })
      .then(function (r) { return r.json().then(function (j) { if (!r.ok) throw new Error(j.message || 'Không gửi được yêu cầu.'); return j; }); })
      .then(function (j) {
        var plan = form.getAttribute('data-plan'), cid = j.conversationId, rid = j.message && j.message.booking;
        var next = function () { location.href = '/tin-nhan/' + cid; };
        if (!plan || !rid) return next();
        var p = new FormData(); p.append('requestId', rid); p.append('planId', plan);
        return fetch('/chat/' + cid + '/dat/chuyen-di', { method: 'POST', body: p, credentials: 'same-origin', headers: { RequestVerificationToken: token } }).then(next, next);
      })
      .catch(function (e) { say(e.message, true); send.disabled = false; send.textContent = 'Gửi yêu cầu cho cơ sở'; });
  });
  if (location.hash === '#dat') { dlg.showModal(); check(); }
})();
