// Kham pha: hero anh + o tim, the diem den dang anh doc (khong con chu cai de len ten), bo tab Kham pha trung lap.
const fs=require('fs'),path=require('path');const root=path.join(__dirname,'../../src/HoTroDuLich.Web');
const rd=f=>fs.readFileSync(path.join(root,f),'utf8'),wr=(f,s)=>fs.writeFileSync(path.join(root,f),s);
// --- CSS: thay hm-hero / hm-dest* trong dia-gioi.css
let css=rd('wwwroot/css/dia-gioi.css');const lines=css.split('\n');
const find=pre=>{const i=lines.findIndex(l=>l.trimStart().startsWith(pre));if(i<0)throw Error("khong thay "+pre);return i;};
const a=find('.hm-hero{display:grid'),b=find('.hm-hero h1{');
const c=find('.hm-dest-grid{'),d=find('.hm-dest-copy em{');
if(!(b===a+1&&d>c))throw Error('khung css khac du kien');
const heroCss=`.hm-hero{position:relative;color:#fff;background:#142447;overflow:hidden}
.hm-hero>img{position:absolute;inset:0;width:100%;height:100%;object-fit:cover;object-position:center 58%}
.hm-hero::after{content:"";position:absolute;inset:0;background:linear-gradient(180deg,rgba(20,36,71,.4),rgba(20,36,71,.84))}
.hm-hero-in{position:relative;z-index:1;display:grid;gap:16px;max-width:1480px;margin:auto;padding:clamp(44px,8vw,92px) clamp(18px,4vw,64px) clamp(30px,5vw,54px)}
body .hm-hero h1{margin:0;color:#fff;font-size:clamp(2rem,5vw,3.3rem);line-height:1.08;letter-spacing:-.03em;max-width:16ch;text-wrap:balance}
body .hm-hero h1 em{font-style:normal;color:#def69b}
body .hm-hero p{color:#e8ecf7;max-width:56ch;margin:0}
body .hm-hero .eyebrow{color:#def69b;margin:0}
.hm-hero .hm-search{background:#fff;border-radius:18px;padding:6px;max-width:680px;box-shadow:0 14px 36px rgba(10,20,50,.3);flex-wrap:nowrap}
.hm-hero .hm-search .ds-query-row{border:0;box-shadow:none;background:transparent;min-height:50px}
.hm-hero .hm-search .btn{min-height:50px;border-radius:13px}
.hm-hero .hm-quick{color:#e8ecf7}
.hm-hero .hm-quick a{background:rgba(255,255,255,.16);border-color:rgba(255,255,255,.42);color:#fff}
.hm-hero .hm-quick a:hover,.hm-hero .hm-quick a:focus-visible{background:#fff;color:#2447e8;border-color:#fff}`;
const destCss=`.hm-dest-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:14px}
body .hm-dest{position:relative;display:flex;align-items:flex-end;aspect-ratio:3/4;border-radius:18px;overflow:hidden;background:linear-gradient(160deg,#2447e8,#142447);color:#fff;text-decoration:none}
.hm-dest img{position:absolute;inset:0;width:100%;height:100%;object-fit:cover;transition:transform .4s}
.hm-dest:hover img,.hm-dest:focus-visible img{transform:scale(1.04)}
.hm-dest::after{content:"";position:absolute;inset:0;background:linear-gradient(180deg,transparent 42%,rgba(20,36,71,.9))}
.hm-dest-blank{position:absolute;inset:0;display:grid;place-items:start end;padding:16px;color:rgba(255,255,255,.3)}
.hm-dest-blank .icon{width:72px;height:72px}
.hm-dest-copy{position:relative;z-index:1;display:grid;gap:3px;padding:16px}
body .hm-dest-copy strong{color:#fff;font-size:1.15rem;line-height:1.2}
body .hm-dest-copy small{color:#e8ecf7}
.hm-dest-copy em{display:none}
.hm-links{display:flex;flex-wrap:wrap;gap:8px 18px;color:var(--ds-muted,#5c667b)}`;
lines.splice(c,d-c+1,destCss);          // thay sau (chi so lon hon) truoc
lines.splice(a,b-a+1,heroCss);
css=lines.join('\n');
css=css.replace('.hm-tiles{grid-template-columns:repeat(3,minmax(0,1fr))}.dest-split','.hm-tiles{grid-template-columns:repeat(3,minmax(0,1fr))}.hm-dest-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.dest-split');
wr('wwwroot/css/dia-gioi.css',css);
// --- Anh: Quang Ninh dung anh vinh Ha Long
let ui=rd('Services/Ui.cs');
ui=ui.split('name.Contains("Hạ Long")').join('name.Contains("Hạ Long") || name.Contains("Quảng Ninh")').split('region.Contains("Hạ Long")').join('region.Contains("Hạ Long") || region.Contains("Quảng Ninh")');
wr('Services/Ui.cs',ui);
// --- Tab Kham pha trung lap: bo (loi tat chuyen sang trang Kham pha)
wr('Views/Shared/_ExploreTabs.cshtml','@model string\n@* Tab Khám phá cũ đã bỏ: thanh trên cùng đã có Khám phá; lối tắt tới bản đồ, dịch vụ đã lưu, lịch mẫu nằm ở cuối trang Khám phá. *@\n');
console.log('ok');
