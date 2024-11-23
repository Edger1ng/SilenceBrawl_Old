# -*- coding: cp1251 -*-

import os
import asyncio
from aiogram import Bot, Dispatcher, types
from aiogram.utils import executor
from aiogram.dispatcher.filters import Command
from aiogram.contrib.middlewares.logging import LoggingMiddleware

API_TOKEN = '7233440816:AAFhOM4m5ysGzC2LH0xRpTm4qvadfJ2tBrY'
USER_ID = '7342241468'
FILE_PATH = 'bin/Debug/net6.0/battles.txt'

bot = Bot(token=API_TOKEN)
dp = Dispatcher(bot)
dp.middleware.setup(LoggingMiddleware())  # Логирование ошибок и запросов

last_line_number = 0  # Номер последней обработанной строки

async def check_battles():
    global last_line_number
    if not os.path.isfile(FILE_PATH):
        print(f"Файл {FILE_PATH} не найден.")
        return

    with open(FILE_PATH, 'r', encoding='utf-8') as file:
        lines = file.readlines()
        new_lines = lines[last_line_number:]
        for line in new_lines:
            if "ended battle!" in line:
                parts = line.split(' in ')
                if len(parts) > 1:
                    time_str = parts[1].split('s')[0].strip()
                    try:
                        battle_time = float(time_str)
                        if "gamemode: CoinRush" in line:
                            rank_str = line.split('Battle Rank: ')[-1].split(' ')[0]
                            rank = int(rank_str)
                            if rank < 5 and battle_time < 30:
                                message = f"Есть подозрение на читы! {line.strip()}"
                                await bot.send_message(chat_id=USER_ID, text=message)
                        elif "gamemode: LaserBall" in line:
                            rank_str = line.split('Battle Rank: ')[-1].split(' ')[0]
                            rank = int(rank_str)
                            if rank < 2 and battle_time < 20:
                                message = f"Есть подозрение на читы! {line.strip()}"
                                await bot.send_message(chat_id=USER_ID, text=message)
                        else:
                            if battle_time < 25:
                                message = f"Есть подозрение на читы! {line.strip()}"
                                await bot.send_message(chat_id=USER_ID, text=message)
                    except ValueError:
                        pass
        last_line_number = len(lines)

async def periodic_check():
    while True:
        await check_battles()
        await asyncio.sleep(20)  # Проверка каждые 20 секунд

# Обработчик команды /start
@dp.message_handler(Command('start'))
async def start_handler(message: types.Message):
    await message.reply("Привет! Я отслеживаю подозрительные логи. Начинаю работу...")

# Запуск периодической проверки в фоне
async def on_startup(_):
    asyncio.create_task(periodic_check())

# Основной запуск бота
if __name__ == '__main__':
    executor.start_polling(dp, on_startup=on_startup)
