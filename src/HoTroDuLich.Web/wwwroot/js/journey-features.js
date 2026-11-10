(() => {
    'use strict';
    document.querySelectorAll('[data-trip-print]').forEach(button => button.addEventListener('click', () => window.print()));
    const copyLink = async (input, status) => {
        const url = new URL(input.value, window.location.origin).href;
        try {
            if (!navigator.clipboard?.writeText) throw new Error('clipboard unavailable');
            await navigator.clipboard.writeText(url);
            if (status) status.textContent = 'Đã sao chép liên kết. Bạn có thể gửi cho người muốn chia sẻ.';
        } catch {
            input.value = url;
            input.focus();
            input.select();
            if (status) status.textContent = 'Liên kết đã được chọn. Nhấn Ctrl+C hoặc dùng thao tác Sao chép của thiết bị.';
        }
    };
    document.querySelectorAll('[data-copy-trip]').forEach(button => button.addEventListener('click', async () => {
        const input = document.getElementById(button.dataset.copyTrip);
        if (input) await copyLink(input, button.closest('.share-created')?.querySelector('[data-share-status]'));
    }));
    document.querySelectorAll('[data-native-trip-share]').forEach(button => button.addEventListener('click', async () => {
        const input = document.getElementById(button.dataset.nativeTripShare);
        if (!input) return;
        const status = button.closest('.share-created')?.querySelector('[data-share-status]');
        if (!navigator.share) { await copyLink(input, status); return; }
        try { await navigator.share({ title: document.title, url: new URL(input.value, window.location.origin).href }); }
        catch (error) { if (error.name !== 'AbortError') await copyLink(input, status); }
    }));
})();
