
namespace LibDz_infoBot
{
    public class SpamDetector
    {
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
    }
}
