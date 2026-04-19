using System;

// define footprint type
public enum FootprintType
{
    AcademicHelp,
    Food,
    Entertainment,
    Sports,
}
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
        public FootprintType type;

        public Footprint() { }

        public Footprint(string text, string makerId, string buildingId, FootprintType type)
        {
            this.text = text;
            this.makerId = makerId;
            this.buildingId = buildingId;
            this.type = type;
            this.createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
