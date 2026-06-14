// BornoBit Restaurant manual — nav, search, dark-mode, sidebar.
(function () {
  // Dark mode (persisted).
  var saved = localStorage.getItem('manual-theme') || 'light';
  document.documentElement.setAttribute('data-theme', saved);

  document.addEventListener('DOMContentLoaded', function () {
    var toggle = document.getElementById('themeToggle');
    function syncIcon() {
      var t = document.documentElement.getAttribute('data-theme');
      if (toggle) toggle.textContent = t === 'dark' ? '☀' : '🌙';
    }
    syncIcon();
    if (toggle) toggle.addEventListener('click', function () {
      var t = document.documentElement.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
      document.documentElement.setAttribute('data-theme', t);
      localStorage.setItem('manual-theme', t);
      syncIcon();
    });

    // Mobile sidebar toggle.
    var menu = document.getElementById('menuToggle');
    var sidebar = document.getElementById('sidebar');
    if (menu && sidebar) menu.addEventListener('click', function () { sidebar.classList.toggle('open'); });

    // Active link = current file.
    var file = (location.pathname.split('/').pop() || 'index.html');
    document.querySelectorAll('.navlist a').forEach(function (a) {
      if (a.getAttribute('href') === file) a.classList.add('active');
    });

    // Client-side nav search/filter.
    var search = document.getElementById('navSearch');
    if (search) search.addEventListener('input', function () {
      var q = this.value.trim().toLowerCase();
      document.querySelectorAll('.navlist li').forEach(function (li) {
        if (li.classList.contains('group')) return;
        var txt = li.textContent.toLowerCase();
        li.classList.toggle('hidden', q && txt.indexOf(q) === -1);
      });
    });
  });
})();
