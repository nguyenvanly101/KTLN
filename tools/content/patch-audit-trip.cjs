// Ra soat chuyen di: nut "Xac nhan lich trinh" dua ra canh nhan trang thai (truoc bi giau trong "Them"); bo chu do "Co van de" o moi hoat dong.
const fs = require('fs'), path = require('path'); const root = path.join(__dirname, '../../src/HoTroDuLich.Web');
const rd = f => fs.readFileSync(path.join(root, f), 'utf8').split('\r\n').join('\n'), wr = (f, s) => fs.writeFileSync(path.join(root, f), s);
const swap = (s, a, b) => { if (!s.includes(a)) throw Error('khong thay: ' + a.slice(0, 90)); return s.replace(a, () => b); };

let v = rd('Views/Plans/Detail.cshtml');
const form = `@if(isOwner){<form action="/Plans/Decision" method="post">@Html.AntiForgeryToken()<input type="hidden" name="Id" value="@plan.MaKeHoach"/><input type="hidden" name="Version" value="@TravelService.Token(plan.RowVersion)"/><button name="Decision" value="@(plan.TrangThai == TrangThaiKeHoach.Nhap ? "DaChapNhan" : "Nhap")" type="submit">@(plan.TrangThai == TrangThaiKeHoach.Nhap ? "Xác nhận lịch trình" : "Chuyển về bản nháp")</button></form><a href="/Journey/Share?planId=@plan.MaKeHoach">Chia sẻ</a>}`;
v = swap(v, form, `@if(isOwner){@if(plan.TrangThai != TrangThaiKeHoach.Nhap){<form action="/Plans/Decision" method="post">@Html.AntiForgeryToken()<input type="hidden" name="Id" value="@plan.MaKeHoach"/><input type="hidden" name="Version" value="@TravelService.Token(plan.RowVersion)"/><button name="Decision" value="Nhap" type="submit">Chuyển về bản nháp</button></form>}<a href="/Journey/Share?planId=@plan.MaKeHoach">Chia sẻ</a>}`);
// Nhan trang thai + nut xac nhan ngay canh (chu so huu, ban nhap)
v = swap(v, `<span class="badge @Labels.Badge(plan.TrangThai.ToString())">@Labels.PlanStatus(plan.TrangThai.ToString())</span></p>`,
  `<span class="badge @Labels.Badge(plan.TrangThai.ToString())">@Labels.PlanStatus(plan.TrangThai.ToString())</span>@if(isOwner&&plan.TrangThai==TrangThaiKeHoach.Nhap){<form class="trip-confirm" action="/Plans/Decision" method="post">@Html.AntiForgeryToken()<input type="hidden" name="Id" value="@plan.MaKeHoach"/><input type="hidden" name="Version" value="@TravelService.Token(plan.RowVersion)"/><button name="Decision" value="DaChapNhan" type="submit" title="Chốt lịch trình: bạn bè trong chuyến thấy đây là lịch đã thống nhất">✓ Xác nhận lịch trình</button></form>}</p>`);
wr('Views/Plans/Detail.cshtml', v);

let css = rd('wwwroot/css/site.css');
if (!css.includes('/* Xac nhan lich trinh */')) css += `
/* Xac nhan lich trinh */
.trip-confirm{display:inline}
.trip-confirm button{margin-left:8px;border:1px solid #ffffff55;background:#ffffff14;color:#fff;border-radius:999px;padding:3px 12px;font-size:13px;font-weight:600;cursor:pointer}
.trip-confirm button:hover{background:#ffffff2a}
`;
wr('wwwroot/css/site.css', css);
console.log('ok');
