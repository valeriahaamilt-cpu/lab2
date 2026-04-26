from django.urls import path
from . import views

urlpatterns = [
    path("", views.home, name="home"),
    path("category/<slug:slug>/", views.category_page, name="category_page"),
    path("driver/<slug:slug>/", views.driver_detail, name="driver_detail"),
    path("team/<slug:slug>/", views.team_detail, name="team_detail"),
    path("article/<slug:slug>/", views.article_detail, name="article_detail"),
    path("grand-prix/<slug:slug>/", views.grand_prix_detail, name="grand_prix_detail"),

    path("register/", views.register_view, name="register"),
    path("login/", views.login_view, name="login"),
    path("logout/", views.logout_view, name="logout"),
    path("profile/", views.profile_view, name="profile"),

    path("forgot-password/", views.forgot_password_view, name="forgot_password"),
    path("verify-reset-code/", views.verify_reset_code_view, name="verify_reset_code"),
    path("set-new-password/", views.set_new_password_view, name="set_new_password"),

    path("about/", views.about, name="about"),
    path("contact/", views.contact, name="contact"),
]