/*
JetPack for the web: the small amount of behaviour the CSS classes need. No dependencies.
Load it in <head> without defer, so the stored accent is set before the page is drawn; the rest
waits for DOMContentLoaded and works through data attributes:

  data-jp-open="id"          opens the <dialog> (Flyout, JetPackDialog) with that id
  data-jp-close              closes the dialog it sits in
  data-jp-dismiss            hides the InfoBar it sits in
  data-jp-clear              empties the text box before it (TextBoxHelper.ClearTextButton)
  data-jp-step="1"           steps the NumericUpDown it sits in
  data-jp-toggle-pane        collapses / expands the HamburgerMenu it sits in
  data-jp-pane-switch        check box bound to "HamburgerMenu is open" (IsPaneOpen)
  data-jp-accent             radio buttons whose value is an accent name
  data-jp-focus-visuals      check box for ThemeManager.ShowFocusVisuals
  tr[data-href]              data grid row that opens a page when clicked
  <canvas class="jp-vignette">   draws the darkened window edges
  [popover].jp-context-menu  is placed under the button that opened it
*/
(function () {
  'use strict';

  const root = document.documentElement;
  const ACCENT_KEY = 'jetpack.accent';
  const FOCUS_KEY = 'jetpack.focusVisuals';
  const PANE_KEY = 'jetpack.paneOpen';

  function load(key) {
    try { return localStorage.getItem(key); } catch { return null; }
  }

  function save(key, value) {
    try { localStorage.setItem(key, value); } catch { /* private window: keep it for this page only */ }
  }

  /* ------------------------------------------------------------ ThemeManager */
  const listeners = [];
  const ThemeManager = {
    accents: ['green', 'orange', 'blue'],

    get currentAccent() {
      return root.dataset.accent || 'green';
    },

    /* Same as ThemeManager.ChangeAccent in JetPack.Wpf; remembered in this browser */
    changeAccent(name) {
      const accent = String(name).toLowerCase();
      if (!this.accents.includes(accent)) {
        throw new Error('Unknown JetPack accent: ' + name);
      }
      root.dataset.accent = accent;
      save(ACCENT_KEY, accent);
      listeners.forEach(fn => fn(accent));
    },

    onAccentChanged(fn) {
      listeners.push(fn);
    },

    get showFocusVisuals() {
      return root.dataset.focus !== 'off';
    },

    set showFocusVisuals(value) {
      if (value) {
        delete root.dataset.focus;
      } else {
        root.dataset.focus = 'off';
      }
      save(FOCUS_KEY, value ? 'on' : 'off');
    },
  };

  /* Before the first paint: a page may set data-accent itself; the stored choice wins */
  const storedAccent = load(ACCENT_KEY);
  if (storedAccent && ThemeManager.accents.includes(storedAccent)) {
    root.dataset.accent = storedAccent;
  }
  if (load(FOCUS_KEY) === 'off') {
    root.dataset.focus = 'off';
  }

  /* ---------------------------------------------------------------- vignette */
  /* Port of Controls/Vignette.cs: a shade of fixed width along each edge, joined at the
     corners with a p-norm, dithered so the gradient does not band. */
  const CURVE = 1.9;
  const CORNER_NORM = 1.6;

  function noise(x, y) {
    let h = (Math.imul(x, 73856093) ^ Math.imul(y, 19349663)) >>> 0;
    h ^= h >>> 13;
    h = Math.imul(h, 0x5bd1e995) >>> 0;
    h ^= h >>> 15;
    return (h & 0xFFFF) / 65536;
  }

  function shades(length, reach, strength) {
    const out = new Float64Array(length);
    for (let i = 0; i < length; i++) {
      const t = 1 - Math.min(i + 0.5, length - i - 0.5) / reach;
      out[i] = t <= 0 ? 0 : strength * Math.pow(t, CURVE);
    }
    return out;
  }

  function drawVignette(canvas) {
    const dpr = window.devicePixelRatio || 1;
    const w = Math.round(canvas.clientWidth * dpr);
    const h = Math.round(canvas.clientHeight * dpr);
    if (w === 0 || h === 0) {
      return;
    }
    const reach = Math.round((Number(canvas.dataset.reach) || 180) * dpr);
    const strength = canvas.dataset.strength ? Number(canvas.dataset.strength) : 0.3;
    canvas.width = w;
    canvas.height = h;
    const context = canvas.getContext('2d');
    const image = context.createImageData(w, h);
    const data = image.data;
    const sx = shades(w, reach, strength);
    const sy = shades(h, reach, strength);
    const left = Math.min(reach, w);
    const right = Math.max(w - reach, left);

    function pixel(x, y) {
      const a = sx[x], b = sy[y];
      const alpha = a === 0 ? b : b === 0 ? a
        : Math.pow(Math.pow(a, CORNER_NORM) + Math.pow(b, CORNER_NORM), 1 / CORNER_NORM);
      /* black; noise below one step before rounding down dithers the gradient */
      data[(y * w + x) * 4 + 3] = Math.max(0, Math.min(255, Math.floor(Math.min(alpha, 1) * 255 + noise(x, y))));
    }

    for (let y = 0; y < h; y++) {
      if (sy[y] > 0) {
        for (let x = 0; x < w; x++) pixel(x, y);
      } else {
        for (let x = 0; x < left; x++) pixel(x, y);
        for (let x = right; x < w; x++) pixel(x, y);
      }
    }
    context.putImageData(image, 0, 0);
  }

  function setUpVignettes() {
    const canvases = document.querySelectorAll('canvas.jp-vignette');
    if (canvases.length === 0) {
      return;
    }
    let pending = 0;
    const redraw = () => {
      cancelAnimationFrame(pending);
      pending = requestAnimationFrame(() => canvases.forEach(drawVignette));
    };
    const observer = new ResizeObserver(redraw);
    canvases.forEach(c => observer.observe(c));
    /* a new device pixel ratio (zoom, another screen) does not resize the element */
    window.addEventListener('resize', redraw);
  }

  /* ------------------------------------------------------------- popovers */
  /* Places a context menu under (or above) the button that opened it */
  let lastInvoker = null;

  function placePopover(popover) {
    const invoker = lastInvoker && lastInvoker.getAttribute('popovertarget') === popover.id
      ? lastInvoker
      : document.querySelector(`[popovertarget="${CSS.escape(popover.id)}"]`);
    if (!invoker) {
      return;
    }
    const anchor = (invoker.closest('.jp-split-button') || invoker).getBoundingClientRect();
    const menu = popover.getBoundingClientRect();
    let top = anchor.bottom + 1;
    if (top + menu.height > window.innerHeight && anchor.top - menu.height - 1 > 0) {
      top = anchor.top - menu.height - 1;
    }
    const alignRight = popover.dataset.jpAlign === 'right';
    let left = alignRight ? anchor.right - menu.width : anchor.left;
    left = Math.max(4, Math.min(left, window.innerWidth - menu.width - 4));
    popover.style.inset = 'auto';
    popover.style.top = top + 'px';
    popover.style.left = left + 'px';
  }

  /* ------------------------------------------------------------- behaviour */
  function closestIn(event, selector) {
    return event.target instanceof Element ? event.target.closest(selector) : null;
  }

  document.addEventListener('click', event => {
    let el;
    if ((el = closestIn(event, '[popovertarget]'))) {
      lastInvoker = el;
    }
    if ((el = closestIn(event, '[data-jp-open]'))) {
      const dialog = document.getElementById(el.dataset.jpOpen);
      if (dialog && !dialog.open) {
        dialog.showModal();
      }
      event.preventDefault();
      return;
    }
    if ((el = closestIn(event, '[data-jp-close]'))) {
      el.closest('dialog')?.close(el.value || undefined);
      return;
    }
    if ((el = closestIn(event, '[data-jp-dismiss]'))) {
      const bar = el.closest('.jp-info-bar');
      if (bar) {
        bar.hidden = true;
      }
      return;
    }
    if ((el = closestIn(event, '[data-jp-clear]'))) {
      const box = el.parentElement.querySelector('input, textarea');
      if (box) {
        box.value = '';
        box.dispatchEvent(new Event('input', { bubbles: true }));
        box.dispatchEvent(new Event('change', { bubbles: true }));
        box.focus();
      }
      return;
    }
    if ((el = closestIn(event, '[data-jp-step]'))) {
      const box = el.closest('.jp-numeric-up-down')?.querySelector('input');
      if (box && !box.disabled && !box.readOnly) {
        if (Number(el.dataset.jpStep) > 0) box.stepUp(); else box.stepDown();
        box.dispatchEvent(new Event('input', { bubbles: true }));
        box.dispatchEvent(new Event('change', { bubbles: true }));
      }
      return;
    }
    if ((el = closestIn(event, '[data-jp-toggle-pane]'))) {
      const menu = el.closest('.jp-hamburger-menu');
      if (menu) {
        setPaneOpen(menu.classList.contains('is-collapsed'));
      }
      return;
    }
    if ((el = closestIn(event, '.jp-tab-item[aria-controls]'))) {
      selectTab(el);
      return;
    }
    if ((el = closestIn(event, 'tr[data-href]')) && !closestIn(event, 'a, button, input, select, label')) {
      if (event.ctrlKey || event.metaKey) {
        window.open(el.dataset.href);
      } else {
        location.href = el.dataset.href;
      }
    }
  });

  /* A click on the dimmed area beside a Flyout closes it */
  document.addEventListener('click', event => {
    const dialog = event.target;
    if (dialog instanceof HTMLDialogElement && dialog.classList.contains('jp-flyout')) {
      const r = dialog.getBoundingClientRect();
      if (event.clientX < r.left || event.clientX > r.right || event.clientY < r.top || event.clientY > r.bottom) {
        dialog.close();
      }
    }
  });

  document.addEventListener('change', event => {
    const el = event.target;
    if (el.matches('[data-jp-accent]') && el.checked) {
      ThemeManager.changeAccent(el.value);
    } else if (el.matches('[data-jp-focus-visuals]')) {
      ThemeManager.showFocusVisuals = el.checked;
    } else if (el.matches('[data-jp-pane-switch]')) {
      setPaneOpen(el.checked);
    }
  });

  document.addEventListener('toggle', event => {
    const popover = event.target;
    if (event.newState === 'open' && popover.classList?.contains('jp-context-menu')) {
      placePopover(popover);
    }
  }, true);

  /* Arrow keys inside an open menu */
  document.addEventListener('keydown', event => {
    const menu = closestIn(event, '.jp-context-menu');
    if (!menu || (event.key !== 'ArrowDown' && event.key !== 'ArrowUp')) {
      return;
    }
    const items = [...menu.querySelectorAll('.jp-menu-item:not(:disabled)')];
    const index = items.indexOf(document.activeElement);
    const next = event.key === 'ArrowDown' ? index + 1 : index - 1;
    items[(next + items.length) % items.length]?.focus();
    event.preventDefault();
  });

  function setPaneOpen(open) {
    document.querySelectorAll('.jp-hamburger-menu').forEach(m => m.classList.toggle('is-collapsed', !open));
    document.querySelectorAll('[data-jp-pane-switch]').forEach(s => { s.checked = open; });
    document.querySelectorAll('[data-jp-toggle-pane]').forEach(b => b.setAttribute('aria-expanded', String(open)));
    save(PANE_KEY, open ? 'open' : 'closed');
  }

  function selectTab(tab) {
    const strip = tab.closest('[role="tablist"]');
    strip.querySelectorAll('.jp-tab-item[aria-controls]').forEach(t => {
      const selected = t === tab;
      t.setAttribute('aria-selected', String(selected));
      t.tabIndex = selected ? 0 : -1;
      const panel = document.getElementById(t.getAttribute('aria-controls'));
      if (panel) {
        panel.hidden = !selected;
      }
    });
  }

  function syncInputs() {
    document.querySelectorAll('[data-jp-accent]').forEach(r => { r.checked = r.value === ThemeManager.currentAccent; });
    document.querySelectorAll('[data-jp-focus-visuals]').forEach(c => { c.checked = ThemeManager.showFocusVisuals; });
    /* the stored choice, else collapsed on narrow screens */
    const pane = load(PANE_KEY);
    if (pane) {
      setPaneOpen(pane === 'open');
    } else if (window.matchMedia('(max-width: 640px)').matches && document.querySelector('.jp-hamburger-menu')) {
      document.querySelectorAll('.jp-hamburger-menu').forEach(m => m.classList.add('is-collapsed'));
      document.querySelectorAll('[data-jp-pane-switch]').forEach(s => { s.checked = false; });
    }
  }

  document.addEventListener('DOMContentLoaded', () => {
    syncInputs();
    setUpVignettes();
  });

  window.JetPack = { ThemeManager };
})();
