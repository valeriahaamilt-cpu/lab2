from django.shortcuts import render

def home(request):
    return render(request, "pages/home.html", {"title": "Головна"})

def about(request):
    return render(request, "pages/about.html", {
        "title": "Про нас",
        "text": "Цей текст передано через context у render()."
    })

def contact(request):
    return render(request, "pages/contact.html", {
        "title": "Контакти",
        "text": "Текст також передається через context."
    })
