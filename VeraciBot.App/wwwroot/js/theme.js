(function () {
    const storageKey = "veracibot.theme";
    let applyingTheme = false;

    function normalize(theme) {
        return theme === "dark" ? "dark" : "light";
    }

    function getPreferredTheme() {
        const storedTheme = localStorage.getItem(storageKey);

        if (storedTheme === "dark" || storedTheme === "light") {
            return storedTheme;
        }

        return window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches
            ? "dark"
            : "light";
    }

    function getAppliedTheme() {
        const theme = document.documentElement.getAttribute("data-bs-theme")
            || (document.body && document.body.getAttribute("data-bs-theme"));

        return normalize(theme || "light");
    }

    function applyTheme(theme) {
        const normalizedTheme = normalize(theme);
        applyingTheme = true;

        document.documentElement.setAttribute("data-bs-theme", normalizedTheme);

        if (document.body) {
            document.body.setAttribute("data-bs-theme", normalizedTheme);
        }

        window.setTimeout(function () {
            applyingTheme = false;
        }, 0);

        return normalizedTheme;
    }

    function initTheme() {
        return applyTheme(getPreferredTheme());
    }

    window.veraciBotTheme = {
        init: initTheme,
        get: function () {
            return getAppliedTheme();
        },
        set: function (theme) {
            const normalizedTheme = applyTheme(theme);
            localStorage.setItem(storageKey, normalizedTheme);
            return normalizedTheme;
        },
        toggle: function () {
            const nextTheme = getAppliedTheme() === "dark" ? "light" : "dark";
            localStorage.setItem(storageKey, nextTheme);
            return applyTheme(nextTheme);
        }
    };

    initTheme();
    document.addEventListener("DOMContentLoaded", initTheme);
    document.addEventListener("enhancedload", initTheme);
    window.addEventListener("pageshow", initTheme);

    new MutationObserver(function () {
        if (applyingTheme) {
            return;
        }

        const preferredTheme = getPreferredTheme();
        const appliedTheme = getAppliedTheme();

        if (appliedTheme !== preferredTheme) {
            applyTheme(preferredTheme);
        }
    }).observe(document.documentElement, {
        attributes: true,
        attributeFilter: ["data-bs-theme"]
    });
})();
