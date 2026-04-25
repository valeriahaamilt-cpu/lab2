console.log("F1 Portal loaded");

document.addEventListener("DOMContentLoaded", function () {
    const table = document.getElementById("standingsTable");
    const button = document.getElementById("showStandingsBtn");

    if (!table || !button) return;

    button.addEventListener("click", function () {
        table.classList.toggle("standings-open");

        if (table.classList.contains("standings-open")) {
            button.textContent = "Show less";
        } else {
            button.textContent = "Show all";
        }
    });
});