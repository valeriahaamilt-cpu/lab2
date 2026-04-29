from django.db import models
from django.conf import settings
from django.utils import timezone

class Category(models.Model):
    name = models.CharField(max_length=100)
    slug = models.SlugField(unique=True)
    description = models.TextField(blank=True)
    created_at = models.DateTimeField(auto_now_add=True)
    updated_at = models.DateTimeField(auto_now=True)

    def __str__(self):
        return self.name


class Team(models.Model):
    name = models.CharField(max_length=100)
    slug = models.SlugField(unique=True)
    country = models.CharField(max_length=100, blank=True)
    base = models.CharField(max_length=150, blank=True)
    logo_url = models.URLField(blank=True)
    car_image_url = models.URLField(blank=True)
    team_color = models.CharField(max_length=20, blank=True, help_text="Example: #e10600")
    created_at = models.DateTimeField(auto_now_add=True)
    updated_at = models.DateTimeField(auto_now=True)

    def __str__(self):
        return self.name


class Driver(models.Model):
    name = models.CharField(max_length=120)
    slug = models.SlugField(unique=True)
    number = models.PositiveIntegerField()
    country = models.CharField(max_length=100)
    team = models.ForeignKey(Team, on_delete=models.CASCADE, related_name="drivers")
    portrait_url = models.URLField(blank=True)
    points = models.PositiveIntegerField(default=0)
    position = models.PositiveIntegerField(default=0)
    created_at = models.DateTimeField(auto_now_add=True)
    updated_at = models.DateTimeField(auto_now=True)
    bio = models.TextField(blank=True)
    date_of_birth = models.DateField(null=True, blank=True)
    birthplace = models.CharField(max_length=150, blank=True)
    grand_prix_entered = models.PositiveIntegerField(default=0)
    career_points = models.PositiveIntegerField(default=0)
    highest_race_finish = models.CharField(max_length=50, blank=True)
    podiums = models.PositiveIntegerField(default=0)
    highest_grid_position = models.CharField(max_length=50, blank=True)
    pole_positions = models.PositiveIntegerField(default=0)
    world_championships = models.PositiveIntegerField(default=0)
    dnfs = models.PositiveIntegerField(default=0)
    initials = models.CharField(max_length=5, blank=True)

    def __str__(self):
        return self.name


class Article(models.Model):
    title = models.CharField(max_length=200)
    slug = models.SlugField(unique=True)
    category = models.ForeignKey(Category, on_delete=models.CASCADE, related_name="articles")
    team = models.ForeignKey(Team, on_delete=models.SET_NULL, null=True, blank=True, related_name="articles")
    driver = models.ForeignKey(Driver, on_delete=models.SET_NULL, null=True, blank=True, related_name="articles")
    excerpt = models.CharField(max_length=250)
    content = models.TextField()
    image_url = models.URLField(blank=True)
    is_featured = models.BooleanField(default=False)
    is_locked = models.BooleanField(default=False)
    created_at = models.DateTimeField(auto_now_add=True)
    updated_at = models.DateTimeField(auto_now=True)

    def __str__(self):
        return self.title


class GrandPrix(models.Model):
    name = models.CharField(max_length=150)
    slug = models.SlugField(unique=True)
    category = models.ForeignKey(Category, on_delete=models.CASCADE, related_name="grand_prix_events")
    country = models.CharField(max_length=100)
    city = models.CharField(max_length=100)
    circuit = models.CharField(max_length=150)
    start_date = models.DateField()
    end_date = models.DateField()
    image_url = models.URLField(blank=True)
    ticket_price = models.DecimalField(max_digits=8, decimal_places=2, default=0)
    is_upcoming = models.BooleanField(default=True)
    created_at = models.DateTimeField(auto_now_add=True)
    updated_at = models.DateTimeField(auto_now=True)

    def __str__(self):
        return self.name
    
class GrandPrixComment(models.Model):
    grand_prix = models.ForeignKey(GrandPrix, on_delete=models.CASCADE, related_name="comments")
    name = models.CharField(max_length=120)
    comment = models.TextField()
    created_at = models.DateTimeField(auto_now_add=True)
    updated_at = models.DateTimeField(auto_now=True)

    def __str__(self):
        return f"{self.name} - {self.grand_prix.name}"


class NewsletterSubscriber(models.Model):
    name = models.CharField(max_length=120)
    email = models.EmailField(unique=True)
    created_at = models.DateTimeField(auto_now_add=True)
    updated_at = models.DateTimeField(auto_now=True)

    def __str__(self):
        return self.email


class GrandPrixRating(models.Model):
    grand_prix = models.ForeignKey(GrandPrix, on_delete=models.CASCADE, related_name="ratings")
    name = models.CharField(max_length=120)
    score = models.PositiveSmallIntegerField(choices=[
        (1, "1"),
        (2, "2"),
        (3, "3"),
        (4, "4"),
        (5, "5"),
    ])
    created_at = models.DateTimeField(auto_now_add=True)
    updated_at = models.DateTimeField(auto_now=True)

    def __str__(self):
        return f"{self.grand_prix.name} - {self.score}"

class TicketOrder(models.Model):
    PAYMENT_STATUS_CHOICES = [
        ("pending", "Pending"),
        ("waiting", "Waiting for payment"),
        ("paid", "Paid"),
        ("failed", "Failed"),
        ("cancelled", "Cancelled"),
    ]

    user = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name="ticket_orders")
    grand_prix = models.ForeignKey(GrandPrix, on_delete=models.CASCADE, related_name="orders")

    full_name = models.CharField(max_length=150)
    email = models.EmailField()
    quantity = models.PositiveIntegerField(default=1)

    status = models.CharField(
        max_length=20,
        choices=PAYMENT_STATUS_CHOICES,
        default="pending"
    )

    payment_id = models.CharField(max_length=255, blank=True)
    payment_url = models.URLField(blank=True)

    created_at = models.DateTimeField(auto_now_add=True)
    updated_at = models.DateTimeField(auto_now=True)

    @property
    def total_price(self):
        return self.quantity * self.grand_prix.ticket_price

    def __str__(self):
        return f"{self.full_name} - {self.grand_prix.name} ({self.status})"

class PasswordResetCode(models.Model):
    user = models.ForeignKey(settings.AUTH_USER_MODEL, on_delete=models.CASCADE, related_name="password_reset_codes")
    code = models.CharField(max_length=6)
    created_at = models.DateTimeField(auto_now_add=True)
    expires_at = models.DateTimeField()
    is_used = models.BooleanField(default=False)

    def is_expired(self):
        return timezone.now() > self.expires_at

    def __str__(self):
        return f"{self.user.username} - {self.code}"

class HomeQuickLink(models.Model):
    title = models.CharField(max_length=100)
    url = models.CharField(max_length=255)
    order = models.PositiveIntegerField(default=0)
    is_active = models.BooleanField(default=True)

    created_at = models.DateTimeField(auto_now_add=True)
    updated_at = models.DateTimeField(auto_now=True)

    class Meta:
        ordering = ["order", "title"]

    def __str__(self):
        return self.title

