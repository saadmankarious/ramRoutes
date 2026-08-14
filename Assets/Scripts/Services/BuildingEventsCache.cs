using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using RamRoutes.Model;

namespace RamRoutes.Services
{
    /// <summary>
    /// Loads every building event once at game startup and serves them from memory,
    /// grouped by building name, so selecting a building never waits on its own
    /// Firestore round trip.
    /// </summary>
    public static class BuildingEventsCache
    {
        private const int MaxEventsPerBuilding = 30;

        private static readonly Task<ILookup<string, BuildingEvent>> loadTask = LoadAsync();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Preload()
        {
            _ = loadTask;
        }

        private static async Task<ILookup<string, BuildingEvent>> LoadAsync()
        {
            List<BuildingEvent> allEvents = await BuildingEventService.Instance.GetBuildingEventsAsync();
            return allEvents.ToLookup(e => e.buildingName);
        }

        public static async Task<List<BuildingEvent>> GetEventsForBuildingAsync(string buildingName)
        {
            ILookup<string, BuildingEvent> eventsByBuilding = await loadTask;
            return eventsByBuilding[buildingName].Take(MaxEventsPerBuilding).ToList();
        }
    }
}
