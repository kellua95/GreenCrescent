window.addEventListener("pageshow", (event) => {
    if (event.persisted) {
        window.location.reload();
    }
});

document.addEventListener("submit", (event) => {
    const form = event.target;

    if (!(form instanceof HTMLFormElement)) {
        return;
    }

    const action = new URL(form.action, document.baseURI);

    if (
        action.origin !== window.location.origin ||
        !action.pathname.toLowerCase().endsWith("/account/logout")
    ) {
        return;
    }

    try {
        const prefix = "gc:receipt-position:";
        const keysToRemove = [];

        for (let index = 0; index < sessionStorage.length; index++) {
            const key = sessionStorage.key(index);

            if (key?.startsWith(prefix)) {
                keysToRemove.push(key);
            }
        }

        for (const key of keysToRemove) {
            sessionStorage.removeItem(key);
        }
    } catch {
        // فشل تنظيف التخزين لا يمنع إرسال طلب تسجيل الخروج.
    }
}, true);