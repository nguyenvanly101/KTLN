// Reuse real authenticated sessions across test modules instead of repeatedly logging in.
process.env.FULLTEXT_TEST_BASE='http://127.0.0.1:5236';
const {Client}=require('./fulltext-smoke.cjs');const original=Client.prototype.login,sessions=new Map();
Client.prototype.login=async function(email,password){if(sessions.has(email)){this.cookies=new Map(sessions.get(email));return {status:302};}const result=await original.call(this,email,password);sessions.set(email,new Map(this.cookies));return result;};
(async()=>{for(const module of ['./discovery-features.cjs','./discovery-context.cjs','./discovery-edge.cjs','./discovery-route.cjs']){await require(module);if(process.exitCode)throw new Error('Không đạt: '+module);}console.log('PASS final functional suite, shared authenticated sessions');})().catch(error=>{console.error(error);process.exitCode=1});
