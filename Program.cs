﻿using LibDz_infoBot;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using System.Timers;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;

internal class Program
{
    public static async Task Main()
    {
        //    5645539273:AAFuIkDhTnFTQNvjBL1ocC9fb3BqmJPt4J0
        string token = "6154384299:AAHkuqxMXNW3Chm2DG-EvOY6DWoxPtOzgOo";
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
            { "blocedTimer", false },
            { "shutdownBot", false },
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
        Dictionary<long, DateTime> blockedUser = new();
        Dictionary<int, object[]> NameImgMessage = new();
        int globalMessageTextId = 0, globalMessagePhotoId = 0, globalNewsId = 0, globalNewsNumber = 0, globalDzInfoAdd = 0, globalDzInfoEdit = 0, globalCullsInfoEditId = 0, globalIdEditMessage = 0, globalTimerCountRestart = 0, globalDeleteMessageCancelId = 0;//глобальные переменные, которые используются для разных действий во всем коде
        long globalUserId = 0, globalChatId = 1545914098, globalIdBaseChat = /*-1001602210737*/  -1001797288636, globalAdminId = 1545914098;
        string globalUsername = "СтандартИмя", globalGroupName = "ИС1-21", Exception = "ПУСТО", globalKeyMenu = "MainMenu", weekType = "Числитель", //keyMenu переменная которая сохраняет меню в котором мы сейчас находимся (для кнопки назад)
            globalFilePathWeekType = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName, "Databases", "weekType.txt"),
            globalConfirmValue = "";
        string EnglishDayNow = "Monday", EnglishDayThen = "Monday", EnglishDayYesterday = "Monday", RussianDayNow = "Понедельник", RussianDayThen = "Понедельник", RussianDayYesterday = "Понедельник", DateDayNow = "01.01", DateDayThen = "01.01", DateDayYesterday = "01.01";
        object[] globalTablePicture = new object[] { 4, 6, "" };
        DateTime globalStartTime = DateTime.Now, globalChangeGroupTime = DateTime.Now.AddMinutes(-10);
        spamDetector.StartTime = globalStartTime;

