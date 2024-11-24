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

async def get_branch_for_commit(commit_sha):
    """
    Получить имя ветки для коммита.
    """
    headers = {"Authorization": f"token {GITHUB_TOKEN}"}
    branches_url = f"{GITHUB_API_URL.format(owner=OWNER, repo=REPO)}/branches"

    async with aiohttp.ClientSession() as session:
        async with session.get(branches_url, headers=headers) as response:
            if response.status != 200:
                logger.error(f"Error fetching branches: {response.status}")
                return None
            
            branches = await response.json()
            for branch in branches:
                branch_name = branch['name']
                branch_commit_sha = branch['commit']['sha']
                if commit_sha == branch_commit_sha:
                    return branch_name
    return "Unknown branch"

async def check_github_updates():
    """
    Проверяет новые коммиты в репозитории.
    """
    global last_commit_sha
    headers = {"Authorization": f"token {GITHUB_TOKEN}"}
    commits_url = f"{GITHUB_API_URL.format(owner=OWNER, repo=REPO)}/commits"

    async with aiohttp.ClientSession() as session:
        async with session.get(commits_url, headers=headers) as response:
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

            # Проверяем, есть ли новый коммит
            if latest_sha != last_commit_sha:
                last_commit_sha = latest_sha
                branch_name = await get_branch_for_commit(latest_sha)
                message = (
                    f"💡 *Новое обновление в  {REPO}!*\n\n"
                    f"🌿 *Ветка:* {branch_name}\n"
                    f"🖋 *Сообщение:* {commit_message}\n"
                    f"👤 *Автор:* Edger1ng\n"
                    f"🕒 *Дата:* {commit_date}\n\n"
                    f"🔗 [Я оставлю это здест](https://github.com/{OWNER}/{REPO}/commit/{latest_sha})"
                )
                await bot.send_message(CHANNEL_ID, message, parse_mode="Markdown")
                logger.info("New commit notification sent!")

async def periodic_task():
    """
    Запускает периодическую проверку.
    """
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