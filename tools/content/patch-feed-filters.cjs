// Bang tin (phuong an B): mot luong bai, loc bang nut Tat ca / Ban be / Tim ban dong hanh; "Bai da luu" chuyen sang menu trai;
// o dang bai gon: mot dong, bam vao mo rong; loai bai la hai nut, nguoi xem la nut nho canh "Dang", them anh la bieu tuong.
const fs = require('fs'), path = require('path'); const root = path.join(__dirname, '../../src/HoTroDuLich.Web');
const rd = f => fs.readFileSync(path.join(root, f), 'utf8').split('\r\n').join('\n'), wr = (f, s) => fs.writeFileSync(path.join(root, f), s);
const swap = (s, a, b) => { if (!s.includes(a)) { if (s.includes(b)) return s; throw Error('khong thay: ' + a.slice(0, 90)); } return s.replace(a, () => b); };

// ---- Pham vi "all": moi bai minh duoc xem (ban be, co so dang theo doi, bai cong khai)
let ps = rd('Services/PostService.cs');
ps = swap(ps, `   case "home":\n   {`, `   case "all":\n    return (q,false); // moi bai minh duoc xem: Visible() da gioi han theo quyen\n   case "home":\n   {`);
wr('Services/PostService.cs', ps);
let sc = rd('Controllers/SocialControllers.cs');
sc = swap(sc, `posts.Feed(scope is "public" or "branch" or "companion" or "home" or "saved" or "friends"?scope:"home"`, `posts.Feed(scope is "all" or "public" or "branch" or "companion" or "home" or "saved" or "friends"?scope:"all"`);
sc = swap(sc, `posts.NewCount(scope is "home" or "public" or "branch" or "companion" or "saved"?scope:"home"`, `posts.NewCount(scope is "all" or "home" or "public" or "branch" or "companion" or "saved"?scope:"all"`);
wr('Controllers/SocialControllers.cs', sc);

// ---- Menu trai: Bai da luu
let fv = rd('Views/Social/Feed.cshtml');
fv = swap(fv, `     <a href="/Saved">@Ui.Icon("check") Dịch vụ đã lưu</a>`, `     <a href="/bang-tin?tab=da-luu">@Ui.Icon("check") Bài đã lưu</a>\n     <a href="/Saved">@Ui.Icon("check") Dịch vụ đã lưu</a>`);
wr('Views/Social/Feed.cshtml', fv);

// ---- Script
let j = rd('wwwroot/js/posts.js');
// O dang bai gon
j = swap(j, `ta.maxLength = 4000; ta.rows = 2;`, `ta.maxLength = 4000; ta.rows = 1;`);
j = swap(j, `    var lab = h('label', 'btn btn-outline', 'Thêm ảnh');`, `    var lab = h('label', 'post-tool', '🖼'); lab.title = 'Thêm ảnh'; lab.setAttribute('aria-label', 'Thêm ảnh');`);
j = swap(j, `    left.appendChild(lab); if (!cfg.provider) left.appendChild(kindSel);`, `    // Loai bai: hai nut thay o chon; nguoi xem: o chon nho canh nut Dang.
    var kinds = h('div', 'post-kinds'); if (!cfg.provider) { kindSel.hidden = true; [['Share', 'Chia sẻ'], ['Companion', 'Tìm bạn đồng hành']].forEach(function (k, n) { var b = h('button', n === 0 ? 'on' : '', k[1]); b.type = 'button'; b.addEventListener('click', function () { kindSel.value = k[0]; kindSel.dispatchEvent(new Event('change')); kinds.querySelectorAll('button').forEach(function (x) { x.classList.toggle('on', x === b); }); box.classList.add('open'); }); kinds.appendChild(b); }); }
    sel.classList.add('post-vis'); Array.prototype.forEach.call(sel.options, function (o) { o.textContent = (o.value === 'Public' ? '🌐 ' : o.value === 'Friends' ? '👥 ' : '🔒 ') + o.textContent; });
    left.appendChild(lab); if (!cfg.provider) { left.appendChild(kinds); left.appendChild(kindSel); }`);
