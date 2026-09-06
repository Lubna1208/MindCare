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
