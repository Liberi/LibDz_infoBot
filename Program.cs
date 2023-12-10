using System.Data;
using System.Configuration;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using LibDz_infoBot;
using System.Timers;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Threading;

internal class Program
{
    public class TimerState
    {
        public TelegramBotClient BotClient { get; set; }
        public long ChatId { get; set; }
        public Dictionary<string, bool> PressingButtons { get; set; }
        public bool ShutdownTimer { get; set; }
    }
    static Task TimerCallback(object state)
    {
        var timerState = (TimerState)state;
        var botClient = timerState.BotClient;
        var chatId = timerState.ChatId;
        var pressingButtons = timerState.PressingButtons;
        var shutdownTimer = timerState.ShutdownTimer;

        if (pressingButtons["shutdownTimerMinuets"])
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"[{DateTime.Now}] Бот был принудительно выключен Админом");
            Console.ResetColor();
            Environment.Exit(0); // завершить приложение с кодом возврата 0, что обычно означает успешное завершение работы приложения
        }

        return Task.CompletedTask;
    }

    private static async Task Main()
    {
        //    6154384299:AAHkuqxMXNW3Chm2DG-EvOY6DWoxPtOzgOo
        string token = "5645539273:AAFuIkDhTnFTQNvjBL1ocC9fb3BqmJPt4J0";
        #region Основные переменные, массивы, параметры запуска и соединения     
        SpamDetector spamDetector = new();
        var greetings = new string[] { "добрый обед", "здарова", "на аппарате", "даров", "привет", "здравствуй", "здравствуйте", "хай", "добрый день", "добрый вечер",
            "доброе утро", "доброго времени суток", "салют", "здрасти", "дарова", "ку", "вечер в хату", "здорово",
            "прив", "бонжур", "здаров", "хеллоу", "хай ёр", "здравия желаю", "день добрый", "утро доброе", "вечер добрый",
            "здравствуй, братан", "здравствуй, кум", "рад тебя видеть", "приветствую тебя", "приветствую всех в чате" };//массив для приветствий, если пользователь поздоровается 
        List<int> newsNumbers = new();//массив для новостей, т.е какие на данный момент новости есть
        bool shutdownTimer = false;
        Dictionary<string, bool> pressingButtons = new()//массив для хранения всех действий для которых нужен ввод
        {
            { "changeGroupFT", false },
            { "changeNameFT", false },
            { "addAdmin", false },
            { "deleteAdmin", false },
            { "InformationAdmins", false },
            { "changeAdminType", false },
            { "deleteNews", false },
            { "addingNews", false },
            { "numberNewsPicture", false },
            { "addPhotoNews", false },
            { "changeNews", false },
            { "changeCalls", false },
            { "changeSchedule", false },
            { "deleteSchedule", false },
            { "addHomework", false },
            { "changeHomework", false },
            { "deleteHomework", false },
            { "messageTextEveryone", false },
            { "shutdownTimerMinuets", false },
            { "lockUsers", false },
            { "unlockUsers", false },
        };
        Dictionary<string, string> weekDays = new()//для перевода из введённого в анг формат 
        {
            { "понедельник", "Monday" },
            { "вторник", "Tuesday" },
            { "среда", "Wednesday" },
            { "четверг", "Thursday" },
            { "пятница", "Friday" },
            { "суббота", "Saturday" },
            { "воскресенье", "Sunday" }
        };
        int globalMessageTextId = 0, globalMessagePhotoId = 0, globalNewsId = 0, globalNewsNumber = 0, globalDzInfo = 0, globalDzTextInfo = 0, globalDzChat = 0, globalTimerCountRestart = 0;//глобальные переменные, которые используются для разных действий во всем коде
        long globalUserId = 0, globalChatId = 1545914098, globalBaseDzChat = -1001602210737  /*-1001797288636*/, globalAdminId = 1545914098;
        string globalUsername = "СтандартИмя", Exception = "ПУСТО", keyMenu = "MainMenu", weekType = "Числитель", globalTablePicture = "4 6 Null", //keyMenu переменная которая сохраняет меню в котором мы сейчас находимся (для кнопки назад)
            globalFilePathWeekType = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "weekType.txt"), globalFilePathEditDzChatID = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "editDzChatID.txt");
        string EnglishDayNow = "Monday", EnglishDayThen = "Monday", EnglishDayYesterday = "Monday", RussianDayNow = "Понедельник", RussianDayThen = "Понедельник", RussianDayYesterday = "Понедельник", DateDayNow = "01.01", DateDayThen = "01.01", DateDayYesterday = "01.01";

        #region Подключение к БД
        string databaseFilePath = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName, "Databases"/*, "DatabaseTelegramBot.mdf"*/);
        
        string connectionString = ConfigurationManager.ConnectionStrings["TelegramBotLibDB"].ConnectionString;

        connectionString = connectionString.Replace("|DataDirectory|", databaseFilePath);

        //хотел получать путь через DataDirectory с помощью AppDomain.CurrentDomain.SetData но оно не работает корректно,
        //теперь получаю путь как обычно через App.config при этом получая путь возвращаясь на 3 директории назад и заменяя |DataDirectory| на нужный путь

        SqlConnection sqlConnection = new(connectionString);
        #endregion

        var botClient = new TelegramBotClient(token);//подключение к боту по токену
        using CancellationTokenSource cts = new();

        // StartReceiver не блокирует вызывающий поток. Получение выполняется в ThreadPool.
        ReceiverOptions receiverOptions = new()
        {
            AllowedUpdates = Array.Empty<UpdateType>() // получать все типы обновлений, кроме обновлений, связанных с ChatMember
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );//подключаем к боту все методы + токен

        var botName = await botClient.GetMeAsync();//запоминаем имя бота
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"[{DateTime.Now}] Запущен бот: @{botName.Username}");//информируем о запуске в консоль
        await ReturnGlobalDzChat();
        await ReturnDayWeek(true);
        await WhatWeekType();
        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.WriteLine($"-> Основные данные:\n" +//информируем о основных данных в консоль
            $">Текущее дни недели учитывая время до 10:00:\n" +
            $"Вчера -> анг: {EnglishDayYesterday} ру: {RussianDayYesterday} дат: {DateDayYesterday}\n" +
            $"Сегодня -> анг: {EnglishDayNow} ру: {RussianDayNow} дат: {DateDayNow}\n" +
            $"Завтра -> анг: {EnglishDayThen} ру: {RussianDayThen} дат: {DateDayThen}\n" +
            $">Текущий тип недели: {weekType}\n" +
            $">Текущее Id администратора: {globalAdminId}\n" +
            $">Текущий владелец бота (стандартный globalChatId): {globalChatId}\n" +
            $">Текущий Id чата отправки дз: {globalBaseDzChat}\n" +
            $">Текущий Id сообщения для редактирования: {globalDzChat}\n" +
            $">Текущий путь для соединения с базой данных: {connectionString}");
        Console.WriteLine();
        Console.ResetColor();//для сброса цвета консоли к стандартной
        await botClient.SendTextMessageAsync(globalAdminId, "🌐Доброго времени суток, бот запущен!🌐");
        #endregion

        #region Timer
        TimeSpan timeLeft;
        /*~{(int)(timerWeekType.Interval / (1000 * 60 * 60 * 24))} дней, ~{(int)(timerWeekType.Interval / 1000) / 60 / 60} час, ~{(int)(timerWeekType.Interval / 1000) / 60} мин, ~{(int)timerWeekType.Interval / 1000} сек.*/
        #region Таймер "Авто отправка дз"
        // Устанавливаем время для отправки сообщения
        var scheduledTime = DateTime.Today.AddHours(17);

        // Если текущее время больше или равно 17:00 и меньше 19:00,
        // запускаем таймер на ближайший час (18:00 или 19:00)
        if (DateTime.Now >= scheduledTime && DateTime.Now < scheduledTime.AddHours(2))
        {
            if (globalDzChat != 0)
            {
                scheduledTime = scheduledTime.AddDays(1);
            }
            else
            {
                var nearestHour = DateTime.Now.AddHours(1);
                scheduledTime = new DateTime(nearestHour.Year, nearestHour.Month, nearestHour.Day, nearestHour.Hour, 0, 0);

                // Записываем значение в глобальную переменную globalTimerCountRestart
                if (nearestHour.Hour == 18)
                {
                    globalTimerCountRestart = 2;
                }
                else if (nearestHour.Hour == 19)
                {
                    globalTimerCountRestart = 3;
                }
            }
        }
        // Если это время уже прошло сегодня, то отправляем сообщение завтра в указанное время
        if (DateTime.Now >= scheduledTime)
        {
            scheduledTime = scheduledTime.AddDays(1);
        }

        // создаем таймер, который будет запускаться в указанное время
        var timerDz = new System.Timers.Timer();
        timerDz.Elapsed += new ElapsedEventHandler(async (sender, eventArgs) =>
        {
            string text = await DzChat();// отправляем сообщение
            int homeworkNullCount = int.Parse(text.Split('☰')[0].Trim());//проверяем сколько пустых строк дз 
            bool FTres = bool.Parse(text.Split('☰')[1].Trim());//отправилось ли вообще 
            if (FTres)
            {
                if (homeworkNullCount >= 3)
                {
                    await botClient.SendTextMessageAsync(chatId: globalAdminId, text: "❗️При отправке дз в заполнении оказалось 3 или более пустых строк❗️");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[{DateTime.Now}] При отправке дз в заполнении оказалось 3 или более пустых строк");
                    Console.ResetColor();
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[{DateTime.Now}] Авто-сообщение отправлено в чат: {globalBaseDzChat}");

                // перезапускаем таймер на следующий день
                timerDz.Interval = TimeSpan.FromDays(1).TotalMilliseconds;
                timeLeft = TimeSpan.FromDays(1);
                Console.WriteLine($"[{DateTime.Now}] Таймер авто отправки дз перезапущен на след день, оставшееся время до запуска: {await FormatTime(timeLeft)}");

                await UpdateIndexStatisticsAsync("Users");
                await UpdateIndexStatisticsAsync("Admins");
                Console.WriteLine($"[{DateTime.Now}] Обновление статистики индекса выполнено");
                Console.ResetColor();
                globalTimerCountRestart = 0;
            }
            else//в течении еще 3 часов пытаемся отправить дз
            {
                if (globalTimerCountRestart == 0)
                {
                    timerDz.Interval = TimeSpan.FromMinutes(30).TotalMilliseconds;
                    await botClient.SendTextMessageAsync(chatId: globalAdminId, text: "❗️Не удалось отправить ДЗ, дз являлось пустым❗️");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[{DateTime.Now}] Не удалось отправить ДЗ, дз являлось пустым! Таймер перезапущен на 30 мин x1");
                    Console.ResetColor();
                    globalTimerCountRestart++;
                }//17:30
                else if (globalTimerCountRestart == 1)
                {
                    timerDz.Interval = TimeSpan.FromMinutes(30).TotalMilliseconds;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[{DateTime.Now}] Не удалось отправить ДЗ, дз являлось пустым! Таймер перезапущен на 30 мин x2");
                    Console.ResetColor();
                    globalTimerCountRestart++;
                }//18:00
                else if (globalTimerCountRestart == 2)
                {
                    timerDz.Interval = TimeSpan.FromMinutes(60).TotalMilliseconds;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[{DateTime.Now}] Не удалось отправить ДЗ, дз являлось пустым! Таймер перезапущен на 1 час x3");
                    Console.ResetColor();
                    globalTimerCountRestart++;
                }//19:00
                else if (globalTimerCountRestart == 3)//если на 4 не отправилось (19:00), то перезапускаем на next день
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[{DateTime.Now}] Не удалось отправить ДЗ, дз являлось пустым!");
                    // перезапускаем таймер на следующий день
                    scheduledTime = DateTime.Today.AddHours(17);
                    // если это время уже прошло сегодня, то отправляем сообщение завтра в указанное время
                    if (DateTime.Now >= scheduledTime)
                    {
                        scheduledTime = scheduledTime.AddDays(1);
                    }
                    var timeToNextRun = scheduledTime - DateTime.Now;
                    timeLeft = scheduledTime - DateTime.Now;
                    timerDz.Interval = timeToNextRun.TotalMilliseconds;//задаём интервал
                    Console.WriteLine($"[{DateTime.Now}] Таймер авто отправки дз перезапущен на след день, оставшееся время до запуска: {await FormatTime(timeLeft)}");
                    Console.ForegroundColor = ConsoleColor.Green;

                    await UpdateIndexStatisticsAsync("Users");
                    await UpdateIndexStatisticsAsync("Admins");
                    Console.WriteLine($"[{DateTime.Now}] Обновление статистики индекса выполнено");
                    Console.ResetColor();
                    globalTimerCountRestart = 0;
                }
            }
        });
        // вычисляем время до следующего запуска
        var timeToNextRun = scheduledTime - DateTime.Now;
        timeLeft = scheduledTime - DateTime.Now;
        timerDz.Interval = timeToNextRun.TotalMilliseconds;
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"[{DateTime.Now}] Таймер авто отправки дз запущен, оставшееся время до запуска: {await FormatTime(timeLeft)}");
        Console.ResetColor();
        timerDz.Start();
        #endregion
        #region Таймер "Тип недели"
        // вычисляем время для следующего запуска таймера
        DateTime now = DateTime.Now;
        DateTime nextSunday = now.AddDays((7 - (int)now.DayOfWeek) % 7).Date;
        TimeSpan timeToNextRunWeekType = nextSunday - now;

        // создаем таймер, который будет запускаться каждую неделю
        var timerWeekType = new System.Timers.Timer();
        timerWeekType.Elapsed += new ElapsedEventHandler(async (sender, eventArgs) =>
        {
            // перезапускаем таймер на следующий день
            timerWeekType.Interval = TimeSpan.FromDays(7).TotalMilliseconds;
            timeLeft = TimeSpan.FromDays(7);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{DateTime.Now}] Таймер типа недели перезапущен на след неделю, оставшееся время до запуска: {await FormatTime(timeLeft)}");
            Console.ResetColor();

            await WhatWeekType();
            // меняем значение переменной weekType каждую неделю
            weekType = weekType == "Числитель" ? "Знаменатель" : "Числитель";
            System.IO.File.WriteAllText(globalFilePathWeekType, weekType);


            foreach (string RussiansDay in weekDays.Keys)
            {
                if (weekDays.TryGetValue(RussiansDay, out string EnglishDay))
                {
                    if (EnglishDay != "Monday")
                    {
                        await clearingTables($"{EnglishDay}_Homework");
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{DateTime.Now}] Ошибка: Невозможно получить английское название дня недели для русского дня {RussiansDay}");
                    Console.ResetColor();
                    return;//маловероятная возможная ошибка
                }
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{DateTime.Now}] Произведена очистка дз для всех дней недели кроме Понедельника");
            Console.WriteLine($"[{DateTime.Now}] Тип недели изменен, теперь тип недели: {weekType}");
            Console.ResetColor();
        });
        // проверяем, что время следующего запуска не прошло
        if (timeToNextRunWeekType < TimeSpan.Zero)
        {
            // если время следующего запуска уже прошло, то вычисляем время для следующего запуска таймера на следующее воскресенье
            nextSunday = nextSunday.AddDays(7);
            timeToNextRunWeekType = nextSunday - now;
        }
        timerWeekType.Interval = timeToNextRunWeekType.TotalMilliseconds;
        timeLeft = timeToNextRunWeekType;
        timerWeekType.Start();
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"[{DateTime.Now}] Таймер типа недели запущен, оставшееся время до запуска: {await FormatTime(timeLeft)}");
        Console.ResetColor();
        #endregion
        #endregion

        Console.ReadLine();
        // Отправить запрос на отмену, чтобы остановить бота
        cts.Cancel();

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)//основной метод, который отвечает за обработку всех входящих действий от пользователя
        {
#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
#pragma warning disable CS8604 // Возможно, аргумент-ссылка, допускающий значение NULL.           
            if (update?.Type == UpdateType.CallbackQuery)// Обработать действие кнопки под сообщениями
            {
                var callbackQuery = update.CallbackQuery;
                if (callbackQuery?.Message?.Chat.Type != ChatType.Private)//если CallbackQuery был отправлен не из личного чата
                {
                    return;
                }
                globalChatId = update.CallbackQuery.Message.Chat.Id;
                globalUserId = update.CallbackQuery.Message.From.Id;
                if (await InitialChecks(globalChatId))
                {
                    return;
                }
                globalUsername = update.CallbackQuery.Message.Chat.Username;//обновляем все данные после каждого сообщения
                if (string.IsNullOrWhiteSpace(globalUsername))//если имя пустое, то записываем стандартное
                {
                    globalUsername = "СтандартИмя";
                }
                Exception = $"Действие кнопки под сообщениями: {update.CallbackQuery.Data}";
                Console.WriteLine($"[{DateTime.Now}] Действие кнопки под сообщениями: <'{update.CallbackQuery.Data}'> из чата: {globalChatId}, имя пользователя {globalUsername}");
                await ButtonUpdate(update.CallbackQuery.Data);//запускаем обработку действия
            }
            else if (update?.Message?.Type == MessageType.Photo)//Обработать действие отправленной фотографии
            {
                var photoMessage = update.Message;
                if (photoMessage.Chat.Type != ChatType.Private)//если фотография была отправлена не из личного чата
                {
                    return;
                }
                globalChatId = update.Message.Chat.Id;
                globalUserId = update.Message.From.Id;
                if (await InitialChecks(globalUserId))
                {
                    return;
                }
                globalUsername = update.Message.Chat.Username;//обновляем все данные после каждого сообщения
                if (string.IsNullOrWhiteSpace(globalUsername))//если имя пустое, то записываем стандартное
                {
                    globalUsername = "СтандартИмя";
                }
                Exception = $"Отправлено фото";
                Console.WriteLine($"[{DateTime.Now}] Отправлено фото: Из чата: {globalChatId}, имя пользователя {globalUsername}");

                await photoUpdate(update.Message);//запускаем обработку действия
            }
            else if (update?.Type == UpdateType.Message)//Обработать сообщение любое сообщение отправленное текстом (включая меню кнопки)
            {
                if (update.Message?.Chat?.Type != ChatType.Private)//обрабатываем только действия из личных чатов
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{DateTime.Now}] Сообщение не из личного чата: <'{update.Message.Text}'> Id чата: {globalChatId}");//информируем о сообщении не из личного чата в консоль
                    Console.ResetColor();
                    return;
                }
                if (!string.IsNullOrWhiteSpace(update.Message?.Text))//проверяем пустое ли сообщение
                {
                    globalChatId = update.Message.Chat.Id;//обновляем все данные после каждого сообщения
                    globalUserId = update.Message.From.Id;
                    if (await InitialChecks(globalUserId))
                    {
                        return;
                    }
                    globalUsername = update.Message.Chat.Username;//обновляем все данные после каждого сообщения
                    if (string.IsNullOrWhiteSpace(globalUsername))//если имя пустое, то записываем стандартное
                    {
                        //await botClient.SendTextMessageAsync(chatId: globalChatId, text: "❌Ошибка [4002]");
                        globalUsername = "СтандартИмя";
                    }
                    Console.WriteLine($"[{DateTime.Now}] Сообщение: <'{update.Message.Text}'> из чата: {globalChatId}, имя пользователя {globalUsername}");//информируем о сообщении из личного чата в консоль
                    Exception = $"Сообщение: {update.Message.Text}";
                    await HandleMessage(update.Message);//запускаем обработку действия
                }
                else
                {
                    if (update.Message.Type == MessageType.ChatMembersAdded)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[{DateTime.Now}] Пользователь: @{globalUsername} c ID пользователя: {globalUserId} ДОБАВЛЕН в чат, Id чата: {globalChatId}");
                        Console.ResetColor();
                    }
                    else if (update.Message.Type == MessageType.ChatMemberLeft)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[{DateTime.Now}] Пользователь: @{globalUsername} c ID пользователя: {globalUserId} УДАЛЕН из чата, Id чата: {globalChatId}");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[{DateTime.Now}] Пришло пустое сообщение! Id чата: {globalChatId}");//информируем о пустом сообщении 
                        Console.ResetColor();
                        await botClient.SendTextMessageAsync(update.Message.Chat, "❌Ошибка [4002]: Пустое сообщение...");
                    }
                }
            }
