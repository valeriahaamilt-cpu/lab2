console.log("F1 Portal loaded");

document.addEventListener("DOMContentLoaded", function () {
    const table = document.getElementById("standingsTable");
    const button = document.getElementById("showStandingsBtn");

    if (table && button) {
        button.addEventListener("click", function () {
            table.classList.toggle("standings-open");

            if (table.classList.contains("standings-open")) {
                button.textContent = "Show less";
            } else {
                button.textContent = "Show all";
            }
        });
    }

    const featuredCard = document.getElementById("featuredNewsCard");
    const featuredImage = document.getElementById("featuredNewsImage");
    const featuredTitle = document.getElementById("featuredNewsTitle");
    const featuredTag = document.getElementById("featuredNewsTag");
    const sideNewsCards = document.querySelectorAll(".side-news-card");

    sideNewsCards.forEach(function (card) {
        card.addEventListener("mouseenter", function () {
            if (!featuredCard || !featuredImage || !featuredTitle || !featuredTag) {
                return;
            }

            const title = card.dataset.title;
            const image = card.dataset.image;
            const url = card.dataset.url;
            const tag = card.dataset.tag;

            if (title) {
                featuredTitle.textContent = title;
            }

            if (image) {
                featuredImage.src = image;
                featuredImage.alt = title;
            }

            if (url) {
                featuredCard.href = url;
            }

            if (tag) {
                featuredTag.textContent = tag;
            }
        });
    });
});