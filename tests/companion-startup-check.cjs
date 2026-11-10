// Read-only smoke checks against the actual start.ps1 entry point on the main database.
const fs=require('node:fs'),assert=require('node:assert/strict');
const mode=process.argv[2];assert.ok(['normal','dev'].includes(mode));
const observations=[],file='tests/companion-startup-results.json';
(async()=>{
 for(const [path,status] of [['/',200],['/Together',200],['/css/companion-features.css',200],['/js/companion-features.js',200],['/Together/Mine',302],['/Together/Profile',302],['/Together/Moderation',302]]){
  const r=await fetch('http://localhost:5216'+path,{redirect:'manual'});assert.equal(r.status,status,path);observations.push({path,status:r.status,passed:true});await r.arrayBuffer();
 }
 const prior=fs.existsSync(file)?JSON.parse(fs.readFileSync(file,'utf8')):{modes:{}};
 prior.modes[mode]={passed:true,checks:observations.length,observations,date:new Date().toISOString()};prior.passed=Object.values(prior.modes).every(x=>x.passed);prior.checks=Object.values(prior.modes).reduce((n,x)=>n+x.checks,0);prior.base='http://localhost:5216';prior.database='HoTroDuLich';fs.writeFileSync(file,JSON.stringify(prior,null,2));console.log('PASS',observations.length,'actual start.ps1',mode,'checks');
})().catch(error=>{console.error(error);process.exitCode=1;});
