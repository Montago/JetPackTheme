// GallerySnapshot.cs for the web: with ?shot=<name>, shows that sample, opens what is marked
// data-demo-open (a drop-down, menu, tool tip or validation call-out) and draws the texture behind
// the sample and its popups. Everything else on the page stays magenta, so tools/web_snapshots.py
// cuts the picture out by colour.
(function () {
  'use strict';

  const name = document.body.dataset.shot;
  if (!name) return;
  const sample = document.querySelector(`.gallery-sample[data-shot="${CSS.escape(name)}"]`);
  if (!sample) return;
  sample.classList.add('is-shot');

  const center = el => {
    const r = el.getBoundingClientRect();
    return { clientX: Math.round(r.left + Math.min(r.width / 2, 24)), clientY: Math.round(r.top + r.height / 2) };
  };
  // A mouse click as jetpack.js sees it: the state at pointerdown decides (dropDownOnClick)
  const click = el => {
    el.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, ...center(el) }));
    el.dispatchEvent(new MouseEvent('click', { bubbles: true, detail: 1, ...center(el) }));
  };
  const hover = el => {
    const at = center(el);
    document.dispatchEvent(new PointerEvent('pointermove', { bubbles: true, ...at }));
    el.dispatchEvent(new PointerEvent('pointerover', { bubbles: true, ...at }));
  };

  function open(el) {
    if (el.matches('select.jp-combo-box, .jp-color-picker')) {
      click(el);
    } else if (el.matches('.jp-date-picker, .jp-time-picker, .jp-search-combo-box')) {
      click(el.querySelector('.jp-text-box-button'));
    } else if (el.matches('.jp-auto-suggest-box')) {
      const input = el.querySelector('input');
      input.value = el.dataset.demoOpen;
      input.dispatchEvent(new Event('input', { bubbles: true }));
    } else if (el.matches('.jp-split-button, .jp-drop-down-button, .jp-menu')) {
      el.querySelector('[popovertarget]').click();
    } else if (el.matches('[data-jp-context-menu]')) {
      const r = el.getBoundingClientRect();
      el.dispatchEvent(new MouseEvent('contextmenu', { bubbles: true, cancelable: true, clientX: Math.round(r.left + 60), clientY: Math.round(r.top + 30) }));
    } else if (el.matches('.jp-scroll-viewer--thin')) {
      el.classList.add('is-active');
    } else if (el.matches('[data-jp-error]')) {
      hover(el);
    } else if (el.matches('[data-jp-tool-tip]')) {
      hover(el);
    }
  }

  document.addEventListener('DOMContentLoaded', () => {
    // jetpack.js sets the controls up on DOMContentLoaded too, before this (it was loaded first)
    sample.querySelectorAll('[data-demo-open]').forEach(open);
    // after the tool tip's 500 ms
    setTimeout(() => {
      let { left, top, right, bottom } = sample.getBoundingClientRect();
      const pad = 12;
      document.querySelectorAll(':popover-open, .jp-tool-tip.is-floating, .jp-validation-tip').forEach(p => {
        const r = p.getBoundingClientRect();
        left = Math.min(left, r.left - pad);
        top = Math.min(top, r.top - pad);
        right = Math.max(right, r.right + pad);
        bottom = Math.max(bottom, r.bottom + pad);
      });
      const backdrop = document.getElementById('GalleryBackdrop');
      Object.assign(backdrop.style, {
        left: Math.floor(left) + 'px',
        top: Math.floor(top) + 'px',
        width: Math.ceil(right - Math.floor(left)) + 'px',
        height: Math.ceil(bottom - Math.floor(top)) + 'px',
      });
    }, 800);
  });
})();
