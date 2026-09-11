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
        initTables();
        initPendingForms();
        initItemKind();
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
    /* --- Tables ----------------------------------------------------------
       Sorting, paging and the mobile card layout are all done here rather than
       in each view, so a table gets them by being a .table-app and nothing has
       to be repeated across the ten list screens. Nothing here talks to the
       server: sorting and paging act on the rows already rendered. */

    var PAGE_SIZE = 12;

    // The text a cell should sort by is not always all of its text. An identity cell
    // leads with an avatar whose initials are derived from the name, and sorting on
    // those puts "Bilal Ahmed" (BA) ahead of "Ben Zhang" (BZ). Prefer the name node
    // when there is one, and otherwise drop anything purely decorative. Computed
    // once per cell rather than on every comparison.
    function sortText(cell) {
        if (cell.getAttribute('data-sort-text') !== null) return cell.getAttribute('data-sort-text');

        var primary = cell.querySelector('.identity__name');
        var text;
        if (primary) {
            text = primary.textContent || '';
        } else {
            var clone = cell.cloneNode(true);
            clone.querySelectorAll('.avatar-app, [aria-hidden="true"]').forEach(function (node) {
                node.parentNode.removeChild(node);
            });
            text = clone.textContent || '';
        }
        text = text.replace(/\s+/g, ' ').trim();
        cell.setAttribute('data-sort-text', text);
        return text;
    }

    // "1,250 PKR" and "Sep 28, 2026" have to sort as a number and a date, not as
    // text. Anything we cannot read confidently falls back to a string compare.
    function sortValue(cell) {
        var text = sortText(cell);
        if (!text) return { type: 'empty', value: '' };

        var numeric = text.replace(/[^0-9.\-]/g, '');
        if (numeric && /[0-9]/.test(numeric) && /^[^A-Za-z]*$/.test(text.replace(/[A-Za-z]{2,}/g, ''))) {
            var asNumber = parseFloat(numeric);
            if (!isNaN(asNumber) && /^[\s\S]{0,40}$/.test(text)) {
                var asDate = Date.parse(text);
                if (!isNaN(asDate) && /[A-Za-z]/.test(text)) return { type: 'number', value: asDate };
                return { type: 'number', value: asNumber };
            }
        }
        var parsed = Date.parse(text);
        if (!isNaN(parsed) && /\d/.test(text) && /[A-Za-z]{3}/.test(text)) return { type: 'number', value: parsed };
        return { type: 'text', value: text.toLowerCase() };
    }

    function compareRows(a, b, index) {
        var av = sortValue(a.cells[index]);
        var bv = sortValue(b.cells[index]);
        // Blanks sort last in both directions, so an empty cell never looks like a zero.
        if (av.type === 'empty' && bv.type === 'empty') return 0;
        if (av.type === 'empty') return 1;
        if (bv.type === 'empty') return -1;
        if (av.type === 'number' && bv.type === 'number') return av.value - bv.value;
        return String(av.value).localeCompare(String(bv.value), undefined, { numeric: true });
    }

    function initTables() {
        document.querySelectorAll('.table-app').forEach(function (table) {
            var head = table.tHead;
            var body = table.tBodies[0];
            if (!head || !body || !head.rows.length) return;

            var headers = Array.prototype.slice.call(head.rows[0].cells);
            var rows = Array.prototype.slice.call(body.rows);
            if (!rows.length) return;

            // Stack each row into a labelled card under 768px. The label comes from
            // the header, so the view markup does not carry a duplicate of it.
            headers.forEach(function (th, index) {
                var label = (th.textContent || '').trim();
                if (!label) return;
                rows.forEach(function (row) {
                    var cell = row.cells[index];
                    if (!cell || cell.hasAttribute('data-label')) return;
                    cell.setAttribute('data-label', label);

                    // The card layout lays the label and the value out side by side. Without
                    // a wrapper each of the cell's own children becomes a sibling of the
                    // label instead, so an email and the phone under it end up on one line
                    // and both get truncated. The wrapper is display:contents on desktop,
                    // so it changes nothing there.
                    if (cell.firstChild && !cell.querySelector(':scope > .cell-body')) {
                        var body = document.createElement('div');
                        body.className = 'cell-body';
                        while (cell.firstChild) body.appendChild(cell.firstChild);
                        cell.appendChild(body);
                    }
                });
            });

            var state = { index: -1, dir: 1, page: 0 };
            var pager = rows.length > PAGE_SIZE ? buildPager(table, body, rows, state) : null;

            headers.forEach(function (th, index) {
                // The actions column holds buttons, not data, so it is not sortable.
                if (th.classList.contains('col-actions')) return;
                var label = (th.textContent || '').trim();
                if (!label) return;

                th.classList.add('is-sortable');
                th.setAttribute('aria-sort', 'none');
                // A real button, so the column is reachable and operable by keyboard
                // and announced as a control rather than as plain header text.
                var button = document.createElement('button');
                button.type = 'button';
                button.className = 'table-sort';
                button.innerHTML = '<span>' + label + '</span><i class="bi bi-arrow-down-up" aria-hidden="true"></i>';
                th.textContent = '';
                th.appendChild(button);

                button.addEventListener('click', function () {
                    state.dir = state.index === index ? -state.dir : 1;
                    state.index = index;

                    headers.forEach(function (other) {
                        if (other === th) return;
                        if (other.getAttribute('aria-sort')) other.setAttribute('aria-sort', 'none');
                        var icon = other.querySelector('.table-sort i');
                        if (icon) icon.className = 'bi bi-arrow-down-up';
                    });
                    th.setAttribute('aria-sort', state.dir === 1 ? 'ascending' : 'descending');
                    button.querySelector('i').className = state.dir === 1 ? 'bi bi-arrow-up' : 'bi bi-arrow-down';

                    rows.sort(function (a, b) { return compareRows(a, b, index) * state.dir; });
                    rows.forEach(function (row) { body.appendChild(row); });
                    state.page = 0;
                    if (pager) pager.render();
                });
            });
        });
    }

    function buildPager(table, body, rows, state) {
        var wrap = table.closest('.table-app-wrap') || table.parentNode;
        var bar = document.createElement('div');
        bar.className = 'table-pager';
        var status = document.createElement('p');
        status.className = 'table-pager__status';
        // Page changes move no focus, so the count is announced instead.
        status.setAttribute('aria-live', 'polite');
        var nav = document.createElement('div');
        nav.className = 'table-pager__nav';
        var prev = document.createElement('button');
        prev.type = 'button';
        prev.className = 'btn-app btn-app--ghost btn-app--sm';
        prev.innerHTML = '<i class="bi bi-chevron-left" aria-hidden="true"></i> Previous';
        var next = document.createElement('button');
        next.type = 'button';
        next.className = 'btn-app btn-app--ghost btn-app--sm';
        next.innerHTML = 'Next <i class="bi bi-chevron-right" aria-hidden="true"></i>';
        nav.appendChild(prev);
        nav.appendChild(next);
        bar.appendChild(status);
        bar.appendChild(nav);
        wrap.appendChild(bar);

        var pages = Math.ceil(rows.length / PAGE_SIZE);

        function render() {
            if (state.page > pages - 1) state.page = pages - 1;
            if (state.page < 0) state.page = 0;
            var from = state.page * PAGE_SIZE;
            var to = Math.min(from + PAGE_SIZE, rows.length);
            rows.forEach(function (row, i) { row.hidden = i < from || i >= to; });
            status.textContent = 'Showing ' + (from + 1) + '–' + to + ' of ' + rows.length;
            prev.disabled = state.page === 0;
            next.disabled = state.page >= pages - 1;
        }

        prev.addEventListener('click', function () { state.page -= 1; render(); });
        next.addEventListener('click', function () { state.page += 1; render(); });
        render();
        return { render: render };
    }

    /* --- Form submit state -----------------------------------------------
       Every form could be submitted twice by double-clicking, which on a create
       screen means two records. The button is disabled on the first submit and
       says so, which also covers the gap on a slow connection where nothing
       otherwise indicates the click registered. */
    function initPendingForms() {
        document.querySelectorAll('form').forEach(function (form) {
            form.addEventListener('submit', function () {
                // A form that failed client-side validation has not really submitted.
                if (form.getAttribute('novalidate') === null && !form.checkValidity()) return;
                if (window.jQuery && jQuery(form).data('validator') && !jQuery(form).valid()) return;

                var button = form.querySelector('button[type=submit], input[type=submit]');
                if (!button || button.disabled) return;

                window.setTimeout(function () {
                    button.disabled = true;
                    button.classList.add('is-pending');
                    var label = button.getAttribute('data-pending-label');
                    if (label && button.tagName === 'BUTTON') {
                        button.innerHTML = '<i class="bi bi-arrow-repeat" aria-hidden="true"></i> ' + label;
                    }
                }, 0);
            });
        });
    }
    /* --- Inventory item kind ---------------------------------------------
       Equipment and stock share a form but not their fields. Only the block
       belonging to the chosen kind is shown; both stay in the DOM so a server-side
       validation message on a hidden field is still there when its block returns. */
    function initItemKind() {
        var select = document.querySelector('[data-item-kind]');
        if (!select) return;

        var blocks = document.querySelectorAll('[data-kind-block]');
        if (!blocks.length) return;

        function apply() {
            // Equipment is 1 and Consumable is 2 in the ItemKind enum.
            var wanted = select.value === '2' ? 'consumable' : 'equipment';
            blocks.forEach(function (block) {
                block.hidden = block.getAttribute('data-kind-block') !== wanted;
            });
        }

        select.addEventListener('change', apply);
        apply();
    }
})();