        #region Подключение к БД
        string databaseFilePath = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName, "Databases"/*, "DatabaseTelegramBot.mdf"*/);

        string connectionString = ConfigurationManager.ConnectionStrings["TelegramBotLibDB"].ConnectionString;

        connectionString = connectionString.Replace("|DataDirectory|", databaseFilePath);

        //хотел получать путь через DataDirectory с помощью AppDomain.CurrentDomain.SetData но оно не работает корректно,
        //теперь получаю путь как обычно через App.config при этом получая путь возвращаясь на 3 директории назад и заменяя |DataDirectory| на нужный путь

        SqlConnection sqlConnection = new(connectionString);
        spamDetector.sqlConnection = sqlConnection;
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

        #region Логи включения
        var botName = await botClient.GetMeAsync();//запоминаем имя бота
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"[{DateTime.Now}] Запущен бот: @{botName.Username}");//информируем о запуске в консоль
        await ReturnDayWeek(true);
        //await WhatWeekType(globalGroupName);
        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.WriteLine($"-> Основные данные:\n" +//информируем о основных данных в консоль
            $">Текущее дни недели учитывая время до 10:00:\n" +
            $"Вчера -> анг: {EnglishDayYesterday} ру: {RussianDayYesterday} дат: {DateDayYesterday}\n" +
            $"Сегодня -> анг: {EnglishDayNow} ру: {RussianDayNow} дат: {DateDayNow}\n" +
            $"Завтра -> анг: {EnglishDayThen} ру: {RussianDayThen} дат: {DateDayThen}\n" +
            $">Текущий основной тип недели: {weekType}\n" +
            $">Текущее Id администратора: {globalAdminId}\n" +
            $">Текущий владелец бота (стандартный globalChatId): {globalChatId}\n" +
            /*$">Текущий Id чата отправки дз: {globalBaseDzChat}\n" +
            $">Текущий Id сообщения для редактирования: {globalDzChat}\n" +*/
            $">Текущий путь для соединения с базой данных: {connectionString}");
        Console.WriteLine();
        Console.ResetColor();//для сброса цвета консоли к стандартной
        await botClient.SendTextMessageAsync(globalAdminId, "🌐Доброго времени суток, бот запущен!🌐");
        #endregion

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
            if (globalIdEditMessage != 0)
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
            object[] DzChat = await DzBaseChat(globalGroupName);// отправляем сообщение
            int homeworkNullCount = (int)DzChat[0];//проверяем сколько пустых строк дз 
            bool FTres = (bool)DzChat[1];//отправилось ли вообще 
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
                Console.WriteLine($"[{DateTime.Now}] Авто-сообщение отправлено в чат: {globalIdBaseChat}");

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
            // перезапускаем таймер на следующую неделю
            timerWeekType.Interval = TimeSpan.FromDays(7).TotalMilliseconds;
            timeLeft = TimeSpan.FromDays(7);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{DateTime.Now}] Таймер типа недели перезапущен на след неделю, оставшееся время до запуска: {await FormatTime(timeLeft)}");
            Console.ResetColor();

            await sqlConnection.OpenAsync();
            List<string[]> updateGroup = new List<string[]>();
            using (SqlCommand command = new SqlCommand("SELECT group_Name, week_Type, week_Day_DZ FROM Groups", sqlConnection))
            {
                using (SqlDataReader readerGroup = await command.ExecuteReaderAsync())
                {
                    int countGroup = 0;
                    if (await readerGroup.ReadAsync())
                    {// Получаем значения из базы
                        string group_Name, week_Type, week_Day_DZ = "";
                        group_Name = (string)readerGroup["group_Name"];
                        if (!readerGroup.IsDBNull(readerGroup.GetOrdinal("week_Type")))
                        {
                            if ((bool)readerGroup["week_Type"])
                            {
                                week_Type = "Числитель";
                            }
                            else
                            {
                                week_Type = "Знаменатель";
                            }
                        }
                        else
                        {
                            week_Type = weekType;
                        }
                        // меняем значение переменной weekType каждую неделю
                        week_Type = week_Type == "Числитель" ? "Знаменатель" : "Числитель";
                        if (!readerGroup.IsDBNull(readerGroup.GetOrdinal("week_Day_DZ")))
                        {
                            if ((string)readerGroup["week_Day_DZ"] != "Monday")
                            {
                                week_Day_DZ = (string)readerGroup["week_Day_DZ"];
                            }
                        }
                        updateGroup[countGroup] = new string[] { group_Name, week_Type, week_Day_DZ };
                    }
                }
            }
            foreach (var GroupMs in updateGroup)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    if (GroupMs[2] == "") GroupMs[2] = "NULL";
                    else
                    {
                        await sqlConnection.CloseAsync();
                        await FullDeleteDz(GroupMs[0]);
                        await sqlConnection.OpenAsync();
                        Console.WriteLine($"[{DateTime.Now}] Произведена очистка дз группы {GroupMs[0]} для дня {GroupMs[2]}");
                    }
                    using (SqlCommand commandUp = new SqlCommand("UPDATE Groups SET week_Type = @weekType, week_Day_DZ = @weekDayDZ WHERE group_Name = @groupName", sqlConnection))
                    {
                        commandUp.Parameters.AddWithValue("@groupName", GroupMs[0]);
                        commandUp.Parameters.AddWithValue("@weekType", GroupMs[1]);
                        commandUp.Parameters.AddWithValue("@weekDayDZ", GroupMs[2]);
                        await commandUp.ExecuteNonQueryAsync();
                    }
                    Console.WriteLine($"[{DateTime.Now}] Тип недели группы {GroupMs[0]} изменен, теперь тип недели: {GroupMs[1]}");
                    Console.ResetColor();
                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{DateTime.Now}] НЕ удалось изменить тип недели {GroupMs[1]} группы {GroupMs[0]} или удалить дз для дня {GroupMs[2]} !");
                    Console.ResetColor();
                }
            }
            await sqlConnection.CloseAsync();

            //System.IO.File.WriteAllText(globalFilePathWeekType, weekType);

            /*foreach (string RussiansDay in weekDays.Keys)
            {
                if (weekDays.TryGetValue(RussiansDay, out string EnglishDay))
                {

                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{DateTime.Now}] Ошибка: Невозможно получить английское название дня недели для русского дня {RussiansDay}");
                    Console.ResetColor();
                    return;//маловероятная возможная ошибка
                }
            }*/

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

        Console.WriteLine();
        Console.WriteLine($"[ЧЧ.ММ.ГГГГ ЧЧ:ММ:СС] |  Id  чата  | Имя Пользователя | _________________________________________________________");
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
                Console.WriteLine($"[{DateTime.Now}] | {globalChatId} | {globalUsername} | Действие кнопки под сообщениями: <'{update.CallbackQuery.Data}'>");
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);
                await ButtonUpdate(update.CallbackQuery.Data, callbackQuery);//запускаем обработку действия
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
                Console.WriteLine($"[{DateTime.Now}] | {globalChatId} | {globalUsername} | Отправлено фото");

                await botClient.SendChatActionAsync(globalChatId, ChatAction.UploadPhoto);
                await photoUpdate(update.Message);//запускаем обработку действия
            }
            else if (update?.Type == UpdateType.Message)//Обработать сообщение любое сообщение отправленное текстом (включая меню кнопки)
            {
                globalChatId = update.Message.Chat.Id;//обновляем все данные после каждого сообщения
                globalUserId = update.Message.From.Id;
                globalUsername = update.Message.Chat.Username;//обновляем все данные после каждого сообщения
                if (string.IsNullOrWhiteSpace(globalUsername))//если имя пустое, то записываем стандартное
                {
                    //await botClient.SendTextMessageAsync(chatId: globalChatId, text: "❌Ошибка [4002]");
                    globalUsername = "СтандартИмя";
                }

                if (update.Message?.Chat?.Type != ChatType.Private)//обрабатываем только действия из личных чатов
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{DateTime.Now}] | {globalChatId} | {globalUsername} | Сообщение не из личного чата: <'{update.Message.Text}'>");//информируем о сообщении не из личного чата в консоль
                    Console.ResetColor();
                    return;
                }
                if (!string.IsNullOrWhiteSpace(update.Message?.Text))//проверяем пустое ли сообщение
                {
                    if (await InitialChecks(globalUserId))
                    {
                        return;
                    }
                    Console.WriteLine($"[{DateTime.Now}] | {globalChatId} | {globalUsername} | Сообщение: <'{update.Message.Text}'>");//информируем о сообщении из личного чата в консоль
                    Exception = $"Сообщение: {update.Message.Text}";
                    await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);
                    await HandleMessage(update.Message);//запускаем обработку действия
                }
                else
                {
                    if (await InitialChecks(globalUserId))
                    {
                        return;
                    }

                    await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

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
                        var message = update.Message;
                        if (message.Sticker != null)
                        {
                            Exception = $"Отправлен стикер";
                            // Обработка стикера
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"[{DateTime.Now}] | {globalChatId} | {globalUsername} | Отправлен стикер");
                            Console.ResetColor();
                            // Создаем объект InputFile с использованием file_id
                            await botClient.SendStickerAsync(update.Message.Chat, InputFile.FromFileId("CAACAgIAAxkBAAEDGORls6AL2bnvk6OFjg37_TXZ5kkaOwACxxAAAvbBUUgn7vLk3PY2aDQE"));
                        }
                        else if (message.Document != null || message.Audio != null || message.Video != null || message.Voice != null)
                        {
                            Exception = $"Отправлен Файл";
                            // Обработка файла
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"[{DateTime.Now}] | {globalChatId} | {globalUsername} | Отправлен Файл");
                            Console.ResetColor();
                            // Создаем объект InputFile с использованием file_id
                            await botClient.SendStickerAsync(update.Message.Chat, InputFile.FromFileId("CAACAgIAAxkBAAEDGUhls7MZMO5RSZERoZXZNtT9HbB9cAACwjIAArZywUu6_3f0O4E_BjQE"));
                            await botClient.SendTextMessageAsync(update.Message.Chat, "❌Ошибка [1400]: Неподдерживаемый формат сообщения!");
                        }
                        else
                        {
                            Exception = $"Пришло пустое сообщение";
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[{DateTime.Now}] | {globalChatId} | {globalUsername} | Пришло пустое сообщение!");//информируем о пустом сообщении 
                            Console.ResetColor();
                            await botClient.SendTextMessageAsync(update.Message.Chat, "❌Ошибка [4002]: Пустое сообщение...");
                        }
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
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);
                // Проверяем, есть ли userId в таблице
                if (!await dataAvailability(globalUserId, "Users"))
                {
                    await AddUserToDatabase(globalUserId, globalUsername);
                    // Отправляем сообщение пользователю с вопросом на ввод группы
                    await botClient.SendTextMessageAsync(globalChatId, "📝Вы добавлены в базу данных\\!\n✏️Для изменения группы введите /group", replyMarkup: Keyboards.MainMenu, parseMode: ParseMode.MarkdownV2);
                    //await UpdateIndexStatisticsAsync("Users");
                }
                else// Если userId нет в таблице, то добавляем данные
                {
                    await botClient.SendTextMessageAsync(globalChatId, "🤝*Мы уже с вами знакомы\\)*\n✏️Но если вы хотите изменить группу введите /group", replyMarkup: Keyboards.MainMenu, parseMode: ParseMode.MarkdownV2);

                    if (globalUsername != "СтандартИмя")
                    {
                        sqlConnection.Open();
                        // Обновляем имя пользователя в базе данных на всякий
                        SqlCommand updateCommand = new("UPDATE Users SET username = @userName WHERE user_id = @userId", sqlConnection);
                        updateCommand.Parameters.AddWithValue("@userId", globalUserId);
                        updateCommand.Parameters.AddWithValue("@userName", globalUsername);
                        updateCommand.ExecuteNonQuery();
                    }
                    else if (globalUsername == "СтандартИмя" || globalUsername == "НетИмени")
                    {
                        await botClient.SendTextMessageAsync(globalChatId, "❕Нам не удалось считать ваше _имя_, пожалуйста добавьте его _\\(Желательно ник TG, Без @\\)_ в своем *профиле* для возможной связи\\!\n" +
                            "✏️Для изменения имени войдите в /profile", replyMarkup: Keyboards.MainMenu, parseMode: ParseMode.MarkdownV2);
                    }
                }
                sqlConnection.Close();
                return;
            }
            #endregion 
            #region /help
            if (message.Text.Trim().ToLower() == "/help")//команда помощи
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);
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
                "/fast\\_edit\\_dz ➯ Быстрый переход к редактированию дз\r\n" +
                "/change\\_admin\\_type ➯ Изменение типа администратора\r\n" +
                "/update\\_photo\\_news ➯ Обновить фото к новости\r\n" +
                "/update\\_week\\_type ➯ Обновить тип недели\r\n" +
                "/update\\_send\\_dz ➯ Обновить возможность отправки дз в ваш чат\n" +
                "\n" +
                "*Список ошибок:*\n" +
                "`\\[0000\\]` ➱ Выключение или блокировка сервера;\r\n" +
                "\r\n" +
                "`\\[1100\\]` ➱ Не удалось обработать текст;\r\n" +
                "`\\[1200\\]` ➱ Не удалось обработать картинку;\r\n" +
                "`\\[1300\\]` ➱ Не удалось обработать кнопку;\r\n" +
                "`\\[1400\\]` ➱ Не удалось обработать файл;\r\n" +
                "\r\n" +
                "`\\[1001\\]` ➱ Превышение размера фила;\r\n" +
                "`\\[1002\\]` ➱ Неподдерживаемый формат файла;\r\n" +
                "`\\[1003\\]` ➱ Проблемы с локализацией дней недели;\r\n" +
                "`\\[1004\\]` ➱ Ошибка в выполнении инструкции Regex;\r\n" +
                "`\\[1005\\]` ➱ Выполнение команды до истечения заданного промежутку времени;\r\n" +
                "\r\n" +
                "`\\[2001\\]` ➱ Ошибка работы сети;\r\n" +
                "`\\[2002\\]` ➱ Ошибка в использовании Id пользователя;\r\n" +
                "\r\n" +
                "`\\[3001\\]` ➱ Ошибка в преобразовании текста для БД/Метода;\r\n" +
                "`\\[3002\\]` ➱ Значение в базе данных не найдено или равно NULL;\r\n" +
                "`\\[3003\\]` ➱ Значение уже находится в Базе данных;\r\n" +
                "`\\[3004\\]` ➱ Невозможно явно определить единственно верное значение из БД;\r\n" +
                "\r\n" +
                "`\\[4001\\]` ➱ Отсутствие файлов;\r\n" +
                "`\\[4002\\]` ➱ Одной из значений имеет значение NULL или размер равный 0;\r\n" +
                "`\\[4003\\]` ➱ Недостаточно прав для выполнения действия;",
                replyMarkup: Keyboards.Help, parseMode: ParseMode.MarkdownV2);
                return;
            }
            #endregion
            #region /group 
            if (message.Text.Trim().ToLower() == "/group" /*|| Regex.IsMatch(message.Text.Trim().ToLower(), "/group")*/)//команда для изменения группы
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                TimeSpan elapsedTime = DateTime.Now - globalChangeGroupTime;
                if (elapsedTime.TotalMinutes > 10 || await dataAvailability(globalUserId, "Admins 1"))
                {
                    await ChangeGroup();
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId: globalChatId, text: "❌Ошибка [1005]: Группу можно сменить только раз в 10 минут!");
                }
                return;
            }
            #endregion
            #region /profile
            if (message.Text.Trim().ToLower() == "/profile")//команда для отображения тек профиля
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (await dataAvailability(globalUserId, "Users"))//проверяем есть ли человек в нашей базе
                {
                    var messageRes = await botClient.SendTextMessageAsync(globalChatId, await userProfile(globalUserId), replyMarkup: Keyboards.Profile, parseMode: ParseMode.MarkdownV2);//если есть то выводим данные
                    globalMessageTextId = messageRes.MessageId; // сохраняем идентификатор сообщения если будет менять имя или группу
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"🔌Увы, но мы не нашли вас в нашей базе данных [3002], сейчас добавим...");

                    await AddUserToDatabase(globalUserId, globalUsername);

                    await botClient.SendTextMessageAsync(globalChatId, $"✅Пользователь *{await EscapeMarkdownV2(globalUsername)}* успешно зарегистрирован в базе данных\\.\n✏️Для изменения группы введите /group", parseMode: ParseMode.MarkdownV2);

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
                globalKeyMenu = "MainMenu";
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
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (await buttonTest())//стандартная проверка на то, выполняется ли какое-либо действие сейчас
                {
                    if (!await ReturnGroupName())
                    {
                        return;
                    }
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
                return;
            }
            #endregion
            #region /update_photo_news
            if (message.Text.Trim().ToLower() == "/update_photo_news")//обновление фотографии новостей
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (await buttonTest())//стандартная проверка на то, выполняется ли какое-либо действие сейчас
                {
                    if (await dataAvailability(globalUserId, "Admins 1") /*|| await dataAvailability(globalUserId, "Admins 2")*/)//проверка что ты являешься админом и какого типа: 1 или 2
                    {
                        await botClient.SendTextMessageAsync(globalChatId, "📝Введите номер новости для обновления картинки:", replyMarkup: Keyboards.cancel);
                        pressingButtons["numberNewsPicture"] = true;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    }
                }
                return;
            }
            #endregion
            #region /update_week_type
            if (message.Text.Trim().ToLower() == "/update_week_type")//обновление типа недели
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (!await ReturnGroupName())
                {
                    return;
                }
                if (await dataAvailability(globalUserId, "Admins 1"))//проверка что ты являешься админом
                {
                    await WhatWeekType(globalGroupName);
                    bool weekTypeBool;
                    if (weekType == "Числитель")
                    {
                        weekType = "Знаменатель";
                        weekTypeBool = false;
                    }
                    else
                    {
                        weekType = "Числитель";
                        weekTypeBool = true;
                    }

                    await sqlConnection.OpenAsync();
                    SqlCommand updateCommand = new SqlCommand("UPDATE Groups SET week_Type = @weekType WHERE group_Name = @groupName", sqlConnection);
                    updateCommand.Parameters.AddWithValue("@weekType", weekTypeBool);
                    updateCommand.Parameters.AddWithValue("@groupName", globalGroupName);
                    await updateCommand.ExecuteNonQueryAsync();
                    await sqlConnection.CloseAsync();

                    await botClient.SendTextMessageAsync(globalChatId, $"✅Теперь тип недели: *{weekType}*", parseMode: ParseMode.Markdown);
                    await DzBaseChat(globalGroupName, true);
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы Не являетесь админом или ваш уровень недостаточен\\! \\[3002\\]+\\[4003\\]\n" +
                        $"_Если вы и точно хотите сменить тип недели, перепроверьте, ошибка маловероятна\\. Для изменения данных обращайтесь по 👥Контактам в соответствующем разделе\\!_",
                        parseMode: ParseMode.MarkdownV2);
                }
                return;
            }
            #endregion
            #region /update_send_dz
            if (message.Text.Trim().ToLower() == "/update_send_dz")//отправка дз
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (!await ReturnGroupName())
                {
                    return;
                }
                if (await dataAvailability(globalUserId, "Admins"))//проверка что ты являешься админом
                {
                    sqlConnection.Open();
                    SqlCommand command = new("SELECT is_Sending_DZ FROM Groups WHERE group_Name = @groupName", sqlConnection);
                    command.Parameters.AddWithValue("@groupName", globalGroupName);
                    bool isSendingDZ = true;
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // обязательно Read
                        if (reader.Read())
                        {
                            isSendingDZ = (bool)reader["is_Sending_DZ"];
                        }
                    }
                    isSendingDZ = !isSendingDZ;
                    sqlConnection.Close();
                    await botClient.SendTextMessageAsync(globalChatId, $"✅Теперь ДЗ*{(isSendingDZ ? "" : " НЕ")} будет* автоматически отправляться в группу\\!", parseMode: ParseMode.MarkdownV2);

                    await sqlConnection.OpenAsync();

                    using SqlCommand command2 = new("UPDATE Groups SET is_Sending_DZ = @isSendingDZ WHERE group_Name = @groupName", sqlConnection);
                    command2.Parameters.AddWithValue("@isSendingDZ", isSendingDZ);
                    command2.Parameters.AddWithValue("@groupName", globalGroupName);
                    await command2.ExecuteNonQueryAsync();

                    await sqlConnection.CloseAsync();
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
                switch (globalKeyMenu)
                {
                    case "MainMenu":
                        await botClient.SendTextMessageAsync(
                                            chatId: globalChatId,
                                            text: "🎛Выберите действие",
                                            replyMarkup: Keyboards.Menu1);
                        globalKeyMenu = "Menu1";
                        return;
                    case "administrator":
                        if (await dataAvailability(globalUserId, "Admins 1") || await dataAvailability(globalUserId, "Admins 2"))
                        {
                            await botClient.SendTextMessageAsync(
                                     chatId: globalChatId,
                                     text: "😎*VIP Admin*, приветствую\\)",
                                     replyMarkup: Keyboards.HelpAdmin, parseMode: ParseMode.MarkdownV2);
                            globalKeyMenu = "HelpAdmin";
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                        }
                        return;
                    case "HelpAdmin":
                        if (await dataAvailability(globalUserId, "Admins 1"))
                        {
                            await botClient.SendTextMessageAsync(
                                     chatId: globalChatId,
                                     text: "🧑‍💻*Moderator*, приветствую\\!\\)",
                                     replyMarkup: Keyboards.Moderator, parseMode: ParseMode.MarkdownV2);
                            globalKeyMenu = "Moderator";
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                        }
                        return;
                    default:
                        await botClient.SendTextMessageAsync(globalChatId, "⤴️Переход назад...", replyMarkup: Keyboards.MainMenu);
                        globalKeyMenu = "MainMenu";
                        return;
                }
            }
            #endregion

            #region Кнопка Текущее ДЗ
            if (message.Text.Trim() == "🏠Текущее ДЗ")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (!await ReturnGroupName())
                {
                    return;
                }
                if (await ReturnDayWeek(true))
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]: Невозможно получить английское название дня недели для русского дня");
                    return;
                }
                await WhatWeekType(globalGroupName);
                if (await dataAvailability(0, "Homework ?", groupName: globalGroupName) && await dataAvailability(0, "GroupDayDZ", EnglishDayThen, globalGroupName))//проверяю есть ли вообще дз в базе
                {
                    // открытие соединения
                    sqlConnection.Open();

                    // создание команды для выполнения SQL-запроса с JOIN-ом таблиц
                    string queryHome = @$"SELECT lesson_name, homework 
                                FROM [{globalGroupName}_Homework]";
                    SqlCommand commandHomeDz = new(queryHome, sqlConnection);
                    string Text = $"*⠀  —–⟨Дз на {await EscapeMarkdownV2(RussianDayThen)}⟩–—*\n" +
                        $"*   —––⟨{weekType}⟩––—*\n";
                    // создание объекта SqlDataReader для чтения данных
                    using (SqlDataReader reader = commandHomeDz.ExecuteReader())
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
                    var messId = await botClient.SendTextMessageAsync(chatId: globalChatId, text: Text, parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.tablePicture);

                    globalTablePicture = new object[] { 10, 10, "Homework" };
                    await ReturnNameImgMessage(globalTablePicture, messId.MessageId);
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
                    globalKeyMenu = "administrator";
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
                await botClient.SendChatActionAsync(globalChatId, ChatAction.UploadDocument);

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
                        string caption = $"⌞⌋𝕹𝕰𝖂𝕾 *№{newsNumber}*⌊⌟\n" +
                            $"*{await EscapeMarkdownV2(newsTitle)}\n\n*"
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
                await botClient.SendChatActionAsync(globalChatId, ChatAction.UploadDocument);

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
                globalKeyMenu = "schedules";
                return;
            }
            #endregion

            #region Кнопка (расписание) На завтра
            if (message.Text.Trim() == "📆На завтра")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (!await ReturnGroupName())
                {
                    return;
                }
                sqlConnection.Close();
                string audience1 = "", audience2 = "";
                if (await ReturnDayWeek(true))
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]: Невозможно получить английское название дня недели для русского дня");
                    return;
                }
                await WhatWeekType(globalGroupName);

                if (await dataAvailability(0, "Schedule ?", EnglishDayThen, globalGroupName))
                {
                    // открытие соединения
                    sqlConnection.Open();
                    // создание команды для выполнения SQL-запроса
                    SqlCommand selectCommand = new($"select * from [{globalGroupName}_{EnglishDayThen}_Schedule]", sqlConnection);
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
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (!await ReturnGroupName())
                {
                    return;
                }
                sqlConnection.Close();

                int count = 0;
                string Text = $"🗓*Расписание на всю неделю:*\n\n", audience1 = "", audience2 = "", lesson_name1 = "", lesson_name2 = "";
                foreach (string RussiansDay in weekDays.Keys)
                {
                    if (weekDays.TryGetValue(RussiansDay, out string EnglishDay))
                    {
                        if (await dataAvailability(0, "Schedule ?", EnglishDay, globalGroupName))
                        {
                            // открытие соединения
                            sqlConnection.Open();
                            // создание команды для выполнения SQL-запроса
                            SqlCommand selectCommand = new($"select * from [{globalGroupName}_{EnglishDay}_Schedule]", sqlConnection);
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
                var messId = await botClient.SendTextMessageAsync(chatId: globalChatId,
                    text: Text, parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.tablePicture);

                globalTablePicture = new object[] { 10, count, "Schedule" };
                await ReturnNameImgMessage(globalTablePicture, messId.MessageId);
                return;
            }
            #endregion
            #region Кнопка Расписание звонков
            if (message.Text.Trim() == "🔔Расписание звонков")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (await dataAvailability(0, "Calls ?", groupName: globalGroupName))
                {//проверяем есть ли вообще звонки в базе
                    string[] calls = await CallsInfo(globalGroupName);
                    //отправляем
                    var messId = await botClient.SendTextMessageAsync(chatId: globalChatId,
                    text: calls[0], parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.tablePicture);

                    globalTablePicture = new object[] { 10, calls[1], "Calls" };
                    await ReturnNameImgMessage(globalTablePicture, messId.MessageId);
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
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (!await ReturnGroupName())
                {
                    return;
                }
                if (await dataAvailability(0, "Calls ?", groupName: globalGroupName))
                {//проверяем есть ли вообще звонки в базе
                    string EnglishDay = "";
                    int maxCalls = 0;
                    if (!weekDays.TryGetValue(DateTime.Today.ToString("dddd"), out EnglishDay))
                    {
                        EnglishDay = DateTime.Today.ToString("dddd");
                        if (weekDays.Values.ToList().IndexOf(EnglishDay) == -1)
                        {
                            await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]", replyMarkup: Keyboards.cancel);
                            return;
                        }
                    }

                    sqlConnection.Open();
                    SqlCommand commandSch = new($"SELECT TOP(1) PERCENT Id_lesson FROM [{globalGroupName}_{EnglishDay}_Schedule] ORDER BY Id_lesson DESC", sqlConnection);
                    SqlDataReader readerSch = commandSch.ExecuteReader();
                    if (readerSch.Read())
                    {
                        if (!readerSch.IsDBNull(readerSch.GetOrdinal("Id_lesson")))
                        {
                            maxCalls = (int)readerSch["Id_lesson"];
                        }
                    }
                    sqlConnection.Close();

                    if (maxCalls == 0)
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"⏲По расписанию у вашей группы сегодня _нет_ пар ❌ \\[3002\\]", parseMode: ParseMode.MarkdownV2);
                        return;
                    }

                    string[] schedule = Array.Empty<string>();
                    using (SqlConnection connection = new(connectionString))
                    {
                        await connection.OpenAsync();
                        SqlCommand command = new($"SELECT time_interval FROM [{globalGroupName}_Calls] ORDER BY lesson_number", connection);
                        SqlDataReader reader = await command.ExecuteReaderAsync();

                        int countCalls = 0;
                        while (await reader.ReadAsync())
                        {
                            countCalls++;
                            if (countCalls > maxCalls)
                            {
                                break;
                            }
                            string timeInterval = reader.GetString(0);
                            timeInterval = await RemoveDigitsAsync(timeInterval);
                            timeInterval = timeInterval.Remove(timeInterval.Length - 2).Trim();
                            Array.Resize(ref schedule, schedule.Length + 1);
                            schedule[schedule.Length - 1] = timeInterval;
                            //schedule = await AddToArray(schedule, timeInterval);
                        }
                        reader.Close();
                    }

                    if (schedule.Length == 0)
                    {
                        await botClient.SendTextMessageAsync(chatId: globalChatId,
                        text: "⏲Расписания звонков пока _нет_ ❌ \\[3002\\]", parseMode: ParseMode.MarkdownV2);
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
                                if (timeUntilNextClass.TotalHours > 5)
                                {
                                    await botClient.SendTextMessageAsync(chatId: globalChatId,
                                    text: $"⏱*До начала пар осталось более 5 часов, а точнее:* {await EscapeMarkdownV2(formattedTime)}", parseMode: ParseMode.MarkdownV2);
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(chatId: globalChatId,
                                    text: $"⏱*До начала следующей пары осталось:* {await EscapeMarkdownV2(formattedTime)}", parseMode: ParseMode.MarkdownV2);
                                }
                                return;
                            }
                        }
                    }
                    await botClient.SendTextMessageAsync(chatId: globalChatId,
                        text: "*❗️По расписанию у вашей группы сейчас _нет_ занятий❗️*", parseMode: ParseMode.MarkdownV2);
                    return;
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId: globalChatId,
                    text: "⏲Расписания звонков пока _нет_ ❌ \\[3002\\]", parseMode: ParseMode.MarkdownV2);
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
                    globalKeyMenu = "homeTasks";
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
                if (await dataAvailability(globalUserId, "Admins 1") || await dataAvailability(globalUserId, "Admins 2"))
                {
                    await botClient.SendTextMessageAsync(
                    chatId: globalChatId,
                    text: "🎛Выберите действие",
                    replyMarkup: Keyboards.schedule);
                    globalKeyMenu = "schedule";
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
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (await buttonTest())
                {
                    if (!await ReturnGroupName())
                    {
                        return;
                    }
                    if (await dataAvailability(globalUserId, "Admins 1") || await dataAvailability(globalUserId, "Admins 2"))
                    {
                        var messageResDel = await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                            text: "✏️Для добавления звонков введите их в данном формате:\n" +
                                                            "*Начало пары \\- Конец пары* `☰` *_Комментарий_* _\\(не обязательно\\)_\n" +
                                                            "' ' \\(_*Перенос строки \\- обязательный разделитель*_\\)\n" +
                                                            "*Начало пары \\- Конец пары* `☰` *_Комментарий_* _\\(не обязательно\\)_\n" +
                                                            "' ' \\(_*Перенос строки \\- обязательный разделитель*_\\)\n" +
                                                            "_И т\\.д\\. сколько нужно_" +
                                                            "\n\n" +
                                                            "_`☰` \\- является *Не* обязательным разделителем\\._" +
                                                            "\n\n" +
                                                            "Т\\.е выглядеть будет так \\(пример\\):\n" +
                                                            "09:00 \\- 10:20 `☰` *_Комментарий_*\n" +
                                                            "10:30 \\- 11:50 `☰` *_Комментарий_*\n" +
                                                            "12:30 \\- 13:50 `☰` *_Комментарий_*\n" +
                                                            "14:00 \\- 15:20 `☰` *_Комментарий_*",
                                                            replyMarkup: Keyboards.cancel, parseMode: ParseMode.MarkdownV2);
                        globalDeleteMessageCancelId = messageResDel.MessageId;

                        pressingButtons["changeCalls"] = true;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    }
                }
                return;
            }
            #endregion
            #region Кнопка Удалить звонки
            if (message.Text.Trim() == "🗑Удалить звонки")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (await buttonTest())
                {
                    if (!await ReturnGroupName())
                    {
                        return;
                    }
                    if (await dataAvailability(globalUserId, "Admins"))
                    {
                        string[] calls = await CallsInfo(globalGroupName);

                        if (Convert.ToInt32(calls[1]) > 3)
                        {
                            var messageRes = await botClient.SendTextMessageAsync(chatId: globalChatId, text: calls[0], parseMode: ParseMode.MarkdownV2);
                            globalCullsInfoEditId = messageRes.MessageId;
                            var messageResDel = await botClient.SendTextMessageAsync(chatId: globalChatId,
                                text: "🗑Подтвердите удаление звонков!", replyMarkup: Keyboards.confirmation);
                            globalDeleteMessageCancelId = messageResDel.MessageId;
                            globalConfirmValue = "CallsDelete";
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3002]: Звонки уже отсутствуют в базе данных...");
                            return;
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом [3002]+[4003]");
                    }
                }
                return;
            }
            #endregion
            #region Кнопка Изменить расписание
            if (message.Text.Trim() == "✏️Изменить расписание")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (await buttonTest())
                {
                    if (!await ReturnGroupName())
                    {
                        return;
                    }
                    if (await dataAvailability(globalUserId, "Admins 1") || await dataAvailability(globalUserId, "Admins 2"))
                    {
                        var messageResDel = await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                            text: "✏️Для добавления расписания введите его в данном формате:\n" +
                                                            "*День недели*\n" +
                                                            "' ' \\(_*Перенос строки \\- обязательный разделитель*_\\)\n" +
                                                            "*Название пары* `☰` *_Код аудитории_*\n" +
                                                            "' ' \\(_*Перенос строки \\- обязательный разделитель*_\\)\n" +
                                                            "*Название пары* `≣` _*Пара по Знаменателю*_ `☰` *_Код аудитории_* *_Еще Код аудитории_*\n" +
                                                            "' ' \\(_*Перенос строки \\- обязательный разделитель*_\\)\n" +
                                                            "_И т\\.д\\. сколько нужно_" +
                                                            "\n\n" +
                                                            "_Перенос строки_ и `☰` \\- являются обязательными разделителями\n" +
                                                            "`≣` и '` `'\\- является _необязательным_ разделителем для пар по *Знаменателю*\n" +
                                                            "Обратите внимание что пробел '` `' разделяет два разных _Кода аудитории_\n" +
                                                            "Так\\-же нужно обязательно писать *'`Ничего`'* или *'`\\-\\-\\-`'*, если иногда по числителю или знаменателю нет пары" +
                                                            "\n\n" +
                                                            "Т\\.е выглядеть будет так \\(пример\\):\n" +
                                                            "*Понедельник*\n" +
                                                            "*Программирование* `☰` *_М/1_*\n" +
                                                            "*Теория вероятности* `≣` *Математика* `☰` *_207_* *_105_*\n" +
                                                            "*Практика* `≣` *Ничего* `☰` *_Вц/5_*",
                                                            replyMarkup: Keyboards.cancel, parseMode: ParseMode.MarkdownV2);
                        globalDeleteMessageCancelId = messageResDel.MessageId;

                        pressingButtons["changeSchedule"] = true;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    }
                }
                return;
            }
            #endregion
            #region Удалить Расписание
            if (message.Text.Trim() == "🗑Удалить расписание")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (await buttonTest())
                {
                    if (!await ReturnGroupName())
                    {
                        return;
                    }
                    if (await dataAvailability(globalUserId, "Admins 1") || await dataAvailability(globalUserId, "Admins 2"))
                    {
                        var messageResDel = await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                            text: $"✂️Для удаления расписания введите *День недели* или введите *ВСЕ* для удаления всего расписания",
                                                            parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.cancel);
                        globalDeleteMessageCancelId = messageResDel.MessageId;

                        pressingButtons["deleteSchedule"] = true;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    }
                }
                return;
            }
            #endregion

            #region Кнопка Добавить ДЗ
            if (message.Text.Trim() == "➕Добавить ДЗ")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (await buttonTest())
                {
                    if (!await ReturnGroupName())
                    {
                        return;
                    }
                    if (await dataAvailability(globalUserId, "Admins"))
                    {
                        if (await ReturnDayWeek(true))
                        {
                            await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]: Невозможно получить английское название дня недели для русского дня");
                            return;
                        }

                        string text = await DzInfoAdd(globalGroupName);
                        if (text == "ОШИБКА")
                        {
                            return;
                        }
                        bool FTres = bool.Parse(text.Split('☰')[1].Trim());
                        var messageRes = await botClient.SendTextMessageAsync(chatId: globalChatId, text: text.Split('☰')[0].Trim(), parseMode: ParseMode.MarkdownV2);
                        globalDzInfoAdd = messageRes.MessageId;
                        if (FTres)
                        {
                            await botClient.SendTextMessageAsync(chatId: globalChatId, text: "✅Все дз заполнено, для редактирования перейдите к кнопке \"✏️Редактировать ДЗ\"");
                        }
                        else
                        {
                            var messageResDel = await botClient.SendTextMessageAsync(chatId: globalChatId,
                                text: "✏️Для добавления ДЗ введите *ID урока* или скопируйте его из сообщения выше⬆️\n" +
                                "_*Вводите в данном формате:*_ ID урока _пробел_ ТекстДЗ\n" +
                                "*Пример:* _2 Тут написан текст ДЗ_\n" +
                                "_пробел_ \\- является обязательным разделителем\\!\n" +
                                "_Если нет пар или Дз, выберите ↓ *\\(Текущее Дз будет удалено\\!\\)*_",
                                parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.noDz);
                            globalDeleteMessageCancelId = messageResDel.MessageId;
                            pressingButtons["addHomework"] = true;
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом [3002]+[4003]");
                    }
                }
                return;
            }
            #endregion
            #region Кнопка Редактировать ДЗ
            if (message.Text.Trim() == "✏️Редактировать ДЗ" || message.Text.Trim().ToLower() == "/fast_edit_dz")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                pressingButtons["addHomework"] = false;//для быстрого перехода от добавления до редактирования
                if (await buttonTest())
                {
                    if (!await ReturnGroupName())
                    {
                        return;
                    }
                    if (await dataAvailability(globalUserId, "Admins"))
                    {
                        if (await ReturnDayWeek(true))
                        {
                            await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]: Невозможно получить английское название дня недели для русского дня");
                            return;
                        }

                        string text = await DzInfoEdit(globalGroupName);
                        if (text == "ОШИБКА")
                        {
                            return;
                        }

                        var messageRes = await botClient.SendTextMessageAsync(chatId: globalChatId, text: text, parseMode: ParseMode.MarkdownV2);
                        globalDzInfoEdit = messageRes.MessageId;
                        if (message.Text.Trim() == "/fast_edit_dz")
                        {
                            messageRes = await botClient.SendTextMessageAsync(chatId: globalChatId, text: "✏️Для добавления Комментария/редактирования введите *ID урока* или скопируйте его из сообщения выше⬆️\n" +
                                  "_*Вводите в данном формате:\n*_ ID урока \\(вероятнее всего всегда 1\\) _пробел_ КомментарийДЗ\n" +
                                  "*Пример:* _1 Тут написан комментарий к ДЗ_\n" +
                                  "_пробел_ \\- является обязательным разделителем\\!\n",
                                  parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.cancel);
                        }
                        else
                        {
                            messageRes = await botClient.SendTextMessageAsync(chatId: globalChatId, text: "✏️Для изменения ДЗ введите *ID урока* или скопируйте его из сообщения выше⬆️\n" +
                                   "_*Вводите в данном формате:*_ ID урока _пробел_ ТекстДЗ\n" +
                                   "*Пример:* _2 Тут написан текст ДЗ_\n" +
                                   "_пробел_ \\- является обязательным разделителем\\!\n" +
                                   "_Если нет пар или Дз, выберите ↓ *\\(Текущее Дз будет удалено\\!\\)*_",
                                   parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.noDz);
                        }
                        globalDeleteMessageCancelId = messageRes.MessageId;
                        pressingButtons["changeHomework"] = true;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом [3002]+[4003]");
                    }
                }
                return;
            }
            #endregion
            #region Кнопка Удалить ДЗ
            if (message.Text.Trim() == "🗑Удалить ДЗ")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (await buttonTest())
                {
                    if (!await ReturnGroupName())
                    {
                        return;
                    }
                    if (await dataAvailability(globalUserId, "Admins"))
                    {
                        if (await ReturnDayWeek(true))
                        {
                            await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]: Невозможно получить английское название дня недели для русского дня");
                            return;
                        }

                        if (await dataAvailability(0, "Homework ?", groupName: globalGroupName))
                        {
                            string text = await DzInfoEdit(globalGroupName);
                            if (text == "ОШИБКА")
                            {
                                return;
                            }

                            var messageRes = await botClient.SendTextMessageAsync(chatId: globalChatId, text: text, parseMode: ParseMode.MarkdownV2);
                            globalDzInfoEdit = messageRes.MessageId;

                            var messageResDel = await botClient.SendTextMessageAsync(chatId: globalChatId, text: "✏️Для удаления ДЗ введите *ID урока* или скопируйте его из сообщения выше⬆️\n" +
                                "Для удаления всего дз введите \"ВСЕ\"\n" +
                                "_Если нет пар или Дз, выберите ↓ *\\(Текущее Дз будет удалено\\!\\)*_",
                                parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.noDz);
                            globalDeleteMessageCancelId = messageResDel.MessageId;
                            pressingButtons["deleteHomework"] = true;
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3002]: ДЗ уже отсутствует в базе данных...");
                            return;
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом [3002]+[4003]");
                    }
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
                    globalKeyMenu = "administratorManagement";
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
                    globalKeyMenu = "database";
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                }
                return;
            }
            #endregion

            #region Кнопка Добавить Админа
            if (message.Text.Trim() == "➕Добавить Админа")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (await buttonTest())
                {
                    if (!await ReturnGroupName())
                    {
                        return;
                    }
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
                return;
            }
            #endregion
            #region Кнопка Удалить Админа
            if (message.Text.Trim() == "🗑Удалить Админа")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (await buttonTest())
                {
                    if (!await ReturnGroupName())
                    {
                        return;
                    }
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
                return;
            }
            #endregion

            #region Кнопка Информация о Админах
            if (message.Text.Trim() == "👨‍💻Информация о Админах")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (!await ReturnGroupName())
                {
                    return;
                }

                string request;
                if (await dataAvailability(globalUserId, "Admins 1"))
                {
                    request = "SELECT * FROM Admins";
                }
                else if (await dataAvailability(globalUserId, "Admins 2"))
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"🔐У вас 2 тип админа, поэтому вы можете смотреть данные только о пользователях из своей группы или у которых её нет! ~[4003]");
                    request = $"SELECT Admins.* FROM Admins INNER JOIN Users ON Admins.user_id = Users.user_id WHERE (Users.user_group = N'{globalGroupName}') OR (Users.user_group = 'НетГруппы')";
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    return;
                }

                // открытие соединения
                sqlConnection.Open();
                // создание объекта команды SQL
                using (SqlCommand command = new(request, sqlConnection))
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
                    var messId = await botClient.SendTextMessageAsync(globalChatId, "👨‍💻*Информация о админах:* \n" + tableString, parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.tablePicture);

                    globalTablePicture = new object[] { 4, count, "Admins" };
                    await ReturnNameImgMessage(globalTablePicture, messId.MessageId);
                }
                sqlConnection.Close();

                return;
            }
            #endregion
            #region Кнопка Информация о Пользователях
            if (message.Text.Trim() == "👤Информация о Пользователях")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (!await ReturnGroupName())
                {
                    return;
                }

                string request;
                if (await dataAvailability(globalUserId, "Admins 1"))
                {
                    request = "SELECT user_id, username, user_group FROM Users";
                }
                else if (await dataAvailability(globalUserId, "Admins 2"))
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"🔐У вас 2 тип админа, поэтому вы можете смотреть данные только о пользователях из своей группы или у которых её нет! ~[4003]");
                    request = $"SELECT user_id, username, user_group FROM Users WHERE (user_group = N'{globalGroupName}') OR (user_group = N'НетГруппы')";
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    return;
                }

                // открытие соединения
                sqlConnection.Open();
                // создание объекта команды SQL
                using (SqlCommand command = new(request, sqlConnection))
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
                    var messId = await botClient.SendTextMessageAsync(globalChatId, "👥*Информация о пользователях:* \n" + tableString, parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.tablePicture);

                    globalTablePicture = new object[] { 3, count, "Users" };
                    await ReturnNameImgMessage(globalTablePicture, messId.MessageId);
                }
                sqlConnection.Close();

                return;
            }
            #endregion

            #region Управление новостями
            if (message.Text.Trim() == "📰Управление новостями")
            {
                if (await dataAvailability(globalUserId, "Admins 1") /*|| await dataAvailability(globalUserId, "Admins 2")*/)
                {
                    await botClient.SendTextMessageAsync(
                    chatId: globalChatId,
                    text: "🎛Выберите действие",
                    replyMarkup: Keyboards.News);
                    globalKeyMenu = "News";
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
                                                        text: "👾*Приветствую, _администратор_\\!*",
                                                        replyMarkup: Keyboards.botManagement, parseMode: ParseMode.MarkdownV2);
                    globalKeyMenu = "botManagement";
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003].\n" +
                        $"📈Для входа в это меню нужно обладать правами администратора не менее 1 типа.");
                }
                return;
            }
            #endregion

            #region Кнопка Добавить новость
            if (message.Text.Trim() == "➕Добавить новость")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (await buttonTest())
                {
                    if (await dataAvailability(globalUserId, "Admins 1") /*|| await dataAvailability(globalUserId, "Admins 2")*/)
                    {
                        var messageResDel = await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                            text: "✏️Для добавления новости введите её в данном формате:\n" +
                                                            "_Номер/IdНовости_ \\(_*обязательно число*_\\)\n" +
                                                            "' ' \\(_*Перенос строки \\- обязательный разделитель*_\\)\n" +
                                                            "_Название новости_ \\(_*длиной до 150 символов*_\\)\n" +
                                                            "' ' \\(_*Перенос строки \\- обязательный разделитель*_\\)\n" +
                                                            "_Текст новости_ \\(_*длиной до 500 символов*_\\)\n" +
                                                            "\n\n" +
                                                            "Т\\.е выглядеть будет так \\(пример\\):\n" +
                                                            "*11*\n" +
                                                            "*Название новости11*\n" +
                                                            "*Текст новости номер11*",
                                                            replyMarkup: Keyboards.cancel, parseMode: ParseMode.MarkdownV2);
                        globalDeleteMessageCancelId = messageResDel.MessageId;

                        pressingButtons["addingNews"] = true;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    }
                }
                return;
            }
            #endregion
            #region Кнопка Удалить новость
            if (message.Text.Trim() == "🗑Удалить новость")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (await buttonTest())
                {
                    if (await dataAvailability(globalUserId, "Admins 1") /*|| await dataAvailability(globalUserId, "Admins 2")*/)
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
                return;
            }
            #endregion
            #region Кнопка Редактировать новость
            if (message.Text.Trim() == "✏️Редактировать новость")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (await buttonTest())
                {
                    if (await dataAvailability(globalUserId, "Admins 1") /*|| await dataAvailability(globalUserId, "Admins 2")*/)
                    {
                        var messageResDel = await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                            text: "✏️Для редактирования новости введите её в данном формате:\n" +
                                                            "_Номер/IdНовости_ \\(_*обязательно число*_\\)\n" +
                                                            "' ' \\(_*Перенос строки \\- обязательный разделитель*_\\)\n" +
                                                            "_Название новости_ \\(_*длиной до 150 символов*_\\)\n" +
                                                            "' ' \\(_*Перенос строки \\- обязательный разделитель*_\\)\n" +
                                                            "_Текст новости_ \\(_*длиной до 500 символов*_\\)\n" +
                                                            "\n\n" +
                                                            "Т\\.е выглядеть будет так \\(пример\\):\n" +
                                                            "*11*\n" +
                                                            "*Название новости11*\n" +
                                                            "*Текст новости номер11*" +
                                                            "_*Если вы хотите изменить фото новости введите*_ /update_photo_news",
                                                            replyMarkup: Keyboards.cancel, parseMode: ParseMode.MarkdownV2);
                        globalDeleteMessageCancelId = messageResDel.MessageId;

                        pressingButtons["changeNews"] = true;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    }
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
                                                            text: "✏️Введите *Текст*, который будет отправлен всем:",
                                                            replyMarkup: Keyboards.cancel, parseMode: ParseMode.MarkdownV2);
                        pressingButtons["messageTextEveryone"] = true;
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                    }
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
                        var messageResDel = await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                            text: "❓Подтвердите *Выключение*",
                                                            replyMarkup: Keyboards.confirmation, parseMode: ParseMode.MarkdownV2);
                        globalDeleteMessageCancelId = messageResDel.MessageId;
                        globalConfirmValue = "ShutdownBot";
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен");
                    }
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
                        globalKeyMenu = "lockUnlockUsers";
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен");
                    }
                }
                return;
            }
            #endregion

            #region Заблокировать пользователя
            if (message.Text.Trim() == "🔐Заблокировать")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (await buttonTest())
                {
                    if (!await ReturnGroupName())
                    {
                        return;
                    }
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
                return;
            }
            #endregion
            #region Разблокировать пользователя
            if (message.Text.Trim() == "🔓Разблокировать")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                if (await buttonTest())
                {
                    if (!await ReturnGroupName())
                    {
                        return;
                    }
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
                return;
            }
            #endregion

            #region Кнопка Назад
            if (message.Text.Trim() == "◀️Назад")//просто переходит назад в меню по текущему меню в котором сейчас находимся
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                var replyMarkup = Keyboards.MainMenu;
                switch (globalKeyMenu)
                {
                    case "schedules":
                    case "MainMenu":
                    case "Menu1":
                    case "administrator":
                        replyMarkup = Keyboards.MainMenu;
                        globalKeyMenu = "MainMenu";
                        break;
                    case "database":
                    case "administratorManagement":
                    case "Moderator":
                        replyMarkup = Keyboards.HelpAdmin;
                        globalKeyMenu = "HelpAdmin";
                        break;
                    case "botManagement":
                    case "News":
                        replyMarkup = Keyboards.Moderator;
                        globalKeyMenu = "Moderator";
                        break;
                    case "HelpAdmin":
                    case "homeTasks":
                    case "schedule":
                        replyMarkup = Keyboards.administrator;
                        globalKeyMenu = "administrator";
                        break;
                    case "lockUnlockUsers":
                        replyMarkup = Keyboards.botManagement;
                        globalKeyMenu = "botManagement";
                        break;
                    default:
                        replyMarkup = Keyboards.MainMenu;
                        globalKeyMenu = "MainMenu";
                        break;
                }
                await botClient.SendTextMessageAsync(globalChatId, "⤴️Переход назад...", replyMarkup: replyMarkup);

                await Cancel(false, false);
                return;
            }
            #endregion

            #region Изменения имени
            if (pressingButtons["changeNameFT"] && message.Text.Trim().ToLower() != "/cancel")//изменение имени через кнопку
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                sqlConnection.Close();
                string userName = message.Text.Trim();
                if (string.IsNullOrEmpty(userName))
                {
                    userName = "Теперь у вас нет имени...";
                    await botClient.SendTextMessageAsync(globalChatId, "❌Ошибка [4002]: Вы ввели имя некорректно!");
                    return;
                }
                else if (await dataAvailability(0, "Users + Name", userName))
                {
                    await botClient.SendTextMessageAsync(globalChatId, "❌Ошибка [3003]: Данное имя пользователя уже находится в базе данных!\n" +
                        "Пожалуйста выберите другое!");
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
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                sqlConnection.Close();
                int adminRang = 3;
                string username = "Имя";
                if (long.TryParse(message.Text.Trim(), out long userId))//проверяем можно ли сообщение преобразовать в int
                {
                    // обработка id пользователя
                    var user = userId;
                    if (await dataAvailability(user, "Users + Group", groupName: globalGroupName))
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
                         text: "❌Ошибка [3002]: Введенного ID Пользователя не обнаружено, проверьте правильность введённых данных!\n" +
                         "Учтите, что у пользователя должна быть выбрана правильная группа, которая совпадает с вашей!", replyMarkup: Keyboards.cancel);
                        return;
                    }
                }
                else
                {
                    // обработка имени пользователя если не удалось преобразовать в int
                    var user = message.Text.Trim();
                    if (user == "СтандартИмя" || user == "НетИмени")
                    {
                        await botClient.SendTextMessageAsync(
                             chatId: globalChatId,
                             text: "❌Ошибка [3004]: У введенного пользователя обязательно должно быть выбрано имя отличающееся от стандартного!", replyMarkup: Keyboards.cancel);
                        pressingButtons["addAdmin"] = false;
                        return;
                    }
                    if (await dataAvailability(0, "Users + Name + Group", user, globalGroupName))//проверяем есть ли чел
                    {
                        if (await dataAvailability(0, "Admins + Name", user))
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
                         text: "❌Ошибка [3002]: Введенного имени Пользователя не обнаружено, проверьте правильность введённых данных!\n" +
                         "Учтите, что у пользователя должна быть выбрана правильная группа, которая совпадает с вашей!", replyMarkup: Keyboards.cancel);
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
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                sqlConnection.Close();
                int rowsAffected = 0;
                if (long.TryParse(message.Text.Trim(), out long userId))//проверяем можно ли сообщение преобразовать в int
                {
                    // обработка id пользователя
                    var user = userId;
                    if (await dataAvailability(user, "Admins + Group", groupName: globalGroupName))
                    {
                        if (await dataAvailability(user, "Admins 1"))
                        {
                            await botClient.SendTextMessageAsync(
                             chatId: globalChatId,
                             text: "❌Ошибка \\[4003\\]: Введенный ID Админа является админом 1 типа и его нельзя удалить обычным способом\\.\n" +
                             "_Для его удаления обратитесь к Главному администратору:\n *👥Контакты*_", parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.cancel);
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
                         text: "❌Ошибка [3002]: Введенного ID Админа не обнаружено, проверьте правильность введённых данных!\n" +
                         "Учтите, что у Админа должна быть выбрана правильная группа, которая совпадает с вашей!", replyMarkup: Keyboards.cancel);
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
                    if (user == "СтандартИмя" || user == "НетИмени")
                    {
                        await botClient.SendTextMessageAsync(
                             chatId: globalChatId,
                             text: "❌Ошибка [3004]: У введенного пользователя обязательно должно быть выбрано имя отличающееся от стандартного!", replyMarkup: Keyboards.cancel);
                        pressingButtons["deleteAdmin"] = false;
                        return;
                    }
                    if (await dataAvailability(0, "Admins + Name + Group", user, globalGroupName))
                    {
                        if (await dataAvailability(0, "Admins 1 + Name", user))
                        {
                            await botClient.SendTextMessageAsync(
                             chatId: globalChatId,
                             text: "❌Ошибка \\[4003\\]: Введенное имя Админа является именем админа 1 типа и его нельзя удалить обычным способом\\.\n" +
                             "_Для его удаления обратитесь к Главному администратору:\n *👥Контакты*_", parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.cancel);
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
                         text: "❌Ошибка [3002]: Введенного имени Админа не обнаружено, проверьте правильность введённых данных!\n" +
                         "Учтите, что у пользователя должна быть выбрана правильная группа, которая совпадает с вашей!", replyMarkup: Keyboards.cancel);
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
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

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
                                if (await dataAvailability(adminId, "Admins + Group", groupName: globalGroupName))
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
                                     text: "❌Ошибка [3002]: Введенного ID Админа не обнаружено, проверьте правильность введённых данных!\n" +
                                       "Учтите, что у пользователя должна быть выбрана правильная группа, которая совпадает с вашей!", replyMarkup: Keyboards.cancel);
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
                    if (userName == "СтандартИмя" || userName == "НетИмени")
                    {
                        await botClient.SendTextMessageAsync(
                             chatId: globalChatId,
                             text: "❌Ошибка [3004]: У введенного пользователя обязательно должно быть выбрано имя отличающееся от стандартного!", replyMarkup: Keyboards.cancel);
                        pressingButtons["changeAdminType"] = false;
                        return;
                    }
                    try//если не получается правильно поделить по пробелам
                    {
                        if (int.TryParse(message.Text.Trim().Split(' ')[1], out adminType))//если вторая часть является int
                        {
                            if (mass.Contains(adminType))//проверяем диапазон
                            {
                                if (await dataAvailability(0, "Admins + Name + Group", userName, globalGroupName))
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
                                     text: "❌Ошибка [3002]: Введенного имени Админа не обнаружено, проверьте правильность введённых данных!\n" +
                                        "Учтите, что у пользователя должна быть выбрана правильная группа, которая совпадает с вашей!", replyMarkup: Keyboards.cancel);
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
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                sqlConnection.Close();
                int tableIdNews = 0;
                byte[] image;
                string newsTitle = "Название новости", newsText = "Текст новости ";
                string[] newsMs = message.Text.Trim().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                try//делим сообщение по разделителям и проверяем получается ли правильно поделить
                {
                    newsTitle = newsMs[1];
                    newsText = newsMs[2];
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
                if (int.TryParse(newsMs[0], out int idNews))//проверяем можно ли преобразовать в int
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
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                sqlConnection.Close();
                string newsTitle = "Название новости", newsText = "Текст новости ";
                string[] newsMs = message.Text.Trim().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                try//делим сообщение по разделителям и проверяем получается ли правильно поделить
                {
                    newsTitle = newsMs[1];
                    newsText = newsMs[2];
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
                if (int.TryParse(newsMs[0], out int idNews))//проверяем можно ли преобразовать в int
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
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

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
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

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
            #region Изменить звонки
            if (pressingButtons["changeCalls"] && message.Text.Trim().ToLower() != "/cancel")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                await clearingTables($"[{globalGroupName}_Calls]");
                sqlConnection.Close();
                sqlConnection.Open();
                string[] timeMass;
                int idCulls = 0;
                string comment, Time;
                try//делим сообщение по разделителям и проверяем получается ли правильно поделить
                {
                    timeMass = message.Text.Trim().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                }
                catch
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Текст введен некорректно", replyMarkup: Keyboards.cancel);
                    return;
                }
                if (timeMass.Length <= 3)
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]/[4002]: Необходимо добавить более 3х записей", replyMarkup: Keyboards.cancel);
                    return;
                }
                for (int i = 0; i < timeMass.Length; i++)
                {
                    idCulls++;
                    try//делим сообщение по разделителям и проверяем получается ли правильно поделить, т.е проверяем есть ли комментарий или нет
                    {
                        Time = timeMass[i].Trim().Split('☰')[0].Trim();
                        comment = timeMass[i].Trim().Split('☰')[1].Trim();
                        if (string.IsNullOrWhiteSpace(comment))
                        {
                            await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [4002]: Введенный комментарий является пустым", replyMarkup: Keyboards.cancel);
                            return;
                        }
                    }
                    catch
                    {
                        Time = timeMass[i].Trim();
                        comment = " ";
                    }
                    if (Regex.IsMatch(Time.Trim(), @"^([01]?\d|2[0-3]):[0-5]\d(-|(\s-\s)?|(\s-)?|(-\s)?)([01]\d|2[0-3]):[0-5]\d$"))//проверяем на соответствие формату времени: 00:00 - 00:00
                    {
                        if (Regex.IsMatch(Time.Trim(), @"^([01]?\d|2[0-3]):[0-5]\d(-|\s-|-\s)([01]\d|2[0-3]):[0-5]\d$"))//если у - не точно 2 пробела с двух сторон
                        {
                            Time = Time.Replace(" -", " ⌁ ").Replace("- ", " ⌁ ").Replace("-", " ⌁ ");
                        }
                        else//меняем 00:00 - 00:00 на 00:00 ⌁ 00:00
                        {
                            Time = Time.Replace("-", "⌁");
                        }
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
                        SqlCommand insertCommand = new($"INSERT into [{globalGroupName}_Calls](time_interval, note) values (@time_interval, @note)", sqlConnection);//добавляем
                        insertCommand.Parameters.AddWithValue("@time_interval", Time);
                        insertCommand.Parameters.AddWithValue("@note", comment);
                        insertCommand.ExecuteNonQuery();
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1004]: Введенный текст не соответствует формату времени: 00:00 - 00:00\n" +
                            $"Более подробно Regex:\n ^([01]\\d|2[0-3]):[0-5]\\d(-|(\\s-\\s)?|(\\s-)?|(-\\s)?)([01]\\d|2[0-3]):[0-5]\\d$", replyMarkup: Keyboards.cancel);
                        return;
                    }
                }
                sqlConnection.Close();
                await botClient.SendTextMessageAsync(globalChatId, $"✅Звонки успешно добавлены!");
                await DzBaseChat(globalGroupName, true);
                pressingButtons["changeCalls"] = false;
                return;
            }
            #endregion
            #region Изменить расписание
            if (pressingButtons["changeSchedule"] && message.Text.Trim().ToLower() != "/cancel")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

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
                    scheduleMass = message.Text.Trim().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
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
                await FullDeleteDz(globalGroupName);
                await clearingTables($"[{globalGroupName}_{EnglishDay}_Schedule]");
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
                                await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Введено больше 2 уроков для определенного времени в расписании", replyMarkup: Keyboards.cancel, parseMode: ParseMode.MarkdownV2);
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
                            await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1001]: Максимальное количество записей: 8", replyMarkup: Keyboards.cancel);
                            return;
                    }

                    SqlCommand insertCommand = new($"INSERT into [{globalGroupName}_{EnglishDay}_Schedule](lesson_name, audience_code) values (@lesson_name, @audience_code)", sqlConnection);
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
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

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
                            rowsAffected += await FullDeleteDz(globalGroupName);
                            rowsAffected += await clearingTables($"[{globalGroupName}_{EnglishDay}_Schedule]");
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

                    rowsAffected += await FullDeleteDz(globalGroupName);
                    rowsAffected += await clearingTables($"[{globalGroupName}_{EnglishDay}_Schedule]");
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
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

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
                    var mes = message.Text.Trim().Split(' ');

                    id = mes[0].Trim();
                    Text = message.Text.Trim().Substring(2).Trim();//обрезать первые 2 символа

                    if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(Text))//если поделились но пустые или специально пустые
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [4002]: Текст или id дз являются пустыми полями", replyMarkup: Keyboards.cancel);
                        return;
                    }
                    if (Text == "^")
                    {
                        Text = "↑…↑";
                    }
                }
                catch
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Текст введен некорректно", replyMarkup: Keyboards.cancel);
                    return;
                }
                if (int.TryParse(id, out int idHomework))//проверяем можно ли сообщение преобразовать в int
                {
                    if (await dataAvailability(idHomework, "Homework", groupName: globalGroupName))
                    {
                        bool ft = false;
                        sqlConnection.Close();
                        sqlConnection.Open();
                        SqlCommand selectCommand;
                        selectCommand = new SqlCommand($"select homework from [{globalGroupName}_Homework] WHERE id_Homework = @idHomework", sqlConnection);
                        selectCommand.Parameters.AddWithValue("@idHomework", idHomework);
                        using (var reader = selectCommand.ExecuteReader())//выполняем проверку и результат записываем в result
                        {
                            if (reader.Read())
                            {
                                try
                                {
                                    string textHomework = reader.GetString(0);
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
                            SqlCommand updateCommand = new($"UPDATE [{globalGroupName}_Homework] SET homework = @homework WHERE id_Homework = @idHomework", sqlConnection);
                            updateCommand.Parameters.AddWithValue("@idHomework", idHomework);
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
                string textDzInfoAdd = await DzInfoAdd(globalGroupName);
                if (textDzInfoAdd == "ОШИБКА")
                {
                    pressingButtons["addHomework"] = false;
                    return;
                }
                if (globalDzInfoAdd == 0)//проверка, можно ли отредактировать сообщение
                {
                    var messageRes = await botClient.SendTextMessageAsync(chatId: globalChatId, text: textDzInfoAdd.Split('☰')[0].Trim(), parseMode: ParseMode.MarkdownV2);
                    globalDzInfoAdd = messageRes.MessageId;
                }
                else
                {
                    try
                    {
                        await botClient.EditMessageTextAsync(globalChatId, globalDzInfoAdd, text: textDzInfoAdd.Split('☰')[0].Trim(), parseMode: ParseMode.MarkdownV2); //Обновляю сообщение
                    }
                    catch { }
                }
                if (globalDzInfoEdit != 0)//проверка, можно ли отредактировать сообщение Редактирования дз если оно было недавно
                {
                    try
                    {
                        string text = await DzInfoEdit(globalGroupName);
                        if (text != "ОШИБКА")
                        {
                            await botClient.EditMessageTextAsync(globalChatId, globalDzInfoEdit, text: text.Trim(), parseMode: ParseMode.MarkdownV2); //Обновляю сообщение
                        }
                    }
                    catch { }
                }
                await ReturnImageDz();
                await DzBaseChat(globalGroupName, true);
                //pressingButtons["addHomework"] = false;
                return;
            }
            #endregion
            #region Редактировать дз 
            if (pressingButtons["changeHomework"] && message.Text.Trim().ToLower() != "/cancel")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

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
                    var mes = message.Text.Trim().Split(' ');

                    id = mes[0].Trim();
                    Text = message.Text.Trim().Substring(2).Trim();//обрезать первые 2 символа

                    if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(Text))//если поделились но пустые или специально пустые
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [4002]: Текст или id дз являются пустыми полями", replyMarkup: Keyboards.cancel);
                        return;
                    }
                    if (Text == "^")
                    {
                        Text = "↑…↑";
                    }
                }
                catch
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Текст введен некорректно", replyMarkup: Keyboards.cancel);
                    return;
                }
                if (int.TryParse(id, out int idHomework))//проверяем можно ли сообщение преобразовать в int
                {
                    if (await dataAvailability(idHomework, "Homework", groupName: globalGroupName))
                    {
                        sqlConnection.Open();
                        SqlCommand updateCommand = new($"UPDATE [{globalGroupName}_Homework] SET homework = @homework WHERE id_Homework = @idHomework", sqlConnection);
                        updateCommand.Parameters.AddWithValue("@idHomework", idHomework);
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
                string textDzInfoEdit = await DzInfoEdit(globalGroupName);
                if (textDzInfoEdit == "ОШИБКА")
                {
                    pressingButtons["changeHomework"] = false;
                    return;
                }
                if (globalDzInfoEdit == 0)//проверка, можно ли отредактировать сообщение
                {
                    var messageRes = await botClient.SendTextMessageAsync(chatId: globalChatId, text: textDzInfoEdit, parseMode: ParseMode.MarkdownV2);
                    globalDzInfoEdit = messageRes.MessageId;
                }
                else
                {
                    try
                    {
                        await botClient.EditMessageTextAsync(globalChatId, globalDzInfoEdit, text: textDzInfoEdit, parseMode: ParseMode.MarkdownV2); //Обновляю сообщение
                    }
                    catch { }
                }
                if (globalDzInfoAdd != 0)//проверка, можно ли отредактировать сообщение Добавления дз если оно было недавно
                {
                    try
                    {
                        string text = await DzInfoAdd(globalGroupName);
                        if (text != "ОШИБКА")
                        {
                            await botClient.EditMessageTextAsync(globalChatId, globalDzInfoAdd, text: text.Split('☰')[0].Trim(), parseMode: ParseMode.MarkdownV2); //Обновляю сообщение
                        }
                    }
                    catch { }
                }
                //pressingButtons["changeHomework"] = false;
                await ReturnImageDz();
                await DzBaseChat(globalGroupName, true);
                return;
            }
            #endregion
            #region Удалить дз
            if (pressingButtons["deleteHomework"] && message.Text.Trim().ToLower() != "/cancel")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

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
                    rowsAffected = await FullDeleteDz(globalGroupName);
                }
                else//по выбранному
                {
                    if (int.TryParse(message.Text.Trim(), out int idHomework))//проверяем можно ли сообщение преобразовать в int
                    {
                        using SqlCommand command = new($"UPDATE [{globalGroupName}_Homework] SET homework = NULL WHERE id_Homework = @idHomework", sqlConnection);
                        command.Parameters.AddWithValue("@idHomework", idHomework);
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
                string textDzInfoEdit = await DzInfoEdit(globalGroupName);
                if (textDzInfoEdit == "ОШИБКА")
                {
                    pressingButtons["deleteHomework"] = false;
                    return;
                }
                if (globalDzInfoEdit == 0)//проверка, можно ли отредактировать сообщение
                {
                    var messageRes = await botClient.SendTextMessageAsync(chatId: globalChatId, text: textDzInfoEdit, parseMode: ParseMode.MarkdownV2);
                    globalDzInfoEdit = messageRes.MessageId;
                }
                else
                {
                    try
                    {
                        await botClient.EditMessageTextAsync(globalChatId, globalDzInfoEdit, text: textDzInfoEdit, parseMode: ParseMode.MarkdownV2); //Обновляю сообщение
                    }
                    catch { }
                }
                if (globalDzInfoAdd != 0)//проверка, можно ли отредактировать сообщение Добавления дз если оно было недавно
                {
                    try
                    {
                        string text = await DzInfoAdd(globalGroupName);
                        if (text != "ОШИБКА")
                        {
                            await botClient.EditMessageTextAsync(globalChatId, globalDzInfoAdd, text: text.Split('☰')[0].Trim(), parseMode: ParseMode.MarkdownV2); //Обновляю сообщение
                        }
                    }
                    catch { }
                }
                pressingButtons["deleteHomework"] = false;
                await ReturnImageDz();
                await DzBaseChat(globalGroupName, true);
                return;
            }
            #endregion
            #region Отправить сообщение всем
            if (pressingButtons["messageTextEveryone"] && message.Text.Trim().ToLower() != "/cancel")
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

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
            #region Выключение бота
            if (pressingButtons["shutdownBot"] && message.Text.Trim().ToLower() != "/cancel")
            {
                if (int.TryParse(message.Text.Trim(), out int shutdownMinut))//проверяем можно ли сообщение преобразовать в int
                {
                    await Cancel(false, false);
                    await messageEveryone("⚠️*Внимание*⚠️\n" +
                        $"Через {shutdownMinut} минут бот перейдет в режим технических работ\\! \\[0000\\]\n" +
                        "_Т\\.е будет выключен на какое\\-то время_ \\[2001\\]");
                    // Создаем объект  таймера
                    await TimerBot(shutdownMinut);

                    pressingButtons["shutdownTimerMinuets"] = true;
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [3001]: Введенное зачение минут не является числом!", replyMarkup: Keyboards.cancel);
                    return;
                }

                pressingButtons["shutdownBot"] = false;
                return;
            }
            #endregion
            #region Текст приветствия
            if (greetings.Contains(message.Text.Trim().ToLower()))//для приветствий, если пользователь поздоровается
            {
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);

                await botClient.SendTextMessageAsync(message.Chat, "Привет-привет!!❤️‍🔥👋");
                return;
            }
            #endregion

            //отправляется тогда, кода не было обработано действие, т.е не было return
            await botClient.SendTextMessageAsync(
                chatId: globalChatId,
                text: "❌Ошибка [1100]: Незарегистрированный текст📜\n");
        }

        async Task ButtonUpdate(string callbackData, CallbackQuery callbackQuery)//обработка действия кнопок под текстом
        {
            //await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"Вы нажали кнопку {callbackData}!");

            if (string.IsNullOrWhiteSpace(callbackData?.Trim())) return;
            if (callbackData == "cancel")//если "отменить" то во всем массиве кнопок делаем их false
            {
                try { await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"Вы 🚫Отмененили действие!", false); } catch { }
                await Cancel(messageDelete: true);
            }
            else if (callbackData.Split(' ')[0].Trim() == "Group")
            {
                await DeleteMessage(globalDeleteMessageCancelId);
                globalDeleteMessageCancelId = 0;

                string userGroup = callbackData.Split(' ')[1].Trim();
                sqlConnection.Close();
                sqlConnection.Open();
                SqlCommand updateCommand = new("UPDATE Users SET user_group = @userGroup WHERE user_id = @userId", sqlConnection);
                updateCommand.Parameters.AddWithValue("@userId", globalChatId);//т.к при нажатии на кнопку под сообщением личное id отличается от стандартного
                updateCommand.Parameters.AddWithValue("@userGroup", userGroup);
                updateCommand.ExecuteNonQuery();
                sqlConnection.Close();
                try { await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"Теперь ваша группа {userGroup}!"); } catch { }
                await botClient.SendTextMessageAsync(globalChatId, $"✅*Группа успешно изменена\\!*\nТеперь ваша группа {await EscapeMarkdownV2(userGroup)}", parseMode: ParseMode.MarkdownV2);
                globalChangeGroupTime = DateTime.Now;
                try
                {
                    if (globalMessageTextId != 0)//проверка, можно ли отредактировать сообщение
                    {
                        string profileText = await userProfile(globalChatId);
                        if (profileText == "ОШИБКА [3002]")
                        {
                            profileText = await userProfile(globalUserId);
                        }
                        await botClient.EditMessageTextAsync(globalChatId, globalMessageTextId, profileText, replyMarkup: Keyboards.Profile, parseMode: ParseMode.MarkdownV2); //Обновляю сообщение
                    }
                }
                catch { }
            }
            else
            {
                if (await buttonTest() || (callbackData == "noPairs" || callbackData == "noDZ"))//проверяем все ли действия выполнены т.е все ли кнопки в массиве false
                {
                    switch (callbackData)//остальные кнопки
                    {
                        case "updateGroup":
                            // Обрабатываем нажатие на кнопку "Изменить группу"
                            TimeSpan elapsedTime = DateTime.Now - globalChangeGroupTime;
                            if (elapsedTime.TotalMinutes > 10 || await dataAvailability(globalChatId, "Admins 1") || await dataAvailability(globalUserId, "Admins 1"))
                            {
                                await ChangeGroup();
                            }
                            else
                            {
                                try { await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"До смены группы осталось: ~{10 - (int)elapsedTime.TotalMinutes}минут!", true); } catch { }
                                await botClient.SendTextMessageAsync(chatId: globalChatId, text: "❌Ошибка [1005]: Группу можно сменить только раз в 10 минут!");
                            }
                            break;
                        case "updateName":
                            // Обрабатываем нажатие на кнопку "Изменить имя"
                            try { await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"Учтите, что имя не должно совпадать с другим человеком!"); } catch { }
                            var messageResDel = await botClient.SendTextMessageAsync(globalChatId, "✏️Введите новое имя _\\(Желательно ник TG, Без @\\)_:", replyMarkup: Keyboards.cancel, parseMode: ParseMode.MarkdownV2);
                            globalDeleteMessageCancelId = messageResDel.MessageId;
                            pressingButtons["changeNameFT"] = true;
                            break;
                        case "addPicture":
                            if (globalNewsId == 0)//если новость для редактирования картинки не нашли
                            {
                                try { await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"❌Ошибка [4002]!"); } catch { }
                                await botClient.SendTextMessageAsync(globalChatId, "❌Ошибка [4002]: Ранее введенной новости не обнаружено.\n" +
                                    "📝Введите номер новости для добавления картинки:");
                                pressingButtons["numberNewsPicture"] = true;
                            }
                            else
                            {
                                try { await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"Учтите, что картинку нужно отправить как фото, а не как файл!", true); } catch { }
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
                        case "noPairs":
                            if (await dataAvailability(globalChatId, "Admins") || await dataAvailability(globalUserId, "Admins"))
                            {
                                try { await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"Выбрано 🚫Отмена пар!"); } catch { }
                                await NoDz("⊗ Отмена Пар");
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом [3002]+[4003]");
                                return;
                            }
                            break;
                        case "noDZ":
                            if (await dataAvailability(globalChatId, "Admins") || await dataAvailability(globalUserId, "Admins"))
                            {
                                try { await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"Выбрано 🚫Нет Дз!"); } catch { }
                                await NoDz("⊗ Нет Дз");
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом [3002]+[4003]");
                                return;
                            }
                            break;
                        case "addComment":
                            await botClient.SendTextMessageAsync(globalChatId, "✏️Для добавления комментария нажмите: /fast_edit_dz");
                            break;
                        case "confirm":
                            switch (globalConfirmValue)
                            {
                                case "CallsDelete":
                                    if (await dataAvailability(globalChatId, "Admins") || await dataAvailability(globalUserId, "Admins"))
                                    {
                                        int rowsAffected = await clearingTables($"[{globalGroupName}_Calls]"); ;//просто удаляем все звонки

                                        try { await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"Звонки удалены!"); } catch { }
                                        await botClient.SendTextMessageAsync(chatId: globalChatId,
                                        text: $"✅Произведено удаление *Звонков*, удалено записей: _{rowsAffected}_",
                                        parseMode: ParseMode.MarkdownV2);

                                        if (globalCullsInfoEditId != 0)//проверка, можно ли отредактировать сообщение
                                        {
                                            try
                                            {
                                                string[] calls = await CallsInfo(globalGroupName);
                                                await botClient.EditMessageTextAsync(globalChatId, globalCullsInfoEditId, text: calls[0], parseMode: ParseMode.MarkdownV2); //Обновляю сообщение
                                            }
                                            catch { }
                                        }

                                        await DeleteMessage(globalDeleteMessageCancelId);
                                        globalDeleteMessageCancelId = 0;
                                    }
                                    else
                                    {
                                        await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом [3002]+[4003]");
                                        return;
                                    }
                                    break;
                                case "ShutdownBot":
                                    try { await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"Действие для выключение бота, ожидание параметра задержки:", true); } catch { }
                                    messageResDel = await botClient.SendTextMessageAsync(chatId: globalChatId,
                                                                   text: "⏱Введите *Время выключения в минутах*\\!", replyMarkup: Keyboards.cancel, parseMode: ParseMode.MarkdownV2);
                                    pressingButtons["shutdownBot"] = true;
                                    await DeleteMessage(globalDeleteMessageCancelId);
                                    globalDeleteMessageCancelId = messageResDel.MessageId;
                                    break;
                                default:
                                    // Обрабатываем неизвестные данные обратного вызова
                                    try { await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"❌Ошибка [1300]!"); } catch { }
                                    await botClient.SendTextMessageAsync(globalChatId, "🚫Действие более недействительно! [1300]");
                                    break;
                            }
                            globalConfirmValue = "";
                            break;
                        case "tablePicture"://обработка таблиц и т.п для отображения картинок
