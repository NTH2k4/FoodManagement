(function () {
    var toggle = document.getElementById('userCardToggle');
    var collapseEl = document.getElementById('userCardCollapse');
    var card = document.querySelector('.user-card');
    if (!toggle || !collapseEl || !card) return;

    toggle.addEventListener('click', function (e) {
        e.preventDefault();
        var inst = bootstrap.Collapse.getOrCreateInstance(collapseEl);
        inst.toggle();
    });

    collapseEl.addEventListener('show.bs.collapse', function () {
        card.classList.add('expanded');
        toggle.setAttribute('aria-expanded', 'true');
    });
    collapseEl.addEventListener('hide.bs.collapse', function () {
        card.classList.remove('expanded');
        toggle.setAttribute('aria-expanded', 'false');
    });

    document.addEventListener('click', function (ev) {
        if (!collapseEl.classList.contains('show')) return;
        var target = ev.target;
        if (toggle.contains(target) || collapseEl.contains(target)) return;
        var inst = bootstrap.Collapse.getInstance(collapseEl);
        if (inst) inst.hide();
    });
})();

function showConfirmLogout(evt) {
    if (evt && evt.stopPropagation) evt.stopPropagation();

    var modalEl = document.getElementById('logoutModal');
    if (!modalEl) {
        console.warn('logoutModal not found');
        return;
    }

    if (modalEl.parentNode !== document.body) {
        document.body.appendChild(modalEl);
    }

    var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    modal.show();

    setTimeout(function () {
        var backdrop = document.querySelector('.modal-backdrop');
        if (backdrop) backdrop.style.zIndex = '20000';
        modalEl.style.zIndex = '20001';
    }, 0);
}
