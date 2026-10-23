// Adds each page's second-level headings under its link in the side menu, folded like the theme's
// own submenus. Runs as a deferred script, before the theme's ready handler, which marks the current
// page active (unfolding its headings) and removes that link's href.
(function () {
  var headings = window.jetpackNavHeadings || {};
  var nav = document.getElementById('site-nav');
  if (!nav) {
    return;
  }

  nav.querySelectorAll('.nav-list-item > a.nav-list-link').forEach(function (link) {
    var item = link.parentNode;
    var list = headings[link.getAttribute('href')];
    // Pages with child pages keep their own submenu.
    if (!list || !list.length || item.querySelector(':scope > .nav-list')) {
      return;
    }

    var button = document.createElement('button');
    button.className = 'nav-list-expander btn-reset';
    button.setAttribute('aria-label', link.textContent + ' sections');
    button.setAttribute('aria-expanded', 'false');
    button.innerHTML = '<svg viewBox="0 0 24 24" aria-hidden="true"><use xlink:href="#svg-arrow-right"></use></svg>';
    item.insertBefore(button, link);

    var sub = document.createElement('ul');
    sub.className = 'nav-list';
    list.forEach(function (heading) {
      var li = document.createElement('li');
      li.className = 'nav-list-item';
      var a = document.createElement('a');
      a.className = 'nav-list-link';
      a.href = link.getAttribute('href') + '#' + heading.id;
      a.textContent = heading.title;
      li.appendChild(a);
      sub.appendChild(li);
    });
    item.appendChild(sub);
  });

  // The theme sets aria-expanded only when the button is clicked; match it for the open page.
  document.addEventListener('DOMContentLoaded', function () {
    nav.querySelectorAll('.nav-list-item.active > .nav-list-expander').forEach(function (button) {
      button.setAttribute('aria-expanded', 'true');
    });
  });
})();
