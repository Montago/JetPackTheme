/*
JetPack for the web: the behaviour the CSS classes need, in plain JavaScript without dependencies.
Load it in <head> without defer, so the stored accent is set before the page is drawn; everything
else starts on DOMContentLoaded and works from classes and data attributes. JetPack.init(element)
sets up controls added later.

  ThemeManager      JetPack.ThemeManager.changeAccent('orange' | '#75A20A'), .controlFontSize, .showFocusVisuals
  Dialogs           JetPack.showMessage(title, message), JetPack.showDialog({ title, content, … }) → Promise
  Flyouts           data-jp-open="id", data-jp-close, data-jp-auto-close="ms"
  InfoBar           data-jp-dismiss
  TextBoxHelper     data-jp-clear (ClearTextButton)
  NumericUpDown     data-jp-step="1" / "-1"
  HamburgerMenu     data-jp-toggle-pane, data-jp-pane-switch
  Accents           data-jp-accent (radio), data-jp-accent-button="green", data-jp-focus-visuals (check box)
  ToolTip           data-jp-tool-tip="text"
  Validation        aria-invalid="true" + data-jp-error="message" shows the red call-out
  ContextMenu       data-jp-context-menu="id" opens that menu on right-click
  Buttons           data-jp-repeat (RepeatButton), data-jp-toggle (ToggleButton)
  Controls          select.jp-combo-box, .jp-combo-box--editable, .jp-list-box, .jp-list-view, .jp-tree-view,
                    .jp-data-grid[data-jp-selection], .jp-tab-control, .jp-slider, .jp-range-slider,
                    .jp-rating[data-jp-input], .jp-calendar, .jp-date-picker, .jp-time-picker,
                    .jp-auto-suggest-box, .jp-search-combo-box, .jp-color-picker, .jp-grid-splitter,
                    .jp-scroll-viewer--thin, .jp-menu, canvas.jp-vignette
  DataGrid          table.jp-data-grid: header click sorts (Shift adds a column), header edge resizes;
                    data-jp-sort="false", data-jp-resize="false", data-sort-value on a cell
  tr[data-href]     a data grid row that opens a page when clicked

Controls raise the usual input and change events; value controls (slider, pickers, combo boxes)
also raise "jp-change" with the new value in event.detail. A data grid raises "jp-sorting"
(cancelable, before it sorts) and "jp-sorted".
*/
(function () {
  'use strict';

  const root = document.documentElement;
  const ACCENT_KEY = 'jetpack.accent';
  const FOCUS_KEY = 'jetpack.focusVisuals';
  const PANE_KEY = 'jetpack.paneOpen';
  const FONT_KEY = 'jetpack.controlFontSize';

  function load(key) {
    try { return localStorage.getItem(key); } catch { return null; }
  }

  function save(key, value) {
    try { localStorage.setItem(key, value); } catch { /* private window: keep it for this page only */ }
  }

  function fire(el, name, detail) {
    el.dispatchEvent(new CustomEvent(name, { bubbles: true, detail }));
  }

  function notify(input) {
    input.dispatchEvent(new Event('input', { bubbles: true }));
    input.dispatchEvent(new Event('change', { bubbles: true }));
  }

  const clamp = (v, lo, hi) => Math.min(hi, Math.max(lo, v));

  /* ======================================================== AccentGenerator */
  /* Port of Theming/AccentGenerator.cs: a whole accent from one colour, with the lightness and
     saturation offsets measured from the original green. */
  function toHsl(hex) {
    const n = parseInt(hex.slice(1), 16);
    const r = (n >> 16 & 255) / 255, g = (n >> 8 & 255) / 255, b = (n & 255) / 255;
    const max = Math.max(r, g, b), min = Math.min(r, g, b);
    let h = 0, s = 0;
    const l = (max + min) / 2;
    if (max !== min) {
      const d = max - min;
      s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
      if (max === r) h = (g - b) / d + (g < b ? 6 : 0);
      else if (max === g) h = (b - r) / d + 2;
      else h = (r - g) / d + 4;
      h /= 6;
    }
    return [h, s, l];
  }

  function fromHsl(h, s, l) {
    let r, g, b;
    if (s === 0) {
      r = g = b = l;
    } else {
      const hue = (p, q, t) => {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1 / 6) return p + (q - p) * 6 * t;
        if (t < 0.5) return q;
        if (t < 2 / 3) return p + (q - p) * (2 / 3 - t) * 6;
        return p;
      };
      const q = l < 0.5 ? l * (1 + s) : l + s - l * s, p = 2 * l - q;
      r = hue(p, q, h + 1 / 3);
      g = hue(p, q, h);
      b = hue(p, q, h - 1 / 3);
    }
    return '#' + [r, g, b].map(v => Math.round(v * 255).toString(16).padStart(2, '0')).join('').toUpperCase();
  }

  function accentFromColor(solid) {
    const [h, s, l] = toHsl(solid);
    const c01 = v => clamp(v, 0, 1);
    const shade = (dl, sMul = 1, fixedS) => fromHsl(h, c01(fixedS ?? s * sMul), c01(l + dl));
    const v = (...stops) => `linear-gradient(${stops.map(([c, o]) => `${c} ${o * 100}%`).join(', ')})`;
    const highlight = shade(0.10, 1, Math.max(s, 0.85));
    const shadow = fromHsl(h, c01(s), 0.12);
    const rgb = parseInt(shadow.slice(1), 16);
    const shadowCss = `0 -1px 0 rgb(${rgb >> 16 & 255} ${rgb >> 8 & 255} ${rgb & 255} / 0.5)`;
    return {
      '--jp-color-accent': solid,
      '--jp-color-accent-highlight': highlight,
      '--jp-color-accent-text-shadow': shadow,
      '--jp-accent-selection': v([shade(0.06, 0.92), 0], [solid, 1]),
      '--jp-accent': v([shade(0.21, 0.74), 0], [shade(0.02, 1.05), 1]),
      '--jp-accent-mouse-over': v([shade(0.05), 0], [shade(0.22, 0.8), 1]),
      '--jp-accent-pressed': v([shade(-0.06), 0.31], [shade(-0.09), 0.78]),
      '--jp-accent-highlight': highlight,
      '--jp-accent-solid': solid,
      '--jp-accent-link': shade(0.16, 1, 1),
      '--jp-accent-foreground': '#FFFFFF',
      '--jp-accent-button-normal': v([shade(0.05, 0.9), 0.176], [shade(-0.04), 0.894]),
      '--jp-accent-button-mouse-over': v([shade(0.07, 0.95), 0.228], [shade(0.01), 0.849]),
      '--jp-accent-button-pressed': v([shade(-0.06), 0.31], [shade(-0.09), 0.78]),
      '--jp-accent-button-highlight': shade(0.14, 0.7),
      '--jp-effect-accent-text-shadow': shadowCss,
      '--jp-focus': highlight,
      '--jp-focus-border': solid,
    };
  }

  /* =========================================================== ThemeManager */
  const accentListeners = [];
  let customAccentProps = [];

  const ThemeManager = {
    accents: ['green', 'orange', 'blue'],

    /* the accent name, or the colour of a generated accent */
    get currentAccent() {
      return root.dataset.accent || 'green';
    },

    /* Same as ThemeManager.ChangeAccent in JetPack.Wpf: a name or a colour; remembered in this browser */
    changeAccent(nameOrColor) {
      const value = String(nameOrColor).trim();
      customAccentProps.forEach(p => root.style.removeProperty(p));
      customAccentProps = [];
      if (/^#[0-9a-f]{6}$/i.test(value)) {
        const props = accentFromColor(value.toUpperCase());
        Object.entries(props).forEach(([k, v]) => root.style.setProperty(k, v));
        customAccentProps = Object.keys(props);
        root.dataset.accent = value.toUpperCase();
      } else {
        const accent = value.toLowerCase();
        if (!this.accents.includes(accent)) {
          throw new Error('Unknown JetPack accent: ' + nameOrColor);
        }
        root.dataset.accent = accent;
      }
      save(ACCENT_KEY, root.dataset.accent);
      accentListeners.forEach(fn => fn(root.dataset.accent));
      fire(document, 'jp-accent-changed', root.dataset.accent);
    },

    onAccentChanged(fn) {
      accentListeners.push(fn);
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

    /* ThemeManager.ControlFontSize: 12 (WPF) or 11 (the original) */
    get controlFontSize() {
      return parseFloat(getComputedStyle(root).getPropertyValue('--jp-font-size-control')) || 12;
    },

    set controlFontSize(px) {
      root.style.setProperty('--jp-font-size-control', px + 'px');
      save(FONT_KEY, String(px));
    },
  };

  /* Before the first paint: the stored choices win over the page's data-accent */
  const storedAccent = load(ACCENT_KEY);
  if (storedAccent) {
    try { ThemeManager.changeAccent(storedAccent); } catch { /* an old value */ }
  }
  if (load(FOCUS_KEY) === 'off') {
    root.dataset.focus = 'off';
  }
  if (load(FONT_KEY)) {
    root.style.setProperty('--jp-font-size-control', load(FONT_KEY) + 'px');
  }

  /* =============================================================== vignette */
  /* Port of Controls/Vignette.cs: a shade of fixed width along each edge, joined at the corners
     with a p-norm, dithered so the gradient does not band. */
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

  let vignetteObserver = null;
  const vignettes = new Set();
  function setUpVignette(canvas) {
    let pending = 0;
    if (!vignetteObserver) {
      const redraw = () => {
        cancelAnimationFrame(pending);
        pending = requestAnimationFrame(() => vignettes.forEach(drawVignette));
      };
      vignetteObserver = new ResizeObserver(redraw);
      /* a new device pixel ratio (zoom, another screen) does not resize the element */
      window.addEventListener('resize', redraw);
    }
    vignettes.add(canvas);
    vignetteObserver.observe(canvas);
  }

  /* ================================================================= popups */
  /* Places a popover element next to an anchor: under it (and as wide), or at its right for a
     submenu; flips when there is no room. */
  function place(popup, anchor, options = {}) {
    const a = anchor instanceof DOMRect ? anchor : anchor.getBoundingClientRect();
    if (options.matchWidth) {
      popup.style.minWidth = Math.round(a.width) + 'px';
    }
    const p = popup.getBoundingClientRect();
    let left, top;
    if (options.side === 'right') {
      left = a.right + (options.offsetX ?? -2);
      top = a.top + (options.offsetY ?? -5);
      if (left + p.width > innerWidth - 4) left = a.left - p.width + 2;
    } else if (options.side === 'pointer') {
      left = a.left;
      top = a.top;
      if (left + p.width > innerWidth - 4) left = a.left - p.width;
    } else {
      left = options.alignRight ? a.right - p.width : a.left;
      top = a.bottom + (options.offsetY ?? 0);
    }
    if (top + p.height > innerHeight - 4) {
      top = options.side === 'right' || options.side === 'pointer' ? innerHeight - p.height - 4 : a.top - p.height - (options.offsetY ?? 0);
    }
    popup.style.inset = 'auto';
    popup.style.left = Math.round(clamp(left, 4, Math.max(4, innerWidth - p.width - 4))) + 'px';
    popup.style.top = Math.round(Math.max(4, top)) + 'px';
  }

  function openPopup(popup, anchor, options) {
    if (!popup.matches(':popover-open')) {
      popup.showPopover();
    }
    place(popup, anchor, options);
  }

  function closePopup(popup) {
    if (popup && popup.matches(':popover-open')) {
      popup.hidePopover();
    }
  }

  function createPopup(className) {
    const popup = document.createElement('div');
    popup.className = 'jp-popup ' + (className || '');
    popup.popover = 'auto';
    document.body.appendChild(popup);
    return popup;
  }

  /*
  Opens and closes a drop-down from its button (or the select) on click. A popover closes itself
  ("light dismiss") when the pointer comes up outside it, which is before the click: so the click
  must not open it again, and opening on mousedown would close it on mouseup. The state at
  pointerdown decides; a keyboard click (detail 0) uses the state at that moment.
  */
  function dropDownOnClick(el, isOpen, open, close) {
    let wasOpen = false;
    el.addEventListener('pointerdown', () => { wasOpen = isOpen(); });
    el.addEventListener('click', e => {
      const openNow = e.detail > 0 ? wasOpen : isOpen();
      wasOpen = false;
      if (openNow) close(); else open();
    });
  }

  /* A list of .jp-list-box-item rows in a popup with a highlighted row that the arrow keys move */
  function listPopup(owner) {
    const popup = createPopup('jp-popup--list');
    popup.setAttribute('role', 'listbox');
    popup.addEventListener('mousedown', e => e.preventDefault());
    const api = {
      popup,
      items: [],
      highlighted: -1,
      onPick: null,
      fill(entries, selectedIndex) {
        popup.replaceChildren(...entries.map((entry, i) => {
          const row = document.createElement('div');
          row.className = 'jp-list-box-item';
          row.setAttribute('role', 'option');
          if (entry.html != null) row.innerHTML = entry.html; else row.textContent = entry.text;
          if (entry.disabled) row.setAttribute('aria-disabled', 'true');
          row.addEventListener('click', () => { if (!entry.disabled) api.onPick?.(i); });
          return row;
        }));
        api.items = [...popup.children];
        api.highlight(selectedIndex ?? -1, true);
      },
      highlight(index, scroll) {
        api.items.forEach((row, i) => row.setAttribute('aria-selected', String(i === index)));
        api.highlighted = index;
        if (scroll && index >= 0) {
          const row = api.items[index];
          const top = row.offsetTop - (popup.clientHeight - row.offsetHeight) / 2;
          popup.scrollTop = Math.max(0, top);
        }
      },
      move(delta) {
        if (api.items.length === 0) return;
        let i = api.highlighted;
        do {
          i = clamp(i + delta, 0, api.items.length - 1);
        } while (api.items[i].hasAttribute('aria-disabled') && i > 0 && i < api.items.length - 1);
        api.highlight(i, true);
      },
      get isOpen() { return popup.matches(':popover-open'); },
      open(anchor) {
        popup.style.maxHeight = (Number(owner.dataset.jpMaxDropDownHeight) || 300) + 'px';
        openPopup(popup, anchor, { matchWidth: true, offsetY: 1 });
        if (api.highlighted >= 0) api.highlight(api.highlighted, true);
      },
      close() { closePopup(popup); },
    };
    popup.addEventListener('toggle', e => owner.setAttribute('aria-expanded', String(e.newState === 'open')));
    return api;
  }

  /* ============================================================== combo box */
  function setUpComboBox(select) {
    const list = listPopup(select);
    const options = () => [...select.options].filter(o => !o.hidden);
    const watermark = select.dataset.jpWatermark;
    const sync = () => select.classList.toggle('is-watermark', Boolean(watermark) && select.value === '');
    list.onPick = i => {
      const option = options()[i];
      if (option && select.value !== option.value) {
        select.value = option.value;
        notify(select);
        fire(select, 'jp-change', option.value);
      }
      sync();
      list.close();
      select.focus();
    };
    const open = () => {
      const opts = options();
      list.fill(opts.map(o => ({ text: o.text, disabled: o.disabled })), opts.indexOf(select.selectedOptions[0]));
      list.open(select);
    };
    /* no browser list: the JetPack one opens on click */
    select.addEventListener('mousedown', e => {
      if (e.button !== 0 || select.disabled) return;
      e.preventDefault();
      select.focus();
    });
    dropDownOnClick(select, () => list.isOpen, () => { if (!select.disabled) open(); }, () => list.close());
    select.addEventListener('keydown', e => {
      if (!list.isOpen) {
        if ((e.altKey && e.key === 'ArrowDown') || e.key === 'F4' || e.key === ' ' || e.key === 'Enter') {
          e.preventDefault();
          open();
        }
        return;
      }
      if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
        e.preventDefault();
        list.move(e.key === 'ArrowDown' ? 1 : -1);
      } else if (e.key === 'Enter' || e.key === 'Tab' || (e.altKey && e.key === 'ArrowUp')) {
        if (e.key !== 'Tab') e.preventDefault();
        if (list.highlighted >= 0) list.onPick(list.highlighted); else list.close();
      } else if (e.key === 'Escape') {
        e.preventDefault();
        list.close();
      }
    });
    select.addEventListener('change', sync);
    sync();
  }

  /* IsEditable: a text box, the arrow button and a <datalist> with the items */
  function setUpEditableComboBox(host) {
    const input = host.querySelector('input');
    const button = host.querySelector('.jp-text-box-button');
    const data = host.querySelector('datalist');
    if (!input || !data) return;
    input.removeAttribute('list');
    const list = listPopup(host);
    const values = () => [...data.options].map(o => o.value);
    list.onPick = i => {
      input.value = values()[i];
      notify(input);
      fire(host, 'jp-change', input.value);
      list.close();
      input.focus();
    };
    const open = () => {
      const all = values();
      list.fill(all.map(text => ({ text })), all.indexOf(input.value));
      list.open(host);
    };
    if (button) {
      /* keep the focus in the text box */
      button.addEventListener('mousedown', e => {
        e.preventDefault();
        input.focus();
      });
      dropDownOnClick(button, () => list.isOpen, open, () => list.close());
    }
    input.addEventListener('keydown', e => {
      if ((e.altKey && e.key === 'ArrowDown') || e.key === 'F4') {
        e.preventDefault();
        open();
      } else if (list.isOpen && (e.key === 'ArrowDown' || e.key === 'ArrowUp')) {
        e.preventDefault();
        list.move(e.key === 'ArrowDown' ? 1 : -1);
      } else if (list.isOpen && e.key === 'Enter') {
        e.preventDefault();
        if (list.highlighted >= 0) list.onPick(list.highlighted);
      } else if (e.key === 'Escape') {
        list.close();
      }
    });
    list.popup.addEventListener('toggle', e => button?.setAttribute('aria-expanded', String(e.newState === 'open')));
  }

  /* ================================================== list box and friends */
  /* Single selection with the mouse and the arrow keys; data-jp-name keeps a hidden input */
  function setUpSelection(container, itemSelector) {
    const items = () => [...container.querySelectorAll(itemSelector)].filter(el => !el.hasAttribute('aria-disabled'));
    const select = (item, focus) => {
      items().forEach(el => el.setAttribute('aria-selected', String(el === item)));
      if (focus !== false) item?.scrollIntoView?.({ block: 'nearest' });
      if (container.dataset.jpName) {
        let hidden = container.querySelector(':scope > input[type="hidden"]');
        if (!hidden) {
          hidden = Object.assign(document.createElement('input'), { type: 'hidden', name: container.dataset.jpName });
          container.appendChild(hidden);
        }
        hidden.value = item?.dataset.value ?? item?.textContent.trim() ?? '';
      }
      fire(container, 'jp-change', item);
    };
    container.addEventListener('click', e => {
      const item = e.target.closest(itemSelector);
      if (item && container.contains(item) && !item.hasAttribute('aria-disabled')) {
        select(item);
      }
    });
    container.addEventListener('keydown', e => {
      if (e.key !== 'ArrowDown' && e.key !== 'ArrowUp' && e.key !== 'Home' && e.key !== 'End') return;
      const all = items();
      if (all.length === 0) return;
      const current = all.findIndex(el => el.getAttribute('aria-selected') === 'true');
      const next = e.key === 'Home' ? 0 : e.key === 'End' ? all.length - 1
        : clamp(current + (e.key === 'ArrowDown' ? 1 : -1), 0, all.length - 1);
      select(all[next]);
      e.preventDefault();
    });
    if (!container.hasAttribute('tabindex')) container.tabIndex = 0;
  }

  /* TreeView: the [+]/[-] boxes are <details>; the items select like a list */
  function setUpTreeView(tree) {
    setUpSelection(tree, '.jp-tree-view__item');
    tree.addEventListener('click', e => {
      /* only the box folds; a click on the header text selects without folding */
      const summary = e.target.closest('summary');
      if (summary && e.target.closest('.jp-tree-view__item')) e.preventDefault();
    });
  }

  /* ============================================================= data grid */
  /* DataGrid: a click on a column header sorts the rows, a second click turns the order round and
     Shift+click sorts by one more column, as CanUserSortColumns does in WPF. Dragging a header's
     right edge resizes the column (CanUserResizeColumns) and a double-click there fits it to its
     content. data-jp-sort="false" / data-jp-resize="false" on the table or a <th> turn either off;
     a header with a link sorts on the server. Cells sort by data-sort-value, else by their text. */
  const MIN_COLUMN_WIDTH = 20;
  const collator = new Intl.Collator(undefined, { numeric: true, sensitivity: 'base' });

  function compareCells(a, b) {
    if (a === b) return 0;
    if (a === '' || b === '') return a === '' ? -1 : 1;
    const x = Number(a), y = Number(b);
    return Number.isFinite(x) && Number.isFinite(y) ? x - y : collator.compare(a, b);
  }

  /* the width a cell needs for its content, padding and border; headers keep room for the arrow */
  function contentWidth(cell) {
    const range = document.createRange();
    const grip = cell.lastElementChild?.classList.contains('jp-data-grid__resizer') ? 1 : 0;
    range.setStart(cell, 0);
    range.setEnd(cell, cell.childNodes.length - grip);
    const s = getComputedStyle(cell);
    const frame = ['paddingLeft', 'paddingRight', 'borderLeftWidth', 'borderRightWidth'].reduce((sum, p) => sum + parseFloat(s[p]), 0);
    return Math.ceil(range.getBoundingClientRect().width + frame + (cell.tagName === 'TH' ? 13 : 0));
  }

  /* style.width for a border-box width in pixels, whatever box-sizing the page uses */
  function setCellWidth(cell, px) {
    const s = getComputedStyle(cell);
    const frame = s.boxSizing === 'border-box' ? 0
      : ['paddingLeft', 'paddingRight', 'borderLeftWidth', 'borderRightWidth'].reduce((sum, p) => sum + parseFloat(s[p]), 0);
    cell.style.width = Math.max(0, px - frame) + 'px';
  }

  function setUpDataGrid(table) {
    const headers = [...(table.tHead?.rows[0]?.cells ?? [])];
    if (headers.length === 0) return;
    const allowed = (th, key) => table.dataset[key] !== 'false' && th.dataset[key] !== 'false';

    /* sorting; the columns the server marked with aria-sort are the order the rows came in */
    let sorts = headers.flatMap((th, i) => {
      const dir = th.getAttribute('aria-sort');
      return dir === 'ascending' || dir === 'descending' ? [{ i, dir }] : [];
    });
    const sortBy = (index, add) => {
      const current = sorts.find(s => s.i === index);
      const dir = current?.dir === 'ascending' ? 'descending' : 'ascending';
      const sorting = new CustomEvent('jp-sorting', { bubbles: true, cancelable: true, detail: { column: index, header: headers[index], direction: dir } });
      if (!table.dispatchEvent(sorting)) return;
      if (!add) sorts = [];
      if (current && add) current.dir = dir; else sorts.push({ i: index, dir });
      headers.forEach((th, i) => {
        const s = sorts.find(s => s.i === i);
        if (s) th.setAttribute('aria-sort', s.dir); else th.removeAttribute('aria-sort');
      });
      for (const body of table.tBodies) {
        const rows = [...body.rows];
        const keys = new Map(rows.map(row => [row, sorts.map(s => {
          const cell = row.cells[s.i];
          return cell ? (cell.dataset.sortValue ?? cell.textContent).trim() : '';
        })]));
        rows.sort((a, b) => {
          const ka = keys.get(a), kb = keys.get(b);
          for (let k = 0; k < sorts.length; k++) {
            const c = compareCells(ka[k], kb[k]);
            if (c) return sorts[k].dir === 'descending' ? -c : c;
          }
          return 0;
        });
        body.append(...rows);
      }
      fire(table, 'jp-sorted', sorts.map(s => ({ column: s.i, header: headers[s.i], direction: s.dir })));
    };
    headers.forEach(th => {
      if (!allowed(th, 'jpSort') || th.querySelector('a, button, input, select')) return;
      th.classList.add('is-sortable');
      th.tabIndex = 0;
    });
    table.tHead.addEventListener('click', e => {
      const th = e.target.closest('th.is-sortable');
      if (th && !e.target.closest('.jp-data-grid__resizer')) sortBy(headers.indexOf(th), e.shiftKey);
    });
    table.tHead.addEventListener('keydown', e => {
      const th = e.target.closest('th.is-sortable');
      if (!th || (e.key !== 'Enter' && e.key !== ' ')) return;
      sortBy(headers.indexOf(th), e.shiftKey);
      e.preventDefault();
    });

    /* resizing: the first drag fixes every column at its width (table-layout: fixed), so only the
       dragged column changes and the ones to its right move along. An empty header after the last
       column takes the room that is left, like the filler header of a WPF DataGrid. */
    let frozen = false;
    const freeze = () => {
      const widths = headers.map(th => th.offsetWidth);
      headers.forEach((th, i) => setCellWidth(th, widths[i]));
      const filler = document.createElement('th');
      filler.className = 'jp-data-grid__filler';
      filler.setAttribute('aria-hidden', 'true');
      headers[headers.length - 1].after(filler);
      table.style.tableLayout = 'fixed';
      table.classList.add('is-resized');
      frozen = true;
    };
    const resize = (i, px) => {
      if (!frozen) freeze();
      setCellWidth(headers[i], Math.max(MIN_COLUMN_WIDTH, Math.round(px)));
    };
    headers.forEach((th, i) => {
      if (!allowed(th, 'jpResize')) return;
      const grip = document.createElement('span');
      grip.className = 'jp-data-grid__resizer';
      grip.setAttribute('aria-hidden', 'true');
      th.appendChild(grip);
      grip.addEventListener('pointerdown', e => {
        if (e.button !== 0) return;
        e.preventDefault();
        const startX = e.clientX, start = th.offsetWidth;
        const move = ev => resize(i, start + ev.clientX - startX);
        const end = () => {
          grip.removeEventListener('pointermove', move);
          grip.removeEventListener('lostpointercapture', end);
          table.classList.remove('is-resizing');
        };
        grip.setPointerCapture(e.pointerId);
        grip.addEventListener('pointermove', move);
        grip.addEventListener('lostpointercapture', end);
        table.classList.add('is-resizing');
      });
      grip.addEventListener('dblclick', () => {
        const cells = [th, ...[...table.tBodies, table.tFoot].filter(Boolean)
          .flatMap(part => [...part.rows].map(row => row.cells[i]).filter(c => c && c.colSpan === 1))];
        resize(i, Math.max(...cells.map(contentWidth)));
      });
    });
  }

  /* =========================================================== thin scroll */
  function setUpThinScroll(viewer) {
    let timer = 0;
    const show = () => {
      viewer.classList.add('is-active');
      clearTimeout(timer);
      timer = setTimeout(() => viewer.classList.remove('is-active'), 1500);
    };
    viewer.addEventListener('scroll', show, { passive: true });
    viewer.addEventListener('pointermove', e => {
      /* only near the bars, as the WPF ScrollBar only reacts when the mouse is over it */
      const r = viewer.getBoundingClientRect();
      if (e.clientX > r.right - 16 || e.clientY > r.bottom - 16) show();
    });
  }

  /* ================================================================ sliders */
  function setUpSlider(slider) {
    const range = slider.classList.contains('jp-range-slider');
    const vertical = slider.classList.contains('jp-slider--vertical');
    const min = Number(slider.getAttribute('aria-valuemin') ?? 0);
    const max = Number(slider.getAttribute('aria-valuemax') ?? 100);
    const small = Number(slider.dataset.jpSmallChange) || (max - min) / 100;
    const large = Number(slider.dataset.jpLargeChange) || (max - min) / 10;
    const tick = Number(slider.dataset.jpTicks) || 0;
    const snap = slider.dataset.jpSnap === 'true' && tick > 0;
    const state = {
      lower: Number(slider.dataset.jpLowerValue ?? min),
      upper: Number(slider.getAttribute('aria-valuenow') ?? min),
    };
    const ticksEl = slider.querySelector('.jp-slider__ticks');
    if (tick > 0 && ticksEl) {
      slider.classList.add('has-ticks');
      slider.style.setProperty('--jp-ticks', String(Math.round((max - min) / tick)));
    }
    const thumbs = [...slider.querySelectorAll('.jp-slider__thumb')];
    if (range && thumbs.length === 1) {
      const lowerThumb = thumbs[0].cloneNode(true);
      thumbs[0].before(lowerThumb);
      thumbs.unshift(lowerThumb);
    }
    if (range) thumbs[0].classList.add('is-lower');

    const step = v => {
      if (snap) v = Math.round((v - min) / tick) * tick + min;
      return clamp(v, min, max);
    };
    const pct = v => ((v - min) / (max - min || 1)) * 100 + '%';
    const render = () => {
      slider.style.setProperty('--jp-slider-at', pct(state.upper));
      slider.style.setProperty('--jp-slider-from', range ? pct(state.lower) : '0%');
      slider.setAttribute('aria-valuenow', String(state.upper));
      if (range) {
        slider.dataset.jpLowerValue = String(state.lower);
        slider.setAttribute('aria-valuetext', `${state.lower} – ${state.upper}`);
      }
      slider.querySelectorAll(':scope > input[type="hidden"]').forEach((input, i) => {
        input.value = String(range && i === 0 ? state.lower : state.upper);
      });
    };
    let active = range ? 1 : 0; /* which thumb the keys move */
    const set = (which, value, final) => {
      value = step(value);
      if (range && which === 0) state.lower = Math.min(value, state.upper);
      else if (range) state.upper = Math.max(value, state.lower);
      else state.upper = value;
      render();
      const detail = range ? { lowerValue: state.lower, upperValue: state.upper } : state.upper;
      fire(slider, 'input', detail);
      fire(slider, 'jp-change', detail);
      if (final) fire(slider, 'change', detail);
    };
    const valueAt = e => {
      const r = slider.getBoundingClientRect();
      const t = vertical ? 1 - (e.clientY - r.top - 6) / (r.height - 12) : (e.clientX - r.left - 6) / (r.width - 12);
      return min + clamp(t, 0, 1) * (max - min);
    };
    slider.addEventListener('pointerdown', e => {
      if (slider.getAttribute('aria-disabled') === 'true' || e.button !== 0) return;
      const v = valueAt(e);
      active = range ? (Math.abs(v - state.lower) < Math.abs(v - state.upper) || v < state.lower ? 0 : 1) : 0;
      thumbs[range ? active : 0]?.classList.add('is-dragging');
      slider.setPointerCapture(e.pointerId);
      set(active, v);
      slider.focus();
      e.preventDefault();
    });
    slider.addEventListener('pointermove', e => {
      if (slider.hasPointerCapture(e.pointerId)) set(active, valueAt(e));
    });
    slider.addEventListener('pointerup', e => {
      if (slider.hasPointerCapture(e.pointerId)) {
        slider.releasePointerCapture(e.pointerId);
        thumbs.forEach(t => t.classList.remove('is-dragging'));
        set(active, valueAt(e), true);
      }
    });
    slider.addEventListener('keydown', e => {
      const current = range && active === 0 ? state.lower : state.upper;
      const delta = { ArrowRight: small, ArrowUp: small, ArrowLeft: -small, ArrowDown: -small, PageUp: large, PageDown: -large }[e.key];
      if (delta !== undefined) {
        set(active, current + (snap ? Math.sign(delta) * tick : delta), true);
      } else if (e.key === 'Home') {
        set(active, min, true);
      } else if (e.key === 'End') {
        set(active, max, true);
      } else if (range && e.key === 'Tab' && !e.shiftKey && active === 0) {
        active = 1;
      } else if (range && e.key === 'Tab' && e.shiftKey && active === 1) {
        active = 0;
      } else {
        return;
      }
      if (e.key !== 'Tab') e.preventDefault();
    });
    if (!slider.hasAttribute('tabindex') && slider.getAttribute('aria-disabled') !== 'true') slider.tabIndex = 0;
    render();
  }

  /* ================================================================= rating */
  function setUpRating(rating) {
    const max = Number(getComputedStyle(rating).getPropertyValue('--max')) || 5;
    const valueAt = e => clamp(Math.ceil((e.clientX - rating.getBoundingClientRect().left) / 20), 1, max);
    const show = v => rating.style.setProperty('--value', String(v));
    const value = () => Number(rating.getAttribute('aria-valuenow') ?? 0);
    rating.addEventListener('pointermove', e => show(valueAt(e)));
    rating.addEventListener('pointerleave', () => show(value()));
    rating.addEventListener('click', e => {
      const v = valueAt(e);
      rating.setAttribute('aria-valuenow', String(v));
      show(v);
      fire(rating, 'jp-change', v);
    });
    rating.addEventListener('keydown', e => {
      const v = clamp(value() + (e.key === 'ArrowRight' || e.key === 'ArrowUp' ? 1 : e.key === 'ArrowLeft' || e.key === 'ArrowDown' ? -1 : 0), 0, max);
      if (v !== value()) {
        rating.setAttribute('aria-valuenow', String(v));
        show(v);
        fire(rating, 'jp-change', v);
        e.preventDefault();
      }
    });
    if (!rating.hasAttribute('tabindex')) rating.tabIndex = 0;
  }

  /* =============================================================== calendar */
  const MONTHS = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
  const DAYS = ['S', 'M', 'T', 'W', 'T', 'F', 'S'];
  const iso = d => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  const parseIso = s => {
    const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(s || '');
    return m ? new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3])) : null;
  };
  const sameDay = (a, b) => a && b && a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
  /* en-US short date, as WPF shows it in the demo: 10/23/2026 */
  const formatDate = d => `${d.getMonth() + 1}/${d.getDate()}/${d.getFullYear()}`;
  const parseDate = s => {
    const m = /^\s*(\d{1,2})\/(\d{1,2})\/(\d{4})\s*$/.exec(s || '');
    if (!m) return null;
    const d = new Date(Number(m[3]), Number(m[1]) - 1, Number(m[2]));
    return d.getMonth() === Number(m[1]) - 1 ? d : null;
  };

  /*
  <div class="jp-calendar" data-selected-date="2026-10-27" data-display-date="2026-10-01"></div>
  Month, year and decade views as in WPF: click the header to go up, a month or year to go down.
  */
  function setUpCalendar(cal) {
    const state = {
      selected: parseIso(cal.dataset.selectedDate),
      display: parseIso(cal.dataset.displayDate) || parseIso(cal.dataset.selectedDate) || new Date(),
      mode: 'month',
    };
    state.display = new Date(state.display.getFullYear(), state.display.getMonth(), 1);
    cal.setAttribute('role', 'group');

    const header = document.createElement('div');
    header.className = 'jp-calendar__header';
    const prev = Object.assign(document.createElement('button'), { type: 'button', className: 'jp-calendar__previous', ariaLabel: 'Previous' });
    const title = Object.assign(document.createElement('button'), { type: 'button', className: 'jp-calendar__title' });
    const next = Object.assign(document.createElement('button'), { type: 'button', className: 'jp-calendar__next', ariaLabel: 'Next' });
    header.append(prev, title, next);
    const grid = document.createElement('div');
    grid.className = 'jp-calendar__grid';
    cal.replaceChildren(header, grid);

    const cell = (text, classes, onClick) => {
      const b = Object.assign(document.createElement('button'), { type: 'button', textContent: text, className: 'jp-calendar__cell ' + classes });
      b.tabIndex = -1;
      if (onClick) b.addEventListener('click', onClick);
      return b;
    };

    function render() {
      const today = new Date();
      grid.dataset.mode = state.mode;
      const y = state.display.getFullYear(), m = state.display.getMonth();
      if (state.mode === 'month') {
        title.textContent = `${MONTHS[m]} ${y}`;
        const first = new Date(y, m, 1);
        const start = new Date(y, m, 1 - first.getDay() - (first.getDay() === 0 ? 7 : 0));
        const cells = DAYS.map(d => Object.assign(document.createElement('span'), { className: 'jp-calendar__day-name', textContent: d }));
        for (let i = 0; i < 42; i++) {
          const d = new Date(start.getFullYear(), start.getMonth(), start.getDate() + i);
          const classes = [d.getMonth() !== m ? 'is-other-month' : '', sameDay(d, today) ? 'is-today' : '', sameDay(d, state.selected) ? 'is-selected' : ''].join(' ');
          const b = cell(String(d.getDate()), classes, () => choose(d));
          b.dataset.date = iso(d);
          if (sameDay(d, state.selected) || (!state.selected && sameDay(d, today))) b.tabIndex = 0;
          cells.push(b);
        }
        grid.replaceChildren(...cells);
      } else if (state.mode === 'year') {
        title.textContent = String(y);
        grid.replaceChildren(...MONTHS.map((name, i) => cell(name.slice(0, 3),
          state.selected && state.selected.getFullYear() === y && state.selected.getMonth() === i ? 'is-selected' : '',
          () => { state.display = new Date(y, i, 1); state.mode = 'month'; render(); })));
      } else {
        const from = Math.floor(y / 10) * 10;
        title.textContent = `${from}-${from + 9}`;
        const years = [];
        for (let i = from - 1; i <= from + 10; i++) {
          years.push(cell(String(i), [i < from || i > from + 9 ? 'is-other-month' : '', state.selected?.getFullYear() === i ? 'is-selected' : ''].join(' '),
            () => { state.display = new Date(i, m, 1); state.mode = 'year'; render(); }));
        }
        grid.replaceChildren(...years);
      }
    }

    function choose(d) {
      state.selected = d;
      state.display = new Date(d.getFullYear(), d.getMonth(), 1);
      cal.dataset.selectedDate = iso(d);
      render();
      grid.querySelector('.is-selected')?.focus();
      fire(cal, 'jp-change', d);
    }

    prev.addEventListener('click', () => shift(-1));
    next.addEventListener('click', () => shift(1));
    title.addEventListener('click', () => {
      state.mode = state.mode === 'month' ? 'year' : 'decade';
      render();
    });
    function shift(n) {
      const y = state.display.getFullYear(), m = state.display.getMonth();
      state.display = state.mode === 'month' ? new Date(y, m + n, 1) : state.mode === 'year' ? new Date(y + n, m, 1) : new Date(y + 10 * n, m, 1);
      render();
    }
    grid.addEventListener('keydown', e => {
      if (state.mode !== 'month') return;
      const focused = document.activeElement?.dataset?.date ? parseIso(document.activeElement.dataset.date) : state.selected || new Date();
      const delta = { ArrowLeft: -1, ArrowRight: 1, ArrowUp: -7, ArrowDown: 7 }[e.key];
      if (delta !== undefined) {
        const d = new Date(focused.getFullYear(), focused.getMonth(), focused.getDate() + delta);
        if (d.getMonth() !== state.display.getMonth()) {
          state.display = new Date(d.getFullYear(), d.getMonth(), 1);
          render();
        }
        grid.querySelector(`[data-date="${iso(d)}"]`)?.focus();
        e.preventDefault();
      } else if (e.key === 'Enter' || e.key === ' ') {
        choose(focused);
        e.preventDefault();
      }
    });
    cal.jetpack = {
      get selectedDate() { return state.selected; },
      set selectedDate(d) { state.selected = d; if (d) state.display = new Date(d.getFullYear(), d.getMonth(), 1); state.mode = 'month'; render(); },
      focus() { (grid.querySelector('.is-selected') || grid.querySelector('[tabindex="0"]'))?.focus(); },
    };
    render();
  }

  /* DatePicker: the text box, the calendar button and a Calendar in a popup */
  function setUpDatePicker(host) {
    const input = host.querySelector('input.jp-text-box');
    const button = host.querySelector('.jp-text-box-button');
    const hidden = host.querySelector('input[type="hidden"]');
    const popup = createPopup('jp-popup--calendar');
    const cal = document.createElement('div');
    cal.className = 'jp-calendar';
    const initial = parseIso(host.dataset.selectedDate) || parseDate(input.value);
    if (initial) {
      cal.dataset.selectedDate = iso(initial);
      input.value = formatDate(initial);
    }
    popup.appendChild(cal);
    setUpCalendar(cal);
    const commit = d => {
      input.value = d ? formatDate(d) : '';
      host.dataset.selectedDate = d ? iso(d) : '';
      if (hidden) hidden.value = d ? iso(d) : '';
      notify(input);
      fire(host, 'jp-change', d);
    };
    cal.addEventListener('jp-change', e => {
      commit(e.detail);
      closePopup(popup);
      input.focus();
    });
    const open = () => {
      cal.jetpack.selectedDate = parseDate(input.value);
      openPopup(popup, host, { offsetY: 1 });
      cal.jetpack.focus();
    };
    dropDownOnClick(button, () => popup.matches(':popover-open'), open, () => closePopup(popup));
    popup.addEventListener('toggle', e => button.setAttribute('aria-expanded', String(e.newState === 'open')));
    input.addEventListener('change', () => {
      const d = parseDate(input.value);
      if (d) commit(d);
    });
    input.addEventListener('keydown', e => {
      if ((e.altKey && e.key === 'ArrowDown') || e.key === 'F4') {
        e.preventDefault();
        open();
      }
    });
  }

  /* TimePicker: a list of times MinuteStep apart */
  function setUpTimePicker(host) {
    const input = host.querySelector('input.jp-text-box');
    const button = host.querySelector('.jp-text-box-button');
    const step = Math.max(1, Number(host.dataset.jpMinuteStep) || 15);
    const times = [];
    for (let m = 0; m < 24 * 60; m += step) {
      times.push(`${String(Math.floor(m / 60)).padStart(2, '0')}:${String(m % 60).padStart(2, '0')}`);
    }
    const list = listPopup(host);
    list.onPick = i => {
      input.value = times[i];
      notify(input);
      fire(host, 'jp-change', times[i]);
      list.close();
      input.focus();
    };
    const open = () => {
      list.fill(times.map(text => ({ text })), times.indexOf(input.value));
      host.dataset.jpMaxDropDownHeight = host.dataset.jpMaxDropDownHeight || '240';
      list.open(host);
    };
    button.addEventListener('mousedown', e => {
      e.preventDefault();
      input.focus();
    });
    dropDownOnClick(button, () => list.isOpen, open, () => list.close());
    list.popup.addEventListener('toggle', e => button.setAttribute('aria-expanded', String(e.newState === 'open')));
    input.addEventListener('keydown', e => {
      if ((e.altKey && e.key === 'ArrowDown') || e.key === 'F4') {
        e.preventDefault();
        open();
      } else if (list.isOpen && (e.key === 'ArrowDown' || e.key === 'ArrowUp')) {
        e.preventDefault();
        list.move(e.key === 'ArrowDown' ? 1 : -1);
      } else if (list.isOpen && e.key === 'Enter') {
        e.preventDefault();
        if (list.highlighted >= 0) list.onPick(list.highlighted);
      }
    });
  }

  /* ============================================ search combo box, suggestions */
  /* Items come from a <script type="application/json"> inside the control: strings, or objects
     with the members named by data-jp-display-member / data-jp-value-member. */
  function readItems(host) {
    const script = host.querySelector('script[type="application/json"]');
    try { return script ? JSON.parse(script.textContent) : []; } catch { return []; }
  }

  const escapeHtml = s => s.replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));

  function setUpSearchComboBox(host) {
    const input = host.querySelector('input.jp-text-box');
    const button = host.querySelector('.jp-text-box-button');
    const hidden = host.querySelector('input[type="hidden"]');
    const items = readItems(host);
    const display = host.dataset.jpDisplayMember;
    const valueOf = host.dataset.jpValueMember;
    const startsWith = host.dataset.jpFilterMode === 'StartsWith';
    const text = item => String(display ? item[display] : item);
    const value = item => (valueOf ? item[valueOf] : text(item));
    const limit = 300;
    let selected = items.find(item => String(value(item)) === host.dataset.jpSelectedValue) ?? null;
    let shown = [];
    const list = listPopup(host);
    host.dataset.jpMaxDropDownHeight = host.dataset.jpMaxDropDownHeight || '260';

    const show = filter => {
      const q = filter.trim().toLowerCase();
      shown = items.filter(item => {
        if (!q) return true;
        const t = text(item).toLowerCase();
        return startsWith ? t.startsWith(q) : t.includes(q);
      }).slice(0, limit);
      list.fill(shown.map(item => ({ text: text(item) })), Math.max(0, q ? 0 : shown.indexOf(selected)));
      if (!list.isOpen) list.open(host);
    };
    const pick = item => {
      selected = item;
      input.value = item ? text(item) : '';
      if (hidden) hidden.value = item ? String(value(item)) : '';
      host.dataset.jpSelectedValue = item ? String(value(item)) : '';
      list.close();
      fire(host, 'jp-change', { selectedItem: item, selectedValue: item ? value(item) : null });
    };
    list.onPick = i => pick(shown[i]);
    if (selected) input.value = text(selected);

    button.addEventListener('mousedown', e => {
      e.preventDefault();
      if (!input.disabled) input.focus();
    });
    dropDownOnClick(button, () => list.isOpen, () => { if (!input.disabled) show(''); }, () => list.close());
    list.popup.addEventListener('toggle', e => button.setAttribute('aria-expanded', String(e.newState === 'open')));
    input.addEventListener('input', () => show(input.value));
    input.addEventListener('keydown', e => {
      if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
        e.preventDefault();
        if (!list.isOpen) show(selected && input.value === text(selected) ? '' : input.value);
        else list.move(e.key === 'ArrowDown' ? 1 : -1);
      } else if ((e.key === 'Enter' || e.key === 'Tab') && list.isOpen) {
        if (e.key === 'Enter') e.preventDefault();
        if (list.highlighted >= 0) pick(shown[list.highlighted]);
      } else if (e.key === 'Escape' && list.isOpen) {
        list.close();
        input.value = selected ? text(selected) : '';
      }
    });
    input.addEventListener('blur', () => {
      if (!list.isOpen && input.value !== (selected ? text(selected) : '')) input.value = selected ? text(selected) : '';
    });
  }

  function setUpAutoSuggestBox(host) {
    const input = host.querySelector('input.jp-text-box');
    const items = readItems(host);
    let shown = [];
    const list = listPopup(host);
    list.onPick = i => {
      input.value = shown[i];
      notify(input);
      fire(host, 'jp-change', shown[i]);
      list.close();
    };
    input.addEventListener('input', () => {
      const q = input.value.trim().toLowerCase();
      shown = q ? items.filter(t => t.toLowerCase().includes(q)) : [];
      if (shown.length === 0) {
        list.close();
        return;
      }
      list.fill(shown.map(t => ({ text: t })), 0);
      list.open(input);
    });
    input.addEventListener('keydown', e => {
      if (!list.isOpen) return;
      if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
        e.preventDefault();
        list.move(e.key === 'ArrowDown' ? 1 : -1);
      } else if (e.key === 'Enter' || e.key === 'Tab') {
        if (e.key === 'Enter') e.preventDefault();
        if (list.highlighted >= 0) list.onPick(list.highlighted);
      } else if (e.key === 'Escape') {
        list.close();
      }
    });
  }

  /* ============================================================ color picker */
  const PALETTE = [
    '#75A20A', '#91B006', '#C2DF00', '#FE770A', '#FFB400', '#148CD1', '#42BAFF', '#4599CA',
    '#DC000C', '#EF0A0A', '#8E44AD', '#2E7D32', '#202020', '#57575C', '#707176', '#C6C6C6',
    '#E0E0E0', '#FFFFFF', '#B62F00', '#00538E', '#2B3D00', '#686868', '#DCDCDD', '#F7F7F7',
  ];

  /* <button class="jp-color-picker" value="#75A20A"><span class="jp-color-picker__swatch"></span><span class="jp-color-picker__text"></span></button> */
  function setUpColorPicker(picker) {
    const popup = createPopup('jp-popup--palette');
    const grid = document.createElement('div');
    grid.className = 'jp-color-picker__palette';
    popup.appendChild(grid);
    const swatch = picker.querySelector('.jp-color-picker__swatch');
    const label = picker.querySelector('.jp-color-picker__text');
    const show = color => {
      picker.value = color;
      picker.dataset.selectedColor = color;
      if (swatch) swatch.style.background = color;
      if (label) label.textContent = color;
      grid.querySelectorAll('button').forEach(b => b.setAttribute('aria-selected', String(b.value === color)));
    };
    const palette = (picker.dataset.jpPalette || '').split(/[\s,]+/).filter(Boolean);
    (palette.length ? palette : PALETTE).forEach(color => {
      const b = Object.assign(document.createElement('button'), { type: 'button', value: color, title: color });
      b.style.background = color;
      b.addEventListener('click', () => {
        show(color);
        closePopup(popup);
        picker.focus();
        fire(picker, 'jp-change', color);
        fire(picker, 'change', color);
      });
      grid.appendChild(b);
    });
    dropDownOnClick(picker, () => popup.matches(':popover-open'), () => openPopup(popup, picker, { offsetY: 1 }), () => closePopup(popup));
    popup.addEventListener('toggle', e => picker.setAttribute('aria-expanded', String(e.newState === 'open')));
    show((picker.dataset.selectedColor || picker.value || PALETTE[0]).toUpperCase());
  }

  /* ============================================================ tab control */
  function setUpTabControl(control) {
    const strip = control.querySelector(':scope > .jp-tab-control__header .jp-tab-control__strip, :scope > .jp-tab-control__strip');
    if (!strip) return;
    const tabs = () => [...strip.querySelectorAll(':scope > .jp-tab-item')];
    const notch = strip.querySelector('.jp-tab-control__notch');

    function placeNotch(tab, animate) {
      if (!notch || !tab) return;
      const left = tab.offsetLeft + tab.offsetWidth / 2 - 7.5;
      const mode = control.dataset.selectionAnimation || 'slide';
      notch.style.visibility = 'visible';
      if (!animate || mode === 'none' || !notch.classList.contains('is-placed')) {
        notch.classList.remove('is-placed');
        notch.style.left = left + 'px';
        void notch.offsetWidth;
        notch.classList.add('is-placed');
        return;
      }
      if (mode === 'drop') {
        notch.classList.add('is-raised');
        setTimeout(() => {
          notch.classList.remove('is-placed');
          notch.style.left = left + 'px';
          void notch.offsetWidth;
          notch.classList.add('is-placed');
          notch.classList.remove('is-raised');
        }, 160);
      } else {
        notch.style.left = left + 'px';
      }
    }

    function select(tab, animate = true, focus = false) {
      if (!tab || tab.disabled) return;
      tabs().forEach(t => {
        const on = t === tab;
        t.setAttribute('aria-selected', String(on));
        t.tabIndex = on ? 0 : -1;
        const panel = document.getElementById(t.getAttribute('aria-controls'));
        if (panel) panel.hidden = !on;
      });
      placeNotch(tab, animate);
      if (focus) tab.focus();
      if (control.dataset.jpHash === 'true' && tab.dataset.name) {
        history.replaceState(null, '', '#' + tab.dataset.name);
      }
      fire(control, 'jp-change', { index: tabs().indexOf(tab), header: tab.textContent.trim(), name: tab.dataset.name });
    }

    strip.addEventListener('click', e => {
      const tab = e.target.closest('.jp-tab-item');
      if (tab && tabs().includes(tab)) select(tab);
    });
    strip.addEventListener('keydown', e => {
      const all = tabs().filter(t => !t.disabled);
      const i = all.indexOf(document.activeElement);
      if (i < 0) return;
      const next = { ArrowRight: 1, ArrowLeft: -1 }[e.key];
      if (next !== undefined) select(all[(i + next + all.length) % all.length], true, true);
      else if (e.key === 'Home') select(all[0], true, true);
      else if (e.key === 'End') select(all[all.length - 1], true, true);
      else return;
      e.preventDefault();
    });
    let initial = tabs().find(t => t.getAttribute('aria-selected') === 'true') || tabs()[0];
    if (control.dataset.jpHash === 'true' && location.hash) {
      initial = tabs().find(t => t.dataset.name === location.hash.slice(1)) || initial;
    }
    select(initial, false);
    new ResizeObserver(() => placeNotch(tabs().find(t => t.getAttribute('aria-selected') === 'true'), false)).observe(strip);
    document.fonts?.ready.then(() => placeNotch(tabs().find(t => t.getAttribute('aria-selected') === 'true'), false));
    control.jetpack = {
      select(indexOrName) {
        const tab = typeof indexOrName === 'number' ? tabs()[indexOrName] : tabs().find(t => t.dataset.name === indexOrName || t.textContent.trim() === indexOrName);
        select(tab);
      },
      get selectedIndex() { return tabs().findIndex(t => t.getAttribute('aria-selected') === 'true'); },
    };
  }

  /* =========================================================== grid splitter */
  /* Resizes the pane before the splitter: its width in a flex row, its height when the splitter
     has jp-grid-splitter--rows (or sits in a flex column) */
  function setUpGridSplitter(splitter) {
    const pane = splitter.previousElementSibling;
    if (!pane) return;
    const rows = splitter.classList.contains('jp-grid-splitter--rows') ||
      getComputedStyle(splitter.parentElement).flexDirection.startsWith('column');
    if (rows) {
      splitter.classList.add('jp-grid-splitter--rows');
      splitter.setAttribute('aria-orientation', 'horizontal');
    }
    const size = el => rows ? el.getBoundingClientRect().height : el.getBoundingClientRect().width;
    const minSize = el => parseFloat(rows ? getComputedStyle(el).minHeight : getComputedStyle(el).minWidth) || 0;
    const position = e => rows ? e.clientY : e.clientX;
    let start = 0, startSize = 0;
    splitter.tabIndex = 0;
    splitter.addEventListener('pointerdown', e => {
      start = position(e);
      startSize = size(pane);
      splitter.setPointerCapture(e.pointerId);
      e.preventDefault();
    });
    splitter.addEventListener('pointermove', e => {
      if (!splitter.hasPointerCapture(e.pointerId)) return;
      const total = size(splitter.parentElement);
      const next = splitter.nextElementSibling;
      const nextMin = next ? minSize(next) : 0;
      pane.style.flex = `0 0 ${clamp(startSize + position(e) - start, minSize(pane), total - size(splitter) - nextMin)}px`;
    });
    splitter.addEventListener('pointerup', e => splitter.releasePointerCapture(e.pointerId));
    splitter.addEventListener('keydown', e => {
      const d = (rows ? { ArrowUp: -10, ArrowDown: 10 } : { ArrowLeft: -10, ArrowRight: 10 })[e.key];
      if (d) {
        pane.style.flex = `0 0 ${size(pane) + d}px`;
        e.preventDefault();
      }
    });
  }

  /* ================================================================== menus */
  let lastInvoker = null;
  let submenuTimer = 0;

  function menuItems(menu) {
    return [...menu.querySelectorAll(':scope > .jp-menu-item:not(:disabled)')];
  }

  function openSubmenu(item) {
    const sub = document.getElementById(item.dataset.jpSubmenu || '');
    if (!sub || sub.matches(':popover-open')) return sub;
    /* close sibling submenus first */
    item.parentElement.querySelectorAll(':scope > .jp-menu-item[aria-haspopup="menu"]').forEach(other => {
      if (other !== item) closePopup(document.getElementById(other.dataset.jpSubmenu || ''));
    });
    /* the submenu sits inside its parent menu in the page, so opening it keeps the parent open */
    sub.showPopover();
    place(sub, item, { side: 'right' });
    return sub;
  }

  function closeMenus() {
    document.querySelectorAll('.jp-context-menu:popover-open').forEach(m => m.hidePopover());
  }

  /* Places a menu opened with popovertarget under the button (or split button) that opened it */
  function placePopover(popover) {
    const invoker = lastInvoker && lastInvoker.getAttribute('popovertarget') === popover.id
      ? lastInvoker
      : document.querySelector(`[popovertarget="${CSS.escape(popover.id)}"]`);
    if (!invoker || invoker.matches('.jp-context-menu .jp-menu-item')) return;
    const anchor = invoker.closest('.jp-split-button') || invoker;
    place(popover, anchor, { alignRight: popover.dataset.jpAlign === 'right', offsetY: invoker.closest('.jp-menu') ? 0 : 1 });
  }

  /* ================================================================ tooltip */
  const pointer = { x: 0, y: 0 };
  document.addEventListener('pointermove', e => { pointer.x = e.clientX; pointer.y = e.clientY; }, { passive: true });
  let tipTimer = 0;
  let tipEl = null;
  function hideToolTip() {
    clearTimeout(tipTimer);
    tipEl?.remove();
    tipEl = null;
  }
  function showToolTip(target, x, y) {
    hideToolTip();
    tipEl = document.createElement('div');
    tipEl.className = 'jp-tool-tip is-floating';
    tipEl.setAttribute('role', 'tooltip');
    tipEl.textContent = target.dataset.jpToolTip;
    document.body.appendChild(tipEl);
    const r = tipEl.getBoundingClientRect();
    tipEl.style.left = clamp(x, 4, innerWidth - r.width - 4) + 'px';
    tipEl.style.top = (y + 20 + r.height > innerHeight ? y - r.height - 4 : y + 20) + 'px';
  }

  /* ======================================================= validation tip */
  let errorEl = null;
  function showError(target) {
    hideError();
    if (!target.dataset.jpError) return;
    errorEl = document.createElement('div');
    errorEl.className = 'jp-validation-tip';
    errorEl.setAttribute('role', 'alert');
    errorEl.textContent = target.dataset.jpError;
    document.body.appendChild(errorEl);
    const r = target.getBoundingClientRect();
    errorEl.style.left = r.right + 7 + 'px';
    errorEl.style.top = r.top + 'px';
  }
  function hideError() {
    errorEl?.remove();
    errorEl = null;
  }

  /* ================================================================ flyouts */
  const flyoutTimers = new WeakMap();
  function openFlyout(flyout) {
    if (flyout instanceof HTMLDialogElement) {
      if (!flyout.open) flyout.showModal();
      return;
    }
    flyout.classList.add('is-open');
    flyout.setAttribute('aria-hidden', 'false');
    restartAutoClose(flyout);
    fire(flyout, 'jp-open');
  }
  function closeFlyout(flyout) {
    if (flyout instanceof HTMLDialogElement) {
      flyout.close();
      return;
    }
    flyout.classList.remove('is-open');
    flyout.setAttribute('aria-hidden', 'true');
    clearTimeout(flyoutTimers.get(flyout));
    fire(flyout, 'jp-close');
  }
  function restartAutoClose(flyout) {
    clearTimeout(flyoutTimers.get(flyout));
    const ms = Number(flyout.dataset.jpAutoClose);
    if (ms > 0 && flyout.classList.contains('is-open') && !flyout.matches(':hover')) {
      flyoutTimers.set(flyout, setTimeout(() => closeFlyout(flyout), ms));
    }
  }

  /* ================================================================ dialogs */
  /* JetPackDialog: JetPack.showDialog({ title, content, affirmativeText, negativeText, affirmativeStyle })
     resolves to 'affirmative' or 'negative'; showMessage(title, message) has a single OK button. */
  function showDialog(options) {
    return new Promise(resolve => {
      const dialog = document.createElement('dialog');
      dialog.className = 'jp-dialog';
      const header = document.createElement('header');
      header.className = 'jp-dialog__header';
      const title = document.createElement('span');
      title.textContent = options.title || '';
      title.id = 'jp-dialog-' + Math.random().toString(36).slice(2);
      dialog.setAttribute('aria-labelledby', title.id);
      const x = Object.assign(document.createElement('button'), { type: 'button', textContent: '×', ariaLabel: 'Close', value: 'negative' });
      header.append(title, x);
      const content = document.createElement('div');
      content.className = 'jp-dialog__content';
      const text = document.createElement('p');
      text.style.margin = '0';
      text.textContent = options.content || '';
      const buttons = document.createElement('div');
      buttons.className = 'jp-dialog__buttons';
      const button = (label, value, style) => {
        const b = Object.assign(document.createElement('button'), { type: 'button', textContent: label, value });
        b.className = 'jp-button' + (style ? ' jp-button--' + style : '');
        b.style.minWidth = '80px';
        return b;
      };
      if (options.negativeText) buttons.append(button(options.negativeText, 'negative'));
      const ok = button(options.affirmativeText || 'OK', 'affirmative', options.affirmativeStyle || 'accent');
      buttons.append(ok);
      content.append(text, buttons);
      dialog.append(header, content);
      document.body.appendChild(dialog);
      dialog.addEventListener('click', e => {
        const b = e.target.closest('button[value]');
        if (b) dialog.close(b.value);
      });
      dialog.addEventListener('close', () => {
        resolve(dialog.returnValue === 'affirmative' ? 'affirmative' : 'negative');
        dialog.remove();
      });
      dialog.showModal();
      ok.focus();
    });
  }

  function showMessage(title, message) {
    return showDialog({ title, content: message, affirmativeText: 'OK' });
  }

  /* =========================================================== hamburger */
  function setPaneOpen(open, menus) {
    (menus || document.querySelectorAll('.jp-window > .jp-window__body > .jp-hamburger-menu, .jp-hamburger-menu[data-jp-window-pane]'))
      .forEach(m => {
        m.classList.toggle('is-collapsed', !open);
        m.querySelector('[data-jp-toggle-pane]')?.setAttribute('aria-expanded', String(open));
      });
    document.querySelectorAll('[data-jp-pane-switch]').forEach(s => { s.checked = open; });
    save(PANE_KEY, open ? 'open' : 'closed');
  }

  /* A HamburgerMenu with its own content (jp-hamburger-menu-host): the items switch the panels */
  function setUpHamburgerHost(host) {
    const items = [...host.querySelectorAll('.jp-hamburger-menu__item[aria-controls]')];
    const select = item => items.forEach(i => {
      const on = i === item;
      i.setAttribute('aria-current', on ? 'page' : 'false');
      i.setAttribute('aria-selected', String(on));
      const panel = document.getElementById(i.getAttribute('aria-controls'));
      if (panel) panel.hidden = !on;
    });
    items.forEach(i => i.addEventListener('click', () => select(i)));
    select(items.find(i => i.getAttribute('aria-current') === 'page') || items[0]);
  }

  /* ================================================================== input */
  function closestIn(event, selector) {
    return event.target instanceof Element ? event.target.closest(selector) : null;
  }

  /* A RepeatButton clicks when pressed and then while held (below); the browser's own click on
     release would be one too many. Keyboard clicks (detail 0) stay. */
  document.addEventListener('click', event => {
    if (event.isTrusted && event.detail > 0 && closestIn(event, '[data-jp-repeat]')) {
      event.stopImmediatePropagation();
      event.preventDefault();
    }
  }, true);

  document.addEventListener('click', event => {
    let el;
    if ((el = closestIn(event, '[popovertarget]'))) {
      lastInvoker = el;
    }
    if ((el = closestIn(event, '[data-jp-open]'))) {
      const target = document.getElementById(el.dataset.jpOpen);
      if (target) {
        const open = target instanceof HTMLDialogElement ? target.open : target.classList.contains('is-open');
        if (open && el.dataset.jpToggleOpen !== 'false' && !(target instanceof HTMLDialogElement)) closeFlyout(target);
        else openFlyout(target);
      }
      event.preventDefault();
      return;
    }
    if ((el = closestIn(event, '[data-jp-close]'))) {
      const flyout = el.closest('.jp-flyout, dialog');
      if (flyout) closeFlyout(flyout);
      return;
    }
    if ((el = closestIn(event, '[data-jp-dismiss]'))) {
      const bar = el.closest('.jp-info-bar');
      if (bar) {
        bar.hidden = true;
        fire(bar, 'jp-close');
      }
      return;
    }
    if ((el = closestIn(event, '[data-jp-clear]'))) {
      const box = el.parentElement.querySelector('input, textarea');
      if (box) {
        box.value = '';
        notify(box);
        box.focus();
      }
      return;
    }
    if ((el = closestIn(event, '[data-jp-step]')) && event.detail === 0) {
      /* keyboard click; the mouse steps through the repeat handler below */
      stepNumeric(el);
      return;
    }
    if ((el = closestIn(event, '[data-jp-toggle-pane]'))) {
      const menu = el.closest('.jp-hamburger-menu');
      if (menu) {
        const open = menu.classList.contains('is-collapsed');
        if (menu.closest('.jp-hamburger-menu-host')) {
          menu.classList.toggle('is-collapsed', !open);
          el.setAttribute('aria-expanded', String(open));
        } else {
          setPaneOpen(open);
        }
      }
      return;
    }
    if ((el = closestIn(event, '[data-jp-toggle]'))) {
      el.setAttribute('aria-pressed', String(el.getAttribute('aria-pressed') !== 'true'));
      fire(el, 'jp-change', el.getAttribute('aria-pressed') === 'true');
    }
    if ((el = closestIn(event, '[data-jp-accent-button]'))) {
      ThemeManager.changeAccent(el.dataset.jpAccentButton);
      syncInputs();
      return;
    }
    if ((el = closestIn(event, '.jp-context-menu .jp-menu-item'))) {
      if (el.getAttribute('aria-haspopup') === 'menu') {
        openSubmenu(el);
        event.preventDefault();
        return;
      }
      if (el.getAttribute('role') === 'menuitemcheckbox') {
        el.setAttribute('aria-checked', String(el.getAttribute('aria-checked') !== 'true'));
      }
      closeMenus();
      return;
    }
    if ((el = closestIn(event, 'tr[data-href]')) && !closestIn(event, 'a, button, input, select, label')) {
      if (event.ctrlKey || event.metaKey) window.open(el.dataset.href);
      else location.href = el.dataset.href;
    }
  });

  /* RepeatButton and the spin buttons of NumericUpDown: once now, then again while held */
  function stepNumeric(button) {
    const box = button.closest('.jp-numeric-up-down')?.querySelector('input');
    if (box && !box.disabled && !box.readOnly) {
      if (box.value === '' && box.placeholder) box.value = box.min || '0';
      if (Number(button.dataset.jpStep) > 0) box.stepUp(); else box.stepDown();
      notify(box);
    }
  }
  let repeatTimer = 0;
  document.addEventListener('pointerdown', event => {
    const el = closestIn(event, '[data-jp-repeat], [data-jp-step]');
    if (!el || event.button !== 0 || el.disabled) return;
    const run = () => (el.matches('[data-jp-step]') ? stepNumeric(el) : el.dispatchEvent(new MouseEvent('click', { bubbles: true, detail: 1 })));
    run();
    if (el.matches('[data-jp-step]')) event.preventDefault();
    clearTimeout(repeatTimer);
    repeatTimer = setTimeout(function again() {
      run();
      repeatTimer = setTimeout(again, 33);
    }, 400);
    const stop = () => {
      clearTimeout(repeatTimer);
      document.removeEventListener('pointerup', stop);
      document.removeEventListener('pointercancel', stop);
    };
    document.addEventListener('pointerup', stop);
    document.addEventListener('pointercancel', stop);
  });

  /* Right-click: ContextMenu */
  document.addEventListener('contextmenu', event => {
    const el = closestIn(event, '[data-jp-context-menu]');
    const menu = el && document.getElementById(el.dataset.jpContextMenu);
    if (!menu) return;
    event.preventDefault();
    closeMenus();
    menu.showPopover();
    place(menu, new DOMRect(event.clientX, event.clientY, 0, 0), { side: 'pointer' });
  });

  /* Hover: submenus open after a moment; in an open Menu bar the other headers open at once */
  document.addEventListener('pointerover', event => {
    const item = closestIn(event, '.jp-context-menu .jp-menu-item');
    if (item) {
      clearTimeout(submenuTimer);
      submenuTimer = setTimeout(() => {
        if (item.getAttribute('aria-haspopup') === 'menu' && !item.disabled) openSubmenu(item);
        else item.parentElement.querySelectorAll(':scope > .jp-menu-item[aria-haspopup="menu"]').forEach(other => {
          const sub = document.getElementById(other.dataset.jpSubmenu || '');
          if (sub && !sub.contains(event.target)) closePopup(sub);
        });
      }, 250);
    }
    const header = closestIn(event, '.jp-menu > .jp-menu-item[popovertarget]');
    if (header && header.parentElement.querySelector(':scope > .jp-menu-item.is-open') && !header.classList.contains('is-open')) {
      lastInvoker = header;
      closeMenus();
      document.getElementById(header.getAttribute('popovertarget'))?.showPopover();
    }
    const tip = closestIn(event, '[data-jp-tool-tip]');
    if (tip && !tip.contains(event.relatedTarget)) {
      clearTimeout(tipTimer);
      /* shown where the pointer is after the wait, as a WPF ToolTip */
      tipTimer = setTimeout(() => showToolTip(tip, pointer.x, pointer.y), 500);
    }
    const invalid = closestIn(event, '[data-jp-error]');
    if (invalid && !invalid.contains(event.relatedTarget)) showError(invalid);
  });
  document.addEventListener('pointerout', event => {
    const tip = closestIn(event, '[data-jp-tool-tip]');
    if (tip && !tip.contains(event.relatedTarget)) hideToolTip();
    const invalid = closestIn(event, '[data-jp-error]');
    if (invalid && !invalid.contains(event.relatedTarget) && !invalid.contains(document.activeElement)) hideError();
    const flyout = closestIn(event, '.jp-flyout[data-jp-auto-close]');
    if (flyout && !flyout.contains(event.relatedTarget)) restartAutoClose(flyout);
  });
  document.addEventListener('pointerover', event => {
    const flyout = closestIn(event, '.jp-flyout[data-jp-auto-close]');
    if (flyout) clearTimeout(flyoutTimers.get(flyout));
  });
  document.addEventListener('pointerdown', hideToolTip, true);
  document.addEventListener('focusin', event => {
    const invalid = closestIn(event, '[data-jp-error]');
    if (invalid) showError(invalid); else hideError();
  });
  document.addEventListener('scroll', () => { hideToolTip(); hideError(); }, true);

  document.addEventListener('change', event => {
    const el = event.target;
    if (el.matches('[data-jp-accent]') && el.checked) {
      ThemeManager.changeAccent(el.value);
    } else if (el.matches('[data-jp-focus-visuals]')) {
      ThemeManager.showFocusVisuals = el.checked;
    } else if (el.matches('[data-jp-pane-switch]')) {
      setPaneOpen(el.checked);
    } else if (el.matches('.jp-radio-button > input')) {
      document.querySelectorAll(`.jp-radio-button > input[name="${CSS.escape(el.name)}"]`).forEach(r => r.classList.remove('is-indeterminate'));
    }
  });

  document.addEventListener('toggle', event => {
    const popover = event.target;
    if (!(popover instanceof HTMLElement) || !popover.classList.contains('jp-context-menu')) return;
    if (event.newState === 'open' && !popover.dataset.jpPlaced) placePopover(popover);
    /* the header of a Menu bar stays lit while its menu is open */
    document.querySelectorAll(`.jp-menu > [popovertarget="${CSS.escape(popover.id)}"]`).forEach(h => h.classList.toggle('is-open', event.newState === 'open'));
    if (event.newState === 'closed') {
      popover.querySelectorAll('.jp-menu-item[aria-haspopup="menu"]').forEach(item => closePopup(document.getElementById(item.dataset.jpSubmenu || '')));
    }
  }, true);

  /* Keys inside an open menu: arrows move, Right opens a submenu, Left closes it */
  document.addEventListener('keydown', event => {
    if (event.key === 'Escape') {
      const flyout = [...document.querySelectorAll('.jp-flyout.is-open')].pop();
      if (flyout && !document.querySelector(':popover-open, dialog[open]')) {
        closeFlyout(flyout);
        return;
      }
    }
    const menu = closestIn(event, '.jp-context-menu');
    if (!menu) return;
    const items = menuItems(menu);
    const index = items.indexOf(document.activeElement);
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      const next = event.key === 'ArrowDown' ? index + 1 : index - 1;
      items[(next + items.length) % items.length]?.focus();
    } else if (event.key === 'ArrowRight' && document.activeElement?.getAttribute('aria-haspopup') === 'menu') {
      menuItems(openSubmenu(document.activeElement) || menu)[0]?.focus();
    } else if (event.key === 'ArrowLeft' && menu.parentElement.closest('.jp-context-menu')) {
      closePopup(menu);
      menu.parentElement.querySelector(`[data-jp-submenu="${CSS.escape(menu.id)}"]`)?.focus();
    } else {
      return;
    }
    event.preventDefault();
  });

  /* ================================================================== start */
  function syncInputs() {
    document.querySelectorAll('[data-jp-accent]').forEach(r => { r.checked = r.value === ThemeManager.currentAccent; });
    document.querySelectorAll('[data-jp-focus-visuals]').forEach(c => { c.checked = ThemeManager.showFocusVisuals; });
  }

  const controls = [
    ['canvas.jp-vignette', setUpVignette],
    ['select.jp-combo-box', setUpComboBox],
    ['.jp-combo-box--editable', setUpEditableComboBox],
    ['.jp-list-box', el => setUpSelection(el, '.jp-list-box-item')],
    ['.jp-list-view', el => setUpSelection(el, 'tbody > tr')],
    ['.jp-data-grid[data-jp-selection]', el => setUpSelection(el, 'tbody > tr')],
    ['table.jp-data-grid', setUpDataGrid],
    ['.jp-tree-view', setUpTreeView],
    ['.jp-scroll-viewer--thin', setUpThinScroll],
    ['.jp-slider, .jp-range-slider', setUpSlider],
    ['.jp-rating[data-jp-input]', setUpRating],
    ['.jp-calendar:not(.jp-popup .jp-calendar)', setUpCalendar],
    ['.jp-date-picker', setUpDatePicker],
    ['.jp-time-picker', setUpTimePicker],
    ['.jp-search-combo-box', setUpSearchComboBox],
    ['.jp-auto-suggest-box', setUpAutoSuggestBox],
    ['.jp-color-picker', setUpColorPicker],
    ['.jp-tab-control', setUpTabControl],
    ['.jp-grid-splitter', setUpGridSplitter],
    ['.jp-hamburger-menu-host', setUpHamburgerHost],
    ['input[data-jp-indeterminate]', el => {
      if (el.type === 'checkbox') el.indeterminate = true;
      else el.classList.add('is-indeterminate');
    }],
  ];

  function init(scope = document) {
    for (const [selector, setUp] of controls) {
      scope.querySelectorAll(selector).forEach(el => {
        if (el.jpReady?.[selector]) return;
        (el.jpReady ??= {})[selector] = true;
        setUp(el);
      });
    }
  }

  document.addEventListener('DOMContentLoaded', () => {
    init();
    syncInputs();
    const pane = load(PANE_KEY);
    if (pane) {
      setPaneOpen(pane === 'open');
    } else if (window.matchMedia('(max-width: 640px)').matches) {
      setPaneOpen(false);
    }
    document.querySelectorAll('.jp-flyout:not(.is-open)').forEach(f => f.setAttribute('aria-hidden', 'true'));
    document.querySelectorAll('.jp-flyout.is-open').forEach(restartAutoClose);
  });

  window.JetPack = {
    ThemeManager,
    init,
    showDialog,
    showMessage,
    openFlyout: el => openFlyout(typeof el === 'string' ? document.getElementById(el) : el),
    closeFlyout: el => closeFlyout(typeof el === 'string' ? document.getElementById(el) : el),
    formatDate,
    parseDate,
  };
})();
