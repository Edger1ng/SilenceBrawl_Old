import logging
import asyncio
import aiohttp
from aiogram import Bot, Dispatcher, executor, types

# Конфигурация
GITHUB_API_URL = "https://api.github.com/repos/{owner}/{repo}/commits"
GITHUB_TOKEN = "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"
TELEGRAM_TOKEN = "8116511420:AAFovL61Zr9XJDitR8_07NpT2o16XXGr5tI"
CHANNEL_ID = "-1002312196529"
OWNER = "Edger1ng"
REPO = "SilenceBrawl"

# Логирование
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Создаем бота и диспетчер
bot = Bot(token=TELEGRAM_TOKEN)
dp = Dispatcher(bot)

# Переменная для хранения последнего хэша коммита
last_commit_sha = None

async def check_github_updates():
    global last_commit_sha
    headers = {"Authorization": f"token {GITHUB_TOKEN}"}

    async with aiohttp.ClientSession() as session:
        async with session.get(GITHUB_API_URL.format(owner=OWNER, repo=REPO), headers=headers) as response:
            if response.status != 200:
                logger.error(f"GitHub API Error: {response.status}")
                return
            
            commits = await response.json()
            if not commits:
                logger.info("No commits found")
                return
            
            latest_commit = commits[0]
            latest_sha = latest_commit['sha']
            commit_message = latest_commit['commit']['message']
            commit_author = latest_commit['commit']['author']['name']
            commit_date = latest_commit['commit']['author']['date']

            # Если новый коммит
            if latest_sha != last_commit_sha:
                last_commit_sha = latest_sha
                message = (
                    f"💡 *New Commit in {REPO}!*\n\n"
                    f"🖋 *Message:* {commit_message}\n"
                    f"👤 *Author:* {commit_author}\n"
                    f"🕒 *Date:* {commit_date}\n\n"
                    f"🔗 [View Commit](https://github.com/{OWNER}/{REPO}/commit/{latest_sha})"
                )
                await bot.send_message(CHANNEL_ID, message, parse_mode="Markdown")
                logger.info("New commit notification sent!")

async def periodic_task():
    while True:
        try:
            await check_github_updates()
        except Exception as e:
            logger.error(f"Error in periodic_task: {e}")
        await asyncio.sleep(300)  # 5 минут

@dp.message_handler(commands=["start"])
async def start(message: types.Message):
    await message.reply("✅ Бот запущен и проверяет обновления репозитория!")

if __name__ == "__main__":
    loop = asyncio.get_event_loop()
    loop.create_task(periodic_task())
    executor.start_polling(dp, skip_updates=True)
