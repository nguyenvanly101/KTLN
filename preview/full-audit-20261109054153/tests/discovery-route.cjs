process.env.FULLTEXT_TEST_BASE=process.env.DISCOVERY_TEST_BASE||'http://127.0.0.1:5236';
const {Client,val,base}=require('./fulltext-smoke.cjs'),fs=require('node:fs'),assert=require('node:assert/strict');
module.exports=(async()=>{
 const c=new Client(),page=await c.get('/Discovery');assert.equal(page.status,200);const token=val(page,'__RequestVerificationToken');
 const points=[{lat:15.883333,lng:108.333333,name:'Mốc di sản công khai'},{lat:15.880058,lng:108.338047,name:'Mốc đơn vị lưu trú công khai'}];
 const request={points,mode:'foot',corridorKm:1,maxDetourMinutes:20,filter:{Kind:'Service',Region:'Hội An'}};
 const start=Date.now(),r=await fetch(base+'/api/discovery/route',{method:'POST',headers:{'Content-Type':'application/json','RequestVerificationToken':token,Cookie:[...c.cookies].map(([k,v])=>k+'='+v).join('; ')},body:JSON.stringify(request)});const data=await r.json();
 assert.equal(r.status,200,JSON.stringify(data));assert.ok(data.coordinates.length>=2);assert.ok(data.distanceKm>0&&data.minutes>0);assert.equal(data.points.length,2);assert.equal(data.points[0].lat,points[0].lat);assert.equal(data.points[1].lng,points[1].lng);assert.ok(data.note.includes('chưa gồm'));assert.ok(data.nearby.every(x=>x.extraMinutes!=null&&x.extraMinutes<=20));
 fs.writeFileSync('tests/discovery-route-results.json',JSON.stringify({passed:true,checks:8,provider:data.provider,distanceKm:data.distanceKm,minutes:data.minutes,points,nearby:data.nearby,elapsedMs:Date.now()-start,date:new Date().toISOString()},null,2));console.log('PASS real OSRM route: '+data.distanceKm.toFixed(2)+' km, '+data.coordinates.length+' geometry vertices, '+data.nearby.length+' detour candidates');
})().catch(error=>{fs.writeFileSync('tests/discovery-route-results.json',JSON.stringify({passed:false,error:error.stack},null,2));console.error(error);process.exitCode=1});
