using System;

namespace RamRoutes.Model
{
    [Serializable]
    public class Chat
    {
        public string fromId;
        public string toId;
        public string chatEmojies;
        public DateTime timestamp;
        
        public Chat()
        {
            timestamp = DateTime.UtcNow;
        }
        
        public Chat(string fromId, string toId, string chatEmojies)
        {
            this.fromId = fromId;
            this.toId = toId;
            this.chatEmojies = chatEmojies;
            this.timestamp = DateTime.UtcNow;
        }
    }
}
