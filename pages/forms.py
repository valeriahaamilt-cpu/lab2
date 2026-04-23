from django import forms
from .models import GrandPrixComment, GrandPrixRating


class GrandPrixCommentForm(forms.ModelForm):
    class Meta:
        model = GrandPrixComment
        fields = ["name", "comment"]
        widgets = {
            "name": forms.TextInput(attrs={"class": "form-input", "placeholder": "Ваше ім'я"}),
            "comment": forms.Textarea(attrs={"class": "form-textarea", "placeholder": "Ваш коментар", "rows": 5}),
        }


class NewsletterSubscriberForm(forms.Form):
    name = forms.CharField(
        max_length=120,
        widget=forms.TextInput(attrs={"class": "form-input", "placeholder": "Ваше ім'я"})
    )
    email = forms.EmailField(
        widget=forms.EmailInput(attrs={"class": "form-input", "placeholder": "Ваш email"})
    )


class GrandPrixRatingForm(forms.ModelForm):
    class Meta:
        model = GrandPrixRating
        fields = ["name", "score"]
        widgets = {
            "name": forms.TextInput(attrs={"class": "form-input", "placeholder": "Ваше ім'я"}),
            "score": forms.Select(attrs={"class": "form-input"}),
        }