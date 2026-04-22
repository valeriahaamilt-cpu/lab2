from django.db import models


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
    team_color = models.CharField(max_length=20, blank=True, help_text="Наприклад: #e10600")
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