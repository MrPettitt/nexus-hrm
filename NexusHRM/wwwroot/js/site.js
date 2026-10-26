// ── Nexus HRM · site.js ───────────────────────────────────────────────────

// ── Modal backdrop click to close ────────────────────────────────────────
document.addEventListener('click', function (e) {
    if (e.target.classList.contains('modal-backdrop')) {
        e.target.style.display = 'none';
    }
});

// ── Escape to close modals ────────────────────────────────────────────────
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
        document.querySelectorAll('.modal-backdrop').forEach(m => m.style.display = 'none');
        closeNotifPanel();
    }
});

// ── Notification bell toggle ──────────────────────────────────────────────
const notifBtn   = document.getElementById('notifBtn');
const notifPanel = document.getElementById('notifPanel');
const notifWrap  = document.getElementById('notifWrap');

function closeNotifPanel() {
    if (notifPanel) notifPanel.style.display = 'none';
}

if (notifBtn && notifPanel) {
    notifBtn.addEventListener('click', function (e) {
        e.stopPropagation();
        const isOpen = notifPanel.style.display !== 'none';
        notifPanel.style.display = isOpen ? 'none' : 'block';
    });

    // Click outside closes panel
    document.addEventListener('click', function (e) {
        if (notifWrap && !notifWrap.contains(e.target)) {
            closeNotifPanel();
        }
    });
}

// ── Global topbar search → navigate to /Employees?q=… ────────────────────
const globalSearch = document.getElementById('globalSearch');
if (globalSearch) {
    globalSearch.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') {
            const q = this.value.trim();
            if (q.length > 0) {
                window.location.href = '/Employees?q=' + encodeURIComponent(q);
            }
        }
    });

    // Also show a small hint on focus
    globalSearch.addEventListener('focus', function () {
        this.placeholder = 'Type and press Enter';
    });
    globalSearch.addEventListener('blur', function () {
        this.placeholder = 'Search employees';
    });
}

// ── Flash messages: auto-dismiss ─────────────────────────────────────────
['successToast', 'errorToast'].forEach(function (id) {
    const t = document.getElementById(id);
    if (!t) return;
    const wait = id === 'errorToast' ? 6000 : 3500;
    setTimeout(function () { t.style.opacity = '0'; }, wait);
    setTimeout(function () { t.remove(); }, wait + 500);
});
