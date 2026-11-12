// Views/Demo/LightDark.cshtml: the filter buttons, and a decision plus note per row that
// stay in this browser (localStorage) and "Copy decisions" turns into Markdown.
(function () {
  'use strict';

  const body = document.body;
  const rows = [...document.querySelectorAll('.ld-row')];
  const storeKey = 'jetpack.dark-version.v1';
  const settingsKey = 'jetpack.dark-version.settings.v1';

  const load = key => { try { return JSON.parse(localStorage.getItem(key)) || {}; } catch { return {}; } };
  const save = (key, value) => { try { localStorage.setItem(key, JSON.stringify(value)); } catch { /* private window */ } };

  // ------------------------------------------------------- filter
  const settings = Object.assign({ filter: 'all' }, load(settingsKey));
  const segs = [...document.querySelectorAll('.ld-seg')].map(b => [b, ...Object.entries(b.dataset)[0]]);
  function apply() {
    body.dataset.filter = settings.filter;
    segs.forEach(([b, k, v]) => b.setAttribute('aria-pressed', String(settings[k] === v)));
    save(settingsKey, settings);
  }
  segs.forEach(([b, k, v]) => b.addEventListener('click', () => {
    settings[k] = v;
    apply();
  }));
  apply();

  // ------------------------------------------------------- counts
  const counts = { exists: 0, missing: 0, dark: 0, neutral: 0 };
  rows.forEach(r => counts[r.dataset.status]++);
  document.getElementById('LdCounts').textContent =
    `${rows.length} controls · ${counts.missing} no dark variant · ${counts.exists} dark variant exists · ${counts.dark} already dark · ${counts.neutral} fit both`;

  // ---------------------------------------------------- decisions
  const decisions = load(storeKey);
  rows.forEach(row => {
    const shot = row.dataset.shot;
    const select = row.querySelector('.ld-decision');
    const note = row.querySelector('.ld-comment');
    const d = decisions[shot] || {};
    select.value = d.decision || '';
    note.value = d.note || '';
    const update = () => {
      decisions[shot] = { decision: select.value, note: note.value.trim() };
      if (!decisions[shot].decision && !decisions[shot].note) delete decisions[shot];
      row.classList.toggle('is-decided', !!select.value);
      save(storeKey, decisions);
    };
    select.addEventListener('change', update);
    note.addEventListener('input', update);
    row.classList.toggle('is-decided', !!select.value);
  });

  const statusText = { exists: 'dark variant exists', missing: 'no dark variant', dark: 'already dark', neutral: 'fits both' };
  function markdown() {
    const lines = ['| Control | Today | Decision | Note |', '| --- | --- | --- | --- |'];
    rows.forEach(r => {
      const d = decisions[r.dataset.shot];
      if (!d) return;
      const cell = s => (s || '').replace(/\|/g, '\\|');
      lines.push(`| ${r.dataset.name} | ${statusText[r.dataset.status]} | ${cell(d.decision)} | ${cell(d.note)} |`);
    });
    return lines.length > 2 ? lines.join('\n') : 'No decisions yet.';
  }

  const dialog = document.getElementById('LdExport');
  document.getElementById('LdCopy').addEventListener('click', async () => {
    const text = markdown();
    document.getElementById('LdExportText').value = text;
    try { await navigator.clipboard.writeText(text); } catch { /* the text area still has it */ }
    dialog.showModal();
  });
  dialog.querySelectorAll('[data-close]').forEach(b => b.addEventListener('click', () => dialog.close()));
})();
