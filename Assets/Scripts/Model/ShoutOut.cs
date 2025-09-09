using System;

namespace RamRoutes.Model
{
    [Serializable]
    public class ShoutOut
    {
        public string fromId;
        public string toId;
        public DateTime timestamp;
        public int kbAmount;
        public int coinAmount;

        public ShoutOut(string fromId, string toId)
        {
            this.fromId = fromId;
            this.toId = toId;
            this.timestamp = DateTime.UtcNow;
            this.kbAmount = 10;
            this.coinAmount = 10;
        }
    }
}
