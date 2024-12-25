
using System.Data.SqlClient;
using System.Data;

namespace LibDz_infoBot
{
    public class SpamDetector
    {
        public SqlConnection sqlConnection;
        public DateTime StartTime;
        private Dictionary<long, List<DateTime>> userMessages;
        int a;
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
            bool result = false;//проверка на блокировку
            try
            {
                if (sqlConnection.State == ConnectionState.Open)
                {
                    await sqlConnection.CloseAsync();
                }

                await sqlConnection.OpenAsync();

                string query = @"SELECT CAST(
                                    CASE
                                        WHEN EXISTS (SELECT 1 FROM Users WHERE user_id = @UserId AND is_blocked = 1)
                                        THEN 1
                                        ELSE 0
                                    END AS BIT)"
                ;

                using SqlCommand command = new(query, sqlConnection);
                command.Parameters.AddWithValue("@UserId", userId);
                object queryResult = await command.ExecuteScalarAsync();

                if (queryResult != null && queryResult != DBNull.Value)
                {
                    result = (bool)queryResult;
                }
            }
            finally
            {
                if (sqlConnection.State == ConnectionState.Open)
                {
                    await sqlConnection.CloseAsync();
                }
            }

            return result;
        }
    }
}
