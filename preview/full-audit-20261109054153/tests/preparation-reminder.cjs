// Read-only SQL evidence around restarts of the isolated product server.
const fs=require('node:fs'),assert=require('node:assert/strict'),{execFileSync}=require('node:child_process');
const phase=process.argv[2],database='HoTroDuLich_Branches_Audit_20261109054153',output='tests/preparation-reminder-results.json';
assert.ok(['before','first-restart','second-restart'].includes(phase));
const task=Number(JSON.parse(fs.readFileSync('tests/companion-product-regression-results.json')).fixture.task);assert.ok(Number.isSafeInteger(task)&&task>0);
const query=`SET NOCOUNT ON; SELECT m.MaMuc,m.NgayDaNhac,m.NhacHan,m.DaHoanThanh,m.HanHoanThanh,(SELECT COUNT(*) FROM ThongBao t WHERE t.LoaiThongBao=N'NhacChuanBi' AND t.MaNguoiNhan=p.MaTaiKhoan AND t.NoiDung=N'Đến hạn chuẩn bị: '+m.NoiDung+N' · '+p.TenKeHoach) AS Notifications FROM MucChuanBi m JOIN KeHoachChuyenDi p ON p.MaKeHoach=m.MaKeHoach WHERE m.MaMuc=${task} FOR JSON PATH;`;
const raw=execFileSync('sqlcmd',['-S',process.env.AUDIT_SQL_SERVER,'-d',database,'-E','-C','-b','-y','0','-w','65535','-f','65001','-Q',query],{encoding:'utf8'});
const item=JSON.parse(raw.slice(raw.indexOf('['),raw.lastIndexOf(']')+1).replace(/[\r\n]/g,''))[0];
assert.ok(item&&item.NhacHan&&!item.DaHoanThanh);
const report=phase==='before'?{database,task,snapshots:[]}:JSON.parse(fs.readFileSync(output));assert.equal(report.task,task);
report.snapshots.push({phase,...item,date:new Date().toISOString()});
if(phase!=='before'){assert.equal(item.Notifications,1,'Exactly one reminder is persisted');assert.ok(item.NgayDaNhac>=item.HanHoanThanh);}
if(phase==='second-restart'){assert.equal(report.snapshots.find(x=>x.phase==='first-restart').Notifications,item.Notifications);report.passed=true;report.checks=3;}
fs.writeFileSync(output,JSON.stringify(report,null,2));console.log(phase,JSON.stringify(item));
