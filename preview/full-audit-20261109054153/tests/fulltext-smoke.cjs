const assert=require('node:assert/strict'),fs=require('node:fs');
const base=process.env.FULLTEXT_TEST_BASE||'http://127.0.0.1:5236';
const decode=s=>s.replace(/&#x([\da-f]+);/gi,(_,n)=>String.fromCodePoint(parseInt(n,16))).replace(/&#(\d+);/g,(_,n)=>String.fromCodePoint(+n)).replaceAll('&amp;','&').replaceAll('&quot;','"');
const val=(page,name)=>decode((typeof page==='string'?page:page.html).match(new RegExp('name="'+name+'"[^>]*value="([^"]*)"'))?.[1]||'');
class Client{
 cookies=new Map();
 async req(path,data){const multipart=data instanceof FormData;const r=await fetch(base+path,{method:data?'POST':'GET',headers:{cookie:[...this.cookies].map(([k,v])=>k+'='+v).join('; '),...(data&&!multipart?{'content-type':'application/x-www-form-urlencoded'}:{})},body:data?(multipart?data:new URLSearchParams(data)):undefined,redirect:'manual'});for(const cookie of r.headers.getSetCookie()){const part=cookie.split(';')[0],at=part.indexOf('=');this.cookies.set(part.slice(0,at),part.slice(at+1));}const html=await r.text();return {status:r.status,location:r.headers.get('location'),html,text:decode(html),headers:r.headers};}
 get(path){return this.req(path);}
 post(path,page,data){assert.ok(val(page,'__RequestVerificationToken'),'CSRF missing '+path);return this.req(path,{__RequestVerificationToken:val(page,'__RequestVerificationToken'),...data});}
 async login(email,password='HanhTrinh@2026'){const page=await this.get('/tai-khoan/dang-nhap/mo-dang-nhap');let r=await this.post('/tai-khoan/dang-nhap/dang-nhap',page,{dinhDanh:email,matKhau:password});assert.equal(r.status,302,r.text.slice(0,1500));if(r.location==='/tai-khoan/doi-mat-khau'){const change=await this.get(r.location);r=await this.post('/tai-khoan/doi-mat-khau',change,{matKhauCu:password,matKhauMoi:'HanhTrinhMoi@2026',xacNhan:'HanhTrinhMoi@2026'});assert.equal(r.status,302,r.text.slice(0,1500));}return r;}
}
module.exports={Client,base,val,decode};
if(require.main===module)(async()=>{
 const guest=new Client(),traveler=new Client(),provider=new Client(),admin=new Client();
 const publicPaths=['/kham-pha','/thong-tin/mo-tim-kiem','/thong-tin/tra-cuu?loai=LuuTru','/thong-tin/xem-chi-tiet?loai=DiaDiem&maMuc=1','/thong-tin/xem-chi-tiet?loai=DichVu&maMuc=1','/dong-hanh/tim-kiem/mo-tim-kiem','/dong-hanh/tim-kiem/tim-nhu-cau','/dong-hanh/tim-kiem/xem-chi-tiet/1','/tai-khoan/dang-ky/mo-dang-ky'];
 const results=[];
 async function check(client,path,expected=200){const r=await client.get(path);results.push({path,status:r.status,expected});console.log(r.status,path);assert.equal(r.status,expected,r.text.replace(/<[^>]+>/g,' ').slice(0,3000));return r;}
 for(const path of publicPaths)await check(guest,path);
 await traveler.login('khach@hanhtrinh.vn');await provider.login('nhacungcap@hanhtrinh.vn');
 try{await admin.login('admin@hanhtrinh.vn');}catch{await admin.login('admin@hanhtrinh.vn','HanhTrinhMoi@2026');}
 for(const path of ['/ke-hoach','/ke-hoach/1','/ke-hoach/mo-ke-hoach-moi','/ke-hoach/1/tinh-huong','/ke-hoach/1/tinh-huong/mo-ghi-nhan','/dong-hanh/nhu-cau/mo-bieu-mau','/dong-hanh/de-nghi','/yeu-cau/mo-dat-dich-vu?maDichVu=1','/yeu-cau/cua-toi','/ho-so/xem-ho-so','/thong-bao','/tai-khoan/ngung-su-dung/mo-ngung'])await check(traveler,path);
 for(const path of ['/nha-cung-cap/dich-vu','/nha-cung-cap/dich-vu/1','/nha-cung-cap/dich-vu/mo-bieu-mau','/nha-cung-cap/ho-so-xac-minh','/nha-cung-cap/quyen-hoat-dong','/nha-cung-cap/yeu-cau','/ho-so/xem-ho-so','/thong-tin/mo-tim-kiem','/dong-hanh/tim-kiem/mo-tim-kiem'])await check(provider,path);
 for(const path of ['/quan-tri/xet-duyet-dich-vu/mo-danh-sach-cho-duyet','/quan-tri/xet-duyet-dich-vu/xem-noi-dung/1','/quan-tri/xac-minh/mo-danh-sach','/quan-tri/xac-minh/xem-ho-so/1','/quan-tri/quyen-hoat-dong/mo-danh-sach','/quan-tri/quyen-hoat-dong/xem-chi-tiet/1','/quan-tri/quyen-hoat-dong/xem-he-qua?maNCC=1&thaoTac=TamDinhChi','/ho-so/xem-ho-so'])await check(admin,path);
 await check(provider,'/ke-hoach',403);await check(traveler,'/nha-cung-cap/dich-vu',403);await check(traveler,'/quan-tri/xac-minh/mo-danh-sach',403);
 fs.writeFileSync(process.env.FULLTEXT_TEST_RESULT||'tests/fulltext-smoke-results.json',JSON.stringify({passed:true,checks:results.length,results,date:new Date().toISOString()},null,2));
})().catch(error=>{console.error(error);process.exitCode=1});
