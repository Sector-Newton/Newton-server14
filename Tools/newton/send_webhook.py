import requests
import os
from datetime import datetime

WEBHOOK_URL = os.environ["UPDATE_WEBHOOK"]
ROLE_ID = os.environ["UPDATE_ROLE_ID"]
WEBHOOK_USERNAME = "Sector Newton"
WEBHOOK_AVATAR_URL = "https://i.imgur.com/xy1Fgdk.jpeg"

header = {
    "Content-Type": "application/json"
}

def get_file_size(path):
  size_bytes = os.path.getsize(path)

  for unit in ["Б", "КБ", "МБ", "ГБ"]:
    if size_bytes < 1024:
      return f"{size_bytes:.2f} {unit}"
    size_bytes /= 1024

def main():
    date = datetime.now()
    formatted_date = date.strftime("%Y-%m-%d | %H:%M:%S")

    payload = {
        "username": WEBHOOK_USERNAME,
        "avatar_url": WEBHOOK_AVATAR_URL,
        "content": f"<@&{ROLE_ID}>",
        "embeds": [
            {
                "title": "Сервер обновлён",
                "description": "Новое обновление уже опубликовано и вскоре будет на сервере.",
                "url": "https://cdn.sector-newton.ru/fork/newton",
                "color": 5763719,
                "footer": {
                    "text": f"Время публикации | {formatted_date}"
                },
            }
        ],
    }
    response = requests.post(WEBHOOK_URL, json=payload, headers=header)
    response.raise_for_status()
    print("SUCCESS - send webhook")

if __name__ == '__main__':
    main()