#pragma warning restore CS8604 // Возможно, аргумент-ссылка, допускающий значение NULL.
#pragma warning restore CS8602 // Разыменование вероятной пустой ссылки.
        }

        async Task photoUpdate(Message message)
        {
            // Получаем последнюю (самую большую) версию фото
            PhotoSize photo = message.Photo[^1];
            #region Обновление фотографии новости
            if (pressingButtons["addPhotoNews"])
            {
                if (await photoVerification(photo))//проверяем подходит ли фото по параметрам
                {
                    // Скачиваем и сохраняем изображение в переменную в виде массива байт
                    byte[] image;
                    using (var stream = new MemoryStream())//преобразуем в массив байт
                    {
                        var fileId = message.Photo.Last().FileId;
                        var fileInfo = await botClient.GetFileAsync(fileId);
                        var filePath = fileInfo.FilePath;

                        await botClient.DownloadFileAsync(filePath: filePath, stream);
                        image = stream.ToArray();
                    }

                    // Создаем объект SqlConnection для подключения к базе данных
                    using (SqlConnection connection = new(connectionString))
                    {
                        connection.Open();

                        // Создаем запрос на обновление данных в таблицу News
                        SqlCommand command = new("UPDATE News SET image = @image WHERE newsNumber = @newsNumber", connection);
                        command.Parameters.AddWithValue("@newsNumber", globalNewsId);

                        SqlParameter imageParam = new("@image", SqlDbType.VarBinary, -1)
                        {
                            Value = image
                        };//обновляем изображение в базе в виде массива байт
                        command.Parameters.Add(imageParam);
                        // Выполняем запрос на добавление данных в таблицу News
                        command.ExecuteNonQuery();
                        // Закрываем соединение с базой данных
                        connection.Close();
                    }
                    // Отправляем сообщение пользователю, что его фото сохранено
                    await botClient.SendTextMessageAsync(globalChatId, $"✅Фото успешно сохранено для новости номер {globalNewsId}");
                }
                pressingButtons["addPhotoNews"] = false;//закрываем обработку действия
                return;
            }
            #endregion

            //отправляется тогда, кода не было обработок действий, т.е не было return
            await botClient.SendTextMessageAsync(
                chatId: globalChatId,
                text: "❌Ошибка [1200]: Незарегистрированная картинка🖼");
        }

        async Task HandleMessage(Message message)
        {
            #region /USSR
            if (message.Text.Trim() == "/SovietUnion" || message.Text.Trim().ToUpper() == "/USSR")//прикол команда пасхалка
            {
                await botClient.SendTextMessageAsync(globalChatId, $"☭ ผ(•̀_•́ผ) ☭");
                return;
            }
            #endregion

            #region /start 
            if (message.Text.Trim().ToLower() == "/start")//самая первая команда, которую должен ввести пользователь
            {
                await botClient.SendTextMessageAsync(message.Chat, "👋Рад видеть тебя!\nДля просмотра профиля введи /profile");
                // Проверяем, есть ли userId в таблице
                if (!await dataAvailability(globalUserId, "Users"))
                {
                    await AddUserToDatabase(globalUserId, globalUsername);
                    // Отправляем сообщение пользователю с вопросом на ввод группы
                    await botClient.SendTextMessageAsync(globalChatId, "📝Вы добавлены в базу данных\\!\n✏️Для изменения группы введите /group _НазваниеГруппы_", replyMarkup: Keyboards.MainMenu, parseMode: ParseMode.MarkdownV2);
                    //await UpdateIndexStatisticsAsync("Users");
                }
                else// Если userId нет в таблице, то добавляем данные
                {
                    await botClient.SendTextMessageAsync(globalChatId, "🤝*Мы уже с вами знакомы\\)*\n✏️Но если вы хотите изменить группу введите /group _НазваниеГруппы_", replyMarkup: Keyboards.MainMenu, parseMode: ParseMode.MarkdownV2);

                    if (globalUsername != "СтандартИмя")
                    {
                        sqlConnection.Open();
                        // Обновляем имя пользователя в базе данных на всякий
                        SqlCommand updateCommand = new("UPDATE Users SET username = @userName WHERE user_id = @userId", sqlConnection);
                        updateCommand.Parameters.AddWithValue("@userId", globalUserId);
                        updateCommand.Parameters.AddWithValue("@userName", globalUsername);
                        updateCommand.ExecuteNonQuery();
                    }
                }
                sqlConnection.Close();
                return;
            }
            #endregion
            #region /help
            if (message.Text.Trim().ToLower() == "/help")//команда помощи
            {
                await botClient.SendTextMessageAsync(
                chatId: globalChatId,
                text: "*↻Информация для помощи↺*\n\n" +
                "☞По всем вопросам можете обращаться\n к *⇊Главному Администратору* или *Разработчику⇊*\n" +
                "\n" +
                "*Актуальные команды:*\n" +
                "/start ➯ Запуск бота\r\n" +
                "/help ➯ Помощь и основная информация\r\n" +
                "/group ➯ Изменение группы\r\n" +
                "/profile ➯ Информация о вас в нашей базе\r\n" +
                "/menu ➯ Переход в главное меню\r\n" +
                "/cancel ➯ Отмена выполнения предыдущего действия\r\n" +
                "/news ➯ Отобразить последние новости\r\n" +
                "/change_admin_type ➯ Изменение типа администратора\r\n" +
                "/update_photo_news ➯ Обновить фото к новости\r\n" +
                "/update_week_type ➯ Обновить тип недели\n" +
                "\n" +
                "*Список ошибок:*\n" +
                "`\\[0000\\]` ➱ Выключение или блокировка сервера;\r\n" +
                "\r\n" +
                "`\\[1100\\]` ➱ Не удалось обработать текст;\r\n" +
                "`\\[1200\\]` ➱ Не удалось обработать картинку;\r\n" +
                "`\\[1300\\]` ➱ Не удалось обработать кнопку;\r\n" +
                "\r\n" +
                "`\\[1001\\]` ➱ Превышение размера фила;\r\n" +
                "`\\[1002\\]` ➱ Неподдерживаемый формат файла;\r\n" +
                "`\\[1003\\]` ➱ Проблемы с локализацией дней недели;\r\n" +
                "`\\[1004\\]` ➱ Ошибка в выполнении инструкции Regex;\r\n" +
                "\r\n" +
                "`\\[2001\\]` ➱ Ошибка работы сети;\r\n" +
                "`\\[2002\\]` ➱ Ошибка в использовании Id пользователя;\r\n" +
                "\r\n" +
                "`\\[3001\\]` ➱ Ошибка в преобразовании текста для базы данных;\r\n" +
                "`\\[3002\\]` ➱ Значение в базе данных не найдено или равно NULL;\r\n" +
                "`\\[3003\\]` ➱ Значение уже находится в базе данных;\r\n" +
                "\r\n" +
                "`\\[4001\\]` ➱ Отсутствие файлов;\r\n" +
                "`\\[4002\\]` ➱ Одной из значений имеет значение NULL или размер равный 0;\r\n" +
                "`\\[4003\\]` ➱ Недостаточно прав для выполнения действия;",
                replyMarkup: Keyboards.Help, parseMode: ParseMode.MarkdownV2);
                return;
            }
            #endregion
            #region /group 
            if (message.Text.Trim().ToLower() == "/group" || Regex.IsMatch(message.Text.Trim().ToLower(), "/group"))//команда для изменения группы
            {//проверяем с помощью Regex т.к мы вводим /group НазваниеГруппы
                string userGroup = message.Text;// Получаем значение группы из сообщения
                if (userGroup.Trim() == "/group" || userGroup.Split(' ')[0].Trim() != "/group" || userGroup.Split(' ').Length > 2)//проверяю правильно ли ввели группу, и что 0 элемент = команде, так-же проверяю параметр массива
                {
                    await botClient.SendTextMessageAsync(globalChatId, "❌Ошибка \\[3001\\]: *Вы вели группу некорректно*\nДля изменения группы введите /group _НазваниеГруппы_ *_\\(НазваниеГруппы без пробелов\\)_* ", parseMode: ParseMode.MarkdownV2);
                }
                else
                {
                    userGroup = userGroup.Split(' ')[1].Trim();//беру 1 элемент как название группы
                    if (userGroup == null || userGroup == String.Empty)//проверяю что не пустое
                    {
                        await botClient.SendTextMessageAsync(globalChatId, "❌Ошибка \\[4002\\]: *Вы вели группу некорректно*\nДля изменения группы введите /group _НазваниеГруппы_", parseMode: ParseMode.MarkdownV2);
                    }
                    else
                    {
                        sqlConnection.Open();
                        SqlCommand updateCommand = new("UPDATE Users SET user_group = @userGroup WHERE user_id = @userId", sqlConnection);
                        updateCommand.Parameters.AddWithValue("@userId", globalUserId);
                        updateCommand.Parameters.AddWithValue("@userGroup", userGroup);
                        updateCommand.ExecuteNonQuery();
                        sqlConnection.Close();
                        await botClient.SendTextMessageAsync(globalChatId, $"✅*Группа успешно изменена\\!*\nТеперь ваша группа {await EscapeMarkdownV2(userGroup)}", parseMode: ParseMode.MarkdownV2);
                    }
                }
                return;
            }
            #endregion
            #region /profile
            if (message.Text.Trim().ToLower() == "/profile")//команда для отображения тек профиля
            {
                if (await dataAvailability(globalUserId, "Users"))//проверяем есть ли человек в нашей базе
                {
                    var messageRes = await botClient.SendTextMessageAsync(globalChatId, await userProfile(globalUserId), replyMarkup: Keyboards.Profile, parseMode: ParseMode.MarkdownV2);//если есть то выводим данные
                    globalMessageTextId = messageRes.MessageId; // сохраняем идентификатор сообщения если будет менять имя или группу
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"🔌Увы, но мы не нашли вас в нашей базе данных [3002], сейчас добавим...");

                    await AddUserToDatabase(globalUserId, globalUsername);

                    await botClient.SendTextMessageAsync(globalChatId, $"✅Пользователь *{await EscapeMarkdownV2(globalUsername)}* успешно зарегистрирован в базе данных\\.\n✏️Для изменения группы введите /group _НазваниеГруппы_", parseMode: ParseMode.MarkdownV2);

                    var messageRes = await botClient.SendTextMessageAsync(globalChatId, await userProfile(globalUserId), replyMarkup: Keyboards.Profile, parseMode: ParseMode.MarkdownV2);//если есть то выводим данные
                    globalMessageTextId = messageRes.MessageId; // сохраняем идентификатор сообщения если будет менять имя или группу

                    //await UpdateIndexStatisticsAsync("Users");
                }
                return;
            }
            #endregion
            #region /menu
            if (message.Text.Trim().ToLower() == "/menu")//просто переход в главное меню, если оно потерялось
            {
                await botClient.SendTextMessageAsync(globalChatId, "⤴️Переход в главное меню...", replyMarkup: Keyboards.MainMenu);
                keyMenu = "MainMenu";
                await Cancel(false, false);
                return;
            }
            #endregion
            #region /cancel
            if (message.Text.Trim().ToLower() == "/cancel")//отмена всех открытых действий
            {
                await Cancel();
                return;
            }
            #endregion
            #region /stopTimer
            if (message.Text.Trim().ToLower() == "/stopTimer")//отмена всех открытых действий с таймером
            {
                await Cancel(true);
                return;
            }
            #endregion
            #region /change_admin_type
            if (message.Text.Trim().ToLower() == "/change_admin_type")//изменение типа админа
            {
                if (await buttonTest())//стандартная проверка на то, выполняется ли какое-либо действие сейчас
                {
                    if (await dataAvailability(globalUserId, "Admins 1") || await dataAvailability(globalUserId, "Admins 2"))//проверка что ты являешься админом и какого типа: 1 или 2
                    {
                        await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                            text: "📝Введите *имя/ID* \\+ *НовыйТипАдмина* для изменения типа в таком формате:\n" +
                                                            "_имя\\(или\\)ID_ _НовыйТипАдмина_\n" +
                                                            "▣🔏Если вы сами являетесь Администратором *типа 1*\\, то введите в _НовыйТипАдмина_ цифру от *1 до 3* включительно\\.\n" +
                                                            "▣🔏Если же вы являетесь Администратором *типа 2*\\, то введите в _НовыйТипАдмина_ цифру от *2 до 3* включительно\\.",
                                                            replyMarkup: Keyboards.cancel, parseMode: ParseMode.MarkdownV2);
                        pressingButtons["changeAdminType"] = true;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, "📍Для выполнения другого действия закончите работу с предыдущим\n⭕️Или введите /cancel");
                }
                return;
            }
            #endregion
            #region /update_photo_news
            if (message.Text.Trim().ToLower() == "/update_photo_news")//обновление фотографии новостей
            {
                if (await buttonTest())//стандартная проверка на то, выполняется ли какое-либо действие сейчас
                {
                    if (await dataAvailability(globalUserId, "Admins 1") || await dataAvailability(globalUserId, "Admins 2"))//проверка что ты являешься админом и какого типа: 1 или 2
                    {
                        await botClient.SendTextMessageAsync(globalChatId, "📝Введите номер новости для обновления картинки:", replyMarkup: Keyboards.cancel);
                        pressingButtons["numberNewsPicture"] = true;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, "📍Для выполнения другого действия закончите работу с предыдущим\n⭕️Или введите /cancel");
                }
                return;
            }
            #endregion
            #region /update_week_type
            if (message.Text.Trim().ToLower() == "/update_week_type")//обновление типа недели
            {
                if (await dataAvailability(globalUserId, "Admins"))//проверка что ты являешься админом
                {
                    await WhatWeekType();
                    weekType = weekType == "Числитель" ? "Знаменатель" : "Числитель";
                    System.IO.File.WriteAllText(globalFilePathWeekType, weekType);
                    await botClient.SendTextMessageAsync(globalChatId, $"✅Теперь тип недели: *{weekType}*", parseMode: ParseMode.Markdown);
                    await DzChat(true);
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом [3002]+[4003]");
                }
                return;
            }
            #endregion

            #region Кнопка Еще...
            if (message.Text.Trim() == "🔰Еще...")//у нас 2 кнопки Ещё..., поэтому нужно знать какая нажата
            {
                if (keyMenu == "MainMenu")
                {
                    await botClient.SendTextMessageAsync(
                    chatId: globalChatId,
                    text: "🎛Выберите действие",
                    replyMarkup: Keyboards.Menu1);
                    keyMenu = "Menu1";
                }
                else if (keyMenu == "administrator")
                {
                    if (await dataAvailability(globalUserId, "Admins 1") || await dataAvailability(globalUserId, "Admins 2"))
                    {
                        await botClient.SendTextMessageAsync(
                                 chatId: globalChatId,
                                 text: "😎*VIP Admin*, приветствую\\)",
                                 replyMarkup: Keyboards.administratorVIP, parseMode: ParseMode.MarkdownV2);
                        keyMenu = "administratorVIP";
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, "⤴️Переход назад...", replyMarkup: Keyboards.MainMenu);
                    keyMenu = "MainMenu";
                }
                return;
            }
            #endregion

            #region Кнопка Текущее ДЗ
            if (message.Text.Trim() == "🏠Текущее ДЗ")
            {
                if (await ReturnDayWeek(true))
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]: Невозможно получить английское название дня недели для русского дня");
                    return;
                }
                await WhatWeekType();
                if (await dataAvailability(0, "Homework ?", EnglishDayThen))//проверяю есть ли вообще дз в базе
                {
                    // открытие соединения
                    sqlConnection.Open();

                    // создание команды для выполнения SQL-запроса с JOIN-ом таблиц
                    string query2 = @$"SELECT s.Id_lesson, s.lesson_name, h.homework 
                            FROM {EnglishDayThen}_Schedule s
                            LEFT JOIN {EnglishDayThen}_Homework h ON s.Id_lesson = h.Id_lesson";
                    SqlCommand command3 = new(query2, sqlConnection);
                    string Text = $"*⠀  —–⟨Дз на {await EscapeMarkdownV2(RussianDayThen)}⟩–—*\n" +
                        $"*   —––⟨{weekType}⟩––—*\n";
                    // создание объекта SqlDataReader для чтения данных
                    using (SqlDataReader reader = command3.ExecuteReader())
                    {
                        // обход результатов выборки
                        while (reader.Read())
                        {
                            string lesson_name, homework;
                            // чтение значений из текущей строки таблицы
                            lesson_name = (string)reader["lesson_name"];
                            try
                            {
                                homework = (string)reader["homework"];
                            }
                            catch//если поле пустое, то записываем ничего
                            {
                                homework = " ❌ ";
                            }

                            try
                            {
                                string lesson_name1 = lesson_name.Trim().Split('≣')[0].Trim(), lesson_name2 = lesson_name.Trim().Split('≣')[1].Trim();
                                if (!string.IsNullOrWhiteSpace(lesson_name1) && !string.IsNullOrWhiteSpace(lesson_name2))//если поделились но пустые или специально пустые
                                {
                                    if (weekType == "Числитель")
                                    {
                                        lesson_name = lesson_name1;
                                    }
                                    else
                                    {
                                        lesson_name = lesson_name2;
                                    }
                                }
                            }
                            catch { }
                            // формирование строки для переменной
                            Text += $"╟\\-‹*{await EscapeMarkdownV2(lesson_name)}:* {await EscapeMarkdownV2(homework)};";
                            Text += "\n║\n";
                        }
                    }
                    sqlConnection.Close();
                    int lastIndex = Text.LastIndexOf('║');//удаляю последнее вхождение знака
                    if (lastIndex >= 0) // проверяем, что символ найден
                    {
                        Text = Text.Substring(0, lastIndex) + Text.Substring(lastIndex + 1);
                    }
                    Text += $"*{await EscapeMarkdownV2($"—–-⟨{DateDayThen}⟩-–—")}*";
                    await botClient.SendTextMessageAsync(chatId: globalChatId, text: Text, parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.tablePicture);
                    globalTablePicture = $"10 10 Homework";
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌*Извините, но дз для дня _\"{RussianDayThen.ToUpper()}\"_ еще не заполнено `[3002]`*", parseMode: ParseMode.MarkdownV2);
                    return;
                }

                return;
            }
            #endregion
            #region Кнопка Администратор
            if (message.Text.Trim() == "👨‍💻Администратор")
            {
                if (await dataAvailability(globalUserId, "Admins"))//проверка что ты являешься любым админом
                {
                    await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                        text: "🔓Успешный вход администратора",
                                                        replyMarkup: Keyboards.administrator);
                    keyMenu = "administrator";
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом [3002]+[4003]");
                }
                return;
            }
            #endregion
            #region Кнопка Новости
            if (message.Text.Trim() == "📰Новости" || message.Text.Trim() == "/news")
            {
                if (await dataAvailability(0, "News ?"))//проверяем есть ли вообще новости в базе
                {
                    await botClient.SendTextMessageAsync(
                    chatId: globalChatId,
                    text: "📰Последняя новость:");

                    sqlConnection.Open();//совмещаем две таблицы для получения имени, т.е кто добавил новость
                    // Создаем запрос на выборку данных из таблиц News и Users по максимальному номеру новости
                    SqlCommand command = new("SELECT TOP 1 n.*, u.username FROM News n JOIN Users u ON n.user_id = u.user_id ORDER BY n.newsNumber DESC", sqlConnection);
                    // Выполняем запрос на выборку данных из таблиц News и Users
                    SqlDataReader reader = command.ExecuteReader();
                    // Считываем данные
                    if (reader.Read())
                    {
                        // Получаем данные
                        byte[] image = (byte[])reader["image"];
                        int newsNumber = reader.GetInt32(reader.GetOrdinal("newsNumber"));
                        long userId = reader.GetInt64(reader.GetOrdinal("user_id"));
                        DateTime data = reader.GetDateTime(reader.GetOrdinal("data"));
                        string newsTitle = reader.GetString(reader.GetOrdinal("newsTitle"));
                        string news = reader.GetString(reader.GetOrdinal("news"));
                        string userName = reader.GetString(reader.GetOrdinal("username"));
                        reader.Close();
                        //создаем сообщение
                        string caption = $"⌞⌋𝕹𝕰𝖂𝕾 *№{newsNumber}*⌊⌟" +
                            $"*{await EscapeMarkdownV2(newsTitle)}*"
                          + $"{await EscapeMarkdownV2(news)}\n\n"
                          + $"║☱*Автор: {await EscapeMarkdownV2(userName)}*☱║\n"
                          + $"*—–\\-⟨{await EscapeMarkdownV2($"{data:yyyy-MM-dd}")}⟩\\-–—*";
                        //преобразуем картинку из массива байт
                        using (MemoryStream ms = new(image))
                        {
                            var messageRes = await botClient.SendPhotoAsync(
                              chatId: globalChatId,
                              photo: InputFile.FromStream(stream: ms, fileName: "image"),
                              caption: caption + "\n", // Добавляем символ перевода строки
                              parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.newsButton);
                            globalMessagePhotoId = messageRes.MessageId;
                        }
                        globalNewsNumber = newsNumber;//запоминаем номер новости для того, что-бы можно было пролистать новости дальше
                    }
                    // Закрываем соединение с базой данных
                    sqlConnection.Close();
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                    chatId: globalChatId,
                    text: "🗞Новостей пока нет [3002]");
                }
                return;
            }
            #endregion
            #region Кнопка Контакты
            if (message.Text.Trim() == "👥Контакты")
            {//просто отображаю текст с контактами

                // Создание InputFileStream из временного файла
                string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Log_dz.png");
                InputFileStream inputFile = new(new FileStream(imagePath, FileMode.Open));

                await botClient.SendPhotoAsync(
                chatId: globalChatId,
                photo: inputFile,
                caption: "👥*☻◄Наши контакты►☻*👥\n" +
                "➤*Разработчик бота*:\nhttps://t\\.me/Lib\\_int \n" +
                "➤*Главный администратор*:\n_Временно отсутствует_ \n" +
                "➤*Основная группа ДЗ*:\nhttps://t\\.me/kadievacrushcringe \n" +
                "➤*Группа колледжа*:\nhttps://t\\.me/vke\\_edu \n" +
                "_by Lib\\_int_©️",
                replyMarkup: Keyboards.Contacts, parseMode: ParseMode.MarkdownV2);
                inputFile.Content.Close();
                return;
            }
            #endregion
            #region Кнопка Расписания
            if (message.Text.Trim() == "🗓Расписания")
            {
                await botClient.SendTextMessageAsync(
                chatId: globalChatId,
                text: "🎛Выберите действие",
                replyMarkup: Keyboards.schedules);
                keyMenu = "schedules";
                return;
            }
            #endregion

            #region Кнопка (расписание) На завтра
            if (message.Text.Trim() == "📆На завтра")
            {
                sqlConnection.Close();
                string audience1 = "", audience2 = "";
                if (await ReturnDayWeek(true))
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]: Невозможно получить английское название дня недели для русского дня");
                    return;
                }
                await WhatWeekType();

                if (await dataAvailability(0, "Schedule ?", EnglishDayThen))
                {
                    // открытие соединения
                    sqlConnection.Open();
                    // создание команды для выполнения SQL-запроса с JOIN-ом таблиц
                    SqlCommand selectCommand = new($"select * from {EnglishDayThen}_Schedule", sqlConnection);
                    string Text = $"📆Расписание для *\"{RussianDayThen.ToUpper()}\"* {weekType}:\n";
                    // создание объекта SqlDataReader для чтения данных
                    using (SqlDataReader reader = selectCommand.ExecuteReader())
                    {
                        // обход результатов выборки
                        while (reader.Read())
                        {
                            // чтение значений из текущей строки таблицы
                            string lesson_name = reader.GetString(1);
                            string ClassNum = reader.GetString(2);
                            try
                            {
                                string lesson_name1 = lesson_name.Trim().Split('≣')[0].Trim(), lesson_name2 = lesson_name.Trim().Split('≣')[1].Trim();
                                if (!string.IsNullOrWhiteSpace(lesson_name1) && !string.IsNullOrWhiteSpace(lesson_name2))//если поделились но пустые или специально пустые
                                {
                                    if (weekType == "Числитель")
                                    {
                                        lesson_name = lesson_name1;
                                    }
                                    else
                                    {
                                        lesson_name = lesson_name2;
                                    }
                                }
                            }
                            catch { }
                            try
                            {
                                audience1 = ClassNum.Trim().Split('≣')[0].Trim();
                                audience2 = ClassNum.Trim().Split('≣')[1].Trim();
                                if (weekType == "Числитель")
                                {
                                    ClassNum = audience1;
                                }
                                else
                                {
                                    ClassNum = audience2;
                                }
                            }
                            catch
                            {
                                if (lesson_name.ToLower().Contains("ничего") || lesson_name.ToLower().Contains("---"))
                                {
                                    ClassNum = "";
                                }
                            }
                            // формирование строки
                            Text += $"*{await EscapeMarkdownV2(lesson_name)}* ┇ {await EscapeMarkdownV2(ClassNum)}\n";
                        }
                    }
                    sqlConnection.Close();
                    await botClient.SendTextMessageAsync(chatId: globalChatId,
                    text: Text, parseMode: ParseMode.MarkdownV2);//отправляем
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Расписания на завтра отсутствует [3002]");
                    return;
                }
                return;
            }
            #endregion
            #region Кнопка (расписание) На всю неделю
            if (message.Text.Trim() == "🗓На всю неделю")
            {
                sqlConnection.Close();

                int count = 0;
                string Text = $"🗓*Расписание на всю неделю:*\n\n", audience1 = "", audience2 = "", lesson_name1 = "", lesson_name2 = "";
                foreach (string RussiansDay in weekDays.Keys)
                {
                    if (weekDays.TryGetValue(RussiansDay, out string EnglishDay))
                    {
                        if (await dataAvailability(0, "Schedule ?", EnglishDay))
                        {
                            // открытие соединения
                            sqlConnection.Open();
                            // создание команды для выполнения SQL-запроса с JOIN-ом таблиц
                            SqlCommand selectCommand = new($"select * from {EnglishDay}_Schedule", sqlConnection);
                            Text += $"❐*\"{RussiansDay.ToUpper()}\"*:\n";
                            // создание объекта SqlDataReader для чтения данных
                            using (SqlDataReader reader = selectCommand.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    // чтение значений из текущей строки таблицы
                                    string lesson_name = reader.GetString(1);
                                    string ClassNum = reader.GetString(2);
                                    // lesson_name = lesson_name.Replace("≣", " //");
                                    try
                                    {
                                        lesson_name1 = lesson_name.Trim().Split('≣')[0].Trim();
                                        lesson_name2 = lesson_name.Trim().Split('≣')[1].Trim();
                                        try
                                        {
                                            audience1 = ClassNum.Trim().Split('≣')[0].Trim();
                                            audience2 = ClassNum.Trim().Split('≣')[1].Trim();

                                            Text += $"*{await EscapeMarkdownV2(lesson_name1)}* ┇ _{await EscapeMarkdownV2(audience1)}_ //*{await EscapeMarkdownV2(await RemoveDigitsAsync(lesson_name2))}* ┇ _{await EscapeMarkdownV2(audience2)}_\n";
                                        }
                                        catch
                                        {
                                            Text += $"*{await EscapeMarkdownV2(lesson_name1)}* //*{await EscapeMarkdownV2(await RemoveDigitsAsync(lesson_name2))}* ┇ _{await EscapeMarkdownV2(ClassNum)}_\n";
                                        }
                                    }
                                    catch
                                    {
                                        try// формирование строки
                                        {
                                            audience1 = ClassNum.Trim().Split('≣')[0].Trim();
                                            audience2 = ClassNum.Trim().Split('≣')[1].Trim();

                                            Text += $"*{await EscapeMarkdownV2(lesson_name)}* ┇ _{await EscapeMarkdownV2(audience1)}_\n";
                                        }
                                        catch
                                        {
                                            Text += $"*{await EscapeMarkdownV2(lesson_name)}* ┇ _{await EscapeMarkdownV2(ClassNum)}_\n";
                                        }
                                    }
                                    count++;
                                }
                            }
                            sqlConnection.Close();
                        }
                        else
                        {
                            Text += $"❐Расписание для *\"{RussiansDay.ToUpper()}\"*:\n" +
                                $"🛑*Отсутствует*🛑\n";
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]: Невозможно получить английское название дня недели для русского дня {RussiansDay}");
                        return;//маловероятная возможная ошибка
                    }
                    Text += "\n";
                }
                //отправляем
                await botClient.SendTextMessageAsync(chatId: globalChatId,
                    text: Text, parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.tablePicture);
                globalTablePicture = $"10 {count} Schedule";
                return;
            }
            #endregion
            #region Кнопка Расписание звонков
            if (message.Text.Trim() == "🔔Расписание звонков")
            {
                if (await dataAvailability(0, "Calls ?"))//проверяем есть ли вообще звонки в базе
                {
                    string Calls = "🛎*Расписание звонков:*\n";
                    sqlConnection.Open();
                    SqlCommand command = new("SELECT time_interval, note FROM Calls", sqlConnection);
                    SqlDataReader reader = command.ExecuteReader();
                    int count = 0;
                    while (reader.Read())//считываем звонки из базы
                    {
                        string time = reader.GetString(reader.GetOrdinal("time_interval"));
                        string note = reader.GetString(reader.GetOrdinal("note"));
                        Calls += $"`{time}`" + note + "\n";//составляем сообщение
                        count++;
                    }
                    reader.Close();
                    sqlConnection.Close();
                    //отправляем
                    await botClient.SendTextMessageAsync(chatId: globalChatId,
                    text: Calls, parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.tablePicture);
                    globalTablePicture = $"10 {count} Calls";
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId: globalChatId,
                    text: "⏲Расписания звонков пока _нет_ \\[3002\\]", parseMode: ParseMode.MarkdownV2);
                }
                return;
            }
            #endregion
            #region Кнопка До конца пары
            if (message.Text.Trim() == "⏱До конца пары")
            {
                if (await dataAvailability(0, "Calls ?"))//проверяем есть ли вообще звонки в базе
                {
                    string[] schedule = Array.Empty<string>();

                    using (SqlConnection connection = new(connectionString))
                    {
                        await connection.OpenAsync();

                        string query = "SELECT time_interval FROM Calls ORDER BY lesson_number";
                        SqlCommand command = new(query, connection);
                        SqlDataReader reader = await command.ExecuteReaderAsync();

                        while (await reader.ReadAsync())
                        {
                            string timeInterval = reader.GetString(0);
                            timeInterval = await RemoveDigitsAsync(timeInterval);
                            timeInterval = timeInterval.Remove(timeInterval.Length - 2).Trim();
                            schedule = await AddToArray(schedule, timeInterval);
                        }

                        reader.Close();
                    }


                    if (schedule.Length == 0)
                    {
                        await botClient.SendTextMessageAsync(chatId: globalChatId,
                        text: "⏲Расписания звонков пока _нет_ \\[3002\\]", parseMode: ParseMode.MarkdownV2);
                        return;
                    }

                    TimeSpan currentTime = DateTime.Now.TimeOfDay;
                    for (int i = 0; i < schedule.Length; i++)
                    {
                        string[] parts = schedule[i].Split('⌁');
                        if (parts.Length == 2)
                        {
                            TimeSpan start = TimeSpan.Parse(parts[0].Trim());
                            TimeSpan end = TimeSpan.Parse(parts[1].Trim());

                            if (currentTime >= start && currentTime < end)
                            {
                                TimeSpan remainingTime = end - currentTime;
                                string formattedTime = await FormatTime(remainingTime);
                                await botClient.SendTextMessageAsync(chatId: globalChatId,
                                text: $"⏱*Оставшееся время до конца пары:* {await EscapeMarkdownV2(formattedTime)}", parseMode: ParseMode.MarkdownV2);
                                return;
                            }
                            else if (currentTime < start)
                            {
                                TimeSpan timeUntilNextClass = start - currentTime;
                                string formattedTime = await FormatTime(timeUntilNextClass);
                                await botClient.SendTextMessageAsync(chatId: globalChatId,
                                text: $"⏱*До начала следующей пары осталось:* {await EscapeMarkdownV2(formattedTime)}", parseMode: ParseMode.MarkdownV2);
                                return;
                            }
                        }
                    }
                    await botClient.SendTextMessageAsync(chatId: globalChatId,
                        text: "*❗️Сейчас _нет_ занятий❗️*", parseMode: ParseMode.MarkdownV2);
                    return;
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId: globalChatId,
                    text: "⏲Расписания звонков пока _нет_ \\[3002\\]", parseMode: ParseMode.MarkdownV2);
                }
                return;
            }
            #endregion

            #region Кнопка Управление ДЗ
            if (message.Text.Trim() == "📖Управление \"ДЗ\"")
            {
                if (await dataAvailability(globalUserId, "Admins"))
                {
                    await botClient.SendTextMessageAsync(
                    chatId: globalChatId,
                    text: "🎛Выберите действие",
                    replyMarkup: Keyboards.homeTasks);
                    keyMenu = "homeTasks";
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                }
                return;
            }
            #endregion
            #region Кнопка Управление Расписание
            if (message.Text.Trim() == "⏰Управление \"Расписание\"")
            {
                if (await dataAvailability(globalUserId, "Admins"))
                {
                    await botClient.SendTextMessageAsync(
                    chatId: globalChatId,
                    text: "🎛Выберите действие",
                    replyMarkup: Keyboards.schedule);
                    keyMenu = "schedule";
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                }
                return;
            }
            #endregion

            #region Кнопка Изменить звонки
            if (message.Text.Trim() == "✏️Изменить звонки")
            {
                if (await buttonTest())
                {
                    if (await dataAvailability(globalUserId, "Admins 1") || await dataAvailability(globalUserId, "Admins 2"))
                    {
                        await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                            text: "✏️Для добавления звонков введите их в данном формате:\n" +
                                                            "*Начало пары \\- Конец пары* `☰` *_Комментарий_* _\\(не обязательно\\)_\n" +
                                                            "`▒` \\(_*обязательный разделитель, нажмите для копирования*_\\)\n" +
                                                            "*Начало пары \\- Конец пары* `☰` *_Комментарий_* _\\(не обязательно\\)_\n" +
                                                            "`▒` \\(_*обязательный разделитель, нажмите для копирования*_\\)\n" +
                                                            "_И т\\.д\\. сколько нужно_" +
                                                            "\n\n" +
                                                            "_`▒` и `☰` \\- являются обязательными разделителями\\._" +
                                                            "\n\n" +
                                                            "Т\\.е выглядеть будет так \\(пример\\):\n" +
                                                            "09:00 \\- 10:20 `☰` *_Комментарий_*\n" +
                                                            "`▒`\n" +
                                                            "10:30 \\- 11:50 `☰` *_Комментарий_*\n" +
                                                            "`▒`\n" +
                                                            "12:30 \\- 13:50 `☰` *_Комментарий_*\n" +
                                                            "`▒`\n" +
                                                            "14:00 \\- 15:20 `☰` *_Комментарий_*",
                                                            replyMarkup: Keyboards.cancel, parseMode: ParseMode.MarkdownV2);
                        pressingButtons["changeCalls"] = true;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, "⭕️Для выполнения другого действия закончите работу с предыдущим\nИли введите /cancel");
                }
                return;
            }
            #endregion
            #region Кнопка Удалить звонки
            if (message.Text.Trim() == "🗑Удалить звонки")
            {
                if (await dataAvailability(globalUserId, "Admins 1") || await dataAvailability(globalUserId, "Admins 2"))
                {
                    int rowsAffected = await clearingTables($"Calls"); ;//просто удаляем все звонки

                    await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                        text: $"✅Произведено удаление *Звонков*, удалено записей: _{rowsAffected}_",
                                                        parseMode: ParseMode.MarkdownV2);
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                }

                return;
            }
            #endregion
            #region Кнопка Изменить расписание
            if (message.Text.Trim() == "✏️Изменить расписание")
            {
                if (await buttonTest())
                {
                    if (await dataAvailability(globalUserId, "Admins 1") || await dataAvailability(globalUserId, "Admins 2"))
                    {
                        await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                            text: "✏️Для добавления расписания введите его в данном формате:\n" +
                                                            "*День недели*\n" +
                                                            "`▒` \\(_*обязательный разделитель, нажмите для копирования*_\\)\n" +
                                                            "*Название пары* `☰` *_Код аудитории_*\n" +
                                                            "`▒` \\(_*обязательный разделитель, нажмите для копирования*_\\)\n" +
                                                            "*Название пары* `≣` _*Пара по Знаменателю*_ `☰` *_Код аудитории_* *_Еще Код аудитории_*\n" +
                                                            "`▒` \\(_*обязательный разделитель, нажмите для копирования*_\\)\n" +
                                                            "_И т\\.д\\. сколько нужно_" +
                                                            "\n\n" +
                                                            "`▒` и `☰` \\- являются обязательными разделителями\n" +
                                                            "`≣` и '` `'\\- является _необязательным_ разделителем для пар по *Знаменателю*\n" +
                                                            "Обратите внимание что пробел '` `' разделяет два разных _Кода аудитории_\n" +
                                                            "Так\\-же нужно обязательно писать *'`Ничего`'* или *'`\\-\\-\\-`'*, если иногда по числителю или знаменателю нет пары" +
                                                            "\n\n" +
                                                            "Т\\.е выглядеть будет так \\(пример\\):\n" +
                                                            "*Понедельник*\n" +
                                                            "`▒`\n" +
                                                            "*Программирование* `☰` *_М/1_*\n" +
                                                            "`▒`\n" +
                                                            "*Теория вероятности* `≣` *Математика* `☰` *_207_* *_105_*\n" +
                                                            "`▒`\n" +
                                                            "*Практика* `≣` *Ничего* `☰` *_Вц/5_*",
                                                            replyMarkup: Keyboards.cancel, parseMode: ParseMode.MarkdownV2);
                        pressingButtons["changeSchedule"] = true;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, "⭕️Для выполнения другого действия закончите работу с предыдущим\nИли введите /cancel");
                }
                return;
            }
            #endregion
            #region Удалить Расписание
            if (message.Text.Trim() == "🗑Удалить расписание")
            {
                if (await buttonTest())
                {
                    if (await dataAvailability(globalUserId, "Admins 1") || await dataAvailability(globalUserId, "Admins 2"))
                    {
                        await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                            text: $"✂️Для удаления расписания введите *День недели* или введите *ВСЕ* для удаления всего расписания",
                                                            parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.cancel);
                        pressingButtons["deleteSchedule"] = true;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, "⭕️Для выполнения другого действия закончите работу с предыдущим\nИли введите /cancel");
                }
                return;
            }
            #endregion

            #region Кнопка Добавить ДЗ
            if (message.Text.Trim() == "➕Добавить ДЗ")
            {
                if (await buttonTest())
                {
                    if (await dataAvailability(globalUserId, "Admins"))
                    {
                        if (await ReturnDayWeek(true))
                        {
                            await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]: Невозможно получить английское название дня недели для русского дня");
                            return;
                        }

                        if (!await dataAvailability(0, "Homework ?", EnglishDayThen))
                        {
                            await clearingTables($"{EnglishDayNow}_Homework");

                            if (await dataAvailability(0, "Schedule ?", EnglishDayThen))
                            {
                                await CopyDataDz(EnglishDayThen);//копируем данные расписания из расписания в дз
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(globalChatId, $"❌Расписания на завтра отсутствует или еще время до 10:00 для заполнения нового дня [3002]");
                                return;
                            }
                        }

                        string text = await DzInfo();
                        bool FTres = bool.Parse(text.Split('☰')[1].Trim());
                        var messageRes = await botClient.SendTextMessageAsync(chatId: globalChatId, text: text.Split('☰')[0].Trim(), parseMode: ParseMode.MarkdownV2);
                        globalDzInfo = messageRes.MessageId;
                        if (FTres)
                        {
                            await botClient.SendTextMessageAsync(chatId: globalChatId, text: "✅Все дз заполнено, для редактирования перейдите к кнопке \"✏️Редактировать ДЗ\"");
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId: globalChatId,
                                text: "✏️Для добавления ДЗ введите *ID урока* или скопируйте его из сообщения выше⬆️\n" +
                                "_*Вводите в данном формате*_: ID урока `☰` ТекстДЗ\n" +
                                "`☰` \\- является обязательным разделителем _\\(нажмите для копирования\\)\\._",
                                parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.cancel);
                            pressingButtons["addHomework"] = true;
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом [3002]+[4003]");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, "⭕️Для выполнения другого действия закончите работу с предыдущим\nИли введите /cancel");
                }
                return;
            }
            #endregion
            #region Кнопка Редактировать ДЗ
            if (message.Text.Trim() == "✏️Редактировать ДЗ")
            {
                if (await buttonTest())
                {
                    if (await dataAvailability(globalUserId, "Admins"))
                    {
                        if (await ReturnDayWeek(true))
                        {
                            await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]: Невозможно получить английское название дня недели для русского дня");
                            return;
                        }

                        if (!await dataAvailability(0, "Homework ?", EnglishDayThen))
                        {
                            await clearingTables($"{EnglishDayNow}_Homework");

                            if (await dataAvailability(0, "Schedule ?", EnglishDayThen))
                            {
                                await CopyDataDz(EnglishDayThen);//копируем данные расписания из расписания в дз
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(globalChatId, $"❌Расписания на завтра отсутствует или еще время до 10:00 для заполнения нового дня [3002]");
                                return;
                            }
                        }

                        var messageRes = await botClient.SendTextMessageAsync(chatId: globalChatId, text: await DzTextInfo(), parseMode: ParseMode.MarkdownV2);
                        globalDzTextInfo = messageRes.MessageId;

                        await botClient.SendTextMessageAsync(chatId: globalChatId, text: "✏️Для изменения ДЗ введите *ID урока* или скопируйте его из сообщения выше⬆️\n" +
                                "_*Вводите в данном формате*_: ID урока `☰` ТекстДЗ\n" +
                                "`☰` \\- является обязательным разделителем _\\(нажмите для копирования\\)\\._",
                                parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.cancel);
                        pressingButtons["changeHomework"] = true;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом [3002]+[4003]");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, "⭕️Для выполнения другого действия закончите работу с предыдущим\nИли введите /cancel");
                }
                return;
            }
            #endregion
            #region Кнопка Удлаить ДЗ
            if (message.Text.Trim() == "🗑Удалить ДЗ")
            {
                if (await buttonTest())
                {
                    if (await dataAvailability(globalUserId, "Admins"))
                    {
                        if (await ReturnDayWeek(true))
                        {
                            await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]: Невозможно получить английское название дня недели для русского дня");
                            return;
                        }

                        if (await dataAvailability(0, "Schedule ?", EnglishDayThen))
                        {
                            var messageRes = await botClient.SendTextMessageAsync(chatId: globalChatId, text: await DzTextInfo(), parseMode: ParseMode.MarkdownV2);
                            globalDzTextInfo = messageRes.MessageId;

                            await botClient.SendTextMessageAsync(chatId: globalChatId, text: "✏️Для удаления ДЗ введите *ID урока* или скопируйте его из сообщения выше⬆️\n" +
                                "Для удаления всего дз введите \"ВСЕ\"",
                                    parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.cancel);
                            pressingButtons["deleteHomework"] = true;
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(globalChatId, $"❌Расписания на завтра отсутствует или еще время до 10:00 [3002]");
                            return;
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом [3002]+[4003]");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, "⭕️Для выполнения другого действия закончите работу с предыдущим\nИли введите /cancel");
                }
                return;
            }
            #endregion

            #region Кнопка Управление Админами
            if (message.Text.Trim() == "👨‍💻Управление админами")
            {
                if (await dataAvailability(globalUserId, "Admins 1") || await dataAvailability(globalUserId, "Admins 2"))
                {
                    await botClient.SendTextMessageAsync(
                    chatId: globalChatId,
                    text: "🎛Выберите действие",
                    replyMarkup: Keyboards.administratorManagement);
                    keyMenu = "administratorManagement";
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                }
                return;
            }
            #endregion
            #region Управление новостями
            if (message.Text.Trim() == "📰Управление новостями")
            {
                if (await dataAvailability(globalUserId, "Admins 1") || await dataAvailability(globalUserId, "Admins 2"))
                {
                    await botClient.SendTextMessageAsync(
                    chatId: globalChatId,
                    text: "🎛Выберите действие",
                    replyMarkup: Keyboards.News);
                    keyMenu = "News";
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                }
                return;
            }
            #endregion
            #region Кнопка База данных
            if (message.Text.Trim() == "🗃База данных")
            {
                if (await dataAvailability(globalUserId, "Admins 1") || await dataAvailability(globalUserId, "Admins 2"))
                {
                    await botClient.SendTextMessageAsync(
                    chatId: globalChatId,
                    text: "🎛Выберите действие",
                    replyMarkup: Keyboards.database);
                    keyMenu = "database";
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                }
                return;
            }
            #endregion
            #region Кнопка Управление ботом
            if (message.Text.Trim() == "🤖Управление ботом")
            {
                if (await dataAvailability(globalUserId, "Admins 1"))
                {
                    await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                        text: "👾*Приветствую, ◖_модератор_◗*",
                                                        replyMarkup: Keyboards.botManagement, parseMode: ParseMode.MarkdownV2);
                    keyMenu = "botManagement";
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003].\n" +
                        $"📈Для входа в это меню нужно обладать правами администратора не менее 1 типа.");
                }
                return;
            }
            #endregion

            #region Кнопка Добавить Админа
            if (message.Text.Trim() == "➕Добавить Админа")
            {
                if (await buttonTest())
                {
                    if (await dataAvailability(globalUserId, "Admins 1") || await dataAvailability(globalUserId, "Admins 2"))
                    {
                        await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                            text: "📝Введите *имя/ID* Пользователя для добавления",
                                                            replyMarkup: Keyboards.cancel, parseMode: ParseMode.MarkdownV2);
                        pressingButtons["addAdmin"] = true;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, "⭕️Для выполнения другого действия закончите работу с предыдущим\nИли введите /cancel");
                }
                return;
            }
            #endregion
            #region Кнопка Удалить Админа
            if (message.Text.Trim() == "🗑Удалить Админа")
            {
                if (await buttonTest())
                {
                    if (await dataAvailability(globalUserId, "Admins 1") || await dataAvailability(globalUserId, "Admins 2"))
                    {
                        await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                            text: "📝Введите *имя/ID* Администратора для удаления",
                                                            replyMarkup: Keyboards.cancel, parseMode: ParseMode.MarkdownV2);
                        pressingButtons["deleteAdmin"] = true;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, "⭕️Для выполнения другого действия закончите работу с предыдущим\nИли введите /cancel");
                }
                return;
            }
            #endregion

            #region Кнопка Информация о Админах
            if (message.Text.Trim() == "👨‍💻Информация о Админах")
            {
                if (await dataAvailability(globalUserId, "Admins 1") || await dataAvailability(globalUserId, "Admins 2"))
                {
                    // открытие соединения
                    sqlConnection.Open();

                    // создание объекта команды SQL
                    using (SqlCommand command = new("SELECT user_admin_id, user_id, username, admin_type FROM Admins", sqlConnection))
                    {
                        // создание объекта для чтения данных
                        using SqlDataReader reader = command.ExecuteReader();
                        // добавление заголовка таблицы
                        string table = "\n", tableHeader = "ID Админа║    ID Юзера   ║Тип Админа║Имя Юзера";

                        int count = 0;
                        // чтение данных из таблицы и добавление строк в таблицу
                        while (reader.Read())
                        {
                            int userAdminId = reader.GetInt32(0);
                            long userId = reader.GetInt64(1);
                            string username = reader.GetString(2);
                            int adminType = reader.GetInt32(3);

                            table += $"     {userAdminId}        ┇`{userId}`┇           {adminType}          ┇`{await EscapeMarkdownV2(username)}`\n";
                            count++;
                        }
                        // сохранение строки таблицы в переменную типа string
                        string tableString = tableHeader + table;
                        await botClient.SendTextMessageAsync(globalChatId, "👨‍💻*Информация о админах:* \n" + tableString, parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.tablePicture);
                        globalTablePicture = $"4 {count} Admins";
                    }
                    sqlConnection.Close();
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                }
                return;
            }
            #endregion
            #region Кнопка Информация о Пользователях
            if (message.Text.Trim() == "👤Информация о Пользователях")
            {
                if (await dataAvailability(globalUserId, "Admins 1") || await dataAvailability(globalUserId, "Admins 2"))
                {
                    // открытие соединения
                    sqlConnection.Open();

                    // создание объекта команды SQL
                    using (SqlCommand command = new("SELECT user_id, username, user_group FROM Users", sqlConnection))
                    {
                        // создание объекта для чтения данных
                        using SqlDataReader reader = command.ExecuteReader();
                        // добавление заголовка таблицы
                        string table = "\n", tableHeader = "ID Пользователя║      Группа      ║Имя Пользователя";

                        int count = 0;
                        // чтение данных из таблицы и добавление строк в таблицу
                        while (reader.Read())
                        {
                            string userId = reader.GetInt64(0).ToString();
                            string username = reader.GetString(1);
                            string userGroup = reader.GetString(2);
                            if (userGroup.Length < 9)
                            {
                                for (int i = 0; i <= 9 - userGroup.Length; i++)
                                {
                                    userGroup = " " + userGroup + " ";
                                }
                                //userGroup = " " + userGroup;
                            }
                            if (userId.Length < 10)
                            {
                                for (int i = 0; i <= 10 - userId.Length; i++)
                                {
                                    userId += " ";
                                }
                                //userId = userId + " ";
                            }

                            table += $"    `{userId}`     ┇ `{await EscapeMarkdownV2(userGroup)}` ┇`{await EscapeMarkdownV2(username)}`\n";
                            count++;
                        }
                        // сохранение строки таблицы в переменную типа string
                        string tableString = tableHeader + table;
                        await botClient.SendTextMessageAsync(globalChatId, "👥*Информация о пользователях:* \n" + tableString, parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.tablePicture);
                        globalTablePicture = $"3 {count} Users";
                    }
                    sqlConnection.Close();
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                }
                return;
            }
            #endregion

            #region Кнопка Добавить новость
            if (message.Text.Trim() == "➕Добавить новость")
            {
                if (await buttonTest())
                {
                    if (await dataAvailability(globalUserId, "Admins 1") || await dataAvailability(globalUserId, "Admins 2"))
                    {
                        await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                            text: "✏️Для добавления новости введите её в данном формате:\n" +
                                                            "_Номер/IdНовости_ \\(_*обязательно число*_\\)\n" +
                                                            "`▒` \\(_*обязательный разделитель, нажмите для копирования*_\\)\n" +
                                                            "_Название новости_ \\(_*длиной до 150 символов*_\\)\n" +
                                                            "`▒` \\(_*обязательный разделитель, нажмите для копирования*_\\)\n" +
                                                            "_Текст новости_ \\(_*длиной до 500 символов*_\\)\n" +
                                                            "_И т\\.д\\. сколько нужно_" +
                                                            "\n\n" +
                                                            "Т\\.е выглядеть будет так \\(пример\\):\n" +
                                                            "*11*\n" +
                                                            "`▒`\n" +
                                                            "*Название новости11*\n" +
                                                            "`▒`\n" +
                                                            "*Текст новости номер11*",
                                                            replyMarkup: Keyboards.cancel, parseMode: ParseMode.MarkdownV2);
                        pressingButtons["addingNews"] = true;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, "⭕️Для выполнения другого действия закончите работу с предыдущим\nИли введите /cancel");
                }
                return;
            }
            #endregion
            #region Кнопка Удалить новость
            if (message.Text.Trim() == "🗑Удалить новость")
            {
                if (await buttonTest())
                {
                    if (await dataAvailability(globalUserId, "Admins 1") || await dataAvailability(globalUserId, "Admins 2"))
                    {
                        await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                            text: "✏️Введите *ID* Новости для удаления",
                                                            replyMarkup: Keyboards.cancel, parseMode: ParseMode.MarkdownV2);
                        pressingButtons["deleteNews"] = true;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, "⭕️Для выполнения другого действия закончите работу с предыдущим\nИли введите /cancel");
                }
                return;
            }
            #endregion
            #region Кнопка Редактировать новость
            if (message.Text.Trim() == "✏️Редактировать новость")
            {
                if (await buttonTest())
                {
                    if (await dataAvailability(globalUserId, "Admins 1") || await dataAvailability(globalUserId, "Admins 2"))
                    {
                        await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                            text: "✏️Для редактирования новости введите её в данном формате:\n" +
                                                            "_Номер/IdНовости_ \\(_*обязательно число*_\\)\n" +
                                                            "`▒` \\(_*обязательный разделитель, нажмите для копирования*_\\)\n" +
                                                            "_Название новости_ \\(_*длиной до 150 символов*_\\)\n" +
                                                            "`▒` \\(_*обязательный разделитель, нажмите для копирования*_\\)\n" +
                                                            "_Текст новости_ \\(_*длиной до 500 символов*_\\)\n" +
                                                            "_И т\\.д\\. сколько нужно_" +
                                                            "\n\n" +
                                                            "Т\\.е выглядеть будет так \\(пример\\):\n" +
                                                            "*11*\n" +
                                                            "`▒`\n" +
                                                            "*Название новости11*\n" +
                                                            "`▒`\n" +
                                                            "*Текст новости номер11*" +
                                                            "_*Если вы хотите изменить фото новости введите*_ /update_photo_news",
                                                            replyMarkup: Keyboards.cancel, parseMode: ParseMode.MarkdownV2);
                        pressingButtons["changeNews"] = true;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, "⭕️Для выполнения другого действия закончите работу с предыдущим\nИли введите /cancel");
                }
                return;
            }
            #endregion

            #region Сообщение всем
            if (message.Text.Trim() == "🗣Сообщение всем")
            {
                if (await buttonTest())
                {
                    if (await dataAvailability(globalUserId, "Admins 1"))
                    {
                        await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                            text: "✏️Введите *Текст*, который будет отправлен всем",
                                                            replyMarkup: Keyboards.cancel, parseMode: ParseMode.MarkdownV2);
                        pressingButtons["messageTextEveryone"] = true;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, "⭕️Для выполнения другого действия закончите работу с предыдущим\nИли введите /cancel");
                }
                return;
            }
            #endregion
            #region Выключение бота
            if (message.Text.Trim() == "🛑Выключение")
            {
                if (await buttonTest())
                {
                    if (await dataAvailability(globalUserId, "Admins 1"))
                    {
                        await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                            text: "❓Подтвердите *Выключение*",
                                                            replyMarkup: Keyboards.shutdownCheck, parseMode: ParseMode.MarkdownV2);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, "⭕️Для выполнения другого действия закончите работу с предыдущим\nИли введите /cancel");
                }
                return;
            }
            #endregion
            #region Б/Р Пользователей
            if (message.Text.Trim() == "🔏Б/Р Пользователей")
            {
                if (await buttonTest())
                {
                    if (await dataAvailability(globalUserId, "Admins 1"))
                    {
                        await botClient.SendTextMessageAsync(
                          chatId: globalChatId,
                          text: "🎛Выберите действие",
                          replyMarkup: Keyboards.lockUnlockUsers);
                        keyMenu = "lockUnlockUsers";
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, "⭕️Для выполнения другого действия закончите работу с предыдущим\nИли введите /cancel");
                }
                return;
            }
            #endregion

            #region Заблокировать пользователя
            if (message.Text.Trim() == "🔐Заблокировать")
            {
                if (await buttonTest())
                {
                    if (await dataAvailability(globalUserId, "Admins 1"))
                    {
                        await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                            text: "✏️Введите *ID Пользователя*, который будет заблокирован",
                                                            replyMarkup: Keyboards.cancel, parseMode: ParseMode.MarkdownV2);
                        pressingButtons["lockUsers"] = true;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, "⭕️Для выполнения другого действия закончите работу с предыдущим\nИли введите /cancel");
                }
                return;
            }
            #endregion
            #region Разблокировать пользователя
            if (message.Text.Trim() == "🔓Разблокировать")
            {
                if (await buttonTest())
                {
                    if (await dataAvailability(globalUserId, "Admins 1"))
                    {
                        await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                            text: "✏️Введите *ID Пользователя*, который будет разблокирован",
                                                            replyMarkup: Keyboards.cancel, parseMode: ParseMode.MarkdownV2);
                        pressingButtons["unlockUsers"] = true;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, "⭕️Для выполнения другого действия закончите работу с предыдущим\nИли введите /cancel");
                }
                return;
            }
            #endregion

            #region Кнопка Назад
            if (message.Text.Trim() == "◀️Назад")//просто переходит назад в меню по текущему меню в котором сейчас находимся
            {
                var replyMarkup = Keyboards.MainMenu;
                switch (keyMenu)
                {
                    case "schedules":
                        replyMarkup = Keyboards.Menu1;
                        keyMenu = "Menu1";
                        break;
                    case "MainMenu":
                    case "Menu1":
                    case "administrator":
                        replyMarkup = Keyboards.MainMenu;
                        keyMenu = "MainMenu";
                        break;
                    case "database":
                    case "botManagement":
                    case "administratorManagement":
                    case "News":
                        replyMarkup = Keyboards.administratorVIP;
                        keyMenu = "administratorVIP";
                        break;
                    case "administratorVIP":
                    case "homeTasks":
                    case "schedule":
                        replyMarkup = Keyboards.administrator;
                        keyMenu = "administrator";
                        break;
                    case "lockUnlockUsers":
                        replyMarkup = Keyboards.botManagement;
                        keyMenu = "botManagement";
                        break;
                    default:
                        replyMarkup = Keyboards.MainMenu;
                        keyMenu = "MainMenu";
                        break;
                }
                await botClient.SendTextMessageAsync(globalChatId, "⤴️Переход назад...", replyMarkup: replyMarkup);

                await Cancel(false, false);
                return;
            }
            #endregion

            #region Изменения группы
            if (pressingButtons["changeGroupFT"] && message.Text.Trim().ToLower() != "/cancel")//изменение группы через кнопку
            {
                sqlConnection.Close();
                string userGroup = message.Text.Trim();
                if (userGroup == null || userGroup == String.Empty)
                {
                    await botClient.SendTextMessageAsync(globalChatId, "❌Ошибка [4002]: Вы вели группу некорректно\n");
                    return;
                }
                else
                {
                    sqlConnection.Open();
                    SqlCommand updateCommand = new("UPDATE Users SET user_group = @userGroup WHERE user_id = @userId", sqlConnection);
                    updateCommand.Parameters.AddWithValue("@userId", globalUserId);
                    updateCommand.Parameters.AddWithValue("@userGroup", userGroup);
                    updateCommand.ExecuteNonQuery();
                    sqlConnection.Close();

                    await botClient.SendTextMessageAsync(globalChatId, $"✅Группа успешно изменена!\nТеперь ваша группа: {userGroup}");
                    try//проверка, можно ли отредактировать сообщение
                    {
                        await botClient.EditMessageTextAsync(globalChatId, globalMessageTextId, await userProfile(globalUserId), replyMarkup: Keyboards.Profile, parseMode: ParseMode.MarkdownV2); //Обновляю сообщение
                    }
                    catch
                    {
                        await botClient.SendTextMessageAsync(globalChatId, await userProfile(globalUserId), replyMarkup: Keyboards.Profile, parseMode: ParseMode.MarkdownV2);
                    }
                }
                pressingButtons["changeGroupFT"] = false;
                return;
            }
            #endregion
            #region Изменения имени
            if (pressingButtons["changeNameFT"] && message.Text.Trim().ToLower() != "/cancel")//изменение имени через кнопку
            {
                sqlConnection.Close();
                string userName = message.Text.Trim();
                if (string.IsNullOrEmpty(userName))
                {
                    userName = "Теперь у вас нет имени...";
                    await botClient.SendTextMessageAsync(globalChatId, "❌Ошибка [4002]: Вы ввели имя некорректно");
                    return;
                }
                else
                {
                    sqlConnection.Open();
                    SqlCommand updateCommand = new("UPDATE Users SET username = @userName WHERE user_id = @userId", sqlConnection);
                    updateCommand.Parameters.AddWithValue("@userId", globalUserId);
                    updateCommand.Parameters.AddWithValue("@userName", userName);
                    updateCommand.ExecuteNonQuery();
                    sqlConnection.Close();

                    await botClient.SendTextMessageAsync(globalChatId, $"✅Имя успешно изменено!\nТеперь ваше имя: {userName}");
                    try//проверка, можно ли отредактировать сообщение
                    {
                        await botClient.EditMessageTextAsync(globalChatId, globalMessageTextId, await userProfile(globalUserId), replyMarkup: Keyboards.Profile, parseMode: ParseMode.MarkdownV2); //Обновляю сообщение
                    }
                    catch
                    {
                        await botClient.SendTextMessageAsync(globalChatId, await userProfile(globalUserId), replyMarkup: Keyboards.Profile, parseMode: ParseMode.MarkdownV2);
                    }
                }
                pressingButtons["changeNameFT"] = false;
                return;
            }
            #endregion
            #region Добавления админа
            if (pressingButtons["addAdmin"] && message.Text.Trim().ToLower() != "/cancel")
            {
                sqlConnection.Close();
                int adminRang = 3;
                string username = "Имя";
                if (long.TryParse(message.Text.Trim(), out long userId))//проверяем можно ли сообщение преобразовать в int
                {
                    // обработка id пользователя
                    var user = userId;
                    if (await dataAvailability(user, "Users"))
                    {
                        if (await dataAvailability(user, "Admins"))
                        {
                            await botClient.SendTextMessageAsync(
                             chatId: globalChatId,
                             text: "❌Ошибка [3003]: Введенный пользователь уже является админом.", replyMarkup: Keyboards.cancel);
                            pressingButtons["addAdmin"] = false;
                            return;
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(
                             chatId: globalChatId,
                             text: "🔍Введенный ID Пользователя обнаружен, производится добавление...");

                            sqlConnection.Open();
                            SqlCommand selectCommandUser = new("SELECT * FROM Users WHERE user_id = @userId", sqlConnection);
                            selectCommandUser.Parameters.AddWithValue("@userId", userId);
                            SqlDataReader readerUser = selectCommandUser.ExecuteReader();

                            if (readerUser.Read())//считываем данные о пользователе
                            {
                                adminRang = 3;//стандартный тип
                                username = readerUser.GetString(readerUser.GetOrdinal("username"));
                                readerUser.Close();
                            }

                            // Добавляем запись в таблицу
                            SqlCommand insertCommand = new("INSERT into Admins(user_id, username, admin_type) values (@userId, @username, @adminType)", sqlConnection);
                            insertCommand.Parameters.AddWithValue("@userId", user);
                            insertCommand.Parameters.AddWithValue("@username", username);
                            insertCommand.Parameters.AddWithValue("@adminType", adminRang);
                            insertCommand.ExecuteNonQuery(); //1
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                         chatId: globalChatId,
                         text: "❌Ошибка [3002]: Введенного ID Пользователя не обнаружено, проверьте правильность введённых данных.", replyMarkup: Keyboards.cancel);
                        return;
                    }
                }
                else
                {
                    // обработка имени пользователя если не удалось преобразовать в int
                    var user = message.Text.Trim();
                    if (await dataAvailability(0, "Users Name", user))//проверяем есть ли чел
                    {
                        if (await dataAvailability(0, "Admins Name", user))
                        {
                            await botClient.SendTextMessageAsync(
                             chatId: globalChatId,
                             text: "❌Введенный пользователь уже является админом.", replyMarkup: Keyboards.cancel);
                            pressingButtons["addAdmin"] = false;
                            return;
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(
                             chatId: globalChatId,
                             text: "🔍Введенное имя Пользователя обнаружено, производится добавление...");

                            sqlConnection.Open();
                            SqlCommand selectCommandUser = new("SELECT * FROM Users WHERE username = @userName", sqlConnection);
                            selectCommandUser.Parameters.AddWithValue("@userName", user);
                            SqlDataReader readerUser = selectCommandUser.ExecuteReader();

                            if (readerUser.Read())//почти как и в id, только теперь мы считываем не имя, а id
                            {
                                adminRang = 3;
                                userId = readerUser.GetInt64(readerUser.GetOrdinal("user_id"));
                                readerUser.Close();
                            }

                            // Добавляем запись в таблицу
                            SqlCommand insertCommand = new("INSERT into Admins(user_id, username, admin_type) values (@userId, @username, @adminType)", sqlConnection);
                            insertCommand.Parameters.AddWithValue("@userId", userId);
                            insertCommand.Parameters.AddWithValue("@username", user);
                            insertCommand.Parameters.AddWithValue("@adminType", adminRang);
                            insertCommand.ExecuteNonQuery(); //2
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                         chatId: globalChatId,
                         text: "❌Введенного имени Пользователя не обнаружено, проверьте правильность введённых данных", replyMarkup: Keyboards.cancel);
                        return;
                    }
                }
                sqlConnection.Close();

                await botClient.SendTextMessageAsync(
                         chatId: globalChatId,
                         text: $"✅Произведено добавление Админа, назначен стандартный тип админа (3), для изменения введите:\n/change_admin_type");
                await botClient.SendTextMessageAsync(
                        chatId: userId,
                        text: $"👾Вам выдан начальный статус администратора!");
                //await UpdateIndexStatisticsAsync("Admins");

                pressingButtons["addAdmin"] = false;
                return;
            }
            #endregion
            #region Удаление админа
            if (pressingButtons["deleteAdmin"] && message.Text.Trim().ToLower() != "/cancel")
            {
                sqlConnection.Close();
                int rowsAffected = 0;
                if (long.TryParse(message.Text.Trim(), out long userId))//проверяем можно ли сообщение преобразовать в int
                {
                    // обработка id пользователя
                    var user = userId;
                    if (await dataAvailability(user, "Admins"))
                    {
                        if (await dataAvailability(user, "Admins 1"))
                        {
                            await botClient.SendTextMessageAsync(
                             chatId: globalChatId,
                             text: "❌Ошибка \\[4003\\]: Введенный ID Админа является админом 1 типа и его нельзя удалить обычным способом\\.\n" +
                             "_Для его удаления обратитесь к Главному администратору_", parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.cancel);
                            pressingButtons["deleteAdmin"] = false;
                            return;
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(
                             chatId: globalChatId,
                             text: "🔍Введенный ID Админа обнаружен, производится удаление...");
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                         chatId: globalChatId,
                         text: "❌Ошибка [3002]: Введенного ID Админа не обнаружено, проверьте правильность введённых данных", replyMarkup: Keyboards.cancel);
                        return;
                    }
                    sqlConnection.Open();//удаляем по id
                    SqlCommand command = new("DELETE FROM Admins WHERE user_id=@UserId", sqlConnection);
                    command.Parameters.AddWithValue("@UserId", user);
                    rowsAffected = command.ExecuteNonQuery();
                }
                else
                {
                    // обработка имени пользователя если не удалось преобразовать в int
                    var user = message.Text.Trim();
                    if (await dataAvailability(0, "Admins Name", user))
                    {
                        if (await dataAvailability(0, "Admins 1 Name", user))
                        {
                            await botClient.SendTextMessageAsync(
                             chatId: globalChatId,
                             text: "❌Ошибка \\[4003\\]: Введенное имя Админа является именем админа 1 типа и его нельзя удалить обычным способом\\.\n" +
                             "_Для его удаления обратитесь к Главному администратору_", parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.cancel);
                            pressingButtons["deleteAdmin"] = false;
                            return;
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(
                             chatId: globalChatId,
                             text: "🔍Введенное имя Админа обнаружено, производится удаление...");

                            sqlConnection.Open();
                            SqlCommand selectCommandUser = new("SELECT * FROM Users WHERE username = @userName", sqlConnection);
                            selectCommandUser.Parameters.AddWithValue("@userName", user);
                            SqlDataReader readerUser = selectCommandUser.ExecuteReader();

                            if (readerUser.Read())//почти как и в id, только теперь мы считываем не имя, а id
                            {
                                userId = readerUser.GetInt64(readerUser.GetOrdinal("user_id"));
                                readerUser.Close();
                            }

                            SqlCommand command = new("DELETE FROM Admins WHERE username=@Username", sqlConnection);
                            command.Parameters.AddWithValue("@Username", user);//удаляем по имени
                            rowsAffected = command.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                         chatId: globalChatId,
                         text: "❌Ошибка [3002]: Введенного имени Админа не обнаружено, проверьте правильность введённых данных", replyMarkup: Keyboards.cancel);
                        return;
                    }
                }
                sqlConnection.Close();

                await botClient.SendTextMessageAsync(
                         chatId: globalChatId,
                         text: $"✅Произведено удаление Админа, удалено записей: {rowsAffected}");
                await botClient.SendTextMessageAsync(
                        chatId: userId,
                        text: $"👾У вас был удален статус администратора!");
                //await UpdateIndexStatisticsAsync("Admins");

                pressingButtons["deleteAdmin"] = false;
                return;
            }
            #endregion
            #region Изменение типа админа
            if (pressingButtons["changeAdminType"] && message.Text.Trim().ToLower() != "/cancel")
            {
                sqlConnection.Close();
                bool FT = false;
                int[] mass;
                if (await dataAvailability(globalUserId, "Admins 1"))//смотря какой ты админ, то такой тип админа ты можешь назначить 
                {
                    mass = new int[] { 1, 2, 3 };
                    FT = true;
                }
                else
                {
                    mass = new int[] { 2, 3 };
                }
                string userName = " ";
                int adminType = 3;//стандартный тип
                if (int.TryParse(message.Text.Trim().Split(' ')[0], out int adminId))//проверяем можно ли сообщение преобразовать в int
                {
                    try//если не получается правильно поделить по пробелам
                    {
                        if (int.TryParse(message.Text.Trim().Split(' ')[1], out adminType))//если вторая часть является int
                        {
                            if (mass.Contains(adminType))//проверяем диапазон
                            {
                                if (await dataAvailability(adminId, "Admins"))
                                {
                                    sqlConnection.Open();
                                    SqlCommand updateCommand = new("UPDATE Admins SET admin_type = @adminType WHERE user_id = @adminId", sqlConnection);
                                    updateCommand.Parameters.AddWithValue("@adminType", adminType);
                                    updateCommand.Parameters.AddWithValue("@adminId", adminId);
                                    updateCommand.ExecuteNonQuery();

                                    await botClient.SendTextMessageAsync(globalChatId, $"✅Тип админа успешно изменён!\nТеперь тип админа {adminId}: {adminType}");
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(
                                     chatId: globalChatId,
                                     text: "❌Ошибка [3002]: Введенного ID Админа не обнаружено, проверьте правильность введённых данных", replyMarkup: Keyboards.cancel);
                                    return;
                                }
                            }
                            else
                            {
                                if (FT)
                                {
                                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [4003]/[3001]: Введенный тип админа не подходит к диапазону 1 - 3 включительно.", replyMarkup: Keyboards.cancel);
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [4003]/[3001]: Введенный тип админа не подходит к диапазону 2 - 3 включительно.", replyMarkup: Keyboards.cancel);
                                }
                                return;
                            }
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Введенный тип админа не является числом.", replyMarkup: Keyboards.cancel);
                            return;
                        }
                    }
                    catch
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [4002]/[3001]: Введены не все данные", replyMarkup: Keyboards.cancel);
                        return;
                    }
                }
                else
                {
                    userName = message.Text.Trim().Split(' ')[0];//получаем имя если не удалось преобразовать в int
                    try//если не получается правильно поделить по пробелам
                    {
                        if (int.TryParse(message.Text.Trim().Split(' ')[1], out adminType))//если вторая часть является int
                        {
                            if (mass.Contains(adminType))//проверяем диапазон
                            {
                                if (await dataAvailability(0, "Admins Name", userName))
                                {
                                    sqlConnection.Open();
                                    SqlCommand updateCommand = new("UPDATE Admins SET admin_type = @adminType WHERE username = @adminName", sqlConnection);
                                    updateCommand.Parameters.AddWithValue("@adminType", adminType);
                                    updateCommand.Parameters.AddWithValue("@adminName", userName);
                                    updateCommand.ExecuteNonQuery();

                                    await botClient.SendTextMessageAsync(globalChatId, $"✅Тип админа успешно изменён!\nТеперь тип админа {userName}: {adminType}");
                                    //await UpdateIndexStatisticsAsync("Admins");
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(
                                     chatId: globalChatId,
                                     text: "❌Ошибка [3002]: Введенного имени Админа не обнаружено, проверьте правильность введённых данных", replyMarkup: Keyboards.cancel);
                                    return;
                                }
                            }
                            else
                            {
                                if (FT)
                                {
                                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [4003]/[3001]: Введенный тип админа не подходит к диапазону 1 - 3 включительно.", replyMarkup: Keyboards.cancel);
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [4003]/[3001]: Введенный тип админа не подходит к диапазону 2 - 3 включительно.", replyMarkup: Keyboards.cancel);
                                }
                                return;
                            }
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Введенный тип админа не является числом.", replyMarkup: Keyboards.cancel);
                            return;
                        }
                    }
                    catch
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [4002]/[3001]: Введены не все данные", replyMarkup: Keyboards.cancel);
                        return;
                    }
                }
                sqlConnection.Close();
                pressingButtons["changeAdminType"] = false;
                return;
            }
            #endregion
            #region Добавление новости
            if (pressingButtons["addingNews"] && message.Text.Trim().ToLower() != "/cancel")
            {
                sqlConnection.Close();
                int tableIdNews = 0;
                byte[] image;
                string newsTitle = "Название новости", newsText = "Текст новости ";
                try//делим сообщение по разделителям и проверяем получается ли правильно поделить
                {
                    newsTitle = message.Text.Trim().Split('▒')[1];
                    newsText = message.Text.Trim().Split('▒')[2];
                }
                catch
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Текст введен некорректно", replyMarkup: Keyboards.cancel);
                    return;
                }
                if (string.IsNullOrWhiteSpace(newsTitle) || string.IsNullOrWhiteSpace(newsText))//если поделились но пустые или специально пустые
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [4002]: Текст или название новости являются пустыми полями.", replyMarkup: Keyboards.cancel);
                    return;
                }
                if (int.TryParse(message.Text.Trim().Split('▒')[0], out int idNews))//проверяем можно ли преобразовать в int
                {
                    if (!await dataAvailability(idNews, "News"))
                    {
                        sqlConnection.Open();
                        // Создаем запрос на выборку данных из таблицы News
                        SqlCommand command = new("SELECT TOP 1 newsNumber FROM News ORDER BY newsNumber DESC", sqlConnection);
                        // Выполняем запрос на выборку данных из таблицы News
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                tableIdNews = reader.GetInt32(reader.GetOrdinal("newsNumber"));
                            }
                            reader.Close();
                        }
                        if (idNews >= tableIdNews)//проверяем что введенный ID новости меньше либо равен максимальному
                        {
                            SqlCommand insertCommand = new("INSERT into News(newsNumber, data, user_id, newsTitle, news, image) values (@newsNumber, @dataNews, @user_id, @newsTitle, @newsText, @image)", sqlConnection);
                            insertCommand.Parameters.AddWithValue("@newsNumber", idNews);
                            insertCommand.Parameters.AddWithValue("@dataNews", DateTime.Now.Date);
                            insertCommand.Parameters.AddWithValue("@user_id", globalUserId);
                            insertCommand.Parameters.AddWithValue("@newsTitle", newsTitle);
                            insertCommand.Parameters.AddWithValue("@newsText", newsText);

                            // Получаем путь к файлу с изображением относительно папки проекта
                            string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "News.jpg");//тут мы загружаем стандартное изображение что-бы после не было ошибок при редактировании сообщения без фотографии
                            var stream = new FileStream(imagePath, FileMode.Open);
                            var memoryStream = new MemoryStream();
                            await stream.CopyToAsync(memoryStream);

                            // Получаем массив байтов из MemoryStream
                            image = memoryStream.ToArray();
                            SqlParameter imageParam = new("@image", SqlDbType.VarBinary, -1)
                            {
                                Value = image//добавляем в базу
                            };
                            insertCommand.Parameters.Add(imageParam);
                            insertCommand.ExecuteNonQuery();

                            stream.Close();
                            memoryStream.Close();
                            sqlConnection.Close();
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Введенный ID новости меньше либо равен максимальному, введите другой номер", replyMarkup: Keyboards.cancel);
                            return;
                        }

                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3003]: Введенный ID новости уже существует, введите другой номер", replyMarkup: Keyboards.cancel);
                        return;
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Введенный ID новости не является числом.", replyMarkup: Keyboards.cancel);
                    return;
                }
                await botClient.SendTextMessageAsync(globalChatId, $"✅Новость успешно добавлена!", replyMarkup: Keyboards.addPicture);
                globalNewsId = idNews;
                await messageEveryone($"📰Добавлена новая новость под номером {idNews}\\!");
                pressingButtons["addingNews"] = false;
                return;
            }
            #endregion
            #region Редактировать новости
            if (pressingButtons["changeNews"] && message.Text.Trim().ToLower() != "/cancel")
            {
                sqlConnection.Close();
                string newsTitle = "Название новости", newsText = "Текст новости ";
                try//делим сообщение по разделителям и проверяем получается ли правильно поделить
                {
                    newsTitle = message.Text.Trim().Split('▒')[1];
                    newsText = message.Text.Trim().Split('▒')[2];
                }
                catch
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Текст введен некорректно", replyMarkup: Keyboards.cancel);
                    return;
                }
                if (string.IsNullOrWhiteSpace(newsTitle) || string.IsNullOrWhiteSpace(newsText))//если поделились но пустые или специально пустые
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [4002]: Текст или название новости являются пустыми полями.", replyMarkup: Keyboards.cancel);
                    return;
                }
                if (int.TryParse(message.Text.Trim().Split('▒')[0], out int idNews))//проверяем можно ли преобразовать в int
                {
                    if (await dataAvailability(idNews, "News"))//есть ли новость
                    {
                        sqlConnection.Open();
                        SqlCommand insertCommand = new("UPDATE News SET data = @dataNews, user_id = @user_id, newsTitle = @newsTitle, news = @newsText WHERE newsNumber = @newsNumber", sqlConnection);
                        insertCommand.Parameters.AddWithValue("@newsNumber", idNews);
                        insertCommand.Parameters.AddWithValue("@dataNews", DateTime.Now.Date);
                        insertCommand.Parameters.AddWithValue("@user_id", globalUserId);
                        insertCommand.Parameters.AddWithValue("@newsTitle", newsTitle);
                        insertCommand.Parameters.AddWithValue("@newsText", newsText);
                        insertCommand.ExecuteNonQuery();
                        sqlConnection.Close();
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3003]: Введенный ID новости не существует, введите другой номер", replyMarkup: Keyboards.cancel);
                        return;
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Введенный ID новости не является числом.", replyMarkup: Keyboards.cancel);
                    return;
                }
                await botClient.SendTextMessageAsync(globalChatId, $"✅Новость отредактирована", replyMarkup: Keyboards.addPicture);
                pressingButtons["changeNews"] = false;
                return;
            }
            #endregion
            #region Удаление новости
            if (pressingButtons["deleteNews"] && message.Text.Trim().ToLower() != "/cancel")
            {
                sqlConnection.Close();
                int rowsAffected;
                if (int.TryParse(message.Text.Trim(), out int idNews))//проверяем можно ли сообщение преобразовать в int
                {
                    if (await dataAvailability(idNews, "News"))//есть ли вообще новость
                    {
                        sqlConnection.Open();//удаляем
                        SqlCommand command = new("DELETE FROM News WHERE newsNumber = @newsNumber", sqlConnection);
                        command.Parameters.AddWithValue("@newsNumber", idNews);
                        rowsAffected = command.ExecuteNonQuery();
                        sqlConnection.Close();
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3002]: Введенный ID новости не существует, введите другой номер", replyMarkup: Keyboards.cancel);
                        return;
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Введенный ID новости не является числом.", replyMarkup: Keyboards.cancel);
                    return;
                }
                await botClient.SendTextMessageAsync(globalChatId, $"🗑Новость успешно удалена!\n" +
                    $"📝Удалено записей: {rowsAffected}");
                pressingButtons["deleteNews"] = false;
                return;
            }
            #endregion
            #region Номер новости для добавления картинки
            if (pressingButtons["numberNewsPicture"] && message.Text.Trim().ToLower() != "/cancel")
            {
                sqlConnection.Close();
                if (int.TryParse(message.Text.Trim(), out int idNews))//проверяем можно ли сообщение преобразовать в int
                {
                    if (await dataAvailability(idNews, "News"))//есть ли вообще новость
                    {
                        globalNewsId = idNews;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3002]: Введенный ID новости не существует, введите другой номер", replyMarkup: Keyboards.cancel);
                        return;
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Введенный ID новости не является числом.", replyMarkup: Keyboards.cancel);
                    return;
                }
                await botClient.SendTextMessageAsync(globalChatId, $"📍Новость найдена!\n" +
                    $"📝Отправьте картинку для добавления", replyMarkup: Keyboards.cancel);
                pressingButtons["addPhotoNews"] = true;//переходим на добавление картинки и ожидаем отправки
                pressingButtons["numberNewsPicture"] = false;
                return;
            }
            #endregion
            #region Редактирование звонков
            if (pressingButtons["changeCalls"] && message.Text.Trim().ToLower() != "/cancel")
            {
                await clearingTables($"Calls");
                sqlConnection.Close();
                sqlConnection.Open();
                string[] timeMass;
                int idCulls = 0;
                string comment, Time;
                try//делим сообщение по разделителям и проверяем получается ли правильно поделить
                {
                    timeMass = message.Text.Trim().Split('▒');
                }
                catch
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Текст введен некорректно", replyMarkup: Keyboards.cancel);
                    return;
                }
                if (timeMass.Length == 0)
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [4002]", replyMarkup: Keyboards.cancel);
                    return;
                }
                for (int i = 0; i < timeMass.Length; i++)
                {
                    idCulls++;
                    try//делим сообщение по разделителям и проверяем получается ли правильно поделить, т.е проверяем есть ли комментарий или нет
                    {
                        Time = timeMass[i].Trim().Split('☰')[0].Trim();
                        comment = timeMass[i].Trim().Split('☰')[1].Trim();
                    }
                    catch
                    {
                        Time = timeMass[i].Trim();
                        comment = " ";
                    }
                    if (Regex.IsMatch(Time.Trim(), @"^([01]\d|2[0-3]):[0-5]\d\s-\s([01]\d|2[0-3]):[0-5]\d$"))//проверяем на соответствие формату времени: 00:00 - 00:00
                    {
                        Time = Time.Replace("-", "⌁");//меняем 00:00 - 00:00 на 00:00 ⌁ 00:00
                        switch (idCulls)//просто для красивого оформления
                        {
                            case 1:
                                Time = "⒈ " + Time + " ╕";
                                break;
                            case 2:
                                Time = "⒉ " + Time + " │";
                                break;
                            case 3:
                                Time = "⒊ " + Time + " │";
                                break;
                            case 4:
                                Time = "⒋ " + Time + " │";
                                break;
                            case 5:
                                Time = "⒌ " + Time + " │";
                                break;
                            case 6:
                                Time = "⒍ " + Time + " │";
                                break;
                            case 7:
                                Time = "⒎ " + Time + " │";
                                break;
                            case 8:
                                Time = "⒏ " + Time + " ╛";
                                break;
                            default:
                                await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1001]: Максимальное количество записей: 8", replyMarkup: Keyboards.cancel);
                                return;
                        }
                        if (i == timeMass.Length - 1 && idCulls != 8)//заменяем последнюю │ на ╛, если это конец строки и он не равен 8
                        {
                            Time = Time.Replace("│", "╛");
                        }
                        SqlCommand insertCommand = new("INSERT into Calls(time_interval, note) values (@time_interval, @note)", sqlConnection);//добавляем
                        insertCommand.Parameters.AddWithValue("@time_interval", Time);
                        insertCommand.Parameters.AddWithValue("@note", comment);
                        insertCommand.ExecuteNonQuery();
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1004]: Введенный текст не соответствует формату времени: 00:00 - 00:00\n" +
                            $"Более подробно Regex:\n ^([01]\\d|2[0-3]):[0-5]\\d\\s-\\s([01]\\d|2[0-3]):[0-5]\\d$", replyMarkup: Keyboards.cancel);
                        return;
                    }
                }
                sqlConnection.Close();
                await botClient.SendTextMessageAsync(globalChatId, $"✅Звонки успешно добавлены!");
                pressingButtons["changeCalls"] = false;
                return;
            }
            #endregion
            #region Изменить расписание
            if (pressingButtons["changeSchedule"] && message.Text.Trim().ToLower() != "/cancel")
            {
                if (message.Text.Trim().ToUpper() == "ВСЕ" || message.Text.Trim().ToUpper() == "ВСЁ")//проверяем что ввел пользователь, т.е закончить или нет
                {
                    pressingButtons["changeSchedule"] = false;
                    await botClient.SendTextMessageAsync(globalChatId, $"✅Редактирование завершено!");
                    return;
                }
                sqlConnection.Close();
                string[] scheduleMass;
                int idSchedule = 0;
                string schedule, audience, EnglishDay, lesson_name1 = "", lesson_name2 = "", audience1 = "", audience2 = "";
                try//делим сообщение по разделителям и проверяем получается ли правильно поделить
                {
                    scheduleMass = message.Text.Trim().Split('▒');
                    if (scheduleMass.Length == 0)
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [4002]", replyMarkup: Keyboards.cancel);
                        return;
                    }
                    else if (!weekDays.TryGetValue(scheduleMass[0].Trim().ToLower(), out EnglishDay))//проверяем является ли слово днем недели и если да, записываем в переменную анг версию 
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]: Введенное 1-ое слово НЕ является днем недели", replyMarkup: Keyboards.cancel);
                        return;
                    }
                    else if (string.IsNullOrWhiteSpace(scheduleMass[1].Trim()) || scheduleMass.Length <= 1)//проверяем что после дня недели сообщение не пустое и что массив не слишком маленький
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [4002]: Введенное расписание является пустым или слишком коротким", replyMarkup: Keyboards.cancel);
                        return;
                    }
                }
                catch
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Текст введен некорректно", replyMarkup: Keyboards.cancel);
                    return;
                }
                await clearingTables($"{EnglishDay}_Homework");
                await clearingTables($"{EnglishDay}_Schedule");
                sqlConnection.Close();
                sqlConnection.Open();
                for (int i = 1; i < scheduleMass.Length; i++)
                {
                    idSchedule++;
                    try
                    {
                        schedule = scheduleMass[i].Trim().Split('☰')[0].Trim();
                        audience = scheduleMass[i].Trim().Split('☰')[1].Trim();
                        if (string.IsNullOrWhiteSpace(audience.Trim()))//проверяем что все номера аудиторий добавлены
                        {
                            await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [4002]: Одно из введенных номеров аудиторий является пустым", replyMarkup: Keyboards.cancel);
                            return;
                        }
                        try
                        {
                            lesson_name1 = schedule.Trim().Split('≣')[0].Trim();
                            lesson_name2 = schedule.Trim().Split('≣')[1].Trim();
                            if (string.IsNullOrWhiteSpace(lesson_name1) || string.IsNullOrWhiteSpace(lesson_name2))//если поделились но пустые или специально пустые
                            {
                                await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [4002]+[3001]: Одно из введенных названий уроков через `≣` является пустым", replyMarkup: Keyboards.cancel, parseMode: ParseMode.Markdown);
                                return;
                            }
                            if (schedule.Trim().Split('≣').Length >= 3)
                            {
                                await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Введено больше 2 уроков за одно время", replyMarkup: Keyboards.cancel, parseMode: ParseMode.MarkdownV2);
                                return;
                            }
                        }
                        catch { }
                        try
                        {
                            audience1 = audience.Trim().Split(' ')[0].Trim();
                            audience2 = audience.Trim().Split(' ')[1].Trim();
                            if (audience.Trim().Split(' ').Length >= 3)
                            {
                                await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Введено больше 2 номеров аудиторий _через пробел_ для двух пар\\.\n" +
                                    $"_Если так и задумывалось, введите их через другой символ *\\(пример: &\\)*_", replyMarkup: Keyboards.cancel, parseMode: ParseMode.MarkdownV2);
                                return;
                            }
                        }
                        catch { }
                    }
                    catch
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Текст введен некорректно", replyMarkup: Keyboards.cancel);
                        return;
                    }

                    switch (idSchedule)//просто для красивого оформления
                    {
                        case 1:
                            schedule = "⒈ " + schedule;
                            lesson_name1 = "⒈ " + lesson_name1;
                            lesson_name2 = "⒈ " + lesson_name2;
                            break;
                        case 2:
                            schedule = "⒉ " + schedule;
                            lesson_name1 = "⒉ " + lesson_name1;
                            lesson_name2 = "⒉ " + lesson_name2;
                            break;
                        case 3:
                            schedule = "⒊ " + schedule;
                            lesson_name1 = "⒊ " + lesson_name1;
                            lesson_name2 = "⒊ " + lesson_name2;
                            break;
                        case 4:
                            schedule = "⒋ " + schedule;
                            lesson_name1 = "⒋ " + lesson_name1;
                            lesson_name2 = "⒋ " + lesson_name2;
                            break;
                        case 5:
                            schedule = "⒌ " + schedule;
                            lesson_name1 = "⒌ " + lesson_name1;
                            lesson_name2 = "⒌ " + lesson_name2;
                            break;
                        case 6:
                            schedule = "⒍ " + schedule;
                            lesson_name1 = "⒍ " + lesson_name1;
                            lesson_name2 = "⒍ " + lesson_name2;
                            break;
                        case 7:
                            schedule = "⒎ " + schedule;
                            lesson_name1 = "⒎ " + lesson_name1;
                            lesson_name2 = "⒎ " + lesson_name2;
                            break;
                        case 8:
                            schedule = "⒏ " + schedule;
                            lesson_name1 = "⒏ " + lesson_name1;
                            lesson_name2 = "⒏ " + lesson_name2;
                            break;
                        default:
                            await botClient.SendTextMessageAsync(globalChatId, $"❌❌Ошибка [1001]: Максимальное количество записей: 8", replyMarkup: Keyboards.cancel);
                            return;
                    }

                    SqlCommand insertCommand = new($"INSERT into {EnglishDay}_Schedule(lesson_name, audience_code) values (@lesson_name, @audience_code)", sqlConnection);
                    if (audience1.Trim().Length < 2 || audience2.Trim().Length < 2)
                    {
                        insertCommand.Parameters.AddWithValue("@audience_code", audience);
                    }
                    else
                    {
                        insertCommand.Parameters.AddWithValue("@audience_code", $"{audience1}≣{audience2}");
                    }

                    if (lesson_name1.Trim().Length <= 3 || lesson_name2.Trim().Length <= 3)
                    {
                        insertCommand.Parameters.AddWithValue("@lesson_name", schedule);
                    }
                    else
                    {
                        insertCommand.Parameters.AddWithValue("@lesson_name", $"{lesson_name1}≣{lesson_name2}");
                    }
                    insertCommand.ExecuteNonQuery();

                    lesson_name1 = "";
                    lesson_name2 = "";
                    audience1 = "";
                    audience2 = "";
                }
                sqlConnection.Close();
                await botClient.SendTextMessageAsync(globalChatId, $"✅Расписание успешно добавлено!\n" +
                    $"📝Можете редактировать дальше, для завершения введите \"ВСЕ\" или введите /cancel");
                return;
            }
            #endregion
            #region Удаление расписания
            if (pressingButtons["deleteSchedule"] && message.Text.Trim().ToLower() != "/cancel")
            {
                sqlConnection.Close();
                string EnglishDay;
                int rowsAffected = 0;
                sqlConnection.Open();
                if (message.Text.Trim().ToUpper() == "ВСЕ" || message.Text.Trim().ToUpper() == "ВСЁ")//проверяем что ввел пользователь, т.е удалить все или по выбранному
                {
                    foreach (string RussiansDay in weekDays.Keys)//если ВСЕ
                    {
                        if (weekDays.TryGetValue(RussiansDay, out EnglishDay))
                        {
                            await clearingTables($"{EnglishDay}_Homework");
                            await clearingTables($"{EnglishDay}_Schedule");
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]: Невозможно получить английское название дня недели для русского дня {RussiansDay}", replyMarkup: Keyboards.cancel);
                            return;//маловероятная возможная ошибка
                        }
                    }
                }
                else//по выбранному
                {
                    if (!weekDays.TryGetValue(message.Text.Trim().ToLower(), out EnglishDay))
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]: Введенное слово НЕ является днем недели", replyMarkup: Keyboards.cancel);
                        return;
                    }

                    rowsAffected += await clearingTables($"{EnglishDay}_Homework");
                    rowsAffected += await clearingTables($"{EnglishDay}_Schedule");
                }
                sqlConnection.Close();
                await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                    text: $"✅Произведено удаление *Расписания*, удалено записей: _{rowsAffected}_",
                                                    parseMode: ParseMode.MarkdownV2);
                pressingButtons["deleteSchedule"] = false;
                return;
            }
            #endregion
            #region Добавить дз
            if (pressingButtons["addHomework"] && message.Text.Trim().ToLower() != "/cancel")
            {
                if (message.Text.Trim().ToUpper() == "ВСЕ" || message.Text.Trim().ToUpper() == "ВСЁ")//проверяем что ввел пользователь, т.е закончить или нет
                {
                    pressingButtons["addHomework"] = false;
                    await botClient.SendTextMessageAsync(globalChatId, $"✅Редактирование завершено!");
                    return;
                }
                sqlConnection.Close();
                string id, Text;
                if (await ReturnDayWeek(true))
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]: Невозможно получить английское название дня недели для русского дня", replyMarkup: Keyboards.cancel);
                    return;
                }
                try//делим сообщение по разделителям и проверяем получается ли правильно поделить
                {
                    var mes = message.Text.Trim().Split('☰');

                    id = mes[0].Trim();
                    Text = mes[1].Trim();

                    if (message.Text.Trim().Split('☰').Length > 2)//проверка размера массива записи дз по разделителю
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Текст введен некорректно", replyMarkup: Keyboards.cancel);
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(Text))//если поделились но пустые или специально пустые
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [4002]: Текст или id дз являются пустыми полями", replyMarkup: Keyboards.cancel);
                        return;
                    }
                    if (Text.Trim() == "^")
                    {
                        Text = "↑…↑";
                    }
                }
                catch
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Текст введен некорректно", replyMarkup: Keyboards.cancel);
                    return;
                }
                if (int.TryParse(id, out int idLesson))//проверяем можно ли сообщение преобразовать в int
                {
                    if (await dataAvailability(idLesson, "Homework", EnglishDayThen))
                    {
                        bool ft = false;
                        sqlConnection.Close();
                        sqlConnection.Open();
                        SqlCommand selectCommand;
                        selectCommand = new SqlCommand($"select homework from {EnglishDayThen}_Homework WHERE id_lesson = @id_lesson", sqlConnection);
                        selectCommand.Parameters.AddWithValue("@id_lesson", idLesson);
                        using (var reader = selectCommand.ExecuteReader())//выполняем проверку и результат записываем в result
                        {
                            if (reader.Read())
                            {
                                try
                                {
                                    string text = reader.GetString(0);
                                    reader.Close();
                                }
                                catch
                                {
                                    ft = true;
                                }
                            }
                        }
                        if (ft)//если пустое поле
                        {
                            SqlCommand updateCommand = new($"UPDATE {EnglishDayThen}_Homework SET homework = @homework WHERE id_lesson = @id_lesson", sqlConnection);
                            updateCommand.Parameters.AddWithValue("@id_lesson", idLesson);
                            updateCommand.Parameters.AddWithValue("@homework", Text);
                            updateCommand.ExecuteNonQuery();
                            sqlConnection.Close();
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3003]: Введенный номер урока уже заполнен дз, для редактирования перейдите к кнопке \"✏️Редактировать ДЗ\"", replyMarkup: Keyboards.cancel);
                            return;
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3002]: Номер урока не найден", replyMarkup: Keyboards.cancel);
                        return;
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Введенный ID дз не является числом.", replyMarkup: Keyboards.cancel);
                    return;
                }
                await botClient.SendTextMessageAsync(globalChatId, $"✅ДЗ успешно добавлено!\n" +
                    $"📝Можете редактировать дальше, для завершения введите \"ВСЕ\" или введите /cancel");
                if (globalDzInfo == 0)//проверка, можно ли отредактировать сообщение
                {
                    string text = await DzInfo();
                    var messageRes = await botClient.SendTextMessageAsync(chatId: globalChatId, text: text.Split('☰')[0].Trim(), parseMode: ParseMode.MarkdownV2);
                    globalDzInfo = messageRes.MessageId;
                }
                else
                {
                    string text = await DzInfo();
                    await botClient.EditMessageTextAsync(globalChatId, globalDzInfo, text: text.Split('☰')[0].Trim(), parseMode: ParseMode.MarkdownV2); //Обновляю сообщение
                }
                await DzChat(true);
                //pressingButtons["addHomework"] = false;
                return;
            }
            #endregion
            #region Редактировать дз
            if (pressingButtons["changeHomework"] && message.Text.Trim().ToLower() != "/cancel")
            {
                if (message.Text.Trim().ToUpper() == "ВСЕ" || message.Text.Trim().ToUpper() == "ВСЁ")//проверяем что ввел пользователь, т.е закончить или нет
                {
                    pressingButtons["changeHomework"] = false;
                    await botClient.SendTextMessageAsync(globalChatId, $"✅Редактирование завершено!");
                    return;
                }
                string id, Text;
                if (await ReturnDayWeek(true))
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]: Невозможно получить английское название дня недели для русского дня", replyMarkup: Keyboards.cancel);
                    return;
                }
                try//делим сообщение по разделителям и проверяем получается ли правильно поделить
                {
                    id = message.Text.Trim().Split('☰')[0].Trim();
                    Text = message.Text.Trim().Split('☰')[1].Trim();
                    if (message.Text.Trim().Split('☰').Length > 2)//проверка размера массива записи дз по разделителю
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Текст введен некорректно", replyMarkup: Keyboards.cancel);
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(id))//если поделились но пустые или специально пустые
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [4002]: Id дз является пустым полем", replyMarkup: Keyboards.cancel);
                        return;
                    }
                    if (Text.Trim() == "^")
                    {
                        Text = "↑…↑";
                    }
                }
                catch
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Текст введен некорректно", replyMarkup: Keyboards.cancel);
                    return;
                }
                if (int.TryParse(id, out int idLesson))//проверяем можно ли сообщение преобразовать в int
                {
                    if (await dataAvailability(idLesson, "Homework", EnglishDayThen))
                    {
                        sqlConnection.Open();
                        SqlCommand updateCommand = new($"UPDATE {EnglishDayThen}_Homework SET homework = @homework WHERE id_lesson = @id_lesson", sqlConnection);
                        updateCommand.Parameters.AddWithValue("@id_lesson", idLesson);
                        updateCommand.Parameters.AddWithValue("@homework", Text);
                        updateCommand.ExecuteNonQuery();
                        sqlConnection.Close();
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3002]: Номер урока не найден", replyMarkup: Keyboards.cancel);
                        return;
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Введенный ID дз не является числом", replyMarkup: Keyboards.cancel);
                    return;
                }

                await botClient.SendTextMessageAsync(globalChatId, $"✅ДЗ успешно изменено!\n" +
                    $"📝Можете редактировать дальше, для завершения введите \"ВСЕ\" или введите /cancel");
                if (globalDzTextInfo == 0)//проверка, можно ли отредактировать сообщение
                {
                    var messageRes = await botClient.SendTextMessageAsync(chatId: globalChatId, text: await DzTextInfo(), parseMode: ParseMode.MarkdownV2);
                    globalDzTextInfo = messageRes.MessageId;
                }
                else
                {
                    try
                    {
                        await botClient.EditMessageTextAsync(globalChatId, globalDzTextInfo, text: await DzTextInfo(), parseMode: ParseMode.MarkdownV2); //Обновляю сообщение
                    }
                    catch { }
                }
                //pressingButtons["changeHomework"] = false;
                await DzChat(true);
                return;
            }
            #endregion
            #region Удалить дз
            if (pressingButtons["deleteHomework"] && message.Text.Trim().ToLower() != "/cancel")
            {
                sqlConnection.Close();
                int rowsAffected = 0;
                if (await ReturnDayWeek(true))
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]: Невозможно получить английское название дня недели для русского дня", replyMarkup: Keyboards.cancel);
                    return;
                }

                sqlConnection.Open();
                if (message.Text.Trim().ToUpper() == "ВСЕ" || message.Text.Trim().ToUpper() == "ВСЁ")//проверяем что ввел пользователь, т.е удалить все или по выбранному
                {
                    rowsAffected = await clearingTables($"{EnglishDayThen}_Homework");
                }
                else//по выбранному
                {
                    if (int.TryParse(message.Text.Trim(), out int idLesson))//проверяем можно ли сообщение преобразовать в int
                    {
                        using SqlCommand command = new($"UPDATE {EnglishDayThen}_Homework SET homework = NULL WHERE id_lesson = @id_lesson", sqlConnection);
                        command.Parameters.AddWithValue("@id_lesson", idLesson);
                        rowsAffected = command.ExecuteNonQuery();// выполняем SQL-запрос на очистку
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Введенное id НЕ является числом", replyMarkup: Keyboards.cancel);
                        return;
                    }
                }
                sqlConnection.Close();
                await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                    text: $"✅Произведено удаление *ДЗ*, удалено записей: _{rowsAffected}_",
                                                    parseMode: ParseMode.MarkdownV2);
                if (globalDzTextInfo == 0)//проверка, можно ли отредактировать сообщение
                {
                    var messageRes = await botClient.SendTextMessageAsync(chatId: globalChatId, text: await DzTextInfo(), parseMode: ParseMode.MarkdownV2);
                    globalDzTextInfo = messageRes.MessageId;
                }
                else
                {
                    try
                    {
                        await botClient.EditMessageTextAsync(globalChatId, globalDzTextInfo, text: await DzTextInfo(), parseMode: ParseMode.MarkdownV2); //Обновляю сообщение
                    }
                    catch { }
                }
                pressingButtons["deleteHomework"] = false;
                return;
            }
            #endregion
            #region Отправить сообщение всем
            if (pressingButtons["messageTextEveryone"] && message.Text.Trim().ToLower() != "/cancel")
            {
                string Text = message.Text.Trim();
                if (!string.IsNullOrWhiteSpace(Text))//если пустые или специально пустые
                {
                    await messageEveryone(await EscapeMarkdownV2(Text));
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [4002]: Текст является пустым полем", replyMarkup: Keyboards.cancel);
                    return;
                }
                pressingButtons["messageTextEveryone"] = false;
                return;
            }
            #endregion
            #region Заблокировать пользователя
            if (pressingButtons["lockUsers"] && message.Text.Trim().ToLower() != "/cancel")
            {
                if (long.TryParse(message.Text.Trim(), out long userId))//проверяем можно ли сообщение преобразовать в int
                {
                    await UpdateUserStatusAsync(userId, true);//заблокировать можно любого пользователя даже если его нет в базе данных, блокировка сама его туда добавит
                    await botClient.SendTextMessageAsync(globalChatId, $"❗️Советую удалить статус Админа у данного пользователя (если он таким являлся)❗️");
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId: globalChatId, text: "❌Ошибка [3001]: Введенный ID Пользователя не является числом", replyMarkup: Keyboards.cancel);
                    return;
                }

                pressingButtons["lockUsers"] = false;
                return;
            }
            #endregion
            #region Разблокировать пользователя
            if (pressingButtons["unlockUsers"] && message.Text.Trim().ToLower() != "/cancel")
            {
                if (long.TryParse(message.Text.Trim(), out long userId))//проверяем можно ли сообщение преобразовать в int
                {
                    if (await dataAvailability(userId, "Users"))//а разблокировать нужно естественно того, кто уже есть в бд
                    {
                        await UpdateUserStatusAsync(userId, false);
                        await botClient.SendTextMessageAsync(globalChatId, $"❗️Надеюсь данный человек оправдал себя❗️");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                         chatId: globalChatId,
                         text: "❌Ошибка [3002]: Введенного ID Пользователя не обнаружено, проверьте правильность введённых данных", replyMarkup: Keyboards.cancel);
                        return;
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                        chatId: globalChatId,
                        text: "❌Ошибка [3001]: Введенный ID Пользователя не является числом", replyMarkup: Keyboards.cancel);
                    return;
                }

                pressingButtons["unlockUsers"] = false;
                return;
            }
            #endregion
            #region Текст приветствия
            if (greetings.Contains(message.Text.Trim().ToLower()))//для приветствий, если пользователь поздоровается
            {
                await botClient.SendTextMessageAsync(message.Chat, "Привет-привет!!❤️‍🔥👋");
                return;
            }
            #endregion

            //отправляется тогда, кода не было обработано действие, т.е не было return
            await botClient.SendTextMessageAsync(
                chatId: globalChatId,
                text: "❌Ошибка [1100]: Незарегистрированный текст📜\n" /*+
                $"Было отправлено: {message.Text}", parseMode: ParseMode.Markdown*/);
        }

        async Task ButtonUpdate(string callbackData)//обработка действия кнопок под текстом
        {
            if (string.IsNullOrWhiteSpace(callbackData?.Trim())) return;
            if (callbackData == "cancel")//если "отменить" то во всем массиве кнопок делаем их false
            {
                await Cancel();
            }
            else
            {
                if (await buttonTest())//проверяем все ли действия выполнены т.е все ли кнопки в массиве false
                {
                    switch (callbackData)//остальные кнопки
                    {
                        case "updateGroup":
                            // Обрабатываем нажатие на кнопку "Изменить группу"
                            await botClient.SendTextMessageAsync(globalChatId, "👥Выберите новую группу:", replyMarkup: Keyboards.cancel);
                            pressingButtons["changeGroupFT"] = true;
                            break;
                        case "updateName":
                            // Обрабатываем нажатие на кнопку "Изменить имя"
                            await botClient.SendTextMessageAsync(globalChatId, "✏️Введите новое имя:", replyMarkup: Keyboards.cancel);
                            pressingButtons["changeNameFT"] = true;
                            break;
                        case "addPicture":
                            if (globalNewsId == 0)//если новость для редактирования картинки не нашли
                            {
                                await botClient.SendTextMessageAsync(globalChatId, "❌Ошибка [4002]: Ранее введенной новости не обнаружено.\n" +
                                    "📝Введите номер новости для добавления картинки:");
                                pressingButtons["numberNewsPicture"] = true;
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(globalChatId, $"📍Новость найдена!\n" +
                                  $"📝Отправьте картинку для добавления", replyMarkup: Keyboards.cancel);
                                pressingButtons["addPhotoNews"] = true;
                            }
                            break;
                        case "NextNews":
                            #region Кнопка след новость
                            await ListNewsNumber();//обновляем массив новостей
                            if (globalNewsNumber == 0)//если еще не листали, то 1 новость
                            {
                                globalNewsNumber = newsNumbers[0];
                                await NewsText(globalNewsNumber);
                                break;
                            }
                            if (newsNumbers.Contains(globalNewsNumber + 1))//в другом случае добавляем 1 (т.е след новость)
                            {
                                await NewsText(++globalNewsNumber);
                            }
                            else//для проверки что новость по номеру боле чем на 1
                            {
                                int index = newsNumbers.IndexOf(globalNewsNumber);

                                if (index + 1 < newsNumbers.Count && index >= 0)
                                    globalNewsNumber = newsNumbers[index + 1];
                                else  //если в итоге это была последняя, то идем по 
                                    globalNewsNumber = newsNumbers[0];

                                await NewsText(globalNewsNumber);
                            }
                            #endregion
                            break;
                        case "PreviousNews":
                            #region Кнопка пред новость
                            await ListNewsNumber();//обновляем массив новостей
                            if (globalNewsNumber == 0)//если еще не листали, то 1 новость
                            {
                                globalNewsNumber = newsNumbers[0];
                                await NewsText(globalNewsNumber);
                                break;
                            }
                            if (newsNumbers.Contains(globalNewsNumber - 1))//в другом случае уменьшаем на 1 (т.е пред новость)
                            {
                                await NewsText(--globalNewsNumber);
                            }
                            else
                            {
                                int index = newsNumbers.IndexOf(globalNewsNumber);
                                if (index - 1 < newsNumbers.Count && index - 1 >= 0)
                                {
                                    globalNewsNumber = newsNumbers[index - 1];
                                }
                                else//если в итоге это была последняя, то идем по кругу
                                {
                                    globalNewsNumber = newsNumbers[newsNumbers.Count - 1];
                                }
                                await NewsText(globalNewsNumber);
                            }
                            #endregion
                            break;
                        case "shutdown":
                            await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                           text: "⏱Выберите *Время* для *Выключения*",
                                                           replyMarkup: Keyboards.shutdownBot, parseMode: ParseMode.MarkdownV2);
                            break;
                        case "cancelShutdown":
                            await botClient.SendTextMessageAsync(globalChatId, "🚫Отменено");
                            break;
                        case "shutdown5minuets":
                            await Cancel(false, false);
                            await messageEveryone("⚠️*Внимание*⚠️\n" +
                                "Через 5 минут бот перед в режим технических работ\\! \\[0000\\]\n" +
                                "_Т\\.е будет выключен на какое\\-то время_ \\[2001\\]");
                            pressingButtons["shutdownTimerMinuets"] = true;
                            // Создаем объект  таймера
                            await TimerBot(5);
                            break;
                        case "shutdown10minuets":
                            await Cancel(false, false);
                            await messageEveryone("⚠️*Внимание*⚠️\n" +
                                "Через 10 минут бот перед в режим технических работ\\! \\[0000\\]\n" +
                                "_Т\\.е будет выключен на какое\\-то время_ \\[2001\\]");
                            pressingButtons["shutdownTimerMinuets"] = true;
                            // Создаем объект  таймера
                            await TimerBot(10);
                            break;
                        case "tablePicture"://обработка таблиц и т.п для отображения картинок
#pragma warning disable CA1416 // Проверка совместимости платформы

                            int Width = Convert.ToInt32(globalTablePicture.Split(' ')[0]), Height = Convert.ToInt32(globalTablePicture.Split(' ')[1]);
                            string Table = globalTablePicture.Split(' ')[2], Text = "";//для начала принимаем размеры изо и что мы будем рисовать

                            int desiredWidth = (Width * 100) + 2;//увеличиваем размеры
                            int desiredHeight = ((Height + 1) * 25) + 2;

                            int maxResolution = 4096;//проверяем что-бы размер изо не был больше максимального

                            if (desiredWidth > maxResolution || desiredHeight > maxResolution)
                            {
                                await botClient.SendTextMessageAsync(globalChatId, "🚫ОШИБКА [1200]+[1001]: Таблица слишком длинная для ее размещения на изображении");
                            }
                            else
                            {

                                // Создание изображения таблицы
                                Bitmap image = new(desiredWidth, desiredHeight); // width и height изо
                                Graphics graphics = Graphics.FromImage(image);
                                graphics.Clear(System.Drawing.Color.FromArgb(30, 30, 30));

                                // Задаём стандартные параметры таблицы
                                int columns = 3; // Количество столбцов
                                int cellWidth = 100; // Ширина ячейки
                                int cellHeight = 25; // Высота ячейки

                                // Задаём стандартные параметры шрифта и кисти
                                Font font = new("Arial", 8);
                                SolidBrush brush = new(System.Drawing.Color.White);

                                Pen pen = new(System.Drawing.Color.FromArgb(70, 70, 70), 3);

                                // открытие соединения
                                sqlConnection.Open();
                                switch (Table)//что мы будем рисовать
                                {
                                    case "Users":
                                        Text = "👥*Информация о пользователях:*";
                                        // создание объекта команды SQL
                                        using (SqlCommand command = new("SELECT user_id, username, user_group FROM Users", sqlConnection))
                                        {
                                            // создание объекта для чтения данных
                                            using SqlDataReader reader = command.ExecuteReader();
                                            string username, userGroup, userId;
                                            int row = 0;//кол-во ячеек
                                                        // чтение данных из таблицы и добавление строк в таблицу
                                            while (reader.Read())
                                            {
                                                if (row == 0)// добавление заголовка таблицы
                                                {
                                                    userId = "ID";
                                                    username = "Имя";
                                                    userGroup = "Группа";
                                                    brush = new SolidBrush(System.Drawing.Color.Green);

                                                    for (int column = 0; column < columns; column++)
                                                    {
                                                        int x = column * cellWidth;
                                                        int y = row * cellHeight;

                                                        graphics.DrawRectangle(pen, x, y, cellWidth, cellHeight);// Нарисуем границу ячейки
                                                        switch (column)
                                                        {
                                                            case 0:
                                                                graphics.DrawString(userId, font, brush, x + 5, y + 5);// Запишем текст в ячейку                                                                                                          
                                                                break;
                                                            case 1:
                                                                graphics.DrawString(username, font, brush, x + 5, y + 5);
                                                                break;
                                                            case 2:
                                                                graphics.DrawString(userGroup, font, brush, x + 5, y + 5);
                                                                break;
                                                        }
                                                    }
                                                    row++;
                                                    brush = new SolidBrush(System.Drawing.Color.White);
                                                }
                                                //остальные данные 
                                                userId = reader.GetInt64(0).ToString();
                                                username = await CheckAndTruncateWordAsync(reader.GetString(1), 13);
                                                userGroup = await CheckAndTruncateWordAsync(reader.GetString(2), 14);

                                                for (int column = 0; column < columns; column++)//просто берем данные и записываем в ячейки
                                                {
                                                    int x = column * cellWidth;
                                                    int y = row * cellHeight;

                                                    graphics.DrawRectangle(pen, x, y, cellWidth, cellHeight);// Нарисуем границу ячейки
                                                    switch (column)
                                                    {
                                                        case 0:
                                                            graphics.DrawString(userId, font, brush, x + 5, y + 5);// Запишем текст в ячейку                                                                                                          
                                                            break;
                                                        case 1:
                                                            graphics.DrawString(username, font, brush, x + 5, y + 5);
                                                            break;
                                                        case 2:
                                                            graphics.DrawString(userGroup, font, brush, x + 5, y + 5);
                                                            break;
                                                    }
                                                }
                                                row++;
                                            }
                                        }
                                        break;
                                    case "Admins":
                                        Text = "👥*Информация о пользователях:*";
                                        // создание объекта команды SQL
                                        using (SqlCommand command = new("SELECT user_admin_id, user_id, username, admin_type FROM Admins", sqlConnection))
                                        {
                                            // создание объекта для чтения данных
                                            using SqlDataReader reader = command.ExecuteReader();
                                            string username, userAdminId, userId, adminType;
                                            int row = 0;//кол-во ячеек
                                            columns = 4;
                                            // чтение данных из таблицы и добавление строк в таблицу
                                            while (reader.Read())
                                            {
                                                if (row == 0)// добавление заголовка таблицы
                                                {
                                                    userAdminId = "ID Админа";
                                                    userId = "ID Юзера";
                                                    username = "Имя Юзера";
                                                    adminType = "Тип Админа";

                                                    brush = new SolidBrush(System.Drawing.Color.Green);
                                                    for (int column = 0; column < columns; column++)
                                                    {
                                                        int x = column * cellWidth;
                                                        int y = row * cellHeight;

                                                        graphics.DrawRectangle(pen, x, y, cellWidth, cellHeight);// Нарисуем границу ячейки
                                                        switch (column)
                                                        {
                                                            case 0:
                                                                graphics.DrawString(userAdminId, font, brush, x + 5, y + 5);// Запишем текст в ячейку                                                                                                          
                                                                break;
                                                            case 1:
                                                                graphics.DrawString(userId, font, brush, x + 5, y + 5);
                                                                break;
                                                            case 2:
                                                                graphics.DrawString(adminType, font, brush, x + 5, y + 5);
                                                                break;
                                                            case 3:
                                                                graphics.DrawString(username, font, brush, x + 5, y + 5);
                                                                break;
                                                        }
                                                    }
                                                    row++;
                                                    brush = new SolidBrush(System.Drawing.Color.White);
                                                }
                                                //остальные данные
                                                userAdminId = reader.GetInt32(0).ToString();
                                                userId = reader.GetInt64(1).ToString();
                                                username = await CheckAndTruncateWordAsync(reader.GetString(2), 13);
                                                adminType = reader.GetInt32(3).ToString();

                                                for (int column = 0; column < columns; column++)//просто берем данные и записываем в ячейки
                                                {
                                                    int x = column * cellWidth;
                                                    int y = row * cellHeight;

                                                    graphics.DrawRectangle(pen, x, y, cellWidth, cellHeight);// Нарисуем границу ячейки
                                                    switch (column)
                                                    {
                                                        case 0:
                                                            graphics.DrawString(userAdminId, font, brush, x + 5, y + 5);// Запишем текст в ячейку                                                                                                          
                                                            break;
                                                        case 1:
                                                            graphics.DrawString(userId, font, brush, x + 5, y + 5);
                                                            break;
                                                        case 2:
                                                            graphics.DrawString(adminType, font, brush, x + 5, y + 5);
                                                            break;
                                                        case 3:
                                                            graphics.DrawString(username, font, brush, x + 5, y + 5);
                                                            break;
                                                    }
                                                }
                                                row++;
                                            }
                                        }
                                        break;
                                    case "Schedule":
                                        sqlConnection.Close();
                                        desiredWidth = 1440;//тут мы задаем уже стандартные размеры изо в 2к
                                        desiredHeight = 2560;
                                        int lengthLine = 50;

                                        StringFormat stringFormatSchedule = new()
                                        {
                                            Alignment = StringAlignment.Near,
                                            LineAlignment = StringAlignment.Near,
                                            Trimming = StringTrimming.Word
                                        };

                                        Text = "✎Расписание ИС1-21";
                                        SizeF textSize = graphics.MeasureString(Text, font, image.Width, stringFormatSchedule);//задаем начальный размер

                                        image = new Bitmap(desiredWidth, desiredHeight); // обновляю width и height изо
                                        graphics = Graphics.FromImage(image);//создаю новый холст
                                        graphics.Clear(System.Drawing.Color.FromArgb(30, 30, 30));//цвет как в вижуал (типо серого)
                                        font = new Font("Arial", 30, FontStyle.Bold);//стиль текста

                                        SolidBrush brushClassNum = new(System.Drawing.Color.LightGreen);//задаём цвета для опред текста
                                        SolidBrush brushLessonName = new(System.Drawing.Color.White);

                                        graphics.DrawRectangle(pen, lengthLine, lengthLine, desiredWidth - lengthLine * 2, desiredHeight - lengthLine * 2);// Нарисуем границу
                                        graphics.DrawRectangle(pen, lengthLine / 2, lengthLine / 2, desiredWidth - lengthLine, desiredHeight - lengthLine);// Начальная обводка

                                        string EnglishDay, audience1 = "", audience2 = "", lesson_name1 = "", lesson_name2 = "";//само расписание
                                        int textWidth = 200;//отступ будет увеличиваться по мере размещения текста на картинке
                                        float defaultPosition = 150f;//стандартный отступ от левого края
                                        foreach (string RussiansDay in weekDays.Keys)
                                        {
                                            if (weekDays.TryGetValue(RussiansDay, out EnglishDay))
                                            {
                                                if (await dataAvailability(0, "Schedule ?", EnglishDay))
                                                {
                                                    // открытие соединения
                                                    sqlConnection.Open();
                                                    // создание команды для выполнения SQL-запроса для чтения данных 
                                                    SqlCommand selectCommand = new($"select * from {EnglishDay}_Schedule", sqlConnection);
                                                    textWidth += 50;//отступ между днями недели
                                                    Text = $"❐\"{RussiansDay.ToUpper()}\":\n";
                                                    brush = new SolidBrush(System.Drawing.Color.Aqua);
                                                    graphics.DrawString(Text, font, brush, defaultPosition, textWidth);//рисуем текст
                                                    textWidth += 70;//отступ после дня недели

                                                    if (textWidth > image.Height - 50)
                                                    {
                                                        await botClient.SendTextMessageAsync(globalChatId, $"*❌Ошибка [1001]:* К сожалению весь текст дз не помещается на картинке", parseMode: ParseMode.MarkdownV2);
                                                        // Освободите ресурсы
                                                        graphics.Dispose();
                                                        image.Dispose();
                                                        return;
                                                    }

                                                    // создание объекта SqlDataReader для чтения данных
                                                    using (SqlDataReader reader = selectCommand.ExecuteReader())
                                                    {
                                                        while (reader.Read())
                                                        {
                                                            Text = "";
                                                            if (textWidth > image.Height - 50)
                                                            {
                                                                await botClient.SendTextMessageAsync(globalChatId, $"*❌Ошибка [1001]:* К сожалению весь текст дз не помещается на картинке", parseMode: ParseMode.MarkdownV2);
                                                                // Освободите ресурсы
                                                                graphics.Dispose();
                                                                image.Dispose();
                                                                return;
                                                            }
                                                            // чтение значений из текущей строки таблицы
                                                            string lesson_name = reader.GetString(1);
                                                            string ClassNum = reader.GetString(2);
                                                            try//начинается проверки как правильно разместить расписание
                                                            {
                                                                lesson_name1 = lesson_name.Trim().Split('≣')[0].Trim();
                                                                lesson_name2 = lesson_name.Trim().Split('≣')[1].Trim();
                                                                try//изначально если все данные есть и все можно поделить
                                                                {
                                                                    audience1 = ClassNum.Trim().Split('≣')[0].Trim();
                                                                    audience2 = ClassNum.Trim().Split('≣')[1].Trim();

                                                                    //тут идет правильное размещение текста с нужными отступами
                                                                    Text = $"{lesson_name1} ┇";
                                                                    graphics.DrawString(Text, font, brushLessonName, defaultPosition, textWidth);
                                                                    textSize = graphics.MeasureString(Text, font, image.Width, stringFormatSchedule);
                                                                    defaultPosition += textSize.Width + 2;//т.е мы размещаем текст, после берем его размеры и относительно их расставляем другой текст
                                                                    Text = $"{audience1}";
                                                                    graphics.DrawString(Text, font, brushClassNum, defaultPosition, textWidth);
                                                                    textSize = graphics.MeasureString(Text, font, image.Width, stringFormatSchedule);
                                                                    defaultPosition += textSize.Width + 2;
                                                                    Text = $"//{await RemoveDigitsAsync(lesson_name2)} ┇";//RemoveDigitsAsync убирает лишние цифры в расписании
                                                                    graphics.DrawString(Text, font, brushLessonName, defaultPosition, textWidth);
                                                                    textSize = graphics.MeasureString(Text, font, image.Width, stringFormatSchedule);
                                                                    defaultPosition += textSize.Width + 2;
                                                                    graphics.DrawString($"{audience2}", font, brushClassNum, defaultPosition, textWidth);
                                                                    textWidth += 70;
                                                                    defaultPosition = 150f;
                                                                }
                                                                catch//и так везде только по разному количеству в зависимости от данных
                                                                {
                                                                    Text = $"{lesson_name1}";
                                                                    graphics.DrawString(Text, font, brushLessonName, defaultPosition, textWidth);
                                                                    textSize = graphics.MeasureString(Text, font, image.Width, stringFormatSchedule);
                                                                    defaultPosition += textSize.Width + 2;
                                                                    Text = $"//{await RemoveDigitsAsync(lesson_name2)} ┇";//RemoveDigitsAsync убирает лишние цифры в расписании
                                                                    graphics.DrawString(Text, font, brushLessonName, defaultPosition, textWidth);
                                                                    textSize = graphics.MeasureString(Text, font, image.Width, stringFormatSchedule);
                                                                    defaultPosition += textSize.Width + 2;
                                                                    graphics.DrawString($"{ClassNum}", font, brushClassNum, defaultPosition, textWidth);
                                                                    textWidth += 70;
                                                                    defaultPosition = 150f;
                                                                }
                                                            }
                                                            catch
                                                            {
                                                                try// формирование строки
                                                                {
                                                                    audience1 = ClassNum.Trim().Split('≣')[0].Trim();
                                                                    audience2 = ClassNum.Trim().Split('≣')[1].Trim();

                                                                    Text = $"{lesson_name} ┇";
                                                                    graphics.DrawString(Text, font, brushLessonName, defaultPosition, textWidth);
                                                                    textSize = graphics.MeasureString(Text, font, image.Width, stringFormatSchedule);
                                                                    graphics.DrawString($"{audience1}", font, brushClassNum, defaultPosition + textSize.Width + 2, textWidth);
                                                                    textWidth += 70;
                                                                }
                                                                catch
                                                                {
                                                                    Text = $"{lesson_name} ┇";
                                                                    graphics.DrawString(Text, font, brushLessonName, defaultPosition, textWidth);
                                                                    textSize = graphics.MeasureString(Text, font, image.Width, stringFormatSchedule);
                                                                    graphics.DrawString($"{ClassNum}", font, brushClassNum, defaultPosition + textSize.Width + 2, textWidth);
                                                                    textWidth += 70;
                                                                }
                                                            }
                                                        }
                                                    }
                                                    sqlConnection.Close();
                                                }
                                                else//если расписание отсутствует
                                                {
                                                    Text = $"❐Расписание для \"{RussiansDay.ToUpper()}\":";
                                                    textWidth += 70;
                                                    brush = new SolidBrush(System.Drawing.Color.Aqua);
                                                    graphics.DrawString(Text, font, brush, defaultPosition, textWidth);
                                                    Text = $"☒Отсутствует☒";
                                                    textWidth += 70;
                                                    brush = new SolidBrush(System.Drawing.Color.Red);
                                                    graphics.DrawString(Text, font, brush, defaultPosition, textWidth);
                                                }
                                            }
                                            else
                                            {
                                                await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]: Невозможно получить английское название дня недели для русского дня {RussiansDay}");
                                                // Освободите ресурсы
                                                graphics.Dispose();
                                                image.Dispose();
                                                return;//маловероятная возможная ошибка
                                            }
                                        }

                                        brush = new SolidBrush(System.Drawing.Color.Green);
                                        font = new Font("Arial", 46, FontStyle.Bold);
                                        Text = "✎Расписание ИС1-21";//последний текст который будет сверху
                                        graphics.DrawString(Text, font, brush, 130, 150);

                                        Text = "🗓*Расписание на всю неделю:*";//текст под картинкой

                                        break;
                                    case "Calls":
                                        desiredWidth = Width * 144;
                                        desiredHeight = (Height + 1) * 200;
                                        lengthLine = 50;
                                        image = new Bitmap(desiredWidth, desiredHeight); // обновляю width и height изо
                                        graphics = Graphics.FromImage(image);
                                        graphics.Clear(System.Drawing.Color.FromArgb(30, 30, 30));
                                        font = new Font("Arial", 60, FontStyle.Bold);

                                        graphics.DrawRectangle(pen, lengthLine, lengthLine, desiredWidth - lengthLine * 2, desiredHeight - lengthLine * 2);// Нарисуем границу
                                        graphics.DrawRectangle(pen, lengthLine / 2, lengthLine / 2, desiredWidth - lengthLine, desiredHeight - lengthLine);

                                        string Calls = "";
                                        textWidth = 90;//изначальный отступ
                                        SqlCommand commandCalls = new("SELECT time_interval FROM Calls", sqlConnection);
                                        SqlDataReader readerCalls = commandCalls.ExecuteReader();
                                        while (readerCalls.Read())//считываем звонки из базы
                                        {
                                            string time = readerCalls.GetString(readerCalls.GetOrdinal("time_interval"));
                                            Calls = $"{time}";//составляем сообщение
                                            textWidth += 120;
                                            brush = new SolidBrush(System.Drawing.Color.Aqua);
                                            graphics.DrawString(Calls, font, brush, 300, textWidth);
                                        }
                                        readerCalls.Close();

                                        Text = "🛎*Расписание звонков:*";//текст под картинкой
                                        brush = new SolidBrush(System.Drawing.Color.Green);
                                        font = new Font("Arial", 55, FontStyle.Bold);
                                        graphics.DrawString("☏Расписание звонков ИС1-21", font, brush, 150, 90);//последний текст который будет сверху
                                        break;
                                    case "Homework":
                                        string[] Culls = new string[8];
                                        if (await dataAvailability(0, "Calls ?"))//проверяем есть ли вообще звонки в базе
                                        {
                                            sqlConnection.Open();
                                            SqlCommand command = new("SELECT time_interval FROM Calls", sqlConnection);
                                            SqlDataReader reader = command.ExecuteReader();
                                            int count = 0;
                                            while (reader.Read())//считываем звонки из базы
                                            {
                                                try
                                                {
                                                    string time = reader.GetString(reader.GetOrdinal("time_interval"));
                                                    time = await RemoveDigitsAsync(time);
                                                    time = time.Remove(time.Length - 2) + ":";
                                                    Culls[count] = time;
                                                }
                                                catch { }
                                                count++;
                                            }
                                            reader.Close();
                                            sqlConnection.Close();
                                        }

                                        await WhatWeekType();
                                        sqlConnection.Close();
                                        float textWidthF = 195f;
                                        string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "backgroundDz.png");
                                        image = new Bitmap(imagePath); // обновляю изо
                                        graphics = Graphics.FromImage(image);
                                        font = new Font("Cascadia Mono", 20, FontStyle.Bold);
                                        Font fontHomework = new("Cascadia Mono", 20, FontStyle.Regular);
                                        Font fontCulls = new("Cascadia Mono", 14, FontStyle.Regular);
                                        Font fontText = new("Cascadia Mono", 26, FontStyle.Bold);
                                        brush = new SolidBrush(System.Drawing.Color.White);
                                        SolidBrush brushText = new(System.Drawing.Color.FromArgb(214, 155, 131));
                                        SolidBrush brushNumbers = new(System.Drawing.Color.FromArgb(181, 206, 168));
                                        SolidBrush brushLesson = new(System.Drawing.Color.FromArgb(216, 160, 222));
                                        Point day = new(670, 42);
                                        Point weekValue = new(483, 138);
                                        Point Date = new(370, 195);
                                        Point Dz = new(210, 245);
                                        StringFormat stringFormat = new()
                                        {
                                            Alignment = StringAlignment.Near,
                                            LineAlignment = StringAlignment.Near,
                                            Trimming = StringTrimming.Word
                                        };

                                        if (await ReturnDayWeek(true))
                                        {
                                            await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]", replyMarkup: Keyboards.cancel);                                         
                                            graphics.Dispose();// Освободите ресурсы
                                            image.Dispose();
                                            return;
                                        }

                                        if (await dataAvailability(0, "Homework ?", EnglishDayThen))//проверяю есть ли вообще дз в базе
                                        {
                                            Text = $"\"{RussianDayThen.ToUpper()}\"";
                                            graphics.DrawString(Text, fontText, brushText, day);
                                            textSize = graphics.MeasureString(Text, fontText, image.Width, stringFormat);
                                            graphics.DrawString(")", fontText, brush, 670 + textSize.Width, 42);

                                            Text = $"\"{weekType}\"";
                                            graphics.DrawString(Text, fontText, brushText, weekValue);
                                            textSize = graphics.MeasureString(Text, fontText, image.Width, stringFormat);
                                            graphics.DrawString(";", fontText, brush, 483 + textSize.Width, 138);

                                            Text = $"{DateDayThen}";
                                            graphics.DrawString(Text, fontText, brushNumbers, Date);
                                            textSize = graphics.MeasureString(Text, fontText, image.Width, stringFormat);
                                            graphics.DrawString(";", fontText, brush, 370 + textSize.Width, 195);

                                            // открытие соединения
                                            sqlConnection.Open();

                                            // создание команды для выполнения SQL-запроса с JOIN-ом таблиц
                                            string query2 = @$"SELECT s.Id_lesson, s.lesson_name, h.homework 
                                             FROM {EnglishDayThen}_Schedule s
                                             LEFT JOIN {EnglishDayThen}_Homework h ON s.Id_lesson = h.Id_lesson";
                                            SqlCommand command3 = new(query2, sqlConnection);

                                            // создание объекта SqlDataReader для чтения данных
                                            using (SqlDataReader reader = command3.ExecuteReader())
                                            {
                                                SizeF homeworkSize = graphics.MeasureString(Text, fontHomework, image.Width, stringFormat);
                                                SizeF lessonSize = graphics.MeasureString(Text, font, image.Width, stringFormat);
                                                SizeF lessonCulls = graphics.MeasureString(Text, fontCulls, image.Width, stringFormat);
                                                Text = "";
                                                int count = 0;
                                                while (reader.Read())
                                                {
                                                    string culls;
                                                    if (Culls[count] != null && !string.IsNullOrWhiteSpace(Culls[count]))
                                                    {
                                                        culls = Culls[count];
                                                    }
                                                    else
                                                    {
                                                        culls = "???";
                                                    }
                                                    string lesson_name, homework;
                                                    // чтение значений из текущей строки таблицы
                                                    lesson_name = (string)reader["lesson_name"];
                                                    try
                                                    {
                                                        homework = (string)reader["homework"] + ";";
                                                    }
                                                    catch//если поле пустое, то записываем ничего
                                                    {
                                                        homework = " ✘ ";
                                                    }

                                                    try
                                                    {
                                                        lesson_name1 = lesson_name.Trim().Split('≣')[0].Trim(); lesson_name2 = lesson_name.Trim().Split('≣')[1].Trim();
                                                        if (!string.IsNullOrWhiteSpace(lesson_name1) && !string.IsNullOrWhiteSpace(lesson_name2))//если поделились но пустые или специально пустые
                                                        {
                                                            if (weekType == "Числитель")
                                                            {
                                                                lesson_name = lesson_name1;
                                                            }
                                                            else
                                                            {
                                                                lesson_name = lesson_name2;
                                                            }
                                                        }
                                                    }
                                                    catch { }

                                                    textWidthF += homeworkSize.Height + 5;
                                                    // Определение размеров текста
                                                    homeworkSize = graphics.MeasureString(homework, fontHomework, image.Width, stringFormat);
                                                    lessonSize = graphics.MeasureString(lesson_name, font, image.Width, stringFormat);
                                                    lessonCulls = graphics.MeasureString(culls, fontCulls, image.Width, stringFormat);

                                                    if (textWidthF + lessonSize.Height + 5 + homeworkSize.Height > image.Height - 50)
                                                    {
                                                        await botClient.SendTextMessageAsync(globalChatId, $"*❌Ошибка [1001]:* К сожалению весь текст дз не помещается на картинке", parseMode: ParseMode.MarkdownV2);
                                                        // Освободите ресурсы
                                                        graphics.Dispose();
                                                        image.Dispose();
                                                        // return;
                                                    }

                                                    // Рисование текста с переносом
                                                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                                                    if (homeworkSize.Width >= image.Width - 200)
                                                    {
                                                        homeworkSize = graphics.MeasureString(await transferText(homework, 41), fontHomework, image.Width, stringFormat);

                                                        graphics.DrawString(lesson_name, font, brushLesson, 205, textWidthF);
                                                        graphics.DrawString(culls, fontCulls, brushNumbers, 205 + lessonSize.Width + 1, textWidthF + 5);
                                                        textWidthF += lessonSize.Height + 5;
                                                        graphics.DrawString(await transferText(homework, 41), fontHomework, brush, 205, textWidthF);
                                                    }
                                                    else
                                                    {
                                                        graphics.DrawString(lesson_name, font, brushLesson, 205, textWidthF /*+ 40*/);
                                                        graphics.DrawString(culls, fontCulls, brushNumbers, 205 + lessonSize.Width + 1, textWidthF + 5);
                                                        graphics.DrawString(" " + homework, fontHomework, brush, 205, textWidthF + 40);
                                                        textWidthF += 40;
                                                    }
                                                    count++;
                                                    // ТАК НАДО
                                                }
                                            }
                                            sqlConnection.Close();
                                            Text = "*🏠Дз на завтра: *";
                                        }
                                        else
                                        {
                                            await botClient.SendTextMessageAsync(globalChatId, $"❌*Извините, но дз для дня _\"{RussianDayThen.ToUpper()}\"_ еще не заполнено `[3002]`*", parseMode: ParseMode.MarkdownV2);
                                            // Освободите ресурсы
                                            graphics.Dispose();
                                            image.Dispose();
                                            return;
                                        }
                                        break;
                                    default://если нет данных то ошибка в виде изо
                                        lengthLine = 10;
                                        graphics.DrawRectangle(pen, lengthLine, lengthLine, desiredWidth - lengthLine * 2, desiredHeight - lengthLine * 2);// Нарисуем границу
                                        graphics.DrawRectangle(pen, lengthLine / 2, lengthLine / 2, desiredWidth - lengthLine, desiredHeight - lengthLine);

                                        Point point1 = new(lengthLine, lengthLine);
                                        Point point2 = new(desiredWidth - lengthLine, desiredHeight - lengthLine);
                                        graphics.DrawLine(pen, point1, point2);

                                        point1 = new Point(lengthLine, desiredHeight - lengthLine);
                                        point2 = new Point(desiredWidth - lengthLine, lengthLine);
                                        graphics.DrawLine(pen, point1, point2);

                                        Text = "🚫*Эта кнопка более недействительна*🚫";
                                        brush = new SolidBrush(System.Drawing.Color.Red);
                                        font = new Font("Arial", 20);
                                        graphics.DrawString("☒ОШИБКА [1300]☒", font, brush, 70, 70);
                                        break;
                                }
                                sqlConnection.Close();

                                // Сохранение изображения во временный файл
                                string tempFilePath = Path.GetTempFileName();
                                image.Save(tempFilePath, ImageFormat.Png);
                                // Создание InputFileStream из временного файла
                                InputFileStream inputFile = new(new FileStream(tempFilePath, FileMode.Open));

                                // Отправка фото пользователю в Telegram
                                await botClient.SendPhotoAsync(chatId: globalChatId, photo: inputFile,
                                                               caption: Text, parseMode: ParseMode.MarkdownV2);

                                inputFile.Content.Close();
                                // Удаление временного файла
                                System.IO.File.Delete(tempFilePath);
                                // Освободите ресурсы
                                graphics.Dispose();
                                image.Dispose();

                            }
                            break;
