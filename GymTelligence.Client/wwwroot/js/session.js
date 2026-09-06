window.gymSession = {
    get: () => localStorage.getItem('gymtelligence.session'),
    set: value => localStorage.setItem('gymtelligence.session', value),
    clear: () => localStorage.removeItem('gymtelligence.session'),
    scrollToBottom: id => { const el = document.getElementById(id); if (el) el.scrollTop = el.scrollHeight; },
    goTo: url => window.location.href = url
};

let pointerFrame;
document.addEventListener('pointermove', event => {
    if (pointerFrame) return;
    pointerFrame = requestAnimationFrame(() => {
        document.documentElement.style.setProperty('--cursor-x', `${event.clientX}px`);
        document.documentElement.style.setProperty('--cursor-y', `${event.clientY}px`);
        pointerFrame = undefined;
    });
}, { passive: true });

document.addEventListener('click', event => {
    const trigger = event.target.closest('button, .button');
    if (!trigger || trigger.disabled) return;
    const bounds = trigger.getBoundingClientRect();
    const ripple = document.createElement('span');
    ripple.className = 'interaction-ripple';
    ripple.style.left = `${event.clientX - bounds.left}px`;
    ripple.style.top = `${event.clientY - bounds.top}px`;
    trigger.appendChild(ripple);
    window.setTimeout(() => ripple.remove(), 650);
});
