from django.shortcuts import render, get_object_or_404
from .models import Category, Team, Driver, Article, GrandPrix


def get_common_context():
    categories = Category.objects.all()
    return {
        "categories": categories,
    }


def home(request):
    featured_article = Article.objects.filter(is_featured=True).select_related(
        "category", "team", "driver"
    ).first()

    latest_articles = Article.objects.select_related(
        "category", "team", "driver"
    ).order_by("-created_at")

    if featured_article:
        side_articles = latest_articles.exclude(id=featured_article.id)[:4]
    else:
        side_articles = latest_articles[:4]

    drivers = Driver.objects.select_related("team").order_by("position")[:6]
    teams = Team.objects.all()[:4]
    grand_prix_events = GrandPrix.objects.filter(is_upcoming=True).order_by("start_date")[:3]

    context = get_common_context()
    context.update({
        "page_title": "F1 Portal",
        "featured_article": featured_article,
        "side_articles": side_articles,
        "drivers": drivers,
        "teams": teams,
        "grand_prix_events": grand_prix_events,
    })

    return render(request, "pages/home.html", context)


def category_page(request, slug):
    category = get_object_or_404(Category, slug=slug)

    articles = Article.objects.filter(category=category).select_related(
        "category", "team", "driver"
    ).order_by("-created_at")

    grand_prix_events = GrandPrix.objects.filter(category=category).order_by("start_date")

    context = get_common_context()
    context.update({
        "page_title": f"{category.name} - F1 Portal",
        "category": category,
        "articles": articles,
        "grand_prix_events": grand_prix_events,
    })

    return render(request, "pages/category.html", context)


def grand_prix_detail(request, slug):
    grand_prix = get_object_or_404(GrandPrix, slug=slug)

    context = get_common_context()
    context.update({
        "page_title": f"{grand_prix.name} - F1 Portal",
        "grand_prix": grand_prix,
    })

    return render(request, "pages/grand_prix_detail.html", context)


def about(request):
    context = get_common_context()
    context.update({
        "page_title": "About - F1 Portal",
    })
    return render(request, "pages/about.html", context)


def contact(request):
    context = get_common_context()
    context.update({
        "page_title": "Contact - F1 Portal",
    })
    return render(request, "pages/contact.html", context)