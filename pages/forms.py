from django import forms
from django.contrib.auth.models import User
from django.contrib.auth.forms import UserCreationForm, AuthenticationForm

from .models import GrandPrixComment, GrandPrixRating, TicketOrder


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


class RegisterForm(UserCreationForm):
    email = forms.EmailField(
        widget=forms.EmailInput(attrs={"class": "form-input", "placeholder": "Email"})
    )

    class Meta:
        model = User
        fields = ["username", "email", "password1", "password2"]
        widgets = {
            "username": forms.TextInput(attrs={"class": "form-input", "placeholder": "Username"}),
        }


class LoginForm(AuthenticationForm):
    username = forms.CharField(
        widget=forms.TextInput(attrs={"class": "form-input", "placeholder": "Username"})
    )
    password = forms.CharField(
        widget=forms.PasswordInput(attrs={"class": "form-input", "placeholder": "Password"})
    )


class TicketOrderForm(forms.ModelForm):
    class Meta:
        model = TicketOrder
        fields = ["full_name", "email", "quantity"]
        widgets = {
            "full_name": forms.TextInput(attrs={"class": "form-input", "placeholder": "Ваше ім'я"}),
            "email": forms.EmailInput(attrs={"class": "form-input", "placeholder": "Ваш email"}),
            "quantity": forms.NumberInput(attrs={"class": "form-input", "min": 1}),
        }


class ForgotPasswordForm(forms.Form):
    email = forms.EmailField(
        widget=forms.EmailInput(attrs={"class": "form-input", "placeholder": "Введіть email"})
    )


class VerifyResetCodeForm(forms.Form):
    code = forms.CharField(
        max_length=6,
        widget=forms.TextInput(attrs={"class": "form-input", "placeholder": "6-значний код"})
    )


class SetNewPasswordForm(forms.Form):
    new_password1 = forms.CharField(
        widget=forms.PasswordInput(attrs={"class": "form-input", "placeholder": "Новий пароль"})
    )
    new_password2 = forms.CharField(
        widget=forms.PasswordInput(attrs={"class": "form-input", "placeholder": "Підтвердіть пароль"})
    )