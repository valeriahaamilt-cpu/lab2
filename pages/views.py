import random
from datetime import timedelta

from django.contrib.auth import get_user_model, login, logout
from django.contrib.auth.decorators import login_required
from django.core.mail import send_mail
from django.db.models import Avg
from django.shortcuts import get_object_or_404, redirect, render
from django.utils import timezone

from .forms import (
    ForgotPasswordForm,
    GrandPrixCommentForm,
    GrandPrixRatingForm,
    LoginForm,
    NewsletterSubscriberForm,
    RegisterForm,
    SetNewPasswordForm,
    TicketOrderForm,
    VerifyResetCodeForm,
)
from .models import (
    Article,
    Category,
    Driver,
    GrandPrix,
    GrandPrixComment,
    GrandPrixRating,
    NewsletterSubscriber,
    PasswordResetCode,
    Team,
    TicketOrder,
)

User = get_user_model()


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
    order_form = TicketOrderForm(prefix="order")

    comment_success = False
    newsletter_success = False
    rating_success = False
    order_success = False

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

        elif "submit_order" in request.POST:
            if not request.user.is_authenticated:
                return redirect("login")

            order_form = TicketOrderForm(request.POST, prefix="order")
            if order_form.is_valid():
                order = order_form.save(commit=False)
                order.user = request.user
                order.grand_prix = grand_prix
                order.save()
                order_success = True
                order_form = TicketOrderForm(prefix="order")

    context = get_common_context()
    context.update({
        "page_title": f"{grand_prix.name} - F1 Portal",
        "grand_prix": grand_prix,
        "comments": comments,
        "average_rating": average_rating,
        "comment_form": comment_form,
        "newsletter_form": newsletter_form,
        "rating_form": rating_form,
        "order_form": order_form,
        "comment_success": comment_success,
        "newsletter_success": newsletter_success,
        "rating_success": rating_success,
        "order_success": order_success,
    })

    return render(request, "pages/grand_prix_detail.html", context)


def register_view(request):
    if request.user.is_authenticated:
        return redirect("profile")

    form = RegisterForm()

    if request.method == "POST":
        form = RegisterForm(request.POST)
        if form.is_valid():
            user = form.save()
            login(request, user)
            return redirect("profile")
        else:
            print(form.errors)

    context = get_common_context()
    context.update({
        "page_title": "Реєстрація - F1 Portal",
        "form": form,
    })

    return render(request, "pages/register.html", context)


def login_view(request):
    if request.user.is_authenticated:
        return redirect("profile")

    form = LoginForm(request=request)

    if request.method == "POST":
        form = LoginForm(request=request, data=request.POST)
        if form.is_valid():
            login(request, form.get_user())
            return redirect("profile")

    context = get_common_context()
    context.update({
        "page_title": "Вхід - F1 Portal",
        "form": form,
    })

    return render(request, "pages/login.html", context)


def logout_view(request):
    logout(request)
    return redirect("home")


@login_required
def profile_view(request):
    if request.user.is_staff or request.user.is_superuser:
        orders = TicketOrder.objects.select_related("user", "grand_prix").order_by("-created_at")
    else:
        orders = TicketOrder.objects.filter(user=request.user).select_related("grand_prix").order_by("-created_at")

    context = get_common_context()
    context.update({
        "page_title": "Особистий кабінет - F1 Portal",
        "orders": orders,
    })

    return render(request, "pages/profile.html", context)


def forgot_password_view(request):
    form = ForgotPasswordForm()
    message = ""

    if request.method == "POST":
        form = ForgotPasswordForm(request.POST)
        if form.is_valid():
            email = form.cleaned_data["email"]
            user = User.objects.filter(email=email).first()

            if user:
                code = f"{random.randint(100000, 999999)}"
                reset_code = PasswordResetCode.objects.create(
                    user=user,
                    code=code,
                    expires_at=timezone.now() + timedelta(minutes=15),
                )

                send_mail(
                    subject="F1 Portal password reset code",
                    message=f"Ваш код для відновлення пароля: {code}",
                    from_email=None,
                    recipient_list=[email],
                    fail_silently=False,
                )

                request.session["reset_user_id"] = user.id
                request.session["reset_code_id"] = reset_code.id
                return redirect("verify_reset_code")
            else:
                message = "Користувача з таким email не знайдено."

    context = get_common_context()
    context.update({
        "page_title": "Відновлення пароля - F1 Portal",
        "form": form,
        "message": message,
    })

    return render(request, "pages/forgot_password.html", context)


def verify_reset_code_view(request):
    reset_user_id = request.session.get("reset_user_id")
    reset_code_id = request.session.get("reset_code_id")

    if not reset_user_id or not reset_code_id:
        return redirect("forgot_password")

    form = VerifyResetCodeForm()
    message = ""

    code_object = PasswordResetCode.objects.filter(
        id=reset_code_id,
        user_id=reset_user_id,
        is_used=False,
    ).first()

    if not code_object or code_object.is_expired():
        message = "Код недійсний або вже прострочений."

    if request.method == "POST":
        form = VerifyResetCodeForm(request.POST)
        if form.is_valid() and code_object and not code_object.is_expired():
            code = form.cleaned_data["code"]

            if code == code_object.code:
                request.session["password_reset_verified"] = True
                return redirect("set_new_password")
            else:
                message = "Неправильний код."

    context = get_common_context()
    context.update({
        "page_title": "Перевірка коду - F1 Portal",
        "form": form,
        "message": message,
    })

    return render(request, "pages/verify_reset_code.html", context)


def set_new_password_view(request):
    reset_user_id = request.session.get("reset_user_id")
    reset_code_id = request.session.get("reset_code_id")
    password_reset_verified = request.session.get("password_reset_verified")

    if not reset_user_id or not reset_code_id or not password_reset_verified:
        return redirect("forgot_password")

    form = SetNewPasswordForm()
    message = ""

    if request.method == "POST":
        form = SetNewPasswordForm(request.POST)
        if form.is_valid():
            password1 = form.cleaned_data["new_password1"]
            password2 = form.cleaned_data["new_password2"]

            if password1 != password2:
                message = "Паролі не співпадають."
            else:
                user = get_object_or_404(User, id=reset_user_id)
                user.set_password(password1)
                user.save()

                PasswordResetCode.objects.filter(id=reset_code_id).update(is_used=True)

                request.session.pop("reset_user_id", None)
                request.session.pop("reset_code_id", None)
                request.session.pop("password_reset_verified", None)

                return redirect("login")

    context = get_common_context()
    context.update({
        "page_title": "Новий пароль - F1 Portal",
        "form": form,
        "message": message,
    })

    return render(request, "pages/set_new_password.html", context)


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