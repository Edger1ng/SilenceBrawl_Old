import os
import asyncio
from aiogram import Bot, Dispatcher, types
from aiogram import executor
import datetime

BATTLES_FILE_PATH = 'bin/Debug/net6.0/battles.txt'
last_updated = None
ADMIN_ID = 7342241468  

def count_battle_rank():
    try:
        with open(BATTLES_FILE_PATH, 'r') as file:
            lines = file.readlines()

        count_battle_royale = 0
        count_battle_royale_team = 0
        count_coin_rush = 0
        count_laser_ball = 0
        count_robo_wars = 0
        count_bounty_hunter = 0
        count_attack_defend = 0
        total_count = 0

        for line in lines:
            # Battle Royale
            if 'gamemode: BattleRoyale!' in line:
                if 'Battle Rank: 1' in line or 'Battle Rank: 2' in line or 'Battle Rank: 3' in line:
                    count_battle_royale += 1
                    total_count += 1

            # Battle Royale Team
            elif 'gamemode: BattleRoyaleTeam!' in line:
                if 'Battle Rank: 1' in line:
                    count_battle_royale_team += 1
                    total_count += 1

            # CoinRush (Захват Кристаллов)
            elif 'gamemode: CoinRush!' in line:
                if 'Battle Result: win' in line:
                    count_coin_rush += 1
                    total_count += 1

            # LaserBall (БроулБол)
            elif 'gamemode: LaserBall!' in line:
                if 'Battle Result: win' in line:
                    count_laser_ball += 1
                    total_count += 1

            # RoboWars (Осада)
            elif 'gamemode: RoboWars!' in line:
                if 'Battle Result: win' in line:
                    count_robo_wars += 1
                    total_count += 1

            # BountyHunter (Награда за поимку)
            elif 'gamemode: BountyHunter!' in line:
                if 'Battle Result: win' in line:
                    count_bounty_hunter += 1
                    total_count += 1

            # AttackDefend (Ограбление)
            elif 'gamemode: AttackDefend!' in line:
                if 'Battle Result: win' in line:
                    count_attack_defend += 1
                    total_count += 1

        return (count_battle_royale, count_battle_royale_team, count_coin_rush, 
                count_laser_ball, count_robo_wars, count_bounty_hunter, count_attack_defend, total_count)

    except FileNotFoundError:
        return None, None, None, None, None, None, None, None

def clear_battle_logs():
    try:
        with open(BATTLES_FILE_PATH, 'w') as file:
            file.truncate(0)  # Очищаем содержимое файла
        return True
    except Exception as e:
        return False

bot = Bot(token='7233440816:AAFhOM4m5ysGzC2LH0xRpTm4qvadfJ2tBrY')
dp = Dispatcher(bot)

async def send_stats_message(chat_id, message_id):
    global last_updated
    (count_battle_royale, count_battle_royale_team, count_coin_rush, count_laser_ball, 
    count_robo_wars, count_bounty_hunter, count_attack_defend, total_count) = count_battle_rank()
    
    if count_battle_royale is not None and count_battle_royale_team is not None:
        response = f'🏆 Общая статистика побед🏆\n\n'
        response += f'👤 Одиночное Столкновение: {count_battle_royale} побед\n'
        response += f'👥 Парное Столкновение: {count_battle_royale_team} побед\n'
        response += f'💎 Захват Кристаллов: {count_coin_rush} побед\n'
        response += f'⚽ БроулБол: {count_laser_ball} побед\n'
        response += f'🤖 Осада: {count_robo_wars} побед\n'
        response += f'🎯 Награда за поимку: {count_bounty_hunter} побед\n'
        response += f'🔓 Ограбление: {count_attack_defend} побед\n\n'
        response += f'📊 Общее количество побед: {total_count}\n\n'

        current_time = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        response += f'🕒 Время получения статистики: {current_time}'

        keyboard = types.InlineKeyboardMarkup()
        button_update = types.InlineKeyboardButton(text="🔄 Обновить статистику", callback_data='update_stats')
        keyboard.add(button_update)

        if chat_id == ADMIN_ID:  # Добавляем кнопку сброса статистики только для админа
            button_reset = types.InlineKeyboardButton(text="🧹 Сбросить статистику", callback_data='reset_stats')
            keyboard.add(button_reset)

        await bot.edit_message_text(chat_id=chat_id, message_id=message_id, text=response, reply_markup=keyboard)

        last_updated = current_time

    else:
        await bot.edit_message_text(chat_id=chat_id, message_id=message_id, text='❌ Файл не найден.')

@dp.message_handler(commands=['start'])
async def start(message: types.Message):
    keyboard = types.InlineKeyboardMarkup()
    button = types.InlineKeyboardButton(text="📊 Получить статистику", callback_data='get_stats')
    keyboard.add(button)
    await message.reply("Привет! Я бот для подсчета статистики побед в CoolBrawl. Нажми кнопку ниже, чтобы получить статистику.", reply_markup=keyboard)

@dp.callback_query_handler(lambda c: c.data == 'get_stats')
async def process_callback_get_stats(callback_query: types.CallbackQuery):
    await send_stats_message(callback_query.message.chat.id, callback_query.message.message_id)

@dp.callback_query_handler(lambda c: c.data == 'update_stats')
async def process_callback_update_stats(callback_query: types.CallbackQuery):
    await send_stats_message(callback_query.message.chat.id, callback_query.message.message_id)

@dp.callback_query_handler(lambda c: c.data == 'reset_stats')
async def process_callback_reset_stats(callback_query: types.CallbackQuery):
    if callback_query.from_user.id == ADMIN_ID:
        if clear_battle_logs():
            await bot.send_message(callback_query.message.chat.id, "🧹 Статистика успешно сброшена!")
        else:
            await bot.send_message(callback_query.message.chat.id, "❌ Ошибка при сбросе статистики!")
    else:
        await bot.send_message(callback_query.message.chat.id, "❌ У вас нет прав для выполнения этой команды.")

if __name__ == '__main__':
    executor.start_polling(dp, skip_updates=True)
