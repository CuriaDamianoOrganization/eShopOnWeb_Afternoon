(function () {
    var storageKey = "eshop-theme";

    function systemTheme() {
        if (window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches) {
            return "dark";
        }

        return "light";
    }

    function storedTheme() {
        try {
            var value = localStorage.getItem(storageKey);
            if (value === "light" || value === "dark") {
                return value;
            }
        } catch (error) {
            return null;
        }

        return null;
    }

    function resolveTheme() {
        return storedTheme() || systemTheme();
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute("data-theme", theme);
        document.documentElement.style.colorScheme = theme;

        var button = document.getElementById("theme-toggle");
        if (!button) {
            return;
        }

        var nextTheme = theme === "dark" ? "light" : "dark";
        button.setAttribute("aria-pressed", theme === "dark" ? "true" : "false");
        button.setAttribute("aria-label", "Switch to " + nextTheme + " theme");

        var label = button.querySelector(".esh-theme-toggle-label");
        if (label) {
            label.textContent = theme === "dark" ? "Light theme" : "Dark theme";
        }
    }

    function toggleTheme() {
        var nextTheme = document.documentElement.getAttribute("data-theme") === "dark" ? "light" : "dark";
        try {
            localStorage.setItem(storageKey, nextTheme);
        } catch (error) {
            // Ignore storage failures and still apply the selected theme for this page.
        }

        applyTheme(nextTheme);
    }

    function refresh() {
        applyTheme(document.documentElement.getAttribute("data-theme") || resolveTheme());
    }

    document.addEventListener("click", function (event) {
        var button = event.target.closest && event.target.closest("#theme-toggle");
        if (!button) {
            return;
        }

        event.preventDefault();
        toggleTheme();
    });

    if (window.matchMedia) {
        window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", function () {
            if (!storedTheme()) {
                applyTheme(systemTheme());
            }
        });
    }

    window.eshopTheme = {
        refresh: refresh,
        toggle: toggleTheme
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", refresh);
    } else {
        refresh();
    }
})();
