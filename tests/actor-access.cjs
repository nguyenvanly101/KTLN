// Independent acceptance matrix transcribed from SUC actor roles, not imported from application policy code.
// Test endpoints with real authentication and CSRF cookies, including direct URLs and forged POST requests.
const assert=require('node:assert/strict'),fs=require('fs');
const base='http://localhost:5217';
const roles=['guest','traveler','provider','admin'];let checks=0;const results=[];
function check(ok,name){assert.ok(ok,name);checks++;results.push(name);}
const decode=s=>s.replaceAll('&amp;','&').replaceAll('&quot;','"');
class Client{
 constructor(role){this.role=role;this.jar=new Map();this.token='';}
 async request(path,data){const res=await fetch(base+path,{method:data?'POST':'GET',headers:{cookie:[...this.jar].map(([k,v])=>k+'='+v).join('; '),...(data?{'content-type':'application/x-www-form-urlencoded'}:{})},body:data?new URLSearchParams(data):undefined,redirect:'manual'});for(const c of res.headers.getSetCookie()){const pos=c.indexOf('=');this.jar.set(c.slice(0,pos),c.slice(pos+1).split(';')[0]);}const html=await res.text();return {status:res.status,html,location:res.headers.get('location')};}
 async signIn(email){let p=await this.request('/Account/Login');this.token=decode(p.html.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/)[1]);const r=await this.request('/Account/Login',{__RequestVerificationToken:this.token,Email:email,Password:'HanhTrinh@2026'});check(r.status===302,this.role+' signs in');}
 async tokenFrom(path){const r=await this.request(path);check(r.status===200,this.role+' opens own workspace');this.token=decode(r.html.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/)[1]);return r;}
}
const getRules=[
 ['/Explore/Index',['guest','traveler']],['/Explore/Detail/1',['guest','traveler']],['/Explore/Place/1',['guest','traveler']],
 ['/Companions',['guest','traveler']],['/Companions/Detail/1',['guest','traveler']],
 ['/Companions/Mine',['traveler']],['/Companions/Edit',['traveler']],['/Companions/Connections',['traveler']],
 ['/Plans',['traveler']],['/Plans/Edit',['traveler']],['/Plans/Detail/1',['traveler']],['/Plans/Activity/1',['traveler']],['/Plans/Select',['traveler']],
 ['/Support',['traveler']],['/Support/Create?planId=1',['traveler']],
 ['/Provider',['provider']],['/Provider/Edit',['provider']],['/Provider/Edit/1',['provider']],['/Provider/Verification',['provider']],['/Provider/Rights',['provider']],
 ['/Admin',['admin']],['/Admin/ServiceReviews',['admin']],['/Admin/Verifications',['admin']],['/Admin/Providers',['admin']],['/Admin/Audit',['admin']],['/Admin/Verification/1',['admin']],['/Admin/Provider/1',['admin']],
 ['/Bookings',['traveler','provider']],['/Bookings/Adjustments',['traveler','provider']],['/Bookings/Results',['traveler','provider']],['/Bookings/Create/1',['traveler']],
 ['/Account/Profile',['traveler','provider']],['/Account/Deactivate',['traveler','provider']],
 ['/Account/Notifications',['traveler','provider','admin']],
 ['/Account/Login',['guest']],['/Account/Register',['guest']]
];
const postRules=[
 ['/Plans/Edit',['traveler']],['/Plans/Activity',['traveler']],['/Plans/DeleteActivity',['traveler']],
 ['/Support/Create',['traveler']],['/Support/Action',['traveler']],
 ['/Companions/Edit',['traveler']],['/Companions/Stop',['traveler']],['/Companions/Connect',['traveler']],['/Companions/Respond',['traveler']],['/Companions/Propose',['traveler']],['/Companions/AgreementAction',['traveler']],
 ['/Provider/Edit',['provider']],['/Provider/Reception',['provider']],['/Provider/Supplement',['provider']],['/Provider/Explain',['provider']],['/Provider/Reapply',['provider']],
 ['/Admin/Review',['admin']],['/Admin/Verify',['admin']],['/Admin/Rights',['admin']],['/Admin/Explanation',['admin']],
 ['/Bookings/Create',['traveler']],['/Bookings/Edit',['traveler']],['/Bookings/Change',['traveler']],['/Bookings/Result',['provider']],
 ['/Bookings/Respond',['traveler','provider']],['/Bookings/ChangeAction',['traveler','provider']],['/Bookings/ResultAction',['traveler','provider']],
 ['/Account/Profile',['traveler','provider']],['/Account/Deactivate',['traveler','provider']],
 ['/Account/Register',['guest']],['/Account/Login',['guest']]
];
(async()=>{
 const clients=Object.fromEntries(roles.map(r=>[r,new Client(r)]));
 await clients.traveler.signIn('khach@hanhtrinh.vn');await clients.provider.signIn('nhacungcap@hanhtrinh.vn');await clients.admin.signIn('admin@hanhtrinh.vn');
 const pages={traveler:await clients.traveler.tokenFrom('/Plans'),provider:await clients.provider.tokenFrom('/Provider'),admin:await clients.admin.tokenFrom('/Admin')};
 const guestLogin=await clients.guest.request('/Account/Login');clients.guest.token=decode(guestLogin.html.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/)[1]);
 for(const [path,allowed] of getRules)for(const role of roles){const r=await clients[role].request(path);const expected=allowed.includes(role)?200:role==='guest'?302:403;check(r.status===expected,`${role} GET ${path}: expected ${expected}, got ${r.status}`);}
 for(const [path,allowed]of postRules)for(const role of roles.filter(r=>!allowed.includes(r))){const r=await clients[role].request(path,{__RequestVerificationToken:clients[role].token,Id:'1',Version:'not-a-valid-version',Decision:'DaXacNhan',Name:'Không được lưu',Destination:'Không được lưu',Start:'2026-11-10',End:'2026-11-11',Text:'Không được lưu'});const expected=role==='guest'?302:403;check(r.status===expected,`${role} POST ${path}: expected ${expected}, got ${r.status}`);}
 const badDecisions=[
  ['provider','/Bookings/Respond','Rut'],['provider','/Bookings/Respond','KhachDongY'],['traveler','/Bookings/Respond','DaXacNhan'],['traveler','/Bookings/Respond','TuChoi'],['traveler','/Bookings/Respond','DeXuatKhac'],
  ['provider','/Bookings/ChangeAction','DaRut'],['provider','/Bookings/ChangeAction','KhachDongY'],['traveler','/Bookings/ChangeAction','ChapNhan'],['traveler','/Bookings/ChangeAction','TuChoi'],['traveler','/Bookings/ChangeAction','DeXuatKhac'],
  ['provider','/Bookings/ResultAction','PhanAnh'],['traveler','/Bookings/ResultAction','DayDu'],['traveler','/Bookings/ResultAction','MotPhan']
 ];
 for(const [role,path,decision]of badDecisions){const r=await clients[role].request(path,{__RequestVerificationToken:clients[role].token,Id:'1',Version:'not-a-valid-version',Decision:decision,Text:'Không được lưu'});check(r.status===403,`${role} must not perform ${decision} in ${path}; got ${r.status}`);}
 for(const role of ['provider','admin']){
  for(const [path,allowed]of getRules.filter(([_,allowed])=>allowed.includes(role))){const r=await clients[role].request(path);const hrefs=[...r.html.matchAll(/href="([^"#]+)[^"]*"/g)].map(m=>m[1]);check(!hrefs.some(h=>/^\/(Companions|Plans|Support|Explore)(\/|\?|$)/i.test(h)),`${role} page ${path} has no traveler navigation or CTA`);if(role==='admin')check(!hrefs.includes('/Account/Profile'),`admin page ${path} has no profile editing link`);}
  const r=await clients[role].request('/');check(r.status===302&&r.location===(role==='provider'?'/Provider':'/Admin'),`${role} root redirects to its own workspace`);
  const alias=await clients[role].request('/Home/Index');check(alias.status===302&&alias.location===(role==='provider'?'/Provider':'/Admin'),`${role} cannot bypass its workspace via Home alias`);
 }
 // Object-level access is still enforced for the other traveler, even with the correct ACT01 role.
 const other=new Client('other traveler');await other.signIn('linh@hanhtrinh.vn');const denied=await other.request('/Plans/Detail/1');check(denied.status===403,'ACT01 cannot read another traveler plan');
 const logout=await clients.provider.request('/Account/Logout',{__RequestVerificationToken:clients.provider.token});check(logout.status===302,'Provider can explicitly sign out');const anonymous=await clients.provider.request('/Companions');check(anonymous.status===200,'Only an actual guest after sign-out uses public companion viewing');
 fs.writeFileSync('D:/KLTN/tests/actor-access-results.json',JSON.stringify({passed:true,checks,date:new Date().toISOString(),cases:results},null,2));console.log(`PASS: ${checks} actor/access checks across all 18 SUCs, wrong-task requests, navigation, and object ownership.`);
})().catch(e=>{console.error(e);process.exitCode=1});
