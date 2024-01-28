
using System.Data.SqlClient;
using System.Data;

namespace LibDz_infoBot
{
    public class SpamDetector
    {
        public SqlConnection sqlConnection;
        public DateTime StartTime;
        private Dictionary<long, List<DateTime>> userMessages;

        public SpamDetector()
        {
            userMessages = new Dictionary<long, List<DateTime>>();
        }

        public bool IsSpam(long userId)
        {
            const int maxMessages = 5;
            TimeSpan timeWindow = TimeSpan.FromSeconds(3);

            if (!userMessages.ContainsKey(userId))
            {
                userMessages.Add(userId, new List<DateTime>());
            }

            var messages = userMessages[userId];
            var currentTime = DateTime.Now;

            messages.RemoveAll(msg => currentTime - msg > timeWindow);
            messages.Add(currentTime);

            if (messages.Count > maxMessages)
            {
                var oldestMessageTime = messages[0];
                var timeElapsed = currentTime - oldestMessageTime;
                TimeSpan elapsedTime = DateTime.Now - StartTime;//не баним пользователя если с момента запуска прошло меньше 30 сек
                return (timeElapsed < timeWindow && elapsedTime.TotalSeconds > 30);
            }

            return false;
        }

        async public Task<bool> IsUserBlockedAsync(long userId)
        {
            sqlConnection.Close();
            bool result = false;//проверка на блокировку
            try
            {
                await sqlConnection.OpenAsync();

                string query = @"IF EXISTS (SELECT * FROM Users WHERE user_id = @UserId AND is_blocked = 1)
                                SELECT 'true'
                            ELSE
                                SELECT 'false'"
                ;

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
    }
}
