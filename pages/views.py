import random
from datetime import timedelta
import hashlib
import hmac
import json
import requests

from django.http import HttpResponse, HttpResponseBadRequest
from django.urls import reverse
from django.views.decorators.csrf import csrf_exempt

from django.conf import settings
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
    HomeQuickLink,
)

User = get_user_model()


def get_common_context():
    today = timezone.localdate()

    categories = Category.objects.all().order_by("created_at")

    drivers_menu = Driver.objects.select_related("team").order_by("name")

    teams_menu = Team.objects.all().order_by("name")

    schedule_previous = GrandPrix.objects.filter(
        end_date__lt=today
    ).order_by("-end_date").first()

    schedule_next = GrandPrix.objects.filter(
        start_date__gte=today
    ).order_by("start_date").first()

    schedule_upcoming = GrandPrix.objects.filter(
        start_date__gt=today
    ).order_by("start_date")

    news_menu_articles = Article.objects.select_related(
    "category", "team", "driver"
    ).order_by("-created_at")[:4]

    news_menu_featured = Article.objects.filter(
        is_featured=True
    ).select_related(
    "category", "team", "driver"
    ).first()

    if schedule_next:
        schedule_upcoming = schedule_upcoming.exclude(id=schedule_next.id)

    schedule_upcoming = schedule_upcoming[:2]

    return {
        "categories": categories,
        "schedule_previous": schedule_previous,
        "schedule_next": schedule_next,
        "schedule_upcoming": schedule_upcoming,
        "news_menu_articles": news_menu_articles,
        "news_menu_featured": news_menu_featured,
        "drivers_menu": drivers_menu,
        "teams_menu": teams_menu,
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

    drivers = Driver.objects.select_related("team").order_by("position")
    teams = Team.objects.all().order_by("name")
    grand_prix_events = GrandPrix.objects.filter(is_upcoming=True).order_by("start_date")[:3]
    quick_links = HomeQuickLink.objects.filter(is_active=True).order_by("order")
    context = get_common_context()
    context.update({
        "page_title": "F1 Portal",
        "featured_article": featured_article,
        "side_articles": side_articles,
        "drivers": drivers,
        "teams": teams,
        "grand_prix_events": grand_prix_events,
        "quick_links": quick_links,
    })
    
    return render(request, "pages/home.html", context)


def category_page(request, slug):
    category = get_object_or_404(Category, slug=slug)

    context = get_common_context()
    context.update({
        "page_title": f"{category.name} - F1 Portal",
        "category": category,
    })

    if slug == "drivers":
        drivers = Driver.objects.select_related("team").order_by("position")
        context.update({
            "drivers": drivers,
        })
        return render(request, "pages/drivers.html", context)
    
    if slug == "results":
        drivers = Driver.objects.select_related("team").order_by("position")
        context.update({
            "drivers": drivers,
        })
        return render(request, "pages/results.html", context)

    if slug == "teams":
        teams = Team.objects.all().order_by("name")
        context.update({
            "teams": teams,
        })
        return render(request, "pages/teams.html", context)

    articles = Article.objects.filter(category=category).select_related(
        "category", "team", "driver"
    ).order_by("-created_at")

    grand_prix_events = GrandPrix.objects.filter(category=category).order_by("start_date")

    context.update({
        "articles": articles,
        "grand_prix_events": grand_prix_events,
    })

    return render(request, "pages/category.html", context)


def article_detail(request, slug):
    article = get_object_or_404(
        Article.objects.select_related("category", "team", "driver"),
        slug=slug
    )

    related_articles = Article.objects.filter(
        category=article.category
    ).exclude(id=article.id).select_related(
        "category", "team", "driver"
    ).order_by("-created_at")[:4]

    context = get_common_context()
    context.update({
        "page_title": f"{article.title} - F1 Portal",
        "article": article,
        "related_articles": related_articles,
    })

    return render(request, "pages/article_detail.html", context)


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
                return redirect("choose_payment", order_id=order.id)
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

    context = get_common_context()
    context.update({
        "page_title": "Sign Up - F1 Portal",
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
        "page_title": "Log In - F1 Portal",
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
        "page_title": "My Account - F1 Portal",
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
                    message=f"Your password reset code is: {code}. The code is valid for 15 minutes.",
                    from_email=settings.DEFAULT_FROM_EMAIL,
                    recipient_list=[email],
                    fail_silently=False,
                )

                request.session["reset_user_id"] = user.id
                request.session["reset_code_id"] = reset_code.id
                return redirect("verify_reset_code")
            else:
                message = "User with this email was not found."

    context = get_common_context()
    context.update({
        "page_title": "Reset Password - F1 Portal",
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
        message = "The code is invalid or has expired."

    if request.method == "POST":
        form = VerifyResetCodeForm(request.POST)
        if form.is_valid() and code_object and not code_object.is_expired():
            code = form.cleaned_data["code"]

            if code == code_object.code:
                request.session["password_reset_verified"] = True
                return redirect("set_new_password")
            else:
                message = "Incorrect code."

    context = get_common_context()
    context.update({
        "page_title": "Verify Reset Code - F1 Portal",
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
                message = "Passwords do not match."
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
        "page_title": "Set New Password - F1 Portal",
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

def driver_detail(request, slug):
    driver = get_object_or_404(
        Driver.objects.select_related("team"),
        slug=slug
    )

    context = get_common_context()
    context.update({
        "page_title": f"{driver.name} - F1 Portal",
        "driver": driver,
    })

    return render(request, "pages/driver_detail.html", context)


def team_detail(request, slug):
    team = get_object_or_404(Team, slug=slug)

    team_drivers = Driver.objects.filter(team=team).order_by("position")

    team_articles = Article.objects.filter(team=team).select_related(
        "category", "team", "driver"
    ).order_by("-created_at")[:4]

    context = get_common_context()
    context.update({
        "page_title": f"{team.name} - F1 Portal",
        "team": team,
        "team_drivers": team_drivers,
        "team_articles": team_articles,
    })

    return render(request, "pages/team_detail.html", context)

@login_required
def pay_order_crypto(request, order_id):
    order = get_object_or_404(
        TicketOrder.objects.select_related("grand_prix", "user"),
        id=order_id,
        user=request.user,
    )

    if order.status == "paid":
        return redirect("profile")

    callback_url = request.build_absolute_uri(reverse("nowpayments_ipn"))
    success_url = request.build_absolute_uri(reverse("profile"))
    cancel_url = request.build_absolute_uri(
        reverse("grand_prix_detail", args=[order.grand_prix.slug])
    )

    payload = {
        "price_amount": float(order.total_price),
        "price_currency": "usd",
        "order_id": str(order.id),
        "order_description": f"Ticket for {order.grand_prix.name}",
        "ipn_callback_url": callback_url,
        "success_url": success_url,
        "cancel_url": cancel_url,
    }

    headers = {
        "x-api-key": settings.NOWPAYMENTS_API_KEY,
        "Content-Type": "application/json",
    }

    response = requests.post(
        "https://api.nowpayments.io/v1/invoice",
        headers=headers,
        json=payload,
        timeout=20,
    )

    if response.status_code not in [200, 201]:
        return HttpResponseBadRequest("Payment invoice was not created.")

    data = response.json()

    order.payment_id = str(data.get("id", ""))
    order.payment_url = data.get("invoice_url", "")
    order.status = "waiting"
    order.save()

    return redirect(order.payment_url)


@csrf_exempt
def nowpayments_ipn(request):
    if request.method != "POST":
        return HttpResponseBadRequest("Invalid request method.")

    received_signature = request.headers.get("x-nowpayments-sig")

    if not received_signature:
        return HttpResponseBadRequest("Missing signature.")

    try:
        body_data = json.loads(request.body.decode("utf-8"))
    except json.JSONDecodeError:
        return HttpResponseBadRequest("Invalid JSON.")

    sorted_body = json.dumps(body_data, separators=(",", ":"), sort_keys=True)

    calculated_signature = hmac.new(
        settings.NOWPAYMENTS_IPN_SECRET.encode("utf-8"),
        sorted_body.encode("utf-8"),
        hashlib.sha512,
    ).hexdigest()

    if not hmac.compare_digest(calculated_signature, received_signature):
        return HttpResponseBadRequest("Invalid signature.")

    order_id = body_data.get("order_id")
    payment_status = body_data.get("payment_status")

    order = TicketOrder.objects.filter(id=order_id).first()

    if not order:
        return HttpResponseBadRequest("Order not found.")

    if payment_status in ["finished", "confirmed", "sending"]:
        order.status = "paid"
    elif payment_status in ["failed", "expired", "refunded"]:
        order.status = "failed"
    else:
        order.status = "waiting"

    order.payment_id = str(body_data.get("payment_id", order.payment_id))
    order.save()

    return HttpResponse("OK")

@login_required
def choose_payment(request, order_id):
    order = get_object_or_404(
        TicketOrder.objects.select_related("grand_prix", "user"),
        id=order_id,
        user=request.user,
    )

    context = get_common_context()
    context.update({
        "page_title": "Choose Payment - F1 Portal",
        "order": order,
    })

    return render(request, "pages/choose_payment.html", context)

@login_required
def card_payment(request, order_id):
    order = get_object_or_404(
        TicketOrder.objects.select_related("grand_prix", "user"),
        id=order_id,
        user=request.user,
    )

    error = ""

    if request.method == "POST":
        card_number = request.POST.get("card_number", "").replace(" ", "")
        expiry = request.POST.get("expiry", "")
        cvv = request.POST.get("cvv", "")
        cardholder = request.POST.get("cardholder", "")

        if not card_number.isdigit() or len(card_number) not in [16, 19]:
            error = "Invalid card number."
        elif not cvv.isdigit() or len(cvv) not in [3, 4]:
            error = "Invalid CVV."
        elif not expiry:
            error = "Expiry date is required."
        elif not cardholder.strip():
            error = "Cardholder name is required."
        else:
            order.status = "paid"
            order.payment_id = f"FAKE-CARD-{order.id}"
            order.save()
            return redirect("profile")

    context = get_common_context()
    context.update({
        "page_title": "Card Payment - F1 Portal",
        "order": order,
        "error": error,
    })

    return render(request, "pages/card_payment.html", context)