#pragma warning disable CA1416 // Проверка совместимости платформы
                            await botClient.SendChatActionAsync(globalChatId, ChatAction.UploadPhoto);

                            globalTablePicture = await ReturnNameImgMessage(globalTablePicture, callbackQuery.Message.MessageId);

                            int Width = Convert.ToInt32(globalTablePicture[0]), Height = Convert.ToInt32(globalTablePicture[1]);
                            string Table = (string)globalTablePicture[2], Text = "";//для начала принимаем размеры изо и что мы будем рисовать

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

                                if (Table != null && Table != "") try { await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"🔄Ожидайте загрузку изображения {Table}"); } catch { }
                                // открытие соединения
                                sqlConnection.Open();
                                switch (Table)//что мы будем рисовать
                                {
                                    case "Users":
                                        string requestUs;
                                        if (await dataAvailability(globalChatId, "Admins 1") || await dataAvailability(globalUserId, "Admins 1"))
                                        {
                                            requestUs = "SELECT user_id, username, user_group FROM Users";
                                        }
                                        else if (await dataAvailability(globalChatId, "Admins 2") || await dataAvailability(globalUserId, "Admins 2"))
                                        {
                                            await botClient.SendTextMessageAsync(globalChatId, $"🔐У вас 2 тип админа, поэтому вы можете смотреть данные только о пользователях из своей группы или у которых её нет! ~[4003]");
                                            requestUs = $"SELECT user_id, username, user_group FROM Users WHERE (user_group = N'{globalGroupName}') OR (user_group = N'НетГруппы')";
                                        }
                                        else
                                        {
                                            await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                                            return;
                                        }
                                        Text = "👥*Информация о пользователях:*";
                                        // создание объекта команды SQL
                                        sqlConnection.Open();
                                        using (SqlCommand command = new(requestUs, sqlConnection))
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
                                        string requestAd;
                                        if (await dataAvailability(globalChatId, "Admins 1") || await dataAvailability(globalUserId, "Admins 1"))
                                        {
                                            requestAd = "SELECT * FROM Admins";
                                        }
                                        else if (await dataAvailability(globalChatId, "Admins 2") || await dataAvailability(globalUserId, "Admins 2"))
                                        {
                                            await botClient.SendTextMessageAsync(globalChatId, $"🔐У вас 2 тип админа, поэтому вы можете смотреть данные только о пользователях из своей группы или у которых её нет! ~[4003]");
                                            requestAd = $"SELECT Admins.* FROM Admins INNER JOIN Users ON Admins.user_id = Users.user_id WHERE (Users.user_group = N'{globalGroupName}') OR (Users.user_group = 'НетГруппы')";
                                        }
                                        else
                                        {
                                            await botClient.SendTextMessageAsync(globalChatId, $"🔐Увы, но вы не являетесь админом или ваш уровень недостаточен [3002]+[4003]");
                                            return;
                                        }
                                        Text = "👥*Информация о пользователях:*";
                                        // создание объекта команды SQL
                                        sqlConnection.Open();
                                        using (SqlCommand command = new(requestAd, sqlConnection))
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
                                        /*if (!await ReturnGroupName())
                                        {
                                            break;
                                        }*/
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

                                        Text = $"✎Расписание {globalGroupName}";
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
                                                if (await dataAvailability(0, "Schedule ?", EnglishDay, globalGroupName))
                                                {
                                                    // открытие соединения
                                                    sqlConnection.Open();
                                                    // создание команды для выполнения SQL-запроса для чтения данных 
                                                    SqlCommand selectCommand = new($"select * from [{globalGroupName}_{EnglishDay}_Schedule]", sqlConnection);
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
                                        Text = $"✎Расписание {globalGroupName}";//последний текст который будет сверху
                                        graphics.DrawString(Text, font, brush, 130, 150);

                                        Text = "🗓*Расписание на всю неделю:*";//текст под картинкой

                                        break;
                                    case "Calls":
                                        if (!await dataAvailability(0, "Calls ?", groupName: globalGroupName))
                                        {
                                            await botClient.SendTextMessageAsync(chatId: globalChatId,
                                            text: "⏲Расписания звонков пока _нет_ \\[3002\\]", parseMode: ParseMode.MarkdownV2);
                                            return;
                                        }
                                        desiredWidth = Width * 144;
                                        desiredHeight = (Height + 1) * 200;
                                        lengthLine = 50;
                                        image = new Bitmap(desiredWidth, desiredHeight); // обновляю width и height изо
                                        graphics = Graphics.FromImage(image);
                                        graphics.Clear(System.Drawing.Color.FromArgb(30, 30, 30));
                                        font = new Font("Arial", 60, FontStyle.Bold);

                                        graphics.DrawRectangle(pen, lengthLine, lengthLine, desiredWidth - lengthLine * 2, desiredHeight - lengthLine * 2);// Нарисуем границу
                                        graphics.DrawRectangle(pen, lengthLine / 2, lengthLine / 2, desiredWidth - lengthLine, desiredHeight - lengthLine);

                                        string[] calls = await CallsInfo(globalGroupName, true);
                                        calls = calls[0].Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                                        string Calls = "";
                                        textWidth = 90;//изначальный отступ

                                        for (int i = 1; i < calls.Length; i++)
                                        {
                                            Calls = calls[i];//составляем сообщение
                                            textWidth += 120;
                                            brush = new SolidBrush(System.Drawing.Color.Aqua);
                                            graphics.DrawString(Calls, font, brush, 300, textWidth);
                                        }

                                        Text = "🛎*Расписание звонков:*";//текст под картинкой
                                        brush = new SolidBrush(System.Drawing.Color.Green);
                                        font = new Font("Arial", 55, FontStyle.Bold);
                                        graphics.DrawString($"☏Расписание звонков {globalGroupName}", font, brush, 150, 90);//последний текст который будет сверху
                                        break;
                                    case "Homework":
                                        byte[] imgMass = null;
                                        SqlCommand commandImgDz = new("SELECT image_Dz FROM Groups WHERE group_Name = @groupName", sqlConnection);
                                        commandImgDz.Parameters.AddWithValue("@groupName", globalGroupName);
                                        SqlDataReader readerImg = commandImgDz.ExecuteReader();
                                        if (readerImg.Read())
                                        {
                                            // Получаем изо из базы для дз
                                            if (readerImg is not null && !readerImg.IsDBNull(readerImg.GetOrdinal("image_Dz")))
                                            {
                                                imgMass = (byte[])readerImg["image_Dz"];
                                                image = new Bitmap(new MemoryStream(imgMass));
                                            }
                                        }
                                        sqlConnection.Close();

                                        if (imgMass == null)
                                        {
                                            object[] imgObg = await ReturnImageDz();
                                            if ((bool)imgObg[0] == true)
                                            {
                                                imgMass = (byte[])imgObg[3];
                                                image = new Bitmap(new MemoryStream(imgMass));
                                                Text = (string)imgObg[2];
                                            }
                                        }
                                        else
                                        {
                                            if (await dataAvailability(0, "Homework ~ noDz", groupName: globalGroupName))//если нет дз
                                            {
                                                Text = "*💤На завтра нет Дз\\!🎉*";
                                            }
                                            else if (await dataAvailability(0, "Homework ~ noPairs", groupName: globalGroupName))//если нет пар
                                            {
                                                Text = "*💤Завтра нет пар\\!🎉*";
                                            }
                                            else
                                            {
                                                Text = "*🏠Дз на завтра:*";
                                            }
                                        }

                                        if (imgMass == null)//если по итогу нет изо
                                        {
                                            await botClient.SendTextMessageAsync(globalChatId, $"❌*Извините, но дз для дня _\"{RussianDayThen.ToUpper()}\"_ еще не заполнено `[3002]`*", parseMode: ParseMode.MarkdownV2);
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

                                        try { await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"ℹ️Повторите отправку текстового сообщения!", false); }
                                        catch { }
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

#pragma warning restore CA1416 // Проверка совместимости платформы
                            }
                            break;
                        default:
                            // Обрабатываем неизвестные данные обратного вызова
                            await botClient.SendTextMessageAsync(globalChatId, "🚫Незарегистрированная кнопка [1300]");
                            break;
                    }
                }
                else
                {
                    //когда не все кнопки были выполнены
                    try { await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"Вы 🚫Не закончили предыдуще действие!"); } catch { }
                }
            }
        }

        async Task<bool> dataAvailability(long ID, string table, string value = "", string groupName = "")//проверка разных действий из разных таблиц в основном содержится ли в таблице что то
        {
            bool result = false;
            sqlConnection.Close();
            await sqlConnection.OpenAsync();
            SqlCommand selectCommand;
            switch (table)//в зависимости от действия задаем в переменную selectCommand различные команды
            {
                case "Admins + Name":
                    selectCommand = new SqlCommand("SELECT * FROM Admins WHERE username = @userName", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@userName", value);
                    break;
                case "Admins 1 + Name":
                    selectCommand = new SqlCommand("SELECT * FROM Admins WHERE username = @userName AND admin_type = @adminType", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@userName", value);
                    selectCommand.Parameters.AddWithValue("@adminType", 1);
                    break;
                case "Admins":
                    selectCommand = new SqlCommand("SELECT * FROM Admins WHERE user_id = @userId", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@userId", ID);
                    break;
                case "Admins + Group":
                    selectCommand = new SqlCommand("SELECT Admins.* FROM Admins INNER JOIN Users ON Admins.user_id = Users.user_id WHERE (Users.user_group = @groupName) AND (Admins.user_id = @userId)", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@userId", ID);
                    selectCommand.Parameters.AddWithValue("@groupName", groupName);
                    break;
                case "Admins + Name + Group":
                    selectCommand = new SqlCommand("SELECT .Admins.* FROM Admins INNER JOIN Users ON Admins.user_id = Users.user_id WHERE (Users.user_group = @groupName) AND (Admins.username = @userName)" /*OR (Users.username = @userName)"*/, sqlConnection);
                    selectCommand.Parameters.AddWithValue("@userName", value);
                    selectCommand.Parameters.AddWithValue("@groupName", groupName);
                    break;
                case "Admins 1":
                case "Admins 2":
                case "Admins 3":
                    selectCommand = new SqlCommand("SELECT * FROM Admins WHERE user_id = @userId AND admin_type = @adminType", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@userId", ID);
                    selectCommand.Parameters.AddWithValue("@adminType", table.Split(' ')[1]);
                    break;
                case "Users":
                    selectCommand = new SqlCommand("SELECT * from Users WHERE user_id = @userId", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@userId", ID);
                    break;
                case "Users + Group":
                    selectCommand = new SqlCommand("SELECT * from Users WHERE user_id = @userId AND user_group = @groupName", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@userId", ID);
                    selectCommand.Parameters.AddWithValue("@groupName", groupName);
                    break;
                case "Users + Name":
                    selectCommand = new SqlCommand("SELECT * from Users WHERE username = @userName", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@userName", value);
                    break;
                case "Users + Name + Group":
                    selectCommand = new SqlCommand("SELECT * from Users WHERE username = @userName AND user_group = @groupName", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@userName", value);
                    selectCommand.Parameters.AddWithValue("@groupName", groupName);
                    break;
                case "News ?":
                    selectCommand = new SqlCommand("SELECT count(*) from News", sqlConnection);
                    break;
                case "News":
                    selectCommand = new SqlCommand("SELECT * from News WHERE newsNumber = @newsNumber", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@newsNumber", ID);
                    break;
                case "Calls ?":
                    selectCommand = new SqlCommand($"SELECT count(*) from [{groupName}_Calls]", sqlConnection);
                    break;
                case "Homework ?":
                    selectCommand = new SqlCommand($"SELECT count(*) from [{groupName}_Homework]", sqlConnection);
                    break;
                case "Homework":
                    selectCommand = new SqlCommand($"SELECT count(*) from [{groupName}_Homework] WHERE id_Homework = @idHomework", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@idHomework", ID);
                    break;
                case "Homework ~ noDz":
                    selectCommand = new SqlCommand($"SELECT count(*) FROM [{groupName}_Homework] WHERE (lesson_name = N'⠀⊗ Нет Дз')", sqlConnection);
                    break;
                case "Homework ~ noPairs":
                    selectCommand = new SqlCommand($"SELECT count(*) FROM [{groupName}_Homework] WHERE (lesson_name = N'⠀⊗ Отмена Пар')", sqlConnection);
                    break;
                case "Schedule ?":
                    selectCommand = new SqlCommand($"SELECT count(*) from [{groupName}_{value}_Schedule]", sqlConnection);
                    break;
                case "GroupName ?":
                    selectCommand = new SqlCommand("SELECT count(*) FROM Users WHERE user_id = @userId AND user_group = @groupName", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@userId", ID);
                    selectCommand.Parameters.AddWithValue("@groupName", groupName);
                    break;
                case "Groups ?":
                    selectCommand = new SqlCommand($"SELECT count(*) FROM Groups WHERE group_Name = @groupName", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@groupName", groupName);
                    break;
                case "GroupDayDZ":
                    selectCommand = new SqlCommand("SELECT count(*) FROM Groups WHERE group_Name = @groupName AND week_Day_DZ = @weekDayDZ", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@weekDayDZ", value);
                    selectCommand.Parameters.AddWithValue("@groupName", groupName);
                    break;
                default:
                    selectCommand = new SqlCommand($"SELECT count(*) FROM {table} WHERE user_id = @userId", sqlConnection);
                    selectCommand.Parameters.AddWithValue("@userId", ID);
                    break;
            }

            using (var reader = selectCommand.ExecuteReader())//выполняем проверку и результат записываем в result
            {
                long count = 0;
                if (reader.Read())
                {
                    try// Приводим значение к нужному типу данных
                    {
                        count = reader.GetInt32(0);
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
                reader.Close();
                // Выполняем запрос и получаем одно значение
                object scalar = selectCommand.ExecuteScalar();
                if (scalar is not null)
                {
                    try// Приводим значение к нужному типу данных
                    {
                        count = (int)scalar;
                    }
                    catch
                    {
                        count = (long)scalar;
                    }
                    // Проверяем значение
                    if (count > 0)
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

            if (await dataAvailability(userID, "Admins"))//является ли админом
            {
                isAdmin = true;
            }

            sqlConnection.Open();

            selectCommandUser = new SqlCommand("SELECT * FROM Users WHERE user_id = @userId", sqlConnection);//считываем данные о человеке
            selectCommandUser.Parameters.AddWithValue("@userId", userID);
            readerUser = selectCommandUser.ExecuteReader();

            if (readerUser.Read())
            {
                string adminRang = "пустышка";
                long userId = readerUser.GetInt64(readerUser.GetOrdinal("user_id"));
                globalGroupName = readerUser.GetString(readerUser.GetOrdinal("user_group"));
                globalUsername = readerUser.GetString(readerUser.GetOrdinal("username"));
                readerUser.Close();
                if (isAdmin)//является ли админом
                {
                    selectCommandAdmin = new SqlCommand("SELECT * FROM Admins WHERE user_id = @userId", sqlConnection);
                    selectCommandAdmin.Parameters.AddWithValue("@userId", userID);

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
                        $"\n*➣Группа:* {await EscapeMarkdownV2(globalGroupName)}," +
                        $"\n*➢Администратор:* _Является_ администратором," +
                        $"\n*➣Уровень администратора:* {adminType} \\- {adminRang}";

                }
                else//записываем сообщение в обоих случаях 
                {
                    messageInfo = $"*❒Информация о пользователе❒*" +
                        $"\n*➣Id:* {userId}," +
                        $"\n*➢Имя:* {await EscapeMarkdownV2(globalUsername)}," +
                        $"\n*➣Группа:* {await EscapeMarkdownV2(globalGroupName)}," +
                        $"\n*➢Администратор:* _Не является_ администратором";
                }
            }
            sqlConnection.Close();
            if (globalUsername == "СтандартИмя" || globalUsername == "НетИмени")
            {
                await botClient.SendTextMessageAsync(globalChatId, "❕Нам не удалось считать ваше _имя_, пожалуйста добавьте его _\\(Желательно ник TG, Без @\\)_ в своем *профиле* для возможной связи\\!\n" +
                    "✏️Для изменения имени нажмите *📝Изменить имя*", replyMarkup: Keyboards.MainMenu, parseMode: ParseMode.MarkdownV2);
            }
            return messageInfo;//и возвращаем его
        }

        async Task<bool> buttonTest()//проверяем все ли действия выполнены т.е все ли кнопки в массиве false
        {
            bool result = true;
            foreach (var testActiveButton in pressingButtons)
            {
                if (testActiveButton.Value) // проверяем значение текущего элемента
                {
                    if (testActiveButton.Key.Contains("Timer"))
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"⏳В данный момент запущен таймер, для остановки таймера введите:\n/stopTimer");
                    }
                    else
                    {
                        result = false;
                        await botClient.SendTextMessageAsync(globalChatId, "⭕️Для выполнения другого действия закончите работу с предыдущим\nℹ️Или введите: /cancel");
                        break;
                    }
                }
            }
            return result;
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
                caption = $"⌞⌋𝕹𝕰𝖂𝕾 *№{idNews}*⌊⌟\n" +
                    $"*{await EscapeMarkdownV2(newsTitle)}\n\n*"
                  + $"{await EscapeMarkdownV2(news)}\n\n"
                  + $"║☱*Автор: {await EscapeMarkdownV2(userName)}*☱║\n"
                  + $"*—–\\-⟨{await EscapeMarkdownV2($"{data:yyyy-MM-dd}")}⟩\\-–—*";

                if (globalMessagePhotoId != 0)//если можно отредактировать сообщение
                {
                    try
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
                    catch
                    {
                        await DeleteMessage(globalMessagePhotoId);
                        globalMessagePhotoId = 0;

                        if (!ImageIsNull)
                        {
                            var messageRes = await botClient.SendPhotoAsync(
                              chatId: globalChatId,
                              photo: InputFile.FromStream(stream: ms, fileName: "image"),//тут не нужно создавать импорт медиа т.к мы не редактируем
                              caption: caption + "\n", // Добавляем символ перевода строки может и не нужен, хотел чтоб фотка была ниже текста но для этого нужно вставлять фотку как ссылку в текст
                              parseMode: ParseMode.MarkdownV2, replyMarkup: Keyboards.newsButton);
                            globalMessagePhotoId = messageRes.MessageId;
                        }
                    }
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

        async Task Cancel(bool changeAllValues = false, bool messageEnabled = true, bool messageDelete = false)
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
            if (messageDelete)
            {
                await DeleteMessage(globalDeleteMessageCancelId);
                globalDeleteMessageCancelId = 0;
            }
        }

        async Task<string> DzInfoAdd(string groupName)
        {
            if (await ReturnDayWeek(true))
            {
                await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]: Невозможно получить английское название дня недели для русского дня", replyMarkup: Keyboards.cancel);
                return "ОШИБКА";
            }

            if (!await dataAvailability(0, "Homework ?", groupName: globalGroupName) || !await dataAvailability(0, "GroupDayDZ", EnglishDayThen, globalGroupName))
            {
                await FullDeleteDz(globalGroupName);//просто удаляем всё дз уже прошедшего дня

                if (await dataAvailability(0, "Schedule ?", EnglishDayThen, globalGroupName))
                {
                    await CopyDataDz(EnglishDayThen, globalGroupName);//копируем данные расписания из расписания в дз
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Расписания на завтра отсутствует или еще время до 10:00 для заполнения нового дня [3002]");
                    return "ОШИБКА";
                }
            }

            await WhatWeekType(globalGroupName);
            // открытие соединения
            sqlConnection.Open();

            // создание команды для выполнения SQL-запроса с JOIN-ом таблиц
            string query = @$"SELECT id_Homework, lesson_name,
                            CASE WHEN homework IS NULL THEN 'X' ELSE 'V' END AS homework_flag 
                            FROM [{groupName}_Homework]";
            SqlCommand command = new(query, sqlConnection);
            string result = "📊Текущая статистика ДЗ:\n*ID┇Flag┇Расписание*\n";
            bool FTres = true;
            // создание объекта SqlDataReader для чтения данных
            using (SqlDataReader reader = command.ExecuteReader())
            {
                // обход результатов выборки
                while (reader.Read())
                {
                    // чтение значений из текущей строки таблицы
                    int id_Homework = (int)reader["id_Homework"];
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
                    result += $" `{id_Homework}` ┇ {homework_flag} ┇{await EscapeMarkdownV2(await RemoveDigitsAsync(lesson_name))}\n";
                }
            }
            sqlConnection.Close();

            result += $"☰ {FTres}";

            return result;
        }

        async Task<string> DzInfoEdit(string groupName)
        {
            if (await ReturnDayWeek(true))
            {
                await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]: Невозможно получить английское название дня недели для русского дня", replyMarkup: Keyboards.cancel);
                return "ОШИБКА";
            }

            if (!await dataAvailability(0, "Homework ?", groupName: globalGroupName) || !await dataAvailability(0, "GroupDayDZ", EnglishDayThen, globalGroupName))
            {
                await FullDeleteDz(globalGroupName);//просто удаляем всё дз уже прошедшего дня

                if (await dataAvailability(0, "Schedule ?", EnglishDayThen, globalGroupName))
                {
                    await CopyDataDz(EnglishDayThen, globalGroupName);//копируем данные расписания из расписания в дз
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Расписания на завтра отсутствует или еще время до 10:00 для заполнения нового дня [3002]");
                    return "ОШИБКА";
                }
            }

            await WhatWeekType(globalGroupName);
            // открытие соединения
            sqlConnection.Open();

            // создание команды для выполнения SQL-запроса с JOIN-ом таблиц
            string query = @$"SELECT id_Homework, lesson_name, homework 
                            FROM [{groupName}_Homework]";
            SqlCommand command = new(query, sqlConnection);
            string result = "📊Текущее ДЗ:\n*ID┇Расписание*\n";
            // создание объекта SqlDataReader для чтения данных
            using (SqlDataReader reader = command.ExecuteReader())
            {
                // обход результатов выборки
                while (reader.Read())
                {
                    int id_Homework;
                    string lesson_name, homework;
                    // чтение значений из текущей строки таблицы
                    id_Homework = (int)reader["id_Homework"];
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
                    result += $" `{id_Homework}` ┇{await EscapeMarkdownV2(await RemoveDigitsAsync(lesson_name))}: `{await EscapeMarkdownV2(homework)}`\n";
                }
            }
            sqlConnection.Close();
            return result;
        }

        async Task<object[]> DzBaseChat(string groupName, bool EditMessage = false)
        {
            object[] Final = new object[] { 0, false };
            if (EditMessage)
            {
                await ReturnGroupIdMessageDz(groupName);
                if (globalIdEditMessage == 0)
                {
                    return Final;
                }
            }

            await WhatWeekType(globalGroupName);
            sqlConnection.Close();

            string lesson_name1 = "", lesson_name2 = "", TextPhoto = "", TextMessage = "";
            byte[] image = null;
            MemoryStream MemStreamImage = new();

            if (await ReturnDayWeek(true))
            {
                await botClient.SendTextMessageAsync(globalAdminId, $"❌Ошибка при отправки дз в чат  [1003]", replyMarkup: Keyboards.cancel);
                return Final;
            }

            if (await dataAvailability(0, "Homework ?", groupName: globalGroupName))//проверяю есть ли вообще дз в базе
            {
                // открытие соединения
                await sqlConnection.OpenAsync();

                // создание команды для выполнения SQL-запроса с JOIN-ом таблиц
                string queryHome = @$"SELECT lesson_name, homework 
                                FROM [{groupName}_Homework]";
                SqlCommand commandDz = new(queryHome, sqlConnection);

                TextMessage = $"*⠀  —–⟨Дз на {await EscapeMarkdownV2(RussianDayThen)}⟩–—*\n" +
                        $"*   —––⟨{weekType}⟩––—*\n";
                int homeworkNullCount = 0;
                // создание объекта SqlDataReader для чтения данных
                using (SqlDataReader reader = commandDz.ExecuteReader())
                {
                    int count = 0;
                    while (reader.Read())
                    {
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

                sqlConnection.Open();
                SqlCommand command = new("SELECT image_Dz FROM Groups WHERE group_Name = @groupName", sqlConnection);
                command.Parameters.AddWithValue("@groupName", groupName);
                SqlDataReader readerImg = command.ExecuteReader();
                if (readerImg.Read())
                {
                    // Получаем изо из базы для дз
                    if (readerImg is not null && !readerImg.IsDBNull(readerImg.GetOrdinal("image_Dz")))
                    {
                        image = (byte[])readerImg["image_Dz"];
                        MemStreamImage = new(image);
                    }
                }
                sqlConnection.Close();
                if (image == null)
                {
                    object[] ingMass = await ReturnImageDz();
                    if ((bool)ingMass[0] == true)
                    {
                        image = (byte[])ingMass[3];
                        MemStreamImage = new(image);
                    }
                }

                await ReturnGroupIdChatDz(groupName);

                if (globalIdBaseChat != 0)
                {
                    if (EditMessage)
                    {
                        //await ReturnGroupIdMessageDz(groupName);

                        /*if (globalIdEditMessage != 0)
                        {*/
                        if (image == null)
                        {
                            // Заменяем текст в сообщении обязательно !EditMessageCaptionAsync!
                            var editedTextMessage = await botClient.EditMessageCaptionAsync(globalIdBaseChat,
                                globalIdEditMessage, TextMessage,
                                parseMode: ParseMode.MarkdownV2);
                        }
                        else
                        {
                            //преобразуем из FileStream и создаем импорт медиа

                            InputMediaPhoto inputMediaPhoto = new(InputFile.FromStream(MemStreamImage, "imageDz.png"));

                            // Заменяем фотографию в сообщении
                            var editedPhotoMessage = await botClient.EditMessageMediaAsync(globalIdBaseChat,
                                globalIdEditMessage, inputMediaPhoto);
                            // Заменяем текст в сообщении обязательно !EditMessageCaptionAsync!
                            var editedTextMessage = await botClient.EditMessageCaptionAsync(globalIdBaseChat,
                                globalIdEditMessage, TextMessage,
                                parseMode: ParseMode.MarkdownV2);
                        }
                        //}
                    }
                    else
                    {
                        if (image == null)
                        {
                            var messageRes = await botClient.SendTextMessageAsync(chatId: globalIdBaseChat, text: TextMessage, parseMode: ParseMode.MarkdownV2);
                            globalIdEditMessage = messageRes.MessageId;
                        }
                        else
                        {
                            // Отправка фото пользователю в Telegram
                            var messageRes = await botClient.SendPhotoAsync(chatId: globalIdBaseChat, photo: InputFile.FromStream(MemStreamImage, "imageDz.png"),
                                                           caption: TextMessage, parseMode: ParseMode.MarkdownV2);
                            globalIdEditMessage = messageRes.MessageId;
                        }

                        await sqlConnection.OpenAsync();
                        using SqlCommand command2 = new("UPDATE Groups SET id_Message_DZ = @idMessageDZ, update_time = @updateTime WHERE group_Name = @groupName", sqlConnection);
                        command2.Parameters.AddWithValue("@groupName", groupName);
                        command2.Parameters.AddWithValue("@idMessageDZ", globalIdEditMessage);
                        command2.Parameters.AddWithValue("@updateTime", DateTime.Now);
                        await command2.ExecuteNonQueryAsync();
                        await sqlConnection.CloseAsync();
                    }
                    Final = new object[] { homeworkNullCount, true };
                }
                else
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка при получении ID группы отправки дз [3002]");
                }
                MemStreamImage.Close();
            }
            return Final;
        }

        async Task<string[]> CallsInfo(string groupName, bool isImage = false)
        {
            string Calls = $"🛎*Расписание звонков {await EscapeMarkdownV2(groupName)}:*\n";
            int count = 0;
            if (await dataAvailability(0, "Calls ?", groupName: groupName))
            {
                //sqlConnection.Close();
                await sqlConnection.OpenAsync();
                SqlCommand command = new($"SELECT time_interval, note FROM [{groupName}_Calls]", sqlConnection);
                SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())//считываем звонки из базы
                {
                    string time = reader.GetString(reader.GetOrdinal("time_interval"));
                    string note = reader.GetString(reader.GetOrdinal("note"));
                    //составляем сообщение
                    if (!isImage)
                    {
                        Calls += $"`{time}`" + note + "\n";
                    }
                    else
                    {
                        Calls += $"{time}" + "\n";
                    }
                    count++;
                }
                reader.Close();
                await sqlConnection.CloseAsync();
            }
            else
            {
                Calls += "\n🛑*Отсутствует*🛑\n⠀";
                count = 2;
            }
            //return Calls + "☰" + count.ToString();

            return new string[2] { Calls, count.ToString() };
        }

        async Task TimerBot(int time = 5, bool isSengMessage = true)
        {
            TimerState timerState = new();
            //System.Threading.Timer Timer;
            // Создаем объект состояния таймера
            timerState = new TimerState
            {
                BotClient = botClient,
                ChatId = globalChatId,
                PressingButtons = pressingButtons,
                ShutdownTimer = shutdownTimer,
                BlockedUser = blockedUser,
                SpamDetector = spamDetector,
                SqlConnection = sqlConnection
            };
            //Timer = new System.Threading.Timer(async state => await timerState.TimerCallback(state), timerState, TimeSpan.FromMinutes(time), TimeSpan.Zero);

            // Создаем таймер и передаем ему метод обратного вызова
            System.Timers.Timer timer = new System.Timers.Timer(TimeSpan.FromMinutes(time).TotalMilliseconds);
            timer.Elapsed += async (sender, e) => await timerState.TimerCallback();//метод выполенения
            timer.AutoReset = false;//запуск 1 раз
            timer.Start();

            if (isSengMessage) await botClient.SendTextMessageAsync(globalChatId, $"⏳Таймер запущен на {time} мин, для остановки таймера введите:\n/stopTimer");
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
                    Console.WriteLine($"[{DateTime.Now}] Начало отправки сообщений -------------------->");
                    Console.ResetColor();
                    // формирование строки
                    try
                    {
                        await botClient.SendChatActionAsync(user_id, ChatAction.Typing);
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
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{DateTime.Now}] Конец  отправки сообщений <--------------------");
            Console.ResetColor();
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

        async Task UpdateUserStatusAsync(long userId, bool locked, bool autoLocked = false)
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


                using SqlCommand command = new("UPDATE Users SET is_blocked = @Locked WHERE user_id = @UserId", sqlConnection);
                command.Parameters.AddWithValue("@Locked", locked);
                command.Parameters.AddWithValue("@UserId", userId);

                int rowsAffected = await command.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    if (!autoLocked)
                    {
                        await botClient.SendTextMessageAsync(chatId: globalChatId, text: $"✅ {lockedText} пользователя ID: {userId} удалась!");
                    }
                    await UpdateIndexStatisticsAsync("Users");//обновляем индекса таблицы
                    await botClient.SendChatActionAsync(userId, ChatAction.Typing);
                    await botClient.SendTextMessageAsync(chatId: userId, text: $"❗️Вам выдана {lockedText}❗️");
                }
                else
                {
                    await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);
                    await botClient.SendTextMessageAsync(chatId: globalChatId, text: $"❌ Ошибка [2002]: {lockedText} пользователя ID: {userId} не удалась...");
                }
            }
            catch (Exception ex)
            {
                // Обработка ошибок
                await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);
                await botClient.SendTextMessageAsync(chatId: globalChatId, text: $"❌ Ошибка [2002]: {lockedText} пользователя ID: {userId} не удалась: {ex.Message}");
            }
            await sqlConnection.CloseAsync();
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
            SqlCommand insertCommand = new("insert into Users(user_id, username, user_group, dateAdditions) values (@userId, @username, @userGroup, @dateAdditions)", sqlConnection);
            insertCommand.Parameters.AddWithValue("@userId", UserId);
            insertCommand.Parameters.AddWithValue("@username", UserName);
            insertCommand.Parameters.AddWithValue("@userGroup", "НетГруппы");
            insertCommand.Parameters.AddWithValue("@dateAdditions", DateTime.Now);
            globalGroupName = "НетГруппы";
            insertCommand.ExecuteNonQuery();
            await sqlConnection.CloseAsync();
            if (UserName == "СтандартИмя" || UserName == "НетИмени")
            {
                try
                {
                    await botClient.SendTextMessageAsync(globalChatId, "❕Нам не удалось считать ваше _имя_, пожалуйста добавьте его _\\(Желательно ник TG, Без @\\)_ в своем *профиле* для возможной связи\\!\n" +
                        "✏️Для изменения имени войдите в /profile", replyMarkup: Keyboards.MainMenu, parseMode: ParseMode.MarkdownV2);
                }
                catch { }
            }
        }

        async Task CopyDataDz(string DayName, string groupName)
        {//копирование расписания из таблицы расписания в дз по дню недели
            await ReturnGroupIdMessageDz(groupName);
            sqlConnection.Close();

            // открытие соединения
            await sqlConnection.OpenAsync();
            // создание команды для выполнения SQL-запроса на копирование номеров расписания в дз
            SqlCommand command = new($"INSERT INTO [{groupName}_Homework] (lesson_name) SELECT lesson_name FROM [{groupName}_{DayName}_Schedule]", sqlConnection);
            // выполнение команды
            await command.ExecuteNonQueryAsync();
            //запоминаю для опред группы для какого для недели записывается дз
            command = new("UPDATE Groups SET week_Day_DZ = @weekDayDZ WHERE group_Name = @groupName", sqlConnection);
            command.Parameters.AddWithValue("@groupName", groupName);
            command.Parameters.AddWithValue("@weekDayDZ", DayName);
            await command.ExecuteNonQueryAsync();

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
            if (await spamDetector.IsUserBlockedAsync(ID))//проверка на блокировку
            {
                return true;
            }
            if (spamDetector.IsSpam(ID))//проверяем на спам
            {
                // Обработка спама
                Console.ForegroundColor = ConsoleColor.Red;//цвет консоли
                Console.WriteLine($"[{DateTime.Now}] | {globalChatId} | {globalUsername} | Пользователь отправлял слишком много сообщений в короткое время!");
                Console.ResetColor();
                await UpdateUserStatusAsync(globalUserId, true, true);//блокируем пользователя

                if (blockedUser.ContainsKey(globalUserId))
                {// Пользователь найден в словаре              
                    DateTime blockedTime = blockedUser[globalUserId]; // Получаем время блокировки пользователя из словаря
                    TimeSpan timeSinceBlocked = DateTime.Now.Subtract(blockedTime); // Вычисляем разницу между текущим временем и временем блокировки

                    // Проверяем, прошло ли меньше 1часа с момента блокировки
                    if (timeSinceBlocked.TotalHours <= 1)
                    {
                        await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);
                        await botClient.SendTextMessageAsync(chatId: globalChatId, text: "❌Ошибка [1005]: Вы повторно заблокированы за спам...");
                        await botClient.SendTextMessageAsync(chatId: globalAdminId, text: $"🛑Пользователь '{globalUserId}' окончательно заблокирован за спам🛑", parseMode: ParseMode.MarkdownV2);
                    }
                    else
                    {
                        blockedUser.Add(globalUserId, DateTime.Now);
                        pressingButtons["blocedTimer"] = true;
                        // Создаем объект  таймера
                        await TimerBot(10, false);

                        await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);
                        await botClient.SendTextMessageAsync(chatId: globalChatId, text: "❌Ошибка [1005]: Вы замучены на 10 минут за спам...");
                        await botClient.SendTextMessageAsync(chatId: globalAdminId, text: $"🛑Пользователь '{globalUserId}' замучен на 10 минут за спам🛑\n" +
                            $"Для отмены автоматического анблока введите:\n/stopTimer", parseMode: ParseMode.MarkdownV2);//отменить анбан всех но пусть будет
                    }
                }
                else
                {
                    // Пользователь не найден
                    blockedUser.Add(globalUserId, DateTime.Now);
                    pressingButtons["blocedTimer"] = true;
                    // Создаем объект  таймера
                    await TimerBot(10, false);

                    await botClient.SendChatActionAsync(globalChatId, ChatAction.Typing);
                    await botClient.SendTextMessageAsync(chatId: globalChatId, text: "❌Ошибка [1005]: Вы замучены на 10 минут за спам...");
                    await botClient.SendTextMessageAsync(chatId: globalAdminId, text: $"🛑Пользователь '{globalUserId}' замучен на 10 минут за спам🛑\n" +
                        $"Для отмены автоматического анблока введите:\n/stopTimer", parseMode: ParseMode.MarkdownV2);//отменить анбан всех но пусть будет
                }
                return true;
            }
            return false;
        }

        async Task WhatWeekType(string groupName)
        {
            string valueWeekType = "";

            await sqlConnection.OpenAsync();

            using (SqlCommand command = new SqlCommand("SELECT week_Type FROM Groups WHERE group_Name = @groupName", sqlConnection))
            {
                command.Parameters.AddWithValue("@groupName", groupName);
                using (SqlDataReader readerImg = await command.ExecuteReaderAsync())
                {
                    if (await readerImg.ReadAsync())
                    {// Получаем значение week_Type из базы
                        if (!readerImg.IsDBNull(readerImg.GetOrdinal("week_Type")))
                        {
                            if ((bool)readerImg["week_Type"])
                            {
                                valueWeekType = "Числитель";
                            }
                            else
                            {
                                valueWeekType = "Знаменатель";
                            }
                        }
                    }
                }
            }

            if (valueWeekType == "")
            {
                valueWeekType = "Числитель";

                using (SqlCommand updateCommand = new SqlCommand("UPDATE Groups SET week_Type = @weekType WHERE group_Name = @groupName", sqlConnection))
                {
                    updateCommand.Parameters.AddWithValue("@weekType", 1);
                    updateCommand.Parameters.AddWithValue("@groupName", groupName);
                    await updateCommand.ExecuteNonQueryAsync();
                }
            }
            await sqlConnection.CloseAsync();

            weekType = valueWeekType;
            /*try
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
            }*/
        }

        /*Task<string[]> AddToArray(string[] array, string element)
        {
            Array.Resize(ref array, array.Length + 1);
            array[array.Length - 1] = element;
            return Task.FromResult(array);
        }*/

        Task<string> FormatTime(TimeSpan time)
        {
            string result;
            if (time.TotalHours >= 24)
            {
                int day = (int)time.Days;
                int hours = (int)time.TotalHours;
                int minutes = time.Minutes;
                int seconds = time.Seconds;

                result = $"{day} дней, {hours} часов";
                if (minutes != 0)
                {
                    result += $", {minutes} минут";
                }
                if (seconds != 0)
                {
                    result += $", {seconds} секунд";
                }

                return Task.FromResult(result);
            }
            else if (time.TotalMinutes >= 60)
            {
                int hours = (int)time.TotalHours;
                int minutes = time.Minutes;
                int seconds = time.Seconds;

                result = $"{hours} часов, {minutes} минут";
                if (seconds != 0)
                {
                    result += $", {seconds} секунд";
                }

                return Task.FromResult(result);
            }
            else
            {
                int minutes = (int)time.TotalMinutes;
                int seconds = time.Seconds;

                return Task.FromResult($"{minutes} минут, {seconds} секунд");
            }
        }

        async Task ReturnGroupIdMessageDz(string groupName)
        {
            string ErrorMessageID = "НЕТоШИБКИ";
            DateTime timeMessage = DateTime.MinValue;//типо 0 в DateTime
            int idMessage = 0;

            sqlConnection.Open();
            SqlCommand command = new("SELECT * FROM Groups WHERE group_Name = @groupName", sqlConnection);
            command.Parameters.AddWithValue("@groupName", groupName);
            using (SqlDataReader reader = command.ExecuteReader())
            {
                // обязательно Read
                if (reader.Read())
                {
                    idMessage = (int)reader["id_Message_DZ"];
                    if (!reader.IsDBNull(reader.GetOrdinal("update_time")))
                    {
                        timeMessage = reader.GetDateTime(reader.GetOrdinal("update_time"));
                    }
                }
            }
            sqlConnection.Close();

            if (idMessage != 0 || timeMessage != DateTime.MinValue)// Проверка, что значение не равно DateTime.MinValue
            {
                // Вычисление времени на следующий день и 10:00
                DateTime nextDay = timeMessage.AddDays(1).AddHours(-(timeMessage.Hour - 10));

                // Проверка времени
                if (DateTime.Now <= nextDay)
                {
                    // Время не превышено, присваиваем значение из файла
                    globalIdEditMessage = idMessage;
                }
                else
                {
                    // Время превышено, присваиваем значение 0
                    globalIdEditMessage = 0;
                    ErrorMessageID = "Время действия файла превышено";
                }
            }
            else
            {
                // Обнуленное ID
                globalIdEditMessage = 0;
                ErrorMessageID = "Ошибка 0";
            }


            if (globalIdEditMessage == 0)
            {
                if (ErrorMessageID == "Время действия файла превышено")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[{DateTime.Now}] Время действия фила \"globalDzChat\" для группы {groupName} превышено, назначено значение 0");
                    Console.ResetColor();

                    await sqlConnection.OpenAsync();

                    using SqlCommand command2 = new("UPDATE Groups SET id_Message_DZ = @idMessageDZ, update_time = @updateTime WHERE group_Name = @groupName", sqlConnection);
                    command2.Parameters.AddWithValue("@groupName", groupName);
                    command2.Parameters.AddWithValue("@idMessageDZ", globalIdEditMessage);
                    command2.Parameters.AddWithValue("@updateTime", DateTime.Now);
                    await command2.ExecuteNonQueryAsync();

                    await sqlConnection.CloseAsync();
                }
                else if (ErrorMessageID != "Ошибка 0" && ErrorMessageID != "НЕТоШИБКИ")
                {
                    await botClient.SendTextMessageAsync(globalChatId, $"*❌Ошибка при получении редактируемого ID сообщения [1100]:* _{ErrorMessageID}_", parseMode: ParseMode.MarkdownV2);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{DateTime.Now}] Ошибка при получении редактируемого ID сообщения: {ErrorMessageID}");
                    Console.ResetColor();
                }
            }
        }

        async Task ReturnGroupIdChatDz(string groupName)
        {
            await sqlConnection.OpenAsync();

            using (SqlCommand command = new("SELECT chat_id FROM Groups WHERE group_Name = @groupName", sqlConnection))
            {
                command.Parameters.AddWithValue("@groupName", groupName);

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        int chatIdOrdinal = reader.GetOrdinal("chat_id");

                        if (!await reader.IsDBNullAsync(chatIdOrdinal))
                        {
                            globalIdBaseChat = reader.GetInt64(chatIdOrdinal);
                        }
                        else
                        {
                            globalIdBaseChat = 0;
                        }
                    }
                    else
                    {
                        globalIdBaseChat = 0;
                    }
                }
            }

            sqlConnection.Close();
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

                    try
                    {
                        RussianDayYesterday = weekDays.Keys.ElementAt(weekDays.Values.ToList().IndexOf(EnglishDayYesterday));
                        RussianDayNow = weekDays.Keys.ElementAt(weekDays.Values.ToList().IndexOf(EnglishDayNow));
                        RussianDayThen = weekDays.Keys.ElementAt(weekDays.Values.ToList().IndexOf(EnglishDayThen));
                    }
                    catch
                    {
                        await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка [1003]", replyMarkup: Keyboards.cancel);
                        return Exception = true;
                    }
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

        async Task<bool> ReturnGroupName()
        {
            if (!await dataAvailability(globalUserId, "GroupName ?", groupName: globalGroupName))
            {
                // Проверяем, есть ли вообще userId в таблице Users
                if (!await dataAvailability(globalUserId, "Users"))
                {
                    await AddUserToDatabase(globalUserId, globalUsername);
                    await botClient.SendTextMessageAsync(globalChatId, "📝Мы не нашли вас в нашей базе, поэтому вы добавлены в базу данных\\!", replyMarkup: Keyboards.MainMenu, parseMode: ParseMode.MarkdownV2);
                }
                else
                {
                    sqlConnection.Open();
                    SqlCommand command = new("SELECT * FROM Users WHERE user_id = @UserId", sqlConnection);
                    command.Parameters.AddWithValue("@UserId", globalUserId);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // обязательно Read
                        if (reader.Read())
                        {
                            globalGroupName = (string)reader["user_group"];
                        }
                    }
                    sqlConnection.Close();

                }
            }
            if (globalGroupName == "НетГруппы")
            {
                await botClient.SendTextMessageAsync(globalChatId, $"*⚠️У вас все еще не выбрана группа, она необходима для выполнения этого действия*\n" +
                    $"ℹ️_Измените группу в профиле или введите команду_ /group",
                    parseMode: ParseMode.MarkdownV2);
                return false;
            }
            if (!await dataAvailability(0, "Groups ?", groupName: globalGroupName))
            {
                await botClient.SendTextMessageAsync(globalChatId, $"*⚠️У вас выбрана незарегистрированная группа, правильная группа необходима для выполнения этого действия\\!*\n" +
                    $"ℹ️_Измените группу в профиле или введите команду_ /group",
                    parseMode: ParseMode.MarkdownV2);
                return false;
            }
            return true;
        }

        async Task ChangeGroup()
        {
            //списки для хранения названий групп и инлайн кнопок
            List<string> groupNames = new();
            List<InlineKeyboardButton[]> inlineButtonRows = new();

            sqlConnection.Close();
            await sqlConnection.OpenAsync();
            //запрос к базе данных для получения данных из столбца "group_Name"
            SqlCommand command = new("SELECT group_Name FROM Groups", sqlConnection);
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    // значения из SqlDataReader и добавим их в списки
                    string groupName = reader.GetString(reader.GetOrdinal("group_Name"));
                    groupNames.Add(groupName);

                    // Создадим инлайн кнопку с использованием названия группы и добавьте ее в текущую строку кнопок
                    InlineKeyboardButton[] inlineButtonRow = new InlineKeyboardButton[]
                    {
                            InlineKeyboardButton.WithCallbackData(text: groupName, callbackData: $"Group {groupName}")
                    };
                    inlineButtonRows.Add(inlineButtonRow);
                }
            }
            InlineKeyboardButton[] inlineButtonCancel = new InlineKeyboardButton[]
            {
                 InlineKeyboardButton.WithCallbackData(text: "🚫Отменить🚫", callbackData: "cancel")
            };
            inlineButtonRows.Add(inlineButtonCancel);
            await sqlConnection.CloseAsync();
            // Создадим объект InlineKeyboardMarkup с использованием списков названий групп и инлайн кнопок
            InlineKeyboardMarkup inlineKeyboardMarkup = new(inlineButtonRows.ToArray());

            var messageResDel = await botClient.SendTextMessageAsync(globalChatId, "*❕Выберите свою группу 👥:*", parseMode: ParseMode.MarkdownV2, replyMarkup: inlineKeyboardMarkup);
            globalDeleteMessageCancelId = messageResDel.MessageId;
        }

        async Task NoDz(string valueNoDz)
        {
            await Cancel(messageEnabled: false, messageDelete: true);

            await clearingTables($"[{globalGroupName}_Homework]");

            //sqlConnection.Close();
            await sqlConnection.OpenAsync();
            SqlCommand insertCommand = new($"INSERT INTO [{globalGroupName}_Homework] (lesson_name, homework) VALUES (@lesson, @homework)", sqlConnection);
            insertCommand.Parameters.AddWithValue("@lesson", "⠀" + valueNoDz);
            insertCommand.Parameters.AddWithValue("@homework", "┈┅┈");
            await insertCommand.ExecuteNonQueryAsync();
            await sqlConnection.CloseAsync();

            await botClient.SendTextMessageAsync(globalChatId, $"❕Теперь текущее дз задано как \"{valueNoDz.Replace("⊗", "").Trim()}\"!", replyMarkup: Keyboards.addComment);

            pressingButtons["addHomework"] = false;
            pressingButtons["changeSchedule"] = false;
            pressingButtons["deleteHomework"] = false;

            if (globalDzInfoAdd != 0)//проверка, можно ли отредактировать сообщение Добавления дз если оно было недавно
            {
                try
                {
                    string text = await DzInfoAdd(globalGroupName);
                    if (text != "ОШИБКА")
                    {
                        await botClient.EditMessageTextAsync(globalChatId, globalDzInfoAdd, text: text.Split('☰')[0].Trim(), parseMode: ParseMode.MarkdownV2); //Обновляю сообщение
                    }
                }
                catch { }
            }
            if (globalDzInfoEdit != 0)//проверка, можно ли отредактировать сообщение Редактирования дз если оно было недавно
            {
                try
                {
                    string text = await DzInfoEdit(globalGroupName);
                    if (text != "ОШИБКА")
                    {
                        await botClient.EditMessageTextAsync(globalChatId, globalDzInfoEdit, text: text.Trim(), parseMode: ParseMode.MarkdownV2); //Обновляю сообщение
                    }
                }
                catch { }
            }
            await ReturnImageDz();
            await DzBaseChat(globalGroupName, true);
        }

        async Task DeleteMessage(int deleteMessageID)
        {
            if (deleteMessageID != 0)
            {
                try
                {
                    await botClient.DeleteMessageAsync(globalChatId, deleteMessageID);
                }
                catch { }
            }
        }

        async Task<object[]> ReturnImageDz()
        {
            await botClient.SendChatActionAsync(globalChatId, ChatAction.UploadPhoto);

            string[] Culls = new string[8];
            byte[] imageBit = new byte[0];
            SizeF textSize;
            string imagePath, Text = "", lesson_name1 = "", lesson_name2 = "";
            bool isDz = true;
            if (await dataAvailability(0, "Homework ~ noDz", groupName: globalGroupName))//если нет дз
            {
                Culls[7] = "*💤На завтра нет Дз\\!🎉*";
                imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "backgroundDzNoDz.png");
                isDz = false;
            }
            else if (await dataAvailability(0, "Homework ~ noPairs", groupName: globalGroupName))//если нет пар
            {
                Culls[7] = "*💤Завтра нет пар\\!🎉*";
                imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "backgroundDzNoPars.png");
                isDz = false;
            }
            else
            {
                if (await dataAvailability(0, "Calls ?", groupName: globalGroupName))
                {//проверяем есть ли вообще звонки в базе
                    string[] calls2 = await CallsInfo(globalGroupName, true);
                    calls2 = calls2[0].Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 1; i < calls2.Length; i++)
                    {
                        string time = calls2[i].Trim();
                        time = await RemoveDigitsAsync(time);
                        time = time.Remove(time.Length - 2) + ":";
                        Culls[i - 1] = time;
                    }
                }

                imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "backgroundDz.png");
            }
            sqlConnection.Close();
            await WhatWeekType(globalGroupName);
            float textWidthF = 195f;
