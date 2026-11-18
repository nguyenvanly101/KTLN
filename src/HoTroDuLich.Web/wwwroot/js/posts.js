// Bang tin kieu Facebook: dang bai (chu + toi da 4 anh), sua, xoa, thich, binh luan (tra loi, sua, xoa), phan trang.
(function () {
  'use strict';
  if (!window.Soc) return;
  var S = window.Soc, h = S.h, api = S.api, form = S.form, toast = S.toast;
  var VIS = { Public: 'Công khai', Friends: 'Bạn bè', Private: 'Chỉ mình tôi' };
  function two(n) { return n < 10 ? '0' + n : '' + n; }
  function when(iso) {
    var d = new Date(iso), s = (Date.now() - d.getTime()) / 1000;
    if (s < 60) return 'Vừa xong'; if (s < 3600) return Math.floor(s / 60) + ' phút trước'; if (s < 86400) return Math.floor(s / 3600) + ' giờ trước';
    return two(d.getDate()) + '/' + two(d.getMonth() + 1) + '/' + d.getFullYear() + ' ' + two(d.getHours()) + ':' + two(d.getMinutes());
  }
  function person(p) { return { name: p.author, avatar: p.avatar }; }


  // ---------------------------------------------------------------- cam xuc, anh lon
  var EMOJI = { like: '👍', love: '❤️', haha: '😆', wow: '😮', sad: '😢', angry: '😡' };
  var EMOJI_NAME = { like: 'Thích', love: 'Yêu thích', haha: 'Haha', wow: 'Wow', sad: 'Buồn', angry: 'Phẫn nộ' };
  var EMOJI_ORDER = ['like', 'love', 'haha', 'wow', 'sad', 'angry'];
  function reactionTotal(map) { var n = 0; for (var k in (map || {})) n += map[k]; return n; }
  function topEmoji(map) { return EMOJI_ORDER.filter(function (k) { return (map || {})[k]; }).sort(function (a, b) { return map[b] - map[a]; }).slice(0, 3); }
  function lightbox(urls, start) {
    var i = start, ov = h('div', 'lb-overlay'); ov.setAttribute('role', 'dialog'); ov.setAttribute('aria-label', 'Xem ảnh');
    var img = h('img', 'lb-img'); var close = h('button', 'lb-close', '×'); close.type = 'button'; close.setAttribute('aria-label', 'Đóng');
    var prev = h('button', 'lb-nav lb-prev', '‹'), next = h('button', 'lb-nav lb-next', '›'); prev.type = next.type = 'button'; prev.setAttribute('aria-label', 'Ảnh trước'); next.setAttribute('aria-label', 'Ảnh sau');
    var count = h('span', 'lb-count');
    function show() { img.src = urls[i]; count.textContent = (i + 1) + ' / ' + urls.length; prev.hidden = next.hidden = urls.length < 2; }
    function go(d) { i = (i + d + urls.length) % urls.length; show(); }
    function stop() { document.removeEventListener('keydown', key); ov.remove(); }
    function key(e) { if (e.key === 'Escape') stop(); else if (e.key === 'ArrowLeft') go(-1); else if (e.key === 'ArrowRight') go(1); }
    close.addEventListener('click', stop); prev.addEventListener('click', function (e) { e.stopPropagation(); go(-1); }); next.addEventListener('click', function (e) { e.stopPropagation(); go(1); });
    ov.addEventListener('click', function (e) { if (e.target === ov) stop(); }); document.addEventListener('keydown', key);
    ov.appendChild(close); ov.appendChild(prev); ov.appendChild(img); ov.appendChild(next); ov.appendChild(count); document.body.appendChild(ov); show(); close.focus();
  }
  function modal(title, body) {
    var ov = h('div', 'lb-overlay'); var box = h('div', 'fb-modal'); var head = h('div', 'fb-modal-head'); head.appendChild(h('strong', null, title));
    var x = h('button', 'lb-close inline', '×'); x.type = 'button'; x.setAttribute('aria-label', 'Đóng'); head.appendChild(x); box.appendChild(head); box.appendChild(body); ov.appendChild(box);
    function stop() { document.removeEventListener('keydown', key); ov.remove(); } function key(e) { if (e.key === 'Escape') stop(); }
    x.addEventListener('click', stop); ov.addEventListener('click', function (e) { if (e.target === ov) stop(); }); document.addEventListener('keydown', key); document.body.appendChild(ov); return stop;
  }

  // ---------------------------------------------------------------- binh luan
  function commentsBox(post, countEl) {
    var box = h('div', 'post-comments'), list = h('div'), loaded = false;
    box.appendChild(list);
    function renderAll(items) {
      list.replaceChildren();
      var tops = items.filter(function (c) { return !c.parentId; });
      tops.forEach(function (c) {
        var node = commentNode(c, post, reload); var replies = items.filter(function (r) { return r.parentId === c.id; });
        var wrap = h('div'); wrap.appendChild(node);
        if (replies.length) { var rb = h('div', 'comment-replies'); replies.forEach(function (r) { rb.appendChild(commentNode(r, post, reload)); }); wrap.appendChild(rb); }
        list.appendChild(wrap);
      });
      var live = items.filter(function (c) { return !c.deleted; }).length;
      if (countEl) countEl.textContent = live + ' bình luận';
    }
    function reload() { return api('/api/mxh/bai-viet/' + post.id + '/binh-luan').then(renderAll).catch(function (e) { toast(e.message); }); }
    box.appendChild(commentForm(post.id, null, function () { reload(); }));
    box.load = function () { if (!loaded) { loaded = true; reload(); } };
    return box;
  }
  function commentForm(postId, parentId, done, placeholder) {
    var f = h('form', 'comment-form'); var input = h('input', 'form-control'); input.type = 'text'; input.maxLength = 1500; input.placeholder = placeholder || 'Viết bình luận…'; input.setAttribute('aria-label', 'Bình luận');
    var b = h('button', 'btn btn-dark', 'Gửi'); b.type = 'submit'; f.appendChild(input); f.appendChild(b);
    f.addEventListener('submit', function (e) {
      e.preventDefault(); var text = input.value.trim(); if (!text) return; b.disabled = true;
      var fd = form({ Content: text }); if (parentId) fd.append('ParentId', parentId);
      api('/api/mxh/bai-viet/' + postId + '/binh-luan', { method: 'POST', body: fd }).then(function () { input.value = ''; done(); }).catch(function (err) { toast(err.message); }).then(function () { b.disabled = false; });
    });
    return f;
  }
  function commentNode(c, post, reload) {
    var row = h('div', 'comment');
    if (c.deleted) { row.appendChild(h('small', 'soc-meta', 'Bình luận đã bị xóa')); return row; }
    row.appendChild(S.avatar({ name: c.author, avatar: c.avatar }, 'small'));
    var col = h('div'); col.style.minWidth = '0';
    var bubble = h('div', 'comment-bubble'); var a = h(c.link === '' ? 'span' : 'a'); if (c.link !== '') a.href = c.link || ('/nguoi/' + c.authorId); a.appendChild(h('strong', null, c.author)); bubble.appendChild(a);
    var text = h('p', null, c.content); bubble.appendChild(text); col.appendChild(bubble);
    var meta = h('div', 'comment-meta'); meta.appendChild(h('span', null, when(c.createdAt) + (c.updatedAt ? ' · đã sửa' : '')));
    var like = h('button', c.liked ? 'on' : '', 'Thích' + (c.likes ? ' (' + c.likes + ')' : '')); like.type = 'button';
    like.addEventListener('click', function () { api('/api/mxh/cam-xuc', { method: 'POST', body: form({ type: 'Comment', target: c.id }) }).then(function (r) { c.liked = !!r.mine; c.likes = r.count; like.className = c.liked ? 'on' : ''; like.textContent = 'Thích' + (c.likes ? ' (' + c.likes + ')' : ''); }).catch(function (e) { toast(e.message); }); });
    meta.appendChild(like);
    if (!c.parentId) { var reply = h('button', null, 'Trả lời'); reply.type = 'button'; reply.addEventListener('click', function () { if (col.querySelector('.reply-form')) return; var f = commentForm(post.id, c.id, reload, 'Trả lời ' + c.author + '…'); f.classList.add('reply-form'); col.appendChild(f); f.querySelector('input').focus(); }); meta.appendChild(reply); }
    if (c.mine) {
      var edit = h('button', null, 'Sửa'); edit.type = 'button';
      edit.addEventListener('click', function () {
        var input = h('input', 'form-control'); input.value = c.content; input.maxLength = 1500; var save = h('button', 'btn btn-dark', 'Lưu'); save.type = 'button'; var cancel = h('button', 'btn btn-outline', 'Hủy'); cancel.type = 'button';
        var f = h('div', 'comment-form'); f.appendChild(input); f.appendChild(save); f.appendChild(cancel); bubble.replaceWith(f); input.focus();
        cancel.addEventListener('click', function () { f.replaceWith(bubble); });
        save.addEventListener('click', function () { api('/api/mxh/binh-luan/' + c.id + '/sua', { method: 'POST', body: form({ Content: input.value }) }).then(reload).catch(function (e) { toast(e.message); }); });
      });
      meta.appendChild(edit);
    }
    if (c.canDelete) { var del = h('button', null, 'Xóa'); del.type = 'button'; del.addEventListener('click', function () { if (!confirm('Xóa bình luận này?')) return; api('/api/mxh/binh-luan/' + c.id + '/xoa', { method: 'POST' }).then(reload).catch(function (e) { toast(e.message); }); }); meta.appendChild(del); }
    col.appendChild(meta); row.appendChild(col); return row;
  }

  // ---------------------------------------------------------------- bai viet
  function renderPost(p, opts) {
    opts = opts || {};
    var card = h('article', 'post panel'); card.dataset.id = p.id;
    var head = h('div', 'post-head'); head.appendChild(S.avatar(person(p)));
    var main = h('div', 'post-head-main'); var name = h('a'); name.href = p.kind === 'Branch' && p.branchId ? '/co-so/' + p.branchId : '/nguoi/' + p.authorId; name.textContent = p.author; main.appendChild(name);
    var sub = h('small'); var link = h('a'); link.href = '/bai-viet/' + p.id; link.textContent = when(p.createdAt); sub.appendChild(link); sub.appendChild(document.createTextNode(' · ' + VIS[p.visibility] + (p.updatedAt ? ' · đã chỉnh sửa' : ''))); main.appendChild(sub); head.appendChild(main);
    var body = h('p', 'post-body', p.content);
    var embed = null;
    if (p.shared) {
      embed = h('div', 'post-embed');
      if (p.shared.gone) embed.appendChild(h('small', 'soc-meta', 'Bài viết gốc không còn hiển thị.'));
      else {
        var ea = h('a'); ea.href = p.shared.kind === 'Branch' && p.shared.branchId ? '/co-so/' + p.shared.branchId : '/bai-viet/' + p.shared.id; ea.appendChild(h('strong', null, p.shared.author)); embed.appendChild(ea);
        embed.appendChild(h('small', 'soc-meta', ' · ' + when(p.shared.createdAt)));
        embed.appendChild(h('p', null, p.shared.content));
        if (p.shared.photos) { var el = h('a', 'text-link', p.shared.photos + ' ảnh trong bài gốc'); el.href = '/bai-viet/' + p.shared.id; embed.appendChild(el); }
      }
    }
    var extra = null;
    if (p.kind === 'Branch') { var bd = h('span', 'post-badge', 'Giới thiệu cơ sở'); main.appendChild(bd); }
    if (p.kind === 'Companion') {
      var bd2 = h('span', 'post-badge', 'Tìm bạn đồng hành'); main.appendChild(bd2);
      var trip = h('div', 'post-trip-info', '📍 ' + p.destination + ' · ' + p.tripStart + ' → ' + p.tripEnd);
      if (!p.mine) { var msg = h('a', 'btn btn-dark', 'Nhắn tin'); msg.href = '/tin-nhan/nguoi/' + p.authorId; trip.appendChild(msg); }
      extra = trip;
    }
    if (p.kind === 'Branch' && p.branchId && !p.mine && document.querySelector('[data-soc-feed]') && document.querySelector('[data-soc-feed]').dataset.role === 'traveler') {
      var fol = h('button', 'btn btn-outline post-follow', p.following ? 'Đang theo dõi' : '+ Theo dõi'); fol.type = 'button';
      fol.addEventListener('click', function () { api('/api/mxh/co-so/' + p.branchId + '/theo-doi', { method: 'POST', body: form({ follow: !p.following }) }).then(function (r) { p.following = r.following; fol.textContent = p.following ? 'Đang theo dõi' : '+ Theo dõi'; toast(p.following ? 'Bài viết của cơ sở này sẽ hiện ở Dành cho bạn.' : 'Đã bỏ theo dõi.'); }).catch(function (e) { toast(e.message); }); });
      head.appendChild(fol);
    }
    if (p.kind === 'Branch' && p.branchId) { var go2 = h('a', 'text-link', 'Xem cơ sở'); go2.href = '/co-so/' + p.branchId; var msg2 = h('a', 'btn btn-outline', 'Nhắn tin cơ sở'); msg2.href = '/tin-nhan/co-so/' + p.branchId; var wrapB = h('div', 'post-trip-info'); wrapB.appendChild(go2); wrapB.appendChild(msg2); extra = wrapB; }
    var photos = h('div', 'post-photos n' + Math.min(p.photos, 4) + (p.photos === 1 ? ' one' : ''));
    var urls = []; for (var i = 0; i < p.photos; i++) urls.push('/mxh/bai-viet/' + p.id + '/anh/' + i);
    urls.slice(0, 4).forEach(function (u, idx) {
      var img = h('img'); img.src = u; img.alt = 'Ảnh ' + (idx + 1) + ' trong bài viết'; img.loading = 'lazy'; img.tabIndex = 0;
      var cell = h('div', 'photo-cell'); cell.appendChild(img);
      if (idx === 3 && p.photos > 4) cell.appendChild(h('span', 'photo-more', '+' + (p.photos - 4)));
      cell.addEventListener('click', function () { lightbox(urls, idx); }); img.addEventListener('keydown', function (e) { if (e.key === 'Enter') lightbox(urls, idx); });
      photos.appendChild(cell);
    });
    if (p.serviceId && p.serviceName) { var tag = h('a', 'post-tag', '🏷 ' + p.serviceName); tag.href = '/Explore/Detail/' + p.serviceId; extra = extra || h('div', 'post-trip-info'); extra.appendChild(tag); }
    if (!p.mine) {
      var rmenu = h('div', 'post-menu'); var rt = h('button', 'post-menu-btn', '⋯'); rt.type = 'button'; rt.setAttribute('aria-label', 'Tùy chọn bài viết'); var rl = h('div', 'post-menu-list'); rl.hidden = true;
      var rep = h('button', null, 'Báo cáo bài viết'); rep.type = 'button'; rl.appendChild(rep); rmenu.appendChild(rt); rmenu.appendChild(rl); head.appendChild(rmenu);
      rt.addEventListener('click', function () { rl.hidden = !rl.hidden; });
      document.addEventListener('click', function (e) { if (!rmenu.contains(e.target)) rl.hidden = true; });
      rep.addEventListener('click', function () {
        rl.hidden = true;
        var map = { '1': 'Spam', '2': 'QuayRoi', '3': 'LuaDao', '4': 'NoiDungXau', '5': 'Khac' };
        var pick = prompt('Lý do báo cáo:\n1. Spam\n2. Quấy rối\n3. Lừa đảo\n4. Nội dung xấu\n5. Khác\nNhập số 1–5:'); if (!pick || !map[pick.trim()]) return;
        api('/api/mxh/bao-cao', { method: 'POST', body: form({ postId: p.id, reason: map[pick.trim()] }) }).then(function () { toast('Đã gửi báo cáo. Cảm ơn bạn.'); }).catch(function (e) { toast(e.message); });
      });
    }
    if (p.mine) {
      var menu = h('div', 'post-menu'); var t = h('button', 'post-menu-btn', '⋯'); t.type = 'button'; t.setAttribute('aria-label', 'Tùy chọn bài viết'); var ml = h('div', 'post-menu-list'); ml.hidden = true;
      var edit = h('button', null, 'Sửa bài viết'); edit.type = 'button'; var del = h('button', null, 'Xóa bài viết'); del.type = 'button'; ml.appendChild(edit); ml.appendChild(del); menu.appendChild(t); menu.appendChild(ml); head.appendChild(menu);
      t.addEventListener('click', function () { ml.hidden = !ml.hidden; });
      document.addEventListener('click', function (e) { if (!menu.contains(e.target)) ml.hidden = true; });
      del.addEventListener('click', function () { ml.hidden = true; if (!confirm('Xóa bài viết này?')) return; api('/api/mxh/bai-viet/' + p.id + '/xoa', { method: 'POST' }).then(function () { card.remove(); if (opts.onDelete) opts.onDelete(); }).catch(function (e) { toast(e.message); }); });
      edit.addEventListener('click', function () {
        ml.hidden = true;
        var f = h('div', 'post-composer'); var ta = h('textarea', 'form-control'); ta.value = p.content; ta.maxLength = 4000; ta.rows = 3;
        var sel = h('select', 'form-select'); Object.keys(VIS).forEach(function (k) { var o = h('option', null, VIS[k]); o.value = k; if (k === p.visibility) o.selected = true; sel.appendChild(o); });
        var rm = null; if (p.photos) { var lab = h('label'); rm = h('input'); rm.type = 'checkbox'; lab.appendChild(rm); lab.appendChild(document.createTextNode(' Xóa ảnh của bài viết')); f.appendChild(ta); f.appendChild(lab); } else f.appendChild(ta);
        var bar = h('div', 'post-composer-bar'); var save = h('button', 'btn btn-dark', 'Lưu'); save.type = 'button'; var cancel = h('button', 'btn btn-outline', 'Hủy'); cancel.type = 'button';
        var right = h('div', 'soc-card-actions'); right.appendChild(cancel); right.appendChild(save); bar.appendChild(sel); bar.appendChild(right); f.appendChild(bar); body.replaceWith(f); photos.hidden = true;
        cancel.addEventListener('click', function () { f.replaceWith(body); photos.hidden = false; });
        save.addEventListener('click', function () {
          var fd = form({ Content: ta.value, Visibility: sel.value }); if (rm && rm.checked) fd.append('RemovePhotos', 'true'); save.disabled = true;
          api('/api/mxh/bai-viet/' + p.id + '/sua', { method: 'POST', body: fd }).then(function (np) { var fresh = renderPost(np, opts); card.replaceWith(fresh); }).catch(function (e) { save.disabled = false; toast(e.message); });
        });
      });
    }
    card.appendChild(head); if (extra) card.appendChild(extra); if (p.content) card.appendChild(body); if (p.photos) card.appendChild(photos);
    var stats = h('div', 'post-stats');
    var reactBtn = h('button', 'post-react-sum'); reactBtn.type = 'button';
    function renderReactSum() {
      reactBtn.replaceChildren(); var tot = reactionTotal(p.reactions);
      if (!tot) { reactBtn.hidden = true; return; } reactBtn.hidden = false;
      var ic = h('span', 'react-icons'); topEmoji(p.reactions).forEach(function (k) { ic.appendChild(h('span', null, EMOJI[k])); }); reactBtn.appendChild(ic); reactBtn.appendChild(h('span', null, ' ' + tot));
    }
    renderReactSum();
    reactBtn.addEventListener('click', function () {
      var body = h('div', 'fb-reactors'); body.appendChild(h('small', 'soc-meta', 'Đang tải…'));
      api('/api/mxh/cam-xuc/danh-sach?type=Post&target=' + p.id).then(function (rows) { body.replaceChildren(); if (!rows.length) body.appendChild(h('small', 'soc-meta', 'Chưa có ai.')); rows.forEach(function (r) { var row = h('div', 'fb-person'); row.appendChild(h('span', null, EMOJI[r.emoji] || '👍')); row.appendChild(h('span', null, r.name)); body.appendChild(row); }); }).catch(function (e) { body.textContent = e.message; });
      modal('Cảm xúc', body);
    });
    var cmtCount = h('span', null, ''); var shareCount = h('span', null, '');
    function renderCounts() { cmtCount.textContent = p.comments ? p.comments + ' bình luận' : ''; shareCount.textContent = p.shares ? p.shares + ' lượt chia sẻ' : ''; }
    renderCounts();
    var right = h('span', 'post-stats-right'); right.appendChild(cmtCount); right.appendChild(shareCount);
    stats.appendChild(reactBtn); stats.appendChild(right); card.appendChild(stats);
    var actions = h('div', 'post-actions');
    var reactWrap = h('div', 'react-wrap'); var like = h('button', '', ''); like.type = 'button';
    function renderLike() { var k = p.myReaction; like.className = k ? 'on' : ''; like.textContent = k ? (EMOJI[k] + ' ' + EMOJI_NAME[k]) : '👍 Thích'; }
    renderLike();
    var picker = h('div', 'react-picker'); picker.setAttribute('role', 'menu'); picker.hidden = true;
    function react(k) {
      api('/api/mxh/cam-xuc', { method: 'POST', body: form({ type: 'Post', target: p.id, emoji: k }) }).then(function (r) { p.myReaction = r.mine; p.reactions = r.reactions || {}; p.likes = r.count; renderLike(); renderReactSum(); }).catch(function (e) { toast(e.message); });
    }
    EMOJI_ORDER.forEach(function (k) { var b = h('button', 'react-opt', EMOJI[k]); b.type = 'button'; b.title = EMOJI_NAME[k]; b.setAttribute('aria-label', EMOJI_NAME[k]); b.setAttribute('role', 'menuitem'); b.addEventListener('click', function (e) { e.stopPropagation(); picker.hidden = true; react(k); }); picker.appendChild(b); });
    var hideTimer = null;
    function showPicker() { clearTimeout(hideTimer); picker.hidden = false; } function hidePicker() { hideTimer = setTimeout(function () { picker.hidden = true; }, 250); }
    reactWrap.addEventListener('mouseenter', showPicker); reactWrap.addEventListener('mouseleave', hidePicker); picker.addEventListener('mouseenter', showPicker);
    var pressTimer = null; like.addEventListener('touchstart', function () { pressTimer = setTimeout(showPicker, 450); }, { passive: true }); like.addEventListener('touchend', function () { clearTimeout(pressTimer); });
    like.addEventListener('click', function () { if (!picker.hidden && window.matchMedia('(hover:none)').matches) { picker.hidden = true; return; } react(p.myReaction || 'like'); });
    reactWrap.appendChild(like); reactWrap.appendChild(picker);
    var cm = h('button', null, '💬 Bình luận'); cm.type = 'button';
    actions.appendChild(reactWrap); actions.appendChild(cm);
    var canShare = p.visibility === 'Public' && document.querySelector('[data-soc-feed]') && document.querySelector('[data-soc-feed]').dataset.role !== 'provider';
    if (canShare) {
      var sh = h('button', null, '↗ Chia sẻ'); sh.type = 'button'; actions.appendChild(sh);
      sh.addEventListener('click', function () {
        var body = h('div', 'fb-share'); var ta2 = h('textarea', 'form-control'); ta2.rows = 3; ta2.maxLength = 2000; ta2.placeholder = 'Nói gì đó về bài viết này…';
        var sel2 = h('select', 'form-select'); Object.keys(VIS).forEach(function (k) { var o = h('option', null, VIS[k]); o.value = k; if (k === 'Public') o.selected = true; sel2.appendChild(o); });
        var go = h('button', 'btn btn-dark', 'Chia sẻ ngay'); go.type = 'button'; var row = h('div', 'post-composer-bar'); row.appendChild(sel2); row.appendChild(go);
        body.appendChild(ta2); body.appendChild(row); var stop = modal('Chia sẻ bài viết', body); ta2.focus();
        go.addEventListener('click', function () { go.disabled = true; api('/api/mxh/bai-viet/' + p.id + '/chia-se', { method: 'POST', body: form({ content: ta2.value, visibility: sel2.value }) }).then(function (np) { stop(); toast('Đã chia sẻ lên trang của bạn.'); p.shares = (p.shares || 0) + 1; renderCounts(); if (opts.onShared) opts.onShared(np); }).catch(function (e) { go.disabled = false; toast(e.message); }); });
      });
    }
    var sv = h('button', p.saved ? 'on' : '', p.saved ? '🔖 Đã lưu' : '🔖 Lưu'); sv.type = 'button'; actions.appendChild(sv);
    sv.addEventListener('click', function () { api('/api/mxh/bai-viet/' + p.id + '/luu', { method: 'POST', body: form({ saved: !p.saved }) }).then(function (r) { p.saved = r.saved; sv.className = p.saved ? 'on' : ''; sv.textContent = p.saved ? '🔖 Đã lưu' : '🔖 Lưu'; if (!p.saved && opts.onUnsave) { card.remove(); opts.onUnsave(); } }).catch(function (e) { toast(e.message); }); });
    card.appendChild(actions);
    var cbox = commentsBox(p, cmtCount); cbox.hidden = true; card.appendChild(cbox);
    if (embed) card.insertBefore(embed, stats);
    function openComments() { cbox.hidden = false; cbox.load(); }
    cm.addEventListener('click', function () { if (cbox.hidden) openComments(); else cbox.hidden = true; });
    cmtCount.style.cursor = 'pointer'; cmtCount.addEventListener('click', openComments);
    if (opts.openComments) openComments();
    return card;
  }

  // ---------------------------------------------------------------- soan bai
  function composer(onCreated, me, cfg) {
    cfg = cfg || {};
    var box = h('form', 'post-composer panel'); var top = h('div', 'post-composer-top');
    top.appendChild(S.avatar(me || { name: 'Tôi' })); var ta = h('textarea', 'form-control'); ta.placeholder = cfg.provider ? 'Chia sẻ ưu đãi, món mới hay hình ảnh của cơ sở…' : 'Bạn đang nghĩ gì về chuyến đi?'; ta.maxLength = 4000; ta.rows = 1; ta.setAttribute('aria-label', 'Nội dung bài viết'); top.appendChild(ta); box.appendChild(top);
    var previews = h('div', 'post-previews'); box.appendChild(previews);
    var bar = h('div', 'post-composer-bar'); var left = h('div', 'soc-card-actions');
    var lab = h('label', 'post-tool', '🖼'); lab.title = 'Thêm ảnh'; lab.setAttribute('aria-label', 'Thêm ảnh'); var file = h('input'); file.type = 'file'; file.accept = 'image/jpeg,image/png'; file.multiple = true; file.hidden = true; lab.appendChild(file);
    var kindSel = h('select', 'form-select'); kindSel.setAttribute('aria-label', 'Loại bài viết'); (cfg.provider ? [['Branch', 'Giới thiệu cơ sở']] : [['Share', 'Chia sẻ'], ['Companion', 'Tìm bạn đồng hành']]).forEach(function (k) { var o = h('option', null, k[1]); o.value = k[0]; kindSel.appendChild(o); });
    var branchSel = null;
    if (cfg.provider) { branchSel = h('select', 'form-select'); branchSel.setAttribute('aria-label', 'Cơ sở'); (cfg.branches || []).forEach(function (b) { var o = h('option', null, b.name); o.value = b.id; branchSel.appendChild(o); }); if (!(cfg.branches || []).length) { var no = h('option', null, 'Chưa có cơ sở được duyệt'); no.value = ''; branchSel.appendChild(no); } }
    var svcSel = null;
    if (!cfg.provider && (cfg.saved || []).length) { svcSel = h('select', 'form-select'); svcSel.setAttribute('aria-label', 'Gắn dịch vụ đã lưu'); var none = h('option', null, 'Gắn dịch vụ đã lưu (không bắt buộc)'); none.value = ''; svcSel.appendChild(none); cfg.saved.forEach(function (s) { var o = h('option', null, s.name); o.value = s.id; svcSel.appendChild(o); }); }
    var trip = h('div', 'post-trip'); trip.hidden = true;
    var dest = h('input', 'form-control'); dest.type = 'text'; dest.maxLength = 200; dest.placeholder = 'Điểm đến (ví dụ: Đà Nẵng)'; dest.setAttribute('aria-label', 'Điểm đến');
    var d1 = h('input', 'form-control'); d1.type = 'date'; d1.setAttribute('aria-label', 'Ngày đi'); var d2 = h('input', 'form-control'); d2.type = 'date'; d2.setAttribute('aria-label', 'Ngày về');
    trip.appendChild(dest); trip.appendChild(d1); trip.appendChild(d2); box.appendChild(trip); if (svcSel) box.appendChild(svcSel);
    kindSel.addEventListener('change', function () { trip.hidden = kindSel.value !== 'Companion'; ta.placeholder = kindSel.value === 'Companion' ? 'Bạn muốn đi cùng ai, kiểu chuyến đi thế nào?' : 'Bạn đang nghĩ gì về chuyến đi?'; });
    var sel = h('select', 'form-select'); sel.setAttribute('aria-label', 'Ai được xem'); Object.keys(VIS).forEach(function (k) { var o = h('option', null, VIS[k]); o.value = k; if (k === 'Friends') o.selected = true; sel.appendChild(o); });
    // Loai bai: hai nut thay o chon; nguoi xem: o chon nho canh nut Dang.
    var kinds = h('div', 'post-kinds'); if (!cfg.provider) { kindSel.hidden = true; [['Share', 'Chia sẻ'], ['Companion', 'Tìm bạn đồng hành']].forEach(function (k, n) { var b = h('button', n === 0 ? 'on' : '', k[1]); b.type = 'button'; b.addEventListener('click', function () { kindSel.value = k[0]; kindSel.dispatchEvent(new Event('change')); kinds.querySelectorAll('button').forEach(function (x) { x.classList.toggle('on', x === b); }); box.classList.add('open'); }); kinds.appendChild(b); }); }
    sel.classList.add('post-vis'); Array.prototype.forEach.call(sel.options, function (o) { o.textContent = (o.value === 'Public' ? '🌐 ' : o.value === 'Friends' ? '👥 ' : '🔒 ') + o.textContent; });
    left.appendChild(lab); if (!cfg.provider) { left.appendChild(kinds); left.appendChild(kindSel); } if (branchSel && (cfg.branches || []).length !== 1) left.appendChild(branchSel); var right = h('div', 'post-composer-right'); if (!cfg.provider) right.appendChild(sel); var go = h('button', 'btn btn-dark', 'Đăng'); go.type = 'submit'; right.appendChild(go); bar.appendChild(left); bar.appendChild(right); box.appendChild(bar);
    // Gon khi chua soan: chi mot dong; bam vao (hoac chon anh) thi mo rong.
    if (!cfg.provider) { box.classList.add('post-composer-compact'); ['focus', 'click', 'input'].forEach(function (ev) { ta.addEventListener(ev, function () { box.classList.add('open'); }); }); file.addEventListener('change', function () { box.classList.add('open'); }); }
    var files = [];
    file.addEventListener('change', function () {
      Array.prototype.forEach.call(file.files, function (f) { if (files.length < 4 && f.size <= 5 * 1024 * 1024) files.push(f); else if (f.size > 5 * 1024 * 1024) toast('Ảnh tối đa 5 MB.'); });
      file.value = ''; previews.replaceChildren();
      files.forEach(function (f, i) { var wrap = h('span'); var img = h('img'); img.src = URL.createObjectURL(f); img.alt = 'Ảnh xem trước'; wrap.appendChild(img); previews.appendChild(wrap); img.title = 'Bấm để bỏ ảnh'; img.style.cursor = 'pointer'; img.addEventListener('click', function () { files.splice(i, 1); file.dispatchEvent(new Event('change')); }); });
    });
    box.addEventListener('submit', function (e) {
      e.preventDefault(); var text = ta.value.trim(); var comp = kindSel.value === 'Companion'; if (!text && !files.length && !comp) { toast('Hãy viết nội dung hoặc chọn ảnh.'); return; }
      if (comp && (!dest.value.trim() || !d1.value || !d2.value)) { toast('Nhập điểm đến, ngày đi và ngày về.'); return; }
      if (cfg.provider && (!branchSel || !branchSel.value)) { toast('Chọn cơ sở để đăng tin.'); return; }
      var fd = form({ Content: text, Visibility: sel.value, Kind: kindSel.value }); if (cfg.provider) fd.append('BranchId', branchSel.value); if (svcSel && svcSel.value) fd.append('ServiceId', svcSel.value); if (comp) { fd.append('Destination', dest.value.trim()); fd.append('TripStart', d1.value); fd.append('TripEnd', d2.value); } files.forEach(function (f) { fd.append('Photos', f); }); go.disabled = true;
      api('/api/mxh/bai-viet', { method: 'POST', body: fd }).then(function (p) { ta.value = ''; dest.value = ''; files = []; previews.replaceChildren(); onCreated(p); }).catch(function (err) { toast(err.message); }).then(function () { go.disabled = false; });
    });
    return box;
  }

  // ---------------------------------------------------------------- danh sach co phan trang
  function list(root, urlFor, opts) {
    opts = opts || {};
    var banner = h('button', 'btn btn-dark post-new-banner'); banner.type = 'button'; banner.hidden = true;
    var items = h('div'), more = h('button', 'btn btn-outline post-more', 'Xem thêm bài viết'); more.type = 'button'; more.hidden = true; var empty = h('p', 'soc-empty', ''); empty.hidden = true;
    var sentinel = h('div', 'post-sentinel'); sentinel.setAttribute('aria-hidden', 'true');
    root.appendChild(banner); root.appendChild(items); root.appendChild(empty); root.appendChild(more); root.appendChild(sentinel);
    var last = null, newest = 0, loading = false, done = false, timer = null, alive = true;
    function checkEmpty() { empty.hidden = items.children.length > 0; empty.textContent = opts.emptyText || 'Chưa có bài viết nào.'; }
    function card(p) { return renderPost(p, { onDelete: checkEmpty, onUnsave: opts.unsaveRemoves ? checkEmpty : null, onShared: opts.onShared }); }
    function load(reset) {
      if (loading) return; loading = true;
      if (reset) { items.replaceChildren(); last = null; done = false; newest = 0; banner.hidden = true; }
      api(urlFor(last)).then(function (page) {
        page.forEach(function (p) { items.appendChild(card(p)); last = p.id; if (p.id > newest) newest = p.id; });
        done = page.length < 10; more.hidden = done; checkEmpty();
      }).catch(function (e) { toast(e.message); }).then(function () { loading = false; });
    }
    more.addEventListener('click', function () { load(false); });
    if ('IntersectionObserver' in window) {
      var io = new IntersectionObserver(function (es) { if (es[0].isIntersecting && !done && !loading && last) load(false); }, { rootMargin: '400px' });
      io.observe(sentinel);
    }
    banner.addEventListener('click', function () { load(true); window.scrollTo({ top: 0, behavior: 'smooth' }); });
    function poll() {
      if (!alive || !opts.pollUrl) return;
      if (!root.isConnected) { alive = false; return; }
      if (document.hidden || !newest) { timer = setTimeout(poll, 45000); return; }
      api(opts.pollUrl(newest)).then(function (r) { if (r && r.count > 0) { banner.textContent = 'Có ' + r.count + ' bài viết mới — bấm để xem'; banner.hidden = false; } }).catch(function () { }).then(function () { timer = setTimeout(poll, 45000); });
    }
    if (opts.pollUrl) timer = setTimeout(poll, 45000);
    load(true);
    return { prepend: function (p) { items.insertBefore(card(p), items.firstChild); if (p.id > newest) newest = p.id; checkEmpty(); }, reload: function () { load(true); }, stop: function () { alive = false; clearTimeout(timer); } };
  }

  // ---------------------------------------------------------------- trang Bang tin
  var feed = document.querySelector('[data-soc-feed]');
  if (feed) {
    var isProv = feed.dataset.role === 'provider';
    var branches0 = JSON.parse(feed.dataset.branches || '[]');
    var scope = isProv ? (branches0.length ? 'branch' : 'public') : 'home', area = h('div'), tabs = h('div', 'soc-tabs');
    // Khach: mot luong bai, loc bang nut. Nha cung cap: tin cua co so / cong dong nhu cu.
    var filters = isProv ? (branches0.length ? [['branch', 'Tin của ' + branches0[0].name], ['public', 'Cộng đồng']] : [['public', 'Cộng đồng']]) : [['all', 'Tất cả'], ['home', 'Bạn bè'], ['companion', 'Tìm bạn đồng hành']];
    var btns = {}; filters.forEach(function (f) { var bt = h('button', null, f[1]); bt.type = 'button'; bt.setAttribute('aria-pressed', 'false'); bt.addEventListener('click', function () { open(f[0]); }); btns[f[0]] = bt; tabs.appendChild(bt); });
    tabs.setAttribute('role', 'group'); tabs.setAttribute('aria-label', 'Lọc bài viết');
    var savedHead = h('div', 'soc-saved-head'); savedHead.hidden = true; savedHead.appendChild(h('strong', null, 'Bài đã lưu')); var back = h('a', null, '← Về bảng tin'); back.href = '/bang-tin'; savedHead.appendChild(back);
    var searchBox = h('input', 'form-control'); searchBox.type = 'search'; searchBox.placeholder = 'Lọc theo điểm đến, ví dụ: Đà Lạt'; searchBox.maxLength = 100; searchBox.hidden = true; searchBox.setAttribute('aria-label', 'Lọc theo điểm đến'); var searchTimer = null;
    var handle;
    var comp = composer(function (p) { if (handle) handle.prepend(p); }, { name: feed.dataset.name || 'Tôi', avatar: feed.dataset.avatar || null }, { provider: isProv, branches: JSON.parse(feed.dataset.branches || '[]'), saved: JSON.parse(feed.dataset.saved || '[]') });
    feed.appendChild(comp); feed.appendChild(savedHead); feed.appendChild(tabs); feed.appendChild(searchBox); feed.appendChild(area);
    function open(s) { scope = s; Object.keys(btns).forEach(function (k) { btns[k].classList.toggle('active', k === s); btns[k].setAttribute('aria-pressed', String(k === s)); }); var saved = s === 'saved'; tabs.hidden = saved; savedHead.hidden = !saved; comp.hidden = saved; searchBox.hidden = s !== 'companion'; area.replaceChildren(); if (handle && handle.stop) handle.stop(); handle = list(area, function (before) { return '/api/mxh/bai-viet?scope=' + scope + (scope === 'branch' ? '&branchId=' + branches0[0].id : '') + (scope === 'companion' && searchBox.value.trim() ? '&q=' + encodeURIComponent(searchBox.value.trim()) : '') + (before ? '&before=' + before : ''); }, {
      emptyText: s === 'home' ? 'Chưa có bài của bạn bè. Kết bạn hoặc theo dõi cơ sở để thấy thêm bài viết.' : s === 'saved' ? 'Bạn chưa lưu bài viết nào. Bấm 🔖 Lưu dưới một bài để xem lại ở đây.' : s === 'companion' ? 'Chưa có bài tìm bạn đồng hành phù hợp.' : 'Chưa có bài viết nào.',
      unsaveRemoves: s === 'saved',
      onShared: function (np) { if ((scope === 'home' || scope === 'all') && handle) handle.prepend(np); },
      pollUrl: s === 'saved' ? null : function (since) { return '/api/mxh/bai-viet-moi?scope=' + scope + '&since=' + since + (scope === 'branch' ? '&branchId=' + branches0[0].id : ''); }
    }); }
    searchBox.addEventListener('input', function () { clearTimeout(searchTimer); searchTimer = setTimeout(function () { open('companion'); }, 350); });
    var q0 = location.search;
    open(isProv ? (branches0.length ? 'branch' : 'public') : /[?&]tab=dong-hanh(&|$)/.test(q0) ? 'companion' : /[?&]tab=da-luu(&|$)/.test(q0) ? 'saved' : 'all');
  }

  // ---------------------------------------------------------------- cot phai kieu Facebook
  var contacts = document.querySelector('[data-soc-contacts]');
  if (contacts) {
    api('/api/mxh/ban-be').then(function (rows) {
      contacts.replaceChildren(); if (!rows.length) { contacts.appendChild(h('small', 'soc-meta', 'Chưa có bạn bè. Kết bạn để nhắn tin nhanh.')); return; }
      rows.slice(0, 8).forEach(function (p) {
        var row = h('div', 'fb-contact'); row.appendChild(S.avatar({ name: p.name, avatar: p.avatar }, 'small'));
        var a = h('a', null, p.name); a.href = '/nguoi/' + p.id; row.appendChild(a);
        var m = h('a', 'btn btn-outline', 'Nhắn'); m.href = '/tin-nhan/nguoi/' + p.id; row.appendChild(m); contacts.appendChild(row);
      });
    }).catch(function () { contacts.replaceChildren(); });
  }
  var sug = document.querySelector('[data-soc-suggest]');
  if (sug) {
    api('/api/mxh/goi-y').then(function (list) {
      sug.replaceChildren(); if (!list.length) { sug.appendChild(h('small', 'soc-meta', 'Chưa có gợi ý.')); return; }
      list.slice(0, 5).forEach(function (p) {
        var row = h('div', 'fb-person'); row.appendChild(S.avatar({ name: p.name, avatar: p.avatar }, 'small'));
        var a = h('a', null, p.name); a.href = '/nguoi/' + p.id; row.appendChild(a);
        var add = h('button', 'btn btn-outline', 'Kết bạn'); add.type = 'button';
        add.addEventListener('click', function () { add.disabled = true; api('/api/mxh/ban-be/gui', { method: 'POST', body: form({ accountId: p.id }) }).then(function () { add.textContent = 'Đã gửi'; }).catch(function (e) { add.disabled = false; toast(e.message); }); });
        row.appendChild(add); sug.appendChild(row);
      });
    }).catch(function () { sug.replaceChildren(); });
    var reqBox = document.querySelector('[data-soc-requests]');
    api('/api/mxh/loi-moi').then(function (r) {
      var inc = (r && r.incoming) || []; if (!inc.length || !reqBox) return; reqBox.hidden = false; var host = reqBox.querySelector('[data-soc-requests-list]');
      inc.slice(0, 3).forEach(function (q) {
        var row = h('div', 'fb-person'); var a = h('a', null, q.name || q.requesterName || 'Người dùng'); a.href = '/nguoi/' + q.id; row.appendChild(a);
        var ok = h('button', 'btn btn-dark', 'Chấp nhận'); ok.type = 'button'; ok.addEventListener('click', function () { api('/api/mxh/ban-be/' + (q.requestId || q.id) + '/chap-nhan', { method: 'POST' }).then(function () { row.remove(); }).catch(function (e) { toast(e.message); }); });
        row.appendChild(ok); host.appendChild(row);
      });
    }).catch(function () { });
  }

  // ---------------------------------------------------------------- ho so
  var prof = document.querySelector('[data-soc-posts]');
  if (prof) {
    var author = Number(prof.dataset.author), self = !!document.querySelector('[data-soc-profile][data-self="1"]');
    var area2 = h('div'), handle2;
    if (self) prof.appendChild(composer(function (p) { if (handle2) handle2.prepend(p); }, { name: document.querySelector('h1').textContent }));
    prof.appendChild(area2); prof.insertBefore(h('h2', 'soc-h2', 'Bài viết'), area2);
    handle2 = list(area2, function (before) { return '/api/mxh/nguoi/' + author + '/bai-viet' + (before ? '?before=' + before : ''); }, { emptyText: 'Chưa có bài viết nào bạn xem được.' });
  }

  // ---------------------------------------------------------------- tab trang ca nhan
  var tabsNav = document.querySelector('.fb-profile-tabs');
  if (tabsNav) {
    var pid = Number(document.querySelector('[data-soc-profile]').dataset.id), loadedTab = {};
    function fillFriends(host) {
      api('/api/mxh/nguoi/' + pid + '/ban-be').then(function (rows) {
        host.replaceChildren(); if (!rows.length) { host.appendChild(h('p', 'soc-empty', 'Chưa có bạn bè nào hiển thị.')); return; }
        var grid = h('div', 'fb-friend-grid');
        rows.forEach(function (p) { var a = h('a', 'fb-friend-card'); a.href = '/nguoi/' + p.id; a.appendChild(S.avatar({ name: p.name, avatar: p.avatar })); a.appendChild(h('strong', null, p.name)); grid.appendChild(a); });
        host.appendChild(grid);
      }).catch(function (e) { host.replaceChildren(); host.appendChild(h('p', 'soc-empty', e.message)); });
    }
    function fillPhotos(host) {
      api('/api/mxh/nguoi/' + pid + '/anh').then(function (urls) {
        host.replaceChildren(); if (!urls.length) { host.appendChild(h('p', 'soc-empty', 'Chưa có ảnh nào bạn xem được.')); return; }
        urls.forEach(function (u, i) { var cell = h('button', 'fb-photo-cell'); cell.type = 'button'; var img = h('img'); img.src = u; img.alt = 'Ảnh ' + (i + 1); img.loading = 'lazy'; cell.appendChild(img); cell.addEventListener('click', function () { lightbox(urls, i); }); host.appendChild(cell); });
      }).catch(function (e) { host.replaceChildren(); host.appendChild(h('p', 'soc-empty', e.message)); });
    }
    tabsNav.addEventListener('click', function (e) {
      var b = e.target.closest('button[data-tab]'); if (!b) return; var t = b.dataset.tab;
      tabsNav.querySelectorAll('button').forEach(function (x) { x.classList.toggle('active', x === b); });
      document.querySelectorAll('[data-tabpane]').forEach(function (p) { p.hidden = p.dataset.tabpane !== t; });
      if (!loadedTab[t]) { loadedTab[t] = true; if (t === 'friends') fillFriends(document.querySelector('[data-profile-friends]')); if (t === 'photos') fillPhotos(document.querySelector('[data-profile-photos]')); }
    });
  }

  // ---------------------------------------------------------------- mot bai viet
  var single = document.querySelector('[data-soc-single]');
  if (single) {
    api('/api/mxh/bai-viet/' + single.dataset.post).then(function (p) { single.appendChild(renderPost(p, { openComments: true, onDelete: function () { location.href = '/bang-tin'; } })); }).catch(function (e) { single.appendChild(h('p', 'soc-empty', e.message)); });
  }
})();
