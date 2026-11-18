// Tin nhan gon: khung cao het man hinh, vung tin nhan luon du cho; nut phu (xem co so, ghi chu, chan, bao cao) gom vao menu "⋯";
// ghi chu noi bo an mac dinh; bo dai thong tin co so trung lap o phia nha cung cap; cot danh sach gon hon.
const fs = require('fs'), path = require('path'); const root = path.join(__dirname, '../../src/HoTroDuLich.Web');
const rd = f => fs.readFileSync(path.join(root, f), 'utf8').split('\r\n').join('\n'), wr = (f, s) => fs.writeFileSync(path.join(root, f), s);
const swap = (s, a, b) => { if (!s.includes(a)) { if (s.includes(b)) return s; throw Error('khong thay: ' + a.slice(0, 90)); } return s.replace(a, () => b); };

let v = rd('Views/Chat/Inbox.cshtml');
v = swap(v, ` @if(!embed){<div class="page-head"><div><span class="eyebrow">TIN NHẮN</span><h1>Tin nhắn</h1><p>@(branch?"Khách hỏi và đặt dịch vụ ngay tại đây. Trả lời nhanh để khách quyết định sớm.":"Trò chuyện với cơ sở và những người cùng đi. Hỏi, đặt dịch vụ và thỏa thuận ngay tại đây.")</p></div></div>}`,
  ` @if(!embed){<h1 class="sr-only">Tin nhắn</h1>}`);
v = swap(v, `   <div class="chat-toolbar">@if(branch){<a class="btn btn-outline" href="/nha-cung-cap/tin-nhan/cai-dat">Cài đặt &amp; tin mẫu</a>}else{`,
  `   @if(!branch){<div class="chat-toolbar">`);
v = swap(v, `<button type="button" class="btn btn-outline" data-chat-open-create>@Ui.Icon("users") Tạo nhóm</button>}</div>`,
  `<button type="button" class="btn btn-outline" data-chat-open-create>@Ui.Icon("users") Tạo nhóm</button></div>}`);
v = swap(v, `     <input class="form-control" type="search" placeholder="Tìm khách hoặc nội dung…" data-chat-search aria-label="Tìm trong tin nhắn"/></div>`,
  `     <div class="chat-search-row"><input class="form-control" type="search" placeholder="Tìm khách hoặc nội dung…" data-chat-search aria-label="Tìm trong tin nhắn"/><a class="chat-gear" href="/nha-cung-cap/tin-nhan/cai-dat" title="Cài đặt &amp; tin mẫu" aria-label="Cài đặt và tin mẫu">⚙</a></div></div>`);
v = swap(v, `<button type="button" class="active" data-chat-filter="">Tất cả</button><button type="button" data-chat-filter="unread">Chưa đọc</button><button type="button" data-chat-filter="open">Đang mở</button><button type="button" data-chat-filter="booking">Chờ xác nhận</button><button type="button" data-chat-filter="done">Đã xử lý</button>`,
  `<button type="button" class="active" data-chat-filter="">Tất cả</button><button type="button" data-chat-filter="unread">Chưa đọc</button><button type="button" data-chat-filter="booking">Chờ xác nhận</button><button type="button" data-chat-filter="done">Đã xử lý</button>`);
v = swap(v, `     <div class="chat-head-actions"><a class="text-link" data-chat-branch-link href="#" hidden>Xem cơ sở</a><button type="button" class="btn btn-outline" data-chat-state hidden></button><button type="button" class="text-link" data-chat-block hidden></button><button type="button" class="text-link" data-chat-report hidden>Báo cáo</button><button type="button" class="btn btn-outline" data-chat-members-toggle hidden>@Ui.Icon("users") Thành viên</button></div></header>`,
  `     <div class="chat-head-actions"><button type="button" class="btn btn-outline" data-chat-state hidden></button><button type="button" class="btn btn-outline" data-chat-members-toggle hidden>@Ui.Icon("users") Thành viên</button>
      <details class="chat-more" data-header-menu><summary aria-label="Thêm thao tác">⋯</summary><div class="chat-more-menu"><a data-chat-branch-link href="#" hidden>Xem cơ sở</a>@if(branch){<button type="button" data-chat-note-toggle>Ghi chú nội bộ &amp; nhãn</button>}<button type="button" data-chat-block hidden></button><button type="button" data-chat-report hidden>Báo cáo</button></div></details></div></header>`);
wr('Views/Chat/Inbox.cshtml', v);

