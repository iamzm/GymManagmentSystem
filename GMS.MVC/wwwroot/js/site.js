// Power Fitness — UI behaviour. Small, dependency-free helpers that every screen shares.
(function () {
    'use strict';

    /* --- Theme -----------------------------------------------------------
       The choice is remembered per browser and applied before paint by the
       inline snippet in the layout, so there is no flash of the wrong theme. */
    var THEME_KEY = 'pf-theme';

    function currentTheme() {
        try { return localStorage.getItem(THEME_KEY) || 'light'; } catch (e) { return 'light'; }
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        try { localStorage.setItem(THEME_KEY, theme); } catch (e) { /* private mode */ }

        document.querySelectorAll('[data-theme-toggle] i').forEach(function (icon) {
            icon.className = theme === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars';
        });
    }

    document.addEventListener('click', function (event) {
        var toggle = event.target.closest('[data-theme-toggle]');
        if (!toggle) return;
        applyTheme(currentTheme() === 'dark' ? 'light' : 'dark');
    });

    /* --- Sidebar (small screens) ----------------------------------------- */
    document.addEventListener('click', function (event) {
        var sidebar = document.querySelector('.sidebar');
        var backdrop = document.querySelector('.sidebar-backdrop');
        if (!sidebar) return;

        if (event.target.closest('[data-sidebar-toggle]')) {
            sidebar.classList.toggle('is-open');
            if (backdrop) backdrop.classList.toggle('is-open');
            return;
        }

        if (event.target.closest('.sidebar-backdrop')) {
            sidebar.classList.remove('is-open');
            if (backdrop) backdrop.classList.remove('is-open');
        }
    });

    /* --- Toasts ----------------------------------------------------------- */
    function dismissToast(toast) {
        toast.classList.add('is-leaving');
        setTimeout(function () { toast.remove(); }, 260);
    }

    document.addEventListener('click', function (event) {
        var closer = event.target.closest('.toast-app__close');
        if (closer) dismissToast(closer.closest('.toast-app'));
    });

    document.addEventListener('DOMContentLoaded', function () {
        applyTheme(currentTheme());

        document.querySelectorAll('.toast-app').forEach(function (toast, index) {
            // Stagger the auto-dismiss so stacked toasts do not all vanish at once.
            setTimeout(function () { dismissToast(toast); }, 5000 + index * 400);
        });

        initPhotoPreviews();
        initSearchForms();
        initConfirmForms();
        initDateTimeDefaults();
        initCharts();
    });

    /* --- Photo upload preview --------------------------------------------- */
    function initPhotoPreviews() {
        document.querySelectorAll('[data-photo-input]').forEach(function (input) {
            input.addEventListener('change', function () {
                var target = document.querySelector(input.getAttribute('data-photo-input'));
                if (!target || !input.files || !input.files[0]) return;

                var reader = new FileReader();
                reader.onload = function (e) {
                    target.innerHTML = '<img src="' + e.target.result + '" alt="Selected photo preview" />';
                };
                reader.readAsDataURL(input.files[0]);
            });
        });
    }

    /* --- Search boxes ------------------------------------------------------
       Submits shortly after typing stops, so filtering feels live without
       firing a request per keystroke. */
    function initSearchForms() {
        document.querySelectorAll('[data-search-form]').forEach(function (form) {
            var input = form.querySelector('input[name="search"]');
            if (!input) return;

            var timer = null;
            input.addEventListener('input', function () {
                clearTimeout(timer);
                timer = setTimeout(function () { form.submit(); }, 500);
            });
        });
    }

    /* --- Destructive actions ----------------------------------------------
       A confirm on top of the dedicated confirmation pages, for the one-click
       actions that do not have one (deactivating a plan, releasing a booking). */
    function initConfirmForms() {
        document.querySelectorAll('[data-confirm]').forEach(function (form) {
            form.addEventListener('submit', function (event) {
                if (!window.confirm(form.getAttribute('data-confirm'))) event.preventDefault();
            });
        });
    }

    /* --- Session date/time fields ------------------------------------------ */
    function initDateTimeDefaults() {
        var start = document.querySelector('input[data-session-start]');
        var end = document.querySelector('input[data-session-end]');
        if (!start || !end) return;

        function toLocalInput(date) {
            return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
        }

        var now = new Date();
        var localNow = toLocalInput(now);

        start.min = localNow;
        end.min = localNow;

        // A model-bound DateTime that was never set arrives as year 0001.
        if (!start.value || start.value.startsWith('0001')) start.value = localNow;
        if (!end.value || end.value.startsWith('0001')) end.value = toLocalInput(new Date(now.getTime() + 3600000));

        start.addEventListener('change', function () {
            end.min = start.value;
            if (end.value && end.value < start.value) end.value = '';
        });
    }

    /* --- Charts ------------------------------------------------------------
       Hand-drawn SVG rather than a charting library: two small shapes do not
       justify shipping a dependency. */
    function initCharts() {
        document.querySelectorAll('[data-bar-chart]').forEach(function (host) {
            var points;
            try { points = JSON.parse(host.getAttribute('data-bar-chart')); } catch (e) { return; }
            if (!points || !points.length) return;

            var max = Math.max.apply(null, points.map(function (p) { return p.value; }));
            var html = '<div style="display:flex;align-items:stretch;gap:8px;height:150px">';

            points.forEach(function (point) {
                // Percentages resolve against the column, which has the definite height above.
                var height = max > 0 ? Math.max((point.value / max) * 100, 3) : 3;
                html += '' +
                    '<div style="flex:1 1 0;min-width:0;height:100%;display:flex;flex-direction:column;justify-content:flex-end;gap:6px">' +
                        '<div class="fs-13 fw-600 mono-num text-muted-2 text-center">' + point.value + '</div>' +
                        '<div style="height:' + height + '%;min-height:4px;border-radius:8px 8px 4px 4px;' +
                             'background:linear-gradient(180deg,var(--brand-600),var(--brand-900))" ' +
                             'title="' + point.label + ': ' + point.value + ' bookings"></div>' +
                        '<div class="fs-13 text-subtle text-center">' + point.label + '</div>' +
                    '</div>';
            });

            host.innerHTML = html + '</div>';
        });
    }
})();
