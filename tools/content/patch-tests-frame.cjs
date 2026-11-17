// Cap nhat kiem thu cho khung moi: khach du lich dung thanh tren duy nhat, khong thanh ben, khong tab Kham pha.
const fs = require('fs');
const edit = (f, pairs) => { let s = fs.readFileSync(f, 'utf8'); const eol = s.includes('\r\n') ? '\r\n' : '\n'; for (let [a, b] of pairs) { a = a.split('\n').join(eol); b = b.split('\n').join(eol); if (!s.includes(a)) throw Error(f + ': khong thay ' + a.slice(0, 70)); s = s.replace(a, () => b); } fs.writeFileSync(f, s); };
const R = String.raw;
edit('tests/flow-v2.cjs', [[
  R`  const side = (t1.match(/<nav[^>]*class="sidebar-nav"[\s\S]*?<\/nav>/) || [''])[0];` + '\n' + R`  check(side.includes('Khám phá') && side.includes('Bảng tin') && side.includes('Chuyến đi') && !side.includes('Tin nhắn') && !side.includes('Hỗ trợ chuyến đi'), 'Khach: menu ben gon (Kham pha, Bang tin, Chuyen di), Tin nhan khong nam trong menu ben');`,
  R`  const side = (t1.match(/<nav[^>]*id="main-navigation"[\s\S]*?<\/nav>/) || [''])[0];` + '\n' + R`  check(!t1.includes('workspace-sidebar') && side.includes('Khám phá') && side.includes('Bảng tin') && side.includes('Chuyến đi') && !side.includes('Tin nhắn') && !side.includes('Hỗ trợ chuyến đi'), 'Khach: chi con thanh tren (Kham pha, Chuyen di, Bang tin), khong thanh ben');`]]);
edit('tests/flow-v3.cjs', [
  [R`const sidebarOf = html => (decode(html).match(/<nav[^>]*class="sidebar-nav"[\s\S]*?<\/nav>/) || [''])[0];`,
   R`const sidebarOf = html => (decode(html).match(/<nav[^>]*id="main-navigation"[\s\S]*?<\/nav>/) || [''])[0];`],
  [R`side.indexOf('Bảng tin') >= 0 && side.indexOf('Bảng tin') < side.indexOf('Khám phá') && side.indexOf('Khám phá') < side.indexOf('Chuyến đi') && !side.includes('Dịch vụ đã lưu'), 'Menu khach: Bang tin dau tien, Kham pha (da gom Da luu), Chuyen di'`,
   R`side.indexOf('Khám phá') >= 0 && side.indexOf('Khám phá') < side.indexOf('Chuyến đi') && side.indexOf('Chuyến đi') < side.indexOf('Bảng tin') && !side.includes('Dịch vụ đã lưu') && !decode(feed.html).includes('workspace-sidebar'), 'Thanh tren cua khach: Kham pha, Chuyen di, Bang tin; khong thanh ben'`],
  ["check(r.status === 200 && decode(r.html).includes('explore-tabs'), `Trang ${label} co thanh chuyen Tim kiem / Ban do / Da luu`);",
   "check(r.status === 200 && !decode(r.html).includes('explore-tabs'), `Trang ${label} khong con thanh tab Kham pha rieng`);"]]);
edit('tests/navigation-redesign.cjs', [[
  R` const sidebar=plans.text.match(/<aside\b[^>]*class="workspace-sidebar"[\s\S]*?<\/aside>/)[0];` + '\n' + R` check(sidebar.includes('Chuyến đi')&&sidebar.includes('Bảng tin'),'Traveler workspace groups trips and feed (companions are feed posts)');` + '\n' + R` check(sidebar.includes('href="/bang-tin"')&&(sidebar.match(/class="sidebar-direct/g)||[]).length<=10,'Traveler sidebar is a short flat list');`,
  R` check(!plans.text.includes('workspace-sidebar'),'Traveler has a single top bar and no sidebar');` + '\n' + R` const sidebar=plans.text.match(/<nav\b[^>]*id="main-navigation"[\s\S]*?<\/nav>/)[0];` + '\n' + R` check(sidebar.includes('Chuyến đi')&&sidebar.includes('Bảng tin')&&sidebar.includes('Khám phá'),'Traveler top bar groups discovery, trips and feed (companions are feed posts)');` + '\n' + R` check(sidebar.includes('href="/bang-tin"')&&(sidebar.match(/<a /g)||[]).length<=4,'Traveler top navigation is a short flat list');`]]);
edit('tests/chat-dock.cjs', [[
  "check(full.html.includes('workspace-sidebar') && !full.html.includes('is-embed'), 'Trang /tin-nhan day du co thanh ben');",
  "check(!full.html.includes('workspace-sidebar') && full.html.includes('id=\"main-navigation\"') && !full.html.includes('is-embed'), 'Trang /tin-nhan day du dung thanh tren, khong thanh ben');"]]);
console.log('ok');
