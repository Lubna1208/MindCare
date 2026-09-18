(() => {
    const root = document.querySelector('[data-resource-ai]');
    if (!root) return;

    const button = root.querySelector('[data-resource-ai-button]');
    const label = root.querySelector('[data-resource-ai-label]');
    const spinner = root.querySelector('[data-resource-ai-spinner]');
    const status = root.querySelector('[data-resource-ai-status]');
    const card = root.querySelector('[data-resource-ai-card]');
    const summary = root.querySelector('[data-resource-ai-summary]');
    const points = root.querySelector('[data-resource-ai-points]');
    const token = root.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const endpoint = root.dataset.summaryEndpoint;
    let hasSummary = false;

    const setStatus = (message, isError = false) => {
        status.textContent = message;
        status.classList.toggle('is-error', isError);
    };
    const setLoading = (loading) => {
        button.disabled = loading;
        button.setAttribute('aria-busy', String(loading));
        if (spinner) spinner.hidden = !loading;
        label.textContent = loading ? 'Generating Summary...' : hasSummary ? 'AI Summary Ready' : 'Summarize with AI';
    };
    const createPointIcon = () => {
        const icon = document.createElement('span');
        icon.className = 'mc-resource-ai-point-icon';
        icon.setAttribute('aria-hidden', 'true');
        const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        svg.setAttribute('viewBox', '0 0 24 24');
        const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
        path.setAttribute('d', 'm5 12 4 4L19 6');
        svg.append(path);
        icon.append(svg);
        return icon;
    };
    const safeErrorMessage = async (response) => {
        if (response.status === 404) return 'This resource could not be found.';
        if (response.status === 401 || response.status === 403) return 'Your session may have expired. Please sign in again.';
        if (response.status === 429) return "You've generated several summaries recently. Please wait a moment and try again.";
        if (response.status === 400) {
            try { const body = await response.json(); return typeof body.message === 'string' ? body.message : 'This resource cannot be summarized right now.'; }
            catch { return 'This resource cannot be summarized right now.'; }
        }
        return 'AI summary is temporarily unavailable. Please try again.';
    };
    const revealCard = () => {
        card.hidden = false;
        card.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    };

    button.addEventListener('click', async () => {
        if (hasSummary) { revealCard(); return; }
        if (!endpoint) { setStatus('AI summary is temporarily unavailable. Please try again.', true); return; }

        setLoading(true);
        setStatus('');
        try {
            const response = await fetch(endpoint, {
                method: 'POST',
                credentials: 'same-origin',
                headers: { RequestVerificationToken: token || '' }
            });
            if (!response.ok) { setStatus(await safeErrorMessage(response), true); return; }

            const body = await response.json();
            if (!body.success || typeof body.summary !== 'string' || !Array.isArray(body.keyPoints)) {
                setStatus('AI summary is temporarily unavailable. Please try again.', true);
                return;
            }

            const keyPoints = body.keyPoints.filter((point) => typeof point === 'string' && point.trim()).slice(0, 5);
            if (!body.summary.trim() || keyPoints.length < 3) {
                setStatus('AI summary is temporarily unavailable. Please try again.', true);
                return;
            }

            summary.textContent = body.summary;
            points.replaceChildren();
            keyPoints.forEach((point) => {
                const item = document.createElement('li');
                item.append(createPointIcon(), document.createTextNode(point));
                points.append(item);
            });
            hasSummary = true;
            setStatus('');
            revealCard();
        } catch {
            setStatus('AI summary is temporarily unavailable. Please try again.', true);
        } finally {
            setLoading(false);
        }
    });
})();
