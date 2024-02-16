using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using System.Diagnostics;
using System.Reflection;


namespace LibDz_infoBot
{
    internal class RestartBot
    {
        public ITelegramBotClient BotClient;
        public long AdminId;
        public long UserId;
        public async Task Restart(string Message, string ErrorMessage, bool isNoErr = false)
        {
            if (!isNoErr)
            {
                await BotClient.SendTextMessageAsync(AdminId, $"❗️Ошибка [1006]: {Message}, бот предпримет попытку Авто-перезапуска \n\n {ErrorMessage}");
                try
                {
                    await BotClient.SendTextMessageAsync(UserId, $"⭕️ К сожалению на ответ по последнему действию потребуется больше времени (от 2 до 5 мин) или вам потребуется вызвать его снова!..");
                }
                catch { }
            }
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"[{DateTime.Now}] Предпринята попытка перезапуска Бота! Причина: {Message}, с ошибкой {ErrorMessage}");
            Console.ResetColor();
            // Запускаем новое приложение
            string patch = Assembly.GetExecutingAssembly().Location;
            Process.Start(patch.Replace(".dll", ".exe"));

            // Завершаем текущее приложение
            // Environment.Exit(0);
        }
    }
}
