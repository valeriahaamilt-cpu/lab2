from django.db.models import Avg
from django.shortcuts import render, get_object_or_404
from .models import (
    Category,
    Team,
    Driver,
    Article,
    GrandPrix,
    GrandPrixComment,
    NewsletterSubscriber,
    GrandPrixRating,
)
from .forms import GrandPrixCommentForm, NewsletterSubscriberForm, GrandPrixRatingForm


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

    comments = grand_prix.comments.order_by("-created_at")
    average_rating = grand_prix.ratings.aggregate(avg_score=Avg("score"))["avg_score"]

    comment_form = GrandPrixCommentForm(prefix="comment")
    newsletter_form = NewsletterSubscriberForm(prefix="newsletter")
    rating_form = GrandPrixRatingForm(prefix="rating")

    comment_success = False
    newsletter_success = False
    rating_success = False

    if request.method == "POST":
        if "submit_comment" in request.POST:
            comment_form = GrandPrixCommentForm(request.POST, prefix="comment")
            if comment_form.is_valid():
                comment = comment_form.save(commit=False)
                comment.grand_prix = grand_prix
                comment.save()
                comment_success = True
                comment_form = GrandPrixCommentForm(prefix="comment")

        elif "submit_newsletter" in request.POST:
            newsletter_form = NewsletterSubscriberForm(request.POST, prefix="newsletter")
            if newsletter_form.is_valid():
                name = newsletter_form.cleaned_data["name"]
                email = newsletter_form.cleaned_data["email"]

                NewsletterSubscriber.objects.get_or_create(
                    email=email,
                    defaults={"name": name},
                )
                newsletter_success = True
                newsletter_form = NewsletterSubscriberForm(prefix="newsletter")

        elif "submit_rating" in request.POST:
            rating_form = GrandPrixRatingForm(request.POST, prefix="rating")
            if rating_form.is_valid():
                rating = rating_form.save(commit=False)
                rating.grand_prix = grand_prix
                rating.save()
                rating_success = True
                rating_form = GrandPrixRatingForm(prefix="rating")
                average_rating = grand_prix.ratings.aggregate(avg_score=Avg("score"))["avg_score"]

    context = get_common_context()
    context.update({
        "page_title": f"{grand_prix.name} - F1 Portal",
        "grand_prix": grand_prix,
        "comments": comments,
        "average_rating": average_rating,
        "comment_form": comment_form,
        "newsletter_form": newsletter_form,
        "rating_form": rating_form,
        "comment_success": comment_success,
        "newsletter_success": newsletter_success,
        "rating_success": rating_success,
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