let j = rd('wwwroot/js/chat.js');
j = swap(j, `el.stateBtn.textContent = t.item.state === 'Done' ? 'Mở lại' : 'Đánh dấu đã xử lý';`, `el.stateBtn.textContent = t.item.state === 'Done' ? 'Mở lại' : '✓ Đã xử lý'; el.stateBtn.title = t.item.state === 'Done' ? 'Mở lại cuộc trò chuyện' : 'Đánh dấu đã xử lý';`);
j = swap(j, `    if (el.branchSel) el.branchSel.addEventListener('change',`, `    var noteToggle = q('[data-chat-note-toggle]');
    if (noteToggle && el.noteBox) noteToggle.addEventListener('click', function () { el.noteBox.classList.toggle('open'); var d = noteToggle.closest('details'); if (d) d.open = false; if (el.noteBox.classList.contains('open') && el.note) el.note.focus(); });
    app.querySelectorAll('.chat-more-menu > *').forEach(function (b) { b.addEventListener('click', function () { var d = b.closest('details'); if (d) d.open = false; }); });
    if (el.branchSel) el.branchSel.addEventListener('change',`);
wr('wwwroot/js/chat.js', j);

let css = rd('wwwroot/css/social.css');
if (!css.includes('/* Tin nhan gon */')) css += `
/* Tin nhan gon */
.chat-page{padding-top:14px!important;padding-bottom:14px!important}
.chat-page .chat-app{height:calc(100dvh - 120px);min-height:460px;max-height:none;grid-template-columns:300px minmax(0,1fr);gap:14px}
.chat-app>.panel{padding:0}
.chat-pane{min-height:0}
.chat-pane>*{flex-shrink:0}
.chat-pane>.chat-messages{flex:1 1 auto;min-height:180px}
.chat-head{padding:10px 14px;gap:10px}
.chat-head-main{min-width:0;flex:1}
.chat-head-main strong,.chat-head-main small{display:block;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.chat-head-actions{gap:6px;flex-shrink:0}
.chat-head-actions .btn{min-height:36px;padding:6px 12px;font-size:13px}
.chat-more{position:relative}
.chat-more>summary{list-style:none;cursor:pointer;width:36px;height:36px;border-radius:10px;border:1px solid var(--line);display:grid;place-items:center;font-size:20px;line-height:1;color:var(--ink);background:#fff}
.chat-more>summary::-webkit-details-marker{display:none}
.chat-more[open]>summary{background:var(--teal-light)}
.chat-more-menu{position:absolute;right:0;top:calc(100% + 6px);z-index:30;background:#fff;border:1px solid var(--line);border-radius:12px;box-shadow:0 12px 30px #15213e22;min-width:210px;padding:6px;display:grid}
.chat-more-menu>a,.chat-more-menu>button{display:block;text-align:left;background:none;border:0;cursor:pointer;padding:9px 12px;border-radius:8px;font-size:14px;color:var(--ink);text-decoration:none}
.chat-more-menu>a:hover,.chat-more-menu>button:hover{background:var(--teal-light)}
.chat-more-menu>[hidden]{display:none}
.chat-more-menu>[data-chat-block],.chat-more-menu>[data-chat-report]{color:#b3362f}
[data-me="Branch"] .chat-context{display:none!important}
.chat-context{padding:6px 14px}.chat-context img{width:40px;height:40px}
.chat-note:not(.open){display:none!important}
.chat-note{max-height:35%;overflow:auto}
.chat-msg{max-width:min(78%,620px);min-width:0}
.chat-bubble{overflow-wrap:anywhere;word-break:normal}
.chat-bubble img,.chat-msg img{max-width:100%;height:auto}
.chat-filters{padding:10px;gap:8px}
.chat-tabs{flex-wrap:nowrap;overflow-x:auto;scrollbar-width:none}
.chat-tabs::-webkit-scrollbar{display:none}
.chat-tabs button{white-space:nowrap;flex-shrink:0}
.chat-search-row{display:flex;gap:6px;align-items:center}
.chat-search-row .form-control{flex:1;min-width:0}
.chat-gear{width:42px;height:42px;flex-shrink:0;display:grid;place-items:center;border:1px solid var(--line);border-radius:10px;color:var(--ink);text-decoration:none;font-size:18px}
.chat-gear:hover{background:var(--teal-light)}
.chat-toolbar{padding:10px;gap:6px}
.chat-toolbar .btn{min-height:38px;padding:6px 12px;font-size:13px}
.chat-composer{padding:8px 12px}
.chat-composer textarea{max-height:140px}
@media(max-width:900px){.chat-page .chat-app{grid-template-columns:1fr;height:calc(100dvh - 100px)}}
`;
wr('wwwroot/css/social.css', css);
console.log('ok');
