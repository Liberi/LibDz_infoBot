using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LibDz_infoBot
{
    internal class TimerState
    {
        public SqlConnection SqlConnection { get; set; }
        public SpamDetector SpamDetector { get; set; }
        public TelegramBotClient BotClient { get; set; }
        public long ChatId { get; set; }
        public Dictionary<string, bool> PressingButtons { get; set; }
        public bool ShutdownTimer { get; set; }
        public Dictionary<long, DateTime> BlockedUser { get; set; }

        public async Task TimerCallback()
        {
            /*Program MainProgram = (Program)state;
            var MainPr = MainProgram.Main();*/

            if (PressingButtons["shutdownTimerMinuets"])
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"[{DateTime.Now}] Бот был принудительно выключен Админом");
                Console.ResetColor();
                Environment.Exit(0); // завершить приложение с кодом возврата 0, что обычно означает успешное завершение работы приложения
            }
            else if (PressingButtons["blocedTimer"])
            {
                foreach (var item in BlockedUser)
                {
                    long IdUser = item.Key; // Получение ключа
                    DateTime blockedTime = item.Value; // Получение значения
                    TimeSpan timeSinceBlocked = DateTime.Now.Subtract(blockedTime); // Вычисляем разницу между текущим временем и временем блокировки

                    // Проверяем, прошло ли больше 10мин с момента блокировки
                    if (timeSinceBlocked.TotalMinutes >= 1)
                    {
                        if (await SpamDetector.IsUserBlockedAsync(IdUser))//проверка на блокировку
                        {
                            await SqlConnection.OpenAsync();
                            using SqlCommand command = new("UPDATE Users SET is_blocked = @Locked WHERE user_id = @UserId", SqlConnection);
                            command.Parameters.AddWithValue("@Locked", false);
                            command.Parameters.AddWithValue("@UserId", IdUser);

                            int rowsAffected = await command.ExecuteNonQueryAsync();
                            await SqlConnection.CloseAsync();

                            if (rowsAffected > 0)
                            {
                                await BotClient.SendTextMessageAsync(chatId: ChatId, text: $"❗️Вам выдан размут❗️");
                            }
                        }
                    }
                }
            }
        }
    }
}
