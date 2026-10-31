// The code-behind of the WPF demo: MainWindow.xaml.cs and the pages' .xaml.cs files.
(function () {
  'use strict';

  const $ = id => document.getElementById(id);
  const on = (el, name, fn) => el && el.addEventListener(name, fn);

  /* ------------------------------------------------------------ MainWindow */
  const pages = $('Pages');
  const host = document.querySelector('.demo-pages-host');

  // Page: <name> in the status bar
  on(pages, 'jp-change', e => {
    if (e.target === pages) $('PageName').textContent = e.detail.header;
  });

  document.addEventListener('change', e => {
    const input = e.target;
    if (!(input instanceof HTMLInputElement) || !input.checked && input.type === 'radio') return;
    switch (input.name) {
      case 'Size':
        JetPack.ThemeManager.controlFontSize = Number(input.value);
        break;
      case 'Navigation': {
        // Tab strip ↔ the full-width navigation bar under the title bar
        const bar = input.value === 'Bar';
        pages.classList.toggle('jp-tab-control--strip', !bar);
        pages.classList.toggle('jp-tab-control--navigation', bar);
        host.classList.toggle('is-bar', bar);
        window.dispatchEvent(new Event('resize'));
        break;
      }
      case 'Notch':
        pages.dataset.selectionAnimation = input.value;
        break;
      case 'Transition':
        pages.dataset.contentTransition = input.value;
        break;
    }
  });

  on($('CustomAccent'), 'jp-change', e => JetPack.ThemeManager.changeAccent(e.detail));
  on($('VignetteSwitch'), 'change', e => {
    document.querySelectorAll('canvas.jp-vignette').forEach(c => { c.hidden = !e.target.checked; });
  });

  /* -------------------------------------------------------------- CorePage */
  let repeats = 0;
  on($('Repeat'), 'click', () => { $('Repeat').textContent = `Repeat (${++repeats})`; });
})();

// The pages' code-behind
(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  const on = (el, name, fn) => el && el.addEventListener(name, fn);

  /* ------------------------------------------------------------- ToolkitPage */
  on($('BusySwitch'), 'change', e => {
    const busy = $('Busy');
    busy.dataset.busy = String(e.target.checked);
    busy.setAttribute('aria-busy', String(e.target.checked));
  });

  /* -------------------------------------------------------------- ExtrasPage */
  const company = $('Company');
  const showCompany = detail => {
    const item = detail?.selectedItem;
    $('CompanyValue').textContent = `SelectedValue: ${item ? item.Id : '–'} · ${item ? item.City : ''}`;
  };
  if (company) {
    const items = JSON.parse(company.querySelector('script[type="application/json"]').textContent);
    showCompany({ selectedItem: items.find(c => String(c.Id) === company.dataset.jpSelectedValue) });
    on(company, 'jp-change', e => showCompany(e.detail));
  }
  const money = n => '$' + Math.round(n).toLocaleString('en-US');
  on($('Range'), 'jp-change', e => { $('RangeText').textContent = `${money(e.detail.lowerValue)} – ${money(e.detail.upperValue)}`; });

  /* -------------------------------------------------------------- WindowPage */
  document.addEventListener('change', e => {
    if (e.target.name === 'TitleBar' && e.target.checked) {
      document.body.dataset.titleBar = e.target.value;
    }
  });
  on($('ShowDialog'), 'click', () => JetPack.showMessage('Timesheet sent', 'Your timesheet for week 43 has been sent to Ida Kragh for approval.'));
  on($('ShowDelete'), 'click', async () => {
    const result = await JetPack.showDialog({
      title: 'Delete company?',
      content: 'Contoso Corporation and 3 linked placements will be deleted permanently. Timesheets that have already been billed stay in the archive.',
      affirmativeText: 'Delete',
      negativeText: 'Cancel',
      affirmativeStyle: 'orange',
    });
    if (result === 'affirmative') JetPack.showMessage('Deleted', 'Contoso Corporation has been deleted.');
  });
  on($('ShowSaved'), 'click', () => {
    const seconds = Number(document.querySelector('#NotificationSeconds input, input#NotificationSeconds')?.value) || 0;
    const saved = $('SavedFlyout');
    if (seconds > 0) saved.dataset.jpAutoClose = String(seconds * 1000); else delete saved.dataset.jpAutoClose;
    JetPack.openFlyout(saved);
  });

  /* ------------------------------------------------------------- PalettePage */
  const paletteGroups = [...document.querySelectorAll('.demo-palette-group')];
  on($('PaletteSearch'), 'input', e => {
    const text = e.target.value.trim().toLowerCase();
    let shown = 0;
    paletteGroups.forEach(group => {
      const swatches = [...group.querySelectorAll('.demo-swatch')];
      swatches.forEach(s => { s.hidden = text.length > 0 && !s.dataset.key.toLowerCase().includes(text); });
      const visible = swatches.filter(s => !s.hidden).length;
      group.hidden = visible === 0;
      shown += visible;
    });
    const count = $('PaletteCount'), total = Number(count.dataset.total);
    count.textContent = shown === total ? `${total} brushes and colours` : `${shown} of ${total}`;
  });

  /* --------------------------------------------------------------- IconsPage */
  const cards = [...document.querySelectorAll('.demo-icon-card')];
  on($('IconSearch'), 'input', e => {
    const text = e.target.value.trim().toLowerCase();
    cards.forEach(c => { c.hidden = text.length > 0 && !c.dataset.kind.toLowerCase().includes(text); });
  });
  document.addEventListener('change', e => {
    if (e.target.name === 'IconSize') $('IconCards').style.setProperty('--demo-icon-size', e.target.value + 'px');
  });
  cards.forEach(card => on(card, 'click', async () => {
    const markup = `<jp-icon kind="${card.dataset.kind}" />`;
    try {
      await navigator.clipboard.writeText(markup);
      $('IconCopied').textContent = 'Copied ' + markup;
    } catch {
      $('IconCopied').textContent = markup;
    }
  }));
})();
