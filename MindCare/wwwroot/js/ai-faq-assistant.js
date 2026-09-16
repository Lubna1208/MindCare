(() => {
    const root = document.querySelector('[data-ai-faq]');
    if (!root) return;

    const launcher = root.querySelector('[data-ai-faq-open]');
    const panel = root.querySelector('[data-ai-faq-panel]');
    const closeButton = root.querySelector('[data-ai-faq-close]');
    const form = root.querySelector('[data-ai-faq-form]');
    const input = root.querySelector('[data-ai-faq-input]');
    const messages = root.querySelector('[data-ai-faq-messages]');
    const status = root.querySelector('[data-ai-faq-status]');
    const counter = root.querySelector('[data-ai-faq-count]');
    const sendButton = root.querySelector('[data-ai-faq-send]');
    const token = root.querySelector('input[name="__RequestVerificationToken"]')?.value;
    let isPending = false;

    const scrollMessages = () => { messages.scrollTop = messages.scrollHeight; };
    const resizeInput = () => { input.style.height = 'auto'; input.style.height = `${Math.min(input.scrollHeight, 100)}px`; };
    const updateCounter = () => { counter.textContent = `${input.value.length} / 1000`; };
    const setOpen = (open) => {
        panel.hidden = !open;
        root.classList.toggle('is-open', open);
        launcher.setAttribute('aria-expanded', String(open));
        if (open) { window.setTimeout(() => input.focus(), 0); scrollMessages(); }
        else launcher.focus();
    };
    const appendMessage = (text, type) => {
        const message = document.createElement('div');
        message.className = `mc-ai-faq-message mc-ai-faq-message--${type}`;
        message.textContent = text;
        messages.append(message);
        scrollMessages();
    };
    const appendTyping = () => {
        const typing = document.createElement('div');
        typing.className = 'mc-ai-faq-typing';
        typing.setAttribute('aria-label', 'MindCare Assistant is thinking');
        typing.append(document.createElement('span'), document.createElement('span'), document.createElement('span'));
        messages.append(typing);
        scrollMessages();
        return typing;
    };
    const responseError = async (response) => {
        if (response.status === 401 || response.status === 403) return 'Your session may have expired. Please sign in again.';
        if (response.status === 429) return "You've asked several questions recently. Please wait a moment and try again.";
        if (response.status === 400) {
            try { const body = await response.json(); return typeof body.message === 'string' ? body.message : 'Please check your question and try again.'; }
            catch { return 'Please check your question and try again.'; }
        }
        return 'MindCare Assistant is temporarily unavailable. Please try again.';
    };
    const ask = async (question) => {
        const trimmedQuestion = question.trim();
        if (!trimmedQuestion) { status.textContent = 'Please enter a question.'; input.focus(); return; }
        if (trimmedQuestion.length > 1000) { status.textContent = 'Questions can be up to 1,000 characters.'; input.focus(); return; }
        if (isPending) return;

        isPending = true;
        sendButton.disabled = true;
        status.textContent = '';
        appendMessage(trimmedQuestion, 'user');
        input.value = '';
        updateCounter();
        resizeInput();
        const typing = appendTyping();

        try {
            const response = await fetch('/AI/Faq/Ask', {
                method: 'POST', credentials: 'same-origin',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token || '' },
                body: JSON.stringify({ question: trimmedQuestion })
            });
            if (!response.ok) { appendMessage(await responseError(response), 'error'); return; }
            const body = await response.json();
            if (body.success && typeof body.answer === 'string' && body.answer.trim()) appendMessage(body.answer, 'assistant');
            else appendMessage('MindCare Assistant is temporarily unavailable. Please try again.', 'error');
        } catch {
            appendMessage('MindCare Assistant is temporarily unavailable. Please try again.', 'error');
        } finally {
            typing.remove();
            isPending = false;
            sendButton.disabled = false;
            input.focus();
        }
    };

    launcher.addEventListener('click', () => setOpen(true));
    closeButton.addEventListener('click', () => setOpen(false));
    document.addEventListener('keydown', (event) => { if (event.key === 'Escape' && !panel.hidden) setOpen(false); });
    input.addEventListener('input', () => { status.textContent = ''; updateCounter(); resizeInput(); });
    input.addEventListener('keydown', (event) => { if (event.key === 'Enter' && !event.shiftKey) { event.preventDefault(); form.requestSubmit(); } });
    form.addEventListener('submit', (event) => { event.preventDefault(); ask(input.value); });
    root.querySelectorAll('[data-ai-faq-suggestion]').forEach((button) => button.addEventListener('click', () => ask(button.textContent || '')));
    updateCounter();
    resizeInput();
})();
