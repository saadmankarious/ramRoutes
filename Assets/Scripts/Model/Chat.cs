using System;

namespace RamRoutes.Model
{
    public enum WhisperType
    {
        Greeting = 0,
        HaveANiceLift = 1,
        Heart = 2,
        Laugh = 3,
        Fire = 4,
        Celebration = 5,
        Wave = 6,
        Thinking = 7,
        Cool = 8,
        Perfect = 9
    }

    [Serializable]
    public class Chat
    {
        public string fromId;
        public string toId;
        public string chatEmojies; // Will store whisper type as string representation
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
        
        /// <summary>
        /// Get the whisper type from the chatEmojies field
        /// </summary>
        public WhisperType GetWhisperType()
        {
            if (System.Enum.TryParse<WhisperType>(chatEmojies, out WhisperType result))
            {
                return result;
            }
            return WhisperType.Greeting; // Default fallback
        }
        
        /// <summary>
        /// Set the whisper type in the chatEmojies field
        /// </summary>
        public void SetWhisperType(WhisperType whisperType)
        {
            chatEmojies = whisperType.ToString();
        }
    }
}
