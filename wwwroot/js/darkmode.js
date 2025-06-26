(() => {
            var html = document.documentElement;
            var nav = document.getElementById('nav');
            var icon = document.getElementById('themeIcon');
            var btn = document.getElementById("themeToggle");
            var stored = localStorage.getItem("theme");

            const preferDark = window.matchMedia('(prefers-color-scheme:dark)').matches;
            let theme = stored || (preferDark ? 'dark' : 'light');

            const applyTheme = (t) => {
                html.setAttribute('data-bs-theme', t);
                localStorage.setItem('theme', t);
                icon.classList.remove('bi-moon', 'bi-sun');
                icon.classList.add(t === 'dark' ? 'bi-moon' : 'bi-sun');
                nav.classList.toggle('navbar-light', t === 'light');
                nav.classList.toggle('navbar-dark', t === 'dark');
            }

            applyTheme(theme);

            btn.addEventListener('click', () => {
                theme = html.getAttribute('data-bs-theme') === 'light' ? 'dark' : 'light';
                applyTheme(theme);
            });
        })();