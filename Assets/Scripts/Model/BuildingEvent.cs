using System;
using UnityEngine;

namespace RamRoutes.Model
{
    [Serializable]
    public class BuildingEvent
    {
        public string buildingId { set; get; }
        public string buildingName  { set; get; }
        public string eventName  { set; get; }
        public DateTime date  { set; get; }

        public BuildingEvent(string buildingId, string buildingName, string eventName, DateTime date)
        {
            this.buildingId = buildingId;
            this.buildingName = buildingName;
            this.eventName = eventName;
            this.date = date;
        }
    }
}
