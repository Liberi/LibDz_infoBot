using Telegram.Bot.Types.ReplyMarkups;

namespace LibDz_infoBot
{
    internal class Keyboards
    {
        public static ReplyKeyboardMarkup MainMenu = new(new[] //любой пользователь
        {
           new KeyboardButton[] { "🏠Текущее ДЗ", "🗓Расписания"},
           new KeyboardButton[] { "🔰Еще...", "👨‍💻Администратор" },
        })
        { ResizeKeyboard = true };

        public static ReplyKeyboardMarkup Menu1 = new(new[] //любой пользователь
        {
            new KeyboardButton[] { "📰Новости", "👥Контакты" },
            new KeyboardButton[] { "◀️Назад" },
        })
        { ResizeKeyboard = true };

        public static ReplyKeyboardMarkup schedules = new(new[] //любой пользователь
        {
            new KeyboardButton[] { "📆На завтра", "🗓На всю неделю" },
            new KeyboardButton[] { "🔔Расписание звонков", "⏱До конца пары" },
            new KeyboardButton[] { "◀️Назад" },
        })
        { ResizeKeyboard = true };

        public static ReplyKeyboardMarkup administrator = new(new[] // мин уровень допуска адм3 или выше
        {
            new KeyboardButton[] { "📖Управление \"ДЗ\"" },
            new KeyboardButton[] { "⏰Управление \"Расписание\"" },
            new KeyboardButton[] { "🔰Еще...", "◀️Назад" },
        })
        { ResizeKeyboard = true };

        public static ReplyKeyboardMarkup schedule = new(new[] // мин уровень допуска адм3 или выше
        {
            new KeyboardButton[] { "✏️Изменить звонки", "🗑Удалить звонки" },
            new KeyboardButton[] { "✏️Изменить расписание", "🗑Удалить расписание"},
            new KeyboardButton[] { "◀️Назад" },
        })
        { ResizeKeyboard = true };

        public static ReplyKeyboardMarkup homeTasks = new(new[] // мин уровень допуска адм3 или выше
        {
            new KeyboardButton[] { "➕Добавить ДЗ", "🗑Удалить ДЗ" },
            new KeyboardButton[] { "✏️Редактировать ДЗ", "◀️Назад" },
            /*new KeyboardButton[] { "◀️Назад" },*/
        })
        { ResizeKeyboard = true };

        public static ReplyKeyboardMarkup HelpAdmin = new(new[] // мин уровень допуска адм2 или выше
        {
            new KeyboardButton[] { "👨‍💻Управление админами" },
            new KeyboardButton[] { "🗃База данных"  },
            new KeyboardButton[] { "🔰Еще...", "◀️Назад" },
        })
        { ResizeKeyboard = true };

        public static ReplyKeyboardMarkup administratorManagement = new(new[] // мин уровень допуска адм2 или выше
        {
            new KeyboardButton[] { "➕Добавить Админа", "🗑Удалить Админа" },
            new KeyboardButton[] { "◀️Назад" },
        })
        { ResizeKeyboard = true };

        public static ReplyKeyboardMarkup database = new(new[] // мин уровень допуска адм2 или выше
        {
            new KeyboardButton[] { "👨‍💻Информация о Админах" },
            new KeyboardButton[] { "👤Информация о Пользователях" },
            new KeyboardButton[] { "◀️Назад" },
        })
        { ResizeKeyboard = true };

        public static ReplyKeyboardMarkup Moderator = new(new[] // мин уровень допуска адм2 или выше
        {
            new KeyboardButton[] { "📰Управление новостями", },
            new KeyboardButton[] { "🤖Управление ботом",  },
            new KeyboardButton[] { "◀️Назад" },
        })
        { ResizeKeyboard = true };

        public static ReplyKeyboardMarkup News = new(new[] // мин уровень допуска адм1
        {
            new KeyboardButton[] { "➕Добавить новость", "🗑Удалить новость" },
            new KeyboardButton[] { "✏️Редактировать новость", "◀️Назад" },
        })
        { ResizeKeyboard = true };


