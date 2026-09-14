// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Each control is scoped to its own wrapper, keeping Register's password fields independent.
document.querySelectorAll('.mc-password-toggle').forEach((button) => {
    button.addEventListener('click', function () {
        const wrapper = this.closest('.mc-password-wrap');
        if (!wrapper) return;

        const input = wrapper.querySelector('input');
        const showIcon = this.querySelector('.mc-icon-show');
        const hideIcon = this.querySelector('.mc-icon-hide');
        if (!input) return;

        const isHidden = input.type === 'password';
        input.type = isHidden ? 'text' : 'password';

        if (showIcon) showIcon.hidden = isHidden;
        if (hideIcon) hideIcon.hidden = !isHidden;

        const label = isHidden
            ? (this.dataset.hideLabel || 'Hide password')
            : (this.dataset.showLabel || 'Show password');

        this.setAttribute('aria-label', label);
        this.setAttribute('aria-pressed', String(isHidden));
        this.title = label;
    });
});

document.querySelectorAll('.mc-security-password-form').forEach((form) => {
    form.addEventListener('submit', function () {
        const newPassword = this.querySelector('#mc-new-password');
        const confirmPassword = this.querySelector('#mc-confirm-password');
        if (!this.checkValidity() || (newPassword && confirmPassword && newPassword.value !== confirmPassword.value)) return;

        const button = this.querySelector('[data-security-submit]');
        const label = this.querySelector('[data-security-submit-label]');
        if (!button || button.disabled) return;

        button.disabled = true;
        if (label) label.textContent = 'Changing Password...';
    });
});

document.querySelectorAll('[data-forgot-password-form]').forEach((form) => {
    form.addEventListener('submit', function () {
        if (!this.checkValidity()) return;

        const button = this.querySelector('[data-forgot-password-submit]');
        const label = this.querySelector('[data-forgot-password-label]');
        if (!button || button.disabled) return;

        button.disabled = true;
        if (label) label.textContent = 'Sending...';
    });
});

document.querySelectorAll('[data-digits-only]').forEach((input) => {
    input.addEventListener('input', function () {
        this.value = this.value.replace(/\D/g, '');
    });
});

document.querySelectorAll('[data-counsellor-search]').forEach((input) => {
    const list = document.querySelector('[data-counsellor-list]');
    const emptyState = document.querySelector('[data-counsellor-no-results]');
    if (!list || !emptyState) return;

    const rows = Array.from(list.querySelectorAll('[data-counsellor-row]'));
    input.addEventListener('input', function () {
        const searchTerm = this.value.trim().toLowerCase();
        let matches = 0;

        rows.forEach((row) => {
            const isMatch = row.textContent.toLowerCase().includes(searchTerm);
            row.hidden = !isMatch;
            if (isMatch) matches += 1;
        });

        emptyState.hidden = matches !== 0;
    });
});

document.querySelectorAll('[data-counsellor-delete-form]').forEach((form) => {
    form.addEventListener('submit', function (event) {
        const name = this.dataset.counsellorName;
        const message = name
            ? `Are you sure you want to delete ${name}?`
            : 'Are you sure you want to delete this counsellor?';

        if (!window.confirm(message)) event.preventDefault();
    });
});

document.querySelectorAll('[data-auto-dismiss]').forEach((alert) => {
    const delay = Number(alert.dataset.autoDismiss) || 5000;
    window.setTimeout(() => {
        alert.classList.add('is-hiding');
        window.setTimeout(() => alert.remove(), 250);
    }, delay);
});

document.querySelectorAll('[data-payment-form]').forEach((form) => {
    form.addEventListener('submit', function () {
        if (!this.checkValidity()) return;

        const button = this.querySelector('button[type="submit"]');
        const label = this.querySelector('[data-payment-label]');
        if (!button || button.disabled) return;

        button.disabled = true;
        if (label) label.textContent = 'Redirecting to Stripe…';
    });
});

const userSidebar = document.querySelector('[data-user-sidebar]');
if (userSidebar) {
    const toggle = document.querySelector('[data-user-sidebar-toggle]');
    const overlay = document.querySelector('.mc-user-sidebar-overlay');
    const setOpen = (open) => {
        userSidebar.classList.toggle('is-open', open);
        document.body.classList.toggle('mc-user-menu-open', open);
        if (overlay) overlay.hidden = !open;
        if (toggle) toggle.setAttribute('aria-expanded', String(open));
        if (open) userSidebar.querySelector('a, button')?.focus();
    };
    toggle?.addEventListener('click', () => setOpen(!userSidebar.classList.contains('is-open')));
    document.querySelectorAll('[data-user-sidebar-close], .mc-user-sidebar a').forEach((element) => element.addEventListener('click', () => setOpen(false)));
    document.addEventListener('keydown', (event) => { if (event.key === 'Escape') setOpen(false); });
}