#pragma warning disable CA1416 // Проверка совместимости платформы
            Bitmap image = new Bitmap(imagePath); // обновляю изо
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
                await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка при формировании изображения: [1003]", replyMarkup: Keyboards.cancel);
                graphics.Dispose();// Освободите ресурсы
                image.Dispose();
                return new object[] { false };
            }

            if (await dataAvailability(0, "Homework ?", groupName: globalGroupName))//проверяю есть ли вообще дз в базе
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

                if (!isDz)
                {
                    Text = Culls[7]; //чтоб не создавать доп переменные)))
                }
                else
                {
                    // открытие соединения
                    sqlConnection.Open();
                    // создание команды для выполнения SQL-запроса
                    string queryHome = @$"SELECT lesson_name, homework FROM [{globalGroupName}_Homework]";
                    SqlCommand commandPicDz = new(queryHome, sqlConnection);

                    // создание объекта SqlDataReader для чтения данных
                    using (SqlDataReader reader = commandPicDz.ExecuteReader())
                    {
                        SizeF homeworkSize = graphics.MeasureString(Text, fontHomework, image.Width, stringFormat);
                        SizeF lessonSize = graphics.MeasureString(Text, font, image.Width, stringFormat);
                        SizeF lessonCulls = graphics.MeasureString(Text, fontCulls, image.Width, stringFormat);
                        Text = "*🏠Дз на завтра:*";
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
                                culls = "?:";
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
                                await botClient.SendTextMessageAsync(globalChatId, $"❌Ошибка при формировании изображения: К сожалению весь текст дз не помещается на картинке [1001]", replyMarkup: Keyboards.cancel);
                                // Освободите ресурсы
                                graphics.Dispose();
                                image.Dispose();
                                return new object[] { false };
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
                }
                MemoryStream MemStreamImage = new();
                image.Save(MemStreamImage, ImageFormat.Png);
                imageBit = MemStreamImage.ToArray();
                MemStreamImage.Close();

                // Создаем объект SqlConnection для подключения к базе данных
                using (SqlConnection connection = new(connectionString))
                {
                    connection.Open();

                    // Создаем запрос на обновление данных в таблицу Groups
                    SqlCommand command = new("UPDATE Groups SET image_Dz = @image WHERE group_Name = @groupName", connection);
                    command.Parameters.AddWithValue("@groupName", globalGroupName);

                    SqlParameter imageParam = new("@image", SqlDbType.VarBinary, -1)
                    {
                        Value = imageBit
                    };//обновляем изображение в базе в виде массива байт
                    command.Parameters.Add(imageParam);
                    // Выполняем запрос на добавление данных в таблицу Groups
                    command.ExecuteNonQuery();
                    connection.Close();
                }
                sqlConnection.Close();
                graphics.Dispose();
                image.Dispose();
                return new object[] { true, isDz, Text, imageBit };
            }
            graphics.Dispose();
            image.Dispose();
            return new object[] { false, isDz, Text, imageBit };
