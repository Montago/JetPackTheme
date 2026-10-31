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
  JetPackGrid       .jp-jetpack-grid: search, header filters, sorting, groups and paging in the browser,
                    or on the server with data-jp-items-source-url (server mode);
                    JetPack.jetpackGrid(el or id) → { searchText, groupMemberPath, pageSize, pageIndex,
                    items, itemsSource, selectedItem, clearFilters(), getVisibleItems(), refresh(), … }

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

  /* ============================================================ JetPackGrid */
  /*
  The data grid for lists of up to a few thousand rows (jp:JetPackGrid). <jp-jetpack-grid> writes the
  header and the rows as JSON in a script element; this draws the rows of the page shown. A word search
  across the columns marks its hits, every header has a value filter (a tick list, a tree of dates, or
  typed values and ranges), a header click sorts ascending, descending and back to the source order
  (Shift adds a column), GroupMemberPath groups the rows under headers that fold, and PageSize shows a
  pager. JetPack.jetpackGrid(element or id) gives the API; the grid raises "jp-filtered" ({ count,
  totalCount }), "jp-page-changed" ({ pageIndex, pageCount }), "jp-selected-item-changed" ({ oldValue,
  newValue }), "jp-sorting" and "jp-sorted".

  Server mode (items-source-url, or grid.itemsSource = url or function): the grid posts what it shows
  (page, page size, search, filters, sort, groups and its columns' bindings and formats: a
  JetPackGridRequest) and draws the page the server answers with (a JetPackGridResponse; see
  JetPackGridServer.Query). A filter box asks for its column's values the same way, with valuesFor.
  The grid is aria-busy while a page is on its way and raises "jp-load-failed" ({ error }) when it fails.
  */
  const GRID_HINT = 'Tick values or type them, separated by spaces or commas. Quote values with spaces; * matches any text. ' +
    'Numbers and dates: &gt;100, &lt;=2019, 2020-01-01..2020-06-30.';
  const DAY_MS = 86400000;

  /* Splits on spaces, commas and semicolons; double quotes keep a value with spaces together. With
     joinRanges (the values of a header filter), "> 5" and "2019 .. 2021" stay one value. */
  function gridTokens(text, joinRanges) {
    const tokens = [];
    if (!text || !text.trim()) return tokens;
    const has = t => tokens.some(x => x.toLowerCase() === t.toLowerCase());
    let current = '', quoted = false, pending = null;
    const flush = () => {
      let token = current.trim();
      current = '';
      if (!token) return;
      if (joinRanges) {
        if (token === '..' && pending === null && tokens.length > 0) {
          pending = tokens.pop() + token;
          return;
        }
        if (token === '>' || token === '<' || token === '>=' || token === '<=') {
          pending = (pending ?? '') + token;
          return;
        }
        if (pending !== null) {
          token = pending + token;
          pending = null;
        }
      }
      if (!has(token)) tokens.push(token);
    };
    for (const ch of text) {
      if (ch === '"') {
        flush();
        quoted = !quoted;
      } else if (!quoted && (/\s/.test(ch) || ch === ',' || ch === ';')) {
        flush();
      } else {
        current += ch;
      }
    }
    flush();
    if (pending !== null && !has(pending)) tokens.push(pending);
    return tokens;
  }

  /* The separators and the order of day, month and year of a culture, for typed numbers and dates */
  function gridCulture(name) {
    let locale;
    try { locale = Intl.getCanonicalLocales(name || [])[0]; } catch { locale = undefined; }
    const parts = new Intl.NumberFormat(locale).formatToParts(12345.6);
    const order = new Intl.DateTimeFormat(locale, { timeZone: 'UTC' }).formatToParts(Date.UTC(2001, 1, 3))
      .filter(p => p.type === 'day' || p.type === 'month' || p.type === 'year').map(p => p.type[0]).join('');
    return {
      locale,
      group: parts.find(p => p.type === 'group')?.value ?? ',',
      decimal: parts.find(p => p.type === 'decimal')?.value ?? '.',
      order: order.length === 3 ? order : 'mdy',
      numbers: new Intl.NumberFormat(locale),
      collator: new Intl.Collator(locale),
    };
  }

  function parseGridNumber(text, c) {
    let s = text.replace(/\s/g, '').split(c.group).join('');
    if (c.decimal !== '.') s = s.replace(c.decimal, '.');
    return /^[+-]?(\d+\.?\d*|\.\d+)$/.test(s) ? Number(s) : NaN;
  }

  /* "2020-03-14", "2020-03-14T10:30" as milliseconds in UTC (the time zone plays no part) */
  function parseIsoMs(s) {
    const m = /^(\d{4})-(\d{1,2})-(\d{1,2})(?:[T ](\d{1,2}):(\d{2})(?::(\d{2}))?)?$/.exec(s);
    return m ? Date.UTC(+m[1], m[2] - 1, +m[3], +(m[4] || 0), +(m[5] || 0), +(m[6] || 0)) : NaN;
  }

  /* A typed date: year first, or in the culture's order; { ms, hasTime } or null */
  function parseGridDate(text, c) {
    const m = /^(\d{1,4})[-/.](\d{1,2})[-/.](\d{1,4})\.?(?:T(\d{1,2}):(\d{2})(?::(\d{2}))?)?$/.exec(text);
    if (!m) return null;
    let y, mo, d;
    if (m[1].length === 4) {
      [y, mo, d] = [+m[1], +m[2], +m[3]];
    } else {
      const at = {};
      [...c.order].forEach((k, i) => { at[k] = +m[i + 1]; });
      [y, mo, d] = [at.y, at.m, at.d];
      if (y < 100) y += 2000;
    }
    const ms = Date.UTC(y, mo - 1, d, +(m[4] || 0), +(m[5] || 0), +(m[6] || 0));
    const date = new Date(ms);
    return date.getUTCMonth() === mo - 1 && date.getUTCDate() === d ? { ms, hasTime: m[4] !== undefined } : null;
  }

  /* One end of a range, read both as a number and as a date (whichever the column's values are). A year
     or a month stands for all of it and a date without a time for the whole day, so "<=2019" takes in
     31 December 2019 and ">2019" starts on 1 January 2020. */
  function parseGridBound(text, c) {
    const number = parseGridNumber(text, c);
    let start = null, end = null, m;
    if ((m = /^(\d{4})$/.exec(text)) && +m[1] >= 1) {
      start = Date.UTC(+m[1], 0, 1);
      end = Date.UTC(+m[1] + 1, 0, 1);
    } else if ((m = /^(\d{4})-(\d{1,2})$/.exec(text)) && +m[2] >= 1 && +m[2] <= 12) {
      start = Date.UTC(+m[1], m[2] - 1, 1);
      end = Date.UTC(+m[1], +m[2], 1);
    } else {
      const date = parseGridDate(text, c);
      if (date) {
        start = date.ms;
        end = date.hasTime ? null : date.ms + DAY_MS;
      }
    }
    return isNaN(number) && start === null ? null : { number: isNaN(number) ? null : number, start, end };
  }

  /* ">5", ">=5", "<5", "<=5" and "a..b"; null for any other value */
  function parseGridRange(token, c) {
    let low = null, high = null, above = false, below = false;
    if (token.startsWith('>=')) low = token.slice(2);
    else if (token.startsWith('<=')) high = token.slice(2);
    else if (token.startsWith('>')) { low = token.slice(1); above = true; }
    else if (token.startsWith('<')) { high = token.slice(1); below = true; }
    else {
      const dots = token.indexOf('..');
      if (dots < 0) return null;
      low = token.slice(0, dots);
      high = token.slice(dots + 2);
    }
    low = low?.trim();
    high = high?.trim();
    if (!low && !high) return null;
    let from = null, to = null;
    if ((low && !(from = parseGridBound(low, c))) || (high && !(to = parseGridBound(high, c)))) return null;
    return { from, above, to, below };
  }

  /* Whether a value is in the range; undefined when the range does not apply to it (text, or a bound
     that is no date for a date), and the value is then matched as text */
  function inGridRange(range, value, type) {
    if (value == null) return false;
    let v, read;
    if (type === 'number' && typeof value === 'number') {
      v = value;
      read = b => b.number === null ? null : { low: b.number, high: b.number, inclusive: true };
    } else if (type === 'date' && !isNaN(v = parseIsoMs(String(value)))) {
      read = b => b.start === null ? null : { low: b.start, high: b.end ?? b.start, inclusive: b.end === null };
    } else {
      return undefined;
    }
    let lo = null, hi = null;
    if ((range.from && !(lo = read(range.from))) || (range.to && !(hi = read(range.to)))) return undefined;
    if (lo) {
      const d = v - (range.above ? lo.high : lo.low);
      if (d < 0 || (range.above && d === 0 && lo.inclusive)) return false;
    }
    if (hi) {
      const d = v - (range.below ? hi.low : hi.high);
      if (range.below ? d >= 0 : d > 0 || (d === 0 && !hi.inclusive)) return false;
    }
    return true;
  }

  /* A header filter value: text that must equal the cell (ignoring case), * for any text, or a range */
  function gridFilterValue(token, c) {
    const anyStart = token.startsWith('*');
    const anyEnd = token.endsWith('*') && token.length > 1;
    const text = token.replace(/^\*+|\*+$/g, '').toLowerCase();
    return {
      range: parseGridRange(token, c),
      matches(cell) {
        const t = cell.toLowerCase();
        if (!text) return t.length > 0;
        return anyStart && anyEnd ? t.includes(text) : anyStart ? t.endsWith(text) : anyEnd ? t.startsWith(text) : t === text;
      },
    };
  }

  /* The cell text with the search words wrapped in marks; null when none is in it */
  function markGridHits(text, words) {
    const lower = text.toLowerCase();
    const ranges = [];
    for (const w of words) {
      for (let i = lower.indexOf(w); i >= 0; i = lower.indexOf(w, i + w.length)) ranges.push([i, i + w.length]);
    }
    if (ranges.length === 0) return null;
    ranges.sort((a, b) => a[0] - b[0]);
    let html = '', at = 0;
    for (const [s, e] of ranges) {
      if (e <= at) continue;
      const from = Math.max(s, at);
      html += escapeHtml(text.slice(at, from)) + '<mark class="jp-jetpack-grid__hit">' + escapeHtml(text.slice(from, e)) + '</mark>';
      at = e;
    }
    return html + escapeHtml(text.slice(at));
  }

  /* ---------------------------------------------------------- date tree */
  /* The years, months and days of a column of dates with their row counts, under decades when the years
     span ten or more, and "(blank)" last. A node is ticked (true), not (false) or partly (null). */
  function buildDateTree(days, blanks, c) {
    const monthName = new Intl.DateTimeFormat(c.locale, { month: 'long', timeZone: 'UTC' });
    const node = (level, day, text, count, children = []) => {
      const n = { level, day, text, count, children, checked: false, expanded: false, parent: null };
      children.forEach(ch => { ch.parent = n; });
      return n;
    };
    const years = [];
    for (const day of [...days.keys()].sort()) {
      const y = +day.slice(0, 4), m = +day.slice(5, 7);
      let year = years[years.length - 1];
      if (year?.y !== y) years.push(year = { y, months: [] });
      let month = year.months[year.months.length - 1];
      if (month?.m !== m) year.months.push(month = { m, days: [] });
      month.days.push(node('day', day, String(+day.slice(8, 10)), days.get(day)));
    }
    const sum = nodes => nodes.reduce((s, n) => s + n.count, 0);
    const yearNodes = years.map(year => {
      const months = year.months.map(month => {
        const name = monthName.format(Date.UTC(2000, month.m - 1, 1));
        return node('month', null, name.charAt(0).toLocaleUpperCase(c.locale) + name.slice(1), sum(month.days), month.days);
      });
      const n = node('year', null, String(year.y), sum(months), months);
      n.y = year.y;
      return n;
    });
    let top = yearNodes;
    if (yearNodes.length > 1 && yearNodes[yearNodes.length - 1].y - yearNodes[0].y >= 10) {
      const decades = new Map();
      yearNodes.forEach(y => {
        const d = Math.floor(y.y / 10) * 10;
        if (!decades.has(d)) decades.set(d, []);
        decades.get(d).push(y);
      });
      top = [...decades].map(([d, ys]) => node('decade', null, `${d}–${d + 9}`, sum(ys), ys));
    }
    /* a single year (or decade) has nothing to choose between; show what is in it */
    if (top.length === 1) top[0].expanded = true;
    if (blanks > 0) top.push(node('blank', null, '(blank)', blanks));
    return top;
  }

  const combineTicks = states => states.every(s => s === states[0]) ? states[0] : null;

  /* Ticks exactly the days in the list (null ticks "(blank)") */
  function tickDateTree(nodes, dates) {
    const days = new Set((dates ?? []).filter(d => d != null).map(d => String(d).slice(0, 10)));
    const blanks = (dates ?? []).includes(null);
    const tick = n => {
      n.checked = n.level === 'day' ? days.has(n.day) : n.level === 'blank' ? blanks : combineTicks(n.children.map(tick));
      return n.checked;
    };
    nodes.forEach(tick);
  }

  function tickedDates(nodes, out = []) {
    for (const n of nodes) {
      if (n.checked === false) continue;
      if (n.level === 'day') out.push(n.day);
      else if (n.level === 'blank') out.push(null);
      else tickedDates(n.children, out);
    }
    return out;
  }

  function setDateTick(node, value) {
    const all = n => { n.checked = value; n.children.forEach(all); };
    all(node);
    for (let p = node.parent; p; p = p.parent) p.checked = combineTicks(p.children.map(ch => ch.checked));
  }

  /* Opens every node with only some of its days ticked, so the tree shows what the filter holds */
  function expandPartlyTicked(nodes) {
    nodes.forEach(n => {
      if (n.checked === null) {
        n.expanded = true;
        expandPartlyTicked(n.children);
      }
    });
  }

  /* -------------------------------------------------------------- the grid */
  function setUpJetPackGrid(grid) {
    const viewport = grid.querySelector('.jp-jetpack-grid__viewport');
    const table = viewport?.querySelector('table');
    const headRow = table?.tHead?.rows[0];
    if (!headRow) return;
    const body = table.tBodies[0] ?? table.createTBody();
    const ds = grid.dataset;
    const c = gridCulture(ds.jpCulture || root.lang);
    const fmt = n => c.numbers.format(n);

    const columns = [...headRow.cells].map((th, index) => ({
      th,
      index,
      header: th.textContent.trim(),
      binding: th.dataset.binding ?? '',
      type: th.dataset.type ?? null,
      align: th.dataset.align ?? (th.hasAttribute('data-check-box') ? 'center' : ''),
      width: Number(th.dataset.width) > 0 ? Number(th.dataset.width) : null,
      checkBox: th.hasAttribute('data-check-box'),
      trueText: th.dataset.trueText ?? 'Yes',
      falseText: th.dataset.falseText ?? 'No',
      searchable: th.dataset.searchable !== 'false',
      filterable: th.dataset.filterable !== 'false',
      sortable: ds.jpSort !== 'false' && th.dataset.jpSort !== 'false',
      resizable: ds.jpResize !== 'false' && th.dataset.jpResize !== 'false',
      filterText: th.dataset.filterText ?? '',
      filterDates: th.dataset.filterDates ? JSON.parse(th.dataset.filterDates) : null,
      stringFormat: th.dataset.stringFormat ?? null,
    }));

    /* ---- rows: { i, t: texts, v: values }, plus the app's object when the items came from script */
    let rows = [];
    const readPath = (obj, path) => path ? path.split('.').reduce((v, name) => v?.[name], obj) : obj;
    const toRow = (cells, i, source) => {
      const t = [], v = [];
      columns.forEach((col, k) => {
        const cell = cells[k];
        if (Array.isArray(cell)) { t.push(String(cell[0] ?? '')); v.push(cell[1] ?? null); }
        else if (cell == null) { t.push(''); v.push(null); }
        else if (typeof cell === 'boolean') { t.push(cell ? col.trueText : col.falseText); v.push(cell); }
        else { t.push(String(cell)); v.push(cell); }
      });
      return { i, t, v, lower: null, item: null, source };
    };
    /* An object of the app's: numbers and dates are shown in the culture's format */
    const fromObject = (obj, i) => toRow(columns.map(col => {
      const value = readPath(obj, col.binding);
      if (value == null) return null;
      if (col.type === 'bool') return Boolean(value);
      if (value instanceof Date) return [value.toLocaleDateString(c.locale), value.toISOString().slice(0, 19)];
      if (col.type === 'date' && !isNaN(parseIsoMs(String(value)))) {
        return [new Date(parseIsoMs(String(value))).toLocaleDateString(c.locale, { timeZone: 'UTC' }), String(value)];
      }
      if (typeof value === 'number') return col.type === 'number' ? [fmt(value), value] : String(value);
      return String(value);
    }), i, obj);
    const inferTypes = list => columns.forEach(col => {
      if (col.type) return;
      const value = list.map(obj => readPath(obj, col.binding)).find(v => v != null);
      col.type = col.checkBox || typeof value === 'boolean' ? 'bool' : typeof value === 'number' ? 'number'
        : value instanceof Date || (typeof value === 'string' && /^\d{4}-\d{2}-\d{2}/.test(value)) ? 'date' : 'text';
    });
    const setRows = list => {
      list = Array.isArray(list) ? list : [];
      if (list.length && !Array.isArray(list[0])) inferTypes(list);
      columns.forEach(col => { col.type ??= col.checkBox ? 'bool' : 'text'; });
      rows = list.map((entry, i) => Array.isArray(entry) ? toRow(entry, i) : fromObject(entry, i));
    };
    const script = grid.querySelector('script.jp-jetpack-grid__items');
    let data = [];
    try { data = JSON.parse(script?.textContent || '[]'); } catch { /* no rows */ }
    setRows(data);
    const itemOf = row => row ? row.source ?? (row.item ??= Object.fromEntries(
      columns.filter(col => col.binding).map(col => [col.binding, row.v[col.index]]))) : null;
    const lowerOf = row => row.lower ??= row.t.map(s => s.toLowerCase());

    /* ---- state */
    let searchText = ds.jpSearchText ?? '';
    let words = [];
    const delay = Number(ds.jpSearchDelay ?? 300);
    const listLimit = Number(ds.jpFilterListLimit ?? 20);
    let groupBy = ds.jpGroupMemberPath ?? '';
    let pageSize = Math.max(0, Number(ds.jpPageSize) || 0);
    let pageIndex = Math.max(0, Number(ds.jpPageIndex) || 0);
    let pageCount = 1;
    let selectionMode = (ds.jpSelection ?? 'single').toLowerCase();
    const frozenCount = Number(ds.jpFrozenColumnCount) || 0;
    let sorts = columns.filter(col => col.th.hasAttribute('aria-sort')).map(col => ({ col, dir: col.th.getAttribute('aria-sort') }));
    let ordered = [];     // the rows that pass, in the order shown
    let runs = null;      // while grouped: the groups as runs of `ordered`
    let page = { rows: [], groups: null, index: 0, start: 0, filteredCount: 0 };  // the page shown
    let entries = [];     // what the page shows: { row, alt } and { group, count, expanded }
    let filtered = false;
    let timer = 0;
    /* server mode: a URL to post requests to, or a function (query) => response (or a promise of one) */
    let itemsSource = ds.jpItemsSourceUrl || null;
    let localRows = rows;
    let totalCount = rows.length;
    let requestId = 0;
    let shownPage = -1;
    let pendingSelectedIndex = -1;
    const folded = new Set();
    const selected = new Set();
    let current = null, anchor = null, lastSelected = null;

    /* ---- header: label, sort arrow, funnel and grip; the column widths in a colgroup */
    columns.forEach(col => {
      const th = col.th;
      th.innerHTML = `<div class="jp-jetpack-grid__head"><span class="jp-jetpack-grid__label">${th.innerHTML}</span>` +
        '<span class="jp-jetpack-grid__sort" aria-hidden="true"></span>' +
        (col.filterable ? `<button type="button" class="jp-jetpack-grid__filter-button" tabindex="-1" aria-haspopup="dialog" aria-expanded="false" aria-label="Filter ${escapeHtml(col.header)}" data-jp-tool-tip="Filter"></button>` : '') +
        '</div>' + (col.resizable ? '<span class="jp-jetpack-grid__resizer" aria-hidden="true"></span>' : '');
      if (col.sortable && col.binding) th.classList.add('is-sortable');
      if (col.align === 'center') th.style.textAlign = 'center';
      col.button = th.querySelector('.jp-jetpack-grid__filter-button');
    });
    const colgroup = document.createElement('colgroup');
    columns.forEach(col => {
      col.col = document.createElement('col');
      if (col.width) col.col.style.width = col.width + 'px';
      colgroup.append(col.col);
    });
    table.prepend(colgroup);
    /* Without a star column, an empty header after the last one takes the room that is left, as the
       filler header of a WPF DataGrid does */
    let filler = null;
    const ensureFiller = () => {
      if (filler || columns.some(col => !col.col.style.width)) return;
      filler = document.createElement('th');
      filler.className = 'jp-jetpack-grid__filler';
      filler.setAttribute('aria-hidden', 'true');
      headRow.append(filler);
      colgroup.append(document.createElement('col'));
    };
    ensureFiller();
    const layout = () => {
      table.style.minWidth = columns.reduce((sum, col) => sum + (parseFloat(col.col.style.width) || 60), 0) + 'px';
      let left = 0;
      columns.forEach((col, k) => {
        col.left = k < frozenCount ? left : null;
        col.th.classList.toggle('is-frozen', col.left !== null);
        col.th.style.left = col.left !== null ? col.left + 'px' : '';
        if (col.left !== null) left += col.th.offsetWidth;
      });
    };
    layout();

    const updateHeaders = () => columns.forEach(col => {
      const sort = sorts.find(s => s.col === col);
      if (sort) col.th.setAttribute('aria-sort', sort.dir); else col.th.removeAttribute('aria-sort');
      col.th.classList.toggle('has-filter', hasFilter(col));
    });
    const hasFilter = col => gridTokens(col.filterText, true).length > 0 || (col.filterDates?.length ?? 0) > 0;

    /* ---- pager */
    const pager = document.createElement('div');
    pager.className = 'jp-jetpack-grid__pager';
    pager.hidden = true;
    const pageButton = (name, label) =>
      `<button type="button" class="jp-jetpack-grid__pager-button" data-page="${name}" aria-label="${label}" data-jp-tool-tip="${label}"></button>`;
    pager.innerHTML = pageButton('first', 'First page') + pageButton('previous', 'Previous page') +
      '<span class="jp-jetpack-grid__page"></span>' + pageButton('next', 'Next page') + pageButton('last', 'Last page') +
      '<span class="jp-jetpack-grid__range"></span>';
    viewport.after(pager);
    pager.addEventListener('click', e => {
      const page = e.target.closest('[data-page]')?.dataset.page;
      if (!page) return;
      showPage({ first: 0, previous: pageIndex - 1, next: pageIndex + 1, last: pageCount - 1 }[page], true);
    });

    /* ---- filtering, sorting, grouping */
    const compareValues = (col, a, b) => {
      if (a === b) return 0;
      if (a == null) return -1;
      if (b == null) return 1;
      if (typeof a === 'number' && typeof b === 'number') return a - b;
      if (typeof a === 'boolean' || typeof b === 'boolean') return Number(a) - Number(b);
      if (col.type === 'date') return a < b ? -1 : a > b ? 1 : 0;
      return c.collator.compare(String(a), String(b));
    };
    const compareRows = (x, y) => {
      for (const s of sorts) {
        const r = compareValues(s.col, x.v[s.col.index], y.v[s.col.index]);
        if (r) return s.dir === 'descending' ? -r : r;
      }
      return x.i - y.i;
    };
    const groupColumn = () => groupBy ? columns.find(col => col.binding.toLowerCase() === groupBy.toLowerCase()) ?? null : null;

    function refilter(keepPage) {
      clearTimeout(timer);
      timer = 0;
      words = gridTokens(searchText, false).map(w => w.toLowerCase());
      if (itemsSource) {
        filtered = words.length > 0 || columns.some(col => col.filterable && hasFilter(col));
        updateHeaders();
        load(keepPage ? pageIndex : 0, !keepPage, true);
        return;
      }
      const searched = words.length ? columns.filter(col => col.searchable) : [];
      const filters = columns.filter(col => col.filterable && hasFilter(col)).map(col => ({
        col,
        values: gridTokens(col.filterText, true).map(t => gridFilterValue(t, c)),
        days: col.filterDates?.length ? new Set(col.filterDates.filter(d => d != null).map(d => String(d).slice(0, 10))) : null,
        blanks: col.filterDates?.includes(null) ?? false,
      }));
      const passesFilter = (f, row) => {
        const raw = row.v[f.col.index];
        if (f.days) {
          const day = f.col.type === 'date' && raw != null ? String(raw).slice(0, 10) : null;
          if (day ? !f.days.has(day) : !(raw == null && f.blanks)) return false;
        }
        if (f.values.length === 0) return true;
        let text = null;
        for (const value of f.values) {
          if (value.range) {
            const inRange = inGridRange(value.range, raw, f.col.type);
            if (inRange !== undefined) {
              if (inRange) return true;
              continue;
            }
          }
          text ??= row.t[f.col.index].trim();
          if (value.matches(text)) return true;
        }
        return false;
      };
      const passes = row => filters.every(f => passesFilter(f, row)) &&
        words.every(w => searched.some(col => lowerOf(row)[col.index].includes(w)));

      filtered = filters.length > 0 || words.length > 0;
      let list = filtered ? rows.filter(passes) : rows.slice();
      if (sorts.length) list.sort(compareRows);

      /* groups in the order of their first row, as CollectionView groups are */
      runs = null;
      const by = groupColumn();
      if (by) {
        const groups = new Map();
        for (const row of list) {
          const raw = row.v[by.index];
          const key = raw == null ? '' : typeof raw + ':' + raw;
          let g = groups.get(key);
          if (!g) groups.set(key, g = { key, name: raw == null ? '(blank)' : row.t[by.index], rows: [] });
          g.rows.push(row);
        }
        list = [];
        runs = [];
        for (const g of groups.values()) {
          runs.push({ key: g.key, name: g.name, start: list.length, count: g.rows.length });
          for (const row of g.rows) list.push(row);
        }
      }
      ordered = list;
      totalCount = rows.length;
      updateHeaders();
      showPage(keepPage ? pageIndex : 0, !keepPage);
      fire(grid, 'jp-filtered', { count: ordered.length, totalCount });
    }

    /* ---- server mode */
    /* What the server needs to send a page: a JetPackGridRequest */
    const query = extra => ({
      pageIndex,
      pageSize,
      searchText,
      groupMemberPath: groupBy,
      culture: ds.jpCulture || c.locale || '',
      columns: columns.map(col => ({
        binding: col.binding,
        stringFormat: col.stringFormat,
        isCheckBox: col.checkBox,
        trueText: col.trueText,
        falseText: col.falseText,
        isSearchable: col.searchable,
        filterText: col.filterable ? col.filterText : '',
        filterDates: col.filterable ? col.filterDates : null,
      })),
      sorts: sorts.map(s => ({ binding: s.col.binding, direction: s.dir })),
      valuesLimit: listLimit,
      ...extra,
    });
    async function ask(request) {
      if (typeof itemsSource === 'function') return itemsSource(request);
      const headers = { 'Content-Type': 'application/json', Accept: 'application/json' };
      /* MVC's [ValidateAntiForgeryToken] reads the token of the page's form */
      const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
      if (token) headers.RequestVerificationToken = token;
      const response = await fetch(itemsSource, { method: 'POST', headers, body: JSON.stringify(request), credentials: 'same-origin' });
      if (!response.ok) throw new Error(`${response.status} ${response.statusText}`);
      return response.json();
    }
    /* reads a property of a response in camelCase or PascalCase, whatever the server's JSON options */
    const prop = (o, name) => o?.[name] ?? o?.[name[0].toUpperCase() + name.slice(1)];
    /* Asks the server for a page; a newer request wins over one still on its way */
    async function load(index, scrollToTop, filterChanged) {
      const id = ++requestId;
      grid.setAttribute('aria-busy', 'true');
      try {
        const response = await ask(query({ pageIndex: Math.max(0, Number(index) || 0) }));
        if (id !== requestId) return;
        rows = (prop(response, 'rows') ?? []).map((cells, i) => toRow(cells, i));
        totalCount = prop(response, 'totalCount') ?? rows.length;
        const filteredCount = prop(response, 'filteredCount') ?? rows.length;
        const at = prop(response, 'pageIndex') ?? index;
        const groups = prop(response, 'groups');
        present({
          rows,
          groups: groups ? groups.map(g => ({ key: prop(g, 'key'), name: prop(g, 'name'), start: prop(g, 'start'), count: prop(g, 'count') })) : null,
          index: at,
          start: pageSize > 0 && filteredCount > pageSize ? at * pageSize : 0,
          filteredCount,
        }, scrollToTop);
        if (filterChanged) fire(grid, 'jp-filtered', { count: filteredCount, totalCount });
      } catch (error) {
        if (id === requestId) fire(grid, 'jp-load-failed', { error });
      } finally {
        if (id === requestId) grid.removeAttribute('aria-busy');
      }
    }

    const schedule = () => {
      clearTimeout(timer);
      if (delay <= 0) refilter(false);
      else timer = setTimeout(() => refilter(false), delay);
    };

    /* One page of the ordered rows (or all of them when they fit on one); in server mode, the page the
       server sends */
    function showPage(index, scrollToTop) {
      if (itemsSource) {
        load(index, scrollToTop, false);
        return;
      }
      const total = ordered.length;
      const pages = pageSize > 0 && total > pageSize ? Math.ceil(total / pageSize) : 1;
      const at = clamp(Number(index) || 0, 0, pages - 1);
      const start = pages > 1 ? at * pageSize : 0;
      const end = pages > 1 ? Math.min(total, start + pageSize) : total;
      present({ rows: ordered.slice(start, end), groups: runs, index: at, start, filteredCount: total }, scrollToTop);
    }

    /* Shows a page: a header in front of each group's part of it; the rows alternate within each group */
    function present(next, scrollToTop) {
      page = next;
      const total = page.filteredCount;
      pageCount = pageSize > 0 && total > pageSize ? Math.ceil(total / pageSize) : 1;
      pageIndex = clamp(page.index, 0, pageCount - 1);
      const start = page.start, end = start + page.rows.length;
      entries = [];
      if (page.groups) {
        for (const run of page.groups) {
          const from = Math.max(start, run.start), to = Math.min(end, run.start + run.count);
          if (from >= to) continue;
          const expanded = !folded.has(run.key);
          entries.push({ group: run, count: to - from, expanded });
          if (expanded) for (let k = from; k < to; k++) entries.push({ row: page.rows[k - start], alt: (k - from) % 2 === 1 });
        }
      } else {
        page.rows.forEach((row, k) => entries.push({ row, alt: k % 2 === 1 }));
      }
      /* rows that are not shown leave the selection, as rows leave a WPF DataGrid's view */
      const shown = new Set(entries.map(e => e.row));
      [...selected].forEach(row => { if (!shown.has(row)) selected.delete(row); });
      if (!shown.has(current)) current = null;
      if (!shown.has(anchor)) anchor = null;

      render();
      pager.hidden = pageCount <= 1;
      pager.querySelector('.jp-jetpack-grid__page').textContent = `Page ${fmt(pageIndex + 1)} of ${fmt(pageCount)}`;
      pager.querySelector('.jp-jetpack-grid__range').textContent = `${fmt(Math.min(start + 1, total))}–${fmt(end)} of ${fmt(total)}`;
      pager.querySelectorAll('[data-page]').forEach(btn => {
        btn.disabled = btn.dataset.page === 'first' || btn.dataset.page === 'previous' ? pageIndex === 0 : pageIndex >= pageCount - 1;
      });
      if (scrollToTop) viewport.scrollTop = 0;
      if (pendingSelectedIndex >= 0 && entries.length) {
        const row = shownRows()[pendingSelectedIndex];
        pendingSelectedIndex = -1;
        if (row) {
          selectRow(row, false, false);
          reveal(row);
        }
      }
      selectionChanged();
      if (shownPage !== -1 && shownPage !== pageIndex) fire(grid, 'jp-page-changed', { pageIndex, pageCount });
      shownPage = pageIndex;
    }

    function cellHtml(col, row) {
      const text = row.t[col.index];
      const classes = [];
      let content;
      if (col.checkBox) {
        classes.push('jp-jetpack-grid__check');
        content = `<span class="jp-check-box jp-check-box--on-light"><input type="checkbox" tabindex="-1" aria-label="${escapeHtml(text)}"${row.v[col.index] === true ? ' checked' : ''}></span>`;
      } else {
        content = escapeHtml(text);
        if (words.length && col.searchable) {
          const marked = markGridHits(text, words);
          if (marked) {
            content = marked;
            classes.push('is-hit');
          }
        }
      }
      if (col.align === 'right') classes.push('is-right');
      else if (col.align === 'center') classes.push('is-center');
      let style = '';
      if (col.left !== null && col.left !== undefined) {
        classes.push('is-frozen');
        style = ` style="left:${col.left}px"`;
      }
      return `<td${classes.length ? ` class="${classes.join(' ')}"` : ''}${style}>${content}</td>`;
    }

    function render() {
      const span = colgroup.children.length;
      let html = '';
      entries.forEach((e, k) => {
        if (e.group) {
          html += `<tr class="jp-jetpack-grid__group-row" data-entry="${k}"><td class="jp-jetpack-grid__group" colspan="${span}">` +
            `<div class="jp-jetpack-grid__group-header" role="button" aria-expanded="${e.expanded}">` +
            `<span class="jp-jetpack-grid__group-name">${escapeHtml(e.group.name)}</span>` +
            `<span class="jp-jetpack-grid__group-count">(${fmt(e.count)})</span></div></td></tr>`;
          return;
        }
        html += `<tr class="jp-jetpack-grid__row${e.alt ? ' is-alternate' : ''}${e.row === current ? ' is-current' : ''}" data-entry="${k}" aria-selected="${selected.has(e.row)}">`;
        for (const col of columns) html += cellHtml(col, e.row);
        html += '</tr>';
      });
      body.innerHTML = html;
    }

    /* ---- selection */
    const shownRows = () => entries.filter(e => e.row).map(e => e.row);
    const firstSelected = () => selected.values().next().value ?? null;
    function selectionChanged() {
      const item = firstSelected();
      if (item === lastSelected) return;
      const old = lastSelected;
      lastSelected = item;
      fire(grid, 'jp-selected-item-changed', { oldValue: itemOf(old), newValue: itemOf(item) });
    }
    function updateSelection() {
      for (const tr of body.rows) {
        const row = entries[tr.dataset.entry]?.row;
        if (!row) continue;
        tr.setAttribute('aria-selected', String(selected.has(row)));
        tr.classList.toggle('is-current', row === current);
      }
      selectionChanged();
    }
    function selectRow(row, toggle, extend) {
      if (selectionMode === 'none' || !row) return;
      if (selectionMode === 'extended' && extend && anchor) {
        const list = shownRows();
        const a = list.indexOf(anchor), b = list.indexOf(row);
        selected.clear();
        list.slice(Math.min(a, b), Math.max(a, b) + 1).forEach(r => selected.add(r));
      } else if (selectionMode === 'extended' && toggle) {
        if (selected.has(row)) selected.delete(row); else selected.add(row);
        anchor = row;
      } else {
        selected.clear();
        selected.add(row);
        anchor = row;
      }
      current = row;
      updateSelection();
    }
    /* scrolls a row into view below the sticky header */
    function reveal(row) {
      const tr = [...body.rows].find(r => entries[r.dataset.entry]?.row === row);
      if (!tr) return;
      const head = table.tHead.offsetHeight;
      if (tr.offsetTop - head < viewport.scrollTop) viewport.scrollTop = tr.offsetTop - head;
      else if (tr.offsetTop + tr.offsetHeight > viewport.scrollTop + viewport.clientHeight) viewport.scrollTop = tr.offsetTop + tr.offsetHeight - viewport.clientHeight;
    }

    function toggleGroup(entry) {
      if (entry.expanded) folded.add(entry.group.key); else folded.delete(entry.group.key);
      present(page, false);
    }

    body.addEventListener('click', e => {
      const entry = entries[e.target.closest('tr')?.dataset.entry];
      if (!entry) return;
      if (entry.group) toggleGroup(entry);
      else selectRow(entry.row, e.ctrlKey || e.metaKey, e.shiftKey);
      viewport.focus({ preventScroll: true });
    });
    /* Shift+click selects a range; keep the browser from selecting text as well */
    body.addEventListener('mousedown', e => { if (e.shiftKey && selectionMode === 'extended') e.preventDefault(); });

    viewport.addEventListener('keydown', e => {
      if (e.target !== viewport) return;
      const list = shownRows();
      if (list.length === 0) return;
      if (e.key === 'a' && (e.ctrlKey || e.metaKey) && selectionMode === 'extended') {
        list.forEach(r => selected.add(r));
        updateSelection();
        e.preventDefault();
        return;
      }
      const height = body.querySelector('tr.jp-jetpack-grid__row')?.offsetHeight || 27;
      const step = Math.max(1, Math.floor((viewport.clientHeight - table.tHead.offsetHeight) / height) - 1);
      let k = list.indexOf(current);
      switch (e.key) {
        case 'ArrowDown': k = k < 0 ? 0 : k + 1; break;
        case 'ArrowUp': k = k < 0 ? 0 : k - 1; break;
        case 'PageDown': k += step; break;
        case 'PageUp': k -= step; break;
        case 'Home': k = 0; break;
        case 'End': k = list.length - 1; break;
        default: return;
      }
      const row = list[clamp(k, 0, list.length - 1)];
      selectRow(row, false, e.shiftKey);
      reveal(row);
      e.preventDefault();
    });

    /* ---- sorting: ascending, descending and back to the source order; Shift keeps the other columns */
    function sortBy(col, add) {
      const index = sorts.findIndex(s => s.col === col);
      const dir = index < 0 ? 'ascending' : sorts[index].dir === 'ascending' ? 'descending' : null;
      const sorting = new CustomEvent('jp-sorting', { bubbles: true, cancelable: true, detail: { column: col.index, binding: col.binding, direction: dir } });
      if (!grid.dispatchEvent(sorting)) return;
      if (!add) sorts = dir ? [{ col, dir }] : [];
      else if (index >= 0) { if (dir) sorts[index].dir = dir; else sorts.splice(index, 1); }
      else sorts.push({ col, dir });
      refilter(false);
      fire(grid, 'jp-sorted', sorts.map(s => ({ column: s.col.index, binding: s.col.binding, direction: s.dir })));
    }
    headRow.addEventListener('click', e => {
      if (e.target.closest('.jp-jetpack-grid__filter-button, .jp-jetpack-grid__resizer')) return;
      const col = columns.find(x => x.th === e.target.closest('th.is-sortable'));
      if (col) sortBy(col, e.shiftKey);
    });

    /* ---- resizing: dragging a header's right edge */
    columns.forEach(col => {
      const grip = col.th.querySelector('.jp-jetpack-grid__resizer');
      grip?.addEventListener('pointerdown', e => {
        if (e.button !== 0) return;
        e.preventDefault();
        const startX = e.clientX, start = col.th.offsetWidth;
        const move = ev => {
          col.col.style.width = Math.max(MIN_COLUMN_WIDTH, Math.round(start + ev.clientX - startX)) + 'px';
          ensureFiller();
          layout();
        };
        const end = () => {
          grip.removeEventListener('pointermove', move);
          grip.removeEventListener('lostpointercapture', end);
          grid.classList.remove('is-resizing');
          if (frozenCount) render();
        };
        grip.setPointerCapture(e.pointerId);
        grip.addEventListener('pointermove', move);
        grip.addEventListener('lostpointercapture', end);
        grid.classList.add('is-resizing');
      });
    });

    /* the group headers are as wide as the viewport, and the frozen columns need the header widths */
    new ResizeObserver(() => {
      grid.style.setProperty('--jp-jetpack-grid-viewport-width', viewport.clientWidth + 'px');
      const before = columns.map(col => col.left).join();
      layout();
      if (columns.map(col => col.left).join() !== before) render();
    }).observe(viewport);

    /* ---- the filter box under a header */
    let popup = null, filterColumn = null, options = null, tree = null, treeNodes = [], filterRequest = 0;
    function writeOptions(col, input) {
      const known = new Set(options.map(o => o.text.toLowerCase()));
      const typed = gridTokens(col.filterText, true).filter(t => !known.has(t.toLowerCase()));
      const values = typed.concat(options.filter(o => o.checked).map(o => o.text))
        .map(v => /[\s,;]/.test(v) ? `"${v}"` : v);
      col.filterText = input.value = values.join(', ');
    }
    function syncOptions(col) {
      const tokens = new Set(gridTokens(col.filterText, true).map(t => t.toLowerCase()));
      options?.forEach(o => { o.checked = tokens.has(o.text.toLowerCase()); });
      popup.querySelectorAll('input[data-option]').forEach(input => { input.checked = options[input.dataset.option].checked; });
    }
    function syncTree() {
      popup.querySelectorAll('input[data-node]').forEach(input => {
        const n = treeNodes[input.dataset.node];
        input.checked = n.checked === true;
        input.indeterminate = n.checked === null;
      });
    }
    const countHtml = n => ` <span class="jp-jetpack-grid-filter__count">(${fmt(n)})</span>`;
    function treeHtml(nodes) {
      return nodes.map(n => {
        const id = treeNodes.push(n) - 1;
        const children = n.children.length && n.expanded ? `<ul role="group">${treeHtml(n.children)}</ul>` : '';
        return `<li><div class="jp-jetpack-grid-filter__node">` +
          `<button type="button" class="jp-jetpack-grid-filter__expander${n.children.length ? '' : ' is-leaf'}" data-expand="${id}" tabindex="-1" aria-label="Expand" aria-expanded="${n.expanded}"></button>` +
          `<label class="jp-check-box"><input type="checkbox" data-node="${id}"><span class="jp-jetpack-grid-filter__text">${escapeHtml(n.text)}${countHtml(n.count)}</span></label>` +
          `</div>${children}</li>`;
      }).join('');
    }
    /* The values of a column for its filter box, as the server sends them: { dates: [{ day, count }] } for a
       column of dates (day null for the rows without one), else { values: [{ text, count }] }, or nothing
       when the column has more than FilterListLimit different values */
    function valuesOf(col) {
      if (listLimit <= 0) return {};
      if (col.type === 'date') {
        const days = new Map();
        for (const row of rows) {
          const v = row.v[col.index];
          const day = v == null ? null : String(v).slice(0, 10);
          days.set(day, (days.get(day) ?? 0) + 1);
        }
        if ([...days.keys()].some(d => d !== null)) return { dates: [...days].map(([day, count]) => ({ day, count })) };
      }
      const counts = new Map();
      for (const row of rows) {
        const text = row.t[col.index].trim();
        if (!text) continue;
        const key = text.toLowerCase();
        const value = counts.get(key);
        if (value) value.count++;
        else if (counts.size === listLimit) return {};
        else counts.set(key, { text, count: 1 });
      }
      return { values: [...counts.values()] };
    }
    /* The tick list or the date tree from a column's values */
    function useValues(col, result) {
      options = tree = null;
      const dates = prop(result, 'dates'), values = prop(result, 'values');
      if (dates?.length) {
        const days = new Map();
        let blanks = 0;
        for (const d of dates) {
          const day = prop(d, 'day');
          if (day == null) blanks += prop(d, 'count'); else days.set(String(day).slice(0, 10), prop(d, 'count'));
        }
        if (days.size) {
          tree = buildDateTree(days, blanks, c);
          tickDateTree(tree, col.filterDates);
          expandPartlyTicked(tree);
          return;
        }
      }
      if (!values?.length) return;
      options = values.map(v => ({ text: String(prop(v, 'text')), count: prop(v, 'count'), checked: false }));
      /* numbers in numeric order, everything else alphabetically */
      if (options.every(o => !isNaN(parseGridNumber(o.text, c)))) options.sort((x, y) => parseGridNumber(x.text, c) - parseGridNumber(y.text, c));
      else options.sort((x, y) => c.collator.compare(x.text, y.text));
    }
    function openFilter(col) {
      if (!popup) {
        popup = document.createElement('div');
        popup.className = 'jp-jetpack-grid-filter';
        popup.popover = 'auto';
        popup.setAttribute('role', 'dialog');
        document.body.appendChild(popup);
        popup.addEventListener('toggle', e => { if (e.newState === 'closed') filterClosed(); });
        popup.addEventListener('input', e => {
          if (!e.target.matches('.jp-text-box') || !filterColumn) return;
          filterColumn.filterText = e.target.value;
          syncOptions(filterColumn);
          updateHeaders();
          schedule();
        });
        popup.addEventListener('change', e => {
          const col = filterColumn, input = e.target;
          if (!col) return;
          if (input.dataset.option !== undefined) {
            options[input.dataset.option].checked = input.checked;
            writeOptions(col, popup.querySelector('.jp-text-box'));
          } else if (input.dataset.node !== undefined) {
            const n = treeNodes[input.dataset.node];
            /* a partly ticked node clears, as the WPF check box goes from partly ticked to empty */
            setDateTick(n, n.checked === false);
            const days = tickedDates(tree);
            col.filterDates = days.length ? days : null;
            syncTree();
          } else {
            return;
          }
          updateHeaders();
          schedule();
        });
        popup.addEventListener('click', e => {
          const expander = e.target.closest('[data-expand]');
          if (expander) {
            const n = treeNodes[expander.dataset.expand];
            n.expanded = !n.expanded;
            expander.setAttribute('aria-expanded', String(n.expanded));
            let list = expander.parentElement.nextElementSibling;
            if (n.expanded && !list) {
              list = document.createElement('ul');
              list.setAttribute('role', 'group');
              list.innerHTML = treeHtml(n.children);
              expander.closest('li').append(list);
              syncTree();
            }
            if (list) list.hidden = !n.expanded;
          } else if (e.target.closest('[data-filter="clear"]') && filterColumn) {
            api.clearFilters(filterColumn.index);
            popup.querySelector('.jp-text-box').value = '';
            syncOptions(filterColumn);
            if (tree) {
              tickDateTree(tree, null);
              syncTree();
            }
          } else if (e.target.closest('[data-filter="done"]')) {
            closePopup(popup);
          }
        });
        popup.addEventListener('keydown', e => {
          if (e.key === 'Enter' && e.target.matches('.jp-text-box')) {
            closePopup(popup);
            e.preventDefault();
          }
        });
      }
      filterColumn = col;
      options = tree = null;
      fillFilter(col);
      columns.forEach(x => x.button?.setAttribute('aria-expanded', String(x === col)));
      openPopup(popup, col.th);
      const input = popup.querySelector('.jp-text-box');
      input.focus();
      input.select();
      /* the values to tick; in server mode the server reads them from every row */
      if (!itemsSource) {
        useValues(col, valuesOf(col));
        fillList(col);
        return;
      }
      if (listLimit <= 0) return;
      const id = ++filterRequest;
      ask(query({ valuesFor: col.binding }))
        .then(result => {
          if (id !== filterRequest || filterColumn !== col) return;
          useValues(col, result);
          fillList(col);
          place(popup, col.th);
        })
        .catch(error => fire(grid, 'jp-load-failed', { error }));
    }
    /* the filter box without its list, which fillList adds once the values are read */
    function fillFilter(col) {
      popup.innerHTML = `<div class="jp-jetpack-grid-filter__title">Filter ${escapeHtml(col.header)}</div>` +
        '<div class="jp-jetpack-grid-filter__list" hidden></div>' +
        '<div class="jp-text-box-host"><input class="jp-text-box" type="search" placeholder="Values, e.g. volvo, saab" aria-label="Values" autocomplete="off">' +
        '<button type="button" class="jp-text-box-host__clear" data-jp-clear aria-label="Clear" tabindex="-1">×</button></div>' +
        `<div class="jp-jetpack-grid-filter__hint">${GRID_HINT}</div>` +
        '<div class="jp-jetpack-grid-filter__buttons"><button type="button" class="jp-button" data-filter="clear">Clear</button>' +
        '<button type="button" class="jp-button jp-button--accent" data-filter="done">Done</button></div>';
      popup.querySelector('.jp-text-box').value = col.filterText;
    }
    function fillList(col) {
      treeNodes = [];
      const list = popup.querySelector('.jp-jetpack-grid-filter__list');
      list.innerHTML = options
        ? options.map((o, k) => `<label class="jp-check-box"><input type="checkbox" data-option="${k}"><span class="jp-jetpack-grid-filter__text">${escapeHtml(o.text)}${countHtml(o.count)}</span></label>`).join('')
        : tree ? `<ul class="jp-jetpack-grid-filter__tree" role="tree">${treeHtml(tree)}</ul>` : '';
      list.hidden = !options && !tree;
      if (options) syncOptions(col);
      if (tree) syncTree();
    }
    function filterClosed() {
      const col = filterColumn;
      filterColumn = null;
      col?.button?.setAttribute('aria-expanded', 'false');
      if (timer) refilter(false);
      if (popup.contains(document.activeElement) || document.activeElement === document.body) viewport.focus({ preventScroll: true });
    }
    columns.forEach(col => {
      if (!col.button) return;
      dropDownOnClick(col.button, () => filterColumn === col && popup?.matches(':popover-open'), () => openFilter(col), () => closePopup(popup));
    });

    /* ---- the search box the grid follows (SearchText bound to a TextBox's Text) */
    const box = ds.jpSearchBox ? document.getElementById(ds.jpSearchBox) : null;
    const searchInput = box?.matches('input, textarea') ? box : box?.querySelector('input');
    if (searchInput) {
      if (searchText) searchInput.value = searchText; else searchText = searchInput.value;
      searchInput.addEventListener('input', () => {
        searchText = searchInput.value;
        schedule();
      });
    }

    const columnOf = key => typeof key === 'number' ? columns[key]
      : columns.find(col => col.binding === key) ?? columns.find(col => col.header === key);

    const api = {
      get columns() { return columns.map(col => ({ header: col.header, binding: col.binding, type: col.type })); },
      /* the rows (in server mode, those of the page shown): the app's objects, or objects of each
         column's binding and value; setting them leaves server mode */
      get items() { return rows.map(itemOf); },
      set items(list) {
        itemsSource = null;
        setRows(list);
        localRows = rows;
        selected.clear();
        folded.clear();
        current = anchor = null;
        refilter(false);
      },
      /* server mode: a URL the grid posts a JetPackGridRequest to for each page, or a function
         (request) => response, or a promise of one; null goes back to the items in the page */
      get itemsSource() { return itemsSource; },
      set itemsSource(value) {
        itemsSource = value || null;
        requestId++;
        if (!itemsSource) rows = localRows;
        selected.clear();
        folded.clear();
        current = anchor = null;
        refilter(false);
      },
      get searchText() { return searchText; },
      set searchText(value) {
        searchText = value ?? '';
        if (searchInput) searchInput.value = searchText;
        schedule();
      },
      /* the sort, as SortDescriptions: [{ binding, direction: 'ascending' | 'descending' }], first column first */
      get sorts() { return sorts.map(x => ({ binding: x.col.binding, direction: x.dir })); },
      set sorts(list) {
        sorts = (list ?? []).flatMap(x => {
          const col = columnOf(x.binding);
          return col ? [{ col, dir: String(x.direction).toLowerCase() === 'descending' ? 'descending' : 'ascending' }] : [];
        });
        refilter(false);
      },
      get groupMemberPath() { return groupBy; },
      set groupMemberPath(value) {
        groupBy = value ?? '';
        folded.clear();
        refilter(false);
      },
      get isGrouped() { return page.groups !== null; },
      get pageSize() { return pageSize; },
      set pageSize(value) {
        pageSize = Math.max(0, Number(value) || 0);
        refilter(false);
      },
      get pageIndex() { return pageIndex; },
      set pageIndex(value) { showPage(value, true); },
      get pageCount() { return pageCount; },
      get isPaging() { return pageCount > 1; },
      get filteredItemCount() { return page.filteredCount; },
      get totalItemCount() { return totalCount; },
      get isFiltered() { return filtered; },
      get selectionMode() { return selectionMode; },
      set selectionMode(value) {
        selectionMode = String(value).toLowerCase();
        if (selectionMode !== 'extended' && selected.size > 1) {
          const first = firstSelected();
          selected.clear();
          if (selectionMode === 'single') selected.add(first);
        }
        if (selectionMode === 'none') selected.clear();
        updateSelection();
      },
      get selectedItem() { return itemOf(firstSelected()); },
      get selectedItems() { return [...selected].map(itemOf); },
      /* the index (in the rows shown) of a row to select, -1 for none */
      set selectedIndex(index) {
        const row = shownRows()[index];
        if (row) selectRow(row, false, false);
        else { selected.clear(); updateSelection(); }
      },
      /* reads the rows again: in server mode the page shown, else after a change to the items' values */
      refresh() { refilter(true); },
      getFilterText(column) { return columnOf(column)?.filterText ?? ''; },
      setFilterText(column, text) {
        const col = columnOf(column);
        if (!col) return;
        col.filterText = text ?? '';
        updateHeaders();
        schedule();
      },
      /* days as "yyyy-mm-dd" or Date (null for rows without a date); null for no filter */
      setFilterDates(column, dates) {
        const col = columnOf(column);
        if (!col) return;
        col.filterDates = dates?.length ? dates.map(d => d instanceof Date ? d.toISOString().slice(0, 10) : d) : null;
        updateHeaders();
        schedule();
      },
      /* clears the filter of one column, or the search and every filter */
      clearFilters(column) {
        const col = column === undefined ? null : columnOf(column);
        if (!col) {
          searchText = '';
          if (searchInput) searchInput.value = '';
        }
        (col ? [col] : columns).forEach(x => {
          x.filterText = '';
          x.filterDates = null;
        });
        refilter(false);
      },
      /* filters now instead of after SearchDelay, and goes back to the first page */
      applyFilters() { refilter(false); },
      /* the rows left after the search and the filters, in the order shown; with paging, every page's
         (in server mode only the page shown: the others are on the server) */
      getVisibleItems() { return (itemsSource ? page.rows : ordered).map(itemOf); },
      expandAllGroups() {
        folded.clear();
        present(page, false);
      },
      collapseAllGroups() {
        page.groups?.forEach(run => folded.add(run.key));
        present(page, false);
      },
    };
    grid.jetpackGrid = api;

    /* SelectedIndex is applied once the first page is shown (in server mode, when it has come) */
    pendingSelectedIndex = Number(ds.jpSelectedIndex ?? -1);
    refilter(true);
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
    ['.jp-jetpack-grid', setUpJetPackGrid],
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
    /* the API of a .jp-jetpack-grid; sets the grid up first when the page asks before DOMContentLoaded */
    jetpackGrid(el) {
      el = typeof el === 'string' ? document.getElementById(el) : el;
      if (el && !el.jetpackGrid && el.matches('.jp-jetpack-grid')) {
        (el.jpReady ??= {})['.jp-jetpack-grid'] = true;
        setUpJetPackGrid(el);
      }
      return el?.jetpackGrid ?? null;
    },
  };
})();