j = swap(j, `if (!cfg.provider) left.appendChild(sel); var go = h('button', 'btn btn-dark', 'Đăng'); go.type = 'submit'; bar.appendChild(left); bar.appendChild(go); box.appendChild(bar);`,
  `var right = h('div', 'post-composer-right'); if (!cfg.provider) right.appendChild(sel); var go = h('button', 'btn btn-dark', 'Đăng'); go.type = 'submit'; right.appendChild(go); bar.appendChild(left); bar.appendChild(right); box.appendChild(bar);
    // Gon khi chua soan: chi mot dong; bam vao (hoac chon anh) thi mo rong.
    if (!cfg.provider) { box.classList.add('post-composer-compact'); ['focus', 'click', 'input'].forEach(function (ev) { ta.addEventListener(ev, function () { box.classList.add('open'); }); }); file.addEventListener('change', function () { box.classList.add('open'); }); }`);
// Luong bai: nut loc
const a = j.indexOf(`    var tFriends = h('button', 'active', 'Dành cho bạn')`), b = j.indexOf(`    open(isProv ? (branches0.length ? 'branch' : 'public')`);
if (a < 0 || b < 0) throw Error('khong thay khoi tab');
const bEnd = j.indexOf('\n', b);
j = j.slice(0, a) + String.raw`    // Khach: mot luong bai, loc bang nut. Nha cung cap: tin cua co so / cong dong nhu cu.
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
    open(isProv ? (branches0.length ? 'branch' : 'public') : /[?&]tab=dong-hanh(&|$)/.test(q0) ? 'companion' : /[?&]tab=da-luu(&|$)/.test(q0) ? 'saved' : 'all');` + j.slice(bEnd);
wr('wwwroot/js/posts.js', j);

let css = rd('wwwroot/css/social.css');
if (!css.includes('/* Bang tin loc gon */')) css += `
/* Bang tin loc gon */
.soc-tabs{gap:6px}
.soc-tabs button{min-height:36px;padding:6px 14px;font-size:14px}
.soc-tabs button.active{background:#15213e;border-color:#15213e;color:#fff}
.soc-saved-head{display:flex;justify-content:space-between;align-items:center;gap:12px;margin:4px 0 12px}
.soc-saved-head strong{font-size:1.15rem}
.soc-saved-head a{color:var(--teal);font-weight:600;text-decoration:none}
.post-composer-compact .post-composer-top textarea{min-height:44px!important;height:44px;resize:none;border-radius:22px;padding:10px 16px;overflow:hidden}
.post-composer-compact.open .post-composer-top textarea{min-height:88px!important;height:auto;border-radius:12px;resize:vertical;overflow:auto}
.post-composer-compact:not(.open) .post-composer-bar,.post-composer-compact:not(.open) .post-trip,.post-composer-compact:not(.open)>select{display:none!important}
.post-composer-bar{display:flex;justify-content:space-between;align-items:center;gap:8px;flex-wrap:wrap}
.post-composer-bar .soc-card-actions{display:flex;align-items:center;gap:8px;flex-wrap:wrap}
.post-composer-right{display:flex;align-items:center;gap:8px;margin-left:auto}
.post-tool{width:40px;height:40px;display:grid;place-items:center;border-radius:12px;border:1px solid var(--line);cursor:pointer;font-size:18px;margin:0;background:#fff}
.post-tool:hover{background:var(--teal-light)}
.post-kinds{display:flex;gap:4px;background:var(--canvas,#f5f7fc);border-radius:999px;padding:3px}
.post-kinds button{border:0;background:none;border-radius:999px;padding:6px 12px;font-size:13px;font-weight:600;color:var(--muted);cursor:pointer}
.post-kinds button.on{background:#fff;color:var(--ink);box-shadow:0 1px 4px #15213e22}
.post-vis{width:auto!important;min-height:40px;padding:6px 30px 6px 12px;font-size:13px;border-radius:12px}
`;
wr('wwwroot/css/social.css', css);
console.log('ok');
