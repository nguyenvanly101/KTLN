(() => {
 const toggle=document.querySelector('.menu-toggle');
 const workspace=document.body.classList.contains('is-workspace');
 const navigation=document.querySelector(workspace?'.workspace-sidebar':'.mainnav');
 const content=document.querySelector('.app-content');
 const scrim=document.querySelector('.nav-scrim');
 const breakpoint=matchMedia('(max-width:1000px)');
 const headerMenus=[...document.querySelectorAll('[data-header-menu]')];
 const visible=element=>{
  if(!element?.getClientRects().length||getComputedStyle(element).visibility==='hidden')return false;
  const closed=element.closest('details:not([open])');
  return !closed||closed.querySelector('summary')?.contains(element);
 };
 const updateAccessibility=()=>{
  const mobileOpen=breakpoint.matches&&navigation?.classList.contains('open');
  if(navigation)navigation.inert=breakpoint.matches&&!mobileOpen;
  if(content)content.inert=!!mobileOpen;
  document.documentElement.classList.toggle('menu-open',!!mobileOpen);
 };
 const closeMenu=(restoreFocus=false)=>{
  const wasOpen=navigation?.classList.contains('open');
  navigation?.classList.remove('open');
  toggle?.setAttribute('aria-expanded','false');
  toggle?.setAttribute('aria-label','Mở menu điều hướng');
  if(scrim)scrim.hidden=true;
  updateAccessibility();
  if(wasOpen&&restoreFocus)toggle?.focus();
 };
 toggle?.addEventListener('click',()=>{
  if(navigation?.classList.contains('open')){closeMenu(true);return;}
  headerMenus.forEach(menu=>menu.open=false);
  navigation?.classList.add('open');
  toggle.setAttribute('aria-expanded','true');
  toggle.setAttribute('aria-label','Đóng menu điều hướng');
  if(scrim)scrim.hidden=false;
  updateAccessibility();
  requestAnimationFrame(()=>[...(navigation?.querySelectorAll('a,button,summary')??[])].find(visible)?.focus());
 });
 breakpoint.addEventListener('change',()=>closeMenu());
 updateAccessibility();
 scrim?.addEventListener('click',()=>closeMenu(true));
 headerMenus.forEach(menu=>menu.addEventListener('toggle',()=>{
  if(menu.open)headerMenus.forEach(other=>{if(other!==menu)other.open=false;});
 }));
 document.addEventListener('keydown',e=>{
  if(e.key==='Escape'){
   const openMenu=headerMenus.find(menu=>menu.open&&menu.contains(document.activeElement));
   if(openMenu){openMenu.open=false;openMenu.querySelector('summary')?.focus();e.preventDefault();return;}
   if(navigation?.classList.contains('open')){closeMenu(true);e.preventDefault();return;}
   headerMenus.forEach(menu=>menu.open=false);
  }
  if(e.key==='Tab'&&breakpoint.matches&&navigation?.classList.contains('open')){
   const controls=[...navigation.querySelectorAll('a,button,summary,input,select,[tabindex]'),toggle].filter(element=>visible(element)&&!element.disabled&&element.tabIndex>=0);
   const index=controls.indexOf(document.activeElement);
   const next=(index+(e.shiftKey?-1:1)+controls.length)%controls.length;
   if(controls.length){e.preventDefault();controls[next].focus();}
  }
 });
 document.addEventListener('click',e=>{headerMenus.forEach(menu=>{if(!menu.contains(e.target))menu.open=false;});});
 document.querySelectorAll('.sidebar-nav a,.mainnav a').forEach(a=>a.addEventListener('click',()=>closeMenu()));
 document.querySelectorAll('.dismiss').forEach(b=>b.addEventListener('click',()=>b.closest('.toast-message').remove()));
 document.querySelectorAll('[data-email]').forEach(b=>b.addEventListener('click',()=>{document.querySelector('[name="Email"]').value=b.dataset.email;document.querySelector('[name="Password"]').value='HanhTrinh@2026';}));
 document.querySelectorAll('[data-confirm]').forEach(f=>f.addEventListener('submit',e=>{if(!confirm(f.dataset.confirm))e.preventDefault();}));
 document.querySelectorAll('[data-back]').forEach(b=>b.addEventListener('click',()=>history.back()));
 document.querySelectorAll('[data-password-target]').forEach(b=>b.addEventListener('click',()=>{const input=document.getElementById(b.dataset.passwordTarget);const reveal=input.type==='password';input.type=reveal?'text':'password';b.textContent=reveal?'Ẩn':'Hiện';b.setAttribute('aria-label',reveal?'Ẩn mật khẩu':'Hiển thị mật khẩu');}));
 const role=document.querySelector('[data-account-role]');
 const updateRole=()=>{const provider=role?.value==='NHA_CUNG_CAP';document.querySelectorAll('[data-provider-fields]').forEach(e=>e.hidden=!provider);['Organization','Address','OrganizationType','ContactName','ContactPhone','ContactEmail','RegistrationDescription','Representative','Documents'].forEach(name=>{const input=document.querySelector('[name="'+name+'"]');if(input)input.required=provider;});};
 role?.addEventListener('change',updateRole);if(role)updateRole();
 document.querySelectorAll('.tabs').forEach(nav=>{const current=location.pathname.replace(/\/$/,'').toLowerCase();const match=[...nav.querySelectorAll('a')].find(a=>new URL(a.href).pathname.replace(/\/$/,'').toLowerCase()===current);if(match){nav.querySelectorAll('a').forEach(a=>{a.classList.remove('active');a.removeAttribute('aria-current');});match.classList.add('active');match.setAttribute('aria-current','page');}});
 document.querySelectorAll('.sidebar-nav a.active,.mainnav a.active').forEach(a=>a.setAttribute('aria-current','page'));
 const plans=document.querySelector('[data-plans]');
 if(plans){
  const cards=[...plans.querySelectorAll('[data-plan-card]')];
  const buttons=[...plans.querySelectorAll('[data-plan-filter]')];
  const query=plans.querySelector('[data-plan-query]');
  const count=plans.querySelector('[data-plan-count]');
  const empty=plans.querySelector('[data-plan-empty]');
  const tools=plans.querySelector('[data-plan-tools]');
  let selected='all';
  const normalize=value=>value.normalize('NFD').replace(/\p{Diacritic}/gu,'').replace(/đ/gi,'d').toLocaleLowerCase('vi');
  const filter=()=>{
   const term=normalize(query?.value.trim()??'');
   let matches=0;
   cards.forEach(card=>{const match=(selected==='all'||card.dataset.status===selected)&&normalize(card.dataset.search).includes(term);card.hidden=!match;if(match)matches++;});
   buttons.forEach(button=>button.setAttribute('aria-pressed',String(button.dataset.planFilter===selected)));
   if(count)count.textContent=matches+' chuyến đi phù hợp trong '+cards.length+' chuyến đi.';
   if(empty)empty.hidden=matches>0||cards.length===0;
  };
  if(tools)tools.hidden=false;
  buttons.forEach(button=>button.addEventListener('click',()=>{selected=button.dataset.planFilter;filter();}));
  query?.addEventListener('input',filter);
  plans.querySelector('[data-plan-reset]')?.addEventListener('click',()=>{selected='all';if(query)query.value='';filter();query?.focus();});
 }
})();
(() => {
 const data=document.getElementById('rejected-input');
 if(data){try{const values=JSON.parse(data.textContent);const id=values.Id?.[0];for(const form of document.querySelectorAll('form[method="post"]')){if(id&&form.querySelector('[name="Id"]')?.value!==id)continue;for(const [key,list] of Object.entries(values)){for(const input of form.elements){if(input.name!==key||['hidden','file','password','submit'].includes(input.type))continue;if(input.type==='checkbox'||input.type==='radio')input.checked=list.includes(input.value);else if(input.tagName==='SELECT'&&input.multiple)for(const option of input.options)option.selected=list.includes(option.value);else input.value=list[0]??'';}}}}catch{}}
 const summary=document.querySelector('.validation-summary-errors');
 if(summary){summary.setAttribute('role','alert');summary.tabIndex=-1;summary.focus();}
})();


// Compare only the chosen saved services; the server repeats these checks.
for (const form of document.querySelectorAll('[data-compare-form]')) {
    form.addEventListener('submit', event => {
        const selected = document.querySelectorAll('input[form="compare-form"][name="ids"]:checked').length;
        const message = document.getElementById('compare-error');
        if (selected < 2 || selected > 3) {
            event.preventDefault();
            message.textContent = 'Chọn từ 2 đến 3 dịch vụ để so sánh.';
            message.tabIndex = -1;
            message.focus();
        } else message.textContent = '';
    });
}

const productErrors = document.querySelector('[data-product-errors]');
if (productErrors) {
    productErrors.focus();
    productErrors.querySelectorAll('a[href^="#"]').forEach(link => link.addEventListener('click', event => {
        const field = document.getElementById(link.hash.slice(1));
        if (!field) return;
        event.preventDefault();
        field.focus();
        field.scrollIntoView({ block: 'center' });
    }));
}
