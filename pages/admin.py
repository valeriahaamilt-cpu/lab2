from django.contrib import admin
from .models import (
    Category,
    Team,
    Driver,
    Article,
    GrandPrix,
    GrandPrixComment,
    NewsletterSubscriber,
    GrandPrixRating,
    TicketOrder,
    PasswordResetCode,
    HomeQuickLink,
)


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
    search_fields = ("name", "country", "team__name")

    fieldsets = (
        ("Main information", {
            "fields": (
                "name",
                "slug",
                "number",
                "team",
                "country",
                "portrait_url",
                "position",
                "points",
            )
        }),
        ("Biography", {
            "fields": (
                "bio",
                "date_of_birth",
                "birthplace",
            )
        }),
        ("Career statistics", {
            "fields": (
                "grand_prix_entered",
                "career_points",
                "highest_race_finish",
                "podiums",
                "highest_grid_position",
                "pole_positions",
                "world_championships",
                "dnfs",
            )
        }),
        ("Dates", {
            "fields": (
                "created_at",
                "updated_at",
            )
        }),
    )

    readonly_fields = ("created_at", "updated_at")


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


@admin.register(GrandPrixComment)
class GrandPrixCommentAdmin(admin.ModelAdmin):
    list_display = ("grand_prix", "name", "created_at", "updated_at")
    list_filter = ("grand_prix",)
    search_fields = ("name", "comment")


@admin.register(NewsletterSubscriber)
class NewsletterSubscriberAdmin(admin.ModelAdmin):
    list_display = ("name", "email", "created_at", "updated_at")
    search_fields = ("name", "email")


@admin.register(GrandPrixRating)
class GrandPrixRatingAdmin(admin.ModelAdmin):
    list_display = ("grand_prix", "name", "score", "created_at", "updated_at")
    list_filter = ("grand_prix", "score")
    search_fields = ("name",)


@admin.register(TicketOrder)
class TicketOrderAdmin(admin.ModelAdmin):
    list_display = ("user", "grand_prix", "full_name", "email", "quantity", "created_at", "updated_at")
    list_filter = ("grand_prix", "created_at", "user")
    search_fields = ("full_name", "email", "user__username", "grand_prix__name")


@admin.register(PasswordResetCode)
class PasswordResetCodeAdmin(admin.ModelAdmin):
    list_display = ("user", "code", "expires_at", "is_used", "created_at")
    list_filter = ("is_used", "created_at")
    search_fields = ("user__username", "code")


@admin.register(HomeQuickLink)
class HomeQuickLinkAdmin(admin.ModelAdmin):
    list_display = ("title", "url", "order", "is_active", "created_at", "updated_at")
    list_filter = ("is_active",)
    search_fields = ("title", "url")