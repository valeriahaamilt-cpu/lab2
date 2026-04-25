from django import forms
from django.contrib.auth.models import User
from django.contrib.auth.forms import UserCreationForm, AuthenticationForm

from .models import GrandPrixComment, GrandPrixRating, TicketOrder


class GrandPrixCommentForm(forms.ModelForm):
    class Meta:
        model = GrandPrixComment
        fields = ["name", "comment"]
        widgets = {
            "name": forms.TextInput(attrs={
                "class": "form-input",
                "placeholder": "Your name"
            }),
            "comment": forms.Textarea(attrs={
                "class": "form-textarea",
                "placeholder": "Your comment",
                "rows": 5
            }),
        }


class NewsletterSubscriberForm(forms.Form):
    name = forms.CharField(
        max_length=120,
        label="Name",
        widget=forms.TextInput(attrs={
            "class": "form-input",
            "placeholder": "Your name"
        })
    )
    email = forms.EmailField(
        label="Email",
        widget=forms.EmailInput(attrs={
            "class": "form-input",
            "placeholder": "Your email"
        })
    )


class GrandPrixRatingForm(forms.ModelForm):
    class Meta:
        model = GrandPrixRating
        fields = ["name", "score"]
        labels = {
            "name": "Name",
            "score": "Rating",
        }
        widgets = {
            "name": forms.TextInput(attrs={
                "class": "form-input",
                "placeholder": "Your name"
            }),
            "score": forms.Select(attrs={
                "class": "form-input"
            }),
        }


class RegisterForm(UserCreationForm):
    email = forms.EmailField(
        label="Email",
        widget=forms.EmailInput(attrs={
            "class": "form-input",
            "placeholder": "Email"
        })
    )

    class Meta:
        model = User
        fields = ["username", "email", "password1", "password2"]
        labels = {
            "username": "Username",
            "email": "Email",
            "password1": "Password",
            "password2": "Confirm password",
        }
        widgets = {
            "username": forms.TextInput(attrs={
                "class": "form-input",
                "placeholder": "Username"
            }),
        }


class LoginForm(AuthenticationForm):
    username = forms.CharField(
        label="Username",
        widget=forms.TextInput(attrs={
            "class": "form-input",
            "placeholder": "Username"
        })
    )
    password = forms.CharField(
        label="Password",
        widget=forms.PasswordInput(attrs={
            "class": "form-input",
            "placeholder": "Password"
        })
    )


class TicketOrderForm(forms.ModelForm):
    class Meta:
        model = TicketOrder
        fields = ["full_name", "email", "quantity"]
        labels = {
            "full_name": "Full name",
            "email": "Email",
            "quantity": "Number of tickets",
        }
        widgets = {
            "full_name": forms.TextInput(attrs={
                "class": "form-input",
                "placeholder": "Your full name"
            }),
            "email": forms.EmailInput(attrs={
                "class": "form-input",
                "placeholder": "Your email"
            }),
            "quantity": forms.NumberInput(attrs={
                "class": "form-input",
                "min": 1
            }),
        }


class ForgotPasswordForm(forms.Form):
    email = forms.EmailField(
        label="Email",
        widget=forms.EmailInput(attrs={
            "class": "form-input",
            "placeholder": "Enter your email"
        })
    )


class VerifyResetCodeForm(forms.Form):
    code = forms.CharField(
        max_length=6,
        label="Reset code",
        widget=forms.TextInput(attrs={
            "class": "form-input",
            "placeholder": "6-digit code"
        })
    )


class SetNewPasswordForm(forms.Form):
    new_password1 = forms.CharField(
        label="New password",
        widget=forms.PasswordInput(attrs={
            "class": "form-input",
            "placeholder": "New password"
        })
    )
    new_password2 = forms.CharField(
        label="Confirm password",
        widget=forms.PasswordInput(attrs={
            "class": "form-input",
            "placeholder": "Confirm password"
        })
    )