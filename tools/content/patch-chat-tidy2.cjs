// Tin nhan gon (dot 2): sua o soan tin bi bop hep / qua cao, nut "⋯" lech, khung noi hut chieu cao;
// gom "Tep PDF" va "Vi tri" vao nut "+" de o soan tin rong hon; o soan tin tu cao theo noi dung.
const fs = require('fs'), path = require('path'); const root = path.join(__dirname, '../../src/HoTroDuLich.Web');
const rd = f => fs.readFileSync(path.join(root, f), 'utf8').split('\r\n').join('\n'), wr = (f, s) => fs.writeFileSync(path.join(root, f), s);
const swap = (s, a, b) => { if (!s.includes(a)) { if (s.includes(b)) return s; throw Error('khong thay: ' + a.slice(0, 90)); } return s.replace(a, () => b); };

let v = rd('Views/Chat/Inbox.cshtml');
v = swap(v, `     <label class="chat-attach" title="Gửi tệp PDF">📄<span class="sr-only">Gửi tệp PDF</span><input type="file" accept="application/pdf" data-chat-pdf hidden/></label>
     <button type="button" class="chat-attach" title="Gửi vị trí của tôi" data-chat-location>📍<span class="sr-only">Gửi vị trí</span></button>
     <button type="button" class="chat-attach" title="Biểu tượng cảm xúc" data-chat-emoji>😊<span class="sr-only">Biểu tượng cảm xúc</span></button>`,
  `     <button type="button" class="chat-attach" title="Biểu tượng cảm xúc" data-chat-emoji>😊<span class="sr-only">Biểu tượng cảm xúc</span></button>
     <details class="chat-plus"><summary class="chat-attach" title="Gửi thêm">＋<span class="sr-only">Gửi thêm tệp hoặc vị trí</span></summary><div class="chat-plus-menu"><label>📄 Tệp PDF<input type="file" accept="application/pdf" data-chat-pdf hidden/></label><button type="button" data-chat-location>📍 Vị trí của tôi</button></div></details>`);
wr('Views/Chat/Inbox.cshtml', v);

let j = rd('wwwroot/js/chat.js');
j = swap(j, `    var noteToggle = q('[data-chat-note-toggle]');`, `    // O soan tin tu cao theo noi dung (toi da 140px); menu "+" dong lai sau khi chon.
    app.querySelectorAll('.chat-plus-menu > *').forEach(function (b) { b.addEventListener('click', function () { var d = b.closest('details'); if (d) setTimeout(function () { d.open = false; }, 0); }); });
    var noteToggle = q('[data-chat-note-toggle]');`);
wr('wwwroot/js/chat.js', j);

let css = rd('wwwroot/css/social.css');
if (!css.includes('/* Tin nhan gon dot 2 */')) css += `
/* Tin nhan gon dot 2 */
.is-embed .chat-page{padding:0!important}
.is-embed .chat-page .chat-app{height:100vh;min-height:0}
.chat-more,.chat-plus{margin:0!important;padding:0!important;border:0!important}
.chat-head-actions{align-items:center}
.chat-composer{align-items:flex-end;gap:4px}
.chat-composer .chat-attach{width:38px;height:38px;padding:0;display:inline-flex;align-items:center;justify-content:center;border-radius:10px;flex-shrink:0;font-size:18px;color:var(--ink,#15213e)}
.chat-composer .chat-attach:hover{background:var(--teal-light,#edf1ff)}
.chat-composer textarea.form-control{flex:1 1 auto;min-width:0;min-height:40px;height:40px;max-height:140px;resize:none;padding:9px 12px;line-height:1.4;border-radius:12px}
.chat-composer [data-chat-send]{flex-shrink:0;min-height:40px}
.chat-plus{position:relative;flex-shrink:0}
.chat-plus>summary{list-style:none;cursor:pointer}
.chat-plus>summary::-webkit-details-marker{display:none}
.chat-plus-menu{position:absolute;left:0;bottom:calc(100% + 6px);z-index:30;background:#fff;border:1px solid var(--line,#e1e6f0);border-radius:12px;box-shadow:0 12px 30px #15213e22;min-width:190px;padding:6px;display:grid}
.chat-plus-menu>label,.chat-plus-menu>button{display:block;text-align:left;background:none;border:0;cursor:pointer;padding:9px 12px;border-radius:8px;font-size:14px;color:var(--ink,#15213e);margin:0}
.chat-plus-menu>label:hover,.chat-plus-menu>button:hover{background:var(--teal-light,#edf1ff)}
@media(max-width:420px){.chat-composer [data-chat-send] span{display:none}}
`;
wr('wwwroot/css/social.css', css);
console.log('ok');
