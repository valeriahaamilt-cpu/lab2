from django.urls import path
from . import views

urlpatterns = [
    path("", views.home, name="home"),
    path("category/<slug:slug>/", views.category_page, name="category_page"),
    path("grand-prix/<slug:slug>/", views.grand_prix_detail, name="grand_prix_detail"),
    path("about/", views.about, name="about"),
    path("contact/", views.contact, name="contact"),
]