        public static ReplyKeyboardMarkup botManagement = new(new[] // мин уровень допуска адм1
        {
            new KeyboardButton[] { "🗣Сообщение всем", "🛑Выключение" },
            new KeyboardButton[] { "🔏Б/Р Пользователей", "◀️Назад" },
        })
        { ResizeKeyboard = true };

        public static ReplyKeyboardMarkup lockUnlockUsers = new(new[] // мин уровень допуска адм1
        {
            new KeyboardButton[] { "🔐Заблокировать", "🔓Разблокировать" },
            new KeyboardButton[] { "◀️Назад" },
        })
        { ResizeKeyboard = true };

        /*------------------------------=========================================------------------------------*/
        public static InlineKeyboardMarkup Help = new(new[]
        {
            new []
            {
                InlineKeyboardButton.WithUrl("👨‍💻Разработчик бота", "https://t.me/Lib_int"),
                InlineKeyboardButton.WithUrl("👤Главный администратор", "https://t.me/Lib_int"),
            },
        });

        public static InlineKeyboardMarkup Contacts = new(new[]
        {
            new []
            {
                InlineKeyboardButton.WithUrl("👨‍💻Разработчик бота", "https://t.me/Lib_int"),
                InlineKeyboardButton.WithUrl("👤Главный администратор", "https://t.me/Lib_int"),
            },
            new []
            {
                InlineKeyboardButton.WithUrl("👥Основная группа ДЗ", "https://t.me/kadievacrushcringe"),
                InlineKeyboardButton.WithUrl("🏫Группа колледжа", "https://t.me/vke_edu"),
            }
        });

        public static InlineKeyboardMarkup Profile = new(new[]
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "📝Изменить группу", callbackData: "updateGroup"),
                InlineKeyboardButton.WithCallbackData(text: "📝Изменить имя", callbackData: "updateName"),
            },
        });

        public static InlineKeyboardMarkup cancel = new(new[]
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "🚫Отменить🚫", callbackData: "cancel"),
            },
        });

        public static InlineKeyboardMarkup addComment = new(new[]
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "📑Добавить комментарий", callbackData: "addComment"),
            },
        });

        public static InlineKeyboardMarkup noDz = new(new[]
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "⚠️Отмена пар", callbackData: "noPairs"),
                InlineKeyboardButton.WithCallbackData(text: "❗️Нет ДЗ", callbackData: "noDZ"),
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "🚫Отменить🚫", callbackData: "cancel"),
            }
        });

        public static InlineKeyboardMarkup confirmation = new(new[]
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "✅Подтвердить✅", callbackData: "confirm"),
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "🚫Отменить🚫", callbackData: "cancel"),
            }
        });

        public static InlineKeyboardMarkup noAutoSendDz = new(new[]
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "✏️Отредактировать", callbackData: "editDz"),
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "✉️Отправить повторно", callbackData: "resendDz"),
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "🚫Отменить🚫", callbackData: "cancel"),
            }
        });


        public static InlineKeyboardMarkup newsButton = new(new[]
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "◀️Предыдущая", callbackData: "PreviousNews"),
                InlineKeyboardButton.WithCallbackData(text: "Следующая▶️", callbackData: "NextNews"),
            },
        });

        public static InlineKeyboardMarkup addPicture = new(new[]
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "🖼Добавить картинку", callbackData: "addPicture"),
            },
        });

        public static InlineKeyboardMarkup deletingSchedule = new(new[]
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "🗂Удалить все", callbackData: "deleteEverything"),//             <----                   <----                     !???!
                InlineKeyboardButton.WithCallbackData(text: "📔Удалить одно", callbackData: "deleteOne"),
            },
        });

        public static InlineKeyboardMarkup tablePicture = new(new[]
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData(text: "🖼В виде картинки", callbackData: "tablePicture"),
            },
        });
    }
}
