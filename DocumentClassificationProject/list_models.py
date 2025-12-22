import requests
import sys

api_key = "AIzaSyAQWNPHM0p53h19y9B9CSp0oYDWrUBbkZo"
url = f"https://generativelanguage.googleapis.com/v1beta/models?key={api_key}"

response = requests.get(url)
if response.status_code == 200:
    models = response.json().get('models', [])
    for model in models:
        print(model.get('name'))
else:
    print(f"Error: {response.status_code} - {response.text}")
