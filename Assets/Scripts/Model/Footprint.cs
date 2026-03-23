using System;

namespace RamRoutes.Model
{
    [Serializable]
    public class Footprint
    {
        public string id;
        public string text;
        public string makerId;
        public string buildingId;
        public long createdAt;

        public Footprint() { }

        public Footprint(string text, string makerId, string buildingId)
        {
            this.text = text;
            this.makerId = makerId;
            this.buildingId = buildingId;
            this.createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
