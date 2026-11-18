// Ra soat 2: o dang tin cua nha cung cap khi co so dang chon chua duoc duyet -> loi nhan ro rang thay o chon rong;
// bai viet cua co so khong con cong khai / tai khoan bi khoa khong hien voi nguoi khac.
const fs = require('fs'), path = require('path'); const root = path.join(__dirname, '../../src/HoTroDuLich.Web');
const rd = f => fs.readFileSync(path.join(root, f), 'utf8').split('\r\n').join('\n'), wr = (f, s) => fs.writeFileSync(path.join(root, f), s);
const swap = (s, a, b) => { if (!s.includes(a)) throw Error('khong thay: ' + a.slice(0, 90)); return s.replace(a, () => b); };

let j = rd('wwwroot/js/posts.js');
j = swap(j, `if (!(cfg.branches || []).length) { var no = h('option', null, 'Chưa có cơ sở được duyệt'); no.value = ''; branchSel.appendChild(no); } }`,
  `if (!(cfg.branches || []).length) branchSel = null; }`);
j = swap(j, `    left.appendChild(lab); if (!cfg.provider) {`, `    // Co so dang chon chua duoc duyet: noi ro va dan toi trang co so, khong de o chon rong.
    if (cfg.provider && !(cfg.branches || []).length) { var note = h('p', 'post-blocked'); note.appendChild(document.createTextNode('Cơ sở đang chọn chưa được duyệt nên chưa đăng tin được. ')); var ln = h('a', null, 'Xem cơ sở của tôi'); ln.href = '/nha-cung-cap/co-so'; note.appendChild(ln); box.insertBefore(note, box.firstChild); ta.disabled = true; ta.placeholder = 'Chưa đăng tin được'; }
    left.appendChild(lab); if (!cfg.provider) {`);
wr('wwwroot/js/posts.js', j);

let ps = rd('Services/PostService.cs');
ps = swap(ps, `  return db.Set<BaiViet>().Where(p=>p.DeletedAt==null&&(p.AuthorId==me||p.Visibility=="Public"||(p.Visibility=="Friends"&&friendIds.Contains(p.AuthorId))));`,
  `  // Nguoi khac chi thay bai cua tai khoan dang hoat dong; bai cua co so chi hien khi co so con cong khai.
  var publicBranches=Resolve<BranchService>().PublicBranches();
  return db.Set<BaiViet>().Where(p=>p.DeletedAt==null&&(p.AuthorId==me||((p.Visibility=="Public"||(p.Visibility=="Friends"&&friendIds.Contains(p.AuthorId)))
   &&db.TaiKhoan.Any(a=>a.MaTaiKhoan==p.AuthorId&&a.TrangThai==TrangThaiTaiKhoan.HoatDong)
   &&(p.BranchId==null||publicBranches.Any(b=>b.Id==p.BranchId)))));`);
wr('Services/PostService.cs', ps);

let css = rd('wwwroot/css/social.css');
if (!css.includes('.post-blocked')) css += `
.post-blocked{margin:0 0 10px;padding:10px 12px;border-radius:12px;background:#fff7e6;color:#7a4b00;font-size:14px}
.post-blocked a{color:inherit;font-weight:700}
`;
wr('wwwroot/css/social.css', css);
console.log('ok');