#pragma warning restore CA1416 // Проверка совместимости платформы
        }

        async Task<int> FullDeleteDz(string groupName)
        {
            int rowsAffected = 0;

            rowsAffected += await clearingTables($"[{groupName}_Homework]");

            sqlConnection.Open();
            SqlCommand updateCommand = new("UPDATE Groups SET week_Day_DZ = NULL, image_Dz = NULL WHERE group_Name = @groupName", sqlConnection);
            updateCommand.Parameters.AddWithValue("@groupName", groupName);
            rowsAffected += updateCommand.ExecuteNonQuery();
            sqlConnection.Close();

            await ReturnGroupIdMessageDz(groupName);

            return rowsAffected;
        }

        Task<object[]> ReturnNameImgMessage(object[] defaultNameImg, int idMessage)
        {
            if (idMessage == 0) return Task.FromResult(defaultNameImg);
            if (!NameImgMessage.Keys.Contains(idMessage))
            {
                if (NameImgMessage.Count >= 5)
                {
                    NameImgMessage.Remove(NameImgMessage.Keys.First());
                }
                NameImgMessage.Add(idMessage, defaultNameImg);
                return Task.FromResult(defaultNameImg);
            }
            return Task.FromResult(NameImgMessage[idMessage]);
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

            // cancellationToken.

            try
            {
                if (globalChatId == globalAdminId)//проверяю кто отправил сообщение, если не я, то отправляю уведомление с ошибкой и примерно где она была найдена
                {
                    if (ErrorMessage.Contains("Telegram.Bot.Exceptions.RequestException: Request timed out")
                        || ErrorMessage.Contains("Request timed out"))
                    {
                        await botClient.SendTextMessageAsync(globalAdminId, $"❗️Проблемы сети: Request timed out❗️");
                    }
                    else if (ErrorMessage.Contains("Telegram.Bot.Exceptions.RequestException: Exception during making request")
                        || ErrorMessage.Contains("Программа на вашем хост-компьютере разорвала установленное подключение")
                        || ErrorMessage.Contains("Попытка установить соединение была безуспешной"))
                    {
                        await botClient.SendTextMessageAsync(globalAdminId, $"❗️Проблемы сети: Exception during making request❗️");
                    }
                    else if (ErrorMessage.Contains("[409]") && ErrorMessage.Contains("make sure that only one bot instance is running"))
                    {
                        TimeSpan elapsedTime = DateTime.Now - globalStartTime;
                        if (elapsedTime.TotalSeconds < 10)
                        {
                            Environment.Exit(1); //Если было запущенно сразу 2 бота, то тот, кто был запущен позже и время запуска меньше 10 сек завершит свою работу
                        }
                        await botClient.SendTextMessageAsync(globalAdminId, $"❗️Было запущенно 2 бота сразу,\n 2-ой прекратил свою работу❗️");
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
                        await botClient.SendTextMessageAsync(globalAdminId, $"❗️Проблемы сети: Request timed out❗️");
                    }
                    else if (ErrorMessage.Contains("Telegram.Bot.Exceptions.RequestException: Exception during making request")
                        || ErrorMessage.Contains("Программа на вашем хост-компьютере разорвала установленное подключение")
                        || ErrorMessage.Contains("Попытка установить соединение была безуспешной"))
                    {
                        await botClient.SendTextMessageAsync(globalAdminId, $"❗️Проблемы сети: Exception during making request❗️");
                    }
                    else if (ErrorMessage.Contains("[409]") && ErrorMessage.Contains("make sure that only one bot instance is running"))
                    {
                        TimeSpan elapsedTime = DateTime.Now - globalStartTime;
                        if (elapsedTime.TotalSeconds < 10)
                        {
                            Environment.Exit(1); //Если было запущенно сразу 2 бота, то тот, кто был запущен позже и время запуска меньше 10 сек завершит свою работу
                        }
                        await botClient.SendTextMessageAsync(globalAdminId, $"❗️Было запущенно 2 бота сразу,\n 2-ой прекратил свою работу❗️");
                    }
                    else
                    {
                        TimeSpan elapsedTime = DateTime.Now - globalStartTime;
                        if (elapsedTime.TotalSeconds > 10)
                        {
                            await botClient.SendTextMessageAsync(globalChatId, $"❌🛑❌Вы сломали бота(((\n" +
                                $"❗️Вероятно всем пользователям будет выдано ограничение на его использование❗️\n" +
                                $"🛠На исправление уже направлены все силы, это займет како-то время..." +
                                $"\n\n" +
                                $"Ошибка:\n{ErrorMessage}");
                            /*await messageEveryone($"❗️К сожалению у бота возникли критические ошибки, всем пользователям будет выдано ограничение на его использование❗️\n" +
                                $"🛠На исправление уже направлены все силы, это займет како-то время");*/
                        }
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
                    }
                }
            }
            catch { }
            if (globalChatId != globalAdminId)
            {
                await botClient.SendTextMessageAsync(globalAdminId, $"❌🛑❌Поломка бота!\n" +
                                $"У пользователя {globalChatId}, вероятное имя @{Username} , время поломки {DateTime.Now}. Последнее отправленное действие: {Exception}" +
                                $"\n\n" +
                                $"Ошибка:\n{ErrorMessage}");
            }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{DateTime.Now}] Ошибка у пользователя {globalChatId}, вероятное имя @{Username} . Последнее отправленное действие: {Exception} И ошибка {ErrorMessage}");
            Console.ResetColor();
            return;
        }

    }

}