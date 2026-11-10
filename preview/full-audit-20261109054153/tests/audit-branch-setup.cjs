const { randomUUID } = require('node:crypto');
const assert = require('node:assert/strict');
const { val } = require('./fulltext-smoke.cjs');
exports.approvedBranch = async function approvedBranch(owner, admin, {type,address='Địa chỉ chi nhánh kiểm thử'}) {
  const name='Audit branch '+randomUUID().slice(0,8);
  const editor = await owner.get('/nha-cung-cap/co-so/them');
  assert.equal(editor.status,200,'Open authorized branch editor');
  const saved = await owner.post('/nha-cung-cap/co-so/luu',editor,{
    Id:0, Name:name, Address:address, Region:'Đà Lạt', Type:type,
    LocationSource:'Provider', Latitude:11.94, Longitude:108.44,
    ConfirmLocation:true, OwnershipEvidence:'Hồ sơ kiểm thử nội bộ', Operation:'Submit'
  });
  assert.equal(saved.status,302,'Submit test branch: '+saved.text.replace(/<[^>]+>/g,' ').slice(-1100));
  const id = Number(saved.location.split('/').at(-1));
  assert.ok(id>0,'Test branch received an id');
  const queue = await admin.get('/quan-tri/co-so');
  assert.equal(queue.status,200);
  const row = queue.html.split(/<article|<tr[\s>]/).find(segment=>segment.includes(name));
  const reviewUrl = row?.match(/href="(\/quan-tri\/co-so\/\d+)"/)?.[1];
  assert.ok(reviewUrl,'Exact branch in moderation queue');
  const review = await admin.get(reviewUrl);
  const decision = await admin.post('/quan-tri/co-so/duyet',review,{Id:val(review,'Id'),Version:val(review,'Version'),Decision:'Approved'});
  assert.equal(decision.status,302,'Approve test branch');
  return id;
};
