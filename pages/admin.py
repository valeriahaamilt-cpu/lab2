from django.contrib import admin
from .models import Category, Team, Driver, Article, GrandPrix


@admin.register(Category)
class CategoryAdmin(admin.ModelAdmin):
    list_display = ("name", "slug", "created_at", "updated_at")
    prepopulated_fields = {"slug": ("name",)}
    search_fields = ("name",)


@admin.register(Team)
class TeamAdmin(admin.ModelAdmin):
    list_display = ("name", "country", "base", "created_at", "updated_at")
    prepopulated_fields = {"slug": ("name",)}
    search_fields = ("name", "country", "base")


@admin.register(Driver)
class DriverAdmin(admin.ModelAdmin):
    list_display = ("name", "number", "team", "country", "position", "points", "created_at", "updated_at")
    prepopulated_fields = {"slug": ("name",)}
    list_filter = ("team", "country")
    search_fields = ("name", "country")


@admin.register(Article)
class ArticleAdmin(admin.ModelAdmin):
    list_display = ("title", "category", "team", "driver", "is_featured", "created_at", "updated_at")
    prepopulated_fields = {"slug": ("title",)}
    list_filter = ("category", "team", "driver", "is_featured", "is_locked")
    search_fields = ("title", "excerpt", "content")


@admin.register(GrandPrix)
class GrandPrixAdmin(admin.ModelAdmin):
    list_display = ("name", "country", "city", "circuit", "start_date", "end_date", "ticket_price", "created_at", "updated_at")
    prepopulated_fields = {"slug": ("name",)}
    list_filter = ("country", "is_upcoming")
    search_fields = ("name", "country", "city", "circuit")