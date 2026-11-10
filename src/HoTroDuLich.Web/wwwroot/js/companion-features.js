// Link validation errors to visible controls inside the form that failed.
(() => {
    const summary = document.querySelector('[data-together-errors]');
    if (!summary) return;
    let values = {};
    try { values = JSON.parse(document.getElementById('rejected-input')?.textContent || '{}'); } catch { /* Keep server-rendered errors. */ }
    const forms = [...document.querySelectorAll('form[method="post"]')];
    const matchingForm = forms.find(form => {
        const action = new URL(form.action, location.href).pathname;
        if (action !== location.pathname) return false;
        return !values.Id?.[0] || form.querySelector('[name="Id"]')?.value === values.Id[0];
    });
    for (const item of summary.querySelectorAll('[data-field]')) {
        const name = item.dataset.field?.replace(/^input\./i, '');
        const field = [...(matchingForm?.elements || [])].find(control => control.name === name && control.type !== 'hidden' && !control.disabled);
        if (!field?.id) continue;
        const link = document.createElement('a');
        link.href = '#' + field.id;
        link.textContent = item.textContent;
        link.addEventListener('click', event => {
            event.preventDefault();
            field.focus();
            field.scrollIntoView({ block: 'center', behavior: 'auto' });
        });
        item.replaceChildren(link);
        field.setAttribute('aria-invalid', 'true');
        const error = document.createElement('p');
        error.id = field.id + '-error';
        error.className = 'field-validation-error';
        error.textContent = link.textContent;
        field.after(error);
        field.setAttribute('aria-describedby', [field.getAttribute('aria-describedby'), error.id].filter(Boolean).join(' '));
    }
    summary.focus();
})();

// Reveal exact allocations only when the member chooses a custom split.
for (const form of document.querySelectorAll('[data-group-expense]')) {
    const mode = form.elements.Equal;
    const update = () => {
        const custom = mode.value === 'false';
        for (const person of form.querySelectorAll('.together-share-person')) {
            const selected = person.querySelector('[name="Participants"]').checked;
            const amount = person.querySelector('input[type="number"]');
            const label = person.querySelector(`label[for="${amount.id}"]`);
            amount.hidden = label.hidden = !custom;
            amount.disabled = !custom || !selected;
            amount.required = custom && selected;
        }
    };
    form.addEventListener('change', update);
    update();
}

// Poll only for the count. The composer and reading position remain untouched.
(() => {
    const thread = document.querySelector('[data-thread-group]');
    const notice = document.querySelector('[data-thread-notice]');
    if (!thread || !notice || Number(thread.dataset.threadGroup) <= 0) return;
    let pending = false;
    const timer = window.setInterval(async () => {
        if (document.hidden || pending) return;
        pending = true;
        try {
            const response = await fetch(`/Together/Thread?groupId=${encodeURIComponent(thread.dataset.threadGroup)}&after=${encodeURIComponent(thread.dataset.threadAfter)}`, { cache: 'no-store', credentials: 'same-origin', redirect: 'error' });
            if (!response.ok) { if ([401, 403, 404].includes(response.status)) window.clearInterval(timer); return; }
            const result = await response.json();
            if (Number(result.newCount) > 0) notice.hidden = false;
        } catch { /* Keep the regular refresh link available when offline. */ }
        finally { pending = false; }
    }, 15000);
    window.addEventListener('pagehide', () => window.clearInterval(timer), { once: true });
})();
