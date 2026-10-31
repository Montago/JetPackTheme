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

  /* ---------------------------------------------------------------- GridPage */
  const cars = $('Cars') && JetPack.jetpackGrid('Cars');
  if (cars) {
    const showStatus = () => {
      $('GridStatus').textContent = `Showing ${cars.filteredItemCount.toLocaleString('en-US')} of ${cars.totalItemCount.toLocaleString('en-US')} cars`;
    };
    const showSelection = car => {
      $('GridSelection').textContent = car ? `Selected: ${car.StockNo}, ${car.Make} ${car.Model} ${car.Year}` : 'Nothing selected';
    };
    on($('Cars'), 'jp-filtered', showStatus);
    on($('Cars'), 'jp-selected-item-changed', e => showSelection(e.detail.newValue));
    showStatus();
    showSelection(cars.selectedItem);

    // GroupMemberPath and PageSize are bound to the combo boxes in GridPage.xaml
    on($('GroupBy'), 'change', e => { cars.groupMemberPath = e.target.value; });
    on($('PageSize'), 'change', e => { cars.pageSize = Number(e.target.value); });
    on(document.querySelector('#Multi'), 'change', e => { cars.selectionMode = e.target.checked ? 'Extended' : 'Single'; });
    // Server mode (web only): the same rows, a page at a time from JetPackGridServer.Query
    on(document.querySelector('#ServerMode'), 'change', e => {
      cars.itemsSource = e.target.checked ? e.target.closest('[data-url]').dataset.url : null;
    });
    on($('Cars'), 'jp-load-failed', e => { $('GridStatus').textContent = `The server did not answer: ${e.detail.error.message}`; });
    on($('Marks'), 'change', e => {
      // The theme gives the accent and the grey; local values override them
      const grid = $('Cars');
      grid.style.removeProperty('--jp-jetpack-grid-highlight-foreground');
      grid.style.removeProperty('--jp-jetpack-grid-highlight-cell-background');
      if (e.target.selectedIndex === 1) grid.style.setProperty('--jp-jetpack-grid-highlight-cell-background', 'transparent');
      if (e.target.selectedIndex === 2) grid.style.setProperty('--jp-jetpack-grid-highlight-foreground', 'firebrick');
    });
    on($('ClearFilters'), 'click', () => cars.clearFilters());
    on($('CopyVisible'), 'click', async () => {
      // getVisibleItems gives the rows left after the search and filters, in the order shown
      const text = 'Stock no.\tMake\tModel\tYear\tPrice\n' +
        cars.getVisibleItems().map(c => `${c.StockNo}\t${c.Make}\t${c.Model}\t${c.Year}\t${c.Price}\n`).join('');
      try {
        await navigator.clipboard.writeText(text);
        $('GridStatus').textContent = `Copied ${cars.filteredItemCount.toLocaleString('en-US')} rows to the clipboard`;
      } catch {
        $('GridStatus').textContent = 'The browser did not allow copying';
      }
    });
  }

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
