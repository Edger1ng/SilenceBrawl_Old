import aiogram
import sys
import sqlite3 as sql
import os
import time

# Инициализация бота
bot = aiogram.Bot(token='7181475381:AAGQb-GpAAWYUlbcaS1kqg0-1FsRe-Fi-Vc')
dp = aiogram.Dispatcher(bot)

admins = []

def getHLid(Hashtag):
    TagChar = ("0", "2", "8", "9", "P", "Y", "L", "Q", "G", "R", "J", "C", "U", "V")
    if not Hashtag.startswith('#'):
        raise ValueError('Wrong Hashtag: Hashtag should start with "#"')

    TagArray = list(Hashtag[1:].upper())
    Id = 0
    for i in range(len(TagArray)):
        Character = TagArray[i]
        try:
            CharIndex = TagChar.index(Character)
        except ValueError:
            raise ValueError('Wrong Hashtag: should only contain "0", "2", "8", "9", "P", "Y", "L", "Q", "G", "R", "J", "C", "U" or "V"')
        Id *= len(TagChar)
        Id += CharIndex

    HighLow = []
    HighLow.append(Id % 256)
    HighLow.append((Id - HighLow[0]) >> 8)

    return HighLow

@dp.message_handler(commands=['start'])
async def send_welcome(message: aiogram.types.Message):
    # Приветственное сообщение
    welcome_message = (
        "Привет! Я бот.\n\n"
        "Вот список доступных команд и их функций:\n\n"
        "/start - Начать взаимодействие с ботом (это сообщение)\n"
        "/help - Получить список команд и их описания\n"
        "/tagtoid <хештег> - Преобразовать хештег в идентификатор\n"
        "/roomTagtoid <хештег> - Преобразовать хештег комнаты в идентификатор\n"
        "/link <хештег> <номер> - Привязать аккаунт\n"
        "/status - Проверить статус сервера\n"
    )

    # Отправка приветственного сообщения
    await message.reply(welcome_message)
    
@dp.message_handler(commands=['help'])
async def send_help(message: aiogram.types.Message):
    # Обработчик команды /help
    await message.reply('/link - привязка аккаунта\n/load - Загрузка аккаунта\n/buygems - купить гемы\n/buyvip - купить вип-статус')

@dp.message_handler(commands=['tagtoid'])
async def convert_tag_to_id(message: aiogram.types.Message):
    # Обработчик команды /tagtoid
    try:
        hashtag = message.get_args()
        ID = getHLid(hashtag)
        await message.reply(f'Готово!\nID: {ID[0]}-{ID[1]}')
    except ValueError as e:
        await message.reply(f'Ошибка:\n{e}')

@dp.message_handler(commands=['roomTagtoid'])
async def convert_room_tag_to_id(message: aiogram.types.Message):
    # Обработчик команды /roomTagtoid
    try:
        room_tag = message.get_args()
        ID = getHLid(room_tag)
        await message.reply(f'Готово!\nroomID: {ID[0]}-{ID[1]}')
    except ValueError as e:
        await message.reply(f'Ошибка:\n{e}')

@dp.message_handler(commands=['link'])
async def link_account(message: aiogram.types.Message):
    # Обработчик команды /link
    try:
        args = message.get_args().split()
        if len(args) != 2:
            raise ValueError('Неправильный синтаксис команды!\nПример команды: /link #000 123456\nВы можете получить код, написав в чате команду "/link"')
        ID = getHLid(args[0])
        await message.reply(f'Готово! Привязка прошла успешно.\nID: {ID[0]}-{ID[1]}')
    except ValueError as e:
        await message.reply(f'Ошибка:\n{e}')

@dp.message_handler(commands=['status'])
async def check_status(message: aiogram.types.Message):
    # Обработчик команды /status
    await message.reply('Server not responding!')

if __name__ == '__main__':
    aiogram.executor.start_polling(dp)
    print('Bot started!')