#pragma warning restore CA1416 // Проверка совместимости платформы
                        default:
                            // Обрабатываем неизвестные данные обратного вызова
                            await botClient.SendTextMessageAsync(globalChatId, "🚫Незарегистрированная кнопка [1300]");
                            break;
                    }
                }
                else
                {
                    //когда не все кнопки были выполнены
                    await botClient.SendTextMessageAsync(globalChatId, "🚫Для выполнения другого действия закончите работу с предыдущим\nИли введите /cancel");
                }
            }
        }

        async Task<bool> dataAvailability(long ID, string table, string value = "")//проверка разных действий из разных таблиц в основном содержится ли в таблице что то
        {
            bool result = false;
            sqlConnection.Close();
            await sqlConnection.OpenAsync();
            SqlCommand selectCommand;
            switch (table)//в зависимости от действия задаем в переменную selectCommand различные команды
            {
                case "Admins Name":
                    selectCommand = new SqlCommand("SELECT * FROM Admins WHERE username = @userName", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@userName", value);
                    break;
                case "Admins 1 Name":
                    selectCommand = new SqlCommand("SELECT * FROM Admins WHERE username = @userName AND admin_type = @adminType", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@userName", value);
                    selectCommand.Parameters.AddWithValue("@adminType", 1);
                    break;
                case "Admins":
                    selectCommand = new SqlCommand("SELECT * FROM Admins WHERE user_id = @userId", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@userId", ID);
                    break;
                case "Admins 1":
                case "Admins 2":
                case "Admins 3":
                    selectCommand = new SqlCommand("SELECT * FROM Admins WHERE user_id = @userId AND admin_type = @adminType", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@userId", ID);
                    selectCommand.Parameters.AddWithValue("@adminType", table.Split(' ')[1]);
                    break;
                case "Users":
                    selectCommand = new SqlCommand("select * from Users WHERE user_id = @userId", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@userId", ID);
                    break;
                case "Users Name":
                    selectCommand = new SqlCommand("select * from Users WHERE username = @userName", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@userName", value);
                    break;
                case "News ?":
                    selectCommand = new SqlCommand("select count(*) from News", sqlConnection);
                    break;
                case "News":
                    selectCommand = new SqlCommand("select * from News WHERE newsNumber = @newsNumber", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@newsNumber", ID);
                    break;
                case "Calls ?":
                    selectCommand = new SqlCommand("select count(*) from Calls", sqlConnection);
                    break;
                case "Homework ?":
                    selectCommand = new SqlCommand($"select count(*) from {value}_Homework", sqlConnection);
                    break;
                case "Homework":
                    selectCommand = new SqlCommand($"select * from {value}_Homework WHERE id_lesson = @id_lesson", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@id_lesson", ID);
                    break;
                case "Schedule ?":
                    selectCommand = new SqlCommand($"select count(*) from {value}_Schedule", sqlConnection);
                    break;
                default:
                    selectCommand = new SqlCommand($"SELECT * FROM {table} WHERE user_id = @userId", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@userId", ID);
                    break;
            }

            using (var reader = selectCommand.ExecuteReader())//выполняем проверку и результат записываем в result
            {
                if (reader.Read())
                {
                    long count = 0;
                    try
                    {
                        count = (int)reader.GetInt32(0);
                    }
                    catch
                    {
                        count = reader.GetInt64(0);
                    }
                    if (!reader.HasRows || count == 0)
                    {
                        result = false;
                    }
                    else
                    {
                        result = true;
                    }
                }
            }
            await sqlConnection.CloseAsync();
            //sqlConnection.Dispose();

            return result;
        }

        async Task<string> userProfile(long userID)//вывод или изменение информации о пользователе, т.е его профиль
        {
            sqlConnection.Close();
            SqlDataReader readerAdmin, readerUser;
            SqlCommand selectCommandUser, selectCommandAdmin;
            bool isAdmin = false;
            string messageInfo = "ОШИБКА [3002]";
            int adminType = 0;

            if (await dataAvailability(globalUserId, "Admins"))//является ли админом
            {
                isAdmin = true;
            }

            sqlConnection.Open();

            selectCommandUser = new SqlCommand("SELECT * FROM Users WHERE user_id = @userId", sqlConnection);//считываем данные о человеке
            selectCommandUser.Parameters.AddWithValue("@userId", globalUserId);
            readerUser = selectCommandUser.ExecuteReader();

            if (readerUser.Read())//является ли админом
            {
                string adminRang = "пустышка";
                long userId = readerUser.GetInt64(readerUser.GetOrdinal("user_id"));
                var userGroup = readerUser.GetString(readerUser.GetOrdinal("user_group"));
                globalUsername = readerUser.GetString(readerUser.GetOrdinal("username"));
                readerUser.Close();
                if (isAdmin)
                {
                    selectCommandAdmin = new SqlCommand("SELECT * FROM Admins WHERE user_id = @userId", sqlConnection);
                    selectCommandAdmin.Parameters.AddWithValue("@userId", globalUserId);

                    using (readerAdmin = selectCommandAdmin.ExecuteReader())
                    {
                        if (readerAdmin.Read())
                        {
                            adminType = readerAdmin.GetInt32(readerAdmin.GetOrdinal("admin_type"));
                        }
                    }

                    switch (adminType)//в зависимости от ранга записываем его класс
                    {
                        case 1:
                            adminRang = "Главный админ";
                            break;
                        case 2:
                            adminRang = "Помощник админа";
                            break;
                        default:
                            adminRang = "Редактор";
                            break;
                    }
                    messageInfo = $"*❒Информация о пользователе❒*" +
                        $"\n*➣Id:* {userId}," +
                        $"\n*➢Имя:* {await EscapeMarkdownV2(globalUsername)}," +
                        $"\n*➣Группа:* {await EscapeMarkdownV2(userGroup)}," +
                        $"\n*➢Администратор:* _Является_ администратором," +
                        $"\n*➣Уровень администратора:* {adminType} \\- {adminRang}";

                }
                else//записываем сообщение в обоих случаях 
                {
                    messageInfo = $"*❒Информация о пользователе❒*" +
                        $"\n*➣Id:* {userId}," +
                        $"\n*➢Имя:* {await EscapeMarkdownV2(globalUsername)}," +
                        $"\n*➣Группа:* {await EscapeMarkdownV2(userGroup)}," +
                        $"\n*➢Администратор:* _Не является_ администратором";
                }
            }
            sqlConnection.Close();
            return messageInfo;//и возвращаем его
        }

        Task<bool> buttonTest()//проверяем все ли действия выполнены т.е все ли кнопки в массиве false
        {
            bool result = true;
            foreach (var kvp in pressingButtons)
            {
                if (kvp.Value) // проверяем значение текущего элемента
                {
                    result = false;
                    break;
                }
            }
            return Task.FromResult(result);
        }

        Task<string> EscapeMarkdownV2(string text)//для того, что-бы не было ошибок в преобразовании текста MarkdownV2
        {
            // Список зарезервированных символов в MarkdownV2
            string[] reservedChars = { "_", "*", "`", "[", "]", "(", ")", "~", ">", "#", "+", "-", "=", "|", "{", "}", ".", "!" };

            text = text.Replace("\\", " ");
            // Заменяем каждый зарезервированный символ в строке на его экранированный аналог с двумя обратными косыми чертами
            foreach (string reservedChar in reservedChars)
            {
                text = text.Replace(reservedChar, "\\" + reservedChar);
            }
            return Task.FromResult(text);
        }

        async Task<int> clearingTables(string Table)
        {
            sqlConnection.Close();
            await sqlConnection.OpenAsync();
            int rowsAffected;
            using (SqlCommand command = new($"DELETE FROM {Table}", sqlConnection))
            {
                rowsAffected = command.ExecuteNonQuery();// выполняем SQL-запрос на очистку
            }
            using (SqlCommand resetIdentityCommand = new($"DBCC CHECKIDENT ({Table}, RESEED, 0)", sqlConnection))
            {
                if (rowsAffected == 0)// выполняем SQL-запрос на сброс автоинкрементного поля
                {
                    rowsAffected = resetIdentityCommand.ExecuteNonQuery();
                }
                else
                {
                    resetIdentityCommand.ExecuteNonQuery();
                }
            }
            await sqlConnection.CloseAsync();
            return rowsAffected;
        }

        async Task<bool> photoVerification(PhotoSize photo)//проверяем соответствие фотографий указанным требованиям
        {
            bool result = false;
            // Получаем информацию о файле фото
            var fileInfo = await botClient.GetInfoAndDownloadFileAsync(photo.FileId, new MemoryStream(), default);
            // Проверяем, что размер файла не превышает 20 МБ
            if (fileInfo.FileSize > 20 * 1024 * 1024)
            {
                // Если размер превышает лимит, отправляем сообщение об ошибке
                await botClient.SendTextMessageAsync(globalChatId, "❌Ошибка [1001]: размер файла превышает 20 МБ");
            }
            else
            {
                // Проверяем формат файла
                string fileExt = Path.GetExtension(fileInfo.FilePath).ToLower();
                if (fileExt != ".jpg" && fileExt != ".jpeg" && fileExt != ".png")
                {
                    // Если формат не подходит, отправляем сообщение об ошибке
                    await botClient.SendTextMessageAsync(globalChatId, "❌Ошибка [1002]: неподдерживаемый формат файла");
                }
                else
                {
                    result = true;
                }
            }
            return result;
        }

        async Task ListNewsNumber()//для обновления массива с номерами новостей если новость была удалена или добавлена
        {
            sqlConnection.Close();
            sqlConnection.Open();
            // Создаем запрос на выборку данных из таблицы News
            SqlCommand command = new("SELECT newsNumber FROM News", sqlConnection);
            // Выполняем запрос на выборку данных из таблицы News
            SqlDataReader reader = await command.ExecuteReaderAsync();
            // Считываем данные
            newsNumbers.Clear();//чистим массив
            while (await reader.ReadAsync())
            {
                // Получаем значение newsNumber и добавляем его в список
                int newsNumber = reader.GetInt32(reader.GetOrdinal("newsNumber"));
                newsNumbers.Add(newsNumber);
            }
            reader.Close();
            sqlConnection.Close();
        }

        async Task NewsText(int idNews)//редактирование сообщения с новостью при нажатии на кнопку След или Пред новость
        {
            sqlConnection.Close();
            sqlConnection.Open();
            bool ImageIsNull = false;
            MemoryStream ms = new();
            string caption = "ОШИБКА [3002]";
            // Создаем запрос на выборку данных из таблиц News и Users, для получения имени пользователя
            SqlCommand command = new("SELECT n.*, u.username FROM News n JOIN Users u ON n.user_id = u.user_id WHERE n.newsNumber = @idNews", sqlConnection);
            command.Parameters.AddWithValue("@idNews", idNews);
            SqlDataReader reader = command.ExecuteReader();
            // Считываем данные
            if (reader.Read())
            {
                // Получаем данные
                try
                {
                    byte[] image = (byte[])reader["image"];//берем изображение
                    ms = new MemoryStream(image);
                }
                catch { ImageIsNull = true; }
                long userId = reader.GetInt64(reader.GetOrdinal("user_id"));//остальные данные
                DateTime data = reader.GetDateTime(reader.GetOrdinal("data"));
                string newsTitle = reader.GetString(reader.GetOrdinal("newsTitle"));
                string news = reader.GetString(reader.GetOrdinal("news"));
                string userName = reader.GetString(reader.GetOrdinal("username"));
                reader.Close();
                //создаем текст
                caption = $"⌞⌋𝕹𝕰𝖂𝕾 *№{idNews}*⌊⌟" +
                    $"*{await EscapeMarkdownV2(newsTitle)}*"
                  + $"{await EscapeMarkdownV2(news)}\n\n"
                  + $"║☱*Автор: {await EscapeMarkdownV2(userName)}*☱║\n"
                  + $"*—–\\-⟨{await EscapeMarkdownV2($"{data:yyyy-MM-dd}")}⟩\\-–—*";

                if (globalMessagePhotoId != 0)//если нельзя отредактировать сообщение
                {
                    if (!ImageIsNull)
                    {
                        var inputMediaPhoto = new InputMediaPhoto(InputFile.FromStream(stream: ms, fileName: "image"));//преобразуем из массива байт и создаем импорт медиа
                        // Заменяем фотографию в сообщении
                        var editedPhotoMessage = await botClient.EditMessageMediaAsync(globalChatId,
                            globalMessagePhotoId, inputMediaPhoto,
                            replyMarkup: Keyboards.newsButton);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                           chatId: globalChatId,
                           text: "❌Ошибка [4001]: Отсутствие изображения");
                        return;
                    }
                    // Заменяем текст в сообщении обязательно !EditMessageCaptionAsync!
                    var editedTextMessage = await botClient.EditMessageCaptionAsync(globalChatId,
                        globalMessagePhotoId, caption,
                        parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.newsButton);
                }
                else
                {
                    if (!ImageIsNull)
                    {
                        var messageRes = await botClient.SendPhotoAsync(
                          chatId: globalChatId,
                          photo: InputFile.FromStream(stream: ms, fileName: "image"),//тут не нужно создавать импорт медиа т.к мы не редактируем
                          caption: caption + "\n", // Добавляем символ перевода строки может и не нужен, хотел чтоб фотка была ниже текста но для этого нужно вставлять фотку как ссылку в текст
                          parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.newsButton);
                        globalMessagePhotoId = messageRes.MessageId;
                    }
                    else
                    {
                        var messageRes = await botClient.SendTextMessageAsync(
                          chatId: globalChatId,
                          text: caption + "\n", // Добавляем символ перевода строки может и не нужен, хотел чтоб фотка была ниже текста но для этого нужно вставлять фотку как ссылку в текст
                          parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.newsButton);
                        globalMessagePhotoId = messageRes.MessageId;
                    }
                }
                ms.Close();
            }
            sqlConnection.Close();
        }

        async Task Cancel(bool changeAllValues = false, bool messageEnabled = true)
        {
            foreach (string key in pressingButtons.Keys)
            {
                if (changeAllValues || !key.Contains("Timer"))
                {
                    pressingButtons[key] = false;
                }
            }
            if (messageEnabled)
            {
                await botClient.SendTextMessageAsync(globalChatId, "🚫Отменено");
            }
        }

        async Task<string> DzInfo()
        {
            if (await ReturnDayWeek(true))
            {
                await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]: Невозможно получить английское название дня недели для русского дня", replyMarkup: Keyboards.cancel);
                return "ОШИБКА";
            }

            if (!await dataAvailability(0, "Homework ?", EnglishDayThen))
            {
                await clearingTables($"{EnglishDayNow}_Homework");//просто удаляем всё дз уже прошедшего дня

                await CopyDataDz(EnglishDayThen);//копируем данные расписания из расписания в дз
            }

            await WhatWeekType();
            // открытие соединения
            sqlConnection.Open();

            // создание команды для выполнения SQL-запроса с JOIN-ом таблиц
            string query2 = @$"SELECT s.Id_lesson, s.lesson_name, 
                            CASE WHEN h.homework IS NULL THEN 'X' ELSE 'V' END AS homework_flag 
                            FROM {EnglishDayThen}_Schedule s
                            LEFT JOIN {EnglishDayThen}_Homework h ON s.Id_lesson = h.Id_lesson";
            SqlCommand command3 = new(query2, sqlConnection);
            string result = "📊Текущая статистика ДЗ:\n*ID┇Flag┇Расписание*\n";
            bool FTres = true;
            // создание объекта SqlDataReader для чтения данных
            using (SqlDataReader reader = command3.ExecuteReader())
            {
                // обход результатов выборки
                while (reader.Read())
                {
                    // чтение значений из текущей строки таблицы
                    int id_lesson = (int)reader["Id_lesson"];
                    string lesson_name = (string)reader["lesson_name"];
                    string homework_flag = (string)reader["homework_flag"];
                    if (homework_flag == "V")
                    {
                        homework_flag = "✅";
                    }
                    else
                    {
                        homework_flag = "❌";
                        FTres = false;
                    }

                    try
                    {
                        string lesson_name1 = lesson_name.Trim().Split('≣')[0].Trim(), lesson_name2 = lesson_name.Trim().Split('≣')[1].Trim();
                        if (!string.IsNullOrWhiteSpace(lesson_name1) && !string.IsNullOrWhiteSpace(lesson_name2))//если поделились но пустые или специально пустые
                        {
                            if (weekType == "Числитель")
                            {
                                lesson_name = lesson_name1;
                            }
                            else
                            {
                                lesson_name = lesson_name2;
                            }
                        }
                    }
                    catch { }
                    // формирование строки для переменной
                    result += $" `{id_lesson}` ┇ {homework_flag} ┇{await EscapeMarkdownV2(lesson_name)}\n";
                }
            }
            sqlConnection.Close();

            result += $"☰ {FTres}";

            return result;
        }

        async Task<string> DzTextInfo()
        {
            if (await ReturnDayWeek(true))
            {
                await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]: Невозможно получить английское название дня недели для русского дня", replyMarkup: Keyboards.cancel);
                return "ОШИБКА";
            }

            if (!await dataAvailability(0, "Homework ?", EnglishDayThen))
            {
                await clearingTables($"{EnglishDayNow}_Homework");//просто удаляем всё дз уже прошедшего дня

                await CopyDataDz(EnglishDayThen);//копируем данные расписания из расписания в дз
            }

            await WhatWeekType();
            // открытие соединения
            sqlConnection.Open();

            // создание команды для выполнения SQL-запроса с JOIN-ом таблиц
            string query2 = @$"SELECT s.Id_lesson, s.lesson_name, h.homework 
                            FROM {EnglishDayThen}_Schedule s
                            LEFT JOIN {EnglishDayThen}_Homework h ON s.Id_lesson = h.Id_lesson";
            SqlCommand command3 = new(query2, sqlConnection);
            string result = "📊Текущее ДЗ:\n*ID┇Расписание*\n";
            // создание объекта SqlDataReader для чтения данных
            using (SqlDataReader reader = command3.ExecuteReader())
            {
                // обход результатов выборки
                while (reader.Read())
                {
                    int id_lesson;
                    string lesson_name, homework;
                    // чтение значений из текущей строки таблицы
                    id_lesson = (int)reader["Id_lesson"];
                    lesson_name = (string)reader["lesson_name"];
                    try
                    {
                        homework = (string)reader["homework"];
                    }
                    catch//если поле пустое, то записываем ничего
                    {
                        homework = " ❌ ";
                    }
                    try
                    {
                        string lesson_name1 = lesson_name.Trim().Split('≣')[0].Trim(), lesson_name2 = lesson_name.Trim().Split('≣')[1].Trim();
                        if (!string.IsNullOrWhiteSpace(lesson_name1) && !string.IsNullOrWhiteSpace(lesson_name2))//если поделились но пустые или специально пустые
                        {
                            if (weekType == "Числитель")
                            {
                                lesson_name = lesson_name1;
                            }
                            else
                            {
                                lesson_name = lesson_name2;
                            }
                        }
                    }
                    catch { }
                    // формирование строки для переменной
                    result += $" `{id_lesson}` ┇{await EscapeMarkdownV2(lesson_name)}: `{await EscapeMarkdownV2(homework)}`\n";
                }
            }
            sqlConnection.Close();
            return result;
        }

        async Task<string> DzChat(bool EditMessage = false)
        {
            string[] Culls = new string[8];
            if (await dataAvailability(0, "Calls ?"))//проверяем есть ли вообще звонки в базе
            {
                sqlConnection.Open();
                SqlCommand command = new("SELECT time_interval FROM Calls", sqlConnection);
                SqlDataReader reader = command.ExecuteReader();
                int count = 0;
                while (reader.Read())//считываем звонки из базы
                {
                    try
                    {
                        string time = reader.GetString(reader.GetOrdinal("time_interval"));
                        time = await RemoveDigitsAsync(time);
                        time = time.Remove(time.Length - 2) + ":";
                        Culls[count] = time;
                    }
                    catch { }
                    count++;
                }
                reader.Close();
                sqlConnection.Close();
            }

            await WhatWeekType();
#pragma warning disable CA1416 // Проверка совместимости платформы
            sqlConnection.Close();
            bool MaxLengthImage = false;
            SizeF textSize;
            float textWidthF = 195f;
            string lesson_name1 = "", lesson_name2 = "", TextPhoto = "", TextMessage = "", Final = "0 ☰ false";
            string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "backgroundDz.png");
            Bitmap image = new(imagePath); // обновляю изо
            Graphics graphics = Graphics.FromImage(image);
            Font font = new("Cascadia Mono", 20, FontStyle.Bold);
            Font fontHomework = new("Cascadia Mono", 20, FontStyle.Regular);
            Font fontCulls = new("Cascadia Mono", 14, FontStyle.Regular);
            Font fontText = new("Cascadia Mono", 26, FontStyle.Bold);
            SolidBrush brush = new(System.Drawing.Color.White);
            SolidBrush brushText = new(System.Drawing.Color.FromArgb(214, 155, 131));
            SolidBrush brushNumbers = new(System.Drawing.Color.FromArgb(181, 206, 168));
            SolidBrush brushLesson = new(System.Drawing.Color.FromArgb(216, 160, 222));
            Point day = new(670, 42);
            Point weekValue = new(483, 138);
            Point Date = new(370, 195);
            Point Dz = new(210, 245);
            StringFormat stringFormat = new()
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Near,
                Trimming = StringTrimming.Word
            };

            if (await ReturnDayWeek(true))
            {
                await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка при отправки дз [1003]", replyMarkup: Keyboards.cancel);              
                graphics.Dispose();// Освободите ресурсы
                image.Dispose();
                return "ОШИБКА";
            }
            

            if (await dataAvailability(0, "Homework ?", EnglishDayThen))//проверяю есть ли вообще дз в базе
            {
                TextPhoto = $"\"{RussianDayThen.ToUpper()}\"";
                graphics.DrawString(TextPhoto, fontText, brushText, day);
                textSize = graphics.MeasureString(TextPhoto, fontText, image.Width, stringFormat);
                graphics.DrawString(")", fontText, brush, 670 + textSize.Width, 42);

                TextPhoto = $"\"{weekType}\"";
                graphics.DrawString(TextPhoto, fontText, brushText, weekValue);
                textSize = graphics.MeasureString(TextPhoto, fontText, image.Width, stringFormat);
                graphics.DrawString(";", fontText, brush, 483 + textSize.Width, 138);

                TextPhoto = $"{DateDayThen}";
                graphics.DrawString(TextPhoto, fontText, brushNumbers, Date);
                textSize = graphics.MeasureString(TextPhoto, fontText, image.Width, stringFormat);
                graphics.DrawString(";", fontText, brush, 370 + textSize.Width, 195);

                // открытие соединения
                await sqlConnection.OpenAsync();

                // создание команды для выполнения SQL-запроса с JOIN-ом таблиц
                string query2 = @$"SELECT s.Id_lesson, s.lesson_name, h.homework 
                                             FROM {EnglishDayThen}_Schedule s
                                             LEFT JOIN {EnglishDayThen}_Homework h ON s.Id_lesson = h.Id_lesson";
                SqlCommand command3 = new(query2, sqlConnection);

                TextMessage = $"*⠀  —–⟨Дз на {await EscapeMarkdownV2(RussianDayThen)}⟩–—*\n" +
                        $"*   —––⟨{weekType}⟩––—*\n";
                int homeworkNullCount = 0;
                // создание объекта SqlDataReader для чтения данных
                using (SqlDataReader reader = command3.ExecuteReader())
                {
                    SizeF homeworkSize = graphics.MeasureString(TextPhoto, fontHomework, image.Width, stringFormat);
                    SizeF lessonSize = graphics.MeasureString(TextPhoto, font, image.Width, stringFormat);
                    SizeF lessonCulls = graphics.MeasureString(TextPhoto, fontCulls, image.Width, stringFormat);
                    TextPhoto = "";
                    int count = 0;
                    while (reader.Read())
                    {
                        string culls;
                        if (Culls[count] != null && !string.IsNullOrWhiteSpace(Culls[count]))
                        {
                            culls = Culls[count];
                        }
                        else
                        {
                            culls = "???";
                        }
                        string lesson_name, homework;
                        // чтение значений из текущей строки таблицы
                        lesson_name = (string)reader["lesson_name"];
                        try
                        {
                            homework = (string)reader["homework"] + ";";
                        }
                        catch//если поле пустое, то записываем ничего
                        {
                            homework = " ✘ ";
                            homeworkNullCount++;
                        }

                        try
                        {
                            lesson_name1 = lesson_name.Trim().Split('≣')[0].Trim(); lesson_name2 = lesson_name.Trim().Split('≣')[1].Trim();
                            if (!string.IsNullOrWhiteSpace(lesson_name1) && !string.IsNullOrWhiteSpace(lesson_name2))//если поделились но пустые или специально пустые
                            {
                                if (weekType == "Числитель")
                                {
                                    lesson_name = lesson_name1;
                                }
                                else
                                {
                                    lesson_name = lesson_name2;
                                }
                            }
                        }
                        catch { }
                        // формирование строки для переменной
                        TextPhoto = $"{lesson_name}: {homework}\n";

                        textWidthF += homeworkSize.Height + 5;
                        // Определение размеров текста
                        homeworkSize = graphics.MeasureString(homework, fontHomework, image.Width, stringFormat);
                        lessonSize = graphics.MeasureString(lesson_name, font, image.Width, stringFormat);
                        lessonCulls = graphics.MeasureString(culls, fontCulls, image.Width, stringFormat);

                        if (textWidthF + lessonSize.Height + 5 + homeworkSize.Height > image.Height - 50)
                        {
                            await botClient.SendTextMessageAsync(globalChatId, $"*❌Ошибка [1001]:* При отправки дз весь текст дз не поместился на картинке", parseMode: ParseMode.MarkdownV2);
                            // Освободите ресурсы
                            graphics.Dispose();
                            image.Dispose();
                            MaxLengthImage = true;
                            // return;
                        }

                        // Рисование текста с переносом
                        graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        if (homeworkSize.Width >= image.Width - 200)
                        {
                            homeworkSize = graphics.MeasureString(await transferText(homework, 41), fontHomework, image.Width, stringFormat);

                            graphics.DrawString(lesson_name, font, brushLesson, 205, textWidthF);
                            graphics.DrawString(culls, fontCulls, brushNumbers, 205 + lessonSize.Width + 1, textWidthF + 5);
                            textWidthF += lessonSize.Height + 5;
                            graphics.DrawString(await transferText(homework, 41), fontHomework, brush, 205, textWidthF);
                        }
                        else
                        {
                            graphics.DrawString(lesson_name, font, brushLesson, 205, textWidthF /*+ 40*/);
                            graphics.DrawString(culls, fontCulls, brushNumbers, 205 + lessonSize.Width + 1, textWidthF + 5);
                            graphics.DrawString(" " + homework, fontHomework, brush, 205, textWidthF + 40);
                            textWidthF += 40;
                        }
                        // формирование строки для переменной
                        TextMessage += $"╟\\-‹*{await EscapeMarkdownV2(lesson_name)}:* {await EscapeMarkdownV2(homework)}";
                        TextMessage += "\n║\n";

                        count++;
                    }
                }
                await sqlConnection.CloseAsync();

                int lastIndex = TextMessage.LastIndexOf('║');//удаляю последнее вхождение знака
                if (lastIndex >= 0) // проверяем, что символ найден
                {
                    TextMessage = TextMessage.Substring(0, lastIndex) + TextMessage.Substring(lastIndex + 1);
                }
                TextMessage += $"*{await EscapeMarkdownV2($"—–-⟨{DateDayThen}⟩-–—")}*";

                MemoryStream MemStreamImage = new();
                image.Save(MemStreamImage, ImageFormat.Png);
                MemStreamImage.Seek(0, SeekOrigin.Begin);

                if (EditMessage)
                {
                    await ReturnGlobalDzChat();

                    if (globalDzChat != 0)
                    {
                        if (MaxLengthImage)
                        {
                            // Заменяем текст в сообщении обязательно !EditMessageCaptionAsync!
                            var editedTextMessage = await botClient.EditMessageCaptionAsync(globalBaseDzChat,
                                globalDzChat, TextMessage,
                                parseMode: ParseMode.MarkdownV2);
                        }
                        else
                        {
                            //преобразуем из FileStream и создаем импорт медиа

                            InputMediaPhoto inputMediaPhoto = new(InputFile.FromStream(MemStreamImage, "imageDz.png"));

                            // Заменяем фотографию в сообщении
                            var editedPhotoMessage = await botClient.EditMessageMediaAsync(globalBaseDzChat,
                                globalDzChat, inputMediaPhoto);
                            // Заменяем текст в сообщении обязательно !EditMessageCaptionAsync!
                            var editedTextMessage = await botClient.EditMessageCaptionAsync(globalBaseDzChat,
                                globalDzChat, TextMessage,
                                parseMode: ParseMode.MarkdownV2);
                        }
                    }

                }
                else
                {
                    if (MaxLengthImage)
                    {
                        var messageRes = await botClient.SendTextMessageAsync(chatId: globalBaseDzChat, text: TextMessage, parseMode: ParseMode.MarkdownV2);
                        globalDzChat = messageRes.MessageId;
                    }
                    else
                    {
                        // Отправка фото пользователю в Telegram
                        var messageRes = await botClient.SendPhotoAsync(chatId: globalBaseDzChat, photo: InputFile.FromStream(MemStreamImage, "imageDz.png"),
                                                       caption: TextMessage, parseMode: ParseMode.MarkdownV2);
                        globalDzChat = messageRes.MessageId;
                    }
                    await System.IO.File.WriteAllTextAsync(globalFilePathEditDzChatID, $"{globalDzChat}☰{DateTime.Now}");
                }

                MemStreamImage.Close();
                graphics.Dispose();
                image.Dispose();
                Final = $"{homeworkNullCount} ☰ true";
#pragma warning restore CA1416 // Проверка совместимости платформы
            }
            return Final;
        }

        async Task TimerBot(int time = 5)
        {
            TimerState timerState = new();
            System.Threading.Timer Timer;
            // Создаем объект состояния таймера
            timerState = new TimerState
            {
                BotClient = botClient,
                ChatId = globalChatId,
                PressingButtons = pressingButtons,
                ShutdownTimer = shutdownTimer
            };
            // Создаем таймер и передаем ему метод обратного вызова, объект состояния таймера, задержку перед первым запуском и интервал между повторениями
            Timer = new System.Threading.Timer(async state => await TimerCallback(state), timerState, TimeSpan.FromMinutes(time), TimeSpan.Zero);
            await botClient.SendTextMessageAsync(globalChatId, $"⏳Таймер запущен на {time} мин, для остановки таймера введите:\n/stopTimer");
        }

        async Task messageEveryone(string message)
        {
            sqlConnection.Close();
            // открытие соединения
            sqlConnection.Open();
            // создание команды для выполнения SQL-запроса с JOIN-ом таблиц
            SqlCommand selectCommand = new($"select * from Users", sqlConnection);
            // создание объекта SqlDataReader для чтения данных
            using (SqlDataReader reader = selectCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    // чтение значений из текущей строки таблицы
                    long user_id = reader.GetInt64(0);
                    string userName = reader.GetString(1);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[{DateTime.Now}] Начало отправки сообщений");
                    Console.ResetColor();
                    // формирование строки
                    try
                    {
                        await botClient.SendTextMessageAsync(user_id, message, parseMode: ParseMode.MarkdownV2);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Сообщение отправлено {userName} c ID {user_id}");
                        Console.ResetColor();
                    }
                    catch
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Не удалось отправить сообщение {userName} c ID {user_id}");
                        Console.ResetColor();
                    }
                }
            }
            sqlConnection.Close();
        }

        async Task<string> RemoveDigitsAsync(string input)
        {
            // Создаем регулярное выражение для поиска цифр от 1 до 8 в формате "⒈", "⒉", и т.д.
            Regex regex = new(@"⒈|⒉|⒊|⒋|⒌|⒍|⒎|⒏");

            // Используем асинхронный метод Replace для удаления цифр из введенного слова
            string result = await Task.Run(() => regex.Replace(input, string.Empty));

            return result;
        }

        Task<string> CheckAndTruncateWordAsync(string word, int maxLength)//для сокращения текста
        {
            if (word.Length > maxLength)
            {
                word = word.Substring(0, maxLength) + "...";
            }

            return Task.FromResult(word);
        }

        Task<string> transferText(string text, int maxLength)//для переноса текста на картинке
        {
            string[] textMs = text.Trim().Split(' ');
            int count = 0;
            string FinalText = "";
            for (int i = 0; i < textMs.Length; i++)
            {
                count += textMs[i].Length;//проходимся по нашему тесту и и складываем длину по словам, если длина в строке больше чем нужно, тогда добавляем перенос
                if (count <= maxLength)
                {
                    FinalText += " ";
                }
                else
                {
                    FinalText += "\n";
                    count = textMs[i].Length;
                }
                FinalText += textMs[i];
            }
            return Task.FromResult(FinalText);
        }

        async Task UpdateUserStatusAsync(long userId, bool locked)
        {
            string lockedText = locked ? "Блокировка" : "Разблокировка";
            try//блокируем или разблокируем пользователя
            {
                using SqlConnection sqlConnection = new(connectionString);
                if (!await dataAvailability(userId, "Users"))//если пользователя нет в бд, то добавляем
                {
                    await AddUserToDatabase(userId, "БлокировкаЮзера");
                }
                await sqlConnection.OpenAsync();

                string query = "UPDATE Users SET is_blocked = @Locked WHERE user_id = @UserId";

                using SqlCommand command = new(query, sqlConnection);
                command.Parameters.AddWithValue("@Locked", locked);
                command.Parameters.AddWithValue("@UserId", userId);

                int rowsAffected = await command.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    await botClient.SendTextMessageAsync(chatId: globalChatId, text: $"✅ {lockedText} пользователя ID: {userId} удалась!");
                    await UpdateIndexStatisticsAsync("Users");//обновляем индекса таблицы
                    await botClient.SendTextMessageAsync(chatId: userId, text: $"❗️Вам выдана {lockedText}❗️");
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId: globalChatId, text: $"❌ Ошибка [2002]: {lockedText} пользователя ID: {userId} не удалась...");
                }
            }
            catch (Exception ex)
            {
                // Обработка ошибок
                await botClient.SendTextMessageAsync(chatId: globalChatId, text: $"❌ Ошибка [2002]: {lockedText} пользователя ID: {userId} не удалась: {ex.Message}");
            }
            await sqlConnection.CloseAsync();
        }

        async Task<bool> IsUserBlockedAsync(long userId)
        {
            sqlConnection.Close();
            bool result = false;//проверка на блокировку
            try
            {
                await sqlConnection.OpenAsync();

                string query = @"IF EXISTS (SELECT * FROM Users WHERE user_id = @UserId AND is_blocked = 1)
                                SELECT 'true'
                            ELSE
                                SELECT 'false'";

                using SqlCommand command = new(query, sqlConnection);
                command.Parameters.AddWithValue("@UserId", userId);
                object queryResult = await command.ExecuteScalarAsync();

                if (queryResult != null && queryResult != DBNull.Value)//берем результат из бд
                {
                    result = Convert.ToBoolean(queryResult);
                }
            }
            finally//в любом случае закрываем соединение с бд
            {
                if (sqlConnection.State == ConnectionState.Open)
                {
                    await sqlConnection.CloseAsync();
                }
                //sqlConnection.Dispose();
            }

            return result;
        }

        async Task UpdateIndexStatisticsAsync(string tableName)
        {
            sqlConnection.Close();//обновление индекса таблиц (для быстрой работы)
            await sqlConnection.OpenAsync();

            SqlCommand updateStatisticsCommand = new($"UPDATE STATISTICS {tableName};", sqlConnection);
            await updateStatisticsCommand.ExecuteNonQueryAsync();

            if (sqlConnection.State == ConnectionState.Open)
            {
                await sqlConnection.CloseAsync();
            }
            //sqlConnection.Dispose();
        }

        async Task AddUserToDatabase(long UserId, string UserName = "НетИмени")
        {
            await sqlConnection.OpenAsync();//добавление человека в базу
                                            // Добавляем запись в таблицу
            SqlCommand insertCommand = new("insert into Users(user_id, username, user_group) values (@userId, @username, @userGroup)", sqlConnection);
            insertCommand.Parameters.AddWithValue("@userId", UserId);
            insertCommand.Parameters.AddWithValue("@username", UserName);
            insertCommand.Parameters.AddWithValue("@userGroup", "НетГруппы");
            insertCommand.ExecuteNonQuery();
            await sqlConnection.CloseAsync();
        }

        async Task CopyDataDz(string DayName)
        {//копирование расписания из таблицы расписания в дз по дню недели
            sqlConnection.Close();
            await ReturnGlobalDzChat();
            if (globalDzChat == 0)
            {
                //обнуляю что-бы не редактировать старое сообщение новым дз
                await System.IO.File.WriteAllTextAsync(globalFilePathEditDzChatID, $"{globalDzChat}☰{DateTime.Now}");
            }
            // открытие соединения
            await sqlConnection.OpenAsync();
            // создание команды для выполнения SQL-запроса на копирование номеров расписания в дз
            string query = $"INSERT INTO {DayName}_Homework (Id_lesson) SELECT Id_lesson FROM {DayName}_Schedule";
            SqlCommand command = new(query, sqlConnection);
            // выполнение команды
            command.ExecuteNonQuery();
            await botClient.SendTextMessageAsync(chatId: globalChatId, text: "📝Данные дня обновлены!");
            // закрытие соединения
            await sqlConnection.CloseAsync();
        }

        async Task<bool> InitialChecks(long ID)
        {
            if (globalUserId == 0)//если id пустое
            {
                await botClient.SendTextMessageAsync(chatId: globalChatId, text: "❌Ошибка [4002]/[2001]");
                return true;
            }
            if (await IsUserBlockedAsync(ID))//проверка на блокировку
            {
                return true;
            }
            if (spamDetector.IsSpam(ID))//проверяем на спам
            {
                // Обработка спама
                Console.ForegroundColor = ConsoleColor.Red;//цвет консоли
                Console.WriteLine($"[{DateTime.Now}] Пользователь {globalChatId} отправлял слишком много сообщений в короткое время!");
                Console.ResetColor();
                await UpdateUserStatusAsync(globalUserId, true);//блокируем пользователя
                await botClient.SendTextMessageAsync(chatId: globalChatId, text: "❌Ошибка [0000]: Вы заблокированы за спам...");
                await botClient.SendTextMessageAsync(chatId: globalAdminId, text: $"🛑Пользователь {globalUserId} заблокирован за спам🛑");
                return true;
            }
            return false;
        }

        async Task WhatWeekType()
        {
            string readContent = "Числитель";
            try
            {
                readContent = await System.IO.File.ReadAllTextAsync(globalFilePathWeekType);
            }
            catch
            {
                readContent = "Числитель";
                await System.IO.File.WriteAllTextAsync(globalFilePathWeekType, readContent);
            }
            if (string.IsNullOrWhiteSpace(readContent.Trim()))
            {
                readContent = "Числитель";
                await System.IO.File.WriteAllTextAsync(globalFilePathWeekType, readContent);
            }
            weekType = readContent;
        }

        Task<string[]> AddToArray(string[] array, string element)
        {
            Array.Resize(ref array, array.Length + 1);
            array[array.Length - 1] = element;
            return Task.FromResult(array);
        }

        Task<string> FormatTime(TimeSpan time)
        {
            if (time.TotalHours >= 24)
            {
                int day = (int)time.Days;
                int hours = (int)time.TotalHours;
                int minutes = time.Minutes;
                int seconds = time.Seconds;

                return Task.FromResult($"{day} дней, {hours} часов, {minutes} минут, {seconds} секунд");
            }
            else if (time.TotalMinutes >= 60)
            {
                int hours = (int)time.TotalHours;
                int minutes = time.Minutes;
                int seconds = time.Seconds;

                return Task.FromResult($"{hours} часов, {minutes} минут, {seconds} секунд");
            }
            else
            {
                int minutes = (int)time.TotalMinutes;
                int seconds = time.Seconds;

                return Task.FromResult($"{minutes} минут, {seconds} секунд");
            }
        }

        async Task ReturnGlobalDzChat()
        {
            string ErrorMessage = "НЕТоШИБКИ";
            // Чтение данных из файла
            if (System.IO.File.Exists(globalFilePathEditDzChatID))
            {
                string fileContent = await System.IO.File.ReadAllTextAsync(globalFilePathEditDzChatID);

                // Разделение содержимого файла на части: текст сообщения и дату/время
                string[] parts = fileContent.Split("☰");
                if (parts.Length == 2)
                {
                    string message = parts[0].Trim();
                    string timeString = parts[1].Trim();

                    if (int.Parse(message) != 0)
                    {
                        // Парсинг времени из строки
                        if (DateTime.TryParse(timeString, out DateTime messageTime))
                        {
                            // Вычисление времени на следующий день и 10:00
                            DateTime nextDay = messageTime.AddDays(1).AddHours(-(messageTime.Hour - 10));

                            // Получение текущего дня и времени
                            DateTime currentDateTime = DateTime.Now;

                            // Проверка времени
                            if (currentDateTime <= nextDay)
                            {
                                // Время не превышено, присваиваем значение из файла
                                globalDzChat = int.Parse(message);
                            }
                            else
                            {
                                // Время превышено, присваиваем значение 0
                                globalDzChat = 0;
                                ErrorMessage = "Время действия фила превышено";
                            }
                        }
                        else
                        {
                            // Ошибка при парсинге времени
                            globalDzChat = 0;
                            ErrorMessage = "Ошибка при парсинге времени";
                        }
                    }
                    else
                    {
                        // Обнуленное ID
                        globalDzChat = 0;
                        ErrorMessage = "Ошибка 0";
                    }
                }
                else
                {
                    // Некорректный формат файла
                    globalDzChat = 0;
                    ErrorMessage = "Некорректный формат файла";
                }
            }
            else
            {
                // Файл не существует
                globalDzChat = 0;
                ErrorMessage = "Файл не существует";
            }

            if (globalDzChat == 0)
            {
                if (ErrorMessage == "Время действия фила превышено")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[{DateTime.Now}] Время действия фила \"globalDzChat\" превышено, назначено значение 0");
                    Console.ResetColor();
                }
                else if (ErrorMessage != "Ошибка 0" && ErrorMessage != "НЕТоШИБКИ")
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"*❌Ошибка при получении редактируемого ID сообщения [1100]:* {ErrorMessage}", parseMode: ParseMode.MarkdownV2);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{DateTime.Now}] Ошибка при получении редактируемого ID сообщения: {ErrorMessage}");
                    Console.ResetColor();
                }
            }
        }

        async Task<bool> ReturnDayWeek(bool isYesterday = false)
        {
            bool Exception = false;
            RussianDayNow = DateTime.Today.ToString("dddd");
            RussianDayThen = DateTime.Today.AddDays(1).ToString("dddd");
            RussianDayYesterday = DateTime.Today.AddDays(-1).ToString("dddd");

            DateDayNow = DateTime.Today.ToString("dd.MM");
            DateDayThen = DateTime.Today.AddDays(1).ToString("dd.MM");
            DateDayYesterday = DateTime.Today.AddDays(-1).ToString("dd.MM");


            if (!(weekDays.TryGetValue(RussianDayNow, out EnglishDayNow)
                && weekDays.TryGetValue(RussianDayThen, out EnglishDayThen)
                && weekDays.TryGetValue(RussianDayYesterday, out EnglishDayYesterday)))
            {//проверяем является ли слово днем недели и если да, записываем в переменную анг версию
                if (weekDays.ContainsValue(RussianDayNow) 
                    && weekDays.ContainsValue(RussianDayThen) 
                    && weekDays.ContainsValue(RussianDayYesterday))
                {
                    EnglishDayNow = RussianDayNow;
                    EnglishDayThen = RussianDayThen;
                    EnglishDayYesterday = RussianDayYesterday;

                    RussianDayNow = weekDays.Keys.ElementAt(weekDays.Values.ToList().IndexOf(EnglishDayNow));
                    RussianDayThen = weekDays.Keys.ElementAt(weekDays.Values.ToList().IndexOf(EnglishDayThen));
                    RussianDayYesterday = weekDays.Keys.ElementAt(weekDays.Values.ToList().IndexOf(EnglishDayYesterday));
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]", replyMarkup: Keyboards.cancel);
                    return Exception = true;
                }
            }
            if (DateTime.Now < DateTime.Today.AddHours(10) && isYesterday)
            {
                EnglishDayThen = EnglishDayNow;
                EnglishDayNow = EnglishDayYesterday;

                RussianDayThen = RussianDayNow;
                RussianDayNow = RussianDayYesterday;

                DateDayThen = DateDayNow;
                DateDayNow = DateDayYesterday;
            }
            return Exception;
        }

        async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)//любые возможные ошибки
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            string Username = "ОШИБКА [1004]: Нет имени";
            try
            {
                if (globalChatId == globalAdminId)//проверяю кто отправил сообщение, если не я, то отправляю уведомление с ошибкой и примерно где она была найдена
                {
                    if (ErrorMessage.Contains("Telegram.Bot.Exceptions.RequestException: Request timed out")
                        || ErrorMessage.Contains("Request timed out"))
                    {
                        await botClient.SendTextMessageAsync(globalAdminId, $"❗️Бот был перезапущен❗️");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalAdminId, $"Ошибка:\n{ErrorMessage}");
                    }
                    Username = globalUsername;
                }
                else
                {
                    if (ErrorMessage.Contains("Telegram.Bot.Exceptions.RequestException: Request timed out")
                        || ErrorMessage.Contains("Request timed out"))
                    {
                        await botClient.SendTextMessageAsync(globalAdminId, $"❗️Бот был перезапущен❗️");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌🛑❌Вы сломали бота(((\n" +
                            $"❗️Всем пользователям будет выдано ограничение на его использование❗️\n" +
                            $"🛠На исправление уже направлены все силы, это займет како-то время..." +
                            $"\n\n" +
                            $"Ошибка:\n{ErrorMessage}");
                        /*await messageEveryone($"❗️К сожалению у бота возникли критические ошибки, всем пользователям будет выдано ограничение на его использование❗️\n" +
                            $"🛠На исправление уже направлены все силы, это займет како-то время");*/

                        sqlConnection.Close();
                        sqlConnection.Open();
                        SqlCommand selectCommandUser = new("SELECT * FROM Users WHERE user_id = @userId", sqlConnection);//считываем данные о человеке
                        selectCommandUser.Parameters.AddWithValue("@userId", globalUserId);
                        var readerUser = selectCommandUser.ExecuteReader();

                        if (readerUser.Read())
                        {
                            Username = readerUser.GetString(readerUser.GetOrdinal("username"));//добавляю имя, что-бы можно было написать
                            readerUser.Close();
                        }
                        sqlConnection.Close();
                        await botClient.SendTextMessageAsync(globalAdminId, $"❌🛑❌Поломка бота!\n" +
                            $"У пользователя {globalChatId}, вероятное имя @{Username}, время поломки {DateTime.Now}. Последнее отправленное действие: {Exception}" +
                            $"\n\n" +
                            $"Ошибка:\n{ErrorMessage}");
                    }
                }
            }
            catch { }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{DateTime.Now}] Ошибка у пользователя {globalChatId}, вероятное имя @{Username} . Последнее отправленное действие: {Exception} И ошибка {ErrorMessage}");
            Console.ResetColor();
            return;
        }

    }

}